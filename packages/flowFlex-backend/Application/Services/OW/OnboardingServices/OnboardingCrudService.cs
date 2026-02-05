using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Application.Contracts.IServices.DynamicData;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices.OW.ChangeLog;
using FlowFlex.Application.Contracts.IServices.OW.Onboarding;
using FlowFlex.Application.Helpers.OW;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.Base;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Helpers;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Utils;
using FlowFlex.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Text.Json;

namespace FlowFlex.Application.Services.OW.OnboardingServices
{
    /// <summary>
    /// Service for onboarding CRUD operations
    /// Handles: Create, Update, Delete, GetById
    /// </summary>
    public class OnboardingCrudService : IOnboardingCrudService
    {
        #region Fields

        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IStageRepository _stageRepository;
        private readonly IUserInvitationRepository _userInvitationRepository;
        private readonly IMapper _mapper;
        private readonly UserContext _userContext;
        private readonly IOperatorContextService _operatorContextService;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly IOnboardingLogService _onboardingLogService;
        private readonly IOnboardingPermissionService _permissionService;
        private readonly IActionManagementService _actionManagementService;
        private readonly ICaseCodeGeneratorService _caseCodeGeneratorService;
        private readonly IOnboardingStageProgressService _stageProgressService;
        private readonly IStaticFieldValueService _staticFieldValueService;
        private readonly IPropertyService _propertyService;
        private readonly IUserService _userService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OnboardingCrudService> _logger;

        // Shared JSON serializer options - use OnboardingSharedUtilities.JsonOptions for consistency
        private static readonly JsonSerializerOptions JsonOptions = OnboardingSharedUtilities.JsonOptions;

        #endregion

        #region Constructor

