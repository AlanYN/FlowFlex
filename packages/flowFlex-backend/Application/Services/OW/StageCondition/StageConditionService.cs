using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Helpers;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.Domain.Shared.Helpers;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Enums.Permission;
using FlowFlex.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RulesEngine.Models;
using SqlSugar;
using OwOperationTypeEnum = FlowFlex.Domain.Shared.Enums.OW.OperationTypeEnum;
using OwBusinessModuleEnum = FlowFlex.Domain.Shared.Enums.OW.BusinessModuleEnum;
using StageConditionEntity = FlowFlex.Domain.Entities.OW.StageCondition;

namespace FlowFlex.Application.Services.OW
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
        private readonly IOperationChangeLogService _operationChangeLogService;
        private readonly IMapper _mapper;
        private readonly UserContext _userContext;
        private readonly ILogger<StageConditionService> _logger;

        public StageConditionService(
            ISqlSugarClient db,
            IStageRepository stageRepository,
            IWorkflowRepository workflowRepository,
            IPermissionService permissionService,
            IOperationChangeLogService operationChangeLogService,
            IMapper mapper,
            UserContext userContext,
            ILogger<StageConditionService> logger)
        {
            _db = db;
            _stageRepository = stageRepository;
            _workflowRepository = workflowRepository;
            _permissionService = permissionService;
            _operationChangeLogService = operationChangeLogService;
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
                throw new CRMException(ErrorCodeEnum.NotFound, "The specified stage does not exist.");
            }

            // Validate one condition per stage
            var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
            var existingCondition = await _db.Queryable<StageConditionEntity>()
                .Where(c => c.StageId == input.StageId && c.IsValid)
                .Where(c => c.TenantId == tenantId)
                .FirstAsync();

            if (existingCondition != null)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, 
                    $"Stage \"{stage.Name}\" already has a condition configured. Each stage can only have one condition.");
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
            var entity = new StageConditionEntity
            {
                StageId = input.StageId,
                WorkflowId = input.WorkflowId > 0 ? input.WorkflowId : stage.WorkflowId,
                Name = input.Name,
                Description = input.Description ?? string.Empty,
                RulesJson = input.RulesJson,
                ActionsJson = input.ActionsJson,
                FallbackStageId = input.FallbackStageId,
                IsActive = input.IsActive,
                Status = StageConditionConstants.StatusValid
            };

            // Initialize entity fields
            entity.InitNewId();
            entity.TenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
            entity.AppCode = TenantContextHelper.GetAppCodeOrDefault(_userContext);
            entity.CreateDate = DateTimeOffset.UtcNow;
            entity.ModifyDate = DateTimeOffset.UtcNow;
            entity.CreateBy = _userContext.UserName ?? StageConditionConstants.SystemUser;
            entity.ModifyBy = _userContext.UserName ?? StageConditionConstants.SystemUser;
            if (long.TryParse(_userContext.UserId, out var userId))
            {
                entity.CreateUserId = userId;
                entity.ModifyUserId = userId;
            }
            entity.IsValid = true;

            await _db.Insertable(entity).ExecuteCommandAsync();

            _logger.LogInformation("Created stage condition {ConditionId} for stage {StageId}", entity.Id, input.StageId);

            // Log to workflow change log
            try
            {
                var workflow = await _workflowRepository.GetByIdAsync(entity.WorkflowId);
                var workflowName = workflow?.Name ?? "Unknown Workflow";

                var afterData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    ConditionId = entity.Id,
                    ConditionName = entity.Name,
                    StageId = entity.StageId,
                    StageName = stage.Name,
                    Description = entity.Description,
                    IsActive = entity.IsActive,
                    RulesJson = entity.RulesJson,
                    ActionsJson = entity.ActionsJson,
                    FallbackStageId = entity.FallbackStageId
                });

                var extendedData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    WorkflowId = entity.WorkflowId,
                    WorkflowName = workflowName,
                    StageId = entity.StageId,
                    StageName = stage.Name,
                    ConditionId = entity.Id,
                    ConditionName = entity.Name,
                    CreatedAt = entity.CreateDate.ToString("MM/dd/yyyy hh:mm tt")
                });

                await _operationChangeLogService.LogOperationAsync(
                    operationType: OwOperationTypeEnum.StageConditionCreate,
                    businessModule: OwBusinessModuleEnum.Workflow,
                    businessId: entity.WorkflowId,
                    onboardingId: null,
                    stageId: entity.StageId,
                    operationTitle: $"Stage Condition Created: {entity.Name}",
                    operationDescription: $"Stage condition '{entity.Name}' has been created for stage '{stage.Name}' in workflow '{workflowName}' by {_userContext.UserName}.",
                    beforeData: null,
                    afterData: afterData,
                    changedFields: new List<string> { "Name", "Description", "RulesJson", "ActionsJson", "IsActive", "FallbackStageId" },
                    extendedData: extendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log stage condition create operation for condition {ConditionId}", entity.Id);
            }

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

            // Capture original values for change log
            var originalName = condition.Name;
            var originalDescription = condition.Description;
            var originalRulesJson = condition.RulesJson;
            var originalActionsJson = condition.ActionsJson;
            var originalFallbackStageId = condition.FallbackStageId;
            var originalIsActive = condition.IsActive;

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
            condition.Status = StageConditionConstants.StatusValid;
            condition.ModifyDate = DateTimeOffset.UtcNow;
            condition.ModifyBy = _userContext.UserName ?? StageConditionConstants.SystemUser;

            var updateResult = await _db.Updateable(condition).ExecuteCommandAsync();

            _logger.LogInformation("Updated stage condition {ConditionId}", id);

            // Log to workflow change log
            if (updateResult > 0)
            {
                try
                {
                    var workflow = await _workflowRepository.GetByIdAsync(condition.WorkflowId);
                    var workflowName = workflow?.Name ?? "Unknown Workflow";
                    var stage = await _stageRepository.GetByIdAsync(condition.StageId);
                    var stageName = stage?.Name ?? "Unknown Stage";

                    // Determine changed fields
                    var changedFields = new List<string>();
                    var changeDescriptions = new List<string>();

                    if (originalName != condition.Name)
                    {
                        changedFields.Add("Name");
                        changeDescriptions.Add($"Name from '{originalName}' to '{condition.Name}'");
                    }
                    if (originalDescription != condition.Description)
                    {
                        changedFields.Add("Description");
                        changeDescriptions.Add($"Description updated");
                    }
                    // Use semantic JSON comparison for RulesJson
                    if (!AreJsonSemanticallyEqual(originalRulesJson, condition.RulesJson))
                    {
                        changedFields.Add("RulesJson");
                        var rulesChangeDetail = GetRulesChangeDescription(originalRulesJson, condition.RulesJson);
                        changeDescriptions.Add(rulesChangeDetail);
                    }
                    // Use semantic JSON comparison for ActionsJson
                    if (!AreJsonSemanticallyEqual(originalActionsJson, condition.ActionsJson))
                    {
                        changedFields.Add("ActionsJson");
                        var actionsChangeDetail = GetActionsChangeDescription(originalActionsJson, condition.ActionsJson);
                        changeDescriptions.Add(actionsChangeDetail);
                    }
                    if (originalFallbackStageId != condition.FallbackStageId)
                    {
                        changedFields.Add("FallbackStageId");
                        changeDescriptions.Add($"Fallback stage updated");
                    }
                    if (originalIsActive != condition.IsActive)
                    {
                        changedFields.Add("IsActive");
                        changeDescriptions.Add($"IsActive from '{originalIsActive}' to '{condition.IsActive}'");
                    }

                    if (changedFields.Count > 0)
                    {
                        var beforeData = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            ConditionId = condition.Id,
                            ConditionName = originalName,
                            StageId = condition.StageId,
                            StageName = stageName,
                            Description = originalDescription,
                            IsActive = originalIsActive,
                            RulesJson = originalRulesJson,
                            ActionsJson = originalActionsJson,
                            FallbackStageId = originalFallbackStageId
                        });

                        var afterData = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            ConditionId = condition.Id,
                            ConditionName = condition.Name,
                            StageId = condition.StageId,
                            StageName = stageName,
                            Description = condition.Description,
                            IsActive = condition.IsActive,
                            RulesJson = condition.RulesJson,
                            ActionsJson = condition.ActionsJson,
                            FallbackStageId = condition.FallbackStageId
                        });

                        var extendedData = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            WorkflowId = condition.WorkflowId,
                            WorkflowName = workflowName,
                            StageId = condition.StageId,
                            StageName = stageName,
                            ConditionId = condition.Id,
                            ConditionName = condition.Name,
                            ChangedFieldsCount = changedFields.Count,
                            UpdatedAt = condition.ModifyDate.ToString("MM/dd/yyyy hh:mm tt")
                        });

                        var changeDescription = string.Join(". ", changeDescriptions);
                        await _operationChangeLogService.LogOperationAsync(
                            operationType: OwOperationTypeEnum.StageConditionUpdate,
                            businessModule: OwBusinessModuleEnum.Workflow,
                            businessId: condition.WorkflowId,
                            onboardingId: null,
                            stageId: condition.StageId,
                            operationTitle: $"Stage Condition Updated: {condition.Name}",
                            operationDescription: $"Stage condition '{condition.Name}' has been updated in workflow '{workflowName}' by {_userContext.UserName}. Changes: {changeDescription}",
                            beforeData: beforeData,
                            afterData: afterData,
                            changedFields: changedFields,
                            extendedData: extendedData
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to log stage condition update operation for condition {ConditionId}", id);
                }
            }

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

            // Capture condition info for change log before deletion
            var conditionName = condition.Name;
            var conditionDescription = condition.Description;
            var workflowId = condition.WorkflowId;
            var stageId = condition.StageId;

            // Validate permission
            await ValidateWorkflowPermissionAsync(condition.WorkflowId, OperationTypeEnum.Operate);

            // Soft delete
            condition.IsValid = false;
            condition.ModifyDate = DateTimeOffset.UtcNow;
            condition.ModifyBy = _userContext.UserName ?? StageConditionConstants.SystemUser;

            var deleteResult = await _db.Updateable(condition)
                .UpdateColumns(c => new { c.IsValid, c.ModifyDate, c.ModifyBy })
                .ExecuteCommandAsync();

            _logger.LogInformation("Deleted stage condition {ConditionId}", id);

            // Log to workflow change log
            if (deleteResult > 0)
            {
                try
                {
                    var workflow = await _workflowRepository.GetByIdAsync(workflowId);
                    var workflowName = workflow?.Name ?? "Unknown Workflow";
                    var stage = await _stageRepository.GetByIdAsync(stageId);
                    var stageName = stage?.Name ?? "Unknown Stage";

                    var beforeData = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        ConditionId = id,
                        ConditionName = conditionName,
                        StageId = stageId,
                        StageName = stageName,
                        Description = conditionDescription,
                        IsActive = condition.IsActive,
                        RulesJson = condition.RulesJson,
                        ActionsJson = condition.ActionsJson,
                        FallbackStageId = condition.FallbackStageId
                    });

                    var extendedData = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        WorkflowId = workflowId,
                        WorkflowName = workflowName,
                        StageId = stageId,
                        StageName = stageName,
                        ConditionId = id,
                        ConditionName = conditionName,
                        DeletedAt = condition.ModifyDate.ToString("MM/dd/yyyy hh:mm tt")
                    });

                    await _operationChangeLogService.LogOperationAsync(
                        operationType: OwOperationTypeEnum.StageConditionDelete,
                        businessModule: OwBusinessModuleEnum.Workflow,
                        businessId: workflowId,
                        onboardingId: null,
                        stageId: stageId,
                        operationTitle: $"Stage Condition Deleted: {conditionName}",
                        operationDescription: $"Stage condition '{conditionName}' has been deleted from stage '{stageName}' in workflow '{workflowName}' by {_userContext.UserName}.",
                        beforeData: beforeData,
                        afterData: null,
                        changedFields: new List<string> { "IsValid" },
                        extendedData: extendedData
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to log stage condition delete operation for condition {ConditionId}", id);
                }
            }

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
            var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
            var entity = await _db.Queryable<StageConditionEntity>()
                .Where(c => c.StageId == stageId && c.IsValid)
                .Where(c => c.TenantId == tenantId)
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

            var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
            var entities = await _db.Queryable<StageConditionEntity>()
                .Where(c => c.WorkflowId == workflowId && c.IsValid)
                .Where(c => c.TenantId == tenantId)
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
                result.Errors.Add(new ValidationError { Code = StageConditionConstants.ErrorCodeRulesRequired, Message = "RulesJson is required" });
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
                    result.Warnings.Add(new ValidationWarning { Code = StageConditionConstants.WarningCodeFormatConverted, Message = "Frontend rule format detected and converted to RulesEngine format" });
                }
                else if (jsonObj is Newtonsoft.Json.Linq.JArray)
                {
                    // Standard RulesEngine format
                    workflows = JsonConvert.DeserializeObject<RulesEngine.Models.Workflow[]>(rulesJson);
                }
                else
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError { Code = StageConditionConstants.ErrorCodeInvalidFormat, Message = "RulesJson must be either a RulesEngine workflow array or frontend rule format with 'logic' property" });
                    return result;
                }
                
                if (workflows == null || workflows.Length == 0)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError { Code = StageConditionConstants.ErrorCodeRulesEmpty, Message = "RulesJson must contain at least one workflow" });
                    return result;
                }

                foreach (var workflow in workflows)
                {
                    if (string.IsNullOrEmpty(workflow.WorkflowName))
                    {
                        result.Warnings.Add(new ValidationWarning { Code = StageConditionConstants.WarningCodeWorkflowNameEmpty, Message = $"Workflow name is empty, will use default name '{StageConditionConstants.DefaultWorkflowName}'" });
                    }

                    if (workflow.Rules == null || !workflow.Rules.Any())
                    {
                        result.IsValid = false;
                        result.Errors.Add(new ValidationError { Code = StageConditionConstants.ErrorCodeRulesEmpty, Message = $"Workflow '{workflow.WorkflowName}' must contain at least one rule" });
                    }
                    else
                    {
                        foreach (var rule in workflow.Rules)
                        {
                            if (string.IsNullOrEmpty(rule.RuleName))
                            {
                                result.IsValid = false;
                                result.Errors.Add(new ValidationError { Code = StageConditionConstants.ErrorCodeRuleNameRequired, Message = "Rule name is required" });
                            }

                            if (string.IsNullOrEmpty(rule.Expression))
                            {
                                result.IsValid = false;
                                result.Errors.Add(new ValidationError { Code = StageConditionConstants.ErrorCodeRuleExpressionRequired, Message = $"Rule '{rule.RuleName}' must have an expression" });
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
                    result.Errors.Add(new ValidationError { Code = StageConditionConstants.ErrorCodeInvalidExpression, Message = $"Invalid rule expression: {ex.Message}" });
                }
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError { Code = StageConditionConstants.ErrorCodeInvalidJson, Message = $"Invalid JSON format: {ex.Message}" });
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
                throw new Newtonsoft.Json.JsonException($"Failed to convert frontend rules format: {ex.Message}");
            }
        }

        /// <summary>
        /// Build RulesEngine expression from frontend rule with security validation
        /// </summary>
        private string BuildExpressionFromFrontendRule(FrontendRule rule)
        {
            if (string.IsNullOrEmpty(rule.FieldPath))
            {
                return null;
            }

            // Validate field path for security
            var fieldPathValidation = ExpressionValidator.ValidateFieldPath(rule.FieldPath);
            if (!fieldPathValidation.IsValid)
            {
                _logger.LogWarning("Invalid field path rejected: {FieldPath}. Reason: {Reason}", 
                    rule.FieldPath, fieldPathValidation.ErrorMessage);
                return null;
            }

            // Validate and sanitize the value
            var valueValidation = ExpressionValidator.ValidateValue(rule.Value);
            if (!valueValidation.IsValid)
            {
                _logger.LogWarning("Invalid value rejected for field {FieldPath}. Reason: {Reason}", 
                    rule.FieldPath, valueValidation.ErrorMessage);
                return null;
            }
            var valueStr = valueValidation.SanitizedValue;

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

        #endregion

        #region Private Methods

        /// <summary>
        /// Get entity by ID with tenant isolation
        /// </summary>
        private async Task<StageConditionEntity?> GetEntityByIdAsync(long id)
        {
            var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
            return await _db.Queryable<StageConditionEntity>()
                .Where(c => c.Id == id && c.IsValid)
                .Where(c => c.TenantId == tenantId)
                .FirstAsync();
        }

        /// <summary>
        /// Validate workflow permission
        /// </summary>
        private async Task ValidateWorkflowPermissionAsync(long workflowId, OperationTypeEnum operationType)
        {
            if (string.IsNullOrEmpty(_userContext?.UserId) || !long.TryParse(_userContext.UserId, out var userId))
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
            return ActionValidationHelper.ValidateActionsJson(actionsJson);
        }

        /// <summary>
        /// Check if parameters dictionary has a non-empty array for the given key
        /// </summary>
        private bool HasNonEmptyArray(Dictionary<string, object> parameters, string key)
        {
            return ActionValidationHelper.HasNonEmptyArray(parameters, key);
        }

        /// <summary>
        /// Validate referenced stages exist (batch query optimized)
        /// </summary>
        private async Task ValidateReferencedStagesAsync(StageConditionEntity condition, ConditionValidationResult result)
        {
            // Collect all stage IDs to validate
            var stageIdsToValidate = new HashSet<long>();
            
            // Add fallback stage ID
            if (condition.FallbackStageId.HasValue)
            {
                stageIdsToValidate.Add(condition.FallbackStageId.Value);
            }
            
            // Add target stage IDs from actions
            try
            {
                var actions = JsonConvert.DeserializeObject<List<ConditionAction>>(condition.ActionsJson);
                if (actions != null)
                {
                    foreach (var action in actions.Where(a => a.TargetStageId.HasValue))
                    {
                        stageIdsToValidate.Add(action.TargetStageId!.Value);
                    }
                }
            }
            catch
            {
                // Already validated in ValidateActionsJson
            }
            
            if (!stageIdsToValidate.Any())
            {
                return;
            }
            
            // Batch query all stages at once
            var stageIdList = stageIdsToValidate.ToList();
            var stages = await _db.Queryable<Stage>()
                .Where(s => stageIdList.Contains(s.Id) && s.IsValid)
                .Select(s => new { s.Id, s.IsActive })
                .ToListAsync();
            
            var stageDict = stages.ToDictionary(s => s.Id, s => s.IsActive);
            
            // Validate fallback stage
            if (condition.FallbackStageId.HasValue)
            {
                if (!stageDict.TryGetValue(condition.FallbackStageId.Value, out var isActive) || !isActive)
                {
                    result.Warnings.Add(new ValidationWarning { Code = "FALLBACK_STAGE_INVALID", Message = $"Fallback stage {condition.FallbackStageId} not found or inactive" });
                }
            }
            
            // Validate target stages in actions
            try
            {
                var actions = JsonConvert.DeserializeObject<List<ConditionAction>>(condition.ActionsJson);
                if (actions != null)
                {
                    foreach (var action in actions.Where(a => a.TargetStageId.HasValue))
                    {
                        if (!stageDict.TryGetValue(action.TargetStageId!.Value, out var isActive) || !isActive)
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
        /// Validate referenced action definitions exist (batch query optimized)
        /// </summary>
        private async Task ValidateReferencedActionsAsync(StageConditionEntity condition, ConditionValidationResult result)
        {
            try
            {
                var actions = JsonConvert.DeserializeObject<List<ConditionAction>>(condition.ActionsJson);
                if (actions == null)
                {
                    return;
                }
                
                // Collect all action definition IDs
                var actionDefIds = actions
                    .Where(a => a.ActionDefinitionId.HasValue)
                    .Select(a => a.ActionDefinitionId!.Value)
                    .Distinct()
                    .ToList();
                
                if (!actionDefIds.Any())
                {
                    return;
                }
                
                // Batch query all action definitions at once
                var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
                var actionDefs = await _db.Queryable<Domain.Entities.Action.ActionDefinition>()
                    .Where(a => actionDefIds.Contains(a.Id) && a.IsValid)
                    .Where(a => a.TenantId == tenantId)
                    .Select(a => new { a.Id, a.IsEnabled })
                    .ToListAsync();
                
                var actionDefDict = actionDefs.ToDictionary(a => a.Id, a => a.IsEnabled);
                
                // Validate each action definition
                foreach (var actionDefId in actionDefIds)
                {
                    if (!actionDefDict.TryGetValue(actionDefId, out var isEnabled) || !isEnabled)
                    {
                        result.Warnings.Add(new ValidationWarning { Code = "ACTION_DEF_INVALID", Message = $"ActionDefinition {actionDefId} not found or disabled" });
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
        private async Task CheckCircularReferencesAsync(StageConditionEntity condition, ConditionValidationResult result)
        {
            try
            {
                var actions = JsonConvert.DeserializeObject<List<ConditionAction>>(condition.ActionsJson);
                if (actions == null) return;

                var goToActions = actions.Where(a => a.Type?.ToLower() == "gotostage" && a.TargetStageId.HasValue).ToList();
                
                foreach (var action in goToActions)
                {
                    // Check if target stage has a condition that points back
                    var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
                    var targetCondition = await _db.Queryable<StageConditionEntity>()
                        .Where(c => c.StageId == action.TargetStageId!.Value && c.IsValid && c.IsActive)
                        .Where(c => c.TenantId == tenantId)
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
        /// Compare two JSON strings semantically (ignoring formatting differences like whitespace)
        /// Returns true if they represent the same JSON structure and values
        /// </summary>
        private bool AreJsonSemanticallyEqual(string json1, string json2)
        {
            // Handle null/empty cases
            if (string.IsNullOrWhiteSpace(json1) && string.IsNullOrWhiteSpace(json2))
                return true;
            if (string.IsNullOrWhiteSpace(json1) || string.IsNullOrWhiteSpace(json2))
                return false;

            try
            {
                var token1 = Newtonsoft.Json.Linq.JToken.Parse(json1);
                var token2 = Newtonsoft.Json.Linq.JToken.Parse(json2);
                return Newtonsoft.Json.Linq.JToken.DeepEquals(token1, token2);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse JSON for semantic comparison, falling back to string comparison");
                // Fallback to string comparison if JSON parsing fails
                return json1 == json2;
            }
        }

        /// <summary>
        /// Get detailed description of rules changes
        /// </summary>
        private string GetRulesChangeDescription(string originalRulesJson, string newRulesJson)
        {
            try
            {
                var originalRules = ParseFrontendRules(originalRulesJson);
                var newRules = ParseFrontendRules(newRulesJson);

                if (originalRules == null && newRules == null)
                    return "Rules configuration updated";

                var changes = new List<string>();

                // Compare logic
                var originalLogic = originalRules?.Logic ?? "AND";
                var newLogic = newRules?.Logic ?? "AND";
                if (!string.Equals(originalLogic, newLogic, StringComparison.OrdinalIgnoreCase))
                {
                    changes.Add($"Logic changed from '{originalLogic}' to '{newLogic}'");
                }

                // Compare rule counts
                var originalCount = originalRules?.Rules?.Count ?? 0;
                var newCount = newRules?.Rules?.Count ?? 0;

                if (originalCount != newCount)
                {
                    if (newCount > originalCount)
                        changes.Add($"Added {newCount - originalCount} rule(s) (total: {newCount})");
                    else
                        changes.Add($"Removed {originalCount - newCount} rule(s) (total: {newCount})");
                }

                // Compare individual rules for modifications
                if (originalRules?.Rules != null && newRules?.Rules != null)
                {
                    var modifiedRules = 0;
                    var minCount = Math.Min(originalCount, newCount);
                    for (int i = 0; i < minCount; i++)
                    {
                        var origRule = originalRules.Rules[i];
                        var newRule = newRules.Rules[i];
                        if (origRule.FieldPath != newRule.FieldPath ||
                            origRule.Operator != newRule.Operator ||
                            origRule.Value?.ToString() != newRule.Value?.ToString())
                        {
                            modifiedRules++;
                        }
                    }
                    if (modifiedRules > 0)
                    {
                        changes.Add($"Modified {modifiedRules} existing rule(s)");
                    }
                }

                return changes.Count > 0 
                    ? $"Rules: {string.Join(", ", changes)}" 
                    : "Rules configuration updated";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse rules for change description");
                return "Rules configuration updated";
            }
        }

        /// <summary>
        /// Get detailed description of actions changes
        /// </summary>
        private string GetActionsChangeDescription(string originalActionsJson, string newActionsJson)
        {
            try
            {
                var originalActions = ParseActions(originalActionsJson);
                var newActions = ParseActions(newActionsJson);

                var changes = new List<string>();

                var originalCount = originalActions?.Count ?? 0;
                var newCount = newActions?.Count ?? 0;

                if (originalCount != newCount)
                {
                    if (newCount > originalCount)
                        changes.Add($"Added {newCount - originalCount} action(s) (total: {newCount})");
                    else
                        changes.Add($"Removed {originalCount - newCount} action(s) (total: {newCount})");
                }

                // Describe action types
                if (newActions != null && newActions.Count > 0)
                {
                    var actionTypes = newActions
                        .Where(a => !string.IsNullOrEmpty(a.Type))
                        .GroupBy(a => a.Type)
                        .Select(g => $"{g.Count()} {g.Key}")
                        .ToList();
                    
                    if (actionTypes.Count > 0)
                    {
                        changes.Add($"Current actions: {string.Join(", ", actionTypes)}");
                    }
                }

                // Compare individual actions for modifications
                if (originalActions != null && newActions != null)
                {
                    var modifiedActions = 0;
                    var minCount = Math.Min(originalCount, newCount);
                    for (int i = 0; i < minCount; i++)
                    {
                        var origAction = originalActions[i];
                        var newAction = newActions[i];
                        if (origAction.Type != newAction.Type ||
                            origAction.TargetStageId != newAction.TargetStageId)
                        {
                            modifiedActions++;
                        }
                    }
                    if (modifiedActions > 0)
                    {
                        changes.Add($"Modified {modifiedActions} existing action(s)");
                    }
                }

                return changes.Count > 0 
                    ? $"Actions: {string.Join(", ", changes)}" 
                    : "Actions configuration updated";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse actions for change description");
                return "Actions configuration updated";
            }
        }

        /// <summary>
        /// Parse frontend rules JSON format
        /// </summary>
        private FrontendRuleConfig ParseFrontendRules(string rulesJson)
        {
            if (string.IsNullOrWhiteSpace(rulesJson))
                return null;

            try
            {
                var jsonObj = Newtonsoft.Json.Linq.JToken.Parse(rulesJson);
                if (jsonObj is Newtonsoft.Json.Linq.JObject jObject && jObject.ContainsKey("logic"))
                {
                    return JsonConvert.DeserializeObject<FrontendRuleConfig>(rulesJson);
                }
            }
            catch
            {
                // Ignore parsing errors
            }
            return null;
        }

        /// <summary>
        /// Parse actions JSON
        /// </summary>
        private List<ConditionActionDto> ParseActions(string actionsJson)
        {
            if (string.IsNullOrWhiteSpace(actionsJson))
                return null;

            try
            {
                return JsonConvert.DeserializeObject<List<ConditionActionDto>>(actionsJson);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Map entity to output DTO
        /// </summary>
        private StageConditionOutputDto MapToOutputDto(StageConditionEntity entity)
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



