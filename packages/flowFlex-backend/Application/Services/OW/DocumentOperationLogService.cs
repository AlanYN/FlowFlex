using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts.Dtos.OW.DocumentSnapshot;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Document operation log service implementation
    /// </summary>
    public class DocumentOperationLogService : IDocumentOperationLogService, IScopedService
    {
        private readonly IDocumentOperationLogRepository _logRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<DocumentOperationLogService> _logger;

        public DocumentOperationLogService(
            IDocumentOperationLogRepository logRepository,
            IMapper mapper,
            ILogger<DocumentOperationLogService> logger)
        {
            _logRepository = logRepository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<string> AddLogAsync(DocumentOperationLogInputDto input)
        {
            var entity = new DocumentOperationLog
            {
                UnitId = input.UnitId,
                OperatorUserId = input.UserId,
                OperatorUserName = input.UserName,
                OpTimestamp = input.Timestamp,
                MutationId = input.MutationId,
                ParamsJson = input.ParamsJson,
                Revision = input.Revision,
            };

            // For set-range-values mutations, capture old cell values from the previous
            // log entry so the history panel can display Before → After comparisons.
            if (input.MutationId == "sheet.mutation.set-range-values" &&
                !string.IsNullOrWhiteSpace(input.ParamsJson))
            {
                entity.OldValuesJson = await BuildOldValuesJsonAsync(input.UnitId, input.ParamsJson);
            }

            entity.InitNewId();
            await _logRepository.InsertAsync(entity);
            return entity.Id.ToString();
        }

        /// <summary>
        /// Builds the old_values_json by looking up the most recent previous log entry
        /// for the same sheet and extracting what each affected cell contained at that time.
        ///
        /// Strategy:
        ///   1. Parse the incoming params_json to get subUnitId + affected row/col indices.
        ///   2. Query the most recent prior log for the same unitId + subUnitId.
        ///   3. From that log's params_json (new values at that time), extract the cells
        ///      that overlap with the current mutation → those are the "old" values now.
        ///   4. For any cell not found in the previous log, the old value is null (empty).
        /// </summary>
        private async Task<string> BuildOldValuesJsonAsync(string unitId, string paramsJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(paramsJson);
                var root = doc.RootElement;

                if (!root.TryGetProperty("subUnitId", out var subUnitIdProp) ||
                    !root.TryGetProperty("cellValue", out var cellValueProp))
                    return null;

                var subUnitId = subUnitIdProp.GetString();
                if (string.IsNullOrWhiteSpace(subUnitId)) return null;

                // Build the set of row/col keys we need old values for
                var neededCells = new Dictionary<string, HashSet<string>>();
                foreach (var rowProp in cellValueProp.EnumerateObject())
                {
                    neededCells[rowProp.Name] = new HashSet<string>();
                    foreach (var colProp in rowProp.Value.EnumerateObject())
                        neededCells[rowProp.Name].Add(colProp.Name);
                }

                // Get the previous log entry for this sheet
                var previousLog = await _logRepository.GetLatestSetRangeLogAsync(unitId, subUnitId);

                // Build old values map: { rowIdx: { colIdx: oldValueString | null } }
                var oldValues = new Dictionary<string, Dictionary<string, object>>();

                foreach (var (rowKey, cols) in neededCells)
                {
                    oldValues[rowKey] = new Dictionary<string, object>();
                    foreach (var colKey in cols)
                        oldValues[rowKey][colKey] = null; // default: cell was empty
                }

                if (previousLog?.ParamsJson != null)
                {
                    try
                    {
                        using var prevDoc = JsonDocument.Parse(previousLog.ParamsJson);
                        if (prevDoc.RootElement.TryGetProperty("cellValue", out var prevCellValue))
                        {
                            foreach (var (rowKey, cols) in neededCells)
                            {
                                if (!prevCellValue.TryGetProperty(rowKey, out var prevRow)) continue;
                                foreach (var colKey in cols)
                                {
                                    if (!prevRow.TryGetProperty(colKey, out var prevCell)) continue;
                                    // Extract the "v" field (raw cell value)
                                    if (prevCell.TryGetProperty("v", out var vProp) &&
                                        vProp.ValueKind != JsonValueKind.Null)
                                    {
                                        oldValues[rowKey][colKey] = vProp.ValueKind switch
                                        {
                                            JsonValueKind.Number => (object)vProp.GetDouble(),
                                            JsonValueKind.True => true,
                                            JsonValueKind.False => false,
                                            _ => vProp.GetString()
                                        };
                                    }
                                }
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse previous log paramsJson for unitId={UnitId}", unitId);
                    }
                }

                return JsonSerializer.Serialize(oldValues);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to build old values JSON for unitId={UnitId}", unitId);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<PagedResult<DocumentOperationLogOutputDto>> GetLogsByUnitIdAsync(
            string unitId,
            DateTime? from = null,
            DateTime? to = null,
            string userId = null,
            string keyword = null,
            int pageIndex = 1,
            int pageSize = 50)
        {
            var (items, totalCount) = await _logRepository.GetLogsByUnitIdAsync(unitId, from, to, userId, keyword, pageIndex, pageSize);

            return new PagedResult<DocumentOperationLogOutputDto>
            {
                Items = _mapper.Map<List<DocumentOperationLogOutputDto>>(items),
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize,
            };
        }

        /// <inheritdoc/>
        public async Task<int> GetCurrentRevisionAsync(string unitId)
        {
            return await _logRepository.GetRevisionAsync(unitId);
        }
    }
}
