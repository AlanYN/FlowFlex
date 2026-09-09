# Implementation Plan: Adobe Sign Integration (OW-731)

## Overview

集成 Adobe Acrobat Sign 电子签名服务，让用户能在 WFE Stage 附件中对 PDF 文档发起具有法律效力的电子签署请求，支持多人签署、顺序控制，Webhook 驱动状态更新，自动归档已签文档和审计追踪。

参考文档：[docs/OW-731-adobe-sign-integration.md](../../../docs/OW-731-adobe-sign-integration.md)

**关键约束：**

- Adobe Sign API Key 暂为占位符，等待申请后替换 `appsettings.json` 中 `AdobeSign:AccessToken`
- Webhook 端点需 HMAC-SHA256 验签（x-adobesign-clientid header）
- 不破坏现有 Quick Sign（WFE 内置）功能
- 仅 PDF 文件 + Stage 已启用 Adobe Sign 时显示 `[Request Legal Sign]` 按钮
- 文件大小限制 20MB，最多 10 个签署人

依赖链：`Task 1（DB）` → `Task 2（Entity）` → `Task 3（Service）` → `Task 4（Controller）` → `Task 5（前端 API）` → `Task 7（前端 UI）`  
Task 6（前端 Toggle）仅依赖 Task 1，可与 Task 2-4 并行。

---

## Tasks

### Task 1：后端数据层 — Stage 字段扩展 + Migration

- [x] 1.1 修改 `Stage.cs` 新增 `AdobeSignEnabled` 字段
  - 文件：`packages/flowFlex-backend/Domain/Entities/OW/Stage.cs`
  - 在 `AttachmentManagementNeeded` 字段下方新增：
    ```csharp
    /// <summary>Adobe Sign Integration - Enables legal signing via Adobe Sign</summary>
    [SugarColumn(ColumnName = "adobe_sign_enabled")]
    public bool AdobeSignEnabled { get; set; } = false;
    ```

- [x] 1.2 修改 `StageInputDto.cs` 和 `StageOutputDto.cs`
  - 文件：`packages/flowFlex-backend/Application.Contracts/Dtos/OW/Stage/StageInputDto.cs`
  - 文件：`packages/flowFlex-backend/Application.Contracts/Dtos/OW/Stage/StageOutputDto.cs`
  - 两个文件均新增：`public bool AdobeSignEnabled { get; set; } = false;`

- [x] 1.3 新建 `Migration_20260908001_AddAdobeSignEnabledToStage.cs`
  - 文件：`packages/flowFlex-backend/SqlSugarDB/Migrations/Migration_20260908001_AddAdobeSignEnabledToStage.cs`
  - 必须是 `public static class`，包含 `Up` 和 `Down` 方法
  - Up SQL：`ALTER TABLE ff_stage ADD COLUMN IF NOT EXISTS adobe_sign_enabled BOOLEAN NOT NULL DEFAULT FALSE`
  - Down SQL：`ALTER TABLE ff_stage DROP COLUMN IF EXISTS adobe_sign_enabled`
  - 在 `MigrationManager.cs` 的 `migrations` 数组末尾注册

---

### Task 2：后端数据层 — AdobeSignAgreement 实体与 Repository

- [x] 2.1 新建 `AdobeSignAgreement.cs` Entity
  - 文件：`packages/flowFlex-backend/Domain/Entities/OW/AdobeSignAgreement.cs`
  - 继承 `EntityBaseCreateInfo`，表名 `ff_adobe_sign_agreement`
  - 关键字段（snake_case 列名）：
    - `agreement_id` VARCHAR — Adobe Sign 返回的 Agreement ID
    - `onboarding_id` BIGINT — 关联 Onboarding
    - `stage_id` BIGINT — 关联 Stage
    - `source_file_id` BIGINT — 关联原始 PDF（ff_onboarding_file.id）
    - `signed_file_id` BIGINT? — 已签署 PDF 的附件 ID（完成后填入）
    - `audit_trail_file_id` BIGINT? — Audit Trail PDF 的附件 ID
    - `status` VARCHAR — Awaiting / Completed / Declined / Expired / Cancelled
    - `signers` JSONB — 签署人列表及各自状态
    - `signing_order` VARCHAR — Sequential / Parallel
    - `expiration_days` INT — 过期天数
    - `message` VARCHAR? — 给签署人的消息
    - `requested_by` BIGINT — 发起人 ID

- [x] 2.2 新建 Repository 接口和实现
  - `packages/flowFlex-backend/Domain/Repository/OW/IAdobeSignAgreementRepository.cs`：继承 `IBaseRepository<AdobeSignAgreement>`
  - `packages/flowFlex-backend/SqlSugarDB/Repositories/OW/AdobeSignAgreementRepository.cs`：继承 `BaseRepository<AdobeSignAgreement>`，实现 `IScopedService`

