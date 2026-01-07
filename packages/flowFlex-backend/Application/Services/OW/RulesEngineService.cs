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
        /// Evaluate condition with transaction lock for concurrency control
        /// </summary>
        public async Task<ConditionEvaluationResult> EvaluateConditionWithLockAsync(long onboardingId, long stageId)
        {
            try
            {
                var dbResult = await _db.Ado.UseTranAsync(async () =>
                {
                    // Row-level lock on onboarding record
                    var onboarding = await _db.Queryable<Onboarding>()
                        .Where(o => o.Id == onboardingId && o.IsValid)
                        .Where(o => o.TenantId == _userContext.TenantId)
                        .With(SqlWith.UpdLock)
                        .FirstAsync();

                    if (onboarding == null)
                    {
                        throw new InvalidOperationException($"Onboarding {onboardingId} not found");
                    }

                    // Evaluate condition
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

                // Add checklist data
                inputDict["checklist"] = new
                {
                    status = checklistData.Status,
                    completedCount = checklistData.CompletedCount,
                    totalCount = checklistData.TotalCount,
                    completionPercentage = checklistData.TotalCount > 0 
                        ? (double)checklistData.CompletedCount / checklistData.TotalCount * 100 
                        : 0,
                    tasks = checklistData.Tasks
                };

                // Add questionnaire data
                inputDict["questionnaire"] = new
                {
                    status = questionnaireData.Status,
                    totalScore = questionnaireData.TotalScore,
                    answers = questionnaireData.Answers
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
        /// </summary>
        private async Task<List<RuleEvaluationDetail>> ExecuteRulesAsync(string rulesJson, object inputData)
        {
            var ruleResults = new List<RuleEvaluationDetail>();

            try
            {
                // Parse RulesEngine workflow JSON
                var workflows = JsonConvert.DeserializeObject<RulesEngine.Models.Workflow[]>(rulesJson);
                
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

        #endregion
    }
}
