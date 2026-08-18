using FlowFlex.Application.Contracts.Dtos.OW.DocumentSigning;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Service interface for document signing operations (OW-704).
    /// Handles file validation, SHA-256 hash computation, blob upload, and
    /// atomic DB recording of a signed document.
    /// </summary>
    public interface IDocumentSigningService
    {
        /// <summary>
        /// Sign a document: validate the source file, compute its SHA-256 hash,
        /// upload the signed PDF to blob storage, and persist the signed
        /// <c>OnboardingFile</c> record together with an operation change-log entry
        /// inside a single DB transaction.
        /// </summary>
        /// <param name="fileId">ID of the original (unsigned) <c>ff_onboarding_file</c> row.</param>
        /// <param name="dto">Multipart form-data containing the signed PDF, signer name and sign time.</param>
        /// <returns>Output DTO with the new signed file ID, download URL, file name and hash.</returns>
        Task<SignDocumentOutputDto> SignDocumentAsync(long fileId, SignDocumentInputDto dto);

        /// <summary>
        /// Compute the SHA-256 hash of <paramref name="data"/> and return it as a
        /// 64-character lower-case hex string.
        /// </summary>
        /// <param name="data">Raw file bytes.</param>
        /// <returns>64-character lower-case hex string, e.g. <c>a3f1...</c></returns>
        string ComputeSha256(byte[] data);

        /// <summary>
        /// Build the canonical file name for a signed document.
        /// Format: <c>{original_without_ext}_signed_{signerName}_{date:MMddyyyy}.pdf</c>
        /// </summary>
        /// <param name="originalName">Original file name (with or without extension).</param>
        /// <param name="signerName">Name of the signer.</param>
        /// <param name="date">Date of signing (only the date portion is used).</param>
        /// <returns>Signed file name string.</returns>
        string BuildSignedFileName(string originalName, string signerName, DateTime date);
    }
}
