using AutoMapper;
using AutoMapper.Internal;
using Item.Common.Lib.Attr;
using Microsoft.OpenApi.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Asn1.X509;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FlowFlex.Infrastructure;

public static class UnisExtensions
{
    public static void SetAnyValue(this PropertyInfo property, object target, object value)
    {
        ArgumentNullException.ThrowIfNull(property);

        ArgumentNullException.ThrowIfNull(target);

        if (value != null)
        {
            value = JToken.FromObject(value).ToObject(property.PropertyType);
        }

        property.SetValue(target, value);
    }

    public static T GetChangeValue<T>(this object value)
    {
        if (value == null)
            return default;

        if (typeof(T) == value.GetType())
            return (T)value;

        return JToken.FromObject(value).ToObject<T>();
    }

    public static bool IsValueEqual(this object value, object target)
    {
        if (value == null && target == null)
            return true;
        else if (value == null && target != null)
            return false;
        else if (value != null && target == null)
            return false;

        var a = JToken.FromObject(value);
        var b = JToken.FromObject(target);

        return a.ToString(Formatting.None).Equals(b.ToString(Formatting.None));
    }

    public static string ChangeTypeString(object value)
    {
        var type = value.GetType();
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return ChangeTypeString(type.GetProperty("Value").GetValue(value));
        }

