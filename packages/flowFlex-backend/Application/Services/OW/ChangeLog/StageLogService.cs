using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Application.Contracts.IServices.OW;
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
            IActionExecutionService actionExecutionService,
            IUserService userService,
            IOperatorContextService operatorContextService)
            : base(operationChangeLogRepository, logger, userContext, httpContextAccessor, mapper, logCacheService, userService, operatorContextService)
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

        public async Task<bool> LogStageCreateAsync(long stageId, string stageName, long? workflowId = null, string afterData = null, string extendedData = null)
        {
            try
            {
                string operationTitle = $"Stage Created: {stageName}";
                string operationDescription = $"Stage '{stageName}' has been created by {GetOperatorDisplayName()}";

                // Add ViewPermissionMode and other important fields if afterData is provided
                if (!string.IsNullOrEmpty(afterData))
                {
                    try
                    {
                        var afterJson = JsonSerializer.Deserialize<JsonElement>(afterData);
                        var details = new List<string>();

                        // Extract ViewPermissionMode and Teams (combine them together)
                        string viewPermissionMode = null;
                        if (afterJson.TryGetProperty("ViewPermissionMode", out var viewPermissionModeElement) ||
                            afterJson.TryGetProperty("viewPermissionMode", out viewPermissionModeElement))
                        {
                            viewPermissionMode = GetViewPermissionModeDisplayName(viewPermissionModeElement);
                        }

                        string viewTeamsSummary = null;
                        if (afterJson.TryGetProperty("ViewTeams", out var viewTeamsElement) ||
                            afterJson.TryGetProperty("viewTeams", out viewTeamsElement))
                        {
                            viewTeamsSummary = await GetTeamsSummaryAsync(viewTeamsElement);
                        }

                        string operateTeamsSummary = null;
                        if (afterJson.TryGetProperty("OperateTeams", out var operateTeamsElement) ||
                            afterJson.TryGetProperty("operateTeams", out operateTeamsElement))
                        {
                            operateTeamsSummary = await GetTeamsSummaryAsync(operateTeamsElement);
                        }

                        // Combine view permission mode and teams information
                        if (!string.IsNullOrEmpty(viewPermissionMode))
                        {
                            var permissionParts = new List<string> { viewPermissionMode };

                            if (!string.IsNullOrEmpty(viewTeamsSummary))
                            {
                                permissionParts.Add($"view teams: {viewTeamsSummary}");
                            }

                            if (!string.IsNullOrEmpty(operateTeamsSummary))
                            {
                                permissionParts.Add($"operate teams: {operateTeamsSummary}");
                            }

                            details.Add(string.Join("; ", permissionParts));
                        }
                        else
                        {
                            // If no view permission mode, still show teams if they exist
                            if (!string.IsNullOrEmpty(viewTeamsSummary))
                            {
                                details.Add($"view teams: {viewTeamsSummary}");
                            }
                            if (!string.IsNullOrEmpty(operateTeamsSummary))
                            {
                                details.Add($"operate teams: {operateTeamsSummary}");
                            }
                        }

                        // Extract UseSameTeamForOperate
                        if (afterJson.TryGetProperty("UseSameTeamForOperate", out var useSameTeamElement) ||
                            afterJson.TryGetProperty("useSameTeamForOperate", out useSameTeamElement))
                        {
                            var useSameTeam = useSameTeamElement.ValueKind == JsonValueKind.True;
                            if (useSameTeam)
                            {
                                details.Add("use same team for operate: Yes");
                            }
                        }

                        // Extract other important fields if needed
                        if (afterJson.TryGetProperty("Description", out var descElement) ||
                            afterJson.TryGetProperty("description", out descElement))
                        {
                            var desc = descElement.GetString();
                            if (!string.IsNullOrEmpty(desc))
                            {
                                details.Add($"description: '{desc}'");
                            }
                        }

                        // Extract VisibleInPortal (Available in Customer Portal)
                        if (afterJson.TryGetProperty("VisibleInPortal", out var visibleInPortalElement) ||
                            afterJson.TryGetProperty("visibleInPortal", out visibleInPortalElement))
                        {
                            var visibleInPortal = visibleInPortalElement.ValueKind == JsonValueKind.True;
                            details.Add($"Available in Customer Portal: {(visibleInPortal ? "Yes" : "No")}");
                        }

                        // Extract DefaultAssignee
                        if (afterJson.TryGetProperty("DefaultAssignee", out var defaultAssigneeElement) ||
                            afterJson.TryGetProperty("defaultAssignee", out defaultAssigneeElement))
                        {
                            var defaultAssigneeSummary = await GetDefaultAssigneeSummaryAsync(defaultAssigneeElement);
                            if (!string.IsNullOrEmpty(defaultAssigneeSummary))
                            {
                                details.Add($"default assignee: {defaultAssigneeSummary}");
                            }
                        }

                        // Extract Components
                        if (afterJson.TryGetProperty("Components", out var componentsElement) ||
                            afterJson.TryGetProperty("components", out componentsElement))
                        {
                            var componentsSummary = GetComponentsSummary(componentsElement);
                            if (!string.IsNullOrEmpty(componentsSummary))
                            {
                                details.Add($"components: {componentsSummary}");
                            }
                        }

                        if (details.Any())
                        {
                            operationDescription += $". {string.Join("; ", details)}";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse afterData for stage create log");
                    }
                }

                if (workflowId.HasValue)
                {
                    operationDescription += " in associated workflow";
                }

                var extendedDataObj = JsonSerializer.Serialize(new
                {
                    StageId = stageId,
                    StageName = stageName,
                    WorkflowId = workflowId,
                    CreatedAt = FormatUsDateTime(DateTimeOffset.UtcNow)
                });

                return await LogOperationAsync(
                    OperationTypeEnum.StageCreate,
                    BusinessModuleEnum.Stage,
                    stageId,
                    null, // No specific onboarding at creation
                    stageId,
                    operationTitle,
                    operationDescription,
                    beforeData: null,
                    afterData: afterData,
                    changedFields: null,
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
                // Debug logging for changedFields parameter
                _stageLogger.LogInformation("[StageLogService] LogStageUpdateAsync called for stage {StageId} with changedFields: Count={Count}, Values={Values}",
                    stageId,
                    changedFields?.Count ?? 0,
                    changedFields != null ? string.Join(", ", changedFields) : "null");

                // Check if there's actually a meaningful change
                if (!HasMeaningfulValueChangeEnhanced(beforeData, afterData))
                {
                    _stageLogger.LogDebug("Skipping stage update log for stage {StageId} as there's no meaningful change", stageId);
                    return true;
                }

                string operationTitle = $"Stage Updated: {stageName}";

                // Use enhanced description method that provides detailed change information
                string operationDescription = await BuildEnhancedStageOperationDescriptionAsync(
                    stageName,
                    "Updated",
                    beforeData,
                    afterData,
                    changedFields,
                    workflowId);

                // Set changedFields to null (empty array in API response)
                // Change details are already included in operationDescription
                string changedFieldsJson = null;

                var extendedDataObj = JsonSerializer.Serialize(new
                {
                    StageId = stageId,
                    StageName = stageName,
                    WorkflowId = workflowId,
                    ChangedFieldsCount = changedFields?.Count ?? 0,
                    UpdatedAt = FormatUsDateTime(DateTimeOffset.UtcNow)
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

                // Don't show reason in operation description for Stage delete operations

                var extendedDataObj = JsonSerializer.Serialize(new
                {
                    StageId = stageId,
                    StageName = stageName,
                    WorkflowId = workflowId,
                    Reason = reason,
                    DeletedAt = FormatUsDateTime(DateTimeOffset.UtcNow)
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

                if (workflowId.HasValue)
                {
                    operationDescription += " in associated workflow";
                }

                var extendedDataObj = JsonSerializer.Serialize(new
                {
                    StageId = stageId,
                    StageName = stageName,
                    WorkflowId = workflowId,
                    OldOrder = oldOrder,
                    NewOrder = newOrder,
                    OrderChangedAt = FormatUsDateTime(DateTimeOffset.UtcNow)
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
                string operationDescription = $"Stage '{stageName}' has been completed by {GetOperatorDisplayName()} for onboarding";

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
                    CompletedAt = FormatUsDateTime(DateTimeOffset.UtcNow)
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
                string operationDescription = $"Stage '{stageName}' has been reopened by {GetOperatorDisplayName()} for onboarding";

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
                    ReopenedAt = FormatUsDateTime(DateTimeOffset.UtcNow)
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

        #region Action Execution Logging

        /// <summary>
        /// Log stage action execution
        /// </summary>
        public async Task<bool> LogStageActionExecutionAsync(long stageId, string stageName, long? onboardingId, string actionName, string actionType, string executionResult, string executionDetails = null, string extendedData = null)
        {
            try
            {
                string operationTitle = $"Stage Action Executed: {actionName}";
                string operationDescription = $"Action '{actionName}' ({actionType}) has been executed on stage '{stageName}' by {GetOperatorDisplayName()}";

                if (onboardingId.HasValue)
                {
                    operationDescription += " for onboarding";
                }

                operationDescription += $" with result: {executionResult}";

                if (!string.IsNullOrEmpty(executionDetails))
                {
                    operationDescription += $". Details: {executionDetails}";
                }

                if (string.IsNullOrEmpty(extendedData))
                {
                    extendedData = JsonSerializer.Serialize(new
                    {
                        StageId = stageId,
                        StageName = stageName,
                        OnboardingId = onboardingId,
                        ActionName = actionName,
                        ActionType = actionType,
                        ExecutionResult = executionResult,
                        ExecutionDetails = executionDetails,
                        ExecutedAt = FormatUsDateTime(DateTimeOffset.UtcNow)
                    });
                }

                return await LogOperationAsync(
                    OperationTypeEnum.StageActionExecution,
                    BusinessModuleEnum.Stage,
                    stageId,
                    onboardingId,
                    stageId,
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData
                );
            }
            catch (Exception ex)
            {
                _stageLogger.LogError(ex, "Failed to log stage action execution for stage {StageId}, action {ActionName}", stageId, actionName);
                return false;
            }
        }

        #endregion

        #region Portal Permission Logging

        /// <summary>
        /// Log stage portal permission change (Available in Customer Portal)
        /// </summary>
        public async Task<bool> LogStagePortalPermissionChangeAsync(long stageId, string stageName, bool beforeVisibleInPortal, bool afterVisibleInPortal, string beforePermission, string afterPermission, long? workflowId = null, string extendedData = null)
        {
            try
            {
                var beforeData = JsonSerializer.Serialize(new
                {
                    VisibleInPortal = beforeVisibleInPortal,
                    PortalPermission = beforePermission
                });

                var afterData = JsonSerializer.Serialize(new
                {
                    VisibleInPortal = afterVisibleInPortal,
                    PortalPermission = afterPermission
                });

                var changedFields = new List<string>();
                if (beforeVisibleInPortal != afterVisibleInPortal)
                    changedFields.Add("VisibleInPortal");
                if (beforePermission != afterPermission)
                    changedFields.Add("PortalPermission");

                string operationTitle = $"Stage Portal Settings Changed: {stageName}";
                string operationDescription = BuildPortalPermissionChangeDescription(
                    stageName,
                    beforeVisibleInPortal,
                    afterVisibleInPortal,
                    beforePermission,
                    afterPermission
                );

                if (string.IsNullOrEmpty(extendedData))
                {
                    extendedData = JsonSerializer.Serialize(new
                    {
                        StageId = stageId,
                        StageName = stageName,
                        WorkflowId = workflowId,
                        PortalVisibilityChanged = beforeVisibleInPortal != afterVisibleInPortal,
                        PortalPermissionChanged = beforePermission != afterPermission,
                        UpdatedAt = FormatUsDateTime(DateTimeOffset.UtcNow)
                    });
                }

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
                    changedFields: JsonSerializer.Serialize(changedFields),
                    extendedData: extendedData
                );
            }
            catch (Exception ex)
            {
                _stageLogger.LogError(ex, "Failed to log stage portal permission change for stage {StageId}", stageId);
                return false;
            }
        }

        #endregion

        #region Component Change Logging

        /// <summary>
        /// Log stage components change
        /// </summary>
        public async Task<bool> LogStageComponentsChangeAsync(long stageId, string stageName, string beforeComponentsJson, string afterComponentsJson, long? workflowId = null, string extendedData = null)
        {
            try
            {
                if (!HasMeaningfulValueChangeEnhanced(beforeComponentsJson, afterComponentsJson))
                {
                    _stageLogger.LogDebug("Skipping components change log for stage {StageId} as there's no meaningful change", stageId);
                    return true;
                }

                var beforeData = JsonSerializer.Serialize(new { ComponentsJson = beforeComponentsJson });
                var afterData = JsonSerializer.Serialize(new { ComponentsJson = afterComponentsJson });
                var changedFields = new List<string> { "ComponentsJson" };

                string operationTitle = $"Stage Components Changed: {stageName}";
                string operationDescription = BuildComponentsChangeDescription(
                    stageName,
                    beforeComponentsJson,
                    afterComponentsJson
                );

                if (string.IsNullOrEmpty(extendedData))
                {
                    extendedData = JsonSerializer.Serialize(new
                    {
                        StageId = stageId,
                        StageName = stageName,
                        WorkflowId = workflowId,
                        ComponentsChanged = true,
                        UpdatedAt = FormatUsDateTime(DateTimeOffset.UtcNow)
                    });
                }

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
                    changedFields: JsonSerializer.Serialize(changedFields),
                    extendedData: extendedData
                );
            }
            catch (Exception ex)
            {
                _stageLogger.LogError(ex, "Failed to log stage components change for stage {StageId}", stageId);
                return false;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Build enhanced stage operation description with detailed change information
        /// </summary>
        private async Task<string> BuildEnhancedStageOperationDescriptionAsync(
            string stageName,
            string operationAction,
            string beforeData = null,
            string afterData = null,
            List<string> changedFields = null,
            long? workflowId = null)
        {
            var description = $"Stage '{stageName}' has been {operationAction.ToLower()} by {GetOperatorDisplayName()}";

            if (workflowId.HasValue)
            {
                description += " in associated workflow";
            }

            // Add specific change details instead of just field names
            if (!string.IsNullOrEmpty(beforeData) && !string.IsNullOrEmpty(afterData))
            {
                // If changedFields is empty, try to detect ComponentsJson changes
                if (changedFields?.Any() == true)
                {
                    var changeDetails = await GetStageSpecificChangeDetailsAsync(beforeData, afterData, changedFields);
                    if (!string.IsNullOrEmpty(changeDetails))
                    {
                        description += $". {changeDetails}";
                    }
                }
                else
                {
                    // Auto-detect ComponentsJson changes when changedFields is empty
                    var beforeJson = JsonSerializer.Deserialize<JsonElement>(beforeData);
                    var afterJson = JsonSerializer.Deserialize<JsonElement>(afterData);

                    if (beforeJson.TryGetProperty("ComponentsJson", out var beforeComponents) &&
                        afterJson.TryGetProperty("ComponentsJson", out var afterComponents))
                    {
                        var beforeComponentsStr = beforeComponents.ValueKind == JsonValueKind.Null ? null : beforeComponents.GetString();
                        var afterComponentsStr = afterComponents.ValueKind == JsonValueKind.Null ? null : afterComponents.GetString();

                        // Check if ComponentsJson changed
                        if (beforeComponentsStr != afterComponentsStr)
                        {
                            var componentsChange = GetComponentsChangeDetails(beforeComponentsStr ?? string.Empty, afterComponentsStr ?? string.Empty);
                            if (!string.IsNullOrEmpty(componentsChange))
                            {
                                description += $". {componentsChange}";
                            }
                        }
                    }
                }
            }
            else if (changedFields?.Any() == true)
            {
                // Fallback to field names if no before/after data
                description += $". Changed fields: {string.Join(", ", changedFields)}";
            }

            return description;
        }

        /// <summary>
        /// Get stage-specific change details from before and after data (async version)
        /// </summary>
        private async Task<string> GetStageSpecificChangeDetailsAsync(string beforeData, string afterData, List<string> changedFields)
        {
            try
            {
                var beforeJson = JsonSerializer.Deserialize<Dictionary<string, object>>(beforeData);
                var afterJson = JsonSerializer.Deserialize<Dictionary<string, object>>(afterData);

                var changeList = new List<string>();

                foreach (var field in changedFields) // Show all changed fields
                {
                    if (beforeJson.TryGetValue(field, out var beforeValue) &&
                        afterJson.TryGetValue(field, out var afterValue))
                    {
                        if (field.Equals("ComponentsJson", StringComparison.OrdinalIgnoreCase))
                        {
                            var beforeJsonStr = beforeValue?.ToString() ?? string.Empty;
                            var afterJsonStr = afterValue?.ToString() ?? string.Empty;

                            _stageLogger.LogDebug("Processing ComponentsJson change - Before: {BeforeJson}, After: {AfterJson}",
                                string.IsNullOrEmpty(beforeJsonStr) ? "null" : beforeJsonStr.Substring(0, Math.Min(100, beforeJsonStr.Length)),
                                string.IsNullOrEmpty(afterJsonStr) ? "null" : afterJsonStr.Substring(0, Math.Min(100, afterJsonStr.Length)));

                            // Use GetComponentsChangeDetails from base class to get detailed information
                            var componentsChange = GetComponentsChangeDetails(beforeJsonStr, afterJsonStr);
                            _stageLogger.LogDebug("Generated components change description: {ComponentsChange}", componentsChange);
                            if (!string.IsNullOrEmpty(componentsChange))
                            {
                                changeList.Add(componentsChange);
                            }
                        }
                        else if (field.Equals("VisibleInPortal", StringComparison.OrdinalIgnoreCase))
                        {
                            var beforeStr = beforeValue?.ToString()?.ToLower() == "true" ? "visible" : "hidden";
                            var afterStr = afterValue?.ToString()?.ToLower() == "true" ? "visible" : "hidden";
                            changeList.Add($"portal visibility from {beforeStr} to {afterStr}");
                        }
                        else if (field.Equals("PortalPermission", StringComparison.OrdinalIgnoreCase))
                        {
                            var beforeStr = beforeValue?.ToString() ?? "null";
                            var afterStr = afterValue?.ToString() ?? "null";
                            changeList.Add($"portal permission from {beforeStr} to {afterStr}");
                        }
                        else if (field.Equals("DefaultAssignee", StringComparison.OrdinalIgnoreCase))
                        {
                            var beforeJsonStr = beforeValue?.ToString() ?? string.Empty;
                            var afterJsonStr = afterValue?.ToString() ?? string.Empty;

                            // Use the base class method for assignee change details (now properly async)
                            var assigneeChange = await GetAssigneeChangeDetailsAsync(beforeJsonStr, afterJsonStr);
                            changeList.Add($"DefaultAssignee: {assigneeChange}");
                        }
                        else if (field.Equals("CoAssignees", StringComparison.OrdinalIgnoreCase))
                        {
                            var beforeJsonStr = beforeValue?.ToString() ?? string.Empty;
                            var afterJsonStr = afterValue?.ToString() ?? string.Empty;

                            // Use the base class method for co-assignee change details
                            var coAssigneeChange = await GetAssigneeChangeDetailsAsync(beforeJsonStr, afterJsonStr);
                            changeList.Add($"CoAssignees: {coAssigneeChange}");
                        }
                        else if (field.Equals("AttachmentManagementNeeded", StringComparison.OrdinalIgnoreCase))
                        {
                            var beforeBool = beforeValue?.ToString()?.ToLower() == "true";
                            var afterBool = afterValue?.ToString()?.ToLower() == "true";
                            var beforeStr = beforeBool ? "enabled" : "disabled";
                            var afterStr = afterBool ? "enabled" : "disabled";
                            changeList.Add($"attachment management from {beforeStr} to {afterStr}");
                        }
                        else if (field.Equals("EstimatedDuration", StringComparison.OrdinalIgnoreCase))
                        {
                            var beforeStr = beforeValue?.ToString() ?? "0";
                            var afterStr = afterValue?.ToString() ?? "0";
                            changeList.Add($"estimated duration from {beforeStr} days to {afterStr} days");
                        }
                        else if (field.Equals("Color", StringComparison.OrdinalIgnoreCase))
                        {
                            var beforeStr = beforeValue?.ToString() ?? "";
                            var afterStr = afterValue?.ToString() ?? "";
                            changeList.Add($"color from '{beforeStr}' to '{afterStr}'");
                        }
                        else if (field.Equals("Order", StringComparison.OrdinalIgnoreCase))
                        {
                            var beforeStr = beforeValue?.ToString() ?? "0";
                            var afterStr = afterValue?.ToString() ?? "0";
                            changeList.Add($"order from {beforeStr} to {afterStr}");
                        }
                        else if (field.Equals("ViewPermissionMode", StringComparison.OrdinalIgnoreCase))
                        {
                            var beforeStr = GetViewPermissionModeDisplayName(beforeValue);
                            var afterStr = GetViewPermissionModeDisplayName(afterValue);
                            changeList.Add($"view permission mode from {beforeStr} to {afterStr}");
                        }
                        else if (field.Equals("ViewTeams", StringComparison.OrdinalIgnoreCase))
                        {
                            var beforeTeams = ParseTeamList(beforeValue?.ToString());
                            var afterTeams = ParseTeamList(afterValue?.ToString());
                            // Now properly async
                            var teamChanges = await GetTeamChangesAsync(beforeTeams, afterTeams, "view");
                            if (!string.IsNullOrEmpty(teamChanges))
                            {
                                changeList.Add(teamChanges);
                            }
                        }
                        else if (field.Equals("OperateTeams", StringComparison.OrdinalIgnoreCase))
                        {
                            var beforeTeams = ParseTeamList(beforeValue?.ToString());
                            var afterTeams = ParseTeamList(afterValue?.ToString());
                            // Now properly async
                            var teamChanges = await GetTeamChangesAsync(beforeTeams, afterTeams, "operate");
                            if (!string.IsNullOrEmpty(teamChanges))
                            {
                                changeList.Add(teamChanges);
                            }
                        }
                        else
                        {
                            var beforeStr = GetDisplayValue(beforeValue, field);
                            var afterStr = GetDisplayValue(afterValue, field);

                            changeList.Add($"{field} from '{beforeStr}' to '{afterStr}'");
                        }
                    }
                }

                return string.Join(", ", changeList);
            }
            catch (Exception ex)
            {
                _stageLogger.LogWarning(ex, "Failed to parse stage-specific change details from JSON data");
            }

            return string.Empty;
        }

        /// <summary>
        /// Build portal permission change description
        /// </summary>
        private string BuildPortalPermissionChangeDescription(
            string stageName,
            bool beforeVisibleInPortal,
            bool afterVisibleInPortal,
            string beforePermission,
            string afterPermission)
        {
            var changes = new List<string>();

            if (beforeVisibleInPortal != afterVisibleInPortal)
            {
                var beforeStr = beforeVisibleInPortal ? "visible" : "hidden";
                var afterStr = afterVisibleInPortal ? "visible" : "hidden";
                changes.Add($"portal visibility changed from {beforeStr} to {afterStr}");
            }

            if (beforePermission != afterPermission)
            {
                changes.Add($"portal permission changed from {beforePermission ?? "null"} to {afterPermission ?? "null"}");
            }

            var description = $"Stage '{stageName}' portal settings have been updated by {GetOperatorDisplayName()}";

            if (changes.Any())
            {
                description += $": {string.Join(", ", changes)}";
            }

            return description;
        }

        /// <summary>
        /// Build components change description
        /// </summary>
        private string BuildComponentsChangeDescription(string stageName, string beforeComponentsJson, string afterComponentsJson)
        {
            var description = $"Stage '{stageName}' components have been updated by {GetOperatorDisplayName()}";

            var changeDetails = GetComponentsChangeDetailsSummary(beforeComponentsJson, afterComponentsJson);
            if (!string.IsNullOrEmpty(changeDetails))
            {
                description += $": {changeDetails}";
            }

            return description;
        }

        /// <summary>
        /// Get components change details summary with detailed component names
        /// </summary>
        private string GetComponentsChangeDetailsSummary(string beforeJson, string afterJson)
        {
            try
            {
                if (string.IsNullOrEmpty(beforeJson) && string.IsNullOrEmpty(afterJson))
                    return "no change in components";

                if (string.IsNullOrEmpty(beforeJson))
                    return "components configuration added";

                if (string.IsNullOrEmpty(afterJson))
                    return "components configuration removed";

                var beforeComponents = ParseStageComponents(beforeJson);
                var afterComponents = ParseStageComponents(afterJson);

                var changes = new List<string>();

                // Compare individual components with detailed information
                var componentChanges = GetDetailedComponentChanges(beforeComponents, afterComponents);
                if (componentChanges.Any())
                {
                    changes.AddRange(componentChanges);
                }

                // Compare component counts
                if (beforeComponents.Count != afterComponents.Count)
                {
                    changes.Add($"component count changed from {beforeComponents.Count} to {afterComponents.Count}");
                }

                // Compare enabled/disabled states
                var beforeEnabled = beforeComponents.Count(c => c.IsEnabled);
                var afterEnabled = afterComponents.Count(c => c.IsEnabled);
                if (beforeEnabled != afterEnabled)
                {
                    changes.Add($"enabled components changed from {beforeEnabled} to {afterEnabled}");
                }

                // Find component key changes
                var beforeKeys = beforeComponents.Select(c => c.Key).ToHashSet();
                var afterKeys = afterComponents.Select(c => c.Key).ToHashSet();

                var addedKeys = afterKeys.Except(beforeKeys).Take(2).ToList();
                var removedKeys = beforeKeys.Except(afterKeys).Take(2).ToList();

                if (addedKeys.Any())
                {
                    changes.Add($"added {string.Join(", ", addedKeys)} components");
                }

                if (removedKeys.Any())
                {
                    changes.Add($"removed {string.Join(", ", removedKeys)} components");
                }

                return changes.Any() ? string.Join(", ", changes) : "components configuration updated";
            }
            catch (Exception ex)
            {
                _stageLogger.LogWarning(ex, "Failed to parse components change details");
                return "components configuration updated";
            }
        }

        /// <summary>
        /// Get detailed changes for individual components with specific content information
        /// </summary>
        private List<string> GetDetailedComponentChanges(List<Domain.Shared.Models.StageComponent> beforeComponents, List<Domain.Shared.Models.StageComponent> afterComponents)
        {
            var changes = new List<string>();

            try
            {
                // Group components by key (case-insensitive) to handle multiple components of the same type
                var beforeGroups = beforeComponents.GroupBy(c => c.Key?.ToLower() ?? "unknown").ToDictionary(g => g.Key, g => g.ToList());
                var afterGroups = afterComponents.GroupBy(c => c.Key?.ToLower() ?? "unknown").ToDictionary(g => g.Key, g => g.ToList());

                // Get all unique component keys from both before and after
                var allKeys = beforeGroups.Keys.Union(afterGroups.Keys).Distinct().ToList();

                // Check each component type for changes
                foreach (var key in allKeys)
                {
                    var beforeComps = beforeGroups.GetValueOrDefault(key, new List<Domain.Shared.Models.StageComponent>());
                    var afterComps = afterGroups.GetValueOrDefault(key, new List<Domain.Shared.Models.StageComponent>());

                    var componentChangeDetails = GetComponentGroupChanges(key, beforeComps, afterComps);
                    if (!string.IsNullOrEmpty(componentChangeDetails))
                    {
                        changes.Add(componentChangeDetails);
                        _stageLogger.LogDebug("Component change detected for {ComponentKey}: {ChangeDetails}", key, componentChangeDetails);
                    }
                    else
                    {
                        _stageLogger.LogDebug("No component change detected for {ComponentKey} (Before: {BeforeCount}, After: {AfterCount})",
                            key, beforeComps.Count, afterComps.Count);
                    }
                }

                // Also detect order changes for components
                var orderChanges = GetComponentOrderChanges(beforeComponents, afterComponents);
                if (!string.IsNullOrEmpty(orderChanges))
                {
                    changes.Add(orderChanges);
                }

                // Detect enabled/disabled state changes for individual components
                var stateChanges = GetComponentStateChanges(beforeComponents, afterComponents);
                if (stateChanges.Any())
                {
                    changes.AddRange(stateChanges);
                }
            }
            catch (Exception ex)
            {
                _stageLogger.LogWarning(ex, "Failed to get detailed component changes");
            }

            return changes;
        }

        /// <summary>
        /// Get order changes for components
        /// </summary>
        private string GetComponentOrderChanges(List<Domain.Shared.Models.StageComponent> beforeComponents, List<Domain.Shared.Models.StageComponent> afterComponents)
        {
            try
            {
                var orderChanges = new List<string>();

                // Create a map of component identifiers to their orders
                foreach (var afterComp in afterComponents)
                {
                    var matchingBefore = FindMatchingComponent(beforeComponents, afterComp);
                    if (matchingBefore != null && matchingBefore.Order != afterComp.Order)
                    {
                        var componentName = GetComponentDisplayName(afterComp);
                        orderChanges.Add($"{componentName} order: {matchingBefore.Order} → {afterComp.Order}");
                    }
                }

                if (orderChanges.Any())
                {
                    return $"reordered: {string.Join("; ", orderChanges.Take(3))}{(orderChanges.Count > 3 ? ", etc." : "")}";
                }
            }
            catch (Exception ex)
            {
                _stageLogger.LogWarning(ex, "Failed to get component order changes");
            }

            return string.Empty;
        }

        /// <summary>
        /// Get enabled/disabled state changes for individual components
        /// </summary>
        private List<string> GetComponentStateChanges(List<Domain.Shared.Models.StageComponent> beforeComponents, List<Domain.Shared.Models.StageComponent> afterComponents)
        {
            var changes = new List<string>();

            try
            {
                foreach (var afterComp in afterComponents)
                {
                    var matchingBefore = FindMatchingComponent(beforeComponents, afterComp);
                    if (matchingBefore != null && matchingBefore.IsEnabled != afterComp.IsEnabled)
                    {
                        var componentName = GetComponentDisplayName(afterComp);
                        var stateChange = afterComp.IsEnabled ? "enabled" : "disabled";
                        changes.Add($"{componentName} {stateChange}");
                    }
                }
            }
            catch (Exception ex)
            {
                _stageLogger.LogWarning(ex, "Failed to get component state changes");
            }

            return changes;
        }

        /// <summary>
        /// Find a matching component from the before list based on key and content
        /// </summary>
        private Domain.Shared.Models.StageComponent FindMatchingComponent(List<Domain.Shared.Models.StageComponent> components, Domain.Shared.Models.StageComponent target)
        {
            var targetKeyLower = target.Key?.ToLower();

            // First try to match by key and specific IDs
            switch (targetKeyLower)
            {
                case "checklist":
                    var targetChecklistIds = target.ChecklistIds ?? new List<long>();
                    return components.FirstOrDefault(c =>
                        c.Key?.ToLower() == "checklist" &&
                        (c.ChecklistIds ?? new List<long>()).SequenceEqual(targetChecklistIds));

                case "questionnaires":
                    var targetQuestionnaireIds = target.QuestionnaireIds ?? new List<long>();
                    return components.FirstOrDefault(c =>
                        c.Key?.ToLower() == "questionnaires" &&
                        (c.QuestionnaireIds ?? new List<long>()).SequenceEqual(targetQuestionnaireIds));

                case "quicklink":
                    var targetQuickLinkIds = target.QuickLinkIds ?? new List<long>();
                    return components.FirstOrDefault(c =>
                        c.Key?.ToLower() == "quicklink" &&
                        (c.QuickLinkIds ?? new List<long>()).SequenceEqual(targetQuickLinkIds));

                case "fields":
                    var targetFields = target.StaticFields ?? new List<StaticFieldConfig>();
                    var targetFieldIds = targetFields.Select(f => f.Id).ToList();
                    return components.FirstOrDefault(c =>
                        c.Key?.ToLower() == "fields" &&
                        (c.StaticFields ?? new List<StaticFieldConfig>()).Select(f => f.Id).SequenceEqual(targetFieldIds));

                default:
                    // For other types (like files), match by key only (case-insensitive)
                    return components.FirstOrDefault(c => c.Key?.ToLower() == targetKeyLower);
            }
        }

        /// <summary>
        /// Get a display name for a component
        /// </summary>
        private string GetComponentDisplayName(Domain.Shared.Models.StageComponent component)
        {
            switch (component.Key?.ToLower())
            {
                case "checklist":
                    var checklistName = component.ChecklistNames?.FirstOrDefault();
                    return !string.IsNullOrEmpty(checklistName) ? $"checklist '{checklistName}'" : "checklist";

                case "questionnaires":
                    var questionnaireName = component.QuestionnaireNames?.FirstOrDefault();
                    return !string.IsNullOrEmpty(questionnaireName) ? $"questionnaire '{questionnaireName}'" : "questionnaire";

                case "quicklink":
                    var quickLinkName = component.QuickLinkNames?.FirstOrDefault();
                    return !string.IsNullOrEmpty(quickLinkName) ? $"quick link '{quickLinkName}'" : "quick link";

                case "fields":
                    var fieldCount = component.StaticFields?.Count ?? 0;
                    return fieldCount > 0 ? $"fields ({fieldCount} fields)" : "fields";

                case "files":
                    return "files";

                default:
                    return component.Key ?? "unknown";
            }
        }

        /// <summary>
        /// Get changes for a group of components of the same type
        /// </summary>
        private string GetComponentGroupChanges(string componentKey, List<Domain.Shared.Models.StageComponent> beforeComps, List<Domain.Shared.Models.StageComponent> afterComps)
        {
            try
            {
                var changes = new List<string>();

                // Handle different scenarios
                if (!beforeComps.Any() && afterComps.Any())
                {
                    // Components were added
                    var componentDetails = GetComponentGroupContentDetails(componentKey, afterComps);
                    if (!string.IsNullOrEmpty(componentDetails))
                    {
                        changes.Add($"added {componentKey}: {componentDetails}");
                    }
                    else
                    {
                        changes.Add($"added {componentKey} component(s)");
                    }
                }
                else if (beforeComps.Any() && !afterComps.Any())
                {
                    // Components were removed
                    changes.Add($"removed all {componentKey} components");
                }
                else if (beforeComps.Any() && afterComps.Any())
                {
                    // Components were modified - analyze the specific changes
                    var modificationDetails = GetComponentGroupModificationDetails(componentKey, beforeComps, afterComps);
                    if (!string.IsNullOrEmpty(modificationDetails))
                    {
                        changes.Add($"{componentKey}: {modificationDetails}");
                    }
                }

                return string.Join(", ", changes);
            }
            catch (Exception ex)
            {
                _stageLogger.LogWarning(ex, "Failed to get component group changes for {ComponentKey}", componentKey);
                return string.Empty;
            }
        }

        /// <summary>
        /// Get content details for a group of components of the same type
        /// </summary>
        private string GetComponentGroupContentDetails(string componentKey, List<Domain.Shared.Models.StageComponent> components)
        {
            try
            {
                switch (componentKey?.ToLower())
                {
                    case "fields":
                        var allFields = components.SelectMany(c => c.StaticFields ?? new List<StaticFieldConfig>()).Select(f => f.Id).Distinct().ToList();
                        if (allFields.Any())
                        {
                            return $"{allFields.Count} static fields ({string.Join(", ", allFields.Take(5))}{(allFields.Count > 5 ? $" +{allFields.Count - 5} more" : "")})";
                        }
                        break;

                    case "checklist":
                        var allChecklistNames = components.SelectMany(c => c.ChecklistNames ?? new List<string>()).Distinct().ToList();
                        if (allChecklistNames.Any())
                        {
                            return $"{allChecklistNames.Count} checklists ({string.Join(", ", allChecklistNames.Select(n => $"'{n}'"))})";
                        }
                        var allChecklistIds = components.SelectMany(c => c.ChecklistIds ?? new List<long>()).Distinct().ToList();
                        if (allChecklistIds.Any())
                        {
                            return $"{allChecklistIds.Count} checklists";
                        }
                        break;

                    case "questionnaires":
                        var allQuestionnaireNames = components.SelectMany(c => c.QuestionnaireNames ?? new List<string>()).Distinct().ToList();
                        if (allQuestionnaireNames.Any())
                        {
                            return $"{allQuestionnaireNames.Count} questionnaires ({string.Join(", ", allQuestionnaireNames.Select(n => $"'{n}'"))})";
                        }
                        var allQuestionnaireIds = components.SelectMany(c => c.QuestionnaireIds ?? new List<long>()).Distinct().ToList();
                        if (allQuestionnaireIds.Any())
                        {
                            return $"{allQuestionnaireIds.Count} questionnaires";
                        }
                        break;

                    case "quicklink":
                        var allQuickLinkNames = components.SelectMany(c => c.QuickLinkNames ?? new List<string>()).Distinct().ToList();
                        if (allQuickLinkNames.Any())
                        {
                            return $"{allQuickLinkNames.Count} quick links ({string.Join(", ", allQuickLinkNames.Select(n => $"'{n}'"))})";
                        }
                        var allQuickLinkIds = components.SelectMany(c => c.QuickLinkIds ?? new List<long>()).Distinct().ToList();
                        if (allQuickLinkIds.Any())
                        {
                            return $"{allQuickLinkIds.Count} quick links";
                        }
                        break;

                    case "files":
                        // For files component, we always return something meaningful
                        var enabledCount = components.Count(c => c.IsEnabled);
                        if (enabledCount > 0)
                        {
                            return "file management";
                        }
                        return "file management (disabled)";

                    default:
                        // For unknown component types
                        return $"{components.Count} {componentKey} component(s)";
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _stageLogger.LogWarning(ex, "Failed to get component group content details for {ComponentKey}", componentKey);
                return string.Empty;
            }
        }

        /// <summary>
        /// Get modification details for a group of components of the same type
        /// </summary>
        private string GetComponentGroupModificationDetails(string componentKey, List<Domain.Shared.Models.StageComponent> beforeComps, List<Domain.Shared.Models.StageComponent> afterComps)
        {
            try
            {
                var changes = new List<string>();

                switch (componentKey?.ToLower())
                {
                    case "fields":
                        var beforeFields = beforeComps.SelectMany(c => c.StaticFields ?? new List<StaticFieldConfig>()).Select(f => f.Id).Distinct().ToList();
                        var afterFields = afterComps.SelectMany(c => c.StaticFields ?? new List<StaticFieldConfig>()).Select(f => f.Id).Distinct().ToList();

                        var addedFields = afterFields.Except(beforeFields).ToList();
                        var removedFields = beforeFields.Except(afterFields).ToList();

                        if (addedFields.Any())
                        {
                            changes.Add($"added fields: {string.Join(", ", addedFields.Take(5))}{(addedFields.Count > 5 ? $" (+{addedFields.Count - 5} more)" : "")}");
                        }
                        if (removedFields.Any())
                        {
                            changes.Add($"removed fields: {string.Join(", ", removedFields.Take(5))}{(removedFields.Count > 5 ? $" (+{removedFields.Count - 5} more)" : "")}");
                        }
                        break;

                    case "checklist":
                        var beforeChecklistNames = beforeComps.SelectMany(c => c.ChecklistNames ?? new List<string>()).Distinct().ToList();
                        var afterChecklistNames = afterComps.SelectMany(c => c.ChecklistNames ?? new List<string>()).Distinct().ToList();

                        var addedChecklists = afterChecklistNames.Except(beforeChecklistNames).ToList();
                        var removedChecklists = beforeChecklistNames.Except(afterChecklistNames).ToList();

                        if (addedChecklists.Any())
                        {
                            changes.Add($"added checklists: {string.Join(", ", addedChecklists.Select(n => $"'{n}'"))}{(addedChecklists.Count > 3 ? $" (+{addedChecklists.Count - 3} more)" : "")}");
                        }
                        if (removedChecklists.Any())
                        {
                            changes.Add($"removed checklists: {string.Join(", ", removedChecklists.Select(n => $"'{n}'"))}{(removedChecklists.Count > 3 ? $" (+{removedChecklists.Count - 3} more)" : "")}");
                        }
                        break;

                    case "questionnaires":
                        var beforeQuestionnaireNames = beforeComps.SelectMany(c => c.QuestionnaireNames ?? new List<string>()).Distinct().ToList();
                        var afterQuestionnaireNames = afterComps.SelectMany(c => c.QuestionnaireNames ?? new List<string>()).Distinct().ToList();

                        var addedQuestionnaires = afterQuestionnaireNames.Except(beforeQuestionnaireNames).ToList();
                        var removedQuestionnaires = beforeQuestionnaireNames.Except(afterQuestionnaireNames).ToList();

                        if (addedQuestionnaires.Any())
                        {
                            changes.Add($"added questionnaires: {string.Join(", ", addedQuestionnaires.Select(n => $"'{n}'"))}{(addedQuestionnaires.Count > 3 ? $" (+{addedQuestionnaires.Count - 3} more)" : "")}");
                        }
                        if (removedQuestionnaires.Any())
                        {
                            changes.Add($"removed questionnaires: {string.Join(", ", removedQuestionnaires.Select(n => $"'{n}'"))}{(removedQuestionnaires.Count > 3 ? $" (+{removedQuestionnaires.Count - 3} more)" : "")}");
                        }
                        break;

                    case "quicklink":
                        var beforeQuickLinkNames = beforeComps.SelectMany(c => c.QuickLinkNames ?? new List<string>()).Distinct().ToList();
                        var afterQuickLinkNames = afterComps.SelectMany(c => c.QuickLinkNames ?? new List<string>()).Distinct().ToList();

                        var addedQuickLinks = afterQuickLinkNames.Except(beforeQuickLinkNames).ToList();
                        var removedQuickLinks = beforeQuickLinkNames.Except(afterQuickLinkNames).ToList();

                        if (addedQuickLinks.Any())
                        {
                            changes.Add($"added quick links: {string.Join(", ", addedQuickLinks.Select(n => $"'{n}'"))}{(addedQuickLinks.Count > 3 ? $" (+{addedQuickLinks.Count - 3} more)" : "")}");
                        }
                        if (removedQuickLinks.Any())
                        {
                            changes.Add($"removed quick links: {string.Join(", ", removedQuickLinks.Select(n => $"'{n}'"))}{(removedQuickLinks.Count > 3 ? $" (+{removedQuickLinks.Count - 3} more)" : "")}");
                        }
                        break;

                    case "files":
                        // For files component, check if enabled status changed
                        var beforeFilesEnabled = beforeComps.Any(c => c.IsEnabled);
                        var afterFilesEnabled = afterComps.Any(c => c.IsEnabled);

                        if (beforeFilesEnabled != afterFilesEnabled)
                        {
                            var statusChange = afterFilesEnabled ? "enabled" : "disabled";
                            changes.Add($"file management {statusChange}");
                        }
                        break;

                    default:
                        // For unknown component types, just report count changes
                        if (beforeComps.Count != afterComps.Count)
                        {
                            changes.Add($"{componentKey} count changed from {beforeComps.Count} to {afterComps.Count}");
                        }
                        break;
                }

                return string.Join(", ", changes);
            }
            catch (Exception ex)
            {
                _stageLogger.LogWarning(ex, "Failed to get component group modification details for {ComponentKey}", componentKey);
                return string.Empty;
            }
        }

        /// <summary>
        /// Get content details for a component (what it contains)
        /// </summary>
        private string GetComponentContentDetails(Domain.Shared.Models.StageComponent component)
        {
            var details = new List<string>();

            try
            {
                switch (component.Key?.ToLower())
                {
                    case "fields":
                        if (component.StaticFields?.Any() == true)
                        {
                            var fieldIds = component.StaticFields.Select(f => f.Id).Take(3);
                            details.Add($"{component.StaticFields.Count} static fields ({string.Join(", ", fieldIds)}{(component.StaticFields.Count > 3 ? ", ..." : "")})");
                        }
                        break;

                    case "checklist":
                        if (component.ChecklistNames?.Any() == true)
                        {
                            details.Add($"{component.ChecklistNames.Count} checklists ({string.Join(", ", component.ChecklistNames.Take(2).Select(n => $"'{n}'"))}{(component.ChecklistNames.Count > 2 ? ", ..." : "")})");
                        }
                        else if (component.ChecklistIds?.Any() == true)
                        {
                            details.Add($"{component.ChecklistIds.Count} checklists");
                        }
                        break;

                    case "questionnaires":
                        if (component.QuestionnaireNames?.Any() == true)
                        {
                            details.Add($"{component.QuestionnaireNames.Count} questionnaires ({string.Join(", ", component.QuestionnaireNames.Take(2).Select(n => $"'{n}'"))}{(component.QuestionnaireNames.Count > 2 ? ", ..." : "")})");
                        }
                        else if (component.QuestionnaireIds?.Any() == true)
                        {
                            details.Add($"{component.QuestionnaireIds.Count} questionnaires");
                        }
                        break;

                    case "quicklink":
                        if (component.QuickLinkNames?.Any() == true)
                        {
                            details.Add($"{component.QuickLinkNames.Count} quick links ({string.Join(", ", component.QuickLinkNames.Take(2).Select(n => $"'{n}'"))}{(component.QuickLinkNames.Count > 2 ? ", ..." : "")})");
                        }
                        else if (component.QuickLinkIds?.Any() == true)
                        {
                            details.Add($"{component.QuickLinkIds.Count} quick links");
                        }
                        break;

                    case "files":
                        details.Add("file management");
                        break;
                }

                if (component.IsEnabled)
                {
                    details.Add("enabled");
                }
                else
                {
                    details.Add("disabled");
                }
            }
            catch (Exception ex)
            {
                _stageLogger.LogWarning(ex, "Failed to get component content details for {ComponentKey}", component.Key);
            }

            return string.Join(", ", details);
        }

        /// <summary>
        /// Get modification details between two versions of the same component
        /// </summary>
        private List<string> GetComponentModificationDetails(Domain.Shared.Models.StageComponent before, Domain.Shared.Models.StageComponent after)
        {
            var changes = new List<string>();

            try
            {
                // Check enabled/disabled state change
                if (before.IsEnabled != after.IsEnabled)
                {
                    changes.Add(after.IsEnabled ? "enabled" : "disabled");
                }

                // Check order change
                if (before.Order != after.Order)
                {
                    changes.Add($"order changed from {before.Order} to {after.Order}");
                }

                // Check content changes based on component type
                switch (before.Key?.ToLower())
                {
                    case "fields":
                        var fieldChanges = GetFieldComponentChanges(before, after);
                        if (fieldChanges.Any())
                        {
                            changes.AddRange(fieldChanges);
                        }
                        break;

                    case "checklist":
                        var checklistChanges = GetChecklistComponentChanges(before, after);
                        if (checklistChanges.Any())
                        {
                            changes.AddRange(checklistChanges);
                        }
                        break;

                    case "questionnaires":
                        var questionnaireChanges = GetQuestionnaireComponentChanges(before, after);
                        if (questionnaireChanges.Any())
                        {
                            changes.AddRange(questionnaireChanges);
                        }
                        break;

                    case "quicklink":
                        var quickLinkChanges = GetQuickLinkComponentChanges(before, after);
                        if (quickLinkChanges.Any())
                        {
                            changes.AddRange(quickLinkChanges);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _stageLogger.LogWarning(ex, "Failed to get component modification details for {ComponentKey}", before.Key);
            }

            return changes;
        }

        /// <summary>
        /// Get changes for fields component
        /// </summary>
        private List<string> GetFieldComponentChanges(Domain.Shared.Models.StageComponent before, Domain.Shared.Models.StageComponent after)
        {
            var changes = new List<string>();

            var beforeFields = before.StaticFields ?? new List<StaticFieldConfig>();
            var afterFields = after.StaticFields ?? new List<StaticFieldConfig>();

            var beforeFieldIds = beforeFields.Select(f => f.Id).ToList();
            var afterFieldIds = afterFields.Select(f => f.Id).ToList();

            var addedFields = afterFieldIds.Except(beforeFieldIds).ToList();
            var removedFields = beforeFieldIds.Except(afterFieldIds).ToList();

            if (addedFields.Any())
            {
                changes.Add($"added fields: {string.Join(", ", addedFields.Take(3))}{(addedFields.Count > 3 ? $" (+{addedFields.Count - 3} more)" : "")}");
            }

            if (removedFields.Any())
            {
                changes.Add($"removed fields: {string.Join(", ", removedFields.Take(3))}{(removedFields.Count > 3 ? $" (+{removedFields.Count - 3} more)" : "")}");
            }

            return changes;
        }

        /// <summary>
        /// Get changes for checklist component
        /// </summary>
        private List<string> GetChecklistComponentChanges(Domain.Shared.Models.StageComponent before, Domain.Shared.Models.StageComponent after)
        {
            var changes = new List<string>();

            var beforeNames = before.ChecklistNames ?? new List<string>();
            var afterNames = after.ChecklistNames ?? new List<string>();

            var addedNames = afterNames.Except(beforeNames).ToList();
            var removedNames = beforeNames.Except(afterNames).ToList();

            if (addedNames.Any())
            {
                changes.Add($"added checklists: {string.Join(", ", addedNames.Take(2).Select(n => $"'{n}'"))}{(addedNames.Count > 2 ? $" (+{addedNames.Count - 2} more)" : "")}");
            }

            if (removedNames.Any())
            {
                changes.Add($"removed checklists: {string.Join(", ", removedNames.Take(2).Select(n => $"'{n}'"))}{(removedNames.Count > 2 ? $" (+{removedNames.Count - 2} more)" : "")}");
            }

            return changes;
        }

        /// <summary>
        /// Get changes for questionnaires component
        /// </summary>
        private List<string> GetQuestionnaireComponentChanges(Domain.Shared.Models.StageComponent before, Domain.Shared.Models.StageComponent after)
        {
            var changes = new List<string>();

            var beforeNames = before.QuestionnaireNames ?? new List<string>();
            var afterNames = after.QuestionnaireNames ?? new List<string>();

            var addedNames = afterNames.Except(beforeNames).ToList();
            var removedNames = beforeNames.Except(afterNames).ToList();

            if (addedNames.Any())
            {
                changes.Add($"added questionnaires: {string.Join(", ", addedNames.Take(2).Select(n => $"'{n}'"))}{(addedNames.Count > 2 ? $" (+{addedNames.Count - 2} more)" : "")}");
            }

            if (removedNames.Any())
            {
                changes.Add($"removed questionnaires: {string.Join(", ", removedNames.Take(2).Select(n => $"'{n}'"))}{(removedNames.Count > 2 ? $" (+{removedNames.Count - 2} more)" : "")}");
            }

            return changes;
        }

        /// <summary>
        /// Get changes for quick link component
        /// </summary>
        private List<string> GetQuickLinkComponentChanges(Domain.Shared.Models.StageComponent before, Domain.Shared.Models.StageComponent after)
        {
            var changes = new List<string>();

            var beforeNames = before.QuickLinkNames ?? new List<string>();
            var afterNames = after.QuickLinkNames ?? new List<string>();

            var addedNames = afterNames.Except(beforeNames).ToList();
            var removedNames = beforeNames.Except(afterNames).ToList();

            if (addedNames.Any())
            {
                changes.Add($"added quick links: {string.Join(", ", addedNames.Take(2).Select(n => $"'{n}'"))}{(addedNames.Count > 2 ? $" (+{addedNames.Count - 2} more)" : "")}");
            }

            if (removedNames.Any())
            {
                changes.Add($"removed quick links: {string.Join(", ", removedNames.Take(2).Select(n => $"'{n}'"))}{(removedNames.Count > 2 ? $" (+{removedNames.Count - 2} more)" : "")}");
            }

            return changes;
        }

        /// <summary>
        /// Parse stage components from JSON string
        /// </summary>
        private List<Domain.Shared.Models.StageComponent> ParseStageComponents(string componentsJson)
        {
            try
            {
                if (string.IsNullOrEmpty(componentsJson))
                    return new List<Domain.Shared.Models.StageComponent>();

                // Handle double JSON encoding scenario
                // First, try to deserialize as a string (which contains the actual JSON array)
                string actualJsonArray = componentsJson;

                // Check if it's double-encoded (starts and ends with quotes)
                if (componentsJson.StartsWith("\"") && componentsJson.EndsWith("\""))
                {
                    try
                    {
                        // Deserialize the outer string to get the inner JSON array string
                        actualJsonArray = JsonSerializer.Deserialize<string>(componentsJson);
                        _stageLogger.LogDebug("Successfully unwrapped double-encoded ComponentsJson");
                    }
                    catch (Exception innerEx)
                    {
                        _stageLogger.LogWarning(innerEx, "Failed to unwrap double-encoded ComponentsJson, trying direct parsing");
                        actualJsonArray = componentsJson;
                    }
                }

                // Now parse the actual JSON array
                return JsonSerializer.Deserialize<List<Domain.Shared.Models.StageComponent>>(actualJsonArray)
                       ?? new List<Domain.Shared.Models.StageComponent>();
            }
            catch (Exception ex)
            {
                _stageLogger.LogWarning(ex, "Failed to parse ComponentsJson: {ComponentsJson}", componentsJson);
                return new List<Domain.Shared.Models.StageComponent>();
            }
        }

        /// <summary>
        /// Format date time in US format (MM/dd/yyyy hh:mm tt)
        /// </summary>
        private string FormatUsDateTime(DateTimeOffset dateTime)
        {
            return dateTime.ToString("MM/dd/yyyy hh:mm tt", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
        }

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

        #region Permission Helper Methods

        /// <summary>
        /// Parse team list from JSON string (handles double-encoded JSON)
        /// </summary>
        private List<string> ParseTeamList(string teamsJson)
        {
            if (string.IsNullOrWhiteSpace(teamsJson))
            {
                return new List<string>();
            }

            try
            {
                var trimmedData = teamsJson.Trim();

                // Handle double-encoded JSON string (e.g., "\"[\\\"123\\\",\\\"456\\\"]\"")
                // First, try to deserialize as a JSON string to get the actual JSON array string
                if (trimmedData.StartsWith("\"") && trimmedData.EndsWith("\""))
                {
                    try
                    {
                        var unescapedJson = JsonSerializer.Deserialize<string>(trimmedData);
                        if (!string.IsNullOrWhiteSpace(unescapedJson))
                        {
                            trimmedData = unescapedJson;
                            _stageLogger.LogDebug("Unescaped double-encoded team JSON: {UnescapedJson}", trimmedData);
                        }
                    }
                    catch
                    {
                        // If deserialization fails, continue with original data
                        _stageLogger.LogDebug("Failed to unescape as double-encoded JSON, using original data");
                    }
                }

                // Try to parse as JSON array
                if (trimmedData.StartsWith("["))
                {
                    var teams = JsonSerializer.Deserialize<List<string>>(trimmedData);
                    if (teams != null)
                    {
                        _stageLogger.LogDebug("Successfully parsed {Count} teams from JSON array", teams.Count);
                        return teams;
                    }
                }

                // Fallback: treat as comma-separated string
                var teamList = trimmedData.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();

                _stageLogger.LogDebug("Parsed {Count} teams from comma-separated string", teamList.Count);
                return teamList;
            }
            catch (Exception ex)
            {
                _stageLogger.LogWarning(ex, "Failed to parse team list: {TeamsJson}", teamsJson);
                return new List<string>();
            }
        }

        /// <summary>
        /// Get team changes description (async version with team name resolution)
        /// </summary>
        private async Task<string> GetTeamChangesAsync(List<string> beforeTeams, List<string> afterTeams, string permissionType)
        {
            var changes = new List<string>();

            var addedTeams = afterTeams.Except(beforeTeams).ToList();
            var removedTeams = beforeTeams.Except(afterTeams).ToList();

            // Get friendly permission type name for display
            var permissionTypeDisplay = permissionType.ToLower() == "view" ? "View Teams" : "Operate Teams";

            // Get team names for all changed teams
            var allChangedTeams = addedTeams.Concat(removedTeams).Distinct().ToList();
            var teamNameMap = new Dictionary<string, string>();

            if (allChangedTeams.Any())
            {
                try
                {
                    // Get tenant ID from UserContext (works in background tasks)
                    var tenantId = _userContext?.TenantId ?? "999";
                    _stageLogger.LogDebug("Using TenantId: {TenantId} for fetching team names", tenantId);

                    teamNameMap = await _userService.GetTeamNamesByIdsAsync(allChangedTeams, tenantId);
                    _stageLogger.LogDebug("Fetched {Count} team names for team change details", teamNameMap.Count);
                }
                catch (Exception ex)
                {
                    _stageLogger.LogWarning(ex, "Failed to fetch team names for team change details. Using IDs instead.");
                }
            }

            if (addedTeams.Any())
            {
                var addedNames = addedTeams
                    .Select(id => teamNameMap.GetValueOrDefault(id, id))
                    .ToList();

                if (addedNames.Count == 1)
                {
                    changes.Add($"added {addedNames[0]} to {permissionTypeDisplay}");
                }
                else
                {
                    changes.Add($"added {string.Join(", ", addedNames)} to {permissionTypeDisplay}");
                }
            }

            if (removedTeams.Any())
            {
                var removedNames = removedTeams
                    .Select(id => teamNameMap.GetValueOrDefault(id, id))
                    .ToList();

                if (removedNames.Count == 1)
                {
                    changes.Add($"removed {removedNames[0]} from {permissionTypeDisplay}");
                }
                else
                {
                    changes.Add($"removed {string.Join(", ", removedNames)} from {permissionTypeDisplay}");
                }
            }

            return string.Join(", ", changes);
        }

        #endregion
    }
}