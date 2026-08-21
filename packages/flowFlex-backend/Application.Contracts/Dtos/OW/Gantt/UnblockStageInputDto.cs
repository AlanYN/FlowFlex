using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Gantt
{
    /// <summary>
    /// Input DTO for unblocking a stage (Req 7.3)
    /// </summary>
    public class UnblockStageInputDto
    {
        /// <summary>
        /// ID of the stage to unblock
        /// </summary>
        [Required]
        public long StageId { get; set; }

        /// <summary>
        /// Optional notes describing how the blocker was resolved
        /// </summary>
        [StringLength(1000)]
        public string? ResolutionNotes { get; set; }
    }
}
