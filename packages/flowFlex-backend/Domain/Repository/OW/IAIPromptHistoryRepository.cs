using FlowFlex.Domain.Entities.OW;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// AI Prompt History Repository Interface
    /// </summary>
    public interface IAIPromptHistoryRepository : IBaseRepository<AIPromptHistory>
    {
        /// <summary>
        /// Get prompt history by entity
        /// </summary>
        /// <param name="entityType">Entity type</param>
        /// <param name="entityId">Entity ID</param>
        /// <param name="promptType">Prompt type (optional)</param>
        /// <param name="limit">Maximum number of records to return</param>
        /// <returns>List of prompt history records</returns>
        Task<List<AIPromptHistory>> GetByEntityAsync(string entityType, long entityId, string promptType = null, int limit = 50);

        /// <summary>
        /// Get prompt history by onboarding
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="promptType">Prompt type (optional)</param>
        /// <param name="limit">Maximum number of records to return</param>
        /// <returns>List of prompt history records</returns>
        Task<List<AIPromptHistory>> GetByOnboardingAsync(long onboardingId, string promptType = null, int limit = 50);

        /// <summary>
        /// Get prompt history by user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="promptType">Prompt type (optional)</param>
        /// <param name="limit">Maximum number of records to return</param>
        /// <returns>List of prompt history records</returns>
        Task<List<AIPromptHistory>> GetByUserAsync(long userId, string promptType = null, int limit = 50);

        /// <summary>
        /// Get recent prompt history
        /// </summary>
        /// <param name="promptType">Prompt type (optional)</param>
        /// <param name="limit">Maximum number of records to return</param>
        /// <returns>List of recent prompt history records</returns>
        Task<List<AIPromptHistory>> GetRecentAsync(string promptType = null, int limit = 100);

        /// <summary>
        /// Clean up old prompt history records
        /// </summary>
        /// <param name="daysToKeep">Number of days to keep records</param>
        /// <returns>Number of deleted records</returns>
        Task<int> CleanupOldRecordsAsync(int daysToKeep = 30);
    }
}