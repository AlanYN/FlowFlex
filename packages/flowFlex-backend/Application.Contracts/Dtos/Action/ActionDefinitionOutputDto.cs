namespace FlowFlex.Application.Contracts.Dtos.Action
{
    /// <summary>
    /// Action definition output DTO
    /// </summary>
    public class ActionDefinitionOutputDto
    {
        /// <summary>
        /// ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Action name
        /// </summary>
        public string ActionName { get; set; } = string.Empty;

        /// <summary>
        /// Action type (Python, HttpApi, SendEmail)
        /// </summary>
        public string ActionType { get; set; } = string.Empty;

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Action configuration (JSON format)
        /// </summary>
        public Dictionary<string, object>? ActionConfig { get; set; }

        /// <summary>
        /// Is enabled
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Create date
        /// </summary>
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// Modify date
        /// </summary>
        public DateTime ModifyDate { get; set; }

        /// <summary>
        /// Created by
        /// </summary>
        public string CreateBy { get; set; } = string.Empty;

        /// <summary>
        /// Modified by
        /// </summary>
        public string ModifyBy { get; set; } = string.Empty;

        /// <summary>
        /// Tenant ID
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// App code
        /// </summary>
        public string AppCode { get; set; } = string.Empty;
    }
} 