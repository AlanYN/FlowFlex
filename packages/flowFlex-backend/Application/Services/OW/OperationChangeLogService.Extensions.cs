using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FlowFlex.Domain.Shared.Enums.OW;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Operation Change Log Service Extensions for Independent Operations (Not Related to Onboarding)
    /// </summary>
    public partial class OperationChangeLogService
    {
        #region Helper Methods for Independent Operations

        /// <summary>
        /// Generic helper method for logging independent operations (without onboardingId and stageId)
        /// </summary>
        private async Task<bool> LogIndependentOperationAsync(
            OperationTypeEnum operationType,
            BusinessModuleEnum businessModule,
            long businessId,
            string entityName,
            string operationAction,
            string beforeData = null,
            string afterData = null,
            List<string> changedFields = null,
            string reason = null,
            string version = null,
            long? relatedEntityId = null,
            string relatedEntityType = null,
            string extendedData = null)
        {
            try
            {
                string operationTitle = $"{businessModule} {operationAction}: {entityName}";
                string operationDescription = $"{businessModule} '{entityName}' has been {operationAction.ToLower()} by {GetOperatorDisplayName()}";

                // Add related entity information if provided
                if (relatedEntityId.HasValue && !string.IsNullOrEmpty(relatedEntityType))
                {
                    operationDescription += $" in {relatedEntityType} ID {relatedEntityId.Value}";
                }

                // Add version information for publish operations
                if (!string.IsNullOrEmpty(version))
                {
                    operationDescription += $" as version {version}";
                }

                // Add reason for delete/unpublish operations
                if (!string.IsNullOrEmpty(reason))
                {
                    operationDescription += $" with reason: {reason}";
                }

                // Add changed fields information for update operations
                if (changedFields?.Any() == true)
                {
                    operationDescription += $". Changed fields: {string.Join(", ", changedFields)}";
                }

                // Generate default extended data if not provided
                if (string.IsNullOrEmpty(extendedData))
                {
                    var extendedDataObj = new Dictionary<string, object>
                    {
                        { $"{businessModule}Id", businessId },
                        { $"{businessModule}Name", entityName },
                        { $"{operationAction}At", DateTimeOffset.UtcNow }
                    };

                    if (relatedEntityId.HasValue && !string.IsNullOrEmpty(relatedEntityType))
                    {
                        extendedDataObj.Add($"{relatedEntityType}Id", relatedEntityId.Value);
                    }

                    if (!string.IsNullOrEmpty(reason))
                    {
                        extendedDataObj.Add("Reason", reason);
                    }

                    if (!string.IsNullOrEmpty(version))
                    {
                        extendedDataObj.Add("Version", version);
                    }

                    if (changedFields?.Any() == true)
                    {
                        extendedDataObj.Add("ChangedFieldsCount", changedFields.Count);
                    }

                    extendedData = JsonSerializer.Serialize(extendedDataObj);
                }

                return await LogOperationAsync(
                    operationType,
                    businessModule,
                    businessId,
                    null, // No onboardingId for independent operations
                    null, // No stageId for independent operations
                    operationTitle,
                    operationDescription,
                    beforeData,
                    afterData,
                    changedFields,
                    extendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log independent {OperationType} operation for {BusinessModule} {BusinessId}", 
                    operationType, businessModule, businessId);
                return false;
            }
        }

        #endregion

        #region Operation Categories Summary

        /// <summary>
        /// Get operation categories summary for independent operations
        /// This method provides a summary of all new operation types that are independent of onboarding
        /// </summary>
        /// <returns>Dictionary containing operation categories and their types</returns>
        public Dictionary<string, List<string>> GetIndependentOperationCategories()
        {
            return new Dictionary<string, List<string>>
            {
                {
                    "Workflow Operations",
                    new List<string>
                    {
                        OperationTypeEnum.WorkflowCreate.ToString(),
                        OperationTypeEnum.WorkflowUpdate.ToString(),
                        OperationTypeEnum.WorkflowDelete.ToString(),
                        OperationTypeEnum.WorkflowPublish.ToString(),
                        OperationTypeEnum.WorkflowUnpublish.ToString(),
                        OperationTypeEnum.WorkflowActivate.ToString(),
                        OperationTypeEnum.WorkflowDeactivate.ToString()
                    }
                },
                {
                    "Stage Operations (Independent)",
                    new List<string>
                    {
                        OperationTypeEnum.StageCreate.ToString(),
                        OperationTypeEnum.StageUpdate.ToString(),
                        OperationTypeEnum.StageDelete.ToString(),
                        OperationTypeEnum.StageOrderChange.ToString()
                    }
                },
                {
                    "Checklist Operations (Independent)",
                    new List<string>
                    {
                        OperationTypeEnum.ChecklistCreate.ToString(),
                        OperationTypeEnum.ChecklistUpdate.ToString(),
                        OperationTypeEnum.ChecklistDelete.ToString(),
                        OperationTypeEnum.ChecklistTaskCreate.ToString(),
                        OperationTypeEnum.ChecklistTaskUpdate.ToString(),
                        OperationTypeEnum.ChecklistTaskDelete.ToString()
                    }
                },
                {
                    "Questionnaire Operations (Independent)",
                    new List<string>
                    {
                        OperationTypeEnum.QuestionnaireCreate.ToString(),
                        OperationTypeEnum.QuestionnaireUpdate.ToString(),
                        OperationTypeEnum.QuestionnaireDelete.ToString(),
                        OperationTypeEnum.QuestionnairePublish.ToString(),
                        OperationTypeEnum.QuestionnaireUnpublish.ToString()
                    }
                },
                {
                    "Onboarding-Related Operations",
                    new List<string>
                    {
                        OperationTypeEnum.ChecklistTaskComplete.ToString(),
                        OperationTypeEnum.ChecklistTaskUncomplete.ToString(),
                        OperationTypeEnum.QuestionnaireAnswerSubmit.ToString(),
                        OperationTypeEnum.QuestionnaireAnswerUpdate.ToString(),
                        OperationTypeEnum.StaticFieldValueChange.ToString(),
                        OperationTypeEnum.FileUpload.ToString(),
                        OperationTypeEnum.FileDelete.ToString(),
                        OperationTypeEnum.FileUpdate.ToString(),
                        OperationTypeEnum.StageComplete.ToString(),
                        OperationTypeEnum.StageReopen.ToString(),
                        OperationTypeEnum.OnboardingStatusChange.ToString()
                    }
                },
                {
                    "Action Execution Operations",
                    new List<string>
                    {
                        OperationTypeEnum.StageActionExecution.ToString(),
                        OperationTypeEnum.TaskActionExecution.ToString(),
                        OperationTypeEnum.QuestionActionExecution.ToString()
                    }
                }
            };
        }

        #endregion
    }
}