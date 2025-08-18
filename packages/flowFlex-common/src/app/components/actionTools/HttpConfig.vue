<template>
	<div class="http-config space-y-6">
		<!-- HTTP Configuration Form -->
		<div class="http-form">
			<el-form :model="config" label-width="120px" class="space-y-6" label-position="top">
				<el-form-item label="Request URL" required class="request-url-input">
					<el-input
						v-model="url"
						placeholder="Enter URL, type '/' to insert variables"
						class="w-full"
					>
						<template #prepend>
							<el-select v-model="method" style="width: 115px">
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
								v-for="(header, index) in config.headers"
								:key="index"
								class="param-row"
							>
								<el-input
									v-model="header.key"
									placeholder="Type '/' to insert variables"
									class="param-input"
									@input="checkAddNewHeaderRow(index)"
								/>
								<el-input
									v-model="header.value"
									placeholder="Type '/' to insert variables"
									class="param-input"
									@input="checkAddNewHeaderRow(index)"
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
								v-for="(param, index) in config.params"
								:key="index"
								class="param-row"
							>
								<el-input
									v-model="param.key"
									placeholder="/"
									class="param-input"
									@input="checkAddNewParamRow(index)"
								/>
								<el-input
									v-model="param.value"
									placeholder="/"
									class="param-input"
									@input="checkAddNewParamRow(index)"
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
						<el-radio-group v-model="bodyType" class="body-type-group">
							<el-radio value="none">none</el-radio>
							<el-radio value="form-data">form-data</el-radio>
							<el-radio value="x-www-form-urlencoded">x-www-form-urlencoded</el-radio>
							<el-radio value="raw">raw</el-radio>
							<el-radio value="binary">binary</el-radio>
						</el-radio-group>

						<!-- Body Content based on type -->
						<div class="body-content">
							<!-- None - No content -->
							<div v-if="config.bodyType === 'none'" class="body-none">
								<p class="text-gray-500 text-sm">This request has no body</p>
							</div>

							<!-- Form Data -->
							<div v-else-if="config.bodyType === 'form-data'" class="params-section">
								<div class="params-header">
									<div class="param-col">Key</div>
									<div class="param-col">Value</div>
									<div class="param-actions">Actions</div>
								</div>
								<div class="params-body">
									<div
										v-for="(item, index) in config.formData"
										:key="index"
										class="param-row"
									>
										<el-input
											v-model="item.key"
											placeholder="/"
											class="param-input"
											@input="checkAddNewFormDataRow(index)"
										/>
										<el-input
											v-model="item.value"
											placeholder="/"
											class="param-input"
											@input="checkAddNewFormDataRow(index)"
										/>
										<el-button
											type="danger"
											text
											@click="removeFormData(index)"
											class="param-delete"
											v-if="config.formData.length > 1"
										>
											<el-icon><Delete /></el-icon>
										</el-button>
									</div>
								</div>
							</div>

							<!-- URL Encoded -->
							<div
								v-else-if="config.bodyType === 'x-www-form-urlencoded'"
								class="params-section"
							>
								<div class="params-header">
									<div class="param-col">Key</div>
									<div class="param-col">Value</div>
									<div class="param-actions">Actions</div>
								</div>
								<div class="params-body">
									<div
										v-for="(item, index) in config.urlEncoded"
										:key="index"
										class="param-row"
									>
										<el-input
											v-model="item.key"
											placeholder="/"
											class="param-input"
											@input="checkAddNewUrlEncodedRow(index)"
										/>
										<el-input
											v-model="item.value"
											placeholder="/"
											class="param-input"
											@input="checkAddNewUrlEncodedRow(index)"
										/>
										<el-button
											type="danger"
											text
											@click="removeUrlEncoded(index)"
											class="param-delete"
											v-if="config.urlEncoded.length > 1"
										>
											<el-icon><Delete /></el-icon>
										</el-button>
									</div>
								</div>
							</div>

							<!-- Raw - with format selection -->
							<div v-else-if="config.bodyType === 'raw'" class="raw-section">
								<div class="raw-header">
									<el-select v-model="rawFormat" class="raw-format-select">
										<el-option label="JSON" value="json" />
										<el-option label="Text" value="text" />
										<el-option label="JavaScript" value="javascript" />
										<el-option label="HTML" value="html" />
										<el-option label="XML" value="xml" />
									</el-select>
								</div>
								<el-input
									v-model="rawBody"
									type="textarea"
									:rows="8"
									placeholder="Enter your content here, type '/' to insert variables..."
									class="font-mono text-sm raw-textarea"
								/>
							</div>

							<!-- Binary -->
							<div v-else-if="config.bodyType === 'binary'" class="binary-section">
								<el-upload
									drag
									:auto-upload="false"
									:show-file-list="true"
									class="binary-upload"
								>
									<el-icon class="el-icon--upload text-4xl"><Upload /></el-icon>
									<div class="el-upload__text">
										Drop file here or click to select
									</div>
								</el-upload>
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
					:disabled="!config.url"
				>
					Test Request
				</el-button>
			</div>

			<div v-if="testResult" class="test-result">
				<div class="bg-gray-50 dark:bg-gray-800 rounded p-3">
					<h6 class="font-medium text-sm mb-2">Test Result:</h6>
					<pre class="text-xs text-gray-700 dark:text-gray-300 whitespace-pre-wrap">{{
						testResult
					}}</pre>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { reactive, computed } from 'vue';
