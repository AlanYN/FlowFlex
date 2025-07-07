using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    /// <summary>
    /// Preferences Language options for company/contact details
    /// </summary>
    public enum PreferencesLanguageEnum
    {
        [Description("Chinese")]
        Chinese = 1,

        [Description("German")]
        German = 2,

        [Description("English")]
        English = 3,

        [Description("French")]
        French = 4,

        [Description("Italian")]
        Italian = 5,

        [Description("Spanish")]
        Spanish = 6,

        [Description("Korean")]
        Korean = 7,

        [Description("Japanese")]
        Japanese = 8
    }
}
