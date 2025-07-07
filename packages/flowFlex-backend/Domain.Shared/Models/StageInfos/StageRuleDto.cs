namespace FlowFlex.Domain.Shared.Models.StageInfos
{
    /// <summary>
    /// Stage rule DTO
    /// </summary>
    public class StageRuleDto
    {
        /// <summary>
        /// Primary key ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Pipeline ID
        /// </summary>
        public long PipelineId { get; set; }

        /// <summary>
        /// Whether all tasks must be completed
        /// </summary>
        public bool AllTaskFinish { get; set; }

        /// <summary>
        /// Whether stage skipping is not allowed
        /// </summary>
        public bool NoAllowSkipStage { get; set; }

        /// <summary>
        /// Whether rollback is not allowed
        /// </summary>
        public bool NoAllowBack { get; set; }
    }
}
