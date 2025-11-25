using FlowFlex.Application.Contracts.Dtos.Integration;

namespace FlowFlex.Application.Contracts.IServices.Integration
{
    /// <summary>
    /// Attachment sharing service interface
    /// </summary>
    public interface IAttachmentSharingService
    {
        /// <summary>
        /// Create attachment sharing configuration
        /// </summary>
        Task<long> CreateAsync(AttachmentSharingInputDto input);

        /// <summary>
        /// Update attachment sharing configuration
        /// </summary>
        Task<bool> UpdateAsync(long id, AttachmentSharingInputDto input);

        /// <summary>
        /// Delete attachment sharing configuration (soft delete)
        /// </summary>
        Task<bool> DeleteAsync(long id);

        /// <summary>
        /// Get attachment sharing by ID
        /// </summary>
        Task<AttachmentSharingOutputDto?> GetByIdAsync(long id);

        /// <summary>
        /// Get attachment sharing by system ID
        /// </summary>
        Task<AttachmentSharingOutputDto?> GetBySystemIdAsync(string systemId);

        /// <summary>
        /// Get all attachment sharing configurations for an integration
        /// </summary>
        Task<List<AttachmentSharingOutputDto>> GetByIntegrationIdAsync(long integrationId);

        /// <summary>
        /// Get active attachment sharing configurations by workflow ID
        /// </summary>
        Task<List<AttachmentSharingOutputDto>> GetByWorkflowIdAsync(long workflowId);
    }
}

