using FlowFlex.Domain.Entities.Action;
using FlowFlex.Domain.Repository.Action;
using SqlSugar;

namespace FlowFlex.SqlSugarDB.Repositories.Action
{
    /// <summary>
    /// Action Definition repository implementation
    /// </summary>
    public class ActionDefinitionRepository : BaseRepository<ActionDefinition>, IActionDefinitionRepository
    {
        public ActionDefinitionRepository(ISqlSugarClient dbContext) : base(dbContext) { }

        /// <summary>
        /// Get action definitions by action type
        /// </summary>
        public async Task<List<ActionDefinition>> GetByActionTypeAsync(string actionType)
        {
            return await db.Queryable<ActionDefinition>()
                .Where(x => x.ActionType == actionType && x.IsValid)
                .OrderBy(x => x.ActionName)
                .ToListAsync();
        }

        /// <summary>
        /// Get all enabled action definitions
        /// </summary>
        public async Task<List<ActionDefinition>> GetAllEnabledAsync()
        {
            return await db.Queryable<ActionDefinition>()
                .Where(x => x.IsEnabled && x.IsValid)
                .OrderBy(x => x.ActionName)
                .ToListAsync();
        }

        /// <summary>
        /// Get action definitions by name (fuzzy search)
        /// </summary>
        public async Task<List<ActionDefinition>> GetByNameAsync(string name)
        {
            return await db.Queryable<ActionDefinition>()
                .Where(x => x.ActionName.Contains(name) && x.IsValid)
                .OrderBy(x => x.ActionName)
                .ToListAsync();
        }

        /// <summary>
        /// Check if action name exists
        /// </summary>
        public async Task<bool> IsActionNameExistsAsync(string actionName, long? excludeId = null)
        {
            var query = db.Queryable<ActionDefinition>()
                .Where(x => x.ActionName == actionName && x.IsValid);

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// Get action definitions with pagination
        /// </summary>
        public async Task<(List<ActionDefinition> Data, int TotalCount)> GetPagedAsync(int pageIndex, int pageSize, string? actionType = null, string? keyword = null)
        {
            var query = db.Queryable<ActionDefinition>()
                .Where(x => x.IsValid);

            // Filter by action type
            if (!string.IsNullOrEmpty(actionType))
            {
                query = query.Where(x => x.ActionType == actionType);
            }

            // Filter by keyword (search in name and description)
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(x => x.ActionName.Contains(keyword) || x.Description.Contains(keyword));
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Get paged data
            var data = await query
                .OrderByDescending(x => x.CreateDate)
                .ToPageListAsync(pageIndex, pageSize);

            return (data, totalCount);
        }

        /// <summary>
        /// Batch enable or disable actions
        /// </summary>
        public async Task<bool> BatchUpdateEnabledStatusAsync(List<long> actionIds, bool isEnabled)
        {
            if (!actionIds.Any()) return true;

            var affectedRows = await db.Updateable<ActionDefinition>()
                .SetColumns(x => x.IsEnabled == isEnabled)
                .SetColumns(x => x.ModifyDate == DateTimeOffset.UtcNow)
                .Where(x => actionIds.Contains(x.Id) && x.IsValid)
                .ExecuteCommandAsync();

            return affectedRows > 0;
        }
    }
} 