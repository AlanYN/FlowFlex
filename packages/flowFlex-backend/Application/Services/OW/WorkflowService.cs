using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.Workflow;
using FlowFlex.Application.Contracts.Dtos.OW.Stage;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;

using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Exceptions;
using System.Text.Json;
using System.Text;
using System.IO;
using System.Linq;
using Item.Excel.Lib;
using FlowFlex.Application.Helpers;
using FlowFlex.Domain.Shared.Models;
using Item.Redis;
using FlowFlex.Application.Services.OW.Extensions;

namespace FlowFlex.Application.Service.OW
{
    public class WorkflowService : IWorkflowService, IScopedService
    {
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IStageRepository _stageRepository;
        private readonly IWorkflowVersionRepository _workflowVersionRepository;
        private readonly IStageVersionRepository _stageVersionRepository;
        private readonly IMapper _mapper;
        private readonly UserContext _userContext;

        public WorkflowService(IWorkflowRepository workflowRepository, IStageRepository stageRepository, IWorkflowVersionRepository workflowVersionRepository, IStageVersionRepository stageVersionRepository, IMapper mapper, UserContext userContext)
        {
            _workflowRepository = workflowRepository;
            _stageRepository = stageRepository;
            _workflowVersionRepository = workflowVersionRepository;
            _stageVersionRepository = stageVersionRepository;
            _mapper = mapper;
            _userContext = userContext;
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
            entity.StartDate = input.StartDate == default ? DateTimeOffset.Now : input.StartDate;

            // Initialize create information with proper ID and timestamps
            entity.InitCreateInfo(_userContext);

            await _workflowRepository.InsertAsync(entity);

            // Create version history record
            await _workflowVersionRepository.CreateVersionHistoryAsync(entity, "Created", "Initial workflow creation");

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

            // If set as default, need to cancel other default workflows first
            if (input.IsDefault && !entity.IsDefault)
            {
                await _workflowRepository.SetDefaultWorkflowAsync(id);
            }
            else if (!input.IsDefault && entity.IsDefault)
            {
                await _workflowRepository.RemoveDefaultWorkflowAsync(id);
            }

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

            var updateResult = await _workflowRepository.UpdateAsync(entity);

            // If there are changes and update is successful, create version history record (save post-change information)
            if (hasChanges && updateResult)
            {
                // Get updated stage list for version snapshot
                var updatedStages = await _stageRepository.GetByWorkflowIdAsync(id);

                // Create version history record after update (including stage snapshot) - save post-change information
                await _workflowVersionRepository.CreateVersionHistoryWithStagesAsync(entity, updatedStages, "Updated", $"Workflow updated to version {entity.Version}");
            }

            // Cache cleanup removed

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

            return await _workflowRepository.DeleteAsync(entity);
        }

        public async Task<WorkflowOutputDto> GetByIdAsync(long id)
        {
            var entity = await _workflowRepository.GetWithStagesAsync(id);
            return _mapper.Map<WorkflowOutputDto>(entity);
        }

        public async Task<List<WorkflowOutputDto>> GetListAsync()
        {
            // Temporarily disable expired workflow processing to avoid concurrent database operations
            // Debug logging handled by structured logging
            var list = await _workflowRepository.GetAllWorkflowsAsync();
            return _mapper.Map<List<WorkflowOutputDto>>(list);
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

            // Cache cleanup removed

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

            // Cache cleanup removed

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

            var duplicatedWorkflow = new Workflow
            {
                Name = input.Name,
                Description = input.Description ?? originalWorkflow.Description,
                StartDate = input.StartDate ?? originalWorkflow.StartDate,
                EndDate = input.EndDate ?? originalWorkflow.EndDate,
                Status = originalWorkflow.Status, // Keep the same status as original
                IsActive = originalWorkflow.IsActive, // Keep the same active status as original
                IsDefault = false, // Duplicated workflows are not default
                Version = 1, // Reset version for duplicated workflow
                TenantId = originalWorkflow.TenantId,
                CreateBy = GetCurrentUserName(),
                ModifyBy = GetCurrentUserName(),
                CreateUserId = GetCurrentUserId(),
                ModifyUserId = GetCurrentUserId()
            };

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


                    WorkflowVersion = stage.WorkflowVersion,
                    IsActive = stage.IsActive,
                    TenantId = stage.TenantId,
                    CreateBy = GetCurrentUserName(),
                    ModifyBy = GetCurrentUserName(),
                    CreateUserId = GetCurrentUserId(),
                    ModifyUserId = GetCurrentUserId()
                };

                // Generate snowflake ID for the duplicated stage
                duplicatedStage.InitNewId();

                await _stageRepository.InsertAsync(duplicatedStage);
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

        public async Task<List<WorkflowVersionDto>> GetVersionHistoryAsync(long id)
        {
            var workflow = await _workflowRepository.GetByIdAsync(id);
            if (workflow == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Workflow with ID {id} not found");
            }

            var versions = await _workflowVersionRepository.GetVersionHistoryAsync(id);

            // 如果没有版本历史记录，为现有工作流创建初始版�?
            if (!versions.Any())
            {
                var stages = await _stageRepository.GetByWorkflowIdAsync(id);
                await _workflowVersionRepository.CreateVersionHistoryWithStagesAsync(workflow, stages, "Initial", "Initial version created for existing workflow");

                // 重新获取版本历史
                versions = await _workflowVersionRepository.GetVersionHistoryAsync(id);
            }

            return _mapper.Map<List<WorkflowVersionDto>>(versions);
        }

