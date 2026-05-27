# Testing Patterns

**Analysis Date:** 2026-05-25

## Test Framework

### Backend (.NET)

**Runner:**
- xUnit 2.6.2
- Config: `packages/flowFlex-backend/Tests/FlowFlex.Tests/FlowFlex.Tests.csproj`

**Assertion Library:**
- FluentAssertions 6.12.0

**Mocking:**
- Moq 4.20.70

**Coverage:**
- coverlet.collector 6.0.0

**Run Commands:**
```bash
dotnet test                                                    # Run all tests
dotnet test packages/flowFlex-backend/Tests/FlowFlex.Tests    # Run unit tests only
dotnet test --collect:"XPlat Code Coverage"                   # Run with coverage
```

### Frontend (Vue/TypeScript)

**Runner:**
- Jest (configured in `packages/flowFlex-common/jest.config.ts`)
- Preset: `ts-jest`
- Test environment: `jsdom`

**Run Commands:**
```bash
pnpm test                  # Run all tests
```

**Coverage:**
- Output directory: `.coverage`
- `collectCoverage: true` — always collected on test run

## Test File Organization

### Backend

**Location:** Separate `Tests/` directory at solution root

```
packages/flowFlex-backend/
└── Tests/
    └── FlowFlex.Tests/
        ├── FlowFlex.Tests.csproj
        ├── TestBase/
        │   ├── MockHelper.cs       # Shared mock factory methods
        │   └── TestDataBuilder.cs  # Shared entity builders
        └── Services/
            ├── Action/
            │   └── TemplateVariableResolverTests.cs
            ├── Integration/
            │   ├── EntityMappingServiceTests.cs
            │   └── QuickLinkServiceTests.cs
            ├── OW/
            │   ├── ActionExecutorTests.cs
            │   ├── DashboardServiceTests.cs
            │   ├── RulesEngineServiceTests.cs
            │   ├── StageConditionServiceTests.cs
            │   └── UserServiceTreeTests.cs
            └── Permission/
                ├── CasePermissionServiceTests.cs
                ├── PermissionHelpersJsonTests.cs
                ├── PermissionHelpersTests.cs
                ├── PermissionPerformanceTests.cs
                ├── StagePermissionServiceTests.cs
                └── WorkflowPermissionServiceTests.cs
```

**Naming:**
- Test classes: `<ServiceUnderTest>Tests` (e.g., `ActionExecutorTests`, `WorkflowPermissionServiceTests`)
- Test methods: `MethodName_Scenario_ExpectedBehavior` (e.g., `ExecuteActionsAsync_WithEmptyActions_ShouldReturnSuccess`)
- Namespace mirrors source: `FlowFlex.Tests.Services.<Domain>`

### Frontend

**Location:** Co-located or in `__tests__` subdirectories (Jest default pattern)

**Naming:**
- `*.test.ts` or `*.spec.ts`
- Skills tests in `.claude/skills/capability-evolver/test/` (framework tooling, not application tests)

## Test Structure

### Backend Suite Organization

```csharp
public class ActionExecutorTests
{
    // Private mock fields declared at class level
    private readonly Mock<ISqlSugarClient> _mockDb;
    private readonly Mock<IStageRepository> _mockStageRepository;
    private readonly ConditionActionExecutor _executor;

    // Constructor: wire up all mocks and create SUT
    public ActionExecutorTests()
    {
        _mockDb = new Mock<ISqlSugarClient>();
        _mockStageRepository = MockHelper.CreateMockStageRepository();
        // ... setup default mock behaviors ...
        _executor = new ConditionActionExecutor(
            _mockDb.Object,
            _mockStageRepository.Object,
            /* ... */);
    }

    // Tests grouped by feature using #region
    #region ExecuteActionsAsync Tests

    [Fact]
    public async Task ExecuteActionsAsync_WithEmptyActions_ShouldReturnSuccess()
    {
        // Arrange
        var actionsJson = "[]";
        var context = CreateExecutionContext();

        // Act
        var result = await _executor.ExecuteActionsAsync(actionsJson, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Details.Should().BeEmpty();
    }

    #endregion

    // Private helpers at bottom in #region Helper Methods
    #region Helper Methods

    private ActionExecutionContext CreateExecutionContext() { ... }
    private void SetupOnboardingUpdateable() { ... }

    #endregion
}
```

