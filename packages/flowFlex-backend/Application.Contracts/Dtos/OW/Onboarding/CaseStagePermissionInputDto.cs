using System.Collections.Generic;
using FlowFlex.Domain.Shared.Enums.OW;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// Input DTO for configuring an individual Case Stage's permission.
    /// Used by PUT /ow/onboardings/v1/{id}/stage-permissions/{stageId}.
    /// </summary>
    public class CaseStagePermissionInputDto
    {
        /// <summary>
        /// Whether this Case Stage should inherit permission from the Workflow Stage Runtime.
        /// true (default) = inherit from MaxStage* snapshot; all other fields are ignored.
        /// false = use the independent configuration fields below.
        /// </summary>
        public bool InheritFromWorkflowStage { get; set; } = true;

        // --- View Permission (used when InheritFromWorkflowStage = false) ---

        /// <summary>
        /// View Permission Mode. Does NOT support Private at Case Stage level.
        /// Nullable: null is treated as Public (inherit) when InheritFromWorkflowStage = false.
        /// </summary>
        public ViewPermissionModeEnum? ViewPermissionMode { get; set; }

        /// <summary>
        /// View Permission Subject Type - Team or User.
        /// </summary>
        public PermissionSubjectTypeEnum ViewPermissionSubjectType { get; set; } = PermissionSubjectTypeEnum.Team;

        /// <summary>
        /// View Teams - used when ViewPermissionSubjectType = Team and ViewPermissionMode = VisibleTo/InvisibleTo.
        /// Must be a subset of MaxStageViewTeams (snapshot boundary).
        /// </summary>
        public List<string> ViewTeams { get; set; }

        /// <summary>
        /// View Users - used when ViewPermissionSubjectType = User.
        /// </summary>
        public List<string> ViewUsers { get; set; }

        // --- Operate Permission ---

        /// <summary>
        /// Use Same Team For Operate - When true, Operate permission mirrors View permission.
        /// </summary>
        public bool UseSameTeamForOperate { get; set; } = true;

        /// <summary>
        /// Operate Permission Subject Type - Team or User.
        /// </summary>
        public PermissionSubjectTypeEnum OperatePermissionSubjectType { get; set; } = PermissionSubjectTypeEnum.Team;

        /// <summary>
        /// Operate Teams - used when UseSameTeamForOperate = false and OperatePermissionSubjectType = Team.
        /// Must be a subset of MaxStageOperateTeams (snapshot boundary).
        /// </summary>
        public List<string> OperateTeams { get; set; }

        /// <summary>
        /// Operate Users - used when UseSameTeamForOperate = false and OperatePermissionSubjectType = User.
        /// </summary>
        public List<string> OperateUsers { get; set; }

        // --- Roll Back Permission ---

        /// <summary>
        /// Roll Back Inherit - When true, Roll Back permission uses MaxStageRollBackTeams snapshot.
        /// </summary>
        public bool RollBackInherit { get; set; } = true;

        /// <summary>
        /// Roll Back Use Same As Operate - When RollBackInherit = false and this is true,
        /// Roll Back permission uses the effective Case Stage Operate Teams.
        /// </summary>
        public bool RollBackUseSameAsOperate { get; set; } = true;

        /// <summary>
        /// Roll Back Permission Subject Type - Team or User.
        /// </summary>
        public PermissionSubjectTypeEnum RollBackPermissionSubjectType { get; set; } = PermissionSubjectTypeEnum.Team;

        /// <summary>
        /// Roll Back Teams - used when RollBackInherit = false and RollBackUseSameAsOperate = false
        /// and RollBackPermissionSubjectType = Team.
        /// Must be a subset of effective Case Stage Operate Teams.
        /// </summary>
        public List<string> RollBackTeams { get; set; }

        /// <summary>
        /// Roll Back Users - used when RollBackInherit = false and RollBackUseSameAsOperate = false
        /// and RollBackPermissionSubjectType = User.
        /// </summary>
        public List<string> RollBackUsers { get; set; }
    }
}
