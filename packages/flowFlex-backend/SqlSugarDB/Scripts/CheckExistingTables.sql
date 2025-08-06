-- =============================================
-- 检查数据库中实际存在的表和字段
-- =============================================

-- 1. 查看所有 ff_ 开头的表
SELECT table_name, table_type
FROM information_schema.tables 
WHERE table_schema = 'public' 
AND table_name LIKE 'ff_%'
ORDER BY table_name;

-- 2. 检查每个表是否有 tenant_id 和 app_code 字段
SELECT 
    t.table_name,
    CASE WHEN c1.column_name IS NOT NULL THEN 'YES' ELSE 'NO' END as has_tenant_id,
    CASE WHEN c2.column_name IS NOT NULL THEN 'YES' ELSE 'NO' END as has_app_code
FROM information_schema.tables t
LEFT JOIN information_schema.columns c1 ON t.table_name = c1.table_name 
    AND c1.column_name = 'tenant_id' AND c1.table_schema = 'public'
LEFT JOIN information_schema.columns c2 ON t.table_name = c2.table_name 
    AND c2.column_name = 'app_code' AND c2.table_schema = 'public'
WHERE t.table_schema = 'public' 
AND t.table_name LIKE 'ff_%'
ORDER BY t.table_name;

-- 3. 详细查看每个表的列信息
SELECT 
    table_name, 
    column_name, 
    data_type, 
    is_nullable, 
    column_default
FROM information_schema.columns 
WHERE table_schema = 'public' 
AND table_name LIKE 'ff_%'
AND column_name IN ('tenant_id', 'app_code')
ORDER BY table_name, column_name; 