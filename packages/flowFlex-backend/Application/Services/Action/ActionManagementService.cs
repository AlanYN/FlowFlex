using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Domain.Entities.Action;
using FlowFlex.Domain.Repository.Action;
using FlowFlex.Domain.Shared.Enums.Action;
using FlowFlex.Domain.Shared.Enums.OW;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using FlowFlex.Domain.Shared.Models;
using Application.Contracts.IServices.Action;
using Item.Excel.Lib;
using System.Text.RegularExpressions;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Entities.OW;
using System.Text.Json;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Application.Contracts.IServices.OW.ChangeLog;

namespace FlowFlex.Application.Services.Action
{
    /// <summary>
    /// Service for managing action definitions and trigger mappings
    /// </summary>
    public class ActionManagementService : IActionManagementService
    {
        private readonly IActionDefinitionRepository _actionDefinitionRepository;
        private readonly IActionTriggerMappingRepository _actionTriggerMappingRepository;
        private readonly IActionCodeGeneratorService _actionCodeGeneratorService;
        private readonly IChecklistTaskRepository _checklistTaskRepository;
        private readonly IQuestionnaireRepository _questionnaireRepository;
        private readonly IStageRepository _stageRepository;
        private readonly IActionLogService _actionLogService;
        private readonly IMapper _mapper;
        private readonly ILogger<ActionManagementService> _logger;
        private readonly UserContext _userContext;

        public ActionManagementService(
            IActionDefinitionRepository actionDefinitionRepository,
            IActionTriggerMappingRepository actionTriggerMappingRepository,
            IActionCodeGeneratorService actionCodeGeneratorService,
            IChecklistTaskRepository checklistTaskRepository,
            IQuestionnaireRepository questionnaireRepository,
            IStageRepository stageRepository,
            IActionLogService actionLogService,
            IMapper mapper,
            UserContext userContext,
            ILogger<ActionManagementService> logger)
        {
            _actionDefinitionRepository = actionDefinitionRepository;
            _actionTriggerMappingRepository = actionTriggerMappingRepository;
            _actionCodeGeneratorService = actionCodeGeneratorService;
            _checklistTaskRepository = checklistTaskRepository;
            _questionnaireRepository = questionnaireRepository;
            _stageRepository = stageRepository;
            _actionLogService = actionLogService;
            _mapper = mapper;
            _userContext = userContext;
            _logger = logger;
        }

        #region Action Definition Management

        public async Task<ActionDefinitionDto> GetActionDefinitionAsync(long id)
        {
            var entity = await _actionDefinitionRepository.GetByIdAsync(id);
            if (entity == null) return null;

            var actionDto = _mapper.Map<ActionDefinitionDto>(entity);

            // Get trigger mapping information
            var triggerMappings = await _actionDefinitionRepository.GetTriggerMappingsWithDetailsByActionIdsAsync(new List<long> { id });

            // Add trigger mapping information to DTO
            actionDto.TriggerMappings = _mapper.Map<List<ActionTriggerMappingInfo>>(triggerMappings);

            return actionDto;
        }

