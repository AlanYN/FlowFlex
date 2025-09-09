using FlowFlex.Domain.Entities.Action;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.Action;
using FlowFlex.Domain.Shared.Enums.Action;
using FlowFlex.Domain.Shared.Models;
using Item.Common.Lib.EnumUtil;
using SqlSugar;

namespace FlowFlex.SqlSugarDB.Repositories.Action
{
    /// <summary>
    /// Action Definition repository implementation
    /// </summary>
    public class ActionDefinitionRepository : BaseRepository<ActionDefinition>, IActionDefinitionRepository
    {
        private readonly UserContext _userContext;

        public ActionDefinitionRepository(
            UserContext userContext,
            ISqlSugarClient dbContext
            ) : base(dbContext)
        {
            _userContext = userContext;
        }

        /// <summary>
        /// Get action definitions by action type
        /// </summary>
        public async Task<List<ActionDefinition>> GetByActionTypeAsync(string actionType)
        {
            return await db.Queryable<ActionDefinition>()
                .Where(x => x.ActionType == actionType
                         && x.IsValid
                         && x.TenantId == _userContext.TenantId
                         && x.AppCode == _userContext.AppCode)
                .OrderBy(x => x.ActionName)
                .ToListAsync();
        }

        /// <summary>
        /// Get all enabled action definitions
        /// </summary>
        public async Task<List<ActionDefinition>> GetAllEnabledAsync()
        {
            return await db.Queryable<ActionDefinition>()
                .Where(x => x.IsEnabled
                         && x.IsValid
                         && x.TenantId == _userContext.TenantId
                         && x.AppCode == _userContext.AppCode)
                .OrderBy(x => x.ActionName)
                .ToListAsync();
        }

        /// <summary>
        /// Get action definitions by name (fuzzy search)
        /// </summary>
        public async Task<List<ActionDefinition>> GetByNameAsync(string name)
        {
            return await db.Queryable<ActionDefinition>()
                .Where(x => x.ActionName.Contains(name)
                         && x.IsValid
                         && x.TenantId == _userContext.TenantId
                         && x.AppCode == _userContext.AppCode)
                .OrderBy(x => x.ActionName)
                .ToListAsync();
        }

        /// <summary>
        /// Check if action name exists
        /// </summary>
        public async Task<bool> IsActionNameExistsAsync(string actionName, long? excludeId = null)
        {
            var query = db.Queryable<ActionDefinition>()
                .Where(x => x.ActionName == actionName
                         && x.IsValid
                         && x.TenantId == _userContext.TenantId
                         && x.AppCode == _userContext.AppCode);

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// Get action definitions with pagination
        /// </summary>
        public async Task<(List<ActionDefinition> Data, int TotalCount)> GetPagedAsync(int pageIndex,
            int pageSize,
            string? actionType = null,
            string? keyword = null,
            bool? isAssignmentStage = null,
            bool? isAssignmentChecklist = null,
            bool? isAssignmentQuestionnaire = null,
            bool? isAssignmentWorkflow = null,
            bool? isTools = null)
        {
            RefAsync<int> totalCount = 0;

            var query = db.Queryable<ActionDefinition>()
                .Where(x => x.IsValid
                         && x.TenantId == _userContext.TenantId
                         && x.AppCode == _userContext.AppCode);

            // Filter by action type
            if (!string.IsNullOrEmpty(actionType))
            {
                query = query.Where(x => x.ActionType == actionType);
            }

            // Filter by keyword (search in name and description)
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(x => x.ActionName.Contains(keyword) || x.ActionCode.Contains(keyword));
            }

            query.WhereIF(isTools.HasValue, x => x.IsTools == isTools.Value);

            query.WhereIF(isTools.HasValue && !isTools.Value, x => x.CreateUserId == Convert.ToInt64(_userContext.UserId));

            var triggerTypeFilters = new[]
            {
                (isAssignmentChecklist, TriggerTypeEnum.Task),
                (isAssignmentQuestionnaire, TriggerTypeEnum.Question),
                (isAssignmentStage, TriggerTypeEnum.Stage),
                (isAssignmentWorkflow, TriggerTypeEnum.Workflow)
            };

            foreach (var (hasValue, triggerType) in triggerTypeFilters)
            {
                if (hasValue.HasValue)
                {
                    var triggerTypeDescription = triggerType.GetDescription();
                    if (hasValue.Value)
                    {
                        query = query.Where(x => SqlFunc.Subqueryable<ActionTriggerMapping>()
                            .Where(m => m.ActionDefinitionId == x.Id && m.TriggerType == triggerTypeDescription && m.IsValid && m.IsEnabled)
                            .Any());
                    }
                    else
                    {
                        query = query.Where(x => SqlFunc.Subqueryable<ActionTriggerMapping>()
                            .Where(m => m.ActionDefinitionId == x.Id && m.TriggerType == triggerTypeDescription && m.IsValid && m.IsEnabled)
                            .NotAny());
                    }
                }
            }

            // Get paged data
            var data = await query
                .OrderByDescending(x => x.CreateDate)
                .ToOffsetPageAsync(pageIndex, pageSize, totalCount);

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
                .Where(x => actionIds.Contains(x.Id)
                         && x.IsValid
                         && x.TenantId == _userContext.TenantId
                         && x.AppCode == _userContext.AppCode)
                .ExecuteCommandAsync();

            return affectedRows > 0;
        }

