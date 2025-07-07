using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Domain.Shared.Enums
{
    /// <summary>
    /// Payment Sub Type
    /// </summary>
    public enum PaymentSubTypeEnum
    {
        /// <summary>
        /// Credit Card
        /// </summary>
        [Display(Name = "Credit Card", Description = "CardPayment")]
        CreditCard = 101,

        /// <summary>
        /// Debit Card
        /// </summary>
        [Display(Name = "Debit Card", Description = "CardPayment")]
        DebitCard = 102,

        // /// <summary>
        // /// Apple Pay
        // /// </summary>
        // [Display(Name = "Apple Pay", Description = "MobilePayment")]
        // ApplePay = 201,
        //
        // /// <summary>
        // /// Google Pay
        // /// </summary>
        // [Display(Name = "Google Pay", Description = "MobilePayment")]
        // GooglePay = 202,
        //
        // /// <summary>
        // /// Alipay
        // /// </summary>
        // [Display(Name = "Alipay", Description = "MobilePayment")]
        // Alipay = 203,
        //
        // /// <summary>
        // /// WeChat Pay
        // /// </summary>
        // [Display(Name = "WeChat Pay", Description = "MobilePayment")]
        // WechatPay = 204,

        /// <summary>
        /// ACH Payment
        /// </summary>
        [Description("ACH")]
        [Display(Name = "ACH", Description = "BankTransfer")]
        Ach = 301,

        /// <summary>
        /// Wire Transfer
        /// </summary>
        [Display(Name = "Wire", Description = "BankTransfer")]
        Wire = 302,

        /// <summary>
        /// Check Payment
        /// </summary>
        [Display(Name = "Check", Description = "BankTransfer")]
        Check = 303,

        // /// <summary>
        // /// Check Payment
        // /// </summary>
        // [Display(Name = "Cash Payment", Description = "CashPayment")]
        // CashPayment = 401
    }
}
