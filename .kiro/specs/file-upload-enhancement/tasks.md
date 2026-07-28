# Implementation Plan: File Upload Enhancement

## Overview

本实现计划将文件上传增强功能拆分为 5 个递增式任务，覆盖类型扩展、问卷文件预览/下载、Stage 编辑器配置 UI、Documents 组件动态展示以及端到端验证。每个任务独立可演示，逐步构建在前一任务基础之上。

## Tasks

- [x] 1. 扩展类型定义和后端 Model
  - [x] 1.1 修改前端 TypeScript 类型定义
    - 在 `packages/flowFlex-common/src/types/onboard.d.ts` 中为 `StageComponentData` 类型添加 `title?: string`、`description?: string`、`isRequired?: boolean` 可选字段
    - 在同文件中为 `SelectedItem` 接口添加 `title?: string`、`isRequired?: boolean` 可选字段
    - 确保新字段标记为可选（`?`），不影响现有代码编译
    - 验证：运行 `pnpm type:check` 确认 TypeScript 编译通过，无新增类型错误
    - _Requirements: 7.1, 7.2, 7.3, 7.4_

  - [x] 1.2 修改后端 StageComponent.cs Model
    - 在 `packages/flowFlex-backend/Domain.Shared/Models/StageComponent.cs` 中添加 `Title`（nullable string）、`Description`（nullable string）、`IsRequired`（bool，默认 false）属性
    - 确认项目已配置 System.Text.Json camelCase 序列化策略，新属性会自动序列化为 `title`、`description`、`isRequired`
    - 无需数据库 Migration，字段存储在现有 `components_json` JSONB 列中
    - 验证：运行 `dotnet build` 确认后端编译通过
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 6.2, 6.3_

- [x] 2. dynamicForm.vue 文件预览/下载按钮
  - [x] 2.1 导入预览组件和图标依赖
    - 在 `packages/flowFlex-common/src/app/views/onboard/onboardingList/components/dynamicForm.vue` 中导入 `vuePreviewFile` 组件
    - 导入 `View`、`Download` 图标（从 `@element-plus/icons-vue`）
    - 注册 `vuePreviewFile` 组件到 components 或确认 auto-import 生效
    - _Requirements: 1.2, 2.2_

  - [x] 2.2 添加预览/下载所需的响应式状态和方法
    - 添加响应式变量：`previewFileUrl`（ref<string>）、`previewFileType`（ref<string>）、`previewFileShow`（ref<boolean>）、`offloading`（ref<boolean>）
    - 实现 `handlePreviewFile(file)` 方法：调用 `previewOnboardingFile(onboardingId, file.fileId)` 获取 blob，根据文件扩展名确定 MIME 类型，创建 Blob URL 后打开预览组件
    - 实现 `handleDownloadFile(file)` 方法：创建临时 `<a>` 元素，设置 `href` 为 `file.fullAccessUrl || file.accessUrl`，设置 `download` 属性为 `file.name`，触发点击后清理
    - 实现 `closePreview()` 方法：关闭预览弹窗并释放 Blob URL
    - _Requirements: 1.2, 2.2_

  - [x] 2.3 在文件列表中添加预览/下载按钮模板
    - 在文件元数据循环（`v-for="file in formData[question.id]"`）中，为每个文件条目添加 View 和 Download 图标按钮
    - 按钮显示条件：`file.accessUrl || file.fullAccessUrl` 为 truthy 时显示
    - 按钮不受 `questionIsDisabled()` 限制（预览/下载为只读操作，任何问卷状态下都可用）
    - 没有 `accessUrl` 也没有 `fullAccessUrl` 的文件隐藏按钮
    - 添加 `vuePreviewFile` 组件到模板底部，绑定 `previewFileUrl`、`previewFileType`、`previewFileShow`
    - 验证：问卷中已上传的文件显示预览和下载按钮，点击预览可打开文件查看器，点击下载可触发浏览器下载
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 2.1, 2.2, 2.3, 2.4_

- [x] 3. Checkpoint - 确认预览/下载功能
  - 确保 TypeScript 编译通过，后端构建通过
  - 确认 dynamicForm 预览/下载在所有问卷状态下可用
  - 如有问题请询问用户

