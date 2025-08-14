<template>
	<div class="python-script-config">
		<el-form :model="config" label-width="120px">
			<el-form-item label="Python Script">
				<CodeEditor
					v-model="config.sourceCode"
					language="python"
					title=""
					description=""
					height="400px"
					:read-only="readOnly"
				/>
				<div v-if="showTestButton" class="test-run-button w-full">
					<div class="button-group">
						<el-button
							type="primary"
							size="small"
							@click="showAICodeGenerator"
							:icon="Star"
						>
							AI Generate
						</el-button>
						<el-button
							type="success"
							size="small"
							@click="handleTestRun"
							:loading="props.testing"
						>
							Test Run
						</el-button>
					</div>
				</div>
			</el-form-item>
		</el-form>

		<!-- Test Result Dialog -->
		<el-dialog v-model="showTestResultLocal" title="Test Run Result" width="600px">
			<div class="test-result-container" v-loading="loading">
				<pre class="test-output">{{ props.testResult }}</pre>
			</div>
			<template #footer>
				<el-button @click="closeTestResult">Close</el-button>
			</template>
		</el-dialog>

		<!-- AI Code Generator Dialog -->
		<el-dialog
			v-model="showAICodeGeneratorDialog"
			title="Code Generator"
			width="800px"
			class="ai-code-generator-dialog"
		>
			<div class="ai-generator-container">
				<div class="ai-generator-description">
					The code generator uses the configured model to generate high-quality code based
					on your instructions. Please provide clear and detailed descriptions.
				</div>

				<!-- Model Selection Section -->
				<div class="model-selection-section">
					<div class="model-info">
						<el-icon><Star /></el-icon>
						<span>Model:</span>
						<el-select
							v-model="selectedModelId"
							placeholder="Select AI Model"
							clearable
							style="width: 260px"
							@change="onModelChange"
							:disabled="loadingModels"
						>
							<el-option
								v-for="model in modelOptions"
								:key="model.id"
								:label="getModelDisplayName(model)"
								:value="String(model.id)"
							/>
						</el-select>
					</div>
				</div>

				<div class="ai-generator-content">
					<!-- Left input section -->
					<div class="input-section">
						<div class="input-label">Instructions</div>
						<el-input
							v-model="aiInstructions"
							type="textarea"
							:rows="12"
							placeholder="Please enter a detailed description of the code you want to generate."
							class="instruction-input"
						/>
						<div class="generate-button-container">
							<el-button
								type="primary"
								@click="generateCode"
								:loading="generating"
								:icon="Star"
								:disabled="!selectedModelId"
							>
								Generate
							</el-button>
						</div>
					</div>

					<!-- Right code preview section -->
					<div class="preview-section">
						<div class="preview-label">Code Preview</div>
						<div class="code-preview-container" v-loading="generating">
							<div v-if="!generatedCode && !generating" class="empty-preview">
								<el-icon size="48" color="#909399"><Star /></el-icon>
								<p>
									Describe your use case on the left, and the code preview will be
									displayed here.
								</p>
							</div>
							<pre v-else-if="generatedCode" class="generated-code">{{
								generatedCode
							}}</pre>
						</div>
						<div v-if="generatedCode" class="preview-actions">
							<el-button size="small" @click="applyGeneratedCode">
								Apply Code
							</el-button>
							<el-button size="small" @click="clearGeneratedCode">Clear</el-button>
						</div>
					</div>
				</div>
			</div>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { reactive, watch, computed, ref } from 'vue';
import { Star } from '@element-plus/icons-vue';
import CodeEditor from '@/components/codeEditor/index.vue';
import { type TestResult } from '../../apis/action';
import { ElMessage } from 'element-plus';
import { getUserAIModels, getDefaultAIModel } from '@/apis/ai/config';
import { sendAIChatMessage } from '@/apis/ai/workflow';

// Props
const props = withDefaults(
	defineProps<{
		modelValue?: any;
		readOnly?: boolean;
		showTestButton?: boolean;
		actionId?: string;
		testResult?: string;
		showTestResult?: boolean;
		testing?: boolean;
		loading?: boolean;
	}>(),
	{
		modelValue: () => ({
			sourceCode: '',
		}),
		readOnly: false,
		showTestButton: false,
		actionId: '',
		testResult: '',
		showTestResult: false,
		testing: false,
	}
);

