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
		<AICodeGeneratorDialog
			v-model="showAICodeGeneratorDialog"
			v-model:selectedModelId="selectedModelId"
			v-model:aiInstructions="aiInstructions"
			v-model:generatedCode="generatedCode"
			:modelOptions="modelOptions"
			:loadingModels="loadingModels"
			:generating="generating"
			@generate-code="generateCode"
			@apply-code="applyGeneratedCode"
			@clear-code="clearGeneratedCode"
			@model-change="onModelChange"
		/>
	</div>
</template>

<script setup lang="ts">
import { reactive, watch, computed } from 'vue';
import { Star } from '@element-plus/icons-vue';
import CodeEditor from '@/components/codeEditor/index.vue';
import { AICodeGeneratorDialog } from '@/components/action-config';
import { type TestResult } from '@/apis/action';
import { useAICodeGenerator } from '@/hooks/useAICodeGenerator';

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

// Use AI Code Generator composable
const {
	showAICodeGeneratorDialog,
	aiInstructions,
	generatedCode,
	generating,
	selectedModelId,
	modelOptions,
	loadingModels,
	showAICodeGenerator,
	generateCode,
	createApplyGeneratedCode,
	clearGeneratedCode,
	onModelChange,
	refreshModels,
} = useAICodeGenerator();

// Close test result dialog
const closeTestResult = () => {
	showTestResultLocal.value = false;
	emit('update:showTestResult', false);
};

// Create apply function for AI generator
const applyGeneratedCode = createApplyGeneratedCode((code: string) => {
	config.sourceCode = code;
});

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
		@apply rounded-xl;
		height: 400px;
		overflow-y: auto;
		background-color: #f5f5f5;
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
</style>
