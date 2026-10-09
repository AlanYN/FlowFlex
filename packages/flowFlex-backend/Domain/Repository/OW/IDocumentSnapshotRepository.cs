using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// Document snapshot repository interface
    /// </summary>
    public interface IDocumentSnapshotRepository : IBaseRepository<DocumentSnapshot>
    {
        /// <summary>
        /// Get document snapshot by unitId (tenant-scoped)
        /// </summary>
        Task<DocumentSnapshot> GetByUnitIdAsync(string unitId);

        /// <summary>
        /// Get documents associated with a specific business entity
        /// </summary>
        Task<List<DocumentSnapshot>> GetByEntityAsync(string entityType, long entityId);

        /// <summary>
        /// Upsert document snapshot by unitId (INSERT if not exists, UPDATE if exists)
        /// </summary>
        Task<bool> UpsertByUnitIdAsync(DocumentSnapshot entity);

        /// <summary>
        /// Get paged document list with optional filters
        /// </summary>
        Task<(List<DocumentSnapshot> items, int totalCount)> GetPagedAsync(
            int pageIndex,
            int pageSize,
            string keyword = null,
            string docType = null,
            string sortBy = "modify_date",
            string sortOrder = "desc");
    }
}
