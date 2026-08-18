using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FlowFlex.Application.Contracts.Dtos.OW.DocumentSigning
{
    /// <summary>
    /// 签署文件输入DTO（multipart/form-data）
    /// </summary>
    public class SignDocumentInputDto
    {
        /// <summary>
        /// 已签署的 PDF 文件
        /// </summary>
        [Required]
        public IFormFile File { get; set; }

        /// <summary>
        /// 签署人姓名
        /// </summary>
        [Required]
        public string SignerName { get; set; }

        /// <summary>
        /// 签署时间（ISO 8601 UTC）
        /// </summary>
        [Required]
        public string SignedAt { get; set; }
    }
}
