using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Application.Contracts.Dtos.OW.Message;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.IServices.OW;

/// <summary>
/// Message Center Service Interface
/// Handles Internal Messages, Customer Emails (via Outlook), and Portal Messages
/// </summary>
public interface IMessageService : IScopedService
{
    #region Message CRUD

    /// <summary>
    /// Get paginated message list with filtering and sorting
    /// Automatically merges local messages and Outlook emails
    /// </summary>
    Task<PageModelDto<MessageListItemDto>> GetPagedAsync(MessageQueryDto query);

    /// <summary>
    /// Get message detail by ID, automatically marks as read
    /// </summary>
    Task<MessageDetailDto?> GetByIdAsync(long id);

    /// <summary>
    /// Create and send a message
    /// - Internal: Local storage and delivery to recipient
    /// - Email: Send via Outlook
    /// - Portal: Local storage and push to Portal
    /// </summary>
    Task<long> CreateAsync(MessageCreateDto input);

    /// <summary>
    /// Update a draft message
    /// </summary>
    Task<bool> UpdateAsync(long id, MessageUpdateDto input);

    /// <summary>
    /// Delete message (move to Trash)
    /// Email type syncs to Outlook DeletedItems
    /// </summary>
    Task<bool> DeleteAsync(long id);

    /// <summary>
    /// Permanently delete message
    /// Email type permanently deletes from Outlook
    /// </summary>
    Task<bool> PermanentDeleteAsync(long id);

    /// <summary>
    /// Restore message from Trash
    /// Email type restores from Outlook DeletedItems
    /// </summary>
    Task<bool> RestoreAsync(long id);

    #endregion

    #region Message Operations

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
    /// Mark message as read
    /// Email type syncs to Outlook
    /// </summary>
    Task<bool> MarkAsReadAsync(long id);

    /// <summary>
    /// Mark message as unread
    /// Email type syncs to Outlook
    /// </summary>
    Task<bool> MarkAsUnreadAsync(long id);

    #endregion

    #region Reply and Forward

    /// <summary>
    /// Reply to a message
    /// Creates new message with original sender as recipient
    /// Preserves conversation thread and related entity
    /// </summary>
    Task<long> ReplyAsync(long id, MessageReplyDto input);

    /// <summary>
    /// Forward a message
    /// Creates new message with original content
    /// Preserves related entity association
    /// </summary>
    Task<long> ForwardAsync(long id, MessageForwardDto input);

    #endregion

    #region Drafts

    /// <summary>
    /// Save message as draft
    /// Email type syncs to Outlook Drafts
    /// </summary>
    Task<long> SaveDraftAsync(MessageCreateDto input);

    /// <summary>
    /// Send a draft message
    /// Email type sends via Outlook
    /// </summary>
    Task<long> SendDraftAsync(long id);

    #endregion

    #region Statistics and Sync

    /// <summary>
    /// Get folder statistics (total count, unread count, by message type)
    /// </summary>
    Task<List<FolderStatsDto>> GetFolderStatsAsync();

    /// <summary>
    /// Get unread message count for inbox
    /// </summary>
    Task<int> GetUnreadCountAsync();

    /// <summary>
    /// Manually trigger Outlook email sync
    /// </summary>
    Task<int> SyncOutlookEmailsAsync();

    #endregion
}
