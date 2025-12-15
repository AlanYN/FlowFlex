using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// Message Attachment repository interface
    /// </summary>
    public interface IMessageAttachmentRepository : IBaseRepository<MessageAttachment>
    {
        /// <summary>
        /// Get attachments by message ID
        /// </summary>
        Task<List<MessageAttachment>> GetByMessageIdAsync(long messageId);

        /// <summary>
        /// Get attachment by external attachment ID (for Outlook sync)
        /// </summary>
        Task<MessageAttachment?> GetByExternalAttachmentIdAsync(string externalAttachmentId);

        /// <summary>
        /// Delete attachments by message ID
        /// </summary>
        Task<bool> DeleteByMessageIdAsync(long messageId);

        /// <summary>
        /// Associate attachments with message
        /// </summary>
        Task<bool> AssociateWithMessageAsync(List<long> attachmentIds, long messageId);

        /// <summary>
        /// Get unassociated attachments (temp attachments)
        /// </summary>
        Task<List<MessageAttachment>> GetUnassociatedAsync();

        /// <summary>
        /// Clean up old unassociated attachments
        /// </summary>
        Task<int> CleanupUnassociatedAsync(int olderThanHours = 24);
    }
}
