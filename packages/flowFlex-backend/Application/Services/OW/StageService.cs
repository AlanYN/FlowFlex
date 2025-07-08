using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.Stage;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Application.Contracts.Models;
using FlowFlex.Domain.Shared;
using System.Text.Json;
using System;
using System.Linq;
using FlowFlex.Domain.Repository;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Application.Services.OW.Extensions;

namespace FlowFlex.Application.Service.OW
{
    /// <summary>
    /// Stage service implementation
    /// </summary>
    public class StageService : IStageService, IScopedService
    {
        private readonly IStageRepository _stageRepository;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IWorkflowVersionRepository _workflowVersionRepository;
        private readonly IMapper _mapper;
        private readonly IOperationChangeLogService _operationLogService;
        private readonly UserContext _userContext;

        // Cache key constants
        private const string STAGE_CACHE_PREFIX = "ow:stage";

        public StageService(IStageRepository stageRepository, IWorkflowRepository workflowRepository, IWorkflowVersionRepository workflowVersionRepository, IMapper mapper, IOperationChangeLogService operationLogService, UserContext userContext)
        {
            _stageRepository = stageRepository;
            _workflowRepository = workflowRepository;
            _workflowVersionRepository = workflowVersionRepository;
            _mapper = mapper;
            _operationLogService = operationLogService;
            _userContext = userContext;
        }

        public async Task<long> CreateAsync(StageInputDto input)
        {
            // Validate if workflow exists
            var workflow = await _workflowRepository.GetByIdAsync(input.WorkflowId);
            if (workflow == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Workflow with ID {input.WorkflowId} not found");
            }

            // Validate stage name uniqueness within workflow
            if (await _stageRepository.ExistsNameInWorkflowAsync(input.WorkflowId, input.Name))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, $"Stage name '{input.Name}' already exists in this workflow");
            }

            var entity = _mapper.Map<Stage>(input);

            // If no order specified, automatically set to last
            if (entity.Order == 0)
            {
                entity.Order = await _stageRepository.GetNextOrderAsync(input.WorkflowId);
            }

            // Set workflow version
            entity.WorkflowVersion = workflow.Version.ToString();

            // Initialize create information with proper ID and timestamps
            entity.InitCreateInfo(_userContext);

            await _stageRepository.InsertAsync(entity);

            // Create new WorkflowVersion (after adding stage)
            await CreateWorkflowVersionForStageChangeAsync(entity.WorkflowId, $"Stage '{entity.Name}' created");

