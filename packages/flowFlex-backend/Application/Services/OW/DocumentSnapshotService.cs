using System;
using System.Collections.Generic;
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
    /// Document snapshot service implementation
    /// </summary>
    public class DocumentSnapshotService : IDocumentSnapshotService, IScopedService
    {
        private readonly IDocumentSnapshotRepository _snapshotRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<DocumentSnapshotService> _logger;

        public DocumentSnapshotService(
            IDocumentSnapshotRepository snapshotRepository,
            IMapper mapper,
            ILogger<DocumentSnapshotService> logger)
        {
            _snapshotRepository = snapshotRepository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<string> CreateAsync(DocumentSnapshotInputDto input)
        {
            var entity = _mapper.Map<DocumentSnapshot>(input);
            entity.InitNewId();
            entity.UnitId = Guid.NewGuid().ToString();
            entity.Revision = 1;
            entity.DataJson = string.IsNullOrWhiteSpace(input.DataJson) ? "{}" : input.DataJson;

            await _snapshotRepository.InsertAsync(entity);
            return entity.UnitId;
        }

        /// <inheritdoc/>
        public async Task<bool> SaveAsync(string unitId, DocumentSnapshotInputDto input)
        {
            var existing = await _snapshotRepository.GetByUnitIdAsync(unitId);

            if (existing == null)
            {
                // INSERT — create new record for this unitId
                var entity = _mapper.Map<DocumentSnapshot>(input);
                entity.InitNewId();
                entity.UnitId = unitId;
                entity.Revision = 1;
                entity.DataJson = input.DataJson ?? "{}";
                return await _snapshotRepository.InsertAsync(entity);
            }

            // UPDATE — update existing record
            existing.Title = input.Title ?? existing.Title;
            existing.DataJson = input.DataJson ?? existing.DataJson;
            existing.ModifyDate = DateTimeOffset.UtcNow;
            return await _snapshotRepository.UpsertByUnitIdAsync(existing);
        }

        /// <inheritdoc/>
        public async Task<DocumentSnapshotOutputDto> GetByUnitIdAsync(string unitId)
        {
            var entity = await _snapshotRepository.GetByUnitIdAsync(unitId);
            if (entity == null)
                throw new CRMException(ErrorCodeEnum.NotFound, $"Document not found: {unitId}");

            return _mapper.Map<DocumentSnapshotOutputDto>(entity);
        }

        /// <inheritdoc/>
        public async Task<List<DocumentSnapshotOutputDto>> GetByEntityAsync(string entityType, long entityId)
        {
            var entities = await _snapshotRepository.GetByEntityAsync(entityType, entityId);
            return _mapper.Map<List<DocumentSnapshotOutputDto>>(entities);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(long id)
        {
            return await _snapshotRepository.UpdateAsync(
                x => new DocumentSnapshot { IsValid = false },
                x => x.Id == id);
        }

        /// <inheritdoc/>
        public async Task<PagedResult<DocumentSnapshotOutputDto>> QueryAsync(DocumentSnapshotQueryRequest query)
        {
            var (items, totalCount) = await _snapshotRepository.GetPagedAsync(
                query.PageIndex,
                query.PageSize,
                query.Keyword,
                query.DocType,
                query.SortBy ?? "modify_date",
                query.SortOrder ?? "desc");

            return new PagedResult<DocumentSnapshotOutputDto>
            {
                Items = _mapper.Map<List<DocumentSnapshotOutputDto>>(items),
                TotalCount = totalCount,
                PageIndex = query.PageIndex,
                PageSize = query.PageSize,
            };
        }
    }
}
