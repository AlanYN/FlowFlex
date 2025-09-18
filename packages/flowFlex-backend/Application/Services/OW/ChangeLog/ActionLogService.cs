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
    /// Action-related operation log service
    /// </summary>
    public class ActionLogService : BaseOperationLogService, IActionLogService
    {
        public ActionLogService(
            IOperationChangeLogRepository operationChangeLogRepository,
            ILogger<ActionLogService> logger,
            UserContext userContext,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ILogCacheService logCacheService,
            IUserService userService)
            : base(operationChangeLogRepository, logger, userContext, httpContextAccessor, mapper, logCacheService, userService)
        {
        }

        protected override string GetBusinessModuleName() => "Action";

        /// <summary>
        /// Log action trigger mapping association operation
        /// </summary>
        public async Task<bool> LogActionMappingAssociateAsync(
            long mappingId,
            long actionDefinitionId,
            string actionName,
            string triggerType,
            long triggerSourceId,
            string triggerSourceName,
            string triggerEvent,
            long? workflowId = null,
            long? stageId = null,
            string extendedData = null)
        {
            try
            {
                var extendedDataObj = new
                {
                    MappingId = mappingId,
                    ActionDefinitionId = actionDefinitionId,
                    ActionName = actionName,
                    TriggerType = triggerType,
                    TriggerSourceId = triggerSourceId,
                    TriggerSourceName = triggerSourceName,
                    TriggerEvent = triggerEvent,
                    WorkflowId = workflowId,
                    StageId = stageId,
                    AssociatedAt = DateTimeOffset.UtcNow
                };

                var description = BuildActionMappingAssociationDescription(
                    actionName, triggerType, triggerSourceName, triggerEvent);

                var operationLog = BuildOperationLogEntity(
                    OperationTypeEnum.ActionMappingCreate,
                    BusinessModuleEnum.ActionMapping,
                    mappingId,
                    workflowId,
                    stageId,
                    $"Action Mapping Associated: {actionName} → {triggerType} {triggerSourceName}",
                    description,
                    extendedData: !string.IsNullOrEmpty(extendedData) ? extendedData : JsonSerializer.Serialize(extendedDataObj)
                );

                return await _operationChangeLogRepository.InsertOperationLogAsync(operationLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log action mapping association for mapping {MappingId}", mappingId);
                return false;
            }
        }

        /// <summary>
        /// Log action trigger mapping disassociation operation
        /// </summary>
        public async Task<bool> LogActionMappingDisassociateAsync(
            long mappingId,
            long actionDefinitionId,
            string actionName,
            string triggerType,
            long triggerSourceId,
            string triggerSourceName,
            string triggerEvent,
            long? workflowId = null,
            long? stageId = null,
            bool wasEnabled = true,
            string extendedData = null)
        {
            try
            {
                var extendedDataObj = new
                {
                    MappingId = mappingId,
                    ActionDefinitionId = actionDefinitionId,
                    ActionName = actionName,
                    TriggerType = triggerType,
                    TriggerSourceId = triggerSourceId,
                    TriggerSourceName = triggerSourceName,
                    TriggerEvent = triggerEvent,
                    WorkflowId = workflowId,
                    StageId = stageId,
                    WasEnabled = wasEnabled,
                    DisassociatedAt = DateTimeOffset.UtcNow
                };

                var description = BuildActionMappingDisassociationDescription(
                    actionName, triggerType, triggerSourceName, triggerEvent, wasEnabled);

                var operationLog = BuildOperationLogEntity(
                    OperationTypeEnum.ActionMappingDelete,
                    BusinessModuleEnum.ActionMapping,
                    mappingId,
                    workflowId,
                    stageId,
                    $"Action Mapping Disassociated: {actionName} ← {triggerType} {triggerSourceName}",
                    description,
                    extendedData: !string.IsNullOrEmpty(extendedData) ? extendedData : JsonSerializer.Serialize(extendedDataObj)
                );

                return await _operationChangeLogRepository.InsertOperationLogAsync(operationLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log action mapping disassociation for mapping {MappingId}", mappingId);
                return false;
            }
        }

        /// <summary>
        /// Log action trigger mapping update operation
        /// </summary>
        public async Task<bool> LogActionMappingUpdateAsync(
            long mappingId,
            long actionDefinitionId,
            string actionName,
            string triggerType,
            long triggerSourceId,
            string triggerSourceName,
            string changeDescription,
            string beforeData,
            string afterData,
            List<string> changedFields,
            string extendedData = null)
        {
            try
            {
                var extendedDataObj = new
                {
                    MappingId = mappingId,
                    ActionDefinitionId = actionDefinitionId,
                    ActionName = actionName,
                    TriggerType = triggerType,
                    TriggerSourceId = triggerSourceId,
                    TriggerSourceName = triggerSourceName,
                    ChangeDescription = changeDescription,
                    ChangedFields = changedFields,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                var description = BuildActionMappingUpdateDescription(
                    actionName, triggerType, triggerSourceName, changeDescription);

                var operationLog = BuildOperationLogEntity(
                    OperationTypeEnum.ActionMappingUpdate,
                    BusinessModuleEnum.ActionMapping,
                    mappingId,
                    null, // No onboarding context for status updates
                    null, // No stage context for status updates
                    $"Action Mapping Updated: {actionName} → {triggerType} {triggerSourceName}",
                    description,
                    beforeData,
                    afterData,
                    changedFields,
                    extendedData: !string.IsNullOrEmpty(extendedData) ? extendedData : JsonSerializer.Serialize(extendedDataObj)
                );

                return await _operationChangeLogRepository.InsertOperationLogAsync(operationLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log action mapping update for mapping {MappingId}", mappingId);
                return false;
            }
        }

        /// <summary>
        /// Log action definition create operation
        /// </summary>
        public async Task<bool> LogActionDefinitionCreateAsync(
            long actionDefinitionId,
            string actionName,
            string actionType,
            string extendedData = null)
        {
            return await LogIndependentOperationAsync(
                OperationTypeEnum.ActionDefinitionCreate,
                BusinessModuleEnum.Action,
                actionDefinitionId,
                actionName,
                "Created",
                extendedData: extendedData
            );
        }

        /// <summary>
        /// Log action definition update operation
        /// </summary>
        public async Task<bool> LogActionDefinitionUpdateAsync(
            long actionDefinitionId,
            string actionName,
            string beforeData,
            string afterData,
            List<string> changedFields,
            string extendedData = null)
        {
            if (!HasMeaningfulValueChangeEnhanced(beforeData, afterData))
            {
                _logger.LogDebug("Skipping operation log for action definition {ActionId} as there's no meaningful value change", actionDefinitionId);
                return true;
            }

            return await LogIndependentOperationAsync(
                OperationTypeEnum.ActionDefinitionUpdate,
                BusinessModuleEnum.Action,
                actionDefinitionId,
                actionName,
                "Updated",
                beforeData,
                afterData,
                changedFields,
                extendedData: extendedData
            );
        }

        /// <summary>
        /// Log action definition delete operation
        /// </summary>
        public async Task<bool> LogActionDefinitionDeleteAsync(
            long actionDefinitionId,
            string actionName,
            string reason = null,
            string extendedData = null)
        {
            return await LogIndependentOperationAsync(
                OperationTypeEnum.ActionDefinitionDelete,
                BusinessModuleEnum.Action,
                actionDefinitionId,
                actionName,
                "Deleted",
                reason: reason,
                extendedData: extendedData
            );
        }

        /// <summary>
        /// Get action logs by action definition ID
        /// </summary>
        public async Task<PagedResult<OperationChangeLogOutputDto>> GetActionDefinitionLogsAsync(
            long actionDefinitionId,
            int pageIndex = 1,
            int pageSize = 20)
        {
            try
            {
                var logs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Action.ToString(), actionDefinitionId);

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
                _logger.LogError(ex, "Failed to get action definition logs for action {ActionId}", actionDefinitionId);
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        /// <summary>
        /// Get action mapping logs by mapping ID
        /// </summary>
        public async Task<PagedResult<OperationChangeLogOutputDto>> GetActionMappingLogsAsync(
            long mappingId,
            int pageIndex = 1,
            int pageSize = 20)
        {
            try
            {
                var logs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.ActionMapping.ToString(), mappingId);

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
                _logger.LogError(ex, "Failed to get action mapping logs for mapping {MappingId}", mappingId);
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        /// <summary>
        /// Get action logs by trigger source
        /// </summary>
        public async Task<PagedResult<OperationChangeLogOutputDto>> GetActionLogsByTriggerSourceAsync(
            string triggerType,
            long triggerSourceId,
            long? onboardingId = null,
            long? stageId = null,
            int pageIndex = 1,
            int pageSize = 20)
        {
            try
            {
                List<Domain.Entities.OW.OperationChangeLog> logs;

                if (onboardingId.HasValue && stageId.HasValue)
                {
                    logs = await _operationChangeLogRepository.GetByOnboardingAndStageAsync(onboardingId.Value, stageId.Value);
                    logs = logs.Where(x => x.BusinessModule == BusinessModuleEnum.ActionMapping.ToString()).ToList();
                }
                else if (onboardingId.HasValue)
                {
                    logs = await _operationChangeLogRepository.GetByOnboardingIdAsync(onboardingId.Value);
                    logs = logs.Where(x => x.BusinessModule == BusinessModuleEnum.ActionMapping.ToString()).ToList();
                }
                else
                {
                    logs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.ActionMapping.ToString(), 0);
                }

                // Filter by trigger source information in extended data
                logs = logs.Where(log =>
                {
                    if (string.IsNullOrEmpty(log.ExtendedData)) return false;
                    try
                    {
                        using var document = JsonDocument.Parse(log.ExtendedData);
                        var root = document.RootElement;

                        var logTriggerType = root.TryGetProperty("TriggerType", out var triggerTypeEl) ? triggerTypeEl.GetString() : null;
                        var logTriggerSourceId = root.TryGetProperty("TriggerSourceId", out var triggerSourceIdEl) ? triggerSourceIdEl.GetInt64() : 0;

                        return string.Equals(logTriggerType, triggerType, StringComparison.OrdinalIgnoreCase) &&
                               logTriggerSourceId == triggerSourceId;
                    }
                    catch
                    {
                        return false;
                    }
                }).ToList();

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
                _logger.LogError(ex, "Failed to get action logs by trigger source {TriggerType} {TriggerSourceId}", triggerType, triggerSourceId);
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

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
                    logs = logs.Where(x => x.BusinessModule == BusinessModuleEnum.Action.ToString() ||
                                         x.BusinessModule == BusinessModuleEnum.ActionMapping.ToString()).ToList();
                }
                else if (onboardingId.HasValue)
                {
                    logs = await _operationChangeLogRepository.GetByOnboardingIdAsync(onboardingId.Value);
                    logs = logs.Where(x => x.BusinessModule == BusinessModuleEnum.Action.ToString() ||
                                         x.BusinessModule == BusinessModuleEnum.ActionMapping.ToString()).ToList();
                }
                else
                {
                    // Get all action-related logs
                    var actionLogs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Action.ToString(), 0);
                    var mappingLogs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.ActionMapping.ToString(), 0);
                    logs.AddRange(actionLogs);
                    logs.AddRange(mappingLogs);
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
                _logger.LogError(ex, "Failed to get action operation logs from database");
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Build action mapping association description
        /// </summary>
        private string BuildActionMappingAssociationDescription(
            string actionName,
            string triggerType,
            string triggerSourceName,
            string triggerEvent)
        {
            return $"Action '{actionName}' has been associated with {triggerType.ToLower()} '{triggerSourceName}' to trigger on '{triggerEvent}' event by {GetOperatorDisplayName()}";
        }

        /// <summary>
        /// Build action mapping disassociation description
        /// </summary>
        private string BuildActionMappingDisassociationDescription(
            string actionName,
            string triggerType,
            string triggerSourceName,
            string triggerEvent,
            bool wasEnabled)
        {
            var statusInfo = wasEnabled ? "(was enabled)" : "(was disabled)";
            return $"Action '{actionName}' has been disassociated from {triggerType.ToLower()} '{triggerSourceName}' (was triggered on '{triggerEvent}' event) {statusInfo} by {GetOperatorDisplayName()}";
        }

        /// <summary>
        /// Build action mapping update description
        /// </summary>
        private string BuildActionMappingUpdateDescription(
            string actionName,
            string triggerType,
            string triggerSourceName,
            string changeDescription)
        {
            return $"Action '{actionName}' mapping to {triggerType.ToLower()} '{triggerSourceName}' has been updated by {GetOperatorDisplayName()}. {changeDescription}";
        }

        #endregion
    }
}