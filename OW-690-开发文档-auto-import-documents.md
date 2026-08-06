# OW-690 — WFE-Ticketing Integration: Auto Import Documents

> 本文档记录 OW-690 需求分析过程中收集的所有背景信息、技术调研结论和开发决策，
> 供下次开发时直接使用，无需重新调研。

---

## 一、需求来源

**JIRA**: OW-690  
**标题**: WFE-Ticketing Integration - Auto Import Documents and Display on UI  
**优先级**: P0  
**报告人**: Amanda Li  
**负责人**: Kai Li  
**Sprint**: OW.2026.07/24-08/06

### 原始需求描述

WFE 与 Ticketing 系统集成后，Ticketing 创建 Case 时，附件会同步到 WFE 暂存。
但目前用户需要手动操作才能导入：

1. 进入 Case 的 Stage 1
2. 找到 Documents section
3. 点击 **Import from Integration** 按钮
4. 展开列表，勾选附件
5. 点击 **Import** 按钮

**BA 期望**：用户进入 Case Details 页面就可以直接阅览 Contract，不需要任何手动 Import。

**附加 Bug**：上传后的文件在记录里看不到时间（Staging 环境正常有日期，Production 缺失）。

### Documents 区域字段要求

| 字段        | 值                             |
| ----------- | ------------------------------ |
| From        | Ticketing System               |
| Uploaded By | API 名或 Action 名             |
| Date & Time | 必须有（当前 Production 缺失） |

---

## 二、现有文件导入链路

### 2.1 整体流程

```
Ticket 系统                    WFE Integration 配置                  WFE Case 页面
    │                                  │                                    │
创建 Case 时传 entityId ─────→ Attachment Sharing 配置                    │
（如 TKT-456）                  Integration.InboundAttachments             │
                                存: ModuleName + WorkflowId + ActionId     │
                                           │                               │
                               用户点"Import from Integration"            │
                               GET /fetch-inbound-attachments              │
                               → 调 Ticket 系统接口拿附件列表              │
                               → 返回 ExternalAttachmentDto[]  ─────────→ 展示给用户
                                                                           │
                                                             用户勾选 + 点 Import
                                                             POST import-async
                                                             → FileImportTaskService
                                                             → 后台下载 + 上传 OSS
                                                             → 写 ff_onboarding_file
```

### 2.2 关键接口

| 接口                                                                                         | 说明                                      |
| -------------------------------------------------------------------------------------------- | ----------------------------------------- |
| `GET /api/integration/external/v1/fetch-inbound-attachments?systemId=&entityId=&workflowId=` | 调外部系统接口，拉取附件列表              |
| `POST /api/ow/onboardings/{onboardingId}/files/v1/import-async`                              | 异步导入文件（立即返回 taskId，后台处理） |
| `POST /api/ow/onboardings/{onboardingId}/files/v1/import`                                    | 同步导入文件                              |
| `GET /api/ow/onboardings/{onboardingId}/files/v1/import-tasks?stageId=`                      | 查询导入任务进度                          |

### 2.3 关键代码文件

| 文件                                                                         | 说明                                                                             |
| ---------------------------------------------------------------------------- | -------------------------------------------------------------------------------- |
| `WebApi/Controllers/OW/OnboardingFileController.cs`                          | import-async 的 HTTP 入口，路由：`ow/onboardings/{id}/files/v{version}`          |
| `Application/Services/OW/OnboardingFileService.cs`                           | `ImportFilesFromUrlAsync`（同步）、`StartImportTaskAsync`（异步入口）            |
| `Application/Services/OW/FileImportTaskService.cs`                           | 后台任务核心：下载文件 → 上传 OSS → 写 DB，支持重试（最多5次），有 SSRF 安全检查 |
| `Application/Services/Integration/ExternalIntegrationService.cs`             | `FetchInboundAttachmentsFromExternalAsync`：调外部系统接口拉附件列表             |
| `Application.Contracts/Dtos/OW/OnboardingFile/ImportFilesFromUrlInputDto.cs` | import 的入参结构                                                                |
| `Application.Contracts/Dtos/Integration/InboundAttachmentDto.cs`             | `InboundAttachmentItemDto`：Integration 的 Attachment Sharing 配置结构           |

