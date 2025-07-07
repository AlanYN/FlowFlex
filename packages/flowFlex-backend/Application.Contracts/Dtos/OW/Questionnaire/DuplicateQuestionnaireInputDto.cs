using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Questionnaire;

/// <summary>
/// Duplicate questionnaire input DTO
/// </summary>
public class DuplicateQuestionnaireInputDto
{
    /// <summary>
    /// 新问卷名称
    /// </summary>
    
    [StringLength(100)]
    public string Name { get; set; }

    /// <summary>
    /// 新问卷描述
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; }

    /// <summary>
    /// 新问卷分类
    /// </summary>
    [StringLength(50)]
    public string Category { get; set; }

    /// <summary>
    /// 是否复制结构
    /// </summary>
    public bool CopyStructure { get; set; } = true;

    /// <summary>
    /// 是否设为模板
    /// </summary>
    public bool SetAsTemplate { get; set; } = true;
}