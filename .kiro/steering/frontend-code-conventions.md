---
inclusion: fileMatch
fileMatchPattern: "packages/flowFlex-common/**"
---

# Frontend Code Conventions (Vue 3 + TypeScript)

## Tech Stack

| Category | Technology | Version |
|----------|-----------|---------|
| Framework | Vue 3 | ^3.5 |
| Language | TypeScript | 5.3 |
| Build Tool | Vite | ^5.4 |
| UI Library | Element Plus | ^2.9 |
| State Management | Pinia | ^2.2 |
| Router | Vue Router | ^4.4 |
| CSS Framework | TailwindCSS | ^3.4 |
| HTTP Client | Axios (via defHttp) | ^1.7 |
| i18n | vue-i18n | ^9.14 |
| Package Manager | pnpm | >=8.10 |
| Monorepo | Turborepo | ^1.13 |

---

## Project Structure

```
packages/flowFlex-common/src/
├── app/
│   ├── api/              ← Legacy API definitions (deprecated, use apis/)
│   ├── apis/             ← API request functions (grouped by module)
│   ├── components/       ← Shared/reusable components
│   ├── config/           ← App configuration
│   ├── enums/            ← Enum definitions
│   ├── hooks/            ← Composables (useXxx)
│   ├── logics/           ← Business logic helpers
│   ├── mitt/             ← Event bus
│   ├── router/           ← Route definitions
│   ├── settings/         ← App settings
│   ├── stores/           ← Pinia stores
│   ├── utils/            ← Utility functions
│   └── views/            ← Page components
├── assets/               ← Static assets (fonts, images, SVGs)
├── locales/              ← i18n language files
├── styles/               ← Global styles, design system, mixins
└── types/                ← Global TypeScript type declarations
```

---

## API Request Convention

### HTTP Client

All API requests use the `defHttp` instance (Axios wrapper).

```typescript
// Good ✅
import { defHttp } from '#/utils/http'

const Api = () => ({
  list: `/${globSetting.apiProName}/onboardings/${globSetting.apiVersion}/`,
  detail: (id: string) => `/${globSetting.apiProName}/onboardings/${globSetting.apiVersion}/${id}`,
})

export function getOnboardingList(params: OnboardingQueryParams) {
  return defHttp.get<PagedResult<OnboardingModel>>({ url: Api().list, params })
}

export function getOnboardingById(id: string) {
  return defHttp.get<OnboardingModel>({ url: Api().detail(id) })
}

export function createOnboarding(data: OnboardingInputDto) {
  return defHttp.post<string>({ url: Api().list }, { data })
}
```

### API Path Format

```
/${globSetting.apiProName}/${module}/${globSetting.apiVersion}/
```

### RESTful Method Mapping

| Method | Usage | Naming Pattern |
|--------|-------|---------------|
| GET | Query data | `get{Entity}List`, `get{Entity}ById` |
| POST | Create data | `create{Entity}`, `add{Entity}` |
| PUT | Update data | `update{Entity}`, `edit{Entity}` |
| DELETE | Delete data | `delete{Entity}` |

---

## Type Definition Convention

### Location

- Global types: `src/types/*.d.ts`
- Import via: `import type { XxxModel } from '#/types'`

### Naming Rules

| Type | Suffix | Example |
|------|--------|---------|
| Request params | `Params` | `OnboardingQueryParams` |
| Response model | `Model` | `OnboardingModel` |
| Input DTO | `InputDto` | `OnboardingInputDto` |
| Enum | `Enum` | `OnboardingStatusEnum` |

```typescript
// Good ✅
interface OnboardingQueryParams {
  pageIndex: number
  pageSize: number
  keyword?: string
  status?: number
}

interface OnboardingModel {
  id: string
  name: string
  status: number
  createTime: string
}

const enum OnboardingStatusEnum {
  Draft = 0,
  Active = 1,
  Completed = 2,
}
```

