using System;
using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.Stage
{
    /// <summary>
    /// Stage output DTO
    /// </summary>
    public class StageOutputDto
    {
        /// <summary>
        /// Primary key ID (Snowflake generated)
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Tenant ID
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Workflow ID
        /// </summary>
        public long WorkflowId { get; set; }

        /// <summary>
        /// Stage name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Portal display name
        /// </summary>
        public string PortalName { get; set; }

        /// <summary>
        /// Internal name
        /// </summary>
        public string InternalName { get; set; }

        /// <summary>
        /// Stage description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Default assigned group (Role)
        /// </summary>
        public string DefaultAssignedGroup { get; set; }

        /// <summary>
        /// Default assignee
        /// </summary>
        public string DefaultAssignee { get; set; }

        /// <summary>
        /// Estimated duration in days (supports decimal)
        /// </summary>
        public decimal? EstimatedDuration { get; set; }

        /// <summary>
        /// Stage order (for sorting)
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Associated checklist ID
        /// </summary>
        public long? ChecklistId { get; set; }

        /// <summary>
        /// Associated questionnaire ID
        /// </summary>
        public long? QuestionnaireId { get; set; }

        /// <summary>
        /// Stage color
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// Stage components configuration (JSON)
        /// </summary>
        public string ComponentsJson { get; set; }

        /// <summary>
        /// Stage components configuration list
        /// Defines the components contained in the stage and their order
        /// </summary>
        public List<FlowFlex.Domain.Shared.Models.StageComponent> Components { get; set; }

        /// <summary>
        /// Whether it's active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Visible in Portal - Controls whether this stage is visible in the portal
        /// </summary>
        public bool VisibleInPortal { get; set; }

        /// <summary>
        /// Attachment Management Needed - Indicates whether file upload is required for this stage
        /// </summary>
        public bool AttachmentManagementNeeded { get; set; }

        /// <summary>
        /// Whether it's valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Create date
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// Modify date
        /// </summary>
        public DateTimeOffset ModifyDate { get; set; }

        /// <summary>
        /// Creator name
        /// </summary>
        public string CreateBy { get; set; }

        /// <summary>
        /// Modifier name
        /// </summary>
        public string ModifyBy { get; set; }

        /// <summary>
        /// Creator user ID
        /// </summary>
        public long CreateUserId { get; set; }

        /// <summary>
        /// Modifier user ID
        /// </summary>
        public long ModifyUserId { get; set; }

        /// <summary>
        /// AI Generated Summary
        /// </summary>
        public string AiSummary { get; set; }

        /// <summary>
        /// AI Summary Generation Date
        /// </summary>
        public DateTime? AiSummaryGeneratedAt { get; set; }

        /// <summary>
        /// AI Summary Confidence Score (0-1)
        /// </summary>
        public decimal? AiSummaryConfidence { get; set; }

        /// <summary>
        /// AI Model Used for Summary Generation
        /// </summary>
        public string AiSummaryModel { get; set; }

        /// <summary>
        /// AI Summary Detailed Data (JSON)
        /// </summary>
        public string AiSummaryData { get; set; }
    }
}