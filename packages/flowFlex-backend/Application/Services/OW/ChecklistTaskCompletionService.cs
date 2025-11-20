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
using FlowFlex.Domain.Shared.Events.Action;
using FlowFlex.Domain.Shared.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using FlowFlex.Domain.Repository.Action;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace FlowFlex.Application.Services.OW;

/// <summary>
/// ChecklistTaskCompletion service implementation
/// </summary>
public class ChecklistTaskCompletionService : IChecklistTaskCompletionService, IScopedService
{
    private readonly IChecklistTaskCompletionRepository _completionRepository;
    private readonly IChecklistTaskRepository _taskRepository;
    private readonly IChecklistTaskNoteRepository _noteRepository;
    private readonly IOnboardingRepository _onboardingRepository;
    private readonly IStageRepository _stageRepository;

    private readonly IOperationChangeLogService _operationChangeLogService;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserContext _userContext;
    private readonly IMediator _mediator;
    private readonly ILogger<ChecklistTaskCompletionService> _logger;
    private readonly IChecklistService _checklistService;
    private readonly IActionDefinitionRepository _actionDefinitionRepository;
    private readonly IActionTriggerMappingRepository _actionTriggerMappingRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOperatorContextService _operatorContextService;

    public ChecklistTaskCompletionService(
    IChecklistTaskCompletionRepository completionRepository,
    IChecklistTaskRepository taskRepository,
    IChecklistTaskNoteRepository noteRepository,
    IOnboardingRepository onboardingRepository,
    IStageRepository stageRepository,

    IOperationChangeLogService operationChangeLogService,
    IMapper mapper,
    IHttpContextAccessor httpContextAccessor,
    UserContext userContext,
    IMediator mediator,
    ILogger<ChecklistTaskCompletionService> logger,
    IChecklistService checklistService,
    IActionDefinitionRepository actionDefinitionRepository,
    IActionTriggerMappingRepository actionTriggerMappingRepository,
    IServiceProvider serviceProvider,
    IOperatorContextService operatorContextService)
    {
        _completionRepository = completionRepository;
        _taskRepository = taskRepository;
        _noteRepository = noteRepository;
        _onboardingRepository = onboardingRepository;
        _stageRepository = stageRepository;

        _operationChangeLogService = operationChangeLogService;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        _userContext = userContext;
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _operatorContextService = operatorContextService;
        _checklistService = checklistService ?? throw new ArgumentNullException(nameof(checklistService));
        _actionDefinitionRepository = actionDefinitionRepository ?? throw new ArgumentNullException(nameof(actionDefinitionRepository));
        _actionTriggerMappingRepository = actionTriggerMappingRepository ?? throw new ArgumentNullException(nameof(actionTriggerMappingRepository));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Get current user name from OperatorContextService (FirstName + LastName > UserName > Email)
    /// </summary>
    private string GetCurrentUserName()
    {
        return _operatorContextService.GetOperatorDisplayName();
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

        // Set LeadId from onboarding if not provided in input
        // LeadId is optional since Case Code is the primary identifier
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
            completion.CompletedTime = DateTimeOffset.UtcNow;
        }
        else
        {
            completion.CompletedTime = null;
        }

        // Initialize create information with proper ID and timestamps
        completion.InitCreateInfo(_userContext);

        var (success, statusChanged) = await _completionRepository.SaveTaskCompletionAsync(completion);

        // Log task completion
        if (success)
        {
            await LogTaskCompletionAsync(onboarding, task, completion);

            // 只有当任务状态真正从未完成变为完成，且有 ActionMapping 时，才执行 ActionTriggerEvent
            // 这避免了当 isCompleted 为 true 但没有变化时重复执行 action
            var taskActionMapping = await GetTaskActionMappingAsync(task.Id);
            if (completion.IsCompleted && taskActionMapping != null && statusChanged)
            {
                _logger.LogInformation("Task completion status changed from false to true, executing action: OnboardingId={OnboardingId}, TaskId={TaskId}, ActionId={ActionId}, MappingId={MappingId}",
                    completion.OnboardingId, completion.TaskId, taskActionMapping.ActionDefinitionId, taskActionMapping.Id);
                await HandleTaskActionAsync(onboarding, task, completion, taskActionMapping);
            }
            else if (completion.IsCompleted && taskActionMapping != null && !statusChanged)
            {
                _logger.LogDebug("Task completion status unchanged (already completed), skipping action execution: OnboardingId={OnboardingId}, TaskId={TaskId}, ActionId={ActionId}",
                    completion.OnboardingId, completion.TaskId, taskActionMapping.ActionDefinitionId);
            }
        }

        return success;
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

            // Set LeadId from onboarding if not provided in input
            // LeadId is optional since Case Code is the primary identifier
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
                completion.CompletedTime = DateTimeOffset.UtcNow;
            }
            else
            {
                completion.CompletedTime = null;
            }

