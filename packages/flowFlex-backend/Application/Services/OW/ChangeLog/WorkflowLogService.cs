using System.Text.Json;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
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
            ILogCacheService logCacheService)
            : base(operationChangeLogRepository, logger, userContext, httpContextAccessor, mapper, logCacheService)
        {
        }

        protected override string GetBusinessModuleName() => "Workflow";

        #region Independent Workflow Operations

        /// <summary>
        /// Log workflow create operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogWorkflowCreateAsync(long workflowId, string workflowName, string workflowDescription = null, string extendedData = null)
        {
            return await LogIndependentOperationAsync(
                OperationTypeEnum.WorkflowCreate,
                BusinessModuleEnum.Workflow,
                workflowId,
                workflowName,
                "Created",
                description: workflowDescription,
                extendedData: extendedData
            );
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

            return await LogIndependentOperationAsync(
                OperationTypeEnum.WorkflowUpdate,
                BusinessModuleEnum.Workflow,
                workflowId,
                workflowName,
                "Updated",
                beforeData,
                afterData,
                changedFields,
                extendedData: extendedData
            );
        }

        /// <summary>
        /// Log workflow delete operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogWorkflowDeleteAsync(long workflowId, string workflowName, string reason = null, string extendedData = null)
        {
            return await LogIndependentOperationAsync(
                OperationTypeEnum.WorkflowDelete,
                BusinessModuleEnum.Workflow,
                workflowId,
                workflowName,
                "Deleted",
                reason: reason,
                extendedData: extendedData
            );
        }

        /// <summary>
        /// Log workflow publish operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogWorkflowPublishAsync(long workflowId, string workflowName, string version = null, string extendedData = null)
        {
            return await LogIndependentOperationAsync(
                OperationTypeEnum.WorkflowPublish,
                BusinessModuleEnum.Workflow,
                workflowId,
                workflowName,
                "Published",
                version: version,
                extendedData: extendedData
            );
        }

        /// <summary>
        /// Log workflow unpublish operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogWorkflowUnpublishAsync(long workflowId, string workflowName, string reason = null, string extendedData = null)
        {
            return await LogIndependentOperationAsync(
                OperationTypeEnum.WorkflowUnpublish,
                BusinessModuleEnum.Workflow,
                workflowId,
                workflowName,
                "Unpublished",
                reason: reason,
                extendedData: extendedData
            );
        }

        /// <summary>
        /// Log workflow activate operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogWorkflowActivateAsync(long workflowId, string workflowName, string extendedData = null)
        {
            return await LogIndependentOperationAsync(
                OperationTypeEnum.WorkflowActivate,
                BusinessModuleEnum.Workflow,
                workflowId,
                workflowName,
                "Activated",
                extendedData: extendedData
            );
        }

        /// <summary>
        /// Log workflow deactivate operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogWorkflowDeactivateAsync(long workflowId, string workflowName, string reason = null, string extendedData = null)
        {
            return await LogIndependentOperationAsync(
                OperationTypeEnum.WorkflowDeactivate,
                BusinessModuleEnum.Workflow,
                workflowId,
                workflowName,
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
                var logs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Workflow.ToString(), workflowId);

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
        /// Build stage order change description
        /// </summary>
        private string BuildStageOrderChangeDescription(string stageName, int oldOrder, int newOrder, long? workflowId)
        {
            var description = $"Stage '{stageName}' order has been changed from {oldOrder} to {newOrder} by {GetOperatorDisplayName()}";

            if (workflowId.HasValue)
            {
                description += $" in workflow ID {workflowId.Value}";
            }

            return description;
        }

        /// <summary>
        /// Generic helper method for logging independent operations
        /// </summary>
        private async Task<bool> LogIndependentOperationAsync(
            OperationTypeEnum operationType,
            BusinessModuleEnum businessModule,
            long businessId,
            string entityName,
            string operationAction,
            string beforeData = null,
            string afterData = null,
            List<string> changedFields = null,
            string reason = null,
            string version = null,
            string description = null,
            long? relatedEntityId = null,
            string relatedEntityType = null,
            string extendedData = null)
        {
            try
            {
                var operationTitle = $"{businessModule} {operationAction}: {entityName}";
                var operationDescription = BuildIndependentOperationDescription(
                    businessModule, entityName, operationAction, description, relatedEntityId, relatedEntityType, reason, version, changedFields);

                if (string.IsNullOrEmpty(extendedData))
                {
                    extendedData = BuildDefaultExtendedData(businessModule, businessId, entityName, operationAction, relatedEntityId, relatedEntityType, reason, version, changedFields);
                }

                var operationLog = BuildOperationLogEntity(
                    operationType,
                    businessModule,
                    businessId,
                    null, // No onboardingId for independent operations
                    null, // No stageId for independent operations
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
                _logger.LogError(ex, "Failed to log independent {OperationType} operation for {BusinessModule} {BusinessId}", 
                    operationType, businessModule, businessId);
                return false;
            }
        }

        /// <summary>
        /// Build independent operation description
        /// </summary>
        private string BuildIndependentOperationDescription(
            BusinessModuleEnum businessModule,
            string entityName,
            string operationAction,
            string description,
            long? relatedEntityId,
            string relatedEntityType,
            string reason,
            string version,
            List<string> changedFields)
        {
            var operationDescription = $"{businessModule} '{entityName}' has been {operationAction.ToLower()} by {GetOperatorDisplayName()}";

            if (!string.IsNullOrEmpty(description))
            {
                operationDescription += $". Description: {description}";
            }

            if (relatedEntityId.HasValue && !string.IsNullOrEmpty(relatedEntityType))
            {
                operationDescription += $" in {relatedEntityType} ID {relatedEntityId.Value}";
            }

            if (!string.IsNullOrEmpty(version))
            {
                operationDescription += $" as version {version}";
            }

            if (!string.IsNullOrEmpty(reason))
            {
                operationDescription += $" with reason: {reason}";
            }

            if (changedFields?.Any() == true)
            {
                operationDescription += $". Changed fields: {string.Join(", ", changedFields)}";
            }

            return operationDescription;
        }

        /// <summary>
        /// Build default extended data
        /// </summary>
        private string BuildDefaultExtendedData(
            BusinessModuleEnum businessModule,
            long businessId,
            string entityName,
            string operationAction,
            long? relatedEntityId,
            string relatedEntityType,
            string reason,
            string version,
            List<string> changedFields)
        {
            var extendedDataObj = new Dictionary<string, object>
            {
                { $"{businessModule}Id", businessId },
                { $"{businessModule}Name", entityName },
                { $"{operationAction}At", DateTimeOffset.UtcNow }
            };

            if (relatedEntityId.HasValue && !string.IsNullOrEmpty(relatedEntityType))
            {
                extendedDataObj.Add($"{relatedEntityType}Id", relatedEntityId.Value);
            }

            if (!string.IsNullOrEmpty(reason))
            {
                extendedDataObj.Add("Reason", reason);
            }

            if (!string.IsNullOrEmpty(version))
            {
                extendedDataObj.Add("Version", version);
            }

            if (changedFields?.Any() == true)
            {
                extendedDataObj.Add("ChangedFieldsCount", changedFields.Count);
            }

            return JsonSerializer.Serialize(extendedDataObj);
        }

        #endregion
    }
}