using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
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
    /// Document snapshot management API
    /// </summary>
    [ApiController]
    [Route("ow/document-snapshots/v{version:apiVersion}")]
    [Display(Name = "document-snapshot")]
    [Authorize]
    public class DocumentSnapshotController : Controllers.ControllerBase
    {
        private readonly IDocumentSnapshotService _service;

        public DocumentSnapshotController(IDocumentSnapshotService service)
        {
            _service = service;
        }

        /// <summary>
        /// Get document snapshot by unitId
        /// </summary>
        [HttpGet("{unitId}")]
        [WFEAuthorize(PermissionConsts.Document.Read)]
        [ProducesResponseType<SuccessResponse<DocumentSnapshotOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByUnitId(string unitId)
        {
            var data = await _service.GetByUnitIdAsync(unitId);
            return Success(data);
        }

        /// <summary>
        /// Create a new empty document snapshot; returns the new unitId
        /// </summary>
        [HttpPost]
        [WFEAuthorize(PermissionConsts.Document.Create)]
        [ProducesResponseType<SuccessResponse<string>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Create([FromBody] DocumentSnapshotInputDto input)
        {
            if (input == null) return BadRequest("Input is required");
            var unitId = await _service.CreateAsync(input);
            return Success(unitId);
        }

        /// <summary>
        /// Save/update document snapshot (Upsert)
        /// </summary>
        [HttpPost("{unitId}/save")]
        [WFEAuthorize(PermissionConsts.Document.Update)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Save(string unitId, [FromBody] DocumentSnapshotInputDto input)
        {
            if (input == null) return BadRequest("Input is required");
            var result = await _service.SaveAsync(unitId, input);
            return Success(result);
        }

        /// <summary>
        /// Soft-delete document by numeric ID
        /// </summary>
        [HttpDelete("{id:long}")]
        [WFEAuthorize(PermissionConsts.Document.Delete)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Paged query with keyword / docType / sort filters
        /// </summary>
        [HttpPost("query")]
        [WFEAuthorize(PermissionConsts.Document.Read)]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Query([FromBody] DocumentSnapshotQueryRequest query)
        {
            var result = await _service.QueryAsync(query ?? new DocumentSnapshotQueryRequest());
            return Success(result);
        }

        /// <summary>
        /// Get documents associated with a specific business entity
        /// </summary>
        [HttpGet("by-entity/{entityType}/{entityId:long}")]
        [WFEAuthorize(PermissionConsts.Document.Read)]
        [ProducesResponseType<SuccessResponse<List<DocumentSnapshotOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByEntity(string entityType, long entityId)
        {
            var data = await _service.GetByEntityAsync(entityType, entityId);
            return Success(data);
        }
    }
}
