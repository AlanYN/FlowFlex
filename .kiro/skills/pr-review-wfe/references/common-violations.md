# FlowFlex 常见违规速查

本文档列出 FlowFlex 项目中最频繁出现的代码违规，评审时优先检查这些项。

---

## 后端违规（.NET）

### 1. 架构层级越界

**违规**
```csharp
// Controller 直接调用 Repository（跳过 Service 层）
public async Task<IActionResult> GetStages()
{
    var result = await _stageRepository.GetListAsync();
    return Success(result);
}
```

**正确**
```csharp
public async Task<IActionResult> GetStages()
{
    var result = await _stageService.GetStagesAsync();
    return Success(result);
}
```

---

### 2. Service 未实现生命周期接口

**违规**
```csharp
public class WorkflowService : IWorkflowService
{
    // 不会被 DI 自动注册
}
```

**正确**
```csharp
public class WorkflowService : IWorkflowService, IScopedService
{
    // IScopedService 标记触发自动注册
}
```

---

### 3. 直接返回 Ok() 而非 Success()

**违规**
```csharp
return Ok(result);
return new JsonResult(result);
```

**正确**
```csharp
return Success(result);   // 有数据
return Success();          // 无数据
```

---

### 4. 在 Controller 层 try-catch

**违规**
```csharp
public async Task<IActionResult> CreateStage([FromBody] CreateStageInput input)
{
    try
    {
        var result = await _stageService.CreateAsync(input);
        return Success(result);
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
    }
}
```

**正确**
```csharp
// 去掉 try-catch，由全局 GlobalExceptionHandlingMiddleware 统一处理
public async Task<IActionResult> CreateStage([FromBody] CreateStageInput input)
{
    var result = await _stageService.CreateAsync(input);
    return Success(result);
}
```

---

### 5. 业务错误未使用 CRMException

**违规**
```csharp
throw new Exception("Workflow not found");
throw new InvalidOperationException("Stage is invalid");
```

**正确**
```csharp
throw new CRMException(ErrorCodeEnum.DataNotFound, "Workflow not found");
```

---

### 6. 循环内查询数据库（N+1 问题）

**违规**
```csharp
foreach (var stageId in stageIds)
{
    var components = await _componentRepo.GetByStageIdAsync(stageId); // N 次查询
}
```

**正确**
```csharp
var components = await _componentRepo.GetByStageIdsAsync(stageIds); // 1 次批量查询
var componentMap = components.GroupBy(c => c.StageId).ToDictionary(g => g.Key, g => g.ToList());
foreach (var stageId in stageIds)
{
    var stageComponents = componentMap.GetValueOrDefault(stageId, new List<Component>());
}
```

---

### 7. Entity 缺少公共审计字段

**违规**
```csharp
[SugarTable("ff_custom_entity")]
public class CustomEntity
{
    public long Id { get; set; }
    public string Name { get; set; }
    // 缺少审计字段
}
```

**正确**
```csharp
[SugarTable("ff_custom_entity")]
public class CustomEntity : EntityBaseCreateInfo
{
    public long Id { get; set; }
    public string Name { get; set; }
    // EntityBaseCreateInfo 包含 AppCode, TenantId, IsValid, CreateDate 等
}
```

---

### 8. 绕过多租户全局过滤器

**违规**
```csharp
// 随意绕过，可能导致跨租户数据泄露
var allData = await _db.Queryable<Workflow>().Filter(null, true).ToListAsync();
```

**正确**
```csharp
// 有明确业务理由时才绕过，且必须加注释说明
// 系统级维护操作：需要获取所有租户的过期工作流进行清理
var expiredWorkflows = await _db.Queryable<Workflow>()
    .Filter(null, true)
    .Where(w => w.ExpireDate < DateTimeOffset.UtcNow)
    .ToListAsync();
```

---

### 9. Migration 修改了 Entity 但未写 Migration SQL

**违规**
```csharp
// 只修改了 Entity，没有对应 Migration
public class Stage : EntityBaseCreateInfo
{
    [SugarColumn(ColumnName = "new_field")]
    public string NewField { get; set; } // 数据库实际没有这列
}
```

