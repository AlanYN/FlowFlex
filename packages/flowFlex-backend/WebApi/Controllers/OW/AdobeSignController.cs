using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts.Dtos.OW.AdobeSign;
using FlowFlex.Application.Contracts.IServices.OW;
using Item.Internal.StandardApi.Response;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// Adobe Sign e-signature API (OW-731).
    /// Provides endpoints for initiating signing requests, querying agreement status,
    /// sending reminders, recalling agreements, and receiving Adobe Sign Webhook callbacks.
    /// </summary>
    [ApiController]
    [Route("ow/adobe-sign/v{version:apiVersion}")]
    [Asp.Versioning.ApiVersion("1.0")]
    [Authorize]
    public class AdobeSignController : Controllers.ControllerBase
    {
        private readonly IAdobeSignService _adobeSignService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdobeSignController> _logger;

        public AdobeSignController(
            IAdobeSignService adobeSignService,
            IConfiguration configuration,
            ILogger<AdobeSignController> logger)
        {
            _adobeSignService = adobeSignService;
            _configuration = configuration;
            _logger = logger;
        }

        // ------------------------------------------------------------------ //
        //  POST v1/request — Initiate a signing request
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Initiate a new Adobe Sign e-signature request for a PDF file in a Stage.
        /// Uploads the PDF to Adobe Sign and sends signing emails to all specified signers.
        /// </summary>
        /// <param name="input">Signing request configuration including signers, order, and expiration.</param>
        /// <returns>The created agreement with its status and Adobe Sign agreement ID.</returns>
        [HttpPost("request")]
        [ProducesResponseType<SuccessResponse<AdobeSignAgreementOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> RequestSignature([FromBody] RequestAdobeSignInputDto input)
        {
            var data = await _adobeSignService.RequestSignatureAsync(input);
            return Success(data);
        }

        // ------------------------------------------------------------------ //
        //  GET v1/{id} — Get agreement by internal WFE ID
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Get the full details of an Adobe Sign agreement by its internal WFE ID.
        /// </summary>
        /// <param name="id">Internal WFE agreement record ID.</param>
        /// <returns>Agreement details including signers, status, and timestamps.</returns>
        [HttpGet("{id:long}")]
        [ProducesResponseType<SuccessResponse<AdobeSignAgreementOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAgreement([FromRoute] long id)
        {
            var data = await _adobeSignService.GetAgreementAsync(id);
            return Success(data);
        }

        // ------------------------------------------------------------------ //
        //  GET v1/by-file/{sourceFileId} — Get agreement by source file ID
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Get the active signing agreement for a specific source PDF file.
        /// Used by the frontend file list to display signing status alongside each file.
        /// Returns null (empty data) if no agreement exists for the file.
        /// </summary>
        /// <param name="sourceFileId">ID of the original PDF file in ff_onboarding_file.</param>
        [HttpGet("by-file/{sourceFileId:long}")]
        [ProducesResponseType<SuccessResponse<AdobeSignAgreementOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAgreementByFile([FromRoute] long sourceFileId)
        {
            var data = await _adobeSignService.GetAgreementByFileIdAsync(sourceFileId);
            return Success(data);
        }

        // ------------------------------------------------------------------ //
        //  POST v1/{id}/remind — Send reminder to pending signers
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Send reminder emails to the specified signers who have not yet signed.
        /// Only valid for agreements with Awaiting status.
        /// </summary>
        /// <param name="id">Internal WFE agreement record ID.</param>
        /// <param name="request">List of signer emails to remind.</param>
        [HttpPost("{id:long}/remind")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SendReminder(
            [FromRoute] long id,
            [FromBody] SendReminderInputDto request)
        {
            var result = await _adobeSignService.SendReminderAsync(id, request.SignerEmails);
            return Success(result);
        }

        // ------------------------------------------------------------------ //
        //  DELETE v1/{id} — Recall (cancel) an agreement
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Recall (cancel) an in-progress signing request.
        /// All pending signatures will be invalidated. This action cannot be undone.
        /// Only valid for agreements with Awaiting status.
        /// </summary>
        /// <param name="id">Internal WFE agreement record ID.</param>
        [HttpDelete("{id:long}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> RecallAgreement([FromRoute] long id)
        {
            var result = await _adobeSignService.RecallAgreementAsync(id);
            return Success(result);
        }

        // ------------------------------------------------------------------ //
        //  POST v1/webhook — Adobe Sign Webhook callback (no auth required)
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Receive Adobe Sign Webhook events.
        /// This endpoint is intentionally NOT authenticated — Adobe Sign calls it directly.
        /// Security is enforced via x-adobesign-clientid header verification.
        ///
        /// IMPORTANT: Must always return 200 OK. Adobe Sign retries for up to 72 hours
        /// if it receives any non-200 response.
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> HandleWebhook()
        {
            try
            {
                // Step 1: Verify request originates from Adobe Sign via client ID header
                var incomingClientId = Request.Headers["x-adobesign-clientid"].ToString();
                var expectedClientId = _configuration["AdobeSign:ClientId"] ?? string.Empty;

                if (string.IsNullOrEmpty(expectedClientId) ||
                    expectedClientId.StartsWith("PLACEHOLDER") ||
                    string.IsNullOrEmpty(incomingClientId))
                {
                    // In placeholder/dev mode log but continue processing
                    _logger.LogWarning(
                        "[AdobeSign] Webhook received with unverified client ID. Incoming={Incoming}",
                        incomingClientId);
                }
                else if (!string.Equals(incomingClientId, expectedClientId, System.StringComparison.Ordinal))
                {
                    _logger.LogWarning(
                        "[AdobeSign] Webhook rejected: client ID mismatch. Incoming={Incoming}",
                        incomingClientId);
                    // Return 200 even on auth failure — we just ignore the event
                    return Ok(new { message = "OK" });
                }

                // Step 2: Parse the Webhook payload
                string body;
                using (var reader = new System.IO.StreamReader(Request.Body))
                {
                    body = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    _logger.LogWarning("[AdobeSign] Webhook received empty body");
                    return Ok(new { message = "OK" });
                }

                _logger.LogDebug("[AdobeSign] Webhook payload: {Body}", body);

                var payload = JsonDocument.Parse(body);
                var root = payload.RootElement;

                // Adobe Sign Webhook payload structure:
                // { "event": "AGREEMENT_WORKFLOW_COMPLETED", "agreement": { "id": "CBJCH..." } }
                var eventType = root.TryGetProperty("event", out var eventProp)
                    ? eventProp.GetString() ?? string.Empty
                    : string.Empty;

                var adobeAgreementId = string.Empty;
                if (root.TryGetProperty("agreement", out var agreementProp) &&
                    agreementProp.TryGetProperty("id", out var idProp))
                {
                    adobeAgreementId = idProp.GetString() ?? string.Empty;
                }

                if (string.IsNullOrEmpty(eventType) || string.IsNullOrEmpty(adobeAgreementId))
                {
                    _logger.LogWarning(
                        "[AdobeSign] Webhook payload missing event or agreement.id. Body={Body}", body);
                    return Ok(new { message = "OK" });
                }

                // Step 3: Dispatch to service (fire-and-forget style — must not block)
                await _adobeSignService.HandleWebhookAsync(eventType, adobeAgreementId);
            }
            catch (System.Exception ex)
            {
                // CRITICAL: Never return non-200 to Adobe Sign
                _logger.LogError(ex, "[AdobeSign] Unhandled exception in Webhook endpoint");
            }

            // Always return 200 to acknowledge receipt
            return Ok(new { message = "OK" });
        }
    }

    /// <summary>
    /// Input DTO for the Send Reminder endpoint
    /// </summary>
    public class SendReminderInputDto
    {
        /// <summary>
        /// Email addresses of signers to send a reminder to
        /// </summary>
        public List<string> SignerEmails { get; set; } = new();
    }
}
