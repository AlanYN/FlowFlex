using System;
using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Simplify StagesProgress Structure Migration
    /// 
    /// This migration addresses the architectural change where:
    /// 1. OnboardingStageProgress now only stores stageId and completion-related fields
    /// 2. Other fields like stageName, stageOrder, estimatedDays, visibleInPortal, etc. 
    ///    are dynamically loaded from the Stage entity during queries
    /// 3. This reduces data duplication and ensures consistency
    /// 
    /// Note: We don't actually modify the JSON structure in database since stagesProgressJson 
    /// is stored as JSON text. The changes are handled at the application layer.
    /// </summary>
    public class SimplifyStagesProgressStructure_20250103000001
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Starting SimplifyStagesProgressStructure migration...");

                // Since stagesProgressJson is stored as JSON text field, we don't need to modify
                // the database schema. The simplification happens at the application layer:
                // 
                // 1. When saving stagesProgress, only essential fields are included in JSON
                // 2. When loading stagesProgress, missing fields are populated from Stage entity
                // 3. New method EnrichStagesProgressWithStageData() handles the population
                
                // Add a comment to indicate the migration ran successfully
                Console.WriteLine("StagesProgress structure simplification completed successfully.");
                Console.WriteLine("Changes:");
                Console.WriteLine("- StagesProgress now only stores: stageId, status, isCompleted, startTime, completionTime, completedById, completedBy, notes, isCurrent");
                Console.WriteLine("- Dynamic fields from Stage entity: stageName, stageOrder, estimatedDays, visibleInPortal, attachmentManagementNeeded, componentsJson, components");
                Console.WriteLine("- Legacy fields preserved for backward compatibility: lastUpdatedTime, lastUpdatedBy, rejection*, termination*");

                // Log completion
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                Console.WriteLine($"Migration SimplifyStagesProgressStructure_20250103000001 completed at {timestamp}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SimplifyStagesProgressStructure migration: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Rolling back SimplifyStagesProgressStructure migration...");
                
                // Since this migration only affects application logic and not database schema,
                // rollback is handled by reverting the application code changes.
                // No database schema changes to revert.
                
                Console.WriteLine("SimplifyStagesProgressStructure migration rollback completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rolling back SimplifyStagesProgressStructure migration: {ex.Message}");
                throw;
            }
        }
    }
} 