# Requirements Document

## 简介

OW-736 对 FlowFlex 的权限模型进行全面重构，解决现有模型中五项核心缺陷：

1. Workflow Permission 同时用于模板编辑和 Case 运行，两个场景未区分
2. Stage Permission 只控制 Workflow Builder 中的配置，不控制实际 Case 中的 Stage
3. Case Permission 可以设置比 Workflow 更大的范围（权限可反向扩大）
4. Case Stage 没有独立的 Runtime Permission
5. Roll Back Teams 可以选没有 Stage Operate Permission 的 Team

重构后采用 **Template / Runtime 双层分离** 设计，引入 **快照语义**（Case 创建时从 Workflow Runtime 复制权限上限，之后与 Workflow 变更解耦），形成 `Workflow Runtime → Case → Case Stage` 四级自顶向下收紧的约束链，保证子级权限永远是父级的子集。

技术栈：后端 .NET 8 + SqlSugar ORM + PostgreSQL，前端 Vue 3 + Element Plus + Pinia。

---

## 术语表

- **Workflow_Template_Permission**：控制谁可以在 Workflow Builder 中编辑 Workflow 模板的权限配置（现有字段，语义不变）
- **Workflow_Runtime_Permission**：Case 运行时的最大权限上限，与 Template 完全独立；Case 创建时作为快照来源
- **Stage_Template_Permission**：控制谁可以在 Workflow Builder 中编辑 Stage 模板的权限配置（现有字段，语义不变）
- **Stage_Runtime_Permission**：Stage 在 Case 运行时的访问控制上限，独立于 Template
- **Case_Permission**：单个 Case 的实际访问权限，只能在 Workflow Runtime 快照范围内收紧
- **Case_Stage_Permission**：单个 Case 中某个 Stage 的实际访问权限，受 Case_Permission 与 Stage_Runtime_Permission 双重约束
- **Permission_Snapshot**：Case 创建时从 Workflow Runtime 复制的只读副本，存储于 `ff_onboarding` 的 `max_*` 字段和 `OnboardingStageProgress` 的 `MaxStage*` 字段
- **Effective_Permission**：后端基于继承标志计算出的最终生效权限，供前端只读展示
- **ViewPermissionModeEnum**：`Public`（0）/ `VisibleTo`（1）/ `InvisibleTo`（2）/ `Private`（3，仅 Case 层支持）
- **PermissionSubjectTypeEnum**：`Team` / `User`
- **Permission_Constraint_Chain**：`Workflow_Runtime_Permission ≥ Case_Permission ≥ Case_Stage_Permission` 的父子包含关系，Operate ≤ View 在所有层级成立
- **Roll_Back_Teams**：被允许对某个 Stage 执行回滚操作的 Team/User 列表，定位为 Runtime 层语义
- **OnboardingStageProgress**：存储在 `ff_onboarding` JSONB 列中的 Case Stage 运行时状态对象（无独立数据库表，无需 Migration）
- **ff_workflow**：Workflow 实体对应的数据库表
- **ff_stage**：Stage 实体对应的数据库表
- **ff_onboarding**：Case（Onboarding）实体对应的数据库表
- **CasePermissionService**：后端负责计算 Case 层权限的 Service
- **StagePermissionService**：后端负责计算 Stage 层有效权限的 Service
- **PermissionCalculator**：新增静态辅助类，集中处理 Effective Permission 计算逻辑

---

## 需求列表

### 需求 1：权限模型层级结构与约束规则

**User Story：** 作为系统架构师，我希望建立清晰的 Template/Runtime 双层权限结构，确保权限只能从父级向子级收紧，不允许子级扩大父级权限，从而消除现有权限漏洞。

#### Acceptance Criteria

1. THE Permission_Constraint_Chain SHALL enforce that Case_Permission's authorized team set is a subset of Workflow_Runtime_Permission's authorized team set.
2. THE Permission_Constraint_Chain SHALL enforce that Case_Stage_Permission's authorized team set is a subset of the intersection of Case_Permission and Stage_Runtime_Permission.
3. THE Permission_Constraint_Chain SHALL enforce that Operate permission is always a subset of View permission at every layer (Workflow Runtime, Stage Runtime, Case, and Case Stage).
4. THE Workflow_Template_Permission AND Workflow_Runtime_Permission SHALL be completely independent; possessing template edit permission SHALL NOT grant runtime case access, and vice versa.
5. WHEN a Case is created, THE Permission_Snapshot SHALL be written from the current Workflow_Runtime_Permission (Effective Runtime) and SHALL NOT be automatically updated when Workflow_Runtime_Permission changes after Case creation.
6. IF a user attempts to configure Case_Permission with teams outside the boundary of the stored Permission_Snapshot, THEN THE System SHALL reject the request with a business error.
7. IF a user attempts to configure Stage_Runtime_Permission independently with teams that are not a subset of Workflow_Runtime_Permission, THEN THE System SHALL reject the request with a business error.
8. THE ViewPermissionModeEnum SHALL support Public, VisibleTo, and InvisibleTo at the Workflow Template, Workflow Runtime, Stage Template, and Stage Runtime layers; additionally Private at the Case layer only; Case_Stage_Permission SHALL NOT support Private mode.

