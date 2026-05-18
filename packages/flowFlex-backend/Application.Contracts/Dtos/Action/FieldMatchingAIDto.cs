namespace FlowFlex.Application.Contracts.Dtos.Action
{
    /// <summary>
    /// Context for a single field that needs AI matching
    /// </summary>
    public class FieldMatchContext
    {
        /// <summary>
        /// The target API field name (e.g. "sales_rep_id")
        /// </summary>
        public string ApiField { get; set; } = string.Empty;

        /// <summary>
        /// The raw user input value to match (e.g. "张三")
        /// </summary>
        public string RawValue { get; set; } = string.Empty;

        /// <summary>
        /// Available options to match against
        /// </summary>
        public List<OptionItem> Options { get; set; } = new();
    }

    /// <summary>
    /// Result of AI field matching for a single field
    /// </summary>
    public class FieldMatchResult
    {
        /// <summary>
        /// The target API field name
        /// </summary>
        public string ApiField { get; set; } = string.Empty;

        /// <summary>
        /// The original raw input value
        /// </summary>
        public string RawValue { get; set; } = string.Empty;

        /// <summary>
        /// The matched option value (null if no match found)
        /// </summary>
        public string? MatchedValue { get; set; }

        /// <summary>
        /// Confidence score between 0 and 1
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Brief reasoning for the match decision
        /// </summary>
        public string Reasoning { get; set; } = string.Empty;

        /// <summary>
        /// Match source: "exact_matched", "ai_matched", or "ai_unmatched"
        /// </summary>
        public string Source { get; set; } = "ai_unmatched";
    }

    /// <summary>
    /// AI match metadata stored in execution record for audit/display purposes
    /// </summary>
    public class AiMatchMetadata
    {
        /// <summary>
        /// The original user input text
        /// </summary>
        public string OriginalInput { get; set; } = string.Empty;

        /// <summary>
        /// The resolved value after matching
        /// </summary>
        public string? MatchedValue { get; set; }

        /// <summary>
        /// Confidence score between 0 and 1
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Brief reasoning for the match
        /// </summary>
        public string Reasoning { get; set; } = string.Empty;

        /// <summary>
        /// Match source: "exact_matched", "ai_matched", or "ai_unmatched"
        /// </summary>
        public string Source { get; set; } = "ai_unmatched";
    }
}
