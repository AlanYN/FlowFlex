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
using SqlSugar;

namespace FlowFlex.Application.Services.Integration
{
    /// <summary>
    /// Entity mapping service implementation
    /// </summary>
    public class EntityMappingService : IEntityMappingService, IScopedService
    {
        private readonly IEntityMappingRepository _entityMappingRepository;
        private readonly IIntegrationRepository _integrationRepository;
        private readonly IMapper _mapper;
        private readonly UserContext _userContext;
        private readonly ILogger<EntityMappingService> _logger;

        public EntityMappingService(
            IEntityMappingRepository entityMappingRepository,
            IIntegrationRepository integrationRepository,
            IMapper mapper,
            UserContext userContext,
            ILogger<EntityMappingService> logger)
        {
            _entityMappingRepository = entityMappingRepository;
            _integrationRepository = integrationRepository;
            _mapper = mapper;
            _userContext = userContext;
            _logger = logger;
        }

        public async Task<long> CreateAsync(EntityMappingInputDto input)
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

            // Validate entity mapping uniqueness
            if (await _entityMappingRepository.ExistsAsync(input.IntegrationId, input.ExternalEntityType))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"Entity mapping for '{input.ExternalEntityType}' already exists");
            }

            var entity = _mapper.Map<EntityMapping>(input);
            entity.WorkflowIds = JsonConvert.SerializeObject(input.WorkflowIds);
            // Generate SystemId automatically using Snowflake ID
            entity.SystemId = SnowFlakeSingle.Instance.NextId().ToString();
            entity.InitCreateInfo(_userContext);

            var id = await _entityMappingRepository.InsertReturnSnowflakeIdAsync(entity);

            // Update integration's configured entity types count
            integration.ConfiguredEntityTypes = await _entityMappingRepository.CountAsync(e => e.IntegrationId == input.IntegrationId);
            integration.InitModifyInfo(_userContext);
            await _integrationRepository.UpdateAsync(integration);

            _logger.LogInformation($"Created entity mapping: {input.ExternalEntityName} (ID: {id})");

            return id;
        }

        public async Task<bool> UpdateAsync(long id, EntityMappingInputDto input)
        {
            if (input == null)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input cannot be null");
            }

            var entity = await _entityMappingRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Entity mapping not found");
            }

            // Validate entity mapping uniqueness (excluding current entity)
            if (await _entityMappingRepository.ExistsAsync(input.IntegrationId, input.ExternalEntityType, id))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"Entity mapping for '{input.ExternalEntityType}' already exists");
            }

            entity.ExternalEntityName = input.ExternalEntityName;
            entity.ExternalEntityType = input.ExternalEntityType;
            entity.WfeEntityType = input.WfeEntityType;
            entity.WorkflowIds = JsonConvert.SerializeObject(input.WorkflowIds);
            entity.IsActive = input.IsActive;
            // SystemId is generated on create, not updated
            entity.InitModifyInfo(_userContext);

            var result = await _entityMappingRepository.UpdateAsync(entity);

            _logger.LogInformation($"Updated entity mapping: {input.ExternalEntityName} (ID: {id})");

            return result;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _entityMappingRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Entity mapping not found");
            }

            // Soft delete
            entity.IsValid = false;
            entity.InitModifyInfo(_userContext);

            var result = await _entityMappingRepository.UpdateAsync(entity);

            // Update integration's configured entity types count
            var integration = await _integrationRepository.GetByIdAsync(entity.IntegrationId);
            if (integration != null)
            {
                integration.ConfiguredEntityTypes = await _entityMappingRepository.CountAsync(e =>
                    e.IntegrationId == entity.IntegrationId && e.IsValid);
                integration.InitModifyInfo(_userContext);
                await _integrationRepository.UpdateAsync(integration);
            }

            _logger.LogInformation($"Deleted entity mapping: {entity.ExternalEntityName} (ID: {id})");

            return result;
        }

        public async Task<EntityMappingOutputDto> GetByIdAsync(long id)
        {
            var entity = await _entityMappingRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Entity mapping not found");
            }

            var dto = _mapper.Map<EntityMappingOutputDto>(entity);
            dto.WorkflowIds = JsonConvert.DeserializeObject<List<long>>(entity.WorkflowIds) ?? new List<long>();

            return dto;
        }

        public async Task<List<EntityMappingOutputDto>> GetByIntegrationIdAsync(long integrationId)
        {
            var entities = await _entityMappingRepository.GetByIntegrationIdAsync(integrationId);
            var dtos = _mapper.Map<List<EntityMappingOutputDto>>(entities);

            foreach (var dto in dtos)
            {
                var entity = entities.FirstOrDefault(e => e.Id == dto.Id);
                if (entity != null)
                {
                    dto.WorkflowIds = JsonConvert.DeserializeObject<List<long>>(entity.WorkflowIds) ?? new List<long>();
                }
            }

            return dtos;
        }

        public async Task<(List<EntityMappingOutputDto> items, int total)> GetPagedListAsync(
            long integrationId,
            int pageIndex,
            int pageSize)
        {
            var (items, total) = await _entityMappingRepository.GetPageListAsync(
                e => e.IntegrationId == integrationId && e.IsValid,
                pageIndex,
                pageSize,
                e => e.CreateDate,
                false);

            var dtos = _mapper.Map<List<EntityMappingOutputDto>>(items);

            foreach (var dto in dtos)
            {
                var entity = items.FirstOrDefault(e => e.Id == dto.Id);
                if (entity != null)
                {
                    dto.WorkflowIds = JsonConvert.DeserializeObject<List<long>>(entity.WorkflowIds) ?? new List<long>();
                }
            }

            return (dtos, total);
        }
    }
}

