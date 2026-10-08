using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// Adobe Sign Agreement repository interface (OW-731)
    /// </summary>
    public interface IAdobeSignAgreementRepository : IBaseRepository<AdobeSignAgreement>
    {
        /// <summary>
        /// Get the active agreement for a specific source file ID.
        /// Returns null if no agreement exists for the file.
        /// </summary>
        /// <param name="sourceFileId">Original PDF file ID in ff_onboarding_file</param>
        Task<AdobeSignAgreement?> GetBySourceFileIdAsync(long sourceFileId);

        /// <summary>
        /// Get all agreements for a specific onboarding case
        /// </summary>
        /// <param name="onboardingId">Onboarding (Case) ID</param>
        Task<List<AdobeSignAgreement>> GetByOnboardingIdAsync(long onboardingId);

        /// <summary>
        /// Get agreement by Adobe Sign's own agreement ID string
        /// </summary>
        /// <param name="adobeAgreementId">Agreement ID returned by Adobe Sign API</param>
        Task<AdobeSignAgreement?> GetByAgreementIdAsync(string adobeAgreementId);

        /// <summary>
        /// Get all Awaiting agreements for an onboarding case (pending signatures)
        /// </summary>
        Task<List<AdobeSignAgreement>> GetPendingByOnboardingIdAsync(long onboardingId);
    }
}
