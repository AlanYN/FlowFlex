using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Onboarding Stage Progress - Progress information for each stage (simplified version)
    /// Only stores stageId and completion-related fields. Other fields are dynamically loaded from Stage entity.
    /// </summary>
    public class OnboardingStageProgress
    {
        /// <summary>
        /// Stage ID - Reference to the Stage entity
        /// </summary>
        public long StageId { get; set; }

        /// <summary>
        /// Completion Status (Pending/InProgress/Completed/Skipped/Rejected/Terminated)
        /// </summary>
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Is Completed
        /// </summary>
        public bool IsCompleted { get; set; } = false;

        /// <summary>
        /// Start Time
        /// </summary>
        public DateTimeOffset? StartTime { get; set; }

        /// <summary>
        /// Completion Time
        /// </summary>
        public DateTimeOffset? CompletionTime { get; set; }

        /// <summary>
        /// Completed By ID
        /// </summary>
        public long? CompletedById { get; set; }

        /// <summary>
        /// Completed By Name
        /// </summary>
        [StringLength(100)]
        public string CompletedBy { get; set; }

        /// <summary>
        /// Notes (completion notes, feedback, etc.)
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Is Current Stage
        /// </summary>
        public bool IsCurrent { get; set; } = false;

        // === Below fields are dynamically populated from Stage entity and not stored in JSON ===
        
        /// <summary>
        /// Stage Name (from Stage entity) - Not stored in JSON
        /// </summary>
        [JsonIgnore]
        public string StageName { get; set; }

        /// <summary>
        /// Stage Description (from Stage entity) - Not stored in JSON
        /// </summary>
        [JsonIgnore]
        public string StageDescription { get; set; }

        /// <summary>
        /// Stage Order (from Stage entity) - Not stored in JSON
        /// </summary>
        [JsonIgnore]
        public int StageOrder { get; set; }

        /// <summary>
        /// Estimated Days (from Stage entity) - Not stored in JSON
        /// </summary>
        [JsonIgnore]
        public decimal? EstimatedDays { get; set; }

        /// <summary>
        /// Actual Days (calculated) - Not stored in JSON
        /// </summary>
        [JsonIgnore]
        public int? ActualDays 
        { 
            get 
            {
                if (StartTime.HasValue && CompletionTime.HasValue)
                {
                    return (int)(CompletionTime.Value - StartTime.Value).TotalDays;
                }
                return null;
            }
        }

        /// <summary>
        /// Visible in Portal (from Stage entity) - Not stored in JSON
        /// </summary>
        [JsonIgnore]
        public bool VisibleInPortal { get; set; } = true;

        /// <summary>
        /// Attachment Management Needed (from Stage entity) - Not stored in JSON
        /// </summary>
        [JsonIgnore]
        public bool AttachmentManagementNeeded { get; set; } = false;

        /// <summary>
        /// Stage Components Configuration JSON (from Stage entity) - Not stored in JSON
        /// </summary>
        [JsonIgnore]
        public string ComponentsJson { get; set; }

        /// <summary>
        /// Stage Components List (from Stage entity) - Not stored in JSON
        /// </summary>
        [JsonIgnore]
        public List<FlowFlex.Domain.Shared.Models.StageComponent> Components { get; set; } = new List<FlowFlex.Domain.Shared.Models.StageComponent>();

        // === Legacy fields for backward compatibility - will be removed in future versions ===
        
        /// <summary>
        /// Last Updated Time
        /// </summary>
        public DateTimeOffset? LastUpdatedTime { get; set; }

        /// <summary>
        /// Last Updated By
        /// </summary>
        [StringLength(100)]
        public string LastUpdatedBy { get; set; }

        /// <summary>
        /// Rejection Reason
        /// </summary>
        [StringLength(500)]
        public string RejectionReason { get; set; }

        /// <summary>
        /// Rejected By ID
        /// </summary>
        public long? RejectedById { get; set; }

        /// <summary>
        /// Rejected By Name
        /// </summary>
        [StringLength(100)]
        public string RejectedBy { get; set; }

        /// <summary>
        /// Rejection Time
        /// </summary>
        public DateTimeOffset? RejectionTime { get; set; }

        /// <summary>
        /// Termination Reason
        /// </summary>
        [StringLength(500)]
        public string TerminationReason { get; set; }

        /// <summary>
        /// Terminated By ID
        /// </summary>
        public long? TerminatedById { get; set; }

        /// <summary>
        /// Termination Time
        /// </summary>
        public DateTimeOffset? TerminationTime { get; set; }

        /// <summary>
        /// Terminated By
        /// </summary>
        [StringLength(100)]
        public string TerminatedBy { get; set; }
    }
}
