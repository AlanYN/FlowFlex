using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Create Integration module tables
    /// Migration: 20251124000001_CreateIntegrationTables
    /// Date: 2025-11-24
    /// 
    /// This migration creates all tables required for the Integration Settings module:
    /// 1. ff_integration - Main integration configuration table
    /// 2. ff_entity_mapping - Maps external entities to WFE entities
    /// 3. ff_entity_key_mapping - Defines key fields for entity synchronization
    /// 4. ff_field_mapping - Maps fields between external systems and WFE
    /// 5. ff_quick_link - Quick links to external system resources
    /// 6. ff_integration_sync_log - Synchronization history and logs
    /// 7. ff_inbound_configuration - Inbound data sync configuration
    /// 8. ff_outbound_configuration - Outbound data sync configuration
    /// 9. ff_receive_external_data_config - Configuration for external system triggers
    /// </summary>
    public static class Migration_20251124000001_CreateIntegrationTables
    {
        public static void Up(ISqlSugarClient db)
        {
            Console.WriteLine("Starting migration: Create Integration module tables");

            // 1. Create ff_integration table
            db.Ado.ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS ff_integration (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
                    type VARCHAR(50) NOT NULL,
                    name VARCHAR(200) NOT NULL,
                    system_name VARCHAR(100) NOT NULL,
                    endpoint_url VARCHAR(500) NOT NULL,
                    auth_method INTEGER NOT NULL DEFAULT 0,
                    credentials TEXT NOT NULL DEFAULT '{}',
                    status INTEGER NOT NULL DEFAULT 0,
                    last_sync_date TIMESTAMPTZ,
                    error_message TEXT,
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

            // 3. Create ff_entity_key_mapping table
            db.Ado.ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS ff_entity_key_mapping (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
                    entity_mapping_id BIGINT NOT NULL,
                    external_key_field VARCHAR(100) NOT NULL,
                    wfe_key_field VARCHAR(100) NOT NULL,
                    key_type VARCHAR(50) NOT NULL DEFAULT 'Primary',
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0,
                    CONSTRAINT fk_entity_key_mapping_entity_mapping FOREIGN KEY (entity_mapping_id) 
                        REFERENCES ff_entity_mapping(id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS idx_entity_key_mapping_entity_mapping_id ON ff_entity_key_mapping(entity_mapping_id);
                CREATE INDEX IF NOT EXISTS idx_entity_key_mapping_key_type ON ff_entity_key_mapping(key_type);
            ");
            Console.WriteLine("✓ Created ff_entity_key_mapping table");

            // 4. Create ff_field_mapping table
            db.Ado.ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS ff_field_mapping (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
                    integration_id BIGINT NOT NULL,
                    entity_mapping_id BIGINT NOT NULL,
                    external_field_name VARCHAR(100) NOT NULL,
                    wfe_field_id VARCHAR(100) NOT NULL,
                    field_type INTEGER NOT NULL DEFAULT 0,
                    sync_direction INTEGER NOT NULL DEFAULT 1,
                    transform_rules TEXT NOT NULL DEFAULT '{}',
                    sort_order INTEGER DEFAULT 0,
                    is_required BOOLEAN DEFAULT FALSE,
                    default_value VARCHAR(500),
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0,
                    CONSTRAINT fk_field_mapping_integration FOREIGN KEY (integration_id) 
                        REFERENCES ff_integration(id) ON DELETE CASCADE,
                    CONSTRAINT fk_field_mapping_entity_mapping FOREIGN KEY (entity_mapping_id) 
                        REFERENCES ff_entity_mapping(id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS idx_field_mapping_integration_id ON ff_field_mapping(integration_id);
                CREATE INDEX IF NOT EXISTS idx_field_mapping_entity_mapping_id ON ff_field_mapping(entity_mapping_id);
                CREATE INDEX IF NOT EXISTS idx_field_mapping_sync_direction ON ff_field_mapping(sync_direction);
            ");
            Console.WriteLine("✓ Created ff_field_mapping table");

            // 5. Create ff_quick_link table
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

            // 6. Create ff_integration_sync_log table
            db.Ado.ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS ff_integration_sync_log (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
                    integration_id BIGINT NOT NULL,
                    sync_direction INTEGER NOT NULL,
                    entity_type VARCHAR(100) NOT NULL,
                    external_id VARCHAR(100),
                    internal_id VARCHAR(100),
                    sync_status INTEGER NOT NULL DEFAULT 0,
                    error_message TEXT,
                    request_payload TEXT,
                    response_payload TEXT,
                    synced_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    duration_ms BIGINT DEFAULT 0,
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0,
                    CONSTRAINT fk_integration_sync_log_integration FOREIGN KEY (integration_id) 
                        REFERENCES ff_integration(id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS idx_integration_sync_log_integration_id ON ff_integration_sync_log(integration_id);
                CREATE INDEX IF NOT EXISTS idx_integration_sync_log_sync_direction ON ff_integration_sync_log(sync_direction);
                CREATE INDEX IF NOT EXISTS idx_integration_sync_log_sync_status ON ff_integration_sync_log(sync_status);
                CREATE INDEX IF NOT EXISTS idx_integration_sync_log_synced_at ON ff_integration_sync_log(synced_at);
                CREATE INDEX IF NOT EXISTS idx_integration_sync_log_entity_type ON ff_integration_sync_log(entity_type);
            ");
            Console.WriteLine("✓ Created ff_integration_sync_log table");

            // 7. Create ff_inbound_configuration table
            db.Ado.ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS ff_inbound_configuration (
                    id BIGINT NOT NULL PRIMARY KEY,
                    integration_id BIGINT NOT NULL,
                    action_id BIGINT,
                    entity_types TEXT NOT NULL DEFAULT '[]',
                    field_mappings TEXT NOT NULL DEFAULT '[]',
                    attachment_settings TEXT NOT NULL DEFAULT '{}',
                    auto_sync BOOLEAN DEFAULT FALSE,
                    sync_interval INTEGER DEFAULT 0,
                    last_sync_date TIMESTAMPTZ,
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0,
                    CONSTRAINT fk_inbound_configuration_integration FOREIGN KEY (integration_id) 
                        REFERENCES ff_integration(id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS idx_inbound_configuration_integration_id ON ff_inbound_configuration(integration_id);
                CREATE INDEX IF NOT EXISTS idx_inbound_configuration_action_id ON ff_inbound_configuration(action_id);
                CREATE INDEX IF NOT EXISTS idx_inbound_configuration_auto_sync ON ff_inbound_configuration(auto_sync);
            ");
            Console.WriteLine("✓ Created ff_inbound_configuration table");

            // 8. Create ff_outbound_configuration table
            db.Ado.ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS ff_outbound_configuration (
                    id BIGINT NOT NULL PRIMARY KEY,
                    integration_id BIGINT NOT NULL,
                    action_id BIGINT,
                    entity_types TEXT NOT NULL DEFAULT '[]',
                    field_mappings TEXT NOT NULL DEFAULT '[]',
                    attachment_settings TEXT NOT NULL DEFAULT '{}',
                    sync_mode INTEGER NOT NULL DEFAULT 0,
                    webhook_url VARCHAR(500),
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0,
                    CONSTRAINT fk_outbound_configuration_integration FOREIGN KEY (integration_id) 
                        REFERENCES ff_integration(id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS idx_outbound_configuration_integration_id ON ff_outbound_configuration(integration_id);
                CREATE INDEX IF NOT EXISTS idx_outbound_configuration_action_id ON ff_outbound_configuration(action_id);
                CREATE INDEX IF NOT EXISTS idx_outbound_configuration_sync_mode ON ff_outbound_configuration(sync_mode);
            ");
            Console.WriteLine("✓ Created ff_outbound_configuration table");

            // 9. Create ff_receive_external_data_config table
            db.Ado.ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS ff_receive_external_data_config (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
                    integration_id BIGINT NOT NULL,
                    entity_name VARCHAR(200) NOT NULL,
                    trigger_workflow_id BIGINT NOT NULL,
                    field_mapping_config TEXT NOT NULL DEFAULT '{}',
                    is_active BOOLEAN DEFAULT TRUE,
                    description VARCHAR(500),
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0,
                    CONSTRAINT fk_receive_external_data_config_integration FOREIGN KEY (integration_id) 
                        REFERENCES ff_integration(id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS idx_receive_external_data_config_integration_id ON ff_receive_external_data_config(integration_id);
                CREATE INDEX IF NOT EXISTS idx_receive_external_data_config_entity_name ON ff_receive_external_data_config(entity_name);
                CREATE INDEX IF NOT EXISTS idx_receive_external_data_config_workflow_id ON ff_receive_external_data_config(trigger_workflow_id);
                CREATE UNIQUE INDEX IF NOT EXISTS idx_receive_external_data_config_unique 
                    ON ff_receive_external_data_config(integration_id, entity_name) WHERE is_valid = TRUE;
            ");
            Console.WriteLine("✓ Created ff_receive_external_data_config table");

            Console.WriteLine("Migration completed: All Integration module tables created successfully");
        }

        public static void Down(ISqlSugarClient db)
        {
            Console.WriteLine("Rolling back migration: Drop Integration module tables");

            // Drop tables in reverse order (to respect foreign key constraints)
            db.Ado.ExecuteCommand(@"
                DROP TABLE IF EXISTS ff_receive_external_data_config;
                DROP TABLE IF EXISTS ff_outbound_configuration;
                DROP TABLE IF EXISTS ff_inbound_configuration;
                DROP TABLE IF EXISTS ff_integration_sync_log;
                DROP TABLE IF EXISTS ff_quick_link;
                DROP TABLE IF EXISTS ff_field_mapping;
                DROP TABLE IF EXISTS ff_entity_key_mapping;
                DROP TABLE IF EXISTS ff_entity_mapping;
                DROP TABLE IF EXISTS ff_integration;
            ");

            Console.WriteLine("✓ Dropped all Integration module tables");
        }
    }
}

