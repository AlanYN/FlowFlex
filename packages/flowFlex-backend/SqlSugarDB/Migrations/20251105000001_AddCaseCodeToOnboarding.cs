using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add CaseCode field to onboarding (case) table
    /// Adds: case_code - Unique identifier with format C00001, C00002, ..., C99999, C100000, ...
    /// Case Code is unique within each tenant and app combination (tenant isolation)
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
                COMMENT ON COLUMN ff_onboarding.case_code IS 'Unique case code with fixed prefix C and auto-increment number. Format: C00001, C00002, ..., C99999, C100000, ... Unique within tenant and app. Cannot be modified after creation.';
            ");

            // Create index for better query performance
            db.Ado.ExecuteCommand(@"
                CREATE INDEX IF NOT EXISTS idx_onboarding_case_code 
                ON ff_onboarding(case_code);
            ");

            // Create unique index to ensure case_code uniqueness within tenant and app
            // Case Code is unique per tenant and app combination
            db.Ado.ExecuteCommand(@"
                CREATE UNIQUE INDEX IF NOT EXISTS idx_onboarding_tenant_app_case_code_unique 
                ON ff_onboarding(tenant_id, app_code, case_code) 
                WHERE case_code IS NOT NULL AND case_code != '';
            ");
        }

        public static void Down(ISqlSugarClient db)
        {
            // Drop unique index
            db.Ado.ExecuteCommand(@"
                DROP INDEX IF EXISTS idx_onboarding_tenant_app_case_code_unique;
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

