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
using FlowFlex.Application.Helpers;
using FlowFlex.Application.Services.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Const;
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
        /// Execute a single action with proper error handling and timeout control
        /// </summary>
        private async Task<ActionExecutionDetail> ExecuteActionAsync(ConditionAction action, ActionExecutionContext context)
        {
            var actionType = action.Type?.ToLower() ?? string.Empty;
            
            // Determine timeout based on action type
            var timeoutSeconds = GetActionTimeout(actionType);
            
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                
                var executeTask = actionType switch
                {
                    StageConditionConstants.ActionTypeGoToStage => ExecuteGoToStageAsync(action, context),
                    StageConditionConstants.ActionTypeSkipStage => ExecuteSkipStageAsync(action, context),
                    StageConditionConstants.ActionTypeEndWorkflow => ExecuteEndWorkflowAsync(action, context),
                    StageConditionConstants.ActionTypeSendNotification => ExecuteSendNotificationAsync(action, context),
                    StageConditionConstants.ActionTypeUpdateField => ExecuteUpdateFieldAsync(action, context),
                    StageConditionConstants.ActionTypeTriggerAction => ExecuteTriggerActionAsync(action, context),
                    StageConditionConstants.ActionTypeAssignUser => ExecuteAssignUserAsync(action, context),
                    _ => Task.FromResult(new ActionExecutionDetail
                    {
                        ActionType = action.Type,
                        Order = action.Order,
                        Success = false,
                        ErrorMessage = $"Unsupported action type: {action.Type}"
                    })
                };
                
                // Wait for action to complete or timeout
                var completedTask = await Task.WhenAny(executeTask, Task.Delay(Timeout.Infinite, cts.Token));
                
                if (completedTask == executeTask)
                {
                    return await executeTask;
                }
                else
                {
                    // Timeout occurred
                    _logger.LogWarning("Action {ActionType} timed out after {TimeoutSeconds}s for condition {ConditionId}",
                        action.Type, timeoutSeconds, context.ConditionId);
                    return new ActionExecutionDetail
                    {
                        ActionType = action.Type,
                        Order = action.Order,
                        Success = false,
                        ErrorMessage = $"Action execution timed out after {timeoutSeconds} seconds"
                    };
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Action {ActionType} was cancelled for condition {ConditionId}",
                    action.Type, context.ConditionId);
                return new ActionExecutionDetail
                {
                    ActionType = action.Type,
                    Order = action.Order,
                    Success = false,
                    ErrorMessage = $"Action execution timed out after {timeoutSeconds} seconds"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing action {ActionType} for condition {ConditionId}. Exception: {ExceptionType}, Message: {Message}", 
                    action.Type, context.ConditionId, ex.GetType().Name, ex.Message);
                return new ActionExecutionDetail
                {
                    ActionType = action.Type,
                    Order = action.Order,
                    Success = false,
                    ErrorMessage = $"{ex.GetType().Name}: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get timeout in seconds based on action type
        /// </summary>
        private int GetActionTimeout(string actionType)
        {
            return actionType switch
            {
                StageConditionConstants.ActionTypeSendNotification => StageConditionConstants.SendNotificationTimeoutSeconds,
                StageConditionConstants.ActionTypeTriggerAction => StageConditionConstants.TriggerActionTimeoutSeconds,
                _ => StageConditionConstants.ActionExecutionTimeoutSeconds
            };
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
                var endStatus = action.EndStatus ?? StageConditionConstants.StatusForceCompleted;

                // Get onboarding to check current status
                var onboarding = await _onboardingRepository.GetByIdWithoutTenantFilterAsync(context.OnboardingId);
                if (onboarding == null)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Onboarding {context.OnboardingId} not found";
                    return result;
                }

                // Skip if already completed
                if (onboarding.Status == StageConditionConstants.StatusCompleted || 
                    onboarding.Status == StageConditionConstants.StatusForceCompleted)
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
                var modifyBy = _userContext.UserName ?? StageConditionConstants.SystemUser;
                await _db.Updateable<Onboarding>()
                    .SetColumns(o => new Onboarding
                    {
                        Status = endStatus,
                        CompletionRate = 100,
                        ActualCompletionDate = DateTimeOffset.UtcNow,
                        ModifyDate = DateTimeOffset.UtcNow,
                        ModifyBy = modifyBy
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
                result.ErrorMessage = $"{ex.GetType().Name}: {ex.Message}";
                _logger.LogError(ex, "Error executing EndWorkflow action for OnboardingId={OnboardingId}. Exception: {ExceptionType}", 
                    context.OnboardingId, ex.GetType().Name);
            }

            return result;
        }


        /// <summary>
        /// Execute SendNotification action - send notification to users and/or team members
        /// Uses users[] and teams[] arrays with subject and emailBody parameters
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
                // Get parameters from action
                string? customSubject = null;
                string? customEmailBody = null;
                List<string> userIds = new List<string>();
                List<string> teamIds = new List<string>();

                // Parse parameters
                if (action.Parameters != null)
                {
                    if (action.Parameters.TryGetValue("users", out var usersObj) && usersObj != null)
                    {
                        userIds = JsonParseHelper.ParseToStringList(usersObj);
                    }
                    
                    if (action.Parameters.TryGetValue("teams", out var teamsObj) && teamsObj != null)
                    {
                        teamIds = JsonParseHelper.ParseToStringList(teamsObj);
                    }
                    
                    if (action.Parameters.TryGetValue("subject", out var subjectObj) && subjectObj != null)
                    {
                        customSubject = subjectObj.ToString();
                    }
                    
                    if (action.Parameters.TryGetValue("emailBody", out var emailBodyObj) && emailBodyObj != null)
                    {
                        customEmailBody = emailBodyObj.ToString();
                    }
                }

                // Validate: must have at least one recipient
                if (!userIds.Any() && !teamIds.Any())
                {
                    result.Success = false;
                    result.ErrorMessage = "Either users or teams array is required for SendNotification action";
                    return result;
                }

                _logger.LogDebug("SendNotification: Processing users={UserCount}, teams={TeamCount}, customSubject={HasSubject}, customBody={HasBody}",
                    userIds.Count, teamIds.Count, !string.IsNullOrEmpty(customSubject), !string.IsNullOrEmpty(customEmailBody));

                // Get onboarding info for email content
                var onboarding = await _onboardingRepository.GetByIdWithoutTenantFilterAsync(context.OnboardingId);
                var stage = await _stageRepository.GetByIdAsync(context.StageId);

                var caseName = onboarding?.CaseName ?? $"Case #{context.OnboardingId}";
                var previousStageName = stage?.Name ?? "Previous Stage";
                
                // Get current stage name
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

                // Process recipients
                var successCount = 0;
                var failedRecipients = new List<string>();
                var sentEmails = new List<string>();

                // Process users
                if (userIds.Any())
                {
                    var parsedUserIds = userIds
                        .Where(id => long.TryParse(id, out _))
                        .Select(id => long.Parse(id))
                        .ToList();

                    if (parsedUserIds.Any())
                    {
                        var users = await _userService.GetUsersByIdsAsync(parsedUserIds, context.TenantId);
                        var userDict = users?.ToDictionary(u => u.Id.ToString(), u => u) ?? new Dictionary<string, Application.Contracts.Dtos.OW.User.UserDto>();

                        foreach (var userId in userIds)
                        {
                            if (userDict.TryGetValue(userId, out var user) && !string.IsNullOrWhiteSpace(user.Email))
                            {
                                var emailSent = await SendEmailWithRetryAsync(
                                    user.Email,
                                    context.OnboardingId.ToString(),
                                    caseName,
                                    previousStageName,
                                    currentStageName,
                                    caseUrl,
                                    customSubject,
                                    customEmailBody);

                                if (emailSent)
                                {
                                    successCount++;
                                    sentEmails.Add(user.Email);
                                    _logger.LogDebug("SendNotification: Email sent to user {UserId} ({Email})", userId, user.Email);
                                }
                                else
                                {
                                    failedRecipients.Add($"user:{userId}(send failed)");
                                }
                            }
                            else
                            {
                                failedRecipients.Add($"user:{userId}(no email)");
                                _logger.LogWarning("SendNotification: User {UserId} not found or has no email", userId);
                            }
                        }
                    }
                    else
                    {
                        failedRecipients.AddRange(userIds.Select(id => $"user:{id}(invalid id)"));
                    }
                }

                // Process teams
                if (teamIds.Any())
                {
                    var teamResult = await ExecuteSendNotificationToTeamsAsync(
                        teamIds, context, caseName, previousStageName, currentStageName, caseUrl, customSubject, customEmailBody);
                    
                    successCount += teamResult.SuccessCount;
                    sentEmails.AddRange(teamResult.SentEmails);
                    failedRecipients.AddRange(teamResult.FailedRecipients);
                }

                // Set result data
                result.Success = successCount > 0;
                result.ResultData["users"] = userIds;
                result.ResultData["teams"] = teamIds;
                result.ResultData["subject"] = customSubject ?? string.Empty;
                result.ResultData["emailBody"] = !string.IsNullOrEmpty(customEmailBody) ? "(custom)" : "(default)";
                result.ResultData["sentEmails"] = sentEmails;
                result.ResultData["successCount"] = successCount;
                result.ResultData["failedCount"] = failedRecipients.Count;
                result.ResultData["previousStageName"] = previousStageName;
                result.ResultData["currentStageName"] = currentStageName;

                if (successCount > 0 && !failedRecipients.Any())
                {
                    _logger.LogInformation("SendNotification executed: Sent {Count} emails for onboarding {OnboardingId}",
                        successCount, context.OnboardingId);
                }
                else if (successCount > 0 && failedRecipients.Any())
                {
                    result.ErrorMessage = $"Partial success: {successCount} sent, {failedRecipients.Count} failed ({string.Join(", ", failedRecipients)})";
                    _logger.LogWarning("SendNotification partial success: {SuccessCount} sent, {FailedCount} failed for onboarding {OnboardingId}. Failed: {Failed}",
                        successCount, failedRecipients.Count, context.OnboardingId, string.Join(", ", failedRecipients));
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = $"Failed to send notifications to all recipients: {string.Join(", ", failedRecipients)}";
                    _logger.LogWarning("SendNotification failed: Could not send to any recipient for onboarding {OnboardingId}. Failed: {Failed}",
                        context.OnboardingId, string.Join(", ", failedRecipients));
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"{ex.GetType().Name}: {ex.Message}";
                _logger.LogError(ex, "Error executing SendNotification action. Exception: {ExceptionType}", ex.GetType().Name);
            }

            return result;
        }

        /// <summary>
        /// Send email with retry logic for transient failures
        /// </summary>
        private async Task<bool> SendEmailWithRetryAsync(
            string email,
            string onboardingId,
            string caseName,
            string previousStageName,
            string currentStageName,
            string caseUrl,
            string? customSubject,
            string? customEmailBody)
        {
            var retryResult = await RetryHelper.ExecuteWithRetryResultAsync(
                async () =>
                {
                    var sent = await _emailService.SendConditionStageNotificationAsync(
                        email,
                        onboardingId,
                        caseName,
                        previousStageName,
                        currentStageName,
                        caseUrl,
                        customSubject,
                        customEmailBody);
                    
                    if (!sent)
                    {
                        throw new InvalidOperationException($"Email service returned false for {email}");
                    }
                    return sent;
                },
                _logger,
                $"SendEmail to {email}",
                StageConditionConstants.MaxRetryAttempts);

            return retryResult.Success;
        }

        /// <summary>
        /// Result class for team notification sending
        /// </summary>
        private class TeamNotificationResult
        {
            public int SuccessCount { get; set; }
            public List<string> SentEmails { get; set; } = new List<string>();
            public List<string> FailedRecipients { get; set; } = new List<string>();
        }

        /// <summary>
        /// Send notifications to multiple teams with custom subject and body
        /// </summary>
        private async Task<TeamNotificationResult> ExecuteSendNotificationToTeamsAsync(
            List<string> teamIds,
            ActionExecutionContext context,
            string caseName,
            string previousStageName,
            string currentStageName,
            string caseUrl,
            string? customSubject,
            string? customEmailBody)
        {
            var result = new TeamNotificationResult();

            try
            {
                _logger.LogInformation("SendNotification to teams: Getting team members for {TeamCount} teams, tenantId={TenantId}",
                    teamIds.Count, context.TenantId);

                // Get all team users from IDM
                var teamUsers = await _idmUserDataClient.GetAllTeamUsersAsync(context.TenantId, 10000, 1);

                if (teamUsers == null || !teamUsers.Any())
                {
                    _logger.LogWarning("SendNotification to teams failed: No team users found for tenant {TenantId}", context.TenantId);
                    result.FailedRecipients.AddRange(teamIds.Select(id => $"team:{id}(no users found)"));
                    return result;
                }

                // Filter users by teamIds and collect unique emails
                var processedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var teamId in teamIds)
                {
                    var teamMembers = teamUsers.Where(tu => tu.TeamId == teamId).ToList();

                    if (!teamMembers.Any())
                    {
                        result.FailedRecipients.Add($"team:{teamId}(no members)");
                        _logger.LogWarning("SendNotification to team failed: No members found for teamId={TeamId}", teamId);
                        continue;
                    }

                    _logger.LogDebug("Found {MemberCount} members in team {TeamId}", teamMembers.Count, teamId);

                    foreach (var member in teamMembers)
                    {
                        if (string.IsNullOrWhiteSpace(member.Email))
                        {
                            _logger.LogDebug("Skipping team member {UserName} - no email address", member.UserName);
                            continue;
                        }

                        // Skip if already processed (user might be in multiple teams)
                        if (processedEmails.Contains(member.Email))
                        {
                            _logger.LogDebug("Skipping duplicate email {Email} (already sent)", member.Email);
                            continue;
                        }

                        try
                        {
                            var emailSent = await SendEmailWithRetryAsync(
                                member.Email,
                                context.OnboardingId.ToString(),
                                caseName,
                                previousStageName,
                                currentStageName,
                                caseUrl,
                                customSubject,
                                customEmailBody);

                            if (emailSent)
                            {
                                result.SuccessCount++;
                                result.SentEmails.Add(member.Email);
                                processedEmails.Add(member.Email);
                                _logger.LogDebug("Email sent to team member {Email} (team {TeamId})", member.Email, teamId);
                            }
                            else
                            {
                                result.FailedRecipients.Add($"team:{teamId}:{member.Email}(send failed)");
                                _logger.LogWarning("Failed to send email to team member {Email}", member.Email);
                            }
                        }
                        catch (Exception ex)
                        {
                            result.FailedRecipients.Add($"team:{teamId}:{member.Email}(error)");
                            _logger.LogWarning(ex, "Exception sending email to team member {Email}", member.Email);
                        }
                    }
                }

                _logger.LogInformation("SendNotification to teams completed: {SuccessCount} emails sent, {FailedCount} failed",
                    result.SuccessCount, result.FailedRecipients.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notifications to teams");
                result.FailedRecipients.AddRange(teamIds.Select(id => $"team:{id}(error)"));
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
                            displayFieldName = property.FieldName;
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

                // Try to convert user IDs to display names for People type fields
                var displayValue = await TryConvertUserIdsToNamesAsync(fieldValue, context.TenantId);

                result.Success = true;
                result.ResultData["stageId"] = caseSharedStageId;
                result.ResultData["fieldId"] = fieldId ?? string.Empty;
                result.ResultData["fieldName"] = storageFieldName;
                result.ResultData["fieldKey"] = storageFieldName;
                result.ResultData["newValue"] = fieldValue!;
                result.ResultData["displayValue"] = displayValue ?? fieldValue!;

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
                    assigneeIds = JsonParseHelper.ParseToStringList(idsObj);
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

                // Parse StagesProgress using helper method
                var stagesProgress = StagesProgressHelper.ParseStagesProgress(
                    onboarding.StagesProgressJson,
                    _logger,
                    $"OnboardingId={context.OnboardingId}");

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
                        // Only include normal users (userType == 3), exclude SystemAdmin (1) and TenantAdmin (2)
                        foreach (var teamId in teamIds)
                        {
                            var teamMembers = teamUsers.Where(tu => tu.TeamId == teamId).ToList();
                            _logger.LogDebug("AssignUser team: Team {TeamId} has {MemberCount} total members",
                                teamId, teamMembers.Count);

                            // Filter to only include normal users (userType == 3)
                            var normalUsers = teamMembers.Where(m => m.UserType == 3).ToList();
                            _logger.LogDebug("AssignUser team: Team {TeamId} has {NormalUserCount} normal users (userType=3)",
                                teamId, normalUsers.Count);

                            foreach (var member in normalUsers)
                            {
                                if (!string.IsNullOrEmpty(member.Id) && !memberUserIds.Contains(member.Id))
                                {
                                    memberUserIds.Add(member.Id);
                                    // Collect member display name
                                    var displayName = !string.IsNullOrEmpty(member.FirstName) || !string.IsNullOrEmpty(member.LastName)
                                        ? $"{member.FirstName} {member.LastName}".Trim()
                                        : member.UserName ?? member.Email ?? member.Id;
                                    memberNames.Add(displayName);
                                    _logger.LogDebug("AssignUser team: Added normal user Id={MemberId}, UserName={UserName}, UserType={UserType}",
                                        member.Id, member.UserName, member.UserType);
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

            return ParseJsonWithDoubleEscapeHandling<List<OnboardingStageProgress>>(json, 
                () => {
                    _logger.LogWarning("Failed to parse StagesProgressJson for onboarding {OnboardingId}", onboardingId);
                    return new List<OnboardingStageProgress>();
                });
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

            return ParseJsonWithDoubleEscapeHandling<List<string>>(json, 
                () => {
                    _logger.LogWarning("Failed to parse JSON string array: {Json}", 
                        json.Length > 100 ? json.Substring(0, 100) + "..." : json);
                    return new List<string>();
                });
        }

        /// <summary>
        /// Generic JSON parser that handles double-escaped JSON strings
        /// </summary>
        private T ParseJsonWithDoubleEscapeHandling<T>(string json, Func<T> onError) where T : class, new()
        {
            try
            {
                // First try direct parsing
                var result = JsonConvert.DeserializeObject<T>(json);
                if (result != null) return result;
            }
            catch (JsonException)
            {
                // If direct parsing fails, try to unescape first (handle double-escaped JSON)
                try
                {
                    var unescapedJson = JsonConvert.DeserializeObject<string>(json);
                    if (!string.IsNullOrEmpty(unescapedJson))
                    {
                        var result = JsonConvert.DeserializeObject<T>(unescapedJson);
                        if (result != null) return result;
                    }
                }
                catch (JsonException)
                {
                    // Format is unexpected
                }
            }

            return onError();
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
        /// Try to convert user IDs to display names for People type fields
        /// Returns the display value string if conversion is successful, otherwise returns null
        /// </summary>
        private async Task<string?> TryConvertUserIdsToNamesAsync(object? fieldValue, string tenantId)
        {
            if (fieldValue == null) return null;

            try
            {
                // Parse field value to get potential user IDs
                var stringValues = JsonParseHelper.ParseToStringList(fieldValue);
                if (!stringValues.Any()) return null;

                // Check if all values look like user IDs (numeric strings)
                var userIds = new List<long>();
                foreach (var val in stringValues)
                {
                    if (long.TryParse(val, out var userId))
                    {
                        userIds.Add(userId);
                    }
                    else
                    {
                        // Not all values are numeric, probably not user IDs
                        return null;
                    }
                }

                if (!userIds.Any()) return null;

                // Try to get user names from UserService
                var users = await _userService.GetUsersByIdsAsync(userIds, tenantId);
                if (users == null || !users.Any())
                {
                    _logger.LogDebug("TryConvertUserIdsToNames: No users found for IDs {UserIds}", string.Join(",", userIds));
                    return null;
                }

                // Build display names list
                var displayNames = new List<string>();
                foreach (var userId in userIds)
                {
                    var user = users.FirstOrDefault(u => u.Id == userId);
                    if (user != null)
                    {
                        // Use Username which already has display name with priority: FirstName + LastName > UserName
                        var displayName = user.Username ?? user.Email ?? userId.ToString();
                        displayNames.Add(displayName);
                    }
                    else
                    {
                        // User not found, keep the ID
                        displayNames.Add(userId.ToString());
                    }
                }

                _logger.LogDebug("TryConvertUserIdsToNames: Converted {IdCount} user IDs to names: {Names}",
                    userIds.Count, string.Join(",", displayNames));

                return string.Join(",", displayNames);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TryConvertUserIdsToNames: Failed to convert user IDs to names");
                return null;
            }
        }

        #endregion
    }
}

