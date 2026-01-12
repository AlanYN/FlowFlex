using System;
using System.Collections.Generic;
using MediatR;

namespace FlowFlex.Domain.Shared.Events
{
    /// <summary>
    /// Onboarding stage completed event
    /// </summary>
    public class OnboardingStageCompletedEvent : INotification
    {
        /// <summary>
        /// Event ID
        /// </summary>
        public string EventId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Event timestamp
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Event version
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Tenant ID
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// User ID who triggered the event
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// User name who triggered the event
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Onboarding ID
        /// </summary>
        public long OnboardingId { get; set; }

        /// <summary>
        /// Lead ID
        /// </summary>
        public string LeadId { get; set; }

        /// <summary>
        /// Workflow ID
        /// </summary>
        public long WorkflowId { get; set; }

        /// <summary>
        /// Workflow name
        /// </summary>
        public string WorkflowName { get; set; }

        /// <summary>
        /// Completed stage ID
        /// </summary>
        public long CompletedStageId { get; set; }

        /// <summary>
        /// Completed stage name
        /// </summary>
        public string CompletedStageName { get; set; }

        /// <summary>
        /// Stage type/category
        /// </summary>
        public string StageCategory { get; set; }

        /// <summary>
        /// Next stage ID
        /// </summary>
        public long? NextStageId { get; set; }

        /// <summary>
        /// Next stage name
        /// </summary>
        public string NextStageName { get; set; }

        /// <summary>
        /// Completion rate (0-100)
        /// </summary>
        public decimal CompletionRate { get; set; }

        /// <summary>
        /// Whether it's the final stage
        /// </summary>
        public bool IsFinalStage { get; set; }

        /// <summary>
        /// Responsible team
        /// </summary>
        public string ResponsibleTeam { get; set; }

        /// <summary>
        /// Assignee ID
        /// </summary>
        public long? AssigneeId { get; set; }

        /// <summary>
        /// Assignee name
        /// </summary>
        public string AssigneeName { get; set; }

        /// <summary>
        /// Business context data
        /// </summary>
        public Dictionary<string, object> BusinessContext { get; set; } = new();

        /// <summary>
        /// Routing tags - for event routing
        /// </summary>
        public List<string> RoutingTags { get; set; } = new();

        /// <summary>
        /// Priority
        /// </summary>
        public string Priority { get; set; } = "Medium";

        /// <summary>
        /// Event source
        /// </summary>
        public string Source { get; set; } = "Unis.CRM.Onboarding";

        /// <summary>
        /// Related entity information
        /// </summary>
        public RelatedEntityInfo RelatedEntity { get; set; }

        /// <summary>
        /// Event description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Event tags
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// Components payload: checklist, tasks, questionnaires, answers, required fields, etc.
        /// </summary>
        public StageCompletionComponents Components { get; set; }
    }

    /// <summary>
    /// Related entity information
    /// </summary>
    public class RelatedEntityInfo
    {
        /// <summary>
        /// Entity type (Lead, Customer, Deal, etc.)
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// Entity ID
        /// </summary>
        public string EntityId { get; set; }

        /// <summary>
        /// Entity name
        /// </summary>
        public string EntityName { get; set; }

        /// <summary>
        /// Entity status
        /// </summary>
        public string EntityStatus { get; set; }

        /// <summary>
        /// Extended properties
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// Stage completion components payload
    /// </summary>
    public class StageCompletionComponents
    {
        /// <summary>
        /// Checklist component info (selected checklist IDs and names if available)
        /// </summary>
        public List<ChecklistComponentInfo> Checklists { get; set; } = new();

        /// <summary>
        /// Checklist task completion records
        /// </summary>
        public List<ChecklistTaskCompletionInfo> TaskCompletions { get; set; } = new();

        /// <summary>
        /// Questionnaire component info (selected questionnaire IDs and names if available)
        /// </summary>
        public List<QuestionnaireComponentInfo> Questionnaires { get; set; } = new();

        /// <summary>
        /// Questionnaire answers captured at completion time
        /// </summary>
        public List<QuestionnaireAnswerInfo> QuestionnaireAnswers { get; set; } = new();

        /// <summary>
        /// Required static fields and their latest values/status
        /// </summary>
        public List<RequiredFieldInfo> RequiredFields { get; set; } = new();
    }

    /// <summary>
    /// Checklist component selection
    /// </summary>
    public class ChecklistComponentInfo
    {
        public long ChecklistId { get; set; }
        public string ChecklistName { get; set; }
        public string Description { get; set; }
        public string Team { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public bool IsTemplate { get; set; }
        public decimal CompletionRate { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public bool IsActive { get; set; }
        public List<ChecklistTaskInfo> Tasks { get; set; } = new();
    }

    /// <summary>
    /// Checklist task information
    /// </summary>
    public class ChecklistTaskInfo
    {
        public long Id { get; set; }
        public long ChecklistId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int OrderIndex { get; set; }
        public string TaskType { get; set; }
        public bool IsRequired { get; set; }
        public decimal EstimatedHours { get; set; }
        public string Priority { get; set; }
        public bool IsCompleted { get; set; }
        public string Status { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Simplified task completion snapshot
    /// </summary>
    public class ChecklistTaskCompletionInfo
    {
        public long ChecklistId { get; set; }
        public long TaskId { get; set; }
        public bool IsCompleted { get; set; }
        public string CompletionNotes { get; set; }
        public string CompletedBy { get; set; }
        public DateTimeOffset? CompletedTime { get; set; }
    }

    /// <summary>
    /// Questionnaire component selection
    /// </summary>
    public class QuestionnaireComponentInfo
    {
        public long QuestionnaireId { get; set; }
        public string QuestionnaireName { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public int Version { get; set; }
        public string Category { get; set; }
        public int TotalQuestions { get; set; }
        public int RequiredQuestions { get; set; }
        public bool AllowDraft { get; set; }
        public bool AllowMultipleSubmissions { get; set; }
        public bool IsActive { get; set; }
        public string StructureJson { get; set; }
    }

    /// <summary>
    /// Simplified questionnaire answer snapshot
    /// </summary>
    public class QuestionnaireAnswerInfo
    {
        public long AnswerId { get; set; }
        public long QuestionnaireId { get; set; }
        public long QuestionId { get; set; }
        public string QuestionText { get; set; }
        public string QuestionType { get; set; }
        public bool IsRequired { get; set; }
        public object Answer { get; set; }
        public DateTimeOffset? AnswerTime { get; set; }
        public string Status { get; set; }
    }

    /// <summary>
    /// Required field latest value/status
    /// </summary>
    public class RequiredFieldInfo
    {
        public string FieldName { get; set; }
        public string DisplayName { get; set; }
        public string FieldType { get; set; }
        public bool IsRequired { get; set; }
        public object FieldValue { get; set; }
        public string ValidationStatus { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
    }
}
