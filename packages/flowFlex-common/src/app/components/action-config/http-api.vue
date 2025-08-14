<template>
	<div class="http-api-config">
		<el-form :model="config" label-width="120px">
			<el-form-item label="URL">
				<el-input v-model="config.url" placeholder="URL" />
			</el-form-item>
			<el-form-item label="Method">
				<el-select v-model="config.method">
					<el-option label="POST" value="POST" />
					<el-option label="GET" value="GET" />
					<el-option label="PUT" value="PUT" />
					<el-option label="DELETE" value="DELETE" />
					<el-option label="PATCH" value="PATCH" />
				</el-select>
			</el-form-item>
			<el-form-item label="Headers">
				<el-input
					v-model="config.headers"
					type="textarea"
					:rows="3"
					placeholder="JSON format headers"
				/>
			</el-form-item>
			<el-form-item label="Timeout (seconds)">
				<el-input-number v-model="config.timeout" :min="1" :max="300" />
			</el-form-item>
		</el-form>

		<div v-if="showTestButton" class="test-run-button">
			<el-button type="success" size="small" @click="handleTestRun" :loading="props.testing">
				Test Run
			</el-button>
		</div>

		<!-- Test Result Dialog -->
		<el-dialog v-model="showTestResultLocal" title="Test Run Result" width="600px">
			<div class="test-result-container">
				<pre class="test-output">{{ props.testResult }}</pre>
			</div>
			<template #footer>
				<el-button @click="closeTestResult">Close</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { reactive, watch, ref } from 'vue';
import { type TestResult } from '../../apis/action';

// Props
const props = withDefaults(
	defineProps<{
		modelValue?: any;
		showTestButton?: boolean;
		actionId?: string;
		testResult?: string;
		showTestResult?: boolean;
		testing?: boolean;
	}>(),
	{
		modelValue: () => ({
			url: '',
			method: 'POST',
			headers: '{"Content-Type": "application/json"}',
			timeout: 30,
		}),
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
const showTestResultLocal = ref(false);

// Watch for props changes to sync local state
watch(
	() => props.showTestResult,
	(newValue) => {
		showTestResultLocal.value = newValue;
	},
	{ immediate: true }
);

// Close test result dialog
const closeTestResult = () => {
	showTestResultLocal.value = false;
	emit('update:showTestResult', false);
};

// Local config
const config = reactive({
	url: '',
	method: 'POST',
	headers: '{"Content-Type": "application/json"}',
	timeout: 30,
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
</script>

<style scoped lang="scss">
.http-api-config {
	// Add any specific styles here

	.test-run-button {
		display: flex;
		justify-content: flex-end;
		margin-top: 12px;
	}

	.test-result-container {
		max-height: 400px;
		overflow-y: auto;

		.test-output {
			background-color: #f5f5f5;
			padding: 12px;
			border-radius: 4px;
			font-family: 'Courier New', monospace;
			font-size: 14px;
			line-height: 1.4;
			white-space: pre-wrap;
			word-wrap: break-word;
			margin: 0;
		}
	}
}
</style>
