using SqlSugar;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.SqlSugarDB;

namespace FlowFlex.SqlSugarDB.Implements.OW;

/// <summary>
/// Message repository implementation
/// </summary>
public class MessageRepository : BaseRepository<Message>, IMessageRepository, IScopedService
{
    public MessageRepository(ISqlSugarClient sqlSugarClient) : base(sqlSugarClient)
    {
    }

    /// <summary>
    /// Get messages by owner ID with pagination
    /// </summary>
    public async Task<(List<Message> items, int totalCount)> GetPagedByOwnerAsync(
        long ownerId,
        int pageIndex,
        int pageSize,
        string? folder = null,
        string? label = null,
        string? messageType = null,
        string? searchTerm = null,
        long? relatedEntityId = null,
        string sortField = "ReceivedDate",
        string sortDirection = "desc")
    {
        var query = db.Queryable<Message>()
            .Where(x => x.OwnerId == ownerId && x.IsValid);

        // Apply folder filter
        if (!string.IsNullOrEmpty(folder))
        {
            if (folder == "Starred")
            {
                query = query.Where(x => x.IsStarred);
            }
            else
            {
                query = query.Where(x => x.Folder == folder);
            }
        }

        // Label filter will be applied in memory after fetching results
        // Note: Due to SqlSugar's limited JSONB support, label filtering is done in memory
        var filterByLabel = !string.IsNullOrEmpty(label);
        
        // DEBUG: Log label filter status
        Console.WriteLine($"[MessageRepository] Label filter: {label}, filterByLabel: {filterByLabel}");

        // Apply message type filter
        if (!string.IsNullOrEmpty(messageType))
        {
            query = query.Where(x => x.MessageType == messageType);
        }

        // Apply search term filter
        if (!string.IsNullOrEmpty(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(x => x.Subject.ToLower().Contains(term) || x.Body.ToLower().Contains(term));
        }

        // Apply related entity filter
        if (relatedEntityId.HasValue)
        {
            query = query.Where(x => x.RelatedEntityId == relatedEntityId.Value);
        }

        // Apply sorting
        query = ApplySorting(query, sortField, sortDirection);

        // Get total count
        var totalCount = await query.CountAsync();

        // If filtering by label, we need to fetch more items and filter in memory
        // This is a workaround for SqlSugar's limited JSONB support in PostgreSQL
        if (filterByLabel)
        {
            // Fetch all matching items (without pagination) for label filtering
            var allItems = await query.ToListAsync();
            
            // Filter by label in memory
            var searchLabel = $"\"{label}\"";
            var filteredItems = allItems
                .Where(x => !string.IsNullOrEmpty(x.Labels) && x.Labels.Contains(searchLabel))
                .ToList();
            
            var filteredTotalCount = filteredItems.Count;
            var pagedItems = filteredItems
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            return (pagedItems, filteredTotalCount);
        }

        // Get paged items (normal path without label filter)
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    private ISugarQueryable<Message> ApplySorting(ISugarQueryable<Message> query, string sortField, string sortDirection)
    {
        var isAsc = sortDirection.ToLower() == "asc";
        return sortField.ToLower() switch
        {
            "sentdate" => isAsc ? query.OrderBy(x => x.SentDate) : query.OrderByDescending(x => x.SentDate),
            "subject" => isAsc ? query.OrderBy(x => x.Subject) : query.OrderByDescending(x => x.Subject),
            "sendername" => isAsc ? query.OrderBy(x => x.SenderName) : query.OrderByDescending(x => x.SenderName),
            _ => isAsc ? query.OrderBy(x => x.ReceivedDate) : query.OrderByDescending(x => x.ReceivedDate)
        };
    }

    /// <summary>
    /// Get messages by folder
    /// </summary>
    public async Task<List<Message>> GetByFolderAsync(long ownerId, string folder)
    {
        return await db.Queryable<Message>()
            .Where(x => x.OwnerId == ownerId && x.Folder == folder && x.IsValid)
            .OrderByDescending(x => x.ReceivedDate)
            .ToListAsync();
    }

    /// <summary>
    /// Get starred messages
    /// </summary>
    public async Task<List<Message>> GetStarredAsync(long ownerId)
    {
        return await db.Queryable<Message>()
            .Where(x => x.OwnerId == ownerId && x.IsStarred && x.IsValid)
            .OrderByDescending(x => x.ReceivedDate)
            .ToListAsync();
    }

    /// <summary>
    /// Get unread messages count
    /// </summary>
    public async Task<int> GetUnreadCountAsync(long ownerId, string? folder = null)
    {
        var query = db.Queryable<Message>()
            .Where(x => x.OwnerId == ownerId && !x.IsRead && x.IsValid);

        if (!string.IsNullOrEmpty(folder))
        {
            query = query.Where(x => x.Folder == folder);
        }

        return await query.CountAsync();
    }

    /// <summary>
    /// Get folder statistics
    /// </summary>
    public async Task<Dictionary<string, (int total, int unread, int internalCount, int emailCount, int portalCount)>> GetFolderStatsAsync(long ownerId)
    {
        var folders = new[] { "Inbox", "Sent", "Archive", "Trash", "Drafts" };
        var result = new Dictionary<string, (int total, int unread, int internalCount, int emailCount, int portalCount)>();

        foreach (var folder in folders)
        {
            var query = db.Queryable<Message>()
                .Where(x => x.OwnerId == ownerId && x.Folder == folder && x.IsValid);

            var total = await query.CountAsync();
            var unread = await query.Where(x => !x.IsRead).CountAsync();
            var internalCount = await query.Where(x => x.MessageType == "Internal").CountAsync();
            var emailCount = await query.Where(x => x.MessageType == "Email").CountAsync();
            var portalCount = await query.Where(x => x.MessageType == "Portal").CountAsync();

            result[folder] = (total, unread, internalCount, emailCount, portalCount);
        }

        // Add Starred folder (virtual folder based on IsStarred flag)
        var starredQuery = db.Queryable<Message>()
            .Where(x => x.OwnerId == ownerId && x.IsStarred && x.IsValid);

        var starredTotal = await starredQuery.CountAsync();
        var starredUnread = await starredQuery.Where(x => !x.IsRead).CountAsync();
        var starredInternal = await starredQuery.Where(x => x.MessageType == "Internal").CountAsync();
        var starredEmail = await starredQuery.Where(x => x.MessageType == "Email").CountAsync();
        var starredPortal = await starredQuery.Where(x => x.MessageType == "Portal").CountAsync();

        result["Starred"] = (starredTotal, starredUnread, starredInternal, starredEmail, starredPortal);

        return result;
    }

    /// <summary>
    /// Mark message as read
    /// </summary>
    public async Task<bool> MarkAsReadAsync(long id)
    {
        return await db.Updateable<Message>()
            .SetColumns(x => x.IsRead == true)
            .SetColumns(x => x.ModifyDate == DateTimeOffset.UtcNow)
            .Where(x => x.Id == id && x.IsValid)
            .ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// Mark message as unread
    /// </summary>
    public async Task<bool> MarkAsUnreadAsync(long id)
    {
        return await db.Updateable<Message>()
            .SetColumns(x => x.IsRead == false)
            .SetColumns(x => x.ModifyDate == DateTimeOffset.UtcNow)
            .Where(x => x.Id == id && x.IsValid)
            .ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// Star a message
    /// </summary>
    public async Task<bool> StarAsync(long id)
    {
        return await db.Updateable<Message>()
            .SetColumns(x => x.IsStarred == true)
            .SetColumns(x => x.ModifyDate == DateTimeOffset.UtcNow)
            .Where(x => x.Id == id && x.IsValid)
            .ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// Unstar a message
    /// </summary>
    public async Task<bool> UnstarAsync(long id)
    {
        return await db.Updateable<Message>()
            .SetColumns(x => x.IsStarred == false)
            .SetColumns(x => x.ModifyDate == DateTimeOffset.UtcNow)
            .Where(x => x.Id == id && x.IsValid)
            .ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// Move message to folder
    /// </summary>
    public async Task<bool> MoveToFolderAsync(long id, string folder, string? originalFolder = null)
    {
        var updateable = db.Updateable<Message>()
            .SetColumns(x => x.Folder == folder)
            .SetColumns(x => x.ModifyDate == DateTimeOffset.UtcNow);

        if (!string.IsNullOrEmpty(originalFolder))
        {
            updateable = updateable.SetColumns(x => x.OriginalFolder == originalFolder);
        }

        return await updateable
            .Where(x => x.Id == id && x.IsValid)
            .ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// Get message by external message ID
    /// </summary>
    public async Task<Message?> GetByExternalMessageIdAsync(string externalMessageId, long ownerId)
    {
        return await db.Queryable<Message>()
            .Where(x => x.ExternalMessageId == externalMessageId && x.OwnerId == ownerId && x.IsValid)
            .FirstAsync();
    }

    /// <summary>
    /// Get messages by conversation ID
    /// </summary>
    public async Task<List<Message>> GetByConversationIdAsync(string conversationId, long ownerId)
    {
        return await db.Queryable<Message>()
            .Where(x => x.ConversationId == conversationId && x.OwnerId == ownerId && x.IsValid)
            .OrderBy(x => x.ReceivedDate)
            .ToListAsync();
    }

    /// <summary>
    /// Get messages by related entity
    /// </summary>
    public async Task<List<Message>> GetByRelatedEntityAsync(string entityType, long entityId, long ownerId)
    {
        return await db.Queryable<Message>()
            .Where(x => x.RelatedEntityType == entityType && x.RelatedEntityId == entityId && x.OwnerId == ownerId && x.IsValid)
            .OrderByDescending(x => x.ReceivedDate)
            .ToListAsync();
    }

    /// <summary>
    /// Search messages by keyword
    /// </summary>
    public async Task<List<Message>> SearchAsync(long ownerId, string keyword, string? folder = null)
    {
        var term = keyword.ToLower();
        var query = db.Queryable<Message>()
            .Where(x => x.OwnerId == ownerId && x.IsValid)
            .Where(x => x.Subject.ToLower().Contains(term) || x.Body.ToLower().Contains(term));

        if (!string.IsNullOrEmpty(folder))
        {
            query = query.Where(x => x.Folder == folder);
        }

        return await query.OrderByDescending(x => x.ReceivedDate).ToListAsync();
    }

    /// <summary>
    /// Get draft messages
    /// </summary>
    public async Task<List<Message>> GetDraftsAsync(long ownerId)
    {
        return await db.Queryable<Message>()
            .Where(x => x.OwnerId == ownerId && x.IsDraft && x.IsValid)
            .OrderByDescending(x => x.ModifyDate)
            .ToListAsync();
    }

    /// <summary>
    /// Batch update messages folder
    /// </summary>
    public async Task<bool> BatchMoveToFolderAsync(List<long> ids, string folder)
    {
        return await db.Updateable<Message>()
            .SetColumns(x => x.Folder == folder)
            .SetColumns(x => x.ModifyDate == DateTimeOffset.UtcNow)
            .Where(x => ids.Contains(x.Id) && x.IsValid)
            .ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// Batch mark messages as read
    /// </summary>
    public async Task<bool> BatchMarkAsReadAsync(List<long> ids)
    {
        return await db.Updateable<Message>()
            .SetColumns(x => x.IsRead == true)
            .SetColumns(x => x.ModifyDate == DateTimeOffset.UtcNow)
            .Where(x => ids.Contains(x.Id) && x.IsValid)
            .ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// Permanently delete message
    /// </summary>
    public async Task<bool> PermanentDeleteAsync(long id)
    {
        return await db.Deleteable<Message>()
            .Where(x => x.Id == id)
            .ExecuteCommandAsync() > 0;
    }
}
