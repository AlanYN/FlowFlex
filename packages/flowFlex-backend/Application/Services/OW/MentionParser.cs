#nullable enable

using System.Text.RegularExpressions;

namespace FlowFlex.Application.Services.OW;

/// <summary>
/// 静态工具类，从 Internal Note content 中解析 @mention 标记
/// 支持格式：{{mention:user:email:username:displayName}} 和 {{mention:email:address}}
/// </summary>
public static partial class MentionParser
{
    /// <summary>
    /// 从 content 中提取所有 mention 标记，自动去重
    /// </summary>
    /// <param name="content">备注内容</param>
    /// <returns>去重后的 MentionInfo 列表</returns>
    public static List<MentionInfo> ParseMentions(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        var matches = MentionRegex().Matches(content);

        return matches
            .Select(m =>
            {
                var mentionType = m.Groups[1].Value; // "user" or "email"
                var payload = m.Groups[2].Value;     // rest of the content

                if (mentionType == "user")
                {
                    // payload format: email:username:displayName
                    // Split on first two colons to get email, username, displayName
                    var parts = payload.Split(':', 3);
                    return new MentionInfo
                    {
                        MentionType = "user",
                        Email = parts.Length > 0 ? parts[0] : string.Empty,
                        Username = parts.Length > 1 ? parts[1] : string.Empty,
                        Value = parts.Length > 2 ? parts[2] : string.Empty
                    };
                }
                else
                {
                    // email type: payload is the email address
                    return new MentionInfo
                    {
                        MentionType = "email",
                        Email = payload,
                        Username = string.Empty,
                        Value = payload
                    };
                }
            })
            .GroupBy(m => m.Key)
            .Select(g => g.First())
            .ToList();
    }

    /// <summary>
    /// 计算新增的 mentions（current - previous），用于编辑场景的增量通知
    /// </summary>
    /// <param name="currentMentions">当前内容解析出的 mentions</param>
    /// <param name="previousMentions">之前内容解析出的 mentions</param>
    /// <returns>新增的 MentionInfo 列表</returns>
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
    /// {{mention:user:email:username:displayName}} → @displayName
    /// {{mention:email:address}} → @address
    /// </summary>
    /// <param name="content">包含 mention 标记的原始内容</param>
    /// <returns>mention 标记被替换为可读文本的内容</returns>
    public static string RenderForDisplay(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        return MentionRegex().Replace(content, m =>
        {
            var mentionType = m.Groups[1].Value;
            var payload = m.Groups[2].Value;

            if (mentionType == "user")
            {
                // payload = "email:username:displayName"
                var parts = payload.Split(':', 3);
                var displayName = parts.Length > 2 ? parts[2] : (parts.Length > 1 ? parts[1] : payload);
                return $"@{displayName}";
            }
            else
            {
                // email type: payload is the email address
                return $"@{payload}";
            }
        });
    }

    [GeneratedRegex(@"\{\{mention:(user|email):([^}]+?)\}\}")]
    private static partial Regex MentionRegex();
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
    public string Key => Email.ToLowerInvariant();
}
