using System;
using FlowFlex.Domain.Shared.Enums;

namespace FlowFlex.Domain.Shared.Models.Invoice
{
    /// <summary>
    /// Invoice output DTO
    /// </summary>
    public class InvoiceDto
    {
        /// <summary>
        /// Invoice ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Invoice number
        /// </summary>
        public string InvoiceNo { get; set; }

        /// <summary>
        /// Deal ID
        /// </summary>
        public long DealId { get; set; }

        /// <summary>
        /// Invoice amount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Invoice status
        /// </summary>
        public InvoiceStatus Status { get; set; }

        /// <summary>
        /// Invoice date
        /// </summary>
        public DateTimeOffset InvoiceDate { get; set; }

        /// <summary>
        /// Remarks
        /// </summary>
        public string Remarks { get; set; }

        /// <summary>
        /// Invoice type
        /// </summary>
        public InvoiceTypeEnum InvoiceType { get; set; }
    }
}
