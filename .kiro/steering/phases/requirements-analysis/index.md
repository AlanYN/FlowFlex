---
inclusion: manual
---

# requirements-analysis：需求分析与规格定义

## 职责定位

将用户原始需求（自然语言）转化为结构化产品规格。**首先识别需求类型**，根据类型裁剪执行路径，避免对简单需求执行不必要的步骤。

---

## 第零步：需求分类（必须最先执行）

收到用户需求后，**在做任何分析之前**，先判断需求类型并告知用户将走哪条路径：

### 需求类型识别

| 类型 | 判断依据 | 示例 |
|------|---------|------|
| 🆕 **新产品** | 全新项目，无已有代码库 | "我想做一个任务管理 App" |
| ➕ **功能新增** | 已有项目，增加新功能模块 | "在现有系统里加一个消息通知功能" |
| 🔧 **重构** | 已有功能，改变实现方式 | "把现有的 REST API 改成 GraphQL" |
| 🐛 **Bug 修复** | 已有功能出现异常行为 | "用户登录后跳转到错误页面" |
| 🎨 **UI/UX 调整** | 只涉及界面和交互变更 | "重新设计首页布局" |

### 各类型执行路径

#### 🆕 新产品（全流程）
```
需求分析（全部 7 步）→ 交互设计 → 技术方案 → 测试验证
```
执行所有 Skills：market-research → competitor-analysis → priority-matrix → feature-spec → requirements-clarify → user-story-writer → flowchart-generator

#### ➕ 功能新增（跳过市场/竞品）
```
需求分析（步骤 3-7）→ 交互设计 → 技术方案 → 测试验证
```
跳过：market-research、competitor-analysis
执行：priority-matrix → feature-spec → requirements-clarify → user-story-writer → flowchart-generator

#### 🔧 重构（跳过市场/竞品/优先级，交互设计可选）
```
需求分析（步骤 4-7）→ [交互设计，如涉及 UI 变更] → 技术方案 → 测试验证
```
跳过：market-research、competitor-analysis、priority-matrix
执行：feature-spec → requirements-clarify → user-story-writer → flowchart-generator
交互设计：仅在重构涉及 UI 变更时执行，否则跳过

#### 🐛 Bug 修复（最精简路径）
```
需求分析（仅步骤 5-6 + 错误行为流程图）→ 技术方案（仅架构确认 + 任务拆解）→ 测试验证
```
跳过：market-research、competitor-analysis、priority-matrix、feature-spec
跳过：交互设计（整个阶段）
技术方案只需：requirements-clarify → user-story-writer（描述 bug 复现步骤）→ architecture-confirm → task-breakdown
执行：test-case-generator → auto-test-runner

**Bug 修复必须生成流程图**：虽然跳过 `flowchart-generator` skill，但必须在 `docs/.../requirements-analysis/flowchart.md` 中手动生成两张对比图：
- **当前错误行为流程图**：描述 bug 触发路径和异常结果
- **预期正确行为流程图**：描述修复后应有的正确流程

这两张图是开发者理解修复目标的关键依据，不得省略。

#### 🎨 UI/UX 调整（跳过市场/竞品，直接交互设计）
```
需求分析（步骤 4-7）→ 交互设计（全部）→ 技术方案（仅组件+任务）→ 测试验证
```
跳过：market-research、competitor-analysis、priority-matrix
技术方案只需：component-breakdown → file-structure-generator → task-breakdown

### 分类后的告知格式

> 需求类型识别为：**{类型}**
>
> 本次将执行以下路径：
> - ✅ 需求分析：{执行的 Skills}
> - {✅/⏭️} 交互设计：{执行 / 跳过，原因}
> - ✅ 技术方案：{执行的 Skills}
> - ✅ 测试验证
>
> 如果分类有误，请告知，我会调整路径。

---

## 输入

用户直接提供的原始需求文本（自然语言，无格式要求）。本阶段不依赖任何前置文件。

---

## 输出文件

### specs 输出（可执行规格）

写入 `specs/{module-name}_{YYYYMMDDHHmmss}/requirements-analysis/`：

