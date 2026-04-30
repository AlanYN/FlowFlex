using FlowFlex.Domain.Repository;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// Stage repository interface
    /// </summary>
    public interface IStageRepository : IBaseRepository<Stage>
    {
        /// <summary>
        /// Get stages by workflow ID
        /// </summary>
        Task<List<Stage>> GetByWorkflowIdAsync(long workflowId);

        /// <summary>
        /// Batch get stages by multiple workflow IDs (performance optimization)
        /// </summary>
        Task<List<Stage>> GetByWorkflowIdsAsync(List<long> workflowIds);

        /// <summary>
        /// Query stages with pagination
        /// </summary>
        Task<(List<Stage> items, int total)> QueryPagedAsync(int pageIndex, int pageSize, long? workflowId = null, string name = null);

        /// <summary>
        /// Get max order value in workflow
        /// </summary>
        Task<int> GetMaxOrderByWorkflowIdAsync(long workflowId);

        /// <summary>
        /// Batch update stage order
        /// </summary>
        Task<bool> BatchUpdateOrderAsync(List<(long stageId, int order)> stageOrders);

        /// <summary>
        /// Get stages by workflow ID and order range
        /// </summary>
        Task<List<Stage>> GetByWorkflowIdAndOrderRangeAsync(long workflowId, int minOrder, int maxOrder);

        /// <summary>
        /// Batch delete stages
        /// </summary>
        Task<bool> BatchDeleteAsync(List<long> stageIds);

        /// <summary>
        /// Check if stage name exists in workflow
        /// </summary>
        Task<bool> ExistsNameInWorkflowAsync(long workflowId, string name, long? excludeId = null);

        /// <summary>
        /// Get next order value for stage
        /// </summary>
        Task<int> GetNextOrderAsync(long workflowId);

        /// <summary>
        /// Get stage count by color
        /// </summary>
        Task<int> GetCountByColorAsync(string color);

        /// <summary>
        /// Get active stages in workflow
        /// </summary>
        Task<List<Stage>> GetActiveStagesByWorkflowIdAsync(long workflowId);

        /// <summary>
        /// Batch update stage active status
        /// </summary>
        Task<bool> BatchUpdateActiveStatusAsync(List<long> stageIds, bool isActive);

        /// <summary>
        /// Get all stages (optimized version, returns only necessary fields)
        /// </summary>
        Task<List<Stage>> GetAllOptimizedAsync();

        /// <summary>
        /// Check if stage name exists in workflow (renamed method)
        /// </summary>
        Task<bool> IsNameExistsInWorkflowAsync(long workflowId, string name, long? excludeId = null);

        /// <summary>
        /// Batch get stages by IDs (performance optimization)
        /// </summary>
        Task<List<Stage>> GetByIdsAsync(List<long> stageIds);
    }
}
