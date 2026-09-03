using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// What's New repository implementation.
    /// What's New is a global product announcement — no app_code / tenant_id filtering applied.
    /// </summary>
    public class WhatsNewRepository : BaseRepository<WhatsNew>, IWhatsNewRepository, IScopedService
    {
        private readonly ILogger<WhatsNewRepository> _logger;

        public WhatsNewRepository(
            ISqlSugarClient sqlSugarClient,
            ILogger<WhatsNewRepository> logger) : base(sqlSugarClient)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get Published entries ordered by publish_time DESC (panel use)
        /// </summary>
        public async Task<List<WhatsNew>> GetPublishedListAsync(int limit = 10)
        {
            _logger.LogInformation("[WhatsNewRepository] GetPublishedListAsync limit={Limit}", limit);

            return await db.Queryable<WhatsNew>()
                .Where(x => x.Status == 1 && x.IsValid == true)
                .OrderByDescending(x => x.PublishTime)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Get admin list with read counts; optionally filter by status.
        /// No app_code / tenant_id filter — What's New is a global product announcement.
        /// </summary>
        public async Task<List<WhatsNewAdminItemProjection>> GetAdminListAsync(int? statusFilter = null)
        {
            _logger.LogInformation(
                "[WhatsNewRepository] GetAdminListAsync StatusFilter={StatusFilter}", statusFilter);

            var parameters = new List<SugarParameter>();

            var whereClause = statusFilter.HasValue
                ? "AND w.status = @StatusFilter"
                : string.Empty;

            if (statusFilter.HasValue)
                parameters.Add(new SugarParameter("@StatusFilter", statusFilter.Value));

            var sql = $@"
SELECT
    w.id,
    w.title,
    w.summary,
    w.category,
    w.status,
    w.publish_time,
    COUNT(r.id) AS read_count
FROM ff_whats_new w
LEFT JOIN ff_whats_new_read_status r ON r.whats_new_id = w.id
WHERE w.is_valid = TRUE
  {whereClause}
GROUP BY w.id, w.title, w.summary, w.category, w.status, w.publish_time
ORDER BY w.create_date DESC";

            return await db.Ado.SqlQueryAsync<WhatsNewAdminItemProjection>(sql, parameters);
        }

        /// <summary>
        /// Get counts of Published and Draft entries (global, no app_code / tenant_id filter)
        /// </summary>
        public async Task<(int publishedCount, int draftCount)> GetStatusCountsAsync()
        {
            var publishedCount = await db.Queryable<WhatsNew>()
                .Where(x => x.Status == 1 && x.IsValid == true)
                .CountAsync();

            var draftCount = await db.Queryable<WhatsNew>()
                .Where(x => x.Status == 0 && x.IsValid == true)
                .CountAsync();

            return (publishedCount, draftCount);
        }

    }
}
