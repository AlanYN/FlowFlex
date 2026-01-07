namespace FlowFlex.Application.Contracts.Dtos.OW.Workflow;

/// <summary>
/// Request model for creating a new version
/// </summary>
public class CreateVersionRequest
{
    /// <summary>
    /// Optional reason for creating the version
    /// </summary>
    public string ChangeReason { get; set; }
}
