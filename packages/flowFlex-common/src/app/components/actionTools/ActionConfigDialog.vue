<template>
	<el-drawer v-model="visible" :title="dialogTitle" size="80%" direction="rtl" @close="onCancel">
		<el-scrollbar class="action-config-scrollbar">
			<!-- Variables Panel (shared across all action types) -->
			<div class="flex gap-4 w-full h-full min-h-0">
				<div class="variables-section mt-6 flex-1 min-w-0 min-h-0 flex flex-col">
					<el-scrollbar ref="scrollbarRefLeft" class="h-full">
						<VariablesPanel :stage-id="stageId" :action-type="formData.type" />
					</el-scrollbar>
				</div>
				<div
					class="action-config-container pr-4 flex-1 min-w-0 min-h-0 flex flex-col"
					v-loading="loading"
				>
					<el-scrollbar ref="scrollbarRefRight" class="h-full">
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
								<el-radio-group
									v-model="formData.type"
									@change="handleActionTypeChange"
								>
									<el-radio
										v-for="type in actionTypes"
										:key="type.value"
										:value="type.value"
										class="action-type-option"
										:disabled="isEditing"
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
												<span
													class="font-medium text-gray-900 dark:text-white"
												>
													{{ type.label }}
												</span>
											</div>
										</div>
									</el-radio>
								</el-radio-group>
							</el-form-item>

							<!-- Action Configuration -->
							<div v-if="formData.type" class="action-config-section">
								<!-- Python Script Configuration -->
								<PythonConfig
									v-if="formData.type === 'python'"
									v-model="formData.actionConfig"
									@test="onTest"
									:testing="testing"
									:test-result="testResult"
									ref="pythonConfigRef"
									:id-editing="isEditing"
								/>

								<!-- HTTP API Configuration -->
								<HttpConfig
									v-else-if="formData.type === 'http'"
									v-model="formData.actionConfig"
									@test="onTest"
									:testing="testing"
									:test-result="testResult"
									ref="httpConfigRef"
									:id-editing="isEditing"
								/>
							</div>
						</el-form>
					</el-scrollbar>
				</div>
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
import { useAdaptiveScrollbar } from '@/hooks/useAdaptiveScrollbar';

import { addAction, ActionType, updateAction, testRunActionNoId } from '@/apis/action';
import { TriggerTypeEnum } from '@/enums/appEnum';
import { ActionItem } from '#/action';

const { scrollbarRef: scrollbarRefLeft } = useAdaptiveScrollbar(110);

const { scrollbarRef: scrollbarRefRight } = useAdaptiveScrollbar(110);

interface Props {
	modelValue?: boolean;
	action?: ActionItem | null;
	isEditing?: boolean;
	stageId?: string;
	workflowId?: string;
	loading?: boolean;
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
const testResult = ref(null);
const pythonConfigRef = ref(); // For getting Python config component reference
const httpConfigRef = ref(); // For getting HTTP config component reference

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
			sourceCode: `from datetime import datetime

def main():
    """
    Main function executed when the stage is completed.
    Access trigger context data through global variables or specific parameters.
    """
    # Example: Access data from trigger context
    # The actual parameter names depend on your backend implementation
    
    try:
        # Print available variables for debugging
        print("=== Action Execution Started ===")
        
        # Access event data (adjust parameter names as needed)
        # Common patterns:
        # - event_data = triggerContext.get('event', {})
        # - onboarding_data = triggerContext.get('onboarding', {})
        # - questionnaire_data = triggerContext.get('questionnaire', {})
        
        print("Processing action execution...")
        
        # Your custom logic here
        # Example operations:
        # 1. Process event data
        # 2. Send notifications
        # 3. Update external systems
        # 4. Log information
        
        print("Action completed successfully")
        
        return {
            "status": "success", 
            "message": "Action completed successfully",
            "timestamp": str(datetime.now().isoformat())
        }
        
    except Exception as e:
        print(f"Error in action execution: {str(e)}")
        return {
            "status": "error",
            "message": f"Action failed: {str(e)}"
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
	testResult.value = null;
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

// Handle test result - 参考 detail.vue 的 handleTestResult 逻辑
const onTest = async (result: any) => {
	// Force get current config values from components

	try {
		testing.value = true;
		testResult.value = null;
		// Execute test
		const testOutput = await testRunActionNoId({
			actionType: formData.type === 'python' ? ActionType.PYTHON_SCRIPT : ActionType.HTTP_API,
			actionConfig: JSON.stringify(formData.actionConfig),
		});

		if (testOutput.code == '200') {
			testResult.value = testOutput.data;
		}
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
				...formData.actionConfig,
				url: formData.actionConfig.url || '',
				method: formData.actionConfig.method || 'GET',
				headers: formData.actionConfig.headers || {},
				params: formData.actionConfig.params || {},
				body: formData.actionConfig.body || '',
				timeout: formData.actionConfig.timeout || 30,
				followRedirects: formData.actionConfig.followRedirects !== false, // 默认为 true
			};
		}

		const res: any = formData.id
			? await updateAction(formData.id, {
					...formData,
					actionConfig: JSON.stringify(cleanActionConfig),
					workflowId: props?.workflowId || '',
					actionType:
						formData.type === 'python' ? ActionType.PYTHON_SCRIPT : ActionType.HTTP_API,
					triggerSourceId: props.stageId,
					triggerType: TriggerTypeEnum.Stage,
			  })
			: await addAction({
					...formData,
					actionConfig: JSON.stringify(cleanActionConfig),
					workflowId: props?.workflowId || '',
					actionType:
						formData.type === 'python' ? ActionType.PYTHON_SCRIPT : ActionType.HTTP_API,
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
	max-height: calc(100vh - 120px);

	:deep(.el-scrollbar__view) {
		display: flex;
		flex-direction: column;
		min-height: 0;
	}

	:deep(.el-scrollbar__wrap) {
		/* avoid nested scrollbars fighting */
		max-height: 100%;
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
