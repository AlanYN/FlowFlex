namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// Input DTO for rolling back a completed stage
    /// </summary>
    public class RollBackStageInput
    {
        /// <summary>
        /// Optional reason for rolling back the stage (recorded in operation log)
        /// </summary>
        public string? Reason { get; set; }
    }
}
