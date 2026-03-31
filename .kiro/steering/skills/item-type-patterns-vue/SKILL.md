---
name: Item Vue Type Patterns
description: 'Item EAG Vue TypeScript type patterns. Namespace exports, #/ alias imports, router types, axios request/response types, Pinia store types. Actions: type definition, TypeScript, interface, namespace, type import.'
inclusion: manual
role: Senior Vue.js Architect
priority: high
---

# Item Vue Type Patterns — TypeScript Reference

## 🔍 Step 0: Check Project First (MANDATORY)

```
Does src/types/ exist with .d.ts files?
        │
   YES  ├─→ Check existing types before defining new ones
        │         │
        │    Found ├─→ Import via #/ alias — do not redefine
        │          │
        │    Not found ─→ Create new namespace file following patterns below
        │
   NO   └─→ Follow "Setting Up from Scratch" section below
```

---

## ✅ Scenario A: Project Already Has Type Structure

### Import rule

```ts
// Always use #/ alias for types, never @/ or relative paths
import type { Feature } from '#/feature';
import type { UserInfo, GlobConfig } from '#/config';
import type { RequestOptions } from '#/axios';
import type { AppRouteRecordRaw } from '#/router';
```

### Existing type files in this project

| File                     | Namespace / Exports                                        | Use for                            |
| :----------------------- | :--------------------------------------------------------- | :--------------------------------- |
| `src/types/axios.d.ts`   | `RequestOptions`, `Result<T>`, `UploadFileParams`          | API request config, response types |
| `src/types/router.d.ts`  | `AppRouteRecordRaw`, `RouteMeta`, `Menu`                   | Route definitions                  |
| `src/types/config.d.ts`  | `UserInfo`, `TokenObj`, `GlobConfig`, `UserState`          | App config, user info, store state |
| `src/types/store.d.ts`   | Store-related types                                        | Pinia store typing                 |
| `src/types/golbal.d.ts`  | Global augmentations, `Recordable`, `Nullable`             | Window, env vars, utility types    |
| `src/types/setting.d.ts` | Settings types                                             | App settings config                |
| `src/types/action.d.ts`  | Action types                                               | User action definitions            |

### Adding a new feature type

Create `src/types/[feature].d.ts` and use the namespace pattern:

```ts
// src/types/feature.d.ts
export namespace Feature {
  export interface Item {
    id: string
    name: string
    status: 'active' | 'inactive'
    createdAt: string
  }

  export interface ListParams {
    page: number
    pageSize: number
    keyword?: string
    status?: Item['status']
  }

  export interface ListResponse {
    items: Item[]
    total: number
  }

  export type DetailResponse = Item
  export type CreateParams = Omit<Item, 'id' | 'createdAt'>
  export type UpdateParams = Partial<CreateParams> & { id: string }
}
```

Then import:

```ts
import type { Feature } from '#/feature'
const params: Feature.ListParams = { page: 1, pageSize: 20 }
```

### Pinia Store Typing Pattern

```ts
// src/types/config.d.ts — add state interface
export interface FeatureState {
  list: Feature.Item[]
  loading: boolean
  total: number
}

// src/app/stores/modules/feature.ts
import { defineStore } from 'pinia'
import { store } from '@/stores'
import type { FeatureState } from '#/config'
import type { Feature } from '#/feature'

export const useFeatureStore = defineStore({
  id: 'item-wfe-feature',
  state: (): FeatureState => ({
    list: [],
    loading: false,
    total: 0,
  }),
  getters: {
    getList(state): Feature.Item[] {
      return state.list
    },
  },
  actions: {
    setList(list: Feature.Item[]) {
      this.list = list
    },
  },
})

// Use outside setup()
export function useFeatureStoreWithOut() {
  return useFeatureStore(store)
}
```

---

## 🏗️ Scenario B: Project Has No Type Structure (Setup from Scratch)

### Directory structure

```
src/types/
├── axios.d.ts      ← RequestOptions, Result<T>, UploadFileParams
├── router.d.ts     ← AppRouteRecordRaw, RouteMeta
├── config.d.ts     ← UserInfo, TokenObj, GlobConfig, store states
├── golbal.d.ts     ← Window augmentations, Recordable, Nullable
└── [feature].d.ts  ← per-feature namespace types
```

### Configure `#/` alias in `tsconfig.json`

```json
{
  "compilerOptions": {
    "paths": {
      "#/*": ["./types/*"]
    }
  }
}
```

And in `vite.config.ts`:

```ts
resolve: {
  alias: {
    '#/': path.resolve(__dirname, 'types/'),
  }
}
```

### Minimal `axios.d.ts`

```ts
// types/axios.d.ts
export type ErrorMessageMode = 'none' | 'modal' | 'message' | undefined

export interface RequestOptions {
  isTransformResponse?: boolean
  isReturnNativeResponse?: boolean
  errorMessageMode?: ErrorMessageMode
  withToken?: boolean
  joinTime?: boolean
  ignoreCancelToken?: boolean
}

export interface Result<T = unknown> {
  code: number | string
  message: string
  data?: T
}

export interface UploadFileParams {
  data?: Record<string, unknown>
  name?: string
  file: File | Blob
  filename?: string
}
```

### Minimal `router.d.ts`

```ts
// types/router.d.ts
import type { RouteComponent } from 'vue-router'

export interface RouteMeta {
  title?: string
  icon?: string
  hidden?: boolean
  hideBreadcrumb?: boolean
  hideMenu?: boolean
  status: boolean
}

export interface AppRouteRecordRaw {
  name: string
  path: string
  meta: RouteMeta
  component?: RouteComponent | (() => Promise<RouteComponent>)
  children?: AppRouteRecordRaw[]
  redirect?: string
}
```

---

## 📋 Core Rules (Both Scenarios)

- **NO `any`** — use `unknown` if type is truly unknown; use `Recordable` (= `Record<string, any>`) for loose objects
- Types live in `src/types/` only — never co-locate with API or component files
- Always use `export namespace` — never bare interfaces at module level
- Always import with `import type` — never runtime import for type-only usage
- One namespace per file, named after the feature
- Pinia store state interfaces belong in `src/types/config.d.ts`

---

## 📋 Type Patterns Reference

#[[file:.kiro/steering/item-type-patterns/reference/type-patterns.csv]]

---

## Import Alias Reference

| Alias      | Resolves to   | Usage             |
| :--------- | :------------ | :---------------- |
| `#/`       | `types/`      | Type-only imports |
| `@/`       | `src/app/`    | Runtime imports   |
| `@assets/` | `src/assets/` | Static assets     |
