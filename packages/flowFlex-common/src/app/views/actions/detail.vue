<template>
	<div class="action-detail-container rounded-md">
		<!-- 页面头部 -->
		<div class="page-header rounded-md">
			<div class="header-content">
				<div class="header-left">
					<el-button
						type="primary"
						link
						class="back-button"
						@click="goBack"
						size="small"
					>
						<el-icon class="back-icon"><ArrowLeft /></el-icon>
					</el-button>
					<div class="header-info">
						<h1 class="page-title">
							Action Detail
						</h1>
						<p class="page-description">
							View and edit action configuration and assignments
						</p>
					</div>
				</div>
				<div class="header-actions">
					<el-button
						type="primary"
						class="save-button"
						@click="handleSave"
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
						
						<el-form :model="actionForm" :rules="rules" ref="formRef" label-width="120px">
							<el-row :gutter="20">
								<el-col :span="12">
									<el-form-item label="Action ID" prop="actionId">
										<el-input v-model="actionForm.actionId" disabled />
									</el-form-item>
								</el-col>
								<el-col :span="12">
									<el-form-item label="Action Name" prop="actionName">
										<el-input v-model="actionForm.actionName" placeholder="Enter action name" />
									</el-form-item>
								</el-col>
							</el-row>
							
							<el-row :gutter="20">
								<el-col :span="12">
									<el-form-item label="Type" prop="type">
										<el-select v-model="actionForm.type" placeholder="Select action type">
											<el-option label="Send Email" value="email" />
											<el-option label="System Action" value="system" />
											<el-option label="Webhook" value="webhook" />
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
						
						<div v-if="actionForm.type === 'email'" class="email-config">
							<el-form :model="emailConfig" label-width="120px">
								<el-form-item label="Subject">
									<el-input v-model="emailConfig.subject" placeholder="Email subject" />
								</el-form-item>
								<el-form-item label="Template">
									<el-input
										v-model="emailConfig.template"
										type="textarea"
										:rows="6"
										placeholder="Email template content"
									/>
								</el-form-item>
								<el-form-item label="Recipients">
									<el-select v-model="emailConfig.recipients" multiple placeholder="Select recipients">
										<el-option label="Customer" value="customer" />
										<el-option label="Sales Team" value="sales" />
										<el-option label="Support Team" value="support" />
									</el-select>
								</el-form-item>
							</el-form>
						</div>
						
						<div v-else-if="actionForm.type === 'webhook'" class="webhook-config">
							<el-form :model="webhookConfig" label-width="120px">
								<el-form-item label="URL">
									<el-input v-model="webhookConfig.url" placeholder="Webhook URL" />
								</el-form-item>
								<el-form-item label="Method">
									<el-select v-model="webhookConfig.method">
										<el-option label="POST" value="POST" />
										<el-option label="GET" value="GET" />
										<el-option label="PUT" value="PUT" />
									</el-select>
								</el-form-item>
								<el-form-item label="Headers">
									<el-input
										v-model="webhookConfig.headers"
										type="textarea"
										:rows="3"
										placeholder="JSON format headers"
									/>
								</el-form-item>
							</el-form>
						</div>
						
						<div v-else-if="actionForm.type === 'system'" class="system-config">
							<el-form :model="systemConfig" label-width="120px">
								<el-form-item label="System Action">
									<el-select v-model="systemConfig.action">
										<el-option label="Create Task" value="create_task" />
										<el-option label="Update Status" value="update_status" />
										<el-option label="Send Notification" value="send_notification" />
									</el-select>
								</el-form-item>
								<el-form-item label="Parameters">
									<el-input
										v-model="systemConfig.parameters"
										type="textarea"
										:rows="4"
										placeholder="Action parameters in JSON format"
									/>
								</el-form-item>
							</el-form>
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
									<el-icon><Plus /></el-icon>
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
									<el-button type="danger" link @click="removeAssignment(assignment.id)">
										<el-icon><Delete /></el-icon>
									</el-button>
								</div>
								<div class="assignment-details">
									<span class="last-applied">Last applied: {{ assignment.lastApplied }}</span>
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
import { ref, reactive, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import {
	ArrowLeft,
	Check,
	Delete,
	Plus,
} from '@element-plus/icons-vue';

// 路由
const router = useRouter();
const route = useRoute();

// 响应式数据
const saving = ref(false);
const showAssignmentDialog = ref(false);
const formRef = ref();

// 表单数据
const actionForm = reactive({
	actionId: 'ACT-001',
	actionName: 'Welcome Email Sequence',
	type: 'email',
	description: 'Send welcome email sequence to new customers',
	assignments: [
		{
			id: 1,
			name: 'Standard Onboarding → Sales Qualification',
			lastApplied: '2025-01-15 14:30',
		},
		{
			id: 2,
			name: 'Enterprise Onboarding → Initial Contact',
			lastApplied: '2025-01-14 09:15',
		},
	],
});

// 配置数据
const emailConfig = reactive({
	subject: 'Welcome to Our Platform',
	template: 'Dear {{customer_name}},\n\nWelcome to our platform...',
	recipients: ['customer'],
});

const webhookConfig = reactive({
	url: '',
	method: 'POST',
	headers: '{"Content-Type": "application/json"}',
});

const systemConfig = reactive({
	action: 'create_task',
	parameters: '{"task_type": "welcome", "priority": "medium"}',
});

// 新分配
const newAssignment = reactive({
	workflow: '',
	stage: '',
});

// 执行历史
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

// 表单验证规则
const rules = {
	actionName: [
		{ required: true, message: 'Please enter action name', trigger: 'blur' },
	],
	type: [
		{ required: true, message: 'Please select action type', trigger: 'change' },
	],
};

// 方法
const goBack = () => {
	router.push('/onboard/actions');
};

const handleSave = async () => {
	try {
		await formRef.value.validate();
		saving.value = true;
		
		// 模拟保存
		await new Promise(resolve => setTimeout(resolve, 1000));
		
		ElMessage.success('Action saved successfully');
		saving.value = false;
	} catch (error) {
		ElMessage.error('Please check the form');
	}
};

const handleDelete = () => {
	ElMessageBox.confirm(
		'Are you sure you want to delete this action?',
		'Confirm Delete',
		{
			confirmButtonText: 'Delete',
			cancelButtonText: 'Cancel',
			type: 'warning',
		}
	).then(() => {
		ElMessage.success('Action deleted');
		router.push('/onboard/actions');
	});
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
	
	// 重置表单
	newAssignment.workflow = '';
	newAssignment.stage = '';
	
	ElMessage.success('Assignment added successfully');
};

const removeAssignment = (id: number) => {
	const index = actionForm.assignments.findIndex(item => item.id === id);
	if (index > -1) {
		actionForm.assignments.splice(index, 1);
		ElMessage.success('Assignment removed');
	}
};

// 生命周期
onMounted(() => {
	const actionId = route.params.actionId;
	if (actionId) {
		// 根据actionId加载数据
		console.log('Loading action:', actionId);
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