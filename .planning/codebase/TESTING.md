# Testing Patterns

**Analysis Date:** 2026-06-08

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
dotnet test packages/flowFlex-backend/Tests/FlowFlex.Tests    # Run all unit tests
dotnet test --collect:"XPlat Code Coverage"                   # Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover  # Generate opencover report
```

### Frontend (Vue/TypeScript)

**Runner:**
- Jest (configured in `packages/flowFlex-common/jest.config.ts`)
- Preset: `ts-jest`
- Test environment: `jsdom`
- Vue transform: `@vue/vue3-jest`

**Run Commands:**
```bash
pnpm test                  # Run all tests
```

**Coverage:**
- Output directory: `.coverage`
- `collectCoverage: true` — always collected on test run

**Note:** Jest infrastructure is configured but no application-level test files exist in `src/`. Frontend testing is effectively not implemented.

## Test File Organization

### Backend

**Location:** Separate `Tests/` directory within the backend package

```
packages/flowFlex-backend/Tests/FlowFlex.Tests/
├── FlowFlex.Tests.csproj
├── README.md
├── TestBase/
│   ├── MockHelper.cs              # Shared mock factory methods
│   └── TestDataBuilder.cs         # Shared entity builders with constants
└── Services/
    ├── Action/
    │   └── TemplateVariableResolverTests.cs       (246 lines)
    ├── Integration/
    │   ├── EntityMappingServiceTests.cs           (158 lines)
    │   └── QuickLinkServiceTests.cs              (171 lines)
    ├── OW/
    │   ├── ActionExecutorTests.cs                (1631 lines)
    │   ├── DashboardServiceTests.cs              (653 lines)
    │   ├── RulesEngineServiceTests.cs            (757 lines)
    │   ├── StageConditionServiceTests.cs         (628 lines)
    │   └── UserServiceTreeTests.cs               (331 lines)
    └── Permission/
        ├── CasePermissionServiceTests.cs          (523 lines)
        ├── PermissionHelpersJsonTests.cs          (216 lines)
        ├── PermissionHelpersTests.cs              (586 lines)
        ├── PermissionPerformanceTests.cs          (482 lines)
        ├── StagePermissionServiceTests.cs         (294 lines)
        └── WorkflowPermissionServiceTests.cs      (343 lines)
```

**Total test code:** ~7,019 lines across 14 test files.

**Naming:**
- Test classes: `{ServiceUnderTest}Tests` (e.g., `ActionExecutorTests`, `WorkflowPermissionServiceTests`)
- Test methods: `MethodName_Scenario_ExpectedBehavior` (e.g., `ExecuteActionsAsync_WithEmptyActions_ShouldReturnSuccess`)
- Namespace mirrors source: `FlowFlex.Tests.Services.{Domain}` (e.g., `FlowFlex.Tests.Services.OW`, `FlowFlex.Tests.Services.Permission`)

### Frontend

**Location:** No application test files exist. Jest is configured with module name mappers for `@/`, `@apis/`, etc. but no `.test.ts` or `.spec.ts` files found outside `node_modules`.

## Test Structure

### Backend Suite Organization

```csharp
namespace FlowFlex.Tests.Services.OW
{
    /// <summary>
    /// Unit tests for StageConditionService
    /// Tests CRUD operations, validation, and permission checks
    /// </summary>
    public class StageConditionServiceTests
    {
        // Private mock fields declared at class level
        private readonly Mock<ISqlSugarClient> _mockDb;
        private readonly Mock<IStageRepository> _mockStageRepository;
        private readonly Mock<IWorkflowRepository> _mockWorkflowRepository;
        private readonly Mock<IPermissionService> _mockPermissionService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<StageConditionService>> _mockLogger;
        private readonly UserContext _userContext;
        private readonly StageConditionService _service;

        // Constructor: wire up all mocks and create SUT
        public StageConditionServiceTests()
        {
            _mockDb = new Mock<ISqlSugarClient>();
            _mockStageRepository = MockHelper.CreateMockStageRepository();
            _mockWorkflowRepository = MockHelper.CreateMockWorkflowRepository();
            _mockPermissionService = new Mock<IPermissionService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = MockHelper.CreateMockLogger<StageConditionService>();

            _userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);

