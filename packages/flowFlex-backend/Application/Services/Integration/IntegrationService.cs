using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.Integration;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Repository.Action;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Domain.Shared.Models;
using Domain.Shared.Enums;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SqlSugar;

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
        private readonly IFieldMappingService _fieldMappingService;
        private readonly IOutboundConfigurationRepository _outboundConfigurationRepository;
        private readonly IInboundConfigurationRepository _inboundConfigurationRepository;
        private readonly IQuickLinkRepository _quickLinkRepository;
        private readonly IActionDefinitionRepository _actionDefinitionRepository;
        private readonly ISqlSugarClient _sqlSugarClient;
        private readonly IMapper _mapper;
        private readonly UserContext _userContext;
        private readonly ILogger<IntegrationService> _logger;

        // AES encryption key (should be moved to configuration in production)
        private const string ENCRYPTION_KEY = "FlowFlex2024IntegrationKey123456"; // 32 bytes for AES-256

        public IntegrationService(
            IIntegrationRepository integrationRepository,
            IEntityMappingRepository entityMappingRepository,
            IFieldMappingRepository fieldMappingRepository,
            IFieldMappingService fieldMappingService,
            IOutboundConfigurationRepository outboundConfigurationRepository,
            IInboundConfigurationRepository inboundConfigurationRepository,
            IQuickLinkRepository quickLinkRepository,
            IActionDefinitionRepository actionDefinitionRepository,
            ISqlSugarClient sqlSugarClient,
            IMapper mapper,
            UserContext userContext,
            ILogger<IntegrationService> logger)
        {
            _integrationRepository = integrationRepository;
            _entityMappingRepository = entityMappingRepository;
            _fieldMappingRepository = fieldMappingRepository;
            _fieldMappingService = fieldMappingService;
            _outboundConfigurationRepository = outboundConfigurationRepository;
            _inboundConfigurationRepository = inboundConfigurationRepository;
            _quickLinkRepository = quickLinkRepository;
            _actionDefinitionRepository = actionDefinitionRepository;
            _sqlSugarClient = sqlSugarClient;
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

            // Validate name uniqueness only if name has changed (excluding current entity)
            if (entity.Name != input.Name && await _integrationRepository.ExistsNameAsync(input.Name, id))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, $"Integration name '{input.Name}' already exists");
            }

            // Update properties
            entity.Type = input.Type;
            entity.Name = input.Name;
            entity.Description = input.Description;
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

            var dto = _mapper.Map<IntegrationOutputDto>(entity);
            await PopulateConfiguredEntityTypeNamesAsync(dto);
            
            // Decrypt credentials
            if (!string.IsNullOrEmpty(entity.EncryptedCredentials) && entity.EncryptedCredentials != "{}")
            {
                try
                {
                    var decryptedJson = DecryptString(entity.EncryptedCredentials, ENCRYPTION_KEY);
                    _logger.LogDebug($"Decrypted JSON for integration {id}: {decryptedJson}");
                    
                    if (!string.IsNullOrEmpty(decryptedJson) && decryptedJson != "{}")
                    {
                        dto.Credentials = JsonConvert.DeserializeObject<Dictionary<string, string>>(decryptedJson) ?? new Dictionary<string, string>();
                        _logger.LogDebug($"Successfully decrypted credentials for integration {id}, found {dto.Credentials.Count} credential keys: {string.Join(", ", dto.Credentials.Keys)}");
                    }
                    else
                    {
                        _logger.LogDebug($"Decrypted JSON is empty or '{{}}' for integration {id}");
                        dto.Credentials = new Dictionary<string, string>();
                    }
                }
                catch (Exception ex)
                {
                    var preview = entity.EncryptedCredentials != null && entity.EncryptedCredentials.Length > 50 
                        ? entity.EncryptedCredentials.Substring(0, 50) 
                        : entity.EncryptedCredentials ?? "";
                    _logger.LogWarning(ex, $"Failed to decrypt credentials for integration {id}. EncryptedCredentials: {preview}...");
                    dto.Credentials = new Dictionary<string, string>();
                }
            }
            else
            {
                _logger.LogDebug($"Integration {id} has no encrypted credentials (empty or '{{}}')");
                dto.Credentials = new Dictionary<string, string>();
            }
            
            return dto;
        }

        public async Task<IntegrationOutputDto> GetWithDetailsAsync(long id)
        {
            var entity = await _integrationRepository.GetWithDetailsAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
            }

            var dto = _mapper.Map<IntegrationOutputDto>(entity);
            await PopulateConfiguredEntityTypeNamesAsync(dto);
            
            // Decrypt credentials for details view
            if (!string.IsNullOrEmpty(entity.EncryptedCredentials) && entity.EncryptedCredentials != "{}")
            {
                try
                {
                    var decryptedJson = DecryptString(entity.EncryptedCredentials, ENCRYPTION_KEY);
                    _logger.LogDebug($"Decrypted JSON for integration {id}: {decryptedJson}");
                    
                    if (!string.IsNullOrEmpty(decryptedJson) && decryptedJson != "{}")
                    {
                        dto.Credentials = JsonConvert.DeserializeObject<Dictionary<string, string>>(decryptedJson) ?? new Dictionary<string, string>();
                        _logger.LogDebug($"Successfully decrypted credentials for integration {id}, found {dto.Credentials.Count} credential keys: {string.Join(", ", dto.Credentials.Keys)}");
                    }
                    else
                    {
                        _logger.LogDebug($"Decrypted JSON is empty or '{{}}' for integration {id}");
                        dto.Credentials = new Dictionary<string, string>();
                    }
                }
                catch (Exception ex)
                {
                    var preview = entity.EncryptedCredentials != null && entity.EncryptedCredentials.Length > 50 
                        ? entity.EncryptedCredentials.Substring(0, 50) 
                        : entity.EncryptedCredentials ?? "";
                    _logger.LogWarning(ex, $"Failed to decrypt credentials for integration {id}. EncryptedCredentials: {preview}...");
                    dto.Credentials = new Dictionary<string, string>();
                }
            }
            else
            {
                _logger.LogDebug($"Integration {id} has no encrypted credentials (empty or '{{}}')");
                dto.Credentials = new Dictionary<string, string>();
            }

            // Populate connection configuration
            dto.Connection = new ConnectionConfigDto
            {
                Status = entity.Status,
                LastSyncDate = entity.LastSyncDate,
                ErrorMessage = entity.ErrorMessage
            };

            // Populate entity mappings
            if (entity.EntityMappings != null && entity.EntityMappings.Any())
            {
                dto.EntityMappings = _mapper.Map<List<EntityMappingOutputDto>>(entity.EntityMappings);
                foreach (var mappingDto in dto.EntityMappings)
                {
                    var mapping = entity.EntityMappings.FirstOrDefault(em => em.Id == mappingDto.Id);
                    if (mapping != null)
                    {
                        mappingDto.WorkflowIds = JsonConvert.DeserializeObject<List<long>>(mapping.WorkflowIds) ?? new List<long>();
                    }
                }
            }

            // Populate inbound settings (use first config if exists)
            var inboundConfigs = await _inboundConfigurationRepository.GetByIntegrationIdListAsync(id);
            if (inboundConfigs != null && inboundConfigs.Any())
            {
                var firstConfig = inboundConfigs.First();
                dto.InboundSettings = new InboundSettingsDto
                {
                    Id = firstConfig.Id,
                    IntegrationId = firstConfig.IntegrationId,
                    ActionId = firstConfig.ActionId,
                    EntityTypes = JsonConvert.DeserializeObject<List<string>>(firstConfig.EntityTypes) ?? new List<string>(),
                    FieldMappings = JsonConvert.DeserializeObject<List<object>>(firstConfig.FieldMappings) ?? new List<object>(),
                    AttachmentSettings = JsonConvert.DeserializeObject<Dictionary<string, object>>(firstConfig.AttachmentSettings) ?? new Dictionary<string, object>(),
                    AutoSync = firstConfig.AutoSync,
                    SyncInterval = firstConfig.SyncInterval,
                    LastSyncDate = firstConfig.LastSyncDate
                };
            }

            // Populate outbound settings (use first config if exists)
            var outboundConfigs = await _outboundConfigurationRepository.GetByIntegrationIdListAsync(id);
            if (outboundConfigs != null && outboundConfigs.Any())
            {
                var firstConfig = outboundConfigs.First();
                dto.OutboundSettings = new OutboundSettingsDto
                {
                    Id = firstConfig.Id,
                    IntegrationId = firstConfig.IntegrationId,
                    ActionId = firstConfig.ActionId,
                    EntityTypes = JsonConvert.DeserializeObject<List<string>>(firstConfig.EntityTypes) ?? new List<string>(),
                    FieldMappings = JsonConvert.DeserializeObject<List<object>>(firstConfig.FieldMappings) ?? new List<object>(),
                    AttachmentSettings = JsonConvert.DeserializeObject<Dictionary<string, object>>(firstConfig.AttachmentSettings) ?? new Dictionary<string, object>(),
                    SyncMode = firstConfig.SyncMode,
                    WebhookUrl = firstConfig.WebhookUrl
                };
            }

            // Populate quick links
            if (entity.QuickLinks != null && entity.QuickLinks.Any())
            {
                dto.QuickLinks = _mapper.Map<List<QuickLinkOutputDto>>(entity.QuickLinks);
                foreach (var quickLinkDto in dto.QuickLinks)
                {
                    var quickLink = entity.QuickLinks.FirstOrDefault(ql => ql.Id == quickLinkDto.Id);
                    if (quickLink != null)
                    {
                        quickLinkDto.UrlParameters = JsonConvert.DeserializeObject<List<UrlParameterDto>>(quickLink.UrlParameters) ?? new List<UrlParameterDto>();
                    }
                }
            }

            // Populate inbound configurations overview
            dto.InboundConfigurations = await GetInboundOverviewAsync(id);

            // Populate outbound configurations overview
            dto.OutboundConfigurations = await GetOutboundOverviewAsync(id);
            
            return dto;
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
            
            // Populate ConfiguredEntityTypeNames for each integration
            foreach (var dto in dtos)
            {
                await PopulateConfiguredEntityTypeNamesAsync(dto);
            }
            
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

        /// <summary>
        /// Get inbound configuration overview for an integration
        /// </summary>
        public async Task<List<InboundConfigurationOverviewDto>> GetInboundOverviewAsync(long integrationId)
        {
            // Verify integration exists
            var integration = await _integrationRepository.GetByIdAsync(integrationId);
            if (integration == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
            }

            // Get all entity mappings for this integration
            var entityMappings = await _entityMappingRepository.GetByIntegrationIdAsync(integrationId);
            
            // Collect all unique action IDs from entity mappings
            var actionIds = new HashSet<long>();
            foreach (var mapping in entityMappings)
            {
                var workflowIds = JsonConvert.DeserializeObject<List<long>>(mapping.WorkflowIds) ?? new List<long>();
                foreach (var workflowId in workflowIds)
                {
                    actionIds.Add(workflowId);
                }
            }

            // Build overview for each action
            var overview = new List<InboundConfigurationOverviewDto>();
            foreach (var actionId in actionIds)
            {
                var actionName = await GetActionNameAsync(actionId);
                
                // Count entity mappings and field mappings for this action
                var entityMappingCount = 0;
                var fieldMappingCount = 0;
                
                foreach (var mapping in entityMappings)
                {
                    var workflowIds = JsonConvert.DeserializeObject<List<long>>(mapping.WorkflowIds) ?? new List<long>();
                    if (workflowIds.Contains(actionId))
                    {
                        entityMappingCount++;
                        var fieldMappings = await _fieldMappingRepository.GetByEntityMappingIdAsync(mapping.Id);
                        // Count only inbound fields (ViewOnly or Editable)
                        fieldMappingCount += fieldMappings.Count(fm => 
                            fm.SyncDirection == SyncDirection.ViewOnly || 
                            fm.SyncDirection == SyncDirection.Editable);
                    }
                }

                overview.Add(new InboundConfigurationOverviewDto
                {
                    ActionId = actionId,
                    ActionName = actionName,
                    EntityMappingCount = entityMappingCount,
                    FieldMappingCount = fieldMappingCount,
                    HasAttachmentConfig = false, // TODO: Implement when InboundConfiguration is available
                    AutoCreateEntities = true, // TODO: Get from InboundConfiguration
                    Status = entityMappingCount > 0 ? "Configured" : "Not Configured"
                });
            }

            return overview.OrderBy(o => o.ActionId).ToList();
        }

        /// <summary>
        /// Get outbound configuration overview for an integration
        /// </summary>
        public async Task<List<OutboundConfigurationOverviewDto>> GetOutboundOverviewAsync(long integrationId)
        {
            // Verify integration exists
            var integration = await _integrationRepository.GetByIdAsync(integrationId);
            if (integration == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
            }

            // Get all entity mappings for this integration
            var entityMappings = await _entityMappingRepository.GetByIntegrationIdAsync(integrationId);
            
            // Collect all unique action IDs from entity mappings
            var actionIds = new HashSet<long>();
            foreach (var mapping in entityMappings)
            {
                var workflowIds = JsonConvert.DeserializeObject<List<long>>(mapping.WorkflowIds) ?? new List<long>();
                foreach (var workflowId in workflowIds)
                {
                    actionIds.Add(workflowId);
                }
            }

            // Build overview for each action
            var overview = new List<OutboundConfigurationOverviewDto>();
            foreach (var actionId in actionIds)
            {
                var actionName = await GetActionNameAsync(actionId);
                
                // Count fields configured for sharing (OutboundOnly or Editable)
                var fieldCount = 0;
                
                foreach (var mapping in entityMappings)
                {
                    var workflowIds = JsonConvert.DeserializeObject<List<long>>(mapping.WorkflowIds) ?? new List<long>();
                    if (workflowIds.Contains(actionId))
                    {
                        var fieldMappings = await _fieldMappingRepository.GetByEntityMappingIdAsync(mapping.Id);
                        // Count only outbound fields (OutboundOnly or Editable)
                        fieldCount += fieldMappings.Count(fm => 
                            fm.SyncDirection == SyncDirection.OutboundOnly || 
                            fm.SyncDirection == SyncDirection.Editable);
                    }
                }

                overview.Add(new OutboundConfigurationOverviewDto
                {
                    ActionId = actionId,
                    ActionName = actionName,
                    MasterDataEnabled = false, // TODO: Get from OutboundConfiguration
                    FieldCount = fieldCount,
                    AttachmentsEnabled = false, // TODO: Get from OutboundConfiguration
                    RealTimeSyncEnabled = false, // TODO: Get from OutboundConfiguration
                    WebhookUrl = null, // TODO: Get from OutboundConfiguration
                    Status = fieldCount > 0 ? "Configured" : "Not Configured"
                });
            }

            return overview.OrderBy(o => o.ActionId).ToList();
        }

        /// <summary>
        /// Get inbound field mappings by action ID (read-only view)
        /// </summary>
        public async Task<List<InboundFieldMappingDto>> GetInboundFieldMappingsByActionAsync(
            long integrationId,
            long actionId,
            string? externalFieldName = null,
            string? wfeFieldName = null)
        {
            // Verify integration exists
            var integration = await _integrationRepository.GetByIdAsync(integrationId);
            if (integration == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
            }

            var actionName = await GetActionNameAsync(actionId);
            var result = new List<InboundFieldMappingDto>();

            // Get field mappings directly by integration ID and action ID
            var fieldMappings = await _fieldMappingRepository.GetByIntegrationIdAndActionIdAsync(integrationId, actionId);
            
            // Filter for inbound fields (ViewOnly or Editable)
            var inboundFields = fieldMappings.Where(fm => 
                fm.SyncDirection == SyncDirection.ViewOnly || 
                fm.SyncDirection == SyncDirection.Editable);

            foreach (var fieldMapping in inboundFields)
            {
                // Apply filters if provided
                if (!string.IsNullOrEmpty(externalFieldName) && 
                    !fieldMapping.ExternalFieldName.Contains(externalFieldName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(wfeFieldName) && 
                    !fieldMapping.WfeFieldId.Contains(wfeFieldName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Add(new InboundFieldMappingDto
                {
                    ActionId = actionId,
                    ActionName = actionName,
                    ExternalFieldName = fieldMapping.ExternalFieldName,
                    WfeFieldId = fieldMapping.WfeFieldId,
                    WfeFieldName = fieldMapping.WfeFieldId // Use WfeFieldId as display name for now
                });
            }

            return result.OrderBy(r => r.ExternalFieldName).ToList();
        }

        /// <summary>
        /// Get outbound shared fields by action ID (read-only view)
        /// Uses OutboundConfiguration and OutboundFieldConfig tables which have actionId
        /// </summary>
        public async Task<List<OutboundSharedFieldDto>> GetOutboundSharedFieldsByActionAsync(
            long integrationId,
            long actionId,
            string? fieldName = null)
        {
            // Verify integration exists
            var integration = await _integrationRepository.GetByIdAsync(integrationId);
            if (integration == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
            }

            var actionName = await GetActionNameAsync(actionId);
            var result = new List<OutboundSharedFieldDto>();

            // Get outbound field configs directly by integration ID and action ID
            // First get outbound configurations for this integration and action
            var outboundConfigs = await _outboundConfigurationRepository.GetByIntegrationIdAndActionIdAsync(integrationId, actionId);
            var configIds = outboundConfigs.Select(c => c.Id).ToList();

            if (configIds.Any())
            {
                // Get outbound field configs for these configurations, filtered by action_id
                var fieldConfigs = await _sqlSugarClient.Queryable<Domain.Entities.Integration.OutboundFieldConfig>()
                    .Where(x => configIds.Contains(x.OutboundConfigurationId) 
                        && x.ActionId == actionId 
                        && x.IsValid)
                    .OrderBy(x => x.SortOrder)
                    .ToListAsync();

                foreach (var fieldConfig in fieldConfigs)
                {
                    // Apply filter if provided
                    if (!string.IsNullOrEmpty(fieldName) && 
                        !fieldConfig.WfeFieldId.Contains(fieldName, StringComparison.OrdinalIgnoreCase) &&
                        !fieldConfig.ExternalFieldName.Contains(fieldName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    result.Add(new OutboundSharedFieldDto
                    {
                        ActionId = actionId,
                        ActionName = actionName,
                        FieldDisplayName = fieldConfig.WfeFieldId, // Use WfeFieldId as display name
                        FieldApiName = fieldConfig.WfeFieldId
                    });
                }
            }

            return result.OrderBy(r => r.FieldApiName).ToList();
        }

        /// <summary>
        /// Populate ConfiguredEntityTypeNames and ConfiguredEntityTypes for IntegrationOutputDto
        /// </summary>
        private async Task PopulateConfiguredEntityTypeNamesAsync(IntegrationOutputDto dto)
        {
            try
            {
                var entityMappings = await _entityMappingRepository.GetByIntegrationIdAsync(dto.Id);
                dto.ConfiguredEntityTypeNames = entityMappings
                    .Where(em => em.IsActive && em.IsValid)
                    .Select(em => em.WfeEntityType)
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList();
                
                // Update ConfiguredEntityTypes to match the count of ConfiguredEntityTypeNames
                dto.ConfiguredEntityTypes = dto.ConfiguredEntityTypeNames.Count;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to populate ConfiguredEntityTypeNames for integration {dto.Id}");
                dto.ConfiguredEntityTypeNames = new List<string>();
                dto.ConfiguredEntityTypes = 0;
            }
        }

        /// <summary>
        /// Get action name by action ID
        /// </summary>
        private async Task<string> GetActionNameAsync(long actionId)
        {
            try
            {
                // Try to get action definition by ID
                var actionDefinition = await _actionDefinitionRepository.GetByIdAsync(actionId);
                if (actionDefinition != null && !string.IsNullOrEmpty(actionDefinition.ActionName))
                {
                    return actionDefinition.ActionName;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get action name for action ID {ActionId}", actionId);
            }

            // Fallback: return formatted action ID
            return $"Action {actionId}";
        }
    }
}
