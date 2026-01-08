using System.Collections.Generic;
using Newtonsoft.Json;
using FlowFlex.Domain.Shared.JsonConverters;

namespace FlowFlex.Application.Contracts.Dtos.OW.StageCondition
{
    /// <summary>
    /// Condition Evaluation Result
    /// </summary>
    public class ConditionEvaluationResult
    {
        /// <summary>
        /// Whether the condition is met
        /// </summary>
        public bool IsConditionMet { get; set; }

        /// <summary>
        /// Rule evaluation details
        /// </summary>
        public List<RuleEvaluationDetail> RuleResults { get; set; } = new List<RuleEvaluationDetail>();

        /// <summary>
        /// Error message (if evaluation failed)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Next Stage ID to navigate to
        /// </summary>
        [JsonConverter(typeof(LongToStringConverter))]
        public long? NextStageId { get; set; }

        /// <summary>
        /// Action execution results
        /// </summary>
        public List<ActionExecutionDetail>? ActionResults { get; set; }
    }

    /// <summary>
    /// Rule Evaluation Detail
    /// </summary>
    public class RuleEvaluationDetail
    {
        /// <summary>
        /// Rule Name
        /// </summary>
        public string RuleName { get; set; } = string.Empty;

        /// <summary>
        /// Whether the rule passed
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Rule Expression
        /// </summary>
        public string Expression { get; set; } = string.Empty;

        /// <summary>
        /// Actual value evaluated
        /// </summary>
        public object? ActualValue { get; set; }

        /// <summary>
        /// Error message (if rule evaluation failed)
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Action Execution Detail
    /// </summary>
    public class ActionExecutionDetail
    {
        /// <summary>
        /// Action Type
        /// </summary>
        public string ActionType { get; set; } = string.Empty;

        /// <summary>
        /// Execution Order
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Whether the action succeeded
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message (if action failed)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Additional result data
        /// </summary>
        public Dictionary<string, object>? ResultData { get; set; }
    }

    /// <summary>
    /// Action Execution Result
    /// </summary>
    public class ActionExecutionResult
    {
        /// <summary>
        /// Overall success status
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Execution details for each action
        /// </summary>
        public List<ActionExecutionDetail> Details { get; set; } = new List<ActionExecutionDetail>();
    }

    /// <summary>
    /// Action Execution Context
    /// </summary>
    public class ActionExecutionContext
    {
        /// <summary>
        /// Onboarding ID
        /// </summary>
        public long OnboardingId { get; set; }

        /// <summary>
        /// Current Stage ID
        /// </summary>
        public long StageId { get; set; }

        /// <summary>
        /// Condition ID
        /// </summary>
        public long ConditionId { get; set; }

        /// <summary>
        /// Tenant ID
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// User ID
        /// </summary>
        public long UserId { get; set; }
    }
}
