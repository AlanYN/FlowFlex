using System.Text;
using System.Text.RegularExpressions;

namespace Infrastructure.CodeGenerator;

public static partial class AutoCodeExtension
{
    /// <summary>
    /// 将字符串中的空格符(包含重复的空格符)替换成指定的分隔符
    /// </summary>
    /// <returns></returns>
    public static string ReplaceRepeatWhiteSpace(this string originalString, string replacement)
    {
        return MergeWhiteSpace().Replace(originalString, replacement).Trim();
    }

    /// <summary>
    /// 将字符串中特殊字符替换成指定的分隔符
    /// </summary>
    /// <param name="originalString"></param>
    /// <param name="excludeChars"></param>
    /// <returns></returns>
    public static string ReplaceSpecialCharacter(this string originalString, string replacement, params char[] excludeChars)
    {
        string excludeStr = new(excludeChars);

        StringBuilder allowCharPattern = new();

        allowCharPattern
            .Append(@"[^a-zA-Z0-9\s")
            .Append(excludeStr)
            .Append(']');

        return Regex.Replace(originalString, allowCharPattern.ToString(), replacement).Trim();
    }

    /// <summary>
    /// 移除指定的特殊字符
    /// </summary>
    /// <param name="originalString"></param>
    /// <param name="chars"></param>
    /// <returns></returns>
    public static string RemoveChars(this string originalString, params char[] chars)
    {
        foreach (var c in chars)
        {
            originalString = originalString.Replace(c.ToString(), string.Empty);
        }
        return originalString;
    }

    /// <summary>
    /// 合并多个空格
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"\s+")]
    public static partial Regex MergeWhiteSpace();
}