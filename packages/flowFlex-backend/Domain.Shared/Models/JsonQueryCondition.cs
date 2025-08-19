namespace FlowFlex.Domain.Shared.Models
{
    /// <summary>
    /// JSON query condition for jsonb fields
    /// </summary>
    public class JsonQueryCondition
    {
        /// <summary>
        /// JSON path (e.g., "OnboardingId", "data.user.id")
        /// </summary>
        public string JsonPath { get; set; } = string.Empty;

        /// <summary>
        /// Operator (e.g., "=", "!=", ">", "<", "contains")
        /// </summary>
        public string Operator { get; set; } = "=";

        /// <summary>
        /// Value to compare
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }
}