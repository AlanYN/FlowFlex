using FlowFlex.Domain.Entities.Action;
using FlowFlex.Domain.Repository.Action;
using SqlSugar;

namespace FlowFlex.SqlSugarDB.Repositories.Action
{
    /// <summary>
    /// Action Trigger Mapping repository implementation
    /// </summary>
    public class ActionTriggerMappingRepository : BaseRepository<ActionTriggerMapping>, IActionTriggerMappingRepository
    {
        public ActionTriggerMappingRepository(ISqlSugarClient dbContext) : base(dbContext) { }

        /// <summary>
        /// Get mappings by trigger source
        /// </summary>
        public async Task<List<ActionTriggerMapping>> GetByTriggerAsync(string triggerType, long triggerSourceId, string triggerEvent)
        {
            return await db.Queryable<ActionTriggerMapping>()
                .Where(x => x.TriggerType == triggerType
                         && x.TriggerSourceId == triggerSourceId
                         && x.TriggerEvent == triggerEvent
                         && x.IsEnabled
                         && x.IsValid)
                .OrderBy(x => x.ExecutionOrder)
                .ToListAsync();
        }

        /// <summary>
        /// Get mappings for a specific trigger
        /// </summary>
        public async Task<List<ActionTriggerMapping>> GetMappingsForTriggerAsync(
            string triggerSourceType,
            long triggerSourceId,
            string triggerEventType,
            long? workflowId = null,
            long? stageId = null,
            CancellationToken cancellationToken = default)
        {
            return await db.Queryable<ActionTriggerMapping>()
                .Where(x => x.TriggerSourceId == triggerSourceId
                         && x.TriggerEvent == triggerEventType
                         && x.IsEnabled
                         && x.IsValid)
                .WhereIF(!string.IsNullOrEmpty(triggerSourceType), x => x.TriggerType == triggerSourceType)
                .WhereIF(workflowId.HasValue, x => x.WorkFlowId == workflowId.Value || SqlFunc.IsNullOrEmpty(x.WorkFlowId))
                .WhereIF(stageId.HasValue, x => x.StageId == stageId.Value || SqlFunc.IsNullOrEmpty(x.StageId))
                .OrderBy(x => x.ExecutionOrder)
                .ToListAsync();
        }

        /// <summary>
        /// Get mappings by action definition ID
        /// </summary>
        public async Task<List<ActionTriggerMapping>> GetByActionDefinitionIdAsync(long actionDefinitionId)
        {
            return await db.Queryable<ActionTriggerMapping>()
                .Where(x => x.ActionDefinitionId == actionDefinitionId && x.IsValid)
                .OrderBy(x => x.TriggerType)
                .ToListAsync();
        }

        /// <summary>
        /// Get mappings by trigger type
        /// </summary>
        public async Task<List<ActionTriggerMapping>> GetByTriggerTypeAsync(string triggerType)
        {
            return await db.Queryable<ActionTriggerMapping>()
                .Where(x => x.TriggerType == triggerType && x.IsValid)
                .OrderBy(x => x.TriggerSourceId)
                .OrderBy(x => x.ExecutionOrder)
                .ToListAsync();
        }

        /// <summary>
        /// Check if mapping exists
        /// </summary>
        public async Task<bool> IsMappingExistsAsync(long actionDefinitionId, string triggerType, long triggerSourceId, long workFlowId)
        {
            var query = db.Queryable<ActionTriggerMapping>()
                .Where(x => x.ActionDefinitionId == actionDefinitionId
                         && x.TriggerType == triggerType
                         && x.TriggerSourceId == triggerSourceId
                         && x.WorkFlowId == workFlowId
                         && x.IsValid);

            return await query.AnyAsync();
        }

        /// <summary>
        /// Get all enabled mappings
        /// </summary>
        public async Task<List<ActionTriggerMapping>> GetAllEnabledAsync()
        {
            return await db.Queryable<ActionTriggerMapping>()
                .Where(x => x.IsEnabled && x.IsValid)
                .OrderBy(x => x.TriggerType)
                .OrderBy(x => x.TriggerSourceId)
                .OrderBy(x => x.ExecutionOrder)
                .ToListAsync();
        }

        /// <summary>
        /// Get mappings with action details
        /// </summary>
        public async Task<List<(ActionTriggerMapping Mapping, ActionDefinition Action)>> GetMappingsWithActionsAsync(string triggerType, long triggerSourceId, string triggerEvent)
        {
            var result = await db.Queryable<ActionTriggerMapping>()
                .LeftJoin<ActionDefinition>((m, a) => m.ActionDefinitionId == a.Id)
                .Where((m, a) => m.TriggerType == triggerType
                              && m.TriggerSourceId == triggerSourceId
                              && m.TriggerEvent == triggerEvent
                              && m.IsEnabled
                              && m.IsValid
                              && a.IsEnabled
                              && a.IsValid)
                .OrderBy((m, a) => m.ExecutionOrder)
                .Select((m, a) => new { Mapping = m, Action = a })
                .ToListAsync();

            return result.Select(x => (x.Mapping, x.Action)).ToList();
        }

        /// <summary>
        /// Batch enable or disable mappings
        /// </summary>
        public async Task<bool> BatchUpdateEnabledStatusAsync(List<long> mappingIds, bool isEnabled)
        {
            if (!mappingIds.Any()) return true;

            var affectedRows = await db.Updateable<ActionTriggerMapping>()
                .SetColumns(x => x.IsEnabled == isEnabled)
                .SetColumns(x => x.ModifyDate == DateTimeOffset.UtcNow)
                .Where(x => mappingIds.Contains(x.Id) && x.IsValid)
                .ExecuteCommandAsync();

            return affectedRows > 0;
        }

        /// <summary>
        /// Delete mappings by action definition ID
        /// </summary>
        public async Task<bool> DeleteByActionDefinitionIdAsync(long actionDefinitionId)
        {
            var affectedRows = await db.Updateable<ActionTriggerMapping>()
                .SetColumns(x => x.IsValid == false)
                .SetColumns(x => x.ModifyDate == DateTimeOffset.UtcNow)
                .Where(x => x.ActionDefinitionId == actionDefinitionId && x.IsValid)
                .ExecuteCommandAsync();

            return affectedRows >= 0; // Return true even if no records affected
        }
    }
}