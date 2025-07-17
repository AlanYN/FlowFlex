using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.User
{
    /// <summary>
    /// 刷新访问令牌请求DTO
    /// </summary>
    public class RefreshTokenRequestDto
    {
        /// <summary>
        /// 当前访问令牌
        /// </summary>
        [Required(ErrorMessage = "访问令牌不能为空")]
        public string AccessToken { get; set; } = string.Empty;
    }
} 