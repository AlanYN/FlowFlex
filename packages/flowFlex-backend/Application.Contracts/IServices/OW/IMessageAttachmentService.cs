using FlowFlex.Application.Contracts.Dtos.OW.Message;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW;

/// <summary>
/// Message Attachment Service Interface
/// </summary>
public interface IMessageAttachmentService : IScopedService
{
    /// <summary>
    /// Get attachments for a message
    /// </summary>
    Task<List<MessageAttachmentDto>> GetByMessageIdAsync(long messageId);

    /// <summary>
    /// Get attachment by ID
    /// </summary>
    Task<MessageAttachmentDto?> GetByIdAsync(long id);

    /// <summary>
    /// Upload attachment (returns attachment ID)
    /// </summary>
    Task<long> UploadAsync(long messageId, string fileName, string contentType, Stream content);

    /// <summary>
    /// Upload attachment without message association (for draft attachments)
    /// </summary>
    Task<long> UploadTempAsync(string fileName, string contentType, Stream content);

    /// <summary>
    /// Download attachment content
    /// </summary>
    Task<(Stream Content, string ContentType, string FileName)?> DownloadAsync(long id);

    /// <summary>
    /// Delete attachment
    /// </summary>
    Task<bool> DeleteAsync(long id);

    /// <summary>
    /// Associate temporary attachments with a message
    /// </summary>
    Task<bool> AssociateWithMessageAsync(List<long> attachmentIds, long messageId);
}
