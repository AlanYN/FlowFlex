using FlowFlex.Domain.Repository;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// Workflow version history repository interface
    /// </summary>
    public interface IWorkflowVersionRepository : IBaseRepository<WorkflowVersion>
    {
        /// <summary>
        /// Get version history by original workflow ID
        /// </summary>
        /// <param name="originalWorkflowId">Original workflow ID</param>
        /// <returns>Version history list</returns>
        Task<List<WorkflowVersion>> GetVersionHistoryAsync(long originalWorkflowId);

        /// <summary>
        /// Create version history record
        /// </summary>
        /// <param name="workflow">Workflow entity</param>
        /// <param name="changeType">Change type</param>
        /// <param name="changeReason">Change reason</param>
        /// <returns>Whether successful</returns>
        Task<bool> CreateVersionHistoryAsync(Workflow workflow, string changeType, string changeReason = null);

        /// <summary>
        /// Get latest version number
        /// </summary>
        /// <param name="originalWorkflowId">Original workflow ID</param>
        /// <returns>Latest version number</returns>
        Task<int> GetLatestVersionAsync(long originalWorkflowId);

        /// <summary>
        /// Get history record by version number
        /// </summary>
        /// <param name="originalWorkflowId">Original workflow ID</param>
        /// <param name="version">Version number</param>
        /// <returns>Version history record</returns>
        Task<WorkflowVersion> GetByVersionAsync(long originalWorkflowId, int version);

        /// <summary>
        /// Get version details by version ID
        /// </summary>
        /// <param name="versionId">Version ID</param>
        /// <returns>Version details</returns>
        Task<WorkflowVersion> GetVersionDetailAsync(long versionId);

        /// <summary>
        /// Create version history record (including stage snapshots)
        /// </summary>
        /// <param name="workflow">Workflow entity</param>
        /// <param name="stages">Stage list</param>
        /// <param name="changeType">Change type</param>
        /// <param name="changeReason">Change reason</param>
        /// <returns>Version history record ID</returns>
        Task<long> CreateVersionHistoryWithStagesAsync(Workflow workflow, List<Stage> stages, string changeType, string changeReason = null);
    }
}
