using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.Dtos.OW.TriggerGraph;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.DynamicData;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Helpers;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Workflow Trigger asynchronous execution engine (OW-724).
    ///
    /// Sequence:
    ///   1. Find outbound TriggerConnections for the completed workflow.
    ///   2. For each enabled connection, evaluate TriggerConditions against source Case data.
    ///   3. If conditions pass: create target Case and apply Data Mapping.
    ///   4. Write TriggerLog (Triggered / Skipped / Failed).
    /// </summary>
    public class TriggerExecutionService : ITriggerExecutionService, IScopedService
    {
        // ── Dependencies ──────────────────────────────────────────────────────
        private readonly ISqlSugarClient                       _db;
        private readonly IWorkflowTriggerConnectionRepository  _connRepo;
        private readonly IWorkflowTriggerGraphRepository       _graphRepo;
        private readonly IWorkflowTriggerLogRepository         _logRepo;
        private readonly IOnboardingService                    _onboardingService;
        private readonly IStaticFieldValueRepository           _staticFieldRepo;
        private readonly IQuestionnaireAnswerRepository        _questionnaireAnswerRepo;
        private readonly IChecklistTaskCompletionRepository    _taskCompletionRepo;
        private readonly UserContext                           _userContext;
        private readonly ILogger<TriggerExecutionService>      _logger;

        public TriggerExecutionService(
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
        {
            _db                      = db;
            _connRepo                = connRepo;
            _graphRepo               = graphRepo;
            _logRepo                 = logRepo;
            _onboardingService       = onboardingService;
            _staticFieldRepo         = staticFieldRepo;
            _questionnaireAnswerRepo = questionnaireAnswerRepo;
            _taskCompletionRepo      = taskCompletionRepo;
            _userContext             = userContext;
            _logger                  = logger;
        }

        // ── Public entry point ────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task ExecuteTriggersAsync(
            long sourceOnboardingId,
            long sourceWorkflowId,
            string completionType,
            string? tenantId = null,
            string? appCode = null,
            string? operatorId = null,
            string? operatorName = null)
        {
            _logger.LogInformation(
                "[TriggerEngine] ExecuteTriggersAsync start | OnboardingId={OnboardingId} WorkflowId={WorkflowId} CompletionType={CompletionType}",
                sourceOnboardingId, sourceWorkflowId, completionType);

            try
            {
                // Prefer explicit parameters (passed from background tasks without HttpContext),
                // fall back to UserContext (set during normal HTTP request lifecycle).
                tenantId     ??= TenantContextHelper.GetTenantIdOrDefault(_userContext);
                appCode      ??= TenantContextHelper.GetAppCodeOrDefault(_userContext);
                operatorId   ??= _userContext?.UserId;
                operatorName ??= _userContext?.UserName ?? "System";

                // Warn if still using fallback values — helps diagnose silent multi-tenancy failures
                if (tenantId == "default")
                    _logger.LogWarning(
                        "[TriggerEngine] TenantId resolved to 'default' — UserContext may not have been populated correctly. " +
                        "Trigger results may be incorrect for non-default tenants. OnboardingId={Id}", sourceOnboardingId);
                if (appCode == "default")
                    _logger.LogWarning(
                        "[TriggerEngine] AppCode resolved to 'default' — UserContext may not have been populated correctly. " +
                        "Trigger results may be incorrect for non-default apps. OnboardingId={Id}", sourceOnboardingId);

                _logger.LogDebug(
                    "[TriggerEngine] Using TenantId={TenantId} AppCode={AppCode}",
                    tenantId, appCode);

                // Load source onboarding (needed for mapping values and CaseName)
                var sourceOnboarding = await _db.Queryable<Onboarding>()
                    .Where(o => o.Id == sourceOnboardingId && o.IsValid == true
                             && o.TenantId == tenantId && o.AppCode == appCode)
                    .FirstAsync();

                if (sourceOnboarding == null)
                {
                    _logger.LogWarning("[TriggerEngine] Source onboarding {Id} not found — aborting", sourceOnboardingId);
                    return;
                }

                // Find all enabled outbound connections for this workflow via Repository.
                // Repository now uses UserContext (set above) so tenant isolation is correct
                // in both HTTP-request and background-task contexts.
                var outbound = await _db.Queryable<WorkflowTriggerConnection>()
                    .Where(c => c.SourceWorkflowId == sourceWorkflowId
                             && c.IsEnabled == true
                             && c.IsValid == true
                             && c.TenantId == tenantId
                             && c.AppCode == appCode)
                    .OrderBy(c => c.ExecutionOrder)
                    .ToListAsync();

                if (!outbound.Any())
                {
                    _logger.LogInformation("[TriggerEngine] No outbound connections for WorkflowId={WorkflowId}", sourceWorkflowId);
                    return;
                }

                // Pre-load source static fields (all stages, all fields)
                var allSourceFields = await _staticFieldRepo.GetByOnboardingIdAsync(sourceOnboardingId);
                // Pre-load source questionnaire answers (all)
                var allSourceAnswers = await _questionnaireAnswerRepo.GetByOnboardingIdAsync(sourceOnboardingId);

                foreach (var conn in outbound)
                {
                    await ProcessConnectionAsync(
                        conn, sourceOnboarding, completionType,
                        allSourceFields, allSourceAnswers,
                        tenantId, appCode, operatorId, operatorName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[TriggerEngine] Unhandled error in ExecuteTriggersAsync for OnboardingId={Id}", sourceOnboardingId);
            }
        }

        // ── Per-connection processing ─────────────────────────────────────────

        private async Task ProcessConnectionAsync(
            WorkflowTriggerConnection conn,
            Onboarding source,
            string completionType,
            List<StaticFieldValue> allSourceFields,
            List<QuestionnaireAnswer> allSourceAnswers,
            string tenantId,
            string appCode,
            string? operatorId = null,
            string? operatorName = null)
        {
            var now      = DateTimeOffset.UtcNow;
            var userName = operatorName ?? _userContext?.UserName ?? "System";
            var userId   = long.TryParse(operatorId ?? _userContext?.UserId, out var uid) ? uid : 0L;

            // Propagate the correct tenant context and operator identity into the shared UserContext
            // so all downstream services read the right values regardless of whether this is running
            // inside an HTTP request or a background task.
            if (_userContext != null)
            {
                _userContext.TenantId = tenantId;
                _userContext.AppCode  = appCode;
                if (!string.IsNullOrEmpty(operatorName))  _userContext.UserName = operatorName;
                if (!string.IsNullOrEmpty(operatorId))    _userContext.UserId   = operatorId;
                if (string.IsNullOrEmpty(_userContext.UserName)) _userContext.UserName = "System";
            }

            // ── 0. 幂等检查（已禁用）────────────────────────────────────────
            // Product decision: rollback + re-complete SHOULD create a new downstream Case.
            // Do NOT add HasAlreadyTriggeredAsync guard here — every completion is intentional.

            var log = new WorkflowTriggerLog
            {
                ConnectionId        = conn.Id,
                SourceWorkflowId    = conn.SourceWorkflowId,
                TargetWorkflowId    = conn.TargetWorkflowId,
                SourceOnboardingId  = source.Id,
                CompletionType      = completionType,
                TenantId            = tenantId,
                AppCode             = appCode,
                // Audit fields — filled manually because background tasks have no HttpContext
                // and SqlSugar AOP cannot auto-populate them in that context.
                IsValid      = true,
                CreateDate   = now,
                ModifyDate   = now,
                CreateBy     = userName,
                ModifyBy     = userName,
                CreateUserId = userId,
                ModifyUserId = userId,
            };

            try
            {
                // Deserialise config
                var config = DeserialiseConfig(conn.ConfigJson);

                log.ConditionsSnapshot = JsonConvert.SerializeObject(config.Conditions);

                // ── 1. Evaluate conditions ────────────────────────────────────
                var (passed, reason) = await EvaluateConditionsAsync(
                    config.Conditions, source.Id, allSourceFields, allSourceAnswers);

                if (!passed)
                {
                    log.Status = "Skipped";
                    log.Reason = reason;
                    _logger.LogInformation("[TriggerEngine] Connection {ConnId} SKIPPED: {Reason}", conn.Id, reason);
                    await _logRepo.InsertAsync(log);
                    return;
                }

                // ── 2. Build OnboardingInputDto for target Case ───────────────
                var createDto = BuildTargetCaseDto(conn, config, source, allSourceFields);

                // ── 3. Create target Case ─────────────────────────────────────
                var targetId = await _onboardingService.CreateAsync(createDto);
                log.TargetOnboardingId = targetId;

                // ── 4. Apply Dynamic Field Mappings ───────────────────────────
                // Build a fieldId→(stageId, fieldName) map for the target workflow so StaticFieldValues
                // are stored with the correct StageId and FieldName (property key) for portal display.
                var targetStageFieldMap = await BuildTargetFieldStageMapAsync(conn.TargetWorkflowId, tenantId, appCode);

                if (config.Mappings?.Any(m => m.Enabled) == true)
                {
                    await ApplyDynamicMappingsAsync(
                        config.Mappings.Where(m => m.Enabled).ToList(),
                        source, targetId, allSourceFields, allSourceAnswers, targetStageFieldMap);
                }

                // ── 5. Apply Auto-mapped Field States ─────────────────────────
                if (config.AutoMap && config.AutoMappedStates?.Any(s => s.Enabled) == true)
                {
                    _logger.LogInformation(
                        "[TriggerEngine] AutoMappedStates count={Count}, enabled={Enabled}",
                        config.AutoMappedStates.Count,
                        config.AutoMappedStates.Count(s => s.Enabled));

                    var autoMappings = config.AutoMappedStates
                        .Where(s => s.Enabled && !string.IsNullOrEmpty(s.SourceId))
                        .Select(s =>
                        {
                            var targetFieldId = ExtractAutoMapTargetFieldId(s.Id);
                            var sourceType = s.SourceId!.StartsWith("input.fields.", StringComparison.OrdinalIgnoreCase)
                                ? "dynamic_field"
                                : s.SourceId.StartsWith("case.", StringComparison.OrdinalIgnoreCase)
                                    ? "case_field"
                                    : "static";
                            _logger.LogInformation(
                                "[TriggerEngine] AutoMap: id={Id} sourceId={SourceId} sourceType={Type} targetFieldId={TargetFieldId}",
                                s.Id, s.SourceId, sourceType, targetFieldId);
                            return new TriggerDataMappingConfig
                            {
                                Id            = s.Id,
                                SourceId      = s.SourceId!,
                                SourceName    = s.SourceName ?? string.Empty,
                                TargetFieldId = targetFieldId,
                                SourceType    = sourceType,
                                Enabled       = true
                            };
                        })
                        .Where(m => !string.IsNullOrEmpty(m.TargetFieldId))
                        .ToList();

                    if (autoMappings.Any())
                    {
                        await ApplyDynamicMappingsAsync(autoMappings, source, targetId, allSourceFields, allSourceAnswers, targetStageFieldMap);
                        _logger.LogInformation("[TriggerEngine] Applied {Count} auto-mapped fields for connection {ConnId}",
                            autoMappings.Count, conn.Id);
                    }
                }
                else
                {
                    _logger.LogInformation(
                        "[TriggerEngine] AutoMap skipped: AutoMap={AutoMap} StatesCount={Count}",
                        config.AutoMap,
                        config.AutoMappedStates?.Count ?? 0);
                }

                log.Status = "Triggered";
                log.MappingsSnapshot = JsonConvert.SerializeObject(config.Mappings ?? new List<TriggerDataMappingConfig>());

                _logger.LogInformation(
                    "[TriggerEngine] Connection {ConnId} TRIGGERED → new onboarding {TargetId}",
                    conn.Id, targetId);
            }
            catch (Exception ex)
            {
                log.Status = "Failed";
                log.Reason = ex.Message.Length > 900 ? ex.Message[..900] : ex.Message;
                _logger.LogError(ex, "[TriggerEngine] Connection {ConnId} FAILED", conn.Id);
            }

            await _logRepo.InsertAsync(log);
        }

        // ── Condition evaluation ──────────────────────────────────────────────

        private async Task<(bool passed, string reason)> EvaluateConditionsAsync(
            List<TriggerConditionConfig> conditions,
            long sourceOnboardingId,
            List<StaticFieldValue> fields,
            List<QuestionnaireAnswer> answers)
        {
            if (conditions == null || !conditions.Any())
                return (true, "No conditions — default trigger");

            var fieldValues  = BuildFieldValueMap(fields);
            var answerValues = BuildAnswerValueMap(answers);

            _logger.LogInformation(
                "[TriggerEngine] EvaluateConditions | ConditionCount={Count} AnswerMapKeys=[{Keys}] FieldMapKeys=[{FKeys}]",
                conditions.Count,
                string.Join(",", answerValues.Keys),
                string.Join(",", fieldValues.Keys.Take(5)));

            var results = new List<(string logic, bool result)>();
            foreach (var cond in conditions)
            {
                bool condPassed = await EvaluateSingleConditionAsync(cond, sourceOnboardingId, fieldValues, answerValues);
                results.Add((cond.Logic ?? "AND", condPassed));
            }

            bool final = results[0].result;
            for (int i = 1; i < results.Count; i++)
            {
                final = string.Equals(results[i].logic, "OR", StringComparison.OrdinalIgnoreCase)
                    ? final || results[i].result
                    : final && results[i].result;
            }

            if (!final)
            {
                var failing = conditions
                    .Where((c, i) => !results[i].result)
                    .Select(c => $"{c.ComponentName ?? c.ComponentKey} {c.Operator} {c.Value}");
                return (false, $"Condition not met: {string.Join("; ", failing)}");
            }

            return (true, "All conditions met");
        }

        private async Task<bool> EvaluateSingleConditionAsync(
            TriggerConditionConfig cond,
            long sourceOnboardingId,
            Dictionary<string, string> fieldValues,
            Dictionary<string, JToken?> answerValues)
        {
            if (string.Equals(cond.ComponentType, "checklist", StringComparison.OrdinalIgnoreCase))
            {
                return await EvaluateChecklistConditionAsync(cond, sourceOnboardingId);
            }

            if (string.Equals(cond.ComponentType, "fields", StringComparison.OrdinalIgnoreCase))
            {
                var fieldId = ExtractIdFromKey(cond.ComponentKey, "field_");
                if (string.IsNullOrEmpty(fieldId)) return false;
                if (string.IsNullOrEmpty(cond.Operator))
                {
                    _logger.LogWarning("[TriggerEngine] Field condition has empty Operator — skipping (treating as not passed)");
                    return false;
                }
                fieldValues.TryGetValue(fieldId, out var actual);
                return CompareValues(actual ?? string.Empty, cond.Operator ?? "==", cond.Value ?? string.Empty);
            }

            if (string.Equals(cond.ComponentType, "questionnaires", StringComparison.OrdinalIgnoreCase))
            {
                var questionId = cond.ResourceId;
                if (string.IsNullOrEmpty(questionId))
                {
                    _logger.LogWarning("[TriggerEngine] Questionnaire condition has empty ResourceId — skipping (treating as not passed)");
                    return false;
                }
                if (string.IsNullOrEmpty(cond.Operator))
                {
                    _logger.LogWarning("[TriggerEngine] Questionnaire condition has empty Operator — skipping (treating as not passed)");
                    return false;
                }
                answerValues.TryGetValue(questionId, out var token);
                var actual = token?.ToString() ?? string.Empty;
                _logger.LogInformation(
                    "[TriggerEngine] Questionnaire condition | QuestionId={QId} AvailableKeys=[{Keys}] ActualValue='{Actual}' Operator='{Op}' Expected='{Expected}'",
                    questionId,
                    string.Join(",", answerValues.Keys.Take(10)),
                    actual,
                    cond.Operator,
                    cond.Value);
                return CompareValues(actual, cond.Operator ?? "==", cond.Value ?? string.Empty);
            }

            // Unknown component type: skip rather than blocking on unrecognised types,
            // but log a warning so developers know the config is incomplete.
            _logger.LogWarning("[TriggerEngine] Unknown component type '{Type}' in condition — treating as not passed", cond.ComponentType);
            return false;
        }

        /// <summary>
        /// Evaluates a checklist-based condition against the source onboarding's task completions.
        /// Operators:
        ///   CompleteTask   — the specific task (ResourceId) must be completed
        ///   AllCompleted   — every task in the checklist (ComponentId) must be completed
        /// </summary>
        private async Task<bool> EvaluateChecklistConditionAsync(
            TriggerConditionConfig cond,
            long sourceOnboardingId)
        {
            switch (cond.Operator)
            {
                case "CompleteTask":
                {
                    if (!long.TryParse(cond.ResourceId, out var taskId))
                    {
                        _logger.LogWarning("[TriggerEngine] CompleteTask condition has invalid ResourceId '{Id}'", cond.ResourceId);
                        return false;
                    }
                    var completion = await _taskCompletionRepo.GetTaskCompletionAsync(sourceOnboardingId, taskId);
                    var result = completion?.IsCompleted == true;
                    _logger.LogInformation(
                        "[TriggerEngine] CompleteTask check | OnboardingId={OId} TaskId={TId} Found={Found} IsCompleted={Done} → {Result}",
                        sourceOnboardingId, taskId, completion != null, completion?.IsCompleted, result);
                    return result;
                }

                case "AllCompleted":
                {
                    if (!long.TryParse(cond.ComponentId, out var checklistId))
                    {
                        _logger.LogWarning("[TriggerEngine] AllCompleted condition has invalid ComponentId '{Id}'", cond.ComponentId);
                        return false;
                    }
                    var (total, completed) = await _taskCompletionRepo.GetCompletionStatsAsync(sourceOnboardingId, checklistId);
                    return total > 0 && completed >= total;
                }

                default:
                    _logger.LogWarning("[TriggerEngine] Unknown checklist operator '{Op}' — treating as not passed", cond.Operator);
                    return false;
            }
        }

        private bool CompareValues(string actual, string op, string expected)
        {
            return op switch
            {
                "==" => EqualValues(actual, expected),
                "!=" => !EqualValues(actual, expected),
                "contains" => ContainsValue(actual, expected),
                ">"  => double.TryParse(actual, out var a)  && double.TryParse(expected, out var e)  && a > e,
                ">=" => double.TryParse(actual, out var a2) && double.TryParse(expected, out var e2) && a2 >= e2,
                "<"  => double.TryParse(actual, out var a3) && double.TryParse(expected, out var e3) && a3 < e3,
                "<=" => double.TryParse(actual, out var a4) && double.TryParse(expected, out var e4) && a4 <= e4,
                // Unknown operator must NOT pass — returning true here would allow every trigger through
                _ => false
            };
        }

        /// <summary>
        /// Equality that handles both plain strings and multi-select (comma-separated or JSON array).
        /// For multi-select: the sets must be identical (same elements, order-insensitive).
        /// </summary>
        private static bool EqualValues(string actual, string expected)
        {
            // Try to interpret as sets if either side contains a comma or looks like a JSON array
            var actualList   = TryParseAsList(actual);
            var expectedList = TryParseAsList(expected);

            if (actualList != null && expectedList != null)
            {
                // Compare as sets, order-insensitive, with normalisation (handles label vs value mismatch)
                var normActual   = actualList.Select(NormalizeOptionValue).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var normExpected = expectedList.Select(NormalizeOptionValue).ToList();
                return normActual.Count == normExpected.Count &&
                       normExpected.All(e => normActual.Contains(e));
            }

            return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static List<string>? TryParseAsList(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;

            if (value.TrimStart().StartsWith('['))
            {
                try
                {
                    var arr = JArray.Parse(value);
                    return arr.Select(t => t.ToString().Trim()).Where(s => s.Length > 0).ToList();
                }
                catch { /* fall through */ }
            }

            if (value.Contains(','))
                return value.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

            return null;
        }

        /// <summary>
        /// Normalises a value for fuzzy comparison:
        /// converts underscores/hyphens to spaces, trims, and lower-cases.
        /// "commercial_follow-up" → "commercial follow up"
        /// "Commercial Follow-up" → "commercial follow up"
        /// </summary>
        private static string NormalizeOptionValue(string s) =>
            s.Replace('_', ' ').Replace('-', ' ')
             .Trim()
             .ToLowerInvariant()
             .Split(' ', StringSplitOptions.RemoveEmptyEntries)
             .Aggregate((a, b) => a + " " + b);

        /// <summary>
        /// Handles "contains" for both free-text substrings and multi-select answers.
        /// Multi-select answer may be stored as JSON array (["A","B"]) or comma-separated ("A,B").
        /// Expected is a comma-separated list of values that must ALL be present.
        /// </summary>
        private static bool ContainsValue(string actual, string expected)
        {
            if (string.IsNullOrEmpty(actual)) return false;

            var actualList = TryParseAsList(actual);
            if (actualList != null)
            {
                // Normalise both sides to handle label vs stored-value mismatch
                var normActual = actualList.Select(NormalizeOptionValue).ToList();
                var expectedItems = expected.Split(',')
                    .Select(s => NormalizeOptionValue(s.Trim()))
                    .Where(s => s.Length > 0);
                return expectedItems.All(e =>
                    normActual.Any(a => string.Equals(a, e, StringComparison.OrdinalIgnoreCase)));
            }

            return actual.Contains(expected, StringComparison.OrdinalIgnoreCase);
        }

        // ── Build target Case DTO ─────────────────────────────────────────────

        private OnboardingInputDto BuildTargetCaseDto(
            WorkflowTriggerConnection conn,
            TriggerConnectionConfig config,
            Onboarding source,
            List<StaticFieldValue> allSourceFields)
        {
            // Start with source Case values as defaults
            var dto = new OnboardingInputDto
            {
                WorkflowId    = conn.TargetWorkflowId,
                CaseName      = !string.IsNullOrEmpty(source.CaseName)
                                    ? $"{source.CaseName} (Triggered)"
                                    : $"{source.CaseCode ?? "Unknown"} (Triggered)",
                ContactEmail  = source.ContactEmail,
                ContactPerson = source.ContactPerson,
                Priority      = source.Priority ?? "Medium",
                Status        = "Started",
                StartDate     = DateTimeOffset.UtcNow,
            };

            // Apply Case Info states from config (user can override source mappings)
            if (config.CaseInfoStates?.Any() == true)
            {
                foreach (var state in config.CaseInfoStates.Where(s => s.Enabled && !string.IsNullOrEmpty(s.SourceId)))
                {
                    var value = ResolveCaseInfoSource(state.SourceId!, source, allSourceFields);
                    if (string.IsNullOrEmpty(value)) continue;

                    // targetId = "case_info_{fieldKey}" e.g. "case_info_caseName"
                    var fieldKey = state.Id.Replace("case_info_case_info_", "").Replace("case_info_", "");
                    ApplyCaseInfoValue(dto, fieldKey, value);
                }
            }

            return dto;
        }

        private string? ResolveCaseInfoSource(string sourceId, Onboarding source, List<StaticFieldValue> fields)
        {
            // "case.caseName" / "case.contactEmail" etc.
            if (sourceId.StartsWith("case.", StringComparison.OrdinalIgnoreCase))
            {
                var key = sourceId["case.".Length..];
                return key switch
                {
                    "caseName"      => source.CaseName,
                    "contactPerson" => source.ContactPerson,
                    "contactEmail"  => source.ContactEmail,
                    "priority"      => source.Priority,
                    _               => null
                };
            }

            // "input.fields.{fieldId}"
            if (sourceId.StartsWith("input.fields.", StringComparison.OrdinalIgnoreCase))
            {
                var fieldId = sourceId["input.fields.".Length..];
                var fv = fields.FirstOrDefault(f =>
                    f.FieldId?.ToString() == fieldId ||
                    string.Equals(f.FieldName, fieldId, StringComparison.OrdinalIgnoreCase));
                if (fv == null) return null;
                try { return JToken.Parse(fv.FieldValueJson ?? "null")?.ToString(); }
                catch { return fv.FieldValueJson; }
            }

            return null;
        }

        private static void ApplyCaseInfoValue(OnboardingInputDto dto, string fieldKey, string value)
        {
            switch (fieldKey)
            {
                case "caseName":      dto.CaseName      = value; break;
                case "contactPerson": dto.ContactPerson = value; break;
                case "contactEmail":  dto.ContactEmail  = value; break;
                case "priority":      dto.Priority      = value; break;
            }
        }

        // ── Dynamic Field Mappings ────────────────────────────────────────────

        private async Task ApplyDynamicMappingsAsync(
            List<TriggerDataMappingConfig> mappings,
            Onboarding source,
            long targetOnboardingId,
            List<StaticFieldValue> allSourceFields,
            List<QuestionnaireAnswer> allSourceAnswers,
            Dictionary<long, (long StageId, string FieldName)> targetFieldStageMap)
        {
            foreach (var mapping in mappings)
            {
                try
                {
                    var rawValue = mapping.SourceType switch
                    {
                        "static"        => mapping.StaticValue,
                        "dynamic_field" => ResolveDynamicFieldValue(mapping.SourceId, allSourceFields),
                        "questionnaire" => ResolveQuestionnaireValue(mapping.SourceId, allSourceAnswers),
                        "case_field"    => ResolveCaseInfoSource(mapping.SourceId, source, allSourceFields),
                        _               => null
                    };

                    if (string.IsNullOrEmpty(rawValue))
                        continue;

                    var targetFieldId = mapping.TargetFieldId;
                    if (string.IsNullOrEmpty(targetFieldId) ||
                        !targetFieldId.StartsWith("input.fields.", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var fieldIdStr = targetFieldId["input.fields.".Length..];
                    if (!long.TryParse(fieldIdStr, out var fieldIdLong))
                        continue;

                    // Look up the stage and property field name for this field in the target workflow
                    targetFieldStageMap.TryGetValue(fieldIdLong, out var fieldMeta);
                    var stageId   = fieldMeta.StageId;
                    // Use the DefineField.FieldName (property key) so the portal renderer can match
                    // by f.fieldName === key. Fall back to raw ID only if not found.
                    var fieldName = !string.IsNullOrEmpty(fieldMeta.FieldName)
                        ? fieldMeta.FieldName
                        : fieldIdStr;

                    var newField = new StaticFieldValue
                    {
                        OnboardingId   = targetOnboardingId,
                        StageId        = stageId,
                        FieldId        = fieldIdLong,
                        FieldName      = fieldName,
                        DisplayName    = fieldName,
                        // rawValue is a plain string — do NOT pre-serialize with JsonConvert.SerializeObject.
                        // SqlSugar's IsJson=true on this column will handle serialization automatically.
                        // Pre-serializing would result in double-quoting ("\"hello\"" → stored as "\"\\\"hello\\\"\"").
                        FieldValueJson = rawValue,
                        FieldType      = "text",
                        Status         = "Submitted",
                        IsLatest       = true,
                        TenantId       = TenantContextHelper.GetTenantIdOrDefault(_userContext),
                        AppCode        = TenantContextHelper.GetAppCodeOrDefault(_userContext),
                    };

                    await _staticFieldRepo.InsertAsync(newField);
                    _logger.LogInformation(
                        "[TriggerEngine] Wrote field: fieldId={FieldId} fieldName={FieldName} stageId={StageId} value={Value}",
                        fieldIdLong, fieldName, stageId, rawValue);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[TriggerEngine] Failed to apply mapping {MappingId}", mapping.Id);
                }
            }
        }

        /// <summary>
        /// Builds dictionaries mapping each field ID to the stage ID and property field name
        /// it belongs to, for the target workflow.
        /// </summary>
        private async Task<Dictionary<long, (long StageId, string FieldName)>> BuildTargetFieldStageMapAsync(
            long targetWorkflowId, string tenantId, string appCode)
        {
            var map = new Dictionary<long, (long, string)>();
            try
            {
                var stages = await _db.Queryable<Stage>()
                    .Where(s => s.WorkflowId == targetWorkflowId
                             && s.IsValid    == true
                             && s.TenantId   == tenantId
                             && s.AppCode    == appCode)
                    .ToListAsync();

                // Collect all field IDs across stages
                var allFieldIds = new List<long>();
                var stageFieldMap = new Dictionary<long, long>(); // fieldId → stageId

                foreach (var stage in stages)
                {
                    if (string.IsNullOrWhiteSpace(stage.ComponentsJson)) continue;
                    try
                    {
                        var components = JsonConvert.DeserializeObject<List<Domain.Shared.Models.StageComponent>>(stage.ComponentsJson);
                        if (components == null) continue;
                        foreach (var comp in components.Where(c => c.Key == "fields"))
                        {
                            foreach (var sf in comp.StaticFields ?? new List<Domain.Shared.Models.StaticFieldConfig>())
                            {
                                if (long.TryParse(sf.Id, out var fId) && !stageFieldMap.ContainsKey(fId))
                                {
                                    stageFieldMap[fId] = stage.Id;
                                    allFieldIds.Add(fId);
                                }
                            }
                        }
                    }
                    catch { /* ignore malformed JSON */ }
                }

                if (allFieldIds.Count == 0) return map;

                // Batch-load DefineField records to get the actual FieldName (property key)
                var defineFields = await _db.Queryable<DefineField>()
                    .Where(f => allFieldIds.Contains(f.Id) && f.IsValid == true)
                    .ToListAsync();

                var fieldNameById = defineFields.ToDictionary(f => f.Id, f => f.FieldName);

                foreach (var kvp in stageFieldMap)
                {
                    fieldNameById.TryGetValue(kvp.Key, out var fn);
                    map[kvp.Key] = (kvp.Value, fn ?? string.Empty);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[TriggerEngine] Failed to build target field stage map for workflow {WorkflowId}", targetWorkflowId);
            }
            return map;
        }

        private string? ResolveDynamicFieldValue(string? sourceId, List<StaticFieldValue> fields)
        {
            if (string.IsNullOrEmpty(sourceId) ||
                !sourceId.StartsWith("input.fields.", StringComparison.OrdinalIgnoreCase))
                return null;

            var fieldId = sourceId["input.fields.".Length..];
            var fv = fields.FirstOrDefault(f =>
                f.FieldId?.ToString() == fieldId ||
                string.Equals(f.FieldName, fieldId, StringComparison.OrdinalIgnoreCase));

            _logger.LogInformation(
                "[TriggerEngine] ResolveDynamicField: sourceId={SourceId} fieldId={FieldId} fieldCount={Count} found={Found} value={Value}",
                sourceId, fieldId, fields.Count, fv != null, fv?.FieldValueJson);

            if (fv == null) return null;

            try { return JToken.Parse(fv.FieldValueJson ?? "null")?.ToString(); }
            catch { return fv.FieldValueJson; }
        }

        private string? ResolveQuestionnaireValue(string? sourceId, List<QuestionnaireAnswer> answers)
        {
            if (string.IsNullOrEmpty(sourceId)) return null;

            // sourceId = input.questionnaire.answers["{qId}"]["{questionId}"]
            var match = Regex.Match(sourceId,
                @"input\.questionnaire\.answers\[""(?<qId>[^""]+)""\]\[""(?<questionId>[^""]+)""\]",
                RegexOptions.IgnoreCase);

            if (!match.Success) return null;

            var questionId  = match.Groups["questionId"].Value;

            foreach (var answer in answers)
            {
                if (answer.Answer == null) continue;
                try
                {
                    // Answer is a JToken — try sections[].questions[]/answers[]
                    var sections = answer.Answer is JArray arr ? arr
                                 : answer.Answer["sections"] as JArray;

                    if (sections == null) continue;

                    foreach (var section in sections)
                    {
                        var questions = section["questions"] as JArray ?? section["answers"] as JArray;
                        if (questions == null) continue;
                        foreach (var q in questions)
                        {
                            if (q["id"]?.ToString() == questionId ||
                                q["questionId"]?.ToString() == questionId)
                            {
                                return (q["value"] ?? q["answer"])?.ToString();
                            }
                        }
                    }
                }
                catch { /* ignore malformed */ }
            }

            return null;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Dictionary<string, string> BuildFieldValueMap(List<StaticFieldValue> fields)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in fields)
            {
                if (f.FieldId == null) continue;
                var id = f.FieldId.ToString()!;
                if (!map.ContainsKey(id))
                {
                    try { map[id] = JToken.Parse(f.FieldValueJson ?? "null")?.ToString() ?? string.Empty; }
                    catch { map[id] = f.FieldValueJson ?? string.Empty; }
                }
            }
            return map;
        }

        private Dictionary<string, JToken?> BuildAnswerValueMap(List<QuestionnaireAnswer> answers)
        {
            var map = new Dictionary<string, JToken?>(StringComparer.OrdinalIgnoreCase);

            // Sort by CreateDate descending so the latest answer wins when same questionId appears in multiple records.
            // TryAddQuestion uses "first one wins" (ContainsKey guard), so processing latest first is correct.
            var sorted = answers.OrderByDescending(a => a.CreateDate);

            foreach (var a in sorted)
            {
                if (a.Answer == null) continue;
                try
                {
                    _logger.LogInformation("[TriggerEngine] Parsing answer for questionnaire {QId} (Status={Status} CreateDate={Date}): RawJson={Json}",
                        a.QuestionnaireId, a.Status, a.CreateDate,
                        a.Answer.ToString(Newtonsoft.Json.Formatting.None)[..Math.Min(500, a.Answer.ToString().Length)]);

                    ParseAnswerToken(a.Answer, map);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[TriggerEngine] Failed to parse questionnaire answer for questionnaire {QId}", a.QuestionnaireId);
                }
            }
            return map;
        }

        /// <summary>
        /// Recursively tries multiple known answer_json layouts to extract {questionId → value} pairs.
        /// Supported layouts:
        ///   A) JArray of questions:   [{id, value}, ...]
        ///   B) {sections: [{questions: [{id, value}]}]}
        ///   C) {answers: [{questionId, value}, ...]}
        ///   D) {questions: [{id, value}]}
        ///   E) {responses: [{questionId, answer, responseText, ...}]}   ← this project's primary format
        ///   F) {sectionInstances: [{responses: [...]}]}                 ← repeatable-section variant
        ///   G) Direct dict: {questionId: value, ...}
        /// </summary>
        private static void ParseAnswerToken(JToken token, Dictionary<string, JToken?> map)
        {
            if (token is JArray arr)
            {
                // Layout A: top-level array of questions
                foreach (var q in arr)
                    TryAddQuestion(q, map);
                return;
            }

            if (token is not JObject obj) return;

            // Layout E (this project's primary format): {responses: [...]}
            if (obj["responses"] is JArray responses)
            {
                foreach (var q in responses) TryAddQuestion(q, map);
                return;
            }

            // Layout F: {sectionInstances: [{responses: [...]}]}  — repeatable sections
            if (obj["sectionInstances"] is JArray sectionInstances)
            {
                foreach (var instance in sectionInstances)
                {
                    if (instance["responses"] is JArray instResponses)
                        foreach (var q in instResponses) TryAddQuestion(q, map);
                }
                return;
            }

            // Layout B: {sections: [...]}
            if (obj["sections"] is JArray sections)
            {
                foreach (var section in sections)
                {
                    var qs = section["questions"] as JArray ?? section["answers"] as JArray;
                    if (qs != null)
                        foreach (var q in qs) TryAddQuestion(q, map);
                }
                return;
            }

            // Layout C: {answers: [...]}
            if (obj["answers"] is JArray answers)
            {
                foreach (var q in answers) TryAddQuestion(q, map);
                return;
            }

            // Layout D: {questions: [...]}
            if (obj["questions"] is JArray questions)
            {
                foreach (var q in questions) TryAddQuestion(q, map);
                return;
            }

            // Layout G: flat dict where every key is a question ID
            foreach (var prop in obj.Properties())
            {
                if (!map.ContainsKey(prop.Name))
                    map[prop.Name] = prop.Value;
            }
        }

        private static void TryAddQuestion(JToken q, Dictionary<string, JToken?> map)
        {
            var qId = q["id"]?.ToString() ?? q["questionId"]?.ToString() ?? q["question_id"]?.ToString();
            if (string.IsNullOrEmpty(qId) || map.ContainsKey(qId)) return;
            // For this project's response format: answer field holds the stored value,
            // responseText holds the display text (labels). We prefer "answer" for comparison
            // since condition values are configured against stored option values, not labels.
            // Fall back to "value", then "values", then "responseText" as last resort.
            var val = q["answer"] ?? q["value"] ?? q["values"] ?? q["responseText"];
            map[qId] = val;
        }

        private static string ExtractIdFromKey(string? key, string prefix)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            return key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? key[prefix.Length..]
                : string.Empty;
        }

        /// <summary>
        /// Extracts the target field path from an auto-map id.
        /// Frontend id format: "auto_{sourceId}_{targetId}"
        /// where both sourceId and targetId are full paths like "input.fields.{fieldId}".
        /// We split on the first occurrence of "_input.fields." to find the target part.
        /// </summary>
        private static string ExtractAutoMapTargetFieldId(string? autoMapId)
        {
            if (string.IsNullOrEmpty(autoMapId)) return string.Empty;
            // id = "auto_{sourceId}_{targetId}"
            // Both source and target already contain "input.fields." so we find the second occurrence
            // e.g. "auto_input.fields.AAA_input.fields.BBB" → target = "input.fields.BBB"
            // Strategy: strip "auto_" prefix, then find the last "_input.fields." separator
            if (!autoMapId.StartsWith("auto_", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            var withoutPrefix = autoMapId["auto_".Length..]; // "input.fields.AAA_input.fields.BBB"
            // Find second "_input.fields." (separator between source and target)
            const string sep = "_input.fields.";
            var sepIndex = withoutPrefix.IndexOf(sep, StringComparison.OrdinalIgnoreCase);
            if (sepIndex < 0)
            {
                // Fallback: old format "auto_{sourceFieldId}_{targetFieldId}" (numeric IDs)
                // Split on "_" and take last part, wrap in input.fields.
                var parts = autoMapId.Split('_', 3);
                if (parts.Length < 3) return string.Empty;
                return $"input.fields.{parts[2]}";
            }

            // The target starts at sepIndex+1 (the underscore before "input.fields.XXX")
            return withoutPrefix[(sepIndex + 1)..]; // "input.fields.BBB"
        }

        private static TriggerConnectionConfig DeserialiseConfig(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new TriggerConnectionConfig();
            try { return JsonConvert.DeserializeObject<TriggerConnectionConfig>(json) ?? new TriggerConnectionConfig(); }
            catch { return new TriggerConnectionConfig(); }
        }
    }
}
