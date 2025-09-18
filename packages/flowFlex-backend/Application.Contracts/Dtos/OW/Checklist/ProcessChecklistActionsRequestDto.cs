using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.Checklist;

/// <summary>
/// Process checklist actions request
/// </summary>
public class ProcessChecklistActionsRequestDto
{
    /// <summary>
    /// Onboarding ID
    /// </summary>
    public long OnboardingId { get; set; }

    /// <summary>
    /// Lead ID
    /// </summary>
    public string LeadId { get; set; } = string.Empty;

    /// <summary>
    /// Workflow ID
    /// </summary>
    public long WorkflowId { get; set; }

    /// <summary>
    /// Workflow name
    /// </summary>
    public string WorkflowName { get; set; } = string.Empty;

    /// <summary>
    /// Completed stage ID
    /// </summary>
    public long CompletedStageId { get; set; }

    /// <summary>
    /// Completed stage name
    /// </summary>
    public string CompletedStageName { get; set; } = string.Empty;

    /// <summary>
    /// Next stage ID
    /// </summary>
    public long? NextStageId { get; set; }

    /// <summary>
    /// Next stage name
    /// </summary>
    public string NextStageName { get; set; } = string.Empty;

    /// <summary>
    /// Completion rate
    /// </summary>
    public decimal CompletionRate { get; set; }

    /// <summary>
    /// Is final stage
    /// </summary>
    public bool IsFinalStage { get; set; }

    /// <summary>
    /// Business context
    /// </summary>
    public object? BusinessContext { get; set; }

    /// <summary>
    /// Components
    /// </summary>
    public object? Components { get; set; }

    /// <summary>
    /// Tenant ID
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Source
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Priority
    /// </summary>
    public string Priority { get; set; } = string.Empty;

    /// <summary>
    /// Original event ID
    /// </summary>
    public string OriginalEventId { get; set; } = string.Empty;

    /// <summary>
    /// Stage components
    /// </summary>
    public List<StageComponentDto> StageComponents { get; set; } = new List<StageComponentDto>();

    /// <summary>
    /// Current user ID
    /// </summary>
    public long? CurrentUserId { get; set; }
}

/// <summary>
/// Stage component DTO
/// </summary>
public class StageComponentDto
{
    /// <summary>
    /// Component key
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Checklist IDs
    /// </summary>
    public List<long> ChecklistIds { get; set; } = new List<long>();
}

/// <summary>
/// Checklist action processing result
/// </summary>
public class ChecklistActionProcessingResultDto
{
    /// <summary>
    /// Success flag
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Total tasks processed
    /// </summary>
    public int TotalTasksProcessed { get; set; }

    /// <summary>
    /// Total actions published
    /// </summary>
    public int TotalActionsPublished { get; set; }

    /// <summary>
    /// Processing messages
    /// </summary>
    public List<string> Messages { get; set; } = new List<string>();

    /// <summary>
    /// Error messages
    /// </summary>
    public List<string> Errors { get; set; } = new List<string>();
}
