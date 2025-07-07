using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.DealWarning
{
    /// <summary>
    /// Warning level enumeration
    /// </summary>
    public enum DwWarningLevelEnum
    {
        /// <summary>
        /// Low risk
        /// </summary>
        [Description("Low Risk")]
        Low = 1,

        /// <summary>
        /// Medium risk
        /// </summary>
        [Description("Medium Risk")]
        Medium = 2,

        /// <summary>
        /// High risk
        /// </summary>
        [Description("High Risk")]
        High = 3
    }
}
