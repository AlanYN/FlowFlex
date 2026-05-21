# Design: Action Questionnaire Context — 需求分析阶段

## 上下文扫描结论

- 相关模块：`action-questionnaire-context`（本次新建 spec 模块；能力上与 `action-field-lookup`、问卷、Stage Condition、HTTP Action 相关）
- 历史 spec 记录：未找到 `specs/action-questionnaire-context_*`；找到相近历史记录 `specs/action-field-lookup_20260511100000/`
- 已有 docs 文件：
  - `docs/action-field-lookup/`
  - 若干问卷/Action 说明文档散落于 `docs/` 根目录
- 变更性质：➕ 功能扩展
- docs 处理方式：后续优先创建 `docs/action-questionnaire-context/`，不在本阶段生成 docs
- 域复杂度评估：🔲 单域（涉及问卷、Action、Stage Condition、HTTP Action，但属于同一条集成能力链路）
- 本次 spec 路径：`specs/action-questionnaire-context_20260518120914/`

---

## 优先级矩阵（MoSCoW）

### Must Have（必须实现）

| ID | 功能 | 对应用户故事 |
|----|------|-------------|
| M-1 | TriggerAction 执行上下文注入当前 Stage 的问卷答案 | US-001 |
| M-2 | 提供 `questionnaireAnswerMap` 和 `questionnaireAnswerByQuestionId` 两种可引用结构 | US-001, US-002 |
| M-3 | HTTP Action 模板解析支持深路径 | US-003 |
| M-4 | 保持 static field / previousActionResult / integrationToken 兼容 | US-005 |
| M-5 | 前端变量提示与真实能力对齐 | US-004 |
| M-6 | 补齐单元测试与链路验证 | US-001~US-005 |

### Should Have（应该实现）

| ID | 功能 | 说明 |
|----|------|------|
| S-1 | 模板变量解析失败时记录结构化 warning 日志 | 便于排查线上配置问题 |
| S-2 | 前端对问卷变量给出推荐示例（按 questionId） | 降低配置门槛 |
| S-3 | 统一 Action 上下文构建职责，避免 TriggerAction 中继续堆积上下文拼装逻辑 | 提高后续扩展性 |

### Could Have（可以实现）

| ID | 功能 | 说明 |
|----|------|------|
| C-1 | 支持数组索引形式模板，如 `questionnaireAnswers[0].answer` | 对调试友好，但不是 CRM 创建场景的核心路径 |
| C-2 | 提供问卷答案调试预览能力 | 可帮助配置排障 |

### Won't Have（本期不做）

| ID | 功能 | 说明 |
|----|------|------|
| W-1 | 问卷题目新增 `questionKey` 产品化能力 | 需要改问卷编辑模型，范围过大 |
| W-2 | 复杂题型到外部系统格式转换产品化 | 另立需求更合适 |
| W-3 | 可视化字段映射器（source selector UI） | 先交付后端能力和基础变量可用性 |
| W-4 | 多 Stage 问卷聚合策略 | 本期先以当前触发 Stage 为准 |

---

## 功能规格说明

### 核心数据流

```text
用户完成问卷 / Stage Condition 满足
    ↓
ConditionActionExecutor.TriggerAction
    ↓
构建 Action 执行上下文（基础字段 + static fields + questionnaire answers + previousActionResult + integrationToken）
    ↓
ActionExecutionService
    ↓
具体执行器（HTTP API / Python / 未来更多）
    ↓
模板变量解析
    ↓
调用 CRM / 外部系统
```

### v1 问卷上下文结构

```json
{
  "questionnaireAnswers": [
    {
      "stageId": "456",
      "questionnaireId": "1001",
      "questionId": "2001",
      "questionText": "Company Name",
      "questionType": "text",
      "answer": "ABC Logistics"
    }
  ],
  "questionnaireAnswerMap": {
    "1001": {
      "2001": "ABC Logistics"
    }
  },
  "questionnaireAnswerByQuestionId": {
    "2001": "ABC Logistics"
  }
}
```

### 取值规则

1. 正式映射以 `questionId` 为准
2. `questionText` 仅用于展示、日志、调试
3. 无问卷值时返回空结构，不抛异常
4. 当多个已完成 Stage 提供同一业务字段时，`questionnaireAnswerByQuestionId` 允许覆盖，但技术设计阶段必须明确覆盖优先级（例如按 Stage 完成时间或 Stage 顺序）

### 模板解析规则（需求阶段约束）

支持下列路径类型：

- 顶层字段：`{{CaseCode}}`
- 嵌套对象：`{{previousActionResult.data.customerCode}}`
- 问卷 map：`{{questionnaireAnswerMap.1001.2001}}`
- 问题直取：`{{questionnaireAnswerByQuestionId.2001}}`

未命中变量的统一行为需要在技术设计阶段明确，但必须满足：
- 不得因单个变量取值失败导致整个 Action 崩溃
- 必须保留排查信号（日志或执行输出）

---

## 约束与假设

### 约束

1. 后端沿用现有 C# / ASP.NET Core / SqlSugar / Newtonsoft.Json 技术栈
2. 本期不引入新的问卷题目标识体系，基于现有 `questionId` 落地
3. 现有 TriggerAction 上下文兼容性必须优先保证
4. 现有前端 Action 配置 UI 只做增量对齐，不进行整体重构

### 假设

1. 当前 CRM Customer 创建场景主要依赖文本、单选、数字、日期等可直接序列化值
2. 当前触发场景以 Stage Condition → TriggerAction → HTTP Action 为主，但 Action 取值可能依赖其他已完成 Stage 的问卷答案
3. v1 以“当前 Case 下所有已完成 Stage 的问卷答案”为默认取值范围，以满足跨 Stage 取值需求
4. 若未来需要跨模板稳定语义 key，后续再引入 `questionKey`

---

## 当前待确认的模糊点

1. **questionId 的覆盖边界**：如果同一 Stage 挂多个问卷，且不同问卷恰好出现同名/同义业务字段，`questionnaireAnswerByQuestionId` 是否接受“后写覆盖前写”？
2. **模板未命中时的行为**：是替换为空字符串，还是保留原模板文本写入请求？当前建议是替换为空字符串并记 warning。
3. **v1 支持的题型边界**：是否明确只承诺文本/单选/多选/数字/日期可直接用于 CRM，同步将附件、复杂 grid 归为非承诺能力？
4. **前端是否需要同步给出推荐问卷变量示例**：例如在变量面板突出展示 `questionnaireAnswerByQuestionId.{questionId}` 形式。
