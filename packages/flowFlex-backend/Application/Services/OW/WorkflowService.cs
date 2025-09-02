using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.Dtos.OW.Workflow;
using FlowFlex.Application.Contracts.Dtos.OW.Stage;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;

using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Domain.Shared.Enums.OW;
using System.Text.Json;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using JsonException = System.Text.Json.JsonException;
using System.Text;
using System.IO;
using System.Linq;
using Item.Excel.Lib;
using FlowFlex.Application.Helpers;
using FlowFlex.Domain.Shared.Models;
using Item.Redis;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Application.Contracts.IServices.OW;

namespace FlowFlex.Application.Service.OW
{
    public class WorkflowService : IWorkflowService, IScopedService
    {
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IStageRepository _stageRepository;
        private readonly IMapper _mapper;
        private readonly UserContext _userContext;
        private readonly ILogger<WorkflowService> _logger;
        private readonly IOperatorContextService _operatorContextService;
        private readonly IComponentMappingService _componentMappingService;
        private readonly IDistributedCacheService _cacheService;
        private readonly IOperationChangeLogService _operationChangeLogService;

        public WorkflowService(IWorkflowRepository workflowRepository, IStageRepository stageRepository, IMapper mapper, UserContext userContext, ILogger<WorkflowService> logger, IOperatorContextService operatorContextService, IComponentMappingService componentMappingService, IDistributedCacheService cacheService, IOperationChangeLogService operationChangeLogService)
        {
            _workflowRepository = workflowRepository;
            _stageRepository = stageRepository;
            _mapper = mapper;
            _userContext = userContext;
            _logger = logger;
            _operatorContextService = operatorContextService;
            _componentMappingService = componentMappingService;
            _cacheService = cacheService;
            _operationChangeLogService = operationChangeLogService;
        }

        public async Task<long> CreateAsync(WorkflowInputDto input)
        {
            if (input == null)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input cannot be null");
            }

            if (_mapper == null)
            {
                throw new CRMException(ErrorCodeEnum.SystemError, "AutoMapper not configured");
            }

