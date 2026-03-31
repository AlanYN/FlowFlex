# Skills 目录说明

本目录包含所有可独立调用的 Skill 模块。每个 Skill 封装一项专项能力，可在 Phase 流程中按需激活，也可单独使用。

---

## 目录结构

```
skills/
├── README.md                        ← 本文件
│
├── 需求分析类
│   ├── market-research/             ← 用户研究与市场洞察
│   ├── competitor-analysis/         ← 竞品分析与功能对比
│   ├── priority-matrix/             ← MoSCoW 需求优先级分类
│   ├── feature-spec/                ← 产品需求文档（PRD）撰写
│   ├── requirements-clarify/        ← 模糊需求识别与澄清
│   └── user-story-writer/           ← 用户故事 + 验收标准生成
│
├── 交互设计类
│   ├── ui-ux-pro-max/               ← UI/UX 设计系统（含数据驱动推荐）
│   ├── wireframe-generator/         ← 文本线框图生成
│   ├── design-token-spec/           ← Design Token 规范生成
│   └── flowchart-generator/         ← 业务/用户流程图生成
│
├── 技术方案类
│   ├── tech-stack-advisor/          ← 技术选型建议与权衡分析
│   ├── architecture-confirm/        ← 架构决策确认与锁定
│   ├── risk-analysis/               ← 技术风险识别与缓解策略
│   ├── component-breakdown/         ← UI 组件拆分与 Props 接口定义
│   ├── database-design/             ← 数据库 Schema + ER 图 + 迁移脚本
│   ├── api-design/                  ← RESTful API 设计 + OpenAPI 文档
│   ├── file-structure-generator/    ← 项目目录结构 + 文件职责 + 创建顺序
│   ├── dev-environment-setup/       ← 开发环境配置 + 依赖清单 + 启动步骤
│   └── task-breakdown/              ← 开发任务拆解（含优先级与依赖关系）
│
├── 测试验证类
│   ├── test-case-generator/         ← 测试用例生成（覆盖 AC + 风险项）
│   └── auto-test-runner/            ← 自动化测试执行与结果收集
│
└── 系统工具类
    ├── find-skills/                 ← 从 skills.sh 生态搜索并安装 Skill
    ├── tavily-search/               ← AI 优化的网络搜索（需 Tavily API Key）
    ├── capability-evolver/          ← Agent 自我进化引擎（分析历史 → 改进）
    ├── proactive-agent/             ← 主动式 Agent 架构（WAL 协议 + 上下文持久化）
    └── using-superpowers/           ← Skill 调用规范（确保 Skill 优先于直接响应）
```

---

## 各 Skill 说明

### 需求分析类

| Skill | 职责 | 主要输出 | 在流程中的位置 |
|-------|------|---------|--------------|
| `market-research` | 合成用户研究与市场洞察，识别机会点，构建 Persona | 主题分析报告、Persona、机会优先级 | requirements-analysis（新产品） |
| `competitor-analysis` | 竞品功能对比矩阵、定位分析、赢/输分析 | 竞品对比表、定位差距、战略建议 | requirements-analysis（新产品） |
| `priority-matrix` | 用 MoSCoW 方法对需求列表分类，输出带理由的优先级矩阵 | Must/Should/Could/Won't 分类表 | requirements-analysis |
| `feature-spec` | 撰写结构化 PRD，包含问题陈述、用户故事、需求分类、成功指标 | PRD 文档 | requirements-analysis |
| `requirements-clarify` | 识别模糊需求（5种类型），生成 AMB 清单，提供建议定义 | AMB-NNN 澄清清单 | requirements-analysis（任意类型） |
| `user-story-writer` | 将功能描述转化为 INVEST 原则的用户故事 + GIVEN/WHEN/THEN 验收标准 | US-NNN + AC 列表 | requirements-analysis |

### 交互设计类

| Skill | 职责 | 主要输出 | 在流程中的位置 |
|-------|------|---------|--------------|
| `ui-ux-pro-max` | 数据驱动的 UI/UX 设计系统，含 67 种风格、96 套配色、57 种字体搭配，支持 13 种技术栈 | 完整设计系统（风格+配色+字体+效果） | interaction-design |
| `wireframe-generator` | 基于用户故事生成文本线框图（ASCII 布局 + 元素表 + 状态说明） | 各页面线框图文档 | interaction-design |
| `design-token-spec` | 生成完整 Design Token 规范（颜色/字体/间距/圆角/阴影），支持亮/暗模式 | CSS 变量形式的 Token 规范 | interaction-design |
| `flowchart-generator` | 将流程描述转化为 Mermaid 流程图，支持业务流程图和用户操作流程图 | Mermaid flowchart 代码 | requirements-analysis / interaction-design |

### 技术方案类

