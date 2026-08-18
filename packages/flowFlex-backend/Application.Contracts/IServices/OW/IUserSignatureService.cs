using FlowFlex.Application.Contracts.Dtos.OW.UserSignature;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Service interface for managing user electronic signatures
    /// </summary>
    public interface IUserSignatureService
    {
        /// <summary>
        /// Get all signatures belonging to the currently authenticated user
        /// </summary>
        /// <returns>List of the user's signatures</returns>
        Task<List<ProfileSignatureOutputDto>> GetByCurrentUserAsync();

        /// <summary>
        /// Create a new signature for the currently authenticated user.
        /// Rejects if the user already has 7 signatures, or if the decoded image exceeds 500 KB.
        /// </summary>
        /// <param name="input">DTO containing the base64-encoded PNG image</param>
        /// <returns>The newly created signature</returns>
        Task<ProfileSignatureOutputDto> CreateAsync(CreateSignatureInputDto input);

        /// <summary>
        /// Soft-delete a signature belonging to the currently authenticated user.
        /// Throws 403 if the signature does not belong to the current user.
        /// </summary>
        /// <param name="signatureId">ID of the signature to delete</param>
        Task DeleteAsync(long signatureId);
    }
}
