namespace FlowFlex.Application.Contracts.Dtos.OW.Event
{
    /// <summary>
    /// Event query request
    /// </summary>
    public class EventQueryRequest
    {
        /// <summary>
        /// Page index (starts from 1)
        /// </summary>
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Sort field
        /// </summary>
        public string? SortField { get; set; } = "EventTimestamp";

        /// <summary>
        /// Sort direction (asc/desc)
        /// </summary>
        public string? SortDirection { get; set; } = "desc";

        /// <summary>
        /// Event type filter
        /// </summary>
        public string? EventType { get; set; }

        /// <summary>
        /// Event status filter
        /// </summary>
        public string? EventStatus { get; set; }

        /// <summary>
        /// Event source filter
        /// </summary>
        public string? EventSource { get; set; }

        /// <summary>
        /// Aggregate ID filter
        /// </summary>
        public long? AggregateId { get; set; }

        /// <summary>
        /// Aggregate type filter
        /// </summary>
        public string? AggregateType { get; set; }

        /// <summary>
        /// Related entity ID filter
        /// </summary>
        public long? RelatedEntityId { get; set; }

        /// <summary>
        /// Related entity type filter
        /// </summary>
        public string? RelatedEntityType { get; set; }

        /// <summary>
        /// Event ID filter
        /// </summary>
        public string? EventId { get; set; }

        /// <summary>
        /// Start date filter
        /// </summary>
        public DateTimeOffset? StartDate { get; set; }

        /// <summary>
        /// End date filter
        /// </summary>
        public DateTimeOffset? EndDate { get; set; }

        /// <summary>
        /// Days filter (recent N days)
        /// </summary>
        public int? Days { get; set; }

        /// <summary>
        /// Tags filter (JSON array contains)
        /// </summary>
        public List<string>? Tags { get; set; }

        /// <summary>
        /// Requires retry filter
        /// </summary>
        public bool? RequiresRetry { get; set; }

        /// <summary>
        /// Search keyword (searches in description, event data, etc.)
        /// </summary>
        public string? SearchKeyword { get; set; }

        /// <summary>
        /// Tenant ID filter
        /// </summary>
        public string? TenantId { get; set; }
    }
}