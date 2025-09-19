<template>
	<div class="space-y-6 import-dialog">
		<!-- Import Section -->
		<div class="import-section flex justify-between items-center mb-4">
			<h4 class="font-medium text-gray-700 dark:text-gray-300">HTTP Configuration</h4>
			<el-button type="primary" @click="showImportDialog" :disabled="disabled">
				Import
			</el-button>
		</div>

		<!-- Import Dialog -->
		<el-dialog
			v-model="importDialogVisible"
			title="Import HTTP Configuration"
			width="600px"
			:before-close="handleImportDialogClose"
			append-to-body
		>
			<PrototypeTabs
				v-model="activeImportTab"
				:tabs="importTabsConfig"
				type="default"
				size="default"
			>
				<!-- cURL Tab -->
				<TabPane value="curl">
					<div class="curl-import-content space-y-4">
						<div
							class="bg-blue-50 dark:bg-gray-800 p-4 rounded-xl border border-blue-200 dark:border-gray-700"
						>
							<div class="flex items-start space-x-3">
								<div class="flex-shrink-0">
									<Icon
										icon="heroicons:information-circle"
										class="w-5 h-5 text-blue-600 dark:text-blue-400 mt-0.5"
									/>
								</div>
								<div>
									<h4
										class="text-sm font-medium text-blue-900 dark:text-blue-100 mb-1"
									>
										How to use cURL Import
									</h4>
									<p class="text-sm text-blue-700 dark:text-blue-300">
										Paste your cURL command below. The system will automatically
										parse and populate all form fields including URL, method,
										headers, and body data.
									</p>
								</div>
							</div>
						</div>
						<variable-auto-complete
							v-model="curlInput"
							type="textarea"
							:rows="10"
							placeholder="Paste your cURL command here...&#10;&#10;Example:&#10;curl -X POST 'https://api.example.com/users' \&#10;  -H 'Content-Type: application/json' \&#10;  -H 'Authorization: Bearer your-token' \&#10;  -d '{&#10;    &quot;name&quot;: &quot;John Doe&quot;,&#10;    &quot;email&quot;: &quot;john@example.com&quot;&#10;  }'"
							class="font-mono text-sm"
						/>

						<!-- 错误信息显示区域 -->
						<div v-if="importError" class="import-error-message">
							<div class="flex items-start space-x-3">
								<div class="flex-shrink-0">
									<Icon
										icon="heroicons:exclamation-triangle"
										class="w-5 h-5 text-red-600 dark:text-red-400 mt-0.5"
									/>
								</div>
								<div class="flex-1">
									<h4
										class="text-sm font-medium text-red-900 dark:text-red-100 mb-1"
									>
										Import Failed
									</h4>
									<p class="text-sm text-red-700 dark:text-red-300">
										{{ importError }}
									</p>
								</div>
							</div>
						</div>
					</div>
				</TabPane>

				<!-- AI Tab -->
				<TabPane value="ai">
					<div class="ai-import-content">
						<div class="text-center py-12">
							<div
								class="mx-auto w-16 h-16 bg-gradient-to-r from-purple-500 to-pink-500 rounded-full flex items-center justify-center mb-6"
							>
								<el-icon class="text-2xl text-white"><MagicStick /></el-icon>
							</div>
							<h3 class="text-xl font-semibold text-gray-900 dark:text-gray-100 mb-3">
								AI Generate
							</h3>
							<p class="text-gray-600 dark:text-gray-400 mb-6 max-w-md mx-auto">
								Generate HTTP configurations automatically using AI. Describe your
								API requirements and let AI create the perfect configuration for
								you.
							</p>
							<div
								class="bg-gradient-to-r from-purple-50 to-pink-50 dark:from-gray-800 dark:to-gray-800 p-4 rounded-xl border border-purple-200 dark:border-gray-700"
							>
								<p class="text-sm text-purple-700 dark:text-purple-300 font-medium">
									🚀 Coming Soon
								</p>
								<p class="text-xs text-purple-600 dark:text-purple-400 mt-1">
									This feature is currently in development and will be available
									in the next update.
								</p>
							</div>
						</div>
					</div>
				</TabPane>
			</PrototypeTabs>

			<template #footer>
				<div class="text-right">
					<el-button @click="handleImportDialogClose">Cancel</el-button>
					<el-button
						v-if="activeImportTab === 'curl'"
						type="primary"
						@click="handleCurlImport"
						:disabled="!curlInput.trim() || importLoading"
						:loading="importLoading"
					>
						Import
					</el-button>
				</div>
			</template>
		</el-dialog>

		<!-- HTTP Configuration Form -->
		<div class="http-form">
			<el-form :model="formConfig" label-width="120px" class="space-y-6" label-position="top">
				<el-form-item label="Request URL" required class="request-url-input">
					<el-input
						:model-value="formConfig.url"
						@update:model-value="setUrl"
						placeholder="Enter URL, type '/' to insert variables"
						class="w-full"
						:disabled="disabled"
					>
						<template #prepend>
							<el-select
								:model-value="formConfig.method"
								@update:model-value="setMethod"
								style="width: 115px"
								:disabled="disabled"
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
					<div class="params-section-enhanced">
						<div class="params-header-enhanced">
							<div class="param-col-key">Key</div>
							<div class="param-col-value">Value</div>
							<div class="param-actions-enhanced"></div>
						</div>
						<div class="params-body-enhanced">
							<div
								v-for="(header, index) in formConfig.headersList"
								:key="index"
								class="param-row-enhanced"
							>
								<div class="param-key-container">
									<variable-auto-complete
										v-model="header.key"
										placeholder="Header key"
										class="param-input-enhanced"
										@update:model-value="updateHeaderKey(index, $event)"
										:disabled="disabled"
									/>
								</div>
								<div class="param-value-container">
									<variable-auto-complete
										v-if="!header.focused"
										v-model="header.value"
										placeholder="Header value"
										class="param-input-enhanced"
										@update:model-value="updateHeaderValue(index, $event)"
										@focus="setHeaderFocused(index, true)"
										:disabled="disabled"
									/>
									<el-input
										v-else
										v-model="header.value"
										type="textarea"
										placeholder="Header value"
										class="param-textarea-auto-height"
										@update:model-value="updateHeaderValue(index, $event)"
										@blur="setHeaderFocused(index, false)"
										:disabled="disabled"
										:autosize="{ minRows: 1, maxRows: 10 }"
										:ref="
											(el) => {
												if (el) headerTextareaRefs[index] = el;
											}
										"
									/>
								</div>
								<div class="param-delete-container">
									<el-button
										type="danger"
										text
										@click="removeHeader(index)"
										class="param-delete-enhanced"
										:disabled="disabled"
									>
										<el-icon><Delete /></el-icon>
									</el-button>
								</div>
							</div>
						</div>
					</div>
				</el-form-item>

				<!-- Params Section -->
				<el-form-item label="PARAMS">
					<div class="params-section-enhanced">
						<div class="params-header-enhanced">
							<div class="param-col-key">Key</div>
							<div class="param-col-value">Value</div>
							<div class="param-actions-enhanced"></div>
						</div>
						<div class="params-body-enhanced">
							<div
								v-for="(param, index) in formConfig.paramsList"
								:key="index"
								class="param-row-enhanced"
							>
								<div class="param-key-container">
									<variable-auto-complete
										v-model="param.key"
										placeholder="Parameter key"
										class="param-input-enhanced"
										@update:model-value="updateParamKey(index, $event)"
										:disabled="disabled"
									/>
								</div>
								<div class="param-value-container">
									<variable-auto-complete
										v-if="!param.focused"
										v-model="param.value"
										placeholder="Parameter value"
										class="param-input-enhanced"
										@update:model-value="updateParamValue(index, $event)"
										@focus="setParamFocused(index, true)"
										:disabled="disabled"
									/>
									<el-input
										v-else
										v-model="param.value"
										type="textarea"
										placeholder="Parameter value"
										class="param-textarea-auto-height"
										@update:model-value="updateParamValue(index, $event)"
										@blur="setParamFocused(index, false)"
										:disabled="disabled"
										:autosize="{ minRows: 1, maxRows: 10 }"
										:ref="
											(el) => {
												if (el) paramTextareaRefs[index] = el;
											}
										"
									/>
								</div>
								<div class="param-delete-container">
									<el-button
										type="danger"
										text
										@click="removeParam(index)"
										class="param-delete-enhanced"
										:disabled="disabled"
									>
										<el-icon><Delete /></el-icon>
									</el-button>
								</div>
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
							:disabled="disabled"
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
								class="params-section-enhanced"
							>
								<div class="params-header-enhanced">
									<div class="param-col-key">Key</div>
									<div class="param-col-value">Value</div>
									<div class="param-actions-enhanced"></div>
								</div>
								<div class="params-body-enhanced">
									<div
										v-for="(item, index) in formConfig.formDataList"
										:key="index"
										class="param-row-enhanced"
									>
										<div class="param-key-container">
											<variable-auto-complete
												v-model="item.key"
												placeholder="Form data key"
												class="param-input-enhanced"
												@update:model-value="
													updateFormDataKey(index, $event)
												"
												:disabled="disabled"
											/>
										</div>
										<div class="param-value-container">
											<variable-auto-complete
												v-if="!item.focused"
												v-model="item.value"
												placeholder="Form data value"
												class="param-input-enhanced"
												@update:model-value="
													updateFormDataValue(index, $event)
												"
												@focus="setFormDataFocused(index, true)"
												:disabled="disabled"
											/>
											<el-input
												v-else
												v-model="item.value"
												type="textarea"
												placeholder="Form data value"
												class="param-textarea-auto-height"
												@update:model-value="
													updateFormDataValue(index, $event)
												"
												@blur="setFormDataFocused(index, false)"
												:disabled="disabled"
												:autosize="{ minRows: 1, maxRows: 10 }"
												:ref="
													(el) => {
														if (el) formDataTextareaRefs[index] = el;
													}
												"
											/>
										</div>
										<div class="param-delete-container">
											<el-button
												type="danger"
												text
												@click="removeFormData(index)"
												class="param-delete-enhanced"
												v-if="formConfig.formDataList.length > 1"
												:disabled="disabled"
											>
												<el-icon><Delete /></el-icon>
											</el-button>
										</div>
									</div>
								</div>
							</div>

							<!-- URL Encoded -->
							<div
								v-else-if="formConfig.bodyType === 'x-www-form-urlencoded'"
								class="params-section-enhanced"
							>
								<div class="params-header-enhanced">
									<div class="param-col-key">Key</div>
									<div class="param-col-value">Value</div>
									<div class="param-actions-enhanced"></div>
								</div>
								<div class="params-body-enhanced">
									<div
										v-for="(item, index) in formConfig.urlEncodedList"
										:key="index"
										class="param-row-enhanced"
									>
										<div class="param-key-container">
											<variable-auto-complete
												v-model="item.key"
												placeholder="URL encoded key"
												class="param-input-enhanced"
												@update:model-value="
													updateUrlEncodedKey(index, $event)
												"
												:disabled="disabled"
											/>
										</div>
										<div class="param-value-container">
											<variable-auto-complete
												v-if="!item.focused"
												v-model="item.value"
												placeholder="URL encoded value"
												class="param-input-enhanced"
												@update:model-value="
													updateUrlEncodedValue(index, $event)
												"
												@focus="setUrlEncodedFocused(index, true)"
												:disabled="disabled"
											/>
											<el-input
												v-else
												v-model="item.value"
												type="textarea"
												placeholder="URL encoded value"
												class="param-textarea-auto-height"
												@update:model-value="
													updateUrlEncodedValue(index, $event)
												"
												@blur="setUrlEncodedFocused(index, false)"
												:disabled="disabled"
												:autosize="{ minRows: 1, maxRows: 10 }"
												:ref="
													(el) => {
														if (el) urlEncodedTextareaRefs[index] = el;
													}
												"
											/>
										</div>
										<div class="param-delete-container">
											<el-button
												type="danger"
												text
												@click="removeUrlEncoded(index)"
												class="param-delete-enhanced"
												v-if="formConfig.urlEncodedList.length > 1"
												:disabled="disabled"
											>
												<el-icon><Delete /></el-icon>
											</el-button>
										</div>
									</div>
								</div>
							</div>

							<!-- Raw - with format selection -->
							<div v-else-if="formConfig.bodyType === 'raw'" class="raw-section">
								<div class="raw-header">
									<div class="raw-format-controls">
										<el-select
											:model-value="formConfig.rawFormat"
											@update:model-value="setRawFormat"
											class="raw-format-select"
											:disabled="disabled"
										>
											<el-option label="JSON" value="json" />
											<el-option label="Text" value="text" />
											<el-option label="JavaScript" value="javascript" />
											<el-option label="HTML" value="html" />
											<el-option label="XML" value="xml" />
										</el-select>
										<el-button
											type="primary"
											@click="formatRawContent"
											:disabled="disabled || !formConfig.body.trim()"
											class="format-btn"
											:icon="DocumentCopy"
										>
											Beautify
										</el-button>
									</div>
								</div>
								<div class="raw-textarea-container">
									<variable-auto-complete
										:model-value="formConfig.body"
										@update:model-value="setBody"
										type="textarea"
										:rows="8"
										placeholder="Enter your content here, type '/' to insert variables..."
										class="font-mono text-sm raw-textarea"
										:disabled="disabled"
									/>
								</div>
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
					@click="handleTest"
					:loading="testing"
					:disabled="!formConfig.url"
				>
					Test Request
				</el-button>
			</div>

			<div v-if="testResult" class="test-result">
				<div class="bg-gray-50 dark:bg-gray-800 rounded-xl p-3">
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
import { computed, ref, nextTick } from 'vue';
import { Delete, MagicStick, DocumentCopy } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import { useI18n } from '@/hooks/useI18n';
import VariableAutoComplete from './VariableAutoComplete.vue';
import PrototypeTabs from '@/components/PrototypeTabs/index.vue';
import TabPane from '@/components/PrototypeTabs/TabPane.vue';
import { parseCurl, type ParsedCurlConfig } from '@/utils/curlParser';

