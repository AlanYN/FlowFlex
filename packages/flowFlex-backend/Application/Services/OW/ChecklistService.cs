using System.IO;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.Checklist;
using FlowFlex.Application.Contracts.Dtos.OW.ChecklistTask;
using FlowFlex.Application.Contracts.Dtos.OW.Common;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Exceptions;
using System.Linq.Expressions;
using SqlSugar;
using System.Diagnostics;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Repository.OW;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowFlex.Application.Services.OW.Extensions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace FlowFlex.Application.Service.OW;

/// <summary>
/// Checklist service implementation
/// </summary>
public class ChecklistService : IChecklistService, IScopedService
{
    private readonly IChecklistRepository _checklistRepository;
    private readonly IChecklistTaskRepository _checklistTaskRepository;
    private readonly IMapper _mapper;
    private readonly IStageAssignmentSyncService _syncService;
    private readonly UserContext _userContext;

    public ChecklistService(
        IChecklistRepository checklistRepository,
        IChecklistTaskRepository checklistTaskRepository,
        IMapper mapper,
        IStageAssignmentSyncService syncService,
        UserContext userContext)
    {
        _checklistRepository = checklistRepository;
        _checklistTaskRepository = checklistTaskRepository;
        _mapper = mapper;
        _syncService = syncService;
        _userContext = userContext;
    }

    /// <summary>
    /// Create a new checklist
    /// </summary>
    public async Task<long> CreateAsync(ChecklistInputDto input)
    {
        if (input == null)
        {
            throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input cannot be null");
        }

        // Validate name uniqueness
        if (await _checklistRepository.IsNameExistsAsync(input.Name, input.Team))
        {
            throw new CRMException(ErrorCodeEnum.BusinessError, $"Checklist name '{input.Name}' already exists");
        }

        // Create a single checklist entity
        var entity = _mapper.Map<Checklist>(input);

        // Handle assignments - store all assignments in JSON field
        if (input.Assignments != null && input.Assignments.Any())
        {
            try
            {
                // Convert Application.Contracts.AssignmentDto to Domain.AssignmentDto
                // Filter out assignments with invalid WorkflowId, but allow null StageId
                entity.Assignments = input.Assignments
                    .Where(a => a.WorkflowId > 0)
                    .Select(a => new Domain.Entities.OW.AssignmentDto
                    {
                        WorkflowId = a.WorkflowId,
                        StageId = a.StageId ?? 0 // Use 0 for null StageId
                    }).ToList();

                Console.WriteLine($"Created checklist with {entity.Assignments.Count} valid assignments out of {input.Assignments.Count} total assignments");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing assignments during creation: {ex.Message}");
                // Log the raw assignment data for debugging
                for (int i = 0; i < input.Assignments.Count; i++)
                {
                    var assignment = input.Assignments[i];
                    Console.WriteLine($"Assignment {i}: WorkflowId={assignment.WorkflowId}, StageId={assignment.StageId}");
                }

                // Initialize empty assignments if processing fails
                entity.Assignments = new List<Domain.Entities.OW.AssignmentDto>();
            }
        }
        else
        {
            // Initialize empty assignments if none provided
            entity.Assignments = new List<Domain.Entities.OW.AssignmentDto>();
        }

        // Initialize create information with proper ID and timestamps
        entity.InitCreateInfo(_userContext);

        await _checklistRepository.InsertAsync(entity);

        return entity.Id;
    }