| Skill | 职责 | 主要输出 | 在流程中的位置 |
|-------|------|---------|--------------|
| `tech-stack-advisor` | 基于项目需求和团队约束推荐技术栈，每项选型含理由、备选方案、权衡说明 | 技术栈推荐表 + 扩展计划 | technical-design 步骤 1 |
| `architecture-confirm` | 与用户逐项确认架构决策，生成架构锁定声明（ADR） | `architecture-decisions.md`（已锁定） | technical-design 步骤 3 ⚠️ 需确认 |
| `risk-analysis` | 识别技术风险，评估影响/概率，生成带缓解策略的风险登记表 | 风险登记表（🔴/🟡/🟢 评分） | technical-design 步骤 2 |
| `component-breakdown` | 按 Atomic Design 拆分 UI 组件树，定义 Props 接口、状态和依赖关系 | `component-tree.md` | technical-design 步骤 4 |
| `database-design` | 设计数据库 Schema，包含表结构、索引策略、ER 图和迁移脚本 | `database-schema.md` | technical-design 步骤 5 |
| `api-design` | 设计 RESTful API 端点，定义请求/响应格式、错误码和 OpenAPI 文档 | `api-spec.md` | technical-design 步骤 6 |
| `file-structure-generator` | 生成完整项目目录结构，标注每个文件职责和按依赖顺序的创建清单 | `file-structure.md` | technical-design 步骤 7 |
| `dev-environment-setup` | 生成开发环境配置清单，包含运行时安装、依赖包、`.env.example` 和启动步骤 | `dev-setup.md` | technical-design 步骤 8 |
| `task-breakdown` | 将用户故事和架构决策拆解为独立可交付的开发任务，含优先级、复杂度和依赖关系 | `tasks.md`（含具体操作步骤） | technical-design 步骤 9 |

### 测试验证类

| Skill | 职责 | 主要输出 | 在流程中的位置 |
|-------|------|---------|--------------|
| `test-case-generator` | 将 AC 和风险项转化为结构化测试用例，覆盖正常路径、错误处理和边界情况 | TC-NNN 测试用例列表 | test-verification |
| `auto-test-runner` | 检测测试框架，执行测试，将结果映射回 TC-NNN，生成执行报告 | 测试执行结果表 + 失败分析 | test-verification |

### 系统工具类

| Skill | 职责 | 使用场景 |
|-------|------|---------|
| `find-skills` | 从 skills.sh 生态搜索并安装 Skill 包（`npx skills find <query>`） | 需要扩展 Agent 能力时 |
| `tavily-search` | 通过 Tavily API 执行 AI 优化的网络搜索，支持深度搜索和新闻模式 | 需要实时网络信息时（需配置 `TAVILY_API_KEY`） |
| `capability-evolver` | 分析 Agent 运行历史，识别改进点，自主更新代码或记忆以提升性能 | Agent 自我优化场景 |
| `proactive-agent` | 主动式 Agent 架构，含 WAL 协议（写前日志）、工作缓冲区和上下文压缩恢复 | 需要跨会话持久化上下文的长期任务 |
| `using-superpowers` | 定义 Skill 调用规范，确保在任何响应前优先检查并调用相关 Skill | 所有对话的基础规范 |

---

## 在 Phase 流程中的调用关系

```
requirements-analysis
  ├── 新产品：market-research → competitor-analysis → priority-matrix → feature-spec → requirements-clarify → user-story-writer → flowchart-generator
  ├── 功能新增：priority-matrix → feature-spec → requirements-clarify → user-story-writer → flowchart-generator
  ├── 重构：feature-spec → requirements-clarify → user-story-writer → flowchart-generator
  ├── Bug修复：requirements-clarify → user-story-writer
  └── UI调整：feature-spec → requirements-clarify → user-story-writer → flowchart-generator

interaction-design
  └── ui-ux-pro-max → wireframe-generator → design-token-spec → flowchart-generator

technical-design
  ├── 并行：tech-stack-advisor + risk-analysis
  ├── 确认节点：architecture-confirm ⚠️
  ├── 并行：component-breakdown + database-design + api-design
  ├── 确认节点：用户确认组件/数据库/API ⚠️
  ├── 并行：file-structure-generator + dev-environment-setup
  └── 最后：task-breakdown

test-verification
  └── test-case-generator → auto-test-runner
```

---

## 澄清标记说明

各 Skill 在执行过程中遇到歧义时会生成标记，需用户确认后才能继续：

| 标记 | 来源 Skill | 含义 |
|------|-----------|------|
| `AMB-NNN` | requirements-clarify | 需求存在歧义，需确认定义 |
| `APIAMB` | api-design | API 设计存在歧义（如 REST vs GraphQL） |
| `ARCHAMB` | architecture-confirm | 架构决策存在冲突或风险 |
| `COMPAMB` | component-breakdown | 组件职责边界不清晰 |
| `DBAMB` | database-design | 数据库设计存在歧义（如软删除策略） |
| `SETUPAMB` | dev-environment-setup | 环境配置存在差异（如 OS 差异） |
| `STRUCTAMB` | file-structure-generator | 文件结构存在歧义（如测试文件位置） |
| `UXAMB` | wireframe-generator | 页面结构不明确，无法生成线框图 |
