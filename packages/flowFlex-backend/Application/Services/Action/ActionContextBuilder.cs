using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Domain.Shared.Enums;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Helpers;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SqlSugar;

namespace FlowFlex.Application.Services.Action
{
    public class ActionContextBuilder : IActionContextBuilder, IScopedService
    {
        private readonly ISqlSugarClient _db;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IStaticFieldValueService _staticFieldValueService;
        private readonly IComponentDataService _componentDataService;
        private readonly IEncryptionService _encryptionService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly UserContext _userContext;
        private readonly ILogger<ActionContextBuilder> _logger;

        public ActionContextBuilder(
            ISqlSugarClient db,
            IOnboardingRepository onboardingRepository,
            IStaticFieldValueService staticFieldValueService,
            IComponentDataService componentDataService,
            IEncryptionService encryptionService,
            IHttpClientFactory httpClientFactory,
            UserContext userContext,
            ILogger<ActionContextBuilder> logger)
        {
            _db = db;
            _onboardingRepository = onboardingRepository;
            _staticFieldValueService = staticFieldValueService;
            _componentDataService = componentDataService;
            _encryptionService = encryptionService;
            _httpClientFactory = httpClientFactory;
            _userContext = userContext;
            _logger = logger;
        }

        public async Task<Dictionary<string, object>> BuildStageConditionTriggerContextAsync(
            ActionExecutionContext context,
            long actionDefinitionId,
            long? integrationId,
            JToken? previousActionResult = null,
            CancellationToken cancellationToken = default)
        {
            var onboarding = await _onboardingRepository.GetByIdWithoutTenantFilterAsync(context.OnboardingId);

            var actionDefinition = await _db.Queryable<Domain.Entities.Action.ActionDefinition>()
                .Where(a => a.Id == actionDefinitionId && a.IsValid)
                .Where(a => a.TenantId == context.TenantId)
                .FirstAsync();

            // 1. Base fields
            var contextData = new Dictionary<string, object>
            {
                ["OnboardingId"] = context.OnboardingId,
                ["StageId"] = context.StageId,
                ["ConditionId"] = context.ConditionId,
                ["TenantId"] = context.TenantId,
                ["ActionDefinitionId"] = actionDefinitionId,
                ["ActionName"] = actionDefinition?.ActionName ?? "",
                ["TriggerSource"] = "StageCondition",
                ["CaseName"] = onboarding?.CaseName ?? "",
                ["CaseCode"] = onboarding?.CaseCode ?? "",
                ["WorkflowId"] = onboarding?.WorkflowId ?? 0L
            };

            // 2. Static fields (flatten to top level, camelCase)
            var fieldData = await GetStaticFieldValuesAsCamelCaseAsync(context.OnboardingId);
            foreach (var kvp in fieldData)
            {
                contextData[kvp.Key] = kvp.Value;
            }

            // 3. Integration token
            if (integrationId.HasValue)
            {
                var token = await GetIntegrationTokenAsync(integrationId.Value);
                if (!string.IsNullOrEmpty(token))
                {
                    contextData["integrationToken"] = token;
                }
            }

            // 4. Questionnaire answers from all completed stages + current triggering stage
            await AggregateQuestionnaireAnswersAsync(context.OnboardingId, context.StageId, onboarding, contextData);

            // 5. Previous action result
            if (previousActionResult != null)
            {
                contextData["previousActionResult"] = previousActionResult;
                FlattenPreviousActionResult(previousActionResult, contextData);
            }

            return contextData;
        }

