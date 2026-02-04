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
        /// 根据工作流ID获取阶段列表
        /// </summary>
        Task<List<Stage>> GetByWorkflowIdAsync(long workflowId);

        /// <summary>
        /// 批量根据多个工作流ID获取阶段列表（优化性能）
        /// </summary>
        Task<List<Stage>> GetByWorkflowIdsAsync(List<long> workflowIds);

        /// <summary>
        /// 分页查询阶段
        /// </summary>
        Task<(List<Stage> items, int total)> QueryPagedAsync(int pageIndex, int pageSize, long? workflowId = null, string name = null);

        /// <summary>
        /// 获取工作流中的最大排序�?
        /// </summary>
        Task<int> GetMaxOrderByWorkflowIdAsync(long workflowId);

        /// <summary>
        /// 批量更新阶段排序
        /// </summary>
        Task<bool> BatchUpdateOrderAsync(List<(long stageId, int order)> stageOrders);

        /// <summary>
        /// 根据工作流ID和排序范围获取阶�?
        /// </summary>
        Task<List<Stage>> GetByWorkflowIdAndOrderRangeAsync(long workflowId, int minOrder, int maxOrder);

        /// <summary>
        /// 删除多个阶段
        /// </summary>
        Task<bool> BatchDeleteAsync(List<long> stageIds);

        /// <summary>
        /// 检查阶段名称在工作流中是否存在
        /// </summary>
        Task<bool> ExistsNameInWorkflowAsync(long workflowId, string name, long? excludeId = null);

        /// <summary>
        /// 获取阶段的下一个排序�?
        /// </summary>
        Task<int> GetNextOrderAsync(long workflowId);

        /// <summary>
        /// 根据颜色获取阶段数量
        /// </summary>
        Task<int> GetCountByColorAsync(string color);

        /// <summary>
        /// 获取工作流中激活的阶段
        /// </summary>
        Task<List<Stage>> GetActiveStagesByWorkflowIdAsync(long workflowId);

        /// <summary>
        /// 批量更新阶段状�?
        /// </summary>
        Task<bool> BatchUpdateActiveStatusAsync(List<long> stageIds, bool isActive);

        /// <summary>
        /// 获取所有阶段（优化版本，仅返回必要字段�?
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
