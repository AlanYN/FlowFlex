using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.ChecklistTask
{
    /// <summary>
    /// Checklist task note input DTO
    /// </summary>
    public class ChecklistTaskNoteInputDto
    {
        /// <summary>
        /// Task ID
        /// </summary>
        public long TaskId { get; set; }

        /// <summary>
        /// Onboarding ID
        /// </summary>
        public long OnboardingId { get; set; }

        /// <summary>
        /// Note content
        /// </summary>
        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Note type (optional)
        /// </summary>
        [StringLength(50)]
        public string NoteType { get; set; } = "General";

        /// <summary>
        /// Priority level (High, Medium, Low)
        /// </summary>
        [StringLength(20)]
        public string Priority { get; set; } = "Medium";
    }

    /// <summary>
    /// Update checklist task note input DTO
    /// </summary>
    public class ChecklistTaskNoteUpdateDto
    {
        /// <summary>
        /// Note ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Note content
        /// </summary>
        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Note type (optional)
        /// </summary>
        [StringLength(50)]
        public string NoteType { get; set; } = "General";

        /// <summary>
        /// Priority level (High, Medium, Low)
        /// </summary>
        [StringLength(20)]
        public string Priority { get; set; } = "Medium";
    }
}