<template>
	<div class="ai-config-page">
		<!-- Page Header -->
		<PageHeader
			title="AI Model Configuration"
			description="Manage your AI model settings and test connections"
		>
			<template #actions>
				<el-button
					@click="addNewConfig"
					type="primary"
					size="default"
					class="page-header-btn page-header-btn-primary"
					:icon="Plus"
				>
					Add New Model
				</el-button>
			</template>
		</PageHeader>

		<!-- Models Table -->
		<el-card shadow="hover">
			<el-table :data="modelConfigs" v-loading="loading" stripe>
				<el-table-column prop="provider" label="Provider" width="120">
					<template #default="{ row }">
						<el-tag :type="getProviderType(row.provider)">
							{{ row.provider }}
						</el-tag>
					</template>
				</el-table-column>

				<el-table-column prop="modelName" label="Model" width="180" />

				<el-table-column
					prop="baseUrl"
					label="API URL"
					min-width="200"
					show-overflow-tooltip
				/>

				<el-table-column label="Status" width="100">
					<template #default="{ row }">
						<el-tag :type="row.isAvailable ? 'success' : 'danger'" size="small">
							{{ row.isAvailable ? 'Online' : 'Offline' }}
						</el-tag>
					</template>
				</el-table-column>

				<el-table-column label="Default" width="80" align="center">
					<template #default="{ row }">
						<el-icon v-if="row.isDefault" class="text-green-500">
							<Check />
						</el-icon>
					</template>
				</el-table-column>

				<el-table-column prop="lastCheckTime" label="Last Check" width="180">
					<template #default="{ row }">
						{{ formatDateTime(row.lastCheckTime) }}
					</template>
				</el-table-column>

				<el-table-column label="Actions" min-width="320" fixed="right">
					<template #default="{ row }">
						<div class="action-buttons">
							<el-button
								size="small"
								@click="testConnection(row)"
								:loading="testingId === row.id"
							>
								Test
							</el-button>
							<el-button size="small" @click="editConfig(row)">Edit</el-button>
							<el-button
								size="small"
								type="primary"
								@click="setDefault(row.id)"
								:disabled="row.isDefault"
							>
								Set Default
							</el-button>
							<el-button
								size="small"
								type="danger"
								@click="deleteConfig(row.id)"
								:disabled="row.isDefault"
							>
								Delete
							</el-button>
						</div>
					</template>
				</el-table-column>
			</el-table>
		</el-card>

		<!-- Add/Edit Dialog -->
		<el-dialog
			v-model="showAddDialog"
			:title="editingConfig ? 'Edit AI Model' : 'Add New AI Model'"
			width="600px"
		>
			<el-form
				ref="configFormRef"
				:model="configForm"
				:rules="configRules"
				label-width="120px"
			>
				<el-form-item label="Provider" prop="provider">
					<el-select
						v-model="configForm.provider"
						placeholder="Select AI Provider"
						@change="onProviderChange"
					>
						<el-option
							v-for="provider in providers"
							:key="provider.name"
							:label="provider.displayName"
							:value="provider.name"
						/>
					</el-select>
				</el-form-item>

				<el-form-item label="Model Name" prop="modelName">
					<el-select
						v-model="configForm.modelName"
						placeholder="Select model"
						:disabled="!configForm.provider"
					>
						<el-option
							v-for="model in supportedModels"
							:key="model"
							:label="model"
							:value="model"
						/>
					</el-select>
				</el-form-item>

				<el-form-item label="API Key" prop="apiKey">
					<el-input
						v-model="configForm.apiKey"
						type="password"
						placeholder="Enter your API key"
						show-password
					/>
				</el-form-item>

				<el-form-item label="Base URL">
					<el-input v-model="configForm.baseUrl" placeholder="API base URL (optional)" />
				</el-form-item>

				<el-form-item label="Temperature">
					<el-slider
						v-model="configForm.temperature"
						:min="0"
						:max="2"
						:step="0.1"
						show-input
					/>
				</el-form-item>

				<el-form-item label="Max Tokens">
					<el-input-number
						v-model="configForm.maxTokens"
						:min="100"
						:max="32000"
						:step="100"
					/>
				</el-form-item>

				<el-form-item label="Set as Default">
					<el-switch v-model="configForm.isDefault" />
				</el-form-item>

				<el-form-item label="Remarks">
					<el-input
						v-model="configForm.remarks"
						type="textarea"
						:rows="3"
						placeholder="Optional notes"
					/>
				</el-form-item>
			</el-form>

			<template #footer>
				<el-button @click="cancelConfig">Cancel</el-button>
				<el-button type="primary" @click="saveConfig" :loading="saving">
					{{ editingConfig ? 'Update' : 'Add' }} Model
				</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Plus, Check } from '@element-plus/icons-vue';