        /// <summary>
        /// Get trigger mappings with related entity names by action definition IDs
        /// </summary>
        public async Task<List<ActionTriggerMappingWithDetails>> GetTriggerMappingsWithDetailsByActionIdsAsync(List<long> actionDefinitionIds)
        {
            if (actionDefinitionIds.Count == 0) return new List<ActionTriggerMappingWithDetails>();

            var workflowQuery = db.Queryable<ActionTriggerMapping>()
                .InnerJoin<Workflow>((m, w) => m.WorkFlowId == w.Id)
                .Where((m, w) => actionDefinitionIds.Contains(m.ActionDefinitionId) &&
                                   m.TriggerType == "Workflow" &&
                                   m.IsValid &&
                                   w.IsValid)
                .Select((m, w) => new ActionTriggerMappingWithDetails
                {
                    Id = m.Id,
                    ActionDefinitionId = m.ActionDefinitionId,
                    TriggerType = m.TriggerType,
                    TriggerSourceId = m.TriggerSourceId,
                    TriggerSourceName = w.Name,
                    WorkFlowId = 0,
                    WorkFlowName = "",
                    StageId = 0,
                    StageName = "",
                    TriggerEvent = m.TriggerEvent,
                    IsEnabled = m.IsEnabled,
                    ExecutionOrder = m.ExecutionOrder,
                    Description = m.Description,
                    LastApplied = SqlFunc.Subqueryable<ActionExecution>().Where(e => e.ActionTriggerMappingId == m.Id && e.IsValid).Max(e => e.CreateDate)
                });

            var stageQuery = db.Queryable<ActionTriggerMapping>()
                .InnerJoin<Workflow>((m, w) => m.WorkFlowId == w.Id)
                .InnerJoin<Stage>((m, w, s) => m.TriggerSourceId == s.Id)
                .Where((m, w, s) => actionDefinitionIds.Contains(m.ActionDefinitionId) &&
                                   m.TriggerType == "Stage" &&
                                   m.IsValid &&
                                   w.IsValid &&
                                   s.IsValid)
                .Select((m, w, s) => new ActionTriggerMappingWithDetails
                {
                    Id = m.Id,
                    ActionDefinitionId = m.ActionDefinitionId,
                    TriggerType = m.TriggerType,
                    TriggerSourceId = m.TriggerSourceId,
                    TriggerSourceName = s.Name,
                    WorkFlowId = w.Id,
                    WorkFlowName = w.Name,
                    StageId = 0,
                    StageName = "",
                    TriggerEvent = m.TriggerEvent,
                    IsEnabled = m.IsEnabled,
                    ExecutionOrder = m.ExecutionOrder,
                    Description = m.Description,
                    LastApplied = SqlFunc.Subqueryable<ActionExecution>().Where(e => e.ActionTriggerMappingId == m.Id && e.IsValid).Max(e => e.CreateDate)
                });

            var taskQuery = db.Queryable<ActionTriggerMapping>()
                .InnerJoin<Checklist>((m, c) => m.TriggerSourceId == c.Id)
                .LeftJoin<Workflow>((m, c, w) => m.WorkFlowId == w.Id)
                .LeftJoin<Stage>((m, c, w, s) => m.StageId == s.Id)
                .Where((m, c, w, s) => actionDefinitionIds.Contains(m.ActionDefinitionId) &&
                                       m.TriggerType == "Task" &&
                                       m.IsValid &&
                                       c.IsValid)
                .Select((m, c, w, s) => new ActionTriggerMappingWithDetails
                {
                    Id = m.Id,
                    ActionDefinitionId = m.ActionDefinitionId,
                    TriggerType = m.TriggerType,
                    TriggerSourceId = m.TriggerSourceId,
                    TriggerSourceName = c.Name,
                    WorkFlowId = SqlFunc.IsNull(w.Id, 0),
                    WorkFlowName = w.Name ?? "",
                    StageId = SqlFunc.IsNull(s.Id, 0),
                    StageName = s.Name ?? "",
                    TriggerEvent = m.TriggerEvent,
                    IsEnabled = m.IsEnabled,
                    ExecutionOrder = m.ExecutionOrder,
                    Description = m.Description,
                    LastApplied = SqlFunc.Subqueryable<ActionExecution>().Where(e => e.ActionTriggerMappingId == m.Id && e.IsValid).Max(e => e.CreateDate)
                });

            var questionQuery = db.Queryable<ActionTriggerMapping>()
                .InnerJoin<Questionnaire>((m, q) => m.TriggerSourceId == q.Id)
                .LeftJoin<Workflow>((m, q, w) => m.WorkFlowId == w.Id)
                .LeftJoin<Stage>((m, q, w, s) => m.StageId == s.Id)
                .Where((m, q, w, s) => actionDefinitionIds.Contains(m.ActionDefinitionId) &&
                                       m.TriggerType == "Question" &&
                                       m.IsValid &&
                                       q.IsValid)
                .Select((m, q, w, s) => new ActionTriggerMappingWithDetails
                {
                    Id = m.Id,
                    ActionDefinitionId = m.ActionDefinitionId,
                    TriggerType = m.TriggerType,
                    TriggerSourceId = m.TriggerSourceId,
                    TriggerSourceName = q.Name,
                    WorkFlowId = SqlFunc.IsNull(w.Id, 0),
                    WorkFlowName = w.Name ?? "",
                    StageId = SqlFunc.IsNull(s.Id, 0),
                    StageName = s.Name ?? "",
                    TriggerEvent = m.TriggerEvent,
                    IsEnabled = m.IsEnabled,
                    ExecutionOrder = m.ExecutionOrder,
                    Description = m.Description,
                    LastApplied = SqlFunc.Subqueryable<ActionExecution>().Where(e => e.ActionTriggerMappingId == m.Id && e.IsValid).Max(e => e.CreateDate)
                });

            return await db.UnionAll(workflowQuery, stageQuery, taskQuery, questionQuery).ToListAsync();
        }

