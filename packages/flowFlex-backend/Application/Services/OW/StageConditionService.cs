using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Enums.Permission;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RulesEngine.Models;
using SqlSugar;

namespace FlowFlex.Application.Service.OW
{
    /// <summary>
    /// Stage Condition service implementation
    /// </summary>
    public class StageConditionService : IStageConditionService, IScopedService
    {
        private readonly ISqlSugarClient _db;
        private readonly IStageRepository _stageRepository;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IPermissionService _permissionService;
        private readonly IMapper _mapper;
        private readonly UserContext _userContext;
        private readonly ILogger<StageConditionService> _logger;

        public StageConditionService(
            ISqlSugarClient db,
            IStageRepository stageRepository,
            IWorkflowRepository workflowRepository,
            IPermissionService permissionService,
            IMapper mapper,
            UserContext userContext,
            ILogger<StageConditionService> logger)
        {
            _db = db;
            _stageRepository = stageRepository;
            _workflowRepository = workflowRepository;
            _permissionService = permissionService;
            _mapper = mapper;
            _userContext = userContext;
            _logger = logger;
        }

        #region CRUD Operations

        /// <summary>
        /// Create a new stage condition
        /// </summary>
        public async Task<long> CreateAsync(StageConditionInputDto input)
        {
            // Validate permission
            await ValidateWorkflowPermissionAsync(input.WorkflowId, OperationTypeEnum.Operate);

            // Validate stage exists
            var stage = await _stageRepository.GetByIdAsync(input.StageId);
            if (stage == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Stage {input.StageId} not found");
            }

            // Validate one condition per stage
            var existingCondition = await _db.Queryable<StageCondition>()
                .Where(c => c.StageId == input.StageId && c.IsValid)
                .Where(c => c.TenantId == _userContext.TenantId)
                .FirstAsync();

            if (existingCondition != null)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, 
                    $"Stage {input.StageId} already has a condition configured. Each stage can only have one condition.");
            }

            // Validate RulesJson format
            var rulesValidation = await ValidateRulesJsonAsync(input.RulesJson);
            if (!rulesValidation.IsValid)
            {
                var ruleErrorMessages = rulesValidation.Errors.Select(e => $"[{e.Code}] {e.Message}");
                throw new CRMException(ErrorCodeEnum.BusinessError, 
                    $"Invalid RulesJson: {string.Join("; ", ruleErrorMessages)}");
            }

            // Validate ActionsJson format
            var actionsValidation = ValidateActionsJson(input.ActionsJson);
            if (!actionsValidation.IsValid)
            {
                var actionErrorMessages = actionsValidation.Errors.Select(e => $"[{e.Code}] {e.Message}");
                throw new CRMException(ErrorCodeEnum.BusinessError, 
                    $"Invalid ActionsJson: {string.Join("; ", actionErrorMessages)}");
            }

            // Create entity
            var entity = new StageCondition
            {
                StageId = input.StageId,
                WorkflowId = input.WorkflowId > 0 ? input.WorkflowId : stage.WorkflowId,
                Name = input.Name,
                Description = input.Description ?? string.Empty,
                RulesJson = input.RulesJson,
                ActionsJson = input.ActionsJson,
                FallbackStageId = input.FallbackStageId,
                IsActive = input.IsActive,
                Status = "Valid"
            };

            // Initialize entity fields
            entity.InitNewId();
            entity.TenantId = _userContext.TenantId ?? "default";
            entity.AppCode = _userContext.AppCode ?? "default";
            entity.CreateDate = DateTimeOffset.UtcNow;
            entity.ModifyDate = DateTimeOffset.UtcNow;
            entity.CreateBy = _userContext.UserName ?? "SYSTEM";
            entity.ModifyBy = _userContext.UserName ?? "SYSTEM";
            if (long.TryParse(_userContext.UserId, out var userId))
            {
                entity.CreateUserId = userId;
                entity.ModifyUserId = userId;
            }
            entity.IsValid = true;

            await _db.Insertable(entity).ExecuteCommandAsync();

            _logger.LogInformation("Created stage condition {ConditionId} for stage {StageId}", entity.Id, input.StageId);

