namespace FlowFlex.Domain.Shared.Extensions;

public static class SqlSugarExtensions
{
    /// <summary>
    /// PostgreSQL fuzzy query method placeholder
    /// In actual use, SqlSugar's custom functions should be configured
    /// </summary>
    /// <param name="field">Field</param>
    /// <param name="value">Value</param>
    /// <returns>Placeholder return value</returns>
    public static bool ILike(string field, string value)
    {
        // This is just a placeholder method, actual SQL generation is handled by SqlSugar
        throw new NotSupportedException("Can only be used in expressions");
    }
} 