- [x] 2.3 新建 `Migration_20260908002_CreateAdobeSignAgreementTable.cs`
  - 文件：`packages/flowFlex-backend/SqlSugarDB/Migrations/Migration_20260908002_CreateAdobeSignAgreementTable.cs`
  - Up SQL：`CREATE TABLE IF NOT EXISTS ff_adobe_sign_agreement (...)` 包含 2.1 所有字段 + 标准审计字段（create_date, modify_date, create_by, modify_by, create_user_id, modify_user_id, is_valid, app_code, tenant_id）
  - `signers` 列类型为 `jsonb`
  - 在 `MigrationManager.cs` 末尾注册（紧接 Task 1 Migration 之后）

---

### Task 3：后端 AdobeSignService — Adobe Sign API 集成

- [x] 3.1 新增 `AdobeSign` 配置节到 `appsettings.json`
  - 文件：`packages/flowFlex-backend/WebApi/appsettings.json`
  - 新增（占位符值，等待实际 Key 后替换）：
    ```json
    "AdobeSign": {
      "BaseUrl": "https://api.na4.adobesign.com/api/rest/v6",
      "AccessToken": "PLACEHOLDER_REPLACE_WITH_REAL_TOKEN",
      "ClientId": "PLACEHOLDER_REPLACE_WITH_REAL_CLIENT_ID",
      "WebhookSecret": "PLACEHOLDER_REPLACE_WITH_REAL_SECRET"
    }
    ```

- [x] 3.2 新建 DTOs（`Application.Contracts/Dtos/OW/AdobeSign/`）
  - `RequestAdobeSignInputDto.cs`：onboardingId, stageId, sourceFileId, signers(List\<AdobeSignerDto\>), signingOrder, expirationDays, message
  - `AdobeSignerDto.cs`：email, name, role（Signer/Approver/CC）, order
  - `AdobeSignAgreementOutputDto.cs`：id, agreementId, status, signers, signingOrder, expirationDays, requestedBy, createDate, completedDate, signedFileId, auditTrailFileId

- [x] 3.3 新建 `IAdobeSignService.cs` 接口
  - 文件：`packages/flowFlex-backend/Application.Contracts/IServices/OW/IAdobeSignService.cs`
  - 实现 `IScopedService`，声明：
    - `RequestSignatureAsync(RequestAdobeSignInputDto dto, long sourceFileBytes[])` → `AdobeSignAgreementOutputDto`
    - `GetAgreementAsync(long id)` → `AdobeSignAgreementOutputDto`
    - `GetAgreementByFileIdAsync(long sourceFileId)` → `AdobeSignAgreementOutputDto?`
    - `SendReminderAsync(long id, List<string> signerEmails)` → `bool`
    - `RecallAgreementAsync(long id)` → `bool`
    - `HandleWebhookAsync(string eventType, string adobeAgreementId)` → `Task`

- [x] 3.4 新建 `AdobeSignService.cs` 实现
  - 文件：`packages/flowFlex-backend/Application/Services/OW/AdobeSignService.cs`
  - 通过 `IConfiguration` 读取 `AdobeSign` 配置节
  - 使用注入的 `IHttpClientFactory` 创建 named client `"AdobeSign"`
  - `RequestSignatureAsync`：
    1. `POST /transientDocuments`（multipart/form-data）上传 PDF，获取 `transientDocumentId`
    2. `POST /agreements` 创建协议（携带签署人列表、顺序、过期时间）
    3. 将结果写入 `ff_adobe_sign_agreement`，返回 DTO
  - `HandleWebhookAsync`：根据 eventType 更新状态；`AGREEMENT_WORKFLOW_COMPLETED` 时调用 Adobe Sign API 下载 combinedDocument 和 auditTrail，存入 `ff_onboarding_file`，已签文件命名规则：`{原文件名}_Signed_{yyyyMMdd}.pdf`
  - 在 `Program.cs` 注册 named HttpClient `"AdobeSign"`（BaseAddress 从配置读取，Authorization Bearer Token header）

---

### Task 4：后端 AdobeSignController + Webhook 端点

- [x] 4.1 新建 `AdobeSignController.cs`
  - 文件：`packages/flowFlex-backend/WebApi/Controllers/OW/AdobeSignController.cs`
  - 继承 `Controllers.ControllerBase`，路由 `[Route("ow/adobe-sign/v{version:apiVersion}")]`
  - 所有业务接口返回 `Success<T>(data)` 格式，错误 `throw CRMException(...)`
  - 接口列表：
    - `POST v1/request` → `RequestSignatureAsync`（需 [Authorize]）
    - `GET v1/{id}` → `GetAgreementAsync`（需 [Authorize]）
    - `GET v1/by-file/{sourceFileId}` → `GetAgreementByFileIdAsync`（需 [Authorize]）
    - `POST v1/{id}/remind` → `SendReminderAsync`（需 [Authorize]）
    - `DELETE v1/{id}` → `RecallAgreementAsync`（需 [Authorize]）
    - `POST v1/webhook` → `HandleWebhookEventAsync`（**不**加 [Authorize]）

