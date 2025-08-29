using SqlSugar;
using static FlowFlex.Infrastructure.SqlSugarFunctionExtensions;

namespace FlowFlex.Infrastructure;


public class SqlFuncExternalHelper
{
    /// <summary>
    /// Sanitize LIKE value to prevent SQL injection
    /// </summary>
    private static string SanitizeLikeValue(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Escape special LIKE characters and prevent SQL injection
        return input
            .Replace("'", "''")           // Escape single quotes
            .Replace("%", "\\%")          // Escape LIKE wildcard
            .Replace("_", "\\_")          // Escape LIKE single character wildcard
            .Replace("\\", "\\\\");       // Escape backslash
    }

    public static List<SqlFuncExternal> GetSqlFuncs()
    {
        var iLike = new SqlFuncExternal()
        {
            UniqueMethodName = nameof(ILike),
            MethodValue = (expInfo, dbType, expContext) =>
            {
                if (dbType == DbType.PostgreSQL)
                {
                    var value = SanitizeLikeValue(expInfo.Args[1].MemberValue?.ToString());
                    if (!string.IsNullOrEmpty(value))
                    {
                        var paramName = $"@p{expContext.Index}";
                        expContext.Parameters.Add(new SugarParameter(paramName, $"%{value}%"));
                        return $" {expInfo.Args[0].MemberName} ILIKE {paramName} ";
                    }
                    else
                        return $" {expInfo.Args[0].MemberName} ILIKE '%%' ";
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
                    var value = SanitizeLikeValue(expInfo.Args[1].MemberValue?.ToString());
                    var type = expInfo.Args.Count == 3 ? expInfo.Args[2].MemberValue : string.Empty;
                    if (!string.IsNullOrEmpty(value))
                    {
                        var paramName = $"@p{expContext.Index}";
                        expContext.Parameters.Add(new SugarParameter(paramName, $"%{value}%"));
                        return $" {expInfo.Args[0].MemberName}::text ILIKE {paramName} ";
                    }
                    else
                        return $" {expInfo.Args[0].MemberName}::text ILIKE '%%' ";
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
