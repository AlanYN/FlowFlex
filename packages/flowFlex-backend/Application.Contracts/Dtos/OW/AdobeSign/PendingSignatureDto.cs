namespace FlowFlex.Application.Contracts.Dtos.OW.AdobeSign
{
    /// <summary>
    /// Summary of a single pending (Awaiting) Adobe Sign agreement.
    /// Returned by the Force Complete pre-check endpoint so the frontend
    /// can display which documents are still waiting for signatures.
    /// </summary>
    public class PendingSignatureDto
    {
        /// <summary>Internal WFE agreement ID</summary>
        public string AgreementId { get; set; }

        /// <summary>Original PDF file name (from ff_onboarding_file)</summary>
        public string FileName { get; set; }

        /// <summary>Stage name the file belongs to</summary>
        public string StageName { get; set; }

        /// <summary>Number of signers who have not yet signed</summary>
        public int PendingSignerCount { get; set; }

        /// <summary>Total number of signers on the agreement</summary>
        public int TotalSignerCount { get; set; }

        /// <summary>When the signing request was initiated</summary>
        public DateTimeOffset CreatedAt { get; set; }
    }
}
