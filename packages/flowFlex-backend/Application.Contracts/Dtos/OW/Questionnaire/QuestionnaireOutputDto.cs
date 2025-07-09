using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using Item.Common.Lib.JsonConverts;
using FlowFlex.Application.Contracts.Dtos.OW.Common;

namespace FlowFlex.Application.Contracts.Dtos.OW.Questionnaire;

/// <summary>
/// Questionnaire output DTO
/// </summary>
public class QuestionnaireOutputDto
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [JsonConverter(typeof(ValueToStringConverter))]
    public long Id { get; set; }

    /// <summary>
    /// 问卷名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 问卷描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 问卷类型
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// 问卷状态
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// 问卷结构定义（JSON）
    /// </summary>
    public string StructureJson { get; set; }

    /// <summary>
    /// 问卷版本号
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// 是否为模板
    /// </summary>
    public bool IsTemplate { get; set; }

    /// <summary>
    /// 模板来源ID
    /// </summary>
    [JsonConverter(typeof(ValueToStringConverter))]
    public long? TemplateId { get; set; }

    /// <summary>
    /// 预览图URL
    /// </summary>
    public string PreviewImageUrl { get; set; }

    /// <summary>
    /// 问卷分类
    /// </summary>
    public string Category { get; set; }

    /// <summary>
    /// 问卷标签（JSON数组）
    /// </summary>
    public string TagsJson { get; set; }

    /// <summary>
    /// 预计填写时间（分钟）
    /// </summary>
    public int EstimatedMinutes { get; set; }

    /// <summary>
    /// 问题总数
    /// </summary>
    public int TotalQuestions { get; set; }

    /// <summary>
    /// 必填问题数
    /// </summary>
    public int RequiredQuestions { get; set; }

    /// <summary>
    /// 是否允许保存草稿
    /// </summary>
    public bool AllowDraft { get; set; }

    /// <summary>
    /// 是否允许多次提交
    /// </summary>
    public bool AllowMultipleSubmissions { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 关联的工作流ID
    /// </summary>
    [JsonConverter(typeof(ValueToStringConverter))]
    [Description("关联的工作流ID - 向后兼容字段")]
    public long? WorkflowId { get; set; }

    /// <summary>
    /// 关联的阶段ID
    /// </summary>
    [JsonConverter(typeof(ValueToStringConverter))]
    [Description("关联的阶段ID - 向后兼容字段")]
    public long? StageId { get; set; }

    /// <summary>
    /// 关联的 workflow-stage 组合列表
    /// </summary>
    [Description("问卷关联的所有 workflow-stage 组合")]
    public List<AssignmentDto> Assignments { get; set; } = new List<AssignmentDto>();

    /// <summary>
    /// 问卷分组
    /// </summary>
    public List<QuestionnaireSectionDto> Sections { get; set; } = new List<QuestionnaireSectionDto>();

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
}