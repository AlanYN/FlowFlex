using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// User signature repository interface
    /// </summary>
    public interface IUserSignatureRepository : IBaseRepository<UserSignature>
    {
        /// <summary>
        /// Get all valid signatures for a specific user, bypassing multi-tenant global filter
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of signatures belonging to the user</returns>
        Task<List<UserSignature>> GetByUserIdAsync(long userId);
    }
}
