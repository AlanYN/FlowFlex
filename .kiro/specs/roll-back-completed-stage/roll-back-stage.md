# Roll Back Stage 功能文档

> 最后更新：2026-08-06  
> 涉及 JIRA：OW-695  
> 状态：已上线（dev 环境）

---

## 一、功能背景

Onboarding 流程中，当某个 Stage 被错误地 Complete 之后，需要一种方式将其退回到 InProgress 状态，让负责人重新编辑。

**设计决策**：
- Roll Back **只看单个 Stage 的状态**，与 Onboarding 整体状态（Active/Inactive 等）无关
- 按钮放在每个 Stage 卡片上，不在页面头部
- 只有 `status === 'Completed'` 的 Stage 才会显示按钮
- 权限由后端计算后通过 `canRollBack` 字段下发，前端不重复校验
- 操作后发邮件通知（fire-and-forget，不影响响应），但 Stage 没有配 Assignee 时不发邮件

---

## 二、涉及文件

### 前端

| 文件 | 改动说明 |
|------|----------|
| `packages/flowFlex-common/src/app/apis/ow/onboarding.ts` | 新增 `rollBackStage` API 函数 |
| `packages/flowFlex-common/src/app/views/onboard/onboardingList/components/OnboardingProgress.vue` | Roll Back 按钮 + 确认弹窗 + 逻辑 |
| `packages/flowFlex-common/src/app/views/onboard/onboardingList/detail.vue` | 监听 `stageRolledBack` 事件刷新数据，传 `onboarding-id` prop |
| `packages/flowFlex-common/src/types/onboard.d.ts` | `Stage` 类型新增 `canRollBack?: boolean` |

### 后端

| 文件 | 改动说明 |
|------|----------|
| `packages/flowFlex-backend/Domain/Entities/OW/Stage.cs` | 新增 `RollBackTeams` 字段（JSONB） |
| `packages/flowFlex-backend/Application.Contracts/Dtos/OW/Onboarding/OnboardingStageProgressDto.cs` | 新增 `CanRollBack` 字段 |
| `packages/flowFlex-backend/Application.Contracts/IServices/OW/Onboarding/IOnboardingStageManagementService.cs` | 新增 `RollBackStageAsync` 接口方法 |
| `packages/flowFlex-backend/Application/Services/OW/OnboardingServices/OnboardingStageManagementService.cs` | `RollBackStageAsync` 完整实现 |
| `packages/flowFlex-backend/Application/Services/OW/OnboardingServices/OnboardingQueryService.cs` | `GetProgressAsync` 中填充 `CanRollBack`（用于 `/progress` 端点） |
| `packages/flowFlex-backend/Application/Services/OW/OnboardingServices/OnboardingCrudService.cs` | `PopulateStageActionsAndPermissionsAsync` 中填充 `CanRollBack`（用于 `GET /onboardings/{id}` 端点） |
| `packages/flowFlex-backend/WebApi/Controllers/OW/OnboardingController.cs` | 新增 `POST /{onboardingId}/stages/{stageId}/roll-back` 端点 |
| `packages/flowFlex-backend/SqlSugarDB/Migrations/Migration_20260806000001_AddRollBackTeamsToStage.cs` | 数据库迁移：`ff_stage` 表新增 `roll_back_teams jsonb` 列 |

---

## 三、数据库

### 新增列

```sql
ALTER TABLE ff_stage ADD COLUMN IF NOT EXISTS roll_back_teams jsonb;
```

- 表：`ff_stage`
- 列名：`roll_back_teams`
- 类型：`jsonb`（team ID 数组，如 `["2085287030467727360"]`）
- 默认：`NULL`（表示任何人都不能 Roll Back，安全默认值）
- ORM 映射：`Stage.RollBackTeams`（`string`，SqlSugar `IsJson = true`）

### 查询示例

```sql
-- 查某个 Onboarding 的所有 Stage 的 roll_back_teams 配置
SELECT s.id, s.name, s.roll_back_teams
FROM ff_stage s
WHERE s.id IN (
    SELECT (jsonb_array_elements(stages_progress_json::jsonb)->>'stageId')::bigint
    FROM ff_onboarding
    WHERE id = '{onboarding_id}'
)
AND s.is_valid = true;
```

---

## 四、API

### Roll Back Stage

```
POST /ow/onboardings/{apiVersion}/{onboardingId}/stages/{stageId}/roll-back
```

**权限**：`CASE:UPDATE`

**Request Body**：
```json
{
  "reason": "string (optional)"
}
```

**Response**：`bool`（`true` 表示成功）

**错误码**：
- `DataNotFound`：Onboarding 或 Stage 不存在
- `OperationNotAllowed`：无权限或 Stage 未配置 RollBackTeams
- `BusinessError`：Stage 状态不是 Completed

### GET /onboardings/v1/{id}（已有接口，但新增了字段）

`stagesProgress` 数组中的每个 Stage 现在包含 `canRollBack: bool`，由后端根据权限计算后下发。

---

## 五、权限逻辑

### canRollBack 计算规则

