using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis
{
    public enum YearsOfExperienceEnum
    {
        [Description("Less than a year")]
        Zero = 1,

        [Description("1-2 years")]
        One = 2,

        [Description("2-5 years")]
        Two = 3,

        [Description("5-10 years")]
        Five = 4,

        [Description("10+ years")]
        Ten = 5,

        [Description("20+ years")]
        Twenty = 6
    }
}
