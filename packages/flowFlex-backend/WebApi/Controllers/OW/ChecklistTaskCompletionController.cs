using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.Dtos.OW.Checklist;
using FlowFlex.Application.Contracts.IServices.OW;

using Item.Internal.StandardApi.Response;
using System.Net;
using FlowFlex.Domain.Shared.Attr;

namespace FlowFlex.WebApi.Controllers.OW;

/// <summary>
/// Checklist Task Completion Controller
/// </summary>

[ApiController]

[Route("ow/checklist-task-completions/v{version:apiVersion}")]
[Display(Name = "checklist-task-completion")]

public class ChecklistTaskCompletionController : Controllers.ControllerBase
{
    private readonly IChecklistTaskCompletionService _completionService;

    public ChecklistTaskCompletionController(IChecklistTaskCompletionService completionService)
    {
        _completionService = completionService;
    }

    /// <summary>
    /// Get all task completions or filter by onboardingId and stageId
    /// </summary>
    [HttpGet]
    [ProducesResponseType<SuccessResponse<List<ChecklistTaskCompletionOutputDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetTaskCompletions([FromQuery] long? onboardingId = null, [FromQuery] long? stageId = null)
    {
        // If both onboardingId and stageId are provided, filter by both
        if (onboardingId.HasValue && stageId.HasValue)
        {
            var result = await _completionService.GetByOnboardingAndStageAsync(onboardingId.Value, stageId.Value);
            return Success(result);
        }
        
        // Otherwise, return all task completions
        var allResults = await _completionService.GetAllTaskCompletionsAsync();
        return Success(allResults);
    }

    /// <summary>
    /// Save task completion
    /// </summary>
    [HttpPost]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> SaveTaskCompletion([FromBody] ChecklistTaskCompletionInputDto input)
    {
        var result = await _completionService.SaveTaskCompletionAsync(input);
        return Success(result);
    }

    /// <summary>
    /// Batch save task completions
    /// </summary>
    [HttpPost("batch")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> BatchSaveTaskCompletions([FromBody] List<ChecklistTaskCompletionInputDto> inputs)
    {
        var result = await _completionService.BatchSaveTaskCompletionsAsync(inputs);
        return Success(result);
    }

    /// <summary>
    /// Get task completions by lead and checklist
    /// </summary>
    [HttpGet("lead/{leadId}/checklist/{checklistId}")]
    [ProducesResponseType<SuccessResponse<List<ChecklistTaskCompletionOutputDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetByLeadAndChecklist(string leadId, long checklistId)
    {
        var result = await _completionService.GetByLeadAndChecklistAsync(leadId, checklistId);
        return Success(result);
    }

    /// <summary>
    /// Get task completions by onboarding and checklist
    /// </summary>
    [HttpGet("onboarding/{onboardingId}/checklist/{checklistId}")]
    [ProducesResponseType<SuccessResponse<List<ChecklistTaskCompletionOutputDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetByOnboardingAndChecklist(long onboardingId, long checklistId)
    {
        var result = await _completionService.GetByOnboardingAndChecklistAsync(onboardingId, checklistId);
        return Success(result);
    }

    /// <summary>
    /// Get completion statistics
    /// </summary>
    [HttpGet("onboarding/{onboardingId}/checklist/{checklistId}/stats")]
    [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetCompletionStats(long onboardingId, long checklistId)
    {
        var (totalTasks, completedTasks, completionRate) = await _completionService.GetCompletionStatsAsync(onboardingId, checklistId);

        var result = new
        {
            TotalTasks = totalTasks,
            CompletedTasks = completedTasks,
            CompletionRate = completionRate
        };

        return Success(result);
    }

    /// <summary>
    /// Toggle task completion
    /// </summary>
    [HttpPost("onboarding/{onboardingId}/task/{taskId}/toggle")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> ToggleTaskCompletion(long onboardingId, long taskId, [FromBody] ToggleTaskCompletionRequest request)
    {
        var result = await _completionService.ToggleTaskCompletionAsync(onboardingId, taskId, request.IsCompleted, request.CompletionNotes);
        return Success(result);
    }
}

/// <summary>
/// Toggle task completion request
/// </summary>
public class ToggleTaskCompletionRequest
{
    /// <summary>
    /// Whether the task is completed
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Completion notes
    /// </summary>
    public string CompletionNotes { get; set; } = string.Empty;
}

