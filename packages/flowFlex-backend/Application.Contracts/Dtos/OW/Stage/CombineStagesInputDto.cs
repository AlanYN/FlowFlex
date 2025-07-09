using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Stage
{
    /// <summary>
    /// Combine stages input DTO
    /// </summary>
    public class CombineStagesInputDto
    {
        /// <summary>
        /// 要合并的阶段ID列表
        /// </summary>

        public List<long> StageIds { get; set; }

        /// <summary>
        /// 新阶段名称
        /// </summary>

        [StringLength(100)]
        public string NewStageName { get; set; }

        /// <summary>
        /// 新阶段描述
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// 默认分配用户组
        /// </summary>
        [StringLength(100)]
        public string DefaultAssignedGroup { get; set; }

        /// <summary>
        /// 默认分配人
        /// </summary>
        [StringLength(100)]
        public string DefaultAssignee { get; set; }

        /// <summary>
        /// 预计持续天数
        /// </summary>
        public int? EstimatedDuration { get; set; }

        /// <summary>
        /// 阶段颜色
        /// </summary>
        [StringLength(20)]
        public string Color { get; set; }
    }
}