            // Validate name uniqueness
            if (await _workflowRepository.ExistsNameAsync(input.Name))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, $"Workflow name '{input.Name}' already exists");
            }

            // If set as default, need to cancel other default workflows first
            if (input.IsDefault)
            {
                var existingDefault = await _workflowRepository.GetDefaultWorkflowAsync();
                if (existingDefault != null)
                {
                    await _workflowRepository.RemoveDefaultWorkflowAsync(existingDefault.Id);
                }
            }

            var entity = _mapper.Map<Workflow>(input);
            entity.StartDate = input.StartDate == default ? DateTimeOffset.UtcNow : input.StartDate;

            // Initialize create information with proper ID and timestamps
            entity.InitCreateInfo(_userContext);
            AuditHelper.ApplyCreateAudit(entity, _operatorContextService);

            await _workflowRepository.InsertAsync(entity);

            // Create stages if provided
            if (input.Stages != null && input.Stages.Any())
            {
                foreach (var stageInput in input.Stages.OrderBy(s => s.Order))
                {
                    var stage = _mapper.Map<Stage>(stageInput);
                    stage.WorkflowId = entity.Id;

                    // Initialize create information
                    stage.InitCreateInfo(_userContext);
                    AuditHelper.ApplyCreateAudit(stage, _operatorContextService);

                    await _stageRepository.InsertAsync(stage);
                }

                _logger.LogInformation("Created {StageCount} stages for workflow {WorkflowId}", input.Stages.Count, entity.Id);

                // Sync component mappings for all stages in the workflow
                try
                {
                    await _componentMappingService.SyncWorkflowMappingsAsync(entity.Id);
                    _logger.LogInformation("Successfully synced component mappings for workflow {WorkflowId}", entity.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sync component mappings for workflow {WorkflowId}: {Error}", entity.Id, ex.Message);
                    // Don't fail the workflow creation if mapping sync fails
                }
            }

            // Log workflow create operation (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _operationChangeLogService.LogWorkflowCreateAsync(
                        workflowId: entity.Id,
                        workflowName: entity.Name,
                        workflowDescription: entity.Description
                    );
                }
                catch
                {
                    // Ignore logging errors to avoid affecting main operation
                }
            });

            return entity.Id;
        }

        public async Task<bool> UpdateAsync(long id, WorkflowInputDto input)
        {
            if (input == null)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input cannot be null");
            }

            if (_mapper == null)
            {
                throw new CRMException(ErrorCodeEnum.SystemError, "AutoMapper not configured");
            }

            var entity = await _workflowRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Workflow with ID {id} not found");
            }

            // Validate name uniqueness (exclude current record)
            if (await _workflowRepository.ExistsNameAsync(input.Name, id))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, $"Workflow name '{input.Name}' already exists");
            }

            // Detect if there are actual changes
            bool hasChanges = HasWorkflowChanges(entity, input);

            // Only handle IsDefault changes if explicitly provided in the input
            // For AI-generated updates, we preserve the existing IsDefault state
            bool shouldUpdateDefaultStatus = ShouldUpdateDefaultStatus(input, entity);

            if (shouldUpdateDefaultStatus)
            {
                // If set as default, need to cancel other default workflows first
                if (input.IsDefault && !entity.IsDefault)
                {
                    await _workflowRepository.SetDefaultWorkflowAsync(id);
                }
                else if (!input.IsDefault && entity.IsDefault)
                {
                    await _workflowRepository.RemoveDefaultWorkflowAsync(id);
                }
            }
            else
            {
                // Preserve existing IsDefault state for AI updates
                input.IsDefault = entity.IsDefault;
            }

            // Store original values for change detection and logging BEFORE mapping
            var originalName = entity.Name;
            var originalDescription = entity.Description;
            var originalStatus = entity.Status;
            var originalIsDefault = entity.IsDefault;
            var originalIsActive = entity.IsActive;
            var originalStartDate = entity.StartDate;
            var originalEndDate = entity.EndDate;

            // Only create version history record when there are actual changes
            if (hasChanges)
            {
                // Increment version number
                entity.Version += 1;
            }

            // Update entity data first
            _mapper.Map(input, entity);

            // Initialize update information with proper timestamps
            entity.InitUpdateInfo(_userContext);
            AuditHelper.ApplyModifyAudit(entity, _operatorContextService);

            var updateResult = await _workflowRepository.UpdateAsync(entity);

            // Handle stages update if provided
            if (updateResult && input.Stages != null)
            {
                // Get existing stages
                var existingStages = await _stageRepository.GetByWorkflowIdAsync(id);

                // Delete existing stages that are not in the input
                var stagesToDelete = existingStages.Where(existing =>
                    !input.Stages.Any(inputStage =>
                        inputStage.Name.Equals(existing.Name, StringComparison.OrdinalIgnoreCase))).ToList();

                foreach (var stageToDelete in stagesToDelete)
                {
                    await _stageRepository.DeleteAsync(stageToDelete);
                }

                // Update or create stages
                foreach (var stageInput in input.Stages.OrderBy(s => s.Order))
                {
                    var existingStage = existingStages.FirstOrDefault(s =>
                        s.Name.Equals(stageInput.Name, StringComparison.OrdinalIgnoreCase));

                    if (existingStage != null)
                    {
                        // Update existing stage
                        _mapper.Map(stageInput, existingStage);
                        existingStage.WorkflowId = id;
                        existingStage.InitUpdateInfo(_userContext);
                        AuditHelper.ApplyModifyAudit(existingStage, _operatorContextService);

                        await _stageRepository.UpdateAsync(existingStage);
                    }
                    else
                    {
                        // Create new stage
                        var newStage = _mapper.Map<Stage>(stageInput);
                        newStage.WorkflowId = id;
                        newStage.InitCreateInfo(_userContext);
                        AuditHelper.ApplyCreateAudit(newStage, _operatorContextService);

                        await _stageRepository.InsertAsync(newStage);
                    }
                }

                _logger.LogInformation("Updated stages for workflow {WorkflowId}: {StageCount} stages processed",
                    id, input.Stages.Count);
            }

            // If there are changes and update is successful, create version history record (save post-change information) - Disabled automatic version creation
            // if (hasChanges && updateResult)
            // {
            //     // Get updated stage list for version snapshot
            //     var updatedStages = await _stageRepository.GetByWorkflowIdAsync(id);
            //     
            //     // Create version history record after update (including stage snapshot) - save post-change information
            //     await _workflowVersionRepository.CreateVersionHistoryWithStagesAsync(entity, updatedStages, "Updated", $"Workflow updated to version {entity.Version}");
            // }

            // Cache cleanup removed

            if (updateResult)
            {
                var cacheKey = $"workflow:get_by_id:{id}:{_userContext.AppCode}";
                await _cacheService.RemoveAsync(cacheKey);

                // Log workflow update operation if there were actual changes (fire-and-forget)
                if (hasChanges)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // Get the updated workflow for logging
                            var updatedWorkflow = await _workflowRepository.GetByIdAsync(id);
                            if (updatedWorkflow != null)
                            {
                                // Prepare before and after data for logging using original values
                                var beforeData = JsonSerializer.Serialize(new
                                {
                                    Name = originalName,
                                    Description = originalDescription,
                                    Status = originalStatus,
                                    IsDefault = originalIsDefault,
                                    IsActive = originalIsActive,
                                    StartDate = originalStartDate,
                                    EndDate = originalEndDate
                                });

                                var afterData = JsonSerializer.Serialize(new
                                {
                                    Name = updatedWorkflow.Name,
                                    Description = updatedWorkflow.Description,
                                    Status = updatedWorkflow.Status,
                                    IsDefault = updatedWorkflow.IsDefault,
                                    IsActive = updatedWorkflow.IsActive,
                                    StartDate = updatedWorkflow.StartDate,
                                    EndDate = updatedWorkflow.EndDate
                                });

                                // Determine changed fields by comparing original vs updated values
                                var changedFields = new List<string>();
                                if (originalName != updatedWorkflow.Name) changedFields.Add("Name");
                                if (originalDescription != updatedWorkflow.Description) changedFields.Add("Description");
                                if (originalStatus != updatedWorkflow.Status) changedFields.Add("Status");
                                if (originalIsDefault != updatedWorkflow.IsDefault) changedFields.Add("IsDefault");
                                if (originalIsActive != updatedWorkflow.IsActive) changedFields.Add("IsActive");
                                if (originalStartDate != updatedWorkflow.StartDate) changedFields.Add("StartDate");
                                if (originalEndDate != updatedWorkflow.EndDate) changedFields.Add("EndDate");

                                // Alternative: Use auto-detection (experimental)
                                // var autoDetectedFields = AutoDetectChangedFields(beforeData, afterData);
                                // if (autoDetectedFields.Any()) changedFields = autoDetectedFields;

                                // Debug logging
                                _logger.LogDebug("WorkflowUpdate Debug - Before: {BeforeData}", beforeData);
                                _logger.LogDebug("WorkflowUpdate Debug - After: {AfterData}", afterData);
                                _logger.LogDebug("WorkflowUpdate Debug - ChangedFields: {ChangedFields}", string.Join(", ", changedFields));

                                await _operationChangeLogService.LogWorkflowUpdateAsync(
                                    workflowId: id,
                                    workflowName: updatedWorkflow.Name,
                                    beforeData: beforeData,
                                    afterData: afterData,
                                    changedFields: changedFields
                                );
                            }
                        }
                        catch
                        {
                            // Ignore logging errors to avoid affecting main operation
                        }
                    });
                }
            }

            return updateResult;
        }

        /// <summary>
        /// Detect if workflow has actual changes
        /// </summary>
        private bool HasWorkflowChanges(Workflow entity, WorkflowInputDto input)
        {
            // Compare if each field has changes
            if (entity.Name != input.Name) return true;
            if (entity.Description != input.Description) return true;
            if (entity.IsDefault != input.IsDefault) return true;
            if (entity.Status != input.Status) return true;
            if (entity.StartDate != input.StartDate) return true;
            if (entity.EndDate != input.EndDate) return true;
            if (entity.IsActive != input.IsActive) return true;
            if (entity.ConfigJson != input.ConfigJson) return true;

            return false; // No changes
        }

        /// <summary>
        /// Get current user name from UserContext
        /// </summary>
        private string GetCurrentUserName()
        {
            return !string.IsNullOrEmpty(_userContext?.UserName) ? _userContext.UserName : "System";
        }

        /// <summary>
        /// Get current user ID from UserContext
        /// </summary>
        private long GetCurrentUserId()
        {
            if (long.TryParse(_userContext?.UserId, out long userId))
            {
                return userId;
            }
            return 0;
        }

        public async Task<bool> DeleteAsync(long id, bool confirm = false)
        {
            if (!confirm)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Delete operation requires confirmation");
            }

            var entity = await _workflowRepository.GetByIdAsync(id);
            if (entity == null)
            {
                return false;
            }

            // Check if there are associated stages
            var stages = await _stageRepository.GetByWorkflowIdAsync(id);
            if (stages.Any())
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Cannot delete workflow with existing stages");
            }

            var workflowName = entity.Name; // Store name before deletion

            var result = await _workflowRepository.DeleteAsync(entity);
            
            // Clear related cache after successful deletion
            if (result)
            {
                var cacheKey = $"workflow:get_by_id:{id}:{_userContext.AppCode}";
                await _cacheService.RemoveAsync(cacheKey);

                // Log workflow delete operation (fire-and-forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _operationChangeLogService.LogWorkflowDeleteAsync(
                            workflowId: id,
                            workflowName: workflowName,
                            reason: "Workflow deleted via admin portal"
                        );
                    }
                    catch
                    {
                        // Ignore logging errors to avoid affecting main operation
                    }
                });
            }
            
            return result;
        }

        public async Task<WorkflowOutputDto> GetByIdAsync(long id)
        {
            var cacheKey = $"workflow:get_by_id:{id}";
            var cachedResult = await _cacheService.GetAsync<WorkflowOutputDto>(cacheKey);
            if (cachedResult != null)
                return cachedResult;

            var entity = await _workflowRepository.GetWithStagesAsync(id);
            var result = _mapper.Map<WorkflowOutputDto>(entity);

            if (result != null)
            {
                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15));
            }

            return result;
        }

        public async Task<List<WorkflowOutputDto>> GetListAsync()
        {
            // Temporarily disable expired workflow processing to avoid concurrent database operations
            // Debug logging handled by structured logging
            var list = await _workflowRepository.GetAllWorkflowsAsync();
            var result = _mapper.Map<List<WorkflowOutputDto>>(list);

            // 为了优化性能，工作流列表接口不返回Stage数据
            // Stage数据通过单独的接口获取: /api/ow/workflows/{id}/stages
            foreach (var workflow in result)
            {
                workflow.Stages = new List<StageOutputDto>();
            }

            return result;
        }

        /// <summary>
        /// Get all workflows (optimized version)
        /// </summary>
        public async Task<List<WorkflowOutputDto>> GetAllAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Safely get tenant ID
                var tenantId = !string.IsNullOrEmpty(_userContext?.TenantId)
                    ? _userContext.TenantId.ToLowerInvariant()
                    : "default";

                // Build cache key
                var cacheKey = $"ow:workflow:all:{tenantId}";

                // Get directly from database, temporarily skip cache to avoid Redis stream reading issues
                // Debug logging handled by structured logging
                // Temporarily disable expired workflow processing to avoid concurrent database operations
                // Debug logging handled by structured logging
                // Get from database using optimized query
                var list = await _workflowRepository.GetAllOptimizedAsync();
                if (list == null)
                {
                    // Debug logging handled by structured logging
                    return new List<WorkflowOutputDto>();
                }

                var result = _mapper.Map<List<WorkflowOutputDto>>(list);
                if (result == null)
                {
                    // Debug logging handled by structured logging
                    return new List<WorkflowOutputDto>();
                }

                // 为了优化性能，工作流列表接口不返回Stage数据
                // Stage数据通过单独的接口获取: /api/ow/workflows/{id}/stages
                foreach (var workflow in result)
                {
                    workflow.Stages = new List<StageOutputDto>();
                }

                stopwatch.Stop();
                // Debug logging handled by structured logging
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Debug logging handled by structured logging
                // Provide more detailed error information
                var errorMessage = ex.InnerException != null
                    ? $"{ex.Message} -> {ex.InnerException.Message}"
                    : ex.Message;

                throw new CRMException(ErrorCodeEnum.SystemError, $"Error getting all workflows: {errorMessage}");
            }
        }

        public async Task<PagedResult<WorkflowOutputDto>> QueryAsync(WorkflowQueryRequest query)
        {
            // Temporarily disable expired workflow processing to avoid concurrent database operations
            // Debug logging handled by structured logging
            var (items, total) = await _workflowRepository.QueryPagedAsync(query.PageIndex, query.PageSize, query.Name, query.IsActive);
            return new PagedResult<WorkflowOutputDto>
            {
                Items = _mapper.Map<List<WorkflowOutputDto>>(items),
                TotalCount = total
            };
        }

        public async Task<bool> ActivateAsync(long id)
        {
            var entity = await _workflowRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Workflow with ID {id} not found");
            }

            entity.IsActive = true;
            entity.Status = "active";
            var result = await _workflowRepository.UpdateAsync(entity);

            // Clear related cache after successful activation
            if (result)
            {
                var cacheKey = $"workflow:get_by_id:{id}:{_userContext.AppCode}";
                await _cacheService.RemoveAsync(cacheKey);
            }

            return result;
        }

        public async Task<bool> DeactivateAsync(long id)
        {
            var entity = await _workflowRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Workflow with ID {id} not found");
            }

            entity.IsActive = false;
            entity.Status = "inactive";
            var result = await _workflowRepository.UpdateAsync(entity);

            // Clear related cache after successful deactivation
            if (result)
            {
                var cacheKey = $"workflow:get_by_id:{id}:{_userContext.AppCode}";
                await _cacheService.RemoveAsync(cacheKey);
            }

            return result;
        }

        public async Task<bool> SetDefaultAsync(long id)
        {
            return await _workflowRepository.SetDefaultWorkflowAsync(id);
        }

        public async Task<bool> RemoveDefaultAsync(long id)
        {
            return await _workflowRepository.RemoveDefaultWorkflowAsync(id);
        }

        public async Task<long> DuplicateAsync(long id, DuplicateWorkflowInputDto input)
        {
            var originalWorkflow = await _workflowRepository.GetByIdAsync(id);
            if (originalWorkflow == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Workflow with ID {id} not found");
            }

            // Determine base name and ensure uniqueness
            var baseName = string.IsNullOrWhiteSpace(input.Name)
                ? $"{originalWorkflow.Name} (Copy)"
                : input.Name;
            var uniqueName = await EnsureUniqueWorkflowNameAsync(baseName);

            var duplicatedWorkflow = new Workflow
            {
                Name = uniqueName,
                Description = input.Description ?? originalWorkflow.Description,
                StartDate = input.StartDate ?? originalWorkflow.StartDate,
                EndDate = input.EndDate ?? originalWorkflow.EndDate,
                Status = originalWorkflow.Status, // Keep the same status as original
                IsActive = originalWorkflow.IsActive, // Keep the same active status as original
                IsDefault = false, // Duplicated workflows are not default
                Version = 1 // Reset version for duplicated workflow
            };

            // Initialize create information with proper ID and timestamps, including AppCode and TenantId from current context
            duplicatedWorkflow.InitCreateInfo(_userContext);
            AuditHelper.ApplyCreateAudit(duplicatedWorkflow, _operatorContextService);

            var newWorkflowId = await _workflowRepository.InsertReturnSnowflakeIdAsync(duplicatedWorkflow);

            // Duplicate stages if they exist
            var originalStages = await _stageRepository.GetByWorkflowIdAsync(id);
            foreach (var stage in originalStages)
            {
                var duplicatedStage = new Stage
                {
                    WorkflowId = newWorkflowId,
                    Name = stage.Name,
                    PortalName = stage.PortalName,
                    InternalName = stage.InternalName,
                    Description = stage.Description,
                    DefaultAssignedGroup = stage.DefaultAssignedGroup,
                    DefaultAssignee = stage.DefaultAssignee,
                    EstimatedDuration = stage.EstimatedDuration,
                    Order = stage.Order,
                    ChecklistId = stage.ChecklistId,
                    QuestionnaireId = stage.QuestionnaireId,
                    Color = stage.Color,
                    IsActive = stage.IsActive
                };

                // Initialize create information with proper ID and timestamps, including AppCode and TenantId from current context
                duplicatedStage.InitCreateInfo(_userContext);
                AuditHelper.ApplyCreateAudit(duplicatedStage, _operatorContextService);

                await _stageRepository.InsertAsync(duplicatedStage);
            }

            // Log duplicate operation
            try
            {
                await _operationChangeLogService.LogOperationAsync(
                    OperationTypeEnum.WorkflowDuplicate,
                    BusinessModuleEnum.Workflow,
                    newWorkflowId,
                    null, // No onboarding context for workflow duplication
                    null, // No stage context for workflow duplication
                    $"Workflow Duplicated",
                    $"Duplicated workflow '{originalWorkflow.Name}' to '{uniqueName}'",
                    originalWorkflow.Name, // beforeData
                    uniqueName, // afterData
                    new List<string> { "Name", "Description", "StartDate", "EndDate", "Stages" },
                    JsonConvert.SerializeObject(new
                    {
                        SourceId = id,
                        SourceName = originalWorkflow.Name,
                        NewId = newWorkflowId,
                        NewName = uniqueName,
                        DuplicatedStagesCount = originalStages.Count()
                    }),
                    OperationStatusEnum.Success
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log workflow duplicate operation for workflow {WorkflowId}", newWorkflowId);
            }

            return newWorkflowId;
        }

        /// <summary>
        /// 处理过期的工作流，将其设置为inactive
        /// </summary>
        public async Task<int> ProcessExpiredWorkflowsAsync()
        {
            try
            {
                var expiredWorkflows = await _workflowRepository.GetExpiredActiveWorkflowsAsync();

                if (!expiredWorkflows.Any())
                {
                    return 0;
                }

                int processedCount = 0;
                foreach (var workflow in expiredWorkflows)
                {
                    workflow.Status = "inactive";
                    workflow.IsActive = false;
                    workflow.ModifyDate = DateTimeOffset.UtcNow;
                    workflow.ModifyBy = GetCurrentUserName();

                    var updated = await _workflowRepository.UpdateAsync(workflow);
                    if (updated)
                    {
                        processedCount++;

                        // Clear related cache after successful update
                        var cacheKey = $"workflow:get_by_id:{workflow.Id}:{workflow.AppCode}";
                        await _cacheService.RemoveAsync(cacheKey);

                        // 记录日志
                        // Debug logging handled by structured logging has been set to inactive due to expiration. End Date: {workflow.EndDate}");
                    }
                }

                return processedCount;
            }
            catch (Exception ex)
            {
                throw new CRMException(ErrorCodeEnum.SystemError, $"Error processing expired workflows: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取即将过期的工作流（提�?天提醒）
        /// </summary>
        public async Task<List<WorkflowOutputDto>> GetExpiringWorkflowsAsync(int daysAhead = 7)
        {
            try
            {
                var expiringWorkflows = await _workflowRepository.GetExpiringWorkflowsAsync(daysAhead);
                return _mapper.Map<List<WorkflowOutputDto>>(expiringWorkflows);
            }
            catch (Exception ex)
            {
                throw new CRMException(ErrorCodeEnum.SystemError, $"Error getting expiring workflows: {ex.Message}");
            }
        }

        public async Task<Stream> ExportDetailedToExcelAsync(long workflowId)
        {
            var workflow = await _workflowRepository.GetWithStagesAsync(workflowId);
            if (workflow == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Workflow with ID {workflowId} not found");
            }

            // 使用专门?WorkflowExcelExportHelper 来生成详细格式的 Excel
            return WorkflowExcelExportHelper.ExportToExcel(workflow);
        }

        public async Task<Stream> ExportMultipleDetailedToExcelAsync(WorkflowExportSearch search)
        {
            List<Workflow> workflows;

            if (search.IsAll)
            {
                workflows = await _workflowRepository.GetListAsync();
            }
            else if (search.Ids?.Any() == true)
            {
                workflows = new List<Workflow>();
                foreach (var id in search.Ids)
                {
                    var workflow = await _workflowRepository.GetWithStagesAsync(id);
                    if (workflow != null)
                    {
                        workflows.Add(workflow);
                    }
                }
            }
            else
            {
                workflows = await _workflowRepository.GetActiveWorkflowsAsync();
            }

            // 使用专门?WorkflowExcelExportHelper 来生成详细格式的 Excel
            return WorkflowExcelExportHelper.ExportMultipleToExcel(workflows);
        }

        /// <summary>
        /// Create workflow from version with stages
        /// </summary>
        public async Task<long> CreateFromVersionAsync(CreateWorkflowFromVersionInputDto input)
        {
            // 验证原始工作流是否存?
            var originalWorkflow = await _workflowRepository.GetByIdAsync(input.OriginalWorkflowId);
            if (originalWorkflow == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Original workflow with ID {input.OriginalWorkflowId} not found");
            }

            // 验证版本是否存在
            // var version = await _workflowVersionRepository.GetVersionDetailAsync(input.VersionId); // Removed
            // if (version == null || version.OriginalWorkflowId != input.OriginalWorkflowId) // Removed
            // {
            //     throw new CRMException(ErrorCodeEnum.NotFound, $"Version with ID {input.VersionId} not found for workflow {input.OriginalWorkflowId}"); // Removed
            // } // Removed

            // 验证新工作流名称唯一?
            if (await _workflowRepository.ExistsNameAsync(input.Name))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, $"Workflow name '{input.Name}' already exists");
            }

            // 如果设置为默认，需要先取消其他默认工作?
            if (input.IsDefault)
            {
                var existingDefault = await _workflowRepository.GetDefaultWorkflowAsync();
                if (existingDefault != null)
                {
                    await _workflowRepository.RemoveDefaultWorkflowAsync(existingDefault.Id);
                }
            }

            // 创建新工作流
            var newWorkflow = new Workflow
            {
                Name = input.Name,
                Description = input.Description,
                IsDefault = input.IsDefault,
                Status = input.Status,
                StartDate = input.StartDate == default ? DateTimeOffset.UtcNow : input.StartDate,
                EndDate = input.EndDate,
                Version = 1,
                IsActive = input.IsActive
            };

            await _workflowRepository.InsertAsync(newWorkflow);

            // 创建阶段
            if (input.Stages?.Any() == true)
            {
                foreach (var stageInput in input.Stages.OrderBy(x => x.Order))
                {
                    var newStage = new Stage
                    {
                        WorkflowId = newWorkflow.Id,
                        Name = stageInput.Name,
                        Description = stageInput.Description,
                        DefaultAssignedGroup = stageInput.DefaultAssignedGroup,
                        DefaultAssignee = stageInput.DefaultAssignee != null && stageInput.DefaultAssignee.Any() 
                            ? string.Join(",", stageInput.DefaultAssignee) 
                            : null,
                        EstimatedDuration = stageInput.EstimatedDuration,
                        Order = stageInput.Order,
                        ChecklistId = stageInput.ChecklistId,
                        QuestionnaireId = stageInput.QuestionnaireId,
                        Color = stageInput.Color,
                        IsActive = true
                    };

                    await _stageRepository.InsertAsync(newStage);
                }
            }

            // 创建版本历史记录
            // await _workflowVersionRepository.CreateVersionHistoryAsync(newWorkflow, "Created", $"Created from version {version.Version} of workflow '{version.Name}'"); // Removed

            return newWorkflow.Id;
        }

        /// <summary>
        /// Manually create a new workflow version without automatic changes
        /// </summary>
        public async Task<bool> CreateNewVersionAsync(long id, string changeReason = null)
        {
            var workflow = await _workflowRepository.GetByIdAsync(id);
            if (workflow == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Workflow with ID {id} not found");
            }

            // Get current stages for version snapshot
            var currentStages = await _stageRepository.GetByWorkflowIdAsync(id);

            // Update workflow start date to current date when creating new version
            // Use today's date at start of day in local timezone
            var today = DateTime.Today;
            // Preserve original local date semantics but convert to UTC offset
            workflow.StartDate = new DateTimeOffset(today, TimeSpan.Zero);

            // Update workflow version number
            workflow.Version += 1;
            workflow.InitUpdateInfo(_userContext);
            AuditHelper.ApplyModifyAudit(workflow, _operatorContextService);
            var result = await _workflowRepository.UpdateAsync(workflow);

            // Clear related cache after successful version creation
            if (result)
            {
                var cacheKey = $"workflow:get_by_id:{id}:{_userContext.AppCode}";
                await _cacheService.RemoveAsync(cacheKey);
            }

            // Create version history record (including stage snapshot) after updating workflow
            var reason = !string.IsNullOrEmpty(changeReason)
                ? changeReason
                : "Manual version creation";

            // await _workflowVersionRepository.CreateVersionHistoryWithStagesAsync( // Removed
            //     workflow, // Removed
            //     currentStages, // Removed
            //     "Manual Version", // Removed
            //     $"{reason} - Workflow manually versioned to {workflow.Version}"); // Removed

            return result;
        }

        /// <summary>
        /// Parse components from JSON string
        /// </summary>
        private static List<FlowFlex.Domain.Shared.Models.StageComponent> ParseComponentsFromJson(string componentsJson)
        {
            if (string.IsNullOrEmpty(componentsJson))
            {
                return new List<FlowFlex.Domain.Shared.Models.StageComponent>();
            }

            try
            {
                var components = JsonSerializer.Deserialize<List<FlowFlex.Domain.Shared.Models.StageComponent>>(componentsJson);
                return components ?? new List<FlowFlex.Domain.Shared.Models.StageComponent>();
            }
            catch (JsonException)
            {
                return new List<FlowFlex.Domain.Shared.Models.StageComponent>();
            }
        }

        /// <summary>
        /// Determine if we should update the IsDefault status based on the context
        /// For AI-generated updates, we typically want to preserve the existing state
        /// </summary>
        private bool ShouldUpdateDefaultStatus(WorkflowInputDto input, Workflow entity)
        {
            // If this is likely an AI-generated update (has stages but no explicit default handling),
            // we should preserve the existing IsDefault state
            if (input.Stages != null && input.Stages.Any())
            {
                // This looks like an AI workflow modification - preserve existing default state
                return false;
            }

            // For regular user updates, allow IsDefault changes
            return true;
        }

        /// <summary>
        /// Ensure unique workflow name by appending number suffix if needed
        /// </summary>
        private async Task<string> EnsureUniqueWorkflowNameAsync(string baseName)
        {
            var originalName = baseName;
            var counter = 1;
            var currentName = baseName;

            while (true)
            {
                var exists = await _workflowRepository.ExistsNameAsync(currentName);
                if (!exists)
                {
                    return currentName;
                }

                counter++;
                currentName = $"{originalName} ({counter})";
            }
        }

        // 缓存相关方法已移除
    }
}