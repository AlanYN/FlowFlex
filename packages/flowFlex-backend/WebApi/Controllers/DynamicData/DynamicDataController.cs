using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.IServices.DynamicData;
using FlowFlex.Application.Contracts.Dtos.DynamicData;
using FlowFlex.Domain.Shared.Models.DynamicData;
using Item.Internal.StandardApi.Response;
using System.Net;

namespace FlowFlex.WebApi.Controllers.DynamicData;

/// <summary>
/// Dynamic data management API - Provides CRUD operations for business data with dynamic field schemas.
/// Supports flexible field definitions with different data types, batch operations, and partial field updates.
/// </summary>
[ApiController]
[Route("ow/dynamic-data/v{version:apiVersion}")]
[Display(Name = "dynamic-data")]
[Asp.Versioning.ApiVersion("1.0")]
[Authorize]
public class DynamicDataController : Controllers.ControllerBase
{
    private readonly IBusinessDataService _businessDataService;
    private const int DefaultModuleId = 0;

    public DynamicDataController(IBusinessDataService businessDataService)
    {
        _businessDataService = businessDataService;
    }

    /// <summary>
    /// Get a single business data record by ID with all its dynamic fields
    /// </summary>
    /// <param name="id">Business data record ID</param>
    /// <returns>Dynamic data object with all field values</returns>
    [HttpGet("{id}")]
    [ProducesResponseType<SuccessResponse<DynamicDataObject>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetById(long id)
    {
        var data = await _businessDataService.GetBusinessDataObjectAsync(id);
        if (data == null)
        {
            return NotFound();
        }
        return Success(data);
    }

    /// <summary>
    /// Batch get multiple business data records by their IDs
    /// </summary>
    /// <param name="ids">List of business data record IDs</param>
    /// <returns>List of dynamic data objects</returns>
    [HttpPost("batch")]
    [ProducesResponseType<SuccessResponse<List<DynamicDataObject>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetByIds([FromBody] List<long> ids)
    {
        var data = await _businessDataService.GetBusinessDataObjectListAsync(ids);
        return Success(data);
    }

    /// <summary>
    /// Create a new business data record with dynamic fields
    /// </summary>
    /// <param name="request">Business data creation request with field definitions and values</param>
    /// <returns>Created record ID</returns>
    [HttpPost]
    [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Create([FromBody] CreateBusinessDataRequest request)
    {
        var data = new DynamicDataObject(DefaultModuleId);
        if (request.Fields != null)
        {
            foreach (var field in request.Fields)
            {
                data.Add(new FieldDataItem
                {
                    FieldName = field.FieldName,
                    Value = field.Value,
                    DataType = field.DataType
                });
            }
        }
        data.InternalData = request.InternalData;

        var id = await _businessDataService.CreateBusinessDataAsync(data);
        return Success(id);
    }

    /// <summary>
    /// Full update of a business data record (replaces all fields)
    /// </summary>
    /// <param name="id">Business data record ID</param>
    /// <param name="request">Complete business data with all fields</param>
    /// <returns>Whether update was successful</returns>
    [HttpPut("{id}")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateBusinessDataRequest request)
    {
        var data = new DynamicDataObject(DefaultModuleId) { BusinessId = id };
        if (request.Fields != null)
        {
            foreach (var field in request.Fields)
            {
                data.Add(new FieldDataItem
                {
                    FieldName = field.FieldName,
                    Value = field.Value,
                    DataType = field.DataType
                });
            }
        }
        data.InternalData = request.InternalData;

        await _businessDataService.UpdateBusinessDataAsync(data);
        return Success(true);
    }

    /// <summary>
    /// Partial update - only update specific fields of a business data record (PATCH semantics)
    /// </summary>
    /// <param name="id">Business data record ID</param>
    /// <param name="fields">Dictionary of field names to new values (only specified fields are updated)</param>
    /// <returns>Whether update was successful</returns>
    [HttpPatch("{id}")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> UpdateFields(long id, [FromBody] Dictionary<string, object?> fields)
    {
        await _businessDataService.UpdateBusinessDataAsync(id, fields);
        return Success(true);
    }

    /// <summary>
    /// Delete a single business data record
    /// </summary>
    /// <param name="id">Business data record ID</param>
    /// <returns>Whether deletion was successful</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Delete(long id)
    {
        await _businessDataService.DeleteBusinessDataAsync(id);
        return Success(true);
    }

    /// <summary>
    /// Batch delete multiple business data records at once
    /// </summary>
    /// <param name="ids">List of business data record IDs to delete</param>
    /// <returns>Whether all deletions were successful</returns>
    [HttpDelete("batch")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> BatchDelete([FromBody] List<long> ids)
    {
        await _businessDataService.BatchDeleteBusinessDataAsync(ids);
        return Success(true);
    }
}
