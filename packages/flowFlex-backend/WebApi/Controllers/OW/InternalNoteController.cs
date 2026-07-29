using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.Dtos.OW.InternalNote;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.Dtos;
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
[Authorize] // Require authentication for all internal notes APIs
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
    /// <param name="input">Internal note creation data</param>
    /// <returns>Created note ID</returns>
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
    /// <param name="id">Note ID</param>
    /// <param name="input">Note update data</param>
    /// <returns>Whether update was successful</returns>
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
    /// <param name="id">Note ID</param>
    /// <returns>Whether deletion was successful</returns>
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
    /// <param name="id">Note ID</param>
    /// <returns>Internal note details</returns>
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
    /// <param name="onboardingId">Onboarding ID</param>
    /// <returns>List of internal notes for the onboarding</returns>
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
    /// <param name="onboardingId">Onboarding ID</param>
    /// <param name="stageId">Stage ID (optional)</param>
    /// <returns>List of internal notes for the onboarding and stage</returns>
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
    /// <param name="onboardingId">Onboarding ID</param>
    /// <returns>List of unresolved internal notes</returns>
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
    /// <param name="id">Note ID</param>
    /// <param name="request">Resolution details (resolved by, resolution notes)</param>
    /// <returns>Whether marking as resolved was successful</returns>
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
    /// <param name="id">Note ID</param>
    /// <returns>Whether marking as unresolved was successful</returns>
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
    /// <param name="pageIndex">Page index (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <param name="onboardingId">Filter by onboarding ID (optional)</param>
    /// <param name="stageId">Filter by stage ID (optional)</param>
    /// <param name="noteType">Filter by note type (optional)</param>
    /// <param name="priority">Filter by priority (optional)</param>
    /// <param name="isResolved">Filter by resolved status (optional)</param>
    /// <param name="sortField">Sort field (default: CreateDate)</param>
    /// <param name="sortDirection">Sort direction (default: desc)</param>
    /// <returns>Paged list of internal notes</returns>
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

