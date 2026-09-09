using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// What's New read-status repository interface.
    /// Read status is per-user only — no app_code / tenant_id scoping.
    /// </summary>
    public interface IWhatsNewReadStatusRepository : IBaseRepository<WhatsNewReadStatus>
    {
        /// <summary>
        /// Idempotently mark a single entry as read for the given user.
        /// Uses INSERT ... ON CONFLICT DO NOTHING under the hood.
        /// </summary>
        Task MarkReadAsync(long whatsNewId, long userId);

        /// <summary>
        /// Bulk-mark all supplied entry IDs as read for the given user
        /// </summary>
        Task MarkAllReadAsync(List<long> whatsNewIds, long userId);

        /// <summary>
        /// Return the set of whatsNewIds already read by the given user
        /// </summary>
        Task<HashSet<long>> GetReadIdsAsync(long userId);

        /// <summary>
        /// Get the total number of distinct users who have read a specific entry (admin use)
        /// </summary>
        Task<int> GetReadCountAsync(long whatsNewId);

        /// <summary>
        /// Get the number of Published entries the given user has not yet read
        /// </summary>
        Task<int> GetUnreadCountAsync(long userId, List<long> publishedIds);
    }
}
