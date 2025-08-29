using Item.Internal.ChangeLog;
using System.ComponentModel.DataAnnotations.Schema;
using FlowFlex.Domain.Abstracts;
using FlowFlex.Domain.Shared.Attr;
using SqlSugar;

namespace FlowFlex.Domain.Entities.Base;

/// <summary>
/// ����ʵ����
/// </summary>
public abstract class AbstractEntityBase : IdEntityBase, ITenantFilter, IAppFilter
{
    [IgnoreDisplay]
    [ChangeLogColumn(IsIgnore = true)]
    [SugarColumn(ColumnName = "tenant_id", IsOnlyIgnoreUpdate = true)]
    public virtual string TenantId { get; set; } = string.Empty;

    [IgnoreDisplay]
    [ChangeLogColumn(IsIgnore = true)]
    [SugarColumn(ColumnName = "app_code", IsOnlyIgnoreUpdate = true)]
    public virtual string AppCode { get; set; } = string.Empty;
}
