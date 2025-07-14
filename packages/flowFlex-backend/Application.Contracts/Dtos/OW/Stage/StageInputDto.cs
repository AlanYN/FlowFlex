using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

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
        /// 默认分配人
        /// </summary>
        [StringLength(100)]
        public string DefaultAssignee { get; set; }

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
        /// Stage组件配置列表
        /// 定义Stage包含的组件及其顺序
        /// </summary>
        public List<FlowFlex.Domain.Shared.Models.StageComponent> Components { get; set; }
    }
}