using SqlSugar;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.SqlSugarDB.Repositories.OW
{
    /// <summary>
    /// User signature repository implementation.
    /// Queries bypass the multi-tenant global filter because ff_user_signature has no
    /// app_code / tenant_id columns — signatures are user-scoped, not tenant-scoped.
    /// </summary>
    public class UserSignatureRepository : BaseRepository<UserSignature>, IUserSignatureRepository, IScopedService
    {
        public UserSignatureRepository(ISqlSugarClient db) : base(db)
        {
        }

        /// <summary>
        /// Get all valid signatures for a specific user.
        /// Uses ClearFilter() to bypass any tenant/app global filters, then queries
        /// only by user_id and is_valid.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of valid signatures belonging to the user</returns>
        public async Task<List<UserSignature>> GetByUserIdAsync(long userId)
        {
            return await db.Queryable<UserSignature>()
                .ClearFilter()
                .Where(s => s.UserId == userId && s.IsValid == true)
                .OrderByDescending(s => s.CreateDate)
                .ToListAsync();
        }
    }
}
