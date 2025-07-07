#nullable enable

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using FlowFlex.Domain.Shared.Enums.DynamicData;
using FlowFlex.Domain.Shared.Extensions;

namespace FlowFlex.Domain.Shared.Models.DynamicData;

public interface IDynamicValue
{
    #region Field

    long FieldId { get; set; }

    string FieldName { get; set; }

    DataType DataType { get; set; }
    #endregion

    #region Value
    long? LongValue { get; set; }

    int? IntValue { get; set; }

    double? DoubleValue { get; set; }

    DateTimeOffset? DateTimeValue { get; set; }

    bool? BoolValue { get; set; }

    string? TextValue { get; set; }

    string? VarcharValue { get; set; }

    string? Varchar100Value { get; set; }

    string? Varchar500Value { get; set; }

    JToken? JsonValue { get; set; }
    #endregion

    int ModuleId { get; set; }

    long BusinessId { get; set; }
}
