# OW-736 权限模型重构技术方案

**Ticket:** OW-736 — Optimize Workflow, Case and Stage Permission Model with Template and Runtime Permissions  
**优先级:** P0  
**负责人:** Kai Li  
**文档状态:** 设计中

---

## 背景与问题

现有权限模型存在以下五个缺陷：

1. Workflow Permission 同时用于模板编辑和实际 Case 运行，两个场景没有区分
2. Stage Permission 只控制 Workflow Builder 里的配置，不控制实际 Case 中的 Stage
3. Case Permission 可以设置比 Workflow 更大的范围（权限可反向扩大）
4. Case Stage 没有独立的 Runtime Permission
5. Roll Back Teams 可以选没有 Stage Operate Permission 的 Team

---

## 权限模型原则

### 层级关系

```
Workflow Template Permission    → 控制谁能编辑 Workflow Builder 中的模板
        ↓ 快照复制（Case 创建时）
Workflow Runtime Permission     → Case 的最大权限上限（不随 Workflow 变更而联动）
        ↓ 快照复制（Case 创建时）
Case Permission                 → 在 Workflow Runtime 快照范围内可进一步收紧
        ↓
Case Stage Permission           → 受 Case Permission + Workflow Stage Runtime 双重约束
```

### 核心约束规则

- **Child Permission ≤ Parent Permission**（下级只能收紧，不能扩大）
- **Operate Permission ≤ View Permission**（操作权限不能超过查看权限）
- **Template 与 Runtime 完全独立**：有模板编辑权限不代表有 Case 访问权限，反之亦然
- **快照语义**：Case 创建时从 Workflow Runtime 复制权限作为上限，之后 Workflow Runtime 的变更只影响新 Case，不影响已有 Case

### ViewPermissionMode 适用范围

| 层级 | Public | Visible to | Invisible to | Private |
|---|---|---|---|---|
| Workflow Template | ✓ | ✓ | ✓ | — |
| Workflow Runtime | ✓ | ✓ | ✓ | — |
| Stage Template | ✓ | ✓ | ✓ | — |
| Stage Runtime | ✓ | ✓ | ✓ | — |
| Case | ✓ | ✓ | ✓ | ✓ |
| Case Stage | ✓ | ✓ | ✓ | — |

---

## 数据模型变更

### 1. `ff_workflow` 表新增字段（需要 Migration）

现有字段 `view_permission_mode / view_teams / operate_teams / use_same_team_for_operate` **语义不变，保留为 Template Permission**。

新增 Runtime Permission 字段：

| 字段名 | 类型 | 默认值 | 说明 |
|---|---|---|---|
| `runtime_use_same_as_template` | bool | `true` | 勾选时 Runtime 直接复用 Template 权限 |
| `runtime_view_permission_mode` | smallint | `0` (Public) | Runtime View 权限模式 |
| `runtime_view_teams` | jsonb | null | Runtime 可查看的 Team 列表 |
| `runtime_operate_teams` | jsonb | null | Runtime 可操作的 Team 列表（独立配置，无 use_same_team_for_operate） |
| `runtime_use_same_team_for_operate` | bool | `true` | 仅在 `runtime_use_same_as_template=true` 时有意义，独立配置时 Runtime Operate 直接存 Team 列表 |

**生效值计算逻辑（Effective Runtime）：**
```
if runtime_use_same_as_template == true:
    effective_runtime = template_permission
else:
    effective_runtime = runtime_* 字段
```

### 2. `ff_stage` 表新增字段（需要 Migration）

现有字段 `view_permission_mode / view_teams / operate_teams / use_same_team_for_operate` **保留为 Template Permission**。

现有字段 `roll_back_teams` **重新定位为 Runtime 层**（原设计本就是控制实际 Case 中的 rollback 行为，与 Template 层语义不同）。

新增 Template 继承标志 + Runtime Permission 字段：

