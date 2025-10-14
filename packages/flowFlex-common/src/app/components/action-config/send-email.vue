<template>
	<div class="send-email-config" v-loading="loading">
		<el-form :model="config" label-width="120px" @submit.prevent>
			<el-form-item label="Subject">
				<el-input v-model="config.subject" placeholder="Email subject" />
			</el-form-item>
			<el-form-item label="Template">
				<el-input
					v-model="config.template"
					type="textarea"
					:rows="6"
					placeholder="Email template content"
				/>
			</el-form-item>
			<el-form-item label="Recipients">
				<el-select v-model="config.recipients" multiple placeholder="Select recipients">
					<el-option label="Customer" value="customer" />
					<el-option label="Sales Team" value="sales" />
					<el-option label="Support Team" value="support" />
					<el-option label="Admin" value="admin" />
				</el-select>
			</el-form-item>
			<el-form-item label="CC">
				<el-input v-model="config.cc" placeholder="CC recipients (comma separated)" />
			</el-form-item>
			<el-form-item label="BCC">
				<el-input v-model="config.bcc" placeholder="BCC recipients (comma separated)" />
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
		loading?: boolean;
	}>(),
	{
		modelValue: () => ({
			subject: '',
			template: '',
			recipients: [],
			cc: '',
			bcc: '',
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
	subject: '',
	template: '',
	recipients: [],
	cc: '',
	bcc: '',
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
.send-email-config {
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
			@apply rounded-xl;
			background-color: var(--el-fill-color-light);
			padding: 12px;
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
