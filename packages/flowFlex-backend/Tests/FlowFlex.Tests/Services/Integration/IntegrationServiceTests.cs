using System.Net.Http;
using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Contracts.IServices.Integration;
using FlowFlex.Application.Services.Integration;
using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository.Action;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Tests.TestBase;
using FluentAssertions;
using Moq;
using Xunit;
using AutoMapper;
using Domain.Shared.Enums;
using FlowFlex.Domain.Shared.Enums;
using SqlSugar;

namespace FlowFlex.Tests.Services.Integration
{
    public class IntegrationServiceTests
    {
        private readonly Mock<IIntegrationRepository> _mockIntegrationRepository;
        private readonly Mock<IEntityMappingRepository> _mockEntityMappingRepository;
        private readonly Mock<IFieldMappingRepository> _mockFieldMappingRepository;
        private readonly Mock<IFieldMappingService> _mockFieldMappingService;
        private readonly Mock<IOutboundConfigurationRepository> _mockOutboundConfigurationRepository;
        private readonly Mock<IInboundConfigurationRepository> _mockInboundConfigurationRepository;
        private readonly Mock<IQuickLinkRepository> _mockQuickLinkRepository;
        private readonly Mock<IActionDefinitionRepository> _mockActionDefinitionRepository;
        private readonly Mock<ISqlSugarClient> _mockSqlSugarClient;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<IntegrationService>> _mockLogger;
        private readonly UserContext _userContext;
        private readonly IntegrationService _service;

        public IntegrationServiceTests()
        {
            _mockIntegrationRepository = new Mock<IIntegrationRepository>();
            _mockEntityMappingRepository = new Mock<IEntityMappingRepository>();
            _mockFieldMappingRepository = new Mock<IFieldMappingRepository>();
            _mockFieldMappingService = new Mock<IFieldMappingService>();
            _mockOutboundConfigurationRepository = new Mock<IOutboundConfigurationRepository>();
            _mockInboundConfigurationRepository = new Mock<IInboundConfigurationRepository>();
            _mockQuickLinkRepository = new Mock<IQuickLinkRepository>();
            _mockActionDefinitionRepository = new Mock<IActionDefinitionRepository>();
            _mockSqlSugarClient = new Mock<ISqlSugarClient>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = MockHelper.CreateMockLogger<IntegrationService>();
            _userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);

            // Setup HttpClientFactory to return a real HttpClient
            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient());

            _service = new IntegrationService(
                _mockIntegrationRepository.Object,
                _mockEntityMappingRepository.Object,
                _mockFieldMappingRepository.Object,
                _mockFieldMappingService.Object,
                _mockOutboundConfigurationRepository.Object,
                _mockInboundConfigurationRepository.Object,
                _mockQuickLinkRepository.Object,
                _mockActionDefinitionRepository.Object,
                _mockSqlSugarClient.Object,
                _mockHttpClientFactory.Object,
                _mockMapper.Object,
                _userContext,
                _mockLogger.Object
            );
        }

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithValidInput_ShouldReturnIntegrationId()
        {
            // Arrange
            var input = new IntegrationInputDto
            {
                Name = "Test CRM Integration",
                Type = "CRM",
                EndpointUrl = "https://api.crm.com",
                AuthMethod = AuthenticationMethod.ApiKey
            };

            var entity = new Domain.Entities.Integration.Integration
            {
                Id = 123,
                Name = input.Name,
                Type = input.Type
            };

            _mockMapper.Setup(m => m.Map<Domain.Entities.Integration.Integration>(It.IsAny<IntegrationInputDto>()))
                .Returns(entity);
            _mockIntegrationRepository.Setup(r => r.InsertReturnSnowflakeIdAsync(It.IsAny<Domain.Entities.Integration.Integration>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(123L);

            // Act
            var result = await _service.CreateAsync(input);

            // Assert
            result.Should().Be(123L);
            _mockIntegrationRepository.Verify(r => r.InsertReturnSnowflakeIdAsync(It.IsAny<Domain.Entities.Integration.Integration>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnDto()
        {
            // Arrange
            var integrationId = 123L;
            var entity = new Domain.Entities.Integration.Integration
            {
                Id = integrationId,
                Name = "Test Integration",
                Type = "CRM",
                Status = IntegrationStatus.Connected
            };

            var expectedDto = new IntegrationOutputDto
            {
                Id = integrationId,
                Name = entity.Name,
                Type = entity.Type,
                Status = entity.Status
            };

            _mockIntegrationRepository.Setup(r => r.GetByIdAsync(It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);
            _mockMapper.Setup(m => m.Map<IntegrationOutputDto>(It.IsAny<Domain.Entities.Integration.Integration>()))
                .Returns(expectedDto);

            // Act
            var result = await _service.GetByIdAsync(integrationId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(integrationId);
            result.Name.Should().Be(entity.Name);
        }

        #endregion

        #region GetByTypeAsync Tests

        [Fact]
        public async Task GetByTypeAsync_WithValidType_ShouldReturnList()
        {
            // Arrange
            var type = "CRM";
            var entities = new List<Domain.Entities.Integration.Integration>
            {
                new Domain.Entities.Integration.Integration { Id = 1, Name = "CRM 1", Type = type },
                new Domain.Entities.Integration.Integration { Id = 2, Name = "CRM 2", Type = type }
            };

            var dtos = new List<IntegrationOutputDto>
            {
                new IntegrationOutputDto { Id = 1, Name = "CRM 1", Type = type },
                new IntegrationOutputDto { Id = 2, Name = "CRM 2", Type = type }
            };

            _mockIntegrationRepository.Setup(r => r.GetByTypeAsync(It.IsAny<string>()))
                .ReturnsAsync(entities);
            _mockMapper.Setup(m => m.Map<List<IntegrationOutputDto>>(It.IsAny<List<Domain.Entities.Integration.Integration>>()))
                .Returns(dtos);

            // Act
            var result = await _service.GetByTypeAsync(type);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(x => x.Type == type).Should().BeTrue();
        }

        #endregion

        #region GetActiveIntegrationsAsync Tests

        [Fact]
        public async Task GetActiveIntegrationsAsync_ShouldReturnOnlyConnectedIntegrations()
        {
            // Arrange
            var entities = new List<Domain.Entities.Integration.Integration>
            {
                new Domain.Entities.Integration.Integration { Id = 1, Name = "Active 1", Status = IntegrationStatus.Connected },
                new Domain.Entities.Integration.Integration { Id = 2, Name = "Active 2", Status = IntegrationStatus.Connected }
            };

            var dtos = new List<IntegrationOutputDto>
            {
                new IntegrationOutputDto { Id = 1, Name = "Active 1", Status = IntegrationStatus.Connected },
                new IntegrationOutputDto { Id = 2, Name = "Active 2", Status = IntegrationStatus.Connected }
            };

            _mockIntegrationRepository.Setup(r => r.GetActiveIntegrationsAsync())
                .ReturnsAsync(entities);
            _mockMapper.Setup(m => m.Map<List<IntegrationOutputDto>>(It.IsAny<List<Domain.Entities.Integration.Integration>>()))
                .Returns(dtos);

            // Act
            var result = await _service.GetActiveIntegrationsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(x => x.Status == IntegrationStatus.Connected).Should().BeTrue();
        }

        #endregion
    }
}
