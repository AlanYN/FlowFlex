using System.Collections.Generic;
using Newtonsoft.Json;
using FlowFlex.Domain.Shared.JsonConverters;

namespace FlowFlex.Application.Contracts.Dtos.OW.StageCondition
{
    /// <summary>
    /// Checklist Component Data
    /// </summary>
    public class ChecklistData
    {
        /// <summary>
        /// Overall completion status (Completed/Pending)
        /// </summary>
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Number of completed tasks
        /// </summary>
        public int CompletedCount { get; set; }

        /// <summary>
        /// Total number of tasks
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Completion percentage
        /// </summary>
        public decimal CompletionPercentage => TotalCount > 0 ? (decimal)CompletedCount / TotalCount * 100 : 0;

        /// <summary>
        /// Individual task statuses
        /// </summary>
        public List<TaskStatusData> Tasks { get; set; } = new List<TaskStatusData>();
    }

    /// <summary>
    /// Task Status Data
    /// </summary>
    public class TaskStatusData
    {
        /// <summary>
        /// Task ID
        /// </summary>
        [JsonConverter(typeof(LongToStringConverter))]
        public long TaskId { get; set; }

        /// <summary>
        /// Checklist ID
        /// </summary>
        [JsonConverter(typeof(LongToStringConverter))]
        public long ChecklistId { get; set; }

        /// <summary>
        /// Task Name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Is Completed
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Completion Notes
        /// </summary>
        public string? CompletionNotes { get; set; }
    }

    /// <summary>
    /// Questionnaire Component Data
    /// </summary>
    public class QuestionnaireData
    {
        /// <summary>
        /// Overall completion status
        /// </summary>
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Total score (if applicable)
        /// </summary>
        public decimal? TotalScore { get; set; }

        /// <summary>
        /// Question answers
        /// </summary>
        public Dictionary<string, object> Answers { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Attachment Component Data
    /// </summary>
    public class AttachmentData
    {
        /// <summary>
        /// Number of files
        /// </summary>
        public int FileCount { get; set; }

        /// <summary>
        /// Whether has any attachment
        /// </summary>
        public bool HasAttachment => FileCount > 0;

        /// <summary>
        /// Total file size in bytes
        /// </summary>
        public long TotalSize { get; set; }

        /// <summary>
        /// File names
        /// </summary>
        public List<string> FileNames { get; set; } = new List<string>();
    }

    /// <summary>
    /// Available Component for condition configuration
    /// </summary>
    public class AvailableComponent
    {
        /// <summary>
        /// Component ID
        /// </summary>
        [JsonConverter(typeof(LongToStringConverter))]
        public long ComponentId { get; set; }

        /// <summary>
        /// Component Type (Fields, Checklist, Questionnaire, FileAttachments)
        /// </summary>
        public string ComponentType { get; set; } = string.Empty;

        /// <summary>
        /// Component Name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Stage ID this component belongs to
        /// </summary>
        [JsonConverter(typeof(LongToStringConverter))]
        public long StageId { get; set; }

        /// <summary>
        /// Stage Name
        /// </summary>
        public string StageName { get; set; } = string.Empty;

        /// <summary>
        /// Stage Order
        /// </summary>
        public int StageOrder { get; set; }
    }

    /// <summary>
    /// Available Field for condition configuration
    /// </summary>
    public class AvailableField
    {
        /// <summary>
        /// Field Key/Path
        /// </summary>
        public string FieldKey { get; set; } = string.Empty;

        /// <summary>
        /// Field Name
        /// </summary>
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// Field Type (string, number, boolean, date, etc.)
        /// </summary>
        public string FieldType { get; set; } = string.Empty;

        /// <summary>
        /// Expression path for RulesEngine
        /// </summary>
        public string ExpressionPath { get; set; } = string.Empty;
    }
}
