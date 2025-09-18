using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using FlowFlex.Application.Contracts.Dtos.OW.InternalNote;
using FlowFlex.Application.Contracts.IServices.OW;

using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Application.Services.OW.Extensions;

namespace FlowFlex.Application.Services.OW;

/// <summary>
/// InternalNote service implementation
/// </summary>
public class InternalNoteService : IInternalNoteService, IScopedService
{
    private readonly IInternalNoteRepository _noteRepository;
    private readonly IOnboardingRepository _onboardingRepository;
    private readonly IStageRepository _stageRepository;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserContext _userContext;

    public InternalNoteService(
        IInternalNoteRepository noteRepository,
        IOnboardingRepository onboardingRepository,
        IStageRepository stageRepository,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        UserContext userContext)
    {
        _noteRepository = noteRepository;
        _onboardingRepository = onboardingRepository;
        _stageRepository = stageRepository;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        _userContext = userContext;
    }

    /// <summary>
    /// Create a new internal note
    /// </summary>
    public async Task<long> CreateAsync(InternalNoteInputDto input)
    {
        // Validate onboarding exists
        var onboarding = await _onboardingRepository.GetByIdAsync(input.OnboardingId);
        if (onboarding == null || !onboarding.IsValid)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
        }

        // Validate stage exists if provided
        if (input.StageId.HasValue)
        {
            var stage = await _stageRepository.GetByIdAsync(input.StageId.Value);
            if (stage == null || !stage.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Stage not found");
            }
        }

        var entity = _mapper.Map<InternalNote>(input);

        // Set default values
        entity.IsResolved = false;
        entity.Priority = string.IsNullOrEmpty(entity.Priority) ? "Medium" : entity.Priority;
        entity.NoteType = string.IsNullOrEmpty(entity.NoteType) ? "General" : entity.NoteType;

        // Initialize create information with proper ID and timestamps
        entity.InitCreateInfo(_userContext);

        await _noteRepository.InsertAsync(entity);

