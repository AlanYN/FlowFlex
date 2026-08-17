using Newtonsoft.Json;
using FlowFlex.Domain.Shared.JsonConverters;

namespace FlowFlex.Application.Contracts.Dtos.OW.DocumentSigning
{
    /// <summary>
    /// 签署文件输出DTO
    /// </summary>
    public class SignDocumentOutputDto
    {
        /// <summary>
        /// 已签署文件的 ID
        /// </summary>
        [JsonConverter(typeof(LongToStringConverter))]
        public long SignedFileId { get; set; }

        /// <summary>
        /// 文件下载链接
        /// </summary>
        public string DownloadUrl { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 文件 SHA-256 哈希值（64位 hex）
        /// </summary>
        public string FileHash { get; set; }
    }
}
