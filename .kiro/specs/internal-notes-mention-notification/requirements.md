# 需求文档

## 简介

本功能为 FlowFlex Internal Notes 的 @mention 机制增加邮件通知能力。当前 @mention 仅作为纯文本标记存储（格式 `[~username]`），不触发任何通知。本需求实现两个核心能力：（1）@mention 内部用户时自动发送邮件通知；（2）支持手动输入外部邮箱地址作为 @mention 目标并触发邮件通知。

## 术语表

- **Internal_Note_System**：FlowFlex 内部备注系统，允许用户在 Onboarding 详情页添加和管理内部备注
- **Mention_Parser**：@mention 解析器，负责从备注内容中提取被 @mention 的用户标识
- **Notification_Service**：通知服务，负责在 @mention 事件发生时向目标用户发送邮件通知
- **Internal_User**：系统内部用户，拥有系统账号和登录权限
- **External_Recipient**：外部收件人，没有系统账号，通过邮箱地址接收通知
- **Mention_Marker**：@mention 标记，内部用户存储格式为 `[~username]`，外部邮箱存储格式为 `[~email@domain.com]`
- **Onboarding_Case**：Onboarding 案例，Internal Note 所关联的业务实体

## 需求

### 需求 1：@mention 内部用户触发邮件通知

**用户故事：** 作为 Onboarding 操作人员，我希望在 Internal Note 中 @mention 内部用户时自动发送邮件通知，以便被 @的同事能及时了解需要关注的备注内容。

#### 验收标准

1. WHEN 用户在 Internal Note 中创建包含 @mention 标记的备注, THE Mention_Parser SHALL 从备注内容中提取所有被 @mention 的内部用户标识
2. WHEN 备注成功保存且包含内部用户 @mention, THE Notification_Service SHALL 向每个被 @mention 的内部用户发送一封邮件通知
3. WHEN 发送 @mention 通知邮件给内部用户, THE Notification_Service SHALL 使用主题行格式 `[FlowFlex] {操作人姓名} mentioned you in {Case Name}`，正文包含操作人姓名、Onboarding Case 编号和名称、所在 Stage 名称、note 完整内容、以及跳转到对应 Onboarding 页面的链接
4. WHEN 备注内容中 @mention 了多个内部用户, THE Notification_Service SHALL 向每个被 @mention 的用户分别发送独立的通知邮件
5. IF @mention 的用户标识在系统中不存在, THEN THE Notification_Service SHALL 跳过该用户的邮件发送并记录警告日志
6. WHEN 用户编辑已有备注并新增了 @mention, THE Notification_Service SHALL 仅向新增的 @mention 用户发送通知，不重复通知已有的 @mention 用户
7. WHEN Internal Note 成功创建, THE Internal_Note_System SHALL 将所有被 @mention 的内部用户 ID 列表持久化到 MentionedUserIds 字段
8. WHEN Internal Note 成功保存, THE Internal_Note_System SHALL 立即自动触发邮件发送流程，无需用户执行额外操作

### 需求 2：支持 @mention 外部人员（手动输入邮箱）

**用户故事：** 作为 Onboarding 操作人员，我希望能在 Internal Note 中手动输入外部邮箱地址作为 @mention 目标，以便通知没有系统账号的外部相关人员。

#### 验收标准

1. WHEN 用户在 @mention 输入框中输入符合邮箱格式的字符串, THE Internal_Note_System SHALL 允许将该邮箱地址作为 @mention 目标添加到备注中
2. WHEN 用户输入的邮箱地址不符合标准邮箱格式, THE Internal_Note_System SHALL 阻止将其作为 @mention 目标添加
3. WHEN 备注保存且包含外部邮箱 @mention, THE Notification_Service SHALL 向该外部邮箱地址发送通知邮件
4. WHEN 发送 @mention 通知邮件给外部收件人, THE Notification_Service SHALL 使用与内部用户相同的邮件格式：主题行 `[FlowFlex] {操作人姓名} mentioned you in {Case Name}`，正文包含操作人姓名、Onboarding Case 编号和名称、所在 Stage 名称、note 完整内容、以及跳转到对应 Onboarding 页面的链接
5. THE Internal_Note_System SHALL 将外部邮箱 @mention 以 `[~email@domain.com]` 格式存储在备注内容中
6. WHEN 前端渲染包含外部邮箱 @mention 的备注内容, THE Internal_Note_System SHALL 以与内部用户 @mention 相同的视觉样式展示外部邮箱

### 需求 3：邮件发送可靠性与异常处理

**用户故事：** 作为系统管理员，我希望 @mention 邮件通知具备基本的可靠性保障，以确保通知不会因异常而丢失且不影响主流程。

#### 验收标准

1. IF 邮件发送过程中发生异常, THEN THE Notification_Service SHALL 记录错误日志并确保 Internal Note 的创建或编辑操作不受影响
2. WHEN @mention 通知邮件发送, THE Notification_Service SHALL 以异步方式执行，不阻塞 Internal Note 的保存响应
3. IF 同一备注中同一用户被 @mention 多次, THEN THE Notification_Service SHALL 仅发送一封通知邮件

### 需求 4：前端 @mention 交互增强

**用户故事：** 作为 Onboarding 操作人员，我希望 @mention 输入交互同时支持选择内部用户和手动输入外部邮箱，以便灵活地 @mention 不同类型的目标。

#### 验收标准

1. WHEN 用户在 @mention 输入框中输入文本, THE Internal_Note_System SHALL 同时展示匹配的内部用户列表供选择
2. WHEN 用户输入的文本匹配邮箱格式且不在内部用户列表中, THE Internal_Note_System SHALL 在候选列表中展示一个"添加外部邮箱"选项
3. WHEN 用户选择"添加外部邮箱"选项, THE Internal_Note_System SHALL 将该邮箱地址插入到备注内容中作为 @mention 标记
4. THE Internal_Note_System SHALL 在 @mention 候选列表中以视觉区分方式展示内部用户和外部邮箱选项

### 需求 5：权限控制

**用户故事：** 作为系统管理员，我希望 @mention 邮件通知的权限规则清晰明确，以便减少不必要的权限配置工作。

#### 验收标准

1. THE Internal_Note_System SHALL 允许所有拥有 Internal Note 添加权限的用户通过 @mention 触发邮件通知，无需额外权限配置