            // Initialize create information with proper ID and timestamps
            completion.InitCreateInfo(_userContext);

            completions.Add(completion);
        }

        var results = await _completionRepository.BatchSaveTaskCompletionsAsync(completions);

        // Check if all operations were successful
        var allSuccessful = results.All(r => r.success);

        // Log batch task completions
        if (allSuccessful)
        {
            await LogBatchTaskCompletionsAsync(inputs, completions);

            // 只为状态真正发生变化的且有 ActionId 的任务发布 ActionTriggerEvent
            await PublishBatchTaskActionTriggerEventsAsync(inputs, completions, results);
        }

        return allSuccessful;
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
        var completionDtos = _mapper.Map<List<ChecklistTaskCompletionOutputDto>>(completions);

        // Fill files and notes count for each completion
        await FillFilesAndNotesCountAsync(completionDtos, onboardingId);

        return completionDtos;
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
    public async Task<bool> ToggleTaskCompletionAsync(long onboardingId, long taskId, bool isCompleted, string completionNotes = "", string filesJson = "[]")
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
            CompletionNotes = completionNotes,
            FilesJson = filesJson
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
    private async Task LogTaskCompletionAsync(Onboarding onboarding, ChecklistTask task, ChecklistTaskCompletion completion)
    {
        try
        {
            var tenantId = GetTenantId();
            // Debug logging handled by structured logging
            // Get stage information from completion.StageId (user-specified stage)
            // This ensures we log the correct stage where the task was completed
            Stage currentStage = null;
            var stageIdToUse = completion.StageId ?? onboarding.CurrentStageId;

            if (stageIdToUse.HasValue)
            {
                currentStage = await _stageRepository.GetByIdAsync(stageIdToUse.Value);
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
                IsCompleted = completion.IsCompleted,
                CompletionNotes = completion.CompletionNotes,
                CompletedTime = DateTimeOffset.UtcNow,
                CompletedBy = GetCurrentUserName(),
                Action = completion.IsCompleted ? "Task Completed" : "Task Marked Incomplete",
                WorkflowId = onboarding.WorkflowId,
                Priority = onboarding.Priority
            };

            var serializedLogData = System.Text.Json.JsonSerializer.Serialize(logData);
            // Debug logging handled by structured logging
            // Stage completion log functionality removed

            // 2. Also log to ff_operation_change_log table (new functionality)
            try
            {
                var operationType = completion.IsCompleted ? OperationTypeEnum.ChecklistTaskComplete : OperationTypeEnum.ChecklistTaskUncomplete;
                var operationDescription = $"Checklist task '{task.Name}' has been {(completion.IsCompleted ? "completed" : "marked as incomplete")} by {GetCurrentUserName()}";

                // Prepare before_data and after_data
                var beforeData = new
                {
                    TaskId = task.Id,
                    TaskName = task.Name,
                    IsCompleted = !completion.IsCompleted, // Opposite state
                    CompletionNotes = "",
                    CompletedTime = (DateTimeOffset?)null
                };

                var afterData = new
                {
                    TaskId = task.Id,
                    TaskName = task.Name,
                    IsCompleted = completion.IsCompleted,
                    CompletionNotes = completion.CompletionNotes ?? "",
                    CompletedTime = completion.IsCompleted ? DateTimeOffset.UtcNow : (DateTimeOffset?)null
                };

                var changedFields = new List<string> { "IsCompleted" };
                if (!string.IsNullOrEmpty(completion.CompletionNotes))
                {
                    changedFields.Add("CompletionNotes");
                }
                if (completion.IsCompleted)
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
                    operationTitle: $"Task {(completion.IsCompleted ? "Completed" : "Incomplete")}: {task.Name}",
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
            // Stage completion log functionality removed
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

                // Stage completion log functionality removed
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

            // Stage completion log functionality removed
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
            tenantId = "DEFAULT";
            // Debug logging handled by structured logging
        }
        else
        {
            // Debug logging handled by structured logging
        }

        return tenantId;
    }

    /// <summary>
    /// Fill files and notes count for completion DTOs
    /// </summary>
    private async Task FillFilesAndNotesCountAsync(List<ChecklistTaskCompletionOutputDto> completionDtos, long onboardingId)
    {
        if (completionDtos == null || !completionDtos.Any())
            return;

        foreach (var completionDto in completionDtos)
        {
            // Get files count from FilesJson
            completionDto.FilesCount = GetFilesCountFromJson(completionDto.FilesJson);

            // Get notes count from ChecklistTaskNote
            completionDto.NotesCount = await _noteRepository.CountNotesAsync(completionDto.TaskId, onboardingId, false);
        }
    }

    /// <summary>
    /// Get files count from FilesJson string
    /// </summary>
    private int GetFilesCountFromJson(string? filesJson)
    {
        if (string.IsNullOrEmpty(filesJson) || filesJson == "[]")
        {
            return 0;
        }

        try
        {
            var files = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>(filesJson);
            return files?.Length ?? 0;
        }
        catch (System.Text.Json.JsonException)
        {
            return 0;
        }
    }

    /// <summary>
    /// Process checklist component actions and publish action trigger events for completed tasks
    /// </summary>
    public async Task<ChecklistActionProcessingResultDto> ProcessChecklistComponentActionsAsync(ProcessChecklistActionsRequestDto request)
    {
        var result = new ChecklistActionProcessingResultDto
        {
            Success = true,
            Messages = new List<string>(),
            Errors = new List<string>()
        };

        try
        {
            _logger.LogInformation("开始处理 checklist 组件 action 发布: OnboardingId={OnboardingId}, StageId={StageId}",
                request.OnboardingId, request.CompletedStageId);

            // 获取所有 checklist 组件
            var checklistComponents = request.StageComponents.Where(c => c.Key == "checklist").ToList();

            if (!checklistComponents.Any())
            {
                result.Messages.Add($"Stage {request.CompletedStageId} 没有 checklist 组件");
                _logger.LogDebug("Stage {StageId} 没有 checklist 组件", request.CompletedStageId);
                return result;
            }

            // 收集所有 checklist IDs
            var checklistIds = checklistComponents
                .SelectMany(c => c.ChecklistIds ?? new List<long>())
                .Distinct()
                .ToList();

            if (!checklistIds.Any())
            {
                result.Messages.Add($"Stage {request.CompletedStageId} 的 checklist 组件没有配置 checklist IDs");
                _logger.LogDebug("Stage {StageId} 的 checklist 组件没有配置 checklist IDs", request.CompletedStageId);
                return result;
            }

            _logger.LogDebug("Stage {StageId} 找到 {Count} 个 checklist IDs: [{ChecklistIds}]",
                request.CompletedStageId, checklistIds.Count, string.Join(", ", checklistIds));

            // 批量获取 checklists
            var checklists = await _checklistService.GetByIdsAsync(checklistIds);

            // 获取当前 onboarding 和 stage 的所有任务完成状态
            var taskCompletions = await GetByOnboardingAndStageAsync(request.OnboardingId, request.CompletedStageId);
            var taskCompletionMap = taskCompletions.ToDictionary(tc => tc.TaskId, tc => tc.IsCompleted);

            int totalTasksProcessed = 0;
            int totalActionsPublished = 0;

            foreach (var checklist in checklists)
            {
                if (checklist.Tasks != null && checklist.Tasks.Any())
                {
                    foreach (var task in checklist.Tasks)
                    {
                        totalTasksProcessed++;

                        // 只为有 ActionMapping 的 task 创建 ActionTriggerEvent
                        var taskActionMapping = await GetTaskActionMappingAsync(task.Id);
                        if (taskActionMapping == null)
                        {
                            _logger.LogDebug("Task {TaskId} ({TaskName}) 没有配置 ActionMapping，跳过发布 ActionTriggerEvent",
                                task.Id, task.Name);
                            result.Messages.Add($"Task {task.Id} ({task.Name}) 没有配置 ActionMapping，跳过");
                            continue;
                        }

                        // 检查 task 是否已完成
                        var isCompleted = taskCompletionMap.ContainsKey(task.Id) && taskCompletionMap[task.Id];
                        if (!isCompleted)
                        {
                            _logger.LogDebug("Task {TaskId} ({TaskName}) 尚未完成，跳过发布 ActionTriggerEvent",
                                task.Id, task.Name);
                            result.Messages.Add($"Task {task.Id} ({task.Name}) 尚未完成，跳过");
                            continue;
                        }

                        // 为每个 task 创建 ActionTriggerEvent
                        var taskContextData = new
                        {
                            // 包含原始上下文数据
                            request.OnboardingId,
                            request.LeadId,
                            request.WorkflowId,
                            request.WorkflowName,
                            request.CompletedStageId,
                            request.CompletedStageName,
                            request.NextStageId,
                            request.NextStageName,
                            request.CompletionRate,
                            request.IsFinalStage,
                            request.BusinessContext,
                            request.Components,
                            request.TenantId,
                            request.Source,
                            request.Priority,
                            request.OriginalEventId,

                            // 添加 task 相关的上下文数据
                            ChecklistId = checklist.Id,
                            ChecklistName = checklist.Name,
                            TaskId = task.Id,
                            TaskName = task.Name,
                            TaskType = task.TaskType,
                            TaskIsRequired = task.IsRequired,
                            TaskPriority = task.Priority,
                            TaskAssigneeId = task.AssigneeId,
                            TaskAssigneeName = task.AssigneeName,
                            TaskAssignedTeam = task.AssignedTeam,
                            TaskActionId = task.ActionId,
                            TaskActionName = task.ActionName
                        };

                        var taskActionTriggerEvent = new ActionTriggerEvent(
                            triggerSourceType: "",
                            triggerSourceId: task.Id,
                            triggerEventType: "Completed",
                            contextData: taskContextData,
                            userId: request.CurrentUserId
                        );

                        await _mediator.Publish(taskActionTriggerEvent);

                        totalActionsPublished++;

                        _logger.LogDebug("已发布 Task ActionTriggerEvent: TaskId={TaskId}, TaskName={TaskName}, ChecklistId={ChecklistId}, StageId={StageId}",
                            task.Id, task.Name, checklist.Id, request.CompletedStageId);

                        result.Messages.Add($"已发布 Task ActionTriggerEvent: TaskId={task.Id}, TaskName={task.Name}");
                    }
                }
            }

            result.TotalTasksProcessed = totalTasksProcessed;
            result.TotalActionsPublished = totalActionsPublished;

            _logger.LogInformation("完成处理 checklist 组件 action 发布: StageId={StageId}, ChecklistCount={ChecklistCount}, TasksProcessed={TasksProcessed}, ActionsPublished={ActionsPublished}",
                request.CompletedStageId, checklists.Count, totalTasksProcessed, totalActionsPublished);

            result.Messages.Add($"处理完成：共处理 {totalTasksProcessed} 个任务，发布 {totalActionsPublished} 个 ActionTriggerEvent");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"处理 checklist 组件 action 时发生错误: {ex.Message}");
            _logger.LogError(ex, "处理 checklist 组件 action 时发生错误: OnboardingId={OnboardingId}, StageId={StageId}",
                request.OnboardingId, request.CompletedStageId);
        }

        return result;
    }

    /// <summary>
    /// 处理任务完成时的 Action 逻辑（包括特殊处理和事件发布）
    /// </summary>
    private async Task HandleTaskActionAsync(Onboarding onboarding, ChecklistTask task, ChecklistTaskCompletion completion, Domain.Entities.Action.ActionTriggerMapping taskActionMapping)
    {
        try
        {
            _logger.LogDebug("开始处理任务完成的 Action: TaskId={TaskId}, ActionId={ActionId}, MappingId={MappingId}",
                task.Id, taskActionMapping.ActionDefinitionId, taskActionMapping.Id);

            // 获取 Action 定义
            var actionDefinition = await _actionDefinitionRepository.GetByIdAsync(taskActionMapping.ActionDefinitionId);
            if (actionDefinition == null)
            {
                _logger.LogWarning("Action 定义不存在: ActionId={ActionId}, TaskId={TaskId}, MappingId={MappingId}",
                    taskActionMapping.ActionDefinitionId, task.Id, taskActionMapping.Id);
                return;
            }

            _logger.LogDebug("获取到 Action 定义: ActionId={ActionId}, ActionType={ActionType}, ActionName={ActionName}, ActionConfig={ActionConfig}",
                actionDefinition.Id, actionDefinition.ActionType, actionDefinition.ActionName, actionDefinition.ActionConfig);

            // 检查是否是 System 类型的 CompleteStage action
            if (await IsCompleteStageSystemActionAsync(actionDefinition))
            {
                _logger.LogInformation("检测到 CompleteStage System Action，开始自动完成 Stage: TaskId={TaskId}, ActionId={ActionId}, OnboardingId={OnboardingId}, StageId={StageId}",
                    task.Id, taskActionMapping.ActionDefinitionId, onboarding.Id, completion.StageId);

                await ExecuteCompleteStageActionAsync(onboarding, task, completion, actionDefinition);
            }

            // 无论是否是特殊 action，都发布 ActionTriggerEvent
            await PublishTaskActionTriggerEventAsync(onboarding, task, completion, taskActionMapping);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理任务 Action 时发生错误: TaskId={TaskId}, ActionId={ActionId}, MappingId={MappingId}",
                task.Id, taskActionMapping.ActionDefinitionId, taskActionMapping.Id);
            // 不重新抛出异常，避免影响主业务流程
        }
    }

    /// <summary>
    /// 检查和修复阶段完成状态不一致的问题
    /// </summary>
    private async Task CheckAndFixStageCompletionInconsistencyAsync(Onboarding onboarding, long stageId)
    {
        try
        {
            if (stageId <= 0) return;

            var onboardingService = _serviceProvider.GetRequiredService<IOnboardingService>();
            var updatedOnboardingDto = await onboardingService.GetByIdAsync(onboarding.Id);

            if (updatedOnboardingDto?.StagesProgress != null)
            {
                var stageProgress = updatedOnboardingDto.StagesProgress.FirstOrDefault(sp => sp.StageId == stageId);
                if (stageProgress != null && !stageProgress.IsCompleted &&
                    string.Equals(updatedOnboardingDto.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("发现数据不一致: Onboarding 已完成但 Stage 标记为未完成, 正在修复: OnboardingId={OnboardingId}, StageId={StageId}, StageIsCompleted={StageIsCompleted}",
                        onboarding.Id, stageId, stageProgress.IsCompleted);

                    // Fix the inconsistency by marking the stage as completed
                    stageProgress.IsCompleted = true;
                    stageProgress.Status = "Completed";
                    stageProgress.CompletionTime = stageProgress.CompletionTime ?? DateTimeOffset.UtcNow;
                    stageProgress.CompletedBy = stageProgress.CompletedBy ?? _userContext?.UserName ?? "System";

                    // Handle UserId type conversion - UserId is string, CompletedById expects long?
                    if (!stageProgress.CompletedById.HasValue)
                    {
                        if (long.TryParse(_userContext?.UserId, out long userId))
                        {
                            stageProgress.CompletedById = userId;
                        }
                        else
                        {
                            stageProgress.CompletedById = null; // Default to null if conversion fails
                        }
                    }

                    // Use raw SQL to update JSONB field to avoid type conversion issues
                    try
                    {
                        var jsonOptions = new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            WriteIndented = false
                        };
                        var stagesProgressJson = System.Text.Json.JsonSerializer.Serialize(updatedOnboardingDto.StagesProgress, jsonOptions);

                        // Use SqlSugar's native SQL execution to handle JSONB update with proper casting
                        var updateSql = "UPDATE ff_onboarding SET stages_progress_json = @stagesProgressJson::jsonb WHERE id = @onboardingId";
                        var sqlSugarClient = _onboardingRepository.GetSqlSugarClient();
                        await sqlSugarClient.Ado.ExecuteCommandAsync(updateSql, new { stagesProgressJson, onboardingId = onboarding.Id });

                        _logger.LogInformation("已修复数据不一致性: OnboardingId={OnboardingId}, StageId={StageId}, 现在标记为已完成",
                            onboarding.Id, stageId);

                        // Log the stage completion inconsistency fix to operation_change_log
                        try
                        {
                            var stage = await _stageRepository.GetByIdAsync(stageId);
                            var beforeData = new { IsCompleted = false, Status = stageProgress.Status };
                            var afterData = new { IsCompleted = true, Status = "Completed" };

                            await _operationChangeLogService.LogOperationAsync(
                                operationType: OperationTypeEnum.StageComplete,
                                businessModule: BusinessModuleEnum.Stage,
                                businessId: stageId,
                                onboardingId: onboarding.Id,
                                stageId: stageId,
                                operationTitle: $"Stage Completion Inconsistency Fixed: {stage?.Name ?? "Unknown"}",
                                operationDescription: $"Fixed data inconsistency - Stage marked as completed (auto-fix by system)",
                                beforeData: System.Text.Json.JsonSerializer.Serialize(beforeData),
                                afterData: System.Text.Json.JsonSerializer.Serialize(afterData),
                                changedFields: new List<string> { "IsCompleted", "Status" },
                                extendedData: System.Text.Json.JsonSerializer.Serialize(new { Reason = "Auto-fix inconsistency", Source = "CheckAndFixStageCompletionInconsistencyAsync" })
                            );
                        }
                        catch (Exception logEx)
                        {
                            _logger.LogError(logEx, "记录 Stage 完成不一致修复日志失败: OnboardingId={OnboardingId}, StageId={StageId}",
                                onboarding.Id, stageId);
                        }
                    }
                    catch (Exception updateEx)
                    {
                        _logger.LogError(updateEx, "更新 JSONB 字段时发生错误: OnboardingId={OnboardingId}, StageId={StageId}",
                            onboarding.Id, stageId);
                        // Don't re-throw to avoid breaking the main flow
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查和修复阶段完成状态不一致时发生错误: OnboardingId={OnboardingId}, StageId={StageId}",
                onboarding.Id, stageId);
        }
    }

    /// <summary>
    /// 检查是否是 CompleteStage System Action
    /// </summary>
    private async Task<bool> IsCompleteStageSystemActionAsync(Domain.Entities.Action.ActionDefinition actionDefinition)
    {
        try
        {
            _logger.LogDebug("检查是否为 CompleteStage System Action: ActionId={ActionId}, ActionType={ActionType}",
                actionDefinition.Id, actionDefinition.ActionType);

            // 检查 ActionType 是否为 System
            if (actionDefinition.ActionType != "System")
            {
                _logger.LogDebug("ActionType 不是 System: ActionType={ActionType}", actionDefinition.ActionType);
                return false;
            }

            // 检查 ActionConfig 中的 actionName 是否为 CompleteStage
            if (actionDefinition.ActionConfig != null)
            {
                var config = actionDefinition.ActionConfig as JObject;
                var actionName = config?["actionName"]?.ToString();

                _logger.LogDebug("检查 ActionConfig 中的 actionName: actionName={ActionName}", actionName);

                var isCompleteStage = string.Equals(actionName, "CompleteStage", StringComparison.OrdinalIgnoreCase);
                _logger.LogDebug("CompleteStage 检查结果: {IsCompleteStage}", isCompleteStage);

                return isCompleteStage;
            }

            _logger.LogDebug("ActionConfig 为空");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查 CompleteStage System Action 时发生错误: ActionId={ActionId}", actionDefinition.Id);
            return false;
        }
    }

    /// <summary>
    /// 执行 CompleteStage System Action
    /// </summary>
    private async Task ExecuteCompleteStageActionAsync(Onboarding onboarding, ChecklistTask task, ChecklistTaskCompletion completion, Domain.Entities.Action.ActionDefinition actionDefinition)
    {
        try
        {
            // Check if Onboarding is already completed to avoid redundant operations
            if (string.Equals(onboarding.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Onboarding 已完成，跳过 CompleteStage 操作: OnboardingId={OnboardingId}, Status={Status}, TaskId={TaskId}",
                    onboarding.Id, onboarding.Status, task.Id);

                // However, check for data inconsistency - if Onboarding is completed but specific stage shows as incomplete
                await CheckAndFixStageCompletionInconsistencyAsync(onboarding, completion.StageId ?? onboarding.CurrentStageId ?? 0);
                return;
            }

            // 确定要完成的 Stage ID - 优先使用用户指定的阶段，避免自动使用当前阶段导致错误的阶段完成
            // 用户在任务完成时明确指定了要完成的阶段，应该严格按照用户的意图执行
            long stageIdToComplete = completion.StageId ?? 0;

            // 如果用户没有指定 StageId，记录警告但不使用当前阶段（避免误操作）
            if (stageIdToComplete <= 0)
            {
                _logger.LogWarning("CompleteStage System Action 需要明确的 StageId，但用户未提供: OnboardingId={OnboardingId}, TaskId={TaskId}, CurrentStageId={CurrentStageId}",
                    onboarding.Id, task.Id, onboarding.CurrentStageId);
                return;
            }

            _logger.LogInformation("开始自动完成 Stage: OnboardingId={OnboardingId}, StageId={StageId}, TaskId={TaskId}, TaskName={TaskName}",
                onboarding.Id, stageIdToComplete, task.Id, task.Name);

            // 构建完成 Stage 的上下文数据
            var completeStageContext = new
            {
                OnboardingId = onboarding.Id,
                StageId = stageIdToComplete,
                CompletionNotes = $"Stage completed automatically by task '{task.Name}' (TaskId: {task.Id})",
                CompletedBy = completion.CreateBy ?? _userContext?.UserName ?? "System",
                AutoMoveToNext = false, // 只完成当前 stage，不自动移动到下一个 stage
                Source = "task_completion_auto",
                TriggerTaskId = task.Id,
                TriggerTaskName = task.Name,
                TriggerActionId = task.ActionId
            };

            // 直接调用内部完成方法，避免事件发布和循环依赖
            var onboardingService = _serviceProvider.GetRequiredService<IOnboardingService>();
            var completed = await onboardingService.CompleteCurrentStageInternalAsync(onboarding.Id, new FlowFlex.Application.Contracts.Dtos.OW.Onboarding.CompleteCurrentStageInputDto
            {
                StageId = stageIdToComplete,
                CompletionNotes = completeStageContext.CompletionNotes,
                ForceComplete = false,
                PreventAutoMove = true // 阻止自动移动，确保严格按照用户指定的阶段执行
            });

            _logger.LogInformation("直接完成 Stage (无事件): OnboardingId={OnboardingId}, StageId={StageId}, TaskId={TaskId}, TaskName={TaskName}, Success={Success}",
                onboarding.Id, stageIdToComplete, task.Id, task.Name, completed);

            if (!completed)
            {
                _logger.LogWarning("Stage 完成操作未成功: OnboardingId={OnboardingId}, StageId={StageId}, TaskId={TaskId}",
                    onboarding.Id, stageIdToComplete, task.Id);
            }
            else
            {
                // Log the auto stage completion to operation_change_log
                try
                {
                    var stage = await _stageRepository.GetByIdAsync(stageIdToComplete);
                    var beforeData = new
                    {
                        StageId = stageIdToComplete,
                        StageName = stage?.Name,
                        IsCompleted = false,
                        Status = "InProgress"
                    };

                    var afterData = new
                    {
                        StageId = stageIdToComplete,
                        StageName = stage?.Name,
                        IsCompleted = true,
                        Status = "Completed",
                        CompletedTime = DateTimeOffset.UtcNow
                    };

                    var extendedData = new
                    {
                        TriggerSource = "TaskCompletion",
                        TriggerTaskId = task.Id,
                        TriggerTaskName = task.Name,
                        TriggerActionId = task.ActionId,
                        ActionType = "System",
                        ActionName = "CompleteStage",
                        AutoCompleted = true,
                        CompletionNotes = completeStageContext.CompletionNotes
                    };

                    await _operationChangeLogService.LogOperationAsync(
                        operationType: OperationTypeEnum.StageComplete,
                        businessModule: BusinessModuleEnum.Stage,
                        businessId: stageIdToComplete,
                        onboardingId: onboarding.Id,
                        stageId: stageIdToComplete,
                        operationTitle: $"Stage Auto-Completed: {stage?.Name ?? "Unknown"}",
                        operationDescription: $"Stage '{stage?.Name}' was automatically completed by task '{task.Name}' (TaskId: {task.Id})",
                        beforeData: System.Text.Json.JsonSerializer.Serialize(beforeData),
                        afterData: System.Text.Json.JsonSerializer.Serialize(afterData),
                        changedFields: new List<string> { "IsCompleted", "Status", "CompletedTime" },
                        extendedData: System.Text.Json.JsonSerializer.Serialize(extendedData)
                    );

                    _logger.LogInformation("已记录 Stage 自动完成日志: OnboardingId={OnboardingId}, StageId={StageId}, TaskId={TaskId}",
                        onboarding.Id, stageIdToComplete, task.Id);
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "记录 Stage 自动完成日志失败: OnboardingId={OnboardingId}, StageId={StageId}, TaskId={TaskId}",
                        onboarding.Id, stageIdToComplete, task.Id);
                    // Don't re-throw to avoid breaking the main flow
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行 CompleteStage System Action 时发生错误: OnboardingId={OnboardingId}, TaskId={TaskId}",
                onboarding.Id, task.Id);
        }
    }

    /// <summary>
    /// 为完成的 task 发布 ActionTriggerEvent
    /// </summary>
    private async Task PublishTaskActionTriggerEventAsync(Onboarding onboarding, ChecklistTask task, ChecklistTaskCompletion completion, Domain.Entities.Action.ActionTriggerMapping taskActionMapping)
    {
        try
        {
            _logger.LogDebug("开始为完成的 task 发布 ActionTriggerEvent: TaskId={TaskId}, TaskName={TaskName}, ActionId={ActionId}, MappingId={MappingId}",
                task.Id, task.Name, taskActionMapping.ActionDefinitionId, taskActionMapping.Id);

            // 获取当前用户ID
            var currentUserId = GetCurrentUserId();

            // 获取 Action 名称
            var actionDefinition = await _actionDefinitionRepository.GetByIdAsync(taskActionMapping.ActionDefinitionId);
            var actionName = actionDefinition?.ActionName;

            // 构建上下文数据
            var contextData = new
            {
                OnboardingId = onboarding.Id,
                LeadId = onboarding.LeadId,
                LeadName = onboarding.LeadName,
                WorkflowId = onboarding.WorkflowId,
                CurrentStageId = onboarding.CurrentStageId,
                CurrentStageOrder = onboarding.CurrentStageOrder,
                Status = onboarding.Status,
                CompletionRate = onboarding.CompletionRate,
                Priority = onboarding.Priority,
                CurrentAssigneeId = onboarding.CurrentAssigneeId,
                CurrentAssigneeName = onboarding.CurrentAssigneeName,
                CurrentTeam = onboarding.CurrentTeam,

                // Task 相关的上下文数据
                ChecklistId = task.ChecklistId,
                TaskId = task.Id,
                TaskName = task.Name,
                TaskDescription = task.Description,
                TaskType = task.TaskType,
                TaskIsRequired = task.IsRequired,
                TaskPriority = task.Priority,
                TaskAssigneeId = task.AssigneeId,
                TaskAssigneeName = task.AssigneeName,
                TaskAssignedTeam = task.AssignedTeam,
                TaskActionId = taskActionMapping.ActionDefinitionId,
                TaskActionName = actionName,
                TaskActionMappingId = taskActionMapping.Id,

                // Completion 相关的上下文数据
                CompletedTime = completion.CompletedTime,
                CompletionNotes = completion.CompletionNotes,
                CompletedBy = completion.CreateBy,
                Source = completion.Source ?? "task_completion",
                StageId = completion.StageId
            };

            var taskActionTriggerEvent = new ActionTriggerEvent(
                triggerSourceType: "Task",
                triggerSourceId: task.Id,
                triggerEventType: "Completed",
                contextData: contextData,
                userId: currentUserId > 0 ? currentUserId : null
            );

            await _mediator.Publish(taskActionTriggerEvent);

            _logger.LogInformation("成功发布 Task ActionTriggerEvent: TaskId={TaskId}, TaskName={TaskName}, ActionId={ActionId}, MappingId={MappingId}, OnboardingId={OnboardingId}",
                task.Id, task.Name, taskActionMapping.ActionDefinitionId, taskActionMapping.Id, onboarding.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布 Task ActionTriggerEvent 时发生错误: TaskId={TaskId}, ActionId={ActionId}, MappingId={MappingId}",
                task.Id, taskActionMapping.ActionDefinitionId, taskActionMapping.Id);
            // 不重新抛出异常，避免影响主业务流程
        }
    }

    /// <summary>
    /// 为批量完成的 task 发布 ActionTriggerEvent
    /// </summary>
    private async Task PublishBatchTaskActionTriggerEventsAsync(List<ChecklistTaskCompletionInputDto> inputs, List<ChecklistTaskCompletion> completions, List<(bool success, bool statusChanged)> results)
    {
        try
        {
            _logger.LogDebug("开始为批量完成的 task 发布 ActionTriggerEvent: Count={Count}", completions.Count);

            for (int i = 0; i < inputs.Count && i < completions.Count && i < results.Count; i++)
            {
                var input = inputs[i];
                var completion = completions[i];
                var result = results[i];

                // 只为完成的任务且状态真正发生变化的任务处理
                if (!completion.IsCompleted || !result.statusChanged)
                {
                    if (completion.IsCompleted && !result.statusChanged)
                    {
                        _logger.LogDebug("Batch task completion status unchanged (already completed), skipping action execution: TaskId={TaskId}",
                            input.TaskId);
                    }
                    continue;
                }

                // 获取 task 和 onboarding 信息
                var task = await _taskRepository.GetByIdAsync(input.TaskId);
                var onboarding = await _onboardingRepository.GetByIdAsync(input.OnboardingId);

                if (task == null || onboarding == null)
                {
                    _logger.LogWarning("跳过发布 ActionTriggerEvent: Task={TaskId} 或 Onboarding={OnboardingId} 不存在",
                        input.TaskId, input.OnboardingId);
                    continue;
                }

                // 只为有 ActionMapping 的 task 发布事件
                var taskActionMapping = await GetTaskActionMappingAsync(task.Id);
                if (taskActionMapping == null)
                {
                    _logger.LogDebug("Task {TaskId} ({TaskName}) 没有配置 ActionMapping，跳过发布 ActionTriggerEvent",
                        task.Id, task.Name);
                    continue;
                }

                _logger.LogInformation("Batch task completion status changed from false to true, executing action: OnboardingId={OnboardingId}, TaskId={TaskId}, ActionId={ActionId}, MappingId={MappingId}",
                    input.OnboardingId, input.TaskId, taskActionMapping.ActionDefinitionId, taskActionMapping.Id);
                await HandleTaskActionAsync(onboarding, task, completion, taskActionMapping);
            }

            _logger.LogDebug("完成为批量完成的 task 发布 ActionTriggerEvent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量发布 Task ActionTriggerEvent 时发生错误");
            // 不重新抛出异常，避免影响主业务流程
        }
    }

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    private long GetCurrentUserId()
    {
        if (long.TryParse(_userContext?.UserId, out long userId))
        {
            return userId;
        }
        return 0;
    }

    /// <summary>
    /// 从映射表获取任务的动作映射（Task/Question 类型每个源只有一条映射）
    /// </summary>
    private async Task<Domain.Entities.Action.ActionTriggerMapping> GetTaskActionMappingAsync(long taskId)
    {
        try
        {
            var allTaskMappings = await _actionTriggerMappingRepository.GetByTriggerTypeAsync("Task");
            var taskMapping = allTaskMappings
                .Where(m => m.TriggerSourceId == taskId && m.IsValid && m.IsEnabled)
                .OrderByDescending(m => (m.ModifyDate > m.CreateDate) ? m.ModifyDate : m.CreateDate)
                .FirstOrDefault();

            return taskMapping;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取任务动作映射失败: TaskId={TaskId}", taskId);
            return null;
        }
    }
}