        return entity.Id;
    }

    /// <summary>
    /// Update an existing internal note
    /// </summary>
    public async Task<bool> UpdateAsync(long id, InternalNoteInputDto input)
    {
        var existingNote = await _noteRepository.GetByIdAsync(id);
        if (existingNote == null || !existingNote.IsValid)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "Note not found");
        }

        // Only validate and update OnboardingId if it's provided and different from existing
        if (input.OnboardingId != 0 && input.OnboardingId != existingNote.OnboardingId)
        {
            var onboarding = await _onboardingRepository.GetByIdAsync(input.OnboardingId);
            if (onboarding == null || !onboarding.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }
            existingNote.OnboardingId = input.OnboardingId;
        }

        // Only validate and update StageId if it's provided and different from existing
        if (input.StageId != existingNote.StageId)
        {
            if (input.StageId.HasValue)
            {
                var stage = await _stageRepository.GetByIdAsync(input.StageId.Value);
                if (stage == null || !stage.IsValid)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, "Stage not found");
                }
            }
            existingNote.StageId = input.StageId;
        }

        // Update content and other properties
        if (!string.IsNullOrEmpty(input.Title))
        {
            existingNote.Title = input.Title;
        }

        if (!string.IsNullOrEmpty(input.Content))
        {
            existingNote.Content = input.Content;
        }

        if (!string.IsNullOrEmpty(input.NoteType))
        {
            existingNote.NoteType = input.NoteType;
        }

        if (!string.IsNullOrEmpty(input.Priority))
        {
            existingNote.Priority = input.Priority;
        }

        existingNote.InitUpdateInfo(_userContext);

        return await _noteRepository.UpdateAsync(existingNote);
    }

    /// <summary>
    /// Update an existing internal note with partial data
    /// </summary>
    public async Task<bool> UpdateAsync(long id, InternalNoteUpdateDto input)
    {
        var existingNote = await _noteRepository.GetByIdAsync(id);
        if (existingNote == null || !existingNote.IsValid)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "Note not found");
        }

        // Only validate and update OnboardingId if it's provided and different from existing
        if (input.OnboardingId.HasValue && input.OnboardingId.Value != existingNote.OnboardingId)
        {
            var onboarding = await _onboardingRepository.GetByIdAsync(input.OnboardingId.Value);
            if (onboarding == null || !onboarding.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }
            existingNote.OnboardingId = input.OnboardingId.Value;
        }

        // Important: StageId is fixed and should not be modified in update operations
        // StageId is set during creation and should not be changed afterwards
        // If you need to modify StageId, use a dedicated interface

        // Update content and other properties only if provided
        // Use != null instead of !string.IsNullOrEmpty to distinguish between "not provided" and "provided empty value"
        if (input.Title != null)
        {
            existingNote.Title = input.Title;
        }

        if (input.Content != null)
        {
            existingNote.Content = input.Content;
        }

        if (input.NoteType != null)
        {
            existingNote.NoteType = input.NoteType;
        }

        if (input.Priority != null)
        {
            existingNote.Priority = input.Priority;
        }

        existingNote.InitUpdateInfo(_userContext);

        // Explicitly specify columns to update, excluding StageId to prevent accidental modification
        return await _noteRepository.UpdateAsync(existingNote, x => new
        {
            x.OnboardingId,
            x.Title,
            x.Content,
            x.NoteType,
            x.Priority,
            x.ModifyDate,
            x.ModifyBy,
            x.ModifyUserId
        });
    }

    /// <summary>
    /// Delete an internal note
    /// </summary>
    public async Task<bool> DeleteAsync(long id)
    {
        var note = await _noteRepository.GetByIdAsync(id);
        if (note == null || !note.IsValid)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "Note not found");
        }

        // Soft delete
        note.IsValid = false;
        note.InitUpdateInfo(_userContext);

        // Explicitly specify columns to update, excluding StageId to prevent accidental modification
        return await _noteRepository.UpdateAsync(note, x => new
        {
            x.IsValid,
            x.ModifyDate,
            x.ModifyBy,
            x.ModifyUserId
        });
    }

    /// <summary>
    /// Get internal note by ID
    /// </summary>
    public async Task<InternalNoteOutputDto> GetByIdAsync(long id)
    {
        var note = await _noteRepository.GetByIdAsync(id);
        if (note == null || !note.IsValid)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "Note not found");
        }

        var result = _mapper.Map<InternalNoteOutputDto>(note);

        // Get stage name if stage ID is provided
        if (note.StageId.HasValue)
        {
            var stage = await _stageRepository.GetByIdAsync(note.StageId.Value);
            result.StageName = stage?.Name ?? "Unknown Stage";
        }

        return result;
    }

    /// <summary>
    /// Get notes by onboarding ID
    /// </summary>
    public async Task<List<InternalNoteOutputDto>> GetByOnboardingIdAsync(long onboardingId)
    {
        var notes = await _noteRepository.GetByOnboardingIdAsync(onboardingId);
        var result = _mapper.Map<List<InternalNoteOutputDto>>(notes);

        // Populate stage names
        await PopulateStageNamesAsync(result);

        return result;
    }

    /// <summary>
    /// Get notes by onboarding and stage
    /// </summary>
    public async Task<List<InternalNoteOutputDto>> GetByOnboardingAndStageAsync(long onboardingId, long? stageId)
    {
        var notes = await _noteRepository.GetByOnboardingAndStageAsync(onboardingId, stageId);
        var result = _mapper.Map<List<InternalNoteOutputDto>>(notes);

        // Populate stage names
        await PopulateStageNamesAsync(result);

        return result;
    }

    /// <summary>
    /// Get unresolved notes
    /// </summary>
    public async Task<List<InternalNoteOutputDto>> GetUnresolvedNotesAsync(long onboardingId)
    {
        var notes = await _noteRepository.GetUnresolvedNotesAsync(onboardingId);
        var result = _mapper.Map<List<InternalNoteOutputDto>>(notes);

        // Populate stage names
        await PopulateStageNamesAsync(result);

        return result;
    }

    /// <summary>
    /// Mark note as resolved
    /// </summary>
    public async Task<bool> MarkAsResolvedAsync(long id, string resolvedBy, string resolutionNotes = "")
    {
        var note = await _noteRepository.GetByIdAsync(id);
        if (note == null || !note.IsValid)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "Note not found");
        }

        note.IsResolved = true;
        note.ResolvedTime = DateTimeOffset.UtcNow;
        note.ResolvedBy = resolvedBy;
        note.ResolutionNotes = resolutionNotes;
        note.InitUpdateInfo(_userContext);

        // Explicitly specify columns to update, excluding StageId to prevent accidental modification
        return await _noteRepository.UpdateAsync(note, x => new
        {
            x.IsResolved,
            x.ResolvedTime,
            x.ResolvedBy,
            x.ResolutionNotes,
            x.ModifyDate,
            x.ModifyBy,
            x.ModifyUserId
        });
    }

    /// <summary>
    /// Mark note as unresolved
    /// </summary>
    public async Task<bool> MarkAsUnresolvedAsync(long id)
    {
        var note = await _noteRepository.GetByIdAsync(id);
        if (note == null || !note.IsValid)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "Note not found");
        }

        note.IsResolved = false;
        note.ResolvedTime = null;
        note.ResolvedBy = string.Empty;
        note.ResolutionNotes = string.Empty;
        note.ModifyDate = DateTimeOffset.UtcNow;
        note.ModifyBy = GetCurrentUser();

        // Explicitly specify columns to update, excluding StageId to prevent accidental modification
        return await _noteRepository.UpdateAsync(note, x => new
        {
            x.IsResolved,
            x.ResolvedTime,
            x.ResolvedBy,
            x.ResolutionNotes,
            x.ModifyDate,
            x.ModifyBy,
            x.ModifyUserId
        });
    }

    /// <summary>
    /// Get notes with pagination
    /// </summary>
    public async Task<PageModelDto<InternalNoteOutputDto>> GetPagedAsync(
        int pageIndex,
        int pageSize,
        long? onboardingId = null,
        long? stageId = null,
        string noteType = null,
        string priority = null,
        bool? isResolved = null,
        string sortField = "CreateDate",
        string sortDirection = "desc")
    {
        var (items, totalCount) = await _noteRepository.GetPagedAsync(
            pageIndex, pageSize, onboardingId, stageId, noteType, priority, isResolved, sortField, sortDirection);

        var result = _mapper.Map<List<InternalNoteOutputDto>>(items);

        // Populate stage names
        await PopulateStageNamesAsync(result);

        return new PageModelDto<InternalNoteOutputDto>(pageIndex, pageSize, result, totalCount);
    }

    /// <summary>
    /// Set request information
    /// </summary>
    private void SetRequestInfo(InternalNote note)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            // Set author from user context or default
            note.Author = GetCurrentUser();
        }

        note.CreateBy = GetCurrentUser();
        note.ModifyBy = GetCurrentUser();
        note.Source = "customer_portal";
    }

    /// <summary>
    /// Get current user name
    /// </summary>
    private string GetCurrentUser()
    {
        return !string.IsNullOrEmpty(_userContext?.UserName) ? _userContext.UserName : "System";
    }

    /// <summary>
    /// Populate stage names for notes
    /// </summary>
    private async Task PopulateStageNamesAsync(List<InternalNoteOutputDto> notes)
    {
        var stageIds = notes.Where(n => n.StageId.HasValue).Select(n => n.StageId.Value).Distinct().ToList();

        if (stageIds.Any())
        {
            var stages = new Dictionary<long, string>();

            foreach (var stageId in stageIds)
            {
                var stage = await _stageRepository.GetByIdAsync(stageId);
                if (stage != null)
                {
                    stages[stageId] = stage.Name;
                }
            }

            foreach (var note in notes.Where(n => n.StageId.HasValue))
            {
                if (stages.TryGetValue(note.StageId.Value, out var stageName))
                {
                    note.StageName = stageName;
                }
            }
        }
    }
}