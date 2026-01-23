using SqlSugar;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.SqlSugarDB;

namespace FlowFlex.SqlSugarDB.Implements.OW;

/// <summary>
/// ChecklistTaskCompletion repository implementation
/// </summary>
public class ChecklistTaskCompletionRepository : BaseRepository<ChecklistTaskCompletion>, IChecklistTaskCompletionRepository, IScopedService
{
    public ChecklistTaskCompletionRepository(ISqlSugarClient sqlSugarClient) : base(sqlSugarClient)
    {
    }

    /// <summary>
    /// Get task completion by lead and checklist
    /// </summary>
    public async Task<List<ChecklistTaskCompletion>> GetByLeadAndChecklistAsync(string leadId, long checklistId)
    {
        return await db.Queryable<ChecklistTaskCompletion>()
            .Where(x => x.LeadId == leadId && x.ChecklistId == checklistId && x.IsValid)
            .ToListAsync();
    }

    /// <summary>
    /// Get task completion by onboarding and checklist
    /// </summary>
    public async Task<List<ChecklistTaskCompletion>> GetByOnboardingAndChecklistAsync(long onboardingId, long checklistId)
    {
        return await db.Queryable<ChecklistTaskCompletion>()
            .Where(x => x.OnboardingId == onboardingId && x.ChecklistId == checklistId && x.IsValid)
            .ToListAsync();
    }

    /// <summary>
    /// Get task completion by onboarding and stage
    /// </summary>
    public async Task<List<ChecklistTaskCompletion>> GetByOnboardingAndStageAsync(long onboardingId, long stageId)
    {
        return await db.Queryable<ChecklistTaskCompletion>()
            .Where(x => x.OnboardingId == onboardingId && x.StageId == stageId && x.IsValid)
            .ToListAsync();
    }

    /// <summary>
    /// Get specific task completion
    /// </summary>
    public async Task<ChecklistTaskCompletion?> GetTaskCompletionAsync(long onboardingId, long taskId)
    {
        return await db.Queryable<ChecklistTaskCompletion>()
            .Where(x => x.OnboardingId == onboardingId && x.TaskId == taskId && x.IsValid)
            .FirstAsync();
    }

    /// <summary>
    /// Save or update task completion
    /// </summary>
    /// <returns>Tuple indicating success and whether completion status actually changed</returns>
    public async Task<(bool success, bool statusChanged)> SaveTaskCompletionAsync(ChecklistTaskCompletion completion)
    {
        var existing = await GetTaskCompletionAsync(completion.OnboardingId, completion.TaskId);

        if (existing != null)
        {
            // Check if the completion status is actually changing
            bool statusChanged = existing.IsCompleted != completion.IsCompleted;

            // Update existing - only update isCompleted and completion time, preserve other data
            existing.IsCompleted = completion.IsCompleted;
            existing.CompletedTime = completion.IsCompleted ? (completion.CompletedTime ?? DateTimeOffset.UtcNow) : null;

            // Only update other fields if they have meaningful values
            if (!string.IsNullOrEmpty(completion.CompletionNotes))
            {
                existing.CompletionNotes = completion.CompletionNotes;
            }

            if (!string.IsNullOrEmpty(completion.FilesJson) && completion.FilesJson != "[]")
            {
                existing.FilesJson = completion.FilesJson;
            }

            if (completion.StageId.HasValue)
            {
                existing.StageId = completion.StageId;
            }

            existing.ModifyDate = DateTimeOffset.UtcNow;
            existing.ModifyBy = completion.ModifyBy;
            existing.ModifyUserId = completion.ModifyUserId;

            var updateResult = await UpdateAsync(existing);
            return (updateResult, statusChanged);
        }
        else
        {
            // Insert new - this is always a status change since it's creating a new completion record
            await InsertAsync(completion);
            return (true, true);
        }
    }

    /// <summary>
    /// Batch save task completions
    /// </summary>
    /// <returns>List of tuples indicating success and status change for each completion</returns>
    public async Task<List<(bool success, bool statusChanged)>> BatchSaveTaskCompletionsAsync(List<ChecklistTaskCompletion> completions)
    {
        var results = new List<(bool success, bool statusChanged)>();

        if (!completions.Any())
            return results;

        try
        {
            await db.Ado.BeginTranAsync();

            foreach (var completion in completions)
            {
                var result = await SaveTaskCompletionAsync(completion);
                results.Add(result);
            }

            await db.Ado.CommitTranAsync();
            return results;
        }
        catch
        {
            await db.Ado.RollbackTranAsync();
            throw;
        }
    }

    /// <summary>
    /// Get completion statistics for checklist
    /// </summary>
    public async Task<(int totalTasks, int completedTasks)> GetCompletionStatsAsync(long onboardingId, long checklistId)
    {
        var completions = await GetByOnboardingAndChecklistAsync(onboardingId, checklistId);
        var completedCount = completions.Count(x => x.IsCompleted);

        // Get total tasks count from checklist
        var totalTasks = await db.Queryable<ChecklistTask>()
            .Where(x => x.ChecklistId == checklistId && x.IsValid)
            .CountAsync();

        return (totalTasks, completedCount);
    }

    /// <summary>
    /// Get task completions by task IDs
    /// </summary>
    public async Task<List<ChecklistTaskCompletion>> GetByTaskIdsAsync(List<long> taskIds)
    {
        if (taskIds == null || !taskIds.Any())
            return new List<ChecklistTaskCompletion>();

        return await db.Queryable<ChecklistTaskCompletion>()
            .LeftJoin<Onboarding>((ctc, ob) => ctc.OnboardingId == ob.Id)
            .Where((ctc, ob) => taskIds.Contains(ctc.TaskId) && ctc.IsValid && ob.IsValid)
            .Select((ctc, ob) => ctc)
            .ToListAsync();
    }

    /// <summary>
    /// Update only completion status without modifying other data
    /// </summary>
    public async Task<bool> UpdateCompletionStatusOnlyAsync(long onboardingId, long taskId, bool isCompleted, string modifyBy, long? modifyUserId = null)
    {
        var existing = await GetTaskCompletionAsync(onboardingId, taskId);

        if (existing != null)
        {
            // Only update completion status and related timestamp
            existing.IsCompleted = isCompleted;
            existing.CompletedTime = isCompleted ? DateTimeOffset.UtcNow : null;
            existing.ModifyDate = DateTimeOffset.UtcNow;
            existing.ModifyBy = modifyBy;
            if (modifyUserId.HasValue)
            {
                existing.ModifyUserId = modifyUserId.Value;
            }

            return await UpdateAsync(existing);
        }

        return false; // Record not found
    }
}
