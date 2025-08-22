using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Drop unused table ff_questionnaire_section
    /// </summary>
    public static class DropQuestionnaireSectionTable_20250801000002
    {
        public static void Up(ISqlSugarClient db)
        {
            // Safely drop table if it exists
            db.Ado.ExecuteCommand(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables 
                        WHERE table_schema = 'public' AND table_name = 'ff_questionnaire_section'
                    ) THEN
                        -- Drop dependent objects if needed then table
                        DROP TABLE IF EXISTS ff_questionnaire_section CASCADE;
                    END IF;
                END $$;
            ");
        }

        public static void Down(ISqlSugarClient db)
        {
            // No-op: we do not recreate the removed table
        }
    }
}