        public async Task<Stream> ExportDetailedToExcelAsync(long workflowId)
        {
            var workflow = await _workflowRepository.GetWithStagesAsync(workflowId);
            if (workflow == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Workflow with ID {workflowId} not found");
            }

            // 使用专门�?WorkflowExcelExportHelper 来生成详细格式的 Excel
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

            // 使用专门�?WorkflowExcelExportHelper 来生成详细格式的 Excel
            return WorkflowExcelExportHelper.ExportMultipleToExcel(workflows);
        }

        /// <summary>
        /// Get stages by workflow version id
        /// </summary>
        public async Task<List<StageOutputDto>> GetStagesByVersionIdAsync(long workflowId, long versionId)
        {
            // 验证工作流是否存�?
            var workflow = await _workflowRepository.GetByIdAsync(workflowId);
            if (workflow == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Workflow with ID {workflowId} not found");
            }

            // 验证版本是否存在
            var version = await _workflowVersionRepository.GetVersionDetailAsync(versionId);
            if (version == null || version.OriginalWorkflowId != workflowId)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Version with ID {versionId} not found for workflow {workflowId}");
            }

            // 获取版本快照中的阶段列表
            var stageVersions = await _stageVersionRepository.GetByWorkflowVersionIdAsync(versionId);

            // 转换为StageOutputDto
            var stageOutputs = stageVersions.Select(sv => new StageOutputDto
            {
                Id = sv.OriginalStageId,
                WorkflowId = workflowId,
                Name = sv.Name,
                Description = sv.Description,
                DefaultAssignedGroup = sv.DefaultAssignedGroup,
                DefaultAssignee = sv.DefaultAssignee,
                EstimatedDuration = sv.EstimatedDuration,
                Order = sv.OrderIndex,
                ChecklistId = sv.ChecklistId,
                QuestionnaireId = sv.QuestionnaireId,
                Color = sv.Color,
                
                WorkflowVersion = sv.WorkflowVersion,
                IsActive = sv.IsActive,
                CreateDate = sv.CreateDate,
                CreateBy = sv.CreateBy
            }).ToList();

            return stageOutputs;
        }

        /// <summary>
        /// Get workflow version detail with stages
        /// </summary>
        public async Task<WorkflowVersionDetailDto> GetVersionDetailAsync(long workflowId, long versionId)
        {
            // 验证工作流是否存�?
            var workflow = await _workflowRepository.GetByIdAsync(workflowId);
            if (workflow == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Workflow with ID {workflowId} not found");
            }

            // 获取版本详情
            var version = await _workflowVersionRepository.GetVersionDetailAsync(versionId);
            if (version == null || version.OriginalWorkflowId != workflowId)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Version with ID {versionId} not found for workflow {workflowId}");
            }

            // 获取版本的阶段列�?
            var stages = await GetStagesByVersionIdAsync(workflowId, versionId);

            // 构建返回结果
            var result = new WorkflowVersionDetailDto
            {
                Id = version.Id,
                OriginalWorkflowId = version.OriginalWorkflowId,
                Name = version.Name,
                Description = version.Description,
                Status = version.Status,
                IsDefault = version.IsDefault,
                StartDate = version.StartDate,
                EndDate = version.EndDate,
                Version = version.Version,
                IsActive = version.IsActive,
                CreatedBy = version.CreateBy,
                CreatedAt = version.CreateDate,
                ChangeType = version.ChangeType,
                ChangeReason = version.ChangeReason,
                Stages = stages
            };

            return result;
        }

        /// <summary>
        /// Create workflow from version with stages
        /// </summary>
        public async Task<long> CreateFromVersionAsync(CreateWorkflowFromVersionInputDto input)
        {
            // 验证原始工作流是否存�?
            var originalWorkflow = await _workflowRepository.GetByIdAsync(input.OriginalWorkflowId);
            if (originalWorkflow == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Original workflow with ID {input.OriginalWorkflowId} not found");
            }

            // 验证版本是否存在
            var version = await _workflowVersionRepository.GetVersionDetailAsync(input.VersionId);
            if (version == null || version.OriginalWorkflowId != input.OriginalWorkflowId)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Version with ID {input.VersionId} not found for workflow {input.OriginalWorkflowId}");
            }

            // 验证新工作流名称唯一�?
            if (await _workflowRepository.ExistsNameAsync(input.Name))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, $"Workflow name '{input.Name}' already exists");
            }

            // 如果设置为默认，需要先取消其他默认工作�?
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
                StartDate = input.StartDate == default ? DateTimeOffset.Now : input.StartDate,
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
                        DefaultAssignee = stageInput.DefaultAssignee,
                        EstimatedDuration = stageInput.EstimatedDuration,
                        Order = stageInput.Order,
                        ChecklistId = stageInput.ChecklistId,
                        QuestionnaireId = stageInput.QuestionnaireId,
                        Color = stageInput.Color,
    
                        WorkflowVersion = "1",
                        IsActive = true
                    };

                    await _stageRepository.InsertAsync(newStage);
                }
            }

            // 创建版本历史记录
            await _workflowVersionRepository.CreateVersionHistoryAsync(newWorkflow, "Created", $"Created from version {version.Version} of workflow '{version.Name}'");

            return newWorkflow.Id;
        }

        // 缓存相关方法已移�?
    }
}