        public async Task<PageModelDto<ActionDefinitionDto>> GetPagedActionDefinitionsAsync(string? search,
            ActionTypeEnum? actionType,
            int pageIndex,
            int pageSize,
            bool? isAssignmentStage = null,
            bool? isAssignmentChecklist = null,
            bool? isAssignmentQuestionnaire = null,
            bool? isAssignmentWorkflow = null,
            bool? isTools = null)
        {
            var (data, total) = await _actionDefinitionRepository.GetPagedAsync(pageIndex,
                pageSize,
                actionType.ToString(),
                search,
                isAssignmentStage,
                isAssignmentChecklist,
                isAssignmentQuestionnaire,
                isAssignmentWorkflow,
                isTools);

            // Get ActionDefinition DTO list
            var actionDtos = _mapper.Map<List<ActionDefinitionDto>>(data);

            // If there is data, get trigger mapping information
            if (actionDtos.Count != 0)
            {
                var actionIds = actionDtos.Select(dto => dto.Id).ToList();
                var triggerMappings = await _actionDefinitionRepository.GetTriggerMappingsWithDetailsByActionIdsAsync(actionIds);

                // Group trigger mappings by ActionDefinitionId
                var mappingsByActionId = triggerMappings.GroupBy(m => m.ActionDefinitionId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Add trigger mapping information to each ActionDefinitionDto
                foreach (var actionDto in actionDtos)
                {
                    if (mappingsByActionId.TryGetValue(actionDto.Id, out var mappings))
                    {
                        actionDto.TriggerMappings = _mapper.Map<List<ActionTriggerMappingInfo>>(mappings);
                    }
                }
            }

            return new PageModelDto<ActionDefinitionDto>(pageIndex, pageSize, actionDtos, total);
        }

        public async Task<List<ActionDefinitionDto>> GetEnabledActionDefinitionsAsync()
        {
            var entities = await _actionDefinitionRepository.GetAllEnabledAsync();
            return _mapper.Map<List<ActionDefinitionDto>>(entities);
        }

        public async Task<ActionDefinitionDto> CreateActionDefinitionAsync(CreateActionDefinitionDto dto)
        {
            ValidateActionConfig(dto.ActionType, dto.ActionConfig);

            var entity = _mapper.Map<ActionDefinition>(dto);
            entity.ActionCode = await _actionCodeGeneratorService.GeneratorActionCodeAsync();

            // Initialize create information with proper tenant and app context
            entity.InitCreateInfo(_userContext);

            await _actionDefinitionRepository.InsertAsync(entity);
            _logger.LogInformation("Created action definition: {ActionId}", entity.Id);

            var resultDto = _mapper.Map<ActionDefinitionDto>(entity);

            if (dto.WorkflowId.HasValue && dto.TriggerSourceId.HasValue && dto.TriggerType.HasValue)
            {
                try
                {
                    var mappingDto = new CreateActionTriggerMappingDto
                    {
                        ActionDefinitionId = entity.Id,
                        WorkFlowId = dto.WorkflowId.Value,
                        TriggerSourceId = dto.TriggerSourceId.Value,
                        TriggerType = dto.TriggerType.ToString() ?? "",
                        StageId = 0,
                        TriggerEvent = "Completed",
                        ExecutionOrder = 1,
                        IsEnabled = true
                    };

                    await CreateActionTriggerMappingAsync(mappingDto);
                    _logger.LogInformation("Created action trigger mapping for action: {ActionId}, WorkflowId: {WorkflowId}, TriggerType: {TriggerType}",
                        entity.Id, dto.WorkflowId.Value, dto.TriggerType);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create action trigger mapping for action: {ActionId}", entity.Id);
                }
            }

            return resultDto;
        }

        public async Task<ActionDefinitionDto> UpdateActionDefinitionAsync(long id, UpdateActionDefinitionDto dto)
        {
            var entity = await _actionDefinitionRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new ArgumentException($"Action definition with ID {id} not found");
            }

            ValidateActionConfig(dto.ActionType, dto.ActionConfig);

            _mapper.Map(dto, entity);

            // Initialize update information with proper tenant and app context
            entity.InitUpdateInfo(_userContext);

            await _actionDefinitionRepository.UpdateAsync(entity);
            _logger.LogInformation("Updated action definition: {ActionId}", id);

            return _mapper.Map<ActionDefinitionDto>(entity);
        }

        public void ValidateActionConfig(ActionTypeEnum actionType, string actionConfig)
        {
            if (string.IsNullOrWhiteSpace(actionConfig))
            {
                throw new ArgumentException("Action configuration cannot be empty");
            }

            try
            {
                var jToken = JToken.Parse(actionConfig);

                switch (actionType)
                {
                    case ActionTypeEnum.Python:
                        ValidatePythonConfig(jToken);
                        break;
                    case ActionTypeEnum.HttpApi:
                        ValidateHttpApiConfig(jToken);
                        break;
                    case ActionTypeEnum.SendEmail:
                        // TODO: Add Email config validation
                        break;
                    case ActionTypeEnum.System:
                        ValidateSystemConfig(jToken);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported action type: {actionType}");
                }
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                throw new ArgumentException($"Invalid JSON configuration for action type {actionType}: {ex.Message}");
            }
        }

