<template>
	<div class="http-config space-y-6">
		<!-- HTTP Configuration Form -->
		<div class="http-form">
			<el-form :model="formConfig" label-width="120px" class="space-y-6" label-position="top">
				<el-form-item label="Request URL" required class="request-url-input">
					<el-input
						:model-value="formConfig.url"
						@update:model-value="setUrl"
						placeholder="Enter URL, type '/' to insert variables"
						class="w-full"
					>
						<template #prepend>
							<el-select
								:model-value="formConfig.method"
								@update:model-value="setMethod"
								style="width: 115px"
							>
								<el-option label="GET" value="GET" />
								<el-option label="POST" value="POST" />
								<el-option label="PUT" value="PUT" />
								<el-option label="DELETE" value="DELETE" />
								<el-option label="PATCH" value="PATCH" />
							</el-select>
						</template>
					</el-input>
					<div class="text-xs text-gray-500 mt-1">
						Use variables like &#123;&#123;onboarding.id&#125;&#125; or
						&#123;&#123;stage.name&#125;&#125; in the URL
					</div>
				</el-form-item>

				<!-- Headers Section -->
				<el-form-item label="HEADERS">
					<div class="params-section">
						<div class="params-header">
							<div class="param-col">Key</div>
							<div class="param-col">Value</div>
							<div class="param-actions"></div>
						</div>
						<div class="params-body">
							<div
								v-for="(header, index) in formConfig.headersList"
								:key="index"
								class="param-row"
							>
								<variable-auto-complete
									v-model="header.key"
									placeholder="Type '/' to insert variables"
									class="param-input"
									@update:model-value="updateHeaderKey(index, $event)"
								/>
								<variable-auto-complete
									v-model="header.value"
									placeholder="Type '/' to insert variables"
									class="param-input"
									@update:model-value="updateHeaderValue(index, $event)"
								/>
								<el-button
									type="danger"
									text
									@click="removeHeader(index)"
									class="param-delete"
								>
									<el-icon><Delete /></el-icon>
								</el-button>
							</div>
						</div>
					</div>
				</el-form-item>

				<!-- Params Section -->
				<el-form-item label="PARAMS">
					<div class="params-section">
						<div class="params-header">
							<div class="param-col">Key</div>
							<div class="param-col">Value</div>
							<div class="param-actions"></div>
						</div>
						<div class="params-body">
							<div
								v-for="(param, index) in formConfig.paramsList"
								:key="index"
								class="param-row"
							>
								<variable-auto-complete
									v-model="param.key"
									placeholder="Type '/' to insert variables"
									class="param-input"
									@update:model-value="updateParamKey(index, $event)"
								/>
								<variable-auto-complete
									v-model="param.value"
									placeholder="Type '/' to insert variables"
									class="param-input"
									@update:model-value="updateParamValue(index, $event)"
								/>
								<el-button
									type="danger"
									text
									@click="removeParam(index)"
									class="param-delete"
								>
									<el-icon><Delete /></el-icon>
								</el-button>
							</div>
						</div>
					</div>
				</el-form-item>

				<!-- Body Section -->
				<el-form-item label="BODY">
					<div class="body-section">
						<!-- Body Type Selection -->
						<el-radio-group
							:model-value="formConfig.bodyType"
							@update:model-value="setBodyType"
							class="body-type-group"
						>
							<el-radio value="none">none</el-radio>
							<el-radio value="form-data">form-data</el-radio>
							<el-radio value="x-www-form-urlencoded">x-www-form-urlencoded</el-radio>
							<el-radio value="raw">raw</el-radio>
						</el-radio-group>

						<!-- Body Content based on type -->
						<div class="body-content">
							<!-- None - No content -->
							<div v-if="formConfig.bodyType === 'none'" class="body-none">
								<p class="text-gray-500 text-sm">This request has no body</p>
							</div>

							<!-- Form Data -->
							<div
								v-else-if="formConfig.bodyType === 'form-data'"
								class="params-section"
							>
								<div class="params-header">
									<div class="param-col">Key</div>
									<div class="param-col">Value</div>
									<div class="param-actions">Actions</div>
								</div>
								<div class="params-body">
									<div
										v-for="(item, index) in formConfig.formDataList"
										:key="index"
										class="param-row"
									>
										<variable-auto-complete
											v-model="item.key"
											placeholder="Type '/' to insert variables"
											class="param-input"
											@update:model-value="updateFormDataKey(index, $event)"
										/>
										<variable-auto-complete
											v-model="item.value"
											placeholder="Type '/' to insert variables"
											class="param-input"
											@update:model-value="updateFormDataValue(index, $event)"
										/>
										<el-button
											type="danger"
											text
											@click="removeFormData(index)"
											class="param-delete"
											v-if="formConfig.formDataList.length > 1"
										>
											<el-icon><Delete /></el-icon>
										</el-button>
									</div>
								</div>
							</div>

							<!-- URL Encoded -->
							<div
								v-else-if="formConfig.bodyType === 'x-www-form-urlencoded'"
								class="params-section"
							>
								<div class="params-header">
									<div class="param-col">Key</div>
									<div class="param-col">Value</div>
									<div class="param-actions">Actions</div>
								</div>
								<div class="params-body">
									<div
										v-for="(item, index) in formConfig.urlEncodedList"
										:key="index"
										class="param-row"
									>
										<variable-auto-complete
											v-model="item.key"
											placeholder="Type '/' to insert variables"
											class="param-input"
											@update:model-value="updateUrlEncodedKey(index, $event)"
										/>
										<variable-auto-complete
											v-model="item.value"
											placeholder="Type '/' to insert variables"
											class="param-input"
											@update:model-value="
												updateUrlEncodedValue(index, $event)
											"
										/>
										<el-button
											type="danger"
											text
											@click="removeUrlEncoded(index)"
											class="param-delete"
											v-if="formConfig.urlEncodedList.length > 1"
										>
											<el-icon><Delete /></el-icon>
										</el-button>
									</div>
								</div>
							</div>

							<!-- Raw - with format selection -->
							<div v-else-if="formConfig.bodyType === 'raw'" class="raw-section">
								<div class="raw-header">
									<el-select
										:model-value="formConfig.rawFormat"
										@update:model-value="setRawFormat"
										class="raw-format-select"
									>
										<el-option label="JSON" value="json" />
										<el-option label="Text" value="text" />
										<el-option label="JavaScript" value="javascript" />
										<el-option label="HTML" value="html" />
										<el-option label="XML" value="xml" />
									</el-select>
								</div>
								<variable-auto-complete
									:model-value="formConfig.rawBody"
									@update:model-value="setRawBody"
									type="textarea"
									:rows="8"
									placeholder="Enter your content here, type '/' to insert variables..."
									class="font-mono text-sm raw-textarea"
								/>
							</div>
						</div>
					</div>
				</el-form-item>
			</el-form>
		</div>

		<!-- Test Run Section -->
		<div class="test-section">
			<div class="flex items-center justify-between mb-3">
				<h5 class="font-medium text-gray-700 dark:text-gray-300">Test API Call</h5>
				<el-button
					type="success"
					size="small"
					@click="handleTest"
					:loading="testing"
					:disabled="!formConfig.url"
				>
					Test Request
				</el-button>
			</div>

			<div v-if="testResult" class="test-result">
				<div class="bg-gray-50 dark:bg-gray-800 rounded p-3">
					<h6 class="font-medium text-sm mb-2">Test Result:</h6>
					<pre class="text-xs text-gray-700 dark:text-gray-300 whitespace-pre-wrap">{{
						testResult.stdout || testResult
					}}</pre>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { Delete } from '@element-plus/icons-vue';
