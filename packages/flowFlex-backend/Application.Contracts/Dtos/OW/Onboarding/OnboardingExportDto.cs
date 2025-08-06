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
        [ExcelColumn(Name = "Lead ID")]
        public string Id { get; set; }

        /// <summary>
        /// Company/Contact Name
        /// </summary>
        [ExcelColumn(Name = "Company/Contact Name")]
        public string CompanyName { get; set; }

        /// <summary>
        /// Life Cycle Stage
        /// </summary>
        [ExcelColumn(Name = "Life Cycle Stage")]
        public string LifeCycleStage { get; set; }

        /// <summary>
        /// Onboard Workflow
        /// </summary>
        [ExcelColumn(Name = "Onboard Workflow")]
        public string WorkFlow { get; set; }

        /// <summary>
        /// Onboard Stage
        /// </summary>
        [ExcelColumn(Name = "Onboard Stage")]
        public string OnboardStage { get; set; }

        /// <summary>
        /// Priority
        /// </summary>
        [ExcelColumn(Name = "Priority")]
        public string Priority { get; set; }

        /// <summary>
        /// Timeline
        /// </summary>
        [ExcelColumn(Name = "Timeline")]
        public string Timeline { get; set; }

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