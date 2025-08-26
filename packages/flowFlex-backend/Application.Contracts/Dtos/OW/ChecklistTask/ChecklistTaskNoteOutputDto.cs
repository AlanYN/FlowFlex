using System;

namespace FlowFlex.Application.Contracts.Dtos.OW.ChecklistTask
{
    /// <summary>
    /// Checklist task note output DTO
    /// </summary>
    public class ChecklistTaskNoteOutputDto
    {
        /// <summary>
        /// Note ID
        /// </summary>
        public long Id { get; set; }

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
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Note type
        /// </summary>
        public string NoteType { get; set; } = "General";

        /// <summary>
        /// Priority level
        /// </summary>
        public string Priority { get; set; } = "Medium";

        /// <summary>
        /// Created by user ID
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Created by user name
        /// </summary>
        public string CreatedByName { get; set; } = string.Empty;

        /// <summary>
        /// Created time
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// Last modified by user ID
        /// </summary>
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// Last modified by user name
        /// </summary>
        public string? ModifiedByName { get; set; }

        /// <summary>
        /// Last modified time
        /// </summary>
        public DateTimeOffset? ModifiedAt { get; set; }

        /// <summary>
        /// Whether the note is deleted
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Whether the note is pinned
        /// </summary>
        public bool IsPinned { get; set; } = false;
    }

    /// <summary>
    /// Checklist task notes summary DTO
    /// </summary>
    public class ChecklistTaskNotesSummaryDto
    {
        /// <summary>
        /// Task ID
        /// </summary>
        public long TaskId { get; set; }

        /// <summary>
        /// Total notes count
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Pinned notes count
        /// </summary>
        public int PinnedCount { get; set; }

        /// <summary>
        /// Latest note
        /// </summary>
        public ChecklistTaskNoteOutputDto? LatestNote { get; set; }

        /// <summary>
        /// All notes
        /// </summary>
        public List<ChecklistTaskNoteOutputDto> Notes { get; set; } = new List<ChecklistTaskNoteOutputDto>();
    }
}