- [x] 4.2 实现 Webhook 端点验签
  - Webhook 端点从 Header 读取 `x-adobesign-clientid`
  - 与 `AdobeSign:ClientId` 配置值比对，不匹配返回 401 并记录日志
  - 任何情况下都必须最终返回 200（Adobe Sign 要求，避免重复推送）
  - 捕获所有异常，记录日志，仍返回 200

---

### Task 5：前端类型定义 + API 模块

- [x] 5.1 新建 TypeScript 类型定义
  - 文件：`packages/flowFlex-common/src/types/adobeSign.d.ts`
  - 定义命名空间 `AdobeSign`，包含：`SigningOrder`、`SignerRole`、`AgreementStatus`、`Signer`、`Agreement`、`RequestInput` 类型

- [x] 5.2 新建 API 模块
  - 文件：`packages/flowFlex-common/src/app/apis/ow/adobeSign.ts`
  - 参考 `src/app/apis/ow/documentSigning.ts` 的写法
  - URL 前缀：`ow/adobe-sign/v1`
  - 导出函数：
    - `requestAdobeSign(data: AdobeSign.RequestInput)` → `AdobeSign.Agreement`
    - `getAgreement(id: string)` → `AdobeSign.Agreement`
    - `getAgreementByFileId(sourceFileId: string)` → `AdobeSign.Agreement | null`
    - `sendReminder(id: string, signerEmails: string[])` → `boolean`
    - `recallAgreement(id: string)` → `boolean`

---

### Task 6：前端 Stage 配置 — Adobe Sign Toggle

- [x] 6.1 修改 `StageComponentsSelector.vue` 新增 Adobe Sign toggle
  - 文件：`packages/flowFlex-common/src/app/views/onboard/workflow/components/StageComponentsSelector.vue`
  - 在 Attachment Management Needed toggle 下方（约 300-380 行 File Management 区域）插入：
    ```vue
    <div v-if="getFileComponent().isEnabled" class="flex items-center justify-between mt-3">
      <div>
        <p class="text-sm font-medium">Enable Adobe Sign</p>
        <p class="text-xs text-gray-500">Allow legally binding signatures via Adobe Sign</p>
      </div>
      <el-switch
        :model-value="props.modelValue.adobeSignEnabled"
        @change="updateAdobeSignEnabled"
      />
    </div>
    ```
  - 新增 update 方法（仿照现有 `updateAttachmentManagementNeeded`）：
    ```typescript
    const updateAdobeSignEnabled = (enabled: boolean) => {
      emit("update:modelValue", {
        ...props.modelValue,
        adobeSignEnabled: enabled,
      });
    };
    ```
  - Adobe Sign toggle 只在 File Management 已启用时显示（与 Attachment Management Needed 一致）
  - 保存 Stage 时 `adobeSignEnabled` 通过 v-model 自动传递到后端（后端 Task 1 已处理）

---

### Task 7：前端文件列表 + Adobe Sign 所有 Modal 组件

- [x] 7.1 新建 `adobeSign/` 目录及 5 个 Modal 组件
  - 目录：`packages/flowFlex-common/src/app/views/onboard/onboardingList/components/adobeSign/`
  - 所有组件使用 `<script setup lang="ts">`，Props 使用 `withDefaults(defineProps<Props>(), {...})`，样式参考同目录 `DocumentSigningDialog.vue`

  - **`AdobeSignRequestModal.vue`**（签署人配置弹窗）
    - Props: `visible`, `fileId`, `fileName`, `onboardingId`, `stageId`
    - Emits: `update:visible`, `confirm(data: AdobeSign.RequestInput)`
    - 签署人列表（最多 10 人）：Email（必填+实时验证格式）、Name（必填）、Role（Signer/Approver/CC）、`[Remove]` 按钮、`[Select User]`（调用现有用户选择器）
    - `[+ Add Signer]` 按钮（达 10 人后禁用）
    - OPTIONS 区：Sequential/Parallel radio，Expiration 下拉（7/14/30/60/90天），Message 文本框
    - 底部：`[Cancel]` `[Send Request]`（点击后 emit confirm 并打开 ConfirmModal）

  - **`AdobeSignConfirmModal.vue`**（发送确认弹窗）
    - Props: `visible`, `requestData: AdobeSign.RequestInput`, `fileName`
    - Emits: `update:visible`, `confirmed`
    - 展示：Document 名、Signers 列表、Signing Order、Expires in
    - 底部：`[Cancel]` `[Confirm & Send]`

  - **`AdobeSignDetailsModal.vue`**（签署详情弹窗）
    - Props: `visible`, `agreementId`
    - 加载时调 `getAgreement(agreementId)`
    - 展示：Document、Status、Agreement ID、SIGNERS 列表（含状态/时间）、TIMELINE 事件列表
    - 底部：`[Close]`

  - **`AdobeSignReminderModal.vue`**（发送提醒弹窗）
    - Props: `visible`, `agreementId`, `signers: AdobeSign.Signer[]`
    - 只列出 `status === 'Awaiting'` 的签署人，支持多选（checkbox）
    - 确认后调 `sendReminder(agreementId, selectedEmails)`
    - 底部：`[Cancel]` `[Send Reminder]`

  - **`AdobeSignRecallModal.vue`**（撤回确认弹窗）
    - Props: `visible`, `agreementId`, `fileName`
    - 显示警告文字（已签署将被作废、不可撤销）
    - 确认后调 `recallAgreement(agreementId)`
    - 底部：`[Cancel]` `[Recall]`

