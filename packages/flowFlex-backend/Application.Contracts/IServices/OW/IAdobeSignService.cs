using System.Collections.Generic;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.AdobeSign;
using FlowFlex.Domain.Shared;
namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Service interface for Adobe Sign e-signature integration (OW-731).
    /// Handles creating agreements, tracking status, sending reminders,
    /// recalling requests, and processing Webhook callbacks.
    /// </summary>
    public interface IAdobeSignService : IScopedService
    {
        /// <summary>
        /// Initiate a new signing request:
        /// 1. Download the source PDF from blob storage.
        /// 2. Upload to Adobe Sign as a transient document.
        /// 3. Create an Agreement with the provided signers and options.
        /// 4. Persist the agreement record in ff_adobe_sign_agreement.
        /// </summary>
        Task<AdobeSignAgreementOutputDto> RequestSignatureAsync(RequestAdobeSignInputDto input);

        /// <summary>
        /// Get an agreement by its internal WFE ID
        /// </summary>
        Task<AdobeSignAgreementOutputDto> GetAgreementAsync(long id);

        /// <summary>
        /// Get the active agreement for a specific source file ID.
        /// Returns null if no agreement exists for that file.
        /// </summary>
        Task<AdobeSignAgreementOutputDto?> GetAgreementByFileIdAsync(long sourceFileId);

        /// <summary>
        /// Send reminder emails to the specified signers who have not yet signed.
        /// </summary>
        /// <param name="id">Internal WFE agreement ID</param>
        /// <param name="signerEmails">Emails of signers to remind (must be Awaiting status)</param>
        Task<bool> SendReminderAsync(long id, List<string> signerEmails);

        /// <summary>
        /// Recall (cancel) an in-progress agreement.
        /// This invalidates all pending signatures. The action cannot be undone.
        /// </summary>
        /// <param name="id">Internal WFE agreement ID</param>
        Task<bool> RecallAgreementAsync(long id);

        /// <summary>
        /// Process an incoming Adobe Sign Webhook event.
        /// Updates the local agreement status and, on AGREEMENT_WORKFLOW_COMPLETED,
        /// downloads the signed PDF + Audit Trail and archives them to ff_onboarding_file.
        /// </summary>
        /// <param name="eventType">Adobe Sign event type (e.g. AGREEMENT_WORKFLOW_COMPLETED)</param>
        /// <param name="adobeAgreementId">The agreement ID from Adobe Sign</param>
        Task HandleWebhookAsync(string eventType, string adobeAgreementId);

        /// <summary>
        /// Get the list of Awaiting (pending) agreements for a specific onboarding case,
        /// including file names and signer counts. Used by Force Complete to warn the user.
        /// </summary>
        Task<List<PendingSignatureDto>> GetPendingSignaturesAsync(long onboardingId);
    }
}
