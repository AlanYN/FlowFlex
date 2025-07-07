using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FlowFlex.Application.Contracts.Dtos.OW.Checklist;

/// <summary>
/// Checklist task completion input DTO
/// </summary>
public class ChecklistTaskCompletionInputDto
{
    /// <summary>
    /// Onboarding ID
    /// </summary>
    
    public long OnboardingId { get; set; }

    /// <summary>
    /// Lead ID
    /// </summary>
    [StringLength(100)]
    public string? LeadId { get; set; }

    /// <summary>
    /// Checklist ID
    /// </summary>
    
    public long ChecklistId { get; set; }

    /// <summary>
    /// Task ID
    /// </summary>
    
    public long TaskId { get; set; }

    /// <summary>
    /// 是否已完成
    /// </summary>
    public bool IsCompleted { get; set; } = false;

    /// <summary>
    /// 完成备注
    /// </summary>
    [StringLength(500)]
    public string CompletionNotes { get; set; } = string.Empty;

    // 支持字符串形式的ID输入，用于处理JavaScript大整数精度丢失问题

    /// <summary>
    /// Onboarding ID as string (for JavaScript large integer support)
    /// </summary>
    [JsonPropertyName("onboardingIdString")]
    public string? OnboardingIdString { get; set; }

    /// <summary>
    /// Checklist ID as string (for JavaScript large integer support)
    /// </summary>
    [JsonPropertyName("checklistIdString")]
    public string? ChecklistIdString { get; set; }

    /// <summary>
    /// Task ID as string (for JavaScript large integer support)
    /// </summary>
    [JsonPropertyName("taskIdString")]
    public string? TaskIdString { get; set; }

    /// <summary>
    /// 自动从字符串转换ID值
    /// </summary>
    public void ConvertStringIds()
    {
        if (!string.IsNullOrEmpty(OnboardingIdString) && long.TryParse(OnboardingIdString, out long onboardingId))
        {
            OnboardingId = onboardingId;
        }

        if (!string.IsNullOrEmpty(ChecklistIdString) && long.TryParse(ChecklistIdString, out long checklistId))
        {
            ChecklistId = checklistId;
        }

        if (!string.IsNullOrEmpty(TaskIdString) && long.TryParse(TaskIdString, out long taskId))
        {
            TaskId = taskId;
        }
    }
}