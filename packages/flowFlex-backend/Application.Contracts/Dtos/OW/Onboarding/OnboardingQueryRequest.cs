using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// Onboarding query request DTO
    /// </summary>
    public class OnboardingQueryRequest
    {
        /// <summary>
        /// Page number
        /// </summary>
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Sort field
        /// </summary>
        public string? SortField { get; set; } = "CreateDate";

        /// <summary>
        /// Sort direction (asc/desc)
        /// </summary>
        public string? SortDirection { get; set; } = "desc";

        /// <summary>
        /// Filter by workflow ID
        /// </summary>
        public long? WorkflowId { get; set; }

        /// <summary>
        /// Filter by current stage ID
        /// </summary>
        public long? CurrentStageId { get; set; }

        /// <summary>
        /// Filter by lead ID (supports comma-separated values and fuzzy matching)
        /// </summary>
        public string? LeadId { get; set; }

        /// <summary>
        /// Filter by multiple lead IDs (batch query)
        /// </summary>
        public List<string>? LeadIds { get; set; }

        /// <summary>
        /// Filter by multiple onboarding IDs (for export selected records)
        /// </summary>
        public List<long>? OnboardingIds { get; set; }

        /// <summary>
        /// Filter by lead name (supports comma-separated values)
        /// </summary>
        public string? LeadName { get; set; }

        /// <summary>
        /// Filter by lead email
        /// </summary>
        public string? LeadEmail { get; set; }

        /// <summary>
        /// Filter by status
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Filter by priority
        /// </summary>
        public string? Priority { get; set; }

        /// <summary>
        /// Filter by assignee ID
        /// </summary>
        public long? CurrentAssigneeId { get; set; }

        /// <summary>
        /// Filter by team
        /// </summary>
        public string? CurrentTeam { get; set; }

        /// <summary>
        /// Filter by start date range - from
        /// </summary>
        public DateTimeOffset? StartDateFrom { get; set; }

        /// <summary>
        /// Filter by start date range - to
        /// </summary>
        public DateTimeOffset? StartDateTo { get; set; }

        /// <summary>
        /// Filter by estimated completion date range - from
        /// </summary>
        public DateTimeOffset? EstimatedCompletionDateFrom { get; set; }

        /// <summary>
        /// Filter by estimated completion date range - to
        /// </summary>
        public DateTimeOffset? EstimatedCompletionDateTo { get; set; }

        /// <summary>
        /// Filter by completion rate range - from
        /// </summary>
        public decimal? CompletionRateFrom { get; set; }

        /// <summary>
        /// Filter by completion rate range - to
        /// </summary>
        public decimal? CompletionRateTo { get; set; }

        /// <summary>
        /// Filter by active status
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Filter by life cycle stage ID
        /// </summary>
        public long? LifeCycleStageId { get; set; }

        /// <summary>
        /// Filter by life cycle stage name
        /// </summary>
        public string? LifeCycleStageName { get; set; }

        /// <summary>
        /// Filter by stage updated by (stage_updated_by, supports comma-separated values)
        /// </summary>
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// Filter by updated by user ID
        /// </summary>
        public long? UpdatedByUserId { get; set; }

        /// <summary>
        /// Filter by created by (create by)
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Filter by created by user ID
        /// </summary>
        public long? CreatedByUserId { get; set; }

        /// <summary>
        /// Filter by ownership (user ID who owns the onboarding)
        /// </summary>
        public long? Ownership { get; set; }

        /// <summary>
        /// Filter by ownership name
        /// </summary>
        public string? OwnershipName { get; set; }

        /// <summary>
        /// Return all data without pagination when set to true
        /// </summary>
        public bool AllData { get; set; } = false;

        /// <summary>
        /// Get Lead IDs as list (splits comma-separated values)
        /// </summary>
        public List<string> GetLeadIdsList()
        {
            if (string.IsNullOrEmpty(LeadId))
                return new List<string>();

            return LeadId.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => id.Trim())
                        .Where(id => !string.IsNullOrEmpty(id))
                        .ToList();
        }

        /// <summary>
        /// Get Lead Names as list (splits comma-separated values)
        /// </summary>
        public List<string> GetLeadNamesList()
        {
            if (string.IsNullOrEmpty(LeadName))
                return new List<string>();

            return LeadName.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(name => name.Trim())
                          .Where(name => !string.IsNullOrEmpty(name))
                          .ToList();
        }

        /// <summary>
        /// Get Updated By users as list (splits comma-separated values)
        /// </summary>
        public List<string> GetUpdatedByList()
        {
            if (string.IsNullOrEmpty(UpdatedBy))
                return new List<string>();

            return UpdatedBy.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(user => user.Trim())
                           .Where(user => !string.IsNullOrEmpty(user))
                           .ToList();
        }
    }
}