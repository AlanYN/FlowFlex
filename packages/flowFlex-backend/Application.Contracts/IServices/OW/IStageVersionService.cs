using FlowFlex.Application.Contracts.Dtos.OW.StageVersion;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Stage Version Service Interface
    /// </summary>
    public interface IStageVersionService
    {
        /// <summary>
        /// Create stage version
        /// </summary>
        /// <param name="dto">Stage version data</param>
        /// <returns></returns>
        Task<StageVersionOutputDto> CreateAsync(StageVersionInputDto dto);

        /// <summary>
        /// Update stage version
        /// </summary>
        /// <param name="id">Version ID</param>
        /// <param name="dto">Stage version data</param>
        /// <returns></returns>
        Task<StageVersionOutputDto> UpdateAsync(long id, StageVersionInputDto dto);

        /// <summary>
        /// Delete stage version
        /// </summary>
        /// <param name="id">Version ID</param>
        /// <returns></returns>
        Task<bool> DeleteAsync(long id);

        /// <summary>
        /// Get stage version by ID
        /// </summary>
        /// <param name="id">Version ID</param>
        /// <returns></returns>
        Task<StageVersionOutputDto> GetByIdAsync(long id);

        /// <summary>
        /// Get stage version list by workflow version ID
        /// </summary>
        /// <param name="workflowVersionId">Workflow version ID</param>
        /// <returns></returns>
        Task<List<StageVersionOutputDto>> GetByWorkflowVersionIdAsync(long workflowVersionId);

        /// <summary>
        /// Get version list by original stage ID
        /// </summary>
        /// <param name="originalStageId">Original stage ID</param>
        /// <returns></returns>
        Task<List<StageVersionOutputDto>> GetByOriginalStageIdAsync(long originalStageId);

        /// <summary>
        /// Get paginated list
        /// </summary>
        /// <param name="request">Query request</param>
        /// <returns></returns>
        Task<(List<StageVersionOutputDto> items, int totalCount)> GetPagedListAsync(StageVersionQueryRequest request);

        /// <summary>
        /// Batch create stage versions
        /// </summary>
        /// <param name="workflowVersionId">Workflow version ID</param>
        /// <param name="stageVersions">Stage version list</param>
        /// <returns></returns>
        Task<List<StageVersionOutputDto>> CreateBatchAsync(long workflowVersionId, List<StageVersionInputDto> stageVersions);

        /// <summary>
        /// Restore stage to specified version
        /// </summary>
        /// <param name="versionId">Version ID</param>
        /// <returns></returns>
        Task<bool> RestoreVersionAsync(long versionId);
    }
} 
 
