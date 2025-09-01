using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.Dtos.OW.Checklist;
using FlowFlex.Application.Contracts.Dtos.OW.ChecklistTask;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Service.OW;

/// <summary>
/// ChecklistTask service implementation
/// </summary>
public class ChecklistTaskService : IChecklistTaskService, IScopedService
{
    private readonly IChecklistTaskRepository _checklistTaskRepository;
    private readonly IChecklistRepository _checklistRepository;
    private readonly IChecklistService _checklistService;
    private readonly IOperationChangeLogService _operationChangeLogService;
    private readonly IChecklistTaskNoteRepository _noteRepository;
    private readonly IChecklistTaskCompletionRepository _completionRepository;
    private readonly IMapper _mapper;
    private readonly UserContext _userContext;
    private readonly IDistributedCacheService _cacheService;

    public ChecklistTaskService(
        IChecklistTaskRepository checklistTaskRepository,
        IChecklistRepository checklistRepository,
        IChecklistService checklistService,
        IOperationChangeLogService operationChangeLogService,
        IChecklistTaskNoteRepository noteRepository,
        IChecklistTaskCompletionRepository completionRepository,
        IMapper mapper,
        UserContext userContext,
        IDistributedCacheService cacheService)
    {
        _checklistTaskRepository = checklistTaskRepository;
        _checklistRepository = checklistRepository;
        _checklistService = checklistService;
        _operationChangeLogService = operationChangeLogService;
        _noteRepository = noteRepository;
        _completionRepository = completionRepository;
        _mapper = mapper;
        _userContext = userContext;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Create a new checklist task
    /// Supports ActionId and ActionName fields for linking tasks to specific actions
    /// </summary>
    public async Task<long> CreateAsync(ChecklistTaskInputDto input)
    {
        // Validate checklist exists
        var checklist = await _checklistRepository.GetByIdAsync(input.ChecklistId);
        if (checklist == null)
        {
            throw new CRMException(ErrorCodeEnum.NotFound, "Checklist not found");
        }

        // Validate dependent task if specified
        if (input.DependsOnTaskId.HasValue)
        {
            var dependentTask = await _checklistTaskRepository.GetByIdAsync(input.DependsOnTaskId.Value);
            if (dependentTask == null || dependentTask.ChecklistId != input.ChecklistId)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    "Dependent task not found or not in same checklist");
            }
        }

        var entity = _mapper.Map<ChecklistTask>(input);

        // Set default values - force Order to be max+1 for new tasks (ignore input OrderIndex)
        int nextOrder = await GetNextOrderAsync(input.ChecklistId);
        entity.Order = nextOrder; // Always use max+1 for new tasks, ignore input OrderIndex
        entity.Status = string.IsNullOrEmpty(entity.Status) ? "Pending" : entity.Status;
        entity.IsCompleted = false;

        // Initialize create information with proper ID and timestamps
        entity.InitCreateInfo(_userContext);

        await _checklistTaskRepository.InsertAsync(entity);

