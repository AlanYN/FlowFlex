<template>
	<el-drawer v-model="visible" :title="dialogTitle" :size="600" direction="rtl" @close="onCancel">
		<el-scrollbar class="action-config-scrollbar" max-height="calc(100vh - 140px)">
			<div class="action-config-container pr-4">
				<el-form
					ref="formRef"
					:model="formData"
					:rules="rules"
					label-position="top"
					label-width="120px"
				>
					<!-- Basic Info -->
					<el-form-item label="Action Name" prop="name">
						<el-input v-model="formData.name" placeholder="Enter action name" />
					</el-form-item>

					<el-form-item label="Description" prop="description">
						<el-input
							v-model="formData.description"
							type="textarea"
							:rows="3"
							placeholder="Enter action description"
						/>
					</el-form-item>

					<el-form-item label="Action Type" prop="type">
						<el-radio-group v-model="formData.type" @change="handleActionTypeChange">
							<el-radio
								v-for="type in actionTypes"
								:key="type.value"
								:value="type.value"
								class="action-type-option"
							>
								<div class="flex items-center space-x-3">
									<div
										class="flex items-center justify-center w-10 h-10 rounded-lg bg-gray-100 dark:bg-gray-700"
									>
										<el-icon class="text-primary-500" size="20">
											<component :is="type.icon" />
										</el-icon>
									</div>
									<div class="flex">
										<span class="font-medium text-gray-900 dark:text-white">
											{{ type.label }}
										</span>
									</div>
								</div>
							</el-radio>
						</el-radio-group>
					</el-form-item>

					<!-- Variables Panel (shared across all action types) -->
					<div v-if="formData.type" class="variables-section mb-6">
						<VariablesPanel :stage-id="stageId" :action-type="formData.type" />
					</div>

					<!-- Action Configuration -->
					<div v-if="formData.type" class="action-config-section">
						<!-- Python Script Configuration -->
						<PythonConfig
							v-if="formData.type === 'python'"
							v-model="formData.actionConfig"
							@test="onTest"
							:testing="testing"
							:test-result="testResult"
						/>

						<!-- HTTP API Configuration -->
						<HttpConfig
							v-else-if="formData.type === 'http'"
							v-model="formData.actionConfig"
							@test="onTest"
							:testing="testing"
							:test-result="testResult"
						/>
					</div>
				</el-form>
			</div>
		</el-scrollbar>

		<template #footer>
			<div class="dialog-footer">
				<el-button @click="onCancel">Cancel</el-button>
				<el-button type="primary" @click="onSave" :loading="saving">
					{{ isEditing ? 'Update' : 'Add' }} Action
				</el-button>
			</div>
		</template>
	</el-drawer>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { Operation, Connection } from '@element-plus/icons-vue';
import PythonConfig from './PythonConfig.vue';
import HttpConfig from './HttpConfig.vue';
import VariablesPanel from './VariablesPanel.vue';

import { addAction, ActionType } from '@/apis/action';
import { TriggerTypeEnum } from '@/enums/appEnum';
import { ActionItem } from '#/action';

interface Props {
	modelValue?: boolean;
	action?: ActionItem | null;
	isEditing?: boolean;
	stageId?: string;
	workflowId?: string;
}

const props = withDefaults(defineProps<Props>(), {
	modelValue: false,
	action: null,
	isEditing: false,
	stageId: '',
	workflowId: '',
});

const emit = defineEmits<{
	'update:modelValue': [value: boolean];
	saveSuccess;
	cancel: [];
}>();

// Form data
const formRef = ref();
const saving = ref(false);
const testing = ref(false);
const testResult = ref('');

const formData = reactive<ActionItem>({
	id: '',
	name: '',
	type: 'python',
	description: '',
	actionConfig: {},
});

// Computed
const visible = computed({
	get: () => props.modelValue,
	set: (value) => emit('update:modelValue', value),
});

const dialogTitle = computed(() => {
	return props.isEditing ? 'Edit Action' : 'Add New Action';
});

// Action Types
const actionTypes = [
	{
		label: 'Python Script',
		value: 'python',
		icon: Operation,
		description: 'Execute custom Python code when stage completes',
	},
	{
		label: 'HTTP API',
		value: 'http',
		icon: Connection,
		description: 'Send HTTP request to external API endpoint',
	},
];

// Form Rules
const rules = {
	name: [{ required: true, message: 'Please enter action name', trigger: 'blur' }],
	type: [{ required: true, message: 'Please select action type', trigger: 'change' }],
};

const getDefaultConfig = (type: string) => {
	if (type === 'python') {
		return {
			sourceCode: `def main(context):
    """
    Main function executed when the stage is completed.
    The context parameter contains all event and questionnaire data.
    """
    # Access event data
    event = context.get('event', {})
    print(f"Processing event: {event.get('eventId', '')}")
    print(f"Onboarding ID: {event.get('onboardingId', '')}")
    print(f"Workflow: {event.get('workflowName', '')}")
    print(f"Completed Stage: {event.get('completedStageName', '')}")
    print(f"Next Stage: {event.get('nextStageName', '')}")
    print(f"Completion Rate: {event.get('completionRate', 0)}%")
    print(f"Final Stage: {event.get('isFinalStage', False)}")
    
    # Access business context
    business_context = event.get('businessContext', {})
    completion_method = business_context.get('CompletionMethod', '')
    completion_notes = business_context.get('CompletionNotes', '')
    print(f"Completion Method: {completion_method}")
    print(f"Notes: {completion_notes}")
    
    # Process questionnaire responses
    questionnaire_data = context.get('questionnaire_responses', {})
    responses = questionnaire_data.get('responses', [])
    print(f"Processing {len(responses)} questionnaire responses")
    
    for response in responses:
        question = response.get('question', '')
        answer = response.get('answer', '')
        response_type = response.get('type', '')
        print(f"Q ({response_type}): {question}")
        print(f"A: {answer}")
    
    # Your custom logic here
    return {
        "status": "success", 
        "message": "Action completed successfully",
        "processed_event": event.get('eventId', ''),
        "stage_completed": event.get('completedStageName', ''),
        "responses_count": len(responses)
    }`,
		};
	} else if (type === 'http') {
		return {
			url: '',
			method: 'GET',
			headers: {
				'Content-Type': 'application/json',
			},
			body: '',
			timeout: 30,
			followRedirects: true,
		};
	}
	return {};
};

