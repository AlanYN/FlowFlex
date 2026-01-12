using SqlSugar;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.SqlSugarDB;

namespace FlowFlex.SqlSugarDB.Implements.OW;

/// <summary>
/// Message Attachment repository implementation
/// </summary>
public class MessageAttachmentRepository : BaseRepository<MessageAttachment>, IMessageAttachmentRepository, IScopedService
{
    public MessageAttachmentRepository(ISqlSugarClient sqlSugarClient) : base(sqlSugarClient)
    {
    }

    /// <summary>
    /// Get attachments by message ID
    /// </summary>
    public async Task<List<MessageAttachment>> GetByMessageIdAsync(long messageId)
    {
        return await db.Queryable<MessageAttachment>()
            .Where(x => x.MessageId == messageId && x.IsValid)
            .OrderBy(x => x.CreateDate)
            .ToListAsync();
    }

    /// <summary>
    /// Get attachment by external attachment ID
    /// </summary>
    public async Task<MessageAttachment?> GetByExternalAttachmentIdAsync(string externalAttachmentId)
    {
        return await db.Queryable<MessageAttachment>()
            .Where(x => x.ExternalAttachmentId == externalAttachmentId && x.IsValid)
            .FirstAsync();
    }

    /// <summary>
    /// Delete attachments by message ID (soft delete)
    /// </summary>
    public async Task<bool> DeleteByMessageIdAsync(long messageId)
    {
        var now = DateTimeOffset.UtcNow;
        return await db.Updateable<MessageAttachment>()
            .SetColumns(it => it.IsValid, false)
            .SetColumns(it => it.ModifyDate, now)
            .Where(x => x.MessageId == messageId && x.IsValid)
            .ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// Associate attachments with message
    /// </summary>
    public async Task<bool> AssociateWithMessageAsync(List<long> attachmentIds, long messageId)
    {
        if (attachmentIds == null || !attachmentIds.Any())
            return true;

        var now = DateTimeOffset.UtcNow;
        // Update each attachment individually to ensure correct association
        var count = 0;
        foreach (var attachmentId in attachmentIds)
        {
            var result = await db.Updateable<MessageAttachment>()
                .SetColumns(it => it.MessageId, messageId)
                .SetColumns(it => it.ModifyDate, now)
                .Where(x => x.Id == attachmentId && x.IsValid)
                .ExecuteCommandAsync();
            count += result;
        }
        return count > 0;
    }

    /// <summary>
    /// Get unassociated attachments (temp attachments with MessageId = 0)
    /// </summary>
    public async Task<List<MessageAttachment>> GetUnassociatedAsync()
    {
        return await db.Queryable<MessageAttachment>()
            .Where(x => x.MessageId == 0 && x.IsValid)
            .ToListAsync();
    }

    /// <summary>
    /// Clean up old unassociated attachments
    /// </summary>
    public async Task<int> CleanupUnassociatedAsync(int olderThanHours = 24)
    {
        var cutoffTime = DateTimeOffset.UtcNow.AddHours(-olderThanHours);
        return await db.Deleteable<MessageAttachment>()
            .Where(x => x.MessageId == 0 && x.CreateDate < cutoffTime)
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// Get existing external attachment IDs for batch sync optimization
    /// </summary>
    public async Task<HashSet<string>> GetExistingExternalIdsAsync(List<string> externalIds)
    {
        if (externalIds == null || externalIds.Count == 0)
            return new HashSet<string>();

        var existingIds = await db.Queryable<MessageAttachment>()
            .Where(x => externalIds.Contains(x.ExternalAttachmentId!) && x.IsValid)
            .Select(x => x.ExternalAttachmentId)
            .ToListAsync();

        return existingIds.Where(id => id != null).Select(id => id!).ToHashSet();
    }

    /// <summary>
    /// Batch insert attachments
    /// </summary>
    public async Task<bool> InsertRangeAsync(List<MessageAttachment> attachments)
    {
        if (attachments == null || attachments.Count == 0)
            return true;

        return await db.Insertable(attachments).ExecuteCommandAsync() > 0;
    }
}
