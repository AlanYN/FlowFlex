using System;

namespace FlowFlex.Domain.Shared.Attr;

[AttributeUsage(AttributeTargets.Field)]
public class IgnoreEnumFieldAttribute : Attribute
{
}
