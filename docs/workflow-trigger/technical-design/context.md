# Workflow Trigger — 技术方案

## 1. 功能概述

在 Workflow Settings 中新增 **Trigger Rules** 入口，打开 Visual Flow Editor。多个 Workflow 共享一个 Trigger Graph，通过拖拽连线配置触发条件与数据映射，实现多 Workflow 之间的自动化衔接。

**核心业务场景：**

```
Sales Pipeline（deal_value > 100K）
    → Contract Signing（完成后并行触发）
        → Ops Implementation
        → IT Implementation
            → Go Live（ALL completed 后触发）
```

---

## 2. 触发机制

### 2.1 触发时机

**两种情况都会触发下游 Workflow：**

| 触发方式                              | 说明                                                               |
| ------------------------------------- | ------------------------------------------------------------------ |
| **最后一个 Stage 正常完成**           | Case 按正常流程推进，最后一个 Stage 完成时触发                     |
| **Case 被强制完成（Force Complete）** | 用户手动点击 Force Complete，跳过剩余 Stage 强制结束 Case 时也触发 |

两种情况执行逻辑完全相同：

```
Case 完成（正常 Complete 或 Force Complete）
  ↓
查找 ff_workflow_trigger_connection WHERE source_workflow_id = X
  ↓
遍历每条 connection，评估 Trigger Condition
  ├─ 条件不满足 → 记录 Skipped，不触发
  └─ 条件满足   → 创建目标 Case，执行 Data Mapping，记录 Triggered
```

> **注意**：Trigger Condition 里配置的 Stage / Component / 字段条件是一个**过滤器**，用于决定"满足什么条件的 Case 才触发下游"，而不是定义触发时机本身。触发时机始终是 Case 完成（含强制完成）。

### 2.2 触发条件（Trigger Condition）

每条 Connection 可配置多条条件规则（AND / OR 逻辑组合）。**不配置条件时，默认"源 Workflow 最后 Stage 完成即触发"（等价于 Completed）。**

每条规则的级联结构：

```
Stage 选择
  └── Component 选择（按类型分组）
        ├── Required Fields（直接选具体字段）
        │     └── Operator（==、!=、>、>=、<、<=、contains）+ Value
        ├── Checklists（选 Checklist）
        │     └── Trigger When（Task Completed / All Tasks Completed）
        └── Questionnaires（选 Questionnaire）
              └── Question 选择
                    └── Operator + Value
```

**字段说明：**

| 字段         | 说明                                                                                |
| ------------ | ----------------------------------------------------------------------------------- |
| Stage        | 选择触发源 Workflow 的某个 Stage                                                    |
| Component    | Stage 下的 Required Field / Checklist / Questionnaire（来自 Stage.components_json） |
| Operator     | 比较操作符（== / != / > / >= / < / <= / contains）                                  |
| Value        | 对比值（文本输入）                                                                  |
| Trigger When | 仅 Checklist 类型专用：单条任务完成 / 所有任务完成                                  |

**条件数据存储格式（存入 `config_json.conditions[]`）：**

```json
{
  "id": "cond_1234567890",
  "logic": "AND",
  "stageId": "2049541143913435136",
  "stageName": "Application Review",
  "componentKey": "field_2067196198997069824",
  "componentType": "fields",
  "componentId": "2067196198997069824",
  "componentName": "Company Name",
  "operator": "==",
  "value": "Enterprise"
}
```

`componentKey` 格式规则：

- Required Field：`field_{fieldId}`
- Checklist：`checklist_{checklistId}`
- Questionnaire：`questionnaire_{questionnaireId}`
- `componentType` 对应：`fields` / `checklist` / `questionnaires`

---

## 3. Data Mapping

### 3.1 业务语义

触发时，将源 Case 中的字段值、问卷答案或静态值，**拷贝到新创建的目标 Case 的对应字段中**。

> **Source** = 源 Workflow 某个 Stage 的动态字段值 / 问卷答案 / 静态文本  
> **Target** = 目标 Workflow 某个 Stage 的动态字段（Stage · Component · Field）

选项格式：`Stage名 · 字段名`，例如 `Deal Information · Deal Value`。

**依据（OW-719 需求文档 4.4.3 + 8.2）：**

> Target 选择：目标 Workflow 中的 Stage · Component · Field  
> Source 和 Target 的字段选择格式为：Component · Field Name（如 Deal Information · Deal Value）

> ⚠️ `ff_onboarding` 表的固定字段（CaseName、ContactEmail 等）**不通过 Data Mapping 填充**，其填充方式由异步执行引擎（OW-724）另行设计。

### 3.2 Auto-map（自动匹配）

