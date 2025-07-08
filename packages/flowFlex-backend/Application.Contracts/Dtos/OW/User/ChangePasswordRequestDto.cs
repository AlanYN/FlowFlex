using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.User
{
    /// <summary>
    /// 修改密码请求DTO
    /// </summary>
    public class ChangePasswordRequestDto
    {
        /// <summary>
        /// 新密码
        /// </summary>
        [Required(ErrorMessage = "新密码不能为空")]
        [MinLength(6, ErrorMessage = "密码长度不能少于6位")]
        public string NewPassword { get; set; }
    }
}
