using Domain.Shared.Enums;

namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Integration sync log output DTO
    /// </summary>
    public class IntegrationSyncLogOutputDto
    {
        /// <summary>
        /// Sync log ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Integration ID
        /// </summary>
        public long IntegrationId { get; set; }

        /// <summary>
        /// Sync direction
        /// </summary>
        public string SyncDirection { get; set; } = string.Empty;

        /// <summary>
        /// Entity type
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// Entity ID
        /// </summary>
        public string EntityId { get; set; } = string.Empty;

        /// <summary>
        /// Sync status
        /// </summary>
        public SyncStatus Status { get; set; }

        /// <summary>
        /// Error message if failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Request payload
        /// </summary>
        public string? RequestPayload { get; set; }

        /// <summary>
        /// Response payload
        /// </summary>
        public string? ResponsePayload { get; set; }

        /// <summary>
        /// Sync duration in milliseconds
        /// </summary>
        public long DurationMs { get; set; }

        /// <summary>
        /// Create date (sync time)
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }
    }
}

