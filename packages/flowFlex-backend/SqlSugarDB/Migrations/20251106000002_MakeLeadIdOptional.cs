using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Make LeadId optional since Case Code is now the primary identifier
    /// </summary>
    public class Migration_20251106000002_MakeLeadIdOptional
    {
        public static void Up(ISqlSugarClient db)
        {
            // Remove NOT NULL constraint from lead_id column
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_onboarding 
                ALTER COLUMN lead_id DROP NOT NULL;
            ");

            // Update column comment to reflect that LeadId is now optional
            db.Ado.ExecuteCommand(@"
                COMMENT ON COLUMN ff_onboarding.lead_id IS 'Customer/Lead ID (optional). Case Code is the primary identifier.';
            ");
        }

        public static void Down(ISqlSugarClient db)
        {
            // Note: Cannot safely restore NOT NULL constraint without checking existing data
            // Only update the comment back
            db.Ado.ExecuteCommand(@"
                COMMENT ON COLUMN ff_onboarding.lead_id IS 'Customer/Lead ID';
            ");

            // Restore NOT NULL only if all existing records have lead_id
            db.Ado.ExecuteCommand(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM ff_onboarding WHERE lead_id IS NULL
                    ) THEN
                        ALTER TABLE ff_onboarding 
                        ALTER COLUMN lead_id SET NOT NULL;
                    END IF;
                END $$;
            ");
        }
    }
}


