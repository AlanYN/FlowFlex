using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    public static class Migration_20251028000001_RemoveOnboardingCheckConstraints
    {
        public static void Up(ISqlSugarClient db)
        {
            Console.WriteLine("[Migration] Removing chk_onboarding_* check constraints from ff_onboarding...");

            // Drop all check constraints on ff_onboarding that start with 'chk_onboarding_'
            var sql = @"
DO $$
DECLARE
    r RECORD;
BEGIN
    FOR r IN 
        SELECT conname 
        FROM pg_constraint c
        JOIN pg_class t ON c.conrelid = t.oid
        JOIN pg_namespace n ON t.relnamespace = n.oid
        WHERE t.relname = 'ff_onboarding'
          AND n.nspname = 'public'
          AND c.conname LIKE 'chk_onboarding_%'
    LOOP
        EXECUTE format('ALTER TABLE public.ff_onboarding DROP CONSTRAINT IF EXISTS %I', r.conname);
        RAISE NOTICE 'Dropped constraint: %', r.conname;
    END LOOP;
END $$;";

            db.Ado.ExecuteCommand(sql);
        }

        public static void Down(ISqlSugarClient db)
        {
            // Intentionally left empty: constraints are deprecated and not restored
        }
    }
}