    /// <summary>
    /// Update an existing checklist
    /// </summary>
    public async Task<bool> UpdateAsync(long id, ChecklistInputDto input)
    {
        if (input == null)
        {
            throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input cannot be null");
        }

        var entity = await _checklistRepository.GetByIdAsync(id);
        if (entity == null)
        {
            throw new CRMException(ErrorCodeEnum.NotFound, $"Checklist with ID {id} not found");
        }

        // Store the original name for cleanup
        var originalName = entity.Name;

        // Validate name uniqueness (exclude current record)
        if (await _checklistRepository.IsNameExistsAsync(input.Name, input.Team, id))
        {
            throw new CRMException(ErrorCodeEnum.BusinessError, $"Checklist name '{input.Name}' already exists");
        }

        // Clean up any duplicate checklists with the same name (legacy data cleanup)
        // This handles cases where the old implementation created multiple entities
        var existingChecklists = await _checklistRepository.GetByNameAsync(originalName);
        var duplicatesToDelete = existingChecklists.Where(c => c.Id != id).ToList();

        foreach (var duplicate in duplicatesToDelete)
        {
            // Soft delete duplicate checklists
            duplicate.IsValid = false;
            duplicate.ModifyDate = DateTimeOffset.Now;
            await _checklistRepository.UpdateAsync(duplicate);
        }

        // If the name is changing, also clean up any existing checklists with the new name
        if (originalName != input.Name)
        {
            var existingChecklistsWithNewName = await _checklistRepository.GetByNameAsync(input.Name);
            var newNameDuplicates = existingChecklistsWithNewName.Where(c => c.Id != id).ToList();

            foreach (var duplicate in newNameDuplicates)
            {
                // Soft delete existing checklists with the new name
                duplicate.IsValid = false;
                duplicate.ModifyDate = DateTimeOffset.Now;
                await _checklistRepository.UpdateAsync(duplicate);
            }
        }

        // Store old assignments for sync comparison
        var oldAssignments = entity.Assignments?.Where(a => a.StageId > 0)
            .Select(a => (a.WorkflowId, a.StageId))
            .ToList() ?? new List<(long, long)>();

        // Update the checklist entity
        _mapper.Map(input, entity);

        // Handle assignments - store all assignments in JSON field
        if (input.Assignments != null && input.Assignments.Any())
        {
            try
            {
                // Convert Application.Contracts.AssignmentDto to Domain.AssignmentDto
                // Filter out assignments with invalid WorkflowId, but allow null StageId
                entity.Assignments = input.Assignments
                    .Where(a => a.WorkflowId > 0)
                    .Select(a => new Domain.Entities.OW.AssignmentDto
                    {
                        WorkflowId = a.WorkflowId,
                        StageId = a.StageId ?? 0 // Use 0 for null StageId
                    }).ToList();

                Console.WriteLine($"Processed {entity.Assignments.Count} valid assignments out of {input.Assignments.Count} total assignments");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing assignments: {ex.Message}");
                // Log the raw assignment data for debugging
                for (int i = 0; i < input.Assignments.Count; i++)
                {
                    var assignment = input.Assignments[i];
                    Console.WriteLine($"Assignment {i}: WorkflowId={assignment.WorkflowId}, StageId={assignment.StageId}");
                }

                // Initialize empty assignments if processing fails
                entity.Assignments = new List<Domain.Entities.OW.AssignmentDto>();
            }
        }
        else
        {
            // Initialize empty assignments if none provided
            entity.Assignments = new List<Domain.Entities.OW.AssignmentDto>();
        }

        // Get new assignments for sync comparison (only valid stage assignments)
        var newAssignments = entity.Assignments?.Where(a => a.StageId > 0)
            .Select(a => (a.WorkflowId, a.StageId))
            .ToList() ?? new List<(long, long)>();

        // Initialize update information with proper timestamps
        entity.InitUpdateInfo(_userContext);

        var result = await _checklistRepository.UpdateAsync(entity);

        // Sync with stage components if update was successful
        if (result)
        {
            try
            {
                await _syncService.SyncStageComponentsFromChecklistAssignmentsAsync(
                    id,
                    oldAssignments,
                    newAssignments);
            }
            catch (Exception ex)
            {
                // Log sync error but don't fail the operation
                Console.WriteLine($"Failed to sync stage components for checklist {id}: {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// Delete a checklist (with confirmation)
    /// </summary>
    public async Task<bool> DeleteAsync(long id, bool confirm = false)
    {
        var checklist = await _checklistRepository.GetByIdAsync(id);
        if (checklist == null)
        {
            throw new CRMException(ErrorCodeEnum.CustomError, "Checklist not found");
        }

        if (!confirm)
        {
            throw new CRMException(ErrorCodeEnum.CustomError, "Please confirm deletion by setting confirm=true");
        }

        // Soft delete
        checklist.IsValid = false;
        checklist.ModifyDate = DateTimeOffset.Now;

        var result = await _checklistRepository.UpdateAsync(checklist);

        // Cache has been removed, no cleanup needed

        return result;
    }

    /// <summary>
    /// Get checklist by ID
    /// </summary>
    public async Task<ChecklistOutputDto> GetByIdAsync(long id)
    {
        var checklist = await _checklistRepository.GetByIdAsync(id);
        if (checklist == null)
        {
            throw new CRMException(ErrorCodeEnum.CustomError, "Checklist not found");
        }

        var result = _mapper.Map<ChecklistOutputDto>(checklist);

        // Fill assignments and tasks for the checklist
        await FillAssignmentsAndTasksAsync(new List<ChecklistOutputDto> { result });

        return result;
    }

    /// <summary>
    /// Get list of checklists by team
    /// </summary>
    public async Task<List<ChecklistOutputDto>> GetListAsync(string team = null)
    {
        var checklists = await _checklistRepository.GetByTeamAsync(team);
        var result = _mapper.Map<List<ChecklistOutputDto>>(checklists);

        // Fill assignments for the checklists
        await FillAssignmentsAsync(result);

        return result;
    }

    /// <summary>
    /// Get checklists by multiple IDs (batch query)
    /// </summary>
    public async Task<List<ChecklistOutputDto>> GetByIdsAsync(List<long> ids)
    {
        if (ids == null || !ids.Any())
        {
            return new List<ChecklistOutputDto>();
        }

        var checklists = await _checklistRepository.GetByIdsAsync(ids);
        var result = _mapper.Map<List<ChecklistOutputDto>>(checklists);

        // Fill assignments and tasks for the checklists
        await FillAssignmentsAndTasksAsync(result);

        return result;
    }

    /// <summary>
    /// Query checklists (paged)
    /// </summary>
    public async Task<PagedResult<ChecklistOutputDto>> QueryAsync(ChecklistQueryRequest query)
    {
        var (items, totalCount) = await _checklistRepository.GetPagedAsync(
            query.PageIndex,
            query.PageSize,
            query.Name,
            query.Team,
            query.Type,
            query.Status,
            query.IsTemplate,
            query.IsActive,
            query.WorkflowId,
            query.StageId,
            query.SortField,
            query.SortDirection
        );

        var dtoItems = _mapper.Map<List<ChecklistOutputDto>>(items);

        // Fill assignments for the checklists
        await FillAssignmentsAsync(dtoItems);

        return new PagedResult<ChecklistOutputDto>
        {
            Items = dtoItems,
            TotalCount = totalCount,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
    }

    /// <summary>
    /// Duplicate a checklist
    /// </summary>
    public async Task<long> DuplicateAsync(long id, DuplicateChecklistInputDto input)
    {
        var sourceChecklist = await _checklistRepository.GetByIdAsync(id);
        if (sourceChecklist == null)
        {
            throw new CRMException(ErrorCodeEnum.CustomError, "Source checklist not found");
        }

        // Validate name uniqueness
        if (await _checklistRepository.IsNameExistsAsync(input.Name, input.TargetTeam ?? sourceChecklist.Team))
        {
            throw new CRMException(ErrorCodeEnum.CustomError,
                $"Checklist name '{input.Name}' already exists");
        }

        // Create new checklist with assignments copied
        var newChecklist = new Checklist
        {
            Name = input.Name,
            Description = input.Description ?? sourceChecklist.Description,
            Team = input.TargetTeam ?? sourceChecklist.Team,
            Type = sourceChecklist.Type,
            Status = "Active",
            IsTemplate = input.SetAsTemplate,
            TemplateId = sourceChecklist.IsTemplate ? sourceChecklist.Id : sourceChecklist.TemplateId,
            EstimatedHours = sourceChecklist.EstimatedHours,
            IsActive = true,
            // Copy assignments from source checklist
            AssignmentsJson = sourceChecklist.AssignmentsJson,
            // Copy tenant and app information from source checklist
            TenantId = sourceChecklist.TenantId,
            AppCode = sourceChecklist.AppCode
        };

        // Initialize create information with proper ID and timestamps
        newChecklist.InitCreateInfo(_userContext);

        var newChecklistId = await _checklistRepository.InsertReturnSnowflakeIdAsync(newChecklist);

        // Copy tasks if requested
        if (input.CopyTasks)
        {
            var sourceTasks = await _checklistTaskRepository.GetByChecklistIdAsync(sourceChecklist.Id);
            if (sourceTasks?.Any() == true)
            {
                var newTasks = sourceTasks.Select(task => new ChecklistTask
                {
                    ChecklistId = newChecklistId,
                    Name = task.Name,
                    Description = task.Description,
                    TaskType = task.TaskType,
                    IsRequired = task.IsRequired,
                    AssignedTeam = input.TargetTeam ?? task.AssignedTeam,
                    Priority = task.Priority,
                    Order = task.Order,
                    EstimatedHours = task.EstimatedHours,
                    DueDate = task.DueDate,
                    AttachmentsJson = task.AttachmentsJson,
                    Status = "Pending",
                    IsActive = true,
                    // Copy tenant and app information from source task
                    TenantId = task.TenantId,
                    AppCode = task.AppCode
                }).ToList();

                // Initialize create info for each task
                foreach (var task in newTasks)
                {
                    task.InitCreateInfo(_userContext);
                }

                await _checklistTaskRepository.InsertRangeAsync(newTasks);

                // Update completion statistics
                await CalculateCompletionAsync(newChecklistId);
            }
        }

        // Cache has been removed, no cleanup needed

        return newChecklistId;
    }

    /// <summary>
    /// Export checklist to PDF
    /// </summary>
    public async Task<Stream> ExportToPdfAsync(long id)
    {
        var checklist = await _checklistRepository.GetByIdAsync(id);
        if (checklist == null)
        {
            throw new CRMException(ErrorCodeEnum.CustomError, "Checklist not found");
        }

        // PDF generation feature - future enhancement
        // This is a placeholder implementation
        var content = GeneratePdfContent(checklist);
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
        await writer.FlushAsync();
        stream.Position = 0;

        return stream;
    }

    /// <summary>
    /// Create checklist from template
    /// </summary>
    public async Task<long> CreateFromTemplateAsync(long templateId, ChecklistInputDto input)
    {
        var template = await _checklistRepository.GetByIdAsync(templateId);
        if (template == null || !template.IsTemplate)
        {
            throw new CRMException(ErrorCodeEnum.CustomError, "Template not found");
        }

        // Validate name uniqueness
        if (await _checklistRepository.IsNameExistsAsync(input.Name, input.Team))
        {
            throw new CRMException(ErrorCodeEnum.CustomError,
                $"Checklist name '{input.Name}' already exists");
        }

        // Create instance from template
        var instance = _mapper.Map<Checklist>(input);
        instance.IsTemplate = false;
        instance.TemplateId = templateId;
        instance.Type = "Instance";
        // Copy tenant and app information from template
        instance.TenantId = template.TenantId;
        instance.AppCode = template.AppCode;

        // Initialize create information with proper ID and timestamps
        instance.InitCreateInfo(_userContext);

        var instanceId = await _checklistRepository.InsertReturnSnowflakeIdAsync(instance);

        // Copy tasks from template
        var templateTasks = await _checklistTaskRepository.GetByChecklistIdAsync(templateId);
        if (templateTasks?.Any() == true)
        {
            var instanceTasks = templateTasks.Select(task => new ChecklistTask
            {
                ChecklistId = instanceId,
                Name = task.Name,
                Description = task.Description,
                TaskType = task.TaskType,
                IsRequired = task.IsRequired,
                AssignedTeam = input.Team,
                Priority = task.Priority,
                Order = task.Order,
                EstimatedHours = task.EstimatedHours,
                Status = "Pending",
                IsActive = true,
                // Copy tenant and app information from template task
                TenantId = task.TenantId,
                AppCode = task.AppCode
            }).ToList();

            // Initialize create info for each task
            foreach (var task in instanceTasks)
            {
                task.InitCreateInfo(_userContext);
            }

            await _checklistTaskRepository.InsertRangeAsync(instanceTasks);

            // Update completion statistics
            await CalculateCompletionAsync(instanceId);
        }

        // Cache has been removed, no cleanup needed

        return instanceId;
    }

    /// <summary>
    /// Calculate completion rate
    /// </summary>
    public async Task<decimal> CalculateCompletionAsync(long id)
    {
        var tasks = await _checklistTaskRepository.GetByChecklistIdAsync(id);
        if (!tasks.Any())
        {
            return 0;
        }

        var completedTasks = tasks.Count(t => t.IsCompleted);
        var totalTasks = tasks.Count;
        var completionRate = (decimal)completedTasks / totalTasks * 100;

        return completionRate;
    }

    /// <summary>
    /// Get checklist templates
    /// </summary>
    public async Task<List<ChecklistOutputDto>> GetTemplatesAsync()
    {
        var templates = await _checklistRepository.GetTemplatesAsync();
        var result = _mapper.Map<List<ChecklistOutputDto>>(templates);

        // Fill assignments for the templates
        await FillAssignmentsAsync(result);

        return result;
    }

    /// <summary>
    /// Get checklist statistics by team
    /// </summary>
    public async Task<ChecklistStatisticsDto> GetStatisticsByTeamAsync(string team)
    {
        var statistics = await _checklistRepository.GetStatisticsByTeamAsync(team);

        // Get overdue tasks count
        var overdueTasks = await _checklistTaskRepository.GetOverdueTasksAsync();
        var teamOverdueTasks = overdueTasks.Count; // Team filtering - future enhancement

        return new ChecklistStatisticsDto
        {
            Team = team,
            TotalChecklists = (int)statistics["TotalChecklists"],
            ActiveChecklists = (int)statistics["ActiveChecklists"],
            TemplateCount = (int)statistics["TemplateCount"],
            InstanceCount = (int)statistics["InstanceCount"],
            TotalTasks = (int)statistics["TotalTasks"],
            CompletedTasks = (int)statistics["CompletedTasks"],
            AverageCompletionRate = (decimal)statistics["AverageCompletionRate"],
            OverdueTasks = teamOverdueTasks,
            TotalEstimatedHours = (int)statistics["TotalEstimatedHours"],
            TotalActualHours = 0 // Task statistics calculation - future enhancement
        };
    }

    /// <summary>
    /// Get checklists by stage ID
    /// </summary>
    public async Task<List<ChecklistOutputDto>> GetByStageIdAsync(long stageId)
    {
        // Since GetByStageIdAsync method was removed, we need to get all checklists and filter by assignments
        var (allChecklists, _) = await _checklistRepository.GetPagedAsync(1, int.MaxValue);
        var checklists = allChecklists.Where(c =>
            c.Assignments?.Any(a => a.StageId == stageId) == true).ToList();
        var result = _mapper.Map<List<ChecklistOutputDto>>(checklists);

        // Fill assignments and tasks for the checklists
        await FillAssignmentsAndTasksAsync(result);

        return result;
    }

    /// <summary>
    /// Get checklists by multiple stage IDs
    /// </summary>
    public async Task<List<ChecklistOutputDto>> GetByStageIdsAsync(List<long> stageIds)
    {
        if (stageIds == null || !stageIds.Any())
        {
            return new List<ChecklistOutputDto>();
        }

        // Since GetByStageIdsAsync method was removed, we need to get all checklists and filter by assignments
        var (allChecklists, _) = await _checklistRepository.GetPagedAsync(1, int.MaxValue);
        var checklists = allChecklists.Where(c =>
            c.Assignments?.Any(a => stageIds.Contains(a.StageId)) == true).ToList();
        var result = _mapper.Map<List<ChecklistOutputDto>>(checklists);

        // Fill assignments and tasks for the checklists
        await FillAssignmentsAndTasksAsync(result);

        return result;
    }

    /// <summary>
    /// Batch get checklists by stage IDs
    /// </summary>
    public async Task<BatchStageChecklistResponse> GetByStageIdsBatchAsync(BatchStageChecklistRequest request)
    {
        var response = new BatchStageChecklistResponse();

        if (request.StageIds == null || !request.StageIds.Any())
        {
            return response;
        }

        // Batch query all checklists for all stages
        var (allChecklistEntities, _) = await _checklistRepository.GetPagedAsync(1, int.MaxValue);
        var allChecklists = allChecklistEntities.Where(c =>
            c.Assignments?.Any(a => request.StageIds.Contains(a.StageId)) == true).ToList();

        // Group by Stage ID - since each checklist can have multiple assignments, we need to handle this differently
        var groupedChecklists = new Dictionary<long, List<ChecklistOutputDto>>();

        foreach (var stageId in request.StageIds)
        {
            var checklistsForStage = allChecklists.Where(c =>
                c.Assignments?.Any(a => a.StageId == stageId) == true).ToList();
            groupedChecklists[stageId] = _mapper.Map<List<ChecklistOutputDto>>(checklistsForStage);
        }

        // Fill assignments for all checklists
        var allChecklistDtos = groupedChecklists.Values.SelectMany(list => list).ToList();
        await FillAssignmentsAsync(allChecklistDtos);

        // Populate response
        foreach (var stageId in request.StageIds)
        {
            response.StageChecklists[stageId] = groupedChecklists.ContainsKey(stageId)
                ? groupedChecklists[stageId]
                : new List<ChecklistOutputDto>();
        }

        return response;
    }

    private string GeneratePdfContent(Checklist checklist)
    {
        // Simplified PDF content generation
        // In a real implementation, use a proper PDF library
        var content = $@"
CHECKLIST REPORT
================

Name: {checklist.Name}
Description: {checklist.Description}
Team: {checklist.Team}

ASSIGNMENTS:
-----------
";

        if (checklist.Assignments?.Any() == true)
        {
            foreach (var assignment in checklist.Assignments)
            {
                content += $"Workflow ID: {assignment.WorkflowId}, Stage ID: {assignment.StageId}\n";
            }
        }

        return content;
    }

    /// <summary>
    /// Fill assignments and task statistics for checklist output DTOs
    /// </summary>
    private async Task FillAssignmentsAsync(List<ChecklistOutputDto> checklists)
    {
        await FillAssignmentsAndTasksAsync(checklists, false);
    }

    /// <summary>
    /// Fill assignments, task statistics and optionally task details for checklist output DTOs
    /// </summary>
    private async Task FillAssignmentsAndTasksAsync(List<ChecklistOutputDto> checklists, bool includeTasks = true)
    {
        if (checklists == null || !checklists.Any())
            return;

        // Get the full checklist entities to access the Assignments property
        var checklistIds = checklists.Select(c => c.Id).ToList();
        var entities = new List<Checklist>();

        foreach (var id in checklistIds)
        {
            var entity = await _checklistRepository.GetByIdAsync(id);
            if (entity != null)
            {
                entities.Add(entity);
            }
        }

        // Map assignments and calculate task statistics
        foreach (var checklist in checklists)
        {
            var entity = entities.FirstOrDefault(e => e.Id == checklist.Id);
            if (entity != null)
            {
                // Use the Assignments property from the entity (which reads from AssignmentsJson)
                checklist.Assignments = entity.Assignments?.Select(a => new FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto
                {
                    WorkflowId = a.WorkflowId,
                    StageId = a.StageId
                }).ToList() ?? new List<FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto>();
            }
            else
            {
                checklist.Assignments = new List<FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto>();
            }

            // Load tasks and calculate task statistics
            try
            {
                var tasks = await _checklistTaskRepository.GetByChecklistIdAsync(checklist.Id);
                checklist.TotalTasks = tasks?.Count ?? 0;
                checklist.CompletedTasks = tasks?.Count(t => t.IsCompleted) ?? 0;

                // Calculate completion rate
                if (checklist.TotalTasks > 0)
                {
                    checklist.CompletionRate = Math.Round((decimal)checklist.CompletedTasks / checklist.TotalTasks * 100, 2);
                }
                else
                {
                    checklist.CompletionRate = 0;
                }

                // Include task details if requested
                if (includeTasks && tasks != null)
                {
                    checklist.Tasks = _mapper.Map<List<ChecklistTaskOutputDto>>(tasks);
                }
                else
                {
                    checklist.Tasks = new List<ChecklistTaskOutputDto>();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the entire operation
                Console.WriteLine($"Error calculating task statistics for checklist {checklist.Id}: {ex.Message}");
                checklist.TotalTasks = 0;
                checklist.CompletedTasks = 0;
                checklist.CompletionRate = 0;
                checklist.Tasks = new List<ChecklistTaskOutputDto>();
            }
        }
    }
}
