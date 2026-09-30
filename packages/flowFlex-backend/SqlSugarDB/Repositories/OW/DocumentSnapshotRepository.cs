using Microsoft.Extensions.Logging;
using SqlSugar;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// Document snapshot repository implementation
    /// </summary>
    public class DocumentSnapshotRepository : BaseRepository<DocumentSnapshot>, IDocumentSnapshotRepository, IScopedService
    {
        private readonly ILogger<DocumentSnapshotRepository> _logger;

        public DocumentSnapshotRepository(
            ISqlSugarClient sqlSugarClient,
            ILogger<DocumentSnapshotRepository> logger) : base(sqlSugarClient)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<DocumentSnapshot> GetByUnitIdAsync(string unitId)
        {
            return await db.Queryable<DocumentSnapshot>()
                .Where(x => x.UnitId == unitId && x.IsValid)
                .FirstAsync();
        }

        /// <inheritdoc/>
        public async Task<List<DocumentSnapshot>> GetByEntityAsync(string entityType, long entityId)
        {
            return await db.Queryable<DocumentSnapshot>()
                .Where(x => x.EntityType == entityType && x.EntityId == entityId && x.IsValid)
                .OrderByDescending(x => x.ModifyDate)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<bool> UpsertByUnitIdAsync(DocumentSnapshot entity)
        {
            var existing = await GetByUnitIdAsync(entity.UnitId);
            if (existing == null)
            {
                entity.InitNewId();
                return await db.Insertable(entity).ExecuteCommandAsync() > 0;
            }

            return await db.Updateable<DocumentSnapshot>()
                .SetColumns(x => new DocumentSnapshot
                {
                    Title = entity.Title,
                    DataJson = entity.DataJson,
                    Revision = entity.Revision,
                    ModifyDate = DateTimeOffset.UtcNow,
                })
                .Where(x => x.Id == existing.Id)
                .ExecuteCommandAsync() > 0;
        }

        /// <inheritdoc/>
        public async Task<(List<DocumentSnapshot> items, int totalCount)> GetPagedAsync(
            int pageIndex,
            int pageSize,
            string keyword = null,
            string docType = null,
            string sortBy = "modify_date",
            string sortOrder = "desc")
        {
            var query = db.Queryable<DocumentSnapshot>()
                .Where(x => x.IsValid);

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(x => x.Title.Contains(keyword));

            if (!string.IsNullOrWhiteSpace(docType))
                query = query.Where(x => x.DocType == docType);

            // Sorting
            query = (sortBy, sortOrder.ToLower()) switch
            {
                ("title", "asc") => query.OrderBy(x => x.Title),
                ("title", _) => query.OrderByDescending(x => x.Title),
                ("create_date", "asc") => query.OrderBy(x => x.CreateDate),
                ("create_date", _) => query.OrderByDescending(x => x.CreateDate),
                (_, "asc") => query.OrderBy(x => x.ModifyDate),
                _ => query.OrderByDescending(x => x.ModifyDate),
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
