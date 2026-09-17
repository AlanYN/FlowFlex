using System;
using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.AdobeSign
{
    /// <summary>
    /// Output DTO representing an Adobe Sign agreement and its current state (OW-731)
    /// </summary>
    public class AdobeSignAgreementOutputDto
    {
        /// <summary>
        /// Internal WFE agreement record ID (ff_adobe_sign_agreement.id)
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Adobe Sign Agreement ID (returned by Adobe Sign API)
        /// </summary>
        public string AgreementId { get; set; }

        /// <summary>
        /// Associated Onboarding ID
        /// </summary>
        public long OnboardingId { get; set; }

        /// <summary>
        /// Associated Stage ID
        /// </summary>
        public long StageId { get; set; }

        /// <summary>
        /// Source PDF file ID (the original unsigned file)
        /// </summary>
        public long SourceFileId { get; set; }

        /// <summary>
        /// Signed PDF file ID (filled after all parties have signed)
        /// </summary>
        public long? SignedFileId { get; set; }

        /// <summary>
        /// Audit Trail PDF file ID (filled after completion)
        /// </summary>
        public long? AuditTrailFileId { get; set; }

        /// <summary>
        /// Current agreement status: Awaiting / Completed / Declined / Expired / Cancelled
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// List of signers with their individual statuses and timestamps
        /// </summary>
        public List<AdobeSignerDto> Signers { get; set; }

        /// <summary>
        /// Signing order: Sequential or Parallel
        /// </summary>
        public string SigningOrder { get; set; }

        /// <summary>
        /// Expiration in days from creation date
        /// </summary>
        public int ExpirationDays { get; set; }

        /// <summary>
        /// Message sent to signers
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Display name of the person who initiated the request
        /// </summary>
        public string RequestedByName { get; set; }

        /// <summary>
        /// Timestamp when the signing request was created
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// Timestamp when all signers completed signing (null if still in progress)
        /// </summary>
        public DateTimeOffset? CompletedDate { get; set; }
    }
}