以 **Target Workflow 的字段**为基准，逐一检查 Source Workflow 中是否存在同名字段（字段名 normalize 后比对）。

```
normalize 规则：toLowerCase + trim + 所有非字母数字序列替换为 _
示例：
  "Contact Email" → "contact_email"
  "contact_email" → "contact_email"  ✅ 匹配
  "Company Name"  → "company_name"
  "Company Name"  → "company_name"   ✅ 匹配
```

**Auto-map 列表显示：**

- Target 有、Source 有同名 → 自动配对，`enabled = true`（可关闭）
- Target 有、Source 无同名 → 显示该行但 `enabled = false`，用户可手动选择 Source 字段

**勾选状态持久化：**用户对每行的 enabled 变更会保存到 `config_json.autoMappedStates[]`，下次打开时恢复。

```json
"autoMappedStates": [
  { "id": "auto_stageId__fieldId_stageId__fieldId", "enabled": true },
  { "id": "auto_none_stageId__fieldId", "enabled": false }
]
```

### 3.3 Field Mappings（手动映射）

用户完全自定义的映射，每行配置：

| 字段        | 选项                 | 说明                                       |
| ----------- | -------------------- | ------------------------------------------ |
| Source 类型 | Dynamic field        | 源 Workflow 某 Stage 的 Required Field     |
|             | Questionnaire answer | 源 Workflow 某 Stage 的 Questionnaire 答案 |
|             | Static value         | 固定文本值                                 |
| Source 选择 | `Stage · Field` 格式 | 如 `Application Review · Company Name`     |
| Target 选择 | `Stage · Field` 格式 | 如 `Contract Stage · Client Name`          |

**Source 选项的数据来源（`dynamicFieldOptions` / `questionnaireOptions`）：**

```
源 Workflow node-info → stages[*].fields  → "stageName · fieldName"
源 Workflow node-info → stages[*].questionnaires[*].questions → "questionnaireName: questionTitle"
```

**Target 选项的数据来源（`targetFieldOptions`）：**

```
目标 Workflow node-info → stages[*].fields → "stageName · fieldName"
```

**选项 ID 格式：** `{stageId}__{fieldId}` 或 `{questionnaireId}__{questionId}`，双下划线分隔，用于执行时定位具体字段。

**手动映射存储格式（`config_json.mappings[]`）：**

```json
{
  "id": "map_1234567890",
  "sourceType": "dynamic_field",
  "sourceId": "stageId__fieldId",
  "sourceName": "Application Review · Company Name",
  "targetFieldId": "stageId__fieldId",
  "targetFieldName": "Contract Stage · Client Name",
  "enabled": true
}
```

### 3.4 Mapping 执行（后端 OW-724，待实现）

执行时机：触发条件评估通过、目标 Case 创建完成后。

执行顺序：

1. 处理 `autoMap: true` 时的 Auto-map 字段（`autoMappedStates[enabled=true]` 的条目）
2. 处理 `mappings[]` 中 `enabled: true` 的手动映射条目
3. Dynamic field → 读取源 Case `StaticFieldValue` 表中对应字段值
4. Questionnaire answer → 读取源 Case `QuestionnaireAnswer` 中对应问题的答案
5. Static value → 直接写入固定文本
6. 将值写入目标 Case 对应 Stage 的 `StaticFieldValue` 记录

---

## 4. 前端架构

### 4.1 文件结构

```
packages/flowFlex-common/src/app/
├── views/onboard/workflow/
│   ├── trigger-editor.vue              # 主页面（三栏布局）
│   └── components/triggers/
│       ├── TriggerSidebar.vue          # 左侧 Workflow 列表 + 筛选 + 统计
│       ├── TriggerCanvas.vue           # 中间画布（SVG 曲线连线 + 缩放）
│       ├── WorkflowCard.vue            # 卡片（拖拽 / Connect 按钮 / Handle 拖线）
│       └── ConnectionPanel.vue         # 右侧配置抽屉
│   └── composables/
│       └── useTriggerEditor.ts         # 所有状态与操作
└── apis/ow/
    └── triggers.ts                     # API 函数封装
```

### 4.2 状态管理（useTriggerEditor）

```
init()
  ├── getTriggerGraphAllWorkflows()  → allWorkflows[]（左侧列表）
  └── getTriggerGraph(workflowId)
        ├── graphData.canvasLayout    → cards[]（卡片位置）
        └── graphData.connections[]   → connections[]（连线数据）

save()
  └── saveTriggerGraph({
        workflowId,
        canvasLayout: JSON.stringify({workflowId: {x,y}}),
        canvasWorkflowIds: JSON.stringify([...]),
        connections: connections[].map(...)
      })

updateConnectionConfig(id, conditionSummary, configJson, ruleName)
  └── 更新内存中的 connection（只改内存，需点页面 Save 才写后端）
```

