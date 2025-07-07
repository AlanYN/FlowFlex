using Item.Excel.Lib;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// Onboarding export DTO
    /// </summary>
    public class OnboardingExportDto
    {
        /// <summary>
        /// Lead ID
        /// </summary>
        [ExcelColumn(Name = "ID")]
        public string Id { get; set; }

        /// <summary>
        /// Company Name
        /// </summary>
        [ExcelColumn(Name = "Company Name")]
        public string CompanyName { get; set; }

        /// <summary>
        /// Life Cycle Stage
        /// </summary>
        [ExcelColumn(Name = "Life Cycle Stage")]
        public string LifeCycleStage { get; set; }

        /// <summary>
        /// Onboard Stage
        /// </summary>
        [ExcelColumn(Name = "Onboard Stage")]
        public string OnboardStage { get; set; }

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

        /// <summary>
        /// Start Date
        /// </summary>
        [ExcelColumn(Name = "Start Date")]
        public string StartDate { get; set; }

        /// <summary>
        /// ETA (Estimated Time of Arrival)
        /// </summary>
        [ExcelColumn(Name = "ETA")]
        public string Eta { get; set; }

        /// <summary>
        /// Priority
        /// </summary>
        [ExcelColumn(Name = "Priority")]
        public string Priority { get; set; }

        /// <summary>
        /// Progress (percentage)
        /// </summary>
        [ExcelColumn(Name = "Progress")]
        public int Progress { get; set; }
    }
}