using System.Collections.Generic;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.StageCompletion;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Stage Completion Log Service Interface
    /// </summary>
    public interface IStageCompletionLogService : IScopedService
    {
        /// <summary>
        /// Create completion log
        /// </summary>
        /// <param name="input">Input parameters</param>
        /// <returns>Whether successful</returns>
        Task<bool> CreateLogAsync(StageCompletionLogInputDto input);

        /// <summary>
        /// Get log list by Onboarding ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Log list</returns>
        Task<List<StageCompletionLogOutputDto>> GetByOnboardingIdAsync(long onboardingId);

        /// <summary>
        /// Get log list by Stage ID
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Log list</returns>
        Task<List<StageCompletionLogOutputDto>> GetByStageIdAsync(long stageId);

        /// <summary>
        /// Get log list by Onboarding ID and Stage ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Log list</returns>
        Task<List<StageCompletionLogOutputDto>> GetByOnboardingAndStageAsync(long onboardingId, long stageId);

        /// <summary>
        /// Get log list by log type
        /// </summary>
        /// <param name="logType">Log type</param>
        /// <param name="days">Recent days</param>
        /// <returns>Log list</returns>
        Task<List<StageCompletionLogOutputDto>> GetByLogTypeAsync(string logType, int days = 7);

        /// <summary>
        /// Get error log list
        /// </summary>
        /// <param name="days">Recent days</param>
        /// <returns>Error log list</returns>
        Task<List<StageCompletionLogOutputDto>> GetErrorLogsAsync(int days = 7);

        /// <summary>
        /// Get log statistics information
        /// </summary>
        /// <param name="onboardingId">Onboarding ID (optional)</param>
        /// <param name="days">Statistics days</param>
        /// <returns>Statistics information</returns>
        Task<Dictionary<string, object>> GetLogStatisticsAsync(long? onboardingId = null, int days = 7);

        /// <summary>
        /// Batch create logs
        /// </summary>
        /// <param name="inputs">Log input list</param>
        /// <returns>Whether successful</returns>
        Task<bool> BatchCreateAsync(List<StageCompletionLogInputDto> inputs);

        /// <summary>
        /// Clean up expired logs
        /// </summary>
        /// <param name="days">Retention days</param>
        /// <returns>Number of cleaned records</returns>
        Task<int> CleanupExpiredLogsAsync(int days = 30);

        /// <summary>
        /// Log list (paginated conditional query)
        /// </summary>
        Task<PageModelDto<StageCompletionLogOutputDto>> ListAsync(StageCompletionLogQueryRequest request);

        /// <summary>
        /// Delete single log
        /// </summary>
        Task<bool> DeleteAsync(long id);
    }
}
