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
    /// Delete attachments by message ID
    /// </summary>
    public async Task<bool> DeleteByMessageIdAsync(long messageId)
    {
        return await db.Updateable<MessageAttachment>()
            .SetColumns(x => x.IsValid == false)
            .SetColumns(x => x.ModifyDate == DateTimeOffset.UtcNow)
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

        return await db.Updateable<MessageAttachment>()
            .SetColumns(x => x.MessageId == messageId)
            .SetColumns(x => x.ModifyDate == DateTimeOffset.UtcNow)
            .Where(x => attachmentIds.Contains(x.Id) && x.IsValid)
            .ExecuteCommandAsync() > 0;
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
}
