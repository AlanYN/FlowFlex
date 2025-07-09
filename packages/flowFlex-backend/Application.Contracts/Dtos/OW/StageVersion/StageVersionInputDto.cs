using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.StageVersion
{
    /// <summary>
    /// 阶段版本输入DTO
    /// </summary>
    public class StageVersionInputDto
    {
        /// <summary>
        /// 工作流版本ID
        /// </summary>

        public long WorkflowVersionId { get; set; }

        /// <summary>
        /// 原始阶段ID
        /// </summary>

        public long OriginalStageId { get; set; }

        /// <summary>
        /// 阶段名称
        /// </summary>

        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// 阶段描述
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// 默认分配组
        /// </summary>
        [StringLength(100)]
        public string DefaultAssignedGroup { get; set; }

        /// <summary>
        /// 默认分配人
        /// </summary>
        [StringLength(100)]
        public string DefaultAssignee { get; set; }

        /// <summary>
        /// 预计持续时间（天，支持小数）
        /// </summary>
        public decimal? EstimatedDuration { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        public int OrderIndex { get; set; }

        /// <summary>
        /// 清单ID
        /// </summary>
        public long? ChecklistId { get; set; }

        /// <summary>
        /// 问卷ID
        /// </summary>
        public long? QuestionnaireId { get; set; }

        /// <summary>
        /// 阶段颜色
        /// </summary>
        [StringLength(20)]
        public string Color { get; set; }

        /// <summary>
        /// 必填字段JSON
        /// </summary>
        [StringLength(1000)]
        public string RequiredFieldsJson { get; set; }

        /// <summary>
        /// 工作流版本
        /// </summary>
        [StringLength(20)]
        public string WorkflowVersion { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