| 字段名 | 类型 | 默认值 | 说明 |
|---|---|---|---|
| `template_use_same_as_workflow` | bool | `true` | Stage Template 是否继承 Workflow Template 权限。true = 跟随 Workflow Template 变更自动生效；false = 独立配置（存 view_teams/operate_teams） |
| `runtime_use_same_as_workflow` | bool | `true` | 勾选时 Stage Runtime 直接复用 Workflow Runtime 权限 |
| `runtime_view_permission_mode` | smallint | `0` (Public) | Stage Runtime View 权限模式 |
| `runtime_view_teams` | jsonb | null | Stage Runtime 可查看的 Team 列表 |
| `runtime_operate_teams` | jsonb | null | Stage Runtime 可操作的 Team 列表 |
| `runtime_use_same_team_for_operate` | bool | `true` | Stage Runtime Operate 是否复用 View Teams |

**生效值计算逻辑（Effective Stage Runtime）：**
```
if runtime_use_same_as_workflow == true:
    effective_stage_runtime = workflow_runtime_permission
else:
    effective_stage_runtime = stage.runtime_* 字段
    约束：effective_stage_runtime ≤ workflow_runtime_permission（只能是其子集）
```

> **注意：** Stage Runtime 独立配置时，只能从 Workflow Runtime 已授权的 Team 范围内选择，前端 choosable tree 需要限制，后端也需要校验。

### 3. `ff_onboarding` 表新增字段（需要 Migration）

现有权限字段（`view_permission_mode / view_teams / view_users / operate_teams / operate_users / use_same_team_for_operate / view_permission_subject_type / operate_permission_subject_type`）**保留，作为 Case 的实际权限配置**（用户可在上限范围内收紧）。

新增"最大范围快照"字段，Case 创建时从 Workflow Runtime 复制，**之后只读，不随 Workflow 变更联动**：

| 字段名 | 类型 | 默认值 | 说明 |
|---|---|---|---|
| `max_view_permission_mode` | smallint | null | 快照自 Workflow Runtime view_permission_mode |
| `max_view_teams` | jsonb | null | 快照自 Workflow Runtime 的最大可查看 Team 列表 |
| `max_operate_teams` | jsonb | null | 快照自 Workflow Runtime 的最大可操作 Team 列表 |

**Case 创建时的写入逻辑：**
1. 计算 Workflow 的 Effective Runtime Permission（考虑 `runtime_use_same_as_template`）
2. 将结果写入 `max_view_permission_mode / max_view_teams / max_operate_teams`
3. Case 的实际权限字段初始设为 Public（等同于继承），前端显示「Inherit from Workflow」

**快照一旦写入后，用户如需重新应用 Workflow 最新 Runtime，需手动触发"重新应用 Workflow Permission"操作**（不自动同步）。

### 4. `OnboardingStageProgress`（JSON 内加字段，无需 Migration）

在 C# 类中新增，JSON 反序列化向后兼容（旧数据中这些字段为 null，视为继承状态）。

**Case Stage 实际配置（用户可收紧）：**

```csharp
/// <summary>
/// 是否继承 Workflow Stage Runtime Permission（默认 true）
/// true = 直接使用 Workflow Stage Runtime；false = 使用下方独立配置
/// </summary>
public bool StagePermissionInheritFromWorkflowStage { get; set; } = true;

/// <summary>Case Stage View Permission Mode</summary>
public ViewPermissionModeEnum? StageViewPermissionMode { get; set; }

/// <summary>Case Stage View Teams</summary>
public List<string> StageViewTeams { get; set; }

/// <summary>Case Stage View Users（Individual Users 模式）</summary>
public List<string> StageViewUsers { get; set; }

/// <summary>Case Stage View Permission Subject Type（Team / User）</summary>
public PermissionSubjectTypeEnum StageViewPermissionSubjectType { get; set; } = PermissionSubjectTypeEnum.Team;

/// <summary>Case Stage Operate Teams</summary>
public List<string> StageOperateTeams { get; set; }

/// <summary>Case Stage Operate Users</summary>
public List<string> StageOperateUsers { get; set; }

/// <summary>Case Stage Operate 是否复用 View</summary>
public bool StageUseSameTeamForOperate { get; set; } = true;

/// <summary>Case Stage Roll Back 是否继承 Workflow Stage Roll Back</summary>
public bool StageRollBackInherit { get; set; } = true;

/// <summary>Case Stage Roll Back Teams</summary>
public List<string> StageRollBackTeams { get; set; }

/// <summary>Case Stage Roll Back Users</summary>
public List<string> StageRollBackUsers { get; set; }
```

