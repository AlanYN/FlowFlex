using Newtonsoft.Json.Linq;
using System;

namespace FlowFlex.Domain.Shared.Models.DynamicData;

public class BusinessDataDto
{
    public long Id { get; set; }

    public int ModuleId { get; set; }

    public DateTimeOffset CreateDate { get; set; }

    public DateTimeOffset ModifyDate { get; set; }

    public string CreateBy { get; set; }

    public string ModifyBy { get; set; }

    public long CreateUserId { get; set; }

    public long ModifyUserId { get; set; }

    public JObject InternalData { get; set; }

    public DynamicDataObject DynamicData { get; set; }
}
