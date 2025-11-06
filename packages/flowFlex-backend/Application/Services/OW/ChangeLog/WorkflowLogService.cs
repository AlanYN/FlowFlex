using System.Text.Json;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices.OW.ChangeLog;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Repository.OW;
using AutoMapper;
using Microsoft.AspNetCore.Http;

namespace FlowFlex.Application.Services.OW.ChangeLog
{
    /// <summary>
    /// Workflow-related operation log service
    /// </summary>
    public class WorkflowLogService : BaseOperationLogService, IWorkflowLogService
    {
        public WorkflowLogService(
            IOperationChangeLogRepository operationChangeLogRepository,
            ILogger<WorkflowLogService> logger,
            UserContext userContext,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ILogCacheService logCacheService,
            IUserService userService,
            IOperatorContextService operatorContextService)
            : base(operationChangeLogRepository, logger, userContext, httpContextAccessor, mapper, logCacheService, userService, operatorContextService)
        {
        }

        protected override string GetBusinessModuleName() => "Workflow";

        #region Independent Workflow Operations

        /// <summary>
        /// Log workflow create operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogWorkflowCreateAsync(long workflowId, string workflowName, string workflowDescription = null, string extendedData = null)
        {
            try
            {
                var operationTitle = $"Workflow Created: {workflowName}";
                var operationDescription = $"Workflow '{workflowName}' has been created by {GetOperatorDisplayName()}";

                if (!string.IsNullOrEmpty(workflowDescription))
                {
                    operationDescription += $" with description: {workflowDescription}";
                }

                if (string.IsNullOrEmpty(extendedData))
                {
                    extendedData = JsonSerializer.Serialize(new
                    {
                        WorkflowId = workflowId,
                        WorkflowName = workflowName,
                        Description = workflowDescription,
                        CreatedAt = FormatUsDateTime(DateTimeOffset.UtcNow)
                    });
                }

                return await LogOperationAsync(
                    OperationTypeEnum.WorkflowCreate,
                    BusinessModuleEnum.Workflow,
                    workflowId,
                    null,
                    null,
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log workflow create operation for workflow {WorkflowId}", workflowId);
                return false;
            }
        }

        /// <summary>
        /// Log workflow update operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogWorkflowUpdateAsync(long workflowId, string workflowName, string beforeData, string afterData, List<string> changedFields, string extendedData = null)
        {
            if (!HasMeaningfulValueChangeEnhanced(beforeData, afterData))
            {
                _logger.LogDebug("Skipping operation log for workflow {WorkflowId} as there's no meaningful value change", workflowId);
                return true;
            }

            try
            {
                var operationTitle = $"Workflow Updated: {workflowName}";
                var operationDescription = await BuildEnhancedWorkflowOperationDescriptionAsync(
                    workflowName,
                    "Updated",
                    beforeData,
                    afterData,
                    changedFields
                );

                if (string.IsNullOrEmpty(extendedData))
                {
                    extendedData = JsonSerializer.Serialize(new
                    {
                        WorkflowId = workflowId,
                        WorkflowName = workflowName,
                        ChangedFieldsCount = changedFields?.Count ?? 0,
                        UpdatedAt = FormatUsDateTime(DateTimeOffset.UtcNow)
                    });
                }

                return await LogOperationAsync(
                    OperationTypeEnum.WorkflowUpdate,
                    BusinessModuleEnum.Workflow,
                    workflowId,
                    null,
                    null,
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
                _logger.LogError(ex, "Failed to log workflow update operation for workflow {WorkflowId}", workflowId);
                return false;
            }
        }

        /// <summary>
        /// Log workflow delete operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogWorkflowDeleteAsync(long workflowId, string workflowName, string reason = null, string extendedData = null)
        {
            try
            {
                var operationTitle = $"Workflow Deleted: {workflowName}";
                var operationDescription = $"Workflow '{workflowName}' has been deleted by {GetOperatorDisplayName()}";

                if (!string.IsNullOrEmpty(reason))
                {
                    operationDescription += $" with reason: {reason}";
                }

                if (string.IsNullOrEmpty(extendedData))
                {
                    extendedData = JsonSerializer.Serialize(new
                    {
                        WorkflowId = workflowId,
                        WorkflowName = workflowName,
                        Reason = reason,
                        DeletedAt = FormatUsDateTime(DateTimeOffset.UtcNow)
                    });
                }

                return await LogOperationAsync(
                    OperationTypeEnum.WorkflowDelete,
                    BusinessModuleEnum.Workflow,
                    workflowId,
                    null,
                    null,
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log workflow delete operation for workflow {WorkflowId}", workflowId);
                return false;
            }
        }

        /// <summary>
        /// Log workflow publish operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogWorkflowPublishAsync(long workflowId, string workflowName, string version = null, string extendedData = null)
        {
            try
            {
                var operationTitle = $"Workflow Published: {workflowName}";
                var operationDescription = $"Workflow '{workflowName}' has been published by {GetOperatorDisplayName()}";

                if (!string.IsNullOrEmpty(version))
                {
                    operationDescription += $" as version {version}";
                }

                if (string.IsNullOrEmpty(extendedData))
                {
                    extendedData = JsonSerializer.Serialize(new
                    {
                        WorkflowId = workflowId,
                        WorkflowName = workflowName,
                        Version = version,
                        PublishedAt = FormatUsDateTime(DateTimeOffset.UtcNow)
                    });
                }

                return await LogOperationAsync(
                    OperationTypeEnum.WorkflowPublish,
                    BusinessModuleEnum.Workflow,
                    workflowId,
                    null,
                    null,
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log workflow publish operation for workflow {WorkflowId}", workflowId);
                return false;
            }
        }

        /// <summary>
        /// Log workflow unpublish operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogWorkflowUnpublishAsync(long workflowId, string workflowName, string reason = null, string extendedData = null)
        {
            try
            {
                var operationTitle = $"Workflow Unpublished: {workflowName}";
                var operationDescription = $"Workflow '{workflowName}' has been unpublished by {GetOperatorDisplayName()}";

                if (!string.IsNullOrEmpty(reason))
                {
                    operationDescription += $" with reason: {reason}";
                }

                if (string.IsNullOrEmpty(extendedData))
                {
                    extendedData = JsonSerializer.Serialize(new
                    {
                        WorkflowId = workflowId,
                        WorkflowName = workflowName,
                        Reason = reason,
                        UnpublishedAt = FormatUsDateTime(DateTimeOffset.UtcNow)
                    });
                }

                return await LogOperationAsync(
                    OperationTypeEnum.WorkflowUnpublish,
                    BusinessModuleEnum.Workflow,
                    workflowId,
                    null,
                    null,
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log workflow unpublish operation for workflow {WorkflowId}", workflowId);
                return false;
            }
        }

        /// <summary>
        /// Log workflow activate operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogWorkflowActivateAsync(long workflowId, string workflowName, string extendedData = null)
        {
            return await LogWorkflowStatusChangeAsync(
                workflowId,
                workflowName,
                "inactive",
                "active",
                OperationTypeEnum.WorkflowActivate,
                "Activated",
                extendedData: extendedData
            );
        }

        /// <summary>
        /// Log workflow deactivate operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogWorkflowDeactivateAsync(long workflowId, string workflowName, string reason = null, string extendedData = null)
        {
            return await LogWorkflowStatusChangeAsync(
                workflowId,
                workflowName,
                "active",
                "inactive",
                OperationTypeEnum.WorkflowDeactivate,
                "Deactivated",
                reason: reason,
                extendedData: extendedData
            );
        }

        #endregion

        #region Stage Operations (Independent)

        /// <summary>
        /// Log stage create operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogStageCreateAsync(long stageId, string stageName, long? workflowId = null, string extendedData = null)
        {
            return await LogIndependentOperationAsync(
                OperationTypeEnum.StageCreate,
                BusinessModuleEnum.Stage,
                stageId,
                stageName,
                "Created",
                relatedEntityId: workflowId,
                relatedEntityType: "workflow",
                extendedData: extendedData
            );
        }

        /// <summary>
        /// Log stage update operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogStageUpdateAsync(long stageId, string stageName, string beforeData, string afterData, List<string> changedFields, long? workflowId = null, string extendedData = null)
        {
            if (!HasMeaningfulValueChangeEnhanced(beforeData, afterData))
            {
                _logger.LogDebug("Skipping operation log for stage {StageId} as there's no meaningful value change", stageId);
                return true;
            }

            return await LogIndependentOperationAsync(
                OperationTypeEnum.StageUpdate,
                BusinessModuleEnum.Stage,
                stageId,
                stageName,
                "Updated",
                beforeData,
                afterData,
                changedFields,
                relatedEntityId: workflowId,
                relatedEntityType: "workflow",
                extendedData: extendedData
            );
        }

        /// <summary>
        /// Log stage delete operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogStageDeleteAsync(long stageId, string stageName, long? workflowId = null, string reason = null, string extendedData = null)
        {
            return await LogIndependentOperationAsync(
                OperationTypeEnum.StageDelete,
                BusinessModuleEnum.Stage,
                stageId,
                stageName,
                "Deleted",
                reason: reason,
                relatedEntityId: workflowId,
                relatedEntityType: "workflow",
                extendedData: extendedData
            );
        }

        /// <summary>
        /// Log stage order change operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogStageOrderChangeAsync(long stageId, string stageName, int oldOrder, int newOrder, long? workflowId = null, string extendedData = null)
        {
            try
            {
                var beforeData = JsonSerializer.Serialize(new { order = oldOrder });
                var afterData = JsonSerializer.Serialize(new { order = newOrder });
                var changedFields = new List<string> { "order" };

                var operationTitle = $"Stage Order Changed: {stageName}";
                var operationDescription = BuildStageOrderChangeDescription(stageName, oldOrder, newOrder, workflowId);

                if (string.IsNullOrEmpty(extendedData))
                {
                    extendedData = JsonSerializer.Serialize(new
                    {
                        StageId = stageId,
                        StageName = stageName,
                        WorkflowId = workflowId,
                        OldOrder = oldOrder,
                        NewOrder = newOrder,
                        ChangedAt = DateTimeOffset.UtcNow
                    });
                }

                var operationLog = BuildOperationLogEntity(
                    OperationTypeEnum.StageOrderChange,
                    BusinessModuleEnum.Stage,
                    stageId,
                    null, // No onboardingId for independent operations
                    null, // No parent stageId for stage order change
                    operationTitle,
                    operationDescription,
                    beforeData,
                    afterData,
                    changedFields,
                    extendedData
                );

                return await _operationChangeLogRepository.InsertOperationLogAsync(operationLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log stage order change operation for stage {StageId}", stageId);
                return false;
            }
        }

        #endregion

        /// <summary>
        /// Get operation logs from database (abstract method implementation)
        /// </summary>
        protected override async Task<PagedResult<OperationChangeLogOutputDto>> GetOperationLogsFromDatabaseAsync(
            long? onboardingId,
            long? stageId,
            OperationTypeEnum? operationType,
            int pageIndex,
            int pageSize)
        {
            try
            {
                var logs = new List<Domain.Entities.OW.OperationChangeLog>();

                // Get logs based on filters
                if (onboardingId.HasValue && stageId.HasValue)
                {
                    logs = await _operationChangeLogRepository.GetByOnboardingAndStageAsync(onboardingId.Value, stageId.Value);
                    logs = logs.Where(x => x.BusinessModule == BusinessModuleEnum.Workflow.ToString() ||
                                         x.BusinessModule == BusinessModuleEnum.Stage.ToString()).ToList();
                }
                else if (onboardingId.HasValue)
                {
                    logs = await _operationChangeLogRepository.GetByOnboardingIdAsync(onboardingId.Value);
                    logs = logs.Where(x => x.BusinessModule == BusinessModuleEnum.Workflow.ToString() ||
                                         x.BusinessModule == BusinessModuleEnum.Stage.ToString()).ToList();
                }
                else if (stageId.HasValue)
                {
                    logs = await _operationChangeLogRepository.GetByStageIdAsync(stageId.Value);
                    logs = logs.Where(x => x.BusinessModule == BusinessModuleEnum.Workflow.ToString() ||
                                         x.BusinessModule == BusinessModuleEnum.Stage.ToString()).ToList();
                }
                else
                {
                    // Get all workflow-related logs
                    var workflowLogs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Workflow.ToString(), 0);
                    var stageLogs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Stage.ToString(), 0);
                    logs.AddRange(workflowLogs);
                    logs.AddRange(stageLogs);
                }

                // Apply operation type filter
                if (operationType.HasValue)
                {
                    logs = logs.Where(x => x.OperationType == operationType.ToString()).ToList();
                }

                // Apply pagination
                var totalCount = logs.Count;
                var pagedLogs = logs.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();

                var outputDtos = pagedLogs.Select(MapToOutputDto).ToList();

                return new PagedResult<OperationChangeLogOutputDto>
                {
                    Items = outputDtos,
                    TotalCount = totalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get workflow operation logs from database");
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        /// <summary>
        /// Get workflow logs by workflow ID
        /// </summary>
        public async Task<PagedResult<OperationChangeLogOutputDto>> GetWorkflowLogsAsync(long workflowId, int pageIndex = 1, int pageSize = 20)
        {
            try
            {
                // Use the new method that includes related stage logs
                var pagedResult = await _operationChangeLogRepository.GetWorkflowWithRelatedLogsAsync(workflowId, pageIndex, pageSize);

                var outputDtos = pagedResult.Items.Select(MapToOutputDto).ToList();

                return new PagedResult<OperationChangeLogOutputDto>
                {
                    Items = outputDtos,
                    TotalCount = pagedResult.TotalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get workflow logs for workflow {WorkflowId}", workflowId);
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        /// <summary>
        /// Get workflow operation statistics
        /// </summary>
        public async Task<Dictionary<string, int>> GetWorkflowOperationStatisticsAsync(long workflowId)
        {
            try
            {
                var logs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Workflow.ToString(), workflowId);

                return logs.GroupBy(x => x.OperationType)
                          .ToDictionary(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get workflow operation statistics for workflow {WorkflowId}", workflowId);
                return new Dictionary<string, int>();
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Log workflow status change operation with detailed before/after tracking
        /// </summary>
        private async Task<bool> LogWorkflowStatusChangeAsync(
            long workflowId,
            string workflowName,
            string beforeStatus,
            string afterStatus,
            OperationTypeEnum operationType,
            string operationAction,
            string reason = null,
            string extendedData = null)
        {
            try
            {
                var beforeData = JsonSerializer.Serialize(new { status = beforeStatus });
                var afterData = JsonSerializer.Serialize(new { status = afterStatus });
                var changedFields = new List<string> { "status" };

                var operationTitle = $"Workflow {operationAction}: {workflowName}";
                var operationDescription = BuildWorkflowStatusChangeDescription(workflowName, beforeStatus, afterStatus, operationAction, reason);

                if (string.IsNullOrEmpty(extendedData))
                {
                    extendedData = JsonSerializer.Serialize(new
                    {
                        WorkflowId = workflowId,
                        WorkflowName = workflowName,
                        StatusFrom = beforeStatus,
                        StatusTo = afterStatus,
                        Reason = reason,
                        Timestamp = FormatUsDateTime(DateTimeOffset.UtcNow)
                    });
                }

                return await LogOperationAsync(
                    operationType,
                    BusinessModuleEnum.Workflow,
                    workflowId,
                    null, // No onboardingId for independent operations
                    null, // No parent stageId for workflow operations
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
                _logger.LogError(ex, "Failed to log workflow status change operation for workflow {WorkflowId}", workflowId);
                return false;
            }
        }

        /// <summary>
        /// Build workflow status change description without showing IDs
        /// </summary>
        private string BuildWorkflowStatusChangeDescription(string workflowName, string beforeStatus, string afterStatus, string operationAction, string reason)
        {
            var description = $"Workflow '{workflowName}' has been {operationAction.ToLower()} (status changed from {beforeStatus} to {afterStatus}) by {GetOperatorDisplayName()}";

            if (!string.IsNullOrEmpty(reason))
            {
                description += $" with reason: {reason}";
            }

            return description;
        }

        /// <summary>
        /// Build stage order change description without showing IDs
        /// </summary>
        private string BuildStageOrderChangeDescription(string stageName, int oldOrder, int newOrder, long? workflowId)
        {
            var description = $"Stage '{stageName}' order has been changed from {oldOrder} to {newOrder} by {GetOperatorDisplayName()}";

            if (workflowId.HasValue)
            {
                description += " in associated workflow";
            }

            return description;
        }

        /// <summary>
        /// Build enhanced workflow operation description without showing IDs (async version)
        /// </summary>
        private async Task<string> BuildEnhancedWorkflowOperationDescriptionAsync(
            string workflowName,
            string operationAction,
            string beforeData = null,
            string afterData = null,
            List<string> changedFields = null)
        {
            var description = $"Workflow '{workflowName}' has been {operationAction.ToLower()} by {GetOperatorDisplayName()}";

            // Add specific change details instead of just field names
            if (!string.IsNullOrEmpty(beforeData) && !string.IsNullOrEmpty(afterData) && changedFields?.Any() == true)
            {
                var changeDetails = await GetChangeDetailsAsync(beforeData, afterData, changedFields);
                if (!string.IsNullOrEmpty(changeDetails))
                {
                    description += $". {changeDetails}";
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
        /// Format date time in US format (MM/dd/yyyy hh:mm tt)
        /// </summary>
        private string FormatUsDateTime(DateTimeOffset dateTime)
        {
            return dateTime.ToString("MM/dd/yyyy hh:mm tt", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
        }

        // LogIndependentOperationAsync method has been moved to base class to eliminate code duplication

        // BuildIndependentOperationDescription method removed - functionality merged into base class

        // BuildDefaultExtendedData method has been moved to base class to eliminate code duplication

        #endregion
    }
}