**最大范围快照（Case 创建时从 Workflow Stage Runtime 复制，只读）：**

```csharp
/// <summary>快照自 Workflow Stage Effective Runtime View Teams</summary>
public List<string> MaxStageViewTeams { get; set; }

/// <summary>快照自 Workflow Stage Effective Runtime Operate Teams</summary>
public List<string> MaxStageOperateTeams { get; set; }

/// <summary>快照自 Workflow Stage Effective Runtime Roll Back Teams</summary>
public List<string> MaxStageRollBackTeams { get; set; }
```

**空值语义规则：**
- `StagePermissionInheritFromWorkflowStage = true`（或字段为 null，向后兼容）→ 等同继承，使用 `MaxStage*` 快照值
- `StagePermissionInheritFromWorkflowStage = false` → 必须有有效的 View Permission 配置（不能为空）
- `StageOperateTeams` 为空 → 没有操作权限（有别于 View，Operate 允许为空）
- `StageRollBackTeams` 为空且 `StageRollBackInherit = false` → 没有 Roll Back 权限

---

## Service 层改造

### 4.1 Case 创建时快照写入

**位置：** `OnboardingCrudService.CreateAsync`

```
1. 读取 Workflow 实体
2. 计算 Workflow Effective Runtime Permission（考虑 runtime_use_same_as_template）
3. 写入 Onboarding.max_view_permission_mode / max_view_teams / max_operate_teams
4. 遍历每个 Stage：
   a. 计算 Stage Effective Runtime Permission（考虑 runtime_use_same_as_workflow）
   b. 写入对应 OnboardingStageProgress.MaxStageViewTeams / MaxStageOperateTeams / MaxStageRollBackTeams
5. Case 实际权限初始设为 Public（继承状态）
```

### 4.2 CasePermissionService 改造

**改动：** 原 Public 模式下继承 Workflow 的逻辑，改为读 `Onboarding.max_view_teams` 快照

```
原逻辑：Public → 查询 Workflow 实体 → 取 Workflow.ViewTeams 做权限判断
新逻辑：Public（继承）→ 直接读 Onboarding.max_view_teams 快照 → 做权限判断
```

好处：去掉一次 Workflow 查询，逻辑更简单，且不受 Workflow 后续修改影响。

**后端校验：** 当 Case 权限配置为非继承时，校验 `view_teams` 是 `max_view_teams` 的子集，否则返回业务错误。

### 4.3 StagePermissionService 改造

新增 Case Stage 层（第三层）权限判断：

```
原来：用户权限 = Workflow View ∩ Stage View
新增：用户权限 = Workflow Runtime View ∩ Stage Runtime View ∩ Case Stage View
```

具体逻辑：

```
1. 读 OnboardingStageProgress.StagePermissionInheritFromWorkflowStage
2. 若 true：使用 MaxStageViewTeams 作为该 Stage 的 Case 级权限
3. 若 false：使用 StageViewTeams / StageViewUsers
4. 最终有效权限 = Case.effective_view ∩ stage_case_view
```

### 4.4 Roll Back 权限校验改造

**位置：** Roll Back 操作的权限检查

```
原逻辑：检查 Stage.RollBackTeams（Template 层字段）
新逻辑：
  1. 读 OnboardingStageProgress.StageRollBackInherit
  2. 若 true：使用 MaxStageRollBackTeams（= Stage.RuntimeRollBackTeams 快照）
  3. 若 false：使用 StageRollBackTeams / StageRollBackUsers
  4. 校验执行者在有效 RollBack 权限范围内
```

---

## 前端改造

### 5.1 Workflow Edit — Permissions Tab 重做

**入口一：** Edit Workflow 弹窗 → Permissions tab（与 Basic Info tab 并列）
**入口二：** Workflow 详情页（`/onboard/onboardWorkflow?id=xxx`）右上角独立的 **Permissions 按钮**

两个入口打开同一个 **Workflow Permissions 弹窗**，标题"Workflow Permissions"，副标题为 Workflow 名称。

