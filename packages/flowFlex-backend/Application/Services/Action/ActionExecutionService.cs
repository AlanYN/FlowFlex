using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Domain.Repository.Action;
using FlowFlex.Domain.Shared.Enums.Action;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Newtonsoft.Json.Linq;

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
        private readonly ILogger<ActionExecutionService> _logger;

        public ActionExecutionService(
            IActionDefinitionRepository actionDefinitionRepository,
            IActionExecutionRepository actionExecutionRepository,
            IActionExecutionFactory actionExecutorFactory,
            ILogger<ActionExecutionService> logger)
        {
            _actionDefinitionRepository = actionDefinitionRepository;
            _actionExecutionRepository = actionExecutionRepository;
            _actionExecutorFactory = actionExecutorFactory;
            _logger = logger;
        }

        public async Task ExecuteActionAsync(
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
                    return;
                }

                // Create execution record
                var execution = new Domain.Entities.Action.ActionExecution
                {
                    ActionDefinitionId = actionDefinitionId,
                    ExecutionStatus = ActionExecutionStatusEnum.Running.ToString(),
                    StartedAt = DateTime.UtcNow,
                    TriggerContext = contextData != null ? JObject.FromObject(contextData) : new JObject(),
                    CreateBy = userId.ToString() ?? "",
                    ExecutionId = SnowFlakeSingle.Instance.NextId().ToString()
                };

                await _actionExecutionRepository.InsertAsync(execution, cancellationToken);

                try
                {
                    // Get executor and execute
                    var executor = _actionExecutorFactory.CreateExecutor((ActionTypeEnum)Enum.Parse(typeof(ActionTypeEnum), actionDefinition.ActionType));
                    var result = await executor.ExecuteAsync(actionDefinition.ActionConfig, contextData);

                    // Update execution record with success
                    execution.ExecutionStatus = ActionExecutionStatusEnum.Completed.ToString();
                    execution.CompletedAt = DateTime.UtcNow;
                    execution.ExecutionOutput = result != null ? JObject.FromObject(result) : new JObject();
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExecuteActionAsync: ActionId={ActionId}", actionDefinitionId);
                throw;
            }
        }
    }
}