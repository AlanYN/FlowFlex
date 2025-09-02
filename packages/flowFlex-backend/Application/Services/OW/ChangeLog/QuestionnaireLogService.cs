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
    /// Questionnaire-related operation log service
    /// </summary>
    public class QuestionnaireLogService : BaseOperationLogService, IQuestionnaireLogService
    {
        public QuestionnaireLogService(
            IOperationChangeLogRepository operationChangeLogRepository,
            ILogger<QuestionnaireLogService> logger,
            UserContext userContext,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ILogCacheService logCacheService,
            IUserService userService)
            : base(operationChangeLogRepository, logger, userContext, httpContextAccessor, mapper, logCacheService, userService)
        {
        }

        protected override string GetBusinessModuleName() => "Questionnaire";

        /// <summary>
        /// Log questionnaire answer submission operation
        /// </summary>
        public async Task<bool> LogQuestionnaireAnswerSubmitAsync(long answerId, long onboardingId, long stageId, long? questionnaireId, string beforeData = null, string afterData = null, bool isUpdate = false)
        {
            try
            {
                // For updates, check if there's actually a meaningful change
                if (isUpdate && !HasMeaningfulValueChangeEnhanced(beforeData, afterData))
                {
                    _logger.LogDebug("Skipping operation log for questionnaire answer {AnswerId} as there's no meaningful value change", answerId);
                    return true;
                }

                var changedFields = GetChangedFieldsFromJson(beforeData, afterData);

                var extendedData = new
                {
                    AnswerId = answerId,
                    QuestionnaireId = questionnaireId,
                    IsUpdate = isUpdate,
                    SubmittedAt = DateTimeOffset.UtcNow,
                    ChangedFieldsCount = changedFields.Count
                };

                var operationType = isUpdate ? OperationTypeEnum.QuestionnaireAnswerUpdate : OperationTypeEnum.QuestionnaireAnswerSubmit;
                var operationAction = isUpdate ? "Updated" : "Submitted";

                var operationLog = BuildOperationLogEntity(
                    operationType,
                    BusinessModuleEnum.QuestionnaireAnswer,
                    answerId,
                    onboardingId,
                    stageId,
                    $"Questionnaire Answer {operationAction}",
                    BuildAnswerOperationDescription(operationAction, questionnaireId),
                    beforeData,
                    afterData,
                    changedFields,
                    JsonSerializer.Serialize(extendedData)
                );

                return await _operationChangeLogRepository.InsertOperationLogAsync(operationLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log questionnaire answer submit operation for answer {AnswerId}", answerId);
                return false;
            }
        }

        /// <summary>
        /// Log static field value change operation
        /// </summary>
        public async Task<bool> LogStaticFieldValueChangeAsync(long fieldValueId, string fieldName, long onboardingId, long stageId, string beforeData, string afterData, List<string> changedFields, string fieldLabel = null)
        {
            try
            {
                // Check if there's actually a meaningful change
                if (!HasMeaningfulValueChangeEnhanced(beforeData, afterData))
                {
                    _logger.LogDebug("Skipping operation log for field {FieldName} as there's no meaningful value change", fieldName);
                    return true;
                }

                var displayFieldName = !string.IsNullOrEmpty(fieldLabel) ? fieldLabel : fieldName;

                // Convert raw field values to proper JSON format for JSONB storage
                var beforeDataJson = BuildFieldValueJson(beforeData, displayFieldName);
                var afterDataJson = BuildFieldValueJson(afterData, displayFieldName);

                var extendedData = new
                {
                    FieldValueId = fieldValueId,
                    FieldName = fieldName,
                    FieldLabel = fieldLabel,
                    DisplayFieldName = displayFieldName,
                    ChangedFieldsCount = changedFields?.Count ?? 0,
                    ChangedAt = DateTimeOffset.UtcNow
                };

                var operationLog = BuildOperationLogEntity(
                    OperationTypeEnum.StaticFieldValueChange,
                    BusinessModuleEnum.StaticField,
                    fieldValueId,
                    onboardingId,
                    stageId,
                    $"Static Field Value Changed: {displayFieldName}",
                    BuildStaticFieldChangeDescription(displayFieldName, changedFields),
                    beforeDataJson,
                    afterDataJson,
                    changedFields,
                    JsonSerializer.Serialize(extendedData)
                );

                return await _operationChangeLogRepository.InsertOperationLogAsync(operationLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log static field value change operation for field {FieldValueId}", fieldValueId);
                return false;
            }
        }

        /// <summary>
        /// Log file upload operation
        /// </summary>
        public async Task<bool> LogFileUploadAsync(long fileId, string fileName, long onboardingId, long? stageId, long fileSize, string contentType, string category)
        {
            try
            {
                var extendedData = new
                {
                    FileId = fileId,
                    FileName = fileName,
                    FileSize = fileSize,
                    FileSizeFormatted = FormatFileSize(fileSize),
                    ContentType = contentType,
                    Category = category,
                    UploadedAt = DateTimeOffset.UtcNow
                };

                var operationLog = BuildOperationLogEntity(
                    OperationTypeEnum.FileUpload,
                    BusinessModuleEnum.File,
                    fileId,
                    onboardingId,
                    stageId,
                    $"File Uploaded: {fileName}",
                    $"File '{fileName}' has been uploaded successfully by {GetOperatorDisplayName()}",
                    extendedData: JsonSerializer.Serialize(extendedData)
                );

                return await _operationChangeLogRepository.InsertOperationLogAsync(operationLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log file upload operation for file {FileId}", fileId);
                return false;
            }
        }

        /// <summary>
        /// Log file delete operation
        /// </summary>
        public async Task<bool> LogFileDeleteAsync(long fileId, string fileName, long onboardingId, long? stageId, string reason = null)
        {
            try
            {
                var extendedData = new
                {
                    FileId = fileId,
                    FileName = fileName,
                    Reason = reason,
                    DeletedAt = DateTimeOffset.UtcNow
                };

                var operationLog = BuildOperationLogEntity(
                    OperationTypeEnum.FileDelete,
                    BusinessModuleEnum.File,
                    fileId,
                    onboardingId,
                    stageId,
                    $"File Deleted: {fileName}",
                    BuildFileDeleteDescription(fileName, reason),
                    extendedData: JsonSerializer.Serialize(extendedData)
                );

                return await _operationChangeLogRepository.InsertOperationLogAsync(operationLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log file delete operation for file {FileId}", fileId);
                return false;
            }
        }

        /// <summary>
        /// Log file update operation
        /// </summary>
        public async Task<bool> LogFileUpdateAsync(long fileId, string fileName, long onboardingId, long? stageId, string beforeData, string afterData, List<string> changedFields)
        {
            try
            {
                // Check if there's actually a meaningful change
                if (!HasMeaningfulValueChangeEnhanced(beforeData, afterData))
                {
                    _logger.LogDebug("Skipping operation log for file {FileName} as there's no meaningful value change", fileName);
                    return true;
                }

                var extendedData = new
                {
                    FileId = fileId,
                    FileName = fileName,
                    ChangedFieldsCount = changedFields?.Count ?? 0,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                var operationLog = BuildOperationLogEntity(
                    OperationTypeEnum.FileUpdate,
                    BusinessModuleEnum.File,
                    fileId,
                    onboardingId,
                    stageId,
                    $"File Updated: {fileName}",
                    BuildFileUpdateDescription(fileName, changedFields),
                    beforeData,
                    afterData,
                    changedFields,
                    JsonSerializer.Serialize(extendedData)
                );

                return await _operationChangeLogRepository.InsertOperationLogAsync(operationLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log file update operation for file {FileId}", fileId);
                return false;
            }
        }

        #region Independent Questionnaire Operations

        /// <summary>
        /// Log questionnaire create operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogQuestionnaireCreateAsync(long questionnaireId, string questionnaireName, string extendedData = null)
        {
            return await LogIndependentOperationAsync(
                OperationTypeEnum.QuestionnaireCreate,
                BusinessModuleEnum.Questionnaire,
                questionnaireId,
                questionnaireName,
                "Created",
                extendedData: extendedData
            );
        }

        /// <summary>
        /// Log questionnaire update operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogQuestionnaireUpdateAsync(long questionnaireId, string questionnaireName, string beforeData, string afterData, List<string> changedFields, string extendedData = null)
        {
            if (!HasMeaningfulValueChangeEnhanced(beforeData, afterData))
            {
                _logger.LogDebug("Skipping operation log for questionnaire {QuestionnaireId} as there's no meaningful value change", questionnaireId);
                return true;
            }

            return await LogIndependentOperationAsync(
                OperationTypeEnum.QuestionnaireUpdate,
                BusinessModuleEnum.Questionnaire,
                questionnaireId,
                questionnaireName,
                "Updated",
                beforeData,
                afterData,
                changedFields,
                extendedData: extendedData
            );
        }

        /// <summary>
        /// Log questionnaire delete operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogQuestionnaireDeleteAsync(long questionnaireId, string questionnaireName, string reason = null, string extendedData = null)
        {
            return await LogIndependentOperationAsync(
                OperationTypeEnum.QuestionnaireDelete,
                BusinessModuleEnum.Questionnaire,
                questionnaireId,
                questionnaireName,
                "Deleted",
                reason: reason,
                extendedData: extendedData
            );
        }

        /// <summary>
        /// Log questionnaire publish operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogQuestionnairePublishAsync(long questionnaireId, string questionnaireName, string version = null, string extendedData = null)
        {
            return await LogIndependentOperationAsync(
                OperationTypeEnum.QuestionnairePublish,
                BusinessModuleEnum.Questionnaire,
                questionnaireId,
                questionnaireName,
                "Published",
                version: version,
                extendedData: extendedData
            );
        }

        /// <summary>
        /// Log questionnaire unpublish operation (independent of onboarding)
        /// </summary>
        public async Task<bool> LogQuestionnaireUnpublishAsync(long questionnaireId, string questionnaireName, string reason = null, string extendedData = null)
        {
            return await LogIndependentOperationAsync(
                OperationTypeEnum.QuestionnaireUnpublish,
                BusinessModuleEnum.Questionnaire,
                questionnaireId,
                questionnaireName,
                "Unpublished",
                reason: reason,
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
                    logs = logs.Where(x => x.BusinessModule == BusinessModuleEnum.Questionnaire.ToString() || 
                                         x.BusinessModule == BusinessModuleEnum.QuestionnaireAnswer.ToString() ||
                                         x.BusinessModule == BusinessModuleEnum.Question.ToString() ||
                                         x.BusinessModule == BusinessModuleEnum.StaticField.ToString() ||
                                         x.BusinessModule == BusinessModuleEnum.File.ToString()).ToList();
                }
                else if (onboardingId.HasValue)
                {
                    logs = await _operationChangeLogRepository.GetByOnboardingIdAsync(onboardingId.Value);
                    logs = logs.Where(x => x.BusinessModule == BusinessModuleEnum.Questionnaire.ToString() || 
                                         x.BusinessModule == BusinessModuleEnum.QuestionnaireAnswer.ToString() ||
                                         x.BusinessModule == BusinessModuleEnum.Question.ToString() ||
                                         x.BusinessModule == BusinessModuleEnum.StaticField.ToString() ||
                                         x.BusinessModule == BusinessModuleEnum.File.ToString()).ToList();
                }
                else
                {
                    // Get all questionnaire-related logs
                    var questionnaireLogs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Questionnaire.ToString(), 0);
                    var answerLogs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.QuestionnaireAnswer.ToString(), 0);
                    var questionLogs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Question.ToString(), 0);
                    var fieldLogs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.StaticField.ToString(), 0);
                    var fileLogs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.File.ToString(), 0);
                    logs.AddRange(questionnaireLogs);
                    logs.AddRange(answerLogs);
                    logs.AddRange(questionLogs);
                    logs.AddRange(fieldLogs);
                    logs.AddRange(fileLogs);
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
                _logger.LogError(ex, "Failed to get questionnaire operation logs from database");
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        /// <summary>
        /// Get questionnaire logs by questionnaire ID
        /// </summary>
        public async Task<PagedResult<OperationChangeLogOutputDto>> GetQuestionnaireLogsAsync(long questionnaireId, int pageIndex = 1, int pageSize = 20)
        {
            try
            {
                var logs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Questionnaire.ToString(), questionnaireId);

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
                _logger.LogError(ex, "Failed to get questionnaire logs for questionnaire {QuestionnaireId}", questionnaireId);
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        /// <summary>
        /// Get question logs by question ID
        /// </summary>
        public async Task<PagedResult<OperationChangeLogOutputDto>> GetQuestionLogsAsync(long questionId, long? onboardingId = null, int pageIndex = 1, int pageSize = 20, bool includeActionExecutions = true)
        {
            try
            {
                var logs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Question.ToString(), questionId);

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
                _logger.LogError(ex, "Failed to get operation logs for question {QuestionId}", questionId);
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        /// <summary>
        /// Get questionnaire answer logs by answer ID
        /// </summary>
        public async Task<PagedResult<OperationChangeLogOutputDto>> GetQuestionnaireAnswerLogsAsync(long answerId, long onboardingId, int pageIndex = 1, int pageSize = 20)
        {
            try
            {
                var logs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.QuestionnaireAnswer.ToString(), answerId);
                
                // Filter by onboarding ID
                logs = logs.Where(x => x.OnboardingId == onboardingId).ToList();

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
                _logger.LogError(ex, "Failed to get questionnaire answer logs for answer {AnswerId}", answerId);
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        /// <summary>
        /// Get questionnaire operation statistics
        /// </summary>
        public async Task<Dictionary<string, int>> GetQuestionnaireOperationStatisticsAsync(long questionnaireId)
        {
            try
            {
                var logs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Questionnaire.ToString(), questionnaireId);

                return logs.GroupBy(x => x.OperationType)
                          .ToDictionary(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get questionnaire operation statistics for questionnaire {QuestionnaireId}", questionnaireId);
                return new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// Get operation logs by question ID (backward compatibility)
        /// </summary>
        public async Task<PagedResult<OperationChangeLogOutputDto>> GetLogsByQuestionAsync(long questionId, long? onboardingId = null, int pageIndex = 1, int pageSize = 20)
        {
            return await GetQuestionLogsAsync(questionId, onboardingId, pageIndex, pageSize, true);
        }

        #region Private Helper Methods

        /// <summary>
        /// Build answer operation description
        /// </summary>
        private string BuildAnswerOperationDescription(string operationAction, long? questionnaireId)
        {
            var description = $"Questionnaire answer has been {operationAction.ToLower()} by {GetOperatorDisplayName()}";

            if (questionnaireId.HasValue)
            {
                description += $" for questionnaire";
            }

            return description;
        }

        /// <summary>
        /// Build static field change description
        /// </summary>
        private string BuildStaticFieldChangeDescription(string displayFieldName, List<string> changedFields)
        {
            var description = $"Static field '{displayFieldName}' value has been changed by {GetOperatorDisplayName()}";

            if (changedFields?.Any() == true)
            {
                description += $". Fields: {string.Join(", ", changedFields)}";
            }

            return description;
        }

        /// <summary>
        /// Build file delete description
        /// </summary>
        private string BuildFileDeleteDescription(string fileName, string reason)
        {
            var description = $"File '{fileName}' has been deleted by {GetOperatorDisplayName()}";

            if (!string.IsNullOrEmpty(reason))
            {
                description += $" with reason: {reason}";
            }

            return description;
        }

        /// <summary>
        /// Build file update description
        /// </summary>
        private string BuildFileUpdateDescription(string fileName, List<string> changedFields)
        {
            var description = $"File '{fileName}' has been updated by {GetOperatorDisplayName()}";

            if (changedFields?.Any() == true)
            {
                description += $". Fields: {string.Join(", ", changedFields)}";
            }

            return description;
        }

        /// <summary>
        /// Build field value JSON for storage
        /// </summary>
        private string BuildFieldValueJson(string value, string fieldName)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            return JsonSerializer.Serialize(new { value = value, fieldName = fieldName });
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
            string extendedData = null)
        {
            try
            {
                var operationTitle = $"{businessModule} {operationAction}: {entityName}";
                
                // Use enhanced description method that can handle beforeData and afterData
                var operationDescription = BuildEnhancedOperationDescription(
                    businessModule,
                    entityName,
                    operationAction,
                    beforeData,
                    afterData,
                    changedFields,
                    relatedEntityId: null,
                    relatedEntityType: null,
                    reason);
                
                // Add questionnaire-specific additions
                if (!string.IsNullOrEmpty(version))
                {
                    operationDescription += $" as version {version}";
                }

                if (string.IsNullOrEmpty(extendedData))
                {
                    extendedData = BuildDefaultExtendedData(businessModule, businessId, entityName, operationAction, reason, version, changedFields);
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
            string reason,
            string version,
            List<string> changedFields)
        {
            // Use the enhanced description method from base class
            var description = BuildEnhancedOperationDescription(
                businessModule,
                entityName,
                operationAction,
                beforeData: null,
                afterData: null,
                changedFields,
                relatedEntityId: null,
                relatedEntityType: null,
                reason);

            // Add questionnaire-specific additions
            if (!string.IsNullOrEmpty(version))
            {
                description += $" as version {version}";
            }

            return description;
        }

        /// <summary>
        /// Build default extended data
        /// </summary>
        private string BuildDefaultExtendedData(
            BusinessModuleEnum businessModule,
            long businessId,
            string entityName,
            string operationAction,
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