#### Template Permissions 区块

副标题："Control who can view and operate this workflow template."

**View Permission（左侧）：**
- 下拉：Public / Visible to / Invisible to
- Visible to：显示 Team 多选，下方展示"People who can view this stage in the Workflow Builder. EFFECTIVE TEAMS: [...]"
- Invisible to：显示"Hidden from Team"多选，下方展示 Effective Teams（排除后的 Team 列表）

**Operate Permission（右侧）：**
- Checkbox：`Use same team that have view permission`（未勾选时展开独立 Team 选择器）
- 未勾选时展示独立 Team 多选，下方提示："Can only include teams that also have view permission. EFFECTIVE TEAMS: [...]"
- Invisible to 模式下，Operate 显示为普通 Team 选择器（无 Use same checkbox，因为 Invisible to 的 Operate 无法直接复用黑名单语义）

#### Runtime Permissions 区块

副标题："Set the maximum runtime access for cases and stages using this workflow."

顶部 Checkbox：`Use same permissions as Template Permissions`（默认未勾选）
- **勾选时**：Runtime View / Operate 均显示继承状态 + Effective Teams（只读）
- **未勾选时**：展开独立配置

**Runtime View Teams（左侧，未勾选状态）：**
- 标题："Runtime View Teams"
- 下拉：Public / Visible to / Invisible to
- Public：下方提示 "EFFECTIVE TEAMS: Public — everyone in the company"
- Visible to：Team 多选 + "People who can view cases created from this workflow. EFFECTIVE TEAMS: [...]"
- Invisible to："Hidden from Team" 多选 + Effective Teams（排除后列表）

**Runtime Operate Teams（右侧，未勾选状态）：**
- 标题："Runtime Operate Teams"
- **无** `Use same team` checkbox（与 Template 侧不同，Runtime Operate 是独立的 Team 选择器）
- Team 多选 + "People who can operate on cases created from this workflow. EFFECTIVE TEAMS: [...]"
- Invisible to 模式下同样无 Use same，直接显示 Team 选择器

### 5.2 Stage Edit — Permissions Tab 重做

Edit Stage 弹窗 → Permissions tab（与 Basic Info / Components tab 并列）。

#### Template Permissions 区块

副标题："Configure who can view and operate this stage inside the Workflow Builder"

顶部 Checkbox：`Use same permissions as Workflow Template Permissions`（默认勾选）
- **勾选时**：View / Operate 均显示只读预览框："Matches the workflow's Template view/operate permission. EFFECTIVE TEAMS: [...]"
- **未勾选时**：展开独立配置
  - View：下拉（Public / Visible to / Invisible to）+ Team 多选 + "People who can view this stage in the Workflow Builder. EFFECTIVE TEAMS: [...]"
  - Operate：Checkbox `Use same team that have view permission` + 独立 Team 多选（限制在 View Teams 范围内）+"Can only include teams that also have view permission. EFFECTIVE TEAMS: [...]"

#### Runtime Permissions 区块

副标题："Set the default access for this stage when used in a case."

顶部 Checkbox：`Use same permission as Workflow Runtime Permissions`（默认勾选）
- **勾选时**：View / Operate 均显示只读预览框："Matches the workflow's Runtime view/operate permission. EFFECTIVE TEAMS: [...]"
- **未勾选时**：展开独立配置
  - Runtime View Teams：下拉（Public / Visible to / Invisible to）+ Team 多选 + "People who can view cases at this stage. EFFECTIVE TEAMS: [...]"
  - Runtime Operate Teams：独立 Team 选择器（无 Use same checkbox）+ "Can only include teams that also have view permission at this stage. EFFECTIVE TEAMS: [...]"

#### Roll Back Teams 区块（在 Runtime Permissions 下方）

标题："Roll Back Teams"
副标题："Choose teams allowed to roll this stage back. Only teams with Runtime Operate permission can be selected."

- Team 多选选择器，choosable 范围**限制在 Runtime Operate Teams 内**
- 下方提示："Can only include teams that also have Runtime Operate permission. EFFECTIVE TEAMS: [...]"
- 与现有 `Stage.RollBackTeams` 字段对应（该字段重新定位为 Runtime 层）

