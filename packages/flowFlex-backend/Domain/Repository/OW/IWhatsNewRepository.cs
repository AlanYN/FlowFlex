using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// What's New repository interface
    /// </summary>
    public interface IWhatsNewRepository : IBaseRepository<WhatsNew>
    {
        /// <summary>
        /// Get Published entries ordered by publish_time DESC (panel use)
        /// </summary>
        Task<List<WhatsNew>> GetPublishedListAsync(int limit = 10);

        /// <summary>
        /// Get admin list with read counts; optionally filter by status
        /// </summary>
        Task<List<WhatsNewAdminItemProjection>> GetAdminListAsync(int? statusFilter = null);

        /// <summary>
        /// Get counts of Published and Draft entries
        /// </summary>
        Task<(int publishedCount, int draftCount)> GetStatusCountsAsync();
    }

    /// <summary>
    /// Admin list projection including read count per entry
    /// </summary>
    public class WhatsNewAdminItemProjection
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Category { get; set; }
        public int Status { get; set; }
        public DateTimeOffset? PublishTime { get; set; }
        public int ReadCount { get; set; }
    }
}