// Methods
const resetForm = () => {
	formData.id = '';
	formData.name = '';
	formData.type = 'python';
	formData.description = '';
	formData.actionConfig = getDefaultConfig('python');
};

// Watch for action prop changes
watch(
	() => props.action,
	(newAction) => {
		if (newAction) {
			Object.assign(formData, { ...newAction });
		} else {
			resetForm();
		}
	},
	{ immediate: true, deep: true }
);

const handleActionTypeChange = (type: string) => {
	formData.actionConfig = getDefaultConfig(type);
};

const onTest = async () => {
	testing.value = true;
	try {
		await new Promise((resolve) => setTimeout(resolve, 2000));

		if (formData.type === 'python') {
			testResult.value = `Test execution completed successfully!

Output:
Processing onboarding: John Doe
Stage completed: Document Collection
Question: What is your company name?
Answer: Example Corp
{'status': 'success', 'message': 'Action completed successfully'}

Execution time: 0.123 seconds`;
		} else if (formData.type === 'http') {
			testResult.value = `HTTP API test completed successfully!

Request:
${formData.actionConfig.method} ${formData.actionConfig.url}
Headers: ${formData.actionConfig.headers}

Response:
Status: 200 OK
Body: {"status": "success", "message": "API call successful"}

Response time: 234ms`;
		}

		ElMessage.success('Test completed successfully');
	} catch (error) {
		testResult.value = `Error: ${error}`;
		ElMessage.error('Test failed');
	} finally {
		testing.value = false;
	}
};

const onSave = async () => {
	if (!formRef.value) return;

	try {
		await formRef.value.validate();
		saving.value = true;

		// Validate based on action type
		if (formData.type === 'python' && !formData.actionConfig.sourceCode) {
			ElMessage.error('Please enter Python script code');
			return;
		}

		if (formData.type === 'http' && !formData.actionConfig.url) {
			ElMessage.error('Please enter HTTP API URL');
			return;
		}

		// 根据 action type 准备不同的 actionConfig
		let cleanActionConfig: any = {};

		if (formData.type === 'python') {
			// Python 类型只需要 sourceCode
			cleanActionConfig = {
				sourceCode: formData.actionConfig.sourceCode,
			};
		} else if (formData.type === 'http') {
			// HTTP 类型需要符合 HttpApiConfigDto 的字段
			cleanActionConfig = {
				url: formData.actionConfig.url || '',
				method: formData.actionConfig.method || 'GET',
				headers: formData.actionConfig.headers || {},
				body: formData.actionConfig.body || '',
				timeout: formData.actionConfig.timeout || 30,
				followRedirects: formData.actionConfig.followRedirects !== false, // 默认为 true
			};
		}

		const res = await addAction({
			...formData,
			actionConfig: JSON.stringify(cleanActionConfig),
			workflowId: props?.workflowId || '',
			actionType: formData.type === 'python' ? ActionType.PYTHON_SCRIPT : ActionType.HTTP_API,
			triggerSourceId: props.stageId,
			triggerType: TriggerTypeEnum.Stage,
		});
		console.log('res:', res);
		if (res.code == '200') {
			ElMessage.success('Action added successfully');
			emit('saveSuccess');
			visible.value = false;
		} else {
			res?.msg && ElMessage.error(res?.msg);
		}
	} finally {
		saving.value = false;
	}
};

const onCancel = () => {
	visible.value = false;
	resetForm();
	emit('cancel');
};
</script>

<style scoped lang="scss">
.action-config-scrollbar {
	@apply h-full;

	:deep(.el-scrollbar__wrap) {
		overflow-x: hidden;
	}

	:deep(.el-scrollbar__bar.is-vertical > div) {
		@apply bg-gray-300 dark:bg-gray-600 opacity-80;
		border-radius: 4px;
		width: 6px;
	}

	:deep(.el-scrollbar__bar.is-vertical) {
		@apply opacity-80;
		width: 8px;
		right: 2px;
	}

	:deep(.el-scrollbar__bar.is-vertical:hover > div) {
		@apply bg-gray-400 dark:bg-gray-500;
	}
}

.action-config-container {
	@apply bg-white dark:bg-gray-800 min-h-full;
}

.section-header {
	@apply border-b border-gray-200 dark:border-gray-700 pb-3 mb-4;
}

.variables-section {
	@apply mb-6;
}

.action-config-section {
	@apply space-y-4;
}

// 抽屉footer样式
.dialog-footer {
	@apply flex justify-end space-x-3 px-6 py-4 border-t border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800;
	position: sticky;
	bottom: 0;
	margin-top: auto;
}

// Variables Panel 在抽屉中的样式调整
.variables-section {
	:deep(.variables-panel) {
		@apply border border-gray-200 dark:border-gray-700 rounded-lg shadow-sm;
	}

	:deep(.variables-header) {
		@apply bg-gray-50 dark:bg-gray-800;
	}
}
</style>