            return entity.Id;
        }

        /// <summary>
        /// Update stage
        /// </summary>
        public async Task<bool> UpdateAsync(long id, StageInputDto input)
        {
            // Get current stage information
            var stage = await _stageRepository.GetByIdAsync(id);
            if (stage == null)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Stage not found");
            }

            // Validate stage name uniqueness within workflow (excluding current stage)
            if (await _stageRepository.IsNameExistsInWorkflowAsync(input.WorkflowId, input.Name, id))
            {
                throw new CRMException(ErrorCodeEnum.CustomError,
                    $"Stage name '{input.Name}' already exists in this workflow");
            }

            // Map update data
            _mapper.Map(input, stage);
            
            // Initialize update information with proper timestamps
            stage.InitUpdateInfo(_userContext);

            // Update database
            var result = await _stageRepository.UpdateAsync(stage);

            if (result)
            {
                // Cache cleanup removed

                // 记录操作日志 - 使用 OperationChangeLogService 来正确处理 JSONB 字段
                var beforeData = JsonSerializer.Serialize(new
                {
                    Name = stage.Name,
                    WorkflowId = stage.WorkflowId,
                    Description = stage.Description,
                    Order = stage.Order
                });
                
                var afterData = JsonSerializer.Serialize(new
                {
                    Name = input.Name,
                    WorkflowId = input.WorkflowId,
                    Description = input.Description,
                    Order = input.Order
                });
                
                var changedFields = new[]
                {
                    stage.Name != input.Name ? "Name" : null,
                    stage.WorkflowId != input.WorkflowId ? "WorkflowId" : null,
                    stage.Description != input.Description ? "Description" : null,
                    stage.Order != input.Order ? "Order" : null
                }.Where(x => x != null).ToList();

                await _operationLogService.LogOperationAsync(
                    OperationTypeEnum.OnboardingStatusChange,
                    BusinessModuleEnum.Stage,
                    id,
                    null, // onboardingId
                    null, // stageId
                    "Stage Update",
                    "Stage information updated",
                    beforeData,
                    afterData,
                    changedFields,
                    null, // extendedData
                    OperationStatusEnum.Success
                );
            }

            return result;
        }

        /// <summary>
        /// Detect if stage has actual changes
        /// </summary>
        private bool HasStageChanges(Stage entity, StageInputDto input)
        {
            // Compare if each field has changes
            if (entity.WorkflowId != input.WorkflowId) return true;
            if (entity.Name != input.Name) return true;
            if (entity.PortalName != input.PortalName) return true;
            if (entity.InternalName != input.InternalName) return true;
            if (entity.Description != input.Description) return true;
            if (entity.DefaultAssignedGroup != input.DefaultAssignedGroup) return true;
            if (entity.DefaultAssignee != input.DefaultAssignee) return true;
            if (entity.EstimatedDuration != input.EstimatedDuration) return true;
            if (entity.Order != input.Order) return true;
            if (entity.ChecklistId != input.ChecklistId) return true;
            if (entity.QuestionnaireId != input.QuestionnaireId) return true;
            if (entity.Color != input.Color) return true;
            if (entity.RequiredFieldsJson != input.RequiredFieldsJson) return true;

            // Compare StaticFields list (through JSON serialization comparison)
            var inputStaticFieldsJson = input.StaticFields != null && input.StaticFields.Any()
                ? JsonSerializer.Serialize(input.StaticFields.OrderBy(x => x))
                : null;
            var currentStaticFieldsJson = !string.IsNullOrEmpty(entity.StaticFieldsJson)
                ? entity.StaticFieldsJson
                : null;

            // If current entity's StaticFields property has value, use it to generate JSON for comparison
            if (entity.StaticFields != null && entity.StaticFields.Any())
            {
                currentStaticFieldsJson = JsonSerializer.Serialize(entity.StaticFields.OrderBy(x => x));
            }

            if (currentStaticFieldsJson != inputStaticFieldsJson)
            {
                return true;
            }

            return false; // No changes
        }

        /// <summary>
        /// Create new workflow version for stage changes
        /// </summary>
        private async Task CreateWorkflowVersionForStageChangeAsync(long workflowId, string changeReason)
        {
            // Get workflow information
            var workflow = await _workflowRepository.GetByIdAsync(workflowId);
            if (workflow == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Workflow with ID {workflowId} not found");
            }

            // Get all current stages for version snapshot
            var currentStages = await _stageRepository.GetByWorkflowIdAsync(workflowId);

            // Create version history record (including stage snapshot)
            await _workflowVersionRepository.CreateVersionHistoryWithStagesAsync(
                workflow,
                currentStages,
                "Stage Updated",
                $"{changeReason} - Workflow updated to version {workflow.Version + 1}");

            // Update workflow version number
            workflow.Version += 1;
            await _workflowRepository.UpdateAsync(workflow);
        }

        public async Task<bool> DeleteAsync(long id, bool confirm = false)
        {
            if (!confirm)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Delete operation requires confirmation");
            }

            var entity = await _stageRepository.GetByIdAsync(id);
            if (entity == null)
            {
                return false;
            }

            // TODO: 检查是否有关联的Onboarding实例
            // 如果有正在进行的Onboarding实例在此阶段，不允许删除

            var workflowId = entity.WorkflowId;
            var stageName = entity.Name;

            // 先删除阶段
            var deleteResult = await _stageRepository.DeleteAsync(entity);

            // 如果删除成功，创建新的 WorkflowVersion（删除阶段后，保存变更后的状态）
            if (deleteResult)
            {
                await CreateWorkflowVersionForStageChangeAsync(workflowId, $"Stage '{stageName}' deleted");
            }

            return deleteResult;
        }

        public async Task<StageOutputDto> GetByIdAsync(long id)
        {
            var entity = await _stageRepository.GetByIdAsync(id);
            return _mapper.Map<StageOutputDto>(entity);
        }

        public async Task<List<StageOutputDto>> GetListByWorkflowIdAsync(long workflowId)
        {
            var list = await _stageRepository.GetByWorkflowIdAsync(workflowId);
            return _mapper.Map<List<StageOutputDto>>(list);
        }

        /// <summary>
        /// Get all stages (no pagination) - Optimized version
        /// </summary>
        public async Task<List<StageOutputDto>> GetAllAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Safely get tenant ID
                var tenantId = _userContext?.TenantId ?? "default";
                if (string.IsNullOrWhiteSpace(tenantId))
                {
                    tenantId = "default";
                }

                Console.WriteLine($"[StageService.GetAllAsync] Using tenant ID: '{tenantId}'");

                // Build cache key using safe tenant ID
                var cacheKey = $"ow:stage:all:{tenantId.ToLowerInvariant()}";

                // Redis cache removed, query database directly

                // Get from database using optimized query
                Console.WriteLine("[StageService.GetAllAsync] Fetching from database...");
                var stages = await _stageRepository.GetAllOptimizedAsync();
                var result = _mapper.Map<List<StageOutputDto>>(stages);

                // Cache functionality removed

                stopwatch.Stop();
                Console.WriteLine($"[StageService.GetAllAsync] Database query completed: {stopwatch.ElapsedMilliseconds}ms, count: {result.Count}");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"[StageService.GetAllAsync] Error: {ex.Message}, elapsed: {stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"[StageService.GetAllAsync] Stack trace: {ex.StackTrace}");
                throw new CRMException(ErrorCodeEnum.SystemError, $"Error getting all stages: {ex.Message}");
            }
        }

        public async Task<PagedResult<StageOutputDto>> QueryAsync(StageQueryRequest query)
        {
            var (items, total) = await _stageRepository.QueryPagedAsync(query.PageIndex, query.PageSize, query.WorkflowId, query.Name);
            return new PagedResult<StageOutputDto>
            {
                Items = _mapper.Map<List<StageOutputDto>>(items),
                TotalCount = total
            };
        }

        public async Task<bool> SortStagesAsync(SortStagesInputDto input)
        {
            // Validate all stages belong to the same workflow
            var stages = await _stageRepository.GetByWorkflowIdAsync(input.WorkflowId);
            var stageIds = input.StageOrders.Select(x => x.StageId).ToList();

            if (!stageIds.All(id => stages.Any(s => s.Id == id)))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Some stages do not belong to the specified workflow");
            }

            // Batch update order
            var orderUpdates = input.StageOrders.Select(x => (x.StageId, x.Order)).ToList();
            var result = await _stageRepository.BatchUpdateOrderAsync(orderUpdates);

            // If order update is successful, create new WorkflowVersion
            if (result)
            {
                await CreateWorkflowVersionForStageChangeAsync(input.WorkflowId, "Stages reordered");
            }

            return result;
        }

        public async Task<long> CombineStagesAsync(CombineStagesInputDto input)
        {
            if (input.StageIds.Count < 2)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "At least 2 stages are required for combination");
            }

            // 获取要合并的阶段
            var stagesToCombine = new List<Stage>();
            foreach (var stageId in input.StageIds)
            {
                var stage = await _stageRepository.GetByIdAsync(stageId);
                if (stage == null)
                {
                    throw new CRMException(ErrorCodeEnum.NotFound, $"Stage with ID {stageId} not found");
                }
                stagesToCombine.Add(stage);
            }

            // 验证所有阶段都属于同一个工作流
            var workflowId = stagesToCombine.First().WorkflowId;
            if (!stagesToCombine.All(s => s.WorkflowId == workflowId))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "All stages must belong to the same workflow");
            }

            // 验证新阶段名称唯一性
            if (await _stageRepository.ExistsNameInWorkflowAsync(workflowId, input.NewStageName))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, $"Stage name '{input.NewStageName}' already exists in this workflow");
            }

            // 创建新的合并阶段
            var newStage = new Stage
            {
                WorkflowId = workflowId,
                Name = input.NewStageName,
                Description = input.Description,
                DefaultAssignedGroup = input.DefaultAssignedGroup,
                DefaultAssignee = input.DefaultAssignee,
                EstimatedDuration = input.EstimatedDuration,
                Order = stagesToCombine.Min(s => s.Order), // 使用最小的排序值
                Color = input.Color,
                WorkflowVersion = stagesToCombine.First().WorkflowVersion,
                IsActive = true
            };

            await _stageRepository.InsertAsync(newStage);

            // 删除原有阶段
            await _stageRepository.BatchDeleteAsync(input.StageIds);

            // 创建新的 WorkflowVersion（合并阶段后）
            await CreateWorkflowVersionForStageChangeAsync(workflowId, $"Stages combined into '{input.NewStageName}'");

            return newStage.Id;
        }

        public async Task<bool> SetColorAsync(long id, string color)
        {
            var entity = await _stageRepository.GetByIdAsync(id);
            if (entity == null)
            {
                return false;
            }

            entity.Color = color;
            return await _stageRepository.UpdateAsync(entity);
        }

        public async Task<bool> UpdateRequiredFieldsAsync(long id, UpdateRequiredFieldsInputDto input)
        {
            var entity = await _stageRepository.GetByIdAsync(id);
            if (entity == null)
            {
                return false;
            }

            // 将必填字段转换为JSON格式存储
            var requiredFieldsData = new
            {
                Fields = input.RequiredFields.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(f => f.Trim()).ToArray(),
                Description = input.FieldDescription,
                UpdatedAt = DateTimeOffset.Now
            };

            entity.RequiredFieldsJson = JsonSerializer.Serialize(requiredFieldsData);
            return await _stageRepository.UpdateAsync(entity);
        }

        public async Task<long> DuplicateAsync(long id, DuplicateStageInputDto input)
        {
            var sourceStage = await _stageRepository.GetByIdAsync(id);
            if (sourceStage == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Source stage with ID {id} not found");
            }

            var targetWorkflowId = input.TargetWorkflowId ?? sourceStage.WorkflowId;

            // 验证目标工作流存在
            var targetWorkflow = await _workflowRepository.GetByIdAsync(targetWorkflowId);
            if (targetWorkflow == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Target workflow with ID {targetWorkflowId} not found");
            }

            // 验证新阶段名称唯一性
            if (await _stageRepository.ExistsNameInWorkflowAsync(targetWorkflowId, input.Name))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, $"Stage name '{input.Name}' already exists in the target workflow");
            }

            // 创建新阶段
            var newStage = new Stage
            {
                WorkflowId = targetWorkflowId,
                Name = input.Name,
                Description = input.Description,
                DefaultAssignedGroup = sourceStage.DefaultAssignedGroup,
                DefaultAssignee = sourceStage.DefaultAssignee,
                EstimatedDuration = sourceStage.EstimatedDuration,
                Order = await _stageRepository.GetNextOrderAsync(targetWorkflowId),
                ChecklistId = sourceStage.ChecklistId,
                QuestionnaireId = sourceStage.QuestionnaireId,
                Color = sourceStage.Color,
                RequiredFieldsJson = sourceStage.RequiredFieldsJson,
                WorkflowVersion = targetWorkflow.Version.ToString(),
                IsActive = sourceStage.IsActive
            };

            await _stageRepository.InsertAsync(newStage);
            return newStage.Id;
        }

        #region StageContent 相关功能实现

        public async Task<StageContentDto> GetStageContentAsync(long stageId, long onboardingId)
        {
            // TODO: 实现获取Stage完整内容的逻辑
            // 这里需要整合静态字段、Checklist、问卷、文件、备注和日志
            throw new NotImplementedException("GetStageContentAsync will be implemented in next phase");
        }

        public async Task<bool> UpdateStaticFieldsAsync(long stageId, long onboardingId, StageStaticFieldsDto staticFields)
        {
            // TODO: 实现更新Stage静态字段的逻辑
            throw new NotImplementedException("UpdateStaticFieldsAsync will be implemented in next phase");
        }

        public async Task<bool> UpdateChecklistTaskAsync(long stageId, long onboardingId, long taskId, bool isCompleted, string completionNotes = null)
        {
            // TODO: 实现更新Checklist任务状态的逻辑
            throw new NotImplementedException("UpdateChecklistTaskAsync will be implemented in next phase");
        }

        public async Task<bool> SubmitQuestionnaireAnswerAsync(long stageId, long onboardingId, long questionId, object answer)
        {
            // TODO: 实现提交问卷答案的逻辑
            throw new NotImplementedException("SubmitQuestionnaireAnswerAsync will be implemented in next phase");
        }

        public async Task<StageFileDto> UploadStageFileAsync(long stageId, long onboardingId, string fileName, byte[] fileContent, string fileCategory = null)
        {
            // TODO: 实现Stage文件上传的逻辑
            throw new NotImplementedException("UploadStageFileAsync will be implemented in next phase");
        }

        public async Task<bool> DeleteStageFileAsync(long stageId, long onboardingId, long fileId)
        {
            // TODO: 实现Stage文件删除的逻辑
            throw new NotImplementedException("DeleteStageFileAsync will be implemented in next phase");
        }

        public async Task<StageCompletionValidationDto> ValidateStageCompletionAsync(long stageId, long onboardingId)
        {
            // TODO: 实现Stage完成条件验证的逻辑
            throw new NotImplementedException("ValidateStageCompletionAsync will be implemented in next phase");
        }

        public async Task<bool> CompleteStageAsync(long stageId, long onboardingId, string completionNotes = null)
        {
            // TODO: 实现Stage完成的逻辑
            throw new NotImplementedException("CompleteStageAsync will be implemented in next phase");
        }

        public async Task<StageLogsDto> GetStageLogsAsync(long stageId, long onboardingId, int pageIndex = 1, int pageSize = 20)
        {
            // 这里只做示例实现，实际应根据业务表结构和日志表结构调整
            // 查询与该阶段相关的操作日志
            var pagedResult = await _operationLogService.GetOperationLogsAsync(onboardingId, stageId, null, pageIndex, pageSize);
            
            var result = new StageLogsDto
            {
                Logs = pagedResult.Items.Select(log => new StageLogDto
                {
                    LogId = log.Id,
                    OperationType = log.OperationType,
                    Description = log.OperationDescription,
                    Details = log.ExtendedData ?? string.Empty,
                    OperatedBy = log.OperatorName,
                    OperatedTime = log.OperationTime,
                    OperatedTimeDisplay = log.OperationTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    Result = log.OperationStatus,
                    LogTypeTag = log.OperationType
                }).ToList(),
                TotalCount = pagedResult.TotalCount,
                HasMore = pagedResult.TotalCount > pageIndex * pageSize
            };
            return result;
        }

        public async Task<bool> AddStageNoteAsync(long stageId, long onboardingId, string noteContent, bool isPrivate = false)
        {
            // TODO: 实现添加Stage备注的逻辑
            throw new NotImplementedException("AddStageNoteAsync will be implemented in next phase");
        }

        public async Task<StageNotesDto> GetStageNotesAsync(long stageId, long onboardingId, int pageIndex = 1, int pageSize = 20)
        {
            // TODO: 实现获取Stage备注列表的逻辑
            throw new NotImplementedException("GetStageNotesAsync will be implemented in next phase");
        }

        public async Task<bool> UpdateStageNoteAsync(long stageId, long onboardingId, long noteId, string noteContent)
        {
            // TODO: 实现更新Stage备注的逻辑
            throw new NotImplementedException("UpdateStageNoteAsync will be implemented in next phase");
        }

        public async Task<bool> DeleteStageNoteAsync(long stageId, long onboardingId, long noteId)
        {
            // TODO: 实现删除Stage备注的逻辑
            throw new NotImplementedException("DeleteStageNoteAsync will be implemented in next phase");
        }

        #endregion

        // 缓存清理方法已移除
    }
}