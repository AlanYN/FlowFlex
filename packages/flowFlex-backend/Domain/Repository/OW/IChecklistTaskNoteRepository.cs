using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW;

/// <summary>
/// ChecklistTaskNote repository interface
/// </summary>
public interface IChecklistTaskNoteRepository : IBaseRepository<ChecklistTaskNote>
{
    /// <summary>
    /// Get notes by task ID and onboarding ID
    /// </summary>
    Task<List<ChecklistTaskNote>> GetByTaskAndOnboardingAsync(long taskId, long onboardingId, bool includeDeleted = false);

    /// <summary>
    /// Get pinned notes by task ID and onboarding ID
    /// </summary>
    Task<List<ChecklistTaskNote>> GetPinnedNotesAsync(long taskId, long onboardingId);

    /// <summary>
    /// Search notes by content
    /// </summary>
    Task<List<ChecklistTaskNote>> SearchByContentAsync(long taskId, long onboardingId, string searchTerm);

    /// <summary>
    /// Get notes by priority
    /// </summary>
    Task<List<ChecklistTaskNote>> GetByPriorityAsync(long taskId, long onboardingId, string priority);

    /// <summary>
    /// Get latest note for a task
    /// </summary>
    Task<ChecklistTaskNote?> GetLatestNoteAsync(long taskId, long onboardingId);

    /// <summary>
    /// Count notes for a task
    /// </summary>
    Task<int> CountNotesAsync(long taskId, long onboardingId, bool includeDeleted = false);

    /// <summary>
    /// Count pinned notes for a task
    /// </summary>
    Task<int> CountPinnedNotesAsync(long taskId, long onboardingId);

    /// <summary>
    /// Batch get notes for multiple tasks
    /// </summary>
    Task<Dictionary<long, List<ChecklistTaskNote>>> BatchGetNotesByTasksAsync(List<long> taskIds, long onboardingId);
}