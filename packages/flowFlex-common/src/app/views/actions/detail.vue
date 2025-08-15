<template>
	<div class="action-detail-container rounded-md">
		<!-- 页面头部 -->
		<div class="page-header rounded-md">
			<div class="header-content">
				<div class="header-left">
					<el-button type="primary" link class="back-button" @click="goBack" size="small">
						<el-icon class="back-icon">
							<ArrowLeft />
						</el-icon>
					</el-button>
					<div class="header-info">
						<h1 class="page-title">Action Detail</h1>
						<p class="page-description">
							View and edit action configuration and assignments
						</p>
					</div>
				</div>
				<div class="header-actions">
					<el-button
						type="primary"
						class="save-button"
						@click="handleSave()"
						:loading="saving"
						:icon="Check"
					>
						Save Changes
					</el-button>
					<el-button
						type="danger"
						class="delete-button"
						@click="handleDelete"
						:icon="Delete"
					>
						Delete Action
					</el-button>
				</div>
			</div>
		</div>

		<!-- 主要内容区域 -->
		<div class="main-content" v-loading="loading">
			<el-row :gutter="24">
				<!-- 左侧：基本信息 -->
				<el-col :span="16">
					<el-card class="info-card">
						<template #header>
							<div class="card-header">
								<span>Basic Information</span>
							</div>
						</template>

						<el-form
							:model="actionForm"
							:rules="rules"
							ref="formRef"
							label-width="120px"
						>
							<el-row :gutter="20">
								<el-col :span="12">
									<el-form-item label="Action ID" prop="actionId">
										<el-input v-model="actionForm.actionId" disabled />
									</el-form-item>
								</el-col>
								<el-col :span="12">
									<el-form-item label="Action Name" prop="actionName">
										<el-input
											v-model="actionForm.actionName"
											placeholder="Enter action name"
										/>
									</el-form-item>
								</el-col>
							</el-row>

							<el-row :gutter="20">
								<el-col :span="12">
									<el-form-item label="Type" prop="type">
										<el-input v-model="actionForm.type" disabled />
									</el-form-item>
								</el-col>
							</el-row>

							<el-form-item label="Description" prop="description">
								<el-input
									v-model="actionForm.description"
									type="textarea"
									:rows="4"
									placeholder="Enter action description"
								/>
							</el-form-item>
						</el-form>
					</el-card>

					<!-- 配置信息 -->
					<el-card class="config-card">
						<template #header>
							<div class="card-header">
								<span>Configuration</span>
							</div>
						</template>

						<div v-if="actionForm.type === 'Send Email'" class="email-config">
							<SendEmailConfig
								v-model="emailConfig"
								:show-test-button="true"
								:action-id="currentActionId"
								:test-result="testResult"
								:show-test-result="showTestResult"
								:testing="testing"
								:loading="testing"
								@test="handleTestResult"
								@update:show-test-result="showTestResult = $event"
							/>
						</div>

						<div v-else-if="actionForm.type === 'HTTP API'" class="http-api-config">
							<HttpApiConfig
								v-model="httpApiConfig"
								:show-test-button="true"
								:action-id="currentActionId"
								:test-result="testResult"
								:show-test-result="showTestResult"
								:testing="testing"
								:loading="testing"
								@test="handleTestResult"
								@update:show-test-result="showTestResult = $event"
							/>
						</div>

						<div
							v-else-if="actionForm.type === 'Python Script'"
							class="python-script-config"
						>
							<PythonScriptConfig
								ref="pythonScriptConfigRef"
								v-model="pythonScriptConfig"
								:show-test-button="true"
								:action-id="currentActionId"
								:test-result="testResult"
								:show-test-result="showTestResult"
								:testing="testing"
								:loading="testing"
								@test="handleTestResult"
								@update:show-test-result="
									showTestResult = $event;
									testResult = '';
								"
							/>
						</div>
					</el-card>
				</el-col>

				<!-- 右侧：分配信息 -->
				<el-col :span="8">
					<el-card class="assignments-card">
						<template #header>
							<div class="card-header">
								<span>Assignments</span>
								<el-button type="primary" link @click="showAssignmentDialog = true">
									<el-icon>
										<Plus />
									</el-icon>
									<span>Add Assignment</span>
								</el-button>
							</div>
						</template>

						<div class="assignments-list">
							<div
								v-for="assignment in actionForm.assignments"
								:key="assignment.id"
								class="assignment-item"
							>
								<div class="assignment-header">
									<span class="assignment-name">{{ assignment.name }}</span>
									<el-button
										type="danger"
										link
										@click="removeAssignment(assignment.id)"
									>
										<el-icon>
											<Delete />
										</el-icon>
									</el-button>
								</div>
								<div class="assignment-details">
									<span class="last-applied">
										Last applied: {{ assignment.lastApplied }}
									</span>
								</div>
							</div>

							<div v-if="actionForm.assignments.length === 0" class="empty-state">
								<el-empty description="No assignments yet" />
							</div>
						</div>
					</el-card>

					<!-- 执行历史 -->
					<el-card class="history-card">
						<template #header>
							<div class="card-header">
								<span>Execution History</span>
							</div>
						</template>

						<div class="history-list">
							<div
								v-for="history in executionHistory"
								:key="history.id"
								class="history-item"
							>
								<div class="history-header">
									<span class="history-status" :class="history.status">
										{{ history.status }}
									</span>
									<span class="history-date">{{ history.date }}</span>
								</div>
								<div class="history-details">
									<span class="history-target">{{ history.target }}</span>
								</div>
							</div>
						</div>
					</el-card>
				</el-col>
			</el-row>
		</div>

		<!-- 分配对话框 -->
		<el-dialog v-model="showAssignmentDialog" title="Add Assignment" width="500px">
			<el-form :model="newAssignment" label-width="100px">
				<el-form-item label="Workflow">
					<el-select v-model="newAssignment.workflow" placeholder="Select workflow">
						<el-option label="Standard Onboarding" value="standard" />
						<el-option label="Enterprise Onboarding" value="enterprise" />
						<el-option label="Expedited Onboarding" value="expedited" />
					</el-select>
				</el-form-item>
				<el-form-item label="Stage">
					<el-select v-model="newAssignment.stage" placeholder="Select stage">
						<el-option label="Sales Qualification" value="sales_qual" />
						<el-option label="Initial Contact" value="initial_contact" />
						<el-option label="System Setup" value="system_setup" />
						<el-option label="Implementation" value="implementation" />
					</el-select>
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="showAssignmentDialog = false">Cancel</el-button>
				<el-button type="primary" @click="addAssignment">Add Assignment</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, nextTick } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { ArrowLeft, Check, Delete, Plus } from '@element-plus/icons-vue';
