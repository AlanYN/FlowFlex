namespace FlowFlex.Domain.Shared.Enums.Unis;

public enum CustomerTagEnum
{
    /// <summary>
    /// Title
    /// </summary>
    Title = 1 << 0,

    /// <summary>
    /// Retailer
    /// </summary>
    Retailer = 1 << 1,

    /// <summary>
    /// Supplier
    /// </summary>
    Supplier = 1 << 2,

    /// <summary>
    /// Brand
    /// </summary>
    Brand = 1 << 3,

    /// <summary>
    /// Customer
    /// </summary>
    Customer = 1 << 4,

    /// <summary>
    /// Manufacturer
    /// </summary>
    Manufacturer = 1 << 5,
}