### 5.3 Case Create/Edit — Access Control 区块改造

Edit Case / Add Case 弹窗中的 **Access Control** 区块。

**区块说明文字（固定显示）：**
> "Case permission can only narrow the workflow's permission, copied in at creation — it can't grant more access than the workflow (or Case Operate more than Case View), and later workflow changes won't affect existing cases."

顶部 Checkbox：`Use same permission as Workflow Runtime Permissions`（默认未勾选）
- **勾选时**：回到继承状态，View / Operate 显示 Effective Teams（只读，来自快照）
- **未勾选时**：展开独立配置（现有逻辑）

**View Permission（左侧）：**
- 下拉：Public / Visible to / Invisible to / Private
- Visible to：User Teams / Individual Users 切换 + 对应选择器 + "People who can view this case. EFFECTIVE TEAMS/USERS: [...]"
- Invisible to：User Teams / Individual Users + "People who can view this case. EFFECTIVE TEAMS: [...]"（排除后的范围）
- Private：仅显示 Individual Users 的用户选择器 + "People who can view this case. EFFECTIVE USERS: [...]"
- 所有模式下 choosable 范围限制在 `max_view_teams / max_view_users` 快照内

**Operate Permission（右侧）：**
- Checkbox：`Use same teams and users that have view permission`（默认勾选）
- 未勾选时：展开独立的 User Teams / Individual Users 选择，choosable 范围限制在 Case View 选择结果内
- 下方始终显示："Same people as View permission / EFFECTIVE TEAMS/USERS: [...]"

> **注意：** 没有单独的"重新应用 Workflow Permission"按钮，用户通过重新勾选顶部 checkbox 实现同等效果（勾选后再保存即为直接覆盖到当前快照状态，即以 Workflow 最新 Runtime 为准）。

### 5.4 Case Stage Permission — 全新弹窗

**触发入口：** Case 详情页右侧 **Case Progress** 面板，每个 Stage 卡片右侧的**护盾图标（shield icon）**，点击弹出。

新建 `CaseStagePermissionDialog.vue`，弹窗标题为 "Case Stage Permission"，副标题为 Stage 名称。

#### 状态一：继承（默认，`StagePermissionInheritFromWorkflowStage = true`）

```
[✓] Use same permission as workflow stage runtime

View Permission                    Operate Permission
Controls who can view ...          Controls who can operate ...
┌─────────────────────────────┐   ┌─────────────────────────────┐
│ Matches the workflow stage's │   │ Matches the workflow stage's │
│ runtime view permission.     │   │ runtime operate permission.  │
│ 🧑 EFFECTIVE TEAMS           │   │ 🧑 EFFECTIVE TEAMS           │
│ [US Accounting Team]         │   │ [Account Set Up Team]        │
│ [Account Set Up Team]        │   └─────────────────────────────┘
└─────────────────────────────┘

Roll Back Permission
Controls who can reopen this stage and edit it again
┌──────────────────────────────────────────────────────────┐
│ Matches the workflow stage's runtime roll back permission. │
│ 🧑 EFFECTIVE TEAMS                                         │
│ [Account Set Up Team]                                      │
└──────────────────────────────────────────────────────────┘
```

#### 状态二：取消继承，View 选 Visible to（User Teams）

```
[ ] Use same permission as workflow stage runtime

View Permission                    Operate Permission
[⊙ Visible to ▼]                  [✓] Use same teams and users that have view permission
Only the teams or people you       Same people as View permission.
pick below can view it.            🧑 EFFECTIVE TEAMS
Team                               No access
[User Teams] [Individual Users]
[Select teams ▼]
People who can view the case at this stage.
🧑 EFFECTIVE TEAMS  No access

Roll Back Permission
[✓] Use same teams and users that have operate permission
Same people as Operate permission.
🧑 EFFECTIVE TEAMS  No access
```

#### 状态三：取消继承，View 选 Invisible to

