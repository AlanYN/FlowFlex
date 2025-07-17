using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Domain.Shared.Events.Action;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Notification.Action
{
    /// <summary>
    /// Unified event handler for all action trigger events
    /// </summary>
    public class ActionTriggerEventHandler : INotificationHandler<ActionTriggerEvent>
    {
        private readonly IActionTriggerService _actionTriggerService;
        private readonly ILogger<ActionTriggerEventHandler> _logger;

        public ActionTriggerEventHandler(
            IActionTriggerService actionTriggerService,
            ILogger<ActionTriggerEventHandler> logger)
        {
            _actionTriggerService = actionTriggerService;
            _logger = logger;
        }

        public async Task Handle(ActionTriggerEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Processing action trigger event: SourceType={SourceType}, SourceId={SourceId}, EventType={EventType}",
                    notification.TriggerSourceType,
                    notification.TriggerSourceId,
                    notification.TriggerEventType);

                // Find and execute actions for this trigger
                await _actionTriggerService.ExecuteActionsForTriggerAsync(
                    notification.TriggerSourceType,
                    notification.TriggerSourceId,
                    notification.TriggerEventType,
                    notification.ContextData,
                    notification.UserId,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing action trigger event: SourceType={SourceType}, SourceId={SourceId}, EventType={EventType}",
                    notification.TriggerSourceType,
                    notification.TriggerSourceId,
                    notification.TriggerEventType);
                throw;
            }
        }
    }
}