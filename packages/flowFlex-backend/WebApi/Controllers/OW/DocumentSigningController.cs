using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlowFlex.Application.Contracts.Dtos.OW.DocumentSigning;
using FlowFlex.Application.Contracts.IServices.OW;
using Item.Internal.StandardApi.Response;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// Document signing API — receives a signed PDF from the browser and persists it (OW-704).
    /// </summary>
    [ApiController]
    [Route("ow/files/v{version:apiVersion}")]
    [Asp.Versioning.ApiVersion("1.0")]
    [Authorize]
    public class DocumentSigningController : Controllers.ControllerBase
    {
        private readonly IDocumentSigningService _documentSigningService;

        public DocumentSigningController(IDocumentSigningService documentSigningService)
        {
            _documentSigningService = documentSigningService;
        }

        /// <summary>
        /// Submit a signed PDF for a given source file.
        /// The backend validates the source file, computes the SHA-256 hash independently,
        /// uploads the signed PDF to blob storage, and writes the signing record atomically.
        /// </summary>
        /// <param name="fileId">ID of the original (unsigned) onboarding file.</param>
        /// <param name="dto">Multipart form-data: signed PDF file, signer name, and sign time (ISO 8601 UTC).</param>
        /// <returns>Signed file metadata including ID, download URL, file name, and SHA-256 hash.</returns>
        [HttpPost("{fileId}/sign")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType<SuccessResponse<SignDocumentOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SignDocument(
            [FromRoute] long fileId,
            [FromForm] SignDocumentInputDto dto)
        {
            var data = await _documentSigningService.SignDocumentAsync(fileId, dto);
            return Success(data);
        }
    }
}
