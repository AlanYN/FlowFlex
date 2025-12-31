using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.IServices;

namespace FlowFlex.Application.Contracts.Dtos.AI;

public class CreateStageComponentsRequest
{
    /// <summary>
    /// Workflow ID
    /// </summary>
    [Required]
    public long WorkflowId { get; set; }

    /// <summary>
    /// Generated stages
    /// </summary>
    public List<AIStageGenerationResult> Stages { get; set; } = new();

    /// <summary>
    /// Generated checklists
    /// </summary>
    public List<AIChecklistGenerationResult> Checklists { get; set; } = new();

    /// <summary>
    /// Generated questionnaires
    /// </summary>
    public List<AIQuestionnaireGenerationResult> Questionnaires { get; set; } = new();
}

public class EnhanceWorkflowRequest
{
    /// <summary>
    /// Enhancement description
    /// </summary>
    [Required]
    public string Enhancement { get; set; } = string.Empty;

    /// <summary>
    /// Additional context
    /// </summary>
    public string Context { get; set; } = string.Empty;
}

public class ParseRequirementsRequest
{
    /// <summary>
    /// Natural language requirements description
    /// </summary>
    [Required]
    public string NaturalLanguage { get; set; } = string.Empty;

    /// <summary>
    /// Context information
    /// </summary>
    public string Context { get; set; } = string.Empty;

    /// <summary>
    /// Optional explicit AI model override
    /// </summary>
    public string? ModelProvider { get; set; }
    public string? ModelName { get; set; }
    public string? ModelId { get; set; }
}

public class AIServiceStatus
{
    public bool IsAvailable { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public List<string> Features { get; set; } = new();
    public string Version { get; set; } = string.Empty;
    public DateTime LastHealthCheck { get; set; }
}
