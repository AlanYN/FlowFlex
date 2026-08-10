# 需求文档

## 简介

本功能为 FlowFlex 的 Condition Action "Send Notification" 增加第三种收件人选择方式——**Select Field**。当前 Recipients 区域只支持 Select User（内部用户）和 Select Team 两种选择器。本次改动允许用户在配置通知时额外指定来自**当前 Stage 之前的 Stage** 中类型为 Email（dataType=4）或 People（dataType=19）的 Dynamic Field，系统在执行通知时将读取这些字段的实际值作为邮箱地址发送邮件。

本功能涉及前端 Vue 组件改动（新增 Select Field 选择器、更新校验逻辑、支持回显）和后端 C# 服务改动（解析 `fieldRefs` 参数、从字段值中解析邮箱地址、执行发送）。

---

## 词汇表

- **Condition Action**：Workflow Stage Condition 触发时执行的动作，类型之一为 `SendNotification`。
- **ConditionActionForm**：前端组件 `ConditionActionForm.vue`，负责渲染和编辑 Action 配置。
- **Dynamic Field**：Workflow Stage 中定义的静态字段（Static Field），有唯一 ID 和 dataType。
- **Email Field**：`dataType = 4` 的字段，字段值为邮箱字符串。
- **People Field**：`dataType = 19` 的字段，字段值为用户 ID 字符串或字符串数组，需通过用户服务查询对应邮箱。
- **fieldRefs**：存储在 Action `parameters` 字典中的新 key，格式为 `Array<{ stageId, fieldId, fieldName, dataType }>`。
- **staticFieldsMap**：前端内存缓存，key = fieldId，value = 字段元数据（来自 `batchIdsDynamicFields()` API）。
- **ExecuteSendNotificationAsync**：后端 `ConditionActionExecutor` 中执行 SendNotification 的方法。
- **StaticFieldValue**：后端从数据库读取的字段实际值记录，包含 `FieldType`、`FieldValueJson`、`PropertyId`、`OnboardingId` 等。

---

## 需求

### 需求 1：前端 — 构建可用字段选项

**用户故事：** 作为 Workflow 配置者，我想在配置 Send Notification 收件人时能够选择表单字段，以便让通知自动发送给字段中填写的真实联系人。

#### 验收标准

1. WHEN 用户打开 Send Notification 的编辑界面 THEN THE ConditionActionForm SHALL 展示一个标题为 "Select Field" 的下拉多选框，位于 Select Team 选择器下方。
2. THE ConditionActionForm SHALL 仅将位于当前 Stage **之前**（不含当前 Stage）的 Stage 中的 `dataType === 4`（Email）或 `dataType === 19`（People）字段纳入 Select Field 的候选选项中。
3. THE ConditionActionForm SHALL 对候选字段按所属 Stage 分组展示（el-option-group），分组标题为 Stage 名称。
4. WHEN 某字段的 dataType 为 Email（4）THEN THE ConditionActionForm SHALL 在该选项旁显示类型标识 "Email"。
5. WHEN 某字段的 dataType 为 People（19）THEN THE ConditionActionForm SHALL 在该选项旁显示类型标识 "People"。
6. IF 当前 Stage 之前不存在 Email 或 People 类型字段 THEN THE ConditionActionForm SHALL 展示空的 Select Field 下拉框（无可选项）。

---

### 需求 2：前端 — 数据绑定与存储

**用户故事：** 作为 Workflow 配置者，我想选中的字段信息能被正确保存到 Action 参数中，以便后端执行时能准确获取收件人。

#### 验收标准

1. WHEN 用户在 Select Field 下拉框中选中一个或多个字段 THEN THE ConditionActionForm SHALL 将所选字段写入 `action.parameters.fieldRefs` 中，格式为 `Array<{ stageId: string, fieldId: string, fieldName: string, dataType: number }>`。
2. THE ConditionActionForm SHALL 保持 `fieldRefs` 与下拉框的双向绑定，即修改选中项时 `fieldRefs` 同步更新。
3. WHEN 用户清空 Select Field 的选中项 THEN THE `action.parameters.fieldRefs` SHALL 被设置为空数组。
4. THE ConditionActionForm SHALL 不修改现有 `users` 和 `teams` 参数的绑定逻辑。

