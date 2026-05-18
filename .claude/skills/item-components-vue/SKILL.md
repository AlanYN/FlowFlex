---
name: Item Vue Components
description: 'Item EAG Vue UI component library. Element Plus usage, custom Vue SFC components, composable patterns. Actions: build component, create UI, implement table, add loading state, use Element Plus components.'
inclusion: manual
role: Senior Vue.js Architect
priority: high
---

# Item Vue Components — Element Plus & SFC Protocol

## 🔍 Step 0: Check Project First (MANDATORY)

```
Does src/app/components/ exist with Vue SFC components?
        │
   YES  ├─→ Check the inventory list below
        │         │
        │    Found ├─→ Import and use directly — STOP, do not recreate
        │          │
        │    Not found ─→ Follow "How to Create" section below
        │
   NO   └─→ Follow "Setting Up from Scratch" section below
```

---

## ✅ Scenario A: Project Already Has Component Structure

### Element Plus — Core Components

| Component        | Tag                  | Use Case                    |
| :--------------- | :------------------- | :-------------------------- |
| Button           | `<el-button>`        | Actions, CTAs               |
| Input            | `<el-input>`         | Text input                  |
| Select           | `<el-select>`        | Dropdown selection          |
| Table            | `<el-table>`         | Data tables                 |
| Dialog           | `<el-dialog>`        | Modal dialogs               |
| Drawer           | `<el-drawer>`        | Side panel                  |
| Form             | `<el-form>`          | Form container with validation |
| DatePicker       | `<el-date-picker>`   | Date/range selection        |
| Pagination       | `<el-pagination>`    | Page navigation             |
| Tag              | `<el-tag>`           | Status indicators           |
| Tabs             | `<el-tabs>`          | Tab navigation              |
| Card             | `<el-card>`          | Content container           |
| Dropdown         | `<el-dropdown>`      | Action menus                |
| Upload           | `<el-upload>`        | File upload                 |
| Switch           | `<el-switch>`        | Toggle                      |
| Checkbox         | `<el-checkbox>`      | Boolean input               |
| Radio            | `<el-radio-group>`   | Single selection            |
| Cascader         | `<el-cascader>`      | Hierarchical selection      |
| ColorPicker      | `<el-color-picker>`  | Color input                 |
| Breadcrumb       | `<el-breadcrumb>`    | Navigation breadcrumb       |
| Tooltip          | `<el-tooltip>`       | Hover hint                  |
| Popover          | `<el-popover>`       | Floating content            |
| Progress         | `<el-progress>`      | Progress bar                |
| Skeleton         | `<el-skeleton>`      | Loading placeholder         |
| Avatar           | `<el-avatar>`        | User avatar                 |
| Badge            | `<el-badge>`         | Notification badge          |
| Divider          | `<el-divider>`       | Visual separator            |

### Custom Business Components (src/app/components/)

| Component          | Path                                    | Use Case                        |
| :----------------- | :-------------------------------------- | :------------------------------ |
| AvatarUsername     | `@/components/avatarUsername`           | Avatar + name display           |
| CountTo            | `@/components/CountTo`                  | Animated number counter         |
| Collapsible        | `@/components/collapsible`              | Side panel collapse             |
| Drawer (Center)    | `@/components/drawer`                   | Center/right drawer variants    |
| EmptyList          | `@/components/emptyList`                | Empty state display             |
| PrototypeTabs      | `@/components/PrototypeTabs`            | Styled tab variant              |
| DraggableTable     | `@/components/draggableTable`           | Sortable table rows             |
| PeopleSelect       | `@/components/form/peopleSelect`        | User/people picker              |
| PeopleTags         | `@/components/form/peopleTags`          | People tag display              |
| InputNumber        | `@/components/form/InputNumber`         | Numeric input with controls     |
| InputDiscount      | `@/components/form/inputDiscount`       | Discount/percentage input       |
| PhoneInput         | `@/components/form/inputPhone`          | Phone with country code         |
| AuthCode           | `@/components/form/authCode`            | OTP / verification code         |
| FileUpload         | `@/components/form/uploadFileInput`     | File/image upload               |
| TimeLine           | `@/components/form/timeLine`            | Time-based event list           |
| PageHeader         | `@/components/global/PageHeader`        | Page title + actions bar        |
| UDetailInfo        | `@/components/global/u-detail-info`     | Key-value detail display        |
| UHeading           | `@/components/global/u-heading`         | Section heading                 |
| UInputTags         | `@/components/global/u-input-tags`      | Structured tag input            |
| UPagination        | `@/components/global/u-pagination`      | Custom pagination wrapper       |
| GradientTag        | `@/components/global/GradientTag`       | Gradient styled badge           |

