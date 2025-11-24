using Domain.Shared.Enums;

namespace FlowFlex.Application.Contracts.Dtos.Integration;

/// <summary>
/// Integration action DTO
/// </summary>
public class IntegrationActionDto
{
    public long Id { get; set; }
    public string ActionCode { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public ActionType ActionType { get; set; }
    public ActionStatus Status { get; set; }
    public List<long> AssignedWorkflowIds { get; set; } = new();
    public List<string> AssignedWorkflowNames { get; set; } = new();
    public Dictionary<string, object> Configuration { get; set; } = new();
}

