using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    public enum SentDataStatusEnum
    {
        /// <summary>
        /// Not sent
        /// </summary>
        [Description("Not Sent")]
        NotSent = 1,

        /// <summary>
        /// Sent
        /// </summary>
        [Description("Sent")]
        Sent = 2
    }
}
