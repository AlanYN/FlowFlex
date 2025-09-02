using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Application.Contracts.IServices.OW.ChangeLog;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Services.OW.ChangeLog
{
    /// <summary>
    /// Stage operation log service implementation
    /// </summary>
    public class StageLogService : BaseOperationLogService, IStageLogService
    {
        private readonly ILogger<StageLogService> _stageLogger;
        private readonly IActionExecutionService _actionExecutionService;

        public StageLogService(
            IOperationChangeLogRepository operationChangeLogRepository,
            ILogger<StageLogService> logger,
            UserContext userContext,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ILogCacheService logCacheService,
            IActionExecutionService actionExecutionService)
            : base(operationChangeLogRepository, logger, userContext, httpContextAccessor, mapper, logCacheService)
        {
            _stageLogger = logger;
            _actionExecutionService = actionExecutionService;
        }

        protected override string GetBusinessModuleName() => BusinessModuleEnum.Stage.ToString();

        protected override async Task<PagedResult<OperationChangeLogOutputDto>> GetOperationLogsFromDatabaseAsync(
            long? onboardingId,
            long? stageId,
            OperationTypeEnum? operationType,
            int pageIndex,
            int pageSize)
        {
            List<Domain.Entities.OW.OperationChangeLog> logs;

            if (stageId.HasValue)
            {
                logs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Stage.ToString(), stageId.Value);
            }
            else
            {
                logs = await _operationChangeLogRepository.GetByBusinessIdAsync(0); // Get all stage logs
                logs = logs.Where(x => x.BusinessModule == BusinessModuleEnum.Stage.ToString()).ToList();
            }

            // Apply filters
            if (onboardingId.HasValue)
            {
                logs = logs.Where(x => x.OnboardingId == onboardingId.Value).ToList();
            }

            if (operationType.HasValue)
            {
                logs = logs.Where(x => x.OperationType == operationType.ToString()).ToList();
            }

            // Apply pagination
            var totalCount = logs.Count;
            var pagedLogs = logs
                .OrderByDescending(x => x.OperationTime)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var logDtos = pagedLogs.Select(MapToOutputDto).ToList();

            return new PagedResult<OperationChangeLogOutputDto>
            {
                Items = logDtos,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        #region Stage Lifecycle Operations

        public async Task<bool> LogStageCreateAsync(long stageId, string stageName, long? workflowId = null, string extendedData = null)
        {
            try
            {
                string operationTitle = $"Stage Created: {stageName}";
                string operationDescription = $"Stage '{stageName}' has been created by {GetOperatorDisplayName()}";

                if (workflowId.HasValue)
                {
                    operationDescription += $" in workflow ID: {workflowId.Value}";
                }

                var extendedDataObj = JsonSerializer.Serialize(new
                {
                    StageId = stageId,
                    StageName = stageName,
                    WorkflowId = workflowId,
                    CreatedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.StageCreate,
                    BusinessModuleEnum.Stage,
                    stageId,
                    null, // No specific onboarding at creation
                    stageId,
                    operationTitle,
                    operationDescription,
                    extendedData: extendedDataObj
                );
            }
            catch (Exception ex)
            {
                _stageLogger.LogError(ex, "Failed to log stage create operation for stage {StageId}", stageId);
                return false;
            }
        }

        public async Task<bool> LogStageUpdateAsync(long stageId, string stageName, string beforeData, string afterData, List<string> changedFields, long? workflowId = null, string extendedData = null)
        {
            try
            {
                // Check if there's actually a meaningful change
                if (!HasMeaningfulValueChange(beforeData, afterData))
                {
                    _stageLogger.LogDebug("Skipping stage update log for stage {StageId} as there's no meaningful change", stageId);
                    return true;
                }

                string operationTitle = $"Stage Updated: {stageName}";
                string operationDescription = $"Stage '{stageName}' has been updated by {GetOperatorDisplayName()}";

                if (changedFields?.Any() == true)
                {
                    operationDescription += $". Changed fields: {string.Join(", ", changedFields)}";
                }

                var changedFieldsJson = changedFields?.Any() == true ? JsonSerializer.Serialize(changedFields) : null;

                var extendedDataObj = JsonSerializer.Serialize(new
                {
                    StageId = stageId,
                    StageName = stageName,
                    WorkflowId = workflowId,
                    ChangedFieldsCount = changedFields?.Count ?? 0,
                    UpdatedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.StageUpdate,
                    BusinessModuleEnum.Stage,
                    stageId,
                    null,
                    stageId,
                    operationTitle,
                    operationDescription,
                    beforeData: beforeData,
                    afterData: afterData,
                    changedFields: changedFieldsJson,
                    extendedData: extendedDataObj
                );
            }
            catch (Exception ex)
            {
                _stageLogger.LogError(ex, "Failed to log stage update operation for stage {StageId}", stageId);
                return false;
            }
        }

        public async Task<bool> LogStageDeleteAsync(long stageId, string stageName, long? workflowId = null, string reason = null, string extendedData = null)
        {
            try
            {
                string operationTitle = $"Stage Deleted: {stageName}";
                string operationDescription = $"Stage '{stageName}' has been deleted by {GetOperatorDisplayName()}";

                if (!string.IsNullOrEmpty(reason))
                {
                    operationDescription += $" with reason: {reason}";
                }

                var extendedDataObj = JsonSerializer.Serialize(new
                {
                    StageId = stageId,
                    StageName = stageName,
                    WorkflowId = workflowId,
                    Reason = reason,
                    DeletedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.StageDelete,
                    BusinessModuleEnum.Stage,
                    stageId,
                    null,
                    stageId,
                    operationTitle,
                    operationDescription,
                    extendedData: extendedDataObj
                );
            }
            catch (Exception ex)
            {
                _stageLogger.LogError(ex, "Failed to log stage delete operation for stage {StageId}", stageId);
                return false;
            }
        }

        public async Task<bool> LogStageOrderChangeAsync(long stageId, string stageName, int oldOrder, int newOrder, long? workflowId = null, string extendedData = null)
        {
            try
            {
                string operationTitle = $"Stage Order Changed: {stageName}";
                string operationDescription = $"Stage '{stageName}' order changed from {oldOrder} to {newOrder} by {GetOperatorDisplayName()}";

                var extendedDataObj = JsonSerializer.Serialize(new
                {
                    StageId = stageId,
                    StageName = stageName,
                    WorkflowId = workflowId,
                    OldOrder = oldOrder,
                    NewOrder = newOrder,
                    OrderChangedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.StageOrderChange,
                    BusinessModuleEnum.Stage,
                    stageId,
                    null,
                    stageId,
                    operationTitle,
                    operationDescription,
                    extendedData: extendedDataObj
                );
            }
            catch (Exception ex)
            {
                _stageLogger.LogError(ex, "Failed to log stage order change operation for stage {StageId}", stageId);
                return false;
            }
        }

        #endregion

        #region Stage Runtime Operations

        public async Task<bool> LogStageCompleteAsync(long stageId, string stageName, long onboardingId, string completionNotes = null, string extendedData = null)
        {
            try
            {
                string operationTitle = $"Stage Completed: {stageName}";
                string operationDescription = $"Stage '{stageName}' has been completed by {GetOperatorDisplayName()} for onboarding {onboardingId}";

                if (!string.IsNullOrEmpty(completionNotes))
                {
                    operationDescription += $" with notes: {completionNotes}";
                }

                var extendedDataObj = JsonSerializer.Serialize(new
                {
                    StageId = stageId,
                    StageName = stageName,
                    OnboardingId = onboardingId,
                    CompletionNotes = completionNotes,
                    CompletedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.StageComplete,
                    BusinessModuleEnum.Stage,
                    stageId,
                    onboardingId,
                    stageId,
                    operationTitle,
                    operationDescription,
                    extendedData: extendedDataObj
                );
            }
            catch (Exception ex)
            {
                _stageLogger.LogError(ex, "Failed to log stage complete operation for stage {StageId}", stageId);
                return false;
            }
        }

        public async Task<bool> LogStageReopenAsync(long stageId, string stageName, long onboardingId, string reason = null, string extendedData = null)
        {
            try
            {
                string operationTitle = $"Stage Reopened: {stageName}";
                string operationDescription = $"Stage '{stageName}' has been reopened by {GetOperatorDisplayName()} for onboarding {onboardingId}";

                if (!string.IsNullOrEmpty(reason))
                {
                    operationDescription += $" with reason: {reason}";
                }

                var extendedDataObj = JsonSerializer.Serialize(new
                {
                    StageId = stageId,
                    StageName = stageName,
                    OnboardingId = onboardingId,
                    Reason = reason,
                    ReopenedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.StageReopen,
                    BusinessModuleEnum.Stage,
                    stageId,
                    onboardingId,
                    stageId,
                    operationTitle,
                    operationDescription,
                    extendedData: extendedDataObj
                );
            }
            catch (Exception ex)
            {
                _stageLogger.LogError(ex, "Failed to log stage reopen operation for stage {StageId}", stageId);
                return false;
            }
        }

        #endregion

        #region Stage-Specific Queries

        public async Task<PagedResult<OperationChangeLogOutputDto>> GetStageLogsAsync(long stageId, long? onboardingId = null, int pageIndex = 1, int pageSize = 20)
        {
            try
            {
                var logs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Stage.ToString(), stageId);

                if (onboardingId.HasValue)
                {
                    logs = logs.Where(x => x.OnboardingId == onboardingId.Value).ToList();
                }

                // Apply pagination
                var totalCount = logs.Count;
                var pagedLogs = logs
                    .OrderByDescending(x => x.OperationTime)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var logDtos = pagedLogs.Select(MapToOutputDto).ToList();

                return new PagedResult<OperationChangeLogOutputDto>
                {
                    Items = logDtos,
                    TotalCount = totalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _stageLogger.LogError(ex, "Error getting stage logs for stage {StageId}", stageId);
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        public async Task<PagedResult<OperationChangeLogOutputDto>> GetStageComponentLogsAsync(long stageId, long? onboardingId = null, OperationTypeEnum? operationType = null, int pageIndex = 1, int pageSize = 20, bool includeActionExecutions = true)
        {
            try
            {
                List<OperationChangeLogOutputDto> allLogs = new List<OperationChangeLogOutputDto>();

                // Get stage logs
                var stageLogs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Stage.ToString(), stageId);
                
                if (onboardingId.HasValue)
                {
                    stageLogs = stageLogs.Where(x => x.OnboardingId == onboardingId.Value).ToList();
                }

                if (operationType.HasValue)
                {
                    stageLogs = stageLogs.Where(x => x.OperationType == operationType.ToString()).ToList();
                }

                allLogs.AddRange(stageLogs.Select(MapToOutputDto));

                // Get task and question logs for this stage
                var (taskIds, questionIds) = await GetTaskAndQuestionIdsBatchAsync(stageId);

                // Get task logs
                if (taskIds.Any())
                {
                    var taskLogs = await _operationChangeLogRepository.GetByBusinessIdsAsync(BusinessModuleEnum.ChecklistTask.ToString(), taskIds, onboardingId);
                    if (operationType.HasValue)
                    {
                        taskLogs = taskLogs.Where(x => x.OperationType == operationType.ToString()).ToList();
                    }
                    allLogs.AddRange(taskLogs.Select(MapToOutputDto));
                }

                // Get question logs
                if (questionIds.Any())
                {
                    var questionLogs = await _operationChangeLogRepository.GetByBusinessIdsAsync(BusinessModuleEnum.Question.ToString(), questionIds, onboardingId);
                    if (operationType.HasValue)
                    {
                        questionLogs = questionLogs.Where(x => x.OperationType == operationType.ToString()).ToList();
                    }
                    allLogs.AddRange(questionLogs.Select(MapToOutputDto));
                }

                // Add action executions if requested
                if (includeActionExecutions)
                {
                    await AddActionExecutionsBatchAsync(allLogs, stageId, taskIds, questionIds, onboardingId);
                }

                // Sort and paginate
                allLogs = allLogs.OrderByDescending(x => x.OperationTime).ToList();
                var totalCount = allLogs.Count;
                var pagedLogs = allLogs
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return new PagedResult<OperationChangeLogOutputDto>
                {
                    Items = pagedLogs,
                    TotalCount = totalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _stageLogger.LogError(ex, "Error getting stage component logs for stage {StageId}", stageId);
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        public async Task<PagedResult<OperationChangeLogOutputDto>> GetStageComponentLogsOptimizedAsync(long stageId, long? onboardingId = null, OperationTypeEnum? operationType = null, int pageIndex = 1, int pageSize = 20, bool includeActionExecutions = true)
        {
            try
            {
                // Get task and question IDs
                var (taskIds, questionIds) = await GetTaskAndQuestionIdsBatchAsync(stageId);

                // Use optimized repository method
                var (logs, totalCount) = await _operationChangeLogRepository.GetStageComponentLogsPaginatedAsync(
                    onboardingId,
                    stageId,
                    taskIds,
                    questionIds,
                    operationType?.ToString(),
                    pageIndex,
                    pageSize);

                var logDtos = logs.Select(MapToOutputDto).ToList();

                // Add action executions for current page only
                if (includeActionExecutions)
                {
                    await AddActionExecutionsBatchAsync(logDtos, stageId, taskIds, questionIds, onboardingId);
                }

                return new PagedResult<OperationChangeLogOutputDto>
                {
                    Items = logDtos,
                    TotalCount = totalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _stageLogger.LogError(ex, "Error getting optimized stage component logs for stage {StageId}", stageId);
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        public async Task<Dictionary<string, int>> GetStageOperationStatisticsAsync(long stageId, long? onboardingId = null)
        {
            try
            {
                var logs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Stage.ToString(), stageId);

                if (onboardingId.HasValue)
                {
                    logs = logs.Where(x => x.OnboardingId == onboardingId.Value).ToList();
                }

                var statistics = logs
                    .GroupBy(x => x.OperationType)
                    .ToDictionary(g => g.Key, g => g.Count());

                return statistics;
            }
            catch (Exception ex)
            {
                _stageLogger.LogError(ex, "Error getting stage operation statistics for stage {StageId}", stageId);
                return new Dictionary<string, int>();
            }
        }

        #endregion

        #region Helper Methods

        private async Task<(List<long> taskIds, List<long> questionIds)> GetTaskAndQuestionIdsBatchAsync(long stageId)
        {
            try
            {
                // This would typically call checklist and questionnaire services to get IDs
                // For now, return empty lists - this should be implemented based on your domain logic
                var taskIds = new List<long>();
                var questionIds = new List<long>();

                // TODO: Implement actual logic to get task and question IDs for a stage
                // Example:
                // var checklists = await _checklistService.GetChecklistsByStageIdAsync(stageId);
                // taskIds = checklists.SelectMany(c => c.Tasks).Select(t => t.Id).ToList();
                // 
                // var questionnaires = await _questionnaireService.GetQuestionnairesByStageIdAsync(stageId);
                // questionIds = questionnaires.SelectMany(q => q.Questions).Select(q => q.Id).ToList();

                return (taskIds, questionIds);
            }
            catch (Exception ex)
            {
                _stageLogger.LogWarning(ex, "Failed to get task and question IDs for stage {StageId}", stageId);
                return (new List<long>(), new List<long>());
            }
        }

        private async Task AddActionExecutionsBatchAsync(List<OperationChangeLogOutputDto> logs, long stageId, List<long> taskIds, List<long> questionIds, long? onboardingId)
        {
            try
            {
                var sourceIds = new List<long> { stageId };
                sourceIds.AddRange(taskIds);
                sourceIds.AddRange(questionIds);

                // This would call the action execution service to get action logs
                // Implementation would depend on your action execution service interface
                // For now, this is a placeholder
                
                _stageLogger.LogDebug("Added action executions for stage {StageId} with {SourceCount} source IDs", 
                    stageId, sourceIds.Count);
            }
            catch (Exception ex)
            {
                _stageLogger.LogWarning(ex, "Failed to add action executions for stage {StageId}", stageId);
            }
        }

        #endregion
    }
}