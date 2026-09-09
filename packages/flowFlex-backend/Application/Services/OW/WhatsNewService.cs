using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.WhatsNew;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using Ganss.Xss;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Services.OW;

/// <summary>
/// What's New service implementation.
/// Handles user-facing query/read operations and admin-facing CRUD.
/// Redis cache (TTL 10 min) is used for unread counts.
/// </summary>
public class WhatsNewService : IWhatsNewService, IScopedService
{
    private readonly IWhatsNewRepository _whatsNewRepository;
    private readonly IWhatsNewReadStatusRepository _readStatusRepository;
    private readonly IDistributedCacheService _cacheService;
    private readonly UserContext _userContext;
    private readonly ILogger<WhatsNewService> _logger;

    public WhatsNewService(
        IWhatsNewRepository whatsNewRepository,
        IWhatsNewReadStatusRepository readStatusRepository,
        IDistributedCacheService cacheService,
        UserContext userContext,
        ILogger<WhatsNewService> logger)
    {
        _whatsNewRepository = whatsNewRepository ?? throw new ArgumentNullException(nameof(whatsNewRepository));
        _readStatusRepository = readStatusRepository ?? throw new ArgumentNullException(nameof(readStatusRepository));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region User-facing

    /// <inheritdoc />
    public async Task<int> GetUnreadCountAsync()
    {
        var cacheKey = BuildCacheKey();
        var cached = await _cacheService.GetAsync<string>(cacheKey);
        if (cached != null)
            return int.Parse(cached);

        // DB fallback: get all published IDs, then count those NOT in user's read_status
        var publishedItems = await _whatsNewRepository.GetPublishedListAsync(int.MaxValue);
        var publishedIds = publishedItems.Select(x => x.Id).ToList();

        int count;
        if (publishedIds.Count == 0)
        {
            count = 0;
        }
        else
        {
            if (!long.TryParse(_userContext.UserId, out var userId))
                userId = 0;

            count = await _readStatusRepository.GetUnreadCountAsync(userId, publishedIds);
        }

        await _cacheService.SetAsync<string>(cacheKey, count.ToString(), TimeSpan.FromMinutes(10));
        return count;
    }

    /// <inheritdoc />
    public async Task<WhatsNewPanelResponseDto> GetPanelAsync()
    {
        var items = await _whatsNewRepository.GetPublishedListAsync(10);
        var ids = items.Select(x => x.Id).ToList();

        HashSet<long> readIds = ids.Count > 0 && long.TryParse(_userContext.UserId, out var userIdForPanel)
            ? await _readStatusRepository.GetReadIdsAsync(userIdForPanel)
            : new HashSet<long>();

        var dtoItems = items.Select(item => new WhatsNewPanelItemDto
        {
            Id = item.Id,
            Title = item.Title,
            Summary = item.Summary,
            Category = item.Category,
            PublishTime = item.PublishTime,
            IsRead = readIds.Contains(item.Id)
        }).ToList();

        int unreadCount = dtoItems.Count(x => !x.IsRead);

        return new WhatsNewPanelResponseDto
        {
            Items = dtoItems,
            UnreadCount = unreadCount
        };
    }

    /// <inheritdoc />
    public async Task<WhatsNewDetailDto> GetDetailAsync(long id)
    {
        var entity = await _whatsNewRepository.GetByIdAsync(id)
            ?? throw new CRMException(ErrorCodeEnum.NotFound, "WhatsNew not found");

        bool isRead = false;
        if (long.TryParse(_userContext.UserId, out var userIdForDetail))
        {
            var readIds = await _readStatusRepository.GetReadIdsAsync(userIdForDetail);
            isRead = readIds.Contains(id);
        }

        return new WhatsNewDetailDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Summary = entity.Summary,
            Content = entity.Content,
            Category = entity.Category,
            PublishTime = entity.PublishTime,
            IsRead = isRead
        };
    }

    /// <inheritdoc />
    public async Task MarkReadAsync(long id)
    {
        if (!long.TryParse(_userContext.UserId, out var userId))
            userId = 0;

        await _readStatusRepository.MarkReadAsync(id, userId);
        await _cacheService.RemoveAsync(BuildCacheKey());
    }

