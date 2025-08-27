using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.Stage;
using FlowFlex.Application.Contracts.Dtos.OW.QuestionnaireAnswer;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;

using FlowFlex.Domain.Shared;
using System.Text.Json;
using System.Text.Json.Serialization;
using System;
using System.Linq;
using FlowFlex.Domain.Repository;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Application.Services.OW.Extensions;
using System.Text.RegularExpressions;
using SqlSugar;

namespace FlowFlex.Application.Service.OW
{
    /// <summary>
    /// Stage service implementation
    /// </summary>
    public class StageService : IStageService, IScopedService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            PropertyNameCaseInsensitive = true
        };
        private readonly IStageRepository _stageRepository;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IMapper _mapper;
        private readonly IStagesProgressSyncService _stagesProgressSyncService;
        private readonly IChecklistService _checklistService;
        private readonly IQuestionnaireService _questionnaireService;
        private readonly IQuestionnaireAnswerService _questionnaireAnswerService;
        private readonly IAIService _aiService;
        private readonly IChecklistTaskCompletionRepository _checklistTaskCompletionRepository;
        private readonly UserContext _userContext;
        private readonly IComponentMappingService _mappingService;
        private readonly ISqlSugarClient _db;

        // Cache key constants
        private const string STAGE_CACHE_PREFIX = "ow:stage";

        public StageService(IStageRepository stageRepository, IWorkflowRepository workflowRepository, IMapper mapper, IStagesProgressSyncService stagesProgressSyncService, IChecklistService checklistService, IQuestionnaireService questionnaireService, IQuestionnaireAnswerService questionnaireAnswerService, IAIService aiService, IChecklistTaskCompletionRepository checklistTaskCompletionRepository, UserContext userContext, IComponentMappingService mappingService, ISqlSugarClient db)
        {
            _stageRepository = stageRepository;
            _workflowRepository = workflowRepository;
            _mapper = mapper;
            _stagesProgressSyncService = stagesProgressSyncService;
            _checklistService = checklistService;
            _questionnaireService = questionnaireService;
            _questionnaireAnswerService = questionnaireAnswerService;
            _aiService = aiService;
            _checklistTaskCompletionRepository = checklistTaskCompletionRepository;
            _userContext = userContext;
            _mappingService = mappingService;
            _db = db;
        }

        public async Task<long> CreateAsync(StageInputDto input)
        {
            // Use transaction to ensure data consistency
            var result = await _db.Ado.UseTranAsync(async () =>
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

                // Validate component uniqueness within the same workflow
                if (input.Components != null && input.Components.Any())
                {
                    await ValidateComponentUniquenessInWorkflowAsync(input.WorkflowId, null, input.Components);
                }

                // Initialize create information with proper ID and timestamps
                entity.InitCreateInfo(_userContext);

                await _stageRepository.InsertAsync(entity);

                // Sync component mappings within the same transaction
                if (input.Components != null && input.Components.Any())
                {
                    var hasChecklist = input.Components.Any(c => c.Key == "checklist" && c.ChecklistIds?.Any() == true);
                    var hasQuestionnaires = input.Components.Any(c => c.Key == "questionnaires" && c.QuestionnaireIds?.Any() == true);

                    if (hasChecklist || hasQuestionnaires)
                    {
                        try
                        {
                            await _mappingService.SyncStageMappingsInTransactionAsync(entity.Id, _db);
                            Console.WriteLine($"[StageService] Synced stage mappings for new stage {entity.Id} within transaction");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[StageService] Error syncing stage mappings for new stage {entity.Id}: {ex.Message}");
                            throw; // Re-throw to rollback transaction
                        }
                    }
                }

                return entity.Id;
            });
            return result.Data;
        }

        /// <summary>
        /// Update stage with transaction support and data consistency validation
        /// </summary>
        public async Task<bool> UpdateAsync(long id, StageInputDto input)
        {
            // Get current stage information first (outside transaction for validation)
            var stage = await _stageRepository.GetByIdAsync(id);
            if (stage == null)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Stage not found");
            }

            // Validate stage name uniqueness within workflow (excluding current stage)
            if (await _stageRepository.IsNameExistsInWorkflowAsync(stage.WorkflowId, input.Name, id))
            {
                throw new CRMException(ErrorCodeEnum.CustomError,
                    $"Stage name '{input.Name}' already exists in this workflow");
            }

            // Fill component names before validation
            if (input.Components != null && input.Components.Any())
            {
                await FillComponentNamesAsync(input.Components);
            }

            // Validate component uniqueness within the same workflow (exclude current stage) - BEFORE transaction
            if (input.Components != null && input.Components.Any())
            {
                await ValidateComponentUniquenessInWorkflowAsync(stage.WorkflowId, id, input.Components);
            }

            // Use transaction to ensure data consistency
            var transactionResult = await _db.Ado.UseTranAsync(async () =>
            {
                // Re-get stage information inside transaction (to ensure consistency)
                var stageInTransaction = await _stageRepository.GetByIdAsync(id);
                if (stageInTransaction == null)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, "Stage not found");
                }

                // Extract old components for sync comparison (before mapping)
                List<StageComponent> oldComponents = new List<StageComponent>();

                if (!string.IsNullOrEmpty(stageInTransaction.ComponentsJson))
                {
                    try
                    {
                        oldComponents = JsonSerializer.Deserialize<List<StageComponent>>(stageInTransaction.ComponentsJson, _jsonOptions) ?? new List<StageComponent>();
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
                    .OrderBy(x => x)
                    .ToList();

                var oldQuestionnaireIds = oldComponents
                    .Where(c => c.Key == "questionnaires")
                    .SelectMany(c => c.QuestionnaireIds ?? new List<long>())
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                // Map update data
                _mapper.Map(input, stageInTransaction);

                // Extract new components for sync comparison (after mapping)
                var newChecklistIds = new List<long>();
                var newQuestionnaireIds = new List<long>();

                if (input.Components != null && input.Components.Any())
                {
                    newChecklistIds = input.Components
                        .Where(c => c.Key == "checklist")
                        .SelectMany(c => c.ChecklistIds ?? new List<long>())
                        .Distinct()
                        .OrderBy(x => x)
                        .ToList();

                    newQuestionnaireIds = input.Components
                        .Where(c => c.Key == "questionnaires")
                        .SelectMany(c => c.QuestionnaireIds ?? new List<long>())
                        .Distinct()
                        .OrderBy(x => x)
                        .ToList();
                }

                // Initialize update information with proper timestamps
                stageInTransaction.InitUpdateInfo(_userContext);

                // Update database within transaction
                var result = await _stageRepository.UpdateAsync(stageInTransaction);

                if (result)
                {
                    // Check if mappings need synchronization using the new method
                    var needsSync = await _mappingService.NeedsSyncAsync(id, newChecklistIds, newQuestionnaireIds);

                    // Sync component mappings if there are changes within the same transaction
                    if (needsSync)
                    {
                        try
                        {
                            await _mappingService.SyncStageMappingsInTransactionAsync(id, _db);
                            Console.WriteLine($"[StageService] Synced stage mappings for stage {id} after update within transaction");

                            // Validate data consistency after sync
                            var isConsistent = await _mappingService.ValidateStageComponentConsistencyAsync(id);
                            if (!isConsistent)
                            {
                                Console.WriteLine($"[StageService] WARNING: Data inconsistency detected after sync for stage {id}");
                                // Could throw exception to rollback transaction if strict consistency is required
                                // throw new CRMException(ErrorCodeEnum.SystemError, "Data consistency validation failed after mapping sync");
                            }
                            else
                            {
                                Console.WriteLine($"[StageService] Data consistency validated for stage {id}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[StageService] Error syncing stage mappings for stage {id}: {ex.Message}");
                            throw; // Re-throw to rollback transaction
                        }

                        // Trigger async AI summary generation if components changed (outside transaction)
                        _ = Task.Run(async () => await GenerateAISummaryInBackgroundAsync(id, "Stage components updated"));
                    }

                    // Sync stages progress for all onboardings in this workflow (outside transaction)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _stagesProgressSyncService.SyncAfterStageUpdateAsync(stageInTransaction.WorkflowId, id);
                        }
                        catch
                        {
                            // Ignore sync errors to avoid impacting the main operation
                        }
                    });
                }

                return result;
            });
            return transactionResult.Data;
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

            if (deleteResult)
            {
                // Sync stages progress for all onboardings in this workflow
                // This is done asynchronously to avoid impacting the main operation
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _stagesProgressSyncService.SyncAfterStageDeleteAsync(workflowId, id);
                    }
                    catch
                    {
                        // Ignore sync errors to avoid impacting the main operation
                    }
                });
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

            if (result)
            {
                // Sync stages progress for all onboardings in this workflow
                // This is done asynchronously to avoid impacting the main operation
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _stagesProgressSyncService.SyncAfterStagesSortAsync(input.WorkflowId, stageIds);
                    }
                    catch
                    {
                        // Ignore sync errors to avoid impacting the main operation
                    }
                });
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
                IsActive = true
            };

            await _stageRepository.InsertAsync(newStage);

            // Delete original stages
            await _stageRepository.BatchDeleteAsync(input.StageIds);

            // Create new WorkflowVersion (after stage combination) - Disabled automatic version creation
            // await CreateWorkflowVersionForStageChangeAsync(workflowId, $"Stages combined into '{input.NewStageName}'");

            // Sync stages progress for all onboardings in this workflow
            // This is done asynchronously to avoid impacting the main operation
            _ = Task.Run(async () =>
            {
                try
                {
                    await _stagesProgressSyncService.SyncAfterStagesCombineAsync(workflowId, input.StageIds, newStage.Id);
                }
                catch
                {
                    // Ignore sync errors to avoid impacting the main operation
                }
            });

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
        /// Update stage components configuration with transaction support
        /// </summary>
        public async Task<bool> UpdateComponentsAsync(long id, UpdateStageComponentsInputDto input)
        {
            // Use transaction to ensure data consistency
            var transactionResult = await _db.Ado.UseTranAsync(async () =>
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

                // Validate component uniqueness within the same workflow (exclude current stage)
                await ValidateComponentUniquenessInWorkflowAsync(entity.WorkflowId, id, input.Components);

                // Extract new IDs for sync comparison
                var newChecklistIds = input.Components
                    .Where(c => c.Key == "checklist")
                    .SelectMany(c => c.ChecklistIds ?? new List<long>())
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                var newQuestionnaireIds = input.Components
                    .Where(c => c.Key == "questionnaires")
                    .SelectMany(c => c.QuestionnaireIds ?? new List<long>())
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                // Update components
                // ComponentsJson 将由 AutoMapper 的 SerializeComponents 统一生成，避免重复/多层序列化
                entity.Components = input.Components;
                entity.InitUpdateInfo(_userContext);

                var result = await _stageRepository.UpdateAsync(entity);

                if (result)
                {
                    // Check if mappings need synchronization
                    var needsSync = await _mappingService.NeedsSyncAsync(id, newChecklistIds, newQuestionnaireIds);

                    // Sync component mappings if there are changes within the same transaction
                    if (needsSync)
                    {
                        try
                        {
                            await _mappingService.SyncStageMappingsInTransactionAsync(id, _db);
                            Console.WriteLine($"[StageService] Synced stage mappings for stage {id} after component update within transaction");

                            // Validate data consistency after sync
                            var isConsistent = await _mappingService.ValidateStageComponentConsistencyAsync(id);
                            if (!isConsistent)
                            {
                                Console.WriteLine($"[StageService] WARNING: Data inconsistency detected after component update sync for stage {id}");
                                // Could throw exception to rollback transaction if strict consistency is required
                                // throw new CRMException(ErrorCodeEnum.SystemError, "Data consistency validation failed after component mapping sync");
                            }
                            else
                            {
                                Console.WriteLine($"[StageService] Data consistency validated for stage {id} after component update");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[StageService] Error syncing stage mappings for stage {id}: {ex.Message}");
                            throw; // Re-throw to rollback transaction
                        }

                        // Trigger async AI summary generation for component updates (outside transaction)
                        _ = Task.Run(async () => await GenerateAISummaryInBackgroundAsync(id, "Stage components updated"));
                    }
                }

                return result;
            });
            return transactionResult.Data;
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
            Console.WriteLine($"[DEBUG] GetComponentsAsync - Stage {id}, ComponentsJson: {entity.ComponentsJson ?? "NULL"}");
            if (string.IsNullOrEmpty(entity.ComponentsJson))
            {
                Console.WriteLine($"[DEBUG] ComponentsJson is null or empty for stage {id}");
                return new List<StageComponent>();
            }

            try
            {
                // Handle double-escaped JSON string
                var jsonString = entity.ComponentsJson;
                
                // If the string starts and ends with quotes, it's likely double-escaped
                if (jsonString.StartsWith("\"") && jsonString.EndsWith("\""))
                {
                    Console.WriteLine($"[DEBUG] Detected double-escaped JSON, unescaping...");
                    // Remove outer quotes and unescape
                    jsonString = JsonSerializer.Deserialize<string>(jsonString) ?? jsonString.Trim('"');
                    Console.WriteLine($"[DEBUG] Unescaped JSON: {jsonString}");
                }

                // Clean problematic escape sequences (e.g., \u0026 and stray \&)
                var cleanedJson = CleanJsonString(jsonString);
                Console.WriteLine($"[DEBUG] Cleaned JSON (preview): {cleanedJson.Substring(0, Math.Min(200, cleanedJson.Length))}...");

                List<StageComponent> components = null;
                try
                {
                    components = JsonSerializer.Deserialize<List<StageComponent>>(cleanedJson, _jsonOptions);
                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine($"[ERROR] Direct JSON deserialization failed: {jsonEx.Message}");
                    // As a last resort, try to parse array items individually
                    try
                    {
                        Console.WriteLine("[DEBUG] Trying manual JSON parsing...");
                        using var doc = JsonDocument.Parse(cleanedJson);
                        JsonElement root = doc.RootElement;
                        if (root.ValueKind == JsonValueKind.String)
                        {
                            var inner = root.GetString() ?? string.Empty;
                            using var innerDoc = JsonDocument.Parse(inner);
                            root = innerDoc.RootElement.Clone();
                        }
                        if (root.ValueKind == JsonValueKind.Array)
                        {
                            var list = new List<StageComponent>();
                            foreach (var elem in root.EnumerateArray())
                            {
                                bool isEnabled = true;
                                if (elem.TryGetProperty("IsEnabled", out var enProp2) && enProp2.ValueKind == JsonValueKind.False)
                                {
                                    isEnabled = false;
                                }
                                var sc = new StageComponent
                                {
                                    Key = elem.TryGetProperty("Key", out var keyProp) ? keyProp.GetString() ?? string.Empty : string.Empty,
                                    Order = elem.TryGetProperty("Order", out var orderProp) && orderProp.TryGetInt32(out var oi) ? oi : 0,
                                    IsEnabled = isEnabled,
                                    Configuration = elem.TryGetProperty("Configuration", out var cfgProp) ? (cfgProp.ValueKind == JsonValueKind.String ? cfgProp.GetString() : null) : null,
                                    StaticFields = elem.TryGetProperty("StaticFields", out var sfProp) && sfProp.ValueKind == JsonValueKind.Array ? sfProp.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList() : new List<string>(),
                                    ChecklistIds = elem.TryGetProperty("ChecklistIds", out var clProp) && clProp.ValueKind == JsonValueKind.Array ? clProp.EnumerateArray().Select(x => x.TryGetInt64(out var v) ? v : 0).ToList() : new List<long>(),
                                    QuestionnaireIds = elem.TryGetProperty("QuestionnaireIds", out var qProp) && qProp.ValueKind == JsonValueKind.Array ? qProp.EnumerateArray().Select(x => x.TryGetInt64(out var v) ? v : 0).ToList() : new List<long>(),
                                    ChecklistNames = elem.TryGetProperty("ChecklistNames", out var clnProp) && clnProp.ValueKind == JsonValueKind.Array ? clnProp.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList() : new List<string>(),
                                    QuestionnaireNames = elem.TryGetProperty("QuestionnaireNames", out var qnnProp) && qnnProp.ValueKind == JsonValueKind.Array ? qnnProp.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList() : new List<string>()
                                };
                                list.Add(sc);
                            }
                            components = list;
                            Console.WriteLine($"[DEBUG] Manual parsing succeeded, {components.Count} components");
                        }
                    }
                    catch (Exception manualEx)
                    {
                        Console.WriteLine($"[ERROR] Manual parsing also failed: {manualEx.Message}");
                        throw; // rethrow original exception
                    }
                }

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
                        // ComponentsJson 由映射层统一生成，避免重复序列化
                        entity.Components = components;
                        await _stageRepository.UpdateAsync(entity);
                    }
                    catch (Exception)
                    {
                        // Ignore persistence error here
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
        /// Clean JSON string from problematic Unicode escape sequences and stray backslashes
        /// </summary>
        private static string CleanJsonString(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString)) return jsonString;

            var cleaned = jsonString;
            // Replace common unicode escape sequences with actual chars
            cleaned = Regex.Replace(cleaned, @"\\u003[cC]", "<");
            cleaned = Regex.Replace(cleaned, @"\\u003[eE]", ">");
            cleaned = Regex.Replace(cleaned, @"\\u0026", "&");
            cleaned = Regex.Replace(cleaned, @"\\u0027", "'");
            // Remove stray escapes before ampersand if any remained (e.g., \&)
            cleaned = Regex.Replace(cleaned, @"\\&", "&");
            return cleaned;
        }

        /// <summary>
        /// Manually sync assignments between stage components and checklist/questionnaire assignments (DEPRECATED)
        /// Assignments are now managed through Stage Components only
        /// </summary>
        [Obsolete("This method is deprecated. Assignments are now managed through Stage Components only.")]
        public async Task<bool> SyncAssignmentsFromStageComponentsAsync(long stageId, long workflowId, List<long> oldChecklistIds, List<long> newChecklistIds, List<long> oldQuestionnaireIds, List<long> newQuestionnaireIds)
        {
            // Assignment sync is no longer needed as assignments are managed through Stage Components only
            return true; // Return success to avoid breaking existing calls
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
                    // Removed operation logging - not relevant for stage management operations
                    // await _operationLogService.LogOperationAsync(
                    //OperationTypeEnum.OnboardingStatusChange,
                    //BusinessModuleEnum.Stage,
                    //0,
                    //null,
                    //null,
                    //"Checklist Names Fetch Error",
                    //$"Failed to fetch checklist names: {ex.Message}",
                    //null,
                    //JsonSerializer.Serialize(allChecklistIds),
                    //new List<string> { "ChecklistNamesFetch" },
                    //null,
                    //OperationStatusEnum.Failed
                    //   );
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
                    // Removed operation logging - not relevant for stage management operations
                    // await _operationLogService.LogOperationAsync(
                    //    OperationTypeEnum.OnboardingStatusChange,
                    //    BusinessModuleEnum.Stage,
                    //    0,
                    //    null,
                    //    null,
                    //    "Questionnaire Names Fetch Error",
                    //    $"Failed to fetch questionnaire names: {ex.Message}",
                    //    null,
                    //    JsonSerializer.Serialize(allQuestionnaireIds),
                    //    new List<string> { "QuestionnaireNamesFetch" },
                    //    null,
                    //    OperationStatusEnum.Failed
                    //);
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

        /// <summary>
        /// Generate AI summary for stage based on its content
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID (optional, for specific onboarding context)</param>
        /// <param name="summaryOptions">Summary generation options</param>
        /// <returns>Generated AI summary</returns>
        public async Task<AIStageSummaryResult> GenerateAISummaryAsync(long stageId, long? onboardingId = null, StageSummaryOptions summaryOptions = null)
        {
            try
            {
                // Get stage information
                var stage = await _stageRepository.GetByIdAsync(stageId);
                if (stage == null)
                {
                    return new AIStageSummaryResult
                    {
                        Success = false,
                        Message = "Stage not found"
                    };
                }

                // Set default options if not provided
                summaryOptions ??= new StageSummaryOptions();

                // Prepare AI summary input
                var aiInput = new AIStageSummaryInput
                {
                    StageId = stageId,
                    OnboardingId = onboardingId,
                    StageName = stage.Name,
                    StageDescription = stage.Description ?? "",
                    Language = summaryOptions.Language,
                    SummaryLength = summaryOptions.SummaryLength,
                    AdditionalContext = summaryOptions.AdditionalContext,
                    ModelId = summaryOptions.ModelId,
                    ModelProvider = summaryOptions.ModelProvider,
                    ModelName = summaryOptions.ModelName,
                    ChecklistTasks = new List<AISummaryTaskInfo>(),
                    QuestionnaireQuestions = new List<AISummaryQuestionInfo>(),
                    StaticFields = new List<AISummaryFieldInfo>()
                };

                // Populate stage component information (checklist and questionnaire data)
                await PopulateStageComponentsForSummary(stageId, aiInput, summaryOptions, onboardingId);

                // Debug: Print final data counts
                Console.WriteLine($"[DEBUG] Final AI Input - ChecklistTasks: {aiInput.ChecklistTasks.Count}, QuestionnaireQuestions: {aiInput.QuestionnaireQuestions.Count}, StaticFields: {aiInput.StaticFields.Count}");

                // Generate AI summary
                var result = await _aiService.GenerateStageSummaryAsync(aiInput);

                // Log the operation
                // Removed operation logging - not relevant for stage management operations
                // await _operationLogService.LogOperationAsync(
                //OperationTypeEnum.OnboardingStatusChange,
                //    BusinessModuleEnum.Stage,
                //    stageId,
                //    null,
                //    null,
                //    "AI Summary Generated",
                //    $"AI summary generated for stage '{stage.Name}' with {aiInput.ChecklistTasks.Count} tasks and {aiInput.QuestionnaireQuestions.Count} questions",
                //    null,
                //    JsonSerializer.Serialize(new { stageId, onboardingId, summaryLength = summaryOptions.SummaryLength }),
                //    new List<string> { "AISummary", "StageAnalysis" },
                //    null,
                //    result.Success? OperationStatusEnum.Success: OperationStatusEnum.Failed
                //);

                return result;
            }
            catch (Exception ex)
            {
                // Log error
                // Removed operation logging - not relevant for stage management operations
                // await _operationLogService.LogOperationAsync(
                //OperationTypeEnum.OnboardingStatusChange,
                //    BusinessModuleEnum.Stage,
                //    stageId,
                //    null,
                //    null,
                //    "AI Summary Generation Failed",
                //    $"Failed to generate AI summary: {ex.Message}",
                //    null,
                //    JsonSerializer.Serialize(new { stageId, onboardingId, error = ex.Message }),
                //    new List<string> { "AISummary", "Error" },
                //    null,
                //    OperationStatusEnum.Failed
                //);

                return new AIStageSummaryResult
                {
                    Success = false,
                    Message = $"Failed to generate AI summary: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Generate AI summary in background without blocking the main operation
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="trigger">What triggered the summary generation</param>
        private async Task GenerateAISummaryInBackgroundAsync(long stageId, string trigger)
        {
            try
            {
                // Use default summary options for background generation
                var summaryOptions = new StageSummaryOptions
                {
                    // Use auto language detection to follow user's input language
                    Language = "auto",
                    SummaryLength = "short", // Use shorter summary for better performance
                    AdditionalContext = $"Auto-generated summary triggered by: {trigger}",
                    IncludeTaskAnalysis = true,
                    IncludeQuestionnaireInsights = true,
                    IncludeRiskAssessment = false, // Disable to reduce complexity
                    IncludeRecommendations = false // Disable to reduce complexity
                };

                // Generate AI summary with retry mechanism
                var result = await GenerateAISummaryWithRetryAsync(stageId, summaryOptions);

                if (result.Success)
                {
                    // Store the summary result (you might want to save this to database)
                    await StoreStageSummaryAsync(stageId, result, trigger);

                    // Log successful background generation
                    // Removed operation logging - not relevant for stage management operations
                    // await _operationLogService.LogOperationAsync(
                    //OperationTypeEnum.OnboardingStatusChange,
                    //    BusinessModuleEnum.Stage,
                    //    stageId,
                    //    null,
                    //    null,
                    //    "Background AI Summary Generated",
                    //    $"AI summary successfully generated in background. Trigger: {trigger}",
                    //    null,
                    //    JsonSerializer.Serialize(new
                    //    {
                    //        stageId,
                    //        trigger,
                    //        summaryLength = result.Summary?.Length ?? 0,
                    //        confidenceScore = result.ConfidenceScore
                    //    }),
                    //    new List<string> { "BackgroundAISummary", "Success" },
                    //    null,
                    //    OperationStatusEnum.Success
                    //);
                }
                else
                {
                    // Log failed background generation
                    // Removed operation logging - not relevant for stage management operations
                    // await _operationLogService.LogOperationAsync(
                    //OperationTypeEnum.OnboardingStatusChange,
                    //    BusinessModuleEnum.Stage,
                    //    stageId,
                    //    null,
                    //    null,
                    //    "Background AI Summary Failed",
                    //    $"Failed to generate AI summary in background. Trigger: {trigger}. Error: {result.Message}",
                    //    null,
                    //    JsonSerializer.Serialize(new { stageId, trigger, error = result.Message }),
                    //    new List<string> { "BackgroundAISummary", "Failed" },
                    //    null,
                    //    OperationStatusEnum.Failed
                    //);
                }
            }
            catch (Exception ex)
            {
                // Log exception in background generation - don't let it bubble up
                try
                {
                    // Removed operation logging - not relevant for stage management operations
                    // await _operationLogService.LogOperationAsync(
                    //OperationTypeEnum.OnboardingStatusChange,
                    //    BusinessModuleEnum.Stage,
                    //    stageId,
                    //    null,
                    //    null,
                    //    "Background AI Summary Exception",
                    //    $"Exception occurred during background AI summary generation. Trigger: {trigger}. Exception: {ex.Message}",
                    //    null,
                    //    JsonSerializer.Serialize(new { stageId, trigger, exception = ex.ToString() }),
                    //    new List<string> { "BackgroundAISummary", "Exception" },
                    //    null,
                    //    OperationStatusEnum.Failed
                    //);
                }
                catch
                {
                    // If logging fails, just ignore - we don't want to crash the background task
                }
            }
        }

        /// <summary>
        /// Generate AI summary with retry mechanism for better reliability
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="summaryOptions">Summary options</param>
        /// <returns>AI summary result</returns>
        private async Task<AIStageSummaryResult> GenerateAISummaryWithRetryAsync(long stageId, StageSummaryOptions summaryOptions)
        {
            const int maxRetries = 2;
            var delays = new[] { TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15) };

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // Add timeout for each attempt
                    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1)); // 1 minute timeout per attempt

                    var result = await GenerateAISummaryAsync(stageId, null, summaryOptions);

                    if (result.Success)
                    {
                        return result;
                    }

                    // If not successful and we have retries left, log and continue
                    if (attempt < maxRetries)
                    {
                        // Removed operation logging - not relevant for stage management operations
                        // await _operationLogService.LogOperationAsync(
                        //OperationTypeEnum.OnboardingStatusChange,
                        //    BusinessModuleEnum.Stage,
                        //    stageId,
                        //    null,
                        //    null,
                        //    "AI Summary Retry",
                        //    $"Attempt {attempt + 1} failed: {result.Message}. Retrying in {delays[attempt].TotalSeconds} seconds.",
                        //    null,
                        //    JsonSerializer.Serialize(new { attempt = attempt + 1, error = result.Message }),
                        //    new List<string> { "AISummaryRetry" },
                        //    null,
                        //    OperationStatusEnum.Failed
                        //);

                        await Task.Delay(delays[attempt]);
                    }
                    else
                    {
                        return result; // Return the last failed result
                    }
                }
                catch (TaskCanceledException ex)
                {
                    var errorMessage = $"AI summary generation timed out on attempt {attempt + 1}";

                    if (attempt < maxRetries)
                    {
                        // Removed operation logging - not relevant for stage management operations
                        // await _operationLogService.LogOperationAsync(
                        //OperationTypeEnum.OnboardingStatusChange,
                        //    BusinessModuleEnum.Stage,
                        //    stageId,
                        //    null,
                        //    null,
                        //    "AI Summary Timeout Retry",
                        //    $"{errorMessage}. Retrying in {delays[attempt].TotalSeconds} seconds.",
                        //    null,
                        //    JsonSerializer.Serialize(new { attempt = attempt + 1, timeout = true }),
                        //    new List<string> { "AISummaryTimeout" },
                        //    null,
                        //    OperationStatusEnum.Failed
                        //);

                        await Task.Delay(delays[attempt]);
                    }
                    else
                    {
                        return new AIStageSummaryResult
                        {
                            Success = false,
                            Message = $"{errorMessage}. All retry attempts exhausted."
                        };
                    }
                }
                catch (Exception ex)
                {
                    var errorMessage = $"AI summary generation failed on attempt {attempt + 1}: {ex.Message}";

                    if (attempt < maxRetries)
                    {
                        await Task.Delay(delays[attempt]);
                    }
                    else
                    {
                        return new AIStageSummaryResult
                        {
                            Success = false,
                            Message = $"{errorMessage}. All retry attempts exhausted."
                        };
                    }
                }
            }

            // This should never be reached, but just in case
            return new AIStageSummaryResult
            {
                Success = false,
                Message = "Unexpected error in retry mechanism"
            };
        }

        /// <summary>
        /// Store the generated AI summary (placeholder for future database storage)
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="summaryResult">AI summary result</param>
        /// <param name="trigger">What triggered the summary</param>
        private async Task StoreStageSummaryAsync(long stageId, AIStageSummaryResult summaryResult, string trigger)
        {
            try
            {
                // Update Stage entity with AI summary data
                var stage = await _stageRepository.GetByIdAsync(stageId);
                if (stage != null)
                {
                    // Update AI summary fields
                    stage.AiSummary = summaryResult.Summary;
                    stage.AiSummaryGeneratedAt = DateTime.UtcNow;
                    stage.AiSummaryConfidence = (decimal?)summaryResult.ConfidenceScore;
                    stage.AiSummaryModel = summaryResult.ModelUsed;

                    // Store detailed AI summary data as JSON
                    var detailedData = new
                    {
                        breakdown = summaryResult.Breakdown,
                        keyInsights = summaryResult.KeyInsights,
                        recommendations = summaryResult.Recommendations,
                        completionStatus = summaryResult.CompletionStatus,
                        trigger = trigger,
                        generatedAt = DateTime.UtcNow
                    };
                    stage.AiSummaryData = JsonSerializer.Serialize(detailedData);

                    // Save to database
                    await _stageRepository.UpdateAsync(stage);

                    // Log successful storage
                    // Removed operation logging - not relevant for stage management operations
                    // await _operationLogService.LogOperationAsync(
                    //OperationTypeEnum.OnboardingStatusChange,
                    //            BusinessModuleEnum.Stage,
                    //            stageId,
                    //            null,
                    //            null,
                    //            "AI Summary Stored",
                    //            $"AI summary successfully stored in database. Trigger: {trigger}",
                    //            null,
                    //            JsonSerializer.Serialize(new
                    //            {
                    //                stageId,
                    //                trigger,
                    //                summaryLength = summaryResult.Summary?.Length ?? 0,
                    //                confidenceScore = summaryResult.ConfidenceScore,
                    //                modelUsed = summaryResult.ModelUsed,
                    //                hasBreakdown = summaryResult.Breakdown != null,
                    //                keyInsightsCount = summaryResult.KeyInsights?.Count ?? 0,
                    //                recommendationsCount = summaryResult.Recommendations?.Count ?? 0,
                    //                storedAt = DateTime.UtcNow
                    //            }),
                    //            new List<string> { "AISummaryStorage", "DatabaseUpdate" },
                    //            null,
                    //            OperationStatusEnum.Success
                    //        );
                }
                else
                {
                    // Stage not found - log error
                    // Removed operation logging - not relevant for stage management operations
                    // await _operationLogService.LogOperationAsync(
                    //OperationTypeEnum.OnboardingStatusChange,
                    //            BusinessModuleEnum.Stage,
                    //            stageId,
                    //            null,
                    //            null,
                    //            "AI Summary Storage Failed",
                    //            $"Stage with ID {stageId} not found when trying to store AI summary",
                    //            null,
                    //            JsonSerializer.Serialize(new { stageId, trigger, error = "Stage not found" }),
                    //            new List<string> { "AISummaryStorage", "Error" },
                    //            null,
                    //            OperationStatusEnum.Failed
                    //        );
                }
            }
            catch (Exception ex)
            {
                // Log storage failure but don't crash the background task
                try
                {
                    // Removed operation logging - not relevant for stage management operations
                    // await _operationLogService.LogOperationAsync(
                    //OperationTypeEnum.OnboardingStatusChange,
                    //            BusinessModuleEnum.Stage,
                    //            stageId,
                    //            null,
                    //            null,
                    //            "AI Summary Storage Error",
                    //            $"Failed to store AI summary in database: {ex.Message}",
                    //            null,
                    //            JsonSerializer.Serialize(new
                    //            {
                    //                stageId,
                    //                trigger,
                    //                error = ex.Message,
                    //                stackTrace = ex.StackTrace
                    //            }),
                    //            new List<string> { "AISummaryStorage", "Exception" },
                    //            null,
                    //            OperationStatusEnum.Failed
                    //        );
                }
                catch
                {
                    // If even logging fails, just ignore to prevent infinite loops
                }
            }
        }

        /// <summary>
        /// Populate stage components information for AI summary when no specific onboarding context is available
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="aiInput">AI input to populate</param>
        /// <param name="summaryOptions">Summary options</param>
        /// <param name="onboardingId">Onboarding ID for task completion context</param>
        private async Task PopulateStageComponentsForSummary(long stageId, AIStageSummaryInput aiInput, StageSummaryOptions summaryOptions, long? onboardingId = null)
        {
            try
            {
                // Get stage components
                var components = await GetComponentsAsync(stageId);
                Console.WriteLine($"[DEBUG] PopulateStageComponentsForSummary - Stage {stageId}, Components count: {components.Count}");

                if (summaryOptions.IncludeTaskAnalysis)
                {
                    // Get checklist information from components
                    var checklistIds = components
                        .Where(c => c.Key == "checklist")
                        .SelectMany(c => c.ChecklistIds ?? new List<long>())
                        .Distinct()
                        .ToList();
                    
                    Console.WriteLine($"[DEBUG] Found {checklistIds.Count} checklist IDs: [{string.Join(", ", checklistIds)}]");

                    foreach (var checklistId in checklistIds)
                    {
                        try
                        {
                            Console.WriteLine($"[DEBUG] Attempting to load checklist {checklistId}");
                            var checklist = await _checklistService.GetByIdAsync(checklistId);
                            Console.WriteLine($"[DEBUG] Checklist loaded: {checklist?.Name}, Tasks count: {checklist?.Tasks?.Count ?? 0}");
                            
                            if (checklist?.Tasks != null && checklist.Tasks.Any())
                            {
                                // Get task completion status if onboarding context is available
                                Dictionary<long, (bool isCompleted, string notes)> taskCompletionMap = new();
                                if (onboardingId.HasValue)
                                {
                                    try
                                    {
                                        // Get task completions for this onboarding and checklist
                                        // Use repository to get task completion entities directly
                                        var taskCompletions = await _checklistTaskCompletionRepository.GetByOnboardingAndChecklistAsync(onboardingId.Value, checklistId);
                                        taskCompletionMap = taskCompletions.ToDictionary(
                                            tc => tc.TaskId,
                                            tc => (tc.IsCompleted, tc.CompletionNotes ?? "")
                                        );
                                        Console.WriteLine($"[DEBUG] Found {taskCompletionMap.Count} task completions for onboarding {onboardingId.Value}, checklist {checklistId}");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"[WARNING] Could not load task completions: {ex.Message}");
                                    }
                                }

                                var taskInfos = checklist.Tasks.Select(task => 
                                {
                                    var hasCompletion = taskCompletionMap.TryGetValue(task.Id, out var completion);
                                    return new AISummaryTaskInfo
                                    {
                                        TaskId = task.Id,
                                        TaskName = task.Name ?? "",
                                        Description = task.Description ?? "",
                                        IsRequired = task.IsRequired,
                                        IsCompleted = hasCompletion ? completion.isCompleted : false,
                                        CompletionNotes = hasCompletion ? completion.notes : "",
                                        Category = checklist.Name ?? "Checklist"
                                    };
                                }).ToList();

                                aiInput.ChecklistTasks.AddRange(taskInfos);
                                Console.WriteLine($"[DEBUG] Added {taskInfos.Count} tasks from checklist {checklist.Name}, {taskInfos.Count(t => t.IsCompleted)} completed");
                            }
                            else
                            {
                                Console.WriteLine($"[DEBUG] Checklist {checklistId} has no tasks or is null");
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log but continue with other checklists
                            Console.WriteLine($"[ERROR] Error loading checklist {checklistId}: {ex.Message}");
                            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                        }
                    }
                }

                // Get static fields information from components
                var fieldsComponents = components
                    .Where(c => c.Key == "fields" && c.StaticFields != null && c.StaticFields.Any())
                    .ToList();

                foreach (var fieldsComponent in fieldsComponents)
                {
                    foreach (var fieldName in fieldsComponent.StaticFields)
                    {
                        var fieldInfo = new AISummaryFieldInfo
                        {
                            FieldName = fieldName,
                            DisplayName = fieldName, // TODO: Get display name from field configuration if available
                            FieldType = "Text", // TODO: Get field type from configuration if available
                            IsRequired = false, // TODO: Get required status from configuration if available
                            Description = $"Static field: {fieldName}",
                            Category = "Static Field"
                        };

                        aiInput.StaticFields.Add(fieldInfo);
                    }
                }

                if (summaryOptions.IncludeQuestionnaireInsights)
                {
                    // Get questionnaire information from components
                    var questionnaireIds = components
                        .Where(c => c.Key == "questionnaires")
                        .SelectMany(c => c.QuestionnaireIds ?? new List<long>())
                        .Distinct()
                        .ToList();
                    
                    Console.WriteLine($"[DEBUG] Found {questionnaireIds.Count} questionnaire IDs: [{string.Join(", ", questionnaireIds)}]");

                    foreach (var questionnaireId in questionnaireIds)
                    {
                        try
                        {
                            Console.WriteLine($"[DEBUG] Attempting to load questionnaire {questionnaireId}");
                            var questionnaire = await _questionnaireService.GetByIdAsync(questionnaireId);
                            Console.WriteLine($"[DEBUG] Questionnaire loaded: {questionnaire?.Name}, Sections count: {questionnaire?.Sections?.Count ?? 0}");
                            
                            if (questionnaire?.Sections != null && questionnaire.Sections.Any())
                            {
                                // Get questionnaire answers if onboarding context is available
                                QuestionnaireAnswerOutputDto? questionnaireAnswer = null;
                                if (onboardingId.HasValue)
                                {
                                    try
                                    {
                                        questionnaireAnswer = await _questionnaireAnswerService.GetAnswerAsync(onboardingId.Value, stageId);
                                        Console.WriteLine($"[DEBUG] Retrieved questionnaire answer for onboarding {onboardingId.Value}, stage {stageId}: {questionnaireAnswer != null}");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"[WARNING] Could not load questionnaire answer: {ex.Message}");
                                    }
                                }

                                var questionInfos = questionnaire.Sections
                                    .SelectMany(section => section.Questions)
                                    .Select(question => 
                                    {
                                        var qId = long.TryParse(question.Id, out var parsedId) ? parsedId : 0;
                                        var isAnswered = false;
                                        string? answer = null;

                                        // Check if this question has an answer
                                        if (questionnaireAnswer?.AnswerJson != null)
                                        {
                                            try
                                            {
                                                using var answersDoc = JsonDocument.Parse(questionnaireAnswer.AnswerJson);
                                                var answersRoot = answersDoc.RootElement;
                                                
                                                // Check if it has responses array structure
                                                if (answersRoot.TryGetProperty("responses", out var responsesElement) && responsesElement.ValueKind == JsonValueKind.Array)
                                                {
                                                    // Search through responses array for matching questionId
                                                    foreach (var responseElement in responsesElement.EnumerateArray())
                                                    {
                                                        if (responseElement.TryGetProperty("questionId", out var qIdElement))
                                                        {
                                                            var responseQuestionId = qIdElement.ValueKind == JsonValueKind.String ? qIdElement.GetString() : qIdElement.ToString();
                                                            if (responseQuestionId == question.Id)
                                                            {
                                                                // Found matching question, check if it has an answer
                                                                if (responseElement.TryGetProperty("answer", out var answerElement) && 
                                                                    !string.IsNullOrEmpty(answerElement.GetString()))
                                                                {
                                                                    isAnswered = true;
                                                                    answer = answerElement.GetString();
                                                                }
                                                                break;
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    // Fallback: old simple structure (questionId -> answer mapping)
                                                    if (answersRoot.TryGetProperty(question.Id, out var answerElement))
                                                    {
                                                        isAnswered = true;
                                                        answer = answerElement.ValueKind == JsonValueKind.String 
                                                            ? answerElement.GetString() 
                                                            : answerElement.ToString();
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine($"[WARNING] Failed to parse answers JSON for question {question.Id}: {ex.Message}");
                                            }
                                        }

                                        return new AISummaryQuestionInfo
                                        {
                                            QuestionId = qId,
                                            QuestionText = question.Text ?? "",
                                            QuestionType = question.Type ?? "",
                                            IsRequired = question.IsRequired,
                                            IsAnswered = isAnswered,
                                            Answer = answer,
                                            Category = questionnaire.Name ?? "Questionnaire"
                                        };
                                    }).ToList();

                                aiInput.QuestionnaireQuestions.AddRange(questionInfos);
                                var answeredCount = questionInfos.Count(q => q.IsAnswered);
                                Console.WriteLine($"[DEBUG] Added {questionInfos.Count} questions from questionnaire {questionnaire.Name}, {answeredCount} answered");
                            }
                            else
                            {
                                Console.WriteLine($"[DEBUG] Questionnaire {questionnaireId} has no sections or is null");
                                
                                // Get questionnaire answers if onboarding context is available (for StructureJson fallback)
                                QuestionnaireAnswerOutputDto? questionnaireAnswer = null;
                                if (onboardingId.HasValue)
                                {
                                    try
                                    {
                                        questionnaireAnswer = await _questionnaireAnswerService.GetAnswerAsync(onboardingId.Value, stageId);
                                        Console.WriteLine($"[DEBUG] Retrieved questionnaire answer for fallback parsing: {questionnaireAnswer != null}");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"[WARNING] Could not load questionnaire answer for fallback: {ex.Message}");
                                    }
                                }
                                
                                // Fallback: parse StructureJson if available
                                if (!string.IsNullOrWhiteSpace(questionnaire?.StructureJson))
                                {
                                    try
                                    {
                                        using var structureDoc = JsonDocument.Parse(questionnaire.StructureJson);
                                        var root = structureDoc.RootElement;
                                        if (root.TryGetProperty("sections", out var sectionsEl) && sectionsEl.ValueKind == JsonValueKind.Array)
                                        {
                                            var parsedQuestions = new List<AISummaryQuestionInfo>();
                                            foreach (var sectionEl in sectionsEl.EnumerateArray())
                                            {
                                                if (sectionEl.TryGetProperty("questions", out var qsEl) && qsEl.ValueKind == JsonValueKind.Array)
                                                {
                                                    foreach (var qEl in qsEl.EnumerateArray())
                                                    {
                                                        var idStr = qEl.TryGetProperty("id", out var idEl) ? (idEl.ValueKind == JsonValueKind.String ? idEl.GetString() : idEl.ToString()) : "0";
                                                        var text = qEl.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : (qEl.TryGetProperty("text", out var textEl) ? textEl.GetString() : "");
                                                        var type = qEl.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : "";
                                                        var required = qEl.TryGetProperty("required", out var reqEl) ? (reqEl.ValueKind == JsonValueKind.True) : (qEl.TryGetProperty("isRequired", out var req2El) && req2El.ValueKind == JsonValueKind.True);

                                                        var questionId = long.TryParse(idStr, out var qid) ? qid : 0;
                                                        var isAnswered = false;
                                                        string? answer = null;

                                                        // Check if this question has an answer
                                                        if (questionnaireAnswer?.AnswerJson != null && !string.IsNullOrEmpty(idStr))
                                                        {
                                                            try
                                                            {
                                                                using var answersDoc = JsonDocument.Parse(questionnaireAnswer.AnswerJson);
                                                                var answersRoot = answersDoc.RootElement;
                                                                
                                                                // Check if it has responses array structure
                                                                if (answersRoot.TryGetProperty("responses", out var responsesElement) && responsesElement.ValueKind == JsonValueKind.Array)
                                                                {
                                                                    // Search through responses array for matching questionId
                                                                    foreach (var responseElement in responsesElement.EnumerateArray())
                                                                    {
                                                                        if (responseElement.TryGetProperty("questionId", out var qIdElement))
                                                                        {
                                                                            var responseQuestionId = qIdElement.ValueKind == JsonValueKind.String ? qIdElement.GetString() : qIdElement.ToString();
                                                                            if (responseQuestionId == idStr)
                                                                            {
                                                                                // Found matching question, check if it has an answer
                                                                                if (responseElement.TryGetProperty("answer", out var answerElement) && 
                                                                                    !string.IsNullOrEmpty(answerElement.GetString()))
                                                                                {
                                                                                    isAnswered = true;
                                                                                    answer = answerElement.GetString();
                                                                                }
                                                                                break;
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    // Fallback: old simple structure (questionId -> answer mapping)
                                                                    if (answersRoot.TryGetProperty(idStr, out var answerElement))
                                                                    {
                                                                        isAnswered = true;
                                                                        answer = answerElement.ValueKind == JsonValueKind.String 
                                                                            ? answerElement.GetString() 
                                                                            : answerElement.ToString();
                                                                    }
                                                                }
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                Console.WriteLine($"[WARNING] Failed to parse answers JSON for question {idStr}: {ex.Message}");
                                                            }
                                                        }

                                                        parsedQuestions.Add(new AISummaryQuestionInfo
                                                        {
                                                            QuestionId = questionId,
                                                            QuestionText = text ?? "",
                                                            QuestionType = type ?? "",
                                                            IsRequired = required,
                                                            IsAnswered = isAnswered,
                                                            Answer = answer,
                                                            Category = questionnaire.Name ?? "Questionnaire"
                                                        });
                                                    }
                                                }
                                            }

                                            if (parsedQuestions.Any())
                                            {
                                                aiInput.QuestionnaireQuestions.AddRange(parsedQuestions);
                                                Console.WriteLine($"[DEBUG] Fallback parsed {parsedQuestions.Count} questions from StructureJson for questionnaire {questionnaire.Name}");
                                            }
                                        }
                                    }
                                    catch (Exception sx)
                                    {
                                        Console.WriteLine($"[ERROR] Failed to parse StructureJson for questionnaire {questionnaireId}: {sx.Message}");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log but continue with other questionnaires
                            Console.WriteLine($"[ERROR] Error loading questionnaire {questionnaireId}: {ex.Message}");
                            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the entire operation
                Console.WriteLine($"Error populating stage components for summary: {ex.Message}");
            }
        }

        /// <summary>
        /// Update Stage AI Summary if it's currently empty (backfill only)
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="aiSummary">AI Summary content</param>
        /// <param name="generatedAt">Generated timestamp</param>
        /// <param name="confidence">Confidence score</param>
        /// <param name="modelUsed">AI model used</param>
        /// <returns>Success status</returns>
        public async Task<bool> UpdateStageAISummaryIfEmptyAsync(long stageId, string aiSummary, DateTime generatedAt, double? confidence, string modelUsed)
        {
            try
            {
                // Get current stage
                var stage = await _stageRepository.GetByIdAsync(stageId);
                if (stage == null)
                {
                    Console.WriteLine($"Stage {stageId} not found for AI summary update");
                    return false;
                }

                // Only update if AI summary is currently empty (backfill only)
                if (!string.IsNullOrWhiteSpace(stage.AiSummary))
                {
                    Console.WriteLine($"Stage {stageId} already has AI summary, skipping backfill");
                    return true; // Return true since it's not an error
                }

                // Update AI summary fields
                stage.AiSummary = aiSummary;
                stage.AiSummaryGeneratedAt = generatedAt;
                stage.AiSummaryConfidence = (decimal?)confidence;
                stage.AiSummaryModel = modelUsed;
                stage.AiSummaryData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    trigger = "Stream API backfill",
                    generatedAt = generatedAt,
                    confidence = confidence,
                    model = modelUsed
                });

                // Save to database
                var result = await _stageRepository.UpdateAsync(stage);
                Console.WriteLine($"✅ Successfully backfilled AI summary for stage {stageId}");

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to update AI summary for stage {stageId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validate and repair data consistency between stage components and mappings
        /// </summary>
        /// <param name="stageId">Stage ID to validate</param>
        /// <param name="autoRepair">Whether to automatically repair inconsistencies</param>
        /// <returns>Validation result</returns>
        public async Task<StageConsistencyResult> ValidateAndRepairConsistencyAsync(long stageId, bool autoRepair = true)
        {
            var result = new StageConsistencyResult
            {
                StageId = stageId,
                IsConsistent = false,
                ValidationTime = DateTime.UtcNow
            };

            try
            {
                // First validate using the mapping service
                var isConsistent = await _mappingService.ValidateStageComponentConsistencyAsync(stageId);
                result.IsConsistent = isConsistent;

                if (!isConsistent && autoRepair)
                {
                    // Attempt to repair by re-syncing mappings within a transaction
                    result.RepairAttempted = true;
                    
                    await _db.Ado.UseTranAsync(async () =>
                    {
                        try
                        {
                            await _mappingService.SyncStageMappingsInTransactionAsync(stageId, _db);
                            
                            // Re-validate after repair
                            var isConsistentAfterRepair = await _mappingService.ValidateStageComponentConsistencyAsync(stageId);
                            result.IsConsistent = isConsistentAfterRepair;
                            result.RepairSuccessful = isConsistentAfterRepair;

                            if (isConsistentAfterRepair)
                            {
                                Console.WriteLine($"[StageService] Successfully repaired data consistency for stage {stageId}");
                            }
                            else
                            {
                                Console.WriteLine($"[StageService] Failed to repair data consistency for stage {stageId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            result.RepairSuccessful = false;
                            result.RepairError = ex.Message;
                            Console.WriteLine($"[StageService] Error during consistency repair for stage {stageId}: {ex.Message}");
                            throw; // Re-throw to rollback transaction
                        }
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                result.ValidationError = ex.Message;
                Console.WriteLine($"[StageService] Error validating consistency for stage {stageId}: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Batch validate and repair data consistency for multiple stages
        /// </summary>
        /// <param name="stageIds">Stage IDs to validate</param>
        /// <param name="autoRepair">Whether to automatically repair inconsistencies</param>
        /// <returns>Validation results for all stages</returns>
        public async Task<List<StageConsistencyResult>> BatchValidateAndRepairConsistencyAsync(List<long> stageIds, bool autoRepair = true)
        {
            var results = new List<StageConsistencyResult>();

            foreach (var stageId in stageIds)
            {
                try
                {
                    var result = await ValidateAndRepairConsistencyAsync(stageId, autoRepair);
                    results.Add(result);

                    // Small delay between validations to avoid overwhelming the database
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    results.Add(new StageConsistencyResult
                    {
                        StageId = stageId,
                        IsConsistent = false,
                        ValidationError = ex.Message,
                        ValidationTime = DateTime.UtcNow
                    });
                }
            }

            // Log summary
            var totalStages = results.Count;
            var consistentStages = results.Count(r => r.IsConsistent);
            var repairedStages = results.Count(r => r.RepairSuccessful == true);
            
            Console.WriteLine($"[StageService] Batch consistency validation completed:");
            Console.WriteLine($"  Total stages: {totalStages}");
            Console.WriteLine($"  Consistent stages: {consistentStages}");
            Console.WriteLine($"  Repaired stages: {repairedStages}");
            Console.WriteLine($"  Failed repairs: {results.Count(r => r.RepairAttempted && r.RepairSuccessful != true)}");

                         return results;
         }

         /// <summary>
         /// Validate that checklist and questionnaire components are unique within the same workflow across different stages
         /// </summary>
         /// <param name="workflowId">Workflow ID</param>
         /// <param name="currentStageId">Current stage ID to exclude from validation (null for new stage)</param>
         /// <param name="components">Components to validate</param>
         private async Task ValidateComponentUniquenessInWorkflowAsync(long workflowId, long? currentStageId, List<StageComponent> components)
         {
             Console.WriteLine($"[StageService] ValidateComponentUniquenessInWorkflowAsync called - WorkflowId: {workflowId}, CurrentStageId: {currentStageId}");
             
             if (components == null || !components.Any())
             {
                 Console.WriteLine($"[StageService] No components to validate, returning");
                 return;
             }

             // Extract checklist and questionnaire IDs from components
             var checklistIds = components
                 .Where(c => c.Key == "checklist")
                 .SelectMany(c => c.ChecklistIds ?? new List<long>())
                 .Distinct()
                 .ToList();

             var questionnaireIds = components
                 .Where(c => c.Key == "questionnaires")
                 .SelectMany(c => c.QuestionnaireIds ?? new List<long>())
                 .Distinct()
                 .ToList();

             Console.WriteLine($"[StageService] Found {checklistIds.Count} checklist IDs: [{string.Join(", ", checklistIds)}]");
             Console.WriteLine($"[StageService] Found {questionnaireIds.Count} questionnaire IDs: [{string.Join(", ", questionnaireIds)}]");

             if (!checklistIds.Any() && !questionnaireIds.Any())
             {
                 Console.WriteLine($"[StageService] No checklist or questionnaire IDs to validate, returning");
                 return;
             }

             // Get all other stages in the same workflow
             var allStagesInWorkflow = await _stageRepository.GetByWorkflowIdAsync(workflowId);
             if (allStagesInWorkflow == null || !allStagesInWorkflow.Any())
             {
                 Console.WriteLine($"[StageService] No stages found in workflow {workflowId}, returning");
                 return;
             }

             Console.WriteLine($"[StageService] Found {allStagesInWorkflow.Count} stages in workflow {workflowId}");

             // Check each stage for conflicts
             foreach (var stage in allStagesInWorkflow)
             {
                 // Skip current stage
                 if (currentStageId.HasValue && stage.Id == currentStageId.Value)
                 {
                     Console.WriteLine($"[StageService] Skipping current stage {stage.Id} ({stage.Name})");
                     continue;
                 }

                 Console.WriteLine($"[StageService] Checking stage {stage.Id} ({stage.Name}) for conflicts");

                 // Parse existing components in this stage
                 var existingComponents = await GetStageComponentsFromEntity(stage);
                 if (existingComponents == null || !existingComponents.Any())
                 {
                     Console.WriteLine($"[StageService] Stage {stage.Id} ({stage.Name}) has no components, skipping");
                     continue;
                 }

                 Console.WriteLine($"[StageService] Stage {stage.Id} ({stage.Name}) has {existingComponents.Count} components");

                 // Extract existing checklist and questionnaire IDs
                 var existingChecklistIds = existingComponents
                     .Where(c => c.Key == "checklist")
                     .SelectMany(c => c.ChecklistIds ?? new List<long>())
                     .ToHashSet();

                 var existingQuestionnaireIds = existingComponents
                     .Where(c => c.Key == "questionnaires")
                     .SelectMany(c => c.QuestionnaireIds ?? new List<long>())
                     .ToHashSet();

                 Console.WriteLine($"[StageService] Stage {stage.Id} ({stage.Name}) existing checklist IDs: [{string.Join(", ", existingChecklistIds)}]");
                 Console.WriteLine($"[StageService] Stage {stage.Id} ({stage.Name}) existing questionnaire IDs: [{string.Join(", ", existingQuestionnaireIds)}]");

                 // Check for checklist conflicts
                 var conflictingChecklists = checklistIds.Where(id => existingChecklistIds.Contains(id)).ToList();
                 if (conflictingChecklists.Any())
                 {
                     Console.WriteLine($"[StageService] CONFLICT DETECTED! Checklist(s) [{string.Join(", ", conflictingChecklists)}] already exist in stage '{stage.Name}'");
                     
                     // Get checklist names for better error message
                     var conflictingChecklistNames = await GetChecklistNamesByIds(conflictingChecklists);
                     var namesDisplay = conflictingChecklistNames.Any() ? string.Join("、", conflictingChecklistNames) : string.Join(", ", conflictingChecklists);
                     
                     throw new CRMException(ErrorCodeEnum.BusinessError, 
                         $"Checklist '{namesDisplay}' are already used in stage '{stage.Name}' within the same workflow");
                 }

                 // Check for questionnaire conflicts
                 var conflictingQuestionnaires = questionnaireIds.Where(id => existingQuestionnaireIds.Contains(id)).ToList();
                 if (conflictingQuestionnaires.Any())
                 {
                     Console.WriteLine($"[StageService] CONFLICT DETECTED! Questionnaire(s) [{string.Join(", ", conflictingQuestionnaires)}] already exist in stage '{stage.Name}'");
                     
                     // Get questionnaire names for better error message
                     var conflictingQuestionnaireNames = await GetQuestionnaireNamesByIds(conflictingQuestionnaires);
                     var namesDisplay = conflictingQuestionnaireNames.Any() ? string.Join("、", conflictingQuestionnaireNames) : string.Join(", ", conflictingQuestionnaires);
                     
                     throw new CRMException(ErrorCodeEnum.BusinessError, 
                         $"Questionnaire '{namesDisplay}' are already used in stage '{stage.Name}' within the same workflow");
                 }

                 Console.WriteLine($"[StageService] No conflicts found with stage {stage.Id} ({stage.Name})");
             }
         }

         /// <summary>
         /// Get stage components from stage entity, handling JSON parsing
         /// </summary>
         /// <param name="stage">Stage entity</param>
         /// <returns>List of stage components</returns>
         private async Task<List<StageComponent>> GetStageComponentsFromEntity(Stage stage)
         {
             if (string.IsNullOrEmpty(stage.ComponentsJson))
                 return new List<StageComponent>();

             try
             {
                 // Handle double-escaped JSON string
                 var jsonString = stage.ComponentsJson;
                 
                 // If the string starts and ends with quotes, it's likely double-escaped
                 if (jsonString.StartsWith("\"") && jsonString.EndsWith("\""))
                 {
                     // Remove outer quotes and unescape
                     jsonString = JsonSerializer.Deserialize<string>(jsonString) ?? jsonString.Trim('"');
                 }

                 // Clean problematic escape sequences
                 var cleanedJson = CleanJsonString(jsonString);
                 
                 // Deserialize components
                 var components = JsonSerializer.Deserialize<List<StageComponent>>(cleanedJson, _jsonOptions);
                 return components ?? new List<StageComponent>();
             }
             catch (JsonException)
             {
                 // If JSON parsing fails, return empty list
                 return new List<StageComponent>();
             }
         }

         /// <summary>
         /// Get checklist names by IDs
         /// </summary>
         /// <param name="checklistIds">Checklist IDs</param>
         /// <returns>List of checklist names</returns>
         private async Task<List<string>> GetChecklistNamesByIds(List<long> checklistIds)
         {
             if (checklistIds == null || !checklistIds.Any())
                 return new List<string>();

             try
             {
                 var checklists = await _checklistService.GetByIdsAsync(checklistIds);
                 return checklists?.Select(c => c.Name).Where(name => !string.IsNullOrEmpty(name)).ToList() ?? new List<string>();
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"[StageService] Error getting checklist names: {ex.Message}");
                 return new List<string>();
             }
         }

         /// <summary>
         /// Get questionnaire names by IDs
         /// </summary>
         /// <param name="questionnaireIds">Questionnaire IDs</param>
         /// <returns>List of questionnaire names</returns>
         private async Task<List<string>> GetQuestionnaireNamesByIds(List<long> questionnaireIds)
         {
             if (questionnaireIds == null || !questionnaireIds.Any())
                 return new List<string>();

             try
             {
                 var questionnaires = await _questionnaireService.GetByIdsAsync(questionnaireIds);
                 return questionnaires?.Select(q => q.Name).Where(name => !string.IsNullOrEmpty(name)).ToList() ?? new List<string>();
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"[StageService] Error getting questionnaire names: {ex.Message}");
                 return new List<string>();
             }
         }
     }
 }