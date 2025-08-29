# FlowFlex User Selector Component

A modern, feature-rich user and team selector component with avatar display and modal selection interface.

## Features

- 🎨 **Avatar Display**: Shows selected users with colorful avatars (random background + initials)
- 🗂️ **Modal Selection**: Left-right panel layout for available and selected users
- 🎯 **Type Filtering**: Select users only or teams only
- 🔒 **Smart Filtering**: Returns only the target type data while allowing flexible selection methods
- 🔢 **Quantity Limits**: Configure min/max selection counts
- 🔍 **Search**: Real-time filtering of users and teams
- 📏 **Consistent Height**: 32px height matching Element Plus components
- ♿ **Accessibility**: Full keyboard navigation and screen reader support
- 📱 **Responsive**: Works on all screen sizes

## Basic Usage

```vue
<template>
  <!-- Consistent height with Element Plus components -->
  <div class="flex items-center gap-4">
    <el-input v-model="input" placeholder="Element Plus Input" />
    <flowflex-user
      v-model="selectedUsers"
      :max-count="5"
      selection-type="user"
      placeholder="Select users"
      @change="handleUsersChange"
    />
  </div>
</template>

<script setup>
import FlowflexUser from '@/components/form/flowflexUser/index.vue';
import { ref } from 'vue';

const input = ref('');
const selectedUsers = ref([]);

const handleUsersChange = (value, selectedData) => {
  console.log('Selected:', value, selectedData);
};
</script>
```

## Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `modelValue` | `string \| string[]` | `undefined` | The selected user/team IDs |
| `placeholder` | `string` | `''` | Placeholder text when no selection |
| `disabled` | `boolean` | `false` | Disable the component |
| `selectionType` | `'user' \| 'team'` | `'user'` | What type can be selected |
| `maxCount` | `number` | `0` | Maximum items to select (0 = unlimited) |
| `minCount` | `number` | `0` | Minimum items required |
| `checkStrictly` | `boolean` | `false` | Whether to check nodes strictly |
| `showEmail` | `boolean` | `true` | Show user emails in tooltips and selection |
| `readonly` | `boolean` | `false` | Display only mode, no input box styling |

## Events

| Event | Parameters | Description |
|-------|------------|-------------|
| `update:modelValue` | `value: string \| string[]` | Emitted when selection changes |
| `change` | `value: string \| string[], selectedData: FlowflexUser \| FlowflexUser[]` | Emitted with full user data |
| `clear` | - | Emitted when selection is cleared |
| `modal-open` | - | Emitted when selection modal opens |
| `modal-close` | - | Emitted when selection modal closes |

## Exposed Methods

| Method | Parameters | Description |
|--------|------------|-------------|
| `openModal()` | - | Programmatically open the selection modal |
| `closeModal()` | - | Close the selection modal |
| `clearSelection()` | - | Clear all selections |
| `getSelectedData()` | - | Get the full data of selected items |
| `refreshData()` | `searchQuery?: string` | Refresh the user data |

## Selection Type Behavior

The component behavior differs based on the `selectionType` prop:

### User Selection Mode (`selectionType="user"`)
- **Left Panel**: Shows all items (users and teams) - users can select anything
- **Right Panel**: Only displays selected users (teams are filtered out)
- **ModelValue**: Only contains user IDs, even if teams were clicked
- **Use Case**: When you want users to be able to click on teams to select all contained users, but only user data is returned

### Team Selection Mode (`selectionType="team"`)
- **Left Panel**: Shows only teams and containers that have teams
- **Right Panel**: Only displays selected teams
- **ModelValue**: Only contains team IDs
- **Use Case**: When you only want team selection with no user access

```vue
<!-- User mode: can click teams to select users, but only users are returned -->
<flowflex-user
  v-model="userSelections"
  selection-type="user"
  :max-count="5"
  placeholder="Select users (can click teams)"
/>

<!-- Team mode: only teams shown and selectable -->
<flowflex-user
  v-model="teamSelections"
  selection-type="team"
  :max-count="3"
  placeholder="Select teams only"
/>
```

## Usage Examples

### Single User Selection

```vue
<flowflex-user
  v-model="singleUser"
  :max-count="1"
  selection-type="user"
  placeholder="Select one user"
/>
```

### Team Selection with Limits

```vue
<flowflex-user
  v-model="selectedTeams"
  selection-type="team"
  :max-count="3"
  :min-count="1"
  placeholder="Select 1-3 teams"
/>
```

### Unlimited User Selection (with team interaction)

```vue
<flowflex-user
  v-model="selectedUsers"
  selection-type="user"
  placeholder="Select users (can click teams to select contained users)"
/>
```

### Disabled State

```vue
<flowflex-user
  v-model="preSelectedUsers"
  :disabled="true"
  placeholder="Read-only selection"
/>
```

### Without Email Display

```vue
<flowflex-user
  v-model="selectedUsers"
  :show-email="false"
  placeholder="Select users (no emails shown)"
/>
```

## Avatar Colors

The component automatically generates consistent colors for user avatars based on their names. The same user will always have the same color across sessions.

Available colors:
- Red variations: `#FF6B6B`, `#F1948A`
- Blue variations: `#4ECDC4`, `#45B7D1`, `#85C1E9`
- Green variations: `#96CEB4`, `#82E0AA`
- Purple variations: `#DDA0DD`, `#BB8FCE`, `#D7BDE2`
- Yellow variations: `#FFEAA7`, `#F7DC6F`, `#F8C471`
- Teal: `#98D8C8`

## Modal Interface

The selection modal features a split-panel design:

- **Left Panel**: Available users/teams with search and tree navigation
- **Right Panel**: Currently selected items with remove functionality
- **Search**: Real-time filtering across all users and teams
- **Counters**: Show available and selected counts

## Data Structure

The component expects `FlowflexUser` objects with the following structure:

```typescript
interface FlowflexUser {
  id: string;
  name: string;
  type: 'user' | 'team';
  children: FlowflexUser[];
  userDetails?: {
    id: string;
    email: string;
    username: string;
    emailVerified: boolean;
    lastLoginDate: string;
  };
  memberCount?: number; // For teams
}
```

## Styling

The component uses SCSS and can be customized through CSS custom properties or by overriding the component styles.

Key style classes:
- `.flowflex-user-selector` - Main container
- `.selected-users-display` - Display area with avatars
- `.user-avatar` - Individual avatar circles
- `.user-selector-modal` - Modal dialog
- `.dual-panel-layout` - Left-right split layout

## Accessibility

- Full keyboard navigation support
- ARIA labels and descriptions
- Screen reader compatible
- High contrast mode support
- Focus management within modal

## Browser Support

- Chrome 70+
- Firefox 65+
- Safari 12+
- Edge 79+

## Dependencies

- Vue 3.x
- Element Plus
- @element-plus/icons-vue

## License

Internal component for FlowFlex application.
