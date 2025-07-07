using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace FlowFlex.Application.Contracts.Dtos.OW.Checklist;

/// <summary>
/// 📋 Checklist 创建/更新输入 DTO
/// </summary>
/// <example>
/// {
///   "name": "New Employee Onboarding",
///   "description": "Standard onboarding checklist for new employees",
///   "team": "HR",
///   "type": "Template",
///   "status": "Active",
///   "isTemplate": true,
///   "templateId": null,
///   "estimatedHours": 40,
///   "isActive": true
/// }
/// </example>
public class ChecklistInputDto
{
    /// <summary>
    /// 清单名称
    /// </summary>
    /// <example>New Employee Onboarding</example>
    [Required(ErrorMessage = "清单名称不能为空")]
    [StringLength(100, ErrorMessage = "清单名称长度不能超过100个字符")]
    [Description("清单名称，必填项，最大长度100字符")]
    public string Name { get; set; }

    /// <summary>
    /// 清单描述
    /// </summary>
    /// <example>Standard onboarding checklist for new employees</example>
    [StringLength(500, ErrorMessage = "清单描述长度不能超过500个字符")]
    [Description("清单的详细描述信息，可选项")]
    public string Description { get; set; }

    /// <summary>
    /// 所属团队
    /// </summary>
    /// <example>HR</example>
    [StringLength(100, ErrorMessage = "团队名称长度不能超过100个字符")]
    [Description("负责此清单的团队或角色")]
    public string Team { get; set; }

    /// <summary>
    /// 清单类型（Template/Instance）
    /// </summary>
    /// <example>Template</example>
    [StringLength(20)]
    [Description("清单类型：Template-模板，Instance-实例")]
    public string Type { get; set; } = "Template";

    /// <summary>
    /// 清单状态（Active/Inactive）
    /// </summary>
    /// <example>Active</example>
    [StringLength(20)]
    [Description("清单状态：Active-活跃，Inactive-非活跃")]
    public string Status { get; set; } = "Active";

    /// <summary>
    /// 是否为模板
    /// </summary>
    /// <example>true</example>
    [Description("标识这是否是一个模板清单")]
    public bool IsTemplate { get; set; } = true;

    /// <summary>
    /// 模板来源ID（当从模板创建实例时使用）
    /// </summary>
    /// <example>null</example>
    [Description("如果此清单是从模板创建的实例，则填写模板ID")]
    public long? TemplateId { get; set; }

    /// <summary>
    /// 预计完成时间（小时）
    /// </summary>
    /// <example>40</example>
    [Range(0, int.MaxValue, ErrorMessage = "预计完成时间不能为负数")]
    [Description("完成整个清单预计需要的时间（小时）")]
    public int EstimatedHours { get; set; } = 0;

    /// <summary>
    /// 是否激活
    /// </summary>
    /// <example>true</example>
    [Description("标识此清单是否处于激活状态")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 关联工作流ID（可选）
    /// </summary>
    /// <example>null</example>
    [Description("将清单关联到特定工作流，可选")]
    public long? WorkflowId { get; set; }

    /// <summary>
    /// 关联阶段ID（可选）
    /// </summary>
    /// <example>null</example>
    [Description("将清单关联到特定阶段，可选")]
    public long? StageId { get; set; }
}