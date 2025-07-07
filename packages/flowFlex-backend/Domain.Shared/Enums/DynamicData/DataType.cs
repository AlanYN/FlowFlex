#nullable enable

using Item.Common.Lib.Attr;
using System;
using System.Reflection;
using FlowFlex.Domain.Shared.Attr;
using FlowFlex.Domain.Shared.DynamicConverter;
using BooleanConverter = FlowFlex.Domain.Shared.DynamicConverter.BooleanConverter;
using DateTimeConverter = FlowFlex.Domain.Shared.DynamicConverter.DateTimeConverter;

namespace FlowFlex.Domain.Shared.Enums.DynamicData;

/// <summary>
/// Data type
/// </summary>
public enum DataType
{
    /// <summary>
    /// unknown
    /// </summary>
    [IgnoreEnumField]
    Unknown = 0,

    /// <summary>
    /// Phone
    /// </summary>
    [EnumValue("Phone",
        Description = DbFieldName.Varchar100Value,
        ResourceType = typeof(ShortTextConverter))]
    Phone = 3,

    /// <summary>
    /// Email
    /// </summary>
    [EnumValue("Email",
        Description = DbFieldName.Varchar100Value,
        ResourceType = typeof(ShortTextConverter))]
    Email = 4,

    /// <summary>
    /// Single line text
    /// </summary>
    [EnumValue("Single line text",
        Description = DbFieldName.Varchar500Value,
        ResourceType = typeof(SingleTextConverter))]
    SingleLineText = 11,

    /// <summary>
    /// Multiline text
    /// </summary>
    [EnumValue("Multiline text",
        Description = DbFieldName.TextValue,
        ResourceType = typeof(MultilineTextConverter))]
    MultilineText = 12,

    /// <summary>
    /// Number
    /// </summary>
    [EnumValue("Number",
        Description = DbFieldName.DoubleValue,
        ResourceType = typeof(NumberConverter))]
    Number = 13,

    /// <summary>
    /// List
    /// </summary>
    [EnumValue("String list",
        Description = DbFieldName.StringListValue,
        ResourceType = typeof(StringListConverter))]
    StringList = 15,

    /// <summary>
    /// DropDown
    /// </summary>
    [EnumValue("Dropdown",
        Description = DbFieldName.LongValue,
        ResourceType = typeof(LongConverter))]
    DropDown = 5,

    /// <summary>
    /// Bool
    /// </summary>
    [EnumValue("Check box",
        Description = DbFieldName.BoolValue,
        ResourceType = typeof(BooleanConverter))]
    Bool = 7,

    /// <summary>
    /// date time
    /// </summary>
    [EnumValue("Date time",
        Description = DbFieldName.DateTimeValue,
        ResourceType = typeof(DateTimeConverter))]
    DateTime = 10,

    [EnumValue("File",
        Description = DbFieldName.LongValue,
        ResourceType = typeof(FileConverter))]
    File = 16,

    [EnumValue("File list",
        Description = DbFieldName.StringListValue,
        ResourceType = typeof(FileListConverter))]
    FileList = 17,

    [Obsolete]
    [EnumValue("ID",
        Description = DbFieldName.LongValue,
        ResourceType = typeof(LongConverter))]
    ID = 18,

    [EnumValue("People",
        Description = DbFieldName.LongValue,
        ResourceType = typeof(LongConverter))]
    People = 19,

    [EnumValue("Connection",
        Description = DbFieldName.LongValue,
        ResourceType = typeof(LongConverter))]
    Connection = 20,

    [Obsolete]
    [EnumValue("Phone prefix id",
        Description = DbFieldName.LongValue,
        ResourceType = typeof(LongConverter))]
    PhonePrefixID = 21,

    [EnumValue("Image",
        Description = DbFieldName.LongValue,
        ResourceType = typeof(LongConverter))]
    Image = 22,

    [EnumValue("Time line",
        Description = DbFieldName.StringListValue,
        ResourceType = typeof(TimeLineConverter))]
    TimeLine = 23,
}

public class DbFieldName
{
    public const string TextValue = "text_value";
    public const string Varchar100Value = "varchar100_value";
    public const string Varchar500Value = "varchar500_value";
    public const string LongValue = "long_value";
    public const string IntValue = "int_value";
    public const string DateTimeValue = "date_time_value";
    public const string BoolValue = "bool_value";
    public const string StringListValue = "string_list_value";
    public const string DoubleValue = "double_value";
}


public static class FieldTypeExtensions
{
    public static string? GetDbField(this DataType dataType)
    {
        return dataType.GetType()!.GetField(dataType.ToString())!.GetCustomAttribute<EnumValueAttribute>()?.Description;
    }

    public static Type? GetResourceType(this DataType dataType)
    {
        return dataType.GetType().GetField(dataType.ToString())!.GetCustomAttribute<EnumValueAttribute>()?.ResourceType;
    }
}
