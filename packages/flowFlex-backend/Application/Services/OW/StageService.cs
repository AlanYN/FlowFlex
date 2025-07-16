using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.Stage;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;

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

                // Log operation - use OperationChangeLogService to properly handle JSONB fields
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

            // Check for related Onboarding instances - future enhancement
            // If there are ongoing Onboarding instances in this stage, deletion is not allowed

            var workflowId = entity.WorkflowId;
            var stageName = entity.Name;

            // Delete stage first
            var deleteResult = await _stageRepository.DeleteAsync(entity);

            // If deletion is successful, create new WorkflowVersion (save changed state after stage deletion)
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
                // Debug logging handled by structured logging
                // Build cache key using safe tenant ID
                var cacheKey = $"ow:stage:all:{tenantId.ToLowerInvariant()}";

                // Redis cache removed, query database directly

                // Get from database using optimized query
                // Debug logging handled by structured logging
                var stages = await _stageRepository.GetAllOptimizedAsync();
                var result = _mapper.Map<List<StageOutputDto>>(stages);

                // Cache functionality removed

                stopwatch.Stop();
                // Debug logging handled by structured logging
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Debug logging handled by structured logging
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

            // Get stages to be combined
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

            // Validate all stages belong to the same workflow
            var workflowId = stagesToCombine.First().WorkflowId;
            if (!stagesToCombine.All(s => s.WorkflowId == workflowId))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "All stages must belong to the same workflow");
            }

            // Validate new stage name uniqueness
            if (await _stageRepository.ExistsNameInWorkflowAsync(workflowId, input.NewStageName))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, $"Stage name '{input.NewStageName}' already exists in this workflow");
            }

            // Create new combined stage
            var newStage = new Stage
            {
                WorkflowId = workflowId,
                Name = input.NewStageName,
                Description = input.Description,
                DefaultAssignedGroup = input.DefaultAssignedGroup,
                DefaultAssignee = input.DefaultAssignee,
                EstimatedDuration = input.EstimatedDuration,
                Order = stagesToCombine.Min(s => s.Order), // Use minimum order number
                Color = input.Color,
                WorkflowVersion = stagesToCombine.First().WorkflowVersion,
                IsActive = true
            };

            await _stageRepository.InsertAsync(newStage);

            // Delete original stages
            await _stageRepository.BatchDeleteAsync(input.StageIds);

            // Create new WorkflowVersion (after stage combination)
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



        public async Task<long> DuplicateAsync(long id, DuplicateStageInputDto input)
        {
            var sourceStage = await _stageRepository.GetByIdAsync(id);
            if (sourceStage == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Source stage with ID {id} not found");
            }

            var targetWorkflowId = input.TargetWorkflowId ?? sourceStage.WorkflowId;

            // Validate target workflow exists
            var targetWorkflow = await _workflowRepository.GetByIdAsync(targetWorkflowId);
            if (targetWorkflow == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Target workflow with ID {targetWorkflowId} not found");
            }

            // Validate new stage name uniqueness
            if (await _stageRepository.ExistsNameInWorkflowAsync(targetWorkflowId, input.Name))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, $"Stage name '{input.Name}' already exists in the target workflow");
            }

            // Create new stage
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

                WorkflowVersion = targetWorkflow.Version.ToString(),
                IsActive = sourceStage.IsActive
            }
            ;

            await _stageRepository.InsertAsync(newStage);
            return newStage.Id;
        }

        #region StageContent Related Function Implementation

        public async Task<StageContentDto> GetStageContentAsync(long stageId, long onboardingId)
        {
            // Get Stage complete content logic - future enhancement
            // This needs to integrate static fields, Checklist, questionnaire, files, notes and logs
            throw new NotImplementedException("GetStageContentAsync will be implemented in next phase");
        }



        public async Task<bool> UpdateChecklistTaskAsync(long stageId, long onboardingId, long taskId, bool isCompleted, string completionNotes = null)
        {
            // Update Checklist task status logic - future enhancement
            throw new NotImplementedException("UpdateChecklistTaskAsync will be implemented in next phase");
        }

        public async Task<bool> SubmitQuestionnaireAnswerAsync(long stageId, long onboardingId, long questionId, object answer)
        {
            // Submit questionnaire answers logic - future enhancement
            throw new NotImplementedException("SubmitQuestionnaireAnswerAsync will be implemented in next phase");
        }

        public async Task<StageFileDto> UploadStageFileAsync(long stageId, long onboardingId, string fileName, byte[] fileContent, string fileCategory = null)
        {
            // Stage file upload logic - future enhancement
            throw new NotImplementedException("UploadStageFileAsync will be implemented in next phase");
        }

        public async Task<bool> DeleteStageFileAsync(long stageId, long onboardingId, long fileId)
        {
            // Stage file deletion logic - future enhancement
            throw new NotImplementedException("DeleteStageFileAsync will be implemented in next phase");
        }

        public async Task<StageCompletionValidationDto> ValidateStageCompletionAsync(long stageId, long onboardingId)
        {
            // Stage completion condition validation logic - future enhancement
            throw new NotImplementedException("ValidateStageCompletionAsync will be implemented in next phase");
        }

        public async Task<bool> CompleteStageAsync(long stageId, long onboardingId, string completionNotes = null)
        {
            // Stage completion logic - future enhancement
            throw new NotImplementedException("CompleteStageAsync will be implemented in next phase");
        }

        public async Task<StageLogsDto> GetStageLogsAsync(long stageId, long onboardingId, int pageIndex = 1, int pageSize = 20)
        {
            // This is only a sample implementation, should be adjusted according to actual business table structure and log table structure
            // Query operation logs related to this stage
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
            // Add Stage notes logic - future enhancement
            throw new NotImplementedException("AddStageNoteAsync will be implemented in next phase");
        }

        public async Task<StageNotesDto> GetStageNotesAsync(long stageId, long onboardingId, int pageIndex = 1, int pageSize = 20)
        {
            // Get Stage notes list logic - future enhancement
            throw new NotImplementedException("GetStageNotesAsync will be implemented in next phase");
        }

        public async Task<bool> UpdateStageNoteAsync(long stageId, long onboardingId, long noteId, string noteContent)
        {
            // Update Stage notes logic - future enhancement
            throw new NotImplementedException("UpdateStageNoteAsync will be implemented in next phase");
        }

        public async Task<bool> DeleteStageNoteAsync(long stageId, long onboardingId, long noteId)
        {
            // Delete Stage notes logic - future enhancement
            throw new NotImplementedException("DeleteStageNoteAsync will be implemented in next phase");
        }

        /// <summary>
        /// Update stage components configuration
        /// </summary>
        public async Task<bool> UpdateComponentsAsync(long id, UpdateStageComponentsInputDto input)
        {
            var entity = await _stageRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Stage not found");
            }

            // Validate components
            var validComponentKeys = new[] { "fields", "checklist", "questionnaires", "files" };
            var invalidComponents = input.Components.Where(c => !validComponentKeys.Contains(c.Key)).ToList();
            if (invalidComponents.Any())
            {
                var invalidKeys = string.Join(", ", invalidComponents.Select(c => c.Key));
                throw new CRMException(ErrorCodeEnum.BusinessError, $"Invalid component keys: {invalidKeys}. Valid keys are: {string.Join(", ", validComponentKeys)}");
            }

            // Validate order uniqueness
            var duplicateOrders = input.Components.GroupBy(c => c.Order).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicateOrders.Any())
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, $"Duplicate order values found: {string.Join(", ", duplicateOrders)}");
            }

            // Ensure all components have proper default values
            foreach (var component in input.Components)
            {
                component.StaticFields ??= new List<string>();
                component.ChecklistIds ??= new List<long>();
                component.QuestionnaireIds ??= new List<long>();
            }

            // Update components JSON
            entity.ComponentsJson = JsonSerializer.Serialize(input.Components);
            entity.Components = input.Components;
            entity.InitUpdateInfo(_userContext);

            var result = await _stageRepository.UpdateAsync(entity);

            if (result)
            {
                // Log operation
                await _operationLogService.LogOperationAsync(
                    OperationTypeEnum.OnboardingStatusChange,
                    BusinessModuleEnum.Stage,
                    id,
                    null, // onboardingId
                    null, // stageId
                    "Stage Components Update",
                    "Stage components configuration updated",
                    null, // beforeData
                    JsonSerializer.Serialize(input.Components), // afterData
                    new List<string> { "Components" },
                    null, // extendedData
                    OperationStatusEnum.Success
                );
            }

            return result;
        }

        /// <summary>
        /// Get stage components configuration
        /// </summary>
        public async Task<List<StageComponent>> GetComponentsAsync(long id)
        {
            var entity = await _stageRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Stage not found");
            }

            // If no components configured, return empty list instead of default components
            if (string.IsNullOrEmpty(entity.ComponentsJson))
            {
                return new List<StageComponent>();
            }

            try
            {
                var components = JsonSerializer.Deserialize<List<StageComponent>>(entity.ComponentsJson);
                return components ?? new List<StageComponent>();
            }
            catch (JsonException)
            {
                // If JSON is invalid, return empty list instead of default components
                return new List<StageComponent>();
            }
        }

        #endregion

        // Cache cleanup methods have been removed
    }
}