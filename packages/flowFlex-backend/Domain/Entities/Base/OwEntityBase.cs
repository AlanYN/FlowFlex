using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SqlSugar;
using Newtonsoft.Json;
using FlowFlex.Domain.Shared.JsonConverters;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Domain.Entities
{
    /// <summary>
    /// OW基础实体 - 使用雪花ID
    /// </summary>
    public abstract class OwEntityBase
    {
        /// <summary>
        /// 主键ID - 使用雪花ID
        /// </summary>
        [Key]
        [SugarColumn(IsPrimaryKey = true)]
        [JsonConverter(typeof(LongToStringConverter))]
        public long Id { get; set; }

        /// <summary>
        /// 初始化雪花ID
        /// </summary>
        public long InitNewId()
        {
            Id = SnowFlakeSingle.Instance.NextId();
            return Id;
        }

        /// <summary>
        /// 租户ID
        /// </summary>
        [MaxLength(32)]
        [SugarColumn(ColumnName = "tenant_id")]
        public string TenantId { get; set; } = "default";

        /// <summary>
        /// 应用代码
        /// </summary>
        [MaxLength(32)]
        [SugarColumn(ColumnName = "app_code")]
        public string AppCode { get; set; } = "default";

        /// <summary>
        /// 是否有效
        /// </summary>
        [SugarColumn(ColumnName = "is_valid")]
        public bool IsValid { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        [SugarColumn(ColumnName = "create_date")]
        public DateTimeOffset CreateDate { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// 修改时间
        /// </summary>
        [SugarColumn(ColumnName = "modify_date")]
        public DateTimeOffset ModifyDate { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// 创建�?
        /// </summary>
        [MaxLength(50)]
        [SugarColumn(ColumnName = "create_by")]
        public string CreateBy { get; set; } = "SYSTEM";

        /// <summary>
        /// 修改�?
        /// </summary>
        [MaxLength(50)]
        [SugarColumn(ColumnName = "modify_by")]
        public string ModifyBy { get; set; } = "SYSTEM";

        /// <summary>
        /// 创建人Id
        /// </summary>
        [SugarColumn(ColumnName = "create_user_id")]
        [JsonConverter(typeof(LongToStringConverter))]
        public long CreateUserId { get; set; } = 0;

        /// <summary>
        /// 修改人Id
        /// </summary>
        [SugarColumn(ColumnName = "modify_user_id")]
        [JsonConverter(typeof(LongToStringConverter))]
        public long ModifyUserId { get; set; } = 0;
    }
}
