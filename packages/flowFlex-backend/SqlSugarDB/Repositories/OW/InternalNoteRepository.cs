using SqlSugar;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.SqlSugarDB;

namespace FlowFlex.SqlSugarDB.Implements.OW;

/// <summary>
/// InternalNote repository implementation
/// </summary>
public class InternalNoteRepository : BaseRepository<InternalNote>, IInternalNoteRepository, IScopedService
{
    public InternalNoteRepository(ISqlSugarClient sqlSugarClient) : base(sqlSugarClient)
    {
    }

    /// <summary>
    /// Get notes by onboarding ID
    /// </summary>
    public async Task<List<InternalNote>> GetByOnboardingIdAsync(long onboardingId)
    {
        return await db.Queryable<InternalNote>()
            .Where(x => x.OnboardingId == onboardingId && x.IsValid)
            .OrderByDescending(x => x.CreateDate)
            .ToListAsync();
    }

    /// <summary>
    /// Get notes by onboarding and stage
    /// </summary>
    public async Task<List<InternalNote>> GetByOnboardingAndStageAsync(long onboardingId, long? stageId)
    {
        var query = db.Queryable<InternalNote>()
            .Where(x => x.OnboardingId == onboardingId && x.IsValid);

        if (stageId.HasValue)
        {
            query = query.Where(x => x.StageId == stageId.Value);
        }
        else
        {
            query = query.Where(x => x.StageId == null);
        }

        return await query.OrderByDescending(x => x.CreateDate).ToListAsync();
    }

    /// <summary>
    /// Get unresolved notes
    /// </summary>
    public async Task<List<InternalNote>> GetUnresolvedNotesAsync(long onboardingId)
    {
        return await db.Queryable<InternalNote>()
            .Where(x => x.OnboardingId == onboardingId && !x.IsResolved && x.IsValid)
            .OrderByDescending(x => x.CreateDate)
            .ToListAsync();
    }

    /// <summary>
    /// Get notes by priority
    /// </summary>
    public async Task<List<InternalNote>> GetByPriorityAsync(string priority)
    {
        return await db.Queryable<InternalNote>()
            .Where(x => x.Priority == priority && x.IsValid)
            .OrderByDescending(x => x.CreateDate)
            .ToListAsync();
    }

    /// <summary>
    /// Get notes by type
    /// </summary>
    public async Task<List<InternalNote>> GetByTypeAsync(string noteType)
    {
        return await db.Queryable<InternalNote>()
            .Where(x => x.NoteType == noteType && x.IsValid)
            .OrderByDescending(x => x.CreateDate)
            .ToListAsync();
    }

    /// <summary>
    /// Mark note as resolved
    /// </summary>
    public async Task<bool> MarkAsResolvedAsync(long id, string resolvedBy)
    {
        var note = await GetByIdAsync(id);
        if (note == null || !note.IsValid)
        {
            return false;
        }

        note.IsResolved = true;
        note.ResolvedTime = DateTimeOffset.Now;
        note.ResolvedBy = resolvedBy;
        note.ModifyDate = DateTimeOffset.Now;

        return await UpdateAsync(note);
    }

    /// <summary>
    /// Mark note as unresolved
    /// </summary>
    public async Task<bool> MarkAsUnresolvedAsync(long id)
    {
        var note = await GetByIdAsync(id);
        if (note == null || !note.IsValid)
        {
            return false;
        }

        note.IsResolved = false;
        note.ResolvedTime = null;
        note.ResolvedBy = string.Empty;
        note.ModifyDate = DateTimeOffset.Now;

        return await UpdateAsync(note);
    }