---

### 需求 2：ff_workflow 表 Runtime Permission 字段扩展

**User Story：** 作为后端开发者，我希望在 `ff_workflow` 表中新增 Runtime Permission 相关字段，支持 Workflow Runtime 权限的独立配置与快照计算，同时保持现有 Template 字段语义完全不变。

#### Acceptance Criteria

1. THE System SHALL add the following columns to `ff_workflow` via a database migration using `IF NOT EXISTS` for idempotency: `runtime_use_same_as_template` (bool, NOT NULL, default true), `runtime_view_permission_mode` (smallint, NOT NULL, default 0), `runtime_view_teams` (jsonb, nullable), `runtime_operate_teams` (jsonb, nullable).
2. WHEN `runtime_use_same_as_template` is true, THE System SHALL compute Effective_Runtime_Permission by copying the Template Permission fields (`view_permission_mode`, `view_teams`, `operate_teams`).
3. WHEN `runtime_use_same_as_template` is false, THE System SHALL compute Effective_Runtime_Permission from the `runtime_view_permission_mode` and `runtime_*_teams` fields directly.
4. THE Workflow Runtime layer SHALL NOT have a `runtime_use_same_team_for_operate` flag; Runtime Operate Teams SHALL always be an independent selector with a separate list stored in `runtime_operate_teams`.
5. THE System SHALL expose `EffectiveRuntimeViewPermissionMode`, `EffectiveRuntimeViewTeams`, and `EffectiveRuntimeOperateTeams` as computed-only output fields in `WorkflowOutputDto`; these fields SHALL NOT be persisted as separate columns.
6. THE existing columns `view_permission_mode`, `view_teams`, `operate_teams`, and `use_same_team_for_operate` in `ff_workflow` SHALL retain their existing semantics as Template Permission fields and SHALL NOT be modified.

---

### 需求 3：ff_stage 表 Runtime Permission 字段扩展

**User Story：** 作为后端开发者，我希望在 `ff_stage` 表中新增 Template 继承标志和 Runtime Permission 字段，支持 Stage 在 Template 层继承 Workflow Template、在 Runtime 层继承 Workflow Runtime 的两种继承模式，并允许独立配置。

#### Acceptance Criteria

1. THE System SHALL add the following columns to `ff_stage` via a database migration using `IF NOT EXISTS`: `template_use_same_as_workflow` (bool, NOT NULL, default true), `runtime_use_same_as_workflow` (bool, NOT NULL, default true), `runtime_view_permission_mode` (smallint, NOT NULL, default 0), `runtime_view_teams` (jsonb, nullable), `runtime_operate_teams` (jsonb, nullable), `runtime_use_same_team_for_operate` (bool, NOT NULL, default true).
2. WHEN `template_use_same_as_workflow` is true, THE System SHALL apply the parent Workflow's Template Permission as the Stage Template Permission, dynamically reflecting Workflow Template changes.
3. WHEN `template_use_same_as_workflow` is false, THE System SHALL use the Stage's own `view_permission_mode`, `view_teams`, `operate_teams`, and `use_same_team_for_operate` fields as Stage Template Permission.
4. WHEN `runtime_use_same_as_workflow` is true, THE System SHALL compute Effective Stage Runtime Permission by inheriting the parent Workflow's Effective_Runtime_Permission.
5. WHEN `runtime_use_same_as_workflow` is false, THE System SHALL compute Effective Stage Runtime Permission from the Stage's own `runtime_*` fields, subject to the constraint that the resulting team set is a subset of the Workflow's Effective_Runtime_Permission.
6. THE existing `roll_back_teams` column in `ff_stage` SHALL be semantically repositioned as a Runtime layer field; the column name and field name SHALL remain unchanged.
7. WHEN `runtime_use_same_team_for_operate` is true on a Stage with `runtime_use_same_as_workflow` = false, THE System SHALL use the Stage's effective Runtime View Teams as the Stage Runtime Operate Teams.
8. THE System SHALL expose `EffectiveRuntimeViewPermissionMode`, `EffectiveRuntimeViewTeams`, `EffectiveRuntimeOperateTeams`, and `EffectiveRollBackTeams` as computed-only output fields in `StageOutputDto`.

---

### 需求 4：ff_onboarding 表 Permission Snapshot 字段扩展

