using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Create Integration module tables (Consolidated Migration)
    /// Migration: 20251124000001_CreateIntegrationTables
    /// Date: 2025-11-24 (Consolidated: 2025-11-28)
    /// 
    /// This migration creates all tables required for the Integration Settings module:
    /// 1. ff_integration - Main integration configuration table
    /// 2. ff_entity_mapping - Maps external entities to WFE entities
    /// 3. ff_inbound_field_mapping - Maps fields for inbound data sync
    /// 4. ff_quick_link - Quick links to external system resources
    /// 
    /// Note: This is a consolidated migration that combines multiple migrations into one.
    /// Removed tables: ff_inbound_configuration, ff_outbound_configuration, ff_outbound_field_config,
    ///                 ff_entity_key_mapping, ff_integration_sync_log, ff_receive_external_data_config
    /// </summary>
    public static class Migration_20251124000001_CreateIntegrationTables
    {
        public static void Up(ISqlSugarClient db)
        {
            Console.WriteLine("Starting migration: Create Integration module tables (Consolidated)");

            // 1. Create ff_integration table
            db.Ado.ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS ff_integration (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
                    type VARCHAR(50) NOT NULL,
                    name VARCHAR(200) NOT NULL,
                    description VARCHAR(500),
                    system_name VARCHAR(100) NOT NULL,
                    endpoint_url VARCHAR(500) NOT NULL,
                    auth_method INTEGER NOT NULL DEFAULT 0,
                    credentials TEXT NOT NULL DEFAULT '{}',
                    status INTEGER NOT NULL DEFAULT 0,
                    last_sync_date TIMESTAMPTZ,
                    error_message TEXT,
                    inbound_attachments TEXT,
                    outbound_attachments TEXT,
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0
                );

                CREATE INDEX IF NOT EXISTS idx_integration_tenant_id ON ff_integration(tenant_id);
                CREATE INDEX IF NOT EXISTS idx_integration_app_code ON ff_integration(app_code);
                CREATE INDEX IF NOT EXISTS idx_integration_type ON ff_integration(type);
                CREATE INDEX IF NOT EXISTS idx_integration_status ON ff_integration(status);
                CREATE INDEX IF NOT EXISTS idx_integration_name ON ff_integration(name);
            ");
            Console.WriteLine("✓ Created ff_integration table");

            // 2. Create ff_entity_mapping table
            db.Ado.ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS ff_entity_mapping (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
                    integration_id BIGINT NOT NULL,
                    system_id VARCHAR(200),
                    external_entity_name VARCHAR(100) NOT NULL,
                    external_entity_type VARCHAR(100) NOT NULL,
                    wfe_entity_type VARCHAR(100) NOT NULL,
                    workflow_ids TEXT NOT NULL DEFAULT '[]',
                    is_active BOOLEAN DEFAULT TRUE,
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0,
                    CONSTRAINT fk_entity_mapping_integration FOREIGN KEY (integration_id) 
                        REFERENCES ff_integration(id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS idx_entity_mapping_integration_id ON ff_entity_mapping(integration_id);
                CREATE INDEX IF NOT EXISTS idx_entity_mapping_external_type ON ff_entity_mapping(external_entity_type);
                CREATE INDEX IF NOT EXISTS idx_entity_mapping_wfe_type ON ff_entity_mapping(wfe_entity_type);
                CREATE INDEX IF NOT EXISTS idx_entity_mapping_is_active ON ff_entity_mapping(is_active);
            ");
            Console.WriteLine("✓ Created ff_entity_mapping table");

            // 3. Create ff_inbound_field_mapping table
            db.Ado.ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS ff_inbound_field_mapping (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
                    integration_id BIGINT NOT NULL,
                    action_id BIGINT,
                    external_field_name VARCHAR(100) NOT NULL,
                    wfe_field_id VARCHAR(100) NOT NULL,
                    field_type INTEGER NOT NULL DEFAULT 0,
                    sync_direction INTEGER NOT NULL DEFAULT 0,
                    sort_order INTEGER NOT NULL DEFAULT 0,
                    is_required BOOLEAN NOT NULL DEFAULT FALSE,
                    default_value VARCHAR(500),
                    is_valid BOOLEAN NOT NULL DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0,
                    CONSTRAINT fk_inbound_field_mapping_integration FOREIGN KEY (integration_id) 
                        REFERENCES ff_integration(id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS idx_inbound_field_mapping_integration_id ON ff_inbound_field_mapping(integration_id);
                CREATE INDEX IF NOT EXISTS idx_inbound_field_mapping_action_id ON ff_inbound_field_mapping(action_id);
                CREATE INDEX IF NOT EXISTS idx_inbound_field_mapping_integration_action ON ff_inbound_field_mapping(integration_id, action_id);
            ");
            Console.WriteLine("✓ Created ff_inbound_field_mapping table");

            // 4. Create ff_quick_link table
            db.Ado.ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS ff_quick_link (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
                    integration_id BIGINT NOT NULL,
                    name VARCHAR(100) NOT NULL,
                    target_url VARCHAR(500) NOT NULL,
                    icon VARCHAR(50),
                    description VARCHAR(500),
                    parameters TEXT NOT NULL DEFAULT '{}',
                    redirect_type INTEGER NOT NULL DEFAULT 0,
                    display_order INTEGER DEFAULT 0,
                    is_active BOOLEAN DEFAULT TRUE,
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0,
                    CONSTRAINT fk_quick_link_integration FOREIGN KEY (integration_id) 
                        REFERENCES ff_integration(id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS idx_quick_link_integration_id ON ff_quick_link(integration_id);
                CREATE INDEX IF NOT EXISTS idx_quick_link_display_order ON ff_quick_link(display_order);
            ");
            Console.WriteLine("✓ Created ff_quick_link table");

            Console.WriteLine("Migration completed: All Integration module tables created successfully");
        }

        public static void Down(ISqlSugarClient db)
        {
            Console.WriteLine("Rolling back migration: Drop Integration module tables");

            // Drop tables in reverse order (to respect foreign key constraints)
            db.Ado.ExecuteCommand(@"
                DROP TABLE IF EXISTS ff_quick_link;
                DROP TABLE IF EXISTS ff_inbound_field_mapping;
                DROP TABLE IF EXISTS ff_entity_mapping;
                DROP TABLE IF EXISTS ff_integration;
            ");

            Console.WriteLine("✓ Dropped all Integration module tables");
        }
    }
}
