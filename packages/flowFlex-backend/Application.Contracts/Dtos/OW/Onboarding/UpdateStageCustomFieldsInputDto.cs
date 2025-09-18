using System;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// Update stage custom fields input DTO
    /// </summary>
    public class UpdateStageCustomFieldsInputDto
    {
        /// <summary>
        /// Stage ID to update
        /// </summary>
        [Required]
        public long StageId { get; set; }

        /// <summary>
        /// Custom estimated days (overrides Stage configuration)
        /// </summary>
        [Range(0.1, 365, ErrorMessage = "Custom estimated days must be between 0.1 and 365")]
        public decimal? CustomEstimatedDays { get; set; }

        /// <summary>
        /// Custom end time (overrides calculated EndTime)
        /// </summary>
        public DateTimeOffset? CustomEndTime { get; set; }

        /// <summary>
        /// Notes for the update
        /// </summary>
        [MaxLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string? Notes { get; set; }
    }
}
