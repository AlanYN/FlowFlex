using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SqlSugar;
using Newtonsoft.Json;
using FlowFlex.Domain.Shared.JsonConverters;

namespace FlowFlex.Domain.Entities
{
    /// <summary>
    /// OW基础实体
    /// </summary>
    public abstract class OwEntityBase
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [SugarColumn(IsPrimaryKey = true)]
        [JsonConverter(typeof(LongToStringConverter))]
        public long Id { get; set; }

        /// <summary>
        /// 租户ID
        /// </summary>
        [MaxLength(32)]
        [SugarColumn(ColumnName = "tenant_id")]
        public string TenantId { get; set; } = "default";

        /// <summary>
        /// 是否有效
        /// </summary>
        [SugarColumn(ColumnName = "is_valid")]
        public bool IsValid { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        [SugarColumn(ColumnName = "create_date")]
        public DateTimeOffset CreateDate { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// 修改时间
        /// </summary>
        [SugarColumn(ColumnName = "modify_date")]
        public DateTimeOffset ModifyDate { get; set; } = DateTimeOffset.Now;

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
