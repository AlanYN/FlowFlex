#nullable enable

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using FlowFlex.Domain.Shared.Attr;

namespace FlowFlex.Domain.Shared.Models.DynamicData
{
    public class ValueContainer
    {
        DateTimeOffset? _dateTimeValue;

        public long BusinessId { get; set; }

        public long FieldId { get; set; }

        public long ValueId { get; set; }

        [Value] public long? LongValue { get; set; }

        [Value] public int? IntValue { get; set; }

        [Value] public double? DoubleValue { get; set; }

        [Value]
        public DateTimeOffset? DateTimeValue
        {
            get => _dateTimeValue.HasValue ? _dateTimeValue.Value.ToUniversalTime() : null;
            set => _dateTimeValue = value;
        }

        [Value] public bool? BoolValue { get; set; }

        [Value] public string? TextValue { get; set; }

        [Value] public string? VarcharValue { get; set; }

        [Value] public string? Varchar100Value { get; set; }

        [Value] public string? Varchar500Value { get; set; }

        [Value] public JToken? JsonValue { get; set; }
    }
}
