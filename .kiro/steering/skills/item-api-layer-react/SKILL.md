---
name: Item API Layer
description: 'Item EAG API integration patterns. defHttp usage, axios configuration, request/response types, useGlobSetting for dynamic versioning. Actions: API integration, HTTP requests, axios setup, endpoint definition.'
inclusion: manual
role: Senior Frontend Architect
priority: high
---

# Item API Layer — defHttp & Axios Patterns

## 🔍 Step 0: Check Project First (MANDATORY)

```
Does src/app/apis/axios/ exist with a defHttp export?
        │
   YES  ├─→ Import defHttp directly — use patterns below
        │
   NO   └─→ Follow "Setting Up from Scratch" section below
```

---

## ✅ Scenario A: Project Already Has axios/defHttp

### Import

```ts
import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';
```

### Available Methods

```ts
defHttp.get<T>(config, options?)
defHttp.post<T>(config, options?)
defHttp.put<T>(config, options?)
defHttp.delete<T>(config, options?)
defHttp.patch<T>(config, options?)
defHttp.uploadFile<T>(config, params)  // multipart/form-data
```

### Standard API File Pattern

```ts
// src/app/apis/[feature]/index.ts
import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';
import type { Feature } from '#/feature';

const globSetting = useGlobSetting();

const Api = () => ({
	list: `${globSetting.apiProName}/api/${globSetting.apiVersion}/features`,
	detail: (id: string) =>
		`${globSetting.apiProName}/api/${globSetting.apiVersion}/features/${id}`,
	create: `${globSetting.apiProName}/api/${globSetting.apiVersion}/features`,
	update: (id: string) =>
		`${globSetting.apiProName}/api/${globSetting.apiVersion}/features/${id}`,
	delete: (id: string) =>
		`${globSetting.apiProName}/api/${globSetting.apiVersion}/features/${id}`,
});

export const featureApi = {
	list: (params: Feature.ListParams) =>
		defHttp.get<Feature.ListResponse>({ url: Api().list, params }),
	detail: (id: string) => defHttp.get<Feature.DetailResponse>({ url: Api().detail(id) }),
	create: (data: Feature.CreateParams) => defHttp.post<Feature.Item>({ url: Api().create, data }),
	update: (id: string, data: Feature.UpdateParams) =>
		defHttp.put<Feature.Item>({ url: Api().update(id), data }),
	delete: (id: string) => defHttp.delete({ url: Api().delete(id) }),
};
```

### `useGlobSetting` Key Fields

| Field        | Value     | Usage                       |
| :----------- | :-------- | :-------------------------- |
| `apiVersion` | `'v1'`    | Always use — never hardcode |
| `apiProName` | `'/api'`  | API path prefix             |
| `apiUrl`     | env-based | Base URL                    |
| `idmUrl`     | env-based | IDM service URL             |
| `uploadUrl`  | env-based | Upload endpoint             |

### Per-Request Options

```ts
defHttp.get(
	{ url: '...' },
	{
		isTransformResponse: false, // skip response transform, return raw data
		isReturnNativeResponse: true, // return full AxiosResponse
		errorMessageMode: 'message', // 'none' | 'modal' | 'message'
		withToken: false, // skip auth header injection
		ignoreCancelToken: true, // don't auto-cancel duplicate requests
	}
);
```

---

## 🏗️ Scenario B: Project Has No axios Layer (Setup from Scratch)

If `src/app/apis/axios/` does not exist, set it up with this structure:

### Directory structure

```
src/app/apis/
├── axios/
│   ├── index.ts          ← exports defHttp instance
│   ├── Axios.ts          ← VAxios class
│   ├── axiosTransform.ts ← transform abstract class
│   ├── axiosCancel.ts    ← cancel token manager
│   ├── checkStatus.ts    ← HTTP status error handler
│   ├── helper.ts         ← timestamp / date helpers
│   └── tokenRefresh.ts   ← 401 token refresh logic
└── [feature]/
    └── index.ts          ← feature API functions
```

### Install dependencies

```bash
pnpm add axios qs lodash-es
pnpm add -D @types/qs @types/lodash-es
```

### Minimal `axios/index.ts` to create `defHttp`

```ts
// src/app/apis/axios/index.ts
import axios from 'axios';
import type { AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios';

class VAxios {
	private instance: AxiosInstance;

	constructor(config: AxiosRequestConfig) {
		this.instance = axios.create(config);
		this.setupInterceptors();
	}

	private setupInterceptors() {
		this.instance.interceptors.request.use((config) => {
			const token = localStorage.getItem('token');
			if (token) config.headers.Authorization = `Bearer ${token}`;
			return config;
		});
		this.instance.interceptors.response.use(
			(res) => res,
			(err) => Promise.reject(err)
		);
	}

	get<T>(config: AxiosRequestConfig): Promise<T> {
		return this.instance
			.request({ ...config, method: 'GET' })
			.then((r: AxiosResponse) => r.data);
	}
	post<T>(config: AxiosRequestConfig): Promise<T> {
		return this.instance
			.request({ ...config, method: 'POST' })
			.then((r: AxiosResponse) => r.data);
	}
	put<T>(config: AxiosRequestConfig): Promise<T> {
		return this.instance
			.request({ ...config, method: 'PUT' })
			.then((r: AxiosResponse) => r.data);
	}
	delete<T>(config: AxiosRequestConfig): Promise<T> {
		return this.instance
			.request({ ...config, method: 'DELETE' })
			.then((r: AxiosResponse) => r.data);
	}
}

export const defHttp = new VAxios({
	baseURL: import.meta.env.VITE_GLOB_API_URL,
	timeout: 60000,
	headers: { 'Content-Type': 'application/json' },
});
```

### Minimal `settings/index.ts` for `useGlobSetting`

```ts
// src/app/settings/index.ts
export const useGlobSetting = () => ({
	apiVersion: 'v1',
	apiProName: import.meta.env.VITE_GLOB_API_PRO_NAME || '/api',
	apiUrl: import.meta.env.VITE_GLOB_API_URL || '',
	idmUrl: import.meta.env.VITE_GLOB_IDM_URL || '',
	uploadUrl: import.meta.env.VITE_GLOB_UPLOAD_URL || '',
});
```

### Required `.env` variables

```env
VITE_GLOB_API_URL=https://api.example.com
VITE_GLOB_API_PRO_NAME=/api
VITE_GLOB_IDM_URL=https://idm.example.com
VITE_GLOB_UPLOAD_URL=https://api.example.com/upload
```

---

## 📋 Standard API Patterns Reference

#[[file:.kiro/steering/item-api-layer/reference/api-patterns.csv]]

---

## 📋 Coding Rules Reference

#[[file:.kiro/steering/item-api-layer/reference/coding-rules.csv]]
