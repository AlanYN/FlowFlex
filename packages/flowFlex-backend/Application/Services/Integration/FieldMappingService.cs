using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Contracts.IServices.Integration;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Services.Integration
{
    /// <summary>
    /// Inbound field mapping service implementation
    /// </summary>
    public class InboundFieldMappingService : IInboundFieldMappingService, IScopedService
    {
        private readonly IInboundFieldMappingRepository _fieldMappingRepository;
        private readonly IMapper _mapper;
        private readonly UserContext _userContext;
        private readonly ILogger<InboundFieldMappingService> _logger;

        public InboundFieldMappingService(
            IInboundFieldMappingRepository fieldMappingRepository,
            IMapper mapper,
            UserContext userContext,
            ILogger<InboundFieldMappingService> logger)
        {
            _fieldMappingRepository = fieldMappingRepository;
            _mapper = mapper;
            _userContext = userContext;
            _logger = logger;
        }

        public async Task<long> CreateAsync(InboundFieldMappingInputDto input)
        {
            if (input == null)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input cannot be null");
            }

            if (input.ActionId <= 0)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "ActionId is required");
            }

            // Validate field mapping uniqueness
            if (await _fieldMappingRepository.ExistsAsync(input.ActionId, input.ExternalFieldName))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"Field mapping for '{input.ExternalFieldName}' already exists");
            }

            var entity = new InboundFieldMapping
            {
                ActionId = input.ActionId,
                ExternalFieldName = input.ExternalFieldName,
                WfeFieldId = input.WfeFieldId,
                FieldType = input.FieldType,
                SyncDirection = input.SyncDirection,
                SortOrder = input.SortOrder,
                IsRequired = input.IsRequired,
                DefaultValue = input.DefaultValue
            };
            entity.InitCreateInfo(_userContext);

            var id = await _fieldMappingRepository.InsertReturnSnowflakeIdAsync(entity);

            _logger.LogInformation($"Created inbound field mapping: {input.ExternalFieldName} (ID: {id})");

            return id;
        }

        public async Task<bool> UpdateAsync(long id, InboundFieldMappingInputDto input)
        {
            if (input == null)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input cannot be null");
            }

            var entity = await _fieldMappingRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Field mapping not found");
            }

            // Validate field mapping uniqueness (excluding current entity)
            if (await _fieldMappingRepository.ExistsAsync(entity.ActionId, input.ExternalFieldName, id))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"Field mapping for '{input.ExternalFieldName}' already exists");
            }

            entity.ExternalFieldName = input.ExternalFieldName;
            entity.WfeFieldId = input.WfeFieldId;
            entity.FieldType = input.FieldType;
            entity.SyncDirection = input.SyncDirection;
            entity.SortOrder = input.SortOrder;
            entity.IsRequired = input.IsRequired;
            entity.DefaultValue = input.DefaultValue;
            entity.InitModifyInfo(_userContext);

            var result = await _fieldMappingRepository.UpdateAsync(entity);

            _logger.LogInformation($"Updated inbound field mapping: {input.ExternalFieldName} (ID: {id})");

            return result;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _fieldMappingRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Field mapping not found");
            }

            // Soft delete
            entity.IsValid = false;
            entity.InitModifyInfo(_userContext);

            var result = await _fieldMappingRepository.UpdateAsync(entity);

            _logger.LogInformation($"Deleted inbound field mapping: {entity.ExternalFieldName} (ID: {id})");

            return result;
        }

        public async Task<InboundFieldMappingOutputDto> GetByIdAsync(long id)
        {
            var entity = await _fieldMappingRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Field mapping not found");
            }

            return MapToOutputDto(entity);
        }

        public async Task<List<InboundFieldMappingOutputDto>> GetByActionIdAsync(long actionId)
        {
            var entities = await _fieldMappingRepository.GetByActionIdAsync(actionId);
            return entities.Select(MapToOutputDto).OrderBy(d => d.SortOrder).ToList();
        }

        public async Task<bool> BatchCreateAsync(long actionId, List<InboundFieldMappingInputDto> inputs)
        {
            if (inputs == null || !inputs.Any())
            {
                return true; // No mappings to create
            }

            try
            {
                var sortOrder = 0;
                foreach (var input in inputs)
                {
                    var entity = new InboundFieldMapping
                    {
                        ActionId = actionId,
                        ExternalFieldName = input.ExternalFieldName,
                        WfeFieldId = input.WfeFieldId,
                        FieldType = input.FieldType,
                        SyncDirection = input.SyncDirection,
                        SortOrder = input.SortOrder > 0 ? input.SortOrder : sortOrder++,
                        IsRequired = input.IsRequired,
                        DefaultValue = input.DefaultValue
                    };
                    entity.InitCreateInfo(_userContext);
                    await _fieldMappingRepository.InsertReturnSnowflakeIdAsync(entity);
                }

                _logger.LogInformation($"Batch created {inputs.Count} inbound field mappings for Action {actionId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during batch create of inbound field mappings");
                throw new CRMException(ErrorCodeEnum.SystemError, $"Batch create failed: {ex.Message}");
            }
        }

        public async Task<bool> DeleteByActionIdAsync(long actionId)
        {
            return await _fieldMappingRepository.DeleteByActionIdAsync(actionId);
        }

        private static InboundFieldMappingOutputDto MapToOutputDto(InboundFieldMapping entity)
        {
            return new InboundFieldMappingOutputDto
            {
                Id = entity.Id,
                ActionId = entity.ActionId,
                ExternalFieldName = entity.ExternalFieldName,
                WfeFieldId = entity.WfeFieldId,
                WfeFieldName = entity.WfeFieldId, // Use WfeFieldId as display name
                FieldType = entity.FieldType.ToString(),
                SyncDirection = entity.SyncDirection.ToString(),
                IsRequired = entity.IsRequired,
                DefaultValue = entity.DefaultValue,
                SortOrder = entity.SortOrder
            };
        }
    }
}
