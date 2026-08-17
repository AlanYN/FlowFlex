using System;
using Newtonsoft.Json;
using FlowFlex.Domain.Shared.JsonConverters;

namespace FlowFlex.Application.Contracts.Dtos.OW.UserSignature
{
    /// <summary>
    /// 用户签名输出DTO
    /// </summary>
    public class ProfileSignatureOutputDto
    {
        /// <summary>
        /// 签名ID
        /// </summary>
        [JsonConverter(typeof(LongToStringConverter))]
        public long Id { get; set; }

        /// <summary>
        /// Base64 编码的签名图片数据
        /// </summary>
        public string ImageBase64 { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTimeOffset CreatedDate { get; set; }
    }
}