import {
	getActionDetail,
	updateAction,
	deleteAction,
	ActionType,
	ACTION_TYPE_MAPPING,
} from '@/apis/action';
import { PythonScriptConfig, HttpApiConfig, SendEmailConfig } from '@/components/action-config';
import { useTestRun } from '@/hooks/useTestRun';
import { ActionDefinition, TriggerMapping } from '#/action';

// Router
const router = useRouter();
const route = useRoute();

// Use Test Run Hook
const { handleComponentTest, executeTest } = useTestRun();

// Reactive data
const saving = ref(false);
const loading = ref(false);
const testing = ref(false); // Test loading state
const showAssignmentDialog = ref(false);
const formRef = ref();
const currentActionId = ref('');
const testResult = ref('');
const showTestResult = ref(false);
const originalData = ref<any>(null); // Save original data for comparison
const pythonScriptConfigRef = ref(); // For getting Python Script config component reference

// Form data
const actionForm = reactive({
	actionId: '',
	actionName: '',
	type: '',
	description: '',
	assignments: [] as any[],
});

// Parse actionConfig method
const parseActionConfig = (config: string) => {
	try {
		return JSON.parse(config);
	} catch (error) {
		console.error('Failed to parse action config:', error);
		return {};
	}
};

// Get action type name
const getActionTypeName = (actionType: number) => {
	return ACTION_TYPE_MAPPING[actionType as ActionType] || 'Unknown';
};

// Config data
const emailConfig = reactive({
	subject: 'Welcome to Our Platform',
	template: 'Dear {{customer_name}},\n\nWelcome to our platform...',
	recipients: ['customer'],
});

const httpApiConfig = reactive({
	url: '',
	method: 'POST',
	headers: '{"Content-Type": "application/json"}',
});