import VariableAutoComplete from './VariableAutoComplete.vue';

interface KeyValueItem {
	key: string;
	value: string;
}

interface Props {
	modelValue?: {
		url?: string;
		method?: string;
		headers?: Record<string, string>;
		params?: Record<string, string>;
		bodyType?: 'none' | 'form-data' | 'x-www-form-urlencoded' | 'raw';
		formData?: Record<string, string>;
		urlEncoded?: Record<string, string>;
		rawFormat?: string;
		rawBody?: string;
		body?: string;
		timeout?: number;
		followRedirects?: boolean;
		// Prefer lists to preserve order and duplicates in UI
		headersList?: KeyValueItem[];
		paramsList?: KeyValueItem[];
		formDataList?: KeyValueItem[];
		urlEncodedList?: KeyValueItem[];
	};
	testing?: boolean;
	testResult?: {
		executionTime: string;
		memoryUsage: number;
		message: string;
		status: string;
		stdout: string;
		success: boolean;
		timestamp: string;
		token: string;
	};
	idEditing?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	modelValue: () => ({
		url: '',
		method: 'GET',
		headers: {},
		params: {},
		bodyType: 'none',
		formData: {},
		urlEncoded: {},
		rawFormat: 'json',
		rawBody: '',
		body: '',
		timeout: 30,
		followRedirects: true,
		headersList: [],
		paramsList: [],
		formDataList: [],
		urlEncodedList: [],
	}),
	testing: false,
});

