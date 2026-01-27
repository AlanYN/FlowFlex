using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Helpers;
using FlowFlex.Application.Service.OW.StageCondition;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Enums.OW;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RulesEngine.Models;
using SqlSugar;
using RulesEngineWorkflow = RulesEngine.Models.Workflow;

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
        private readonly IComponentNameQueryService _componentNameQueryService;
        private readonly UserContext _userContext;
        private readonly ILogger<RulesEngineService> _logger;
        private readonly RuleConversionHelper _ruleConversionHelper;

        // RulesEngine settings with custom functions
        private readonly ReSettings _reSettings;

        public RulesEngineService(
            ISqlSugarClient db,
            IStageRepository stageRepository,
            IOnboardingRepository onboardingRepository,
            IComponentDataService componentDataService,
            IComponentNameQueryService componentNameQueryService,
            UserContext userContext,
            ILogger<RulesEngineService> logger)
        {
            _db = db;
            _stageRepository = stageRepository;
            _onboardingRepository = onboardingRepository;
            _componentDataService = componentDataService;
            _componentNameQueryService = componentNameQueryService;
            _userContext = userContext;
            _logger = logger;
            _ruleConversionHelper = new RuleConversionHelper(logger);

            _reSettings = new ReSettings
            {
                CustomTypes = new[] { typeof(RuleUtils) }
            };
        }

        #region Public Methods

        /// <summary>
        /// Evaluate condition for a completed stage
        /// </summary>
        public async Task<ConditionEvaluationResult> EvaluateConditionAsync(long onboardingId, long stageId)
        {
            try
            {
                var condition = await GetConditionByStageIdAsync(stageId);

                if (condition == null || !condition.IsActive || condition.Status != StageConditionConstants.StatusValid)
                {
                    _logger.LogDebug("No active condition found for stage {StageId}, proceeding to next stage", stageId);
                    return new ConditionEvaluationResult
                    {
                        IsConditionMet = false,
                        NextStageId = await GetNextStageIdAsync(stageId)
                    };
                }

                var inputData = await RuleInputDataBuilder.BuildInputDataAsync(onboardingId, stageId, _componentDataService, _logger);
                var ruleResults = await ExecuteRulesAsync(condition.RulesJson, inputData);
                var isConditionMet = EvaluateRuleResults(ruleResults, condition.RulesJson);

                var result = new ConditionEvaluationResult
                {
                    IsConditionMet = isConditionMet,
                    RuleResults = ruleResults
                };

                var logic = GetLogicFromRulesJson(condition.RulesJson) ?? StageConditionConstants.LogicAnd;
                if (isConditionMet)
                {
                    _logger.LogInformation("Condition '{ConditionName}' met for onboarding {OnboardingId}, stage {StageId} (Logic={Logic}, {SuccessCount}/{TotalCount} rules passed)",
                        condition.Name, onboardingId, stageId, logic, ruleResults.Count(r => r.IsSuccess), ruleResults.Count);
                }
                else
                {
                    result.NextStageId = condition.FallbackStageId ?? await GetNextStageIdAsync(stageId);
                    _logger.LogInformation("Condition '{ConditionName}' not met for onboarding {OnboardingId}, stage {StageId}, fallback to stage {NextStageId}",
                        condition.Name, onboardingId, stageId, result.NextStageId);
                }

                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid RulesJson format for stage {StageId}. Exception: {ExceptionType}", stageId, ex.GetType().Name);
                return CreateFallbackResult(stageId, "Invalid RulesJson format");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating condition for onboarding {OnboardingId}, stage {StageId}. Exception: {ExceptionType}",
                    onboardingId, stageId, ex.GetType().Name);
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
        /// </summary>
        public async Task<ConditionEvaluationResult> EvaluateConditionWithLockAsync(long onboardingId, long stageId)
        {
            try
            {
                var dbResult = await _db.Ado.UseTranAsync(async () =>
                {
                    var onboarding = await _db.Queryable<Onboarding>()
                        .Where(o => o.Id == onboardingId && o.IsValid)
                        .Where(o => o.TenantId == _userContext.TenantId)
                        .With(SqlWith.UpdLock)
                        .FirstAsync();

                    if (onboarding == null)
                    {
                        throw new InvalidOperationException($"Onboarding {onboardingId} not found");
                    }

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

                    return await EvaluateConditionAsync(onboardingId, stageId);
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
        /// </summary>
        public async Task<ConditionEvaluationResult> EvaluateAndExecuteWithTransactionAsync(
            long onboardingId,
            long stageId,
            IConditionActionExecutor actionExecutor,
            IOperationChangeLogService changeLogService)
        {
            Domain.Entities.OW.StageCondition condition = null;
            ConditionEvaluationResult result = null;

            try
            {
                var dbResult = await _db.Ado.UseTranAsync(async () =>
                {
                    // 1. Acquire row-level lock
                    var onboarding = await AcquireLockAndValidateAsync(onboardingId, stageId);
                    if (onboarding == null)
                    {
                        return new ConditionEvaluationResult
                        {
                            IsConditionMet = false,
                            ErrorMessage = "Stage already completed by another request",
                            NextStageId = await GetNextStageIdAsync(stageId)
                        };
                    }

                    // 2. Load condition
                    condition = await GetConditionByStageIdAsync(stageId);
                    if (condition == null || !condition.IsActive || condition.Status != StageConditionConstants.StatusValid)
                    {
                        _logger.LogDebug("No active condition for stage {StageId}, proceeding to next stage", stageId);
                        return new ConditionEvaluationResult
                        {
                            IsConditionMet = false,
                            NextStageId = await GetNextStageIdAsync(stageId)
                        };
                    }

                    // 3. Evaluate rules
                    var inputData = await RuleInputDataBuilder.BuildInputDataAsync(onboardingId, stageId, _componentDataService, _logger);
                    var ruleResults = await ExecuteRulesAsync(condition.RulesJson, inputData);
                    var isConditionMet = EvaluateRuleResults(ruleResults, condition.RulesJson);

                    result = new ConditionEvaluationResult
                    {
                        IsConditionMet = isConditionMet,
                        RuleResults = ruleResults
                    };

                    // 4. Execute actions based on condition result
                    var context = new ActionExecutionContext
                    {
                        OnboardingId = onboardingId,
                        StageId = stageId,
                        ConditionId = condition.Id,
                        TenantId = _userContext.TenantId,
                        UserId = long.TryParse(_userContext.UserId, out var uid) ? uid : 0
                    };

                    if (isConditionMet && actionExecutor != null)
                    {
                        await HandleConditionMetAsync(result, condition, context, actionExecutor);
                    }
                    else if (!isConditionMet)
                    {
                        await HandleConditionNotMetAsync(result, condition, context, actionExecutor);
                    }

                    return result;
                });

                result = dbResult.Data;

                // 5. Log changes after transaction commits
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
        /// Build input data object for RulesEngine evaluation
        /// </summary>
        public async Task<object> BuildInputDataAsync(long onboardingId, long stageId)
        {
            return await RuleInputDataBuilder.BuildInputDataAsync(onboardingId, stageId, _componentDataService, _logger);
        }

        #endregion

        #region Private Methods - Transaction Helpers

        private async Task<Onboarding> AcquireLockAndValidateAsync(long onboardingId, long stageId)
        {
            var onboarding = await _db.Queryable<Onboarding>()
                .Where(o => o.Id == onboardingId && o.IsValid)
                .Where(o => o.TenantId == _userContext.TenantId)
                .With(SqlWith.UpdLock)
                .FirstAsync();

            if (onboarding == null)
            {
                throw new InvalidOperationException($"Onboarding {onboardingId} not found");
            }

            var stageProgress = onboarding.StagesProgress?.FirstOrDefault(sp => sp.StageId == stageId);
            if (stageProgress != null && stageProgress.IsCompleted)
            {
                _logger.LogWarning("Stage {StageId} already completed, concurrent request detected", stageId);
                return null;
            }

            return onboarding;
        }

        private async Task HandleConditionMetAsync(
            ConditionEvaluationResult result,
            Domain.Entities.OW.StageCondition condition,
            ActionExecutionContext context,
            IConditionActionExecutor actionExecutor)
        {
            var actionResult = await actionExecutor.ExecuteActionsAsync(condition.ActionsJson, context);
            result.ActionResults = actionResult.Details;

            _logger.LogInformation("Condition '{ConditionName}' met, executed {ActionCount} actions",
                condition.Name, actionResult.Details.Count);

            // Check if any stage-related action was executed
            var hasSuccessfulStageAction = actionResult.Details.Any(d =>
                d.Success && StageConditionConstants.StageControlActionTypes.Contains(d.ActionType.ToLower()));

            // Auto-advance if no stage action executed
            if (!hasSuccessfulStageAction)
            {
                var nextStageId = await GetNextStageIdAsync(context.StageId);
                if (nextStageId.HasValue)
                {
                    _logger.LogInformation("Condition '{ConditionName}' met but no stage-related action executed, auto-advancing to next stage {NextStageId}",
                        condition.Name, nextStageId.Value);

                    var autoAdvanceActionsJson = System.Text.Json.JsonSerializer.Serialize(new[]
                    {
                        new { type = "GoToStage", order = StageConditionConstants.AutoAdvanceActionOrder, targetStageId = nextStageId.Value.ToString() }
                    });

                    var autoAdvanceResult = await actionExecutor.ExecuteActionsAsync(autoAdvanceActionsJson, context);
                    if (autoAdvanceResult.Details.Any())
                    {
                        var autoAdvanceDetail = autoAdvanceResult.Details.First();
                        autoAdvanceDetail.ActionType = "AutoToNextStage";
                        result.ActionResults.Add(autoAdvanceDetail);
                    }

                    result.NextStageId = nextStageId;
                }
            }
        }

        private async Task HandleConditionNotMetAsync(
            ConditionEvaluationResult result,
            Domain.Entities.OW.StageCondition condition,
            ActionExecutionContext context,
            IConditionActionExecutor actionExecutor)
        {
            result.NextStageId = condition.FallbackStageId ?? await GetNextStageIdAsync(context.StageId);

            if (condition.FallbackStageId.HasValue && actionExecutor != null)
            {
                var fallbackActionsJson = System.Text.Json.JsonSerializer.Serialize(new[]
                {
                    new { type = "GoToStage", order = 0, targetStageId = condition.FallbackStageId.Value.ToString() }
                });

                var actionResult = await actionExecutor.ExecuteActionsAsync(fallbackActionsJson, context);
                result.ActionResults = actionResult.Details;

                _logger.LogInformation("Condition '{ConditionName}' not met, executed fallback GoToStage to stage {FallbackStageId}",
                    condition.Name, condition.FallbackStageId.Value);
            }
            else
            {
                _logger.LogInformation("Condition '{ConditionName}' not met, fallback to stage {NextStageId}",
                    condition.Name, result.NextStageId);
            }
        }

        #endregion

        #region Private Methods - Rule Evaluation

        private bool EvaluateRuleResults(List<RuleEvaluationDetail> ruleResults, string rulesJson)
        {
            if (ruleResults == null || !ruleResults.Any())
            {
                return false;
            }

            var logic = GetLogicFromRulesJson(rulesJson);

            if (string.Equals(logic, StageConditionConstants.LogicOr, StringComparison.OrdinalIgnoreCase))
            {
                return ruleResults.Any(r => r.IsSuccess);
            }

            return ruleResults.All(r => r.IsSuccess);
        }

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
            catch { }
            return null;
        }

        private async Task<List<RuleEvaluationDetail>> ExecuteRulesAsync(string rulesJson, object inputData)
        {
            var ruleResults = new List<RuleEvaluationDetail>();

            try
            {
                var convertedRulesJson = await _ruleConversionHelper.ConvertToRulesEngineFormatIfNeededAsync(
                    rulesJson,
                    _componentNameQueryService.BuildComponentNameMapAsync);

                var workflows = JsonConvert.DeserializeObject<RulesEngineWorkflow[]>(convertedRulesJson);

                if (workflows == null || workflows.Length == 0)
                {
                    _logger.LogWarning("No workflows found in RulesJson");
                    return ruleResults;
                }

                var rulesEngine = new RulesEngine.RulesEngine(workflows, _reSettings);
                var ruleParameter = new RuleParameter("input", inputData);
                var workflowName = workflows[0].WorkflowName ?? "StageCondition";
                var results = await rulesEngine.ExecuteAllRulesAsync(workflowName, ruleParameter);

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

        #endregion

        #region Private Methods - Database Queries

        private async Task<Domain.Entities.OW.StageCondition> GetConditionByStageIdAsync(long stageId)
        {
            return await _db.Queryable<Domain.Entities.OW.StageCondition>()
                .Where(c => c.StageId == stageId && c.IsValid)
                .Where(c => c.TenantId == _userContext.TenantId)
                .FirstAsync();
        }

        private async Task<long?> GetNextStageIdAsync(long currentStageId, string tenantId = null)
        {
            try
            {
                var currentStage = await _stageRepository.GetByIdAsync(currentStageId);
                if (currentStage == null)
                {
                    return null;
                }

                var effectiveTenantId = tenantId ?? currentStage.TenantId;

                var nextStage = await _db.Queryable<Stage>()
                    .Where(s => s.WorkflowId == currentStage.WorkflowId && s.IsValid && s.IsActive)
                    .Where(s => s.TenantId == effectiveTenantId)
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

        #endregion

        #region Private Methods - Logging

        private async Task LogConditionEvaluationAsync(
            IOperationChangeLogService changeLogService,
            long onboardingId,
            long stageId,
            Domain.Entities.OW.StageCondition condition,
            ConditionEvaluationResult result)
        {
            try
            {
                var successfulRules = result.RuleResults?.Where(r => r.IsSuccess).Select(r => r.RuleName).ToList() ?? new List<string>();
                var failedRules = result.RuleResults?.Where(r => !r.IsSuccess).Select(r => r.RuleName).ToList() ?? new List<string>();
                var successfulActions = result.ActionResults?.Where(a => a.Success).ToList() ?? new List<ActionExecutionDetail>();
                var failedActions = result.ActionResults?.Where(a => !a.Success).ToList() ?? new List<ActionExecutionDetail>();

                var operationTitle = ConditionLogHelper.BuildOperationTitle(
                    condition.Name, result.IsConditionMet, successfulRules, failedRules, result.ActionResults);

                var operationDescription = ConditionLogHelper.BuildOperationDescription(
                    condition.Name, result.IsConditionMet, successfulRules, failedRules, successfulActions, failedActions);

                var extendedData = System.Text.Json.JsonSerializer.Serialize(
                    ConditionLogHelper.BuildExtendedData(condition, result));

                await changeLogService.LogOperationAsync(
                    operationType: OperationTypeEnum.StageConditionEvaluate,
                    businessModule: BusinessModuleEnum.StageCondition,
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
            }
        }

        #endregion

        #region Private Methods - Fallback Results

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

        private async Task<ConditionEvaluationResult> CreateFallbackResultAsync(long stageId, string errorMessage)
        {
            var result = CreateFallbackResult(stageId, errorMessage);
            result.NextStageId = await GetNextStageIdAsync(stageId);
            return result;
        }

        #endregion
    }
}