const pythonScriptConfig = reactive({
	sourceCode: '',
});

// New assignment
const newAssignment = reactive({
	workflow: '',
	stage: '',
});

// Execution history
const executionHistory = ref([
	{
		id: 1,
		status: 'success',
		date: '2025-01-15 14:30',
		target: 'Customer: John Doe',
	},
	{
		id: 2,
		status: 'success',
		date: '2025-01-14 09:15',
		target: 'Customer: Jane Smith',
	},
	{
		id: 3,
		status: 'failed',
		date: '2025-01-13 16:20',
		target: 'Customer: Bob Johnson',
	},
]);

// Form validation rules
const rules = {
	actionName: [{ required: true, message: 'Please enter action name', trigger: 'blur' }],
	description: [{ required: true, message: 'Please enter action description', trigger: 'blur' }],
};

// Methods
const goBack = () => {
	router.push('/onboard/actions');
};

const handleSave = async (silent = false) => {
	try {
		await formRef.value.validate();
		saving.value = true;

		// Build update data
		const updateData: Partial<ActionDefinition> = {
			name: actionForm.actionName,
			description: actionForm.description,
			isEnabled: true,
		};

		// Get latest Python Script config if needed
		let currentPythonScriptConfig = { ...pythonScriptConfig };
		if (actionForm.type === 'Python Script' && pythonScriptConfigRef.value) {
			// Get latest config through exposed method
			const currentConfig = pythonScriptConfigRef.value.getCurrentConfig();
			currentPythonScriptConfig = currentConfig;
		}

		// Build actionConfig based on action type
		if (actionForm.type === 'Send Email') {
			updateData.actionType = ActionType.SEND_EMAIL;
			updateData.actionConfig = JSON.stringify(emailConfig);
		} else if (actionForm.type === 'HTTP API') {
			updateData.actionType = ActionType.HTTP_API;
			updateData.actionConfig = JSON.stringify(httpApiConfig);
		} else if (actionForm.type === 'Python Script') {
			updateData.actionType = ActionType.PYTHON_SCRIPT;
			updateData.actionConfig = JSON.stringify(currentPythonScriptConfig);
		}

		// Call update API
		const response = await updateAction(currentActionId.value, updateData);

		if (response.code === '200' && response.success) {
			if (!silent) {
				ElMessage.success('Action updated successfully');
			}

			// Reload detail API to get latest data
			await loadActionDetail(currentActionId.value);
		} else {
			ElMessage.error(response.msg || 'Failed to update action');
		}
	} catch (error) {
		console.error('Failed to update action:', error);
		ElMessage.error('Failed to update action');
	} finally {
		saving.value = false;
	}
};

const handleDelete = async () => {
	try {
		await ElMessageBox.confirm(
			`Are you sure you want to delete action "${actionForm.actionName}"?`,
			'Confirm Delete',
			{
				confirmButtonText: 'Delete',
				cancelButtonText: 'Cancel',
				type: 'warning',
			}
		);

		loading.value = true;
		const response = await deleteAction(currentActionId.value);

		if (response.code === '200' && response.success) {
			ElMessage.success('Action deleted successfully');
			// Redirect to actions list
			router.push('/onboard/actions');
		} else {
			ElMessage.error(response.msg || 'Failed to delete action');
		}
	} catch (error) {
		if (error !== 'cancel') {
			console.error('Failed to delete action:', error);
			ElMessage.error('Failed to delete action');
		}
	} finally {
		loading.value = false;
	}
};

// Handle test result
const handleTestResult = async (result: any) => {
	// Force get current CodeEditor value (if any)
	let currentPythonScriptConfig = { ...pythonScriptConfig };

	// If current type is Python Script, try to get latest value from PythonScriptConfig component
	if (actionForm.type === 'Python Script' && pythonScriptConfigRef.value) {
		// Get latest config through exposed method
		const currentConfig = pythonScriptConfigRef.value.getCurrentConfig();
		currentPythonScriptConfig = currentConfig;
	}

	const canTest = await handleComponentTest(result, {
		actionId: currentActionId.value,
		currentData: {
			...actionForm,
			emailConfig,
			httpApiConfig,
			pythonScriptConfig: currentPythonScriptConfig,
		},
		originalData: originalData.value,
		silentSave: true, // Don't show save success message for test
		onSave: async () => {
			try {
				await handleSave(true); // Silent save for test
				return true;
			} catch (error) {
				return false;
			}
		},
	});

	if (canTest) {
		try {
			testing.value = true;
			await nextTick();
			showTestResult.value = true;
			// Execute test
			const testOutput = await executeTest(currentActionId.value, actionForm.type);
			if (testOutput) {
				testResult.value = testOutput;
			}
		} catch (error) {
			console.error('Test execution failed:', error);
			ElMessage.error('Test execution failed');
		} finally {
			testing.value = false;
		}
	}
};

