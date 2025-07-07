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
}
