using Domain.Shared.Enums;

namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Connection configuration DTO
    /// </summary>
    public class ConnectionConfigDto
    {
        /// <summary>
        /// Connection status
        /// </summary>
        public IntegrationStatus Status { get; set; }

        /// <summary>
        /// Last synchronization date
        /// </summary>
        public DateTimeOffset? LastSyncDate { get; set; }

        /// <summary>
        /// Error message if connection failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}

