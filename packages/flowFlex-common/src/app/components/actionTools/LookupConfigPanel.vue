<template>
	<div class="lookup-config-panel rounded-lg p-4 border-l-4 border-primary">
		<!-- Configuration Fields -->
		<div class="grid grid-cols-2 gap-4">
			<!-- Row 1: Endpoint + Display Path -->
			<el-form-item label="Options Source (Endpoint)" class="mb-3">
				<el-input
					v-model="localConfig.endpoint"
					placeholder="/api/v1/options"
					:disabled="disabled"
					@input="emitUpdate"
				/>
			</el-form-item>

			<el-form-item label="Display Field (JSONPath)" class="mb-3">
				<el-input
					v-model="localConfig.displayPath"
					placeholder="name"
					:disabled="disabled"
					@input="emitUpdate"
				/>
			</el-form-item>

			<!-- Row 2: Value Path + Response Path -->
			<el-form-item label="Value Field (JSONPath)" class="mb-3">
				<el-input
					v-model="localConfig.valuePath"
					placeholder="id"
					:disabled="disabled"
					@input="emitUpdate"
				/>
			</el-form-item>

			<el-form-item label="Response Path (optional)" class="mb-3">
				<el-input
					v-model="localConfig.responsePath"
					placeholder="data.items"
					:disabled="disabled"
					@input="emitUpdate"
				/>
			</el-form-item>
		</div>

		<!-- Custom Headers (collapsible) -->
		<div class="mt-2">
			<div
				class="flex items-center gap-2 cursor-pointer select-none text-sm text-gray-600 dark:text-gray-400 hover:text-primary"
				@click="headersExpanded = !headersExpanded"
			>
				<icon
					:icon="headersExpanded ? 'tabler:chevron-down' : 'tabler:chevron-right'"
					class="text-xs"
				/>
				<span>Custom Headers (optional)</span>
				<span v-if="headersList.length > 0" class="text-xs text-gray-400">
					({{ headersList.length }})
				</span>
			</div>

			<div v-if="headersExpanded" class="mt-3 space-y-2">
				<div
					v-for="(header, index) in headersList"
					:key="index"
					class="flex items-center gap-2"
				>
					<el-input
						v-model="header.key"
						placeholder="Header name"
						class="flex-1"
						:disabled="disabled"
						@input="onHeadersChange"
					/>
					<el-input
						v-model="header.value"
						placeholder="Header value"
						class="flex-1"
						:disabled="disabled"
						@input="onHeadersChange"
					/>
					<el-button
						type="danger"
						:icon="Delete"
						circle
						size="small"
						:disabled="disabled"
						@click="removeHeader(index)"
					/>
				</div>

				<el-button
					type="primary"
					plain
					size="small"
					:disabled="disabled"
					@click="addHeader"
				>
					+ Add Header
				</el-button>
			</div>
		</div>

		<!-- Test Button + Preview -->
		<div class="mt-4 flex items-center gap-3">
			<el-button
				type="primary"
				size="small"
				:loading="previewLoading"
				:disabled="!canTest || disabled"
				@click="handleTest"
			>
				Test Options
			</el-button>
			<span v-if="previewResult && previewResult.success" class="text-xs text-green-600">
				Showing {{ previewResult.options?.length || 0 }} of
				{{ previewResult.totalCount || 0 }} options
			</span>
		</div>

		<!-- Preview Result -->
		<div v-if="previewResult" class="mt-3">
			<!-- Success: show options table -->
			<el-table
				v-if="previewResult.success"
				:data="previewResult.options"
				size="small"
				border
				max-height="200"
				class="preview-table"
			>
				<el-table-column prop="display" label="Display" min-width="150" />
				<el-table-column prop="value" label="Value" min-width="150" />
			</el-table>

			<!-- Error: show alert -->
			<el-alert
				v-else
				type="error"
				:title="previewResult.error || 'Failed to fetch options'"
				:closable="true"
				show-icon
				@close="previewResult = null"
			/>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue';
import { Delete } from '@element-plus/icons-vue';
import { previewLookupOptions } from '@/apis/action/field-lookup';
import type { LookupConfig, LookupPreviewResponse } from '#/action-field-lookup';

