using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Stage
{
    /// <summary>
    /// Duplicate stage input DTO
    /// </summary>
    public class DuplicateStageInputDto
    {
        /// <summary>
        /// 新阶段名称
        /// </summary>

        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// 新阶段描述
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// 目标工作流ID（如果为空则复制到同一工作流）
        /// </summary>
        public long? TargetWorkflowId { get; set; }
    }
}