import PageHeader from '@/components/global/PageHeader/index.vue';
import {
	getUserAIModels,
	createAIModel,
	updateAIModel,
	deleteAIModel,
	setDefaultAIModel,
	testAIModelConnection,
	getAIProviders,
	type AIModelConfig,
	type AIProviderInfo,
} from '@/apis/ai/config';

// Reactive data
const loading = ref(false);
const saving = ref(false);
const testingId = ref<number | null>(null);
const modelConfigs = ref<AIModelConfig[]>([]);
const providers = ref<AIProviderInfo[]>([]);
const showAddDialog = ref(false);
const editingConfig = ref<AIModelConfig | null>(null);
const configFormRef = ref();

const configForm = reactive<Partial<AIModelConfig>>({
	provider: '',
	modelName: '',
	apiKey: '',
	baseUrl: '',
	temperature: 0.7,
	maxTokens: 4096,
	isDefault: false,
	remarks: '',
});

const configRules = {
	provider: [{ required: true, message: 'Please select a provider', trigger: 'change' }],
	modelName: [{ required: true, message: 'Please select model name', trigger: 'change' }],
	apiKey: [{ required: true, message: 'Please enter API key', trigger: 'blur' }],
};

// Computed properties
const supportedModels = computed(() => {
	if (!configForm.provider) return [];
	const provider = providers.value.find((p) => p.name === configForm.provider);
	return provider?.supportedModels || [];
});

// Methods
const loadConfigs = async () => {
	loading.value = true;
	try {
		console.log('🔍 Loading AI model configurations...');
		const response = await getUserAIModels();
		console.log('📥 AI model configuration response:', response);

		if (response.success && response.code == 200) {
			modelConfigs.value = response.data || [];
			console.log(
				'✅ AI model configurations loaded successfully:',
				modelConfigs.value.length,
				'configurations'
			);
		} else {
			console.error('❌ Failed to load AI model configurations:', response);
			ElMessage.error(
				`Failed to load AI model configurations: ${response.message || 'Unknown error'}`
			);
		}
	} catch (error) {
		console.error('💥 AI model configuration loading error:', error);

		// More detailed error information
		if (error?.response) {
			const status = error.response.status;
			const statusText = error.response.statusText;
			const data = error.response.data;
			ElMessage.error(
				`Request failed (${status} ${statusText}): ${
					data?.message || data || 'Server error'
				}`
			);
		} else if (error?.request) {
			ElMessage.error(
				'Network request failed, please check if the backend service is running'
			);
		} else {
			ElMessage.error(`Request configuration error: ${error.message}`);
		}
	} finally {
		loading.value = false;
	}
};

