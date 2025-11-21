using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Contracts.IServices;
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
    /// Field mapping service implementation
    /// </summary>
    public class FieldMappingService : IFieldMappingService, IScopedService
    {
        private readonly IFieldMappingRepository _fieldMappingRepository;
        private readonly IEntityMappingRepository _entityMappingRepository;
        private readonly IIntegrationRepository _integrationRepository;
        private readonly IMapper _mapper;
        private readonly UserContext _userContext;
        private readonly ILogger<FieldMappingService> _logger;

        public FieldMappingService(
            IFieldMappingRepository fieldMappingRepository,
            IEntityMappingRepository entityMappingRepository,
            IIntegrationRepository integrationRepository,
            IMapper mapper,
            UserContext userContext,
            ILogger<FieldMappingService> logger)
        {
            _fieldMappingRepository = fieldMappingRepository;
            _entityMappingRepository = entityMappingRepository;
            _integrationRepository = integrationRepository;
            _mapper = mapper;
            _userContext = userContext;
            _logger = logger;
        }

        public async Task<long> CreateAsync(FieldMappingInputDto input)
        {
            if (input == null)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input cannot be null");
            }

            // Validate entity mapping exists
            var entityMapping = await _entityMappingRepository.GetByIdAsync(input.EntityMappingId);
            if (entityMapping == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Entity mapping not found");
            }

            // Validate field mapping uniqueness
            if (await _fieldMappingRepository.ExistsAsync(input.EntityMappingId, input.ExternalFieldName))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, 
                    $"Field mapping for '{input.ExternalFieldName}' already exists");
            }

            var entity = _mapper.Map<FieldMapping>(input);
            entity.TransformRules = JsonConvert.SerializeObject(input.TransformRules);
            entity.InitCreateInfo(_userContext);

            var id = await _fieldMappingRepository.InsertReturnSnowflakeIdAsync(entity);

            _logger.LogInformation($"Created field mapping: {input.ExternalFieldName} (ID: {id})");

            return id;
        }

        public async Task<bool> UpdateAsync(long id, FieldMappingInputDto input)
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
            if (await _fieldMappingRepository.ExistsAsync(input.EntityMappingId, input.ExternalFieldName, id))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, 
                    $"Field mapping for '{input.ExternalFieldName}' already exists");
            }

            entity.ExternalFieldName = input.ExternalFieldName;
            entity.WfeFieldId = input.WfeFieldId;
            entity.FieldType = input.FieldType;
            entity.SyncDirection = input.SyncDirection;
            entity.TransformRules = JsonConvert.SerializeObject(input.TransformRules);
            entity.SortOrder = input.SortOrder;
            entity.IsRequired = input.IsRequired;
            entity.DefaultValue = input.DefaultValue;
            entity.InitModifyInfo(_userContext);

            var result = await _fieldMappingRepository.UpdateAsync(entity);

            _logger.LogInformation($"Updated field mapping: {input.ExternalFieldName} (ID: {id})");

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

            _logger.LogInformation($"Deleted field mapping: {entity.ExternalFieldName} (ID: {id})");

            return result;
        }

        public async Task<FieldMappingOutputDto> GetByIdAsync(long id)
        {
            var entity = await _fieldMappingRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Field mapping not found");
            }

            var dto = _mapper.Map<FieldMappingOutputDto>(entity);
            dto.TransformRules = JsonConvert.DeserializeObject<Dictionary<string, object>>(entity.TransformRules) 
                ?? new Dictionary<string, object>();

            return dto;
        }

        public async Task<List<FieldMappingOutputDto>> GetByEntityMappingIdAsync(long entityMappingId)
        {
            var entities = await _fieldMappingRepository.GetByEntityMappingIdAsync(entityMappingId);
            var dtos = _mapper.Map<List<FieldMappingOutputDto>>(entities);

            foreach (var dto in dtos)
            {
                var entity = entities.FirstOrDefault(e => e.Id == dto.Id);
                if (entity != null)
                {
                    dto.TransformRules = JsonConvert.DeserializeObject<Dictionary<string, object>>(entity.TransformRules) 
                        ?? new Dictionary<string, object>();
                }
            }

            return dtos.OrderBy(d => d.SortOrder).ToList();
        }

        public async Task<List<FieldMappingOutputDto>> GetByIntegrationIdAsync(long integrationId)
        {
            var entities = await _fieldMappingRepository.GetByIntegrationIdAsync(integrationId);
            var dtos = _mapper.Map<List<FieldMappingOutputDto>>(entities);

            foreach (var dto in dtos)
            {
                var entity = entities.FirstOrDefault(e => e.Id == dto.Id);
                if (entity != null)
                {
                    dto.TransformRules = JsonConvert.DeserializeObject<Dictionary<string, object>>(entity.TransformRules) 
                        ?? new Dictionary<string, object>();
                }
            }

            return dtos.OrderBy(d => d.SortOrder).ToList();
        }

        public async Task<bool> BatchUpdateAsync(List<FieldMappingInputDto> inputs)
        {
            if (inputs == null || !inputs.Any())
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input list cannot be null or empty");
            }

            try
            {
                foreach (var input in inputs)
                {
                    var entity = _mapper.Map<FieldMapping>(input);
                    entity.TransformRules = JsonConvert.SerializeObject(input.TransformRules);

                    if (entity.Id > 0)
                    {
                        // Update existing
                        entity.InitModifyInfo(_userContext);
                        await _fieldMappingRepository.UpdateAsync(entity);
                    }
                    else
                    {
                        // Create new
                        entity.InitCreateInfo(_userContext);
                        await _fieldMappingRepository.InsertReturnSnowflakeIdAsync(entity);
                    }
                }

                _logger.LogInformation($"Batch updated {inputs.Count} field mappings");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during batch update of field mappings");
                throw new CRMException(ErrorCodeEnum.SystemError, $"Batch update failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Get bidirectional (editable) field mappings for an entity mapping
        /// </summary>
        public async Task<List<FieldMappingOutputDto>> GetBidirectionalMappingsAsync(long entityMappingId)
        {
            var entities = await _fieldMappingRepository.GetBidirectionalMappingsAsync(entityMappingId);
            var dtos = _mapper.Map<List<FieldMappingOutputDto>>(entities);

            foreach (var dto in dtos)
            {
                var entity = entities.FirstOrDefault(e => e.Id == dto.Id);
                if (entity != null)
                {
                    dto.TransformRules = JsonConvert.DeserializeObject<Dictionary<string, object>>(entity.TransformRules) 
                        ?? new Dictionary<string, object>();
                }
            }

            return dtos;
        }
    }
}

