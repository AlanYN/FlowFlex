using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.DocumentSnapshot;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Shared.Const;
using Item.Internal.StandardApi.Response;
using WebApi.Authorization;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// Document operation log API
    /// </summary>
    [ApiController]
    [Route("ow/document-operation-logs/v{version:apiVersion}")]
    [Display(Name = "document-operation-log")]
    [Authorize]
    public class DocumentOperationLogController : Controllers.ControllerBase
    {
        private readonly IDocumentOperationLogService _service;

        public DocumentOperationLogController(IDocumentOperationLogService service)
        {
            _service = service;
        }

        /// <summary>
        /// Report a MUTATION operation log from frontend (fire-and-forget on frontend side)
        /// </summary>
        [HttpPost]
        [WFEAuthorize(PermissionConsts.Document.Update)]
        [ProducesResponseType<SuccessResponse<string>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> AddLog([FromBody] DocumentOperationLogInputDto input)
        {
            if (input == null) return BadRequest("Input is required");
            var id = await _service.AddLogAsync(input);
            return Success(id);
        }

        /// <summary>
        /// Query operation logs by unitId (supports time range and user filters)
        /// </summary>
        [HttpGet("{unitId}")]
        [WFEAuthorize(PermissionConsts.Document.Read)]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLogs(
            string unitId,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string userId = null,
            [FromQuery] string keyword = null,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _service.GetLogsByUnitIdAsync(unitId, from, to, userId, keyword, pageIndex, pageSize);
            return Success(result);
        }

        /// <summary>
        /// Get current max revision number for a document
        /// </summary>
        [HttpGet("{unitId}/revision")]
        [WFEAuthorize(PermissionConsts.Document.Read)]
        [ProducesResponseType<SuccessResponse<int>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetRevision(string unitId)
        {
            var revision = await _service.GetCurrentRevisionAsync(unitId);
            return Success(revision);
        }
    }
}
