using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.SqlSugarDB.Context;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// User invitation repository implementation
    /// </summary>
    public class UserInvitationRepository : OwBaseRepository<UserInvitation>, IUserInvitationRepository, IScopedService
    {
        public UserInvitationRepository(ISqlSugarContext context) : base(context)
        {
        }

        /// <summary>
        /// Get invitation by token
        /// </summary>
        /// <param name="token">Invitation token</param>
        /// <returns>User invitation</returns>
        public async Task<UserInvitation> GetByTokenAsync(string token)
        {
            return await _db.Queryable<UserInvitation>()
                .Where(x => x.InvitationToken == token && x.IsValid)
                .FirstAsync();
        }

        /// <summary>
        /// Get invitations by onboarding ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>List of user invitations</returns>
        public async Task<List<UserInvitation>> GetByOnboardingIdAsync(long onboardingId)
        {
            return await _db.Queryable<UserInvitation>()
                .Where(x => x.OnboardingId == onboardingId && x.IsValid)
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get invitation by email and onboarding ID
        /// </summary>
        /// <param name="email">Email address</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>User invitation</returns>
        public async Task<UserInvitation> GetByEmailAndOnboardingIdAsync(string email, long onboardingId)
        {
            return await _db.Queryable<UserInvitation>()
                .Where(x => x.Email == email && x.OnboardingId == onboardingId && x.IsValid)
                .OrderByDescending(x => x.CreateDate)
                .FirstAsync();
        }

        /// <summary>
        /// Check if invitation exists for email and onboarding
        /// </summary>
        /// <param name="email">Email address</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Whether invitation exists</returns>
        public async Task<bool> ExistsAsync(string email, long onboardingId)
        {
            return await _db.Queryable<UserInvitation>()
                .Where(x => x.Email == email && x.OnboardingId == onboardingId && x.IsValid)
                .AnyAsync();
        }

        /// <summary>
        /// Update invitation status
        /// </summary>
        /// <param name="id">Invitation ID</param>
        /// <param name="status">New status</param>
        /// <returns>Whether update was successful</returns>
        public async Task<bool> UpdateStatusAsync(long id, string status)
        {
            var result = await _db.Updateable<UserInvitation>()
                .SetColumns(x => new UserInvitation { Status = status, ModifyDate = DateTimeOffset.UtcNow })
                .Where(x => x.Id == id)
                .ExecuteCommandAsync();

            return result > 0;
        }

        /// <summary>
        /// Mark invitation as used
        /// </summary>
        /// <param name="token">Invitation token</param>
        /// <param name="userId">User ID</param>
        /// <returns>Whether update was successful</returns>
        public async Task<bool> MarkAsUsedAsync(string token, long userId)
        {
            var result = await _db.Updateable<UserInvitation>()
                .SetColumns(x => new UserInvitation
                {
                    Status = "Used",
                    UserId = userId,
                    LastAccessDate = DateTimeOffset.UtcNow,
                    ModifyDate = DateTimeOffset.UtcNow
                })
                .Where(x => x.InvitationToken == token)
                .ExecuteCommandAsync();

            return result > 0;
        }
    }
}