# Context: Action Field Lookup — 交互设计（最终版）

> 更新于 2026-05-14，反映所有迭代后的最终交互方案。

## UI 位置

Lookup 配置位于 **Action 编辑弹窗**（ActionConfigDialog）中，作为独立的 **"Pre-Execution Lookup"** 折叠区域，放在 HTTP Configuration 下方、Response Field Mapping 上方。

---

## UI 结构

```
Action 编辑弹窗：
├── Basic Info (Name / Condition / Description / Type)
├── HTTP Configuration (URL / Params / Headers / Body)
├── ▼ Pre-Execution Lookup (独立区域)
│   ├── Detected fields 提示条（自动从 Params/Body 提取 {{placeholder}}）
│   ├── 表格：Source Field (WFE) | Target Param (API) | Lookup 开关 | 删除
│   └── 展开的 LookupConfigPanel：
│       ├── Options Source (endpoint) | Display Field (displayPath)
│       ├── Value Field (valuePath) | Response Path (responsePath)
│       ├── ▸ Custom Headers (可折叠)
│       └── [Test Lookup] 按钮 + 预览结果
├── ▼ Response Field Mapping (现有，返回值映射，不动)
└── Footer (Cancel / Save)
```

---

## 组件清单

| 组件               | 文件路径                                                | 职责                                 |
| ------------------ | ------------------------------------------------------- | ------------------------------------ |
| LookupConfigPanel  | `src/app/components/actionTools/LookupConfigPanel.vue`  | Lookup 配置面板（含 headers + 预览） |
| ActionConfigDialog | `src/app/components/actionTools/ActionConfigDialog.vue` | 包含 Pre-Execution Lookup 区域       |

---

## 关键交互约定

1. **Lookup 开关**：`el-switch` size="small"，打开时展开配置面板
2. **配置面板**：`bg-gray-50 dark:bg-gray-800`，`border-l-4 border-primary`，`rounded-lg p-4`
3. **Custom Headers**：默认收起，点击展开，Key-Value 对输入行
4. **Test 按钮**：必填字段（endpoint/displayPath/valuePath）未填时 disabled，不依赖 integrationId
5. **预览结果**：`el-table` size="small" border max-height="200px"，最多 10 条
6. **数据保存**：Lookup 配置随 Action 一起保存到 `actionConfig.lookupMappings`
7. **数据回显**：从 `actionConfig.lookupMappings` 中恢复

---

## 前端 API

| 方法 | 路径                        | 用途              |
| ---- | --------------------------- | ----------------- |
| POST | `/action/v1/lookup/preview` | Test 按钮预览选项 |