**User Story：** 作为后端开发者，我希望在 `ff_onboarding` 表中新增权限快照字段，在 Case 创建时一次性写入 Workflow Runtime 权限作为只读上限，确保后续 Workflow 权限变更不影响已有 Case。

#### Acceptance Criteria

1. THE System SHALL add the following columns to `ff_onboarding` via a database migration using `IF NOT EXISTS`: `max_view_permission_mode` (smallint, nullable), `max_view_teams` (jsonb, nullable), `max_operate_teams` (jsonb, nullable).
2. WHEN a Case is created, THE System SHALL compute the Workflow's Effective_Runtime_Permission and write the result into `max_view_permission_mode`, `max_view_teams`, and `max_operate_teams` as part of the same transaction.
3. WHEN a Case is created, THE System SHALL initialize the Case's actual permission fields (`view_permission_mode` etc.) to the Public (inherit) state by default, indicating full inheritance from the snapshot.
4. THE `max_*` snapshot fields SHALL be read-only after Case creation; subsequent changes to the Workflow's Runtime Permission SHALL NOT automatically overwrite these fields.
5. WHEN a user triggers the "Reapply Workflow Permission" action via `POST /ow/onboardings/v1/{id}/reapply-workflow-permission`, THE System SHALL recompute the Workflow's current Effective_Runtime_Permission and overwrite the Case's `max_*` snapshot fields.
6. THE existing permission fields in `ff_onboarding` (`view_permission_mode`, `view_teams`, `view_users`, `operate_teams`, `operate_users`, `use_same_team_for_operate`, `view_permission_subject_type`, `operate_permission_subject_type`) SHALL be retained unchanged as the Case's actual configurable permission fields.

---

### 需求 5：OnboardingStageProgress Case Stage 权限字段扩展

**User Story：** 作为后端开发者，我希望在 `OnboardingStageProgress` C# 类中新增 Case Stage 实际权限字段和快照字段，支持 Case Stage 在继承 Workflow Stage Runtime 和独立配置两种状态之间切换，无需数据库 Migration。

#### Acceptance Criteria

1. THE System SHALL add C# properties to `OnboardingStageProgress` for Case Stage actual permission: `StagePermissionInheritFromWorkflowStage` (bool, default true), `StageViewPermissionMode`, `StageViewPermissionSubjectType`, `StageViewTeams`, `StageViewUsers`, `StageUseSameTeamForOperate`, `StageOperatePermissionSubjectType`, `StageOperateTeams`, `StageOperateUsers`, `StageRollBackInherit` (bool, default true), `StageRollBackUseSameAsOperate` (bool, default true), `StageRollBackPermissionSubjectType`, `StageRollBackTeams`, `StageRollBackUsers`.
2. THE System SHALL add C# properties to `OnboardingStageProgress` for Permission Snapshot (written at Case creation, read-only afterwards): `MaxStageViewPermissionMode` (ViewPermissionModeEnum), `MaxStageViewTeams` (List\<string\>), `MaxStageOperatePermissionMode` (ViewPermissionModeEnum), `MaxStageOperateTeams` (List\<string\>), `MaxStageRollBackTeams` (List\<string\>).
3. THE snapshot SHALL include both Mode and Teams for Stage View and Operate (not Teams only), because the downstream intersection logic requires the Mode to determine effective access.
4. WHEN `StagePermissionInheritFromWorkflowStage` is true (or the field is null for backward compatibility with existing records), THE System SHALL treat the effective Case Stage permission as equal to the `MaxStage*` snapshot values.
5. WHEN `StagePermissionInheritFromWorkflowStage` is false, THE System SHALL use `StageViewTeams` / `StageViewUsers` as the effective view permission, subject to the constraint that the team set is a subset of `MaxStageViewTeams`.
6. WHEN `StageOperateTeams` is empty and `StageUseSameTeamForOperate` is false, THE System SHALL treat this as "no operate permission for this stage" and SHALL NOT treat it as an error.
7. WHEN `StageRollBackTeams` is empty and `StageRollBackInherit` is false and `StageRollBackUseSameAsOperate` is false, THE System SHALL treat this as "no roll back permission for this stage" and SHALL NOT treat it as an error.
8. THE JSON deserialization of `OnboardingStageProgress` SHALL be backward-compatible; existing records missing the new fields SHALL be treated as null (interpreted as inherit state).

---

### 需求 6：Case 创建时快照写入逻辑

**User Story：** 作为后端开发者，我希望在 Case 创建时自动计算并写入 Workflow 和所有 Stage 的 Runtime 权限快照，确保 Case 的权限上限在创建时即固定。

#### Acceptance Criteria

