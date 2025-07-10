namespace FlowFlex.Application.Contracts.Dtos.OW.Event
{
    /// <summary>
    /// Event output DTO
    /// </summary>
    public class EventOutputDto
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Event ID (业务层生成的唯一标识)
        /// </summary>
        public string EventId { get; set; } = string.Empty;

        /// <summary>
        /// Event type
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Event version
        /// </summary>
        public string EventVersion { get; set; } = "1.0";

        /// <summary>
        /// Event timestamp
        /// </summary>
        public DateTimeOffset EventTimestamp { get; set; }

        /// <summary>
        /// Aggregate ID
        /// </summary>
        public long AggregateId { get; set; }

        /// <summary>
        /// Aggregate type
        /// </summary>
        public string AggregateType { get; set; } = string.Empty;

        /// <summary>
        /// Event source
        /// </summary>
        public string EventSource { get; set; } = string.Empty;

        /// <summary>
        /// Event data (JSON)
        /// </summary>
        public string EventData { get; set; } = "{}";

        /// <summary>
        /// Event metadata (JSON)
        /// </summary>
        public string EventMetadata { get; set; } = "{}";

        /// <summary>
        /// Event description
        /// </summary>
        public string EventDescription { get; set; } = string.Empty;

        /// <summary>
        /// Event status
        /// </summary>
        public string EventStatus { get; set; } = "Published";

        /// <summary>
        /// Process count
        /// </summary>
        public int ProcessCount { get; set; } = 0;

        /// <summary>
        /// Last processed time
        /// </summary>
        public DateTimeOffset? LastProcessedAt { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Related entity ID
        /// </summary>
        public long? RelatedEntityId { get; set; }

        /// <summary>
        /// Related entity type
        /// </summary>
        public string RelatedEntityType { get; set; } = string.Empty;

        /// <summary>
        /// Event tags (JSON array)
        /// </summary>
        public string EventTags { get; set; } = "[]";

        /// <summary>
        /// Requires retry
        /// </summary>
        public bool RequiresRetry { get; set; } = false;

        /// <summary>
        /// Next retry time
        /// </summary>
        public DateTimeOffset? NextRetryAt { get; set; }

        /// <summary>
        /// Maximum retry count
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>
        /// Tenant ID
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Create time
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// Created by
        /// </summary>
        public string CreateBy { get; set; } = string.Empty;

        /// <summary>
        /// Modify time
        /// </summary>
        public DateTimeOffset ModifyDate { get; set; }

        /// <summary>
        /// Modified by
        /// </summary>
        public string ModifyBy { get; set; } = string.Empty;
    }
} 