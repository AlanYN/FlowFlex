﻿using AutoMapper;
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
using FlowFlex.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

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
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly IServiceScopeFactory _serviceScopeFactory;
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
            IBackgroundTaskQueue backgroundTaskQueue,
            IServiceScopeFactory serviceScopeFactory,
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
            _backgroundTaskQueue = backgroundTaskQueue;
            _serviceScopeFactory = serviceScopeFactory;
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
            // If querying System actions, ensure system predefined actions exist
            if (actionType == ActionTypeEnum.System)
            {
                await EnsureSystemPredefinedActionsExistAsync();
            }

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

            // Log action definition create operation with structured logging
            _logger.LogInformation("Action definition created: {ActionId} - {ActionName} (Type: {ActionType})",
                entity.Id, entity.ActionName, dto.ActionType);

            _logger.LogDebug("Action definition create details: {@ActionDetails}", new
            {
                ActionId = entity.Id,
                ActionCode = entity.ActionCode,
                ActionName = entity.ActionName,
                ActionType = dto.ActionType,
                IsTools = dto.IsTools,
                IsAIGenerated = dto.IsAIGenerated,
                TriggerType = dto.TriggerType,
                CreatedAt = entity.CreateDate,
                TenantId = entity.TenantId,
                CreateBy = entity.CreateBy
            });

            // Capture current user context for async operation
            var currentUserContext = _userContext;
            var currentUserName = currentUserContext?.UserName ?? "SYSTEM";
            var currentUserId = long.TryParse(currentUserContext?.UserId, out var parsedUserId) ? parsedUserId : 0L;
            var currentTenantId = currentUserContext?.TenantId ?? "DEFAULT";

            // Async change log recording to database using IActionLogService (fire-and-forget)
            _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var actionLogService = scope.ServiceProvider.GetRequiredService<IActionLogService>();

                    var extendedData = JsonSerializer.Serialize(new
                    {
                        ActionId = entity.Id,
                        ActionCode = entity.ActionCode,
                        ActionName = entity.ActionName,
                        ActionType = dto.ActionType.ToString(),
                        IsTools = dto.IsTools,
                        IsAIGenerated = dto.IsAIGenerated,
                        TriggerType = dto.TriggerType?.ToString(),
                        CreatedAt = entity.CreateDate,
                        TenantId = currentTenantId,
                        CreateBy = currentUserName,
                        CreateUserId = currentUserId
                    });

                    await actionLogService.LogActionDefinitionCreateWithUserContextAsync(
                        entity.Id,
                        entity.ActionName,
                        dto.ActionType.ToString(),
                        currentUserName,
                        currentUserId,
                        currentTenantId,
                        extendedData
                    );
                }
                catch (Exception ex)
                {
                    // Log error but don't throw to avoid breaking background task processing
                    _logger.LogError(ex, "Failed to record action definition create change log for action {ActionId}", entity.Id);
                }
            });

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

            // Store original values for change logging
            var originalName = entity.ActionName;
            var originalDescription = entity.Description;
            var originalActionType = entity.ActionType;
            var originalActionConfig = entity.ActionConfig;
            var originalIsEnabled = entity.IsEnabled;
            var originalIsTools = entity.IsTools;

            ActionDefinitionDto resultDto;
            List<string> changedFields = new List<string>();

            // Check if this is a System Action
            if (entity.ActionType == ActionTypeEnum.System.ToString())
            {
                _logger.LogInformation("Updating System Action {ActionId}: only IsEnabled and IsTools fields will be updated, core configuration remains unchanged", id);

                // For System Actions, only update specific fields, ignore core configuration
                if (entity.IsEnabled != dto.IsEnabled)
                {
                    entity.IsEnabled = dto.IsEnabled;
                    changedFields.Add("IsEnabled");
                }

                if (entity.IsTools != dto.IsTools)
                {
                    entity.IsTools = dto.IsTools;
                    changedFields.Add("IsTools");
                }

                // Initialize update information with proper tenant and app context
                entity.InitUpdateInfo(_userContext);

                await _actionDefinitionRepository.UpdateAsync(entity);
                _logger.LogInformation("Updated System Action definition: {ActionId}", id);

                resultDto = _mapper.Map<ActionDefinitionDto>(entity);
            }
            else
            {
                // For non-System Actions, perform full validation and update
                ValidateActionConfig(dto.ActionType, dto.ActionConfig);

                // Track changes
                if (entity.ActionName != dto.Name)
                {
                    entity.ActionName = dto.Name;
                    changedFields.Add("Name");
                }
                if (entity.Description != dto.Description)
                {
                    entity.Description = dto.Description;
                    changedFields.Add("Description");
                }
                if (entity.ActionType != dto.ActionType.ToString())
                {
                    entity.ActionType = dto.ActionType.ToString();
                    changedFields.Add("ActionType");
                }
                if (entity.ActionConfig?.ToString() != dto.ActionConfig)
                {
                    entity.ActionConfig = JToken.Parse(dto.ActionConfig ?? "{}");
                    changedFields.Add("ActionConfig");
                }
                if (entity.IsEnabled != dto.IsEnabled)
                {
                    entity.IsEnabled = dto.IsEnabled;
                    changedFields.Add("IsEnabled");
                }
                if (entity.IsTools != dto.IsTools)
                {
                    entity.IsTools = dto.IsTools;
                    changedFields.Add("IsTools");
                }

                // Initialize update information with proper tenant and app context
                entity.InitUpdateInfo(_userContext);

                await _actionDefinitionRepository.UpdateAsync(entity);
                _logger.LogInformation("Updated action definition: {ActionId}", id);

                resultDto = _mapper.Map<ActionDefinitionDto>(entity);
            }

            // Log action definition update operation with structured logging
            if (changedFields.Any())
            {
                _logger.LogInformation("Action definition updated: {ActionId} - {ActionName} - {ChangeCount} field(s) changed: {ChangedFields}",
                    id, entity.ActionName, changedFields.Count, string.Join(", ", changedFields));

                _logger.LogDebug("Action definition update details: {@UpdateDetails}", new
                {
                    ActionId = id,
                    ActionCode = entity.ActionCode,
                    ActionName = entity.ActionName,
                    ActionType = entity.ActionType,
                    ChangedFields = changedFields,
                    IsSystemAction = entity.ActionType == ActionTypeEnum.System.ToString(),
                    UpdatedAt = entity.ModifyDate,
                    TenantId = entity.TenantId,
                    ModifyBy = entity.ModifyBy
                });

                // Capture current user context for async operation
                var currentUserContext = _userContext;
                var currentUserName = currentUserContext?.UserName ?? "SYSTEM";
                var currentUserId = long.TryParse(currentUserContext?.UserId, out var parsedUserId) ? parsedUserId : 0L;
                var currentTenantId = currentUserContext?.TenantId ?? "DEFAULT";

                // Async change log recording to database using IActionLogService (fire-and-forget)
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var actionLogService = scope.ServiceProvider.GetRequiredService<IActionLogService>();

                        // Prepare before and after data for logging using original values
                        var beforeData = JsonSerializer.Serialize(new
                        {
                            Name = originalName,
                            Description = originalDescription,
                            ActionType = originalActionType,
                            ActionConfig = originalActionConfig?.ToString(),
                            IsEnabled = originalIsEnabled,
                            IsTools = originalIsTools
                        });

                        var afterData = JsonSerializer.Serialize(new
                        {
                            Name = entity.ActionName,
                            Description = entity.Description,
                            ActionType = entity.ActionType,
                            ActionConfig = entity.ActionConfig?.ToString(),
                            IsEnabled = entity.IsEnabled,
                            IsTools = entity.IsTools
                        });

                        var extendedData = JsonSerializer.Serialize(new
                        {
                            ActionId = entity.Id,
                            ActionCode = entity.ActionCode,
                            ChangeCount = changedFields.Count,
                            ChangedFields = changedFields,
                            IsSystemAction = entity.ActionType == ActionTypeEnum.System.ToString(),
                            UpdatedAt = entity.ModifyDate,
                            TenantId = currentTenantId,
                            ModifyBy = currentUserName,
                            ModifyUserId = currentUserId
                        });

                        await actionLogService.LogActionDefinitionUpdateWithUserContextAsync(
                            entity.Id,
                            entity.ActionName,
                            beforeData,
                            afterData,
                            changedFields,
                            currentUserName,
                            currentUserId,
                            currentTenantId,
                            extendedData
                        );
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't throw to avoid breaking background task processing
                        _logger.LogError(ex, "Failed to record action definition update change log for action {ActionId}", entity.Id);
                    }
                });

                // If action name changed, sync the new name to related ChecklistTasks and Questionnaires
                if (changedFields.Contains("Name") && originalName != entity.ActionName)
                {
                    _logger.LogInformation("Action name changed from '{OldName}' to '{NewName}', syncing to related entities", 
                        originalName, entity.ActionName);

                    // Capture action ID and name for background task
                    var actionId = id;
                    var newActionName = entity.ActionName;
                    var tenantId = currentTenantId;

                    // Async sync action name to related entities (fire-and-forget)
                    _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                    {
                        try
                        {
                            // Create a new scope for the background task to avoid disposed object issues
                            using var scope = _serviceScopeFactory.CreateScope();
                            var checklistTaskRepository = scope.ServiceProvider.GetRequiredService<IChecklistTaskRepository>();
                            var questionnaireRepository = scope.ServiceProvider.GetRequiredService<IQuestionnaireRepository>();
                            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ActionManagementService>>();

                            await SyncActionNameToRelatedEntitiesAsync(
                                actionId, 
                                newActionName, 
                                tenantId, 
                                checklistTaskRepository, 
                                questionnaireRepository, 
                                logger);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to sync action name to related entities for action {ActionId}", actionId);
                        }
                    });
                }
            }

            return resultDto;
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

            // Validate useValidationApi parameter if present
            if (config.ContainsKey("useValidationApi"))
            {
                if (!bool.TryParse(config["useValidationApi"]?.ToString(), out _))
                {
                    throw new ArgumentException("useValidationApi parameter must be a boolean value");
                }
            }
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

            // Store information for logging before deletion
            var actionName = entity.ActionName;
            var actionType = entity.ActionType;
            var actionCode = entity.ActionCode;

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

            // Log action definition delete operation with structured logging
            _logger.LogInformation("Action definition deleted: {ActionId} - {ActionName} (Type: {ActionType}) - Affected tasks: {TaskCount}, questions: {QuestionCount}",
                id, actionName, actionType, taskIds.Count, questionIds.Count);

            _logger.LogDebug("Action definition delete details: {@DeleteDetails}", new
            {
                ActionId = id,
                ActionCode = actionCode,
                ActionName = actionName,
                ActionType = actionType,
                TaskIdsAffected = taskIds,
                QuestionIdsAffected = questionIds,
                TriggerMappingsDeleted = taskIds.Count + questionIds.Count,
                ComprehensiveCleanup = true,
                DeletedAt = DateTimeOffset.UtcNow,
                TenantId = _userContext?.TenantId
            });

            // Capture current user context for async operation
            var currentUserContext = _userContext;
            var currentUserName = currentUserContext?.UserName ?? "SYSTEM";
            var currentUserId = long.TryParse(currentUserContext?.UserId, out var parsedUserId) ? parsedUserId : 0L;
            var currentTenantId = currentUserContext?.TenantId ?? "DEFAULT";

            // Async change log recording to database using IActionLogService (fire-and-forget)
            _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var actionLogService = scope.ServiceProvider.GetRequiredService<IActionLogService>();

                    var extendedData = JsonSerializer.Serialize(new
                    {
                        ActionId = id,
                        ActionCode = actionCode,
                        ActionName = actionName,
                        ActionType = actionType,
                        TaskIdsAffected = taskIds,
                        QuestionIdsAffected = questionIds,
                        TriggerMappingsDeleted = taskIds.Count + questionIds.Count,
                        ComprehensiveCleanup = true,
                        DeletedAt = DateTimeOffset.UtcNow,
                        TenantId = currentTenantId,
                        DeleteBy = currentUserName,
                        DeleteUserId = currentUserId
                    });

                    var reason = $"Action definition deleted along with {taskIds.Count + questionIds.Count} related trigger mappings by {currentUserName}";

                    await actionLogService.LogActionDefinitionDeleteWithUserContextAsync(
                        id,
                        actionName,
                        currentUserName,
                        currentUserId,
                        currentTenantId,
                        reason,
                        extendedData
                    );
                }
                catch (Exception ex)
                {
                    // Log error but don't throw to avoid breaking background task processing
                    _logger.LogError(ex, "Failed to record action definition delete change log for action {ActionId}", id);
                }
            });

            return true;
        }

        public async Task<bool> UpdateActionDefinitionStatusAsync(long id, bool isEnabled)
        {
            var entity = await _actionDefinitionRepository.GetByIdAsync(id);
            if (entity == null)
            {
                return false;
            }

            var originalStatus = entity.IsEnabled;
            entity.IsEnabled = isEnabled;
            entity.ModifyDate = DateTimeOffset.UtcNow;

            await _actionDefinitionRepository.UpdateAsync(entity);
            _logger.LogInformation("Updated action definition status: {ActionId}, IsEnabled: {IsEnabled}", id, isEnabled);

            // Log action definition status change with structured logging
            var statusAction = isEnabled ? "enabled" : "disabled";
            _logger.LogInformation("Action definition status changed: {ActionId} - {ActionName} - Status: {OldStatus} -> {NewStatus}",
                id, entity.ActionName, originalStatus, isEnabled);

            _logger.LogDebug("Action definition status change details: {@StatusChangeDetails}", new
            {
                ActionId = id,
                ActionCode = entity.ActionCode,
                ActionName = entity.ActionName,
                ActionType = entity.ActionType,
                OldStatus = originalStatus,
                NewStatus = isEnabled,
                StatusAction = statusAction,
                UpdatedAt = entity.ModifyDate,
                TenantId = entity.TenantId,
                ModifyBy = entity.ModifyBy
            });

            // Capture current user context for async operation
            var currentUserContext = _userContext;
            var currentUserName = currentUserContext?.UserName ?? "SYSTEM";
            var currentUserId = long.TryParse(currentUserContext?.UserId, out var parsedUserId) ? parsedUserId : 0L;
            var currentTenantId = currentUserContext?.TenantId ?? "DEFAULT";

            // Async change log recording to database using IActionLogService (fire-and-forget)
            _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var actionLogService = scope.ServiceProvider.GetRequiredService<IActionLogService>();

                    var beforeData = JsonSerializer.Serialize(new
                    {
                        IsEnabled = originalStatus
                    });

                    var afterData = JsonSerializer.Serialize(new
                    {
                        IsEnabled = isEnabled
                    });

                    var extendedData = JsonSerializer.Serialize(new
                    {
                        ActionId = id,
                        ActionCode = entity.ActionCode,
                        ActionName = entity.ActionName,
                        ActionType = entity.ActionType,
                        OldStatus = originalStatus,
                        NewStatus = isEnabled,
                        StatusAction = statusAction,
                        UpdatedAt = entity.ModifyDate,
                        TenantId = currentTenantId,
                        ModifyBy = currentUserName,
                        ModifyUserId = currentUserId
                    });

                    await actionLogService.LogActionDefinitionUpdateWithUserContextAsync(
                        id,
                        entity.ActionName,
                        beforeData,
                        afterData,
                        new List<string> { "IsEnabled" },
                        currentUserName,
                        currentUserId,
                        currentTenantId,
                        extendedData
                    );
                }
                catch (Exception ex)
                {
                    // Log error but don't throw to avoid breaking background task processing
                    _logger.LogError(ex, "Failed to record action definition status change log for action {ActionId}", id);
                }
            });

            return true;
        }

        public async Task<Stream> ExportAsync(string? search,
            ActionTypeEnum? actionType,
            bool? isAssignmentStage = null,
            bool? isAssignmentChecklist = null,
            bool? isAssignmentQuestionnaire = null,
            bool? isAssignmentWorkflow = null,
            bool? isTools = null,
            string? actionIds = null)
        {
            List<ActionDefinition> data;

            // If actionIds is provided, export specific actions by IDs
            if (!string.IsNullOrWhiteSpace(actionIds))
            {
                var ids = actionIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(id => long.TryParse(id.Trim(), out var parsedId) ? parsedId : (long?)null)
                                 .Where(id => id.HasValue)
                                 .Select(id => id.Value)
                                 .ToList();

                if (ids.Any())
                {
                    data = await _actionDefinitionRepository.GetByIdsAsync(ids);
                    _logger.LogInformation("Exporting {Count} specific actions by IDs: {ActionIds}", data.Count, actionIds);
                }
                else
                {
                    _logger.LogWarning("No valid action IDs found in actionIds parameter: {ActionIds}", actionIds);
                    data = new List<ActionDefinition>();
                }
            }
            else
            {
                // Export with filtering conditions (existing logic)
                var (pagedData, total) = await _actionDefinitionRepository.GetPagedAsync(1,
                    10000,
                    actionType.ToString(),
                    search,
                    isAssignmentStage,
                    isAssignmentChecklist,
                    isAssignmentQuestionnaire,
                    isAssignmentWorkflow,
                    isTools);

                data = pagedData;
                _logger.LogInformation("Exporting {Count} actions with filter conditions", data.Count);
            }

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
            // Business rule: Task/Question type can only have ONE mapping per TriggerSourceId
            // Stage type can have multiple mappings (checked by ActionDefinitionId + TriggerType + TriggerSourceId + WorkFlowId)
            if (dto.TriggerType?.Trim().Equals("Task", StringComparison.OrdinalIgnoreCase) == true ||
                dto.TriggerType?.Trim().Equals("Question", StringComparison.OrdinalIgnoreCase) == true)
            {
                // For Task/Question: check if TriggerSourceId already has any valid mapping
                var existingMappingsForSource = await _actionTriggerMappingRepository.GetByTriggerTypeAsync(dto.TriggerType);
                var existingMappingForThisSource = existingMappingsForSource
                    .FirstOrDefault(m => m.TriggerSourceId == dto.TriggerSourceId && m.IsValid);

                if (existingMappingForThisSource != null)
                {
                    // If the existing mapping is for the same action, return it directly
                    if (existingMappingForThisSource.ActionDefinitionId == dto.ActionDefinitionId)
                    {
                        _logger.LogInformation("Mapping already exists for {TriggerType} TriggerSourceId={TriggerSourceId}. " +
                            "Returning existing mapping: {MappingId} (ActionDefinitionId={ExistingActionId}). " +
                            "Task/Question type only allows ONE mapping per source.",
                            dto.TriggerType, dto.TriggerSourceId, existingMappingForThisSource.Id, existingMappingForThisSource.ActionDefinitionId);

                        // 显示层动态读取映射信息：不再把映射写回任务字段

                        return _mapper.Map<ActionTriggerMappingDto>(existingMappingForThisSource);
                    }
                    else
                    {
                        // Conflict: different action, delete old mapping and create new one
                        _logger.LogWarning("Mapping conflict detected for {TriggerType} TriggerSourceId={TriggerSourceId}. " +
                            "Existing mapping {ExistingMappingId} (ActionDefinitionId={OldActionId}) will be deleted and replaced with new mapping (ActionDefinitionId={NewActionId}).",
                            dto.TriggerType, dto.TriggerSourceId, existingMappingForThisSource.Id, 
                            existingMappingForThisSource.ActionDefinitionId, dto.ActionDefinitionId);

                        // Delete the old mapping
                        existingMappingForThisSource.IsValid = false;
                        existingMappingForThisSource.ModifyDate = DateTimeOffset.UtcNow;
                        await _actionTriggerMappingRepository.UpdateAsync(existingMappingForThisSource);

                        _logger.LogInformation("Deleted conflicting mapping {MappingId} for {TriggerType} TriggerSourceId={TriggerSourceId}",
                            existingMappingForThisSource.Id, dto.TriggerType, dto.TriggerSourceId);

                        // Continue to create new mapping below
                    }
                }
            }
            else
            {
                // For Stage or other types: check if exact mapping already exists (by ActionDefinitionId + TriggerType + TriggerSourceId + WorkFlowId)
                var workflowIdForCheck = dto.WorkFlowId ?? 0;
                var existingMapping = await _actionTriggerMappingRepository.GetExistingMappingAsync(
                    dto.ActionDefinitionId, dto.TriggerType, dto.TriggerSourceId, workflowIdForCheck);

                if (existingMapping != null)
                {
                    _logger.LogInformation("Mapping already exists for ActionDefinitionId={ActionDefinitionId}, TriggerType={TriggerType}, TriggerSourceId={TriggerSourceId}, WorkFlowId={WorkFlowId}. Returning existing mapping: {MappingId}",
                        dto.ActionDefinitionId, dto.TriggerType, dto.TriggerSourceId, dto.WorkFlowId?.ToString() ?? "None", existingMapping.Id);

                    // 显示层动态读取映射信息：不再把映射写回任务字段

                    return _mapper.Map<ActionTriggerMappingDto>(existingMapping);
                }
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

            // 显示层动态读取映射信息：不再把映射写回任务字段

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

        /// <summary>
        /// Update trigger source entity (Task, Question, etc.) with action mapping information
        /// </summary>
        /// <param name="dto">Create action trigger mapping DTO</param>
        /// <param name="mappingId">Action trigger mapping ID</param>
        /// <param name="actionDefinitionId">Action definition ID</param>
        private async Task UpdateTriggerSourceWithMappingInfoAsync(CreateActionTriggerMappingDto dto, long mappingId, long actionDefinitionId)
        {
            try
            {
                // Only update Tasks for now, can be extended for other trigger types if needed
                if (dto.TriggerType?.ToLower() == "task")
                {
                    var task = await _checklistTaskRepository.GetByIdAsync(dto.TriggerSourceId);
                    if (task != null && task.IsValid)
                    {
                        var actionDefinition = await _actionDefinitionRepository.GetByIdAsync(actionDefinitionId);
                        var actionName = actionDefinition?.ActionName ?? "Unknown Action";

                        // Update task with action mapping information
                        task.ActionId = actionDefinitionId;
                        task.ActionName = actionName;
                        task.ActionMappingId = mappingId;
                        task.ModifyDate = DateTimeOffset.UtcNow;

                        await _checklistTaskRepository.UpdateAsync(task);
                        _logger.LogDebug("Updated task {TaskId} with action mapping info: ActionId={ActionId}, ActionName={ActionName}, MappingId={MappingId}",
                            task.Id, actionDefinitionId, actionName, mappingId);
                    }
                    else
                    {
                        _logger.LogWarning("Task {TaskId} not found or invalid when updating action mapping info", dto.TriggerSourceId);
                    }
                }
                else
                {
                    _logger.LogDebug("TriggerType {TriggerType} does not require source entity update, skipping", dto.TriggerType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating trigger source {TriggerType} {TriggerSourceId} with action mapping info",
                    dto.TriggerType, dto.TriggerSourceId);
                // Don't throw - this is not critical enough to fail the whole operation
            }
        }


        #endregion

        #region System Predefined Actions Management

        /// <summary>
        /// Ensure system predefined actions exist, create them if they don't
        /// </summary>
        private async Task EnsureSystemPredefinedActionsExistAsync()
        {
            try
            {
                var currentTenantId = _userContext?.TenantId ?? "DEFAULT";
                _logger.LogInformation("Checking for system predefined actions for tenant: {TenantId}", currentTenantId);

                // Get existing system actions for current tenant to check which ones exist
                var (existingData, _) = await _actionDefinitionRepository.GetPagedAsync(1, 1000,
                    ActionTypeEnum.System.ToString(), null, null, null, null, null, false);

                // Filter by current tenant and get action names
                var existingActionNames = existingData
                    .Where(a => !string.IsNullOrEmpty(a.ActionName) && a.TenantId == currentTenantId)
                    .Select(a => a.ActionName.Trim())
                    .ToHashSet();

                // Define system predefined actions that should exist
                var systemActions = GetSystemPredefinedActionDefinitions();

                foreach (var systemAction in systemActions)
                {
                    if (!existingActionNames.Contains(systemAction.Name.Trim()))
                    {
                        _logger.LogInformation("Creating missing system action: {ActionName} for tenant: {TenantId}",
                            systemAction.Name, currentTenantId);

                        try
                        {
                            await CreateActionDefinitionAsync(systemAction);
                            _logger.LogInformation("Successfully created system action: {ActionName} for tenant: {TenantId}",
                                systemAction.Name, currentTenantId);

                            // Log system predefined action creation with structured logging
                            _logger.LogDebug("System predefined action auto-creation details: {@SystemActionDetails}", new
                            {
                                ActionName = systemAction.Name,
                                ActionType = systemAction.ActionType,
                                ActionConfig = systemAction.ActionConfig,
                                TriggerType = systemAction.TriggerType,
                                ActionTriggerType = systemAction.ActionTriggerType,
                                IsSystemPredefined = true,
                                AutoCreated = true,
                                TenantId = currentTenantId,
                                CreationReason = "System predefined action auto-creation during API request",
                                CreatedAt = DateTimeOffset.UtcNow
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to create system action: {ActionName} for tenant: {TenantId}",
                                systemAction.Name, currentTenantId);

                            // Log failed system predefined action creation with structured logging
                            _logger.LogDebug("System predefined action creation failed: {@FailureDetails}", new
                            {
                                ActionName = systemAction.Name,
                                ActionType = systemAction.ActionType,
                                IsSystemPredefined = true,
                                AutoCreated = false,
                                FailureReason = ex.Message,
                                TenantId = currentTenantId,
                                AttemptedAt = DateTimeOffset.UtcNow
                            });
                        }
                    }
                    else
                    {
                        _logger.LogDebug("System action {ActionName} already exists for tenant: {TenantId}",
                            systemAction.Name, currentTenantId);
                    }
                }

                _logger.LogInformation("System predefined actions check completed for tenant: {TenantId}", currentTenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring system predefined actions exist for tenant: {TenantId}",
                    _userContext?.TenantId ?? "DEFAULT");
                // Don't throw - this should not break the main query
            }
        }

        /// <summary>
        /// Get system predefined action definitions for creation
        /// </summary>
        private List<CreateActionDefinitionDto> GetSystemPredefinedActionDefinitions()
        {
            return new List<CreateActionDefinitionDto>
            {
                new CreateActionDefinitionDto
                {
                    Name = "Complete Stage",
                    Description = "Complete a specific stage in the workflow automatically",
                    ActionType = ActionTypeEnum.System,
                    ActionConfig = @"{""actionName"": ""CompleteStage""}",
                    IsEnabled = true,
                    IsTools = false,
                    IsAIGenerated = false,
                    TriggerType = TriggerTypeEnum.Task,
                    ActionTriggerType = "Task"
                }
            };
        }

        #endregion

        #region Action Name Synchronization

        /// <summary>
        /// Sync action name to related ChecklistTasks and Questionnaires
        /// Uses scoped repositories to avoid disposed object issues in background tasks
        /// </summary>
        private async Task SyncActionNameToRelatedEntitiesAsync(
            long actionDefinitionId, 
            string newActionName, 
            string tenantId,
            IChecklistTaskRepository checklistTaskRepository,
            IQuestionnaireRepository questionnaireRepository,
            ILogger<ActionManagementService> logger)
        {
            logger.LogInformation("Starting action name sync for action {ActionId} to '{NewName}' in tenant {TenantId}", 
                actionDefinitionId, newActionName, tenantId);

            var totalTasksUpdated = 0;
            var totalQuestionnairesUpdated = 0;

            try
            {
                // Sync to ChecklistTasks
                totalTasksUpdated = await SyncActionNameToChecklistTasksAsync(
                    actionDefinitionId, newActionName, tenantId, checklistTaskRepository, logger);
                logger.LogInformation("Synced action name to {Count} ChecklistTasks for action {ActionId}", 
                    totalTasksUpdated, actionDefinitionId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error syncing action name to ChecklistTasks for action {ActionId}", actionDefinitionId);
            }

            try
            {
                // Sync to Questionnaires
                totalQuestionnairesUpdated = await SyncActionNameToQuestionnairesAsync(
                    actionDefinitionId, newActionName, tenantId, questionnaireRepository, logger);
                logger.LogInformation("Synced action name to {Count} Questionnaires for action {ActionId}", 
                    totalQuestionnairesUpdated, actionDefinitionId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error syncing action name to Questionnaires for action {ActionId}", actionDefinitionId);
            }

            logger.LogInformation("Completed action name sync for action {ActionId}: {TaskCount} tasks, {QuestionnaireCount} questionnaires updated", 
                actionDefinitionId, totalTasksUpdated, totalQuestionnairesUpdated);
        }

        /// <summary>
        /// Sync action name to ChecklistTasks
        /// Uses scoped repository to avoid disposed object issues
        /// </summary>
        private async Task<int> SyncActionNameToChecklistTasksAsync(
            long actionDefinitionId, 
            string newActionName, 
            string tenantId,
            IChecklistTaskRepository checklistTaskRepository,
            ILogger<ActionManagementService> logger)
        {
            var updatedCount = 0;

            try
            {
                // Get all tasks that reference this action using scoped repository
                var tasks = await checklistTaskRepository.GetTasksByActionIdAsync(actionDefinitionId);
                
                // Filter by tenant to ensure tenant isolation
                tasks = tasks.Where(t => t.TenantId == tenantId && t.IsValid).ToList();

                if (!tasks.Any())
                {
                    logger.LogDebug("No ChecklistTasks found for action {ActionId} in tenant {TenantId}", 
                        actionDefinitionId, tenantId);
                    return 0;
                }

                logger.LogInformation("Found {Count} ChecklistTasks to update for action {ActionId}", 
                    tasks.Count, actionDefinitionId);

                // Update each task's ActionName
                // AOP will work correctly because we're using a scoped repository from a new scope
                foreach (var task in tasks)
                {
                    try
                    {
                        var oldName = task.ActionName;
                        task.ActionName = newActionName;
                        task.ModifyDate = DateTimeOffset.UtcNow;

                        await checklistTaskRepository.UpdateAsync(task);
                        updatedCount++;

                        logger.LogDebug("Updated ChecklistTask {TaskId} action name from '{OldName}' to '{NewName}'", 
                            task.Id, oldName, newActionName);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to update ChecklistTask {TaskId} for action {ActionId}", 
                            task.Id, actionDefinitionId);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in SyncActionNameToChecklistTasksAsync for action {ActionId}", actionDefinitionId);
                throw;
            }

            return updatedCount;
        }

        /// <summary>
        /// Sync action name to Questionnaires
        /// Uses scoped repository to avoid disposed object issues
        /// </summary>
        private async Task<int> SyncActionNameToQuestionnairesAsync(
            long actionDefinitionId, 
            string newActionName, 
            string tenantId,
            IQuestionnaireRepository questionnaireRepository,
            ILogger<ActionManagementService> logger)
        {
            var updatedCount = 0;

            try
            {
                // Use GetListWithExplicitFiltersAsync to bypass HttpContext dependency
                // Background tasks don't have HttpContext, so GetListAsync would return DEFAULT tenant
                var questionnaires = await questionnaireRepository.GetListWithExplicitFiltersAsync(tenantId, "DEFAULT");
                
                logger.LogInformation("Retrieved {Count} questionnaires for tenant {TenantId} with AppCode=DEFAULT", 
                    questionnaires.Count, tenantId);

                if (!questionnaires.Any())
                {
                    logger.LogInformation("No Questionnaires found in tenant {TenantId}. " +
                        "This is expected if no questionnaires exist or they use a different AppCode.", tenantId);
                    return 0;
                }
                
                // Log questionnaire IDs for debugging
                logger.LogDebug("Questionnaire IDs found: {Ids}", 
                    string.Join(", ", questionnaires.Select(q => q.Id)));

                logger.LogInformation("Checking {Count} Questionnaires for action {ActionId} references in tenant {TenantId}", 
                    questionnaires.Count, actionDefinitionId, tenantId);

                foreach (var questionnaire in questionnaires)
                {
                    try
                    {
                        if (questionnaire.Structure == null)
                            continue;

                        var structureJson = questionnaire.Structure.ToString(Newtonsoft.Json.Formatting.None);
                        var updated = UpdateActionNameInStructureJson(structureJson, actionDefinitionId, newActionName, out var newStructureJson, logger);

                        if (updated)
                        {
                            questionnaire.Structure = Newtonsoft.Json.Linq.JToken.Parse(newStructureJson);
                            questionnaire.ModifyDate = DateTimeOffset.UtcNow;

                            await questionnaireRepository.UpdateAsync(questionnaire);
                            updatedCount++;

                            logger.LogDebug("Updated Questionnaire {QuestionnaireId} ('{Name}') with new action name '{NewName}'", 
                                questionnaire.Id, questionnaire.Name, newActionName);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to update Questionnaire {QuestionnaireId} for action {ActionId}", 
                            questionnaire.Id, actionDefinitionId);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in SyncActionNameToQuestionnairesAsync for action {ActionId}", actionDefinitionId);
                throw;
            }

            return updatedCount;
        }

        /// <summary>
        /// Update action name in questionnaire structure JSON
        /// </summary>
        private bool UpdateActionNameInStructureJson(
            string structureJson, 
            long actionDefinitionId, 
            string newActionName, 
            out string newStructureJson,
            ILogger<ActionManagementService> logger)
        {
            newStructureJson = structureJson;
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
                        // Process questions in sections
                        hasChanges |= UpdateActionNameInQuestionArray(section, structureObj.sections[sectionIndex], 
                            "questions", actionDefinitionId, newActionName, logger);

                        // Process subsections if they exist
                        if (section.TryGetProperty("subsections", out var subsectionsElement))
                        {
                            var subsectionIndex = 0;
                            foreach (var subsection in subsectionsElement.EnumerateArray())
                            {
                                hasChanges |= UpdateActionNameInQuestionArray(subsection, 
                                    structureObj.sections[sectionIndex].subsections[subsectionIndex],
                                    "questions", actionDefinitionId, newActionName, logger);
                                subsectionIndex++;
                            }
                        }
                        sectionIndex++;
                    }
                }

                if (hasChanges)
                {
                    newStructureJson = Newtonsoft.Json.JsonConvert.SerializeObject(structureObj);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error updating action name in structure JSON for action {ActionId}", actionDefinitionId);
                return false;
            }

            return hasChanges;
        }

        /// <summary>
        /// Update action name in question array
        /// </summary>
        private bool UpdateActionNameInQuestionArray(
            JsonElement section, 
            dynamic sectionObj, 
            string arrayName, 
            long actionDefinitionId, 
            string newActionName,
            ILogger<ActionManagementService> logger)
        {
            var hasChanges = false;

            if (section.TryGetProperty(arrayName, out var questionsElement) && questionsElement.ValueKind == JsonValueKind.Array)
            {
                var questionIndex = 0;
                foreach (var question in questionsElement.EnumerateArray())
                {
                    // Check and update question action
                    if (question.TryGetProperty("action", out var actionElement))
                    {
                        if (actionElement.TryGetProperty("id", out var actionIdEl) &&
                            actionIdEl.GetString() == actionDefinitionId.ToString())
                        {
                            sectionObj[arrayName][questionIndex].action.name = newActionName;
                            hasChanges = true;
                            logger.LogDebug("Updated action name in question at index {Index}", questionIndex);
                        }
                    }

                    // Check and update options actions
                    if (question.TryGetProperty("options", out var optionsElement) && optionsElement.ValueKind == JsonValueKind.Array)
                    {
                        var optionIndex = 0;
                        foreach (var option in optionsElement.EnumerateArray())
                        {
                            if (option.TryGetProperty("action", out var optionActionElement))
                            {
                                if (optionActionElement.TryGetProperty("id", out var optionActionIdEl) &&
                                    optionActionIdEl.GetString() == actionDefinitionId.ToString())
                                {
                                    sectionObj[arrayName][questionIndex].options[optionIndex].action.name = newActionName;
                                    hasChanges = true;
                                    logger.LogDebug("Updated action name in option at question index {QuestionIndex}, option index {OptionIndex}", 
                                        questionIndex, optionIndex);
                                }
                            }
                            optionIndex++;
                        }
                    }
                    questionIndex++;
                }
            }

            return hasChanges;
        }

        #endregion
    }
}