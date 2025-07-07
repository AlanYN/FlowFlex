using FlowFlex.Application.Contracts.Dtos.OW.WorkflowVersion;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Workflow Version Service Interface
    /// </summary>
    public interface IWorkflowVersionService
    {
        /// <summary>
        /// Create workflow version
        /// </summary>
        /// <param name="dto">Workflow version data</param>
        /// <returns></returns>
        Task<WorkflowVersionOutputDto> CreateAsync(WorkflowVersionInputDto dto);

        /// <summary>
        /// Update workflow version
        /// </summary>
        /// <param name="id">Version ID</param>
        /// <param name="dto">Workflow version data</param>
        /// <returns></returns>
        Task<WorkflowVersionOutputDto> UpdateAsync(long id, WorkflowVersionInputDto dto);

        /// <summary>
        /// Delete workflow version
        /// </summary>
        /// <param name="id">Version ID</param>
        /// <returns></returns>
        Task<bool> DeleteAsync(long id);

        /// <summary>
        /// Get workflow version by ID
        /// </summary>
        /// <param name="id">Version ID</param>
        /// <returns></returns>
        Task<WorkflowVersionOutputDto> GetByIdAsync(long id);

        /// <summary>
        /// Get version list by original workflow ID
        /// </summary>
        /// <param name="originalWorkflowId">Original workflow ID</param>
        /// <returns></returns>
        Task<List<WorkflowVersionOutputDto>> GetByOriginalWorkflowIdAsync(long originalWorkflowId);

        /// <summary>
        /// Get paginated list
        /// </summary>
        /// <param name="request">Query request</param>
        /// <returns></returns>
        Task<(List<WorkflowVersionOutputDto> items, int totalCount)> GetPagedListAsync(WorkflowVersionQueryRequest request);

        /// <summary>
        /// Get versions by change type
        /// </summary>
        /// <param name="changeType">Change type</param>
        /// <returns></returns>
        Task<List<WorkflowVersionOutputDto>> GetByChangeTypeAsync(string changeType);

        /// <summary>
        /// Restore to specified version
        /// </summary>
        /// <param name="versionId">Version ID</param>
        /// <returns></returns>
        Task<bool> RestoreVersionAsync(long versionId);
    }
} 
 
