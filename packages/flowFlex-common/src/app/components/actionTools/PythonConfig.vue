<template>
	<div>
		<!-- Context Parameter Documentation -->
		<div class="context-doc-section">
			<div class="context-structure">
				<div
					class="bg-gray-50 dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700"
				>
					<pre
						class="text-xs text-gray-700 dark:text-gray-300 whitespace-pre-wrap font-mono"
						>{{ contextStructure }}</pre
					>
				</div>
			</div>
		</div>

		<!-- Python Script Editor -->
		<div>
			<CodeEditor
				v-if="isReady"
				ref="codeEditorRef"
				v-model="sourceCode"
				language="python"
				title="Python Script"
				description="Write your Python code here. The context parameter structure is shown above."
				height="400px"
			/>
		</div>

		<!-- Test Run Section -->
		<div class="test-section">
			<div class="flex items-center justify-between mb-3">
				<h5 class="font-medium text-gray-700 dark:text-gray-300">Test Script</h5>
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
						@click="handleTest"
						:loading="testing"
						:disabled="!sourceCode || isCodeEditorLoading"
					>
						Test Run
					</el-button>
				</div>
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
import { computed, ref, nextTick, onMounted } from 'vue';
import { Star } from '@element-plus/icons-vue';
import CodeEditor from '@/components/codeEditor/index.vue';
import { AICodeGeneratorDialog } from '@/components/action-config';
import { useAICodeGenerator } from '@/hooks/useAICodeGenerator';

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

// Control CodeEditor rendering
const isReady = ref(false);
const codeEditorRef = ref<any>(null);

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

// Computed two-way binding for sourceCode
const sourceCode = computed({
	get: () => props.modelValue?.sourceCode || '',
	set: (newValue: string) => {
		emit('update:modelValue', { ...props.modelValue, sourceCode: newValue });
	},
});

// Check if CodeEditor is loading
const isCodeEditorLoading = computed(() => {
	return codeEditorRef.value?.isLoading?.() || false;
});

// Context parameter structure documentation
const contextStructure = computed(() => {
	return `context = {
    "event": {
        "eventId": "string",
        "timestamp": "string", 
        "tenantId": "string",
        "onboardingId": "string",
        "leadId": "string",
        "workflowId": "string",
        "workflowName": "string",
        "completionRate": "number",
        "isFinalStage": "boolean",
        "responsibleTeam": "string",
        "assigneeId": "string",
        "assigneeName": "string",
        "completedStageId": "string",
        "completedStageName": "string",
        "stageCategory": "string",
        "nextStageId": "string",
        "nextStageName": "string",
        "businessContext": {
            "CompletionMethod": "string",
            "AutoMoveToNext": "boolean",
            "CompletionNotes": "string"
        }
    },
    "questionnaire_responses": {
        "questionnaireId": "string",
        "stageId": "string", 
        "responses": [
            {
                "questionId": "string",
                "question": "string",
                "answer": "string",
                "type": "string",
                "responseText": "string",
                "lastModifiedAt": "string",
                "lastModifiedBy": "string"
            }
        ]
    }
}`;
});

// Create apply function for AI generator
const applyGeneratedCode = createApplyGeneratedCode((code: string) => {
	sourceCode.value = code;
});

// Initialize component
onMounted(() => {
	nextTick(() => {
		isReady.value = true;
	});
	// Initial refresh on component mount
	refreshModels();
});

// Methods
const handleTest = () => {
	emit('test');
};
</script>

<style scoped lang="scss">
.test-section {
	@apply border-t border-gray-200 dark:border-gray-700 pt-4;
}

.test-result {
	@apply mt-3;

	pre {
		@apply max-h-40 overflow-y-auto;
	}
}

.context-doc-section {
	@apply border-b border-gray-200 dark:border-gray-700 pb-4;
}

.context-structure {
	@apply mt-3;

	pre {
		@apply max-h-60 overflow-y-auto text-xs leading-relaxed;
	}
}

.button-group {
	@apply flex gap-2;
}
</style>
