using Item.Common.Lib.Encrypt;
using Item.Internal.ChangeLog;
using SqlSugar;
using System;
using System.Collections.Generic;
using FlowFlex.Domain.Shared.Attr;
using FlowFlex.Domain.Shared.Enums;

namespace FlowFlex.Domain.Shared.Models.Payment;

/// <summary>
/// Payment List Model
/// </summary>
public class PaymentListModel
{
    /// <summary>
    /// Payment Method ID
    /// </summary>
    public long MethodId { get; set; }

    /// <summary>
    /// Payment ID
    /// </summary>
    public long PaymentId { get; set; }

    /// <summary>
    /// Payment Type
    /// </summary>
    public PaymentTypeEnum PaymentType { get; set; }

    /// <summary>
    /// Payment Sub Type
    /// </summary>
    public PaymentSubTypeEnum SubType { get; set; }

    /// <summary>
    /// Account Holder Name
    /// </summary>
    public string AccountHolderName { get; set; }

    /// <summary>
    /// Account Number
    /// </summary>
    [Encrypt]
    public string AccountNumber { get; set; }

    /// <summary>
    /// Card Number (for card payments)
    /// </summary>
    [Encrypt]
    public string CardNumber { get; set; }


    /// <summary>
    /// Card Holder Name
    /// </summary>
    public string CardHolderName { get; set; }

    /// <summary>
    /// Routing Number
    /// </summary>
    [Encrypt]
    public string RoutingNumber { get; set; }

    /// <summary>
    /// Expiry Date (for card payments)
    /// </summary>
    public DateTimeOffset? ExpiryDate { get; set; }


    /// <summary>
    /// Swift Code (for wire payments)
    /// </summary>
    public string SwiftCode { get; set; }

    /// <summary>
    /// Branch Number (for wire payments)
    /// </summary>
    public string BranchNumber { get; set; }

    /// <summary>
    /// Address Line 1
    /// </summary>
    public string Address1 { get; set; }

    /// <summary>
    /// City
    /// </summary>
    public string City { get; set; }

    /// <summary>
    /// State
    /// </summary>
    public string State { get; set; }

    /// <summary>
    /// Country
    /// </summary>
    public string Country { get; set; }

    /// <summary>
    /// Is Default
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// CVV
    /// </summary>
    [Encrypt]
    public string Cvv { get; set; }


    /// <summary>
    /// Zip Code
    /// </summary>
    public string ZipCode { get; set; }


    /// <summary>
    /// Address Line 2
    /// </summary>
    public string Address2 { get; set; }


    /// <summary>
    /// Street Address
    /// </summary>
    public string StreetAddress { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    public string CreateBy { get; set; }

    /// <summary>
    /// 修改人
    /// </summary>
    public string ModifyBy { get; set; }

    /// <summary>
    /// 创建人Id
    /// </summary>
    public long CreateUserId { get; set; }

    /// <summary>
    /// 修改人Id
    /// </summary>
    public long ModifyUserId { get; set; }

    public bool IsValid { get; set; }
}
