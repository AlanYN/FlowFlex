using FlowFlex.Application.Contracts.Dtos.OW.OnboardingFile;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// File import task service interface
    /// Manages file import tasks with progress tracking and cancellation support
    /// </summary>
    public interface IFileImportTaskService : ISingletonService
    {
        /// <summary>
        /// Start a new import task
        /// Returns immediately with task ID, import runs in background
        /// </summary>
        /// <param name="input">Import input containing file URLs</param>
        /// <param name="createdBy">User who created the task</param>
        /// <returns>Start import response with task ID</returns>
        Task<StartImportTaskResponseDto> StartImportTaskAsync(ImportFilesFromUrlInputDto input, string createdBy);

        /// <summary>
        /// Get import task progress by task ID
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <returns>Task progress details</returns>
        Task<FileImportTaskDto> GetTaskProgressAsync(string taskId);

        /// <summary>
        /// Get import tasks by onboarding ID and stage ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>List of import tasks for the specified onboarding and stage</returns>
        Task<List<FileImportTaskDto>> GetTasksByOnboardingAndStageAsync(long onboardingId, long stageId);

        /// <summary>
        /// Cancel a specific file import item
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="itemId">Item ID to cancel</param>
        /// <returns>Cancel response</returns>
        Task<CancelImportFileResponseDto> CancelImportItemAsync(string taskId, string itemId);

        /// <summary>
        /// Cancel all pending items in a task
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <returns>Cancel response</returns>
        Task<CancelImportFileResponseDto> CancelAllPendingItemsAsync(string taskId);

        /// <summary>
        /// Clean up completed tasks older than specified time
        /// </summary>
        /// <param name="olderThan">Time span for cleanup threshold</param>
        void CleanupOldTasks(TimeSpan olderThan);
    }
}