| 文件 | 内容 |
|------|------|
| `requirements.md` | 用户故事 + AC（EARS 格式） |
| `design.md` | 市场调研、竞品分析、优先级决策（按需求类型裁剪） |
| `tasks.md` | 需求分析阶段任务列表 |

### docs 输出（知识库文档）

写入 `docs/{module-name}/requirements-analysis/`（修改已有文件，或在首次时新建）：

| 文件 | 类型 | 内容 |
|------|------|------|
| `context.md` | 必需 | 精简摘要，供下游阶段读取，**必须包含需求类型字段** |
| `flowchart.md` | 附加 | 每个 US 对应一张业务流程图（Bug 修复类型跳过） |
| `index.md` | 必需 | 文件清单 + 完整性校验结果 |

根目录追加写入 `docs/{module-name}/raw-requirements.md`（保留历史记录，不覆盖）。

---

## 首要步骤：上下文扫描与归档路径确认

在开始任何分析之前，**必须**先执行以下步骤：

**步骤 0-1：扫描已有文件**
- 查看 `docs/` 目录，寻找与当前需求相关的模块文件夹
- 查看 `specs/` 目录，寻找历史 spec 记录（格式：`{module-name}_{timestamp}/`）
- 查看相关代码文件（如有）

**步骤 0-2：确认模块名称**
询问用户模块名称（如未提供），确定：
- `{module-name}`：模块名，kebab-case（如 `user-auth`）
- 本次 spec 路径：`specs/{module-name}_{YYYYMMDDHHmmss}/requirements-analysis/`
- docs 路径：`docs/{module-name}/requirements-analysis/`（已存在则修改，不存在则新建）

**步骤 0-3：输出扫描结论**
> - 相关模块：`{module-name}`（已存在 / 不存在）
> - 历史 spec：找到 N 条 / 无记录
> - 已有 docs：列出相关文件 / 无
> - 变更性质：{🆕 全新模块 / ➕ 功能扩展 / 🔧 局部修改 / 🔄 重大重构}
> - docs 处理：{修改已有文件 / 新建文件夹}

确定路径后，将原始需求**原文**追加写入 `docs/{module-name}/raw-requirements.md`（禁止覆盖历史记录）。

**第 1 步 — market-research**（仅 🆕 新产品）
读取 `.kiro/steering/skills/market-research/SKILL.md`
输出：市场调研摘要 → 写入 `specs/.../requirements-analysis/design.md`

**第 2 步 — competitor-analysis**（仅 🆕 新产品）
读取 `.kiro/steering/skills/competitor-analysis/SKILL.md`
输出：竞品对比表格 → 写入 `specs/.../requirements-analysis/design.md`

**第 3 步 — priority-matrix**（🆕 新产品 / ➕ 功能新增）
读取 `.kiro/steering/skills/priority-matrix/SKILL.md`
输出：MoSCoW 优先级矩阵 → 写入 `specs/.../requirements-analysis/design.md`

**第 4 步 — feature-spec**（除 🐛 Bug 修复外均执行）
读取 `.kiro/steering/skills/feature-spec/SKILL.md`
输出：功能规格 → 写入 `specs/.../requirements-analysis/requirements.md`

**第 5 步 — requirements-clarify**（所有类型必须执行）
读取 `.kiro/steering/skills/requirements-clarify/SKILL.md`
输出：已确认的精确需求定义 → 更新 `specs/.../requirements-analysis/requirements.md`

**第 6 步 — user-story-writer**（所有类型必须执行）
读取 `.kiro/steering/skills/user-story-writer/SKILL.md`
输出：完整用户故事列表（含 AC）→ 写入 `specs/.../requirements-analysis/requirements.md`
> Bug 修复类型：用户故事描述复现步骤和预期行为，而非新功能

**第 7 步 — flowchart-generator**（除 🐛 Bug 修复外均执行）
读取 `.kiro/steering/skills/flowchart-generator/SKILL.md`
输出：业务流程图 → 写入 `docs/.../requirements-analysis/flowchart.md`

---

## 执行规范

