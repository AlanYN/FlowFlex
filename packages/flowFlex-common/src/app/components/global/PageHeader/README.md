# PageHeader 组件

统一的页面标题组件，为所有列表页面提供一致的视觉风格和交互体验。

## 特性

- 🎨 现代化的渐变背景设计
- ✨ 轻量级的动态背景效果
- 📱 完全响应式设计（基于 Tailwind CSS）
- 🌙 支持暗色主题
- 🎯 高度可定制的插槽系统
- 📏 统一高度设计（所有页面保持一致的视觉高度）
- ♿ 支持无障碍访问
- 🎭 支持高对比度和减少动画模式
- 🚀 使用 Tailwind CSS 优化，更轻量高效

## 基本用法

### 简单标题

```vue
<template>
  <!-- 仅标题 -->
  <PageHeader title="页面标题" />
  
  <!-- 带描述 -->
  <PageHeader
    title="页面标题"
    description="页面描述信息"
  />
</template>

<script setup>
import PageHeader from '@/components/global/PageHeader/index.vue';
</script>
```

### 带操作按钮

```vue
<template>
  <PageHeader
    title="Questionnaires"
    description="Create and manage questionnaires for different workflow stages"
  >
    <template #actions>
      <el-button
        type="primary"
        @click="handleCreate"
        class="page-header-btn page-header-btn-primary"
      >
        <el-icon class="mr-2"><Plus /></el-icon>
        New Questionnaire
      </el-button>
    </template>
  </PageHeader>
</template>
```

### 多个操作按钮

```vue
<template>
  <PageHeader
    title="Tools"
    description="Create and manage custom tools and actions for workflow automation"
  >
    <template #actions>
      <el-button 
        class="page-header-btn page-header-btn-secondary" 
        @click="handleExport"
      >
        <el-icon class="mr-2"><Download /></el-icon>
        Export
      </el-button>
      <el-button 
        class="page-header-btn page-header-btn-primary" 
        type="primary" 
        @click="handleCreate"
      >
        <el-icon class="mr-2"><Plus /></el-icon>
        New Tool
      </el-button>
    </template>
  </PageHeader>
</template>
```

### 自定义标题和描述

```vue
<template>
  <PageHeader>
    <template #title>
      <div class="flex items-center gap-2">
        <el-icon><DocumentAdd /></el-icon>
        <span>自定义标题</span>
      </div>
    </template>
    <template #description>
      <div class="flex items-center gap-2 text-sm">
        <el-tag size="small">Beta</el-tag>
        <span>这是一个自定义的描述区域</span>
      </div>
    </template>
    <template #actions>
      <!-- 操作按钮 -->
    </template>
  </PageHeader>
</template>
```

## API

### Props

| 参数 | 说明 | 类型 | 默认值 |
|------|------|------|--------|
| title | 页面标题 | `string` | `''` |
| description | 页面描述 | `string` | `''` |

### 插槽

| 插槽名 | 说明 | 参数 |
|--------|------|------|
| title | 自定义标题内容 | - |
| description | 自定义描述内容 | - |
| actions | 自定义操作区域 | - |

### 样式类

为了保持视觉一致性，组件提供了预定义的按钮样式类：

#### 基础按钮类
- `page-header-btn`: 基础按钮样式

#### 按钮类型类
- `page-header-btn-primary`: 主要按钮样式（渐变背景，白色文字）
- `page-header-btn-secondary`: 次要按钮样式（透明背景，主色文字）

#### 辅助类
- `mr-2`: 右边距（用于图标与文字的间距）

### 使用示例

```vue
<!-- 主要按钮 -->
<el-button class="page-header-btn page-header-btn-primary">
  <el-icon class="mr-2"><Plus /></el-icon>
  Primary Action
</el-button>

<!-- 次要按钮 -->
<el-button class="page-header-btn page-header-btn-secondary">
  <el-icon class="mr-2"><Download /></el-icon>
  Secondary Action
</el-button>
```

## 设计规范

### 视觉层次

1. **标题**: 使用渐变文字效果，突出页面主题
2. **描述**: 较小字号，提供补充信息
3. **操作区**: 右对齐，主要操作使用 primary 样式

### 响应式行为

- **桌面端**: 标题和操作区水平排列
- **移动端**: 垂直堆叠，操作区全宽显示

### 暗色主题适配

组件自动适配暗色主题，包括：
- 背景渐变调整
- 文字颜色适配
- 阴影效果优化

## 最佳实践

1. **保持标题简洁**: 标题应该清晰地表达页面功能
2. **描述信息有用**: 描述应该提供有价值的上下文信息
3. **操作按钮有序**: 将最重要的操作放在右侧，使用 primary 样式
4. **图标一致性**: 使用 Element Plus 图标库保持视觉一致
5. **加载状态**: 为异步操作添加 loading 状态

## 注意事项

- 组件使用了 CSS 变量，确保项目中已定义相关的主题变量
- 动画效果会在用户设置了 `prefers-reduced-motion: reduce` 时自动禁用
- 高对比度模式下会自动简化视觉效果
