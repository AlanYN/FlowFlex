using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    public interface IUserTourRecordRepository : IOwBaseRepository<UserTourRecord>
    {
        /// <summary>
        /// Returns true if the user has already seen the specified tour.
        /// </summary>
        Task<bool> HasSeenAsync(long userId, string tourKey);

        /// <summary>
        /// Marks a tour as seen. Idempotent — silently ignores duplicates.
        /// </summary>
        Task MarkSeenAsync(long userId, string tourKey);
    }
}
