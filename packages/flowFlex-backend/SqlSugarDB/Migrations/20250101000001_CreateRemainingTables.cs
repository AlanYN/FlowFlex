using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// FlowFlex database remaining tables migration
    /// Version: 2.0.0
    /// Created: 2025-01-01
    /// Description: Create remaining table structures and triggers
    /// </summary>
    public class CreateRemainingTables_20250101000001
    {
        /// <summary>
        /// Execute migration - create remaining table structures
        /// </summary>
        public static void Up(ISqlSugarClient db)
        {
            // Create onboarding file table
            db.Ado.ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS ff_onboarding_file (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    onboarding_id BIGINT NOT NULL,
                    stage_id BIGINT,
                    attachment_id BIGINT NOT NULL,
                    original_file_name VARCHAR(255) NOT NULL,
                    stored_file_name VARCHAR(255) NOT NULL,
                    file_extension VARCHAR(10),
                    file_size BIGINT DEFAULT 0,
                    content_type VARCHAR(100),
                    category VARCHAR(50) DEFAULT 'Document',
                    description VARCHAR(500),
                    is_required BOOLEAN DEFAULT FALSE,
                    tags VARCHAR(200),
                    access_url VARCHAR(500),
                    storage_path VARCHAR(500),
                    uploaded_by_id VARCHAR(50),
                    uploaded_by_name VARCHAR(100),
                    uploaded_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    last_modified_date TIMESTAMPTZ,
                    status VARCHAR(20) DEFAULT 'Active',
                    version INTEGER DEFAULT 1,
                    file_hash VARCHAR(64),
                    sort_order INTEGER DEFAULT 0,
                    extended_properties TEXT,
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0
                );
            ");

            // Create operation change log table
            db.Ado.ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS ff_operation_change_log (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    operation_type VARCHAR(50) NOT NULL,
                    business_module VARCHAR(50) NOT NULL,
                    business_id BIGINT NOT NULL,
                    onboarding_id BIGINT,
                    stage_id BIGINT,
                    operation_status VARCHAR(20),
                    operation_description VARCHAR(500),
                    operation_title VARCHAR(200),
                    operation_source VARCHAR(100),
                    before_data JSONB,
                    after_data JSONB,
                    changed_fields JSONB,
                    operator_id BIGINT NOT NULL,
                    operator_name VARCHAR(100) NOT NULL,
                    operation_time TIMESTAMPTZ NOT NULL,
                    ip_address VARCHAR(50),
                    user_agent VARCHAR(500),
                    extended_data JSONB,
                    error_message VARCHAR(1000),
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0
                );
            ");

            // Create internal notes table
            db.Ado.ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS ff_internal_notes (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    onboarding_id BIGINT NOT NULL,
                    stage_id BIGINT,
                    title VARCHAR(200) DEFAULT '',
                    content VARCHAR(4000) NOT NULL DEFAULT '',
                    note_type VARCHAR(50) DEFAULT 'General',
                    priority VARCHAR(20) DEFAULT 'Normal',
                    is_resolved BOOLEAN DEFAULT FALSE,
                    resolved_time TIMESTAMPTZ,
                    resolved_by_id BIGINT,
                    resolved_by VARCHAR(100) DEFAULT '',
                    resolution_notes VARCHAR(1000) DEFAULT '',
                    tags JSONB DEFAULT '[]',
                    visibility VARCHAR(20) DEFAULT 'Internal',
                    mentioned_user_ids JSONB DEFAULT '[]',
                    author VARCHAR(100) DEFAULT '',
                    author_id BIGINT,
                    parent_note_id BIGINT,
                    source VARCHAR(50) DEFAULT 'customer_portal',
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0
                );
            ");

            // Create stage completion log table
            db.Ado.ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS ff_stage_completion_log (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    onboarding_id BIGINT NOT NULL,
                    stage_id BIGINT NOT NULL,
                    stage_name VARCHAR(200) DEFAULT '',
                    log_type VARCHAR(50) NOT NULL DEFAULT '',
                    action VARCHAR(100) DEFAULT '',
                    log_data JSONB DEFAULT '{}',
                    success BOOLEAN DEFAULT TRUE,
                    error_message VARCHAR(2000) DEFAULT '',
                    network_status VARCHAR(20) DEFAULT '',
                    response_time INTEGER,
                    user_agent VARCHAR(500) DEFAULT '',
                    request_url VARCHAR(500) DEFAULT '',
                    source VARCHAR(50) DEFAULT 'customer_portal',
                    ip_address VARCHAR(45) DEFAULT '',
                    session_id VARCHAR(100) DEFAULT '',
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0
                );
            ");

            // Create static field values table
            db.Ado.ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS ff_static_field_values (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    onboarding_id BIGINT NOT NULL,
                    stage_id BIGINT NOT NULL,
                    field_name VARCHAR(100) NOT NULL,
                    display_name VARCHAR(200),
                    field_value_json JSONB DEFAULT '{}',
                    field_type VARCHAR(50) DEFAULT 'text',
                    is_required BOOLEAN DEFAULT FALSE,
                    status VARCHAR(20) DEFAULT 'Draft',
                    completion_rate INTEGER DEFAULT 0,
                    submit_time TIMESTAMPTZ,
                    review_time TIMESTAMPTZ,
                    reviewer_id BIGINT,
                    review_notes VARCHAR(1000),
                    version INTEGER DEFAULT 1,
                    is_latest BOOLEAN DEFAULT TRUE,
                    is_submitted BOOLEAN DEFAULT FALSE,
                    source VARCHAR(50) DEFAULT 'customer_portal',
                    ip_address VARCHAR(45) DEFAULT '',
                    user_agent VARCHAR(500) DEFAULT '',
                    validation_status VARCHAR(20) DEFAULT 'Pending',
                    validation_errors VARCHAR(1000) DEFAULT '',
                    metadata JSONB,
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0
                );
            ");

            // Create workflow version table
            db.Ado.ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS ff_workflow_version (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    original_workflow_id BIGINT NOT NULL,
                    name VARCHAR(100) NOT NULL,
                    description VARCHAR(500),
                    is_default BOOLEAN DEFAULT FALSE,
                    status VARCHAR(50) DEFAULT 'active',
                    start_date TIMESTAMPTZ,
                    end_date TIMESTAMPTZ,
                    version INTEGER DEFAULT 1,
                    is_active BOOLEAN DEFAULT TRUE,
                    config_json VARCHAR(2000),
                    change_reason VARCHAR(500),
                    change_type VARCHAR(50) DEFAULT 'Created',
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0
                );
            ");

            // Create stage version table
            db.Ado.ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS ff_stage_version (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    workflow_version_id BIGINT NOT NULL,
                    original_stage_id BIGINT NOT NULL,
                    name VARCHAR(100) NOT NULL,
                    description VARCHAR(500),
                    default_assigned_group VARCHAR(100),
                    default_assignee VARCHAR(100),
                    estimated_duration DECIMAL,
                    order_index INTEGER NOT NULL,
                    checklist_id BIGINT,
                    questionnaire_id BIGINT,
                    color VARCHAR(20),
                    required_fields_json VARCHAR(1000),
                    components_json TEXT,
                    workflow_version VARCHAR(20),
                    is_active BOOLEAN DEFAULT TRUE,
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0
                );
            ");

            // Create indexes
            CreateIndexes(db);

            // Create foreign key constraints
            CreateForeignKeys(db);

            // Create triggers
            CreateTriggers(db);

            // Insert initial data
            InsertInitialData(db);
        }

        /// <summary>
        /// Rollback migration - Delete remaining table structures
        /// </summary>
        public static void Down(ISqlSugarClient db)
        {
            db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_stage_version CASCADE");
            db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_workflow_version CASCADE");
            db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_static_field_values CASCADE");
            db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_stage_completion_log CASCADE");
            db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_internal_notes CASCADE");
            db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_operation_change_log CASCADE");
            db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_onboarding_file CASCADE");
        }

        private static void CreateIndexes(ISqlSugarClient db)
        {
            // Create indexes for all tables
            db.Ado.ExecuteCommand(@"
                CREATE INDEX IF NOT EXISTS idx_onboarding_file_onboarding_id ON ff_onboarding_file(onboarding_id);
                CREATE INDEX IF NOT EXISTS idx_onboarding_file_stage_id ON ff_onboarding_file(stage_id);
                CREATE INDEX IF NOT EXISTS idx_operation_change_log_business_id ON ff_operation_change_log(business_id);
                CREATE INDEX IF NOT EXISTS idx_operation_change_log_onboarding_id ON ff_operation_change_log(onboarding_id);
                CREATE INDEX IF NOT EXISTS idx_internal_notes_onboarding_id ON ff_internal_notes(onboarding_id);
                CREATE INDEX IF NOT EXISTS idx_stage_completion_log_onboarding_id ON ff_stage_completion_log(onboarding_id);
                CREATE INDEX IF NOT EXISTS idx_static_field_values_onboarding_id ON ff_static_field_values(onboarding_id);
                CREATE INDEX IF NOT EXISTS idx_workflow_version_original_workflow_id ON ff_workflow_version(original_workflow_id);
                CREATE INDEX IF NOT EXISTS idx_stage_version_workflow_version_id ON ff_stage_version(workflow_version_id);
            ");
        }

        private static void CreateForeignKeys(ISqlSugarClient db)
        {
            // Check and create foreign keys only if they don't exist
            db.Ado.ExecuteCommand(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_onboarding_file_onboarding') THEN
                        ALTER TABLE ff_onboarding_file 
                        ADD CONSTRAINT fk_onboarding_file_onboarding 
                        FOREIGN KEY (onboarding_id) REFERENCES ff_onboarding(id) ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_internal_notes_onboarding') THEN
                        ALTER TABLE ff_internal_notes 
                        ADD CONSTRAINT fk_internal_notes_onboarding 
                        FOREIGN KEY (onboarding_id) REFERENCES ff_onboarding(id) ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_stage_completion_log_onboarding') THEN
                        ALTER TABLE ff_stage_completion_log 
                        ADD CONSTRAINT fk_stage_completion_log_onboarding 
                        FOREIGN KEY (onboarding_id) REFERENCES ff_onboarding(id) ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_static_field_values_onboarding') THEN
                        ALTER TABLE ff_static_field_values 
                        ADD CONSTRAINT fk_static_field_values_onboarding 
                        FOREIGN KEY (onboarding_id) REFERENCES ff_onboarding(id) ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_workflow_version_original_workflow') THEN
                        ALTER TABLE ff_workflow_version 
                        ADD CONSTRAINT fk_workflow_version_original_workflow 
                        FOREIGN KEY (original_workflow_id) REFERENCES ff_workflow(id) ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_stage_version_workflow_version') THEN
                        ALTER TABLE ff_stage_version 
                        ADD CONSTRAINT fk_stage_version_workflow_version 
                        FOREIGN KEY (workflow_version_id) REFERENCES ff_workflow_version(id) ON DELETE CASCADE;
                    END IF;
                END $$;
            ");
        }

        private static void CreateTriggers(ISqlSugarClient db)
        {
            // Create trigger function
            db.Ado.ExecuteCommand(@"
                CREATE OR REPLACE FUNCTION update_modify_date()
                RETURNS TRIGGER AS $$
                BEGIN
                    NEW.modify_date = CURRENT_TIMESTAMP;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ");

            // Create triggers for new tables
            db.Ado.ExecuteCommand(@"
                CREATE TRIGGER tr_onboarding_file_update_modify_date
                    BEFORE UPDATE ON ff_onboarding_file
                    FOR EACH ROW
                    EXECUTE FUNCTION update_modify_date();

                CREATE TRIGGER tr_operation_change_log_update_modify_date
                    BEFORE UPDATE ON ff_operation_change_log
                    FOR EACH ROW
                    EXECUTE FUNCTION update_modify_date();

                CREATE TRIGGER tr_internal_notes_update_modify_date
                    BEFORE UPDATE ON ff_internal_notes
                    FOR EACH ROW
                    EXECUTE FUNCTION update_modify_date();

                CREATE TRIGGER tr_stage_completion_log_update_modify_date
                    BEFORE UPDATE ON ff_stage_completion_log
                    FOR EACH ROW
                    EXECUTE FUNCTION update_modify_date();

                CREATE TRIGGER tr_static_field_values_update_modify_date
                    BEFORE UPDATE ON ff_static_field_values
                    FOR EACH ROW
                    EXECUTE FUNCTION update_modify_date();

                CREATE TRIGGER tr_workflow_version_update_modify_date
                    BEFORE UPDATE ON ff_workflow_version
                    FOR EACH ROW
                    EXECUTE FUNCTION update_modify_date();

                CREATE TRIGGER tr_stage_version_update_modify_date
                    BEFORE UPDATE ON ff_stage_version
                    FOR EACH ROW
                    EXECUTE FUNCTION update_modify_date();
            ");
        }

        private static void InsertInitialData(ISqlSugarClient db)
        {
            // No initial data insertion - let the application handle data creation through proper business flows
        }
    }
}
