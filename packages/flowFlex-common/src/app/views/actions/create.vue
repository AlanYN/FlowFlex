<template>
	<div class="create-action-container rounded-md">
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
						<h1 class="page-title">Create New Action</h1>
						<p class="page-description">
							Design and configure automated actions for your onboarding workflows
						</p>
					</div>
				</div>
				<div class="header-actions">
					<!-- Save as Draft - 下一期实现 -->
					<!-- <el-button
						type="default"
						class="draft-button"
						@click="handleSaveAsDraft"
						:icon="Document"
					>
						Save as Draft
					</el-button> -->
					<el-button
						type="primary"
						class="save-button"
						@click="handleSave"
						:loading="saving"
						:icon="Check"
					>
						Save Action
					</el-button>
				</div>
			</div>
		</div>

		<!-- 主要内容区域 -->
		<div class="main-content">
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
									<el-form-item label="Action Name" prop="actionName">
										<el-input
											v-model="actionForm.actionName"
											placeholder="Enter action name"
										/>
									</el-form-item>
								</el-col>
								<el-col :span="12">
									<el-form-item label="Type" prop="type">
										<el-select
											v-model="actionForm.type"
											placeholder="Select action type"
										>
											<el-option label="Send Email" value="Send Email" />
											<el-option label="HTTP API" value="HTTP API" />
											<el-option
												label="Python Script"
												value="Python Script"
											/>
										</el-select>
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
								:action-id="''"
								@test="handleTestRun"
							/>
							<div class="template-variables">
								<span class="variable-label">Available variables:</span>
								<el-tag
									v-for="variable in emailVariables"
									:key="variable"
									size="small"
									class="variable-tag"
								>
									{{ variable }}
								</el-tag>
							</div>
						</div>

						<div v-else-if="actionForm.type === 'HTTP API'" class="http-api-config">
							<HttpApiConfig
								v-model="httpApiConfig"
								:show-test-button="true"
								:action-id="''"
								@test="handleTestRun"
							/>
						</div>

						<div
							v-else-if="actionForm.type === 'Python Script'"
							class="python-script-config"
						>
							<PythonScriptConfig
								v-model="pythonScriptConfig"
								:show-test-button="true"
								:action-id="''"
								@test="handleTestRun"
							/>
						</div>
					</el-card>
				</el-col>

				<!-- 右侧：分配和条件 -->
				<el-col :span="8">
					<!-- 分配信息 -->
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
									<span class="assignment-stage">{{ assignment.stage }}</span>
								</div>
							</div>

							<div v-if="actionForm.assignments.length === 0" class="empty-state">
								<el-empty description="No assignments yet" />
							</div>
						</div>
					</el-card>

					<!-- 触发条件 - 下一期实现 -->
					<!-- <el-card class="conditions-card">
						<template #header>
							<div class="card-header">
								<span>Trigger Conditions</span>
								<el-button type="primary" link @click="showConditionDialog = true">
									<el-icon><Plus /></el-icon>
									Add Condition
								</el-button>
							</div>
						</template>
						
						<div class="conditions-list">
							<div
								v-for="condition in actionForm.conditions"
								:key="condition.id"
								class="condition-item"
							>
								<div class="condition-header">
									<span class="condition-field">{{ condition.field }}</span>
									<el-button type="danger" link @click="removeCondition(condition.id)">
										<el-icon><Delete /></el-icon>
									</el-button>
								</div>
								<div class="condition-details">
									<span class="condition-operator">{{ condition.operator }}</span>
									<span class="condition-value">{{ condition.value }}</span>
								</div>
							</div>
							
							<div v-if="actionForm.conditions.length === 0" class="empty-state">
								<el-empty description="No conditions set" />
							</div>
						</div>
					</el-card> -->

					<!-- 高级设置 - 下一期实现 -->
					<!-- <el-card class="advanced-card">
						<template #header>
							<div class="card-header">
								<span>Advanced Settings</span>
							</div>
						</template>
						
						<el-form :model="advancedSettings" label-width="100px">
							<el-form-item label="Execution">
								<el-select v-model="advancedSettings.execution" placeholder="Select execution type">
									<el-option label="Immediate" value="immediate" />
									<el-option label="Scheduled" value="scheduled" />
									<el-option label="Delayed" value="delayed" />
								</el-select>
							</el-form-item>
							
							<el-form-item v-if="advancedSettings.execution === 'scheduled'" label="Schedule">
								<el-input v-model="advancedSettings.schedule" placeholder="Cron expression" />
							</el-form-item>
							
							<el-form-item v-if="advancedSettings.execution === 'delayed'" label="Delay">
								<el-input-number v-model="advancedSettings.delay" :min="1" :max="1440" />
								<span class="delay-unit">minutes</span>
							</el-form-item>
							
							<el-form-item label="Max Retries">
								<el-input-number v-model="advancedSettings.maxRetries" :min="0" :max="10" />
							</el-form-item>
							
							<el-form-item label="Timeout">
								<el-input-number v-model="advancedSettings.timeout" :min="1" :max="300" />
								<span class="timeout-unit">seconds</span>
							</el-form-item>
						</el-form>
					</el-card> -->
				</el-col>
			</el-row>
		</div>

		<!-- 分配对话框 -->
		<el-dialog v-model="showAssignmentDialog" title="Add Assignment" width="500px">
			<el-form :model="newAssignment" label-width="100px">
				<el-form-item label="Workflow" prop="workflow">
					<el-select v-model="newAssignment.workflow" placeholder="Select workflow">
						<el-option label="Standard Onboarding" value="standard" />
						<el-option label="Enterprise Onboarding" value="enterprise" />
						<el-option label="Expedited Onboarding" value="expedited" />
					</el-select>
				</el-form-item>
				<el-form-item label="Stage" prop="stage">
					<el-select v-model="newAssignment.stage" placeholder="Select stage">
						<el-option label="Sales Qualification" value="sales_qual" />
						<el-option label="Initial Contact" value="initial_contact" />
						<el-option label="System Setup" value="system_setup" />
						<el-option label="Implementation" value="implementation" />
						<el-option label="Billing Configuration" value="billing_config" />
					</el-select>
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="showAssignmentDialog = false">Cancel</el-button>
				<el-button type="primary" @click="addAssignment">Add Assignment</el-button>
			</template>
		</el-dialog>

		<!-- 条件对话框 - 下一期实现 -->
		<!-- <el-dialog v-model="showConditionDialog" title="Add Condition" width="500px">
			<el-form :model="newCondition" label-width="100px">
				<el-form-item label="Field" prop="field">
					<el-select v-model="newCondition.field" placeholder="Select field">
						<el-option label="Customer Status" value="customer_status" />
						<el-option label="Onboarding Stage" value="onboarding_stage" />
						<el-option label="Account Type" value="account_type" />
						<el-option label="Created Date" value="created_date" />
					</el-select>
				</el-form-item>
				<el-form-item label="Operator" prop="operator">
					<el-select v-model="newCondition.operator" placeholder="Select operator">
						<el-option label="Equals" value="equals" />
						<el-option label="Not Equals" value="not_equals" />
						<el-option label="Contains" value="contains" />
						<el-option label="Greater Than" value="greater_than" />
						<el-option label="Less Than" value="less_than" />
					</el-select>
				</el-form-item>
				<el-form-item label="Value" prop="value">
					<el-input v-model="newCondition.value" placeholder="Enter value" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="showConditionDialog = false">Cancel</el-button>
				<el-button type="primary" @click="addCondition">Add Condition</el-button>
			</template>
		</el-dialog> -->
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import {
	ArrowLeft,
	Check,
	// Document,
	Plus,
	Delete,
} from '@element-plus/icons-vue';
import { PythonScriptConfig, HttpApiConfig, SendEmailConfig } from '@/components/action-config';

