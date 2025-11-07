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
    /// Onboarding service - CRUD operations
    /// </summary>
    public partial class OnboardingService
    {
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

                // 娣诲姞璋冭瘯鏃ュ織 - 妫€鏌?CurrentStageId 鏄惁姝ｇ‘璁剧疆
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
                            $"Cannot change workflow for a case with status '{entity.Status}'.");
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

                // currentStageStartTime 鍙彇 startTime锛堟棤鍒欎负null锛?
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
                        // currentStageEndTime 浼樺厛绾? customEndTime > endTime > (startTime+estimatedDays) > null
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
                            // 涓夌骇浼樺厛锛歫son.customEstimatedDays > json.estimatedDays > stage瀹炰綋
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
                // 鍗曠嫭鎺ㄧ畻 currentStageEndTime鈥斺€斾粎褰搒tartTime鍜宔stimatedDays閮藉瓨鍦?
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

                        // Get permission for this stage (STRICT MODE: Workflow 鈭?Stage)
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
    }
}

