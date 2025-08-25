using System;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// AI Prompt History Entity - Stores AI prompts and related information
    /// </summary>
    [SugarTable("ff_ai_prompt_history")]
    public class AIPromptHistory : EntityBaseCreateInfo
    {
        /// <summary>
        /// Prompt Type (e.g., "StageSummary", "WorkflowGeneration", "ChecklistGeneration")
        /// </summary>
        [Required]
        [StringLength(50)]
        [SugarColumn(ColumnName = "prompt_type")]
        public string PromptType { get; set; }

        /// <summary>
        /// Related Entity Type (e.g., "Stage", "Workflow", "Onboarding")
        /// </summary>
        [StringLength(50)]
        [SugarColumn(ColumnName = "entity_type")]
        public string EntityType { get; set; }

        /// <summary>
        /// Related Entity ID
        /// </summary>
        [SugarColumn(ColumnName = "entity_id")]
        public long? EntityId { get; set; }

        /// <summary>
        /// Related Onboarding ID (for context-specific prompts)
        /// </summary>
        [SugarColumn(ColumnName = "onboarding_id")]
        public long? OnboardingId { get; set; }

        /// <summary>
        /// AI Model Provider (e.g., "OpenAI", "Claude", "ZhipuAI")
        /// </summary>
        [StringLength(50)]
        [SugarColumn(ColumnName = "model_provider")]
        public string ModelProvider { get; set; }

        /// <summary>
        /// AI Model Name (e.g., "gpt-4", "claude-3-sonnet")
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "model_name")]
        public string ModelName { get; set; }

        /// <summary>
        /// AI Model ID (internal identifier)
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "model_id")]
        public string ModelId { get; set; }

        /// <summary>
        /// Input Prompt Content
        /// </summary>
        [Required]
        [SugarColumn(ColumnName = "prompt_content", ColumnDataType = "text")]
        public string PromptContent { get; set; }

        /// <summary>
        /// AI Response Content
        /// </summary>
        [SugarColumn(ColumnName = "response_content", ColumnDataType = "text")]
        public string ResponseContent { get; set; }

        /// <summary>
        /// Request Success Status
        /// </summary>
        [SugarColumn(ColumnName = "is_success")]
        public bool IsSuccess { get; set; } = false;

        /// <summary>
        /// Error Message (if request failed)
        /// </summary>
        [StringLength(1000)]
        [SugarColumn(ColumnName = "error_message")]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Response Time in Milliseconds
        /// </summary>
        [SugarColumn(ColumnName = "response_time_ms")]
        public int? ResponseTimeMs { get; set; }

        /// <summary>
        /// Token Usage Information (JSON)
        /// </summary>
        [SugarColumn(ColumnName = "token_usage", ColumnDataType = "jsonb", IsJson = true)]
        public object TokenUsage { get; set; }

        /// <summary>
        /// Additional Metadata (JSON)
        /// </summary>
        [SugarColumn(ColumnName = "metadata", ColumnDataType = "jsonb", IsJson = true)]
        public object Metadata { get; set; }

        /// <summary>
        /// User ID who triggered the request
        /// </summary>
        [SugarColumn(ColumnName = "user_id")]
        public long? UserId { get; set; }

        /// <summary>
        /// User Name who triggered the request
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "user_name")]
        public string UserName { get; set; }

        /// <summary>
        /// Request IP Address
        /// </summary>
        [StringLength(45)]
        [SugarColumn(ColumnName = "ip_address")]
        public string IpAddress { get; set; }

        /// <summary>
        /// User Agent
        /// </summary>
        [StringLength(500)]
        [SugarColumn(ColumnName = "user_agent")]
        public string UserAgent { get; set; }
    }
}