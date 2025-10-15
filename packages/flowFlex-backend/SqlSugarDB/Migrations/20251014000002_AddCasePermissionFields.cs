using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add permission configuration fields to ff_onboarding table
    /// Migration: 20251014000002_AddCasePermissionFields
    /// Date: 2024-10-14
    /// 
    /// This migration adds the following permission fields to Case (Onboarding) entity:
    /// 1. permission_subject_type - Defines whether permissions are based on Teams or Users
    /// 2. view_permission_mode - Controls view permission strategy (Public/VisibleTo/InvisibleTo/Private)
    /// 3. view_subjects - JSONB array of team names or user IDs for view permission control
    /// 4. operate_subjects - JSONB array of team names or user IDs for operate permission control
    /// 
    /// These fields enable fine-grained access control for Cases, including:
    /// - Team-based or User-based permission models
    /// - Public/Private visibility control
    /// - Selective visibility to specific teams or users
    /// - Operation permission management
    /// </summary>
    public static class Migration_20251014000002_AddCasePermissionFields
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Starting migration: 20251014000002_AddCasePermissionFields");

                // 1. Add permission_subject_type column (INTEGER, default 1 for Team-based)
                Console.WriteLine("Adding permission_subject_type column...");
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ff_onboarding' 
                            AND column_name = 'permission_subject_type'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD COLUMN permission_subject_type INTEGER NOT NULL DEFAULT 1;
                            
                            COMMENT ON COLUMN ff_onboarding.permission_subject_type IS 'Permission Subject Type: 1=Team-based, 2=User-based';
                        END IF;
                    END $$;
                ");

                // 2. Add view_permission_mode column (INTEGER, default 0 for Public)
                Console.WriteLine("Adding view_permission_mode column...");
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ff_onboarding' 
                            AND column_name = 'view_permission_mode'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD COLUMN view_permission_mode INTEGER NOT NULL DEFAULT 0;
                            
                            COMMENT ON COLUMN ff_onboarding.view_permission_mode IS 'View Permission Mode: 0=Public, 1=VisibleToSubjects, 2=InvisibleToSubjects, 3=Private';
                        END IF;
                    END $$;
                ");

                // 3. Add view_teams column (JSONB for storing team names)
                Console.WriteLine("Adding view_teams column...");
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ff_onboarding' 
                            AND column_name = 'view_teams'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD COLUMN view_teams JSONB;
                            
                            COMMENT ON COLUMN ff_onboarding.view_teams IS 'JSONB array of team names for view permission control (used when permission_subject_type=1)';
                        END IF;
                    END $$;
                ");

                // 4. Add view_users column (JSONB for storing user IDs)
                Console.WriteLine("Adding view_users column...");
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ff_onboarding' 
                            AND column_name = 'view_users'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD COLUMN view_users JSONB;
                            
                            COMMENT ON COLUMN ff_onboarding.view_users IS 'JSONB array of user IDs for view permission control (used when permission_subject_type=2)';
                        END IF;
                    END $$;
                ");

                // 5. Add operate_teams column (JSONB for storing team names)
                Console.WriteLine("Adding operate_teams column...");
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ff_onboarding' 
                            AND column_name = 'operate_teams'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD COLUMN operate_teams JSONB;
                            
                            COMMENT ON COLUMN ff_onboarding.operate_teams IS 'JSONB array of team names that can perform operations (used when permission_subject_type=1)';
                        END IF;
                    END $$;
                ");

                // 6. Add operate_users column (JSONB for storing user IDs)
                Console.WriteLine("Adding operate_users column...");
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ff_onboarding' 
                            AND column_name = 'operate_users'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD COLUMN operate_users JSONB;
                            
                            COMMENT ON COLUMN ff_onboarding.operate_users IS 'JSONB array of user IDs that can perform operations (used when permission_subject_type=2)';
                        END IF;
                    END $$;
                ");

                // 5. Add indexes for better query performance
                Console.WriteLine("Creating indexes...");

                // Index on permission_subject_type for filtering by permission model
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_indexes 
                            WHERE tablename = 'ff_onboarding' 
                            AND indexname = 'idx_onboarding_permission_subject_type'
                        ) THEN
                            CREATE INDEX idx_onboarding_permission_subject_type 
                            ON ff_onboarding(permission_subject_type);
                        END IF;
                    END $$;
                ");

                // Index on view_permission_mode for filtering by visibility mode
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_indexes 
                            WHERE tablename = 'ff_onboarding' 
                            AND indexname = 'idx_onboarding_view_permission_mode'
                        ) THEN
                            CREATE INDEX idx_onboarding_view_permission_mode 
                            ON ff_onboarding(view_permission_mode);
                        END IF;
                    END $$;
                ");

                // GIN index on view_teams for efficient JSONB queries
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_indexes 
                            WHERE tablename = 'ff_onboarding' 
                            AND indexname = 'idx_onboarding_view_teams_gin'
                        ) THEN
                            CREATE INDEX idx_onboarding_view_teams_gin 
                            ON ff_onboarding USING GIN(view_teams);
                        END IF;
                    END $$;
                ");

                // GIN index on view_users for efficient JSONB queries
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_indexes 
                            WHERE tablename = 'ff_onboarding' 
                            AND indexname = 'idx_onboarding_view_users_gin'
                        ) THEN
                            CREATE INDEX idx_onboarding_view_users_gin 
                            ON ff_onboarding USING GIN(view_users);
                        END IF;
                    END $$;
                ");

                // GIN index on operate_teams for efficient JSONB queries
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_indexes 
                            WHERE tablename = 'ff_onboarding' 
                            AND indexname = 'idx_onboarding_operate_teams_gin'
                        ) THEN
                            CREATE INDEX idx_onboarding_operate_teams_gin 
                            ON ff_onboarding USING GIN(operate_teams);
                        END IF;
                    END $$;
                ");

                // GIN index on operate_users for efficient JSONB queries
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_indexes 
                            WHERE tablename = 'ff_onboarding' 
                            AND indexname = 'idx_onboarding_operate_users_gin'
                        ) THEN
                            CREATE INDEX idx_onboarding_operate_users_gin 
                            ON ff_onboarding USING GIN(operate_users);
                        END IF;
                    END $$;
                ");

                // 6. Add check constraints to ensure data integrity
                Console.WriteLine("Adding check constraints...");

                // Constraint for permission_subject_type (must be 1 or 2)
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_constraint 
                            WHERE conname = 'chk_onboarding_permission_subject_type'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD CONSTRAINT chk_onboarding_permission_subject_type 
                            CHECK (permission_subject_type IN (1, 2));
                        END IF;
                    END $$;
                ");

                // Constraint for view_permission_mode (must be 0, 1, 2, or 3)
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_constraint 
                            WHERE conname = 'chk_onboarding_view_permission_mode'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD CONSTRAINT chk_onboarding_view_permission_mode 
                            CHECK (view_permission_mode IN (0, 1, 2, 3));
                        END IF;
                    END $$;
                ");

                // Constraint: view_teams, view_users, operate_teams, operate_users must be NULL or valid JSON arrays
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_constraint 
                            WHERE conname = 'chk_onboarding_view_teams_array'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD CONSTRAINT chk_onboarding_view_teams_array 
                            CHECK (view_teams IS NULL OR jsonb_typeof(view_teams) = 'array');
                        END IF;
                    END $$;
                ");

                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_constraint 
                            WHERE conname = 'chk_onboarding_view_users_array'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD CONSTRAINT chk_onboarding_view_users_array 
                            CHECK (view_users IS NULL OR jsonb_typeof(view_users) = 'array');
                        END IF;
                    END $$;
                ");

                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_constraint 
                            WHERE conname = 'chk_onboarding_operate_teams_array'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD CONSTRAINT chk_onboarding_operate_teams_array 
                            CHECK (operate_teams IS NULL OR jsonb_typeof(operate_teams) = 'array');
                        END IF;
                    END $$;
                ");

                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_constraint 
                            WHERE conname = 'chk_onboarding_operate_users_array'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD CONSTRAINT chk_onboarding_operate_users_array 
                            CHECK (operate_users IS NULL OR jsonb_typeof(operate_users) = 'array');
                        END IF;
                    END $$;
                ");

                // 7. Business logic constraints
                Console.WriteLine("Adding business logic constraints...");

                // When view_permission_mode is VisibleToSubjects(1) or InvisibleToSubjects(2), 
                // view_teams (when PermissionSubjectType=Team) or view_users (when PermissionSubjectType=User) should not be NULL or empty
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
                                    (permission_subject_type = 1 AND view_teams IS NOT NULL AND jsonb_array_length(view_teams) > 0) OR
                                    (permission_subject_type = 2 AND view_users IS NOT NULL AND jsonb_array_length(view_users) > 0)
                                )
                            );
                        END IF;
                    END $$;
                ");

                // When view_permission_mode is Private(3), both view_teams and view_users should be NULL
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_constraint 
                            WHERE conname = 'chk_onboarding_private_no_subjects'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD CONSTRAINT chk_onboarding_private_no_subjects 
                            CHECK (
                                (view_permission_mode <> 3) OR 
                                (view_teams IS NULL AND view_users IS NULL)
                            );
                        END IF;
                    END $$;
                ");

                Console.WriteLine("Migration 20251014000002_AddCasePermissionFields completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in migration 20251014000002_AddCasePermissionFields: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Rolling back migration: 20251014000002_AddCasePermissionFields");

                // Drop constraints
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_onboarding 
                    DROP CONSTRAINT IF EXISTS chk_onboarding_private_no_subjects;
                ");

                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_onboarding 
                    DROP CONSTRAINT IF EXISTS chk_onboarding_view_subjects_required;
                ");

                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_onboarding 
                    DROP CONSTRAINT IF EXISTS chk_onboarding_operate_users_array;
                ");

                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_onboarding 
                    DROP CONSTRAINT IF EXISTS chk_onboarding_operate_teams_array;
                ");

                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_onboarding 
                    DROP CONSTRAINT IF EXISTS chk_onboarding_view_users_array;
                ");

                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_onboarding 
                    DROP CONSTRAINT IF EXISTS chk_onboarding_view_teams_array;
                ");

                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_onboarding 
                    DROP CONSTRAINT IF EXISTS chk_onboarding_view_permission_mode;
                ");

                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_onboarding 
                    DROP CONSTRAINT IF EXISTS chk_onboarding_permission_subject_type;
                ");

                // Drop indexes
                db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_onboarding_operate_users_gin;");
                db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_onboarding_operate_teams_gin;");
                db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_onboarding_view_users_gin;");
                db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_onboarding_view_teams_gin;");
                db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_onboarding_view_permission_mode;");
                db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_onboarding_permission_subject_type;");

                // Drop columns
                db.Ado.ExecuteCommand("ALTER TABLE ff_onboarding DROP COLUMN IF EXISTS operate_users;");
                db.Ado.ExecuteCommand("ALTER TABLE ff_onboarding DROP COLUMN IF EXISTS operate_teams;");
                db.Ado.ExecuteCommand("ALTER TABLE ff_onboarding DROP COLUMN IF EXISTS view_users;");
                db.Ado.ExecuteCommand("ALTER TABLE ff_onboarding DROP COLUMN IF EXISTS view_teams;");
                db.Ado.ExecuteCommand("ALTER TABLE ff_onboarding DROP COLUMN IF EXISTS view_permission_mode;");
                db.Ado.ExecuteCommand("ALTER TABLE ff_onboarding DROP COLUMN IF EXISTS permission_subject_type;");

                Console.WriteLine("Rollback of migration 20251014000002_AddCasePermissionFields completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rolling back migration 20251014000002_AddCasePermissionFields: {ex.Message}");
                throw;
            }
        }
    }
}