### 2.4 import-async 入参结构

```csharp
ImportFilesFromUrlInputDto
├── OnboardingId    long        // Onboarding ID
├── StageId         long        // 必填，写入哪个 Stage
├── Category        string      // "Document"
├── OperatorId      string      // 由 Controller 从 Context 注入
├── OperatorName    string      // 显示为 "Uploaded By"，Controller 注入
├── TenantId        string      // 后台 Job 必须手动设置（无 HTTP Context）
└── Files[]
    ├── DownloadLink string     // 文件下载 URL
    ├── FileName     string     // 文件名（可选，为空则从 URL 提取）
    ├── Description  string     // 描述（可选）
    └── Source       string     // 显示为 "From" 列，传 "Ticketing System"
```

### 2.5 ExternalIntegrationService 拉附件列表的逻辑

`FetchInboundAttachmentsFromExternalAsync(systemId, entityId, workflowId)` 内部步骤：

1. 通过 `systemId` → `EntityMapping` → 拿到 `IntegrationId`
2. 读 `Integration.InboundAttachments`（JSON 存的 `List<InboundAttachmentItemDto>`）
3. 每个配置项有 `ActionId`，指向一个 Action Definition（Action Definition 里存了调外部系统的 URL）
4. 按 `workflowId` 过滤配置项（支持不同 Workflow 配不同的附件来源）
5. 调外部系统接口，返回 `ExternalAttachmentDto[]`

返回的每条 `ExternalAttachmentDto` 包含：`id / fileName / fileSize / fileType / fileExt / downloadLink / createDate`

### 2.6 InboundAttachmentItemDto 结构

```csharp
public class InboundAttachmentItemDto
{
    public string Id { get; set; }          // 配置项唯一 ID
    public string ModuleName { get; set; }  // 外部模块名
    public long WorkflowId { get; set; }    // 关联的 Workflow ID
    public long ActionId { get; set; }      // 用于调用外部系统的 Action ID
}
```

### 2.7 ff_onboarding_file 表关键字段

| 字段                           | 说明                                  |
| ------------------------------ | ------------------------------------- |
| `is_external_import`           | bool，标记为外部导入                  |
| `source`                       | string，来源（如 "Ticketing System"） |
| `create_by` / `create_user_id` | 上传者（OperatorName / OperatorId）   |
| `create_date`                  | 上传时间                              |

---

## 三、现有 Condition Action 机制

### 3.1 触发时机（现有）

**触发点：用户点击 "Complete Stage" 时**，在标记 Stage 完成**之前**执行：

```
用户点击 Complete Stage
  → CompleteCurrentStageAsync
      → EvaluateAndExecuteStageConditionAsync   ← 触发点
          → RulesEngineService.EvaluateAndExecuteWithTransactionAsync
              → 按 Order 遍历 Conditions（first-match-wins）
              → 条件满足 → 执行 ActionsJson 里的 Actions
              → 无匹配 → 走 FallbackStage
      → 执行成功 → 标记 Stage 完成
```

TriggerAction 失败会抛 `CRMException`，阻断 Stage Complete 并回滚。

### 3.2 现有 Action 类型（7种）

| 类型常量           | 说明              | 超时 |
| ------------------ | ----------------- | ---- |
| `gotostage`        | 跳转到指定 Stage  | 30s  |
| `skipstage`        | 跳过下 N 个 Stage | 30s  |
| `endworkflow`      | 直接结束 Workflow | 30s  |
| `sendnotification` | 发邮件/通知       | 60s  |
| `updatefield`      | 更新字段值        | 30s  |
| `triggeraction`    | 调外部 API        | 45s  |
| `assignuser`       | 重新分配用户/团队 | 30s  |

StageControlActionTypes（互斥，同一 Condition 只能有一个）：GoToStage、SkipStage、EndWorkflow

### 3.3 关键代码文件