// Emits
const emit = defineEmits<{
	'update:modelValue': [value: any];
	test: [result: TestResult | null];
	'update:showTestResult': [value: boolean];
}>();

// Internal state
const showTestResultLocal = computed({
	get: () => props.showTestResult,
	set: (value) => emit('update:showTestResult', value),
});

// AI Code Generator state
const showAICodeGeneratorDialog = ref(false);
const aiInstructions = ref('');
const generatedCode = ref('');
const generating = ref(false);

// AI Model state
const selectedModelId = ref('');
const modelOptions = ref<any[]>([]);
const loadingModels = ref(false);

// Close test result dialog
const closeTestResult = () => {
	showTestResultLocal.value = false;
	emit('update:showTestResult', false);
};

// Show AI Code Generator dialog
const showAICodeGenerator = () => {
	showAICodeGeneratorDialog.value = true;
	aiInstructions.value = '';
	generatedCode.value = '';
};

// Generate code using AI
const generateCode = async () => {
	if (!aiInstructions.value.trim()) {
		ElMessage.warning('Please enter code generation instructions');
		return;
	}

	if (!selectedModelId.value) {
		ElMessage.warning('Please select an AI model to generate code.');
		return;
	}

	generating.value = true;
	try {
		// Get selected model info
		const selectedModel = modelOptions.value.find(
			(model) => String(model.id) === selectedModelId.value
		);
		if (!selectedModel) {
			throw new Error('Selected model not found');
		}

		// Prepare request payload using existing API format
		const requestPayload = {
			messages: [
				{
					role: 'user' as const,
					content: aiInstructions.value,
					timestamp: new Date().toISOString(),
				},
			],
			mode: 'generate_code' as const,
			modelId: selectedModelId.value,
			modelProvider: selectedModel.provider,
			modelName: selectedModel.name,
		};

		console.log('Sending AI code generation request:', requestPayload);

		// Use existing API function
		const response = await sendAIChatMessage(requestPayload);
		console.log('AI code generation response:', response);

		// Handle response - use type assertion since actual response might differ from type definition
		const responseAny = response as any;
		let content = '';

		if (responseAny.data?.success && responseAny.data?.response?.content) {
			// If response has nested data structure
			content = responseAny.data.response.content;
		} else if (responseAny.success && responseAny.response?.content) {
			// If response is already unwrapped
			content = responseAny.response.content;
		} else {
			throw new Error(
				responseAny.data?.message || responseAny.message || 'Code generation failed'
			);
		}

		if (content) {
			generatedCode.value = content;
			ElMessage.success('Code generated successfully!');
		} else {
			throw new Error('No content received from AI');
		}
	} catch (error) {
		console.error('AI code generation error:', error);
		ElMessage.error(`Code generation failed: ${error.message}`);
	} finally {
		generating.value = false;
	}
};

// Apply generated code to editor
const applyGeneratedCode = () => {
	if (generatedCode.value) {
		config.sourceCode = generatedCode.value;
		showAICodeGeneratorDialog.value = false;
		ElMessage.success('Code has been applied to the editor');
	}
};

// Clear generated code
const clearGeneratedCode = () => {
	generatedCode.value = '';
	aiInstructions.value = '';
};

// Local config
const config = reactive({
	sourceCode: '',
});

// Watch for prop changes
watch(
	() => props.modelValue,
	(newValue) => {
		if (newValue) {
			Object.assign(config, newValue);
		}
	},
	{ immediate: true, deep: true }
);

// Watch for local changes
watch(
	config,
	(newValue) => {
		emit('update:modelValue', { ...newValue });
	},
	{ deep: true }
);

// Test run method
const handleTestRun = async () => {
	// Trigger external event, let parent component control test logic
	emit('test', null);
};

// Expose methods for parent component
defineExpose({
	getCurrentSourceCode: () => config.sourceCode,
	getCurrentConfig: () => ({ ...config }),
});

// Model selection change handler
const onModelChange = (value: string) => {
	selectedModelId.value = value;
};

// Get model display name
const getModelDisplayName = (model: any) => {
	return `${model.name} (${model.provider})`;
};

