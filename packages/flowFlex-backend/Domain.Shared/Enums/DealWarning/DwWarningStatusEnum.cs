using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.DealWarning
{
    /// <summary>
    /// Warning status enumeration
    /// </summary>
    public enum DwWarningStatusEnum
    {
        /// <summary>
        /// Unprocessed
        /// </summary>
        [Description("Unprocessed")]
        Unprocessed = 1,

        /// <summary>
        /// Processed
        /// </summary>
        [Description("Processed")]
        Processed = 2,

        /// <summary>
        /// Ignored
        /// </summary>
        [Description("Ignored")]
        Ignored = 3
    }
}
