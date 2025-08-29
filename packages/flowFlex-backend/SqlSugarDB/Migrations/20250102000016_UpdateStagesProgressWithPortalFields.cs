using SqlSugar;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Update existing stages progress JSON to include VisibleInPortal and AttachmentManagementNeeded fields
    /// </summary>
    public class UpdateStagesProgressWithPortalFields_20250102000016
    {
        public static void Up(ISqlSugarClient db)
        {
            // This migration will update existing onboarding records to sync stages progress
            // with the new VisibleInPortal and AttachmentManagementNeeded fields from stages

            // First, get all onboarding records that have stages_progress_json
            var onboardings = db.Queryable<dynamic>()
                .AS("ff_onboarding")
                .Where("stages_progress_json IS NOT NULL AND stages_progress_json != '[]' AND stages_progress_json != ''")
                .Select("id, workflow_id, stages_progress_json")
                .ToList();

            foreach (var onboarding in onboardings)
            {
                try
                {
                    var onboardingId = onboarding.id;
                    var workflowId = onboarding.workflow_id;
                    var stagesProgressJson = onboarding.stages_progress_json?.ToString();

                    if (string.IsNullOrEmpty(stagesProgressJson))
                        continue;

                    // Parse existing stages progress JSON
                    var stagesProgress = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(stagesProgressJson);
                    if (stagesProgress == null || !stagesProgress.Any())
                        continue;

                    // Get stages for this workflow
                    var stages = db.Queryable<dynamic>()
                        .AS("ff_stage")
                        .Where($"workflow_id = {workflowId} AND is_valid = 1")
                        .Select("id, visible_in_portal, attachment_management_needed, components_json, estimated_duration")
                        .ToList();

                    var stageDict = stages.ToDictionary(
                        s => Convert.ToInt64(s.id),
                        s => new
                        {
                            VisibleInPortal = s.visible_in_portal ?? true,
                            AttachmentManagementNeeded = s.attachment_management_needed ?? false,
                            ComponentsJson = s.components_json?.ToString(),
                            EstimatedDuration = s.estimated_duration
                        });

                    bool hasChanges = false;

                    // Update each stage progress with new fields
                    foreach (var stageProgress in stagesProgress)
                    {
                        if (stageProgress.TryGetValue("StageId", out object stageIdObj) &&
                            stageIdObj != null)
                        {
                            if (long.TryParse(stageIdObj.ToString(), out long stageId) &&
                                stageDict.TryGetValue(stageId, out var stage))
                            {
                                // Add VisibleInPortal if not exists
                                if (!stageProgress.ContainsKey("VisibleInPortal"))
                                {
                                    stageProgress["VisibleInPortal"] = stage.VisibleInPortal;
                                    hasChanges = true;
                                }

                                // Add AttachmentManagementNeeded if not exists
                                if (!stageProgress.ContainsKey("AttachmentManagementNeeded"))
                                {
                                    stageProgress["AttachmentManagementNeeded"] = stage.AttachmentManagementNeeded;
                                    hasChanges = true;
                                }

                                // Update ComponentsJson if different
                                var currentComponentsJson = stageProgress.GetValueOrDefault("ComponentsJson")?.ToString();
                                if (currentComponentsJson != stage.ComponentsJson)
                                {
                                    stageProgress["ComponentsJson"] = stage.ComponentsJson;
                                    hasChanges = true;
                                }

                                // Update EstimatedDays if different
                                if (stageProgress.TryGetValue("EstimatedDays", out object estimatedDaysObj))
                                {
                                    var currentEstimatedDays = estimatedDaysObj != null ? Convert.ToDecimal(estimatedDaysObj) : (decimal?)null;
                                    if (currentEstimatedDays != stage.EstimatedDuration)
                                    {
                                        stageProgress["EstimatedDays"] = stage.EstimatedDuration;
                                        hasChanges = true;
                                    }
                                }
                                else if (stage.EstimatedDuration.HasValue)
                                {
                                    stageProgress["EstimatedDays"] = stage.EstimatedDuration;
                                    hasChanges = true;
                                }

                                // Update LastUpdatedTime
                                if (hasChanges)
                                {
                                    stageProgress["LastUpdatedTime"] = DateTimeOffset.UtcNow;
                                    stageProgress["LastUpdatedBy"] = "Migration_20250102000016";
                                }
                            }
                        }
                    }

                    // Update the record if there are changes
                    if (hasChanges)
                    {
                        var updatedJson = JsonSerializer.Serialize(stagesProgress);
                        db.Ado.ExecuteCommand(
                            "UPDATE ff_onboarding SET stages_progress_json = @json WHERE id = @id",
                            new { json = updatedJson, id = onboardingId });
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue with other records
                    Console.WriteLine($"Error updating onboarding {onboarding.id}: {ex.Message}");
                }
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            // Remove the new fields from stages progress JSON
            var onboardings = db.Queryable<dynamic>()
                .AS("ff_onboarding")
                .Where("stages_progress_json IS NOT NULL AND stages_progress_json != '[]' AND stages_progress_json != ''")
                .Select("id, stages_progress_json")
                .ToList();

            foreach (var onboarding in onboardings)
            {
                try
                {
                    var onboardingId = onboarding.id;
                    var stagesProgressJson = onboarding.stages_progress_json?.ToString();

                    if (string.IsNullOrEmpty(stagesProgressJson))
                        continue;

                    var stagesProgress = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(stagesProgressJson);
                    if (stagesProgress == null || !stagesProgress.Any())
                        continue;

                    bool hasChanges = false;

                    foreach (var stageProgress in stagesProgress)
                    {
                        if (stageProgress.Remove("VisibleInPortal"))
                            hasChanges = true;
                        if (stageProgress.Remove("AttachmentManagementNeeded"))
                            hasChanges = true;
                    }

                    if (hasChanges)
                    {
                        var updatedJson = JsonSerializer.Serialize(stagesProgress);
                        db.Ado.ExecuteCommand(
                            "UPDATE ff_onboarding SET stages_progress_json = @json WHERE id = @id",
                            new { json = updatedJson, id = onboardingId });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reverting onboarding {onboarding.id}: {ex.Message}");
                }
            }
        }
    }
}