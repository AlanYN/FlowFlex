using System;

namespace FlowFlex.Domain.Shared.Models
{
    /// <summary>
    /// Enriched trigger log record with source and target case display info.
    /// Returned by the OW-729 Trigger History API.
    /// </summary>
    public class TriggerLogOutputDto
    {
        public string Id { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;

        // Source
        public string SourceWorkflowId { get; set; } = string.Empty;
        public string SourceOnboardingId { get; set; } = string.Empty;
        public string SourceCaseName { get; set; } = string.Empty;
        public string SourceCaseCode { get; set; } = string.Empty;

        // Target
        public string TargetWorkflowId { get; set; } = string.Empty;
        public string? TargetOnboardingId { get; set; }
        public string? TargetCaseName { get; set; }
        public string? TargetCaseCode { get; set; }

        public string Status { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string CompletionType { get; set; } = string.Empty;

        public string ConditionsSnapshot { get; set; } = string.Empty;
        public string MappingsSnapshot { get; set; } = string.Empty;

        public DateTimeOffset CreateDate { get; set; }
        public string CreateBy { get; set; } = string.Empty;

        public string TenantId { get; set; } = string.Empty;
        public string AppCode { get; set; } = string.Empty;
    }
}