            _service = new StageConditionService(
                _mockDb.Object,
                _mockStageRepository.Object,
                _mockWorkflowRepository.Object,
                _mockPermissionService.Object,
                /* ... */
                _mockLogger.Object);
        }

        // Tests grouped by feature using #region
        #region ValidateRulesJsonAsync Tests

        [Fact]
        public async Task ValidateRulesJsonAsync_WithEmptyJson_ShouldReturnInvalid()
        {
            // Arrange
            var rulesJson = "";

            // Act
            var result = await _service.ValidateRulesJsonAsync(rulesJson);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == "RULES_REQUIRED");
        }

        #endregion
    }
}
```

**Patterns:**
- All mocks and SUT created in constructor (no `[SetUp]` or `IAsyncLifetime`)
- `// Arrange`, `// Act`, `// Assert` comments in every test body
- `#region` blocks group tests by method/scenario being tested
- Private factory methods (`CreateExecutionContext()`, `CreateService()`) reduce repetition
- Class-level doc comment describes scope of the test class

## Mocking

**Framework:** Moq 4.20.70

### Backend Patterns

**Creating mocks — use MockHelper for common dependencies:**
```csharp
// Shared mock factories
var mockStageRepo = MockHelper.CreateMockStageRepository();
var mockLogger = MockHelper.CreateMockLogger<StageConditionService>();
var mockHttpContext = MockHelper.CreateMockHttpContextAccessor(isPortalToken: true);

// Direct mock for less common interfaces
var mockEmailService = new Mock<IEmailService>();
var mockPermissionService = new Mock<IPermissionService>();
```

**Setting up behaviors:**
```csharp
// Return value setup with any-args
_mockEmailService.Setup(e => e.SendStageCompletedNotificationAsync(
    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
    It.IsAny<string>(), It.IsAny<string>()))
    .ReturnsAsync(true);

// Specific argument matching via MockHelper static setup
MockHelper.SetupOnboardingRepositoryGetById(
    _mockOnboardingRepository, caseId, onboarding);

// Under the hood:
mockRepo.Setup(x => x.GetByIdAsync(
    It.Is<object>(id => id.Equals(workflowId)),
    It.IsAny<bool>(),
    It.IsAny<CancellationToken>()))
    .ReturnsAsync(workflow);
```

**SqlSugar Updateable mock chain:**
```csharp
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
- `IMapper` (AutoMapper)

**What NOT to Mock:**
- Concrete classes with non-virtual methods (e.g., `IdmUserDataClient`) — create real instances with mocked sub-dependencies
- `PermissionHelpers` — create with `new Mock<PermissionHelpers>(logger, userContext, httpContextAccessor)` for virtual methods, or instantiate directly
- Value objects, DTOs, entity instances — construct directly via `TestDataBuilder`
- `UserContext` — create real instance via `TestDataBuilder.CreateUserContext()`

**Non-mockable dependency pattern:**
```csharp
// When a class has non-virtual methods, create real instance with stub config
var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("http://localhost:5000");
var mockOptions = new Mock<IOptionsSnapshot<IdentityHubOptions>>();
mockOptions.Setup(o => o.Value).Returns(new IdentityHubOptions { /* stub values */ });

_idmUserDataClient = new IdmUserDataClient(
    httpClient, mockOptions.Object, mockCache.Object, mockIdmLogger.Object);
```

## Fixtures and Factories

### TestDataBuilder (`packages/flowFlex-backend/Tests/FlowFlex.Tests/TestBase/TestDataBuilder.cs`)

Static builder class providing pre-configured domain entities:

```csharp
// Shared constants for consistent test data
public const long DefaultUserId = 123;
public const long OwnerUserId = 100;
public const long OtherUserId = 999;
public const string TeamA = "1001";
public const string TeamB = "1002";
public const string DefaultTenantId = "tenant-1";

