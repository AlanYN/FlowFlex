namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Inbound data DTO
    /// </summary>
    public class InboundDataDto
    {
        public string ExternalEntityType { get; set; }
        public string ExternalRecordId { get; set; }
        public Dictionary<string, object> Data { get; set; }
        public List<AttachmentDto> Attachments { get; set; }
    }

    /// <summary>
    /// Outbound sync request DTO
    /// </summary>
    public class OutboundSyncRequestDto
    {
        public long WfeRecordId { get; set; }
        public string EntityType { get; set; }
        public Dictionary<string, object> Data { get; set; }
        public List<AttachmentDto> Attachments { get; set; }
    }

    /// <summary>
    /// Sync result DTO
    /// </summary>
    public class SyncResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ExternalRecordId { get; set; }
        public long? WfeRecordId { get; set; }
        public DateTime SyncTime { get; set; }
        public string ErrorDetails { get; set; }
    }

    /// <summary>
    /// Batch sync result DTO
    /// </summary>
    public class BatchSyncResultDto
    {
        public int TotalRecords { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<SyncResultDto> Results { get; set; }
        public DateTime SyncTime { get; set; }
    }

    /// <summary>
    /// Attachment DTO
    /// </summary>
    public class AttachmentDto
    {
        public string FileName { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; }
        public string FileUrl { get; set; }
        public byte[] FileContent { get; set; }
    }

    /// <summary>
    /// Integration sync log DTO
    /// </summary>
    public class IntegrationSyncLogDto
    {
        public long Id { get; set; }
        public long IntegrationId { get; set; }
        public string Direction { get; set; }
        public string EntityType { get; set; }
        public string ExternalRecordId { get; set; }
        public long? WfeRecordId { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime SyncTime { get; set; }
        public int? DurationMs { get; set; }
    }

    /// <summary>
    /// Sync log filter DTO
    /// </summary>
    public class SyncLogFilterDto
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public long? IntegrationId { get; set; }
        public string Direction { get; set; }
        public string Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string SortField { get; set; } = "SyncTime";
        public string SortDirection { get; set; } = "desc";
    }

    /// <summary>
    /// Sync log query request
    /// </summary>
    public class SyncLogQueryRequest
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public long? IntegrationId { get; set; }
        public string Direction { get; set; }
        public string Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string SortField { get; set; } = "SyncTime";
        public string SortDirection { get; set; } = "desc";
    }
}
