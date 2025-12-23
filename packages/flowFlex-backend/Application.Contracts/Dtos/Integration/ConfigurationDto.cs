using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Inbound settings DTO (detailed view for GetWithDetails)
    /// </summary>
    public class InboundSettingsDto
    {
        public long Id { get; set; }
        public long IntegrationId { get; set; }
        public long? ActionId { get; set; }
        public List<string> EntityTypes { get; set; } = new();
        public List<object> FieldMappings { get; set; } = new();
        public Dictionary<string, object> AttachmentSettings { get; set; } = new();
        public bool AutoSync { get; set; }
        public int SyncInterval { get; set; }
        public DateTimeOffset? LastSyncDate { get; set; }

        // Legacy fields (kept for backward compatibility)
        public bool ReceiveMasterData { get; set; }
        public bool ReceiveFields { get; set; }
        public bool ReceiveAttachments { get; set; }
        public string AttachmentTypes { get; set; } = string.Empty;
        public int? MaxAttachmentSize { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
    }

    /// <summary>
    /// Update inbound settings DTO
    /// </summary>
    public class UpdateInboundSettingsDto
    {
        public bool ReceiveMasterData { get; set; }
        public bool ReceiveFields { get; set; }
        public bool ReceiveAttachments { get; set; }
        public string AttachmentTypes { get; set; }
        public int? MaxAttachmentSize { get; set; }
    }

    /// <summary>
    /// Outbound settings DTO (detailed view for GetWithDetails)
    /// </summary>
    public class OutboundSettingsDto
    {
        public long Id { get; set; }
        public long IntegrationId { get; set; }
        public long? ActionId { get; set; }
        public List<string> EntityTypes { get; set; } = new();
        public List<object> FieldMappings { get; set; } = new();
        public Dictionary<string, object> AttachmentSettings { get; set; } = new();
        public int SyncMode { get; set; }
        public string? WebhookUrl { get; set; }

        // Legacy fields (kept for backward compatibility)
        public bool ShareMasterData { get; set; }
        public bool ShareFields { get; set; }
        public bool ShareAttachments { get; set; }
        public string AttachmentTypes { get; set; } = string.Empty;
        public bool EnableRealTimeSync { get; set; }
        public string WebhookSecret { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
    }

    /// <summary>
    /// Update outbound settings DTO
    /// </summary>
    public class UpdateOutboundSettingsDto
    {
        public bool ShareMasterData { get; set; }
        public bool ShareFields { get; set; }
        public bool ShareAttachments { get; set; }
        public string AttachmentTypes { get; set; }
        public bool EnableRealTimeSync { get; set; }
        public string WebhookUrl { get; set; }
        public string WebhookSecret { get; set; }
    }
}

