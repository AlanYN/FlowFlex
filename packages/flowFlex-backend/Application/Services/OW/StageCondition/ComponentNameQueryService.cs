using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Helpers;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SqlSugar;

namespace FlowFlex.Application.Services.OW.StageCondition
{
    /// <summary>
    /// Service for querying component names (tasks, questions, fields) for rule display
    /// </summary>
    public interface IComponentNameQueryService
    {
        /// <summary>
        /// Build a map of component IDs to their display names
        /// </summary>
        Task<Dictionary<string, string>> BuildComponentNameMapAsync(List<FrontendRule> rules);
    }

    /// <summary>
    /// Implementation of component name query service
    /// </summary>
    public class ComponentNameQueryService : IComponentNameQueryService, IScopedService
    {
        private readonly ISqlSugarClient _db;
        private readonly UserContext _userContext;
        private readonly ILogger<ComponentNameQueryService> _logger;
        private readonly RuleConversionHelper _ruleConversionHelper;

        public ComponentNameQueryService(
            ISqlSugarClient db,
            UserContext userContext,
            ILogger<ComponentNameQueryService> logger)
        {
            _db = db;
            _userContext = userContext;
            _logger = logger;
            _ruleConversionHelper = new RuleConversionHelper(logger);
        }

        /// <summary>
        /// Build a map of component IDs to their display names (parallel query optimized)
        /// </summary>
        public async Task<Dictionary<string, string>> BuildComponentNameMapAsync(List<FrontendRule> rules)
        {
            var nameMap = new Dictionary<string, string>();

            try
            {
                // Extract all component IDs from rules
                var taskIds = new HashSet<long>();
                var questionnaireQuestionMap = new Dictionary<long, HashSet<long>>();
                var fieldIds = new HashSet<long>();

                foreach (var rule in rules)
                {
                    if (string.IsNullOrEmpty(rule.FieldPath)) continue;

                    var componentType = _ruleConversionHelper.DetectComponentType(rule);

                    _logger.LogDebug("Rule detection - ComponentType: {ComponentType}, FieldPath: {FieldPath}, DetectedType: {DetectedType}",
                        rule.ComponentType, rule.FieldPath, componentType);

                    if (componentType == "checklist")
                    {
                        var taskId = _ruleConversionHelper.ExtractTaskIdFromPath(rule.FieldPath);
                        if (taskId > 0) taskIds.Add(taskId);
                    }
                    else if (componentType == "questionnaire")
                    {
                        var (questionnaireId, questionId) = _ruleConversionHelper.ExtractQuestionnaireAndQuestionIdFromPath(rule.FieldPath);
                        _logger.LogDebug("Extracted questionnaire IDs - QuestionnaireId: {QuestionnaireId}, QuestionId: {QuestionId}",
                            questionnaireId, questionId);
                        if (questionnaireId > 0 && questionId > 0)
                        {
                            if (!questionnaireQuestionMap.ContainsKey(questionnaireId))
                            {
                                questionnaireQuestionMap[questionnaireId] = new HashSet<long>();
                            }
                            questionnaireQuestionMap[questionnaireId].Add(questionId);
                        }
                    }
                    else if (componentType == "field" || componentType == "fields")
                    {
                        var fieldId = _ruleConversionHelper.ExtractFieldIdFromFieldPath(rule.FieldPath);
                        if (fieldId > 0) fieldIds.Add(fieldId);
                    }
                }

                // Execute all queries in parallel
                var taskNamesTask = taskIds.Any()
                    ? QueryTaskNamesAsync(taskIds)
                    : Task.FromResult(new Dictionary<string, string>());

                var questionTitlesTask = questionnaireQuestionMap.Any()
                    ? QueryQuestionTitlesAsync(questionnaireQuestionMap)
                    : Task.FromResult(new Dictionary<string, string>());

                var fieldNamesTask = fieldIds.Any()
                    ? QueryFieldNamesAsync(fieldIds)
                    : Task.FromResult(new Dictionary<string, string>());

                await Task.WhenAll(taskNamesTask, questionTitlesTask, fieldNamesTask);

                // Merge results
                foreach (var kvp in await taskNamesTask)
                {
                    nameMap[kvp.Key] = kvp.Value;
                }
                foreach (var kvp in await questionTitlesTask)
                {
                    nameMap[kvp.Key] = kvp.Value;
                }
                foreach (var kvp in await fieldNamesTask)
                {
                    nameMap[kvp.Key] = kvp.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to build component name map, will use default names");
            }

            return nameMap;
        }

        /// <summary>
        /// Query task names from database
        /// </summary>
        private async Task<Dictionary<string, string>> QueryTaskNamesAsync(HashSet<long> taskIds)
        {
            var result = new Dictionary<string, string>();
            var taskIdList = taskIds.ToList();

            var tasks = await _db.Queryable<ChecklistTask>()
                .Where(t => taskIdList.Contains(t.Id) && t.IsValid)
                .Select(t => new { t.Id, t.Name })
                .ToListAsync();

            foreach (var task in tasks)
            {
                result[$"task_{task.Id}"] = task.Name ?? $"Task {task.Id}";
            }

            _logger.LogDebug("Found {Count} task names for IDs: {TaskIds}", tasks.Count, string.Join(", ", taskIds));
            return result;
        }

        /// <summary>
        /// Query question titles from questionnaire structure_json
        /// </summary>
        private async Task<Dictionary<string, string>> QueryQuestionTitlesAsync(Dictionary<long, HashSet<long>> questionnaireQuestionMap)
        {
            var result = new Dictionary<string, string>();
            var questionnaireIds = questionnaireQuestionMap.Keys.ToList();

            // Query questionnaire data using raw SQL
            var idPlaceholders = string.Join(", ", questionnaireIds.Select((_, i) => $"@id{i}"));
            var parameters = questionnaireIds.Select((id, i) => new SugarParameter($"@id{i}", id)).ToList();

            var questionnaires = await _db.Ado.SqlQueryAsync<QuestionnaireNameStructure>(
                $@"SELECT id AS ""Id"", name AS ""Name"", structure_json::text AS ""StructureJson"" 
                   FROM ff_questionnaire 
                   WHERE id IN ({idPlaceholders}) AND is_valid = true",
                parameters);

            _logger.LogDebug("Found {Count} questionnaires for IDs: {QuestionnaireIds}",
                questionnaires.Count, string.Join(", ", questionnaireIds));

            foreach (var questionnaire in questionnaires)
            {
                if (string.IsNullOrEmpty(questionnaire.StructureJson))
                {
                    _logger.LogDebug("Questionnaire {Id} has null structure", questionnaire.Id);
                    continue;
                }

                var neededQuestionIds = questionnaireQuestionMap.GetValueOrDefault(questionnaire.Id);
                if (neededQuestionIds == null || !neededQuestionIds.Any()) continue;

                JToken structureToken = null;
                try
                {
                    structureToken = JToken.Parse(questionnaire.StructureJson);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to parse structure JSON for questionnaire {Id}", questionnaire.Id);
                    continue;
                }

                var questionTitles = ExtractQuestionTitlesFromStructure(structureToken, neededQuestionIds);

                foreach (var questionId in neededQuestionIds)
                {
                    if (questionTitles.TryGetValue(questionId, out var title))
                    {
                        result[$"question_{questionId}"] = title;
                    }
                    else
                    {
                        var fallbackName = !string.IsNullOrEmpty(questionnaire.Name)
                            ? $"{questionnaire.Name} Q{questionId % 10000}"
                            : $"Question {questionId % 10000}";
                        result[$"question_{questionId}"] = fallbackName;
                        _logger.LogDebug("Question {QuestionId} not found in structure, using fallback: {FallbackName}",
                            questionId, fallbackName);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Query field names from DefineField table
        /// </summary>
        private async Task<Dictionary<string, string>> QueryFieldNamesAsync(HashSet<long> fieldIds)
        {
            var result = new Dictionary<string, string>();
            var fieldIdList = fieldIds.ToList();

            var fields = await _db.Queryable<Domain.Entities.DynamicData.DefineField>()
                .Where(f => fieldIdList.Contains(f.Id) && f.IsValid)
                .Select(f => new { f.Id, f.FieldName })
                .ToListAsync();

            foreach (var field in fields)
            {
                result[$"field_{field.Id}"] = field.FieldName ?? $"Field {field.Id}";
            }

            return result;
        }

        /// <summary>
        /// Extract question titles from questionnaire structure JSON
        /// </summary>
        private Dictionary<long, string> ExtractQuestionTitlesFromStructure(JToken structure, HashSet<long> questionIds)
        {
            var result = new Dictionary<long, string>();

            try
            {
                var questions = new List<JToken>();

                // Try various structure formats
                var sectionsQuestions = structure.SelectTokens("$.sections[*].questions[*]");
                questions.AddRange(sectionsQuestions);

                var sectionsItems = structure.SelectTokens("$.sections[*].items[*]");
                questions.AddRange(sectionsItems);

                var directQuestions = structure.SelectTokens("$.questions[*]");
                questions.AddRange(directQuestions);

                var directItems = structure.SelectTokens("$.items[*]");
                questions.AddRange(directItems);

                foreach (var question in questions)
                {
                    var idToken = question["id"];
                    var titleToken = question["title"] ?? question["label"] ?? question["name"] ?? question["text"];

                    if (idToken != null && titleToken != null)
                    {
                        var idStr = idToken.ToString();
                        if (long.TryParse(idStr, out var questionId) && questionIds.Contains(questionId))
                        {
                            result[questionId] = titleToken.ToString();
                        }
                    }
                }

                _logger.LogDebug("Extracted {Count} question titles from questionnaire structure for IDs: {QuestionIds}",
                    result.Count, string.Join(", ", questionIds));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to extract question titles from structure");
            }

            return result;
        }

        /// <summary>
        /// DTO for questionnaire name and structure query result
        /// </summary>
        private class QuestionnaireNameStructure
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public string StructureJson { get; set; }
        }
    }
}
