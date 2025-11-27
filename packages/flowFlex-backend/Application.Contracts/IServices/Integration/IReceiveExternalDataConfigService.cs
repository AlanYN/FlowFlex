using FlowFlex.Application.Contracts.Dtos.Integration;

namespace FlowFlex.Application.Contracts.IServices.Integration;

/// <summary>
/// Service interface for Receive External Data Configuration
/// </summary>
public interface IReceiveExternalDataConfigService
{
    /// <summary>
    /// Get all configurations for an integration
    /// </summary>
    Task<List<ReceiveExternalDataConfigOutputDto>> GetByIntegrationIdAsync(long integrationId);

    /// <summary>
    /// Get configuration by ID
    /// </summary>
    Task<ReceiveExternalDataConfigOutputDto> GetByIdAsync(long id);

    /// <summary>
    /// Create a new configuration
    /// </summary>
    Task<long> CreateAsync(long integrationId, ReceiveExternalDataConfigInputDto input);

    /// <summary>
    /// Delete a configuration
    /// </summary>
    Task<bool> DeleteAsync(long id);

    /// <summary>
    /// Get field mappings for a configuration
    /// </summary>
    Task<List<FieldMappingOutputDto>> GetFieldMappingsAsync(long configId);

    /// <summary>
    /// Update field mappings for a configuration
    /// </summary>
    Task<bool> UpdateFieldMappingsAsync(long configId, List<FieldMappingInputDto> fieldMappings);

    /// <summary>
    /// Get active configurations for an integration
    /// </summary>
    Task<List<ReceiveExternalDataConfigOutputDto>> GetActiveConfigsAsync(long integrationId);
}

