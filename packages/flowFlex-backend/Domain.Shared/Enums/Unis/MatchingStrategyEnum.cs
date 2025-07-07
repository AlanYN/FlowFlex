using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis
{
    public enum MatchingStrategyEnum
    {
        /// <summary>
        /// Exact match
        /// </summary>
        [Description("Exact Match")]
        ExactMatch = 1,

        /// <summary>
        /// Prefix matching
        /// </summary>
        [Description("Prefix Matching")]
        PrefixMatching = 2,

        /// <summary>
        /// Suffix matching
        /// </summary>
        [Description("Suffix Matching")]
        SuffixMatching = 3,

        /// <summary>
        /// Substring matching
        /// </summary>
        [Description("Substring Matching")]
        SubstringMatching = 4
    }
}
