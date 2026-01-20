using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;
using FlowFlex.Application.Contracts.Dtos.OW.StaticField;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Application.Contracts.IServices.DynamicData;
using FlowFlex.Application.Services.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Events.Action;
using FlowFlex.Domain.Shared.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SqlSugar;

namespace FlowFlex.Application.Service.OW
{
    /// <summary>
    /// Condition action executor implementation for Stage Condition feature
    /// </summary>
    public class ConditionActionExecutor : IConditionActionExecutor, IScopedService
    {
        private readonly ISqlSugarClient _db;
        private readonly IStageRepository _stageRepository;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IEmailService _emailService;
        private readonly IUserService _userService;
        private readonly UserContext _userContext;
        private readonly IMediator _mediator;
        private readonly IActionExecutionService _actionExecutionService;
        private readonly IStaticFieldValueService _staticFieldValueService;
        private readonly IPropertyService _propertyService;
        private readonly IdmUserDataClient _idmUserDataClient;
        private readonly ILogger<ConditionActionExecutor> _logger;

        public ConditionActionExecutor(
            ISqlSugarClient db,
            IStageRepository stageRepository,
            IOnboardingRepository onboardingRepository,
            IEmailService emailService,
            IUserService userService,
            UserContext userContext,
            IMediator mediator,
            IActionExecutionService actionExecutionService,
            IStaticFieldValueService staticFieldValueService,
            IPropertyService propertyService,
            IdmUserDataClient idmUserDataClient,
            ILogger<ConditionActionExecutor> logger)
        {
            _db = db;
            _stageRepository = stageRepository;
            _onboardingRepository = onboardingRepository;
            _emailService = emailService;
            _userService = userService;
            _userContext = userContext;
            _mediator = mediator;
            _actionExecutionService = actionExecutionService;
            _staticFieldValueService = staticFieldValueService;
            _propertyService = propertyService;
            _idmUserDataClient = idmUserDataClient;
            _logger = logger;
        }

