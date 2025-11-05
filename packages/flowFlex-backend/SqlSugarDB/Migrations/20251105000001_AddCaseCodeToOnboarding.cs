using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add CaseCode field to onboarding (case) table
    /// Adds: case_code - Unique identifier generated from Lead Name
    /// </summary>
    public class Migration_20251105000001_AddCaseCodeToOnboarding
    {
        public static void Up(ISqlSugarClient db)
        {
            // Add case_code column
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_onboarding 
                ADD COLUMN IF NOT EXISTS case_code VARCHAR(50);
            ");

            // Add comment to case_code column
            db.Ado.ExecuteCommand(@"
                COMMENT ON COLUMN ff_onboarding.case_code IS 'Unique case code generated from Lead Name. Format: PREFIX + Sequential Number (e.g., TPLINK0001, CHRRES0001). Cannot be modified after creation.';
            ");

            // Create index for better query performance
            db.Ado.ExecuteCommand(@"
                CREATE INDEX IF NOT EXISTS idx_onboarding_case_code 
                ON ff_onboarding(case_code);
            ");

            // Create unique index to ensure case_code uniqueness
            db.Ado.ExecuteCommand(@"
                CREATE UNIQUE INDEX IF NOT EXISTS idx_onboarding_case_code_unique 
                ON ff_onboarding(case_code) 
                WHERE case_code IS NOT NULL AND case_code != '';
            ");
        }

        public static void Down(ISqlSugarClient db)
        {
            // Drop unique index
            db.Ado.ExecuteCommand(@"
                DROP INDEX IF EXISTS idx_onboarding_case_code_unique;
            ");

            // Drop index
            db.Ado.ExecuteCommand(@"
                DROP INDEX IF EXISTS idx_onboarding_case_code;
            ");

            // Drop column from ff_onboarding table
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_onboarding 
                DROP COLUMN IF EXISTS case_code;
            ");
        }
    }
}

