# Context — 交互设计摘要

> 供下游阶段（technical-design）读取  
> 最后更新：2026-09-22

---

## 一、页面清单

| 页面 | 路由 | 组件文件 |
|------|------|---------|
| 文档列表页 | `/documents/list` | `views/document/list/index.vue` |
| 新建文档弹窗 | （列表页内） | `views/document/list/CreateDocumentDialog.vue` |
| 文档编辑器页 | `/documents/:unitId/edit` | `views/document/editor/index.vue` |

---

## 二、组件清单

| 组件 | 文件 | Props | Emits |
|------|------|-------|-------|
| DocumentCard | `views/document/list/DocumentCard.vue` | id, unitId, title, docType, modifyDate, createBy | click, rename, delete |
| CreateDocumentDialog | `views/document/list/CreateDocumentDialog.vue` | modelValue: boolean | update:modelValue, created(unitId) |
| EditorToolbar | 内联于编辑器页 | title, collaborators, saving, hasUnsaved | save, back, titleChange |
| UniverSheet | `components/UniverEditor/UniverSheet.vue` | workbookData?, height?, readonly? | mutation, ready |
| UniverDoc | `components/UniverEditor/UniverDoc.vue` | documentData?, height?, readonly? | mutation, ready |
| UniverSlide | `components/UniverEditor/UniverSlide.vue` | slideData?, height? | — |

---

## 三、Design Tokens 摘要

| 类别 | 关键 Token |
|------|-----------|
| 类型色 | Sheet=#10B981, Doc=#3B82F6, Slide=#F59E0B |
| 卡片 | bg=#FFFFFF, hover-bg=#F5F7FA, radius=8px, padding=16px |
| 顶栏 | height=56px, bg=#FFFFFF, border-bottom=#E4E7ED |
| 网格 | gap=16px, page-padding=24px |

---

## 四、关键交互约定（technical-design 必须遵守）

| 编号 | 约定 | 影响实现 |
|------|------|---------|
| KD-01 | 编辑器容器高度固定：`calc(100vh - 56px)` | UniverSheet/Doc/Slide 的 height prop 默认值 |
| KD-02 | `workbookData` 不放入 Vue 响应式 | 组件内直接 pass 原始 JSON，不用 `ref()` |
| KD-03 | 操作日志 fire-and-forget | `logMutation` 异步调用，失败静默，不影响编辑主流程 |
| KD-04 | 离开确认三选项 | ElMessageBox + `onBeforeRouteLeave`：保存并离开 / 不保存离开 / 取消 |
| KD-05 | 标题空值保护 | 行内编辑失焦时若 title 为空，自动还原上一次合法值 |
| KD-06 | 协同头像 collaborators 由 useCollaboration composable 维护 | JOIN/LEAVE 消息驱动更新 |

---

## 五、响应式约定

| 断点 | 卡片列数 |
|------|---------|
| ≥ 1280px | 4 列 |
| 960–1279px | 3 列 |
| 640–959px | 2 列 |