```
isAdmin = IsSystemAdmin || IsTenantAdmin（当前租户）

if isAdmin:
    canRollBack = true
else:
    canRollBack = (用户所在团队 ∩ stage.RollBackTeams).length > 0
```

- **System Admin**（`UserType = 1`）：天然拥有 Roll Back 权限，无需配置 RollBackTeams
- **Tenant Admin**（`UserType = 2`，当前租户）：同上
- **普通用户**：必须属于 Stage 配置的 `RollBackTeams` 中至少一个团队

### 团队数据来源

`UserContext.UserTeams`，在 Token 验证阶段（`TokenValidatedHandler.LoadUserTeamsAsync`）从 IDM 加载。

> **注意**：Admin 用户跳过 LoadUserTeamsAsync，所以 `GetUserTeamIds()` 返回空列表。但 `isAdmin` 判断在前，不影响结果。

### canRollBack 填充的两个位置

1. **`OnboardingQueryService.GetProgressAsync`** → 对应 `GET /onboardings/{id}/progress` 端点
2. **`OnboardingCrudService.PopulateStageActionsAndPermissionsAsync`** → 对应 `GET /onboardings/{id}` 端点（前端详情页调用的是这个）

> ⚠️ **常见踩坑**：两个地方都需要填充，漏了任意一个都会导致前端拿到 `false`。

---

## 六、前端实现

### OnboardingProgress.vue

**按钮显示条件**：
```html
v-if="stage.status === 'Completed' && stage.canRollBack"
```

**canRollBack 透传**：
```typescript
// stages computed 里需要显式透传
canRollBack: (stage as any).canRollBack ?? false,
```

**弹窗注意事项**：
- `el-dialog` 必须加 `append-to-body`，否则被父级 `el-scrollbar` 的 `overflow: hidden` 截断，表现为点击无反应
- 按钮用 `@click.stop` 阻止冒泡，避免触发 stage 行的 `handleStageClick`

**emit 事件**：
- `stageRolledBack`：Roll Back 成功后触发，父组件监听并调用 `loadOnboardingDetail()` 刷新

### detail.vue

```html
<OnboardingProgress
    :onboarding-id="onboardingId"   <!-- 必须传，否则 API 拿不到 ID -->
    @stage-rolled-back="handleStageRolledBack"
/>
```

```typescript
const handleStageRolledBack = async () => {
    await loadOnboardingDetail();
};
```

---

## 七、Roll Back 的业务逻辑

### 后端执行步骤（`RollBackStageAsync`）

1. 加载 Onboarding，初始化 StagesProgress
2. 权限校验（Admin bypass 或 RollBackTeams 白名单）
3. 校验目标 Stage 状态必须为 `Completed`
4. 重置 progress 字段：
   - `Status = "InProgress"`
   - `IsCompleted = false`
   - `CompletionTime = null`
   - `CompletedBy = null`、`CompletedById = null`
   - `IsCurrent = true`
5. 更新 Onboarding 的 `CurrentStageId` 和 `CurrentStageOrder` 指向被退回的 Stage
6. **Onboarding 状态联动**：如果整体状态是 `Completed`，改回 `InProgress`，清空 `ActualCompletionDate`
7. 保存到数据库
8. 写入操作日志（`ff_onboarding_log`）
9. **发邮件通知**（fire-and-forget，Task.Run 异步）：
   - 通知被退回 Stage 的 Assignee
   - 通知该 Stage 之后所有非 Skipped Stage 的 Assignee

### 邮件发送条件

- Stage 必须有 Assignee（`stageProgress.Assignee` 或 `stage.DefaultAssignee`）
- 用户必须有 Email
- dev 环境邮件配置：`appsettings.Development.json` 中 `Email.SmtpServer = smtp.mailgun.org`
- 如果 Stage 没有配 Assignee，**静默跳过，不报错**

---

## 八、Workflow 配置端

Roll Back Teams 在 Workflow 配置页的 Stage 表单中配置：

- 配置入口：`/onboard/workflow/{workflowId}/stages`（Stage 编辑 Form）
- 对应组件：`StageForm.vue`
- 字段：`rollBackTeams`（多选 Team 下拉）
- 数据库字段：`ff_stage.roll_back_teams`（jsonb 数组）

---

## 九、已知问题 & 注意事项

1. **`stages_progress_json` 不含 `canRollBack`**：这个字段存在 `ff_stage` 表，不在 `ff_onboarding.stages_progress_json` JSONB 里。每次 GET 时由服务层实时计算后注入到 DTO，不持久化。

2. **Admin 用户看到按钮但 RollBackTeams 为 null 也能操作**：这是设计行为，Admin bypass 绕过白名单检查。

3. **Roll Back 不影响 Questionnaire 答案**：答案存在 `ff_questionnaire_answer` 表，Roll Back 不清除，用户退回后看到的是之前填写的内容。

4. **Roll Back 不会触发 Stage Condition Actions**：只是把 Stage 状态改回 InProgress，不走 Complete 流程，不触发 condition 评估。

5. **并发安全**：当前实现没有加分布式锁，极端情况下两个人同时 Roll Back 同一个 Stage 可能有竞态，概率极低但理论上存在。
