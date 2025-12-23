using System;

namespace FlowFlex.Application.Contracts.Dtos.OW.Dashboard
{
    /// <summary>
    /// Internal DTO for dashboard task query result (includes joined data)
    /// </summary>
    public class DashboardTaskQueryResult
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Priority { get; set; } = "Medium";
        public DateTimeOffset? DueDate { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsRequired { get; set; }
        public string? AssignedTeam { get; set; }
        public string? AssigneeName { get; set; }
        public long? AssigneeId { get; set; }
        public string Status { get; set; } = "Pending";
        
        // Joined from Onboarding
        public long OnboardingId { get; set; }
        public string? CaseCode { get; set; }
        public string? CaseName { get; set; }
    }

    /// <summary>
    /// Internal DTO for deadline query result
    /// </summary>
    public class DeadlineQueryResult
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset? DueDate { get; set; }
        public string Priority { get; set; } = "Medium";
        public string? AssignedTeam { get; set; }
        
        // Joined from Onboarding
        public long OnboardingId { get; set; }
        public string? CaseCode { get; set; }
        public string? CaseName { get; set; }
    }
}
