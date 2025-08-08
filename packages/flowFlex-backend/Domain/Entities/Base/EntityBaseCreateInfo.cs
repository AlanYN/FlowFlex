using Item.Common.Lib.Attr;
using Item.Internal.ChangeLog;
using System.ComponentModel;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Attr;
using FlowFlex.Domain.Abstracts;
using SqlSugar;
using Newtonsoft.Json;
using FlowFlex.Domain.Shared.JsonConverters;

namespace FlowFlex.Domain.Entities.Base
{
    public abstract class EntityBaseCreateInfo : EntityBase, IEntityBasicInfo, ISoftDeletable
    {
        /// <summary>
        /// 创建时间
        /// </summary>
        [ChangeLogColumn(IsIgnore = true)]
        [IgnoreDisplay]
        [SugarColumn(ColumnName = "create_date", IsOnlyIgnoreUpdate = true)]
        public virtual DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        [ChangeLogColumn(IsIgnore = true)]
        [IgnoreDisplay]
        [SugarColumn(ColumnName = "modify_date")]
        public virtual DateTimeOffset ModifyDate { get; set; }

        /// <summary>
        /// 创建者
        /// </summary>
        [ChangeLogColumn(IsIgnore = true)]
        [IgnoreDisplay]
        [SugarColumn(ColumnName = "create_by", IsOnlyIgnoreUpdate = true)]
        public virtual string CreateBy { get; set; }

        /// <summary>
        /// 修改者
        /// </summary>
        [ChangeLogColumn(IsIgnore = true)]
        [IgnoreDisplay]
        [SugarColumn(ColumnName = "modify_by")]
        public virtual string ModifyBy { get; set; }

        /// <summary>
        /// 创建者Id
        /// </summary>
        [ChangeLogColumn(IsIgnore = true)]
        [IgnoreDisplay]
        [SugarColumn(ColumnName = "create_user_id", IsOnlyIgnoreUpdate = true)]
        [JsonConverter(typeof(LongToStringConverter))]
        public virtual long CreateUserId { get; set; }

        /// <summary>
        /// 修改者Id
        /// </summary>
        [ChangeLogColumn(IsIgnore = true)]
        [IgnoreDisplay]
        [SugarColumn(ColumnName = "modify_user_id")]
        [JsonConverter(typeof(LongToStringConverter))]
        public virtual long ModifyUserId { get; set; }
    }
}
