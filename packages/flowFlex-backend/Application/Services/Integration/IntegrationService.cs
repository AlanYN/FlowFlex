using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.Integration;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FlowFlex.Application.Services.Integration
{
    /// <summary>
    /// Integration service implementation
    /// </summary>
    public class IntegrationService : IIntegrationService, IScopedService
    {
        private readonly IIntegrationRepository _integrationRepository;
        private readonly IEntityMappingRepository _entityMappingRepository;
        private readonly IFieldMappingRepository _fieldMappingRepository;
        private readonly IQuickLinkRepository _quickLinkRepository;
        private readonly IMapper _mapper;
        private readonly UserContext _userContext;
        private readonly ILogger<IntegrationService> _logger;

        // AES encryption key (should be moved to configuration in production)
        private const string ENCRYPTION_KEY = "FlowFlex2024IntegrationKey123456"; // 32 bytes for AES-256

        public IntegrationService(
            IIntegrationRepository integrationRepository,
            IEntityMappingRepository entityMappingRepository,
            IFieldMappingRepository fieldMappingRepository,
            IQuickLinkRepository quickLinkRepository,
            IMapper mapper,
            UserContext userContext,
            ILogger<IntegrationService> logger)
        {
            _integrationRepository = integrationRepository;
            _entityMappingRepository = entityMappingRepository;
            _fieldMappingRepository = fieldMappingRepository;
            _quickLinkRepository = quickLinkRepository;
            _mapper = mapper;
            _userContext = userContext;
            _logger = logger;
        }

        public async Task<long> CreateAsync(IntegrationInputDto input)
        {
            if (input == null)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input cannot be null");
            }

            // Validate name uniqueness
            if (await _integrationRepository.ExistsNameAsync(input.Name))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, $"Integration name '{input.Name}' already exists");
            }

            var entity = _mapper.Map<Domain.Entities.Integration.Integration>(input);
            
            // Encrypt credentials
            entity.EncryptedCredentials = EncryptCredentials(input.Credentials);
            
            // Initialize create information
            entity.InitCreateInfo(_userContext);
            entity.TenantId = _userContext.TenantId;

            var id = await _integrationRepository.InsertReturnSnowflakeIdAsync(entity);
            
            _logger.LogInformation($"Created integration: {input.Name} (ID: {id})");
            
            return id;
        }

        public async Task<bool> UpdateAsync(long id, IntegrationInputDto input)
        {
            if (input == null)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input cannot be null");
            }

            var entity = await _integrationRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
            }

            // Validate name uniqueness (excluding current entity)
            if (await _integrationRepository.ExistsNameAsync(input.Name, id))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, $"Integration name '{input.Name}' already exists");
            }

            // Update properties
            entity.Type = input.Type;
            entity.Name = input.Name;
            entity.SystemName = input.SystemName;
            entity.EndpointUrl = input.EndpointUrl;
            entity.AuthMethod = input.AuthMethod;
            entity.Status = input.Status;
            
            // Encrypt credentials
            entity.EncryptedCredentials = EncryptCredentials(input.Credentials);
            
            // Update modify information
            entity.InitModifyInfo(_userContext);

            var result = await _integrationRepository.UpdateAsync(entity);
            
            _logger.LogInformation($"Updated integration: {input.Name} (ID: {id})");
            
            return result;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _integrationRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
            }

            // Soft delete by setting IsValid to false
            entity.IsValid = false;
            entity.InitModifyInfo(_userContext);

            var result = await _integrationRepository.UpdateAsync(entity);
            
            _logger.LogInformation($"Deleted integration: {entity.Name} (ID: {id})");
            
            return result;
        }

        public async Task<IntegrationOutputDto> GetByIdAsync(long id)
        {
            var entity = await _integrationRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
            }

            return _mapper.Map<IntegrationOutputDto>(entity);
        }

        public async Task<IntegrationOutputDto> GetWithDetailsAsync(long id)
        {
            var entity = await _integrationRepository.GetWithDetailsAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
            }

            return _mapper.Map<IntegrationOutputDto>(entity);
        }

        public async Task<(List<IntegrationOutputDto> items, int total)> GetPagedListAsync(
            int pageIndex,
            int pageSize,
            string? name = null,
            string? type = null,
            string? status = null,
            string sortField = "CreateDate",
            string sortDirection = "desc")
        {
            var (items, total) = await _integrationRepository.QueryPagedAsync(
                pageIndex,
                pageSize,
                name,
                type,
                status,
                sortField,
                sortDirection);

            var dtos = _mapper.Map<List<IntegrationOutputDto>>(items);
            
            return (dtos, total);
        }

        public async Task<bool> TestConnectionAsync(long id)
        {
            var entity = await _integrationRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
            }

            try
            {
                // Decrypt credentials
                var credentials = DecryptCredentials(entity.EncryptedCredentials);
                
                // TODO: Implement actual connection test based on AuthMethod and EndpointUrl
                // For now, just return true
                _logger.LogInformation($"Testing connection for integration: {entity.Name} (ID: {id})");
                
                // Update status to Connected if test succeeds
                entity.Status = global::Domain.Shared.Enums.IntegrationStatus.Connected;
                entity.InitModifyInfo(_userContext);
                await _integrationRepository.UpdateAsync(entity);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Connection test failed for integration: {entity.Name} (ID: {id})");
                
                // Update status to Error if test fails
                entity.Status = global::Domain.Shared.Enums.IntegrationStatus.Error;
                entity.InitModifyInfo(_userContext);
                await _integrationRepository.UpdateAsync(entity);
                
                throw new CRMException(ErrorCodeEnum.BusinessError, $"Connection test failed: {ex.Message}");
            }
        }

        public async Task<bool> UpdateStatusAsync(long id, global::Domain.Shared.Enums.IntegrationStatus status)
        {
            var entity = await _integrationRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
            }

            entity.Status = status;
            entity.InitModifyInfo(_userContext);

            var result = await _integrationRepository.UpdateAsync(entity);
            
            _logger.LogInformation($"Updated integration status: {entity.Name} (ID: {id}) to {status}");
            
            return result;
        }

        #region Private Methods

        private string EncryptCredentials(Dictionary<string, string> credentials)
        {
            var json = JsonConvert.SerializeObject(credentials);
            var encrypted = EncryptString(json, ENCRYPTION_KEY);
            return encrypted;
        }

        private Dictionary<string, string> DecryptCredentials(string encryptedCredentials)
        {
            var json = DecryptString(encryptedCredentials, ENCRYPTION_KEY);
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
        }

        private string EncryptString(string plainText, string key)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = new byte[16]; // Use zero IV for simplicity (should use random IV in production)

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }
            
            return Convert.ToBase64String(ms.ToArray());
        }

        private string DecryptString(string cipherText, string key)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = new byte[16]; // Use zero IV for simplicity (should use random IV in production)

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            
            return sr.ReadToEnd();
        }

        #endregion

        /// <summary>
        /// Get integrations by type
        /// </summary>
        public async Task<List<IntegrationOutputDto>> GetByTypeAsync(string type)
        {
            var entities = await _integrationRepository.GetByTypeAsync(type);
            return _mapper.Map<List<IntegrationOutputDto>>(entities);
        }

        /// <summary>
        /// Get all active (connected) integrations
        /// </summary>
        public async Task<List<IntegrationOutputDto>> GetActiveIntegrationsAsync()
        {
            var entities = await _integrationRepository.GetActiveIntegrationsAsync();
            return _mapper.Map<List<IntegrationOutputDto>>(entities);
        }
    }
}
