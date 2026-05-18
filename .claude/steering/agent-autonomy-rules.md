---
inclusion: always
---

# Agent 自主行为规则

## 核心原则

Agent 在执行任务过程中，应主动识别能力缺口并自主扩展，而非被动等待用户指令。以下三类自主行为是被鼓励和期望的。

---

## 1. MCP 服务器：随时创建和使用

### 触发条件

遇到以下场景时，应主动检查是否有可用的 MCP 服务器，没有则建议或直接创建：

- 需要访问外部 API（数据库、第三方服务、内部微服务）
- 需要与开发工具集成（Git、Docker、CI/CD）
- 需要实时数据源（日志、监控、消息队列）
- 需要执行特定领域操作（文件转换、数据迁移、代码生成）

### 执行规则

1. 优先检查已安装的 MCP 服务器（通过 Powers 面板或 `.kiro/settings/mcp.json`）
2. 如果已有合适的 MCP 服务器 → 直接使用，不重复创建
3. 如果没有 → 向用户建议配置，说明用途和配置方式
4. MCP 配置写入 `.kiro/settings/mcp.json`（workspace 级别）或 `~/.kiro/settings/mcp.json`（用户级别）
5. 不覆盖已有配置，只追加新的服务器定义

### 配置模板

```json
{
  "mcpServers": {
    "server-name": {
      "command": "uvx",
      "args": ["package-name@latest"],
      "env": {},
      "disabled": false,
      "autoApprove": []
    }
  }
}
```

---

## 2. Skills：随时创建和使用

### 触发条件

遇到以下场景时，应主动查找或创建 Skill：

- 执行重复性任务（代码审查、测试生成、文档撰写）
- 需要特定领域专业知识（框架最佳实践、设计模式、安全规范）
- 发现现有 Skill 不覆盖当前场景
- 用户描述的工作流可以被标准化

### 执行规则

1. 优先检查 `.kiro/steering/skills/` 下是否已有匹配的 Skill
2. 如果有 → 按 Skill 指引执行，不跳过
3. 如果没有但可能存在公开 Skill → 使用 `npx skills find [query]` 搜索
4. 如果完全没有 → 在 `.kiro/steering/skills/{skill-name}/SKILL.md` 创建新 Skill
5. 新建 Skill 必须包含：name、description、触发条件、执行步骤、输出格式

### 新建 Skill 模板

```markdown
---
name: {skill-name}
description: {一句话描述用途}
inclusion: manual
---

# {Skill 名称}

## 触发条件
- {何时使用}

## 执行步骤
1. {步骤}

## 输出格式
- {产出物}
```

### Skill 使用优先级

```
已有 Skill 完全匹配 → 直接使用
已有 Skill 部分匹配 → 使用并补充
公开生态有可用 Skill → 建议安装
无匹配 Skill → 创建新 Skill 后使用
```

---

## 3. 本体（Ontology）：创建并持续补充

### 什么是本体

本体是项目的结构化知识图谱，记录业务概念、实体关系、术语定义和领域规则。它是所有阶段（需求、设计、开发、测试）的共享语义基础。

### 存储位置

```
.kiro/ontology/
├── glossary.md              ← 术语表：业务术语 ↔ 技术术语的映射
├── entities.md              ← 实体目录：核心业务实体及其关系
├── business-rules.md        ← 业务规则：不变量、约束、计算逻辑
└── domain-events.md         ← 领域事件：事件触发条件和影响范围
```

### 触发条件

以下场景必须检查并更新本体：

| 场景 | 动作 |
|------|------|
| 首次接触项目 | 扫描代码库，初始化本体四个文件 |
| 需求分析阶段 | 从用户故事中提取新术语和实体，补充 glossary 和 entities |
| 技术设计阶段 | 从数据库设计和 API 设计中补充实体关系和业务规则 |
| 发现术语歧义 | 立即在 glossary 中明确定义，消除二义性 |
| 新增业务实体 | 在 entities 中添加实体定义和关系 |
| 新增业务规则 | 在 business-rules 中记录规则和约束 |
| 新增领域事件 | 在 domain-events 中记录事件和影响 |
| Bug 修复揭示隐含规则 | 将隐含规则显式化，补充到 business-rules |

### 各文件格式

#### glossary.md（术语表）

```markdown
# 术语表

| 业务术语 | 英文标识 | 技术对应 | 定义 |
|---------|---------|---------|------|
| 入职流程 | Onboarding | `ff_onboarding` 表 | 新客户从创建到完成所有阶段的完整流程 |
| 阶段 | Stage | `ff_stage` 表 | 入职流程中的一个步骤，包含多个检查项 |
```

#### entities.md（实体目录）

```markdown
# 实体目录

## Onboarding
- 表名：`ff_onboarding`
- 所属模块：OW
- 关联实体：Stage (1:N), ChecklistTask (1:N via Stage)
- 生命周期：Draft → Active → Completed → Archived
```

#### business-rules.md（业务规则）

```markdown
# 业务规则

## BR-001: 阶段完成条件
- 所有必填 ChecklistTask 状态为 Completed 时，Stage 自动标记为 Completed
- 触发条件：ChecklistTask 状态变更
- 影响范围：Stage.Status, Onboarding.Progress
```

#### domain-events.md（领域事件）

```markdown
# 领域事件

## DE-001: OnboardingStageCompleted
- 触发条件：Stage 所有必填任务完成
- 发布方：OnboardingService
- 订阅方：ActionTriggerEventHandler, NotificationService
- 副作用：触发 Action、发送通知、更新进度
```

### 执行规则

1. 本体文件采用追加模式，不删除已有内容
2. 每次修改需在文件末尾记录更新日期和原因
3. 术语一旦定义，在所有 specs 和 docs 中必须使用一致的术语
4. 发现代码中的命名与本体不一致时，主动提醒用户

---

## 自主行为边界

### 可以自主执行（不需要确认）

- 查找和阅读已有的 MCP 服务器、Skills、本体文件
- 在对话中引用和遵循已有的 Skills 指引
- 补充本体中的术语和实体定义（追加，不修改已有内容）

### 需要用户确认后执行

- 创建新的 MCP 服务器配置
- 安装外部 Skill 包
- 创建新的 Skill 文件
- 修改本体中已有的定义（可能影响已有共识）
- 初始化本体文件（首次创建 `.kiro/ontology/`）
