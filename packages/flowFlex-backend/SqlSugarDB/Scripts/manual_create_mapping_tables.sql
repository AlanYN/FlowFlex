-- 手动创建组件映射表
-- 用于修复迁移失败后的手动执行

-- 创建问卷-阶段映射表
CREATE TABLE IF NOT EXISTS ff_questionnaire_stage_mapping (
    questionnaire_id BIGINT NOT NULL,
    stage_id BIGINT NOT NULL,
    workflow_id BIGINT NOT NULL,
    tenant_id VARCHAR(50) NOT NULL DEFAULT 'DEFAULT',
    app_code VARCHAR(50) NOT NULL DEFAULT 'DEFAULT',
    is_valid BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (questionnaire_id, stage_id)
);

-- 创建问卷映射表索引
CREATE INDEX IF NOT EXISTS idx_questionnaire_mapping 
ON ff_questionnaire_stage_mapping (questionnaire_id, tenant_id, app_code);

CREATE INDEX IF NOT EXISTS idx_questionnaire_stage_mapping 
ON ff_questionnaire_stage_mapping (stage_id, tenant_id, app_code);

CREATE INDEX IF NOT EXISTS idx_questionnaire_workflow_mapping 
ON ff_questionnaire_stage_mapping (workflow_id, tenant_id, app_code);

-- 创建清单-阶段映射表
CREATE TABLE IF NOT EXISTS ff_checklist_stage_mapping (
    checklist_id BIGINT NOT NULL,
    stage_id BIGINT NOT NULL,
    workflow_id BIGINT NOT NULL,
    tenant_id VARCHAR(50) NOT NULL DEFAULT 'DEFAULT',
    app_code VARCHAR(50) NOT NULL DEFAULT 'DEFAULT',
    is_valid BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (checklist_id, stage_id)
);

-- 创建清单映射表索引
CREATE INDEX IF NOT EXISTS idx_checklist_mapping 
ON ff_checklist_stage_mapping (checklist_id, tenant_id, app_code);

CREATE INDEX IF NOT EXISTS idx_checklist_stage_mapping 
ON ff_checklist_stage_mapping (stage_id, tenant_id, app_code);

CREATE INDEX IF NOT EXISTS idx_checklist_workflow_mapping 
ON ff_checklist_stage_mapping (workflow_id, tenant_id, app_code);

-- 更新迁移历史
INSERT INTO __migration_history (migration_id) 
VALUES ('20250122000003_CreateComponentMappingTables')
ON CONFLICT (migration_id) DO NOTHING;

-- 验证创建成功
SELECT 'Tables created successfully' as status,
       (SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'ff_questionnaire_stage_mapping') as questionnaire_table,
       (SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'ff_checklist_stage_mapping') as checklist_table;