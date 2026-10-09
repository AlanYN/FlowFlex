using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.DocumentSnapshot;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.IServices.OW;

/// <summary>
/// Document operation log service interface
/// </summary>
public interface IDocumentOperationLogService : IScopedService
{
    /// <summary>
    /// Add an operation log entry; returns the new log ID
    /// </summary>
    Task<string> AddLogAsync(DocumentOperationLogInputDto input);

    /// <summary>
    /// Query operation logs by unitId with optional filters
    /// </summary>
    Task<PagedResult<DocumentOperationLogOutputDto>> GetLogsByUnitIdAsync(
        string unitId,
        DateTime? from = null,
        DateTime? to = null,
        string userId = null,
        string keyword = null,
        int pageIndex = 1,
        int pageSize = 50);

    /// <summary>
    /// Get the current max revision number for a document
    /// </summary>
    Task<int> GetCurrentRevisionAsync(string unitId);
}
