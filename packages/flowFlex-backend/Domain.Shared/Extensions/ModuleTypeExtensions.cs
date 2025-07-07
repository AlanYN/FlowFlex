using Item.Common.Lib.Attr;
using System;
using FlowFlex.Domain.Shared.Enums;
using FlowFlex.Domain.Shared.Attributes;
// using FlowFlex.Infrastructure.Attributes;

namespace FlowFlex.Domain.Shared.Extensions;

public static class ModuleTypeExtensions
{
    public static Type GetModuleType(int module)
    {
        var enumValue = AttributeHelper.GetAttribute<EnumValueAttribute>(typeof(ModuleType).GetField(module.ToString()));
        if (enumValue == null) return null;
        return enumValue.ResourceType;
    }
}
