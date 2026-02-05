namespace FlowFlex.Domain.Shared.Attr;

/// <summary>
/// Attribute to mark properties that should be ignored in display/UI contexts
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class IgnoreDisplayAttribute : Attribute
{
}
