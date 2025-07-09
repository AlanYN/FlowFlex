using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// 添加备注输入DTO
    /// </summary>
    public class AddNoteInputDto
    {
        /// <summary>
        /// 备注内容
        /// </summary>

        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 备注类型
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// 是否私有
        /// </summary>
        public bool IsPrivate { get; set; } = false;
    }
}