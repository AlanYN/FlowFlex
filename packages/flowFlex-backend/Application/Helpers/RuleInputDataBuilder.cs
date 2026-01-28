using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Service.OW;
using Microsoft.Extensions.Logging;

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
            try
            {
                // Get component data
                var checklistData = await componentDataService.GetChecklistDataAsync(onboardingId, stageId);
                var questionnaireData = await componentDataService.GetQuestionnaireDataAsync(onboardingId, stageId);
                var attachmentData = await componentDataService.GetAttachmentDataAsync(onboardingId, stageId);
                var fieldsData = await componentDataService.GetFieldsDataAsync(onboardingId);

                // Build dynamic input object
                dynamic input = new ExpandoObject();
                var inputDict = (IDictionary<string, object>)input;

                // Build nested tasks dictionary
                var tasksDict = BuildNestedTasksDictionary(checklistData, stageId, logger);

                // Add checklist data with nested tasks dictionary
                inputDict["checklist"] = new
                {
                    status = checklistData.Status,
                    completedCount = checklistData.CompletedCount,
                    totalCount = checklistData.TotalCount,
                    completionPercentage = checklistData.TotalCount > 0
                        ? (double)checklistData.CompletedCount / checklistData.TotalCount * 100
                        : 0,
                    tasks = tasksDict
                };

                // Build nested answers dictionary
                var answersDict = BuildNestedAnswersDictionary(questionnaireData, stageId, logger);

                // Add questionnaire data with nested answers dictionary
                inputDict["questionnaire"] = new
                {
                    status = questionnaireData.Status,
                    totalScore = questionnaireData.TotalScore,
                    answers = answersDict
                };

                // Add attachment data
                inputDict["attachments"] = new
                {
                    fileCount = attachmentData.FileCount,
                    hasAttachment = attachmentData.FileCount > 0,
                    totalSize = attachmentData.TotalSize,
                    fileNames = attachmentData.FileNames
                };

                // Add fields data (dynamic data from onboarding)
                inputDict["fields"] = SafeFieldsDictionary.FromDictionary(fieldsData);

                return input;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error building input data for onboarding {OnboardingId}, stage {StageId}", onboardingId, stageId);
                return new ExpandoObject();
            }
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