**重要**：ConnectionPanel 点 Apply → 更新内存 → 页面顶部显示 "Unsaved changes" → 用户点 Save 才调用接口持久化。

### 4.3 TriggerCanvas（画布）

- **卡片层**：`position: absolute`，拖拽通过 mousedown/mousemove/mouseup 更新 `(x, y)`
- **SVG 层**：三次 Bezier 曲线连线（`getCubicBezierPath`），`transform: scale(zoomLevel)` 实现缩放
- **连线标签**：用 SVG `<g>` 包裹 `<rect>` + `<text>`，支持点击打开配置面板（`@click.stop="emit('select-connection', conn.id)"`）
- **缩放工具栏**：右上角，25%–200%
- **连线颜色**：默认 `--el-border-color`，hover `--el-color-primary-light-3`，选中 `--el-color-primary`，箭头颜色通过 `getComputedStyle` 动态读取 CSS 变量注入 SVG marker（因 SVG defs 不支持 CSS 变量）

### 4.4 ConnectionPanel（配置抽屉）

**打开时机：** 点击画布上的连线（连线路径或标签均可点击）

**数据加载：** 打开时调用 `loadNodeInfo(connection.sourceWorkflowId, connection.targetWorkflowId)`，

- 仅请求 source 的 node-info（target 也需要，两侧各一次请求并发）
- 加载期间 `v-loading` 蒙层覆盖整个 body，Apply 按钮 `:disabled`
- `watch(connection, immediate: true)` 同步已保存的 `configJson` 到本地状态

**Apply 流程：**

```
用户点 Apply
  → handleSave() → emit('save', {conditionSummary, configJson, ruleName})
  → trigger-editor.vue handlePanelSave()
  → editor.updateConnectionConfig()  ← 更新内存
  → ElMessage.success("Trigger connection saved. Click Save to persist changes.")
```

---

## 5. 后端架构

### 5.1 文件结构

```
packages/flowFlex-backend/
├── Domain/Entities/OW/
│   ├── WorkflowTriggerGraph.cs
│   └── WorkflowTriggerConnection.cs
├── Domain/Repository/OW/
│   ├── IWorkflowTriggerGraphRepository.cs
│   └── IWorkflowTriggerConnectionRepository.cs
├── SqlSugarDB/
│   ├── Repositories/OW/
│   │   ├── WorkflowTriggerGraphRepository.cs
│   │   └── WorkflowTriggerConnectionRepository.cs
│   └── Migrations/
│       └── Migration_20260825000001_CreateWorkflowTriggerTables.cs
├── Application.Contracts/
│   ├── Dtos/OW/TriggerGraph/
│   │   ├── TriggerGraphDto.cs
│   │   ├── TriggerConnectionDto.cs
│   │   ├── SaveTriggerGraphInput.cs
│   │   └── WorkflowNodeInfoDto.cs
│   └── IServices/OW/ITriggerGraphService.cs
├── Application/
│   ├── Maps/TriggerGraphMapProfile.cs
│   └── Services/OW/TriggerGraphService.cs
└── WebApi/Controllers/OW/TriggerGraphController.cs
```

### 5.2 数据库表

**ff_workflow_trigger_graph**

| 列名                 | 类型         | 说明                                           |
| -------------------- | ------------ | ---------------------------------------------- |
| id                   | bigint       | 雪花 ID                                        |
| workflow_id          | bigint       | 一个 Workflow 只有一个 Graph                   |
| name                 | varchar(200) | Graph 名称                                     |
| canvas_layout        | jsonb        | `{"workflowId": {"x": 100, "y": 200}}`         |
| canvas_workflow_ids  | jsonb        | `["id1", "id2"]`，画布上除 owner 外的 Workflow |
| tenant_id / app_code | varchar      | 多租户                                         |

**ff_workflow_trigger_connection**

| 列名                 | 类型         | 说明                                   |
| -------------------- | ------------ | -------------------------------------- |
| id                   | bigint       | 雪花 ID                                |
| graph_id             | bigint       | 关联 Graph                             |
| source_workflow_id   | bigint       | 触发源                                 |
| target_workflow_id   | bigint       | 被触发目标                             |
| rule_name            | varchar(200) | 用户定义的规则名                       |
| condition_summary    | varchar(500) | 连线标签摘要，如 `deal_value > 100000` |
| config_json          | jsonb        | 完整配置（见 3.3 节格式）              |
| is_enabled           | boolean      | 启用状态                               |
| execution_order      | int          | 同源多条时的执行顺序                   |
| tenant_id / app_code | varchar      | 多租户                                 |

### 5.3 API

