using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums
{
    /// <summary>
    /// Payment Type
    /// </summary>
    public enum PaymentTypeEnum
    {
        /// <summary>
        /// Payment by card
        /// </summary>
        [Description("Card payment")]
        CardPayment = 1,

        // /// <summary>
        // /// Mobile Payment
        // /// </summary>
        // [Description("Mobile Payment")]
        // MobilePayment = 2,

        /// <summary>
        /// Bank Transfer
        /// </summary>
        [Description("Bank Transfer")]
        BankTransfer = 3,

        // /// <summary>
        // /// Cash Payment
        // /// </summary>
        // [Description("Cash Payment")]
        // CashPayment = 4
    }
}