1. WHEN `OnboardingCrudService.CreateAsync` is called, THE System SHALL load the parent Workflow entity and compute its Effective_Runtime_Permission via `PermissionCalculator.ComputeWorkflowEffectiveRuntime`, respecting the `runtime_use_same_as_template` flag.
2. WHEN `OnboardingCrudService.CreateAsync` is called, THE System SHALL write the computed Effective_Runtime_Permission into `Onboarding.max_view_permission_mode`, `max_view_teams`, and `max_operate_teams`.
3. WHEN `OnboardingCrudService.CreateAsync` is called, THE System SHALL iterate over each Stage in the Workflow and compute each Stage's Effective Stage Runtime Permission via `PermissionCalculator.ComputeStageEffectiveRuntime`, respecting the `runtime_use_same_as_workflow` flag.
4. WHEN `OnboardingCrudService.CreateAsync` is called, THE System SHALL write each Stage's computed Effective Runtime into the corresponding `OnboardingStageProgress` fields: `MaxStageViewPermissionMode`, `MaxStageViewTeams`, `MaxStageOperatePermissionMode`, `MaxStageOperateTeams`, `MaxStageRollBackTeams`.
5. THE snapshot write operation SHALL be atomic with the Case creation; IF the snapshot write fails, THEN the entire Case creation transaction SHALL be rolled back.
6. THE `PermissionCalculator` class SHALL be a static helper class that computes Effective Permissions without accessing repositories; Effective field population SHALL be done in the Service layer after mapping, and SHALL NOT be computed inside AutoMapper Profiles to avoid the anti-pattern of injecting repositories into Profiles.

---

### 需求 7：CasePermissionService 改造（读快照）

**User Story：** 作为后端开发者，我希望改造 `CasePermissionService`，将原有"Public 模式下实时查询 Workflow 实体"的逻辑替换为读取 `Onboarding.max_*` 快照，减少跨表查询并保证权限计算的确定性。

#### Acceptance Criteria

1. WHEN `UseWorkflowRuntimePermission` is true (inherit mode), THE CasePermissionService SHALL read `Onboarding.max_view_teams` and `Onboarding.max_operate_teams` from the snapshot fields instead of querying the Workflow entity.
2. WHEN `UseWorkflowRuntimePermission` is true, THE System SHALL ignore any view/operate team fields provided in the input DTO and use the snapshot values directly.
3. WHEN Case permission is in non-inherit mode (`UseWorkflowRuntimePermission` = false), THE CasePermissionService SHALL validate that the configured `view_teams` is a subset of `max_view_teams`; IF NOT, THEN THE System SHALL return a business error.
4. THE refactored CasePermissionService SHALL produce identical `CanView` and `CanOperate` results for all existing Case permission configurations as the pre-refactoring implementation.

---

### 需求 8：StagePermissionService 改造（三层权限取交集）

**User Story：** 作为后端开发者，我希望改造 `StagePermissionService`，在原有"Workflow View ∩ Stage View"两层判断基础上新增 Case Stage 层，实现三层权限取交集，确保 Case Stage 权限不超过 Case 权限和 Stage Runtime 权限。

#### Acceptance Criteria

1. WHEN computing effective permission for a Stage in a Case, THE StagePermissionService SHALL compute the intersection of three layers: Workflow Runtime Permission (from `Onboarding.max_*` snapshot), Stage Runtime Permission (from `Stage.EffectiveRuntimeViewTeams`), and Case Stage Permission.
2. WHEN `OnboardingStageProgress.StagePermissionInheritFromWorkflowStage` is true, THE StagePermissionService SHALL use `MaxStageViewTeams` and `MaxStageViewPermissionMode` as the Case Stage permission component for the intersection.
3. WHEN `OnboardingStageProgress.StagePermissionInheritFromWorkflowStage` is false, THE StagePermissionService SHALL use `StageViewTeams` / `StageViewUsers` (based on `StageViewPermissionSubjectType`) as the Case Stage permission component.
4. THE final effective `CanView` for a Stage SHALL be true only when the user passes all three layers simultaneously.
5. THE `POST /ow/permissions/v1/check` endpoint contract (returning `CanView` and `CanOperate`) SHALL remain unchanged; only the internal computation logic SHALL be updated.
6. THE Layer 1 data source SHALL be `Onboarding.MaxViewTeams` snapshot (not a live query to the Workflow entity), consistent with the snapshot model.

---

### 需求 9：Roll Back 权限校验改造

**User Story：** 作为后端开发者，我希望改造 Roll Back 操作的权限校验逻辑，读取 Case Stage 的 Roll Back 快照或独立配置，并确保 Roll Back Teams 只能从具有 Stage Operate Permission 的 Team 中选择。

#### Acceptance Criteria

