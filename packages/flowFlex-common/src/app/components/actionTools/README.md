# Action Tools Components

This directory contains reusable components for displaying and managing action execution results in the FlowFlex application.

## Components

### ActionTag.vue

A universal tag component that displays action names and provides click functionality to view execution results.

**Props:**
- `action`: ActionInfo object with `id` and `name` properties
- `triggerSourceId`: The ID of the source that triggers the action (question id, option id, task id, or stage id)
- `triggerSourceType`: Type of trigger source ('question', 'option', 'task', 'stage')
- `onboardingId`: Current case/onboarding ID
- `size`: Tag size ('large', 'default', 'small') - default: 'small'
- `type`: Tag type ('success', 'info', 'warning', 'danger') - default: 'success'
- `closable`: Whether the tag can be closed - default: false
- `disabled`: Whether the tag is disabled - default: false

**Events:**
- `close`: Emitted when the tag is closed

**Usage:**
```vue
<template>
  <ActionTag
    :action="{ id: 'action-123', name: 'Send Email' }"
    triggerSourceId="question-456"
    triggerSourceType="question"
    :onboardingId="currentOnboardingId"
    size="small"
    type="success"
  />
</template>

<script setup>
import ActionTag from '@/components/actionTools/ActionTag.vue';
</script>
```

### ActionResultDialog.vue

A comprehensive dialog component for displaying action execution results with filtering, pagination, and detailed views.

**Props:**
- `modelValue`: Dialog visibility state
- `triggerSourceId`: The ID of the source that triggers the action
- `triggerSourceType`: Type of trigger source
- `actionName`: Name of the action for display
- `onboardingId`: Current case/onboarding ID for filtering results

**Events:**
- `update:modelValue`: Emitted when dialog visibility changes

**Features:**
- Status filtering (success, failed, running, pending)
- Date range filtering
- Pagination support
- Expandable detail views
- Input/Output display with JSON formatting
- Error message and stack trace display
- Responsive design

**Usage:**
```vue
<template>
  <ActionResultDialog
    v-model="dialogVisible"
    triggerSourceId="task-789"
    triggerSourceType="task"
    actionName="Process Data"
    :onboardingId="currentOnboardingId"
  />
</template>

<script setup>
import ActionResultDialog from '@/components/actionTools/ActionResultDialog.vue';
import { ref } from 'vue';

const dialogVisible = ref(false);
</script>
```

## API Integration

The components use the `getActionResult` API from `@/apis/action/index.ts`:

```typescript
getActionResult(triggerSourceId: string, {
  pageIndex: number,
  pageSize: number,
  jsonConditions: [
    {
      jsonPath: 'onboardingId',
      operator: '=',
      value: currentOnboardingId
    }
  ]
})
```

## Integration Examples

### Question Level Action
```vue
<ActionTag
  :action="question.action"
  :triggerSourceId="question.id"
  triggerSourceType="question"
  :onboardingId="onboardingId"
/>
```

### Option Level Action
```vue
<ActionTag
  :action="option.action"
  :triggerSourceId="option.id"
  triggerSourceType="option"
  :onboardingId="onboardingId"
/>
```

### Task Level Action
```vue
<ActionTag
  :action="task.action"
  :triggerSourceId="task.id"
  triggerSourceType="task"
  :onboardingId="onboardingId"
/>
```

### Stage Level Action
```vue
<ActionTag
  :action="stage.action"
  :triggerSourceId="stage.id"
  triggerSourceType="stage"
  :onboardingId="onboardingId"
/>
```

## Styling

The components use Tailwind CSS classes and Element Plus components for consistent styling with the rest of the application. The ActionTag component includes hover effects for better user interaction feedback.

## Type Definitions

The components use TypeScript interfaces defined in `#/action` for type safety:

- `ActionExecutionResult`: Structure of action execution records
- `ActionResultFilters`: Filter options for results
- `ActionResultPagination`: Pagination configuration

## Date Formatting

All date displays use the `timeZoneConvert` utility from `@/hooks/time` to ensure consistent timezone handling across the application.