// Workflow builders
TestDataBuilder.CreatePublicWorkflow(createUserId?)
TestDataBuilder.CreateVisibleToTeamsWorkflow(viewTeams, operateTeams?)
TestDataBuilder.CreateInvisibleToTeamsWorkflow(viewTeams, operateTeams?)
TestDataBuilder.CreatePrivateWorkflow(createUserId)

// Stage builders
TestDataBuilder.CreateStageWithInheritedPermissions(workflowId)
TestDataBuilder.CreateStageWithNarrowedPermissions(workflowId, viewTeams, operateTeams?)
TestDataBuilder.CreateStageWithAssignedUser(workflowId, assignedUserIds)

// Case (Onboarding) builders
TestDataBuilder.CreateCase(workflowId, ownership?)
TestDataBuilder.CreateCaseWithTeamPermissions(workflowId, viewTeams, operateTeams?, ownership?)
TestDataBuilder.CreateCaseWithUserPermissions(workflowId, viewUsers, operateUsers?, ownership?)
TestDataBuilder.CreateCaseWithOwnership(workflowId, ownerUserId)
TestDataBuilder.CreatePublicCase(workflowId, ownership?)
TestDataBuilder.CreateVisibleToTeamsCase(viewTeams, operateTeams?, ownership?)
TestDataBuilder.CreateVisibleToUsersCase(viewUsers, operateUsers?, ownership?)

// UserContext builders
TestDataBuilder.CreateUserContext(userId, teamIds?, isSystemAdmin?, tenantId?, iamToken?)
TestDataBuilder.CreateSystemAdminContext()
TestDataBuilder.CreateTenantAdminContext(tenantId?)
```

### MockHelper (`packages/flowFlex-backend/Tests/FlowFlex.Tests/TestBase/MockHelper.cs`)

Static helper for creating pre-configured mock objects:

```csharp
// Generic logger mock
MockHelper.CreateMockLogger<T>()

// Repository mocks (pre-configured but empty)
MockHelper.CreateMockWorkflowRepository()
MockHelper.CreateMockStageRepository()
MockHelper.CreateMockOnboardingRepository()

// HTTP context mock with optional portal token/attribute setup
MockHelper.CreateMockHttpContextAccessor(isPortalToken?, hasPortalAccessAttribute?)

// Setup helpers that wire specific GetById behavior
MockHelper.SetupWorkflowRepositoryGetById(mockRepo, workflowId, workflow)
MockHelper.SetupStageRepositoryGetById(mockRepo, stageId, stage)
MockHelper.SetupOnboardingRepositoryGetById(mockRepo, caseId, onboarding)

// Identity hub permission check setup
MockHelper.SetupIdentityHubClientPermissionCheck(mockClient, permission, hasPermission)
```

## Coverage

**Backend Requirements:** No enforced threshold. coverlet.collector installed but no minimum configured in csproj.

**Frontend Requirements:** `collectCoverage: true` always runs; no threshold enforced.

**View Coverage:**
```bash
# Backend
dotnet test packages/flowFlex-backend/Tests/FlowFlex.Tests --collect:"XPlat Code Coverage"

