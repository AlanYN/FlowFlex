using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Application.Services.Action.Executors;
using FlowFlex.Domain.Shared.Enums.Action;

namespace FlowFlex.Application.Services.Action
{
    /// <summary>
    /// Action execution factory - creates action executor instances
    /// </summary>
    public class ActionExecutionFactory : IActionExecutionFactory
    {
        private readonly ILogger<ActionExecutionFactory> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<ActionTypeEnum, Type> _executorTypes;

        public ActionExecutionFactory(
            ILogger<ActionExecutionFactory> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;

            // Register all executor types - add new executors here for future extension
            _executorTypes = new Dictionary<ActionTypeEnum, Type>
            {
                { ActionTypeEnum.Python, typeof(PythonActionExecutor) },
                { ActionTypeEnum.HttpApi, typeof(HttpApiActionExecutor) },
                { ActionTypeEnum.SendEmail, typeof(EmailActionExecutor) }
            };
        }

        /// <summary>
        /// Create action executor instance by action type
        /// </summary>
        public IActionExecutor CreateExecutor(ActionTypeEnum actionType)
        {
            if (_executorTypes.TryGetValue(actionType, out var executorType))
            {
                _logger.LogDebug("Creating executor for action type: {ActionType}", actionType);
                return (IActionExecutor)_serviceProvider.GetRequiredService(executorType);
            }

            var supportedTypes = string.Join(", ", _executorTypes.Keys);
            throw new NotSupportedException($"Action type '{actionType}' is not supported. Supported types: {supportedTypes}");
        }
    }
}