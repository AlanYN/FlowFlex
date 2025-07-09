using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Newtonsoft.Json;
using FlowFlex.Application.Contracts.Dtos.OW.Common;

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
    [JsonConverter(typeof(NullableLongConverter))]
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
    /// 关联的工作流ID（向后兼容）
    /// </summary>
    [JsonConverter(typeof(NullableLongConverter))]
    public long? WorkflowId { get; set; }

    /// <summary>
    /// 关联的阶段ID（向后兼容）
    /// </summary>
    [JsonConverter(typeof(NullableLongConverter))]
    public long? StageId { get; set; }

    /// <summary>
    /// 多个工作流和阶段的关联配置
    /// </summary>
    /// <example>
    /// [
    ///   {
    ///     "workflowId": "1942226709378109440",
    ///     "stageId": "1942226861090279424"
    ///   }
    /// ]
    /// </example>
    public List<AssignmentDto> Assignments { get; set; } = new List<AssignmentDto>();

    /// <summary>
    /// 问卷分组
    /// </summary>
    public List<QuestionnaireSectionInputDto> Sections { get; set; } = new List<QuestionnaireSectionInputDto>();
}

/// <summary>
/// Newtonsoft.Json 自定义转换器，用于处理空字符串到 nullable long 的转换
/// </summary>
public class NullableLongConverter : JsonConverter<long?>
{
    public override void WriteJson(JsonWriter writer, long? value, JsonSerializer serializer)
    {
        if (value.HasValue)
        {
            writer.WriteValue(value.Value);
        }
        else
        {
            writer.WriteNull();
        }
    }

    public override long? ReadJson(JsonReader reader, Type objectType, long? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonToken.String)
        {
            var stringValue = reader.Value?.ToString();
            if (string.IsNullOrEmpty(stringValue))
            {
                return null;
            }
            if (long.TryParse(stringValue, out var result))
            {
                return result;
            }
            return null; // 无法解析时返回 null 而不是抛出异常
        }

        if (reader.TokenType == JsonToken.Integer)
        {
            return Convert.ToInt64(reader.Value);
        }

        return null;
    }
}