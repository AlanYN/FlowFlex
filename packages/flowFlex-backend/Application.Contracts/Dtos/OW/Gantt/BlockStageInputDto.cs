using System;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Gantt
{
    /// <summary>
    /// Input DTO for blocking a stage (Req 7.1)
    /// </summary>
    public class BlockStageInputDto
    {
        /// <summary>
        /// ID of the stage to block
        /// </summary>
        [Required]
        public long StageId { get; set; }

        /// <summary>
        /// Reason why this stage is being blocked (required, max 500 characters)
        /// </summary>
        [Required]
        [StringLength(500, MinimumLength = 1)]
        public string BlockerReason { get; set; }

        /// <summary>
        /// Expected date by which the blocker will be resolved (optional)
        /// </summary>
        public DateTimeOffset? ExpectedResolutionDate { get; set; }
    }
}
