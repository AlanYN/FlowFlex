using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// FlowFlex Database Initial Migration
    /// Version: 2.0.0
    /// Created: 2025-01-01
    /// Description: Create all core table structures
    /// </summary>
    public class InitialCreate_20250101000000
    {
        /// <summary>
        /// Execute migration - Create table structures
        /// </summary>
        public static void Up(ISqlSugarClient db)
        {
            // Set timezone
            db.Ado.ExecuteCommand("SET timezone = 'UTC'");

            // Create extensions
            db.Ado.ExecuteCommand("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\"");

            // 1. Create users table
            CreateUsersTable(db);

            // 2. Create workflow table
            CreateWorkflowTable(db);

            // 3. Create stage table
            CreateStageTable(db);

            // 4. Create checklist table
            CreateChecklistTable(db);

            // 5. Create checklist task table
            CreateChecklistTaskTable(db);

            // 6. Create checklist task completion table
            CreateChecklistTaskCompletionTable(db);

            // 7. Create questionnaire table
            CreateQuestionnaireTable(db);

            // 8. (removed) questionnaire section table was deprecated and removed in later migration

            // 9. Create questionnaire answers table
            CreateQuestionnaireAnswersTable(db);

            // 10. Create onboarding table
            CreateOnboardingTable(db);
        }

        /// <summary>
        /// Rollback migration - Drop table structures
        /// </summary>
        public static void Down(ISqlSugarClient db)
        {
            // Drop tables (in reverse dependency order)
            db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_onboarding CASCADE");
            db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_questionnaire_answers CASCADE");
            // ff_questionnaire_section was removed by migration 20250801000002
            db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_questionnaire CASCADE");
            db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_checklist_task_completion CASCADE");
            db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_checklist_task CASCADE");
            db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_checklist CASCADE");
            db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_stage CASCADE");
            db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_workflow CASCADE");
            db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_users CASCADE");

            // Drop functions
            db.Ado.ExecuteCommand("DROP FUNCTION IF EXISTS update_modify_date()");
        }

        private static void CreateUsersTable(ISqlSugarClient db)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS ff_users (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    email VARCHAR(100) NOT NULL,
                    username VARCHAR(50) NOT NULL,
                    password_hash VARCHAR(255),
                    email_verified BOOLEAN DEFAULT FALSE,
                    email_verification_code VARCHAR(6),
                    verification_code_expiry TIMESTAMPTZ,
                    status VARCHAR(20) DEFAULT 'active',
                    last_login_date TIMESTAMPTZ,
                    last_login_ip VARCHAR(45),
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0
                );
                
                CREATE UNIQUE INDEX idx_users_email_tenant ON ff_users(email, tenant_id) WHERE is_valid = TRUE;
                CREATE INDEX idx_users_tenant_id ON ff_users(tenant_id);
                CREATE INDEX idx_users_status ON ff_users(status);
            ";

            db.Ado.ExecuteCommand(sql);
        }

        private static void CreateWorkflowTable(ISqlSugarClient db)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS ff_workflow (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    name VARCHAR(100) NOT NULL,
                    description VARCHAR(500),
                    is_default BOOLEAN DEFAULT FALSE,
                    status VARCHAR(20) DEFAULT 'active',
                    start_date TIMESTAMPTZ,
                    end_date TIMESTAMPTZ,
                    version INTEGER DEFAULT 1,
                    is_active BOOLEAN DEFAULT TRUE,
                    config_json VARCHAR(2000),
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0
                );
                
                CREATE INDEX idx_workflow_tenant_id ON ff_workflow(tenant_id);
                CREATE INDEX idx_workflow_status ON ff_workflow(status);
                CREATE INDEX idx_workflow_is_active ON ff_workflow(is_active);
            ";

            db.Ado.ExecuteCommand(sql);
        }

        private static void CreateStageTable(ISqlSugarClient db)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS ff_stage (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    workflow_id BIGINT NOT NULL,
                    name VARCHAR(100) NOT NULL,
                    portal_name VARCHAR(100),
                    internal_name VARCHAR(100),
                    description VARCHAR(500),
                    default_assigned_group VARCHAR(100),
                    default_assignee VARCHAR(100),
                    estimated_duration DECIMAL,
                    order_index INTEGER NOT NULL,
                    checklist_id BIGINT,
                    questionnaire_id BIGINT,
                    color VARCHAR(20),
                    required_fields_json TEXT,
                    static_fields_json TEXT,
                    workflow_version VARCHAR(32),
                    is_active BOOLEAN DEFAULT TRUE,
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0
                );
                
                CREATE INDEX IF NOT EXISTS idx_stage_workflow_id ON ff_stage(workflow_id);
                CREATE INDEX IF NOT EXISTS idx_stage_tenant_id ON ff_stage(tenant_id);
                CREATE INDEX IF NOT EXISTS idx_stage_order_index ON ff_stage(order_index);
                
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_stage_workflow') THEN
                        ALTER TABLE ff_stage 
                        ADD CONSTRAINT fk_stage_workflow 
                        FOREIGN KEY (workflow_id) REFERENCES ff_workflow(id) ON DELETE CASCADE;
                    END IF;
                END $$;
            ";

            db.Ado.ExecuteCommand(sql);
        }

        private static void CreateChecklistTable(ISqlSugarClient db)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS ff_checklist (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    name VARCHAR(100) NOT NULL,
                    description VARCHAR(500),
                    team VARCHAR(100),
                    type VARCHAR(20) DEFAULT 'Template',
                    status VARCHAR(20) DEFAULT 'Active',
                    is_template BOOLEAN DEFAULT TRUE,
                    template_id BIGINT,
                    completion_rate DECIMAL DEFAULT 0,
                    total_tasks INTEGER DEFAULT 0,
                    completed_tasks INTEGER DEFAULT 0,
                    estimated_hours INTEGER DEFAULT 0,
                    is_active BOOLEAN DEFAULT TRUE,
                    workflow_id BIGINT,
                    stage_id BIGINT,
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0
                );
                
                CREATE INDEX idx_checklist_tenant_id ON ff_checklist(tenant_id);
                CREATE INDEX idx_checklist_team ON ff_checklist(team);
                CREATE INDEX idx_checklist_workflow_id ON ff_checklist(workflow_id);
                CREATE INDEX idx_checklist_stage_id ON ff_checklist(stage_id);
            ";

            db.Ado.ExecuteCommand(sql);
        }

        private static void CreateChecklistTaskTable(ISqlSugarClient db)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS ff_checklist_task (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    checklist_id BIGINT NOT NULL,
                    name VARCHAR(200) NOT NULL,
                    description VARCHAR(1000),
                    task_type VARCHAR(20) DEFAULT 'Manual',
                    is_completed BOOLEAN DEFAULT FALSE,
                    is_required BOOLEAN DEFAULT FALSE,
                    assignee_id BIGINT,
                    assignee_name VARCHAR(100),
                    assigned_team VARCHAR(100),
                    priority VARCHAR(20) DEFAULT 'Medium',
                    order_index INTEGER NOT NULL,
                    estimated_hours INTEGER DEFAULT 0,
                    actual_hours INTEGER DEFAULT 0,
                    due_date TIMESTAMPTZ,
                    completed_date TIMESTAMPTZ,
                    completion_notes VARCHAR(500),
                    depends_on_task_id BIGINT,
                    attachments_json TEXT,
                    status VARCHAR(20) DEFAULT 'Pending',
                    is_active BOOLEAN DEFAULT TRUE,
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0
                );
                
                CREATE INDEX IF NOT EXISTS idx_checklist_task_checklist_id ON ff_checklist_task(checklist_id);
                CREATE INDEX IF NOT EXISTS idx_checklist_task_tenant_id ON ff_checklist_task(tenant_id);
                CREATE INDEX IF NOT EXISTS idx_checklist_task_order_index ON ff_checklist_task(order_index);
                
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_checklist_task_checklist') THEN
                        ALTER TABLE ff_checklist_task 
                        ADD CONSTRAINT fk_checklist_task_checklist 
                        FOREIGN KEY (checklist_id) REFERENCES ff_checklist(id) ON DELETE CASCADE;
                    END IF;
                END $$;
            ";

            db.Ado.ExecuteCommand(sql);
        }

        private static void CreateChecklistTaskCompletionTable(ISqlSugarClient db)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS ff_checklist_task_completion (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    onboarding_id BIGINT NOT NULL,
                    lead_id VARCHAR(100) NOT NULL,
                    checklist_id BIGINT NOT NULL,
                    task_id BIGINT NOT NULL,
                    stage_id BIGINT NULL,
                    is_completed BOOLEAN DEFAULT FALSE,
                    completed_time TIMESTAMPTZ,
                    completion_notes VARCHAR(500) DEFAULT '',
                    source VARCHAR(50) DEFAULT 'customer_portal',
                    ip_address VARCHAR(50) DEFAULT '',
                    user_agent VARCHAR(500) DEFAULT '',
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0
                );
                
                CREATE INDEX idx_checklist_task_completion_onboarding_id ON ff_checklist_task_completion(onboarding_id);
                CREATE INDEX idx_checklist_task_completion_task_id ON ff_checklist_task_completion(task_id);
                CREATE INDEX idx_checklist_task_completion_lead_id ON ff_checklist_task_completion(lead_id);
                CREATE INDEX idx_checklist_task_completion_stage_id ON ff_checklist_task_completion(stage_id);
                CREATE INDEX idx_checklist_task_completion_onboarding_stage ON ff_checklist_task_completion(onboarding_id, stage_id);
            ";

            db.Ado.ExecuteCommand(sql);
        }

        private static void CreateQuestionnaireTable(ISqlSugarClient db)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS ff_questionnaire (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    name VARCHAR(100) NOT NULL,
                    description VARCHAR(500),
                    type VARCHAR(20) DEFAULT 'Template',
                    status VARCHAR(20) DEFAULT 'Draft',
                    structure_json TEXT,
                    version INTEGER DEFAULT 1,
                    is_template BOOLEAN DEFAULT TRUE,
                    template_id BIGINT,
                    preview_image_url VARCHAR(500),
                    category VARCHAR(50),
                    tags_json TEXT,
                    estimated_minutes INTEGER DEFAULT 0,
                    total_questions INTEGER DEFAULT 0,
                    required_questions INTEGER DEFAULT 0,
                    allow_draft BOOLEAN DEFAULT TRUE,
                    allow_multiple_submissions BOOLEAN DEFAULT FALSE,
                    is_active BOOLEAN DEFAULT TRUE,
                    workflow_id BIGINT,
                    stage_id BIGINT,
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0
                );
                
                CREATE INDEX idx_questionnaire_tenant_id ON ff_questionnaire(tenant_id);
                CREATE INDEX idx_questionnaire_status ON ff_questionnaire(status);
                CREATE INDEX idx_questionnaire_workflow_id ON ff_questionnaire(workflow_id);
                CREATE INDEX idx_questionnaire_stage_id ON ff_questionnaire(stage_id);
            ";

            db.Ado.ExecuteCommand(sql);
        }

        // Removed CreateQuestionnaireSectionTable

        private static void CreateQuestionnaireAnswersTable(ISqlSugarClient db)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS ff_questionnaire_answers (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    onboarding_id BIGINT NOT NULL,
                    stage_id BIGINT NOT NULL,
                    questionnaire_id BIGINT,
                    answer_json TEXT NOT NULL DEFAULT '',
                    status VARCHAR(20) DEFAULT 'Draft',
                    completion_rate INTEGER DEFAULT 0,
                    submit_time TIMESTAMPTZ,
                    review_time TIMESTAMPTZ,
                    reviewer_id BIGINT,
                    review_notes VARCHAR(1000) DEFAULT '',
                    version INTEGER DEFAULT 1,
                    is_latest BOOLEAN DEFAULT TRUE,
                    source VARCHAR(50) DEFAULT 'customer_portal',
                    ip_address VARCHAR(45) DEFAULT '',
                    user_agent VARCHAR(500) DEFAULT '',
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0
                );
                
                CREATE INDEX idx_questionnaire_answers_onboarding_id ON ff_questionnaire_answers(onboarding_id);
                CREATE INDEX idx_questionnaire_answers_stage_id ON ff_questionnaire_answers(stage_id);
                CREATE INDEX idx_questionnaire_answers_questionnaire_id ON ff_questionnaire_answers(questionnaire_id);
            ";

            db.Ado.ExecuteCommand(sql);
        }

        private static void CreateOnboardingTable(ISqlSugarClient db)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS ff_onboarding (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    workflow_id BIGINT NOT NULL,
                    current_stage_id BIGINT,
                    current_stage_order INTEGER DEFAULT 1,
                    lead_id VARCHAR(100) NOT NULL,
                    lead_name VARCHAR(200),
                    lead_email VARCHAR(200),
                    lead_phone VARCHAR(50),
                    contact_person VARCHAR(200),
                    contact_email VARCHAR(200),
                    life_cycle_stage_id BIGINT,
                    life_cycle_stage_name VARCHAR(100),
                    status VARCHAR(20) DEFAULT 'Started',
                    completion_rate DECIMAL DEFAULT 0,
                    start_date TIMESTAMPTZ,
                    estimated_completion_date TIMESTAMPTZ,
                    actual_completion_date TIMESTAMPTZ,
                    current_assignee_id BIGINT,
                    current_assignee_name VARCHAR(100),
                    current_team VARCHAR(100),
                    stage_updated_by_id BIGINT,
                    stage_updated_by VARCHAR(100),
                    stage_updated_by_email VARCHAR(200),
                    stage_updated_time TIMESTAMPTZ,
                    current_stage_start_time TIMESTAMPTZ,
                    priority VARCHAR(20) DEFAULT 'Medium',
                    is_priority_set BOOLEAN DEFAULT FALSE,
                    custom_fields_json TEXT,
                    notes VARCHAR(1000),
                    is_active BOOLEAN DEFAULT TRUE,
                    stages_progress_json TEXT,
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0
                );
                
                CREATE INDEX IF NOT EXISTS idx_onboarding_tenant_id ON ff_onboarding(tenant_id);
                CREATE INDEX IF NOT EXISTS idx_onboarding_lead_email ON ff_onboarding(lead_email);
                CREATE INDEX IF NOT EXISTS idx_onboarding_lead_id ON ff_onboarding(lead_id);
                CREATE INDEX IF NOT EXISTS idx_onboarding_workflow_id ON ff_onboarding(workflow_id);
                CREATE INDEX IF NOT EXISTS idx_onboarding_current_stage_id ON ff_onboarding(current_stage_id);
                CREATE INDEX IF NOT EXISTS idx_onboarding_status ON ff_onboarding(status);
                
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_onboarding_workflow') THEN
                        ALTER TABLE ff_onboarding 
                        ADD CONSTRAINT fk_onboarding_workflow 
                        FOREIGN KEY (workflow_id) REFERENCES ff_workflow(id) ON DELETE CASCADE;
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_onboarding_current_stage') THEN
                        ALTER TABLE ff_onboarding 
                        ADD CONSTRAINT fk_onboarding_current_stage 
                        FOREIGN KEY (current_stage_id) REFERENCES ff_stage(id) ON DELETE SET NULL;
                    END IF;
                END $$;
            ";

            db.Ado.ExecuteCommand(sql);
        }
    }
}
