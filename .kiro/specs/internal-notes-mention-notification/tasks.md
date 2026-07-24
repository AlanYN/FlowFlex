# Implementation Plan: Internal Notes @Mention 邮件通知

## Overview

基于 MediatR 中介者模式，为 Internal Notes 系统添加 @mention 邮件通知能力。后端采用领域事件驱动，InternalNoteService 保存 note 后发布事件，由独立 Handler 处理 mention 解析、diff 计算和邮件发送。前端扩展 mention 组件支持外部邮箱输入和视觉区分。

## Tasks

- [x] 1. 创建 MentionParser 静态工具类
  - [x] 1.1 实现 MentionParser 核心解析逻辑
    - 创建文件 `Application/Services/OW/MentionParser.cs`
    - 实现 `ParseMentions(string content)` 方法：使用正则 `\[~([^\]]+)\]` 提取所有 mention 标记
    - 实现 `MentionInfo` 类：包含 Value、IsExternal（邮箱正则判断 `^[^@\s]+@[^@\s]+\.[^@\s]+$`）、Key（小写去重标识）
    - 实现 `GetNewMentions(current, previous)` 方法：计算增量 diff（new - old，基于 Key 比较）
    - 内置去重逻辑：基于 Key 的 GroupBy 去重
    - _Requirements: 1.1, 1.6, 2.5, 3.3_

  - [ ]\* 1.2 编写 MentionParser 属性测试
    - **Property 1: Mention 解析完整性**
    - **Property 2: 内部/外部分类正确性**
    - **Property 3: 增量 diff 正确性**
    - **Property 4: 去重一致性**
    - **Validates: Requirements 1.1, 1.6, 2.5, 3.3**

  - [ ]\* 1.3 编写 MentionParser 单元测试
    - 测试空 content 返回空列表
    - 测试单个内部用户 mention 提取
    - 测试单个外部邮箱 mention 提取
    - 测试混合多个 mention 提取
    - 测试重复 mention 去重
    - 测试 content 中无 mention 标记
    - _Requirements: 1.1, 2.5, 3.3_

- [x] 2. 创建 InternalNoteMentionEvent 领域事件
  - [x] 2.1 定义 InternalNoteMentionEvent 事件类
    - 创建文件 `Domain.Shared/Events/InternalNoteMentionEvent.cs`
    - 继承 `INotification`（MediatR），遵循 `OnboardingStageCompletedEvent` 模式
    - 包含字段：EventId、Timestamp、TenantId、NoteId、Content、PreviousContent、OnboardingId、SenderName、CaseName、CaseCode、StageName
    - _Requirements: 1.8_

- [x] 3. 扩展 IEmailService 接口并实现 SendMentionNotificationAsync
  - [x] 3.1 在 IEmailService 接口新增 SendMentionNotificationAsync 方法签名
    - 修改文件 `Application.Contracts/IServices/OW/IEmailService.cs`
    - 新增方法：`Task<bool> SendMentionNotificationAsync(string to, string senderName, string caseName, string caseCode, string stageName, string noteContent, string onboardingUrl)`
    - _Requirements: 1.3, 2.4_

  - [x] 3.2 在 EmailService 中实现 SendMentionNotificationAsync
    - 修改文件 `Application/Services/MessageCenter/EmailService.cs`
    - 实现邮件 HTML 模板，包含：senderName、caseCode、caseName、stageName、noteContent、onboardingUrl
    - 邮件主题格式：`[FlowFlex] {senderName} mentioned you in {caseName}`
    - 正文包含跳转链接：`{BaseUrl}/onboard/onboardDetail?onboardingId={onboardingId}`
    - _Requirements: 1.3, 2.4_

  - [ ]\* 3.3 编写 SendMentionNotificationAsync 单元测试
    - **Property 5: 邮件内容完整性**
    - 验证邮件主题格式正确
    - 验证邮件正文包含所有必要字段
    - **Validates: Requirements 1.3, 2.4**