import { Delete, Upload } from '@element-plus/icons-vue';

interface KeyValuePair {
	key: string;
	value: string;
}

interface Props {
	modelValue?: any;
	testing?: boolean;
	testResult?: string;
}

const props = withDefaults(defineProps<Props>(), {
	modelValue: () => ({}),
	testing: false,
	testResult: '',
});

const emit = defineEmits<{
	'update:modelValue': [value: any];
	test: [];
}>();

// Local reactive config for internal use
const localConfig = reactive({
	url: '',
	method: 'GET',
	headers: [{ key: '', value: '' }] as KeyValuePair[],
	params: [{ key: '', value: '' }] as KeyValuePair[],
	bodyType: 'none' as 'none' | 'form-data' | 'x-www-form-urlencoded' | 'raw' | 'binary',
	formData: [{ key: '', value: '' }] as KeyValuePair[],
	urlEncoded: [{ key: '', value: '' }] as KeyValuePair[],
	rawFormat: 'json',
	rawBody: '',
});

// Helper function to emit changes
const emitChanges = () => {
	const exportValue = {
		...localConfig,
		// Convert headers back to string format for compatibility
		headers: Array.isArray(localConfig.headers)
			? localConfig.headers.reduce(
					(acc, { key, value }) => {
						if (key && value) acc[key] = value;
						return acc;
					},
					{} as Record<string, string>
			  )
			: {},
	};
	emit('update:modelValue', exportValue);
};

// Initialize localConfig from props
if (props.modelValue) {
	if (typeof props.modelValue.headers === 'string') {
		try {
			const headerObj = JSON.parse(props.modelValue.headers);
			localConfig.headers = Object.entries(headerObj).map(([key, value]) => ({
				key,
				value: String(value),
			}));
		} catch {
			localConfig.headers = [{ key: '', value: '' }];
		}
	}
	Object.assign(localConfig, {
		...props.modelValue,
		headers: localConfig.headers, // Keep the converted headers
	});
}

// Individual computed properties for form fields
const url = computed({
	get: () => localConfig.url,
	set: (value) => {
		localConfig.url = value;
		emitChanges();
	},
});

const method = computed({
	get: () => localConfig.method,
	set: (value) => {
		localConfig.method = value;
		emitChanges();
	},
});

const bodyType = computed({
	get: () => localConfig.bodyType,
	set: (value) => {
		localConfig.bodyType = value;
		emitChanges();
	},
});

const rawFormat = computed({
	get: () => localConfig.rawFormat,
	set: (value) => {
		localConfig.rawFormat = value;
		emitChanges();
	},
});

const rawBody = computed({
	get: () => localConfig.rawBody,
	set: (value) => {
		localConfig.rawBody = value;
		emitChanges();
	},
});

// Computed for accessing arrays (read-only in template, modified via functions)
const config = computed(() => localConfig);

// Header management
const removeHeader = (index: number) => {
	if (localConfig.headers.length > 1) {
		localConfig.headers.splice(index, 1);
		emitChanges();
	}
};

const checkAddNewHeaderRow = (index: number) => {
	const header = localConfig.headers[index];
	const isLastRow = index === localConfig.headers.length - 1;
	const hasContent = header.key.trim() || header.value.trim();

	if (isLastRow && hasContent) {
		localConfig.headers.push({ key: '', value: '' });
		emitChanges();
	}
};

// Params management
const removeParam = (index: number) => {
	if (localConfig.params.length > 1) {
		localConfig.params.splice(index, 1);
		emitChanges();
	}
};

const checkAddNewParamRow = (index: number) => {
	const param = localConfig.params[index];
	const isLastRow = index === localConfig.params.length - 1;
	const hasContent = param.key.trim() || param.value.trim();

	if (isLastRow && hasContent) {
		localConfig.params.push({ key: '', value: '' });
		emitChanges();
	}
};

// Form data management
const removeFormData = (index: number) => {
	if (localConfig.formData.length > 1) {
		localConfig.formData.splice(index, 1);
		emitChanges();
	}
};

const checkAddNewFormDataRow = (index: number) => {
	const item = localConfig.formData[index];
	const isLastRow = index === localConfig.formData.length - 1;
	const hasContent = item.key.trim() || item.value.trim();

	if (isLastRow && hasContent) {
		localConfig.formData.push({ key: '', value: '' });
		emitChanges();
	}
};

// URL encoded management
const removeUrlEncoded = (index: number) => {
	if (localConfig.urlEncoded.length > 1) {
		localConfig.urlEncoded.splice(index, 1);
		emitChanges();
	}
};

const checkAddNewUrlEncodedRow = (index: number) => {
	const item = localConfig.urlEncoded[index];
	const isLastRow = index === localConfig.urlEncoded.length - 1;
	const hasContent = item.key.trim() || item.value.trim();

	if (isLastRow && hasContent) {
		localConfig.urlEncoded.push({ key: '', value: '' });
		emitChanges();
	}
};

const handleTest = () => {
	emit('test');
};
</script>

<style scoped lang="scss">
.http-form {
	@apply border border-gray-200 dark:border-gray-700 rounded-lg p-6;
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

.binary-section {
	@apply space-y-3;
}

.binary-upload {
	@apply w-full;
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
