using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Models;
using AutoMapper;
using System.ComponentModel;
using System.Reflection;
using SqlSugar;
using FlowFlex.Application.Services.OW.Extensions;
using System.Security.Claims;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Notification;
using FlowFlex.Application.Contracts.Dtos.OW.ChecklistTask;
using FlowFlex.Domain.Shared.Models;
using Newtonsoft.Json.Linq;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Operation change log service implementation
    /// </summary>
    public partial class OperationChangeLogService : IOperationChangeLogService, IScopedService
    {
        private readonly IOperationChangeLogRepository _operationChangeLogRepository;
        private readonly IActionExecutionService _actionExecutionService;
        private readonly ILogger<OperationChangeLogService> _logger;
        private readonly UserContext _userContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMapper _mapper;
        private readonly IServiceProvider _serviceProvider; // 使用 IServiceProvider 来延迟解析服务，避免循环依赖

        // Cache to store source IDs that have no action executions to avoid repeated queries
        private static readonly ConcurrentDictionary<string, DateTime> _emptySourceIdCache = new();
        private static readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(2); // Increased cache time for better performance
        private const int _maxCacheSize = 20000; // Increased cache size for better hit rate
        private static DateTime _lastCacheCleanup = DateTime.UtcNow;

        public OperationChangeLogService(
            IOperationChangeLogRepository operationChangeLogRepository,
            IActionExecutionService actionExecutionService,
            ILogger<OperationChangeLogService> logger,
            UserContext userContext,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            IServiceProvider serviceProvider)
        {
            _operationChangeLogRepository = operationChangeLogRepository;
            _actionExecutionService = actionExecutionService;
            _logger = logger;
            _userContext = userContext;
            _httpContextAccessor = httpContextAccessor;
            _mapper = mapper;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Log checklist task completion operation
        /// </summary>
        public async Task<bool> LogChecklistTaskCompleteAsync(long taskId, string taskName, long onboardingId, long? stageId, string completionNotes = null, int actualHours = 0)
        {
            try
            {
                string operationTitle = $"Checklist Task Completed: {taskName}";
                string operationDescription = $"Task '{taskName}' has been marked as completed by {GetOperatorDisplayName()}";

                if (!string.IsNullOrEmpty(completionNotes))
                {
                    operationDescription += $" with notes: {completionNotes}";
                }

                if (actualHours > 0)
                {
                    operationDescription += $". Actual time spent: {actualHours} hours";
                }

                var extendedData = JsonSerializer.Serialize(new
                {
                    TaskId = taskId,
                    TaskName = taskName,
                    CompletionNotes = completionNotes,
                    ActualHours = actualHours,
                    CompletedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.ChecklistTaskComplete,
                    BusinessModuleEnum.ChecklistTask,
                    taskId,
                    onboardingId,
                    stageId,
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData
                );
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
                string operationTitle = $"Checklist Task Uncompleted: {taskName}";
                string operationDescription = $"Task '{taskName}' has been marked as uncompleted by {GetOperatorDisplayName()}";

                if (!string.IsNullOrEmpty(reason))
                {
                    operationDescription += $" with reason: {reason}";
                }

                var extendedData = JsonSerializer.Serialize(new
                {
                    TaskId = taskId,
                    TaskName = taskName,
                    Reason = reason,
                    UncompletedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.ChecklistTaskUncomplete,
                    BusinessModuleEnum.ChecklistTask,
                    taskId,
                    onboardingId,
                    stageId,
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log checklist task uncomplete operation for task {TaskId}", taskId);
                return false;
            }
        }

        /// <summary>
        /// Log questionnaire answer submission operation
        /// </summary>
        public async Task<bool> LogQuestionnaireAnswerSubmitAsync(long answerId, long onboardingId, long stageId, long? questionnaireId, string beforeData = null, string afterData = null, bool isUpdate = false)
        {
            try
            {
                // For updates, check if there's actually a meaningful change
                if (isUpdate && !HasMeaningfulValueChange(beforeData, afterData))
                {
                    _logger.LogDebug("Skipping operation log for questionnaire answer {AnswerId} as there's no meaningful value change. Before: {BeforeData}, After: {AfterData}",
                        answerId, beforeData, afterData);
                    return true; // Return true as this is not an error, just no need to log
                }

                string operationType = isUpdate ? "Updated" : "Submitted";
                string operationTitle = $"Questionnaire Answer {operationType}";
                string operationDescription = $"Questionnaire answer has been {operationType.ToLower()} by {GetOperatorDisplayName()}";

                if (questionnaireId.HasValue)
                {
                    operationDescription += $" for questionnaire ID: {questionnaireId.Value}";
                }

                var changedFields = new List<string>();
                if (!string.IsNullOrEmpty(beforeData) && !string.IsNullOrEmpty(afterData))
                {
                    try
                    {
                        var beforeJson = JsonSerializer.Deserialize<Dictionary<string, object>>(beforeData);
                        var afterJson = JsonSerializer.Deserialize<Dictionary<string, object>>(afterData);

                        changedFields = GetChangedFields(beforeJson, afterJson);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse JSON data for change detection");
                    }
                }

                var extendedData = JsonSerializer.Serialize(new
                {
                    AnswerId = answerId,
                    QuestionnaireId = questionnaireId,
                    IsUpdate = isUpdate,
                    SubmittedAt = DateTimeOffset.UtcNow,
                    ChangedFieldsCount = changedFields.Count
                });

                return await LogOperationAsync(
                    isUpdate ? OperationTypeEnum.QuestionnaireAnswerUpdate : OperationTypeEnum.QuestionnaireAnswerSubmit,
                    BusinessModuleEnum.QuestionnaireAnswer,
                    answerId,
                    onboardingId,
                    stageId,
                    operationTitle,
                    operationDescription,
                    beforeData,
                    afterData,
                    changedFields,
                    extendedData
                );
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
                if (!HasMeaningfulValueChange(beforeData, afterData))
                {
                    _logger.LogDebug("Skipping operation log for field {FieldName} as there's no meaningful value change. Before: {BeforeData}, After: {AfterData}",
                        fieldName, beforeData, afterData);
                    return true; // Return true as this is not an error, just no need to log
                }

                // Use fieldLabel if provided, otherwise fallback to fieldName
                string displayFieldName = !string.IsNullOrEmpty(fieldLabel) ? fieldLabel : fieldName;

                string operationTitle = $"Static Field Value Changed: {displayFieldName}";
                string operationDescription = $"Static field '{displayFieldName}' value has been changed by {_userContext.UserName}";

                if (changedFields?.Any() == true)
                {
                    operationDescription += $". Changed fields: {string.Join(", ", changedFields)}";
                }

                // Convert raw field values to proper JSON format for JSONB storage
                string beforeDataJson = null;
                string afterDataJson = null;

                if (!string.IsNullOrEmpty(beforeData))
                {
                    beforeDataJson = JsonSerializer.Serialize(new { value = beforeData, fieldName = displayFieldName });
                }

                if (!string.IsNullOrEmpty(afterData))
                {
                    afterDataJson = JsonSerializer.Serialize(new { value = afterData, fieldName = displayFieldName });
                }

                var extendedData = JsonSerializer.Serialize(new
                {
                    FieldValueId = fieldValueId,
                    FieldName = fieldName,
                    FieldLabel = fieldLabel,
                    DisplayFieldName = displayFieldName,
                    ChangedFieldsCount = changedFields?.Count ?? 0,
                    ChangedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.StaticFieldValueChange,
                    BusinessModuleEnum.StaticField,
                    fieldValueId,
                    onboardingId,
                    stageId,
                    operationTitle,
                    operationDescription,
                    beforeDataJson,
                    afterDataJson,
                    changedFields,
                    extendedData
                );
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
                _logger.LogInformation($"📝 [Log Step 1] Starting to log file upload for file {fileId}");
                // Debug logging handled by structured logging
                _logger.LogInformation($"📝 [Log Step 2] Preparing operation title and description...");
                // Debug logging handled by structured logging
                string operationTitle = $"File Uploaded: {fileName}";
                string operationDescription = $"File '{fileName}' has been uploaded successfully by {GetOperatorDisplayName()}";

                _logger.LogInformation($"📝 [Log Step 3] Serializing extended data...");
                // Debug logging handled by structured logging
                var extendedData = JsonSerializer.Serialize(new
                {
                    FileId = fileId,
                    FileName = fileName,
                    FileSize = fileSize,
                    FileSizeFormatted = FormatFileSize(fileSize),
                    ContentType = contentType,
                    Category = category,
                    UploadedAt = DateTimeOffset.UtcNow
                });

                _logger.LogInformation($"📝 [Log Step 4] Calling LogOperationAsync...");
                // Debug logging handled by structured logging
                var result = await LogOperationAsync(
                    OperationTypeEnum.FileUpload,
                    BusinessModuleEnum.File,
                    fileId,
                    onboardingId,
                    stageId,
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData
                );

                _logger.LogInformation($"📝 [Log Step 5] LogOperationAsync completed with result: {result}");
                // Debug logging handled by structured logging
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�?Failed to log file upload operation for file {FileId}. Error: {ErrorMessage}", fileId, ex.Message);

                // Log detailed error information to console for debugging
                // Debug logging handled by structured logging
                // Return false instead of throwing exception to ensure no program crash
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
                string operationTitle = $"File Deleted: {fileName}";
                string operationDescription = $"File '{fileName}' has been deleted by {GetOperatorDisplayName()}";

                if (!string.IsNullOrEmpty(reason))
                {
                    operationDescription += $" with reason: {reason}";
                }

                var extendedData = JsonSerializer.Serialize(new
                {
                    FileId = fileId,
                    FileName = fileName,
                    Reason = reason,
                    DeletedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.FileDelete,
                    BusinessModuleEnum.File,
                    fileId,
                    onboardingId,
                    stageId,
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData
                );
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
                if (!HasMeaningfulValueChange(beforeData, afterData))
                {
                    _logger.LogDebug("Skipping operation log for file {FileName} as there's no meaningful value change. Before: {BeforeData}, After: {AfterData}",
                        fileName, beforeData, afterData);
                    return true; // Return true as this is not an error, just no need to log
                }

                string operationTitle = $"File Updated: {fileName}";
                string operationDescription = $"File '{fileName}' has been updated by {GetOperatorDisplayName()}";

                if (changedFields?.Any() == true)
                {
                    operationDescription += $". Changed fields: {string.Join(", ", changedFields)}";
                }

                var extendedData = JsonSerializer.Serialize(new
                {
                    FileId = fileId,
                    FileName = fileName,
                    ChangedFieldsCount = changedFields?.Count ?? 0,
                    UpdatedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.FileUpdate,
                    BusinessModuleEnum.File,
                    fileId,
                    onboardingId,
                    stageId,
                    operationTitle,
                    operationDescription,
                    beforeData,
                    afterData,
                    changedFields,
                    extendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log file update operation for file {FileId}", fileId);
                return false;
            }
        }

        /// <summary>
        /// Generic operation logging method
        /// </summary>
        public async Task<bool> LogOperationAsync(
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
            try
            {
                _logger.LogInformation($"🔧 [Op Step 1] Starting LogOperationAsync for {operationType}");
                // Debug logging handled by structured logging
                _logger.LogInformation($"🔧 [Op Step 2] Getting HTTP context information...");
                // Debug logging handled by structured logging
                var httpContext = _httpContextAccessor.HttpContext;
                string ipAddress = GetClientIpAddress(httpContext);
                string userAgent = httpContext?.Request.Headers["User-Agent"].FirstOrDefault() ?? string.Empty;
                string operationSource = GetOperationSource(httpContext);

                _logger.LogInformation($"🔧 [Op Step 3] Creating OperationChangeLog entity...");
                // Debug logging handled by structured logging
                var operationLog = new OperationChangeLog
                {
                    OperationType = operationType.ToString(),
                    BusinessModule = businessModule.ToString(),
                    BusinessId = businessId,
                    OnboardingId = onboardingId,
                    StageId = stageId,
                    OperationTitle = operationTitle,
                    OperationDescription = operationDescription,
                    BeforeData = !string.IsNullOrEmpty(beforeData) ? beforeData : null,
                    AfterData = !string.IsNullOrEmpty(afterData) ? afterData : null,
                    ChangedFields = changedFields != null ? JsonSerializer.Serialize(changedFields) : null,
                    OperatorId = GetOperatorId(),
                    OperatorName = GetOperatorDisplayName(),
                    OperationTime = DateTimeOffset.UtcNow,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    OperationSource = operationSource,
                    ExtendedData = !string.IsNullOrEmpty(extendedData) ? extendedData : null,
                    OperationStatus = operationStatus.ToString(),
                    ErrorMessage = errorMessage
                };

                _logger.LogInformation($"🔧 [Op Step 4] Initializing create information...");
                // Debug logging handled by structured logging
                // Initialize create information with proper ID and timestamps
                operationLog.InitCreateInfo(_userContext);

                _logger.LogInformation($"🔧 [Op Step 5] Entity created with ID: {operationLog.Id}");
                // Debug logging handled by structured logging
                _logger.LogInformation($"🔧 [Op Step 6] Calling InsertOperationLogWithJsonbAsync...");
                // Debug logging handled by structured logging
                // Use SqlSugar direct insertion with JSONB type handling
                bool result = await InsertOperationLogWithJsonbAsync(operationLog);

                _logger.LogInformation($"🔧 [Op Step 7] InsertOperationLogWithJsonbAsync completed with result: {result}");
                // Debug logging handled by structured logging
                if (result)
                {
                    _logger.LogInformation("�?Operation log recorded: {OperationType} for {BusinessModule} {BusinessId}",
                        operationType, businessModule, businessId);
                    // Debug logging handled by structured logging
                }
                else
                {
                    _logger.LogWarning("⚠️ Failed to record operation log: {OperationType} for {BusinessModule} {BusinessId}",
                        operationType, businessModule, businessId);
                    // Debug logging handled by structured logging
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording operation log: {OperationType} for {BusinessModule} {BusinessId}. Error: {ErrorMessage}",
                    operationType, businessModule, businessId, ex.Message);

                // 记录详细错误信息到控制台，便于调�?
                // Debug logging handled by structured logging
                // 返回 false 而不是抛出异常，确保不会导致程序崩溃
                return false;
            }
        }

        /// <summary>
        /// 获取操作日志列表
        /// </summary>
        public async Task<PagedResult<OperationChangeLogOutputDto>> GetOperationLogsAsync(long? onboardingId = null, long? stageId = null, OperationTypeEnum? operationType = null, int pageIndex = 1, int pageSize = 20, bool includeActionExecutions = true)
        {
            try
            {
                List<OperationChangeLogOutputDto> allLogs = new List<OperationChangeLogOutputDto>();

                // Get operation change logs
                List<OperationChangeLog> logs;

                // 优先处理同时提供 onboardingId 和 stageId 的情况
                if (onboardingId.HasValue && stageId.HasValue)
                {
                    logs = await _operationChangeLogRepository.GetByOnboardingAndStageAsync(onboardingId.Value, stageId.Value);
                }
                else if (onboardingId.HasValue)
                {
                    logs = await _operationChangeLogRepository.GetByOnboardingIdAsync(onboardingId.Value);
                }
                else if (stageId.HasValue)
                {
                    logs = await _operationChangeLogRepository.GetByStageIdAsync(stageId.Value);
                }
                else if (operationType.HasValue)
                {
                    logs = await _operationChangeLogRepository.GetByOperationTypeAsync(operationType.ToString(), onboardingId);
                }
                else
                {
                    // 如果没有指定条件，返回空结果
                    logs = new List<OperationChangeLog>();
                }

                // 只有在没有同时提供 onboardingId 和 stageId 的情况下才需要额外过滤
                if (stageId.HasValue && onboardingId.HasValue == false && logs.Any())
                {
                    logs = logs.Where(x => x.StageId == stageId.Value).ToList();
                }

                if (operationType.HasValue && logs.Any())
                {
                    logs = logs.Where(x => x.OperationType == operationType.ToString()).ToList();
                }

                // Convert operation logs to output DTOs
                var operationLogDtos = logs.Select(MapToOutputDto).ToList();
                allLogs.AddRange(operationLogDtos);

                // Get action execution logs if requested and stageId is provided
                if (includeActionExecutions && stageId.HasValue)
                {
                    try
                    {
                        // Prepare JSON conditions for filtering by onboardingId if provided
                        List<JsonQueryCondition> jsonConditions = null;
                        if (onboardingId.HasValue)
                        {
                            jsonConditions = new List<JsonQueryCondition>
                            {
                                new JsonQueryCondition
                                {
                                    JsonPath = "OnboardingId",
                                    Operator = "=",
                                    Value = onboardingId.Value.ToString()
                                }
                            };
                        }

                        var actionExecutionsResult = await _actionExecutionService.GetExecutionsByTriggerSourceIdAsync(
                            stageId.Value, 1, 1000, jsonConditions); // Get more records for merging with filtering

                        // Convert action executions to change log DTOs
                        var actionExecutionDtos = actionExecutionsResult.Data
                            .Select(MapActionExecutionToChangeLogDto)
                            .ToList();
                        allLogs.AddRange(actionExecutionDtos);

                        _logger.LogInformation($"Integrated {actionExecutionDtos.Count} action executions for stageId {stageId}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch action executions for stageId {StageId}, continuing without action logs", stageId);
                        // Continue without action executions rather than failing the entire request
                    }
                }

                // Sort all logs by operation time (descending)
                allLogs = allLogs.OrderByDescending(x => x.OperationTime).ToList();

                // Apply pagination to combined results
                int totalCount = allLogs.Count;
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
                _logger.LogError(ex, "Error getting operation logs");
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        /// <summary>
        /// 根据业务模块和业务ID获取操作日志
        /// </summary>
        public async Task<PagedResult<OperationChangeLogOutputDto>> GetLogsByBusinessAsync(string businessModule, long businessId, int pageIndex = 1, int pageSize = 20)
        {
            try
            {
                var logs = await _operationChangeLogRepository.GetByBusinessAsync(businessModule, businessId);

                // 分页处理
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
                _logger.LogError(ex, "Failed to get operation logs for business {BusinessModule} {BusinessId}", businessModule, businessId);
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
        /// 根据业务ID获取操作日志（不需要指定业务模块）
        /// </summary>
        public async Task<PagedResult<OperationChangeLogOutputDto>> GetLogsByBusinessIdAsync(long businessId, int pageIndex = 1, int pageSize = 20)
        {
            try
            {
                var logs = await _operationChangeLogRepository.GetByBusinessIdAsync(businessId);

                // 分页处理
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
                _logger.LogError(ex, "Failed to get operation logs for business ID {BusinessId}", businessId);
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
        /// 根据业务ID和业务类型获取操作日志（支持关联查询）
        /// </summary>
        public async Task<PagedResult<OperationChangeLogOutputDto>> GetLogsByBusinessIdWithTypeAsync(long businessId, BusinessTypeEnum? businessType = null, int pageIndex = 1, int pageSize = 20)
        {
            try
            {
                var allLogs = new List<OperationChangeLog>();

                if (businessType.HasValue)
                {
                    // 根据指定的业务类型查询
                    switch (businessType.Value)
                    {
                        case BusinessTypeEnum.Workflow:
                            // 查询Workflow及其关联的Stage
                            var workflowLogs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Workflow.ToString(), businessId);
                            allLogs.AddRange(workflowLogs);

                            // 查询该Workflow下的所有Stage
                            var relatedStages = await GetRelatedStagesByWorkflowIdAsync(businessId);
                            foreach (var stageId in relatedStages)
                            {
                                var relatedStageLogs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Stage.ToString(), stageId);
                                allLogs.AddRange(relatedStageLogs);
                            }
                            break;

                        case BusinessTypeEnum.Stage:
                            // 只查询Stage
                            var stageLogs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Stage.ToString(), businessId);
                            allLogs.AddRange(stageLogs);
                            break;

                        case BusinessTypeEnum.Checklist:
                            // 查询Checklist及其关联的ChecklistTask
                            var checklistLogs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Checklist.ToString(), businessId);
                            allLogs.AddRange(checklistLogs);

                            // 查询该Checklist下的所有ChecklistTask
                            var relatedTasks = await GetRelatedTasksByChecklistIdAsync(businessId);
                            foreach (var taskId in relatedTasks)
                            {
                                var relatedTaskLogs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Task.ToString(), taskId);
                                allLogs.AddRange(relatedTaskLogs);
                            }
                            break;

                        case BusinessTypeEnum.Questionnaire:
                            // 只查询Questionnaire
                            var questionnaireLogs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Question.ToString(), businessId);
                            allLogs.AddRange(questionnaireLogs);
                            break;

                        case BusinessTypeEnum.ChecklistTask:
                            // 只查询ChecklistTask
                            var taskLogs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Task.ToString(), businessId);
                            allLogs.AddRange(taskLogs);
                            break;

                        default:
                            // 如果类型不匹配，返回空结果
                            break;
                    }
                }
                else
                {
                    // 如果没有指定类型，使用原来的逻辑查询所有模块
                    allLogs = await _operationChangeLogRepository.GetByBusinessIdAsync(businessId);
                }

                // 按操作时间降序排序
                allLogs = allLogs.OrderByDescending(x => x.OperationTime).ToList();

                // 分页处理
                var totalCount = allLogs.Count;
                var pagedLogs = allLogs.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();

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
        /// 获取指定Workflow下的所有Stage ID
        /// </summary>
        private async Task<List<long>> GetRelatedStagesByWorkflowIdAsync(long workflowId)
        {
            try
            {
                var stageRepository = _serviceProvider.GetService<IStageRepository>();
                if (stageRepository == null)
                {
                    _logger.LogWarning("StageRepository not found, cannot get related stages for workflow {WorkflowId}", workflowId);
                    return new List<long>();
                }

                var stages = await stageRepository.GetByWorkflowIdAsync(workflowId);
                return stages?.Select(s => s.Id).ToList() ?? new List<long>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get related stages for workflow {WorkflowId}", workflowId);
                return new List<long>();
            }
        }

        /// <summary>
        /// 获取指定Checklist下的所有ChecklistTask ID
        /// </summary>
        private async Task<List<long>> GetRelatedTasksByChecklistIdAsync(long checklistId)
        {
            try
            {
                var checklistTaskRepository = _serviceProvider.GetService<IChecklistTaskRepository>();
                if (checklistTaskRepository == null)
                {
                    _logger.LogWarning("ChecklistTaskRepository not found, cannot get related tasks for checklist {ChecklistId}", checklistId);
                    return new List<long>();
                }

                var tasks = await checklistTaskRepository.GetByChecklistIdAsync(checklistId);
                return tasks?.Select(t => t.Id).ToList() ?? new List<long>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get related tasks for checklist {ChecklistId}", checklistId);
                return new List<long>();
            }
        }

        /// <summary>
        /// 根据多个业务ID获取操作日志（批量查询）
        /// </summary>
        public async Task<PagedResult<OperationChangeLogOutputDto>> GetLogsByBusinessIdsAsync(List<long> businessIds, int pageIndex = 1, int pageSize = 20)
        {
            try
            {
                if (businessIds == null || !businessIds.Any())
                {
                    return new PagedResult<OperationChangeLogOutputDto>
                    {
                        Items = new List<OperationChangeLogOutputDto>(),
                        TotalCount = 0,
                        PageIndex = pageIndex,
                        PageSize = pageSize
                    };
                }

                var logs = await _operationChangeLogRepository.GetByBusinessIdsAsync(businessIds);

                // 分页处理
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
                _logger.LogError(ex, "Failed to get operation logs for business IDs {BusinessIds}", string.Join(",", businessIds));
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
        /// 获取操作统计信息
        /// </summary>
        public async Task<Dictionary<string, int>> GetOperationStatisticsAsync(long? onboardingId = null, long? stageId = null)
        {
            try
            {
                return await _operationChangeLogRepository.GetOperationStatisticsAsync(onboardingId, stageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting operation statistics");
                return new Dictionary<string, int>();
            }
        }

        #region Private Methods

        /// <summary>
        /// Check if a source ID is in the empty results cache and still valid
        /// </summary>
        private bool IsSourceIdCachedAsEmpty(long sourceId, long? onboardingId)
        {
            var cacheKey = $"{sourceId}_{onboardingId ?? 0}";
            if (_emptySourceIdCache.TryGetValue(cacheKey, out var cachedTime))
            {
                if (DateTime.UtcNow - cachedTime < _cacheExpiration)
                {
                    return true;
                }
                else
                {
                    // Remove expired entry
                    _emptySourceIdCache.TryRemove(cacheKey, out _);
                }
            }
            return false;
        }

        /// <summary>
        /// Add a source ID to the empty results cache with improved cache management
        /// </summary>
        private void CacheSourceIdAsEmpty(long sourceId, long? onboardingId)
        {
            var cacheKey = $"{sourceId}_{onboardingId ?? 0}";

            // Periodic cache cleanup (every hour)
            if (DateTime.UtcNow - _lastCacheCleanup > TimeSpan.FromHours(1))
            {
                PerformCacheCleanup();
            }

            // Prevent cache from growing too large
            if (_emptySourceIdCache.Count >= _maxCacheSize)
            {
                // Remove oldest 25% of entries (improved LRU simulation)
                var entriesToRemove = _maxCacheSize / 4;
                var oldestEntries = _emptySourceIdCache
                    .OrderBy(kvp => kvp.Value)
                    .Take(entriesToRemove)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in oldestEntries)
                {
                    _emptySourceIdCache.TryRemove(key, out _);
                }

                _logger.LogDebug("Removed {Count} oldest cache entries to prevent overflow", oldestEntries.Count);
            }

            _emptySourceIdCache.TryAdd(cacheKey, DateTime.UtcNow);
        }

        /// <summary>
        /// Perform periodic cache cleanup to remove expired entries
        /// </summary>
        private void PerformCacheCleanup()
        {
            try
            {
                var now = DateTime.UtcNow;
                var expiredKeys = _emptySourceIdCache
                    .Where(kvp => now - kvp.Value > _cacheExpiration)
                    .Select(kvp => kvp.Key)
                    .ToList();

                var removedCount = 0;
                foreach (var key in expiredKeys)
                {
                    if (_emptySourceIdCache.TryRemove(key, out _))
                    {
                        removedCount++;
                    }
                }

                _lastCacheCleanup = now;

                if (removedCount > 0)
                {
                    _logger.LogDebug("Cache cleanup completed: removed {ExpiredCount} expired entries, {ActiveCount} entries remain",
                        removedCount, _emptySourceIdCache.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during cache cleanup");
            }
        }

        /// <summary>
        /// Log batch query optimization statistics for performance monitoring
        /// </summary>
        private void LogBatchQueryOptimizationStats(int totalSourceIds, int cachedSourceIds, int processedSourceIds, int foundExecutions)
        {
            var cacheHitRate = totalSourceIds > 0 ? (cachedSourceIds * 100.0 / totalSourceIds) : 0;
            var executionFoundRate = processedSourceIds > 0 ? (foundExecutions * 100.0 / processedSourceIds) : 0;

            if (cachedSourceIds > totalSourceIds * 0.5) // More than 50% cache hit rate
            {
                _logger.LogInformation("Excellent cache performance: {CacheHitRate:F1}% cache hit rate ({CachedCount}/{TotalCount} source IDs cached)",
                    cacheHitRate, cachedSourceIds, totalSourceIds);
            }
            else if (processedSourceIds > 100 && foundExecutions == 0)
            {
                _logger.LogInformation("Suggestion: Consider implementing bulk query optimization for {ProcessedCount} source IDs with no executions found. " +
                    "Cache hit rate: {CacheHitRate:F1}%", processedSourceIds, cacheHitRate);
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Batch query stats: Total={TotalSourceIds}, Cached={CachedSourceIds} ({CacheHitRate:F1}%), " +
                    "Processed={ProcessedSourceIds}, Found={FoundExecutions} ({ExecutionFoundRate:F1}%)",
                    totalSourceIds, cachedSourceIds, cacheHitRate, processedSourceIds, foundExecutions, executionFoundRate);
            }
        }

        /// <summary>
        /// 使用专门的JSONB处理方法插入操作日志
        /// </summary>
        private async Task<bool> InsertOperationLogWithJsonbAsync(OperationChangeLog operationLog)
        {
            try
            {
                _logger.LogInformation($"💾 [Insert Step 1] Starting InsertOperationLogWithJsonbAsync for ID: {operationLog.Id}");
                // Debug logging handled by structured logging
                // 确保 JSON 字段正确处理 null �?                // 对于 JSONB 字段，null 应该保持�?null，而不�?"null" 字符�?                // 只有当字段有实际内容时才设置�?                
                _logger.LogInformation($"💾 [Insert Step 2] Calling InsertOperationLogWithRawSqlAsync...");
                // Debug logging handled by structured logging
                // 主要方法：使用原生SQL处理JSONB字段
                var result = await InsertOperationLogWithRawSqlAsync(operationLog);

                _logger.LogInformation($"💾 [Insert Step 3] InsertOperationLogWithRawSqlAsync completed with result: {result}");
                // Debug logging handled by structured logging
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to insert operation log with JSONB handling using raw SQL. Error: {ErrorMessage}", ex.Message);

                // 记录详细错误信息到控制台
                // Debug logging handled by structured logging
                // Fallback: try using SqlSugar's standard method (though it may fail)
                try
                {
                    // Debug logging handled by structured logging
                    return await _operationChangeLogRepository.InsertOperationLogAsync(operationLog);
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Fallback SqlSugar insertion also failed. Error: {ErrorMessage}", fallbackEx.Message);
                    // Debug logging handled by structured logging
                    return false;
                }
            }
        }

        /// <summary>
        /// 使用原生SQL插入操作日志，显式处理JSONB类型转换
        /// </summary>
        private async Task<bool> InsertOperationLogWithRawSqlAsync(OperationChangeLog operationLog)
        {
            try
            {
                _logger.LogInformation($"🗃�?[SQL Step 1] Starting InsertOperationLogWithRawSqlAsync for ID: {operationLog.Id}");
                // Debug logging handled by structured logging
                _logger.LogInformation($"🗃�?[SQL Step 2] Preparing SQL statement...");
                // Debug logging handled by structured logging
                string sql = @"
                    INSERT INTO ff_operation_change_log (
                        id, tenant_id, operation_type, business_module, business_id, 
                        onboarding_id, stage_id, operation_title, operation_description,
                        before_data, after_data, changed_fields, operator_id, operator_name,
                        operation_time, ip_address, user_agent, operation_source,
                        extended_data, operation_status, error_message, is_valid,
                        create_date, modify_date, create_by, modify_by, create_user_id, modify_user_id
                    ) VALUES (
                        @Id, @TenantId, @OperationType, @BusinessModule, @BusinessId,
                        CASE WHEN @OnboardingId IS NULL THEN NULL ELSE @OnboardingId::bigint END,
                        CASE WHEN @StageId IS NULL THEN NULL ELSE @StageId::bigint END,
                        @OperationTitle, @OperationDescription,
                        CASE 
                            WHEN @BeforeData IS NULL OR @BeforeData = '' THEN NULL::jsonb 
                            ELSE @BeforeData::jsonb 
                        END,
                        CASE 
                            WHEN @AfterData IS NULL OR @AfterData = '' THEN NULL::jsonb 
                            ELSE @AfterData::jsonb 
                        END,
                        CASE 
                            WHEN @ChangedFields IS NULL OR @ChangedFields = '' THEN NULL::jsonb 
                            ELSE @ChangedFields::jsonb 
                        END,
                        @OperatorId, @OperatorName,
                        @OperationTime, @IpAddress, @UserAgent, @OperationSource,
                        CASE 
                            WHEN @ExtendedData IS NULL OR @ExtendedData = '' THEN NULL::jsonb 
                            ELSE @ExtendedData::jsonb 
                        END,
                        @OperationStatus, @ErrorMessage, @IsValid,
                        @CreateDate, @ModifyDate, @CreateBy, @ModifyBy, @CreateUserId, @ModifyUserId
                    )";

                _logger.LogInformation($"🗃️ [SQL Step 3] Preparing parameters...");
                // Debug logging handled by structured logging
                // Use SqlSugar parameter object to ensure correct type
                var parameters = new List<SugarParameter>
                {
                    new SugarParameter("@Id", operationLog.Id),
                    new SugarParameter("@TenantId", operationLog.TenantId ?? "default"),
                    new SugarParameter("@OperationType", operationLog.OperationType),
                    new SugarParameter("@BusinessModule", operationLog.BusinessModule),
                    new SugarParameter("@BusinessId", operationLog.BusinessId),
                    new SugarParameter("@OnboardingId", operationLog.OnboardingId, System.Data.DbType.Int64),
                    new SugarParameter("@StageId", operationLog.StageId, System.Data.DbType.Int64),
                    new SugarParameter("@OperationTitle", operationLog.OperationTitle),
                    new SugarParameter("@OperationDescription", operationLog.OperationDescription),
                    new SugarParameter("@BeforeData", string.IsNullOrEmpty(operationLog.BeforeData) ? null : operationLog.BeforeData),
                    new SugarParameter("@AfterData", string.IsNullOrEmpty(operationLog.AfterData) ? null : operationLog.AfterData),
                    new SugarParameter("@ChangedFields", string.IsNullOrEmpty(operationLog.ChangedFields) ? null : operationLog.ChangedFields),
                    new SugarParameter("@OperatorId", operationLog.OperatorId),
                    new SugarParameter("@OperatorName", operationLog.OperatorName),
                    new SugarParameter("@OperationTime", operationLog.OperationTime),
                    new SugarParameter("@IpAddress", operationLog.IpAddress),
                    new SugarParameter("@UserAgent", operationLog.UserAgent),
                    new SugarParameter("@OperationSource", operationLog.OperationSource),
                    new SugarParameter("@ExtendedData", string.IsNullOrEmpty(operationLog.ExtendedData) ? null : operationLog.ExtendedData),
                    new SugarParameter("@OperationStatus", operationLog.OperationStatus),
                    new SugarParameter("@ErrorMessage", operationLog.ErrorMessage),
                    new SugarParameter("@IsValid", operationLog.IsValid),
                    new SugarParameter("@CreateDate", operationLog.CreateDate),
                    new SugarParameter("@ModifyDate", operationLog.ModifyDate),
                    new SugarParameter("@CreateBy", operationLog.CreateBy),
                    new SugarParameter("@ModifyBy", operationLog.ModifyBy),
                    new SugarParameter("@CreateUserId", operationLog.CreateUserId),
                    new SugarParameter("@ModifyUserId", operationLog.ModifyUserId)
                }
                ;

                _logger.LogInformation($"🗃️ [SQL Step 4] Executing SQL command...");
                // Debug logging handled by structured logging
                var result = await _operationChangeLogRepository.ExecuteInsertWithJsonbAsync(sql, parameters.ToArray());

                _logger.LogInformation($"🗃️ [SQL Step 5] SQL execution completed with result: {result}");
                // Debug logging handled by structured logging
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed in InsertOperationLogWithRawSqlAsync. Error: {ErrorMessage}", ex.Message);
                // Debug logging handled by structured logging
                throw; // Re-throw exception for upper layer handling
            }
        }

        /// <summary>
        /// Get client IP address
        /// </summary>
        private string GetClientIpAddress(HttpContext context)
        {
            if (context == null) return string.Empty;

            string ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            }
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = context.Connection.RemoteIpAddress?.ToString();
            }

            return ipAddress ?? string.Empty;
        }

        /// <summary>
        /// Get operation source
        /// </summary>
        private string GetOperationSource(HttpContext context)
        {
            if (context == null) return "System";

            string path = context.Request.Path.Value ?? string.Empty;

            if (path.Contains("/customer-portal/", StringComparison.OrdinalIgnoreCase))
            {
                return "CustomerPortal";
            }
            else if (path.Contains("/admin/", StringComparison.OrdinalIgnoreCase))
            {
                return "AdminPortal";
            }
            else if (path.Contains("/api/", StringComparison.OrdinalIgnoreCase))
            {
                return "WebApi";
            }

            return "WebApi";
        }

        /// <summary>
        /// Resolve operator display name from context, headers, or claims
        /// </summary>
        private string GetOperatorDisplayName()
        {
            // Prefer explicit email then username from UserContext
            if (!string.IsNullOrWhiteSpace(_userContext?.Email))
            {
                return _userContext.Email;
            }
            if (!string.IsNullOrWhiteSpace(_userContext?.UserName))
            {
                return _userContext.UserName;
            }

            var httpContext = _httpContextAccessor?.HttpContext;
            // Custom headers from gateway/frontend
            var headerEmail = httpContext?.Request?.Headers["X-User-Email"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerEmail))
            {
                return headerEmail;
            }
            var headerName = httpContext?.Request?.Headers["X-User-Name"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerName))
            {
                return headerName;
            }

            // Claims
            var user = httpContext?.User;
            if (user != null)
            {
                string[] emailFirstClaims = new[]
                {
                    ClaimTypes.Email,
                    "email",
                    "preferred_username",
                    ClaimTypes.Name,
                    "name",
                    ClaimTypes.GivenName,
                    "upn"
                };
                foreach (var ct in emailFirstClaims)
                {
                    var value = user.Claims.FirstOrDefault(c => c.Type == ct)?.Value;
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }

            return "System";
        }

        /// <summary>
        /// Resolve operator id from context, headers, or claims
        /// </summary>
        private long GetOperatorId()
        {
            if (!string.IsNullOrWhiteSpace(_userContext?.UserId) && long.TryParse(_userContext.UserId, out var idFromContext))
            {
                return idFromContext;
            }

            var httpContext = _httpContextAccessor?.HttpContext;
            var headerUserId = httpContext?.Request?.Headers["X-User-Id"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerUserId) && long.TryParse(headerUserId, out var idFromHeader))
            {
                return idFromHeader;
            }

            var user = httpContext?.User;
            if (user != null)
            {
                string[] idClaims = new[]
                {
                    ClaimTypes.NameIdentifier,
                    "sub",
                    "user_id",
                    "uid"
                };
                foreach (var ct in idClaims)
                {
                    var v = user.Claims.FirstOrDefault(c => c.Type == ct)?.Value;
                    if (!string.IsNullOrWhiteSpace(v) && long.TryParse(v, out var idFromClaims))
                    {
                        return idFromClaims;
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// Check if there are actual value changes
        /// </summary>
        private bool HasMeaningfulValueChange(string beforeData, string afterData)
        {
            // If both values are empty or null, there is no change
            if (string.IsNullOrEmpty(beforeData) && string.IsNullOrEmpty(afterData))
                return false;

            // If one is empty and the other is not, there is a change
            if (string.IsNullOrEmpty(beforeData) || string.IsNullOrEmpty(afterData))
                return true;

            // Normalize values for comparison
            string normalizedBefore = NormalizeValue(beforeData);
            string normalizedAfter = NormalizeValue(afterData);

            return !string.Equals(normalizedBefore, normalizedAfter, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Normalize value, remove unnecessary quotes and spaces
        /// </summary>
        private string NormalizeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // Remove leading and trailing spaces
            value = value.Trim();

            // If value is surrounded by double quotes, remove quotes
            if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
            {
                value = value.Substring(1, value.Length - 2);
            }

            // Try to parse as number, if it's a number then normalize format
            if (decimal.TryParse(value, out decimal decimalValue))
            {
                // For numbers, use standard format (remove unnecessary decimal places)
                return decimalValue.ToString("0.##");
            }

            // For boolean values, normalize to lowercase
            if (bool.TryParse(value, out bool boolValue))
            {
                return boolValue.ToString().ToLowerInvariant();
            }

            return value;
        }

        /// <summary>
        /// Get list of changed fields
        /// </summary>
        private List<string> GetChangedFields(Dictionary<string, object> beforeData, Dictionary<string, object> afterData)
        {
            var changedFields = new List<string>();

            if (beforeData == null && afterData == null) return changedFields;
            if (beforeData == null) return afterData.Keys.ToList();
            if (afterData == null) return beforeData.Keys.ToList();

            var allKeys = beforeData.Keys.Union(afterData.Keys).ToList();

            foreach (var key in allKeys)
            {
                var beforeValue = beforeData.ContainsKey(key) ? beforeData[key] : null;
                var afterValue = afterData.ContainsKey(key) ? afterData[key] : null;

                if (!Equals(beforeValue, afterValue))
                {
                    changedFields.Add(key);
                }
            }

            return changedFields;
        }

        /// <summary>
        /// Format file size
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 Bytes";

            string[] sizes = { "Bytes", "KB", "MB", "GB", "TB" };
            int i = (int)Math.Floor(Math.Log(bytes) / Math.Log(1024));
            return Math.Round(bytes / Math.Pow(1024, i), 2) + " " + sizes[i];
        }

        /// <summary>
        /// Get enum description
        /// </summary>
        private string GetEnumDescription(Enum value)
        {
            FieldInfo field = value.GetType().GetField(value.ToString());
            DescriptionAttribute attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? value.ToString();
        }

        /// <summary>
        /// Get relative time display
        /// </summary>
        private string GetRelativeTimeDisplay(DateTimeOffset dateTime)
        {
            var now = DateTimeOffset.UtcNow;
            var timeSpan = now - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minutes ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hours ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} days ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} months ago";

            return $"{(int)(timeSpan.TotalDays / 365)} years ago";
        }

        /// <summary>
        /// 映射到输出DTO
        /// </summary>
        private OperationChangeLogOutputDto MapToOutputDto(OperationChangeLog entity)
        {
            var dto = _mapper.Map<OperationChangeLogOutputDto>(entity);

            // 设置显示名称
            if (Enum.TryParse<OperationTypeEnum>(entity.OperationType, out var operationTypeEnum))
            {
                dto.OperationTypeDisplayName = GetEnumDescription(operationTypeEnum);
            }

            if (Enum.TryParse<OperationStatusEnum>(entity.OperationStatus, out var operationStatusEnum))
            {
                dto.OperationStatusDisplayName = GetEnumDescription(operationStatusEnum);
            }

            // 设置相对时间显示
            dto.OperationTimeDisplay = GetRelativeTimeDisplay(entity.OperationTime);

            // 解析变更字段
            if (!string.IsNullOrEmpty(entity.ChangedFields))
            {
                try
                {
                    dto.ChangedFields = JsonSerializer.Deserialize<List<string>>(entity.ChangedFields) ?? new List<string>();
                }
                catch
                {
                    dto.ChangedFields = new List<string>();
                }
            }

            return dto;
        }

        /// <summary>
        /// Convert ActionExecutionWithActionInfoDto to OperationChangeLogOutputDto with source type
        /// </summary>
        private OperationChangeLogOutputDto MapActionExecutionToChangeLogDto(ActionExecutionWithActionInfoDto actionExecution, string sourceType = null)
        {
            var dto = MapActionExecutionToChangeLogDto(actionExecution);

            // Override operationType based on source type
            if (!string.IsNullOrEmpty(sourceType))
            {
                switch (sourceType)
                {
                    case "Stage":
                        dto.OperationType = OperationTypeEnum.StageActionExecution.ToString();
                        dto.OperationTypeDisplayName = GetEnumDescription(OperationTypeEnum.StageActionExecution);
                        dto.BusinessModule = BusinessModuleEnum.Stage.ToString();
                        break;
                    case "Task":
                        dto.OperationType = OperationTypeEnum.TaskActionExecution.ToString();
                        dto.OperationTypeDisplayName = GetEnumDescription(OperationTypeEnum.TaskActionExecution);
                        dto.BusinessModule = BusinessModuleEnum.Task.ToString();
                        break;
                    case "Question":
                        dto.OperationType = OperationTypeEnum.QuestionActionExecution.ToString();
                        dto.OperationTypeDisplayName = GetEnumDescription(OperationTypeEnum.QuestionActionExecution);
                        dto.BusinessModule = BusinessModuleEnum.Question.ToString();
                        break;
                }
            }

            return dto;
        }

        /// <summary>
        /// Convert ActionExecutionWithActionInfoDto to OperationChangeLogOutputDto (original method)
        /// </summary>
        private OperationChangeLogOutputDto MapActionExecutionToChangeLogDto(ActionExecutionWithActionInfoDto actionExecution)
        {
            // Calculate duration if not available
            var duration = actionExecution.DurationMs;
            if (!duration.HasValue && actionExecution.StartedAt.HasValue && actionExecution.CompletedAt.HasValue)
            {
                duration = (long)(actionExecution.CompletedAt.Value - actionExecution.StartedAt.Value).TotalMilliseconds;
            }

            // Extract action name and type, fallback to defaults if empty
            var actionName = !string.IsNullOrEmpty(actionExecution.ActionName)
                ? actionExecution.ActionName
                : $"Action-{actionExecution.ActionCode}";
            var actionType = !string.IsNullOrEmpty(actionExecution.ActionType)
                ? actionExecution.ActionType
                : "Python";

            // Generate title and description
            var operationTitle = $"Action Executed: {actionName}";
            var operationDescription = GenerateActionDescription(actionExecution, actionType, duration);

            // Generate operation type based on execution status
            var operationType = GetActionExecutionOperationType(actionExecution.ExecutionStatus);
            var operationTypeDisplayName = GetActionExecutionTypeDisplayName(actionExecution.ExecutionStatus);

            // Parse and include execution output in extended data
            var extendedData = GenerateActionExtendedData(actionExecution);

            // Set operator name with special handling for system operations
            var operatorName = !string.IsNullOrEmpty(actionExecution.CreatedBy)
                ? actionExecution.CreatedBy
                : "1"; // Default to "1" for system operations

            // Convert "1" to empty string as per business requirement
            if (string.Equals(operatorName, "1", StringComparison.OrdinalIgnoreCase))
            {
                operatorName = "";
            }

            return new OperationChangeLogOutputDto
            {
                Id = actionExecution.Id,
                OperationType = operationType,
                OperationTypeDisplayName = operationTypeDisplayName,
                BusinessModule = "ActionExecution",
                BusinessId = actionExecution.Id,
                OperationTitle = operationTitle,
                OperationDescription = operationDescription,
                BeforeData = null, // Action executions don't have before/after data
                AfterData = null,
                ChangedFields = new List<string>(),
                ExtendedData = extendedData,
                OperationStatus = GetActionExecutionOperationStatus(actionExecution.ExecutionStatus),
                OperationStatusDisplayName = GetActionExecutionStatusDisplayName(actionExecution.ExecutionStatus),
                ErrorMessage = actionExecution.ErrorMessage,
                OperationTime = actionExecution.StartedAt ?? actionExecution.CreatedAt,
                OperatorName = operatorName,
                CreateDate = actionExecution.CreatedAt,
                ModifyDate = actionExecution.CompletedAt ?? actionExecution.CreatedAt,
                CreateBy = actionExecution.CreatedBy,
                ModifyBy = actionExecution.CreatedBy,
                CreateUserId = 0, // ActionExecution doesn't have UserId fields
                ModifyUserId = 0
            };
        }

        /// <summary>
        /// Generate action execution description with rich information
        /// </summary>
        private string GenerateActionDescription(ActionExecutionWithActionInfoDto execution, string actionType, long? duration)
        {
            var status = execution.ExecutionStatus?.ToLower();
            var durationText = duration.HasValue ? $" (Duration: {duration}ms)" : "";

            var description = status switch
            {
                "success" or "completed" => $"{actionType} action completed successfully{durationText}",
                "failed" => $"{actionType} action failed: {GetActionErrorMessage(execution)}",
                "running" => $"{actionType} action is currently running",
                "pending" => $"{actionType} action is pending execution",
                "cancelled" => $"{actionType} action was cancelled",
                _ => $"{actionType} action status: {execution.ExecutionStatus}"
            };

            // Add detailed execution output information for all executions
            if (!string.IsNullOrEmpty(execution.ExecutionOutput))
            {
                try
                {
                    var executionOutputToken = JToken.Parse(execution.ExecutionOutput);
                    var outputDetails = GetFriendlyExecutionOutput(executionOutputToken);
                    if (!string.IsNullOrEmpty(outputDetails))
                    {
                        description += $". {outputDetails}";
                    }
                }
                catch
                {
                    // Ignore JSON parsing errors
                }
            }

            return description;
        }

        /// <summary>
        /// Get action execution operation type
        /// </summary>
        private string GetActionExecutionOperationType(string executionStatus)
        {
            return executionStatus?.ToLower() switch
            {
                "success" or "completed" => "ActionExecutionSuccess",
                "failed" => "ActionExecutionFailed",
                "running" => "ActionExecutionRunning",
                "pending" => "ActionExecutionPending",
                "cancelled" => "ActionExecutionCancelled",
                _ => "ActionExecution"
            };
        }

        /// <summary>
        /// Get action execution type display name
        /// </summary>
        private string GetActionExecutionTypeDisplayName(string executionStatus)
        {
            return executionStatus?.ToLower() switch
            {
                "success" or "completed" => "Action Execution Success",
                "failed" => "Action Execution Failed",
                "running" => "Action Execution Running",
                "pending" => "Action Execution Pending",
                "cancelled" => "Action Execution Cancelled",
                _ => "Action Execution"
            };
        }

        /// <summary>
        /// Get action execution operation status
        /// </summary>
        private string GetActionExecutionOperationStatus(string executionStatus)
        {
            return executionStatus?.ToLower() switch
            {
                "success" or "completed" => OperationStatusEnum.Success.ToString(),
                "failed" => OperationStatusEnum.Failed.ToString(),
                "running" => OperationStatusEnum.InProgress.ToString(),
                "pending" => OperationStatusEnum.Pending.ToString(),
                "cancelled" => OperationStatusEnum.Cancelled.ToString(),
                _ => OperationStatusEnum.Unknown.ToString()
            };
        }

        /// <summary>
        /// Get action execution status display name
        /// </summary>
        private string GetActionExecutionStatusDisplayName(string executionStatus)
        {
            return executionStatus?.ToLower() switch
            {
                "success" or "completed" => "Completed Successfully",
                "failed" => "Failed",
                "running" => "In Progress",
                "pending" => "Pending",
                "cancelled" => "Cancelled",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Get error message from action execution
        /// </summary>
        private string GetActionErrorMessage(ActionExecutionWithActionInfoDto execution)
        {
            if (!string.IsNullOrEmpty(execution.ErrorMessage))
                return execution.ErrorMessage;

            try
            {
                if (!string.IsNullOrEmpty(execution.ExecutionOutput))
                {
                    var output = JToken.Parse(execution.ExecutionOutput);
                    if (output.Type == JTokenType.Object)
                    {
                        var outputObj = output as JObject;
                        if (outputObj?.TryGetValue("message", out var message) == true)
                        {
                            return message.ToString();
                        }
                        if (outputObj?.TryGetValue("errorDetails", out var errorDetails) == true)
                        {
                            return errorDetails.ToString();
                        }
                    }
                }
            }
            catch { }

            return "Unknown error";
        }

        /// <summary>
        /// Get action output summary
        /// </summary>
        private string GetActionOutputSummary(Newtonsoft.Json.Linq.JToken executionOutput)
        {
            try
            {
                if (executionOutput?.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                {
                    var output = executionOutput as Newtonsoft.Json.Linq.JObject;
                    if (output == null) return "";

                    var summaryParts = new List<string>();

                    // Check for stdout content
                    if (output.TryGetValue("stdout", out var stdout) && !string.IsNullOrWhiteSpace(stdout.ToString()))
                    {
                        var stdoutText = stdout.ToString().Trim();
                        if (stdoutText.Length > 50)
                            stdoutText = stdoutText.Substring(0, 50) + "...";
                        summaryParts.Add($"Output: {stdoutText}");
                    }

                    // Check for memory usage
                    if (output.TryGetValue("memoryUsage", out var memory) && long.TryParse(memory.ToString(), out var memoryBytes))
                    {
                        var memoryKB = memoryBytes / 1024.0;
                        summaryParts.Add($"Memory: {memoryKB:F1}KB");
                    }

                    // Check for execution status/token
                    if (output.TryGetValue("status", out var statusToken))
                    {
                        summaryParts.Add($"Status: {statusToken}");
                    }

                    return string.Join(", ", summaryParts);
                }
            }
            catch { }

            return "";
        }

        /// <summary>
        /// Get action context summary
        /// </summary>
        private string GetActionContextSummary(Newtonsoft.Json.Linq.JToken triggerContext)
        {
            try
            {
                if (triggerContext?.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                {
                    var context = triggerContext as Newtonsoft.Json.Linq.JObject;
                    if (context == null) return "";

                    var summaryParts = new List<string>();

                    // Extract key information from trigger context
                    foreach (var prop in context.Properties())
                    {
                        if (prop.Name.ToLower() == "workflowid")
                        {
                            summaryParts.Add($"Workflow: {prop.Value}");
                        }
                        else if (!prop.Name.ToLower().Contains("id") &&
                                 !prop.Name.ToLower().Contains("event") &&
                                 !prop.Name.ToLower().Contains("source"))
                        {
                            summaryParts.Add($"{prop.Name}: {prop.Value}");
                        }
                    }

                    return string.Join(", ", summaryParts);
                }
            }
            catch { }

            return "";
        }

        /// <summary>
        /// Generate extended data for action execution including full execution output
        /// </summary>
        private string GenerateActionExtendedData(ActionExecutionWithActionInfoDto actionExecution)
        {
            try
            {
                var extendedData = new
                {
                    ActionCode = actionExecution.ActionCode,
                    ActionType = actionExecution.ActionType,
                    ExecutionId = actionExecution.ExecutionId,
                    ExecutionStatus = actionExecution.ExecutionStatus,
                    StartedAt = actionExecution.StartedAt,
                    CompletedAt = actionExecution.CompletedAt,
                    DurationMs = actionExecution.DurationMs,
                    TriggerContext = !string.IsNullOrEmpty(actionExecution.TriggerContext)
                        ? JToken.Parse(actionExecution.TriggerContext)
                        : null,
                    ExecutionInput = !string.IsNullOrEmpty(actionExecution.ExecutionInput)
                        ? JToken.Parse(actionExecution.ExecutionInput)
                        : null,
                    ExecutionOutput = !string.IsNullOrEmpty(actionExecution.ExecutionOutput)
                        ? JToken.Parse(actionExecution.ExecutionOutput)
                        : null,
                    ErrorMessage = actionExecution.ErrorMessage,
                    ErrorStackTrace = actionExecution.ErrorStackTrace,
                    ExecutorInfo = !string.IsNullOrEmpty(actionExecution.ExecutorInfo)
                        ? JToken.Parse(actionExecution.ExecutorInfo)
                        : null
                };

                return JsonSerializer.Serialize(extendedData, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate extended data for action execution {ExecutionId}", actionExecution.ExecutionId);

                // Fallback to simpler format
                return JsonSerializer.Serialize(new
                {
                    ActionCode = actionExecution.ActionCode,
                    ExecutionStatus = actionExecution.ExecutionStatus,
                    ErrorMessage = actionExecution.ErrorMessage
                });
            }
        }

        /// <summary>
        /// Get friendly execution output description for operation description
        /// </summary>
        private string GetFriendlyExecutionOutput(Newtonsoft.Json.Linq.JToken executionOutput)
        {
            try
            {
                if (executionOutput?.Type != Newtonsoft.Json.Linq.JTokenType.Object)
                    return "";

                var output = executionOutput as Newtonsoft.Json.Linq.JObject;
                if (output == null) return "";

                var details = new List<string>();

                // Extract key information in a user-friendly way
                if (output.TryGetValue("success", out var success))
                {
                    var isSuccess = success.Value<bool>();
                    details.Add($"Result: {(isSuccess ? "Success" : "Failed")}");
                }

                if (output.TryGetValue("status", out var status))
                {
                    details.Add($"Status: {status}");
                }

                if (output.TryGetValue("message", out var message) && !string.IsNullOrEmpty(message.ToString()))
                {
                    var messageText = message.ToString();
                    if (messageText.Length > 100)
                        messageText = messageText.Substring(0, 100) + "...";
                    details.Add($"Message: {messageText}");
                }

                if (output.TryGetValue("executionTime", out var execTime))
                {
                    details.Add($"Execution Time: {execTime}s");
                }

                if (output.TryGetValue("memoryUsage", out var memory) && long.TryParse(memory.ToString(), out var memoryBytes))
                {
                    var memoryKB = memoryBytes / 1024.0;
                    details.Add($"Memory Usage: {memoryKB:F1}KB");
                }

                if (output.TryGetValue("token", out var token) && !string.IsNullOrEmpty(token.ToString()))
                {
                    var tokenStr = token.ToString();
                    if (tokenStr.Length > 12)
                        tokenStr = tokenStr.Substring(0, 8) + "...";
                    details.Add($"Token: {tokenStr}");
                }

                // Extract summary from stdout if available
                if (output.TryGetValue("stdout", out var stdout) && !string.IsNullOrEmpty(stdout.ToString()))
                {
                    var stdoutText = stdout.ToString().Trim();

                    // Look for specific patterns in stdout
                    if (stdoutText.Contains("Action completed successfully"))
                    {
                        details.Add("Output: Action completed successfully");
                    }
                    else if (stdoutText.Contains("=== Action Execution Started ==="))
                    {
                        details.Add("Output: Execution started and processed");
                    }
                    else if (stdoutText.Length > 0)
                    {
                        // Extract first meaningful line
                        var lines = stdoutText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        var meaningfulLine = lines.FirstOrDefault(line =>
                            !line.Trim().StartsWith("===") &&
                            !string.IsNullOrWhiteSpace(line.Trim()));

                        if (!string.IsNullOrEmpty(meaningfulLine))
                        {
                            if (meaningfulLine.Length > 50)
                                meaningfulLine = meaningfulLine.Substring(0, 50) + "...";
                            details.Add($"Output: {meaningfulLine.Trim()}");
                        }
                    }
                }

                if (output.TryGetValue("stderr", out var stderr) &&
                    stderr != null &&
                    !string.IsNullOrEmpty(stderr.ToString()) &&
                    stderr.ToString().ToLower() != "null")
                {
                    var stderrText = stderr.ToString();
                    if (stderrText.Length > 50)
                        stderrText = stderrText.Substring(0, 50) + "...";
                    details.Add($"Error: {stderrText}");
                }

                return string.Join(", ", details);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to parse friendly execution output");
                return "";
            }
        }

        /// <summary>
        /// Get operation log list for task
        /// </summary>
        public async Task<PagedResult<OperationChangeLogOutputDto>> GetOperationLogsByTaskAsync(long taskId, long? onboardingId = null, OperationTypeEnum? operationType = null, int pageIndex = 1, int pageSize = 20, bool includeActionExecutions = true)
        {
            try
            {
                List<OperationChangeLogOutputDto> allLogs = new List<OperationChangeLogOutputDto>();

                // Get operation change logs for task
                List<OperationChangeLog> logs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Task.ToString(), taskId);

                // Apply filters
                if (onboardingId.HasValue && logs.Any())
                {
                    logs = logs.Where(x => x.OnboardingId == onboardingId.Value).ToList();
                }

                if (operationType.HasValue && logs.Any())
                {
                    logs = logs.Where(x => x.OperationType == operationType.ToString()).ToList();
                }

                // Convert operation logs to output DTOs
                var operationLogDtos = logs.Select(MapToOutputDto).ToList();
                allLogs.AddRange(operationLogDtos);

                // Get action execution logs if requested
                if (includeActionExecutions)
                {
                    try
                    {
                        // Prepare JSON conditions for filtering by onboardingId if provided
                        List<JsonQueryCondition> jsonConditions = null;
                        if (onboardingId.HasValue)
                        {
                            jsonConditions = new List<JsonQueryCondition>
                            {
                                new JsonQueryCondition
                                {
                                    JsonPath = "OnboardingId",
                                    Operator = "=",
                                    Value = onboardingId.Value.ToString()
                                }
                            };
                        }

                        var actionExecutionsResult = await _actionExecutionService.GetExecutionsByTriggerSourceIdAsync(
                            taskId, 1, 1000, jsonConditions); // Get more records for merging with filtering

                        // Convert action executions to change log DTOs
                        var actionExecutionDtos = actionExecutionsResult.Data
                            .Select(ae => MapActionExecutionToChangeLogDto(ae, "Task"))
                            .ToList();
                        allLogs.AddRange(actionExecutionDtos);

                        _logger.LogInformation($"Integrated {actionExecutionDtos.Count} action executions for taskId {taskId}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch action executions for taskId {TaskId}, continuing without action logs", taskId);
                        // Continue without action executions rather than failing the entire request
                    }
                }

                // Sort all logs by operation time (descending)
                allLogs = allLogs.OrderByDescending(x => x.OperationTime).ToList();

                // Apply pagination to combined results
                int totalCount = allLogs.Count;
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
                _logger.LogError(ex, "Error getting operation logs for task {TaskId}", taskId);
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        /// <summary>
        /// Get operation log list for question
        /// </summary>
        public async Task<PagedResult<OperationChangeLogOutputDto>> GetOperationLogsByQuestionAsync(long questionId, long? onboardingId = null, OperationTypeEnum? operationType = null, int pageIndex = 1, int pageSize = 20, bool includeActionExecutions = true)
        {
            try
            {
                List<OperationChangeLogOutputDto> allLogs = new List<OperationChangeLogOutputDto>();

                // Get operation change logs for question
                List<OperationChangeLog> logs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Question.ToString(), questionId);

                // Apply filters
                if (onboardingId.HasValue && logs.Any())
                {
                    logs = logs.Where(x => x.OnboardingId == onboardingId.Value).ToList();
                }

                if (operationType.HasValue && logs.Any())
                {
                    logs = logs.Where(x => x.OperationType == operationType.ToString()).ToList();
                }

                // Convert operation logs to output DTOs
                var operationLogDtos = logs.Select(MapToOutputDto).ToList();
                allLogs.AddRange(operationLogDtos);

                // Get action execution logs if requested
                if (includeActionExecutions)
                {
                    try
                    {
                        // Prepare JSON conditions for filtering by onboardingId if provided
                        List<JsonQueryCondition> jsonConditions = null;
                        if (onboardingId.HasValue)
                        {
                            jsonConditions = new List<JsonQueryCondition>
                            {
                                new JsonQueryCondition
                                {
                                    JsonPath = "OnboardingId",
                                    Operator = "=",
                                    Value = onboardingId.Value.ToString()
                                }
                            };
                        }

                        var actionExecutionsResult = await _actionExecutionService.GetExecutionsByTriggerSourceIdAsync(
                            questionId, 1, 1000, jsonConditions); // Get more records for merging with filtering

                        // Convert action executions to change log DTOs
                        var actionExecutionDtos = actionExecutionsResult.Data
                            .Select(ae => MapActionExecutionToChangeLogDto(ae, "Question"))
                            .ToList();
                        allLogs.AddRange(actionExecutionDtos);

                        _logger.LogInformation($"Integrated {actionExecutionDtos.Count} action executions for questionId {questionId}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch action executions for questionId {QuestionId}, continuing without action logs", questionId);
                        // Continue without action executions rather than failing the entire request
                    }
                }

                // Sort all logs by operation time (descending)
                allLogs = allLogs.OrderByDescending(x => x.OperationTime).ToList();

                // Apply pagination to combined results
                int totalCount = allLogs.Count;
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
                _logger.LogError(ex, "Error getting operation logs for question {QuestionId}", questionId);
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        /// <summary>
        /// Get operation log list for stage components (OPTIMIZED VERSION with database-level pagination)
        /// </summary>
        public async Task<PagedResult<OperationChangeLogOutputDto>> GetOperationLogsByStageComponentsOptimizedAsync(long stageId, long? onboardingId = null, OperationTypeEnum? operationType = null, int pageIndex = 1, int pageSize = 20, bool includeActionExecutions = true)
        {
            try
            {
                // Step 1: Collect task and question IDs in a single batch operation
                var (taskIds, questionIds) = await GetTaskAndQuestionIdsBatchAsync(stageId);

                _logger.LogInformation("Collected {TaskCount} tasks and {QuestionCount} questions for stage {StageId}. TaskIds: [{TaskIds}], QuestionIds: [{QuestionIds}]",
                    taskIds.Count, questionIds.Count, stageId,
                    string.Join(", ", taskIds), string.Join(", ", questionIds));

                // Step 2: Use optimized repository method to get paginated results from database
                var (logs, totalCount) = await _operationChangeLogRepository.GetStageComponentLogsPaginatedAsync(
                    onboardingId,
                    stageId,
                    taskIds,
                    questionIds,
                    operationType?.ToString(),
                    pageIndex,
                    pageSize);

                // Step 3: Convert to DTOs (only for current page data)
                var operationLogDtos = logs.Select(MapToOutputDto).ToList();

                // Step 4: Optionally add action executions for current page only
                if (includeActionExecutions)
                {
                    // Create source ID type mapping for proper action execution categorization
                    var sourceIdTypes = new Dictionary<long, string>();
                    sourceIdTypes[stageId] = "Stage"; // Stage itself

                    foreach (var taskId in taskIds)
                    {
                        sourceIdTypes[taskId] = "Task";
                    }

                    foreach (var questionId in questionIds)
                    {
                        sourceIdTypes[questionId] = "Question";
                    }

                    await AddActionExecutionsBatchOptimizedAsync(operationLogDtos, sourceIdTypes, onboardingId);
                }

                _logger.LogDebug("Returned {Count} operation logs from {TotalCount} total for stage {StageId}",
                    operationLogDtos.Count, totalCount, stageId);

                return new PagedResult<OperationChangeLogOutputDto>
                {
                    Items = operationLogDtos,
                    TotalCount = totalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting optimized operation logs for stage components {StageId}", stageId);
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        /// <summary>
        /// Get operation log list for stage components (all related tasks and questions)
        /// </summary>
        public async Task<PagedResult<OperationChangeLogOutputDto>> GetOperationLogsByStageComponentsAsync(long stageId, long? onboardingId = null, OperationTypeEnum? operationType = null, int pageIndex = 1, int pageSize = 20, bool includeActionExecutions = true)
        {
            try
            {
                List<OperationChangeLogOutputDto> allLogs = new List<OperationChangeLogOutputDto>();

                // Get operation change logs using the same logic as the original GetOperationLogsAsync method
                List<OperationChangeLog> logs;

                // Use the same logic as GetOperationLogsAsync to get comprehensive operation logs
                if (onboardingId.HasValue)
                {
                    logs = await _operationChangeLogRepository.GetByOnboardingAndStageAsync(onboardingId.Value, stageId);
                }
                else
                {
                    logs = await _operationChangeLogRepository.GetByStageIdAsync(stageId);
                }

                // Apply operationType filter if provided
                if (operationType.HasValue && logs.Any())
                {
                    logs = logs.Where(x => x.OperationType == operationType.ToString()).ToList();
                }

                // Convert operation logs to output DTOs
                var operationLogDtos = logs.Select(MapToOutputDto).ToList();
                allLogs.AddRange(operationLogDtos);

                // Additionally, get operation logs for all tasks and questions in this stage
                await CollectTaskAndQuestionOperationLogsAsync(stageId, onboardingId, operationType, allLogs);

                // Action executions are now collected through CollectOperationLogsForQuestionsAsync and CollectOperationLogsForTasksAsync
                // to avoid duplication and provide better control over question vs option actions

                // Sort all logs by operation time (descending)
                allLogs = allLogs.OrderByDescending(x => x.OperationTime).ToList();

                // Apply pagination to combined results
                int totalCount = allLogs.Count;
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
                _logger.LogError(ex, "Error getting operation logs for stage components {StageId}", stageId);
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        /// <summary>
        /// Collect operation logs for all tasks and questions in this stage
        /// </summary>
        private async Task CollectTaskAndQuestionOperationLogsAsync(long stageId, long? onboardingId, OperationTypeEnum? operationType, List<OperationChangeLogOutputDto> allLogs)
        {
            try
            {
                // Get stage components to find all tasks and questions
                var stageService = _serviceProvider.GetRequiredService<IStageService>();
                var stageComponents = await stageService.GetComponentsAsync(stageId);
                _logger.LogDebug("Found {Count} components for stage {StageId}", stageComponents.Count, stageId);

                // Collect task and question IDs
                var taskIds = new List<long>();
                var questionIds = new List<long>();
                await CollectTaskAndQuestionIdsAsync(stageComponents, taskIds, questionIds);

                // Get operation logs for all tasks
                await CollectOperationLogsForTasksAsync(taskIds, onboardingId, operationType, allLogs);

                // Get operation logs for all questions
                await CollectOperationLogsForQuestionsAsync(questionIds, onboardingId, operationType, allLogs);

                _logger.LogDebug("Collected operation logs for {TaskCount} tasks and {QuestionCount} questions",
                    taskIds.Count, questionIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting task and question operation logs for stage {StageId}", stageId);
            }
        }

        /// <summary>
        /// Collect task and question IDs from stage components (including option IDs as question action sources)
        /// </summary>
        private async Task CollectTaskAndQuestionIdsAsync(List<StageComponent> stageComponents, List<long> taskIds, List<long> questionIds)
        {
            try
            {
                // Collect task IDs from checklist components
                var checklistComponents = stageComponents.Where(c => c.Key == "checklist").ToList();
                if (checklistComponents.Any())
                {
                    var checklistIdsList = checklistComponents
                        .SelectMany(c => c.ChecklistIds ?? new List<long>())
                        .Distinct()
                        .ToList();

                    if (checklistIdsList.Any())
                    {
                        var checklistService = _serviceProvider.GetRequiredService<IChecklistService>();
                        var checklists = await checklistService.GetByIdsAsync(checklistIdsList);

                        foreach (var checklist in checklists)
                        {
                            if (checklist.Tasks?.Any() == true)
                            {
                                taskIds.AddRange(checklist.Tasks.Select(t => t.Id));
                            }
                        }
                    }
                }

                // Collect question IDs from questionnaire components (using HashSet to avoid duplicates)
                var questionnaireComponents = stageComponents.Where(c => c.Key == "questionnaires").ToList();
                if (questionnaireComponents.Any())
                {
                    var questionnaireIdsList = questionnaireComponents
                        .SelectMany(c => c.QuestionnaireIds ?? new List<long>())
                        .Distinct()
                        .ToList();

                    if (questionnaireIdsList.Any())
                    {
                        var questionnaireService = _serviceProvider.GetRequiredService<IQuestionnaireService>();
                        var questionnaires = await questionnaireService.GetByIdsAsync(questionnaireIdsList);

                        // Use HashSet to prevent duplicate IDs
                        var uniqueQuestionIds = new HashSet<long>();

                        foreach (var questionnaire in questionnaires)
                        {
                            if (string.IsNullOrWhiteSpace(questionnaire.StructureJson)) continue;

                            try
                            {
                                var structureData = JsonSerializer.Deserialize<QuestionnaireStructure>(
                                    questionnaire.StructureJson,
                                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                                if (structureData?.Sections?.Any() != true) continue;

                                foreach (var section in structureData.Sections)
                                {
                                    var allQuestions = new List<QuestionnaireQuestion>();
                                    if (section.Items?.Any() == true) allQuestions.AddRange(section.Items);
                                    if (section.Questions?.Any() == true) allQuestions.AddRange(section.Questions);

                                    allQuestions = allQuestions.GroupBy(q => q.Id).Select(g => g.First()).ToList();

                                    foreach (var question in allQuestions)
                                    {
                                        // 添加question ID
                                        if (long.TryParse(question.Id, out long questionIdLong))
                                        {
                                            uniqueQuestionIds.Add(questionIdLong);
                                        }

                                        // 添加option IDs（option的action作为question action处理）
                                        if (question.Options?.Any() == true)
                                        {
                                            foreach (var option in question.Options)
                                            {
                                                // 解析option对象获取ID
                                                var optionJson = JsonSerializer.Serialize(option);
                                                using var optionDoc = JsonDocument.Parse(optionJson);
                                                var optionElement = optionDoc.RootElement;

                                                // 检查option是否有action
                                                if (optionElement.TryGetProperty("action", out var actionElement))
                                                {
                                                    // 优先使用正式的id，如果没有则跳过
                                                    var optionId = optionElement.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;

                                                    if (!string.IsNullOrWhiteSpace(optionId) && long.TryParse(optionId, out long optionIdLong))
                                                    {
                                                        uniqueQuestionIds.Add(optionIdLong); // 将option ID作为question ID处理
                                                        _logger.LogDebug("Added option ID {OptionId} as question action source for questionnaire {QuestionnaireId}",
                                                            optionIdLong, questionnaire.Id);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogError(ex, "Error parsing StructureJson for questionnaire {QuestionnaireId}", questionnaire.Id);
                            }
                        }

                        // Add unique IDs to the questionIds list
                        questionIds.AddRange(uniqueQuestionIds);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting task and question IDs from stage components");
            }
        }

        /// <summary>
        /// Collect operation logs for tasks
        /// </summary>
        private async Task CollectOperationLogsForTasksAsync(List<long> taskIds, long? onboardingId, OperationTypeEnum? operationType, List<OperationChangeLogOutputDto> allLogs)
        {
            try
            {
                foreach (var taskId in taskIds)
                {
                    var taskLogs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Task.ToString(), taskId);

                    // Apply filters
                    if (onboardingId.HasValue && taskLogs.Any())
                    {
                        taskLogs = taskLogs.Where(x => x.OnboardingId == onboardingId.Value).ToList();
                    }

                    if (operationType.HasValue && taskLogs.Any())
                    {
                        taskLogs = taskLogs.Where(x => x.OperationType == operationType.ToString()).ToList();
                    }

                    // Convert to DTOs and add to allLogs
                    var taskLogDtos = taskLogs.Select(MapToOutputDto).ToList();
                    allLogs.AddRange(taskLogDtos);
                }

                _logger.LogDebug("Collected operation logs for {TaskCount} tasks", taskIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting operation logs for tasks");
            }
        }

        /// <summary>
        /// Collect operation logs for questions (including option action executions as question actions)
        /// </summary>
        private async Task CollectOperationLogsForQuestionsAsync(List<long> questionIds, long? onboardingId, OperationTypeEnum? operationType, List<OperationChangeLogOutputDto> allLogs)
        {
            try
            {
                foreach (var questionId in questionIds)
                {
                    // Get traditional operation logs for questions
                    var questionLogs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Question.ToString(), questionId);

                    // Apply filters
                    if (onboardingId.HasValue && questionLogs.Any())
                    {
                        questionLogs = questionLogs.Where(x => x.OnboardingId == onboardingId.Value).ToList();
                    }

                    if (operationType.HasValue && questionLogs.Any())
                    {
                        questionLogs = questionLogs.Where(x => x.OperationType == operationType.ToString()).ToList();
                    }

                    // Convert to DTOs and add to allLogs
                    var questionLogDtos = questionLogs.Select(MapToOutputDto).ToList();
                    allLogs.AddRange(questionLogDtos);

                    // Get action execution logs for this question/option ID (treating option actions as question actions)
                    try
                    {
                        // Prepare JSON conditions for filtering by onboardingId if provided
                        List<JsonQueryCondition> jsonConditions = null;
                        if (onboardingId.HasValue)
                        {
                            jsonConditions = new List<JsonQueryCondition>
                            {
                                new JsonQueryCondition
                                {
                                    JsonPath = "OnboardingId",
                                    Operator = "=",
                                    Value = onboardingId.Value.ToString()
                                }
                            };
                        }

                        var actionExecutionsResult = await _actionExecutionService.GetExecutionsByTriggerSourceIdAsync(
                            questionId, 1, 1000, jsonConditions);

                        // Convert action executions to change log DTOs (treating all as "Question" type)
                        var actionExecutionDtos = actionExecutionsResult.Data
                            .Select(ae => MapActionExecutionToChangeLogDto(ae, "Question"))
                            .ToList();
                        allLogs.AddRange(actionExecutionDtos);

                        if (actionExecutionDtos.Any())
                        {
                            _logger.LogDebug("Added {Count} action executions for sourceId {SourceId} as question actions",
                                actionExecutionDtos.Count, questionId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch action executions for sourceId {SourceId}, continuing without action logs", questionId);
                        // Continue without action executions rather than failing the entire request
                    }
                }

                _logger.LogDebug("Collected operation logs for {QuestionCount} questions/options", questionIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting operation logs for questions");
            }
        }

        /// <summary>
        /// Collect all source IDs (tasks and questions) from stage components with their types
        /// All IDs will be used the same way: /api/action/v1/executions/trigger-source/{sourceId}/search
        /// </summary>
        private async Task CollectAllSourceIdsFromStageComponentsAsync(List<StageComponent> stageComponents, Dictionary<long, string> sourceIdTypes)
        {
            try
            {
                // Reuse the same logic for collecting IDs
                var taskIds = new List<long>();
                var questionIds = new List<long>();
                await CollectTaskAndQuestionIdsAsync(stageComponents, taskIds, questionIds);

                // Add to sourceIdTypes with correct types
                foreach (var taskId in taskIds)
                {
                    sourceIdTypes.TryAdd(taskId, "Task");
                }

                foreach (var questionId in questionIds)
                {
                    sourceIdTypes.TryAdd(questionId, "Question");
                }

                _logger.LogDebug("Collected {TaskCount} task IDs and {QuestionCount} question IDs for action executions",
                    taskIds.Count, questionIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting source IDs from stage components");
            }
        }



        /// <summary>
        /// Collect action executions for all source IDs using the unified API
        /// All source IDs (stage, task, question) use the same API: /api/action/v1/executions/trigger-source/{sourceId}/search
        /// OPTIMIZED: Use batch querying, bulk caching, and early exit for better performance
        /// </summary>
        private async Task CollectActionExecutionsForAllSourceIdsAsync(Dictionary<long, string> sourceIdTypes, List<OperationChangeLogOutputDto> allLogs, long? onboardingId)
        {
            try
            {
                var totalActionExecutions = 0;
                var skippedFromCache = 0;
                var batchSize = 20; // Increased batch size for better efficiency

                // Pre-filter out cached empty source IDs
                var uncachedSourceIds = sourceIdTypes.Where(kvp => !IsSourceIdCachedAsEmpty(kvp.Key, onboardingId))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                skippedFromCache = sourceIdTypes.Count - uncachedSourceIds.Count;

                if (uncachedSourceIds.Count == 0)
                {
                    _logger.LogInformation("All {TotalCount} source IDs are cached as empty, skipping API calls", sourceIdTypes.Count);
                    return;
                }

                var sourceIdBatches = uncachedSourceIds.Keys.Select((id, index) => new { id, index })
                    .GroupBy(x => x.index / batchSize)
                    .Select(g => g.Select(x => x.id).ToList())
                    .ToList();

                _logger.LogInformation("Processing {UncachedSourceIds} uncached source IDs (skipped {CachedCount} cached) in {BatchCount} batches of {BatchSize}",
                    uncachedSourceIds.Count, skippedFromCache, sourceIdBatches.Count, batchSize);

                var emptySourceIds = new List<long>(); // Collect empty source IDs for bulk caching

                foreach (var batch in sourceIdBatches)
                {
                    var batchTasks = batch.Select(async sourceId =>
                    {
                        var sourceType = uncachedSourceIds[sourceId];
                        try
                        {
                            // Prepare JSON conditions for filtering by onboardingId if provided
                            List<JsonQueryCondition> jsonConditions = null;
                            if (onboardingId.HasValue)
                            {
                                jsonConditions = new List<JsonQueryCondition>
                                {
                                    new JsonQueryCondition
                                    {
                                        JsonPath = "OnboardingId",
                                        Operator = "=",
                                        Value = onboardingId.Value.ToString()
                                    }
                                };
                            }

                            // Call the unified API: /api/action/v1/executions/trigger-source/{sourceId}/search
                            var actionExecutionsResult = await _actionExecutionService.GetExecutionsByTriggerSourceIdAsync(
                                sourceId, 1, 1000, jsonConditions);

                            if (actionExecutionsResult?.Data?.Any() == true)
                            {
                                var actionExecutionDtos = actionExecutionsResult.Data
                                    .Select(ae => MapActionExecutionToChangeLogDto(ae, sourceType))
                                    .ToList();

                                if (_logger.IsEnabled(LogLevel.Debug))
                                {
                                    _logger.LogDebug("Found {Count} {SourceType} action executions for sourceId {SourceId}",
                                        actionExecutionDtos.Count, sourceType, sourceId);
                                }

                                return new { SourceId = sourceId, Results = actionExecutionDtos, IsEmpty = false };
                            }
                            else
                            {
                                return new { SourceId = sourceId, Results = new List<OperationChangeLogOutputDto>(), IsEmpty = true };
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to fetch action executions for {SourceType} sourceId {SourceId}, continuing", sourceType, sourceId);
                            return new { SourceId = sourceId, Results = new List<OperationChangeLogOutputDto>(), IsEmpty = true };
                        }
                    });

                    var batchResults = await Task.WhenAll(batchTasks);

                    foreach (var result in batchResults)
                    {
                        if (result.IsEmpty)
                        {
                            emptySourceIds.Add(result.SourceId);
                        }
                        else if (result.Results.Any())
                        {
                            allLogs.AddRange(result.Results);
                            totalActionExecutions += result.Results.Count;
                        }
                    }
                }

                // Bulk cache empty source IDs
                if (emptySourceIds.Any())
                {
                    foreach (var sourceId in emptySourceIds)
                    {
                        CacheSourceIdAsEmpty(sourceId, onboardingId);
                    }

                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Cached {EmptyCount} source IDs as empty for future optimization", emptySourceIds.Count);
                    }
                }

                _logger.LogInformation("Action execution collection completed: {TotalCount} executions from {ProcessedCount} source IDs ({CachedCount} cached, {EmptyCount} newly cached as empty)",
                    totalActionExecutions, uncachedSourceIds.Count, skippedFromCache, emptySourceIds.Count);

                // Log optimization statistics
                LogBatchQueryOptimizationStats(sourceIdTypes.Count, skippedFromCache, uncachedSourceIds.Count, totalActionExecutions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting action executions for all source IDs");
            }
        }

        /// <summary>
        /// Optimized method to get task and question IDs in batch operations
        /// </summary>
        private async Task<(List<long> taskIds, List<long> questionIds)> GetTaskAndQuestionIdsBatchAsync(long stageId)
        {
            var taskIds = new List<long>();
            var questionIds = new List<long>();

            try
            {
                // Get stage components once
                var stageService = _serviceProvider.GetRequiredService<IStageService>();
                var stageComponents = await stageService.GetComponentsAsync(stageId);

                // Collect task IDs from checklist components
                var checklistComponents = stageComponents.Where(c => c.Key == "checklist").ToList();
                if (checklistComponents.Any())
                {
                    var checklistIdsList = checklistComponents
                        .SelectMany(c => c.ChecklistIds ?? new List<long>())
                        .Distinct()
                        .ToList();

                    if (checklistIdsList.Any())
                    {
                        var checklistService = _serviceProvider.GetRequiredService<IChecklistService>();
                        var checklists = await checklistService.GetByIdsAsync(checklistIdsList);

                        foreach (var checklist in checklists)
                        {
                            if (checklist.Tasks?.Any() == true)
                            {
                                taskIds.AddRange(checklist.Tasks.Select(t => t.Id));
                            }
                        }
                    }
                }

                // Collect question IDs from questionnaire components
                var questionnaireComponents = stageComponents.Where(c => c.Key == "questionnaires").ToList();
                if (questionnaireComponents.Any())
                {
                    var questionnaireIdsList = questionnaireComponents
                        .SelectMany(c => c.QuestionnaireIds ?? new List<long>())
                        .Distinct()
                        .ToList();

                    if (questionnaireIdsList.Any())
                    {
                        var questionnaireService = _serviceProvider.GetRequiredService<IQuestionnaireService>();
                        var questionnaires = await questionnaireService.GetByIdsAsync(questionnaireIdsList);

                        var uniqueQuestionIds = new HashSet<long>();

                        foreach (var questionnaire in questionnaires)
                        {
                            if (string.IsNullOrWhiteSpace(questionnaire.StructureJson)) continue;

                            try
                            {
                                var structureData = JsonSerializer.Deserialize<QuestionnaireStructure>(
                                    questionnaire.StructureJson,
                                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                                if (structureData?.Sections?.Any() != true) continue;

                                foreach (var section in structureData.Sections)
                                {
                                    var allQuestions = new List<QuestionnaireQuestion>();
                                    if (section.Items?.Any() == true) allQuestions.AddRange(section.Items);
                                    if (section.Questions?.Any() == true) allQuestions.AddRange(section.Questions);

                                    allQuestions = allQuestions.GroupBy(q => q.Id).Select(g => g.First()).ToList();

                                    foreach (var question in allQuestions)
                                    {
                                        if (long.TryParse(question.Id, out long questionIdLong))
                                        {
                                            uniqueQuestionIds.Add(questionIdLong);
                                        }

                                        // Add option IDs as well for action executions
                                        if (question.Options?.Any() == true)
                                        {
                                            foreach (var option in question.Options)
                                            {
                                                var optionJson = JsonSerializer.Serialize(option);
                                                using var optionDoc = JsonDocument.Parse(optionJson);
                                                var optionElement = optionDoc.RootElement;

                                                if (optionElement.TryGetProperty("action", out var actionElement))
                                                {
                                                    var optionId = optionElement.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;

                                                    if (!string.IsNullOrWhiteSpace(optionId) && long.TryParse(optionId, out long optionIdLong))
                                                    {
                                                        uniqueQuestionIds.Add(optionIdLong);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to parse questionnaire structure for questionnaire {QuestionnaireId}", questionnaire.Id);
                            }
                        }

                        questionIds.AddRange(uniqueQuestionIds);
                    }
                }

                // Remove duplicate IDs and apply smart filtering to reduce unnecessary queries
                var filteredTaskIds = taskIds.Distinct().ToList();
                var filteredQuestionIds = questionIds.Distinct().ToList();

                _logger.LogInformation("Collected {TaskCount} unique task IDs and {QuestionCount} unique question IDs for stage {StageId}",
                    filteredTaskIds.Count, filteredQuestionIds.Count, stageId);

                return (filteredTaskIds, filteredQuestionIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting task and question IDs for stage {StageId}", stageId);
                return (new List<long>(), new List<long>());
            }
        }

        /// <summary>
        /// Add action executions in batch for better performance with proper source type mapping
        /// OPTIMIZED: Use parallel processing and reduce logging noise
        /// </summary>
        private async Task AddActionExecutionsBatchOptimizedAsync(List<OperationChangeLogOutputDto> operationLogDtos, Dictionary<long, string> sourceIdTypes, long? onboardingId)
        {
            try
            {
                if (!sourceIdTypes.Any()) return;

                // Pre-filter cached empty source IDs
                var uncachedSourceIds = sourceIdTypes.Where(kvp => !IsSourceIdCachedAsEmpty(kvp.Key, onboardingId))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                var skippedFromCache = sourceIdTypes.Count - uncachedSourceIds.Count;

                if (uncachedSourceIds.Count == 0)
                {
                    _logger.LogInformation("All {TotalCount} source IDs are cached as empty, skipping API calls", sourceIdTypes.Count);
                    return;
                }

                _logger.LogInformation("Fetching action executions for {UncachedCount} uncached source IDs (skipped {CachedCount} cached)",
                    uncachedSourceIds.Count, skippedFromCache);

                var emptySourceIds = new List<long>(); // Collect empty source IDs for bulk caching

                // Use controlled concurrent processing to avoid overwhelming the database
                var semaphore = new SemaphoreSlim(8, 8); // Increased concurrency limit
                var actionExecutionTasks = uncachedSourceIds.Select(async kvp =>
                {
                    var sourceId = kvp.Key;
                    var sourceType = kvp.Value;

                    await semaphore.WaitAsync();
                    try
                    {
                        List<JsonQueryCondition> jsonConditions = null;
                        if (onboardingId.HasValue)
                        {
                            jsonConditions = new List<JsonQueryCondition>
                            {
                                new JsonQueryCondition
                                {
                                    JsonPath = "OnboardingId",
                                    Operator = "=",
                                    Value = onboardingId.Value.ToString()
                                }
                            };
                        }

                        var actionExecutionsResult = await _actionExecutionService.GetExecutionsByTriggerSourceIdAsync(
                            sourceId, 1, 1000, jsonConditions);

                        if (actionExecutionsResult?.Data?.Any() == true)
                        {
                            var actionExecutionDtos = actionExecutionsResult.Data
                                .Select(ae => MapActionExecutionToChangeLogDto(ae, sourceType))
                                .ToList();

                            if (_logger.IsEnabled(LogLevel.Debug))
                            {
                                _logger.LogDebug("Found {Count} action executions for {SourceType} sourceId {SourceId}",
                                    actionExecutionDtos.Count, sourceType, sourceId);
                            }

                            return new { SourceId = sourceId, Results = actionExecutionDtos, IsEmpty = false };
                        }
                        else
                        {
                            return new { SourceId = sourceId, Results = new List<OperationChangeLogOutputDto>(), IsEmpty = true };
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch action executions for {SourceType} sourceId {SourceId}",
                            sourceType, sourceId);
                        return new { SourceId = sourceId, Results = new List<OperationChangeLogOutputDto>(), IsEmpty = true };
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                var allActionExecutions = await Task.WhenAll(actionExecutionTasks);

                // Process results and collect empty source IDs
                var totalAdded = 0;
                foreach (var result in allActionExecutions)
                {
                    if (result.IsEmpty)
                    {
                        emptySourceIds.Add(result.SourceId);
                    }
                    else if (result.Results.Any())
                    {
                        operationLogDtos.AddRange(result.Results);
                        totalAdded += result.Results.Count;
                    }
                }

                // Bulk cache empty source IDs
                if (emptySourceIds.Any())
                {
                    foreach (var sourceId in emptySourceIds)
                    {
                        CacheSourceIdAsEmpty(sourceId, onboardingId);
                    }

                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Cached {EmptyCount} source IDs as empty for future optimization", emptySourceIds.Count);
                    }
                }

                _logger.LogInformation("Batch action execution collection completed: {TotalCount} executions from {ProcessedCount} source IDs ({CachedCount} cached, {EmptyCount} newly cached as empty)",
                    totalAdded, uncachedSourceIds.Count, skippedFromCache, emptySourceIds.Count);

                // Log optimization statistics
                LogBatchQueryOptimizationStats(sourceIdTypes.Count, skippedFromCache, uncachedSourceIds.Count, totalAdded);

                // Re-sort by operation time
                operationLogDtos.Sort((x, y) => y.OperationTime.CompareTo(x.OperationTime));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding action executions in batch");
            }
        }

        #endregion

        #region Workflow Operations (Independent of Onboarding)

        /// <summary>
        /// Log workflow create operation
        /// </summary>
        public async Task<bool> LogWorkflowCreateAsync(long workflowId, string workflowName, string workflowDescription = null, string extendedData = null)
        {
            try
            {
                string operationTitle = $"Workflow Created: {workflowName}";
                string operationDescription = $"Workflow '{workflowName}' has been created by {GetOperatorDisplayName()}";

                if (!string.IsNullOrEmpty(workflowDescription))
                {
                    operationDescription += $". Description: {workflowDescription}";
                }

                var defaultExtendedData = JsonSerializer.Serialize(new
                {
                    WorkflowId = workflowId,
                    WorkflowName = workflowName,
                    WorkflowDescription = workflowDescription,
                    CreatedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.WorkflowCreate,
                    BusinessModuleEnum.Workflow,
                    workflowId,
                    null, // No onboardingId for independent workflow operations
                    null, // No stageId for independent workflow operations
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData ?? defaultExtendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log workflow create operation for workflow {WorkflowId}", workflowId);
                return false;
            }
        }

        /// <summary>
        /// Log workflow update operation
        /// </summary>
        public async Task<bool> LogWorkflowUpdateAsync(long workflowId, string workflowName, string beforeData, string afterData, List<string> changedFields, string extendedData = null)
        {
            try
            {
                // Check if there's actually a meaningful change
                if (!HasMeaningfulValueChange(beforeData, afterData))
                {
                    _logger.LogDebug("Skipping operation log for workflow {WorkflowId} as there's no meaningful value change", workflowId);
                    return true;
                }

                string operationTitle = $"Workflow Updated: {workflowName}";
                string operationDescription = $"Workflow '{workflowName}' has been updated by {GetOperatorDisplayName()}";

                if (changedFields?.Any() == true)
                {
                    operationDescription += $". Changed fields: {string.Join(", ", changedFields)}";
                }

                var defaultExtendedData = JsonSerializer.Serialize(new
                {
                    WorkflowId = workflowId,
                    WorkflowName = workflowName,
                    ChangedFieldsCount = changedFields?.Count ?? 0,
                    UpdatedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.WorkflowUpdate,
                    BusinessModuleEnum.Workflow,
                    workflowId,
                    null, // No onboardingId for independent workflow operations
                    null, // No stageId for independent workflow operations
                    operationTitle,
                    operationDescription,
                    beforeData,
                    afterData,
                    changedFields,
                    extendedData ?? defaultExtendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log workflow update operation for workflow {WorkflowId}", workflowId);
                return false;
            }
        }

        /// <summary>
        /// Log workflow delete operation
        /// </summary>
        public async Task<bool> LogWorkflowDeleteAsync(long workflowId, string workflowName, string reason = null, string extendedData = null)
        {
            try
            {
                string operationTitle = $"Workflow Deleted: {workflowName}";
                string operationDescription = $"Workflow '{workflowName}' has been deleted by {GetOperatorDisplayName()}";

                if (!string.IsNullOrEmpty(reason))
                {
                    operationDescription += $" with reason: {reason}";
                }

                var defaultExtendedData = JsonSerializer.Serialize(new
                {
                    WorkflowId = workflowId,
                    WorkflowName = workflowName,
                    Reason = reason,
                    DeletedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.WorkflowDelete,
                    BusinessModuleEnum.Workflow,
                    workflowId,
                    null, // No onboardingId for independent workflow operations
                    null, // No stageId for independent workflow operations
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData ?? defaultExtendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log workflow delete operation for workflow {WorkflowId}", workflowId);
                return false;
            }
        }

        /// <summary>
        /// Log workflow publish operation
        /// </summary>
        public async Task<bool> LogWorkflowPublishAsync(long workflowId, string workflowName, string version = null, string extendedData = null)
        {
            try
            {
                string operationTitle = $"Workflow Published: {workflowName}";
                string operationDescription = $"Workflow '{workflowName}' has been published by {GetOperatorDisplayName()}";

                if (!string.IsNullOrEmpty(version))
                {
                    operationDescription += $" as version {version}";
                }

                var defaultExtendedData = JsonSerializer.Serialize(new
                {
                    WorkflowId = workflowId,
                    WorkflowName = workflowName,
                    Version = version,
                    PublishedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.WorkflowPublish,
                    BusinessModuleEnum.Workflow,
                    workflowId,
                    null, // No onboardingId for independent workflow operations
                    null, // No stageId for independent workflow operations
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData ?? defaultExtendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log workflow publish operation for workflow {WorkflowId}", workflowId);
                return false;
            }
        }

        /// <summary>
        /// Log workflow unpublish operation
        /// </summary>
        public async Task<bool> LogWorkflowUnpublishAsync(long workflowId, string workflowName, string reason = null, string extendedData = null)
        {
            try
            {
                string operationTitle = $"Workflow Unpublished: {workflowName}";
                string operationDescription = $"Workflow '{workflowName}' has been unpublished by {GetOperatorDisplayName()}";

                if (!string.IsNullOrEmpty(reason))
                {
                    operationDescription += $" with reason: {reason}";
                }

                var defaultExtendedData = JsonSerializer.Serialize(new
                {
                    WorkflowId = workflowId,
                    WorkflowName = workflowName,
                    Reason = reason,
                    UnpublishedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.WorkflowUnpublish,
                    BusinessModuleEnum.Workflow,
                    workflowId,
                    null, // No onboardingId for independent workflow operations
                    null, // No stageId for independent workflow operations
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData ?? defaultExtendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log workflow unpublish operation for workflow {WorkflowId}", workflowId);
                return false;
            }
        }

        /// <summary>
        /// Log workflow activate operation
        /// </summary>
        public async Task<bool> LogWorkflowActivateAsync(long workflowId, string workflowName, string extendedData = null)
        {
            try
            {
                string operationTitle = $"Workflow Activated: {workflowName}";
                string operationDescription = $"Workflow '{workflowName}' has been activated by {GetOperatorDisplayName()}";

                var defaultExtendedData = JsonSerializer.Serialize(new
                {
                    WorkflowId = workflowId,
                    WorkflowName = workflowName,
                    ActivatedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.WorkflowActivate,
                    BusinessModuleEnum.Workflow,
                    workflowId,
                    null, // No onboardingId for independent workflow operations
                    null, // No stageId for independent workflow operations
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData ?? defaultExtendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log workflow activate operation for workflow {WorkflowId}", workflowId);
                return false;
            }
        }

        /// <summary>
        /// Log workflow deactivate operation
        /// </summary>
        public async Task<bool> LogWorkflowDeactivateAsync(long workflowId, string workflowName, string reason = null, string extendedData = null)
        {
            try
            {
                string operationTitle = $"Workflow Deactivated: {workflowName}";
                string operationDescription = $"Workflow '{workflowName}' has been deactivated by {GetOperatorDisplayName()}";

                if (!string.IsNullOrEmpty(reason))
                {
                    operationDescription += $" with reason: {reason}";
                }

                var defaultExtendedData = JsonSerializer.Serialize(new
                {
                    WorkflowId = workflowId,
                    WorkflowName = workflowName,
                    Reason = reason,
                    DeactivatedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.WorkflowDeactivate,
                    BusinessModuleEnum.Workflow,
                    workflowId,
                    null, // No onboardingId for independent workflow operations
                    null, // No stageId for independent workflow operations
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData ?? defaultExtendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log workflow deactivate operation for workflow {WorkflowId}", workflowId);
                return false;
            }
        }

        #endregion

        #region Stage Operations (Independent of Onboarding)

        /// <summary>
        /// Log stage create operation
        /// </summary>
        public async Task<bool> LogStageCreateAsync(long stageId, string stageName, long? workflowId = null, string extendedData = null)
        {
            try
            {
                string operationTitle = $"Stage Created: {stageName}";
                string operationDescription = $"Stage '{stageName}' has been created by {GetOperatorDisplayName()}";

                if (workflowId.HasValue)
                {
                    operationDescription += $" in workflow ID {workflowId.Value}";
                }

                var defaultExtendedData = JsonSerializer.Serialize(new
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
                    null, // No onboardingId for independent stage operations
                    null, // No parent stageId for stage creation
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData ?? defaultExtendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log stage create operation for stage {StageId}", stageId);
                return false;
            }
        }

        /// <summary>
        /// Log stage update operation
        /// </summary>
        public async Task<bool> LogStageUpdateAsync(long stageId, string stageName, string beforeData, string afterData, List<string> changedFields, long? workflowId = null, string extendedData = null)
        {
            try
            {
                // Check if there's actually a meaningful change
                if (!HasMeaningfulValueChange(beforeData, afterData))
                {
                    _logger.LogDebug("Skipping operation log for stage {StageId} as there's no meaningful value change", stageId);
                    return true;
                }

                string operationTitle = $"Stage Updated: {stageName}";
                string operationDescription = $"Stage '{stageName}' has been updated by {GetOperatorDisplayName()}";

                if (workflowId.HasValue)
                {
                    operationDescription += $" in workflow ID {workflowId.Value}";
                }

                if (changedFields?.Any() == true)
                {
                    operationDescription += $". Changed fields: {string.Join(", ", changedFields)}";
                }

                var defaultExtendedData = JsonSerializer.Serialize(new
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
                    null, // No onboardingId for independent stage operations
                    null, // No parent stageId for stage update
                    operationTitle,
                    operationDescription,
                    beforeData,
                    afterData,
                    changedFields,
                    extendedData ?? defaultExtendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log stage update operation for stage {StageId}", stageId);
                return false;
            }
        }

        /// <summary>
        /// Log stage delete operation
        /// </summary>
        public async Task<bool> LogStageDeleteAsync(long stageId, string stageName, long? workflowId = null, string reason = null, string extendedData = null)
        {
            try
            {
                string operationTitle = $"Stage Deleted: {stageName}";
                string operationDescription = $"Stage '{stageName}' has been deleted by {GetOperatorDisplayName()}";

                if (workflowId.HasValue)
                {
                    operationDescription += $" from workflow ID {workflowId.Value}";
                }

                if (!string.IsNullOrEmpty(reason))
                {
                    operationDescription += $" with reason: {reason}";
                }

                var defaultExtendedData = JsonSerializer.Serialize(new
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
                    null, // No onboardingId for independent stage operations
                    null, // No parent stageId for stage deletion
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData ?? defaultExtendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log stage delete operation for stage {StageId}", stageId);
                return false;
            }
        }

        /// <summary>
        /// Log stage order change operation
        /// </summary>
        public async Task<bool> LogStageOrderChangeAsync(long stageId, string stageName, int oldOrder, int newOrder, long? workflowId = null, string extendedData = null)
        {
            try
            {
                string operationTitle = $"Stage Order Changed: {stageName}";
                string operationDescription = $"Stage '{stageName}' order has been changed from {oldOrder} to {newOrder} by {GetOperatorDisplayName()}";

                if (workflowId.HasValue)
                {
                    operationDescription += $" in workflow ID {workflowId.Value}";
                }

                var beforeData = JsonSerializer.Serialize(new { order = oldOrder });
                var afterData = JsonSerializer.Serialize(new { order = newOrder });

                var defaultExtendedData = JsonSerializer.Serialize(new
                {
                    StageId = stageId,
                    StageName = stageName,
                    WorkflowId = workflowId,
                    OldOrder = oldOrder,
                    NewOrder = newOrder,
                    ChangedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.StageOrderChange,
                    BusinessModuleEnum.Stage,
                    stageId,
                    null, // No onboardingId for independent stage operations
                    null, // No parent stageId for stage order change
                    operationTitle,
                    operationDescription,
                    beforeData,
                    afterData,
                    new List<string> { "order" },
                    extendedData ?? defaultExtendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log stage order change operation for stage {StageId}", stageId);
                return false;
            }
        }

        #endregion

        #region Checklist Operations (Independent of Onboarding)

        /// <summary>
        /// Log checklist create operation
        /// </summary>
        public async Task<bool> LogChecklistCreateAsync(long checklistId, string checklistName, string extendedData = null)
        {
            try
            {
                string operationTitle = $"Checklist Created: {checklistName}";
                string operationDescription = $"Checklist '{checklistName}' has been created by {GetOperatorDisplayName()}";

                var defaultExtendedData = JsonSerializer.Serialize(new
                {
                    ChecklistId = checklistId,
                    ChecklistName = checklistName,
                    CreatedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.ChecklistCreate,
                    BusinessModuleEnum.Checklist,
                    checklistId,
                    null, // No onboardingId for independent checklist operations
                    null, // No stageId for independent checklist operations
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData ?? defaultExtendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log checklist create operation for checklist {ChecklistId}", checklistId);
                return false;
            }
        }

        /// <summary>
        /// Log checklist update operation
        /// </summary>
        public async Task<bool> LogChecklistUpdateAsync(long checklistId, string checklistName, string beforeData, string afterData, List<string> changedFields, string extendedData = null)
        {
            try
            {
                // Check if there's actually a meaningful change
                if (!HasMeaningfulValueChange(beforeData, afterData))
                {
                    _logger.LogDebug("Skipping operation log for checklist {ChecklistId} as there's no meaningful value change", checklistId);
                    return true;
                }

                string operationTitle = $"Checklist Updated: {checklistName}";
                string operationDescription = $"Checklist '{checklistName}' has been updated by {GetOperatorDisplayName()}";

                if (changedFields?.Any() == true)
                {
                    operationDescription += $". Changed fields: {string.Join(", ", changedFields)}";
                }

                var defaultExtendedData = JsonSerializer.Serialize(new
                {
                    ChecklistId = checklistId,
                    ChecklistName = checklistName,
                    ChangedFieldsCount = changedFields?.Count ?? 0,
                    UpdatedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.ChecklistUpdate,
                    BusinessModuleEnum.Checklist,
                    checklistId,
                    null, // No onboardingId for independent checklist operations
                    null, // No stageId for independent checklist operations
                    operationTitle,
                    operationDescription,
                    beforeData,
                    afterData,
                    changedFields,
                    extendedData ?? defaultExtendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log checklist update operation for checklist {ChecklistId}", checklistId);
                return false;
            }
        }

        /// <summary>
        /// Log checklist delete operation
        /// </summary>
        public async Task<bool> LogChecklistDeleteAsync(long checklistId, string checklistName, string reason = null, string extendedData = null)
        {
            try
            {
                string operationTitle = $"Checklist Deleted: {checklistName}";
                string operationDescription = $"Checklist '{checklistName}' has been deleted by {GetOperatorDisplayName()}";

                if (!string.IsNullOrEmpty(reason))
                {
                    operationDescription += $" with reason: {reason}";
                }

                var defaultExtendedData = JsonSerializer.Serialize(new
                {
                    ChecklistId = checklistId,
                    ChecklistName = checklistName,
                    Reason = reason,
                    DeletedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.ChecklistDelete,
                    BusinessModuleEnum.Checklist,
                    checklistId,
                    null, // No onboardingId for independent checklist operations
                    null, // No stageId for independent checklist operations
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData ?? defaultExtendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log checklist delete operation for checklist {ChecklistId}", checklistId);
                return false;
            }
        }

        /// <summary>
        /// Log checklist task create operation
        /// </summary>
        public async Task<bool> LogChecklistTaskCreateAsync(long taskId, string taskName, long checklistId, string extendedData = null)
        {
            try
            {
                string operationTitle = $"Checklist Task Created: {taskName}";
                string operationDescription = $"Checklist task '{taskName}' has been created by {GetOperatorDisplayName()} in checklist ID {checklistId}";

                var defaultExtendedData = JsonSerializer.Serialize(new
                {
                    TaskId = taskId,
                    TaskName = taskName,
                    ChecklistId = checklistId,
                    CreatedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.ChecklistTaskCreate,
                    BusinessModuleEnum.Task,
                    taskId,
                    null, // No onboardingId for independent checklist task operations
                    null, // No stageId for independent checklist task operations
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData ?? defaultExtendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log checklist task create operation for task {TaskId}", taskId);
                return false;
            }
        }

        /// <summary>
        /// Log checklist task update operation
        /// </summary>
        public async Task<bool> LogChecklistTaskUpdateAsync(long taskId, string taskName, string beforeData, string afterData, List<string> changedFields, long checklistId, string extendedData = null)
        {
            try
            {
                // Check if there's actually a meaningful change
                if (!HasMeaningfulValueChange(beforeData, afterData))
                {
                    _logger.LogDebug("Skipping operation log for checklist task {TaskId} as there's no meaningful value change", taskId);
                    return true;
                }

                string operationTitle = $"Checklist Task Updated: {taskName}";
                string operationDescription = $"Checklist task '{taskName}' has been updated by {GetOperatorDisplayName()} in checklist ID {checklistId}";

                if (changedFields?.Any() == true)
                {
                    operationDescription += $". Changed fields: {string.Join(", ", changedFields)}";
                }

                var defaultExtendedData = JsonSerializer.Serialize(new
                {
                    TaskId = taskId,
                    TaskName = taskName,
                    ChecklistId = checklistId,
                    ChangedFieldsCount = changedFields?.Count ?? 0,
                    UpdatedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.ChecklistTaskUpdate,
                    BusinessModuleEnum.Task,
                    taskId,
                    null, // No onboardingId for independent checklist task operations
                    null, // No stageId for independent checklist task operations
                    operationTitle,
                    operationDescription,
                    beforeData,
                    afterData,
                    changedFields,
                    extendedData ?? defaultExtendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log checklist task update operation for task {TaskId}", taskId);
                return false;
            }
        }

        /// <summary>
        /// Log checklist task delete operation
        /// </summary>
        public async Task<bool> LogChecklistTaskDeleteAsync(long taskId, string taskName, long checklistId, string reason = null, string extendedData = null)
        {
            try
            {
                string operationTitle = $"Checklist Task Deleted: {taskName}";
                string operationDescription = $"Checklist task '{taskName}' has been deleted by {GetOperatorDisplayName()} from checklist ID {checklistId}";

                if (!string.IsNullOrEmpty(reason))
                {
                    operationDescription += $" with reason: {reason}";
                }

                var defaultExtendedData = JsonSerializer.Serialize(new
                {
                    TaskId = taskId,
                    TaskName = taskName,
                    ChecklistId = checklistId,
                    Reason = reason,
                    DeletedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.ChecklistTaskDelete,
                    BusinessModuleEnum.Task,
                    taskId,
                    null, // No onboardingId for independent checklist task operations
                    null, // No stageId for independent checklist task operations
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData ?? defaultExtendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log checklist task delete operation for task {TaskId}", taskId);
                return false;
            }
        }

        #endregion

        #region Questionnaire Operations (Independent of Onboarding)

        /// <summary>
        /// Log questionnaire create operation
        /// </summary>
        public async Task<bool> LogQuestionnaireCreateAsync(long questionnaireId, string questionnaireName, string extendedData = null)
        {
            try
            {
                string operationTitle = $"Questionnaire Created: {questionnaireName}";
                string operationDescription = $"Questionnaire '{questionnaireName}' has been created by {GetOperatorDisplayName()}";

                var defaultExtendedData = JsonSerializer.Serialize(new
                {
                    QuestionnaireId = questionnaireId,
                    QuestionnaireName = questionnaireName,
                    CreatedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.QuestionnaireCreate,
                    BusinessModuleEnum.Questionnaire,
                    questionnaireId,
                    null, // No onboardingId for independent questionnaire operations
                    null, // No stageId for independent questionnaire operations
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData ?? defaultExtendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log questionnaire create operation for questionnaire {QuestionnaireId}", questionnaireId);
                return false;
            }
        }

        /// <summary>
        /// Log questionnaire update operation
        /// </summary>
        public async Task<bool> LogQuestionnaireUpdateAsync(long questionnaireId, string questionnaireName, string beforeData, string afterData, List<string> changedFields, string extendedData = null)
        {
            try
            {
                // Check if there's actually a meaningful change
                if (!HasMeaningfulValueChange(beforeData, afterData))
                {
                    _logger.LogDebug("Skipping operation log for questionnaire {QuestionnaireId} as there's no meaningful value change", questionnaireId);
                    return true;
                }

                string operationTitle = $"Questionnaire Updated: {questionnaireName}";
                string operationDescription = $"Questionnaire '{questionnaireName}' has been updated by {GetOperatorDisplayName()}";

                if (changedFields?.Any() == true)
                {
                    operationDescription += $". Changed fields: {string.Join(", ", changedFields)}";
                }

                var defaultExtendedData = JsonSerializer.Serialize(new
                {
                    QuestionnaireId = questionnaireId,
                    QuestionnaireName = questionnaireName,
                    ChangedFieldsCount = changedFields?.Count ?? 0,
                    UpdatedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.QuestionnaireUpdate,
                    BusinessModuleEnum.Questionnaire,
                    questionnaireId,
                    null, // No onboardingId for independent questionnaire operations
                    null, // No stageId for independent questionnaire operations
                    operationTitle,
                    operationDescription,
                    beforeData,
                    afterData,
                    changedFields,
                    extendedData ?? defaultExtendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log questionnaire update operation for questionnaire {QuestionnaireId}", questionnaireId);
                return false;
            }
        }

        /// <summary>
        /// Log questionnaire delete operation
        /// </summary>
        public async Task<bool> LogQuestionnaireDeleteAsync(long questionnaireId, string questionnaireName, string reason = null, string extendedData = null)
        {
            try
            {
                string operationTitle = $"Questionnaire Deleted: {questionnaireName}";
                string operationDescription = $"Questionnaire '{questionnaireName}' has been deleted by {GetOperatorDisplayName()}";

                if (!string.IsNullOrEmpty(reason))
                {
                    operationDescription += $" with reason: {reason}";
                }

                var defaultExtendedData = JsonSerializer.Serialize(new
                {
                    QuestionnaireId = questionnaireId,
                    QuestionnaireName = questionnaireName,
                    Reason = reason,
                    DeletedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.QuestionnaireDelete,
                    BusinessModuleEnum.Questionnaire,
                    questionnaireId,
                    null, // No onboardingId for independent questionnaire operations
                    null, // No stageId for independent questionnaire operations
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData ?? defaultExtendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log questionnaire delete operation for questionnaire {QuestionnaireId}", questionnaireId);
                return false;
            }
        }

        /// <summary>
        /// Log questionnaire publish operation
        /// </summary>
        public async Task<bool> LogQuestionnairePublishAsync(long questionnaireId, string questionnaireName, string version = null, string extendedData = null)
        {
            try
            {
                string operationTitle = $"Questionnaire Published: {questionnaireName}";
                string operationDescription = $"Questionnaire '{questionnaireName}' has been published by {GetOperatorDisplayName()}";

                if (!string.IsNullOrEmpty(version))
                {
                    operationDescription += $" as version {version}";
                }

                var defaultExtendedData = JsonSerializer.Serialize(new
                {
                    QuestionnaireId = questionnaireId,
                    QuestionnaireName = questionnaireName,
                    Version = version,
                    PublishedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.QuestionnairePublish,
                    BusinessModuleEnum.Questionnaire,
                    questionnaireId,
                    null, // No onboardingId for independent questionnaire operations
                    null, // No stageId for independent questionnaire operations
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData ?? defaultExtendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log questionnaire publish operation for questionnaire {QuestionnaireId}", questionnaireId);
                return false;
            }
        }

        /// <summary>
        /// Log questionnaire unpublish operation
        /// </summary>
        public async Task<bool> LogQuestionnaireUnpublishAsync(long questionnaireId, string questionnaireName, string reason = null, string extendedData = null)
        {
            try
            {
                string operationTitle = $"Questionnaire Unpublished: {questionnaireName}";
                string operationDescription = $"Questionnaire '{questionnaireName}' has been unpublished by {GetOperatorDisplayName()}";

                if (!string.IsNullOrEmpty(reason))
                {
                    operationDescription += $" with reason: {reason}";
                }

                var defaultExtendedData = JsonSerializer.Serialize(new
                {
                    QuestionnaireId = questionnaireId,
                    QuestionnaireName = questionnaireName,
                    Reason = reason,
                    UnpublishedAt = DateTimeOffset.UtcNow
                });

                return await LogOperationAsync(
                    OperationTypeEnum.QuestionnaireUnpublish,
                    BusinessModuleEnum.Questionnaire,
                    questionnaireId,
                    null, // No onboardingId for independent questionnaire operations
                    null, // No stageId for independent questionnaire operations
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData ?? defaultExtendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log questionnaire unpublish operation for questionnaire {QuestionnaireId}", questionnaireId);
                return false;
            }
        }

        #endregion
    }
}