-- =============================================
-- FlowFlex Database Migration Script
-- Version: 20241219_AddAppCodeColumn
-- Created: 2024-12-19
-- Description: Add app_code column to all tables for application isolation
-- =============================================

-- Start transaction
BEGIN;

-- Add app_code column to ff_users table
ALTER TABLE ff_users 
ADD COLUMN IF NOT EXISTS app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT';

-- Add app_code column to ff_workflow table
ALTER TABLE ff_workflow 
ADD COLUMN IF NOT EXISTS app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT';

-- Add app_code column to ff_workflow_version table
ALTER TABLE ff_workflow_version 
ADD COLUMN IF NOT EXISTS app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT';

-- Add app_code column to ff_stage table
ALTER TABLE ff_stage 
ADD COLUMN IF NOT EXISTS app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT';

-- Add app_code column to ff_stage_version table
ALTER TABLE ff_stage_version 
ADD COLUMN IF NOT EXISTS app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT';

-- Add app_code column to ff_onboarding table
ALTER TABLE ff_onboarding 
ADD COLUMN IF NOT EXISTS app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT';

-- Add app_code column to ff_questionnaire table
ALTER TABLE ff_questionnaire 
ADD COLUMN IF NOT EXISTS app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT';

-- Add app_code column to ff_questionnaire_section table
ALTER TABLE ff_questionnaire_section 
ADD COLUMN IF NOT EXISTS app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT';

-- Add app_code column to ff_questionnaire_answers table
ALTER TABLE ff_questionnaire_answers 
ADD COLUMN IF NOT EXISTS app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT';

-- Add app_code column to ff_checklist table
ALTER TABLE ff_checklist 
ADD COLUMN IF NOT EXISTS app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT';

-- Add app_code column to ff_checklist_task table
ALTER TABLE ff_checklist_task 
ADD COLUMN IF NOT EXISTS app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT';

-- Add app_code column to ff_checklist_task_completion table
ALTER TABLE ff_checklist_task_completion 
ADD COLUMN IF NOT EXISTS app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT';

-- Add app_code column to ff_internal_notes table
ALTER TABLE ff_internal_notes 
ADD COLUMN IF NOT EXISTS app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT';

-- Add app_code column to ff_onboarding_file table
ALTER TABLE ff_onboarding_file 
ADD COLUMN IF NOT EXISTS app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT';

-- Add app_code column to ff_operation_change_log table
ALTER TABLE ff_operation_change_log 
ADD COLUMN IF NOT EXISTS app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT';

-- Add app_code column to ff_static_field_values table
ALTER TABLE ff_static_field_values 
ADD COLUMN IF NOT EXISTS app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT';

-- Add app_code column to ff_stage_completion_log table
ALTER TABLE ff_stage_completion_log 
ADD COLUMN IF NOT EXISTS app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT';

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_ff_users_app_code_tenant_id ON ff_users(app_code, tenant_id);
CREATE INDEX IF NOT EXISTS idx_ff_workflow_app_code_tenant_id ON ff_workflow(app_code, tenant_id);
CREATE INDEX IF NOT EXISTS idx_ff_workflow_version_app_code_tenant_id ON ff_workflow_version(app_code, tenant_id);
CREATE INDEX IF NOT EXISTS idx_ff_stage_app_code_tenant_id ON ff_stage(app_code, tenant_id);
CREATE INDEX IF NOT EXISTS idx_ff_stage_version_app_code_tenant_id ON ff_stage_version(app_code, tenant_id);
CREATE INDEX IF NOT EXISTS idx_ff_onboarding_app_code_tenant_id ON ff_onboarding(app_code, tenant_id);
CREATE INDEX IF NOT EXISTS idx_ff_questionnaire_app_code_tenant_id ON ff_questionnaire(app_code, tenant_id);
CREATE INDEX IF NOT EXISTS idx_ff_questionnaire_section_app_code_tenant_id ON ff_questionnaire_section(app_code, tenant_id);
CREATE INDEX IF NOT EXISTS idx_ff_questionnaire_answers_app_code_tenant_id ON ff_questionnaire_answers(app_code, tenant_id);
CREATE INDEX IF NOT EXISTS idx_ff_checklist_app_code_tenant_id ON ff_checklist(app_code, tenant_id);
CREATE INDEX IF NOT EXISTS idx_ff_checklist_task_app_code_tenant_id ON ff_checklist_task(app_code, tenant_id);
CREATE INDEX IF NOT EXISTS idx_ff_checklist_task_completion_app_code_tenant_id ON ff_checklist_task_completion(app_code, tenant_id);
CREATE INDEX IF NOT EXISTS idx_ff_internal_notes_app_code_tenant_id ON ff_internal_notes(app_code, tenant_id);
CREATE INDEX IF NOT EXISTS idx_ff_onboarding_file_app_code_tenant_id ON ff_onboarding_file(app_code, tenant_id);
CREATE INDEX IF NOT EXISTS idx_ff_operation_change_log_app_code_tenant_id ON ff_operation_change_log(app_code, tenant_id);
CREATE INDEX IF NOT EXISTS idx_ff_static_field_values_app_code_tenant_id ON ff_static_field_values(app_code, tenant_id);
CREATE INDEX IF NOT EXISTS idx_ff_stage_completion_log_app_code_tenant_id ON ff_stage_completion_log(app_code, tenant_id);

-- Add comments for app_code columns
COMMENT ON COLUMN ff_users.app_code IS 'Application code for application isolation';
COMMENT ON COLUMN ff_workflow.app_code IS 'Application code for application isolation';
COMMENT ON COLUMN ff_workflow_version.app_code IS 'Application code for application isolation';
COMMENT ON COLUMN ff_stage.app_code IS 'Application code for application isolation';
COMMENT ON COLUMN ff_stage_version.app_code IS 'Application code for application isolation';
COMMENT ON COLUMN ff_onboarding.app_code IS 'Application code for application isolation';
COMMENT ON COLUMN ff_questionnaire.app_code IS 'Application code for application isolation';
COMMENT ON COLUMN ff_questionnaire_section.app_code IS 'Application code for application isolation';
COMMENT ON COLUMN ff_questionnaire_answers.app_code IS 'Application code for application isolation';
COMMENT ON COLUMN ff_checklist.app_code IS 'Application code for application isolation';
COMMENT ON COLUMN ff_checklist_task.app_code IS 'Application code for application isolation';
COMMENT ON COLUMN ff_checklist_task_completion.app_code IS 'Application code for application isolation';
COMMENT ON COLUMN ff_internal_notes.app_code IS 'Application code for application isolation';
COMMENT ON COLUMN ff_onboarding_file.app_code IS 'Application code for application isolation';
COMMENT ON COLUMN ff_operation_change_log.app_code IS 'Application code for application isolation';
COMMENT ON COLUMN ff_static_field_values.app_code IS 'Application code for application isolation';
COMMENT ON COLUMN ff_stage_completion_log.app_code IS 'Application code for application isolation';

-- Commit transaction
COMMIT;

-- Log migration completion
SELECT 'Migration 20241219_AddAppCodeColumn completed successfully' AS status; 