```
[ ] Use same permission as workflow stage runtime

View Permission                    Operate Permission
[⊙ Invisible to ▼]                [✓] Use same teams and users that have view permission
Everyone can view it, except       Same people as View permission.
the teams or people you            🧑 EFFECTIVE TEAMS
pick below.                        [Account Set Up Team]
Team
[User Teams] [Individual Users]
[Select teams ▼]
People who can view the case at this stage.
🧑 EFFECTIVE TEAMS  [US Accounting Team] [Account Set Up Team]

Roll Back Permission
[✓] Use same teams and users that have operate permission
Same people as Operate permission.
🧑 EFFECTIVE TEAMS  [Account Set Up Team]
```

#### 状态四：Operate 取消复用 View，独立配置 Individual Users

```
Operate Permission
[ ] Use same teams and users that have view permission
Teams
[User Teams] [Individual Users ●]
[Select users ▼]
People who can operate on the case at this stage.
🧑 EFFECTIVE USERS  No access
```

#### 状态五：Roll Back 取消复用 Operate，独立配置

```
Roll Back Permission
Controls who can reopen this stage and edit it again
[ ] Use same teams and users that have operate permission
Teams
[User Teams] [Individual Users ●]
[Select users ▼]
People who can roll back this stage.
🧑 EFFECTIVE USERS  No access
```

> **注意：** Case Stage View 下拉只提供 Public / Visible to / Invisible to 三个选项，不含 Private（按 BA Q5 确认）。Roll Back 独立配置时支持 User Teams 和 Individual Users 两种维度。

#### 保存校验规则

| 字段 | 规则 |
|---|---|
| View Permission | 非继承状态下，Visible to 时必须至少选一个 Team/User |
| Operate Permission | 可为空（代表没有操作权限） |
| Roll Back Permission | 可为空（代表没有 Roll Back 权限） |
| 整体约束 | **Operate ⊆ View ∩ Workflow Stage Runtime Operate**；Roll Back ⊆ Operate effective 范围 |

---

## API 接口变更设计

### 现状

| 接口 | 路径 | 现有权限字段 |
|---|---|---|
| Workflow CRUD | `PUT /ow/workflows/v1/{id}` | Template: ViewPermissionMode, ViewTeams, OperateTeams, UseSameTeamForOperate |
| Stage CRUD | `PUT /ow/stages/v1/{id}` | Template 同上 + RollBackTeams |
| Case CRUD | `PUT /ow/onboardings/v1/{id}` | ViewPermissionMode, ViewTeams/Users, OperateTeams/Users, UseSameTeamForOperate, SubjectType |
| CheckPermission | `POST /ow/permissions/v1/check` | 返回 CanView + CanOperate |
| Stage Progress | 内嵌在 OnboardingOutputDto | Permission(PermissionInfoDto) + CanRollBack |

不需要新增独立的 Controller，只在现有 DTO 上加字段 + 新增 2 个子资源端点。

---

### 6.1 Workflow DTO 变更

**`WorkflowInputDto` 新增：**

```csharp
// Runtime Permissions
public bool RuntimeUseSameAsTemplate { get; set; } = true;
public ViewPermissionModeEnum RuntimeViewPermissionMode { get; set; } = ViewPermissionModeEnum.Public;
public List<string> RuntimeViewTeams { get; set; }
public List<string> RuntimeOperateTeams { get; set; }
```

**`WorkflowOutputDto` 新增：**

```csharp
// Runtime 配置（用于回显）
public bool RuntimeUseSameAsTemplate { get; set; }
public ViewPermissionModeEnum RuntimeViewPermissionMode { get; set; }
public List<string> RuntimeViewTeams { get; set; }
public List<string> RuntimeOperateTeams { get; set; }

// Effective Runtime（后端计算好，前端直接展示 Effective Teams）
public ViewPermissionModeEnum EffectiveRuntimeViewPermissionMode { get; set; }
public List<string> EffectiveRuntimeViewTeams { get; set; }
public List<string> EffectiveRuntimeOperateTeams { get; set; }
```

> Runtime Operate Teams 在 Workflow 层是独立选择器（无 `use_same_team_for_operate` 标志），所以没有对应的 `RuntimeUseSameTeamForOperate` 字段。

---

### 6.2 Stage DTO 变更

**`StageInputDto` 新增：**