const emit = defineEmits<{
	'update:modelValue': [value: any];
	test: [];
}>();

// 主要的配置对象，在computed中处理所有数据格式转换
const formConfig = computed({
	get() {
		const data = props.modelValue || ({} as NonNullable<Props['modelValue']>);

		// headers：优先使用外部传入的列表以保留重复与顺序
		let headersList: KeyValueItem[] = Array.isArray(data.headersList)
			? [...(data.headersList as KeyValueItem[])]
			: Object.entries(data.headers || {}).map(([key, value]) => ({ key, value }));
		if (
			headersList.length === 0 ||
			String(headersList[headersList.length - 1]?.key || '').trim() ||
			String(headersList[headersList.length - 1]?.value || '').trim()
		) {
			headersList.push({ key: '', value: '' });
		}

		// params：同样优先使用列表
		let paramsList: KeyValueItem[] = Array.isArray(data.paramsList)
			? [...(data.paramsList as KeyValueItem[])]
			: Object.entries(data.params || {}).map(([key, value]) => ({ key, value }));
		if (
			paramsList.length === 0 ||
			String(paramsList[paramsList.length - 1]?.key || '').trim() ||
			String(paramsList[paramsList.length - 1]?.value || '').trim()
		) {
			paramsList.push({ key: '', value: '' });
		}

		// formData：优先列表
		let formDataList: KeyValueItem[] = Array.isArray(data.formDataList)
			? [...(data.formDataList as KeyValueItem[])]
			: Object.entries(data.formData || {}).map(([key, value]) => ({ key, value }));
		if (
			formDataList.length === 0 ||
			String(formDataList[formDataList.length - 1]?.key || '').trim() ||
			String(formDataList[formDataList.length - 1]?.value || '').trim()
		) {
			formDataList.push({ key: '', value: '' });
		}

		// urlEncoded：优先列表
		let urlEncodedList: KeyValueItem[] = Array.isArray(data.urlEncodedList)
			? [...(data.urlEncodedList as KeyValueItem[])]
			: Object.entries(data.urlEncoded || {}).map(([key, value]) => ({ key, value }));
		if (
			urlEncodedList.length === 0 ||
			String(urlEncodedList[urlEncodedList.length - 1]?.key || '').trim() ||
			String(urlEncodedList[urlEncodedList.length - 1]?.value || '').trim()
		) {
			urlEncodedList.push({ key: '', value: '' });
		}

		return {
			...data,
			url: data.url || '',
			method: data.method || 'GET',
			bodyType: data.bodyType || 'none',
			rawFormat: data.rawFormat || 'json',
			rawBody: data.rawBody || '',
			body: data.body || '',
			timeout: data.timeout || 30,
			followRedirects: data.followRedirects !== undefined ? data.followRedirects : true,
			// 转换后的数组格式，用于模板显示（保留到 modelValue）
			headersList,
			paramsList,
			formDataList,
			urlEncodedList,
		};
	},
	set(value) {
		// 将数组格式转换回对象格式（对象只用于请求执行；UI 仍依赖列表以保留重复与顺序）
		const headers: Record<string, string> = {};
		if (value.headersList) {
			value.headersList.forEach(({ key, value: val }: { key: string; value: string }) => {
				const k = String(key || '').trim();
				if (k) {
					headers[k] = val; // last-wins
				}
			});
		}

		const params: Record<string, string> = {};
		if (value.paramsList) {
			value.paramsList.forEach(({ key, value: val }: { key: string; value: string }) => {
				const k = String(key || '').trim();
				if (k) {
					params[k] = val; // last-wins
				}
			});
		}

		const formData: Record<string, string> = {};
		if (value.formDataList) {
			value.formDataList.forEach(({ key, value: val }: { key: string; value: string }) => {
				const k = String(key || '').trim();
				if (k) {
					formData[k] = val; // last-wins
				}
			});
		}

		const urlEncoded: Record<string, string> = {};
		if (value.urlEncodedList) {
			value.urlEncodedList.forEach(({ key, value: val }: { key: string; value: string }) => {
				const k = String(key || '').trim();
				if (k) {
					urlEncoded[k] = val; // last-wins
				}
			});
		}
		// 保留列表字段到上层 modelValue，以维持 UI 的重复 key 与顺序
		emit('update:modelValue', {
			...value,
			headers,
			params,
			formData,
			urlEncoded,
		});
	},
});