const loadProviders = async () => {
	try {
		console.log('🔍 Loading AI providers list...');
		const response = await getAIProviders();
		console.log('📥 AI providers response:', response);

		if (response.success && response.code == 200) {
			providers.value = (response.data as unknown as AIProviderInfo[]) || [];
			console.log(
				'✅ AI providers loaded successfully:',
				providers.value.length,
				'providers'
			);
		} else {
			console.error('❌ Failed to load AI providers:', response);
			ElMessage.error(`Failed to load AI providers: ${response.message || 'Unknown error'}`);
		}
	} catch (error) {
		console.error('💥 AI providers loading error:', error);

		// More detailed error information
		if (error?.response) {
			const status = error.response.status;
			const statusText = error.response.statusText;
			const data = error.response.data;
			ElMessage.error(
				`Request failed (${status} ${statusText}): ${
					data?.message || data || 'Server error'
				}`
			);
		} else if (error?.request) {
			ElMessage.error(
				'Network request failed, please check if the backend service is running'
			);
		} else {
			ElMessage.error(`Request configuration error: ${error.message}`);
		}
	}
};

const testConnection = async (config: AIModelConfig) => {
	testingId.value = config.id!;
	try {
		const response = await testAIModelConnection(config);
		if (response.success && response.code == 200) {
			const result = response.data;
			if (result.success) {
				ElMessage.success(result.message || 'Connection test successful!');
				// Update configuration status
				const configIndex = modelConfigs.value.findIndex((c) => c.id === config.id);
				if (configIndex !== -1) {
					modelConfigs.value[configIndex].isAvailable = true;
					modelConfigs.value[configIndex].lastCheckTime = new Date().toISOString();
				}
			} else {
				ElMessage.error(result.message || 'Connection test failed');
			}
		} else {
			ElMessage.error(response.message || 'Connection test failed');
		}
	} catch (error) {
		console.error('Connection test error:', error);
		ElMessage.error('Connection test failed');
	} finally {
		testingId.value = null;
	}
};

const addNewConfig = () => {
	editingConfig.value = null;
	resetConfigForm();
	showAddDialog.value = true;
};

const editConfig = (config: AIModelConfig) => {
	editingConfig.value = config;
	Object.assign(configForm, {
		provider: config.provider,
		modelName: config.modelName,
		apiKey: config.apiKey,
		baseUrl: config.baseUrl,
		temperature: config.temperature,
		maxTokens: config.maxTokens,
		isDefault: config.isDefault,
		remarks: config.remarks,
	});
	showAddDialog.value = true;
};

const setDefault = async (configId: number) => {
	try {
		const response = await setDefaultAIModel(configId);
		if (response.success && response.code == 200) {
			// Update local state
			modelConfigs.value.forEach((config) => {
				config.isDefault = config.id === configId;
			});
			ElMessage.success('Default model updated successfully!');
		} else {
			ElMessage.error(response.message || 'Failed to set default model');
		}
	} catch (error) {
		console.error('Set default error:', error);
		ElMessage.error('Failed to set default model');
	}
};

const deleteConfig = async (configId: number) => {
	try {
		await ElMessageBox.confirm(
			'This will permanently delete the AI model configuration. Continue?',
			'Warning',
			{ confirmButtonText: 'Delete', cancelButtonText: 'Cancel', type: 'warning' }
		);

		const response = await deleteAIModel(configId);
		if (response.success && response.code == 200) {
			modelConfigs.value = modelConfigs.value.filter((config) => config.id !== configId);
			ElMessage.success('Configuration deleted successfully!');
		} else {
			ElMessage.error(response.message || 'Failed to delete configuration');
		}
	} catch (error) {
		if (error !== 'cancel') {
			console.error('Delete error:', error);
			ElMessage.error('Failed to delete configuration');
		}
	}
};

