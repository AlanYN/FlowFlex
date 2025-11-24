using Domain.Shared.Enums;

namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Integration sync log input DTO
    /// </summary>
    public class IntegrationSyncLogInputDto
    {
        /// <summary>
        /// Integration ID
        /// </summary>
        public long IntegrationId { get; set; }

        /// <summary>
        /// Entity mapping ID
        /// </summary>
        public long? EntityMappingId { get; set; }

        /// <summary>
        /// Sync direction
        /// </summary>
        public SyncDirection SyncDirection { get; set; }

        /// <summary>
        /// External system entity ID
        /// </summary>
        public string? ExternalId { get; set; }

        /// <summary>
        /// WFE internal entity ID
        /// </summary>
        public string? InternalId { get; set; }

        /// <summary>
        /// Sync status
        /// </summary>
        public SyncStatus SyncStatus { get; set; }

        /// <summary>
        /// Number of records processed
        /// </summary>
        public int RecordsProcessed { get; set; }

        /// <summary>
        /// Error message if failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Sync duration in milliseconds
        /// </summary>
        public int DurationMs { get; set; }
    }
}