// Refresh models
const refreshModels = async () => {
	loadingModels.value = true;
	try {
		// Get all available models
		const modelsResponse = await getUserAIModels();

		if (modelsResponse.success && modelsResponse.data) {
			modelOptions.value = modelsResponse.data.map((model: any) => ({
				id: model.id,
				name: model.modelName,
				provider: model.provider,
				isDefault: model.isDefault,
				isAvailable: model.isAvailable,
			}));
		} else {
			modelOptions.value = [];
		}

		// Get default model configuration
		try {
			const defaultResponse = await getDefaultAIModel();
			if (defaultResponse.success && defaultResponse.data) {
				// Auto-select default model if available
				const defaultModel = modelOptions.value.find(
					(model: any) => model.id === defaultResponse.data.id
				);
				if (defaultModel) {
					selectedModelId.value = String(defaultModel.id);
				} else if (modelOptions.value.length > 0) {
					selectedModelId.value = String(modelOptions.value[0].id);
				}
			} else if (modelOptions.value.length > 0) {
				// Fallback to first available model if no default
				selectedModelId.value = String(modelOptions.value[0].id);
			}
		} catch (defaultError) {
			console.warn('Failed to get default model, using first available:', defaultError);
			if (modelOptions.value.length > 0) {
				selectedModelId.value = String(modelOptions.value[0].id);
			}
		}
	} catch (error) {
		ElMessage.error('Failed to refresh models');
		console.error('Refresh models error:', error);
		modelOptions.value = [];
		selectedModelId.value = '';
	} finally {
		loadingModels.value = false;
	}
};

// Initial refresh on component mount
refreshModels();
</script>

<style scoped lang="scss">
.python-script-config {
	.test-run-button {
		display: flex;
		justify-content: flex-end;
		margin-top: 12px;

		.button-group {
			display: flex;
			gap: 8px;
		}
	}

	.test-result-container {
		height: 400px;
		overflow-y: auto;
		background-color: #f5f5f5;
		border-radius: 4px;
		padding: 12px;

		.test-output {
			font-size: 14px;
			line-height: 1.4;
			white-space: pre-wrap;
			word-wrap: break-word;
			margin: 0;
		}
	}
}

.ai-code-generator-dialog {
	.ai-generator-container {
		.ai-generator-description {
			color: #606266;
			font-size: 14px;
			margin-bottom: 16px;
			line-height: 1.5;
		}

		.model-selection-section {
			margin-bottom: 20px;

			.model-info {
				display: flex;
				align-items: center;
				gap: 8px;
				padding: 12px 16px;
				background-color: #f5f7fa;
				border-radius: 6px;
				font-size: 14px;
				color: #606266;
				border: 1px solid #e4e7ed;
			}
		}

		.ai-generator-content {
			display: flex;
			gap: 20px;
			height: 500px;

			.input-section {
				flex: 1;
				display: flex;
				flex-direction: column;

				.input-label {
					font-weight: 500;
					margin-bottom: 8px;
					color: #303133;
				}

				.instruction-input {
					flex: 1;

					:deep(.el-textarea__inner) {
						resize: none;
						font-family: 'Monaco', 'Menlo', 'Ubuntu Mono', monospace;
						font-size: 13px;
					}
				}

				.generate-button-container {
					display: flex;
					justify-content: flex-end;
					margin-top: 12px;
				}
			}

			.preview-section {
				flex: 1;
				display: flex;
				flex-direction: column;

				.preview-label {
					font-weight: 500;
					margin-bottom: 8px;
					color: #303133;
				}

				.code-preview-container {
					flex: 1;
					background-color: #f8f9fa;
					border: 1px solid #e4e7ed;
					border-radius: 4px;
					padding: 12px;
					overflow-y: auto;
					position: relative;

					.empty-preview {
						display: flex;
						flex-direction: column;
						align-items: center;
						justify-content: center;
						height: 100%;
						color: #909399;
						text-align: center;

						p {
							margin-top: 12px;
							font-size: 14px;
							line-height: 1.5;
						}
					}

					.generated-code {
						font-family: 'Monaco', 'Menlo', 'Ubuntu Mono', monospace;
						font-size: 13px;
						line-height: 1.4;
						white-space: pre-wrap;
						word-wrap: break-word;
						margin: 0;
						color: #303133;
					}
				}

				.preview-actions {
					display: flex;
					gap: 8px;
					margin-top: 12px;
					justify-content: flex-end;
				}
			}
		}
	}
}
</style>
