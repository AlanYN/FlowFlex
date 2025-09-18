# GradientTag Component

A beautiful, versatile gradient tag component with rich customization options, designed as an enhanced alternative to `el-tag`.

## Features

- 🎨 **Gradient Backgrounds**: Beautiful gradient color schemes for all tag types
- 📏 **Multiple Sizes**: Large, default, and small size variants
- 🌙 **Dark Mode Support**: Automatic adaptation to light/dark themes
- 💫 **Interactive Elements**: Optional dots, pulse animations, and close buttons
- 🎯 **Hover Effects**: Subtle hover animations for better UX
- 🔧 **Highly Customizable**: Extensive props for different use cases
- 🎪 **Slot Support**: Custom content via default slot

## Usage

### Basic Usage

```vue
<template>
  <GradientTag text="Default Tag" />
  <GradientTag type="primary" text="Primary Tag" />
  <GradientTag type="success" text="Success Tag" />
  <GradientTag type="warning" text="Warning Tag" />
  <GradientTag type="danger" text="Danger Tag" />
  <GradientTag type="info" text="Info Tag" />
</template>

<script setup>
import GradientTag from '@/components/global/GradientTag/index.vue';
</script>
```

### With Custom Content (Slot)

```vue
<template>
  <GradientTag type="primary">
    <el-icon><Star /></el-icon>
    <span class="ml-1">Featured</span>
  </GradientTag>
</template>
```

### With Dot Indicator

```vue
<template>
  <GradientTag type="primary" text="Active" dot />
  <GradientTag type="warning" text="Processing" dot pulse />
</template>
```

### Different Sizes

```vue
<template>
  <GradientTag size="small" text="Small Tag" />
  <GradientTag size="default" text="Default Tag" />
  <GradientTag size="large" text="Large Tag" />
</template>
```

### Closable Tags

```vue
<template>
  <GradientTag
    v-for="tag in tags"
    :key="tag.id"
    :text="tag.name"
    closable
    @close="handleClose(tag.id)"
  />
</template>
```

### Clickable Tags

```vue
<template>
  <GradientTag
    text="Click me"
    type="primary"
    @click="handleClick"
  />
</template>
```

## Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `type` | `'primary' \| 'success' \| 'warning' \| 'danger' \| 'info' \| 'default'` | `'default'` | Tag color theme |
| `size` | `'large' \| 'default' \| 'small'` | `'default'` | Tag size |
| `text` | `string` | - | Tag text content (ignored if slot is used) |
| `closable` | `boolean` | `false` | Whether the tag can be closed |
| `dot` | `boolean` | `false` | Whether to show status dot |
| `pulse` | `boolean` | `false` | Whether the dot should pulse |
| `round` | `boolean` | `true` | Whether to use rounded-xl corners |

## Events

| Event | Parameters | Description |
|-------|------------|-------------|
| `close` | `(event: MouseEvent)` | Emitted when close button is clicked |
| `click` | `(event: MouseEvent)` | Emitted when tag is clicked |

## Slots

| Slot | Description |
|------|-------------|
| `default` | Custom tag content (overrides `text` prop) |

## Type Variants

### Visual Appearance

#### Light Mode
- **primary**: Blue to Indigo gradient
- **success**: Green to Emerald gradient  
- **warning**: Yellow to Amber gradient
- **danger**: Red to Rose gradient
- **info**: Gray to Slate gradient
- **default**: Light gray gradient

#### Dark Mode
- All variants automatically adapt with darker, high-contrast versions
- Maintains excellent readability and accessibility

## Size Variants

| Size | Padding | Text Size | Dot Size |
|------|---------|-----------|----------|
| `small` | `px-2 py-0.5` | `text-xs` | `w-1.5 h-1.5` |
| `default` | `px-3 py-1` | `text-sm` | `w-2 h-2` |
| `large` | `px-4 py-2` | `text-base` | `w-3 h-3` |

## Examples

### Tag List with Actions

```vue
<template>
  <div class="flex flex-wrap gap-2">
    <GradientTag
      v-for="tag in tags"
      :key="tag.id"
      :type="tag.type"
      :text="tag.name"
      closable
      @close="removeTag(tag.id)"
      @click="selectTag(tag.id)"
    />
  </div>
</template>

<script setup>
const tags = ref([
  { id: 1, name: 'Vue.js', type: 'primary' },
  { id: 2, name: 'TypeScript', type: 'info' },
  { id: 3, name: 'Urgent', type: 'danger' },
]);

const removeTag = (id) => {
  tags.value = tags.value.filter(tag => tag.id !== id);
};

const selectTag = (id) => {
  console.log('Selected tag:', id);
};
</script>
```

### Status Indicators

```vue
<template>
  <div class="space-y-2">
    <div class="flex items-center justify-between">
      <span>Server Status</span>
      <GradientTag type="success" text="Online" dot />
    </div>
    <div class="flex items-center justify-between">
      <span>Build Status</span>
      <GradientTag type="warning" text="Building" dot pulse />
    </div>
    <div class="flex items-center justify-between">
      <span>Deploy Status</span>
      <GradientTag type="danger" text="Failed" dot />
    </div>
  </div>
</template>
```

### Filter Tags

```vue
<template>
  <div class="mb-4">
    <span class="text-sm font-medium mr-3">Filters:</span>
    <GradientTag
      v-for="filter in activeFilters"
      :key="filter"
      :text="filter"
      type="primary"
      size="small"
      closable
      @close="removeFilter(filter)"
    />
  </div>
</template>
```

### Custom Content with Icons

```vue
<template>
  <GradientTag type="success" size="large">
    <el-icon class="mr-1"><Check /></el-icon>
    <span>Completed</span>
    <span class="ml-2 bg-white bg-opacity-20 px-1 rounded-xl text-xs">5</span>
  </GradientTag>
</template>
```

## Migration from el-tag

The GradientTag component is designed to be a drop-in replacement for `el-tag` with enhanced visual appeal:

```vue
<!-- Before (el-tag) -->
<el-tag type="primary" closable @close="handleClose">
  Primary Tag
</el-tag>

<!-- After (GradientTag) -->
<GradientTag type="primary" text="Primary Tag" closable @close="handleClose" />
```

## Accessibility

- Proper color contrast ratios in both light and dark modes
- Keyboard navigation support for closable tags
- Screen reader friendly with semantic HTML structure
- Focus indicators for interactive elements

## Performance

- Lightweight with minimal CSS and JavaScript
- Uses CSS gradients for smooth rendering
- Optimized hover and transition effects
- No external dependencies beyond Element Plus icons
