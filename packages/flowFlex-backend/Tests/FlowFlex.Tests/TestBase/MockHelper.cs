using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared.Models;
using Item.ThirdParty.IdentityHub;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using System.Threading;

namespace FlowFlex.Tests.TestBase
{
    /// <summary>
    /// Helper class for creating mock objects
    /// </summary>
    public static class MockHelper
    {
        public static Mock<ILogger<T>> CreateMockLogger<T>()
        {
            return new Mock<ILogger<T>>();
        }

        public static Mock<IWorkflowRepository> CreateMockWorkflowRepository()
        {
            var mock = new Mock<IWorkflowRepository>();
            return mock;
        }

        public static Mock<IStageRepository> CreateMockStageRepository()
        {
            var mock = new Mock<IStageRepository>();
            return mock;
        }

        public static Mock<IOnboardingRepository> CreateMockOnboardingRepository()
        {
            var mock = new Mock<IOnboardingRepository>();
            return mock;
        }

        public static Mock<IHttpContextAccessor> CreateMockHttpContextAccessor(
            bool isPortalToken = false,
            bool hasPortalAccessAttribute = false)
        {
            var mock = new Mock<IHttpContextAccessor>();

            if (isPortalToken || hasPortalAccessAttribute)
            {
                var httpContext = new DefaultHttpContext();

                // Set up user claims for portal token
                if (isPortalToken)
                {
                    var claims = new List<Claim>
                    {
                        new Claim("scope", "portal"),
                        new Claim("token_type", "portal-access")
                    };
                    var identity = new ClaimsIdentity(claims, "TestAuth");
                    httpContext.User = new ClaimsPrincipal(identity);
                }

                // Set up endpoint metadata for PortalAccess attribute
                if (hasPortalAccessAttribute)
                {
                    var endpoint = new Endpoint(
                        requestDelegate: context => Task.CompletedTask,
                        metadata: new EndpointMetadataCollection(
                            new FlowFlex.Application.Filter.PortalAccessAttribute()),
                        displayName: "Test Portal Endpoint");
                    httpContext.SetEndpoint(endpoint);
                }

                mock.Setup(x => x.HttpContext).Returns(httpContext);
            }
            else
            {
                mock.Setup(x => x.HttpContext).Returns((HttpContext)null);
            }

            return mock;
        }

        public static void SetupWorkflowRepositoryGetById(
            Mock<IWorkflowRepository> mockRepo,
            long workflowId,
            Workflow workflow)
        {
            mockRepo.Setup(x => x.GetByIdAsync(It.Is<object>(id => id.Equals(workflowId)), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(workflow);
        }

        public static void SetupStageRepositoryGetById(
            Mock<IStageRepository> mockRepo,
            long stageId,
            Stage stage)
        {
            mockRepo.Setup(x => x.GetByIdAsync(It.Is<object>(id => id.Equals(stageId)), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stage);
        }

        public static void SetupOnboardingRepositoryGetById(
            Mock<IOnboardingRepository> mockRepo,
            long caseId,
            Onboarding onboarding)
        {
            mockRepo.Setup(x => x.GetByIdAsync(It.Is<object>(id => id.Equals(caseId)), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(onboarding);
        }

        public static void SetupIdentityHubClientPermissionCheck(
            Mock<IdentityHubClient> mockClient,
            string permission,
            bool hasPermission)
        {
            mockClient.Setup(x => x.UserRolePermissionCheck(
                    It.IsAny<string>(),
                    It.Is<List<string>>(p => p.Contains(permission))))
                .ReturnsAsync(hasPermission);
        }
    }
}

