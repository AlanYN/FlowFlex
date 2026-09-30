using Microsoft.Extensions.Logging;
using SqlSugar;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// Document operation log repository implementation
    /// </summary>
    public class DocumentOperationLogRepository : BaseRepository<DocumentOperationLog>, IDocumentOperationLogRepository, IScopedService
    {
        private readonly ILogger<DocumentOperationLogRepository> _logger;

        public DocumentOperationLogRepository(
            ISqlSugarClient sqlSugarClient,
            ILogger<DocumentOperationLogRepository> logger) : base(sqlSugarClient)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<(List<DocumentOperationLog> items, int totalCount)> GetLogsByUnitIdAsync(
            string unitId,
            DateTime? from = null,
            DateTime? to = null,
            string userId = null,
            string keyword = null,
            int pageIndex = 1,
            int pageSize = 50)
        {
            var query = db.Queryable<DocumentOperationLog>()
                .Where(x => x.UnitId == unitId && x.IsValid);

            if (from.HasValue)
                query = query.Where(x => x.CreateDate >= (DateTimeOffset)from.Value);

            if (to.HasValue)
                query = query.Where(x => x.CreateDate <= (DateTimeOffset)to.Value);

            if (!string.IsNullOrWhiteSpace(userId))
                query = query.Where(x => x.OperatorUserId == userId);

            // Keyword search: match against params_json text, old_values_json text,
            // operator_user_name, and mutation_id using PostgreSQL ILIKE / JSONB cast.
            // JSONB::text gives the full serialized JSON string for a simple text search.
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                query = query.Where(
                    $"(params_json::text ILIKE @kw " +
                    $"OR old_values_json::text ILIKE @kw " +
                    $"OR operator_user_name ILIKE @kw " +
                    $"OR mutation_id ILIKE @kw)",
                    new { kw = $"%{kw}%" });
            }

            query = query.OrderByDescending(x => x.CreateDate);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        /// <inheritdoc/>
        public async Task<int> GetRevisionAsync(string unitId)
        {
            var maxRevision = await db.Queryable<DocumentOperationLog>()
                .Where(x => x.UnitId == unitId && x.IsValid)
                .MaxAsync(x => x.Revision);

            return maxRevision;
        }

        /// <inheritdoc/>
        public async Task<DocumentOperationLog> GetLatestSetRangeLogAsync(string unitId, string subUnitId)
        {
            // Find the most recent set-range-values log for this sheet (subUnitId is stored
            // inside params_json). We use a PostgreSQL JSONB operator to filter efficiently.
            // The ->> operator extracts a top-level text field from jsonb.
            return await db.Queryable<DocumentOperationLog>()
                .Where(x => x.UnitId == unitId && x.IsValid &&
                             x.MutationId == "sheet.mutation.set-range-values")
                .Where("params_json->>'subUnitId' = @subUnitId", new { subUnitId })
                .OrderByDescending(x => x.CreateDate)
                .FirstAsync();
        }
    }
}