---

### 需求 3：前端 — 校验规则更新

**用户故事：** 作为 Workflow 配置者，我想保存时系统能正确判断收件人是否为空，以便在三种选择器均为空时得到明确提示。

#### 验收标准

1. WHEN 用户尝试保存 SendNotification Action 且 `users`、`teams`、`fieldRefs` 三者均为空 THEN THE ConditionActionForm SHALL 触发校验错误，提示 "Please select at least one recipient (user, team, or field)"。
2. WHEN `users` 或 `teams` 或 `fieldRefs` 中至少有一个不为空 THEN THE ConditionActionForm SHALL 通过校验，不显示错误。
3. THE ConditionActionForm SHALL 在原 `parameters.recipients` 校验规则的 validator 函数中增加对 `fieldRefs` 的检查，不新增独立校验规则。

---

### 需求 4：前端 — 编辑回显

**用户故事：** 作为 Workflow 配置者，我想重新打开已保存的 Condition 时，Select Field 能正确回显之前选中的字段，以便我能看到并修改已有配置。

#### 验收标准

1. WHEN 用户打开已保存的 SendNotification Action 且 `action.parameters.fieldRefs` 不为空 THEN THE ConditionActionForm SHALL 在 Select Field 下拉框中正确显示之前选中的字段名称。
2. THE ConditionActionForm SHALL 在 `staticFieldsMap` 加载完成后才渲染 Select Field 的已选状态，确保字段名称能正确展示。
3. IF `fieldRefs` 中引用的字段在 `staticFieldsMap` 中不存在 THEN THE ConditionActionForm SHALL 仍正常展示该 fieldRef 中保存的 `fieldName` 作为回退显示。

---

### 需求 5：后端 — 解析 fieldRefs 并发送通知

**用户故事：** 作为系统，我想在执行 SendNotification 时能解析 `fieldRefs`，并根据字段类型获取对应的邮箱地址发送邮件，以确保通知准确传达到动态指定的收件人。

#### 验收标准

1. WHEN `ExecuteSendNotificationAsync` 执行时 THEN THE ExecuteSendNotificationAsync SHALL 从 `action.Parameters["fieldRefs"]` 中解析 fieldRef 列表（`stageId`、`fieldId`、`dataType` 为必填项）。
2. WHEN 某 fieldRef 的 `dataType` 为 4（Email）THEN THE ExecuteSendNotificationAsync SHALL 直接将 `field_value_json` 的字符串值作为邮箱地址发送通知。
3. WHEN 某 fieldRef 的 `dataType` 为 19（People）THEN THE ExecuteSendNotificationAsync SHALL 将 `field_value_json` 解析为用户 ID 列表（单个字符串或字符串数组），然后调用 `GetUsersByIdsAsync` 获取用户邮箱后发送通知。
4. WHEN `fieldRefs` 中的字段值为 null、空字符串或空数组 THEN THE ExecuteSendNotificationAsync SHALL 跳过该 fieldRef，不计入发送失败。
5. IF `users`、`teams`、`fieldRefs` 三者解析后均无有效收件人 THEN THE ExecuteSendNotificationAsync SHALL 返回失败，错误信息为 "Either users, teams, or fieldRefs must specify at least one valid recipient"。
6. WHEN fieldRefs 处理成功发送邮件时 THEN THE ExecuteSendNotificationAsync SHALL 将发送的邮箱地址计入 `sentEmails`，并增加 `successCount`。

---

### 需求 6：后端 — 校验规则更新

**用户故事：** 作为系统，我想将 SendNotification 的收件人空值检查更新为三方兼容，以避免在只配置了 fieldRefs 时误报错误。

#### 验收标准

1. THE ExecuteSendNotificationAsync SHALL 在解析 `users`、`teams` 之后同等解析 `fieldRefs`，再统一判断三者是否均为空。
2. WHEN 仅 `fieldRefs` 不为空（`users` 和 `teams` 均为空）THEN THE ExecuteSendNotificationAsync SHALL 正常执行字段值解析和发送，不返回错误。
3. THE ExecuteSendNotificationAsync SHALL 在 result.ResultData 中追加 `"fieldRefsCount"` 字段记录处理的 fieldRef 数量，便于日志追踪。
