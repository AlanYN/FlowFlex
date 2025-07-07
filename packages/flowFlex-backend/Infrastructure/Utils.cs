using System.Collections;

namespace FlowFlex.Infrastructure;

public static class Utils
{
    public static void TraverseProperties<T>(object obj, List<T> models, HashSet<object> visited = null)
    {
        if (obj == null) return;

        visited ??= [];
        if (visited.Contains(obj)) return;
        visited.Add(obj);

        var type = obj.GetType();
        var properties = type.GetProperties();
        if (type.IsAssignableTo(typeof(IEnumerable)))
        {
            var objects = obj as IEnumerable;
            foreach (var item in objects)
            {
                if (item != null && IsNotPrimitiveType(item.GetType()))
                {
                    TraverseProperties(item, models, visited);
                }
            }
        }
        else
        {
            if (obj is T fullDisplay)
            {
                models.Add(fullDisplay);
            }
            foreach (var property in properties)
            {
                if (property.CanRead == false)
                    continue;

                object propertyValue = property.GetValue(obj);
                if (propertyValue == null)
                    continue;

                if (IsNotPrimitiveType(property.PropertyType))
                {
                    TraverseProperties(propertyValue, models, visited);
                }
            }
        }
    }

    public static bool IsNotPrimitiveType(Type type)
    {
        return !(type.IsPrimitive || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) &&
                !type.IsEnum &&
                type != typeof(DateTimeOffset) &&
                type != typeof(DateTime) &&
                type != typeof(string);
    }

    public static object ChangeType(object value, Type conversionType)
    {
        try
        {
            if (Nullable.GetUnderlyingType(conversionType) != null)
            {
                if (value == null)
                {
                    return null;
                }
                conversionType = Nullable.GetUnderlyingType(conversionType);
            }

            if (conversionType.IsEnum)
            {
                if (value is string strValue)
                {
                    if (strValue == string.Empty)
                        return null;

                    return Enum.Parse(conversionType, value.ToString());
                }
                else
                {
                    return Enum.ToObject(conversionType, value);
                }
            }
            if (conversionType == typeof(DateTimeOffset))
            {
                return DateTimeOffset.Parse(value.ToString());
            }
            else if (conversionType == typeof(DateTime))
            {
                return DateTime.Parse(value.ToString());
            }
            else if (conversionType == typeof(int))
            {
                return int.Parse(value.ToString());
            }
            else if (conversionType == typeof(long))
            {
                return long.Parse(value.ToString());
            }
            else if (conversionType == typeof(double))
            {
                return double.Parse(value.ToString());
            }
            else if (conversionType == typeof(bool))
            {
                return bool.Parse(value.ToString());
            }
            else if (conversionType == typeof(decimal))
            {
                return decimal.Parse(value.ToString());
            }
            else
            {
                return Convert.ChangeType(value, conversionType);
            }
        }
        catch
        {
            throw;
        }
    }

    public static bool IsSnowflakeId(this object obj)
    {
        if (obj == null)
            return false;

        var str = obj.ToString();
        if (long.TryParse(str, out long snowflakeId))
        {
            long maxSnowflakeId = unchecked((1L << 63) - 1);
            return snowflakeId >= 0 && snowflakeId <= maxSnowflakeId && snowflakeId.ToString().Length >= 18;
        }
        return false;
    }

}
