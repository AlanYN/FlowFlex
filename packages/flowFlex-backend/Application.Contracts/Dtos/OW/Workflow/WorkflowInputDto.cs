using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.Dtos.OW.Stage;

namespace FlowFlex.Application.Contracts.Dtos.OW.Workflow
{
    /// <summary>
    /// Workflow create/update input DTO
    /// </summary>
    public class WorkflowInputDto
    {
        /// <summary>
        /// 流程名称
        /// </summary>

        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// 流程描述
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
        /// Workflow version
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// JSON for workflow configuration (stages, settings, etc.)
        /// </summary>
        public string? ConfigJson { get; set; }

        /// <summary>
        /// Stages to be created with this workflow
        /// </summary>
        public List<StageInputDto>? Stages { get; set; }
    }
}