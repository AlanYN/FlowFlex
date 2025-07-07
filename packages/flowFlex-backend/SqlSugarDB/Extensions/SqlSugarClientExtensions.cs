using FlowFlex.Domain.Shared.Models.Models;
using SqlSugar;

namespace FlowFlex.SqlSugarDB.Extensions;

public static class SqlSugarClientExtensions
{
    public static ClearFilterScope CreateFilterScope(this ISqlSugarClient client)
    {
        client.QueryFilter.ClearAndBackup();
        return new ClearFilterScope(client);
    }
}

