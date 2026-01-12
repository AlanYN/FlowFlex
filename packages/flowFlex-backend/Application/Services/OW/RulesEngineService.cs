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

                // 4. Determine if condition is met (all rules must pass)
                bool isConditionMet = ruleResults.All(r => r.IsSuccess);

                // 5. Build result
                var result = new ConditionEvaluationResult
                {
                    IsConditionMet = isConditionMet,
                    RuleResults = ruleResults
                };

                if (isConditionMet)
                {
                    // Condition met - actions will be executed by ActionExecutor
                    _logger.LogInformation("Condition '{ConditionName}' met for onboarding {OnboardingId}, stage {StageId}",
                        condition.Name, onboardingId, stageId);
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
                    bool isConditionMet = ruleResults.All(r => r.IsSuccess);

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
                    actionCount = result.ActionResults?.Count ?? 0
                });

                await changeLogService.LogOperationAsync(
                    operationType: Domain.Shared.Enums.OW.OperationTypeEnum.StageConditionEvaluate,
                    businessModule: Domain.Shared.Enums.OW.BusinessModuleEnum.StageCondition,
                    businessId: condition.Id,
                    onboardingId: onboardingId,
                    stageId: stageId,
                    operationTitle: $"Condition Evaluated: {condition.Name}",
                    operationDescription: $"Condition '{condition.Name}' evaluated: {(result.IsConditionMet ? "Met" : "Not Met")}",
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

                // Add fields data (dynamic data from onboarding)
                inputDict["fields"] = fieldsData;

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
        /// Format: tasks[checklistId][taskId] = { isCompleted, name, completionNotes }
        /// </summary>
        private Dictionary<string, Dictionary<string, object>> BuildNestedTasksDictionary(ChecklistData checklistData, long stageId)
        {
            var result = new Dictionary<string, Dictionary<string, object>>();

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

                // Create task data object as dictionary for property access
                var taskData = new Dictionary<string, object>
                {
                    ["isCompleted"] = task.IsCompleted,
                    ["name"] = task.Name ?? string.Empty,
                    ["completionNotes"] = task.CompletionNotes ?? string.Empty
                };

                // Ensure checklist dictionary exists
                if (!result.ContainsKey(checklistIdStr))
                {
                    result[checklistIdStr] = new Dictionary<string, object>();
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
        /// </summary>
        private Dictionary<string, Dictionary<string, object>> BuildNestedAnswersDictionary(QuestionnaireData questionnaireData, long stageId)
        {
            var result = new Dictionary<string, Dictionary<string, object>>();

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
                    result[questionnaireIdStr] = nestedDict;
                    _logger.LogDebug("Questionnaire {QuestionnaireId} has {QuestionCount} questions: [{QuestionIds}]", 
                        questionnaireIdStr, nestedDict.Count, string.Join(", ", nestedDict.Keys));
                }
                else if (kvp.Value is Newtonsoft.Json.Linq.JObject jObj)
                {
                    // Convert JObject to Dictionary<string, object>
                    var dict = new Dictionary<string, object>();
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
                    result[questionnaireIdStr] = new Dictionary<string, object>(iDict);
                    _logger.LogDebug("Questionnaire {QuestionnaireId} (IDictionary) has {QuestionCount} questions", 
                        questionnaireIdStr, iDict.Count);
                }
                else
                {
                    // Single value - wrap it
                    if (!result.ContainsKey(questionnaireIdStr))
                    {
                        result[questionnaireIdStr] = new Dictionary<string, object>();
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
        /// Get next stage ID based on order
        /// </summary>
        private async Task<long?> GetNextStageIdAsync(long currentStageId)
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
                    .Where(s => s.TenantId == _userContext.TenantId)
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
                        rulesEngineRules.Add(new RulesEngine.Models.Rule
                        {
                            RuleName = $"Rule{ruleIndex}",
                            Expression = expression,
                            SuccessEvent = "true"
                        });
                        expressions.Add(expression);
                        ruleIndex++;
                    }
                }

                // If logic is OR, combine all rules into one with OR operator
                if (frontendRules.Logic?.ToUpper() == "OR" && rulesEngineRules.Count > 1)
                {
                    var combinedExpression = string.Join(" || ", expressions.Select(e => $"({e})"));
                    rulesEngineRules = new List<RulesEngine.Models.Rule>
                    {
                        new RulesEngine.Models.Rule
                        {
                            RuleName = "CombinedOrRule",
                            Expression = combinedExpression,
                            SuccessEvent = "true"
                        }
                    };
                }

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

            // Get the value representation
            string valueStr;
            if (rule.Value == null)
            {
                valueStr = "null";
            }
            else if (rule.Value is string strValue)
            {
                valueStr = $"\"{strValue}\"";
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
                return $"{rule.FieldPath} == true";
            }
            else if (operatorLower == "completestage")
            {
                // CompleteStage: check if all tasks in stage are completed
                // This requires checking checklist.status == "Completed"
                return "input.checklist.status == \"Completed\"";
            }

            var op = operatorLower switch
            {
                "==" or "equals" or "eq" => "==",
                "!=" or "notequals" or "ne" => "!=",
                ">" or "gt" => ">",
                "<" or "lt" => "<",
                ">=" or "gte" => ">=",
                "<=" or "lte" => "<=",
                "contains" => ".Contains",
                "notcontains" => "!.Contains",
                "startswith" => ".StartsWith",
                "endswith" => ".EndsWith",
                "isnull" => "== null",
                "isnotnull" => "!= null",
                "isempty" => "IsEmpty",
                "isnotempty" => "!IsEmpty",
                "inlist" => "InList",
                "notinlist" => "!InList",
                _ => "=="
            };

            // Build expression based on operator type
            if (op == ".Contains" || op == ".StartsWith" || op == ".EndsWith")
            {
                return $"{rule.FieldPath}{op}({valueStr})";
            }
            else if (op == "!.Contains")
            {
                return $"!{rule.FieldPath}.Contains({valueStr})";
            }
            else if (op == "== null" || op == "!= null")
            {
                return $"{rule.FieldPath} {op}";
            }
            else if (op == "IsEmpty" || op == "!IsEmpty")
            {
                var prefix = op.StartsWith("!") ? "!" : "";
                return $"{prefix}RuleUtils.IsEmpty({rule.FieldPath})";
            }
            else if (op == "InList" || op == "!InList")
            {
                // InList: check if value is in a comma-separated list
                var prefix = op.StartsWith("!") ? "!" : "";
                return $"{prefix}RuleUtils.InList({rule.FieldPath}, {valueStr})";
            }
            else
            {
                return $"{rule.FieldPath} {op} {valueStr}";
            }
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
