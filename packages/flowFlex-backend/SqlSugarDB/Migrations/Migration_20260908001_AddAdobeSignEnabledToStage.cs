using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add adobe_sign_enabled column to ff_stage table (OW-731)
    /// </summary>
    public static class Migration_20260908001_AddAdobeSignEnabledToStage
    {
        public static void Up(ISqlSugarClient db)
        {
            var sql = @"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'ff_stage'
                        AND column_name = 'adobe_sign_enabled'
                    ) THEN
                        ALTER TABLE ff_stage
                        ADD COLUMN adobe_sign_enabled BOOLEAN NOT NULL DEFAULT FALSE;

                        COMMENT ON COLUMN ff_stage.adobe_sign_enabled IS 'Enables legally binding signatures via Adobe Sign for PDF files in this stage (OW-731)';
                    END IF;
                END $$;
            ";

            db.Ado.ExecuteCommand(sql);
        }

        public static void Down(ISqlSugarClient db)
        {
            var sql = @"
                ALTER TABLE ff_stage DROP COLUMN IF EXISTS adobe_sign_enabled;
            ";

            db.Ado.ExecuteCommand(sql);
        }
    }
}
