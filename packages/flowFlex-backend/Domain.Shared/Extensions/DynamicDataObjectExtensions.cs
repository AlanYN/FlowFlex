using System.Collections.Generic;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.Domain.Shared.Enums;
using FlowFlex.Domain.Shared.Models.DynamicData;

namespace FlowFlex.Domain.Shared.Extensions;

public static class DynamicDataObjectExtensions
{
    public static string GetContactName(this DynamicDataObject model)
    {
        if (model == null)
            return string.Empty;
        var name = (model.GetValueOrDefault<string>(FieldName.FirstName) + " " + model.GetValueOrDefault<string>(FieldName.LastName)).Trim();
        if (string.IsNullOrEmpty(name))
            name = model.GetValueOrDefault<string>(FieldName.Email);

        return name;
    }

    public static string GetCompanyName(this DynamicDataObject model)
    {
        if (model == null)
            return string.Empty;
        return model.GetValueOrDefault<string>(FieldName.CompanyName);
    }

    public static string GetDealName(this DynamicDataObject model)
    {
        if (model == null)
            return string.Empty;
        return model.GetValueOrDefault<string>(FieldName.DealName);
    }

    public static string GetBusinessName(this DynamicDataObject model)
    {
        if (model == null)
            return string.Empty;

        if (model.ModuleId == ModuleType.Company)
            return model.GetCompanyName();
        else if (model.ModuleId == ModuleType.Contact)
            return model.GetContactName();
        else if (model.ModuleId == ModuleType.Deal)
            return model.GetDealName();

        return string.Empty;
    }

    public static Dictionary<string, object> ToDictionary(this DynamicDataObject model)
    {
        var dict = new Dictionary<string, object>();
        foreach (var item in model)
        {
            dict.Add(item.FieldName, item.Value);
        }
        return dict;
    }
}
