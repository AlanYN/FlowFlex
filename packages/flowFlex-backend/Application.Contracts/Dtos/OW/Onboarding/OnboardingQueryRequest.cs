using System;
using System.Collections.Generic;

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
        /// Filter by lead ID
        /// </summary>
        public string? LeadId { get; set; }

        /// <summary>
        /// Filter by multiple lead IDs (batch query)
        /// </summary>
        public List<string>? LeadIds { get; set; }

        /// <summary>
        /// Filter by lead name
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
        /// Filter by stage updated by (stage_updated_by)
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
    }
}