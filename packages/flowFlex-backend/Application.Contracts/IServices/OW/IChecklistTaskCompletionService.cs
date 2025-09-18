using System.Collections.Generic;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Checklist;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW;

/// <summary>
/// ChecklistTaskCompletion service interface
/// </summary>
public interface IChecklistTaskCompletionService : IScopedService
{
    /// <summary>
    /// Get all task completions
    /// </summary>
    Task<List<ChecklistTaskCompletionOutputDto>> GetAllTaskCompletionsAsync();

    /// <summary>
    /// Save task completion
    /// </summary>
    Task<bool> SaveTaskCompletionAsync(ChecklistTaskCompletionInputDto input);

    /// <summary>
    /// Batch save task completions
    /// </summary>
    Task<bool> BatchSaveTaskCompletionsAsync(List<ChecklistTaskCompletionInputDto> inputs);

    /// <summary>
    /// Get task completions by lead and checklist
    /// </summary>
    Task<List<ChecklistTaskCompletionOutputDto>> GetByLeadAndChecklistAsync(string leadId, long checklistId);

    /// <summary>
    /// Get task completions by onboarding and checklist
    /// </summary>
    Task<List<ChecklistTaskCompletionOutputDto>> GetByOnboardingAndChecklistAsync(long onboardingId, long checklistId);

    /// <summary>
    /// Get task completions by onboarding and stage
    /// </summary>
    Task<List<ChecklistTaskCompletionOutputDto>> GetByOnboardingAndStageAsync(long onboardingId, long stageId);

    /// <summary>
    /// Get completion statistics
    /// </summary>
    Task<(int totalTasks, int completedTasks, decimal completionRate)> GetCompletionStatsAsync(long onboardingId, long checklistId);

    /// <summary>
    /// Toggle task completion
    /// </summary>
    Task<bool> ToggleTaskCompletionAsync(long onboardingId, long taskId, bool isCompleted, string completionNotes = "", string filesJson = "[]");

    /// <summary>
    /// Process checklist component actions and publish action trigger events for completed tasks
    /// </summary>
    Task<ChecklistActionProcessingResultDto> ProcessChecklistComponentActionsAsync(ProcessChecklistActionsRequestDto request);
}
