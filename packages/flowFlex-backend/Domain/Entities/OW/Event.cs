using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Event Entity - 通用事件存储表
    /// </summary>
    [Table("ff_events")]
    [SugarTable("ff_events")]
    public class Event : EntityBaseCreateInfo
    {
        /// <summary>
        /// 事件ID (业务层生成的唯一标识)
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "event_id")]
        public string EventId { get; set; } = string.Empty;

        /// <summary>
        /// 事件类型 (OnboardingStageCompleted, WorkflowCreated, etc.)
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "event_type")]
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// 事件版本
        /// </summary>
        [StringLength(20)]
        [SugarColumn(ColumnName = "event_version")]
        public string EventVersion { get; set; } = "1.0";

        /// <summary>
        /// 事件时间戳
        /// </summary>
        [SugarColumn(ColumnName = "event_timestamp")]
        public DateTimeOffset EventTimestamp { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// 聚合根ID (OnboardingId, WorkflowId, etc.)
        /// </summary>
        [SugarColumn(ColumnName = "aggregate_id")]
        public long AggregateId { get; set; }

        /// <summary>
        /// 聚合根类型 (Onboarding, Workflow, etc.)
        /// </summary>
        [StringLength(50)]
        [SugarColumn(ColumnName = "aggregate_type")]
        public string AggregateType { get; set; } = string.Empty;

        /// <summary>
        /// 事件来源 (CustomerPortal, AdminPanel, API, etc.)
        /// </summary>
        [StringLength(50)]
        [SugarColumn(ColumnName = "event_source")]
        public string EventSource { get; set; } = string.Empty;

        /// <summary>
        /// 事件数据 (JSON格式)
        /// </summary>
        [SugarColumn(ColumnName = "event_data", ColumnDataType = "jsonb", IsJson = true)]
        public string EventData { get; set; } = "{}";

        /// <summary>
        /// 事件元数据 (JSON格式，包含路由标签、优先级等)
        /// </summary>
        [SugarColumn(ColumnName = "event_metadata", ColumnDataType = "jsonb", IsJson = true)]
        public string EventMetadata { get; set; } = "{}";

        /// <summary>
        /// 事件描述
        /// </summary>
        [StringLength(500)]
        [SugarColumn(ColumnName = "event_description")]
        public string EventDescription { get; set; } = string.Empty;

        /// <summary>
        /// 事件状态 (Published, Failed, Processed, etc.)
        /// </summary>
        [StringLength(20)]
        [SugarColumn(ColumnName = "event_status")]
        public string EventStatus { get; set; } = "Published";

        /// <summary>
        /// 处理次数
        /// </summary>
        [SugarColumn(ColumnName = "process_count")]
        public int ProcessCount { get; set; } = 0;

        /// <summary>
        /// 最后处理时间
        /// </summary>
        [SugarColumn(ColumnName = "last_processed_at")]
        public DateTimeOffset? LastProcessedAt { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        [StringLength(2000)]
        [SugarColumn(ColumnName = "error_message")]
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// 关联的业务实体ID (可选，用于快速查询)
        /// </summary>
        [SugarColumn(ColumnName = "related_entity_id")]
        public long? RelatedEntityId { get; set; }

        /// <summary>
        /// 关联的业务实体类型 (可选)
        /// </summary>
        [StringLength(50)]
        [SugarColumn(ColumnName = "related_entity_type")]
        public string RelatedEntityType { get; set; } = string.Empty;

        /// <summary>
        /// 事件标签 (JSON数组，用于分类和过滤)
        /// </summary>
        [SugarColumn(ColumnName = "event_tags", ColumnDataType = "jsonb", IsJson = true)]
        public string EventTags { get; set; } = "[]";

        /// <summary>
        /// 是否需要重试
        /// </summary>
        [SugarColumn(ColumnName = "requires_retry")]
        public bool RequiresRetry { get; set; } = false;

        /// <summary>
        /// 下次重试时间
        /// </summary>
        [SugarColumn(ColumnName = "next_retry_at")]
        public DateTimeOffset? NextRetryAt { get; set; }

        /// <summary>
        /// 最大重试次数
        /// </summary>
        [SugarColumn(ColumnName = "max_retry_count")]
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>
        /// 应用代码 (用于多应用隔离)
        /// </summary>
        [StringLength(50)]
        [SugarColumn(ColumnName = "app_code")]
        public string AppCode { get; set; } = "default";
    }
}