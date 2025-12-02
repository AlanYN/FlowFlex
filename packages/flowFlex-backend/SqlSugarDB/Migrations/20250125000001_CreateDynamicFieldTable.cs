using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Create Dynamic Field table for Field Mapping
    /// Migration: 20250125000001_CreateDynamicFieldTable
    /// Date: 2025-01-25
    /// 
    /// This migration creates the ff_dynamic_field table to store dynamic field definitions
    /// that can be used in Field Mapping. Fields are tenant-isolated and can be initialized
    /// from static-field.json.
    /// </summary>
    public static class Migration_20250125000001_CreateDynamicFieldTable
    {
        public static void Up(ISqlSugarClient db)
        {
            Console.WriteLine("Starting migration: Create Dynamic Field table");

            // Create ff_dynamic_field table
            db.Ado.ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS ff_dynamic_field (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
                    field_id VARCHAR(100) NOT NULL,
                    field_label VARCHAR(200) NOT NULL,
                    form_prop VARCHAR(100) NOT NULL DEFAULT '',
                    category VARCHAR(100) NOT NULL DEFAULT '',
                    field_type INTEGER NOT NULL DEFAULT 0,
                    sort_order INTEGER NOT NULL DEFAULT 0,
                    is_required BOOLEAN NOT NULL DEFAULT FALSE,
                    is_system BOOLEAN NOT NULL DEFAULT FALSE,
                    is_valid BOOLEAN NOT NULL DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0
                );

                CREATE INDEX IF NOT EXISTS idx_dynamic_field_field_id ON ff_dynamic_field(field_id);
                CREATE INDEX IF NOT EXISTS idx_dynamic_field_category ON ff_dynamic_field(category);
                CREATE INDEX IF NOT EXISTS idx_dynamic_field_tenant_app ON ff_dynamic_field(app_code, tenant_id);
                CREATE UNIQUE INDEX IF NOT EXISTS idx_dynamic_field_unique_field_id_tenant_app 
                    ON ff_dynamic_field(field_id, app_code, tenant_id) WHERE is_valid = TRUE;
            ");
            Console.WriteLine("✓ Created ff_dynamic_field table");

            Console.WriteLine("Migration completed: Dynamic Field table created successfully");
        }

        public static void Down(ISqlSugarClient db)
        {
            Console.WriteLine("Rolling back migration: Drop Dynamic Field table");

            db.Ado.ExecuteCommand(@"
                DROP TABLE IF EXISTS ff_dynamic_field;
            ");

            Console.WriteLine("✓ Dropped ff_dynamic_field table");
        }
    }
}

