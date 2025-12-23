using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Contracts.IServices.Integration;
using Item.Internal.StandardApi.Response;
using System.Net;
using FlowFlex.Application.Filter;

namespace FlowFlex.WebApi.Controllers.Integration
{
    /// <summary>
    /// Quick link management API
    /// </summary>
    [ApiController]
    [PortalAccess] // Allow Portal token access
    [Route("integration/quick-links/v{version:apiVersion}")]
    [Display(Name = "quick-link")]
    [Authorize]
    public class QuickLinkController : Controllers.ControllerBase
    {
        private readonly IQuickLinkService _quickLinkService;

        public QuickLinkController(IQuickLinkService quickLinkService)
        {
            _quickLinkService = quickLinkService;
        }

        /// <summary>
        /// Create quick link
        /// </summary>
        [HttpPost]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Create([FromBody] QuickLinkInputDto input)
        {
            var id = await _quickLinkService.CreateAsync(input);
            return Success(id);
        }

        /// <summary>
        /// Update quick link
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Update(long id, [FromBody] QuickLinkInputDto input)
        {
            var result = await _quickLinkService.UpdateAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Delete quick link
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _quickLinkService.DeleteAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Get quick link by id
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType<SuccessResponse<QuickLinkOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetById(long id)
        {
            var data = await _quickLinkService.GetByIdAsync(id);
            return Success(data);
        }

        /// <summary>
        /// Get quick links by integration id (query parameter) or get all quick links if integrationId is not provided
        /// </summary>
        [HttpGet]
        [ProducesResponseType<SuccessResponse<List<QuickLinkOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByIntegration([FromQuery] long? integrationId)
        {
            if (integrationId.HasValue)
            {
                var data = await _quickLinkService.GetByIntegrationIdAsync(integrationId.Value);
                return Success(data);
            }
            // Return all quick links if integrationId is not provided
            var allData = await _quickLinkService.GetAllAsync();
            return Success(allData);
        }

        /// <summary>
        /// Get quick links by integration id (path parameter)
        /// </summary>
        [HttpGet("by-integration/{integrationId}")]
        [ProducesResponseType<SuccessResponse<List<QuickLinkOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByIntegrationId(long integrationId)
        {
            var data = await _quickLinkService.GetByIntegrationIdAsync(integrationId);
            return Success(data);
        }

        /// <summary>
        /// Generate URL with parameters
        /// </summary>
        [HttpPost("{id}/generate-url")]
        [ProducesResponseType<SuccessResponse<string>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GenerateUrl(long id, [FromBody] Dictionary<string, string> dataContext)
        {
            var url = await _quickLinkService.GenerateUrlAsync(id, dataContext);
            return Success(url);
        }
    }
}

