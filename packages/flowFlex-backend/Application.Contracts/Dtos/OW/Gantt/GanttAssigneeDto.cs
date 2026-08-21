namespace FlowFlex.Application.Contracts.Dtos.OW.Gantt
{
    /// <summary>
    /// Assignee information returned in Gantt stage items
    /// </summary>
    public class GanttAssigneeDto
    {
        /// <summary>
        /// Display name of the assignee
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Email address of the assignee
        /// </summary>
        public string Email { get; set; }
    }
}
