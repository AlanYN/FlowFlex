---
inclusion: manual
---

# interaction-design：交互设计与视觉规范

## 职责定位

将业务规范转化为可视化交互设计方案，产出线框图、用户流程图、Design Tokens 和组件清单，为开发提供明确的界面实现参考。

---

## 输入

读取 `docs/{module-name}/requirements-analysis/context.md`，关键字段：
- 用户故事摘要 + AC 完整列表
- 功能范围（In Scope / Out of Scope）
- 商业目标

---

## 输出文件

### specs 输出（可执行规格）

写入 `specs/{module-name}_{YYYYMMDDHHmmss}/interaction-design/`：

| 文件　　　　　　　| 内容　　　　　　　　　　　　　　　|
| -------------------| -----------------------------------|
| `requirements.md` | 交互设计需求、覆盖范围、验收标准　|
| `design.md`　　　 | 线框图 + Design Tokens + 组件架构 |
| `tasks.md`　　　　| 交互设计阶段任务列表　　　　　　　|

### docs 输出（知识库文档）

写入 `docs/{module-name}/interaction-design/`（修改已有文件，或在首次时新建）：

| 文件　　　　　 | 类型 | 内容　　　　　　　　　　　　　　　　　　　　 |
| ----------------| ------| ----------------------------------------------|
| `context.md`　 | 必需 | 精简摘要，供 technical-design 读取　　　　　 |
| `flowchart.md` | 附加 | 每个关键页面的用户操作流程图　　　　　　　　 |
| `demo.html`　　| 附加 | 可交互 HTML 原型，覆盖所有 US 页面和 UI 状态 |
| `index.md`　　 | 必需 | 文件清单 + 完整性校验结果　　　　　　　　　　|

---

## 强制执行的 Skills（按序）

> **执行前检测**：在执行任何 Skill 之前，先检查 `docs/{module-name}/interaction-design/context.md` 是否已存在。
>
> ```
> 已有 interaction-design/context.md？
>   ├── 是（功能新增 / 重构 / UI调整）
>   │     → 读取已有 context.md 和 specs/.../interaction-design/design.md 中的 Design Tokens
>   │     → 跳过步骤 1（ui-ux-pro-max），直接复用已有设计系统
>   │     → 步骤 2（wireframe-generator）仍需执行，但线框图必须沿用已有 Design Tokens
>   │     → 步骤 3（design-token-spec）仅补充新增 token，不重新生成整套
>   └── 否（全新模块）→ 执行全部 4 个步骤
> ```

**第 1 步 — ui-ux-pro-max**（仅全新模块执行）
读取 `.kiro/steering/skills/ui-ux-pro-max/SKILL.md`
执行：`python3 .kiro/steering/skills/ui-ux-pro-max/scripts/search.py "{product_type} {style_keywords}" --design-system -p "{project-name}"`
输出：完整设计系统 → 作为 design-token-spec 的数据来源

**第 2 步 — wireframe-generator**
读取 `.kiro/steering/skills/wireframe-generator/SKILL.md`
输出：页面线框图 → 写入 `specs/.../interaction-design/design.md` 线框图部分
> 已有设计系统时：线框图中所有样式描述必须引用已有 Design Tokens，不得引入新变量

**第 3 步 — design-token-spec**
读取 `.kiro/steering/skills/design-token-spec/SKILL.md`
输出：Design Tokens 表格 → 写入 `specs/.../interaction-design/design.md` Design Tokens 部分
> 已有设计系统时：仅补充本次新增的 token，在已有 token 表格末尾追加，不重新生成整套

**第 4 步 — flowchart-generator**
读取 `.kiro/steering/skills/flowchart-generator/SKILL.md`
输出：用户流程图 → 写入 `docs/.../interaction-design/flowchart.md`

---

## 交互歧义识别（UXAMB）

以下情况标记为 UXAMB：操作路径不明确、状态转换未定义、错误反馈缺失、空状态未设计、加载状态缺失、权限边界模糊。

标注格式：
```
### UXAMB-{编号}
- 关联用户故事：US-{编号}
- 歧义描述：{具体哪个交互环节不明确}
- 设计建议：{UX 给出的交互方案}
- 状态：待确认 / 已确认 / 已忽略
```

---

## 执行规范

1. 执行全部 4 个 Skill（按顺序）
2. 展示 UXAMB 列表，等待用户确认所有交互歧义
3. 将确认后的定义更新到线框图和交互说明
4. 用户确认后，生成所有输出文件，标记阶段为 `completed`

