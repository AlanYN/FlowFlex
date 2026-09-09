using System;
using SqlSugar;
using FlowFlex.Domain.Entities.Base;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Adobe Sign Agreement Entity - Tracks e-signature requests sent via Adobe Sign (OW-731).
    /// Each record represents one signing request for a specific PDF file in a Stage.
    /// </summary>
    [SugarTable("ff_adobe_sign_agreement")]
    public class AdobeSignAgreement : EntityBaseCreateInfo
    {
        /// <summary>
        /// Adobe Sign Agreement ID returned by Adobe Sign API
        /// </summary>
        [SugarColumn(ColumnName = "agreement_id", Length = 200)]
        public string AgreementId { get; set; }

        /// <summary>
        /// Associated Onboarding (Case) ID
        /// </summary>
        [SugarColumn(ColumnName = "onboarding_id")]
        public long OnboardingId { get; set; }

        /// <summary>
        /// Associated Stage ID
        /// </summary>
        [SugarColumn(ColumnName = "stage_id")]
        public long StageId { get; set; }

        /// <summary>
        /// Source file ID — the original PDF in ff_onboarding_file that was sent for signing
        /// </summary>
        [SugarColumn(ColumnName = "source_file_id")]
        public long SourceFileId { get; set; }

        /// <summary>
        /// Signed file ID — the completed signed PDF saved back to ff_onboarding_file (filled after completion)
        /// </summary>
        [SugarColumn(ColumnName = "signed_file_id", IsNullable = true)]
        public long? SignedFileId { get; set; }

        /// <summary>
        /// Audit Trail file ID — the Audit Trail PDF saved to ff_onboarding_file (filled after completion)
        /// </summary>
        [SugarColumn(ColumnName = "audit_trail_file_id", IsNullable = true)]
        public long? AuditTrailFileId { get; set; }

        /// <summary>
        /// Current agreement status: Awaiting / Completed / Declined / Expired / Cancelled
        /// </summary>
        [SugarColumn(ColumnName = "status", Length = 50)]
        public string Status { get; set; } = "Awaiting";

        /// <summary>
        /// Signers list with each signer's status and timestamps (JSONB).
        /// Format: [{"email":"...","name":"...","role":"Signer","order":1,"status":"Awaiting","signedAt":null}]
        /// </summary>
        [SugarColumn(ColumnName = "signers", ColumnDataType = "jsonb", IsJson = true)]
        public string Signers { get; set; }

        /// <summary>
        /// Signing order: Sequential (one by one) or Parallel (all at once)
        /// </summary>
        [SugarColumn(ColumnName = "signing_order", Length = 20)]
        public string SigningOrder { get; set; } = "Sequential";

        /// <summary>
        /// Number of days before the agreement expires
        /// </summary>
        [SugarColumn(ColumnName = "expiration_days")]
        public int ExpirationDays { get; set; } = 30;

        /// <summary>
        /// Optional message sent to all signers
        /// </summary>
        [SugarColumn(ColumnName = "message", Length = 1000, IsNullable = true)]
        public string Message { get; set; }

        /// <summary>
        /// User ID of the person who initiated the signing request
        /// </summary>
        [SugarColumn(ColumnName = "requested_by")]
        public long RequestedBy { get; set; }

        /// <summary>
        /// Timestamp when all signers completed signing
        /// </summary>
        [SugarColumn(ColumnName = "completed_date", IsNullable = true)]
        public DateTimeOffset? CompletedDate { get; set; }
    }
}
