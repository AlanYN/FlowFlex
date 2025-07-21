using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add app_code field to ff_events table
    /// </summary>
    public class AddAppCodeToEvents_20250101000014
    {
        public static void Up(ISqlSugarClient db)
        {
            // Check if app_code column already exists
            var columnExists = false;
            try
            {
                var checkSql = @"
                    SELECT COUNT(*) 
                    FROM information_schema.columns 
                    WHERE table_name = 'ff_events' 
                    AND column_name = 'app_code'";
                var result = db.Ado.GetScalar(checkSql);
                columnExists = Convert.ToInt32(result) > 0;
            }
            catch
            {
                // If check fails, assume column doesn't exist
            }

            if (!columnExists)
            {
                // Add app_code column to ff_events table
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_events 
                    ADD COLUMN app_code VARCHAR(50) DEFAULT 'DEFAULT';
                ");

                // Update existing records to have default value
                db.Ado.ExecuteCommand(@"
                    UPDATE ff_events 
                    SET app_code = 'DEFAULT' 
                    WHERE app_code IS NULL;
                ");
            }

            // Create index for better performance (safe to run multiple times)
            db.Ado.ExecuteCommand(@"
                CREATE INDEX IF NOT EXISTS idx_events_app_code 
                ON ff_events(app_code);
            ");
        }

        public static void Down(ISqlSugarClient db)
        {
            // Drop index first
            db.Ado.ExecuteCommand(@"
                DROP INDEX IF EXISTS idx_events_app_code;
            ");

            // Remove app_code column
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_events 
                DROP COLUMN IF EXISTS app_code;
            ");
        }
    }
} 