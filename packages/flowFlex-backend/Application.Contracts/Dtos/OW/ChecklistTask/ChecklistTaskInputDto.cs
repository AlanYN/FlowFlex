using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.ChecklistTask
{
    /// <summary>
    /// 检查清单任务输入DTO
    /// </summary>
    public class ChecklistTaskInputDto
    {
        /// <summary>
        /// 检查清单ID
        /// </summary>

        public long ChecklistId { get; set; }

        /// <summary>
        /// 任务名称
        /// </summary>

        [StringLength(200)]
        public string Name { get; set; }

        /// <summary>
        /// 任务描述
        /// </summary>
        [StringLength(1000)]
        public string Description { get; set; }

        /// <summary>
        /// 排序索引
        /// </summary>
        public int OrderIndex { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 依赖任务ID
        /// </summary>
        public long? DependsOnTaskId { get; set; }

        /// <summary>
        /// 任务类型
        /// </summary>

        [StringLength(50)]
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
        [StringLength(100)]
        public string AssigneeName { get; set; }

        /// <summary>
        /// 分配团队
        /// </summary>
        [StringLength(100)]
        public string AssignedTeam { get; set; }

        /// <summary>
        /// 预计完成时间（小时）
        /// </summary>
        public int EstimatedHours { get; set; }

        /// <summary>
        /// 优先级
        /// </summary>
        [StringLength(20)]
        public string Priority { get; set; }

        /// <summary>
        /// 截止日期
        /// </summary>
        public DateTimeOffset? DueDate { get; set; }

        /// <summary>
        /// 附件JSON
        /// </summary>
        public string AttachmentsJson { get; set; }

        /// <summary>
        /// 结构化负责人信息
        /// </summary>
        public AssigneeDto Assignee { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; set; }
    }
}