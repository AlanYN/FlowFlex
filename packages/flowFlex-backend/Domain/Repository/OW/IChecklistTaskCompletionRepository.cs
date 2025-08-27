using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// ChecklistTaskCompletion repository interface
    /// </summary>
    public interface IChecklistTaskCompletionRepository : IBaseRepository<ChecklistTaskCompletion>
    {
        /// <summary>
        /// Get task completion by lead and checklist
        /// </summary>
        Task<List<ChecklistTaskCompletion>> GetByLeadAndChecklistAsync(string leadId, long checklistId);

        /// <summary>
        /// Get task completion by onboarding and checklist
        /// </summary>
        Task<List<ChecklistTaskCompletion>> GetByOnboardingAndChecklistAsync(long onboardingId, long checklistId);

        /// <summary>
        /// Get task completion by onboarding and stage
        /// </summary>
        Task<List<ChecklistTaskCompletion>> GetByOnboardingAndStageAsync(long onboardingId, long stageId);

        /// <summary>
        /// Get specific task completion
        /// </summary>
        Task<ChecklistTaskCompletion?> GetTaskCompletionAsync(long onboardingId, long taskId);

        /// <summary>
        /// Save or update task completion
        /// </summary>
        Task<bool> SaveTaskCompletionAsync(ChecklistTaskCompletion completion);

        /// <summary>
        /// Batch save task completions
        /// </summary>
        Task<bool> BatchSaveTaskCompletionsAsync(List<ChecklistTaskCompletion> completions);

        /// <summary>
        /// Get completion statistics for checklist
        /// </summary>
        Task<(int totalTasks, int completedTasks)> GetCompletionStatsAsync(long onboardingId, long checklistId);

        /// <summary>
        /// Get task completions by task IDs
        /// </summary>
        Task<List<ChecklistTaskCompletion>> GetByTaskIdsAsync(List<long> taskIds);

        /// <summary>
        /// Update only completion status without modifying other data
        /// </summary>
        Task<bool> UpdateCompletionStatusOnlyAsync(long onboardingId, long taskId, bool isCompleted, string modifyBy, long? modifyUserId = null);
    }
}
