using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Contracts.IServices.Integration;
using Item.Internal.StandardApi.Response;
using System.Net;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace FlowFlex.WebApi.Controllers.Integration
{
    /// <summary>
    /// External Integration API - APIs for external CRM/ERP systems to integrate with FlowFlex
    /// </summary>
    [ApiController]
    [Route("integration/external/v{version:apiVersion}")]
    [Display(Name = "external-integration")]
    [Authorize]
    public class ExternalIntegrationController : Controllers.ControllerBase
    {
        private readonly IExternalIntegrationService _externalIntegrationService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ExternalIntegrationController(
            IExternalIntegrationService externalIntegrationService,
            IWebHostEnvironment webHostEnvironment)
        {
            _externalIntegrationService = externalIntegrationService;
            _webHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// Get workflows by System ID
        /// Returns a list of workflows available for a specific entity mapping identified by System ID
        /// </summary>
        /// <param name="systemId">System ID (unique identifier for entity mapping)</param>
        /// <returns>List of available workflows</returns>
        [HttpGet("workflows")]
        [ProducesResponseType<SuccessResponse<List<WorkflowInfoDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetWorkflowsBySystemId([FromQuery] string systemId)
        {
            var workflows = await _externalIntegrationService.GetWorkflowsBySystemIdAsync(systemId);
            return Success(workflows);
        }

        /// <summary>
        /// Create a case from external system
        /// Creates a new case/onboarding based on entity type and workflow selection
        /// </summary>
        /// <param name="request">Create case request</param>
        /// <returns>Created case information</returns>
        [HttpPost("cases")]
        [ProducesResponseType<SuccessResponse<CreateCaseFromExternalResponse>>((int)HttpStatusCode.Created)]
        public async Task<IActionResult> CreateCase([FromBody] CreateCaseFromExternalRequest request)
        {
            var result = await _externalIntegrationService.CreateCaseAsync(request);
            return Success(result);
        }

        /// <summary>
        /// Get case info (demo endpoint)
        /// Retrieves case information based on search parameters. 
        /// Returns the same parameter structure with additional case data if found.
        /// </summary>
        /// <param name="leadId">Lead ID from external system</param>
        /// <param name="customerName">Customer name</param>
        /// <param name="contactName">Contact person name</param>
        /// <param name="contactEmail">Contact email</param>
        /// <param name="contactPhone">Contact phone</param>
        /// <returns>Case information response</returns>
        [HttpGet("case-info")]
        [ProducesResponseType<SuccessResponse<CaseInfoResponse>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetCaseInfo(
            [FromQuery(Name = "Lead ID")] string? leadId = null,
            [FromQuery(Name = "Customer Name")] string? customerName = null,
            [FromQuery(Name = "Contact Name")] string? contactName = null,
            [FromQuery(Name = "Contact Email")] string? contactEmail = null,
            [FromQuery(Name = "Contact Phone")] string? contactPhone = null)
        {
            var request = new CaseInfoRequest
            {
                LeadId = leadId,
                CustomerName = customerName,
                ContactName = contactName,
                ContactEmail = contactEmail,
                ContactPhone = contactPhone
            };

            var result = await _externalIntegrationService.GetCaseInfoAsync(request);
            return Success(result);
        }

        /// <summary>
        /// CRM Case Info Demo Endpoint (GET with Query Parameters)
        /// This endpoint demonstrates the HTTP API Action configuration.
        /// It accepts the same parameters shown in the action configuration and returns case information.
        /// Note: This endpoint does not require authentication.
        /// </summary>
        /// <param name="leadId">Lead ID</param>
        /// <param name="customerName">Customer Name</param>
        /// <param name="contactName">Contact Name</param>
        /// <param name="contactEmail">Contact Email</param>
        /// <param name="contactPhone">Contact Phone</param>
        /// <returns>Case information with the same parameter structure</returns>
        [HttpGet("crm-case-info-demo")]
        [AllowAnonymous]
        [ProducesResponseType<SuccessResponse<CaseInfoResponse>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetCrmCaseInfoDemo(
            [FromQuery(Name = "Lead ID")] string? leadId = null,
            [FromQuery(Name = "Customer Name")] string? customerName = null,
            [FromQuery(Name = "Contact Name")] string? contactName = null,
            [FromQuery(Name = "Contact Email")] string? contactEmail = null,
            [FromQuery(Name = "Contact Phone")] string? contactPhone = null)
        {
            var request = new CaseInfoRequest
            {
                LeadId = leadId,
                CustomerName = customerName,
                ContactName = contactName,
                ContactEmail = contactEmail,
                ContactPhone = contactPhone
            };

            var result = await _externalIntegrationService.GetCaseInfoAsync(request);

            // Return with custom JSON settings to include null values
            return new JsonResult(SuccessResponse.Create(result), new Newtonsoft.Json.JsonSerializerSettings
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Include
            });
        }

        /// <summary>
        /// CRM Case Info Demo Endpoint (POST with JSON Body)
        /// This endpoint accepts case info request parameters in JSON body format.
        /// Supports both application/json and text/plain content types.
        /// Also supports reading from query parameters as fallback.
        /// Note: This endpoint does not require authentication.
        /// </summary>
        /// <param name="request">Case info request with parameters in body (optional)</param>
        /// <param name="leadId">Lead ID from query (fallback if body is empty)</param>
        /// <param name="customerName">Customer Name from query (fallback if body is empty)</param>
        /// <param name="contactName">Contact Name from query (fallback if body is empty)</param>
        /// <param name="contactEmail">Contact Email from query (fallback if body is empty)</param>
        /// <param name="contactPhone">Contact Phone from query (fallback if body is empty)</param>
        /// <returns>Case information with the same parameter structure</returns>
        [HttpPost("crm-case-info-demo")]
        [AllowAnonymous]
        [ProducesResponseType<SuccessResponse<CaseInfoResponse>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> PostCrmCaseInfoDemo(
            [FromBody] CaseInfoRequest? request,
            [FromQuery(Name = "Lead ID")] string? leadId = null,
            [FromQuery(Name = "Customer Name")] string? customerName = null,
            [FromQuery(Name = "Contact Name")] string? contactName = null,
            [FromQuery(Name = "Contact Email")] string? contactEmail = null,
            [FromQuery(Name = "Contact Phone")] string? contactPhone = null)
        {
            // If request is null or empty, try to read from request body manually
            // This handles cases where Content-Type is text/plain or other non-JSON types
            if (request == null || (string.IsNullOrEmpty(request.LeadId) && string.IsNullOrEmpty(request.CustomerName)))
            {
                try
                {
                    // Enable buffering to allow reading body multiple times
                    Request.EnableBuffering();
                    Request.Body.Position = 0;

                    using var reader = new System.IO.StreamReader(Request.Body, System.Text.Encoding.UTF8, leaveOpen: true);
                    var bodyContent = await reader.ReadToEndAsync();

                    // Reset position for potential future reads
                    Request.Body.Position = 0;

                    if (!string.IsNullOrWhiteSpace(bodyContent))
                    {
                        // Try to parse JSON from body
                        request = Newtonsoft.Json.JsonConvert.DeserializeObject<CaseInfoRequest>(bodyContent);
                    }
                }
                catch (Exception)
                {
                    // If parsing fails, continue to use query parameters
                }
            }

            // If still null or empty, try to use query parameters as fallback
            if (request == null || (string.IsNullOrEmpty(request.LeadId) && string.IsNullOrEmpty(request.CustomerName)))
            {
                if (!string.IsNullOrEmpty(leadId) || !string.IsNullOrEmpty(customerName))
                {
                    request = new CaseInfoRequest
                    {
                        LeadId = leadId,
                        CustomerName = customerName,
                        ContactName = contactName,
                        ContactEmail = contactEmail,
                        ContactPhone = contactPhone
                    };
                }
                else
                {
                    request = new CaseInfoRequest();
                }
            }

            var result = await _externalIntegrationService.GetCaseInfoAsync(request);

            // Return with custom JSON settings to include null values
            return new JsonResult(SuccessResponse.Create(result), new Newtonsoft.Json.JsonSerializerSettings
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Include
            });
        }

        /// <summary>
        /// Get attachments by case ID
        /// Retrieves attachment list from ff_onboarding_file table based on case ID (onboarding ID)
        /// </summary>
        /// <param name="caseId">Case ID (onboarding ID)</param>
        /// <returns>Attachments list response</returns>
        [HttpGet("attachments")]
        [ProducesResponseType<SuccessResponse<GetAttachmentsFromExternalResponse>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAttachments([FromQuery(Name = "CaseId")] string caseId)
        {
            var result = await _externalIntegrationService.GetAttachmentsByCaseIdAsync(caseId);
            return Success(result);
        }

        /// <summary>
        /// Get WFE Attachment integration protocol documentation
        /// Returns the API documentation for WFE Attachment integration protocol
        /// </summary>
        /// <returns>Documentation content</returns>
        [HttpGet("attachments/protocol")]
        [ProducesResponseType<SuccessResponse<string>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAttachmentProtocolDocumentation()
        {
            try
            {
                // Get base paths
                var contentRoot = _webHostEnvironment.ContentRootPath;
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var currentDirectory = Directory.GetCurrentDirectory();

                // Try multiple possible paths (prioritize same directory as controller)
                var possiblePaths = new List<string>
                {
                    // Path in the same directory as this controller (most reliable)
                    Path.Combine(baseDirectory, "Controllers", "Integration", "WFE Attachment 接入协议.md"),
                    // Path relative to content root Controllers/Integration
                    Path.Combine(contentRoot, "Controllers", "Integration", "WFE Attachment 接入协议.md"),
                    // Path relative to current directory Controllers/Integration
                    Path.Combine(currentDirectory, "Controllers", "Integration", "WFE Attachment 接入协议.md"),
                    // Fallback to Docs/API paths
                    Path.Combine(contentRoot, "Docs", "API", "WFE Attachment 接入协议.md"),
                    Path.Combine(baseDirectory, "Docs", "API", "WFE Attachment 接入协议.md"),
                    Path.Combine(currentDirectory, "Docs", "API", "WFE Attachment 接入协议.md"),
                    // Monorepo paths
                    Path.Combine(contentRoot, "..", "Docs", "API", "WFE Attachment 接入协议.md"),
                    Path.Combine(baseDirectory, "..", "Docs", "API", "WFE Attachment 接入协议.md")
                };

                string? docPath = null;
                foreach (var path in possiblePaths)
                {
                    try
                    {
                        var normalizedPath = Path.GetFullPath(path);
                        if (System.IO.File.Exists(normalizedPath))
                        {
                            docPath = normalizedPath;
                            break;
                        }
                    }
                    catch
                    {
                        // Continue to next path if this one fails
                        continue;
                    }
                }

                if (docPath == null || !System.IO.File.Exists(docPath))
                {
                    var searchedPaths = string.Join("\n", possiblePaths.Select(p =>
                    {
                        try { return Path.GetFullPath(p); } catch { return p; }
                    }));
                    return NotFound(new { message = $"Documentation file not found. Searched paths:\n{searchedPaths}" });
                }

                var content = await System.IO.File.ReadAllTextAsync(docPath, System.Text.Encoding.UTF8);
                return Success(content);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = $"Failed to read documentation: {ex.Message}" });
            }
        }
    }
}

