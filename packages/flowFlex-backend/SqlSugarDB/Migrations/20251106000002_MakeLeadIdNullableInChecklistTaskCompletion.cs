using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations;

/// <summary>
/// Make lead_id nullable in ff_checklist_task_completion table
/// Since Case Code is now the primary identifier for Onboarding, LeadId should be optional
/// 
/// 此迁移将 ff_checklist_task_completion 表的 lead_id 字段改为可空
/// 
/// 原因：
/// - Case Code 现在是 Onboarding 的主要标识符
/// - LeadId 应该是可选的，以支持没有 Lead 的 Onboarding 记录
/// - 解决插入任务完成记录时 lead_id 为空导致的 NOT NULL 约束错误
/// </summary>
public class Migration_20251106000002_MakeLeadIdNullableInChecklistTaskCompletion
{
    public static void Up(ISqlSugarClient db)
    {
        try
        {
            Console.WriteLine("Starting MakeLeadIdNullableInChecklistTaskCompletion migration...");

            // Make lead_id column nullable
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_checklist_task_completion 
                ALTER COLUMN lead_id DROP NOT NULL;
            ");

            Console.WriteLine("Successfully made lead_id nullable in ff_checklist_task_completion");

            // Update existing records with empty lead_id to NULL for data consistency
            db.Ado.ExecuteCommand(@"
                UPDATE ff_checklist_task_completion 
                SET lead_id = NULL 
                WHERE lead_id = '';
            ");

            Console.WriteLine("Updated empty lead_id values to NULL");
            Console.WriteLine("MakeLeadIdNullableInChecklistTaskCompletion migration completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in MakeLeadIdNullableInChecklistTaskCompletion migration: {ex.Message}");
            throw;
        }
    }

    public static void Down(ISqlSugarClient db)
    {
        try
        {
            Console.WriteLine("Rolling back MakeLeadIdNullableInChecklistTaskCompletion migration...");

            // Before making lead_id NOT NULL again, we need to set a default value for NULL records
            // Use a placeholder value for records without lead_id
            db.Ado.ExecuteCommand(@"
                UPDATE ff_checklist_task_completion 
                SET lead_id = 'UNKNOWN_LEAD_' || id::text 
                WHERE lead_id IS NULL OR lead_id = '';
            ");

            Console.WriteLine("Set default values for NULL lead_id records");

            // Make lead_id column NOT NULL again
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_checklist_task_completion 
                ALTER COLUMN lead_id SET NOT NULL;
            ");

            Console.WriteLine("Successfully made lead_id NOT NULL again");
            Console.WriteLine("MakeLeadIdNullableInChecklistTaskCompletion rollback completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error rolling back MakeLeadIdNullableInChecklistTaskCompletion migration: {ex.Message}");
            throw;
        }
    }
}

