using Item.Internal.ChangeLog;
using System.ComponentModel.DataAnnotations.Schema;
using FlowFlex.Domain.Abstracts;
using FlowFlex.Domain.Shared.Attr;
using SqlSugar;

namespace FlowFlex.Domain.Entities.Base;

/// <summary>
/// 基础实体类
/// </summary>
public abstract class AbstractEntityBase : IdEntityBase, ITenantFilter
{
    [IgnoreDisplay]
    [ChangeLogColumn(IsIgnore = true)]
    [SugarColumn(ColumnName = "tenant_id", IsOnlyIgnoreUpdate = true)]
    public virtual string TenantId { get; set; } = string.Empty;
}
