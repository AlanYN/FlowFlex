# FlowFlex Permission Services 单元测试

## 🚀 快速开始

### 运行所有测试

```bash
cd D:\item\code\FlowFlex\packages\flowFlex-backend
dotnet test Tests/FlowFlex.Tests/FlowFlex.Tests.csproj
```

### 预期输出

```
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    46, Skipped:     0, Total:    46, Duration: 2s
```

---

## 📊 当前测试覆盖

| 测试类                             | 测试数量 | 状态   |
| ---------------------------------- | -------- | ------ |
| **PermissionHelpersTests**         | 18       | ✅     |
| **WorkflowPermissionServiceTests** | 15       | ✅     |
| **StagePermissionServiceTests**    | 13       | ✅     |
| **总计**                           | **46**   | **✅** |

---

## 📁 测试文件

```
Tests/FlowFlex.Tests/
├── README.md (本文件)
├── FlowFlex.Tests.csproj
├── TestBase/
│   ├── TestDataBuilder.cs         - 测试数据构建器
│   └── MockHelper.cs               - Mock 对象辅助类
└── Services/
    └── Permission/
        ├── PermissionHelpersTests.cs
        ├── WorkflowPermissionServiceTests.cs
        └── StagePermissionServiceTests.cs
```

---

## 🧪 测试示例

### PermissionHelpers 测试

```csharp
[Fact]
public void CheckTeamWhitelist_UserInWhitelist_ShouldReturnTrue()
{
    // Arrange
    var userContext = TestDataBuilder.CreateUserContext(userId);
    var helpers = new PermissionHelpers(...);
    var teamsJson = "[\"team-a\", \"team-b\"]";
    var userTeamIds = new List<string> { "team-a" };

    // Act
    var result = helpers.CheckTeamWhitelist(teamsJson, userTeamIds);

    // Assert
    result.Should().BeTrue();
}
```

### WorkflowPermissionService 测试

```csharp
[Fact]
public void CheckWorkflowPermission_PublicMode_ViewOperation_ShouldGrantView()
{
    // Arrange
    var workflow = TestDataBuilder.CreatePublicWorkflow();
    var service = CreateService(userContext);

    // Act
    var result = service.CheckWorkflowPermission(workflow, userId, OperationTypeEnum.View);

    // Assert
    result.Success.Should().BeTrue();
    result.CanView.Should().BeTrue();
}
```

### StagePermissionService 测试

```csharp
[Fact]
public void CheckStagePermission_InheritedFromPublicWorkflow_ViewOperation_ShouldGrantView()
{
    // Arrange
    var workflow = TestDataBuilder.CreatePublicWorkflow();
    var stage = TestDataBuilder.CreateStageWithInheritedPermissions(workflow.Id);
    var service = CreateService(userContext);

    // Act
    var result = service.CheckStagePermission(stage, workflow, userId, OperationTypeEnum.View);

    // Assert
    result.Success.Should().BeTrue();
    result.CanView.Should().BeTrue();
}
```

---

## 📚 详细文档

请参阅完整测试指南:
[PermissionService 单元测试指南.md](../../../Docs/PermissionService单元测试指南.md)

---

## 🛠️ 技术栈

- **xunit** - 测试框架
- **Moq** - Mock 框架
- **FluentAssertions** - 流畅断言库
- **.NET 8.0** - 运行时

---

## 📊 代码覆盖率

当前覆盖率: **~90%**

生成覆盖率报告:

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## ✅ 已测试功能

### PermissionHelpers

- ✅ 团队和用户白名单/黑名单检查
- ✅ 所有者检查
- ✅ Admin 绕过逻辑
- ✅ Portal Token 检测
- ✅ JSON 反序列化 (包括双重转义处理)
- ✅ 模块级权限检查

### WorkflowPermissionService

- ✅ Public 模式权限检查
- ✅ VisibleToTeams 模式权限检查
- ✅ InvisibleToTeams 模式权限检查
- ✅ Private 模式权限检查
- ✅ OperateTeams 白名单/黑名单
- ✅ System Admin 绕过
- ✅ 错误处理 (不存在的 Workflow, 无效输入)

### StagePermissionService

- ✅ Stage 权限继承 Workflow
- ✅ Stage 权限缩小 (严格模式)
- ✅ Assigned User 特殊权限
- ✅ 批量优化性能验证
- ✅ System Admin 绕过
- ✅ 错误处理 (不存在的 Stage/Workflow)

---

## 🔜 待扩展

- ⏳ CasePermissionService 测试 (预计 15+ 测试)
- ⏳ PermissionService 集成测试 (预计 10+ 测试)
- ⏳ 端到端权限检查流程测试

---

## 💡 贡献指南

添加新测试时请遵循:

1. **AAA 模式** (Arrange-Act-Assert)
2. **命名约定**: `MethodName_Scenario_ExpectedBehavior`
3. **使用 TestDataBuilder** 构建测试数据
4. **使用 MockHelper** 创建 Mock 对象
5. **使用 FluentAssertions** 编写断言

---

## 🎉 测试质量

**高质量单元测试** ✅

- 遵循最佳实践
- 易于维护和扩展
- 覆盖核心业务逻辑
- 验证边界条件和错误处理
