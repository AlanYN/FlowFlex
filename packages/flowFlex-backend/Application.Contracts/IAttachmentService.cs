using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Domain.Shared.Enums;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FlowFlex.Application.Contracts
{
    public interface IAttachmentService
    {
        /// <summary>
        /// Creates a new attachment
        /// </summary>
        /// <param name="attachment">Attachment data</param>
        /// <param name="tenantId">Tenant identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created attachment details</returns>
        Task<AttachmentOutputDto> CreateAttachmentAsync(AttachmentDto attachment, string tenantId, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves an attachment by its ID
        /// </summary>
        /// <param name="Id">Attachment identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Attachment details</returns>
        Task<AttachmentOutputDto> GetAttachmentByIdAsync(long Id, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves multiple attachments by their IDs
        /// </summary>
        /// <param name="Ids">List of attachment identifiers</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of attachment details</returns>
        Task<List<AttachmentOutputDto>> GetAttachmentsAsync(List<long> Ids, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves the URL of an attachment
        /// </summary>
        /// <param name="id">Attachment identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>URL of the attachment</returns>
        Task<string> GetAttachmentUrlAsync(long id, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes an attachment
        /// </summary>
        /// <param name="attachmentId">Attachment identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task DeleteAttachment(long attachmentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes multiple attachments in batch
        /// </summary>
        /// <param name="attachmentIds">List of attachment identifiers</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task BatchDeleteAttachment(IList<long> attachmentIds);

        /// <summary>
        /// Deletes attachments associated with a specific business ID
        /// </summary>
        /// <param name="businessId">Business identifier</param>
        /// <returns>True if deletion was successful, otherwise false</returns>
        Task<bool> DeleteByBusinessIdAsync(long businessId);

        /// <summary>
        /// Deletes the mapping relationship and attachments not referenced by other business data
        /// </summary>
        /// <param name="mappingId">Mapping identifier</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task DeleteByMappingIdAsync(long mappingId);

        /// <summary>
        /// Retrieves attachments associated with a specific business ID
        /// </summary>
        /// <param name="businessId">Business identifier</param>
        /// <returns>List of attachment details</returns>
        Task<List<AttachmentOutputDto>> GetAttachmentsByBusinessIdAsync(long businessId);

        /// <summary>
        /// Retrieves attachments associated with a specific business ID and attachment type
        /// </summary>
        /// <param name="businessId">Business identifier</param>
        /// <param name="attachmentType">Type of attachment</param>
        /// <returns>List of attachment details</returns>
        Task<List<AttachmentOutputDto>> GetAttachmentsByAttachmentTypeAsync(long businessId, AttachmentTypeEnum attachmentType);

        /// <summary>
        /// Retrieves the file content and details of an attachment
        /// </summary>
        /// <param name="id">Attachment identifier</param>
        /// <returns>Tuple containing the file stream and attachment details</returns>
        Task<(Stream, AttachmentOutputDto)> GetAttachmentAsync(long id);
    }
}
