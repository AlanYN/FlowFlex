using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Questionnaire;

/// <summary>
/// Questionnaire Section Input DTO
/// </summary>
public class QuestionnaireSectionInputDto
{
    /// <summary>
    /// Section ID (for updates)
    /// </summary>
    public long? Id { get; set; }

    /// <summary>
    /// Questionnaire ID
    /// </summary>
    public long QuestionnaireId { get; set; }

    /// <summary>
    /// Section title
    /// </summary>
    
    [StringLength(100)]
    public string Title { get; set; }

    /// <summary>
    /// Section description
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Section icon
    /// </summary>
    [StringLength(50)]
    public string Icon { get; set; }

    /// <summary>
    /// Section color
    /// </summary>
    [StringLength(20)]
    public string Color { get; set; }

    /// <summary>
    /// Is collapsible
    /// </summary>
    public bool IsCollapsible { get; set; } = true;

    /// <summary>
    /// Is expanded by default
    /// </summary>
    public bool IsExpanded { get; set; } = true;

    /// <summary>
    /// Questions in this section
    /// </summary>
    public List<QuestionInputDto> Questions { get; set; } = new List<QuestionInputDto>();
}

/// <summary>
/// Question Input DTO
/// </summary>
public class QuestionInputDto
{
    /// <summary>
    /// Question ID
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Question text
    /// </summary>
    
    public string Text { get; set; }

    /// <summary>
    /// Question type
    /// </summary>
    
    public string Type { get; set; }

    /// <summary>
    /// Is required
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Question options (for choice questions)
    /// </summary>
    public List<QuestionOptionDto> Options { get; set; } = new List<QuestionOptionDto>();

    /// <summary>
    /// Question settings
    /// </summary>
    public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Validation rules
    /// </summary>
    public Dictionary<string, object> Validation { get; set; } = new Dictionary<string, object>();
}