using System;

namespace FlowFlex.Domain.Shared.Attr;

/// <summary>
/// Not displayed in the dynamic attribute list
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class IgnoreDisplayAttribute : Attribute
{
}
