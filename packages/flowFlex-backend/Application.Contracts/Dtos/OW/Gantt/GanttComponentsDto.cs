namespace FlowFlex.Application.Contracts.Dtos.OW.Gantt
{
    /// <summary>
    /// Component completion statistics for a Gantt stage item (Req 6.4)
    /// </summary>
    public class GanttComponentsDto
    {
        /// <summary>
        /// Total number of checklist tasks configured on the stage
        /// </summary>
        public int ChecklistsTotal { get; set; }

        /// <summary>
        /// Number of checklist tasks that have been completed
        /// </summary>
        public int ChecklistsCompleted { get; set; }

        /// <summary>
        /// Total number of questionnaires attached to the stage
        /// </summary>
        public int QuestionnairesTotal { get; set; }

        /// <summary>
        /// Number of questionnaires that have been submitted
        /// </summary>
        public int QuestionnairesSubmitted { get; set; }

        /// <summary>
        /// Total number of required fields on the stage
        /// </summary>
        public int FieldsTotal { get; set; }

        /// <summary>
        /// Number of required fields that have been filled
        /// </summary>
        public int FieldsFilled { get; set; }

        /// <summary>
        /// Number of files that have been uploaded for the stage
        /// </summary>
        public int FilesUploaded { get; set; }
    }
}
