# OnboardingStagesProgress同步功能实现完成

## 修改总结

本次实现添加了对onboarding stages progress的`VisibleInPortal`和`AttachmentManagementNeeded`字段同步支持。

## 已完成的修改

### 1. 域模型修改

#### `OnboardingStageProgress.cs`
- 添加了`VisibleInPortal`字段 (bool, default: true)
- 添加了`AttachmentManagementNeeded`字段 (bool, default: false)

#### `OnboardingStageProgressDto.cs`
- 添加了`VisibleInPortal`字段 (bool, default: true)
- 添加了`AttachmentManagementNeeded`字段 (bool, default: false)

### 2. 服务层修改

#### `OnboardingService.cs`
- **修改了`InitializeStagesProgressAsync`方法**: 在初始化时从Stage实体同步`VisibleInPortal`和`AttachmentManagementNeeded`字段
- **新增了`SyncStagesProgressAsync`方法**: 用于同步现有onboarding记录的stagesProgress字段

#### `IOnboardingService.cs`
- 添加了`SyncStagesProgressAsync`方法的接口声明

### 3. 控制器修改

#### `OnboardingController.cs`
- **新增API端点**: `POST /ow/onboardings/v1/{id}/sync-stages-progress`
- 提供手动同步stagesProgress的功能

### 4. 映射配置修改

#### `OnboardingMapProfile.cs`
- 更新了AutoMapper配置，确保新字段正确映射
- 添加了`VisibleInPortal`和`AttachmentManagementNeeded`字段的映射规则

### 5. 数据库迁移

#### `20250102000016_UpdateStagesProgressWithPortalFields.cs`
- **新建迁移文件**: 处理现有数据的字段同步
- **Up方法**: 扫描所有onboarding记录，从对应的stage定义中同步新字段
- **Down方法**: 移除新添加的字段（回滚功能）

#### `MigrationManager.cs`
- 注册了新的迁移文件到迁移执行列表

### 6. 文档

#### `STAGES_PROGRESS_SYNC.md`
- 详细的功能说明文档
- API使用示例
- 前端集成建议
- 测试建议

## 新增API端点

### 同步StagesProgress
```http
POST /ow/onboardings/v1/{id}/sync-stages-progress
```

**功能**: 从workflow的stage定义中同步配置到onboarding的stagesProgress

**请求参数**: 
- `id` (path): Onboarding ID

**响应**: 
```json
{
  "success": true,
  "data": true,
  "msg": "Success"
}
```

## 自动同步机制

### 新建Onboarding
- 创建新onboarding时，自动从Stage定义同步所有相关字段
- 包括`VisibleInPortal`、`AttachmentManagementNeeded`、`ComponentsJson`等

### 现有Onboarding
- 通过新的API端点手动触发同步
- 迁移脚本在系统升级时自动同步现有数据

## 字段同步内容

每次同步会更新以下字段：
1. **VisibleInPortal**: 控制阶段在客户门户中的可见性
2. **AttachmentManagementNeeded**: 指示该阶段是否需要文件上传
3. **ComponentsJson**: 阶段组件配置
4. **EstimatedDays**: 预计完成天数
5. **LastUpdatedTime**: 最后更新时间
6. **LastUpdatedBy**: 最后更新人

## 前端集成建议

### 显示逻辑
```typescript
// 根据VisibleInPortal字段过滤显示的stages
const visibleStages = computed(() => {
  return onboardingData.value.stagesProgress.filter(stage => stage.visibleInPortal);
});

// 根据AttachmentManagementNeeded字段显示文件上传组件
const showFileUpload = computed(() => {
  const currentStage = onboardingData.value.stagesProgress.find(s => s.isCurrent);
  return currentStage?.attachmentManagementNeeded || false;
});
```

### 同步API调用
```typescript
const syncStagesProgress = async (onboardingId: string) => {
  try {
    const response = await api.post(`/api/ow/onboardings/v1/${onboardingId}/sync-stages-progress`);
    if (response.data.success) {
      ElMessage.success('Stages progress synced successfully');
      await loadOnboardingDetail(); // 重新加载数据
    }
  } catch (error) {
    ElMessage.error('Failed to sync stages progress');
    console.error(error);
  }
};
```

## 部署注意事项

1. **数据库迁移**: 确保迁移脚本在部署时正确执行
2. **API权限**: 确保同步API有适当的权限控制
3. **性能考虑**: 对于大量数据，建议分批处理
4. **监控**: 监控迁移过程和同步操作的执行情况

## 测试建议

1. **单元测试**: 测试`SyncStagesProgressAsync`方法的各种场景
2. **集成测试**: 测试API端点的完整流程
3. **迁移测试**: 在测试环境验证迁移脚本
4. **前端测试**: 验证新字段在UI中的正确显示

## 完成状态

✅ 所有后端代码修改已完成
✅ 数据库迁移文件已创建
✅ API端点已实现
✅ AutoMapper配置已更新
✅ 编译错误已修复
✅ 文档已创建

实现已经完成，可以进行测试和部署。 