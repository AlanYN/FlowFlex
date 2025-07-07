using System;

namespace FlowFlex.Domain.Shared.Models.DynamicData;

public interface IBusinessTable
{
    public long Id { get; set; }

    DateTimeOffset CreateDate { get; set; }

    DateTimeOffset ModifyDate { get; set; }

    string CreateBy { get; set; }

    string ModifyBy { get; set; }

    long CreateUserId { get; set; }

    long ModifyUserId { get; set; }

    string TenantId { get; set; }

    bool IsValid { get; set; }
}
