using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.Dtos.OW.ChecklistTask;
using FlowFlex.Application.Contracts.IServices.OW;
using Item.Internal.StandardApi.Response;
using System.Net;
using FlowFlex.Domain.Shared.Attr;

namespace FlowFlex.WebApi.Controllers.OW;

/// <summary>
/// Checklist Task Note Controller
/// </summary>
[ApiController]
[Route("ow/checklist-task-notes/v{version:apiVersion}")]
[Display(Name = "checklist-task-note")]
[Authorize] // 添加授权特性，要求所有note API都需要认证
public class ChecklistTaskNoteController : Controllers.ControllerBase
{
    private readonly IChecklistTaskNoteService _noteService;

    public ChecklistTaskNoteController(IChecklistTaskNoteService noteService)
    {
        _noteService = noteService;
    }

    /// <summary>
    /// Create a new note for a task
    /// </summary>
    [HttpPost]
    [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> CreateNote([FromBody] ChecklistTaskNoteInputDto input)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (input == null)
        {
            return BadRequest("Input parameter is null");
        }

        var noteId = await _noteService.CreateNoteAsync(input);
        return Success(noteId);
    }

    /// <summary>
    /// Update an existing note
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> UpdateNote(long id, [FromBody] ChecklistTaskNoteUpdateDto input)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (input == null)
        {
            return BadRequest("Input parameter is null");
        }

        input.Id = id; // Ensure the ID matches the route parameter
        var result = await _noteService.UpdateNoteAsync(input);
        return Success(result);
    }

    /// <summary>
    /// Delete a note
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> DeleteNote(long id)
    {
        var result = await _noteService.DeleteNoteAsync(id);
        return Success(result);
    }

    /// <summary>
    /// Get a specific note by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType<SuccessResponse<ChecklistTaskNoteOutputDto>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetNoteById(long id)
    {
        var note = await _noteService.GetNoteByIdAsync(id);
        if (note == null)
        {
            return NotFound($"Note with ID {id} not found");
        }
        return Success(note);
    }

    /// <summary>
    /// Get all notes for a specific task
    /// </summary>
    [HttpGet("task/{taskId}/onboarding/{onboardingId}")]
    [ProducesResponseType<SuccessResponse<List<ChecklistTaskNoteOutputDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetNotesByTask(long taskId, long onboardingId, [FromQuery] bool includeDeleted = false)
    {
        var notes = await _noteService.GetNotesByTaskAsync(taskId, onboardingId, includeDeleted);
        return Success(notes);
    }

    /// <summary>
    /// Get notes summary for a task
    /// </summary>
    [HttpGet("task/{taskId}/onboarding/{onboardingId}/summary")]
    [ProducesResponseType<SuccessResponse<ChecklistTaskNotesSummaryDto>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetNotesSummary(long taskId, long onboardingId)
    {
        var summary = await _noteService.GetNotesSummaryAsync(taskId, onboardingId);
        return Success(summary);
    }

    /// <summary>
    /// Pin or unpin a note
    /// </summary>
    [HttpPost("{id}/toggle-pin")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> ToggleNotePin(long id, [FromBody] ToggleNotePinRequest request)
    {
        var result = await _noteService.ToggleNotePinAsync(id, request.IsPinned);
        return Success(result);
    }

    /// <summary>
    /// Get all pinned notes for a task
    /// </summary>
    [HttpGet("task/{taskId}/onboarding/{onboardingId}/pinned")]
    [ProducesResponseType<SuccessResponse<List<ChecklistTaskNoteOutputDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetPinnedNotes(long taskId, long onboardingId)
    {
        var notes = await _noteService.GetPinnedNotesAsync(taskId, onboardingId);
        return Success(notes);
    }

    /// <summary>
    /// Search notes by content
    /// </summary>
    [HttpGet("task/{taskId}/onboarding/{onboardingId}/search")]
    [ProducesResponseType<SuccessResponse<List<ChecklistTaskNoteOutputDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> SearchNotes(long taskId, long onboardingId, [FromQuery] string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return BadRequest("Search term is required");
        }

        var notes = await _noteService.SearchNotesAsync(taskId, onboardingId, searchTerm);
        return Success(notes);
    }

    /// <summary>
    /// Get notes by priority
    /// </summary>
    [HttpGet("task/{taskId}/onboarding/{onboardingId}/priority/{priority}")]
    [ProducesResponseType<SuccessResponse<List<ChecklistTaskNoteOutputDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetNotesByPriority(long taskId, long onboardingId, string priority)
    {
        var notes = await _noteService.GetNotesByPriorityAsync(taskId, onboardingId, priority);
        return Success(notes);
    }

    /// <summary>
    /// Batch get notes summary for multiple tasks
    /// </summary>
    [HttpPost("batch/summary")]
    [ProducesResponseType<SuccessResponse<Dictionary<long, ChecklistTaskNotesSummaryDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> BatchGetNotesSummary([FromBody] BatchNotesSummaryRequest request)
    {
        if (request == null || request.TaskIds == null || request.TaskIds.Count == 0)
        {
            return BadRequest("Task IDs are required");
        }

        var summaries = await _noteService.BatchGetNotesSummaryAsync(request.TaskIds, request.OnboardingId);
        return Success(summaries);
    }
}

/// <summary>
/// Toggle note pin request
/// </summary>
public class ToggleNotePinRequest
{
    /// <summary>
    /// Whether the note should be pinned
    /// </summary>
    public bool IsPinned { get; set; }
}

/// <summary>
/// Batch notes summary request
/// </summary>
public class BatchNotesSummaryRequest
{
    /// <summary>
    /// List of task IDs
    /// </summary>
    public List<long> TaskIds { get; set; } = new List<long>();

    /// <summary>
    /// Onboarding ID
    /// </summary>
    public long OnboardingId { get; set; }
}