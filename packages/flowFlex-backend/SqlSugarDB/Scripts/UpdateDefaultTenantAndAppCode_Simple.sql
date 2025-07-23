-- =============================================
-- FlowFlex 安全数据更新脚本
-- 用途：将存在的表的 tenant_id 和 app_code 更新为 'DEFAULT'
-- 只更新实际存在的表，避免错误
-- =============================================

-- 先检查表是否存在，然后执行更新
DO $$
DECLARE
    table_name TEXT;
    sql_stmt TEXT;
    update_count INTEGER;
    tables_to_update TEXT[] := ARRAY[
        'ff_users', 'ff_workflow', 'ff_workflow_version', 'ff_stage', 'ff_stage_version',
        'ff_onboarding', 'ff_questionnaire', 'ff_questionnaire_section', 'ff_questionnaire_answers',
        'ff_checklist', 'ff_checklist_task', 'ff_checklist_task_completion', 'ff_internal_notes',
        'ff_onboarding_file', 'ff_operation_change_log', 'ff_static_field_values', 'ff_stage_completion_log',
        'ff_events', 'ff_access_tokens'
    ];
BEGIN
    RAISE NOTICE '=== 开始更新数据 ===';
    
    FOREACH table_name IN ARRAY tables_to_update
    LOOP
        -- 检查表是否存在
        IF EXISTS (SELECT 1 FROM information_schema.tables 
                   WHERE table_schema = 'public' AND table_name = table_name) THEN
            
            -- 检查表是否有 tenant_id 和 app_code 字段
            IF EXISTS (SELECT 1 FROM information_schema.columns 
                      WHERE table_schema = 'public' AND table_name = table_name AND column_name = 'tenant_id') AND
               EXISTS (SELECT 1 FROM information_schema.columns 
                      WHERE table_schema = 'public' AND table_name = table_name AND column_name = 'app_code') THEN
                
                -- 更新 tenant_id 和 app_code
                sql_stmt := format('UPDATE %I SET tenant_id = ''DEFAULT'', app_code = ''DEFAULT'' WHERE tenant_id != ''DEFAULT'' OR app_code != ''DEFAULT'' OR app_code IS NULL', table_name);
                EXECUTE sql_stmt;
                GET DIAGNOSTICS update_count = ROW_COUNT;
                RAISE NOTICE '表 %: 更新了 % 条记录 (tenant_id + app_code)', table_name, update_count;
                
            ELSIF EXISTS (SELECT 1 FROM information_schema.columns 
                         WHERE table_schema = 'public' AND table_name = table_name AND column_name = 'app_code') THEN
                
                -- 只更新 app_code (如 ff_events 表)
                sql_stmt := format('UPDATE %I SET app_code = ''DEFAULT'' WHERE app_code != ''DEFAULT'' OR app_code IS NULL', table_name);
                EXECUTE sql_stmt;
                GET DIAGNOSTICS update_count = ROW_COUNT;
                RAISE NOTICE '表 %: 更新了 % 条记录 (仅 app_code)', table_name, update_count;
                
            ELSIF EXISTS (SELECT 1 FROM information_schema.columns 
                         WHERE table_schema = 'public' AND table_name = table_name AND column_name = 'tenant_id') THEN
                
                -- 只更新 tenant_id
                sql_stmt := format('UPDATE %I SET tenant_id = ''DEFAULT'' WHERE tenant_id != ''DEFAULT''', table_name);
                EXECUTE sql_stmt;
                GET DIAGNOSTICS update_count = ROW_COUNT;
                RAISE NOTICE '表 %: 更新了 % 条记录 (仅 tenant_id)', table_name, update_count;
                
            ELSE
                RAISE NOTICE '表 %: 不包含 tenant_id 或 app_code 字段，跳过', table_name;
            END IF;
        ELSE
            RAISE NOTICE '表 % 不存在，跳过', table_name;
        END IF;
    END LOOP;
    
    RAISE NOTICE '=== 更新完成 ===';
END $$; 