            return entity.Id;
        }

        /// <summary>
        /// Update an existing stage condition
        /// </summary>
        public async Task<bool> UpdateAsync(long id, StageConditionInputDto input)
        {
            // Get existing condition
            var condition = await GetEntityByIdAsync(id);
            if (condition == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Condition {id} not found");
            }

            // Validate permission
            await ValidateWorkflowPermissionAsync(condition.WorkflowId, OperationTypeEnum.Operate);

            // Validate RulesJson format
            var rulesValidation = await ValidateRulesJsonAsync(input.RulesJson);
            if (!rulesValidation.IsValid)
            {
                var ruleErrorMessages = rulesValidation.Errors.Select(e => $"[{e.Code}] {e.Message}");
                throw new CRMException(ErrorCodeEnum.BusinessError, 
                    $"Invalid RulesJson: {string.Join("; ", ruleErrorMessages)}");
            }

            // Validate ActionsJson format
            var actionsValidation = ValidateActionsJson(input.ActionsJson);
            if (!actionsValidation.IsValid)
            {
                var actionErrorMessages = actionsValidation.Errors.Select(e => $"[{e.Code}] {e.Message}");
                throw new CRMException(ErrorCodeEnum.BusinessError, 
                    $"Invalid ActionsJson: {string.Join("; ", actionErrorMessages)}");
            }

            // Update entity
            condition.Name = input.Name;
            condition.Description = input.Description ?? string.Empty;
            condition.RulesJson = input.RulesJson;
            condition.ActionsJson = input.ActionsJson;
            condition.FallbackStageId = input.FallbackStageId;
            condition.IsActive = input.IsActive;
            condition.Status = "Valid";
            condition.ModifyDate = DateTimeOffset.UtcNow;
            condition.ModifyBy = _userContext.UserName ?? "SYSTEM";

            var result = await _db.Updateable(condition).ExecuteCommandAsync();

            _logger.LogInformation("Updated stage condition {ConditionId}", id);

            return result > 0;
        }

        /// <summary>
        /// Delete a stage condition
        /// </summary>
        public async Task<bool> DeleteAsync(long id)
        {
            // Get existing condition
            var condition = await GetEntityByIdAsync(id);
            if (condition == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Condition {id} not found");
            }

            // Validate permission
            await ValidateWorkflowPermissionAsync(condition.WorkflowId, OperationTypeEnum.Operate);

            // Soft delete
            condition.IsValid = false;
            condition.ModifyDate = DateTimeOffset.UtcNow;
            condition.ModifyBy = _userContext.UserName ?? "SYSTEM";

            var result = await _db.Updateable(condition)
                .UpdateColumns(c => new { c.IsValid, c.ModifyDate, c.ModifyBy })
                .ExecuteCommandAsync();

            _logger.LogInformation("Deleted stage condition {ConditionId}", id);

            return result > 0;
        }

        /// <summary>
        /// Get condition by ID
        /// </summary>
        public async Task<StageConditionOutputDto?> GetByIdAsync(long id)
        {
            var entity = await GetEntityByIdAsync(id);
            if (entity == null)
            {
                return null;
            }

            // Validate read permission
            await ValidateWorkflowPermissionAsync(entity.WorkflowId, OperationTypeEnum.View);

            return MapToOutputDto(entity);
        }

        /// <summary>
        /// Get condition by stage ID
        /// </summary>
        public async Task<StageConditionOutputDto?> GetByStageIdAsync(long stageId)
        {
            var entity = await _db.Queryable<StageCondition>()
                .Where(c => c.StageId == stageId && c.IsValid)
                .Where(c => c.TenantId == _userContext.TenantId)
                .FirstAsync();

            if (entity == null)
            {
                return null;
            }

            // Validate read permission
            await ValidateWorkflowPermissionAsync(entity.WorkflowId, OperationTypeEnum.View);

            return MapToOutputDto(entity);
        }

        /// <summary>
        /// Get all conditions for a workflow
        /// </summary>
        public async Task<List<StageConditionOutputDto>> GetByWorkflowIdAsync(long workflowId)
        {
            // Validate read permission
            await ValidateWorkflowPermissionAsync(workflowId, OperationTypeEnum.View);

            var entities = await _db.Queryable<StageCondition>()
                .Where(c => c.WorkflowId == workflowId && c.IsValid)
                .Where(c => c.TenantId == _userContext.TenantId)
                .OrderBy(c => c.CreateDate)
                .ToListAsync();

            return entities.Select(MapToOutputDto).ToList();
        }

        #endregion


        #region Validation

        /// <summary>
        /// Validate a condition configuration
        /// </summary>
        public async Task<ConditionValidationResult> ValidateAsync(long id)
        {
            var result = new ConditionValidationResult { IsValid = true };

            var condition = await GetEntityByIdAsync(id);
            if (condition == null)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError { Code = "NOT_FOUND", Message = "Condition not found" });
                return result;
            }

            // Validate RulesJson
            var rulesValidation = await ValidateRulesJsonAsync(condition.RulesJson);
            if (!rulesValidation.IsValid)
            {
                result.IsValid = false;
                result.Errors.AddRange(rulesValidation.Errors);
            }

            // Validate ActionsJson
            var actionsValidation = ValidateActionsJson(condition.ActionsJson);
            if (!actionsValidation.IsValid)
            {
                result.IsValid = false;
                result.Errors.AddRange(actionsValidation.Errors);
            }

            // Validate referenced stages exist
            await ValidateReferencedStagesAsync(condition, result);

            // Validate referenced actions exist
            await ValidateReferencedActionsAsync(condition, result);

            // Check for circular references
            await CheckCircularReferencesAsync(condition, result);

            return result;
        }

        /// <summary>
        /// Validate RulesJson format - supports both frontend custom format and RulesEngine format
        /// </summary>
        public async Task<ConditionValidationResult> ValidateRulesJsonAsync(string rulesJson)
        {
            var result = new ConditionValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(rulesJson))
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError { Code = "RULES_REQUIRED", Message = "RulesJson is required" });
                return result;
            }

            try
            {
                RulesEngine.Models.Workflow[] workflows = null;

                // First, try to detect if it's frontend custom format (has "logic" property)
                var jsonObj = Newtonsoft.Json.Linq.JToken.Parse(rulesJson);
                
                if (jsonObj is Newtonsoft.Json.Linq.JObject jObject && jObject.ContainsKey("logic"))
                {
                    // Frontend custom format - convert to RulesEngine format
                    var convertedJson = ConvertFrontendRulesToRulesEngineFormat(rulesJson);
                    workflows = JsonConvert.DeserializeObject<RulesEngine.Models.Workflow[]>(convertedJson);
                    
                    // Add info that format was converted
                    result.Warnings.Add(new ValidationWarning { Code = "FORMAT_CONVERTED", Message = "Frontend rule format detected and converted to RulesEngine format" });
                }
                else if (jsonObj is Newtonsoft.Json.Linq.JArray)
                {
                    // Standard RulesEngine format
                    workflows = JsonConvert.DeserializeObject<RulesEngine.Models.Workflow[]>(rulesJson);
                }
                else
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError { Code = "INVALID_FORMAT", Message = "RulesJson must be either a RulesEngine workflow array or frontend rule format with 'logic' property" });
                    return result;
                }
                
                if (workflows == null || workflows.Length == 0)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError { Code = "RULES_EMPTY", Message = "RulesJson must contain at least one workflow" });
                    return result;
                }

                foreach (var workflow in workflows)
                {
                    if (string.IsNullOrEmpty(workflow.WorkflowName))
                    {
                        result.Warnings.Add(new ValidationWarning { Code = "WORKFLOW_NAME_EMPTY", Message = "Workflow name is empty, will use default name 'StageCondition'" });
                    }

                    if (workflow.Rules == null || !workflow.Rules.Any())
                    {
                        result.IsValid = false;
                        result.Errors.Add(new ValidationError { Code = "RULES_EMPTY", Message = $"Workflow '{workflow.WorkflowName}' must contain at least one rule" });
                    }
                    else
                    {
                        foreach (var rule in workflow.Rules)
                        {
                            if (string.IsNullOrEmpty(rule.RuleName))
                            {
                                result.IsValid = false;
                                result.Errors.Add(new ValidationError { Code = "RULE_NAME_REQUIRED", Message = "Rule name is required" });
                            }

                            if (string.IsNullOrEmpty(rule.Expression))
                            {
                                result.IsValid = false;
                                result.Errors.Add(new ValidationError { Code = "RULE_EXPRESSION_REQUIRED", Message = $"Rule '{rule.RuleName}' must have an expression" });
                            }
                        }
                    }
                }

                // Try to create RulesEngine instance to validate expressions
                try
                {
                    var reSettings = new ReSettings
                    {
                        CustomTypes = new[] { typeof(RuleUtils) }
                    };
                    var rulesEngine = new RulesEngine.RulesEngine(workflows, reSettings);
                }
                catch (Exception ex)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError { Code = "INVALID_EXPRESSION", Message = $"Invalid rule expression: {ex.Message}" });
                }
            }
            catch (JsonException ex)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError { Code = "INVALID_JSON", Message = $"Invalid JSON format: {ex.Message}" });
            }

            return await Task.FromResult(result);
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
                    // Return empty workflow array
                    return "[]";
                }

                var expressions = new List<string>();
                var rulesEngineRules = new List<RulesEngine.Models.Rule>();
                int ruleIndex = 1;

                foreach (var rule in frontendRules.Rules)
                {
                    // Build expression from frontend rule
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

                // If logic is OR, we need to combine all rules into one with OR operator
                // If logic is AND, each rule is evaluated separately (all must pass)
                if (frontendRules.Logic?.ToUpper() == "OR" && rulesEngineRules.Count > 1)
                {
                    // Combine all expressions with OR
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
                throw new JsonException($"Failed to convert frontend rules format: {ex.Message}");
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
            var op = rule.Operator?.ToLower() switch
            {
                "==" or "equals" or "eq" => "==",
                "!=" or "notequals" or "ne" => "!=",
                ">" or "gt" => ">",
                "<" or "lt" => "<",
                ">=" or "gte" => ">=",
                "<=" or "lte" => "<=",
                "contains" => ".Contains",
                "startswith" => ".StartsWith",
                "endswith" => ".EndsWith",
                "isnull" => "== null",
                "isnotnull" => "!= null",
                "isempty" => "IsEmpty",
                "isnotempty" => "!IsEmpty",
                _ => "=="
            };

            // Build expression based on operator type
            if (op == ".Contains" || op == ".StartsWith" || op == ".EndsWith")
            {
                return $"{rule.FieldPath}{op}({valueStr})";
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

        #region Private Methods

        /// <summary>
        /// Get entity by ID with tenant isolation
        /// </summary>
        private async Task<StageCondition?> GetEntityByIdAsync(long id)
        {
            return await _db.Queryable<StageCondition>()
                .Where(c => c.Id == id && c.IsValid)
                .Where(c => c.TenantId == _userContext.TenantId)
                .FirstAsync();
        }

        /// <summary>
        /// Validate workflow permission
        /// </summary>
        private async Task ValidateWorkflowPermissionAsync(long workflowId, OperationTypeEnum operationType)
        {
            if (string.IsNullOrEmpty(_userContext.UserId) || !long.TryParse(_userContext.UserId, out var userId))
            {
                throw new CRMException(ErrorCodeEnum.AuthenticationFail, "User not authenticated");
            }

            var permission = await _permissionService.CheckWorkflowAccessAsync(userId, workflowId, operationType);
            
            if (!permission.Success)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, 
                    $"No permission to {operationType} workflow: {permission.ErrorMessage}");
            }
        }

        /// <summary>
        /// Validate ActionsJson format
        /// </summary>
        private ConditionValidationResult ValidateActionsJson(string actionsJson)
        {
            var result = new ConditionValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(actionsJson))
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError { Code = "ACTIONS_REQUIRED", Message = "ActionsJson is required" });
                return result;
            }

            try
            {
                var actions = JsonConvert.DeserializeObject<List<ConditionAction>>(actionsJson);
                
                if (actions == null || actions.Count == 0)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError { Code = "ACTIONS_EMPTY", Message = "ActionsJson must contain at least one action" });
                    return result;
                }

                var validActionTypes = new[] { "gotostage", "skipstage", "endworkflow", "sendnotification", "updatefield", "triggeraction", "assignuser" };

                foreach (var action in actions)
                {
                    if (string.IsNullOrEmpty(action.Type))
                    {
                        result.IsValid = false;
                        result.Errors.Add(new ValidationError { Code = "ACTION_TYPE_REQUIRED", Message = "Action type is required" });
                        continue;
                    }

                    if (!validActionTypes.Contains(action.Type.ToLower()))
                    {
                        result.IsValid = false;
                        result.Errors.Add(new ValidationError { Code = "INVALID_ACTION_TYPE", Message = $"Invalid action type: {action.Type}" });
                    }

                    // Validate required parameters for each action type
                    switch (action.Type.ToLower())
                    {
                        case "gotostage":
                            if (!action.TargetStageId.HasValue)
                            {
                                result.IsValid = false;
                                result.Errors.Add(new ValidationError { Code = "GOTOSTAGE_TARGET_REQUIRED", Message = "GoToStage action requires targetStageId" });
                            }
                            break;

                        case "updatefield":
                            // Support fieldId, fieldName, and parameters.fieldPath/fieldName
                            var hasFieldId = !string.IsNullOrEmpty(action.FieldId);
                            var hasFieldName = !string.IsNullOrEmpty(action.FieldName);
                            var hasFieldInParams = action.Parameters != null && 
                                (action.Parameters.ContainsKey("fieldId") || action.Parameters.ContainsKey("fieldPath") || action.Parameters.ContainsKey("fieldName"));
                            
                            if (!hasFieldId && !hasFieldName && !hasFieldInParams)
                            {
                                result.IsValid = false;
                                result.Errors.Add(new ValidationError { Code = "UPDATEFIELD_NAME_REQUIRED", Message = "UpdateField action requires fieldId, fieldName or parameters.fieldPath" });
                            }
                            break;

                        case "triggeraction":
                            if (!action.ActionDefinitionId.HasValue)
                            {
                                result.IsValid = false;
                                result.Errors.Add(new ValidationError { Code = "TRIGGERACTION_ID_REQUIRED", Message = "TriggerAction action requires actionDefinitionId" });
                            }
                            break;

                        case "assignuser":
                            // Check parameters dictionary for assigneeType and assigneeIds
                            if (action.Parameters != null)
                            {
                                // Check assigneeType in parameters
                                if (!action.Parameters.TryGetValue("assigneeType", out var assigneeType) || 
                                    string.IsNullOrEmpty(assigneeType?.ToString()))
                                {
                                    result.IsValid = false;
                                    result.Errors.Add(new ValidationError { Code = "ASSIGNUSER_TYPE_REQUIRED", Message = "AssignUser action requires assigneeType ('user' or 'team') in parameters" });
                                }
                                else
                                {
                                    var assigneeTypeStr = assigneeType.ToString()?.ToLower();
                                    if (assigneeTypeStr != "user" && assigneeTypeStr != "team")
                                    {
                                        result.IsValid = false;
                                        result.Errors.Add(new ValidationError { Code = "ASSIGNUSER_TYPE_INVALID", Message = "AssignUser action assigneeType must be 'user' or 'team'" });
                                    }
                                }

                                // Check assigneeIds array
                                if (!HasNonEmptyArray(action.Parameters, "assigneeIds"))
                                {
                                    result.IsValid = false;
                                    result.Errors.Add(new ValidationError { Code = "ASSIGNUSER_IDS_REQUIRED", Message = "AssignUser action requires assigneeIds (non-empty array) in parameters" });
                                }
                            }
                            else
                            {
                                result.IsValid = false;
                                result.Errors.Add(new ValidationError { Code = "ASSIGNUSER_PARAMS_REQUIRED", Message = "AssignUser action requires parameters with assigneeType and assigneeIds" });
                            }
                            break;
                    }
                }
            }
            catch (JsonException ex)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError { Code = "INVALID_JSON", Message = $"Invalid JSON format: {ex.Message}" });
            }

            return result;
        }

        /// <summary>
        /// Validate referenced stages exist
        /// </summary>
        private async Task ValidateReferencedStagesAsync(StageCondition condition, ConditionValidationResult result)
        {
            // Validate fallback stage
            if (condition.FallbackStageId.HasValue)
            {
                var fallbackStage = await _stageRepository.GetByIdAsync(condition.FallbackStageId.Value);
                if (fallbackStage == null || !fallbackStage.IsActive)
                {
                    result.Warnings.Add(new ValidationWarning { Code = "FALLBACK_STAGE_INVALID", Message = $"Fallback stage {condition.FallbackStageId} not found or inactive" });
                }
            }            // Validate target stages in actions
            try
            {
                var actions = JsonConvert.DeserializeObject<List<ConditionAction>>(condition.ActionsJson);
                if (actions != null)
                {
                    foreach (var action in actions.Where(a => a.TargetStageId.HasValue))
                    {
                        var targetStage = await _stageRepository.GetByIdAsync(action.TargetStageId!.Value);
                        if (targetStage == null || !targetStage.IsActive)
                        {
                            result.Warnings.Add(new ValidationWarning { Code = "TARGET_STAGE_INVALID", Message = $"Target stage {action.TargetStageId} in action not found or inactive" });
                        }
                    }
                }
            }
            catch
            {
                // Already validated in ValidateActionsJson
            }
        }

        /// <summary>
        /// Validate referenced action definitions exist
        /// </summary>
        private async Task ValidateReferencedActionsAsync(StageCondition condition, ConditionValidationResult result)
        {
            try
            {
                var actions = JsonConvert.DeserializeObject<List<ConditionAction>>(condition.ActionsJson);
                if (actions != null)
                {
                    foreach (var action in actions.Where(a => a.ActionDefinitionId.HasValue))
                    {
                        var actionDef = await _db.Queryable<Domain.Entities.Action.ActionDefinition>()
                            .Where(a => a.Id == action.ActionDefinitionId!.Value && a.IsValid)
                            .Where(a => a.TenantId == _userContext.TenantId)
                            .FirstAsync();

                        if (actionDef == null || !actionDef.IsEnabled)
                        {
                            result.Warnings.Add(new ValidationWarning { Code = "ACTION_DEF_INVALID", Message = $"ActionDefinition {action.ActionDefinitionId} not found or disabled" });
                        }
                    }
                }
            }
            catch
            {
                // Already validated in ValidateActionsJson
            }
        }

        /// <summary>
        /// Check for circular references in stage jumps
        /// </summary>
        private async Task CheckCircularReferencesAsync(StageCondition condition, ConditionValidationResult result)
        {
            try
            {
                var actions = JsonConvert.DeserializeObject<List<ConditionAction>>(condition.ActionsJson);
                if (actions == null) return;

                var goToActions = actions.Where(a => a.Type?.ToLower() == "gotostage" && a.TargetStageId.HasValue).ToList();
                
                foreach (var action in goToActions)
                {
                    // Check if target stage has a condition that points back
                    var targetCondition = await _db.Queryable<StageCondition>()
                        .Where(c => c.StageId == action.TargetStageId!.Value && c.IsValid && c.IsActive)
                        .Where(c => c.TenantId == _userContext.TenantId)
                        .FirstAsync();

                    if (targetCondition != null)
                    {
                        var targetActions = JsonConvert.DeserializeObject<List<ConditionAction>>(targetCondition.ActionsJson);
                        if (targetActions != null)
                        {
                            var circularAction = targetActions.FirstOrDefault(a => 
                                a.Type?.ToLower() == "gotostage" && a.TargetStageId == condition.StageId);

                            if (circularAction != null)
                            {
                                result.Warnings.Add(new ValidationWarning { Code = "CIRCULAR_REFERENCE", Message = $"Potential circular reference detected: Stage {condition.StageId} -> Stage {action.TargetStageId} -> Stage {condition.StageId}" });
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors in circular reference check
            }
        }

        /// <summary>
        /// Check if parameters dictionary has a non-empty array for the given key
        /// </summary>
        private bool HasNonEmptyArray(Dictionary<string, object> parameters, string key)
        {
            if (!parameters.TryGetValue(key, out var value) || value == null)
                return false;

            // Check if it's a JArray
            if (value is Newtonsoft.Json.Linq.JArray jArray)
                return jArray.Count > 0;

            // Check if it's an IEnumerable (but not string)
            if (value is System.Collections.IEnumerable enumerable && !(value is string))
            {
                var enumerator = enumerable.GetEnumerator();
                return enumerator.MoveNext();
            }

            return false;
        }

        /// <summary>
        /// Map entity to output DTO
        /// </summary>
        private StageConditionOutputDto MapToOutputDto(StageCondition entity)
        {
            var dto = new StageConditionOutputDto
            {
                Id = entity.Id,
                StageId = entity.StageId,
                WorkflowId = entity.WorkflowId,
                Name = entity.Name,
                Description = entity.Description,
                RulesJson = entity.RulesJson,
                ActionsJson = entity.ActionsJson,
                FallbackStageId = entity.FallbackStageId,
                IsActive = entity.IsActive,
                Status = entity.Status,
                CreateDate = entity.CreateDate,
                ModifyDate = entity.ModifyDate
            };

            // Parse rules for convenience
            try
            {
                var workflows = JsonConvert.DeserializeObject<RulesEngine.Models.Workflow[]>(entity.RulesJson);
                if (workflows != null && workflows.Length > 0 && workflows[0].Rules != null)
                {
                    dto.Rules = workflows[0].Rules.Select(r => new ConditionRuleDto
                    {
                        RuleName = r.RuleName,
                        Expression = r.Expression ?? string.Empty
                    }).ToList();
                }
            }
            catch
            {
                dto.Rules = new List<ConditionRuleDto>();
            }

            // Parse actions for convenience
            try
            {
                dto.Actions = JsonConvert.DeserializeObject<List<ConditionActionDto>>(entity.ActionsJson) ?? new List<ConditionActionDto>();
            }
            catch
            {
                dto.Actions = new List<ConditionActionDto>();
            }

            return dto;
        }

        #endregion
    }
}