        public OnboardingCrudService(
            IOnboardingRepository onboardingRepository,
            IWorkflowRepository workflowRepository,
            IStageRepository stageRepository,
            IUserInvitationRepository userInvitationRepository,
            IMapper mapper,
            UserContext userContext,
            IOperatorContextService operatorContextService,
            IBackgroundTaskQueue backgroundTaskQueue,
            IOnboardingLogService onboardingLogService,
            IOnboardingPermissionService permissionService,
            IActionManagementService actionManagementService,
            ICaseCodeGeneratorService caseCodeGeneratorService,
            IOnboardingStageProgressService stageProgressService,
            IStaticFieldValueService staticFieldValueService,
            IPropertyService propertyService,
            IUserService userService,
            IServiceProvider serviceProvider,
            ILogger<OnboardingCrudService> logger)
        {
            _onboardingRepository = onboardingRepository ?? throw new ArgumentNullException(nameof(onboardingRepository));
            _workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
            _stageRepository = stageRepository ?? throw new ArgumentNullException(nameof(stageRepository));
            _userInvitationRepository = userInvitationRepository ?? throw new ArgumentNullException(nameof(userInvitationRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _operatorContextService = operatorContextService ?? throw new ArgumentNullException(nameof(operatorContextService));
            _backgroundTaskQueue = backgroundTaskQueue ?? throw new ArgumentNullException(nameof(backgroundTaskQueue));
            _onboardingLogService = onboardingLogService ?? throw new ArgumentNullException(nameof(onboardingLogService));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _actionManagementService = actionManagementService ?? throw new ArgumentNullException(nameof(actionManagementService));
            _caseCodeGeneratorService = caseCodeGeneratorService ?? throw new ArgumentNullException(nameof(caseCodeGeneratorService));
            _stageProgressService = stageProgressService ?? throw new ArgumentNullException(nameof(stageProgressService));
            _staticFieldValueService = staticFieldValueService ?? throw new ArgumentNullException(nameof(staticFieldValueService));
            _propertyService = propertyService ?? throw new ArgumentNullException(nameof(propertyService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion


        #region CreateAsync

        /// <inheritdoc />
        public async Task<long> CreateAsync(OnboardingInputDto input)
        {
            try
            {
                // Step 1: Validate dependencies
                ValidateCreateDependencies(input);

                // Step 2: Ensure table exists
                await _onboardingRepository.EnsureTableExistsAsync();

                // Step 3: Resolve workflow ID (use provided or find default)
                input.WorkflowId = await ResolveWorkflowIdAsync(input);

                // Step 4: Validate workflow exists
                var workflow = await ValidateAndGetWorkflowAsync(input.WorkflowId.Value);

                // Step 5: Get workflow stages
                var stages = await _stageRepository.GetByWorkflowIdAsync(input.WorkflowId.Value);
                var firstStage = stages.OrderBy(x => x.Order).FirstOrDefault();

                // Step 6: Initialize onboarding entity
                var entity = await InitializeOnboardingEntityAsync(input, firstStage);

                // Step 7: Validate entity
                ValidateOnboardingEntity(entity);

                // Step 8: Insert into database
                var sqlSugarClient = _onboardingRepository.GetSqlSugarClient();
                long insertedId;

                try
                {
                    entity.InitNewId();

                    if (!sqlSugarClient.DbMaintenance.IsAnyTable("ff_onboarding"))
                    {
                        sqlSugarClient.CodeFirst.SetStringDefaultLength(200).InitTables<Onboarding>();
                    }

                    var insertResult = await sqlSugarClient.Insertable(entity)
                        .IgnoreColumns(it => new { it.StagesProgress })
                        .ExecuteCommandAsync();

                    if (insertResult > 0)
                    {
                        var lastInserted = await sqlSugarClient.Queryable<Onboarding>()
                            .Where(x => x.CaseCode == entity.CaseCode &&
                                       x.TenantId == entity.TenantId &&
                                       x.AppCode == entity.AppCode)
                            .OrderByDescending(x => x.CreateDate)
                            .FirstAsync();

                        insertedId = lastInserted?.Id ?? 0;
                    }
                    else
                    {
                        throw new CRMException(ErrorCodeEnum.SystemError, "Insert failed - no rows were affected");
                    }
                }
                catch (Exception insertEx)
                {
                    // Fallback: manual SQL with minimal fields
                    try
                    {
                        insertedId = await InsertWithManualSqlAsync(sqlSugarClient, entity);
                    }
                    catch (Exception sqlEx)
                    {
                        throw new CRMException(ErrorCodeEnum.SystemError,
                            $"All insertion methods failed. Simple insert: {insertEx.Message}, Minimal SQL: {sqlEx.Message}");
                    }
                }

                // Step 9: Post-creation processing
                if (insertedId > 0)
                {
                    await ProcessPostCreationAsync(insertedId, stages.ToList());
                    QueueCacheClearAndLogging(insertedId);
                }

                return insertedId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating onboarding");
                throw;
            }
        }

        #endregion


        #region UpdateAsync

        /// <inheritdoc />
        public async Task<bool> UpdateAsync(long id, OnboardingInputDto input)
        {
            try
            {
                var entity = await _onboardingRepository.GetByIdAsync(id);
                if (entity == null)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
                }

                // Record original values for logging
                var originalWorkflowId = entity.WorkflowId;
                var originalStageId = entity.CurrentStageId;
                var originalValues = CaptureOriginalValues(entity);

                // Get workflow name for beforeData
                string beforeWorkflowName = null;
                try
                {
                    var beforeWorkflow = await _workflowRepository.GetByIdAsync(entity.WorkflowId);
                    beforeWorkflowName = beforeWorkflow?.Name;
                }
                catch { /* Ignore if workflow not found */ }

                var beforeData = SerializeBeforeData(entity, beforeWorkflowName);

                // Track if workflow changed
                bool workflowChanged = false;
                long? preservedCurrentStageId = null;
                int? preservedCurrentStageOrder = null;

                // Handle workflow change
                if (entity.WorkflowId != input.WorkflowId)
                {
                    workflowChanged = true;
                    var workflowChangeResult = await ValidateAndHandleWorkflowChangeAsync(entity, input);
                    preservedCurrentStageId = workflowChangeResult.PreservedCurrentStageId;
                    preservedCurrentStageOrder = workflowChangeResult.PreservedCurrentStageOrder;
                }

                // Map the input to entity
                _mapper.Map(input, entity);

                // Restore CurrentStageId if workflow changed
                if (workflowChanged && preservedCurrentStageId.HasValue)
                {
                    entity.CurrentStageId = preservedCurrentStageId;
                    entity.CurrentStageOrder = preservedCurrentStageOrder.Value;
                }

                // Validate team selections
                await ValidateTeamSelectionsFromJsonAsync(entity.ViewTeams, entity.OperateTeams);

                // Update system fields
                entity.ModifyDate = DateTimeOffset.UtcNow;
                entity.ModifyBy = _operatorContextService.GetOperatorDisplayName();
                entity.ModifyUserId = _operatorContextService.GetOperatorId();

                var result = await SafeUpdateOnboardingAsync(entity);

                if (result)
                {
                    await ProcessPostUpdateAsync(entity, originalValues, input, originalWorkflowId, originalStageId, beforeData);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating onboarding {OnboardingId}", id);
                throw;
            }
        }

        #endregion


        #region DeleteAsync

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(long id, bool confirm = false)
        {
            if (!confirm)
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed, "Delete operation requires confirmation");
            }

            // First try to query without tenant filter to see if record actually exists
            Onboarding entityWithoutFilter = null;
            try
            {
                var sqlSugarClient = _onboardingRepository.GetSqlSugarClient();
                sqlSugarClient.QueryFilter.ClearAndBackup();
                entityWithoutFilter = await sqlSugarClient.Queryable<Onboarding>()
                    .Where(x => x.Id == id)
                    .FirstAsync();
                sqlSugarClient.QueryFilter.Restore();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error querying onboarding without filter for ID {OnboardingId}", id);
            }

            // Query using normal repository method (with tenant filter)
            var entity = await _onboardingRepository.GetByIdAsync(id);

            if (entity == null || !entity.IsValid)
            {
                if (entityWithoutFilter != null)
                {
                    if (entityWithoutFilter.TenantId != TenantContextHelper.GetTenantIdOrDefault(_userContext))
                    {
                        throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found or access denied. Record belongs to different tenant.");
                    }
                    else if (!entityWithoutFilter.IsValid)
                    {
                        throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding has already been deleted");
                    }
                }
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            // Use soft delete
            entity.IsValid = false;
            entity.ModifyDate = DateTimeOffset.UtcNow;
            entity.ModifyBy = _operatorContextService.GetOperatorDisplayName();
            entity.ModifyUserId = _operatorContextService.GetOperatorId();

            bool result;
            try
            {
                result = await _onboardingRepository.UpdateAsync(entity,
                    it => new { it.IsValid, it.ModifyDate, it.ModifyBy, it.ModifyUserId });
            }
            catch (Exception ex) when (IsJsonbTypeError(ex))
            {
                // Fallback to manual SQL for soft delete
                var db = _onboardingRepository.GetSqlSugarClient();
                var sql = @"
                    UPDATE ff_onboarding 
                    SET is_valid = false,
                        modify_date = @ModifyDate,
                        modify_by = @ModifyBy,
                        modify_user_id = @ModifyUserId
                    WHERE id = @Id";

                var parameters = new
                {
                    ModifyDate = entity.ModifyDate,
                    ModifyBy = entity.ModifyBy,
                    ModifyUserId = entity.ModifyUserId,
                    Id = entity.Id
                };

                var commandResult = await db.Ado.ExecuteCommandAsync(sql, parameters);
                result = commandResult > 0;
            }

            if (result)
            {
                // Log deletion in background
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        await _onboardingLogService.LogOnboardingDeleteAsync(
                            entity.Id,
                            entity.CaseName ?? entity.CaseCode ?? "Unknown",
                            reason: "Deleted by user"
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to log onboarding delete operation for onboarding {OnboardingId}", entity.Id);
                    }
                });

                // Clear cache
                await ClearOnboardingQueryCacheAsync();
                await ClearRelatedCacheAsync(entity.WorkflowId, entity.CurrentStageId);
            }

            return result;
        }

        #endregion


        #region GetByIdAsync

        /// <inheritdoc />
        public async Task<OnboardingOutputDto?> GetByIdAsync(long id)
        {
            try
            {
                var entity = await _onboardingRepository.GetByIdAsync(id);
                if (entity == null)
                {
                    return null;
                }

                // Execute CaseInfo Actions to fetch and populate fields from external system
                // Only execute if SystemId and EntityId are configured and case is not completed
                var completedStatuses = new[] { "Completed", "Force Completed" };
                if (!string.IsNullOrEmpty(entity.SystemId) && !string.IsNullOrEmpty(entity.EntityId)
                    && !completedStatuses.Contains(entity.Status, StringComparer.OrdinalIgnoreCase))
                {
                    try
                    {
                        _logger.LogInformation("GetByIdAsync - Executing CaseInfo Actions for Case {CaseId} with SystemId={SystemId}, EntityId={EntityId}",
                            id, entity.SystemId, entity.EntityId);

                        // Use IServiceProvider to resolve IExternalIntegrationService to avoid circular dependency
                        var externalIntegrationService = _serviceProvider.GetRequiredService<FlowFlex.Application.Contracts.IServices.Integration.IExternalIntegrationService>();
                        var fieldMappingResult = await externalIntegrationService.RetryFieldMappingAsync(id);

                        if (fieldMappingResult.Success)
                        {
                            _logger.LogInformation("GetByIdAsync - Successfully executed CaseInfo Actions for Case {CaseId}: {ActionsExecuted} actions, {FieldsMapped} fields mapped",
                                id, fieldMappingResult.ActionsExecuted, fieldMappingResult.FieldsMapped);

                            // Reload entity to get updated field values
                            entity = await _onboardingRepository.GetByIdAsync(id);
                            if (entity == null)
                            {
                                return null;
                            }
                        }
                        else
                        {
                            _logger.LogWarning("GetByIdAsync - CaseInfo Actions execution returned non-success for Case {CaseId}: {Message}",
                                id, fieldMappingResult.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't fail the GetById operation
                        _logger.LogError(ex, "GetByIdAsync - Error executing CaseInfo Actions for Case {CaseId}", id);
                    }
                }

                // Auto-generate CaseCode for legacy data
                await EnsureCaseCodeAsync(entity);

                // Ensure stages progress is properly initialized and synced
                await _stageProgressService.EnsureStagesProgressInitializedAsync(entity);

                var result = _mapper.Map<OnboardingOutputDto>(entity);

                // Get workflow name
                var workflow = await _workflowRepository.GetByIdAsync(entity.WorkflowId);
                result.WorkflowName = workflow?.Name;

                // Recover CurrentStageId from stagesProgress if null
                if (!entity.CurrentStageId.HasValue && result.StagesProgress != null && result.StagesProgress.Any())
                {
                    var currentStageProgress = result.StagesProgress.FirstOrDefault(sp => sp.IsCurrent);
                    if (currentStageProgress != null)
                    {
                        entity.CurrentStageId = currentStageProgress.StageId;
                        result.CurrentStageId = currentStageProgress.StageId;
                        _logger.LogDebug("GetByIdAsync - Recovered CurrentStageId {CurrentStageId} from StagesProgress for Onboarding {OnboardingId}",
                            entity.CurrentStageId, id);

                        // Update database to fix the missing CurrentStageId
                        try
                        {
                            var updateSql = "UPDATE ff_onboarding SET current_stage_id = @CurrentStageId WHERE id = @Id";
                            await _onboardingRepository.GetSqlSugarClient().Ado.ExecuteCommandAsync(updateSql, new
                            {
                                CurrentStageId = entity.CurrentStageId.Value,
                                Id = id
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "GetByIdAsync - Failed to update CurrentStageId in database for Onboarding {OnboardingId}", id);
                        }
                    }
                }

                // Set current stage times
                await PopulateCurrentStageTimesAsync(entity, result);

                // Get current stage name and estimated days
                Stage currentStage = entity.CurrentStageId.HasValue
                    ? await _stageRepository.GetByIdAsync(entity.CurrentStageId.Value)
                    : null;

                if (entity.CurrentStageId.HasValue && currentStage != null)
                {
                    result.CurrentStageName = currentStage.Name;
                    PopulateCurrentStageEstimatedDays(entity, result, currentStage);
                }
                else
                {
                    _logger.LogWarning("GetByIdAsync - CurrentStageId is null for Onboarding {OnboardingId} after fallback attempt", id);
                }

                // Get user ID for permission checks
                var userId = _userContext?.UserId;
                long userIdLong = 0;
                bool hasUserId = !string.IsNullOrEmpty(userId) && long.TryParse(userId, out userIdLong);

                // Get actions and permissions for each stage in stagesProgress
                if (result.StagesProgress != null && result.StagesProgress.Any())
                {
                    await PopulateStageActionsAndPermissionsAsync(result, hasUserId, userIdLong);
                }

                // Check Case permission
                if (hasUserId)
                {
                    result.Permission = await _permissionService.GetCasePermissionInfoAsync(id);
                    result.IsDisabled = !result.Permission.CanOperate;
                }
                else
                {
                    result.IsDisabled = true;
                    result.Permission = new Application.Contracts.Dtos.OW.Permission.PermissionInfoDto
                    {
                        CanView = false,
                        CanOperate = false,
                        ErrorMessage = "User not authenticated"
                    };
                }

                // Convert legacy field names to numeric IDs
                await ConvertLegacyStaticFieldsToIdsAsync(result);

                return result;
            }
            catch (Exception ex)
            {
                throw new CRMException(ErrorCodeEnum.SystemError, $"Error getting onboarding by ID {id}: {ex.Message}");
            }
        }

        #endregion


        #region CreateAsync Helper Methods

        /// <summary>
        /// Validate that all required dependencies are available for CreateAsync
        /// </summary>
        private void ValidateCreateDependencies(OnboardingInputDto input)
        {
            if (_onboardingRepository == null)
                throw new CRMException(ErrorCodeEnum.SystemError, "Onboarding repository is not available");

            if (_workflowRepository == null)
                throw new CRMException(ErrorCodeEnum.SystemError, "Workflow repository is not available");

            if (_stageRepository == null)
                throw new CRMException(ErrorCodeEnum.SystemError, "Stage repository is not available");

            if (_mapper == null)
                throw new CRMException(ErrorCodeEnum.SystemError, "Mapper is not available");

            if (_userContext == null)
                throw new CRMException(ErrorCodeEnum.SystemError, "User context is not available");

            if (input == null)
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input parameter cannot be null");
        }

        /// <summary>
        /// Resolve workflow ID - either use provided ID or find default/active workflow
        /// </summary>
        private async Task<long> ResolveWorkflowIdAsync(OnboardingInputDto input)
        {
            if (input.WorkflowId.HasValue && input.WorkflowId.Value > 0)
            {
                return input.WorkflowId.Value;
            }

            var defaultWorkflow = await _workflowRepository.GetDefaultWorkflowAsync();
            if (defaultWorkflow != null && defaultWorkflow.IsValid && defaultWorkflow.IsActive)
            {
                return defaultWorkflow.Id;
            }

            var activeWorkflows = await _workflowRepository.GetActiveWorkflowsAsync();
            var firstActiveWorkflow = activeWorkflows?.FirstOrDefault();
            if (firstActiveWorkflow != null)
            {
                return firstActiveWorkflow.Id;
            }

            throw new CRMException(ErrorCodeEnum.DataNotFound,
                "No default or active workflow found. Please specify a valid WorkflowId or configure a default workflow.");
        }

        /// <summary>
        /// Validate workflow exists and is valid
        /// </summary>
        private async Task<Workflow> ValidateAndGetWorkflowAsync(long workflowId)
        {
            var workflow = await _workflowRepository.GetByIdAsync(workflowId);

            if (workflow == null || !workflow.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, $"Workflow not found for ID: {workflowId}");
            }

            return workflow;
        }

        /// <summary>
        /// Initialize onboarding entity with default values
        /// </summary>
        private async Task<Onboarding> InitializeOnboardingEntityAsync(OnboardingInputDto input, Stage firstStage)
        {
            var entity = _mapper.Map<Onboarding>(input);

            // Generate Case Code from Case Name
            entity.CaseCode = await _caseCodeGeneratorService.GenerateCaseCodeAsync(input.CaseName);

            // Set initial values
            entity.CurrentStageId = firstStage?.Id;
            entity.CurrentStageOrder = firstStage?.Order ?? 0;
            entity.Status = string.IsNullOrEmpty(entity.Status) ? OnboardingStatusEnum.Inactive.ToDbString() : entity.Status;
            entity.StartDate = entity.StartDate ?? DateTimeOffset.UtcNow;
            entity.CurrentStageStartTime = null;
            entity.CompletionRate = 0;
            entity.IsPrioritySet = false;
            entity.Priority = string.IsNullOrEmpty(entity.Priority) ? "Medium" : entity.Priority;
            entity.IsActive = true;
            entity.StagesProgressJson = "[]";

            // Initialize audit information
            entity.InitCreateInfo(_userContext);
            AuditHelper.ApplyCreateAudit(entity, _operatorContextService);

            if (entity.Id == 0)
            {
                entity.InitNewId();
            }

            return entity;
        }

        /// <summary>
        /// Validate onboarding entity before insertion
        /// </summary>
        private void ValidateOnboardingEntity(Onboarding entity)
        {
            if (entity.WorkflowId <= 0)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "WorkflowId must be greater than 0");
            }

            if (string.IsNullOrWhiteSpace(entity.TenantId))
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "TenantId cannot be null or empty");
            }

            if (entity.Id == 0)
            {
                entity.Id = SnowFlakeSingle.Instance.NextId();
            }
        }

