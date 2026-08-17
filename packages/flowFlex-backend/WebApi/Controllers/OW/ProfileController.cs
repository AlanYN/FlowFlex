using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlowFlex.Application.Contracts.Dtos.OW.UserSignature;
using FlowFlex.Application.Contracts.IServices.OW;
using Item.Internal.StandardApi.Response;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// User profile API — electronic signatures
    /// </summary>
    [ApiController]
    [Route("ow/profile/v{version:apiVersion}")]
    [Asp.Versioning.ApiVersion("1.0")]
    [Authorize]
    public class ProfileController : Controllers.ControllerBase
    {
        private readonly IUserSignatureService _userSignatureService;

        public ProfileController(IUserSignatureService userSignatureService)
        {
            _userSignatureService = userSignatureService;
        }

        /// <summary>
        /// Get all signatures belonging to the current user
        /// </summary>
        /// <returns>List of the current user's signatures</returns>
        [HttpGet("signatures")]
        [ProducesResponseType<SuccessResponse<List<ProfileSignatureOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetSignatures()
        {
            var data = await _userSignatureService.GetByCurrentUserAsync();
            return Success(data);
        }

        /// <summary>
        /// Create a new signature for the current user
        /// </summary>
        /// <param name="input">DTO containing the base64-encoded PNG image</param>
        /// <returns>The newly created signature</returns>
        [HttpPost("signatures")]
        [ProducesResponseType<SuccessResponse<ProfileSignatureOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CreateSignature([FromBody] CreateSignatureInputDto input)
        {
            var data = await _userSignatureService.CreateAsync(input);
            return Success(data);
        }

        /// <summary>
        /// Soft-delete a signature belonging to the current user.
        /// Returns 403 if the signature does not belong to the current user.
        /// </summary>
        /// <param name="signatureId">ID of the signature to delete</param>
        [HttpDelete("signatures/{signatureId}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> DeleteSignature([FromRoute] long signatureId)
        {
            await _userSignatureService.DeleteAsync(signatureId);
            return Success(true);
        }
    }
}
