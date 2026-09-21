using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Services.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Tests.TestBase;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using SqlSugar;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace FlowFlex.Tests.Services.OW
{
    /// <summary>
    /// Unit tests for TriggerExecutionService (OW-724).
    ///
    /// Coverage:
    ///   - No outbound connections → engine exits early
    ///   - No conditions → default trigger fires
    ///   - Questionnaire condition == match → Triggered
    ///   - Questionnaire condition == mismatch → Skipped
    ///   - Questionnaire condition != match → Triggered
    ///   - Questionnaire condition != mismatch → Skipped
    ///   - Questionnaire condition case-insensitive match
    ///   - Questionnaire answer not found → Skipped
    ///   - sectionInstances answer layout parsed correctly
    ///   - Field condition == match → Triggered
    ///   - Field condition == mismatch → Skipped
    ///   - Multiple AND conditions — all pass → Triggered
    ///   - Multiple AND conditions — one fails → Skipped
    ///   - Multiple OR conditions — one passes → Triggered
    ///   - Unknown component type → Skipped
    ///   - Empty operator → Skipped
    ///   - CaseName built correctly (with name / with code / neither)
    ///   - Log records source+target IDs and completion type
    ///   - Log has no targetId when Skipped
    /// </summary>
    public class TriggerExecutionServiceTests
    {
        // ── Mocks ────────────────────────────────────────────────────────────
        private readonly Mock<ISqlSugarClient>                        _db;
        private readonly Mock<IWorkflowTriggerConnectionRepository>   _connRepo;
        private readonly Mock<IWorkflowTriggerGraphRepository>        _graphRepo;
        private readonly Mock<IWorkflowTriggerLogRepository>          _logRepo;
        private readonly Mock<IOnboardingService>                     _onboardingService;
        private readonly Mock<IStaticFieldValueRepository>            _staticFieldRepo;
        private readonly Mock<IQuestionnaireAnswerRepository>         _questionnaireAnswerRepo;
        private readonly Mock<IChecklistTaskCompletionRepository>     _taskCompletionRepo;
        private readonly Mock<ILogger<TriggerExecutionService>>       _logger;
        private readonly UserContext                                   _userContext;

        // ── Snowflake IDs ────────────────────────────────────────────────────
        private const long SourceOnboardingId = 2000000000000000001L;
        private const long SourceWorkflowId   = 2000000000000000002L;
        private const long TargetWorkflowId   = 2000000000000000003L;
        private const long ConnectionId       = 2000000000000000004L;
        private const long TargetOnboardingId = 2000000000000000005L;

        private const string QuestionId = "q_short_answer_1";

        /// <summary>
        /// Subclass that replaces SqlSugar query seam methods with in-memory data,
        /// bypassing ISugarQueryable optional-parameter mock incompatibility.
        /// </summary>
        private class TestableTriggerExecutionService : TriggerExecutionService
        {
            public Onboarding? SourceOnboarding { get; set; }
            public List<WorkflowTriggerConnection> OutboundConnections { get; set; } = new();

            public TestableTriggerExecutionService(
                ISqlSugarClient db,
                IWorkflowTriggerConnectionRepository connRepo,
                IWorkflowTriggerGraphRepository graphRepo,
                IWorkflowTriggerLogRepository logRepo,
                IOnboardingService onboardingService,
                IStaticFieldValueRepository staticFieldRepo,
                IQuestionnaireAnswerRepository questionnaireAnswerRepo,
                IChecklistTaskCompletionRepository taskCompletionRepo,
                UserContext userContext,
                ILogger<TriggerExecutionService> logger)
                : base(db, connRepo, graphRepo, logRepo, onboardingService,
                       staticFieldRepo, questionnaireAnswerRepo, taskCompletionRepo,
                       userContext, logger)
            { }

            protected override Task<Onboarding?> LoadSourceOnboardingAsync(
                long sourceOnboardingId, string tenantId, string appCode)
                => Task.FromResult(SourceOnboarding);

            protected override Task<List<WorkflowTriggerConnection>> LoadOutboundConnectionsAsync(
                long sourceWorkflowId, string tenantId, string appCode)
                => Task.FromResult(OutboundConnections);
        }

        private readonly TestableTriggerExecutionService _sut;

        public TriggerExecutionServiceTests()
        {
            _db                      = new Mock<ISqlSugarClient>();
            _connRepo                = new Mock<IWorkflowTriggerConnectionRepository>();
            _graphRepo               = new Mock<IWorkflowTriggerGraphRepository>();
            _logRepo                 = new Mock<IWorkflowTriggerLogRepository>();
            _onboardingService       = new Mock<IOnboardingService>();
            _staticFieldRepo         = new Mock<IStaticFieldValueRepository>();
            _questionnaireAnswerRepo = new Mock<IQuestionnaireAnswerRepository>();
            _taskCompletionRepo      = new Mock<IChecklistTaskCompletionRepository>();
            _logger                  = MockHelper.CreateMockLogger<TriggerExecutionService>();

            _userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                tenantId: TestDataBuilder.DefaultTenantId);

            _sut = new TestableTriggerExecutionService(
                _db.Object, _connRepo.Object, _graphRepo.Object, _logRepo.Object,
                _onboardingService.Object, _staticFieldRepo.Object,
                _questionnaireAnswerRepo.Object, _taskCompletionRepo.Object,
                _userContext, _logger.Object);

            _sut.SourceOnboarding = BuildSourceOnboarding("Test Case", "CASE-001");

            _staticFieldRepo
                .Setup(r => r.GetByOnboardingIdAsync(SourceOnboardingId))
                .ReturnsAsync(new List<StaticFieldValue>());
            _questionnaireAnswerRepo
                .Setup(r => r.GetByOnboardingIdAsync(SourceOnboardingId))
                .ReturnsAsync(new List<QuestionnaireAnswer>());
            _logRepo
                .Setup(r => r.InsertAsync(It.IsAny<WorkflowTriggerLog>(),
                    It.IsAny<System.Threading.CancellationToken>(), It.IsAny<bool>()))
                .ReturnsAsync(true);
            _onboardingService
                .Setup(s => s.CreateAsync(It.IsAny<OnboardingInputDto>()))
                .ReturnsAsync(TargetOnboardingId);
        }

        // ── Builders ─────────────────────────────────────────────────────────

        private static Onboarding BuildSourceOnboarding(string? caseName, string? caseCode) =>
            new Onboarding
            {
                Id = SourceOnboardingId, WorkflowId = SourceWorkflowId,
                CaseName = caseName, CaseCode = caseCode,
                TenantId = TestDataBuilder.DefaultTenantId, AppCode = "default", IsValid = true,
            };

        private static WorkflowTriggerConnection BuildConnection(string configJson = "{}") =>
            new WorkflowTriggerConnection
            {
                Id = ConnectionId, SourceWorkflowId = SourceWorkflowId,
                TargetWorkflowId = TargetWorkflowId, IsEnabled = true, IsValid = true,
                TenantId = TestDataBuilder.DefaultTenantId, AppCode = "default",
                ConfigJson = configJson,
            };

        private void SetupConnections(params WorkflowTriggerConnection[] conns) =>
            _sut.OutboundConnections = new List<WorkflowTriggerConnection>(conns);

        private static string QnConfig(string qId, string op, string val) =>
            $@"{{""conditions"":[{{""id"":""c1"",""logic"":""AND"",""stageId"":""s1"",
               ""componentKey"":""questionnaire_q1"",""componentType"":""questionnaires"",
               ""componentId"":""q1"",""componentName"":""Q"",
               ""resourceId"":""{qId}"",""operator"":""{op}"",""value"":""{val}""}}],
               ""mappings"":[],""autoMap"":false}}";

        private static string FldConfig(string fieldId, string op, string val) =>
            $@"{{""conditions"":[{{""id"":""c1"",""logic"":""AND"",""stageId"":""s1"",
               ""componentKey"":""field_{fieldId}"",""componentType"":""fields"",
               ""componentId"":""{fieldId}"",""componentName"":""F"",
               ""operator"":""{op}"",""value"":""{val}""}}],
               ""mappings"":[],""autoMap"":false}}";

        private static QuestionnaireAnswer BuildAnswer(string qId, string answer) =>
            new QuestionnaireAnswer
            {
                OnboardingId = SourceOnboardingId, QuestionnaireId = 999L, IsValid = true,
                Status = "Submitted", TenantId = TestDataBuilder.DefaultTenantId,
                Answer = JToken.Parse(
                    $@"{{""responses"":[{{""questionId"":""{qId}"",""answer"":""{answer}"",""type"":""short_answer""}}]}}"),
            };

        private static StaticFieldValue BuildFieldValue(string fieldId, string value) =>
            new StaticFieldValue
            {
                OnboardingId = SourceOnboardingId, FieldId = long.Parse(fieldId),
                FieldName = "testField", FieldValueJson = $"\"{value}\"",
                IsValid = true, TenantId = TestDataBuilder.DefaultTenantId,
            };

        private const string NoConditions =
            @"{""conditions"":[],""mappings"":[],""autoMap"":false}";

        // ── Early exit ───────────────────────────────────────────────────────

        [Fact]
        public async Task ExecuteTriggersAsync_NoConnections_DoesNotCreateCase()
        {
            // Arrange — no connections (default empty list)

            // Act
            await _sut.ExecuteTriggersAsync(SourceOnboardingId, SourceWorkflowId, "Completed",
                TestDataBuilder.DefaultTenantId, "default");

            // Assert
            _onboardingService.Verify(s => s.CreateAsync(It.IsAny<OnboardingInputDto>()), Times.Never);
            _logRepo.Verify(r => r.InsertAsync(It.IsAny<WorkflowTriggerLog>(),
                It.IsAny<System.Threading.CancellationToken>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteTriggersAsync_SourceNotFound_Aborts()
        {
            // Arrange
            _sut.SourceOnboarding = null;
            SetupConnections(BuildConnection());

            // Act
            await _sut.ExecuteTriggersAsync(SourceOnboardingId, SourceWorkflowId, "Completed",
                TestDataBuilder.DefaultTenantId, "default");

            // Assert
            _onboardingService.Verify(s => s.CreateAsync(It.IsAny<OnboardingInputDto>()), Times.Never);
        }

        // ── No conditions = default trigger ──────────────────────────────────

        [Fact]
        public async Task ExecuteTriggersAsync_NoConditions_Triggered()
        {
            // Arrange
            SetupConnections(BuildConnection(NoConditions));

            // Act
            await _sut.ExecuteTriggersAsync(SourceOnboardingId, SourceWorkflowId, "Completed",
                TestDataBuilder.DefaultTenantId, "default");

            // Assert
            _onboardingService.Verify(s => s.CreateAsync(It.IsAny<OnboardingInputDto>()), Times.Once);
            _logRepo.Verify(r => r.InsertAsync(It.Is<WorkflowTriggerLog>(l => l.Status == "Triggered"),
                It.IsAny<System.Threading.CancellationToken>(), It.IsAny<bool>()), Times.Once);
        }

        // ── Questionnaire == ─────────────────────────────────────────────────

        [Fact]
        public async Task ExecuteTriggersAsync_QuestionnaireEquals_Match_Triggered()
        {
            // Arrange
            SetupConnections(BuildConnection(QnConfig(QuestionId, "==", "testwzy")));
            _questionnaireAnswerRepo
                .Setup(r => r.GetByOnboardingIdAsync(SourceOnboardingId))
                .ReturnsAsync(new List<QuestionnaireAnswer> { BuildAnswer(QuestionId, "testwzy") });

            // Act
            await _sut.ExecuteTriggersAsync(SourceOnboardingId, SourceWorkflowId, "Completed",
                TestDataBuilder.DefaultTenantId, "default");

            // Assert
            _onboardingService.Verify(s => s.CreateAsync(It.IsAny<OnboardingInputDto>()), Times.Once);
            _logRepo.Verify(r => r.InsertAsync(It.Is<WorkflowTriggerLog>(l => l.Status == "Triggered"),
                It.IsAny<System.Threading.CancellationToken>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteTriggersAsync_QuestionnaireEquals_Mismatch_Skipped()
        {
            // Arrange
            SetupConnections(BuildConnection(QnConfig(QuestionId, "==", "testwzy")));
            _questionnaireAnswerRepo
                .Setup(r => r.GetByOnboardingIdAsync(SourceOnboardingId))
                .ReturnsAsync(new List<QuestionnaireAnswer> { BuildAnswer(QuestionId, "123") });

            // Act
            await _sut.ExecuteTriggersAsync(SourceOnboardingId, SourceWorkflowId, "Completed",
                TestDataBuilder.DefaultTenantId, "default");

            // Assert
            _onboardingService.Verify(s => s.CreateAsync(It.IsAny<OnboardingInputDto>()), Times.Never);
            _logRepo.Verify(r => r.InsertAsync(It.Is<WorkflowTriggerLog>(l => l.Status == "Skipped"),
                It.IsAny<System.Threading.CancellationToken>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteTriggersAsync_QuestionnaireEquals_CaseInsensitive_Triggered()
        {
            // Arrange — "TESTWZY" matches condition "testwzy"
            SetupConnections(BuildConnection(QnConfig(QuestionId, "==", "testwzy")));
            _questionnaireAnswerRepo
                .Setup(r => r.GetByOnboardingIdAsync(SourceOnboardingId))
                .ReturnsAsync(new List<QuestionnaireAnswer> { BuildAnswer(QuestionId, "TESTWZY") });

            // Act
            await _sut.ExecuteTriggersAsync(SourceOnboardingId, SourceWorkflowId, "Completed",
                TestDataBuilder.DefaultTenantId, "default");

            // Assert
            _onboardingService.Verify(s => s.CreateAsync(It.IsAny<OnboardingInputDto>()), Times.Once);
        }

        // ── Questionnaire != ─────────────────────────────────────────────────

        [Fact]
        public async Task ExecuteTriggersAsync_QuestionnaireNotEquals_Different_Triggered()
        {
            // Arrange — triggers when answer != "123132"
            SetupConnections(BuildConnection(QnConfig(QuestionId, "!=", "123132")));
            _questionnaireAnswerRepo
                .Setup(r => r.GetByOnboardingIdAsync(SourceOnboardingId))
                .ReturnsAsync(new List<QuestionnaireAnswer> { BuildAnswer(QuestionId, "different") });

            // Act
            await _sut.ExecuteTriggersAsync(SourceOnboardingId, SourceWorkflowId, "Completed",
                TestDataBuilder.DefaultTenantId, "default");

            // Assert
            _onboardingService.Verify(s => s.CreateAsync(It.IsAny<OnboardingInputDto>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteTriggersAsync_QuestionnaireNotEquals_SameValue_Skipped()
        {
            // Arrange — answer equals condition value → != should NOT fire
            SetupConnections(BuildConnection(QnConfig(QuestionId, "!=", "123132")));
            _questionnaireAnswerRepo
                .Setup(r => r.GetByOnboardingIdAsync(SourceOnboardingId))
                .ReturnsAsync(new List<QuestionnaireAnswer> { BuildAnswer(QuestionId, "123132") });

            // Act
            await _sut.ExecuteTriggersAsync(SourceOnboardingId, SourceWorkflowId, "Completed",
                TestDataBuilder.DefaultTenantId, "default");

            // Assert
            _onboardingService.Verify(s => s.CreateAsync(It.IsAny<OnboardingInputDto>()), Times.Never);
            _logRepo.Verify(r => r.InsertAsync(It.Is<WorkflowTriggerLog>(l => l.Status == "Skipped"),
                It.IsAny<System.Threading.CancellationToken>(), It.IsAny<bool>()), Times.Once);
        }

        // ── Answer not found ─────────────────────────────────────────────────

        [Fact]
        public async Task ExecuteTriggersAsync_AnswerNotFound_Skipped()
        {
            // Arrange — no answers; questionId missing from map
            SetupConnections(BuildConnection(QnConfig(QuestionId, "==", "testwzy")));

            // Act
            await _sut.ExecuteTriggersAsync(SourceOnboardingId, SourceWorkflowId, "Completed",
                TestDataBuilder.DefaultTenantId, "default");

            // Assert
            _onboardingService.Verify(s => s.CreateAsync(It.IsAny<OnboardingInputDto>()), Times.Never);
            _logRepo.Verify(r => r.InsertAsync(It.Is<WorkflowTriggerLog>(l => l.Status == "Skipped"),
                It.IsAny<System.Threading.CancellationToken>(), It.IsAny<bool>()), Times.Once);
        }

        // ── sectionInstances layout ──────────────────────────────────────────

        [Fact]
        public async Task ExecuteTriggersAsync_SectionInstancesLayout_ParsedCorrectly()
        {
            // Arrange — repeatable-section answer format
            SetupConnections(BuildConnection(QnConfig(QuestionId, "==", "hello")));
            var answer = new QuestionnaireAnswer
            {
                OnboardingId = SourceOnboardingId, QuestionnaireId = 999L, IsValid = true,
                Status = "Submitted", TenantId = TestDataBuilder.DefaultTenantId,
                Answer = JToken.Parse(
                    $@"{{""sectionInstances"":[{{""sectionId"":""s1"",""groupIndex"":0,
                       ""responses"":[{{""questionId"":""{QuestionId}"",""answer"":""hello""}}]}}]}}"),
            };
            _questionnaireAnswerRepo
                .Setup(r => r.GetByOnboardingIdAsync(SourceOnboardingId))
                .ReturnsAsync(new List<QuestionnaireAnswer> { answer });

            // Act
            await _sut.ExecuteTriggersAsync(SourceOnboardingId, SourceWorkflowId, "Completed",
                TestDataBuilder.DefaultTenantId, "default");

            // Assert
            _onboardingService.Verify(s => s.CreateAsync(It.IsAny<OnboardingInputDto>()), Times.Once);
        }

        // ── Field conditions ─────────────────────────────────────────────────

        [Fact]
        public async Task ExecuteTriggersAsync_FieldEquals_Match_Triggered()
        {
            // Arrange
            const string fieldId = "2009101837793366016";
            SetupConnections(BuildConnection(FldConfig(fieldId, "==", "approved")));
            _staticFieldRepo
                .Setup(r => r.GetByOnboardingIdAsync(SourceOnboardingId))
                .ReturnsAsync(new List<StaticFieldValue> { BuildFieldValue(fieldId, "approved") });

            // Act
            await _sut.ExecuteTriggersAsync(SourceOnboardingId, SourceWorkflowId, "Completed",
                TestDataBuilder.DefaultTenantId, "default");

            // Assert
            _onboardingService.Verify(s => s.CreateAsync(It.IsAny<OnboardingInputDto>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteTriggersAsync_FieldEquals_Mismatch_Skipped()
        {
            // Arrange
            const string fieldId = "2009101837793366016";
            SetupConnections(BuildConnection(FldConfig(fieldId, "==", "approved")));
            _staticFieldRepo
                .Setup(r => r.GetByOnboardingIdAsync(SourceOnboardingId))
                .ReturnsAsync(new List<StaticFieldValue> { BuildFieldValue(fieldId, "pending") });

            // Act
            await _sut.ExecuteTriggersAsync(SourceOnboardingId, SourceWorkflowId, "Completed",
                TestDataBuilder.DefaultTenantId, "default");

            // Assert
            _onboardingService.Verify(s => s.CreateAsync(It.IsAny<OnboardingInputDto>()), Times.Never);
            _logRepo.Verify(r => r.InsertAsync(It.Is<WorkflowTriggerLog>(l => l.Status == "Skipped"),
                It.IsAny<System.Threading.CancellationToken>(), It.IsAny<bool>()), Times.Once);
        }

        // ── Multi-condition: AND ─────────────────────────────────────────────

        [Fact]
        public async Task ExecuteTriggersAsync_AndConditions_AllPass_Triggered()
        {
            // Arrange
            const string q1 = "q_a1", q2 = "q_a2";
            var config = $@"{{""conditions"":[
                {{""id"":""c1"",""logic"":""AND"",""componentType"":""questionnaires"",""componentKey"":""questionnaire_q"",""resourceId"":""{q1}"",""operator"":""=="",""value"":""yes""}},
                {{""id"":""c2"",""logic"":""AND"",""componentType"":""questionnaires"",""componentKey"":""questionnaire_q"",""resourceId"":""{q2}"",""operator"":""=="",""value"":""pass""}}
            ],""mappings"":[],""autoMap"":false}}";
            SetupConnections(BuildConnection(config));
            _questionnaireAnswerRepo
                .Setup(r => r.GetByOnboardingIdAsync(SourceOnboardingId))
                .ReturnsAsync(new List<QuestionnaireAnswer>
                {
                    new QuestionnaireAnswer
                    {
                        OnboardingId = SourceOnboardingId, QuestionnaireId = 1L, IsValid = true,
                        TenantId = TestDataBuilder.DefaultTenantId,
                        Answer = JToken.Parse(
                            $@"{{""responses"":[{{""questionId"":""{q1}"",""answer"":""yes""}},{{""questionId"":""{q2}"",""answer"":""pass""}}]}}"),
                    }
                });

            // Act
            await _sut.ExecuteTriggersAsync(SourceOnboardingId, SourceWorkflowId, "Completed",
                TestDataBuilder.DefaultTenantId, "default");

            // Assert
            _onboardingService.Verify(s => s.CreateAsync(It.IsAny<OnboardingInputDto>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteTriggersAsync_AndConditions_OneFails_Skipped()
        {
            // Arrange — second condition fails
            const string q1 = "q_a1", q2 = "q_a2";
            var config = $@"{{""conditions"":[
                {{""id"":""c1"",""logic"":""AND"",""componentType"":""questionnaires"",""componentKey"":""questionnaire_q"",""resourceId"":""{q1}"",""operator"":""=="",""value"":""yes""}},
                {{""id"":""c2"",""logic"":""AND"",""componentType"":""questionnaires"",""componentKey"":""questionnaire_q"",""resourceId"":""{q2}"",""operator"":""=="",""value"":""pass""}}
            ],""mappings"":[],""autoMap"":false}}";
            SetupConnections(BuildConnection(config));
            _questionnaireAnswerRepo
                .Setup(r => r.GetByOnboardingIdAsync(SourceOnboardingId))
                .ReturnsAsync(new List<QuestionnaireAnswer>
                {
                    new QuestionnaireAnswer
                    {
                        OnboardingId = SourceOnboardingId, QuestionnaireId = 1L, IsValid = true,
                        TenantId = TestDataBuilder.DefaultTenantId,
                        Answer = JToken.Parse(
                            $@"{{""responses"":[{{""questionId"":""{q1}"",""answer"":""yes""}},{{""questionId"":""{q2}"",""answer"":""fail""}}]}}"),
                    }
                });

            // Act
            await _sut.ExecuteTriggersAsync(SourceOnboardingId, SourceWorkflowId, "Completed",
                TestDataBuilder.DefaultTenantId, "default");

            // Assert
            _onboardingService.Verify(s => s.CreateAsync(It.IsAny<OnboardingInputDto>()), Times.Never);
            _logRepo.Verify(r => r.InsertAsync(It.Is<WorkflowTriggerLog>(l => l.Status == "Skipped"),
                It.IsAny<System.Threading.CancellationToken>(), It.IsAny<bool>()), Times.Once);
        }

        // ── Multi-condition: OR ──────────────────────────────────────────────

        [Fact]
        public async Task ExecuteTriggersAsync_OrConditions_FirstPasses_Triggered()
        {
            // Arrange — first passes, second fails, OR → should trigger
            const string q1 = "q_o1", q2 = "q_o2";
            var config = $@"{{""conditions"":[
                {{""id"":""c1"",""logic"":""AND"",""componentType"":""questionnaires"",""componentKey"":""questionnaire_q"",""resourceId"":""{q1}"",""operator"":""=="",""value"":""yes""}},
                {{""id"":""c2"",""logic"":""OR"",""componentType"":""questionnaires"",""componentKey"":""questionnaire_q"",""resourceId"":""{q2}"",""operator"":""=="",""value"":""pass""}}
            ],""mappings"":[],""autoMap"":false}}";
            SetupConnections(BuildConnection(config));
            _questionnaireAnswerRepo
                .Setup(r => r.GetByOnboardingIdAsync(SourceOnboardingId))
                .ReturnsAsync(new List<QuestionnaireAnswer>
                {
                    new QuestionnaireAnswer
                    {
                        OnboardingId = SourceOnboardingId, QuestionnaireId = 1L, IsValid = true,
                        TenantId = TestDataBuilder.DefaultTenantId,
                        Answer = JToken.Parse(
                            $@"{{""responses"":[{{""questionId"":""{q1}"",""answer"":""yes""}},{{""questionId"":""{q2}"",""answer"":""nope""}}]}}"),
                    }
                });

            // Act
            await _sut.ExecuteTriggersAsync(SourceOnboardingId, SourceWorkflowId, "Completed",
                TestDataBuilder.DefaultTenantId, "default");

            // Assert
            _onboardingService.Verify(s => s.CreateAsync(It.IsAny<OnboardingInputDto>()), Times.Once);
        }

        // ── Defensive: bad config ────────────────────────────────────────────

        [Fact]
        public async Task ExecuteTriggersAsync_UnknownComponentType_Skipped()
        {
            // Arrange
            SetupConnections(BuildConnection(
                @"{""conditions"":[{""id"":""c1"",""logic"":""AND"",""componentType"":""unknown"",""componentKey"":""u1"",""resourceId"":""r1"",""operator"":""=="",""value"":""x""}],""mappings"":[],""autoMap"":false}"));

            // Act
            await _sut.ExecuteTriggersAsync(SourceOnboardingId, SourceWorkflowId, "Completed",
                TestDataBuilder.DefaultTenantId, "default");

            // Assert — unknown type must NOT pass
            _onboardingService.Verify(s => s.CreateAsync(It.IsAny<OnboardingInputDto>()), Times.Never);
            _logRepo.Verify(r => r.InsertAsync(It.Is<WorkflowTriggerLog>(l => l.Status == "Skipped"),
                It.IsAny<System.Threading.CancellationToken>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteTriggersAsync_EmptyOperator_Skipped()
        {
            // Arrange — empty operator = incomplete config
            SetupConnections(BuildConnection(QnConfig(QuestionId, "", "testwzy")));
            _questionnaireAnswerRepo
                .Setup(r => r.GetByOnboardingIdAsync(SourceOnboardingId))
                .ReturnsAsync(new List<QuestionnaireAnswer> { BuildAnswer(QuestionId, "testwzy") });

            // Act
            await _sut.ExecuteTriggersAsync(SourceOnboardingId, SourceWorkflowId, "Completed",
                TestDataBuilder.DefaultTenantId, "default");

            // Assert — empty operator must NOT pass
            _onboardingService.Verify(s => s.CreateAsync(It.IsAny<OnboardingInputDto>()), Times.Never);
            _logRepo.Verify(r => r.InsertAsync(It.Is<WorkflowTriggerLog>(l => l.Status == "Skipped"),
                It.IsAny<System.Threading.CancellationToken>(), It.IsAny<bool>()), Times.Once);
        }

        // ── CaseName construction ────────────────────────────────────────────

        [Fact]
        public async Task ExecuteTriggersAsync_SourceHasCaseName_UsesNameWithSuffix()
        {
            // Arrange
            SetupConnections(BuildConnection(NoConditions));
            OnboardingInputDto? captured = null;
            _onboardingService
                .Setup(s => s.CreateAsync(It.IsAny<OnboardingInputDto>()))
                .Callback<OnboardingInputDto>(d => captured = d)
                .ReturnsAsync(TargetOnboardingId);

            // Act
            await _sut.ExecuteTriggersAsync(SourceOnboardingId, SourceWorkflowId, "Completed",
                TestDataBuilder.DefaultTenantId, "default");

            // Assert
            captured.Should().NotBeNull();
            captured!.CaseName.Should().Be("Test Case (Triggered)");
        }

        [Fact]
        public async Task ExecuteTriggersAsync_NullCaseName_UsesCaseCode()
        {
            // Arrange
            _sut.SourceOnboarding = BuildSourceOnboarding(caseName: null, caseCode: "CASE-999");
            SetupConnections(BuildConnection(NoConditions));
            OnboardingInputDto? captured = null;
            _onboardingService
                .Setup(s => s.CreateAsync(It.IsAny<OnboardingInputDto>()))
                .Callback<OnboardingInputDto>(d => captured = d)
                .ReturnsAsync(TargetOnboardingId);

            // Act
            await _sut.ExecuteTriggersAsync(SourceOnboardingId, SourceWorkflowId, "Completed",
                TestDataBuilder.DefaultTenantId, "default");

            // Assert — must not produce " (Triggered)" with leading space
            captured.Should().NotBeNull();
            captured!.CaseName.Should().Be("CASE-999 (Triggered)");
            captured.CaseName.Should().NotStartWith(" ");
        }

        [Fact]
        public async Task ExecuteTriggersAsync_NullCaseNameAndCode_FallsBackToUnknown()
        {
            // Arrange
            _sut.SourceOnboarding = BuildSourceOnboarding(caseName: null, caseCode: null);
            SetupConnections(BuildConnection(NoConditions));
            OnboardingInputDto? captured = null;
            _onboardingService
                .Setup(s => s.CreateAsync(It.IsAny<OnboardingInputDto>()))
                .Callback<OnboardingInputDto>(d => captured = d)
                .ReturnsAsync(TargetOnboardingId);

            // Act
            await _sut.ExecuteTriggersAsync(SourceOnboardingId, SourceWorkflowId, "Completed",
                TestDataBuilder.DefaultTenantId, "default");

            // Assert
            captured.Should().NotBeNull();
            captured!.CaseName.Should().Be("Unknown (Triggered)");
        }

        // ── Log content ──────────────────────────────────────────────────────

        [Fact]
        public async Task ExecuteTriggersAsync_Triggered_LogHasSourceAndTargetIds()
        {
            // Arrange
            SetupConnections(BuildConnection(NoConditions));
            WorkflowTriggerLog? log = null;
            _logRepo
                .Setup(r => r.InsertAsync(It.IsAny<WorkflowTriggerLog>(),
                    It.IsAny<System.Threading.CancellationToken>(), It.IsAny<bool>()))
                .Callback<WorkflowTriggerLog, System.Threading.CancellationToken, bool>((l, ct, cp) => log = l)
                .ReturnsAsync(true);

            // Act
            await _sut.ExecuteTriggersAsync(SourceOnboardingId, SourceWorkflowId, "Completed",
                TestDataBuilder.DefaultTenantId, "default");

            // Assert
            log.Should().NotBeNull();
            log!.SourceOnboardingId.Should().Be(SourceOnboardingId);
            log.TargetOnboardingId.Should().Be(TargetOnboardingId);
            log.Status.Should().Be("Triggered");
            log.CompletionType.Should().Be("Completed");
        }

        [Fact]
        public async Task ExecuteTriggersAsync_Skipped_LogHasNoTargetId()
        {
            // Arrange
            SetupConnections(BuildConnection(QnConfig(QuestionId, "==", "testwzy")));
            _questionnaireAnswerRepo
                .Setup(r => r.GetByOnboardingIdAsync(SourceOnboardingId))
                .ReturnsAsync(new List<QuestionnaireAnswer> { BuildAnswer(QuestionId, "wrong") });
            WorkflowTriggerLog? log = null;
            _logRepo
                .Setup(r => r.InsertAsync(It.IsAny<WorkflowTriggerLog>(),
                    It.IsAny<System.Threading.CancellationToken>(), It.IsAny<bool>()))
                .Callback<WorkflowTriggerLog, System.Threading.CancellationToken, bool>((l, ct, cp) => log = l)
                .ReturnsAsync(true);

            // Act
            await _sut.ExecuteTriggersAsync(SourceOnboardingId, SourceWorkflowId, "Completed",
                TestDataBuilder.DefaultTenantId, "default");

            // Assert
            log.Should().NotBeNull();
            log!.Status.Should().Be("Skipped");
            log.TargetOnboardingId.Should().BeNull();
        }
    }
}