        if (value is JArray jArray)
        {
            return jArray.ToString(Formatting.None);
        }
        if (value is JObject jObject)
        {
            return jObject.ToString(Formatting.None);
        }
        if (value is IList list)
        {
            var strs = new List<string>();
            foreach (var item in list)
            {
                strs.Add(item.ToString());
            }
            return string.Join(",", strs);
        }
        else if (value is DateTime dateTime)
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        else if (value is DateTimeOffset datetimeOffset)
            return datetimeOffset.ToString("yyyy-MM-dd HH:mm:ss zzz");
        else if (value is long or double or decimal or int)
            return value.ToString();
        else if (value is string)
            return value.ToString();
        else
            return JsonConvert.SerializeObject(value);
    }

    public static object ChangeTypeObject(string strValue, Type type)
    {
        object value = null;

        Type nullableType = null;
        Type gType = type;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            nullableType = type;
            gType = type.GetGenericArguments().First();
        }

        if (nullableType != null && string.IsNullOrWhiteSpace(strValue))
            return null;

        if (strValue == null)
            return null;

        if (gType != typeof(string) && string.IsNullOrEmpty(strValue))
        {
            if (IsNumericType(gType))
            {
                strValue = "0";
            }
            else
            {
                return null;
            }
        }

        if (typeof(string).IsAssignableFrom(gType))
            value = strValue;
        else if (typeof(long).IsAssignableFrom(gType))
            value = Convert.ToInt64(string.IsNullOrWhiteSpace(strValue) ? "0" : strValue);
        else if (typeof(int).IsAssignableFrom(gType))
            value = Convert.ToInt32(string.IsNullOrWhiteSpace(strValue) ? "0" : strValue);
        else if (typeof(double).IsAssignableFrom(gType))
            value = Convert.ToDouble(string.IsNullOrWhiteSpace(strValue) ? "0" : strValue);
        else if (typeof(decimal).IsAssignableFrom(gType))
            value = Convert.ToDecimal(string.IsNullOrWhiteSpace(strValue) ? "0" : strValue);
        else if (typeof(DateTime).IsAssignableFrom(gType))
            value = Convert.ToDateTime(strValue);
        else if (typeof(DateTimeOffset).IsAssignableFrom(gType))
            value = DateTimeOffset.Parse(strValue);
        else if (typeof(bool).IsAssignableFrom(gType))
            value = Convert.ToBoolean(strValue);
        else if (typeof(JArray).IsAssignableFrom(gType))
            value = JArray.Parse(strValue);
        else if (typeof(JObject).IsAssignableFrom(gType))
            value = JObject.Parse(strValue);
        else if (typeof(List<string>).IsAssignableFrom(gType))
            value = strValue.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList();
        else if (typeof(string[]).IsAssignableFrom(gType))
            value = strValue.Split(",", StringSplitOptions.RemoveEmptyEntries).ToArray();
        else
            value = JsonConvert.DeserializeObject(strValue, gType);

        if (nullableType != null)
        {
            var gtype = type.GetGenericArguments().First();
            var constructor = type.GetConstructor([gtype]);
            value = constructor.Invoke([Convert.ChangeType(value, gtype)]);
        }

        return value;
    }

    /// <summary>
    /// Determines whether the type is a numeric type
    /// </summary>
    /// <param name="type">The type to check</param>
    /// <returns>True if the type is numeric, otherwise false</returns>
    private static bool IsNumericType(Type type)
    {
        return typeof(int).IsAssignableFrom(type) ||
               typeof(long).IsAssignableFrom(type) ||
               typeof(double).IsAssignableFrom(type) ||
               typeof(decimal).IsAssignableFrom(type) ||
               typeof(float).IsAssignableFrom(type) ||
               typeof(short).IsAssignableFrom(type) ||
               typeof(byte).IsAssignableFrom(type);
    }

    public static Dictionary<K, T> SelectMap<K, S, T>(this Dictionary<K, S> diction, Func<S, T> map)
    {
        var result = new Dictionary<K, T>();
        foreach (var item in diction)
        {
            result.Add(item.Key, map(item.Value));
        }
        return result;
    }

    public static void TryAddOrUpdate<K, T>(this Dictionary<K, T> diction, K key, T value)
    {
        if (diction.ContainsKey(key))
            diction[key] = value;
        else
            diction.TryAdd(key, value);
    }

    public static int Value(this Enum @enum)
    {
        return Convert.ToInt32(@enum);
    }

    /// <summary>
    /// Converts the first letter to lowercase
    /// </summary>
    /// <param name="str">The string to convert</param>
    /// <returns>String with first letter converted to lowercase</returns>
    public static string ToLowercaseFirstLetter(this string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return str;

        char firstCharLower = char.ToLower(str.FirstOrDefault());
        string result = firstCharLower + str[1..];
        return result;
    }

    public static string TrimEnd(this string str, string trimString)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        if (trimString.Length > str.Length)
            return str;

        if (str.EndsWith(trimString))
            return str[..^trimString.Length];

        return str;
    }

    /// <summary>
    /// Batch update extension method, returns data to be added, updated, and deleted
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="models">The data to be modified</param>
    /// <param name="existModels">Existing data</param>
    /// <param name="selectId">id selector</param>
    /// <returns></returns>
    public static (List<T> CreateItems, List<T> UpdateItems, List<T> DeleteIds) SelectBatchUpdate<T>(this List<T> models, List<T> existModels,
       Expression<Func<T, long>> selectId)
    {
        var idName = (selectId.Body as MemberExpression).Member.Name;
        var param = Expression.Parameter(typeof(T), "model");

        var existIds = existModels.Select(selectId.Compile().Invoke).ToList();
        var existIdsExp = Expression.Constant(existIds);

        var containsExp = typeof(List<long>).GetMethod(nameof(List<long>.Contains));

        var whereCreateExp =
            Expression.Lambda<Func<T, bool>>(
                Expression.Not(
                    Expression.Call(existIdsExp, containsExp, Expression.Property(param, idName))), param);

        var whereUpdateExp =
            Expression.Lambda<Func<T, bool>>(
                Expression.Call(existIdsExp, containsExp, Expression.Property(param, idName)), param);

        var createItems = models.Where(whereCreateExp.Compile()).ToList();
        var updateItems = models.Where(whereUpdateExp.Compile()).ToList();
        var deleteIds = existModels.Select(selectId.Compile()).Except(updateItems.Select(selectId.Compile())).ToList();

        var idsExp = Expression.Constant(deleteIds);

        var idProp = Expression.Property(param, idName);
        var callExp = Expression.Call(idsExp, containsExp, idProp);

        var deleteWhere = Expression.Lambda<Func<T, bool>>(callExp, param).Compile();
        var deleteItems = existModels.Where(deleteWhere).ToList();

        return (createItems, updateItems, deleteItems);
    }

    public static bool ValueEqual<T>(this List<T> list, List<T> target)
    {
        if (list.Count != target.Count)
            return false;
        for (int i = 0; i < list.Count; i++)
        {
            if (!list[i].Equals(target[i]))
                return false;
        }

        return true;
    }

    public static bool ValueEqual(this JToken list, JToken target)
    {
        if (list == null && target != null)
            return false;
        else if (list != null && target == null)
            return false;
        else if (list == null && target == null)
            return false;
        else
        {
            return list.ToString(Formatting.None) == target.ToString(Formatting.None);
        }
    }

    public static T ToEnum<T>(this string enumValue) where T : Enum
    {
        var member = typeof(T).GetFields()
            .FirstOrDefault(x => x.GetCustomAttribute<EnumValueAttribute>()?.Name == enumValue || x.ToString() == enumValue);
        if (member != null)
        {
            return (T)member.GetValue(null);
        }
        return default;
    }

    public static string ToEnumValue(this Enum @enum)
    {
        try
        {
            var enumValue = @enum.GetAttributeOfType<EnumValueAttribute>();
            return enumValue != null ? enumValue.Name.ToString() : @enum.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    public static IMappingExpression<TSource, TDestination> PropertyMap<TSource, TDestination, TMember>(
        this IMappingExpression<TSource, TDestination> mapping,
         Expression<Func<TDestination, TMember>> destExp, Expression<Func<TSource, TMember>> sourceExp
        )
    {
        mapping.ForMember(destExp, x => x.MapFrom(sourceExp));

        return mapping;
    }

    public static string GetEnumDisplayName(this Enum @enum)
    {
        var enumValue = @enum.GetAttributeOfType<EnumValueAttribute>();
        return enumValue?.Name ?? @enum.ToString();
    }
}
