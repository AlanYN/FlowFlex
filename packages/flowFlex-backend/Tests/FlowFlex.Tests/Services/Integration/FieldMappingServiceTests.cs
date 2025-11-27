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
    public class InboundFieldMappingServiceTests
    {
        private readonly Mock<IInboundFieldMappingRepository> _mockRepository;
        private readonly Mock<IIntegrationRepository> _mockIntegrationRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<InboundFieldMappingService>> _mockLogger;
        private readonly UserContext _userContext;
        private readonly InboundFieldMappingService _service;

        public InboundFieldMappingServiceTests()
        {
            _mockRepository = new Mock<IInboundFieldMappingRepository>();
            _mockIntegrationRepository = new Mock<IIntegrationRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = MockHelper.CreateMockLogger<InboundFieldMappingService>();
            _userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);

            _service = new InboundFieldMappingService(
                _mockRepository.Object,
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
            var input = new InboundFieldMappingInputDto
            {
                IntegrationId = 123,
                ActionId = 456,
                ExternalFieldName = "email",
                WfeFieldId = "Email",
                SyncDirection = SyncDirection.Editable,
                IsRequired = true
            };

            var integration = new FlowFlex.Domain.Entities.Integration.Integration
            {
                Id = input.IntegrationId,
                Name = "Test Integration"
            };

            _mockIntegrationRepository.Setup(r => r.GetByIdAsync(It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(integration);
            _mockRepository.Setup(r => r.InsertReturnSnowflakeIdAsync(It.IsAny<InboundFieldMapping>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(789L);

            // Act
            var result = await _service.CreateAsync(input);

            // Assert
            result.Should().Be(789L);
            _mockRepository.Verify(r => r.InsertReturnSnowflakeIdAsync(It.IsAny<InboundFieldMapping>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region GetByIntegrationIdAsync Tests

        [Fact]
        public async Task GetByIntegrationIdAsync_WithValidId_ShouldReturnList()
        {
            // Arrange
            var integrationId = 123L;
            var entities = new List<InboundFieldMapping>
            {
                new InboundFieldMapping { Id = 1, IntegrationId = integrationId, ExternalFieldName = "email", FieldType = FieldType.Text, SyncDirection = SyncDirection.ViewOnly },
                new InboundFieldMapping { Id = 2, IntegrationId = integrationId, ExternalFieldName = "phone", FieldType = FieldType.Text, SyncDirection = SyncDirection.Editable }
            };

            _mockRepository.Setup(r => r.GetByIntegrationIdAsync(It.IsAny<long>()))
                .ReturnsAsync(entities);

            // Act
            var result = await _service.GetByIntegrationIdAsync(integrationId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }

        #endregion

        #region GetByActionIdAsync Tests

        [Fact]
        public async Task GetByActionIdAsync_WithValidId_ShouldReturnList()
        {
            // Arrange
            var actionId = 456L;
            var entities = new List<InboundFieldMapping>
            {
                new InboundFieldMapping { Id = 1, ActionId = actionId, ExternalFieldName = "email", FieldType = FieldType.Text, SyncDirection = SyncDirection.ViewOnly },
                new InboundFieldMapping { Id = 2, ActionId = actionId, ExternalFieldName = "phone", FieldType = FieldType.Text, SyncDirection = SyncDirection.Editable }
            };

            _mockRepository.Setup(r => r.GetByActionIdAsync(It.IsAny<long>()))
                .ReturnsAsync(entities);

            // Act
            var result = await _service.GetByActionIdAsync(actionId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }

        #endregion

        #region GetByIntegrationIdAndActionIdAsync Tests

        [Fact]
        public async Task GetByIntegrationIdAndActionIdAsync_ShouldReturnFilteredList()
        {
            // Arrange
            var integrationId = 123L;
            var actionId = 456L;
            var entities = new List<InboundFieldMapping>
            {
                new InboundFieldMapping
                {
                    Id = 1,
                    IntegrationId = integrationId,
                    ActionId = actionId,
                    ExternalFieldName = "email",
                    FieldType = FieldType.Text,
                    SyncDirection = SyncDirection.Editable
                }
            };

            _mockRepository.Setup(r => r.GetByIntegrationIdAndActionIdAsync(It.IsAny<long>(), It.IsAny<long>()))
                .ReturnsAsync(entities);

            // Act
            var result = await _service.GetByIntegrationIdAndActionIdAsync(integrationId, actionId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().SyncDirection.Should().Be("Editable");
        }

        #endregion
    }
}
