using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Domain.Repository.Action;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Services.Action
{
    /// <summary>
    /// Service for triggering and executing actions based on business events
    /// </summary>
    public class ActionTriggerService : IActionTriggerService
    {
        private readonly IActionTriggerMappingRepository _actionTriggerMappingRepository;
        private readonly IActionExecutionService _actionExecutionService;
        private readonly ILogger<ActionTriggerService> _logger;

        public ActionTriggerService(
            IActionTriggerMappingRepository actionTriggerMappingRepository,
            IActionExecutionService actionExecutionService,
            ILogger<ActionTriggerService> logger)
        {
            _actionTriggerMappingRepository = actionTriggerMappingRepository;
            _actionExecutionService = actionExecutionService;
            _logger = logger;
        }

        public async Task ExecuteActionsForTriggerAsync(
            string triggerSourceType,
            long triggerSourceId,
            string triggerEventType,
            object contextData = null,
            long? userId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Find action mappings for this trigger
                var actionMappings = await _actionTriggerMappingRepository.GetMappingsForTriggerAsync(
                    triggerSourceType,
                    triggerSourceId,
                    triggerEventType,
                    cancellationToken);

                if (!actionMappings.Any())
                {
                    _logger.LogDebug(
                        "No action mappings found for trigger: SourceType={SourceType}, SourceId={SourceId}, EventType={EventType}",
                        triggerSourceType,
                        triggerSourceId,
                        triggerEventType);
                    return;
                }

                _logger.LogInformation(
                    "Found {Count} action mappings for trigger: SourceType={SourceType}, SourceId={SourceId}, EventType={EventType}",
                    actionMappings.Count(),
                    triggerSourceType,
                    triggerSourceId,
                    triggerEventType);

                // Execute actions in order
                foreach (var mapping in actionMappings.OrderBy(m => m.ExecutionOrder))
                {
                    try
                    {
                        await _actionExecutionService.ExecuteActionAsync(
                            mapping.ActionDefinitionId,
                            contextData,
                            userId,
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error executing action {ActionId} for mapping {MappingId}",
                            mapping.ActionDefinitionId,
                            mapping.Id);
                        // Continue with next action instead of stopping all
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in ExecuteActionsForTriggerAsync: SourceType={SourceType}, SourceId={SourceId}, EventType={EventType}",
                    triggerSourceType,
                    triggerSourceId,
                    triggerEventType);
                throw;
            }
        }
    }
}