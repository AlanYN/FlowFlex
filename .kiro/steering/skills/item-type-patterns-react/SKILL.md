---
name: Item Type Patterns
description: 'Item EAG TypeScript type patterns. Namespace exports, #/ alias imports, router types, axios request/response types, user store types. Actions: type definition, TypeScript, interface, namespace, type import.'
inclusion: manual
role: Senior Frontend Architect
priority: high
---

# Item Type Patterns — TypeScript Reference

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
import type { User } from '#/user';
import type { RequestOptions } from '#/axios';
import type { AppRouteModule } from '#/router';
```

### Existing type files in this project

| File                    | Namespace / Exports                               | Use for                            |
| :---------------------- | :------------------------------------------------ | :--------------------------------- |
| `src/types/axios.d.ts`  | `RequestOptions`, `Result<T>`, `UploadFileParams` | API request config, response types |
| `src/types/router.d.ts` | `AppRouteModule`, `RouteMeta`, `Menu`             | Route definitions                  |
| `src/types/user.d.ts`   | `User.State`, `User.Actions`, `User.Store`        | User store access                  |
| `src/types/config.d.ts` | `UserInfo`, `TokenObj`, `GlobConfig`              | App config, user info              |
| `src/types/global.d.ts` | Global augmentations                              | Window, env vars                   |
| `src/types/layout.d.ts` | Layout types                                      | Sidebar, header config             |

### Adding a new feature type

Create `src/types/[feature].d.ts` and use the namespace pattern:

```ts
// src/types/feature.d.ts
export namespace Feature {
	export interface Item {
		id: string;
		name: string;
		status: 'active' | 'inactive';
		createdAt: string;
	}

	export interface ListParams {
		page: number;
		pageSize: number;
		keyword?: string;
		status?: Item['status'];
	}

	export interface ListResponse {
		items: Item[];
		total: number;
	}

	export type DetailResponse = Item;
	export type CreateParams = Omit<Item, 'id' | 'createdAt'>;
	export type UpdateParams = Partial<CreateParams> & { id: string };
}
```

Then import:

```ts
import type { Feature } from '#/feature';
const params: Feature.ListParams = { page: 1, pageSize: 20 };
```

---

## 🏗️ Scenario B: Project Has No Type Structure (Setup from Scratch)

If `src/types/` does not exist, create it with this structure:

### Directory structure

```
src/types/
├── axios.d.ts      ← RequestOptions, Result<T>, UploadFileParams
├── router.d.ts     ← AppRouteModule, RouteMeta
├── config.d.ts     ← UserInfo, TokenObj, GlobConfig
├── global.d.ts     ← Window augmentations, env vars
└── [feature].d.ts  ← per-feature namespace types
```

### Configure `#/` alias in `tsconfig.json`

```json
{
	"compilerOptions": {
		"paths": {
			"#/*": ["./src/types/*"]
		}
	}
}
```

And in `vite.config.ts`:

```ts
resolve: {
  alias: {
    '#/': path.resolve(__dirname, 'src/types/'),
  }
}
```

### Minimal `axios.d.ts` to bootstrap

```ts
// src/types/axios.d.ts
export type ErrorMessageMode = 'none' | 'modal' | 'message' | undefined;

export interface RequestOptions {
	isTransformResponse?: boolean;
	isReturnNativeResponse?: boolean;
	errorMessageMode?: ErrorMessageMode;
	withToken?: boolean;
	joinTime?: boolean;
	ignoreCancelToken?: boolean;
}

export interface Result<T = unknown> {
	code: number | string;
	message: string;
	data?: T;
}

export interface UploadFileParams {
	data?: Record<string, unknown>;
	name?: string;
	file: File | Blob;
	filename?: string;
}
```

### Minimal `router.d.ts` to bootstrap

```ts
// src/types/router.d.ts
import { ComponentType, LazyExoticComponent } from 'react';

export type Component<T = unknown> = ComponentType<T> | LazyExoticComponent<ComponentType<T>>;

export interface RouteMeta {
	title?: string;
	icon?: string;
	hidden?: boolean;
	status: boolean;
}

export interface AppRouteRecordRaw {
	name: string;
	path: string;
	meta: RouteMeta;
	component?: Component;
	children?: AppRouteRecordRaw[];
	redirect?: string;
}

export type AppRouteModule = AppRouteRecordRaw;
```

---

## 📋 Core Rules (Both Scenarios)

-   **NO `any`** — use `unknown` if type is truly unknown
-   Types live in `src/types/` only — never co-locate with API or component files
-   Always use `export namespace` — never bare interfaces at module level
-   Always import with `import type` — never runtime import for type-only usage
-   One namespace per file, named after the feature

---

## 📋 Type Patterns Reference

#[[file:.kiro/steering/item-type-patterns/reference/type-patterns.csv]]

---

## Import Alias Reference

| Alias      | Resolves to   | Usage             |
| :--------- | :------------ | :---------------- |
| `#/`       | `src/types/`  | Type-only imports |
| `@/`       | `src/app/`    | Runtime imports   |
| `@assets/` | `src/assets/` | Static assets     |
