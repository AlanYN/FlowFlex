# PrototypeTabs 组件

基于原型设计的Tab组件，提供与shadcn/ui tabs组件相似的外观和体验。

## 特性

- 🎨 **原型样式**: 完全符合原型设计的视觉效果
- 🌈 **主题支持**: 支持项目主题色系统和暗色模式
- ✨ **动画效果**: 丰富的切换动画和交互反馈
- 📱 **响应式**: 在移动端自动切换为横向滚动布局
- 🔧 **灵活配置**: 支持多种尺寸、类型和自定义样式
- ⚡ **高性能**: 基于Vue 3 Composition API构建
- 🎯 **类型安全**: 完整的TypeScript类型定义
- ♿ **无障碍**: 支持键盘导航和屏幕阅读器

## 基本用法

```vue
<template>
  <PrototypeTabs v-model="activeTab" :tabs="tabsConfig">
    <TabPane value="tab1">
      <div>Tab 1 Content</div>
    </TabPane>
    <TabPane value="tab2">
      <div>Tab 2 Content</div>
    </TabPane>
  </PrototypeTabs>
</template>

<script setup>
import { ref } from 'vue'
import { PrototypeTabs, TabPane } from '@/components/PrototypeTabs'

const activeTab = ref('tab1')

const tabsConfig = [
  {
    value: 'tab1',
    label: 'Tab 1',
  },
  {
    value: 'tab2',
    label: 'Tab 2',
  },
]
</script>
```

## 高级用法

### 带图标和徽章的Tab

```vue
<template>
  <PrototypeTabs v-model="activeTab" :tabs="tabsConfig">
    <TabPane value="questions">
      <div>Questions Content</div>
    </TabPane>
    <TabPane value="preview">
      <div>Preview Content</div>
    </TabPane>
  </PrototypeTabs>
</template>

<script setup>
import { Edit, View } from '@element-plus/icons-vue'

const tabsConfig = [
  {
    value: 'questions',
    label: 'Questions',
    icon: Edit,
    badge: 5,
    badgeType: 'primary'
  },
  {
    value: 'preview',
    label: 'Preview',
    icon: View,
    disabled: false
  },
]
</script>
```

### 不同尺寸和类型

```vue
<template>
  <!-- 小尺寸 -->
  <PrototypeTabs 
    v-model="activeTab" 
    :tabs="tabsConfig" 
    size="small"
  >
    <!-- Tab内容 -->
  </PrototypeTabs>

  <!-- 卡片类型 -->
  <PrototypeTabs 
    v-model="activeTab" 
    :tabs="tabsConfig" 
    type="card"
  >
    <!-- Tab内容 -->
  </PrototypeTabs>

  <!-- 边框卡片类型 -->
  <PrototypeTabs 
    v-model="activeTab" 
    :tabs="tabsConfig" 
    type="border-card"
  >
    <!-- Tab内容 -->
  </PrototypeTabs>
</template>
```

## API

### PrototypeTabs Props

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| modelValue | string | - | 当前激活的tab值 |
| tabs | TabItem[] | [] | Tab配置数组 |
| size | 'small' \| 'default' \| 'large' | 'default' | Tab尺寸 |
| type | 'default' \| 'card' \| 'border-card' | 'default' | Tab类型 |
| tabsListClass | string | '' | Tab列表自定义样式类 |
| contentClass | string | '' | 内容区域自定义样式类 |

### TabItem 接口

```typescript
interface TabItem {
  value: string;           // Tab的唯一标识
  label: string;           // Tab显示文本
  icon?: any;              // Tab图标组件
  disabled?: boolean;      // 是否禁用
  badge?: string | number; // 徽章内容
  badgeType?: 'primary' | 'success' | 'warning' | 'danger' | 'info'; // 徽章类型
}
```

### PrototypeTabs Events

| 事件名 | 参数 | 说明 |
|--------|------|------|
| update:modelValue | (value: string) | 当前激活tab改变时触发 |
| tab-click | (value: string) | 点击tab时触发 |
| tab-change | (value: string) | tab切换时触发 |

