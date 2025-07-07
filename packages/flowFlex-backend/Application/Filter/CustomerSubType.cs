namespace FlowFlex.Application.Filter;

/// <summary>
/// Business sub-items
/// </summary>
public enum CustomerSubType
{
    Default = 0,
    /// <summary>
    /// Customer address
    /// </summary>
    Address = 1,
    /// <summary>
    /// Customer document
    /// </summary>
    CustomerDocument = 2,
    /// <summary>
    /// Bank payment detail for credit
    /// </summary>
    BankPaymentDetailCredit = 3,
    /// <summary>
    /// Bank payment detail for debit
    /// </summary>
    BankPaymentDetailDebit = 4,
    /// <summary>
    /// Customer brand
    /// </summary>
    CustomerBrand = 5,
    /// <summary>
    /// Customer retailer
    /// </summary>
    CustomerRetailer = 6,
    /// <summary>
    /// Customer supplier
    /// </summary>
    CustomerSupplier = 7,
    /// <summary>
    /// Customer title
    /// </summary>
    CustomerTitle = 8,
    /// <summary>
    /// Credit application
    /// </summary>
    CreditApplication = 9,
    /// <summary>
    /// Credit application detail
    /// </summary>
    CreditAppDetail = 10,
    /// <summary>
    /// Customer program attribute
    /// </summary>
    CustomerProgramAttribute = 11,
    /// <summary>
    /// Refer personal guarantee
    /// </summary>
    ReferPersonalGuarantee = 12,
    /// <summary>
    /// Customer account holder
    /// </summary>
    CustomerAccountHolder = 13,
    /// <summary>
    /// Customer contract information
    /// </summary>
    CustomerContractInfo = 14,
    /// <summary>
    /// Facility customer relationships
    /// </summary>
    FacilityCustomerRelationships = 15,
    /// <summary>
    /// Create account
    /// </summary>
    CreateAccount = 16
}
