using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.Dtos.OW.Stage;

namespace FlowFlex.Application.Contracts.Dtos.OW.Workflow
{
    /// <summary>
    /// Create workflow from version input DTO
    /// </summary>
    public class CreateWorkflowFromVersionInputDto
    {
        /// <summary>
        /// 原始工作流ID
        /// </summary>
        
        public long OriginalWorkflowId { get; set; }

        /// <summary>
        /// 版本ID
        /// </summary>
        
        public long VersionId { get; set; }

        /// <summary>
        /// 新工作流名称
        /// </summary>
        
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// 新工作流描述
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// 是否为默认流程
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// 流程状态（active/inactive）
        /// </summary>
        [StringLength(20)]
        public string Status { get; set; } = "active";

        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTimeOffset StartDate { get; set; }

        /// <summary>
        /// 结束日期（可空，默认流程不可填写）
        /// </summary>
        public DateTimeOffset? EndDate { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 编辑后的阶段列表
        /// </summary>
        public List<StageInputDto> Stages { get; set; } = new List<StageInputDto>();
    }
}