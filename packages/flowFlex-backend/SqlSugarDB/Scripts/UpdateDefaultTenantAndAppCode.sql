-- =============================================
-- FlowFlex Database Update Script
-- Purpose: Update all tables to set tenant_id and app_code to 'DEFAULT'
-- Version: 1.0.0
-- Created: 2025-01-19
-- Database: PostgreSQL 15+
-- =============================================

-- Set timezone
SET timezone = 'UTC';

-- Begin transaction for safety
BEGIN;

-- =============================================
-- Update tenant_id and app_code to 'DEFAULT' for all tables
-- =============================================

-- Users table
UPDATE ff_users 
SET 
    tenant_id = 'DEFAULT',
    app_code = 'DEFAULT',
    modify_date = CURRENT_TIMESTAMP,
    modify_by = 'SYSTEM_UPDATE'
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- Workflows table
UPDATE ff_workflow 
SET 
    tenant_id = 'DEFAULT',
    app_code = 'DEFAULT',
    modify_date = CURRENT_TIMESTAMP,
    modify_by = 'SYSTEM_UPDATE'
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- Workflow versions table
UPDATE ff_workflow_version 
SET 
    tenant_id = 'DEFAULT',
    app_code = 'DEFAULT',
    modify_date = CURRENT_TIMESTAMP,
    modify_by = 'SYSTEM_UPDATE'
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- Stages table
UPDATE ff_stage 
SET 
    tenant_id = 'DEFAULT',
    app_code = 'DEFAULT',
    modify_date = CURRENT_TIMESTAMP,
    modify_by = 'SYSTEM_UPDATE'
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- Stage versions table
UPDATE ff_stage_version 
SET 
    tenant_id = 'DEFAULT',
    app_code = 'DEFAULT',
    modify_date = CURRENT_TIMESTAMP,
    modify_by = 'SYSTEM_UPDATE'
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- Onboarding table
UPDATE ff_onboarding 
SET 
    tenant_id = 'DEFAULT',
    app_code = 'DEFAULT',
    modify_date = CURRENT_TIMESTAMP,
    modify_by = 'SYSTEM_UPDATE'
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- Questionnaires table
UPDATE ff_questionnaire 
SET 
    tenant_id = 'DEFAULT',
    app_code = 'DEFAULT',
    modify_date = CURRENT_TIMESTAMP,
    modify_by = 'SYSTEM_UPDATE'
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- Questionnaire sections table
UPDATE ff_questionnaire_section 
SET 
    tenant_id = 'DEFAULT',
    app_code = 'DEFAULT',
    modify_date = CURRENT_TIMESTAMP,
    modify_by = 'SYSTEM_UPDATE'
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- Questionnaire answers table
UPDATE ff_questionnaire_answers 
SET 
    tenant_id = 'DEFAULT',
    app_code = 'DEFAULT',
    modify_date = CURRENT_TIMESTAMP,
    modify_by = 'SYSTEM_UPDATE'
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- Checklists table
UPDATE ff_checklist 
SET 
    tenant_id = 'DEFAULT',
    app_code = 'DEFAULT',
    modify_date = CURRENT_TIMESTAMP,
    modify_by = 'SYSTEM_UPDATE'
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- Checklist tasks table
UPDATE ff_checklist_task 
SET 
    tenant_id = 'DEFAULT',
    app_code = 'DEFAULT',
    modify_date = CURRENT_TIMESTAMP,
    modify_by = 'SYSTEM_UPDATE'
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- Checklist task completion table
UPDATE ff_checklist_task_completion 
SET 
    tenant_id = 'DEFAULT',
    app_code = 'DEFAULT',
    modify_date = CURRENT_TIMESTAMP,
    modify_by = 'SYSTEM_UPDATE'
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- Internal notes table
UPDATE ff_internal_notes 
SET 
    tenant_id = 'DEFAULT',
    app_code = 'DEFAULT',
    modify_date = CURRENT_TIMESTAMP,
    modify_by = 'SYSTEM_UPDATE'
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- Onboarding files table
UPDATE ff_onboarding_file 
SET 
    tenant_id = 'DEFAULT',
    app_code = 'DEFAULT',
    modify_date = CURRENT_TIMESTAMP,
    modify_by = 'SYSTEM_UPDATE'
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- Operation change log table
UPDATE ff_operation_change_log 
SET 
    tenant_id = 'DEFAULT',
    app_code = 'DEFAULT',
    modify_date = CURRENT_TIMESTAMP,
    modify_by = 'SYSTEM_UPDATE'
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- Static field values table
UPDATE ff_static_field_values 
SET 
    tenant_id = 'DEFAULT',
    app_code = 'DEFAULT',
    modify_date = CURRENT_TIMESTAMP,
    modify_by = 'SYSTEM_UPDATE'
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- Stage completion log table
UPDATE ff_stage_completion_log 
SET 
    tenant_id = 'DEFAULT',
    app_code = 'DEFAULT',
    modify_date = CURRENT_TIMESTAMP,
    modify_by = 'SYSTEM_UPDATE'
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- Events table
UPDATE ff_events 
SET 
    app_code = 'DEFAULT',
    modify_date = CURRENT_TIMESTAMP,
    modify_by = 'SYSTEM_UPDATE'
