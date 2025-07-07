using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Questionnaire Section Entity - Questionnaire Grouping
    /// </summary>
    [SugarTable("ff_questionnaire_section")]
    public class QuestionnaireSection : EntityBaseCreateInfo
    {
        /// <summary>
        /// Questionnaire ID
        /// </summary>
        
        [SugarColumn(ColumnName = "questionnaire_id")]
        public long QuestionnaireId { get; set; }

        /// <summary>
        /// Section Name
        /// </summary>
        
        [StringLength(100)]
        public string Title { get; set; }

        /// <summary>
        /// Section Description
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// Sort Order
        /// </summary>
        [SugarColumn(ColumnName = "order_index")]
        public int Order { get; set; } = 0;

        /// <summary>
        /// Is Active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Section Icon
        /// </summary>
        [StringLength(50)]
        public string Icon { get; set; }

        /// <summary>
        /// Section Color
        /// </summary>
        [StringLength(20)]
        public string Color { get; set; }

        /// <summary>
        /// Is Collapsible
        /// </summary>
        public bool IsCollapsible { get; set; } = true;

        /// <summary>
        /// Default Is Expanded
        /// </summary>
        public bool IsExpanded { get; set; } = true;
    }
}
