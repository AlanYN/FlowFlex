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
    public async Task<bool> SaveTaskCompletionAsync(ChecklistTaskCompletion completion)
    {
        var existing = await GetTaskCompletionAsync(completion.OnboardingId, completion.TaskId);

        if (existing != null)
        {
            // Update existing
            existing.IsCompleted = completion.IsCompleted;
            existing.CompletedTime = completion.CompletedTime;
            existing.CompletionNotes = completion.CompletionNotes;
            existing.StageId = completion.StageId;
            existing.ModifyDate = DateTimeOffset.Now;
            existing.ModifyBy = completion.ModifyBy;
            existing.ModifyUserId = completion.ModifyUserId;

            return await UpdateAsync(existing);
        }
        else
        {
            // Insert new
            await InsertAsync(completion);
            return true;
        }
    }

    /// <summary>
    /// Batch save task completions
    /// </summary>
    public async Task<bool> BatchSaveTaskCompletionsAsync(List<ChecklistTaskCompletion> completions)
    {
        if (!completions.Any()) return true;

        try
        {
            await db.Ado.BeginTranAsync();

            foreach (var completion in completions)
            {
                await SaveTaskCompletionAsync(completion);
            }

            await db.Ado.CommitTranAsync();
            return true;
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
}