- [x] 4. Checkpoint - 确保基础组件可用
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. 创建 InternalNoteMentionHandler 事件处理器
  - [x] 5.1 实现 InternalNoteMentionHandler
    - 创建文件 `Application/Notification/InternalNoteMentionHandler.cs`
    - 实现 `INotificationHandler<InternalNoteMentionEvent>`
    - 注入 IEmailService、IUserRepository、ILogger
    - Handle 方法逻辑：调用 MentionParser.ParseMentions 解析当前 content
    - 编辑场景：解析 PreviousContent 并调用 GetNewMentions 计算增量 diff
    - 去重：按 Key GroupBy 取 First
    - 对每个 unique mention：IsExternal 直接使用邮箱，否则通过 IUserRepository 查找内部用户邮箱
    - 用户不存在时记录 Warning 日志并跳过
    - 调用 EmailService.SendMentionNotificationAsync 发送邮件
    - 单个 mention 发送失败 try-catch 记录 Error 日志，不影响其他 mention
    - 顶层 try-catch 所有异常并记录日志，不 throw
    - _Requirements: 1.2, 1.4, 1.5, 1.6, 2.3, 3.1, 3.2, 3.3_

  - [ ]\* 5.2 编写 InternalNoteMentionHandler 单元测试
    - **Property 6: 错误隔离性**
    - 测试创建场景：验证对每个 unique mention 调用一次 EmailService
    - 测试编辑场景：验证只对 new-old diff 调用 EmailService
    - 测试用户不存在场景：验证跳过并记录日志
    - 测试异常场景：Mock EmailService 抛异常，验证方法正常返回
    - **Validates: Requirements 1.2, 1.4, 1.5, 1.6, 3.1, 3.2**

- [x] 6. 改造 InternalNoteService（注入 IMediator，发布事件）
  - [x] 6.1 修改 InternalNoteService 注入 IMediator 并在 CreateAsync 中发布事件
    - 修改文件 `Application/Services/OW/InternalNoteService.cs`
    - 构造函数新增 `IMediator _mediator` 注入
    - CreateAsync 中保存成功后：获取 senderName、caseName、caseCode、stageName
    - 发布 `InternalNoteMentionEvent`（PreviousContent = null）
    - 填充 MentionedUserIds 字段（使用统一列表格式，ext: 前缀区分外部邮箱）
    - _Requirements: 1.7, 1.8, 5.1_

  - [x] 6.2 修改 InternalNoteService UpdateAsync 中发布事件
    - 修改文件 `Application/Services/OW/InternalNoteService.cs`
    - 在两个 UpdateAsync 重载中：记录更新前的 Content 作为 PreviousContent
    - 更新成功后发布 `InternalNoteMentionEvent`（含 PreviousContent，用于增量 diff）
    - 更新 MentionedUserIds 字段
    - _Requirements: 1.6, 1.8, 5.1_

- [x] 7. Checkpoint - 确保后端全链路可用
  - Ensure all tests pass, ask the user if questions arise.

- [x] 8. 改造前端 useInternalNoteUsers hook 支持外部邮箱
  - [x] 8.1 修改 useInternalNoteUsers hook 的 remoteMethod 逻辑
    - 修改文件 `packages/flowFlex-common/src/app/hooks/useInternalNoteUsers.ts`
    - remoteMethod 中增加邮箱格式正则检测：`/^[^\s@]+@[^\s@]+\.[^\s@]+$/`
    - 当输入匹配邮箱格式且不在内部用户列表中时，在 assignOptions 末尾添加"添加外部邮箱"选项
    - 外部邮箱选项包含 `isExternal: true` 标记
    - 使用邮箱作为 key 和 value
    - _Requirements: 4.1, 4.2, 4.3_

- [x] 9. 改造 mention.vue 组件支持外部邮箱视觉区分
  - [x] 9.1 修改 mention.vue 候选列表和插入逻辑
    - 修改文件 `packages/flowFlex-common/src/app/components/mention/mention.vue`
    - 候选列表中为外部邮箱选项增加视觉区分（邮箱 icon 或 "External" 标签）
    - 选中外部邮箱后以 `[~email@domain.com]` 格式插入 content
    - 渲染时识别 mention value 是否为邮箱格式，邮箱格式使用邮箱样式 tag 展示
    - _Requirements: 2.1, 2.2, 2.6, 4.4_

- [x] 10. 改造 InternalNotes.vue 渲染 mention 样式
  - [x] 10.1 修改 InternalNotes.vue 的 renderNoteContent 方法
    - 修改文件 `packages/flowFlex-common/src/app/views/onboard/onboardingList/components/InternalNotes.vue`
    - renderNoteContent 方法中识别 `[~xxx]` 格式的 mention 标记
    - 邮箱格式和用户名格式的 mention 统一使用高亮样式 tag 展示
    - _Requirements: 2.6_