# Frontend (output goes to packages/flowFlex-common/.coverage/)
cd packages/flowFlex-common && pnpm test
```

## Test Types

**Unit Tests (Backend):**
- Scope: Single service class with all dependencies mocked
- Location: `packages/flowFlex-backend/Tests/FlowFlex.Tests/Services/`
- Pattern: Constructor-based setup, `[Fact]` attributes, FluentAssertions
- Covers: Permission logic, action execution, rules engine, dashboard, stage conditions, template resolution, entity mapping

**Integration Tests:**
- Not present in the current test project
- Comments in unit tests note: "Tests requiring actual IDM API calls should use integration tests"
- No separate integration test project exists

**E2E Tests:**
- Not detected in the repository

**Performance Tests:**
- `PermissionPerformanceTests.cs` (482 lines) — tests permission check performance characteristics
- Lives alongside unit tests, not a separate benchmarking suite

**Frontend Tests:**
- Jest configured with `ts-jest` and `@vue/vue3-jest` but **no application test files exist**
- Test infrastructure is ready but unused

## What's Well-Tested vs Gaps

### Well-Tested Areas

| Area | File | Coverage |
|------|------|----------|
| Action Execution | `ActionExecutorTests.cs` (1631 lines) | GoToStage, SendNotification, AssignUser, error handling |
| Permission Helpers | `PermissionHelpersTests.cs` (586 lines) | Team whitelist/blacklist, admin bypass, portal detection |
| Permission (Case) | `CasePermissionServiceTests.cs` (523 lines) | Team/user permission modes, ownership |
| Permission (Workflow) | `WorkflowPermissionServiceTests.cs` (343 lines) | Public/Private/VisibleToTeams/InvisibleToTeams modes |
| Permission (Stage) | `StagePermissionServiceTests.cs` (294 lines) | Inheritance, narrowing, assigned users |
| Rules Engine | `RulesEngineServiceTests.cs` (757 lines) | Rule validation, evaluation |
| Stage Conditions | `StageConditionServiceTests.cs` (628 lines) | CRUD, validation, permission checks |
| Dashboard | `DashboardServiceTests.cs` (653 lines) | Aggregation queries, filtering |
| JSON Parsing | `PermissionHelpersJsonTests.cs` (216 lines) | Double-escaped JSON, malformed input |

### Gaps

| Area | Impact | Priority |
|------|--------|----------|
| **Frontend** — no tests at all | UI regressions undetectable | High |
| **WorkflowService** — no unit tests | Core CRUD logic untested | High |
| **StageService** — no unit tests | Complex orchestration logic untested | High |
| **OnboardingService** — no unit tests | Case lifecycle untested | High |
| **QuestionnaireService** — no tests | Form logic untested | Medium |
| **ChecklistService** — no tests | Task completion logic untested | Medium |
| **Controllers** — no tests | Request validation, auth untested | Medium |
| **Integration tests** — none exist | External service interactions unverified | Medium |
| **AI services** — no tests | AI generation flows untested | Low |

## Common Patterns

**Async Testing:**
```csharp
[Fact]
public async Task ValidateRulesJsonAsync_WithEmptyJson_ShouldReturnInvalid()
{
    // Arrange
    var rulesJson = "";

    // Act
    var result = await _service.ValidateRulesJsonAsync(rulesJson);

    // Assert
    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.Code == "RULES_REQUIRED");
}
```

**Permission Testing:**
```csharp
[Fact]
public void GetUserTeamIds_WithValidTeams_ShouldReturnTeamIdStrings()
{
    // Arrange
    var userContext = TestDataBuilder.CreateUserContext(
        TestDataBuilder.DefaultUserId,
        new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB });
    var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
    var helpers = new PermissionHelpers(
        _mockLogger.Object, userContext, mockHttpContextAccessor.Object);

    // Act
    var result = helpers.GetUserTeamIds();

    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(2);
    result.Should().Contain(TestDataBuilder.TeamA);
}
```

**Result Object Assertions:**
```csharp
result.Should().NotBeNull();
result.Success.Should().BeTrue();
result.Details.Should().HaveCount(2);
result.Details[0].Success.Should().BeFalse();
result.Details[0].ErrorMessage.Should().Contain("not found");
```

**JSON Input Testing:**
```csharp
// Use raw JSON strings for exact format control
var actionsJson = @"[{""type"":""SendNotification"",""order"":1,""parameters"":{""users"":[""456""]}}]";

// Or use serialization for typed input
var actions = new List<ConditionAction> { new() { Type = "GoToStage", Order = 1 } };
var actionsJson = JsonConvert.SerializeObject(actions);
```

**Null/Not-Found Testing:**
```csharp
// Setup mock to return null to simulate missing entity
MockHelper.SetupOnboardingRepositoryGetById(_mockOnboardingRepository, context.OnboardingId, null);

// Assert failure
result.Details[0].Success.Should().BeFalse();
result.Details[0].ErrorMessage.Should().Contain("not found");
```

---

*Testing analysis: 2026-06-08*
