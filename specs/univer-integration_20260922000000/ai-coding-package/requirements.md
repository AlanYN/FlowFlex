# Requirements — 完整产品需求与验收标准

> 来源：requirements-analysis + interaction-design + technical-design + test-verification

---

<!-- 来源: requirements-analysis/requirements.md -->

## 一、用户故事与验收标准

### US-01：文档列表管理

**As a** FlowFlex 用户，**I want to** 在独立的「文档管理」菜单下查看所有文档，**So that** 我可以快速找到、创建或删除文档。

| AC ID | 验收标准 |
|-------|---------|
| AC-01-1 | `/documents/list` 以卡片展示所有文档，显示标题、类型 Tag、创建人、修改时间 |
| AC-01-2 | 支持关键词搜索，300ms debounce，实时过滤 |
| AC-01-3 | 支持类型筛选：All / Sheet / Doc / Slide |
| AC-01-4 | 支持排序：最近修改（默认）/ 创建时间 / 名称 |
| AC-01-5 | 每页 20 条，底部分页组件 |
| AC-01-6 | 每张卡片有「重命名」「删除」操作，删除二次确认 |

### US-02：新建文档

| AC ID | 验收标准 |
|-------|---------|
| AC-02-1 | 点击「+ 新建文档」弹出弹窗，含名称输入框 + 类型三选一 |
| AC-02-2 | 未填名称直接确认时，显示错误提示，弹窗不关闭 |
| AC-02-3 | 填写信息后确认，后端创建空白快照，跳转编辑器页 |
| AC-02-4 | 默认类型为「电子表格（Sheet）」 |

### US-03：编辑 Sheet

| AC ID | 验收标准 |
|-------|---------|
| AC-03-1 | 点击 Sheet 卡片，Univer Sheets 编辑器加载完成 |
| AC-03-2 | 支持公式、条件格式、数据验证、筛选/排序、查找替换、超链接、绘图、批注、便签、表格 |
| AC-03-3 | 保存按钮调用 `getWorkbookData()` 后 POST 到后端 |
| AC-03-4 | `Ctrl+S` 触发保存 |
| AC-03-5 | 文档标题支持行内编辑，失焦自动保存（空值不保存） |
| AC-03-6 | 离开页面前有未保存变更时弹三选确认框 |

### US-04：编辑 Doc

| AC ID | 验收标准 |
|-------|---------|
| AC-04-1 | 点击 Doc 卡片，Univer Docs 编辑器加载完成 |
| AC-04-2 | 支持富文本、超链接、绘图、目录、查找替换、批注 |
| AC-04-3 | 注册 Web Worker 排版插件，大文档不卡顿 |

### US-05：编辑 Slide（P1）

| AC ID | 验收标准 |
|-------|---------|
| AC-05-1 | 点击 Slide 卡片，Univer Slides 编辑器加载完成（底层插件模式） |

### US-06：操作日志记录

| AC ID | 验收标准 |
|-------|---------|
| AC-06-1 | 用户编辑时，前端异步 POST 操作日志，不阻塞编辑（fire-and-forget） |
| AC-06-2 | 日志记录：unitId、操作用户ID/名、时间戳、MutationId、操作参数 JSON |
| AC-06-3 | 后端提供按 unitId 查询日志的 API，支持时间范围和用户过滤 |

### US-07：多人实时协同

| AC ID | 验收标准 |
|-------|---------|
| AC-07-1 | 两人同时打开同一文档，顶栏显示协同者头像 |
| AC-07-2 | 用户 A 编辑后，用户 B 在 1 秒内同步显示变更 |
| AC-07-3 | 并发冲突时后端返回 cs_rej，前端指数退避重试（最多 3 次） |
| AC-07-4 | 协同者离开后，顶栏头像列表自动移除 |

---

<!-- 来源: interaction-design/requirements.md -->

## 二、交互需求（关键条目）

| ID | 交互要求 |
|----|---------|
| IR-L-04 | 卡片悬停：背景色提升，右上角显示「⋯」操作按钮，150ms 过渡 |
| IR-L-06 | 删除确认：`ElMessageBox.confirm`，确认按钮危险色 |
| IR-E-07 | 编辑器容器高度：`height: calc(100vh - 56px)`，宽度 100%，无内外边距 |
| IR-E-09 | 离开确认三选：「保存并离开」/ 「不保存离开」/ 「取消」 |
| KD-02 | `workbookData` 不放入 Vue 响应式（禁止 `ref()` 包裹） |

---

<!-- 来源: technical-design/requirements.md -->

## 三、业务规则

| BR ID | 规则 |
|-------|------|
| BR-01 | `SaveAsync` Upsert：unit_id 存在→UPDATE，不存在→INSERT |
| BR-02 | DELETE 为软删除：`is_valid = FALSE` |
| BR-03 | 协同乐观锁：`baseRev != currentRev` 返回 cs_rej |
| BR-04 | 操作日志 fire-and-forget：失败静默 |
| BR-05 | 多租户隔离：所有查询自动附加 tenant_id + app_code |
| BR-06 | 标题空值保护：空标题不保存，自动还原上一次合法值 |

---

<!-- 来源: test-verification/requirements.md -->

## 四、质量目标

| 指标 | 目标 |
|------|------|
| 后端 Service 单元测试覆盖率 | ≥ 80% |
| 所有 BR 业务规则 | 100% 覆盖 |
| 关键 API 端点 | happy path + 1 个 error case |
| 样式回归 | 现有页面样式无变化 |
