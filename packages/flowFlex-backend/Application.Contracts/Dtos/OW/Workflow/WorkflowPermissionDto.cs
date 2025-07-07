using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.Workflow
{
    /// <summary>
    /// 工作流权限DTO - 控制Active状态下的操作权限
    /// </summary>
    public class WorkflowPermissionDto
    {
        /// <summary>
        /// 工作流ID
        /// </summary>
        public long WorkflowId { get; set; }

        /// <summary>
        /// 工作流是否为Active状态
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 是否允许修改基本信息（名称、描述等）
        /// </summary>
        public bool CanModifyBasicInfo { get; set; }

        /// <summary>
        /// 是否允许新增Stage
        /// </summary>
        public bool CanAddStage { get; set; }

        /// <summary>
        /// 是否允许删除Stage
        /// </summary>
        public bool CanDeleteStage { get; set; }

        /// <summary>
        /// 是否允许修改Stage排序
        /// </summary>
        public bool CanModifyStageOrder { get; set; }

        /// <summary>
        /// 是否允许修改Question
        /// </summary>
        public bool CanModifyQuestion { get; set; }

        /// <summary>
        /// 是否允许新增Question
        /// </summary>
        public bool CanAddQuestion { get; set; }

        /// <summary>
        /// 是否允许禁用相关功能
        /// </summary>
        public bool CanDisableFeatures { get; set; }

        /// <summary>
        /// 受限操作列表
        /// </summary>
        public List<string> RestrictedOperations { get; set; } = new List<string>();

        /// <summary>
        /// 权限检查说明
        /// </summary>
        public string PermissionNote { get; set; }
    }
}