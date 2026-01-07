using FlowFlex.Domain.Entities.OW;
using SqlSugar;
using FlowFlex.Domain.Repository;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// Onboarding repository interface
    /// </summary>
    public interface IOnboardingRepository : IBaseRepository<Onboarding>
    {
        /// <summary>
        /// Ensure the onboarding table exists, create if necessary
        /// </summary>
        Task EnsureTableExistsAsync();

        /// <summary>
        /// Get SqlSugar client for direct testing purposes
        /// </summary>
        ISqlSugarClient GetSqlSugarClient();



        /// <summary>
        /// Get onboarding list by lead IDs (batch operation)
        /// </summary>
        Task<List<Onboarding>> GetByLeadIdsAsync(List<string> leadIds);

        /// <summary>
        /// Get onboarding list by workflow ID
        /// </summary>
        Task<List<Onboarding>> GetListByWorkflowIdAsync(long workflowId);

        /// <summary>
        /// Get onboarding list by stage ID
        /// </summary>
        Task<List<Onboarding>> GetListByStageIdAsync(long stageId);

        /// <summary>
        /// Get onboarding list by status
        /// </summary>
        Task<List<Onboarding>> GetListByStatusAsync(string status);

        /// <summary>
        /// Get onboarding list by assignee ID
        /// </summary>
        Task<List<Onboarding>> GetListByAssigneeIdAsync(long assigneeId);

        /// <summary>
        /// Update stage for onboarding
        /// </summary>
        Task<bool> UpdateStageAsync(long id, long stageId, int stageOrder);

        /// <summary>
        /// Update completion rate
        /// </summary>
        Task<bool> UpdateCompletionRateAsync(long id, decimal completionRate);

        /// <summary>
        /// Update status
        /// </summary>
        Task<bool> UpdateStatusAsync(long id, string status);

        /// <summary>
        /// Get onboarding statistics
        /// </summary>
        Task<Dictionary<string, object>> GetStatisticsAsync();

        /// <summary>
        /// Batch update status
        /// </summary>
        Task<bool> BatchUpdateStatusAsync(List<long> ids, string status);

        /// <summary>
        /// Get overdue onboarding list
        /// </summary>
        Task<List<Onboarding>> GetOverdueListAsync();

        /// <summary>
        /// Get onboarding count by status
        /// </summary>
        Task<Dictionary<string, int>> GetCountByStatusAsync();

        /// <summary>
        /// 直接查询，使用显式过滤条件
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="appCode">应用代码</param>
        /// <returns>入职流程列表</returns>
        Task<List<Onboarding>> GetListWithExplicitFiltersAsync(string tenantId, string appCode);

        /// <summary>
        /// Get onboarding by ID without tenant isolation
        /// Used for background tasks where HttpContext is not available (e.g., AI Summary updates)
        /// </summary>
        /// <param name="id">Onboarding ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Onboarding entity or null</returns>
        Task<Onboarding> GetByIdWithoutTenantFilterAsync(long id, CancellationToken cancellationToken = default);

        #region Dashboard Methods

        /// <summary>
        /// Get recently completed cases for achievements
        /// </summary>
        /// <param name="limit">Maximum number of cases to return</param>
        /// <param name="team">Optional team filter</param>
        /// <returns>List of recently completed onboarding cases</returns>
        Task<List<Onboarding>> GetRecentlyCompletedAsync(int limit, string? team = null);

        #endregion
    }
}
