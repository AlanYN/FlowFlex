using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
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
    private readonly IMapper _mapper;
    private readonly UserContext _userContext;

    public ChecklistTaskService(
        IChecklistTaskRepository checklistTaskRepository,
        IChecklistRepository checklistRepository,
        IChecklistService checklistService,
        IOperationChangeLogService operationChangeLogService,
        IMapper mapper,
        UserContext userContext)
    {
        _checklistTaskRepository = checklistTaskRepository;
        _checklistRepository = checklistRepository;
        _checklistService = checklistService;
        _operationChangeLogService = operationChangeLogService;
        _mapper = mapper;
        _userContext = userContext;
    }

    /// <summary>
    /// Create a new checklist task
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

        // Set default values
        entity.Order = input.Order > 0 ? input.Order : await GetNextOrderAsync(input.ChecklistId);
        entity.Status = string.IsNullOrEmpty(entity.Status) ? "Pending" : entity.Status;
        entity.IsCompleted = false;

        // Initialize create information with proper ID and timestamps
        entity.InitCreateInfo(_userContext);

        await _checklistTaskRepository.InsertAsync(entity);

        // Update checklist completion rate
        await _checklistService.CalculateCompletionAsync(input.ChecklistId);

        return entity.Id;
    }

    /// <summary>
    /// Update an existing checklist task
    /// </summary>
    public async Task<bool> UpdateAsync(long id, ChecklistTaskInputDto input)
    {
        var existingTask = await _checklistTaskRepository.GetByIdAsync(id);
        if (existingTask == null)
        {
            throw new CRMException(ErrorCodeEnum.NotFound, "Task not found");
        }

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

        var result = await _checklistTaskRepository.UpdateAsync(existingTask);

        // Update checklist completion rate
        await _checklistService.CalculateCompletionAsync(existingTask.ChecklistId);

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

        // Soft delete
        task.IsValid = false;
        task.InitUpdateInfo(_userContext);

        var result = await _checklistTaskRepository.UpdateAsync(task);

        // Update checklist completion rate
        await _checklistService.CalculateCompletionAsync(task.ChecklistId);

        return result;
    }

    /// <summary>
    /// Get checklist task by ID
    /// </summary>
    public async Task<ChecklistTaskOutputDto> GetByIdAsync(long id)
    {
        var task = await _checklistTaskRepository.GetByIdAsync(id);
        if (task == null)
        {
            throw new CRMException(ErrorCodeEnum.CustomError, "Task not found");
        }

        return _mapper.Map<ChecklistTaskOutputDto>(task);
    }

    /// <summary>
    /// Get tasks by checklist ID
    /// </summary>
    public async Task<List<ChecklistTaskOutputDto>> GetListByChecklistIdAsync(long checklistId)
    {
        var tasks = await _checklistTaskRepository.GetByChecklistIdAsync(checklistId);
        return _mapper.Map<List<ChecklistTaskOutputDto>>(tasks);
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
        task.CompletedDate = input.CompletedDate ?? DateTimeOffset.Now;
        task.CompletionNotes = input.CompletionNotes;
        task.ActualHours = input.ActualHours;
        task.Status = "Completed";
        task.InitUpdateInfo(_userContext);

        var result = await _checklistTaskRepository.UpdateAsync(task);

        // Log the operation
        if (result)
        {
            // Note: OnboardingId is not available in the current Checklist entity
            // This should be passed from the calling context or retrieved from the stage/workflow relationship
            await _operationChangeLogService.LogChecklistTaskCompleteAsync(
                task.Id,
                task.Name,
                0, // Onboarding ID from context - future enhancement
                checklist?.Assignments?.FirstOrDefault()?.StageId,
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

        // Log the operation
        if (result)
        {
            // Note: OnboardingId is not available in the current Checklist entity
            // This should be passed from the calling context or retrieved from the stage/workflow relationship
            await _operationChangeLogService.LogChecklistTaskUncompleteAsync(
                task.Id,
                task.Name,
                0, // Onboarding ID from context - future enhancement
                checklist?.Assignments?.FirstOrDefault()?.StageId,
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
                    checklist?.Assignments?.FirstOrDefault()?.StageId,
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

        return await _checklistTaskRepository.UpdateOrderAsync(checklistId, taskOrders);
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
        task.ModifyDate = DateTimeOffset.Now;

        return await _checklistTaskRepository.UpdateAsync(task);
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
}