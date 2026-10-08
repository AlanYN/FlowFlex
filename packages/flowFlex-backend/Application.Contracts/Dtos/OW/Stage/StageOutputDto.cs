using System;
using System.Collections.Generic;
using FlowFlex.Application.Contracts.Dtos.OW.Permission;
using FlowFlex.Domain.Shared.Enums;
using FlowFlex.Domain.Shared.Enums.OW;

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
        /// Default assignee（支持多个分配人）
        /// </summary>
        public List<string> DefaultAssignee { get; set; }

        /// <summary>
        /// Co-assignees (additional assignees for the stage)
        /// </summary>
        public List<string> CoAssignees { get; set; }

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
        /// Portal Permission - Defines the level of access in the customer portal (Viewable or Completable)
        /// Only applies when VisibleInPortal is true
        /// </summary>
        public PortalPermissionEnum? PortalPermission { get; set; }

        /// <summary>
        /// View Permission Mode - Public/VisibleToTeams/InvisibleToTeams/Private
        /// </summary>
        public ViewPermissionModeEnum ViewPermissionMode { get; set; }

        /// <summary>
        /// View Teams - List of team names for view permission control
        /// </summary>
        public List<string> ViewTeams { get; set; }

        /// <summary>
        /// Operate Teams - List of team names that can perform operations
        /// </summary>
        public List<string> OperateTeams { get; set; }

        /// <summary>
        /// Use Same Team For Operate - Indicates whether operate teams should use the same teams as view permission
        /// When true, OperateTeams will be automatically synchronized with ViewTeams
        /// </summary>
        public bool UseSameTeamForOperate { get; set; }

        // --- Template/Runtime Configuration (persisted, echoed back) ---

        /// <summary>
        /// Template Use Same As Workflow - When true, Stage Template Permission inherits from Workflow Template.
        /// </summary>
        public bool TemplateUseSameAsWorkflow { get; set; } = true;

        /// <summary>
        /// Runtime Use Same As Workflow - When true, Stage Runtime Permission inherits from Workflow Runtime.
        /// </summary>
        public bool RuntimeUseSameAsWorkflow { get; set; } = true;

        /// <summary>
        /// Stage Runtime View Permission Mode (only used when RuntimeUseSameAsWorkflow = false).
        /// </summary>
        public ViewPermissionModeEnum RuntimeViewPermissionMode { get; set; }

        /// <summary>
        /// Stage Runtime View Teams (only used when RuntimeUseSameAsWorkflow = false).
        /// </summary>
        public List<string> RuntimeViewTeams { get; set; }

        /// <summary>
        /// Stage Runtime Operate Teams (only used when RuntimeUseSameAsWorkflow = false AND RuntimeUseSameTeamForOperate = false).
        /// </summary>
        public List<string> RuntimeOperateTeams { get; set; }

        /// <summary>
        /// Runtime Use Same Team For Operate (only relevant when RuntimeUseSameAsWorkflow = false).
        /// </summary>
        public bool RuntimeUseSameTeamForOperate { get; set; } = true;

        // --- Effective Runtime (computed by Service layer, NOT by AutoMapper) ---

        /// <summary>
        /// Effective Runtime View Permission Mode - computed by PermissionCalculator in Service layer.
        /// </summary>
        public ViewPermissionModeEnum EffectiveRuntimeViewPermissionMode { get; set; }

        /// <summary>
        /// Effective Runtime View Teams - computed by PermissionCalculator in Service layer.
        /// </summary>
        public List<string> EffectiveRuntimeViewTeams { get; set; }

        /// <summary>
        /// Effective Runtime Operate Teams - computed by PermissionCalculator in Service layer.
        /// </summary>
        public List<string> EffectiveRuntimeOperateTeams { get; set; }

        /// <summary>
        /// Effective Roll Back Teams - computed by PermissionCalculator in Service layer.
        /// Represents the resolved set of teams allowed to roll back this stage.
        /// </summary>
        public List<string> EffectiveRollBackTeams { get; set; }

        /// <summary>
        /// Roll Back Teams - List of team IDs allowed to roll back completed stages.
        /// NULL or empty means no one can roll back (security default).
        /// </summary>
        public List<string> RollBackTeams { get; set; }

        /// <summary>
        /// Attachment Management Needed - Indicates whether file upload is required for this stage
        /// </summary>
        public bool AttachmentManagementNeeded { get; set; }

        /// <summary>
        /// Adobe Sign Enabled - Enables legally binding signatures via Adobe Sign for PDF files in this stage
        /// </summary>
        public bool AdobeSignEnabled { get; set; }

        /// <summary>
        /// Required - Indicates whether this stage is required to complete the workflow
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// Condition Fallback Stage ID - The stage to jump to when all conditions are not met.
        /// NULL means "Continue to next stage" (default behavior).
        /// </summary>
        public long? ConditionFallbackStageId { get; set; }

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
        /// User's permission for this stage
        /// Includes view and operate permissions
        /// </summary>
        public PermissionInfoDto Permission { get; set; }

        /// <summary>
        /// Component weight configuration deserialized from ff_stage.component_weights.
        /// null = no weights have been configured (equal distribution is used for completion calculation).
        /// </summary>
        public List<ComponentWeightItem>? ComponentWeights { get; set; }
    }
}