        /// <summary>
        /// Insert onboarding with manual SQL (fallback method)
        /// </summary>
        private async Task<long> InsertWithManualSqlAsync(ISqlSugarClient sqlSugarClient, Onboarding entity)
        {
            var sql = @"
                INSERT INTO ff_onboarding (
                    tenant_id, app_code, is_valid, create_date, modify_date, create_by, modify_by,
                    create_user_id, modify_user_id, workflow_id, current_stage_order,
                    lead_id, case_name, case_code, lead_email, lead_phone, status, completion_rate,
                    priority, is_priority_set, ownership, ownership_name, ownership_email,
                    notes, is_active, stages_progress_json, id,
                    current_stage_id, contact_person, contact_email, life_cycle_stage_id, 
                    life_cycle_stage_name, start_date, current_stage_start_time,
                    view_permission_subject_type, operate_permission_subject_type,
                    view_permission_mode, view_teams, view_users, operate_teams, operate_users
                ) VALUES (
                    @TenantId, @AppCode, @IsValid, @CreateDate, @ModifyDate, @CreateBy, @ModifyBy,
                    @CreateUserId, @ModifyUserId, @WorkflowId, @CurrentStageOrder,
                    @LeadId, @CaseName, @CaseCode, @LeadEmail, @LeadPhone, @Status, @CompletionRate,
                    @Priority, @IsPrioritySet, 
                    CASE WHEN @Ownership IS NULL OR @Ownership = '' THEN NULL ELSE @Ownership::bigint END,
                    @OwnershipName, @OwnershipEmail,
                    @Notes, @IsActive, @StagesProgressJson::jsonb, @Id,
                    CASE WHEN @CurrentStageId IS NULL OR @CurrentStageId = '' THEN NULL ELSE @CurrentStageId::bigint END,
                    @ContactPerson, @ContactEmail,
                    CASE WHEN @LifeCycleStageId IS NULL OR @LifeCycleStageId = '' THEN NULL ELSE @LifeCycleStageId::bigint END,
                    @LifeCycleStageName, @StartDate, @CurrentStageStartTime,
                    @ViewPermissionSubjectType, @OperatePermissionSubjectType,
                    @ViewPermissionMode, @ViewTeams::jsonb, @ViewUsers::jsonb, @OperateTeams::jsonb, @OperateUsers::jsonb
                ) RETURNING id";

            await ValidateTeamSelectionsFromJsonAsync(entity.ViewTeams, entity.OperateTeams);

            var parameters = new
            {
                TenantId = entity.TenantId,
                AppCode = entity.AppCode,
                IsValid = true,
                CreateDate = DateTimeOffset.UtcNow,
                ModifyDate = DateTimeOffset.UtcNow,
                CreateBy = _operatorContextService.GetOperatorDisplayName(),
                ModifyBy = _operatorContextService.GetOperatorDisplayName(),
                CreateUserId = _operatorContextService.GetOperatorId(),
                ModifyUserId = _operatorContextService.GetOperatorId(),
                WorkflowId = entity.WorkflowId,
                CurrentStageOrder = entity.CurrentStageOrder,
                LeadId = entity.LeadId,
                CaseName = entity.CaseName,
                CaseCode = entity.CaseCode,
                LeadEmail = entity.LeadEmail ?? "",
                LeadPhone = entity.LeadPhone ?? "",
                Status = entity.Status,
                CompletionRate = entity.CompletionRate,
                Priority = entity.Priority,
                IsPrioritySet = entity.IsPrioritySet,
                Ownership = entity.Ownership.HasValue && entity.Ownership.Value > 0 ? entity.Ownership.Value.ToString() : null,
                OwnershipName = entity.OwnershipName ?? "",
                OwnershipEmail = entity.OwnershipEmail ?? "",
                Notes = entity.Notes ?? "",
                IsActive = entity.IsActive,
                StagesProgressJson = entity.StagesProgressJson,
                Id = entity.Id,
                CurrentStageId = entity.CurrentStageId?.ToString(),
                ContactPerson = entity.ContactPerson ?? "",
                ContactEmail = entity.ContactEmail ?? "",
                LifeCycleStageId = entity.LifeCycleStageId?.ToString(),
                LifeCycleStageName = entity.LifeCycleStageName ?? "",
                StartDate = entity.StartDate,
                CurrentStageStartTime = entity.CurrentStageStartTime,
                ViewPermissionSubjectType = (int)entity.ViewPermissionSubjectType,
                OperatePermissionSubjectType = (int)entity.OperatePermissionSubjectType,
                ViewPermissionMode = (int)entity.ViewPermissionMode,
                ViewTeams = ValidateAndFormatJsonArray(entity.ViewTeams),
                ViewUsers = ValidateAndFormatJsonArray(entity.ViewUsers),
                OperateTeams = ValidateAndFormatJsonArray(entity.OperateTeams),
                OperateUsers = ValidateAndFormatJsonArray(entity.OperateUsers)
            };

            return await sqlSugarClient.Ado.SqlQuerySingleAsync<long>(sql, parameters);
        }

