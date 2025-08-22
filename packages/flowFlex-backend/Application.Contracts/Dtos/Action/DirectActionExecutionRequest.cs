using FlowFlex.Domain.Shared.Enums.Action;

namespace FlowFlex.Application.Contracts.Dtos.Action
{
    /// <summary>
    /// Direct action execution request DTO
    /// </summary>
    public class DirectActionExecutionRequest
    {
        /// <summary>
        /// Action type (Python, HttpApi, SendEmail)
        /// </summary>
        public ActionTypeEnum ActionType { get; set; }

        /// <summary>
        /// Action configuration JSON string
        /// </summary>
        public string ActionConfig { get; set; } = string.Empty;

        /// <summary>
        /// Context data for execution (optional)
        /// </summary>
        public object? ContextData { get; set; }
    }
}