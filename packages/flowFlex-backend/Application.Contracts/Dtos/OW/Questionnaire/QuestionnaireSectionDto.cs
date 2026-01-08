using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.Questionnaire;

/// <summary>
/// Questionnaire Section DTO
/// </summary>
public class QuestionnaireSectionDto
{
    /// <summary>
    /// Section ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Questionnaire ID
    /// </summary>
    public long QuestionnaireId { get; set; }

    /// <summary>
    /// Section title
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Section description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Section icon
    /// </summary>
    public string Icon { get; set; }

    /// <summary>
    /// Section color
    /// </summary>
    public string Color { get; set; }

    /// <summary>
    /// Is collapsible
    /// </summary>
    public bool IsCollapsible { get; set; }

    /// <summary>
    /// Is expanded by default
    /// </summary>
    public bool IsExpanded { get; set; }

    /// <summary>
    /// Questions in this section
    /// </summary>
    public List<QuestionDto> Questions { get; set; } = new List<QuestionDto>();
}

/// <summary>
/// Question DTO
/// </summary>
public class QuestionDto
{
    /// <summary>
    /// Question ID
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Section ID
    /// </summary>
    public long? SectionId { get; set; }

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
    /// Question options (for multiple choice, etc.)
    /// </summary>
    public List<QuestionOptionDto> Options { get; set; } = new List<QuestionOptionDto>();

    /// <summary>
    /// Validation rules
    /// </summary>
    public object ValidationRules { get; set; }

    /// <summary>
    /// Additional properties
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Question Option DTO
/// </summary>
public class QuestionOptionDto
{
    /// <summary>
    /// Option ID
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Option value
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// Option label
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int Order { get; set; }
}