// Router
const router = useRouter();

// Reactive data
const saving = ref(false);
const showAssignmentDialog = ref(false);
const formRef = ref();

// Form data
const actionForm = reactive({
	actionName: '',
	type: '',
	description: '',
	assignments: [] as any[],
});

// Config data
const emailConfig = reactive({
	subject: '',
	template: '',
	recipients: [],
	cc: '',
	bcc: '',
});

const httpApiConfig = reactive({
	url: '',
	method: 'POST',
	headers: '{"Content-Type": "application/json"}',
	timeout: 30,
});

const pythonScriptConfig = reactive({
	sourceCode: '',
});

// New assignment
const newAssignment = reactive({
	workflow: '',
	stage: '',
});

// Email variables
const emailVariables = [
	'{{customer_name}}',
	'{{company_name}}',
	'{{onboarding_stage}}',
	'{{account_type}}',
	'{{created_date}}',
];

// Form validation rules
const rules = {
	actionName: [{ required: true, message: 'Please enter action name', trigger: 'blur' }],
	type: [{ required: true, message: 'Please select action type', trigger: 'change' }],
};

// Methods
const goBack = () => {
	router.push('/onboard/actions');
};

const handleSave = async (silent = false) => {
	try {
		await formRef.value.validate();

		// Validate config based on type
		if (actionForm.type === 'Send Email') {
			if (
				!emailConfig.subject ||
				!emailConfig.template ||
				emailConfig.recipients.length === 0
			) {
				ElMessage.error('Please fill all required email fields');
				return;
			}
		} else if (actionForm.type === 'HTTP API') {
			if (!httpApiConfig.url || !httpApiConfig.method) {
				ElMessage.error('Please fill all required HTTP API fields');
				return;
			}
		} else if (actionForm.type === 'Python Script') {
			if (!pythonScriptConfig.sourceCode) {
				ElMessage.error('Please enter Python script code');
				return;
			}
		}

		saving.value = true;

		// Mock save
		await new Promise((resolve) => setTimeout(resolve, 1000));

		if (!silent) {
			ElMessage.success('Action saved successfully');
		}
		router.push('/onboard/actions');
	} catch (error) {
		ElMessage.error('Please check the form');
	} finally {
		saving.value = false;
	}
};