// Controlled setters to trigger computed.set via whole-object assignment
const setUrl = (val: string) => {
	formConfig.value = { ...formConfig.value, url: val } as any;
};

const setMethod = (val: string) => {
	formConfig.value = { ...formConfig.value, method: val } as any;
};

const setBodyType = (val: 'none' | 'form-data' | 'x-www-form-urlencoded' | 'raw') => {
	formConfig.value = { ...formConfig.value, bodyType: val } as any;
};

const setRawFormat = (val: string) => {
	formConfig.value = { ...formConfig.value, rawFormat: val } as any;
};

const setRawBody = (val: string) => {
	formConfig.value = { ...formConfig.value, rawBody: val } as any;
};

// 在每次更新后，确保末尾存在一行空白输入
const ensureTrailingEmptyRow = (
	listName: 'headersList' | 'paramsList' | 'formDataList' | 'urlEncodedList'
) => {
	const currentList = [...(formConfig.value as any)[listName]] as Array<{
		key: unknown;
		value: unknown;
	}>;
	const last = currentList[currentList.length - 1];
	const lastKeyFilled = String((last?.key as any) ?? '').trim();
	const lastValueFilled = String((last?.value as any) ?? '').trim();
	if (last && (lastKeyFilled || lastValueFilled)) {
		currentList.push({ key: '', value: '' });
		(formConfig.value as any) = { ...(formConfig.value as any), [listName]: currentList };
	}
};

// 更新函数 - 简化为直接触发computed的set
const updateHeaderKey = (index: number, newKey: string) => {
	const newHeadersList = [...formConfig.value.headersList];
	newHeadersList[index] = { ...newHeadersList[index], key: newKey };
	formConfig.value = { ...formConfig.value, headersList: newHeadersList };
	ensureTrailingEmptyRow('headersList');
};

const updateHeaderValue = (index: number, newValue: string) => {
	const newHeadersList = [...formConfig.value.headersList];
	newHeadersList[index] = { ...newHeadersList[index], value: newValue };
	formConfig.value = { ...formConfig.value, headersList: newHeadersList };
	ensureTrailingEmptyRow('headersList');
};

const updateParamKey = (index: number, newKey: string) => {
	const newParamsList = [...formConfig.value.paramsList];
	newParamsList[index] = { ...newParamsList[index], key: newKey };
	formConfig.value = { ...formConfig.value, paramsList: newParamsList };
	ensureTrailingEmptyRow('paramsList');
};

const updateParamValue = (index: number, newValue: string) => {
	const newParamsList = [...formConfig.value.paramsList];
	newParamsList[index] = { ...newParamsList[index], value: newValue };
	formConfig.value = { ...formConfig.value, paramsList: newParamsList };
	ensureTrailingEmptyRow('paramsList');
};

const updateFormDataKey = (index: number, newKey: string) => {
	const newFormDataList = [...formConfig.value.formDataList];
	newFormDataList[index] = { ...newFormDataList[index], key: newKey };
	formConfig.value = { ...formConfig.value, formDataList: newFormDataList };
	ensureTrailingEmptyRow('formDataList');
};

const updateFormDataValue = (index: number, newValue: string) => {
	const newFormDataList = [...formConfig.value.formDataList];
	newFormDataList[index] = { ...newFormDataList[index], value: newValue };
	formConfig.value = { ...formConfig.value, formDataList: newFormDataList };
	ensureTrailingEmptyRow('formDataList');
};

