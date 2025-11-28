using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Contracts.IServices.Integration;
using Item.Internal.StandardApi.Response;
using System.Net;

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

        public ExternalIntegrationController(IExternalIntegrationService externalIntegrationService)
        {
            _externalIntegrationService = externalIntegrationService;
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
    }
}

