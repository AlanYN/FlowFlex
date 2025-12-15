using SqlSugar;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.SqlSugarDB;

namespace FlowFlex.SqlSugarDB.Implements.OW;

/// <summary>
/// Email Binding Repository Implementation
/// </summary>
public class EmailBindingRepository : BaseRepository<EmailBinding>, IEmailBindingRepository, IScopedService
{
    public EmailBindingRepository(ISqlSugarClient sqlSugarClient) : base(sqlSugarClient)
    {
    }

    /// <summary>
    /// Get binding by ID (nullable version)
    /// </summary>
    public async Task<EmailBinding?> GetByIdNullableAsync(long id)
    {
        return await db.Queryable<EmailBinding>()
            .Where(x => x.Id == id && x.IsValid)
            .FirstAsync();
    }

    /// <summary>
    /// Get binding by user ID
    /// </summary>
    public async Task<EmailBinding?> GetByUserIdAsync(long userId)
    {
        return await db.Queryable<EmailBinding>()
            .Where(x => x.UserId == userId && x.IsValid)
            .FirstAsync();
    }

    /// <summary>
    /// Get binding by user ID and provider
    /// </summary>
    public async Task<EmailBinding?> GetByUserIdAndProviderAsync(long userId, string provider)
    {
        return await db.Queryable<EmailBinding>()
            .Where(x => x.UserId == userId && x.Provider == provider && x.IsValid)
            .FirstAsync();
    }

    /// <summary>
    /// Get all bindings for a user
    /// </summary>
    public async Task<List<EmailBinding>> GetAllByUserIdAsync(long userId)
    {
        return await db.Queryable<EmailBinding>()
            .Where(x => x.UserId == userId && x.IsValid)
            .ToListAsync();
    }

    /// <summary>
    /// Check if user has active binding
    /// </summary>
    public async Task<bool> HasActiveBindingAsync(long userId, string provider = "Outlook")
    {
        return await db.Queryable<EmailBinding>()
            .Where(x => x.UserId == userId && x.Provider == provider && x.IsValid && x.SyncStatus == "Active")
            .AnyAsync();
    }

    /// <summary>
    /// Update token information
    /// </summary>
    public async Task<bool> UpdateTokenAsync(long id, string accessToken, string refreshToken, DateTimeOffset expireTime)
    {
        return await db.Updateable<EmailBinding>()
            .SetColumns(x => new EmailBinding
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenExpireTime = expireTime,
                SyncStatus = "Active",
                LastSyncError = null,
                ModifyDate = DateTimeOffset.UtcNow
            })
            .Where(x => x.Id == id)
            .ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// Update sync status
    /// </summary>
    public async Task<bool> UpdateSyncStatusAsync(long id, string status, string? errorMessage = null)
    {
        return await db.Updateable<EmailBinding>()
            .SetColumns(x => new EmailBinding
            {
                SyncStatus = status,
                LastSyncError = errorMessage,
                ModifyDate = DateTimeOffset.UtcNow
            })
            .Where(x => x.Id == id)
            .ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// Update last sync time
    /// </summary>
    public async Task<bool> UpdateLastSyncTimeAsync(long id)
    {
        return await db.Updateable<EmailBinding>()
            .SetColumns(x => new EmailBinding
            {
                LastSyncTime = DateTimeOffset.UtcNow,
                ModifyDate = DateTimeOffset.UtcNow
            })
            .Where(x => x.Id == id)
            .ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// Delete binding (soft delete)
    /// </summary>
    public async Task<bool> DeleteAsync(long id)
    {
        return await db.Updateable<EmailBinding>()
            .SetColumns(x => new EmailBinding
            {
                IsValid = false,
                ModifyDate = DateTimeOffset.UtcNow
            })
            .Where(x => x.Id == id)
            .ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// Get bindings that need sync (for background job)
    /// </summary>
    public async Task<List<EmailBinding>> GetBindingsNeedingSyncAsync()
    {
        var now = DateTime.UtcNow;
        return await db.Queryable<EmailBinding>()
            .Where(x => x.IsValid 
                && x.AutoSyncEnabled 
                && x.SyncStatus == "Active"
                && (x.LastSyncTime == null || 
                    SqlFunc.DateDiff(DateType.Minute, x.LastSyncTime.Value.DateTime, now) >= x.SyncIntervalMinutes))
            .ToListAsync();
    }

    /// <summary>
    /// Insert a new email binding
    /// </summary>
    public async Task<bool> InsertAsync(EmailBinding binding)
    {
        return await db.Insertable(binding).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// Update an existing email binding
    /// </summary>
    public async Task<bool> UpdateAsync(EmailBinding binding)
    {
        return await db.Updateable(binding).ExecuteCommandAsync() > 0;
    }
}
