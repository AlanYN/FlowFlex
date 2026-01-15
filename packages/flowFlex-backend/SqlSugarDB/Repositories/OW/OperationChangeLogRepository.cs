using SqlSugar;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// Operation change log repository implementation
    /// </summary>
    public class OperationChangeLogRepository : BaseRepository<OperationChangeLog>, IOperationChangeLogRepository, IScopedService
    {
        private readonly ILogger<OperationChangeLogRepository> _logger;

        public OperationChangeLogRepository(ISqlSugarClient dbContext, ILogger<OperationChangeLogRepository> logger) : base(dbContext)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get operation logs by Onboarding ID
        /// </summary>
        public async Task<List<OperationChangeLog>> GetByOnboardingIdAsync(long onboardingId)
        {
            return await base.GetListAsync(x => x.OnboardingId == onboardingId && x.IsValid, x => x.OperationTime, OrderByType.Desc);
        }

        /// <summary>
        /// Get operation logs by Stage ID
        /// </summary>
        public async Task<List<OperationChangeLog>> GetByStageIdAsync(long stageId)
        {
            return await base.GetListAsync(x => x.StageId == stageId && x.IsValid, x => x.OperationTime, OrderByType.Desc);
        }

        /// <summary>
        /// Get operation logs by Onboarding ID and Stage ID
        /// </summary>
        public async Task<List<OperationChangeLog>> GetByOnboardingAndStageAsync(long onboardingId, long stageId)
        {
            return await base.GetListAsync(x => x.OnboardingId == onboardingId && x.StageId == stageId && x.IsValid, x => x.OperationTime, OrderByType.Desc);
        }

        /// <summary>
        /// Get operation logs by business ID and module
        /// </summary>
        public async Task<List<OperationChangeLog>> GetByBusinessAsync(string businessModule, long businessId)
        {
            return await base.GetListAsync(x => x.BusinessModule == businessModule && x.BusinessId == businessId && x.IsValid, x => x.OperationTime, OrderByType.Desc);
        }

        /// <summary>
        /// Get operation logs by business ID (without specifying business module)
        /// </summary>
        public async Task<List<OperationChangeLog>> GetByBusinessIdAsync(long businessId)
        {
            return await base.GetListAsync(x => x.BusinessId == businessId && x.IsValid, x => x.OperationTime, OrderByType.Desc);
        }

        /// <summary>
        /// Get operation logs by multiple business IDs (batch query)
        /// </summary>
        public async Task<List<OperationChangeLog>> GetByBusinessIdsAsync(List<long> businessIds)
        {
            if (businessIds == null || !businessIds.Any())
            {
                return new List<OperationChangeLog>();
            }

            return await base.GetListAsync(x => businessIds.Contains(x.BusinessId) && x.IsValid, x => x.OperationTime, OrderByType.Desc);
        }

        /// <summary>
        /// Get operation logs by operation type
        /// </summary>
        public async Task<List<OperationChangeLog>> GetByOperationTypeAsync(string operationType, long? onboardingId = null)
        {
            var predicate = Expressionable.Create<OperationChangeLog>();
            predicate.And(x => x.OperationType == operationType && x.IsValid);
            predicate.AndIF(onboardingId.HasValue, x => x.OnboardingId == onboardingId.Value);

            return await base.GetListAsync(predicate.ToExpression(), x => x.OperationTime, OrderByType.Desc);
        }

        /// <summary>
        /// Get operation logs by operator
        /// </summary>
        public async Task<List<OperationChangeLog>> GetByOperatorAsync(long operatorId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            var predicate = Expressionable.Create<OperationChangeLog>();
            predicate.And(x => x.OperatorId == operatorId && x.IsValid);
            predicate.AndIF(startDate.HasValue, x => x.OperationTime >= startDate.Value);
            predicate.AndIF(endDate.HasValue, x => x.OperationTime <= endDate.Value);

            return await base.GetListAsync(predicate.ToExpression(), x => x.OperationTime, OrderByType.Desc);
        }

        /// <summary>
        /// Get operation logs within time range
        /// </summary>
        public async Task<List<OperationChangeLog>> GetByTimeRangeAsync(DateTimeOffset startDate, DateTimeOffset endDate, long? onboardingId = null)
        {
            var predicate = Expressionable.Create<OperationChangeLog>();
            predicate.And(x => x.OperationTime >= startDate && x.OperationTime <= endDate && x.IsValid);
            predicate.AndIF(onboardingId.HasValue, x => x.OnboardingId == onboardingId.Value);

            return await base.GetListAsync(predicate.ToExpression(), x => x.OperationTime, OrderByType.Desc);
        }

        /// <summary>
        /// Get operation statistics
        /// </summary>
        public async Task<Dictionary<string, int>> GetOperationStatisticsAsync(long? onboardingId = null, long? stageId = null)
        {
            var predicate = Expressionable.Create<OperationChangeLog>();
            predicate.And(x => x.IsValid);
            predicate.AndIF(onboardingId.HasValue, x => x.OnboardingId == onboardingId.Value);
            predicate.AndIF(stageId.HasValue, x => x.StageId == stageId.Value);

            var query = base.db.Queryable<OperationChangeLog>()
                .Where(predicate.ToExpression())
                .GroupBy(x => x.OperationType)
                .Select(x => new { OperationType = x.OperationType, Count = SqlFunc.AggregateCount(x.Id) });

            var result = await query.ToListAsync();
            return result.ToDictionary(x => x.OperationType, x => x.Count);
        }

        /// <summary>
        /// Insert operation log using native SQL, specifically handles JSONB fields
        /// </summary>
        public async Task<bool> ExecuteInsertWithJsonbAsync(string sql, object parameters)
        {
            try
            {
                // Debug logging handled by structured logging
                // Debug logging handled by structured logging)}...");

                int result;

                // Check parameter type, use corresponding method if it's SugarParameter array
                if (parameters is SugarParameter[] sugarParams)
                {
                    result = await base.db.Ado.ExecuteCommandAsync(sql, sugarParams);
                }
                else
                {
                    result = await base.db.Ado.ExecuteCommandAsync(sql, parameters);
                }
                // Debug logging handled by structured logging
                return result > 0;
            }
            catch (Exception ex)
            {
                // Log detailed error information, but don't let the program crash
                // Debug logging handled by structured logging
                // Debug logging handled by structured logging.Name}");

                // Safely access StackTrace, avoid null reference exception
                if (!string.IsNullOrEmpty(ex.StackTrace))
                {
                    // Debug logging handled by structured logging
                }

                // If there's an inner exception, also log it
                if (ex.InnerException != null)
                {
                    // Debug logging handled by structured logging
                    // Debug logging handled by structured logging.Name}");
                }

                // Return false instead of throwing exception, let caller decide how to handle
                return false;
            }
        }

        /// <summary>
        /// Insert operation log using SqlSugar standard method
        /// </summary>
        public async Task<bool> InsertOperationLogAsync(OperationChangeLog operationLog)
        {
            var insertable = base.db.Insertable(operationLog);
            var result = await insertable.ExecuteCommandAsync();
            return result > 0;
        }

        /// <summary>
        /// Get operation logs by multiple business IDs and module (batch query to avoid N+1 problem)
        /// </summary>
        public async Task<List<OperationChangeLog>> GetByBusinessIdsAsync(string businessModule, List<long> businessIds, long? onboardingId = null)
        {
            if (businessIds == null || !businessIds.Any())
                return new List<OperationChangeLog>();

            var predicate = Expressionable.Create<OperationChangeLog>();
            predicate.And(x => x.BusinessModule == businessModule && businessIds.Contains(x.BusinessId) && x.IsValid);
            predicate.AndIF(onboardingId.HasValue, x => x.OnboardingId == onboardingId.Value);

            return await base.GetListAsync(predicate.ToExpression(), x => x.OperationTime, OrderByType.Desc);
        }

        /// <summary>
        /// Get operation logs with pagination for stage components (optimized for large datasets)
        /// Includes: Stage logs, ChecklistTask logs, Task logs, Question logs, QuestionnaireAnswer logs, 
        /// StaticField logs, File logs, Checklist logs, and Questionnaire logs
        /// </summary>
        public async Task<(List<OperationChangeLog> logs, int totalCount)> GetStageComponentLogsPaginatedAsync(
            long? onboardingId,
            long stageId,
            List<long> taskIds,
            List<long> questionIds,
            string operationType,
            int pageIndex,
            int pageSize)
        {
            try
            {
                // Create complex query with UNION (not UNION ALL) to combine all relevant logs and remove duplicates
                var query = base.db.Union(
                    // Stage-level logs
                    base.db.Queryable<OperationChangeLog>()
                        .Where(x => x.StageId == stageId && x.IsValid)
                        .WhereIF(onboardingId.HasValue, x => x.OnboardingId == onboardingId.Value)
                        .WhereIF(!string.IsNullOrEmpty(operationType), x => x.OperationType == operationType),

                    // ChecklistTask logs (if taskIds provided)
                    taskIds != null && taskIds.Any() ?
                        base.db.Queryable<OperationChangeLog>()
                            .Where(x => x.BusinessModule == "ChecklistTask" && taskIds.Contains(x.BusinessId) && x.IsValid)
                            .WhereIF(onboardingId.HasValue, x => x.OnboardingId == onboardingId.Value)
                            .WhereIF(!string.IsNullOrEmpty(operationType), x => x.OperationType == operationType) :
                        base.db.Queryable<OperationChangeLog>().Where(x => false), // Empty query if no task IDs

                    // General Task logs (filtered by onboardingId and stageId)
                    base.db.Queryable<OperationChangeLog>()
                        .Where(x => x.BusinessModule == "Task" && x.IsValid)
                        .WhereIF(onboardingId.HasValue, x => x.OnboardingId == onboardingId.Value)
                        .Where(x => x.StageId == stageId)
                        .WhereIF(!string.IsNullOrEmpty(operationType), x => x.OperationType == operationType),

                    // Question logs (if questionIds provided)
                    questionIds != null && questionIds.Any() ?
                        base.db.Queryable<OperationChangeLog>()
                            .Where(x => x.BusinessModule == "Question" && questionIds.Contains(x.BusinessId) && x.IsValid)
                            .WhereIF(onboardingId.HasValue, x => x.OnboardingId == onboardingId.Value)
                            .WhereIF(!string.IsNullOrEmpty(operationType), x => x.OperationType == operationType) :
                        base.db.Queryable<OperationChangeLog>().Where(x => false), // Empty query if no question IDs

                    // QuestionnaireAnswer logs (filtered by onboardingId and stageId)
                    base.db.Queryable<OperationChangeLog>()
                        .Where(x => x.BusinessModule == "QuestionnaireAnswer" && x.IsValid)
                        .WhereIF(onboardingId.HasValue, x => x.OnboardingId == onboardingId.Value)
                        .Where(x => x.StageId == stageId)
                        .WhereIF(!string.IsNullOrEmpty(operationType), x => x.OperationType == operationType),

                    // StaticField logs (filtered by onboardingId and stageId)
                    base.db.Queryable<OperationChangeLog>()
                        .Where(x => x.BusinessModule == "StaticField" && x.IsValid)
                        .WhereIF(onboardingId.HasValue, x => x.OnboardingId == onboardingId.Value)
                        .Where(x => x.StageId == stageId)
                        .WhereIF(!string.IsNullOrEmpty(operationType), x => x.OperationType == operationType),

                    // File logs (filtered by onboardingId and stageId)
                    base.db.Queryable<OperationChangeLog>()
                        .Where(x => x.BusinessModule == "File" && x.IsValid)
                        .WhereIF(onboardingId.HasValue, x => x.OnboardingId == onboardingId.Value)
                        .Where(x => x.StageId == stageId)
                        .WhereIF(!string.IsNullOrEmpty(operationType), x => x.OperationType == operationType),

                    // Checklist logs (filtered by onboardingId and stageId)
                    base.db.Queryable<OperationChangeLog>()
                        .Where(x => x.BusinessModule == "Checklist" && x.IsValid)
                        .WhereIF(onboardingId.HasValue, x => x.OnboardingId == onboardingId.Value)
                        .Where(x => x.StageId == stageId)
                        .WhereIF(!string.IsNullOrEmpty(operationType), x => x.OperationType == operationType),

                    // Questionnaire logs (filtered by onboardingId and stageId)
                    base.db.Queryable<OperationChangeLog>()
                        .Where(x => x.BusinessModule == "Questionnaire" && x.IsValid)
                        .WhereIF(onboardingId.HasValue, x => x.OnboardingId == onboardingId.Value)
                        .Where(x => x.StageId == stageId)
                        .WhereIF(!string.IsNullOrEmpty(operationType), x => x.OperationType == operationType)
                );

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination and sorting, with additional deduplication by ID to ensure no duplicates
                var logs = await query
                    .OrderByDescending(x => x.OperationTime)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Additional deduplication by ID to ensure no duplicates at application level
                logs = logs.GroupBy(x => x.Id).Select(g => g.First()).ToList();

                return (logs, totalCount);
            }
            catch (Exception ex)
            {
                // Log error and return empty result
                // In production, you might want to use ILogger here
                return (new List<OperationChangeLog>(), 0);
            }
        }

        /// <summary>
        /// Get workflow and related stage logs by workflow ID
        /// Excludes Case-level logs (where onboardingId is not null)
        /// </summary>
        public async Task<PagedResult<OperationChangeLog>> GetWorkflowWithRelatedLogsAsync(long workflowId, int pageIndex = 1, int pageSize = 20)
        {
            try
            {
                // This query includes:
                // 1. Workflow operations where business_id = workflowId AND business_module = 'Workflow'
                // 2. Stage operations where the stage belongs to the workflow (using JOIN with ff_stage table)
                // 3. ActionMapping operations where the mapping belongs to the workflow (using JOIN with ff_action_trigger_mappings table)
                // Excludes: Case-level logs where onboardingId is not null (those are Case-specific stage updates)

                var query = base.db.Queryable<OperationChangeLog>()
                    .LeftJoin<Domain.Entities.OW.Stage>((log, stage) =>
                        log.BusinessModule == "Stage" && log.BusinessId == stage.Id)
                    .LeftJoin<Domain.Entities.Action.ActionTriggerMapping>((log, stage, mapping) =>
                        log.BusinessModule == "ActionMapping" && log.BusinessId == mapping.Id)
                    .Where((log, stage, mapping) =>
                        log.OnboardingId == null && // Exclude Case-level logs
                        ((log.BusinessModule == "Workflow" && log.BusinessId == workflowId) ||
                        (log.BusinessModule == "Stage" && stage.WorkflowId == workflowId) ||
                        (log.BusinessModule == "ActionMapping" && mapping.WorkFlowId == workflowId)))
                    .OrderByDescending(log => log.OperationTime)
                    .Select(log => log);

                // Get total count
                var totalCount = await query.CountAsync();

                // Get paginated results
                var logs = await query
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return new PagedResult<OperationChangeLog>
                {
                    Items = logs,
                    TotalCount = totalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                // Log error and return empty result
                // In production, you might want to use ILogger here
                return new PagedResult<OperationChangeLog>
                {
                    Items = new List<OperationChangeLog>(),
                    TotalCount = 0,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };
            }
        }

        /// <summary>
        /// Get questionnaire and related ActionMapping logs by questionnaire ID
        /// </summary>
        public async Task<PagedResult<OperationChangeLog>> GetQuestionnaireWithRelatedLogsAsync(long questionnaireId, int pageIndex = 1, int pageSize = 20)
        {
            try
            {
                // This query includes:
                // 1. Questionnaire operations where business_id = questionnaireId AND business_module = 'Questionnaire'
                // 2. ActionMapping operations where the action is associated with questions from this questionnaire
                // Note: For Question type triggers, we need to get the questionnaire first to extract question IDs from structure

                // First, get the questionnaire to extract question IDs from its structure
                var questionnaire = await base.db.Queryable<Domain.Entities.OW.Questionnaire>()
                    .Where(q => q.Id == questionnaireId && q.IsValid)
                    .FirstAsync();

                List<long> questionIds = new List<long>();
                if (questionnaire?.Structure != null)
                {
                    try
                    {
                        var structure = JsonDocument.Parse(questionnaire.Structure.ToString());
                        if (structure.RootElement.TryGetProperty("sections", out var sections))
                        {
                            foreach (var section in sections.EnumerateArray())
                            {
                                if (section.TryGetProperty("questions", out var questions))
                                {
                                    foreach (var question in questions.EnumerateArray())
                                    {
                                        if (question.TryGetProperty("id", out var questionId))
                                        {
                                            // Try to parse as Int64 first, then as string
                                            if (questionId.ValueKind == JsonValueKind.Number && questionId.TryGetInt64(out var id))
                                            {
                                                questionIds.Add(id);
                                            }
                                            else if (questionId.ValueKind == JsonValueKind.String && long.TryParse(questionId.GetString(), out var stringId))
                                            {
                                                questionIds.Add(stringId);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse questionnaire structure for ID {QuestionnaireId}", questionnaireId);
                    }
                }

                _logger.LogDebug("Extracted {Count} question IDs from questionnaire {QuestionnaireId}: [{QuestionIds}]",
                    questionIds.Count, questionnaireId, string.Join(", ", questionIds));

                var query = base.db.Queryable<OperationChangeLog>()
                    .LeftJoin<Domain.Entities.Action.ActionTriggerMapping>((log, mapping) =>
                        log.BusinessModule == "ActionMapping" && log.BusinessId == mapping.Id)
                    .Where((log, mapping) =>
                        (log.BusinessModule == "Questionnaire" && log.BusinessId == questionnaireId) ||
                        (log.BusinessModule == "ActionMapping" && mapping.TriggerType == "Question" && questionIds.Contains(mapping.TriggerSourceId)))
                    .OrderByDescending(log => log.OperationTime)
                    .Select(log => log);

                // Get total count
                var totalCount = await query.CountAsync();

                // Get paginated results
                var logs = await query
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return new PagedResult<OperationChangeLog>
                {
                    Items = logs,
                    TotalCount = totalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting questionnaire with related logs for questionnaire {QuestionnaireId}", questionnaireId);
                return new PagedResult<OperationChangeLog>
                {
                    Items = new List<OperationChangeLog>(),
                    TotalCount = 0,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };
            }
        }

        /// <summary>
        /// Get checklist and related checklist task logs by checklist ID
        /// </summary>
        public async Task<PagedResult<OperationChangeLog>> GetChecklistWithRelatedLogsAsync(long checklistId, int pageIndex = 1, int pageSize = 20)
        {
            try
            {
                // This query includes:
                // 1. Checklist operations where business_id = checklistId AND business_module = 'Checklist'
                // 2. ChecklistTask operations where the task belongs to the checklist (using JOIN with ff_checklist_task table)
                // 3. ActionMapping operations where the action is associated with tasks in this checklist

                var query = base.db.Queryable<OperationChangeLog>()
                    .LeftJoin<Domain.Entities.OW.ChecklistTask>((log, task) =>
                        log.BusinessModule == "Task" && log.BusinessId == task.Id)
                    .LeftJoin<Domain.Entities.Action.ActionTriggerMapping>((log, task, mapping) =>
                        log.BusinessModule == "ActionMapping" && log.BusinessId == mapping.Id)
                    .LeftJoin<Domain.Entities.OW.ChecklistTask>((log, task, mapping, triggerTask) =>
                        mapping.TriggerType == "Task" && mapping.TriggerSourceId == triggerTask.Id)
                    .Where((log, task, mapping, triggerTask) =>
                        (log.BusinessModule == "Checklist" && log.BusinessId == checklistId) ||
                        (log.BusinessModule == "Task" && task.ChecklistId == checklistId) ||
                        (log.BusinessModule == "ActionMapping" && triggerTask.ChecklistId == checklistId))
                    .OrderByDescending(log => log.OperationTime)
                    .Select(log => log);

                // Get total count
                var totalCount = await query.CountAsync();

                // Get paginated results
                var logs = await query
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return new PagedResult<OperationChangeLog>
                {
                    Items = logs,
                    TotalCount = totalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                // Log error and return empty result
                // In production, you might want to use ILogger here
                return new PagedResult<OperationChangeLog>
                {
                    Items = new List<OperationChangeLog>(),
                    TotalCount = 0,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };
            }
        }
    }
}
