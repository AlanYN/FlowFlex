using AutoMapper;
using Microsoft.AspNetCore.Http;
using FlowFlex.Application.Contracts.Dtos.OW.ChecklistTask;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Services.OW;

/// <summary>
/// ChecklistTaskNote service implementation
/// </summary>
public class ChecklistTaskNoteService : IChecklistTaskNoteService, IScopedService
{
    private readonly IChecklistTaskNoteRepository _noteRepository;
    private readonly IChecklistTaskRepository _taskRepository;
    private readonly IOnboardingRepository _onboardingRepository;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserContext _userContext;
    private readonly IOperatorContextService _operatorContextService;

    public ChecklistTaskNoteService(
        IChecklistTaskNoteRepository noteRepository,
        IChecklistTaskRepository taskRepository,
        IOnboardingRepository onboardingRepository,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        UserContext userContext,
        IOperatorContextService operatorContextService)
    {
        _noteRepository = noteRepository;
        _taskRepository = taskRepository;
        _onboardingRepository = onboardingRepository;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        _userContext = userContext;
        _operatorContextService = operatorContextService;
    }

    /// <summary>
    /// Create a new note for a task
    /// </summary>
    public async Task<long> CreateNoteAsync(ChecklistTaskNoteInputDto input)
    {
        // Validate task exists
        var task = await _taskRepository.GetFirstAsync(x => x.Id == input.TaskId);
        if (task == null)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "Task not found");
        }

        // Validate onboarding exists
        var onboarding = await _onboardingRepository.GetByIdAsync(input.OnboardingId);
        if (onboarding == null)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
        }

        var note = new ChecklistTaskNote
        {
            TaskId = input.TaskId,
            OnboardingId = input.OnboardingId,
            Content = input.Content,
            NoteType = input.NoteType,
            Priority = input.Priority,
            CreatedById = GetCurrentUserId(),
            CreatedByName = GetCurrentUserName(),
            CreateBy = GetCurrentUserName(),
            ModifyBy = GetCurrentUserName()
            // ModifiedById 和 ModifiedByName 在创建时保持为 null
        };

        await _noteRepository.InsertAsync(note);
        return note.Id;
    }

    /// <summary>
    /// Update an existing note
    /// </summary>
    public async Task<bool> UpdateNoteAsync(ChecklistTaskNoteUpdateDto input)
    {
        var note = await _noteRepository.GetByIdAsync(input.Id);
        if (note == null || note.IsDeleted)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "Note not found");
        }

        note.Content = input.Content;
        note.NoteType = input.NoteType;
        note.Priority = input.Priority;
        note.ModifiedById = GetCurrentUserId();
        note.ModifiedByName = GetCurrentUserName();
        note.ModifyBy = GetCurrentUserName();
        note.ModifyDate = DateTimeOffset.UtcNow;

        await _noteRepository.UpdateAsync(note);
        return true;
    }

    /// <summary>
    /// Delete a note (soft delete)
    /// </summary>
    public async Task<bool> DeleteNoteAsync(long noteId)
    {
        var note = await _noteRepository.GetByIdAsync(noteId);
        if (note == null)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "Note not found");
        }

        note.IsDeleted = true;
        note.ModifyBy = GetCurrentUserName();
        note.ModifyDate = DateTimeOffset.UtcNow;

        await _noteRepository.UpdateAsync(note);
        return true;
    }

    /// <summary>
    /// Get a specific note by ID
    /// </summary>
    public async Task<ChecklistTaskNoteOutputDto?> GetNoteByIdAsync(long noteId)
    {
        var note = await _noteRepository.GetByIdAsync(noteId);
        if (note == null || note.IsDeleted)
        {
            return null;
        }

        return MapToOutputDto(note);
    }

    /// <summary>
    /// Get all notes for a specific task
    /// </summary>
    public async Task<List<ChecklistTaskNoteOutputDto>> GetNotesByTaskAsync(long taskId, long onboardingId, bool includeDeleted = false)
    {
        var notes = await _noteRepository.GetByTaskAndOnboardingAsync(taskId, onboardingId, includeDeleted);
        return notes.Select(MapToOutputDto).OrderByDescending(x => x.CreatedAt).ToList();
    }

    /// <summary>
    /// Get notes summary for a task
    /// </summary>
    public async Task<ChecklistTaskNotesSummaryDto> GetNotesSummaryAsync(long taskId, long onboardingId)
    {
        var notes = await _noteRepository.GetByTaskAndOnboardingAsync(taskId, onboardingId, false);
        var pinnedCount = await _noteRepository.CountPinnedNotesAsync(taskId, onboardingId);
        var latestNote = await _noteRepository.GetLatestNoteAsync(taskId, onboardingId);

        return new ChecklistTaskNotesSummaryDto
        {
            TaskId = taskId,
            TotalCount = notes.Count,
            PinnedCount = pinnedCount,
            LatestNote = latestNote != null ? MapToOutputDto(latestNote) : null,
            Notes = notes.Select(MapToOutputDto).OrderByDescending(x => x.CreatedAt).ToList()
        };
    }

    /// <summary>
    /// Pin or unpin a note
    /// </summary>
    public async Task<bool> ToggleNotePinAsync(long noteId, bool isPinned)
    {
        var note = await _noteRepository.GetByIdAsync(noteId);
        if (note == null || note.IsDeleted)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "Note not found");
        }

        note.IsPinned = isPinned;
        note.ModifyBy = GetCurrentUserName();
        note.ModifyDate = DateTimeOffset.UtcNow;

        await _noteRepository.UpdateAsync(note);
        return true;
    }

    /// <summary>
    /// Get all pinned notes for a task
    /// </summary>
    public async Task<List<ChecklistTaskNoteOutputDto>> GetPinnedNotesAsync(long taskId, long onboardingId)
    {
        var notes = await _noteRepository.GetPinnedNotesAsync(taskId, onboardingId);
        return notes.Select(MapToOutputDto).OrderByDescending(x => x.CreatedAt).ToList();
    }

    /// <summary>
    /// Search notes by content
    /// </summary>
    public async Task<List<ChecklistTaskNoteOutputDto>> SearchNotesAsync(long taskId, long onboardingId, string searchTerm)
    {
        var notes = await _noteRepository.SearchByContentAsync(taskId, onboardingId, searchTerm);
        return notes.Select(MapToOutputDto).OrderByDescending(x => x.CreatedAt).ToList();
    }

    /// <summary>
    /// Get notes by priority
    /// </summary>
    public async Task<List<ChecklistTaskNoteOutputDto>> GetNotesByPriorityAsync(long taskId, long onboardingId, string priority)
    {
        var notes = await _noteRepository.GetByPriorityAsync(taskId, onboardingId, priority);
        return notes.Select(MapToOutputDto).OrderByDescending(x => x.CreatedAt).ToList();
    }

    /// <summary>
    /// Batch get notes summary for multiple tasks
    /// </summary>
    public async Task<Dictionary<long, ChecklistTaskNotesSummaryDto>> BatchGetNotesSummaryAsync(List<long> taskIds, long onboardingId)
    {
        var result = new Dictionary<long, ChecklistTaskNotesSummaryDto>();

        foreach (var taskId in taskIds)
        {
            var summary = await GetNotesSummaryAsync(taskId, onboardingId);
            result[taskId] = summary;
        }

        return result;
    }

    /// <summary>
    /// Map entity to output DTO
    /// </summary>
    private ChecklistTaskNoteOutputDto MapToOutputDto(ChecklistTaskNote note)
    {
        return new ChecklistTaskNoteOutputDto
        {
            Id = note.Id,
            TaskId = note.TaskId,
            OnboardingId = note.OnboardingId,
            Content = note.Content,
            NoteType = note.NoteType,
            Priority = note.Priority,
            CreatedBy = note.CreatedById,
            CreatedByName = note.CreatedByName,
            CreatedAt = note.CreateDate,
            ModifiedBy = note.ModifiedById,
            ModifiedByName = note.ModifiedByName,
            ModifiedAt = note.ModifyDate,
            IsDeleted = note.IsDeleted,
            IsPinned = note.IsPinned
        };
    }

    /// <summary>
    /// Get current user ID
    /// </summary>
    private string GetCurrentUserId()
    {
        return _userContext?.UserId ?? "system";
    }

    /// <summary>
    /// Get current user name (FirstName + LastName > UserName > Email)
    /// </summary>
    private string GetCurrentUserName()
    {
        return _operatorContextService.GetOperatorDisplayName();
    }
}