### TabPane Props

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| value | string | - | Tab面板的唯一标识 |
| label | string | '' | Tab标签文本（可选，主要通过tabs配置） |
| disabled | boolean | false | 是否禁用 |
| paneClass | string | '' | 面板自定义样式类 |

## 样式定制

组件使用CSS变量进行样式定制，支持项目的主题色系统：

```scss
// 主要使用的CSS变量
--primary-50    // 背景色
--primary-100   // 边框色
--primary-500   // 激活状态背景色
--primary-600   // 深色模式激活状态
--primary-700   // 悬停状态
```

### 自定义样式示例

```vue
<template>
  <PrototypeTabs 
    v-model="activeTab" 
    :tabs="tabsConfig"
    tabs-list-class="custom-tabs-list"
    content-class="custom-content"
  >
    <!-- Tab内容 -->
  </PrototypeTabs>
</template>

<style scoped>
.custom-tabs-list {
  @apply rounded-xl
  padding: 6px;
}

.custom-content {
  margin-top: 24px;
  padding: 16px;
  border: 1px solid var(--primary-100);
  @apply rounded-xl
}
</style>
```

## 响应式设计

组件在移动端（屏幕宽度 < 768px）会自动切换为横向滚动布局，确保在小屏幕设备上的良好体验。

## 主题适配

组件完全支持项目的主题色系统和暗色模式，会根据当前主题自动调整颜色。

### 支持的主题

- **蓝色主题** (`:root.blue`): 使用蓝色作为主色调
- **紫色主题** (`:root.pruple`): 使用紫色作为主色调  
- **暗色模式** (`:root.dark`): 深色背景主题

## 动画效果

组件内置了丰富的动画效果：

### Tab切换动画
- **移动指示器**: 使用GSAP实现的流畅滑动指示器
- **涟漪效果**: 点击时的水波纹动画
- **悬停反馈**: 鼠标悬停时的图标和文字微动画
- **激活状态**: 选中tab的视觉反馈效果

### 内容切换动画
- **GSAP驱动**: 使用GSAP库实现高性能动画
- **滑入效果**: 新内容从右侧滑入并带有缩放效果
- **流畅过渡**: 0.25秒的快速切换，避免用户等待
- **硬件加速**: 启用GPU加速确保动画流畅

### 响应式动画
- **减少动画**: 自动检测用户的动画偏好设置
- **高对比度**: 在高对比度模式下增强边框
- **打印优化**: 打印时移除所有动画效果

## 注意事项

1. 确保每个TabPane的value属性与tabs配置中的value一致
2. 在使用图标时，需要先导入对应的图标组件
3. 组件使用了CSS的`:has()`选择器来实现网格布局，在较老的浏览器中可能需要polyfill
4. 徽章功能依赖Element Plus的Badge组件
5. **GSAP依赖**: 组件使用GSAP库实现动画效果，确保项目中已安装GSAP

## 依赖要求

```bash
npm install gsap
```

或者如果项目中已经安装了GSAP，确保版本兼容：

```json
{
  "dependencies": {
    "gsap": "^3.12.0"
  }
}
```

## 迁移指南

从Element Plus的el-tabs迁移到PrototypeTabs：

```vue
<!-- 原来的写法 -->
<el-tabs v-model="activeTab">
  <el-tab-pane label="Tab 1" name="tab1">
    Content 1
  </el-tab-pane>
  <el-tab-pane label="Tab 2" name="tab2">
    Content 2
  </el-tab-pane>
</el-tabs>

<!-- 新的写法 -->
<PrototypeTabs v-model="activeTab" :tabs="tabsConfig">
  <TabPane value="tab1">
    Content 1
  </TabPane>
  <TabPane value="tab2">
    Content 2
  </TabPane>
</PrototypeTabs>

<script setup>
const tabsConfig = [
  { value: 'tab1', label: 'Tab 1' },
 