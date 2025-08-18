using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Domain.Repository.Action;
using FlowFlex.Domain.Shared.Enums.Action;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace FlowFlex.Application.Services.Action
{
    /// <summary>
    /// Service for executing individual actions
    /// </summary>
    public class ActionExecutionService : IActionExecutionService
    {
        private readonly IActionDefinitionRepository _actionDefinitionRepository;
        private readonly IActionExecutionRepository _actionExecutionRepository;
        private readonly IActionExecutionFactory _actionExecutorFactory;
        private readonly IActionManagementService _actionManagementService;
        private readonly ILogger<ActionExecutionService> _logger;

        public ActionExecutionService(
            IActionDefinitionRepository actionDefinitionRepository,
            IActionExecutionRepository actionExecutionRepository,
            IActionExecutionFactory actionExecutorFactory,
            IActionManagementService actionManagementService,
            ILogger<ActionExecutionService> logger)
        {
            _actionDefinitionRepository = actionDefinitionRepository;
            _actionExecutionRepository = actionExecutionRepository;
            _actionExecutorFactory = actionExecutorFactory;
            _actionManagementService = actionManagementService;
            _logger = logger;
        }

        public async Task<JToken?> ExecuteActionAsync(
            long actionDefinitionId,
            object contextData = null,
            long? userId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Get action definition
                var actionDefinition = await _actionDefinitionRepository.GetByIdAsync(actionDefinitionId);
                if (actionDefinition == null)
                {
                    _logger.LogWarning("Action definition not found: {ActionId}", actionDefinitionId);
                    return null;
                }

                // Create execution record
                var execution = new Domain.Entities.Action.ActionExecution
                {
                    ActionDefinitionId = actionDefinitionId,
                    ExecutionStatus = ActionExecutionStatusEnum.Running.ToString(),
                    StartedAt = DateTime.UtcNow,
                    TriggerContext = contextData != null ? JToken.FromObject(contextData) : new JObject(),
                    CreateBy = userId.ToString() ?? "",
                    ExecutionId = SnowFlakeSingle.Instance.NextId().ToString(),
                    CreateDate = DateTimeOffset.UtcNow
                };

                await _actionExecutionRepository.InsertAsync(execution, cancellationToken);

                try
                {
                    // Get executor and execute
                    var executor = _actionExecutorFactory.CreateExecutor((ActionTypeEnum)Enum.Parse(typeof(ActionTypeEnum), actionDefinition.ActionType));
                    var result = await executor.ExecuteAsync(JsonConvert.SerializeObject(actionDefinition.ActionConfig), contextData);

                    // Update execution record with success
                    execution.ExecutionStatus = ActionExecutionStatusEnum.Completed.ToString();
                    execution.CompletedAt = DateTime.UtcNow;
                    execution.ExecutionOutput = result != null ? JToken.FromObject(result) : new JObject();
                    execution.ModifyDate = DateTimeOffset.Now;
                    await _actionExecutionRepository.UpdateAsync(execution);

                    _logger.LogInformation(
                        "Action executed successfully: ActionId={ActionId}, ExecutionId={ExecutionId}",
                        actionDefinitionId,
                        execution.Id);
                }
                catch (Exception ex)
                {
                    // Update execution record with failure
                    execution.ExecutionStatus = ActionExecutionStatusEnum.Failed.ToString();
                    execution.CompletedAt = DateTime.UtcNow;
                    execution.ErrorMessage = ex.Message;
                    await _actionExecutionRepository.UpdateAsync(execution);

                    _logger.LogError(ex,
                        "Action execution failed: ActionId={ActionId}, ExecutionId={ExecutionId}",
                        actionDefinitionId,
                        execution.Id);
                    throw;
                }
                return execution.ExecutionOutput;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExecuteActionAsync: ActionId={ActionId}", actionDefinitionId);
                throw;
            }
        }

        public async Task<object> ExecuteActionDirectlyAsync(
            ActionTypeEnum actionType,
            string actionConfig,
            object contextData = null)
        {
            try
            {
                _logger.LogInformation("Executing action directly: ActionType={ActionType}", actionType);

                // Validate action config using ActionManagementService
                _actionManagementService.ValidateActionConfig(actionType, actionConfig);

                // Create executor and execute
                var executor = _actionExecutorFactory.CreateExecutor(actionType);
                var result = await executor.ExecuteAsync(actionConfig, contextData);

                _logger.LogInformation("Action executed successfully: ActionType={ActionType}", actionType);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExecuteActionDirectlyAsync: ActionType={ActionType}", actionType);
                throw;
            }
        }
    }
}