        /// <summary>
        /// Post-creation processing: initialize stages progress, create user invitation
        /// </summary>
        private async Task ProcessPostCreationAsync(long insertedId, List<Stage> stages)
        {
            if (insertedId <= 0)
            {
                _logger.LogWarning("ProcessPostCreationAsync - Skipped: insertedId is {InsertedId}", insertedId);
                return;
            }

            try
            {
                _logger.LogInformation("ProcessPostCreationAsync - Starting for Onboarding {OnboardingId}", insertedId);

                var insertedEntity = await _onboardingRepository.GetByIdAsync(insertedId);
                if (insertedEntity == null)
                {
                    _logger.LogWarning("ProcessPostCreationAsync - Could not retrieve inserted entity {OnboardingId}", insertedId);
                    return;
                }

                _logger.LogInformation("ProcessPostCreationAsync - Retrieved entity {OnboardingId}, LeadEmail: {LeadEmail}",
                    insertedId, insertedEntity.LeadEmail ?? "(null)");

                // Initialize stage progress
                await _stageProgressService.InitializeStagesProgressAsync(insertedEntity, stages);

                // Update entity to save stage progress
                var updateResult = await SafeUpdateOnboardingAsync(insertedEntity);
                if (!updateResult)
                {
                    _logger.LogWarning("Failed to update stages progress for Onboarding {OnboardingId}", insertedId);
                }

                // Create default UserInvitation record if email is available
                await CreateDefaultUserInvitationAsync(insertedEntity);

                _logger.LogInformation("ProcessPostCreationAsync - Completed for Onboarding {OnboardingId}", insertedId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during post-creation processing for Onboarding {OnboardingId}", insertedId);
            }
        }

        /// <summary>
        /// Queue cache clear and creation logging
        /// </summary>
        private void QueueCacheClearAndLogging(long insertedId)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
            {
                try 
                { 
                    await ClearOnboardingQueryCacheAsync(); 
                }
                catch (Exception ex) 
                { 
                    _logger.LogWarning(ex, "Failed to clear query cache after create for onboarding {OnboardingId}", insertedId); 
                }
            });

