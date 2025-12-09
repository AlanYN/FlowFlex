# Design Document

## Overview

This design document outlines the implementation of an integration attachment import dialog feature. The feature allows users to view and select attachments from connected integration systems through a modal dialog interface. The dialog displays attachment information in a table format with checkboxes for selection, following the application's existing design patterns and component architecture.

## Architecture

### Component Structure

The feature will be implemented as a new Vue 3 component using the Composition API with TypeScript. The component will follow the existing application architecture:

```
src/app/views/onboard/onboardingList/components/
├── Documents.vue (existing - will be modified)
└── ImportAttachmentsDialog.vue (new component)
```

### Data Flow

1. User clicks "Import from Integration" button in Documents.vue
2. Documents.vue calls `getCaseAttachmentIntegration` API
3. API returns `IntegrationAttachment[]` data
4. Documents.vue opens ImportAttachmentsDialog with attachment data
5. User selects attachments via checkboxes
6. Dialog emits selected attachments back to parent
7. Parent component handles the selection (future implementation)

### Technology Stack

- **Framework**: Vue 3 with Composition API
- **Language**: TypeScript
- **UI Library**: Element Plus
- **Styling**: Tailwind CSS + SCSS
- **State Management**: Vue Composition API (ref, computed)

## Components and Interfaces

### ImportAttachmentsDialog Component

**Props:**
```typescript
interface ImportAttachmentsDialogProps {
  visible: boolean;                      // Controls dialog visibility
  attachments: IntegrationAttachment[];  // List of attachments to display
  loading?: boolean;                     // Loading state for async operations
}
```

**Emits:**
```typescript
interface ImportAttachmentsDialogEmits {
  'update:visible': (value: boolean) => void;     // Two-way binding for visibility
  'close': () => void;                            // Dialog close event
  'select': (selected: IntegrationAttachment[]) => void; // Selection event (future use)
}
```

**Internal State:**
```typescript
const selectedAttachments = ref<Set<string>>(new Set()); // Track selected attachment IDs
const allSelected = computed(() => ...);                  // Computed for select-all state
```

### Documents.vue Modifications

**New State:**
```typescript
const importDialogVisible = ref(false);  // Control dialog visibility
```

**Modified Method:**
```typescript
const importFormIntegration = async () => {
  try {
    if (!props?.systemId) return;
    importLoading.value = true;
    const res = await getCaseAttachmentIntegration({
      systemId: props?.systemId,
    });
    if (res.code == '200') {
      importFileList.value = res?.data?.attachments || [];
      importDialogVisible.value = true; // Open dialog
    }
  } finally {
    importLoading.value = false;
  }
};
```

## Data Models

### IntegrationAttachment (Existing)

```typescript
export interface IntegrationAttachment {
  id: string;           // Unique identifier
  fileName: string;     // Display name of the file
  fileSize: string;     // File size in bytes (as string)
  fileType: string;     // MIME type
  fileExt: string;      // File extension
  createDate: string;   // ISO date string
  downloadLink: string; // URL for downloading
  source: string;       // Integration system name (CRM, BNP, WMS)
  module: string;       // Module name (Deals, Leads, Invoices)
}
```

### Selection State

