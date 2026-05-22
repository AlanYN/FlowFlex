# File Structure: Action Questionnaire Context

## 后端新增文件

```text
packages/flowFlex-backend/
├── Application.Contracts/
│   ├── Dtos/Action/
│   │   └── ActionQuestionnaireContextDto.cs        ← 问卷上下文 DTO
│   └── IServices/Action/
│       └── IActionContextBuilder.cs                ← Action 上下文构建接口
├── Application/
│   └── Services/Action/
│       ├── ActionContextBuilder.cs                 ← 上下文聚合实现
│       └── TemplateVariableResolver.cs             ← 深路径模板解析器
└── Tests/FlowFlex.Tests/
    └── Services/Action/
        └── TemplateVariableResolverTests.cs        ← 模板解析测试
```

## 后端修改文件

```text
packages/flowFlex-backend/
├── Application/Services/OW/StageCondition/ActionExecutor.cs   ← TriggerAction 改用 Builder
├── Application/Services/Action/Executors/HttpApiActionExecutor.cs ← 接入深路径模板解析
├── Application/Services/OW/ComponentDataService.cs            ← 复用问卷数据读取逻辑
├── Application.Contracts/IServices/OW/IComponentDataService.cs ← 如有必要补辅助契约
└── Tests/FlowFlex.Tests/Services/OW/ActionExecutorTests.cs    ← 补问卷链路测试
```

## 前端修改文件

```text
packages/flowFlex-common/src/app/components/actionTools/
├── VariablesPanel.vue               ← 展示真实可用问卷变量
├── VariableAutoComplete.vue         ← 自动补全问卷变量
└── HttpConfig.vue                   ← 提示文案改为真实字段路径
```

## 各文件职责

| 文件 | 职责 |
|------|------|
| `ActionQuestionnaireContextDto.cs` | 定义问卷明细、map、按 questionId 索引的结构 |
| `IActionContextBuilder.cs` | 定义 StageCondition TriggerAction 的上下文构建接口 |
| `ActionContextBuilder.cs` | 聚合基础信息、static fields、多 Stage 问卷答案、token、previousActionResult |
| `TemplateVariableResolver.cs` | 统一解析 `{{a.b.c}}`、数字 key 路径等模板变量 |
| `ActionExecutor.cs` | 用 Builder 替代本地 contextData 拼装 |
| `HttpApiActionExecutor.cs` | 在 URL/Params/Headers/Body 中使用 Resolver |
| `VariablesPanel.vue` | 给用户展示推荐的问卷取值变量 |
| `VariableAutoComplete.vue` | 给配置输入框提供真实候选项 |
| `HttpConfig.vue` | 避免继续展示后端不支持的示例变量 |
