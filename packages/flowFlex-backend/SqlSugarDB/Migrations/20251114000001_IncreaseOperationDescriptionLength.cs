using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Increase OperationDescription column length from 500 to 4000 characters
    /// to support detailed change descriptions without truncation
    /// Migration: 20251114000001_IncreaseOperationDescriptionLength
    /// Date: 2025-11-14
    /// 
    /// This migration increases the operation_description column length to support detailed change logs:
    /// - Previous limit: VARCHAR(500) - caused truncation for detailed questionnaire change descriptions
    /// - New limit: VARCHAR(4000) - allows full display of detailed change descriptions including all questions and options
    /// 
    /// The migration will:
    /// 1. Alter the operation_description column to VARCHAR(4000)
    /// 2. Preserve all existing data (no data loss)
    /// 
    /// Note: This is a safe operation that only increases the column size.
    /// Rollback is supported but may truncate data if any descriptions exceed 500 characters.
    /// </summary>
    public static class Migration_20251114000001_IncreaseOperationDescriptionLength
    {
        public static void Up(ISqlSugarClient db)
        {
            Console.WriteLine("Starting migration: Increase OperationDescription column length");

            // Increase operation_description column length from VARCHAR(500) to VARCHAR(4000)
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_operation_change_log 
                ALTER COLUMN operation_description TYPE VARCHAR(4000);
            ");

            Console.WriteLine("✓ Increased operation_description column length to 4000 characters");
        }

        public static void Down(ISqlSugarClient db)
        {
            Console.WriteLine("Rolling back migration: Increase OperationDescription column length");

            // Revert operation_description column length back to VARCHAR(500)
            // Warning: This may truncate existing data if any descriptions exceed 500 characters
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_operation_change_log 
                ALTER COLUMN operation_description TYPE VARCHAR(500);
            ");

            Console.WriteLine("✓ Reverted operation_description column length to 500 characters");
            Console.WriteLine("⚠ Warning: Any descriptions exceeding 500 characters may have been truncated");
        }
    }
}


