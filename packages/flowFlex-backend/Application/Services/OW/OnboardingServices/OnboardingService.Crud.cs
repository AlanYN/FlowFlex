using AutoMapper;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.Dtos.OW.Permission;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices.OW.ChangeLog;
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
using Microsoft.Extensions.Logging;


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
                    // Note: Lead ID duplicate check removed - allow multiple onboardings with same Lead ID
                    // If there's a unique constraint violation, it's for other fields (e.g., case_code)

                    // Final fallback: manual SQL with minimal fields
                    // Debug logging handled by structured logging
                    try
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
                        // Debug logging handled by structured logging
                        insertedId = await sqlSugarClient.Ado.SqlQuerySingleAsync<long>(sql, parameters);
                        // Debug logging handled by structured logging
                    }
                    catch (Exception sqlEx)
                    {
                        // Debug logging handled by structured logging
                        // Note: Lead ID duplicate check removed - allow multiple onboardings with same Lead ID

                        throw new CRMException(ErrorCodeEnum.SystemError,
                            $"All insertion methods failed. Simple insert: {insertEx.Message}, Minimal SQL: {sqlEx.Message}");
                    }
                }

                // Step 9: Post-creation processing
                if (insertedId > 0)
                {
                    // Initialize stages progress and create user invitation
                    await ProcessPostCreationAsync(insertedId, stages.ToList());

                    // Clear query cache (async execution)
                    _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                    {
                        try { await ClearOnboardingQueryCacheAsync(); }
                        catch (Exception ex) { _logger.LogWarning(ex, "Failed to clear query cache after create"); }
                    });

                    // Log creation in background
                    QueueCreationLogging(insertedId);
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

                // Store original values for static field sync comparison and logging
                var originalLeadId = entity.LeadId;
                var originalCaseName = entity.CaseName;
                var originalContactPerson = entity.ContactPerson;
                var originalContactEmail = entity.ContactEmail;
                var originalLeadPhone = entity.LeadPhone;
                var originalLifeCycleStageId = entity.LifeCycleStageId;
                var originalPriority = entity.Priority;
                var originalLifeCycleStageName = entity.LifeCycleStageName;
                var originalOwnership = entity.Ownership;
                var originalCurrentStageId = entity.CurrentStageId;
                var originalViewPermissionMode = entity.ViewPermissionMode;
                var originalViewTeams = entity.ViewTeams;
                var originalViewUsers = entity.ViewUsers;
                var originalViewPermissionSubjectType = entity.ViewPermissionSubjectType;
                var originalOperateTeams = entity.OperateTeams;
                var originalOperateUsers = entity.OperateUsers;
                var originalOperatePermissionSubjectType = entity.OperatePermissionSubjectType;
                var originalOwnershipName = entity.OwnershipName;

                // Get workflow name for beforeData
                string beforeWorkflowName = null;
                try
                {
                    var beforeWorkflow = await _workflowRepository.GetByIdAsync(entity.WorkflowId);
                    beforeWorkflowName = beforeWorkflow?.Name;
                }
                catch
                {
                    // Ignore if workflow not found
                }

                // Prepare beforeData for logging
                var beforeData = JsonSerializer.Serialize(new
                {
                    CaseName = entity.CaseName,
                    CaseCode = entity.CaseCode,
                    WorkflowId = entity.WorkflowId,
                    WorkflowName = beforeWorkflowName,
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
                            originalCaseName,
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
                        _logger.LogDebug("Static field sync skipped - No stage found for Onboarding {OnboardingId}", entity.Id);
                    }

                    // Log onboarding update
                    _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                    {
                        try
                        {
                            // Get workflow name for afterData
                            string afterWorkflowName = null;
                            try
                            {
                                var afterWorkflow = await _workflowRepository.GetByIdAsync(entity.WorkflowId);
                                afterWorkflowName = afterWorkflow?.Name;
                            }
                            catch
                            {
                                // Ignore if workflow not found
                            }

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
                            if (originalCaseName != entity.CaseName) changedFields.Add("CaseName");
                            if (originalContactPerson != entity.ContactPerson) changedFields.Add("ContactPerson");
                            if (originalContactEmail != entity.ContactEmail) changedFields.Add("ContactEmail");
                            if (originalPriority != entity.Priority) changedFields.Add("Priority");
                            // Skip LifeCycleStageId - don't log this field
                            if (originalLifeCycleStageName != entity.LifeCycleStageName) changedFields.Add("LifeCycleStageName");
                            if (originalWorkflowId != entity.WorkflowId) changedFields.Add("WorkflowId");
                            if (originalOwnership != entity.Ownership) changedFields.Add("Ownership");
                            // Skip CurrentStageId - don't log this field
                            if (originalViewPermissionMode != entity.ViewPermissionMode) changedFields.Add("ViewPermissionMode");
                            if (originalViewTeams != entity.ViewTeams) changedFields.Add("ViewTeams");
                            if (originalViewUsers != entity.ViewUsers) changedFields.Add("ViewUsers");
                            if (originalViewPermissionSubjectType != entity.ViewPermissionSubjectType) changedFields.Add("ViewPermissionSubjectType");
                            if (originalOperateTeams != entity.OperateTeams) changedFields.Add("OperateTeams");
                            if (originalOperateUsers != entity.OperateUsers) changedFields.Add("OperateUsers");
                            if (originalOperatePermissionSubjectType != entity.OperatePermissionSubjectType) changedFields.Add("OperatePermissionSubjectType");

                            await _onboardingLogService.LogOnboardingUpdateAsync(
                                entity.Id,
                                entity.CaseName ?? entity.CaseCode ?? "Unknown",
                                beforeData: beforeData,
                                afterData: afterData,
                                changedFields: changedFields
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to log onboarding update operation for onboarding {OnboardingId}", entity.Id);
                        }
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
                // Log onboarding deletion
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

                await ClearOnboardingQueryCacheAsync();
                await ClearRelatedCacheAsync(entity.WorkflowId, entity.CurrentStageId);
            }

            return result;
        }

        /// <summary>
        /// Get onboarding by ID
        /// Returns null if not found (instead of throwing exception)
        /// </summary>
        public async Task<OnboardingOutputDto?> GetByIdAsync(long id)
        {
            try
            {
                var entity = await _onboardingRepository.GetByIdAsync(id);
                if (entity == null)
                {
                    return null;
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
                            _logger.LogDebug("GetByIdAsync - Updated database with CurrentStageId {CurrentStageId} for Onboarding {OnboardingId}", 
                                entity.CurrentStageId, id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "GetByIdAsync - Failed to update CurrentStageId in database for Onboarding {OnboardingId}", id);
                        }
                    }
                }

                // currentStageStartTime 鍙彇 startTime锛堟棤鍒欎负null锛?
                result.CurrentStageStartTime = null;
                result.CurrentStageEndTime = null;
                double? estimatedDays = null;
                
                // OPTIMIZATION: Fetch current stage once to avoid N+1 queries
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
                                if ((!estimatedDays.HasValue || estimatedDays.Value <= 0) && currentStage != null)
                                {
                                    if (currentStage.EstimatedDuration != null && currentStage.EstimatedDuration > 0)
                                        estimatedDays = (double?)currentStage.EstimatedDuration;
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

                // Get current stage name and estimated days (using pre-fetched stage from above)
                if (entity.CurrentStageId.HasValue && currentStage != null)
                {
                    result.CurrentStageName = currentStage.Name;

                    // IMPORTANT: Priority for EstimatedDays: customEstimatedDays > stage.EstimatedDuration
                    var currentStageProgress = result.StagesProgress?.FirstOrDefault(sp => sp.StageId == entity.CurrentStageId.Value);
                    if (currentStageProgress != null && currentStageProgress.CustomEstimatedDays.HasValue && currentStageProgress.CustomEstimatedDays.Value > 0)
                    {
                        result.CurrentStageEstimatedDays = currentStageProgress.CustomEstimatedDays.Value;
                        _logger.LogDebug("GetByIdAsync - Using customEstimatedDays {EstimatedDays} for Stage {StageId}", 
                            result.CurrentStageEstimatedDays, entity.CurrentStageId);
                    }
                    else
                    {
                        result.CurrentStageEstimatedDays = currentStage.EstimatedDuration;
                    }
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

                    // Apply Required Stage constraint: 
                    // For each stage, if there's any preceding required stage that is not completed,
                    // the current stage should have canOperate = false
                    var orderedStages = result.StagesProgress.OrderBy(s => s.StageOrder).ToList();
                    
                    foreach (var stageProgress in orderedStages)
                    {
                        // Check if any preceding required stage is not completed
                        var hasBlockingRequiredStage = orderedStages
                            .Where(s => s.StageOrder < stageProgress.StageOrder && s.Required && !s.IsCompleted)
                            .Any();
                        
                        if (hasBlockingRequiredStage && stageProgress.Permission != null && stageProgress.Permission.CanOperate)
                        {
                            stageProgress.Permission = new Application.Contracts.Dtos.OW.Permission.PermissionInfoDto
                            {
                                CanView = stageProgress.Permission.CanView,
                                CanOperate = false,
                                ErrorMessage = stageProgress.Permission.ErrorMessage
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

            // Try to get default workflow
            var defaultWorkflow = await _workflowRepository.GetDefaultWorkflowAsync();
            if (defaultWorkflow != null && defaultWorkflow.IsValid && defaultWorkflow.IsActive)
            {
                return defaultWorkflow.Id;
            }

            // Fallback to first active workflow
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
            entity.Status = string.IsNullOrEmpty(entity.Status) ? "Inactive" : entity.Status;
            entity.StartDate = entity.StartDate ?? DateTimeOffset.UtcNow;
            entity.CurrentStageStartTime = null; // Only set when onboarding is started
            entity.CompletionRate = 0;
            entity.IsPrioritySet = false;
            entity.Priority = string.IsNullOrEmpty(entity.Priority) ? "Medium" : entity.Priority;
            entity.IsActive = true;
            entity.StagesProgressJson = "[]";

            // Initialize audit information
            entity.InitCreateInfo(_userContext);
            AuditHelper.ApplyCreateAudit(entity, _operatorContextService);

            // Generate unique ID if not set
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

            // Ensure Id is generated
            if (entity.Id == 0)
            {
                entity.Id = SnowFlakeSingle.Instance.NextId();
            }
        }

        /// <summary>
        /// Post-creation processing: initialize stages progress, create user invitation, log creation
        /// </summary>
        private async Task ProcessPostCreationAsync(long insertedId, List<Stage> stages)
        {
            if (insertedId <= 0) return;

            try
            {
                var insertedEntity = await _onboardingRepository.GetByIdAsync(insertedId);
                if (insertedEntity == null) return;

                // Initialize stage progress
                await InitializeStagesProgressAsync(insertedEntity, stages);

                // Update entity to save stage progress
                var updateResult = await SafeUpdateOnboardingAsync(insertedEntity);
                if (!updateResult)
                {
                    _logger.LogWarning("Failed to update stages progress for Onboarding {OnboardingId}", insertedId);
                }

                // Create default UserInvitation record if email is available
                await CreateDefaultUserInvitationAsync(insertedEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during post-creation processing for Onboarding {OnboardingId}", insertedId);
                // Don't fail the entire creation for post-processing errors
            }
        }

        /// <summary>
        /// Log onboarding creation in background
        /// </summary>
        private void QueueCreationLogging(long insertedId)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
            {
                try
                {
                    var insertedEntity = await _onboardingRepository.GetByIdAsync(insertedId);
                    if (insertedEntity == null) return;

                    string workflowName = null;
                    try
                    {
                        var workflow = await _workflowRepository.GetByIdAsync(insertedEntity.WorkflowId);
                        workflowName = workflow?.Name;
                    }
                    catch { /* Ignore if workflow not found */ }

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
    }
}

