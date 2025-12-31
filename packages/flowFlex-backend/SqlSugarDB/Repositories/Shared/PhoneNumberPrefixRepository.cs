using FlowFlex.Domain.Entities.Shared;
using FlowFlex.Domain.Repository.Shared;
using FlowFlex.Domain.Shared;
using SqlSugar;

namespace FlowFlex.SqlSugarDB.Repositories.Shared;

/// <summary>
/// Phone number prefix repository implementation
/// </summary>
public class PhoneNumberPrefixRepository : IPhoneNumberPrefixRepository, IScopedService
{
    private readonly ISqlSugarClient _db;

    public PhoneNumberPrefixRepository(ISqlSugarClient sqlSugarClient)
    {
        _db = sqlSugarClient;
    }

    /// <summary>
    /// Get all phone number prefixes from master schema
    /// </summary>
    public async Task<List<PhoneNumberPrefix>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _db.Ado.CancellationToken = cancellationToken;

        // Query from master schema without tenant filter
        return await _db.Queryable<PhoneNumberPrefix>()
            .AS("master.phone_number_prefixes")
            .ToListAsync();
    }
}
