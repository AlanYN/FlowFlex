using FlowFlex.Application.Contracts.Dtos.Integration;

namespace FlowFlex.Application.Contracts.IServices;

/// <summary>
/// Field mapping engine interface
/// </summary>
public interface IFieldMappingEngine
{
    /// <summary>
    /// Map inbound fields from external system to WFE
    /// </summary>
    Task<Dictionary<string, object>> MapInboundFieldsAsync(long integrationId, long entityMappingId, Dictionary<string, object> externalData);

    /// <summary>
    /// Map outbound fields from WFE to external system
    /// </summary>
    Task<Dictionary<string, object>> MapOutboundFieldsAsync(long integrationId, string entityType, Dictionary<string, object> internalData);

    /// <summary>
    /// Suggest field mappings based on sample data
    /// </summary>
    Task<List<FieldMappingSuggestionDto>> SuggestFieldMappingsAsync(Dictionary<string, object> sampleData, string wfeEntityType);

    /// <summary>
    /// Validate field mapping configuration
    /// </summary>
    Task ValidateFieldMappingAsync(long integrationId, long fieldMappingId);
}

