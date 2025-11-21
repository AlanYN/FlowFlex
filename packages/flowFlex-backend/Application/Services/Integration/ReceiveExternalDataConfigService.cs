using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Contracts.IServices.Integration;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FlowFlex.Application.Services.Integration;

/// <summary>
/// Service implementation for Receive External Data Configuration
/// </summary>
public class ReceiveExternalDataConfigService : IReceiveExternalDataConfigService
{
    private readonly IReceiveExternalDataConfigRepository _repository;
    private readonly IIntegrationRepository _integrationRepository;
    private readonly IMapper _mapper;
    private readonly UserContext _userContext;
    private readonly ILogger<ReceiveExternalDataConfigService> _logger;

    public ReceiveExternalDataConfigService(
        IReceiveExternalDataConfigRepository repository,
        IIntegrationRepository integrationRepository,
        IMapper mapper,
        UserContext userContext,
        ILogger<ReceiveExternalDataConfigService> logger)
    {
        _repository = repository;
        _integrationRepository = integrationRepository;
        _mapper = mapper;
        _userContext = userContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all configurations for an integration
    /// </summary>
    public async Task<List<ReceiveExternalDataConfigOutputDto>> GetByIntegrationIdAsync(long integrationId)
    {
        var configs = await _repository.GetByIntegrationIdAsync(integrationId);
        var dtos = _mapper.Map<List<ReceiveExternalDataConfigOutputDto>>(configs);

        // Deserialize field mappings
        foreach (var dto in dtos)
        {
            var config = configs.FirstOrDefault(c => c.Id == dto.Id);
            if (config != null && !string.IsNullOrEmpty(config.FieldMappingConfig))
            {
                dto.FieldMappings = JsonConvert.DeserializeObject<List<FieldMappingOutputDto>>(config.FieldMappingConfig) 
                    ?? new List<FieldMappingOutputDto>();
            }
        }

        return dtos;
    }

    /// <summary>
    /// Get configuration by ID
    /// </summary>
    public async Task<ReceiveExternalDataConfigOutputDto> GetByIdAsync(long id)
    {
        var config = await _repository.GetByIdAsync(id);
        if (config == null)
        {
            throw new CRMException(ErrorCodeEnum.NotFound, "Configuration not found");
        }

        var dto = _mapper.Map<ReceiveExternalDataConfigOutputDto>(config);

        // Deserialize field mappings
        if (!string.IsNullOrEmpty(config.FieldMappingConfig))
        {
            dto.FieldMappings = JsonConvert.DeserializeObject<List<FieldMappingOutputDto>>(config.FieldMappingConfig) 
                ?? new List<FieldMappingOutputDto>();
        }

        return dto;
    }

    /// <summary>
    /// Create a new configuration
    /// </summary>
    public async Task<long> CreateAsync(long integrationId, ReceiveExternalDataConfigInputDto input)
    {
        // Verify integration exists
        var integration = await _integrationRepository.GetByIdAsync(integrationId);
        if (integration == null)
        {
            throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
        }

        // Check if entity name already exists
        var exists = await _repository.ExistsEntityNameAsync(integrationId, input.EntityName);
        if (exists)
        {
            throw new CRMException(ErrorCodeEnum.BusinessError, $"Entity name '{input.EntityName}' already exists");
        }

        var entity = _mapper.Map<ReceiveExternalDataConfig>(input);
        entity.IntegrationId = integrationId;
        entity.FieldMappingConfig = JsonConvert.SerializeObject(input.FieldMappings);
        entity.InitCreateInfo(_userContext);

        var id = await _repository.InsertReturnSnowflakeIdAsync(entity);

        _logger.LogInformation("Created ReceiveExternalDataConfig {ConfigId} for Integration {IntegrationId}", 
            id, integrationId);

        return id;
    }

    /// <summary>
    /// Delete a configuration
    /// </summary>
    public async Task<bool> DeleteAsync(long id)
    {
        var config = await _repository.GetByIdAsync(id);
        if (config == null)
        {
            throw new CRMException(ErrorCodeEnum.NotFound, "Configuration not found");
        }

        // Soft delete
        config.IsValid = false;
        config.InitUpdateInfo(_userContext);

        var result = await _repository.UpdateAsync(config);

        _logger.LogInformation("Deleted ReceiveExternalDataConfig {ConfigId}", id);

        return result;
    }

    /// <summary>
    /// Get field mappings for a configuration
    /// </summary>
    public async Task<List<FieldMappingOutputDto>> GetFieldMappingsAsync(long configId)
    {
        var config = await _repository.GetByIdAsync(configId);
        if (config == null)
        {
            throw new CRMException(ErrorCodeEnum.NotFound, "Configuration not found");
        }

        if (string.IsNullOrEmpty(config.FieldMappingConfig))
        {
            return new List<FieldMappingOutputDto>();
        }

        return JsonConvert.DeserializeObject<List<FieldMappingOutputDto>>(config.FieldMappingConfig) 
            ?? new List<FieldMappingOutputDto>();
    }

    /// <summary>
    /// Update field mappings for a configuration
    /// </summary>
    public async Task<bool> UpdateFieldMappingsAsync(long configId, List<FieldMappingInputDto> fieldMappings)
    {
        var config = await _repository.GetByIdAsync(configId);
        if (config == null)
        {
            throw new CRMException(ErrorCodeEnum.NotFound, "Configuration not found");
        }

        config.FieldMappingConfig = JsonConvert.SerializeObject(fieldMappings);
        config.InitUpdateInfo(_userContext);

        var result = await _repository.UpdateAsync(config);

        _logger.LogInformation("Updated field mappings for ReceiveExternalDataConfig {ConfigId}", configId);

        return result;
    }

    /// <summary>
    /// Get active configurations for an integration
    /// </summary>
    public async Task<List<ReceiveExternalDataConfigOutputDto>> GetActiveConfigsAsync(long integrationId)
    {
        var configs = await _repository.GetActiveConfigsAsync(integrationId);
        var dtos = _mapper.Map<List<ReceiveExternalDataConfigOutputDto>>(configs);

        // Deserialize field mappings
        foreach (var dto in dtos)
        {
            var config = configs.FirstOrDefault(c => c.Id == dto.Id);
            if (config != null && !string.IsNullOrEmpty(config.FieldMappingConfig))
            {
                dto.FieldMappings = JsonConvert.DeserializeObject<List<FieldMappingOutputDto>>(config.FieldMappingConfig) 
                    ?? new List<FieldMappingOutputDto>();
            }
        }

        return dtos;
    }
}

