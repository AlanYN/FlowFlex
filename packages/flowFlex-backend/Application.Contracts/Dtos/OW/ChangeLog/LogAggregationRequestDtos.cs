using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Shared.Enums.OW;

namespace FlowFlex.Application.Contracts.Dtos.OW.ChangeLog;

/// <summary>
/// Aggregated log query request
/// </summary>
public class AggregatedLogQueryRequest
{
    public long? OnboardingId { get; set; }
    public long? StageId { get; set; }
    public List<BusinessModuleEnum> BusinessModules { get; set; }
    public List<OperationTypeEnum> OperationTypes { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Multi-business ID query request
/// </summary>
public class MultiBusinessIdQueryRequest
{
    [Required]
    public List<long> BusinessIds { get; set; }

    public BusinessModuleEnum? BusinessModule { get; set; }
    public long? OnboardingId { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Comprehensive statistics request
/// </summary>
public class ComprehensiveStatisticsRequest
{
    public long? OnboardingId { get; set; }
    public long? StageId { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
}

/// <summary>
/// Operation timeline request
/// </summary>
public class OperationTimelineRequest
{
    public long? OnboardingId { get; set; }
    public long? StageId { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
}

/// <summary>
/// Log search request
/// </summary>
public class LogSearchRequest
{
    [Required]
    [StringLength(500)]
    public string SearchTerm { get; set; }

    public long? OnboardingId { get; set; }
    public long? StageId { get; set; }
    public List<BusinessModuleEnum> BusinessModules { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// User activity request
/// </summary>
public class UserActivityRequest
{
    [Required]
    public long OperatorId { get; set; }

    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public List<BusinessModuleEnum> BusinessModules { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Log export request
/// </summary>
public class LogExportRequest
{
    [Required]
    [StringLength(10)]
    public string Format { get; set; } // "csv", "excel", "json"

    public long? OnboardingId { get; set; }
    public long? StageId { get; set; }
    public List<BusinessModuleEnum> BusinessModules { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
}

/// <summary>
/// Cache warm-up request
/// </summary>
public class CacheWarmUpRequest
{
    [Required]
    public long OnboardingId { get; set; }

    [Required]
    public long StageId { get; set; }
}
