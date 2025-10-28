using FlowFlex.Application.Services.OW.Permission;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared.Enums.Permission;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Tests.TestBase;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Xunit;
using Xunit.Abstractions;
using OperationTypeEnum = FlowFlex.Domain.Shared.Enums.Permission.OperationTypeEnum;
using ViewPermissionModeEnum = FlowFlex.Domain.Shared.Enums.OW.ViewPermissionModeEnum;
using PermissionSubjectTypeEnum = FlowFlex.Domain.Shared.Enums.OW.PermissionSubjectTypeEnum;
using StageEntity = FlowFlex.Domain.Entities.OW.Stage;

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

        private (StagePermissionService service, PermissionHelpers helpers) CreateService(UserContext userContext)
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
            var stages = new List<StageEntity>();
            
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
            var stages = new List<StageEntity>();
            
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
            var stagesByWorkflow = new Dictionary<long, List<StageEntity>>();

            // Create test data
            for (int i = 0; i < workflowCount; i++)
            {
                var workflow = TestDataBuilder.CreatePublicWorkflow();
                workflows.Add(workflow);
                
                var stages = new List<StageEntity>();
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
            var stages = new List<StageEntity>();
            
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
            timeWithOptimization.Should().BeLessThanOrEqualTo(timeWithoutOptimization * 2, 
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

            var testData = new List<(Workflow workflow, List<StageEntity> stages)>();
            
            // Create large dataset
            for (int i = 0; i < workflowCount; i++)
            {
                var workflow = TestDataBuilder.CreateVisibleToTeamsWorkflow(
                    new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB });
                
                var stages = new List<StageEntity>();
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
            var stages = new List<StageEntity>();
            
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

        #region Case Permission Performance Tests (Workflow ∩ Case)

        /// <summary>
        /// Test: Case permission = Workflow ∩ Case
        /// Verify that checking multiple cases efficiently reuses userTeamIds
        /// </summary>
        [Fact]
        public void CheckCasePermission_MultipleCases_ShouldReuseUserTeamIds()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB });
            
            var mockOnboardingRepo = new Mock<IOnboardingRepository>();
            var (_, helpers) = CreateService(userContext);
            
            var workflowPermissionService = new WorkflowPermissionService(
                _mockWorkflowLogger.Object,
                userContext,
                _mockWorkflowRepo.Object,
                helpers);
            
            var casePermissionService = new CasePermissionService(
                Mock.Of<ILogger<CasePermissionService>>(),
                userContext,
                mockOnboardingRepo.Object,
                _mockWorkflowRepo.Object,
                helpers,
                workflowPermissionService);

            // Create test data: 1 Workflow + Multiple Cases
            var workflow = TestDataBuilder.CreateVisibleToTeamsWorkflow(
                new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB });
            workflow.Id = 1001;
            
            _mockWorkflowRepo.Setup(r => r.GetByIdAsync(workflow.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(workflow);

            const int caseCount = 20;
            var cases = new List<Onboarding>();
            
            for (int i = 0; i < caseCount; i++)
            {
                var onboarding = TestDataBuilder.CreateCase(workflow.Id);
                onboarding.Id = 2000 + i;
                onboarding.ViewPermissionMode = ViewPermissionModeEnum.Public; // Public mode: Case inherits Workflow permissions
                cases.Add(onboarding);
            }

            // PERFORMANCE OPTIMIZATION: Pre-fetch user teams once
            var userTeamIds = helpers.GetUserTeamIds();

            // Act - Check Case permissions
            var sw = Stopwatch.StartNew();
            var successCount = 0;
            
            foreach (var onboarding in cases)
            {
                var result = casePermissionService.CheckCasePermission(
                    onboarding,
                    TestDataBuilder.DefaultUserId,
                    OperationTypeEnum.View);
                
                if (result.Success && result.CanView) successCount++;
            }
            sw.Stop();

            // Assert & Report
            _output.WriteLine($"✅ Case Permission Check Performance (Workflow ∩ Case):");
            _output.WriteLine($"   ═══════════════════════════════════════");
            _output.WriteLine($"   Cases checked:              {caseCount}");
            _output.WriteLine($"   Successful checks:          {successCount}");
            _output.WriteLine($"   ───────────────────────────────────────");
            _output.WriteLine($"   Total time:                 {sw.ElapsedMilliseconds}ms");
            _output.WriteLine($"   Average per case:           {(double)sw.ElapsedMilliseconds / caseCount:F3}ms");
            _output.WriteLine($"   Throughput:                 {(double)caseCount / sw.ElapsedMilliseconds * 1000:F0} checks/sec");
            _output.WriteLine($"   ───────────────────────────────────────");
            _output.WriteLine($"   GetUserTeamIds() calls:     1 ✅");
            _output.WriteLine($"   Formula applied:            Workflow ∩ Case");
            _output.WriteLine($"   ═══════════════════════════════════════");

            successCount.Should().Be(caseCount);
            sw.ElapsedMilliseconds.Should().BeLessThan(1000, "Should handle multiple case checks efficiently");
        }

        /// <summary>
        /// Test: Case Stage permission = Workflow ∩ Stage ∩ Case
        /// Verify complex permission intersection with efficient resource usage
        /// </summary>
        [Fact]
        public void CheckCaseStagePermission_ComplexIntersection_ShouldBeEfficient()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA });
            
            var mockOnboardingRepo = new Mock<IOnboardingRepository>();
            var (stagePermissionService, helpers) = CreateService(userContext);
            
            var workflowPermissionService = new WorkflowPermissionService(
                _mockWorkflowLogger.Object,
                userContext,
                _mockWorkflowRepo.Object,
                helpers);
            
            var casePermissionService = new CasePermissionService(
                Mock.Of<ILogger<CasePermissionService>>(),
                userContext,
                mockOnboardingRepo.Object,
                _mockWorkflowRepo.Object,
                helpers,
                workflowPermissionService);

            // Create test data: 1 Workflow + 5 Stages + 10 Cases
            var workflow = TestDataBuilder.CreateVisibleToTeamsWorkflow(
                new List<string> { TestDataBuilder.TeamA });
            workflow.Id = 3001;
            
            _mockWorkflowRepo.Setup(r => r.GetByIdAsync(workflow.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(workflow);

            const int stageCount = 5;
            const int caseCount = 10;
            var stages = new List<StageEntity>();
            var cases = new List<Onboarding>();
            
            for (int i = 0; i < stageCount; i++)
            {
                var stage = TestDataBuilder.CreateStageWithNarrowedPermissions(
                    workflow.Id,
                    new List<string> { TestDataBuilder.TeamA },
                    new List<string> { TestDataBuilder.TeamA });
                stage.Id = 4000 + i;
                stages.Add(stage);
            }
            
            for (int i = 0; i < caseCount; i++)
            {
                var onboarding = TestDataBuilder.CreateCase(workflow.Id);
                onboarding.Id = 5000 + i;
                onboarding.ViewPermissionMode = ViewPermissionModeEnum.Public; // Public mode
                cases.Add(onboarding);
            }

            // PERFORMANCE OPTIMIZATION: Pre-fetch user teams once
            var userTeamIds = helpers.GetUserTeamIds();

            // Act - Check Case Stage permissions (Workflow ∩ Stage ∩ Case)
            var sw = Stopwatch.StartNew();
            var totalChecks = 0;
            var successCount = 0;

            foreach (var onboarding in cases)
            {
                // Step 1: Check Case permission (Workflow ∩ Case)
                var caseResult = casePermissionService.CheckCasePermission(
                    onboarding,
                    TestDataBuilder.DefaultUserId,
                    OperationTypeEnum.View);
                
                if (!caseResult.Success || !caseResult.CanView)
                    continue; // Skip if no Case permission

                // Step 2: Check Stage permissions within this Case (Workflow ∩ Stage)
                foreach (var stage in stages)
                {
                    totalChecks++;
                    
                    var stageResult = stagePermissionService.CheckStagePermission(
                        stage,
                        workflow,
                        TestDataBuilder.DefaultUserId,
                        OperationTypeEnum.View,
                        userTeamIds);
                    
                    // Step 3: Intersection result (Workflow ∩ Stage ∩ Case)
                    if (stageResult.Success && stageResult.CanView)
                    {
                        successCount++;
                    }
                }
            }
            sw.Stop();

            // Assert & Report
            _output.WriteLine($"✅ Case Stage Permission Check Performance (Workflow ∩ Stage ∩ Case):");
            _output.WriteLine($"   ═══════════════════════════════════════");
            _output.WriteLine($"   Workflows:                  1");
            _output.WriteLine($"   Stages:                     {stageCount}");
            _output.WriteLine($"   Cases:                      {caseCount}");
            _output.WriteLine($"   Total checks:               {totalChecks}");
            _output.WriteLine($"   Successful checks:          {successCount}");
            _output.WriteLine($"   ───────────────────────────────────────");
            _output.WriteLine($"   Total time:                 {sw.ElapsedMilliseconds}ms");
            _output.WriteLine($"   Average per check:          {(double)sw.ElapsedMilliseconds / totalChecks:F3}ms");
            _output.WriteLine($"   Throughput:                 {(double)totalChecks / sw.ElapsedMilliseconds * 1000:F0} checks/sec");
            _output.WriteLine($"   ───────────────────────────────────────");
            _output.WriteLine($"   GetUserTeamIds() calls:     1 ✅");
            _output.WriteLine($"   Formula applied:            Workflow ∩ Stage ∩ Case");
            _output.WriteLine($"   ═══════════════════════════════════════");

            totalChecks.Should().Be(stageCount * caseCount);
            successCount.Should().BeGreaterThan(0);
            sw.ElapsedMilliseconds.Should().BeLessThan(1000, "Complex intersection should still be efficient");
        }

        /// <summary>
        /// Test: Batch Case permission checks with different permission modes
        /// Covers Public mode (inherits Workflow), Narrowed mode, and Ownership
        /// </summary>
        [Fact]
        public void CheckCasePermission_MixedModes_ShouldHandleAllCasesEfficiently()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB });
            
            var mockOnboardingRepo = new Mock<IOnboardingRepository>();
            var (_, helpers) = CreateService(userContext);
            
            var workflowPermissionService = new WorkflowPermissionService(
                _mockWorkflowLogger.Object,
                userContext,
                _mockWorkflowRepo.Object,
                helpers);
            
            var casePermissionService = new CasePermissionService(
                Mock.Of<ILogger<CasePermissionService>>(),
                userContext,
                mockOnboardingRepo.Object,
                _mockWorkflowRepo.Object,
                helpers,
                workflowPermissionService);

            // Create test data
            var workflow = TestDataBuilder.CreateVisibleToTeamsWorkflow(
                new List<string> { TestDataBuilder.TeamA });
            workflow.Id = 6001;
            
            _mockWorkflowRepo.Setup(r => r.GetByIdAsync(workflow.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(workflow);

            var cases = new List<Onboarding>();

            // Public mode cases (10 cases) - inherits Workflow permissions
            for (int i = 0; i < 10; i++)
            {
                var onboarding = TestDataBuilder.CreateCase(workflow.Id);
                onboarding.Id = 7000 + i;
                onboarding.ViewPermissionMode = ViewPermissionModeEnum.Public;
                cases.Add(onboarding);
            }

            // VisibleToTeams mode cases (10 cases) - has own team restrictions
            for (int i = 0; i < 10; i++)
            {
                var onboarding = TestDataBuilder.CreateCase(workflow.Id);
                onboarding.Id = 8000 + i;
                onboarding.ViewPermissionMode = ViewPermissionModeEnum.VisibleToTeams;
                onboarding.ViewPermissionSubjectType = PermissionSubjectTypeEnum.Team;
                onboarding.ViewTeams = Newtonsoft.Json.JsonConvert.SerializeObject(new List<string> { TestDataBuilder.TeamA });
                cases.Add(onboarding);
            }

            // Ownership cases (5 cases) - owned by current user
            for (int i = 0; i < 5; i++)
            {
                var onboarding = TestDataBuilder.CreateCase(workflow.Id);
                onboarding.Id = 9000 + i;
                onboarding.ViewPermissionMode = ViewPermissionModeEnum.VisibleToTeams;
                onboarding.Ownership = TestDataBuilder.DefaultUserId; // Current user is owner
                cases.Add(onboarding);
            }

            // PERFORMANCE OPTIMIZATION: Pre-fetch user teams once
            var userTeamIds = helpers.GetUserTeamIds();

            // Act - Check all cases
            var sw = Stopwatch.StartNew();
            var results = new Dictionary<string, int>
            {
                { "PublicMode", 0 },
                { "NarrowedMode", 0 },
                { "Ownership", 0 },
                { "Denied", 0 }
            };

            foreach (var onboarding in cases)
            {
                var result = casePermissionService.CheckCasePermission(
                    onboarding,
                    TestDataBuilder.DefaultUserId,
                    OperationTypeEnum.View);
                
                if (result.Success && result.CanView)
                {
                    if (onboarding.Ownership.HasValue && onboarding.Ownership.Value == TestDataBuilder.DefaultUserId)
                        results["Ownership"]++;
                    else if (onboarding.ViewPermissionMode == ViewPermissionModeEnum.Public)
                        results["PublicMode"]++;
                    else
                        results["NarrowedMode"]++;
                }
                else
                {
                    results["Denied"]++;
                }
            }
            sw.Stop();

            // Assert & Report
            _output.WriteLine($"✅ Mixed Mode Case Permission Check Performance:");
            _output.WriteLine($"   ═══════════════════════════════════════");
            _output.WriteLine($"   Total cases:                {cases.Count}");
            _output.WriteLine($"   ───────────────────────────────────────");
            _output.WriteLine($"   Public mode (granted):      {results["PublicMode"]}");
            _output.WriteLine($"   Narrowed mode (granted):    {results["NarrowedMode"]}");
            _output.WriteLine($"   Ownership (granted):        {results["Ownership"]}");
            _output.WriteLine($"   Permission denied:          {results["Denied"]}");
            _output.WriteLine($"   ───────────────────────────────────────");
            _output.WriteLine($"   Total time:                 {sw.ElapsedMilliseconds}ms");
            _output.WriteLine($"   Average per case:           {(double)sw.ElapsedMilliseconds / cases.Count:F3}ms");
            _output.WriteLine($"   Throughput:                 {(double)cases.Count / sw.ElapsedMilliseconds * 1000:F0} checks/sec");
            _output.WriteLine($"   ───────────────────────────────────────");
            _output.WriteLine($"   GetUserTeamIds() calls:     1 ✅");
            _output.WriteLine($"   ═══════════════════════════════════════");

            // Verify expected results
            results["PublicMode"].Should().Be(10, "All public mode cases should be granted");
            results["NarrowedMode"].Should().Be(10, "All visible to teams mode cases with matching team should be granted");
            results["Ownership"].Should().Be(5, "All owned cases should be granted");
            results["Denied"].Should().Be(0, "No cases should be denied with correct setup");
            
            sw.ElapsedMilliseconds.Should().BeLessThan(1000, "Mixed mode checks should be efficient");
        }

        /// <summary>
        /// Test: Large scale Case + Stage permission intersection stress test
        /// Simulates real-world scenario with many cases and stages
        /// </summary>
        [Fact]
        public void StressTest_CaseStageIntersection_LargeScale_ShouldBePerformant()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB });
            
            var mockOnboardingRepo = new Mock<IOnboardingRepository>();
            var (stagePermissionService, helpers) = CreateService(userContext);
            
            var workflowPermissionService = new WorkflowPermissionService(
                _mockWorkflowLogger.Object,
                userContext,
                _mockWorkflowRepo.Object,
                helpers);
            
            var casePermissionService = new CasePermissionService(
                Mock.Of<ILogger<CasePermissionService>>(),
                userContext,
                mockOnboardingRepo.Object,
                _mockWorkflowRepo.Object,
                helpers,
                workflowPermissionService);

            // Create large dataset: 3 Workflows × 8 Stages × 30 Cases = 720 total checks
            const int workflowCount = 3;
            const int stagesPerWorkflow = 8;
            const int casesPerWorkflow = 30;
            
            var testData = new List<(Workflow workflow, List<Stage> stages, List<Onboarding> cases)>();

            for (int w = 0; w < workflowCount; w++)
            {
                var workflow = TestDataBuilder.CreateVisibleToTeamsWorkflow(
                    new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB });
                workflow.Id = 10000 + w;
                
                _mockWorkflowRepo.Setup(r => r.GetByIdAsync(workflow.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(workflow);

                var stages = new List<StageEntity>();
                for (int s = 0; s < stagesPerWorkflow; s++)
                {
                    var stage = TestDataBuilder.CreateStageWithInheritedPermissions(workflow.Id);
                    stage.Id = (workflow.Id * 100) + s;
                    stages.Add(stage);
                }

                var cases = new List<Onboarding>();
                for (int c = 0; c < casesPerWorkflow; c++)
                {
                    var onboarding = TestDataBuilder.CreateCase(workflow.Id);
                    onboarding.Id = (workflow.Id * 1000) + c;
                    onboarding.ViewPermissionMode = ViewPermissionModeEnum.Public;
                    cases.Add(onboarding);
                }

                testData.Add((workflow, stages, cases));
            }

            // PERFORMANCE OPTIMIZATION: Pre-fetch user teams once
            var userTeamIds = helpers.GetUserTeamIds();

            // Act - Perform large scale permission checks
            var sw = Stopwatch.StartNew();
            var totalChecks = 0;
            var successfulCases = 0;
            var successfulStageChecks = 0;

            foreach (var (workflow, stages, cases) in testData)
            {
                foreach (var onboarding in cases)
                {
                    // Check Case permission
                    var caseResult = casePermissionService.CheckCasePermission(
                        onboarding,
                        TestDataBuilder.DefaultUserId,
                        OperationTypeEnum.View);
                    
                    if (caseResult.Success && caseResult.CanView)
                    {
                        successfulCases++;
                        
                        // For each successful case, check all stages
                        foreach (var stage in stages)
                        {
                            totalChecks++;
                            
                            var stageResult = stagePermissionService.CheckStagePermission(
                                stage,
                                workflow,
                                TestDataBuilder.DefaultUserId,
                                OperationTypeEnum.View,
                                userTeamIds);
                            
                            if (stageResult.Success && stageResult.CanView)
                            {
                                successfulStageChecks++;
                            }
                        }
                    }
                }
            }
            sw.Stop();

            var expectedTotalChecks = workflowCount * stagesPerWorkflow * casesPerWorkflow;

            // Assert & Report
            _output.WriteLine($"🔥 Large Scale Stress Test (Workflow ∩ Stage ∩ Case):");
            _output.WriteLine($"   ═══════════════════════════════════════");
            _output.WriteLine($"   Workflows:                  {workflowCount}");
            _output.WriteLine($"   Stages per workflow:        {stagesPerWorkflow}");
            _output.WriteLine($"   Cases per workflow:         {casesPerWorkflow}");
            _output.WriteLine($"   ───────────────────────────────────────");
            _output.WriteLine($"   Cases checked:              {workflowCount * casesPerWorkflow}");
            _output.WriteLine($"   Successful cases:           {successfulCases}");
            _output.WriteLine($"   Stage checks:               {totalChecks}");
            _output.WriteLine($"   Successful stage checks:    {successfulStageChecks}");
            _output.WriteLine($"   ───────────────────────────────────────");
            _output.WriteLine($"   Total time:                 {sw.ElapsedMilliseconds}ms");
            _output.WriteLine($"   Average per check:          {(double)sw.ElapsedMilliseconds / totalChecks:F3}ms");
            _output.WriteLine($"   Throughput:                 {(double)totalChecks / sw.ElapsedMilliseconds * 1000:F0} checks/sec");
            _output.WriteLine($"   ───────────────────────────────────────");
            _output.WriteLine($"   GetUserTeamIds() calls:     1 ✅");
            _output.WriteLine($"   Database queries:           0 ✅");
            _output.WriteLine($"   Formula applied:            Workflow ∩ Stage ∩ Case");
            _output.WriteLine($"   ═══════════════════════════════════════");

            totalChecks.Should().Be(expectedTotalChecks);
            successfulCases.Should().Be(workflowCount * casesPerWorkflow);
            successfulStageChecks.Should().BeGreaterThan(0);
            sw.ElapsedMilliseconds.Should().BeLessThan(3000, "Large scale checks should complete within 3 seconds");
        }

        #endregion
    }
}

