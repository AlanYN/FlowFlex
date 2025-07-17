using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Operation change log service implementation
    /// </summary>
    public class OperationChangeLogService : IOperationChangeLogService, IScopedService
    {
        private readonly IOperationChangeLogRepository _operationChangeLogRepository;
        private readonly ILogger<OperationChangeLogService> _logger;
        private readonly UserContext _userContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMapper _mapper;

        public OperationChangeLogService(
            IOperationChangeLogRepository operationChangeLogRepository,
            ILogger<OperationChangeLogService> logger,
            UserContext userContext,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper)
        {
            _operationChangeLogRepository = operationChangeLogRepository;
            _logger = logger;
            _userContext = userContext;
            _httpContextAccessor = httpContextAccessor;
            _mapper = mapper;
        }

        /// <summary>
        /// Log checklist task completion operation
        /// </summary>
        public async Task<bool> LogChecklistTaskCompleteAsync(long taskId, string taskName, long onboardingId, long? stageId, string completionNotes = null, int actualHours = 0)
        {
            try
            {
                string operationTitle = $"Checklist Task Completed: {taskName}";
                string operationDescription = $"Task '{taskName}' has been marked as completed by {_userContext.UserName}";

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
                    CompletedAt = DateTimeOffset.Now
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
                string operationDescription = $"Task '{taskName}' has been marked as uncompleted by {_userContext.UserName}";

                if (!string.IsNullOrEmpty(reason))
                {
                    operationDescription += $" with reason: {reason}";
                }

                var extendedData = JsonSerializer.Serialize(new
                {
                    TaskId = taskId,
                    TaskName = taskName,
                    Reason = reason,
                    UncompletedAt = DateTimeOffset.Now
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
                string operationDescription = $"Questionnaire answer has been {operationType.ToLower()} by {_userContext.UserName}";

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
                    SubmittedAt = DateTimeOffset.Now,
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
                    ChangedAt = DateTimeOffset.Now
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
                string operationDescription = $"File '{fileName}' has been uploaded successfully by {_userContext.UserName}";

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
                    UploadedAt = DateTimeOffset.Now
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
                string operationDescription = $"File '{fileName}' has been deleted by {_userContext.UserName}";

                if (!string.IsNullOrEmpty(reason))
                {
                    operationDescription += $" with reason: {reason}";
                }

                var extendedData = JsonSerializer.Serialize(new
                {
                    FileId = fileId,
                    FileName = fileName,
                    Reason = reason,
                    DeletedAt = DateTimeOffset.Now
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
                string operationDescription = $"File '{fileName}' has been updated by {_userContext.UserName}";

                if (changedFields?.Any() == true)
                {
                    operationDescription += $". Changed fields: {string.Join(", ", changedFields)}";
                }

                var extendedData = JsonSerializer.Serialize(new
                {
                    FileId = fileId,
                    FileName = fileName,
                    ChangedFieldsCount = changedFields?.Count ?? 0,
                    UpdatedAt = DateTimeOffset.Now
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
                    OperatorId = long.TryParse(_userContext.UserId, out long operatorId) ? operatorId : 0,
                    OperatorName = !string.IsNullOrEmpty(_userContext.Email) ? _userContext.Email : (_userContext.UserName ?? "System"),
                    OperationTime = DateTimeOffset.Now,
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
        public async Task<PagedResult<OperationChangeLogOutputDto>> GetOperationLogsAsync(long? onboardingId = null, long? stageId = null, OperationTypeEnum? operationType = null, int pageIndex = 1, int pageSize = 20)
        {
            try
            {
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

                // 分页
                int totalCount = logs.Count;
                var pagedLogs = logs
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // 转换为输出DTO
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
            var now = DateTimeOffset.Now;
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

        #endregion
    }
}