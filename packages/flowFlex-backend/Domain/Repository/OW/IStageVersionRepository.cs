using FlowFlex.Domain.Repository;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// Stage version snapshot repository interface
    /// </summary>
    public interface IStageVersionRepository : IBaseRepository<StageVersion>
    {
        /// <summary>
        /// Get stage snapshot list by workflow version ID
        /// </summary>
        /// <param name="workflowVersionId">Workflow version ID</param>
        /// <returns>Stage snapshot list</returns>
        Task<List<StageVersion>> GetByWorkflowVersionIdAsync(long workflowVersionId);

        /// <summary>
        /// Batch create stage version snapshots
        /// </summary>
        /// <param name="stageVersions">Stage version snapshot list</param>
        /// <returns>Whether successful</returns>
        Task<bool> BatchInsertAsync(List<StageVersion> stageVersions);

        /// <summary>
        /// Get stage snapshot by original stage ID and workflow version ID
        /// </summary>
        /// <param name="originalStageId">Original stage ID</param>
        /// <param name="workflowVersionId">Workflow version ID</param>
        /// <returns>Stage snapshot</returns>
        Task<StageVersion> GetByOriginalStageIdAsync(long originalStageId, long workflowVersionId);
    }
}
