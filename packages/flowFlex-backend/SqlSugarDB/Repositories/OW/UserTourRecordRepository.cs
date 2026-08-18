using SqlSugar;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.SqlSugarDB.Repositories.OW
{
    /// <summary>
    /// Repository for user tour seen records.
    /// </summary>
    public class UserTourRecordRepository : BaseRepository<UserTourRecord>, IUserTourRecordRepository, IScopedService
    {
        public UserTourRecordRepository(ISqlSugarClient db) : base(db)
        {
        }

        public async Task<bool> HasSeenAsync(long userId, string tourKey)
        {
            return await db.Queryable<UserTourRecord>()
                .Where(x => x.UserId == userId && x.TourKey == tourKey && x.IsValid)
                .AnyAsync();
        }

        public async Task MarkSeenAsync(long userId, string tourKey)
        {
            // Idempotent: only insert if no record exists yet.
            var exists = await HasSeenAsync(userId, tourKey);
            if (exists) return;

            var record = new UserTourRecord
            {
                UserId = userId,
                TourKey = tourKey,
                SeenAt = DateTimeOffset.UtcNow,
            };
            record.InitNewId();

            await db.Insertable(record).ExecuteCommandAsync();
        }
    }
}
