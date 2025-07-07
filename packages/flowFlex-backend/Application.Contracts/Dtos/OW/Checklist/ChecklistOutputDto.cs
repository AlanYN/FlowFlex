using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using FlowFlex.Application.Contracts.Dtos.OW.ChecklistTask;

namespace FlowFlex.Application.Contracts.Dtos.OW.Checklist;

/// <summary>
/// 📋 Checklist 输出 DTO
/// </summary>
/// <example>
/// {
///   "id": 1,
///   "name": "New Employee Onboarding",
///   "description": "Standard onboarding checklist for new employees",
///   "team": "HR",
///   "type": "Template",
///   "status": "Active",
///   "isTemplate": true,
///   "templateId": null,
///   "completionRate": 75.50,
///   "totalTasks": 8,
///   "completedTasks": 6,
///   "estimatedHours": 40,
///   "isActive": true,
///   "createDate": "2024-01-15T10:30:00+08:00",
///   "createBy": "admin",
///   "tasks": []
/// }
/// </example>
public class ChecklistOutputDto
{
    /// <summary>
    /// 主键ID
    /// </summary>
    /// <example>1</example>
    [Description("清单的唯一标识符")]
    public long Id { get; set; }

    /// <summary>
    /// 清单名称
    /// </summary>
    /// <example>New Employee Onboarding</example>
    [Description("清单的名称")]
    public string Name { get; set; }

    /// <summary>
    /// 清单描述
    /// </summary>
    /// <example>Standard onboarding checklist for new employees</example>
    [Description("清单的详细描述")]
    public string Description { get; set; }

    /// <summary>
    /// 所属团队
    /// </summary>
    /// <example>HR</example>
    [Description("负责此清单的团队或角色")]
    public string Team { get; set; }

    /// <summary>
    /// 清单类型
    /// </summary>
    /// <example>Template</example>
    [Description("清单类型：Template-模板，Instance-实例")]
    public string Type { get; set; }

    /// <summary>
    /// 清单状态
    /// </summary>
    /// <example>Active</example>
    [Description("清单状态：Active-活跃，Inactive-非活跃")]
    public string Status { get; set; }

    /// <summary>
    /// 是否为模板
    /// </summary>
    /// <example>true</example>
    [Description("标识这是否是一个模板清单")]
    public bool IsTemplate { get; set; }

    /// <summary>
    /// 模板来源ID
    /// </summary>
    /// <example>null</example>
    [Description("如果此清单是从模板创建的实例，则显示模板ID")]
    public long? TemplateId { get; set; }

    /// <summary>
    /// 完成率（0-100）
    /// </summary>
    /// <example>75.50</example>
    [Description("清单的完成百分比（0-100）")]
    public decimal CompletionRate { get; set; }

    /// <summary>
    /// 总任务数
    /// </summary>
    /// <example>8</example>
    [Description("清单中包含的任务总数")]
    public int TotalTasks { get; set; }

    /// <summary>
    /// 已完成任务数
    /// </summary>
    /// <example>6</example>
    [Description("已完成的任务数量")]
    public int CompletedTasks { get; set; }

    /// <summary>
    /// 预计完成时间（小时）
    /// </summary>
    /// <example>40</example>
    [Description("完成整个清单预计需要的时间（小时）")]
    public int EstimatedHours { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    /// <example>true</example>
    [Description("标识此清单是否处于激活状态")]
    public bool IsActive { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    /// <example>2024-01-15T10:30:00+08:00</example>
    [Description("清单的创建时间")]
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    /// <example>admin</example>
    [Description("创建此清单的用户")]
    public string CreateBy { get; set; }

    /// <summary>
    /// 关联工作流ID（可选）
    /// </summary>
    /// <example>null</example>
    [Description("关联的工作流ID")]
    public long? WorkflowId { get; set; }

    /// <summary>
    /// 关联阶段ID（可选）
    /// </summary>
    /// <example>null</example>
    [Description("关联的阶段ID")]
    public long? StageId { get; set; }

    /// <summary>
    /// 工作流名称（只读）
    /// </summary>
    /// <example>Customer Onboarding Workflow</example>
    [Description("关联工作流的名称")]
    public string WorkflowName { get; set; }

    /// <summary>
    /// 阶段名称（只读）
    /// </summary>
    /// <example>Initial Setup</example>
    [Description("关联阶段的名称")]
    public string StageName { get; set; }

    /// <summary>
    /// 任务项列表
    /// </summary>
    /// <example>[]</example>
    [Description("清单中包含的任务项列表")]
    public List<ChecklistTaskOutputDto> Tasks { get; set; } = new List<ChecklistTaskOutputDto>();
}