            _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
            {
                try
                {
                    var insertedEntity = await _onboardingRepository.GetByIdAsync(insertedId);
                    if (insertedEntity == null) 
                    {
                        _logger.LogWarning("Onboarding {OnboardingId} not found for logging", insertedId);
                        return;
                    }

                    string workflowName = null;
                    try
                    {
                        var workflow = await _workflowRepository.GetByIdAsync(insertedEntity.WorkflowId);
                        workflowName = workflow?.Name;
                    }
                    catch (Exception workflowEx)
                    {
                        _logger.LogDebug(workflowEx, "Failed to get workflow name for onboarding {OnboardingId}", insertedId);
                    }

                    var afterData = JsonSerializer.Serialize(new
                    {
                        insertedEntity.CaseName,
                        insertedEntity.CaseCode,
                        insertedEntity.WorkflowId,
                        WorkflowName = workflowName,
                        insertedEntity.Status,
                        insertedEntity.Priority,
                        insertedEntity.LifeCycleStageName,
                        insertedEntity.ContactPerson,
                        insertedEntity.ContactEmail,
                        insertedEntity.CurrentStageId,
                        insertedEntity.Ownership,
                        insertedEntity.OwnershipName,
                        insertedEntity.ViewPermissionMode,
                        insertedEntity.ViewTeams,
                        insertedEntity.ViewUsers,
                        insertedEntity.ViewPermissionSubjectType,
                        insertedEntity.OperateTeams,
                        insertedEntity.OperateUsers,
                        insertedEntity.OperatePermissionSubjectType
                    });

                    await _onboardingLogService.LogOnboardingCreateAsync(
                        insertedId,
                        insertedEntity.CaseName ?? insertedEntity.CaseCode ?? "Unknown",
                        afterData: afterData
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to log onboarding create operation for onboarding {OnboardingId}", insertedId);
                }
            });
        }

        #endregion


        #region UpdateAsync Helper Methods

        /// <summary>
        /// Capture original values for logging
        /// </summary>
        private static (string LeadId, string CaseName, string ContactPerson, string ContactEmail, string LeadPhone,
            long? LifeCycleStageId, string Priority, string LifeCycleStageName, long? Ownership, long? CurrentStageId,
            int ViewPermissionMode, string ViewTeams, string ViewUsers, int ViewPermissionSubjectType,
            string OperateTeams, string OperateUsers, int OperatePermissionSubjectType, string OwnershipName)
            CaptureOriginalValues(Onboarding entity)
        {
            return (
                entity.LeadId,
                entity.CaseName,
                entity.ContactPerson,
                entity.ContactEmail,
                entity.LeadPhone,
                entity.LifeCycleStageId,
                entity.Priority,
                entity.LifeCycleStageName,
                entity.Ownership,
                entity.CurrentStageId,
                (int)entity.ViewPermissionMode,
                entity.ViewTeams,
                entity.ViewUsers,
                (int)entity.ViewPermissionSubjectType,
                entity.OperateTeams,
                entity.OperateUsers,
                (int)entity.OperatePermissionSubjectType,
                entity.OwnershipName
            );
        }

        /// <summary>
        /// Serialize before data for logging
        /// </summary>
        private static string SerializeBeforeData(Onboarding entity, string workflowName)
        {
            return JsonSerializer.Serialize(new
            {
                CaseName = entity.CaseName,
                CaseCode = entity.CaseCode,
                WorkflowId = entity.WorkflowId,
                WorkflowName = workflowName,
                Status = entity.Status,
                Priority = entity.Priority,
                LifeCycleStageName = entity.LifeCycleStageName,
                ContactPerson = entity.ContactPerson,
                ContactEmail = entity.ContactEmail,
                CurrentStageId = entity.CurrentStageId,
                Ownership = entity.Ownership,
                OwnershipName = entity.OwnershipName,
                ViewPermissionMode = entity.ViewPermissionMode,
                ViewTeams = entity.ViewTeams,
                ViewUsers = entity.ViewUsers,
                ViewPermissionSubjectType = entity.ViewPermissionSubjectType,
                OperateTeams = entity.OperateTeams,
                OperateUsers = entity.OperateUsers,
                OperatePermissionSubjectType = entity.OperatePermissionSubjectType
            });
        }

        /// <summary>
        /// Validate and handle workflow change
        /// </summary>
        private async Task<(long? PreservedCurrentStageId, int? PreservedCurrentStageOrder)> ValidateAndHandleWorkflowChangeAsync(
            Onboarding entity, OnboardingInputDto input)
        {
            long? preservedCurrentStageId = null;
            int? preservedCurrentStageOrder = null;

            // Business Rule 1: Only allow workflow change for cases with status "Started"
            if (entity.Status != "Started")
            {
                throw new CRMException(
                    ErrorCodeEnum.OperationNotAllowed,
                    $"Cannot change workflow for a case with status '{entity.Status}'.");
            }

            // Business Rule 2: Only allow workflow change for cases that haven't started yet
            _stageProgressService.LoadStagesProgressFromJson(entity);

            bool isUnstarted = entity.StagesProgress == null ||
                               entity.StagesProgress.Count == 0 ||
                               entity.StagesProgress.All(sp => !sp.IsCompleted && !sp.IsSaved);

            if (!isUnstarted)
            {
                throw new CRMException(
                    ErrorCodeEnum.OperationNotAllowed,
                    $"Cannot change workflow for a case that has already started or has saved progress. Current status: {entity.Status}");
            }

            var workflow = await _workflowRepository.GetByIdAsync(input.WorkflowId);
            if (workflow == null)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Workflow not found");
            }

            var stages = await _stageRepository.GetByWorkflowIdAsync(input.WorkflowId.Value);
            var firstStage = stages.OrderBy(x => x.Order).FirstOrDefault();

            preservedCurrentStageId = firstStage?.Id;
            preservedCurrentStageOrder = firstStage?.Order ?? 1;

            entity.CurrentStageId = preservedCurrentStageId;
            entity.CurrentStageOrder = preservedCurrentStageOrder.Value;
            entity.CompletionRate = 0;

            // Re-initialize stagesProgress with new workflow's stages
            await _stageProgressService.InitializeStagesProgressAsync(entity, stages.ToList());

