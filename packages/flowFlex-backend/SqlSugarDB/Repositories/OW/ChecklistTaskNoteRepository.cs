using SqlSugar;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.SqlSugarDB.Repositories.OW
{
    /// <summary>
    /// ChecklistTaskNote repository implementation
    /// </summary>
    public class ChecklistTaskNoteRepository : BaseRepository<ChecklistTaskNote>, IChecklistTaskNoteRepository, IScopedService
    {
        public ChecklistTaskNoteRepository(ISqlSugarClient dbContext) : base(dbContext) { }

        /// <summary>
        /// Get notes by task ID and onboarding ID
        /// </summary>
        public async Task<List<ChecklistTaskNote>> GetByTaskAndOnboardingAsync(long taskId, long onboardingId, bool includeDeleted = false)
        {
            var query = base.db.Queryable<ChecklistTaskNote>()
                .Where(x => x.TaskId == taskId && x.OnboardingId == onboardingId);

            if (!includeDeleted)
            {
                query = query.Where(x => !x.IsDeleted);
            }

            return await query.OrderBy(x => x.CreateDate, OrderByType.Desc).ToListAsync();
        }

        /// <summary>
        /// Get pinned notes by task ID and onboarding ID
        /// </summary>
        public async Task<List<ChecklistTaskNote>> GetPinnedNotesAsync(long taskId, long onboardingId)
        {
            return await base.db.Queryable<ChecklistTaskNote>()
                .Where(x => x.TaskId == taskId && x.OnboardingId == onboardingId && x.IsPinned && !x.IsDeleted)
                .OrderBy(x => x.CreateDate, OrderByType.Desc)
                .ToListAsync();
        }

        /// <summary>
        /// Search notes by content
        /// </summary>
        public async Task<List<ChecklistTaskNote>> SearchByContentAsync(long taskId, long onboardingId, string searchTerm)
        {
            return await base.db.Queryable<ChecklistTaskNote>()
                .Where(x => x.TaskId == taskId && x.OnboardingId == onboardingId && !x.IsDeleted)
                .Where(x => x.Content.Contains(searchTerm))
                .OrderBy(x => x.CreateDate, OrderByType.Desc)
                .ToListAsync();
        }

        /// <summary>
        /// Get notes by priority
        /// </summary>
        public async Task<List<ChecklistTaskNote>> GetByPriorityAsync(long taskId, long onboardingId, string priority)
        {
            return await base.db.Queryable<ChecklistTaskNote>()
                .Where(x => x.TaskId == taskId && x.OnboardingId == onboardingId && x.Priority == priority && !x.IsDeleted)
                .OrderBy(x => x.CreateDate, OrderByType.Desc)
                .ToListAsync();
        }

        /// <summary>
        /// Get latest note for a task
        /// </summary>
        public async Task<ChecklistTaskNote?> GetLatestNoteAsync(long taskId, long onboardingId)
        {
            return await base.db.Queryable<ChecklistTaskNote>()
                .Where(x => x.TaskId == taskId && x.OnboardingId == onboardingId && !x.IsDeleted)
                .OrderBy(x => x.CreateDate, OrderByType.Desc)
                .FirstAsync();
        }

        /// <summary>
        /// Count notes for a task
        /// </summary>
        public async Task<int> CountNotesAsync(long taskId, long onboardingId, bool includeDeleted = false)
        {
            var query = base.db.Queryable<ChecklistTaskNote>()
                .Where(x => x.TaskId == taskId && x.OnboardingId == onboardingId);

            if (!includeDeleted)
            {
                query = query.Where(x => !x.IsDeleted);
            }

            return await query.CountAsync();
        }

        /// <summary>
        /// Count pinned notes for a task
        /// </summary>
        public async Task<int> CountPinnedNotesAsync(long taskId, long onboardingId)
        {
            return await base.db.Queryable<ChecklistTaskNote>()
                .Where(x => x.TaskId == taskId && x.OnboardingId == onboardingId && x.IsPinned && !x.IsDeleted)
                .CountAsync();
        }

        /// <summary>
        /// Batch get notes for multiple tasks
        /// </summary>
        public async Task<Dictionary<long, List<ChecklistTaskNote>>> BatchGetNotesByTasksAsync(List<long> taskIds, long onboardingId)
        {
            if (!taskIds.Any())
            {
                return new Dictionary<long, List<ChecklistTaskNote>>();
            }

            var notes = await base.db.Queryable<ChecklistTaskNote>()
                .Where(x => taskIds.Contains(x.TaskId) && x.OnboardingId == onboardingId && !x.IsDeleted)
                .OrderBy(x => x.CreateDate, OrderByType.Desc)
                .ToListAsync();

            return notes.GroupBy(x => x.TaskId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>
        /// Count notes by task ID (without onboarding filter, but exclude deleted onboardings)
        /// </summary>
        public async Task<int> CountByTaskIdAsync(long taskId)
        {
            return await base.db.Queryable<ChecklistTaskNote>()
                .LeftJoin<Onboarding>((ctn, ob) => ctn.OnboardingId == ob.Id)
                .Where((ctn, ob) => ctn.TaskId == taskId && !ctn.IsDeleted && ob.IsValid)
                .CountAsync();
        }
    }
}