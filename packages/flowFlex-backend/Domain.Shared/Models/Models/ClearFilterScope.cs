using SqlSugar;
using System;

namespace FlowFlex.Domain.Shared.Models.Models;

public class ClearFilterScope(ISqlSugarClient client) : IDisposable
{
    private readonly ISqlSugarClient _client = client;

    public event Action OnDispose;

    /// <summary>
    /// Restore filter state
    /// </summary>
    public void Dispose()
    {
        OnDispose?.Invoke();
        GC.SuppressFinalize(this);
        _client.QueryFilter.Restore();
    }
}
