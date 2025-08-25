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
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Domain.Shared.Enums.Action;
using Newtonsoft.Json.Linq;

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
    private readonly IActionManagementService _actionManagementService;
    private readonly IMapper _mapper;
    private readonly UserContext _userContext;

    public ChecklistTaskService(
        IChecklistTaskRepository checklistTaskRepository,
        IChecklistRepository checklistRepository,
        IChecklistService checklistService,
        IOperationChangeLogService operationChangeLogService,
        IActionManagementService actionManagementService,
        IMapper mapper,
        UserContext userContext)
    {
        _checklistTaskRepository = checklistTaskRepository;
        _checklistRepository = checklistRepository;
        _checklistService = checklistService;
        _operationChangeLogService = operationChangeLogService;
        _actionManagementService = actionManagementService;
        _mapper = mapper;
        _userContext = userContext;
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

        // Set default values
        entity.Order = input.Order > 0 ? input.Order : await GetNextOrderAsync(input.ChecklistId);
        entity.Status = string.IsNullOrEmpty(entity.Status) ? "Pending" : entity.Status;
        entity.IsCompleted = false;

        // Handle Action creation and ActionTriggerMapping
        await HandleActionCreationAsync(entity, input, checklist);

        // Initialize create information with proper ID and timestamps
        entity.InitCreateInfo(_userContext);

        await _checklistTaskRepository.InsertAsync(entity);

        // Create ActionTriggerMapping after task insertion (if Action was created)
        if (entity.ActionId.HasValue)
        {
            await CreateActionTriggerMappingAsync(entity, checklist);
        }

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

        // Handle Action update
        var checklist = await _checklistRepository.GetByIdAsync(existingTask.ChecklistId);
        await HandleActionUpdateAsync(existingTask, input, checklist);

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
    /// Returns tasks with ActionId and ActionName fields populated
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
            task.CompletedDate = input.CompletedDate ?? DateTimeOffset.UtcNow;
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
            task.ModifyDate = DateTimeOffset.UtcNow;

        return await _checklistTaskRepository.UpdateAsync(task);
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
    /// Handle Action creation and ActionTriggerMapping for ChecklistTask
    /// If ActionName is provided but ActionId is null, creates a new Action and ActionTriggerMapping
    /// </summary>
    private async Task HandleActionCreationAsync(ChecklistTask entity, ChecklistTaskInputDto input, Checklist checklist)
    {
        // If ActionName is provided but ActionId is null, create a new Action
        if (!string.IsNullOrWhiteSpace(input.ActionName) && !input.ActionId.HasValue)
        {
            try
            {
                // Create ActionDefinition with default Python action configuration
                var createActionDto = new CreateActionDefinitionDto
                {
                    Name = input.ActionName,
                    Description = $"Auto-generated action for task: {input.Name}",
                    ActionType = ActionTypeEnum.Python,
                    ActionConfig = CreateDefaultPythonActionConfig(input, checklist),
                    IsEnabled = true
                };

                var actionDefinition = await _actionManagementService.CreateActionDefinitionAsync(createActionDto);
                
                // Update entity with created Action information
                entity.ActionId = actionDefinition.Id;
                entity.ActionName = actionDefinition.Name;

                // Note: ActionTriggerMapping will be created after task is inserted
                // since we need the TaskId for TriggerSourceId
            }
            catch (Exception ex)
            {
                // Log error but don't fail task creation
                // Task will be created without Action linkage
                Console.WriteLine($"Failed to create action for task '{input.Name}': {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Create ActionTriggerMapping after task insertion
    /// </summary>
    private async Task CreateActionTriggerMappingAsync(ChecklistTask entity, Checklist checklist)
    {
        if (!entity.ActionId.HasValue) return;

        try
        {
            // Get stage and workflow information from checklist
            // Note: Since checklists are now component-based, we need to find the associated stage
            // This is a simplified approach - in a real scenario, you'd need to determine 
            // the correct workflow and stage from the component mapping
            
            var createMappingDto = new CreateActionTriggerMappingDto
            {
                ActionDefinitionId = entity.ActionId.Value,
                TriggerType = "Task",
                TriggerSourceId = entity.Id,
                WorkFlowId = 0, // This should be determined from context
                StageId = 0,    // This should be determined from context  
                TriggerEvent = "Completed",
                ExecutionOrder = 1,
                IsEnabled = true
            };

            await _actionManagementService.CreateActionTriggerMappingAsync(createMappingDto);
        }
        catch (Exception ex)
        {
            // Log error but don't fail task creation
            Console.WriteLine($"Failed to create action trigger mapping for task '{entity.Name}': {ex.Message}");
        }
    }

    /// <summary>
    /// Create default Python action configuration for a task
    /// </summary>
    private string CreateDefaultPythonActionConfig(ChecklistTaskInputDto input, Checklist checklist)
    {
        var config = new
        {
            sourceCode = $@"def main(context_data):
    """"""
    Auto-generated action for task: {input.Name}
    Description: {input.Description ?? "No description provided"}
    """"""
    
    # Extract task information from context
    task_id = context_data.get('TaskId')
    task_name = context_data.get('TaskName', '{input.Name}')
    checklist_name = context_data.get('ChecklistName', '{checklist?.Name ?? "Unknown"}')
    
    print(f'Executing action for task: {{task_name}} (ID: {{task_id}})')
    print(f'Checklist: {{checklist_name}}')
    
    # TODO: Add your custom logic here
    # You can access all context data passed from the trigger event
    
    return {{
        'success': True,
        'message': f'Action completed for task: {{task_name}}',
        'task_id': task_id
    }}",
            timeout = 30,
            environmentVariables = new { },
            requirements = new string[] { }
        };

        return System.Text.Json.JsonSerializer.Serialize(config);
    }

    /// <summary>
    /// Handle Action update for ChecklistTask
    /// If ActionName is provided but ActionId is null, creates a new Action and ActionTriggerMapping
    /// If ActionName is changed for existing Action, updates the Action name
    /// If ActionId exists but no ActionTriggerMapping, creates the mapping
    /// </summary>
    private async Task HandleActionUpdateAsync(ChecklistTask existingTask, ChecklistTaskInputDto input, Checklist checklist)
    {
        // If ActionName is provided but ActionId is null, create new Action
        if (!string.IsNullOrWhiteSpace(input.ActionName) && !input.ActionId.HasValue)
        {
            try
            {
                var createActionDto = new CreateActionDefinitionDto
                {
                    Name = input.ActionName,
                    Description = $"Auto-generated action for task: {input.Name}",
                    ActionType = ActionTypeEnum.Python,
                    ActionConfig = CreateDefaultPythonActionConfig(input, checklist),
                    IsEnabled = true
                };

                var actionDefinition = await _actionManagementService.CreateActionDefinitionAsync(createActionDto);
                
                // Update task with created Action information
                existingTask.ActionId = actionDefinition.Id;
                existingTask.ActionName = actionDefinition.Name;

                // Create ActionTriggerMapping
                await CreateActionTriggerMappingAsync(existingTask, checklist);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create action for task '{input.Name}': {ex.Message}");
            }
        }
        // If ActionName is changed for existing Action, update Action name
        else if (!string.IsNullOrWhiteSpace(input.ActionName) && 
                 input.ActionId.HasValue && 
                 existingTask.ActionName != input.ActionName)
        {
            try
            {
                var updateActionDto = new UpdateActionDefinitionDto
                {
                    Name = input.ActionName,
                    Description = $"Updated action for task: {input.Name}",
                    ActionType = ActionTypeEnum.Python,
                    ActionConfig = CreateDefaultPythonActionConfig(input, checklist),
                    IsEnabled = true
                };

                await _actionManagementService.UpdateActionDefinitionAsync(input.ActionId.Value, updateActionDto);
                existingTask.ActionName = input.ActionName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update action for task '{input.Name}': {ex.Message}");
            }
        }

        // Check if ActionId exists but no ActionTriggerMapping exists, create the mapping
        if (input.ActionId.HasValue || existingTask.ActionId.HasValue)
        {
            var actionId = input.ActionId ?? existingTask.ActionId;
            if (actionId.HasValue)
            {
                await EnsureActionTriggerMappingExistsAsync(existingTask, actionId.Value, checklist);
            }
        }
    }

    /// <summary>
    /// Ensure ActionTriggerMapping exists for the given task and action
    /// </summary>
    private async Task EnsureActionTriggerMappingExistsAsync(ChecklistTask task, long actionId, Checklist checklist)
    {
        try
        {
            // Check if ActionTriggerMapping already exists
            var existingMappings = await _actionManagementService.GetActionTriggerMappingsByTriggerSourceIdAsync(task.Id);
            var hasMapping = existingMappings.Any(m => m.ActionDefinitionId == actionId && 
                                                      m.TriggerType == "Task" && 
                                                      m.TriggerSourceId == task.Id);
            
            if (!hasMapping)
            {
                Console.WriteLine($"Creating missing ActionTriggerMapping for Task {task.Id} and Action {actionId}");
                
                // Set the ActionId for CreateActionTriggerMappingAsync
                task.ActionId = actionId;
                await CreateActionTriggerMappingAsync(task, checklist);
            }
            else
            {
                Console.WriteLine($"ActionTriggerMapping already exists for Task {task.Id} and Action {actionId}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to ensure ActionTriggerMapping exists for task '{task.Name}': {ex.Message}");
        }
    }
}