- [x] 12. 重构 mention 存储格式（{{mention:type:...}} 结构化标记）
  - [x] 12.1 重构 MentionParser.cs 支持新格式
    - 修改文件 `packages/flowFlex-backend/Application/Services/OW/MentionParser.cs`
    - 新存储格式：内部用户 `{{mention:user:userId:username:displayName}}`，外部邮箱 `{{mention:email:address}}`
    - 更新 `MentionRegex` 为 `\{\{mention:(user|email):([^}]+?)\}\}`
    - 重构 `MentionInfo` 类：新增 `MentionType` 属性（"user"/"email"），新增 `UserId` 属性，新增 `Username` 属性
    - user 类型解析：从 group2 按前两个 `:` 分割得到 userId、username、displayName
    - email 类型解析：group2 整体作为邮箱地址
    - `IsExternal` 改为基于 MentionType == "email" 判断
    - `Key` 改为：user 类型用 userId，email 类型用邮箱小写
    - _Requirements: 1.1, 2.5_

  - [x] 12.2 重构 InternalNoteMentionHandler.cs 适配新格式
    - 修改文件 `packages/flowFlex-backend/Application/Notification/InternalNoteMentionHandler.cs`
    - 内部用户查找改为：通过 `mention.UserId`（long 类型 Id）查找用户获取邮箱
    - 使用 `IBaseRepository.GetByIdAsync(long.Parse(mention.UserId))` 替代 username 查找
    - 外部邮箱处理不变：直接使用 `mention.Value` 作为邮箱
    - _Requirements: 1.2, 1.5_

  - [x] 12.3 更新 InternalNoteService.cs 的 MentionedUserIds 填充逻辑
    - 修改文件 `packages/flowFlex-backend/Application/Services/OW/InternalNoteService.cs`
    - CreateAsync 和 UpdateAsync 中的 mentionedIds 映射逻辑适配新 MentionInfo 结构
    - user 类型：直接存 `mention.UserId`
    - email 类型：存 `ext:{mention.Value}`
    - _Requirements: 1.7_

  - [x] 12.4 重构 useInternalNoteUsers.ts 的 options 结构和 mentionUserMap
    - 修改文件 `packages/flowFlex-common/src/app/hooks/useInternalNoteUsers.ts`
    - options 结构改为：`{ value: item.username, label: item.name, userId: item.id, email: item.email }`
    - initAssign 同步修改：`value: username, label: displayName, userId: id`
    - 新增导出 `mentionUserMap: Map<string, { userId: string, displayName: string }>`（username → 用户信息）
    - fetchOptions 时填充 mentionUserMap
    - 外部邮箱选项保持：`{ value: email, label: email, isExternal: true }`
    - _Requirements: 4.1, 2.5_

  - [x] 12.5 重构 mention.vue 的 get/set 逻辑适配新存储格式
    - 修改文件 `packages/flowFlex-common/src/app/components/mention/mention.vue`
    - 从 hook 导入 `mentionUserMap`
    - `get` computed：将 `{{mention:user:userId:username:displayName}}` 转为 `@username`（提取第3段 username）；将 `{{mention:email:address}}` 转为 `@address`
    - `set` computed：将 `@xxx` 转为存储格式 —— 查 mentionUserMap[xxx] 得到 userId/displayName，生成 `{{mention:user:userId:username:displayName}}`；若 xxx 匹配邮箱格式则生成 `{{mention:email:xxx}}`；若都不匹配则保留原文本
    - _Requirements: 2.5, 4.4_

  - [x] 12.6 重构 InternalNotes.vue 的 parseNoteContent 适配新格式
    - 修改文件 `packages/flowFlex-common/src/app/views/onboard/onboardingList/components/InternalNotes.vue`
    - `parseNoteContent` 正则改为匹配 `{{mention:(user|email):...}}`
    - 解析 user 类型提取 displayName 作为渲染文本（`@Kai Li`）
    - 解析 email 类型提取邮箱作为渲染文本（`@ext@company.com`）
    - 渲染样式保持不变（蓝色高亮 tag）
    - _Requirements: 2.6_

- [~] 11. Final Checkpoint - 确保全部功能集成正确
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- 后端采用 MediatR 领域事件模式，与项目现有 ComponentDeletedCleanupHandler 等模式一致
- Handler 内 try-catch 保证邮件发送异常不影响 note 保存主流程
- 属性测试使用 xUnit + FsCheck，每个属性最少 100 次迭代
- 前端改造保持与 Element Plus ElMention 组件的兼容性
- MentionedUserIds 使用统一列表格式 `["userId", "ext:email@domain.com"]` 保持向后兼容

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "2.1", "3.1"] },
    { "id": 1, "tasks": ["1.2", "1.3", "3.2"] },
    { "id": 2, "tasks": ["3.3", "5.1"] },
    { "id": 3, "tasks": ["5.2", "6.1"] },
    { "id": 4, "tasks": ["6.2"] },
    { "id": 5, "tasks": ["8.1", "9.1", "10.1"] },
    { "id": 6, "tasks": ["12.1", "12.4"] },
    { "id": 7, "tasks": ["12.2", "12.3", "12.5", "12.6"] }
  ]
}
```
