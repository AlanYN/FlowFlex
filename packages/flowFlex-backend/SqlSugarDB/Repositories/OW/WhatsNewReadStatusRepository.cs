using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace FlowFlex.SqlSugarDB.Repositories.OW
{
    /// <summary>
    /// Repository for What's New read-status records.
    /// Read status is keyed by (whats_new_id, user_id) only —
    /// no app_code / tenant_id scoping because What's New is a global product announcement.
    /// </summary>
    public class WhatsNewReadStatusRepository : BaseRepository<WhatsNewReadStatus>, IWhatsNewReadStatusRepository, IScopedService
    {
        private readonly ILogger<WhatsNewReadStatusRepository> _logger;

        public WhatsNewReadStatusRepository(
            ISqlSugarClient db,
            ILogger<WhatsNewReadStatusRepository> logger) : base(db)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task MarkReadAsync(long whatsNewId, long userId)
        {
            var sql = @"
                INSERT INTO ff_whats_new_read_status (id, whats_new_id, user_id, read_time)
                VALUES (@Id, @WhatsNewId, @UserId, NOW())
                ON CONFLICT (whats_new_id, user_id) DO NOTHING";

            var newId = SnowFlakeSingle.Instance.NextId();

            await db.Ado.ExecuteCommandAsync(sql,
                new SugarParameter("@Id", newId),
                new SugarParameter("@WhatsNewId", whatsNewId),
                new SugarParameter("@UserId", userId));
        }

        /// <inheritdoc />
        public async Task MarkAllReadAsync(List<long> whatsNewIds, long userId)
        {
            if (whatsNewIds == null || whatsNewIds.Count == 0)
                return;

            foreach (var id in whatsNewIds)
                await MarkReadAsync(id, userId);
        }

        /// <inheritdoc />
        public async Task<HashSet<long>> GetReadIdsAsync(long userId)
        {
            var list = await db.Queryable<WhatsNewReadStatus>()
                .Filter(null, true)
                .Where(x => x.UserId == userId)
                .Select(x => x.WhatsNewId)
                .ToListAsync();

            return new HashSet<long>(list);
        }

        /// <inheritdoc />
        public async Task<int> GetReadCountAsync(long whatsNewId)
        {
            return await db.Queryable<WhatsNewReadStatus>()
                .Filter(null, true)
                .Where(x => x.WhatsNewId == whatsNewId)
                .CountAsync();
        }

        /// <inheritdoc />
        public async Task<int> GetUnreadCountAsync(long userId, List<long> publishedIds)
        {
            if (publishedIds == null || publishedIds.Count == 0)
                return 0;

            var readIds = await GetReadIdsAsync(userId);
            return publishedIds.Count(id => !readIds.Contains(id));
        }
    }
}
