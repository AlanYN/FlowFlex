using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Shared.Enums.Action;

namespace FlowFlex.Application.Contracts.Dtos.Action
{
    /// <summary>
    /// DTO for updating existing action definition
    /// </summary>
    public class UpdateActionDefinitionDto
    {
        /// <summary>
        /// Action name
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Action description
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// Action type
        /// </summary>
        [Required]
        public ActionTypeEnum ActionType { get; set; }

        /// <summary>
        /// Action configuration (JSON format)
        /// </summary>
        [Required]
        public string ActionConfig { get; set; }

        /// <summary>
        /// Whether the action is enabled
        /// </summary>
        public bool IsEnabled { get; set; }
    }
}