1. WHEN a roll back operation is attempted, THE System SHALL read `OnboardingStageProgress.StageRollBackInherit` to determine the effective Roll Back permission source.
2. WHEN `StageRollBackInherit` is true, THE System SHALL use `MaxStageRollBackTeams` (snapshot from Stage Runtime `roll_back_teams` at Case creation time) as the effective Roll Back permission set.
3. WHEN `StageRollBackInherit` is false AND `StageRollBackUseSameAsOperate` is true, THE System SHALL use the computed effective Case Stage Operate Teams as the effective Roll Back permission set.
4. WHEN `StageRollBackInherit` is false AND `StageRollBackUseSameAsOperate` is false, THE System SHALL use `StageRollBackTeams` / `StageRollBackUsers` (based on `StageRollBackPermissionSubjectType`) as the effective Roll Back permission set.
5. IF the executing user is not in the effective Roll Back permission set, THEN THE System SHALL reject the roll back operation with a 403 authorization error.
6. WHEN configuring `Stage.roll_back_teams` in the Workflow Builder, THE System SHALL only allow selection of Teams that are present in the Stage's Effective Runtime Operate Teams; Teams outside this scope SHALL NOT be selectable.
7. WHEN configuring `StageRollBackTeams` in a Case Stage independently, THE System SHALL only allow selection of Teams present in the effective Case Stage Operate Teams.

---

### 需求 10：Workflow DTO 变更（Runtime Permission 字段）

**User Story：** 作为后端开发者，我希望在 `WorkflowInputDto` 和 `WorkflowOutputDto` 中新增 Runtime Permission 相关字段，支持前端展示和配置 Workflow Runtime 权限。

#### Acceptance Criteria

1. THE `WorkflowInputDto` SHALL include: `RuntimeUseSameAsTemplate` (bool, default true), `RuntimeViewPermissionMode` (ViewPermissionModeEnum), `RuntimeViewTeams` (List\<string\>), `RuntimeOperateTeams` (List\<string\>).
2. THE `WorkflowOutputDto` SHALL include all fields from `WorkflowInputDto` plus the computed-only output fields: `EffectiveRuntimeViewPermissionMode`, `EffectiveRuntimeViewTeams`, `EffectiveRuntimeOperateTeams`.
3. WHEN `WorkflowInputDto.RuntimeUseSameAsTemplate` is true, THE System SHALL ignore `RuntimeViewPermissionMode`, `RuntimeViewTeams`, and `RuntimeOperateTeams` in the input and compute Effective Runtime from Template fields.
4. THE `WorkflowInputDto` SHALL NOT include a `RuntimeUseSameTeamForOperate` field; the Workflow Runtime layer uses independent Operate Teams without a "use same" flag.
5. THE existing `WorkflowInputDto` and `WorkflowOutputDto` fields for Template Permission SHALL remain unchanged and backward-compatible.

---

### 需求 11：Stage DTO 变更（Template 继承 + Runtime Permission 字段）

**User Story：** 作为后端开发者，我希望在 `StageInputDto` 和 `StageOutputDto` 中新增 Template 继承标志和 Runtime Permission 字段，支持前端展示和配置 Stage 权限的两种状态。

#### Acceptance Criteria

1. THE `StageInputDto` SHALL include: `TemplateUseSameAsWorkflow` (bool, default true), `RuntimeUseSameAsWorkflow` (bool, default true), `RuntimeViewPermissionMode` (ViewPermissionModeEnum), `RuntimeViewTeams` (List\<string\>), `RuntimeOperateTeams` (List\<string\>), `RuntimeUseSameTeamForOperate` (bool, default true).
2. THE `StageOutputDto` SHALL include all `StageInputDto` fields plus computed-only output fields: `EffectiveRuntimeViewPermissionMode`, `EffectiveRuntimeViewTeams`, `EffectiveRuntimeOperateTeams`, `EffectiveRollBackTeams`.
3. THE existing `RollBackTeams` field in `StageInputDto` / `StageOutputDto` SHALL be retained; its backend semantic SHALL be repositioned to the Runtime layer without changing the field name or API contract.
4. WHEN `StageInputDto.RuntimeUseSameAsWorkflow` is true and Runtime Teams are provided in the input, THE System SHALL ignore those provided Runtime Teams and compute Effective Runtime from the parent Workflow's Effective Runtime.

---

### 需求 12：Case DTO 变更（继承标志 + 快照只读字段）

**User Story：** 作为后端开发者，我希望在 `OnboardingInputDto` 和 `OnboardingOutputDto` 中新增权限继承标志和只读快照字段，让前端能够展示和控制"继承 Workflow Runtime"开关，并在 choosable tree 中限制可选范围。

#### Acceptance Criteria

