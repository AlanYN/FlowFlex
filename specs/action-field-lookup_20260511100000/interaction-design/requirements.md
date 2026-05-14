# Requirements: Field Lookup — 交互设计需求

## 概述

在 Action 编辑弹窗（ActionConfigDialog）的 Field Mapping 区域，为每行字段映射增加 Lookup 配置能力。交互设计需保持与现有 UI 风格一致（Element Plus + TailwindCSS），且不影响现有字段映射的操作体验。

---

## 交互需求

### IR-001: Field Mapping 表格增加 Lookup 列

**验收标准**：

- AC-IR-001-1: 在现有 Field Mapping 表格中，Direction 列和 Actions 列之间新增"Lookup"列
- AC-IR-001-2: Lookup 列显示一个 `el-switch` 开关，默认关闭
- AC-IR-001-3: 开关标签为空（节省空间），列标题为"Lookup"
- AC-IR-001-4: 开关状态变化时有平滑过渡动画

### IR-002: Lookup 配置面板展开/收起

**验收标准**：

- AC-IR-002-1: 当某行的 Lookup 开关打开时，该行下方展开一个配置面板（使用 `el-collapse-transition`）
- AC-IR-002-2: 配置面板占据表格全宽，视觉上属于该行的子区域
- AC-IR-002-3: 面板背景色使用 `bg-gray-50 dark:bg-gray-800`，与弹窗中其他配置区域风格一致
- AC-IR-002-4: 面板左侧有 4px 的 primary 色竖线，标识归属关系
- AC-IR-002-5: 关闭开关时，面板收起，已填写的数据保留（不清空）

### IR-003: Lookup 配置面板内容

**验收标准**：

- AC-IR-003-1: 面板内使用 2 列网格布局（`grid grid-cols-2 gap-4`）
- AC-IR-003-2: 包含以下输入字段：
  | 字段 | 标签 | Placeholder | 必填 |
  |------|------|-------------|------|
  | endpoint | Options Source | `/api/users?active=true` | ✅ |
  | displayPath | Display Field | `$.full_name` | ✅ |
  | valuePath | Value Field | `$.user_id` | ✅ |
  | responsePath | Response Path | `$.data` (optional) | ❌ |
- AC-IR-003-3: 每个输入框使用 `el-input`，size="default"
- AC-IR-003-4: 必填字段标签后有红色星号
- AC-IR-003-5: 面板底部右侧有"Test"按钮（`el-button` type="primary" plain size="small"）
- AC-IR-003-6: 面板中包含可折叠的"Custom Headers"区域（默认收起），点击标题展开
- AC-IR-003-7: Custom Headers 区域展开后显示 Key-Value 对输入行（`el-input` × 2 + 删除按钮），支持添加/删除多行
- AC-IR-003-8: Custom Headers 的 UI 风格与现有 HttpConfig 的 Headers tab 一致（Key | Value | 删除按钮）

### IR-004: Test 预览功能

**验收标准**：

- AC-IR-004-1: 点击 Test 按钮时，按钮显示 loading 状态
- AC-IR-004-2: 必填字段未填写时，Test 按钮 disabled，hover 时 tooltip 提示"Please fill in required fields"
- AC-IR-004-3: 请求成功后，在 Test 按钮下方展示预览结果区域
- AC-IR-004-4: 预览结果使用 `el-table`，两列：Display | Value，最多显示 10 条
- AC-IR-004-5: 表格上方显示总数提示："Showing 10 of {total} options"
- AC-IR-004-6: 请求失败时，显示 `el-alert` type="error"，内容为错误信息
- AC-IR-004-7: 预览结果区域可通过再次点击 Test 刷新

### IR-005: 表格行展开状态管理

**验收标准**：

- AC-IR-005-1: 多行可以同时展开 Lookup 配置面板（不互斥）
- AC-IR-005-2: 删除某行时，如果该行有展开的面板，面板随行一起移除
- AC-IR-005-3: 新增行时，Lookup 开关默认关闭

### IR-006: 保存交互

**验收标准**：

- AC-IR-006-1: 保存 Action 时，如果有 Lookup 开关打开但必填字段未填写，阻止保存并高亮对应字段
- AC-IR-006-2: 保存成功后，Lookup 配置持久化到 MappingConfig
- AC-IR-006-3: 重新打开 Action 编辑弹窗时，已保存的 Lookup 配置正确回显（开关状态 + 字段值）

---

## 非功能交互需求

| 类别     | 要求                                                                        |
| -------- | --------------------------------------------------------------------------- |
| 响应式   | Drawer 宽度足够时（≥800px），Lookup 面板使用 2 列布局；窄屏时自动切换为单列 |
| 暗色模式 | 所有新增 UI 元素支持 dark mode（使用 TailwindCSS dark: 前缀）               |
| 无障碍   | 所有输入框有 label 关联；Test 按钮有 aria-label                             |
| 性能     | Lookup 面板使用 v-show 而非 v-if（保留 DOM，避免重复渲染）                  |
