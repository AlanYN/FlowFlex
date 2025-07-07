using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace FlowFlex.Infrastructure.Attributes
{
    /// <summary>
    /// Attribute operation helper class.
    /// </summary>
    public static class AttributeHelper
    {
        // Used to store runtime-added attributes
        private static readonly ConcurrentDictionary<MemberInfo, List<Attribute>> _runtimeAttributes = new();

        /// <summary>
        /// Add an attribute to a type at runtime.
        /// </summary>
        /// <param name="type">Target type</param>
        /// <param name="attribute">Attribute instance to add</param>
        public static void AddAttributeToType(Type type, Attribute attribute)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(attribute);
            AddRuntimeAttribute(type, attribute);
        }

        /// <summary>
        /// Add an attribute to a property at runtime.
        /// </summary>
        /// <param name="propertyInfo">Target property</param>
        /// <param name="attribute">Attribute instance to add</param>
        public static void AddAttributeToProperty(PropertyInfo propertyInfo, Attribute attribute)
        {
            ArgumentNullException.ThrowIfNull(propertyInfo);
            ArgumentNullException.ThrowIfNull(attribute);
            AddRuntimeAttribute(propertyInfo, attribute);
        }

        /// <summary>
        /// Add an attribute to a field at runtime.
        /// </summary>
        /// <param name="fieldInfo">Target field</param>
        /// <param name="attribute">Attribute instance to add</param>
        public static void AddAttributeToField(FieldInfo fieldInfo, Attribute attribute)
        {
            ArgumentNullException.ThrowIfNull(fieldInfo);
            ArgumentNullException.ThrowIfNull(attribute);
            AddRuntimeAttribute(fieldInfo, attribute);
        }

        /// <summary>
        /// Create a CustomAttributeBuilder for a given attribute instance.
        /// </summary>
        /// <param name="attribute">Attribute instance</param>
        /// <returns>CustomAttributeBuilder</returns>
        private static CustomAttributeBuilder CreateCustomAttributeBuilder(Attribute attribute)
        {
            var attributeType = attribute.GetType();
            var properties = attributeType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .ToArray();
            var propertyValues = properties.Select(p => p.GetValue(attribute)).ToArray();
            var fields = attributeType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var fieldValues = fields.Select(f => f.GetValue(attribute)).ToArray();
            var constructor = attributeType.GetConstructors().First();
            var constructorArgs = constructor.GetParameters()
                .Select(p => GetDefaultValue(p.ParameterType))
                .ToArray();
            return new CustomAttributeBuilder(
                constructor,
                constructorArgs,
                properties,
                propertyValues,
                fields,
                fieldValues);
        }

        /// <summary>
        /// Get the default value for a type.
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>Default value</returns>
        private static object GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        /// <summary>
        /// Get a specific attribute from a type, including runtime-added attributes.
        /// </summary>
        /// <typeparam name="T">Attribute type</typeparam>
        /// <param name="type">Target type</param>
        /// <returns>Attribute instance or null if not found</returns>
        public static T GetAttribute<T>(Type type) where T : Attribute
        {
            ArgumentNullException.ThrowIfNull(type);
            var attribute = type.GetCustomAttribute<T>();
            if (attribute != null)
                return attribute;
            return GetRuntimeAttribute<T>(type);
        }

        /// <summary>
        /// Get a specific attribute from a property, including runtime-added attributes.
        /// </summary>
        /// <typeparam name="T">Attribute type</typeparam>
        /// <param name="propertyInfo">Target property</param>
        /// <returns>Attribute instance or null if not found</returns>
        public static T GetAttribute<T>(PropertyInfo propertyInfo) where T : Attribute
        {
            ArgumentNullException.ThrowIfNull(propertyInfo);
            var attribute = propertyInfo.GetCustomAttribute<T>();
            if (attribute != null)
                return attribute;
            return GetRuntimeAttribute<T>(propertyInfo);
        }

        /// <summary>
        /// Get a specific attribute from a field, including runtime-added attributes.
        /// </summary>
        /// <typeparam name="T">Attribute type</typeparam>
        /// <param name="fieldInfo">Target field</param>
        /// <returns>Attribute instance or null if not found</returns>
        public static T GetAttribute<T>(FieldInfo fieldInfo) where T : Attribute
        {
            ArgumentNullException.ThrowIfNull(fieldInfo);
            var attribute = fieldInfo.GetCustomAttribute<T>();
            if (attribute != null)
                return attribute;
            return GetRuntimeAttribute<T>(fieldInfo);
        }

        /// <summary>
        /// Check if a member contains a specific attribute, including runtime-added attributes.
        /// </summary>
        /// <typeparam name="T">Attribute type</typeparam>
        /// <param name="memberInfo">Target member</param>
        /// <returns>True if the attribute exists, otherwise false</returns>
        public static bool HasAttribute<T>(MemberInfo memberInfo) where T : Attribute
        {
            ArgumentNullException.ThrowIfNull(memberInfo);
            if (memberInfo.GetCustomAttribute<T>() != null)
                return true;
            return GetRuntimeAttribute<T>(memberInfo) != null;
        }

        /// <summary>
        /// Get all attributes from a member, including runtime-added attributes.
        /// </summary>
        /// <param name="memberInfo">Target member</param>
        /// <returns>All attributes</returns>
        public static IEnumerable<Attribute> GetAllAttributes(MemberInfo memberInfo)
        {
            ArgumentNullException.ThrowIfNull(memberInfo);
            var compiletimeAttributes = memberInfo.GetCustomAttributes();
            if (_runtimeAttributes.TryGetValue(memberInfo, out var runtimeAttributes))
            {
                return compiletimeAttributes.Concat(runtimeAttributes);
            }
            return compiletimeAttributes;
        }

        private static void AddRuntimeAttribute(MemberInfo memberInfo, Attribute attribute)
        {
            var attributes = _runtimeAttributes.GetOrAdd(memberInfo, _ => []);
            lock (attributes)
            {
                attributes.Add(attribute);
            }
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
}
