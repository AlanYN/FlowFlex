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

    public static bool IsValueEqual(this object value, object target)
    {
        if (value == null && target == null)
            return true;
        
        if (value == null || target == null)
            return false;
            
        return value.Equals(target);
    }
} 
