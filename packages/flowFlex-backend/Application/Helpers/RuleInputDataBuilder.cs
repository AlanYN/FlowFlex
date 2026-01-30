using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Service.OW;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FlowFlex.Application.Helpers
{
    /// <summary>
    /// Helper class for building input data for RulesEngine evaluation
    /// </summary>
    public class RuleInputDataBuilder
    {
        /// <summary>
        /// Build input data object for RulesEngine evaluation
        /// </summary>
        public static async Task<object> BuildInputDataAsync(
            long onboardingId,
            long stageId,
            IComponentDataService componentDataService,
            ILogger logger)
        {
            // Use the multi-stage version with single stage for backward compatibility
            return await BuildInputDataForMultipleStagesAsync(
                onboardingId, 
                new List<long> { stageId }, 
                componentDataService, 
                logger);
        }

        /// <summary>
        /// Build input data object for RulesEngine evaluation from multiple stages
        /// This method collects data from all specified stages and merges them
        /// </summary>
        public static async Task<object> BuildInputDataForMultipleStagesAsync(
            long onboardingId,
            List<long> stageIds,
            IComponentDataService componentDataService,
            ILogger logger)
        {
            try
            {
                if (stageIds == null || !stageIds.Any())
                {
                    logger.LogWarning("No stage IDs provided for building input data");
                    return new ExpandoObject();
                }

                // Build dynamic input object
                dynamic input = new ExpandoObject();
                var inputDict = (IDictionary<string, object>)input;

                // Merged data containers
                var mergedTasksDict = new SafeTasksDictionary();
                var mergedAnswersDict = new SafeNestedDictionary();
                var mergedAttachmentFileCount = 0;
                var mergedAttachmentTotalSize = 0L;
                var mergedAttachmentFileNames = new List<string>();

                // Collect data from all stages
                foreach (var stageId in stageIds.Distinct())
                {
                    logger.LogDebug("Building input data for onboarding {OnboardingId}, stage {StageId}", onboardingId, stageId);

                    // Get checklist data for this stage
                    var checklistData = await componentDataService.GetChecklistDataAsync(onboardingId, stageId);
                    var tasksDict = BuildNestedTasksDictionary(checklistData, stageId, logger);
                    
                    // Merge tasks
                    foreach (var checklistId in tasksDict.Keys)
                    {
                        if (!mergedTasksDict.ContainsKey(checklistId))
                        {
                            mergedTasksDict[checklistId] = new SafeTaskInnerDictionary();
                        }
                        foreach (var taskId in tasksDict[checklistId].Keys)
                        {
                            mergedTasksDict[checklistId][taskId] = tasksDict[checklistId][taskId];
                        }
                    }

                    // Get questionnaire data for this stage
                    var questionnaireData = await componentDataService.GetQuestionnaireDataAsync(onboardingId, stageId);
                    var answersDict = BuildNestedAnswersDictionary(questionnaireData, stageId, logger);
                    
                    // Merge answers
                    foreach (var questionnaireId in answersDict.Keys)
                    {
                        if (!mergedAnswersDict.ContainsKey(questionnaireId))
                        {
                            mergedAnswersDict[questionnaireId] = new SafeInnerDictionary();
                        }
                        foreach (var questionId in answersDict[questionnaireId].Keys)
                        {
                            mergedAnswersDict[questionnaireId][questionId] = answersDict[questionnaireId][questionId];
                        }
                    }

                    // Get attachment data for this stage
                    var attachmentData = await componentDataService.GetAttachmentDataAsync(onboardingId, stageId);
                    mergedAttachmentFileCount += attachmentData.FileCount;
                    mergedAttachmentTotalSize += attachmentData.TotalSize;
                    if (attachmentData.FileNames != null)
                    {
                        mergedAttachmentFileNames.AddRange(attachmentData.FileNames);
                    }
                }

                // Calculate merged checklist stats
                var totalTaskCount = 0;
                var completedTaskCount = 0;
                foreach (var checklistId in mergedTasksDict.Keys)
                {
                    foreach (var taskId in mergedTasksDict[checklistId].Keys)
                    {
                        totalTaskCount++;
                        if (mergedTasksDict[checklistId][taskId].isCompleted)
                        {
                            completedTaskCount++;
                        }
                    }
                }

                // Add merged checklist data
                inputDict["checklist"] = new
                {
                    status = totalTaskCount > 0 && completedTaskCount == totalTaskCount ? "Completed" : "InProgress",
                    completedCount = completedTaskCount,
                    totalCount = totalTaskCount,
                    completionPercentage = totalTaskCount > 0
                        ? (double)completedTaskCount / totalTaskCount * 100
                        : 0,
                    tasks = mergedTasksDict
                };

                // Add merged questionnaire data
                inputDict["questionnaire"] = new
                {
                    status = mergedAnswersDict.Count > 0 ? "Completed" : "Pending",
                    totalScore = 0, // Score calculation would need more complex logic
                    answers = mergedAnswersDict
                };

                // Add merged attachment data
                inputDict["attachments"] = new
                {
                    fileCount = mergedAttachmentFileCount,
                    hasAttachment = mergedAttachmentFileCount > 0,
                    totalSize = mergedAttachmentTotalSize,
                    fileNames = mergedAttachmentFileNames
                };

                // Get fields data (global, not stage-specific)
                var fieldsData = await componentDataService.GetFieldsDataAsync(onboardingId);
                inputDict["fields"] = SafeFieldsDictionary.FromDictionary(fieldsData);

                logger.LogDebug("Built merged input data from {StageCount} stages for onboarding {OnboardingId}", 
                    stageIds.Count, onboardingId);

                return input;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error building input data for onboarding {OnboardingId}, stages [{StageIds}]", 
                    onboardingId, string.Join(", ", stageIds));
                return new ExpandoObject();
            }
        }

        /// <summary>
        /// Extract unique source stage IDs from RulesJson
        /// </summary>
        public static List<long> ExtractSourceStageIdsFromRulesJson(string rulesJson, long defaultStageId, ILogger logger)
        {
            var stageIds = new HashSet<long> { defaultStageId };

            try
            {
                var jsonObj = Newtonsoft.Json.Linq.JToken.Parse(rulesJson);

                // Check if it's frontend format (has "logic" and "rules" properties)
                if (jsonObj is Newtonsoft.Json.Linq.JObject jObject && jObject.ContainsKey("rules"))
                {
                    var rules = jObject["rules"] as Newtonsoft.Json.Linq.JArray;
                    if (rules != null)
                    {
                        foreach (var rule in rules)
                        {
                            var sourceStageIdStr = rule["sourceStageId"]?.ToString();
                            if (!string.IsNullOrEmpty(sourceStageIdStr) && long.TryParse(sourceStageIdStr, out var sourceStageId))
                            {
                                stageIds.Add(sourceStageId);
                            }
                        }
                    }
                }

                logger.LogDebug("Extracted {Count} unique source stage IDs from RulesJson: [{StageIds}]", 
                    stageIds.Count, string.Join(", ", stageIds));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to extract source stage IDs from RulesJson, using default stage {DefaultStageId}", defaultStageId);
            }

            return stageIds.ToList();
        }

        /// <summary>
        /// Build nested tasks dictionary for RulesEngine access
        /// Format: tasks[checklistId][taskId] = TaskData { isCompleted, name, completionNotes }
        /// </summary>
        public static SafeTasksDictionary BuildNestedTasksDictionary(ChecklistData checklistData, long stageId, ILogger logger)
        {
            var result = new SafeTasksDictionary();

            if (checklistData?.Tasks == null || !checklistData.Tasks.Any())
            {
                logger.LogDebug("No tasks found in checklist data for stage {StageId}", stageId);
                return result;
            }

            logger.LogDebug("Building nested tasks dictionary with {TaskCount} tasks for stage {StageId}",
                checklistData.Tasks.Count, stageId);

            foreach (var task in checklistData.Tasks)
            {
                var checklistIdStr = task.ChecklistId.ToString();
                var taskIdStr = task.TaskId.ToString();

                logger.LogDebug("Adding task: ChecklistId={ChecklistId}, TaskId={TaskId}, IsCompleted={IsCompleted}",
                    checklistIdStr, taskIdStr, task.IsCompleted);

                var taskData = new TaskData
                {
                    isCompleted = task.IsCompleted,
                    name = task.Name ?? string.Empty,
                    completionNotes = task.CompletionNotes ?? string.Empty
                };

                if (!result.ContainsKey(checklistIdStr))
                {
                    result[checklistIdStr] = new SafeTaskInnerDictionary();
                }

                result[checklistIdStr][taskIdStr] = taskData;
            }

            logger.LogDebug("Built nested tasks dictionary with {ChecklistCount} checklists: [{ChecklistIds}]",
                result.Count, string.Join(", ", result.Keys));

            return result;
        }

        /// <summary>
        /// Build nested answers dictionary for RulesEngine access
        /// Format: answers[questionnaireId][questionId] = value
        /// </summary>
        public static SafeNestedDictionary BuildNestedAnswersDictionary(QuestionnaireData questionnaireData, long stageId, ILogger logger)
        {
            var result = new SafeNestedDictionary();

            if (questionnaireData?.Answers == null || !questionnaireData.Answers.Any())
            {
                logger.LogDebug("No questionnaire answers found for stage {StageId}", stageId);
                return result;
            }

            logger.LogDebug("Building nested answers dictionary with {AnswerCount} questionnaire answers for stage {StageId}",
                questionnaireData.Answers.Count, stageId);

            foreach (var kvp in questionnaireData.Answers)
            {
                var questionnaireIdStr = kvp.Key;

                logger.LogDebug("Processing questionnaire {QuestionnaireId}, value type: {ValueType}",
                    questionnaireIdStr, kvp.Value?.GetType().Name ?? "null");

                if (kvp.Value is Dictionary<string, object> nestedDict)
                {
                    var safeDict = new SafeInnerDictionary();
                    foreach (var item in nestedDict)
                    {
                        safeDict[item.Key] = item.Value;
                    }
                    result[questionnaireIdStr] = safeDict;
                    logger.LogDebug("Questionnaire {QuestionnaireId} has {QuestionCount} questions: [{QuestionIds}]",
                        questionnaireIdStr, nestedDict.Count, string.Join(", ", nestedDict.Keys));
                }
                else if (kvp.Value is Newtonsoft.Json.Linq.JObject jObj)
                {
                    var dict = new SafeInnerDictionary();
                    foreach (var prop in jObj.Properties())
                    {
                        dict[prop.Name] = prop.Value.Type == Newtonsoft.Json.Linq.JTokenType.Object
                            ? prop.Value.ToString()
                            : prop.Value.ToObject<object>();
                    }
                    result[questionnaireIdStr] = dict;
                    logger.LogDebug("Questionnaire {QuestionnaireId} (JObject) has {QuestionCount} questions: [{QuestionIds}]",
                        questionnaireIdStr, dict.Count, string.Join(", ", dict.Keys));
                }
                else if (kvp.Value is IDictionary<string, object> iDict)
                {
                    var safeDict = new SafeInnerDictionary();
                    foreach (var item in iDict)
                    {
                        safeDict[item.Key] = item.Value;
                    }
                    result[questionnaireIdStr] = safeDict;
                    logger.LogDebug("Questionnaire {QuestionnaireId} (IDictionary) has {QuestionCount} questions",
                        questionnaireIdStr, iDict.Count);
                }
                else
                {
                    if (!result.ContainsKey(questionnaireIdStr))
                    {
                        result[questionnaireIdStr] = new SafeInnerDictionary();
                    }
                    result[questionnaireIdStr][questionnaireIdStr] = kvp.Value;
                    logger.LogDebug("Questionnaire {QuestionnaireId} has single value: {Value}",
                        questionnaireIdStr, kvp.Value);
                }
            }

            logger.LogDebug("Built nested answers dictionary with {QuestionnaireCount} questionnaires: [{QuestionnaireIds}]",
                result.Count, string.Join(", ", result.Keys));

            return result;
        }
    }
}
