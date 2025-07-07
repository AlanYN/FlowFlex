namespace FlowFlex.Domain.Shared.Models.Customer.Detail
{
    /// <summary>
    /// Represents a message containing various types of notes related to a customer
    /// </summary>
    public class NoteMessage : DataMessageBase
    {
        /// <summary>
        /// Notes related to the invoice
        /// </summary>
        public string? InvoiceNotes { get; set; } = "";

        /// <summary>
        /// Notes for the driver or delivery personnel
        /// </summary>
        public string? DriverNotes { get; set; } = "";

        /// <summary>
        /// Internal notes for company use only
        /// </summary>
        public string? InternalNotes { get; set; } = "";
    }
}
