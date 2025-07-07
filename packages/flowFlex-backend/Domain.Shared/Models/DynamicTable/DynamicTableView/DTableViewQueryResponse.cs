using System;
using System.Collections.Generic;

using FlowFlex.Domain.Shared.Enums;

namespace FlowFlex.Domain.Shared.Models;

public class DTableViewQueryResponse
{
    public long Id { get; set; }

    public string Name { get; set; }

    public ViewShareTypeEnum ShareType { get; set; }

    public int ModuleType { get; set; }

    public bool IsDefault { get; set; }

    public List<DTableColumnConfig> Columns { get; set; } = [];

    public List<DTableFilterConfig> Filters { get; set; } = [];

    public string CreateBy { get; set; }

    public DateTimeOffset CreateDate { get; set; }

    public string ModifyBy { get; set; }

    public DateTimeOffset ModifyDate { get; set; }

    public long CreateUserId { get; set; }

    public long ModifyUserId { get; set; }

    public bool IsEdit { get; set; }
}