    /// <summary>
    /// Get paged internal notes with filters
    /// </summary>
    public async Task<(List<InternalNote> items, int totalCount)> GetPagedAsync(
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
        // Build base query with explicit table name to avoid field mapping issues
        var query = db.Queryable<InternalNote>()
            .Where(x => x.IsValid);

        // Apply filters with explicit field references
        if (onboardingId.HasValue)
        {
            query = query.Where(x => x.OnboardingId == onboardingId.Value);
        }

        if (stageId.HasValue)
        {
            query = query.Where(x => x.StageId == stageId.Value);
        }

        if (!string.IsNullOrEmpty(noteType))
        {
            query = query.Where(x => x.NoteType == noteType);
        }

        if (!string.IsNullOrEmpty(priority))
        {
            query = query.Where(x => x.Priority == priority);
        }

        if (isResolved.HasValue)
        {
            query = query.Where(x => x.IsResolved == isResolved.Value);
        }

        // Apply sorting
        switch (sortField.ToLower())
        {
            case "title":
                query = sortDirection.ToLower() == "asc"
                    ? query.OrderBy(x => x.Title)
                    : query.OrderByDescending(x => x.Title);
                break;
            case "priority":
                query = sortDirection.ToLower() == "asc"
                    ? query.OrderBy(x => x.Priority)
                    : query.OrderByDescending(x => x.Priority);
                break;
            case "notetype":
                query = sortDirection.ToLower() == "asc"
                    ? query.OrderBy(x => x.NoteType)
                    : query.OrderByDescending(x => x.NoteType);
                break;
            default: // CreateDate
                query = sortDirection.ToLower() == "asc"
                    ? query.OrderBy(x => x.CreateDate)
                    : query.OrderByDescending(x => x.CreateDate);
                break;
        }

        // Get total count using a separate simple query to avoid field mapping issues
        var countQuery = db.Queryable<InternalNote>()
            .Where(x => x.IsValid);

        // Apply the same filters for count
        if (onboardingId.HasValue)
        {
            countQuery = countQuery.Where(x => x.OnboardingId == onboardingId.Value);
        }

        if (stageId.HasValue)
        {
            countQuery = countQuery.Where(x => x.StageId == stageId.Value);
        }

        if (!string.IsNullOrEmpty(noteType))
        {
            countQuery = countQuery.Where(x => x.NoteType == noteType);
        }

        if (!string.IsNullOrEmpty(priority))
        {
            countQuery = countQuery.Where(x => x.Priority == priority);
        }

        if (isResolved.HasValue)
        {
            countQuery = countQuery.Where(x => x.IsResolved == isResolved.Value);
        }

        // Execute count query first
        var totalCount = await countQuery.CountAsync();

        // Get paged items
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <summary>
    /// Get notes by note type
    /// </summary>
    public async Task<List<InternalNote>> GetByNoteTypeAsync(string noteType, int days = 30)
    {
        var startDate = DateTimeOffset.Now.AddDays(-days);
        return await db.Queryable<InternalNote>()
            .Where(x => x.NoteType == noteType && x.CreateDate >= startDate && x.IsValid)
            .OrderByDescending(x => x.CreateDate)
            .ToListAsync();
    }

    /// <summary>
    /// Get notes by visibility
    /// </summary>
    public async Task<List<InternalNote>> GetByVisibilityAsync(string visibility, long onboardingId)
    {
        return await db.Queryable<InternalNote>()
            .Where(x => x.Visibility == visibility && x.OnboardingId == onboardingId && x.IsValid)
            .OrderByDescending(x => x.CreateDate)
            .ToListAsync();
    }

    /// <summary>
    /// Search notes by tags
    /// </summary>
    public async Task<List<InternalNote>> SearchByTagsAsync(List<string> tags, long? onboardingId = null)
    {
        var query = db.Queryable<InternalNote>()
            .Where(x => x.IsValid);

        if (onboardingId.HasValue)
        {
            query = query.Where(x => x.OnboardingId == onboardingId.Value);
        }

        // For PostgreSQL jsonb array contains search
        foreach (var tag in tags)
        {
            query = query.Where(x => x.Tags.Contains(tag));
        }

        return await query.OrderByDescending(x => x.CreateDate).ToListAsync();
    }

    /// <summary>
    /// Search notes by keyword
    /// </summary>
    public async Task<List<InternalNote>> SearchByKeywordAsync(string keyword, long? onboardingId = null)
    {
        var query = db.Queryable<InternalNote>()
            .Where(x => x.IsValid &&
                       (x.Title.ToLower().Contains(keyword.ToLower()) || x.Content.ToLower().Contains(keyword.ToLower())));

        if (onboardingId.HasValue)
        {
            query = query.Where(x => x.OnboardingId == onboardingId.Value);
        }

        return await query.OrderByDescending(x => x.CreateDate).ToListAsync();
    }

    /// <summary>
    /// Batch resolve notes
    /// </summary>
    public async Task<bool> BatchResolveAsync(List<long> noteIds, long resolvedById, string resolutionNotes = "")
    {
        var notes = await db.Queryable<InternalNote>()
            .Where(x => noteIds.Contains(x.Id) && x.IsValid)
            .ToListAsync();

        if (!notes.Any())
        {
            return false;
        }

        foreach (var note in notes)
        {
            note.IsResolved = true;
            note.ResolvedTime = DateTimeOffset.Now;
            note.ResolvedById = resolvedById;
            note.ResolutionNotes = resolutionNotes;
            note.ModifyDate = DateTimeOffset.Now;
        }

        return await db.Updateable(notes).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// Get note statistics
    /// </summary>
    public async Task<Dictionary<string, object>> GetNoteStatisticsAsync(long? onboardingId = null, int days = 30)
    {
        var startDate = DateTimeOffset.Now.AddDays(-days);
        var query = db.Queryable<InternalNote>()
            .Where(x => x.CreateDate >= startDate && x.IsValid);

        if (onboardingId.HasValue)
        {
            query = query.Where(x => x.OnboardingId == onboardingId.Value);
        }

        var totalCount = await query.CountAsync();
        var resolvedCount = await query.Where(x => x.IsResolved).CountAsync();
        var unresolvedCount = totalCount - resolvedCount;

        var priorityStats = await query
            .GroupBy(x => x.Priority)
            .Select(g => new { Priority = g.Priority, Count = SqlFunc.AggregateCount(g.Priority) })
            .ToListAsync();

        var typeStats = await query
            .GroupBy(x => x.NoteType)
            .Select(g => new { Type = g.NoteType, Count = SqlFunc.AggregateCount(g.NoteType) })
            .ToListAsync();

        return new Dictionary<string, object>
        {
            ["totalCount"] = totalCount,
            ["resolvedCount"] = resolvedCount,
            ["unresolvedCount"] = unresolvedCount,
            ["priorityStats"] = priorityStats,
            ["typeStats"] = typeStats
        };
    }

    /// <summary>
    /// Get mentioned notes
    /// </summary>
    public async Task<List<InternalNote>> GetMentionedNotesAsync(long userId, int days = 30)
    {
        var startDate = DateTimeOffset.Now.AddDays(-days);
        return await db.Queryable<InternalNote>()
            .Where(x => x.CreateDate >= startDate && x.IsValid &&
                       x.MentionedUserIds.Contains(userId.ToString()))
            .OrderByDescending(x => x.CreateDate)
            .ToListAsync();
    }
}