1. THE `OnboardingInputDto` SHALL include `UseWorkflowRuntimePermission` (bool, default true) to indicate whether the Case should inherit Workflow Runtime Permission from the snapshot.
2. THE `OnboardingOutputDto` SHALL include `UseWorkflowRuntimePermission` (bool), `MaxViewPermissionMode` (ViewPermissionModeEnum, nullable), `MaxViewTeams` (List\<string\>), `MaxOperateTeams` (List\<string\>).
3. WHEN `UseWorkflowRuntimePermission` is true, THE System SHALL treat the Case permission as equivalent to the `max_*` snapshot values and SHALL NOT apply independent view/operate team filters from the configurable fields.
4. THE `MaxViewTeams` and `MaxOperateTeams` in `OnboardingOutputDto` SHALL be read-only; the frontend SHALL use these values to restrict the choosable range in the permission Team selectors.
5. THE existing Case permission fields in `OnboardingInputDto` and `OnboardingOutputDto` SHALL remain unchanged and SHALL be active when `UseWorkflowRuntimePermission` is false.

---

### 需求 13：Case Stage Permission 独立端点

**User Story：** 作为后端开发者，我希望新增 `PUT /ow/onboardings/v1/{id}/stage-permissions/{stageId}` 端点，支持对单个 Case Stage 的权限进行独立配置或重置为继承状态，并新增快照重置端点。

#### Acceptance Criteria

1. THE System SHALL expose `PUT /ow/onboardings/v1/{id}/stage-permissions/{stageId}` accepting `CaseStagePermissionInputDto` containing: `InheritFromWorkflowStage` (bool), `ViewPermissionMode`, `ViewPermissionSubjectType`, `ViewTeams`, `ViewUsers`, `UseSameTeamForOperate`, `OperatePermissionSubjectType`, `OperateTeams`, `OperateUsers`, `RollBackInherit`, `RollBackUseSameAsOperate`, `RollBackPermissionSubjectType`, `RollBackTeams`, `RollBackUsers`.
2. WHEN `InheritFromWorkflowStage` is true, THE System SHALL reset the Case Stage to inherit state by setting `StagePermissionInheritFromWorkflowStage = true` and clearing all independent permission configuration fields.
3. WHEN `InheritFromWorkflowStage` is false AND `ViewPermissionMode` is VisibleTo AND no Teams or Users are provided, THEN THE System SHALL return a validation error and reject the request.
4. WHEN `InheritFromWorkflowStage` is false, THE System SHALL validate that the provided `ViewTeams` are a subset of `MaxStageViewTeams`; IF NOT, THEN THE System SHALL return a business error.
5. THE `OnboardingStageProgressDto` SHALL be extended to include all Case Stage permission fields for display: actual configuration fields (14 properties), plus computed-only fields `EffectiveViewTeams`, `EffectiveOperateTeams`, `EffectiveRollBackTeams`.
6. THE System SHALL expose `POST /ow/onboardings/v1/{id}/reapply-workflow-permission` (no request body) that recomputes the Workflow's current Effective_Runtime_Permission and overwrites the Case's `max_*` snapshot fields, returning `SuccessResponse<bool>`.

---

### 需求 14：PermissionCalculator 辅助类

**User Story：** 作为后端开发者，我希望新建 `PermissionCalculator` 静态辅助类，集中处理所有 Effective Permission 计算逻辑，避免在多个 Service 中重复实现并防止在 AutoMapper Profile 中注入 Repository 的反模式。

#### Acceptance Criteria

1. THE System SHALL create a `PermissionCalculator` static class with a `ComputeWorkflowEffectiveRuntime(Workflow workflow)` method that returns a `WorkflowEffectiveRuntime` record containing `ViewMode`, `ViewTeams`, and `OperateTeams`.
2. THE System SHALL implement a `ComputeStageEffectiveRuntime(Stage stage, WorkflowEffectiveRuntime workflowRuntime)` method that returns a `StageEffectiveRuntime` record containing `ViewMode`, `ViewTeams`, `OperateTeams`, and `RollBackTeams`.
3. THE System SHALL implement an `IsSubsetOf(List<string> teams, List<string> maxTeams)` method that returns true when every team in `teams` is present in `maxTeams`; an empty or null `teams` list SHALL always return true.
4. THE `PermissionCalculator` methods SHALL be pure functions with no side effects and no repository or database access; all inputs SHALL be passed as parameters.
5. THE Effective field values in Output DTOs SHALL be populated in the Service layer after entity-to-DTO mapping by calling `PermissionCalculator` methods directly, and SHALL NOT be computed inside AutoMapper Profiles.

---

### 需求 15：前端 Workflow Permissions 弹窗改造

**User Story：** 作为 Workflow 管理员，我希望通过重新设计的 Workflow Permissions 弹窗分别配置 Template Permissions 和 Runtime Permissions，使两者的设置完全独立，并能实时看到 Effective Teams 预览。

#### Acceptance Criteria

