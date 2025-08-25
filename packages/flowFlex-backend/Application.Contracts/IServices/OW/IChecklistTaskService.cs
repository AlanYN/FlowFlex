using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Checklist;
using FlowFlex.Application.Contracts.Dtos.OW.ChecklistTask;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW;

/// <summary>
/// ChecklistTask service interface
/// </summary>
public interface IChecklistTaskService : IScopedService
{
    /// <summary>
    /// Create a new checklist task
    /// </summary>
    Task<long> CreateAsync(ChecklistTaskInputDto input);

    /// <summary>
    /// Update an existing checklist task
    /// </summary>
    Task<bool> UpdateAsync(long id, ChecklistTaskInputDto input);

    /// <summary>
    /// Delete a checklist task (with confirmation)
    /// </summary>
    Task<bool> DeleteAsync(long id, bool confirm = false);

    /// <summary>
    /// Get checklist task by ID
    /// </summary>
    Task<ChecklistTaskOutputDto> GetByIdAsync(long id);

    /// <summary>
    /// Get tasks by checklist ID
    /// </summary>
    Task<List<ChecklistTaskOutputDto>> GetListByChecklistIdAsync(long checklistId);

    /// <summary>
    /// Complete a task
    /// </summary>
    Task<bool> CompleteTaskAsync(long id, CompleteTaskInputDto input);

    /// <summary>
    /// Uncomplete a task
    /// </summary>
    Task<bool> UncompleteTaskAsync(long id);

    /// <summary>
    /// Batch complete tasks
    /// </summary>
    Task<bool> BatchCompleteAsync(List<long> taskIds, CompleteTaskInputDto input);

    /// <summary>
    /// Sort tasks within checklist
    /// </summary>
    Task<bool> SortTasksAsync(long checklistId, Dictionary<long, int> taskOrders);

    /// <summary>
    /// Assign task to user
    /// </summary>
    Task<bool> AssignTaskAsync(long id, long assigneeId, string assigneeName);

    /// <summary>
    /// Set structured assignee information for task (configuration stage)
    /// </summary>
    Task<bool> SetTaskAssigneeAsync(long id, AssigneeDto assignee);

    /// <summary>
    /// Get pending tasks by assignee
    /// </summary>
    Task<List<ChecklistTaskOutputDto>> GetPendingTasksByAssigneeAsync(long assigneeId);

    /// <summary>
    /// Get overdue tasks
    /// </summary>
    Task<List<ChecklistTaskOutputDto>> GetOverdueTasksAsync();

    /// <summary>
    /// Get tasks by action ID
    /// </summary>
    Task<List<ChecklistTaskOutputDto>> GetTasksByActionIdAsync(long actionId);
}