        // Log checklist task create operation (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await _operationChangeLogService.LogChecklistTaskCreateAsync(
                    taskId: entity.Id,
                    taskName: entity.Name,
                    checklistId: entity.ChecklistId
                );
            }
            catch
            {
                // Ignore logging errors to avoid affecting main operation
            }
        });

        // Update checklist completion rate
        await _checklistService.CalculateCompletionAsync(input.ChecklistId);

        return entity.Id;
    }

    /// <summary>
    /// Update an existing checklist task
    /// Supports updating ActionId and ActionName fields for linking tasks to specific actions
    /// </summary>
    public async Task<bool> UpdateAsync(long id, ChecklistTaskInputDto input)
    {
        var existingTask = await _checklistTaskRepository.GetByIdAsync(id);
        if (existingTask == null)
        {
            throw new CRMException(ErrorCodeEnum.NotFound, "Task not found");
        }

        // Store original values for change detection and logging
        var originalTask = new
        {
            Name = existingTask.Name,
            Description = existingTask.Description,
            Status = existingTask.Status,
            Priority = existingTask.Priority,
            IsCompleted = existingTask.IsCompleted,
            IsRequired = existingTask.IsRequired,
            AssigneeId = existingTask.AssigneeId,
            AssigneeName = existingTask.AssigneeName,
            Order = existingTask.Order,
            EstimatedHours = existingTask.EstimatedHours,
            DueDate = existingTask.DueDate,
            DependsOnTaskId = existingTask.DependsOnTaskId
        };

        // Validate dependent task if specified
        if (input.DependsOnTaskId.HasValue)
        {
            var dependentTask = await _checklistTaskRepository.GetByIdAsync(input.DependsOnTaskId.Value);
            if (dependentTask == null || dependentTask.ChecklistId != existingTask.ChecklistId)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    "Dependent task not found or not in same checklist");
            }
        }

        _mapper.Map(input, existingTask);
        existingTask.InitUpdateInfo(_userContext);

        // Check if there are any actual changes
        var hasChanges = 
            originalTask.Name != existingTask.Name ||
            originalTask.Description != existingTask.Description ||
            originalTask.Status != existingTask.Status ||
            originalTask.Priority != existingTask.Priority ||
            originalTask.IsCompleted != existingTask.IsCompleted ||
            originalTask.IsRequired != existingTask.IsRequired ||
            originalTask.AssigneeId != existingTask.AssigneeId ||
            originalTask.AssigneeName != existingTask.AssigneeName ||
            originalTask.Order != existingTask.Order ||
            originalTask.EstimatedHours != existingTask.EstimatedHours ||
            originalTask.DueDate != existingTask.DueDate ||
            originalTask.DependsOnTaskId != existingTask.DependsOnTaskId;

        if (!hasChanges)
        {
            // No actual changes, return true without database operations
            return true;
        }

        var result = await _checklistTaskRepository.UpdateAsync(existingTask);

        if (result)
        {
            // Determine which completion-related fields changed
            var completionChanged = originalTask.IsCompleted != existingTask.IsCompleted;

            // Handle cache clearing and background operations asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    // Clear caches
                    var cacheKey = $"checklist_task:get_by_id:{id}:{_userContext.AppCode}";
                    await _cacheService.RemoveAsync(cacheKey);

                    var checklistCacheKey = $"checklist_task:get_by_checklist_id:{existingTask.ChecklistId}:{_userContext.AppCode}";
                    await _cacheService.RemoveAsync(checklistCacheKey);

                    // Only recalculate completion rate if completion status changed
                    if (completionChanged)
                    {
                        await _checklistService.CalculateCompletionAsync(existingTask.ChecklistId);
                    }

                    // Log the operation - use current task data instead of additional DB query
                    var beforeData = System.Text.Json.JsonSerializer.Serialize(originalTask);
                    var afterData = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        Name = existingTask.Name,
                        Description = existingTask.Description,
                        Status = existingTask.Status,
                        Priority = existingTask.Priority,
                        IsCompleted = existingTask.IsCompleted
                    });

                    // Determine changed fields
                    var changedFields = new List<string>();
                    if (originalTask.Name != existingTask.Name) changedFields.Add("Name");
                    if (originalTask.Description != existingTask.Description) changedFields.Add("Description");
                    if (originalTask.Status != existingTask.Status) changedFields.Add("Status");
                    if (originalTask.Priority != existingTask.Priority) changedFields.Add("Priority");
                    if (originalTask.IsCompleted != existingTask.IsCompleted) changedFields.Add("IsCompleted");
                    if (originalTask.IsRequired != existingTask.IsRequired) changedFields.Add("IsRequired");
                    if (originalTask.AssigneeId != existingTask.AssigneeId) changedFields.Add("AssigneeId");
                    if (originalTask.AssigneeName != existingTask.AssigneeName) changedFields.Add("AssigneeName");
                    if (originalTask.Order != existingTask.Order) changedFields.Add("Order");
                    if (originalTask.EstimatedHours != existingTask.EstimatedHours) changedFields.Add("EstimatedHours");
                    if (originalTask.DueDate != existingTask.DueDate) changedFields.Add("DueDate");
                    if (originalTask.DependsOnTaskId != existingTask.DependsOnTaskId) changedFields.Add("DependsOnTaskId");

                    if (changedFields.Any())
                    {
                        await _operationChangeLogService.LogChecklistTaskUpdateAsync(
                            taskId: id,
                            taskName: existingTask.Name,
                            beforeData: beforeData,
                            afterData: afterData,
                            changedFields: changedFields,
                            checklistId: existingTask.ChecklistId
                        );
                    }
                }
                catch
                {
                    // Ignore errors in background operations
                }
            });
        }

        return result;
    }

    /// <summary>
    /// Delete a checklist task (with confirmation)
    /// </summary>
    public async Task<bool> DeleteAsync(long id, bool confirm = false)
    {
        if (!confirm)
        {
            throw new CRMException(ErrorCodeEnum.CustomError,
                "Delete confirmation is required for task deletion");
        }

        var task = await _checklistTaskRepository.GetByIdAsync(id);
        if (task == null)
        {
            throw new CRMException(ErrorCodeEnum.CustomError, "Task not found");
        }

        // Check if other tasks depend on this task
        var dependentTasks = await _checklistTaskRepository.GetDependentTasksAsync(id);
        if (dependentTasks.Any())
        {
            throw new CRMException(ErrorCodeEnum.CustomError,
                $"Cannot delete task that has {dependentTasks.Count} dependent tasks");
        }

        var taskName = task.Name; // Store name before deletion
        var checklistId = task.ChecklistId; // Store checklist ID before deletion

        // Soft delete
        task.IsValid = false;
        task.InitUpdateInfo(_userContext);

        var result = await _checklistTaskRepository.UpdateAsync(task);

        // Clear related cache after successful deletion
        if (result)
        {
            // Log checklist task delete operation (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _operationChangeLogService.LogChecklistTaskDeleteAsync(
                        taskId: id,
                        taskName: taskName,
                        checklistId: checklistId,
                        reason: "Checklist task deleted via admin portal"
                    );
                }
                catch
                {
                    // Ignore logging errors to avoid affecting main operation
                }
            });

            var cacheKey = $"checklist_task:get_by_id:{id}:{_userContext.AppCode}";
            await _cacheService.RemoveAsync(cacheKey);
            
            // Also clear checklist-level cache
            var checklistCacheKey = $"checklist_task:get_by_checklist_id:{task.ChecklistId}:{_userContext.AppCode}";
            await _cacheService.RemoveAsync(checklistCacheKey);
        }

        // Update checklist completion rate
        await _checklistService.CalculateCompletionAsync(task.ChecklistId);

        return result;
    }

    /// <summary>
    /// Get checklist task by ID
    /// </summary>
    public async Task<ChecklistTaskOutputDto> GetByIdAsync(long id)
    {
        var cacheKey = $"checklist_task:get_by_id:{id}";
        var cachedResult = await _cacheService.GetAsync<ChecklistTaskOutputDto>(cacheKey);
        if (cachedResult != null)
            return cachedResult;

        var task = await _checklistTaskRepository.GetByIdAsync(id);
        if (task == null)
        {
            throw new CRMException(ErrorCodeEnum.CustomError, "Task not found");
        }

        var result = _mapper.Map<ChecklistTaskOutputDto>(task);
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
        return result;
    }

    /// <summary>
    /// Get tasks by checklist ID
    /// Returns tasks with ActionId and ActionName fields populated, including files and notes count
    /// </summary>
    public async Task<List<ChecklistTaskOutputDto>> GetListByChecklistIdAsync(long checklistId)
    {
        var cacheKey = $"checklist_task:get_by_checklist_id:{checklistId}";
        var cachedResult = await _cacheService.GetAsync<List<ChecklistTaskOutputDto>>(cacheKey);
        if (cachedResult != null)
            return cachedResult;

        var tasks = await _checklistTaskRepository.GetByChecklistIdAsync(checklistId);
        var taskDtos = _mapper.Map<List<ChecklistTaskOutputDto>>(tasks);

        // Fill files and notes count for each task
        await FillFilesAndNotesCountAsync(taskDtos);

        await _cacheService.SetAsync(cacheKey, taskDtos, TimeSpan.FromMinutes(10));
        return taskDtos;
    }



    /// <summary>
    /// Complete a task
    /// </summary>
    public async Task<bool> CompleteTaskAsync(long id, CompleteTaskInputDto input)
    {
        var task = await _checklistTaskRepository.GetByIdAsync(id);
        if (task == null)
        {
            throw new CRMException(ErrorCodeEnum.CustomError, "Task not found");
        }

        if (task.IsCompleted)
        {
            throw new CRMException(ErrorCodeEnum.CustomError, "Task is already completed");
        }

        // Check if task can be completed (dependency validation)
        if (!await _checklistTaskRepository.CanCompleteAsync(id))
        {
            throw new CRMException(ErrorCodeEnum.CustomError,
                "Dependent task must be completed first");
        }

        // Get checklist to obtain onboarding and stage information
        var checklist = await _checklistRepository.GetByIdAsync(task.ChecklistId);

        task.IsCompleted = true;
        task.CompletedDate = input.CompletedDate ?? DateTimeOffset.UtcNow;
        task.CompletionNotes = input.CompletionNotes;
        task.ActualHours = input.ActualHours;
        task.Status = "Completed";
        task.InitUpdateInfo(_userContext);

        var result = await _checklistTaskRepository.UpdateAsync(task);

        // Clear related cache after successful completion
        if (result)
        {
            var cacheKey = $"checklist_task:get_by_id:{id}:{_userContext.AppCode}";
            await _cacheService.RemoveAsync(cacheKey);
            
            // Also clear checklist-level cache
            var checklistCacheKey = $"checklist_task:get_by_checklist_id:{task.ChecklistId}:{_userContext.AppCode}";
            await _cacheService.RemoveAsync(checklistCacheKey);
        }

        // Log the operation
        if (result)
        {
            // Note: OnboardingId is not available in the current Checklist entity
            // This should be passed from the calling context or retrieved from the stage/workflow relationship
            await _operationChangeLogService.LogChecklistTaskCompleteAsync(
                task.Id,
                task.Name,
                0, // Onboarding ID from context - future enhancement
                null, // StageId - assignments are now managed through Stage Components
                input.CompletionNotes,
                input.ActualHours
            );
        }

        // Update checklist completion rate
        await _checklistService.CalculateCompletionAsync(task.ChecklistId);

        return result;
    }

    /// <summary>
    /// Uncomplete a task
    /// </summary>
    public async Task<bool> UncompleteTaskAsync(long id)
    {
        var task = await _checklistTaskRepository.GetByIdAsync(id);
        if (task == null)
        {
            throw new CRMException(ErrorCodeEnum.CustomError, "Task not found");
        }

        if (!task.IsCompleted)
        {
            throw new CRMException(ErrorCodeEnum.CustomError, "Task is not completed");
        }

        // Check if there are dependent tasks that are completed
        var dependentTasks = await _checklistTaskRepository.GetDependentTasksAsync(id);
        var completedDependentTasks = dependentTasks.Where(t => t.IsCompleted).ToList();

        if (completedDependentTasks.Any())
        {
            throw new CRMException(ErrorCodeEnum.CustomError,
                $"Cannot uncomplete task because {completedDependentTasks.Count} dependent tasks are completed");
        }

        // Get checklist to obtain onboarding and stage information
        var checklist = await _checklistRepository.GetByIdAsync(task.ChecklistId);

        task.IsCompleted = false;
        task.CompletedDate = null;
        task.CompletionNotes = null;
        task.ActualHours = 0;
        task.Status = "Pending";
        task.InitUpdateInfo(_userContext);

        var result = await _checklistTaskRepository.UpdateAsync(task);

        // Clear related cache after successful uncomplete
        if (result)
        {
            var cacheKey = $"checklist_task:get_by_id:{id}:{_userContext.AppCode}";
            await _cacheService.RemoveAsync(cacheKey);
            
            // Also clear checklist-level cache
            var checklistCacheKey = $"checklist_task:get_by_checklist_id:{task.ChecklistId}:{_userContext.AppCode}";
            await _cacheService.RemoveAsync(checklistCacheKey);
        }

        // Log the operation
        if (result)
        {
            // Note: OnboardingId is not available in the current Checklist entity
            // This should be passed from the calling context or retrieved from the stage/workflow relationship
            await _operationChangeLogService.LogChecklistTaskUncompleteAsync(
                task.Id,
                task.Name,
                0, // Onboarding ID from context - future enhancement
                null, // StageId - assignments are now managed through Stage Components
                "Task marked as uncompleted"
            );
        }

        // Update checklist completion rate
        await _checklistService.CalculateCompletionAsync(task.ChecklistId);

        return result;
    }

    /// <summary>
    /// Batch complete tasks
    /// </summary>
    public async Task<bool> BatchCompleteAsync(List<long> taskIds, CompleteTaskInputDto input)
    {
        if (!taskIds.Any())
        {
            throw new CRMException(ErrorCodeEnum.CustomError, "No tasks selected for batch operation");
        }

        // Validate all tasks exist and can be completed
        var tasks = new List<ChecklistTask>();
        var checklistIds = new HashSet<long>();

        foreach (var taskId in taskIds)
        {
            var task = await _checklistTaskRepository.GetByIdAsync(taskId);
            if (task == null)
            {
                throw new CRMException(ErrorCodeEnum.CustomError, $"Task {taskId} not found");
            }

            if (task.IsCompleted)
            {
                continue; // Skip already completed tasks
            }

            if (!await _checklistTaskRepository.CanCompleteAsync(taskId))
            {
                throw new CRMException(ErrorCodeEnum.CustomError,
                    $"Task {taskId} cannot be completed due to incomplete dependencies");
            }

            tasks.Add(task);
            checklistIds.Add(task.ChecklistId);
        }

        // Batch update using repository method
        var result = await _checklistTaskRepository.BatchCompleteAsync(
            tasks.Select(t => t.Id).ToList(),
            input.CompletionNotes,
            input.ActualHours
        );

        // Log operations for each completed task
        if (result)
        {
            foreach (var task in tasks)
            {
                var checklist = await _checklistRepository.GetByIdAsync(task.ChecklistId);
                // Note: OnboardingId is not available in the current Checklist entity
                // This should be passed from the calling context or retrieved from the stage/workflow relationship
                await _operationChangeLogService.LogChecklistTaskCompleteAsync(
                    task.Id,
                    task.Name,
                    0, // Onboarding ID from context - future enhancement
                    null, // StageId - assignments are now managed through Stage Components
                    input.CompletionNotes,
                    input.ActualHours
                );
            }
        }

        // Update completion rates for affected checklists
        foreach (var checklistId in checklistIds)
        {
            await _checklistService.CalculateCompletionAsync(checklistId);
        }

        return result;
    }

    /// <summary>
    /// Sort tasks within checklist
    /// </summary>
    public async Task<bool> SortTasksAsync(long checklistId, Dictionary<long, int> taskOrders)
    {
        // Validate checklist exists
        var checklist = await _checklistRepository.GetByIdAsync(checklistId);
        if (checklist == null)
        {
            throw new CRMException(ErrorCodeEnum.CustomError, "Checklist not found");
        }

        // Validate all tasks belong to the checklist
        var tasks = await _checklistTaskRepository.GetByChecklistIdAsync(checklistId);
        var existingTaskIds = tasks.Select(t => t.Id).ToHashSet();

        foreach (var taskId in taskOrders.Keys)
        {
            if (!existingTaskIds.Contains(taskId))
            {
                throw new CRMException(ErrorCodeEnum.CustomError,
                    $"Task {taskId} does not belong to checklist {checklistId}");
            }
        }

        var result = await _checklistTaskRepository.UpdateOrderAsync(checklistId, taskOrders);
        
        // Clear related cache after successful order update
        if (result)
        {
            var cacheKey = $"checklist_task:get_by_checklist_id:{checklistId}:{_userContext.AppCode}";
            await _cacheService.RemoveAsync(cacheKey);
        }
        
        return result;
    }

    /// <summary>
    /// Assign task to user
    /// </summary>
    public async Task<bool> AssignTaskAsync(long id, long assigneeId, string assigneeName)
    {
        var task = await _checklistTaskRepository.GetByIdAsync(id);
        if (task == null)
        {
            throw new CRMException(ErrorCodeEnum.CustomError, "Task not found");
        }

        task.AssigneeId = assigneeId;
        task.AssigneeName = assigneeName;
        task.ModifyDate = DateTimeOffset.UtcNow;

        var result = await _checklistTaskRepository.UpdateAsync(task);
        
        // Clear related cache after successful assignment
        if (result)
        {
            var cacheKey = $"checklist_task:get_by_id:{id}:{_userContext.AppCode}";
            await _cacheService.RemoveAsync(cacheKey);
            
            // Also clear checklist-level cache
            var checklistCacheKey = $"checklist_task:get_by_checklist_id:{task.ChecklistId}:{_userContext.AppCode}";
            await _cacheService.RemoveAsync(checklistCacheKey);
        }
        
        return result;
    }

    /// <summary>
    /// Set structured assignee information for task (configuration stage)
    /// </summary>
    public async Task<bool> SetTaskAssigneeAsync(long id, AssigneeDto assignee)
    {
        var task = await _checklistTaskRepository.GetByIdAsync(id);
        if (task == null)
        {
            throw new CRMException(ErrorCodeEnum.CustomError, "Task not found");
        }

        // Update both traditional fields and structured JSON field
        if (assignee != null)
        {
            task.AssigneeId = assignee.UserId;
            task.AssigneeName = assignee.Name;
            task.AssignedTeam = assignee.Team;

            // Serialize to JSON with proper options
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            task.AssigneeJson = System.Text.Json.JsonSerializer.Serialize(assignee, options);
        }
        else
        {
            // Clear all assignee information
            task.AssigneeId = null;
            task.AssigneeName = null;
            task.AssignedTeam = null;
            task.AssigneeJson = null;
        }

        task.InitUpdateInfo(_userContext);

        var result = await _checklistTaskRepository.UpdateAsync(task);
        
        // Clear related cache after successful assignee update
        if (result)
        {
            var cacheKey = $"checklist_task:get_by_id:{id}:{_userContext.AppCode}";
            await _cacheService.RemoveAsync(cacheKey);
            
            // Also clear checklist-level cache
            var checklistCacheKey = $"checklist_task:get_by_checklist_id:{task.ChecklistId}:{_userContext.AppCode}";
            await _cacheService.RemoveAsync(checklistCacheKey);
        }
        
        return result;
    }

    /// <summary>
    /// Get pending tasks by assignee
    /// </summary>
    public async Task<List<ChecklistTaskOutputDto>> GetPendingTasksByAssigneeAsync(long assigneeId)
    {
        var tasks = await _checklistTaskRepository.GetPendingTasksByAssigneeAsync(assigneeId);
        return _mapper.Map<List<ChecklistTaskOutputDto>>(tasks);
    }

    /// <summary>
    /// Get overdue tasks
    /// </summary>
    public async Task<List<ChecklistTaskOutputDto>> GetOverdueTasksAsync()
    {
        var tasks = await _checklistTaskRepository.GetOverdueTasksAsync();
        return _mapper.Map<List<ChecklistTaskOutputDto>>(tasks);
    }

    /// <summary>
    /// Get tasks by action ID
    /// </summary>
    public async Task<List<ChecklistTaskOutputDto>> GetTasksByActionIdAsync(long actionId)
    {
        var tasks = await _checklistTaskRepository.GetTasksByActionIdAsync(actionId);
        return _mapper.Map<List<ChecklistTaskOutputDto>>(tasks);
    }

    /// <summary>
    /// Get next order number for checklist
    /// </summary>
    private async Task<int> GetNextOrderAsync(long checklistId)
    {
        var tasks = await _checklistTaskRepository.GetByChecklistIdAsync(checklistId);
        return tasks.Any() ? tasks.Max(t => t.Order) + 1 : 1;
    }

    /// <summary>
    /// Check for circular dependency
    /// </summary>
    private async Task<bool> HasCircularDependencyAsync(long taskId, long dependsOnTaskId)
    {
        var visited = new HashSet<long>();
        var recursionStack = new HashSet<long>();
        return await HasCircularDependencyRecursiveAsync(taskId, dependsOnTaskId, visited, recursionStack);
    }

    private async Task<bool> HasCircularDependencyRecursiveAsync(
        long originalTaskId,
        long currentTaskId,
        HashSet<long> visited,
        HashSet<long> recursionStack)
    {
        if (currentTaskId == originalTaskId)
        {
            return true; // Circular dependency found
        }

        if (recursionStack.Contains(currentTaskId))
        {
            return true; // Cycle detected
        }

        if (visited.Contains(currentTaskId))
        {
            return false; // Already processed this path
        }

        visited.Add(currentTaskId);
        recursionStack.Add(currentTaskId);

        var task = await _checklistTaskRepository.GetByIdAsync(currentTaskId);
        if (task?.DependsOnTaskId.HasValue == true)
        {
            if (await HasCircularDependencyRecursiveAsync(originalTaskId, task.DependsOnTaskId.Value, visited, recursionStack))
            {
                return true;
            }
        }

        recursionStack.Remove(currentTaskId);
        return false;
    }

    /// <summary>
    /// Fill files and notes count for task DTOs
    /// </summary>
    private async Task FillFilesAndNotesCountAsync(List<ChecklistTaskOutputDto> taskDtos)
    {
        if (taskDtos == null || !taskDtos.Any())
            return;

        var taskIds = taskDtos.Select(t => t.Id).ToList();

        // Get files count from ChecklistTaskCompletion
        var completions = await _completionRepository.GetByTaskIdsAsync(taskIds);
        var filesCountMap = new Dictionary<long, int>();

        foreach (var completion in completions)
        {
            var filesCount = GetFilesCountFromJson(completion.FilesJson);
            if (filesCountMap.ContainsKey(completion.TaskId))
            {
                filesCountMap[completion.TaskId] += filesCount;
            }
            else
            {
                filesCountMap[completion.TaskId] = filesCount;
            }
        }

        // Get notes count from ChecklistTaskNote
        var notesCountMap = await GetNotesCountByTaskIdsAsync(taskIds);

        // Fill the counts in DTOs
        foreach (var taskDto in taskDtos)
        {
            taskDto.FilesCount = filesCountMap.GetValueOrDefault(taskDto.Id, 0);
            taskDto.NotesCount = notesCountMap.GetValueOrDefault(taskDto.Id, 0);
        }
    }

    /// <summary>
    /// Get notes count by task IDs
    /// </summary>
    private async Task<Dictionary<long, int>> GetNotesCountByTaskIdsAsync(List<long> taskIds)
    {
        var notesCountMap = new Dictionary<long, int>();

        foreach (var taskId in taskIds)
        {
            var count = await _noteRepository.CountByTaskIdAsync(taskId);
            notesCountMap[taskId] = count;
        }

        return notesCountMap;
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
}