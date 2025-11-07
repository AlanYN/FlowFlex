using AutoMapper;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.Dtos.OW.Permission;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Attr;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Events;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Utils;
using FlowFlex.Infrastructure.Extensions;
using FlowFlex.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using SqlSugar;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
// using Item.Redis; // Temporarily disable Redis
using System.Text.Json;
using PermissionOperationType = FlowFlex.Domain.Shared.Enums.Permission.OperationTypeEnum;
using FlowFlex.Application.Contracts.Dtos.OW.User;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Onboarding service implementation
    /// </summary>
    public class OnboardingService : IOnboardingService
    {
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IStageRepository _stageRepository;
        private readonly IUserInvitationRepository _userInvitationRepository;

        private readonly IMapper _mapper;
        private readonly UserContext _userContext;
        private readonly IMediator _mediator;
        private readonly IStageService _stageService;
        private readonly IChecklistTaskCompletionService _checklistTaskCompletionService;
        private readonly IQuestionnaireAnswerService _questionnaireAnswerService;
        private readonly IStaticFieldValueService _staticFieldValueService;
        private readonly IChecklistService _checklistService;
        private readonly IQuestionnaireService _questionnaireService;
        private readonly IOperatorContextService _operatorContextService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly IActionManagementService _actionManagementService;
        private readonly IOperationChangeLogService _operationChangeLogService;
        private readonly IPermissionService _permissionService;
        private readonly Permission.CasePermissionService _casePermissionService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserService _userService;
        private readonly ICaseCodeGeneratorService _caseCodeGeneratorService;

        // Cache key constants - temporarily disable Redis cache
        private const string WORKFLOW_CACHE_PREFIX = "ow:workflow";
        private const string STAGE_CACHE_PREFIX = "ow:stage";
        private const int CACHE_EXPIRY_MINUTES = 30; // Cache for 30 minutes

        public OnboardingService(
            IOnboardingRepository onboardingRepository,
            IWorkflowRepository workflowRepository,
            IStageRepository stageRepository,
            IUserInvitationRepository userInvitationRepository,

            IMapper mapper,
            UserContext userContext,
            IMediator mediator,
            IStageService stageService,
            IChecklistTaskCompletionService checklistTaskCompletionService,
            IQuestionnaireAnswerService questionnaireAnswerService,
            IStaticFieldValueService staticFieldValueService,
            IChecklistService checklistService,
            IQuestionnaireService questionnaireService,
            IOperatorContextService operatorContextService,
            IServiceScopeFactory serviceScopeFactory,
            IBackgroundTaskQueue backgroundTaskQueue,
            IActionManagementService actionManagementService,
            IOperationChangeLogService operationChangeLogService,
            IPermissionService permissionService,
            Permission.CasePermissionService casePermissionService,
            IHttpContextAccessor httpContextAccessor,
            IUserService userService,
            ICaseCodeGeneratorService caseCodeGeneratorService)
        {
            _onboardingRepository = onboardingRepository ?? throw new ArgumentNullException(nameof(onboardingRepository));
            _workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
            _stageRepository = stageRepository ?? throw new ArgumentNullException(nameof(stageRepository));
            _userInvitationRepository = userInvitationRepository ?? throw new ArgumentNullException(nameof(userInvitationRepository));

            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _stageService = stageService ?? throw new ArgumentNullException(nameof(stageService));
            _checklistTaskCompletionService = checklistTaskCompletionService ?? throw new ArgumentNullException(nameof(checklistTaskCompletionService));
            _questionnaireAnswerService = questionnaireAnswerService ?? throw new ArgumentNullException(nameof(questionnaireAnswerService));
            _staticFieldValueService = staticFieldValueService ?? throw new ArgumentNullException(nameof(staticFieldValueService));
            _httpContextAccessor = httpContextAccessor;
            _checklistService = checklistService ?? throw new ArgumentNullException(nameof(checklistService));
            _questionnaireService = questionnaireService ?? throw new ArgumentNullException(nameof(questionnaireService));
            _operatorContextService = operatorContextService ?? throw new ArgumentNullException(nameof(operatorContextService));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _backgroundTaskQueue = backgroundTaskQueue ?? throw new ArgumentNullException(nameof(backgroundTaskQueue));
            _actionManagementService = actionManagementService ?? throw new ArgumentNullException(nameof(actionManagementService));
            _operationChangeLogService = operationChangeLogService ?? throw new ArgumentNullException(nameof(operationChangeLogService));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _casePermissionService = casePermissionService ?? throw new ArgumentNullException(nameof(casePermissionService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _caseCodeGeneratorService = caseCodeGeneratorService ?? throw new ArgumentNullException(nameof(caseCodeGeneratorService));
        }

        /// <summary>
        /// Create a new onboarding instance
        /// </summary>
        public async Task<long> CreateAsync(OnboardingInputDto input)
        {
            try
            {
                // Debug logging handled by structured logging
                // Check all injected dependencies
                if (_onboardingRepository == null)
                {
                    // Debug logging handled by structured logging
                    throw new CRMException(ErrorCodeEnum.SystemError, "Onboarding repository is not available");
                }

                if (_workflowRepository == null)
                {
                    // Debug logging handled by structured logging
                    throw new CRMException(ErrorCodeEnum.SystemError, "Workflow repository is not available");
                }

                if (_stageRepository == null)
                {
                    // Debug logging handled by structured logging
                    throw new CRMException(ErrorCodeEnum.SystemError, "Stage repository is not available");
                }

                if (_mapper == null)
                {
                    // Debug logging handled by structured logging
                    throw new CRMException(ErrorCodeEnum.SystemError, "Mapper is not available");
                }

                if (_userContext == null)
                {
                    // Debug logging handled by structured logging
                    throw new CRMException(ErrorCodeEnum.SystemError, "User context is not available");
                }

                if (input == null)
                {
                    // Debug logging handled by structured logging
                    throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input parameter cannot be null");
                }
                // Debug logging handled by structured logging
                // Ensure the table exists before inserting
                // Debug logging handled by structured logging
                await _onboardingRepository.EnsureTableExistsAsync();
                // Debug logging handled by structured logging
                // Get tenant ID and app code from UserContext (injected from HTTP headers via middleware)
                string tenantId = _userContext?.TenantId ?? "default";
                string appCode = _userContext?.AppCode ?? "default";
                // Debug logging handled by structured logging
                // Handle default workflow selection if WorkflowId is not provided
                // Debug logging handled by structured logging ?? "null"} ===");

                if (!input.WorkflowId.HasValue || input.WorkflowId.Value <= 0)
                {
                    // Debug logging handled by structured logging
                    var defaultWorkflow = await _workflowRepository.GetDefaultWorkflowAsync();

                    if (defaultWorkflow != null && defaultWorkflow.IsValid && defaultWorkflow.IsActive)
                    {
                        input.WorkflowId = defaultWorkflow.Id;
                        // Debug logging handled by structured logging
                    }
                    else
                    {
                        // Debug logging handled by structured logging
                        var activeWorkflows = await _workflowRepository.GetActiveWorkflowsAsync();
                        var firstActiveWorkflow = activeWorkflows?.FirstOrDefault();

                        if (firstActiveWorkflow != null)
                        {
                            input.WorkflowId = firstActiveWorkflow.Id;
                            // Debug logging handled by structured logging
                        }
                        else
                        {
                            throw new CRMException(ErrorCodeEnum.DataNotFound, "No default or active workflow found. Please specify a valid WorkflowId or configure a default workflow.");
                        }
                    }
                }
                // Debug logging handled by structured logging
                // Check if Lead ID already exists for this tenant with enhanced checking (only if LeadId is provided)
                // Debug logging handled by structured logging
                // Use SqlSugar client directly for more precise checking
                var sqlSugarClient = _onboardingRepository.GetSqlSugarClient();
                
                if (!string.IsNullOrWhiteSpace(input.LeadId))
                {
                    var existingActiveOnboarding = await sqlSugarClient.Queryable<Onboarding>()
                        .Where(x => x.TenantId == tenantId &&
                                   x.AppCode == appCode &&
                                   x.LeadId == input.LeadId &&
                                   x.IsValid == true &&
                                   x.IsActive == true)
                        .FirstAsync();

                    if (existingActiveOnboarding != null)
                    {
                        // Debug logging handled by structured logging
                        throw new CRMException(ErrorCodeEnum.BusinessError,
                            $"An active onboarding already exists for Lead ID '{input.LeadId}' in tenant '{tenantId}', app '{appCode}'. " +
                            $"Existing onboarding ID: {existingActiveOnboarding.Id}, Status: {existingActiveOnboarding.Status}");
                    }
                }
                // Debug logging handled by structured logging
                // Validate workflow exists with detailed logging
                // Debug logging handled by structured logging
                var workflow = await _workflowRepository.GetByIdAsync(input.WorkflowId.Value);
                // Debug logging handled by structured logging}");

                if (workflow != null)
                {
                    // Debug logging handled by structured logging
                }

                if (workflow == null || !workflow.IsValid)
                {
                    // Try to get all workflows to see what's available
                    // Debug logging handled by structured logging
                    try
                    {
                        var allWorkflows = await _workflowRepository.GetListAsync(x => x.IsValid);
                        // Debug logging handled by structured logging
                        foreach (var w in allWorkflows.Take(5)) // Show first 5
                        {
                            // Debug logging handled by structured logging
                        }
                    }
                    catch (Exception ex)
                    {
                        // Debug logging handled by structured logging
                    }

                    throw new CRMException(ErrorCodeEnum.DataNotFound, $"Workflow not found for ID: {input.WorkflowId.Value}");
                }

                // Get first stage of the workflow with detailed logging
                // Debug logging handled by structured logging
                var stages = await _stageRepository.GetByWorkflowIdAsync(input.WorkflowId.Value);
                // Debug logging handled by structured logging
                var firstStage = stages.OrderBy(x => x.Order).FirstOrDefault();
                if (firstStage == null)
                {
                    // Debug logging handled by structured logging
                    // Instead of failing, let's allow creation without stages and warn
                    // Debug logging handled by structured logging
                }
                else
                {
                    // Debug logging handled by structured logging
                }

                // Create new onboarding entity
                var entity = _mapper.Map<Onboarding>(input);
                
                // Generate Case Code from Lead Name
                entity.CaseCode = await _caseCodeGeneratorService.GenerateCaseCodeAsync(input.LeadName);
                
                // Debug logging handled by structured logging
                // Set initial values with explicit null checks
                entity.CurrentStageId = firstStage?.Id;
                entity.CurrentStageOrder = firstStage?.Order ?? 0;
                entity.Status = string.IsNullOrEmpty(entity.Status) ? "Inactive" : entity.Status;

                // 添加调试日志 - 检查 CurrentStageId 是否正确设置
                LoggingExtensions.WriteLine($"[DEBUG] Onboarding Create - CurrentStageId set to: {entity.CurrentStageId}, CurrentStageOrder: {entity.CurrentStageOrder}, FirstStage: {firstStage?.Id}");
                LoggingExtensions.WriteLine($"[DEBUG] Onboarding Status: ID={entity.Id}, Status={entity.Status}");
                entity.StartDate = entity.StartDate ?? DateTimeOffset.UtcNow;

                // IMPORTANT: Do NOT set CurrentStageStartTime during creation
                // CurrentStageStartTime should only be set when:
                // 1. Onboarding is started (status changes to Active/InProgress/Started)
                // 2. Stage is saved for the first time
                // 3. Stage is completed and advances to next stage
                entity.CurrentStageStartTime = null;

                entity.CompletionRate = 0;
                entity.IsPrioritySet = false;
                entity.Priority = string.IsNullOrEmpty(entity.Priority) ? "Medium" : entity.Priority;
                entity.IsActive = true;

                // Initialize stages progress as empty JSON array for JSONB compatibility
                entity.StagesProgressJson = "[]";

                // Initialize create information with proper ID and timestamps
                entity.InitCreateInfo(_userContext);
                AuditHelper.ApplyCreateAudit(entity, _operatorContextService);


                // Debug logging handled by structured logging
                // Generate unique ID if not set
                if (entity.Id == 0)
                {
                    entity.InitNewId();
                }
                // Debug logging handled by structured logging
                // Validate entity before insertion
                if (entity.WorkflowId <= 0)
                {
                    throw new CRMException(ErrorCodeEnum.ParamInvalid, "WorkflowId must be greater than 0");
                }

                // LeadId is now optional since Case Code is the primary identifier
                // if (string.IsNullOrWhiteSpace(entity.LeadId))
                // {
                //     throw new CRMException(ErrorCodeEnum.ParamInvalid, "LeadId cannot be null or empty");
                // }

                if (string.IsNullOrWhiteSpace(entity.TenantId))
                {
                    throw new CRMException(ErrorCodeEnum.ParamInvalid, "TenantId cannot be null or empty");
                }

                // Ensure Id is generated (SqlSugar expects this for some operations)
                if (entity.Id == 0)
                {
                    entity.Id = SnowFlakeSingle.Instance.NextId();
                    // Debug logging handled by structured logging
                }
                // Debug logging handled by structured logging
                // Use a completely simplified approach to avoid SqlSugar issues
                long insertedId;

                try
                {
                    // Generate snowflake ID for the entity
                    entity.InitNewId();
                    // Debug logging handled by structured logging
                    // Create table if not exists first
                    // Debug logging handled by structured logging
                    if (!sqlSugarClient.DbMaintenance.IsAnyTable("ff_onboarding"))
                    {
                        // Debug logging handled by structured logging
                        sqlSugarClient.CodeFirst.SetStringDefaultLength(200).InitTables<Onboarding>();
                    }

                    // Use simple insert with explicit JSONB handling
                    // Debug logging handled by structured logging
                    var insertResult = await sqlSugarClient.Insertable(entity)
                        .IgnoreColumns(it => new { it.StagesProgress }) // Ignore the non-mapped property
                        .ExecuteCommandAsync();

                    if (insertResult > 0)
                    {
                        // Debug logging handled by structured logging
                        // Get the last inserted record by Case Code (unique identifier)
                        var lastInserted = await sqlSugarClient.Queryable<Onboarding>()
                            .Where(x => x.CaseCode == entity.CaseCode &&
                                       x.TenantId == entity.TenantId &&
                                       x.AppCode == entity.AppCode)
                            .OrderByDescending(x => x.CreateDate)
                            .FirstAsync();

                        if (lastInserted != null)
                        {
                            insertedId = lastInserted.Id;
                            // Debug logging handled by structured logging
                        }
                        else
                        {
                            // Debug logging handled by structured logging
                            // If insert was successful but we can't find the record, still return success
                            // This might happen due to timing or indexing issues
                            insertedId = 0; // Return 0 to indicate success but unknown ID
                                            // Debug logging handled by structured logging
                        }
                    }
                    else
                    {
                        // Debug logging handled by structured logging
                        throw new CRMException(ErrorCodeEnum.SystemError, "Insert failed - no rows were affected");
                    }
                }
                catch (Exception insertEx)
                {
                    // Debug logging handled by structured logging
                    // Debug logging handled by structured logging.Name}");
                    // Debug logging handled by structured logging
                    // Check if this is a duplicate key error
                    if (insertEx.Message.Contains("23505") && insertEx.Message.Contains("idx_ff_onboarding_unique_lead"))
                    {
                        // This is specifically the unique constraint violation we're dealing with
                        // Debug logging handled by structured logging
                        throw new CRMException(ErrorCodeEnum.BusinessError,
                            $"A duplicate onboarding record was detected for Lead ID '{entity.LeadId}' in tenant '{entity.TenantId}'. " +
                            $"This may be due to concurrent requests or an existing active record. Please check existing onboardings and try again.");
                    }

                    // Final fallback: manual SQL with minimal fields
                    // Debug logging handled by structured logging
                    try
                    {
                        var sql = @"
                INSERT INTO ff_onboarding (
                    tenant_id, app_code, is_valid, create_date, modify_date, create_by, modify_by,
                    create_user_id, modify_user_id, workflow_id, current_stage_order,
                    lead_id, lead_name, case_code, lead_email, lead_phone, status, completion_rate,
                    priority, is_priority_set, ownership, ownership_name, ownership_email,
                    notes, is_active, stages_progress_json, id,
                    current_stage_id, contact_person, contact_email, life_cycle_stage_id, 
                    life_cycle_stage_name, start_date, current_stage_start_time,
                    view_permission_subject_type, operate_permission_subject_type,
                    view_permission_mode, view_teams, view_users, operate_teams, operate_users
                ) VALUES (
                    @TenantId, @AppCode, @IsValid, @CreateDate, @ModifyDate, @CreateBy, @ModifyBy,
                    @CreateUserId, @ModifyUserId, @WorkflowId, @CurrentStageOrder,
                    @LeadId, @LeadName, @CaseCode, @LeadEmail, @LeadPhone, @Status, @CompletionRate,
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
                            LeadName = entity.LeadName,
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
                        // Debug logging handled by structured logging
                        insertedId = await sqlSugarClient.Ado.SqlQuerySingleAsync<long>(sql, parameters);
                        // Debug logging handled by structured logging
                    }
                    catch (Exception sqlEx)
                    {
                        // Debug logging handled by structured logging
                        // Check if this is also a duplicate key error
                        if (sqlEx.Message.Contains("23505") && sqlEx.Message.Contains("idx_ff_onboarding_unique_lead"))
                        {
                            throw new CRMException(ErrorCodeEnum.BusinessError,
                                $"A duplicate onboarding record exists for Lead ID '{entity.LeadId}' in tenant '{entity.TenantId}'. " +
                                $"Please check existing onboardings and ensure the Lead ID is unique within the tenant.");
                        }

                        throw new CRMException(ErrorCodeEnum.SystemError,
                            $"All insertion methods failed. Simple insert: {insertEx.Message}, Minimal SQL: {sqlEx.Message}");
                    }
                }

                // Initialize stage progress after successful creation
                if (insertedId > 0)
                {
                    try
                    {
                        // Re-fetch the inserted entity to ensure we have complete data
                        var insertedEntity = await _onboardingRepository.GetByIdAsync(insertedId);
                        if (insertedEntity != null)
                        {
                            // Initialize stage progress
                            await InitializeStagesProgressAsync(insertedEntity, stages);

                            // Update entity to save stage progress using safe method
                            var updateResult = await SafeUpdateOnboardingAsync(insertedEntity);
                            if (!updateResult)
                            {
                                // Log warning but don't fail the creation
                                // Debug logging handled by structured logging
                            }
                            else
                            {
                                // Debug logging handled by structured logging
                            }

                            // Create default UserInvitation record if email is available
                            await CreateDefaultUserInvitationAsync(insertedEntity);
                        }
                        else
                        {
                            // Debug logging handled by structured logging
                        }
                    }
                    catch (Exception ex)
                    {
                        // Debug logging handled by structured logging
                        // Important: Re-throw if this is a critical initialization failure
                        // But first check if it's just a minor update issue
                        if (ex.Message.Contains("JSONB") || ex.Message.Contains("stages_progress"))
                        {
                            // This might be the JSONB conversion issue, don't fail the entire creation
                            // Debug logging handled by structured logging
                        }
                        else
                        {
                            // For other critical errors, we might want to throw
                            // Debug logging handled by structured logging
                        }
                    }

                    // Clear query cache (async execution, doesn't affect main flow)
                    _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                    {
                        try
                        {
                            await ClearOnboardingQueryCacheAsync();
                            // Debug logging handled by structured logging
                        }
                        catch (Exception ex)
                        {
                            // Debug logging handled by structured logging
                        }
                    });
                }

                return insertedId;
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                // Debug logging handled by structured logging.Name}");
                // Debug logging handled by structured logging
                if (ex.InnerException != null)
                {
                    // Debug logging handled by structured logging
                }
                // Debug logging handled by structured logging
                throw;
            }
        }

        /// <summary>
        /// Update an existing onboarding
        /// </summary>
        public async Task<bool> UpdateAsync(long id, OnboardingInputDto input)
        {
            try
            {
                // Debug logging handled by structured logging
                var entity = await _onboardingRepository.GetByIdAsync(id);
                if (entity == null)
                {
                    // Debug logging handled by structured logging
                    throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
                }
                // Debug logging handled by structured logging
                // Record original workflow and stage ID for cache cleanup
                var originalWorkflowId = entity.WorkflowId;
                var originalStageId = entity.CurrentStageId;

                // Store original values for static field sync comparison
                var originalLeadId = entity.LeadId;
                var originalLeadName = entity.LeadName;
                var originalContactPerson = entity.ContactPerson;
                var originalContactEmail = entity.ContactEmail;
                var originalLeadPhone = entity.LeadPhone;
                var originalLifeCycleStageId = entity.LifeCycleStageId;
                var originalPriority = entity.Priority;

                // Track if workflow changed to preserve CurrentStageId after mapping
                bool workflowChanged = false;
                long? preservedCurrentStageId = null;
                int? preservedCurrentStageOrder = null;

                // If workflow changed, validate new workflow and reset stages
                if (entity.WorkflowId != input.WorkflowId)
                {
                    workflowChanged = true;

                    // Business Rule 1: Only allow workflow change for cases with status "Started"
                    if (entity.Status != "Started")
                    {
                        throw new CRMException(
                            ErrorCodeEnum.OperationNotAllowed,
                            $"Cannot change workflow for a case with status '{entity.Status}'. Only cases with status 'Started' can change workflow.");
                    }

                    // Business Rule 2: Only allow workflow change for cases that haven't started yet
                    // "Unstarted" is defined as all stages having isCompleted: false and isSaved: false
                    LoadStagesProgressFromJson(entity); // Ensure stagesProgress is loaded

                    bool isUnstarted = entity.StagesProgress == null ||
                                       entity.StagesProgress.Count == 0 ||
                                       entity.StagesProgress.All(sp => !sp.IsCompleted && !sp.IsSaved);

                    if (!isUnstarted)
                    {
                        throw new CRMException(
                            ErrorCodeEnum.OperationNotAllowed,
                            $"Cannot change workflow for a case that has already started or has saved progress. Current status: {entity.Status}");
                    }

                    // Debug logging handled by structured logging
                    var workflow = await _workflowRepository.GetByIdAsync(input.WorkflowId);
                    if (workflow == null)
                    {
                        // Debug logging handled by structured logging
                        throw new CRMException(ErrorCodeEnum.DataNotFound, "Workflow not found");
                    }
                    // Debug logging handled by structured logging
                    var stages = await _stageRepository.GetByWorkflowIdAsync(input.WorkflowId.Value);
                    var firstStage = stages.OrderBy(x => x.Order).FirstOrDefault();

                    // Debug logging handled by structured logging}");

                    // Store the new stage ID and order to preserve after mapping
                    preservedCurrentStageId = firstStage?.Id;
                    preservedCurrentStageOrder = firstStage?.Order ?? 1;

                    entity.CurrentStageId = preservedCurrentStageId;
                    entity.CurrentStageOrder = preservedCurrentStageOrder.Value;
                    entity.CompletionRate = 0;

                    // Re-initialize stagesProgress with new workflow's stages
                    await InitializeStagesProgressAsync(entity, stages.ToList());
                }

                // Map the input to entity (this will update all the mappable fields)
                _mapper.Map(input, entity);

                // IMPORTANT: If workflow changed, restore the CurrentStageId to prevent input from overwriting it
                // When workflow changes, CurrentStageId must be the first stage of the new workflow
                if (workflowChanged && preservedCurrentStageId.HasValue)
                {
                    entity.CurrentStageId = preservedCurrentStageId;
                    entity.CurrentStageOrder = preservedCurrentStageOrder.Value;
                }

                // Note: We preserve all permission fields (both Teams and Users) regardless of PermissionSubjectType
                // This allows users to switch between Team/User modes without losing data
                // The frontend will use PermissionSubjectType to determine which fields to display/use

                // Update system fields
                // Validate team IDs in ViewTeams and OperateTeams (JSON arrays)
                await ValidateTeamSelectionsFromJsonAsync(entity.ViewTeams, entity.OperateTeams);

                // Update system fields
                entity.ModifyDate = DateTimeOffset.UtcNow;
                entity.ModifyBy = _operatorContextService.GetOperatorDisplayName();
                entity.ModifyUserId = _operatorContextService.GetOperatorId();
                // Debug logging handled by structured logging
                var result = await SafeUpdateOnboardingAsync(entity);

                // Log onboarding update and clear cache
                if (result)
                {
                    // Sync static field values
                    // If current stage exists, use it; otherwise try to get the first stage from workflow
                    long? targetStageId = entity.CurrentStageId;

                    if (!targetStageId.HasValue && entity.WorkflowId > 0)
                    {
                        // Try to get first stage from workflow
                        var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
                        var firstStage = stages.OrderBy(s => s.Order).FirstOrDefault();
                        targetStageId = firstStage?.Id;
                    }

                    if (targetStageId.HasValue)
                    {
                        await SyncStaticFieldValuesAsync(
                            entity.Id,
                            targetStageId.Value,
                            originalLeadId,
                            originalLeadName,
                            originalContactPerson,
                            originalContactEmail,
                            originalLeadPhone,
                            originalLifeCycleStageId,
                            originalPriority,
                            input
                        );
                    }
                    else
                    {
                        // Log when static field sync is skipped
                        Console.WriteLine($"[OnboardingService] Static field sync skipped - No stage found for Onboarding {entity.Id}");
                    }

                    await LogOnboardingActionAsync(entity, "Update Onboarding", "onboarding_update", true, new
                    {
                        UpdatedFields = new
                        {
                            input.LeadName,
                            input.Priority,
                            input.CurrentAssigneeName,
                            input.CurrentTeam,
                            input.Notes,
                            input.CustomFieldsJson
                        },
                        UpdatedBy = _operatorContextService.GetOperatorDisplayName(),
                        UpdatedAt = DateTimeOffset.UtcNow
                    });

                    // Clear related cache data (async execution, doesn't affect main flow)
                    _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                    {
                        try
                        {
                            var cacheCleanupTasks = new List<Task>();

                            // Clear query cache
                            cacheCleanupTasks.Add(ClearOnboardingQueryCacheAsync());

                            // If workflow changed, clear cache for both original and new workflow
                            if (originalWorkflowId != entity.WorkflowId)
                            {
                                cacheCleanupTasks.Add(ClearRelatedCacheAsync(originalWorkflowId));
                                cacheCleanupTasks.Add(ClearRelatedCacheAsync(entity.WorkflowId));
                            }
                            else
                            {
                                cacheCleanupTasks.Add(ClearRelatedCacheAsync(entity.WorkflowId));
                            }

                            // If stage changed, clear related stage cache
                            if (originalStageId != entity.CurrentStageId)
                            {
                                if (originalStageId.HasValue)
                                {
                                    cacheCleanupTasks.Add(ClearRelatedCacheAsync(null, originalStageId.Value));
                                }
                                if (entity.CurrentStageId.HasValue)
                                {
                                    cacheCleanupTasks.Add(ClearRelatedCacheAsync(null, entity.CurrentStageId.Value));
                                }
                            }

                            await Task.WhenAll(cacheCleanupTasks);
                            // Debug logging handled by structured logging
                        }
                        catch (Exception ex)
                        {
                            // Debug logging handled by structured logging
                        }
                    });
                }
                // Debug logging handled by structured logging
                return result;
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                // Debug logging handled by structured logging.Name}");
                // Debug logging handled by structured logging
                if (ex.InnerException != null)
                {
                    // Debug logging handled by structured logging
                }
                // Debug logging handled by structured logging
                throw;
            }
        }
        /// <summary>
        /// Delete an onboarding (with confirmation)
        /// </summary>
        public async Task<bool> DeleteAsync(long id, bool confirm = false)
        {
            if (!confirm)
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed, "Delete operation requires confirmation");
            }
            // Debug logging handled by structured logging
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

                if (entityWithoutFilter != null)
                {
                    // Debug logging handled by structured logging
                }
                else
                {
                    // Debug logging handled by structured logging
                }
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
            }

            // Query using normal repository method (with tenant filter)
            var entity = await _onboardingRepository.GetByIdAsync(id);
            // Debug logging handled by structured logging}");

            if (entity == null || !entity.IsValid)
            {
                // If record exists but tenant doesn't match, provide more detailed error information
                if (entityWithoutFilter != null)
                {
                    if (entityWithoutFilter.TenantId != _userContext.TenantId)
                    {
                        // Debug logging handled by structured logging
                        throw new CRMException(ErrorCodeEnum.DataNotFound, $"Onboarding not found or access denied. Record belongs to different tenant.");
                    }
                    else if (!entityWithoutFilter.IsValid)
                    {
                        // Debug logging handled by structured logging");
                        throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding has already been deleted");
                    }
                }

                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            // Use soft delete instead of hard delete
            entity.IsValid = false;
            entity.ModifyDate = DateTimeOffset.UtcNow;
            entity.ModifyBy = _operatorContextService.GetOperatorDisplayName();
            entity.ModifyUserId = _operatorContextService.GetOperatorId();

            // Update only specific columns to avoid JSONB type conversion issues
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

            // Clear related cache after successful deletion
            if (result)
            {
                await ClearOnboardingQueryCacheAsync();
                await ClearRelatedCacheAsync(entity.WorkflowId, entity.CurrentStageId);
            }

            return result;
        }

        /// <summary>
        /// Get onboarding by ID
        /// </summary>
        public async Task<OnboardingOutputDto> GetByIdAsync(long id)
        {
            try
            {
                var entity = await _onboardingRepository.GetByIdAsync(id);
                if (entity == null)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
                }

                // Auto-generate CaseCode for legacy data
                await EnsureCaseCodeAsync(entity);

                // Ensure stages progress is properly initialized and synced
                await EnsureStagesProgressInitializedAsync(entity);

                var result = _mapper.Map<OnboardingOutputDto>(entity);

                // Get workflow name
                var workflow = await _workflowRepository.GetByIdAsync(entity.WorkflowId);
                result.WorkflowName = workflow?.Name;

                // IMPORTANT: If CurrentStageId is null but stagesProgress exists, try to get current stage from stagesProgress
                if (!entity.CurrentStageId.HasValue && result.StagesProgress != null && result.StagesProgress.Any())
                {
                    var currentStageProgress = result.StagesProgress.FirstOrDefault(sp => sp.IsCurrent);
                    if (currentStageProgress != null)
                    {
                        entity.CurrentStageId = currentStageProgress.StageId;
                        result.CurrentStageId = currentStageProgress.StageId;
                        LoggingExtensions.WriteLine($"[DEBUG] GetByIdAsync - Recovered CurrentStageId from StagesProgress: {entity.CurrentStageId} for Onboarding {id}");

                        // Update database to fix the missing CurrentStageId
                        try
                        {
                            var updateSql = "UPDATE ff_onboarding SET current_stage_id = @CurrentStageId WHERE id = @Id";
                            await _onboardingRepository.GetSqlSugarClient().Ado.ExecuteCommandAsync(updateSql, new
                            {
                                CurrentStageId = entity.CurrentStageId.Value,
                                Id = id
                            });
                            LoggingExtensions.WriteLine($"[DEBUG] GetByIdAsync - Updated database with CurrentStageId: {entity.CurrentStageId} for Onboarding {id}");
                        }
                        catch (Exception ex)
                        {
                            LoggingExtensions.WriteLine($"[ERROR] GetByIdAsync - Failed to update CurrentStageId in database: {ex.Message}");
                        }
                    }
                }

                // currentStageStartTime 只取 startTime（无则为null）
                result.CurrentStageStartTime = null;
                result.CurrentStageEndTime = null;
                double? estimatedDays = null;
                if (entity.CurrentStageId.HasValue && result.StagesProgress != null && result.StagesProgress.Any())
                {
                    var currentStageProgress = result.StagesProgress.FirstOrDefault(sp => sp.StageId == entity.CurrentStageId.Value);
                    if (currentStageProgress != null)
                    {
                        if (currentStageProgress.StartTime.HasValue)
                        {
                            result.CurrentStageStartTime = currentStageProgress.StartTime;
                        }
                        // currentStageEndTime 优先级: customEndTime > endTime > (startTime+estimatedDays) > null
                        if (currentStageProgress.CustomEndTime.HasValue)
                        {
                            result.CurrentStageEndTime = currentStageProgress.CustomEndTime.Value;
                        }
                        else if (currentStageProgress.EndTime.HasValue)
                        {
                            result.CurrentStageEndTime = currentStageProgress.EndTime.Value;
                        }
                        else
                        {
                            // 三级优先：json.customEstimatedDays > json.estimatedDays > stage实体
                            estimatedDays = (double?)currentStageProgress.CustomEstimatedDays;
                            if (!estimatedDays.HasValue || estimatedDays.Value <= 0)
                            {
                                estimatedDays = (double?)currentStageProgress.EstimatedDays;
                                if ((!estimatedDays.HasValue || estimatedDays.Value <= 0) && entity.CurrentStageId.HasValue)
                                {
                                    var stage = await _stageRepository.GetByIdAsync(entity.CurrentStageId.Value);
                                    if (stage?.EstimatedDuration != null && stage.EstimatedDuration > 0)
                                        estimatedDays = (double?)stage.EstimatedDuration;
                                }
                            }
                        }
                    }
                }
                // 单独推算 currentStageEndTime——仅当startTime和estimatedDays都存在
                if (result.CurrentStageEndTime == null && result.CurrentStageStartTime.HasValue && (estimatedDays.HasValue && estimatedDays.Value > 0))
                {
                    result.CurrentStageEndTime = result.CurrentStageStartTime.Value.AddDays(estimatedDays.Value);
                }

                // Get current stage name and estimated days
                if (entity.CurrentStageId.HasValue)
                {
                    var stage = await _stageRepository.GetByIdAsync(entity.CurrentStageId.Value);
                    result.CurrentStageName = stage?.Name;

                    // IMPORTANT: Priority for EstimatedDays: customEstimatedDays > stage.EstimatedDuration
                    var currentStageProgress = result.StagesProgress?.FirstOrDefault(sp => sp.StageId == entity.CurrentStageId.Value);
                    if (currentStageProgress != null && currentStageProgress.CustomEstimatedDays.HasValue && currentStageProgress.CustomEstimatedDays.Value > 0)
                    {
                        result.CurrentStageEstimatedDays = currentStageProgress.CustomEstimatedDays.Value;
                        LoggingExtensions.WriteLine($"[DEBUG] GetByIdAsync - Using customEstimatedDays: {result.CurrentStageEstimatedDays} for Stage {entity.CurrentStageId}");
                    }
                    else
                    {
                        result.CurrentStageEstimatedDays = stage?.EstimatedDuration;
                    }

                    // Fallback: if still missing, fetch stage directly
                    if ((!result.CurrentStageEstimatedDays.HasValue || result.CurrentStageEstimatedDays.Value <= 0) && entity.CurrentStageId.HasValue)
                    {
                        try
                        {
                            var stageFallback = await _stageRepository.GetByIdAsync(entity.CurrentStageId.Value);
                            if (stageFallback?.EstimatedDuration != null && stageFallback.EstimatedDuration > 0)
                            {
                                result.CurrentStageEstimatedDays = stageFallback.EstimatedDuration;
                                LoggingExtensions.WriteLine($"[DEBUG] GetByIdAsync - Fallback EstimatedDays from Stage fetch: {result.CurrentStageEstimatedDays} for Onboarding {id}");
                            }
                        }
                        catch { }
                    }

                    // End time already derived strictly from stagesProgress above
                }
                else
                {
                    LoggingExtensions.WriteLine($"[WARNING] GetByIdAsync - CurrentStageId is null for Onboarding {id} after fallback attempt");
                }

                // Get user ID for permission checks
                var userId = _userContext?.UserId;
                long userIdLong = 0;
                bool hasUserId = !string.IsNullOrEmpty(userId) && long.TryParse(userId, out userIdLong);

                // Get actions and permissions for each stage in stagesProgress
                if (result.StagesProgress != null)
                {
                    foreach (var stageProgress in result.StagesProgress)
                    {
                        // Get actions for this stage
                        try
                        {
                            var actions = await _actionManagementService.GetActionTriggerMappingsByTriggerSourceIdAsync(stageProgress.StageId);
                            stageProgress.Actions = actions;
                        }
                        catch (Exception ex)
                        {
                            // Log error but don't fail the entire request
                            // Debug logging handled by structured logging
                            stageProgress.Actions = new List<ActionTriggerMappingWithActionInfo>();
                        }

                        // Get permission for this stage (STRICT MODE: Workflow ∩ Stage)
                        if (hasUserId)
                        {
                            try
                            {
                                stageProgress.Permission = await _permissionService.GetStagePermissionInfoAsync(userIdLong, stageProgress.StageId);
                            }
                            catch (Exception ex)
                            {
                                // Log error but don't fail the entire request
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

                // Check Case permission and fill Permission field (optimized single call)
                if (hasUserId)
                {
                    result.Permission = await _permissionService.GetCasePermissionInfoAsync(userIdLong, id);
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

                return result;
            }
            catch (Exception ex)
            {
                throw new CRMException(ErrorCodeEnum.SystemError, $"Error getting onboarding by ID {id}: {ex.Message}");
            }
        }

        /// <summary>
        /// Query onboarding with pagination
        /// </summary>
        public async Task<PageModelDto<OnboardingOutputDto>> QueryAsync(OnboardingQueryRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var tenantId = _userContext?.TenantId ?? "default";
            var appCode = _userContext?.AppCode ?? "default";
            try
            {
                // Debug logging handled by structured logging
                // Build query conditions list - using safe BaseRepository approach
                var whereExpressions = new List<Expression<Func<Onboarding, bool>>>();

                // Basic filter conditions
                whereExpressions.Add(x => x.IsValid == true);
                // Apply tenant isolation
                if (!string.IsNullOrEmpty(tenantId))
                {
                    whereExpressions.Add(x => x.TenantId.ToLower() == tenantId.ToLower());
                }
                if (!string.IsNullOrEmpty(appCode))
                {
                    whereExpressions.Add(x => x.AppCode.ToLower() == appCode.ToLower());
                }
                // Apply filter conditions
                if (request.WorkflowId.HasValue && request.WorkflowId.Value > 0)
                {
                    whereExpressions.Add(x => x.WorkflowId == request.WorkflowId.Value);
                }

                if (request.CurrentStageId.HasValue && request.CurrentStageId.Value > 0)
                {
                    whereExpressions.Add(x => x.CurrentStageId == request.CurrentStageId.Value);
                }

                // Support comma-separated Lead IDs with fuzzy matching
                if (!string.IsNullOrEmpty(request.LeadId) && request.LeadId != "string")
                {
                    var leadIds = request.GetLeadIdsList();
                    if (leadIds.Any())
                    {
                        // Use OR condition to match any of the lead IDs with fuzzy matching (case-insensitive)
                        whereExpressions.Add(x => leadIds.Any(id => x.LeadId.ToLower().Contains(id.ToLower())));
                    }
                }

                // Support batch LeadIds query for performance optimization
                if (request.LeadIds?.Any() == true)
                {
                    whereExpressions.Add(x => request.LeadIds.Contains(x.LeadId));
                }

                // Support batch OnboardingIds query for exporting selected records
                if (request.OnboardingIds?.Any() == true)
                {
                    whereExpressions.Add(x => request.OnboardingIds.Contains(x.Id));
                }

                // Support comma-separated Lead Names
                if (!string.IsNullOrEmpty(request.LeadName) && request.LeadName != "string")
                {
                    var leadNames = request.GetLeadNamesList();
                    if (leadNames.Any())
                    {
                        // Use OR condition to match any of the lead names (case-insensitive)
                        whereExpressions.Add(x => leadNames.Any(name => x.LeadName.ToLower().Contains(name.ToLower())));
                    }
                }

                // Support comma-separated Case Codes with fuzzy matching
                if (!string.IsNullOrEmpty(request.CaseCode) && request.CaseCode != "string")
                {
                    var caseCodes = request.GetCaseCodesList();
                    if (caseCodes.Any())
                    {
                        // Use OR condition to match any of the case codes (case-insensitive)
                        whereExpressions.Add(x => x.CaseCode != null && caseCodes.Any(code => x.CaseCode.ToLower().Contains(code.ToLower())));
                    }
                }

                if (request.LifeCycleStageId.HasValue && request.LifeCycleStageId.Value > 0)
                {
                    whereExpressions.Add(x => x.LifeCycleStageId == request.LifeCycleStageId.Value);
                }

                if (!string.IsNullOrEmpty(request.LifeCycleStageName) && request.LifeCycleStageName != "string")
                {
                    whereExpressions.Add(x => x.LifeCycleStageName.ToLower().Contains(request.LifeCycleStageName.ToLower()));
                }

                if (!string.IsNullOrEmpty(request.Priority) && request.Priority != "string")
                {
                    whereExpressions.Add(x => x.Priority == request.Priority);
                }

                if (!string.IsNullOrEmpty(request.Status) && request.Status != "string")
                {
                    whereExpressions.Add(x => x.Status == request.Status);
                }

                if (request.IsActive.HasValue)
                {
                    whereExpressions.Add(x => x.IsActive == request.IsActive.Value);
                }

                // Support comma-separated Updated By users
                if (!string.IsNullOrEmpty(request.UpdatedBy) && request.UpdatedBy != "string")
                {
                    var updatedByUsers = request.GetUpdatedByList();
                    if (updatedByUsers.Any())
                    {
                        // Match by StageUpdatedBy first; fallback to ModifyBy (case-insensitive)
                        whereExpressions.Add(x => updatedByUsers.Any(user =>
                            (!string.IsNullOrEmpty(x.StageUpdatedBy) && x.StageUpdatedBy.ToLower().Contains(user.ToLower()))
                            || x.ModifyBy.ToLower().Contains(user.ToLower())));
                    }
                }

                if (request.UpdatedByUserId.HasValue && request.UpdatedByUserId.Value > 0)
                {
                    whereExpressions.Add(x => x.ModifyUserId == request.UpdatedByUserId.Value);
                }

                if (!string.IsNullOrEmpty(request.CreatedBy) && request.CreatedBy != "string")
                {
                    whereExpressions.Add(x => x.CreateBy.ToLower().Contains(request.CreatedBy.ToLower()));
                }

                if (request.CreatedByUserId.HasValue && request.CreatedByUserId.Value > 0)
                {
                    whereExpressions.Add(x => x.CreateUserId == request.CreatedByUserId.Value);
                }

                // Filter by Ownership
                if (request.Ownership.HasValue && request.Ownership.Value > 0)
                {
                    whereExpressions.Add(x => x.Ownership == request.Ownership.Value);
                }

                if (!string.IsNullOrEmpty(request.OwnershipName) && request.OwnershipName != "string")
                {
                    whereExpressions.Add(x => x.OwnershipName.ToLower().Contains(request.OwnershipName.ToLower()));
                }

                // Determine sort field and direction
                Expression<Func<Onboarding, object>> orderByExpression = GetOrderByExpression(request);
                bool isAsc = GetSortDirection(request);

                // Step 1: Get all data matching query criteria (for accurate filtering and pagination)
                int pageIndex = Math.Max(1, request.PageIndex > 0 ? request.PageIndex : 1);
                int pageSize = Math.Max(1, Math.Min(100, request.PageSize > 0 ? request.PageSize : 10));

                List<Onboarding> allEntities;

                if (request.AllData)
                {
                    // Get all data without pagination
                    var queryable = _onboardingRepository.GetSqlSugarClient().Queryable<Onboarding>();

                    // Apply all where conditions
                    foreach (var whereExpression in whereExpressions)
                    {
                        queryable = queryable.Where(whereExpression);
                    }

                    // Apply sorting
                    if (isAsc)
                    {
                        queryable = queryable.OrderBy(orderByExpression);
                    }
                    else
                    {
                        queryable = queryable.OrderByDescending(orderByExpression);
                    }

                    allEntities = await queryable.ToListAsync();
                }
                else
                {
                    // For pagination: Get all matching records first (not just one page)
                    // This ensures accurate totalCount after permission filtering
                    var queryable = _onboardingRepository.GetSqlSugarClient().Queryable<Onboarding>();

                    // Apply all where conditions
                    foreach (var whereExpression in whereExpressions)
                    {
                        queryable = queryable.Where(whereExpression);
                    }

                    // Apply sorting
                    if (isAsc)
                    {
                        queryable = queryable.OrderBy(orderByExpression);
                    }
                    else
                    {
                        queryable = queryable.OrderByDescending(orderByExpression);
                    }

                    allEntities = await queryable.ToListAsync();
                }

                // Step 2: Apply permission filtering - filter out cases user cannot view
                List<Onboarding> filteredEntities;
                var userId = _userContext?.UserId;
                if (!string.IsNullOrEmpty(userId) && long.TryParse(userId, out var userIdLong))
                {
                    // Fast path: If user is System Admin, skip permission checks
                    if (_userContext?.IsSystemAdmin == true)
                    {
                        LoggingExtensions.WriteLine($"[Permission Filter] User {userIdLong} is System Admin, skipping permission checks for {allEntities.Count} cases");
                        filteredEntities = allEntities;
                    }
                    // Fast path: If user is Tenant Admin for current tenant, skip permission checks
                    else if (_userContext != null && _userContext.HasAdminPrivileges(_userContext.TenantId))
                    {
                        LoggingExtensions.WriteLine($"[Permission Filter] User {userIdLong} is Tenant Admin for tenant {_userContext.TenantId}, skipping permission checks for {allEntities.Count} cases");
                        filteredEntities = allEntities;
                    }
                    else
                    {
                        // 🚀 PERFORMANCE OPTIMIZATION: Batch load all unique Workflows first
                        // This reduces N+1 queries from (N Cases × 2 Workflow queries) to (1 batch query)
                        var uniqueWorkflowIds = allEntities.Select(e => e.WorkflowId).Distinct().ToList();
                        var workflowEntities = await _workflowRepository.GetListAsync(w => uniqueWorkflowIds.Contains(w.Id));
                        var workflowEntityDict = workflowEntities.ToDictionary(w => w.Id);

                        LoggingExtensions.WriteLine($"[Performance] Batch loaded {workflowEntities.Count} unique workflows for {allEntities.Count} cases");

                        // Get user teams once (avoid repeated calls)
                        var userTeams = _permissionService.GetUserTeamIds();
                        var userTeamLongs = userTeams?.Select(t => long.TryParse(t, out var tid) ? tid : 0).Where(t => t > 0).ToList() ?? new List<long>();

                        // Regular users need permission filtering (now using in-memory workflow data)
                        filteredEntities = new List<Onboarding>();

                        foreach (var entity in allEntities)
                        {
                            // ⚡ In-memory permission check using pre-loaded Workflow
                            if (!workflowEntityDict.TryGetValue(entity.WorkflowId, out var workflow))
                            {
                                LoggingExtensions.WriteLine($"[Permission Debug] Case {entity.Id} - Workflow {entity.WorkflowId} not found in dictionary");
                                continue;
                            }

                            // Check Workflow view permission (in-memory, no DB query)
                            bool hasWorkflowViewPermission = CheckWorkflowViewPermissionInMemory(workflow, userIdLong, userTeamLongs);
                            LoggingExtensions.WriteLine($"[Permission Debug] Case {entity.Id} - Workflow {workflow.Id} permission: {hasWorkflowViewPermission} (ViewMode={workflow.ViewPermissionMode}, ViewTeams={workflow.ViewTeams ?? "NULL"})");
                            if (!hasWorkflowViewPermission)
                            {
                                continue; // No Workflow permission, skip this Case
                            }

                            var viewResult = await _casePermissionService.CheckCasePermissionAsync(
                     entity, userIdLong, PermissionOperationType.View);
                            bool hasCaseViewPermission = viewResult.CanView;
                            // Check Case-level view permission (in-memory)
                            //bool hasCaseViewPermission = CheckCaseViewPermissionInMemory(entity, userIdLong, userTeamLongs);

                            LoggingExtensions.WriteLine($"[Permission Debug] Case {entity.Id} - Case permission: {hasCaseViewPermission} (ViewMode={entity.ViewPermissionMode}, SubjectType={entity.ViewPermissionSubjectType}, ViewTeams={entity.ViewTeams ?? "NULL"}, ViewUsers={entity.ViewUsers ?? "NULL"}, Ownership={entity.Ownership})");
                            if (!hasCaseViewPermission)
                            {
                                continue; // No Case permission, skip
                            }

                            // ✅ Both Workflow and Case permissions passed
                            LoggingExtensions.WriteLine($"[Permission Debug] Case {entity.Id} - GRANTED (both checks passed)");
                            filteredEntities.Add(entity);
                        }

                        LoggingExtensions.WriteLine($"[Permission Filter] Original count: {allEntities.Count}, Filtered count: {filteredEntities.Count}");
                    }
                }
                else
                {
                    // No user context, return empty result
                    filteredEntities = new List<Onboarding>();
                }

                // Step 3: Apply pagination to filtered results
                var totalCount = filteredEntities.Count;
                var pagedEntities = request.AllData
                    ? filteredEntities
                    : filteredEntities.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();

                // Batch get Workflow and Stage information to avoid N+1 queries (only for paged data)
                var (workflows, stages) = await GetRelatedDataBatchOptimizedAsync(pagedEntities);

                // Create lookup dictionaries to improve search performance
                var workflowDict = workflows.ToDictionary(w => w.Id, w => w.Name);
                var stageDict = stages.ToDictionary(s => s.Id, s => s.Name);

                // Batch process JSON deserialization (only for paged data)
                ProcessStagesProgressParallel(pagedEntities);

                // Map to output DTOs
                var results = _mapper.Map<List<OnboardingOutputDto>>(pagedEntities);

                // 添加调试日志 - 检查状态映射
                LoggingExtensions.WriteLine($"[DEBUG] Query Results Count: {results.Count}");
                LoggingExtensions.WriteLine($"[DEBUG] Original Entities Count: {pagedEntities.Count}");

                for (int i = 0; i < Math.Min(3, pagedEntities.Count); i++)
                {
                    var entity = pagedEntities[i];
                    var result = results[i];
                    LoggingExtensions.WriteLine($"[DEBUG] Entity[{i}]: ID={entity.Id}, LeadName={entity.LeadName}, Status={entity.Status}");
                    LoggingExtensions.WriteLine($"[DEBUG] Result[{i}]: ID={result.Id}, LeadName={result.LeadName}, Status={result.Status}");
                }

                // Populate workflow/stage names and calculate current stage end time
                await PopulateOnboardingOutputDtoAsync(results, pagedEntities);

                // Create page model with appropriate pagination info
                var pageModel = request.AllData
                    ? new PageModelDto<OnboardingOutputDto>(1, totalCount, results, totalCount)
                    : new PageModelDto<OnboardingOutputDto>(pageIndex, pageSize, results, totalCount);

                // Record performance statistics
                stopwatch.Stop();
                // Debug logging handled by structured logging
                return pageModel;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Debug logging handled by structured logging
                throw new CRMException(ErrorCodeEnum.SystemError,
                    $"Error querying onboardings: {ex.Message}");
            }
        }

        /// <summary>
        /// Get sort expression
        /// </summary>
        private Expression<Func<Onboarding, object>> GetOrderByExpression(OnboardingQueryRequest request)
        {
            var sortBy = request.SortField?.ToLower() ?? "createdate";

            return sortBy switch
            {
                "id" => x => x.Id,
                "leadid" => x => x.LeadId,
                "leadname" => x => x.LeadName,
                "casecode" => x => x.CaseCode ?? "",
                "contactperson" => x => x.ContactPerson ?? "",
                "contactemail" => x => x.ContactEmail ?? "",
                "leademail" => x => x.LeadEmail ?? "",
                "leadphone" => x => x.LeadPhone ?? "",
                "workflowid" => x => x.WorkflowId,
                "currentstageid" => x => x.CurrentStageId,
                "lifecyclestageid" => x => x.LifeCycleStageId,
                "lifecyclestagename" => x => x.LifeCycleStageName ?? "",
                "priority" => x => x.Priority ?? "",
                "status" => x => x.Status ?? "",
                "isactive" => x => x.IsActive,
                "completionrate" => x => x.CompletionRate,
                "ownership" => x => x.Ownership,
                "ownershipname" => x => x.OwnershipName ?? "",
                "createdate" => x => x.CreateDate,
                "modifydate" => x => x.ModifyDate,
                "createby" => x => x.CreateBy ?? "",
                "modifyby" => x => x.ModifyBy ?? "",
                _ => x => x.CreateDate
            };
        }

        /// <summary>
        /// Get sort direction
        /// </summary>
        private bool GetSortDirection(OnboardingQueryRequest request)
        {
            var sortDirection = request.SortDirection?.ToLower() ?? "desc";
            return sortDirection == "asc";
        }

        /// <summary>
        /// Batch get related Workflow and Stage data (avoid N+1 queries) - Sequential execution to avoid concurrency issues
        /// </summary>
        private async Task<(List<Workflow> workflows, List<Stage> stages)> GetRelatedDataBatchOptimizedAsync(List<Onboarding> entities)
        {
            var workflowIds = entities.Select(x => x.WorkflowId).Distinct().ToList();
            var stageIds = entities.Where(x => x.CurrentStageId.HasValue)
                    .Select(x => x.CurrentStageId.Value).Distinct().ToList();

            // Sequential execution to avoid SQL concurrency conflicts
            var workflows = await GetWorkflowsBatchAsync(workflowIds);
            var stages = await GetStagesBatchAsync(stageIds);

            return (workflows, stages);
        }

        /// <summary>
        /// Parallel processing of stage progress
        /// </summary>
        private void ProcessStagesProgressParallel(List<Onboarding> entities)
        {
            try
            {
                if (entities.Count <= 10)
                {
                    // Direct processing for small datasets
                    foreach (var entity in entities)
                    {
                        LoadStagesProgressFromJson(entity);
                    }
                }
                else
                {
                    // Parallel processing for large datasets, but using safer approach
                    // Create copy of entities list to avoid collection modification exceptions
                    var entitiesCopy = entities.ToList();
                    Parallel.ForEach(entitiesCopy, new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 4) // Limit parallelism degree
                    }, entity =>
                    {
                        try
                        {
                            LoadStagesProgressFromJson(entity);
                        }
                        catch (Exception ex)
                        {
                            // Debug logging handled by structured logging
                            // Ensure that even if single entity processing fails, it doesn't affect other entities
                            entity.StagesProgress = new List<OnboardingStageProgress>();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                // If parallel processing fails, fallback to sequential processing
                foreach (var entity in entities)
                {
                    try
                    {
                        LoadStagesProgressFromJson(entity);
                    }
                    catch (Exception entityEx)
                    {
                        // Debug logging handled by structured logging
                        entity.StagesProgress = new List<OnboardingStageProgress>();
                    }
                }
            }
        }

        /// <summary>
        /// Batch retrieve Workflows information, avoid N+1 queries, support caching
        /// </summary>
        private async Task<List<Workflow>> GetWorkflowsBatchAsync(List<long> workflowIds)
        {
            if (!workflowIds.Any())
                return new List<Workflow>();

            try
            {
                var workflows = new List<Workflow>();
                var uncachedIds = new List<long>();

                // Safely get tenant ID
                var tenantId = !string.IsNullOrEmpty(_userContext?.TenantId)
                    ? _userContext.TenantId.ToLowerInvariant()
                    : "default";

                // First get from cache
                foreach (var id in workflowIds)
                {
                    try
                    {
                        var cacheKey = $"{WORKFLOW_CACHE_PREFIX}:{tenantId}:{id}";
                        // Redis cache temporarily disabled
                        string cachedWorkflow = null;

                        if (!string.IsNullOrEmpty(cachedWorkflow))
                        {
                            var workflow = JsonSerializer.Deserialize<Workflow>(cachedWorkflow);
                            if (workflow != null)
                            {
                                workflows.Add(workflow);
                            }
                        }
                        else
                        {
                            uncachedIds.Add(id);
                        }
                    }
                    catch (Exception cacheEx)
                    {
                        // Debug logging handled by structured logging
                        uncachedIds.Add(id);
                    }
                }

                // Get uncached data from database - use direct query to avoid repository conflicts
                if (uncachedIds.Any())
                {
                    List<Workflow> dbWorkflows;
                    try
                    {
                        // Use direct repository method to avoid connection conflicts
                        dbWorkflows = await _workflowRepository.GetListAsync(w => uncachedIds.Contains(w.Id) && w.IsValid);
                        workflows.AddRange(dbWorkflows);
                    }
                    catch (Exception dbEx)
                    {
                        // Debug logging handled by structured logging
                        // Fallback to repository method if direct query fails
                        dbWorkflows = await _workflowRepository.GetListAsync(w => uncachedIds.Contains(w.Id) && w.IsValid);
                        workflows.AddRange(dbWorkflows);
                    }
                    // Cache newly retrieved data
                    var cacheExpiry = TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES);
                    var cacheTasks = dbWorkflows.Select(async workflow =>
                    {
                        try
                        {
                            var cacheKey = $"{WORKFLOW_CACHE_PREFIX}:{tenantId}:{workflow.Id}";
                            var serializedWorkflow = JsonSerializer.Serialize(workflow);
                            // Redis cache temporarily disabled
                            await Task.CompletedTask;
                        }
                        catch (Exception cacheEx)
                        {
                            // Debug logging handled by structured logging
                        }
                    });

                    await Task.WhenAll(cacheTasks);
                }

                return workflows;
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                // If cache fails, get directly from database
                return await _workflowRepository.GetListAsync(w => workflowIds.Contains(w.Id) && w.IsValid);
            }
        }

        /// <summary>
        /// Batch get Stages information, avoid N+1 queries, support caching
        /// </summary>
        private async Task<List<Stage>> GetStagesBatchAsync(List<long> stageIds)
        {
            if (!stageIds.Any())
                return new List<Stage>();

            try
            {
                var stages = new List<Stage>();
                var uncachedIds = new List<long>();

                // Safely get tenant ID
                var tenantId = !string.IsNullOrEmpty(_userContext?.TenantId)
                    ? _userContext.TenantId.ToLowerInvariant()
                    : "default";

                // First get from cache
                foreach (var id in stageIds)
                {
                    try
                    {
                        var cacheKey = $"{STAGE_CACHE_PREFIX}:{tenantId}:{id}";
                        // Redis cache temporarily disabled
                        string cachedStage = null;

                        if (!string.IsNullOrEmpty(cachedStage))
                        {
                            var stage = JsonSerializer.Deserialize<Stage>(cachedStage);
                            if (stage != null)
                            {
                                stages.Add(stage);
                            }
                        }
                        else
                        {
                            uncachedIds.Add(id);
                        }
                    }
                    catch (Exception cacheEx)
                    {
                        // Debug logging handled by structured logging
                        uncachedIds.Add(id);
                    }
                }

                // Get uncached data from database - use direct query to avoid repository conflicts
                if (uncachedIds.Any())
                {
                    List<Stage> dbStages;
                    try
                    {
                        // Use direct repository method to avoid connection conflicts
                        dbStages = await _stageRepository.GetListAsync(s => uncachedIds.Contains(s.Id) && s.IsValid);
                        stages.AddRange(dbStages);
                    }
                    catch (Exception dbEx)
                    {
                        // Debug logging handled by structured logging
                        // Fallback to repository method if direct query fails
                        dbStages = await _stageRepository.GetListAsync(s => uncachedIds.Contains(s.Id) && s.IsValid);
                        stages.AddRange(dbStages);
                    }
                    // Cache newly retrieved data
                    var cacheExpiry = TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES);
                    var cacheTasks = dbStages.Select(async stage =>
                    {
                        try
                        {
                            var cacheKey = $"{STAGE_CACHE_PREFIX}:{tenantId}:{stage.Id}";
                            var serializedStage = JsonSerializer.Serialize(stage);
                            // Redis cache temporarily disabled
                            await Task.CompletedTask;
                        }
                        catch (Exception cacheEx)
                        {
                            // Debug logging handled by structured logging
                        }
                    });

                    await Task.WhenAll(cacheTasks);
                }

                return stages;
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                // If cache fails, get directly from database
                return await _stageRepository.GetListAsync(s => stageIds.Contains(s.Id) && s.IsValid);
            }
        }

        /// <summary>
        /// Move to next stage
        /// </summary>
        public async Task<bool> MoveToNextStageAsync(long id)
        {
            // Check permission
            if (!await CheckCaseOperatePermissionAsync(id))
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed,
                    $"User does not have permission to operate on case {id}");
            }

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            var orderedStages = stages.OrderBy(x => x.Order).ToList();
            var currentStageIndex = orderedStages.FindIndex(x => x.Id == entity.CurrentStageId);

            if (currentStageIndex == -1 || currentStageIndex >= orderedStages.Count - 1)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "No next stage available");
            }

            var nextStage = orderedStages[currentStageIndex + 1];
            await _onboardingRepository.UpdateStageAsync(id, nextStage.Id, nextStage.Order);

            // Update status to InProgress if it was Started
            if (entity.Status == "Started")
            {
                await _onboardingRepository.UpdateStatusAsync(id, "InProgress");
            }

            return true;
        }

        /// <summary>
        /// Move to specific stage
        /// </summary>
        public async Task<bool> MoveToStageAsync(long id, MoveToStageInputDto input)
        {
            // Check permission
            if (!await CheckCaseOperatePermissionAsync(id))
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed,
                    $"User does not have permission to operate on case {id}");
            }

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            var stage = await _stageRepository.GetByIdAsync(input.StageId);
            if (stage == null || !stage.IsValid || stage.WorkflowId != entity.WorkflowId)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Stage not found or not belongs to current workflow");
            }

            return await _onboardingRepository.UpdateStageAsync(id, stage.Id, stage.Order);
        }

        public async Task<bool> CompleteCurrentStageInternalAsync(long id, CompleteCurrentStageInputDto input)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status == "Completed")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Onboarding is already completed");
            }

            // Ensure stages progress is initialized
            await EnsureStagesProgressInitializedAsync(entity);

            // Get all stages for this workflow
            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            var orderedStages = stages.OrderBy(x => x.Order).ToList();

            // Get target stage ID with backward compatibility
            long stageIdToComplete;
            try
            {
                stageIdToComplete = input.GetTargetStageId();
            }
            catch (ArgumentException ex)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, $"Invalid stage ID parameters: {ex.Message}");
            }

            // Find the stage to complete
            var stageToComplete = orderedStages.FirstOrDefault(s => s.Id == stageIdToComplete);
            if (stageToComplete == null)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, $"Stage with ID {stageIdToComplete} not found in workflow");
            }

            // Validate if this stage can be completed
            (bool canComplete, string validationError) = await ValidateStageCanBeCompletedAsync(entity, stageIdToComplete);
            if (!canComplete && !input.ForceComplete)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, validationError);
            }

            // Update stages progress
            await UpdateStagesProgressAsync(entity, stageIdToComplete, GetCurrentUserName(), GetCurrentUserId(), input.CompletionNotes);

            // Check if all stages are completed
            var allStagesCompleted = entity.StagesProgress.All(sp => sp.IsCompleted);

            if (allStagesCompleted)
            {
                // Complete the entire onboarding
                entity.Status = "Completed";
                entity.CompletionRate = 100;
                entity.ActualCompletionDate = DateTimeOffset.UtcNow;
            }
            else
            {
                // Update completion rate
                entity.CompletionRate = CalculateCompletionRateByCompletedStages(entity.StagesProgress);

                // Update current stage if needed
                if (entity.CurrentStageId == stageToComplete.Id)
                {
                    // If we completed the current stage, advance to the next incomplete stage
                    var nextIncompleteStage = orderedStages
                        .Where(stage => stage.Order > stageToComplete.Order &&
                                       !entity.StagesProgress.Any(sp => sp.StageId == stage.Id && sp.IsCompleted))
                        .OrderBy(stage => stage.Order)
                        .FirstOrDefault();

                    if (nextIncompleteStage != null)
                    {
                        entity.CurrentStageId = nextIncompleteStage.Id;
                        entity.CurrentStageOrder = nextIncompleteStage.Order;
                        entity.CurrentStageStartTime = DateTimeOffset.UtcNow;
                    }
                }
                else if (entity.CurrentStageId != stageToComplete.Id && !input.PreventAutoMove)
                {
                    // If we completed a different stage (not the current one), 
                    // advance to the next incomplete stage AFTER the completed stage (only look forward)
                    // 但如果 PreventAutoMove 为 true，则不自动移动（用于系统动作的精确控制）
                    var completedStageOrder = orderedStages.FirstOrDefault(s => s.Id == stageToComplete.Id)?.Order ?? 0;
                    var nextIncompleteStage = orderedStages
                        .Where(stage => stage.Order > completedStageOrder &&
                                       !entity.StagesProgress.Any(sp => sp.StageId == stage.Id && sp.IsCompleted))
                        .OrderBy(stage => stage.Order)
                        .FirstOrDefault();

                    if (nextIncompleteStage != null)
                    {
                        entity.CurrentStageId = nextIncompleteStage.Id;
                        entity.CurrentStageOrder = nextIncompleteStage.Order;
                        entity.CurrentStageStartTime = DateTimeOffset.UtcNow;
                    }
                }

                // Add completion notes if provided
                if (!string.IsNullOrEmpty(input.CompletionNotes))
                {
                    var noteText = $"[Stage Completed] {stageToComplete.Name}: {input.CompletionNotes}";
                    SafeAppendToNotes(entity, noteText);
                }
            }

            // Update stage tracking info
            await UpdateStageTrackingInfoAsync(entity);

            // Update the entity using safe method - NO EVENT PUBLISHING
            var result = await SafeUpdateOnboardingAsync(entity);

            return result;
        }
        /// <summary>
        /// Complete specified stage with validation (supports non-sequential completion)
        /// </summary>
        public async Task<bool> CompleteCurrentStageAsync(long id, CompleteCurrentStageInputDto input)
        {
            // Check permission
            if (!await CheckCaseOperatePermissionAsync(id))
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed,
                    $"User does not have permission to operate on case {id}");
            }

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            // Ensure stages progress is properly initialized and synced
            await EnsureStagesProgressInitializedAsync(entity);

            // Save the initialized stages progress
            await SafeUpdateOnboardingAsync(entity);

            // Debug logging handled by structured logging ===");
            // Debug logging handled by structured logging
            // Get target stage ID with backward compatibility
            long targetStageId;
            try
            {
                targetStageId = input.GetTargetStageId();
            }
            catch (ArgumentException ex)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, $"Invalid stage ID parameters: {ex.Message}");
            }
            // Debug logging handled by structured logging
            // Check if onboarding is already completed
            if (entity.Status == "Completed")
            {
                // Debug logging handled by structured logging
                return true; // Return success since the desired outcome (completion) is already achieved
            }

            // Optional: Check if frontend stage matches backend stage (only if CurrentStageId is provided)
            if (input.CurrentStageId.HasValue && entity.CurrentStageId != input.CurrentStageId)
            {
                // Debug logging handled by structured logging
                // Auto-correct: Update completion rate and sync stage information
                // Debug logging handled by structured logging
                try
                {
                    await UpdateCompletionRateAsync(id);
                    // Debug logging handled by structured logging
                    // Reload the entity to get the latest data after completion rate update
                    entity = await _onboardingRepository.GetByIdAsync(id);
                    // Debug logging handled by structured logging
                }
                catch (Exception ex)
                {
                    // Debug logging handled by structured logging
                }

                // Check if the mismatch still exists after correction
                if (entity.CurrentStageId != input.CurrentStageId)
                {
                    // Debug logging handled by structured logging
                }
                else
                {
                    // Debug logging handled by structured logging
                }
            }
            // Debug logging handled by structured logging
            // Get all stages for this workflow
            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            var orderedStages = stages.OrderBy(x => x.Order).ToList();
            var totalStages = orderedStages.Count;
            // Debug logging handled by structured logging
            // Debug logging handled by structured logging)}");

            // Find the stage to complete
            var stageToComplete = orderedStages.FirstOrDefault(x => x.Id == targetStageId);
            if (stageToComplete == null)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Specified stage not found in workflow");
            }

            // Debug logging handled by structured logging");

            // Validate if this stage can be completed
            var (canComplete, validationError) = await ValidateStageCanBeCompletedAsync(entity, stageToComplete.Id);
            if (!canComplete)
            {
                // Debug logging handled by structured logging
                throw new CRMException(ErrorCodeEnum.BusinessError, validationError);
            }
            // Debug logging handled by structured logging
            // Check stage completion logs to see if this stage has already been completed
            // Debug logging handled by structured logging
            // Stage completion log checking functionality removed

            // Update stages progress for the completed stage (non-sequential completion)
            // Debug logging handled by structured logging
            await UpdateStagesProgressAsync(entity, stageToComplete.Id, GetCurrentUserName(), GetCurrentUserId(), input.CompletionNotes);

            // After updating progress, synchronously generate AI summary so client can fetch it right after completion
            //try
            //{
            //    var lang = string.IsNullOrWhiteSpace(input.Language) ? "auto" : input.Language;
            //    var opts = new StageSummaryOptions { Language = lang, SummaryLength = "short", IncludeTaskAnalysis = true, IncludeQuestionnaireInsights = true };
            //    // Try multiple attempts synchronously before falling back to placeholder
            //    var ai = await _stageService.GenerateAISummaryAsync(stageToComplete.Id, null, opts);
            //    if (ai == null || !ai.Success)
            //    {
            //        try { await Task.Delay(1500); } catch {}
            //        ai = await _stageService.GenerateAISummaryAsync(stageToComplete.Id, null, opts);
            //    }
            //    LoadStagesProgressFromJson(entity);
            //    var sp = entity.StagesProgress?.FirstOrDefault(s => s.StageId == stageToComplete.Id);
            //    if (sp != null)
            //    {
            //        if (ai != null && ai.Success)
            //        {
            //            sp.AiSummary = ai.Summary;
            //            sp.AiSummaryGeneratedAt = DateTime.UtcNow;
            //            sp.AiSummaryConfidence = (decimal?)Convert.ToDecimal(ai.ConfidenceScore);
            //            sp.AiSummaryModel = ai.ModelUsed;
            //            var detailedData = new { ai.Breakdown, ai.KeyInsights, ai.Recommendations, ai.CompletionStatus, generatedAt = DateTime.UtcNow };
            //            sp.AiSummaryData = System.Text.Json.JsonSerializer.Serialize(detailedData);
            //        }
            //        else
            //        {
            //            // Fallback placeholder to ensure frontend sees a value immediately; schedule retry in background
            //            sp.AiSummary = "AI summary is being generated...";
            //            sp.AiSummaryGeneratedAt = DateTime.UtcNow;
            //            sp.AiSummaryConfidence = null;
            //            sp.AiSummaryModel = null;
            //            sp.AiSummaryData = null;
            //            _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
            //            {
            //                try
            //                {
            //                    var retry = await _stageService.GenerateAISummaryAsync(stageToComplete.Id, null, opts);
            //                    if (retry == null || !retry.Success)
            //                    {
            //                        try { await Task.Delay(3000); } catch {}
            //                        retry = await _stageService.GenerateAISummaryAsync(stageToComplete.Id, null, opts);
            //                    }
            //                    if (retry != null && retry.Success)
            //                    {
            //                        LoadStagesProgressFromJson(entity);
            //                        var sp2 = entity.StagesProgress?.FirstOrDefault(s => s.StageId == stageToComplete.Id);
            //                        if (sp2 != null)
            //                        {
            //                            sp2.AiSummary = retry.Summary;
            //                            sp2.AiSummaryGeneratedAt = DateTime.UtcNow;
            //                            sp2.AiSummaryConfidence = (decimal?)Convert.ToDecimal(retry.ConfidenceScore);
            //                            sp2.AiSummaryModel = retry.ModelUsed;
            //                            var dd = new { retry.Breakdown, retry.KeyInsights, retry.Recommendations, retry.CompletionStatus, generatedAt = DateTime.UtcNow };
            //                            sp2.AiSummaryData = System.Text.Json.JsonSerializer.Serialize(dd);
            //                            entity.StagesProgressJson = SerializeStagesProgress(entity.StagesProgress);
            //                            await SafeUpdateOnboardingAsync(entity);
            //                        }
            //                    }
            //                }
            //                catch { }
            //            });
            //        }
            //        entity.StagesProgressJson = SerializeStagesProgress(entity.StagesProgress);
            //        await SafeUpdateOnboardingAsync(entity);
            //    }
            //}
            //catch { /* keep non-blocking if AI fails */ }

            // Calculate new completion rate based on completed stages
            entity.CompletionRate = CalculateCompletionRateByCompletedStages(entity.StagesProgress);
            var completedCount = entity.StagesProgress.Count(s => s.IsCompleted);
            // Debug logging handled by structured logging
            // Check if all stages are completed
            var allStagesCompleted = entity.StagesProgress.All(s => s.IsCompleted);
            if (allStagesCompleted)
            {
                // Complete the entire onboarding
                // Debug logging handled by structured logging
                entity.Status = "Completed";
                entity.CompletionRate = 100;
                entity.ActualCompletionDate = DateTimeOffset.UtcNow;

                // Add completion notes if provided
                if (!string.IsNullOrEmpty(input.CompletionNotes))
                {
                    var noteText = $"[Onboarding Completed] Final stage '{stageToComplete.Name}' completed: {input.CompletionNotes}";
                    SafeAppendToNotes(entity, noteText);
                    // Debug logging handled by structured logging
                }
            }
            else
            {
                // Update status to InProgress if it was Started
                if (entity.Status == "Started")
                {
                    entity.Status = "InProgress";
                    // Debug logging handled by structured logging
                }

                // Auto-advance to next stage logic (similar to CompleteCurrentStageAsync without input)
                // Find the next incomplete stage to advance to
                var currentStageIndex = orderedStages.FindIndex(x => x.Id == entity.CurrentStageId);
                var nextStageIndex = currentStageIndex + 1;

                LoggingExtensions.WriteLine($"[DEBUG] CompleteCurrentStageAsync - Before auto-advance: CurrentStageId={entity.CurrentStageId}, CompletedStageId={stageToComplete.Id}, NextIndex={nextStageIndex}");

                // If current stage is the completed stage and there's a next stage, advance to it
                if (entity.CurrentStageId == stageToComplete.Id && nextStageIndex < orderedStages.Count)
                {
                    var nextStage = orderedStages[nextStageIndex];
                    var oldStageId = entity.CurrentStageId;
                    entity.CurrentStageId = nextStage.Id;
                    entity.CurrentStageOrder = nextStage.Order;
                    entity.CurrentStageStartTime = DateTimeOffset.UtcNow;
                    // Debug logging handled by structured logging
                    LoggingExtensions.WriteLine($"[DEBUG] CompleteCurrentStageAsync - Advanced to next stage: OldStageId={oldStageId}, NewStageId={entity.CurrentStageId}, StageName={nextStage.Name}");
                }
                else if (entity.CurrentStageId != stageToComplete.Id)
                {
                    // If we completed a different stage (not the current one), 
                    // advance to the next incomplete stage AFTER the completed stage (only look forward)
                    var completedStageOrder = orderedStages.FirstOrDefault(s => s.Id == stageToComplete.Id)?.Order ?? 0;
                    var nextIncompleteStage = orderedStages
                        .Where(stage => stage.Order > completedStageOrder &&
                                       !entity.StagesProgress.Any(sp => sp.StageId == stage.Id && sp.IsCompleted))
                        .OrderBy(stage => stage.Order)
                        .FirstOrDefault();

                    if (nextIncompleteStage != null)
                    {
                        entity.CurrentStageId = nextIncompleteStage.Id;
                        entity.CurrentStageOrder = nextIncompleteStage.Order;
                        entity.CurrentStageStartTime = DateTimeOffset.UtcNow;
                        // Debug logging handled by structured logging
                    }
                }

                // Add completion notes if provided
                if (!string.IsNullOrEmpty(input.CompletionNotes))
                {
                    var noteText = $"[Stage Completed] {stageToComplete.Name}: {input.CompletionNotes}";
                    SafeAppendToNotes(entity, noteText);
                    // Debug logging handled by structured logging
                }
            }

            // Update stage tracking info
            await UpdateStageTrackingInfoAsync(entity);

            // Update the entity using safe method
            var result = await SafeUpdateOnboardingAsync(entity);
            // Debug logging handled by structured logging
            // Publish stage completion event
            if (result)
            {
                // Debug logging handled by structured logging
                await PublishStageCompletionEventForCurrentStageAsync(entity, stageToComplete, allStagesCompleted);
            }

            // Debug logging handled by structured logging End ===");
            return result;
        }

        /// <summary>
        /// Pause onboarding
        /// </summary>
        public async Task<bool> PauseAsync(long id)
        {
            // Check permission
            if (!await CheckCaseOperatePermissionAsync(id))
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed,
                    $"User does not have permission to operate on case {id}");
            }

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status == "Completed")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Cannot pause completed onboarding");
            }

            return await _onboardingRepository.UpdateStatusAsync(id, "Paused");
        }

        /// <summary>
        /// Resume onboarding
        /// </summary>
        public async Task<bool> ResumeAsync(long id)
        {
            // Check permission
            if (!await CheckCaseOperatePermissionAsync(id))
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed,
                    $"User does not have permission to operate on case {id}");
            }

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status != "Paused")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Only paused onboarding can be resumed");
            }

            return await _onboardingRepository.UpdateStatusAsync(id, "InProgress");
        }

        /// <summary>
        /// Cancel onboarding
        /// </summary>
        public async Task<bool> CancelAsync(long id, string reason)
        {
            // Check permission
            if (!await CheckCaseOperatePermissionAsync(id))
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed,
                    $"User does not have permission to operate on case {id}");
            }

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status == "Completed")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Cannot cancel completed onboarding");
            }

            // Update notes with cancellation reason
            if (!string.IsNullOrEmpty(reason))
            {
                var cancellationNote = $"Cancelled: {reason}";
                // For cancellation, we want to prepend the note, so we'll handle this specially
                var currentNotes = entity.Notes ?? string.Empty;
                var newContent = string.IsNullOrEmpty(currentNotes)
                    ? cancellationNote
                    : $"{cancellationNote}. {currentNotes}";

                // Ensure we don't exceed the length limit
                if (newContent.Length > 1000)
                {
                    newContent = newContent.Substring(0, 1000);
                }
                entity.Notes = newContent;
                await SafeUpdateOnboardingAsync(entity);
            }

            // Log cancellation to Change Log
            await LogOnboardingActionAsync(entity, "Cancel Onboarding", "onboarding_cancel", true, new
            {
                CancellationReason = reason,
                CancelledAt = DateTimeOffset.UtcNow,
                CancelledBy = _operatorContextService.GetOperatorDisplayName()
            });

            return await _onboardingRepository.UpdateStatusAsync(id, "Cancelled");
        }

        /// <summary>
        /// Reject onboarding application
        /// </summary>
        public async Task<bool> RejectAsync(long id, RejectOnboardingInputDto input)
        {
            // Check permission
            if (!await CheckCaseOperatePermissionAsync(id))
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed,
                    $"User does not have permission to operate on case {id}");
            }

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status == "Completed")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Cannot reject completed onboarding");
            }

            if (entity.Status == "Rejected" || entity.Status == "Terminated")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Onboarding is already rejected or terminated");
            }

            var currentTime = DateTimeOffset.UtcNow;

            // Update onboarding status
            entity.Status = input.TerminateWorkflow ? "Terminated" : "Rejected";
            entity.ModifyDate = currentTime;

            // Update notes with rejection reason
            var rejectionNote = $"[{(input.TerminateWorkflow ? "TERMINATED" : "REJECTED")}] {input.RejectionReason}";
            if (!string.IsNullOrEmpty(input.AdditionalNotes))
            {
                rejectionNote += $" - Additional Notes: {input.AdditionalNotes}";
            }
            rejectionNote += $" - {(input.TerminateWorkflow ? "Terminated" : "Rejected")} by: {input.RejectedBy} at {currentTime:yyyy-MM-dd HH:mm:ss}";

            SafeAppendToNotes(entity, rejectionNote);

            // Update stages progress to reflect rejection/termination
            LoadStagesProgressFromJson(entity);
            if (entity.StagesProgress != null && entity.StagesProgress.Any())
            {
                foreach (var stage in entity.StagesProgress)
                {
                    if (stage.Status == "InProgress" || stage.Status == "Pending")
                    {
                        stage.Status = input.TerminateWorkflow ? "Terminated" : "Rejected";
                        stage.RejectionReason = input.RejectionReason;
                        stage.RejectionTime = currentTime;
                        stage.RejectedBy = input.RejectedBy;
                        stage.LastUpdatedTime = currentTime;
                        stage.LastUpdatedBy = input.RejectedBy;

                        if (input.TerminateWorkflow)
                        {
                            stage.TerminationTime = currentTime;
                            stage.TerminatedBy = input.RejectedBy;
                        }
                    }
                }

                // Serialize updated stages progress back to JSON (only progress fields)
                entity.StagesProgressJson = SerializeStagesProgress(entity.StagesProgress);
            }

            // Update stage tracking info
            await UpdateStageTrackingInfoAsync(entity);

            // Save changes
            var result = await SafeUpdateOnboardingAsync(entity);

            if (result)
            {
                // Log rejection to Change Log
                await LogOnboardingActionAsync(entity,
                    input.TerminateWorkflow ? "Terminate Onboarding" : "Reject Application",
                    input.TerminateWorkflow ? "onboarding_terminate" : "application_reject",
                    true,
                    new
                    {
                        RejectionReason = input.RejectionReason,
                        TerminateWorkflow = input.TerminateWorkflow,
                        AdditionalNotes = input.AdditionalNotes,
                        RejectedBy = input.RejectedBy,
                        RejectedById = input.RejectedById,
                        RejectedAt = currentTime,
                        SendNotification = input.SendNotification,
                        PreviousStatus = "InProgress", // Assuming it was in progress
                        NewStatus = entity.Status
                    });

                // Notification sending - future enhancement
                if (input.SendNotification)
                {
                    // Implement notification logic here
                    // Debug logging handled by structured logging} onboarding {id}");
                }
            }

            return result;
        }

        /// <summary>
        /// Assign onboarding to user
        /// </summary>
        public async Task<bool> AssignAsync(long id, AssignOnboardingInputDto input)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            entity.CurrentAssigneeId = input.AssigneeId;
            entity.CurrentAssigneeName = input.AssigneeName;
            entity.CurrentTeam = input.Team;

            return await SafeUpdateOnboardingAsync(entity);
        }

        /// <summary>
        /// Update completion rate based on stage progress
        /// </summary>
        public async Task<bool> UpdateCompletionRateAsync(long id)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            var totalStages = stages.Count;

            // Calculate completion rate based on stage progress
            // CurrentStageOrder represents the current stage (1-based), so completed stages = CurrentStageOrder - 1
            var completedStages = Math.Max(0, entity.CurrentStageOrder - 1);
            var stageBasedCompletionRate = totalStages > 0 ? (decimal)completedStages / totalStages * 100 : 0;

            // Log calculation details for debugging
            // Debug logging handled by structured logging
            // Always update to the stage-based completion rate
            // Debug logging handled by structured logging
            return await _onboardingRepository.UpdateCompletionRateAsync(id, stageBasedCompletionRate);
        }

        /// <summary>
        /// Update stage tracking information
        /// </summary>
        private async Task UpdateStageTrackingInfoAsync(Onboarding entity)
        {
            // Use OperatorContextService for consistent user information
            entity.StageUpdatedTime = DateTimeOffset.UtcNow;
            entity.StageUpdatedBy = _operatorContextService.GetOperatorDisplayName();
            entity.StageUpdatedById = _operatorContextService.GetOperatorId();
            entity.StageUpdatedByEmail = GetCurrentUserEmail();

            // Sync isCurrent flag in stagesProgress to match currentStageId
            // Note: Don't reload from JSON here as UpdateStagesProgressAsync may have just updated the data
            if (entity.StagesProgress != null && entity.StagesProgress.Any())
            {
                foreach (var stage in entity.StagesProgress)
                {
                    // Update isCurrent flag based on currentStageId
                    stage.IsCurrent = stage.StageId == entity.CurrentStageId;

                    // Update stage status based on completion and current status, but preserve IsCompleted status
                    if (stage.IsCompleted)
                    {
                        stage.Status = "Completed";
                    }
                    else if (stage.IsCurrent)
                    {
                        stage.Status = "InProgress";
                    }
                    else
                    {
                        stage.Status = "Pending";
                    }
                }

                // Serialize back to JSON (only progress fields)
                entity.StagesProgressJson = SerializeStagesProgress(entity.StagesProgress);
            }
        }

        /// <summary>
        /// Get onboarding progress
        /// </summary>
        public async Task<OnboardingProgressDto> GetProgressAsync(long id)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            // Ensure stages progress is properly initialized and synced
            await EnsureStagesProgressInitializedAsync(entity);

            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            var totalStages = stages.Count;
            var completedStages = entity.CurrentStageOrder;

            // Calculate estimated completion time based on average stage duration
            var avgStageDuration = TimeSpan.FromDays(7); // Default 7 days per stage
            var remainingStages = totalStages - completedStages;
            var estimatedCompletion = entity.CreateDate.AddDays(totalStages * 7);

            // Check if overdue
            var isOverdue = entity.Status != "Completed" &&
                           entity.EstimatedCompletionDate.HasValue &&
                           DateTimeOffset.UtcNow > entity.EstimatedCompletionDate.Value;

            // Map stages progress to DTO
            var stagesProgressDto = _mapper.Map<List<OnboardingStageProgressDto>>(entity.StagesProgress);

            // Get actions for each stage
            foreach (var stageProgress in stagesProgressDto)
            {
                try
                {
                    var actions = await _actionManagementService.GetActionTriggerMappingsByTriggerSourceIdAsync(stageProgress.StageId);
                    stageProgress.Actions = actions;
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the entire request
                    // Debug logging handled by structured logging
                    stageProgress.Actions = new List<ActionTriggerMappingWithActionInfo>();
                }
            }

            return new OnboardingProgressDto
            {
                OnboardingId = entity.Id,
                CurrentStageId = entity.CurrentStageId,
                CurrentStageName = stages.FirstOrDefault(s => s.Id == entity.CurrentStageId)?.Name,
                TotalStages = totalStages,
                CompletedStages = completedStages,
                CompletionPercentage = entity.CompletionRate,
                StartTime = entity.CreateDate,
                EstimatedCompletionTime = entity.EstimatedCompletionDate ?? estimatedCompletion,
                ActualCompletionTime = entity.ActualCompletionDate,
                IsOverdue = isOverdue,
                Status = entity.Status,
                Priority = entity.Priority,
                StagesProgress = stagesProgressDto
            };
        }

        /// <summary>
        /// Log general onboarding action to change log
        /// </summary>
        private async Task LogOnboardingActionAsync(Onboarding onboarding, string action, string logType, bool success, object additionalData = null)
        {
            try
            {
                var logData = new
                {
                    OnboardingId = onboarding.Id,
                    LeadId = onboarding.LeadId,
                    LeadName = onboarding.LeadName,
                    WorkflowId = onboarding.WorkflowId,
                    CurrentStageId = onboarding.CurrentStageId,
                    Status = onboarding.Status,
                    Priority = onboarding.Priority,
                    ActionTime = DateTimeOffset.UtcNow,
                    ActionBy = _operatorContextService.GetOperatorDisplayName(),
                    AdditionalData = additionalData
                };

                // Stage completion log functionality removed
                // Debug logging handled by structured logging
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
            }
        }

        /// <summary>
        /// Publish stage completion event for current stage completion (without CompleteStageInputDto)
        /// </summary>
        private async Task PublishStageCompletionEventForCurrentStageAsync(Onboarding onboarding, Stage completedStage, bool isFinalStage)
        {
            try
            {
                // Get next stage info if not final stage
                string nextStageName = null;
                long? nextStageId = null;
                if (!isFinalStage && onboarding.CurrentStageId.HasValue)
                {
                    var nextStage = await _stageRepository.GetByIdAsync(onboarding.CurrentStageId.Value);
                    nextStageName = nextStage?.Name;
                    nextStageId = nextStage?.Id;
                }

                // Build components payload
                var componentsPayload2 = await BuildStageCompletionComponentsAsync(onboarding.Id, completedStage.Id, completedStage.Components, completedStage.ComponentsJson);

                // Publish the OnboardingStageCompletedEvent for enhanced event handling
                var onboardingStageCompletedEvent = new OnboardingStageCompletedEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    Timestamp = DateTimeOffset.UtcNow,
                    Version = "1.0",
                    TenantId = onboarding.TenantId,
                    OnboardingId = onboarding.Id,
                    LeadId = onboarding.LeadId,
                    WorkflowId = onboarding.WorkflowId,
                    WorkflowName = (await _workflowRepository.GetByIdAsync(onboarding.WorkflowId))?.Name ?? "Unknown",
                    CompletedStageId = completedStage.Id,
                    CompletedStageName = completedStage.Name,
                    StageCategory = completedStage.Name ?? "Unknown",
                    NextStageId = nextStageId,
                    NextStageName = nextStageName,
                    CompletionRate = onboarding.CompletionRate,
                    IsFinalStage = isFinalStage,
                    AssigneeName = onboarding.CurrentAssigneeName ?? _operatorContextService.GetOperatorDisplayName(),
                    ResponsibleTeam = onboarding.CurrentTeam ?? "Default",
                    Priority = onboarding.Priority ?? "Medium",
                    Source = "CustomerPortal",
                    BusinessContext = new Dictionary<string, object>
                    {
                        ["CompletionMethod"] = "CompleteCurrentStage",
                        ["AutoMoveToNext"] = !isFinalStage,
                        ["CompletionNotes"] = "Stage completed via CompleteCurrentStageAsync"
                    },
                    RoutingTags = new List<string> { "onboarding", "stage-completion", "customer-portal", "auto-progression" },
                    Description = $"Stage '{completedStage.Name}' completed for Onboarding {onboarding.Id} via CompleteCurrentStageAsync",
                    Tags = new List<string> { "onboarding", "stage-completion", "auto-progression" },
                    Components = componentsPayload2
                };
                // Append lightweight debug metrics into business context for verification
                try
                {
                    onboardingStageCompletedEvent.BusinessContext["Components.ChecklistsCount"] = componentsPayload2?.Checklists?.Count ?? 0;
                    onboardingStageCompletedEvent.BusinessContext["Components.QuestionnairesCount"] = componentsPayload2?.Questionnaires?.Count ?? 0;
                    onboardingStageCompletedEvent.BusinessContext["Components.TaskCompletionsCount"] = componentsPayload2?.TaskCompletions?.Count ?? 0;
                    onboardingStageCompletedEvent.BusinessContext["Components.RequiredFieldsCount"] = componentsPayload2?.RequiredFields?.Count ?? 0;
                }
                catch { }

                // 使用 fire-and-forget 方式异步处理事件，不阻塞主流程
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        // 创建新的作用域来避免 ServiceProvider disposed 错误
                        using var scope = _serviceScopeFactory.CreateScope();
                        var scopedMediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                        await scopedMediator.Publish(onboardingStageCompletedEvent);
                    }
                    catch (Exception ex)
                    {
                        // 记录错误但不影响主流程
                        // TODO: 可以考虑添加重试机制或者使用消息队列
                        // 这里可以添加日志记录，但要确保不抛出异常
                    }
                });

                // Debug logging handled by structured logging
            }
            catch (Exception ex)
            {
                // Log error but don't fail the stage completion
                // Debug logging handled by structured logging
            }
        }

        /// <summary>
        /// Build components payload for stage completion event
        /// </summary>
        private async Task<StageCompletionComponents> BuildStageCompletionComponentsAsync(long onboardingId, long stageId, List<StageComponent> stageComponents, string componentsJson)
        {
            var payload = new StageCompletionComponents();

            LoggingExtensions.WriteLine($"[DEBUG] BuildStageCompletionComponentsAsync - OnboardingId: {onboardingId}, StageId: {stageId}");
            LoggingExtensions.WriteLine($"[DEBUG] Initial stageComponents count: {stageComponents?.Count ?? 0}");
            LoggingExtensions.WriteLine($"[DEBUG] Initial componentsJson: {(!string.IsNullOrWhiteSpace(componentsJson) ? "Present" : "Missing")}");

            // Ensure we have componentsJson from DB if missing
            if (string.IsNullOrWhiteSpace(componentsJson))
            {
                try
                {
                    var stageEntity = await _stageRepository.GetByIdAsync(stageId);
                    if (!string.IsNullOrWhiteSpace(stageEntity?.ComponentsJson))
                    {
                        componentsJson = stageEntity.ComponentsJson;
                        LoggingExtensions.WriteLine($"[DEBUG] Retrieved componentsJson from DB: {componentsJson.Substring(0, Math.Min(200, componentsJson.Length))}...");
                    }
                }
                catch (Exception ex)
                {
                    LoggingExtensions.WriteLine($"[DEBUG] Error retrieving componentsJson from DB: {ex.Message}");
                }
            }

            // Ensure we have components from JSON if not provided
            if ((stageComponents == null || stageComponents.Count == 0) && !string.IsNullOrWhiteSpace(componentsJson))
            {
                try
                {
                    stageComponents = JsonSerializer.Deserialize<List<StageComponent>>(componentsJson) ?? new List<StageComponent>();
                    LoggingExtensions.WriteLine($"[DEBUG] Standard JSON deserialization successful, components count: {stageComponents.Count}");
                }
                catch (Exception ex)
                {
                    LoggingExtensions.WriteLine($"[DEBUG] Standard JSON deserialization failed: {ex.Message}");
                    // Fallback: parse components from raw JSON with lenient parsing
                    var (parsedComponents, parsedStaticFields) = ParseComponentsFromJson(componentsJson);
                    stageComponents = parsedComponents;
                    LoggingExtensions.WriteLine($"[DEBUG] Lenient parsing successful, components count: {stageComponents.Count}");
                }
            }

            // Try fetch components from service if still empty
            if (stageComponents == null || stageComponents.Count == 0)
            {
                try
                {
                    var comps = await _stageService.GetComponentsAsync(stageId);
                    if (comps != null && comps.Count > 0)
                    {
                        stageComponents = comps;
                        LoggingExtensions.WriteLine($"[DEBUG] Retrieved components from service, count: {stageComponents.Count}");
                    }
                }
                catch (Exception ex)
                {
                    LoggingExtensions.WriteLine($"[DEBUG] Error retrieving components from service: {ex.Message}");
                }
            }

            LoggingExtensions.WriteLine($"[DEBUG] Final stageComponents count before processing: {stageComponents?.Count ?? 0}");

            // Process components to populate payload
            await ProcessStageComponentsAsync(onboardingId, stageId, stageComponents, componentsJson, payload);

            LoggingExtensions.WriteLine($"[DEBUG] Final payload counts - Checklists: {payload.Checklists.Count}, Questionnaires: {payload.Questionnaires.Count}, TaskCompletions: {payload.TaskCompletions.Count}, RequiredFields: {payload.RequiredFields.Count}");

            return payload;
        }
        /// <summary>
        /// Parse components from JSON with lenient parsing for both camelCase and PascalCase
        /// </summary>
        private (List<StageComponent> stageComponents, List<string> staticFieldNames) ParseComponentsFromJson(string componentsJson)
        {
            var stageComponents = new List<StageComponent>();
            var staticFieldNames = new List<string>();

            try
            {
                // Recursively unwrap JSON strings until we get to the actual array
                string currentJson = componentsJson;
                JsonDocument currentDoc = null;
                int unwrapCount = 0;

                while (true)
                {
                    currentDoc?.Dispose();
                    currentDoc = JsonDocument.Parse(currentJson);
                    LoggingExtensions.WriteLine($"[DEBUG] JSON unwrap #{unwrapCount}: root type = {currentDoc.RootElement.ValueKind}");

                    if (currentDoc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        LoggingExtensions.WriteLine($"[DEBUG] Found JSON array after {unwrapCount} unwraps");
                        break;
                    }
                    else if (currentDoc.RootElement.ValueKind == JsonValueKind.String)
                    {
                        currentJson = currentDoc.RootElement.GetString();
                        LoggingExtensions.WriteLine($"[DEBUG] Unwrapped JSON string, length: {currentJson?.Length}");
                        unwrapCount++;

                        if (unwrapCount > 5) // Prevent infinite loop
                        {
                            LoggingExtensions.WriteLine($"[ERROR] Too many JSON unwrap levels, stopping at {unwrapCount}");
                            break;
                        }
                    }
                    else
                    {
                        LoggingExtensions.WriteLine($"[ERROR] Unexpected JSON root type: {currentDoc.RootElement.ValueKind}");
                        break;
                    }
                }

                if (currentDoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    LoggingExtensions.WriteLine($"[DEBUG] Processing {currentDoc.RootElement.GetArrayLength()} JSON array elements");
                    int elementIndex = 0;
                    foreach (var elem in currentDoc.RootElement.EnumerateArray())
                    {
                        LoggingExtensions.WriteLine($"[DEBUG] Element {elementIndex}: Type={elem.ValueKind}");
                        if (elem.ValueKind != JsonValueKind.Object)
                        {
                            elementIndex++;
                            continue;
                        }

                        // Get key with both camelCase and PascalCase support
                        string key = GetJsonProperty(elem, "key", "Key");
                        LoggingExtensions.WriteLine($"[DEBUG] Element {elementIndex} - Raw JSON: {elem.GetRawText()}");
                        LoggingExtensions.WriteLine($"[DEBUG] Element {elementIndex} - Extracted key: '{key}'");
                        if (string.IsNullOrWhiteSpace(key))
                        {
                            elementIndex++;
                            continue;
                        }

                        // Create StageComponent for each parsed element
                        var component = new StageComponent { Key = key };

                        if (string.Equals(key, "checklist", StringComparison.OrdinalIgnoreCase))
                        {
                            // Populate the StageComponent
                            component.ChecklistIds = new List<long>();
                            component.ChecklistNames = new List<string>();
                            var idArr = GetJsonArrayProperty(elem, "checklistIds", "ChecklistIds");
                            if (idArr.HasValue)
                            {
                                foreach (var idEl in idArr.Value.EnumerateArray())
                                {
                                    if (idEl.ValueKind == JsonValueKind.Number && idEl.TryGetInt64(out var lid)) component.ChecklistIds.Add(lid);
                                    else if (idEl.ValueKind == JsonValueKind.String && long.TryParse(idEl.GetString(), out var lsid)) component.ChecklistIds.Add(lsid);
                                }
                            }
                            var nameArr = GetJsonArrayProperty(elem, "checklistNames", "ChecklistNames");
                            if (nameArr.HasValue)
                            {
                                foreach (var nEl in nameArr.Value.EnumerateArray())
                                {
                                    if (nEl.ValueKind == JsonValueKind.String) component.ChecklistNames.Add(nEl.GetString());
                                }
                            }
                        }
                        else if (string.Equals(key, "questionnaires", StringComparison.OrdinalIgnoreCase))
                        {
                            // Populate the StageComponent
                            component.QuestionnaireIds = new List<long>();
                            component.QuestionnaireNames = new List<string>();
                            var idArr = GetJsonArrayProperty(elem, "questionnaireIds", "QuestionnaireIds");
                            if (idArr.HasValue)
                            {
                                foreach (var idEl in idArr.Value.EnumerateArray())
                                {
                                    if (idEl.ValueKind == JsonValueKind.Number && idEl.TryGetInt64(out var lid)) component.QuestionnaireIds.Add(lid);
                                    else if (idEl.ValueKind == JsonValueKind.String && long.TryParse(idEl.GetString(), out var lsid)) component.QuestionnaireIds.Add(lsid);
                                }
                            }
                            var nameArr = GetJsonArrayProperty(elem, "questionnaireNames", "QuestionnaireNames");
                            if (nameArr.HasValue)
                            {
                                foreach (var nEl in nameArr.Value.EnumerateArray())
                                {
                                    if (nEl.ValueKind == JsonValueKind.String) component.QuestionnaireNames.Add(nEl.GetString());
                                }
                            }
                        }
                        else if (string.Equals(key, "fields", StringComparison.OrdinalIgnoreCase))
                        {
                            // Extract static field names
                            var sfArr = GetJsonArrayProperty(elem, "staticFields", "StaticFields");
                            if (sfArr.HasValue)
                            {
                                foreach (var s in sfArr.Value.EnumerateArray())
                                {
                                    if (s.ValueKind == JsonValueKind.String)
                                    {
                                        var name = s.GetString();
                                        if (!string.IsNullOrWhiteSpace(name)) staticFieldNames.Add(name);
                                    }
                                }
                            }

                            // Also populate the StageComponent
                            component.StaticFields = new List<string>(staticFieldNames);
                        }

                        // Add component to list after processing all types
                        stageComponents.Add(component);
                        LoggingExtensions.WriteLine($"[DEBUG] Added component '{key}' to list, total count: {stageComponents.Count}");
                        elementIndex++;
                    }
                }
                LoggingExtensions.WriteLine($"[DEBUG] ParseComponentsFromJson completed - Components: {stageComponents.Count}, StaticFields: {staticFieldNames.Count}");

                currentDoc?.Dispose();
            }
            catch (Exception ex)
            {
                LoggingExtensions.WriteLine($"[ERROR] ParseComponentsFromJson failed: {ex.Message}");
            }

            return (stageComponents, staticFieldNames);
        }

        /// <summary>
        /// Get JSON property value with support for both camelCase and PascalCase
        /// </summary>
        private string GetJsonProperty(JsonElement elem, string camelCase, string pascalCase)
        {
            if (elem.TryGetProperty(camelCase, out var prop)) return prop.GetString();
            if (elem.TryGetProperty(pascalCase, out var propPascal)) return propPascal.GetString();
            return null;
        }

        /// <summary>
        /// Get JSON array property with support for both camelCase and PascalCase
        /// </summary>
        private JsonElement? GetJsonArrayProperty(JsonElement elem, string camelCase, string pascalCase)
        {
            if (elem.TryGetProperty(camelCase, out var arr) && arr.ValueKind == JsonValueKind.Array) return arr;
            if (elem.TryGetProperty(pascalCase, out var arrPascal) && arrPascal.ValueKind == JsonValueKind.Array) return arrPascal;
            return null;
        }

        /// <summary>
        /// Process stage components to populate the payload
        /// </summary>
        private async Task ProcessStageComponentsAsync(long onboardingId, long stageId, List<StageComponent> stageComponents, string componentsJson, StageCompletionComponents payload)
        {
            var staticFieldNames = new List<string>();

            // If we need to parse from JSON for static fields, do it once here
            if ((stageComponents == null || !stageComponents.Any(c => c.Key == "fields")) && !string.IsNullOrWhiteSpace(componentsJson))
            {
                var (_, parsedStaticFields) = ParseComponentsFromJson(componentsJson);
                staticFieldNames.AddRange(parsedStaticFields);
            }

            // Process checklists
            await ProcessChecklistsAsync(onboardingId, stageId, stageComponents, componentsJson, payload);

            // Process questionnaires
            await ProcessQuestionnairesAsync(onboardingId, stageId, stageComponents, componentsJson, payload);

            // Process required fields
            await ProcessRequiredFieldsAsync(onboardingId, stageId, stageComponents, componentsJson, payload, staticFieldNames);
        }

        /// <summary>
        /// Process checklists and task completions
        /// </summary>
        private async Task ProcessChecklistsAsync(long onboardingId, long stageId, List<StageComponent> stageComponents, string componentsJson, StageCompletionComponents payload)
        {
            try
            {
                // Get checklist components - prioritize stageComponents, fallback to JSON parsing
                var checklistComponents = (stageComponents ?? new List<StageComponent>()).Where(c => c.Key == "checklist").ToList();

                if (checklistComponents.Count == 0 && !string.IsNullOrWhiteSpace(componentsJson))
                {
                    var (parsedComponents, _) = ParseComponentsFromJson(componentsJson);
                    checklistComponents = parsedComponents.Where(c => c.Key == "checklist").ToList();
                }

                // Fill checklist selections from components with detailed information
                foreach (var component in checklistComponents)
                {
                    if (component.ChecklistIds != null)
                    {
                        for (int i = 0; i < component.ChecklistIds.Count; i++)
                        {
                            var checklistId = component.ChecklistIds[i];
                            var checklistName = (component.ChecklistNames != null && component.ChecklistNames.Count > i)
                                ? component.ChecklistNames[i]
                                : $"Checklist {checklistId}";

                            // Get detailed checklist information
                            try
                            {
                                var detailedChecklist = await _checklistService.GetByIdAsync(checklistId);
                                if (detailedChecklist != null)
                                {
                                    // Map tasks
                                    var tasks = new List<ChecklistTaskInfo>();
                                    if (detailedChecklist.Tasks != null)
                                    {
                                        foreach (var task in detailedChecklist.Tasks)
                                        {
                                            tasks.Add(new ChecklistTaskInfo
                                            {
                                                Id = task.Id,
                                                ChecklistId = task.ChecklistId,
                                                Name = task.Name,
                                                Description = task.Description,
                                                OrderIndex = task.OrderIndex,
                                                TaskType = task.TaskType,
                                                IsRequired = task.IsRequired,
                                                EstimatedHours = task.EstimatedHours,
                                                Priority = task.Priority,
                                                IsCompleted = task.IsCompleted,
                                                Status = task.Status,
                                                IsActive = task.IsActive
                                            });
                                        }
                                    }

                                    payload.Checklists.Add(new ChecklistComponentInfo
                                    {
                                        ChecklistId = checklistId,
                                        ChecklistName = detailedChecklist.Name ?? checklistName,
                                        Description = detailedChecklist.Description,
                                        Team = detailedChecklist.Team,
                                        Type = detailedChecklist.Type,
                                        Status = detailedChecklist.Status,
                                        IsTemplate = detailedChecklist.IsTemplate,
                                        CompletionRate = detailedChecklist.CompletionRate,
                                        TotalTasks = detailedChecklist.TotalTasks,
                                        CompletedTasks = detailedChecklist.CompletedTasks,
                                        IsActive = detailedChecklist.IsActive,
                                        Tasks = tasks
                                    });
                                }
                                else
                                {
                                    // Fallback to basic info if detailed info not available
                                    payload.Checklists.Add(new ChecklistComponentInfo
                                    {
                                        ChecklistId = checklistId,
                                        ChecklistName = checklistName,
                                        IsActive = true
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                LoggingExtensions.WriteLine($"[ERROR] Failed to get checklist details for {checklistId}: {ex.Message}");
                                // Fallback to basic info
                                payload.Checklists.Add(new ChecklistComponentInfo
                                {
                                    ChecklistId = checklistId,
                                    ChecklistName = checklistName,
                                    IsActive = true
                                });
                            }
                        }
                    }
                }

                // Load task completions
                var completions = await _checklistTaskCompletionService.GetByOnboardingAndStageAsync(onboardingId, stageId);
                foreach (var c in completions)
                {
                    payload.TaskCompletions.Add(new ChecklistTaskCompletionInfo
                    {
                        ChecklistId = c.ChecklistId,
                        TaskId = c.TaskId,
                        IsCompleted = c.IsCompleted,
                        CompletionNotes = c.CompletionNotes,
                        CompletedBy = c.ModifyBy ?? c.CreateBy,
                        CompletedTime = c.CompletedTime
                    });
                }
            }
            catch { }
        }

        /// <summary>
        /// Process questionnaires and answers
        /// </summary>
        private async Task ProcessQuestionnairesAsync(long onboardingId, long stageId, List<StageComponent> stageComponents, string componentsJson, StageCompletionComponents payload)
        {
            try
            {
                // Get questionnaire components - prioritize stageComponents, fallback to JSON parsing
                var questionnaireComponents = (stageComponents ?? new List<StageComponent>()).Where(c => c.Key == "questionnaires").ToList();

                if (questionnaireComponents.Count == 0 && !string.IsNullOrWhiteSpace(componentsJson))
                {
                    var (parsedComponents, _) = ParseComponentsFromJson(componentsJson);
                    questionnaireComponents = parsedComponents.Where(c => c.Key == "questionnaires").ToList();
                }

                // Fill questionnaire selections from components with detailed information
                foreach (var component in questionnaireComponents)
                {
                    if (component.QuestionnaireIds != null)
                    {
                        for (int i = 0; i < component.QuestionnaireIds.Count; i++)
                        {
                            var questionnaireId = component.QuestionnaireIds[i];
                            var questionnaireName = (component.QuestionnaireNames != null && component.QuestionnaireNames.Count > i)
                                ? component.QuestionnaireNames[i]
                                : $"Questionnaire {questionnaireId}";

                            // Get detailed questionnaire information
                            try
                            {
                                var detailedQuestionnaire = await _questionnaireService.GetByIdAsync(questionnaireId);
                                if (detailedQuestionnaire != null)
                                {
                                    payload.Questionnaires.Add(new QuestionnaireComponentInfo
                                    {
                                        QuestionnaireId = questionnaireId,
                                        QuestionnaireName = detailedQuestionnaire.Name ?? questionnaireName,
                                        Description = detailedQuestionnaire.Description,
                                        Status = detailedQuestionnaire.Status,
                                        Version = detailedQuestionnaire.Version,
                                        Category = detailedQuestionnaire.Category,
                                        TotalQuestions = detailedQuestionnaire.TotalQuestions,
                                        RequiredQuestions = detailedQuestionnaire.RequiredQuestions,
                                        AllowDraft = detailedQuestionnaire.AllowDraft,
                                        AllowMultipleSubmissions = detailedQuestionnaire.AllowMultipleSubmissions,
                                        IsActive = detailedQuestionnaire.IsActive,
                                        StructureJson = detailedQuestionnaire.StructureJson
                                    });
                                }
                                else
                                {
                                    // Fallback to basic info if detailed info not available
                                    payload.Questionnaires.Add(new QuestionnaireComponentInfo
                                    {
                                        QuestionnaireId = questionnaireId,
                                        QuestionnaireName = questionnaireName,
                                        IsActive = true
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                LoggingExtensions.WriteLine($"[ERROR] Failed to get questionnaire details for {questionnaireId}: {ex.Message}");
                                // Fallback to basic info
                                payload.Questionnaires.Add(new QuestionnaireComponentInfo
                                {
                                    QuestionnaireId = questionnaireId,
                                    QuestionnaireName = questionnaireName,
                                    IsActive = true
                                });
                            }
                        }
                    }
                }

                // Load questionnaire answers
                var answers = await _questionnaireAnswerService.GetAllAnswersAsync(onboardingId, stageId);
                foreach (var a in answers)
                {
                    payload.QuestionnaireAnswers.Add(new QuestionnaireAnswerInfo
                    {
                        AnswerId = a.Id,
                        QuestionnaireId = a.QuestionnaireId ?? 0,
                        QuestionId = 0,
                        QuestionText = string.Empty,
                        QuestionType = string.Empty,
                        IsRequired = false,
                        Answer = a.AnswerJson,
                        AnswerTime = a.SubmitTime,
                        Status = a.Status
                    });
                }
            }
            catch { }
        }

        /// <summary>
        /// Process required fields
        /// </summary>
        private async Task ProcessRequiredFieldsAsync(long onboardingId, long stageId, List<StageComponent> stageComponents, string componentsJson, StageCompletionComponents payload, List<string> staticFieldNames)
        {
            try
            {
                // Collect static field names from components
                foreach (var comp in (stageComponents ?? new List<StageComponent>()).Where(c => c.Key == "fields"))
                {
                    if (comp.StaticFields != null)
                    {
                        foreach (var name in comp.StaticFields)
                        {
                            if (!string.IsNullOrWhiteSpace(name) && !staticFieldNames.Contains(name))
                            {
                                staticFieldNames.Add(name);
                            }
                        }
                    }
                }

                // If no static fields found, parse from JSON
                if (staticFieldNames.Count == 0 && !string.IsNullOrWhiteSpace(componentsJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(componentsJson);
                        if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var elem in doc.RootElement.EnumerateArray())
                            {
                                if (elem.ValueKind != JsonValueKind.Object) continue;

                                string key = GetJsonProperty(elem, "key", "Key");
                                if (!string.Equals(key, "fields", StringComparison.OrdinalIgnoreCase)) continue;

                                var sfArr = GetJsonArrayProperty(elem, "staticFields", "StaticFields");
                                if (sfArr.HasValue)
                                {
                                    foreach (var s in sfArr.Value.EnumerateArray())
                                    {
                                        if (s.ValueKind == JsonValueKind.String)
                                        {
                                            var name = s.GetString();
                                            if (!string.IsNullOrWhiteSpace(name) && !staticFieldNames.Contains(name))
                                            {
                                                staticFieldNames.Add(name);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }

                // Load existing field values
                var fieldValues = await _staticFieldValueService.GetLatestByOnboardingAndStageAsync(onboardingId, stageId);
                var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var f in fieldValues)
                {
                    payload.RequiredFields.Add(new RequiredFieldInfo
                    {
                        FieldName = f.FieldName,
                        DisplayName = f.DisplayName,
                        FieldType = f.FieldType,
                        IsRequired = f.IsRequired,
                        FieldValue = f.FieldValueJson,
                        ValidationStatus = f.ValidationStatus,
                        ValidationErrors = string.IsNullOrWhiteSpace(f.ValidationErrors)
                            ? new List<string>()
                            : new List<string>(f.ValidationErrors.Split('\n'))
                    });
                    existing.Add(f.FieldName ?? string.Empty);
                }

                // Add placeholder entries for missing required fields
                foreach (var fieldName in staticFieldNames.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (!existing.Contains(fieldName))
                    {
                        payload.RequiredFields.Add(new RequiredFieldInfo
                        {
                            FieldName = fieldName,
                            DisplayName = null,
                            FieldType = string.Empty,
                            IsRequired = true,
                            FieldValue = null,
                            ValidationStatus = "Pending",
                            ValidationErrors = new List<string>()
                        });
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Export onboarding data to Excel
        /// </summary>
        public async Task<Stream> ExportToExcelAsync(OnboardingQueryRequest query)
        {
            // Get data using existing query method
            var result = await QueryAsync(query);
            var data = result.Data;

            // Transform to export format
            var exportData = data.Select(item => new OnboardingExportDto
            {
                CustomerName = item.LeadName,
                Id = item.LeadId,
                CaseCode = item.CaseCode,
                ContactName = item.ContactPerson,
                LifeCycleStage = item.LifeCycleStageName,
                WorkFlow = item.WorkflowName,
                OnboardStage = item.CurrentStageName,
                Priority = item.Priority,
                Ownership = !string.IsNullOrWhiteSpace(item.OwnershipName)
                    ? $"{item.OwnershipName} ({item.OwnershipEmail})"
                    : string.Empty,
                Status = GetDisplayStatus(item.Status),
                StartDate = FormatDateForExport(item.CurrentStageStartTime),
                EndDate = FormatDateForExport(item.CurrentStageEndTime),
                UpdatedBy = string.IsNullOrWhiteSpace(item.StageUpdatedBy) ? item.ModifyBy : item.StageUpdatedBy,
                UpdateTime = (item.StageUpdatedTime.HasValue ? item.StageUpdatedTime.Value : item.ModifyDate)
                  .ToString("MM/dd/yyyy HH:mm:ss")
            }).ToList();
            // Use EPPlus to generate Excel file (avoid NPOI version conflict)
            return GenerateExcelWithEPPlus(exportData);
        }

        /// <summary>
        /// Convert status to display format to match frontend logic
        /// </summary>
        private string GetDisplayStatus(string status)
        {
            if (string.IsNullOrEmpty(status))
                return status;

            return status switch
            {
                "Active" or "Started" => "InProgress",
                "Cancelled" => "Aborted",
                "Force Completed" => "Force Completed",
                _ => status
            };
        }

        /// <summary>
        /// Format date to match frontend display format (MM/dd/yyyy HH:mm)
        /// </summary>
        private string FormatDateForExport(DateTimeOffset? dateTime)
        {
            if (!dateTime.HasValue)
                return "";

            // Format as MM/dd/yyyy HH:mm to include time precision to minutes
            return dateTime.Value.ToString("MM/dd/yyyy HH:mm");
        }

        /// <summary>
        /// Generate Excel file using EPPlus
        /// </summary>
        private Stream GenerateExcelWithEPPlus(List<OnboardingExportDto> data)
        {
            using var package = new OfficeOpenXml.ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Onboarding Export");

            // Set headers
            var headers = new[]
            {
                "Customer Name", "Case Code", "Contact Name", "Life Cycle Stage", "Workflow", "Stage",
                "Priority", "Ownership", "Status", "Start Date", "End Date", "Updated By", "Update Time"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }

            // Set data
            for (int row = 0; row < data.Count; row++)
            {
                var item = data[row];
                worksheet.Cells[row + 2, 1].Value = item.CustomerName;
                worksheet.Cells[row + 2, 2].Value = item.CaseCode;
                worksheet.Cells[row + 2, 3].Value = item.ContactName;
                worksheet.Cells[row + 2, 4].Value = item.LifeCycleStage;
                worksheet.Cells[row + 2, 5].Value = item.WorkFlow;
                worksheet.Cells[row + 2, 6].Value = item.OnboardStage;
                worksheet.Cells[row + 2, 7].Value = item.Priority;
                worksheet.Cells[row + 2, 8].Value = item.Ownership;
                worksheet.Cells[row + 2, 9].Value = item.Status;
                worksheet.Cells[row + 2, 10].Value = item.StartDate;
                worksheet.Cells[row + 2, 11].Value = item.EndDate;
                worksheet.Cells[row + 2, 12].Value = item.UpdatedBy;
                worksheet.Cells[row + 2, 13].Value = item.UpdateTime;
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// Initialize stages progress array for a new onboarding
        /// </summary>
        private async Task InitializeStagesProgressAsync(Onboarding entity, List<Stage> stages)
        {
            try
            {
                entity.StagesProgress = new List<OnboardingStageProgress>();

                if (stages == null || !stages.Any())
                {
                    // Debug logging handled by structured logging
                    entity.StagesProgressJson = "[]";
                    return;
                }

                var orderedStages = stages.OrderBy(s => s.Order).ToList();
                var currentTime = DateTimeOffset.UtcNow;

                // Use sequential stage order (1, 2, 3, 4, 5...) instead of the original stage.Order
                for (int i = 0; i < orderedStages.Count; i++)
                {
                    var stage = orderedStages[i];
                    var sequentialOrder = i + 1; // Sequential order starting from 1

                    var stageProgress = new OnboardingStageProgress
                    {
                        // Core progress fields (will be serialized to JSON)
                        StageId = stage.Id,
                        Status = sequentialOrder == 1 ? "InProgress" : "Pending", // First stage starts as InProgress
                        IsCompleted = false,
                        StartTime = null, // StartTime is now null by default, will be set when stage is saved or completed
                        CompletionTime = null,
                        CompletedById = null,
                        CompletedBy = null,
                        Notes = null,
                        IsCurrent = sequentialOrder == 1, // First stage is current

                        // Stage configuration fields (not serialized, populated dynamically)
                        StageName = stage.Name,
                        StageDescription = stage.Description,
                        StageOrder = sequentialOrder,
                        EstimatedDays = stage.EstimatedDuration,
                        VisibleInPortal = stage.VisibleInPortal,
                        PortalPermission = stage.PortalPermission,
                        AttachmentManagementNeeded = stage.AttachmentManagementNeeded,
                        ComponentsJson = stage.ComponentsJson,
                        Components = stage.Components
                    };

                    entity.StagesProgress.Add(stageProgress);

                    // Debug logging handled by structured logging");
                }

                // Serialize to JSON for database storage (only progress fields, not stage configuration)
                entity.StagesProgressJson = SerializeStagesProgress(entity.StagesProgress);
                // Debug logging handled by structured logging
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                entity.StagesProgress = new List<OnboardingStageProgress>();
                entity.StagesProgressJson = "[]";
            }
        }
        /// <summary>
        /// Update stages progress - supports non-sequential stage completion
        /// </summary>
        private async Task UpdateStagesProgressAsync(Onboarding entity, long completedStageId, string completedBy = null, long? completedById = null, string notes = null)
        {
            try
            {
                // Load current progress using the proper method that handles JSON formatting
                LoadStagesProgressFromJson(entity);

                // Debug: Check if stageIds are correctly loaded
                LoggingExtensions.WriteLine($"[DEBUG] UpdateStagesProgressAsync - After LoadStagesProgressFromJson:");
                LoggingExtensions.WriteLine($"[DEBUG] StagesProgress count: {entity.StagesProgress?.Count ?? 0}");
                if (entity.StagesProgress != null)
                {
                    foreach (var sp in entity.StagesProgress)
                    {
                        LoggingExtensions.WriteLine($"[DEBUG] StageProgress: StageId={sp.StageId}, Status={sp.Status}, IsCurrent={sp.IsCurrent}");
                    }
                }

                var currentTime = DateTimeOffset.UtcNow;
                var completedStage = entity.StagesProgress.FirstOrDefault(s => s.StageId == completedStageId);

                if (completedStage != null)
                {
                    // Debug logging handled by structured logging
                    // Debug logging handled by structured logging");

                    // Check if stage can be re-completed
                    var wasAlreadyCompleted = completedStage.IsCompleted;

                    // Mark current stage as completed
                    completedStage.Status = "Completed";
                    completedStage.IsCompleted = true;
                    completedStage.CompletionTime = currentTime;
                    completedStage.CompletedBy = completedBy ?? _operatorContextService.GetOperatorDisplayName();
                    completedStage.CompletedById = completedById ?? _operatorContextService.GetOperatorId();
                    completedStage.IsCurrent = false;
                    completedStage.LastUpdatedTime = currentTime;
                    completedStage.LastUpdatedBy = completedBy ?? _operatorContextService.GetOperatorDisplayName();

                    // Set StartTime if not already set (only during complete operations)
                    // This ensures StartTime is set when user actually completes work, not during status changes
                    if (!completedStage.StartTime.HasValue)
                    {
                        completedStage.StartTime = currentTime;
                    }

                    if (!string.IsNullOrEmpty(notes))
                    {
                        // Append new notes to existing notes if stage was re-completed
                        if (wasAlreadyCompleted && !string.IsNullOrEmpty(completedStage.Notes))
                        {
                            completedStage.Notes += $"\n[Re-completed {currentTime:yyyy-MM-dd HH:mm:ss}] {notes}";
                        }
                        else
                        {
                            completedStage.Notes = notes;
                        }
                    }

                    // Debug logging handled by structured logging}");

                    // Find next stage to activate (first incomplete stage after current completed stage)
                    var nextStage = entity.StagesProgress
                        .Where(s => s.StageOrder > completedStage.StageOrder && !s.IsCompleted)
                        .OrderBy(s => s.StageOrder)
                        .FirstOrDefault();

                    // Clear all current stage flags first
                    foreach (var stage in entity.StagesProgress)
                    {
                        stage.IsCurrent = false;
                    }

                    if (nextStage != null)
                    {
                        // Activate the next incomplete stage
                        nextStage.Status = "InProgress";
                        // Don't set StartTime here - only set it during save or complete operations
                        nextStage.IsCurrent = true;
                        nextStage.LastUpdatedTime = currentTime;
                        nextStage.LastUpdatedBy = completedBy ?? _operatorContextService.GetOperatorDisplayName();

                        // Debug logging handled by structured logging");
                    }
                    else
                    {
                        // All stages after the completed stage are already completed
                        // Don't go backward to find incomplete stages - this maintains forward progression
                        // Debug logging handled by structured logging
                    }

                    // Update completion rate based on completed stages
                    entity.CompletionRate = CalculateCompletionRateByCompletedStages(entity.StagesProgress);

                    // Debug logging handled by structured logging");
                }

                // Serialize back to JSON (only progress fields)
                await FilterValidStagesProgress(entity);
                entity.StagesProgressJson = SerializeStagesProgress(entity.StagesProgress);
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
            }
        }

        /// <summary>
        /// Load stages progress from JSONB - optimized version with JSONB support
        /// Handles both legacy JSON format and new JSONB format with camelCase properties
        /// </summary>
        private void LoadStagesProgressFromJson(Onboarding entity)
        {
            try
            {
                if (!string.IsNullOrEmpty(entity.StagesProgressJson))
                {
                    // Debug: Show input JSON
                    LoggingExtensions.WriteLine($"[DEBUG] LoadStagesProgressFromJson - Input JSON:");
                    LoggingExtensions.WriteLine($"[DEBUG] {entity.StagesProgressJson}");

                    // Configure JsonSerializer options to handle both formats
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        // Allow trailing commas and comments for JSONB compatibility
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    };

                    entity.StagesProgress = JsonSerializer.Deserialize<List<OnboardingStageProgress>>(
                        entity.StagesProgressJson, options) ?? new List<OnboardingStageProgress>();

                    // Debug: Show loaded data
                    LoggingExtensions.WriteLine($"[DEBUG] LoadStagesProgressFromJson - Loaded {entity.StagesProgress.Count} items:");
                    foreach (var sp in entity.StagesProgress)
                    {
                        LoggingExtensions.WriteLine($"[DEBUG] Loaded StageProgress: StageId={sp.StageId}, Status={sp.Status}, IsCurrent={sp.IsCurrent}");
                    }

                    // Only fix stage order when needed, avoid unnecessary serialization
                    if (NeedsStageOrderFix(entity.StagesProgress))
                    {
                        FixStageOrderSequence(entity.StagesProgress);
                        entity.StagesProgressJson = SerializeStagesProgress(entity.StagesProgress);
                    }
                }
                else
                {
                    entity.StagesProgress = new List<OnboardingStageProgress>();
                }
            }
            catch (JsonException jsonEx)
            {
                // Handle JSON parsing errors specifically
                LoggingExtensions.WriteLine($"JSON parsing error in LoadStagesProgressFromJson: {jsonEx.Message}");
                entity.StagesProgress = new List<OnboardingStageProgress>();
            }
            catch (Exception ex)
            {
#if DEBUG
                // Debug logging handled by structured logging
#endif
                entity.StagesProgress = new List<OnboardingStageProgress>();
            }
        }

        /// <summary>
        /// Check if stage order needs to be fixed
        /// </summary>
        private bool NeedsStageOrderFix(List<OnboardingStageProgress> stagesProgress)
        {
            if (stagesProgress == null || !stagesProgress.Any())
                return false;

            var orderedStages = stagesProgress.OrderBy(s => s.StageOrder).ToList();
            for (int i = 0; i < orderedStages.Count; i++)
            {
                if (orderedStages[i].StageOrder != i + 1)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Fix stage order to be sequential (1, 2, 3, 4, 5...) instead of potentially non-consecutive orders
        /// </summary>
        private void FixStageOrderSequence(List<OnboardingStageProgress> stagesProgress)
        {
            try
            {
                if (stagesProgress == null || !stagesProgress.Any())
                {
                    return;
                }

                // Sort by current stage order to maintain the original sequence
                var orderedStages = stagesProgress.OrderBy(s => s.StageOrder).ToList();

                // Check if stage orders are already sequential
                bool needsFixing = false;
                for (int i = 0; i < orderedStages.Count; i++)
                {
                    if (orderedStages[i].StageOrder != i + 1)
                    {
                        needsFixing = true;
                        break;
                    }
                }

                if (!needsFixing)
                {
                    // Debug logging handled by structured logging
                    return;
                }
                // Debug logging handled by structured logging
                // Reassign sequential stage orders
                for (int i = 0; i < orderedStages.Count; i++)
                {
                    var oldOrder = orderedStages[i].StageOrder;
                    var newOrder = i + 1;

                    orderedStages[i].StageOrder = newOrder;
                    // Debug logging handled by structured logging
                }

                // Update the original list with fixed orders safely
                // Instead of modifying the list during enumeration, replace each item individually
                for (int i = 0; i < stagesProgress.Count; i++)
                {
                    var matchingStage = orderedStages.FirstOrDefault(s => s.StageId == stagesProgress[i].StageId);
                    if (matchingStage != null)
                    {
                        stagesProgress[i] = matchingStage;
                    }
                }
                // Debug logging handled by structured logging
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
            }
        }

        /// <summary>
        /// Validate if a stage can be completed based on business rules
        /// </summary>
        private async Task<(bool CanComplete, string ErrorMessage)> ValidateStageCanBeCompletedAsync(Onboarding entity, long stageId)
        {
            try
            {
                // Debug logging handled by structured logging
                // Load stages progress
                LoadStagesProgressFromJson(entity);

                if (entity.StagesProgress == null || !entity.StagesProgress.Any())
                {
                    return (false, "No stages progress found");
                }

                var stageToComplete = entity.StagesProgress.FirstOrDefault(s => s.StageId == stageId);
                if (stageToComplete == null)
                {
                    return (false, "Stage not found in progress");
                }

                // Debug logging handled by structured logging");

                // Check if stage is already completed
                if (stageToComplete.IsCompleted)
                {
                    // Debug logging handled by structured logging
                    // Don't return false, allow re-completion but log the warning
                }

                // Check onboarding status
                if (entity.Status == "Completed")
                {
                    return (false, "Onboarding is already completed");
                }

                if (entity.Status == "Cancelled" || entity.Status == "Rejected")
                {
                    return (false, $"Cannot complete stages when onboarding status is {entity.Status}");
                }

                // Allow completing any non-completed stage (removed sequential restriction)
                // Debug logging handled by structured logging");
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                return (false, $"Validation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get current user name from OperatorContextService
        /// </summary>
        private string GetCurrentUserName()
        {
            return _operatorContextService.GetOperatorDisplayName();
        }

        /// <summary>
        /// Get current user email from OperatorContextService 
        /// </summary>
        private string GetCurrentUserEmail()
        {
            var displayName = _operatorContextService.GetOperatorDisplayName();
            // If display name looks like an email, return it; otherwise fallback
            if (!string.IsNullOrEmpty(displayName) && displayName.Contains("@"))
            {
                return displayName;
            }
            return !string.IsNullOrEmpty(_userContext?.Email) ? _userContext.Email : "system@example.com";
        }

        /// <summary>
        /// Get current user ID from OperatorContextService
        /// </summary>
        private long? GetCurrentUserId()
        {
            var id = _operatorContextService.GetOperatorId();
            return id == 0 ? null : id;
        }

        /// <summary>
        /// Get current user full name from OperatorContextService
        /// </summary>
        private string GetCurrentUserFullName()
        {
            return _operatorContextService.GetOperatorDisplayName();
        }

        /// <summary>
        /// Calculate completion rate based on completed stages count
        /// This method calculates progress based on how many stages are completed vs total stages
        /// Supports non-sequential stage completion
        /// </summary>
        private decimal CalculateCompletionRateByCompletedStages(List<OnboardingStageProgress> stagesProgress)
        {
            try
            {
                if (stagesProgress == null || !stagesProgress.Any())
                {
                    return 0;
                }

                var totalStagesCount = stagesProgress.Count;
                var completedStagesCount = stagesProgress.Count(s => s.IsCompleted);

                if (totalStagesCount == 0)
                {
                    return 0;
                }

                var completionRate = Math.Round((decimal)completedStagesCount / totalStagesCount * 100, 2);
                // Debug logging handled by structured logging
                // Debug logging handled by structured logging.Select(s => $"{s.StageOrder}:{s.StageName}"))}]");
                // Debug logging handled by structured logging.Select(s => $"{s.StageOrder}:{s.StageName}"))}]");

                return completionRate;
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                return 0;
            }
        }

        /// <summary>
        /// Clear related cache data
        /// </summary>
        private async Task ClearRelatedCacheAsync(long? workflowId = null, long? stageId = null)
        {
            try
            {
                // Safely get tenant ID
                var tenantId = !string.IsNullOrEmpty(_userContext?.TenantId)
                    ? _userContext.TenantId.ToLowerInvariant()
                    : "default";

                var tasks = new List<Task>();

                if (workflowId.HasValue)
                {
                    var workflowCacheKey = $"{WORKFLOW_CACHE_PREFIX}:{tenantId}:{workflowId.Value}";
                    // Redis cache temporarily disabled
                    // tasks.Add(_redisService.KeyDelAsync(workflowCacheKey));
                }

                if (stageId.HasValue)
                {
                    var stageCacheKey = $"{STAGE_CACHE_PREFIX}:{tenantId}:{stageId.Value}";
                    // Redis cache temporarily disabled
                    // tasks.Add(_redisService.KeyDelAsync(stageCacheKey));
                }

                if (tasks.Any())
                {
                    await Task.WhenAll(tasks);
#if DEBUG
                    // Debug logging handled by structured logging
#endif
                }
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                // Cache cleanup failure should not affect main flow
            }
        }

        /// <summary>
        /// Clear Onboarding query cache
        /// </summary>
        private async Task ClearOnboardingQueryCacheAsync()
        {
            try
            {
                string tenantId = _userContext?.TenantId ?? "default";

                // Use Keys method to get all matching keys, then batch delete
                var pattern = $"ow:onboarding:query:{tenantId}:*";
                // Redis cache temporarily disabled
                var keys = new List<string>();

                if (keys != null && keys.Any())
                {
                    // Batch delete all matching keys
                    // Redis cache temporarily disabled
                    var deleteTasks = keys.Select(key => Task.CompletedTask);
                    await Task.WhenAll(deleteTasks);

#if DEBUG
                    // Debug logging handled by structured logging
#endif
                }
                else
                {
#if DEBUG
                    // Debug logging handled by structured logging
#endif
                }
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                // Cache cleanup failure should not affect main flow
            }
        }

        /// <summary>
        /// Enrich stages progress with data from Stage entities
        /// This method dynamically populates fields like stageName, stageOrder, estimatedDays etc.
        /// from the Stage entities, ensuring consistency and reducing data duplication.
        /// </summary>
        private async Task EnrichStagesProgressWithStageDataAsync(Onboarding entity)
        {
            try
            {
                if (entity?.StagesProgress == null || !entity.StagesProgress.Any())
                {
                    return;
                }

                // Get all stages for this workflow
                var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
                if (stages == null || !stages.Any())
                {
                    return;
                }

                // Create a dictionary for fast lookup
                var stageDict = stages.ToDictionary(s => s.Id, s => s);

                // Enrich each stage progress with stage data
                foreach (var stageProgress in entity.StagesProgress)
                {
                    if (stageDict.TryGetValue(stageProgress.StageId, out var stage))
                    {
                        // Populate fields from Stage entity
                        stageProgress.StageName = stage.Name;
                        stageProgress.StageDescription = stage.Description;

                        // IMPORTANT: Only update EstimatedDays if CustomEstimatedDays is not set
                        // This preserves the custom values set by users and maintains the correct priority:
                        // CustomEstimatedDays > EstimatedDays (from Stage)
                        if (!stageProgress.CustomEstimatedDays.HasValue)
                        {
                            stageProgress.EstimatedDays = stage.EstimatedDuration;
                        }
                        // If CustomEstimatedDays exists, EstimatedDays should show the custom value
                        // (This will be handled by AutoMapper: EstimatedDays = CustomEstimatedDays ?? EstimatedDays)

                        stageProgress.VisibleInPortal = stage.VisibleInPortal;
                        stageProgress.PortalPermission = stage.PortalPermission;
                        stageProgress.AttachmentManagementNeeded = stage.AttachmentManagementNeeded;
                        stageProgress.ComponentsJson = stage.ComponentsJson;
                        stageProgress.Components = stage.Components;
                        // AI Summary 回填策略已移除：Stage不再包含AI摘要字段
                        // AI摘要数据现在仅存储在Onboarding的StageProgress中

                        // Backfill: If stage is completed but Onboarding's AI Summary is still empty, trigger async generation once
                        if (stageProgress.IsCompleted && string.IsNullOrWhiteSpace(stageProgress.AiSummary))
                        {
                            try
                            {
                                var opts = new StageSummaryOptions
                                {
                                    Language = "auto",
                                    SummaryLength = "short",
                                    IncludeTaskAnalysis = true,
                                    IncludeQuestionnaireInsights = true
                                };
                                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                                {
                                    var ai = await _stageService.GenerateAISummaryAsync(stageProgress.StageId, entity.Id, opts);
                                    if (ai != null && ai.Success)
                                    {
                                        LoadStagesProgressFromJson(entity);
                                        var sp = entity.StagesProgress?.FirstOrDefault(s => s.StageId == stageProgress.StageId);
                                        if (sp != null && string.IsNullOrWhiteSpace(sp.AiSummary))
                                        {
                                            sp.AiSummary = ai.Summary;
                                            sp.AiSummaryGeneratedAt = DateTime.UtcNow;
                                            sp.AiSummaryConfidence = (decimal?)Convert.ToDecimal(ai.ConfidenceScore);
                                            sp.AiSummaryModel = ai.ModelUsed;
                                            var detailedData = new { ai.Breakdown, ai.KeyInsights, ai.Recommendations, ai.CompletionStatus, generatedAt = DateTime.UtcNow };
                                            sp.AiSummaryData = System.Text.Json.JsonSerializer.Serialize(detailedData);
                                            entity.StagesProgressJson = SerializeStagesProgress(entity.StagesProgress);
                                            await SafeUpdateOnboardingAsync(entity);
                                        }
                                    }
                                });
                            }
                            catch { /* fire-and-forget */ }
                        }

                        // Retry: If placeholder exists for a while, attempt regeneration in background
                        if (stageProgress.IsCompleted &&
                            !string.IsNullOrWhiteSpace(stageProgress.AiSummary) &&
                            stageProgress.AiSummary.StartsWith("AI summary is being generated", StringComparison.OrdinalIgnoreCase))
                        {
                            var shouldRetry = !stageProgress.AiSummaryGeneratedAt.HasValue ||
                                              (DateTime.UtcNow - stageProgress.AiSummaryGeneratedAt.Value).TotalMinutes >= 1;
                            if (shouldRetry)
                            {
                                try
                                {
                                    var opts = new StageSummaryOptions
                                    {
                                        Language = "auto",
                                        SummaryLength = "short",
                                        IncludeTaskAnalysis = true,
                                        IncludeQuestionnaireInsights = true
                                    };
                                    _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                                    {
                                        var ai = await _stageService.GenerateAISummaryAsync(stageProgress.StageId, entity.Id, opts);
                                        if (ai != null && ai.Success)
                                        {
                                            LoadStagesProgressFromJson(entity);
                                            var sp = entity.StagesProgress?.FirstOrDefault(s => s.StageId == stageProgress.StageId);
                                            if (sp != null && sp.AiSummary != null && sp.AiSummary.StartsWith("AI summary is being generated", StringComparison.OrdinalIgnoreCase))
                                            {
                                                sp.AiSummary = ai.Summary;
                                                sp.AiSummaryGeneratedAt = DateTime.UtcNow;
                                                sp.AiSummaryConfidence = (decimal?)Convert.ToDecimal(ai.ConfidenceScore);
                                                sp.AiSummaryModel = ai.ModelUsed;
                                                var dd = new { ai.Breakdown, ai.KeyInsights, ai.Recommendations, ai.CompletionStatus, generatedAt = DateTime.UtcNow };
                                                sp.AiSummaryData = System.Text.Json.JsonSerializer.Serialize(dd);
                                                entity.StagesProgressJson = SerializeStagesProgress(entity.StagesProgress);
                                                await SafeUpdateOnboardingAsync(entity);
                                            }
                                        }
                                    });
                                }
                                catch { /* fire-and-forget */ }
                            }
                        }
                    }
                }

                // Set stage orders based on the order in workflow (sequential: 1, 2, 3, ...)
                var orderedStages = stages.OrderBy(s => s.Order).ToList();
                for (int i = 0; i < orderedStages.Count; i++)
                {
                    var stage = orderedStages[i];
                    var stageProgress = entity.StagesProgress.FirstOrDefault(sp => sp.StageId == stage.Id);
                    if (stageProgress != null)
                    {
                        stageProgress.StageOrder = i + 1; // Sequential order starting from 1
                    }
                }
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                // Don't throw exception here to avoid breaking the main flow
            }
        }

        /// <summary>
        /// Sync stages progress with workflow stages - handle new stages addition
        /// This method ensures that if workflow has new stages, they are added to stagesProgress.
        /// </summary>
        private async Task SyncStagesProgressWithWorkflowAsync(Onboarding entity)
        {
            try
            {
                // Get current workflow stages
                var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
                if (stages == null || !stages.Any())
                {
                    return;
                }


                // Load current stages progress
                LoadStagesProgressFromJson(entity);

                if (entity.StagesProgress == null)
                {
                    entity.StagesProgress = new List<OnboardingStageProgress>();
                }
                // 过滤无效的 stageId
                var validStageIds = stages.Select(s => s.Id).ToHashSet();
                entity.StagesProgress?.RemoveAll(x => !validStageIds.Contains(x.StageId));

                // Get existing stage IDs
                var existingStageIds = entity.StagesProgress.Select(sp => sp.StageId).ToHashSet();

                // Find new stages that are not in stagesProgress
                var newStages = stages.Where(s => !existingStageIds.Contains(s.Id)).ToList();

                if (newStages.Any())
                {
                    // Order all stages properly
                    var orderedStages = stages.OrderBy(s => s.Order).ToList();

                    foreach (var newStage in newStages)
                    {
                        // Find the position to insert
                        var stageIndex = orderedStages.FindIndex(s => s.Id == newStage.Id);
                        var sequentialOrder = stageIndex + 1;

                        var newStageProgress = new OnboardingStageProgress
                        {
                            StageId = newStage.Id,
                            Status = "Pending",
                            IsCompleted = false,
                            StartTime = null,
                            CompletionTime = null,
                            CompletedById = null,
                            CompletedBy = null,
                            Notes = null,
                            IsCurrent = false
                        };


                        // Insert at the correct position to maintain order
                        if (stageIndex < entity.StagesProgress.Count)
                        {
                            entity.StagesProgress.Insert(stageIndex, newStageProgress);
                        }
                        else
                        {
                            entity.StagesProgress.Add(newStageProgress);
                        }
                    }

                    // Serialize updated progress back to JSON (only progress fields)
                    entity.StagesProgressJson = SerializeStagesProgress(entity.StagesProgress);
                }
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
            }
        }

        /// <summary>
        /// Check if exception is related to JSONB type conversion error
        /// </summary>
        private static bool IsJsonbTypeError(Exception ex)
        {
            var errorMessage = ex.ToString().ToLower();
            return errorMessage.Contains("42804") ||
                   errorMessage.Contains("jsonb") ||
                   (errorMessage.Contains("column") && errorMessage.Contains("text") && errorMessage.Contains("expression")) ||
                   ex.GetType().Name.Contains("Postgres");
        }

        /// <summary>
        /// Safely append text to Notes field with length validation
        /// Ensures the total length doesn't exceed the database constraint (1000 characters)
        /// </summary>
        private static void SafeAppendToNotes(Onboarding entity, string noteText)
        {
            const int maxNotesLength = 1000;

            if (string.IsNullOrEmpty(noteText))
                return;

            var currentNotes = entity.Notes ?? string.Empty;
            var newContent = string.IsNullOrEmpty(currentNotes)
                ? noteText
                : $"{currentNotes}\n{noteText}";

            // If the new content exceeds the limit, truncate it intelligently
            if (newContent.Length > maxNotesLength)
            {
                // Try to keep the most recent notes by truncating from the beginning
                var truncationMessage = "[...truncated older notes...]\n";
                var availableSpace = maxNotesLength - truncationMessage.Length - noteText.Length - 1; // -1 for newline

                if (availableSpace > 0 && currentNotes.Length > availableSpace)
                {
                    // Keep the most recent part of existing notes
                    var recentNotes = currentNotes.Substring(currentNotes.Length - availableSpace);
                    // Find the first newline to avoid cutting in the middle of a note
                    var firstNewlineIndex = recentNotes.IndexOf('\n');
                    if (firstNewlineIndex > 0)
                    {
                        recentNotes = recentNotes.Substring(firstNewlineIndex + 1);
                    }
                    entity.Notes = $"{truncationMessage}{recentNotes}\n{noteText}";
                }
                else
                {
                    // If even the new note is too long, truncate it
                    var maxNewNoteLength = maxNotesLength - truncationMessage.Length - 1;
                    if (maxNewNoteLength > 0)
                    {
                        entity.Notes = $"{truncationMessage}{noteText.Substring(0, maxNewNoteLength)}";
                    }
                    else
                    {
                        // Fallback: just use the first part of the new note
                        entity.Notes = noteText.Substring(0, Math.Min(noteText.Length, maxNotesLength));
                    }
                }
            }
            else
            {
                entity.Notes = newContent;
            }
        }

        /// <summary>
        /// Safely update onboarding entity with JSONB compatibility
        /// This method handles the JSONB type conversion issue for stages_progress_json
        /// </summary>
        private async Task<bool> SafeUpdateOnboardingAsync(Onboarding entity)
        {
            try
            {
                LoggingExtensions.WriteLine($"[DEBUG] SafeUpdateOnboardingAsync - Updating Onboarding {entity.Id}: CurrentStageId={entity.CurrentStageId}, Status={entity.Status}");

                // Always use the JSONB-safe approach to avoid type conversion errors
                var db = _onboardingRepository.GetSqlSugarClient();

                // First, update permission JSONB fields AND permission mode/type together with explicit JSONB casting
                // This ensures that the database constraints are satisfied in a single transaction
                try
                {
                    var permissionSql = @"
                        UPDATE ff_onboarding 
                        SET view_teams = @ViewTeams::jsonb,
                            view_users = @ViewUsers::jsonb,
                            operate_teams = @OperateTeams::jsonb,
                            operate_users = @OperateUsers::jsonb,
                            view_permission_mode = @ViewPermissionMode,
                            view_permission_subject_type = @ViewPermissionSubjectType,
                            operate_permission_subject_type = @OperatePermissionSubjectType,
                            use_same_team_for_operate = @UseSameTeamForOperate
                        WHERE id = @Id";

                    await db.Ado.ExecuteCommandAsync(permissionSql, new
                    {
                        ViewTeams = entity.ViewTeams,
                        ViewUsers = entity.ViewUsers,
                        OperateTeams = entity.OperateTeams,
                        OperateUsers = entity.OperateUsers,
                        ViewPermissionMode = (int)entity.ViewPermissionMode,
                        ViewPermissionSubjectType = (int)entity.ViewPermissionSubjectType,
                        OperatePermissionSubjectType = (int)entity.OperatePermissionSubjectType,
                        UseSameTeamForOperate = entity.UseSameTeamForOperate,
                        Id = entity.Id
                    });
                }
                catch (Exception permEx)
                {
                    LoggingExtensions.WriteLine($"Error: Failed to update permission JSONB fields: {permEx.Message}");
                    // This is critical, so we should throw
                    throw new CRMException($"Failed to update permission fields: {permEx.Message}");
                }

                // Then update all other fields (permission fields were already updated above)
                // Now the constraint will be satisfied because JSONB fields and permission modes are already updated
                LoggingExtensions.WriteLine($"[DEBUG] SafeUpdateOnboardingAsync - About to update repository with CurrentStageId={entity.CurrentStageId}");

                // Serialize back to JSON (only progress fields)
                await FilterValidStagesProgress(entity);
                var result = await _onboardingRepository.UpdateAsync(entity,
                    it => new
                    {
                        it.WorkflowId,
                        it.CurrentStageId,
                        it.CurrentStageOrder,
                        it.LeadId,
                        it.LeadName,
                        it.LeadEmail,
                        it.LeadPhone,
                        it.ContactPerson,
                        it.ContactEmail,
                        it.LifeCycleStageId,
                        it.LifeCycleStageName,
                        it.Status,
                        it.CompletionRate,
                        it.StartDate,
                        it.EstimatedCompletionDate,
                        it.ActualCompletionDate,
                        it.CurrentAssigneeId,
                        it.CurrentAssigneeName,
                        it.CurrentTeam,
                        it.StageUpdatedById,
                        it.StageUpdatedBy,
                        it.StageUpdatedByEmail,
                        it.StageUpdatedTime,
                        it.CurrentStageStartTime,
                        it.Priority,
                        it.IsPrioritySet,
                        it.Ownership,
                        it.OwnershipName,
                        it.OwnershipEmail,
                        it.CustomFieldsJson,
                        it.Notes,
                        it.IsActive,
                        // Note: ViewPermissionSubjectType, OperatePermissionSubjectType, ViewPermissionMode were already updated above
                        // Note: ViewTeams, ViewUsers, OperateTeams, OperateUsers were already updated above
                        it.ModifyDate,
                        it.ModifyBy,
                        it.ModifyUserId,
                        it.IsValid
                    });

                // Update stages_progress_json separately with explicit JSONB casting
                if (!string.IsNullOrEmpty(entity.StagesProgressJson))
                {
                    try
                    {
                        var progressSql = "UPDATE ff_onboarding SET stages_progress_json = @StagesProgressJson::jsonb WHERE id = @Id";
                        await db.Ado.ExecuteCommandAsync(progressSql, new
                        {
                            StagesProgressJson = entity.StagesProgressJson,
                            Id = entity.Id
                        });
                    }
                    catch (Exception progressEx)
                    {
                        // Log but don't fail the main update
                        LoggingExtensions.WriteLine($"Warning: Failed to update stages_progress_json: {progressEx.Message}");
                        // Try alternative approach with parameter substitution
                        try
                        {
                            var escapedJson = entity.StagesProgressJson.Replace("'", "''");
                            var directSql = $"UPDATE ff_onboarding SET stages_progress_json = '{escapedJson}'::jsonb WHERE id = {entity.Id}";
                            await db.Ado.ExecuteCommandAsync(directSql);
                        }
                        catch (Exception directEx)
                        {
                            LoggingExtensions.WriteLine($"Error: Both parameterized and direct JSONB update failed: {directEx.Message}");
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new CRMException(ErrorCodeEnum.SystemError,
                    $"Failed to safely update onboarding: {ex.Message}");
            }
        }

        /// <summary>
        /// Ensure stages progress is properly initialized and synced with workflow
        /// This method handles cases where stages progress might be empty or outdated
        /// </summary>
        private static readonly HashSet<long> _initializingEntities = new HashSet<long>();
        private static readonly object _initializationLock = new object();

        private async Task EnsureStagesProgressInitializedAsync(Onboarding entity)
        {
            // Prevent infinite recursion using thread-safe entity tracking
            lock (_initializationLock)
            {
                if (_initializingEntities.Contains(entity.Id))
                {
                    return; // Already being initialized, avoid recursion
                }
                _initializingEntities.Add(entity.Id);
            }

            try
            {
                // Load current stages progress
                LoadStagesProgressFromJson(entity);

                // Get current workflow stages
                var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
                if (stages == null || !stages.Any())
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, "No stages found for workflow");
                }

                // If stages progress is empty, initialize it
                if (entity.StagesProgress == null || !entity.StagesProgress.Any())
                {
                    await InitializeStagesProgressAsync(entity, stages);
                }
                else
                {
                    // Sync with workflow stages (handle new stages addition)
                    await SyncStagesProgressWithWorkflowAsync(entity);
                }

                // Always enrich with stage data to ensure consistency
                await EnrichStagesProgressWithStageDataAsync(entity);

                // NOTE: Do NOT call SafeUpdateOnboardingAsync here to avoid recursion
                // The caller is responsible for saving changes
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid breaking the main flow
                // The validation will catch if stages progress is still missing
            }
            finally
            {
                // Remove from initialization tracking
                lock (_initializationLock)
                {
                    _initializingEntities.Remove(entity.Id);
                }
            }
        }

        /// <summary>
        /// Serialize stages progress to JSON - only stores progress state, not stage configuration
        /// Stage configuration fields (stageName, stageOrder, etc.) are excluded via JsonIgnore attributes
        /// and are populated dynamically from Stage entities when needed.
        /// </summary>
        private string SerializeStagesProgress(List<OnboardingStageProgress> stagesProgress)
        {
            try
            {
                if (stagesProgress == null || !stagesProgress.Any())
                {
                    return "[]";
                }

                // Debug: Check input data before serialization
                LoggingExtensions.WriteLine($"[DEBUG] SerializeStagesProgress - Input data:");
                foreach (var sp in stagesProgress)
                {
                    LoggingExtensions.WriteLine($"[DEBUG] Input StageProgress: StageId={sp.StageId}, Status={sp.Status}, IsCurrent={sp.IsCurrent}");
                }

                // Serialize OnboardingStageProgress objects with JsonIgnore attributes respected
                // Only progress-related fields will be included, not stage configuration fields
                var result = System.Text.Json.JsonSerializer.Serialize(stagesProgress, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                // Debug: Check final result
                LoggingExtensions.WriteLine($"[DEBUG] SerializeStagesProgress - Final JSON result:");
                LoggingExtensions.WriteLine($"[DEBUG] {result}");

                return result;
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                return "[]";
            }
        }

        /// <summary>
        /// Create default UserInvitation record without sending email
        /// </summary>
        /// <param name="onboarding">Onboarding entity</param>
        private async Task CreateDefaultUserInvitationAsync(Onboarding onboarding)
        {
            try
            {
                // Determine which email to use (prefer ContactEmail, fallback to LeadEmail)
                var emailToUse = !string.IsNullOrWhiteSpace(onboarding.ContactEmail)
                    ? onboarding.ContactEmail
                    : onboarding.LeadEmail;

                // Skip if no email is available
                if (string.IsNullOrWhiteSpace(emailToUse))
                {
                    return;
                }

                // Check if invitation already exists for this onboarding and email
                var existingInvitation = await _userInvitationRepository.GetByEmailAndOnboardingIdAsync(emailToUse, onboarding.Id);
                if (existingInvitation != null)
                {
                    // Invitation already exists, skip creation
                    return;
                }

                // Create new UserInvitation record
                var invitation = new UserInvitation
                {
                    OnboardingId = onboarding.Id,
                    Email = emailToUse,
                    InvitationToken = CryptoHelper.GenerateSecureToken(),
                    Status = "Pending",
                    SentDate = GetCurrentTimeWithTimeZone(),
                    TokenExpiry = null, // No expiry
                    SendCount = 0, // Not sent via email
                    TenantId = onboarding.TenantId,
                    Notes = "Auto-created default invitation (no email sent)"
                };

                // Generate short URL ID and invitation URL
                invitation.ShortUrlId = CryptoHelper.GenerateShortUrlId(
                    onboarding.Id,
                    emailToUse,
                    invitation.InvitationToken);

                // Generate invitation URL (using default base URL)
                invitation.InvitationUrl = GenerateShortInvitationUrl(
                    invitation.ShortUrlId,
                    onboarding.TenantId ?? "DEFAULT",
                    onboarding.AppCode ?? "DEFAULT");

                // Initialize create info
                invitation.InitCreateInfo(_userContext);

                // Insert the invitation record
                await _userInvitationRepository.InsertAsync(invitation);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the onboarding creation
                // This is a non-critical operation
                // Note: In a real implementation, you would use structured logging here
            }
        }

        /// <summary>
        /// Get current time with +08:00 timezone (China Standard Time)
        /// </summary>
        /// <returns>Current time with +08:00 offset</returns>
        private DateTimeOffset GetCurrentTimeWithTimeZone()
        {
            // China Standard Time is UTC+8
            var chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
            var utcNow = DateTime.UtcNow;
            var chinaTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, chinaTimeZone);

            // Create DateTimeOffset with +08:00 offset
            return new DateTimeOffset(chinaTime, TimeSpan.FromHours(8));
        }

        /// <summary>
        /// Generate short invitation URL (copied from UserInvitationService)
        /// </summary>
        /// <param name="shortUrlId">Short URL ID</param>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="appCode">App code</param>
        /// <param name="baseUrl">Base URL (optional)</param>
        /// <returns>Generated invitation URL</returns>
        private string GenerateShortInvitationUrl(string shortUrlId, string tenantId, string appCode, string? baseUrl = null)
        {
            // Use provided base URL or fall back to a default one
            var effectiveBaseUrl = baseUrl ?? "https://portal.flowflex.com"; // Default base URL

            // Generate the short URL format: {baseUrl}/portal/{tenantId}/{appCode}/invite/{shortUrlId}
            return $"{effectiveBaseUrl.TrimEnd('/')}/portal/{tenantId}/{appCode}/invite/{shortUrlId}";
        }
        /// <summary>
        /// Update AI Summary for a specific stage in onboarding's stagesProgress
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="aiSummary">AI Summary content</param>
        /// <param name="generatedAt">Generated timestamp</param>
        /// <param name="confidence">Confidence score</param>
        /// <param name="modelUsed">AI model used</param>
        /// <returns>Success status</returns>
        public async Task<bool> UpdateOnboardingStageAISummaryAsync(long onboardingId, long stageId, string aiSummary, DateTime generatedAt, double? confidence, string modelUsed)
        {
            try
            {
                // Get current onboarding
                var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
                if (onboarding == null)
                {
                    LoggingExtensions.WriteLine($"Onboarding {onboardingId} not found for AI summary update");
                    return false;
                }

                // Sync stages progress with workflow to ensure all stages are included
                await SyncStagesProgressWithWorkflowAsync(onboarding);

                // Load stages progress from JSON (after sync)
                LoadStagesProgressFromJson(onboarding);

                // Find the stage progress entry
                var stageProgress = onboarding.StagesProgress?.FirstOrDefault(sp => sp.StageId == stageId);
                if (stageProgress == null)
                {
                    LoggingExtensions.WriteLine($"Stage progress not found for stage {stageId} in onboarding {onboardingId} even after sync. Available stages: {string.Join(", ", onboarding.StagesProgress?.Select(sp => sp.StageId.ToString()) ?? Array.Empty<string>())}");
                    return false;
                }

                // Update AI summary fields - always overwrite for Onboarding-specific summaries
                stageProgress.AiSummary = aiSummary;
                stageProgress.AiSummaryGeneratedAt = generatedAt;
                stageProgress.AiSummaryConfidence = (decimal?)confidence;
                stageProgress.AiSummaryModel = modelUsed;
                stageProgress.AiSummaryData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    trigger = "Stream API onboarding update",
                    generatedAt = generatedAt,
                    confidence = confidence,
                    model = modelUsed,
                    onboardingSpecific = true
                });

                // Save stages progress back to JSON
                onboarding.StagesProgressJson = SerializeStagesProgress(onboarding.StagesProgress);

                // Update in database
                var result = await SafeUpdateOnboardingAsync(onboarding);

                if (result)
                {
                    LoggingExtensions.WriteLine($"✅ Successfully updated AI summary for stage {stageId} in onboarding {onboardingId}");
                }
                else
                {
                    LoggingExtensions.WriteLine($"❌ Failed to save AI summary for stage {stageId} in onboarding {onboardingId} - database update failed");
                }

                return result;
            }
            catch (Exception ex)
            {
                LoggingExtensions.WriteLine($"❌ Failed to update AI summary for stage {stageId} in onboarding {onboardingId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Update custom fields for a specific stage in onboarding's stagesProgress
        /// Updates CustomEstimatedDays and CustomEndTime fields
        /// </summary>
        public async Task<bool> UpdateStageCustomFieldsAsync(long onboardingId, UpdateStageCustomFieldsInputDto input)
        {
            try
            {
                // Get current onboarding
                var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
                if (onboarding == null)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
                }

                // Load stages progress from JSON
                LoadStagesProgressFromJson(onboarding);

                // Find the stage progress entry
                var stageProgress = onboarding.StagesProgress?.FirstOrDefault(sp => sp.StageId == input.StageId);
                if (stageProgress == null)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, $"Stage {input.StageId} not found in onboarding {onboardingId}");
                }

                // Capture original values for comparison
                var originalEstimatedDays = stageProgress.CustomEstimatedDays;
                var originalEndTime = stageProgress.CustomEndTime;

                // Capture before data for change log
                var beforeData = JsonSerializer.Serialize(new
                {
                    CustomEstimatedDays = stageProgress.CustomEstimatedDays,
                    CustomEndTime = stageProgress.CustomEndTime,
                    LastUpdatedTime = stageProgress.LastUpdatedTime,
                    LastUpdatedBy = stageProgress.LastUpdatedBy
                });

                // Update custom fields
                stageProgress.CustomEstimatedDays = input.CustomEstimatedDays;
                stageProgress.CustomEndTime = input.CustomEndTime;

                // Add notes if provided
                if (!string.IsNullOrEmpty(input.Notes))
                {
                    var currentTime = DateTimeOffset.UtcNow;
                    var currentUser = GetCurrentUserName();
                    var updateNote = $"[Custom fields updated {currentTime:yyyy-MM-dd HH:mm:ss} by {currentUser}] {input.Notes}";

                    if (string.IsNullOrEmpty(stageProgress.Notes))
                    {
                        stageProgress.Notes = updateNote;
                    }
                    else
                    {
                        stageProgress.Notes += $"\n{updateNote}";
                    }
                }

                // Update last modified fields
                stageProgress.LastUpdatedTime = DateTimeOffset.UtcNow;
                stageProgress.LastUpdatedBy = GetCurrentUserName();

                // Capture after data for change log
                var afterData = JsonSerializer.Serialize(new
                {
                    CustomEstimatedDays = stageProgress.CustomEstimatedDays,
                    CustomEndTime = stageProgress.CustomEndTime,
                    LastUpdatedTime = stageProgress.LastUpdatedTime,
                    LastUpdatedBy = stageProgress.LastUpdatedBy
                });

                // Save stages progress back to JSON
                onboarding.StagesProgressJson = SerializeStagesProgress(onboarding.StagesProgress);

                // Update in database
                var result = await SafeUpdateOnboardingAsync(onboarding);

                // Log the operation if update was successful
                if (result)
                {
                    var changedFields = new List<string>();
                    var changeDetails = new List<string>();

                    // Check for actual changes in CustomEstimatedDays
                    if (input.CustomEstimatedDays.HasValue && originalEstimatedDays != input.CustomEstimatedDays)
                    {
                        changedFields.Add("CustomEstimatedDays");
                        var beforeValue = originalEstimatedDays?.ToString() ?? "null";
                        changeDetails.Add($"EstimatedDays: {beforeValue} → {input.CustomEstimatedDays}");
                    }

                    // Check for actual changes in CustomEndTime
                    if (input.CustomEndTime.HasValue && originalEndTime != input.CustomEndTime)
                    {
                        changedFields.Add("CustomEndTime");
                        var beforeValue = originalEndTime?.ToString("yyyy-MM-dd HH:mm") ?? "null";
                        changeDetails.Add($"EndTime: {beforeValue} → {input.CustomEndTime?.ToString("yyyy-MM-dd HH:mm")}");
                    }

                    // Check if notes were added
                    if (!string.IsNullOrEmpty(input.Notes))
                    {
                        changedFields.Add("Notes");
                        changeDetails.Add("Notes: Added");
                    }

                    // Only log if there are actual changes or notes added
                    if (changeDetails.Any())
                    {
                        var operationTitle = $"Update Stage Custom Fields: {string.Join(", ", changeDetails)}";
                        var operationDescription = $"Updated custom fields for stage {input.StageId} in onboarding {onboardingId}";

                        // Log the stage custom fields update operation
                        await _operationChangeLogService.LogOperationAsync(
                            operationType: FlowFlex.Domain.Shared.Enums.OW.OperationTypeEnum.StageUpdate,
                            businessModule: BusinessModuleEnum.Stage,
                            businessId: input.StageId,
                            onboardingId: onboardingId,
                            stageId: input.StageId,
                            operationTitle: operationTitle,
                            operationDescription: operationDescription,
                            beforeData: beforeData,
                            afterData: afterData,
                            changedFields: changedFields,
                            extendedData: JsonSerializer.Serialize(new
                            {
                                Notes = input.Notes,
                                OperationSource = "UpdateStageCustomFieldsAsync",
                                HasActualChanges = true
                            })
                        );
                    }
                    // Note: No log entry is created when there are no actual changes
                    // This reduces log noise and focuses on meaningful operations
                }

                return result;
            }
            catch (Exception ex)
            {
                // Log the failed operation
                await _operationChangeLogService.LogOperationAsync(
                    operationType: FlowFlex.Domain.Shared.Enums.OW.OperationTypeEnum.StageUpdate,
                    businessModule: BusinessModuleEnum.Stage,
                    businessId: input.StageId,
                    onboardingId: onboardingId,
                    stageId: input.StageId,
                    operationTitle: "Update Stage Custom Fields",
                    operationDescription: $"Failed to update custom fields for stage {input.StageId} in onboarding {onboardingId}",
                    operationStatus: OperationStatusEnum.Failed,
                    errorMessage: ex.Message
                );

                throw new CRMException(ErrorCodeEnum.SystemError, $"Failed to update custom fields for stage {input.StageId} in onboarding {onboardingId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Save a specific stage in onboarding's stagesProgress
        /// Updates the stage's IsSaved, SaveTime, and SavedById fields
        /// </summary>
        public async Task<bool> SaveStageAsync(long onboardingId, long stageId)
        {
            // Check permission
            if (!await CheckCaseOperatePermissionAsync(onboardingId))
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed,
                    $"User does not have permission to operate on case {onboardingId}");
            }

            try
            {
                // Get current onboarding
                var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
                if (onboarding == null)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
                }

                // Load stages progress from JSON
                LoadStagesProgressFromJson(onboarding);

                // Find the stage progress entry
                var stageProgress = onboarding.StagesProgress?.FirstOrDefault(sp => sp.StageId == stageId);
                if (stageProgress == null)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, $"Stage {stageId} not found in onboarding {onboardingId}");
                }

                // Update save fields
                stageProgress.IsSaved = true;
                stageProgress.SaveTime = DateTimeOffset.UtcNow;
                stageProgress.SavedById = GetCurrentUserId()?.ToString();
                stageProgress.SavedBy = GetCurrentUserName();

                // Set StartTime if not already set (only during save operations)
                // This ensures StartTime is only set when user actually saves or completes work
                if (!stageProgress.StartTime.HasValue)
                {
                    stageProgress.StartTime = DateTimeOffset.UtcNow;
                }

                // IMPORTANT: If this is the current stage and CurrentStageStartTime is not set, set it now
                if (stageProgress.StageId == onboarding.CurrentStageId && !onboarding.CurrentStageStartTime.HasValue)
                {
                    onboarding.CurrentStageStartTime = stageProgress.StartTime;
                    LoggingExtensions.WriteLine($"[DEBUG] SaveStageAsync - Set CurrentStageStartTime to {onboarding.CurrentStageStartTime} for Stage {stageId}");
                }

                // Save stages progress back to JSON
                await FilterValidStagesProgress(onboarding);
                onboarding.StagesProgressJson = SerializeStagesProgress(onboarding.StagesProgress);

                // Update in database
                var result = await SafeUpdateOnboardingAsync(onboarding);

                return result;
            }
            catch (Exception ex)
            {
                throw new CRMException(ErrorCodeEnum.SystemError, $"Failed to save stage {stageId} in onboarding {onboardingId}: {ex.Message}");
            }
        }

        /// <summary>
        /// 过滤无效的 stagesProgress，保留当前 workflow 中有效 stage 的进度
        /// </summary>
        private async Task FilterValidStagesProgress(Onboarding entity)
        {
            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            if (stages == null || !stages.Any()) return;
            var validStageIds = stages.Select(s => s.Id).ToHashSet();
            entity.StagesProgress?.RemoveAll(x => !validStageIds.Contains(x.StageId));
        }

        /// <summary>
        /// Helper method to populate workflow/stage names and calculate current stage end time for OnboardingOutputDto lists
        /// </summary>
        /// <summary>
        /// Check if current user has permission to operate on a case
        /// </summary>
        private async Task<bool> CheckCaseOperatePermissionAsync(long caseId)
        {
            var userId = _userContext?.UserId;
            if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var userIdLong))
            {
                return false;
            }

            var permissionResult = await _permissionService.CheckCaseAccessAsync(
                userIdLong,
                caseId,
                PermissionOperationType.Operate);

            return permissionResult.Success && permissionResult.CanOperate;
        }

        private async Task PopulateOnboardingOutputDtoAsync(List<OnboardingOutputDto> results, List<Onboarding> entities)
        {
            if (!results.Any() || !entities.Any()) return;

            // Auto-generate CaseCode for legacy data (batch processing)
            var entitiesWithoutCaseCode = entities.Where(e => string.IsNullOrWhiteSpace(e.CaseCode)).ToList();
            if (entitiesWithoutCaseCode.Any())
            {
                foreach (var entity in entitiesWithoutCaseCode)
                {
                    await EnsureCaseCodeAsync(entity);
                }
            }

            // Batch get related data to avoid N+1 queries
            var (workflows, stages) = await GetRelatedDataBatchOptimizedAsync(entities);
            var workflowDict = workflows.ToDictionary(w => w.Id, w => w.Name);
            var stageDict = stages.ToDictionary(s => s.Id, s => s.Name);
            var stageEstimatedDaysDict = stages.ToDictionary(s => s.Id, s => s.EstimatedDuration);

            // 🚀 PERFORMANCE OPTIMIZATION: Batch check permissions using in-memory data (no DB queries)
            var userId = _userContext?.UserId;
            bool isSystemAdmin = _userContext?.IsSystemAdmin == true;
            bool isTenantAdmin = _userContext != null && _userContext.HasAdminPrivileges(_userContext.TenantId);
            Dictionary<long, PermissionInfoDto> permissions = null;

            if (!string.IsNullOrEmpty(userId) && long.TryParse(userId, out var userIdLong))
            {
                // Pre-check module permissions once (batch optimization)
                bool canViewCases = await _permissionService.CheckGroupPermissionAsync(userIdLong, PermissionConsts.Case.Read);
                bool canOperateCases = await _permissionService.CheckGroupPermissionAsync(userIdLong, PermissionConsts.Case.Update);

                // Get user teams once (avoid repeated calls)
                var userTeams = _permissionService.GetUserTeamIds();
                var userTeamLongs = userTeams?.Select(t => long.TryParse(t, out var tid) ? tid : 0).Where(t => t > 0).ToList() ?? new List<long>();

                // 🔄 Use unified CasePermissionService for permission checks
                permissions = new Dictionary<long, PermissionInfoDto>();

                // Create entity lookup for fast access
                var entityDict = entities.ToDictionary(e => e.Id);

                foreach (var result in results)
                {
                    if (!entityDict.TryGetValue(result.Id, out var entity))
                    {
                        // Fallback: entity not found, deny access
                        permissions[result.Id] = new PermissionInfoDto { CanView = false, CanOperate = false };
                        continue;
                    }

                    // ✅ Use unified CasePermissionService - includes admin bypass
                    var viewResult = await _casePermissionService.CheckCasePermissionAsync(
                        entity, userIdLong, PermissionOperationType.View);

                    bool canOperateCase = false;
                    if (viewResult.Success && viewResult.CanView)
                    {
                        var operateResult = await _casePermissionService.CheckCasePermissionAsync(
                            entity, userIdLong, PermissionOperationType.Operate);
                        canOperateCase = operateResult.Success && operateResult.CanOperate;
                    }

                    permissions[result.Id] = new PermissionInfoDto
                    {
                        CanView = canViewCases && viewResult.Success && viewResult.CanView,
                        CanOperate = canOperateCases && canOperateCase
                    };
                }
            }

            // Populate workflow and stage names, calculate current stage end time, and check permissions
            foreach (var result in results)
            {
                result.WorkflowName = workflowDict.GetValueOrDefault(result.WorkflowId);

                // IMPORTANT: If CurrentStageId is null but stagesProgress exists, try to get current stage from stagesProgress
                if (!result.CurrentStageId.HasValue && result.StagesProgress != null && result.StagesProgress.Any())
                {
                    var currentStageProgress = result.StagesProgress.FirstOrDefault(sp => sp.IsCurrent);
                    if (currentStageProgress != null)
                    {
                        result.CurrentStageId = currentStageProgress.StageId;
                        LoggingExtensions.WriteLine($"[DEBUG] PopulateOnboardingOutputDto - Recovered CurrentStageId from StagesProgress: {result.CurrentStageId} for Onboarding {result.Id}");
                    }
                }

                // currentStageStartTime 只取 startTime（无则为null）
                result.CurrentStageStartTime = null;
                result.CurrentStageEndTime = null;
                double? estimatedDays = null;
                if (result.CurrentStageId.HasValue && result.StagesProgress != null && result.StagesProgress.Any())
                {
                    var currentStageProgress = result.StagesProgress.FirstOrDefault(sp => sp.StageId == result.CurrentStageId.Value);
                    if (currentStageProgress != null)
                    {
                        if (currentStageProgress.StartTime.HasValue)
                        {
                            result.CurrentStageStartTime = currentStageProgress.StartTime;
                        }
                        // currentStageEndTime 优先级: customEndTime > endTime > (startTime+estimatedDays) > null
                        if (currentStageProgress.CustomEndTime.HasValue)
                        {
                            result.CurrentStageEndTime = currentStageProgress.CustomEndTime.Value;
                        }
                        else if (currentStageProgress.EndTime.HasValue)
                        {
                            result.CurrentStageEndTime = currentStageProgress.EndTime.Value;
                        }
                        else
                        {
                            // 三级优先：json.customEstimatedDays > json.estimatedDays > stageDict
                            estimatedDays = (double?)currentStageProgress.CustomEstimatedDays;
                            if (!estimatedDays.HasValue || estimatedDays.Value <= 0)
                            {
                                estimatedDays = (double?)currentStageProgress.EstimatedDays;
                                if ((!estimatedDays.HasValue || estimatedDays.Value <= 0) && result.CurrentStageId.HasValue)
                                {
                                    // stageEstimatedDaysDict key: stageId -> EstimatedDuration
                                    if (stageEstimatedDaysDict.TryGetValue(result.CurrentStageId.Value, out var val) && val != null && val > 0)
                                    {
                                        estimatedDays = (double?)val;
                                    }
                                }
                            }
                        }
                    }
                }
                // 单独推算 currentStageEndTime——仅当startTime和estimatedDays都存在
                if (result.CurrentStageEndTime == null && result.CurrentStageStartTime.HasValue && (estimatedDays.HasValue && estimatedDays.Value > 0))
                {
                    result.CurrentStageEndTime = result.CurrentStageStartTime.Value.AddDays(estimatedDays.Value);
                }

                if (result.CurrentStageId.HasValue)
                {
                    result.CurrentStageName = stageDict.GetValueOrDefault(result.CurrentStageId.Value);

                    // Fallback: if name missing from dictionary, fetch directly
                    if (string.IsNullOrEmpty(result.CurrentStageName))
                    {
                        try
                        {
                            var stageNameFallback = await _stageRepository.GetByIdAsync(result.CurrentStageId.Value);
                            if (!string.IsNullOrEmpty(stageNameFallback?.Name))
                            {
                                result.CurrentStageName = stageNameFallback.Name;
                                LoggingExtensions.WriteLine($"[DEBUG] PopulateOnboardingOutputDto - Fallback CurrentStageName='{result.CurrentStageName}' for Onboarding {result.Id}");
                            }
                        }
                        catch { }
                    }

                    // IMPORTANT: Priority for EstimatedDays: customEstimatedDays > stage.EstimatedDuration
                    var currentStageProgress = result.StagesProgress?.FirstOrDefault(sp => sp.StageId == result.CurrentStageId.Value);
                    if (currentStageProgress != null && currentStageProgress.CustomEstimatedDays.HasValue && currentStageProgress.CustomEstimatedDays.Value > 0)
                    {
                        result.CurrentStageEstimatedDays = currentStageProgress.CustomEstimatedDays.Value;
                    }
                    else
                    {
                        result.CurrentStageEstimatedDays = stageEstimatedDaysDict.GetValueOrDefault(result.CurrentStageId.Value);
                    }

                    // Fallback: if still missing, fetch stage directly
                    if ((!result.CurrentStageEstimatedDays.HasValue || result.CurrentStageEstimatedDays.Value <= 0) && result.CurrentStageId.HasValue)
                    {
                        try
                        {
                            var stageFallback = await _stageRepository.GetByIdAsync(result.CurrentStageId.Value);
                            if (stageFallback?.EstimatedDuration != null && stageFallback.EstimatedDuration > 0)
                            {
                                result.CurrentStageEstimatedDays = stageFallback.EstimatedDuration;
                                LoggingExtensions.WriteLine($"[DEBUG] PopulateOnboardingOutputDto - Fallback EstimatedDays from Stage fetch: {result.CurrentStageEstimatedDays} for Onboarding {result.Id}");
                            }
                        }
                        catch { }
                    }

                    // End time already derived strictly from stagesProgress above
                }
                else
                {
                    // Log when CurrentStageId is still null after fallback attempt
                    LoggingExtensions.WriteLine($"[WARNING] PopulateOnboardingOutputDto - CurrentStageId is null for Onboarding {result.Id}, StagesProgress count: {result.StagesProgress?.Count ?? 0}");
                }

                // Set IsDisabled and Permission fields
                if (isSystemAdmin)
                {
                    result.IsDisabled = false; // System Admin can operate on all cases
                    result.Permission = new Application.Contracts.Dtos.OW.Permission.PermissionInfoDto
                    {
                        CanView = true,
                        CanOperate = true,
                        ErrorMessage = null
                    };
                }
                else if (isTenantAdmin)
                {
                    result.IsDisabled = false; // Tenant Admin can operate on all cases in their tenant
                    result.Permission = new Application.Contracts.Dtos.OW.Permission.PermissionInfoDto
                    {
                        CanView = true,
                        CanOperate = true,
                        ErrorMessage = null
                    };
                }
                else if (permissions != null && permissions.ContainsKey(result.Id))
                {
                    result.Permission = permissions[result.Id];
                    result.IsDisabled = !result.Permission.CanOperate;
                }
                else
                {
                    result.IsDisabled = true; // No user context, disable by default
                    result.Permission = new Application.Contracts.Dtos.OW.Permission.PermissionInfoDto
                    {
                        CanView = false,
                        CanOperate = false,
                        ErrorMessage = "User not authenticated"
                    };
                }
            }
        }

        #region New Status Management Methods

        /// <summary>
        /// Start onboarding (activate an inactive onboarding)
        /// </summary>
        public async Task<bool> StartOnboardingAsync(long id, StartOnboardingInputDto input)
        {
            // Check permission
            if (!await CheckCaseOperatePermissionAsync(id))
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed,
                    $"User does not have permission to operate on case {id}");
            }

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            // Validate current status - only Inactive onboardings can be started
            if (entity.Status != "Inactive")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"Cannot start onboarding. Current status is '{entity.Status}'. Only 'Inactive' onboardings can be started.");
            }

            // Preserve the original stages_progress_json to avoid any modifications
            var originalStagesProgressJson = entity.StagesProgressJson;

            // Update status to Active
            entity.Status = "Active";
            entity.StartDate = DateTimeOffset.UtcNow;

            // IMPORTANT: Set CurrentStageStartTime when starting onboarding
            // This marks the beginning of the current stage timeline
            entity.CurrentStageStartTime = DateTimeOffset.UtcNow;

            // Reset progress if requested
            if (input.ResetProgress)
            {
                entity.CurrentStageId = null;
                entity.CurrentStageOrder = 0;
                entity.CompletionRate = 0;
            }

            // Add start notes
            var startText = $"[Start Onboarding] Onboarding activated";
            if (!string.IsNullOrEmpty(input.Reason))
            {
                startText += $" - Reason: {input.Reason}";
            }
            if (!string.IsNullOrEmpty(input.Notes))
            {
                startText += $" - Notes: {input.Notes}";
            }

            SafeAppendToNotes(entity, startText);

            // Update stage tracking info (without modifying stagesProgress)
            entity.StageUpdatedTime = DateTimeOffset.UtcNow;
            entity.StageUpdatedBy = _operatorContextService.GetOperatorDisplayName();
            entity.StageUpdatedById = _operatorContextService.GetOperatorId();
            entity.StageUpdatedByEmail = GetCurrentUserEmail();

            // Use SafeUpdateOnboardingWithoutStagesProgressAsync to preserve stagesProgress
            return await SafeUpdateOnboardingWithoutStagesProgressAsync(entity, originalStagesProgressJson);
        }

        /// <summary>
        /// Abort onboarding (terminate the process)
        /// </summary>
        public async Task<bool> AbortAsync(long id, AbortOnboardingInputDto input)
        {
            // Check permission
            if (!await CheckCaseOperatePermissionAsync(id))
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed,
                    $"User does not have permission to operate on case {id}");
            }

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            // Validate current status - cannot abort already completed or aborted onboardings
            if (entity.Status == "Completed" || entity.Status == "Aborted")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"Cannot abort onboarding with status '{entity.Status}'");
            }

            // Preserve the original stages_progress_json to avoid any modifications
            var originalStagesProgressJson = entity.StagesProgressJson;

            // Update status to Aborted
            entity.Status = "Aborted";
            entity.EstimatedCompletionDate = null; // Remove ETA

            // Add abort notes
            var abortText = $"[Abort] Onboarding terminated - Reason: {input.Reason}";
            if (!string.IsNullOrEmpty(input.Notes))
            {
                abortText += $" - Notes: {input.Notes}";
            }

            SafeAppendToNotes(entity, abortText);

            // Update stage tracking info (without modifying stagesProgress)
            entity.StageUpdatedTime = DateTimeOffset.UtcNow;
            entity.StageUpdatedBy = _operatorContextService.GetOperatorDisplayName();
            entity.StageUpdatedById = _operatorContextService.GetOperatorId();
            entity.StageUpdatedByEmail = GetCurrentUserEmail();

            // Use SafeUpdateOnboardingWithoutStagesProgressAsync to preserve stagesProgress
            return await SafeUpdateOnboardingWithoutStagesProgressAsync(entity, originalStagesProgressJson);
        }

        /// <summary>
        /// Reactivate onboarding (restart an aborted onboarding)
        /// </summary>
        public async Task<bool> ReactivateAsync(long id, ReactivateOnboardingInputDto input)
        {
            // Check permission
            if (!await CheckCaseOperatePermissionAsync(id))
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed,
                    $"User does not have permission to operate on case {id}");
            }

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            // Validate current status - only Aborted onboardings can be reactivated
            if (entity.Status != "Aborted")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"Cannot reactivate onboarding. Current status is '{entity.Status}'. Only 'Aborted' onboardings can be reactivated.");
            }

            // Preserve the original stages_progress_json to avoid any modifications
            var originalStagesProgressJson = entity.StagesProgressJson;

            // Update status to Active
            entity.Status = "Active";
            entity.ActualCompletionDate = null;

            // Update stage tracking info (without modifying stagesProgress)
            entity.StageUpdatedTime = DateTimeOffset.UtcNow;
            entity.StageUpdatedBy = _operatorContextService.GetOperatorDisplayName();
            entity.StageUpdatedById = _operatorContextService.GetOperatorId();
            entity.StageUpdatedByEmail = GetCurrentUserEmail();

            // Add reactivation notes
            var reactivateText = $"[Reactivate] Onboarding reactivated from Aborted status - Reason: {input.Reason}";
            if (!string.IsNullOrEmpty(input.Notes))
            {
                reactivateText += $" - Notes: {input.Notes}";
            }
            if (input.PreserveAnswers)
            {
                reactivateText += " - Questionnaire answers preserved";
            }

            SafeAppendToNotes(entity, reactivateText);

            // CRITICAL: Use SafeUpdateOnboardingWithoutStagesProgressAsync to ensure stages_progress_json is NOT modified
            // This preserves all existing progress state (IsCompleted, Status, CompletionTime, etc.)
            return await SafeUpdateOnboardingWithoutStagesProgressAsync(entity, originalStagesProgressJson);
        }

        /// <summary>
        /// Resume onboarding with confirmation
        /// </summary>
        public async Task<bool> ResumeWithConfirmationAsync(long id, ResumeOnboardingInputDto input)
        {
            // Check permission
            if (!await CheckCaseOperatePermissionAsync(id))
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed,
                    $"User does not have permission to operate on case {id}");
            }

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            // Validate current status - only Paused onboardings can be resumed
            if (entity.Status != "Paused")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"Cannot resume onboarding. Current status is '{entity.Status}'. Only 'Paused' onboardings can be resumed.");
            }

            // Preserve the original stages_progress_json to avoid any modifications
            var originalStagesProgressJson = entity.StagesProgressJson;

            // Update status to Active
            entity.Status = "Active";

            // Add resume notes
            var resumeText = $"[Resume] Onboarding resumed from Paused status";
            if (!string.IsNullOrEmpty(input.Reason))
            {
                resumeText += $" - Reason: {input.Reason}";
            }
            if (!string.IsNullOrEmpty(input.Notes))
            {
                resumeText += $" - Notes: {input.Notes}";
            }

            SafeAppendToNotes(entity, resumeText);

            // Update stage tracking info (without modifying stagesProgress)
            entity.StageUpdatedTime = DateTimeOffset.UtcNow;
            entity.StageUpdatedBy = _operatorContextService.GetOperatorDisplayName();
            entity.StageUpdatedById = _operatorContextService.GetOperatorId();
            entity.StageUpdatedByEmail = GetCurrentUserEmail();

            // Use SafeUpdateOnboardingWithoutStagesProgressAsync to preserve stagesProgress
            return await SafeUpdateOnboardingWithoutStagesProgressAsync(entity, originalStagesProgressJson);
        }
        /// <summary>
        /// Force complete onboarding (bypass normal validation and set to Force Completed status)
        /// </summary>
        public async Task<bool> ForceCompleteAsync(long id, ForceCompleteOnboardingInputDto input)
        {
            // Check permission
            if (!await CheckCaseOperatePermissionAsync(id))
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed,
                    $"User does not have permission to operate on case {id}");
            }

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            // Validate current status - cannot force complete already completed or force completed onboardings
            if (entity.Status == "Completed" || entity.Status == "Force Completed")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"Cannot force complete onboarding with status '{entity.Status}'");
            }

            // Update status to Force Completed
            entity.Status = "Force Completed";
            entity.ActualCompletionDate = DateTimeOffset.UtcNow;
            entity.CompletionRate = 100; // Set completion rate to 100%

            // Add completion notes
            var completionText = $"[Force Complete] Onboarding force completed - Reason: {input.Reason}";
            if (!string.IsNullOrEmpty(input.CompletionNotes))
            {
                completionText += $" - Notes: {input.CompletionNotes}";
            }
            if (input.Rating.HasValue)
            {
                completionText += $" - Rating: {input.Rating}/5";
            }
            if (!string.IsNullOrEmpty(input.Feedback))
            {
                completionText += $" - Feedback: {input.Feedback}";
            }

            SafeAppendToNotes(entity, completionText);

            // Important: Do NOT modify stagesProgress data - keep it as is
            // This ensures that the stage progress remains unchanged as per requirement
            // Preserve the original stages progress data to prevent any changes
            var originalStagesProgressJson = entity.StagesProgressJson;
            var originalStagesProgress = entity.StagesProgress?.ToList(); // Create a copy

            // Update stage tracking info (this may modify stages progress, so we'll restore it afterward)
            await UpdateStageTrackingInfoAsync(entity);

            // CRITICAL: Restore the original stages progress to ensure no changes
            entity.StagesProgressJson = originalStagesProgressJson;
            entity.StagesProgress = originalStagesProgress;

            // Log the force completion action
            await LogOnboardingActionAsync(entity, "Force Complete", "Status Change", true, new
            {
                Reason = input.Reason,
                CompletionNotes = input.CompletionNotes,
                Rating = input.Rating,
                Feedback = input.Feedback,
                CompletedBy = GetCurrentUserFullName(),
                CompletedAt = DateTimeOffset.UtcNow
            });

            // Use special update method that excludes stages_progress_json field
            return await SafeUpdateOnboardingWithoutStagesProgressAsync(entity, originalStagesProgressJson);
        }
        /// <summary>
        /// Safely update onboarding entity without modifying stages_progress_json
        /// This method is specifically designed for operations where stages progress should not be changed
        /// </summary>
        private async Task<bool> SafeUpdateOnboardingWithoutStagesProgressAsync(Onboarding entity, string preserveStagesProgressJson)
        {
            try
            {
                // Always use the JSONB-safe approach to avoid type conversion errors
                var db = _onboardingRepository.GetSqlSugarClient();

                // Update all fields except stages_progress_json first
                var result = await _onboardingRepository.UpdateAsync(entity,
                    it => new
                    {
                        it.WorkflowId,
                        it.CurrentStageId,
                        it.CurrentStageOrder,
                        it.LeadId,
                        it.LeadName,
                        it.LeadEmail,
                        it.LeadPhone,
                        it.ContactPerson,
                        it.ContactEmail,
                        it.LifeCycleStageId,
                        it.LifeCycleStageName,
                        it.Status,
                        it.CompletionRate,
                        it.StartDate,
                        it.EstimatedCompletionDate,
                        it.ActualCompletionDate,
                        it.CurrentAssigneeId,
                        it.CurrentAssigneeName,
                        it.CurrentTeam,
                        it.StageUpdatedById,
                        it.StageUpdatedBy,
                        it.StageUpdatedByEmail,
                        it.StageUpdatedTime,
                        it.CurrentStageStartTime,
                        it.Priority,
                        it.IsPrioritySet,
                        it.Ownership,
                        it.OwnershipName,
                        it.OwnershipEmail,
                        it.CustomFieldsJson,
                        it.Notes,
                        it.IsActive,
                        it.ModifyDate,
                        it.ModifyBy,
                        it.ModifyUserId,
                        it.IsValid
                    });

                // IMPORTANT: Restore the original stages_progress_json to ensure no changes to stages progress
                if (!string.IsNullOrEmpty(preserveStagesProgressJson))
                {
                    try
                    {
                        var progressSql = "UPDATE ff_onboarding SET stages_progress_json = @StagesProgressJson::jsonb WHERE id = @Id";
                        await db.Ado.ExecuteCommandAsync(progressSql, new
                        {
                            StagesProgressJson = preserveStagesProgressJson,
                            Id = entity.Id
                        });

                        LoggingExtensions.WriteLine($"[ForceComplete] Preserved original stages_progress_json for onboarding {entity.Id}");
                    }
                    catch (Exception progressEx)
                    {
                        // Log but don't fail the main update
                        LoggingExtensions.WriteLine($"Warning: Failed to preserve stages_progress_json: {progressEx.Message}");
                        // Try alternative approach with parameter substitution
                        try
                        {
                            var escapedJson = preserveStagesProgressJson.Replace("'", "''");
                            var directSql = $"UPDATE ff_onboarding SET stages_progress_json = '{escapedJson}'::jsonb WHERE id = {entity.Id}";
                            await db.Ado.ExecuteCommandAsync(directSql);

                            LoggingExtensions.WriteLine($"[ForceComplete] Preserved original stages_progress_json for onboarding {entity.Id} using direct SQL");
                        }
                        catch (Exception directEx)
                        {
                            LoggingExtensions.WriteLine($"Error: Both parameterized and direct JSONB preserve failed: {directEx.Message}");
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new CRMException(ErrorCodeEnum.SystemError,
                    $"Failed to safely update onboarding without stages progress changes: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates and formats JSON array string for PostgreSQL JSONB
        /// </summary>
        /// <summary>
        /// Parse JSON array that might be double-encoded (handles both "[...]" and "\"[...]\"")
        /// </summary>
        private static List<string> ParseJsonArraySafe(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return new List<string>();
            }

            try
            {
                // Handle potential double-encoded JSON string
                var workingString = jsonString.Trim();

                // If the string starts and ends with quotes, it's double-encoded, so deserialize twice
                if (workingString.StartsWith("\"") && workingString.EndsWith("\""))
                {
                    // First deserialize to remove outer quotes and unescape
                    workingString = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(workingString);
                }

                // Now deserialize to list
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(workingString);
                return result ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Check Workflow view permission in-memory (no database query)
        /// Used for batch permission filtering in list queries
        /// </summary>
        private bool CheckWorkflowViewPermissionInMemory(
            Domain.Entities.OW.Workflow workflow,
            long userId,
            List<long> userTeamIds)
        {
            // Public view mode = everyone can view
            if (workflow.ViewPermissionMode == ViewPermissionModeEnum.Public)
            {
                return true;
            }

            // VisibleToTeams mode = check team whitelist
            if (workflow.ViewPermissionMode == ViewPermissionModeEnum.VisibleToTeams)
            {
                if (string.IsNullOrWhiteSpace(workflow.ViewTeams))
                {
                    LoggingExtensions.WriteLine($"[Permission Debug] Workflow {workflow.Id} - VisibleToTeams mode with NULL ViewTeams = DENY");
                    return false; // NULL ViewTeams in VisibleToTeams mode = deny all (whitelist is empty)
                }

                // Parse ViewTeams JSON array (handles double-encoding)
                var viewTeams = ParseJsonArraySafe(workflow.ViewTeams);
                LoggingExtensions.WriteLine($"[Permission Debug] Workflow {workflow.Id} - Parsed ViewTeams: [{string.Join(", ", viewTeams)}]");

                if (viewTeams.Count == 0)
                {
                    LoggingExtensions.WriteLine($"[Permission Debug] Workflow {workflow.Id} - Empty ViewTeams = DENY");
                    return false; // Empty whitelist = deny all
                }

                var viewTeamLongs = viewTeams.Select(t => long.TryParse(t, out var tid) ? tid : 0).Where(t => t > 0).ToHashSet();
                LoggingExtensions.WriteLine($"[Permission Debug] Workflow {workflow.Id} - ViewTeamLongs: [{string.Join(", ", viewTeamLongs)}]");
                LoggingExtensions.WriteLine($"[Permission Debug] Workflow {workflow.Id} - UserTeamIds: [{string.Join(", ", userTeamIds)}]");

                bool hasMatch = userTeamIds.Any(ut => viewTeamLongs.Contains(ut));
                LoggingExtensions.WriteLine($"[Permission Debug] Workflow {workflow.Id} - Team match result: {hasMatch}");

                return hasMatch;
            }

            // Private mode or unknown mode
            return false;
        }
      

        private static string ValidateAndFormatJsonArray(string jsonArray)
        {
            if (string.IsNullOrWhiteSpace(jsonArray))
            {
                return "[]";
            }

            try
            {
                // Try to parse and validate the JSON
                var parsed = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonArray);
                if (parsed is Newtonsoft.Json.Linq.JArray)
                {
                    return jsonArray;
                }
                // If it's not an array, return empty array
                return "[]";
            }
            catch
            {
                // If parsing fails, return empty array
                return "[]";
            }
        }

        /// <summary>
        /// Validate that team IDs from JSON arrays exist in the team tree from UserService.
        /// Accepts JSON arrays (possibly double-encoded) like ["team1","team2"].
        /// Throws BusinessError if any invalid IDs are found.
        /// </summary>
        private async Task ValidateTeamSelectionsFromJsonAsync(string viewTeamsJson, string operateTeamsJson)
        {
            var viewTeams = ParseJsonArraySafe(viewTeamsJson) ?? new List<string>();
            var operateTeams = ParseJsonArraySafe(operateTeamsJson) ?? new List<string>();

            var needsValidation = (viewTeams.Any() || operateTeams.Any());
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
                // Do not block the operation if IDM is unavailable; just log
                LoggingExtensions.WriteLine($"[TeamValidation] Skipped due to error fetching team tree: {ex.Message}");
                return;
            }

            var invalid = new List<string>();
            invalid.AddRange(viewTeams.Where(id => !string.IsNullOrWhiteSpace(id) && !allTeamIds.Contains(id)));
            invalid.AddRange(operateTeams.Where(id => !string.IsNullOrWhiteSpace(id) && !allTeamIds.Contains(id)));
            invalid = invalid.Distinct(StringComparer.Ordinal).ToList();

            if (invalid.Any())
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"The following team IDs do not exist: {string.Join(", ", invalid)}");
            }
        }

        /// <summary>
        /// Get all valid team IDs from UserService team tree (excludes placeholder teams like 'Other').
        /// </summary>
        private async Task<HashSet<string>> GetAllTeamIdsFromUserTreeAsync()
        {
            var tree = await _userService.GetUserTreeAsync();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (tree == null || !tree.Any())
            {
                return ids;
            }

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
        /// Sync Onboarding fields to Static Field Values when Onboarding is updated
        /// </summary>
        private async Task SyncStaticFieldValuesAsync(
            long onboardingId,
            long stageId,
            string originalLeadId,
            string originalLeadName,
            string originalContactPerson,
            string originalContactEmail,
            string originalLeadPhone,
            long? originalLifeCycleStageId,
            string originalPriority,
            OnboardingInputDto input)
        {
            try
            {
                Console.WriteLine($"[OnboardingService] Starting static field sync - OnboardingId: {onboardingId}, StageId: {stageId}");

                var staticFieldUpdates = new List<FlowFlex.Application.Contracts.Dtos.OW.StaticField.StaticFieldValueInputDto>();

                // Field mapping: Onboarding field -> Static Field Name
                // Only update fields that have changed
                if (!string.Equals(originalLeadId, input.LeadId, StringComparison.Ordinal))
                {
                    Console.WriteLine($"[OnboardingService] LEADID changed: '{originalLeadId}' -> '{input.LeadId}'");
                    staticFieldUpdates.Add(CreateStaticFieldInput(
                        onboardingId,
                        stageId,
                        "LEADID",
                        input.LeadId,
                        "text",
                        "Lead ID",
                        isRequired: false
                    ));
                }

                if (!string.Equals(originalLeadName, input.LeadName, StringComparison.Ordinal))
                {
                    Console.WriteLine($"[OnboardingService] CUSTOMERNAME changed: '{originalLeadName}' -> '{input.LeadName}'");
                    staticFieldUpdates.Add(CreateStaticFieldInput(
                        onboardingId,
                        stageId,
                        "CUSTOMERNAME",
                        input.LeadName,
                        "text",
                        "Customer Name",
                        isRequired: false
                    ));
                }

                if (!string.Equals(originalContactPerson, input.ContactPerson, StringComparison.Ordinal))
                {
                    Console.WriteLine($"[OnboardingService] CONTACTNAME changed: '{originalContactPerson}' -> '{input.ContactPerson}'");
                    staticFieldUpdates.Add(CreateStaticFieldInput(
                        onboardingId,
                        stageId,
                        "CONTACTNAME",
                        input.ContactPerson,
                        "text",
                        "Contact Name",
                        isRequired: false
                    ));
                }

                if (!string.Equals(originalContactEmail, input.ContactEmail, StringComparison.Ordinal))
                {
                    Console.WriteLine($"[OnboardingService] CONTACTEMAIL changed: '{originalContactEmail}' -> '{input.ContactEmail}'");
                    staticFieldUpdates.Add(CreateStaticFieldInput(
                        onboardingId,
                        stageId,
                        "CONTACTEMAIL",
                        input.ContactEmail,
                        "email",
                        "Contact Email",
                        isRequired: false
                    ));
                }

                if (!string.Equals(originalLeadPhone, input.LeadPhone, StringComparison.Ordinal))
                {
                    Console.WriteLine($"[OnboardingService] CONTACTPHONE changed: '{originalLeadPhone}' -> '{input.LeadPhone}'");
                    staticFieldUpdates.Add(CreateStaticFieldInput(
                        onboardingId,
                        stageId,
                        "CONTACTPHONE",
                        input.LeadPhone,
                        "tel",
                        "Contact Phone",
                        isRequired: false
                    ));
                }

                if (originalLifeCycleStageId != input.LifeCycleStageId)
                {
                    Console.WriteLine($"[OnboardingService] LIFECYCLESTAGE changed: '{originalLifeCycleStageId}' -> '{input.LifeCycleStageId}'");
                    staticFieldUpdates.Add(CreateStaticFieldInput(
                        onboardingId,
                        stageId,
                        "LIFECYCLESTAGE",
                        input.LifeCycleStageId?.ToString() ?? "",
                        "select",
                        "Life Cycle Stage",
                        isRequired: false
                    ));
                }

                if (!string.Equals(originalPriority, input.Priority, StringComparison.Ordinal))
                {
                    Console.WriteLine($"[OnboardingService] PRIORITY changed: '{originalPriority}' -> '{input.Priority}'");
                    staticFieldUpdates.Add(CreateStaticFieldInput(
                        onboardingId,
                        stageId,
                        "PRIORITY",
                        input.Priority,
                        "select",
                        "Priority",
                        isRequired: false
                    ));
                }

                // Batch update static field values if any fields changed
                if (staticFieldUpdates.Any())
                {
                    Console.WriteLine($"[OnboardingService] Syncing {staticFieldUpdates.Count} static field(s) to database");

                    var batchInput = new FlowFlex.Application.Contracts.Dtos.OW.StaticField.BatchStaticFieldValueInputDto
                    {
                        OnboardingId = onboardingId,
                        StageId = stageId,
                        FieldValues = staticFieldUpdates,
                        Source = "onboarding_update",
                        IpAddress = GetClientIpAddress(),
                        UserAgent = GetUserAgent()
                    };

                    await _staticFieldValueService.BatchSaveAsync(batchInput);
                    Console.WriteLine($"[OnboardingService] Static field sync completed successfully");
                }
                else
                {
                    Console.WriteLine($"[OnboardingService] No static field changes detected, sync skipped");
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the main update operation
                Console.WriteLine($"[OnboardingService] Failed to sync static field values: {ex.Message}");
                Console.WriteLine($"[OnboardingService] Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Get client IP address from HTTP context
        /// </summary>
        private string GetClientIpAddress()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null) return string.Empty;

            var ipAddress = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            }
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            }

            return ipAddress ?? string.Empty;
        }

        /// <summary>
        /// Get user agent from HTTP context
        /// </summary>
        private string GetUserAgent()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            return httpContext?.Request.Headers["User-Agent"].ToString() ?? string.Empty;
        }

        /// <summary>
        /// Create StaticFieldValueInputDto for static field sync
        /// </summary>
        private FlowFlex.Application.Contracts.Dtos.OW.StaticField.StaticFieldValueInputDto CreateStaticFieldInput(
            long onboardingId,
            long stageId,
            string fieldName,
            string fieldValue,
            string fieldType,
            string fieldLabel,
            bool isRequired)
        {
            return new FlowFlex.Application.Contracts.Dtos.OW.StaticField.StaticFieldValueInputDto
            {
                OnboardingId = onboardingId,
                StageId = stageId,
                FieldName = fieldName,
                FieldValueJson = JsonSerializer.Serialize(fieldValue),
                FieldType = fieldType,
                DisplayName = fieldLabel,
                FieldLabel = fieldLabel,
                IsRequired = isRequired,
                Status = "Draft",
                CompletionRate = string.IsNullOrWhiteSpace(fieldValue) ? 0 : 100,
                ValidationStatus = "Pending"
            };
        }

        /// <summary>
        /// Get authorized users for onboarding based on permission configuration
        /// If case has no permission restrictions (Public mode), returns all users
        /// If case has permission restrictions, returns only authorized users based on ViewPermissionMode and ViewPermissionSubjectType
        /// </summary>
        public async Task<List<UserTreeNodeDto>> GetAuthorizedUsersAsync(long id)
        {
            // Get onboarding entity
            var onboarding = await _onboardingRepository.GetByIdAsync(id);
            if (onboarding == null)
            {
                throw new CRMException(System.Net.HttpStatusCode.NotFound, $"Onboarding with ID {id} not found");
            }

            // Get all users tree
            var allUsersTree = await _userService.GetUserTreeAsync();

            // If permission mode is Public, return all users (flatten to user-only list)
            if (onboarding.ViewPermissionMode == ViewPermissionModeEnum.Public)
            {
                return ExtractUserNodes(allUsersTree);
            }

            // Parse permission fields
            var viewTeams = ParseJsonArraySafe(onboarding.ViewTeams) ?? new List<string>();
            var viewUsers = ParseJsonArraySafe(onboarding.ViewUsers) ?? new List<string>();

            // Filter users based on permission configuration
            var filteredTree = await FilterUserTreeByPermissionAsync(
                allUsersTree,
                onboarding.ViewPermissionMode,
                onboarding.ViewPermissionSubjectType,
                viewTeams,
                viewUsers,
                onboarding.Ownership
            );

            // Always return flat user-only list
            return ExtractUserNodes(filteredTree);
        }

        /// <summary>
        /// Filter user tree based on permission configuration
        /// </summary>
        private async Task<List<UserTreeNodeDto>> FilterUserTreeByPermissionAsync(
            List<UserTreeNodeDto> allUsersTree,
            ViewPermissionModeEnum viewPermissionMode,
            PermissionSubjectTypeEnum viewPermissionSubjectType,
            List<string> viewTeams,
            List<string> viewUsers,
            long? ownership)
        {
            if (allUsersTree == null || !allUsersTree.Any())
            {
                return new List<UserTreeNodeDto>();
            }

            // Private mode: return ownership user only
            if (viewPermissionMode == ViewPermissionModeEnum.Private)
            {
                return await FilterUserTreeByOwnershipAsync(allUsersTree, ownership);
            }

            // Team-based permission
            if (viewPermissionSubjectType == PermissionSubjectTypeEnum.Team)
            {
                return FilterUserTreeByTeams(allUsersTree, viewPermissionMode, viewTeams);
            }
            // User-based permission
            else if (viewPermissionSubjectType == PermissionSubjectTypeEnum.User)
            {
                return FilterUserTreeByUsers(allUsersTree, viewPermissionMode, viewUsers);
            }

            // Default: return all users
            return allUsersTree;
        }

        /// <summary>
        /// Filter user tree to return only ownership user
        /// </summary>
        private async Task<List<UserTreeNodeDto>> FilterUserTreeByOwnershipAsync(
            List<UserTreeNodeDto> allUsersTree,
            long? ownership)
        {
            if (!ownership.HasValue || ownership.Value <= 0)
            {
                // No ownership set, return empty tree
                return new List<UserTreeNodeDto>();
            }

            var ownershipUserId = ownership.Value.ToString();
            var ownershipUserNode = FindUserNodeInTree(allUsersTree, ownershipUserId);

            if (ownershipUserNode == null)
            {
                // Ownership user not found in tree, try to get from UserService
                try
                {
                    var userDto = await _userService.GetUserByIdAsync(ownership.Value);
                    if (userDto != null)
                    {
                        // Create a user node from UserDto
                        ownershipUserNode = new UserTreeNodeDto
                        {
                            Id = userDto.Id.ToString(),
                            Name = userDto.Username ?? userDto.Email,
                            Type = "user",
                            Username = userDto.Username,
                            Email = userDto.Email,
                            UserDetails = userDto,
                            MemberCount = 0,
                            Children = null
                        };
                    }
                }
                catch
                {
                    // If user not found, return empty tree
                    return new List<UserTreeNodeDto>();
                }
            }

            if (ownershipUserNode == null)
            {
                return new List<UserTreeNodeDto>();
            }

            // Return only the ownership user as a flat list (no team structure)
            return new List<UserTreeNodeDto>
            {
                new UserTreeNodeDto
                {
                    Id = ownershipUserNode.Id,
                    Name = ownershipUserNode.Name,
                    Type = "user",
                    Username = ownershipUserNode.Username,
                    Email = ownershipUserNode.Email,
                    UserDetails = ownershipUserNode.UserDetails,
                    MemberCount = 0,
                    Children = null
                }
            };
        }

        /// <summary>
        /// Find user node in the tree by user ID
        /// </summary>
        private UserTreeNodeDto FindUserNodeInTree(List<UserTreeNodeDto> tree, string userId)
        {
            if (tree == null || !tree.Any())
            {
                return null;
            }

            foreach (var node in tree)
            {
                // Check if current node is the user
                if (node.Type == "user" && node.Id == userId)
                {
                    return node;
                }

                // Recursively search in children
                if (node.Children != null && node.Children.Any())
                {
                    var found = FindUserNodeInTree(node.Children, userId);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Build tree structure for a single user, preserving team hierarchy if possible
        /// </summary>
        private List<UserTreeNodeDto> BuildTreeForSingleUser(UserTreeNodeDto userNode, List<UserTreeNodeDto> allUsersTree)
        {
            if (userNode == null)
            {
                return new List<UserTreeNodeDto>();
            }

            // Try to find the team that contains this user
            var userTeam = FindUserTeam(allUsersTree, userNode.Id);

            if (userTeam != null)
            {
                // Create a team node with only this user
                var teamNode = new UserTreeNodeDto
                {
                    Id = userTeam.Id,
                    Name = userTeam.Name,
                    Type = "team",
                    MemberCount = 1,
                    Children = new List<UserTreeNodeDto>
                    {
                        new UserTreeNodeDto
                        {
                            Id = userNode.Id,
                            Name = userNode.Name,
                            Type = "user",
                            Username = userNode.Username,
                            Email = userNode.Email,
                            UserDetails = userNode.UserDetails,
                            MemberCount = 0,
                            Children = null
                        }
                    }
                };

                return new List<UserTreeNodeDto> { teamNode };
            }
            else
            {
                // User not in any team, create a simple user node
                // Put it in "Other" team for consistency
                var otherTeamNode = new UserTreeNodeDto
                {
                    Id = "Other",
                    Name = "Other",
                    Type = "team",
                    MemberCount = 1,
                    Children = new List<UserTreeNodeDto>
                    {
                        new UserTreeNodeDto
                        {
                            Id = userNode.Id,
                            Name = userNode.Name,
                            Type = "user",
                            Username = userNode.Username,
                            Email = userNode.Email,
                            UserDetails = userNode.UserDetails,
                            MemberCount = 0,
                            Children = null
                        }
                    }
                };

                return new List<UserTreeNodeDto> { otherTeamNode };
            }
        }

        /// <summary>
        /// Extract flat list of user nodes from a (team+user) tree, with deduplication by user ID
        /// </summary>
        private List<UserTreeNodeDto> ExtractUserNodes(List<UserTreeNodeDto> nodes)
        {
            var result = new List<UserTreeNodeDto>();
            var seenUserIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            if (nodes == null || nodes.Count == 0) return result;

            void Traverse(UserTreeNodeDto node)
            {
                if (node == null) return;
                if (node.Type == "user")
                {
                    // Deduplicate by user ID
                    if (!string.IsNullOrEmpty(node.Id) && !seenUserIds.Contains(node.Id))
                    {
                        seenUserIds.Add(node.Id);
                        result.Add(new UserTreeNodeDto
                        {
                            Id = node.Id,
                            Name = node.Name,
                            Type = node.Type,
                            Username = node.Username,
                            Email = node.Email,
                            UserDetails = node.UserDetails,
                            MemberCount = 0,
                            Children = null
                        });
                    }
                }
                if (node.Children != null)
                {
                    foreach (var child in node.Children)
                    {
                        Traverse(child);
                    }
                }
            }

            foreach (var root in nodes)
            {
                Traverse(root);
            }

            return result;
        }

        /// <summary>
        /// Find the team that contains a specific user
        /// </summary>
        private UserTreeNodeDto FindUserTeam(List<UserTreeNodeDto> tree, string userId)
        {
            if (tree == null || !tree.Any())
            {
                return null;
            }

            foreach (var node in tree)
            {
                if (node.Type == "team" && node.Children != null && node.Children.Any())
                {
                    // Check if this team contains the user
                    var containsUser = FindUserNodeInTree(node.Children, userId);
                    if (containsUser != null)
                    {
                        return node;
                    }

                    // Recursively search in child teams
                    var childTeam = FindUserTeam(node.Children, userId);
                    if (childTeam != null)
                    {
                        return childTeam;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Filter user tree by teams based on permission mode
        /// </summary>
        private List<UserTreeNodeDto> FilterUserTreeByTeams(
            List<UserTreeNodeDto> allUsersTree,
            ViewPermissionModeEnum viewPermissionMode,
            List<string> viewTeams)
        {
            var viewTeamsSet = new HashSet<string>(viewTeams ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            var result = new List<UserTreeNodeDto>();

            foreach (var teamNode in allUsersTree)
            {
                if (teamNode.Type != "team")
                {
                    continue;
                }

                bool includeTeam = false;

                if (viewPermissionMode == ViewPermissionModeEnum.VisibleToTeams)
                {
                    // Only include teams in the whitelist
                    includeTeam = viewTeamsSet.Contains(teamNode.Id);
                }
                else if (viewPermissionMode == ViewPermissionModeEnum.InvisibleToTeams)
                {
                    // Exclude teams in the blacklist
                    includeTeam = !viewTeamsSet.Contains(teamNode.Id);
                }

                if (includeTeam)
                {
                    // Create a copy of the team node with filtered children
                    var filteredTeamNode = new UserTreeNodeDto
                    {
                        Id = teamNode.Id,
                        Name = teamNode.Name,
                        Type = teamNode.Type,
                        MemberCount = 0,
                        Children = new List<UserTreeNodeDto>()
                    };

                    // Recursively filter child teams and include all users under this team
                    if (teamNode.Children != null && teamNode.Children.Any())
                    {
                        foreach (var child in teamNode.Children)
                        {
                            if (child.Type == "team")
                            {
                                // Recursively filter child teams
                                var filteredChildTeams = FilterUserTreeByTeams(
                                    new List<UserTreeNodeDto> { child },
                                    viewPermissionMode,
                                    viewTeams);
                                if (filteredChildTeams.Any())
                                {
                                    filteredTeamNode.Children.AddRange(filteredChildTeams);
                                }
                            }
                            else if (child.Type == "user")
                            {
                                // Include all users under the team
                                filteredTeamNode.Children.Add(child);
                            }
                        }
                    }

                    // Update member count
                    filteredTeamNode.MemberCount = filteredTeamNode.Children.Count;

                    // Only add team if it has members or child teams
                    if (filteredTeamNode.Children.Any())
                    {
                        result.Add(filteredTeamNode);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Filter user tree by users based on permission mode
        /// </summary>
        private List<UserTreeNodeDto> FilterUserTreeByUsers(
            List<UserTreeNodeDto> allUsersTree,
            ViewPermissionModeEnum viewPermissionMode,
            List<string> viewUsers)
        {
            var viewUsersSet = new HashSet<string>(viewUsers ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            var result = new List<UserTreeNodeDto>();

            foreach (var rootNode in allUsersTree)
            {
                var filteredNode = FilterNodeByUsers(rootNode, viewPermissionMode, viewUsersSet);
                if (filteredNode != null)
                {
                    result.Add(filteredNode);
                }
            }

            return result;
        }

        /// <summary>
        /// Recursively filter a node and its children by users
        /// </summary>
        private UserTreeNodeDto FilterNodeByUsers(
            UserTreeNodeDto node,
            ViewPermissionModeEnum viewPermissionMode,
            HashSet<string> viewUsersSet)
        {
            if (node == null)
            {
                return null;
            }

            // If it's a user node, check if it should be included
            if (node.Type == "user")
            {
                bool includeUser = false;

                if (viewPermissionMode == ViewPermissionModeEnum.VisibleToTeams)
                {
                    // Only include users in the whitelist
                    includeUser = viewUsersSet.Contains(node.Id);
                }
                else if (viewPermissionMode == ViewPermissionModeEnum.InvisibleToTeams)
                {
                    // Exclude users in the blacklist
                    includeUser = !viewUsersSet.Contains(node.Id);
                }

                if (includeUser)
                {
                    return new UserTreeNodeDto
                    {
                        Id = node.Id,
                        Name = node.Name,
                        Type = node.Type,
                        Username = node.Username,
                        Email = node.Email,
                        UserDetails = node.UserDetails,
                        MemberCount = 0,
                        Children = null
                    };
                }

                return null;
            }

            // If it's a team node, filter its children
            if (node.Type == "team")
            {
                var filteredChildren = new List<UserTreeNodeDto>();

                if (node.Children != null && node.Children.Any())
                {
                    foreach (var child in node.Children)
                    {
                        var filteredChild = FilterNodeByUsers(child, viewPermissionMode, viewUsersSet);
                        if (filteredChild != null)
                        {
                            filteredChildren.Add(filteredChild);
                        }
                    }
                }

                // Only include team if it has filtered children
                if (filteredChildren.Any())
                {
                    return new UserTreeNodeDto
                    {
                        Id = node.Id,
                        Name = node.Name,
                        Type = node.Type,
                        MemberCount = filteredChildren.Count,
                        Children = filteredChildren
                    };
                }
            }

            return null;
        }

        #endregion

        #region Case Code Auto-Fill for Legacy Data

        /// <summary>
        /// Ensure case code is generated for legacy data (if CaseCode is null or empty)
        /// </summary>
        private async Task EnsureCaseCodeAsync(Onboarding entity)
        {
            if (string.IsNullOrWhiteSpace(entity.CaseCode))
            {
                try
                {
                    // Generate case code from lead name
                    entity.CaseCode = await _caseCodeGeneratorService.GenerateCaseCodeAsync(entity.LeadName);

                    // Update database
                    var updateSql = "UPDATE ff_onboarding SET case_code = @CaseCode WHERE id = @Id";
                    await _onboardingRepository.GetSqlSugarClient().Ado.ExecuteCommandAsync(updateSql, new
                    {
                        CaseCode = entity.CaseCode,
                        Id = entity.Id
                    });

                    LoggingExtensions.WriteLine($"[INFO] Auto-generated CaseCode '{entity.CaseCode}' for Onboarding {entity.Id}");
                }
                catch (Exception ex)
                {
                    LoggingExtensions.WriteLine($"[ERROR] Failed to auto-generate CaseCode for Onboarding {entity.Id}: {ex.Message}");
                    // Don't throw - this is a background enhancement, not critical
                }
            }
        }

        #endregion
    }
}