interface KeyValueItem {
	key: string;
	value: string;
	focused?: boolean;
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
		body?: string; // 统一的请求体字段，当bodyType为'raw'时存储raw内容
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
	disabled?: boolean;
}

// Refs for textarea elements
const headerTextareaRefs = ref<any[]>([]);
const paramTextareaRefs = ref<any[]>([]);
const formDataTextareaRefs = ref<any[]>([]);
const urlEncodedTextareaRefs = ref<any[]>([]);

const { t } = useI18n();

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
		body: '', // 统一的请求体字段
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
			body: data.body || '', // 统一的请求体字段
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

const setBody = (val: string) => {
	formConfig.value = { ...formConfig.value, body: val } as any;
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

// 聚焦/失焦功能
const setHeaderFocused = (index: number, focused: boolean) => {
	const newHeadersList = [...formConfig.value.headersList];
	newHeadersList[index] = {
		...newHeadersList[index],
		focused: focused,
	};
	formConfig.value = { ...formConfig.value, headersList: newHeadersList };

	// 如果是聚焦，等待DOM更新后重新聚焦textarea
	if (focused) {
		nextTick(() => {
			const textareaRef = headerTextareaRefs.value[index];
			if (textareaRef && textareaRef.textarea) {
				textareaRef.textarea.focus();
			}
		});
	}
};

const setParamFocused = (index: number, focused: boolean) => {
	const newParamsList = [...formConfig.value.paramsList];
	newParamsList[index] = {
		...newParamsList[index],
		focused: focused,
	};
	formConfig.value = { ...formConfig.value, paramsList: newParamsList };

	// 如果是聚焦，等待DOM更新后重新聚焦textarea
	if (focused) {
		nextTick(() => {
			const textareaRef = paramTextareaRefs.value[index];
			if (textareaRef && textareaRef.textarea) {
				textareaRef.textarea.focus();
			}
		});
	}
};

const setFormDataFocused = (index: number, focused: boolean) => {
	const newFormDataList = [...formConfig.value.formDataList];
	newFormDataList[index] = {
		...newFormDataList[index],
		focused: focused,
	};
	formConfig.value = { ...formConfig.value, formDataList: newFormDataList };

	// 如果是聚焦，等待DOM更新后重新聚焦textarea
	if (focused) {
		nextTick(() => {
			const textareaRef = formDataTextareaRefs.value[index];
			if (textareaRef && textareaRef.textarea) {
				textareaRef.textarea.focus();
			}
		});
	}
};

const setUrlEncodedFocused = (index: number, focused: boolean) => {
	const newUrlEncodedList = [...formConfig.value.urlEncodedList];
	newUrlEncodedList[index] = {
		...newUrlEncodedList[index],
		focused: focused,
	};
	formConfig.value = { ...formConfig.value, urlEncodedList: newUrlEncodedList };

	// 如果是聚焦，等待DOM更新后重新聚焦textarea
	if (focused) {
		nextTick(() => {
			const textareaRef = urlEncodedTextareaRefs.value[index];
			if (textareaRef && textareaRef.textarea) {
				textareaRef.textarea.focus();
			}
		});
	}
};

// 格式化Raw内容
const formatRawContent = () => {
	try {
		const content = formConfig.value.body.trim();
		if (!content) {
			ElMessage.warning(t('sys.api.formatEmptyContent'));
			return;
		}

		let formattedContent = '';
		const format = formConfig.value.rawFormat;

		switch (format) {
			case 'json':
				try {
					const parsed = JSON.parse(content);
					formattedContent = JSON.stringify(parsed, null, 2);
				} catch (error) {
					ElMessage.warning(t('sys.api.formatInvalidJson'));
					return;
				}
				break;

			case 'xml':
			case 'html':
				// 简单的XML/HTML格式化
				formattedContent = formatXmlHtml(content);
				break;

			case 'javascript':
				// 对于JavaScript，我们可以做一些基本的格式化
				formattedContent = formatJavaScript(content);
				break;

			case 'text':
			default:
				ElMessage.info(t('sys.api.formatNotNeeded'));
				return;
		}

		setBody(formattedContent);
		ElMessage.success(t('sys.api.formatSuccess'));
	} catch (error) {
		ElMessage.warning(t('sys.api.formatFailed'));
	}
};

// XML/HTML格式化辅助函数
const formatXmlHtml = (content: string): string => {
	// 简单的XML/HTML格式化逻辑
	let formatted = content
		.replace(/></g, '>\n<') // 在标签之间添加换行
		.replace(/^\s+|\s+$/g, '') // 去除首尾空白
		.split('\n');

	let indentLevel = 0;
	const indentSize = 2;

	return formatted
		.map((line) => {
			const trimmed = line.trim();
			if (!trimmed) return '';

			// 减少缩进（闭合标签）
			if (trimmed.startsWith('</')) {
				indentLevel = Math.max(0, indentLevel - 1);
			}

			const indentedLine = ' '.repeat(indentLevel * indentSize) + trimmed;

			// 增加缩进（开放标签，但不是自闭合标签）
			if (trimmed.startsWith('<') && !trimmed.startsWith('</') && !trimmed.endsWith('/>')) {
				indentLevel++;
			}

			return indentedLine;
		})
		.join('\n');
};

// JavaScript格式化辅助函数
const formatJavaScript = (content: string): string => {
	// 简单的JavaScript格式化
	return content
		.replace(/;/g, ';\n') // 分号后换行
		.replace(/{/g, '{\n') // 开括号后换行
		.replace(/}/g, '\n}') // 闭括号前换行
		.replace(/,/g, ',\n') // 逗号后换行
		.split('\n')
		.map((line) => line.trim())
		.filter((line) => line.length > 0)
		.join('\n');
};

// 导入弹窗相关状态
const importDialogVisible = ref(false);
const activeImportTab = ref('curl');
const curlInput = ref('');
const importError = ref(''); // 导入错误信息
const importLoading = ref(false); // 导入加载状态

// 导入标签页配置
const importTabsConfig = [
	{
		value: 'curl',
		label: 'From cURL',
	},
	{
		value: 'ai',
		label: 'AI Generate',
	},
];

// 显示导入弹窗
const showImportDialog = () => {
	importDialogVisible.value = true;
	activeImportTab.value = 'curl';
	curlInput.value = '';
	importError.value = ''; // 清空错误信息
	importLoading.value = false; // 重置加载状态
};

// 关闭导入弹窗
const handleImportDialogClose = () => {
	importDialogVisible.value = false;
	curlInput.value = '';
	importError.value = ''; // 清空错误信息
	importLoading.value = false; // 重置加载状态
};

// 处理cURL导入
const handleCurlImport = async () => {
	importError.value = ''; // 清空之前的错误
	importLoading.value = true; // 开始加载
	try {
		if (!curlInput.value.trim()) {
			importError.value = 'Please enter a cURL command';
			return;
		}

		// 解析cURL命令
		const parsedConfig = parseCurl(curlInput.value);
		// 更新表单配置
		await updateFormFromParsedCurl(parsedConfig);

		// 成功后关闭弹窗
		handleImportDialogClose();
		ElMessage.success('cURL command imported successfully');
	} catch (error) {
		// 显示通用的格式错误信息
		importError.value = 'Incorrect format, please input the right format to import';
	} finally {
		importLoading.value = false; // 结束加载
	}
};

/**
 * 将键值对对象转换为KeyValueItem数组，并确保末尾有空行
 */
const convertToKeyValueList = (obj: Record<string, string>): KeyValueItem[] => {
	const list: KeyValueItem[] = [];

	// 转换有效的键值对
	Object.entries(obj || {}).forEach(([key, value]) => {
		if (key && value !== undefined && value !== null) {
			list.push({ key: key.trim(), value: String(value).trim() });
		}
	});

	// 确保至少有一个空行用于添加新项
	if (list.length === 0 || list[list.length - 1].key !== '') {
		list.push({ key: '', value: '' });
	}

	return list;
};

/**
 * 重置所有body相关字段为默认状态
 */
const resetBodyFields = () => {
	formConfig.value.formDataList = [{ key: '', value: '' }];
	formConfig.value.urlEncodedList = [{ key: '', value: '' }];
	formConfig.value.body = '';
	formConfig.value.rawFormat = 'json';
};

// 根据解析的cURL配置更新表单
const updateFormFromParsedCurl = async (parsedConfig: ParsedCurlConfig) => {
	try {
		// 1. 更新基本信息
		formConfig.value.url = parsedConfig.url || '';
		formConfig.value.method = parsedConfig.method || 'GET';

		// 2. 更新查询参数
		formConfig.value.paramsList = convertToKeyValueList(parsedConfig.params);

		// 3. 更新请求头
		formConfig.value.headersList = convertToKeyValueList(parsedConfig.headers);

		// 4. 更新请求体类型
		formConfig.value.bodyType = parsedConfig.bodyType || 'none';

		// 5. 根据body类型更新相应字段
		switch (parsedConfig.bodyType) {
			case 'form-data':
				// 重置其他body字段
				formConfig.value.urlEncodedList = [{ key: '', value: '' }];
				formConfig.value.body = '';
				// 设置form-data
				formConfig.value.formDataList = convertToKeyValueList(parsedConfig.formData || {});
				break;

			case 'x-www-form-urlencoded':
				// 重置其他body字段
				formConfig.value.formDataList = [{ key: '', value: '' }];
				formConfig.value.body = '';
				// 设置url-encoded
				formConfig.value.urlEncodedList = convertToKeyValueList(
					parsedConfig.urlEncoded || {}
				);
				break;

			case 'raw':
				// 重置其他body字段
				formConfig.value.formDataList = [{ key: '', value: '' }];
				formConfig.value.urlEncodedList = [{ key: '', value: '' }];
				// 设置raw body
				formConfig.value.body = parsedConfig.rawBody || '';
				formConfig.value.rawFormat = parsedConfig.rawFormat || 'json';
				break;

			case 'none':
			default:
				// 重置所有body字段
				resetBodyFields();
				break;
		}

		// 6. 触发响应式更新
		formConfig.value = { ...formConfig.value };
	} catch (error) {
		throw new Error('Failed to update form configuration');
	}
};

const handleTest = () => {
	emit('test');
};
</script>

<style scoped lang="scss">
.http-form {
	@apply dark:border-gray-700 rounded-xl;
}

// Params Section Styles (Legacy)
.params-section {
	@apply w-full border border-gray-200 dark:border-gray-700 rounded-xl overflow-hidden;
}

// Enhanced Params Section Styles (New)
.params-section-enhanced {
	@apply w-full border border-gray-200 dark:border-gray-700 rounded-xl overflow-hidden;
}

.params-header-enhanced {
	@apply bg-gray-50 dark:bg-gray-800 px-4 py-3 border-b border-gray-200 dark:border-gray-700 grid grid-cols-12 gap-4 items-center;

	.param-col-key {
		@apply col-span-4 text-sm font-medium text-gray-600 dark:text-gray-400;
	}

	.param-col-value {
		@apply col-span-7 text-sm font-medium text-gray-600 dark:text-gray-400;
	}

	.param-actions-enhanced {
		@apply col-span-1 text-sm font-medium text-gray-600 dark:text-gray-400 text-center;
	}
}

.params-body-enhanced {
	@apply p-3 space-y-2;
}

.param-row-enhanced {
	@apply grid grid-cols-12 gap-4 items-start;
}

.param-key-container {
	@apply col-span-4;
}

.param-value-container {
	@apply col-span-7 space-y-1;
}

.param-delete-container {
	@apply col-span-1 flex justify-center;
}

.param-input-enhanced {
	@apply w-full;
}

.param-value-wrapper {
	@apply relative w-full;
}

// 输入框内部的按钮组
.param-input-actions {
	@apply flex items-center space-x-1 mr-2;
}

.param-action-btn {
	@apply w-5 h-5 flex items-center justify-center text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition-colors;

	&:hover {
		@apply bg-gray-100 dark:bg-gray-700 rounded;
	}
}

// 文本域容器
.param-textarea-container {
	@apply relative w-full;
}

.param-textarea-enhanced {
	@apply font-mono text-sm w-full;
}

.param-textarea-auto-height {
	@apply font-mono text-sm w-full;

	:deep(.el-textarea__inner) {
		resize: none !important;
		overflow-y: hidden !important;
	}
}

// 文本域右上角的按钮组
.param-textarea-actions {
	@apply absolute top-2 right-2 flex items-center space-x-1 bg-white dark:bg-gray-800 px-2 py-1 rounded-xl shadow-sm border border-gray-200 dark:border-gray-600;
	z-index: 10;
}

.param-value-hint {
	@apply text-xs text-gray-500 dark:text-gray-400 italic;
}

.param-delete-enhanced {
	@apply w-8 h-8 flex items-center justify-center text-red-400 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-xl transition-colors;
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

// Raw Section Styles
.raw-section {
	@apply w-full;
}

.raw-header {
	@apply mb-4;
}

.raw-format-controls {
	@apply flex items-center gap-3;
}

.raw-format-select {
	@apply min-w-32;
}

.format-btn {
	@apply flex items-center gap-2;

	.el-icon {
		@apply text-sm;
	}
}

.raw-textarea-container {
	@apply relative;
}

.raw-textarea {
	@apply w-full min-h-48 resize-y;
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
	@apply border border-gray-200 dark:border-gray-700 rounded-xl p-4;
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

// 导入错误信息样式
.import-error-message {
	@apply mt-4 p-4 rounded-xl border border-red-200 dark:border-red-700 bg-red-50 dark:bg-red-900/20;
}
</style>
