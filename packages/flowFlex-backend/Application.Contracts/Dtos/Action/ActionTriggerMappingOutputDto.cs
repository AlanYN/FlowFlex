namespace FlowFlex.Application.Contracts.Dtos.Action
{
    /// <summary>
    /// Action trigger mapping output DTO
    /// </summary>
    public class ActionTriggerMappingOutputDto
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
        /// Trigger type (Stage, Task, Question, Workflow, Checklist)
        /// </summary>
        public string TriggerType { get; set; } = string.Empty;

        /// <summary>
        /// Trigger source ID
        /// </summary>
        public long TriggerSourceId { get; set; }

        /// <summary>
        /// Trigger event (Started, Completed, Failed, etc.)
        /// </summary>
        public string TriggerEvent { get; set; } = string.Empty;

        /// <summary>
        /// Trigger conditions (JSON format)
        /// </summary>
        public Dictionary<string, object>? TriggerConditions { get; set; }

        /// <summary>
        /// Is enabled
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Execution order
        /// </summary>
        public int ExecutionOrder { get; set; }

        /// <summary>
        /// Mapping configuration (JSON format)
        /// </summary>
        public Dictionary<string, object>? MappingConfig { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Associated action definition
        /// </summary>
        public ActionDefinitionOutputDto? ActionDefinition { get; set; }

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