using Newtonsoft.Json;
using System;
using Item.Common.Lib.JsonConverts;

namespace FlowFlex.Domain.Shared.Models.Attachment
{
    public class CustomerDocumentPageResultModel
    {
        /// <summary>
        /// Primary key ID
        /// </summary>
        [JsonConverter(typeof(ValueToStringConverter))]
        public long Id { get; set; }
        /// <summary>
        /// Attachment ID
        /// </summary>
        [JsonConverter(typeof(ValueToStringConverter))]
        public long AttachmentId { get; set; }
        /// <summary>
        /// Attachment type
        /// </summary>
        public string FileType { get; set; }
        /// <summary>
        /// FileUploadType
        /// </summary>
        public string FileUploadType { get; set; }
        /// <summary>
        /// Create time
        /// </summary>
        public DateTimeOffset? CreateDate { get; set; }

        public string RealName { get; set; }

        /// <summary>
        /// FileSize
        /// </summary>
        public long FileSize { get; set; }
        /// <summary>
        /// File extension
        /// </summary>
        public string FileExt { get; set; }

        /// <summary>
        /// Storage type
        /// </summary>
        public int? StoreType { get; set; }

        /// <summary>
        /// Access path
        /// </summary>
        public string AccessUrl { get; set; }
    }
}
