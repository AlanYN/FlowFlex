using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Reflection;

namespace FlowFlex.Infrastructure.Services
{
    /// <summary>
    /// Database migration service for automatic migration execution
    /// </summary>
    public class DatabaseMigrationService
    {
        private readonly ISqlSugarClient _db;
        private readonly ILogger<DatabaseMigrationService> _logger;

        public DatabaseMigrationService(ISqlSugarClient db, ILogger<DatabaseMigrationService> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Execute all pending migrations
        /// </summary>
        public async Task ExecuteMigrationsAsync()
        {
            try
            {
                _logger.LogInformation("Starting database migration check...");

                // Create migration history table if it doesn't exist
                await EnsureMigrationHistoryTableAsync();

                // Get all migration scripts
                var migrations = GetMigrationScripts();

                // Execute pending migrations
                foreach (var migration in migrations)
                {
                    if (!await IsMigrationExecutedAsync(migration.Name))
                    {
                        await ExecuteMigrationAsync(migration);
                    }
                    else
                    {
                        _logger.LogDebug("Migration {MigrationName} already executed, skipping", migration.Name);
                    }
                }

                _logger.LogInformation("Database migration check completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database migration");
                throw;
            }
        }

        /// <summary>
        /// Create migration history table if it doesn't exist
        /// </summary>
        private async Task EnsureMigrationHistoryTableAsync()
        {
            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS ff_migration_history (
                    id SERIAL PRIMARY KEY,
                    migration_name VARCHAR(255) NOT NULL UNIQUE,
                    executed_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    execution_time_ms BIGINT NOT NULL,
                    success BOOLEAN NOT NULL DEFAULT TRUE,
                    error_message TEXT,
                    created_by VARCHAR(50) DEFAULT 'SYSTEM'
                );
                
                CREATE INDEX IF NOT EXISTS idx_ff_migration_history_name ON ff_migration_history(migration_name);
                CREATE INDEX IF NOT EXISTS idx_ff_migration_history_executed_at ON ff_migration_history(executed_at);
            ";

            await _db.Ado.ExecuteCommandAsync(createTableSql);
            _logger.LogDebug("Migration history table ensured");
        }

        /// <summary>
        /// Get all migration scripts from embedded resources or file system
        /// </summary>
        private List<MigrationScript> GetMigrationScripts()
        {
            var migrations = new List<MigrationScript>();

            // First try to get from embedded resources in all loaded assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && a.GetName().Name?.Contains("FlowFlex") == true)
                .ToList();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var resourceNames = assembly.GetManifestResourceNames()
                        .Where(name => name.Contains("Migrations") && name.EndsWith(".sql"))
                        .OrderBy(name => name)
                        .ToList();

                    foreach (var resourceName in resourceNames)
                    {
                        using var stream = assembly.GetManifestResourceStream(resourceName);
                        if (stream != null)
                        {
                            using var reader = new StreamReader(stream);
                            var content = reader.ReadToEnd();

                            var migrationName = Path.GetFileNameWithoutExtension(resourceName.Split('.').Last());
                            migrations.Add(new MigrationScript
                            {
                                Name = migrationName,
                                Content = content
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load migration resources from assembly: {AssemblyName}", assembly.GetName().Name);
                }
            }

            // If no embedded resources found, try to read from file system
            if (!migrations.Any())
            {
                migrations = GetMigrationScriptsFromFileSystem();
            }

            return migrations.OrderBy(m => m.Name).ToList();
        }

        /// <summary>
        /// Get migration scripts from file system
        /// </summary>
        private List<MigrationScript> GetMigrationScriptsFromFileSystem()
        {
            var migrations = new List<MigrationScript>();
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var migrationPath = Path.Combine(baseDirectory, "SqlSugarDB", "Migrations");

            if (!Directory.Exists(migrationPath))
            {
                // Try alternative path
                migrationPath = Path.Combine(Directory.GetCurrentDirectory(), "SqlSugarDB", "Migrations");
            }

            if (!Directory.Exists(migrationPath))
            {
                // Try relative path from current directory
                var currentDir = Directory.GetCurrentDirectory();
                var possiblePaths = new[]
                {
                    Path.Combine(currentDir, "packages", "flowFlex-backend", "SqlSugarDB", "Migrations"),
                    Path.Combine(currentDir, "..", "SqlSugarDB", "Migrations"),
                    Path.Combine(currentDir, "SqlSugarDB", "Migrations")
                };

                foreach (var path in possiblePaths)
                {
                    if (Directory.Exists(path))
                    {
                        migrationPath = path;
                        break;
                    }
                }
            }

            if (Directory.Exists(migrationPath))
            {
                var sqlFiles = Directory.GetFiles(migrationPath, "*.sql")
                    .OrderBy(f => f)
                    .ToList();

                foreach (var file in sqlFiles)
                {
                    var migrationName = Path.GetFileNameWithoutExtension(file);
                    var content = File.ReadAllText(file);

                    migrations.Add(new MigrationScript
                    {
                        Name = migrationName,
                        Content = content
                    });
                }

                _logger.LogInformation("Found {Count} migration scripts in {Path}", migrations.Count, migrationPath);
            }
            else
            {
                _logger.LogWarning("Migration directory not found: {MigrationPath}", migrationPath);

                // Fallback: return hardcoded migrations
                migrations = GetHardcodedMigrations();
            }

            return migrations;
        }

        /// <summary>
        /// Get hardcoded migration scripts as fallback
        /// </summary>
        private List<MigrationScript> GetHardcodedMigrations()
        {
            var migrations = new List<MigrationScript>();

            // Add the app_code column migration (safe version)
            migrations.Add(new MigrationScript
            {
                Name = "20241219_AddAppCodeColumn",
                Content = @"
-- =============================================
-- FlowFlex Database Migration Script
-- Version: 20241219_AddAppCodeColumn_Safe
-- Created: 2024-12-19
-- Description: Add app_code column to all existing tables for application isolation
-- =============================================

-- Start transaction
BEGIN;

-- Function to safely add app_code column to a table
CREATE OR REPLACE FUNCTION add_app_code_column_safe(table_name text) RETURNS void AS $$
BEGIN
    -- Check if table exists
    IF EXISTS (SELECT 1 FROM information_schema.tables t WHERE t.table_name = add_app_code_column_safe.table_name AND t.table_schema = 'public') THEN
        -- Check if app_code column already exists
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns c WHERE c.table_name = add_app_code_column_safe.table_name AND c.column_name = 'app_code' AND c.table_schema = 'public') THEN
            EXECUTE format('ALTER TABLE %I ADD COLUMN app_code VARCHAR(32) NOT NULL DEFAULT ''DEFAULT''', table_name);
            RAISE NOTICE 'Added app_code column to table %', table_name;
        ELSE
            RAISE NOTICE 'Column app_code already exists in table %', table_name;
        END IF;
    ELSE
        RAISE NOTICE 'Table % does not exist, skipping...', table_name;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- Function to safely create index
CREATE OR REPLACE FUNCTION create_app_code_index_safe(table_name text) RETURNS void AS $$
DECLARE
    index_name text;
BEGIN
    -- Check if table exists
    IF EXISTS (SELECT 1 FROM information_schema.tables t WHERE t.table_name = create_app_code_index_safe.table_name AND t.table_schema = 'public') THEN
        -- Check if app_code column exists
        IF EXISTS (SELECT 1 FROM information_schema.columns c WHERE c.table_name = create_app_code_index_safe.table_name AND c.column_name = 'app_code' AND c.table_schema = 'public') THEN
            -- Check if tenant_id column exists
            IF EXISTS (SELECT 1 FROM information_schema.columns c WHERE c.table_name = create_app_code_index_safe.table_name AND c.column_name = 'tenant_id' AND c.table_schema = 'public') THEN
                index_name := format('idx_%s_app_code_tenant_id', table_name);
                EXECUTE format('CREATE INDEX IF NOT EXISTS %I ON %I(app_code, tenant_id)', index_name, table_name);
                RAISE NOTICE 'Created index % on table %', index_name, table_name;
            ELSE
                RAISE NOTICE 'Column tenant_id does not exist in table %, skipping index creation', table_name;
            END IF;
        ELSE
            RAISE NOTICE 'Column app_code does not exist in table %, skipping index creation', table_name;
        END IF;
    ELSE
        RAISE NOTICE 'Table % does not exist, skipping index creation...', table_name;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- Function to safely add column comment
CREATE OR REPLACE FUNCTION add_app_code_comment_safe(table_name text) RETURNS void AS $$
BEGIN
    -- Check if table exists and has app_code column
    IF EXISTS (SELECT 1 FROM information_schema.tables t WHERE t.table_name = add_app_code_comment_safe.table_name AND t.table_schema = 'public') AND
       EXISTS (SELECT 1 FROM information_schema.columns c WHERE c.table_name = add_app_code_comment_safe.table_name AND c.column_name = 'app_code' AND c.table_schema = 'public') THEN
        EXECUTE format('COMMENT ON COLUMN %I.app_code IS ''Application code for application isolation''', table_name);
        RAISE NOTICE 'Added comment to app_code column in table %', table_name;
    ELSE
        RAISE NOTICE 'Table % or app_code column does not exist, skipping comment...', table_name;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- Apply changes to all tables
DO $$
DECLARE
    table_names text[] := ARRAY[
        'ff_user_role',
        'ff_users',
        'ff_workflow',
        'ff_stage',
        'ff_checklist',
        'ff_checklist_task',
        'ff_checklist_task_completion',
        'ff_internal_notes',
        'ff_onboarding_file',
        'ff_operation_change_log',
        'ff_static_field_values',
        'ff_stage_completion_log'
    ];
    table_name text;
BEGIN
    -- Add app_code column to all tables
    FOREACH table_name IN ARRAY table_names LOOP
        PERFORM add_app_code_column_safe(table_name);
    END LOOP;
    
    -- Create indexes for all tables
    FOREACH table_name IN ARRAY table_names LOOP
        PERFORM create_app_code_index_safe(table_name);
    END LOOP;
    
    -- Add comments for all tables
    FOREACH table_name IN ARRAY table_names LOOP
        PERFORM add_app_code_comment_safe(table_name);
    END LOOP;
END;
$$;

-- Clean up temporary functions
DROP FUNCTION IF EXISTS add_app_code_column_safe(text);
DROP FUNCTION IF EXISTS create_app_code_index_safe(text);
DROP FUNCTION IF EXISTS add_app_code_comment_safe(text);

-- Commit transaction
COMMIT;

-- Log migration completion
SELECT 'Migration 20241219_AddAppCodeColumn completed successfully' AS status;
"
            });

            _logger.LogInformation("Using hardcoded migrations as fallback");
            return migrations;
        }

        /// <summary>
        /// Check if migration has been executed
        /// </summary>
        private async Task<bool> IsMigrationExecutedAsync(string migrationName)
        {
            var sql = "SELECT COUNT(*) FROM ff_migration_history WHERE migration_name = @migrationName AND success = true";
            var count = await _db.Ado.GetIntAsync(sql, new { migrationName });
            return count > 0;
        }

        /// <summary>
        /// Execute a single migration
        /// </summary>
        private async Task ExecuteMigrationAsync(MigrationScript migration)
        {
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Executing migration: {MigrationName}", migration.Name);

                // Execute the migration script
                await _db.Ado.ExecuteCommandAsync(migration.Content);

                stopwatch.Stop();

                // Record successful execution
                await RecordMigrationExecutionAsync(migration.Name, stopwatch.ElapsedMilliseconds, true, null);

                _logger.LogInformation("Migration {MigrationName} executed successfully in {ElapsedMs}ms",
                    migration.Name, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Record failed execution
                await RecordMigrationExecutionAsync(migration.Name, stopwatch.ElapsedMilliseconds, false, ex.Message);

                _logger.LogError(ex, "Migration {MigrationName} failed after {ElapsedMs}ms",
                    migration.Name, stopwatch.ElapsedMilliseconds);

                throw;
            }
        }

        /// <summary>
        /// Record migration execution in history table
        /// </summary>
        private async Task RecordMigrationExecutionAsync(string migrationName, long executionTimeMs, bool success, string errorMessage)
        {
            var sql = @"
                INSERT INTO ff_migration_history (migration_name, executed_at, execution_time_ms, success, error_message)
                VALUES (@migrationName, @executedAt, @executionTimeMs, @success, @errorMessage)
                ON CONFLICT (migration_name) 
                DO UPDATE SET 
                    executed_at = EXCLUDED.executed_at,
                    execution_time_ms = EXCLUDED.execution_time_ms,
                    success = EXCLUDED.success,
                    error_message = EXCLUDED.error_message";

            await _db.Ado.ExecuteCommandAsync(sql, new
            {
                migrationName,
                executedAt = DateTime.UtcNow,
                executionTimeMs,
                success,
                errorMessage
            });
        }

        /// <summary>
        /// Get migration history
        /// </summary>
        public async Task<List<MigrationHistoryRecord>> GetMigrationHistoryAsync()
        {
            var sql = @"
                SELECT migration_name, executed_at, execution_time_ms, success, error_message
                FROM ff_migration_history
                ORDER BY executed_at DESC";

            var result = await _db.Ado.SqlQueryAsync<MigrationHistoryRecord>(sql);
            return result.ToList();
        }
    }

    /// <summary>
    /// Migration script model
    /// </summary>
    public class MigrationScript
    {
        public string Name { get; set; }
        public string Content { get; set; }
    }

    /// <summary>
    /// Migration history record model
    /// </summary>
    public class MigrationHistoryRecord
    {
        public string MigrationName { get; set; }
        public DateTime ExecutedAt { get; set; }
        public long ExecutionTimeMs { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}