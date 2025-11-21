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
    public class QuickLinkServiceTests
    {
        private readonly Mock<IQuickLinkRepository> _mockRepository;
        private readonly Mock<IIntegrationRepository> _mockIntegrationRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<QuickLinkService>> _mockLogger;
        private readonly UserContext _userContext;
        private readonly QuickLinkService _service;

        public QuickLinkServiceTests()
        {
            _mockRepository = new Mock<IQuickLinkRepository>();
            _mockIntegrationRepository = new Mock<IIntegrationRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = MockHelper.CreateMockLogger<QuickLinkService>();
            _userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);

            _service = new QuickLinkService(
                _mockRepository.Object,
                _mockIntegrationRepository.Object,
                _mockMapper.Object,
                _userContext,
                _mockLogger.Object
            );
        }

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithValidInput_ShouldReturnQuickLinkId()
        {
            // Arrange
            var input = new QuickLinkInputDto
            {
                IntegrationId = 123,
                LinkName = "View in CRM",
                TargetUrl = "https://crm.com/contacts/{id}",
                DisplayIcon = "external-link",
                RedirectType = RedirectType.Direct,
                IsActive = true
            };

            var entity = new QuickLink
            {
                Id = 789,
                IntegrationId = input.IntegrationId,
                LinkName = input.LinkName,
                TargetUrl = input.TargetUrl,
                UrlParameters = "[]"
            };

            var integration = new Domain.Entities.Integration.Integration
            {
                Id = input.IntegrationId,
                Name = "Test Integration"
            };

            _mockIntegrationRepository.Setup(r => r.GetByIdAsync(It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(integration);
            _mockMapper.Setup(m => m.Map<QuickLink>(It.IsAny<QuickLinkInputDto>()))
                .Returns(entity);
            _mockRepository.Setup(r => r.InsertReturnSnowflakeIdAsync(It.IsAny<QuickLink>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(789L);

            // Act
            var result = await _service.CreateAsync(input);

            // Assert
            result.Should().Be(789L);
            _mockRepository.Verify(r => r.InsertReturnSnowflakeIdAsync(It.IsAny<QuickLink>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region GetByIntegrationIdAsync Tests

        [Fact]
        public async Task GetByIntegrationIdAsync_WithValidId_ShouldReturnList()
        {
            // Arrange
            var integrationId = 123L;
            var entities = new List<QuickLink>
            {
                new QuickLink { Id = 1, IntegrationId = integrationId, LinkName = "View", SortOrder = 1, UrlParameters = "[]" },
                new QuickLink { Id = 2, IntegrationId = integrationId, LinkName = "Edit", SortOrder = 2, UrlParameters = "[]" }
            };

            var dtos = new List<QuickLinkOutputDto>
            {
                new QuickLinkOutputDto { Id = 1, IntegrationId = integrationId, LinkName = "View", SortOrder = 1 },
                new QuickLinkOutputDto { Id = 2, IntegrationId = integrationId, LinkName = "Edit", SortOrder = 2 }
            };

            _mockRepository.Setup(r => r.GetByIntegrationIdAsync(It.IsAny<long>()))
                .ReturnsAsync(entities);
            _mockMapper.Setup(m => m.Map<List<QuickLinkOutputDto>>(It.IsAny<List<QuickLink>>()))
                .Returns(dtos);

            // Act
            var result = await _service.GetByIntegrationIdAsync(integrationId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(x => x.IntegrationId == integrationId).Should().BeTrue();
            result.Should().BeInAscendingOrder(x => x.SortOrder);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnDto()
        {
            // Arrange
            var quickLinkId = 789L;
            var entity = new QuickLink
            {
                Id = quickLinkId,
                IntegrationId = 123,
                LinkName = "View in CRM",
                TargetUrl = "https://crm.com/contacts/{id}",
                IsActive = true,
                UrlParameters = "[]"
            };

            var expectedDto = new QuickLinkOutputDto
            {
                Id = quickLinkId,
                IntegrationId = 123,
                LinkName = "View in CRM",
                TargetUrl = "https://crm.com/contacts/{id}",
                IsActive = true
            };

            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);
            _mockMapper.Setup(m => m.Map<QuickLinkOutputDto>(It.IsAny<QuickLink>()))
                .Returns(expectedDto);

            // Act
            var result = await _service.GetByIdAsync(quickLinkId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(quickLinkId);
            result.LinkName.Should().Be("View in CRM");
        }

        #endregion
    }
}
