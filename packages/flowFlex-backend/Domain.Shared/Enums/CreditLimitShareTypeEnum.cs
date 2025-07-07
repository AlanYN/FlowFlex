using Item.Common.Lib.Attr;

namespace FlowFlex.Domain.Shared.Enums
{
    public enum CreditLimitShareTypeEnum
    {
        /// <summary>
        /// Neither balance or credit limit share with main
        /// </summary>
        [EnumValue("Neither balance or credit limit share with main")]
        NeitherShare = 0,

        /// <summary>
        /// Only balance share with main
        /// </summary>
        [EnumValue("Only balance share with main")]
        OnlyBalance = 1,

        /// <summary>
        /// Both balance and credit limit share with main
        /// </summary>
        [EnumValue("Both balance and credit limit share with main")]
        BalanceAndCreditLimit = 2
    }
}