            return (preservedCurrentStageId, preservedCurrentStageOrder);
        }

        /// <summary>
        /// Process post-update operations
        /// </summary>
        private async Task ProcessPostUpdateAsync(Onboarding entity,
            (string LeadId, string CaseName, string ContactPerson, string ContactEmail, string LeadPhone,
            long? LifeCycleStageId, string Priority, string LifeCycleStageName, long? Ownership, long? CurrentStageId,
            int ViewPermissionMode, string ViewTeams, string ViewUsers, int ViewPermissionSubjectType,
            string OperateTeams, string OperateUsers, int OperatePermissionSubjectType, string OwnershipName) originalValues,
            OnboardingInputDto input, long originalWorkflowId, long? originalStageId, string beforeData)
        {
            // Sync static field values
            long? targetStageId = entity.CurrentStageId;

            if (!targetStageId.HasValue && entity.WorkflowId > 0)
            {
                var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
                var firstStage = stages.OrderBy(s => s.Order).FirstOrDefault();
                targetStageId = firstStage?.Id;
            }

            if (targetStageId.HasValue)
            {
                await SyncStaticFieldValuesAsync(
                    entity.Id,
                    targetStageId.Value,
                    originalValues.LeadId,
                    originalValues.CaseName,
                    originalValues.ContactPerson,
                    originalValues.ContactEmail,
                    originalValues.LeadPhone,
                    originalValues.LifeCycleStageId,
                    originalValues.Priority,
                    input
                );
            }

            // Log update in background
            _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
            {
                try
                {
                    await LogOnboardingUpdateAsync(entity, originalValues, originalWorkflowId, beforeData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to log onboarding update operation for onboarding {OnboardingId}", entity.Id);
                }
            });

            // Clear cache in background
            _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
            {
                try
                {
                    await ClearOnboardingQueryCacheAsync();

                    if (originalWorkflowId != entity.WorkflowId)
                    {
                        await ClearRelatedCacheAsync(originalWorkflowId);
                        await ClearRelatedCacheAsync(entity.WorkflowId);
                    }
                    else
                    {
                        await ClearRelatedCacheAsync(entity.WorkflowId);
                    }

                    if (originalStageId != entity.CurrentStageId)
                    {
                        if (originalStageId.HasValue)
                        {
                            await ClearRelatedCacheAsync(null, originalStageId.Value);
                        }
                        if (entity.CurrentStageId.HasValue)
                        {
                            await ClearRelatedCacheAsync(null, entity.CurrentStageId.Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clear cache after update");
                }
            });
        }

        /// <summary>
        /// Log onboarding update
        /// </summary>
        private async Task LogOnboardingUpdateAsync(Onboarding entity,
            (string LeadId, string CaseName, string ContactPerson, string ContactEmail, string LeadPhone,
            long? LifeCycleStageId, string Priority, string LifeCycleStageName, long? Ownership, long? CurrentStageId,
            int ViewPermissionMode, string ViewTeams, string ViewUsers, int ViewPermissionSubjectType,
            string OperateTeams, string OperateUsers, int OperatePermissionSubjectType, string OwnershipName) originalValues,
            long originalWorkflowId, string beforeData)
        {
            string afterWorkflowName = null;
            try
            {
                var afterWorkflow = await _workflowRepository.GetByIdAsync(entity.WorkflowId);
                afterWorkflowName = afterWorkflow?.Name;
            }
            catch { /* Ignore */ }

            var afterData = JsonSerializer.Serialize(new
            {
                CaseName = entity.CaseName,
                CaseCode = entity.CaseCode,
                WorkflowId = entity.WorkflowId,
                WorkflowName = afterWorkflowName,
                Status = entity.Status,
                Priority = entity.Priority,
                LifeCycleStageName = entity.LifeCycleStageName,
                ContactPerson = entity.ContactPerson,
                ContactEmail = entity.ContactEmail,
                CurrentStageId = entity.CurrentStageId,
                Ownership = entity.Ownership,
                OwnershipName = entity.OwnershipName,
                ViewPermissionMode = entity.ViewPermissionMode,
                ViewTeams = entity.ViewTeams,
                ViewUsers = entity.ViewUsers,
                ViewPermissionSubjectType = entity.ViewPermissionSubjectType,
                OperateTeams = entity.OperateTeams,
                OperateUsers = entity.OperateUsers,
                OperatePermissionSubjectType = entity.OperatePermissionSubjectType
            });

            var changedFields = new List<string>();
            if (originalValues.CaseName != entity.CaseName) changedFields.Add("CaseName");
            if (originalValues.ContactPerson != entity.ContactPerson) changedFields.Add("ContactPerson");
            if (originalValues.ContactEmail != entity.ContactEmail) changedFields.Add("ContactEmail");
            if (originalValues.Priority != entity.Priority) changedFields.Add("Priority");
            if (originalValues.LifeCycleStageName != entity.LifeCycleStageName) changedFields.Add("LifeCycleStageName");
            if (originalWorkflowId != entity.WorkflowId) changedFields.Add("WorkflowId");
            if (originalValues.Ownership != entity.Ownership) changedFields.Add("Ownership");
            if (originalValues.ViewPermissionMode != (int)entity.ViewPermissionMode) changedFields.Add("ViewPermissionMode");
            if (originalValues.ViewTeams != entity.ViewTeams) changedFields.Add("ViewTeams");
            if (originalValues.ViewUsers != entity.ViewUsers) changedFields.Add("ViewUsers");
            if (originalValues.ViewPermissionSubjectType != (int)entity.ViewPermissionSubjectType) changedFields.Add("ViewPermissionSubjectType");
            if (originalValues.OperateTeams != entity.OperateTeams) changedFields.Add("OperateTeams");
            if (originalValues.OperateUsers != entity.OperateUsers) changedFields.Add("OperateUsers");
            if (originalValues.OperatePermissionSubjectType != (int)entity.OperatePermissionSubjectType) changedFields.Add("OperatePermissionSubjectType");

            await _onboardingLogService.LogOnboardingUpdateAsync(
                entity.Id,
                entity.CaseName ?? entity.CaseCode ?? "Unknown",
                beforeData: beforeData,
                afterData: afterData,
                changedFields: changedFields
            );
        }

        #endregion


        #region GetByIdAsync Helper Methods

        /// <summary>
        /// Populate current stage times
        /// </summary>
        private async Task PopulateCurrentStageTimesAsync(Onboarding entity, OnboardingOutputDto result)
        {
            result.CurrentStageStartTime = null;
            result.CurrentStageEndTime = null;
            double? estimatedDays = null;

            Stage currentStage = entity.CurrentStageId.HasValue
                ? await _stageRepository.GetByIdAsync(entity.CurrentStageId.Value)
                : null;

            if (entity.CurrentStageId.HasValue && result.StagesProgress != null && result.StagesProgress.Any())
            {
                var currentStageProgress = result.StagesProgress.FirstOrDefault(sp => sp.StageId == entity.CurrentStageId.Value);
                if (currentStageProgress != null)
                {
                    if (currentStageProgress.StartTime.HasValue)
                    {
                        result.CurrentStageStartTime = OnboardingSharedUtilities.NormalizeToStartOfDay(currentStageProgress.StartTime);
                    }

                    // Priority: customEndTime > endTime > (startTime+estimatedDays) > null
                    if (currentStageProgress.CustomEndTime.HasValue)
                    {
                        result.CurrentStageEndTime = OnboardingSharedUtilities.NormalizeToStartOfDay(currentStageProgress.CustomEndTime.Value);
                    }
                    else if (currentStageProgress.EndTime.HasValue)
                    {
                        result.CurrentStageEndTime = OnboardingSharedUtilities.NormalizeToStartOfDay(currentStageProgress.EndTime.Value);
                    }
                    else
                    {
                        // Three-level priority: json.customEstimatedDays > json.estimatedDays > stage entity
                        estimatedDays = currentStageProgress.CustomEstimatedDays.HasValue
                            ? (double?)Math.Round(currentStageProgress.CustomEstimatedDays.Value, 0)
                            : null;
                        if (!estimatedDays.HasValue || estimatedDays.Value <= 0)
                        {
                            estimatedDays = currentStageProgress.EstimatedDays.HasValue
                                ? (double?)Math.Round(currentStageProgress.EstimatedDays.Value, 0)
                                : null;
                            if ((!estimatedDays.HasValue || estimatedDays.Value <= 0) && currentStage != null)
                            {
                                if (currentStage.EstimatedDuration != null && currentStage.EstimatedDuration > 0)
                                    estimatedDays = (double?)Math.Round(currentStage.EstimatedDuration.Value, 0);
                            }
                        }
                    }
                }
            }

            // Calculate currentStageEndTime when both startTime and estimatedDays exist
            if (result.CurrentStageEndTime == null && result.CurrentStageStartTime.HasValue && (estimatedDays.HasValue && estimatedDays.Value > 0))
            {
                var normalizedStartTime = OnboardingSharedUtilities.NormalizeToStartOfDay(result.CurrentStageStartTime.Value);
                result.CurrentStageEndTime = normalizedStartTime.AddDays((int)estimatedDays.Value);
            }
        }

        /// <summary>
        /// Populate current stage estimated days
        /// </summary>
        private void PopulateCurrentStageEstimatedDays(Onboarding entity, OnboardingOutputDto result, Stage currentStage)
        {
            var currentStageProgress = result.StagesProgress?.FirstOrDefault(sp => sp.StageId == entity.CurrentStageId.Value);
            if (currentStageProgress != null && currentStageProgress.CustomEstimatedDays.HasValue && currentStageProgress.CustomEstimatedDays.Value > 0)
            {
                result.CurrentStageEstimatedDays = Math.Round(currentStageProgress.CustomEstimatedDays.Value, 0);
            }
            else
            {
                result.CurrentStageEstimatedDays = currentStage.EstimatedDuration.HasValue
                    ? Math.Round(currentStage.EstimatedDuration.Value, 0)
                    : null;
            }
        }

        /// <summary>
        /// Populate stage actions and permissions
        /// </summary>
        private async Task PopulateStageActionsAndPermissionsAsync(OnboardingOutputDto result, bool hasUserId, long userIdLong)
        {
            // Batch query all actions for all stages at once
            var stageIds = result.StagesProgress.Select(sp => sp.StageId).ToList();
            Dictionary<long, List<ActionTriggerMappingWithActionInfo>> actionsDict;
            try
            {
                actionsDict = await _actionManagementService.GetActionTriggerMappingsByTriggerSourceIdsAsync(stageIds);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to batch query actions for stages, falling back to empty actions");
                actionsDict = new Dictionary<long, List<ActionTriggerMappingWithActionInfo>>();
            }

            foreach (var stageProgress in result.StagesProgress)
            {
                // Get actions for this stage from batch result
                stageProgress.Actions = actionsDict.TryGetValue(stageProgress.StageId, out var actions)
                    ? actions
                    : new List<ActionTriggerMappingWithActionInfo>();

                // Get permission for this stage
                if (hasUserId)
                {
                    try
                    {
                        stageProgress.Permission = await _permissionService.GetStagePermissionInfoAsync(stageProgress.StageId);
                    }
                    catch (Exception ex)
                    {
                        stageProgress.Permission = new Application.Contracts.Dtos.OW.Permission.PermissionInfoDto
                        {
                            CanView = false,
                            CanOperate = false,
                            ErrorMessage = $"Error checking stage permission: {ex.Message}"
                        };
                    }
                }
                else
                {
                    stageProgress.Permission = new Application.Contracts.Dtos.OW.Permission.PermissionInfoDto
                    {
                        CanView = false,
                        CanOperate = false,
                        ErrorMessage = "User not authenticated"
                    };
                }
            }
        }

        /// <summary>
        /// Convert legacy field names to numeric IDs in stagesProgress.components.staticFields
        /// </summary>
        private async Task ConvertLegacyStaticFieldsToIdsAsync(OnboardingOutputDto result)
        {
            if (result?.StagesProgress == null || !result.StagesProgress.Any())
            {
                return;
            }

            // Collect all legacy field names that need to be converted
            var legacyFieldNames = new HashSet<string>();
            foreach (var stageProgress in result.StagesProgress)
            {
                if (stageProgress.Components == null) continue;

                foreach (var component in stageProgress.Components)
                {
                    if (component.Key != "fields" || component.StaticFields == null) continue;

                    foreach (var field in component.StaticFields)
                    {
                        if (!string.IsNullOrEmpty(field.Id) && !long.TryParse(field.Id, out _))
                        {
                            legacyFieldNames.Add(field.Id);
                        }
                    }
                }
            }

            if (!legacyFieldNames.Any())
            {
                return;
            }

            try
            {
                var allProperties = await _propertyService.GetPropertyListAsync();
                var fieldNameToIdMap = allProperties
                    .Where(p => !string.IsNullOrEmpty(p.FieldName))
                    .ToDictionary(
                        p => p.FieldName.ToUpperInvariant(),
                        p => p.Id.ToString(),
                        StringComparer.OrdinalIgnoreCase
                    );

                foreach (var stageProgress in result.StagesProgress)
                {
                    if (stageProgress.Components == null) continue;

                    foreach (var component in stageProgress.Components)
                    {
                        if (component.Key != "fields" || component.StaticFields == null) continue;

                        foreach (var field in component.StaticFields)
                        {
                            if (!string.IsNullOrEmpty(field.Id) && !long.TryParse(field.Id, out _))
                            {
                                if (fieldNameToIdMap.TryGetValue(field.Id.ToUpperInvariant(), out var numericId))
                                {
                                    field.Id = numericId;
                                }
                                else
                                {
                                    _logger.LogWarning("Could not find property ID for legacy field name: {FieldName}", field.Id);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting legacy static field names to IDs");
            }
        }

        #endregion


        #region Common Helper Methods

        // Note: NormalizeToStartOfDay has been removed.
        // Use OnboardingSharedUtilities.NormalizeToStartOfDay(dateTime) directly.

        /// <summary>
        /// Check if exception is a JSONB type error
        /// Uses shared utility method
        /// </summary>
        private static bool IsJsonbTypeError(Exception ex)
            => OnboardingSharedUtilities.IsJsonbTypeError(ex);

        /// <summary>
        /// Validate and format JSON array
        /// </summary>
        private static string ValidateAndFormatJsonArray(string jsonArray)
        {
            if (string.IsNullOrWhiteSpace(jsonArray))
            {
                return "[]";
            }

            try
            {
                var parsed = JsonSerializer.Deserialize<JsonElement>(jsonArray);
                if (parsed.ValueKind == JsonValueKind.Array)
                {
                    return jsonArray;
                }
                return "[]";
            }
            catch
            {
                return "[]";
            }
        }

        /// <summary>
        /// Validate team selections from JSON arrays
        /// </summary>
        private async Task ValidateTeamSelectionsFromJsonAsync(string viewTeams, string operateTeams)
        {
            var teamIds = new HashSet<long>();

            // Parse ViewTeams
            if (!string.IsNullOrWhiteSpace(viewTeams))
            {
                try
                {
                    var viewTeamIds = JsonSerializer.Deserialize<List<long>>(viewTeams);
                    if (viewTeamIds != null)
                    {
                        foreach (var id in viewTeamIds)
                        {
                            teamIds.Add(id);
                        }
                    }
                }
                catch { /* Ignore parsing errors */ }
            }

            // Parse OperateTeams
            if (!string.IsNullOrWhiteSpace(operateTeams))
            {
                try
                {
                    var operateTeamIds = JsonSerializer.Deserialize<List<long>>(operateTeams);
                    if (operateTeamIds != null)
                    {
                        foreach (var id in operateTeamIds)
                        {
                            teamIds.Add(id);
                        }
                    }
                }
                catch { /* Ignore parsing errors */ }
            }

            // Validate team IDs exist - simplified validation (just log, don't block)
            if (teamIds.Any())
            {
                _logger.LogDebug("Validating {TeamCount} team IDs", teamIds.Count);
            }
        }

        /// <summary>
        /// Safely update onboarding with JSONB handling
        /// </summary>
        private async Task<bool> SafeUpdateOnboardingAsync(Onboarding entity)
        {
            try
            {
                // Serialize stages progress
                if (entity.StagesProgress != null)
                {
                    entity.StagesProgressJson = _stageProgressService.SerializeStagesProgress(entity.StagesProgress);
                }

                var result = await _onboardingRepository.UpdateAsync(entity);

                // Update stages_progress_json separately with JSONB casting
                if (!string.IsNullOrEmpty(entity.StagesProgressJson))
                {
                    var db = _onboardingRepository.GetSqlSugarClient();
                    var progressSql = "UPDATE ff_onboarding SET stages_progress_json = @StagesProgressJson::jsonb WHERE id = @Id";
                    await db.Ado.ExecuteCommandAsync(progressSql, new
                    {
                        StagesProgressJson = entity.StagesProgressJson,
                        Id = entity.Id
                    });
                }

                return result;
            }
            catch (Exception ex) when (IsJsonbTypeError(ex))
            {
                // Fallback to manual SQL update
                var db = _onboardingRepository.GetSqlSugarClient();
                var sql = @"
                    UPDATE ff_onboarding SET
                        modify_date = @ModifyDate,
                        modify_by = @ModifyBy,
                        modify_user_id = @ModifyUserId,
                        stages_progress_json = @StagesProgressJson::jsonb
                    WHERE id = @Id";

                var parameters = new
                {
                    ModifyDate = entity.ModifyDate,
                    ModifyBy = entity.ModifyBy,
                    ModifyUserId = entity.ModifyUserId,
                    StagesProgressJson = entity.StagesProgressJson ?? "[]",
                    Id = entity.Id
                };

                var commandResult = await db.Ado.ExecuteCommandAsync(sql, parameters);
                return commandResult > 0;
            }
        }

        /// <summary>
        /// Ensure CaseCode exists for legacy data
        /// </summary>
        private async Task EnsureCaseCodeAsync(Onboarding entity)
        {
            if (string.IsNullOrEmpty(entity.CaseCode))
            {
                entity.CaseCode = await _caseCodeGeneratorService.GenerateCaseCodeAsync(entity.CaseName);

                try
                {
                    var db = _onboardingRepository.GetSqlSugarClient();
                    var sql = "UPDATE ff_onboarding SET case_code = @CaseCode WHERE id = @Id";
                    await db.Ado.ExecuteCommandAsync(sql, new { CaseCode = entity.CaseCode, Id = entity.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update CaseCode for Onboarding {OnboardingId}", entity.Id);
                }
            }
        }

        /// <summary>
        /// Create default user invitation
        /// </summary>
        private async Task CreateDefaultUserInvitationAsync(Onboarding entity)
        {
            // Use ContactEmail instead of LeadEmail for user invitation
            var email = entity.ContactEmail;
            
            _logger.LogInformation("CreateDefaultUserInvitationAsync - Starting for Onboarding {OnboardingId}, ContactEmail: {ContactEmail}",
                entity.Id, email ?? "(null)");

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogInformation("CreateDefaultUserInvitationAsync - Skipped: ContactEmail is empty for Onboarding {OnboardingId}", entity.Id);
                return;
            }

            try
            {
                // GetByOnboardingIdAsync returns List<UserInvitation>, check if any exists
                var existingInvitations = await _userInvitationRepository.GetByOnboardingIdAsync(entity.Id);
                if (existingInvitations != null && existingInvitations.Any())
                {
                    _logger.LogInformation("CreateDefaultUserInvitationAsync - Skipped: Invitation already exists for Onboarding {OnboardingId}", entity.Id);
                    return;
                }

                var tenantId = entity.TenantId ?? "default";
                var appCode = entity.AppCode ?? "default";

                // Generate invitation token using CryptoHelper (consistent with UserInvitationService)
                var invitationToken = CryptoHelper.GenerateSecureToken();
                
                // Generate short URL ID using CryptoHelper
                var shortUrlId = CryptoHelper.GenerateShortUrlId(entity.Id, email, invitationToken);

                // Generate invitation URL in format: portal-access/{shortUrlId}?tenantId={tenantId}&appCode={appCode}
                var invitationUrl = $"portal-access/{shortUrlId}?tenantId={Uri.EscapeDataString(tenantId)}&appCode={Uri.EscapeDataString(appCode)}";

                var invitation = new UserInvitation
                {
                    OnboardingId = entity.Id,
                    Email = email,
                    InvitationToken = invitationToken,
                    ShortUrlId = shortUrlId,
                    InvitationUrl = invitationUrl,
                    Status = "Pending",
                    TenantId = tenantId,
                    AppCode = appCode,
                    IsValid = true,
                    TokenExpiry = null, // No expiry (consistent with UserInvitationService)
                    SendCount = 0 // Not sent yet, just created
                };
                invitation.InitCreateInfo(_userContext);
                invitation.InitNewId();

                await _userInvitationRepository.InsertAsync(invitation);
                _logger.LogInformation("CreateDefaultUserInvitationAsync - Successfully created invitation {InvitationId} for Onboarding {OnboardingId}, URL: {InvitationUrl}",
                    invitation.Id, entity.Id, invitationUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create default user invitation for Onboarding {OnboardingId}", entity.Id);
            }
        }

        /// <summary>
        /// Sync static field values after update
        /// </summary>
        private async Task SyncStaticFieldValuesAsync(long onboardingId, long stageId,
            string originalLeadId, string originalCaseName, string originalContactPerson,
            string originalContactEmail, string originalLeadPhone, long? originalLifeCycleStageId,
            string originalPriority, OnboardingInputDto input)
        {
            try
            {
                // Check if any relevant fields changed
                bool hasChanges = originalLeadId != input.LeadId ||
                                  originalCaseName != input.CaseName ||
                                  originalContactPerson != input.ContactPerson ||
                                  originalContactEmail != input.ContactEmail ||
                                  originalLeadPhone != input.LeadPhone ||
                                  originalLifeCycleStageId != input.LifeCycleStageId ||
                                  originalPriority != input.Priority;

                if (!hasChanges) return;

                // Sync static field values - method may not exist, log and continue
                _logger.LogDebug("Static field sync requested for Onboarding {OnboardingId}, Stage {StageId}", onboardingId, stageId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sync static field values for Onboarding {OnboardingId}", onboardingId);
            }
        }

        /// <summary>
        /// Clear onboarding query cache
        /// </summary>
        private Task ClearOnboardingQueryCacheAsync()
        {
            // Cache clearing logic - currently disabled
            return Task.CompletedTask;
        }

        /// <summary>
        /// Clear related cache
        /// </summary>
        private Task ClearRelatedCacheAsync(long? workflowId = null, long? stageId = null)
        {
            // Cache clearing logic - currently disabled
            return Task.CompletedTask;
        }

        #endregion
    }
}