WHERE app_code != 'DEFAULT' OR app_code IS NULL;

-- Access tokens table
UPDATE ff_access_tokens 
SET 
    tenant_id = 'DEFAULT',
    app_code = 'DEFAULT',
    modify_date = CURRENT_TIMESTAMP,
    modify_by = 'SYSTEM_UPDATE'
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- =============================================
-- Display update summary
-- =============================================

-- Show count of updated records by table
DO $$
DECLARE
    rec RECORD;
    table_name TEXT;
    count_result INTEGER;
BEGIN
    RAISE NOTICE '=== Update Summary ===';
    
    FOR table_name IN 
        SELECT unnest(ARRAY[
            'ff_users', 'ff_workflow', 'ff_workflow_version', 'ff_stage', 'ff_stage_version',
            'ff_onboarding', 'ff_questionnaire', 'ff_questionnaire_section', 'ff_questionnaire_answers',
            'ff_checklist', 'ff_checklist_task', 'ff_checklist_task_completion', 'ff_internal_notes',
            'ff_onboarding_file', 'ff_operation_change_log', 'ff_static_field_values', 'ff_stage_completion_log',
            'ff_events', 'ff_access_tokens'
        ])
    LOOP
        -- Check if table exists
        IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = table_name AND table_schema = 'public') THEN
            -- Count total records in table
            EXECUTE format('SELECT COUNT(*) FROM %I WHERE is_valid = TRUE OR is_valid IS NULL', table_name) INTO count_result;
            RAISE NOTICE 'Table %: % total records', table_name, count_result;
        ELSE
            RAISE NOTICE 'Table % does not exist, skipped', table_name;
        END IF;
    END LOOP;
    
    RAISE NOTICE '=== Update Complete ===';
END $$;

-- Commit transaction
COMMIT;

-- =============================================
-- Verification queries (optional)
-- =============================================

-- Uncomment the following queries to verify the updates

/*
-- Check if any records still have non-DEFAULT values
SELECT 'ff_users' as table_name, COUNT(*) as non_default_count 
FROM ff_users 
WHERE (tenant_id != 'DEFAULT' OR app_code != 'DEFAULT') AND is_valid = TRUE

UNION ALL

SELECT 'ff_workflow' as table_name, COUNT(*) as non_default_count 
FROM ff_workflow 
WHERE (tenant_id != 'DEFAULT' OR app_code != 'DEFAULT') AND is_valid = TRUE

UNION ALL

SELECT 'ff_onboarding' as table_name, COUNT(*) as non_default_count 
FROM ff_onboarding 
WHERE (tenant_id != 'DEFAULT' OR app_code != 'DEFAULT') AND is_valid = TRUE

-- Add more verification queries for other tables as needed
;

-- Sample data check
SELECT 'Sample updated records' as info;
SELECT id, tenant_id, app_code, modify_date, modify_by 
FROM ff_workflow 
WHERE modify_by = 'SYSTEM_UPDATE' 
LIMIT 5;
*/ 