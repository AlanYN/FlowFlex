using AutoMapper;
using Microsoft.AspNetCore.Http;
using FlowFlex.Application.Contracts.Dtos.OW.Checklist;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Application.Services.OW.Extensions;

namespace FlowFlex.Application.Services.OW;

/// <summary>
/// ChecklistTaskCompletion service implementation
/// </summary>
public class ChecklistTaskCompletionService : IChecklistTaskCompletionService, IScopedService
{
    private readonly IChecklistTaskCompletionRepository _completionRepository;
    private readonly IChecklistTaskRepository _taskRepository;
    private readonly IOnboardingRepository _onboardingRepository;
    private readonly IStageRepository _stageRepository;
    private readonly IStageCompletionLogRepository _stageCompletionLogRepository;
    private readonly IOperationChangeLogService _operationChangeLogService;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserContext _userContext;

    public ChecklistTaskCompletionService(
    IChecklistTaskCompletionRepository completionRepository,
    IChecklistTaskRepository taskRepository,
    IOnboardingRepository onboardingRepository,
    IStageRepository stageRepository,
    IStageCompletionLogRepository stageCompletionLogRepository,
    IOperationChangeLogService operationChangeLogService,
    IMapper mapper,
    IHttpContextAccessor httpContextAccessor,
    UserContext userContext)
    {
        _completionRepository = completionRepository;
        _taskRepository = taskRepository;
        _onboardingRepository = onboardingRepository;
        _stageRepository = stageRepository;
        _stageCompletionLogRepository = stageCompletionLogRepository;
        _operationChangeLogService = operationChangeLogService;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        _userContext = userContext;
    }

    /// <summary>
    /// Get current user name from UserContext
    /// </summary>
    private string GetCurrentUserName()
    {
        return !string.IsNullOrEmpty(_userContext?.UserName) ? _userContext.UserName : "System";
    }

    /// <summary>
    /// Get all task completions
    /// </summary>
    public async Task<List<ChecklistTaskCompletionOutputDto>> GetAllTaskCompletionsAsync()
    {
        var completions = await _completionRepository.GetListAsync();
        return _mapper.Map<List<ChecklistTaskCompletionOutputDto>>(completions);
    }

