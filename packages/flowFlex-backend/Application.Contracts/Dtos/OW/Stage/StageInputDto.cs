using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using FlowFlex.Domain.Shared.Enums;
using FlowFlex.Domain.Shared.Enums.OW;

namespace FlowFlex.Application.Contracts.Dtos.OW.Stage
{
    /// <summary>
    /// Stage create/update input DTO
    /// </summary>
    public class StageInputDto
    {
        /// <summary>
        /// Workflow Id
        /// </summary>

        public long WorkflowId { get; set; }

        /// <summary>
        /// Stage name
        /// </summary>

        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Portal显示名称
        /// </summary>
        [StringLength(100)]
        public string PortalName { get; set; }

        /// <summary>
        /// 内部名称
        /// </summary>
        [StringLength(100)]
        public string InternalName { get; set; }

        /// <summary>
        /// Stage description
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// 默认分配用户组（Role）
        /// </summary>
        [StringLength(100)]
        public string DefaultAssignedGroup { get; set; }

        /// <summary>
        /// 默认分配人（支持多个分配人）
        /// </summary>
        public List<string> DefaultAssignee { get; set; }

        /// <summary>
        /// Co-assignees (additional assignees for the stage)
        /// </summary>
        public List<string> CoAssignees { get; set; }

        /// <summary>
        /// 预计持续天数（支持小数）
        /// </summary>
        public decimal? EstimatedDuration { get; set; }

        /// <summary>
        /// Stage order (for sorting)
        /// </summary>
        public int Order { get; set; } = 1;

        /// <summary>
        /// 关联Checklist主键ID
        /// </summary>
        public long? ChecklistId { get; set; }

        /// <summary>
        /// 关联问卷主键ID
        /// </summary>
        public long? QuestionnaireId { get; set; }

        /// <summary>
        /// 阶段颜色
        /// </summary>
        [StringLength(20)]
        public string Color { get; set; }

        /// <summary>
        /// Visible in Portal - Controls whether this stage is visible in the portal
        /// </summary>
        public bool VisibleInPortal { get; set; } = true;

        /// <summary>
        /// Portal Permission - Defines the level of access in the customer portal (Viewable or Completable)
        /// Only applies when VisibleInPortal is true
        /// </summary>
        public PortalPermissionEnum? PortalPermission { get; set; } = PortalPermissionEnum.Viewable;

        /// <summary>
        /// View Permission Mode - Public/VisibleToTeams/InvisibleToTeams/Private
        /// </summary>
        public ViewPermissionModeEnum ViewPermissionMode { get; set; } = ViewPermissionModeEnum.Public;

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
        public bool UseSameTeamForOperate { get; set; } = false;

        /// <summary>
        /// Template Use Same As Workflow - When true, Stage Template Permission inherits from
        /// parent Workflow Template Permission (dynamically, not a snapshot).
        /// </summary>
        public bool TemplateUseSameAsWorkflow { get; set; } = true;

        /// <summary>
        /// Runtime Use Same As Workflow - When true, Stage Runtime Permission inherits from
        /// parent Workflow's Effective Runtime Permission.
        /// </summary>
        public bool RuntimeUseSameAsWorkflow { get; set; } = true;

        /// <summary>
        /// Stage Runtime View Permission Mode.
        /// Only effective when RuntimeUseSameAsWorkflow = false.
        /// </summary>
        public ViewPermissionModeEnum RuntimeViewPermissionMode { get; set; } = ViewPermissionModeEnum.Public;

        /// <summary>
        /// Stage Runtime View Teams.
        /// Only effective when RuntimeUseSameAsWorkflow = false.
        /// </summary>
        public List<string> RuntimeViewTeams { get; set; }

        /// <summary>
        /// Stage Runtime Operate Teams.
        /// Only effective when RuntimeUseSameAsWorkflow = false AND RuntimeUseSameTeamForOperate = false.
        /// </summary>
        public List<string> RuntimeOperateTeams { get; set; }

        /// <summary>
        /// Runtime Use Same Team For Operate - When true AND RuntimeUseSameAsWorkflow = false,
        /// Stage Runtime Operate Teams = Stage Runtime View Teams.
        /// </summary>
        public bool RuntimeUseSameTeamForOperate { get; set; } = true;

        /// <summary>
        /// Attachment Management Needed - Indicates whether file upload is required for this stage
        /// </summary>
        public bool AttachmentManagementNeeded { get; set; } = false;

        /// <summary>
        /// Adobe Sign Enabled - Enables legally binding signatures via Adobe Sign for PDF files in this stage
        /// </summary>
        public bool AdobeSignEnabled { get; set; } = false;

        /// <summary>
        /// Required - Indicates whether this stage is required to complete the workflow
        /// </summary>
        public bool Required { get; set; } = false;

        /// <summary>
        /// Stage组件配置列表
        /// 定义Stage包含的组件及其顺序
        /// </summary>
        public List<FlowFlex.Domain.Shared.Models.StageComponent> Components { get; set; }

        /// <summary>
        /// Roll Back Teams - JSONB array of team IDs allowed to roll back completed stages.
        /// NULL or empty means no one can roll back (security default).
        /// </summary>
        public List<string> RollBackTeams { get; set; }

        /// <summary>
        /// Component weight configuration for CompletionPercentage calculation.
        /// null = do not update existing weights (preserve current value).
        /// Non-null and non-empty: all weights must sum to 100.
        /// Empty list = clear all weight configuration (revert to equal distribution).
        /// </summary>
        public List<ComponentWeightItem>? ComponentWeights { get; set; }
    }
}