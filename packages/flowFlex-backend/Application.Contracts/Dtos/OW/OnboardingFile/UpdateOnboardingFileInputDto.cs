using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.OnboardingFile
{
    /// <summary>
    /// 更新Onboarding文件信息输入DTO
    /// </summary>
    public class UpdateOnboardingFileInputDto
    {
        /// <summary>
        /// 文件分类 (Document, Image, Certificate, Other)
        /// </summary>
        [StringLength(50)]
        public string Category { get; set; }

        /// <summary>
        /// 文件描述
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// 是否为必需文件
        /// </summary>
        public bool? IsRequired { get; set; }

        /// <summary>
        /// 文件标签 (用于分组或搜索)
        /// </summary>
        [StringLength(200)]
        public string Tags { get; set; }

        /// <summary>
        /// 文件状态 (Active, Archived)
        /// </summary>
        [StringLength(20)]
        public string Status { get; set; }
    }
}