**正确**
```csharp
// 同时在 SqlSugarDB/Migrations/ 创建对应 Migration：
// Migration_20260801001_AddNewFieldToStage.cs
public static void Up(ISqlSugarClient db)
{
    db.Ado.ExecuteCommand(@"
        ALTER TABLE ff_stage
        ADD COLUMN IF NOT EXISTS new_field VARCHAR(255);
    ");
}
```

---

### 10. 硬编码魔法数字/字符串

**违规**
```csharp
if (questionType == 3) { ... }        // 3 是什么？
if (status == "in_progress") { ... }  // 字符串散落各处
```

**正确**
```csharp
if (questionType == QuestionTypeEnum.Number) { ... }
if (status == StageStatusEnum.InProgress) { ... }
```

---

### 11. 异步方法未加 Async 后缀

**违规**
```csharp
public async Task<List<Stage>> GetStages() { ... }
```

**正确**
```csharp
public async Task<List<Stage>> GetStagesAsync() { ... }
```

---

### 12. 非异步的数据库调用

**违规**
```csharp
var result = _stageRepository.GetList(); // 同步调用阻塞线程
```

**正确**
```csharp
var result = await _stageRepository.GetListAsync();
```

---

## 前端违规（Vue 3 / TypeScript）

### 13. 使用 Options API

**违规**
```vue
<script lang="ts">
export default {
  data() { return { count: 0 } },
  methods: { increment() { this.count++ } }
}
</script>
```

**正确**
```vue
<script setup lang="ts">
import { ref } from 'vue'
const count = ref(0)
const increment = () => { count.value++ }
</script>
```

---

### 14. API URL 缺少域前缀

**违规**
```typescript
// 缺少 ow/ 前缀，会导致 404
const getWorkflows = () => defHttp.get({ url: '/workflows/v1' })
```

**正确**
```typescript
const getWorkflows = () => defHttp.get({ url: '/ow/workflows/v1' })
```

---

### 15. Store ID 不符合规范

**违规**
```typescript
const useWorkflowStore = defineStore('workflowStore', { ... })
const useWorkflowStore = defineStore('wfe-workflow', { ... })
```

**正确**
```typescript
const useWorkflowStore = defineStore('item-wfe-app-workflow', { ... })
```

---

### 16. v-html 未做 XSS 净化

**违规**
```vue
<div v-html="userContent"></div>
<div v-html="aiGeneratedContent"></div>
```

**正确**
```vue
<script setup lang="ts">
import DOMPurify from 'dompurify'
const safeContent = computed(() => DOMPurify.sanitize(userContent.value))
</script>
<template>
  <div v-html="safeContent"></div>
</template>
```

---

### 17. Props 缺少 withDefaults 默认值

**违规**
```vue
<script setup lang="ts">
const props = defineProps<{
  title: string
  visible?: boolean
}>()
</script>
```

**正确**
```vue
<script setup lang="ts">
const props = withDefaults(defineProps<{
  title: string
  visible?: boolean
}>(), {
  visible: false
})
</script>
```

---

### 18. 直接修改 props

**违规**
```vue
<script setup lang="ts">
const props = defineProps<{ modelValue: string }>()
props.modelValue = 'new value' // 直接修改 prop
```

**正确**
```vue
<script setup lang="ts">
const props = defineProps<{ modelValue: string }>()
const emit = defineEmits<{ 'update:modelValue': [value: string] }>()
// 通过 emit 通知父组件更新
emit('update:modelValue', 'new value')
```

---

## 安全违规速查

| 违规 | 风险等级 | 说明 |
|------|---------|------|
| 硬编码 Token/Secret | 🔴 严重 | 凭据泄露 |
| 缺少租户过滤 | 🔴 严重 | 跨租户数据泄露 |
| Controller 无鉴权特性 | 🔴 严重 | 未授权访问 |
| `v-html` 未净化 | 🔴 严重 | XSS 攻击 |
| 文件上传无类型校验 | ⚠️ 高 | 恶意文件上传 |
| 日志中记录敏感数据 | ⚠️ 高 | 信息泄露 |
| 批量操作无数量上限 | ⚠️ 中 | DoS 风险 |
| 外部 URL 无白名单 | ⚠️ 中 | SSRF 风险 |
