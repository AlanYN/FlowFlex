using FlowFlex.Domain.Entities.Action;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Domain.Repository.Action
{
    /// <summary>
    /// Action Execution repository interface
    /// </summary>
    public interface IActionExecutionRepository : IBaseRepository<ActionExecution>, IScopedService
    {
        /// <summary>
        /// Get executions by action definition ID
        /// </summary>
        /// <param name="actionDefinitionId">Action definition ID</param>
        /// <param name="days">Recent days (default: 30)</param>
        /// <returns>Execution list ordered by creation date desc</returns>
        Task<List<ActionExecution>> GetByActionDefinitionIdAsync(long actionDefinitionId, int days = 30);

        /// <summary>
        /// Get executions by execution status
        /// </summary>
        /// <param name="status">Execution status (Pending, Running, Success, Failed, Cancelled)</param>
        /// <param name="days">Recent days (default: 7)</param>
        /// <returns>Execution list</returns>
        Task<List<ActionExecution>> GetByStatusAsync(string status, int days = 7);

        /// <summary>
        /// Get execution by execution ID
        /// </summary>
        /// <param name="executionId">Execution ID</param>
        /// <returns>Execution or null</returns>
        Task<ActionExecution?> GetByExecutionIdAsync(string executionId);

        /// <summary>
        /// Get failed executions that need retry
        /// </summary>
        /// <param name="maxRetryCount">Maximum retry count</param>
        /// <returns>Failed execution list</returns>
        Task<List<ActionExecution>> GetFailedExecutionsAsync(int maxRetryCount = 3);

        /// <summary>
        /// Get executions with pagination
        /// </summary>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="actionDefinitionId">Action definition ID filter (optional)</param>
        /// <param name="status">Status filter (optional)</param>
        /// <param name="startDate">Start date filter (optional)</param>
        /// <param name="endDate">End date filter (optional)</param>
        /// <returns>Paginated executions</returns>
        Task<(List<ActionExecution> Data, int TotalCount)> GetPagedAsync(
            int pageIndex,
            int pageSize,
            long? actionDefinitionId = null,
            string? status = null,
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null);

        /// <summary>
        /// Get execution statistics by action definition
        /// </summary>
        /// <param name="actionDefinitionId">Action definition ID</param>
        /// <param name="days">Recent days (default: 30)</param>
        /// <returns>Execution statistics</returns>
        Task<(int TotalCount, int SuccessCount, int FailedCount, double AvgDurationMs)> GetExecutionStatsAsync(long actionDefinitionId, int days = 30);

        /// <summary>
        /// Get overall execution statistics
        /// </summary>
        /// <param name="days">Recent days (default: 7)</param>
        /// <returns>Overall statistics</returns>
        Task<Dictionary<string, int>> GetOverallStatsAsync(int days = 7);

        /// <summary>
        /// Clean up old execution records
        /// </summary>
        /// <param name="keepDays">Keep records for how many days</param>
        /// <returns>Number of deleted records</returns>
        Task<int> CleanupOldExecutionsAsync(int keepDays = 90);

        /// <summary>
        /// Get executions by trigger source ID with action information
        /// </summary>
        /// <param name="triggerSourceId">Trigger source ID</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="triggerContextWhere"></param>
        /// <returns>Paginated executions with action information</returns>
        Task<(List<ActionExecutionWithActionInfo> Data, int TotalCount)> GetByTriggerSourceIdWithActionInfoAsync(
            long triggerSourceId,
            int pageIndex = 1,
            int pageSize = 10,
            List<JsonQueryCondition>? jsonConditions = null);
    }
}