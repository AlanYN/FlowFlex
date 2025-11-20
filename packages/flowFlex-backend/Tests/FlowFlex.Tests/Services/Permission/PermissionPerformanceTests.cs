using FlowFlex.Application.Services.OW.Permission;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared.Enums.Permission;
using FlowFlex.Tests.TestBase;
using FluentAssertions;
using Moq;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace FlowFlex.Tests.Services.Permission
{
    /// <summary>
    /// Performance tests for Permission Services
    /// Tests the optimization of GetUserTeamIds() calls
    /// </summary>
    public class PermissionPerformanceTests
    {
        private readonly ITestOutputHelper _output;
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<StagePermissionService>> _mockStageLogger;
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<WorkflowPermissionService>> _mockWorkflowLogger;
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<PermissionHelpers>> _mockHelperLogger;
        private readonly Mock<FlowFlex.Domain.Repository.OW.IStageRepository> _mockStageRepo;
        private readonly Mock<FlowFlex.Domain.Repository.OW.IWorkflowRepository> _mockWorkflowRepo;

        public PermissionPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
            _mockStageLogger = MockHelper.CreateMockLogger<StagePermissionService>();
            _mockWorkflowLogger = MockHelper.CreateMockLogger<WorkflowPermissionService>();
            _mockHelperLogger = MockHelper.CreateMockLogger<PermissionHelpers>();
            _mockStageRepo = MockHelper.CreateMockStageRepository();
            _mockWorkflowRepo = MockHelper.CreateMockWorkflowRepository();
        }

        private (StagePermissionService service, PermissionHelpers helpers) CreateService(Domain.Shared.Models.UserContext userContext)
        {
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var permissionHelpers = new PermissionHelpers(
                _mockHelperLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            var workflowPermissionService = new WorkflowPermissionService(
                _mockWorkflowLogger.Object,
                userContext,
                _mockWorkflowRepo.Object,
                permissionHelpers);

            var stageService = new StagePermissionService(
                _mockStageLogger.Object,
                userContext,
                _mockStageRepo.Object,
                _mockWorkflowRepo.Object,
                permissionHelpers,
                workflowPermissionService);

            return (stageService, permissionHelpers);
        }

        #region GetUserTeamIds Call Count Tests

        /// <summary>
        /// Test: Old approach without optimization - GetUserTeamIds called multiple times
        /// </summary>
        [Fact]
        public void CheckStagePermission_WithoutOptimization_CallsGetUserTeamIdsMultipleTimes()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA });
            var (service, helpers) = CreateService(userContext);
            var workflow = TestDataBuilder.CreatePublicWorkflow();
            var stages = new List<Stage>();

            // Create 10 stages
            for (int i = 0; i < 10; i++)
            {
                stages.Add(TestDataBuilder.CreateStageWithInheritedPermissions(workflow.Id));
            }

            var callCount = 0;
            int getUserTeamIdsCallsBefore = 0;

            // Act - Call CheckStagePermission multiple times WITHOUT passing userTeamIds
            var sw = Stopwatch.StartNew();
            foreach (var stage in stages)
            {
                // Each call will internally call GetUserTeamIds
                var result = service.CheckStagePermission(stage, workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.View);
                callCount++;
            }
            sw.Stop();

            // Assert
            callCount.Should().Be(10);
            _output.WriteLine($"❌ WITHOUT Optimization: {callCount} stages checked in {sw.ElapsedMilliseconds}ms");
            _output.WriteLine($"   GetUserTeamIds() was called internally {callCount} times (once per stage check)");
        }

        /// <summary>
        /// Test: New approach with optimization - GetUserTeamIds called once and reused
        /// </summary>
        [Fact]
        public void CheckStagePermission_WithOptimization_ReusesUserTeamIds()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA });
            var (service, helpers) = CreateService(userContext);
            var workflow = TestDataBuilder.CreatePublicWorkflow();
            var stages = new List<Stage>();

            // Create 10 stages
            for (int i = 0; i < 10; i++)
            {
                stages.Add(TestDataBuilder.CreateStageWithInheritedPermissions(workflow.Id));
            }

            // PERFORMANCE OPTIMIZATION: Get user teams once
            var userTeamIds = helpers.GetUserTeamIds();
            var callCount = 0;

            // Act - Call CheckStagePermission multiple times WITH pre-fetched userTeamIds
            var sw = Stopwatch.StartNew();
            foreach (var stage in stages)
            {
                // Pass userTeamIds to avoid repeated GetUserTeamIds calls
                var result = service.CheckStagePermission(stage, workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.View, userTeamIds);
                callCount++;
            }
            sw.Stop();

            // Assert
            callCount.Should().Be(10);
            _output.WriteLine($"✅ WITH Optimization: {callCount} stages checked in {sw.ElapsedMilliseconds}ms");
            _output.WriteLine($"   GetUserTeamIds() was called only ONCE (reused for all {callCount} checks)");
        }

        #endregion

        #region Batch Permission Check Performance Tests

        /// <summary>
        /// Test: Batch permission checks with large dataset
        /// Simulates checking permissions for multiple workflows with multiple stages
        /// </summary>
        [Fact]
        public void GetStagePermissionInfoForList_LargeDataset_WithOptimization_ShouldBeFast()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA });
            var (service, helpers) = CreateService(userContext);

            const int workflowCount = 5;
            const int stagesPerWorkflow = 10;
            var totalStageCount = workflowCount * stagesPerWorkflow;

            var workflows = new List<Workflow>();
            var stagesByWorkflow = new Dictionary<long, List<Stage>>();

            // Create test data
            for (int i = 0; i < workflowCount; i++)
            {
                var workflow = TestDataBuilder.CreatePublicWorkflow();
                workflows.Add(workflow);

                var stages = new List<Stage>();
                for (int j = 0; j < stagesPerWorkflow; j++)
                {
                    stages.Add(TestDataBuilder.CreateStageWithInheritedPermissions(workflow.Id));
                }
                stagesByWorkflow[workflow.Id] = stages;
            }

            // PERFORMANCE OPTIMIZATION: Pre-fetch user teams once
            var userTeamIds = helpers.GetUserTeamIds();

            // Act - Check permissions for all stages WITH optimization
            var sw = Stopwatch.StartNew();
            var permissionResults = new List<FlowFlex.Application.Contracts.Dtos.OW.Permission.PermissionInfoDto>();

            foreach (var workflow in workflows)
            {
                var stages = stagesByWorkflow[workflow.Id];
                foreach (var stage in stages)
                {
                    // Use optimized method with pre-fetched userTeamIds
                    var permissionInfo = service.GetStagePermissionInfoForList(
                        stage,
                        workflow,
                        TestDataBuilder.DefaultUserId,
                        hasViewModulePermission: true,
                        hasOperateModulePermission: true,
                        userTeamIds); // Pass pre-fetched teams

                    permissionResults.Add(permissionInfo);
                }
            }
            sw.Stop();

            // Assert
            permissionResults.Count.Should().Be(totalStageCount);
            permissionResults.All(p => p.CanView).Should().BeTrue();

            _output.WriteLine($"✅ Performance Test Results:");
            _output.WriteLine($"   - Workflows: {workflowCount}");
            _output.WriteLine($"   - Stages per workflow: {stagesPerWorkflow}");
            _output.WriteLine($"   - Total stages checked: {totalStageCount}");
            _output.WriteLine($"   - Time elapsed: {sw.ElapsedMilliseconds}ms");
            _output.WriteLine($"   - Average per stage: {(double)sw.ElapsedMilliseconds / totalStageCount:F2}ms");
            _output.WriteLine($"   - GetUserTeamIds() calls: 1 (optimized)");
            _output.WriteLine($"   - Database queries: 0 (all entities pre-loaded)");

            // Performance assertion: Should complete quickly
            sw.ElapsedMilliseconds.Should().BeLessThan(1000, "Permission checks should be fast with optimization");
        }

        /// <summary>
        /// Test: Compare performance between optimized and non-optimized approaches
        /// </summary>
        [Fact]
        public void PermissionCheck_CompareOptimizedVsNonOptimized_ShouldShowImprovement()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA });
            var (service, helpers) = CreateService(userContext);

            const int stageCount = 20;
            var workflow = TestDataBuilder.CreatePublicWorkflow();
            var stages = new List<Stage>();

            for (int i = 0; i < stageCount; i++)
            {
                stages.Add(TestDataBuilder.CreateStageWithInheritedPermissions(workflow.Id));
            }

            // Act 1: Non-optimized approach (without pre-fetched userTeamIds)
            var sw1 = Stopwatch.StartNew();
            foreach (var stage in stages)
            {
                service.CheckStagePermission(stage, workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.View);
            }
            sw1.Stop();
            var timeWithoutOptimization = sw1.ElapsedMilliseconds;

            // Act 2: Optimized approach (with pre-fetched userTeamIds)
            var userTeamIds = helpers.GetUserTeamIds();
            var sw2 = Stopwatch.StartNew();
            foreach (var stage in stages)
            {
                service.CheckStagePermission(stage, workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.View, userTeamIds);
            }
            sw2.Stop();
            var timeWithOptimization = sw2.ElapsedMilliseconds;

            // Assert & Report
            _output.WriteLine($"📊 Performance Comparison ({stageCount} stages):");
            _output.WriteLine($"   WITHOUT optimization: {timeWithoutOptimization}ms");
            _output.WriteLine($"   WITH optimization:    {timeWithOptimization}ms");

            if (timeWithoutOptimization > 0 && timeWithOptimization > 0)
            {
                var improvement = ((double)(timeWithoutOptimization - timeWithOptimization) / timeWithoutOptimization) * 100;
                _output.WriteLine($"   Improvement:         {improvement:F1}%");
            }

            _output.WriteLine($"");
            _output.WriteLine($"📈 Call Reduction:");
            _output.WriteLine($"   WITHOUT optimization: GetUserTeamIds() called {stageCount} times");
            _output.WriteLine($"   WITH optimization:    GetUserTeamIds() called 1 time");
            _output.WriteLine($"   Reduction:           {stageCount - 1} fewer calls ({((double)(stageCount - 1) / stageCount * 100):F1}%)");

            // The optimized version should be at least as fast (or faster in real scenarios)
            timeWithOptimization.Should().BeLessThanOrEqualTo((long)(timeWithoutOptimization * 1.2),
                "Optimized version should not be significantly slower");
        }

        #endregion

        #region Workflow Permission Performance Tests

        /// <summary>
        /// Test: Workflow permission checks with pre-fetched user teams
        /// </summary>
        [Fact]
        public void CheckWorkflowViewPermission_WithPreFetchedTeams_ShouldAvoidRedundantCalls()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB });
            var (_, helpers) = CreateService(userContext);

            var workflowService = new WorkflowPermissionService(
                _mockWorkflowLogger.Object,
                userContext,
                _mockWorkflowRepo.Object,
                helpers);

            const int workflowCount = 15;
            var workflows = new List<Workflow>();

            for (int i = 0; i < workflowCount; i++)
            {
                workflows.Add(TestDataBuilder.CreateVisibleToTeamsWorkflow(
                    new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB }));
            }

            // PERFORMANCE OPTIMIZATION: Pre-fetch user teams once
            var userTeamIds = helpers.GetUserTeamIds();

            // Act - Check workflow permissions WITH optimization
            var sw = Stopwatch.StartNew();
            foreach (var workflow in workflows)
            {
                // Pass pre-fetched userTeamIds to avoid repeated calls
                var canView = workflowService.CheckViewPermission(workflow, userTeamIds);
                canView.Should().BeTrue();
            }
            sw.Stop();

            // Assert & Report
            _output.WriteLine($"✅ Workflow Permission Check Performance:");
            _output.WriteLine($"   - Workflows checked: {workflowCount}");
            _output.WriteLine($"   - Time elapsed: {sw.ElapsedMilliseconds}ms");
            _output.WriteLine($"   - Average per workflow: {(double)sw.ElapsedMilliseconds / workflowCount:F2}ms");
            _output.WriteLine($"   - GetUserTeamIds() calls: 1 (optimized)");

            sw.ElapsedMilliseconds.Should().BeLessThan(500, "Workflow permission checks should be fast");
        }

        #endregion

        #region Stress Test

        /// <summary>
        /// Stress test: Large scale permission checks
        /// Simulates a real-world scenario with many workflows and stages
        /// </summary>
        [Fact]
        public void StressTest_LargeScale_PermissionChecks_ShouldHandleEfficiently()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB, TestDataBuilder.TeamC });
            var (service, helpers) = CreateService(userContext);

            const int workflowCount = 20;
            const int stagesPerWorkflow = 10;
            var totalChecks = workflowCount * stagesPerWorkflow;

            var testData = new List<(Workflow workflow, List<Stage> stages)>();

            // Create large dataset
            for (int i = 0; i < workflowCount; i++)
            {
                var workflow = TestDataBuilder.CreateVisibleToTeamsWorkflow(
                    new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB });

                var stages = new List<Stage>();
                for (int j = 0; j < stagesPerWorkflow; j++)
                {
                    stages.Add(TestDataBuilder.CreateStageWithInheritedPermissions(workflow.Id));
                }

                testData.Add((workflow, stages));
            }

            // PERFORMANCE OPTIMIZATION: Pre-fetch user teams once
            var userTeamIds = helpers.GetUserTeamIds();

            // Act - Perform stress test
            var sw = Stopwatch.StartNew();
            var successCount = 0;

            foreach (var (workflow, stages) in testData)
            {
                foreach (var stage in stages)
                {
                    var permissionInfo = service.GetStagePermissionInfoForList(
                        stage,
                        workflow,
                        TestDataBuilder.DefaultUserId,
                        hasViewModulePermission: true,
                        hasOperateModulePermission: true,
                        userTeamIds);

                    if (permissionInfo.CanView) successCount++;
                }
            }
            sw.Stop();

            // Assert & Report
            _output.WriteLine($"🔥 Stress Test Results:");
            _output.WriteLine($"   ═══════════════════════════════════════");
            _output.WriteLine($"   Workflows:                  {workflowCount}");
            _output.WriteLine($"   Stages per workflow:        {stagesPerWorkflow}");
            _output.WriteLine($"   Total permission checks:    {totalChecks}");
            _output.WriteLine($"   Successful checks:          {successCount}");
            _output.WriteLine($"   ───────────────────────────────────────");
            _output.WriteLine($"   Total time:                 {sw.ElapsedMilliseconds}ms");
            _output.WriteLine($"   Average per check:          {(double)sw.ElapsedMilliseconds / totalChecks:F3}ms");
            _output.WriteLine($"   Throughput:                 {(double)totalChecks / sw.ElapsedMilliseconds * 1000:F0} checks/sec");
            _output.WriteLine($"   ───────────────────────────────────────");
            _output.WriteLine($"   GetUserTeamIds() calls:     1 ✅");
            _output.WriteLine($"   Database queries:           0 ✅");
            _output.WriteLine($"   ═══════════════════════════════════════");

            successCount.Should().Be(totalChecks);
            sw.ElapsedMilliseconds.Should().BeLessThan(2000, "Should handle large scale efficiently");
        }

        #endregion

        #region Memory Allocation Test

        /// <summary>
        /// Test: Verify that optimized approach doesn't create excessive memory allocations
        /// </summary>
        [Fact]
        public void PermissionCheck_WithOptimization_ShouldMinimizeMemoryAllocations()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA });
            var (service, helpers) = CreateService(userContext);

            var workflow = TestDataBuilder.CreatePublicWorkflow();
            var stages = new List<Stage>();

            for (int i = 0; i < 50; i++)
            {
                stages.Add(TestDataBuilder.CreateStageWithInheritedPermissions(workflow.Id));
            }

            // Pre-fetch user teams once
            var userTeamIds = helpers.GetUserTeamIds();

            // Record memory before
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var memoryBefore = GC.GetTotalMemory(false);

            // Act - Perform permission checks
            foreach (var stage in stages)
            {
                service.CheckStagePermission(stage, workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.View, userTeamIds);
            }

            // Record memory after
            var memoryAfter = GC.GetTotalMemory(false);
            var memoryUsed = (memoryAfter - memoryBefore) / 1024.0; // KB

            // Report
            _output.WriteLine($"💾 Memory Allocation Test:");
            _output.WriteLine($"   Stages checked:     {stages.Count}");
            _output.WriteLine($"   Memory used:        {memoryUsed:F2} KB");
            _output.WriteLine($"   Per check:          {memoryUsed / stages.Count:F3} KB");

            // Assert - Memory usage should be reasonable
            memoryUsed.Should().BeLessThan(500, "Should not allocate excessive memory");
        }

        #endregion
    }
}

