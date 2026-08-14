# 实现计划：Send Notification Field Recipients

## 概述

将 FlowFlex Condition Action "Send Notification" 的收件人配置扩展为三种方式，在现有 Select User / Select Team 基础上新增 Select Field。实现分为前端 UI + 逻辑、后端执行两大块，相互独立，可并行开发。

---

## 任务列表

- [x] 1. 补充前端类型定义
  - [x] 1.1 在类型文件中新增 `FieldRefItem` 接口
    - 定位现有 `condition.d.ts`（或 `#/condition` 对应的类型文件）
    - 新增 `FieldRefItem { stageId: string; fieldId: string; fieldName: string; dataType: number }`
    - 确保 `ActionFormItem.parameters` 类型允许 `fieldRefs: FieldRefItem[]`
    - _需求：2.1_

- [x] 2. 前端 — 构建 recipientFieldOptions 计算属性
  - [x] 2.1 在 `ConditionActionForm.vue` 中添加 `recipientFieldOptions` computed
    - 遍历 `props.stages.slice(0, props.currentStageIndex)`（之前的 stage，不含当前）
    - 筛选 `dataType === propertyTypeEnum.Email (4)` 或 `dataType === propertyTypeEnum.Pepole (19)`
    - 输出 `RecipientFieldOptionGroup[]`，结构与 `groupedFieldOptions` 保持一致
    - 依赖现有 `staticFieldsMap`，无需额外 API 调用
    - _需求：1.2、1.3_
  - [ ]\* 2.2 为 recipientFieldOptions 编写属性测试
    - **属性 1：字段过滤正确性** — 结果中所有字段 dataType 必须为 4 或 19，且 stage 索引 < currentStageIndex
    - **属性 2：分组聚合正确性** — 同一 stageId 的字段归属同一分组
    - 使用 fast-check 生成随机 stage 列表和 currentStageIndex
    - _需求：1.2、1.3_

- [x] 3. 前端 — 新增 Select Field UI 与数据绑定
  - [x] 3.1 在 `ConditionActionForm.vue` SendNotification 区域新增 Select Field 选择器
    - 在 Select Team 下方插入 `<div class="text-gray-500 mb-1">Select Field</div>`
    - 使用 `el-select` 多选模式，`v-model` 绑定到 key 数组（`stageId_fieldId`）
    - 内部用 `el-option-group` 按 Stage 分组展示字段
    - 每个 `el-option` 在标签旁显示 dataType badge（Email / People）
    - _需求：1.1、1.4、1.5_
  - [x] 3.2 实现 `getFieldRefKeys` 和 `handleFieldRefsChange` 方法
    - `getFieldRefKeys(action)` — 从 `fieldRefs` 转换为 key 数组，用于 el-select v-model
    - `handleFieldRefsChange(action, keys)` — 反向将 key 数组映射回完整的 `FieldRefItem[]` 写入 `parameters.fieldRefs`
    - _需求：2.1、2.2、2.3_
  - [ ]\* 3.3 为 fieldRefs 写入逻辑编写属性测试
    - **属性 3：fieldRefs 写入结构正确性** — 对任意 key 数组，写入的 fieldRefs 每条记录包含四个必填字段，且 key 能反向还原
    - 使用 fast-check 生成随机 key 列表
    - _需求：2.1_

- [x] 4. 前端 — 更新校验逻辑
  - [x] 4.1 修改 `getActionValidationRules` 中 `parameters.recipients` 的 validator
    - 在原有 `hasUsers || hasTeams` 基础上增加 `hasFields`（fieldRefs?.length > 0）
    - 错误提示改为 "Please select at least one recipient (user, team, or field)"
    - _需求：3.1、3.2、3.3_
  - [ ]\* 4.2 为校验逻辑编写属性测试
    - **属性 4：收件人校验完备性** — 三者均为空 → 校验失败；至少一个非空 → 校验通过
    - 使用 fast-check 生成 users/teams/fieldRefs 的各种组合（含空数组、非空数组）
    - _需求：3.1、3.2_

- [x] 5. Checkpoint — 前端基础功能验证
  - 确保所有前端单测通过，Select Field 选择器在界面可正常渲染，ask the user if questions arise.

