using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations;

/// <summary>
/// Migration to add document signing fields to ff_onboarding_file table.
/// Adds is_signed, source_file_id, file_hash, signer_name, sign_time columns.
/// </summary>
public static class Migration_20260810000002_AddSigningFieldsToOnboardingFile
{
    public static void Up(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            ALTER TABLE ff_onboarding_file ADD COLUMN IF NOT EXISTS is_signed bool NOT NULL DEFAULT false;
            ALTER TABLE ff_onboarding_file ADD COLUMN IF NOT EXISTS source_file_id bigint;
            ALTER TABLE ff_onboarding_file ADD COLUMN IF NOT EXISTS file_hash varchar(64);
            ALTER TABLE ff_onboarding_file ADD COLUMN IF NOT EXISTS signer_name varchar(200);
            ALTER TABLE ff_onboarding_file ADD COLUMN IF NOT EXISTS sign_time timestamptz;
        ");

        Console.WriteLine("[Migration] Added signing fields to ff_onboarding_file table");
    }

    public static void Down(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            ALTER TABLE ff_onboarding_file DROP COLUMN IF EXISTS is_signed;
            ALTER TABLE ff_onboarding_file DROP COLUMN IF EXISTS source_file_id;
            ALTER TABLE ff_onboarding_file DROP COLUMN IF EXISTS file_hash;
            ALTER TABLE ff_onboarding_file DROP COLUMN IF EXISTS signer_name;
            ALTER TABLE ff_onboarding_file DROP COLUMN IF EXISTS sign_time;
        ");

        Console.WriteLine("[Migration] Dropped signing fields from ff_onboarding_file table");
    }
}
