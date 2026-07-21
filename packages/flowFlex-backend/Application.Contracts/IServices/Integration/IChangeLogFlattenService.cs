using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.Integration;

/// <summary>
/// Service for converting ChangeLog records into a flattened field-level diff format.
/// Used by external systems (e.g., Ticketing) to pull structured change history.
/// </summary>
public interface IChangeLogFlattenService : IScopedService
{
    /// <summary>
    /// Get change logs by entity ID in flattened format (field/oldValue/newValue)
    /// </summary>
    /// <param name="entityId">Entity ID stored in ff_onboarding.entity_id (e.g., Ticket ID)</param>
    /// <param name="onboardingId">WFE Case ID (alternative to entityId, for internal use)</param>
    /// <param name="since">Only return records after this timestamp (optional)</param>
    /// <param name="changesOnly">If true, only return records that have field-level changes (excludes TaskComplete, StageSave etc.)</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paged flattened change logs</returns>
    Task<FlattenedChangeLogPagedResponse> GetFlattenedChangeLogsAsync(
        string? entityId = null,
        long? onboardingId = null,
        DateTimeOffset? since = null,
        bool changesOnly = false,
        int pageIndex = 1,
        int pageSize = 20);

    /// <summary>
    /// Extract field-level changes from a single OperationChangeLog record.
    /// Used by Kafka producer to build the changes array in the message payload.
    /// </summary>
    List<FieldChangeDto> ExtractChanges(OperationChangeLog log);
}
