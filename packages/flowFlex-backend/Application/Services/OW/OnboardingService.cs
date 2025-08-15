using AutoMapper;
using MediatR;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Events;
using System.Linq;
using SqlSugar;
using FlowFlex.Domain.Shared.Attr;
using System.Text;
using System.IO;
using System.Globalization;
using OfficeOpenXml;
// using Item.Redis; // Temporarily disable Redis
using System.Text.Json;
using System.Diagnostics;
using FlowFlex.Domain.Shared.Models;
using System.Linq.Expressions;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Shared.Utils;

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
            IStageService stageService)
        {
            _onboardingRepository = onboardingRepository ?? throw new ArgumentNullException(nameof(onboardingRepository));
            _workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
            _stageRepository = stageRepository ?? throw new ArgumentNullException(nameof(stageRepository));
            _userInvitationRepository = userInvitationRepository ?? throw new ArgumentNullException(nameof(userInvitationRepository));

            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _stageService = stageService ?? throw new ArgumentNullException(nameof(stageService));
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
                // Check if Lead ID already exists for this tenant with enhanced checking
                // Debug logging handled by structured logging
                // Use SqlSugar client directly for more precise checking
                var sqlSugarClient = _onboardingRepository.GetSqlSugarClient();
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
                // Debug logging handled by structured logging
                // Set initial values with explicit null checks
                entity.CurrentStageId = firstStage?.Id;
                entity.CurrentStageOrder = firstStage?.Order ?? 0;
                entity.Status = string.IsNullOrEmpty(entity.Status) ? "Started" : entity.Status;
                entity.StartDate = entity.StartDate ?? DateTimeOffset.UtcNow;
                entity.CurrentStageStartTime = DateTimeOffset.UtcNow;
                entity.CompletionRate = 0;
                entity.IsPrioritySet = false;
                entity.Priority = string.IsNullOrEmpty(entity.Priority) ? "Medium" : entity.Priority;
                entity.IsActive = true;

                // Initialize stages progress as empty JSON array for JSONB compatibility
                entity.StagesProgressJson = "[]";

                // Initialize create information with proper ID and timestamps
                entity.InitCreateInfo(_userContext);


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

                if (string.IsNullOrWhiteSpace(entity.LeadId))
                {
                    throw new CRMException(ErrorCodeEnum.ParamInvalid, "LeadId cannot be null or empty");
                }

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
                        // Get the last inserted record by combining unique fields
                        var lastInserted = await sqlSugarClient.Queryable<Onboarding>()
                            .Where(x => x.LeadId == entity.LeadId &&
                                       x.WorkflowId == entity.WorkflowId &&
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
                    lead_id, lead_name, lead_email, lead_phone, status, completion_rate,
                    priority, is_priority_set, notes, is_active, stages_progress_json, id,
                    current_stage_id, contact_person, contact_email, life_cycle_stage_id, 
                    life_cycle_stage_name, start_date, current_stage_start_time
                ) VALUES (
                    @TenantId, @AppCode, @IsValid, @CreateDate, @ModifyDate, @CreateBy, @ModifyBy,
                    @CreateUserId, @ModifyUserId, @WorkflowId, @CurrentStageOrder,
                    @LeadId, @LeadName, @LeadEmail, @LeadPhone, @Status, @CompletionRate,
                    @Priority, @IsPrioritySet, @Notes, @IsActive, @StagesProgressJson::jsonb, @Id,
                    CASE WHEN @CurrentStageId IS NULL OR @CurrentStageId = '' THEN NULL ELSE @CurrentStageId::bigint END,
                    @ContactPerson, @ContactEmail,
                    CASE WHEN @LifeCycleStageId IS NULL OR @LifeCycleStageId = '' THEN NULL ELSE @LifeCycleStageId::bigint END,
                    @LifeCycleStageName, @StartDate, @CurrentStageStartTime
                ) RETURNING id";

                        var parameters = new
                        {
                            TenantId = entity.TenantId,
                            AppCode = entity.AppCode,
                            IsValid = true,
                            CreateDate = DateTimeOffset.UtcNow,
                            ModifyDate = DateTimeOffset.UtcNow,
                            CreateBy = GetCurrentUserName(),
                            ModifyBy = GetCurrentUserName(),
                            CreateUserId = 0L,
                            ModifyUserId = 0L,
                            WorkflowId = entity.WorkflowId,
                            CurrentStageOrder = entity.CurrentStageOrder,
                            LeadId = entity.LeadId,
                            LeadName = entity.LeadName,
                            LeadEmail = entity.LeadEmail,
                            LeadPhone = entity.LeadPhone,
                            Status = entity.Status,
                            CompletionRate = entity.CompletionRate,
                            Priority = entity.Priority,
                            IsPrioritySet = entity.IsPrioritySet,
                            Notes = entity.Notes ?? "",
                            IsActive = entity.IsActive,
                            StagesProgressJson = entity.StagesProgressJson,
                            Id = entity.Id,
                            CurrentStageId = entity.CurrentStageId?.ToString(),
                            ContactPerson = entity.ContactPerson,
                            ContactEmail = entity.ContactEmail,
                            LifeCycleStageId = entity.LifeCycleStageId?.ToString(),
                            LifeCycleStageName = entity.LifeCycleStageName,
                            StartDate = entity.StartDate,
                            CurrentStageStartTime = entity.CurrentStageStartTime
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
                    _ = Task.Run(async () =>
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

                // If workflow changed, validate new workflow and reset stages
                if (entity.WorkflowId != input.WorkflowId)
                {
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

                    entity.CurrentStageId = firstStage?.Id;
                    entity.CurrentStageOrder = firstStage?.Order ?? 1;
                    entity.CompletionRate = 0;
                }

                // Map the input to entity (this will update all the mappable fields)
                // Debug logging handled by structured logging
                _mapper.Map(input, entity);

                // Update system fields
                entity.ModifyDate = DateTimeOffset.UtcNow;
                entity.ModifyBy = GetCurrentUserName();
                entity.ModifyUserId = GetCurrentUserId() ?? 0; // User context integration
                                                               // Debug logging handled by structured logging
                var result = await SafeUpdateOnboardingAsync(entity);

                // Log onboarding update and clear cache
                if (result)
                {
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
                        UpdatedBy = GetCurrentUserName(),
                        UpdatedAt = DateTimeOffset.UtcNow
                    });

                    // Clear related cache data (async execution, doesn't affect main flow)
                    _ = Task.Run(async () =>
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
            entity.ModifyBy = GetCurrentUserName();
            entity.ModifyUserId = GetCurrentUserId() ?? 0;

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

                // Ensure stages progress is properly initialized and synced
                await EnsureStagesProgressInitializedAsync(entity);

                var result = _mapper.Map<OnboardingOutputDto>(entity);

                // Get workflow name
                var workflow = await _workflowRepository.GetByIdAsync(entity.WorkflowId);
                result.WorkflowName = workflow?.Name;

                // Get current stage name
                if (entity.CurrentStageId.HasValue)
                {
                    var stage = await _stageRepository.GetByIdAsync(entity.CurrentStageId.Value);
                    result.CurrentStageName = stage?.Name;
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new CRMException(ErrorCodeEnum.SystemError, $"Error getting onboarding by ID {id}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get onboarding list
        /// </summary>
        public async Task<List<OnboardingOutputDto>> GetListAsync()
        {
            var entities = await _onboardingRepository.GetListAsync(x => x.IsValid);

            // Ensure stages progress is properly initialized for each entity
            foreach (var entity in entities)
            {
                await EnsureStagesProgressInitializedAsync(entity);
            }

            return _mapper.Map<List<OnboardingOutputDto>>(entities);
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

                // Support comma-separated Lead IDs
                if (!string.IsNullOrEmpty(request.LeadId) && request.LeadId != "string")
                {
                    var leadIds = request.GetLeadIdsList();
                    if (leadIds.Any())
                    {
                        // Use OR condition to match any of the lead IDs with exact match (case-insensitive)
                        whereExpressions.Add(x => leadIds.Any(id => x.LeadId.ToLower() == id.ToLower()));
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

                // Determine sort field and direction
                Expression<Func<Onboarding, object>> orderByExpression = GetOrderByExpression(request);
                bool isAsc = GetSortDirection(request);

                // Apply pagination or get all data
                List<Onboarding> pagedEntities;
                int totalCount;
                int pageIndex = Math.Max(1, request.PageIndex > 0 ? request.PageIndex : 1);
                int pageSize = Math.Max(1, Math.Min(100, request.PageSize > 0 ? request.PageSize : 10));

                if (request.AllData)
                {
                    // Get all data without pagination using the existing GetListAsync method
                    // that accepts multiple where expressions
                    var orderByType = isAsc ? OrderByType.Asc : OrderByType.Desc;

                    // Use the SqlSugar client directly to build the query
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

                    pagedEntities = await queryable.ToListAsync();
                    totalCount = pagedEntities.Count;
                }
                else
                {
                    // Use BaseRepository's safe pagination method
                    var (entities, count) = await _onboardingRepository.GetPageListAsync(
                        whereExpressions,
                        pageIndex,
                        pageSize,
                        orderByExpression,
                        isAsc
                    );
                    pagedEntities = entities;
                    totalCount = count;
                }

                // Batch get Workflow and Stage information to avoid N+1 queries
                var (workflows, stages) = await GetRelatedDataBatchOptimizedAsync(pagedEntities);

                // Create lookup dictionaries to improve search performance
                var workflowDict = workflows.ToDictionary(w => w.Id, w => w.Name);
                var stageDict = stages.ToDictionary(s => s.Id, s => s.Name);

                // Batch process JSON deserialization
                ProcessStagesProgressParallel(pagedEntities);

                // Map to output DTOs
                var results = _mapper.Map<List<OnboardingOutputDto>>(pagedEntities);

                // Use dictionaries to quickly populate workflow and stage names
                foreach (var result in results)
                {
                    result.WorkflowName = workflowDict.GetValueOrDefault(result.WorkflowId);
                    if (result.CurrentStageId.HasValue)
                    {
                        result.CurrentStageName = stageDict.GetValueOrDefault(result.CurrentStageId.Value);
                    }
                }

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
                "workflowid" => x => x.WorkflowId,
                "currentstageid" => x => x.CurrentStageId,
                "lifecyclestageid" => x => x.LifeCycleStageId,
                "lifecyclestagename" => x => x.LifeCycleStageName,
                "priority" => x => x.Priority,
                "status" => x => x.Status,
                "isactive" => x => x.IsActive,
                "completionrate" => x => x.CompletionRate,
                "createdate" => x => x.CreateDate,
                "modifydate" => x => x.ModifyDate,
                "createby" => x => x.CreateBy,
                "modifyby" => x => x.ModifyBy,
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
        /// Build query cache key
        /// </summary>
        private string BuildQueryCacheKey(OnboardingQueryRequest request, string tenantId)
        {
            var keyParts = new List<string>
            {
                "ow:onboarding:query",
                tenantId,
                request.PageIndex.ToString(),
                request.PageSize.ToString(),
                request.WorkflowId?.ToString() ?? "null",
                request.CurrentStageId?.ToString() ?? "null",
                request.LeadId ?? "null",
                request.Status ?? "null",
                request.Priority ?? "null",
                request.SortField ?? "createdate",
                request.SortDirection ?? "desc"
            };
            return string.Join(":", keyParts);
        }

        /// <summary>
        /// Try to get query results from cache
        /// </summary>
        private async Task<PageModelDto<OnboardingOutputDto>> TryGetCachedQueryResultAsync(string cacheKey)
        {
            try
            {
                // Redis cache temporarily disabled
                string cachedJson = null;
                if (!string.IsNullOrEmpty(cachedJson))
                {
                    var cached = JsonSerializer.Deserialize<PageModelDto<OnboardingOutputDto>>(cachedJson);
                    if (cached != null)
                    {
                        // Debug logging handled by structured logging
                        return cached;
                    }
                }
                // Debug logging handled by structured logging
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
            }
            return null;
        }

        /// <summary>
        /// Cache query results
        /// </summary>
        private async Task CacheQueryResultAsync(string cacheKey, PageModelDto<OnboardingOutputDto> result, TimeSpan expiry)
        {
            try
            {
                var json = JsonSerializer.Serialize(result);
                // Redis cache temporarily disabled
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
            }
        }

        /// <summary>
        /// Optimized total count query
        /// </summary>
        private async Task<int> GetOptimizedTotalCountAsync(ISugarQueryable<Onboarding> queryable)
        {
            // Use optimized count query, avoid complex JOINs
            return await queryable.CountAsync();
        }

        /// <summary>
        /// Apply optimized sorting
        /// </summary>
        private ISugarQueryable<Onboarding> ApplyOptimizedSorting(ISugarQueryable<Onboarding> queryable, OnboardingQueryRequest request)
        {
            switch (request.SortField?.ToLower())
            {
                case "leadname":
                    return request.SortDirection?.ToLower() == "asc"
                            ? queryable.OrderBy(x => x.LeadName)
                            : queryable.OrderByDescending(x => x.LeadName);
                case "status":
                    return request.SortDirection?.ToLower() == "asc"
                            ? queryable.OrderBy(x => x.Status)
                            : queryable.OrderByDescending(x => x.Status);
                case "completionrate":
                    return request.SortDirection?.ToLower() == "asc"
                            ? queryable.OrderBy(x => x.CompletionRate)
                            : queryable.OrderByDescending(x => x.CompletionRate);
                case "startdate":
                    return request.SortDirection?.ToLower() == "asc"
                            ? queryable.OrderBy(x => x.StartDate)
                            : queryable.OrderByDescending(x => x.StartDate);
                case "string":
                case null:
                case "":
                default:
                    return request.SortDirection?.ToLower() == "asc"
                            ? queryable.OrderBy(x => x.CreateDate)
                            : queryable.OrderByDescending(x => x.CreateDate);
            }
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
        /// Check if leads have onboarding (batch operation)
        /// </summary>
        public async Task<Dictionary<string, bool>> BatchCheckLeadOnboardingAsync(List<string> leadIds)
        {
            var result = new Dictionary<string, bool>();

            if (leadIds == null || !leadIds.Any())
            {
                return result;
            }

            try
            {
                // Batch query all Lead's Onboarding records
                var onboardings = await _onboardingRepository.GetByLeadIdsAsync(leadIds);
                var existingLeadIds = onboardings.Select(o => o.LeadId).ToHashSet();

                // Set status for all Leads
                foreach (var leadId in leadIds)
                {
                    result[leadId] = existingLeadIds.Contains(leadId);
                }

                return result;
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                // If batch query fails, set default status for all Leads
                foreach (var leadId in leadIds)
                {
                    result[leadId] = false;
                }

                return result;
            }
        }

        /// <summary>
        /// Move to next stage
        /// </summary>
        public async Task<bool> MoveToNextStageAsync(long id)
        {
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
        /// Move to previous stage
        /// </summary>
        public async Task<bool> MoveToPreviousStageAsync(long id)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            var orderedStages = stages.OrderBy(x => x.Order).ToList();
            var currentStageIndex = orderedStages.FindIndex(x => x.Id == entity.CurrentStageId);

            if (currentStageIndex <= 0)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "No previous stage available");
            }

            var previousStage = orderedStages[currentStageIndex - 1];
            return await _onboardingRepository.UpdateStageAsync(id, previousStage.Id, previousStage.Order);
        }

        /// <summary>
        /// Move to specific stage
        /// </summary>
        public async Task<bool> MoveToStageAsync(long id, MoveToStageInputDto input)
        {
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

        /// <summary>
        /// Complete current stage
        /// </summary>
        public async Task<bool> CompleteCurrentStageAsync(long id)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }
            // Debug logging handled by structured logging
            // Check if onboarding is already completed
            if (entity.Status == "Completed")
            {
                // Debug logging handled by structured logging
                return true; // Return success since the desired outcome (completion) is already achieved
            }

            // Removed validation that priority must be set before completing Stage 1
            // if (entity.CurrentStageOrder == 1 && !entity.IsPrioritySet)
            // {
            //     throw new CRMException(ErrorCodeEnum.BusinessError, "Priority must be set before completing Stage 1. Please set priority first.");
            // }

            // Get all stages for this workflow
            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            var orderedStages = stages.OrderBy(x => x.Order).ToList();
            var totalStages = orderedStages.Count;
            // Debug logging handled by structured logging
            // Debug logging handled by structured logging)}");

            // Find current stage index
            var currentStageIndex = orderedStages.FindIndex(x => x.Id == entity.CurrentStageId);
            // Debug logging handled by structured logging
            if (currentStageIndex == -1)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Current stage not found in workflow");
            }

            // Get current stage details
            var currentStage = orderedStages[currentStageIndex];
            // Debug logging handled by structured logging");

            // Check if current stage is already completed by comparing completion rate and stage progression
            var expectedCompletionRateForCurrentStage = totalStages > 0 ? Math.Round((decimal)currentStageIndex / totalStages * 100, 2) : 0;
            var expectedCompletionRateAfterCompletion = totalStages > 0 ? Math.Round((decimal)(currentStageIndex + 1) / totalStages * 100, 2) : 0;
            // Debug logging handled by structured logging
            // More precise check: if completion rate is already at or above what it should be after completing this stage,
            // check if force completion is enabled
            if (entity.CompletionRate >= expectedCompletionRateAfterCompletion)
            {
                // Debug logging handled by structured logging");
                // Debug logging handled by structured logging
                // Always allow re-completion for flexibility, just log the warning
            }

            // Additional check: if the current stage order doesn't match the expected stage based on completion rate
            var expectedCurrentStageIndex = entity.CompletionRate > 0 ? (int)Math.Floor((decimal)entity.CompletionRate / 100 * totalStages) : 0;
            if (currentStageIndex < expectedCurrentStageIndex)
            {
                // Debug logging handled by structured logging
                // Don't throw exception, just log the warning and continue
            }



            // Check if this is the last stage
            var isLastStage = currentStageIndex >= totalStages - 1;
            // Debug logging handled by structured logging
            if (isLastStage)
            {
                // Complete the entire onboarding
                // Debug logging handled by structured logging");
                entity.Status = "Completed";
                entity.CompletionRate = 100;
                entity.ActualCompletionDate = DateTimeOffset.UtcNow;

                // Update stage tracking info
                await UpdateStageTrackingInfoAsync(entity);

                var result = await SafeUpdateOnboardingAsync(entity);
                // Debug logging handled by structured logging
                // Publish stage completion event for final stage completion
                if (result)
                {
                    // Debug logging handled by structured logging
                    await PublishStageCompletionEventForCurrentStageAsync(entity, currentStage, isLastStage);
                }

                // Fire-and-forget: generate and persist AI Summary only for this onboarding (do not update Stage entity)
                try
                {
                    var opts = new StageSummaryOptions { Language = "auto", SummaryLength = "short", IncludeTaskAnalysis = true, IncludeQuestionnaireInsights = true };
                    _ = Task.Run(async () =>
                    {
                        var ai = await _stageService.GenerateAISummaryAsync(currentStage.Id, null, opts);
                        if (ai != null && ai.Success)
                        {
                            LoadStagesProgressFromJson(entity);
                            var sp = entity.StagesProgress?.FirstOrDefault(s => s.StageId == currentStage.Id);
                            if (sp != null)
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

                return result;
            }
            else
            {
                // Move to next stage
                var nextStage = orderedStages[currentStageIndex + 1];
                // Debug logging handled by structured logging");

                entity.CurrentStageId = nextStage.Id;
                entity.CurrentStageOrder = nextStage.Order;
                entity.CurrentStageStartTime = DateTimeOffset.UtcNow;

                // Update stages progress and calculate completion rate based on stage order progression
                await UpdateStagesProgressAsync(entity, currentStage.Id, GetCurrentUserName(), GetCurrentUserId(), "Stage completed via customer portal");
                LoadStagesProgressFromJson(entity);
                entity.CompletionRate = CalculateCompletionRateByStageOrder(entity.StagesProgress);

                // Fire-and-forget: generate and persist AI Summary only for this onboarding (do not update Stage entity)
                try
                {
                    var opts = new StageSummaryOptions { Language = "auto", SummaryLength = "short", IncludeTaskAnalysis = true, IncludeQuestionnaireInsights = true };
                    _ = Task.Run(async () =>
                    {
                        var ai = await _stageService.GenerateAISummaryAsync(currentStage.Id, null, opts);
                        if (ai != null && ai.Success)
                        {
                            LoadStagesProgressFromJson(entity);
                            var sp = entity.StagesProgress?.FirstOrDefault(s => s.StageId == currentStage.Id);
                            if (sp != null)
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

                // Log stage completion to Change Log
                await LogStageCompletionForCurrentStageAsync(entity, currentStage, GetCurrentUserName(), GetCurrentUserId(), "Stage completed via customer portal");

                var completedCount = entity.StagesProgress.Count(s => s.IsCompleted);
                // Debug logging handled by structured logging
                // Update status to InProgress if it was Started
                if (entity.Status == "Started")
                {
                    entity.Status = "InProgress";
                    // Debug logging handled by structured logging
                }

                // Update stage tracking info
                await UpdateStageTrackingInfoAsync(entity);

                var result = await SafeUpdateOnboardingAsync(entity);
                // Debug logging handled by structured logging
                // Publish stage completion event
                if (result)
                {
                    // Debug logging handled by structured logging
                    await PublishStageCompletionEventForCurrentStageAsync(entity, currentStage, isLastStage);
                }
                // Debug logging handled by structured logging
                return result;
            }
        }

        /// <summary>
        /// Complete specified stage with validation (supports non-sequential completion)
        /// </summary>
        public async Task<bool> CompleteCurrentStageAsync(long id, CompleteCurrentStageInputDto input)
        {
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
            try
            {
                var lang = string.IsNullOrWhiteSpace(input.Language) ? "auto" : input.Language;
                var opts = new StageSummaryOptions { Language = lang, SummaryLength = "short", IncludeTaskAnalysis = true, IncludeQuestionnaireInsights = true };
                // Try multiple attempts synchronously before falling back to placeholder
                var ai = await _stageService.GenerateAISummaryAsync(stageToComplete.Id, null, opts);
                if (ai == null || !ai.Success)
                {
                    try { await Task.Delay(1500); } catch {}
                    ai = await _stageService.GenerateAISummaryAsync(stageToComplete.Id, null, opts);
                }
                LoadStagesProgressFromJson(entity);
                var sp = entity.StagesProgress?.FirstOrDefault(s => s.StageId == stageToComplete.Id);
                if (sp != null)
                {
                    if (ai != null && ai.Success)
                    {
                        sp.AiSummary = ai.Summary;
                        sp.AiSummaryGeneratedAt = DateTime.UtcNow;
                        sp.AiSummaryConfidence = (decimal?)Convert.ToDecimal(ai.ConfidenceScore);
                        sp.AiSummaryModel = ai.ModelUsed;
                        var detailedData = new { ai.Breakdown, ai.KeyInsights, ai.Recommendations, ai.CompletionStatus, generatedAt = DateTime.UtcNow };
                        sp.AiSummaryData = System.Text.Json.JsonSerializer.Serialize(detailedData);
                    }
                    else
                    {
                        // Fallback placeholder to ensure frontend sees a value immediately; schedule retry in background
                        sp.AiSummary = "AI summary is being generated...";
                        sp.AiSummaryGeneratedAt = DateTime.UtcNow;
                        sp.AiSummaryConfidence = null;
                        sp.AiSummaryModel = null;
                        sp.AiSummaryData = null;
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                var retry = await _stageService.GenerateAISummaryAsync(stageToComplete.Id, null, opts);
                                if (retry == null || !retry.Success)
                                {
                                    try { await Task.Delay(3000); } catch {}
                                    retry = await _stageService.GenerateAISummaryAsync(stageToComplete.Id, null, opts);
                                }
                                if (retry != null && retry.Success)
                                {
                                    LoadStagesProgressFromJson(entity);
                                    var sp2 = entity.StagesProgress?.FirstOrDefault(s => s.StageId == stageToComplete.Id);
                                    if (sp2 != null)
                                    {
                                        sp2.AiSummary = retry.Summary;
                                        sp2.AiSummaryGeneratedAt = DateTime.UtcNow;
                                        sp2.AiSummaryConfidence = (decimal?)Convert.ToDecimal(retry.ConfidenceScore);
                                        sp2.AiSummaryModel = retry.ModelUsed;
                                        var dd = new { retry.Breakdown, retry.KeyInsights, retry.Recommendations, retry.CompletionStatus, generatedAt = DateTime.UtcNow };
                                        sp2.AiSummaryData = System.Text.Json.JsonSerializer.Serialize(dd);
                                        entity.StagesProgressJson = SerializeStagesProgress(entity.StagesProgress);
                                        await SafeUpdateOnboardingAsync(entity);
                                    }
                                }
                            }
                            catch { }
                        });
                    }
                    entity.StagesProgressJson = SerializeStagesProgress(entity.StagesProgress);
                    await SafeUpdateOnboardingAsync(entity);
                }
            }
            catch { /* keep non-blocking if AI fails */ }

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
                    entity.Notes = string.IsNullOrEmpty(entity.Notes)
                        ? noteText
                        : $"{entity.Notes}\n{noteText}";
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

                // If current stage is the completed stage and there's a next stage, advance to it
                if (entity.CurrentStageId == stageToComplete.Id && nextStageIndex < orderedStages.Count)
                {
                    var nextStage = orderedStages[nextStageIndex];
                    entity.CurrentStageId = nextStage.Id;
                    entity.CurrentStageOrder = nextStage.Order;
                    entity.CurrentStageStartTime = DateTimeOffset.UtcNow;
                    // Debug logging handled by structured logging
                }
                else if (entity.CurrentStageId != stageToComplete.Id)
                {
                    // If we completed a different stage (not the current one), 
                    // advance to the next incomplete stage
                    var nextIncompleteStage = orderedStages
                        .Where(stage => !entity.StagesProgress.Any(sp => sp.StageId == stage.Id && sp.IsCompleted))
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
                    entity.Notes = string.IsNullOrEmpty(entity.Notes)
                        ? noteText
                        : $"{entity.Notes}\n{noteText}";
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
        /// Complete current stage with details
        /// </summary>
        public async Task<bool> CompleteStageAsync(long id, CompleteStageInputDto input)
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

            // Get current stage info
            var currentStage = await _stageRepository.GetByIdAsync(entity.CurrentStageId ?? 0);
            if (currentStage == null)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Current stage not found");
            }

            // Validate if this stage can be completed
            var (canComplete, validationError) = await ValidateStageCanBeCompletedAsync(entity, currentStage.Id);
            if (!canComplete)
            {
                // Debug logging handled by structured logging
                throw new CRMException(ErrorCodeEnum.BusinessError, validationError);
            }
            // Debug logging handled by structured logging
            // Check if this is the last stage
            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            var orderedStages = stages.OrderBy(x => x.Order).ToList();
            var currentStageIndex = orderedStages.FindIndex(x => x.Id == entity.CurrentStageId);

            if (currentStageIndex == orderedStages.Count - 1)
            {
                // This is the last stage, complete the entire onboarding
                entity.Status = "Completed";
                entity.CompletionRate = 100;
                entity.ActualCompletionDate = DateTimeOffset.UtcNow;
            }
            else if (input.AutoMoveToNext)
            {
                // Move to next stage
                var nextStage = orderedStages[currentStageIndex + 1];
                entity.CurrentStageId = nextStage.Id;
                entity.CurrentStageOrder = nextStage.Order;
                entity.CurrentStageStartTime = DateTimeOffset.UtcNow;

                // Update completion rate based on stage order progression
                await UpdateStagesProgressAsync(entity, currentStage.Id, input.CompletedBy ?? GetCurrentUserName(), input.CompletedById ?? GetCurrentUserId(), input.CompletionNotes);

                // Calculate completion rate based on completed stages count
                LoadStagesProgressFromJson(entity);
                entity.CompletionRate = CalculateCompletionRateByCompletedStages(entity.StagesProgress);

                // Update status to InProgress if it was Started
                if (entity.Status == "Started")
                {
                    entity.Status = "InProgress";
                }
            }

            // Add completion notes to onboarding notes
            if (!string.IsNullOrEmpty(input.CompletionNotes))
            {
                var noteText = $"[Stage Completed] {currentStage.Name}: {input.CompletionNotes}";
                entity.Notes = string.IsNullOrEmpty(entity.Notes)
                    ? noteText
                    : $"{entity.Notes}\n{noteText}";
            }

            // Add rating if provided
            if (input.Rating.HasValue)
            {
                var ratingText = $"[Stage Rating] {currentStage.Name}: {input.Rating}/5 stars";
                entity.Notes = string.IsNullOrEmpty(entity.Notes)
                    ? ratingText
                    : $"{entity.Notes}\n{ratingText}";
            }

            // Add feedback if provided
            if (!string.IsNullOrEmpty(input.Feedback))
            {
                var feedbackText = $"[Stage Feedback] {currentStage.Name}: {input.Feedback}";
                entity.Notes = string.IsNullOrEmpty(entity.Notes)
                    ? feedbackText
                    : $"{entity.Notes}\n{feedbackText}";
            }

            // Update stage tracking info
            await UpdateStageTrackingInfoAsync(entity);

            var result = await SafeUpdateOnboardingAsync(entity);

            // Publish Kafka event for stage completion
            if (result)
            {
                await PublishStageCompletionEventAsync(entity, currentStage, input);
            }

            return result;
        }

        /// <summary>
        /// Complete onboarding
        /// </summary>
        public async Task<bool> CompleteAsync(long id)
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

            return await _onboardingRepository.UpdateStatusAsync(id, "Completed");
        }

        /// <summary>
        /// Pause onboarding
        /// </summary>
        public async Task<bool> PauseAsync(long id)
        {
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
                entity.Notes = $"Cancelled: {reason}. {entity.Notes}".Trim();
                await SafeUpdateOnboardingAsync(entity);
            }

            // Log cancellation to Change Log
            await LogOnboardingActionAsync(entity, "Cancel Onboarding", "onboarding_cancel", true, new
            {
                CancellationReason = reason,
                CancelledAt = DateTimeOffset.UtcNow,
                CancelledBy = GetCurrentUserName()
            });

            return await _onboardingRepository.UpdateStatusAsync(id, "Cancelled");
        }

        /// <summary>
        /// Reject onboarding application
        /// </summary>
        public async Task<bool> RejectAsync(long id, RejectOnboardingInputDto input)
        {
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

            entity.Notes = string.IsNullOrEmpty(entity.Notes)
                ? rejectionNote
                : $"{entity.Notes}\n{rejectionNote}";

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
        /// Get onboarding statistics
        /// </summary>
        public async Task<OnboardingStatisticsDto> GetStatisticsAsync()
        {
            try
            {
                var stats = await _onboardingRepository.GetStatisticsAsync();
                var statusCount = await _onboardingRepository.GetCountByStatusAsync();

                return new OnboardingStatisticsDto
                {
                    TotalCount = (int)stats["TotalCount"],
                    InProgressCount = (int)stats["InProgressCount"],
                    CompletedCount = (int)stats["CompletedCount"],
                    PausedCount = (int)stats["PausedCount"],
                    CancelledCount = (int)stats["CancelledCount"],
                    OverdueCount = (int)stats["OverdueCount"],
                    AverageCompletionRate = (decimal)stats["AverageCompletionRate"],
                    StatusStatistics = statusCount,
                    PriorityStatistics = new Dictionary<string, int>(),
                    TeamStatistics = new Dictionary<string, int>(),
                    StageStatistics = new Dictionary<string, int>()
                };
            }
            catch (Exception ex)
            {
                throw new CRMException(ErrorCodeEnum.SystemError, $"Error getting statistics: {ex.Message}");
            }
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
        /// Get overdue onboarding list
        /// </summary>
        public async Task<List<OnboardingOutputDto>> GetOverdueListAsync()
        {
            var entities = await _onboardingRepository.GetOverdueListAsync();
            return _mapper.Map<List<OnboardingOutputDto>>(entities);
        }

        /// <summary>
        /// Batch update status
        /// </summary>
        public async Task<bool> BatchUpdateStatusAsync(List<long> ids, string status)
        {
            if (!ids.Any())
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "No onboarding IDs provided");
            }

            var validStatuses = new[] { "Started", "InProgress", "Completed", "Paused", "Cancelled" };
            if (!validStatuses.Contains(status))
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Invalid status");
            }

            return await _onboardingRepository.BatchUpdateStatusAsync(ids, status);
        }

        /// <summary>
        /// Set priority for onboarding (required for Stage 1 completion)
        /// </summary>
        public async Task<bool> SetPriorityAsync(long id, string priority)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            var validPriorities = new[] { "Low", "Medium", "High", "Critical" };
            if (!validPriorities.Contains(priority))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Invalid priority. Must be one of: Low, Medium, High, Critical");
            }

            entity.Priority = priority;
            entity.IsPrioritySet = true;

            // Update stage tracking info
            await UpdateStageTrackingInfoAsync(entity);

            return await SafeUpdateOnboardingAsync(entity);
        }

        /// <summary>
        /// Update stage tracking information
        /// </summary>
        private async Task UpdateStageTrackingInfoAsync(Onboarding entity)
        {
            // Current user context - future enhancement
            // Use real user information from UserContext
            entity.StageUpdatedTime = DateTimeOffset.UtcNow;
            entity.StageUpdatedBy = GetCurrentUserName();
            entity.StageUpdatedById = GetCurrentUserId() ?? 0;
            entity.StageUpdatedByEmail = GetCurrentUserEmail();

            // Sync isCurrent flag in stagesProgress to match currentStageId
            LoadStagesProgressFromJson(entity);
            if (entity.StagesProgress != null && entity.StagesProgress.Any())
            {
                foreach (var stage in entity.StagesProgress)
                {
                    // Update isCurrent flag based on currentStageId
                    stage.IsCurrent = stage.StageId == entity.CurrentStageId;

                    // Update stage status based on completion and current status
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
        /// Get onboarding timeline
        /// </summary>
        public async Task<List<OnboardingTimelineDto>> GetTimelineAsync(long id)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            // Timeline repository implementation - future enhancement
            // For now, return a sample timeline based on onboarding data
            var timeline = new List<OnboardingTimelineDto>
            {
                new OnboardingTimelineDto
                {
                    Id = 1,
                    EventType = "Created",
                    Description = "Onboarding created",
                    EventTime = entity.CreateDate,
                    UserId = entity.CreateUserId,
                    UserName = entity.CreateBy,
                    Details = $"Onboarding created for {entity.LeadName}"
                }
            };

            if (entity.StageUpdatedTime.HasValue)
            {
                timeline.Add(new OnboardingTimelineDto
                {
                    Id = 2,
                    EventType = "StageUpdated",
                    Description = "Stage updated",
                    EventTime = entity.StageUpdatedTime.Value,
                    UserId = entity.StageUpdatedById,
                    UserName = entity.StageUpdatedBy,
                    StageId = entity.CurrentStageId,
                    Details = $"Stage updated by {entity.StageUpdatedBy}"
                });
            }

            return timeline.OrderByDescending(t => t.EventTime).ToList();
        }

        /// <summary>
        /// Add note to onboarding
        /// </summary>
        public async Task<bool> AddNoteAsync(long id, AddNoteInputDto input)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            // Add note to existing notes
            var noteText = $"[{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm}] {input.Content}";
            if (!string.IsNullOrEmpty(input.Type))
            {
                noteText = $"[{input.Type}] {noteText}";
            }

            entity.Notes = string.IsNullOrEmpty(entity.Notes)
                ? noteText
                : $"{entity.Notes}\n{noteText}";

            return await SafeUpdateOnboardingAsync(entity);
        }

        /// <summary>
        /// Update onboarding status
        /// </summary>
        public async Task<bool> UpdateStatusAsync(long id, UpdateStatusInputDto input)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            var validStatuses = new[] { "Started", "InProgress", "Completed", "Paused", "Cancelled" };
            if (!validStatuses.Contains(input.Status))
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Invalid status");
            }

            entity.Status = input.Status;

            if (!string.IsNullOrEmpty(input.Remarks))
            {
                var remarkText = $"[Status Update] {input.Remarks}";
                entity.Notes = string.IsNullOrEmpty(entity.Notes)
                    ? remarkText
                    : $"{entity.Notes}\n{remarkText}";
            }

            // Update stage tracking info
            await UpdateStageTrackingInfoAsync(entity);

            return await SafeUpdateOnboardingAsync(entity);
        }

        /// <summary>
        /// Update onboarding priority
        /// </summary>
        public async Task<bool> UpdatePriorityAsync(long id, UpdatePriorityInputDto input)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            var validPriorities = new[] { "Low", "Medium", "High", "Critical" };
            if (!validPriorities.Contains(input.Priority))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Invalid priority. Must be one of: Low, Medium, High, Critical");
            }

            entity.Priority = input.Priority;
            entity.IsPrioritySet = true;

            if (!string.IsNullOrEmpty(input.Remarks))
            {
                var remarkText = $"[Priority Update] Priority set to {input.Priority}. {input.Remarks}";
                entity.Notes = string.IsNullOrEmpty(entity.Notes)
                    ? remarkText
                    : $"{entity.Notes}\n{remarkText}";
            }

            // Update stage tracking info
            await UpdateStageTrackingInfoAsync(entity);

            return await SafeUpdateOnboardingAsync(entity);
        }

        /// <summary>
        /// Complete onboarding with details
        /// </summary>
        public async Task<bool> CompleteAsync(long id, CompleteOnboardingInputDto input)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status == "Completed")
            {
                // Debug logging handled by structured logging
                return true; // Return success since the desired outcome (completion) is already achieved
            }

            entity.Status = "Completed";
            entity.CompletionRate = 100;
            entity.ActualCompletionDate = DateTimeOffset.UtcNow;

            // Add completion notes
            if (!string.IsNullOrEmpty(input.CompletionNotes))
            {
                var noteText = $"[Completion] {input.CompletionNotes}";
                entity.Notes = string.IsNullOrEmpty(entity.Notes)
                    ? noteText
                    : $"{entity.Notes}\n{noteText}";
            }

            // Add rating if provided
            if (input.Rating.HasValue)
            {
                var ratingText = $"[Rating] {input.Rating}/5 stars";
                entity.Notes = string.IsNullOrEmpty(entity.Notes)
                    ? ratingText
                    : $"{entity.Notes}\n{ratingText}";
            }

            // Add feedback if provided
            if (!string.IsNullOrEmpty(input.Feedback))
            {
                var feedbackText = $"[Feedback] {input.Feedback}";
                entity.Notes = string.IsNullOrEmpty(entity.Notes)
                    ? feedbackText
                    : $"{entity.Notes}\n{feedbackText}";
            }

            // Update stage tracking info
            await UpdateStageTrackingInfoAsync(entity);

            return await SafeUpdateOnboardingAsync(entity);
        }

        /// <summary>
        /// Restart onboarding
        /// </summary>
        public async Task<bool> RestartAsync(long id, RestartOnboardingInputDto input)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status == "InProgress")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Onboarding is already in progress");
            }

            entity.Status = "InProgress";
            entity.ActualCompletionDate = null;

            if (input.ResetProgress)
            {
                entity.CurrentStageId = null;
                entity.CurrentStageOrder = 0;
                entity.CompletionRate = 0;
                entity.CurrentStageStartTime = DateTimeOffset.UtcNow;
            }

            // Add restart notes
            var restartText = $"[Restart] Onboarding restarted";
            if (!string.IsNullOrEmpty(input.Reason))
            {
                restartText += $" - Reason: {input.Reason}";
            }
            if (!string.IsNullOrEmpty(input.Notes))
            {
                restartText += $" - Notes: {input.Notes}";
            }

            entity.Notes = string.IsNullOrEmpty(entity.Notes)
                ? restartText
                : $"{entity.Notes}\n{restartText}";

            // Update stage tracking info
            await UpdateStageTrackingInfoAsync(entity);

            return await SafeUpdateOnboardingAsync(entity);
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
                    ActionBy = GetCurrentUserName(),
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
        /// Log task completion to change log
        /// </summary>
        private async Task LogTaskCompletionAsync(long onboardingId, long stageId, string stageName, long taskId, string taskName, bool isCompleted, string completionNotes = "", string completedBy = null)
        {
            try
            {
                var logData = new
                {
                    OnboardingId = onboardingId,
                    StageId = stageId,
                    StageName = stageName,
                    TaskId = taskId,
                    TaskName = taskName,
                    IsCompleted = isCompleted,
                    CompletionNotes = completionNotes,
                    CompletedTime = DateTimeOffset.UtcNow,
                    CompletedBy = completedBy ?? GetCurrentUserName(),
                    Action = isCompleted ? "Task Completed" : "Task Marked Incomplete"
                };

                // Stage completion log functionality removed
                // Debug logging handled by structured logging}");
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
            }
        }

        /// <summary>
        /// Log stage completion for current stage async
        /// </summary>
        private async Task LogStageCompletionForCurrentStageAsync(Onboarding onboarding, Stage stage, string completedBy, long? completedById, string completionNotes)
        {
            try
            {
                // Get real user information, use default if parameter not provided
                var actualCompletedBy = !string.IsNullOrEmpty(completedBy) ? completedBy : GetCurrentUserName();
                var actualCompletedById = completedById ?? GetCurrentUserId();

                var logData = new
                {
                    OnboardingId = onboarding.Id,
                    LeadId = onboarding.LeadId,
                    LeadName = onboarding.LeadName,
                    StageId = stage.Id,
                    StageName = stage.Name,
                    StageOrder = stage.Order,
                    CompletionNotes = completionNotes,
                    CompletedTime = DateTimeOffset.UtcNow,
                    CompletedBy = actualCompletedBy,
                    CompletedById = actualCompletedById,
                    CompletionMethod = "Manual",
                    PreviousStatus = "InProgress",
                    NewStatus = "Completed",
                    CompletionRate = onboarding.CompletionRate,
                    WorkflowId = onboarding.WorkflowId,
                    Priority = onboarding.Priority
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
        /// Publish stage completion event
        /// </summary>
        private async Task PublishStageCompletionEventAsync(Onboarding onboarding, Stage stage, CompleteStageInputDto input)
        {
            try
            {
                // Get next stage info if auto-moving
                string nextStageName = null;
                if (input.AutoMoveToNext && onboarding.CurrentStageId.HasValue)
                {
                    var nextStage = await _stageRepository.GetByIdAsync(onboarding.CurrentStageId.Value);
                    nextStageName = nextStage?.Name;
                }

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
                    CompletedStageId = stage.Id,
                    CompletedStageName = stage.Name,
                    StageCategory = stage.Name ?? "Unknown",
                    NextStageId = input.AutoMoveToNext ? onboarding.CurrentStageId : null,
                    NextStageName = nextStageName,
                    CompletionRate = onboarding.CompletionRate,
                    IsFinalStage = onboarding.Status == "Completed",
                    AssigneeName = GetCurrentUserFullName(),
                    ResponsibleTeam = onboarding.CurrentTeam ?? "Default",
                    Priority = onboarding.Priority ?? "Medium",
                    Source = "CustomerPortal",
                    BusinessContext = new Dictionary<string, object>
                    {
                        ["CompletionNotes"] = input.CompletionNotes ?? "",
                        ["Rating"] = input.Rating?.ToString() ?? "",
                        ["Feedback"] = input.Feedback ?? "",
                        ["AutoMoveToNext"] = input.AutoMoveToNext
                    },
                    RoutingTags = new List<string> { "onboarding", "stage-completion", "customer-portal" },
                    Description = $"Stage '{stage.Name}' completed for Onboarding {onboarding.Id}",
                    Tags = new List<string> { "onboarding", "stage-completion", "unknown" }
                };

                await _mediator.Publish(onboardingStageCompletedEvent);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the stage completion
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
                    AssigneeName = onboarding.CurrentAssigneeName ?? GetCurrentUserFullName(),
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
                    Tags = new List<string> { "onboarding", "stage-completion", "auto-progression" }
                };
                // Debug logging handled by structured logging
                await _mediator.Publish(onboardingStageCompletedEvent);
                // Debug logging handled by structured logging
            }
            catch (Exception ex)
            {
                // Log error but don't fail the stage completion
                // Debug logging handled by structured logging
            }
        }

        /// <summary>
        /// Export onboarding data to CSV
        /// </summary>
        public async Task<Stream> ExportToCsvAsync(OnboardingQueryRequest query)
        {
            // Get data using existing query method
            var result = await QueryAsync(query);
            var data = result.Data;

            // Transform to export format
            var exportData = data.Select(item => new OnboardingExportDto
            {
                Id = item.LeadId,
                CompanyName = item.LeadName,
                LifeCycleStage = item.LifeCycleStageName,
                WorkFlow = item.WorkflowName,
                OnboardStage = item.CurrentStageName,
                Priority = item.Priority,
                Timeline = item.StartDate.HasValue ? $"Start: {item.StartDate.Value.ToString("MM/dd/yyyy")}" : "",
                // Keep consistency with frontend index.vue display logic:
                // Updated By => stageUpdatedBy || modifyBy
                // Update Time => stageUpdatedTime || modifyDate
                UpdatedBy = string.IsNullOrWhiteSpace(item.StageUpdatedBy) ? item.ModifyBy : item.StageUpdatedBy,
                UpdateTime = (item.StageUpdatedTime.HasValue ? item.StageUpdatedTime.Value : item.ModifyDate)
                    .ToString("MM/dd/yyyy HH:mm:ss")
            }).ToList();

            // Generate CSV content
            var csvContent = new StringBuilder();
            csvContent.AppendLine("Lead ID,Company/Contact Name,Life Cycle Stage,Onboard Workflow,Onboard Stage,Priority,Timeline,Updated By,Update Time");

            foreach (var item in exportData)
            {
                csvContent.AppendLine($"\"{item.Id}\"," +
                    $"\"{item.CompanyName?.Replace("\"", "\"\"")}\"," +
                    $"\"{item.LifeCycleStage?.Replace("\"", "\"\"")}\"," +
                    $"\"{item.WorkFlow?.Replace("\"", "\"\"")}\"," +
                    $"\"{item.OnboardStage?.Replace("\"", "\"\"")}\"," +
                    $"\"{item.Priority?.Replace("\"", "\"\"")}\"," +
                    $"\"{item.Timeline?.Replace("\"", "\"\"")}\"," +
                    $"\"{item.UpdatedBy?.Replace("\"", "\"\"")}\"," +
                    $"\"{item.UpdateTime}\"");
            }

            // Convert to stream
            var bytes = Encoding.UTF8.GetBytes(csvContent.ToString());
            return new MemoryStream(bytes);
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
                Id = item.LeadId,
                CompanyName = item.LeadName,
                LifeCycleStage = item.LifeCycleStageName,
                WorkFlow = item.WorkflowName,
                OnboardStage = item.CurrentStageName,
                Priority = item.Priority,
                Timeline = item.StartDate.HasValue ? $"Start: {item.StartDate.Value.ToString("MM/dd/yyyy")}" : "",
                UpdatedBy = string.IsNullOrWhiteSpace(item.StageUpdatedBy) ? item.ModifyBy : item.StageUpdatedBy,
                UpdateTime = (item.StageUpdatedTime.HasValue ? item.StageUpdatedTime.Value : item.ModifyDate)
                  .ToString("MM/dd/yyyy HH:mm:ss")
            }).ToList();
            // Use EPPlus to generate Excel file (avoid NPOI version conflict)
            return GenerateExcelWithEPPlus(exportData);
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
                "Lead ID", "Company/Contact Name", "Life Cycle Stage", "Onboard Workflow", "Onboard Stage",
                "Priority", "Timeline", "Updated By", "Update Time"
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
                worksheet.Cells[row + 2, 1].Value = item.Id;
                worksheet.Cells[row + 2, 2].Value = item.CompanyName;
                worksheet.Cells[row + 2, 3].Value = item.LifeCycleStage;
                worksheet.Cells[row + 2, 4].Value = item.WorkFlow;
                worksheet.Cells[row + 2, 5].Value = item.OnboardStage;
                worksheet.Cells[row + 2, 6].Value = item.Priority;
                worksheet.Cells[row + 2, 7].Value = item.Timeline;
                worksheet.Cells[row + 2, 8].Value = item.UpdatedBy;
                worksheet.Cells[row + 2, 9].Value = item.UpdateTime;
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// Sync stages progress from workflow stages configuration
        /// Updates existing stages and adds new stages from workflow
        /// </summary>
        public async Task<bool> SyncStagesProgressAsync(long id)
        {
            try
            {
                var entity = await _onboardingRepository.GetByIdAsync(id);
                if (entity == null || !entity.IsValid)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
                }

                // Get current workflow stages
                var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
                if (stages == null || !stages.Any())
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, "No stages found for workflow");
                }

                // Load current stages progress
                LoadStagesProgressFromJson(entity);

                if (entity.StagesProgress == null || !entity.StagesProgress.Any())
                {
                    // If no stages progress exists, initialize it
                    await InitializeStagesProgressAsync(entity, stages);
                }
                else
                {
                    // Sync with workflow stages (handle new stages addition)
                    await SyncStagesProgressWithWorkflowAsync(entity);

                    // Note: Dynamic fields are now populated via EnrichStagesProgressWithStageDataAsync
                    // This method focuses on essential progress data only
                }

                // Enrich with stage data
                await EnrichStagesProgressWithStageDataAsync(entity);

                // Update tracking info
                await UpdateStageTrackingInfoAsync(entity);

                // Save changes
                return await SafeUpdateOnboardingAsync(entity);
            }
            catch (Exception ex)
            {
                throw new CRMException(ErrorCodeEnum.SystemError, $"Error syncing stages progress for onboarding {id}: {ex.Message}");
            }
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
                        StartTime = sequentialOrder == 1 ? currentTime : null, // First stage starts immediately
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
                Console.WriteLine($"[DEBUG] UpdateStagesProgressAsync - After LoadStagesProgressFromJson:");
                Console.WriteLine($"[DEBUG] StagesProgress count: {entity.StagesProgress?.Count ?? 0}");
                if (entity.StagesProgress != null)
                {
                    foreach (var sp in entity.StagesProgress)
                    {
                        Console.WriteLine($"[DEBUG] StageProgress: StageId={sp.StageId}, Status={sp.Status}, IsCurrent={sp.IsCurrent}");
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
                    completedStage.CompletedBy = completedBy ?? GetCurrentUserName();
                    completedStage.CompletedById = completedById ?? GetCurrentUserId();
                    completedStage.IsCurrent = false;
                    completedStage.LastUpdatedTime = currentTime;
                    completedStage.LastUpdatedBy = completedBy ?? GetCurrentUserName();

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
                        nextStage.StartTime = currentTime;
                        nextStage.IsCurrent = true;
                        nextStage.LastUpdatedTime = currentTime;
                        nextStage.LastUpdatedBy = completedBy ?? GetCurrentUserName();

                        // Debug logging handled by structured logging");
                    }
                    else
                    {
                        // Check if there are any incomplete stages with lower order (in case of non-sequential completion)
                        var earlierIncompleteStage = entity.StagesProgress
                            .Where(s => !s.IsCompleted)
                            .OrderBy(s => s.StageOrder)
                            .FirstOrDefault();

                        if (earlierIncompleteStage != null)
                        {
                            // Activate the earliest incomplete stage
                            earlierIncompleteStage.Status = "InProgress";
                            earlierIncompleteStage.StartTime = currentTime;
                            earlierIncompleteStage.IsCurrent = true;
                            earlierIncompleteStage.LastUpdatedTime = currentTime;
                            earlierIncompleteStage.LastUpdatedBy = completedBy ?? GetCurrentUserName();

                            // Debug logging handled by structured logging");
                        }
                        else
                        {
                            // Debug logging handled by structured logging
                        }
                    }

                    // Update completion rate based on completed stages
                    entity.CompletionRate = CalculateCompletionRateByCompletedStages(entity.StagesProgress);

                    // Debug logging handled by structured logging");
                }

                // Serialize back to JSON (only progress fields)
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
                    Console.WriteLine($"[DEBUG] LoadStagesProgressFromJson - Input JSON:");
                    Console.WriteLine($"[DEBUG] {entity.StagesProgressJson}");

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
                    Console.WriteLine($"[DEBUG] LoadStagesProgressFromJson - Loaded {entity.StagesProgress.Count} items:");
                    foreach (var sp in entity.StagesProgress)
                    {
                        Console.WriteLine($"[DEBUG] Loaded StageProgress: StageId={sp.StageId}, Status={sp.Status}, IsCurrent={sp.IsCurrent}");
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
                Console.WriteLine($"JSON parsing error in LoadStagesProgressFromJson: {jsonEx.Message}");
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
        /// Validate stage completion order - ensures stages are completed in sequence
        /// </summary>
        private bool ValidateStageCompletionOrder(List<OnboardingStageProgress> stagesProgress, OnboardingStageProgress stageToComplete)
        {
            try
            {
                if (stagesProgress == null || !stagesProgress.Any() || stageToComplete == null)
                {
                    return false;
                }

                // If this is the first stage (order 1), it can always be completed
                if (stageToComplete.StageOrder == 1)
                {
                    // Debug logging handled by structured logging
                    return true;
                }

                // Check if all previous stages are completed
                var previousStages = stagesProgress
                    .Where(s => s.StageOrder < stageToComplete.StageOrder)
                    .OrderBy(s => s.StageOrder)
                    .ToList();

                var incompleteStages = previousStages.Where(s => !s.IsCompleted).ToList();

                if (incompleteStages.Any())
                {
                    // Debug logging handled by structured logging
                    foreach (var incompleteStage in incompleteStages)
                    {
                        // Debug logging handled by structured logging is {incompleteStage.Status}");
                    }
                    return false;
                }
                // Debug logging handled by structured logging
                return true;
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                return false;
            }
        }

        /// <summary>
        /// Calculate completion rate based on stage order progression
        /// This method ensures that progress is calculated correctly even when stage orders are not consecutive (e.g., 1, 3, 4, 5)
        /// </summary>
        private decimal CalculateCompletionRateByStageOrder(List<OnboardingStageProgress> stagesProgress)
        {
            try
            {
                if (stagesProgress == null || !stagesProgress.Any())
                {
                    return 0;
                }

                // Sort stages by order to ensure correct progression calculation
                var orderedStages = stagesProgress.OrderBy(s => s.StageOrder).ToList();

                // Get all unique stage orders
                var stageOrders = orderedStages.Select(s => s.StageOrder).Distinct().OrderBy(o => o).ToList();

                if (!stageOrders.Any())
                {
                    return 0;
                }

                // Calculate completion based on stage order progression
                var completedStageOrders = orderedStages
                    .Where(s => s.IsCompleted)
                    .Select(s => s.StageOrder)
                    .Distinct()
                    .OrderBy(o => o)
                    .ToList();

                if (!completedStageOrders.Any())
                {
                    return 0;
                }

                // Find the highest completed stage order
                var highestCompletedOrder = completedStageOrders.Max();

                // Calculate progress based on stage order position
                var totalStageOrderRange = stageOrders.Max() - stageOrders.Min() + 1;
                var completedStageOrderRange = highestCompletedOrder - stageOrders.Min() + 1;

                // Alternative calculation: based on completed stages count vs total stages count
                var completedStagesCount = completedStageOrders.Count;
                var totalStagesCount = stageOrders.Count;

                // Use the more accurate calculation method
                decimal completionRate;

                if (totalStagesCount > 0)
                {
                    // Method 1: Simple count-based calculation (more intuitive)
                    completionRate = Math.Round((decimal)completedStagesCount / totalStagesCount * 100, 2);
                    // Debug logging handled by structured logging
                    // Debug logging handled by structured logging}]");
                    // Debug logging handled by structured logging}]");
                    // Debug logging handled by structured logging
                }
                else
                {
                    completionRate = 0;
                }

                return completionRate;
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                return 0;
            }
        }

        /// <summary>
        /// Get TenantId from onboarding
        /// </summary>
        private async Task<string> GetTenantIdFromOnboardingAsync(long onboardingId)
        {
            try
            {
                var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
                return onboarding?.TenantId ?? "default";
            }
            catch
            {
                return "default";
            }
        }

        /// <summary>
        /// Get current user name from UserContext
        /// </summary>
        private string GetCurrentUserName()
        {
            return !string.IsNullOrEmpty(_userContext?.UserName) ? _userContext.UserName : "System";
        }

        /// <summary>
        /// Get current user email from UserContext
        /// </summary>
        private string GetCurrentUserEmail()
        {
            return !string.IsNullOrEmpty(_userContext?.Email) ? _userContext.Email : "system@example.com";
        }

        /// <summary>
        /// Get current user ID from UserContext
        /// </summary>
        private long? GetCurrentUserId()
        {
            if (long.TryParse(_userContext?.UserId, out long userId))
            {
                return userId;
            }
            return null;
        }

        /// <summary>
        /// Get current user full name from UserContext
        /// </summary>
        private string GetCurrentUserFullName()
        {
            if (_userContext != null)
            {
                var fullName = $"{_userContext.FirstName} {_userContext.LastName}".Trim();
                if (!string.IsNullOrEmpty(fullName))
                {
                    return fullName;
                }
                return !string.IsNullOrEmpty(_userContext.UserName) ? _userContext.UserName : "System";
            }
            return "System";
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
        /// Batch clear all workflow-related cache
        /// </summary>
        private async Task ClearWorkflowRelatedCacheAsync(long workflowId)
        {
            try
            {
                // Safely get tenant ID
                var tenantId = !string.IsNullOrEmpty(_userContext?.TenantId)
                    ? _userContext.TenantId.ToLowerInvariant()
                    : "default";

                // Clear workflow cache
                var workflowCacheKey = $"{WORKFLOW_CACHE_PREFIX}:{tenantId}:{workflowId}";
                // Redis cache temporarily disabled
                await Task.CompletedTask;

                // Get all stages under this workflow and clear cache
                var stages = await _stageRepository.GetByWorkflowIdAsync(workflowId);
                var stageCacheTasks = stages.Select(stage =>
                {
                    var stageCacheKey = $"{STAGE_CACHE_PREFIX}:{tenantId}:{stage.Id}";
                    // Redis cache temporarily disabled
                    return Task.CompletedTask;
                });

                await Task.WhenAll(stageCacheTasks);

#if DEBUG
                // Debug logging handled by structured logging
#endif
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
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
                        stageProgress.EstimatedDays = stage.EstimatedDuration;
                        stageProgress.VisibleInPortal = stage.VisibleInPortal;
                        stageProgress.AttachmentManagementNeeded = stage.AttachmentManagementNeeded;
                        stageProgress.ComponentsJson = stage.ComponentsJson;
                        stageProgress.Components = stage.Components;
                        // AI Summary 回填策略：仅当 Onboarding 的该阶段为空时，采用 Stage 的AI字段做一次性填充，不覆盖已有值
                        if (string.IsNullOrWhiteSpace(stageProgress.AiSummary) && !string.IsNullOrWhiteSpace(stage.AiSummary))
                        {
                            stageProgress.AiSummary = stage.AiSummary;
                            stageProgress.AiSummaryGeneratedAt = stage.AiSummaryGeneratedAt;
                            stageProgress.AiSummaryConfidence = stage.AiSummaryConfidence;
                            stageProgress.AiSummaryModel = stage.AiSummaryModel;
                            stageProgress.AiSummaryData = stage.AiSummaryData;
                        }

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
                                _ = Task.Run(async () =>
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
                                    _ = Task.Run(async () =>
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
        /// Safely update onboarding entity with JSONB compatibility
        /// This method handles the JSONB type conversion issue for stages_progress_json
        /// </summary>
        private async Task<bool> SafeUpdateOnboardingAsync(Onboarding entity)
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
                        it.CustomFieldsJson,
                        it.Notes,
                        it.IsActive,
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
                        Console.WriteLine($"Warning: Failed to update stages_progress_json: {progressEx.Message}");
                        // Try alternative approach with parameter substitution
                        try
                        {
                            var escapedJson = entity.StagesProgressJson.Replace("'", "''");
                            var directSql = $"UPDATE ff_onboarding SET stages_progress_json = '{escapedJson}'::jsonb WHERE id = {entity.Id}";
                            await db.Ado.ExecuteCommandAsync(directSql);
                        }
                        catch (Exception directEx)
                        {
                            Console.WriteLine($"Error: Both parameterized and direct JSONB update failed: {directEx.Message}");
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
                Console.WriteLine($"[DEBUG] SerializeStagesProgress - Input data:");
                foreach (var sp in stagesProgress)
                {
                    Console.WriteLine($"[DEBUG] Input StageProgress: StageId={sp.StageId}, Status={sp.Status}, IsCurrent={sp.IsCurrent}");
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
                Console.WriteLine($"[DEBUG] SerializeStagesProgress - Final JSON result:");
                Console.WriteLine($"[DEBUG] {result}");

                return result;
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                return "[]";
            }
        }

        /// <summary>
        /// Query onboardings by stage status using JSONB operators
        /// Utilizes PostgreSQL JSONB querying capabilities for efficient filtering
        /// </summary>
        public async Task<List<OnboardingOutputDto>> QueryByStageStatusAsync(string status)
        {
            try
            {
                // Use JSONB containment operator for efficient querying
                var sql = @"
                    SELECT * FROM ff_onboarding 
                    WHERE is_valid = true 
                    AND stages_progress_json @> '[{""status"": """ + status + @"""}]'
                    ORDER BY create_date DESC
                ";

                var db = _onboardingRepository.GetSqlSugarClient();
                var entities = await db.Ado.SqlQueryAsync<Onboarding>(sql);

                // Load and enrich stages progress for each entity
                foreach (var entity in entities)
                {
                    LoadStagesProgressFromJson(entity);
                    await SyncStagesProgressWithWorkflowAsync(entity);
                    await EnrichStagesProgressWithStageDataAsync(entity);
                }

                return _mapper.Map<List<OnboardingOutputDto>>(entities);
            }
            catch (Exception ex)
            {
                throw new CRMException(ErrorCodeEnum.SystemError, $"Error querying onboardings by stage status {status}: {ex.Message}");
            }
        }

        /// <summary>
        /// Query onboardings by completion status using JSONB operators
        /// </summary>
        public async Task<List<OnboardingOutputDto>> QueryByCompletionStatusAsync(bool isCompleted)
        {
            try
            {
                // Use JSONB containment operator for efficient querying
                var sql = @"
                    SELECT * FROM ff_onboarding 
                    WHERE is_valid = true 
                    AND stages_progress_json @> '[{""isCompleted"": " + isCompleted.ToString().ToLower() + @"}]'
                    ORDER BY create_date DESC
                ";

                var db = _onboardingRepository.GetSqlSugarClient();
                var entities = await db.Ado.SqlQueryAsync<Onboarding>(sql);

                // Load and enrich stages progress for each entity
                foreach (var entity in entities)
                {
                    LoadStagesProgressFromJson(entity);
                    await SyncStagesProgressWithWorkflowAsync(entity);
                    await EnrichStagesProgressWithStageDataAsync(entity);
                }

                return _mapper.Map<List<OnboardingOutputDto>>(entities);
            }
            catch (Exception ex)
            {
                throw new CRMException(ErrorCodeEnum.SystemError, $"Error querying onboardings by completion status {isCompleted}: {ex.Message}");
            }
        }

        /// <summary>
        /// Query onboardings by specific stage ID using JSONB operators
        /// </summary>
        public async Task<List<OnboardingOutputDto>> QueryByStageIdAsync(long stageId)
        {
            try
            {
                // Use JSONB containment operator for efficient querying
                var sql = @"
                    SELECT * FROM ff_onboarding 
                    WHERE is_valid = true 
                    AND stages_progress_json @> '[{""stageId"": " + stageId + @"}]'
                    ORDER BY create_date DESC
                ";

                var db = _onboardingRepository.GetSqlSugarClient();
                var entities = await db.Ado.SqlQueryAsync<Onboarding>(sql);

                // Load and enrich stages progress for each entity
                foreach (var entity in entities)
                {
                    LoadStagesProgressFromJson(entity);
                    await SyncStagesProgressWithWorkflowAsync(entity);
                    await EnrichStagesProgressWithStageDataAsync(entity);
                }

                return _mapper.Map<List<OnboardingOutputDto>>(entities);
            }
            catch (Exception ex)
            {
                throw new CRMException(ErrorCodeEnum.SystemError, $"Error querying onboardings by stage ID {stageId}: {ex.Message}");
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
    }
}