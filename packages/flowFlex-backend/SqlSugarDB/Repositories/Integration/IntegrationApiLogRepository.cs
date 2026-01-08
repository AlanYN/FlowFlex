using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
using Microsoft.AspNetCore.Http;
using SqlSugar;

namespace FlowFlex.SqlSugarDB.Repositories.Integration;

/// <summary>
/// Integration API Log repository implementation
/// </summary>
public class IntegrationApiLogRepository : BaseRepository<IntegrationApiLog>, IIntegrationApiLogRepository, IScopedService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IntegrationApiLogRepository(ISqlSugarClient dbContext, IHttpContextAccessor httpContextAccessor) 
        : base(dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private string GetCurrentTenantId()
    {
        return _httpContextAccessor.HttpContext?.Items["TenantId"]?.ToString() ?? "default";
    }

    /// <summary>
    /// Insert log with proper jsonb handling for PostgreSQL
    /// </summary>
    public async Task<long> InsertLogAsync(IntegrationApiLog log)
    {
        // Use CAST(NULL AS type) for nullable fields to handle NULL properly
        var sql = @"
            INSERT INTO ff_integration_api_log (
                id, tenant_id, app_code, integration_id, system_id, endpoint, http_method,
                started_at, completed_at, duration_ms, status_code, is_success, error_message,
                request_params, caller_user_id, caller_ip,
                create_date, modify_date, create_by, modify_by, create_user_id, modify_user_id, is_valid
            ) VALUES (
                @Id, @TenantId, @AppCode, @IntegrationId, @SystemId, @Endpoint, @HttpMethod,
                @StartedAt::timestamptz, 
                CAST(NULLIF(@CompletedAt, '') AS timestamptz), 
                CAST(NULLIF(@DurationMs, -1) AS bigint), 
                @StatusCode, @IsSuccess, @ErrorMessage,
                @RequestParams::jsonb, 
                CAST(NULLIF(@CallerUserId, -1) AS bigint), 
                @CallerIp,
                @CreateDate::timestamptz, @ModifyDate::timestamptz, @CreateBy, @ModifyBy, @CreateUserId, @ModifyUserId, @IsValid
            )";

        var parameters = new List<SugarParameter>
        {
            new SugarParameter("@Id", log.Id),
            new SugarParameter("@TenantId", log.TenantId),
            new SugarParameter("@AppCode", (log.AppCode ?? "default").ToLowerInvariant()),
            new SugarParameter("@IntegrationId", log.IntegrationId),
            new SugarParameter("@SystemId", log.SystemId),
            new SugarParameter("@Endpoint", log.Endpoint),
            new SugarParameter("@HttpMethod", log.HttpMethod),
            new SugarParameter("@StartedAt", log.StartedAt.ToString("o")),
            new SugarParameter("@CompletedAt", log.CompletedAt?.ToString("o") ?? ""),
            new SugarParameter("@DurationMs", log.DurationMs ?? -1),
            new SugarParameter("@StatusCode", log.StatusCode),
            new SugarParameter("@IsSuccess", log.IsSuccess),
            new SugarParameter("@ErrorMessage", log.ErrorMessage ?? (object)DBNull.Value),
            new SugarParameter("@RequestParams", log.RequestParams ?? "{}"),
            new SugarParameter("@CallerUserId", log.CallerUserId ?? -1),
            new SugarParameter("@CallerIp", log.CallerIp ?? (object)DBNull.Value),
            new SugarParameter("@CreateDate", log.CreateDate.ToString("o")),
            new SugarParameter("@ModifyDate", log.ModifyDate.ToString("o")),
            new SugarParameter("@CreateBy", log.CreateBy ?? (object)DBNull.Value),
            new SugarParameter("@ModifyBy", log.ModifyBy ?? (object)DBNull.Value),
            new SugarParameter("@CreateUserId", log.CreateUserId),
            new SugarParameter("@ModifyUserId", log.ModifyUserId),
            new SugarParameter("@IsValid", log.IsValid)
        };

        await db.Ado.ExecuteCommandAsync(sql, parameters);
        return log.Id;
    }

    /// <summary>
    /// Get logs by integration ID within date range
    /// </summary>
    public async Task<List<IntegrationApiLog>> GetByIntegrationIdAsync(long integrationId, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var tenantId = GetCurrentTenantId();
        return await db.Queryable<IntegrationApiLog>()
            .Where(l => l.TenantId == tenantId && 
                        l.IntegrationId == integrationId && 
                        l.CreateDate >= startDate && 
                        l.CreateDate <= endDate &&
                        l.IsValid)
            .OrderByDescending(l => l.CreateDate)
            .ToListAsync();
    }

    /// <summary>
    /// Get daily duration statistics for an integration
    /// </summary>
    public async Task<Dictionary<string, long>> GetDailyDurationStatsAsync(long integrationId, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var tenantId = GetCurrentTenantId();
        var logs = await db.Queryable<IntegrationApiLog>()
            .Where(l => l.TenantId == tenantId && 
                        l.IntegrationId == integrationId && 
                        l.CreateDate >= startDate && 
                        l.CreateDate <= endDate &&
                        l.DurationMs != null &&
                        l.IsValid)
            .Select(l => new { l.CreateDate, l.DurationMs })
            .ToListAsync();

        return logs
            .GroupBy(l => l.CreateDate.UtcDateTime.Date.ToString("yyyy-MM-dd"))
            .ToDictionary(
                g => g.Key,
                g => g.Sum(l => l.DurationMs ?? 0)
            );
    }

    /// <summary>
    /// Get daily duration statistics for multiple integrations
    /// </summary>
    public async Task<Dictionary<long, Dictionary<string, long>>> GetDailyDurationStatsBatchAsync(List<long> integrationIds, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        if (integrationIds == null || integrationIds.Count == 0)
        {
            return new Dictionary<long, Dictionary<string, long>>();
        }

        var tenantId = GetCurrentTenantId();
        var logs = await db.Queryable<IntegrationApiLog>()
            .Where(l => l.TenantId == tenantId && 
                        integrationIds.Contains(l.IntegrationId) && 
                        l.CreateDate >= startDate && 
                        l.CreateDate <= endDate &&
                        l.DurationMs != null &&
                        l.IsValid)
            .Select(l => new { l.IntegrationId, l.CreateDate, l.DurationMs })
            .ToListAsync();

        return logs
            .GroupBy(l => l.IntegrationId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(l => l.CreateDate.UtcDateTime.Date.ToString("yyyy-MM-dd"))
                      .ToDictionary(
                          dg => dg.Key,
                          dg => dg.Sum(l => l.DurationMs ?? 0)
                      )
            );
    }
}
