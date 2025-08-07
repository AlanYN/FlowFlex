-- ================================================================
-- Action System Tables - PostgreSQL DDL
-- ================================================================

-- 1. Action Definitions Table (Action定义表)
CREATE TABLE IF NOT EXISTS ff_action_definitions (
    id BIGSERIAL PRIMARY KEY,
    action_name VARCHAR(100) NOT NULL,
    action_type VARCHAR(50) NOT NULL,
    description VARCHAR(500) DEFAULT '',
    action_config JSONB DEFAULT '{}',
    is_enabled BOOLEAN DEFAULT true,
    
    -- 基础字段（继承自EntityBaseCreateInfo）
    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    create_by VARCHAR(50) DEFAULT 'SYSTEM',
    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
    create_user_id BIGINT DEFAULT 0,
    modify_user_id BIGINT DEFAULT 0,
    
    -- 租户和应用隔离（继承自AbstractEntityBase）
    tenant_id VARCHAR(32) DEFAULT 'DEFAULT',
    app_code VARCHAR(32) DEFAULT 'DEFAULT',
    
    -- 软删除标记（继承自EntityBase）
    is_valid BOOLEAN DEFAULT true
);

-- 2. Action Trigger Mappings Table (Action触发映射表)
CREATE TABLE IF NOT EXISTS ff_action_trigger_mappings (
    id BIGSERIAL PRIMARY KEY,
    action_definition_id BIGINT NOT NULL,
    trigger_type VARCHAR(50) NOT NULL,
    trigger_source_id BIGINT NOT NULL,
    trigger_source_name VARCHAR(200) DEFAULT '',
    trigger_event VARCHAR(50) DEFAULT 'Completed',
    trigger_conditions JSONB DEFAULT '{}',
    is_enabled BOOLEAN DEFAULT true,
    execution_order INTEGER DEFAULT 0,
    mapping_config JSONB DEFAULT '{}',
    description VARCHAR(500) DEFAULT '',
    
    -- 基础字段（继承自EntityBaseCreateInfo）
    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    create_by VARCHAR(50) DEFAULT 'SYSTEM',
    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
    create_user_id BIGINT DEFAULT 0,
    modify_user_id BIGINT DEFAULT 0,
    
    -- 租户和应用隔离（继承自AbstractEntityBase）
    tenant_id VARCHAR(32) DEFAULT 'DEFAULT',
    app_code VARCHAR(32) DEFAULT 'DEFAULT',
    
    -- 软删除标记（继承自EntityBase）
    is_valid BOOLEAN DEFAULT true
);

-- 3. Action Executions Table (Action执行记录表)
CREATE TABLE IF NOT EXISTS ff_action_executions (
    id BIGSERIAL PRIMARY KEY,
    action_definition_id BIGINT NOT NULL,
    action_trigger_mapping_id BIGINT,
    execution_id VARCHAR(100) NOT NULL UNIQUE,
    action_name VARCHAR(100) NOT NULL,
    action_type VARCHAR(50) NOT NULL,
    trigger_context JSONB DEFAULT '{}',
    execution_status VARCHAR(20) DEFAULT 'Pending',
    started_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,
    duration_ms BIGINT,
    execution_input JSONB DEFAULT '{}',
    execution_output JSONB DEFAULT '{}',
    error_message VARCHAR(2000) DEFAULT '',
    error_stack_trace TEXT DEFAULT '',
    executor_info JSONB DEFAULT '{}',
    
    -- 基础字段（继承自EntityBaseCreateInfo）
    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    create_by VARCHAR(50) DEFAULT 'SYSTEM',
    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
    create_user_id BIGINT DEFAULT 0,
    modify_user_id BIGINT DEFAULT 0,
    
    -- 租户和应用隔离（继承自AbstractEntityBase）
    tenant_id VARCHAR(32) DEFAULT 'DEFAULT',
    app_code VARCHAR(32) DEFAULT 'DEFAULT',
    
    -- 软删除标记（继承自EntityBase）
    is_valid BOOLEAN DEFAULT true
);

-- ================================================================
-- 索引创建
-- ================================================================

-- ActionDefinition 索引
CREATE INDEX IF NOT EXISTS idx_action_definitions_type_enabled 
ON ff_action_definitions(action_type, is_enabled);

CREATE INDEX IF NOT EXISTS idx_action_definitions_name 
ON ff_action_definitions(action_name);

-- ActionTriggerMapping 索引（核心查询索引）
CREATE INDEX IF NOT EXISTS idx_trigger_mappings_trigger_source 
ON ff_action_trigger_mappings(trigger_type, trigger_source_id, trigger_event, is_enabled);

CREATE INDEX IF NOT EXISTS idx_trigger_mappings_action_definition 
ON ff_action_trigger_mappings(action_definition_id);

-- ActionExecution 索引
CREATE INDEX IF NOT EXISTS idx_action_executions_definition_date 
ON ff_action_executions(action_definition_id, create_date);

CREATE INDEX IF NOT EXISTS idx_action_executions_status_date 
ON ff_action_executions(execution_status, create_date);

CREATE INDEX IF NOT EXISTS idx_action_executions_execution_id 
ON ff_action_executions(execution_id);

 