- [x] 6. 后端 — 新增 FieldRefItem 内部 DTO 和辅助方法
  - [ ] 6.1 在 `ActionExecutor.cs` 中添加私有类 `FieldRefItem`
    - 包含属性：`StageId`、`FieldId`、`FieldName`、`DataType`（均带 `[JsonProperty]` 注解）
    - _需求：5.1_
  - [ ] 6.2 新增私有静态方法 `ParsePeopleFieldValue(string rawValue)`
    - 先尝试 `JsonConvert.DeserializeObject<List<string>>(rawValue)`
    - 失败则将 `rawValue.Trim('"')` 作为单 ID 返回
    - 过滤空字符串，返回 `List<string>`
    - _需求：5.3_
  - [ ]\* 6.3 为 ParsePeopleFieldValue 编写属性测试
    - **属性 5：People 字段值解析健壮性** — 对任意非空 rawValue，验证解析结果无空字符串；JSON 数组输入与数组内容一致；单字符串输入返回单元素列表
    - 使用 FsCheck 生成随机 ID 字符串、JSON 数组字符串、带引号字符串
    - _需求：5.3_

- [x] 7. 后端 — 扩展 ExecuteSendNotificationAsync
  - [ ] 7.1 在现有 `users`/`teams` 解析之后添加 `fieldRefs` 解析逻辑
    - 从 `action.Parameters["fieldRefs"]` 提取，反序列化为 `List<FieldRefItem>`
    - _需求：5.1、6.1_
  - [ ] 7.2 更新空收件人校验：三者均为空才报错
    - 将 `if (!userIds.Any() && !teamIds.Any())` 改为 `if (!userIds.Any() && !teamIds.Any() && !fieldRefs.Any())`
    - 更新错误信息为 "Either users, teams, or fieldRefs must specify at least one valid recipient"
    - _需求：5.5、6.1、6.2_
  - [ ] 7.3 实现 fieldRefs 处理主逻辑
    - 调用 `_staticFieldValueService.GetByOnboardingIdAsync(context.OnboardingId)` 获取所有字段值
    - 按 fieldId 建立字典，遍历 fieldRefs
    - dataType==4（Email）：直接 Trim('"') 取邮箱，调用 `SendEmailWithRetryAsync`
    - dataType==19（People）：调用 `ParsePeopleFieldValue`，再调用 `GetUsersByIdsAsync`，遍历 UserDto.Email 发送
    - 字段值为空时跳过，发送失败时计入 `failedRecipients`
    - _需求：5.2、5.3、5.4、5.6_
  - [ ] 7.4 在 result.ResultData 中追加 `fieldRefsCount`
    - `result.ResultData["fieldRefsCount"] = fieldRefs.Count`
    - _需求：6.3_
  - [ ]\* 7.5 为后端 fieldRefs 处理编写单测
    - **属性 6：发送统计不变量** — sentEmails.Count == successCount（包含 fieldRefs 贡献）
    - 示例测试：Email 字段直接发邮件；People 单/多 ID 场景；仅 fieldRefs 不为空时不报错；字段值为空时跳过
    - 使用 xUnit + Moq，Mock `_staticFieldValueService`、`_userService`、`_emailService`
    - _需求：5.2、5.3、5.5、5.6_

- [ ] 8. Checkpoint — 后端功能验证
  - 确保所有后端单测通过，ask the user if questions arise.

- [x] 9. 前端 — 编辑回显支持
  - [ ] 9.1 确认 staticFieldsMap 加载完成后 Select Field 能正确回显
    - 检查 `isLoading` 骨架屏在 `loadingFields` 为 true 时覆盖 Select Field 区域
    - 验证 `getFieldRefKeys` 在 staticFieldsMap 已有数据后能正确返回 key 数组
    - _需求：4.1、4.2_
  - [ ] 9.2 实现 fieldRef 回退显示逻辑
    - 当 recipientFieldOptions 中找不到对应 key 时，在 el-option 中使用 `fieldRef.fieldName` 作为 label
    - 或在 el-select 的 `value-key` 中直接展示保存的 fieldName
    - _需求：4.3_

- [ ] 10. Final Checkpoint — 端到端验证
  - 确认前后端均通过测试，回显正常，ask the user if questions arise.

---

## 说明

- 标有 `*` 的子任务为可选测试任务，可在 MVP 阶段跳过以加快交付
- 每个任务均引用了对应的需求条款，便于追溯
- 任务 2、3、4 均依赖现有 `staticFieldsMap` 缓存，无需修改数据加载逻辑
- 任务 6、7 相互独立，可与前端任务并行开发

---

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["2.1", "6.1", "6.2"] },
    { "id": 2, "tasks": ["2.2", "3.1", "3.2", "6.3", "7.1"] },
    { "id": 3, "tasks": ["3.3", "4.1", "7.2", "7.3"] },
    { "id": 4, "tasks": ["4.2", "7.4", "9.1"] },
    { "id": 5, "tasks": ["7.5", "9.2"] }
  ]
}
```