- [x] 7.2 修改 `Documents.vue` 新增 Adobe Sign 按钮和状态展示
  - 文件：`packages/flowFlex-common/src/app/views/onboard/onboardingList/components/Documents.vue`
  - 新增 prop：`adobeSignEnabled: boolean`（默认 false，由父组件传入）
  - 在 `fetchDocuments` 完成后，批量查询各 PDF 文件的 Agreement 状态（调 `getAgreementByFileId`），存入本地 Map `agreementMap: Map<string, AdobeSign.Agreement>`
  - 文件行新增逻辑：
    - 仅 PDF 文件（`file.fileType === 'application/pdf'`）且 `adobeSignEnabled === true` 才处理 Adobe Sign
    - 无 Agreement：显示 `[Request Legal Sign]` 按钮（NEW）
    - 有 Agreement：按状态显示标签（颜色见下）和对应操作按钮
  - 状态标签颜色（Tailwind 或内联 style）：
    - Awaiting `#F59E0B` + `[View Details]` `[Send Reminder]` `[Recall]`
    - Completed `#10B981` + `[View Details]`
    - Declined `#EF4444` + `[View Details]` `[Request New Signature]`
    - Expired `#6B7280` + `[View Details]` `[Request New Signature]`
    - Cancelled `#374151`（无操作按钮）
  - 引入并使用 5 个 adobeSign Modal 组件
  - 各按钮操作流程：
    - `[Request Legal Sign]` → 打开 `AdobeSignRequestModal`
    - `AdobeSignRequestModal` confirm → 打开 `AdobeSignConfirmModal`
    - `AdobeSignConfirmModal` confirmed → 调 `requestAdobeSign()` → Loading Toast → Success Toast → `refreshDocumentsSilently()`
    - `[View Details]` → 打开 `AdobeSignDetailsModal`
    - `[Send Reminder]` → 打开 `AdobeSignReminderModal`
    - `[Recall]` → 打开 `AdobeSignRecallModal`，成功后刷新列表
    - `[Request New Signature]` → 复用 `AdobeSignRequestModal`（基于原文件重新发起）
  - **不影响现有 Quick Sign**：`[Quick Sign]` 按钮和 `DocumentSigningDialog` 保持不变

---

## Notes

- Task 1 的 Migration 编写完成后必须在 `MigrationManager.cs` 末尾注册，否则不会执行
- Task 3 的 `HandleWebhookAsync` 在下载文件存入 `ff_onboarding_file` 时，调用现有 `IOnboardingFileService` 或 Repository 写库，不要绕过服务层直接操作数据库
- Task 4 的 Webhook 端点无论发生什么异常都必须返回 200，否则 Adobe Sign 会在 72 小时内重复推送
- Task 7 的 `Documents.vue` 查询 Agreement 状态时，在 `fetchDocuments` 完成后**批量查询**（而非每个文件单独请求），避免 N+1 问题
- API 凭证申请到后，只需替换 `appsettings.json` 中 `AdobeSign:AccessToken` 和 `AdobeSign:ClientId`，代码无需修改
- 已签名文件命名规则：`{原文件名}_Signed_{yyyyMMdd}.pdf`（如 `Sales_Contract_v3_Signed_20260821.pdf`）

---

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1"] },
    { "id": 1, "tasks": ["2", "6"] },
    { "id": 2, "tasks": ["3"] },
    { "id": 3, "tasks": ["4"] },
    { "id": 4, "tasks": ["5"] },
    { "id": 5, "tasks": ["7"] }
  ]
}
```
