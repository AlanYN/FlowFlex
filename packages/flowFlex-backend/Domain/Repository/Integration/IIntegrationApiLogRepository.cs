using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Domain.Repository.Integration;

/// <summary>
/// Integration API Log repository interface
/// </summary>
public interface IIntegrationApiLogRepository : IBaseRepository<IntegrationApiLog>, IScopedService
{
    /// <summary>
    /// Insert log with proper jsonb handling for PostgreSQL
    /// </summary>
    Task<long> InsertLogAsync(IntegrationApiLog log);

    /// <summary>
    /// Get logs by integration ID within date range
    /// </summary>
    Task<List<IntegrationApiLog>> GetByIntegrationIdAsync(long integrationId, DateTimeOffset startDate, DateTimeOffset endDate);

    /// <summary>
    /// Get daily duration statistics for an integration
    /// </summary>
    Task<Dictionary<string, long>> GetDailyDurationStatsAsync(long integrationId, DateTimeOffset startDate, DateTimeOffset endDate);

    /// <summary>
    /// Get daily duration statistics for multiple integrations
    /// </summary>
    Task<Dictionary<long, Dictionary<string, long>>> GetDailyDurationStatsBatchAsync(List<long> integrationIds, DateTimeOffset startDate, DateTimeOffset endDate);
}