```csharp
// Template 继承标志（true = 跟随 Workflow Template 权限）
public bool TemplateUseSameAsWorkflow { get; set; } = true;
// 未继承时用现有 ViewTeams/OperateTeams/UseSameTeamForOperate 字段

// Runtime Permissions
public bool RuntimeUseSameAsWorkflow { get; set; } = true;
public ViewPermissionModeEnum RuntimeViewPermissionMode { get; set; } = ViewPermissionModeEnum.Public;
public List<string> RuntimeViewTeams { get; set; }
public List<string> RuntimeOperateTeams { get; set; }
public bool RuntimeUseSameTeamForOperate { get; set; } = true;
// RollBackTeams 已有字段，语义重新定位为 Runtime 层，字段名不变
```

**`StageOutputDto` 新增：**

```csharp
// 继承标志（回显）
public bool TemplateUseSameAsWorkflow { get; set; }
public bool RuntimeUseSameAsWorkflow { get; set; }
public ViewPermissionModeEnum RuntimeViewPermissionMode { get; set; }
public List<string> RuntimeViewTeams { get; set; }
public List<string> RuntimeOperateTeams { get; set; }
public bool RuntimeUseSameTeamForOperate { get; set; }

// Effective（后端计算，前端只读展示）
public ViewPermissionModeEnum EffectiveRuntimeViewPermissionMode { get; set; }
public List<string> EffectiveRuntimeViewTeams { get; set; }
public List<string> EffectiveRuntimeOperateTeams { get; set; }
public List<string> EffectiveRollBackTeams { get; set; }
```

---

### 6.3 Case DTO 变更

**`OnboardingInputDto` 新增：**

```csharp
// 继承标志（勾选时 = Use same permission as Workflow Runtime）
public bool UseWorkflowRuntimePermission { get; set; } = true;
// 未勾选时用现有 ViewPermissionMode/ViewTeams/ViewUsers/OperateTeams/OperateUsers 字段
```

**`OnboardingOutputDto` 新增：**

```csharp
// 继承标志（回显）
public bool UseWorkflowRuntimePermission { get; set; }

// 最大范围快照（Case 创建时从 Workflow Runtime 复制，只读，前端用于限制 choosable tree）
public ViewPermissionModeEnum MaxViewPermissionMode { get; set; }
public List<string> MaxViewTeams { get; set; }
public List<string> MaxOperateTeams { get; set; }
```

---

### 6.4 Case Stage Permission — 新增独立端点

Stage Progress 目前没有独立的权限 CRUD，需要新增：

**`PUT /ow/onboardings/v1/{id}/stage-permissions/{stageId}`**

```csharp
public class CaseStagePermissionInputDto
{
    // 顶部继承 checkbox
    public bool InheritFromWorkflowStage { get; set; } = true;

    // View（未继承时填）
    public ViewPermissionModeEnum? ViewPermissionMode { get; set; }
    public PermissionSubjectTypeEnum ViewPermissionSubjectType { get; set; }
    public List<string> ViewTeams { get; set; }
    public List<string> ViewUsers { get; set; }

    // Operate
    public bool UseSameTeamForOperate { get; set; } = true;
    public PermissionSubjectTypeEnum OperatePermissionSubjectType { get; set; }
    public List<string> OperateTeams { get; set; }
    public List<string> OperateUsers { get; set; }

    // Roll Back
    public bool RollBackInherit { get; set; } = true;
    public bool RollBackUseSameAsOperate { get; set; } = true;
    public PermissionSubjectTypeEnum RollBackPermissionSubjectType { get; set; }
    public List<string> RollBackTeams { get; set; }
    public List<string> RollBackUsers { get; set; }
}
```

**`OnboardingStageProgressDto` 新增：**

