using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Contracts.IServices.Integration;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FlowFlex.Application.Services.Integration
{
    /// <summary>
    /// Attachment sharing service implementation
    /// </summary>
    public class AttachmentSharingService : IAttachmentSharingService, IScopedService
    {
        private readonly IAttachmentSharingRepository _repository;
        private readonly IIntegrationRepository _integrationRepository;
        private readonly IMapper _mapper;
        private readonly UserContext _userContext;
        private readonly ILogger<AttachmentSharingService> _logger;

        public AttachmentSharingService(
            IAttachmentSharingRepository repository,
            IIntegrationRepository integrationRepository,
            IMapper mapper,
            UserContext userContext,
            ILogger<AttachmentSharingService> logger)
        {
            _repository = repository;
            _integrationRepository = integrationRepository;
            _mapper = mapper;
            _userContext = userContext;
            _logger = logger;
        }

        /// <summary>
        /// Create attachment sharing configuration
        /// </summary>
        public async Task<long> CreateAsync(AttachmentSharingInputDto input)
        {
            if (input == null)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input cannot be null");
            }

            // Validate integration exists
            var integration = await _integrationRepository.GetByIdAsync(input.IntegrationId);
            if (integration == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
            }

            // Check if module name already exists
            if (await _repository.ExistsModuleNameAsync(input.IntegrationId, input.ExternalModuleName))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, 
                    $"Attachment sharing for module '{input.ExternalModuleName}' already exists");
            }

            // Create entity
            var entity = new AttachmentSharing
            {
                IntegrationId = input.IntegrationId,
                ExternalModuleName = input.ExternalModuleName,
                SystemId = GenerateSystemId(input.ExternalModuleName),
                WorkflowIds = JsonConvert.SerializeObject(input.WorkflowIds ?? new List<long>()),
                IsActive = input.IsActive,
                Description = input.Description,
                AllowedFileTypes = input.AllowedFileTypes != null 
                    ? JsonConvert.SerializeObject(input.AllowedFileTypes) 
                    : null,
                MaxFileSizeMB = input.MaxFileSizeMB
            };

            entity.InitCreateInfo(_userContext);

            var id = await _repository.InsertReturnSnowflakeIdAsync(entity);

            _logger.LogInformation("Created attachment sharing: {ModuleName} (ID: {Id}, SystemId: {SystemId})", 
                input.ExternalModuleName, id, entity.SystemId);

            return id;
        }

        /// <summary>
        /// Update attachment sharing configuration
        /// </summary>
        public async Task<bool> UpdateAsync(long id, AttachmentSharingInputDto input)
        {
            if (input == null)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input cannot be null");
            }

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Attachment sharing configuration not found");
            }

            // Check if module name already exists (excluding current)
            if (entity.ExternalModuleName != input.ExternalModuleName)
            {
                if (await _repository.ExistsModuleNameAsync(input.IntegrationId, input.ExternalModuleName, id))
                {
                    throw new CRMException(ErrorCodeEnum.BusinessError, 
                        $"Attachment sharing for module '{input.ExternalModuleName}' already exists");
                }
                
                // Regenerate system ID if module name changed
                entity.SystemId = GenerateSystemId(input.ExternalModuleName);
            }

            entity.ExternalModuleName = input.ExternalModuleName;
            entity.WorkflowIds = JsonConvert.SerializeObject(input.WorkflowIds ?? new List<long>());
            entity.IsActive = input.IsActive;
            entity.Description = input.Description;
            entity.AllowedFileTypes = input.AllowedFileTypes != null 
                ? JsonConvert.SerializeObject(input.AllowedFileTypes) 
                : null;
            entity.MaxFileSizeMB = input.MaxFileSizeMB;

            entity.InitModifyInfo(_userContext);

            var result = await _repository.UpdateAsync(entity);

            _logger.LogInformation("Updated attachment sharing: {ModuleName} (ID: {Id})", 
                input.ExternalModuleName, id);

            return result;
        }

        /// <summary>
        /// Delete attachment sharing configuration (soft delete)
        /// </summary>
        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Attachment sharing configuration not found");
            }

            entity.IsValid = false;
            entity.InitModifyInfo(_userContext);

            var result = await _repository.UpdateAsync(entity);

            _logger.LogInformation("Deleted attachment sharing: {ModuleName} (ID: {Id})", 
                entity.ExternalModuleName, id);

            return result;
        }

        /// <summary>
        /// Get attachment sharing by ID
        /// </summary>
        public async Task<AttachmentSharingOutputDto?> GetByIdAsync(long id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                return null;
            }

            return MapToOutputDto(entity);
        }

        /// <summary>
        /// Get attachment sharing by system ID
        /// </summary>
        public async Task<AttachmentSharingOutputDto?> GetBySystemIdAsync(string systemId)
        {
            var entity = await _repository.GetBySystemIdAsync(systemId);
            if (entity == null)
            {
                return null;
            }

            return MapToOutputDto(entity);
        }

        /// <summary>
        /// Get all attachment sharing configurations for an integration
        /// </summary>
        public async Task<List<AttachmentSharingOutputDto>> GetByIntegrationIdAsync(long integrationId)
        {
            var entities = await _repository.GetByIntegrationIdAsync(integrationId);
            return entities.Select(MapToOutputDto).ToList();
        }

        /// <summary>
        /// Get active attachment sharing configurations by workflow ID
        /// </summary>
        public async Task<List<AttachmentSharingOutputDto>> GetByWorkflowIdAsync(long workflowId)
        {
            var entities = await _repository.GetByWorkflowIdAsync(workflowId);
            return entities.Select(MapToOutputDto).ToList();
        }

        /// <summary>
        /// Generate system ID from module name
        /// </summary>
        private string GenerateSystemId(string moduleName)
        {
            // Convert to lowercase, replace spaces with underscores, remove special characters
            var baseId = moduleName
                .ToLowerInvariant()
                .Replace(" ", "_")
                .Replace("-", "_");
            
            // Remove any characters that are not alphanumeric or underscore
            baseId = new string(baseId.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
            
            // Add timestamp suffix to ensure uniqueness
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 100000;
            
            return $"{baseId}_{timestamp}";
        }

        /// <summary>
        /// Map entity to output DTO
        /// </summary>
        private AttachmentSharingOutputDto MapToOutputDto(AttachmentSharing entity)
        {
            return new AttachmentSharingOutputDto
            {
                Id = entity.Id,
                IntegrationId = entity.IntegrationId,
                ExternalModuleName = entity.ExternalModuleName,
                SystemId = entity.SystemId,
                WorkflowIds = !string.IsNullOrEmpty(entity.WorkflowIds) 
                    ? JsonConvert.DeserializeObject<List<long>>(entity.WorkflowIds) ?? new List<long>()
                    : new List<long>(),
                IsActive = entity.IsActive,
                Description = entity.Description,
                AllowedFileTypes = !string.IsNullOrEmpty(entity.AllowedFileTypes)
                    ? JsonConvert.DeserializeObject<List<string>>(entity.AllowedFileTypes)
                    : null,
                MaxFileSizeMB = entity.MaxFileSizeMB,
                CreateDate = entity.CreateDate,
                ModifyDate = entity.ModifyDate,
                CreateBy = entity.CreateBy,
                ModifyBy = entity.ModifyBy
            };
        }
    }
}

