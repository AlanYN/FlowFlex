using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.UserSignature
{
    /// <summary>
    /// 创建用户签名输入DTO
    /// </summary>
    public class CreateSignatureInputDto
    {
        /// <summary>
        /// Base64 编码的签名图片数据（PNG 格式）
        /// </summary>
        [Required]
        public string ImageBase64 { get; set; }
    }
}
