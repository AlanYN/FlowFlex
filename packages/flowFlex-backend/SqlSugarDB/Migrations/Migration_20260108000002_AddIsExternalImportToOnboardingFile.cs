using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add is_external_import and source columns to ff_onboarding_file table
    /// </summary>
    public static class Migration_20260108000002_AddIsExternalImportToOnboardingFile
    {
        public static void Up(ISqlSugarClient db)
        {
            var sql = @"
                DO $$
                BEGIN
                    -- Add is_external_import column if not exists
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'ff_onboarding_file' 
                        AND column_name = 'is_external_import'
                    ) THEN
                        ALTER TABLE ff_onboarding_file 
                        ADD COLUMN is_external_import BOOLEAN NOT NULL DEFAULT FALSE;
                        
                        COMMENT ON COLUMN ff_onboarding_file.is_external_import IS 'Indicates if the file was imported from external system (e.g., CRM)';
                    END IF;

                    -- Add source column if not exists
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'ff_onboarding_file' 
                        AND column_name = 'source'
                    ) THEN
                        ALTER TABLE ff_onboarding_file 
                        ADD COLUMN source VARCHAR(100);
                        
                        COMMENT ON COLUMN ff_onboarding_file.source IS 'Source of the file (e.g., CRM, Portal, Manual)';
                    END IF;
                END $$;
            ";

            db.Ado.ExecuteCommand(sql);
        }

        public static void Down(ISqlSugarClient db)
        {
            var sql = @"
                ALTER TABLE ff_onboarding_file DROP COLUMN IF EXISTS is_external_import;
                ALTER TABLE ff_onboarding_file DROP COLUMN IF EXISTS source;
            ";

            db.Ado.ExecuteCommand(sql);
        }
    }
}
