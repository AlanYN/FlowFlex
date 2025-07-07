using System.IO;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.Checklist;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.Models;
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

namespace FlowFlex.Application.Service.OW;

/// <summary>
/// Checklist service implementation
/// </summary>
public class ChecklistService : IChecklistService, IScopedService
{
    private readonly IChecklistRepository _checklistRepository;
    private readonly IChecklistTaskRepository _checklistTaskRepository;
    private readonly IMapper _mapper;
    private readonly UserContext _userContext;

    public ChecklistService(
        IChecklistRepository checklistRepository,
        IChecklistTaskRepository checklistTaskRepository,
        IMapper mapper,
        UserContext userContext)
    {
        _checklistRepository = checklistRepository;
        _checklistTaskRepository = checklistTaskRepository;
        _mapper = mapper;
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
        if (await _checklistRepository.IsNameExistsAsync(input.Name, null))
        {
            throw new CRMException(ErrorCodeEnum.BusinessError, $"Checklist name '{input.Name}' already exists");
        }

        var entity = _mapper.Map<Checklist>(input);
        
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

        // Validate name uniqueness (exclude current record)
        if (await _checklistRepository.IsNameExistsAsync(input.Name, null, id))
        {
            throw new CRMException(ErrorCodeEnum.BusinessError, $"Checklist name '{input.Name}' already exists");
        }

        _mapper.Map(input, entity);
        
        // Initialize update information with proper timestamps
        entity.InitUpdateInfo(_userContext);

        return await _checklistRepository.UpdateAsync(entity);
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
        var checklist = await _checklistRepository.GetWithTasksAsync(id);
        if (checklist == null)
        {
            throw new CRMException(ErrorCodeEnum.CustomError, "Checklist not found");
        }

        var result = _mapper.Map<ChecklistOutputDto>(checklist);
        return result;
    }

    /// <summary>
    /// Get list of checklists by team
    /// </summary>
    public async Task<List<ChecklistOutputDto>> GetListAsync(string team = null)
    {
        var checklists = await _checklistRepository.GetByTeamAsync(team);
        var result = _mapper.Map<List<ChecklistOutputDto>>(checklists);
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
        var sourceChecklist = await _checklistRepository.GetWithTasksAsync(id);
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

        // Create new checklist
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
            IsActive = true
        };

        var newChecklistId = await _checklistRepository.InsertReturnSnowflakeIdAsync(newChecklist);

        // Copy tasks if requested
        if (input.CopyTasks && sourceChecklist.Tasks?.Any() == true)
        {
            var newTasks = sourceChecklist.Tasks.Select(task => new ChecklistTask
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
                IsActive = true
            }).ToList();

            await _checklistTaskRepository.InsertRangeAsync(newTasks);

            // Update completion statistics
            await CalculateCompletionAsync(newChecklistId);
        }

        // Cache has been removed, no cleanup needed

        return newChecklistId;
    }

    /// <summary>
    /// Export checklist to PDF
    /// </summary>
    public async Task<Stream> ExportToPdfAsync(long id)
    {
        var checklist = await _checklistRepository.GetWithTasksAsync(id);
        if (checklist == null)
        {
            throw new CRMException(ErrorCodeEnum.CustomError, "Checklist not found");
        }

        // TODO: Implement PDF generation using a PDF library like iTextSharp or PdfSharp
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
        var template = await _checklistRepository.GetWithTasksAsync(templateId);
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

        var instanceId = await _checklistRepository.InsertReturnSnowflakeIdAsync(instance);

        // Copy tasks from template
        if (template.Tasks?.Any() == true)
        {
            var instanceTasks = template.Tasks.Select(task => new ChecklistTask
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
                IsActive = true
            }).ToList();

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
            await _checklistRepository.UpdateCompletionRateAsync(id, 0, 0);

            // Cache has been removed, no cleanup needed

            return 0;
        }

        var completedTasks = tasks.Count(t => t.IsCompleted);
        var totalTasks = tasks.Count;
        var completionRate = (decimal)completedTasks / totalTasks * 100;

        await _checklistRepository.UpdateCompletionRateAsync(id, completionRate, completedTasks);

        // Cache has been removed, no cleanup needed

        return completionRate;
    }

    /// <summary>
    /// Get checklist templates
    /// </summary>
    public async Task<List<ChecklistOutputDto>> GetTemplatesAsync()
    {
        var templates = await _checklistRepository.GetTemplatesAsync();
        return _mapper.Map<List<ChecklistOutputDto>>(templates);
    }

    /// <summary>
    /// Get checklist statistics by team
    /// </summary>
    public async Task<ChecklistStatisticsDto> GetStatisticsByTeamAsync(string team)
    {
        var statistics = await _checklistRepository.GetStatisticsByTeamAsync(team);

        // Get overdue tasks count
        var overdueTasks = await _checklistTaskRepository.GetOverdueTasksAsync();
        var teamOverdueTasks = overdueTasks.Count; // TODO: Filter by team

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
            TotalActualHours = 0 // TODO: Calculate from task statistics
        };
    }

    /// <summary>
    /// Get checklists by stage ID
    /// </summary>
    public async Task<List<ChecklistOutputDto>> GetByStageIdAsync(long stageId)
    {
        var checklists = await _checklistRepository.GetByStageIdWithTasksAsync(stageId);
        Console.WriteLine($"Debug: Found {checklists.Count} checklists for stageId: {stageId}");

        return _mapper.Map<List<ChecklistOutputDto>>(checklists);
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
Completion Rate: {checklist.CompletionRate:F1}%
Total Tasks: {checklist.TotalTasks}
Completed Tasks: {checklist.CompletedTasks}

TASKS:
------
";

        if (checklist.Tasks?.Any() == true)
        {
            foreach (var task in checklist.Tasks.OrderBy(t => t.Order))
            {
                var status = task.IsCompleted ? "[✓]" : "[ ]";
                content += $"{status} {task.Name}\n";
                if (!string.IsNullOrEmpty(task.Description))
                {
                    content += $"    Description: {task.Description}\n";
                }
                content += $"    Priority: {task.Priority}, Estimated: {task.EstimatedHours}h\n\n";
            }
        }

        return content;
    }


}
