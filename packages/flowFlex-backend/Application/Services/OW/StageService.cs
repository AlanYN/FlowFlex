using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.Permission;
using FlowFlex.Application.Contracts.Dtos.OW.Stage;
using FlowFlex.Application.Contracts.Dtos.OW.QuestionnaireAnswer;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Infrastructure.Services;

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
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using JsonSerializer = System.Text.Json.JsonSerializer;
using JsonException = System.Text.Json.JsonException;
using System.Text.RegularExpressions;
using SqlSugar;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.Dtos.OW.User;

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
        private readonly IAIService _aiService; // Restored for Onboarding-specific AI summary generation
        private readonly IChecklistTaskCompletionRepository _checklistTaskCompletionRepository;
        private readonly UserContext _userContext;
        private readonly IComponentMappingService _mappingService;
        private readonly ISqlSugarClient _db;
        private readonly IDistributedCacheService _cacheService;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly IOperationChangeLogService _operationChangeLogService;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<StageService> _logger;
        private readonly IUserService _userService;

        // Cache key constants
        private const string STAGE_CACHE_PREFIX = "ow:stage";
        private static readonly TimeSpan STAGE_CACHE_DURATION = TimeSpan.FromMinutes(10);

        public StageService(IStageRepository stageRepository, IWorkflowRepository workflowRepository, IMapper mapper, IStagesProgressSyncService stagesProgressSyncService, IChecklistService checklistService, IQuestionnaireService questionnaireService, IQuestionnaireAnswerService questionnaireAnswerService, IAIService aiService, IChecklistTaskCompletionRepository checklistTaskCompletionRepository, UserContext userContext, IComponentMappingService mappingService, ISqlSugarClient db, IDistributedCacheService cacheService, IBackgroundTaskQueue backgroundTaskQueue, IOperationChangeLogService operationChangeLogService, IPermissionService permissionService, ILogger<StageService> logger, IUserService userService)
        {
            _stageRepository = stageRepository;
            _workflowRepository = workflowRepository;
            _mapper = mapper;
            _stagesProgressSyncService = stagesProgressSyncService;
            _checklistService = checklistService;
            _questionnaireService = questionnaireService;
            _questionnaireAnswerService = questionnaireAnswerService;
            _aiService = aiService; // Restored for Onboarding-specific AI summary generation
            _checklistTaskCompletionRepository = checklistTaskCompletionRepository;
            _userContext = userContext;
            _mappingService = mappingService;
            _db = db;
            _cacheService = cacheService;
            _backgroundTaskQueue = backgroundTaskQueue;
            _operationChangeLogService = operationChangeLogService;
            _permissionService = permissionService;
            _logger = logger;
            _userService = userService;
        }

        public async Task<long> CreateAsync(StageInputDto input)
        {
        // Validate if workflow exists (outside transaction)
        var workflow = await _workflowRepository.GetByIdAsync(input.WorkflowId);
        if (workflow == null)
        {
            throw new CRMException(ErrorCodeEnum.NotFound, "Workflow not found");
        }

        // Check Workflow permission before creating stage
        var userId = _userContext?.UserId;
        if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var userIdLong))
        {
            throw new CRMException(ErrorCodeEnum.AuthenticationFail, "User not authenticated");
        }

        var workflowPermission = await _permissionService.CheckWorkflowAccessAsync(
            userIdLong, 
            input.WorkflowId, 
            Domain.Shared.Enums.Permission.OperationTypeEnum.Operate);

        if (!workflowPermission.Success || !workflowPermission.CanOperate)
        {
            throw new CRMException(ErrorCodeEnum.BusinessError, 
                $"No permission to create stage in workflow '{workflow.Name}': {workflowPermission.ErrorMessage}");
        }

        _logger.LogInformation("User {UserId} has permission to create stage in workflow {WorkflowId} ({WorkflowName})", 
            userIdLong, input.WorkflowId, workflow.Name);

            // Validate team IDs in ViewTeams and OperateTeams
            await ValidateTeamSelectionsAsync(input.ViewTeams, input.OperateTeams);

            // Use transaction to ensure data consistency
            var result = await _db.Ado.UseTranAsync(async () =>
            {
                // Re-validate workflow exists inside transaction
                var workflowInTransaction = await _workflowRepository.GetByIdAsync(input.WorkflowId);
                if (workflowInTransaction == null)
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

            // Check transaction result
            if (!result.IsSuccess)
            {
                // SqlSugar wrapped the exception, re-throw it
                throw new CRMException(ErrorCodeEnum.SystemError, result.ErrorMessage ?? "Transaction failed");
            }

            // Log stage create operation if successful (outside transaction)
            if (result.Data > 0)
            {
                try
                {
                    // Get the created stage for logging
                    var createdStage = await _stageRepository.GetByIdAsync(result.Data);
                    if (createdStage != null)
                    {
                        // Log the create operation (fire-and-forget)
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _operationChangeLogService.LogStageCreateAsync(
                                    stageId: result.Data,
                                    stageName: createdStage.Name,
                                    workflowId: createdStage.WorkflowId
                                );
                            }
                            catch
                            {
                                // Ignore logging errors to avoid affecting main operation
                            }
                        });
                    }
                }
                catch
                {
                    // Ignore logging errors to avoid affecting main operation
                }
            }

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

        // Get workflow information for error messages
        var workflow = await _workflowRepository.GetByIdAsync(stage.WorkflowId);
        if (workflow == null)
        {
            throw new CRMException(ErrorCodeEnum.NotFound, "Workflow not found");
        }

        // Check Workflow permission before updating stage
        var userId = _userContext?.UserId;
        if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var userIdLong))
        {
            throw new CRMException(ErrorCodeEnum.AuthenticationFail, "User not authenticated");
        }

        var workflowPermission = await _permissionService.CheckWorkflowAccessAsync(
            userIdLong, 
            stage.WorkflowId, 
            Domain.Shared.Enums.Permission.OperationTypeEnum.Operate);

        if (!workflowPermission.Success || !workflowPermission.CanOperate)
        {
            throw new CRMException(ErrorCodeEnum.BusinessError, 
                $"No permission to update stage '{stage.Name}' in workflow '{workflow.Name}': {workflowPermission.ErrorMessage}");
        }

        _logger.LogInformation("User {UserId} has permission to update stage {StageId} ({StageName}) in workflow {WorkflowId} ({WorkflowName})", 
            userIdLong, id, stage.Name, stage.WorkflowId, workflow.Name);

            // Validate team IDs in ViewTeams and OperateTeams (only when provided)
            await ValidateTeamSelectionsAsync(input.ViewTeams, input.OperateTeams);

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

                        // AI summary generation removed - Stage entity no longer contains AI summary fields
                    }

                    // DISABLED: Stages progress sync after stage update to prevent data loss
                    // The sync was causing onboarding stages progress to be cleared or modified
                    // when stages are updated. Keeping onboarding progress data intact.

                    _logger.LogInformation("Stage update completed for workflow {WorkflowId}, stage {StageId}. " +
                        "Stages progress sync is DISABLED to preserve existing onboarding data.",
                        stageInTransaction.WorkflowId, id);

                    // Note: If sync is absolutely necessary, it should be done manually or
                    // with explicit user confirmation to prevent accidental data loss

                    // Original sync code (DISABLED):
                    // _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                    // {
                    //     try
                    //     {
                    //         await _stagesProgressSyncService.SyncAfterStageUpdateAsync(stageInTransaction.WorkflowId, id);
                    //     }
                    //     catch
                    //     {
                    //         // Ignore sync errors to avoid impacting the main operation
                    //     }
                    // });
                }

                // Clear related cache after successful update
                if (result)
                {
                    var cacheKey = $"{STAGE_CACHE_PREFIX}:workflow:{stageInTransaction.WorkflowId}";
                    await _cacheService.RemoveAsync(cacheKey);
                }

                return result;
            });

            // Check transaction result
            if (!transactionResult.IsSuccess)
            {
                // SqlSugar wrapped the exception, re-throw it
                throw new CRMException(ErrorCodeEnum.SystemError, transactionResult.ErrorMessage ?? "Transaction failed during stage update");
            }

            // Log stage update operation if successful (outside transaction)
            if (transactionResult.Data)
            {
                try
                {
                    // Get current stage data for comparison
                    var updatedStage = await _stageRepository.GetByIdAsync(id);
                    if (updatedStage != null)
                    {
                        // Prepare before and after data for logging
                        var beforeData = JsonSerializer.Serialize(new
                        {
                            Name = stage.Name,
                            Description = stage.Description,
                            Order = stage.Order,
                            ComponentsJson = stage.ComponentsJson,
                            DefaultAssignee = stage.DefaultAssignee,
                            DefaultAssignedGroup = stage.DefaultAssignedGroup,
                            EstimatedDuration = stage.EstimatedDuration,
                            IsActive = stage.IsActive,
                            VisibleInPortal = stage.VisibleInPortal,
                            PortalPermission = stage.PortalPermission,
                            ViewPermissionMode = stage.ViewPermissionMode,
                            ViewTeams = stage.ViewTeams,
                            OperateTeams = stage.OperateTeams,
                            Color = stage.Color
                        });

                        var afterData = JsonSerializer.Serialize(new
                        {
                            Name = updatedStage.Name,
                            Description = updatedStage.Description,
                            Order = updatedStage.Order,
                            ComponentsJson = updatedStage.ComponentsJson,
                            DefaultAssignee = updatedStage.DefaultAssignee,
                            DefaultAssignedGroup = updatedStage.DefaultAssignedGroup,
                            EstimatedDuration = updatedStage.EstimatedDuration,
                            IsActive = updatedStage.IsActive,
                            VisibleInPortal = updatedStage.VisibleInPortal,
                            PortalPermission = updatedStage.PortalPermission,
                            ViewPermissionMode = updatedStage.ViewPermissionMode,
                            ViewTeams = updatedStage.ViewTeams,
                            OperateTeams = updatedStage.OperateTeams,
                            Color = updatedStage.Color
                        });

                        // Determine changed fields
                        var changedFields = new List<string>();
                        if (stage.Name != updatedStage.Name) changedFields.Add("Name");
                        if (stage.Description != updatedStage.Description) changedFields.Add("Description");
                        if (stage.Order != updatedStage.Order) changedFields.Add("Order");
                        if (stage.ComponentsJson != updatedStage.ComponentsJson) changedFields.Add("ComponentsJson");
                        if (stage.DefaultAssignee != updatedStage.DefaultAssignee) changedFields.Add("DefaultAssignee");
                        if (stage.DefaultAssignedGroup != updatedStage.DefaultAssignedGroup) changedFields.Add("DefaultAssignedGroup");
                        if (stage.EstimatedDuration != updatedStage.EstimatedDuration) changedFields.Add("EstimatedDuration");
                        if (stage.IsActive != updatedStage.IsActive) changedFields.Add("IsActive");
                        if (stage.VisibleInPortal != updatedStage.VisibleInPortal) changedFields.Add("VisibleInPortal");
                        if (stage.PortalPermission != updatedStage.PortalPermission) changedFields.Add("PortalPermission");
                        if (stage.ViewPermissionMode != updatedStage.ViewPermissionMode) changedFields.Add("ViewPermissionMode");
                        if (stage.ViewTeams != updatedStage.ViewTeams) changedFields.Add("ViewTeams");
                        if (stage.OperateTeams != updatedStage.OperateTeams) changedFields.Add("OperateTeams");
                        if (stage.Color != updatedStage.Color) changedFields.Add("Color");

                        // Log the update operation (fire-and-forget)
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                // Check if components were changed and log separately for better tracking
                                bool componentsChanged = changedFields.Contains("ComponentsJson");

                                if (componentsChanged)
                                {
                                    // Create enhanced extended data for component changes
                                    var enhancedExtendedData = JsonSerializer.Serialize(new
                                    {
                                        StageId = id,
                                        StageName = updatedStage.Name,
                                        WorkflowId = updatedStage.WorkflowId,
                                        ComponentsChanged = true,
                                        FieldsChanged = changedFields,
                                        UpdatedAt = FormatUsDateTime(DateTimeOffset.UtcNow),
                                        ComponentChangeDetails = GetComponentUpdateSummary(stage.ComponentsJson, updatedStage.ComponentsJson)
                                    });

                                    // Log general stage update
                                    await _operationChangeLogService.LogStageUpdateAsync(
                                        stageId: id,
                                        stageName: updatedStage.Name,
                                        beforeData: beforeData,
                                        afterData: afterData,
                                        changedFields: changedFields,
                                        workflowId: updatedStage.WorkflowId,
                                        extendedData: enhancedExtendedData
                                    );
                                }
                                else
                                {
                                    // Log standard stage update
                                    await _operationChangeLogService.LogStageUpdateAsync(
                                        stageId: id,
                                        stageName: updatedStage.Name,
                                        beforeData: beforeData,
                                        afterData: afterData,
                                        changedFields: changedFields,
                                        workflowId: updatedStage.WorkflowId
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
                catch
                {
                    // Ignore logging errors to avoid affecting main operation
                }
            }

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
            var inputDefaultAssigneeString = input.DefaultAssignee != null && input.DefaultAssignee.Any()
                ? string.Join(",", input.DefaultAssignee)
                : null;
            if (entity.DefaultAssignee != inputDefaultAssigneeString) return true;
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
                // Clear related cache after successful deletion
                var cacheKey = $"{STAGE_CACHE_PREFIX}:workflow:{workflowId}";
                await _cacheService.RemoveAsync(cacheKey);

                // Log stage delete operation (fire-and-forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _operationChangeLogService.LogStageDeleteAsync(
                            stageId: id,
                            stageName: stageName,
                            workflowId: workflowId,
                            reason: "Stage deleted via admin portal"
                        );
                    }
                    catch
                    {
                        // Ignore logging errors to avoid affecting main operation
                    }
                });

                // DISABLED: Stages progress sync after stage deletion to prevent data loss
                // The sync was causing onboarding stages progress to be modified when stages are deleted.
                _logger.LogInformation("Stage deleted for workflow {WorkflowId}, stage {StageId}. " +
                    "Stages progress sync is DISABLED to preserve existing onboarding data.",
                    workflowId, id);

                // Original sync code (DISABLED):
                // _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                // {
                //     try
                //     {
                //         await _stagesProgressSyncService.SyncAfterStageDeleteAsync(workflowId, id);
                //     }
                //     catch
                //     {
                //         // Ignore sync errors to avoid impacting the main operation
                //     }
                // });
            }

            return deleteResult;
        }

        public async Task<StageOutputDto> GetByIdAsync(long id)
        {
            var entity = await _stageRepository.GetByIdAsync(id);
            var result = _mapper.Map<StageOutputDto>(entity);

            // Fill permission info (optimized single call)
            var userId = _userContext?.UserId;

            if (!string.IsNullOrEmpty(userId) && long.TryParse(userId, out var userIdLong))
            {
                result.Permission = await _permissionService.GetStagePermissionInfoAsync(userIdLong, id);
            }
            else
            {
                result.Permission = new Application.Contracts.Dtos.OW.Permission.PermissionInfoDto
                {
                    CanView = false,
                    CanOperate = false,
                    ErrorMessage = "User not authenticated"
                };
            }

            return result;
        }

        public async Task<List<StageOutputDto>> GetListByWorkflowIdAsync(long workflowId)
        {
            // Note: Cache is disabled for this method because Stage permissions are user-specific
            // Each user may see different stages based on their team membership
            
            // Get all stages for the workflow
            var list = await _stageRepository.GetByWorkflowIdAsync(workflowId);
            
            // Get current user ID
            var userIdString = _userContext?.UserId;
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId) || userId <= 0)
            {
                _logger.LogWarning("User ID is invalid, returning empty stage list");
                return new List<StageOutputDto>();
            }

            // Fast path: If user is System Admin, return all stages without permission checks
            if (_userContext?.IsSystemAdmin == true)
            {
                _logger.LogDebug(
                    "User {UserId} is System Admin, skipping permission filtering for workflow {WorkflowId} stages",
                    userId, workflowId);
                var adminResult = _mapper.Map<List<StageOutputDto>>(list);
                
                // System Admin has full permissions on all stages
                foreach (var stageDto in adminResult)
                {
                    stageDto.Permission = new PermissionInfoDto
                    {
                        CanView = true,
                        CanOperate = true,
                        ErrorMessage = null
                    };
                }
                
                return adminResult;
            }

            // Fast path: If user is Tenant Admin for current tenant, return all stages
            var currentTenantIdString = _userContext?.TenantId;
            if (!string.IsNullOrEmpty(currentTenantIdString) && 
                _userContext != null && 
                _userContext.HasAdminPrivileges(currentTenantIdString))
            {
                _logger.LogDebug(
                    "User {UserId} is Tenant Admin for tenant {TenantId}, skipping permission filtering for workflow {WorkflowId} stages",
                    userId, currentTenantIdString, workflowId);
                var tenantAdminResult = _mapper.Map<List<StageOutputDto>>(list);
                
                // Tenant Admin has full permissions on all stages in their tenant
                foreach (var stageDto in tenantAdminResult)
                {
                    stageDto.Permission = new PermissionInfoDto
                    {
                        CanView = true,
                        CanOperate = true,
                        ErrorMessage = null
                    };
                }
                
                return tenantAdminResult;
            }

            // Pre-check module permissions once (batch optimization)
            // Stage inherits Workflow module permissions
            bool canViewStages = await _permissionService.CheckGroupPermissionAsync(userId, PermissionConsts.Workflow.Read);
            bool canOperateStages = await _permissionService.CheckGroupPermissionAsync(userId, PermissionConsts.Workflow.Update);
            
            _logger.LogDebug("Stage list module permission check - UserId: {UserId}, CanView: {CanView}, CanOperate: {CanOperate}", 
                userId, canViewStages, canOperateStages);

            // Filter stages and fill permission info (batch-optimized)
            var filteredStages = new List<Stage>();
            var stagePermissions = new Dictionary<long, PermissionInfoDto>();
            
            foreach (var stage in list)
            {
                // Get permission info (batch-optimized: entity-level check only)
                var permissionInfo = await _permissionService.GetStagePermissionInfoForListAsync(
                    userId, stage.Id, canViewStages, canOperateStages);

                if (permissionInfo.CanView)
                {
                    filteredStages.Add(stage);
                    stagePermissions[stage.Id] = permissionInfo;
                    _logger.LogDebug(
                        "Stage {StageId} ({StageName}) included - User {UserId} has view permission",
                        stage.Id, stage.Name, userId);
                }
                else
                {
                    _logger.LogDebug(
                        "Stage {StageId} ({StageName}) filtered out - User {UserId} denied: {Reason}",
                        stage.Id, stage.Name, userId, permissionInfo.ErrorMessage);
                }
            }

            var result = _mapper.Map<List<StageOutputDto>>(filteredStages);
            
            // Fill permission info for each stage
            foreach (var stageDto in result)
            {
                if (stagePermissions.TryGetValue(stageDto.Id, out var permissionInfo))
                {
                    stageDto.Permission = permissionInfo;
                }
            }
            
            _logger.LogInformation(
                "GetListByWorkflowIdAsync - WorkflowId: {WorkflowId}, Total stages: {TotalCount}, Visible to user {UserId}: {VisibleCount}",
                workflowId, list.Count, userId, filteredStages.Count);

            return result;
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
                // Clear related cache after successful sort
                var cacheKey = $"{STAGE_CACHE_PREFIX}:workflow:{input.WorkflowId}";
                await _cacheService.RemoveAsync(cacheKey);

                // DISABLED: Stages progress sync after stage sorting to prevent data loss
                // The sync was causing onboarding stages progress to be modified when stages are reordered.
                _logger.LogInformation("Stages sorted for workflow {WorkflowId}. " +
                    "Stages progress sync is DISABLED to preserve existing onboarding data.",
                    input.WorkflowId);

                // Original sync code (DISABLED):
                // _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                // {
                //     try
                //     {
                //         await _stagesProgressSyncService.SyncAfterStagesSortAsync(input.WorkflowId, stageIds);
                //     }
                //     catch
                //     {
                //         // Ignore sync errors to avoid impacting the main operation
                //     }
                // });
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

            // Clear related cache after successful stage combination
            var cacheKey = $"{STAGE_CACHE_PREFIX}:workflow:{workflowId}";
            await _cacheService.RemoveAsync(cacheKey);

            // Create new WorkflowVersion (after stage combination) - Disabled automatic version creation
            // await CreateWorkflowVersionForStageChangeAsync(workflowId, $"Stages combined into '{input.NewStageName}'");

            // DISABLED: Stages progress sync after stage combination to prevent data loss
            // The sync was causing onboarding stages progress to be modified when stages are combined.
            _logger.LogInformation("Stages combined for workflow {WorkflowId}, new stage {NewStageId}. " +
                "Stages progress sync is DISABLED to preserve existing onboarding data.",
                workflowId, newStage.Id);

            // Original sync code (DISABLED):
            // _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
            // {
            //     try
            //     {
            //         await _stagesProgressSyncService.SyncAfterStagesCombineAsync(workflowId, input.StageIds, newStage.Id);
            //     }
            //     catch
            //     {
            //         // Ignore sync errors to avoid impacting the main operation
            //     }
            // });

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
            var result = await _stageRepository.UpdateAsync(entity);

            // Clear related cache after successful color update
            if (result)
            {
                var cacheKey = $"{STAGE_CACHE_PREFIX}:workflow:{entity.WorkflowId}";
                await _cacheService.RemoveAsync(cacheKey);
            }

            return result;
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

            // Log duplicate operation
            try
            {
                await _operationChangeLogService.LogOperationAsync(
                    OperationTypeEnum.StageDuplicate,
                    BusinessModuleEnum.Stage,
                    newStage.Id,
                    null, // No onboarding context for stage duplication
                    null, // No stage context for stage duplication
                    $"Stage Duplicated",
                    $"Duplicated stage '{sourceStage.Name}' to '{input.Name}' in workflow {targetWorkflowId}",
                    sourceStage.Name, // beforeData
                    input.Name, // afterData
                    new List<string> { "Name", "Description", "WorkflowId", "Order" },
                    JsonConvert.SerializeObject(new
                    {
                        SourceId = id,
                        SourceName = sourceStage.Name,
                        NewId = newStage.Id,
                        NewName = input.Name,
                        SourceWorkflowId = sourceStage.WorkflowId,
                        TargetWorkflowId = targetWorkflowId,
                        ChecklistId = sourceStage.ChecklistId,
                        QuestionnaireId = sourceStage.QuestionnaireId
                    }),
                    OperationStatusEnum.Success
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log stage duplicate operation for stage {StageId}", newStage.Id);
            }

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

                        // AI summary generation removed - Stage entity no longer contains AI summary fields
                    }
                }

                // Clear related cache after successful components update
                if (result)
                {
                    var cacheKey = $"{STAGE_CACHE_PREFIX}:workflow:{entity.WorkflowId}";
                    await _cacheService.RemoveAsync(cacheKey);
                }

                return result;
            });

            // Check transaction result
            if (!transactionResult.IsSuccess)
            {
                // SqlSugar wrapped the exception, re-throw it
                throw new CRMException(ErrorCodeEnum.SystemError, transactionResult.ErrorMessage ?? "Transaction failed during components update");
            }

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
                // Use the shared JSON parsing logic for consistency
                var components = ParseStageComponentsJson(entity.ComponentsJson, id);

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

        #region Component Change Logging Helpers

        /// <summary>
        /// Get summary of component updates for logging purposes
        /// </summary>
        private string GetComponentUpdateSummary(string beforeComponentsJson, string afterComponentsJson)
        {
            try
            {
                if (string.IsNullOrEmpty(beforeComponentsJson) && string.IsNullOrEmpty(afterComponentsJson))
                    return "no component changes";

                if (string.IsNullOrEmpty(beforeComponentsJson))
                    return "components configuration added";

                if (string.IsNullOrEmpty(afterComponentsJson))
                    return "components configuration removed";

                var beforeComponents = ParseStageComponentsForLogging(beforeComponentsJson);
                var afterComponents = ParseStageComponentsForLogging(afterComponentsJson);

                var changes = new List<string>();

                // Check each component type for changes
                foreach (var key in new[] { "fields", "checklist", "questionnaires", "files" })
                {
                    var beforeComp = beforeComponents.FirstOrDefault(c => c.Key == key);
                    var afterComp = afterComponents.FirstOrDefault(c => c.Key == key);

                    if (beforeComp == null && afterComp != null)
                    {
                        var details = GetComponentContentSummary(afterComp);
                        changes.Add($"added {key}: {details}");
                    }
                    else if (beforeComp != null && afterComp == null)
                    {
                        changes.Add($"removed {key} component");
                    }
                    else if (beforeComp != null && afterComp != null)
                    {
                        var componentChanges = GetComponentChangesSummary(beforeComp, afterComp);
                        if (!string.IsNullOrEmpty(componentChanges))
                        {
                            changes.Add($"{key}: {componentChanges}");
                        }
                    }
                }

                return changes.Any() ? string.Join("; ", changes) : "components updated";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate component update summary");
                return "components updated";
            }
        }

        /// <summary>
        /// Parse stage components for logging purposes
        /// </summary>
        private List<StageComponent> ParseStageComponentsForLogging(string componentsJson)
        {
            try
            {
                if (string.IsNullOrEmpty(componentsJson))
                    return new List<StageComponent>();

                return JsonSerializer.Deserialize<List<StageComponent>>(componentsJson, _jsonOptions)
                       ?? new List<StageComponent>();
            }
            catch
            {
                return new List<StageComponent>();
            }
        }

        /// <summary>
        /// Get content summary for a component
        /// </summary>
        private string GetComponentContentSummary(StageComponent component)
        {
            var details = new List<string>();

            try
            {
                switch (component.Key?.ToLower())
                {
                    case "fields":
                        if (component.StaticFields?.Any() == true)
                        {
                            details.Add($"{component.StaticFields.Count} static fields ({string.Join(", ", component.StaticFields.Take(3))}{(component.StaticFields.Count > 3 ? ", etc." : "")})");
                        }
                        break;

                    case "checklist":
                        if (component.ChecklistNames?.Any() == true)
                        {
                            details.Add($"{component.ChecklistNames.Count} checklists ({string.Join(", ", component.ChecklistNames.Take(2).Select(n => $"'{n}'"))}{(component.ChecklistNames.Count > 2 ? ", etc." : "")})");
                        }
                        else if (component.ChecklistIds?.Any() == true)
                        {
                            details.Add($"{component.ChecklistIds.Count} checklists");
                        }
                        break;

                    case "questionnaires":
                        if (component.QuestionnaireNames?.Any() == true)
                        {
                            details.Add($"{component.QuestionnaireNames.Count} questionnaires ({string.Join(", ", component.QuestionnaireNames.Take(2).Select(n => $"'{n}'"))}{(component.QuestionnaireNames.Count > 2 ? ", etc." : "")})");
                        }
                        else if (component.QuestionnaireIds?.Any() == true)
                        {
                            details.Add($"{component.QuestionnaireIds.Count} questionnaires");
                        }
                        break;

                    case "files":
                        details.Add("file management enabled");
                        break;
                }

                if (component.IsEnabled)
                {
                    details.Add("enabled");
                }
                else
                {
                    details.Add("disabled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get component content summary for {ComponentKey}", component.Key);
            }

            return details.Any() ? string.Join(", ", details) : "configured";
        }

        /// <summary>
        /// Get changes between two versions of the same component
        /// </summary>
        private string GetComponentChangesSummary(StageComponent before, StageComponent after)
        {
            var changes = new List<string>();

            try
            {
                // Check enabled/disabled state change
                if (before.IsEnabled != after.IsEnabled)
                {
                    changes.Add(after.IsEnabled ? "enabled" : "disabled");
                }

                // Check content changes based on component type
                switch (before.Key?.ToLower())
                {
                    case "fields":
                        var beforeFields = before.StaticFields ?? new List<string>();
                        var afterFields = after.StaticFields ?? new List<string>();

                        var addedFields = afterFields.Except(beforeFields).ToList();
                        var removedFields = beforeFields.Except(afterFields).ToList();

                        if (addedFields.Any())
                        {
                            changes.Add($"added fields: {string.Join(", ", addedFields.Take(3))}{(addedFields.Count > 3 ? ", etc." : "")}");
                        }
                        if (removedFields.Any())
                        {
                            changes.Add($"removed fields: {string.Join(", ", removedFields.Take(3))}{(removedFields.Count > 3 ? ", etc." : "")}");
                        }
                        break;

                    case "checklist":
                        var beforeChecklistNames = before.ChecklistNames ?? new List<string>();
                        var afterChecklistNames = after.ChecklistNames ?? new List<string>();

                        var addedChecklists = afterChecklistNames.Except(beforeChecklistNames).ToList();
                        var removedChecklists = beforeChecklistNames.Except(afterChecklistNames).ToList();

                        if (addedChecklists.Any())
                        {
                            changes.Add($"added: {string.Join(", ", addedChecklists.Take(2).Select(n => $"'{n}'"))}{(addedChecklists.Count > 2 ? ", etc." : "")}");
                        }
                        if (removedChecklists.Any())
                        {
                            changes.Add($"removed: {string.Join(", ", removedChecklists.Take(2).Select(n => $"'{n}'"))}{(removedChecklists.Count > 2 ? ", etc." : "")}");
                        }
                        break;

                    case "questionnaires":
                        var beforeQuestionnaireNames = before.QuestionnaireNames ?? new List<string>();
                        var afterQuestionnaireNames = after.QuestionnaireNames ?? new List<string>();

                        var addedQuestionnaires = afterQuestionnaireNames.Except(beforeQuestionnaireNames).ToList();
                        var removedQuestionnaires = beforeQuestionnaireNames.Except(afterQuestionnaireNames).ToList();

                        if (addedQuestionnaires.Any())
                        {
                            changes.Add($"added: {string.Join(", ", addedQuestionnaires.Take(2).Select(n => $"'{n}'"))}{(addedQuestionnaires.Count > 2 ? ", etc." : "")}");
                        }
                        if (removedQuestionnaires.Any())
                        {
                            changes.Add($"removed: {string.Join(", ", removedQuestionnaires.Take(2).Select(n => $"'{n}'"))}{(removedQuestionnaires.Count > 2 ? ", etc." : "")}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get component changes summary for {ComponentKey}", before.Key);
            }

            return string.Join(", ", changes);
        }

        /// <summary>
        /// Format date time in US format (MM/dd/yyyy hh:mm tt)
        /// </summary>
        private static string FormatUsDateTime(DateTimeOffset dateTime)
        {
            return dateTime.ToString("MM/dd/yyyy hh:mm tt", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
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
        /// Validate that provided team IDs exist in team tree from UserService.
        /// Throws BusinessError if any invalid IDs are found.
        /// </summary>
        private async Task ValidateTeamSelectionsAsync(List<string> viewTeams, List<string> operateTeams)
        {
            var needsValidation = (viewTeams != null && viewTeams.Any()) || (operateTeams != null && operateTeams.Any());
            if (!needsValidation)
            {
                return;
            }

            HashSet<string> allTeamIds;
            try
            {
                allTeamIds = await GetAllTeamIdsFromUserTreeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch team tree for validation in StageService. Skipping team ID validation.");
                return;
            }

            var invalidIds = new List<string>();
            if (viewTeams != null && viewTeams.Any())
            {
                invalidIds.AddRange(viewTeams.Where(id => !string.IsNullOrWhiteSpace(id) && !allTeamIds.Contains(id)));
            }
            if (operateTeams != null && operateTeams.Any())
            {
                invalidIds.AddRange(operateTeams.Where(id => !string.IsNullOrWhiteSpace(id) && !allTeamIds.Contains(id)));
            }
            invalidIds = invalidIds.Distinct(StringComparer.Ordinal).ToList();

            if (invalidIds.Any())
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"The following team IDs do not exist: {string.Join(", ", invalidIds)}");
            }
        }

        /// <summary>
        /// Get all valid team IDs from UserService team tree (excludes placeholder teams like 'Other').
        /// </summary>
        private async Task<HashSet<string>> GetAllTeamIdsFromUserTreeAsync()
        {
            var tree = await _userService.GetUserTreeAsync();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (tree == null || !tree.Any()) return ids;

            void Traverse(IEnumerable<UserTreeNodeDto> nodes)
            {
                foreach (var node in nodes)
                {
                    if (node == null) continue;
                    if (string.Equals(node.Type, "team", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrWhiteSpace(node.Id) && !string.Equals(node.Id, "Other", StringComparison.Ordinal))
                        {
                            ids.Add(node.Id);
                        }
                    }
                    if (node.Children != null && node.Children.Any())
                    {
                        Traverse(node.Children);
                    }
                }
            }

            Traverse(tree);
            return ids;
        }

        /// <summary>
        /// Generate AI summary for stage based on its content
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID (optional, for specific onboarding context)</param>
        /// <param name="summaryOptions">Summary generation options</param>
        /// <returns>Generated AI summary</returns>
        public async Task<AIStageSummaryResult> GenerateAISummaryAsync(long stageId, long? onboardingId = null, StageSummaryOptions summaryOptions = null)
        {
            _logger.LogInformation("Starting AI summary generation for Stage {StageId}, Onboarding {OnboardingId}", stageId, onboardingId);

            try
            {
                // Get stage entity
                var stage = await _stageRepository.GetByIdAsync(stageId);
                if (stage == null)
                {
                    return new AIStageSummaryResult
                    {
                        Success = false,
                        Message = "Stage not found",
                        Summary = string.Empty,
                        ConfidenceScore = 0.0,
                        ModelUsed = null,
                        GeneratedAt = DateTime.UtcNow
                    };
                }

                // Prepare AI input with stage and onboarding-specific context
                var aiInput = new AIStageSummaryInput
                {
                    StageId = stageId,
                    StageName = stage.Name,
                    StageDescription = stage.Description ?? "",
                    OnboardingId = onboardingId,
                    Language = summaryOptions?.Language ?? "auto",
                    SummaryLength = summaryOptions?.SummaryLength ?? "medium",
                    IncludeTaskAnalysis = summaryOptions?.IncludeTaskAnalysis ?? true,
                    IncludeQuestionnaireInsights = summaryOptions?.IncludeQuestionnaireInsights ?? true
                };

                // Populate stage components and completion data
                await PopulateStageComponentsForSummary(stageId, aiInput, summaryOptions, onboardingId);

                // Generate AI summary using AI service
                var summaryResult = await _aiService.GenerateStageSummaryAsync(aiInput);

                _logger.LogInformation("AI summary generation completed for Stage {StageId}. Success: {Success}", stageId, summaryResult.Success);

                return summaryResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI summary for Stage {StageId}", stageId);

                return new AIStageSummaryResult
                {
                    Success = false,
                    Message = $"Error generating AI summary: {ex.Message}",
                    Summary = string.Empty,
                    ConfidenceScore = 0.0,
                    ModelUsed = null,
                    GeneratedAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Generate AI summary in background without blocking the main operation
        /// DEPRECATED: AI summary functionality has been removed from Stage entity
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="trigger">What triggered the summary generation</param>
        private async Task GenerateAISummaryInBackgroundAsync(long stageId, string trigger)
        {
            // AI summary functionality has been removed from Stage entity
            _logger.LogInformation("Background AI summary generation skipped for Stage {StageId} - functionality removed. Trigger: {Trigger}", stageId, trigger);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Generate AI summary with retry mechanism for better reliability
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="summaryOptions">Summary options</param>
        /// <returns>AI summary result</returns>
        private async Task<AIStageSummaryResult> GenerateAISummaryWithRetryAsync(long stageId, StageSummaryOptions summaryOptions)
        {
            // AI summary functionality has been removed from Stage entity
            // Return the same result as GenerateAISummaryAsync for consistency
            return await GenerateAISummaryAsync(stageId, null, summaryOptions);
        }

        /// <summary>
        /// Store the generated AI summary - AI摘要字段已从Stage实体中移除
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="summaryResult">AI summary result</param>
        /// <param name="trigger">What triggered the summary</param>
        private async Task StoreStageSummaryAsync(long stageId, AIStageSummaryResult summaryResult, string trigger)
        {
            try
            {
                // AI摘要字段已从Stage实体中移除
                // Stage不再存储AI摘要数据，所有AI摘要数据现在仅存储在Onboarding的StageProgress中
                Console.WriteLine($"StoreStageSummaryAsync: Stage {stageId} - AI summary storage skipped (fields removed from Stage entity)");
                // 所有AI摘要数据现在仅存储在Onboarding的StageProgress中
            }
            catch (Exception ex)
            {
                Console.WriteLine($"StoreStageSummaryAsync: Exception occurred but ignored - {ex.Message}");
            }
        }

        /// <summary>
        /// Populate stage components information for AI summary when no specific onboarding context is available
        /// DEPRECATED: AI summary functionality has been removed from Stage entity
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

                // Get checklist data if components include checklists
                var allChecklistIds = components
                    .Where(c => c.ChecklistIds?.Any() == true)
                    .SelectMany(c => c.ChecklistIds)
                    .Distinct()
                    .ToList();

                if (allChecklistIds.Any())
                {
                    var checklists = await _checklistService.GetByIdsAsync(allChecklistIds);

                    // Convert checklists to task info format for AI summary
                    var taskInfos = new List<AISummaryTaskInfo>();
                    foreach (var checklist in checklists)
                    {
                        if (checklist.Tasks?.Any() == true)
                        {
                            foreach (var task in checklist.Tasks)
                            {
                                var taskInfo = new AISummaryTaskInfo
                                {
                                    TaskId = task.Id,
                                    TaskTitle = task.Name ?? "",
                                    TaskDescription = task.Description ?? "",
                                    IsRequired = task.IsRequired,
                                    Category = checklist.Name ?? "Checklist"
                                };

                                // Get completion status if onboarding context is available
                                if (onboardingId.HasValue)
                                {
                                    try
                                    {
                                        var completions = await _checklistTaskCompletionRepository.GetByOnboardingAndChecklistAsync(onboardingId.Value, checklist.Id);
                                        var completion = completions?.FirstOrDefault(c => c.TaskId == task.Id);
                                        if (completion != null)
                                        {
                                            taskInfo.IsCompleted = completion.IsCompleted;
                                            taskInfo.CompletionNotes = completion.CompletionNotes ?? "";
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Failed to get task completion data for task {TaskId}", task.Id);
                                    }
                                }

                                taskInfos.Add(taskInfo);
                            }
                        }
                    }
                    aiInput.Tasks = taskInfos;
                }

                // Get questionnaire data if components include questionnaires
                var allQuestionnaireIds = components
                    .Where(c => c.QuestionnaireIds?.Any() == true)
                    .SelectMany(c => c.QuestionnaireIds)
                    .Distinct()
                    .ToList();

                if (allQuestionnaireIds.Any())
                {
                    var questionnaires = await _questionnaireService.GetByIdsAsync(allQuestionnaireIds);

                    // Convert questionnaires to question info format for AI summary
                    var questionInfos = new List<AISummaryQuestionInfo>();
                    foreach (var questionnaire in questionnaires)
                    {
                        if (questionnaire.Sections?.Any() == true)
                        {
                            foreach (var section in questionnaire.Sections)
                            {
                                if (section.Questions?.Any() == true)
                                {
                                    foreach (var question in section.Questions)
                                    {
                                        var questionInfo = new AISummaryQuestionInfo
                                        {
                                            QuestionId = long.TryParse(question.Id, out var qId) ? qId : 0,
                                            QuestionText = question.Text ?? "",
                                            QuestionType = question.Type ?? "",
                                            IsRequired = question.IsRequired,
                                            Category = questionnaire.Name ?? "Questionnaire"
                                        };

                                        // Note: Detailed answer retrieval will be implemented when needed
                                        // For now, we provide the question structure for AI analysis

                                        questionInfos.Add(questionInfo);
                                    }
                                }
                            }
                        }
                    }
                    aiInput.Questions = questionInfos;
                }

                _logger.LogDebug("Populated stage components for AI summary: {TaskCount} tasks, {QuestionCount} questions",
                    aiInput.Tasks.Count, aiInput.Questions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating stage components for AI summary for Stage {StageId}", stageId);
                // Don't throw - allow AI summary generation to continue with limited data
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
            // AI摘要字段已从Stage实体中移除
            // Stage不再存储AI摘要数据，所有AI摘要数据现在仅存储在Onboarding的StageProgress中
            Console.WriteLine($"UpdateStageAISummaryIfEmptyAsync: Stage {stageId} - AI summary fields removed from Stage entity");
            return true; // 返回true避免破坏现有流程
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
                // Check if stage exists
                var stage = await _stageRepository.GetByIdAsync(stageId);
                if (stage == null)
                {
                    result.ValidationError = "Stage not found";
                    return result;
                }

                // Validate component consistency
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

            try
            {
                // Extract checklist and questionnaire IDs from new components
                var newChecklistIds = components
                .Where(c => c.Key == "checklist")
                .SelectMany(c => c.ChecklistIds ?? new List<long>())
                    .ToHashSet();

                var newQuestionnaireIds = components
                .Where(c => c.Key == "questionnaires")
                .SelectMany(c => c.QuestionnaireIds ?? new List<long>())
                    .ToHashSet();

                Console.WriteLine($"[StageService] New checklist IDs: [{string.Join(", ", newChecklistIds)}]");
                Console.WriteLine($"[StageService] New questionnaire IDs: [{string.Join(", ", newQuestionnaireIds)}]");

                // Get all stages in the same workflow
                var allStagesInWorkflow = await _stageRepository.GetByWorkflowIdAsync(workflowId);
                if (allStagesInWorkflow == null || !allStagesInWorkflow.Any())
                {
                    Console.WriteLine($"[StageService] No stages found in workflow {workflowId}, returning");
                    return;
                }

                Console.WriteLine($"[StageService] Found {allStagesInWorkflow.Count} stages in workflow {workflowId}");

                // Track how many stages have components for validation integrity
                var stagesWithComponents = 0;
                var stagesWithParseErrors = 0;

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
                    Console.WriteLine($"[StageService] Stage {stage.Id} ({stage.Name}) ComponentsJson: {(string.IsNullOrEmpty(stage.ComponentsJson) ? "NULL" : $"Length: {stage.ComponentsJson.Length}")}");
                    var existingComponents = await GetStageComponentsFromEntity(stage);

                    if (existingComponents == null || !existingComponents.Any())
                    {
                        // Check if this was due to a parsing error or truly empty components
                        if (!string.IsNullOrEmpty(stage.ComponentsJson))
                        {
                            Console.WriteLine($"[StageService] WARNING: Stage {stage.Id} ({stage.Name}) has ComponentsJson but no parsed components - possible parsing error");
                            stagesWithParseErrors++;
                        }
                        else
                        {
                            Console.WriteLine($"[StageService] Stage {stage.Id} ({stage.Name}) has no components, skipping validation");
                        }
                        continue;
                    }

                    stagesWithComponents++;

                    Console.WriteLine($"[StageService] Stage {stage.Id} ({stage.Name}) has {existingComponents.Count} components");

                    // Log component details for debugging
                    foreach (var comp in existingComponents)
                    {
                        Console.WriteLine($"[StageService] Stage {stage.Id} component: Key={comp.Key}, ChecklistIds=[{string.Join(",", comp.ChecklistIds ?? new List<long>())}], QuestionnaireIds=[{string.Join(",", comp.QuestionnaireIds ?? new List<long>())}]");
                    }

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

                    // Check for conflicts
                    var conflictingChecklists = newChecklistIds.Intersect(existingChecklistIds).ToList();
                    var conflictingQuestionnaires = newQuestionnaireIds.Intersect(existingQuestionnaireIds).ToList();

                    if (conflictingChecklists.Any() || conflictingQuestionnaires.Any())
                    {
                        var conflictMessages = new List<string>();

                        if (conflictingChecklists.Any())
                        {
                            // Get checklist names for user-friendly error message
                            var checklistNames = await GetChecklistNamesByIdsAsync(conflictingChecklists.ToList());
                            var conflictMsg = $"Checklist '{string.Join(", ", checklistNames)}' already used in stage '{stage.Name}'";
                            conflictMessages.Add(conflictMsg);
                            Console.WriteLine($"[StageService] CONFLICT DETECTED: {conflictMsg} (IDs: {string.Join(", ", conflictingChecklists)})");
                        }

                        if (conflictingQuestionnaires.Any())
                        {
                            // Get questionnaire names for user-friendly error message
                            var questionnaireNames = await GetQuestionnaireNamesByIdsAsync(conflictingQuestionnaires.ToList());
                            var conflictMsg = $"Questionnaire '{string.Join(", ", questionnaireNames)}' already used in stage '{stage.Name}'";
                            conflictMessages.Add(conflictMsg);
                            Console.WriteLine($"[StageService] CONFLICT DETECTED: {conflictMsg} (IDs: {string.Join(", ", conflictingQuestionnaires)})");
                        }

                        var fullErrorMessage = $"Components must be unique within the same workflow. {string.Join("; ", conflictMessages)}.";
                        Console.WriteLine($"[StageService] Component uniqueness validation FAILED: {fullErrorMessage}");
                        _logger.LogWarning("Component uniqueness validation failed for workflow {WorkflowId}: {ErrorMessage}", workflowId, fullErrorMessage);

                        throw new CRMException(ErrorCodeEnum.BusinessError, fullErrorMessage);
                    }
                    else
                    {
                        Console.WriteLine($"[StageService] No conflicts found with stage {stage.Id} ({stage.Name})");
                    }
                }

                // Report validation statistics
                Console.WriteLine($"[StageService] Validation completed for workflow {workflowId}: {stagesWithComponents} stages with components validated, {stagesWithParseErrors} stages with parse errors");

                // Warning if there were parse errors that could affect validation
                if (stagesWithParseErrors > 0)
                {
                    _logger.LogWarning("Component uniqueness validation for workflow {WorkflowId} completed with {ParseErrors} JSON parse errors that could affect validation accuracy",
                        workflowId, stagesWithParseErrors);
                }

                Console.WriteLine($"[StageService] Component uniqueness validation passed for workflow {workflowId}");
            }
            catch (Exception ex) when (!(ex is CRMException))
            {
                Console.WriteLine($"[StageService] Error during component uniqueness validation: {ex.Message}");
                throw new CRMException(ErrorCodeEnum.BusinessError, $"Failed to validate component uniqueness: {ex.Message}");
            }
        }

        /// <summary>
        /// Parse stage components JSON with robust error handling (extracted from GetComponentsAsync)
        /// </summary>
        private List<StageComponent> ParseStageComponentsJson(string componentsJson, long stageId)
        {
            try
            {
                if (string.IsNullOrEmpty(componentsJson))
                {
                    Console.WriteLine($"[StageService] ComponentsJson is null or empty for stage {stageId}");
                    return new List<StageComponent>();
                }

                // Handle double-escaped JSON string
                var jsonString = componentsJson;

                // If the string starts and ends with quotes, it's likely double-escaped
                if (jsonString.StartsWith("\"") && jsonString.EndsWith("\""))
                {
                    Console.WriteLine($"[StageService] Detected double-escaped JSON for stage {stageId}, unescaping...");
                    // Remove outer quotes and unescape
                    jsonString = JsonSerializer.Deserialize<string>(jsonString) ?? jsonString.Trim('"');
                }

                // Clean problematic escape sequences (e.g., \u0026 and stray \&)
                var cleanedJson = CleanJsonString(jsonString);

                List<StageComponent> components = null;
                try
                {
                    components = JsonSerializer.Deserialize<List<StageComponent>>(cleanedJson, _jsonOptions);
                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine($"[StageService] Direct JSON deserialization failed for stage {stageId}: {jsonEx.Message}");
                    // As a last resort, try to parse array items individually
                    try
                    {
                        Console.WriteLine($"[StageService] Trying manual JSON parsing for stage {stageId}...");
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
                            Console.WriteLine($"[StageService] Manual parsing succeeded for stage {stageId}, {components.Count} components");
                        }
                    }
                    catch (Exception manualEx)
                    {
                        Console.WriteLine($"[StageService] Manual parsing also failed for stage {stageId}: {manualEx.Message}");
                        // Instead of throwing, return empty list to not break validation flow completely
                        return new List<StageComponent>();
                    }
                }

                return components ?? new List<StageComponent>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StageService] Error parsing components JSON for stage {stageId}: {ex.Message}");
                _logger.LogError(ex, "Failed to parse stage components JSON for stage {StageId}", stageId);
                return new List<StageComponent>();
            }
        }

        /// <summary>
        /// Get stage components from entity (helper method for validation)
        /// </summary>
        private async Task<List<StageComponent>> GetStageComponentsFromEntity(Stage stageEntity)
        {
            return ParseStageComponentsJson(stageEntity.ComponentsJson, stageEntity.Id);
        }

        /// <summary>
        /// Helper method to get checklist names by IDs
        /// </summary>
        private async Task<List<string>> GetChecklistNamesByIdsAsync(List<long> checklistIds)
        {
            try
            {
                var checklists = new List<string>();
                foreach (var id in checklistIds)
                {
                    try
                    {
                        var checklist = await _checklistService.GetByIdAsync(id);
                        checklists.Add(checklist?.Name ?? $"Checklist {id}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[StageService] Error getting checklist name for ID {id}: {ex.Message}");
                        checklists.Add($"Checklist {id}"); // Fallback to ID-based name
                    }
                }
                return checklists;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StageService] Error getting checklist names: {ex.Message}");
                // Return fallback names based on IDs instead of empty list
                return checklistIds.Select(id => $"Checklist {id}").ToList();
            }
        }

        /// <summary>
        /// Helper method to get questionnaire names by IDs
        /// </summary>
        private async Task<List<string>> GetQuestionnaireNamesByIdsAsync(List<long> questionnaireIds)
        {
            try
            {
                var questionnaires = new List<string>();
                foreach (var id in questionnaireIds)
                {
                    try
                    {
                        var questionnaire = await _questionnaireService.GetByIdAsync(id);
                        questionnaires.Add(questionnaire?.Name ?? $"Questionnaire {id}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[StageService] Error getting questionnaire name for ID {id}: {ex.Message}");
                        questionnaires.Add($"Questionnaire {id}"); // Fallback to ID-based name
                    }
                }
                return questionnaires;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StageService] Error getting questionnaire names: {ex.Message}");
                // Return fallback names based on IDs instead of empty list
                return questionnaireIds.Select(id => $"Questionnaire {id}").ToList();
            }
        }
    }
}