        private void ValidatePythonConfig(JToken actionConfig)
        {
            var config = actionConfig.ToObject<PythonActionConfigDto>(new Newtonsoft.Json.JsonSerializer
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            if (config == null)
            {
                throw new ArgumentException("Failed to parse Python action configuration");
            }

            if (string.IsNullOrWhiteSpace(config.SourceCode))
            {
                throw new ArgumentException("Python script source code is required");
            }

            if (!ValidateMainFunction(config.SourceCode))
            {
                throw new ArgumentException("Source code must contain a 'main' function definition");
            }

            _logger.LogInformation("Python action configuration validated successfully");
        }

        /// <summary>
        /// Validate that source code contains a main function
        /// </summary>
        /// <param name="sourceCode">Python source code</param>
        /// <returns>True if main function exists, false otherwise</returns>
        private bool ValidateMainFunction(string sourceCode)
        {
            if (string.IsNullOrWhiteSpace(sourceCode))
                return false;

            var mainPattern = @"def\s+main\s*\(([^)]*)\)(?:\s*->\s*[^:]*)?\s*:";
            var match = Regex.Match(sourceCode, mainPattern, RegexOptions.IgnoreCase);

            return match.Success;
        }

        private void ValidateHttpApiConfig(JToken actionConfig)
        {
            var config = actionConfig.ToObject<HttpApiConfigDto>(new Newtonsoft.Json.JsonSerializer
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            if (config == null)
            {
                throw new ArgumentException("Failed to parse HTTP API action configuration");
            }

            if (string.IsNullOrWhiteSpace(config.Url))
            {
                throw new ArgumentException("HTTP API URL is required");
            }

            if (string.IsNullOrWhiteSpace(config.Method))
            {
                throw new ArgumentException("HTTP API Method is required");
            }

            _logger.LogInformation("HTTP API action configuration validated successfully");
        }

        private void ValidateSystemConfig(JToken actionConfig)
        {
            var config = actionConfig.ToObject<Dictionary<string, object>>();

            if (config == null)
            {
                throw new ArgumentException("Failed to parse System action configuration");
            }

            if (!config.ContainsKey("actionName") || string.IsNullOrWhiteSpace(config["actionName"]?.ToString()))
            {
                throw new ArgumentException("System action must specify 'actionName' in configuration");
            }

            var actionName = config["actionName"].ToString().ToLower();
            var supportedActions = new[] { "completestage", "movetostage", "assignonboarding" };

            if (!supportedActions.Contains(actionName))
            {
                throw new ArgumentException($"System action '{actionName}' is not supported. Supported actions: {string.Join(", ", supportedActions)}");
            }

            // Validate specific action configurations
            switch (actionName)
            {
                case "completestage":
                    ValidateCompleteStageConfig(config);
                    break;
                case "movetostage":
                    ValidateMoveToStageConfig(config);
                    break;
                case "assignonboarding":
                    ValidateAssignOnboardingConfig(config);
                    break;
            }

            _logger.LogInformation("System action configuration validated successfully for action: {ActionName}", actionName);
        }

        private void ValidateCompleteStageConfig(Dictionary<string, object> config)
        {
            // Optional parameters - can be provided in config or extracted from trigger context
            // No strict validation needed as parameters can come from context
        }

        private void ValidateMoveToStageConfig(Dictionary<string, object> config)
        {
            // Optional parameters - can be provided in config or extracted from trigger context
            // No strict validation needed as parameters can come from context
        }

        private void ValidateAssignOnboardingConfig(Dictionary<string, object> config)
        {
            // Optional parameters - can be provided in config or extracted from trigger context
            // No strict validation needed as parameters can come from context
        }

        public async Task<bool> DeleteActionDefinitionAsync(long id)
        {
            var entity = await _actionDefinitionRepository.GetByIdAsync(id);
            if (entity == null)
            {
                // Return true for idempotent delete operation - if action doesn't exist, consider it already deleted
                _logger.LogInformation("Action definition {ActionId} not found, considering delete operation successful", id);
                return true;
            }

            _logger.LogInformation("Starting comprehensive cleanup for action definition: {ActionId}", id);

            // 1. Delete all related ActionTriggerMappings and collect affected IDs for targeted cleanup
            var (taskIds, questionIds) = await CleanupActionTriggerMappingsAsync(id);

            // 2. Clear action references from specific ChecklistTasks (performance optimized)
            await CleanupChecklistTaskActionReferencesAsync(id, taskIds);

            // 3. Clear action references from Questionnaires for specific questions/options (performance optimized)
            await CleanupQuestionnaireActionReferencesAsync(id, questionIds);

            // 4. Finally, mark the ActionDefinition as deleted
            entity.IsValid = false;
            entity.ModifyDate = DateTimeOffset.UtcNow;

            await _actionDefinitionRepository.UpdateAsync(entity);
            _logger.LogInformation("Successfully deleted action definition and all related references: {ActionId}", id);

            return true;
        }

        public async Task<bool> UpdateActionDefinitionStatusAsync(long id, bool isEnabled)
        {
            var entity = await _actionDefinitionRepository.GetByIdAsync(id);
            if (entity == null)
            {
                return false;
            }

            entity.IsEnabled = isEnabled;
            entity.ModifyDate = DateTimeOffset.UtcNow;

            await _actionDefinitionRepository.UpdateAsync(entity);
            _logger.LogInformation("Updated action definition status: {ActionId}, IsEnabled: {IsEnabled}", id, isEnabled);

            return true;
        }

        public async Task<Stream> ExportAsync(string? search,
            ActionTypeEnum? actionType,
            bool? isAssignmentStage = null,
            bool? isAssignmentChecklist = null,
            bool? isAssignmentQuestionnaire = null,
            bool? isAssignmentWorkflow = null,
            bool? isTools = null)
        {
            var (data, total) = await _actionDefinitionRepository.GetPagedAsync(1,
                10000,
                actionType.ToString(),
                search,
                isAssignmentStage,
                isAssignmentChecklist,
                isAssignmentQuestionnaire,
                isAssignmentWorkflow,
                isTools);

            var map = _mapper.Map<List<ActionDefinitionDto>>(data);
            Stream result = ExcelHelper<ActionDefinitionDto>.ExportExcel(map);
            return result;
        }

        #endregion

        #region Action Trigger Mapping Management

        public async Task<ActionTriggerMappingDto> GetActionTriggerMappingAsync(long id)
        {
            var entity = await _actionTriggerMappingRepository.GetByIdAsync(id);
            return _mapper.Map<ActionTriggerMappingDto>(entity);
        }

        public async Task<List<ActionTriggerMappingDto>> GetAllActionTriggerMappingsAsync()
        {
            var entities = await _actionTriggerMappingRepository.GetAllEnabledAsync();
            return _mapper.Map<List<ActionTriggerMappingDto>>(entities);
        }

        public async Task<List<ActionTriggerMappingInfo>> GetActionTriggerMappingsByActionIdAsync(long actionDefinitionId)
        {
            var entities = await _actionDefinitionRepository.GetTriggerMappingsWithDetailsByActionIdsAsync([actionDefinitionId]);
            return _mapper.Map<List<ActionTriggerMappingInfo>>(entities);
        }

        public async Task<List<ActionTriggerMappingDto>> GetActionTriggerMappingsByTriggerTypeAsync(string triggerType)
        {
            var entities = await _actionTriggerMappingRepository.GetByTriggerTypeAsync(triggerType);
            return _mapper.Map<List<ActionTriggerMappingDto>>(entities);
        }

        public async Task<List<ActionTriggerMappingWithActionInfo>> GetActionTriggerMappingsByTriggerSourceIdAsync(long triggerSourceId)
        {
            var entities = await _actionDefinitionRepository.GetMappingsWithActionDetailsByTriggerSourceIdAsync(triggerSourceId);
            return _mapper.Map<List<ActionTriggerMappingWithActionInfo>>(entities);
        }

        public async Task<ActionTriggerMappingDto> CreateActionTriggerMappingAsync(CreateActionTriggerMappingDto dto)
        {
            // Check if mapping already exists
            // If exists, return the existing mapping instead of creating a new one
            var workflowIdForCheck = dto.WorkFlowId ?? 0;
            var existingMapping = await _actionTriggerMappingRepository.GetExistingMappingAsync(
                dto.ActionDefinitionId, dto.TriggerType, dto.TriggerSourceId, workflowIdForCheck);

            if (existingMapping != null)
            {
                _logger.LogInformation("Mapping already exists for ActionDefinitionId={ActionDefinitionId}, TriggerType={TriggerType}, TriggerSourceId={TriggerSourceId}, WorkFlowId={WorkFlowId}. Returning existing mapping: {MappingId}",
                    dto.ActionDefinitionId, dto.TriggerType, dto.TriggerSourceId, dto.WorkFlowId?.ToString() ?? "None", existingMapping.Id);

                // Optional: Log duplicate request attempt (uncomment if needed)
                // try
                // {
                //     var actionDefinition = await _actionDefinitionRepository.GetByIdAsync(dto.ActionDefinitionId);
                //     var triggerSourceName = await GetTriggerSourceNameAsync(dto.TriggerType, dto.TriggerSourceId);
                //     var actionName = actionDefinition?.ActionName ?? "Unknown Action";
                //     
                //     await _actionLogService.LogActionMappingAssociateAsync(
                //         existingMapping.Id,
                //         dto.ActionDefinitionId,
                //         actionName,
                //         dto.TriggerType,
                //         dto.TriggerSourceId,
                //         triggerSourceName,
                //         dto.TriggerEvent,
                //         dto.WorkFlowId,
                //         dto.StageId,
                //         $"Duplicate request - returned existing mapping");
                // }
                // catch (Exception ex)
                // {
                //     _logger.LogWarning(ex, "Failed to log duplicate mapping request for mapping {MappingId}", existingMapping.Id);
                // }

                return _mapper.Map<ActionTriggerMappingDto>(existingMapping);
            }

            // Ensure TriggerEvent has a valid value - default to "Completed" if not provided or empty
            if (string.IsNullOrWhiteSpace(dto.TriggerEvent))
            {
                dto.TriggerEvent = "Completed";
                _logger.LogDebug("TriggerEvent was null/empty, defaulting to 'Completed' for mapping: ActionDefinitionId={ActionDefinitionId}, TriggerSourceId={TriggerSourceId}",
                    dto.ActionDefinitionId, dto.TriggerSourceId);
            }

            var entity = _mapper.Map<ActionTriggerMapping>(dto);

            // Temporary fix: Handle null values until database schema is updated
            // TODO: Remove this after running database migration to make fields nullable
            if (!entity.WorkFlowId.HasValue)
            {
                entity.WorkFlowId = 0; // Use 0 as placeholder for null workflow
                _logger.LogDebug("Setting WorkFlowId to 0 (placeholder for null) for mapping: {MappingId}", entity.Id);
            }
            if (!entity.StageId.HasValue)
            {
                entity.StageId = 0; // Use 0 as placeholder for null stage
                _logger.LogDebug("Setting StageId to 0 (placeholder for null) for mapping: {MappingId}", entity.Id);
            }

            await _actionTriggerMappingRepository.InsertAsync(entity);
            _logger.LogInformation("Created action trigger mapping: {MappingId} with TriggerEvent: {TriggerEvent}", entity.Id, entity.TriggerEvent);

            // Log action trigger mapping association using structured logging and change log
            try
            {
                var actionDefinition = await _actionDefinitionRepository.GetByIdAsync(dto.ActionDefinitionId);
                var triggerSourceName = await GetTriggerSourceNameAsync(dto.TriggerType, dto.TriggerSourceId);
                var actionName = actionDefinition?.ActionName ?? "Unknown Action";

                // Structured logging (existing)
                _logger.LogInformation("Action mapping associated: Action '{ActionName}' (ID: {ActionId}) has been associated with {TriggerType} '{TriggerSourceName}' (ID: {TriggerSourceId}) to trigger on '{TriggerEvent}' event by {UserName}. Mapping ID: {MappingId}, Workflow: {WorkflowId}, Stage: {StageId}",
                    actionName,
                    dto.ActionDefinitionId,
                    dto.TriggerType,
                    triggerSourceName,
                    dto.TriggerSourceId,
                    dto.TriggerEvent,
                    _userContext?.UserName ?? "System",
                    entity.Id,
                    dto.WorkFlowId?.ToString() ?? "None",
                    dto.StageId?.ToString() ?? "None");

                // Change log recording (new) - get additional context for better log association
                long? onboardingId = dto.WorkFlowId; // Use WorkFlowId as onboardingId for workflow-related logs
                long? checklistId = null;

                // For Task triggers, get associated checklist and onboarding information
                if (dto.TriggerType?.ToLower() == "task")
                {
                    try
                    {
                        var task = await _checklistTaskRepository.GetByIdAsync(dto.TriggerSourceId);
                        if (task != null)
                        {
                            checklistId = task.ChecklistId;
                            // You might need to add a method to get onboarding ID from checklist ID
                            // onboardingId = await GetOnboardingIdByChecklistIdAsync(task.ChecklistId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get task context for action mapping log: TaskId={TaskId}", dto.TriggerSourceId);
                    }
                }
                // For Stage triggers, the workflow ID is already set as onboardingId above

                // Create extended data with additional context
                var extendedDataWithContext = JsonSerializer.Serialize(new
                {
                    MappingId = entity.Id,
                    ActionDefinitionId = dto.ActionDefinitionId,
                    ActionName = actionName,
                    TriggerType = dto.TriggerType,
                    TriggerSourceId = dto.TriggerSourceId,
                    TriggerSourceName = triggerSourceName,
                    TriggerEvent = dto.TriggerEvent,
                    WorkflowId = dto.WorkFlowId,
                    StageId = dto.StageId,
                    ChecklistId = checklistId,
                    OnboardingId = onboardingId,
                    AssociatedAt = DateTimeOffset.UtcNow
                });

                await _actionLogService.LogActionMappingAssociateAsync(
                    entity.Id,
                    dto.ActionDefinitionId,
                    actionName,
                    dto.TriggerType,
                    dto.TriggerSourceId,
                    triggerSourceName,
                    dto.TriggerEvent,
                    dto.WorkFlowId,
                    dto.StageId,
                    extendedDataWithContext);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log action trigger mapping association details for mapping {MappingId}", entity.Id);
            }

            return _mapper.Map<ActionTriggerMappingDto>(entity);
        }

        public async Task<bool> DeleteActionTriggerMappingAsync(long id)
        {
            var entity = await _actionTriggerMappingRepository.GetByIdAsync(id);
            if (entity == null)
            {
                // Return true for idempotent delete operation - if mapping doesn't exist, consider it already deleted
                _logger.LogInformation("Action trigger mapping {MappingId} not found, considering delete operation successful", id);
                return true;
            }

            // Log action trigger mapping disassociation using structured logging and change log
            try
            {
                var actionDefinition = await _actionDefinitionRepository.GetByIdAsync(entity.ActionDefinitionId);
                var triggerSourceName = await GetTriggerSourceNameAsync(entity.TriggerType, entity.TriggerSourceId);
                var actionName = actionDefinition?.ActionName ?? "Unknown Action";

                // Structured logging (existing)
                _logger.LogInformation("Action mapping disassociated: Action '{ActionName}' (ID: {ActionId}) has been disassociated from {TriggerType} '{TriggerSourceName}' (ID: {TriggerSourceId}) (was triggered on '{TriggerEvent}' event) by {UserName}. Mapping ID: {MappingId}, Workflow: {WorkflowId}, Stage: {StageId}, Was Enabled: {WasEnabled}",
                    actionName,
                    entity.ActionDefinitionId,
                    entity.TriggerType,
                    triggerSourceName,
                    entity.TriggerSourceId,
                    entity.TriggerEvent,
                    _userContext?.UserName ?? "System",
                    entity.Id,
                    entity.WorkFlowId?.ToString() ?? "None",
                    entity.StageId?.ToString() ?? "None",
                    entity.IsEnabled);

                // Change log recording (new)
                await _actionLogService.LogActionMappingDisassociateAsync(
                    entity.Id,
                    entity.ActionDefinitionId,
                    actionName,
                    entity.TriggerType,
                    entity.TriggerSourceId,
                    triggerSourceName,
                    entity.TriggerEvent,
                    entity.WorkFlowId,
                    entity.StageId,
                    entity.IsEnabled);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log action trigger mapping disassociation details for mapping {MappingId}", id);
            }

            entity.IsValid = false;
            entity.ModifyDate = DateTimeOffset.UtcNow;

            await _actionTriggerMappingRepository.UpdateAsync(entity);

            var mappingCount = await _actionTriggerMappingRepository.CountAsync(m => m.ActionDefinitionId == entity.ActionDefinitionId && m.IsValid);
            if (mappingCount == 0)
            {
                await _actionDefinitionRepository.UpdateSetColumnsTrueAsync(m => new ActionDefinition
                {
                    IsEnabled = false,
                    ModifyDate = DateTimeOffset.UtcNow
                }, d => d.Id == entity.ActionDefinitionId);
            }
            _logger.LogInformation("Deleted action trigger mapping: {MappingId}", id);

            return true;
        }

        public async Task<bool> UpdateActionTriggerMappingStatusAsync(long id, bool isEnabled)
        {
            var entity = await _actionTriggerMappingRepository.GetByIdAsync(id);
            if (entity == null)
            {
                return false;
            }

            var oldStatus = entity.IsEnabled;
            entity.IsEnabled = isEnabled;
            entity.ModifyDate = DateTimeOffset.UtcNow;

            await _actionTriggerMappingRepository.UpdateAsync(entity);
            _logger.LogInformation("Updated action trigger mapping status: {MappingId}, IsEnabled: {IsEnabled}", id, isEnabled);

            // Log status change using ActionLogService
            try
            {
                var actionDefinition = await _actionDefinitionRepository.GetByIdAsync(entity.ActionDefinitionId);
                var triggerSourceName = await GetTriggerSourceNameAsync(entity.TriggerType, entity.TriggerSourceId);
                var actionName = actionDefinition?.ActionName ?? "Unknown Action";

                var statusAction = isEnabled ? "enabled" : "disabled";
                var extendedData = JsonSerializer.Serialize(new
                {
                    MappingId = entity.Id,
                    ActionDefinitionId = entity.ActionDefinitionId,
                    ActionName = actionName,
                    TriggerType = entity.TriggerType,
                    TriggerSourceId = entity.TriggerSourceId,
                    TriggerSourceName = triggerSourceName,
                    TriggerEvent = entity.TriggerEvent,
                    OldStatus = oldStatus,
                    NewStatus = isEnabled,
                    UpdatedAt = DateTimeOffset.UtcNow
                });

                // Use a general ActionMapping update operation type
                await _actionLogService.LogActionMappingUpdateAsync(
                    entity.Id,
                    entity.ActionDefinitionId,
                    actionName,
                    entity.TriggerType,
                    entity.TriggerSourceId,
                    triggerSourceName,
                    $"Status {statusAction}",
                    oldStatus.ToString(),
                    isEnabled.ToString(),
                    new List<string> { "IsEnabled" },
                    extendedData);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log action trigger mapping status update for mapping {MappingId}", id);
            }

            return true;
        }

        public async Task<bool> BatchUpdateActionTriggerMappingStatusAsync(List<long> mappingIds, bool isEnabled)
        {
            var result = await _actionTriggerMappingRepository.BatchUpdateEnabledStatusAsync(mappingIds, isEnabled);

            if (result)
            {
                _logger.LogInformation("Batch updated {Count} action trigger mapping statuses to IsEnabled: {IsEnabled}", mappingIds.Count, isEnabled);

                // Log batch status change - simplified logging without individual details for performance
                try
                {
                    var statusAction = isEnabled ? "enabled" : "disabled";
                    var extendedData = JsonSerializer.Serialize(new
                    {
                        MappingIds = mappingIds,
                        NewStatus = isEnabled,
                        UpdatedAt = DateTimeOffset.UtcNow,
                        UpdatedCount = mappingIds.Count
                    });

                    // Use a simple log entry for batch operations
                    _logger.LogInformation("Batch Action Mapping Status Update: {Count} mappings have been {StatusAction} by {UserName}",
                        mappingIds.Count, statusAction, _userContext?.UserName ?? "System");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to log batch action trigger mapping status update for {Count} mappings", mappingIds.Count);
                }
            }

            return result;
        }

        #endregion

        #region Action Cleanup Helper Methods

        /// <summary>
        /// Clean up all ActionTriggerMappings for a specific ActionDefinition and return mapping info for targeted cleanup
        /// </summary>
        private async Task<(List<long> taskIds, List<long> questionIds)> CleanupActionTriggerMappingsAsync(long actionDefinitionId)
        {
            var taskIds = new List<long>();
            var questionIds = new List<long>();

            try
            {
                // Get all mappings for this action definition
                var actionMappings = await _actionTriggerMappingRepository.GetByActionDefinitionIdAsync(actionDefinitionId);
                // Filter only valid mappings
                actionMappings = actionMappings.Where(m => m.IsValid).ToList();

                _logger.LogInformation("Found {Count} ActionTriggerMappings to cleanup for action {ActionId}",
                    actionMappings.Count, actionDefinitionId);

                foreach (var mapping in actionMappings)
                {
                    // Collect source IDs for targeted cleanup
                    if (mapping.TriggerType?.Trim() == "Task")
                    {
                        taskIds.Add(mapping.TriggerSourceId);
                    }
                    else if (mapping.TriggerType?.Trim() == "Question")
                    {
                        questionIds.Add(mapping.TriggerSourceId);
                    }

                    // Mark mapping as deleted
                    mapping.IsValid = false;
                    mapping.ModifyDate = DateTimeOffset.UtcNow;
                    await _actionTriggerMappingRepository.UpdateAsync(mapping);

                    _logger.LogDebug("Deleted ActionTriggerMapping {MappingId} (Type: {TriggerType}, SourceId: {TriggerSourceId}) for action {ActionId}",
                        mapping.Id, mapping.TriggerType, mapping.TriggerSourceId, actionDefinitionId);
                }

                _logger.LogInformation("Successfully cleaned up {Count} ActionTriggerMappings for action {ActionId} - Tasks: {TaskCount}, Questions: {QuestionCount}",
                    actionMappings.Count, actionDefinitionId, taskIds.Count, questionIds.Count);

                return (taskIds.Distinct().ToList(), questionIds.Distinct().ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up ActionTriggerMappings for action {ActionId}", actionDefinitionId);
                return (taskIds, questionIds);
            }
        }

        /// <summary>
        /// Clear action references from specific ChecklistTasks (performance optimized)
        /// </summary>
        private async Task CleanupChecklistTaskActionReferencesAsync(long actionDefinitionId, List<long> taskIds)
        {
            if (!taskIds.Any())
            {
                _logger.LogInformation("No ChecklistTask IDs provided for cleanup, skipping task reference cleanup");
                return;
            }

            try
            {
                var cleanedCount = 0;

                foreach (var taskId in taskIds)
                {
                    var task = await _checklistTaskRepository.GetByIdAsync(taskId);
                    if (task != null && task.ActionId == actionDefinitionId && task.IsValid)
                    {
                        task.ActionId = null;
                        task.ActionName = null;
                        task.ActionMappingId = null;
                        task.ModifyDate = DateTimeOffset.UtcNow;
                        await _checklistTaskRepository.UpdateAsync(task);
                        cleanedCount++;
                        _logger.LogDebug("Cleared action reference from ChecklistTask {TaskId}", task.Id);
                    }
                    else
                    {
                        _logger.LogDebug("ChecklistTask {TaskId} not found or does not reference action {ActionId}", taskId, actionDefinitionId);
                    }
                }

                _logger.LogInformation("Successfully cleaned up {Count} ChecklistTask action references for action {ActionId} from {TotalIds} provided IDs",
                    cleanedCount, actionDefinitionId, taskIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up ChecklistTask action references for action {ActionId}", actionDefinitionId);
            }
        }

        /// <summary>
        /// Clear action references from Questionnaires for specific question/option IDs (performance optimized)
        /// </summary>
        private async Task CleanupQuestionnaireActionReferencesAsync(long actionDefinitionId, List<long> questionIds)
        {
            if (!questionIds.Any())
            {
                _logger.LogInformation("No Question/Option IDs provided for cleanup, skipping questionnaire reference cleanup");
                return;
            }

            try
            {
                // Get all questionnaires - we still need to iterate through them as questions are embedded in JSON
                var questionnaires = await _questionnaireRepository.GetListAsync();
                var updatedCount = 0;
                var questionIdStrings = questionIds.Select(id => id.ToString()).ToHashSet();

                foreach (var questionnaire in questionnaires.Where(q => q.IsValid))
                {
                    if (questionnaire?.Structure == null) continue;

                    var structureJson = questionnaire.Structure.ToString(Newtonsoft.Json.Formatting.None);
                    var hasChanges = false;

                    try
                    {
                        using var document = JsonDocument.Parse(structureJson);
                        var root = document.RootElement;

                        // Create a mutable copy for modifications
                        var structureObj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(structureJson);

                        if (root.TryGetProperty("sections", out var sectionsElement))
                        {
                            var sectionIndex = 0;
                            foreach (var section in sectionsElement.EnumerateArray())
                            {
                                // Process questions array - only clean matching IDs
                                hasChanges |= CleanupActionReferencesInTargetedArray(section, structureObj.sections[sectionIndex],
                                    "questions", actionDefinitionId, questionIdStrings);

                                // Process subsections if they exist
                                if (section.TryGetProperty("subsections", out var subsectionsElement))
                                {
                                    var subsectionIndex = 0;
                                    foreach (var subsection in subsectionsElement.EnumerateArray())
                                    {
                                        hasChanges |= CleanupActionReferencesInTargetedArray(subsection,
                                            structureObj.sections[sectionIndex].subsections[subsectionIndex],
                                            "questions", actionDefinitionId, questionIdStrings);
                                        subsectionIndex++;
                                    }
                                }
                                sectionIndex++;
                            }
                        }

                        if (hasChanges)
                        {
                            questionnaire.Structure = Newtonsoft.Json.Linq.JObject.FromObject(structureObj);
                            questionnaire.ModifyDate = DateTimeOffset.UtcNow;
                            await _questionnaireRepository.UpdateAsync(questionnaire);
                            updatedCount++;
                            _logger.LogDebug("Updated questionnaire {QuestionnaireId} to remove targeted action references",
                                questionnaire.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing questionnaire {QuestionnaireId} for targeted action cleanup",
                            questionnaire.Id);
                    }
                }

                _logger.LogInformation("Successfully cleaned up action references from {Count} questionnaires for action {ActionId} targeting {TargetCount} question/option IDs",
                    updatedCount, actionDefinitionId, questionIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up Questionnaire action references for action {ActionId}", actionDefinitionId);
            }
        }

        /// <summary>
        /// Helper method to clean up action references in question arrays - performance optimized for specific IDs
        /// </summary>
        private bool CleanupActionReferencesInTargetedArray(JsonElement section, dynamic sectionObj,
            string arrayName, long actionDefinitionId, HashSet<string> targetQuestionIds)
        {
            var hasChanges = false;

            if (section.TryGetProperty(arrayName, out var questionsElement) && questionsElement.ValueKind == JsonValueKind.Array)
            {
                var questionIndex = 0;
                foreach (var question in questionsElement.EnumerateArray())
                {
                    var questionId = question.TryGetProperty("id", out var questionIdEl) ? questionIdEl.GetString() : null;

                    // Only process if this question ID is in our target list
                    if (!string.IsNullOrEmpty(questionId) && targetQuestionIds.Contains(questionId))
                    {
                        // Check and clean question action
                        if (question.TryGetProperty("action", out var actionElement))
                        {
                            if (actionElement.TryGetProperty("id", out var actionIdEl) &&
                                actionIdEl.GetString() == actionDefinitionId.ToString())
                            {
                                sectionObj[arrayName][questionIndex].action = null;
                                hasChanges = true;
                                _logger.LogDebug("Removed action reference from question {QuestionId} in {ArrayName}", questionId, arrayName);
                            }
                        }

                        // Check and clean options actions
                        if (question.TryGetProperty("options", out var optionsElement) && optionsElement.ValueKind == JsonValueKind.Array)
                        {
                            var optionIndex = 0;
                            foreach (var option in optionsElement.EnumerateArray())
                            {
                                var optionId = option.TryGetProperty("id", out var optIdEl) ? optIdEl.GetString() :
                                              option.TryGetProperty("temporaryId", out var tempIdEl) ? tempIdEl.GetString() : null;

                                // Check if this option ID is also in our target list
                                if (!string.IsNullOrEmpty(optionId) && targetQuestionIds.Contains(optionId))
                                {
                                    if (option.TryGetProperty("action", out var optionActionElement))
                                    {
                                        if (optionActionElement.TryGetProperty("id", out var optionActionIdEl) &&
                                            optionActionIdEl.GetString() == actionDefinitionId.ToString())
                                        {
                                            sectionObj[arrayName][questionIndex].options[optionIndex].action = null;
                                            hasChanges = true;
                                            _logger.LogDebug("Removed action reference from option {OptionId} in {ArrayName}", optionId, arrayName);
                                        }
                                    }
                                }
                                optionIndex++;
                            }
                        }
                    }
                    questionIndex++;
                }
            }

            return hasChanges;
        }

        #endregion

        #region Private Helper Methods for Logging

        /// <summary>
        /// Get trigger source name based on trigger type and source ID
        /// </summary>
        private async Task<string> GetTriggerSourceNameAsync(string triggerType, long triggerSourceId)
        {
            try
            {
                switch (triggerType?.ToLower())
                {
                    case "task":
                        var task = await _checklistTaskRepository.GetByIdAsync(triggerSourceId);
                        return task?.Name ?? $"Task {triggerSourceId}";

                    case "question":
                        var questionnaire = await _questionnaireRepository.GetByIdAsync(triggerSourceId);
                        return questionnaire?.Name ?? $"Question {triggerSourceId}";

                    case "stage":
                        var stage = await _stageRepository.GetByIdAsync(triggerSourceId);
                        return stage?.Name ?? $"Stage {triggerSourceId}";

                    default:
                        return $"{triggerType} {triggerSourceId}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get trigger source name for {TriggerType} {TriggerSourceId}", triggerType, triggerSourceId);
                return $"{triggerType} {triggerSourceId}";
            }
        }


        #endregion
    }
}