using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add tenant_id and app_code fields to InboundConfiguration and OutboundConfiguration tables
    /// Migration: 20251124000004_AddTenantIdAndAppCodeToConfigurations
    /// Date: 2025-11-24
    /// 
    /// This migration adds tenant_id and app_code columns to:
    /// 1. ff_inbound_configuration - to support tenant and app isolation
    /// 2. ff_outbound_configuration - to support tenant and app isolation
    /// 
    /// These fields are required because the entities inherit from EntityBase which includes
    /// TenantId and AppCode properties from base classes.
    /// </summary>
    public static class Migration_20251124000004_AddTenantIdAndAppCodeToConfigurations
    {
        public static void Up(ISqlSugarClient db)
        {
            Console.WriteLine("Starting migration: Add tenant_id and app_code to Configuration tables");

            // 1. Add tenant_id and app_code to ff_inbound_configuration table
            var inboundTenantIdExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_inbound_configuration' 
                AND column_name = 'tenant_id'
            ").Rows.Count > 0;

            if (!inboundTenantIdExists)
            {
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_inbound_configuration 
                    ADD COLUMN tenant_id VARCHAR(32) NOT NULL DEFAULT 'default';
                ");

                // Create index for tenant_id
                db.Ado.ExecuteCommand(@"
                    CREATE INDEX IF NOT EXISTS idx_inbound_configuration_tenant_id 
                    ON ff_inbound_configuration(tenant_id);
                ");

                Console.WriteLine("✓ Added tenant_id column to ff_inbound_configuration table");
            }
            else
            {
                Console.WriteLine("✓ Column 'tenant_id' already exists in ff_inbound_configuration table, skipping");
            }

            var inboundAppCodeExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_inbound_configuration' 
                AND column_name = 'app_code'
            ").Rows.Count > 0;

            if (!inboundAppCodeExists)
            {
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_inbound_configuration 
                    ADD COLUMN app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT';
                ");

                // Create index for app_code
                db.Ado.ExecuteCommand(@"
                    CREATE INDEX IF NOT EXISTS idx_inbound_configuration_app_code 
                    ON ff_inbound_configuration(app_code);
                ");

                Console.WriteLine("✓ Added app_code column to ff_inbound_configuration table");
            }
            else
            {
                Console.WriteLine("✓ Column 'app_code' already exists in ff_inbound_configuration table, skipping");
            }

            // 2. Add tenant_id and app_code to ff_outbound_configuration table
            var outboundTenantIdExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_outbound_configuration' 
                AND column_name = 'tenant_id'
            ").Rows.Count > 0;

            if (!outboundTenantIdExists)
            {
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_outbound_configuration 
                    ADD COLUMN tenant_id VARCHAR(32) NOT NULL DEFAULT 'default';
                ");

                // Create index for tenant_id
                db.Ado.ExecuteCommand(@"
                    CREATE INDEX IF NOT EXISTS idx_outbound_configuration_tenant_id 
                    ON ff_outbound_configuration(tenant_id);
                ");

                Console.WriteLine("✓ Added tenant_id column to ff_outbound_configuration table");
            }
            else
            {
                Console.WriteLine("✓ Column 'tenant_id' already exists in ff_outbound_configuration table, skipping");
            }

            var outboundAppCodeExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_outbound_configuration' 
                AND column_name = 'app_code'
            ").Rows.Count > 0;

            if (!outboundAppCodeExists)
            {
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_outbound_configuration 
                    ADD COLUMN app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT';
                ");

                // Create index for app_code
                db.Ado.ExecuteCommand(@"
                    CREATE INDEX IF NOT EXISTS idx_outbound_configuration_app_code 
                    ON ff_outbound_configuration(app_code);
                ");

                Console.WriteLine("✓ Added app_code column to ff_outbound_configuration table");
            }
            else
            {
                Console.WriteLine("✓ Column 'app_code' already exists in ff_outbound_configuration table, skipping");
            }

            Console.WriteLine("Migration completed: Add tenant_id and app_code to Configuration tables");
        }

        public static void Down(ISqlSugarClient db)
        {
            Console.WriteLine("Starting rollback: Remove tenant_id and app_code from Configuration tables");

            // 1. Remove tenant_id and app_code from ff_inbound_configuration table
            var inboundTenantIdExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_inbound_configuration' 
                AND column_name = 'tenant_id'
            ").Rows.Count > 0;

            if (inboundTenantIdExists)
            {
                // Drop index first
                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_inbound_configuration_tenant_id;
                ");

                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_inbound_configuration 
                    DROP COLUMN tenant_id;
                ");

                Console.WriteLine("✓ Removed tenant_id column from ff_inbound_configuration table");
            }

            var inboundAppCodeExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_inbound_configuration' 
                AND column_name = 'app_code'
            ").Rows.Count > 0;

            if (inboundAppCodeExists)
            {
                // Drop index first
                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_inbound_configuration_app_code;
                ");

                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_inbound_configuration 
                    DROP COLUMN app_code;
                ");

                Console.WriteLine("✓ Removed app_code column from ff_inbound_configuration table");
            }

            // 2. Remove tenant_id and app_code from ff_outbound_configuration table
            var outboundTenantIdExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_outbound_configuration' 
                AND column_name = 'tenant_id'
            ").Rows.Count > 0;

            if (outboundTenantIdExists)
            {
                // Drop index first
                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_outbound_configuration_tenant_id;
                ");

                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_outbound_configuration 
                    DROP COLUMN tenant_id;
                ");

                Console.WriteLine("✓ Removed tenant_id column from ff_outbound_configuration table");
            }

            var outboundAppCodeExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_outbound_configuration' 
                AND column_name = 'app_code'
            ").Rows.Count > 0;

            if (outboundAppCodeExists)
            {
                // Drop index first
                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_outbound_configuration_app_code;
                ");

                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_outbound_configuration 
                    DROP COLUMN app_code;
                ");

                Console.WriteLine("✓ Removed app_code column from ff_outbound_configuration table");
            }

            Console.WriteLine("Rollback completed: Remove tenant_id and app_code from Configuration tables");
        }
    }
}