const addAssignment = () => {
	if (!newAssignment.workflow || !newAssignment.stage) {
		ElMessage.warning('Please select both workflow and stage');
		return;
	}

	const assignment = {
		id: Date.now(),
		name: `${newAssignment.workflow} → ${newAssignment.stage}`,
		lastApplied: 'Never',
	};

	actionForm.assignments.push(assignment);
	showAssignmentDialog.value = false;

	// Reset form
	newAssignment.workflow = '';
	newAssignment.stage = '';

	ElMessage.success('Assignment added successfully');
};

const removeAssignment = (id: number) => {
	const index = actionForm.assignments.findIndex((item) => item.id === id);
	if (index > -1) {
		actionForm.assignments.splice(index, 1);
		ElMessage.success('Assignment removed');
	}
};

// Load Action detail
const loadActionDetail = async (actionId: string) => {
	try {
		loading.value = true;
		currentActionId.value = actionId;
		const response = await getActionDetail(actionId);

		if (response.code === '200' && response.success) {
			const actionData = response.data;

			// Update form data
			actionForm.actionId = actionData.actionCode;
			actionForm.actionName = actionData.name;
			actionForm.description = actionData.description;
			actionForm.type = getActionTypeName(actionData.actionType);

			// Convert triggerMappings to assignments
			actionForm.assignments = actionData.triggerMappings.map((mapping: TriggerMapping) => ({
				id: mapping.id,
				name: getAssignmentDisplayName(mapping),
				lastApplied: mapping.lastApplied || 'Never',
			}));

			// Parse actionConfig and update config data
			const config = parseActionConfig(actionData.actionConfig);
			if (actionData.actionType === ActionType.SEND_EMAIL) {
				// Email config
				Object.assign(emailConfig, config);
			} else if (actionData.actionType === ActionType.HTTP_API) {
				// HTTP API config
				Object.assign(httpApiConfig, config);
			} else if (actionData.actionType === ActionType.PYTHON_SCRIPT) {
				// Python script config
				Object.assign(pythonScriptConfig, config);
			}

			// Save original data for comparison
			originalData.value = {
				...actionForm,
				emailConfig: { ...emailConfig },
				httpApiConfig: { ...httpApiConfig },
				pythonScriptConfig: { ...pythonScriptConfig },
			};
		} else {
			ElMessage.error(response.msg || 'Failed to load action details');
		}
	} catch (error) {
		console.error('Failed to load action details:', error);
		ElMessage.error('Failed to load action details');
	} finally {
		loading.value = false;
	}
};

// Get assignment display name
const getAssignmentDisplayName = (mapping: TriggerMapping) => {
	const parts: string[] = [];

	// Add WorkflowName
	if (mapping.workFlowName && mapping.workFlowName.trim()) {
		parts.push(mapping.workFlowName);
	}

	// Add StageName
	if (mapping.stageName && mapping.stageName.trim()) {
		parts.push(mapping.stageName);
	}

	// Add triggerSourceName
	if (mapping.triggerSourceName && mapping.triggerSourceName.trim()) {
		parts.push(mapping.triggerSourceName);
	}

	// If all fields are empty, return default value
	if (parts.length === 0) {
		return 'Unknown Assignment';
	}

	// Join all non-empty parts with arrows
	return parts.join(' → ');
};

// Lifecycle
onMounted(() => {
	const actionId = route.params.actionId as string;
	if (actionId) {
		loadActionDetail(actionId);
	} else {
		ElMessage.error('Action ID is required');
		router.push('/onboard/actions');
	}
});
</script>

<style scoped lang="scss">
.action-detail-container {
	display: flex;
	flex-direction: column;
	background-color: var(--el-bg-color-page);
}

