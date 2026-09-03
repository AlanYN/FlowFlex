using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.TriggerGraph
{
    /// <summary>
    /// Lightweight workflow info for the trigger graph editor left panel
    /// </summary>
    public class WorkflowNodeInfoDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsDefault { get; set; }

        /// <summary>Stages with their component info</summary>
        public List<StageNodeInfoDto> Stages { get; set; } = new();
    }

    /// <summary>
    /// Stage with its components (fields / questionnaires / checklists) for condition configuration
    /// </summary>
    public class StageNodeInfoDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }

        /// <summary>Required Fields in this stage</summary>
        public List<FieldOptionDto> Fields { get; set; } = new();

        /// <summary>Questionnaires in this stage with their questions</summary>
        public List<QuestionnaireNodeDto> Questionnaires { get; set; } = new();

        /// <summary>Checklists in this stage with their tasks</summary>
        public List<ChecklistNodeDto> Checklists { get; set; } = new();
    }

    public class FieldOptionDto
    {
        /// <summary>DefineField.Id（string 形式）</summary>
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
    }

    public class QuestionnaireNodeDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<QuestionOptionDto> Questions { get; set; } = new();
    }

    public class QuestionOptionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        /// <summary>Question type: radio / checkbox / checkboxes / text / number / date / select / etc.</summary>
        public string Type { get; set; } = string.Empty;
        /// <summary>Selectable options for radio / checkbox / select questions. Empty for other types.</summary>
        public List<QuestionOptionItemDto> Options { get; set; } = new();
    }

    /// <summary>
    /// A single selectable option within a question.
    /// Label = display text; Value = stored value in answer JSON (may be slug/snake_case).
    /// </summary>
    public class QuestionOptionItemDto
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class ChecklistNodeDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<TaskOptionDto> Tasks { get; set; } = new();
    }

    public class TaskOptionDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TaskType { get; set; } = string.Empty;
    }
}