```typescript
interface SelectionState {
  selectedIds: Set<string>;           // Set of selected attachment IDs
  isAllSelected: boolean;             // Whether all items are selected
  selectedCount: number;              // Number of selected items
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Table structure completeness
*For any* list of attachments displayed in the dialog, the table should contain columns for "File Name", "Source", and "Module"
**Validates: Requirements 2.1**

### Property 2: File name rendering format
*For any* attachment displayed, the rendered file name cell should contain both a document icon element and the file name text
**Validates: Requirements 2.2**

### Property 3: Source badge rendering
*For any* attachment displayed, the source should be rendered as a badge element (not plain text)
**Validates: Requirements 2.3**

### Property 4: Module plain text rendering
*For any* attachment displayed, the module should be rendered as plain text (not as a badge or special element)
**Validates: Requirements 2.4**

### Property 5: Alternating row styling
*For any* table with multiple rows, alternating rows should have different styling classes applied
**Validates: Requirements 2.5**

### Property 6: Checkbox presence
*For any* attachment row in the table, a checkbox element should exist in the leftmost column
**Validates: Requirements 3.1**

### Property 7: Checkbox toggle behavior
*For any* checkbox click event, the selection state of that attachment should toggle from selected to unselected or vice versa
**Validates: Requirements 3.2**

### Property 8: Row click toggle behavior
*For any* table row click event, the checkbox for that row should toggle its state
**Validates: Requirements 3.3**

### Property 9: Selection visual feedback
*For any* selected attachment, the row should have visual styling that distinguishes it from unselected rows
**Validates: Requirements 3.4**

### Property 10: Multiple selection support
*For any* sequence of checkbox selections, the system should maintain all selected states simultaneously (not replace previous selections)
**Validates: Requirements 3.5**

### Property 11: Backdrop overlay presence
*For any* state where the dialog is open, a backdrop overlay element should exist in the DOM
**Validates: Requirements 4.4**

### Property 12: Close event emission
*For any* dialog close action (clicking X button or backdrop), a close event should be emitted to the parent component
**Validates: Requirements 5.4**

### Property 13: Selection state persistence
*For any* sequence of selection and deselection actions, the component's internal state should accurately reflect the current set of selected attachment IDs
**Validates: Requirements 5.5**

## Error Handling

### API Error Handling

1. **Network Failures**: When `getCaseAttachmentIntegration` fails, display an error message using Element Plus `ElMessage.error()`
2. **Empty Response**: When API returns empty attachments array, display empty state in dialog
3. **Invalid Data**: Validate attachment data structure before rendering, skip invalid entries

### User Input Validation

1. **Selection Validation**: Ensure selected IDs exist in the attachments list
2. **State Consistency**: Prevent race conditions when rapidly toggling selections

### Error Recovery

1. **Retry Mechanism**: Allow users to retry failed API calls by closing and reopening dialog
2. **Graceful Degradation**: If attachment data is partially invalid, display valid entries and log errors

## Testing Strategy

### Unit Testing

Unit tests will verify specific examples and edge cases:

1. **Dialog Visibility**: Test that dialog opens when `visible` prop is true
2. **Empty State**: Test that empty state message displays when attachments array is empty
3. **Close Button**: Test that clicking close button emits close event
4. **Single Selection**: Test selecting and deselecting a single attachment
5. **Error Display**: Test that error messages display correctly

### Property-Based Testing

Property-based tests will verify universal properties across all inputs using **fast-check** (JavaScript/TypeScript property testing library):

1. **Table Rendering Properties**: Generate random attachment arrays and verify table structure
2. **Selection State Properties**: Generate random selection sequences and verify state consistency
3. **Event Emission Properties**: Generate random user interactions and verify correct events are emitted

Each property-based test will run a minimum of 100 iterations with randomly generated data.

### Integration Testing

1. **Parent-Child Communication**: Test that Documents.vue correctly passes data to ImportAttachmentsDialog
2. **API Integration**: Test the full flow from button click to dialog display with mocked API responses
3. **Theme Integration**: Verify dialog styling matches application theme (light/dark mode)

## Implementation Details

### Dialog Layout

```vue
<el-dialog
  v-model="dialogVisible"
  title="Import Attachments from Integration Systems"
  width="900px"
  :before-close="handleClose"
>
  <template #header>
    <div>
      <h2>Import Attachments from Integration Systems</h2>
      <p class="subtitle">Select attachments from your connected integration systems to import into</p>
    </div>
  </template>
  
  <div v-if="loading">
    <!-- Loading state -->
  </div>
  
  <div v-else-if="attachments.length === 0">
    <!-- Empty state -->
  </div>
  
  <el-table v-else :data="attachments" @row-click="handleRowClick">
    <el-table-column width="55">
      <template #default="{ row }">
        <el-checkbox v-model="selectedIds[row.id]" />
      </template>
    </el-table-column>
    
    <el-table-column label="File Name">
      <template #default="{ row }">
        <el-icon><Document /></el-icon>
        <span>{{ row.fileName }}</span>
      </template>
    </el-table-column>
    
    <el-table-column label="Source">
      <template #default="{ row }">
        <el-tag>{{ row.source }}</el-tag>
      </template>
    </el-table-column>
    
    <el-table-column label="Module">
      <template #default="{ row }">
        <span>{{ row.module }}</span>
      </template>
    </el-table-column>
  </el-table>
</el-dialog>
```

### Selection Management

```typescript
// Track selected attachment IDs
const selectedIds = ref<Record<string, boolean>>({});

// Toggle selection for a specific attachment
const toggleSelection = (attachmentId: string) => {
  selectedIds.value[attachmentId] = !selectedIds.value[attachmentId];
};

// Handle row click
const handleRowClick = (row: IntegrationAttachment) => {
  toggleSelection(row.id);
};

// Get array of selected attachments
const getSelectedAttachments = computed(() => {
  return props.attachments.filter(att => selectedIds.value[att.id]);
});
```

### Styling Approach

1. **Use Element Plus Components**: Leverage `el-dialog`, `el-table`, `el-checkbox`, `el-tag` for consistent UI
2. **Tailwind Utilities**: Use Tailwind classes for spacing, layout, and responsive design
3. **Dark Mode Support**: Use CSS variables and conditional classes for theme support
4. **Custom SCSS**: Minimal custom styles for specific requirements not covered by Element Plus

### File Structure

```
src/app/views/onboard/onboardingList/components/ImportAttachmentsDialog.vue
├── <template>
│   ├── el-dialog (main container)
│   ├── Loading state
│   ├── Empty state
│   └── el-table (attachment list)
├── <script setup lang="ts">
│   ├── Props definition
│   ├── Emits definition
│   ├── Selection state management
│   ├── Event handlers
│   └── Computed properties
└── <style scoped>
    └── Custom styling (minimal)
```