覆盖要求：每个 US 至少对应一个页面线框图和一张用户流程图。

---

## 输出文件规范

### specs/.../interaction-design/requirements.md

```markdown
# Interaction Design — Requirements

## 交付范围

### 必须交付
| 交付物 | 验收标准 |
|--------|---------|
| 线框图 | 每个 US 对应至少一个页面线框图 |
| Design Tokens | 覆盖颜色/字体/间距/圆角/阴影五类 |
| 用户流程图 | 每个 US 对应一张 Mermaid 流程图 |
| demo.html | 单文件，覆盖所有 US 页面和 UI 状态 |

### Out of Scope
- 技术实现细节：属于 technical-design 职责
- 后端 API 设计：属于 technical-design 职责

## 验收标准（EARS 格式）

### AC-UX-1
WHEN 交互设计完成 THEN 系统 SHALL 为每个 US 生成页面线框图，含关键元素表格和所有 UI 状态

### AC-UX-2
WHEN Design Tokens 生成完成 THEN 系统 SHALL 提供 Light 和 Dark 两套颜色值
```

### specs/.../interaction-design/design.md

```markdown
# Interaction Design — Design

## 设计决策

### 决策 1：{设计风格选型}
- **选择**：{选择了哪种设计风格}
- **理由**：{依据}
- **备选方案**：{其他考虑}
- **权衡**：{接受的取舍}

## 线框图

### {页面名称}（US-{NNN}）

**Purpose**: {一句话描述页面功能}

**Layout**:
```
+------------------------------------------+
| [Logo]                    [Nav links]     |
+------------------------------------------+
|  [_________________________]  ← input    |
|  [    Submit Button    ]                  |
+------------------------------------------+
```

**Key Elements**:
| Element | Type | Behavior |
|---------|------|----------|

**States**: default / loading / success / error / empty

## Design Tokens

### Colors
| Token | Light | Dark | Usage |
|-------|-------|------|-------|
| `--color-primary` | {值} | {值} | 按钮、链接 |

### Typography / Spacing / Radius / Shadow
（同上格式）

## 组件清单
| 组件名 | 用途 | 状态列表 |
|--------|------|---------|
```

### specs/.../interaction-design/tasks.md

```markdown
# Interaction Design — Tasks

## 概览
- 总任务数：{N} | P0 任务数：{N} | 预估工时：{N} 天

## 任务列表

### TASK-ID-001：{任务名称}
- **描述**：{足够详细}
- **优先级**：P0 / P1 / P2
- **复杂度**：XS / S / M
- **关联需求**：{US/AC 编号}
- **依赖**：无 / TASK-ID-{N}
- **状态**：pending
```

> 任务 ID 前缀改为 `TASK-ID-`（Interaction Design）

### docs/.../interaction-design/context.md

```markdown
# Interaction Design Context → technical-design

## 模块名称
{module-name}

## 组件清单摘要
| 组件名 | 用途 | 状态列表 |
|--------|------|---------|

## Design Tokens 摘要
- 主色：{--color-primary 值}
- 字体：{--font-family-base 值}
- 基础间距：{--spacing-4 值}

## 关键交互约定
- {功能模块}：{核心交互约定，1-2句}

## 状态
confirmed
```

### docs/.../interaction-design/demo.html（附加）

单文件、离线可用、覆盖所有 US 页面和 UI 状态。

技术约束：
- 纯 HTML + 内联 CSS + 原生 JS，不引用任何外部 CDN
- CSS 变量名与 `specs/.../interaction-design/design.md` 中的 token 名称一致
- US ≤ 5 用 Tab 导航，> 5 用侧边栏导航
- 每个页面实现：default / loading / success / error / empty 五种状态

---

## 完整性校验

| 文件 | 位置 | 缺失时 |
|------|------|--------|
| `requirements.md` | specs | 校验失败，阶段保持 `in_progress` |
| `design.md` | specs | 校验失败，阶段保持 `in_progress` |
| `tasks.md` | specs | 校验失败，阶段保持 `in_progress` |
| `context.md` | docs | 校验失败，阶段保持 `in_progress` |
| `index.md` | docs | 校验失败，阶段保持 `in_progress` |
| `flowchart.md` | docs | 可标记 `pending`，不阻断校验 |
| `demo.html` | docs | 可标记 `pending`，不阻断校验 |

所有必需文件均为 `generated` → 校验通过，标记 interaction-design 为 `completed`。
