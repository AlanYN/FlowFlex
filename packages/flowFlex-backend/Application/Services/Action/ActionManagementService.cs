using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Domain.Entities.Action;
using FlowFlex.Domain.Repository.Action;
using FlowFlex.Domain.Shared.Enums.Action;
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
        private readonly IMapper _mapper;
        private readonly ILogger<ActionManagementService> _logger;

        public ActionManagementService(
            IActionDefinitionRepository actionDefinitionRepository,
            IActionTriggerMappingRepository actionTriggerMappingRepository,
            IActionCodeGeneratorService actionCodeGeneratorService,
            IChecklistTaskRepository checklistTaskRepository,
            IQuestionnaireRepository questionnaireRepository,
            IMapper mapper,
            ILogger<ActionManagementService> logger)
        {
            _actionDefinitionRepository = actionDefinitionRepository;
            _actionTriggerMappingRepository = actionTriggerMappingRepository;
            _actionCodeGeneratorService = actionCodeGeneratorService;
            _checklistTaskRepository = checklistTaskRepository;
            _questionnaireRepository = questionnaireRepository;
            _mapper = mapper;
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
            bool? isAssignmentWorkflow = null)
        {
            var (data, total) = await _actionDefinitionRepository.GetPagedAsync(1,
                10000,
                actionType.ToString(),
                search,
                isAssignmentStage,
                isAssignmentChecklist,
                isAssignmentQuestionnaire,
                isAssignmentWorkflow);

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
            var exists = await _actionTriggerMappingRepository.IsMappingExistsAsync(
                dto.ActionDefinitionId, dto.TriggerType, dto.TriggerSourceId, dto.WorkFlowId);

            if (exists)
            {
                throw new InvalidOperationException("Mapping already exists for this action and trigger");
            }

            // Ensure TriggerEvent has a valid value - default to "Completed" if not provided or empty
            if (string.IsNullOrWhiteSpace(dto.TriggerEvent))
            {
                dto.TriggerEvent = "Completed";
                _logger.LogDebug("TriggerEvent was null/empty, defaulting to 'Completed' for mapping: ActionDefinitionId={ActionDefinitionId}, TriggerSourceId={TriggerSourceId}",
                    dto.ActionDefinitionId, dto.TriggerSourceId);
            }

            var entity = _mapper.Map<ActionTriggerMapping>(dto);

            await _actionTriggerMappingRepository.InsertAsync(entity);
            _logger.LogInformation("Created action trigger mapping: {MappingId} with TriggerEvent: {TriggerEvent}", entity.Id, entity.TriggerEvent);

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

            entity.IsEnabled = isEnabled;
            entity.ModifyDate = DateTimeOffset.UtcNow;

            await _actionTriggerMappingRepository.UpdateAsync(entity);
            _logger.LogInformation("Updated action trigger mapping status: {MappingId}, IsEnabled: {IsEnabled}", id, isEnabled);

            return true;
        }

        public async Task<bool> BatchUpdateActionTriggerMappingStatusAsync(List<long> mappingIds, bool isEnabled)
        {
            return await _actionTriggerMappingRepository.BatchUpdateEnabledStatusAsync(mappingIds, isEnabled);
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
    }
}