        /// <summary>
        /// Execute all actions for a condition
        /// </summary>
        public async Task<ActionExecutionResult> ExecuteActionsAsync(string actionsJson, ActionExecutionContext context)
        {
            var result = new ActionExecutionResult { Success = true };

            try
            {
                _logger.LogDebug("Parsing actionsJson: {ActionsJson}", actionsJson);
                
                // Parse actions JSON with settings to handle various formats
                var settings = new JsonSerializerSettings
                {
                    Error = (sender, args) =>
                    {
                        _logger.LogWarning("JSON deserialization error at path {Path}: {Error}", 
                            args.ErrorContext.Path, args.ErrorContext.Error.Message);
                        args.ErrorContext.Handled = true;
                    }
                };
                
                var actions = JsonConvert.DeserializeObject<List<ConditionAction>>(actionsJson, settings);
                
                if (actions == null || actions.Count == 0)
                {
                    _logger.LogDebug("No actions to execute for condition {ConditionId}", context.ConditionId);
                    return result;
                }

                // Sort by order and execute sequentially
                foreach (var action in actions.OrderBy(a => a.Order))
                {
                    var actionResult = await ExecuteActionAsync(action, context);
                    result.Details.Add(actionResult);

                    if (!actionResult.Success)
                    {
                        _logger.LogWarning("Action {ActionType} failed for condition {ConditionId}: {ErrorMessage}",
                            action.Type, context.ConditionId, actionResult.ErrorMessage);
                        // Continue with next action even if one fails
                    }
                }

                // Overall success if at least one action succeeded
                result.Success = result.Details.Any(d => d.Success);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse ActionsJson for condition {ConditionId}", context.ConditionId);
                result.Success = false;
                result.Details.Add(new ActionExecutionDetail
                {
                    ActionType = "ParseError",
                    Success = false,
                    ErrorMessage = $"Invalid ActionsJson format: {ex.Message}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing actions for condition {ConditionId}", context.ConditionId);
                result.Success = false;
                result.Details.Add(new ActionExecutionDetail
                {
                    ActionType = "ExecutionError",
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }

            return result;
        }

        #region Private Methods

        /// <summary>
        /// Execute a single action
        /// </summary>
        private async Task<ActionExecutionDetail> ExecuteActionAsync(ConditionAction action, ActionExecutionContext context)
        {
            try
            {
                return action.Type.ToLower() switch
                {
                    "gotostage" => await ExecuteGoToStageAsync(action, context),
                    "skipstage" => await ExecuteSkipStageAsync(action, context),
                    "endworkflow" => await ExecuteEndWorkflowAsync(action, context),
                    "sendnotification" => await ExecuteSendNotificationAsync(action, context),
                    "updatefield" => await ExecuteUpdateFieldAsync(action, context),
                    "triggeraction" => await ExecuteTriggerActionAsync(action, context),
                    "assignuser" => await ExecuteAssignUserAsync(action, context),
                    _ => new ActionExecutionDetail
                    {
                        ActionType = action.Type,
                        Order = action.Order,
                        Success = false,
                        ErrorMessage = $"Unsupported action type: {action.Type}"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing action {ActionType}", action.Type);
                return new ActionExecutionDetail
                {
                    ActionType = action.Type,
                    Order = action.Order,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }


        /// <summary>
        /// Execute GoToStage action - jump to a specific stage
        /// </summary>
        private async Task<ActionExecutionDetail> ExecuteGoToStageAsync(ConditionAction action, ActionExecutionContext context)
        {
            var result = new ActionExecutionDetail
            {
                ActionType = "GoToStage",
                Order = action.Order,
                ResultData = new Dictionary<string, object>()
            };

            if (!action.TargetStageId.HasValue)
            {
                result.Success = false;
                result.ErrorMessage = "TargetStageId is required for GoToStage action";
                return result;
            }

            try
            {
                // Validate target stage exists
                var targetStage = await _stageRepository.GetByIdAsync(action.TargetStageId.Value);
                if (targetStage == null || !targetStage.IsActive)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Target stage {action.TargetStageId} not found or inactive";
                    return result;
                }

                // Get onboarding - use GetByIdWithoutTenantFilterAsync to avoid tenant filter issues in background tasks
                var onboarding = await _onboardingRepository.GetByIdWithoutTenantFilterAsync(context.OnboardingId);
                if (onboarding == null)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Onboarding {context.OnboardingId} not found";
                    return result;
                }

                // Get current stage for comparison
                var currentStage = await _stageRepository.GetByIdAsync(context.StageId);

                // Mark skipped stages if jumping forward
                if (currentStage != null && targetStage.Order > currentStage.Order)
                {
                    await MarkSkippedStagesAsync(onboarding, currentStage.Order, targetStage.Order);
                }

                // Update onboarding current stage
                await _db.Updateable<Onboarding>()
                    .SetColumns(o => new Onboarding
                    {
                        CurrentStageId = action.TargetStageId.Value,
                        CurrentStageOrder = targetStage.Order,
                        ModifyDate = DateTimeOffset.UtcNow,
                        ModifyBy = _userContext.UserName ?? "SYSTEM"
                    })
                    .Where(o => o.Id == context.OnboardingId)
                    .ExecuteCommandAsync();

                result.Success = true;
                result.ResultData["targetStageId"] = action.TargetStageId.Value;
                result.ResultData["targetStageName"] = targetStage.Name;

                _logger.LogInformation("GoToStage executed: Onboarding {OnboardingId} moved to stage {StageId} ({StageName})",
                    context.OnboardingId, action.TargetStageId.Value, targetStage.Name);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Execute SkipStage action - skip the next N stages
        /// </summary>
        private async Task<ActionExecutionDetail> ExecuteSkipStageAsync(ConditionAction action, ActionExecutionContext context)
        {
            var result = new ActionExecutionDetail
            {
                ActionType = "SkipStage",
                Order = action.Order,
                ResultData = new Dictionary<string, object>()
            };

            try
            {
                // Get current stage
                var currentStage = await _stageRepository.GetByIdAsync(context.StageId);
                if (currentStage == null)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Current stage {context.StageId} not found";
                    return result;
                }

                // Get stages to skip
                var skipCount = action.SkipCount > 0 ? action.SkipCount : 1;
                var nextStages = await _db.Queryable<Stage>()
                    .Where(s => s.WorkflowId == currentStage.WorkflowId && s.IsValid && s.IsActive)
                    .Where(s => s.TenantId == context.TenantId)
                    .Where(s => s.Order > currentStage.Order)
                    .OrderBy(s => s.Order)
                    .Take(skipCount + 1)
                    .ToListAsync();

                if (nextStages.Count <= skipCount)
                {
                    // Not enough stages to skip, go to last stage or end workflow
                    var lastStage = nextStages.LastOrDefault();
                    if (lastStage != null)
                    {
                        action.TargetStageId = lastStage.Id;
                        return await ExecuteGoToStageAsync(action, context);
                    }
                    else
                    {
                        // No more stages, end workflow
                        return await ExecuteEndWorkflowAsync(action, context);
                    }
                }

                // Target stage is the one after skipped stages
                var targetStage = nextStages[skipCount];
                action.TargetStageId = targetStage.Id;

                // Mark skipped stages - use GetByIdWithoutTenantFilterAsync to avoid tenant filter issues in background tasks
                var onboarding = await _onboardingRepository.GetByIdWithoutTenantFilterAsync(context.OnboardingId);
                if (onboarding != null)
                {
                    await MarkSkippedStagesAsync(onboarding, currentStage.Order, targetStage.Order);
                }

                // Use GoToStage to complete the action
                var goToResult = await ExecuteGoToStageAsync(action, context);
                result.Success = goToResult.Success;
                result.ErrorMessage = goToResult.ErrorMessage;
                result.ResultData["skippedCount"] = skipCount;
                result.ResultData["targetStageId"] = targetStage.Id;
                result.ResultData["targetStageName"] = targetStage.Name;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Execute EndWorkflow action - end the workflow (equivalent to Force Complete)
        /// </summary>
        private async Task<ActionExecutionDetail> ExecuteEndWorkflowAsync(ConditionAction action, ActionExecutionContext context)
        {
            var result = new ActionExecutionDetail
            {
                ActionType = "EndWorkflow",
                Order = action.Order,
                ResultData = new Dictionary<string, object>()
            };

            try
            {
                var endStatus = action.EndStatus ?? "Force Completed";

                // Get onboarding to check current status
                var onboarding = await _onboardingRepository.GetByIdWithoutTenantFilterAsync(context.OnboardingId);
                if (onboarding == null)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Onboarding {context.OnboardingId} not found";
                    return result;
                }

                // Skip if already completed
                if (onboarding.Status == "Completed" || onboarding.Status == "Force Completed")
                {
                    result.Success = true;
                    result.ResultData["endStatus"] = onboarding.Status;
                    result.ResultData["message"] = "Onboarding already completed";
                    _logger.LogInformation("EndWorkflow skipped: Onboarding {OnboardingId} already has status {Status}",
                        context.OnboardingId, onboarding.Status);
                    return result;
                }

                // Build completion notes
                var completionNotes = $"[EndWorkflow Action] Workflow ended by Stage Condition - ConditionId: {context.ConditionId}";

                // Update onboarding status - equivalent to Force Complete operation
                // Important: Do NOT modify stagesProgress data - keep it as is (same as ForceCompleteAsync)
                await _db.Updateable<Onboarding>()
                    .SetColumns(o => new Onboarding
                    {
                        Status = endStatus,
                        CompletionRate = 100,
                        ActualCompletionDate = DateTimeOffset.UtcNow,
                        ModifyDate = DateTimeOffset.UtcNow,
                        ModifyBy = _userContext.UserName ?? "SYSTEM"
                    })
                    .Where(o => o.Id == context.OnboardingId)
                    .ExecuteCommandAsync();

                // Append notes using raw SQL to avoid overwriting existing notes
                var existingNotes = onboarding.Notes ?? "";
                var newNotes = string.IsNullOrEmpty(existingNotes) 
                    ? completionNotes 
                    : $"{existingNotes}\n{completionNotes}";
                
                await _db.Updateable<Onboarding>()
                    .SetColumns(o => new Onboarding { Notes = newNotes })
                    .Where(o => o.Id == context.OnboardingId)
                    .ExecuteCommandAsync();

                result.Success = true;
                result.ResultData["endStatus"] = endStatus;
                result.ResultData["completionRate"] = 100;
                result.ResultData["actualCompletionDate"] = DateTimeOffset.UtcNow;
                result.ResultData["previousStatus"] = onboarding.Status;

                _logger.LogInformation("EndWorkflow executed (Force Complete): Onboarding {OnboardingId} ended with status {Status}, previous status was {PreviousStatus}",
                    context.OnboardingId, endStatus, onboarding.Status);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error executing EndWorkflow action for OnboardingId={OnboardingId}", context.OnboardingId);
            }

            return result;
        }


        /// <summary>
        /// Execute SendNotification action - send notification to user or team members
        /// </summary>
        private async Task<ActionExecutionDetail> ExecuteSendNotificationAsync(ConditionAction action, ActionExecutionContext context)
        {
            var result = new ActionExecutionDetail
            {
                ActionType = "SendNotification",
                Order = action.Order,
                ResultData = new Dictionary<string, object>()
            };

            try
            {
                // Get recipient info from action or parameters
                var recipientType = action.RecipientType;
                var recipientId = action.RecipientId;
                var templateId = action.TemplateId;

                // Try to get from parameters if not set at top level
                if (action.Parameters != null)
                {
                    if (string.IsNullOrEmpty(recipientType) && action.Parameters.TryGetValue("recipientType", out var typeObj))
                    {
                        recipientType = typeObj?.ToString();
                    }
                    if (string.IsNullOrEmpty(recipientId) && action.Parameters.TryGetValue("recipientId", out var idObj))
                    {
                        recipientId = idObj?.ToString();
                    }
                    // Also support recipientEmail parameter directly
                    if (string.IsNullOrEmpty(recipientId) && action.Parameters.TryGetValue("recipientEmail", out var emailObj))
                    {
                        recipientId = emailObj?.ToString();
                        // If recipientEmail is provided directly, treat it as email type
                        if (string.IsNullOrEmpty(recipientType))
                        {
                            recipientType = "email";
                        }
                    }
                    if (string.IsNullOrEmpty(templateId) && action.Parameters.TryGetValue("templateId", out var templateObj))
                    {
                        templateId = templateObj?.ToString();
                    }
                }

                if (string.IsNullOrEmpty(recipientId))
                {
                    result.Success = false;
                    result.ErrorMessage = "RecipientId or RecipientEmail is required for SendNotification action";
                    return result;
                }

                // Get onboarding info for email content (needed for all recipient types)
                var onboarding = await _onboardingRepository.GetByIdWithoutTenantFilterAsync(context.OnboardingId);
                var stage = await _stageRepository.GetByIdAsync(context.StageId);

                var caseName = onboarding?.CaseName ?? $"Case #{context.OnboardingId}";
                var previousStageName = stage?.Name ?? "Previous Stage";
                
                // Get current stage name - the stage that onboarding is currently at
                var currentStageName = "Current Stage";
                if (onboarding != null && onboarding.CurrentStageId.HasValue)
                {
                    var currentStage = await _stageRepository.GetByIdAsync(onboarding.CurrentStageId.Value);
                    if (currentStage != null)
                    {
                        currentStageName = currentStage.Name;
                    }
                }
                
                var caseUrl = $"/onboarding/{context.OnboardingId}";

                // Handle team type - send notification to all team members
                if (recipientType?.ToLower() == "team")
                {
                    return await ExecuteSendNotificationToTeamAsync(recipientId, context, result, templateId, caseName, previousStageName, currentStageName, caseUrl);
                }

                // Get recipient email based on recipientType (user or email)
                string recipientEmail = null;
                string recipientName = null;

                if (recipientType?.ToLower() == "user")
                {
                    // Get user email using UserService (supports IDM integration)
                    if (long.TryParse(recipientId, out var userId))
                    {
                        var users = await _userService.GetUsersByIdsAsync(new List<long> { userId }, context.TenantId);
                        var user = users?.FirstOrDefault();

                        if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                        {
                            recipientEmail = user.Email;
                            recipientName = user.Username ?? user.Email;
                            _logger.LogDebug("Found user {UserId} with email {Email} via UserService", userId, recipientEmail);
                        }
                        else
                        {
                            _logger.LogWarning("User {UserId} not found or has no email via UserService", userId);
                        }
                    }
                }
                else if (recipientType?.ToLower() == "email")
                {
                    // recipientId is the email address directly
                    recipientEmail = recipientId;
                    recipientName = recipientId;
                }

                if (string.IsNullOrEmpty(recipientEmail))
                {
                    result.Success = false;
                    result.ErrorMessage = $"Could not find email for recipient: {recipientType}={recipientId}";
                    return result;
                }

                // Send condition stage notification email (uses "current stage" instead of "next stage")
                var emailSent = await _emailService.SendConditionStageNotificationAsync(
                    recipientEmail,
                    context.OnboardingId.ToString(),
                    caseName,
                    previousStageName,
                    currentStageName,
                    caseUrl);

                result.Success = emailSent;
                result.ResultData["recipientType"] = recipientType ?? string.Empty;
                result.ResultData["recipientId"] = recipientId ?? string.Empty;
                result.ResultData["recipientEmail"] = recipientEmail;
                result.ResultData["recipientName"] = recipientName ?? string.Empty;
                result.ResultData["templateId"] = templateId ?? string.Empty;
                result.ResultData["previousStageName"] = previousStageName;
                result.ResultData["currentStageName"] = currentStageName;
                result.ResultData["status"] = emailSent ? "Sent" : "Failed";

                if (emailSent)
                {
                    _logger.LogInformation("SendNotification executed: Email sent to {RecipientEmail} for onboarding {OnboardingId}",
                        recipientEmail, context.OnboardingId);
                }
                else
                {
                    result.ErrorMessage = "Failed to send email";
                    _logger.LogWarning("SendNotification failed: Could not send email to {RecipientEmail} for onboarding {OnboardingId}",
                        recipientEmail, context.OnboardingId);
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error executing SendNotification action");
            }

            return result;
        }

        /// <summary>
        /// Send notification to all members of a team
        /// </summary>
        private async Task<ActionExecutionDetail> ExecuteSendNotificationToTeamAsync(
            string teamId,
            ActionExecutionContext context,
            ActionExecutionDetail result,
            string templateId,
            string caseName,
            string previousStageName,
            string currentStageName,
            string caseUrl)
        {
            try
            {
                _logger.LogInformation("SendNotification to team: Getting team members for teamId={TeamId}, tenantId={TenantId}",
                    teamId, context.TenantId);

                // Get all team users from IDM
                var teamUsers = await _idmUserDataClient.GetAllTeamUsersAsync(context.TenantId, 10000, 1);

                if (teamUsers == null || !teamUsers.Any())
                {
                    result.Success = false;
                    result.ErrorMessage = $"No team users found for tenant {context.TenantId}";
                    _logger.LogWarning("SendNotification to team failed: No team users found for tenant {TenantId}", context.TenantId);
                    return result;
                }

                // Filter users by teamId
                var teamMembers = teamUsers.Where(tu => tu.TeamId == teamId).ToList();

                if (!teamMembers.Any())
                {
                    result.Success = false;
                    result.ErrorMessage = $"No members found for team {teamId}";
                    _logger.LogWarning("SendNotification to team failed: No members found for teamId={TeamId}", teamId);
                    return result;
                }

                _logger.LogInformation("Found {MemberCount} members in team {TeamId}", teamMembers.Count, teamId);

                // Send email to each team member with valid email
                var sentEmails = new List<string>();
                var failedEmails = new List<string>();

                foreach (var member in teamMembers)
                {
                    if (string.IsNullOrWhiteSpace(member.Email))
                    {
                        _logger.LogDebug("Skipping team member {UserName} - no email address", member.UserName);
                        continue;
                    }

                    try
                    {
                        var emailSent = await _emailService.SendConditionStageNotificationAsync(
                            member.Email,
                            context.OnboardingId.ToString(),
                            caseName,
                            previousStageName,
                            currentStageName,
                            caseUrl);

                        if (emailSent)
                        {
                            sentEmails.Add(member.Email);
                            _logger.LogDebug("Email sent to team member {Email}", member.Email);
                        }
                        else
                        {
                            failedEmails.Add(member.Email);
                            _logger.LogWarning("Failed to send email to team member {Email}", member.Email);
                        }
                    }
                    catch (Exception ex)
                    {
                        failedEmails.Add(member.Email);
                        _logger.LogWarning(ex, "Exception sending email to team member {Email}", member.Email);
                    }
                }

                // Determine overall success
                result.Success = sentEmails.Any();
                result.ResultData["recipientType"] = "team";
                result.ResultData["recipientId"] = teamId;
                result.ResultData["teamMemberCount"] = teamMembers.Count;
                result.ResultData["sentCount"] = sentEmails.Count;
                result.ResultData["failedCount"] = failedEmails.Count;
                result.ResultData["sentEmails"] = sentEmails;
                result.ResultData["failedEmails"] = failedEmails;
                result.ResultData["templateId"] = templateId ?? string.Empty;
                result.ResultData["previousStageName"] = previousStageName;
                result.ResultData["currentStageName"] = currentStageName;
                result.ResultData["status"] = sentEmails.Any() ? "Sent" : "Failed";

                if (sentEmails.Any())
                {
                    _logger.LogInformation("SendNotification to team executed: {SentCount}/{TotalCount} emails sent for team {TeamId}, onboarding {OnboardingId}",
                        sentEmails.Count, teamMembers.Count, teamId, context.OnboardingId);
                }
                else
                {
                    result.ErrorMessage = $"Failed to send email to any team member. Team has {teamMembers.Count} members.";
                    _logger.LogWarning("SendNotification to team failed: No emails sent for team {TeamId}", teamId);
                }

                if (failedEmails.Any())
                {
                    var failedMessage = $"Failed to send to {failedEmails.Count} members: {string.Join(", ", failedEmails)}";
                    if (string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        result.ErrorMessage = failedMessage;
                    }
                    else
                    {
                        result.ErrorMessage += $"; {failedMessage}";
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Error getting team members: {ex.Message}";
                _logger.LogError(ex, "Error executing SendNotification to team {TeamId}", teamId);
            }

            return result;
        }

        /// <summary>
        /// Execute UpdateField action - update a field value in onboarding
        /// </summary>
        private async Task<ActionExecutionDetail> ExecuteUpdateFieldAsync(ConditionAction action, ActionExecutionContext context)
        {
            var result = new ActionExecutionDetail
            {
                ActionType = "UpdateField",
                Order = action.Order,
                ResultData = new Dictionary<string, object>()
            };

            // Support stageId, fieldId, fieldName, and parameters
            var stageId = action.StageId;
            var fieldId = action.FieldId;
            var fieldName = action.FieldName;
            var fieldValue = action.FieldValue;

            // If top-level fields are empty, try to get from parameters
            if (action.Parameters != null)
            {
                if (!stageId.HasValue && action.Parameters.TryGetValue("stageId", out var stageIdObj))
                {
                    if (long.TryParse(stageIdObj?.ToString(), out var parsedStageId))
                    {
                        stageId = parsedStageId;
                    }
                }
                if (string.IsNullOrEmpty(fieldId) && action.Parameters.TryGetValue("fieldId", out var fieldIdObj))
                {
                    fieldId = fieldIdObj?.ToString();
                }
                if (string.IsNullOrEmpty(fieldName))
                {
                    if (action.Parameters.TryGetValue("fieldPath", out var fieldPathObj))
                    {
                        fieldName = fieldPathObj?.ToString();
                    }
                    else if (action.Parameters.TryGetValue("fieldName", out var fieldNameObj))
                    {
                        fieldName = fieldNameObj?.ToString();
                    }
                }

                // Get value from parameters if not set at top level
                if (fieldValue == null)
                {
                    if (action.Parameters.TryGetValue("newValue", out var newValueObj))
                    {
                        fieldValue = newValueObj;
                    }
                    else if (action.Parameters.TryGetValue("fieldValue", out var fieldValueObj))
                    {
                        fieldValue = fieldValueObj;
                    }
                }
            }

            // Use fieldId as key if provided, otherwise use fieldName
            var fieldKey = !string.IsNullOrEmpty(fieldId) ? fieldId : fieldName;

            if (string.IsNullOrEmpty(fieldKey))
            {
                result.Success = false;
                result.ErrorMessage = "FieldId, FieldName or parameters.fieldPath is required for UpdateField action";
                return result;
            }

            try
            {
                // Verify onboarding exists
                var onboarding = await _onboardingRepository.GetByIdAsync(context.OnboardingId);
                if (onboarding == null)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Onboarding {context.OnboardingId} not found";
                    return result;
                }

                // Always use stageId = 0 for case-level shared fields
                const long caseSharedStageId = 0;

                // Resolve the actual fieldName from property definition if fieldId is provided
                var storageFieldName = fieldKey;
                var displayFieldName = fieldName ?? fieldKey;
                
                if (!string.IsNullOrEmpty(fieldId) && long.TryParse(fieldId, out var propertyId))
                {
                    try
                    {
                        var property = await _propertyService.GetPropertyByIdAsync(propertyId);
                        if (property != null && !string.IsNullOrEmpty(property.FieldName))
                        {
                            // Use the actual fieldName from property definition
                            storageFieldName = property.FieldName;
                            displayFieldName = property.DisplayName ?? property.FieldName;
                            _logger.LogDebug("Resolved fieldId {FieldId} to fieldName {FieldName}", fieldId, storageFieldName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to resolve fieldId {FieldId} to fieldName, using fieldId as key", fieldId);
                    }
                }

                // Save field value to StaticFieldValue table
                var staticFieldInput = new StaticFieldValueInputDto
                {
                    OnboardingId = context.OnboardingId,
                    StageId = caseSharedStageId,
                    FieldName = storageFieldName,
                    FieldId = !string.IsNullOrEmpty(fieldId) && long.TryParse(fieldId, out var parsedFieldId) ? parsedFieldId : null,
                    DisplayName = displayFieldName,
                    FieldLabel = displayFieldName,
                    FieldValueJson = JsonConvert.SerializeObject(fieldValue),
                    FieldType = "text",
                    Source = "stage_condition",
                    Status = "Submitted"
                };

                var batchInput = new BatchStaticFieldValueInputDto
                {
                    OnboardingId = context.OnboardingId,
                    StageId = caseSharedStageId,
                    FieldValues = new List<StaticFieldValueInputDto> { staticFieldInput },
                    OverwriteExisting = true,
                    Source = "stage_condition"
                };

                await _staticFieldValueService.BatchSaveAsync(batchInput);

                result.Success = true;
                result.ResultData["stageId"] = caseSharedStageId;
                result.ResultData["fieldId"] = fieldId ?? string.Empty;
                result.ResultData["fieldName"] = storageFieldName;
                result.ResultData["fieldKey"] = storageFieldName;
                result.ResultData["newValue"] = fieldValue!;

                _logger.LogInformation("UpdateField executed: Onboarding {OnboardingId}, Field {FieldKey} set to {NewValue} (case-level shared)",
                    context.OnboardingId, storageFieldName, fieldValue);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Execute TriggerAction action - publish ActionTriggerEvent to trigger predefined action
        /// </summary>
        private async Task<ActionExecutionDetail> ExecuteTriggerActionAsync(ConditionAction action, ActionExecutionContext context)
        {
            var result = new ActionExecutionDetail
            {
                ActionType = "TriggerAction",
                Order = action.Order,
                ResultData = new Dictionary<string, object>()
            };

            if (!action.ActionDefinitionId.HasValue)
            {
                result.Success = false;
                result.ErrorMessage = "ActionDefinitionId is required for TriggerAction action";
                return result;
            }

            try
            {
                // Validate action definition exists
                var actionDefinition = await _db.Queryable<Domain.Entities.Action.ActionDefinition>()
                    .Where(a => a.Id == action.ActionDefinitionId.Value && a.IsValid)
                    .Where(a => a.TenantId == context.TenantId)
                    .FirstAsync();

                if (actionDefinition == null || !actionDefinition.IsEnabled)
                {
                    result.Success = false;
                    result.ErrorMessage = $"ActionDefinition {action.ActionDefinitionId} not found or disabled";
                    return result;
                }

                // Get onboarding info for context data
                var onboarding = await _onboardingRepository.GetByIdWithoutTenantFilterAsync(context.OnboardingId);

                // Build context data for action execution
                var contextData = new
                {
                    OnboardingId = context.OnboardingId,
                    StageId = context.StageId,
                    ConditionId = context.ConditionId,
                    TenantId = context.TenantId,
                    ActionDefinitionId = action.ActionDefinitionId.Value,
                    ActionName = actionDefinition.ActionName,
                    TriggerSource = "StageCondition",
                    CaseName = onboarding?.CaseName,
                    CaseCode = onboarding?.CaseCode,
                    WorkflowId = onboarding?.WorkflowId
                };

                // Get current user ID
                long? currentUserId = null;
                if (long.TryParse(_userContext.UserId, out var userId))
                {
                    currentUserId = userId;
                }

                // Execute action directly using ActionExecutionService
                _logger.LogInformation("TriggerAction: Executing ActionDefinitionId={ActionDefinitionId}, ActionName={ActionName}, OnboardingId={OnboardingId}",
                    action.ActionDefinitionId, actionDefinition.ActionName, context.OnboardingId);

                var executionResult = await _actionExecutionService.ExecuteActionAsync(
                    action.ActionDefinitionId.Value,
                    contextData,
                    currentUserId);

                _logger.LogInformation("TriggerAction executed successfully: ActionDefinitionId={ActionDefinitionId}, ActionName={ActionName}, OnboardingId={OnboardingId}",
                    action.ActionDefinitionId, actionDefinition.ActionName, context.OnboardingId);

                result.Success = true;
                result.ResultData["actionDefinitionId"] = action.ActionDefinitionId.Value;
                result.ResultData["actionName"] = actionDefinition.ActionName;
                result.ResultData["status"] = "Executed";
                result.ResultData["executionResult"] = executionResult?.ToString() ?? "null";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error executing TriggerAction for ActionDefinitionId={ActionDefinitionId}", action.ActionDefinitionId);
            }

            return result;
        }

        /// <summary>
        /// Execute AssignUser action - assign user or team to current stage's CustomStageAssignee
        /// </summary>
        private async Task<ActionExecutionDetail> ExecuteAssignUserAsync(ConditionAction action, ActionExecutionContext context)
        {
            var result = new ActionExecutionDetail
            {
                ActionType = "AssignUser",
                Order = action.Order,
                ResultData = new Dictionary<string, object>()
            };

            // Get assigneeType and assigneeIds from parameters
            string? assigneeType = null;
            List<string> assigneeIds = new List<string>();

            if (action.Parameters != null)
            {
                if (action.Parameters.TryGetValue("assigneeType", out var typeObj))
                {
                    assigneeType = typeObj?.ToString()?.ToLower();
                }

                if (action.Parameters.TryGetValue("assigneeIds", out var idsObj))
                {
                    _logger.LogDebug("AssignUser: assigneeIds raw value type={Type}, value={Value}", 
                        idsObj?.GetType().FullName ?? "null", idsObj?.ToString() ?? "null");
                    
                    if (idsObj is Newtonsoft.Json.Linq.JArray jArray)
                    {
                        assigneeIds = jArray.Select(x => x.ToString()).ToList();
                        _logger.LogDebug("AssignUser: Parsed as JArray, count={Count}", assigneeIds.Count);
                    }
                    else if (idsObj is Newtonsoft.Json.Linq.JToken jToken)
                    {
                        // Handle JToken (could be JArray or JValue)
                        if (jToken.Type == Newtonsoft.Json.Linq.JTokenType.Array)
                        {
                            assigneeIds = jToken.ToObject<List<string>>() ?? new List<string>();
                            _logger.LogDebug("AssignUser: Parsed JToken as array, count={Count}", assigneeIds.Count);
                        }
                        else
                        {
                            var val = jToken.ToString();
                            if (!string.IsNullOrEmpty(val))
                            {
                                assigneeIds.Add(val);
                            }
                        }
                    }
                    else if (idsObj is List<object> objList)
                    {
                        assigneeIds = objList.Select(x => x?.ToString() ?? "").Where(x => !string.IsNullOrEmpty(x)).ToList();
                        _logger.LogDebug("AssignUser: Parsed as List<object>, count={Count}", assigneeIds.Count);
                    }
                    else if (idsObj is IEnumerable<object> enumerable && !(idsObj is string))
                    {
                        assigneeIds = enumerable.Select(x => x?.ToString() ?? "").Where(x => !string.IsNullOrEmpty(x)).ToList();
                        _logger.LogDebug("AssignUser: Parsed as IEnumerable, count={Count}", assigneeIds.Count);
                    }
                    else if (idsObj is string strValue)
                    {
                        _logger.LogDebug("AssignUser: assigneeIds is string: {Value}", strValue);
                        // Handle case where assigneeIds is a JSON string array
                        if (strValue.TrimStart().StartsWith("["))
                        {
                            try
                            {
                                var parsed = JsonConvert.DeserializeObject<List<string>>(strValue);
                                if (parsed != null)
                                {
                                    assigneeIds = parsed;
                                    _logger.LogDebug("AssignUser: Parsed JSON string as List, count={Count}", assigneeIds.Count);
                                }
                            }
                            catch
                            {
                                assigneeIds.Add(strValue);
                            }
                        }
                        else if (!string.IsNullOrEmpty(strValue))
                        {
                            assigneeIds.Add(strValue);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("AssignUser: Unhandled assigneeIds type: {Type}", idsObj?.GetType().FullName ?? "null");
                        // Try to convert to string and add
                        var strVal = idsObj?.ToString();
                        if (!string.IsNullOrEmpty(strVal))
                        {
                            assigneeIds.Add(strVal);
                        }
                    }
                }
            }

            // Fallback to legacy top-level properties
            if (string.IsNullOrEmpty(assigneeType))
            {
                if (action.UserId.HasValue)
                {
                    assigneeType = "user";
                    assigneeIds.Add(action.UserId.Value.ToString());
                }
                else if (!string.IsNullOrEmpty(action.TeamId))
                {
                    assigneeType = "team";
                    assigneeIds.Add(action.TeamId);
                }
            }

            if (string.IsNullOrEmpty(assigneeType) || !assigneeIds.Any())
            {
                result.Success = false;
                result.ErrorMessage = "AssignUser action requires assigneeType and assigneeIds in parameters";
                return result;
            }

            try
            {
                // Get onboarding - use GetByIdWithoutTenantFilterAsync to avoid tenant filter issues
                var onboarding = await _onboardingRepository.GetByIdWithoutTenantFilterAsync(context.OnboardingId);
                if (onboarding == null)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Onboarding {context.OnboardingId} not found";
                    return result;
                }

                // Get current stage ID from context (the completed stage)
                var currentStageId = context.StageId;

                // Parse StagesProgress from StagesProgressJson (since StagesProgress is [SugarColumn(IsIgnore = true)])
                // Handle double-encoded JSON (string containing escaped JSON)
                var stagesProgress = new List<OnboardingStageProgress>();
                if (!string.IsNullOrEmpty(onboarding.StagesProgressJson))
                {
                    try
                    {
                        var jsonValue = onboarding.StagesProgressJson.Trim();
                        
                        // Check if it's double-encoded (starts with quote, indicating a string value)
                        if (jsonValue.StartsWith("\""))
                        {
                            // First deserialize to get the inner JSON string
                            var innerJson = JsonConvert.DeserializeObject<string>(jsonValue);
                            if (!string.IsNullOrEmpty(innerJson))
                            {
                                stagesProgress = JsonConvert.DeserializeObject<List<OnboardingStageProgress>>(innerJson) 
                                    ?? new List<OnboardingStageProgress>();
                            }
                        }
                        else
                        {
                            // Normal JSON array
                            stagesProgress = JsonConvert.DeserializeObject<List<OnboardingStageProgress>>(jsonValue) 
                                ?? new List<OnboardingStageProgress>();
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse StagesProgressJson for Onboarding {OnboardingId}", context.OnboardingId);
                    }
                }

                // Find the stage progress for the current stage
                var stageProgress = stagesProgress.FirstOrDefault(sp => sp.StageId == currentStageId);

                if (stageProgress == null)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Stage progress not found for StageId {currentStageId}";
                    _logger.LogWarning("AssignUser: Stage progress not found. OnboardingId={OnboardingId}, StageId={StageId}, AvailableStageIds={AvailableStageIds}",
                        context.OnboardingId, currentStageId, string.Join(",", stagesProgress.Select(sp => sp.StageId)));
                    return result;
                }

                // Store original values for logging
                var originalCustomAssignee = stageProgress.CustomStageAssignee?.ToList() ?? new List<string>();

                if (assigneeType == "user")
                {
                    // Update CustomStageAssignee for the current stage
                    stageProgress.CustomStageAssignee = assigneeIds;
                    stageProgress.LastUpdatedTime = DateTimeOffset.UtcNow;

                    // Serialize updated stagesProgress
                    var updatedStagesProgressJson = JsonConvert.SerializeObject(stagesProgress);

                    await _db.Updateable<Onboarding>()
                        .SetColumns(o => new Onboarding
                        {
                            StagesProgressJson = updatedStagesProgressJson,
                            ModifyDate = DateTimeOffset.UtcNow,
                            ModifyBy = _userContext.UserName ?? "SYSTEM"
                        })
                        .Where(o => o.Id == context.OnboardingId)
                        .ExecuteCommandAsync();

                    // Get user names for display
                    var assigneeNames = new List<string>();
                    var userIds = assigneeIds.Where(id => long.TryParse(id, out _)).Select(long.Parse).ToList();
                    if (userIds.Any())
                    {
                        try
                        {
                            var users = await _userService.GetUsersByIdsAsync(userIds, context.TenantId);
                            assigneeNames = users?.Select(u => u.Username ?? u.Email ?? u.Id.ToString()).ToList() ?? new List<string>();
                        }
                        catch
                        {
                            // Fallback to IDs if user lookup fails
                            assigneeNames = assigneeIds.ToList();
                        }
                    }

                    result.ResultData["assigneeType"] = "user";
                    result.ResultData["assigneeIds"] = assigneeIds;
                    result.ResultData["assigneeNames"] = assigneeNames;
                    result.ResultData["assigneeCount"] = assigneeIds.Count;
                    result.ResultData["stageId"] = currentStageId;
                    result.ResultData["originalCustomStageAssignee"] = originalCustomAssignee;
                    result.ResultData["newCustomStageAssignee"] = assigneeIds;

                    _logger.LogInformation("AssignUser executed: Updated CustomStageAssignee for Onboarding {OnboardingId}, StageId={StageId}, OldAssignee={OldAssignee}, NewAssignee={NewAssignee}",
                        context.OnboardingId, currentStageId, string.Join(",", originalCustomAssignee), string.Join(",", assigneeIds));
                }
                else if (assigneeType == "team")
                {
                    // For team assignment, get team members and update CustomStageAssignee with member user IDs
                    var teamIds = assigneeIds;
                    var memberUserIds = new List<string>();
                    var memberNames = new List<string>();

                    _logger.LogDebug("AssignUser team: Starting team member lookup for teams: {TeamIds}, TenantId: {TenantId}",
                        string.Join(",", teamIds), context.TenantId);

                    // Get all team users from IDM
                    var teamUsers = await _idmUserDataClient.GetAllTeamUsersAsync(context.TenantId, 10000, 1);

                    _logger.LogDebug("AssignUser team: IDM returned {TotalUsers} team users", teamUsers?.Count ?? 0);

                    if (teamUsers != null && teamUsers.Any())
                    {
                        // Log sample of available team IDs for debugging
                        var availableTeamIds = teamUsers.Select(tu => tu.TeamId).Distinct().Take(10).ToList();
                        _logger.LogDebug("AssignUser team: Available team IDs (sample): {AvailableTeamIds}",
                            string.Join(",", availableTeamIds));

                        // Filter users by teamIds and collect their user IDs and names
                        foreach (var teamId in teamIds)
                        {
                            var teamMembers = teamUsers.Where(tu => tu.TeamId == teamId).ToList();
                            _logger.LogDebug("AssignUser team: Team {TeamId} has {MemberCount} members",
                                teamId, teamMembers.Count);

                            foreach (var member in teamMembers)
                            {
                                if (!string.IsNullOrEmpty(member.Id) && !memberUserIds.Contains(member.Id))
                                {
                                    memberUserIds.Add(member.Id);
                                    // Collect member display name
                                    var displayName = !string.IsNullOrEmpty(member.FirstName) || !string.IsNullOrEmpty(member.LastName)
                                        ? $"{member.FirstName} {member.LastName}".Trim()
                                        : member.UserName ?? member.Email ?? member.Id;
                                    memberNames.Add(displayName);
                                    _logger.LogDebug("AssignUser team: Added member Id={MemberId}, UserName={UserName}",
                                        member.Id, member.UserName);
                                }
                            }
                        }

                        _logger.LogInformation("AssignUser team: Found {MemberCount} members from {TeamCount} teams: {TeamIds}",
                            memberUserIds.Count, teamIds.Count, string.Join(",", teamIds));
                    }
                    else
                    {
                        _logger.LogWarning("AssignUser team: IDM returned no team users for tenant {TenantId}", context.TenantId);
                    }

                    if (!memberUserIds.Any())
                    {
                        _logger.LogWarning("AssignUser team: No members found for teams {TeamIds}, will use team IDs as fallback",
                            string.Join(",", teamIds));
                        // Fallback: use team IDs if no members found
                        memberUserIds = teamIds;
                    }

                    // Update CustomStageAssignee with team member user IDs
                    stageProgress.CustomStageAssignee = memberUserIds;
                    stageProgress.LastUpdatedTime = DateTimeOffset.UtcNow;

                    // Serialize updated stagesProgress
                    var updatedStagesProgressJson = JsonConvert.SerializeObject(stagesProgress);

                    await _db.Updateable<Onboarding>()
                        .SetColumns(o => new Onboarding
                        {
                            StagesProgressJson = updatedStagesProgressJson,
                            ModifyDate = DateTimeOffset.UtcNow,
                            ModifyBy = _userContext.UserName ?? "SYSTEM"
                        })
                        .Where(o => o.Id == context.OnboardingId)
                        .ExecuteCommandAsync();

                    result.ResultData["assigneeType"] = "team";
                    result.ResultData["teamIds"] = teamIds;
                    result.ResultData["memberUserIds"] = memberUserIds;
                    result.ResultData["memberCount"] = memberUserIds.Count;
                    result.ResultData["assigneeNames"] = memberNames;
                    result.ResultData["assigneeCount"] = memberUserIds.Count;
                    result.ResultData["stageId"] = currentStageId;
                    result.ResultData["originalCustomStageAssignee"] = originalCustomAssignee;
                    result.ResultData["newCustomStageAssignee"] = memberUserIds;

                    _logger.LogInformation("AssignUser executed: Updated CustomStageAssignee with team members for Onboarding {OnboardingId}, StageId={StageId}, Teams={Teams}, OldAssignee={OldAssignee}, NewAssignee={NewAssignee}",
                        context.OnboardingId, currentStageId, string.Join(",", teamIds), string.Join(",", originalCustomAssignee), string.Join(",", memberUserIds));
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error executing AssignUser action for OnboardingId={OnboardingId}, StageId={StageId}",
                    context.OnboardingId, context.StageId);
            }

            return result;
        }

        /// <summary>
        /// Parse StagesProgressJson handling double-escaped JSON strings
        /// </summary>
        private List<OnboardingStageProgress> ParseStagesProgressJson(string json, long onboardingId)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new List<OnboardingStageProgress>();
            }

            try
            {
                // First, try direct parsing as List<OnboardingStageProgress>
                return JsonConvert.DeserializeObject<List<OnboardingStageProgress>>(json) 
                    ?? new List<OnboardingStageProgress>();
            }
            catch (JsonException)
            {
                // If direct parsing fails, try to unescape first (handle double-escaped JSON)
                try
                {
                    // The JSON might be stored as a double-escaped string like: "\"[{\\\"stageId\\\":1}]\""
                    // First deserialize to get the inner string
                    var unescapedJson = JsonConvert.DeserializeObject<string>(json);
                    if (!string.IsNullOrEmpty(unescapedJson))
                    {
                        return JsonConvert.DeserializeObject<List<OnboardingStageProgress>>(unescapedJson) 
                            ?? new List<OnboardingStageProgress>();
                    }
                }
                catch (JsonException)
                {
                    // If still fails, the format is unexpected
                }

                _logger.LogWarning("Failed to parse StagesProgressJson for onboarding {OnboardingId}, starting with empty list. Raw value: {RawJson}", 
                    onboardingId, json.Length > 200 ? json.Substring(0, 200) + "..." : json);
                return new List<OnboardingStageProgress>();
            }
        }

        /// <summary>
        /// Mark stages as skipped in StagesProgressJson
        /// </summary>
        private async Task MarkSkippedStagesAsync(Onboarding onboarding, int fromOrder, int toOrder)
        {
            try
            {
                // Get stages to mark as skipped using onboarding's TenantId
                var stagesToSkip = await _db.Queryable<Stage>()
                    .Where(s => s.WorkflowId == onboarding.WorkflowId && s.IsValid && s.IsActive)
                    .Where(s => s.TenantId == onboarding.TenantId)
                    .Where(s => s.Order > fromOrder && s.Order < toOrder)
                    .ToListAsync();

                if (!stagesToSkip.Any())
                {
                    return;
                }

                // Parse StagesProgressJson - handle both string and already-parsed scenarios
                List<OnboardingStageProgress> stagesProgress;
                
                if (onboarding.StagesProgress != null && onboarding.StagesProgress.Any())
                {
                    // Use already-parsed StagesProgress if available
                    stagesProgress = onboarding.StagesProgress;
                }
                else if (!string.IsNullOrEmpty(onboarding.StagesProgressJson))
                {
                    stagesProgress = ParseStagesProgressJson(onboarding.StagesProgressJson, onboarding.Id);
                }
                else
                {
                    stagesProgress = new List<OnboardingStageProgress>();
                }

                foreach (var stage in stagesToSkip)
                {
                    var existingProgress = stagesProgress.FirstOrDefault(p => p.StageId == stage.Id);
                    if (existingProgress != null)
                    {
                        existingProgress.Status = "Skipped";
                        existingProgress.IsCompleted = false;
                    }
                    else
                    {
                        stagesProgress.Add(new OnboardingStageProgress
                        {
                            StageId = stage.Id,
                            StageName = stage.Name,
                            StageOrder = stage.Order,
                            Status = "Skipped",
                            IsCompleted = false
                        });
                    }
                }

                // Update onboarding
                await _db.Updateable<Onboarding>()
                    .SetColumns(o => new Onboarding
                    {
                        StagesProgressJson = JsonConvert.SerializeObject(stagesProgress),
                        ModifyDate = DateTimeOffset.UtcNow
                    })
                    .Where(o => o.Id == onboarding.Id)
                    .ExecuteCommandAsync();

                _logger.LogInformation("Marked {Count} stages as skipped for onboarding {OnboardingId}",
                    stagesToSkip.Count, onboarding.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking skipped stages for onboarding {OnboardingId}", onboarding.Id);
            }
        }

        /// <summary>
        /// Parse JSON string array, handling both normal and double-encoded formats
        /// </summary>
        private List<string> ParseJsonStringArray(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new List<string>();
            }

            try
            {
                // First try normal JSON array deserialization
                var result = JsonConvert.DeserializeObject<List<string>>(json);
                if (result != null)
                {
                    return result;
                }
            }
            catch (JsonException)
            {
                // If normal parsing fails, try to handle double-encoded string
                try
                {
                    // Check if it's a double-encoded JSON string (e.g., "\"[\\\"123\\\"]\"")
                    var unescaped = JsonConvert.DeserializeObject<string>(json);
                    if (!string.IsNullOrEmpty(unescaped))
                    {
                        var result = JsonConvert.DeserializeObject<List<string>>(unescaped);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
                catch
                {
                    // Ignore nested parsing errors
                }
            }

            _logger.LogWarning("Failed to parse JSON string array: {Json}", 
                json.Length > 100 ? json.Substring(0, 100) + "..." : json);
            return new List<string>();
        }

        #endregion
    }
}