| 文件                                                                             | 说明                                                 |
| -------------------------------------------------------------------------------- | ---------------------------------------------------- |
| `Application/Services/OW/StageCondition/RulesEngineService.cs`                   | 规则评估核心，EvaluateAndExecuteWithTransactionAsync |
| `Application/Services/OW/StageCondition/ActionExecutor.cs`                       | 各 Action 类型的具体执行逻辑                         |
| `Application/Services/OW/OnboardingServices/OnboardingStageManagementService.cs` | 触发入口 CompleteCurrentStageAsync                   |
| `Domain.Shared/Const/StageConditionConstants.cs`                                 | 所有 Action 类型常量、超时配置                       |
| `Application.Contracts/Dtos/OW/StageCondition/ConditionAction.cs`                | ConditionAction DTO，含 Parameters 字典可扩展        |
| `packages/flowFlex-common/src/types/condition.d.ts`                              | 前端 TypeScript 类型定义                             |

### 3.4 前端配置页面

- `src/app/views/onboard/workflow/components/condition/StageConditionEditor.vue` — 主编辑器
- `src/app/views/onboard/workflow/components/condition/ConditionCard.vue` — 单条 Condition
- `src/app/views/onboard/workflow/components/condition/ConditionActionForm.vue` — Action 配置表单

---

## 四、技术调研结论与开发决策

### 4.1 潜在风险（已与 BA 沟通）

| 风险               | 说明                                                    |
| ------------------ | ------------------------------------------------------- |
| 用户不需要所有文件 | 自动导入会把所有 Ticketing 附件拉进来，包括用户不需要的 |
| 文件数量失控       | Ticketing 接口可能返回大量附件，Documents 区域混乱      |
| 大文件             | 附件可能几百 MB，影响存储和性能                         |
| 危险文件类型       | .exe/.sh/.zip 等非文档类格式                            |
| 重复导入           | 多次触发同一 Stage 导致文件重复                         |
| 外部接口不稳定     | Ticket 接口超时/宕机                                    |
| 存储费用           | 批量下载占用 OSS 存储                                   |

### 4.2 约束配置（需与 BA 确认默认值）

新 Action 配置时支持以下可配置约束（存在 `ConditionAction.Parameters` 中）：

| 配置项              | 类型     | 建议默认值              | 说明                           |
| ------------------- | -------- | ----------------------- | ------------------------------ |
| `fileTypeWhitelist` | string[] | `["pdf","docx","xlsx"]` | 只导入指定格式                 |
| `maxFileCount`      | int      | 20                      | 单次最多导入 N 个              |
| `maxFileSizeMb`     | int      | 50                      | 单文件大小上限（MB），超出跳过 |

### 4.3 触发时机决策

**最终方案：新增 `triggerOn` 字段，支持 OnEnter 触发**

原因：

- 不同 Stage 的文件类型可能不同（Stage 1 下载 Excel，Stage 2 下载 PDF）
- 每个 Stage 需要独立配置，文件应在进入该 Stage **之前**准备好
- Stage 1 可能是 Workflow 的第一个 Stage，没有前置 Stage 可以在 OnExit 时触发
- 把 Action 配在"需要文件的 Stage"上，语义更直觉，配置者不需要绕一层思考

**两种触发语义：**

```
triggerOn: "OnExit"（默认，现有所有 Action 保持不变）
  → Stage Complete 时触发

triggerOn: "OnEnter"（新增）
  → 进入该 Stage 时触发（异步，不阻塞用户）
  → 触发点：AutoAdvanceToNextStageAsync / SetCurrentStageAsync 里检查新 CurrentStage 的 OnEnter Conditions
```

**时序示意：**

```
Case 创建 → CurrentStage = Stage 1
  → 触发 Stage 1 的 OnEnter Conditions
  → AutoImportDocuments Job 入队（Hangfire 后台）
        ↓ (异步，不阻塞)
用户打开 Stage 1，文件已在（或显示"导入中"状态）

用户 Complete Stage 1
  → 触发 Stage 1 的 OnExit Conditions（GoToStage Stage 2 等）
  → 同时触发 Stage 2 的 OnEnter Conditions（如有）
  → AutoImportDocuments(Stage 2) Job 入队
        ↓
用户进入 Stage 2，文件已在
```

### 4.4 新 Action 类型设计

**Action 类型名**：`AutoImportDocuments`（待定，可调整）

**配置字段**：

```json
{
  "type": "AutoImportDocuments",
  "order": 1,
  "triggerOn": "OnEnter",
  "parameters": {
    "fileTypeWhitelist": ["pdf", "docx", "xlsx"],
    "maxFileCount": 20,
    "maxFileSizeMb": 50
  }
}
```