**Patterns:**
- All mocks and SUT created in constructor (no `[SetUp]` / `BeforeEach`)
- `clearMocks: true` in Jest config (frontend)
- `// Arrange`, `// Act`, `// Assert` comments in every test body
- `#region` blocks group tests by the action/scenario being tested
- Private factory methods (`CreateExecutionContext()`, `CreateService()`) reduce repetition

## Mocking

**Framework:** Moq (backend), Jest mocks (frontend)

### Backend Patterns

**Creating mocks:**
```csharp
// Simple mock
var mockEmailService = new Mock<IEmailService>();

// Using shared factory in MockHelper
var mockStageRepo = MockHelper.CreateMockStageRepository();
var mockLogger = MockHelper.CreateMockLogger<MyService>();
```

**Setting up behaviors:**
```csharp
// Return value setup
_mockEmailService.Setup(e => e.SendStageCompletedNotificationAsync(
    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
    It.IsAny<string>(), It.IsAny<string>()))
    .ReturnsAsync(true);

// Specific argument matching
mockRepo.Setup(x => x.GetByIdAsync(
    It.Is<object>(id => id.Equals(stageId)),
    It.IsAny<bool>(),
    It.IsAny<CancellationToken>()))
    .ReturnsAsync(stage);

// Chained mock for SqlSugar Updateable
var mockUpdateable = new Mock<IUpdateable<Onboarding>>();
mockUpdateable.Setup(u => u.SetColumns(It.IsAny<Expression<Func<Onboarding, Onboarding>>>()))
    .Returns(mockUpdateable.Object);
mockUpdateable.Setup(u => u.Where(It.IsAny<Expression<Func<Onboarding, bool>>>()))
    .Returns(mockUpdateable.Object);
mockUpdateable.Setup(u => u.ExecuteCommandAsync())
    .ReturnsAsync(1);
_mockDb.Setup(db => db.Updateable<Onboarding>())
    .Returns(mockUpdateable.Object);
```

**What to Mock:**
- All repository interfaces (`IOnboardingRepository`, `IStageRepository`, `IWorkflowRepository`)
- All external service interfaces (`IEmailService`, `IUserService`, `IMediator`)
- `ISqlSugarClient` for database operations
- `ILogger<T>` via `MockHelper.CreateMockLogger<T>()`
- `IHttpContextAccessor` via `MockHelper.CreateMockHttpContextAccessor()`

**What NOT to Mock:**
- Concrete classes with non-virtual methods (e.g., `IdmUserDataClient`) — create real instances with mocked dependencies instead
- `PermissionHelpers` — instantiate directly with mocked dependencies
- Simple value objects and DTOs — construct directly

**Note on non-mockable dependencies:** When a concrete class has non-virtual methods (like `IdmUserDataClient`), create a real instance with minimal/stub configuration. Tests that require actual HTTP calls should be integration tests, not unit tests. This is documented inline in test comments.

## Fixtures and Factories

### TestDataBuilder (`Tests/FlowFlex.Tests/TestBase/TestDataBuilder.cs`)

Static builder class with named factory methods for domain entities:

```csharp
// Constants for reuse across tests
public const long DefaultUserId = 123;
public const string DefaultTenantId = "tenant-1";

// Entity builders
TestDataBuilder.CreatePublicWorkflow()
TestDataBuilder.CreateVisibleToTeamsWorkflow(viewTeams, operateTeams)
TestDataBuilder.CreatePrivateWorkflow(createUserId)
TestDataBuilder.CreateStageWithInheritedPermissions(workflowId)
TestDataBuilder.CreateCase(workflowId, ownership)
TestDataBuilder.CreateCaseWithTeamPermissions(workflowId, viewTeams, operateTeams)
TestDataBuilder.CreateUserContext(userId, teamIds, isSystemAdmin, tenantId)
TestDataBuilder.CreateSystemAdminContext()
TestDataBuilder.CreateTenantAdminContext(tenantId)
```

