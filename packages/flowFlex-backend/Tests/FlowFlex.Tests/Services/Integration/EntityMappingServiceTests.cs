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

namespace FlowFlex.Tests.Services.Integration
{
    public class EntityMappingServiceTests
    {
        private readonly Mock<IEntityMappingRepository> _mockRepository;
        private readonly Mock<IIntegrationRepository> _mockIntegrationRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<EntityMappingService>> _mockLogger;
        private readonly UserContext _userContext;
        private readonly EntityMappingService _service;

        public EntityMappingServiceTests()
        {
            _mockRepository = new Mock<IEntityMappingRepository>();
            _mockIntegrationRepository = new Mock<IIntegrationRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = MockHelper.CreateMockLogger<EntityMappingService>();
            _userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);

            _service = new EntityMappingService(
                _mockRepository.Object,
                _mockIntegrationRepository.Object,
                _mockMapper.Object,
                _userContext,
                _mockLogger.Object
            );
        }

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithValidInput_ShouldReturnEntityMappingId()
        {
            // Arrange
            var input = new EntityMappingInputDto
            {
                IntegrationId = 123,
                ExternalEntityName = "Contact",
                ExternalEntityType = "contact",
                WfeEntityType = "Customer"
            };

            var entity = new EntityMapping
            {
                Id = 456,
                IntegrationId = input.IntegrationId,
                ExternalEntityName = input.ExternalEntityName
            };

            var integration = new Domain.Entities.Integration.Integration
            {
                Id = input.IntegrationId,
                Name = "Test Integration"
            };

            _mockIntegrationRepository.Setup(r => r.GetByIdAsync(It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(integration);
            _mockMapper.Setup(m => m.Map<EntityMapping>(It.IsAny<EntityMappingInputDto>()))
                .Returns(entity);
            _mockRepository.Setup(r => r.InsertReturnSnowflakeIdAsync(It.IsAny<EntityMapping>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(456L);

            // Act
            var result = await _service.CreateAsync(input);

            // Assert
            result.Should().Be(456L);
            _mockRepository.Verify(r => r.InsertReturnSnowflakeIdAsync(It.IsAny<EntityMapping>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region GetByIntegrationIdAsync Tests

        [Fact]
        public async Task GetByIntegrationIdAsync_WithValidIntegrationId_ShouldReturnList()
        {
            // Arrange
            var integrationId = 123L;
            var entities = new List<EntityMapping>
            {
                new EntityMapping { Id = 1, IntegrationId = integrationId, ExternalEntityName = "Contact" },
                new EntityMapping { Id = 2, IntegrationId = integrationId, ExternalEntityName = "Account" }
            };

            var dtos = new List<EntityMappingOutputDto>
            {
                new EntityMappingOutputDto { Id = 1, IntegrationId = integrationId, ExternalEntityName = "Contact" },
                new EntityMappingOutputDto { Id = 2, IntegrationId = integrationId, ExternalEntityName = "Account" }
            };

            _mockRepository.Setup(r => r.GetByIntegrationIdAsync(It.IsAny<long>()))
                .ReturnsAsync(entities);
            _mockMapper.Setup(m => m.Map<List<EntityMappingOutputDto>>(It.IsAny<List<EntityMapping>>()))
                .Returns(dtos);

            // Act
            var result = await _service.GetByIntegrationIdAsync(integrationId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(x => x.IntegrationId == integrationId).Should().BeTrue();
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnDto()
        {
            // Arrange
            var entityMappingId = 456L;
            var entity = new EntityMapping
            {
                Id = entityMappingId,
                IntegrationId = 123,
                ExternalEntityName = "Contact",
                WfeEntityType = "Customer"
            };

            var expectedDto = new EntityMappingOutputDto
            {
                Id = entityMappingId,
                IntegrationId = 123,
                ExternalEntityName = "Contact",
                WfeEntityType = "Customer"
            };

            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);
            _mockMapper.Setup(m => m.Map<EntityMappingOutputDto>(It.IsAny<EntityMapping>()))
                .Returns(expectedDto);

            // Act
            var result = await _service.GetByIdAsync(entityMappingId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(entityMappingId);
            result.ExternalEntityName.Should().Be("Contact");
        }

        #endregion
    }
}
