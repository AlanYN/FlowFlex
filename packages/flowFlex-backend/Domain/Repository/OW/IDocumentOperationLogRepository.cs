using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// Document operation log repository interface
    /// </summary>
    public interface IDocumentOperationLogRepository : IBaseRepository<DocumentOperationLog>
    {
        /// <summary>
        /// Get operation logs by unitId with optional filters
        /// </summary>
        Task<(List<DocumentOperationLog> items, int totalCount)> GetLogsByUnitIdAsync(
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
        Task<int> GetRevisionAsync(string unitId);

        /// <summary>
        /// Get the most recent set-range-values log entry for a given unitId + subUnitId,
        /// used to extract old cell values when recording a new mutation.
        /// </summary>
        Task<DocumentOperationLog> GetLatestSetRangeLogAsync(string unitId, string subUnitId);
    }
}
