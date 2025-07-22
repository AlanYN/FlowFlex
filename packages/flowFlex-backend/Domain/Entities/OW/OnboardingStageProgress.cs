using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Onboarding Stage Progress - Progress information for each stage
    /// </summary>
    public class OnboardingStageProgress
    {
        /// <summary>
        /// Stage ID
        /// </summary>

        public long StageId { get; set; }

        /// <summary>
        /// Stage Name
        /// </summary>
        [StringLength(200)]
        public string StageName { get; set; }

        /// <summary>
        /// Stage Order
        /// </summary>
        public int StageOrder { get; set; }

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
        /// Estimated Days (supports decimal)
        /// </summary>
        public decimal? EstimatedDays { get; set; }

        /// <summary>
        /// Actual Days Used
        /// </summary>
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
        /// Notes
        /// </summary>
        [StringLength(500)]
        public string Notes { get; set; }

        /// <summary>
        /// Is Current Stage
        /// </summary>
        public bool IsCurrent { get; set; } = false;

        /// <summary>
        /// Completion Method (Manual/Auto/System)
        /// </summary>
        [StringLength(20)]
        public string CompletionMethod { get; set; } = "Manual";

        /// <summary>
        /// Can Re-Complete
        /// </summary>
        public bool CanReComplete { get; set; } = true;

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
        /// Rejection Reason (when Status is Rejected)
        /// </summary>
        [StringLength(1000)]
        public string RejectionReason { get; set; }

        /// <summary>
        /// Rejection Time
        /// </summary>
        public DateTimeOffset? RejectionTime { get; set; }

        /// <summary>
        /// Rejected By
        /// </summary>
        [StringLength(100)]
        public string RejectedBy { get; set; }

        /// <summary>
        /// Is Terminated
        /// </summary>
        public bool IsTerminated { get; set; } = false;

        /// <summary>
        /// Termination Time
        /// </summary>
        public DateTimeOffset? TerminationTime { get; set; }

        /// <summary>
        /// Terminated By
        /// </summary>
        [StringLength(100)]
        public string TerminatedBy { get; set; }

        /// <summary>
        /// Visible in Portal - Controls whether this stage is visible in the portal
        /// </summary>
        public bool VisibleInPortal { get; set; } = true;

        /// <summary>
        /// Attachment Management Needed - Indicates whether file upload is required for this stage
        /// </summary>
        public bool AttachmentManagementNeeded { get; set; } = false;

        /// <summary>
        /// Stage Components Configuration (JSON)
        /// </summary>
        public string ComponentsJson { get; set; }

        /// <summary>
        /// Stage Components List (not mapped to database)
        /// </summary>
        public List<FlowFlex.Domain.Shared.Models.StageComponent> Components { get; set; } = new List<FlowFlex.Domain.Shared.Models.StageComponent>();
    }
}
