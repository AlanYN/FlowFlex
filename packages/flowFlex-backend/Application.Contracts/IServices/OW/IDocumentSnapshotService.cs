using System.Collections.Generic;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.DocumentSnapshot;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.IServices.OW;

/// <summary>
/// Document snapshot service interface
/// </summary>
public interface IDocumentSnapshotService : IScopedService
{
    /// <summary>
    /// Create a new empty document snapshot; returns the new unitId
    /// </summary>
    Task<string> CreateAsync(DocumentSnapshotInputDto input);

    /// <summary>
    /// Upsert (save/update) document snapshot by unitId
    /// </summary>
    Task<bool> SaveAsync(string unitId, DocumentSnapshotInputDto input);

    /// <summary>
    /// Get document snapshot by unitId
    /// </summary>
    Task<DocumentSnapshotOutputDto> GetByUnitIdAsync(string unitId);

    /// <summary>
    /// Get documents associated with a business entity
    /// </summary>
    Task<List<DocumentSnapshotOutputDto>> GetByEntityAsync(string entityType, long entityId);

    /// <summary>
    /// Soft-delete a document by numeric ID
    /// </summary>
    Task<bool> DeleteAsync(long id);

    /// <summary>
    /// Paged query with keyword / docType / sort filters
    /// </summary>
    Task<PagedResult<DocumentSnapshotOutputDto>> QueryAsync(DocumentSnapshotQueryRequest query);
}