- [x] 4. StageComponentsSelector.vue 配置 UI
  - [x] 4.1 修改 Selected Items 面板中 files 组件的显示名称
    - 在 `packages/flowFlex-common/src/app/views/onboard/workflow/components/StageComponentsSelector.vue` 中修改 `updateItemsDisplay()` 或构建 `selectedItems` 的逻辑
    - files case 使用 `component.title || 'File Attachments'` 作为显示名称
    - 将 `component.title`、`component.description`、`component.isRequired` 传递到 SelectedItem 对象
    - _Requirements: 3.5, 4.5_

  - [x] 4.2 添加 files 类型的 Title/Description/Required 编辑区域
    - 在 Selected Items 右侧面板中，针对 `element.type === 'files'` 的项目，添加配置表单区域
    - 包含：Title 文本输入框（placeholder: "File Attachments"）、Description 文本输入框（placeholder: "Upload and manage files in this stage"）、Required 开关
    - 使用 Element Plus 的 `el-input` 和 `el-switch` 组件
    - 所有输入绑定到 element 的对应字段，变更时触发 `handleFileComponentConfigChange(element)`
    - _Requirements: 3.1, 3.2, 4.1, 4.2, 5.1_

  - [x] 4.3 实现配置变更处理方法
    - 实现 `handleFileComponentConfigChange(element)` 方法：将 element 的 title、description、isRequired 同步到底层 `StageComponentData`
    - 修改 `updateItemOrder()` 方法的 files case，确保 `title`、`description`、`isRequired` 字段被保留到新的 components 数组中
    - 验证：Stage 编辑器中可以为 File Management 组件配置自定义标题、描述和必填开关，保存后数据正确持久化到 `components_json`
    - _Requirements: 3.1, 3.2, 4.1, 4.2, 5.1, 5.4_

- [x] 5. Documents.vue 动态展示配置
  - [x] 5.1 修改 Documents 组件标题和描述展示
    - 在 `packages/flowFlex-common/src/app/views/onboard/onboardingList/components/Documents.vue` 中
    - 标题从硬编码 "Documents" 改为 `{{ component.title || 'Documents' }}`
    - 在标题下方添加 description 展示区域：`<p v-if="component.description">{{ component.description }}</p>`
    - Required 标识使用 `component.isRequired || documentIsRequired` 作为条件显示红色星号
    - _Requirements: 3.3, 3.4, 4.3, 4.4, 5.2, 5.3_

  - [x] 5.2 修改验证方法支持配置化 Required
    - 修改 `vailComponent()` 方法，将 required 判断条件更新为 `props?.documentIsRequired || props.component?.isRequired`
    - 当 required 为 true 且无文件上传时，显示警告提示
    - 验证：Case Detail 中 Documents 组件展示自定义标题和描述，Required 标识正确显示
    - _Requirements: 5.2, 5.3, 6.1_

- [~] 6. Checkpoint - 端到端验证和边界情况
  - 测试旧数据兼容性：无 title/description/isRequired 的 Stage 正常显示默认值（"Documents" 标题，无描述，非必填）
  - 测试 Portal 页面 Documents 组件也能展示配置（如果 Portal 复用同一 Documents 组件）
  - 确认 dynamicForm 预览/下载在 Draft、Submitted 等所有问卷状态下可用
  - 确认 StageComponentNameSyncService 不会覆盖自定义 Title（如存在该服务逻辑）
  - 确保所有修改的文件通过 TypeScript 类型检查和后端编译
  - 如有问题请询问用户
  - _Requirements: 6.1, 6.2, 6.3_

## Notes

- 本功能无需数据库 Migration，新字段通过现有 `components_json` JSONB 列存储
- 前端所有新增字段标记为可选（`?`），确保旧数据向后兼容
- 后端 `IsRequired` 使用 `bool` 类型（默认 false），缺失时自动为 false
- dynamicForm 的预览/下载逻辑复用 Documents.vue 已有的 `previewOnboardingFile` API 模式
- StageComponentsSelector 的配置 UI 仅在 files 类型组件被选中时展示
- PBT（属性基测试）不适用于本功能——主要是 UI 渲染和 CRUD 配置，使用示例测试和集成测试即可

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2"] },
    { "id": 1, "tasks": ["2.1", "4.1"] },
    { "id": 2, "tasks": ["2.2", "4.2"] },
    { "id": 3, "tasks": ["2.3", "4.3"] },
    { "id": 4, "tasks": ["5.1"] },
    { "id": 5, "tasks": ["5.2"] }
  ]
}
```
