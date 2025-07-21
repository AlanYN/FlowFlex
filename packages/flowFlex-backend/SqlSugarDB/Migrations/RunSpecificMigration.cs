using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Manual migration runner for specific migrations
    /// </summary>
    public class RunSpecificMigration
    {
        /// <summary>
        /// Run the encrypted access token migration specifically
        /// </summary>
        public static void RunEncryptedAccessTokenMigration(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("[RunSpecificMigration] Starting encrypted access token migration...");
                
                // Check if the column already exists
                var columnExists = CheckColumnExists(db, "ff_user_invitations", "encrypted_access_token");
                
                if (columnExists)
                {
                    Console.WriteLine("✓ Column encrypted_access_token already exists, skipping migration");
                    return;
                }
                
                // Run the migration
                AddEncryptedAccessTokenField_20250101000012.Up(db);
                
                // Mark migration as completed in history
                MarkMigrationAsCompleted(db, "20250101000012_AddEncryptedAccessTokenField");
                
                Console.WriteLine("✓ Encrypted access token migration completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error running encrypted access token migration: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
        
        /// <summary>
        /// Check if a column exists in a table
        /// </summary>
        private static bool CheckColumnExists(ISqlSugarClient db, string tableName, string columnName)
        {
            try
            {
                var sql = @"
                    SELECT COUNT(*) 
                    FROM information_schema.columns 
                    WHERE table_name = @tableName 
                    AND column_name = @columnName";
                
                var count = db.Ado.GetInt(sql, new { tableName, columnName });
                return count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not check if column exists: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Mark a migration as completed in the migration history
        /// </summary>
        private static void MarkMigrationAsCompleted(ISqlSugarClient db, string migrationName)
        {
            try
            {
                // Create migration history table if it doesn't exist
                db.Ado.ExecuteCommand(@"
                    CREATE TABLE IF NOT EXISTS migration_history (
                        id SERIAL PRIMARY KEY,
                        migration_name VARCHAR(255) NOT NULL UNIQUE,
                        applied_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        success BOOLEAN DEFAULT TRUE
                    );
                ");
                
                // Insert migration record
                var sql = @"
                    INSERT INTO migration_history (migration_name, applied_at, success) 
                    VALUES (@migrationName, @appliedAt, @success)
                    ON CONFLICT (migration_name) 
                    DO UPDATE SET applied_at = @appliedAt, success = @success";
                
                db.Ado.ExecuteCommand(sql, new
                {
                    migrationName,
                    appliedAt = DateTime.UtcNow,
                    success = true
                });
                
                Console.WriteLine($"✓ Marked migration {migrationName} as completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not mark migration as completed: {ex.Message}");
            }
        }
    }
} 