```csharp
// Case Stage 实际配置（回显）
public bool StagePermissionInheritFromWorkflowStage { get; set; }
public ViewPermissionModeEnum? StageViewPermissionMode { get; set; }
public PermissionSubjectTypeEnum StageViewPermissionSubjectType { get; set; }
public List<string> StageViewTeams { get; set; }
public List<string> StageViewUsers { get; set; }
public bool StageUseSameTeamForOperate { get; set; }
public PermissionSubjectTypeEnum StageOperatePermissionSubjectType { get; set; }
public List<string> StageOperateTeams { get; set; }
public List<string> StageOperateUsers { get; set; }
public bool StageRollBackInherit { get; set; }
public bool StageRollBackUseSameAsOperate { get; set; }
public PermissionSubjectTypeEnum StageRollBackPermissionSubjectType { get; set; }
public List<string> StageRollBackTeams { get; set; }
public List<string> StageRollBackUsers { get; set; }

// Effective（后端计算好，前端弹窗继承状态展示用）
public List<string> EffectiveViewTeams { get; set; }
public List<string> EffectiveOperateTeams { get; set; }
public List<string> EffectiveRollBackTeams { get; set; }
```

---

### 6.5 CheckPermission 接口无需改动

`POST /ow/permissions/v1/check` 契约不变（CanView + CanOperate），只需更新后端计算逻辑（改为读快照字段）。前端调用方式不变。

---

### 6.6 新增 Workflow Runtime 快照重置端点

对应 Case Edit 里"重新应用 Workflow Permission"操作：

**`POST /ow/onboardings/v1/{id}/reapply-workflow-permission`**

无 Body。后端从 Workflow 当前 Effective Runtime 重新计算，覆盖 Case 的 `max_*` 快照字段，返回 `SuccessResponse<bool>`。

| 模块 | 改动类型 | 预估工作量 |
|---|---|---|
| `ff_workflow` Migration | 新增 5 个字段 | XS |
| `ff_stage` Migration | 新增 5 个字段 | XS |
| `ff_onboarding` Migration | 新增 3 个字段 | XS |
| `Workflow` Entity | 加字段 + DTO 更新 | S |
| `Stage` Entity | 加字段 + DTO 更新 | S |
| `Onboarding` Entity | 加字段 + DTO 更新 | S |
| `OnboardingStageProgress` C# 类 | 加字段，无 Migration | S |
| Case 创建快照逻辑 | `OnboardingCrudService` 改造 | M |
| `CasePermissionService` | 改继承来源为快照 | S |
| `StagePermissionService` | 新增 Case Stage 层 | M |
| Roll Back 权限校验 | 改为读 Case Stage 快照 | S |
| Workflow/Stage 权限 API | Runtime 字段的 CRUD + 验证 | M |
| 前端 Workflow Permissions Tab | 重做 | L |
| 前端 Stage Permissions Tab | 重做 | L |
| 前端 Case Access Control | 改造 + choosable tree 限制 | M |
| 前端 Case Stage Permission 弹窗 | 全新组件 | L |

---

## 遗留问题 / 待确认

1. ~~**"重新应用 Workflow Permission"功能的交互**~~ ✅ **已确认**：用户点击后直接覆盖快照，无需二次确认预览。

2. ~~**已有 Case 的快照迁移**~~ ✅ **已确认**：Migration 时批量回填，以 Workflow 当前 Runtime 为准写入历史 Case 的 `max_*` 快照字段。

3. ~~**Case Stage Permission 入口**~~ ✅ **已确认**：入口在 Case 详情页右侧 **Case Progress** 面板中，每个 Stage 卡片右侧有一个**护盾图标（shield icon）**，点击弹出 `CaseStagePermissionDialog`。

4. ~~**Case Stage Permission 的 Private 模式**~~ ✅ **已确认**：按 BA Q5 回答为准，Case Stage 不支持 Private 模式，View 下拉只提供 Public / Visible to / Invisible to 三个选项。截图中的 Private 选项为原型草稿，以文字需求为准。

5. ~~**Roll Back 独立配置时是否只支持 Individual Users**~~ ✅ **已确认**：截图中 User Teams 灰色为设计草稿偶然现象，Roll Back 独立配置时同样支持 User Teams 和 Individual Users 两种维度。

---

## 关联 Ticket

- **OW-737** — Restrict Stage File Access to Users with Operate Permission（相关，权限模型统一后需联动）
- **roll-back-completed-stage spec** — Stage.RollBackTeams 在本次重构中重新定位为 Runtime 层字段
