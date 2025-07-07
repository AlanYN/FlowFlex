# FlowFlex 项目实体初始化修复总结

## 修复目标
统一使用 `EntityBaseCreateInfoExtension` 扩展方法来正确初始化实体的 ID 和时间戳，确保：
- 使用雪花算法生成唯一 ID
- 正确设置 CreateDate 和 ModifyDate
- 从 UserContext 获取用户信息
- 统一的时间戳格式（DateTimeOffset）

## 已修复的服务

### ✅ WorkflowService.cs
- 已更新 CreateAsync 方法使用 `InitCreateInfo()`
- 已更新 UpdateAsync 方法使用 `InitUpdateInfo()`

### ✅ StageService.cs
- 已更新 CreateAsync 方法使用 `InitCreateInfo()`
- 已更新 UpdateAsync 方法使用 `InitUpdateInfo()`

### ✅ OnboardingService.cs
- 已更新 CreateAsync 方法使用 `InitCreateInfo()`
- 已添加扩展方法引用

### ✅ ChecklistService.cs
- 已更新 CreateAsync 方法使用 `InitCreateInfo()`
- 已更新 UpdateAsync 方法使用 `InitUpdateInfo()`

### ✅ QuestionnaireService.cs
- 已更新 CreateAsync 方法使用 `InitCreateInfo()`
- 已更新 UpdateAsync 方法使用 `InitUpdateInfo()`

### ✅ UserService.cs
- 已更新用户创建和更新方法使用扩展方法
- 已添加扩展方法引用

### ✅ AttachmentService.cs
- 已修复 ID 生成使用雪花算法

## 需要修复的服务

### ⚠️ ChecklistTaskService.cs
- 需要更新多个方法中的手动时间戳设置

### ⚠️ InternalNoteService.cs
- 需要更新多个方法中的手动时间戳设置

### ⚠️ ChecklistTaskCompletionService.cs
- 需要更新相关方法

### ⚠️ QuestionnaireSectionService.cs
- 需要更新相关方法

### ⚠️ StageCompletionLogService.cs
- 需要更新相关方法

### ⚠️ OnboardingFileService.cs
- 需要更新相关方法

### ⚠️ StaticFieldValueService.cs
- 需要更新相关方法

### ⚠️ OperationChangeLogService.cs
- 需要更新相关方法

### ⚠️ QuestionnaireAnswerService.cs
- 需要更新相关方法

## 需要修复的仓储层

### ⚠️ Repository 层
- OnboardingRepository.cs
- ChecklistTaskRepository.cs
- QuestionnaireRepository.cs
- StaticFieldValueRepository.cs
- InternalNoteRepository.cs
- 等多个仓储类

## 修复模式

### 创建实体时：
```csharp
var entity = _mapper.Map<EntityType>(input);
entity.InitCreateInfo(_userContext);
await _repository.InsertAsync(entity);
```

### 更新实体时：
```csharp
var entity = await _repository.GetByIdAsync(id);
_mapper.Map(input, entity);
entity.InitUpdateInfo(_userContext);
await _repository.UpdateAsync(entity);
```

## 预期效果
修复完成后，所有实体应该具有：
- 正确的雪花算法生成的 ID
- 正确的时间戳（非默认值）
- 完整的基础实体信息
- 统一的数据格式

## 测试验证
可以通过调用 API 接口验证修复效果：
- ID 应该是长整型雪花算法生成的值
- CreateDate 和 ModifyDate 应该是实际的时间戳
- TenantId 应该正确设置
- CreateBy 和 ModifyBy 应该有正确的用户信息 