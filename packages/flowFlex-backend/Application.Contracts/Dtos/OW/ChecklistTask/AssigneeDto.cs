namespace FlowFlex.Application.Contracts.Dtos.OW.ChecklistTask
{
    /// <summary>
    /// Assignee DTO for ChecklistTask
    /// </summary>
    public class AssigneeDto
    {
        /// <summary>
        /// Assignee User ID
        /// </summary>
        public long? UserId { get; set; }

        /// <summary>
        /// Assignee Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Assignee Email
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Assigned Team
        /// </summary>
        public string Team { get; set; }

        /// <summary>
        /// Assignment Type (User/Team)
        /// </summary>
        public string Type { get; set; } = "User";

        /// <summary>
        /// Assignment Source (Manual/Auto/Template)
        /// </summary>
        public string Source { get; set; } = "Manual";
    }
}