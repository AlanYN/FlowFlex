using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Update Permission Constraints to use new split fields
    /// Migration: 20251021000002_UpdatePermissionConstraints
    /// Date: 2025-10-21
    /// 
    /// This migration updates the database constraints to use the new split permission subject type fields:
    /// - view_permission_subject_type (instead of permission_subject_type for view permissions)
    /// - operate_permission_subject_type (instead of permission_subject_type for operate permissions)
    /// 
    /// The migration will:
    /// 1. Drop old constraints that reference permission_subject_type
    /// 2. Create new constraints that reference view_permission_subject_type
    /// 3. Add constraints for operate_permission_subject_type
    /// </summary>
    public static class Migration_20251021000002_UpdatePermissionConstraints
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Starting migration: 20251021000002_UpdatePermissionConstraints");

                // Step 1: Drop old constraint that references permission_subject_type
                Console.WriteLine("Dropping old chk_onboarding_view_subjects_required constraint...");
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_onboarding 
                    DROP CONSTRAINT IF EXISTS chk_onboarding_view_subjects_required;
                ");

                // Step 2: Create new constraint using view_permission_subject_type
                Console.WriteLine("Creating new chk_onboarding_view_subjects_required constraint...");
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_constraint 
                            WHERE conname = 'chk_onboarding_view_subjects_required'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD CONSTRAINT chk_onboarding_view_subjects_required 
                            CHECK (
                                (view_permission_mode NOT IN (1, 2)) OR 
                                (
                                    (view_permission_subject_type = 1 AND view_teams IS NOT NULL AND jsonb_array_length(view_teams) > 0) OR
                                    (view_permission_subject_type = 2 AND view_users IS NOT NULL AND jsonb_array_length(view_users) > 0)
                                )
                            );
                            RAISE NOTICE 'Created chk_onboarding_view_subjects_required constraint';
                        ELSE
                            RAISE NOTICE 'Constraint chk_onboarding_view_subjects_required already exists';
                        END IF;
                    END $$;
                ");

                // Step 3: Add constraint for operate permissions
                Console.WriteLine("Creating chk_onboarding_operate_subjects_required constraint...");
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_constraint 
                            WHERE conname = 'chk_onboarding_operate_subjects_required'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD CONSTRAINT chk_onboarding_operate_subjects_required 
                            CHECK (
                                (operate_permission_subject_type = 1 AND operate_teams IS NOT NULL AND jsonb_array_length(operate_teams) > 0) OR
                                (operate_permission_subject_type = 2 AND operate_users IS NOT NULL AND jsonb_array_length(operate_users) > 0)
                            );
                            RAISE NOTICE 'Created chk_onboarding_operate_subjects_required constraint';
                        ELSE
                            RAISE NOTICE 'Constraint chk_onboarding_operate_subjects_required already exists';
                        END IF;
                    END $$;
                ");

                // Step 4: Drop old permission_subject_type constraint if it exists
                Console.WriteLine("Dropping old chk_onboarding_permission_subject_type constraint...");
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_onboarding 
                    DROP CONSTRAINT IF EXISTS chk_onboarding_permission_subject_type;
                ");

                // Step 5: Add constraints for new fields
                Console.WriteLine("Creating constraints for new permission subject type fields...");
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_constraint 
                            WHERE conname = 'chk_onboarding_view_permission_subject_type'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD CONSTRAINT chk_onboarding_view_permission_subject_type 
                            CHECK (view_permission_subject_type IN (1, 2));
                            RAISE NOTICE 'Created chk_onboarding_view_permission_subject_type constraint';
                        ELSE
                            RAISE NOTICE 'Constraint chk_onboarding_view_permission_subject_type already exists';
                        END IF;
                    END $$;
                ");

                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_constraint 
                            WHERE conname = 'chk_onboarding_operate_permission_subject_type'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD CONSTRAINT chk_onboarding_operate_permission_subject_type 
                            CHECK (operate_permission_subject_type IN (1, 2));
                            RAISE NOTICE 'Created chk_onboarding_operate_permission_subject_type constraint';
                        ELSE
                            RAISE NOTICE 'Constraint chk_onboarding_operate_permission_subject_type already exists';
                        END IF;
                    END $$;
                ");

                // Step 6: Verify constraints
                Console.WriteLine("Verifying constraints...");
                var constraints = db.Ado.GetDataTable(@"
                    SELECT conname, pg_get_constraintdef(oid) as definition
                    FROM pg_constraint
                    WHERE conrelid = 'ff_onboarding'::regclass
                    AND conname LIKE '%permission%'
                    ORDER BY conname
                ");

                Console.WriteLine("Current permission-related constraints:");
                foreach (System.Data.DataRow row in constraints.Rows)
                {
                    Console.WriteLine($"  - {row["conname"]}: {row["definition"]}");
                }

                Console.WriteLine("Migration 20251021000002_UpdatePermissionConstraints completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in migration 20251021000002_UpdatePermissionConstraints: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Rolling back migration: 20251021000002_UpdatePermissionConstraints");

                // Step 1: Drop new constraints
                Console.WriteLine("Dropping new constraints...");
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_onboarding 
                    DROP CONSTRAINT IF EXISTS chk_onboarding_operate_permission_subject_type;
                ");

                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_onboarding 
                    DROP CONSTRAINT IF EXISTS chk_onboarding_view_permission_subject_type;
                ");

                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_onboarding 
                    DROP CONSTRAINT IF EXISTS chk_onboarding_operate_subjects_required;
                ");

                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_onboarding 
                    DROP CONSTRAINT IF EXISTS chk_onboarding_view_subjects_required;
                ");

                // Step 2: Restore old constraints (if permission_subject_type still exists)
                Console.WriteLine("Restoring old constraints...");
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ff_onboarding' 
                            AND column_name = 'permission_subject_type'
                        ) THEN
                            -- Restore old permission_subject_type constraint
                            IF NOT EXISTS (
                                SELECT 1 FROM pg_constraint 
                                WHERE conname = 'chk_onboarding_permission_subject_type'
                            ) THEN
                                ALTER TABLE ff_onboarding 
                                ADD CONSTRAINT chk_onboarding_permission_subject_type 
                                CHECK (permission_subject_type IN (1, 2));
                            END IF;

                            -- Restore old view_subjects_required constraint
                            IF NOT EXISTS (
                                SELECT 1 FROM pg_constraint 
                                WHERE conname = 'chk_onboarding_view_subjects_required'
                            ) THEN
                                ALTER TABLE ff_onboarding 
                                ADD CONSTRAINT chk_onboarding_view_subjects_required 
                                CHECK (
                                    (view_permission_mode NOT IN (1, 2)) OR 
                                    (
                                        (permission_subject_type = 1 AND view_teams IS NOT NULL AND jsonb_array_length(view_teams) > 0) OR
                                        (permission_subject_type = 2 AND view_users IS NOT NULL AND jsonb_array_length(view_users) > 0)
                                    )
                                );
                            END IF;

                            RAISE NOTICE 'Restored old constraints';
                        ELSE
                            RAISE NOTICE 'Column permission_subject_type does not exist, skipping constraint restoration';
                        END IF;
                    END $$;
                ");

                Console.WriteLine("Rollback of migration 20251021000002_UpdatePermissionConstraints completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in rollback of migration 20251021000002_UpdatePermissionConstraints: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}

