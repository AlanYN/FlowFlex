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
        public async Task<StageConditionSaveResultDto> CreateAsync(StageConditionInputDto input)
        {
            var result = new StageConditionSaveResultDto();

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
                throw new CRMException(ErrorCodeEnum.ValidFail, 
                    $"Invalid RulesJson: {string.Join("; ", ruleErrorMessages)}") { ResponseCode = 410 };
            }
            // Collect warnings from rules validation
            result.Warnings.AddRange(rulesValidation.Warnings);

            // Validate ActionsJson format
            var actionsValidation = ValidateActionsJson(input.ActionsJson);
            if (!actionsValidation.IsValid)
            {
                var actionErrorMessages = actionsValidation.Errors.Select(e => $"[{e.Code}] {e.Message}");
                throw new CRMException(ErrorCodeEnum.ValidFail, 
                    $"Invalid ActionsJson: {string.Join("; ", actionErrorMessages)}") { ResponseCode = 410 };
            }
            // Collect warnings from actions validation
            result.Warnings.AddRange(actionsValidation.Warnings);

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

            result.Id = entity.Id;
            result.Success = true;
            return result;
        }

        /// <summary>
        /// Update an existing stage condition
        /// </summary>
        public async Task<StageConditionSaveResultDto> UpdateAsync(long id, StageConditionInputDto input)
        {
            var result = new StageConditionSaveResultDto { Id = id };

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
                throw new CRMException(ErrorCodeEnum.ValidFail, 
                    $"Invalid RulesJson: {string.Join("; ", ruleErrorMessages)}") { ResponseCode = 410 };
            }
            // Collect warnings from rules validation
            result.Warnings.AddRange(rulesValidation.Warnings);

            // Validate ActionsJson format
            var actionsValidation = ValidateActionsJson(input.ActionsJson);
            if (!actionsValidation.IsValid)
            {
                var actionErrorMessages = actionsValidation.Errors.Select(e => $"[{e.Code}] {e.Message}");
                throw new CRMException(ErrorCodeEnum.ValidFail, 
                    $"Invalid ActionsJson: {string.Join("; ", actionErrorMessages)}") { ResponseCode = 410 };
            }
            // Collect warnings from actions validation
            result.Warnings.AddRange(actionsValidation.Warnings);

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

            var updateResult = await _db.Updateable(condition).ExecuteCommandAsync();

            _logger.LogInformation("Updated stage condition {ConditionId}", id);

            result.Success = updateResult > 0;
            return result;
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

            var deleteResult = await _db.Updateable(condition)
                .UpdateColumns(c => new { c.IsValid, c.ModifyDate, c.ModifyBy })
                .ExecuteCommandAsync();

            _logger.LogInformation("Deleted stage condition {ConditionId}", id);

            return deleteResult > 0;
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

            // Check for conflicting rules in frontend format
            try
            {
                var jsonObj = Newtonsoft.Json.Linq.JToken.Parse(rulesJson);
                if (jsonObj is Newtonsoft.Json.Linq.JObject jObject && jObject.ContainsKey("logic"))
                {
                    var frontendRules = JsonConvert.DeserializeObject<FrontendRuleConfig>(rulesJson);
                    if (frontendRules != null && frontendRules.Rules != null)
                    {
                        // Check for conflicting rules (same field with contradictory conditions under AND logic)
                        var conflictWarnings = DetectConflictingRules(frontendRules);
                        result.Warnings.AddRange(conflictWarnings);
                    }
                }
            }
            catch
            {
                // Ignore errors in conflict detection - it's just a warning
            }

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Detect conflicting rules that can never be satisfied together
        /// For example: field > 10 AND field < 10 (impossible)
        /// </summary>
        private List<ValidationWarning> DetectConflictingRules(FrontendRuleConfig config)
        {
            var warnings = new List<ValidationWarning>();
            
            if (config.Logic?.ToUpper() != "AND" || config.Rules == null || config.Rules.Count < 2)
            {
                return warnings; // Only check AND logic with multiple rules
            }

            // Group rules by fieldPath
            var rulesByField = config.Rules
                .Where(r => !string.IsNullOrEmpty(r.FieldPath))
                .GroupBy(r => r.FieldPath)
                .Where(g => g.Count() > 1);

            foreach (var fieldGroup in rulesByField)
            {
                var rules = fieldGroup.ToList();
                
                // Check for contradictory comparison operators on the same field
                for (int i = 0; i < rules.Count; i++)
                {
                    for (int j = i + 1; j < rules.Count; j++)
                    {
                        var rule1 = rules[i];
                        var rule2 = rules[j];
                        
                        var conflict = DetectOperatorConflict(rule1, rule2);
                        if (conflict != null)
                        {
                            warnings.Add(new ValidationWarning
                            {
                                Code = "CONFLICTING_RULES",
                                Message = conflict
                            });
                        }
                    }
                }
            }

            return warnings;
        }

        /// <summary>
        /// Detect if two rules on the same field have contradictory operators
        /// </summary>
        private string DetectOperatorConflict(FrontendRule rule1, FrontendRule rule2)
        {
            var op1 = rule1.Operator?.ToLower();
            var op2 = rule2.Operator?.ToLower();
            var val1 = rule1.Value?.ToString();
            var val2 = rule2.Value?.ToString();

            // Try to parse as numbers for comparison
            var isNum1 = double.TryParse(val1, out var num1);
            var isNum2 = double.TryParse(val2, out var num2);

            // Check for impossible conditions
            // Case 1: > X AND < X (or >= X AND <= Y where Y < X)
            if ((op1 == ">" || op1 == "gt") && (op2 == "<" || op2 == "lt"))
            {
                if (isNum1 && isNum2 && num1 >= num2)
                {
                    return $"Conflicting rules: '{rule1.FieldPath}' > {val1} AND < {val2} can never be satisfied (impossible range)";
                }
            }
            if ((op1 == "<" || op1 == "lt") && (op2 == ">" || op2 == "gt"))
            {
                if (isNum1 && isNum2 && num1 <= num2)
                {
                    return $"Conflicting rules: '{rule1.FieldPath}' < {val1} AND > {val2} can never be satisfied (impossible range)";
                }
            }

            // Case 2: >= X AND <= Y where X > Y
            if ((op1 == ">=" || op1 == "gte") && (op2 == "<=" || op2 == "lte"))
            {
                if (isNum1 && isNum2 && num1 > num2)
                {
                    return $"Conflicting rules: '{rule1.FieldPath}' >= {val1} AND <= {val2} can never be satisfied (impossible range)";
                }
            }
            if ((op1 == "<=" || op1 == "lte") && (op2 == ">=" || op2 == "gte"))
            {
                if (isNum1 && isNum2 && num1 < num2)
                {
                    return $"Conflicting rules: '{rule1.FieldPath}' <= {val1} AND >= {val2} can never be satisfied (impossible range)";
                }
            }

            // Case 3: == X AND == Y where X != Y
            if ((op1 == "==" || op1 == "equals" || op1 == "eq") && 
                (op2 == "==" || op2 == "equals" || op2 == "eq"))
            {
                if (val1 != val2)
                {
                    return $"Conflicting rules: '{rule1.FieldPath}' == '{val1}' AND == '{val2}' can never be satisfied (different values)";
                }
            }

            // Case 4: == X AND != X
            if ((op1 == "==" || op1 == "equals" || op1 == "eq") && 
                (op2 == "!=" || op2 == "notequals" || op2 == "ne"))
            {
                if (val1 == val2)
                {
                    return $"Conflicting rules: '{rule1.FieldPath}' == '{val1}' AND != '{val2}' can never be satisfied";
                }
            }
            if ((op1 == "!=" || op1 == "notequals" || op1 == "ne") && 
                (op2 == "==" || op2 == "equals" || op2 == "eq"))
            {
                if (val1 == val2)
                {
                    return $"Conflicting rules: '{rule1.FieldPath}' != '{val1}' AND == '{val2}' can never be satisfied";
                }
            }

            // Case 5: isNull AND isNotNull
            if ((op1 == "isnull" && op2 == "isnotnull") || (op1 == "isnotnull" && op2 == "isnull"))
            {
                return $"Conflicting rules: '{rule1.FieldPath}' isNull AND isNotNull can never be satisfied";
            }

            // Case 6: isEmpty AND isNotEmpty
            if ((op1 == "isempty" && op2 == "isnotempty") || (op1 == "isnotempty" && op2 == "isempty"))
            {
                return $"Conflicting rules: '{rule1.FieldPath}' isEmpty AND isNotEmpty can never be satisfied";
            }

            return null;
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

                // Check for conflicting actions (multiple GoToStage, SkipStage, or EndWorkflow)
                var stageControlActions = actions.Where(a => 
                    a.Type?.ToLower() == "gotostage" || 
                    a.Type?.ToLower() == "skipstage" || 
                    a.Type?.ToLower() == "endworkflow").ToList();
                
                if (stageControlActions.Count > 1)
                {
                    // Multiple stage control actions - this is a conflict
                    var actionTypes = stageControlActions.Select(a => a.Type).Distinct().ToList();
                    var actionDetails = stageControlActions.Select(a => 
                    {
                        if (a.Type?.ToLower() == "gotostage" && a.TargetStageId.HasValue)
                            return $"{a.Type}(targetStageId={a.TargetStageId})";
                        else if (a.Type?.ToLower() == "skipstage")
                            return $"{a.Type}(skipCount={a.SkipCount})";
                        else
                            return a.Type;
                    });
                    
                    result.Warnings.Add(new ValidationWarning 
                    { 
                        Code = "CONFLICTING_STAGE_ACTIONS", 
                        Message = $"Multiple stage control actions detected: [{string.Join(", ", actionDetails)}]. Only the first action will take effect for stage navigation." 
                    });
                }

                // Check for multiple GoToStage with different targets
                var goToStageActions = actions.Where(a => a.Type?.ToLower() == "gotostage" && a.TargetStageId.HasValue).ToList();
                if (goToStageActions.Count > 1)
                {
                    var targetStageIds = goToStageActions.Select(a => a.TargetStageId!.Value).Distinct().ToList();
                    if (targetStageIds.Count > 1)
                    {
                        result.Warnings.Add(new ValidationWarning 
                        { 
                            Code = "MULTIPLE_GOTOSTAGE_TARGETS", 
                            Message = $"Multiple GoToStage actions with different targets: [{string.Join(", ", targetStageIds)}]. Only the first GoToStage action will be executed." 
                        });
                    }
                }

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
