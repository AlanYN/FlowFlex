namespace FlowFlex.Domain.Shared.Enums
{
    /// <summary>
    /// Invoice status
    /// </summary>
    public enum InvoiceStatus
    {
        /// <summary>
        /// Generated
        /// </summary>
        Generated = 1,

        /// <summary>
        /// Issued
        /// </summary>
        Issued = 2,

        /// <summary>
        /// Voided
        /// </summary>
        Voided = 3
    }

    public enum InvoiceTypeEnum
    {
        /// <summary>
        /// Contract
        /// </summary>
        Contact = 1,
    }
}