### MockHelper (`Tests/FlowFlex.Tests/TestBase/MockHelper.cs`)

Static helper for creating pre-configured mock objects:

```csharp
MockHelper.CreateMockLogger<T>()
MockHelper.CreateMockWorkflowRepository()
MockHelper.CreateMockStageRepository()
MockHelper.CreateMockOnboardingRepository()
MockHelper.CreateMockHttpContextAccessor(isPortalToken, hasPortalAccessAttribute)
MockHelper.SetupWorkflowRepositoryGetById(mockRepo, workflowId, workflow)
MockHelper.SetupStageRepositoryGetById(mockRepo, stageId, stage)
MockHelper.SetupOnboardingRepositoryGetById(mockRepo, caseId, onboarding)
```

**Location:** `packages/flowFlex-backend/Tests/FlowFlex.Tests/TestBase/`

## Coverage

**Backend Requirements:** No enforced threshold. coverlet.collector installed but no minimum configured.

**Frontend Requirements:** `collectCoverage: true` always runs; no threshold enforced.

**View Coverage:**
```bash
# Backend
dotnet test --collect:"XPlat Code Coverage"

# Frontend
pnpm test   # Coverage output goes to packages/flowFlex-common/.coverage/
```

## Test Types

**Unit Tests:**
- Scope: Single service class with all dependencies mocked
- Location: `Tests/FlowFlex.Tests/Services/`
- Covers: Permission logic, action execution, rules engine, dashboard, stage conditions
- Pattern: Constructor-based setup, `[Fact]` attributes, FluentAssertions

**Integration Tests:**
- Not present in the current test project
- Comments in unit tests explicitly note: "For full team notification testing, use integration tests" and "Tests requiring actual IDM API calls should use integration tests"
- No separate integration test project exists yet

**E2E Tests:**
- Not detected in the repository

**Frontend Tests:**
- Jest configured with `ts-jest` preset and `@vue/vue3-jest` for `.vue` files
- No application-level test files found under `src/` — test infrastructure is configured but tests are not written

## Common Patterns

**Async Testing (Backend):**
```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange
    var input = ...;

    // Act
    var result = await _service.MethodAsync(input);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
}
```

**Null/Not-Found Testing:**
```csharp
// Setup mock to return null to simulate missing entity
MockHelper.SetupOnboardingRepositoryGetById(_mockOnboardingRepository, context.OnboardingId, null);

// Assert failure result
result.Details[0].Success.Should().BeFalse();
result.Details[0].ErrorMessage.Should().Contain("not found");
```

**Result Object Assertions:**
```csharp
// Check overall success
result.Success.Should().BeTrue();
result.Details.Should().HaveCount(2);

// Check individual action results
result.Details[0].Success.Should().BeFalse();
result.Details[0].ErrorMessage.Should().Contain("Unsupported action type");
result.Details[0].ResultData.Should().ContainKey("targetStageId");
result.Details[0].ResultData["targetStageId"].Should().Be(20);
```

**Conditional Assertions for Non-Mockable Dependencies:**
```csharp
// When a dependency cannot be mocked, use conditional assertion
if (result.Details[0].Success)
{
    result.Details[0].ResultData.Should().ContainKey("assigneeType");
    result.Details[0].ResultData["assigneeType"].Should().Be("team");
}
```

**JSON Input Testing:**
```csharp
// Use JsonConvert.SerializeObject for typed input
var actions = new List<ConditionAction> { new ConditionAction { Type = "GoToStage", Order = 1 } };
var actionsJson = JsonConvert.SerializeObject(actions);

// Use raw JSON strings for exact format control
var actionsJson = @"[{""type"":""SendNotification"",""order"":1,""parameters"":{""users"":[""456""]}}]";
```

---

*Testing analysis: 2026-05-25*
