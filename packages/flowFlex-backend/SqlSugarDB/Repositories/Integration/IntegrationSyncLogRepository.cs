using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Linq.Expressions;
using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
using Microsoft.AspNetCore.Http;
using Domain.Shared.Enums;

namespace FlowFlex.SqlSugarDB.Implements.Integration
{
    /// <summary>
    /// Integration sync log repository implementation
    /// </summary>
    public class IntegrationSyncLogRepository : BaseRepository<IntegrationSyncLog>, IIntegrationSyncLogRepository, IScopedService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<IntegrationSyncLogRepository> _logger;

        public IntegrationSyncLogRepository(
            ISqlSugarClient sqlSugarClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<IntegrationSyncLogRepository> logger) : base(sqlSugarClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Get sync logs by integration ID
        /// </summary>
        public async Task<List<IntegrationSyncLog>> GetByIntegrationIdAsync(long integrationId, int limit = 100)
        {
            return await db.Queryable<IntegrationSyncLog>()
                .Where(x => x.IntegrationId == integrationId && x.IsValid)
                .OrderBy(x => x.CreateDate, OrderByType.Desc)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Query sync logs with pagination
        /// </summary>
        public async Task<(List<IntegrationSyncLog> items, int total)> QueryPagedAsync(
            int pageIndex,
            int pageSize,
            long? integrationId = null,
            string? direction = null,
            string? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string sortField = "SyncedAt",
            string sortDirection = "desc")
        {
            var whereExpressions = new List<Expression<Func<IntegrationSyncLog, bool>>>();
            whereExpressions.Add(x => x.IsValid == true);

            if (integrationId.HasValue)
            {
                whereExpressions.Add(x => x.IntegrationId == integrationId.Value);
            }

            if (!string.IsNullOrWhiteSpace(direction))
            {
                whereExpressions.Add(x => x.SyncDirection.ToString() == direction);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                whereExpressions.Add(x => x.SyncStatus.ToString() == status);
            }

            if (startDate.HasValue)
            {
                whereExpressions.Add(x => x.CreateDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                whereExpressions.Add(x => x.CreateDate <= endDate.Value);
            }

            var query = db.Queryable<IntegrationSyncLog>();

            foreach (var expr in whereExpressions)
            {
                query = query.Where(expr);
            }

            var total = await query.CountAsync();

            // Convert sortField from PascalCase to snake_case for database column name
            // Handle special case: SyncTime -> SyncedAt -> synced_at
            var normalizedSortField = sortField;
            if (sortField.Equals("SyncTime", StringComparison.OrdinalIgnoreCase))
            {
                normalizedSortField = "SyncedAt";
            }
            
            var dbSortField = SqlSugar.UtilMethods.ToUnderLine(normalizedSortField);
            var orderByClause = sortDirection.ToLower() == "asc" 
                ? $"{dbSortField} ASC" 
                : $"{dbSortField} DESC";

            var items = await query
                .OrderBy(orderByClause)
                .ToPageListAsync(pageIndex, pageSize);

            return (items, total);
        }

        /// <summary>
        /// Get sync statistics
        /// </summary>
        public async Task<Dictionary<string, int>> GetSyncStatisticsAsync(
            long integrationId,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = db.Queryable<IntegrationSyncLog>()
                .Where(x => x.IntegrationId == integrationId && x.IsValid);

            if (startDate.HasValue)
            {
                query = query.Where(x => x.CreateDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.CreateDate <= endDate.Value);
            }

            var logs = await query.ToListAsync();

            var statistics = new Dictionary<string, int>
            {
                ["Total"] = logs.Count,
                ["Success"] = logs.Count(x => x.SyncStatus == SyncStatus.Success),
                ["Failed"] = logs.Count(x => x.SyncStatus == SyncStatus.Failed),
                ["Pending"] = logs.Count(x => x.SyncStatus == SyncStatus.Pending),
                ["InProgress"] = logs.Count(x => x.SyncStatus == SyncStatus.InProgress),
                ["PartialSuccess"] = logs.Count(x => x.SyncStatus == SyncStatus.PartialSuccess)
            };

            return statistics;
        }

        /// <summary>
        /// Delete sync logs by integration ID
        /// </summary>
        public async Task<bool> DeleteByIntegrationIdAsync(long integrationId)
        {
            var logs = await db.Queryable<IntegrationSyncLog>()
                .Where(x => x.IntegrationId == integrationId)
                .ToListAsync();

            if (logs.Any())
            {
                foreach (var log in logs)
                {
                    log.IsValid = false;
                }
                return await db.Updateable(logs).ExecuteCommandAsync() > 0;
            }

            return true;
        }

        /// <summary>
        /// Get failed sync logs
        /// </summary>
        public async Task<List<IntegrationSyncLog>> GetFailedSyncLogsAsync(long integrationId, int limit = 50)
        {
            return await db.Queryable<IntegrationSyncLog>()
                .Where(x => x.IntegrationId == integrationId 
                    && x.SyncStatus == SyncStatus.Failed 
                    && x.IsValid)
                .OrderBy(x => x.CreateDate, OrderByType.Desc)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Get failed sync logs (interface method)
        /// </summary>
        public async Task<List<IntegrationSyncLog>> GetFailedLogsAsync(long? integrationId = null, int limit = 100)
        {
            var query = db.Queryable<IntegrationSyncLog>()
                .Where(x => x.SyncStatus == SyncStatus.Failed && x.IsValid);

            if (integrationId.HasValue)
            {
                query = query.Where(x => x.IntegrationId == integrationId.Value);
            }

            return await query
                .OrderBy(x => x.CreateDate, OrderByType.Desc)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Get sync logs by external record ID
        /// </summary>
        public async Task<List<IntegrationSyncLog>> GetByExternalRecordIdAsync(string externalRecordId)
        {
            return await db.Queryable<IntegrationSyncLog>()
                .Where(x => x.ExternalId == externalRecordId && x.IsValid)
                .OrderBy(x => x.CreateDate, OrderByType.Desc)
                .ToListAsync();
        }

        /// <summary>
        /// Get sync logs by WFE record ID
        /// </summary>
        public async Task<List<IntegrationSyncLog>> GetByWfeRecordIdAsync(long wfeRecordId)
        {
            return await db.Queryable<IntegrationSyncLog>()
                .Where(x => x.InternalId == wfeRecordId.ToString() && x.IsValid)
                .OrderBy(x => x.CreateDate, OrderByType.Desc)
                .ToListAsync();
        }

        /// <summary>
        /// Delete old sync logs
        /// </summary>
        public async Task<bool> DeleteOldLogsAsync(DateTime beforeDate)
        {
            var logs = await db.Queryable<IntegrationSyncLog>()
                .Where(x => x.CreateDate < beforeDate)
                .ToListAsync();

            if (logs.Any())
            {
                foreach (var log in logs)
                {
                    log.IsValid = false;
                }
                return await db.Updateable(logs).ExecuteCommandAsync() > 0;
            }

            return true;
        }
    }
}

