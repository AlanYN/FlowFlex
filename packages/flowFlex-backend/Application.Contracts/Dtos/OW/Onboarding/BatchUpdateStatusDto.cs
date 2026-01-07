namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding;

/// <summary>
/// Batch update status DTO
/// </summary>
public class BatchUpdateStatusDto
{
    /// <summary>
    /// Onboarding IDs
    /// </summary>
    public List<long> Ids { get; set; }

    /// <summary>
    /// Target status
    /// </summary>
    public string Status { get; set; }
}
