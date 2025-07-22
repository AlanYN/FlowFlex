using System.ComponentModel.DataAnnotations;
using SqlSugar;
using System.Collections.Generic;
using FlowFlex.Domain.Entities.Base;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Workflow Stage Entity
    /// </summary>
    [SugarTable("ff_stage")]
    public class Stage : EntityBaseCreateInfo
    {
        /// <summary>
        /// Associated Workflow Primary Key ID
        /// </summary>
        [SugarColumn(ColumnName = "workflow_id")]
        public long WorkflowId { get; set; }

        /// <summary>
        /// Stage Name
        /// </summary>

        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Portal Display Name
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "portal_name")]
        public string PortalName { get; set; }

        /// <summary>
        /// Internal Name
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "internal_name")]
        public string InternalName { get; set; }

        /// <summary>
        /// Stage Description
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// Default Assigned User Group (Role)
        /// </summary>
        [StringLength(100)]
        public string DefaultAssignedGroup { get; set; }

        /// <summary>
        /// Default Assignee
        /// </summary>
        [StringLength(100)]
        public string DefaultAssignee { get; set; }

        /// <summary>
        /// Estimated Duration in Days (supports decimal)
        /// </summary>
        public decimal? EstimatedDuration { get; set; }

        /// <summary>
        /// Stage Order
        /// </summary>
        [SugarColumn(ColumnName = "order_index")]
        public int Order { get; set; }

        /// <summary>
        /// Associated Checklist Primary Key ID
        /// </summary>
        [SugarColumn(ColumnName = "checklist_id")]
        public long? ChecklistId { get; set; }

        /// <summary>
        /// Associated Questionnaire Primary Key ID
        /// </summary>
        [SugarColumn(ColumnName = "questionnaire_id")]
        public long? QuestionnaireId { get; set; }

        /// <summary>
        /// Stage Color
        /// </summary>
        [StringLength(20)]
        public string Color { get; set; }





        /// <summary>
        /// Workflow Version
        /// </summary>
        [StringLength(32)]
        [SugarColumn(ColumnName = "workflow_version")]
        public string WorkflowVersion { get; set; }

        /// <summary>
        /// Is Active
        /// </summary>
        [SugarColumn(ColumnName = "is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Visible in Portal - Controls whether this stage is visible in the portal
        /// </summary>
        [SugarColumn(ColumnName = "visible_in_portal")]
        public bool VisibleInPortal { get; set; } = true;

        /// <summary>
        /// Attachment Management Needed - Indicates whether file upload is required for this stage
        /// </summary>
        [SugarColumn(ColumnName = "attachment_management_needed")]
        public bool AttachmentManagementNeeded { get; set; } = false;

        /// <summary>
        /// Stage Components Configuration (JSON)
        /// </summary>
        [SugarColumn(ColumnName = "components_json")]
        public string ComponentsJson { get; set; }



        /// <summary>
        /// Stage Components List (not mapped to database)
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public List<FlowFlex.Domain.Shared.Models.StageComponent> Components { get; set; } = new List<FlowFlex.Domain.Shared.Models.StageComponent>();
    }
}
