-- =============================================
-- FlowFlex 手动数据更新脚本
-- 用途：手动执行常见表的更新，避免不存在的表错误
-- 使用方法：逐条执行，如果表不存在会报错，跳过即可
-- =============================================

-- 核心表更新 (逐条执行，如果报错就跳过)

-- 用户表
UPDATE ff_users SET tenant_id = 'DEFAULT', app_code = 'DEFAULT' 
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- 工作流表
UPDATE ff_workflow SET tenant_id = 'DEFAULT', app_code = 'DEFAULT' 
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- 阶段表
UPDATE ff_stage SET tenant_id = 'DEFAULT', app_code = 'DEFAULT' 
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- 入职表
UPDATE ff_onboarding SET tenant_id = 'DEFAULT', app_code = 'DEFAULT' 
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- 问卷表
UPDATE ff_questionnaire SET tenant_id = 'DEFAULT', app_code = 'DEFAULT' 
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- 问卷答案表
UPDATE ff_questionnaire_answers SET tenant_id = 'DEFAULT', app_code = 'DEFAULT' 
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- 检查单表
UPDATE ff_checklist SET tenant_id = 'DEFAULT', app_code = 'DEFAULT' 
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- 检查单任务表
UPDATE ff_checklist_task SET tenant_id = 'DEFAULT', app_code = 'DEFAULT' 
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- 内部备注表
UPDATE ff_internal_notes SET tenant_id = 'DEFAULT', app_code = 'DEFAULT' 
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- 事件表 (可能只有 app_code 字段)
UPDATE ff_events SET app_code = 'DEFAULT' 
WHERE app_code != 'DEFAULT' OR app_code IS NULL;

-- 访问令牌表
UPDATE ff_access_tokens SET tenant_id = 'DEFAULT', app_code = 'DEFAULT' 
WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- =============================================
-- 可选表更新 (如果存在的话)
-- =============================================

-- 工作流版本表
-- UPDATE ff_workflow_version SET tenant_id = 'DEFAULT', app_code = 'DEFAULT' 
-- WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- 阶段版本表
-- UPDATE ff_stage_version SET tenant_id = 'DEFAULT', app_code = 'DEFAULT' 
-- WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- 问卷章节表
-- UPDATE ff_questionnaire_section SET tenant_id = 'DEFAULT', app_code = 'DEFAULT' 
-- WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- 检查单任务完成表
-- UPDATE ff_checklist_task_completion SET tenant_id = 'DEFAULT', app_code = 'DEFAULT' 
-- WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- 入职文件表
-- UPDATE ff_onboarding_file SET tenant_id = 'DEFAULT', app_code = 'DEFAULT' 
-- WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- 操作日志表
-- UPDATE ff_operation_change_log SET tenant_id = 'DEFAULT', app_code = 'DEFAULT' 
-- WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL;

-- 静态字段值表
-- UPDATE ff_static_field_values SET tenant_id = 'DEFAULT', app_code = 'DEFAULT' 
-- WHERE tenant_id != 'DEFAULT' OR app_code != 'DEFAULT' OR app_code IS NULL; 