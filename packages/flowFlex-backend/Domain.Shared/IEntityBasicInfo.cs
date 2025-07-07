using Item.Internal.ChangeLog;
using SqlSugar;
using System;
using FlowFlex.Domain.Shared.Attr;

namespace FlowFlex.Domain.Shared;

public interface IEntityBasicInfo
{
    long Id { get; set; }

    string TenantId { get; set; }

    DateTimeOffset CreateDate { get; set; }

    DateTimeOffset ModifyDate { get; set; }

    string CreateBy { get; set; }

    string ModifyBy { get; set; }

    long CreateUserId { get; set; }

    long ModifyUserId { get; set; }
}
