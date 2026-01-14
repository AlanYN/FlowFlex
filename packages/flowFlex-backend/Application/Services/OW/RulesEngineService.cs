using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Enums.OW;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RulesEngine.Models;
using SqlSugar;

namespace FlowFlex.Application.Service.OW
{
    /// <summary>
    /// RulesEngine evaluation service implementation
    /// </summary>
    public class RulesEngineService : IRulesEngineService, IScopedService
    {
        private readonly ISqlSugarClient _db;
        private readonly IStageRepository _stageRepository;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IComponentDataService _componentDataService;
        private readonly UserContext _userContext;
        private readonly ILogger<RulesEngineService> _logger;

        // RulesEngine settings with custom functions
        private readonly ReSettings _reSettings;

        public RulesEngineService(
            ISqlSugarClient db,
            IStageRepository stageRepository,
            IOnboardingRepository onboardingRepository,
            IComponentDataService componentDataService,
            UserContext userContext,
            ILogger<RulesEngineService> logger)
        {
            _db = db;
            _stageRepository = stageRepository;
            _onboardingRepository = onboardingRepository;
            _componentDataService = componentDataService;
            _userContext = userContext;
            _logger = logger;

            // Configure RulesEngine with custom utility functions
            _reSettings = new ReSettings
            {
                CustomTypes = new[] { typeof(RuleUtils) }
            };
        }

        /// <summary>
        /// Evaluate condition for a completed stage
        /// </summary>
        public async Task<ConditionEvaluationResult> EvaluateConditionAsync(long onboardingId, long stageId)
        {
            try
            {
                // 1. Load condition for the stage
                var condition = await GetConditionByStageIdAsync(stageId);
                
                if (condition == null || !condition.IsActive || condition.Status != "Valid")
                {
                    _logger.LogDebug("No active condition found for stage {StageId}, proceeding to next stage", stageId);
                    return new ConditionEvaluationResult
                    {
                        IsConditionMet = false,
                        NextStageId = await GetNextStageIdAsync(stageId)
                    };
                }

                // 2. Build input data for RulesEngine
                var inputData = await BuildInputDataAsync(onboardingId, stageId);

                // 3. Parse and execute RulesEngine
                var ruleResults = await ExecuteRulesAsync(condition.RulesJson, inputData);

                // 4. Determine if condition is met based on logic type (AND/OR)
                var logic = GetLogicFromRulesJson(condition.RulesJson);
                bool isConditionMet;
                
                if (logic?.ToUpper() == "OR")
                {
                    // OR logic: at least one rule must pass
                    isConditionMet = ruleResults.Any(r => r.IsSuccess);
                }
                else
                {
                    // AND logic (default): all rules must pass
                    isConditionMet = ruleResults.All(r => r.IsSuccess);
                }

                // 5. Build result
                var result = new ConditionEvaluationResult
                {
                    IsConditionMet = isConditionMet,
                    RuleResults = ruleResults
                };

                if (isConditionMet)
                {
                    // Condition met - actions will be executed by ActionExecutor
                    _logger.LogInformation("Condition '{ConditionName}' met for onboarding {OnboardingId}, stage {StageId} (Logic={Logic}, {SuccessCount}/{TotalCount} rules passed)",
                        condition.Name, onboardingId, stageId, logic ?? "AND", ruleResults.Count(r => r.IsSuccess), ruleResults.Count);
                }
                else
                {
                    // Condition not met - go to fallback stage or next stage
                    result.NextStageId = condition.FallbackStageId ?? await GetNextStageIdAsync(stageId);
                    _logger.LogInformation("Condition '{ConditionName}' not met for onboarding {OnboardingId}, stage {StageId}, fallback to stage {NextStageId}",
                        condition.Name, onboardingId, stageId, result.NextStageId);
                }

                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Invalid RulesJson format for stage {StageId}", stageId);
                return CreateFallbackResult(stageId, "Invalid RulesJson format");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating condition for onboarding {OnboardingId}, stage {StageId}", onboardingId, stageId);
                return CreateFallbackResult(stageId, ex.Message);
            }
        }

        /// <summary>
        /// Extract logic type (AND/OR) from RulesJson
        /// </summary>
        private string GetLogicFromRulesJson(string rulesJson)
        {
            try
            {
                var jsonObj = Newtonsoft.Json.Linq.JToken.Parse(rulesJson);
                if (jsonObj is Newtonsoft.Json.Linq.JObject jObject && jObject.ContainsKey("logic"))
                {
                    return jObject["logic"]?.ToString();
                }
            }
            catch
            {
                // Ignore parsing errors
            }
            return null; // Default to AND logic
        }

        /// <summary>
        /// Evaluate condition for a completed stage by case code and stage ID
        /// </summary>
        public async Task<ConditionEvaluationResult> EvaluateConditionByCaseCodeAsync(string caseCode, long stageId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(caseCode))
                {
                    _logger.LogWarning("CaseCode is null or empty for stage condition evaluation");
                    return new ConditionEvaluationResult
                    {
                        IsConditionMet = false,
                        ErrorMessage = "CaseCode is required"
                    };
                }

                // Find onboarding by case code
                var onboarding = await _db.Queryable<Onboarding>()
                    .Where(o => o.CaseCode == caseCode && o.IsValid)
                    .Where(o => o.TenantId == _userContext.TenantId)
                    .FirstAsync();

                if (onboarding == null)
                {
                    _logger.LogWarning("Onboarding not found for CaseCode={CaseCode}", caseCode);
                    return new ConditionEvaluationResult
                    {
                        IsConditionMet = false,
                        ErrorMessage = $"Onboarding not found for CaseCode: {caseCode}"
                    };
                }

                _logger.LogDebug("Found onboarding {OnboardingId} for CaseCode={CaseCode}, evaluating stage condition for StageId={StageId}",
                    onboarding.Id, caseCode, stageId);