    /// <summary>
    /// Save task completion
    /// </summary>
    public async Task<bool> SaveTaskCompletionAsync(ChecklistTaskCompletionInputDto input)
    {
        // Validate task exists
        var task = await _taskRepository.GetFirstAsync(x => x.Id == input.TaskId);
        if (task == null)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, $"Task with ID {input.TaskId} not found");
        }

        // Validate onboarding exists
        var onboarding = await _onboardingRepository.GetByIdAsync(input.OnboardingId);
        if (onboarding == null)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
        }

        var completion = _mapper.Map<ChecklistTaskCompletion>(input);

        // Ensure LeadId is set from onboarding if not provided in input
        if (string.IsNullOrEmpty(completion.LeadId))
        {
            completion.LeadId = onboarding.LeadId;
        }

        // Set StageId from onboarding current stage if not provided in input
        if (!completion.StageId.HasValue && onboarding.CurrentStageId.HasValue)
        {
            completion.StageId = onboarding.CurrentStageId.Value;
        }

        // Set completion time if completed
        if (completion.IsCompleted)
        {
            completion.CompletedTime = DateTimeOffset.Now;
        }
        else
        {
            completion.CompletedTime = null;
        }

        // Initialize create information with proper ID and timestamps
        completion.InitCreateInfo(_userContext);

        var result = await _completionRepository.SaveTaskCompletionAsync(completion);

        // Log task completion
        if (result)
        {
            await LogTaskCompletionAsync(onboarding, task, completion.IsCompleted, completion.CompletionNotes);
        }

        return result;
    }

    /// <summary>
    /// Batch save task completions
    /// </summary>
    public async Task<bool> BatchSaveTaskCompletionsAsync(List<ChecklistTaskCompletionInputDto> inputs)
    {
        if (inputs == null || !inputs.Any()) return true;

        var completions = new List<ChecklistTaskCompletion>();

        foreach (var input in inputs)
        {
            // Validate task exists
            var task = await _taskRepository.GetFirstAsync(x => x.Id == input.TaskId);
            if (task == null)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, $"Task {input.TaskId} not found");
            }

            // Validate onboarding exists
            var onboarding = await _onboardingRepository.GetByIdAsync(input.OnboardingId);
            if (onboarding == null)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, $"Onboarding {input.OnboardingId} not found");
            }

            var completion = _mapper.Map<ChecklistTaskCompletion>(input);

            // Ensure LeadId is set from onboarding if not provided in input
            if (string.IsNullOrEmpty(completion.LeadId))
            {
                completion.LeadId = onboarding.LeadId;
            }

            // Set StageId from onboarding current stage if not provided in input
            if (!completion.StageId.HasValue && onboarding.CurrentStageId.HasValue)
            {
                completion.StageId = onboarding.CurrentStageId.Value;
            }

            // Set completion time if completed
            if (completion.IsCompleted)
            {
                completion.CompletedTime = DateTimeOffset.Now;
            }
            else
            {
                completion.CompletedTime = null;
            }

            // Initialize create information with proper ID and timestamps
            completion.InitCreateInfo(_userContext);

            completions.Add(completion);
        }

        var result = await _completionRepository.BatchSaveTaskCompletionsAsync(completions);

        // Log batch task completions
        if (result)
        {
            await LogBatchTaskCompletionsAsync(inputs, completions);
        }

        return result;
    }

    /// <summary>
    /// Get task completions by lead and checklist
    /// </summary>
    public async Task<List<ChecklistTaskCompletionOutputDto>> GetByLeadAndChecklistAsync(string leadId, long checklistId)
    {
        var completions = await _completionRepository.GetByLeadAndChecklistAsync(leadId, checklistId);
        return _mapper.Map<List<ChecklistTaskCompletionOutputDto>>(completions);
    }

    /// <summary>
    /// Get task completions by onboarding and checklist
    /// </summary>
    public async Task<List<ChecklistTaskCompletionOutputDto>> GetByOnboardingAndChecklistAsync(long onboardingId, long checklistId)
    {
        var completions = await _completionRepository.GetByOnboardingAndChecklistAsync(onboardingId, checklistId);
        return _mapper.Map<List<ChecklistTaskCompletionOutputDto>>(completions);
    }

    /// <summary>
    /// Get task completions by onboarding and stage
    /// </summary>
    public async Task<List<ChecklistTaskCompletionOutputDto>> GetByOnboardingAndStageAsync(long onboardingId, long stageId)
    {
        var completions = await _completionRepository.GetByOnboardingAndStageAsync(onboardingId, stageId);
        return _mapper.Map<List<ChecklistTaskCompletionOutputDto>>(completions);
    }

    /// <summary>
    /// Get completion statistics
    /// </summary>
    public async Task<(int totalTasks, int completedTasks, decimal completionRate)> GetCompletionStatsAsync(long onboardingId, long checklistId)
    {
        var (totalTasks, completedTasks) = await _completionRepository.GetCompletionStatsAsync(onboardingId, checklistId);

        var completionRate = totalTasks > 0 ? Math.Round((decimal)completedTasks / totalTasks * 100, 2) : 0;

        return (totalTasks, completedTasks, completionRate);
    }

    /// <summary>
    /// Toggle task completion
    /// </summary>
    public async Task<bool> ToggleTaskCompletionAsync(long onboardingId, long taskId, bool isCompleted, string completionNotes = "")
    {
        // Simplified validation: only check if onboarding exists
        var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
        if (onboarding == null)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
        }

        // Simplified validation: only check if task exists
        var task = await _taskRepository.GetFirstAsync(x => x.Id == taskId);
        if (task == null)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "Task not found");
        }

        var input = new ChecklistTaskCompletionInputDto
        {
            OnboardingId = onboardingId,
            LeadId = onboarding.LeadId,
            ChecklistId = task.ChecklistId,
            TaskId = taskId,
            IsCompleted = isCompleted,
            CompletionNotes = completionNotes
        };

        return await SaveTaskCompletionAsync(input);
    }

    /// <summary>
    /// Set request information
    /// </summary>
    private void SetRequestInfo(ChecklistTaskCompletion completion)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            completion.IpAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            completion.UserAgent = httpContext.Request.Headers["User-Agent"].ToString();
        }

        completion.CreateBy = GetCurrentUserName();
        completion.ModifyBy = GetCurrentUserName();
    }

    /// <summary>
    /// Log task completion to Change Log
    /// </summary>
    private async Task LogTaskCompletionAsync(Onboarding onboarding, ChecklistTask task, bool isCompleted, string completionNotes)
    {
        try
        {
            var tenantId = GetTenantId();
            // Debug logging handled by structured logging
            // Get current stage information
            Stage currentStage = null;
            if (onboarding.CurrentStageId.HasValue)
            {
                currentStage = await _stageRepository.GetByIdAsync(onboarding.CurrentStageId.Value);
                // Debug logging handled by structured logging
            }
            else
            {
                // Debug logging handled by structured logging
            }

            var logData = new
            {
                OnboardingId = onboarding.Id,
                LeadId = onboarding.LeadId,
                LeadName = onboarding.LeadName,
                StageId = currentStage?.Id ?? 0,
                StageName = currentStage?.Name ?? "Unknown Stage",
                ChecklistId = task.ChecklistId,
                TaskId = task.Id,
                TaskName = task.Name,
                TaskDescription = task.Description,
                IsRequired = task.IsRequired,
                IsCompleted = isCompleted,
                CompletionNotes = completionNotes,
                CompletedTime = DateTimeOffset.UtcNow,
                CompletedBy = GetCurrentUserName(),
                Action = isCompleted ? "Task Completed" : "Task Marked Incomplete",
                WorkflowId = onboarding.WorkflowId,
                Priority = onboarding.Priority
            };

            var serializedLogData = System.Text.Json.JsonSerializer.Serialize(logData);
            // Debug logging handled by structured logging
            // 1. Log to ff_stage_completion_log table (maintain original functionality)
            var stageCompletionLog = new StageCompletionLog
            {
                TenantId = tenantId,
                OnboardingId = onboarding.Id,
                StageId = currentStage?.Id ?? 0,
                StageName = currentStage?.Name ?? "Unknown Stage",
                LogType = "task_completion",
                Action = isCompleted ? "Task Completed" : "Task Marked Incomplete",
                LogData = serializedLogData,
                Success = true,
                NetworkStatus = "online",
                CreateBy = GetCurrentUserName(),
                ModifyBy = GetCurrentUserName()
            };
            // Debug logging handled by structured logging
            await _stageCompletionLogRepository.InsertAsync(stageCompletionLog);
            // Debug logging handled by structured logging}");

            // 2. Also log to ff_operation_change_log table (new functionality)
            try
            {
                var operationType = isCompleted ? OperationTypeEnum.ChecklistTaskComplete : OperationTypeEnum.ChecklistTaskUncomplete;
                var operationDescription = $"Checklist task '{task.Name}' has been {(isCompleted ? "completed" : "marked as incomplete")} by {GetCurrentUserName()}";

                // Prepare before_data and after_data
                var beforeData = new
                {
                    TaskId = task.Id,
                    TaskName = task.Name,
                    IsCompleted = !isCompleted, // Opposite state
                    CompletionNotes = "",
                    CompletedTime = (DateTimeOffset?)null
                };

                var afterData = new
                {
                    TaskId = task.Id,
                    TaskName = task.Name,
                    IsCompleted = isCompleted,
                    CompletionNotes = completionNotes ?? "",
                    CompletedTime = isCompleted ? DateTimeOffset.UtcNow : (DateTimeOffset?)null
                };

                var changedFields = new List<string> { "IsCompleted" };
                if (!string.IsNullOrEmpty(completionNotes))
                {
                    changedFields.Add("CompletionNotes");
                }
                if (isCompleted)
                {
                    changedFields.Add("CompletedTime");
                }

                var extendedData = new
                {
                    ChecklistId = task.ChecklistId,
                    TaskType = task.TaskType,
                    Priority = task.Priority,
                    IsRequired = task.IsRequired,
                    WorkflowId = onboarding.WorkflowId,
                    CompletedAt = DateTimeOffset.UtcNow
                };

                await _operationChangeLogService.LogOperationAsync(
                    operationType: operationType,
                    businessModule: BusinessModuleEnum.ChecklistTask,
                    businessId: task.Id,
                    onboardingId: onboarding.Id,
                    stageId: currentStage?.Id ?? 0,
                    operationTitle: $"Task {(isCompleted ? "Completed" : "Incomplete")}: {task.Name}",
                    operationDescription: operationDescription,
                    beforeData: System.Text.Json.JsonSerializer.Serialize(beforeData),
                    afterData: System.Text.Json.JsonSerializer.Serialize(afterData),
                    changedFields: changedFields,
                    extendedData: System.Text.Json.JsonSerializer.Serialize(extendedData)
                );

                // Debug logging handled by structured logging}");
            }
            catch (Exception operationLogEx)
            {
                // Debug logging handled by structured logging
                // Don't affect main business flow, continue execution
            }
        }
        catch (Exception ex)
        {
            // Debug logging handled by structured logging
            // Log to system log, but don't affect main business flow
            // If needed, more detailed error handling logic can be added here
            try
            {
                // Consider logging failed logs to backup storage or sending alerts
                // Debug logging handled by structured logging
            }
            catch
            {
                // Prevent secondary exceptions
            }
        }
    }

    /// <summary>
    /// Log batch task completions to Change Log
    /// </summary>
    private async Task LogBatchTaskCompletionsAsync(List<ChecklistTaskCompletionInputDto> inputs, List<ChecklistTaskCompletion> completions)
    {
        try
        {
            var logEntries = new List<StageCompletionLog>();
            var tenantId = GetTenantId();
            // Debug logging handled by structured logging
            for (int i = 0; i < inputs.Count && i < completions.Count; i++)
            {
                var input = inputs[i];
                var completion = completions[i];
                // Debug logging handled by structured logging
                // Get onboarding and task information
                var onboarding = await _onboardingRepository.GetByIdAsync(input.OnboardingId);
                var task = await _taskRepository.GetByIdAsync(input.TaskId);

                if (onboarding == null || task == null)
                {
                    // Debug logging handled by structured logging
                    continue;
                }

                // Get current stage information
                Stage currentStage = null;
                if (onboarding.CurrentStageId.HasValue)
                {
                    currentStage = await _stageRepository.GetByIdAsync(onboarding.CurrentStageId.Value);
                }

                var logData = new
                {
                    OnboardingId = onboarding.Id,
                    LeadId = onboarding.LeadId,
                    LeadName = onboarding.LeadName,
                    StageId = currentStage?.Id ?? 0,
                    StageName = currentStage?.Name ?? "Unknown Stage",
                    ChecklistId = task.ChecklistId,
                    TaskId = task.Id,
                    TaskName = task.Name,
                    IsRequired = task.IsRequired,
                    IsCompleted = completion.IsCompleted,
                    CompletionNotes = completion.CompletionNotes,
                    CompletedTime = completion.CompletedTime,
                    CompletedBy = GetCurrentUserName(),
                    Action = completion.IsCompleted ? "Task Completed" : "Task Marked Incomplete",
                    BatchOperation = true
                };

                var stageCompletionLog = new StageCompletionLog
                {
                    TenantId = tenantId,
                    OnboardingId = onboarding.Id,
                    StageId = currentStage?.Id ?? 0,
                    StageName = currentStage?.Name ?? "Unknown Stage",
                    LogType = "task_completion_batch",
                    Action = completion.IsCompleted ? "Batch Task Completed" : "Batch Task Marked Incomplete",
                    LogData = System.Text.Json.JsonSerializer.Serialize(logData),
                    Success = true,
                    NetworkStatus = "online",
                    CreateBy = GetCurrentUserName(),
                    ModifyBy = GetCurrentUserName()
                };

                logEntries.Add(stageCompletionLog);
                // Debug logging handled by structured logging
                // Also log to ff_operation_change_log table
                try
                {
                    var operationType = completion.IsCompleted ? OperationTypeEnum.ChecklistTaskComplete : OperationTypeEnum.ChecklistTaskUncomplete;
                    var operationDescription = $"Checklist task '{task.Name}' has been {(completion.IsCompleted ? "completed" : "marked as incomplete")} by {GetCurrentUserName()} (Batch Operation)";

                    // Prepare before_data and after_data
                    var beforeData = new
                    {
                        TaskId = task.Id,
                        TaskName = task.Name,
                        IsCompleted = !completion.IsCompleted, // Opposite status
                        CompletionNotes = "",
                        CompletedTime = (DateTimeOffset?)null
                    };

                    var afterData = new
                    {
                        TaskId = task.Id,
                        TaskName = task.Name,
                        IsCompleted = completion.IsCompleted,
                        CompletionNotes = completion.CompletionNotes ?? "",
                        CompletedTime = completion.CompletedTime
                    };

                    var changedFields = new List<string> { "IsCompleted" };
                    if (!string.IsNullOrEmpty(completion.CompletionNotes))
                    {
                        changedFields.Add("CompletionNotes");
                    }
                    if (completion.IsCompleted && completion.CompletedTime.HasValue)
                    {
                        changedFields.Add("CompletedTime");
                    }

                    var extendedData = new
                    {
                        ChecklistId = task.ChecklistId,
                        TaskType = task.TaskType,
                        Priority = task.Priority,
                        IsRequired = task.IsRequired,
                        WorkflowId = onboarding.WorkflowId,
                        BatchOperation = true,
                        BatchIndex = i + 1,
                        BatchTotal = inputs.Count,
                        CompletedAt = DateTimeOffset.UtcNow
                    };

                    await _operationChangeLogService.LogOperationAsync(
                        operationType: operationType,
                        businessModule: BusinessModuleEnum.ChecklistTask,
                        businessId: task.Id,
                        onboardingId: onboarding.Id,
                        stageId: currentStage?.Id ?? 0,
                        operationTitle: $"Batch Task {(completion.IsCompleted ? "Completed" : "Incomplete")}: {task.Name}",
                        operationDescription: operationDescription,
                        beforeData: System.Text.Json.JsonSerializer.Serialize(beforeData),
                        afterData: System.Text.Json.JsonSerializer.Serialize(afterData),
                        changedFields: changedFields,
                        extendedData: System.Text.Json.JsonSerializer.Serialize(extendedData)
                    );

                    // Debug logging handled by structured logging}");
                }
                catch (Exception operationLogEx)
                {
                    // Debug logging handled by structured logging
                    // Does not affect main business flow, continue execution
                }
            }

            // Batch insert log entries to stage completion log
            // Debug logging handled by structured logging
            foreach (var logEntry in logEntries)
            {
                await _stageCompletionLogRepository.InsertAsync(logEntry);
            }
            // Debug logging handled by structured logging
        }
        catch (Exception ex)
        {
            // Debug logging handled by structured logging
            // Log to system log, but does not affect main business flow
            try
            {
                // Debug logging handled by structured logging
            }
            catch
            {
                // Prevent secondary exceptions
            }
        }
    }

    private string GetTenantId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            // Debug logging handled by structured logging
            // Try to get from UserContext
            if (!string.IsNullOrEmpty(_userContext?.TenantId))
            {
                // Debug logging handled by structured logging
                return _userContext.TenantId;
            }
            // Debug logging handled by structured logging
            return "default";
        }

        // Try to get TenantId from request headers
        var tenantId = context.Request.Headers["TenantId"].FirstOrDefault();
        // Debug logging handled by structured logging
        if (string.IsNullOrEmpty(tenantId))
        {
            tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            // Debug logging handled by structured logging
        }

        // Try to get from UserContext
        if (string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(_userContext?.TenantId))
        {
            tenantId = _userContext.TenantId;
            // Debug logging handled by structured logging
        }

        // If still empty, use default value
        if (string.IsNullOrEmpty(tenantId))
        {
            tenantId = "default";
            // Debug logging handled by structured logging
        }
        else
        {
            // Debug logging handled by structured logging
        }

        return tenantId;
    }
}