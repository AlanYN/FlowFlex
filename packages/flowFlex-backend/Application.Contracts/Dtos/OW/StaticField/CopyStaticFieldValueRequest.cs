namespace FlowFlex.Application.Contracts.Dtos.OW.StaticField;

/// <summary>
/// Copy static field value request
/// </summary>
public class CopyStaticFieldValueRequest
{
    /// <summary>
    /// Source onboarding ID
    /// </summary>
    public long SourceOnboardingId { get; set; }

    /// <summary>
    /// Target onboarding ID
    /// </summary>
    public long TargetOnboardingId { get; set; }

    /// <summary>
    /// Field names to copy (if empty, copy all)
    /// </summary>
    public List<string> FieldNames { get; set; } = new();
}
