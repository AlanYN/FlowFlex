using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.Questionnaire;

/// <summary>
/// Questionnaire create/update input DTO
/// </summary>
public class QuestionnaireInputDto
{
    /// <summary>
    /// 问卷名称
    /// </summary>
    
    [StringLength(100)]
    public string Name { get; set; }

    /// <summary>
    /// 问卷描述
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; }

    /// <summary>
    /// 问卷类型（Template/Instance）
    /// </summary>
    [StringLength(20)]
    public string Type { get; set; } = "Template";

    /// <summary>
    /// 问卷状态（Draft/Published/Archived）
    /// </summary>
    [StringLength(20)]
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// 问卷结构定义（JSON）
    /// </summary>
    public string StructureJson { get; set; }

    /// <summary>
    /// 问卷版本号
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// 是否为模板
    /// </summary>
    public bool IsTemplate { get; set; } = true;

    /// <summary>
    /// 模板来源ID
    /// </summary>
    public long? TemplateId { get; set; }

    /// <summary>
    /// 预览图URL
    /// </summary>
    [StringLength(500)]
    public string PreviewImageUrl { get; set; }

    /// <summary>
    /// 问卷分类
    /// </summary>
    [StringLength(50)]
    public string Category { get; set; }

    /// <summary>
    /// 问卷标签（JSON数组）
    /// </summary>
    public string TagsJson { get; set; }

    /// <summary>
    /// 预计填写时间（分钟）
    /// </summary>
    public int EstimatedMinutes { get; set; } = 0;

    /// <summary>
    /// 是否允许保存草稿
    /// </summary>
    public bool AllowDraft { get; set; } = true;

    /// <summary>
    /// 是否允许多次提交
    /// </summary>
    public bool AllowMultipleSubmissions { get; set; } = false;

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 关联的工作流ID
    /// </summary>
    public long? WorkflowId { get; set; }

    /// <summary>
    /// 关联的阶段ID
    /// </summary>
    public long? StageId { get; set; }

    /// <summary>
    /// 问卷分组
    /// </summary>
    public List<QuestionnaireSectionInputDto> Sections { get; set; } = new List<QuestionnaireSectionInputDto>();
}