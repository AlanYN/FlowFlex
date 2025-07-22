# Stages Progress Sync功能说明

## 概述

本功能添加了对onboarding stages progress的同步支持，特别是从Stage定义中同步`VisibleInPortal`和`AttachmentManagementNeeded`字段。

## 新增字段

### OnboardingStageProgress实体新增字段

```csharp
/// <summary>
/// Visible in Portal - Controls whether this stage is visible in the portal
/// </summary>
public bool VisibleInPortal { get; set; } = true;

/// <summary>
/// Attachment Management Needed - Indicates whether file upload is required for this stage
/// </summary>
public bool AttachmentManagementNeeded { get; set; } = false;
```

### OnboardingStageProgressDto新增字段

```csharp
/// <summary>
/// Visible in Portal - Controls whether this stage is visible in the portal
/// </summary>
public bool VisibleInPortal { get; set; } = true;

/// <summary>
/// Attachment Management Needed - Indicates whether file upload is required for this stage
/// </summary>
public bool AttachmentManagementNeeded { get; set; } = false;
```

## 新增API端点

### 同步Stages Progress

```http
POST /ow/onboardings/v1/{id}/sync-stages-progress
```

#### 功能描述
- 从workflow的stage定义中同步`VisibleInPortal`和`AttachmentManagementNeeded`字段到onboarding的stagesProgress
- 同时同步`ComponentsJson`和`EstimatedDays`字段
- 更新`LastUpdatedTime`和`LastUpdatedBy`追踪字段

#### 请求参数
- `id` (path): Onboarding ID

#### 响应示例
```json
{
  "success": true,
  "data": true,
  "msg": "Success"
}
```

#### 使用场景
1. **Stage配置更新后**: 当修改了workflow中stage的配置（如VisibleInPortal或AttachmentManagementNeeded）后，调用此API同步到现有的onboarding记录
2. **数据修复**: 修复可能不一致的stagesProgress数据
3. **批量更新**: 在系统升级或配置变更后批量同步所有onboarding记录

## 自动同步机制

### 新建Onboarding时
- 在`InitializeStagesProgressAsync`方法中，自动从Stage定义同步所有相关字段
- 包括`VisibleInPortal`、`AttachmentManagementNeeded`、`ComponentsJson`等

### 已有Onboarding
- 通过新的`SyncStagesProgressAsync`方法手动触发同步
- 迁移脚本`20250102000016_UpdateStagesProgressWithPortalFields`会在升级时自动同步现有数据

## 数据库迁移

### 迁移脚本: 20250102000016_UpdateStagesProgressWithPortalFields

#### 功能
- 扫描所有现有的onboarding记录
- 解析stages_progress_json字段
- 从对应的stage定义中同步新字段
- 更新stages_progress_json内容

#### 处理逻辑
1. 获取所有有效的onboarding记录
2. 对每个记录解析其stagesProgress JSON
3. 查找对应workflow的stage定义
4. 对比并更新需要同步的字段
5. 重新序列化并保存到数据库

## 前端集成

### 使用示例

```typescript
// 调用同步API
const syncStagesProgress = async (onboardingId: string) => {
  try {
    const response = await api.post(`/api/ow/onboardings/v1/${onboardingId}/sync-stages-progress`);
    if (response.data.success) {
      ElMessage.success('Stages progress synced successfully');
      // 重新加载onboarding详情
      await loadOnboardingDetail();
    }
  } catch (error) {
    ElMessage.error('Failed to sync stages progress');
    console.error(error);
  }
};
```

### Portal显示逻辑

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

## 注意事项

1. **数据一致性**: 同步操作会覆盖现有的字段值，请确保stage定义是正确的
2. **性能考虑**: 对于大量onboarding记录，建议分批处理同步操作
3. **权限控制**: 同步API需要适当的权限控制
4. **日志记录**: 同步操作会更新`LastUpdatedTime`和`LastUpdatedBy`字段用于审计

## 测试建议

1. **单元测试**: 测试`SyncStagesProgressAsync`方法的各种场景
2. **集成测试**: 测试API端点的完整流程
3. **迁移测试**: 在测试环境中验证迁移脚本的正确性
4. **前端测试**: 验证新字段在portal中的正确显示和功能 