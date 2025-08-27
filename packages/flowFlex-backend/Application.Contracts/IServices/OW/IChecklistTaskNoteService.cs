using System.Collections.Generic;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.ChecklistTask;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW;

/// <summary>
/// ChecklistTaskNote service interface
/// </summary>
public interface IChecklistTaskNoteService : IScopedService
{
    /// <summary>
    /// Create a new note for a task
    /// </summary>
    /// <param name="input">Note input data</param>
    /// <returns>Created note ID</returns>
    Task<long> CreateNoteAsync(ChecklistTaskNoteInputDto input);

    /// <summary>
    /// Update an existing note
    /// </summary>
    /// <param name="input">Note update data</param>
    /// <returns>Update result</returns>
    Task<bool> UpdateNoteAsync(ChecklistTaskNoteUpdateDto input);

    /// <summary>
    /// Delete a note (soft delete)
    /// </summary>
    /// <param name="noteId">Note ID</param>
    /// <returns>Delete result</returns>
    Task<bool> DeleteNoteAsync(long noteId);

    /// <summary>
    /// Get a specific note by ID
    /// </summary>
    /// <param name="noteId">Note ID</param>
    /// <returns>Note details</returns>
    Task<ChecklistTaskNoteOutputDto?> GetNoteByIdAsync(long noteId);

    /// <summary>
    /// Get all notes for a specific task
    /// </summary>
    /// <param name="taskId">Task ID</param>
    /// <param name="onboardingId">Onboarding ID</param>
    /// <param name="includeDeleted">Whether to include deleted notes</param>
    /// <returns>List of notes</returns>
    Task<List<ChecklistTaskNoteOutputDto>> GetNotesByTaskAsync(long taskId, long onboardingId, bool includeDeleted = false);

    /// <summary>
    /// Get notes summary for a task
    /// </summary>
    /// <param name="taskId">Task ID</param>
    /// <param name="onboardingId">Onboarding ID</param>
    /// <returns>Notes summary including count and latest note</returns>
    Task<ChecklistTaskNotesSummaryDto> GetNotesSummaryAsync(long taskId, long onboardingId);

    /// <summary>
    /// Pin or unpin a note
    /// </summary>
    /// <param name="noteId">Note ID</param>
    /// <param name="isPinned">Pin status</param>
    /// <returns>Operation result</returns>
    Task<bool> ToggleNotePinAsync(long noteId, bool isPinned);

    /// <summary>
    /// Get all pinned notes for a task
    /// </summary>
    /// <param name="taskId">Task ID</param>
    /// <param name="onboardingId">Onboarding ID</param>
    /// <returns>List of pinned notes</returns>
    Task<List<ChecklistTaskNoteOutputDto>> GetPinnedNotesAsync(long taskId, long onboardingId);

    /// <summary>
    /// Search notes by content
    /// </summary>
    /// <param name="taskId">Task ID</param>
    /// <param name="onboardingId">Onboarding ID</param>
    /// <param name="searchTerm">Search term</param>
    /// <returns>Matching notes</returns>
    Task<List<ChecklistTaskNoteOutputDto>> SearchNotesAsync(long taskId, long onboardingId, string searchTerm);

    /// <summary>
    /// Get notes by priority
    /// </summary>
    /// <param name="taskId">Task ID</param>
    /// <param name="onboardingId">Onboarding ID</param>
    /// <param name="priority">Priority level</param>
    /// <returns>Notes with specified priority</returns>
    Task<List<ChecklistTaskNoteOutputDto>> GetNotesByPriorityAsync(long taskId, long onboardingId, string priority);

    /// <summary>
    /// Batch get notes summary for multiple tasks
    /// </summary>
    /// <param name="taskIds">List of task IDs</param>
    /// <param name="onboardingId">Onboarding ID</param>
    /// <returns>Dictionary of task ID to notes summary</returns>
    Task<Dictionary<long, ChecklistTaskNotesSummaryDto>> BatchGetNotesSummaryAsync(List<long> taskIds, long onboardingId);
}