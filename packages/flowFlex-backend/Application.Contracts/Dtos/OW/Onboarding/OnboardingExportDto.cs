using Item.Excel.Lib;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// Onboarding export DTO
    /// </summary>
    public class OnboardingExportDto
    {
        /// <summary>
        /// Customer Name
        /// </summary>
        [ExcelColumn(Name = "Customer Name")]
        public string CustomerName { get; set; }

        /// <summary>
        /// Lead ID
        /// </summary>
        [ExcelColumn(Name = "Lead ID")]
        public string Id { get; set; }

        /// <summary>
        /// Contact Name
        /// </summary>
        [ExcelColumn(Name = "Contact Name")]
        public string ContactName { get; set; }

        /// <summary>
        /// Life Cycle Stage
        /// </summary>
        [ExcelColumn(Name = "Life Cycle Stage")]
        public string LifeCycleStage { get; set; }

        /// <summary>
        /// Workflow
        /// </summary>
        [ExcelColumn(Name = "Workflow")]
        public string WorkFlow { get; set; }

        /// <summary>
        /// Stage
        /// </summary>
        [ExcelColumn(Name = "Stage")]
        public string OnboardStage { get; set; }

        /// <summary>
        /// Priority
        /// </summary>
        [ExcelColumn(Name = "Priority")]
        public string Priority { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        [ExcelColumn(Name = "Status")]
        public string Status { get; set; }

        /// <summary>
        /// Start Date
        /// </summary>
        [ExcelColumn(Name = "Start Date")]
        public string StartDate { get; set; }

        /// <summary>
        /// End Date
        /// </summary>
        [ExcelColumn(Name = "End Date")]
        public string EndDate { get; set; }

        /// <summary>
        /// Updated By
        /// </summary>
        [ExcelColumn(Name = "Updated By")]
        public string UpdatedBy { get; set; }

        /// <summary>
        /// Update Time
        /// </summary>
        [ExcelColumn(Name = "Update Time")]
        public string UpdateTime { get; set; }
    }
}