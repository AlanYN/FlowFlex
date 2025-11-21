using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Services.Integration;
using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Tests.TestBase;
using FluentAssertions;
using Moq;
using Xunit;
using AutoMapper;
using Domain.Shared.Enums;

namespace FlowFlex.Tests.Services.Integration
{
    public class FieldMappingServiceTests
    {
        private readonly Mock<IFieldMappingRepository> _mockRepository;
        private readonly Mock<IEntityMappingRepository> _mockEntityMappingRepository;
        private readonly Mock<IIntegrationRepository> _mockIntegrationRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<FieldMappingService>> _mockLogger;
        private readonly UserContext _userContext;
        private readonly FieldMappingService _service;

        public FieldMappingServiceTests()
        {
            _mockRepository = new Mock<IFieldMappingRepository>();
            _mockEntityMappingRepository = new Mock<IEntityMappingRepository>();
            _mockIntegrationRepository = new Mock<IIntegrationRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = MockHelper.CreateMockLogger<FieldMappingService>();
            _userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);

            _service = new FieldMappingService(
                _mockRepository.Object,
                _mockEntityMappingRepository.Object,
                _mockIntegrationRepository.Object,
                _mockMapper.Object,
                _userContext,
                _mockLogger.Object
            );
        }

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithValidInput_ShouldReturnFieldMappingId()
        {
            // Arrange
            var input = new FieldMappingInputDto
            {
                IntegrationId = 123,
                EntityMappingId = 456,
                ExternalFieldName = "email",
                WfeFieldId = "Email",
                SyncDirection = SyncDirection.Editable,
                IsRequired = true
            };

            var entity = new FieldMapping
            {
                Id = 789,
                IntegrationId = input.IntegrationId,
                EntityMappingId = input.EntityMappingId,
                ExternalFieldName = input.ExternalFieldName,
                WfeFieldId = input.WfeFieldId,
                WorkflowIds = "[]",
                TransformRules = "{}"
            };

            var entityMapping = new EntityMapping
            {
                Id = input.EntityMappingId,
                IntegrationId = input.IntegrationId
            };

            _mockEntityMappingRepository.Setup(r => r.GetByIdAsync(It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(entityMapping);
            _mockMapper.Setup(m => m.Map<FieldMapping>(It.IsAny<FieldMappingInputDto>()))
                .Returns(entity);
            _mockRepository.Setup(r => r.InsertReturnSnowflakeIdAsync(It.IsAny<FieldMapping>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(789L);

            // Act
            var result = await _service.CreateAsync(input);

            // Assert
            result.Should().Be(789L);
            _mockRepository.Verify(r => r.InsertReturnSnowflakeIdAsync(It.IsAny<FieldMapping>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region GetByEntityMappingIdAsync Tests

        [Fact]
        public async Task GetByEntityMappingIdAsync_WithValidId_ShouldReturnList()
        {
            // Arrange
            var entityMappingId = 456L;
            var entities = new List<FieldMapping>
            {
                new FieldMapping { Id = 1, EntityMappingId = entityMappingId, ExternalFieldName = "email", WorkflowIds = "[]", TransformRules = "{}" },
                new FieldMapping { Id = 2, EntityMappingId = entityMappingId, ExternalFieldName = "phone", WorkflowIds = "[]", TransformRules = "{}" }
            };

            var dtos = new List<FieldMappingOutputDto>
            {
                new FieldMappingOutputDto { Id = 1, EntityMappingId = entityMappingId, ExternalFieldName = "email" },
                new FieldMappingOutputDto { Id = 2, EntityMappingId = entityMappingId, ExternalFieldName = "phone" }
            };

            _mockRepository.Setup(r => r.GetByEntityMappingIdAsync(It.IsAny<long>()))
                .ReturnsAsync(entities);
            _mockMapper.Setup(m => m.Map<List<FieldMappingOutputDto>>(It.IsAny<List<FieldMapping>>()))
                .Returns(dtos);

            // Act
            var result = await _service.GetByEntityMappingIdAsync(entityMappingId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(x => x.EntityMappingId == entityMappingId).Should().BeTrue();
        }

        #endregion

        #region GetBidirectionalMappingsAsync Tests

        [Fact]
        public async Task GetBidirectionalMappingsAsync_ShouldReturnOnlyEditableMappings()
        {
            // Arrange
            var entityMappingId = 456L;
            var entities = new List<FieldMapping>
            {
                new FieldMapping 
                { 
                    Id = 1, 
                    EntityMappingId = entityMappingId, 
                    ExternalFieldName = "email",
                    SyncDirection = SyncDirection.Editable,
                    WorkflowIds = "[]",
                    TransformRules = "{}"
                }
            };

            var dtos = new List<FieldMappingOutputDto>
            {
                new FieldMappingOutputDto { Id = 1, SyncDirection = SyncDirection.Editable }
            };

            _mockRepository.Setup(r => r.GetBidirectionalMappingsAsync(It.IsAny<long>()))
                .ReturnsAsync(entities);
            _mockMapper.Setup(m => m.Map<List<FieldMappingOutputDto>>(It.IsAny<List<FieldMapping>>()))
                .Returns(dtos);

            // Act
            var result = await _service.GetBidirectionalMappingsAsync(entityMappingId);

            // Assert
            result.Should().NotBeNull();
            result.All(x => x.SyncDirection == SyncDirection.Editable).Should().BeTrue();
        }

        #endregion
    }
}