1. THE Workflow_Permissions_Dialog SHALL be accessible from two entry points: the Edit Workflow modal's Permissions tab, and a standalone Permissions button on the Workflow detail page (`/onboard/onboardWorkflow?id=xxx`).
2. THE Workflow_Permissions_Dialog SHALL contain two clearly separated sections: "Template Permissions" and "Runtime Permissions" with independent configurations.
3. WHEN the "Use same permissions as Template Permissions" checkbox in Runtime Permissions is checked, THE System SHALL display Runtime View and Operate as read-only Effective Teams inherited from Template.
4. WHEN the "Use same permissions as Template Permissions" checkbox is unchecked, THE System SHALL display an independent Runtime View selector (Public / Visible to / Invisible to dropdown) and an independent Runtime Operate Teams selector.
5. THE Runtime Operate Teams selector SHALL NOT include a "Use same team that have view permission" checkbox; Runtime Operate is always an independent selector at the Workflow level.
6. WHEN ViewPermissionMode is VisibleTo, THE System SHALL display a Team multi-selector and show "EFFECTIVE TEAMS: [...]" below.
7. WHEN ViewPermissionMode is InvisibleTo, THE System SHALL display a "Hidden from Team" multi-selector and show Effective Teams (the remaining teams after exclusion) below.
8. THE Template Permissions Operate section SHALL include a "Use same team that have view permission" checkbox; WHEN checked, THE System SHALL use View Teams for Operate.

---

### 需求 16：前端 Stage Permissions Tab 重做

**User Story：** 作为 Workflow Builder 用户，我希望在 Stage Edit 弹窗的 Permissions Tab 中分别配置 Template Permissions 和 Runtime Permissions（含 Roll Back Teams），每个区块支持"继承 Workflow"或独立配置两种状态。

#### Acceptance Criteria

1. THE Stage_Edit_Dialog SHALL contain a Permissions tab alongside Basic Info and Components tabs.
2. THE Stage_Permissions_Tab SHALL contain three sections: "Template Permissions", "Runtime Permissions", and "Roll Back Teams".
3. WHEN "Use same permissions as Workflow Template Permissions" is checked (default), THE System SHALL display Template View and Operate as read-only previews showing "Matches the workflow's Template view/operate permission. EFFECTIVE TEAMS: [...]".
4. WHEN "Use same permissions as Workflow Template Permissions" is unchecked, THE System SHALL show an independent View dropdown (Public / Visible to / Invisible to) and Operate section (with "Use same team that have view permission" checkbox).
5. WHEN "Use same permission as Workflow Runtime Permissions" is checked (default), THE System SHALL display Runtime View and Operate as read-only previews showing "Matches the workflow's Runtime view/operate permission. EFFECTIVE TEAMS: [...]".
6. WHEN "Use same permission as Workflow Runtime Permissions" is unchecked, THE System SHALL show an independent Runtime View dropdown and an independent Runtime Operate Teams selector (without "Use same" checkbox).
7. THE Roll_Back_Teams selector choosable range SHALL be limited to Teams present in the Stage's Effective Runtime Operate Teams.
8. THE Roll_Back_Teams section SHALL display "Can only include teams that also have Runtime Operate permission. EFFECTIVE TEAMS: [...]" below the selector.

---

### 需求 17：前端 Case Access Control 区块改造

**User Story：** 作为 Case 创建/编辑者，我希望 Case 的 Access Control 区块展示权限只能在 Workflow Runtime 快照范围内收紧的提示，并通过"继承 Workflow Runtime"开关快速重置权限，所有选择器均限制在快照范围内。

#### Acceptance Criteria

1. THE Case_Access_Control_Section SHALL display a fixed description text: "Case permission can only narrow the workflow's permission, copied in at creation — it can't grant more access than the workflow (or Case Operate more than Case View), and later workflow changes won't affect existing cases."
2. THE Case_Access_Control_Section SHALL include a top-level checkbox "Use same permission as Workflow Runtime Permissions"; WHEN checked, THE System SHALL display Effective Teams read-only (sourced from the `max_*` snapshot) for both View and Operate.
3. WHEN the "Use same permission as Workflow Runtime Permissions" checkbox is unchecked, THE System SHALL display the View Permission selector (Public / Visible to / Invisible to / Private) and Operate Permission selector.
4. WHEN ViewPermissionMode is VisibleTo or InvisibleTo, THE System SHALL limit the choosable teams to `MaxViewTeams` from the Permission Snapshot.
5. THE Operate Permission section SHALL include a "Use same teams and users that have view permission" checkbox; WHEN unchecked, the choosable teams/users SHALL be limited to the teams/users selected for Case View.
6. WHEN ViewPermissionMode is Private, THE System SHALL display only an Individual Users selector for View permission.

---

### 需求 18：前端 Case Stage Permission 弹窗（全新组件）