1. **先执行第零步**：识别需求类型，告知用户执行路径，等待确认
2. 按裁剪后的路径执行对应 Skills
3. 展示 AMB 列表，等待用户确认所有模糊需求
4. 将确认后的定义更新到用户故事
5. 用户确认后，生成所有输出文件，标记阶段为 `completed`

---

## 输出文件规范

### specs/.../requirements-analysis/requirements.md

```markdown
# Requirements Analysis — Requirements

## 需求类型
{🆕 新产品 / ➕ 功能新增 / 🔧 重构 / 🐛 Bug 修复 / 🎨 UI/UX 调整}

## 执行路径
跳过：{跳过的 Skills，如无则填"无"}
执行：{实际执行的 Skills}

## 功能范围

### In Scope（本次交付）
| 功能点 | 优先级 | 说明 |
|--------|--------|------|

### Out of Scope
- {排除项}：{原因}

## 用户故事

### US-001：{故事标题}
**As a** {用户角色} **I want to** {操作} **So that** {价值}
**Priority**: P0 | **Related goal**: BG-01

**Acceptance Criteria**:
- AC-001-1: GIVEN {前置} WHEN {操作} THEN {结果}

## 验收标准（EARS 格式）

### AC-REQ-1
WHEN {条件} THEN 系统 SHALL {行为}
```

### specs/.../requirements-analysis/design.md

```markdown
# Requirements Analysis — Design

## 需求类型
{类型} — 本文件仅包含适用于该类型的分析内容

## 商业目标
- BG-01: {目标描述，可量化}（Bug 修复类型填"修复缺陷，恢复正常功能"）

## 市场调研结论（仅 🆕 新产品）
{市场规模、目标用户画像、核心痛点}

## 竞品分析（仅 🆕 新产品）
| 功能点 | 本产品 | 竞品 A | 竞品 B | 差异化机会 |
|--------|--------|--------|--------|-----------|

## 优先级矩阵（🆕 新产品 / ➕ 功能新增）

### Must Have（P0）
| 功能 | 理由 |
|------|------|

## 核心决策

### 决策 1：{功能范围定义}
- **选择**：{选择了哪些功能作为 P0}
- **理由**：{依据}
- **权衡**：{接受的取舍}
```

### docs/.../requirements-analysis/context.md

```markdown
# Requirements Analysis Context → 下游阶段

## 模块名称
{module-name}

## 需求类型
{🆕 新产品 / ➕ 功能新增 / 🔧 重构 / 🐛 Bug 修复 / 🎨 UI/UX 调整}

## 后续阶段执行路径
- 交互设计：{执行 / 跳过，原因}
- 技术方案：{全部执行 / 裁剪执行，说明跳过的 Skills}
- 测试验证：{全部执行 / 裁剪执行}

## 归档路径
docs/{module-name}/
specs/{module-name}_{YYYYMMDDHHmmss}/

## 商业目标
- BG-01: {目标描述，可量化}

## 功能范围

### In Scope（P0 优先）
| 功能点 | 优先级 | 说明 |
|--------|--------|------|

### Out of Scope
- {排除项}：{原因}

## 用户故事摘要
| US ID | 标题 | 优先级 | AC 数量 |
|-------|------|--------|---------|

## 关键 AC 列表
- AC-001-1: GIVEN {前置} WHEN {操作} THEN {结果}

## 数据实体
- {实体名}：{字段列表}

## 系统集成点
| 集成目标 | 方向 | 用途 |
|---------|------|------|

## 关键约束
- {约束描述}

## 状态
confirmed
```

---

## 完整性校验

| 文件 | 位置 | 缺失时 |
|------|------|--------|
| `requirements.md` | specs | 校验失败，阶段保持 `in_progress` |
| `design.md` | specs | 校验失败，阶段保持 `in_progress` |
| `tasks.md` | specs | 校验失败，阶段保持 `in_progress` |
| `context.md` | docs | 校验失败，阶段保持 `in_progress` |
| `index.md` | docs | 校验失败，阶段保持 `in_progress` |
| `flowchart.md` | docs | 可标记 `pending`，不阻断校验（Bug 修复类型标记为 `skipped`） |

所有必需文件均为 `generated` → 校验通过，标记 requirements-analysis 为 `completed`。