        private async Task AggregateQuestionnaireAnswersAsync(
            long onboardingId,
            long currentStageId,
            Onboarding? onboarding,
            Dictionary<string, object> contextData)
        {
            var answersList = new List<ActionQuestionnaireAnswerDto>();
            var answerMap = new Dictionary<string, Dictionary<string, object>>();
            var answerByQuestionId = new Dictionary<string, object>();

            try
            {
                if (onboarding == null)
                {
                    SetEmptyQuestionnaireContext(contextData, answersList, answerMap, answerByQuestionId);
                    return;
                }

                // Parse stages progress to find completed stages
                var stagesProgress = StagesProgressHelper.ParseStagesProgress(
                    onboarding.StagesProgressJson, _logger, $"OnboardingId={onboardingId}");

                // Collect completed stages + always include the current triggering stage
                var stageIdsToCollect = stagesProgress
                    .Where(sp => sp.IsCompleted || sp.Status == "Completed")
                    .OrderBy(sp => sp.CompletionTime ?? DateTimeOffset.MaxValue)
                    .ThenBy(sp => sp.StageId)
                    .Select(sp => sp.StageId)
                    .ToList();

                // Ensure current stage is included (it may not be marked Completed yet when Action fires)
                if (!stageIdsToCollect.Contains(currentStageId))
                {
                    stageIdsToCollect.Add(currentStageId);
                }

                // For each stage, get questionnaire data
                foreach (var stageId in stageIdsToCollect)
                {
                    QuestionnaireData questionnaireData;
                    try
                    {
                        questionnaireData = await _componentDataService.GetQuestionnaireDataAsync(onboardingId, stageId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get questionnaire data for stage {StageId}, skipping", stageId);
                        continue;
                    }

                    if (questionnaireData?.Answers == null || !questionnaireData.Answers.Any())
                        continue;

                    foreach (var qEntry in questionnaireData.Answers)
                    {
                        var questionnaireId = qEntry.Key;

                        if (!answerMap.ContainsKey(questionnaireId))
                            answerMap[questionnaireId] = new Dictionary<string, object>();

                        if (qEntry.Value is Dictionary<string, object> questionsDict)
                        {
                            foreach (var qItem in questionsDict)
                            {
                                var questionId = qItem.Key;
                                var answer = NormalizeAnswerValue(qItem.Value);

                                answerMap[questionnaireId][questionId] = answer;
                                // Later stage overwrites earlier stage (ordered by CompletionTime)
                                answerByQuestionId[questionId] = answer;

                                answersList.Add(new ActionQuestionnaireAnswerDto
                                {
                                    StageId = stageId.ToString(),
                                    QuestionnaireId = questionnaireId,
                                    QuestionId = questionId,
                                    QuestionText = "",
                                    QuestionType = "",
                                    Answer = answer
                                });
                            }
                        }
                        else if (qEntry.Value is JObject jObj)
                        {
                            foreach (var prop in jObj.Properties())
                            {
                                var questionId = prop.Name;
                                var answer = NormalizeAnswerValue(
                                    prop.Value.Type == JTokenType.Object || prop.Value.Type == JTokenType.Array
                                        ? prop.Value.ToString()
                                        : (object)(prop.Value.ToString()));

                                answerMap[questionnaireId][questionId] = answer;
                                answerByQuestionId[questionId] = answer;

                                answersList.Add(new ActionQuestionnaireAnswerDto
                                {
                                    StageId = stageId.ToString(),
                                    QuestionnaireId = questionnaireId,
                                    QuestionId = questionId,
                                    QuestionText = "",
                                    QuestionType = "",
                                    Answer = answer
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error aggregating questionnaire answers for onboarding {OnboardingId}", onboardingId);
            }

            SetEmptyQuestionnaireContext(contextData, answersList, answerMap, answerByQuestionId);
        }

        private static object NormalizeAnswerValue(object value)
        {
            if (value is string str && str.TrimStart().StartsWith("["))
            {
                try
                {
                    var arr = JArray.Parse(str);
                    return string.Join(",", arr.Select(t => t.ToString()));
                }
                catch
                {
                    // Not valid JSON array, return as-is
                }
            }
            return value;
        }

        private static void SetEmptyQuestionnaireContext(
            Dictionary<string, object> contextData,
            List<ActionQuestionnaireAnswerDto> answersList,
            Dictionary<string, Dictionary<string, object>> answerMap,
            Dictionary<string, object> answerByQuestionId)
        {
            contextData["questionnaireAnswers"] = answersList;
            contextData["questionnaireAnswerMap"] = answerMap;
            contextData["questionnaireAnswerByQuestionId"] = answerByQuestionId;
        }

        private void FlattenPreviousActionResult(JToken previousResult, Dictionary<string, object> contextData)
        {
            try
            {
                var responseStr = previousResult?["response"]?.ToString();
                if (string.IsNullOrEmpty(responseStr)) return;

                JObject responseObj;
                try { responseObj = JObject.Parse(responseStr); }
                catch { return; }

                var dataObj = responseObj["data"] as JObject;
                if (dataObj != null)
                {
                    foreach (var prop in dataObj.Properties())
                    {
                        var key = $"prev_{prop.Name}";
                        if (prop.Value.Type == JTokenType.Null) continue;
                        contextData[key] = prop.Value.Type == JTokenType.Object || prop.Value.Type == JTokenType.Array
                            ? prop.Value.ToString()
                            : prop.Value.ToObject<object>();
                    }
                }

                foreach (var prop in responseObj.Properties())
                {
                    if (prop.Name == "data" || prop.Name == "success" || prop.Name == "code" || prop.Name == "msg") continue;
                    var key = $"prev_{prop.Name}";
                    if (!contextData.ContainsKey(key) && prop.Value.Type != JTokenType.Null)
                    {
                        contextData[key] = prop.Value.Type == JTokenType.Object || prop.Value.Type == JTokenType.Array
                            ? prop.Value.ToString()
                            : prop.Value.ToObject<object>();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error flattening previous action result");
            }
        }

        private async Task<Dictionary<string, object>> GetStaticFieldValuesAsCamelCaseAsync(long onboardingId)
        {
            var result = new Dictionary<string, object>();
            try
            {
                var fieldValues = await _staticFieldValueService.GetByOnboardingIdAsync(onboardingId);
                if (fieldValues == null) return result;

                foreach (var field in fieldValues)
                {
                    object value = null;
                    if (!string.IsNullOrEmpty(field.FieldValueJson))
                    {
                        try
                        {
                            var parsed = JsonConvert.DeserializeObject(field.FieldValueJson);
                            if (parsed is JArray jArray && jArray.Count == 1)
                                value = jArray[0]?.ToString();
                            else
                                value = parsed;
                        }
                        catch
                        {
                            value = field.FieldValueJson;
                        }
                    }

                    if (value != null && !string.IsNullOrEmpty(field.FieldName))
                    {
                        var camelKey = ToCamelCase(field.FieldName);
                        result[camelKey] = value;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get StaticFieldValues for onboarding {OnboardingId}", onboardingId);
            }
            return result;
        }

        internal static string ToCamelCase(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName)) return fieldName;

            var words = fieldName.Split(new[] { ' ', '/', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return fieldName;

            var sb = new StringBuilder();
            sb.Append(words[0].ToLowerInvariant());
            for (int i = 1; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    sb.Append(char.ToUpperInvariant(words[i][0]));
                    if (words[i].Length > 1)
                        sb.Append(words[i].Substring(1).ToLowerInvariant());
                }
            }
            return sb.ToString();
        }

        private async Task<string?> GetIntegrationTokenAsync(long integrationId)
        {
            try
            {
                var integration = await _db.Queryable<Domain.Entities.Integration.Integration>()
                    .Where(i => i.Id == integrationId && i.IsValid)
                    .FirstAsync();

                if (integration == null)
                {
                    _logger.LogWarning("Integration {IntegrationId} not found", integrationId);
                    return null;
                }

                if (integration.AuthMethod != AuthenticationMethod.OAuth2)
                    return null;

                if (string.IsNullOrEmpty(integration.EncryptedCredentials) || integration.EncryptedCredentials == "{}")
                    return null;

                Dictionary<string, string> credentials;
                try
                {
                    var json = _encryptionService.Decrypt(integration.EncryptedCredentials);
                    credentials = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to decrypt credentials for Integration {IntegrationId}", integrationId);
                    return null;
                }

                if (credentials == null ||
                    !credentials.TryGetValue("clientId", out var clientId) ||
                    !credentials.TryGetValue("clientSecret", out var clientSecret))
                    return null;

                if (string.IsNullOrEmpty(integration.EndpointUrl))
                    return null;

                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var request = new HttpRequestMessage(HttpMethod.Post, integration.EndpointUrl);
                var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" }
                });

                var response = await httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OAuth2 token request failed for Integration {IntegrationId}: {StatusCode}",
                        integrationId, response.StatusCode);
                    return null;
                }

                var tokenResponse = JObject.Parse(content);
                return tokenResponse["access_token"]?.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OAuth2 token for Integration {IntegrationId}", integrationId);
                return null;
            }
        }
    }
}
