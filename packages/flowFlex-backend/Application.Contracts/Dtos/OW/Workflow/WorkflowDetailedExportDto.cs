using System;
using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.Workflow
{
    /// <summary>
    /// Workflow detailed export DTO for CSV format
    /// </summary>
    public class WorkflowDetailedExportDto
    {
        /// <summary>
        /// 工作流基本信息
        /// </summary>
        public WorkflowInfoSection WorkflowInfo { get; set; }

        /// <summary>
        /// 工作流阶段列表
        /// </summary>
        public List<WorkflowStageSection> Stages { get; set; }
    }

    /// <summary>
    /// 工作流信息部分
    /// </summary>
    public class WorkflowInfoSection
    {
        /// <summary>
        /// 工作流名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 工作流描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 开始日期
        /// </summary>
        public string StartDate { get; set; }

        /// <summary>
        /// 结束日期
        /// </summary>
        public string EndDate { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 是否为默认工作流
        /// </summary>
        public string DefaultWorkflow { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// 创建日期
        /// </summary>
        public string CreatedDate { get; set; }

        /// <summary>
        /// 总阶段数
        /// </summary>
        public int TotalStages { get; set; }

        /// <summary>
        /// 总预估时长
        /// </summary>
        public string TotalEstimatedDuration { get; set; }
    }

    /// <summary>
    /// 工作流阶段部分
    /// </summary>
    public class WorkflowStageSection
    {
        /// <summary>
        /// 序号
        /// </summary>
        public int Sequence { get; set; }

        /// <summary>
        /// 阶段名称
        /// </summary>
        public string StageName { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 分配组
        /// </summary>
        public string AssignedGroup { get; set; }

        /// <summary>
        /// 负责人
        /// </summary>
        public string Assignee { get; set; }

        /// <summary>
        /// 预估时长
        /// </summary>
        public string EstimatedDuration { get; set; }
    }
}