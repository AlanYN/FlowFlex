using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.Action
{
    /// <summary>
    /// Action definition DTO for CRUD operations
    /// </summary>
    public class ActionDefinitionDto
    {
        /// <summary>
        /// Action definition ID
        /// </summary>
        public long Id { get; set; }

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

        /// <summary>
        /// Creation time
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last update time
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
} 