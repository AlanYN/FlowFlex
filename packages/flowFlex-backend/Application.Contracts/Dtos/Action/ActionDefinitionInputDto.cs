using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace FlowFlex.Application.Contracts.Dtos.Action
{
    /// <summary>
    /// Action definition create/update input DTO
    /// </summary>
    public class ActionDefinitionInputDto
    {
        /// <summary>
        /// Action name
        /// </summary>
        [Required(ErrorMessage = "Action name is required")]
        [StringLength(100, ErrorMessage = "Action name length cannot exceed 100 characters")]
        [DisplayName("Action Name")]
        public string ActionName { get; set; } = string.Empty;

        /// <summary>
        /// Action type (Python, HttpApi, SendEmail)
        /// </summary>
        [Required(ErrorMessage = "Action type is required")]
        [StringLength(50, ErrorMessage = "Action type length cannot exceed 50 characters")]
        [DisplayName("Action Type")]
        public string ActionType { get; set; } = string.Empty;

        /// <summary>
        /// Description
        /// </summary>
        [StringLength(500, ErrorMessage = "Description length cannot exceed 500 characters")]
        [DisplayName("Description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Action configuration (JSON format)
        /// </summary>
        [DisplayName("Action Configuration")]
        public Dictionary<string, object>? ActionConfig { get; set; }

        /// <summary>
        /// Is enabled
        /// </summary>
        [DisplayName("Is Enabled")]
        public bool IsEnabled { get; set; } = true;
    }
} 