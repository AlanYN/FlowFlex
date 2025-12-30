using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices.OW.ChangeLog;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FlowFlex.Application.Services.OW.ChangeLog
{
    /// <summary>
    /// Legacy adapter for IOperationChangeLogService - minimal implementation for backward compatibility
    /// DEPRECATED: Use specialized services instead (IChecklistLogService, IQuestionnaireLogService, etc.)
    /// </summary>
    public class OperationChangeLogServiceLegacyAdapter : IOperationChangeLogService
    {
        private readonly IChecklistLogService _checklistLogService;
        private readonly IQuestionnaireLogService _questionnaireLogService;
        private readonly IWorkflowLogService _workflowLogService;
        private readonly IStageLogService _stageLogService;
        private readonly IActionLogService _actionLogService;
        private readonly IOnboardingLogService _onboardingLogService;
        private readonly ILogAggregationService _logAggregationService;
        private readonly IOperationChangeLogRepository _operationChangeLogRepository;
        private readonly ILogger<OperationChangeLogServiceLegacyAdapter> _logger;

        public OperationChangeLogServiceLegacyAdapter(
            IChecklistLogService checklistLogService,
            IQuestionnaireLogService questionnaireLogService,
            IWorkflowLogService workflowLogService,
            IStageLogService stageLogService,
            IActionLogService actionLogService,
            IOnboardingLogService onboardingLogService,
            ILogAggregationService logAggregationService,
            IOperationChangeLogRepository operationChangeLogRepository,
            ILogger<OperationChangeLogServiceLegacyAdapter> logger)
        {
            _checklistLogService = checklistLogService;
            _questionnaireLogService = questionnaireLogService;
            _workflowLogService = workflowLogService;
            _stageLogService = stageLogService;
            _actionLogService = actionLogService;
            _onboardingLogService = onboardingLogService;
            _logAggregationService = logAggregationService;
            _operationChangeLogRepository = operationChangeLogRepository;
            _logger = logger;
        }

        #region Most Used Methods (Implemented)

        public async Task<bool> LogChecklistTaskCompleteAsync(long taskId, string taskName, long onboardingId, long? stageId, string completionNotes = null, int actualHours = 0)
        {
            return await _checklistLogService.LogChecklistTaskCompleteAsync(taskId, taskName, onboardingId, stageId, completionNotes, actualHours);
        }

        public async Task<bool> LogChecklistTaskUncompleteAsync(long taskId, string taskName, long onboardingId, long? stageId, string reason = null)
        {
            return await _checklistLogService.LogChecklistTaskUncompleteAsync(taskId, taskName, onboardingId, stageId, reason);
        }

        public async Task<bool> LogQuestionnaireAnswerSubmitAsync(long answerId, long onboardingId, long stageId, long? questionnaireId, string beforeData = null, string afterData = null, bool isUpdate = false)
        {
            return await _questionnaireLogService.LogQuestionnaireAnswerSubmitAsync(answerId, onboardingId, stageId, questionnaireId, beforeData, afterData, isUpdate);
        }

        public async Task<bool> LogFileUploadAsync(long fileId, string fileName, long onboardingId, long? stageId, long fileSize, string contentType, string category)
        {
            return await _questionnaireLogService.LogFileUploadAsync(fileId, fileName, onboardingId, stageId, fileSize, contentType, category);
        }

        public async Task<bool> LogFileUploadAsync(long fileId, string fileName, long onboardingId, long? stageId, long fileSize, string contentType, string category, long operatorId, string operatorName, string tenantId)
        {
            return await _questionnaireLogService.LogFileUploadAsync(fileId, fileName, onboardingId, stageId, fileSize, contentType, category, operatorId, operatorName, tenantId);
        }

        public async Task<bool> LogFileDeleteAsync(long fileId, string fileName, long onboardingId, long? stageId, string reason = null)
        {
            return await _questionnaireLogService.LogFileDeleteAsync(fileId, fileName, onboardingId, stageId, reason);
        }

        #endregion

        #region Less Used Methods (Placeholder Implementation)

        public async Task<bool> LogStaticFieldValueChangeAsync(long fieldValueId, string fieldName, long onboardingId, long stageId, string beforeData, string afterData, List<string> changedFields, string fieldLabel = null)
        {
            _logger.LogWarning("LogStaticFieldValueChangeAsync called - consider migrating to IQuestionnaireLogService");
            return await _questionnaireLogService.LogStaticFieldValueChangeAsync(fieldValueId, fieldName, onboardingId, stageId, beforeData, afterData, changedFields, fieldLabel);
        }

        public async Task<bool> LogFileUpdateAsync(long fileId, string fileName, long onboardingId, long? stageId, string beforeData, string afterData, List<string> changedFields)
        {
            _logger.LogWarning("LogFileUpdateAsync called - consider migrating to IQuestionnaireLogService");
            return await _questionnaireLogService.LogFileUpdateAsync(fileId, fileName, onboardingId, stageId, beforeData, afterData, changedFields);
        }

        #endregion

        #region Not Implemented Methods (Throw NotImplementedException)

        public Task<bool> LogOperationAsync(
            OperationTypeEnum operationType,
            BusinessModuleEnum businessModule,
            long businessId,
            long? onboardingId,
            long? stageId,
            string operationTitle,
            string operationDescription,
            string beforeData = null,
            string afterData = null,
            List<string> changedFields = null,
            string extendedData = null,
            OperationStatusEnum operationStatus = OperationStatusEnum.Success,
            string errorMessage = null)
        {
            _logger.LogWarning("LogOperationAsync called - this is a legacy method. Consider using specialized services instead.");

            // Route to appropriate specialized service based on business module
            return businessModule switch
            {
                BusinessModuleEnum.Checklist => _checklistLogService.LogOperationAsync(
                    operationType, businessModule, businessId, onboardingId, stageId,
                    operationTitle, operationDescription, null, beforeData, afterData,
                    changedFields?.FirstOrDefault(), extendedData),
                BusinessModuleEnum.ChecklistTask => _checklistLogService.LogOperationAsync(
                    operationType, businessModule, businessId, onboardingId, stageId,
                    operationTitle, operationDescription, null, beforeData, afterData,
                    changedFields?.FirstOrDefault(), extendedData),
                BusinessModuleEnum.Questionnaire => _questionnaireLogService.LogOperationAsync(
                    operationType, businessModule, businessId, onboardingId, stageId,
                    operationTitle, operationDescription, null, beforeData, afterData,
                    changedFields?.FirstOrDefault(), extendedData),
                BusinessModuleEnum.QuestionnaireAnswer => _questionnaireLogService.LogOperationAsync(
                    operationType, businessModule, businessId, onboardingId, stageId,
                    operationTitle, operationDescription, null, beforeData, afterData,
                    changedFields?.FirstOrDefault(), extendedData),
                BusinessModuleEnum.Workflow => _workflowLogService.LogOperationAsync(
                    operationType, businessModule, businessId, onboardingId, stageId,
                    operationTitle, operationDescription, null, beforeData, afterData,
                    changedFields?.FirstOrDefault(), extendedData),
                BusinessModuleEnum.Stage => _stageLogService.LogOperationAsync(
                    operationType, businessModule, businessId, onboardingId, stageId,
                    operationTitle, operationDescription, null, beforeData, afterData,
                    changedFields?.FirstOrDefault(), extendedData),
                _ => throw new NotSupportedException($"Business module {businessModule} is not supported by the legacy adapter.")
            };
        }

        public Task<PagedResult<OperationChangeLogOutputDto>> GetOperationLogsAsync(long? onboardingId = null, long? stageId = null, OperationTypeEnum? operationType = null, int pageIndex = 1, int pageSize = 20, bool includeCache = true)
        {
            _logger.LogWarning("GetOperationLogsAsync called - consider migrating to ILogAggregationService.GetAggregatedLogsAsync");
            var operationTypes = operationType.HasValue ? new List<OperationTypeEnum> { operationType.Value } : null;
            return _logAggregationService.GetAggregatedLogsAsync(onboardingId, stageId, null, operationTypes, null, null, pageIndex, pageSize);
        }

        public Task<PagedResult<OperationChangeLogOutputDto>> GetOperationLogsByTaskAsync(long taskId, long? onboardingId = null, OperationTypeEnum? operationType = null, int pageIndex = 1, int pageSize = 20, bool includeCache = true)
        {
            _logger.LogWarning("GetOperationLogsByTaskAsync called - consider migrating to IChecklistLogService.GetChecklistTaskLogsAsync");
            return _checklistLogService.GetChecklistTaskLogsAsync(taskId, onboardingId, pageIndex, pageSize, includeCache);
        }

        public Task<PagedResult<OperationChangeLogOutputDto>> GetOperationLogsByQuestionAsync(long questionId, long? onboardingId = null, OperationTypeEnum? operationType = null, int pageIndex = 1, int pageSize = 20, bool includeCache = true)
        {
            _logger.LogWarning("GetOperationLogsByQuestionAsync called - consider migrating to IQuestionnaireLogService.GetQuestionLogsAsync");
            return _questionnaireLogService.GetQuestionLogsAsync(questionId, onboardingId, pageIndex, pageSize, includeCache);
        }

        public Task<PagedResult<OperationChangeLogOutputDto>> GetOperationLogsByStageComponentsAsync(long stageId, long? onboardingId = null, OperationTypeEnum? operationType = null, int pageIndex = 1, int pageSize = 20, bool includeCache = true)
        {
            _logger.LogWarning("GetOperationLogsByStageComponentsAsync called - consider migrating to ILogAggregationService.GetAggregatedLogsAsync");

            var operationTypes = operationType.HasValue ? new List<OperationTypeEnum> { operationType.Value } : null;

            return _logAggregationService.GetAggregatedLogsAsync(
                onboardingId: onboardingId,
                stageId: stageId,
                businessModules: null, // Get logs from all modules for stage components
                operationTypes: operationTypes,
                startDate: null,
                endDate: null,
                pageIndex: pageIndex,
                pageSize: pageSize
            );
        }

        public Task<PagedResult<OperationChangeLogOutputDto>> GetOperationLogsByStageComponentsOptimizedAsync(long stageId, long? onboardingId = null, OperationTypeEnum? operationType = null, int pageIndex = 1, int pageSize = 20, bool includeCache = true)
        {
            _logger.LogWarning("GetOperationLogsByStageComponentsOptimizedAsync called - consider migrating to ILogAggregationService.GetAggregatedLogsAsync");

            var operationTypes = operationType.HasValue ? new List<OperationTypeEnum> { operationType.Value } : null;

            return _logAggregationService.GetAggregatedLogsAsync(
                onboardingId: onboardingId,
                stageId: stageId,
                businessModules: null, // Get logs from all modules for stage components
                operationTypes: operationTypes,
                startDate: null,
                endDate: null,
                pageIndex: pageIndex,
                pageSize: pageSize
            );
        }

        public Task<PagedResult<OperationChangeLogOutputDto>> GetLogsByBusinessAsync(string businessModule, long businessId, int pageIndex = 1, int pageSize = 20)
        {
            _logger.LogWarning("GetLogsByBusinessAsync called - consider migrating to specialized services");

            // Parse business module
            if (!Enum.TryParse<BusinessModuleEnum>(businessModule, true, out var moduleEnum))
            {
                return Task.FromResult(new PagedResult<OperationChangeLogOutputDto>());
            }

            return _logAggregationService.GetLogsByBusinessIdsAsync(
                businessIds: new List<long> { businessId },
                businessModule: moduleEnum,
                onboardingId: null,
                pageIndex: pageIndex,
                pageSize: pageSize
            );
        }

        public Task<PagedResult<OperationChangeLogOutputDto>> GetLogsByBusinessIdAsync(long businessId, int pageIndex = 1, int pageSize = 20)
        {
            _logger.LogWarning("GetLogsByBusinessIdAsync called - consider migrating to ILogAggregationService.GetLogsByBusinessIdsAsync");
            return _logAggregationService.GetLogsByBusinessIdsAsync(new List<long> { businessId }, null, null, pageIndex, pageSize);
        }

        public async Task<PagedResult<OperationChangeLogOutputDto>> GetLogsByBusinessIdWithTypeAsync(long businessId, BusinessTypeEnum? businessType = null, int pageIndex = 1, int pageSize = 20)
        {
            _logger.LogWarning("GetLogsByBusinessIdWithTypeAsync called - consider migrating to specialized services");

            try
            {
                // If no business type specified, get all logs for this business ID
                if (!businessType.HasValue)
                {
                    return await _logAggregationService.GetLogsByBusinessIdsAsync(new List<long> { businessId }, null, null, pageIndex, pageSize);
                }

                // Route to appropriate specialized service based on business type
                return businessType.Value switch
                {
                    BusinessTypeEnum.Workflow => await GetWorkflowWithRelatedLogsAsync(businessId, pageIndex, pageSize),
                    BusinessTypeEnum.Checklist => await _checklistLogService.GetChecklistLogsAsync(businessId, pageIndex, pageSize),
                    BusinessTypeEnum.ChecklistTask => await _checklistLogService.GetChecklistTaskLogsAsync(businessId, null, pageIndex, pageSize, true),
                    BusinessTypeEnum.Questionnaire => await GetQuestionnaireWithRelatedLogsAsync(businessId, pageIndex, pageSize),
                    BusinessTypeEnum.Stage => await GetStageLogsAsync(businessId, pageIndex, pageSize),
                    BusinessTypeEnum.Onboarding => await _onboardingLogService.GetOnboardingLogsAsync(businessId, pageIndex, pageSize),
                    BusinessTypeEnum.Action => await _actionLogService.GetActionDefinitionLogsAsync(businessId, pageIndex, pageSize),
                    BusinessTypeEnum.ActionMapping => await _actionLogService.GetActionMappingLogsAsync(businessId, pageIndex, pageSize),
                    _ => throw new NotSupportedException($"Business type {businessType} is not supported.")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get operation logs for business ID {BusinessId} with type {BusinessType}", businessId, businessType);
                return new PagedResult<OperationChangeLogOutputDto>
                {
                    Items = new List<OperationChangeLogOutputDto>(),
                    TotalCount = 0,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };
            }
        }

        /// <summary>
        /// Get stage logs (helper method since IStageLogService doesn't have a direct GetStageLogsAsync method)
        /// </summary>
        private async Task<PagedResult<OperationChangeLogOutputDto>> GetStageLogsAsync(long stageId, int pageIndex, int pageSize)
        {
            // Use LogAggregationService to get stage-specific logs
            var stageModule = new List<BusinessModuleEnum> { BusinessModuleEnum.Stage };
            return await _logAggregationService.GetAggregatedLogsAsync(null, stageId, stageModule, null, null, null, pageIndex, pageSize);
        }

        public Task<PagedResult<OperationChangeLogOutputDto>> GetLogsByBusinessIdsAsync(List<long> businessIds, int pageIndex = 1, int pageSize = 20)
        {
            _logger.LogWarning("GetLogsByBusinessIdsAsync called - consider migrating to ILogAggregationService.GetLogsByBusinessIdsAsync");
            return _logAggregationService.GetLogsByBusinessIdsAsync(businessIds, null, null, pageIndex, pageSize);
        }

        public async Task<Dictionary<string, int>> GetOperationStatisticsAsync(long? onboardingId = null, long? stageId = null)
        {
            _logger.LogWarning("GetOperationStatisticsAsync called - consider migrating to ILogAggregationService.GetComprehensiveStatisticsAsync");

            try
            {
                var comprehensiveStats = await _logAggregationService.GetComprehensiveStatisticsAsync(
                    onboardingId: onboardingId,
                    stageId: stageId,
                    startDate: null,
                    endDate: null
                );

                // Convert to simple string-int dictionary format
                var result = new Dictionary<string, int>();

                foreach (var kvp in comprehensiveStats)
                {
                    if (kvp.Value is int intValue)
                    {
                        result[kvp.Key] = intValue;
                    }
                    else if (kvp.Value is long longValue)
                    {
                        result[kvp.Key] = (int)Math.Min(longValue, int.MaxValue);
                    }
                    else if (int.TryParse(kvp.Value?.ToString(), out var parsedValue))
                    {
                        result[kvp.Key] = parsedValue;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get operation statistics for onboarding {OnboardingId}, stage {StageId}", onboardingId, stageId);
                return new Dictionary<string, int>();
            }
        }

        // All other workflow/stage/checklist/questionnaire methods
        public Task<bool> LogWorkflowCreateAsync(long workflowId, string workflowName, string workflowDescription = null, string afterData = null, string extendedData = null)
        {
            return _workflowLogService.LogWorkflowCreateAsync(workflowId, workflowName, workflowDescription, afterData, extendedData);
        }

        public Task<bool> LogWorkflowUpdateAsync(long workflowId, string workflowName, string beforeData, string afterData, List<string> changedFields, string extendedData = null)
        {
            return _workflowLogService.LogWorkflowUpdateAsync(workflowId, workflowName, beforeData, afterData, changedFields, extendedData);
        }

        public Task<bool> LogWorkflowDeleteAsync(long workflowId, string workflowName, string reason = null, string extendedData = null)
        {
            return _workflowLogService.LogWorkflowDeleteAsync(workflowId, workflowName, reason, extendedData);
        }

        public Task<bool> LogWorkflowPublishAsync(long workflowId, string workflowName, string version = null, string extendedData = null)
        {
            return _workflowLogService.LogWorkflowPublishAsync(workflowId, workflowName, version, extendedData);
        }

        public Task<bool> LogWorkflowUnpublishAsync(long workflowId, string workflowName, string reason = null, string extendedData = null)
        {
            return _workflowLogService.LogWorkflowUnpublishAsync(workflowId, workflowName, reason, extendedData);
        }

        public Task<bool> LogWorkflowActivateAsync(long workflowId, string workflowName, string extendedData = null)
        {
            return _workflowLogService.LogWorkflowActivateAsync(workflowId, workflowName, extendedData);
        }

        public Task<bool> LogWorkflowDeactivateAsync(long workflowId, string workflowName, string reason = null, string extendedData = null)
        {
            return _workflowLogService.LogWorkflowDeactivateAsync(workflowId, workflowName, reason, extendedData);
        }

        // Stage methods - simplified signatures
        public Task<bool> LogStageCreateAsync(long stageId, string stageName, long? workflowId = null, string afterData = null, string extendedData = null)
        {
            // Note: Original interface has different signature than IStageLogService
            _logger.LogWarning("LogStageCreateAsync called - consider migrating to IStageLogService.LogStageCreateAsync");
            return _stageLogService.LogStageCreateAsync(stageId, stageName, workflowId, afterData, extendedData);
        }

        public Task<bool> LogStageUpdateAsync(long stageId, string stageName, string beforeData, string afterData, List<string> changedFields, long? workflowId = null, string extendedData = null)
        {
            _logger.LogWarning("LogStageUpdateAsync called - consider migrating to IStageLogService.LogStageUpdateAsync");
            return _stageLogService.LogStageUpdateAsync(stageId, stageName, beforeData, afterData, changedFields, workflowId, extendedData);
        }

        public Task<bool> LogStageDeleteAsync(long stageId, string stageName, long? workflowId = null, string reason = null, string extendedData = null)
        {
            _logger.LogWarning("LogStageDeleteAsync called - consider migrating to IStageLogService.LogStageDeleteAsync");
            return _stageLogService.LogStageDeleteAsync(stageId, stageName, workflowId, reason, extendedData);
        }

        public Task<bool> LogStageOrderChangeAsync(long stageId, string stageName, int oldOrder, int newOrder, long? workflowId = null, string extendedData = null)
        {
            _logger.LogWarning("LogStageOrderChangeAsync called - consider migrating to IStageLogService.LogStageOrderChangeAsync");
            return _stageLogService.LogStageOrderChangeAsync(stageId, stageName, oldOrder, newOrder, workflowId, extendedData);
        }

        // Checklist methods
        public Task<bool> LogChecklistCreateAsync(long checklistId, string checklistName, string afterData = null, string extendedData = null)
        {
            return _checklistLogService.LogChecklistCreateAsync(checklistId, checklistName, afterData, extendedData);
        }

        public Task<bool> LogChecklistUpdateAsync(long checklistId, string checklistName, string beforeData, string afterData, List<string> changedFields, string extendedData = null)
        {
            return _checklistLogService.LogChecklistUpdateAsync(checklistId, checklistName, beforeData, afterData, changedFields, extendedData);
        }

        public Task<bool> LogChecklistDeleteAsync(long checklistId, string checklistName, string reason = null, string extendedData = null)
        {
            return _checklistLogService.LogChecklistDeleteAsync(checklistId, checklistName, reason, extendedData);
        }

        public Task<bool> LogChecklistTaskCreateAsync(long taskId, string taskName, long checklistId, string afterData = null, string extendedData = null)
        {
            return _checklistLogService.LogChecklistTaskCreateAsync(taskId, taskName, checklistId, afterData, extendedData);
        }

        public Task<bool> LogChecklistTaskUpdateAsync(long taskId, string taskName, string beforeData, string afterData, List<string> changedFields, long checklistId, string extendedData = null)
        {
            return _checklistLogService.LogChecklistTaskUpdateAsync(taskId, taskName, beforeData, afterData, changedFields, checklistId, extendedData);
        }

        public Task<bool> LogChecklistTaskDeleteAsync(long taskId, string taskName, long checklistId, string reason = null, string extendedData = null)
        {
            return _checklistLogService.LogChecklistTaskDeleteAsync(taskId, taskName, checklistId, reason, extendedData);
        }

        // Questionnaire methods
        public Task<bool> LogQuestionnaireCreateAsync(long questionnaireId, string questionnaireName, string afterData = null, string extendedData = null)
        {
            return _questionnaireLogService.LogQuestionnaireCreateAsync(questionnaireId, questionnaireName, afterData, extendedData);
        }

        public Task<bool> LogQuestionnaireUpdateAsync(long questionnaireId, string questionnaireName, string beforeData, string afterData, List<string> changedFields, string extendedData = null)
        {
            return _questionnaireLogService.LogQuestionnaireUpdateAsync(questionnaireId, questionnaireName, beforeData, afterData, changedFields, extendedData);
        }

        public Task<bool> LogQuestionnaireDeleteAsync(long questionnaireId, string questionnaireName, string reason = null, string extendedData = null)
        {
            return _questionnaireLogService.LogQuestionnaireDeleteAsync(questionnaireId, questionnaireName, reason, extendedData);
        }

        public Task<bool> LogQuestionnairePublishAsync(long questionnaireId, string questionnaireName, string version = null, string extendedData = null)
        {
            return _questionnaireLogService.LogQuestionnairePublishAsync(questionnaireId, questionnaireName, version, extendedData);
        }

        public Task<bool> LogQuestionnaireUnpublishAsync(long questionnaireId, string questionnaireName, string reason = null, string extendedData = null)
        {
            return _questionnaireLogService.LogQuestionnaireUnpublishAsync(questionnaireId, questionnaireName, reason, extendedData);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get workflow logs including related stage logs
        /// </summary>
        private async Task<PagedResult<OperationChangeLogOutputDto>> GetWorkflowWithRelatedLogsAsync(long workflowId, int pageIndex, int pageSize)
        {
            try
            {
                _logger.LogDebug("Getting workflow logs with related stages for workflow {WorkflowId}", workflowId);

                // Now using the enhanced WorkflowLogService that includes related stage logs
                return await _workflowLogService.GetWorkflowLogsAsync(workflowId, pageIndex, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get workflow logs with related stages for workflow {WorkflowId}", workflowId);
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        /// <summary>
        /// Get questionnaire and related ActionMapping logs
        /// </summary>
        private async Task<PagedResult<OperationChangeLogOutputDto>> GetQuestionnaireWithRelatedLogsAsync(long questionnaireId, int pageIndex, int pageSize)
        {
            try
            {
                var result = await _operationChangeLogRepository.GetQuestionnaireWithRelatedLogsAsync(questionnaireId, pageIndex, pageSize);

                var outputDtos = result.Items.Select(log => new OperationChangeLogOutputDto
                {
                    Id = log.Id,
                    OperationType = log.OperationType,
                    BusinessModule = log.BusinessModule,
                    BusinessId = log.BusinessId,
                    OnboardingId = log.OnboardingId,
                    StageId = log.StageId,
                    OperationStatus = log.OperationStatus,
                    OperationDescription = log.OperationDescription,
                    OperationTitle = log.OperationTitle,
                    OperationSource = log.OperationSource,
                    BeforeData = log.BeforeData,
                    AfterData = log.AfterData,
                    ChangedFields = ParseChangedFields(log.ChangedFields),
                    OperatorId = log.OperatorId,
                    OperatorName = log.OperatorName,
                    OperationTime = log.OperationTime,
                    IpAddress = log.IpAddress,
                    UserAgent = log.UserAgent,
                    ExtendedData = log.ExtendedData,
                    ErrorMessage = log.ErrorMessage
                }).ToList();

                return new PagedResult<OperationChangeLogOutputDto>
                {
                    Items = outputDtos,
                    TotalCount = result.TotalCount,
                    PageIndex = result.PageIndex,
                    PageSize = result.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get questionnaire with related logs for questionnaire {QuestionnaireId}", questionnaireId);
                return new PagedResult<OperationChangeLogOutputDto>
                {
                    Items = new List<OperationChangeLogOutputDto>(),
                    TotalCount = 0,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };
            }
        }

        /// <summary>
        /// Safely parse ChangedFields from JSON string to List<string>
        /// </summary>
        private static List<string> ParseChangedFields(string changedFieldsJson)
        {
            if (string.IsNullOrEmpty(changedFieldsJson))
            {
                return new List<string>();
            }

            try
            {
                // Try to deserialize as List<string> first
                var result = JsonSerializer.Deserialize<List<string>>(changedFieldsJson);
                return result ?? new List<string>();
            }
            catch (JsonException)
            {
                try
                {
                    // If that fails, try to deserialize as a single string and wrap in list
                    var singleString = JsonSerializer.Deserialize<string>(changedFieldsJson);
                    return string.IsNullOrEmpty(singleString) ? new List<string>() : new List<string> { singleString };
                }
                catch (JsonException)
                {
                    // If all fails, treat the whole string as a single field (fallback)
                    return new List<string> { changedFieldsJson };
                }
            }
        }

        #endregion
    }
}