const saveConfig = async () => {
	if (!configFormRef.value) return;

	try {
		await configFormRef.value.validate();
	} catch {
		return;
	}

	saving.value = true;
	try {
		if (editingConfig.value) {
			// Update existing
			const updatedConfig = { ...editingConfig.value, ...configForm } as AIModelConfig;
			const response = await updateAIModel(updatedConfig);
			if (response.success && response.code == 200) {
				// Update local state to reflect the changes including isDefault
				Object.assign(editingConfig.value, configForm);

				// Update the configuration in the list
				const configIndex = modelConfigs.value.findIndex(
					(config) => config.id === editingConfig.value!.id
				);
				if (configIndex !== -1) {
					Object.assign(modelConfigs.value[configIndex], configForm);
				}

				ElMessage.success('Configuration updated successfully!');
			} else {
				ElMessage.error(response.message || 'Failed to update configuration');
				return;
			}
		} else {
			// Add new
			const response = await createAIModel(configForm);
			if (response.success && response.code == 200) {
				const newConfig: AIModelConfig = {
					...(configForm as AIModelConfig),
					id: response.data,
					isAvailable: false,
					lastCheckTime: undefined, // Set to undefined instead of empty string
					userId: 0,
					enableStreaming: false,
					createdTime: new Date().toISOString(),
					updatedTime: new Date().toISOString(),
				};
				modelConfigs.value.push(newConfig);

				// If user wants to set this as default, call the separate API
				if (configForm.isDefault) {
					const defaultResponse = await setDefaultAIModel(response.data);
					if (defaultResponse.success && defaultResponse.code == 200) {
						// Update local state - set all others to false, this one to true
						modelConfigs.value.forEach((config) => {
							config.isDefault = config.id === response.data;
						});
					} else {
						ElMessage.warning('Configuration created but failed to set as default');
					}
				}

				ElMessage.success('Configuration added successfully!');
			} else {
				ElMessage.error(response.message || 'Failed to create configuration');
				return;
			}
		}

		showAddDialog.value = false;
		editingConfig.value = null;
		resetConfigForm();
	} catch (error) {
		console.error('Save error:', error);
		ElMessage.error('Failed to save configuration');
	} finally {
		saving.value = false;
	}
};

const resetConfigForm = () => {
	Object.assign(configForm, {
		provider: '',
		modelName: '',
		apiKey: '',
		baseUrl: '',
		temperature: 0.7,
		maxTokens: 4096,
		isDefault: false,
		remarks: '',
	});
};

const cancelConfig = () => {
	showAddDialog.value = false;
	editingConfig.value = null;
	resetConfigForm();
	configFormRef.value?.resetFields();
};

const onProviderChange = () => {
	// Clear model name when switching providers and set default baseUrl
	configForm.modelName = '';
	const provider = providers.value.find((p) => p.name === configForm.provider);
	if (provider) {
		// Use default base URL if available
		if (provider.supportedModels && provider.supportedModels.length > 0) {
			configForm.modelName = provider.supportedModels[0];
		}
	}
};

const getProviderType = (provider: string) => {
	const types: Record<
		'OpenAI' | 'ZhipuAI' | 'Claude' | 'DeepSeek',
		'success' | 'warning' | 'info' | 'primary' | 'danger'
	> = {
		OpenAI: 'success',
		ZhipuAI: 'primary',
		Claude: 'warning',
		DeepSeek: 'info',
	};
	return types[provider] || 'default';
};

const formatDateTime = (dateTime: string | undefined) => {
	if (!dateTime) return 'Never';
	const date = new Date(dateTime);
	return date.toLocaleString('en-US', {
		month: '2-digit',
		day: '2-digit',
		year: 'numeric',
		hour: '2-digit',
		minute: '2-digit',
		second: '2-digit',
		hour12: true,
	});
};

// Lifecycle
onMounted(async () => {
	await Promise.all([loadConfigs(), loadProviders()]);
});
</script>

<style scoped lang="scss">
.ai-config-page {
	max-width: 1400px;
	margin: 0 auto;
}

.action-buttons {
	display: flex;
	gap: 8px;
	flex-wrap: nowrap;
	align-items: center;
	justify-content: flex-start;
}

.action-buttons .el-button {
	white-space: nowrap;
}

.el-slider {
	width: 100%;
}

/* 确保表格内容完整显示 */
:deep(.el-table) {
	width: 100%;
}

:deep(.el-card__body) {
	padding: 20px;
	overflow-x: auto;
}
</style>
