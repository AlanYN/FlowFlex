-- =============================================
-- FlowFlex Database Migration Script
-- Version: 20241219_AddAppCodeColumn_Safe
-- Created: 2024-12-19
-- Description: Add app_code column to all existing tables for application isolation
-- =============================================

-- Start transaction
BEGIN;

-- Function to safely add app_code column to a table
CREATE OR REPLACE FUNCTION add_app_code_column_safe(table_name text) RETURNS void AS $$
BEGIN
    -- Check if table exists
    IF EXISTS (SELECT 1 FROM information_schema.tables t WHERE t.table_name = add_app_code_column_safe.table_name AND t.table_schema = 'public') THEN
        -- Check if app_code column already exists
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns c WHERE c.table_name = add_app_code_column_safe.table_name AND c.column_name = 'app_code' AND c.table_schema = 'public') THEN
            EXECUTE format('ALTER TABLE %I ADD COLUMN app_code VARCHAR(32) NOT NULL DEFAULT ''DEFAULT''', table_name);
            RAISE NOTICE 'Added app_code column to table %', table_name;
        ELSE
            RAISE NOTICE 'Column app_code already exists in table %', table_name;
        END IF;
    ELSE
        RAISE NOTICE 'Table % does not exist, skipping...', table_name;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- Function to safely create index
CREATE OR REPLACE FUNCTION create_app_code_index_safe(table_name text) RETURNS void AS $$
DECLARE
    index_name text;
BEGIN
    -- Check if table exists
    IF EXISTS (SELECT 1 FROM information_schema.tables t WHERE t.table_name = create_app_code_index_safe.table_name AND t.table_schema = 'public') THEN
        -- Check if app_code column exists
        IF EXISTS (SELECT 1 FROM information_schema.columns c WHERE c.table_name = create_app_code_index_safe.table_name AND c.column_name = 'app_code' AND c.table_schema = 'public') THEN
            -- Check if tenant_id column exists
            IF EXISTS (SELECT 1 FROM information_schema.columns c WHERE c.table_name = create_app_code_index_safe.table_name AND c.column_name = 'tenant_id' AND c.table_schema = 'public') THEN
                index_name := format('idx_%s_app_code_tenant_id', table_name);
                EXECUTE format('CREATE INDEX IF NOT EXISTS %I ON %I(app_code, tenant_id)', index_name, table_name);
                RAISE NOTICE 'Created index % on table %', index_name, table_name;
            ELSE
                RAISE NOTICE 'Column tenant_id does not exist in table %, skipping index creation', table_name;
            END IF;
        ELSE
            RAISE NOTICE 'Column app_code does not exist in table %, skipping index creation', table_name;
        END IF;
    ELSE
        RAISE NOTICE 'Table % does not exist, skipping index creation...', table_name;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- Function to safely add column comment
CREATE OR REPLACE FUNCTION add_app_code_comment_safe(table_name text) RETURNS void AS $$
BEGIN
    -- Check if table exists and has app_code column
    IF EXISTS (SELECT 1 FROM information_schema.tables t WHERE t.table_name = add_app_code_comment_safe.table_name AND t.table_schema = 'public') AND
       EXISTS (SELECT 1 FROM information_schema.columns c WHERE c.table_name = add_app_code_comment_safe.table_name AND c.column_name = 'app_code' AND c.table_schema = 'public') THEN
        EXECUTE format('COMMENT ON COLUMN %I.app_code IS ''Application code for application isolation''', table_name);
        RAISE NOTICE 'Added comment to app_code column in table %', table_name;
    ELSE
        RAISE NOTICE 'Table % or app_code column does not exist, skipping comment...', table_name;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- Apply changes to all tables
DO $$
DECLARE
    table_names text[] := ARRAY[
        'ff_users',
        'ff_workflow',
        'ff_workflow_version',
        'ff_stage',
        'ff_stage_version',
        'ff_onboarding',
        'ff_questionnaire',
        'ff_questionnaire_section',
        'ff_questionnaire_answers',
        'ff_checklist',
        'ff_checklist_task',
        'ff_checklist_task_completion',
        'ff_internal_notes',
        'ff_onboarding_file',
        'ff_operation_change_log',
        'ff_static_field_values',
        'ff_stage_completion_log'
    ];
    table_name text;
BEGIN
    -- Add app_code column to all tables
    FOREACH table_name IN ARRAY table_names LOOP
        PERFORM add_app_code_column_safe(table_name);
    END LOOP;
    
    -- Create indexes for all tables
    FOREACH table_name IN ARRAY table_names LOOP
        PERFORM create_app_code_index_safe(table_name);
    END LOOP;
    
    -- Add comments for all tables
    FOREACH table_name IN ARRAY table_names LOOP
        PERFORM add_app_code_comment_safe(table_name);
    END LOOP;
END;
$$;

-- Clean up temporary functions
DROP FUNCTION IF EXISTS add_app_code_column_safe(text);
DROP FUNCTION IF EXISTS create_app_code_index_safe(text);
DROP FUNCTION IF EXISTS add_app_code_comment_safe(text);

-- Commit transaction
COMMIT;

-- Log migration completion
SELECT 'Migration 20241219_AddAppCodeColumn_Safe completed successfully' AS status; 