#nullable enable

using System.Text.RegularExpressions;

namespace FlowFlex.Application.Services.OW;

/// <summary>
/// 静态工具类，从 Internal Note content 中解析 @mention 标记
/// 支持格式：{{mention:user:email:username:displayName}} 和 {{mention:email:address}}
/// 向后兼容旧格式：[~username] 或 [~email@domain.com]
/// </summary>
public static partial class MentionParser
{
    /// <summary>
    /// 从 content 中提取所有 mention 标记，自动去重
    /// 同时支持新旧两种格式
    /// </summary>
    /// <param name="content">备注内容</param>
    /// <returns>去重后的 MentionInfo 列表</returns>
    public static List<MentionInfo> ParseMentions(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        var results = new List<MentionInfo>();

        // 1. Parse new format: {{mention:user:...}} and {{mention:email:...}}
        var newMatches = MentionRegex().Matches(content);
        foreach (Match m in newMatches)
        {
            var mentionType = m.Groups[1].Value;
            var payload = m.Groups[2].Value;

            if (mentionType == "user")
            {
                var parts = payload.Split(':', 3);
                results.Add(new MentionInfo
                {
                    MentionType = "user",
                    Email = parts.Length > 0 ? parts[0] : string.Empty,
                    Username = parts.Length > 1 ? parts[1] : string.Empty,
                    Value = parts.Length > 2 ? parts[2] : string.Empty
                });
            }
            else
            {
                results.Add(new MentionInfo
                {
                    MentionType = "email",
                    Email = payload,
                    Username = string.Empty,
                    Value = payload
                });
            }
        }

        // 2. Parse legacy format: [~value] (only if no new format matches found, to avoid double-parsing)
        if (results.Count == 0)
        {
            var legacyMatches = LegacyMentionRegex().Matches(content);
            foreach (Match m in legacyMatches)
            {
                var value = m.Groups[1].Value;
                var isEmail = EmailCheckRegex().IsMatch(value);

                results.Add(new MentionInfo
                {
                    MentionType = isEmail ? "email" : "user",
                    Email = isEmail ? value : string.Empty,
                    Username = isEmail ? string.Empty : value,
                    Value = value
                });
            }
        }

        return results
            .GroupBy(m => m.Key)
            .Select(g => g.First())
            .ToList();
    }

    /// <summary>
    /// 计算新增的 mentions（current - previous），用于编辑场景的增量通知
    /// </summary>
    public static List<MentionInfo> GetNewMentions(
        List<MentionInfo> currentMentions,
        List<MentionInfo> previousMentions)
    {
        if (currentMentions is null || currentMentions.Count == 0)
        {
            return [];
        }

        if (previousMentions is null || previousMentions.Count == 0)
        {
            return currentMentions;
        }

        var previousKeys = previousMentions.Select(m => m.Key).ToHashSet();

        return currentMentions
            .Where(m => !previousKeys.Contains(m.Key))
            .ToList();
    }

    /// <summary>
    /// 将 content 中的 mention 标记转为可读显示文本
    /// 新格式：{{mention:user:email:username:displayName}} → @displayName
    /// 新格式：{{mention:email:address}} → @address
    /// 旧格式：[~value] → @value
    /// </summary>
    public static string RenderForDisplay(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        // Replace new format
        var result = MentionRegex().Replace(content, m =>
        {
            var mentionType = m.Groups[1].Value;
            var payload = m.Groups[2].Value;

            if (mentionType == "user")
            {
                var parts = payload.Split(':', 3);
                var displayName = parts.Length > 2 ? parts[2] : (parts.Length > 1 ? parts[1] : payload);
                return $"@{displayName}";
            }
            else
            {
                return $"@{payload}";
            }
        });

        // Replace legacy format
        result = LegacyMentionRegex().Replace(result, m => $"@{m.Groups[1].Value}");

        return result;
    }

    /// <summary>New format regex: {{mention:(user|email):payload}}</summary>
    [GeneratedRegex(@"\{\{mention:(user|email):([^}]+?)\}\}")]
    private static partial Regex MentionRegex();

    /// <summary>Legacy format regex: [~value]</summary>
    [GeneratedRegex(@"\[~([^\]]+)\]")]
    private static partial Regex LegacyMentionRegex();

    /// <summary>Email check regex</summary>
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailCheckRegex();
}

/// <summary>
/// Mention 信息
/// </summary>
public class MentionInfo
{
    /// <summary>Mention 类型："user" 或 "email"</summary>
    public string MentionType { get; set; } = string.Empty;

    /// <summary>邮箱地址（user 类型和 email 类型都有值）</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>用户名（仅 user 类型有值）</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>显示值：user 类型为 displayName，email 类型为邮箱地址</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>是否为外部邮箱</summary>
    public bool IsExternal => MentionType == "email";

    /// <summary>唯一标识（用于去重比较）：统一用邮箱小写</summary>
    public string Key => !string.IsNullOrEmpty(Email)
        ? Email.ToLowerInvariant()
        : Value.ToLowerInvariant();
}