---

## Vue Component Convention

### Script Setup

All components use `<script setup lang="ts">` syntax.

```vue
<script setup lang="ts">
import { ref, computed } from 'vue'
import type { OnboardingModel } from '#/types'

interface Props {
  /** Onboarding record data */
  data: OnboardingModel
  /** Whether the component is in readonly mode */
  readonly?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  readonly: false,
})

const emit = defineEmits<{
  (e: 'update', data: OnboardingModel): void
  (e: 'delete', id: string): void
}>()
</script>

<template>
  <div class="onboarding-card">
    <!-- template content -->
  </div>
</template>

<style lang="scss" scoped>
.onboarding-card {
  // styles
}
</style>
```

### Component Naming

- File name: PascalCase (e.g., `OnboardingCard.vue`)
- Shared components: `src/app/components/`
- Page components: `src/app/views/`

---

## State Management Convention (Pinia)

```typescript
// Good ✅
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export const useOnboardingStore = defineStore('onboarding', () => {
  // State
  const list = ref<OnboardingModel[]>([])
  const loading = ref(false)

  // Getters
  const activeCount = computed(() =>
    list.value.filter((item) => item.status === 1).length
  )

  // Actions
  async function fetchList(params: OnboardingQueryParams) {
    loading.value = true
    try {
      const result = await getOnboardingList(params)
      list.value = result.items
    } finally {
      loading.value = false
    }
  }

  return { list, loading, activeCount, fetchList }
})
```

- Use `storeToRefs()` when destructuring reactive state
- Store files: `src/app/stores/`
- One store per business module

---

## Composable Convention (Hooks)

- File name: `use{Feature}.ts` (e.g., `useOnboardingFilter.ts`)
- Location: `src/app/hooks/`
- Always return reactive refs or computed values

```typescript
// Good ✅
export function useOnboardingFilter(list: Ref<OnboardingModel[]>) {
  const keyword = ref('')
  const filtered = computed(() =>
    list.value.filter((item) =>
      item.name.toLowerCase().includes(keyword.value.toLowerCase())
    )
  )
  return { keyword, filtered }
}
```

---

## Styling Convention

- Use TailwindCSS utility classes for layout and spacing
- Use SCSS with `scoped` for component-specific styles
- Design tokens defined in `src/styles/design-system/`
- Element Plus theme overrides in `src/styles/element-plus/`
- Global mixins in `src/styles/mixins/`

```vue
<!-- Good ✅ - TailwindCSS for layout, scoped SCSS for custom styles -->
<template>
  <div class="flex items-center gap-4 p-4">
    <span class="status-badge">{{ status }}</span>
  </div>
</template>

<style lang="scss" scoped>
.status-badge {
  @apply rounded-full px-3 py-1 text-sm font-medium;
}
</style>
```

---

## i18n Convention

- Use `useI18n()` composable in components
- Language files: `src/locales/lang/`
- All user-facing text must be internationalized

```vue
<script setup lang="ts">
import { useI18n } from 'vue-i18n'
const { t } = useI18n()
</script>

<template>
  <el-button>{{ t('common.save') }}</el-button>
</template>
```

---

## Error Handling Convention

- API errors handled globally by Axios interceptor
- Use `try-catch` for specific error handling in actions
- Use Element Plus `ElMessage` / `ElNotification` for user feedback

```typescript
// Good ✅
async function handleSubmit() {
  try {
    await createOnboarding(formData.value)
    ElMessage.success(t('common.createSuccess'))
  } catch (error) {
    // Global interceptor handles API errors
    // Only catch here for specific recovery logic
  }
}
```

---

## Pagination Convention

- `pageIndex` starts from 1
- Default `pageSize`: 20, max: 100
- Use `PagedResult<T>` type for paginated responses

```typescript
interface PagedResult<T> {
  items: T[]
  totalCount: number
  pageIndex: number
  pageSize: number
}
```