**执行逻辑（后台 Job）**：

1. 调 `FetchInboundAttachmentsFromExternalAsync(systemId, entityId, workflowId)` 拉附件列表
2. 按约束过滤（文件类型、数量、大小）
3. 查已导入文件，做去重（基于外部 `id` 或 `Source + fileName`）
4. 查目标 Stage ID（即当前进入的 Stage）
5. 构建 `ImportFilesFromUrlInputDto`：
   - `OperatorName = "Auto Import"` （显示在 Uploaded By）
   - `Source = "Ticketing System"`（显示在 From）
   - `TenantId` 从 Onboarding 上取（后台 Job 无 HTTP Context）
6. 调 `StartImportTaskAsync` 执行导入

---

## 五、开发任务拆分

### 后端

| #   | 任务                                                                                                                                                                 | 涉及文件                                                                         |
| --- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------- |
| 1   | `StageCondition` / `ConditionAction` 增加 `triggerOn` 字段（`OnExit` / `OnEnter`）                                                                                   | `Domain/Entities/OW/StageCondition.cs`、`ConditionAction.cs`                     |
| 2   | `StageConditionConstants` 新增 `ActionTypeAutoImportDocuments` 常量                                                                                                  | `Domain.Shared/Const/StageConditionConstants.cs`                                 |
| 3   | `ActionExecutor` 新增 `ExecuteAutoImportDocumentsAsync` 方法                                                                                                         | `Application/Services/OW/StageCondition/ActionExecutor.cs`                       |
| 4   | `OnboardingStageManagementService` 的 `AutoAdvanceToNextStageAsync` / `SetCurrentStageAsync` 中，当 CurrentStage 变更时检查并触发 OnEnter Conditions（异步，不阻塞） | `Application/Services/OW/OnboardingServices/OnboardingStageManagementService.cs` |
| 5   | `AutoImportDocuments` Job 实现：拉附件 → 过滤 → 去重 → 调 `StartImportTaskAsync`                                                                                     | 新建 `Application/Services/OW/AutoImportDocumentsJobService.cs`                  |
| 6   | 去重逻辑：查 `ff_onboarding_file` 表已有记录，避免重复导入                                                                                                           | `Application/Services/OW/OnboardingFileService.cs`                               |
| 7   | 修复 Production 环境 Documents 文件列表缺少 Date & Time 的 Bug                                                                                                       | 待定位具体原因                                                                   |

### 前端

| #   | 任务                                                                  | 涉及文件                                                                      |
| --- | --------------------------------------------------------------------- | ----------------------------------------------------------------------------- |
| 1   | `ConditionActionForm.vue` 新增 Action 类型选项：`AutoImportDocuments` | `src/app/views/onboard/workflow/components/condition/ConditionActionForm.vue` |
| 2   | `ConditionActionForm.vue` 新增 `triggerOn` 选项（OnExit / OnEnter）   | 同上                                                                          |
| 3   | `AutoImportDocuments` 配置表单：文件类型白名单、数量上限、大小上限    | 同上                                                                          |
| 4   | Documents 区域展示"导入中"状态（OnEnter Job 运行时）                  | `detail.vue` 或 Documents 组件                                                |

---

## 六、待确认事项（下次开发前需与 BA/Amanda 对齐）

- [ ] 文件类型白名单、数量上限、大小上限的**默认值**是否符合预期
      已得到答案：
      1：文件类型白名单配置，只自动Import指定格式：pdf, .docx, .xlsx
      2：文件数量上限：单次最多导入 7个
      3：单文件大小上限：20MB，超出则跳过

- [ ] 自动导入的文件，用户是否可以手动删除？还是只读？
- [ ] 如果 Ticket 接口不可达，是静默失败还是给用户提示？
- [ ] Documents 区域"导入中"状态的 UI 设计是否需要 UX 介入
- [ ] 是否需要支持用户手动重新触发导入（Retry）

---

## 七、相关文档

- `WFE外部系统对接指南.md` — 外部系统对接 WFE 的完整 API 文档
- `Ticket系统对接WFE 附件接入指南.md` — Ticket 系统提供附件接口的规范文档

---

_记录时间：2026-08-03_  
_记录人：Kai Li_
