namespace FlowFlex.Application.Contracts.Dtos.OW.StageVersion
{
    /// <summary>
    /// 阶段版本输出DTO
    /// </summary>
    public class StageVersionOutputDto
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        public long Id { get; set; }

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
        public string Name { get; set; }

        /// <summary>
        /// 阶段描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 默认分配组
        /// </summary>
        public string DefaultAssignedGroup { get; set; }

        /// <summary>
        /// 默认分配人
        /// </summary>
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
        public string Color { get; set; }

        /// <summary>
        /// 必填字段JSON
        /// </summary>
        public string RequiredFieldsJson { get; set; }

        /// <summary>
        /// 工作流版本
        /// </summary>
        public string WorkflowVersion { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTimeOffset ModifyDate { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        public string CreateBy { get; set; }

        /// <summary>
        /// 修改人
        /// </summary>
        public string ModifyBy { get; set; }
    }
} 
 