**User Story：** 作为 Case 处理者，我希望通过 Case Progress 面板中每个 Stage 卡片的护盾图标（shield icon）打开 Case Stage Permission 弹窗，独立配置该 Stage 的 View、Operate 和 Roll Back 权限，并能在继承模式和独立配置模式之间切换。

#### Acceptance Criteria

1. THE System SHALL create a new `CaseStagePermissionDialog.vue` component triggered by clicking the shield icon on each Stage card in the Case Progress panel.
2. THE CaseStagePermissionDialog SHALL display the title "Case Stage Permission" with the Stage name as subtitle.
3. WHEN "Use same permission as workflow stage runtime" is checked (default inherit state), THE System SHALL display View Permission, Operate Permission, and Roll Back Permission as read-only EFFECTIVE TEAMS sections derived from the `MaxStage*` snapshot values.
4. WHEN the inherit checkbox is unchecked, THE System SHALL show an independent View Permission selector (Public / Visible to / Invisible to only — Private SHALL NOT be available at Case Stage level), Operate Permission (with "Use same teams and users that have view permission" checkbox), and Roll Back Permission (with "Use same teams and users that have operate permission" checkbox).
5. WHEN ViewPermissionMode is VisibleTo AND no teams or users are selected, THE System SHALL display a validation error and prevent form submission.
6. THE Operate Permission choosable range SHALL be limited to the intersection of the Case Stage View Effective Teams and `MaxStageOperateTeams`.
7. THE Roll_Back_Permission choosable range SHALL be limited to the effective Case Stage Operate Teams.
8. WHEN Roll Back is configured independently (not inheriting and not using same as Operate), THE System SHALL support both User Teams and Individual Users selection modes.
9. THE CaseStagePermissionDialog SHALL call `PUT /ow/onboardings/v1/{id}/stage-permissions/{stageId}` on save, and SHALL refresh the Stage Progress display on success.

---

### 需求 19：历史数据迁移 — 历史 Case 快照回填

**User Story：** 作为系统管理员，我希望在数据库 Migration 执行时，自动对现有历史 Case 的权限快照字段进行回填，以 Workflow 当前 Runtime Permission 为准，确保历史数据与新权限模型兼容。

#### Acceptance Criteria

1. WHEN the database migration for `ff_onboarding` new snapshot columns is executed, THE System SHALL batch-fill `max_view_permission_mode`, `max_view_teams`, and `max_operate_teams` for all existing active Cases using the current Effective_Runtime_Permission of their parent Workflow.
2. WHEN the database migration is executed, THE System SHALL batch-fill `MaxStageViewPermissionMode`, `MaxStageViewTeams`, `MaxStageOperatePermissionMode`, `MaxStageOperateTeams`, and `MaxStageRollBackTeams` in `OnboardingStageProgress` JSON for all existing active Case Stages using the current Effective Stage Runtime Permission of the corresponding Workflow Stage.
3. THE backfill operation SHALL be idempotent; re-running the migration SHALL NOT overwrite snapshot fields that have already been filled (idempotency condition: `max_view_permission_mode IS NULL` for the SQL portion, `MaxStageViewTeams == null` for the C# JSON portion).
4. IF a Case's parent Workflow is not found during backfill, THE System SHALL log a warning and skip that Case without failing the entire migration.
5. THE migration SHALL execute in the correct order: `ff_workflow` new columns → `ff_stage` new columns → `ff_onboarding` new columns + backfill, because the backfill SQL depends on `runtime_use_same_as_template` being present on `ff_workflow`.
6. THE C# backfill logic (for `OnboardingStageProgress` JSONB) SHALL load Cases in batches, deserialize `stages_progress`, compute `StageEffectiveRuntime` for each Stage, and serialize back, performing the write within the migration `Up()` method.

---

### 需求 20：迁移执行顺序与注册

**User Story：** 作为后端开发者，我希望三个 Migration 文件按正确顺序在 `MigrationManager` 中注册，确保回填 SQL 在其依赖字段已存在的情况下执行。

#### Acceptance Criteria

1. THE System SHALL create three migration files in `SqlSugarDB/Migrations/` with the naming pattern `Migration_{YYYYMMDD}{序号}_{描述}.cs`: one for `ff_workflow`, one for `ff_stage`, and one for `ff_onboarding` (including backfill).
2. THE three migration files SHALL be registered in `MigrationManager.cs` in the order: `ff_workflow` → `ff_stage` → `ff_onboarding`, because the `ff_onboarding` backfill SQL references the `runtime_use_same_as_template` column added in the `ff_workflow` migration.
3. ALL DDL statements in the migration files SHALL use `IF NOT EXISTS` / `IF EXISTS` guards to ensure idempotency on re-execution.
4. THE `Down()` method in each migration file SHALL remove only the newly added columns and SHALL NOT roll back backfilled data.

