<template>
	<div class="python-config space-y-4">
		<!-- Python Script Editor -->
		<div>
			<CodeEditor
				v-if="isReady"
				ref="codeEditorRef"
				v-model="sourceCode"
				language="python"
				title="Python Script"
				description="Write your Python code here. Use variables from the panel above."
				height="400px"
			/>
		</div>

		<!-- Test Run Section -->
		<div class="test-section">
			<div class="flex items-center justify-between mb-3">
				<h5 class="font-medium text-gray-700 dark:text-gray-300">Test Script</h5>
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
import { computed, ref, nextTick, onMounted } from 'vue';
import CodeEditor from '@/components/codeEditor/index.vue';

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

// Computed two-way binding for config
const config = computed({
	get: () => props.modelValue || { sourceCode: '' },
	set: (newValue: any) => {
		emit('update:modelValue', { ...newValue });
	},
});

// Computed two-way binding for sourceCode
const sourceCode = computed({
	get: () => config.value.sourceCode || '',
	set: (newValue: string) => {
		config.value = { ...config.value, sourceCode: newValue };
	},
});

// Check if CodeEditor is loading
const isCodeEditorLoading = computed(() => {
	return codeEditorRef.value?.isLoading?.() || false;
});

// Initialize component
onMounted(() => {
	nextTick(() => {
		isReady.value = true;
	});
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
</style>
