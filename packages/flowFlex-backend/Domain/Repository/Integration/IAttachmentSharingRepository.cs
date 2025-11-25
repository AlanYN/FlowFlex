using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository;

namespace FlowFlex.Domain.Repository.Integration
{
    /// <summary>
    /// Attachment sharing repository interface
    /// </summary>
    public interface IAttachmentSharingRepository : IBaseRepository<AttachmentSharing>
    {
        /// <summary>
        /// Get attachment sharing configurations by integration ID
        /// </summary>
        Task<List<AttachmentSharing>> GetByIntegrationIdAsync(long integrationId);

        /// <summary>
        /// Get attachment sharing by system ID
        /// </summary>
        Task<AttachmentSharing?> GetBySystemIdAsync(string systemId);

        /// <summary>
        /// Check if external module name exists for the integration
        /// </summary>
        Task<bool> ExistsModuleNameAsync(long integrationId, string externalModuleName, long? excludeId = null);

        /// <summary>
        /// Check if system ID exists
        /// </summary>
        Task<bool> ExistsSystemIdAsync(string systemId);

        /// <summary>
        /// Delete attachment sharing configurations by integration ID
        /// </summary>
        Task<bool> DeleteByIntegrationIdAsync(long integrationId);

        /// <summary>
        /// Get active attachment sharing configurations by workflow ID
        /// </summary>
        Task<List<AttachmentSharing>> GetByWorkflowIdAsync(long workflowId);
    }
}

