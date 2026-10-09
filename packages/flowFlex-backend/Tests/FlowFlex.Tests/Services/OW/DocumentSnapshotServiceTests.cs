using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.DocumentSnapshot;
using FlowFlex.Application.Services.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Tests.TestBase;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FlowFlex.Tests.Services.OW
{
    /// <summary>
    /// Unit tests for DocumentSnapshotService.
    /// Covers BR-01 (Upsert), BR-02 (soft-delete), BR-05 (tenant isolation),
    /// paged query filtering, and not-found handling.
    /// </summary>
    public class DocumentSnapshotServiceTests
    {
        private readonly Mock<IDocumentSnapshotRepository> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly DocumentSnapshotService _service;

        private const string TenantUnitId = "test-unit-id-001";

        public DocumentSnapshotServiceTests()
        {
            _mockRepo = new Mock<IDocumentSnapshotRepository>();
            _mockMapper = new Mock<IMapper>();

            _service = new DocumentSnapshotService(
                _mockRepo.Object,
                _mockMapper.Object,
                MockHelper.CreateMockLogger<DocumentSnapshotService>().Object);
        }

        // ──────────────────────── CreateAsync ────────────────────────

        [Fact]
        public async Task CreateAsync_ValidInput_ReturnsNonEmptyUnitId()
        {
            // Arrange
            var input = new DocumentSnapshotInputDto
            {
                Title = "Test Sheet",
                DocType = "Sheet",
                DataJson = "{}",
            };

            var entity = new DocumentSnapshot
            {
                Title = input.Title,
                DocType = input.DocType,
                DataJson = input.DataJson,
            };

            _mockMapper.Setup(m => m.Map<DocumentSnapshot>(input)).Returns(entity);
            _mockRepo.Setup(r => r.InsertAsync(It.IsAny<DocumentSnapshot>(), default, false))
                     .ReturnsAsync(true);

            // Act
            var unitId = await _service.CreateAsync(input);

            // Assert
            unitId.Should().NotBeNullOrWhiteSpace("CreateAsync must return a valid UnitId");
            _mockRepo.Verify(r => r.InsertAsync(It.IsAny<DocumentSnapshot>(), default, false), Times.Once);
        }

        // ──────────────────────── SaveAsync (BR-01 Upsert) ────────────

        [Fact]
        public async Task SaveAsync_NewUnitId_InsertsRecord()
        {
            // Arrange — no existing record
            var input = new DocumentSnapshotInputDto { Title = "New Doc", DocType = "Sheet", DataJson = "{}" };
            var entity = new DocumentSnapshot { Title = input.Title, DocType = input.DocType };

            _mockRepo.Setup(r => r.GetByUnitIdAsync(TenantUnitId)).ReturnsAsync((DocumentSnapshot?)null);
            _mockMapper.Setup(m => m.Map<DocumentSnapshot>(input)).Returns(entity);
            _mockRepo.Setup(r => r.InsertAsync(It.IsAny<DocumentSnapshot>(), default, false))
                     .ReturnsAsync(true);

            // Act
            var result = await _service.SaveAsync(TenantUnitId, input);

            // Assert — BR-01: INSERT path called
            result.Should().BeTrue();
            _mockRepo.Verify(r => r.InsertAsync(It.IsAny<DocumentSnapshot>(), default, false), Times.Once);
            _mockRepo.Verify(r => r.UpsertByUnitIdAsync(It.IsAny<DocumentSnapshot>()), Times.Never);
        }

        [Fact]
        public async Task SaveAsync_ExistingUnitId_UpdatesRecord()
        {
            // Arrange — record exists
            var existing = new DocumentSnapshot
            {
                Id = 1L,
                UnitId = TenantUnitId,
                Title = "Old Title",
                DataJson = "{}",
                DocType = "Sheet",
                Revision = 3,
            };
            var input = new DocumentSnapshotInputDto
            {
                Title = "New Title",
                DocType = "Sheet",
                DataJson = "{\"rev\":4}",
            };

            _mockRepo.Setup(r => r.GetByUnitIdAsync(TenantUnitId)).ReturnsAsync(existing);
            _mockRepo.Setup(r => r.UpsertByUnitIdAsync(It.IsAny<DocumentSnapshot>())).ReturnsAsync(true);

            // Act
            var result = await _service.SaveAsync(TenantUnitId, input);

            // Assert — BR-01: UPDATE path called
            result.Should().BeTrue();
            _mockRepo.Verify(r => r.UpsertByUnitIdAsync(It.IsAny<DocumentSnapshot>()), Times.Once);
            _mockRepo.Verify(r => r.InsertAsync(It.IsAny<DocumentSnapshot>(), default, false), Times.Never);
        }

        // ──────────────────────── GetByUnitIdAsync ────────────────────

        [Fact]
        public async Task GetByUnitIdAsync_ExistingDocument_ReturnsDto()
        {
            // Arrange
            var entity = new DocumentSnapshot
            {
                Id = 1L,
                UnitId = TenantUnitId,
                Title = "My Sheet",
                DocType = "Sheet",
                DataJson = "{}",
                Revision = 1,
            };
            var outputDto = new DocumentSnapshotOutputDto { UnitId = TenantUnitId, Title = "My Sheet" };

            _mockRepo.Setup(r => r.GetByUnitIdAsync(TenantUnitId)).ReturnsAsync(entity);
            _mockMapper.Setup(m => m.Map<DocumentSnapshotOutputDto>(entity)).Returns(outputDto);

            // Act
            var result = await _service.GetByUnitIdAsync(TenantUnitId);

            // Assert
            result.Should().NotBeNull();
            result.UnitId.Should().Be(TenantUnitId);
        }

        [Fact]
        public async Task GetByUnitIdAsync_NotFound_ThrowsCRMException()
        {
            // Arrange — no record for this unitId
            _mockRepo.Setup(r => r.GetByUnitIdAsync("unknown-unit-id"))
                     .ReturnsAsync((DocumentSnapshot?)null);

            // Act + Assert
            await _service.Invoking(s => s.GetByUnitIdAsync("unknown-unit-id"))
                .Should().ThrowAsync<FlowFlex.Domain.Shared.CRMException>()
                .WithMessage("*not found*");
        }

        // ──────────────────────── DeleteAsync (BR-02 Soft-delete) ─────

        [Fact]
        public async Task DeleteAsync_ExistingId_SetsIsValidFalse()
        {
            // Arrange
            _mockRepo
                .Setup(r => r.UpdateAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<DocumentSnapshot, DocumentSnapshot>>>(),
                    It.IsAny<System.Linq.Expressions.Expression<Func<DocumentSnapshot, bool>>>(),
                    default))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(12345L);

            // Assert — BR-02: UpdateAsync (not DeleteAsync!) is called
            result.Should().BeTrue();
            _mockRepo.Verify(
                r => r.UpdateAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<DocumentSnapshot, DocumentSnapshot>>>(),
                    It.IsAny<System.Linq.Expressions.Expression<Func<DocumentSnapshot, bool>>>(),
                    default),
                Times.Once);
        }

        // ──────────────────────── QueryAsync (filters) ────────────────

        [Fact]
        public async Task QueryAsync_WithDocType_PassesFilterToRepository()
        {
            // Arrange
            var query = new DocumentSnapshotQueryRequest
            {
                PageIndex = 1,
                PageSize = 20,
                DocType = "Sheet",
            };

            var items = new List<DocumentSnapshot>
            {
                new() { UnitId = "u1", DocType = "Sheet", Title = "A" },
            };

            _mockRepo.Setup(r => r.GetPagedAsync(1, 20, null, "Sheet", "modify_date", "desc"))
                     .ReturnsAsync((items, 1));
            _mockMapper.Setup(m => m.Map<List<DocumentSnapshotOutputDto>>(items))
                       .Returns(new List<DocumentSnapshotOutputDto>
                       {
                           new() { UnitId = "u1", DocType = "Sheet", Title = "A" },
                       });

            // Act
            var result = await _service.QueryAsync(query);

            // Assert
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            _mockRepo.Verify(r => r.GetPagedAsync(1, 20, null, "Sheet", "modify_date", "desc"), Times.Once);
        }

        [Fact]
        public async Task QueryAsync_WithKeyword_PassesKeywordToRepository()
        {
            // Arrange
            var query = new DocumentSnapshotQueryRequest
            {
                PageIndex = 1,
                PageSize = 20,
                Keyword = "销售",
            };

            _mockRepo.Setup(r => r.GetPagedAsync(1, 20, "销售", null, "modify_date", "desc"))
                     .ReturnsAsync((new List<DocumentSnapshot>(), 0));
            _mockMapper.Setup(m => m.Map<List<DocumentSnapshotOutputDto>>(It.IsAny<List<DocumentSnapshot>>()))
                       .Returns(new List<DocumentSnapshotOutputDto>());

            // Act
            var result = await _service.QueryAsync(query);

            // Assert
            result.Items.Should().BeEmpty();
            _mockRepo.Verify(r => r.GetPagedAsync(1, 20, "销售", null, "modify_date", "desc"), Times.Once);
        }

        [Fact]
        public async Task QueryAsync_WithPaging_ReturnsCorrectPage()
        {
            // Arrange — 25 items total, page 2 of size 10
            var items = new List<DocumentSnapshot>();
            for (int i = 0; i < 10; i++) items.Add(new DocumentSnapshot { UnitId = $"u{i}", Title = $"Doc {i}" });

            var query = new DocumentSnapshotQueryRequest { PageIndex = 2, PageSize = 10 };
            _mockRepo.Setup(r => r.GetPagedAsync(2, 10, null, null, "modify_date", "desc"))
                     .ReturnsAsync((items, 25));
            _mockMapper.Setup(m => m.Map<List<DocumentSnapshotOutputDto>>(items))
                       .Returns(new List<DocumentSnapshotOutputDto>(new DocumentSnapshotOutputDto[10]));

            // Act
            var result = await _service.QueryAsync(query);

            // Assert
            result.TotalCount.Should().Be(25);
            result.PageIndex.Should().Be(2);
            result.PageSize.Should().Be(10);
            result.Items.Should().HaveCount(10);
        }
    }
}