---

## 🏗️ Scenario B: Creating a New Vue SFC Component

Every component must follow this pattern:

```vue
<!-- src/app/components/[name]/index.vue -->
<template>
  <div :class="cn('base-classes', 'dark:dark-classes', props.class)">
    <!-- content -->
  </div>
</template>

<script setup lang="ts">
// ALWAYS use <script setup lang="ts">
interface Props {
  title?: string
  class?: string
  // add project-specific props here
}

const props = withDefaults(defineProps<Props>(), {
  title: '',
})

const emit = defineEmits<{
  change: [value: string]
  close: []
}>()
</script>
```

Rules:
- Always `<script setup lang="ts">` — never Options API
- Use `defineProps<T>()` with TypeScript generics
- Use `defineEmits<T>()` with typed event signatures
- Accept and forward `class` prop for style customization
- Include `dark:` Tailwind variants
- No business logic inside shared UI wrappers
- Named exports from `index.ts` barrel file if needed

---

## 📐 Typical Feature Page (Vue SFC)

```vue
<!-- src/app/views/[feature]/index.vue -->
<template>
  <div class="flex flex-col gap-4 p-6 dark:bg-gray-900">
    <PageHeader title="Feature Title" />

    <div class="flex gap-2">
      <el-input
        v-model="searchKeyword"
        placeholder="Search..."
        class="max-w-xs"
        clearable
      />
      <el-button type="primary" @click="handleCreate">
        <Icon icon="lucide:plus" class="mr-2 h-4 w-4" />
        Add New
      </el-button>
    </div>

    <el-table
      v-loading="loading"
      :data="tableData"
      border
      stripe
    >
      <el-table-column prop="name" label="Name" />
      <el-table-column prop="status" label="Status">
        <template #default="{ row }">
          <el-tag :type="row.status === 'active' ? 'success' : 'info'">
            {{ row.status }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="Actions" width="120">
        <template #default="{ row }">
          <el-button link type="primary" @click="handleEdit(row)">Edit</el-button>
          <el-button link type="danger" @click="handleDelete(row.id)">Delete</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-pagination
      v-model:current-page="pagination.page"
      v-model:page-size="pagination.pageSize"
      :total="pagination.total"
      layout="total, sizes, prev, pager, next"
      @change="fetchData"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { Icon } from '@iconify/vue'
import { ElMessage } from 'element-plus'
import PageHeader from '@/components/global/PageHeader/index.vue'
import { featureApi } from '@/apis/feature'
import type { Feature } from '#/feature'

const loading = ref(false)
const searchKeyword = ref('')
const tableData = ref<Feature.Item[]>([])
const pagination = reactive({ page: 1, pageSize: 20, total: 0 })

async function fetchData() {
  loading.value = true
  try {
    const res = await featureApi.list({
      page: pagination.page,
      pageSize: pagination.pageSize,
      keyword: searchKeyword.value,
    })
    tableData.value = res.items
    pagination.total = res.total
  } finally {
    loading.value = false
  }
}

function handleCreate() { /* open dialog */ }
function handleEdit(row: Feature.Item) { /* open dialog with row */ }
async function handleDelete(id: string) {
  await featureApi.delete(id)
  ElMessage.success('Deleted successfully')
  fetchData()
}

onMounted(fetchData)
</script>
```

---

## 🎨 Tailwind Utilities Reference

#[[file:.kiro/steering/item-components/reference/tailwind-utilities.csv]]

---

## 🎭 Common Icons Reference

#[[file:.kiro/steering/item-components/reference/common-icons.csv]]

---

## 🧩 Vue Patterns Reference

#[[file:.kiro/steering/item-components/reference/vue-patterns.csv]]
