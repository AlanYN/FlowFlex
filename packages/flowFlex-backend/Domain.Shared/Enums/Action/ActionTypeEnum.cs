using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Action
{
    /// <summary>
    /// Action type enumeration
    /// </summary>
    public enum ActionTypeEnum
    {
        /// <summary>
        /// Python script execution using Judge0
        /// </summary>
        [Description("Python Script")]
        Python = 1,

        /// <summary>
        /// HTTP API call
        /// </summary>
        [Description("HTTP API Call")]
        HttpApi = 2,

        /// <summary>
        /// Email sending
        /// </summary>
        [Description("Email")]
        SendEmail = 3,

        /// <summary>
        /// System predefined actions
        /// </summary>
        [Description("System Action")]
        System = 4
    }
}