    /// <inheritdoc />
    public async Task MarkAllReadAsync()
    {
        if (!long.TryParse(_userContext.UserId, out var userId))
            userId = 0;

        var publishedItems = await _whatsNewRepository.GetPublishedListAsync(int.MaxValue);
        var publishedIds = publishedItems.Select(x => x.Id).ToList();
        if (publishedIds.Count > 0)
        {
            await _readStatusRepository.MarkAllReadAsync(publishedIds, userId);
        }
        await _cacheService.RemoveAsync(BuildCacheKey());
    }

    #endregion

    #region Private helpers

    private string BuildCacheKey() =>
        $"whats-new:unread:{_userContext.UserId}";

    /// <summary>
    /// White-list HTML sanitizer to prevent XSS.
    /// Strips script tags, on* event handlers, and javascript: protocol URIs.
    /// </summary>
    private static string SanitizeHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return html ?? string.Empty;

        var sanitizer = new HtmlSanitizer();

        // Whitelist tags used by the Quill rich-text editor
        sanitizer.AllowedTags.Clear();
        foreach (var tag in new[]
        {
            "p", "br", "strong", "em", "u", "s",
            "h1", "h2", "h3", "h4", "h5", "h6",
            "ul", "ol", "li", "blockquote", "pre", "code",
            "a", "img", "span", "div",
            "table", "thead", "tbody", "tr", "th", "td"
        })
            sanitizer.AllowedTags.Add(tag);

        sanitizer.AllowedAttributes.Clear();
        foreach (var attr in new[]
        {
            "href", "src", "alt", "class", "style", "target", "rel",
            "data-row", "data-cell"
        })
            sanitizer.AllowedAttributes.Add(attr);

        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add("http");
        sanitizer.AllowedSchemes.Add("https");
        sanitizer.AllowedSchemes.Add("data");

        return sanitizer.Sanitize(html);
    }

    #endregion

    #region Admin-facing

    /// <inheritdoc />
    public async Task<WhatsNewAdminListResponseDto> GetAdminListAsync(int? status = null)
    {
        var projections = await _whatsNewRepository.GetAdminListAsync(status);
        var items = projections.Select(p => new WhatsNewAdminItemDto
        {
            Id = p.Id,
            Title = p.Title,
            Summary = p.Summary,
            Category = p.Category,
            Status = p.Status,
            PublishTime = p.PublishTime,
            ReadCount = p.ReadCount
        }).ToList();

        var (publishedCount, draftCount) = await _whatsNewRepository.GetStatusCountsAsync();

        return new WhatsNewAdminListResponseDto
        {
            Items = items,
            PublishedCount = publishedCount,
            DraftCount = draftCount
        };
    }

    /// <inheritdoc />
    public async Task<long> CreateAsync(CreateWhatsNewRequest request)
    {
        var entity = new WhatsNew
        {
            Title = request.Title,
            Summary = request.Summary,
            Content = SanitizeHtml(request.Content),
            Category = request.Category,
            Status = request.Status,
            PublishTime = request.Status == 1 ? DateTimeOffset.UtcNow : (DateTimeOffset?)null
        };
        await _whatsNewRepository.InsertAsync(entity);
        return entity.Id;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(long id, UpdateWhatsNewRequest request)
    {
        var entity = await _whatsNewRepository.GetByIdAsync(id)
            ?? throw new CRMException(ErrorCodeEnum.NotFound, "WhatsNew not found");

        entity.Title = request.Title;
        entity.Summary = request.Summary;
        entity.Content = SanitizeHtml(request.Content);
        entity.Category = request.Category;

        // Draft → Published: set publish time
        if (entity.Status != 1 && request.Status == 1)
        {
            entity.PublishTime = DateTimeOffset.UtcNow;
        }
        entity.Status = request.Status;

        return await _whatsNewRepository.UpdateAsync(entity);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(long id)
    {
        var entity = await _whatsNewRepository.GetByIdAsync(id)
            ?? throw new CRMException(ErrorCodeEnum.NotFound, "WhatsNew not found");

        entity.IsValid = false;
        return await _whatsNewRepository.UpdateAsync(entity);
    }

    #endregion

}
