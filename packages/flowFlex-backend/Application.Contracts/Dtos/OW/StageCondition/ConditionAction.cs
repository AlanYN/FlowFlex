using System.Collections.Generic;
using Newtonsoft.Json;
using FlowFlex.Domain.Shared.JsonConverters;

namespace FlowFlex.Application.Contracts.Dtos.OW.StageCondition
{
    /// <summary>
    /// Condition Action Type Enum
    /// </summary>
    public enum ConditionActionType
    {
        /// <summary>
        /// Go to a specific stage
        /// </summary>
        GoToStage = 1,

        /// <summary>
        /// Skip the next stage
        /// </summary>
        SkipStage = 2,

        /// <summary>
        /// End the workflow
        /// </summary>
        EndWorkflow = 3,

        /// <summary>
        /// Send notification
        /// </summary>
        SendNotification = 4,

        /// <summary>
        /// Update a field value
        /// </summary>
        UpdateField = 5,

        /// <summary>
        /// Trigger a predefined action
        /// </summary>
        TriggerAction = 6,

        /// <summary>
        /// Assign user to onboarding
        /// </summary>
        AssignUser = 7
    }

    /// <summary>
    /// Condition Action Configuration
    /// </summary>
    public class ConditionAction
    {
        /// <summary>
        /// Action Type
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Execution Order
        /// </summary>
        [JsonProperty("order")]
        public int Order { get; set; } = 1;

        /// <summary>
        /// Target Stage ID (for GoToStage action)
        /// </summary>
        [JsonProperty("targetStageId")]
        [JsonConverter(typeof(LongToStringConverter))]
        public long? TargetStageId { get; set; }

        /// <summary>
        /// Number of stages to skip (for SkipStage action)
        /// </summary>
        [JsonProperty("skipCount")]
        public int SkipCount { get; set; } = 1;

        /// <summary>
        /// Workflow end status (for EndWorkflow action)
        /// </summary>
        [JsonProperty("endStatus")]
        public string? EndStatus { get; set; }

        /// <summary>
        /// Notification recipient type (for SendNotification action)
        /// </summary>
        [JsonProperty("recipientType")]
        public string? RecipientType { get; set; }

        /// <summary>
        /// Notification recipient ID (for SendNotification action)
        /// </summary>
        [JsonProperty("recipientId")]
        public string? RecipientId { get; set; }

        /// <summary>
        /// Notification template ID (for SendNotification action)
        /// </summary>
        [JsonProperty("templateId")]
        public string? TemplateId { get; set; }

        /// <summary>
        /// Stage ID for field update (for UpdateField action)
        /// </summary>
        [JsonProperty("stageId")]
        [JsonConverter(typeof(LongToStringConverter))]
        public long? StageId { get; set; }

        /// <summary>
        /// Field ID to update (for UpdateField action)
        /// </summary>
        [JsonProperty("fieldId")]
        public string? FieldId { get; set; }

        /// <summary>
        /// Field name to update (for UpdateField action)
        /// </summary>
        [JsonProperty("fieldName")]
        public string? FieldName { get; set; }

        /// <summary>
        /// Field value to set (for UpdateField action)
        /// </summary>
        [JsonProperty("fieldValue")]
        public object? FieldValue { get; set; }

        /// <summary>
        /// Action Definition ID (for TriggerAction action)
        /// </summary>
        [JsonProperty("actionDefinitionId")]
        [JsonConverter(typeof(LongToStringConverter))]
        public long? ActionDefinitionId { get; set; }

        /// <summary>
        /// User ID to assign (for AssignUser action)
        /// </summary>
        [JsonProperty("userId")]
        [JsonConverter(typeof(LongToStringConverter))]
        public long? UserId { get; set; }

        /// <summary>
        /// Team ID to assign (for AssignUser action)
        /// </summary>
        [JsonProperty("teamId")]
        public string? TeamId { get; set; }

        /// <summary>
        /// Additional parameters
        /// </summary>
        [JsonProperty("parameters")]
        public Dictionary<string, object>? Parameters { get; set; }
    }
}
