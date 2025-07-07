using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// Stage completion log repository interface
    /// </summary>
    public interface IStageCompletionLogRepository : IBaseRepository<StageCompletionLog>
    {
        /// <summary>
        /// Get log list by Onboarding ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Log list</returns>
        Task<List<StageCompletionLog>> GetByOnboardingIdAsync(long onboardingId);

        /// <summary>
        /// Get log list by Stage ID
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Log list</returns>
        Task<List<StageCompletionLog>> GetByStageIdAsync(long stageId);

        /// <summary>
        /// Get log list by Onboarding ID and Stage ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Log list</returns>
        Task<List<StageCompletionLog>> GetByOnboardingAndStageAsync(long onboardingId, long stageId);

        /// <summary>
        /// Get log list by log type
        /// </summary>
        /// <param name="logType">Log type</param>
        /// <param name="days">Recent days</param>
        /// <returns>Log list</returns>
        Task<List<StageCompletionLog>> GetByLogTypeAsync(string logType, int days = 7);

        /// <summary>
        /// Get error log list
        /// </summary>
        /// <param name="days">Recent days</param>
        /// <returns>Error log list</returns>
        Task<List<StageCompletionLog>> GetErrorLogsAsync(int days = 7);

        /// <summary>
        /// Get log statistics
        /// </summary>
        /// <param name="onboardingId">Onboarding ID (optional)</param>
        /// <param name="days">Statistics days</param>
        /// <returns>Statistics information</returns>
        Task<Dictionary<string, object>> GetLogStatisticsAsync(long? onboardingId = null, int days = 7);

        /// <summary>
        /// Batch create logs
        /// </summary>
        /// <param name="logs">Log list</param>
        /// <returns>Creation result</returns>
        Task<bool> CreateBatchAsync(List<StageCompletionLog> logs);

        /// <summary>
        /// Clean up expired logs
        /// </summary>
        /// <param name="days">Retention days</param>
        /// <returns>Number of cleaned records</returns>
        Task<int> CleanupExpiredLogsAsync(int days = 30);

        /// <summary>
        /// Log list (pagination/conditional query)
        /// </summary>
        Task<(List<StageCompletionLog> Data, int Total)> ListAsync(long? onboardingId, long? stageId, string? logType, bool? success, DateTimeOffset? startDate, DateTimeOffset? endDate, int pageIndex, int pageSize);

        /// <summary>
        /// Delete single log
        /// </summary>
        Task<bool> DeleteAsync(long id);
    }
} 
