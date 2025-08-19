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
        private readonly IMapper _mapper;
        private readonly ILogger<ActionManagementService> _logger;

        public ActionManagementService(
            IActionDefinitionRepository actionDefinitionRepository,
            IActionTriggerMappingRepository actionTriggerMappingRepository,
            IActionCodeGeneratorService actionCodeGeneratorService,
            IMapper mapper,
            ILogger<ActionManagementService> logger)
        {
            _actionDefinitionRepository = actionDefinitionRepository;
            _actionTriggerMappingRepository = actionTriggerMappingRepository;
            _actionCodeGeneratorService = actionCodeGeneratorService;
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
            bool? isAssignmentWorkflow = null)
        {
            var (data, total) = await _actionDefinitionRepository.GetPagedAsync(pageIndex,
                pageSize,
                actionType.ToString(),
                search,
                isAssignmentStage,
                isAssignmentChecklist,
                isAssignmentQuestionnaire,
                isAssignmentWorkflow);

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
                return false;
            }

            entity.IsValid = false;
            entity.ModifyDate = DateTimeOffset.UtcNow;

            await _actionDefinitionRepository.UpdateAsync(entity);
            _logger.LogInformation("Deleted action definition: {ActionId}", id);

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

            var entity = _mapper.Map<ActionTriggerMapping>(dto);

            await _actionTriggerMappingRepository.InsertAsync(entity);
            _logger.LogInformation("Created action trigger mapping: {MappingId}", entity.Id);

            return _mapper.Map<ActionTriggerMappingDto>(entity);
        }

        public async Task<bool> DeleteActionTriggerMappingAsync(long id)
        {
            var entity = await _actionTriggerMappingRepository.GetByIdAsync(id);
            if (entity == null)
            {
                return false;
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
    }
}