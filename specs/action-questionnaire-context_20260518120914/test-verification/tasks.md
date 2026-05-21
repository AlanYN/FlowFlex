# Tasks: Action Questionnaire Context — 测试任务

## 任务概览

| 分组 | 任务数 | 预估工时 |
|------|------|------|
| 后端单元测试 | 3 | 0.5 天 |
| 链路回归测试 | 1 | 0.25 天 |
| 前端手动验证 | 1 | 0.25 天 |
| 兼容性与降级验证 | 1 | 0.25 天 |
| **合计** | **6** | **1.25 天** |

---

## 第一组：后端单元测试

### TASK-TV-001: ActionContextBuilder 单元测试

- **文件**：`packages/flowFlex-backend/Tests/FlowFlex.Tests/Services/Action/ActionContextBuilderTests.cs`（新增）
- **覆盖用例**：TC-BE-001 ~ TC-BE-010
- **内容**：
  - Mock onboarding / stage / componentData 相关依赖
  - 验证基础字段、static fields、integrationToken、previousActionResult 注入
  - 验证所有已完成 Stage 的问卷聚合
  - 验证同 `questionId` 的覆盖优先级
  - 验证无问卷时空结构返回
- **依赖**：TD-T01、TD-T02、TD-T04、TD-T06 完成

### TASK-TV-002: TemplateVariableResolver 单元测试

- **文件**：`packages/flowFlex-backend/Tests/FlowFlex.Tests/Services/Action/TemplateVariableResolverTests.cs`
- **覆盖用例**：TC-BE-020 ~ TC-BE-027
- **内容**：
  - 验证顶层变量、嵌套路径、数字 key 路径
  - 验证多模板混合替换
  - 验证未命中返回空字符串
  - 验证 warning 日志记录
  - 验证旧浅层模板兼容
- **依赖**：TD-T03 完成

### TASK-TV-003: HttpApiActionExecutor 模板解析测试

- **文件**：`packages/flowFlex-backend/Tests/FlowFlex.Tests/Services/Action/HttpApiActionExecutorTests.cs`（如已有则扩展，否则新增）
- **覆盖用例**：TC-BE-030 ~ TC-BE-035
- **内容**：
  - 验证 URL / Params / Headers / Body 四处模板替换
  - 验证问卷路径模板与旧模板共存
  - 验证未命中时不中断执行
- **依赖**：TD-T03、TD-T07 完成

---

## 第二组：链路回归测试

### TASK-TV-004: TriggerAction 执行链路测试

- **文件**：`packages/flowFlex-backend/Tests/FlowFlex.Tests/Services/OW/ActionExecutorTests.cs`
- **覆盖用例**：TC-BE-040 ~ TC-BE-044
- **内容**：
  - 验证 `ActionExecutor` 改为通过 `IActionContextBuilder` 构建上下文
  - 验证问卷上下文能透传到 `ActionExecutionService`
  - 验证 static field / previousActionResult 兼容不回归
  - 验证无问卷场景仍可正常执行
- **依赖**：TD-T04、TD-T05、TD-T07 完成

---

## 第三组：前端手动验证

### TASK-TV-005: Action 配置前端手动验证

- **覆盖用例**：TC-FE-001 ~ TC-FE-004
- **内容**：
  - 打开 Action 配置界面
  - 验证 VariablesPanel 问卷变量展示
  - 验证输入框自动补全候选
  - 验证 HttpConfig 示例文案更新
- **依赖**：TD-T08、TD-T09、TD-T10 完成

---

## 第四组：兼容性与降级验证

### TASK-TV-006: 兼容性 / 降级专项验证

- **覆盖用例**：TC-DG-001 ~ TC-DG-005
- **内容**：
  - 验证无 completed stage、空问卷、复杂值、未命中路径等场景
  - 验证旧 Action 配置不受本次变更影响
  - 将执行中发现的问题补充到问题清单
- **依赖**：所有开发任务完成
