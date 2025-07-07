using System.Collections.Generic;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.InternalNote;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.IServices.OW;

/// <summary>
/// InternalNote service interface
/// </summary>
public interface IInternalNoteService : IScopedService
{
    /// <summary>
    /// Create a new internal note
    /// </summary>
    Task<long> CreateAsync(InternalNoteInputDto input);

    /// <summary>
    /// Update an existing internal note
    /// </summary>
    Task<bool> UpdateAsync(long id, InternalNoteInputDto input);

    /// <summary>
    /// Update an existing internal note with partial data
    /// </summary>
    Task<bool> UpdateAsync(long id, InternalNoteUpdateDto input);

    /// <summary>
    /// Delete an internal note
    /// </summary>
    Task<bool> DeleteAsync(long id);

    /// <summary>
    /// Get internal note by ID
    /// </summary>
    Task<InternalNoteOutputDto> GetByIdAsync(long id);

    /// <summary>
    /// Get notes by onboarding ID
    /// </summary>
    Task<List<InternalNoteOutputDto>> GetByOnboardingIdAsync(long onboardingId);

    /// <summary>
    /// Get notes by onboarding and stage
    /// </summary>
    Task<List<InternalNoteOutputDto>> GetByOnboardingAndStageAsync(long onboardingId, long? stageId);

    /// <summary>
    /// Get unresolved notes
    /// </summary>
    Task<List<InternalNoteOutputDto>> GetUnresolvedNotesAsync(long onboardingId);

    /// <summary>
    /// Mark note as resolved
    /// </summary>
    Task<bool> MarkAsResolvedAsync(long id, string resolvedBy, string resolutionNotes = "");

    /// <summary>
    /// Mark note as unresolved
    /// </summary>
    Task<bool> MarkAsUnresolvedAsync(long id);

    /// <summary>
    /// Get notes with pagination
    /// </summary>
    Task<PageModelDto<InternalNoteOutputDto>> GetPagedAsync(
        int pageIndex,
        int pageSize,
        long? onboardingId = null,
        long? stageId = null,
        string noteType = null,
        string priority = null,
        bool? isResolved = null,
        string sortField = "CreateDate",
        string sortDirection = "desc");
}
