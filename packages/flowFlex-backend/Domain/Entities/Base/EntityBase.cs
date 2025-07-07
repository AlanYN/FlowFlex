using FlowFlex.Domain.Abstracts;
using FlowFlex.Domain.Shared.Attr;
using SqlSugar;

namespace FlowFlex.Domain.Entities.Base
{
    public abstract class EntityBase : AbstractEntityBase, IValidFilter
    {
        [IgnoreDisplay]
        [SugarColumn(ColumnName = "is_valid")]
        public virtual bool IsValid { get; set; } = true;
    }
}
