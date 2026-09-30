# Design — UI设计 + 架构方案 + 测试策略

> 来源：interaction-design + technical-design + test-verification

---

<!-- 来源: interaction-design/design.md -->

## 一、Design Tokens

| Token | 值 | 用途 |
|-------|-----|------|
| `--doc-type-sheet` | #10B981（绿） | Sheet 类型图标色 |
| `--doc-type-doc` | #3B82F6（蓝） | Doc 类型图标色 |
| `--doc-type-slide` | #F59E0B（橙） | Slide 类型图标色 |
| `--card-bg` / hover | #FFFFFF / #F5F7FA | 卡片背景 |
| `--card-shadow` | 0 1px 4px rgba(0,0,0,.08) | 卡片阴影 |
| `--toolbar-height` | 56px | 编辑器顶栏高度（固定值） |
| `--card-radius` | 8px | 卡片圆角 |
| `--grid-gap` | 16px | 卡片网格间距 |

## 二、组件规格摘要

| 组件 | 文件 | Props | Emits |
|------|------|-------|-------|
| DocumentCard | `views/document/list/DocumentCard.vue` | id, unitId, title, docType, modifyDate, createBy | click, rename, delete |
| CreateDocumentDialog | `views/document/list/CreateDocumentDialog.vue` | modelValue: boolean | update:modelValue, created(unitId) |
| UniverSheet | `components/UniverEditor/UniverSheet.vue` | workbookData?, height?, readonly? | mutation, ready |
| UniverDoc | `components/UniverEditor/UniverDoc.vue` | documentData?, height?, readonly? | mutation, ready |
| UniverSlide | `components/UniverEditor/UniverSlide.vue` | slideData?, height? | — |

---

<!-- 来源: technical-design/design.md -->

## 三、架构决策（必须遵守）

| DD | 决策 | 实现要点 |
|----|------|---------|
| DD-01 | `data_json` C# 类型为 `string`，不加 `IsJson = true` | Service 直接存取字符串 |
| DD-02 | 乐观锁而非完整 OT | CollabHub.Ingest 判断 baseRev；前端重试最多 3 次（100/200/400ms） |
| DD-03 | 操作日志 fire-and-forget | store.logMutation 不 await，catch 静默 |
| DD-04 | 列表/编辑器两个 Store 分离 | documentList.ts + documentEditor.ts 独立 |
| DD-05 | `important: '#app'` 隔离 Tailwind | tailwind.config.ts 一行改动 |
| DD-06 | DocumentCard/弹窗放视图目录 | `views/document/list/` 而非 `components/global/` |

## 四、关键 API 端点

| 方法 | 路径 | 权限 |
|------|------|------|
| GET | `ow/document-snapshots/v1/:unitId` | DOCUMENT:READ |
| POST | `ow/document-snapshots/v1` | DOCUMENT:CREATE |
| POST | `ow/document-snapshots/v1/:unitId/save` | DOCUMENT:UPDATE |
| DELETE | `ow/document-snapshots/v1/:id` | DOCUMENT:DELETE |
| POST | `ow/document-snapshots/v1/query` | DOCUMENT:READ |
| POST | `ow/document-operation-logs/v1` | DOCUMENT:UPDATE |
| GET | `ow/document-operation-logs/v1/:unitId` | DOCUMENT:READ |
| WS | `/collab/:unitId` | Bearer Token |

---

<!-- 来源: test-verification/design.md -->

## 五、测试策略摘要

### 后端单元测试（xUnit + Moq，覆盖 BR-01~BR-06）

| 重点用例 | 关联规则 |
|---------|---------|
| SaveAsync_NewDocument_InsertsRecord | BR-01 |
| SaveAsync_ExistingDocument_UpdatesRecord | BR-01 |
| DeleteAsync_SetsIsValidFalse | BR-02 |
| Ingest 版本冲突返回 cs_rej | BR-03 |
| logMutation 失败静默不抛异常 | BR-04 |
| 不同租户间数据隔离 | BR-05 |

### 风险验证重点

| ISS | 验证点 |
|-----|--------|
| ISS-01 | `important: '#app'` 不破坏 ElPlus Dropdown/Popover z-index |
| ISS-02 | 生产环境 Web Worker MIME 类型正确（application/javascript） |