                // Delegate to the existing method
                return await EvaluateConditionAsync(onboarding.Id, stageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating condition for CaseCode={CaseCode}, StageId={StageId}", caseCode, stageId);
                return new ConditionEvaluationResult
                {
                    IsConditionMet = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Evaluate condition with transaction lock for concurrency control
        /// Implements Requirements 9.1-9.5: Transaction wrapping, row-level lock, single execution guarantee
        /// </summary>
        public async Task<ConditionEvaluationResult> EvaluateConditionWithLockAsync(long onboardingId, long stageId)
        {
            try
            {
                var dbResult = await _db.Ado.UseTranAsync(async () =>
                {
                    // Row-level lock on onboarding record (SELECT ... FOR UPDATE)
                    // This prevents concurrent modifications and ensures only one request succeeds
                    var onboarding = await _db.Queryable<Onboarding>()
                        .Where(o => o.Id == onboardingId && o.IsValid)
                        .Where(o => o.TenantId == _userContext.TenantId)
                        .With(SqlWith.UpdLock)
                        .FirstAsync();

                    if (onboarding == null)
                    {
                        throw new InvalidOperationException($"Onboarding {onboardingId} not found");
                    }

                    // Check if stage is already completed to prevent duplicate evaluation
                    var stageProgress = onboarding.StagesProgress?.FirstOrDefault(sp => sp.StageId == stageId);
                    if (stageProgress != null && stageProgress.IsCompleted)
                    {
                        _logger.LogWarning("Stage {StageId} already completed for onboarding {OnboardingId}, skipping condition evaluation",
                            stageId, onboardingId);
                        return new ConditionEvaluationResult
                        {
                            IsConditionMet = false,
                            ErrorMessage = "Stage already completed",
                            NextStageId = await GetNextStageIdAsync(stageId)
                        };
                    }

                    // Evaluate condition within transaction
                    var result = await EvaluateConditionAsync(onboardingId, stageId);

                    return result;
                });

                return dbResult.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating condition with lock for onboarding {OnboardingId}, stage {StageId}", onboardingId, stageId);
                return await CreateFallbackResultAsync(stageId, ex.Message);
            }
        }

        /// <summary>
        /// Evaluate condition and execute actions with full transaction support
        /// Implements Requirements 9.1-9.5: Complete evaluation flow with concurrency control
        /// </summary>
        public async Task<ConditionEvaluationResult> EvaluateAndExecuteWithTransactionAsync(
            long onboardingId, 
            long stageId,
            IConditionActionExecutor actionExecutor,
            IOperationChangeLogService changeLogService)
        {
            ConditionEvaluationResult result = null;
            StageCondition condition = null;

            try
            {
                var dbResult = await _db.Ado.UseTranAsync(async () =>
                {
                    // 1. Acquire row-level lock on onboarding record
                    var onboarding = await _db.Queryable<Onboarding>()
                        .Where(o => o.Id == onboardingId && o.IsValid)
                        .Where(o => o.TenantId == _userContext.TenantId)
                        .With(SqlWith.UpdLock)
                        .FirstAsync();

                    if (onboarding == null)
                    {
                        throw new InvalidOperationException($"Onboarding {onboardingId} not found");
                    }

                    // 2. Check if stage is already completed
                    var stageProgress = onboarding.StagesProgress?.FirstOrDefault(sp => sp.StageId == stageId);
                    if (stageProgress != null && stageProgress.IsCompleted)
                    {
                        _logger.LogWarning("Stage {StageId} already completed, concurrent request detected", stageId);
                        return new ConditionEvaluationResult
                        {
                            IsConditionMet = false,
                            ErrorMessage = "Stage already completed by another request",
                            NextStageId = await GetNextStageIdAsync(stageId)
                        };
                    }

                    // 3. Load condition configuration
                    condition = await GetConditionByStageIdAsync(stageId);
                    
                    if (condition == null || !condition.IsActive || condition.Status != "Valid")
                    {
                        _logger.LogDebug("No active condition for stage {StageId}, proceeding to next stage", stageId);
                        return new ConditionEvaluationResult
                        {
                            IsConditionMet = false,
                            NextStageId = await GetNextStageIdAsync(stageId)
                        };
                    }

                    // 4. Build input data and evaluate rules
                    var inputData = await BuildInputDataAsync(onboardingId, stageId);
                    var ruleResults = await ExecuteRulesAsync(condition.RulesJson, inputData);
                    
                    // Determine if condition is met based on logic type (AND/OR)
                    var logic = GetLogicFromRulesJson(condition.RulesJson);
                    bool isConditionMet;
                    
                    if (logic?.ToUpper() == "OR")
                    {
                        // OR logic: at least one rule must pass
                        isConditionMet = ruleResults.Any(r => r.IsSuccess);
                    }
                    else
                    {
                        // AND logic (default): all rules must pass
                        isConditionMet = ruleResults.All(r => r.IsSuccess);
                    }

                    result = new ConditionEvaluationResult
                    {
                        IsConditionMet = isConditionMet,
                        RuleResults = ruleResults
                    };

                    // 5. Execute actions if condition is met
                    if (isConditionMet && actionExecutor != null)
                    {
                        var context = new ActionExecutionContext
                        {
                            OnboardingId = onboardingId,
                            StageId = stageId,
                            ConditionId = condition.Id,
                            TenantId = _userContext.TenantId,
                            UserId = long.TryParse(_userContext.UserId, out var uid) ? uid : 0
                        };

                        var actionResult = await actionExecutor.ExecuteActionsAsync(condition.ActionsJson, context);
                        result.ActionResults = actionResult.Details;

                        _logger.LogInformation("Condition '{ConditionName}' met, executed {ActionCount} actions",
                            condition.Name, actionResult.Details.Count);
                    }
                    else if (!isConditionMet)
                    {
                        // Condition not met - determine fallback stage
                        result.NextStageId = condition.FallbackStageId ?? await GetNextStageIdAsync(stageId);
                        _logger.LogInformation("Condition '{ConditionName}' not met, fallback to stage {NextStageId}",
                            condition.Name, result.NextStageId);
                    }

                    return result;
                });

                result = dbResult.Data;

                // 6. Log changes AFTER transaction commits successfully (Requirements 9.5)
                if (changeLogService != null && condition != null)
                {
                    await LogConditionEvaluationAsync(changeLogService, onboardingId, stageId, condition, result);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EvaluateAndExecuteWithTransactionAsync for onboarding {OnboardingId}, stage {StageId}",
                    onboardingId, stageId);
                return await CreateFallbackResultAsync(stageId, ex.Message);
            }
        }

        /// <summary>
        /// Log condition evaluation result to OperationChangeLog
        /// Called after transaction commits to ensure data consistency
        /// </summary>
        private async Task LogConditionEvaluationAsync(
            IOperationChangeLogService changeLogService,
            long onboardingId,
            long stageId,
            StageCondition condition,
            ConditionEvaluationResult result)
        {
            try
            {
                // Get successful and failed rule names
                var successfulRules = result.RuleResults?.Where(r => r.IsSuccess).Select(r => r.RuleName).ToList() ?? new List<string>();
                var failedRules = result.RuleResults?.Where(r => !r.IsSuccess).Select(r => r.RuleName).ToList() ?? new List<string>();
                
                // Get successful and failed action details
                var successfulActions = result.ActionResults?.Where(a => a.Success).ToList() ?? new List<ActionExecutionDetail>();
                var failedActions = result.ActionResults?.Where(a => !a.Success).ToList() ?? new List<ActionExecutionDetail>();

                // Build extended data with detailed action results
                var extendedData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    conditionId = condition.Id,
                    conditionName = condition.Name,
                    result = result.IsConditionMet,
                    ruleEvaluations = result.RuleResults?.Select(r => new
                    {
                        ruleName = r.RuleName,
                        isSuccess = r.IsSuccess,
                        expression = r.Expression,
                        errorMessage = r.ErrorMessage
                    }),
                    nextStageId = result.NextStageId,
                    actionCount = result.ActionResults?.Count ?? 0,
                    // Include detailed action execution results
                    actionExecutions = result.ActionResults?.Select(a => new
                    {
                        actionType = a.ActionType,
                        order = a.Order,
                        success = a.Success,
                        errorMessage = a.ErrorMessage,
                        resultSummary = BuildActionResultSummary(a)
                    })
                });

                // Build descriptive title with specific rule and action names
                // Format: "Condition Met: t1 | Rules: Rule_3 ✓ | Actions: GoToStage→Stage4 ✓, SendNotification→admin@test.com ✓"
                var statusText = result.IsConditionMet ? "Met" : "Not Met";
                
                // Build rules summary (show successful rules for Met, failed rules for Not Met)
                var rulesSummary = "";
                if (successfulRules.Any())
                {
                    rulesSummary = string.Join(", ", successfulRules.Take(3).Select(r => $"{r} ✓"));
                    if (successfulRules.Count > 3)
                    {
                        rulesSummary += $" +{successfulRules.Count - 3}";
                    }
                }
                
                // Build actions summary with detailed result indicators
                var actionsSummary = "";
                if (result.ActionResults != null && result.ActionResults.Any())
                {
                    var actionParts = result.ActionResults
                        .OrderBy(a => a.Order)
                        .Take(4)
                        .Select(a => BuildActionTitlePart(a));
                    actionsSummary = string.Join(", ", actionParts);
                    if (result.ActionResults.Count > 4)
                    {
                        actionsSummary += $" +{result.ActionResults.Count - 4}";
                    }
                }

                // Build operationTitle
                var titleParts = new List<string> { $"Condition {statusText}: {condition.Name}" };
                if (!string.IsNullOrEmpty(rulesSummary))
                {
                    titleParts.Add($"Rules: {rulesSummary}");
                }
                if (!string.IsNullOrEmpty(actionsSummary))
                {
                    titleParts.Add($"Actions: {actionsSummary}");
                }
                var operationTitle = string.Join(" | ", titleParts);
                
                // Build detailed description
                var descParts = new List<string>();
                descParts.Add($"Condition '{condition.Name}' evaluated: {statusText}");
                
                if (successfulRules.Any())
                {
                    descParts.Add($"Passed rules: {string.Join(", ", successfulRules)}");
                }
                if (failedRules.Any())
                {
                    descParts.Add($"Failed rules: {string.Join(", ", failedRules)}");
                }
                if (successfulActions.Any())
                {
                    var actionDetails = successfulActions.Select(a => BuildActionDescriptionPart(a));
                    descParts.Add($"Executed actions: {string.Join("; ", actionDetails)}");
                }
                if (failedActions.Any())
                {
                    var failedDetails = failedActions.Select(a => $"{a.ActionType}({a.ErrorMessage ?? "unknown error"})");
                    descParts.Add($"Failed actions: {string.Join("; ", failedDetails)}");
                }
                
                var operationDescription = string.Join(". ", descParts);

                await changeLogService.LogOperationAsync(
                    operationType: Domain.Shared.Enums.OW.OperationTypeEnum.StageConditionEvaluate,
                    businessModule: Domain.Shared.Enums.OW.BusinessModuleEnum.StageCondition,
                    businessId: condition.Id,
                    onboardingId: onboardingId,
                    stageId: stageId,
                    operationTitle: operationTitle,
                    operationDescription: operationDescription,
                    extendedData: extendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log condition evaluation for condition {ConditionId}", condition.Id);
                // Don't throw - logging failure should not affect main flow
            }
        }

        /// <summary>
        /// Build action title part with result details for operationTitle
        /// Format: "GoToStage→Stage4 ✓" or "SendNotification→admin@test.com ✓"
        /// </summary>
        private string BuildActionTitlePart(ActionExecutionDetail action)
        {
            var statusIcon = action.Success ? "✓" : "✗";
            var resultDetail = GetActionResultDetail(action);
            
            if (!string.IsNullOrEmpty(resultDetail))
            {
                return $"{action.ActionType}→{resultDetail} {statusIcon}";
            }
            return $"{action.ActionType} {statusIcon}";
        }

        /// <summary>
        /// Build action description part with full details for operationDescription
        /// </summary>
        private string BuildActionDescriptionPart(ActionExecutionDetail action)
        {
            var resultDetail = GetActionResultDetail(action);
            if (!string.IsNullOrEmpty(resultDetail))
            {
                return $"{action.ActionType}({resultDetail})";
            }
            return action.ActionType;
        }

        /// <summary>
        /// Get action result detail based on action type
        /// </summary>
        private string GetActionResultDetail(ActionExecutionDetail action)
        {
            if (action.ResultData == null || !action.ResultData.Any())
                return string.Empty;

            return action.ActionType.ToLower() switch
            {
                "gotostage" => action.ResultData.TryGetValue("targetStageName", out var stageName) 
                    ? stageName?.ToString() ?? "" : "",
                "skipstage" => action.ResultData.TryGetValue("skippedCount", out var count) 
                    ? $"skip{count}" : "",
                "endworkflow" => action.ResultData.TryGetValue("endStatus", out var status) 
                    ? status?.ToString() ?? "" : "",
                "sendnotification" => action.ResultData.TryGetValue("recipientEmail", out var email) 
                    ? TruncateEmail(email?.ToString()) : "",
                "updatefield" => action.ResultData.TryGetValue("fieldKey", out var fieldKey) 
                    ? fieldKey?.ToString() ?? "" : "",
                "triggeraction" => action.ResultData.TryGetValue("actionName", out var actionName) 
                    ? actionName?.ToString() ?? "" : "",
                "assignuser" => BuildAssignUserDetail(action.ResultData),
                _ => ""
            };
        }

        /// <summary>
        /// Build AssignUser action detail
        /// </summary>
        private string BuildAssignUserDetail(Dictionary<string, object> resultData)
        {
            var assigneeType = resultData.TryGetValue("assigneeType", out var type) ? type?.ToString() : "";
            var assigneeCount = resultData.TryGetValue("assigneeCount", out var count) ? count?.ToString() : "0";
            return $"{assigneeType}×{assigneeCount}";
        }

        /// <summary>
        /// Truncate email for display (show first part only if too long)
        /// </summary>
        private string TruncateEmail(string? email)
        {
            if (string.IsNullOrEmpty(email)) return "";
            if (email.Length <= 20) return email;
            
            var atIndex = email.IndexOf('@');
            if (atIndex > 0 && atIndex <= 15)
            {
                return email;
            }
            return email.Substring(0, 17) + "...";
        }

        /// <summary>
        /// Build action result summary for extendedData
        /// </summary>
        private string BuildActionResultSummary(ActionExecutionDetail action)
        {
            if (!action.Success)
            {
                return $"Failed: {action.ErrorMessage ?? "unknown error"}";
            }

            var detail = GetActionResultDetail(action);
            return string.IsNullOrEmpty(detail) ? "Success" : $"Success: {detail}";
        }

        /// <summary>
        /// Build input data object for RulesEngine evaluation
        /// </summary>
        public async Task<object> BuildInputDataAsync(long onboardingId, long stageId)
        {
            try
            {
                // Get component data
                var checklistData = await _componentDataService.GetChecklistDataAsync(onboardingId, stageId);
                var questionnaireData = await _componentDataService.GetQuestionnaireDataAsync(onboardingId, stageId);
                var attachmentData = await _componentDataService.GetAttachmentDataAsync(onboardingId, stageId);
                var fieldsData = await _componentDataService.GetFieldsDataAsync(onboardingId);

                // Build dynamic input object
                dynamic input = new ExpandoObject();
                var inputDict = (IDictionary<string, object>)input;

                // Build nested tasks dictionary: tasks[checklistId][taskId] = { isCompleted, name, completionNotes }
                var tasksDict = BuildNestedTasksDictionary(checklistData, stageId);

                // Add checklist data with nested tasks dictionary
                inputDict["checklist"] = new
                {
                    status = checklistData.Status,
                    completedCount = checklistData.CompletedCount,
                    totalCount = checklistData.TotalCount,
                    completionPercentage = checklistData.TotalCount > 0 
                        ? (double)checklistData.CompletedCount / checklistData.TotalCount * 100 
                        : 0,
                    tasks = tasksDict
                };

                // Build nested answers dictionary: answers[questionnaireId][questionId] = value
                var answersDict = BuildNestedAnswersDictionary(questionnaireData, stageId);

                // Add questionnaire data with nested answers dictionary
                inputDict["questionnaire"] = new
                {
                    status = questionnaireData.Status,
                    totalScore = questionnaireData.TotalScore,
                    answers = answersDict
                };

                // Add attachment data
                inputDict["attachments"] = new
                {
                    fileCount = attachmentData.FileCount,
                    hasAttachment = attachmentData.FileCount > 0,
                    totalSize = attachmentData.TotalSize,
                    fileNames = attachmentData.FileNames
                };

                // Add fields data (dynamic data from onboarding) - use SafeFieldsDictionary for safe access
                inputDict["fields"] = SafeFieldsDictionary.FromDictionary(fieldsData);

                return input;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building input data for onboarding {OnboardingId}, stage {StageId}", onboardingId, stageId);
                // Return empty input to allow evaluation to continue
                return new ExpandoObject();
            }
        }

        /// <summary>
        /// Build nested tasks dictionary for RulesEngine access
        /// Format: tasks[checklistId][taskId] = TaskData { isCompleted, name, completionNotes }
        /// Uses SafeTasksDictionary to handle missing keys gracefully
        /// </summary>
        private SafeTasksDictionary BuildNestedTasksDictionary(ChecklistData checklistData, long stageId)
        {
            var result = new SafeTasksDictionary();

            if (checklistData?.Tasks == null || !checklistData.Tasks.Any())
            {
                _logger.LogDebug("No tasks found in checklist data for stage {StageId}", stageId);
                return result;
            }

            _logger.LogDebug("Building nested tasks dictionary with {TaskCount} tasks for stage {StageId}", 
                checklistData.Tasks.Count, stageId);

            // Group tasks by checklistId
            // The frontend fieldPath format is: tasks["checklistId"]["taskId"].isCompleted
            foreach (var task in checklistData.Tasks)
            {
                var checklistIdStr = task.ChecklistId.ToString();
                var taskIdStr = task.TaskId.ToString();
                
                _logger.LogDebug("Adding task: ChecklistId={ChecklistId}, TaskId={TaskId}, IsCompleted={IsCompleted}", 
                    checklistIdStr, taskIdStr, task.IsCompleted);

                // Create task data object with strongly-typed properties
                var taskData = new TaskData
                {
                    isCompleted = task.IsCompleted,
                    name = task.Name ?? string.Empty,
                    completionNotes = task.CompletionNotes ?? string.Empty
                };

                // Ensure checklist dictionary exists
                if (!result.ContainsKey(checklistIdStr))
                {
                    result[checklistIdStr] = new SafeTaskInnerDictionary();
                }

                // Add task under its checklistId
                result[checklistIdStr][taskIdStr] = taskData;
            }

            _logger.LogDebug("Built nested tasks dictionary with {ChecklistCount} checklists: [{ChecklistIds}]", 
                result.Count, string.Join(", ", result.Keys));

            return result;
        }

        /// <summary>
        /// Build nested answers dictionary for RulesEngine access
        /// Format: answers[questionnaireId][questionId] = value
        /// Uses SafeNestedDictionary to handle missing keys gracefully
        /// </summary>
        private SafeNestedDictionary BuildNestedAnswersDictionary(QuestionnaireData questionnaireData, long stageId)
        {
            var result = new SafeNestedDictionary();

            if (questionnaireData?.Answers == null || !questionnaireData.Answers.Any())
            {
                _logger.LogDebug("No questionnaire answers found for stage {StageId}", stageId);
                return result;
            }

            _logger.LogDebug("Building nested answers dictionary with {AnswerCount} questionnaire answers for stage {StageId}", 
                questionnaireData.Answers.Count, stageId);

            // The frontend fieldPath format is: answers["questionnaireId"]["questionId"]
            // QuestionnaireData.Answers should already be: { "questionnaireId": { "questionId": value, ... }, ... }
            foreach (var kvp in questionnaireData.Answers)
            {
                var questionnaireIdStr = kvp.Key;
                
                _logger.LogDebug("Processing questionnaire {QuestionnaireId}, value type: {ValueType}", 
                    questionnaireIdStr, kvp.Value?.GetType().Name ?? "null");

                // Check if the value is already a dictionary (nested structure)
                if (kvp.Value is Dictionary<string, object> nestedDict)
                {
                    var safeDict = new SafeInnerDictionary();
                    foreach (var item in nestedDict)
                    {
                        safeDict[item.Key] = item.Value;
                    }
                    result[questionnaireIdStr] = safeDict;
                    _logger.LogDebug("Questionnaire {QuestionnaireId} has {QuestionCount} questions: [{QuestionIds}]", 
                        questionnaireIdStr, nestedDict.Count, string.Join(", ", nestedDict.Keys));
                }
                else if (kvp.Value is Newtonsoft.Json.Linq.JObject jObj)
                {
                    // Convert JObject to SafeInnerDictionary
                    var dict = new SafeInnerDictionary();
                    foreach (var prop in jObj.Properties())
                    {
                        dict[prop.Name] = prop.Value.Type == Newtonsoft.Json.Linq.JTokenType.Object 
                            ? prop.Value.ToString() 
                            : prop.Value.ToObject<object>();
                    }
                    result[questionnaireIdStr] = dict;
                    _logger.LogDebug("Questionnaire {QuestionnaireId} (JObject) has {QuestionCount} questions: [{QuestionIds}]", 
                        questionnaireIdStr, dict.Count, string.Join(", ", dict.Keys));
                }
                else if (kvp.Value is IDictionary<string, object> iDict)
                {
                    var safeDict = new SafeInnerDictionary();
                    foreach (var item in iDict)
                    {
                        safeDict[item.Key] = item.Value;
                    }
                    result[questionnaireIdStr] = safeDict;
                    _logger.LogDebug("Questionnaire {QuestionnaireId} (IDictionary) has {QuestionCount} questions", 
                        questionnaireIdStr, iDict.Count);
                }
                else
                {
                    // Single value - wrap it
                    if (!result.ContainsKey(questionnaireIdStr))
                    {
                        result[questionnaireIdStr] = new SafeInnerDictionary();
                    }
                    result[questionnaireIdStr][questionnaireIdStr] = kvp.Value;
                    _logger.LogDebug("Questionnaire {QuestionnaireId} has single value: {Value}", 
                        questionnaireIdStr, kvp.Value);
                }
            }

            _logger.LogDebug("Built nested answers dictionary with {QuestionnaireCount} questionnaires: [{QuestionnaireIds}]", 
                result.Count, string.Join(", ", result.Keys));

            return result;
        }


        #region Private Methods

        /// <summary>
        /// Get condition by stage ID
        /// </summary>
        private async Task<StageCondition?> GetConditionByStageIdAsync(long stageId)
        {
            return await _db.Queryable<StageCondition>()
                .Where(c => c.StageId == stageId && c.IsValid)
                .Where(c => c.TenantId == _userContext.TenantId)
                .FirstAsync();
        }

        /// <summary>
        /// Execute rules using Microsoft RulesEngine
        /// Supports both frontend custom format and RulesEngine format
        /// </summary>
        private async Task<List<RuleEvaluationDetail>> ExecuteRulesAsync(string rulesJson, object inputData)
        {
            var ruleResults = new List<RuleEvaluationDetail>();

            try
            {
                // Convert frontend format to RulesEngine format if needed
                var convertedRulesJson = ConvertToRulesEngineFormatIfNeeded(rulesJson);
                
                // Parse RulesEngine workflow JSON
                var workflows = JsonConvert.DeserializeObject<RulesEngine.Models.Workflow[]>(convertedRulesJson);
                
                if (workflows == null || workflows.Length == 0)
                {
                    _logger.LogWarning("No workflows found in RulesJson");
                    return ruleResults;
                }

                // Create RulesEngine instance with custom settings
                var rulesEngine = new RulesEngine.RulesEngine(workflows, _reSettings);

                // Create RuleParameter for input data
                var ruleParameter = new RuleParameter("input", inputData);

                // Execute all rules in the first workflow
                var workflowName = workflows[0].WorkflowName ?? "StageCondition";
                var results = await rulesEngine.ExecuteAllRulesAsync(workflowName, ruleParameter);

                // Convert results to RuleEvaluationDetail
                foreach (var result in results)
                {
                    ruleResults.Add(new RuleEvaluationDetail
                    {
                        RuleName = result.Rule.RuleName,
                        IsSuccess = result.IsSuccess,
                        Expression = result.Rule.Expression ?? string.Empty,
                        ErrorMessage = result.ExceptionMessage
                    });

                    if (!result.IsSuccess && !string.IsNullOrEmpty(result.ExceptionMessage))
                    {
                        _logger.LogDebug("Rule '{RuleName}' failed: {ErrorMessage}", 
                            result.Rule.RuleName, result.ExceptionMessage);
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse RulesJson");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing rules");
                ruleResults.Add(new RuleEvaluationDetail
                {
                    RuleName = "RulesEngine",
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                });
            }

            return ruleResults;
        }

        /// <summary>
        /// Get next stage ID based on order with explicit tenant ID
        /// </summary>
        private async Task<long?> GetNextStageIdAsync(long currentStageId, string tenantId)
        {
            try
            {
                // Get current stage
                var currentStage = await _stageRepository.GetByIdAsync(currentStageId);
                if (currentStage == null)
                {
                    return null;
                }

                // Get next stage by order
                var nextStage = await _db.Queryable<Stage>()
                    .Where(s => s.WorkflowId == currentStage.WorkflowId && s.IsValid && s.IsActive)
                    .Where(s => s.TenantId == tenantId)
                    .Where(s => s.Order > currentStage.Order)
                    .OrderBy(s => s.Order)
                    .FirstAsync();

                return nextStage?.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next stage for stage {StageId}", currentStageId);
                return null;
            }
        }

        /// <summary>
        /// Get next stage ID based on order (gets TenantId from current stage)
        /// </summary>
        private async Task<long?> GetNextStageIdAsync(long currentStageId)
        {
            try
            {
                // Get current stage to obtain TenantId
                var currentStage = await _stageRepository.GetByIdAsync(currentStageId);
                if (currentStage == null)
                {
                    return null;
                }

                // Use stage's TenantId
                return await GetNextStageIdAsync(currentStageId, currentStage.TenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next stage for stage {StageId}", currentStageId);
                return null;
            }
        }

        /// <summary>
        /// Create fallback result when evaluation fails
        /// </summary>
        private ConditionEvaluationResult CreateFallbackResult(long stageId, string errorMessage)
        {
            return new ConditionEvaluationResult
            {
                IsConditionMet = false,
                ErrorMessage = $"Condition evaluation failed: {errorMessage}",
                RuleResults = new List<RuleEvaluationDetail>
                {
                    new RuleEvaluationDetail
                    {
                        RuleName = "EvaluationError",
                        IsSuccess = false,
                        ErrorMessage = errorMessage
                    }
                }
            };
        }

        /// <summary>
        /// Create fallback result with next stage ID (async version)
        /// </summary>
        private async Task<ConditionEvaluationResult> CreateFallbackResultAsync(long stageId, string errorMessage)
        {
            var result = CreateFallbackResult(stageId, errorMessage);
            result.NextStageId = await GetNextStageIdAsync(stageId);
            return result;
        }

        /// <summary>
        /// Convert frontend custom rule format to RulesEngine format if needed
        /// </summary>
        private string ConvertToRulesEngineFormatIfNeeded(string rulesJson)
        {
            try
            {
                var jsonObj = Newtonsoft.Json.Linq.JToken.Parse(rulesJson);
                
                // Check if it's frontend format (has "logic" property)
                if (jsonObj is Newtonsoft.Json.Linq.JObject jObject && jObject.ContainsKey("logic"))
                {
                    _logger.LogDebug("Converting frontend rule format to RulesEngine format");
                    return ConvertFrontendRulesToRulesEngineFormat(rulesJson);
                }
                
                // Already in RulesEngine format
                return rulesJson;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to detect rules format, assuming RulesEngine format");
                return rulesJson;
            }
        }

        /// <summary>
        /// Convert frontend custom rule format to RulesEngine format
        /// Frontend format: {"logic":"AND","rules":[{"fieldPath":"...","operator":"==","value":"..."}]}
        /// RulesEngine format: [{"WorkflowName":"StageCondition","Rules":[{"RuleName":"Rule1","Expression":"..."}]}]
        /// </summary>
        private string ConvertFrontendRulesToRulesEngineFormat(string frontendRulesJson)
        {
            try
            {
                var frontendRules = JsonConvert.DeserializeObject<FrontendRuleConfig>(frontendRulesJson);
                if (frontendRules == null || frontendRules.Rules == null || !frontendRules.Rules.Any())
                {
                    return "[]";
                }

                var expressions = new List<string>();
                var rulesEngineRules = new List<RulesEngine.Models.Rule>();
                int ruleIndex = 1;

                foreach (var rule in frontendRules.Rules)
                {
                    var expression = BuildExpressionFromFrontendRule(rule);
                    if (!string.IsNullOrEmpty(expression))
                    {
                        // Use descriptive rule name based on field path
                        var ruleName = GetDescriptiveRuleName(rule, ruleIndex);
                        rulesEngineRules.Add(new RulesEngine.Models.Rule
                        {
                            RuleName = ruleName,
                            Expression = expression,
                            SuccessEvent = "true"
                        });
                        expressions.Add(expression);
                        ruleIndex++;
                    }
                }

                // Keep all rules separate for detailed results
                // The evaluation logic will handle OR/AND based on frontendRules.Logic

                var workflow = new RulesEngine.Models.Workflow
                {
                    WorkflowName = "StageCondition",
                    Rules = rulesEngineRules
                };

                return JsonConvert.SerializeObject(new[] { workflow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert frontend rules to RulesEngine format");
                throw;
            }
        }

        /// <summary>
        /// Build RulesEngine expression from frontend rule
        /// </summary>
        private string BuildExpressionFromFrontendRule(FrontendRule rule)
        {
            if (string.IsNullOrEmpty(rule.FieldPath))
            {
                return null;
            }

            // Convert input.fields.xxx to input.fields["xxx"] for numeric field IDs
            var fieldPath = ConvertFieldPathToDictionaryAccess(rule.FieldPath);

            // Get the value representation - escape special characters for expression parser
            string valueStr;
            if (rule.Value == null)
            {
                valueStr = "null";
            }
            else if (rule.Value is string strValue)
            {
                // Escape backslashes and quotes, then wrap in quotes
                var escaped = strValue
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"");
                valueStr = $"\"{escaped}\"";
            }
            else if (rule.Value is bool boolValue)
            {
                valueStr = boolValue.ToString().ToLower();
            }
            else
            {
                valueStr = rule.Value.ToString();
            }

            // Map frontend operator to C# expression operator
            var operatorLower = rule.Operator?.ToLower();
            
            // Handle special checklist operators
            if (operatorLower == "completetask")
            {
                // CompleteTask: check if task isCompleted == true
                // fieldPath already points to .isCompleted
                return $"{fieldPath} == true";
            }
            else if (operatorLower == "completestage")
            {
                // CompleteStage: check if the specified task/field is completed
                // If fieldPath points to a specific task's isCompleted, use it
                // Otherwise, check the overall checklist status
                if (!string.IsNullOrEmpty(fieldPath) && fieldPath.Contains(".isCompleted"))
                {
                    // fieldPath points to a specific task's isCompleted property
                    return $"{fieldPath} == true";
                }
                else
                {
                    // Fallback: check if all tasks in stage are completed
                    return "input.checklist.status == \"Completed\"";
                }
            }

            var op = operatorLower switch
            {
                "==" or "equals" or "eq" => "==",
                "!=" or "notequals" or "ne" => "!=",
                ">" or "gt" => ">",
                "<" or "lt" => "<",
                ">=" or "gte" => ">=",
                "<=" or "lte" => "<=",
                "contains" => "Contains",
                "notcontains" => "NotContains",
                "startswith" => "StartsWith",
                "endswith" => "EndsWith",
                "isnull" => "== null",
                "isnotnull" => "!= null",
                "isempty" => "IsEmpty",
                "isnotempty" => "!IsEmpty",
                "inlist" => "InList",
                "notinlist" => "!InList",
                _ => "=="
            };

            // Build expression based on operator type
            // Handle null values safely - if field is null, comparison should return false (except for null checks)
            var fieldPathStr = $"(np({fieldPath}) == null ? \"\" : np({fieldPath}).ToString())";
            
            if (op == "Contains")
            {
                return $"(np({fieldPath}) != null && np({fieldPath}).ToString().Contains({valueStr}))";
            }
            else if (op == "NotContains")
            {
                return $"(np({fieldPath}) == null || !np({fieldPath}).ToString().Contains({valueStr}))";
            }
            else if (op == "StartsWith")
            {
                return $"(np({fieldPath}) != null && np({fieldPath}).ToString().StartsWith({valueStr}))";
            }
            else if (op == "EndsWith")
            {
                return $"(np({fieldPath}) != null && np({fieldPath}).ToString().EndsWith({valueStr}))";
            }
            else if (op == "== null")
            {
                return $"np({fieldPath}) == null";
            }
            else if (op == "!= null")
            {
                return $"np({fieldPath}) != null";
            }
            else if (op == "IsEmpty")
            {
                return $"(np({fieldPath}) == null || string.IsNullOrWhiteSpace(np({fieldPath}).ToString()))";
            }
            else if (op == "!IsEmpty")
            {
                return $"(np({fieldPath}) != null && !string.IsNullOrWhiteSpace(np({fieldPath}).ToString()))";
            }
            else if (op == "InList" || op == "!InList")
            {
                // InList: check if value is in a comma-separated list
                var prefix = op.StartsWith("!") ? "!" : "";
                return $"(np({fieldPath}) != null && {prefix}{valueStr}.Contains(np({fieldPath}).ToString()))";
            }
            else if (op == ">" || op == "<" || op == ">=" || op == "<=")
            {
                // For numeric comparison - use Convert.ToDouble for simpler expression
                // If field is null or not a number, the comparison will fail gracefully
                return $"(np({fieldPath}) != null && double.Parse(np({fieldPath}).ToString()) {op} double.Parse({valueStr}))";
            }
            else if (op == "==")
            {
                // For equality - null field equals empty string or null value
                return $"(np({fieldPath}) == null ? {valueStr} == \"\" : np({fieldPath}).ToString() == {valueStr})";
            }
            else if (op == "!=")
            {
                // For inequality
                return $"(np({fieldPath}) == null ? {valueStr} != \"\" : np({fieldPath}).ToString() != {valueStr})";
            }
            else
            {
                // Default to equality comparison
                return $"(np({fieldPath}) == null ? {valueStr} == \"\" : np({fieldPath}).ToString() == {valueStr})";
            }
        }

        /// <summary>
        /// Generate descriptive rule name based on field path and operator
        /// Format: Task_1_completed, Question_2_equals, Field_3_contains
        /// </summary>
        private string GetDescriptiveRuleName(FrontendRule rule, int index)
        {
            var componentType = rule.ComponentType?.ToLower() ?? "unknown";
            var op = GetOperatorDisplayName(rule.Operator);
            
            // Use 1-based index for human readability
            var ruleNumber = index + 1;
            
            // Build descriptive name without meaningless IDs
            var name = componentType switch
            {
                "field" or "fields" => $"Field_{ruleNumber}_{op}",
                "questionnaire" => $"Question_{ruleNumber}_{op}",
                "checklist" => $"Task_{ruleNumber}_{op}",
                "attachment" => $"Attachment_{ruleNumber}_{op}",
                _ => $"Rule_{ruleNumber}_{op}"
            };

            return name;
        }

        /// <summary>
        /// Get human-readable operator display name
        /// </summary>
        private string GetOperatorDisplayName(string op)
        {
            if (string.IsNullOrEmpty(op)) return "check";
            
            return op.ToLower() switch
            {
                "==" or "eq" or "equals" => "equals",
                "!=" or "ne" or "notequals" => "notEquals",
                ">" or "gt" => "greaterThan",
                ">=" or "gte" => "greaterOrEqual",
                "<" or "lt" => "lessThan",
                "<=" or "lte" => "lessOrEqual",
                "contains" => "contains",
                "notcontains" => "notContains",
                "startswith" => "startsWith",
                "endswith" => "endsWith",
                "isempty" => "isEmpty",
                "isnotempty" => "isNotEmpty",
                "isnull" => "isNull",
                "isnotnull" => "isNotNull",
                "in" or "inlist" => "inList",
                "notin" or "notinlist" => "notInList",
                "completestage" => "completed",
                _ => op.Length > 10 ? op.Substring(0, 10) : op
            };
        }

        /// <summary>
        /// Extract field/question/task ID from field path
        /// </summary>
        private string ExtractFieldIdFromPath(string fieldPath)
        {
            if (string.IsNullOrEmpty(fieldPath))
            {
                return "unknown";
            }

            // Try to extract numeric ID from various path formats
            // e.g., input.fields["2006236814662307840"] -> 2006236814662307840
            // e.g., input.questionnaire.answers["123"]["456"] -> 456
            // e.g., input.checklist.tasks["123"]["456"].isCompleted -> 456
            
            var matches = System.Text.RegularExpressions.Regex.Matches(fieldPath, @"\[""(\d+)""\]");
            if (matches.Count > 0)
            {
                // Return the last numeric ID (most specific)
                var lastMatch = matches[matches.Count - 1];
                var id = lastMatch.Groups[1].Value;
                // Truncate long IDs for readability
                return id.Length > 8 ? id.Substring(id.Length - 8) : id;
            }

            // Try to extract from dot notation: input.fields.123456
            var dotMatch = System.Text.RegularExpressions.Regex.Match(fieldPath, @"\.(\d+)(?:\.|$)");
            if (dotMatch.Success)
            {
                var id = dotMatch.Groups[1].Value;
                return id.Length > 8 ? id.Substring(id.Length - 8) : id;
            }

            return "unknown";
        }

        /// <summary>
        /// Convert field path with numeric identifiers to dictionary access syntax
        /// e.g., input.fields.2006236814662307840 -> input.fields["2006236814662307840"]
        /// </summary>
        private string ConvertFieldPathToDictionaryAccess(string fieldPath)
        {
            if (string.IsNullOrEmpty(fieldPath))
            {
                return fieldPath;
            }

            // Pattern: input.fields.{numericId} -> input.fields["{numericId}"]
            var pattern = @"input\.fields\.(\d+)";
            var result = System.Text.RegularExpressions.Regex.Replace(
                fieldPath, 
                pattern, 
                "input.fields[\"$1\"]");

            return result;
        }

        /// <summary>
        /// Frontend rule configuration model
        /// </summary>
        private class FrontendRuleConfig
        {
            [JsonProperty("logic")]
            public string Logic { get; set; }

            [JsonProperty("rules")]
            public List<FrontendRule> Rules { get; set; }
        }

        /// <summary>
        /// Frontend individual rule model
        /// </summary>
        private class FrontendRule
        {
            [JsonProperty("sourceStageId")]
            public string SourceStageId { get; set; }

            [JsonProperty("componentType")]
            public string ComponentType { get; set; }

            [JsonProperty("componentId")]
            public string ComponentId { get; set; }

            [JsonProperty("fieldPath")]
            public string FieldPath { get; set; }

            [JsonProperty("operator")]
            public string Operator { get; set; }

            [JsonProperty("value")]
            public object Value { get; set; }
        }

        #endregion
    }
}
