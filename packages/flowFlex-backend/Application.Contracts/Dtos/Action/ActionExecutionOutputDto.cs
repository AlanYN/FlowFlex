namespace FlowFlex.Application.Contracts.Dtos.Action
{
    /// <summary>
    /// Action execution output DTO
    /// </summary>
    public class ActionExecutionOutputDto
    {
        /// <summary>
        /// ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Action definition ID
        /// </summary>
        public long ActionDefinitionId { get; set; }

        /// <summary>
        /// Action trigger mapping ID
        /// </summary>
        public long? ActionTriggerMappingId { get; set; }

        /// <summary>
        /// Execution ID (unique identifier)
        /// </summary>
        public string ExecutionId { get; set; } = string.Empty;

        /// <summary>
        /// Action name
        /// </summary>
        public string ActionName { get; set; } = string.Empty;

        /// <summary>
        /// Action type
        /// </summary>
        public string ActionType { get; set; } = string.Empty;

        /// <summary>
        /// Trigger context
        /// </summary>
        public Dictionary<string, object>? TriggerContext { get; set; }

        /// <summary>
        /// Execution status (Pending, Running, Completed, Failed, Cancelled, Timeout, Retrying)
        /// </summary>
        public string ExecutionStatus { get; set; } = string.Empty;

        /// <summary>
        /// Started at
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// Completed at
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Duration in milliseconds
        /// </summary>
        public long? DurationMs { get; set; }

        /// <summary>
        /// Execution input
        /// </summary>
        public Dictionary<string, object>? ExecutionInput { get; set; }

        /// <summary>
        /// Execution output
        /// </summary>
        public Dictionary<string, object>? ExecutionOutput { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Error stack trace
        /// </summary>
        public string ErrorStackTrace { get; set; } = string.Empty;

        /// <summary>
        /// Executor information
        /// </summary>
        public Dictionary<string, object>? ExecutorInfo { get; set; }

        /// <summary>
        /// Associated action definition
        /// </summary>
        public ActionDefinitionOutputDto? ActionDefinition { get; set; }

        /// <summary>
        /// Associated trigger mapping
        /// </summary>
        public ActionTriggerMappingOutputDto? ActionTriggerMapping { get; set; }

        /// <summary>
        /// Create date
        /// </summary>
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// Modify date
        /// </summary>
        public DateTime ModifyDate { get; set; }

        /// <summary>
        /// Created by
        /// </summary>
        public string CreateBy { get; set; } = string.Empty;

        /// <summary>
        /// Modified by
        /// </summary>
        public string ModifyBy { get; set; } = string.Empty;

        /// <summary>
        /// Tenant ID
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// App code
        /// </summary>
        public string AppCode { get; set; } = string.Empty;
    }
} 