.main-content {
	flex: 1;
	padding: 1.5rem 0;
	overflow: hidden;
}

.page-header {
	background: linear-gradient(135deg, var(--primary-50) 0%, var(--primary-100) 100%);
	border-bottom: 1px solid var(--primary-200);
	padding: 1.5rem 2rem;
}

.header-content {
	display: flex;
	justify-content: space-between;
	align-items: center;
	width: 100%;
}

.header-left {
	display: flex;
	align-items: center;
	gap: 1rem;
}

.back-button {
	background-color: transparent;
	border: none;
	color: var(--primary-700);
	padding: 0.5rem;
	margin-right: 1rem;
	transition: all 0.2s;
	min-height: 2rem;
	display: flex;
	align-items: center;
	justify-content: center;
}

.back-button:hover {
	background-color: var(--primary-200);
	color: var(--primary-700);
}

.back-icon {
	font-size: 1.25rem;
	width: 1.25rem;
	height: 1.25rem;
}

.header-info {
	display: flex;
	flex-direction: column;
}

.page-title {
	font-size: 1.875rem;
	font-weight: 700;
	color: var(--primary-800);
	margin: 0;
}

.page-description {
	color: var(--primary-600);
	margin: 0.25rem 0 0 0;
}

.header-actions {
	display: flex;
	gap: 0.5rem;
}

.save-button {
	background-color: var(--primary-600);
	border-color: var(--primary-600);
}

.save-button:hover {
	background-color: var(--primary-700);
	border-color: var(--primary-700);
}

.delete-button {
	background-color: var(--el-color-danger);
	border-color: var(--el-color-danger);
}

.delete-button:hover {
	background-color: var(--el-color-danger-light-3);
	border-color: var(--el-color-danger-light-3);
}

/* 深色模式支持 */
.dark .page-header {
	background: linear-gradient(135deg, var(--primary-800) 0%, var(--primary-700) 100%);
	border-bottom-color: var(--primary-600);
}

.dark .page-title {
	color: var(--primary-100);
}

.dark .page-description {
	color: var(--primary-200);
}

.dark .back-button {
	background-color: transparent;
	color: var(--primary-200);
}

.dark .back-button:hover {
	background-color: var(--primary-600);
	color: var(--primary-200);
}

.dark .save-button {
	background-color: var(--primary-600);
	border-color: var(--primary-600);
}

.dark .save-button:hover {
	background-color: var(--primary-500);
	border-color: var(--primary-500);
}

.dark .delete-button {
	background-color: var(--el-color-danger);
	border-color: var(--el-color-danger);
}

.dark .delete-button:hover {
	background-color: var(--el-color-danger-light-3);
	border-color: var(--el-color-danger-light-3);
}

.main-content {
	.info-card,
	.config-card,
	.assignments-card,
	.history-card {
		margin-bottom: 24px;
		box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);

		.card-header {
			display: flex;
			justify-content: space-between;
			align-items: center;
			font-weight: 600;
		}
	}

	.assignments-list {
		.assignment-item {
			padding: 12px;
			border: 1px solid #e5e7eb;
			border-radius: 6px;
			margin-bottom: 12px;

			.assignment-header {
				display: flex;
				justify-content: space-between;
				align-items: center;
				margin-bottom: 8px;

				.assignment-name {
					font-weight: 500;
					color: #374151;
				}
			}

			.assignment-details {
				.last-applied {
					font-size: 12px;
					color: #6b7280;
				}
			}
		}

		.empty-state {
			padding: 40px 0;
		}
	}

	.history-list {
		.history-item {
			padding: 12px;
			border: 1px solid #e5e7eb;
			border-radius: 6px;
			margin-bottom: 12px;

			.history-header {
				display: flex;
				justify-content: space-between;
				align-items: center;
				margin-bottom: 8px;

				.history-status {
					padding: 2px 8px;
					border-radius: 4px;
					font-size: 12px;
					font-weight: 500;

					&.success {
						background: #dcfce7;
						color: #166534;
					}

					&.failed {
						background: #fef2f2;
						color: #dc2626;
					}
				}

				.history-date {
					font-size: 12px;
					color: #6b7280;
				}
			}

			.history-details {
				.history-target {
					font-size: 14px;
					color: #374151;
				}
			}
		}
	}
}
</style>
