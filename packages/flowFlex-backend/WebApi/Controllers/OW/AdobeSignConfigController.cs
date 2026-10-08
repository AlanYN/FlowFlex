using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlowFlex.Application.Contracts.Dtos.OW.AdobeSign;
using FlowFlex.Application.Contracts.IServices.OW;
using Item.Internal.StandardApi.Response;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// Manages the AppCode → Adobe Sign account key mapping.
    /// Allows admins to configure which Adobe Sign account (Item / Unisco) is used
    /// for each AppCode. AppCodes without a DB record fall back to DefaultAccount in config.
    /// Route: ow/adobe-sign-config/v{version}
    /// </summary>
    [ApiController]
    [Route("ow/adobe-sign-config/v{version:apiVersion}")]
    [Asp.Versioning.ApiVersion("1.0")]
    [Authorize]
    public class AdobeSignConfigController : Controllers.ControllerBase
    {
        private readonly IAdobeSignTenantConfigService _configService;

        public AdobeSignConfigController(IAdobeSignTenantConfigService configService)
        {
            _configService = configService;
        }

        // ------------------------------------------------------------------ //
        //  GET v1/accounts — list available account keys from appsettings
        // ------------------------------------------------------------------ //

        /// <summary>
        /// List the Adobe Sign account keys available in appsettings.json.
        /// Use these values when creating or updating a tenant mapping.
        /// </summary>
        [HttpGet("accounts")]
        [ProducesResponseType<SuccessResponse<List<string>>>((int)HttpStatusCode.OK)]
        public IActionResult GetAvailableAccounts()
        {
            var keys = _configService.GetAvailableAccountKeys();
            return Success(new
            {
                availableAccountKeys = keys,
                defaultAccountKey = _configService.GetDefaultAccountKey()
            });
        }

        // ------------------------------------------------------------------ //
        //  GET v1 — list all DB-configured tenant mappings
        // ------------------------------------------------------------------ //

        /// <summary>
        /// List all AppCode → AccountKey mappings stored in the DB.
        /// AppCodes not listed here will use the DefaultAccount.
        /// </summary>
        [HttpGet]
        [ProducesResponseType<SuccessResponse<List<AdobeSignTenantConfigOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAll()
        {
            var data = await _configService.GetAllAsync();
            return Success(data);
        }

        // ------------------------------------------------------------------ //
        //  PUT v1 — create or update a mapping
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Create or update the Adobe Sign account for a specific AppCode.
        /// AccountKey must match one of the keys returned by GET /accounts.
        /// </summary>
        [HttpPut]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Upsert([FromBody] UpsertAdobeSignTenantConfigInputDto input)
        {
            await _configService.UpsertAsync(input);
            return Success(true);
        }

        // ------------------------------------------------------------------ //
        //  DELETE v1/{appCode} — remove a mapping (falls back to DefaultAccount)
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Remove the account mapping for an AppCode.
        /// After deletion the AppCode will fall back to the DefaultAccount.
        /// </summary>
        [HttpDelete("{appCode}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete([FromRoute] string appCode)
        {
            var result = await _configService.DeleteAsync(appCode);
            return Success(result);
        }
    }
}
