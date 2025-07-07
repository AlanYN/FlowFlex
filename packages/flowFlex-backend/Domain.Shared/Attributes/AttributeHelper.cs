using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FlowFlex.Domain.Shared.Attributes;

/// <summary>
/// Attribute操作帮助�?
/// </summary>
public static class AttributeHelper
{
    // 用于存储运行时添加的Attributes
    private static readonly ConcurrentDictionary<MemberInfo, List<Attribute>> _runtimeAttributes = new();

    /// <summary>
    /// 动态添加Attribute到字�?
    /// </summary>
    /// <param name="fieldInfo">目标字段</param>
    /// <param name="attribute">要添加的Attribute实例</param>
    public static void AddAttributeToField(FieldInfo fieldInfo, Attribute attribute)
    {
        ArgumentNullException.ThrowIfNull(fieldInfo);
        ArgumentNullException.ThrowIfNull(attribute);
        
        AddRuntimeAttribute(fieldInfo, attribute);
    }

    /// <summary>
    /// 获取字段上的指定Attribute（包括运行时添加的）
    /// </summary>
    /// <typeparam name="T">Attribute类型</typeparam>
    /// <param name="fieldInfo">目标字段</param>
    /// <returns>指定类型的Attribute，如果不存在则返回null</returns>
    public static T GetAttribute<T>(FieldInfo fieldInfo) where T : Attribute
    {
        ArgumentNullException.ThrowIfNull(fieldInfo);

        // 先查找编译时的Attribute
        var attribute = fieldInfo.GetCustomAttribute<T>();
        if (attribute != null)
            return attribute;

        // 再查找运行时添加的Attribute
        return GetRuntimeAttribute<T>(fieldInfo);
    }

    private static void AddRuntimeAttribute(MemberInfo memberInfo, Attribute attribute)
    {
        _runtimeAttributes.AddOrUpdate(memberInfo,
            new List<Attribute> { attribute },
            (key, existingAttributes) =>
            {
                existingAttributes.Add(attribute);
                return existingAttributes;
            });
    }

    private static T GetRuntimeAttribute<T>(MemberInfo memberInfo) where T : Attribute
    {
        if (_runtimeAttributes.TryGetValue(memberInfo, out var attributes))
        {
            return attributes.OfType<T>().FirstOrDefault();
        }
        return null;
    }
} 
