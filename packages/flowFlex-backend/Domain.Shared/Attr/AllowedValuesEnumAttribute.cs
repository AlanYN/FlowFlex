using System;

namespace FlowFlex.Domain.Shared.Attr
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AllowedValuesEnumAttribute : Attribute
    {
        public Type EnumType { get; }

        public AllowedValuesEnumAttribute(Type enumType)
        {
            if (!enumType.IsEnum)
                throw new ArgumentException("The type must be an enum.", nameof(enumType));

            EnumType = enumType;
        }
    }
}
