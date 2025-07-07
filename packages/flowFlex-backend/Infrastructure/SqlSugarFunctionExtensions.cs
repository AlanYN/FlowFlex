using SqlSugar;
using static FlowFlex.Infrastructure.SqlSugarFunctionExtensions;

namespace FlowFlex.Infrastructure;


public class SqlFuncExternalHelper
{
    public static List<SqlFuncExternal> GetSqlFuncs()
    {
        var iLike = new SqlFuncExternal()
        {
            UniqueMethodName = nameof(ILike),
            MethodValue = (expInfo, dbType, expContext) =>
            {
                if (dbType == DbType.PostgreSQL)
                {
                    var value = expInfo.Args[1].MemberValue?.ToString()?.ToCheckField();
                    if (value != null)
                    {
                        return string.Format(" {0} ILike '%{1}%' ", $"{expInfo.Args[0].MemberName}", value);
                    }
                    else
                        return string.Format(" {0} ILike '%%' ", $"{expInfo.Args[0].MemberName}");
                }
                else
                    throw new NotImplementedException();
            }
        };

        var jsonLike = new SqlFuncExternal()
        {
            UniqueMethodName = nameof(JsonILike),
            MethodValue = (expInfo, dbType, expContext) =>
            {
                if (dbType == DbType.PostgreSQL)
                {
                    var value = expInfo.Args[1].MemberValue?.ToString()?.ToCheckField();
                    var type = expInfo.Args.Count == 3 ? expInfo.Args[2].MemberValue : string.Empty;
                    if (value != null)
                    {
                        return string.Format(" {0} ILike '%{1}%' ", $"{expInfo.Args[0].MemberName}::text", value);
                    }
                    else
                        return string.Format(" {0} ILike '%%' ", $"{expInfo.Args[0].MemberName}::text");
                }
                else
                    throw new NotImplementedException();
            }
        };

        return [iLike, jsonLike];
    }
}

public class SqlSugarFunctionExtensions
{
    /// <summary>
    /// PostgreSQL fuzzy query
    /// </summary>
    /// <param name="name">Field to perform fuzzy query on</param>
    /// <param name="value">Value for fuzzy query, no need to add percent signs</param>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public static bool ILike(string name, string value) =>
        throw new NotSupportedException("Can only be used in expressions");

    public static bool JsonILike(object name, string value) =>
        throw new NotSupportedException("Can only be used in expressions");
}
