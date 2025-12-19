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
            else if (folder == "Archive")
            {
                query = query.Where(x => x.IsArchived);
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

        // Apply search term filter using PostgreSQL full-text search
        if (!string.IsNullOrEmpty(searchTerm))
        {
            // Use PostgreSQL full-text search for better performance
            // to_tsvector creates a text search vector, plainto_tsquery converts search term to query
            // The '||' operator combines subject and body for searching
            // 'simple' configuration is used for basic tokenization without language-specific stemming
            var sanitizedTerm = SanitizeSearchTerm(searchTerm);
            query = query.Where(x => 
                SqlFunc.MappingColumn<bool>($"to_tsvector('simple', coalesce(\"subject\", '') || ' ' || coalesce(\"body\", '')) @@ plainto_tsquery('simple', '{sanitizedTerm}')"));
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
        // Only real folders (Inbox, Sent, Trash, Drafts), Archive and Starred are virtual folders
        var folders = new[] { "Inbox", "Sent", "Trash", "Drafts" };
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

        // Add Archive folder (virtual folder based on IsArchived flag)
        var archivedQuery = db.Queryable<Message>()
            .Where(x => x.OwnerId == ownerId && x.IsArchived && x.IsValid);

        var archivedTotal = await archivedQuery.CountAsync();
        var archivedUnread = await archivedQuery.Where(x => !x.IsRead).CountAsync();
        var archivedInternal = await archivedQuery.Where(x => x.MessageType == "Internal").CountAsync();
        var archivedEmail = await archivedQuery.Where(x => x.MessageType == "Email").CountAsync();
        var archivedPortal = await archivedQuery.Where(x => x.MessageType == "Portal").CountAsync();

        result["Archive"] = (archivedTotal, archivedUnread, archivedInternal, archivedEmail, archivedPortal);

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
    /// Archive a message
    /// </summary>
    public async Task<bool> ArchiveAsync(long id)
    {
        return await db.Updateable<Message>()
            .SetColumns(x => x.IsArchived == true)
            .SetColumns(x => x.ModifyDate == DateTimeOffset.UtcNow)
            .Where(x => x.Id == id && x.IsValid)
            .ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// Unarchive a message
    /// </summary>
    public async Task<bool> UnarchiveAsync(long id)
    {
        return await db.Updateable<Message>()
            .SetColumns(x => x.IsArchived == false)
            .SetColumns(x => x.ModifyDate == DateTimeOffset.UtcNow)
            .Where(x => x.Id == id && x.IsValid)
            .ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// Get archived messages
    /// </summary>
    public async Task<List<Message>> GetArchivedAsync(long ownerId)
    {
        return await db.Queryable<Message>()
            .Where(x => x.OwnerId == ownerId && x.IsArchived && x.IsValid)
            .OrderByDescending(x => x.ReceivedDate)
            .ToListAsync();
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
    /// Get first message by conversation ID (without owner filter)
    /// </summary>
    public async Task<Message?> GetByConversationIdAsync(string conversationId)
    {
        return await db.Queryable<Message>()
            .Where(x => x.ConversationId == conversationId && x.IsValid)
            .FirstAsync();
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
    /// Search messages by keyword using PostgreSQL full-text search
    /// </summary>
    public async Task<List<Message>> SearchAsync(long ownerId, string keyword, string? folder = null)
    {
        var sanitizedTerm = SanitizeSearchTerm(keyword);
        var query = db.Queryable<Message>()
            .Where(x => x.OwnerId == ownerId && x.IsValid)
            .Where(x => SqlFunc.MappingColumn<bool>(
                $"to_tsvector('simple', coalesce(\"subject\", '') || ' ' || coalesce(\"body\", '')) @@ plainto_tsquery('simple', '{sanitizedTerm}')"));

        if (!string.IsNullOrEmpty(folder))
        {
            query = query.Where(x => x.Folder == folder);
        }

        return await query.OrderByDescending(x => x.ReceivedDate).ToListAsync();
    }

    /// <summary>
    /// Sanitize search term to prevent SQL injection
    /// </summary>
    private static string SanitizeSearchTerm(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return string.Empty;
        }

        // Remove or escape special characters that could cause issues
        // Replace single quotes with two single quotes (SQL escaping)
        var sanitized = searchTerm
            .Replace("'", "''")
            .Replace("\\", "\\\\")
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();

        // Limit length to prevent abuse
        if (sanitized.Length > 200)
        {
            sanitized = sanitized.Substring(0, 200);
        }

        return sanitized;
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

    /// <summary>
    /// Find a locally sent message by subject and sent time (for linking with Outlook sync)
    /// </summary>
    public async Task<Message?> FindLocalSentMessageAsync(long ownerId, string subject, DateTimeOffset sentTime, TimeSpan tolerance)
    {
        var minTime = sentTime.Add(-tolerance);
        var maxTime = sentTime.Add(tolerance);

        return await db.Queryable<Message>()
            .Where(x => x.OwnerId == ownerId 
                && x.IsValid 
                && x.Folder == "Sent"
                && x.MessageType == "Email"
                && x.Subject == subject
                && string.IsNullOrEmpty(x.ExternalMessageId)
                && x.SentDate >= minTime 
                && x.SentDate <= maxTime)
            .OrderByDescending(x => x.SentDate)
            .FirstAsync();
    }

    /// <summary>
    /// Get messages by folder that have ExternalMessageId (for sync deleted detection)
    /// </summary>
    public async Task<List<Message>> GetByFolderWithExternalIdAsync(long ownerId, string folder)
    {
        return await db.Queryable<Message>()
            .Where(x => x.OwnerId == ownerId 
                && x.IsValid 
                && x.Folder == folder
                && x.MessageType == "Email"
                && !string.IsNullOrEmpty(x.ExternalMessageId))
            .ToListAsync();
    }

    /// <summary>
    /// Delete all synced emails for a user (soft delete)
    /// Used when user unbinds their email account
    /// </summary>
    public async Task<int> DeleteSyncedEmailsByOwnerAsync(long ownerId)
    {
        return await db.Updateable<Message>()
            .SetColumns(x => new Message
            {
                IsValid = false,
                ModifyDate = DateTimeOffset.UtcNow
            })
            .Where(x => x.OwnerId == ownerId 
                && x.IsValid 
                && x.MessageType == "Email"
                && !string.IsNullOrEmpty(x.ExternalMessageId))
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// Get existing external message IDs for batch sync optimization
    /// </summary>
    public async Task<HashSet<string>> GetExistingExternalIdsAsync(List<string> externalIds, long ownerId)
    {
        if (externalIds == null || externalIds.Count == 0)
        {
            return new HashSet<string>();
        }

        var ids = await db.Queryable<Message>()
            .Where(x => x.OwnerId == ownerId && x.IsValid && externalIds.Contains(x.ExternalMessageId!))
            .Select(x => x.ExternalMessageId)
            .ToListAsync();

        return ids.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).ToHashSet();
    }
}
