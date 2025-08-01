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
        private readonly IMapper _mapper;
        private readonly IOperationChangeLogService _operationLogService;
        private readonly IStageAssignmentSyncService _syncService;
        private readonly IChecklistService _checklistService;
        private readonly IQuestionnaireService _questionnaireService;
        private readonly UserContext _userContext;

        // Cache key constants
        private const string STAGE_CACHE_PREFIX = "ow:stage";

        public StageService(IStageRepository stageRepository, IWorkflowRepository workflowRepository, IMapper mapper, IOperationChangeLogService operationLogService, IStageAssignmentSyncService syncService, IChecklistService checklistService, IQuestionnaireService questionnaireService, UserContext userContext)
        {
            _stageRepository = stageRepository;
            _workflowRepository = workflowRepository;
            _mapper = mapper;
            _operationLogService = operationLogService;
            _syncService = syncService;
            _checklistService = checklistService;
            _questionnaireService = questionnaireService;
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

            // Initialize create information with proper ID and timestamps
            entity.InitCreateInfo(_userContext);

            await _stageRepository.InsertAsync(entity);

            // Create new WorkflowVersion (after adding stage) - Disabled automatic version creation
            // await CreateWorkflowVersionForStageChangeAsync(entity.WorkflowId, $"Stage '{entity.Name}' created");

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

            // Extract old components for sync comparison (before mapping)
            List<StageComponent> oldComponents = new List<StageComponent>();

            if (!string.IsNullOrEmpty(stage.ComponentsJson))
            {
                try
                {
                    oldComponents = JsonSerializer.Deserialize<List<StageComponent>>(stage.ComponentsJson) ?? new List<StageComponent>();
                }
                catch (JsonException)
                {
                    oldComponents = new List<StageComponent>();
                }
            }

            var oldChecklistIds = oldComponents
                .Where(c => c.Key == "checklist")
                .SelectMany(c => c.ChecklistIds ?? new List<long>())
                .Distinct()
                .ToList();

            var oldQuestionnaireIds = oldComponents
                .Where(c => c.Key == "questionnaires")
                .SelectMany(c => c.QuestionnaireIds ?? new List<long>())
                .Distinct()
                .ToList();

            // Fill component names before mapping
            if (input.Components != null && input.Components.Any())
            {
                await FillComponentNamesAsync(input.Components);
            }

            // Map update data
            _mapper.Map(input, stage);

            // Extract new components for sync comparison (after mapping)
            var newChecklistIds = new List<long>();
            var newQuestionnaireIds = new List<long>();

            if (input.Components != null && input.Components.Any())
            {
                newChecklistIds = input.Components
                    .Where(c => c.Key == "checklist")
                    .SelectMany(c => c.ChecklistIds ?? new List<long>())
                    .Distinct()
                    .ToList();

                newQuestionnaireIds = input.Components
                    .Where(c => c.Key == "questionnaires")
                    .SelectMany(c => c.QuestionnaireIds ?? new List<long>())
                    .Distinct()
                    .ToList();
            }

            // Initialize update information with proper timestamps
            stage.InitUpdateInfo(_userContext);

            // Update database
            var result = await _stageRepository.UpdateAsync(stage);

            if (result)
            {
                // Check if there are actual changes to sync
                var hasChecklistChanges = !oldChecklistIds.SequenceEqual(newChecklistIds);
                var hasQuestionnaireChanges = !oldQuestionnaireIds.SequenceEqual(newQuestionnaireIds);

                if (hasChecklistChanges || hasQuestionnaireChanges)
                {
                    // Sync assignments with checklists and questionnaires if components changed
                    try
                    {
                        await _syncService.SyncAssignmentsFromStageComponentsAsync(
                            id,
                            stage.WorkflowId,
                            oldChecklistIds,
                            newChecklistIds,
                            oldQuestionnaireIds,
                            newQuestionnaireIds);
                    }
                    catch (Exception ex)
                    {
                        // Log sync error but don't fail the operation
                        // The stage update succeeded, sync is a secondary operation
                        await _operationLogService.LogOperationAsync(
                            OperationTypeEnum.OnboardingStatusChange,
                            BusinessModuleEnum.Stage,
                            id,
                            null, // onboardingId
                            null, // stageId
                            "Stage Update Assignment Sync Error",
                            "Failed to sync assignments with checklist/questionnaire during stage update",
                            null, // beforeData
                            ex.Message, // afterData
                            new List<string> { "AssignmentSync" },
                            null, // extendedData
                            OperationStatusEnum.Failed
                        );
                    }
                }

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
                    tenantId = "DEFAULT";
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

            // If order update is successful, create new WorkflowVersion - Disabled automatic version creation
            // if (result)
            // {
            //     await CreateWorkflowVersionForStageChangeAsync(input.WorkflowId, "Stages reordered");
            // }

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
                IsActive = true
            };

            await _stageRepository.InsertAsync(newStage);

            // Delete original stages
            await _stageRepository.BatchDeleteAsync(input.StageIds);

            // Create new WorkflowVersion (after stage combination) - Disabled automatic version creation
            // await CreateWorkflowVersionForStageChangeAsync(workflowId, $"Stages combined into '{input.NewStageName}'");

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
                IsActive = true,
                // Copy tenant and app information from source stage
                TenantId = sourceStage.TenantId,
                AppCode = sourceStage.AppCode
            };

            // Initialize create information with proper ID and timestamps
            newStage.InitCreateInfo(_userContext);

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

            // Ensure all components have proper default values and fill names
            await FillComponentNamesAsync(input.Components);

            // Get old components for comparison
            List<StageComponent> oldComponents = new List<StageComponent>();
            if (!string.IsNullOrEmpty(entity.ComponentsJson))
            {
                try
                {
                    oldComponents = JsonSerializer.Deserialize<List<StageComponent>>(entity.ComponentsJson) ?? new List<StageComponent>();
                }
                catch (JsonException)
                {
                    oldComponents = new List<StageComponent>();
                }
            }

            // Extract old and new IDs for sync
            var oldChecklistIds = oldComponents
                .Where(c => c.Key == "checklist")
                .SelectMany(c => c.ChecklistIds ?? new List<long>())
                .Distinct()
                .ToList();

            var newChecklistIds = input.Components
                .Where(c => c.Key == "checklist")
                .SelectMany(c => c.ChecklistIds ?? new List<long>())
                .Distinct()
                .ToList();

            var oldQuestionnaireIds = oldComponents
                .Where(c => c.Key == "questionnaires")
                .SelectMany(c => c.QuestionnaireIds ?? new List<long>())
                .Distinct()
                .ToList();

            var newQuestionnaireIds = input.Components
                .Where(c => c.Key == "questionnaires")
                .SelectMany(c => c.QuestionnaireIds ?? new List<long>())
                .Distinct()
                .ToList();

            // Update components JSON
            entity.ComponentsJson = JsonSerializer.Serialize(input.Components);
            entity.Components = input.Components;
            entity.InitUpdateInfo(_userContext);

            var result = await _stageRepository.UpdateAsync(entity);

            if (result)
            {
                // Sync assignments with checklists and questionnaires
                try
                {
                    await _syncService.SyncAssignmentsFromStageComponentsAsync(
                        id,
                        entity.WorkflowId,
                        oldChecklistIds,
                        newChecklistIds,
                        oldQuestionnaireIds,
                        newQuestionnaireIds);
                }
                catch (Exception ex)
                {
                    // Log sync error but don't fail the operation
                    // The stage components update succeeded, sync is a secondary operation
                    await _operationLogService.LogOperationAsync(
                        OperationTypeEnum.OnboardingStatusChange,
                        BusinessModuleEnum.Stage,
                        id,
                        null, // onboardingId
                        null, // stageId
                        "Stage Components Assignment Sync Error",
                        "Failed to sync assignments with checklist/questionnaire",
                        null, // beforeData
                        ex.Message, // afterData
                        new List<string> { "AssignmentSync" },
                        null, // extendedData
                        OperationStatusEnum.Failed
                    );
                }

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
                if (components == null || !components.Any())
                {
                    return new List<StageComponent>();
                }

                // Check if any component is missing names and fill them
                bool needsNameFilling = components.Any(c => 
                    (c.ChecklistIds?.Any() == true && (c.ChecklistNames?.Count != c.ChecklistIds.Count)) ||
                    (c.QuestionnaireIds?.Any() == true && (c.QuestionnaireNames?.Count != c.QuestionnaireIds.Count))
                );

                if (needsNameFilling)
                {
                    await FillComponentNamesAsync(components);
                    
                    // Update the entity with filled names for future use
                    try
                    {
                        entity.ComponentsJson = JsonSerializer.Serialize(components);
                        entity.Components = components;
                        await _stageRepository.UpdateAsync(entity);
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail - names were filled for this request
                        await _operationLogService.LogOperationAsync(
                            OperationTypeEnum.OnboardingStatusChange,
                            BusinessModuleEnum.Stage,
                            id,
                            null,
                            null,
                            "Component Names Fill Update Error",
                            $"Failed to update stage with filled component names: {ex.Message}",
                            null,
                            null,
                            new List<string> { "ComponentNamesFill" },
                            null,
                            OperationStatusEnum.Failed
                        );
                    }
                }

                return components;
            }
            catch (JsonException)
            {
                // If JSON is invalid, return empty list instead of default components
                return new List<StageComponent>();
            }
        }

        /// <summary>
        /// Manually sync assignments between stage components and checklist/questionnaire assignments
        /// </summary>
        public async Task<bool> SyncAssignmentsFromStageComponentsAsync(long stageId, long workflowId, List<long> oldChecklistIds, List<long> newChecklistIds, List<long> oldQuestionnaireIds, List<long> newQuestionnaireIds)
        {
            return await _syncService.SyncAssignmentsFromStageComponentsAsync(
                stageId,
                workflowId,
                oldChecklistIds,
                newChecklistIds,
                oldQuestionnaireIds,
                newQuestionnaireIds);
        }

        #endregion

        /// <summary>
        /// Fill component names by querying checklist and questionnaire services
        /// </summary>
        private async Task FillComponentNamesAsync(List<StageComponent> components)
        {
            if (components == null || !components.Any())
                return;

            // Collect all checklist and questionnaire IDs
            var allChecklistIds = new List<long>();
            var allQuestionnaireIds = new List<long>();

            foreach (var component in components)
            {
                // Ensure all components have proper default values
                component.StaticFields ??= new List<string>();
                component.ChecklistIds ??= new List<long>();
                component.QuestionnaireIds ??= new List<long>();
                component.ChecklistNames ??= new List<string>();
                component.QuestionnaireNames ??= new List<string>();

                if (component.ChecklistIds?.Any() == true)
                {
                    allChecklistIds.AddRange(component.ChecklistIds);
                }

                if (component.QuestionnaireIds?.Any() == true)
                {
                    allQuestionnaireIds.AddRange(component.QuestionnaireIds);
                }
            }

            // Remove duplicates
            allChecklistIds = allChecklistIds.Distinct().ToList();
            allQuestionnaireIds = allQuestionnaireIds.Distinct().ToList();

            // Batch query names
            var checklistNameMap = new Dictionary<long, string>();
            var questionnaireNameMap = new Dictionary<long, string>();

            if (allChecklistIds.Any())
            {
                try
                {
                    var checklists = await _checklistService.GetByIdsAsync(allChecklistIds);
                    checklistNameMap = checklists.ToDictionary(c => c.Id, c => c.Name);
                }
                catch (Exception ex)
                {
                    // Log error but continue - names will be empty if service fails
                    // This ensures the operation doesn't fail completely
                    await _operationLogService.LogOperationAsync(
                        OperationTypeEnum.OnboardingStatusChange,
                        BusinessModuleEnum.Stage,
                        0,
                        null,
                        null,
                        "Checklist Names Fetch Error",
                        $"Failed to fetch checklist names: {ex.Message}",
                        null,
                        JsonSerializer.Serialize(allChecklistIds),
                        new List<string> { "ChecklistNamesFetch" },
                        null,
                        OperationStatusEnum.Failed
                    );
                }
            }

            if (allQuestionnaireIds.Any())
            {
                try
                {
                    var questionnaires = await _questionnaireService.GetByIdsAsync(allQuestionnaireIds);
                    questionnaireNameMap = questionnaires.ToDictionary(q => q.Id, q => q.Name);
                }
                catch (Exception ex)
                {
                    // Log error but continue - names will be empty if service fails
                    await _operationLogService.LogOperationAsync(
                        OperationTypeEnum.OnboardingStatusChange,
                        BusinessModuleEnum.Stage,
                        0,
                        null,
                        null,
                        "Questionnaire Names Fetch Error",
                        $"Failed to fetch questionnaire names: {ex.Message}",
                        null,
                        JsonSerializer.Serialize(allQuestionnaireIds),
                        new List<string> { "QuestionnaireNamesFetch" },
                        null,
                        OperationStatusEnum.Failed
                    );
                }
            }

            // Fill names for each component
            foreach (var component in components)
            {
                // Fill checklist names
                if (component.ChecklistIds?.Any() == true)
                {
                    component.ChecklistNames = component.ChecklistIds
                        .Select(id => checklistNameMap.TryGetValue(id, out var name) ? name : $"Checklist {id}")
                        .ToList();
                }

                // Fill questionnaire names
                if (component.QuestionnaireIds?.Any() == true)
                {
                    component.QuestionnaireNames = component.QuestionnaireIds
                        .Select(id => questionnaireNameMap.TryGetValue(id, out var name) ? name : $"Questionnaire {id}")
                        .ToList();
                }
            }
        }

        // Cache cleanup methods have been removed
    }
}