
namespace FlowFlex.Domain.Shared.Models.Customer.Detail;

/// <summary>
/// Represents the AR (Accounts Receivable) balance information for a customer
/// </summary>
public class ArBalanceMessage
{
    /// <summary>
    /// The unique identifier of the vendor
    /// </summary>
    public string VendorID { get; set; }

    /// <summary>
    /// The account identifier associated with the customer
    /// </summary>
    public string AccountID { get; set; }

    /// <summary>
    /// The CRM customer code
    /// </summary>
    public string CrmCustomerCode { get; set; }

    /// <summary>
    /// The primary identifier in the CRM system
    /// </summary>
    public string CrmPrimaryID { get; set; }

    /// <summary>
    /// The current AR balance for the customer
    /// </summary>
    public decimal Balance { get; set; }
}
