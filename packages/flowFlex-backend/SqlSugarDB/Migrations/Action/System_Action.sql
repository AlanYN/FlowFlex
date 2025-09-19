-- Insert system actions with short codes
-- Note: FlowFlex uses SnowFlake ID generator, not database sequences
DO $$
BEGIN
    
    -- Add action_code column if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'ff_action_definitions' 
                   AND column_name = 'action_code') THEN
        ALTER TABLE ff_action_definitions ADD COLUMN action_code VARCHAR(20);
        RAISE NOTICE 'Added action_code column';
    END IF;
    
    -- Add is_tools column if it doesn't exist (from ActionDefinition entity)
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'ff_action_definitions' 
                   AND column_name = 'is_tools') THEN
        ALTER TABLE ff_action_definitions ADD COLUMN is_tools BOOLEAN DEFAULT FALSE;
        RAISE NOTICE 'Added is_tools column';
    END IF;
    
    -- Add is_ai_generated column if it doesn't exist (from ActionDefinition entity)
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'ff_action_definitions' 
                   AND column_name = 'is_ai_generated') THEN
        ALTER TABLE ff_action_definitions ADD COLUMN is_ai_generated BOOLEAN DEFAULT FALSE;
        RAISE NOTICE 'Added is_ai_generated column';
    END IF;
    
    -- Add trigger_type column if it doesn't exist (from ActionDefinition entity)
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'ff_action_definitions' 
                   AND column_name = 'trigger_type') THEN
        ALTER TABLE ff_action_definitions ADD COLUMN trigger_type VARCHAR(50) DEFAULT 'Task';
        RAISE NOTICE 'Added trigger_type column';
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
-- Using specific SnowFlake IDs to ensure consistent records across deployments
INSERT INTO ff_action_definitions (
    id, action_code, action_name, description, action_type, action_config, 
    is_enabled, create_date, modify_date, tenant_id, app_code, 
    create_user_id, modify_user_id, is_valid
) VALUES 
(
    1753249000001,
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
    true
),
(
    1753249000002,
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
    true
),
(
    1753249000003,
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
    true
)
ON CONFLICT (action_code) DO UPDATE SET
    action_name = EXCLUDED.action_name,
    description = EXCLUDED.description,
    action_config = EXCLUDED.action_config,
    modify_date = NOW();
