using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add outbound_attachment_workflow_ids field to Integration table
    /// Migration: 20251125000002_AddOutboundAttachmentWorkflowIds
    /// Date: 2025-11-25
    /// 
    /// This migration adds a field to store workflow IDs for outbound attachment sharing.
    /// The field stores a JSON array of workflow IDs whose attachments can be shared 
    /// with the external system.
    /// </summary>
    public static class Migration_20251125000002_AddOutboundAttachmentWorkflowIds
    {
        public static void Up(ISqlSugarClient db)
        {
            Console.WriteLine("Starting migration: Add outbound_attachment_workflow_ids to Integration table");

            // Check if column already exists
            var columnExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_integration' 
                AND column_name = 'outbound_attachment_workflow_ids'
            ").Rows.Count > 0;

            if (!columnExists)
            {
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_integration 
                    ADD COLUMN outbound_attachment_workflow_ids TEXT;
                ");

                Console.WriteLine("Added outbound_attachment_workflow_ids column to ff_integration table");
            }
            else
            {
                Console.WriteLine("Column 'outbound_attachment_workflow_ids' already exists in ff_integration table, skipping");
            }

            Console.WriteLine("Migration completed: Add outbound_attachment_workflow_ids to Integration table");
        }

        public static void Down(ISqlSugarClient db)
        {
            Console.WriteLine("Starting rollback: Remove outbound_attachment_workflow_ids from Integration table");

            var columnExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_integration' 
                AND column_name = 'outbound_attachment_workflow_ids'
            ").Rows.Count > 0;

            if (columnExists)
            {
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_integration 
                    DROP COLUMN outbound_attachment_workflow_ids;
                ");

                Console.WriteLine("Removed outbound_attachment_workflow_ids column from ff_integration table");
            }
            else
            {
                Console.WriteLine("Column 'outbound_attachment_workflow_ids' does not exist in ff_integration table, skipping");
            }

            Console.WriteLine("Rollback completed: Remove outbound_attachment_workflow_ids from Integration table");
        }
    }
}

