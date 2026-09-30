# Context — Univer 集成需求分析摘要

> 供下游阶段（interaction-design / technical-design）读取  
> 最后更新：2026-09-22

---

## 一、业务目标

在 FlowFlex 平台内嵌 Univer 在线文档编辑能力，用户无需离开平台即可创建和编辑类 Excel / Word / PPT 文档，并支持多人实时协作。

---

## 二、用户角色

| 角色 | 使用场景 |
|------|---------|
| FlowFlex 普通用户 | 创建、编辑、查看文档 |
| FlowFlex 管理员 | 查看操作日志审计记录 |

---

## 三、核心用户故事摘要

| ID | 故事 | 关键 AC |
|----|------|---------|
| US-01 | 文档列表管理 | 卡片展示 + 搜索 + 类型筛选 + 排序 + 分页 + 删除确认 |
| US-02 | 新建文档 | 弹窗选类型（Sheet/Doc/Slide），默认 Sheet，创建后跳转编辑器 |
| US-03 | 编辑 Sheet | Univer Sheets 全功能，保存（按钮 + Ctrl+S），标题行内编辑，离开确认 |
| US-04 | 编辑 Doc | Univer Docs 全功能 + Web Worker 排版 |
| US-05 | 编辑 Slide | Univer Slides 底层插件模式（P1） |
| US-06 | 操作日志 | 每次 MUTATION 异步上报，后端存储，支持按 unitId/时间/用户查询 |
| US-07 | 实时协同 | SignalR + 乐观锁，冲突重试，顶栏显示协同者头像 |

---

## 四、关键数据实体

| 实体 | 表名 | 核心字段 |
|------|------|---------|
| 文档快照 | `ff_document_snapshot` | unit_id, title, doc_type, data_json(JSONB), revision |
| 操作日志 | `ff_document_operation_log` | unit_id, operator_user_id, mutation_id, params_json, revision |

两表均含多租户字段（tenant_id, app_code）和软删除（is_valid）。

---

## 五、前端页面结构

```
/documents（独立菜单，code: DOCUMENTS）
  /documents/list        ← 列表页（keepAlive）
  /documents/:unitId/edit ← 编辑器页（hidden，activeMenu 指向列表）
```

---

## 六、后端 API 端点摘要

| 路径 | 用途 |
|------|------|
| `ow/document-snapshots/v1` | CRUD + 分页查询 |
| `ow/document-operation-logs/v1` | 操作日志上报与查询 |
| `/collab/:unitId` | SignalR WebSocket 协同 |

---

## 七、技术约束

- `workbookData` 不得放入 `ref/reactive`（性能）
- Tailwind 使用 `important: '#app'` 防止破坏 Univer UI
- Univer 容器必须有明确的像素高度
- 协同方案：简单乐观锁 + 重试，不做完整 OT

---

## 八、开放问题（供后续阶段决策）

| ID | 问题 | 默认假设 |
|----|------|---------|
| OQ-01 | Slide 是否 P0？ | 按 P1 处理，先交付 Sheet + Doc |
| OQ-02 | 操作日志是否需要前端查看入口？ | 本期仅后端存储，不做前端查看页 |
| OQ-03 | 协同光标共享是否本期实现？ | 本期不做，仅显示头像 |
| OQ-04 | 文档是否与业务实体绑定？ | 独立存在，entity_type/entity_id 字段预留但不强制使用 |
