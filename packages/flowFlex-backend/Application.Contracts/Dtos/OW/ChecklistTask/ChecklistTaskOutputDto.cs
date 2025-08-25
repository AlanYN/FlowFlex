namespace FlowFlex.Application.Contracts.Dtos.OW.ChecklistTask
{
    /// <summary>
    /// 检查清单任务输出DTO
    /// </summary>
    public class ChecklistTaskOutputDto
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 检查清单ID
        /// </summary>
        public long ChecklistId { get; set; }

        /// <summary>
        /// 任务名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 任务描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 排序索引
        /// </summary>
        public int OrderIndex { get; set; }

        /// <summary>
        /// 任务类型
        /// </summary>
        public string TaskType { get; set; }

        /// <summary>
        /// 是否必需
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 分配人员ID
        /// </summary>
        public long? AssigneeId { get; set; }

        /// <summary>
        /// 分配人员名称
        /// </summary>
        public string AssigneeName { get; set; }

        /// <summary>
        /// 分配团队
        /// </summary>
        public string AssignedTeam { get; set; }

        /// <summary>
        /// 预计完成时间（小时）
        /// </summary>
        public int EstimatedHours { get; set; }

        /// <summary>
        /// 优先级
        /// </summary>
        public string Priority { get; set; }

        /// <summary>
        /// 截止日期
        /// </summary>
        public DateTimeOffset? DueDate { get; set; }

        /// <summary>
        /// 是否完成
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTimeOffset? CompletedAt { get; set; }

        /// <summary>
        /// 完成人员ID
        /// </summary>
        public long? CompletedById { get; set; }

        /// <summary>
        /// 完成人员名称
        /// </summary>
        public string CompletedByName { get; set; }

        /// <summary>
        /// 完成备注
        /// </summary>
        public string CompletionNotes { get; set; }

        /// <summary>
        /// 附件JSON
        /// </summary>
        public string AttachmentsJson { get; set; }

        /// <summary>
        /// 结构化负责人信息
        /// </summary>
        public AssigneeDto Assignee { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        public string CreateBy { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTimeOffset ModifyDate { get; set; }

        /// <summary>
        /// 修改人
        /// </summary>
        public string ModifyBy { get; set; }

        /// <summary>
        /// 关联动作ID
        /// </summary>
        public long? ActionId { get; set; }

        /// <summary>
        /// 动作名称
        /// </summary>
        public string ActionName { get; set; }

        /// <summary>
        /// 动作触发映射ID
        /// </summary>
        public long? ActionMappingId { get; set; }
    }
}