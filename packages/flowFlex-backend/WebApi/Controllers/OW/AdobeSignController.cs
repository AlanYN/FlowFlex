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

        [HttpDelete("{id:long}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> RecallAgreement([FromRoute] long id)
        {
            var result = await _adobeSignService.RecallAgreementAsync(id);
            return Success(result);
        }

        // ------------------------------------------------------------------ //
        //  GET v1/pending/{onboardingId} — List pending signatures before Force Complete
        // ------------------------------------------------------------------ //

        [HttpGet("pending/{onboardingId:long}")]
        [ProducesResponseType<SuccessResponse<List<AdobeSignAgreementOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetPendingSignatures([FromRoute] long onboardingId)
        {
            var data = await _adobeSignService.GetPendingSignaturesAsync(onboardingId);
            return Success(data);
        }

        // ------------------------------------------------------------------ //
        //  GET  v1/webhook — Adobe Sign Webhook verification (no auth required)
        //  POST v1/webhook — Adobe Sign Webhook callback   (no auth required)
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Adobe Sign Webhook verification — called when registering a new Webhook.
        /// Echoes back the X-ADOBESIGN-CLIENTID header as required by Adobe Sign.
        /// </summary>
        [HttpGet("webhook")]
        [AllowAnonymous]
        public IActionResult VerifyWebhook()
        {
            var clientId = Request.Headers["x-adobesign-clientid"].ToString();
            Response.Headers["x-adobesign-clientid"] = clientId;
            return Ok(new { xAdobeSignClientId = clientId });
        }

        /// <summary>
        /// Receive Adobe Sign Webhook event notifications.
        /// Must always return 200 with xAdobeSignClientId echoed back — Adobe Sign retries for 72 hours on failure.
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> HandleWebhook()
        {
            var incomingClientId = Request.Headers["x-adobesign-clientid"].ToString();
            Response.Headers["x-adobesign-clientid"] = incomingClientId;

            try
            {
                var expectedClientId = _configuration["AdobeSign:ClientId"] ?? string.Empty;

                if (!string.IsNullOrEmpty(expectedClientId) &&
                    !expectedClientId.StartsWith("PLACEHOLDER") &&
                    !string.IsNullOrEmpty(incomingClientId) &&
                    !string.Equals(incomingClientId, expectedClientId, System.StringComparison.Ordinal))
                {
                    _logger.LogWarning("[AdobeSign] Webhook rejected: client ID mismatch. Incoming={Incoming}", incomingClientId);
                    return Ok(new { xAdobeSignClientId = incomingClientId });
                }

                string body;
                using (var reader = new System.IO.StreamReader(Request.Body))
                {
                    body = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    _logger.LogWarning("[AdobeSign] Webhook received empty body");
                    return Ok(new { xAdobeSignClientId = incomingClientId });
                }

                _logger.LogDebug("[AdobeSign] Webhook payload: {Body}", body);

                var payload = JsonDocument.Parse(body);
                var root = payload.RootElement;

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
                    _logger.LogWarning("[AdobeSign] Webhook payload missing event or agreement.id. Body={Body}", body);
                    return Ok(new { xAdobeSignClientId = incomingClientId });
                }

                await _adobeSignService.HandleWebhookAsync(eventType, adobeAgreementId);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "[AdobeSign] Unhandled exception in Webhook endpoint");
            }

            return Ok(new { xAdobeSignClientId = incomingClientId });
        }
    }

    /// <summary>Input DTO for the Send Reminder endpoint</summary>
    public class SendReminderInputDto
    {
        public List<string> SignerEmails { get; set; } = new();
    }
}
