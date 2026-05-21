# Design: Action Questionnaire Context — 技术方案设计

## 1. 技术栈确认

| 层 | 技术 | 说明 |
|----|------|------|
| 后端 | C# / ASP.NET Core | 现有项目技术栈 |
| ORM | SqlSugar | 现有数据访问层 |
| JSON | Newtonsoft.Json + JToken | 现有 Action 执行和模板解析基础 |
| 前端 | Vue 3 + TypeScript + Element Plus | 现有前端技术栈 |
| 测试 | xUnit / 现有 Tests 项目 | 现有后端测试体系 |

---

## 2. 后端架构设计

### 2.1 新增 / 修改文件清单

```text
Application.Contracts/
├── Dtos/Action/ActionQuestionnaireContextDto.cs              ← 新增：问卷上下文 DTO
├── IServices/Action/IActionContextBuilder.cs                 ← 新增：Action 上下文构建接口

Application/
└── Services/
    └── Action/
        ├── ActionContextBuilder.cs                           ← 新增：统一构建 Action 上下文
        └── TemplateVariableResolver.cs                       ← 新增：深路径模板解析器

Application/Services/OW/StageCondition/
└── ActionExecutor.cs                                         ← 修改：TriggerAction 改为调用 ActionContextBuilder

Application/Services/Action/Executors/
└── HttpApiActionExecutor.cs                                  ← 修改：接入 TemplateVariableResolver

Application.Contracts/IServices/OW/
└── IComponentDataService.cs                                  ← 修改：增加按多 Stage 读取问卷数据的方法

Application/Services/OW/
└── ComponentDataService.cs                                   ← 修改：提供多 Stage 问卷聚合支持

Tests/FlowFlex.Tests/
└── Services/
    ├── Action/TemplateVariableResolverTests.cs              ← 新增
    └── OW/ActionExecutorTests.cs                             ← 修改：补链路测试

前端
packages/flowFlex-common/src/app/components/actionTools/
├── VariablesPanel.vue                                        ← 修改
├── VariableAutoComplete.vue                                  ← 修改
└── HttpConfig.vue                                            ← 修改
```

### 2.2 Action 上下文模型

建议最终 Action context 至少包含：

```json
{
  "OnboardingId": 123,
  "StageId": 456,
  "ConditionId": 789,
  "CaseCode": "OW-001",
  "CaseName": "ABC Logistics",
  "WorkflowId": 3001,
  "integrationToken": "xxx",
  "questionnaireAnswers": [
    {
      "stageId": "10",
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
  },
  "previousActionResult": {
    "data": {
      "customerCode": "C0001"
    }
  }
}
```

### 2.3 多 Stage 聚合规则

v1 决策：默认读取当前 Case 下所有已完成 Stage 的问卷答案。

**已确认优先级规则**：
1. 先按 Stage 完成时间排序
2. 后完成的 Stage 覆盖先完成的 Stage
3. 若完成时间不可用，则回退到 Stage 顺序或 StageId 排序

这样 `questionnaireAnswerByQuestionId` 的覆盖规则可测试、可解释。

### 2.4 ActionContextBuilder 设计

```csharp
public interface IActionContextBuilder : IScopedService
{
    Task<Dictionary<string, object>> BuildStageConditionTriggerContextAsync(
        ActionExecutionContext context,
        long actionDefinitionId,
        long? integrationId,
        JToken? previousActionResult = null,
        CancellationToken cancellationToken = default);
}
```

**内部职责**：
1. 读取 onboarding / workflow 基础信息
2. 读取 static fields 并 flatten 到顶层
3. 读取所有已完成 Stage 的问卷答案
4. 构造三类问卷结构：明细数组 / questionnaireAnswerMap / questionnaireAnswerByQuestionId
5. 注入 integrationToken
6. 注入 previousActionResult 和 `prev_` flatten 字段

### 2.5 多 Stage 问卷读取设计

建议在 `IComponentDataService` 中增加类似能力：

```csharp
Task<List<QuestionnaireStageAnswerSnapshot>> GetQuestionnaireDataForCompletedStagesAsync(long onboardingId);
```

由 `ActionContextBuilder` 自己先查已完成 Stage，再逐个调用 `GetQuestionnaireDataAsync(onboardingId, stageId)`。

**已确认方案**：先复用现有 `GetQuestionnaireDataAsync`，在 Builder 层聚合，避免大改原有 service 语义。

### 2.6 深路径模板解析器设计

新增 `TemplateVariableResolver`，统一给 HTTP Action 使用。

```csharp
public interface ITemplateVariableResolver
{
    string Replace(string input, object context);
    object? ResolvePath(object context, string path);
}
```

**支持路径**：
- `CaseCode`
- `previousActionResult.data.customerCode`
- `questionnaireAnswerByQuestionId.2001`
- `questionnaireAnswerMap.1001.2001`

**规则**：
- 找不到路径 → 返回空字符串
- 同时记录 warning 日志
- 解析基于 JToken，避免反射层层取值的复杂性

### 2.7 HttpApiActionExecutor 改造点

当前 `ReplacePlaceholders()` 只支持浅层路径，需要：

1. 注入 `ITemplateVariableResolver`
2. URL、headers、params、body 全部统一走 resolver
3. 保持现有行为兼容：旧模板 `{{CaseCode}}` 继续生效

### 2.8 前端改造点

#### VariablesPanel.vue
新增或强化问卷变量展示：
- `questionnaireAnswerByQuestionId.2001`
- `questionnaireAnswerMap.1001.2001`
- `questionnaireAnswers[i].questionId`
- `questionnaireAnswers[i].answer`

#### VariableAutoComplete.vue
同步候选列表，确保自动补全和面板一致。

#### HttpConfig.vue
示例文案从类似 `{{onboarding.id}}` 这类非真实字段，调整为：
- `{{CaseCode}}`
- `{{questionnaireAnswerByQuestionId.2001}}`
- `{{previousActionResult.data.customerCode}}`

---

## 3. 风险与取舍

### 风险 1：多 Stage 覆盖冲突
- 问题：同一 `questionId` 在不同 Stage 出现冲突值
- 方案：明确“后完成 Stage 覆盖先完成 Stage”的固定规则，并保留 `questionnaireAnswers` 明细供排查

### 风险 2：questionId 不是长期业务语义 key
- 问题：复制问卷模板后 questionId 可能变化
- 方案：v1 接受该约束，技术设计预留未来 `questionKey` 扩展点

### 风险 3：复杂题型值不适合直接发 CRM
- 问题：grid/附件等结构不一定能直接序列化为目标接口期望格式
- 方案：v1 只强承诺简单题型；复杂题型按 raw value 透出，不额外格式化

### 风险 4：日志泄露敏感问卷内容
- 问题：模板未命中和上下文调试时可能记录敏感值
- 方案：warning 日志只记录路径和命中状态，不完整打印全部问卷值
