using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Increase OperationTitle column length from 200 to 500 characters
    /// to avoid truncation of long checklist/task names
    /// Migration: 20251021100000_IncreaseOperationTitleLength
    /// Date: 2025-10-21
    /// 
    /// This migration increases the operation_title column length to support longer names:
    /// - Previous limit: VARCHAR(200) - caused truncation for long checklist/task names
    /// - New limit: VARCHAR(500) - allows full display of longer names
    /// 
    /// The migration will:
    /// 1. Alter the operation_title column to VARCHAR(500)
    /// 2. Preserve all existing data (no data loss)
    /// 
    /// Note: This is a safe operation that only increases the column size.
    /// Rollback is supported but may truncate data if any titles exceed 200 characters.
    /// </summary>
    public static class Migration_20251021100000_IncreaseOperationTitleLength
    {
        public static void Up(ISqlSugarClient db)
        {
            Console.WriteLine("Starting migration: Increase OperationTitle column length");

            // Increase operation_title column length from VARCHAR(200) to VARCHAR(500)
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_operation_change_log 
                ALTER COLUMN operation_title TYPE VARCHAR(500);
            ");

            Console.WriteLine("✓ Increased operation_title column length to 500 characters");
        }

        public static void Down(ISqlSugarClient db)
        {
            Console.WriteLine("Rolling back migration: Increase OperationTitle column length");

            // Revert operation_title column length back to VARCHAR(200)
            // Warning: This may truncate existing data if any titles exceed 200 characters
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_operation_change_log 
                ALTER COLUMN operation_title TYPE VARCHAR(200);
            ");

            Console.WriteLine("✓ Reverted operation_title column length to 200 characters");
            Console.WriteLine("⚠ Warning: Any titles exceeding 200 characters may have been truncated");
        }
    }
}

