using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using FlowFlex.Domain.Shared.JsonConverters;

namespace FlowFlex.Application.Contracts.Dtos.OW.AdobeSign
{
    /// <summary>
    /// Output DTO for a single Adobe Sign tenant → account mapping record.
    /// </summary>
    public class AdobeSignTenantConfigOutputDto
    {
        [JsonConverter(typeof(LongToStringConverter))]
        public long Id { get; set; }

        /// <summary>The AppCode being configured (e.g. "item-wfe")</summary>
        public string ConfiguredAppCode { get; set; }

        /// <summary>The Adobe Sign account key assigned to this AppCode (e.g. "Item", "Unisco")</summary>
        public string AccountKey { get; set; }

        public DateTimeOffset CreateDate { get; set; }
        public DateTimeOffset ModifyDate { get; set; }
    }

    /// <summary>
    /// Input DTO for creating or updating an AppCode → account mapping.
    /// </summary>
    public class UpsertAdobeSignTenantConfigInputDto
    {
        /// <summary>
        /// The AppCode to configure (must match the X-App-Code header value, e.g. "item-wfe")
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string AppCode { get; set; }

        /// <summary>
        /// The Adobe Sign account key to assign. Must match a key in appsettings.json AdobeSign:Accounts.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string AccountKey { get; set; }
    }
}
