using System;

namespace FlowFlex.Domain.Shared.Models.Customer.Detail
{
    /// <summary>
    /// Represents credit application head message containing various credit-related information.
    /// </summary>
    public class CreditAppHeadMessage : DataMessageBase
    {
        /// <summary>
        /// Gets or sets the overall credit score.
        /// </summary>
        public decimal? CreditScore { get; set; }

        /// <summary>
        /// Gets or sets the Experian commercial score.
        /// </summary>
        public decimal? ExperianCommercialScore { get; set; }

        /// <summary>
        /// Gets or sets the Experian Financial Stability Risk (FSR) score.
        /// </summary>
        public decimal? ExperianFsrScore { get; set; }

        /// <summary>
        /// Gets or sets the Experian collection score.
        /// </summary>
        public decimal? ExperianCollectionScore { get; set; }

        /// <summary>
        /// Gets or sets the date and time of the last Experian operation.
        /// </summary>
        public DateTimeOffset? ExperianLastOperateDate { get; set; }

        /// <summary>
        /// Gets or sets the CreditSafe credit score.
        /// </summary>
        public decimal? CreditSafeCreditScore { get; set; }

        /// <summary>
        /// Gets or sets the CreditSafe rating.
        /// </summary>
        public string CreditSafeRating { get; set; }

        /// <summary>
        /// Gets or sets the CreditSafe description.
        /// </summary>
        public string CreditSafeDescription { get; set; }

        /// <summary>
        /// Gets or sets the date and time of the CreditSafe operation.
        /// </summary>
        public DateTimeOffset? CreditSafeOperateDate { get; set; }

        /// <summary>
        /// Gets or sets the past CreditSafe credit score.
        /// </summary>
        public decimal? CreditSafePastCreditScore { get; set; }

        /// <summary>
        /// Gets or sets the past CreditSafe rating.
        /// </summary>
        public string CreditSafePastRating { get; set; }

        /// <summary>
        /// Gets or sets the past CreditSafe description.
        /// </summary>
        public string CreditSafePastDescription { get; set; }

        /// <summary>
        /// Gets or sets the CreditSafe company ID.
        /// </summary>
        public string CreditSafeCompayId { get; set; }

        /// <summary>
        /// Gets or sets the Experian Business Identification Number (BIN).
        /// </summary>
        public string ExperianBin { get; set; }
    }
}
