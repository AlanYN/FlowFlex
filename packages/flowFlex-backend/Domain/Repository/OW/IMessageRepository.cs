using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// Message repository interface
    /// </summary>
    public interface IMessageRepository : IBaseRepository<Message>
    {
        /// <summary>
        /// Get messages by owner ID with pagination
        /// </summary>
        Task<(List<Message> items, int totalCount)> GetPagedByOwnerAsync(
            long ownerId,
            int pageIndex,
            int pageSize,
            string? folder = null,
            string? label = null,
            string? messageType = null,
            string? searchTerm = null,
            long? relatedEntityId = null,
            string sortField = "ReceivedDate",
            string sortDirection = "desc");

        /// <summary>
        /// Get messages by folder
        /// </summary>
        Task<List<Message>> GetByFolderAsync(long ownerId, string folder);

        /// <summary>
        /// Get starred messages
        /// </summary>
        Task<List<Message>> GetStarredAsync(long ownerId);

        /// <summary>
        /// Get unread messages count
        /// </summary>
        Task<int> GetUnreadCountAsync(long ownerId, string? folder = null);

        /// <summary>
        /// Get folder statistics
        /// </summary>
        Task<Dictionary<string, (int total, int unread, int internalCount, int emailCount, int portalCount)>> GetFolderStatsAsync(long ownerId);

        /// <summary>
        /// Mark message as read
        /// </summary>
        Task<bool> MarkAsReadAsync(long id);

        /// <summary>
        /// Mark message as unread
        /// </summary>
        Task<bool> MarkAsUnreadAsync(long id);

        /// <summary>
        /// Star a message
        /// </summary>
        Task<bool> StarAsync(long id);

        /// <summary>
        /// Unstar a message
        /// </summary>
        Task<bool> UnstarAsync(long id);

        /// <summary>
        /// Archive a message
        /// </summary>
        Task<bool> ArchiveAsync(long id);

        /// <summary>
        /// Unarchive a message
        /// </summary>
        Task<bool> UnarchiveAsync(long id);

        /// <summary>
        /// Get archived messages
        /// </summary>
        Task<List<Message>> GetArchivedAsync(long ownerId);

        /// <summary>
        /// Move message to folder
        /// </summary>
        Task<bool> MoveToFolderAsync(long id, string folder, string? originalFolder = null);

        /// <summary>
        /// Get message by external message ID (for Outlook sync)
        /// </summary>
        Task<Message?> GetByExternalMessageIdAsync(string externalMessageId, long ownerId);

        /// <summary>
        /// Get messages by conversation ID
        /// </summary>
        Task<List<Message>> GetByConversationIdAsync(string conversationId, long ownerId);

        /// <summary>
        /// Get first message by conversation ID (without owner filter)
        /// </summary>
        Task<Message?> GetByConversationIdAsync(string conversationId);

        /// <summary>
        /// Get messages by related entity
        /// </summary>
        Task<List<Message>> GetByRelatedEntityAsync(string entityType, long entityId, long ownerId);

        /// <summary>
        /// Search messages by keyword in subject and body
        /// </summary>
        Task<List<Message>> SearchAsync(long ownerId, string keyword, string? folder = null);

        /// <summary>
        /// Get draft messages
        /// </summary>
        Task<List<Message>> GetDraftsAsync(long ownerId);

        /// <summary>
        /// Batch update messages folder
        /// </summary>
        Task<bool> BatchMoveToFolderAsync(List<long> ids, string folder);

        /// <summary>
        /// Batch mark messages as read
        /// </summary>
        Task<bool> BatchMarkAsReadAsync(List<long> ids);

        /// <summary>
        /// Permanently delete message
        /// </summary>
        Task<bool> PermanentDeleteAsync(long id);

        /// <summary>
        /// Find a locally sent message by subject and sent time (for linking with Outlook sync)
        /// </summary>
        Task<Message?> FindLocalSentMessageAsync(long ownerId, string subject, DateTimeOffset sentTime, TimeSpan tolerance);

        /// <summary>
        /// Get messages by folder that have ExternalMessageId (for sync deleted detection)
        /// </summary>
        Task<List<Message>> GetByFolderWithExternalIdAsync(long ownerId, string folder);

        /// <summary>
        /// Delete all synced emails for a user (soft delete)
        /// Used when user unbinds their email account
        /// </summary>
        Task<int> DeleteSyncedEmailsByOwnerAsync(long ownerId);

        /// <summary>
        /// Get existing external message IDs for batch sync optimization
        /// </summary>
        Task<HashSet<string>> GetExistingExternalIdsAsync(List<string> externalIds, long ownerId);

        /// <summary>
        /// Get messages by external IDs for status sync
        /// </summary>
        Task<List<Message>> GetByExternalIdsAsync(List<string> externalIds, long ownerId);

        /// <summary>
        /// Batch update message read status
        /// </summary>
        Task<int> BatchUpdateReadStatusAsync(List<long> ids, bool isRead);

        /// <summary>
        /// Get message by external ID (alias for GetByExternalMessageIdAsync)
        /// </summary>
        Task<Message?> GetByExternalIdAsync(string externalId, long ownerId);

        /// <summary>
        /// Batch move messages to folder
        /// </summary>
        Task<int> BatchMoveToFolderAsync(List<long> ids, string folder, string? originalFolder = null);

        #region Dashboard Methods

        /// <summary>
        /// Get recent messages for user (for dashboard)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="limit">Maximum number of messages</param>
        /// <returns>List of recent messages</returns>
        Task<List<Message>> GetRecentMessagesForUserAsync(long userId, int limit);

        /// <summary>
        /// Get unread message count for user (for dashboard)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Unread message count</returns>
        Task<int> GetUnreadCountForUserAsync(long userId);

        #endregion
    }
}
