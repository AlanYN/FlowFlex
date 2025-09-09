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
    /// Checklist-related operation log service
    /// </summary>
    public class ChecklistLogService : BaseOperationLogService, IChecklistLogService
    {
        public ChecklistLogService(
            IOperationChangeLogRepository operationChangeLogRepository,
            ILogger<ChecklistLogService> logger,
            UserContext userContext,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ILogCacheService logCacheService,
            IUserService userService)
            : base(operationChangeLogRepository, logger, userContext, httpContextAccessor, mapper, logCacheService, userService)
        {
        }

        protected override string GetBusinessModuleName() => "Checklist";

        /// <summary>
        /// Log checklist task completion operation
        /// </summary>
        public async Task<bool> LogChecklistTaskCompleteAsync(long taskId, string taskName, long onboardingId, long? stageId, string completionNotes = null, int actualHours = 0)
        {
            try
            {
                var extendedData = new
                {
                    TaskId = taskId,
                    TaskName = taskName,
                    CompletionNotes = completionNotes,
                    ActualHours = actualHours,
                    CompletedAt = DateTimeOffset.UtcNow
                };

                var operationLog = BuildOperationLogEntity(
                    OperationTypeEnum.ChecklistTaskComplete,
                    BusinessModuleEnum.ChecklistTask,
                    taskId,
                    onboardingId,
                    stageId,
                    $"Checklist Task Completed: {taskName}",
                    BuildTaskCompletionDescription(taskName, completionNotes, actualHours),
                    extendedData: JsonSerializer.Serialize(extendedData)
                );

                return await _operationChangeLogRepository.InsertOperationLogAsync(operationLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log checklist task complete operation for task {TaskId}", taskId);
                return false;
            }
        }

        /// <summary>
        /// Log checklist task uncomplete operation
        /// </summary>
        public async Task<bool> LogChecklistTaskUncompleteAsync(long taskId, string taskName, long onboardingId, long? stageId, string reason = null)
        {
            try
            {
                var extendedData = new
                {
                    TaskId = taskId,
                    TaskName = taskName,
                    Reason = reason,
                    UncompletedAt = DateTimeOffset.UtcNow
                };

                var operationLog = BuildOperationLogEntity(
                    OperationTypeEnum.ChecklistTaskUncomplete,
                    BusinessModuleEnum.ChecklistTask,
                    taskId,
                    onboardingId,
                    stageId,
                    $"Checklist Task Uncompleted: {taskName}",
                    BuildTaskUncompletionDescription(taskName, reason),
                    extendedData: JsonSerializer.Serialize(extendedData)
                );

                return await _operationChangeLogRepository.InsertOperationLogAsync(operationLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log checklist task uncomplete operation for task {TaskId}", taskId);
                return false;
            }
        }

        #region Independent Checklist Operations

        /// <summary>
        /// Log checklist create operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogChecklistCreateAsync(long checklistId, string checklistName, string extendedData = null)
        {
            return await LogIndependentOperationAsync(
                OperationTypeEnum.ChecklistCreate,
                BusinessModuleEnum.Checklist,
                checklistId,
                checklistName,
                "Created",
                extendedData: extendedData
            );
        }

        /// <summary>
        /// Log checklist update operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogChecklistUpdateAsync(long checklistId, string checklistName, string beforeData, string afterData, List<string> changedFields, string extendedData = null)
        {
            if (!HasMeaningfulValueChangeEnhanced(beforeData, afterData))
            {
                _logger.LogDebug("Skipping operation log for checklist {ChecklistId} as there's no meaningful value change", checklistId);
                return true;
            }

            return await LogIndependentOperationAsync(
                OperationTypeEnum.ChecklistUpdate,
                BusinessModuleEnum.Checklist,
                checklistId,
                checklistName,
                "Updated",
                beforeData,
                afterData,
                changedFields,
                extendedData: extendedData
            );
        }

        /// <summary>
        /// Log checklist delete operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogChecklistDeleteAsync(long checklistId, string checklistName, string reason = null, string extendedData = null)
        {
            return await LogIndependentOperationAsync(
                OperationTypeEnum.ChecklistDelete,
                BusinessModuleEnum.Checklist,
                checklistId,
                checklistName,
                "Deleted",
                reason: reason,
                extendedData: extendedData
            );
        }

        /// <summary>
        /// Log checklist task create operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogChecklistTaskCreateAsync(long taskId, string taskName, long checklistId, string extendedData = null)
        {
            return await LogIndependentOperationAsync(
                OperationTypeEnum.ChecklistTaskCreate,
                BusinessModuleEnum.Task,
                taskId,
                taskName,
                "Created",
                relatedEntityId: checklistId,
                relatedEntityType: "checklist",
                extendedData: extendedData
            );
        }

        /// <summary>
        /// Log checklist task update operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogChecklistTaskUpdateAsync(long taskId, string taskName, string beforeData, string afterData, List<string> changedFields, long checklistId, string extendedData = null)
        {
            if (!HasMeaningfulValueChangeEnhanced(beforeData, afterData))
            {
                _logger.LogDebug("Skipping operation log for checklist task {TaskId} as there's no meaningful value change", taskId);
                return true;
            }

            return await LogIndependentOperationAsync(
                OperationTypeEnum.ChecklistTaskUpdate,
                BusinessModuleEnum.Task,
                taskId,
                taskName,
                "Updated",
                beforeData,
                afterData,
                changedFields,
                relatedEntityId: checklistId,
                relatedEntityType: "checklist",
                extendedData: extendedData
            );
        }

        /// <summary>
        /// Log checklist task delete operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogChecklistTaskDeleteAsync(long taskId, string taskName, long checklistId, string reason = null, string extendedData = null)
        {
            return await LogIndependentOperationAsync(
                OperationTypeEnum.ChecklistTaskDelete,
                BusinessModuleEnum.Task,
                taskId,
                taskName,
                "Deleted",
                reason: reason,
                relatedEntityId: checklistId,
                relatedEntityType: "checklist",
                extendedData: extendedData
            );
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
                    logs = logs.Where(x => x.BusinessModule == BusinessModuleEnum.Checklist.ToString() ||
                                         x.BusinessModule == BusinessModuleEnum.ChecklistTask.ToString()).ToList();
                }
                else if (onboardingId.HasValue)
                {
                    logs = await _operationChangeLogRepository.GetByOnboardingIdAsync(onboardingId.Value);
                    logs = logs.Where(x => x.BusinessModule == BusinessModuleEnum.Checklist.ToString() ||
                                         x.BusinessModule == BusinessModuleEnum.ChecklistTask.ToString()).ToList();
                }
                else
                {
                    // Get all checklist-related logs
                    var checklistLogs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Checklist.ToString(), 0);
                    var taskLogs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.ChecklistTask.ToString(), 0);
                    logs.AddRange(checklistLogs);
                    logs.AddRange(taskLogs);
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
                _logger.LogError(ex, "Failed to get checklist operation logs from database");
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        /// <summary>
        /// Get checklist logs by checklist ID
        /// </summary>
        public async Task<PagedResult<OperationChangeLogOutputDto>> GetChecklistLogsAsync(long checklistId, int pageIndex = 1, int pageSize = 20)
        {
            try
            {
                // Use the new method that includes related checklist task logs
                var pagedResult = await _operationChangeLogRepository.GetChecklistWithRelatedLogsAsync(checklistId, pageIndex, pageSize);

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
                _logger.LogError(ex, "Failed to get checklist logs for checklist {ChecklistId}", checklistId);
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        /// <summary>
        /// Get checklist task logs by task ID
        /// </summary>
        public async Task<PagedResult<OperationChangeLogOutputDto>> GetChecklistTaskLogsAsync(long taskId, long? onboardingId = null, int pageIndex = 1, int pageSize = 20, bool includeActionExecutions = true)
        {
            try
            {
                var logs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.ChecklistTask.ToString(), taskId);

                if (onboardingId.HasValue)
                {
                    logs = logs.Where(x => x.OnboardingId == onboardingId.Value).ToList();
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
                _logger.LogError(ex, "Failed to get operation logs for task {TaskId}", taskId);
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        /// <summary>
        /// Get checklist operation statistics
        /// </summary>
        public async Task<Dictionary<string, int>> GetChecklistOperationStatisticsAsync(long checklistId)
        {
            try
            {
                var logs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Checklist.ToString(), checklistId);

                return logs.GroupBy(x => x.OperationType)
                          .ToDictionary(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get checklist operation statistics for checklist {ChecklistId}", checklistId);
                return new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// Get checklist task statistics
        /// </summary>
        public async Task<Dictionary<string, int>> GetChecklistTaskStatisticsAsync(long taskId, long? onboardingId = null)
        {
            try
            {
                var logs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.ChecklistTask.ToString(), taskId);

                if (onboardingId.HasValue)
                {
                    logs = logs.Where(x => x.OnboardingId == onboardingId.Value).ToList();
                }

                return logs.GroupBy(x => x.OperationType)
                          .ToDictionary(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get checklist task statistics for task {TaskId}", taskId);
                return new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// Get operation logs by task ID (backward compatibility)
        /// </summary>
        public async Task<PagedResult<OperationChangeLogOutputDto>> GetLogsByTaskAsync(long taskId, long? onboardingId = null, int pageIndex = 1, int pageSize = 20)
        {
            return await GetChecklistTaskLogsAsync(taskId, onboardingId, pageIndex, pageSize, true);
        }

        #region Private Helper Methods

        /// <summary>
        /// Build task completion description
        /// </summary>
        private string BuildTaskCompletionDescription(string taskName, string completionNotes, int actualHours)
        {
            var description = $"Task '{taskName}' has been marked as completed by {GetOperatorDisplayName()}";

            if (!string.IsNullOrEmpty(completionNotes))
            {
                description += $" with notes: {completionNotes}";
            }

            if (actualHours > 0)
            {
                description += $". Actual time spent: {actualHours} hours";
            }

            return description;
        }

        /// <summary>
        /// Build task uncompletion description
        /// </summary>
        private string BuildTaskUncompletionDescription(string taskName, string reason)
        {
            var description = $"Task '{taskName}' has been marked as uncompleted by {GetOperatorDisplayName()}";

            // Remove the "with reason" part for cleaner description
            // if (!string.IsNullOrEmpty(reason))
            // {
            //     description += $" with reason: {reason}";
            // }

            return description;
        }

        // LogIndependentOperationAsync method has been moved to base class to eliminate code duplication

        // BuildIndependentOperationDescription method removed - functionality merged into base class

        // BuildDefaultExtendedData method has been moved to base class to eliminate code duplication

        #endregion
    }
}