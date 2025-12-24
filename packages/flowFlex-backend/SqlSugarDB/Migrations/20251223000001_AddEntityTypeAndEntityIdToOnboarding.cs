using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Migration to add EntityType and EntityId fields to Onboarding table
    /// These fields store external system entity information for integration purposes
    /// Also renames lead_name column to case_name
    /// </summary>
    public class Migration_20251223000001_AddEntityTypeAndEntityIdToOnboarding
    {
        public static void Up(ISqlSugarClient db)
        {
            // Add entity_type column if not exists
            var addEntityTypeColumn = @"
                ALTER TABLE ff_onboarding 
                ADD COLUMN IF NOT EXISTS entity_type VARCHAR(100) NULL";
            db.Ado.ExecuteCommand(addEntityTypeColumn);

            // Add entity_id column if not exists
            var addEntityIdColumn = @"
                ALTER TABLE ff_onboarding 
                ADD COLUMN IF NOT EXISTS entity_id VARCHAR(100) NULL";
            db.Ado.ExecuteCommand(addEntityIdColumn);

            // Create indexes for better query performance
            var createIndex1 = "CREATE INDEX IF NOT EXISTS idx_ff_onboarding_entity_type ON ff_onboarding(entity_type)";
            db.Ado.ExecuteCommand(createIndex1);

            var createIndex2 = "CREATE INDEX IF NOT EXISTS idx_ff_onboarding_entity_id ON ff_onboarding(entity_id)";
            db.Ado.ExecuteCommand(createIndex2);

            var createIndex3 = "CREATE INDEX IF NOT EXISTS idx_ff_onboarding_entity_type_id ON ff_onboarding(entity_type, entity_id)";
            db.Ado.ExecuteCommand(createIndex3);

            // Rename lead_name column to case_name (if lead_name exists)
            var renameLeadNameColumn = @"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'ff_onboarding' AND column_name = 'lead_name'
                    ) THEN
                        ALTER TABLE ff_onboarding RENAME COLUMN lead_name TO case_name;
                    END IF;
                END $$";
            db.Ado.ExecuteCommand(renameLeadNameColumn);
        }

        public static void Down(ISqlSugarClient db)
        {
            // Drop indexes first
            db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_ff_onboarding_entity_type");
            db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_ff_onboarding_entity_id");
            db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_ff_onboarding_entity_type_id");

            // Drop columns
            db.Ado.ExecuteCommand("ALTER TABLE ff_onboarding DROP COLUMN IF EXISTS entity_type");
            db.Ado.ExecuteCommand("ALTER TABLE ff_onboarding DROP COLUMN IF EXISTS entity_id");

            // Rename case_name column back to lead_name (if case_name exists)
            var renameBackColumn = @"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'ff_onboarding' AND column_name = 'case_name'
                    ) THEN
                        ALTER TABLE ff_onboarding RENAME COLUMN case_name TO lead_name;
                    END IF;
                END $$";
            db.Ado.ExecuteCommand(renameBackColumn);
        }
    }
}
