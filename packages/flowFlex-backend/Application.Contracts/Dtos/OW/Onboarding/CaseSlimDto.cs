namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding;

/// <summary>
/// Lightweight case summary DTO — only the fields needed by CRM's
/// "Connect to WFE Workflow" dialog. Intentionally excludes all large
/// JSONB columns (stages_progress_json, permission snapshots, etc.)
/// to keep the response small and fast.
/// </summary>
public class CaseSlimDto
{
    /// <summary>Case primary key ID</summary>
    public long Id { get; set; }

    /// <summary>Human-readable case name</summary>
    public string CaseName { get; set; } = string.Empty;

    /// <summary>Workflow ID this case belongs to</summary>
    public long WorkflowId { get; set; }

    /// <summary>Current case status (e.g. InProgress, Completed)</summary>
    public string Status { get; set; } = string.Empty;
}