const updateUrlEncodedKey = (index: number, newKey: string) => {
	const newUrlEncodedList = [...formConfig.value.urlEncodedList];
	newUrlEncodedList[index] = { ...newUrlEncodedList[index], key: newKey };
	formConfig.value = { ...formConfig.value, urlEncodedList: newUrlEncodedList };
	ensureTrailingEmptyRow('urlEncodedList');
};

const updateUrlEncodedValue = (index: number, newValue: string) => {
	const newUrlEncodedList = [...formConfig.value.urlEncodedList];
	newUrlEncodedList[index] = { ...newUrlEncodedList[index], value: newValue };
	formConfig.value = { ...formConfig.value, urlEncodedList: newUrlEncodedList };
	ensureTrailingEmptyRow('urlEncodedList');
};

// 删除函数
const removeHeader = (index: number) => {
	const newHeadersList = [...formConfig.value.headersList];
	if (newHeadersList.length > 1) {
		newHeadersList.splice(index, 1);
		formConfig.value = { ...formConfig.value, headersList: newHeadersList };
	}
};

const removeParam = (index: number) => {
	const newParamsList = [...formConfig.value.paramsList];
	if (newParamsList.length > 1) {
		newParamsList.splice(index, 1);
		formConfig.value = { ...formConfig.value, paramsList: newParamsList };
	}
};

const removeFormData = (index: number) => {
	const newFormDataList = [...formConfig.value.formDataList];
	if (newFormDataList.length > 1) {
		newFormDataList.splice(index, 1);
		formConfig.value = { ...formConfig.value, formDataList: newFormDataList };
	}
};

const removeUrlEncoded = (index: number) => {
	const newUrlEncodedList = [...formConfig.value.urlEncodedList];
	if (newUrlEncodedList.length > 1) {
		newUrlEncodedList.splice(index, 1);
		formConfig.value = { ...formConfig.value, urlEncodedList: newUrlEncodedList };
	}
};

const handleTest = () => {
	emit('test');
};
</script>

<style scoped lang="scss">
.http-form {
	@apply dark:border-gray-700 rounded-lg;
}

// Params Section Styles
.params-section {
	@apply w-full border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden;
}

.params-header {
	@apply bg-gray-50 dark:bg-gray-800 px-4 py-3 border-b border-gray-200 dark:border-gray-700 flex items-center gap-4;

	.param-col {
		@apply flex-1 text-sm font-medium text-gray-600 dark:text-gray-400;
	}

	.param-actions {
		@apply w-12 text-sm font-medium text-gray-600 dark:text-gray-400;
	}
}

.params-body {
	@apply p-4 space-y-3;
}

.param-row {
	@apply flex items-center gap-4;
}

.param-input {
	@apply flex-1;
}

.param-delete {
	@apply w-8 h-8 flex items-center justify-center;
}

.add-param-btn {
	@apply mt-2;
}

// Body Section Styles
.body-section {
	@apply w-full space-y-4;
}

.body-type-group {
	@apply flex flex-wrap gap-4;

	:deep(.el-radio) {
		@apply mr-0;
	}
}

.body-content {
	@apply mt-4;
}

.body-none {
	@apply py-8 text-center;
}

.raw-section {
	@apply space-y-3;
}

.raw-header {
	@apply flex justify-end;
}

.raw-format-select {
	@apply w-32;
}

.raw-textarea {
	@apply font-mono;
}

// Options Section
.options-section {
	@apply border border-gray-200 dark:border-gray-700 rounded-lg p-4;
}

.test-section {
	@apply border-t border-gray-200 dark:border-gray-700 pt-6;
}

.test-result {
	@apply mt-3;

	pre {
		@apply max-h-40 overflow-y-auto;
	}
}

:deep(.el-input-number) {
	.el-input-group__append {
		@apply bg-gray-50 dark:bg-gray-700 text-gray-600 dark:text-gray-300;
	}
}

:deep(.request-url-input .el-input .el-input__wrapper) {
	border-top-left-radius: 0 !important;
	border-bottom-left-radius: 0 !important;
}
</style>
