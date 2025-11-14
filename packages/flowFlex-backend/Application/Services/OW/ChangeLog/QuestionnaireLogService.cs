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
        private readonly QuestionnaireAnswerParser _answerParser;
        private readonly IQuestionnaireRepository _questionnaireRepository;

        public QuestionnaireLogService(
            IOperationChangeLogRepository operationChangeLogRepository,
            ILogger<QuestionnaireLogService> logger,
            UserContext userContext,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ILogCacheService logCacheService,
            IUserService userService,
            QuestionnaireAnswerParser answerParser,
            IQuestionnaireRepository questionnaireRepository,
            IOperatorContextService operatorContextService)
            : base(operationChangeLogRepository, logger, userContext, httpContextAccessor, mapper, logCacheService, userService, operatorContextService)
        {
            _answerParser = answerParser;
            _questionnaireRepository = questionnaireRepository;
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

                var operationType = isUpdate ? OperationTypeEnum.QuestionnaireAnswerUpdate : OperationTypeEnum.QuestionnaireAnswerSubmit;
                var operationAction = isUpdate ? "Updated" : "Submitted";

                // Build operation description and check if there are meaningful changes
                var operationDescription = await BuildAnswerOperationDescriptionAsync(operationAction, questionnaireId, beforeData, afterData);

                // If no meaningful changes detected, skip logging
                if (string.IsNullOrEmpty(operationDescription))
                {
                    _logger.LogDebug("Skipping operation log for questionnaire answer {AnswerId} as no meaningful changes were detected", answerId);
                    return true;
                }

                var extendedData = new
                {
                    AnswerId = answerId,
                    QuestionnaireId = questionnaireId,
                    IsUpdate = isUpdate,
                    SubmittedAt = DateTimeOffset.UtcNow,
                    ChangedFieldsCount = changedFields.Count
                };

                var operationLog = BuildOperationLogEntity(
                    operationType,
                    BusinessModuleEnum.QuestionnaireAnswer,
                    answerId,
                    onboardingId,
                    stageId,
                    $"Questionnaire Answer {operationAction}",
                    operationDescription,
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
                // Check if both before and after values are empty - skip logging if so
                if (AreBothValuesEmpty(beforeData, afterData))
                {
                    _logger.LogDebug("Skipping operation log for field {FieldName} as both before and after values are empty", fieldName);
                    return true;
                }

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
        public async Task<bool> LogQuestionnaireCreateAsync(long questionnaireId, string questionnaireName, string afterData = null, string extendedData = null)
        {
            // Build detailed description for created questionnaire
            string operationDescription = null;
            if (!string.IsNullOrEmpty(afterData))
            {
                try
                {
                    var afterJson = JsonSerializer.Deserialize<Dictionary<string, object>>(afterData);
                    if (afterJson != null)
                    {
                        var changeDetails = await GetQuestionnaireSpecificChangeDetailsAsync(null, afterData, new List<string> { "StructureJson" });
                        if (!string.IsNullOrEmpty(changeDetails))
                        {
                            operationDescription = $"{BusinessModuleEnum.Questionnaire} '{questionnaireName}' has been created by {GetOperatorDisplayName()}. {changeDetails}";
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to build detailed description for questionnaire create");
                }
            }

            // If no detailed description, use default
            if (string.IsNullOrEmpty(operationDescription))
            {
                operationDescription = $"{BusinessModuleEnum.Questionnaire} '{questionnaireName}' has been created by {GetOperatorDisplayName()}";
            }

            return await LogIndependentOperationAsync(
                OperationTypeEnum.QuestionnaireCreate,
                BusinessModuleEnum.Questionnaire,
                questionnaireId,
                questionnaireName,
                "Created",
                beforeData: null,
                afterData: afterData,
                changedFields: null,
                extendedData: extendedData,
                customDescription: operationDescription
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
        /// Build answer operation description with detailed question and answer changes
        /// </summary>
        private async Task<string> BuildAnswerOperationDescriptionAsync(string operationAction, long? questionnaireId, string beforeData = null, string afterData = null)
        {
            try
            {
                var baseDescription = $"Questionnaire answer has been {operationAction.ToLower()} by {GetOperatorDisplayName()}";

                if (questionnaireId.HasValue)
                {
                    baseDescription += $" for questionnaire";
                }

                // Get questionnaire configuration for answer formatting
                object questionnaireConfig = null;
                if (questionnaireId.HasValue)
                {
                    try
                    {
                        var questionnaire = await _questionnaireRepository.GetByIdAsync(questionnaireId.Value);
                        if (!string.IsNullOrEmpty(questionnaire?.Structure?.ToString()))
                        {
                            questionnaireConfig = JsonSerializer.Deserialize<object>(questionnaire.Structure.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get questionnaire config for ID {QuestionnaireId}", questionnaireId);
                    }
                }

                // Parse questionnaire answer changes to get detailed descriptions
                var changesList = _answerParser.ParseQuestionnaireAnswerChanges(beforeData, afterData, questionnaireConfig);

                if (changesList.Any())
                {
                    var changesText = string.Join("; ", changesList);
                    // Limit description length to avoid database field overflow
                    if (changesText.Length > 2000)
                    {
                        changesText = changesText.Substring(0, 1997) + "...";
                    }
                    baseDescription += $". Changes: {changesText}";
                    return baseDescription;
                }
                else
                {
                    // No meaningful changes detected, return null to indicate no logging needed
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build detailed answer operation description for questionnaire {QuestionnaireId}", questionnaireId);

                // Fallback to simple description
                var fallbackDescription = $"Questionnaire answer has been {operationAction.ToLower()} by {GetOperatorDisplayName()}";
                if (questionnaireId.HasValue)
                {
                    fallbackDescription += $" for questionnaire";
                }
                return fallbackDescription;
            }
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

        // LogIndependentOperationAsync method has been moved to base class to eliminate code duplication

        // BuildIndependentOperationDescription method removed - functionality merged into base class

        // BuildDefaultExtendedData method has been moved to base class to eliminate code duplication

        /// <summary>
        /// Override to provide questionnaire-specific enhanced operation descriptions (async version)
        /// </summary>
        protected override async Task<string> BuildEnhancedOperationDescriptionAsync(
            BusinessModuleEnum businessModule,
            string entityName,
            string operationAction,
            string beforeData = null,
            string afterData = null,
            List<string> changedFields = null,
            long? relatedEntityId = null,
            string relatedEntityType = null,
            string reason = null)
        {
            var description = $"{businessModule} '{entityName}' has been {operationAction.ToLower()} by {GetOperatorDisplayName()}";

            // Enhanced questionnaire-specific change details
            if (!string.IsNullOrEmpty(beforeData) && !string.IsNullOrEmpty(afterData) && changedFields?.Any() == true)
            {
                var changeDetails = await GetQuestionnaireSpecificChangeDetailsAsync(beforeData, afterData, changedFields);
                if (!string.IsNullOrEmpty(changeDetails))
                {
                    description += $". {changeDetails}";
                }
            }
            else if (changedFields?.Any() == true)
            {
                // Fallback to field names if no before/after data
                var friendlyFields = changedFields.Select(GetFriendlyFieldName).ToList();
                description += $". Changed: {string.Join(", ", friendlyFields)}";
            }

            // Add related entity info without showing ID
            if (relatedEntityId.HasValue && !string.IsNullOrEmpty(relatedEntityType))
            {
                description += $" in {relatedEntityType}";
            }

            if (!string.IsNullOrEmpty(reason))
            {
                description += $" with reason: {reason}";
            }

            return description;
        }

        /// <summary>
        /// Get questionnaire-specific change details (async version)
        /// </summary>
        private Task<string> GetQuestionnaireSpecificChangeDetailsAsync(string beforeData, string afterData, List<string> changedFields)
        {
            try
            {
                Dictionary<string, object> beforeJson = null;
                if (!string.IsNullOrEmpty(beforeData))
                {
                    beforeJson = JsonSerializer.Deserialize<Dictionary<string, object>>(beforeData);
                }

                Dictionary<string, object> afterJson = null;
                if (!string.IsNullOrEmpty(afterData))
                {
                    afterJson = JsonSerializer.Deserialize<Dictionary<string, object>>(afterData);
                }

                var changeList = new List<string>();

                foreach (var field in changedFields)
                {
                    // Handle creation case (beforeData is null)
                    if (beforeJson == null && afterJson != null)
                    {
                        if (afterJson.TryGetValue(field, out var newValue))
                        {
                            if (field.Equals("StructureJson", StringComparison.OrdinalIgnoreCase))
                            {
                                var afterJsonStr = newValue?.ToString() ?? string.Empty;
                                if (IsJsonString(afterJsonStr))
                                {
                                    // For creation, show structure summary
                                    var structureSummary = GetQuestionnaireStructureSummary(afterJsonStr);
                                    if (!string.IsNullOrEmpty(structureSummary))
                                    {
                                        changeList.Add(structureSummary);
                                    }
                                    else
                                    {
                                        changeList.Add("Structure created");
                                    }
                                }
                                else
                                {
                                    changeList.Add("Structure created");
                                }
                            }
                            else
                            {
                                var friendlyFieldName = GetFriendlyFieldName(field);
                                var afterStr = GetDisplayValue(newValue, field);
                                changeList.Add($"{friendlyFieldName} set to '{afterStr}'");
                            }
                        }
                        continue;
                    }

                    // Handle update case (both beforeData and afterData exist)
                    if (beforeJson != null && afterJson != null &&
                        beforeJson.TryGetValue(field, out var beforeValue) &&
                        afterJson.TryGetValue(field, out var afterValue))
                    {
                        if (field.Equals("StructureJson", StringComparison.OrdinalIgnoreCase))
                        {
                            var beforeJsonStr = beforeValue?.ToString() ?? string.Empty;
                            var afterJsonStr = afterValue?.ToString() ?? string.Empty;

                            if (IsJsonString(beforeJsonStr) && IsJsonString(afterJsonStr))
                            {
                                var structuralChange = GetStructuralChangeDetails(beforeJsonStr, afterJsonStr);
                                changeList.Add(structuralChange);
                            }
                            else
                            {
                                changeList.Add("Structure modified");
                            }
                        }
                        else
                        {
                            var friendlyFieldName = GetFriendlyFieldName(field);
                            var beforeStr = GetDisplayValue(beforeValue, field);
                            var afterStr = GetDisplayValue(afterValue, field);

                            // Special formatting for common questionnaire fields
                            if (field.Equals("IsActive", StringComparison.OrdinalIgnoreCase))
                            {
                                var status = afterStr.ToLower() == "true" ? "activated" : "deactivated";
                                changeList.Add($"questionnaire {status}");
                            }
                            else if (field.Equals("Category", StringComparison.OrdinalIgnoreCase))
                            {
                                changeList.Add($"category changed to '{afterStr}'");
                            }
                            // Skip EstimatedMinutes - don't show estimated time changes
                            else if (field.Equals("EstimatedMinutes", StringComparison.OrdinalIgnoreCase))
                            {
                                // Skip this field - don't add to changeList
                                continue;
                            }
                            else
                            {
                                changeList.Add($"{friendlyFieldName}: '{beforeStr}' → '{afterStr}'");
                            }
                        }
                    }
                    else if (afterJson.TryGetValue(field, out var newValue))
                    {
                        var friendlyFieldName = GetFriendlyFieldName(field);
                        var newStr = GetDisplayValue(newValue, field);
                        changeList.Add($"{friendlyFieldName} set to '{newStr}'");
                    }
                }

                if (changeList.Any())
                {
                    var result = $"Changes: {string.Join("; ", changeList)}";
                    return Task.FromResult(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse questionnaire change details from JSON data");
            }

            return Task.FromResult(string.Empty);
        }

        /// <summary>
        /// Get friendly field names for display
        /// </summary>
        private string GetFriendlyFieldName(string fieldName)
        {
            return fieldName switch
            {
                "StructureJson" => "Structure",
                "IsActive" => "Status",
                "EstimatedMinutes" => "Estimated Time",
                "CreatedAt" => "Created Time",
                "UpdatedAt" => "Updated Time",
                _ => fieldName
            };
        }

        /// <summary>
        /// Check if both before and after values are empty
        /// </summary>
        private bool AreBothValuesEmpty(string beforeData, string afterData)
        {
            try
            {
                // Extract value from JSON if it's JSON format
                string beforeValue = ExtractValueFromJson(beforeData);
                string afterValue = ExtractValueFromJson(afterData);

                // Check if both values are empty (null, empty string, or whitespace)
                bool beforeEmpty = string.IsNullOrWhiteSpace(beforeValue);
                bool afterEmpty = string.IsNullOrWhiteSpace(afterValue);

                return beforeEmpty && afterEmpty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check if values are empty, proceeding with normal check");
                return false;
            }
        }

        /// <summary>
        /// Extract value from JSON data (handles both raw strings and JSON objects)
        /// </summary>
        private string ExtractValueFromJson(string data)
        {
            if (string.IsNullOrEmpty(data))
                return null;

            try
            {
                // Try to parse as JSON object first
                var jsonDoc = JsonDocument.Parse(data);
                var root = jsonDoc.RootElement;

                // Check if it has a "value" property
                if (root.TryGetProperty("value", out var valueElement))
                {
                    if (valueElement.ValueKind == JsonValueKind.String)
                    {
                        var value = valueElement.GetString();
                        // Handle escaped empty string: "\"\""
                        if (value == "\"\"")
                            return string.Empty;
                        return value;
                    }
                    return valueElement.ToString();
                }

                // If no "value" property, try to get the string value directly
                if (root.ValueKind == JsonValueKind.String)
                {
                    return root.GetString();
                }
            }
            catch
            {
                // If parsing fails, treat as raw string
            }

            // If not JSON or parsing failed, return as-is (might be a raw string)
            // Handle escaped empty string
            if (data == "\"\"")
                return string.Empty;

            return data;
        }

        #endregion
    }
}