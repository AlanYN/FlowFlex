using FlowFlex.Application.Contracts.Dtos.OW.WhatsNew;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW;

/// <summary>
/// What's New service interface.
/// Exposes user-facing query/read methods and admin-facing CRUD methods.
/// Registered as a scoped service via <see cref="IScopedService"/>.
/// </summary>
public interface IWhatsNewService : IScopedService
{
    #region User-facing

    /// <summary>
    /// Returns the number of unread published updates for the current user.
    /// Redis cache (TTL 10 min) is checked first; DB is queried on a cache miss.
    /// </summary>
    Task<int> GetUnreadCountAsync();

    /// <summary>
    /// Returns the What's New panel payload: up to 10 most recent published updates
    /// (sorted by publish_time DESC) with per-item isRead flags, plus the total unread count.
    /// </summary>
    Task<WhatsNewPanelResponseDto> GetPanelAsync();

    /// <summary>
    /// Returns full details for a single What's New entry, including the rich-text content field.
    /// Throws <c>CRMException(NotFound)</c> when the entry does not exist.
    /// </summary>
    Task<WhatsNewDetailDto> GetDetailAsync(long id);

    /// <summary>
    /// Idempotently marks the specified entry as read for the current user
    /// and invalidates the Redis unread-count cache.
    /// Uses INSERT … ON CONFLICT DO NOTHING under the hood.
    /// </summary>
    Task MarkReadAsync(long id);

    /// <summary>
    /// Marks all published entries as read for the current user
    /// and invalidates the Redis unread-count cache.
    /// </summary>
    Task MarkAllReadAsync();

    #endregion

    #region Admin-facing

    /// <summary>
    /// Returns the full admin list (all is_valid = true entries) with per-item read counts
    /// and aggregate published/draft statistics.
    /// Supports optional status filter (0 = Draft, 1 = Published).
    /// </summary>
    Task<WhatsNewAdminListResponseDto> GetAdminListAsync(int? status = null);

    /// <summary>
    /// Creates a new What's New entry.
    /// HTML content is XSS-filtered before persistence.
    /// When <c>status = 1</c>, <c>publish_time</c> is set to the current UTC time.
    /// Returns the new entry's snowflake ID.
    /// </summary>
    Task<long> CreateAsync(CreateWhatsNewRequest request);

    /// <summary>
    /// Updates an existing What's New entry.
    /// HTML content is XSS-filtered before persistence.
    /// When transitioning from Draft (0) to Published (1), <c>publish_time</c> is set to now.
    /// </summary>
    Task<bool> UpdateAsync(long id, UpdateWhatsNewRequest request);

    /// <summary>
    /// Soft-deletes the entry (<c>is_valid = false</c>).
    /// Read-status history in <c>ff_whats_new_read_status</c> is preserved.
    /// Throws <c>CRMException(NotFound)</c> when the entry does not exist.
    /// </summary>
    Task<bool> DeleteAsync(long id);

    #endregion
}
