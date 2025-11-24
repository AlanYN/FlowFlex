namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Inbound settings DTO
    /// </summary>
    public class InboundSettingsDto
    {
        public long Id { get; set; }
        public long IntegrationId { get; set; }
        public bool ReceiveMasterData { get; set; }
        public bool ReceiveFields { get; set; }
        public bool ReceiveAttachments { get; set; }
        public string AttachmentTypes { get; set; }
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
    /// Outbound settings DTO
    /// </summary>
    public class OutboundSettingsDto
    {
        public long Id { get; set; }
        public long IntegrationId { get; set; }
        public bool ShareMasterData { get; set; }
        public bool ShareFields { get; set; }
        public bool ShareAttachments { get; set; }
        public string AttachmentTypes { get; set; }
        public bool EnableRealTimeSync { get; set; }
        public string WebhookUrl { get; set; }
        public string WebhookSecret { get; set; }
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

