namespace FlowFlex.Application.Contracts.Dtos.OW.WorkflowVersion
{
    /// <summary>
    /// 工作流版本输出DTO
    /// </summary>
    public class WorkflowVersionOutputDto
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 原始工作流ID
        /// </summary>
        public long OriginalWorkflowId { get; set; }

        /// <summary>
        /// 工作流名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 工作流描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 是否默认工作流
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTimeOffset? StartDate { get; set; }

        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTimeOffset? EndDate { get; set; }

        /// <summary>
        /// 版本号
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 配置JSON
        /// </summary>
        public string ConfigJson { get; set; }

        /// <summary>
        /// 变更原因
        /// </summary>
        public string ChangeReason { get; set; }

        /// <summary>
        /// 变更类型
        /// </summary>
        public string ChangeType { get; set; }

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