const addAssignment = () => {
	if (!newAssignment.workflow || !newAssignment.stage) {
		ElMessage.warning('Please select both workflow and stage');
		return;
	}

	const assignment = {
		id: Date.now(),
		name: `${newAssignment.workflow} Onboarding`,
		stage: newAssignment.stage,
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

// Handle test run
const handleTestRun = async (result: any) => {
	// Create page has no Action ID, so just show warning
	ElMessage.warning('Please save the action first before testing');
};

// Lifecycle
onMounted(() => {
	// Initialize form
});
</script>

<style scoped lang="scss">
.create-action-container {
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

/* Save as Draft 按钮样式 - 下一期实现 */
/* .draft-button {
	border-color: var(--primary-300);
	color: var(--primary-700);
	background-color: transparent;
}

.draft-button:hover {
	background-color: var(--primary-100);
	border-color: var(--primary-400);
} */

.save-button:hover {
	background-color: var(--primary-700);
	border-color: var(--primary-700);
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

/* Save as Draft 按钮深色模式样式 - 下一期实现 */
/* .dark .draft-button {
	border-color: var(--primary-600);
	color: var(--primary-200);
	background-color: transparent;
}

.dark .draft-button:hover {
	background-color: var(--primary-700);
	border-color: var(--primary-500);
} */

.dark .save-button {
	background-color: var(--primary-600);
	border-color: var(--primary-600);
}

.dark .save-button:hover {
	background-color: var(--primary-500);
	border-color: var(--primary-500);
}

.main-content {
	.info-card,
	.config-card,
	.assignments-card,
	.conditions-card,
	.advanced-card {
		margin-bottom: 24px;
		box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);

		.card-header {
			display: flex;
			justify-content: space-between;
			align-items: center;
			font-weight: 600;
		}
	}

	.template-variables {
		margin-top: 8px;

		.variable-label {
			font-size: 12px;
			color: #6b7280;
			margin-right: 8px;
		}

		.variable-tag {
			margin-right: 4px;
			margin-bottom: 4px;
			cursor: pointer;
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
				font-size: 12px;
				color: #6b7280;

				.assignment-stage {
					margin-right: 8px;
				}
			}
		}

		.empty-state {
			padding: 40px 0;
		}
	}

	// 条件相关样式 - 下一期实现
	// .conditions-list {
	// 	.condition-item {
	// 		padding: 12px;
	// 		border: 1px solid #e5e7eb;
	// 		border-radius: 6px;
	// 		margin-bottom: 12px;

	// 		.condition-header {
	// 			display: flex;
	// 			justify-content: space-between;
	// 			align-items: center;
	// 			margin-bottom: 8px;

	// 			.condition-field {
	// 				font-weight: 500;
	// 				color: #374151;
	// 			}
	// 		}

	// 		.condition-details {
	// 			font-size: 12px;
	// 			color: #6b7280;

	// 			.condition-operator,
	// 			.condition-value {
	// 				margin-right: 8px;
	// 			}
	// 		}
	// 	}

	// 	.empty-state {
	// 		padding: 40px 0;
	// 	}
	// }

	// 高级设置相关样式 - 下一期实现
	// .delay-unit,
	// .timeout-unit {
	// 	margin-left: 8px;
	// 	font-size: 12px;
	// 	color: #6b7280;
	// }
}
</style>
