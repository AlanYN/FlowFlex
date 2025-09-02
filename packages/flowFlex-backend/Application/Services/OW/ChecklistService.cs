using System;
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
using System.Text.Json;
using System.Linq;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using FlowFlex.Domain.Shared.Enums.OW;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using FlowFlex.Infrastructure.Services;
using FlowFlex.Infrastructure.Extensions;

namespace FlowFlex.Application.Service.OW;

/// <summary>
/// Checklist service implementation
/// </summary>
public class ChecklistService : IChecklistService, IScopedService
{
    private readonly IChecklistRepository _checklistRepository;
    private readonly IChecklistTaskRepository _checklistTaskRepository;
    private readonly IMapper _mapper;
    private readonly IStageRepository _stageRepository;
    private readonly UserContext _userContext;
    private readonly IOperatorContextService _operatorContextService;
    private readonly IComponentMappingService _mappingService;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly IOperationChangeLogService _operationChangeLogService;
    private readonly ILogger<ChecklistService> _logger;

    public ChecklistService(
        IChecklistRepository checklistRepository,
        IChecklistTaskRepository checklistTaskRepository,
        IMapper mapper,
        IStageRepository stageRepository,
        UserContext userContext,
        IOperatorContextService operatorContextService,
        IComponentMappingService mappingService,
        IBackgroundTaskQueue backgroundTaskQueue,
        IOperationChangeLogService operationChangeLogService,
        ILogger<ChecklistService> logger)
    {
        _checklistRepository = checklistRepository;
        _checklistTaskRepository = checklistTaskRepository;
        _mapper = mapper;
        _mappingService = mappingService;
        _stageRepository = stageRepository;
        _userContext = userContext;
        _operatorContextService = operatorContextService;
        _backgroundTaskQueue = backgroundTaskQueue;
        _operationChangeLogService = operationChangeLogService;
        _logger = logger;
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

        // Assignments are no longer stored in Checklist entity
        // They will be managed through Stage Components only

        // Initialize create information with proper ID and timestamps
        entity.InitCreateInfo(_userContext);
        AuditHelper.ApplyCreateAudit(entity, _operatorContextService);

        await _checklistRepository.InsertAsync(entity);

        // Log checklist create operation (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await _operationChangeLogService.LogChecklistCreateAsync(
                    checklistId: entity.Id,
                    checklistName: entity.Name
                );
            }
            catch
            {
                // Ignore logging errors to avoid affecting main operation
            }
        });

        // Sync service is no longer needed as assignments are managed through Stage Components

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
            duplicate.ModifyDate = DateTimeOffset.UtcNow;
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
                duplicate.ModifyDate = DateTimeOffset.UtcNow;
                await _checklistRepository.UpdateAsync(duplicate);
            }
        }

        // Update the checklist entity (without assignments)
        _mapper.Map(input, entity);

        // Initialize update information with proper timestamps
        entity.InitUpdateInfo(_userContext);
        AuditHelper.ApplyModifyAudit(entity, _operatorContextService);

        var result = await _checklistRepository.UpdateAsync(entity);

        // Log checklist update operation if successful (fire-and-forget)
        if (result)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    // Prepare before and after data for logging
                    var beforeData = JsonSerializer.Serialize(new
                    {
                        Name = originalName,
                        Description = entity.Description, // This will be the old description since we mapped after
                        Team = entity.Team,
                        EstimatedHours = entity.EstimatedHours,
                        IsActive = entity.IsActive
                    });

                    var afterData = JsonSerializer.Serialize(new
                    {
                        Name = entity.Name,
                        Description = entity.Description,
                        Team = entity.Team,
                        EstimatedHours = entity.EstimatedHours,
                        IsActive = entity.IsActive
                    });

                    // Determine changed fields
                    var changedFields = new List<string>();
                    if (originalName != entity.Name) changedFields.Add("Name");

                    await _operationChangeLogService.LogChecklistUpdateAsync(
                        checklistId: entity.Id,
                        checklistName: entity.Name,
                        beforeData: beforeData,
                        afterData: afterData,
                        changedFields: changedFields
                    );
                }
                catch
                {
                    // Ignore logging errors to avoid affecting main operation
                }
            });
        }

        // If name changed, sync the new name to all related stages
        if (result && originalName != input.Name)
        {
            try
            {
                // Use background task queue for safe async processing
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        await _mappingService.NotifyChecklistNameChangeAsync(id, input.Name);
                    }
                    catch (Exception)
                    {
                        // Error already logged by BackgroundTaskService
                        // Don't throw to avoid breaking the background task
                    }
                });
            }
            catch (Exception ex)
            {
                LoggingExtensions.WriteError($"[ChecklistService] Error starting checklist name sync background task: {ex.Message}");
                // Don't throw to avoid breaking the main operation
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

        var checklistName = checklist.Name; // Store name before deletion

        // Soft delete
        checklist.IsValid = false;
        checklist.ModifyDate = DateTimeOffset.UtcNow;

        var result = await _checklistRepository.UpdateAsync(checklist);

        // Log checklist delete operation if successful (fire-and-forget)
        if (result)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _operationChangeLogService.LogChecklistDeleteAsync(
                        checklistId: id,
                        checklistName: checklistName,
                        reason: "Checklist deleted via admin portal"
                    );
                }
                catch
                {
                    // Ignore logging errors to avoid affecting main operation
                }
            });
        }

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

        // Determine base name and ensure uniqueness
        var baseName = string.IsNullOrWhiteSpace(input.Name)
            ? $"{sourceChecklist.Name} (Copy)"
            : input.Name;
        var uniqueName = await EnsureUniqueChecklistNameAsync(baseName, input.TargetTeam ?? sourceChecklist.Team);

        // Create new checklist with assignments copied
        var newChecklist = new Checklist
        {
            Name = uniqueName,
            Description = input.Description ?? sourceChecklist.Description,
            Team = input.TargetTeam ?? sourceChecklist.Team,
            Type = sourceChecklist.Type,
            Status = "Active",
            IsTemplate = input.SetAsTemplate,
            TemplateId = sourceChecklist.IsTemplate ? sourceChecklist.Id : sourceChecklist.TemplateId,
            EstimatedHours = sourceChecklist.EstimatedHours,
            IsActive = true,
            // Assignments are no longer stored in Checklist entity
            // Copy tenant and app information from source checklist
            TenantId = sourceChecklist.TenantId,
            AppCode = sourceChecklist.AppCode
        };

        // Initialize create information with proper ID and timestamps
        newChecklist.InitCreateInfo(_userContext);
        AuditHelper.ApplyCreateAudit(newChecklist, _operatorContextService);

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
                    // Copy assignee information
                    AssigneeId = task.AssigneeId,
                    AssigneeName = task.AssigneeName,
                    AssigneeJson = task.AssigneeJson,
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

        // Log duplicate operation
        try
        {
            await _operationChangeLogService.LogOperationAsync(
                OperationTypeEnum.ChecklistDuplicate,
                BusinessModuleEnum.Checklist,
                newChecklistId,
                null, // No onboarding context for checklist duplication
                null, // No stage context for checklist duplication
                $"Checklist Duplicated",
                $"Duplicated checklist '{sourceChecklist.Name}' to '{uniqueName}'",
                sourceChecklist.Name, // beforeData
                uniqueName, // afterData
                new List<string> { "Name", "Description", "Team", "Tasks" },
                JsonConvert.SerializeObject(new
                {
                    SourceId = id,
                    SourceName = sourceChecklist.Name,
                    NewId = newChecklistId,
                    NewName = uniqueName,
                    CopyTasks = input.CopyTasks,
                    TargetTeam = input.TargetTeam,
                    SetAsTemplate = input.SetAsTemplate
                }),
                OperationStatusEnum.Success
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log checklist duplicate operation for checklist {ChecklistId}", newChecklistId);
        }

        return newChecklistId;
    }

    private async Task<string> EnsureUniqueChecklistNameAsync(string baseName, string team = null)
    {
        var originalName = baseName;
        var counter = 1;
        var currentName = baseName;

        while (true)
        {
            var exists = await _checklistRepository.IsNameExistsAsync(currentName, team);
            if (!exists)
            {
                return currentName;
            }

            counter++;
            currentName = $"{originalName} ({counter})";
        }
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
        AuditHelper.ApplyCreateAudit(instance, _operatorContextService);

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
        var checklistIds = await GetChecklistIdsByStageIdAsync(stageId);
        if (!checklistIds.Any())
            return new List<ChecklistOutputDto>();

        var checklists = await _checklistRepository.GetByIdsAsync(checklistIds);
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

        var allChecklistIds = new HashSet<long>();
        foreach (var stageId in stageIds)
        {
            var checklistIds = await GetChecklistIdsByStageIdAsync(stageId);
            foreach (var id in checklistIds)
            {
                allChecklistIds.Add(id);
            }
        }

        if (!allChecklistIds.Any())
            return new List<ChecklistOutputDto>();

        var checklists = await _checklistRepository.GetByIdsAsync(allChecklistIds.ToList());
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

        // Get checklists for each stage from Stage Components
        foreach (var stageId in request.StageIds)
        {
            var checklistIds = await GetChecklistIdsByStageIdAsync(stageId);
            if (checklistIds.Any())
            {
                var checklists = await _checklistRepository.GetByIdsAsync(checklistIds);
                var checklistDtos = _mapper.Map<List<ChecklistOutputDto>>(checklists);
                await FillAssignmentsAsync(checklistDtos);
                response.StageChecklists[stageId] = checklistDtos;
            }
            else
            {
                response.StageChecklists[stageId] = new List<ChecklistOutputDto>();
            }
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

        // Assignments are now managed through Stage Components
        // For PDF generation, we would need to query the assignments from Stage Components
        // For now, show a placeholder message
        content += "Assignments are now managed through Stage Components\n";

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

        var checklistIds = checklists.Select(c => c.Id).ToList();

        LoggingExtensions.WriteLine($"[ChecklistService] Using mapping table to fill assignments for {checklistIds.Count} checklists");

        // Get assignments from mapping table (ultra-fast)
        var assignments = await _mappingService.GetChecklistAssignmentsAsync(checklistIds);

        // Map assignments to checklists
        foreach (var checklist in checklists)
        {
            if (assignments.TryGetValue(checklist.Id, out var checklistAssignments))
            {
                checklist.Assignments = checklistAssignments.Select(a => new FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto
                {
                    WorkflowId = a.WorkflowId,
                    StageId = a.StageId
                }).ToList();
            }
            else
            {
                checklist.Assignments = new List<FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto>();
            }
        }

        // Batch load tasks for all checklists (avoid N+1)
        List<ChecklistTask> allTasks;
        try
        {
            allTasks = await _checklistTaskRepository.GetByChecklistIdsAsync(checklistIds);
        }
        catch (Exception ex)
        {
            LoggingExtensions.WriteError($"Error batch loading tasks for checklists: {ex.Message}");
            allTasks = new List<ChecklistTask>();
        }

        var tasksGrouped = allTasks
            .GroupBy(t => t.ChecklistId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var checklist in checklists)
        {
            var tasks = tasksGrouped.TryGetValue(checklist.Id, out var list) ? list : new List<ChecklistTask>();

            checklist.TotalTasks = tasks.Count;
            checklist.CompletedTasks = tasks.Count(t => t.IsCompleted);

            if (checklist.TotalTasks > 0)
            {
                checklist.CompletionRate = Math.Round((decimal)checklist.CompletedTasks / checklist.TotalTasks * 100, 2);
            }
            else
            {
                checklist.CompletionRate = 0;
            }

            if (includeTasks && tasks.Count > 0)
            {
                checklist.Tasks = _mapper.Map<List<ChecklistTaskOutputDto>>(tasks);
            }
            else
            {
                checklist.Tasks = new List<ChecklistTaskOutputDto>();
            }
        }
    }



    /// <summary>
    /// Get checklist IDs by stage ID from mapping table (ultra-fast)
    /// </summary>
    private async Task<List<long>> GetChecklistIdsByStageIdAsync(long stageId)
    {
        try
        {
            LoggingExtensions.WriteLine($"[ChecklistService] Getting checklist IDs for stage {stageId} using ComponentMappingService");

            // Use ComponentMappingService for ultra-fast mapping table query
            var checklistIds = await _mappingService.GetChecklistIdsByWorkflowStageAsync(null, stageId);

            LoggingExtensions.WriteLine($"[ChecklistService] ComponentMappingService found {checklistIds.Count} checklist IDs for stage {stageId}: [{string.Join(", ", checklistIds)}]");

            return checklistIds;
        }
        catch (Exception ex)
        {
            LoggingExtensions.WriteError($"[ChecklistService] Error getting checklist IDs for stage {stageId}: {ex.Message}");
            return new List<long>();
        }
    }
}
