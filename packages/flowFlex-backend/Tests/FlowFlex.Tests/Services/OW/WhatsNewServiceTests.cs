using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.WhatsNew;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Services.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Tests.TestBase;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FlowFlex.Tests.Services.OW
{
    /// <summary>
    /// Unit tests for WhatsNewService.
    /// Covers cache hit/miss, read marking, CRUD lifecycle, and XSS sanitization.
    /// Requirements: 9, 10, 13
    /// </summary>
    public class WhatsNewServiceTests
    {
        private readonly Mock<IWhatsNewRepository> _mockWhatsNewRepo;
        private readonly Mock<IWhatsNewReadStatusRepository> _mockReadStatusRepo;
        private readonly Mock<IDistributedCacheService> _mockCache;
        private readonly UserContext _userContext;
        private readonly WhatsNewService _service;

        private const string AppCode = "WFE";
        private const string TenantId = "tenant-1";
        private const string UserId = "123";
        private string ExpectedCacheKey => $"whats-new:unread:{UserId}";

        public WhatsNewServiceTests()
        {
            _mockWhatsNewRepo = new Mock<IWhatsNewRepository>();
            _mockReadStatusRepo = new Mock<IWhatsNewReadStatusRepository>();
            _mockCache = new Mock<IDistributedCacheService>();

            _userContext = new UserContext
            {
                UserId = UserId,
                AppCode = AppCode,
                TenantId = TenantId
            };

            _service = new WhatsNewService(
                _mockWhatsNewRepo.Object,
                _mockReadStatusRepo.Object,
                _mockCache.Object,
                _userContext,
                MockHelper.CreateMockLogger<WhatsNewService>().Object);
        }

        #region GetUnreadCountAsync Tests

        [Fact]
        public async Task GetUnreadCountAsync_CacheHit_ReturnsCachedValue()
        {
            // Arrange
            _mockCache
                .Setup(c => c.GetAsync<string>(ExpectedCacheKey))
                .ReturnsAsync("7");

            // Act
            var result = await _service.GetUnreadCountAsync();

            // Assert
            result.Should().Be(7);
            _mockWhatsNewRepo.Verify(r => r.GetPublishedListAsync(It.IsAny<int>()), Times.Never,
                "DB should not be queried when cache has a valid value");
            _mockCache.Verify(
                c => c.SetAsync<string>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()),
                Times.Never,
                "Cache should not be written again on a cache hit");
        }

        [Fact]
        public async Task GetUnreadCountAsync_CacheMiss_QueriesDbAndCaches()
        {
            // Arrange
            var publishedItems = new List<WhatsNew>
            {
                new WhatsNew { Id = 101 },
                new WhatsNew { Id = 102 },
                new WhatsNew { Id = 103 }
            };

            _mockCache
                .Setup(c => c.GetAsync<string>(ExpectedCacheKey))
                .ReturnsAsync((string?)null);

            _mockWhatsNewRepo
                .Setup(r => r.GetPublishedListAsync(int.MaxValue))
                .ReturnsAsync(publishedItems);

            _mockReadStatusRepo
                .Setup(r => r.GetUnreadCountAsync(
                    long.Parse(UserId),
                    It.Is<List<long>>(ids => ids.Count == 3)))
                .ReturnsAsync(2);

            // Act
            var result = await _service.GetUnreadCountAsync();

            // Assert
            result.Should().Be(2);
            _mockWhatsNewRepo.Verify(r => r.GetPublishedListAsync(int.MaxValue), Times.Once);
            _mockCache.Verify(
                c => c.SetAsync<string>(
                    ExpectedCacheKey,
                    "2",
                    It.Is<TimeSpan?>(t => t.HasValue && t.Value == TimeSpan.FromMinutes(10))),
                Times.Once,
                "Unread count should be cached after DB query");
        }

        #endregion

        #region MarkReadAsync Tests

        [Fact]
        public async Task MarkReadAsync_Success_InvalidatesCache()
        {
            // Arrange
            long itemId = 42;

            _mockReadStatusRepo
                .Setup(r => r.MarkReadAsync(itemId, long.Parse(UserId)))
                .Returns(Task.CompletedTask);

            _mockCache
                .Setup(c => c.RemoveAsync(ExpectedCacheKey))
                .Returns(Task.CompletedTask);

            // Act
            await _service.MarkReadAsync(itemId);

            // Assert
            _mockReadStatusRepo.Verify(
                r => r.MarkReadAsync(itemId, long.Parse(UserId)),
                Times.Once);
            _mockCache.Verify(c => c.RemoveAsync(ExpectedCacheKey), Times.Once,
                "Cache must be invalidated after marking an item as read");
        }

        #endregion

        #region MarkAllReadAsync Tests

        [Fact]
        public async Task MarkAllReadAsync_Success_InvalidatesCache()
        {
            // Arrange
            var publishedItems = new List<WhatsNew>
            {
                new WhatsNew { Id = 1 },
                new WhatsNew { Id = 2 },
                new WhatsNew { Id = 3 }
            };

            _mockWhatsNewRepo
                .Setup(r => r.GetPublishedListAsync(int.MaxValue))
                .ReturnsAsync(publishedItems);

            _mockReadStatusRepo
                .Setup(r => r.MarkAllReadAsync(
                    It.Is<List<long>>(ids => ids.Count == 3),
                    long.Parse(UserId)))
                .Returns(Task.CompletedTask);

            _mockCache
                .Setup(c => c.RemoveAsync(ExpectedCacheKey))
                .Returns(Task.CompletedTask);

            // Act
            await _service.MarkAllReadAsync();

            // Assert
            _mockReadStatusRepo.Verify(
                r => r.MarkAllReadAsync(
                    It.Is<List<long>>(ids => ids.Count == 3),
                    long.Parse(UserId)),
                Times.Once);
            _mockCache.Verify(c => c.RemoveAsync(ExpectedCacheKey), Times.Once,
                "Cache must be invalidated after marking all items as read");
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithPublishNow_SetsPublishTime()
        {
            // Arrange
            var request = new CreateWhatsNewRequest
            {
                Title = "New Feature",
                Summary = "Summary",
                Content = "<p>Plain content</p>",
                Category = "NewFeature",
                Status = 1  // Published
            };

            WhatsNew? capturedEntity = null;
            _mockWhatsNewRepo
                .Setup(r => r.InsertAsync(It.IsAny<WhatsNew>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
                .Callback<WhatsNew, CancellationToken, bool>((e, ct, copy) => capturedEntity = e)
                .ReturnsAsync(true);

            var beforeCreate = DateTimeOffset.UtcNow;

            // Act
            await _service.CreateAsync(request);

            var afterCreate = DateTimeOffset.UtcNow;

            // Assert
            capturedEntity.Should().NotBeNull();
            capturedEntity!.Status.Should().Be(1);
            capturedEntity.PublishTime.Should().NotBeNull("PublishTime must be set when creating with status=Published");
            capturedEntity.PublishTime!.Value.Should().BeOnOrAfter(beforeCreate);
            capturedEntity.PublishTime.Value.Should().BeOnOrBefore(afterCreate);
        }

        [Fact]
        public async Task CreateAsync_WithDraft_NoPublishTime()
        {
            // Arrange
            var request = new CreateWhatsNewRequest
            {
                Title = "Draft Feature",
                Summary = "Summary",
                Content = "<p>Content</p>",
                Category = "Improvement",
                Status = 0  // Draft
            };

            WhatsNew? capturedEntity = null;
            _mockWhatsNewRepo
                .Setup(r => r.InsertAsync(It.IsAny<WhatsNew>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
                .Callback<WhatsNew, CancellationToken, bool>((e, ct, copy) => capturedEntity = e)
                .ReturnsAsync(true);

            // Act
            await _service.CreateAsync(request);

            // Assert
            capturedEntity.Should().NotBeNull();
            capturedEntity!.Status.Should().Be(0);
            capturedEntity.PublishTime.Should().BeNull("PublishTime must NOT be set for draft entries");
        }

        [Fact]
        public async Task CreateAsync_SanitizesHtmlContent()
        {
            // Arrange
            var maliciousContent = "<p>Safe text</p><script>alert('xss')</script><img src='x' onerror='alert(1)'>";
            var request = new CreateWhatsNewRequest
            {
                Title = "Security Test",
                Summary = "Summary",
                Content = maliciousContent,
                Category = "Announcement",
                Status = 0
            };

            WhatsNew? capturedEntity = null;
            _mockWhatsNewRepo
                .Setup(r => r.InsertAsync(It.IsAny<WhatsNew>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
                .Callback<WhatsNew, CancellationToken, bool>((e, ct, copy) => capturedEntity = e)
                .ReturnsAsync(true);

            // Act
            await _service.CreateAsync(request);

            // Assert
            capturedEntity.Should().NotBeNull();
            capturedEntity!.Content.Should().NotContain("<script>",
                "script tags must be stripped by XSS sanitizer");
            capturedEntity.Content.Should().NotContain("onerror=",
                "event handler attributes must be stripped by XSS sanitizer");
            capturedEntity.Content.Should().Contain("Safe text",
                "safe content must be preserved");
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_DraftToPublished_SetsPublishTime()
        {
            // Arrange
            long itemId = 10;
            var existingEntity = new WhatsNew
            {
                Id = itemId,
                Title = "Old Title",
                Summary = "Old Summary",
                Content = "<p>Old content</p>",
                Category = "Improvement",
                Status = 0,  // Draft
                PublishTime = null,
                IsValid = true
            };

            _mockWhatsNewRepo
                .Setup(r => r.GetByIdAsync(
                    It.Is<object>(id => id.Equals((object)itemId)),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEntity);

            WhatsNew? capturedEntity = null;
            _mockWhatsNewRepo
                .Setup(r => r.UpdateAsync(
                    It.IsAny<WhatsNew>(),
                    null,
                    It.IsAny<CancellationToken>(),
                    false))
                .Callback<WhatsNew, System.Linq.Expressions.Expression<Func<WhatsNew, object>>?, CancellationToken, bool>(
                    (e, cols, ct, copy) => capturedEntity = e)
                .ReturnsAsync(true);

            var request = new UpdateWhatsNewRequest
            {
                Title = "Updated Title",
                Summary = "Updated Summary",
                Content = "<p>Updated content</p>",
                Category = "Improvement",
                Status = 1  // Draft → Published transition
            };

            var beforeUpdate = DateTimeOffset.UtcNow;

            // Act
            var result = await _service.UpdateAsync(itemId, request);

            var afterUpdate = DateTimeOffset.UtcNow;

            // Assert
            result.Should().BeTrue();
            capturedEntity.Should().NotBeNull();
            capturedEntity!.Status.Should().Be(1);
            capturedEntity.PublishTime.Should().NotBeNull("PublishTime must be set when transitioning from Draft to Published");
            capturedEntity.PublishTime!.Value.Should().BeOnOrAfter(beforeUpdate);
            capturedEntity.PublishTime.Value.Should().BeOnOrBefore(afterUpdate);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_SetsIsValidFalse()
        {
            // Arrange
            long itemId = 99;
            var existingEntity = new WhatsNew
            {
                Id = itemId,
                Title = "To Be Deleted",
                Summary = "Summary",
                Content = "<p>Content</p>",
                Category = "BugFix",
                Status = 1,
                IsValid = true
            };

            _mockWhatsNewRepo
                .Setup(r => r.GetByIdAsync(
                    It.Is<object>(id => id.Equals((object)itemId)),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEntity);

            WhatsNew? capturedEntity = null;
            _mockWhatsNewRepo
                .Setup(r => r.UpdateAsync(
                    It.IsAny<WhatsNew>(),
                    null,
                    It.IsAny<CancellationToken>(),
                    false))
                .Callback<WhatsNew, System.Linq.Expressions.Expression<Func<WhatsNew, object>>?, CancellationToken, bool>(
                    (e, cols, ct, copy) => capturedEntity = e)
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(itemId);

            // Assert
            result.Should().BeTrue();
            capturedEntity.Should().NotBeNull();
            capturedEntity!.IsValid.Should().BeFalse(
                "soft-delete must set IsValid to false, not physically remove the record");
            _mockWhatsNewRepo.Verify(
                r => r.UpdateAsync(
                    It.IsAny<WhatsNew>(),
                    null,
                    It.IsAny<CancellationToken>(),
                    false),
                Times.Once);
        }

        #endregion
    }
}
