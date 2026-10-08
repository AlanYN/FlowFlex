# OW-731 Adobe Sign Integration

> Jira: [OW-731](https://jira.logisticsteam.com/browse/OW-731)
> 优先级: P1 | 状态: New | 负责人: Kai Li
> Sprint: OW.2026.09/04-09/17

---

## 一、背景与核心价值

### 1.1 为什么需要 Adobe Sign

| 场景         | 当前痛点                | 解决方案                                       |
| ------------ | ----------------------- | ---------------------------------------------- |
| 正式合同签署 | WFE 内置签名法律效力弱  | Adobe Sign 提供符合 ESIGN Act/eIDAS 的法律效力 |
| 外部客户签署 | 客户无 WFE 账号无法签署 | 通过邮件发送签署链接，无需注册                 |
| 多人签署     | MVP 不支持多人签署      | 原生支持多签署人 + 签署顺序控制                |
| 法律证据     | 无完整审计追踪          | 自动生成审计追踪 PDF（法院级证据）             |
| 身份验证     | 仅依赖 WFE 登录         | Adobe Sign 提供邮箱/短信等多重验证             |

### 1.2 与现有 Quick Sign 的对比

| 对比项     | 快速签署 - WFE 内置         | 法律签署 - Adobe Sign           |
| ---------- | --------------------------- | ------------------------------- |
| 实现方式   | 本地手写签名 + pdf-lib 合成 | 邮件发送签署链接                |
| 特点       | 即时完成、无外部依赖        | 多人签署、审计追踪、法律效力强  |
| 适用场景   | 内部审批、简单签收          | 正式合同、外部客户签署          |
| 法律效力   | 无                          | 有（符合 ESIGN Act / eIDAS）    |
| 审计追踪   | 无                          | 有（Certificate of Completion） |
| 签署人范围 | WFE 内部用户                | 任意邮箱，无需 WFE 账号         |

两者并存，互不影响。

### 1.3 账号状态

Adobe Sign Enterprise 账号已购买（Transaction Plan），账号已配置完成（VIP order 153141448），可登录 https://adminconsole.adobe.com/ 进行管理。

---

## 二、Stage 配置

### 2.1 配置入口

位置：**Edit Stage → Components Tab → File Management 区域**

新增 toggle：`Enable Adobe Sign`（紧接 `Attachment Management Needed` 下方）

```
File Management
Enable file upload and attachment functionality

○ Attachment Management Needed
    Allow users to upload and manage files in this stage

○ Enable Adobe Sign                              ← NEW
    Allow legally binding signatures via Adobe Sign
```

### 2.2 典型配置场景

| Stage    | Adobe Sign | 说明                         |
| -------- | ---------- | ---------------------------- |
| 内部审批 | 关闭       | 内部流程，用 Quick Sign 即可 |
| 合同签署 | 开启       | 正式合同，需要法律效力       |
| 交付验收 | 开启       | 客户签收，需要法律效力       |
| 资料归档 | 关闭       | 只存档，不需要签署           |

### 2.3 按钮显示规则

| 配置            | PDF 文件显示的按钮                                     |
| --------------- | ------------------------------------------------------ |
| Adobe Sign 关闭 | [Preview] [Download] [Quick Sign]                      |
| Adobe Sign 开启 | [Preview] [Download] [Quick Sign] [Request Legal Sign] |

注意：**仅 PDF 文件**显示签署按钮，非 PDF（jpg、docx 等）不显示；PDF 文件大小限制 **20MB**。

---

## 三、核心用户流程

```
用户在 Stage 附件列表找到 PDF
  ↓
点击 [Request Legal Sign]（仅当 Adobe Sign 已启用 且为 PDF 文件时显示）
  ↓
打开 Request Legal Signature Modal（见 4.1）
  ↓
配置签署人 + 选项，点击 [Send Request]
  ↓
打开 Confirm Signature Request Modal（见 4.2）
  ↓
点击 [Confirm & Send]
  ↓
Loading: 上传 PDF 到 Adobe Sign
  ↓
Success: 显示发送结果（邮件已发送给哪些人）
  ↓
Adobe Sign 发邮件给所有签署人
  ↓
签署人点击邮件链接 → 在 Adobe Sign 页面完成签署
  ↓
（Sequential：按顺序逐一签；Parallel：同时收到邮件）
  ↓
Webhook 回调 WFE → 更新文件状态
  ↓
所有人签完 → 下载已签 PDF + Audit Trail PDF
  ↓
两个文件自动保存到 Stage 附件
```

---

## 四、UI 组件详细规格

### 4.1 Request Legal Signature Modal

```
Request Legal Signature                                    [X]
┌──────────────────────────────────────────────────────────┐
│ 📄 Document: Sales_Contract_v3.pdf                       │
│                                                          │
│ SIGNERS                                                  │
│ 1  Email *    [customer@acme.com              ]          │
│    Name *     [John Smith                    ]           │
│    Role       [Signer ▼]              [Remove]           │
│                                                          │
│ 2  Email *    [legal@ourcompany.com           ]          │
│    Name *     [Jane Doe                      ] [Select User] │
│    Role       [Signer ▼]              [Remove]           │
│                                                          │
│ [+ Add Signer]                                           │
│                                                          │
│ OPTIONS                                                  │
│ Signing Order                                            │
│   ● Sequential (1 signs, then 2, then 3...)              │
│   ○ Parallel (All receive at the same time)              │
│                                                          │
│ Expiration    [30 ▼] days                                │
│                                                          │
│ Message to Signers                                       │
│ [Please review and sign this contract.          ]        │
│                                                          │
│ ⚠ Signers will receive an email from Adobe Sign.        │
│                              [Cancel] [Send Request]     │
└──────────────────────────────────────────────────────────┘
```

字段规格：

- Email：必填，实时校验格式，输入框变红提示
- Name：必填，发送前校验
- Role：下拉选择 `Signer` / `Approver` / `CC`
- `[Select User]`：从 WFE 用户列表快速选择
- 最多 10 个签署人
- Signing Order：Sequential（顺序）/ Parallel（同时）
- Expiration：7 / 14 / 30 / 60 / 90 天
- Message：可选，发给所有签署人的说明

### 4.2 Confirm Signature Request Modal

```
Confirm Signature Request                                  [X]
┌──────────────────────────────────────────────────────────┐
│ 📄 Document: Sales_Contract_v3.pdf                       │
│                                                          │
│ Signers:                                                 │
│   1. John Smith (customer@acme.com)                      │
│   2. Jane Doe (legal@ourcompany.com)                     │
│                                                          │
│ Signing Order: Sequential                                │
│ Expires in: 30 days                                      │
│                                                          │
│ Signers will receive an email from Adobe Sign.           │
│                         [Cancel] [Confirm & Send]        │
└──────────────────────────────────────────────────────────┘
```

### 4.3 Loading / Success 状态

```
Loading:                        Success:
┌─────────────────┐             ┌─────────────────────────┐
│ Sending Request │             │ Request Sent          X │
│                 │             │                         │
│   ⏳            │             │   ✅                    │
│ Sending...      │             │ Request sent!           │
│ Uploading to    │             │                         │
│ Adobe Sign      │             │ Email sent to:          │
│                 │             │ • customer@acme.com     │
└─────────────────┘             │ • legal@ourcompany.com  │
                                │                  [Done] │
                                └─────────────────────────┘
```

### 4.4 文件列表 — Awaiting Signatures 状态

```
Attachments
┌──────────────────────────────────────────────────────────┐
│ 📄 Sales_Contract_v3.pdf (Original)                      │
│    Uploaded: Aug 20, 2026                                │
│    [Preview] [Download]                                  │
│                                                          │
│ 📝 Sales_Contract_v3.pdf   ⏳ Awaiting Signatures        │
│    Requested: Aug 21, 2026 9:00 AM · by Mike Johnson     │
│    Expires: Sep 20, 2026                                 │
│                                                          │
│    1 ✅ John Smith (customer@acme.com)                   │
│         Signed Aug 21, 2:30 PM                          │
│    2 ⏳ Jane Doe (legal@ourcompany.com)                  │
│         Awaiting signature                               │
│                                                          │
│    [View Details] [Send Reminder] [Recall]               │
└──────────────────────────────────────────────────────────┘
```

### 4.5 Send Reminder Modal

```
Send Reminder                                              [X]
┌──────────────────────────────────────────────────────────┐
│ Select signers to remind:                                │
│                                                          │
│ ☑ Jane Doe (legal@ourcompany.com)                        │
│      Status: Awaiting signature                          │
│                                                          │
│ ☐ John Smith (customer@acme.com)                         │
│      Status: Signed ✅                                   │
│                                                          │
│                          [Cancel] [Send Reminder]        │
└──────────────────────────────────────────────────────────┘
```

### 4.6 Recall Confirmation Modal

```
Recall Signature Request                                   [X]
┌──────────────────────────────────────────────────────────┐
│ ⚠ Are you sure you want to recall this request?          │
│                                                          │
│ Document: Sales_Contract_v3.pdf                          │
│                                                          │
│ If recalled:                                             │
│ • All signers will no longer be able to sign             │
│ • Completed signatures will be invalidated               │
│ • This action cannot be undone                           │
│                                                          │
│                              [Cancel] [Recall]           │
└──────────────────────────────────────────────────────────┘
```

### 4.7 文件列表 — Signing Completed 状态

```
Attachments
┌──────────────────────────────────────────────────────────┐
│ 📄 Sales_Contract_v3.pdf (Original)                      │
│    [Preview] [Download]                                  │
│                                                          │
│ 📝 Sales_Contract_v3.pdf   ✅ Signing Completed          │
│    Completed: Aug 21, 2026 4:45 PM                       │
│    1 ✅ John Smith — Aug 21, 2:30 PM                     │
│    2 ✅ Jane Doe — Aug 21, 4:45 PM                       │
│    [View Details]                                        │
│                                                          │
│ 📄 Sales_Contract_v3_Signed_20260821.pdf   ✅ Signed     │
│    [Preview] [Download] [Print]                          │
│                                                          │
│ 📄 Sales_Contract_v3_Audit_Trail.pdf   🔒 Audit          │
│    [Preview] [Download]                                  │
└──────────────────────────────────────────────────────────┘
```

已签文件命名规则：`原文件名_Signed_日期.pdf`（如 `Sales_Contract_v3_Signed_20260821.pdf`）

### 4.8 Signature Details Modal

```
Signature Details                                          [X]
┌──────────────────────────────────────────────────────────┐
│ Document: Sales_Contract_v3.pdf                          │
│ Status: ✅ Signing Completed                             │
│ Agreement ID: CBJCHBCAABAA5x7...                         │
│                                                          │
│ SIGNERS                                                  │
│ 1  John Smith                                            │
│    customer@acme.com · Signer                            │
│    ✅ Signed Aug 21, 2026 2:30:25 PM                     │
│                                                          │
│ 2  Jane Doe                                              │
│    legal@ourcompany.com · Signer                         │
│    ✅ Signed Aug 21, 2026 4:45:12 PM                     │
│                                                          │
│ TIMELINE                                                 │
│ Aug 21, 9:00 AM   🚀 Request initiated by Mike Johnson   │
│ Aug 21, 9:00 AM   📧 Email sent to customer@acme.com     │
│ Aug 21, 2:28 PM   👁 John Smith viewed document          │
│ Aug 21, 2:30 PM   ✍ John Smith signed                   │
│ Aug 21, 2:30 PM   📧 Email sent to legal@ourcompany.com  │
│ Aug 21, 4:42 PM   👁 Jane Doe viewed document            │
│ Aug 21, 4:45 PM   ✍ Jane Doe signed                     │
│ Aug 21, 4:45 PM   ✅ Signing completed, files archived   │
│                                                   [Close] │
└──────────────────────────────────────────────────────────┘
```

### 4.9 Declined / Expired 状态

```
Declined:
┌──────────────────────────────────────────────────────────┐
│ 📝 Sales_Contract_v3.pdf   ❌ Declined                   │
│    Declined: Aug 21, 2026 3:30 PM                        │
│    1 ❌ John Smith — Declined: "Terms need revision"     │
│    2 ⚫ Jane Doe — Cancelled                              │
│    [View Details] [Request New Signature]                │
└──────────────────────────────────────────────────────────┘

Expired:
┌──────────────────────────────────────────────────────────┐
│ 📝 Sales_Contract_v3.pdf   🕐 Expired                    │
│    Expired: Sep 20, 2026                                 │
│    1 ✅ John Smith — Signed                              │
│    2 🕐 Jane Doe — Expired                               │
│    [View Details] [Request New Signature]                │
└──────────────────────────────────────────────────────────┘
```

---

## 五、状态体系

### 5.1 状态颜色与图标

| 状态      | Color     | Hex     | 图标 |
| --------- | --------- | ------- | ---- |
| Awaiting  | Yellow    | #F59E0B | ⏳   |
| Completed | Green     | #10B981 | ✅   |
| Declined  | Red       | #EF4444 | ❌   |
| Expired   | Gray      | #6B7280 | 🕐   |
| Cancelled | Dark Gray | #374151 | ⚫   |

### 5.2 Adobe Sign 状态映射

| Adobe Sign API 状态 | WFE 显示状态 | 触发 Webhook 事件            |
| ------------------- | ------------ | ---------------------------- |
| DRAFT               | 草稿         | -                            |
| OUT_FOR_SIGNATURE   | 等待签署     | AGREEMENT_CREATED            |
| （部分签署）        | 部分签署     | AGREEMENT_ACTION_COMPLETED   |
| SIGNED / APPROVED   | 签署完成     | AGREEMENT_WORKFLOW_COMPLETED |
| ABORTED             | 已拒绝       | AGREEMENT_REJECTED           |
| EXPIRED             | 已过期       | AGREEMENT_EXPIRED            |
| RECALLED            | 已撤回       | AGREEMENT_RECALLED           |

---

## 六、错误处理

| 错误场景            | 错误提示                                                                  | 处理方式                   |
| ------------------- | ------------------------------------------------------------------------- | -------------------------- |
| 文件上传失败        | Document upload failed. Please try again later.                           | 显示重试按钮               |
| 邮箱格式错误        | Please enter a valid email address.                                       | 输入框标红，实时校验       |
| 网络超时            | Network connection timed out. Please check your connection and try again. | 显示重试按钮               |
| Adobe Sign 服务异常 | Signing service is temporarily unavailable. Please try again later.       | 记录日志，显示重试         |
| 文件过大            | File size exceeds 20MB limit.                                             | 阻止发送                   |
| 签署人邮箱弹回      | Email delivery failed: xxx@example.com                                    | 显示在状态中，提示更换邮箱 |

---

## 七、验收标准

### Part 0：Stage 配置

| #   | 需求描述    | 验收标准                                                             |
| --- | ----------- | -------------------------------------------------------------------- |
| 1   | 配置入口    | Edit Stage → Components → File Management 区域显示 Adobe Sign toggle |
| 2   | Toggle 开关 | 开启后该 Stage 的 PDF 文件显示 [Request Legal Sign] 按钮             |
| 3   | 配置生效    | 配置保存后立即生效                                                   |

### Part 1：发起签署

| #   | 需求描述 | 验收标准                                                        |
| --- | -------- | --------------------------------------------------------------- |
| 1   | 入口按钮 | PDF 文件显示 [Request Legal Sign] 按钮（仅当 Stage 配置启用时） |
| 2   | 文件类型 | 仅 PDF 文件显示签署按钮                                         |
| 3   | 文件大小 | 支持最大 20MB 的 PDF 文件                                       |

### Part 2：签署人配置

| #   | 需求描述   | 验收标准                                |
| --- | ---------- | --------------------------------------- |
| 1   | 添加签署人 | 可手动输入签署人邮箱和姓名              |
| 2   | 多签署人   | 支持添加多个签署人（最多 10 人）        |
| 3   | 签署顺序   | 可设置签署顺序（Sequential / Parallel） |
| 4   | 签署角色   | 支持角色：Signer / Approver / CC        |
| 5   | 签署消息   | 可填写发送给签署人的说明消息            |
| 6   | 用户选择   | 可从 WFE 用户列表快速选择签署人         |
| 7   | 邮箱校验   | 输入邮箱时实时校验格式                  |
| 8   | 必填校验   | 发送前校验邮箱和姓名必填                |

### Part 3：签署状态追踪

| #   | 需求描述 | 验收标准                                                       |
| --- | -------- | -------------------------------------------------------------- |
| 1   | 状态显示 | 显示状态：Awaiting / Completed / Declined / Expired / Recalled |
| 2   | 签署进度 | 显示各签署人的签署进度                                         |
| 3   | 实时更新 | 通过 Webhook 实时更新签署状态                                  |
| 4   | 查看详情 | 可查看完整签署详情和时间线                                     |

### Part 4：生命周期管理

| #   | 需求描述 | 验收标准                                |
| --- | -------- | --------------------------------------- |
| 1   | 发送提醒 | 可向未签署的签署人发送提醒邮件          |
| 2   | 撤回签署 | 发起人可撤回未完成的签署请求            |
| 3   | 重新发起 | Declined/Expired 后可基于原文件重新发起 |

### Part 5：已签署文档管理

| #   | 需求描述 | 验收标准                             |
| --- | -------- | ------------------------------------ |
| 1   | 自动归档 | 签署完成后自动下载并存储已签署 PDF   |
| 2   | 审计追踪 | 自动下载并存储 Audit Trail PDF       |
| 3   | 文件命名 | 已签署文件：原文件名*Signed*日期.pdf |
| 4   | 预览下载 | 可预览和下载已签署文档及审计追踪     |

### Part 6：审计与安全

| #   | 需求描述 | 验收标准                         |
| --- | -------- | -------------------------------- |
| 1   | 操作记录 | 签署操作记入 Case Change History |
| 2   | 审计追踪 | 完整记录签署时间、IP、设备等信息 |

---

## 八、后端技术要点

### 8.1 Adobe Sign API 调用清单

| 步骤           | API                                               | 说明                                 |
| -------------- | ------------------------------------------------- | ------------------------------------ |
| 上传 PDF       | `POST /transientDocuments`                        | 返回 transientDocumentId（24h 有效） |
| 创建签署请求   | `POST /agreements`                                | 携带签署人、顺序、过期时间           |
| 查询状态       | `GET /agreements/{agreementId}`                   | Webhook 失败时的备用轮询             |
| 获取签署人详情 | `GET /agreements/{agreementId}/members`           | 各签署人状态和时间戳                 |
| 下载已签 PDF   | `GET /agreements/{agreementId}/combinedDocument`  | 合并版已签文档                       |
| 下载审计追踪   | `GET /agreements/{agreementId}/auditTrail`        | Audit Trail PDF                      |
| 发送提醒       | `POST /agreements/{agreementId}/reminders`        | 向指定签署人催签                     |
| 撤回           | `PUT /agreements/{agreementId}/state` (CANCELLED) | 取消请求                             |

### 8.2 Webhook 事件

需注册以下事件：

- `AGREEMENT_CREATED` → 更新为等待签署
- `AGREEMENT_ACTION_COMPLETED` → 更新某签署人已完成
- `AGREEMENT_WORKFLOW_COMPLETED` → 全部签完，触发下载归档
- `AGREEMENT_REJECTED` → 更新为已拒绝
- `AGREEMENT_EXPIRED` → 更新为已过期
- `AGREEMENT_RECALLED` → 更新为已撤回

### 8.3 数据库

新增 `ff_adobe_sign_agreement` 表（不修改现有表结构）：

| 字段                      | 类型      | 说明                             |
| ------------------------- | --------- | -------------------------------- |
| id                        | bigint    | Snowflake ID                     |
| agreement_id              | varchar   | Adobe Sign Agreement ID          |
| stage_id                  | bigint    | 关联 Stage                       |
| attachment_id             | bigint    | 关联原始附件                     |
| status                    | varchar   | 当前状态                         |
| signers                   | jsonb     | 签署人列表及各自状态             |
| signed_attachment_id      | bigint    | 已签署 PDF 附件 ID（完成后填入） |
| audit_trail_attachment_id | bigint    | Audit Trail PDF 附件 ID          |
| created_by                | bigint    | 发起人                           |
| create_date               | timestamp | 发起时间                         |
| completed_date            | timestamp | 完成时间                         |
| app_code                  | varchar   | 多租户                           |
| tenant_id                 | bigint    | 多租户                           |
| is_valid                  | bool      | 软删除                           |

### 8.4 Stage Entity 变更

`Stage` entity 新增字段：

```csharp
[SugarColumn(ColumnName = "adobe_sign_enabled")]
public bool AdobeSignEnabled { get; set; } = false;
```

---

## 九、MVP 不含（有意排除）

- 嵌入式签署（签署人在 WFE 内签，不跳转 Adobe Sign）
- 签名位置预设（由签署人自行在 Adobe Sign 中选择）
- 模板管理（Adobe Sign Library Templates）
- 批量签署（MegaSign）
- 高级身份验证（短信验证、政府 ID、知识问答）
- 自定义签署字段（文本框、复选框、下拉框等）
- 替换签署人
- 与 WFE Workflow 自动触发集成
- 签署人排序拖拽调整

---

## 十、参考资料

- Adobe Sign API 文档：https://developer.adobe.com/document-services/docs/overview/
- Adobe Sign 管理控制台：https://adminconsole.adobe.com/
- Jira 附件：`Feature_Adobe_Sign_Integration.md`（#908432）、`adobe-sign-integration-guide.md`（#908433）
