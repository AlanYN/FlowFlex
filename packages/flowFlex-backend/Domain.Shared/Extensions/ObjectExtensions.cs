using System;

namespace FlowFlex.Domain.Shared.Extensions;

public static class ObjectExtensions
{
    public static T GetChangeValue<T>(this object value)
    {
        if (value == null)
            return default(T);

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default(T);
        }
    }
}
