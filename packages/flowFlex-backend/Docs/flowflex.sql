-- =============================================
-- FlowFlex Database Schema Script
-- Version: 1.0.0
-- Created: 2024-12-19
-- Database: PostgreSQL 15+
-- Description: FlowFlex Open Source Workflow Management System Database Structure
-- =============================================

-- Set timezone
SET timezone = 'UTC';

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- =============================================
-- 1. Users Table (ff_users)
-- =============================================
CREATE TABLE ff_users (
    id BIGSERIAL NOT NULL PRIMARY KEY,
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

-- =============================================
-- 2. Workflows Table (ff_workflow)
-- =============================================
CREATE TABLE ff_workflow (
    id BIGSERIAL NOT NULL PRIMARY KEY,
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

-- =============================================
-- 3. Workflow Stages Table (ff_stage)
-- =============================================
CREATE TABLE ff_stage (
    id BIGSERIAL NOT NULL PRIMARY KEY,
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

-- =============================================
-- 4. Checklists Table (ff_checklist)
-- =============================================
CREATE TABLE ff_checklist (
    id BIGSERIAL NOT NULL PRIMARY KEY,
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

-- =============================================
-- 5. Checklist Tasks Table (ff_checklist_task)
-- =============================================
CREATE TABLE ff_checklist_task (
    id BIGSERIAL NOT NULL PRIMARY KEY,
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

-- =============================================
-- 6. Checklist Task Completion Table (ff_checklist_task_completion)
-- =============================================
CREATE TABLE ff_checklist_task_completion (
    id BIGSERIAL NOT NULL PRIMARY KEY,
    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
    onboarding_id BIGINT NOT NULL,
    lead_id VARCHAR(100) NOT NULL,
    checklist_id BIGINT NOT NULL,
    task_id BIGINT NOT NULL,
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

-- =============================================
-- 7. Questionnaires Table (ff_questionnaire)
-- =============================================
CREATE TABLE ff_questionnaire (
    id BIGSERIAL NOT NULL PRIMARY KEY,
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

-- =============================================
-- 8. Questionnaire Sections Table (ff_questionnaire_section)
-- =============================================
CREATE TABLE ff_questionnaire_section (
    id BIGSERIAL NOT NULL PRIMARY KEY,
    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
    questionnaire_id BIGINT NOT NULL,
    title VARCHAR(100) NOT NULL,
    description VARCHAR(500),
    order_index INTEGER DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE,
    icon VARCHAR(50),
    color VARCHAR(20),
    is_collapsible BOOLEAN DEFAULT TRUE,
    is_expanded BOOLEAN DEFAULT TRUE,
    is_valid BOOLEAN DEFAULT TRUE,
    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    create_by VARCHAR(50) DEFAULT 'SYSTEM',
    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
    create_user_id BIGINT DEFAULT 0,
    modify_user_id BIGINT DEFAULT 0
);

-- =============================================
-- 9. Questionnaire Answers Table (ff_questionnaire_answers)
-- =============================================
CREATE TABLE ff_questionnaire_answers (
    id BIGSERIAL NOT NULL PRIMARY KEY,
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

-- =============================================
-- 10. Onboarding Management Table (ff_onboarding)
-- =============================================
CREATE TABLE ff_onboarding (
    id BIGSERIAL NOT NULL PRIMARY KEY,
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

-- =============================================
-- 11. Onboarding Files Table (ff_onboarding_file)
-- =============================================
CREATE TABLE ff_onboarding_file (
    id BIGSERIAL NOT NULL PRIMARY KEY,
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

-- =============================================
-- 12. Operation Change Log Table (ff_operation_change_log)
-- =============================================
CREATE TABLE ff_operation_change_log (
    id BIGSERIAL NOT NULL PRIMARY KEY,
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

-- =============================================
-- 13. Internal Notes Table (ff_internal_notes)
-- =============================================
CREATE TABLE ff_internal_notes (
    id BIGSERIAL NOT NULL PRIMARY KEY,
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

-- =============================================
-- 14. Stage Completion Log Table (ff_stage_completion_log)
-- =============================================
CREATE TABLE ff_stage_completion_log (
    id BIGSERIAL NOT NULL PRIMARY KEY,
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

-- =============================================
-- 15. Static Field Values Table (ff_static_field_values)
-- =============================================
CREATE TABLE ff_static_field_values (
    id BIGSERIAL NOT NULL PRIMARY KEY,
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

-- =============================================
-- 16. Workflow Version Table (ff_workflow_version)
-- =============================================
CREATE TABLE ff_workflow_version (
    id BIGSERIAL NOT NULL PRIMARY KEY,
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

-- =============================================
-- 17. Stage Version Table (ff_stage_version)
-- =============================================
CREATE TABLE ff_stage_version (
    id BIGSERIAL NOT NULL PRIMARY KEY,
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

-- =============================================
-- Index Creation
-- =============================================

-- Users table indexes
CREATE UNIQUE INDEX idx_users_email_tenant ON ff_users(email, tenant_id) WHERE is_valid = TRUE;
CREATE INDEX idx_users_tenant_id ON ff_users(tenant_id);
CREATE INDEX idx_users_status ON ff_users(status);

-- Workflow table indexes
CREATE INDEX idx_workflow_tenant_id ON ff_workflow(tenant_id);
CREATE INDEX idx_workflow_status ON ff_workflow(status);
CREATE INDEX idx_workflow_is_active ON ff_workflow(is_active);

-- Stage table indexes
CREATE INDEX idx_stage_workflow_id ON ff_stage(workflow_id);
CREATE INDEX idx_stage_tenant_id ON ff_stage(tenant_id);
CREATE INDEX idx_stage_order_index ON ff_stage(order_index);

-- Checklist table indexes
CREATE INDEX idx_checklist_tenant_id ON ff_checklist(tenant_id);
CREATE INDEX idx_checklist_team ON ff_checklist(team);
CREATE INDEX idx_checklist_workflow_id ON ff_checklist(workflow_id);
CREATE INDEX idx_checklist_stage_id ON ff_checklist(stage_id);

-- Checklist task table indexes
CREATE INDEX idx_checklist_task_checklist_id ON ff_checklist_task(checklist_id);
CREATE INDEX idx_checklist_task_tenant_id ON ff_checklist_task(tenant_id);
CREATE INDEX idx_checklist_task_order_index ON ff_checklist_task(order_index);

-- Checklist task completion table indexes
CREATE INDEX idx_checklist_task_completion_onboarding_id ON ff_checklist_task_completion(onboarding_id);
CREATE INDEX idx_checklist_task_completion_task_id ON ff_checklist_task_completion(task_id);
CREATE INDEX idx_checklist_task_completion_lead_id ON ff_checklist_task_completion(lead_id);

-- Questionnaire table indexes
CREATE INDEX idx_questionnaire_tenant_id ON ff_questionnaire(tenant_id);
CREATE INDEX idx_questionnaire_status ON ff_questionnaire(status);
CREATE INDEX idx_questionnaire_workflow_id ON ff_questionnaire(workflow_id);
CREATE INDEX idx_questionnaire_stage_id ON ff_questionnaire(stage_id);

-- Questionnaire section table indexes
CREATE INDEX idx_questionnaire_section_questionnaire_id ON ff_questionnaire_section(questionnaire_id);
CREATE INDEX idx_questionnaire_section_tenant_id ON ff_questionnaire_section(tenant_id);

-- Questionnaire answers table indexes
CREATE INDEX idx_questionnaire_answers_onboarding_id ON ff_questionnaire_answers(onboarding_id);
CREATE INDEX idx_questionnaire_answers_stage_id ON ff_questionnaire_answers(stage_id);
CREATE INDEX idx_questionnaire_answers_questionnaire_id ON ff_questionnaire_answers(questionnaire_id);

-- Onboarding management table indexes
CREATE INDEX idx_onboarding_tenant_id ON ff_onboarding(tenant_id);
CREATE INDEX idx_onboarding_lead_email ON ff_onboarding(lead_email);
CREATE INDEX idx_onboarding_lead_id ON ff_onboarding(lead_id);
CREATE INDEX idx_onboarding_workflow_id ON ff_onboarding(workflow_id);
CREATE INDEX idx_onboarding_current_stage_id ON ff_onboarding(current_stage_id);
CREATE INDEX idx_onboarding_status ON ff_onboarding(status);

-- Onboarding file table indexes
CREATE INDEX idx_onboarding_file_onboarding_id ON ff_onboarding_file(onboarding_id);
CREATE INDEX idx_onboarding_file_stage_id ON ff_onboarding_file(stage_id);
CREATE INDEX idx_onboarding_file_attachment_id ON ff_onboarding_file(attachment_id);

-- Operation change log table indexes
CREATE INDEX idx_operation_change_log_business_id ON ff_operation_change_log(business_id);
CREATE INDEX idx_operation_change_log_onboarding_id ON ff_operation_change_log(onboarding_id);
CREATE INDEX idx_operation_change_log_operation_time ON ff_operation_change_log(operation_time);
CREATE INDEX idx_operation_change_log_operator_id ON ff_operation_change_log(operator_id);

-- Internal notes table indexes
CREATE INDEX idx_internal_notes_onboarding_id ON ff_internal_notes(onboarding_id);
CREATE INDEX idx_internal_notes_stage_id ON ff_internal_notes(stage_id);
CREATE INDEX idx_internal_notes_author_id ON ff_internal_notes(author_id);

-- Stage completion log table indexes
CREATE INDEX idx_stage_completion_log_onboarding_id ON ff_stage_completion_log(onboarding_id);
CREATE INDEX idx_stage_completion_log_stage_id ON ff_stage_completion_log(stage_id);
CREATE INDEX idx_stage_completion_log_log_type ON ff_stage_completion_log(log_type);

-- Static field values table indexes
CREATE INDEX idx_static_field_values_onboarding_id ON ff_static_field_values(onboarding_id);
CREATE INDEX idx_static_field_values_stage_id ON ff_static_field_values(stage_id);
CREATE INDEX idx_static_field_values_field_name ON ff_static_field_values(field_name);

-- Workflow version table indexes
CREATE INDEX idx_workflow_version_original_workflow_id ON ff_workflow_version(original_workflow_id);
CREATE INDEX idx_workflow_version_tenant_id ON ff_workflow_version(tenant_id);
CREATE INDEX idx_workflow_version_version ON ff_workflow_version(version);

-- Stage version table indexes
CREATE INDEX idx_stage_version_workflow_version_id ON ff_stage_version(workflow_version_id);
CREATE INDEX idx_stage_version_original_stage_id ON ff_stage_version(original_stage_id);
CREATE INDEX idx_stage_version_tenant_id ON ff_stage_version(tenant_id);
CREATE INDEX idx_stage_version_order_index ON ff_stage_version(order_index);

-- =============================================
-- Foreign Key Constraints
-- =============================================

-- Stage -> Workflow
ALTER TABLE ff_stage 
ADD CONSTRAINT fk_stage_workflow 
FOREIGN KEY (workflow_id) REFERENCES ff_workflow(id) ON DELETE CASCADE;

-- Checklist Task -> Checklist
ALTER TABLE ff_checklist_task 
ADD CONSTRAINT fk_checklist_task_checklist 
FOREIGN KEY (checklist_id) REFERENCES ff_checklist(id) ON DELETE CASCADE;

-- Checklist Task Completion -> Onboarding
ALTER TABLE ff_checklist_task_completion 
ADD CONSTRAINT fk_checklist_task_completion_onboarding 
FOREIGN KEY (onboarding_id) REFERENCES ff_onboarding(id) ON DELETE CASCADE;

-- Checklist Task Completion -> Checklist Task
ALTER TABLE ff_checklist_task_completion 
ADD CONSTRAINT fk_checklist_task_completion_task 
FOREIGN KEY (task_id) REFERENCES ff_checklist_task(id) ON DELETE CASCADE;

-- Questionnaire Section -> Questionnaire
ALTER TABLE ff_questionnaire_section 
ADD CONSTRAINT fk_questionnaire_section_questionnaire 
FOREIGN KEY (questionnaire_id) REFERENCES ff_questionnaire(id) ON DELETE CASCADE;

-- Questionnaire Answers -> Onboarding
ALTER TABLE ff_questionnaire_answers 
ADD CONSTRAINT fk_questionnaire_answers_onboarding 
FOREIGN KEY (onboarding_id) REFERENCES ff_onboarding(id) ON DELETE CASCADE;

-- Questionnaire Answers -> Stage
ALTER TABLE ff_questionnaire_answers 
ADD CONSTRAINT fk_questionnaire_answers_stage 
FOREIGN KEY (stage_id) REFERENCES ff_stage(id) ON DELETE CASCADE;

-- Onboarding -> Workflow
ALTER TABLE ff_onboarding 
ADD CONSTRAINT fk_onboarding_workflow 
FOREIGN KEY (workflow_id) REFERENCES ff_workflow(id) ON DELETE CASCADE;

-- Onboarding -> Current Stage
ALTER TABLE ff_onboarding 
ADD CONSTRAINT fk_onboarding_current_stage 
FOREIGN KEY (current_stage_id) REFERENCES ff_stage(id) ON DELETE SET NULL;

-- Onboarding File -> Onboarding
ALTER TABLE ff_onboarding_file 
ADD CONSTRAINT fk_onboarding_file_onboarding 
FOREIGN KEY (onboarding_id) REFERENCES ff_onboarding(id) ON DELETE CASCADE;

-- Onboarding File -> Stage
ALTER TABLE ff_onboarding_file 
ADD CONSTRAINT fk_onboarding_file_stage 
FOREIGN KEY (stage_id) REFERENCES ff_stage(id) ON DELETE SET NULL;

-- Operation Change Log -> Onboarding
ALTER TABLE ff_operation_change_log 
ADD CONSTRAINT fk_operation_change_log_onboarding 
FOREIGN KEY (onboarding_id) REFERENCES ff_onboarding(id) ON DELETE SET NULL;

-- Internal Notes -> Onboarding
ALTER TABLE ff_internal_notes 
ADD CONSTRAINT fk_internal_notes_onboarding 
FOREIGN KEY (onboarding_id) REFERENCES ff_onboarding(id) ON DELETE CASCADE;

-- Internal Notes -> Stage
ALTER TABLE ff_internal_notes 
ADD CONSTRAINT fk_internal_notes_stage 
FOREIGN KEY (stage_id) REFERENCES ff_stage(id) ON DELETE SET NULL;

-- Stage Completion Log -> Onboarding
ALTER TABLE ff_stage_completion_log 
ADD CONSTRAINT fk_stage_completion_log_onboarding 
FOREIGN KEY (onboarding_id) REFERENCES ff_onboarding(id) ON DELETE CASCADE;

-- Stage Completion Log -> Stage
ALTER TABLE ff_stage_completion_log 
ADD CONSTRAINT fk_stage_completion_log_stage 
FOREIGN KEY (stage_id) REFERENCES ff_stage(id) ON DELETE CASCADE;

-- Static Field Values -> Onboarding
ALTER TABLE ff_static_field_values 
ADD CONSTRAINT fk_static_field_values_onboarding 
FOREIGN KEY (onboarding_id) REFERENCES ff_onboarding(id) ON DELETE CASCADE;

-- Static Field Values -> Stage
ALTER TABLE ff_static_field_values 
ADD CONSTRAINT fk_static_field_values_stage 
FOREIGN KEY (stage_id) REFERENCES ff_stage(id) ON DELETE CASCADE;

-- Workflow Version -> Workflow
ALTER TABLE ff_workflow_version 
ADD CONSTRAINT fk_workflow_version_original_workflow 
FOREIGN KEY (original_workflow_id) REFERENCES ff_workflow(id) ON DELETE CASCADE;

-- Stage Version -> Workflow Version
ALTER TABLE ff_stage_version 
ADD CONSTRAINT fk_stage_version_workflow_version 
FOREIGN KEY (workflow_version_id) REFERENCES ff_workflow_version(id) ON DELETE CASCADE;

-- Stage Version -> Stage
ALTER TABLE ff_stage_version 
ADD CONSTRAINT fk_stage_version_original_stage 
FOREIGN KEY (original_stage_id) REFERENCES ff_stage(id) ON DELETE CASCADE;

-- =============================================
-- Triggers: Auto-update modification time
-- =============================================

-- Create function to update modification time
CREATE OR REPLACE FUNCTION update_modify_date()
RETURNS TRIGGER AS $$
BEGIN
    NEW.modify_date = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create triggers for all tables
CREATE TRIGGER tr_users_update_modify_date
    BEFORE UPDATE ON ff_users
    FOR EACH ROW
    EXECUTE FUNCTION update_modify_date();

CREATE TRIGGER tr_workflow_update_modify_date
    BEFORE UPDATE ON ff_workflow
    FOR EACH ROW
    EXECUTE FUNCTION update_modify_date();

CREATE TRIGGER tr_stage_update_modify_date
    BEFORE UPDATE ON ff_stage
    FOR EACH ROW
    EXECUTE FUNCTION update_modify_date();

CREATE TRIGGER tr_checklist_update_modify_date
    BEFORE UPDATE ON ff_checklist
    FOR EACH ROW
    EXECUTE FUNCTION update_modify_date();

CREATE TRIGGER tr_checklist_task_update_modify_date
    BEFORE UPDATE ON ff_checklist_task
    FOR EACH ROW
    EXECUTE FUNCTION update_modify_date();

CREATE TRIGGER tr_checklist_task_completion_update_modify_date
    BEFORE UPDATE ON ff_checklist_task_completion
    FOR EACH ROW
    EXECUTE FUNCTION update_modify_date();

CREATE TRIGGER tr_questionnaire_update_modify_date
    BEFORE UPDATE ON ff_questionnaire
    FOR EACH ROW
    EXECUTE FUNCTION update_modify_date();

CREATE TRIGGER tr_questionnaire_section_update_modify_date
    BEFORE UPDATE ON ff_questionnaire_section
    FOR EACH ROW
    EXECUTE FUNCTION update_modify_date();

CREATE TRIGGER tr_questionnaire_answers_update_modify_date
    BEFORE UPDATE ON ff_questionnaire_answers
    FOR EACH ROW
    EXECUTE FUNCTION update_modify_date();

CREATE TRIGGER tr_onboarding_update_modify_date
    BEFORE UPDATE ON ff_onboarding
    FOR EACH ROW
    EXECUTE FUNCTION update_modify_date();

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

-- =============================================
-- Initial Data
-- =============================================

-- Insert admin users
INSERT INTO ff_users (tenant_id, email, username, password_hash, email_verified, status, create_by, modify_by) VALUES
('default', 'admin@flowflex.com', 'admin@flowflex.com', '$2a$11$dummy.hash.for.initial.setup', TRUE, 'active', 'SYSTEM', 'SYSTEM'),
('default', 'demo@flowflex.com', 'demo@flowflex.com', '$2a$11$dummy.hash.for.demo.user', TRUE, 'active', 'SYSTEM', 'SYSTEM')
ON CONFLICT DO NOTHING;

-- Insert sample workflows
INSERT INTO ff_workflow (tenant_id, name, description, status, version, is_active) VALUES
('default', 'Employee Onboarding Process', 'Standard employee onboarding workflow', 'active', 1, TRUE),
('default', 'Customer Survey Process', 'Customer satisfaction survey workflow', 'active', 1, TRUE)
ON CONFLICT DO NOTHING;

-- Insert sample stages
INSERT INTO ff_stage (tenant_id, workflow_id, name, description, order_index, estimated_duration, is_active) VALUES
('default', 1, 'Preparation Stage', 'Prepare onboarding materials and equipment', 1, 1, TRUE),
('default', 1, 'Onboarding Training', 'Company introduction and job training', 2, 3, TRUE),
('default', 1, 'System Setup', 'Setup system accounts and permissions', 3, 1, TRUE)
ON CONFLICT DO NOTHING;

-- Insert sample checklists
INSERT INTO ff_checklist (tenant_id, name, description, team, is_template, is_active) VALUES
('default', 'Onboarding Checklist', 'New employee onboarding checklist items', 'HR', TRUE, TRUE),
('default', 'Project Delivery Checklist', 'Quality checklist items before project delivery', 'Tech', TRUE, TRUE)
ON CONFLICT DO NOTHING;

-- Insert sample questionnaires
INSERT INTO ff_questionnaire (tenant_id, name, description, status, is_active) VALUES
('default', 'Employee Satisfaction Survey', 'Employee satisfaction and feedback survey', 'Published', TRUE),
('default', 'Customer Service Quality Survey', 'Customer service quality feedback collection', 'Published', TRUE)
ON CONFLICT DO NOTHING;

-- Display created tables
SELECT 
    schemaname,
    tablename,
    tableowner
FROM pg_tables 
WHERE schemaname = 'public' 
  AND tablename LIKE 'ff_%'
ORDER BY tablename;

-- Completion message
SELECT 'FlowFlex database initialization completed successfully! All tables created with ff_ prefix.' as message; 
 
