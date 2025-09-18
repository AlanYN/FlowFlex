-- Fix sequence and insert system actions with short codes
DO $$
DECLARE
    max_id BIGINT;
    seq_val BIGINT;
BEGIN
    -- Get current max ID from the table
    SELECT COALESCE(MAX(id), 0) INTO max_id FROM ff_action_definitions;
    
    -- Get current sequence value
    SELECT last_value INTO seq_val FROM ff_action_definitions_id_seq;
    
    -- If sequence is behind, restart it
    IF seq_val <= max_id THEN
        PERFORM setval('ff_action_definitions_id_seq', max_id + 1);
        RAISE NOTICE 'Sequence restarted from % to %', seq_val, max_id + 1;
    END IF;
    
    -- Add action_code column if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'ff_action_definitions' 
                   AND column_name = 'action_code') THEN
        ALTER TABLE ff_action_definitions ADD COLUMN action_code VARCHAR(20);
        RAISE NOTICE 'Added action_code column';
    END IF;
    
    -- Create unique constraint if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints 
                   WHERE table_name = 'ff_action_definitions' 
                   AND constraint_name = 'uk_action_definitions_action_code') THEN
        ALTER TABLE ff_action_definitions 
        ADD CONSTRAINT uk_action_definitions_action_code UNIQUE (action_code);
        RAISE NOTICE 'Added unique constraint on action_code';
    END IF;
END $$;

-- Insert system actions with short codes
INSERT INTO ff_action_definitions (
    action_code, action_name, description, action_type, action_config, 
    is_enabled, create_date, modify_date, tenant_id, app_code, 
    create_user_id, modify_user_id, version, is_valid, is_tools
) VALUES 
(
    'SYS-COMP-STG',
    'Complete Stage',
    'Complete current stage with validation',
    '4',
    '{"actionName":"CompleteStage","useValidationApi":true}',
    true,
    NOW(),
    NOW(),
    'Cyntest-UT',
    'crm-web',
    1753248912142,
    1753248912142,
    999,
    true,
    false
),
(
    'SYS-MOVE-STG',
    'Move to Stage', 
    'Move onboarding to a specific stage',
    '4',
    '{"actionName":"MoveToStage"}',
    true,
    NOW(),
    NOW(),
    'Cyntest-UT',
    'crm-web',
    1753248912142,
    1753248912142,
    999,
    true,
    false
),
(
    'SYS-ASSIGN-OB',
    'Assign Onboarding',
    'Assign onboarding to a user or team',
    '4', 
    '{"actionName":"AssignOnboarding"}',
    true,
    NOW(),
    NOW(),
    'Cyntest-UT',
    'crm-web',
    1753248912142,
    1753248912142,
    999,
    true,
    false
)
ON CONFLICT (action_code) DO UPDATE SET
    action_name = EXCLUDED.action_name,
    description = EXCLUDED.description,
    action_config = EXCLUDED.action_config,
    modify_date = NOW();
