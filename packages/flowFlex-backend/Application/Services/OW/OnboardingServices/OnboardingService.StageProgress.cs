using AutoMapper;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.Dtos.OW.Permission;
using FlowFlex.Application.Contracts.IServices.Action;
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
using Microsoft.Extensions.Logging;
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
    /// Onboarding service - Stage progress management
    /// </summary>
    public partial class OnboardingService
    {
        private async Task InitializeStagesProgressAsync(Onboarding entity, List<Stage> stages)
        {
            await _stageProgressService.InitializeStagesProgressAsync(entity, stages);
        }
        /// <summary>
        /// Update stages progress - supports non-sequential stage completion
        /// </summary>
        private async Task UpdateStagesProgressAsync(Onboarding entity, long completedStageId, string completedBy = null, long? completedById = null, string notes = null)
        {
            await _stageProgressService.UpdateStagesProgressAsync(entity, completedStageId, completedBy, completedById, notes);
        }

        /// <summary>
        /// Load stages progress from JSONB - optimized version with JSONB support
        /// Handles both legacy JSON format and new JSONB format with camelCase properties
        /// </summary>
        private void LoadStagesProgressFromJson(Onboarding entity)
        {
            _stageProgressService.LoadStagesProgressFromJson(entity);
        }

        /// <summary>
        /// Load stages progress from JSONB - read-only version for query operations
        /// Does not fix stage order or serialize back to JSON
        /// </summary>
        private void LoadStagesProgressFromJsonReadOnly(Onboarding entity)
        {
            _stageProgressService.LoadStagesProgressFromJsonReadOnly(entity);
        }

        /// <summary>
        /// Check if stage order needs to be fixed
        /// </summary>
        private bool NeedsStageOrderFix(List<OnboardingStageProgress> stagesProgress)
        {
            return _stageProgressService.NeedsStageOrderFix(stagesProgress);
        }

        /// <summary>
        /// Fix stage order to be sequential (1, 2, 3, 4, 5...) instead of potentially non-consecutive orders
        /// </summary>
        private void FixStageOrderSequence(List<OnboardingStageProgress> stagesProgress)
        {
            _stageProgressService.FixStageOrderSequence(stagesProgress);
        }

        /// <summary>
        /// Validate if a stage can be completed based on business rules
        /// </summary>
        private async Task<(bool CanComplete, string ErrorMessage)> ValidateStageCanBeCompletedAsync(Onboarding entity, long stageId)
        {
            return await _stageProgressService.ValidateStageCanBeCompletedAsync(entity, stageId);
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
            return _stageProgressService.CalculateCompletionRateByCompletedStages(stagesProgress);
        }

        /// <summary>
        /// Clear related cache data (placeholder for future cache implementation)
        /// </summary>
        private Task ClearRelatedCacheAsync(long? workflowId = null, long? stageId = null)
        {
            // Redis cache temporarily disabled - no-op for now
            _logger.LogDebug("ClearRelatedCacheAsync called - cache disabled, WorkflowId: {WorkflowId}, StageId: {StageId}", 
                workflowId, stageId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Clear Onboarding query cache (placeholder for future cache implementation)
        /// </summary>
        private Task ClearOnboardingQueryCacheAsync()
        {
            // Redis cache temporarily disabled - no-op for now
            _logger.LogDebug("ClearOnboardingQueryCacheAsync called - cache disabled");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Enrich stages progress with data from Stage entities
        /// This method dynamically populates fields like stageName, stageOrder, estimatedDays etc.
        /// from the Stage entities, ensuring consistency and reducing data duplication.
        /// </summary>
        private void EnrichStagesProgressWithStageData(Onboarding entity, List<Stage> stages)
        {
            _stageProgressService.EnrichStagesProgressWithStageData(entity, stages);
        }

        /// <summary>
        /// Enrich stages progress with data from Stage entities (async version for backward compatibility)
        /// </summary>
        private async Task EnrichStagesProgressWithStageDataAsync(Onboarding entity)
        {
            await _stageProgressService.EnrichStagesProgressWithStageDataAsync(entity);
        }

        /// <summary>
        /// Sync stages progress with workflow stages - handle new stages addition
        /// This method ensures that if workflow has new stages, they are added to stagesProgress.
        /// </summary>
        private async Task SyncStagesProgressWithWorkflowAsync(Onboarding entity, List<Stage>? preloadedStages = null)
        {
            await _stageProgressService.SyncStagesProgressWithWorkflowAsync(entity, preloadedStages);
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
                _logger.LogDebug("SafeUpdateOnboardingAsync - Updating Onboarding {OnboardingId}: CurrentStageId={CurrentStageId}, Status={Status}",
                    entity.Id, entity.CurrentStageId, entity.Status);

                var db = _onboardingRepository.GetSqlSugarClient();

                // Step 1: Update permission JSONB fields
                await UpdatePermissionFieldsAsync(db, entity);

                // Step 2: Filter valid stages progress
                await FilterValidStagesProgress(entity);

                // Step 3: Prepare audit values
                var auditInfo = PrepareAuditInfo();
                entity.ModifyDate = auditInfo.ModifyDate;
                entity.ModifyBy = auditInfo.ModifyBy;
                entity.ModifyUserId = auditInfo.ModifyUserId;

                // Step 4: Update main entity fields
                var result = await UpdateMainEntityFieldsAsync(entity);

                // Step 5: Update audit fields via SQL (workaround for SqlSugar reset issue)
                await UpdateAuditFieldsAsync(db, entity.Id, auditInfo);

                // Step 6: Update stages_progress_json separately with JSONB casting
                await UpdateStagesProgressJsonAsync(db, entity);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to safely update onboarding {OnboardingId}", entity.Id);
                throw new CRMException(ErrorCodeEnum.SystemError,
                    $"Failed to safely update onboarding: {ex.Message}");
            }
        }

        /// <summary>
        /// Update permission JSONB fields with explicit casting
        /// </summary>
        private async Task UpdatePermissionFieldsAsync(ISqlSugarClient db, Onboarding entity)
        {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update permission JSONB fields for onboarding {OnboardingId}", entity.Id);
                throw new CRMException($"Failed to update permission fields: {ex.Message}");
            }
        }

        /// <summary>
        /// Audit info container
        /// </summary>
        private record AuditInfo(DateTimeOffset ModifyDate, string ModifyBy, long ModifyUserId);

        /// <summary>
        /// Prepare audit information
        /// </summary>
        private AuditInfo PrepareAuditInfo()
        {
            return new AuditInfo(
                DateTimeOffset.UtcNow,
                _operatorContextService.GetOperatorDisplayName(),
                _operatorContextService.GetOperatorId()
            );
        }

        /// <summary>
        /// Update main entity fields using repository
        /// </summary>
        private async Task<bool> UpdateMainEntityFieldsAsync(Onboarding entity)
        {
            return await _onboardingRepository.UpdateAsync(entity,
                it => new
                {
                    it.WorkflowId,
                    it.CurrentStageId,
                    it.CurrentStageOrder,
                    it.LeadId,
                    it.CaseName,
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
                    it.Notes,
                    it.IsActive,
                    it.ModifyDate,
                    it.ModifyBy,
                    it.ModifyUserId,
                    it.IsValid
                });
        }

        /// <summary>
        /// Update audit fields via direct SQL
        /// </summary>
        private async Task UpdateAuditFieldsAsync(ISqlSugarClient db, long entityId, AuditInfo auditInfo)
        {
            try
            {
                var auditSql = @"
                    UPDATE ff_onboarding 
                    SET modify_date = @ModifyDate,
                        modify_by = @ModifyBy,
                        modify_user_id = @ModifyUserId
                    WHERE id = @Id";

                await db.Ado.ExecuteCommandAsync(auditSql, new
                {
                    ModifyDate = auditInfo.ModifyDate,
                    ModifyBy = auditInfo.ModifyBy,
                    ModifyUserId = auditInfo.ModifyUserId,
                    Id = entityId
                });

                _logger.LogDebug("Audit fields updated for onboarding {OnboardingId}: ModifyBy='{ModifyBy}'",
                    entityId, auditInfo.ModifyBy);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update audit fields for onboarding {OnboardingId}", entityId);
                // Don't fail the entire update if audit field update fails
            }
        }

        /// <summary>
        /// Update stages_progress_json with JSONB casting
        /// </summary>
        private async Task UpdateStagesProgressJsonAsync(ISqlSugarClient db, Onboarding entity)
        {
            if (string.IsNullOrEmpty(entity.StagesProgressJson))
            {
                return;
            }

            try
            {
                var progressSql = "UPDATE ff_onboarding SET stages_progress_json = @StagesProgressJson::jsonb WHERE id = @Id";
                await db.Ado.ExecuteCommandAsync(progressSql, new
                {
                    StagesProgressJson = entity.StagesProgressJson,
                    Id = entity.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update stages_progress_json with parameterized query for onboarding {OnboardingId}", entity.Id);
                
                // Try alternative approach with direct SQL
                try
                {
                    var escapedJson = entity.StagesProgressJson.Replace("'", "''");
                    var directSql = $"UPDATE ff_onboarding SET stages_progress_json = '{escapedJson}'::jsonb WHERE id = {entity.Id}";
                    await db.Ado.ExecuteCommandAsync(directSql);
                }
                catch (Exception directEx)
                {
                    _logger.LogError(directEx, "Both parameterized and direct JSONB update failed for onboarding {OnboardingId}", entity.Id);
                }
            }
        }

        /// <summary>
        /// Ensure stages progress is properly initialized and synced with workflow
        /// This method handles cases where stages progress might be empty or outdated
        /// </summary>
        private async Task EnsureStagesProgressInitializedAsync(Onboarding entity, IEnumerable<Stage>? preloadedStages = null)
        {
            await _stageProgressService.EnsureStagesProgressInitializedAsync(entity, preloadedStages);
        }

        /// <summary>
        /// Serialize stages progress to JSON - only stores progress state, not stage configuration
        /// Stage configuration fields (stageName, stageOrder, etc.) are excluded via JsonIgnore attributes
        /// and are populated dynamically from Stage entities when needed.
        /// </summary>
        private string SerializeStagesProgress(List<OnboardingStageProgress> stagesProgress)
        {
            return _stageProgressService.SerializeStagesProgress(stagesProgress);
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
                    SentDate = null, // Leave empty - will be set when invitation is actually sent
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
                    onboarding.TenantId ?? "default",
                    onboarding.AppCode ?? "default");

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
                // Get current onboarding without tenant filter (for background tasks where HttpContext is not available)
                var onboarding = await _onboardingRepository.GetByIdWithoutTenantFilterAsync(onboardingId);
                if (onboarding == null)
                {
                    _logger.LogWarning("Onboarding {OnboardingId} not found for AI summary update", onboardingId);
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
                    _logger.LogWarning("Stage progress not found for stage {StageId} in onboarding {OnboardingId}. Available stages: {AvailableStages}",
                        stageId, onboardingId, string.Join(", ", onboarding.StagesProgress?.Select(sp => sp.StageId.ToString()) ?? Array.Empty<string>()));
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

                // Update only stages_progress_json in database WITHOUT updating modifyBy/modifyDate/modifyUserId
                // AI summary updates are system-generated and should not affect audit fields
                var result = await UpdateStagesProgressJsonOnlyAsync(onboarding.Id, onboarding.StagesProgressJson);

                if (result)
                {
                    _logger.LogInformation("Successfully updated AI summary for stage {StageId} in onboarding {OnboardingId}", stageId, onboardingId);
                }
                else
                {
                    _logger.LogWarning("Failed to save AI summary for stage {StageId} in onboarding {OnboardingId} - database update failed", stageId, onboardingId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update AI summary for stage {StageId} in onboarding {OnboardingId}", stageId, onboardingId);
                return false;
            }
        }

        /// <summary>
        /// Update only stages_progress_json field without modifying audit fields (modifyBy, modifyDate, modifyUserId)
        /// Used for system-generated updates like AI summary that should not affect audit trail
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stagesProgressJson">Serialized stages progress JSON</param>
        /// <returns>Success status</returns>
        private async Task<bool> UpdateStagesProgressJsonOnlyAsync(long onboardingId, string stagesProgressJson)
        {
            if (string.IsNullOrEmpty(stagesProgressJson))
            {
                return true;
            }

            try
            {
                var db = _onboardingRepository.GetSqlSugarClient();
                var sql = "UPDATE ff_onboarding SET stages_progress_json = @StagesProgressJson::jsonb WHERE id = @Id";
                var rowsAffected = await db.Ado.ExecuteCommandAsync(sql, new
                {
                    StagesProgressJson = stagesProgressJson,
                    Id = onboardingId
                });

                _logger.LogDebug("Updated stages_progress_json only for onboarding {OnboardingId}, rows affected: {RowsAffected}", 
                    onboardingId, rowsAffected);

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update stages_progress_json for onboarding {OnboardingId}", onboardingId);
                return false;
            }
        }

        /// <summary>
        /// Update custom fields for a specific stage in onboarding's stagesProgress
        /// Updates CustomEstimatedDays, CustomEndTime, CustomStageAssignee, and CustomStageCoAssignees fields
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

                // Ensure stages progress is properly initialized and synced
                // This handles cases where stagesProgress is empty or outdated
                await EnsureStagesProgressInitializedAsync(onboarding);

                // Find the stage progress entry
                var stageProgress = onboarding.StagesProgress?.FirstOrDefault(sp => sp.StageId == input.StageId);
                if (stageProgress == null)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, $"Stage {input.StageId} not found in onboarding {onboardingId}");
                }

                // Capture original values for comparison
                var originalEstimatedDays = stageProgress.CustomEstimatedDays;
                var originalEndTime = stageProgress.CustomEndTime;
                var originalCustomAssignee = stageProgress.CustomStageAssignee?.ToList() ?? new List<string>();
                var originalCustomCoAssignees = stageProgress.CustomStageCoAssignees?.ToList() ?? new List<string>();

                // Capture before data for change log
                var beforeData = JsonSerializer.Serialize(new
                {
                    CustomEstimatedDays = stageProgress.CustomEstimatedDays,
                    CustomEndTime = stageProgress.CustomEndTime,
                    CustomStageAssignee = stageProgress.CustomStageAssignee,
                    CustomStageCoAssignees = stageProgress.CustomStageCoAssignees,
                    LastUpdatedTime = stageProgress.LastUpdatedTime,
                    LastUpdatedBy = stageProgress.LastUpdatedBy
                });

                // Update custom fields - normalize estimated days to integer and end time to start of day
                stageProgress.CustomEstimatedDays = NormalizeEstimatedDays(input.CustomEstimatedDays);
                stageProgress.CustomEndTime = NormalizeToStartOfDay(input.CustomEndTime);

                // Update CustomStageAssignee if Assignee is provided (frontend uses Assignee field)
                if (input.Assignee != null)
                {
                    stageProgress.CustomStageAssignee = input.Assignee;
                }

                // Update CustomStageCoAssignees if CoAssignees is provided (frontend uses CoAssignees field)
                if (input.CoAssignees != null)
                {
                    stageProgress.CustomStageCoAssignees = input.CoAssignees;
                }

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
                    CustomStageAssignee = stageProgress.CustomStageAssignee,
                    CustomStageCoAssignees = stageProgress.CustomStageCoAssignees,
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

                    // Collect all user IDs that need name resolution
                    var allUserIds = new HashSet<long>();
                    
                    // Add user IDs from assignee changes
                    foreach (var id in originalCustomAssignee.Concat(input.Assignee ?? new List<string>())
                        .Concat(originalCustomCoAssignees).Concat(input.CoAssignees ?? new List<string>()))
                    {
                        if (long.TryParse(id, out var userId))
                        {
                            allUserIds.Add(userId);
                        }
                    }

                    // Fetch user names for all IDs
                    var userNameMap = new Dictionary<long, string>();
                    if (allUserIds.Any())
                    {
                        try
                        {
                            var tenantId = _userContext?.TenantId ?? "default";
                            var users = await _userService.GetUsersByIdsAsync(allUserIds.ToList(), tenantId);
                            userNameMap = users
                                .GroupBy(u => u.Id)
                                .ToDictionary(
                                    g => g.Key,
                                    g =>
                                    {
                                        var user = g.First();
                                        return !string.IsNullOrEmpty(user.Username) ? user.Username :
                                               (!string.IsNullOrEmpty(user.Email) ? user.Email : $"User_{user.Id}");
                                    }
                                );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to fetch user names for stage custom fields change log");
                        }
                    }

                    // Helper function to convert user IDs to names
                    string GetUserDisplayNames(IEnumerable<string> userIds)
                    {
                        if (userIds == null || !userIds.Any()) return "empty";
                        var names = userIds
                            .Select(id => long.TryParse(id, out var userId) && userNameMap.TryGetValue(userId, out var name) ? name : id)
                            .ToList();
                        return string.Join(", ", names);
                    }

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

                    // Check for actual changes in CustomStageAssignee (input uses Assignee field)
                    if (input.Assignee != null && !originalCustomAssignee.SequenceEqual(input.Assignee))
                    {
                        changedFields.Add("CustomStageAssignee");
                        var beforeValue = GetUserDisplayNames(originalCustomAssignee);
                        var afterValue = GetUserDisplayNames(input.Assignee);
                        changeDetails.Add($"Assignee: {beforeValue} → {afterValue}");
                    }

                    // Check for actual changes in CustomStageCoAssignees (input uses CoAssignees field)
                    if (input.CoAssignees != null && !originalCustomCoAssignees.SequenceEqual(input.CoAssignees))
                    {
                        changedFields.Add("CustomStageCoAssignees");
                        var beforeValue = GetUserDisplayNames(originalCustomCoAssignees);
                        var afterValue = GetUserDisplayNames(input.CoAssignees);
                        changeDetails.Add($"CoAssignees: {beforeValue} → {afterValue}");
                    }

                    // Check if notes were added
                    if (!string.IsNullOrEmpty(input.Notes))
                    {
                        changedFields.Add("Notes");
                        changeDetails.Add("Notes: Added");
                    }

                    // Log as Stage operation with onboardingId to associate with Case
                    // Use BusinessModule.Stage because legacy adapter doesn't support Onboarding module
                    if (changeDetails.Any())
                    {
                        var operationTitle = $"Update Stage Custom Fields: {string.Join(", ", changeDetails)}";
                        var operationDescription = $"Updated custom fields for stage {input.StageId} in case {onboardingId}";

                        // Log the case stage custom fields update operation
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
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update custom fields for stage {StageId} in onboarding {OnboardingId}", input.StageId, onboardingId);
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
            await EnsureCaseOperatePermissionAsync(onboardingId);

            try
            {
                // Get current onboarding
                var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
                if (onboarding == null)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
                }

                // Ensure stages progress is properly initialized and synced
                // This handles cases where stagesProgress is empty or outdated
                await EnsureStagesProgressInitializedAsync(onboarding);

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
                // Normalize to start of day (00:00:00)
                if (!stageProgress.StartTime.HasValue)
                {
                    stageProgress.StartTime = GetNormalizedUtcNow();
                }

                // IMPORTANT: If this is the current stage and CurrentStageStartTime is not set, set it now
                // Normalize to start of day (00:00:00)
                if (stageProgress.StageId == onboarding.CurrentStageId && !onboarding.CurrentStageStartTime.HasValue)
                {
                    onboarding.CurrentStageStartTime = NormalizeToStartOfDay(stageProgress.StartTime);
                    _logger.LogDebug("SaveStageAsync - Set CurrentStageStartTime to {StartTime} for Stage {StageId}", 
                        onboarding.CurrentStageStartTime, stageId);
                }

                // Save stages progress back to JSON
                await FilterValidStagesProgress(onboarding);
                onboarding.StagesProgressJson = SerializeStagesProgress(onboarding.StagesProgress);

                // Update in database
                var result = await SafeUpdateOnboardingAsync(onboarding);

                // Log stage save to operation_change_log
                if (result)
                {
                    try
                    {
                        var stage = await _stageRepository.GetByIdAsync(stageId);
                        var beforeData = new
                        {
                            StageId = stageId,
                            StageName = stage?.Name,
                            IsSaved = false,
                            SaveTime = (DateTimeOffset?)null
                        };

                        var afterData = new
                        {
                            StageId = stageId,
                            StageName = stage?.Name,
                            IsSaved = true,
                            SaveTime = stageProgress.SaveTime,
                            SavedBy = stageProgress.SavedBy,
                            StartTime = stageProgress.StartTime
                        };

                        var extendedData = new
                        {
                            WorkflowId = onboarding.WorkflowId,
                            IsCurrentStage = stageProgress.StageId == onboarding.CurrentStageId,
                            CurrentStageStartTime = onboarding.CurrentStageStartTime,
                            Source = "manual_save"
                        };

                        await _operationChangeLogService.LogOperationAsync(
                            operationType: OperationTypeEnum.StageSave,
                            businessModule: BusinessModuleEnum.Stage,
                            businessId: stageId,
                            onboardingId: onboardingId,
                            stageId: stageId,
                            operationTitle: $"Stage Saved: {stage?.Name ?? "Unknown"}",
                            operationDescription: $"Stage '{stage?.Name}' has been saved by {stageProgress.SavedBy}",
                            beforeData: System.Text.Json.JsonSerializer.Serialize(beforeData),
                            afterData: System.Text.Json.JsonSerializer.Serialize(afterData),
                            changedFields: new List<string> { "IsSaved", "SaveTime", "SavedBy" },
                            extendedData: System.Text.Json.JsonSerializer.Serialize(extendedData)
                        );

                        _logger.LogInformation("Stage save log recorded: OnboardingId={OnboardingId}, StageId={StageId}, StageName={StageName}",
                            onboardingId, stageId, stage?.Name);
                    }
                    catch (Exception logEx)
                    {
                        _logger.LogError(logEx, "Failed to record Stage save log: OnboardingId={OnboardingId}, StageId={StageId}",
                            onboardingId, stageId);
                        // Don't re-throw to avoid breaking the main flow
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new CRMException(ErrorCodeEnum.SystemError, $"Failed to save stage {stageId} in onboarding {onboardingId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Filter invalid stagesProgress, keeping only stages that exist in current workflow
        /// </summary>
        private async Task FilterValidStagesProgress(Onboarding entity)
        {
            await _stageProgressService.FilterValidStagesProgressAsync(entity);
        }

        /// <summary>
        /// Parse DefaultAssignee JSON string to List of user IDs
        /// </summary>
        private List<string> ParseDefaultAssignee(string defaultAssigneeJson)
        {
            return _stageProgressService.ParseDefaultAssignee(defaultAssigneeJson);
        }

        /// <summary>
        /// Get CoAssignees filtered to exclude any IDs already in DefaultAssignee
        /// </summary>
        private List<string> GetFilteredCoAssignees(string coAssigneesJson, string defaultAssigneeJson)
        {
            return _stageProgressService.GetFilteredCoAssignees(coAssigneesJson, defaultAssigneeJson);
        }

        /// <summary>
        /// Normalize DateTimeOffset to start of day (00:00:00) for stage time fields
        /// </summary>
        private static DateTimeOffset NormalizeToStartOfDay(DateTimeOffset dateTime)
        {
            return new DateTimeOffset(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, dateTime.Offset);
        }

        /// <summary>
        /// Normalize nullable DateTimeOffset to start of day (00:00:00) for stage time fields
        /// </summary>
        private static DateTimeOffset? NormalizeToStartOfDay(DateTimeOffset? dateTime)
        {
            if (!dateTime.HasValue) return null;
            return NormalizeToStartOfDay(dateTime.Value);
        }

        /// <summary>
        /// Normalize estimated days to integer (round to nearest whole number)
        /// </summary>
        private static decimal? NormalizeEstimatedDays(decimal? days)
        {
            if (!days.HasValue) return null;
            return Math.Round(days.Value, 0);
        }

        /// <summary>
        /// Get current UTC time normalized to start of day (00:00:00)
        /// </summary>
        private static DateTimeOffset GetNormalizedUtcNow()
        {
            return NormalizeToStartOfDay(DateTimeOffset.UtcNow);
        }
    }
}