interface Props {
	/** Lookup configuration value */
	modelValue: LookupConfig;
	/** Integration ID for authentication */
	integrationId: string;
	/** Whether the panel is disabled */
	disabled?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	disabled: false,
});

const emit = defineEmits<{
	(e: 'update:modelValue', value: LookupConfig): void;
}>();

// Local reactive copy of config
const localConfig = reactive<LookupConfig>({
	endpoint: '',
	displayPath: '',
	valuePath: '',
	responsePath: '',
	headers: undefined,
	integrationId: null,
});

// Headers as key-value list for editing
const headersList = ref<{ key: string; value: string }[]>([]);
const headersExpanded = ref(false);

// Preview state
const previewLoading = ref(false);
const previewResult = ref<LookupPreviewResponse | null>(null);

// Can test when required fields are filled
const canTest = computed(() => {
	return (
		localConfig.endpoint?.trim() &&
		localConfig.displayPath?.trim() &&
		localConfig.valuePath?.trim()
	);
});

// Watch modelValue and sync to local state
watch(
	() => props.modelValue,
	(newVal) => {
		if (newVal) {
			localConfig.endpoint = newVal.endpoint || '';
			localConfig.displayPath = newVal.displayPath || '';
			localConfig.valuePath = newVal.valuePath || '';
			localConfig.responsePath = newVal.responsePath || '';
			localConfig.integrationId = newVal.integrationId || null;

			// Convert headers object to list
			if (newVal.headers && Object.keys(newVal.headers).length > 0) {
				headersList.value = Object.entries(newVal.headers).map(([key, value]) => ({
					key,
					value,
				}));
			} else {
				headersList.value = [];
			}
		}
	},
	{ immediate: true, deep: true }
);

// Emit updated config to parent
function emitUpdate() {
	const config: LookupConfig = {
		endpoint: localConfig.endpoint,
		displayPath: localConfig.displayPath,
		valuePath: localConfig.valuePath,
		responsePath: localConfig.responsePath || undefined,
		headers: buildHeadersObject(),
		integrationId: localConfig.integrationId,
	};
	emit('update:modelValue', config);
}

// Build headers object from list
function buildHeadersObject(): Record<string, string> | undefined {
	const validHeaders = headersList.value.filter((h) => h.key.trim());
	if (validHeaders.length === 0) return undefined;

	const obj: Record<string, string> = {};
	for (const h of validHeaders) {
		obj[h.key.trim()] = h.value;
	}
	return obj;
}

// Header management
function addHeader() {
	headersList.value.push({ key: '', value: '' });
}

function removeHeader(index: number) {
	headersList.value.splice(index, 1);
	onHeadersChange();
}

function onHeadersChange() {
	emitUpdate();
}

// Test lookup
async function handleTest() {
	if (!canTest.value) return;

	previewLoading.value = true;
	previewResult.value = null;

	try {
		const result = await previewLookupOptions({
			integrationId: props.integrationId || '0',
			endpoint: localConfig.endpoint,
			displayPath: localConfig.displayPath,
			valuePath: localConfig.valuePath,
			responsePath: localConfig.responsePath || undefined,
			headers: buildHeadersObject(),
		});

		// Handle response - defHttp may return the data directly or wrapped
		if (result && typeof result === 'object') {
			previewResult.value = {
				success: result.success ?? false,
				options: result.options ?? [],
				totalCount: result.totalCount ?? 0,
				error: result.error ?? undefined,
			};
		} else {
			previewResult.value = {
				success: false,
				options: [],
				totalCount: 0,
				error: 'Unexpected response format',
			};
		}
	} catch (error: any) {
		previewResult.value = {
			success: false,
			options: [],
			totalCount: 0,
			error: error?.message || 'Failed to preview lookup options',
		};
	} finally {
		previewLoading.value = false;
	}
}
</script>

<style lang="scss" scoped>
.lookup-config-panel {
	background-color: var(--el-fill-color-lighter);
	border-color: var(--el-color-primary);

	:deep(.el-form-item__label) {
		font-size: 12px;
		color: var(--el-text-color-secondary);
	}

	.preview-table {
		:deep(.el-table__header th) {
			font-size: 12px;
		}
		:deep(.el-table__body td) {
			font-size: 12px;
		}
	}
}
</style>
