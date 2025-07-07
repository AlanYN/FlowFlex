using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.Dtos.OW.InternalNote;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Application.Contracts.Models;
using Item.Internal.StandardApi.Response;

using FlowFlex.Domain.Shared.Attr;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.WebApi.Controllers.OW;

/// <summary>
/// Internal Note Controller
/// </summary>

[ApiController]

[Route("ow/internal-notes/v{version:apiVersion}")]
[Display(Name = "internal-notes")]

public class InternalNoteController : Controllers.ControllerBase
{
    private readonly IInternalNoteService _noteService;

    public InternalNoteController(IInternalNoteService noteService)
    {
        _noteService = noteService;
    }

    /// <summary>
    /// Create a new internal note
    /// </summary>
    [HttpPost]
    [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> CreateAsync([FromBody] InternalNoteInputDto input)
    {
        var id = await _noteService.CreateAsync(input);
        return Success(id);
    }

    /// <summary>
    /// Update an existing internal note
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> UpdateAsync(long id, [FromBody] InternalNoteUpdateDto input)
    {
        var result = await _noteService.UpdateAsync(id, input);
        return Success(result);
    }

    /// <summary>
    /// Delete an internal note
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> DeleteAsync(long id)
    {
        var result = await _noteService.DeleteAsync(id);
        return Success(result);
    }

    /// <summary>
    /// Get internal note by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType<SuccessResponse<InternalNoteOutputDto>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetByIdAsync(long id)
    {
        var result = await _noteService.GetByIdAsync(id);
        return Success(result);
    }

    /// <summary>
    /// Get notes by onboarding ID
    /// </summary>
    [HttpGet("onboarding/{onboardingId}")]
    [ProducesResponseType<SuccessResponse<List<InternalNoteOutputDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetByOnboardingIdAsync(long onboardingId)
    {
        var result = await _noteService.GetByOnboardingIdAsync(onboardingId);
        return Success(result);
    }

    /// <summary>
    /// Get notes by onboarding and stage
    /// </summary>
    [HttpGet("onboarding/{onboardingId}/stage/{stageId?}")]
    [ProducesResponseType<SuccessResponse<List<InternalNoteOutputDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetByOnboardingAndStageAsync(long onboardingId, long? stageId = null)
    {
        var result = await _noteService.GetByOnboardingAndStageAsync(onboardingId, stageId);
        return Success(result);
    }

    /// <summary>
    /// Get unresolved notes
    /// </summary>
    [HttpGet("onboarding/{onboardingId}/unresolved")]
    [ProducesResponseType<SuccessResponse<List<InternalNoteOutputDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetUnresolvedNotesAsync(long onboardingId)
    {
        var result = await _noteService.GetUnresolvedNotesAsync(onboardingId);
        return Success(result);
    }

    /// <summary>
    /// Mark note as resolved
    /// </summary>
    [HttpPost("{id}/resolve")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> MarkAsResolvedAsync(long id, [FromBody] ResolveNoteRequest request)
    {
        var result = await _noteService.MarkAsResolvedAsync(id, request.ResolvedBy, request.ResolutionNotes);
        return Success(result);
    }

    /// <summary>
    /// Mark note as unresolved
    /// </summary>
    [HttpPost("{id}/unresolve")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> MarkAsUnresolvedAsync(long id)
    {
        var result = await _noteService.MarkAsUnresolvedAsync(id);
        return Success(result);
    }

    /// <summary>
    /// Get notes with pagination
    /// </summary>
    [HttpGet("paged")]
    [ProducesResponseType<SuccessResponse<PageModelDto<InternalNoteOutputDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetPagedAsync(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] long? onboardingId = null,
        [FromQuery] long? stageId = null,
        [FromQuery] string noteType = null,
        [FromQuery] string priority = null,
        [FromQuery] bool? isResolved = null,
        [FromQuery] string sortField = "CreateDate",
        [FromQuery] string sortDirection = "desc")
    {
        var result = await _noteService.GetPagedAsync(
            pageIndex, pageSize, onboardingId, stageId, noteType, priority, isResolved, sortField, sortDirection);
        return Success(result);
    }
}

/// <summary>
/// Resolve note request model
/// </summary>
public class ResolveNoteRequest
{
    /// <summary>
    /// Resolved by
    /// </summary>
    public string ResolvedBy { get; set; } = string.Empty;

    /// <summary>
    /// Resolution notes
    /// </summary>
    public string ResolutionNotes { get; set; } = string.Empty;
}

