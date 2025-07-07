using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis
{
    public enum CustomerAccountsGroupEnum
    {
        [Description("Select One")]
        SelectOne = 0,
        [Description("Brocker")]
        Brocker = 1,
        [Description("SB")]
        SB = 2,
        [Description("SBH")]
        SBH = 3,
        [Description("SBFM")]
        SBFM = 4,
        [Description("Cofrieght")]
        Cofrieght = 5,
        [Description("UNIS")]
        UNIS = 6,
        [Description("Master Inv")]
        MasterInv = 7,
        [Description("Trinium")]
        Trinium = 8,
        [Description("3PL")]
        T3PL = 9,
        [Description("Others")]
        Others = 10,
    }
}