**Base URL：** `ow/trigger-graph/v1`

| Method | Path                                | 权限            | 说明                                |
| ------ | ----------------------------------- | --------------- | ----------------------------------- |
| GET    | `/{workflowId}`                     | WORKFLOW:READ   | 获取 Graph + 所有 connections       |
| POST   | `/`                                 | WORKFLOW:UPDATE | 保存 Graph（全量替换 connections）  |
| GET    | `/workflows/all`                    | WORKFLOW:READ   | 所有 Workflow 列表（左侧面板）      |
| GET    | `/workflows/{workflowId}/node-info` | WORKFLOW:READ   | Stage + Fields/Questions/Tasks 聚合 |

**node-info 设计：** 后端一次性聚合查询，批量加载所有 Stage 的 Questionnaire questions（解析 structure_json JSONB）和 ChecklistTask，减少前端多次请求。

### 5.4 租户隔离与权限

遵循 `StageConditionService` 模式：

- 所有查询：`_db.Queryable<T>().Where(w => w.TenantId == tenantId && w.AppCode == appCode)`
- 读取：`ValidateWorkflowPermissionAsync(workflowId, OperationTypeEnum.View)`
- 写入：`ValidateWorkflowPermissionAsync(workflowId, OperationTypeEnum.Operate)`
- Service-to-Service（Client Credentials schema）：自动跳过权限校验

### 5.5 SaveAsync 全量替换逻辑

```csharp
// 1. Upsert Graph（无则创建，有则更新 layout）
// 2. 软删除该 Graph 下所有旧 connections（is_valid = false）
// 3. 批量插入新 connections，设置 tenant_id / app_code / execution_order
```

---

## 6. 路由

```ts
// router/routers/modules/workflow.ts
{
  path: 'workflow/:workflowId/triggers',
  component: () => import('@/views/onboard/workflow/trigger-editor.vue'),
}
```

入口：Workflow 详情页顶部 `Trigger Rules` 按钮，路由跳转并带 `workflowId`。

---

## 7. 已知限制与后续 TODO

| 功能                 | JIRA    | 状态       | 说明                                   |
| -------------------- | ------- | ---------- | -------------------------------------- |
| 异步触发执行引擎     | OW-724  | ⏳ pending | Stage Complete 时评估条件、创建 Case   |
| Data Mapping 执行    | OW-724  | ⏳ pending | 拷贝字段值到目标 Case                  |
| Case Related Cases   | OW-728  | ⏳ pending | Case 详情页展示上下游 Case             |
| Trigger History      | OW-729  | ⏳ pending | 触发日志（Success / Skipped / Failed） |
| 多对一汇聚触发       | Phase 2 | ⏳ pending | ALL / ANY 上游完成才触发               |
| 循环触发检测         | Phase 2 | ⏳ pending | A→B→C→A 环路检测                       |
| File attachment 映射 | Phase 2 | ⏳ pending | 文件附件类型的 Data Mapping            |
| 乐观锁并发控制       | Phase 2 | ⏳ pending | 多人同时编辑 Graph 的冲突处理          |

## 8. 待产品确认的设计问题

以下问题需求文档 OW-719 中未明确定义，**须产品给出答案后方可在 OW-724 中实现**，不可自行假设。

### 8.1 新 Case 的基础字段如何填充

触发下游 Workflow 时需创建新 Case，`ff_onboarding` 表的必填字段（`case_name`、`contact_email` 等）从哪里来？

**待确认**：是继承源 Case 的对应字段？还是通过 Data Mapping 用户显式配置？还是使用默认值留空？

### 8.2 Roll Back 后再次 Complete 是否重复触发

场景：源 Case 最后一个 Stage Complete → 触发下游创建了新 Case → 用户将该 Stage Roll Back → 重新 Complete。

**待确认**：是否应再次触发下游？（防止重复 vs 支持重试）

---

## 9. 已知技术缺陷（实现 OW-724 前需修复）

### 9.1 ~~node-info 缺少 component_id~~ ✅ 已修复

前端选项 ID 改为使用与项目规范一致的 `fieldPath` 格式（与 `ruleUtils.ts` / `ConditionRuleForm.vue` 的既有规范对齐）：

| 类型                   | 选项 value（id）格式                                               |
| ---------------------- | ------------------------------------------------------------------ |
| Required Field         | `input.fields.{fieldId}`                                           |
| Questionnaire question | `input.questionnaire.answers["{questionnaireId}"]["{questionId}"]` |

这与 `ConditionRuleForm.vue` 中的 `fieldPath` 格式完全一致，OW-724 执行引擎可以直接复用现有的 `ConvertFrontendRulesToRulesEngineFormat` 逻辑解析，无需额外适配。
