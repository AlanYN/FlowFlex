using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.QuestionnaireSection
{
    /// <summary>
    /// 问卷部分输入DTO
    /// </summary>
    public class QuestionnaireSectionInputDto
    {
        /// <summary>
        /// 问卷ID
        /// </summary>

        public long QuestionnaireId { get; set; }

        /// <summary>
        /// 部分名称
        /// </summary>

        [StringLength(200)]
        public string SectionName { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        [StringLength(200)]
        public string Title { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        [StringLength(1000)]
        public string Description { get; set; }

        /// <summary>
        /// 排序索引
        /// </summary>
        public int OrderIndex { get; set; }

        /// <summary>
        /// 是否必需
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 图标
        /// </summary>
        [StringLength(100)]
        public string Icon { get; set; }

        /// <summary>
        /// 颜色
        /// </summary>
        [StringLength(20)]
        public string Color { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; set; }
    }
}