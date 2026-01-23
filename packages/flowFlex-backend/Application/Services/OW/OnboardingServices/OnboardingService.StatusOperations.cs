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
    /// Onboarding service - Additional status operations
    /// </summary>
    public partial class OnboardingService
    {
        public async Task<bool> StartOnboardingAsync(long id, StartOnboardingInputDto input)
        {
            // Check permission
            await EnsureCaseOperatePermissionAsync(id);

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
            entity.StartDate = NormalizeToStartOfDay(DateTimeOffset.UtcNow);

            // IMPORTANT: Set CurrentStageStartTime when starting onboarding
            // This marks the beginning of the current stage timeline
            // Normalize to start of day (00:00:00)
            entity.CurrentStageStartTime = GetNormalizedUtcNow();

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
            var result = await SafeUpdateOnboardingWithoutStagesProgressAsync(entity, originalStagesProgressJson);

            // Log start operation
            if (result)
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        await _onboardingLogService.LogOnboardingStartAsync(
                            id,
                            entity.CaseName ?? entity.CaseCode ?? "Unknown",
                            reason: input.Reason
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to log onboarding start operation for onboarding {OnboardingId}", id);
                    }
                });
            }

            return result;
        }

        /// <summary>
        /// Abort onboarding (terminate the process)
        /// </summary>
        public async Task<bool> AbortAsync(long id, AbortOnboardingInputDto input)
        {
            // Check permission
            await EnsureCaseOperatePermissionAsync(id);

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
            var result = await SafeUpdateOnboardingWithoutStagesProgressAsync(entity, originalStagesProgressJson);

            // Log abort operation
            if (result)
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        await _onboardingLogService.LogOnboardingAbortAsync(
                            id,
                            entity.CaseName ?? entity.CaseCode ?? "Unknown",
                            reason: input.Reason
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to log onboarding abort operation for onboarding {OnboardingId}", id);
                    }
                });
            }

            return result;
        }

        /// <summary>
        /// Reactivate onboarding (restart an aborted onboarding)
        /// </summary>
        public async Task<bool> ReactivateAsync(long id, ReactivateOnboardingInputDto input)
        {
            // Check permission
            await EnsureCaseOperatePermissionAsync(id);

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
            var result = await SafeUpdateOnboardingWithoutStagesProgressAsync(entity, originalStagesProgressJson);

            // Log reactivate operation
            if (result)
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        await _onboardingLogService.LogOnboardingReactivateAsync(
                            id,
                            entity.CaseName ?? entity.CaseCode ?? "Unknown",
                            reason: input.Reason
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to log onboarding reactivate operation for onboarding {OnboardingId}", id);
                    }
                });
            }

            return result;
        }

        /// <summary>
        /// Resume onboarding with confirmation
        /// </summary>
        public async Task<bool> ResumeWithConfirmationAsync(long id, ResumeOnboardingInputDto input)
        {
            // Check permission
            await EnsureCaseOperatePermissionAsync(id);

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
            var result = await SafeUpdateOnboardingWithoutStagesProgressAsync(entity, originalStagesProgressJson);

            // Log resume operation
            if (result)
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        await _onboardingLogService.LogOnboardingResumeAsync(
                            id,
                            entity.CaseName ?? entity.CaseCode ?? "Unknown",
                            reason: input.Reason
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to log onboarding resume operation for onboarding {OnboardingId}", id);
                    }
                });
            }

            return result;
        }
        /// <summary>
        /// Force complete onboarding (bypass normal validation and set to Force Completed status)
        /// </summary>
        public async Task<bool> ForceCompleteAsync(long id, ForceCompleteOnboardingInputDto input)
        {
            // Check permission
            await EnsureCaseOperatePermissionAsync(id);

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

            // Use special update method that excludes stages_progress_json field
            var result = await SafeUpdateOnboardingWithoutStagesProgressAsync(entity, originalStagesProgressJson);

            // Log force complete operation
            if (result)
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        await _onboardingLogService.LogOnboardingForceCompleteAsync(
                            id,
                            entity.CaseName ?? entity.CaseCode ?? "Unknown",
                            reason: input.Reason
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to log onboarding force complete operation for onboarding {OnboardingId}", id);
                    }
                });
            }

            return result;
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

                        _logger.LogDebug("ForceComplete - Preserved original stages_progress_json for onboarding {OnboardingId}", entity.Id);
                    }
                    catch (Exception progressEx)
                    {
                        // Log but don't fail the main update
                        _logger.LogWarning(progressEx, "Failed to preserve stages_progress_json for onboarding {OnboardingId}", entity.Id);
                        // Try alternative approach with parameter substitution
                        try
                        {
                            var escapedJson = preserveStagesProgressJson.Replace("'", "''");
                            var directSql = $"UPDATE ff_onboarding SET stages_progress_json = '{escapedJson}'::jsonb WHERE id = {entity.Id}";
                            await db.Ado.ExecuteCommandAsync(directSql);

                            _logger.LogDebug("ForceComplete - Preserved original stages_progress_json for onboarding {OnboardingId} using direct SQL", entity.Id);
                        }
                        catch (Exception directEx)
                        {
                            _logger.LogError(directEx, "Both parameterized and direct JSONB preserve failed for onboarding {OnboardingId}", entity.Id);
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
                    _logger.LogDebug("Workflow {WorkflowId} - VisibleToTeams mode with NULL ViewTeams = DENY", workflow.Id);
                    return false;
                }

                // Parse ViewTeams JSON array
                var viewTeams = ParseJsonArraySafe(workflow.ViewTeams);

                if (viewTeams.Count == 0)
                {
                    _logger.LogDebug("Workflow {WorkflowId} - Empty ViewTeams = DENY", workflow.Id);
                    return false;
                }

                var viewTeamLongs = viewTeams.Select(t => long.TryParse(t, out var tid) ? tid : 0).Where(t => t > 0).ToHashSet();
                bool hasMatch = userTeamIds.Any(ut => viewTeamLongs.Contains(ut));

                _logger.LogDebug("Workflow {WorkflowId} - Team match result: {HasMatch}", workflow.Id, hasMatch);

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
        /// Sync Onboarding fields to Static Field Values when Onboarding is updated
        /// </summary>
        private async Task SyncStaticFieldValuesAsync(
            long onboardingId,
            long stageId,
            string originalLeadId,
            string originalCaseName,
            string originalContactPerson,
            string originalContactEmail,
            string originalLeadPhone,
            long? originalLifeCycleStageId,
            string originalPriority,
            OnboardingInputDto input)
        {
            try
            {
                _logger.LogDebug("Starting static field sync - OnboardingId: {OnboardingId}, StageId: {StageId}", onboardingId, stageId);

                var staticFieldUpdates = new List<FlowFlex.Application.Contracts.Dtos.OW.StaticField.StaticFieldValueInputDto>();

                // Field mapping: Onboarding field -> Static Field Name
                // Only update fields that have changed
                if (!string.Equals(originalLeadId, input.LeadId, StringComparison.Ordinal))
                {
                    _logger.LogDebug("LEADID changed: '{OriginalValue}' -> '{NewValue}'", originalLeadId, input.LeadId);
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

                if (!string.Equals(originalCaseName, input.CaseName, StringComparison.Ordinal))
                {
                    _logger.LogDebug("CUSTOMERNAME changed: '{OriginalValue}' -> '{NewValue}'", originalCaseName, input.CaseName);
                    staticFieldUpdates.Add(CreateStaticFieldInput(
                        onboardingId,
                        stageId,
                        "CUSTOMERNAME",
                        input.CaseName,
                        "text",
                        "Customer Name",
                        isRequired: false
                    ));
                }

                if (!string.Equals(originalContactPerson, input.ContactPerson, StringComparison.Ordinal))
                {
                    _logger.LogDebug("CONTACTNAME changed: '{OriginalValue}' -> '{NewValue}'", originalContactPerson, input.ContactPerson);
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
                    _logger.LogDebug("CONTACTEMAIL changed: '{OriginalValue}' -> '{NewValue}'", originalContactEmail, input.ContactEmail);
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
                    _logger.LogDebug("CONTACTPHONE changed: '{OriginalValue}' -> '{NewValue}'", originalLeadPhone, input.LeadPhone);
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
                    _logger.LogDebug("LIFECYCLESTAGE changed: '{OriginalValue}' -> '{NewValue}'", originalLifeCycleStageId, input.LifeCycleStageId);
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
                    _logger.LogDebug("PRIORITY changed: '{OriginalValue}' -> '{NewValue}'", originalPriority, input.Priority);
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
                    _logger.LogDebug("Syncing {FieldCount} static field(s) to database", staticFieldUpdates.Count);

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
                    _logger.LogDebug("Static field sync completed successfully");
                }
                else
                {
                    _logger.LogDebug("No static field changes detected, sync skipped");
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the main update operation
                _logger.LogError(ex, "Failed to sync static field values for Onboarding {OnboardingId}", onboardingId);
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
    }
}

