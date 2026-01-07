namespace FlowFlex.Application.Contracts.Dtos.OW.InternalNote;

/// <summary>
/// Resolve note request model
/// </summary>
public class ResolveNoteRequest
{
    /// <summary>
    /// Resolved by
    /// </summary>
    public string ResolvedBy { get; set; } = string.Empty;

    /// <summary>
    /// Resolution notes
    /// </summary>
    public string ResolutionNotes { get; set; } = string.Empty;
}