        /// <summary>
        /// Get trigger mappings with action details by trigger source id
        /// </summary>
        public async Task<List<ActionTriggerMappingWithActionDetails>> GetMappingsWithActionDetailsByTriggerSourceIdAsync(long triggerSourceId)
        {
            var query = db.Queryable<ActionTriggerMapping>()
                .InnerJoin<ActionDefinition>((m, a) => m.ActionDefinitionId == a.Id)
                .Where((m, a) => m.TriggerSourceId == triggerSourceId &&
                                   m.IsValid &&
                                   a.IsValid)
                .Select((m, a) => new ActionTriggerMappingWithActionDetails
                {
                    Id = m.Id,
                    ActionDefinitionId = m.ActionDefinitionId,
                    ActionCode = a.ActionCode,
                    ActionName = a.ActionName,
                    ActionType = a.ActionType,
                    ActionDescription = a.Description,
                    ActionIsEnabled = a.IsEnabled,
                    TriggerType = m.TriggerType,
                    TriggerSourceId = m.TriggerSourceId,
                    TriggerEvent = m.TriggerEvent,
                    IsEnabled = m.IsEnabled,
                    ExecutionOrder = m.ExecutionOrder,
                    Description = m.Description,
                    LastApplied = SqlFunc.Subqueryable<ActionExecution>().Where(e => e.ActionTriggerMappingId == m.Id && e.IsValid).Max(e => e.CreateDate)
                });

            return await query.ToListAsync();
        }
    }

}


