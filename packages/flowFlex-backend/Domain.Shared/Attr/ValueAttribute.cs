using System;

namespace FlowFlex.Domain.Shared.Attr
{
    /// <summary>
    /// Attribute to mark properties as value fields in dynamic data containers
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ValueAttribute : Attribute
    {
    }
}
