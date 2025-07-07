using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    /// <summary>
    /// Preferences Communication options for company/contact details
    /// </summary>
    public enum PreferencesCommunicationEnum
    {
        [Description("Email")]
        Email = 1,

        [Description("Call")]
        Call = 2,

        [Description("Offline")]
        Offline = 3
    }
}
