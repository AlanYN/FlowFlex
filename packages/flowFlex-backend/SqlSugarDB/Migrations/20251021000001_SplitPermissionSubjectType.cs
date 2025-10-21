using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Split PermissionSubjectType into ViewPermissionSubjectType and OperatePermissionSubjectType
    /// Migration: 20251021000001_SplitPermissionSubjectType
    /// Date: 2025-10-21
    /// 
    /// This migration splits the single permission_subject_type field into two separate fields:
    /// 1. view_permission_subject_type - Controls whether view permissions are based on Teams or Users
    /// 2. operate_permission_subject_type - Controls whether operate permissions are based on Teams or Users
    /// 
    /// This allows for more flexible permission control, for example:
    /// - View permissions based on Users (precise control)
    /// - Operate permissions based on Teams (easier management)
    /// 
    /// The migration will:
    /// 1. Add two new columns with default value 0 (Team)
    /// 2. Copy existing permission_subject_type values to both new fields
    /// 3. Add column comments for documentation
    /// 
    /// Note: The old permission_subject_type column is NOT dropped to maintain backward compatibility.
    /// It can be dropped in a future migration after confirming all applications are updated.
    /// </summary>
    public static class Migration_20251021000001_SplitPermissionSubjectType
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Starting migration: 20251021000001_SplitPermissionSubjectType");

                // Step 1: Add view_permission_subject_type column
                Console.WriteLine("Adding view_permission_subject_type column...");
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ff_onboarding' 
                            AND column_name = 'view_permission_subject_type'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD COLUMN view_permission_subject_type INTEGER NOT NULL DEFAULT 0;
                            
                            COMMENT ON COLUMN ff_onboarding.view_permission_subject_type IS 'View Permission Subject Type: 0=Team (default), 1=User';
                            
                            RAISE NOTICE 'Added view_permission_subject_type column';
                        ELSE
                            RAISE NOTICE 'Column view_permission_subject_type already exists, skipping';
                        END IF;
                    END $$;
                ");

                // Step 2: Add operate_permission_subject_type column
                Console.WriteLine("Adding operate_permission_subject_type column...");
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ff_onboarding' 
                            AND column_name = 'operate_permission_subject_type'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD COLUMN operate_permission_subject_type INTEGER NOT NULL DEFAULT 0;
                            
                            COMMENT ON COLUMN ff_onboarding.operate_permission_subject_type IS 'Operate Permission Subject Type: 0=Team (default), 1=User';
                            
                            RAISE NOTICE 'Added operate_permission_subject_type column';
                        ELSE
                            RAISE NOTICE 'Column operate_permission_subject_type already exists, skipping';
                        END IF;
                    END $$;
                ");

                // Step 3: Migrate existing data from permission_subject_type to new fields
                Console.WriteLine("Migrating existing permission_subject_type data to new fields...");
                db.Ado.ExecuteCommand(@"
                    DO $$
                    DECLARE
                        updated_count INTEGER;
                    BEGIN
                        -- Check if permission_subject_type column exists
                        IF EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ff_onboarding' 
                            AND column_name = 'permission_subject_type'
                        ) THEN
                            -- Copy existing values to both new fields
                            UPDATE ff_onboarding 
                            SET 
                                view_permission_subject_type = COALESCE(permission_subject_type, 0),
                                operate_permission_subject_type = COALESCE(permission_subject_type, 0)
                            WHERE view_permission_subject_type = 0 
                               OR operate_permission_subject_type = 0;
                            
                            GET DIAGNOSTICS updated_count = ROW_COUNT;
                            RAISE NOTICE 'Migrated % records from permission_subject_type to new fields', updated_count;
                        ELSE
                            RAISE NOTICE 'Column permission_subject_type does not exist, skipping data migration';
                        END IF;
                    END $$;
                ");

                // Step 4: Verify migration
                Console.WriteLine("Verifying migration...");
                var verificationResult = db.Ado.GetDataTable(@"
                    SELECT 
                        COUNT(*) as total_records,
                        COUNT(CASE WHEN view_permission_subject_type IS NULL THEN 1 END) as null_view_count,
                        COUNT(CASE WHEN operate_permission_subject_type IS NULL THEN 1 END) as null_operate_count
                    FROM ff_onboarding
                ");

                if (verificationResult.Rows.Count > 0)
                {
                    var row = verificationResult.Rows[0];
                    Console.WriteLine($"Verification results:");
                    Console.WriteLine($"  Total records: {row["total_records"]}");
                    Console.WriteLine($"  NULL view_permission_subject_type: {row["null_view_count"]}");
                    Console.WriteLine($"  NULL operate_permission_subject_type: {row["null_operate_count"]}");

                    var nullViewCount = Convert.ToInt32(row["null_view_count"]);
                    var nullOperateCount = Convert.ToInt32(row["null_operate_count"]);

                    if (nullViewCount > 0 || nullOperateCount > 0)
                    {
                        Console.WriteLine("WARNING: Found NULL values in new columns!");
                    }
                    else
                    {
                        Console.WriteLine("✓ All records have valid values in new columns");
                    }
                }

                Console.WriteLine("Migration 20251021000001_SplitPermissionSubjectType completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in migration 20251021000001_SplitPermissionSubjectType: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Rolling back migration: 20251021000001_SplitPermissionSubjectType");

                // Step 1: Drop operate_permission_subject_type column
                Console.WriteLine("Dropping operate_permission_subject_type column...");
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ff_onboarding' 
                            AND column_name = 'operate_permission_subject_type'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            DROP COLUMN operate_permission_subject_type;
                            
                            RAISE NOTICE 'Dropped operate_permission_subject_type column';
                        ELSE
                            RAISE NOTICE 'Column operate_permission_subject_type does not exist, skipping';
                        END IF;
                    END $$;
                ");

                // Step 2: Drop view_permission_subject_type column
                Console.WriteLine("Dropping view_permission_subject_type column...");
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ff_onboarding' 
                            AND column_name = 'view_permission_subject_type'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            DROP COLUMN view_permission_subject_type;
                            
                            RAISE NOTICE 'Dropped view_permission_subject_type column';
                        ELSE
                            RAISE NOTICE 'Column view_permission_subject_type does not exist, skipping';
                        END IF;
                    END $$;
                ");

                Console.WriteLine("Rollback of migration 20251021000001_SplitPermissionSubjectType completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in rollback of migration 20251021000001_SplitPermissionSubjectType: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}

