using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.DealWarning
{
    /// <summary>
    /// Feedback type enumeration
    /// </summary>
    public enum DwFeedbackTypeEnum
    {
        /// <summary>
        /// Useful
        /// </summary>
        [Description("Useful")]
        Useful = 1,

        /// <summary>
        /// Irrelevant
        /// </summary>
        [Description("Irrelevant")]
        Irrelevant = 2,

        /// <summary>
        /// Adopted
        /// </summary>
        [Description("Adopted")]
        Adopted = 3,

        /// <summary>
        /// Rejected
        /// </summary>
        [Description("Rejected")]
        Rejected = 4
    }
}
