using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.Action
{
    /// <summary>
    /// DTO for creating new action definition
    /// </summary>
    public class CreateActionDefinitionDto
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
        /// Action type (PythonScript, HttpApi, Email)
        /// </summary>
        [Required]
        public string ActionType { get; set; }

        /// <summary>
        /// Action configuration (JSON format)
        /// </summary>
        [Required]
        public string ActionConfig { get; set; }

        /// <summary>
        /// Whether the action is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }
} 