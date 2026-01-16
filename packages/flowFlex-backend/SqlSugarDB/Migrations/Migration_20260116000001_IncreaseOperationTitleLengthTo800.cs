using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Increase OperationTitle column length from 500 to 800 characters
    /// to support displaying all action details in Stage Condition logs
    /// Migration: 20260116000001_IncreaseOperationTitleLengthTo800
    /// Date: 2026-01-16
    /// 
    /// This migration increases the operation_title column length to support longer titles:
    /// - Previous limit: VARCHAR(500) - not enough for Stage Condition with many actions
    /// - New limit: VARCHAR(800) - allows full display of all action details
    /// 
    /// The migration will:
    /// 1. Alter the operation_title column to VARCHAR(800)
    /// 2. Preserve all existing data (no data loss)
    /// 
    /// Note: This is a safe operation that only increases the column size.
    /// </summary>
    public static class Migration_20260116000001_IncreaseOperationTitleLengthTo800
    {
        public static void Up(ISqlSugarClient db)
        {
            Console.WriteLine("Starting migration: Increase OperationTitle column length to 800");

            // Increase operation_title column length to VARCHAR(800)
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_operation_change_log 
                ALTER COLUMN operation_title TYPE VARCHAR(800);
            ");

            Console.WriteLine("✓ Increased operation_title column length to 800 characters");
        }

        public static void Down(ISqlSugarClient db)
        {
            Console.WriteLine("Rolling back migration: Revert OperationTitle column length to 500");

            // Revert operation_title column length back to VARCHAR(500)
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_operation_change_log 
                ALTER COLUMN operation_title TYPE VARCHAR(500);
            ");

            Console.WriteLine("✓ Reverted operation_title column length to 500 characters");
        }
    }
}
