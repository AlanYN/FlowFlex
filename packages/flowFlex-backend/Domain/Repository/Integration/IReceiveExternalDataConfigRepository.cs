using FlowFlex.Domain.Entities.Integration;

namespace FlowFlex.Domain.Repository.Integration;

/// <summary>
/// Repository interface for ReceiveExternalDataConfig
/// </summary>
public interface IReceiveExternalDataConfigRepository : IBaseRepository<ReceiveExternalDataConfig>
{
    /// <summary>
    /// Get all configurations for an integration
    /// </summary>
    Task<List<ReceiveExternalDataConfig>> GetByIntegrationIdAsync(long integrationId);

    /// <summary>
    /// Get configuration by entity name
    /// </summary>
    Task<ReceiveExternalDataConfig?> GetByEntityNameAsync(long integrationId, string entityName);

    /// <summary>
    /// Check if entity name exists for an integration
    /// </summary>
    Task<bool> ExistsEntityNameAsync(long integrationId, string entityName, long? excludeId = null);

    /// <summary>
    /// Get active configurations for an integration
    /// </summary>
    Task<List<ReceiveExternalDataConfig>> GetActiveConfigsAsync(long integrationId);

    /// <summary>
    /// Get configurations by workflow ID
    /// </summary>
    Task<List<ReceiveExternalDataConfig>> GetByWorkflowIdAsync(long workflowId);
}

