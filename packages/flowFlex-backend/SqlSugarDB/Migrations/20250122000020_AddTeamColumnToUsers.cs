using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add Team column to ff_users table
    /// </summary>
    public class Migration_20250122000020_AddTeamColumnToUsers
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("[AddTeamColumnToUsers] Starting migration to add Team column...");

                // Check if Team column already exists
                var columnExists = db.Ado.GetDataTable(@"
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'ff_users' AND column_name = 'team'
                ").Rows.Count > 0;

                if (!columnExists)
                {
                    Console.WriteLine("[AddTeamColumnToUsers] Adding Team column to ff_users table...");
                    
                    // Add Team column
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_users 
                        ADD COLUMN team VARCHAR(100) NULL;
                    ");

                    Console.WriteLine("[AddTeamColumnToUsers] Team column added successfully");
                }
                else
                {
                    Console.WriteLine("[AddTeamColumnToUsers] Team column already exists, skipping...");
                }

                // Create index on team column for better query performance
                try
                {
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_users_team 
                        ON ff_users(team) 
                        WHERE team IS NOT NULL;
                    ");
                    Console.WriteLine("[AddTeamColumnToUsers] Index on team column created successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AddTeamColumnToUsers] Warning: Could not create index on team column: {ex.Message}");
                }

                Console.WriteLine("[AddTeamColumnToUsers] Migration completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddTeamColumnToUsers] Migration failed: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("[AddTeamColumnToUsers] Rolling back Team column migration...");

                // Drop index first
                try
                {
                    db.Ado.ExecuteCommand(@"DROP INDEX IF EXISTS idx_users_team;");
                    Console.WriteLine("[AddTeamColumnToUsers] Index dropped successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AddTeamColumnToUsers] Warning: Could not drop index: {ex.Message}");
                }

                // Check if Team column exists
                var columnExists = db.Ado.GetDataTable(@"
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'ff_users' AND column_name = 'team'
                ").Rows.Count > 0;

                if (columnExists)
                {
                    // Drop Team column
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_users 
                        DROP COLUMN team;
                    ");
                    Console.WriteLine("[AddTeamColumnToUsers] Team column dropped successfully");
                }

                Console.WriteLine("[AddTeamColumnToUsers] Rollback completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddTeamColumnToUsers] Rollback failed: {ex.Message}");
                throw;
            }
        }
    }
}