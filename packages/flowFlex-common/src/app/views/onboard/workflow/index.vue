<template>
	<div class="workflow-container">
		<!-- 标题和操作区 -->
		<div class="workflow-header">
			<h1 class="title">Workflows</h1>
			<div class="actions">
				<el-button @click="showVersionHistory" :disabled="loading.versions">
					<el-icon v-if="loading.versions"><Loading /></el-icon>
					<el-icon v-else><Clock /></el-icon>
					<span>Version History</span>
				</el-button>
				<el-button
					type="primary"
					@click="showNewWorkflowDialog"
					:disabled="loading.createWorkflow"
				>
					<el-icon v-if="loading.createWorkflow"><Loading /></el-icon>
					<el-icon v-else><Plus /></el-icon>
					<span>New Workflow</span>
				</el-button>
			</div>
		</div>

		<!-- 主要内容区 -->
		<div>
			<!-- 加载中状态 -->
			<div v-if="loading.workflows" class="loading-container rounded-md">
				<el-skeleton style="width: 100%" :rows="10" animated />
			</div>

			<!-- 工作流内容 -->
			<div class="workflow-list" v-else-if="workflow">
				<div
					class="workflow-card rounded-md"
					:class="{ active: (displayWorkflow || workflow).isActive }"
				>
					<div class="workflow-card-header">
						<div class="left-section">
							<div class="title-and-tags">
								<span class="workflow-name">
									{{ (displayWorkflow || workflow).name }}
								</span>
								<el-tag
									v-if="(displayWorkflow || workflow).isDefault"
									type="warning"
									effect="light"
									size="small"
									class="default-tag rounded-md"
								>
									<div class="flex items-center gapx-2">
										<StarIcon class="star-icon" />
										Default
									</div>
								</el-tag>
								<el-tag
									v-if="(displayWorkflow || workflow).status === 'active'"
									type="success"
									effect="light"
									size="small"
									class="rounded-md"
								>
									Active
								</el-tag>
								<el-tag
									v-else
									type="info"
									size="small"
									effect="light"
									class="rounded-md"
								>
									Inactive
								</el-tag>
							</div>
							<span class="workflow-desc">
								{{ (displayWorkflow || workflow).description }}
							</span>
						</div>
						<div class="right-section">
							<!-- 工作流选择器 -->
							<el-select
								v-model="selectedWorkflow"
								placeholder="Select Workflow"
								size="default"
								class="workflow-selector"
								@change="onWorkflowChange"
							>
								<el-option
									v-for="workflowItem in workflowListData"
									:key="workflowItem.id"
									:label="workflowItem.name"
									:value="workflowItem.id"
								>
									<div class="flex items-center justify-between">
										<div>
											<el-icon v-if="workflowItem.id === workflow?.id">
												<Check />
											</el-icon>
											<span>{{ workflowItem.name }}</span>
										</div>
										<div class="flex items-center gap-1">
											<div v-if="workflowItem.isDefault">⭐</div>
											<el-icon
												v-if="workflowItem.status === 'inactive'"
												class="inactive-icon"
											>
												<VideoPause />
											</el-icon>
										</div>
									</div>
								</el-option>
							</el-select>

							<!-- 更多操作按钮 -->
							<el-dropdown
								trigger="click"
								@command="(cmd) => workflow && handleCommand(cmd, workflow)"
								:disabled="
									loading.activateWorkflow ||
									loading.deactivateWorkflow ||
									loading.duplicateWorkflow ||
									loading.updateWorkflow ||
									loading.exportWorkflow
								"
								:teleported="false"
								:popper-options="{
									modifiers: [
										{
											name: 'computeStyles',
											options: {
												adaptive: false,
												enabled: false,
											},
										},
									],
								}"
							>
								<el-button
									class="more-actions-btn rounded-md"
									aria-label="More actions"
									:aria-expanded="false"
								>
									<el-icon
										v-if="
											loading.activateWorkflow ||
											loading.deactivateWorkflow ||
											loading.duplicateWorkflow ||
											loading.updateWorkflow ||
											loading.exportWorkflow
										"
									>
										<Loading />
									</el-icon>
									<el-icon v-else><MoreFilled /></el-icon>
								</el-button>
								<template #dropdown>
									<el-dropdown-menu class="actions-dropdown">
										<el-dropdown-item command="edit">
											<el-icon><Edit /></el-icon>
											Edit Workflow
										</el-dropdown-item>
										<el-dropdown-item
											v-if="
												!workflow.isDefault && workflow.status === 'active'
											"
											divided
											command="setDefault"
										>
											<el-icon><Star /></el-icon>
											Set as Default
										</el-dropdown-item>
										<el-dropdown-item
											v-if="workflow.status === 'active'"
											command="deactivate"
										>
											<el-icon><CircleClose /></el-icon>
											Set as Inactive
										</el-dropdown-item>
										<el-dropdown-item
											v-if="workflow.status === 'inactive'"
											command="activate"
										>
											<el-icon><Check /></el-icon>
											Set as Active
										</el-dropdown-item>
										<el-dropdown-item divided command="duplicate">
											<el-icon><CopyDocument /></el-icon>
											Duplicate
										</el-dropdown-item>
										<el-dropdown-item command="addStage">
											<el-icon><Plus /></el-icon>
											Add Stage
										</el-dropdown-item>
										<!-- <el-dropdown-item
											command="combineStages"
											:disabled="workflow.stages.length < 2"
										>
											<el-icon><Connection /></el-icon>
											Combine Stages
										</el-dropdown-item> -->
										<el-dropdown-item divided command="export">
											<el-icon><Download /></el-icon>
											Export Workflow
										</el-dropdown-item>
									</el-dropdown-menu>
								</template>
							</el-dropdown>
						</div>
					</div>

					<!-- Workflow 内容 -->
					<div class="workflow-card-body">
						<div class="workflow-header-actions">
							<div class="dates-container">
								<div class="date-item">
									<el-icon class="calendar-icon"><Calendar /></el-icon>
									<span class="date-label">Start:</span>
									<span class="date-value">
										{{
											formatDate(
												(displayWorkflow || workflow).startDate || ''
											)
										}}
									</span>
								</div>
								<div v-if="(displayWorkflow || workflow).endDate" class="date-item">
									<el-icon
										class="calendar-icon"
										style="color: var(--el-color-danger)"
									>
										<Calendar />
									</el-icon>
									<span class="date-label">End:</span>
									<span class="date-value">
										{{
											formatDate((displayWorkflow || workflow).endDate || '')
										}}
									</span>
								</div>
							</div>
							<button
								class="add-stage-btn"
								@click="addStage()"
								:disabled="loading.createStage"
							>
								<el-icon v-if="loading.createStage" class="mr-1 text-primary">
									<Loading />
								</el-icon>
								<el-icon v-else class="mr-1 text-primary"><Plus /></el-icon>
								<span>Add Stage</span>
							</button>
						</div>

						<!-- Stages 标题 -->
						<div class="stages-header">
							<div class="stages-header-actions">
								<el-tag
									v-if="displayWorkflow && displayWorkflow.id !== workflow.id"
									type="info"
									effect="light"
									size="small"
									class="viewing-history-tag"
								>
									Viewing Historical Version
								</el-tag>
								<el-button
									v-if="displayWorkflow && displayWorkflow.id !== workflow.id"
									type="primary"
									size="small"
									@click="displayWorkflow = null"
									class="back-to-current-btn"
								>
									<el-icon><ArrowLeft /></el-icon>
									Back to Current
								</el-button>
							</div>
						</div>

						<StagesList
							v-model:stages="(displayWorkflow || workflow).stages"
							:workflow-id="workflow.id"
							:is-editing="
								isEditing &&
								(!displayWorkflow || displayWorkflow.id === workflow.id)
							"
							:loading="{
								stages: loading.stages,
								deleteStage: loading.deleteStage,
								sortStages: loading.sortStages,
							}"
							@edit="(stage) => editStage(stage)"
							@delete="(stageId) => deleteStage(stageId)"
							@drag-start="onDragStart"
							@order-changed="() => updateStagesOrder()"
						/>
					</div>

					<!-- 总阶段数信息 -->
					<div class="workflow-footer">
						<p class="stage-count">
							Total stages: {{ (displayWorkflow || workflow).stages.length }}
						</p>
					</div>
				</div>
			</div>
		</div>

		<!-- 工作流表单对话框 -->
		<el-dialog
			v-model="dialogVisible.workflowForm"
			:title="dialogTitle"
			:width="dialogWidth + 'px'"
			destroy-on-close
			custom-class="workflow-dialog"
			:show-close="true"
			:close-on-click-modal="false"
			draggable
		>
			<template #header>
				<div class="dialog-header">
					<h2 class="dialog-title">
						{{ dialogTitle }}
					</h2>
					<p class="dialog-subtitle">
						{{ dialogSubtitle }}
					</p>
				</div>
			</template>
			<NewWorkflowForm
				v-if="dialogVisible.workflowForm"
				:initial-data="
					isEditingWorkflow
						? isEditingFromHistory
							? editingWorkflowData
							: workflow
						: undefined
				"
				:is-editing="isEditingWorkflow"
				@submit="handleWorkflowSubmit"
				@cancel="handleWorkflowCancel"
				:loading="isEditingWorkflow ? loading.updateWorkflow : loading.createWorkflow"
			/>
		</el-dialog>

		<!-- 版本历史对话框 -->
		<el-dialog
			v-model="dialogVisible.versionHistory"
			:width="bigDialogWidth + 'px'"
			destroy-on-close
			custom-class="version-history-dialog"
			:show-close="true"
			:close-on-click-modal="false"
			draggable
		>
			<template #header>
				<div class="version-dialog-header">
					<h2 class="version-dialog-title">Workflow Version History</h2>
					<p class="version-dialog-subtitle">View and manage all workflow versions.</p>
				</div>
			</template>

			<div class="version-history-content">
				<el-table
					:data="versionHistoryData"
					:border="false"
					:stripe="false"
					v-loading="loading.versions"
					class="version-history-table"
					header-row-class-name="version-table-header"
					row-class-name="version-table-row"
				>
					<template #empty>
						<el-empty description="No Data" :image-size="50" />
					</template>

					<el-table-column prop="name" label="Name" min-width="200">
						<template #default="scope">
							<div class="version-name">{{ scope.row.name }}</div>
						</template>
					</el-table-column>

					<el-table-column prop="status" label="Status" width="100">
						<template #default="scope">
							<el-tag
								:type="scope.row.status === 'Active' ? 'success' : 'info'"
								effect="light"
								size="small"
								class="status-tag"
							>
								{{ scope.row.status }}
							</el-tag>
						</template>
					</el-table-column>

					<el-table-column prop="isDefault" label="Default" width="100">
						<template #default="scope">
							<el-tag
								v-if="scope.row.isDefault"
								type="warning"
								effect="light"
								size="small"
								class="default-tag"
							>
								Default
							</el-tag>
							<el-tag
								v-else
								type="info"
								effect="light"
								size="small"
								class="status-tag"
							>
								No
							</el-tag>
						</template>
					</el-table-column>

					<el-table-column prop="startDate" label="Start Date" width="120">
						<template #default="scope">
							<span class="date-text">
								{{ formatDate(scope.row.startDate) || 'Not set' }}
							</span>
						</template>
					</el-table-column>

					<el-table-column prop="endDate" label="End Date" width="120">
						<template #default="scope">
							<span class="date-text">
								{{ formatDate(scope.row.endDate) || 'Not set' }}
							</span>
						</template>
					</el-table-column>

					<el-table-column prop="createdBy" label="Created By" width="120">
						<template #default="scope">
							<span class="created-by">{{ scope.row.createdBy || 'Admin' }}</span>
						</template>
					</el-table-column>

					<el-table-column prop="createdAt" label="Created At" width="140">
						<template #default="scope">
							<span class="date-text">{{ formatDate(scope.row.createdAt) }}</span>
						</template>
					</el-table-column>

					<el-table-column fixed="right" label="Actions" width="150">
						<template #default="scope">
							<div class="action-buttons">
								<el-button
									link
									type="primary"
									size="small"
									@click="selectVersion(scope.row)"
									class="view-btn"
								>
									View Stages
								</el-button>
								<el-tooltip content="Edit Workflow" placement="top">
									<el-button
										link
										type="primary"
										size="small"
										class="edit-btn"
										@click="editWorkflowVersion(scope.row)"
										:disabled="loading.updateWorkflow"
									>
										<el-icon><Edit /></el-icon>
									</el-button>
								</el-tooltip>
							</div>
						</template>
					</el-table-column>
				</el-table>
			</div>

			<template #footer>
				<div class="version-dialog-footer">
					<el-button @click="dialogVisible.versionHistory = false" class="close-btn">
						Close
					</el-button>
				</div>
			</template>
		</el-dialog>

		<!-- 新建/编辑阶段对话框 -->
		<el-dialog
			v-model="dialogVisible.stageForm"
			:title="isEditingStage ? 'Edit Stage' : 'Add New Stage'"
			:width="bigDialogWidth"
			destroy-on-close
			custom-class="workflow-dialog"
			:show-close="true"
			:close-on-click-modal="false"
			draggable
		>
			<template #header>
				<div class="dialog-header">
					<h2 class="dialog-title">
						{{ isEditingStage ? 'Edit Stage' : 'Add New Stage' }}
					</h2>
					<p class="dialog-subtitle">
						{{
							isEditingStage
								? 'Update the stage details with customizable components.'
								: 'Add a new stage to the workflow with customizable components.'
						}}
					</p>
				</div>
			</template>
			<StageForm
				v-if="dialogVisible.stageForm"
				:stage="currentStage"
				:is-editing="isEditingStage"
				:loading="isEditingStage ? loading.updateStage : loading.createStage"
				:checklists="checklists"
				:questionnaires="questionnaires"
				@submit="submitStage"
				@cancel="dialogVisible.stageForm = false"
			/>
		</el-dialog>

		<!-- 合并阶段对话框 -->
		<el-dialog
			v-model="dialogVisible.combineStages"
			title="Combine Stages"
			:width="dialogWidth"
			destroy-on-close
			draggable
		>
			<div v-if="dialogVisible.combineStages" class="combine-stages-form">
				<p class="text-sm text-muted mb-4">
					Select the stages you want to combine. The selected stages will be merged into a
					new stage.
				</p>

				<div class="stages-to-combine-list mb-4">
					<el-checkbox-group v-model="stagesToCombine">
						<div
							v-for="stage in getWorkflowStages()"
							:key="stage.id"
							class="stage-item-select"
						>
							<el-checkbox :label="stage.id">
								<div class="flex items-center">
									<div
										class="stage-color-indicator"
										:style="{
											backgroundColor:
												stage.color || getAvatarColor(stage.name),
										}"
									></div>
									<span>{{ stage.name }}</span>
								</div>
							</el-checkbox>
						</div>
					</el-checkbox-group>
				</div>

				<div class="combined-stage-info space-y-4">
					<div>
						<label class="block text-sm font-medium mb-1">New Stage Name</label>
						<el-input
							v-model="combinedStageName"
							placeholder="Enter name for combined stage"
						/>
					</div>

					<div>
						<label class="block text-sm font-medium mb-1">Assigned Group</label>
						<el-select
							v-model="combinedStageGroup"
							placeholder="Select group"
							class="w-full"
						>
							<el-option label="Account Management" value="Account Management" />
							<el-option label="Sales" value="Sales" />
							<el-option label="Customer" value="Customer" />
							<el-option label="Legal" value="Legal" />
							<el-option label="IT" value="IT" />
						</el-select>
					</div>

					<div>
						<label class="block text-sm font-medium mb-1">
							Estimated Duration (days)
						</label>
						<el-input-number v-model="combinedStageDuration" :min="1" :max="30" />
					</div>
				</div>

				<div class="flex justify-end space-x-2 mt-6">
					<el-button
						@click="dialogVisible.combineStages = false"
						:disabled="loading.combineStages"
					>
						Cancel
					</el-button>
					<el-button
						type="primary"
						:loading="loading.combineStages"
						:disabled="
							stagesToCombine.length < 2 ||
							!combinedStageName ||
							!combinedStageGroup ||
							!combinedStageDuration ||
							loading.combineStages
						"
						class="combine-btn"
						:class="{
							'disabled-btn':
								stagesToCombine.length < 2 ||
								!combinedStageName ||
								!combinedStageGroup ||
								!combinedStageDuration ||
								loading.combineStages,
						}"
						@click="combineSelectedStages"
					>
						Combine Stages
					</el-button>
				</div>
			</div>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, watch, computed } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import {
	Plus,
	MoreFilled,
	Clock,
	Calendar,
	Edit,
	CircleClose,
	Check,
	CopyDocument,
	Download,
	// Connection,
	Loading,
	Star,
	ArrowLeft,
	VideoPause,
} from '@element-plus/icons-vue';

import StarIcon from '@assets/svg/workflow/star.svg';
import { timeZoneConvert } from '@/hooks/time';
import { projectDate, dialogWidth, bigDialogWidth } from '@/settings/projectSetting';
import { useI18n } from '@/hooks/useI18n';

// 引入OW模块API接口
import {
	createWorkflow as createWorkflowApi,
	getWorkflowList,
	updateWorkflow as updateWorkflowApi,
	deactivateWorkflow as deactivateWorkflowApi,
	activateWorkflow as activateWorkflowApi,
	duplicateWorkflow as duplicateWorkflowApi,
	getWorkflowVersions,
	getWorkflowVersionStages,
	createWorkflowFromVersion as createWorkflowFromVersionApi,
	createStage,
	getStagesByWorkflow,
	combineStages,
	sortStages,
	updateStage,
	deleteStage as deleteStageApi,
	exportWorkflowToExcel,
} from '@/apis/ow';

import { getChecklists } from '@/apis/ow/checklist';
import { queryQuestionnaires } from '@/apis/ow/questionnaire';

// 引入自定义组件
import StagesList from './components/StagesList.vue';
import NewWorkflowForm from './components/NewWorkflowForm.vue';
import StageForm from './components/StageForm.vue';
import { Stage, Workflow, Questionnaire, Checklist } from '#/onboard';

const { t } = useI18n();

// 状态
const workflow = ref<Workflow | null>(null); // 当前操作的工作流
const displayWorkflow = ref<Workflow | null>(null); // 当前展示的工作流（可能是历史版本）
const isEditing = ref(true);
const currentStage = ref<Stage | null>(null);
const isEditingStage = ref(false);
const selectedVersion = ref<string>('');
const selectedWorkflow = ref<string>('');
const versionHistoryData = ref<any[]>([]); // 版本历史数据
const workflowListData = ref<any[]>([]); // 工作流列表数据
const isEditingWorkflow = ref(false);
const isEditingFromHistory = ref(false); // 标识是否从历史版本编辑
const editingWorkflowData = ref<any>(null); // 存储正在编辑的工作流数据（用于历史版本编辑） // 是否为编辑模式
const originalStagesOrder = ref<Stage[]>([]); // 保存拖动前的原始阶段顺序

// API加载状态变量
const loading = reactive({
	workflows: false, // 获取工作流列表
	stages: false, // 获取阶段列表
	versions: false, // 获取版本历史
	createWorkflow: false, // 创建工作流
	updateWorkflow: false, // 更新工作流
	activateWorkflow: false, // 激活工作流
	deactivateWorkflow: false, // 停用工作流
	duplicateWorkflow: false, // 复制工作流
	createStage: false, // 创建阶段
	updateStage: false, // 更新阶段
	deleteStage: false, // 删除阶段
	sortStages: false, // 排序阶段
	combineStages: false, // 合并阶段
	exportWorkflow: false, // 导出工作流
});

// 合并阶段相关状态
const stagesToCombine = ref<string[]>([]);
const combinedStageName = ref<string>('');
const combinedStageGroup = ref<string>('');
const combinedStageDuration = ref<number>(1);

// 对话框状态
const dialogVisible = reactive({
	workflowForm: false,
	stageForm: false,
	versionHistory: false,
	combineStages: false,
});

// 计算对话框标题
const dialogTitle = computed(() => {
	if (isEditingWorkflow.value) {
		const currentWorkflow = isEditingFromHistory.value
			? editingWorkflowData.value
			: workflow.value;
		if (currentWorkflow) {
			const versionInfo = currentWorkflow.version
				? ` (Version ${currentWorkflow.version})`
				: '';
			return `Edit Workflow${versionInfo}`;
		}
	}
	return 'Create New Workflow';
});

// 计算对话框副标题
const dialogSubtitle = computed(() => {
	if (isEditingWorkflow.value) {
		const currentWorkflow = isEditingFromHistory.value
			? editingWorkflowData.value
			: workflow.value;
		if (currentWorkflow) {
			return `Update the details for "${currentWorkflow.name}".`;
		}
	}
	return 'Create a new workflow version for the onboarding process.';
});

// 监听选中版本变化
watch(selectedVersion, (newValue) => {
	if (newValue && workflow.value && newValue !== workflow.value.id) {
		// 如果选择了不同的版本，设置当前工作流
		setCurrentWorkflow(newValue);
	}
});

// 工作流切换处理
const onWorkflowChange = (workflowId: string) => {
	if (workflowId && workflowId !== workflow.value?.id) {
		setCurrentWorkflow(workflowId);
	}
};

const checklists = ref<Checklist[]>([]);
const questionnaires = ref<Questionnaire[]>([]);
const fetchChecklists = async () => {
	try {
		const res = await getChecklists();
		if (res.code === '200') {
			checklists.value = res.data;
		} else {
			checklists.value = [];
		}
	} catch (error) {
		checklists.value = [];
	}
};
const fetchQuestionnaires = async () => {
	try {
		const res = await queryQuestionnaires({
			pageIndex: 1,
			pageSize: 1000,
		});
		if (res.code === '200') {
			questionnaires.value = res?.data?.items || [];
		} else {
			questionnaires.value = [];
		}
	} catch (error) {
		questionnaires.value = [];
	}
};

// 初始化数据
onMounted(async () => {
	// 获取工作流列表数据
	await fetchWorkflows();
	fetchChecklists();
	fetchQuestionnaires();
	if (workflow.value) {
		selectedVersion.value = workflow.value.id;
		selectedWorkflow.value = workflow.value.id;
	}
});

// 获取工作流列表
const fetchWorkflows = async () => {
	try {
		loading.workflows = true;
		const res = await getWorkflowList();
		if (res.code === '200' && res.data && res.data.length > 0) {
			// 获取默认工作流或第一个工作流
			const defaultWorkflow = res.data.find((wf) => wf.isDefault) || res.data[0];

			workflowListData.value = res.data;
			// 设置当前工作流并获取阶段
			if (defaultWorkflow) {
				await setCurrentWorkflow(defaultWorkflow.id);
			}
		} else {
			// 如果没有数据，显示空状态
			workflow.value = null;
		}
	} finally {
		loading.workflows = false;
	}
};

// 设置当前工作流并获取阶段
const setCurrentWorkflow = async (workflowId: string | number) => {
	// 首先从工作流列表中查找
	let selectedWorkflowData = workflowListData.value.find((wf) => wf.id === workflowId);

	// 如果在工作流列表中没找到，可能是版本历史中的工作流，从版本历史中查找
	if (!selectedWorkflowData && versionHistoryData.value.length > 0) {
		selectedWorkflowData = versionHistoryData.value.find((wf) => wf.id === workflowId);
	}

	if (selectedWorkflowData) {
		workflow.value = selectedWorkflowData;
		selectedWorkflow.value = workflowId.toString();
		selectedVersion.value = workflowId.toString();
		// 重置展示工作流，回到当前工作流
		displayWorkflow.value = null;
		// 获取工作流关联的阶段
		await fetchStages(workflowId);
	} else {
		ElMessage.error('Workflow not found');
	}
};

// 获取工作流关联的阶段
const fetchStages = async (workflowId: string | number) => {
	try {
		loading.stages = true;
		const res = await getStagesByWorkflow(workflowId);
		if (res.code === '200' && workflow.value) {
			workflow.value.stages = res.data || [];
		} else {
			ElMessage.error(res.msg || t('sys.api.operationFailed'));
		}
	} finally {
		loading.stages = false;
	}
};

// 获取工作流版本历史
const fetchVersionHistory = async (id: string | number) => {
	try {
		loading.versions = true;
		const res = await getWorkflowVersions(id);
		if (res.code === '200') {
			return res.data;
		} else {
			ElMessage.error(res.msg || t('sys.api.operationFailed'));
			return [];
		}
	} finally {
		loading.versions = false;
	}
};

// 方法
const formatDate = (date: string) => {
	return timeZoneConvert(date, false, projectDate);
};

const showNewWorkflowDialog = () => {
	isEditingWorkflow.value = false;
	isEditingFromHistory.value = false; // 重置历史版本编辑标识
	editingWorkflowData.value = null; // 清空编辑数据
	dialogVisible.workflowForm = true;
};

const showVersionHistory = async () => {
	if (workflow.value) {
		try {
			loading.versions = true;
			const versionData = await fetchVersionHistory(workflow.value.id);
			if (versionData && versionData.length > 0) {
				// 更新版本历史数据
				versionHistoryData.value = versionData;
			} else {
				versionHistoryData.value = [];
			}
			dialogVisible.versionHistory = true;
		} finally {
			loading.versions = false;
		}
	} else {
		versionHistoryData.value = [];
		dialogVisible.versionHistory = true;
	}
};

const selectVersion = async (version: any) => {
	try {
		// 关闭版本历史对话框
		dialogVisible.versionHistory = false;

		// 获取历史版本的stage信息
		if (workflow.value && version.id !== workflow.value.id) {
			// 调用接口获取历史版本的stages
			const stagesRes = await getWorkflowVersionStages(workflow.value.id, version.id);
			if (stagesRes.code === '200') {
				// 设置展示的工作流为历史版本，包含获取到的stages
				displayWorkflow.value = {
					...version,
					stages: stagesRes.data || [],
				};
			} else {
				// 如果获取stages失败，仍然设置版本但使用空的stages
				displayWorkflow.value = {
					...version,
					stages: [],
				};
				ElMessage.warning('Failed to load stages for this version');
			}
		} else {
			// 如果是当前版本，直接使用现有的stages
			displayWorkflow.value = {
				...version,
				stages: version.stages || [],
			};
		}

		ElMessage.success(`Now viewing ${version.name} (Version ${version.version || 'N/A'})`);
	} catch (error) {
		ElMessage.error('Failed to view selected version');
		console.error('Error viewing selected version:', error);
	}
};

const editWorkflowVersion = (version: any) => {
	try {
		// 检查是否已经在编辑状态
		if (dialogVisible.workflowForm) {
			ElMessage.warning(
				'Another workflow is currently being edited. Please close the current dialog first.'
			);
			return;
		}

		// 只存储要编辑的工作流基本信息，不包含 stages，不影响当前显示
		editingWorkflowData.value = {
			id: version.id,
			name: version.name,
			description: version.description,
			startDate: version.startDate,
			endDate: version.endDate,
			status: version.status,
			isDefault: version.isDefault,
			version: version.version,
			// 不包含 stages，避免影响当前页面显示
		};

		// 设置为编辑模式和历史版本编辑标识
		isEditingWorkflow.value = true;
		isEditingFromHistory.value = true;
		// 关闭版本历史对话框
		dialogVisible.versionHistory = false;
		// 打开工作流编辑对话框
		dialogVisible.workflowForm = true;
	} catch (error) {
		console.error('Error opening workflow for editing:', error);
	}
};

const handleCommand = (command: string, currentWorkflow: Workflow) => {
	switch (command) {
		case 'edit':
			isEditingWorkflow.value = true;
			isEditingFromHistory.value = false; // 重置历史版本编辑标识
			editingWorkflowData.value = null; // 清空编辑数据
			dialogVisible.workflowForm = true;
			break;
		case 'setDefault':
			setAsDefault();
			break;
		case 'activate':
			activateWorkflow();
			break;
		case 'deactivate':
			deactivateWorkflow();
			break;
		case 'addStage':
			addStage();
			break;
		case 'duplicate':
			duplicateWorkflow();
			break;
		case 'export':
			exportWorkflow();
			break;
		case 'combineStages':
			showCombineStagesDialog();
			break;
	}
};

// 处理工作流提交
const handleWorkflowSubmit = async (workflowData: Partial<Workflow>) => {
	if (isEditingWorkflow.value) {
		// 检查是否是从历史版本编辑
		if (isEditingFromHistory.value) {
			await createWorkflowFromHistoryVersion(workflowData);
		} else {
			await updateWorkflow(workflowData);
		}
	} else {
		await createWorkflow(workflowData);
	}
};

// 处理工作流取消
const handleWorkflowCancel = () => {
	dialogVisible.workflowForm = false;
	isEditingFromHistory.value = false;
	editingWorkflowData.value = null;
};

const createWorkflow = async (newWorkflow: Partial<Workflow>) => {
	try {
		loading.createWorkflow = true;
		
		// 检查系统中是否已有默认工作流
		let shouldSetAsDefault = newWorkflow.isDefault || false;
		
		// 如果用户没有主动设置为默认，检查系统中是否已有默认工作流
		if (!shouldSetAsDefault) {
			try {
				const workflowRes = await getWorkflowList();
				if (workflowRes.code === '200' && workflowRes.data) {
					const hasDefaultWorkflow = workflowRes.data.some((wf: any) => wf.isDefault);
					// 如果系统中没有默认工作流，自动设置新工作流为默认
					if (!hasDefaultWorkflow || workflowRes.data.length === 0) {
						shouldSetAsDefault = true;
					}
				} else {
					// 如果获取工作流列表失败，假设系统为空，设为默认
					shouldSetAsDefault = true;
				}
			} catch (error) {
				console.warn('Failed to check existing workflows, setting as default:', error);
				// 出错时设为默认，确保系统至少有一个默认工作流
				shouldSetAsDefault = true;
			}
		}

		const params = {
			name: newWorkflow.name || '',
			description: newWorkflow.description || '',
			isDefault: shouldSetAsDefault,
			status: newWorkflow.status || 'Active',
			startDate: timeZoneConvert(newWorkflow.startDate || ''),
			endDate: timeZoneConvert(newWorkflow.endDate || ''),
			isActive: newWorkflow.status === 'active',
			version: 1,
		};

		// 调用创建工作流API
		const res = await createWorkflowApi(params);

		if (res.code === '200') {
			ElMessage.success(t('sys.api.operationSuccess'));
			// 重新获取工作流列表
			dialogVisible.workflowForm = false;
			isEditingFromHistory.value = false; // 重置历史版本编辑标识
			editingWorkflowData.value = null; // 清空编辑数据
			await fetchWorkflows();
		} else {
			ElMessage.error(res.msg || t('sys.api.operationFailed'));
		}
	} finally {
		loading.createWorkflow = false;
	}
};

const createWorkflowFromHistoryVersion = async (updatedWorkflow: Partial<Workflow>) => {
	if (!editingWorkflowData.value || !workflow.value) return;

	try {
		loading.updateWorkflow = true;
		// 准备接口参数 - 使用 createWorkflowFromVersion API
		const params = {
			name: updatedWorkflow.name || editingWorkflowData.value.name,
			description: updatedWorkflow.description || editingWorkflowData.value.description,
			isDefault:
				updatedWorkflow.isDefault !== undefined
					? updatedWorkflow.isDefault
					: editingWorkflowData.value.isDefault,
			status: updatedWorkflow.status || editingWorkflowData.value.status,
			startDate: updatedWorkflow.startDate || editingWorkflowData.value.startDate,
			endDate:
				updatedWorkflow.endDate !== undefined
					? updatedWorkflow.endDate
					: editingWorkflowData.value.endDate,
			isActive: (updatedWorkflow.status || editingWorkflowData.value.status) === 'active',
			versionId: editingWorkflowData.value.id, // 使用历史版本的ID
			originalWorkflowId: workflow.value.id, // 添加原始工作流ID参数
		};

		// 调用从版本创建工作流API
		const res = await createWorkflowFromVersionApi(workflow.value.id, params);

		if (res.code === '200') {
			ElMessage.success('Workflow created successfully from history version');
			// 重新获取工作流列表
			dialogVisible.workflowForm = false;
			isEditingFromHistory.value = false; // 重置历史版本编辑标识
			editingWorkflowData.value = null; // 清空编辑数据
			await fetchWorkflows();
		} else {
			ElMessage.error(res.msg || t('sys.api.operationFailed'));
		}
	} finally {
		loading.updateWorkflow = false;
	}
};

const updateWorkflow = async (updatedWorkflow: Partial<Workflow>) => {
	if (!workflow.value) return;

	try {
		loading.updateWorkflow = true;
		// 准备接口参数
		const params = {
			name: updatedWorkflow.name || workflow.value.name,
			description: updatedWorkflow.description || workflow.value.description,
			isDefault:
				updatedWorkflow.isDefault !== undefined
					? updatedWorkflow.isDefault
					: workflow.value.isDefault,
			status: updatedWorkflow.status || workflow.value.status,
			startDate: updatedWorkflow.startDate || workflow.value.startDate,
			endDate:
				updatedWorkflow.endDate !== undefined
					? updatedWorkflow.endDate
					: workflow.value.endDate,
			isActive: (updatedWorkflow.status || workflow.value.status) === 'active',
			version: workflow.value.version + 1,
		};

		// 调用更新工作流API
		const res = await updateWorkflowApi(workflow.value.id, params);

		if (res.code === '200') {
			ElMessage.success(t('sys.api.operationSuccess'));
			// 重新获取工作流列表
			dialogVisible.workflowForm = false;
			isEditingFromHistory.value = false; // 重置历史版本编辑标识
			editingWorkflowData.value = null; // 清空编辑数据
			await fetchWorkflows();
		} else {
			ElMessage.error(res.msg || t('sys.api.operationFailed'));
		}
	} finally {
		loading.updateWorkflow = false;
	}
};

const activateWorkflow = async () => {
	if (!workflow.value) return;

	// 检查end date是否已过期
	if (workflow.value.endDate) {
		const endDate = new Date(workflow.value.endDate);
		const currentDate = new Date();

		if (endDate < currentDate) {
			// End date已过期，显示警告提示
			ElMessageBox.confirm(
				`⚠️ Warning: The workflow "${
					workflow.value.name
				}" has an expired end date (${formatDate(workflow.value.endDate)}). 

Activating an expired workflow may cause issues with the onboarding process. Do you want to continue activating this workflow?`,
				'End Date Expired',
				{
					confirmButtonText: 'Continue Activation',
					cancelButtonText: 'Cancel',
					confirmButtonClass: 'warning-confirm-btn',
					cancelButtonClass: 'cancel-confirm-btn',
					distinguishCancelAndClose: true,
					customClass: 'expired-date-confirmation-dialog',
					showCancelButton: true,
					showConfirmButton: true,
					beforeClose: async (action, instance, done) => {
						if (action === 'confirm') {
							// 显示loading状态
							instance.confirmButtonLoading = true;
							instance.confirmButtonText = 'Activating...';

							try {
								// 调用激活工作流API
								const res = await activateWorkflowApi(workflow.value!.id);

								if (res.code === '200') {
									ElMessage.success(t('sys.api.operationSuccess'));
									// 更新本地状态
									workflow.value!.status = 'active';
									workflow.value!.isActive = true;
									done(); // 关闭对话框
								} else {
									ElMessage.error(res.msg || t('sys.api.operationFailed'));
									// 恢复按钮状态
									instance.confirmButtonLoading = false;
									instance.confirmButtonText = 'Continue Activation';
								}
							} catch (error) {
								ElMessage.error(t('sys.api.operationFailed'));
								// 恢复按钮状态
								instance.confirmButtonLoading = false;
								instance.confirmButtonText = 'Continue Activation';
							}
						} else {
							done(); // 取消或关闭时直接关闭对话框
						}
					},
				}
			);
			return; // 直接返回，不执行后面的代码
		}
	}

	// 如果没有end date或者end date未过期，直接激活
	try {
		loading.activateWorkflow = true;
		// 调用激活工作流API
		const res = await activateWorkflowApi(workflow.value.id);

		if (res.code === '200') {
			ElMessage.success(t('sys.api.operationSuccess'));
			// 更新本地状态
			workflow.value.status = 'active';
			workflow.value.isActive = true;
		} else {
			ElMessage.error(res.msg || t('sys.api.operationFailed'));
		}
	} finally {
		loading.activateWorkflow = false;
	}
};

const deactivateWorkflow = async () => {
	if (!workflow.value) return;

	ElMessageBox.confirm(
		`Are you sure you want to set the workflow "${workflow.value.name}" as inactive? This will stop all active processes and cannot be easily undone.`,
		'⚠️ Confirm Workflow Deactivation',
		{
			confirmButtonText: 'Set as Inactive',
			cancelButtonText: 'Cancel',
			confirmButtonClass: 'warning-confirm-btn',
			cancelButtonClass: 'cancel-confirm-btn',
			distinguishCancelAndClose: true,
			customClass: 'deactivate-confirmation-dialog',
			showCancelButton: true,
			showConfirmButton: true,
			beforeClose: async (action, instance, done) => {
				if (action === 'confirm') {
					// 显示loading状态
					instance.confirmButtonLoading = true;
					instance.confirmButtonText = 'Deactivating...';

					try {
						// 调用停用工作流API
						const res = await deactivateWorkflowApi(workflow.value!.id);

						if (res.code === '200') {
							ElMessage.success(t('sys.api.operationSuccess'));
							// 更新本地状态
							workflow.value!.status = 'inactive';
							workflow.value!.isActive = false;
							workflow.value!.endDate = new Date().toISOString();
							done(); // 关闭对话框
						} else {
							ElMessage.error(res.msg || t('sys.api.operationFailed'));
							// 恢复按钮状态
							instance.confirmButtonLoading = false;
							instance.confirmButtonText = 'Set as Inactive';
						}
					} catch (error) {
						ElMessage.error(t('sys.api.operationFailed'));
						// 恢复按钮状态
						instance.confirmButtonLoading = false;
						instance.confirmButtonText = 'Set as Inactive';
					}
				} else {
					done(); // 取消或关闭时直接关闭对话框
				}
			},
		}
	);
};

const setAsDefault = async () => {
	if (!workflow.value) return;

	try {
		loading.updateWorkflow = true;
		// 调用设置默认工作流API
		const params = {
			...workflow.value,
			isDefault: true,
		};

		const res = await updateWorkflowApi(workflow.value.id, params);

		if (res.code === '200') {
			ElMessage.success(t('sys.api.operationSuccess'));
			// 重新获取工作流列表以更新所有工作流的默认状态
			await fetchWorkflows();
		} else {
			ElMessage.error(res.msg || t('sys.api.operationFailed'));
		}
	} catch (err) {
		ElMessage.error(err);
	} finally {
		loading.updateWorkflow = false;
	}
};

const addStage = () => {
	currentStage.value = null;
	isEditingStage.value = false;
	dialogVisible.stageForm = true;
};

const editStage = (stage: Stage) => {
	currentStage.value = { ...stage };
	isEditingStage.value = true;
	dialogVisible.stageForm = true;
};

const submitStage = async (stage: Partial<Stage>) => {
	if (!workflow.value) return;
	try {
		if (isEditingStage.value && currentStage.value) {
			// 更新阶段
			loading.updateStage = true;
			const params = {
				workflowId: workflow.value.id,
				...stage,
			};

			const res = await updateStage(currentStage.value.id, params);

			if (res.code === '200') {
				ElMessage.success(t('sys.api.operationSuccess'));
				// 重新获取阶段列表
				await fetchStages(workflow.value.id);
			} else {
				ElMessage.error(res.msg || t('sys.api.operationFailed'));
			}
			loading.updateStage = false;
		} else {
			// 创建阶段
			loading.createStage = true;
			const params = {
				workflowId: workflow.value.id,
				...stage,
			};

			const res = await createStage(params);

			if (res.code === '200') {
				ElMessage.success(t('sys.api.operationSuccess'));
				// 重新获取阶段列表
				await fetchStages(workflow.value.id);
			} else {
				ElMessage.error(res.msg || t('sys.api.operationFailed'));
			}
			loading.createStage = false;
		}

		dialogVisible.stageForm = false;
	} finally {
		loading.createStage = false;
		loading.updateStage = false;
	}
};

const deleteStage = async (stageId: string) => {
	if (!workflow.value) return;

	// 找到要删除的阶段名称
	const stageToDelete = workflow.value.stages.find((stage) => stage.id === stageId);
	const stageName = stageToDelete ? stageToDelete.name : 'this stage';

	ElMessageBox.confirm(
		`Are you sure you want to delete the stage "${stageName}"? This action cannot be undone.`,
		'⚠️ Confirm Stage Deletion',
		{
			confirmButtonText: 'Delete Stage',
			cancelButtonText: 'Cancel',
			confirmButtonClass: 'danger-confirm-btn',
			cancelButtonClass: 'cancel-confirm-btn',
			distinguishCancelAndClose: true,
			customClass: 'delete-confirmation-dialog',
			showCancelButton: true,
			showConfirmButton: true,
			beforeClose: async (action, instance, done) => {
				if (action === 'confirm') {
					// 显示loading状态
					instance.confirmButtonLoading = true;
					instance.confirmButtonText = 'Deleting...';

					try {
						// 调用删除阶段API
						const res = await deleteStageApi(stageId, true);

						if (res.code === '200') {
							ElMessage.success(t('sys.api.operationSuccess'));
							// 重新获取阶段列表
							await fetchStages(workflow.value!.id);
							done(); // 关闭对话框
						} else {
							ElMessage.error(res.msg || t('sys.api.operationFailed'));
							// 恢复按钮状态
							instance.confirmButtonLoading = false;
							instance.confirmButtonText = 'Delete Stage';
						}
					} catch (error) {
						ElMessage.error(t('sys.api.operationFailed'));
						// 恢复按钮状态
						instance.confirmButtonLoading = false;
						instance.confirmButtonText = 'Delete Stage';
					}
				} else {
					done(); // 取消或关闭时直接关闭对话框
				}
			},
		}
	);
};

// 在拖动开始前保存原始顺序
const onDragStart = () => {
	if (workflow.value) {
		originalStagesOrder.value = [...workflow.value.stages];
	}
};

const updateStagesOrder = async () => {
	if (!workflow.value) return;

	try {
		loading.sortStages = true;
		const params = {
			workflowId: workflow.value.id,
			stageOrders: workflow.value.stages.map((stage, index) => ({
				stageId: stage.id,
				order: index + 1,
			})),
		};

		const res = await sortStages(params);

		if (res.code === '200') {
			ElMessage.success(t('sys.api.operationSuccess'));
			// 更新本地状态
			workflow.value.stages.forEach((stage, index) => {
				stage.order = index + 1;
			});
			// 清空原始顺序备份
			originalStagesOrder.value = [];
		} else {
			ElMessage.error(res.msg || t('sys.api.operationFailed'));
			// API调用失败，恢复原始顺序
			if (originalStagesOrder.value.length > 0) {
				workflow.value.stages = [...originalStagesOrder.value];
				originalStagesOrder.value = [];
			}
		}
	} catch (error) {
		ElMessage.error(t('sys.api.operationFailed'));
		// 发生异常，恢复原始顺序
		if (originalStagesOrder.value.length > 0) {
			workflow.value.stages = [...originalStagesOrder.value];
			originalStagesOrder.value = [];
		}
	} finally {
		loading.sortStages = false;
	}
};

/**
 * 根据MIME类型获取文件扩展名
 * @param mimeType MIME类型
 * @returns 文件扩展名
 */
const getFileExtensionFromMimeType = (mimeType: string): string => {
	const mimeToExtension: Record<string, string> = {
		'application/pdf': 'pdf',
		'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet': 'xlsx',
		'application/vnd.ms-excel': 'xls',
		'application/vnd.openxmlformats-officedocument.wordprocessingml.document': 'docx',
		'application/msword': 'doc',
		'text/csv': 'csv',
		'application/json': 'json',
		'text/plain': 'txt',
		'application/zip': 'zip',
	};

	return mimeToExtension[mimeType] || 'file';
};

/**
 * 下载文件流
 * @param blob 文件流
 * @param baseFileName 基础文件名（不含扩展名）
 * @returns 是否下载成功
 */
const downloadFileFromBlob = (blob: Blob, baseFileName: string): boolean => {
	try {
		// 根据Blob的MIME类型确定文件扩展名
		const extension = getFileExtensionFromMimeType(blob.type);
		const fileName = `${baseFileName}.${extension}`;

		// 创建下载链接
		const url = URL.createObjectURL(blob);
		const link = document.createElement('a');
		link.href = url;
		link.download = fileName;
		document.body.appendChild(link);
		link.click();

		// 清理资源
		document.body.removeChild(link);
		URL.revokeObjectURL(url);

		return true;
	} catch (error) {
		console.error('Download failed:', error);
		return false;
	}
};

const exportWorkflow = async () => {
	if (!workflow.value) return;

	try {
		loading.exportWorkflow = true;
		// 调用导出工作流API，直接返回文件流
		const res = await exportWorkflowToExcel(workflow.value.id);

		// 检查返回的是否为文件流
		if (res instanceof Blob) {
			// 生成基础文件名（不含扩展名）
			const baseFileName = `${workflow.value.name}_workflow_${
				new Date().toISOString().split('T')[0]
			}`;

			// 下载文件，自动检测文件类型
			const success = downloadFileFromBlob(res, baseFileName);

			if (success) {
				ElMessage.success(t('sys.api.operationSuccess'));
			} else {
				ElMessage.error(t('sys.api.operationFailed'));
			}
		} else {
			// 如果不是文件流，可能是错误响应
			ElMessage.error(res.msg || t('sys.api.operationFailed'));
		}
	} finally {
		loading.exportWorkflow = false;
	}
};

const duplicateWorkflow = async () => {
	if (!workflow.value) return;

	try {
		loading.duplicateWorkflow = true;
		const params = {
			name: `${workflow.value.name} (Copy)`,
			description: workflow.value.description,
			setAsDefault: false,
		};

		const res = await duplicateWorkflowApi(workflow.value.id, params);

		if (res.code === '200') {
			ElMessage.success(t('sys.api.operationSuccess'));
			// 重新获取工作流列表
			await fetchWorkflows();
		} else {
			ElMessage.error(res.msg || t('sys.api.operationFailed'));
		}
	} finally {
		loading.duplicateWorkflow = false;
	}
};

const showCombineStagesDialog = () => {
	resetCombineStagesForm();
	dialogVisible.combineStages = true;
};

const getWorkflowStages = () => {
	if (!workflow.value) return [];
	return workflow.value.stages;
};

const getAvatarColor = (name: string) => {
	const colors = [
		'#4F46E5',
		'#0EA5E9',
		'#10B981',
		'#F59E0B',
		'#EC4899',
		'#8B5CF6',
		'#06B6D4',
		'#14B8A6',
		'#F43F5E',
		'#22C55E',
		'#3B82F6',
		'#A855F7',
	];

	let hash = 0;
	for (let i = 0; i < name.length; i++) {
		hash = name.charCodeAt(i) + ((hash << 5) - hash);
	}

	return colors[Math.abs(hash) % colors.length];
};

const combineSelectedStages = async () => {
	if (stagesToCombine.value.length < 2 || !combinedStageName.value || !workflow.value) {
		return;
	}

	try {
		loading.combineStages = true;
		const params = {
			stageIds: stagesToCombine.value,
			newStageName: combinedStageName.value,
			description: '从多个阶段合并而来',
			defaultAssignedGroup: combinedStageGroup.value,
			defaultAssignee: '',
			estimatedDuration: combinedStageDuration.value,
			color: getAvatarColor(combinedStageName.value),
		};

		const res = await combineStages(params);

		if (res.code === '200') {
			ElMessage.success(t('sys.api.operationSuccess'));
			// 重新获取阶段列表
			await fetchStages(workflow.value.id);
			resetCombineStagesForm();
			dialogVisible.combineStages = false;
		} else {
			ElMessage.error(res.msg || t('sys.api.operationFailed'));
		}
	} finally {
		loading.combineStages = false;
	}
};

const resetCombineStagesForm = () => {
	stagesToCombine.value = [];
	combinedStageName.value = '';
	combinedStageGroup.value = '';
	combinedStageDuration.value = 1;
};
</script>

<style lang="scss" scoped>
.workflow-container {
	margin: 0 auto;
	height: 100%;
}

.loading-container {
	padding: 24px;
	background-color: #fff;
	margin-bottom: 24px;
}

.workflow-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	padding: 0 24px 24px 24px;
}

.title {
	font-size: 24px;
	color: var(--primary-500, #2468f2);
	margin: 0;
	font-weight: 700;
}

.actions {
	display: flex;
	gap: 10px;
	align-items: center;
}

.workflow-card {
	margin-bottom: 16px;
	border: 1px solid var(--el-border-color-light, #e6edf7);
	box-shadow: 0 1px 3px 0 rgba(0, 0, 0, 0.1);
	transition: all 0.3s ease;
	background-color: #fff;
	overflow: hidden;
	display: flex;
	flex-direction: column;
}

.workflow-card.active {
	border-color: var(--primary-100, #e6f7ff);
}

.workflow-card-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	padding: 24px;
	border-bottom: 1px solid var(--el-border-color-light, #e6edf7);
	background-color: var(--primary-50, #f0f5ff);
}

.left-section {
	display: flex;
	align-items: flex-start;
	flex-direction: column;
	gap: 5px;
	max-width: 60%;
}

.title-and-tags {
	display: flex;
	align-items: center;
	gap: 10px;
	flex-wrap: wrap;
}

.workflow-name {
	font-weight: 600;
	font-size: 18px;
	color: #303133;
}

.workflow-desc {
	color: #606266;
	font-size: 13px;
	margin-top: 4px;
}

.right-section {
	display: flex;
	align-items: center;
	gap: 16px;
}

.workflow-selector {
	min-width: 250px;
}

.more-actions-btn {
	display: flex;
	align-items: center;
	justify-content: center;
	width: 36px;
	height: 36px;
	padding: 0;
	border: 1px solid var(--el-border-color-light, #e4e7ed);
}

.actions-dropdown {
	min-width: 180px;
}

.actions-dropdown :deep(.el-dropdown-menu__item) {
	display: flex;
	align-items: center;
	gap: 8px;
}

.el-tag--success {
	background-color: transparent;
	border-color: var(--el-border-color-light, #e6edf7);
	color: #606266;
	margin-left: 8px;
	padding: 2px 8px;
	font-size: 11px;
}

.el-tag--info {
	background-color: transparent;
	border-color: var(--el-border-color-light, #e6edf7);
	color: #606266;
	margin-left: 8px;
	padding: 2px 8px;
	font-size: 11px;
}

.default-tag {
	background: linear-gradient(to right, var(--yellow-400, #f59e0b), var(--yellow-500, #e6a23c));
	color: white;
	border-color: transparent;
	padding: 2px 8px;
	font-size: 11px;
	display: inline-flex;
	align-items: center;
	margin-left: 8px;
}

.star-icon {
	color: white;
	margin-right: 4px;
	width: 12px;
	height: 12px;
}

.inactive-icon {
	color: #f56c6c;
	font-size: 14px;
}

.calendar-icon {
	color: var(--primary-500);
	margin-right: 0;
	font-size: 16px;
}

.delete-item {
	color: var(--red-500, #f56c6c);
}

.workflow-card-body {
	padding: 16px 10px 16px 24px;
	background-color: rgba(var(--primary-500-rgb, 36, 104, 242), 0.05);
	flex: 1;
}

.workflow-header-actions {
	display: flex;
	justify-content: space-between;
	align-items: center;
}

.dates-container {
	display: flex;
	align-items: center;
	gap: 16px;
	color: #606266;
	font-size: 14px;
}

.date-item {
	display: flex;
	align-items: center;
	gap: 4px;
}

.date-label {
	font-weight: 500;
	color: #303133;
	margin-right: 2px;
}

.date-value {
	color: #606266;
}

.stages-header {
	display: flex;
	justify-content: right;
	margin: 16px 0;
}

.stages-header h3 {
	margin: 0;
	font-size: 16px;
	color: #303133;
	font-weight: 500;
}

.stages-header-actions {
	display: flex;
	align-items: center;
	gap: 12px;
}

.viewing-history-tag {
	font-size: 12px;
}

.back-to-current-btn {
	font-size: 12px;
	padding: 4px 8px;
}

.version-tag {
	font-size: 12px;
}

.add-stage-btn {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	font-size: 12px;
	background-color: transparent;
	border: 1px solid var(--primary-500, #2468f2);
	color: var(--primary-500, #2468f2);
	border-radius: 4px;
	padding: 6px 12px;
	cursor: pointer;
	transition: all 0.2s ease;
}

.add-stage-btn:hover {
	background-color: rgba(var(--primary-500-rgb, 36, 104, 242), 0.1);
}

.drag-handle {
	cursor: move;
	color: #909399;
}

.ghost-workflow {
	opacity: 0.5;
	background: #c8ebfb;
}

.version-history-placeholder {
	padding: 10px;
}

:deep(.el-icon) {
	vertical-align: middle;
}

.workflow-footer {
	display: flex;
	justify-content: flex-start;
	align-items: center;
	padding: 16px 24px;
	background-color: #f8fafc;
	border-top: 1px solid #edf2f7;
	margin: 0;
}

.stage-count {
	color: #64748b;
	font-size: 14px;
	font-weight: 400;
	margin: 0;
}

.text-primary {
	color: var(--primary-500, #2468f2);
}

.combine-stages-form {
	padding: 0 10px;
}

.text-muted {
	color: #64748b;
}

.stage-item-select {
	padding: 8px;
	margin-bottom: 4px;
	border-radius: 4px;
	transition: background-color 0.2s;
}

.stage-item-select:hover {
	background-color: #f8fafc;
}

.stage-color-indicator {
	width: 12px;
	height: 12px;
	border-radius: 50%;
	margin-right: 8px;
	display: inline-block;
}

.space-y-4 > * + * {
	margin-top: 1rem;
}

.flex.justify-end {
	display: flex;
	justify-content: flex-end;
}

.space-x-2 > * + * {
	margin-left: 0.5rem;
}

.mt-6 {
	margin-top: 1.5rem;
}

.mb-4 {
	margin-bottom: 1rem;
}

.w-full {
	width: 100%;
}

.block {
	display: block;
}

.text-sm {
	font-size: 0.875rem;
}

.font-medium {
	font-weight: 500;
}

.mb-1 {
	margin-bottom: 0.25rem;
}

.disabled-btn {
	opacity: 0.6;
	cursor: not-allowed;
}

.dialog-header {
	border-bottom: none;
}

.dialog-title {
	font-size: 18px;
	font-weight: 600;
	color: #303133;
	margin: 0 0 4px 0;
}

.dialog-subtitle {
	color: #606266;
	font-size: 13px;
	margin: 0;
	font-weight: normal;
	line-height: 1.4;
}

:deep(.workflow-dialog .el-dialog__header) {
	padding: 0;
	margin-right: 0;
	border-bottom: none;
}

:deep(.workflow-dialog .el-dialog__headerbtn) {
	top: 16px;
	right: 16px;
	z-index: 10;
}

:deep(.workflow-dialog .el-dialog__body) {
	padding: 24px;
}

/* 版本历史对话框样式 */
:deep(.version-history-dialog) {
	border-radius: 12px;
	overflow: hidden;
}

:deep(.version-history-dialog .el-dialog__header) {
	padding: 0;
	margin-right: 0;
	border-bottom: 1px solid #f0f0f0;
}

:deep(.version-history-dialog .el-dialog__headerbtn) {
	top: 20px;
	right: 20px;
	z-index: 10;
}

:deep(.version-history-dialog .el-dialog__body) {
	padding: 0;
}

:deep(.version-history-dialog .el-dialog__footer) {
	padding: 20px 24px;
	border-top: 1px solid #f0f0f0;
	background-color: #fafafa;
}

.version-dialog-header {
	padding: 24px 24px 20px 24px;
	background-color: #fff;
}

.version-dialog-title {
	font-size: 20px;
	font-weight: 600;
	color: #1a1a1a;
	margin: 0 0 8px 0;
	line-height: 1.2;
}

.version-dialog-subtitle {
	color: #6b7280;
	font-size: 14px;
	margin: 0;
	font-weight: normal;
	line-height: 1.4;
}

.version-history-content {
	padding: 0;
	background-color: #fff;
}

.version-history-table {
	width: 100%;
}

:deep(.version-table-header) {
	background-color: #f8fafc;
	border-bottom: 1px solid #e5e7eb;
}

:deep(.version-table-header th) {
	background-color: #f8fafc !important;
	color: #374151;
	font-weight: 600;
	font-size: 13px;
	padding: 16px 12px;
	border-bottom: 1px solid #e5e7eb;
}

:deep(.version-table-row td) {
	padding: 16px 12px;
	border-bottom: 1px solid #f3f4f6;
	vertical-align: middle;
}

:deep(.version-table-row:hover td) {
	background-color: #f9fafb;
}

.version-name {
	font-weight: 500;
	color: #1f2937;
	font-size: 14px;
}

.status-tag {
	font-weight: 500;
	font-size: 12px;
}

.default-tag {
	background: linear-gradient(135deg, #fbbf24, #f59e0b);
	color: white;
	border: none;
	font-weight: 500;
	font-size: 12px;
}

.date-text {
	color: #6b7280;
	font-size: 13px;
}

.created-by {
	color: #374151;
	font-size: 13px;
	font-weight: 500;
}

.action-buttons {
	display: flex;
	align-items: center;
	gap: 8px;
}

.view-btn {
	font-size: 13px;
	font-weight: 500;
}

.edit-btn {
	padding: 4px;
	border-radius: 4px;
	transition: all 0.2s ease;
}

.edit-btn:hover {
	background-color: #f3f4f6;
}

.version-dialog-footer {
	display: flex;
	justify-content: flex-end;
	margin: 0;
}

.close-btn {
	color: #6b7280;
	border-color: #d1d5db;
	background-color: #fff;
	font-weight: 500;
	padding: 8px 16px;
}

.close-btn:hover {
	background-color: #f9fafb;
	border-color: #9ca3af;
}

.new-workflow-btn-footer {
	background: linear-gradient(135deg, #3b82f6, #2563eb);
	border: none;
	color: white;
	font-weight: 500;
	padding: 8px 16px;
	display: flex;
	align-items: center;
	gap: 6px;
	transition: all 0.2s ease;
}

.new-workflow-btn-footer:hover {
	background: linear-gradient(135deg, #2563eb, #1d4ed8);
	transform: translateY(-1px);
	box-shadow: 0 4px 12px rgba(59, 130, 246, 0.3);
}

.mr-1 {
	margin-right: 4px;
}

/* 改善下拉菜单的可访问性 */
:deep(.el-dropdown__popper) {
	z-index: 9999;
}

:deep(.el-dropdown__popper[aria-hidden='true']) {
	visibility: hidden;
	pointer-events: none;
}

:deep(.el-dropdown__popper[aria-hidden='false']) {
	visibility: visible;
	pointer-events: auto;
}

:deep(.el-dropdown-menu__item:focus) {
	background-color: var(--el-dropdown-menuItem-hover-fill);
	color: var(--el-dropdown-menuItem-hover-color);
	outline: 2px solid var(--el-color-primary);
	outline-offset: -2px;
}

.version-badge {
	font-size: 12px;
	opacity: 0.7;
}
</style>

<!-- 全局样式用于 MessageBox 删除确认对话框 -->
<style lang="scss">
/* 删除确认对话框样式 */
.delete-confirmation-dialog {
	/* border-radius removed - using rounded-md class */

	.el-message-box__message {
		color: #606266;
		font-size: 14px;
		line-height: 1.5;
	}
}

/* 停用确认对话框样式 */
.deactivate-confirmation-dialog {
	.el-message-box__message {
		color: #606266;
		font-size: 14px;
		line-height: 1.5;
	}
}

/* 过期日期确认对话框样式 */
.expired-date-confirmation-dialog {
	.el-message-box__message {
		color: #606266;
		font-size: 14px;
		line-height: 1.5;
		white-space: pre-line; /* 保持换行格式 */
	}
}

/* 确保删除按钮为纯红色 - 仅限于删除确认对话框 */
.delete-confirmation-dialog {
	.el-message-box__btns .danger-confirm-btn,
	.danger-confirm-btn {
		background: var(--el-color-danger) !important;
		background-color: var(--el-color-danger) !important;
		background-image: none !important;
		border-color: var(--el-color-danger) !important;
		color: #ffffff !important;
		opacity: 1 !important;
		box-shadow: none !important;

		&:hover,
		&:focus {
			background: var(--el-color-danger-light-3) !important;
			background-color: var(--el-color-danger-light-3) !important;
			background-image: none !important;
			border-color: var(--el-color-danger-light-3) !important;
			color: #ffffff !important;
			opacity: 1 !important;
			box-shadow: 0 0 0 2px var(--el-color-danger-light-8) !important;
		}

		&:active {
			background: var(--el-color-danger-dark-2) !important;
			background-color: var(--el-color-danger-dark-2) !important;
			background-image: none !important;
			border-color: var(--el-color-danger-dark-2) !important;
			color: #ffffff !important;
			opacity: 1 !important;
		}

		&::before,
		&::after {
			display: none !important;
		}
	}

	/* 额外的强制样式，处理 Element Plus 的默认样式覆盖 */
	.el-button--primary.danger-confirm-btn,
	.el-message-box__btns .el-button--primary.danger-confirm-btn {
		background: var(--el-color-danger) !important;
		background-color: var(--el-color-danger) !important;
		background-image: none !important;
		border: 1px solid var(--el-color-danger) !important;
		border-color: var(--el-color-danger) !important;
		color: #ffffff !important;

		&:not(:disabled):not(.is-disabled) {
			background: var(--el-color-danger) !important;
			background-color: var(--el-color-danger) !important;
			background-image: none !important;
			border-color: var(--el-color-danger) !important;
			color: #ffffff !important;
		}
	}
}

/* 取消按钮样式 - 仅限于删除确认对话框 */
.delete-confirmation-dialog {
	.cancel-confirm-btn {
		background-color: #ffffff !important;
		border-color: #dcdfe6 !important;
		color: #606266 !important;

		&:hover {
			background-color: #f5f7fa !important;
			border-color: #c0c4cc !important;
			color: #606266 !important;
		}
	}
}

/* 停用确认对话框的警告按钮样式 */
.deactivate-confirmation-dialog {
	.el-message-box__btns .warning-confirm-btn,
	.warning-confirm-btn {
		background: var(--el-color-warning) !important;
		background-color: var(--el-color-warning) !important;
		background-image: none !important;
		border-color: var(--el-color-warning) !important;
		color: #ffffff !important;
		opacity: 1 !important;
		box-shadow: none !important;

		&:hover,
		&:focus {
			background: var(--el-color-warning-light-3) !important;
			background-color: var(--el-color-warning-light-3) !important;
			background-image: none !important;
			border-color: var(--el-color-warning-light-3) !important;
			color: #ffffff !important;
			opacity: 1 !important;
			box-shadow: 0 0 0 2px var(--el-color-warning-light-8) !important;
		}

		&:active {
			background: var(--el-color-warning-dark-2) !important;
			background-color: var(--el-color-warning-dark-2) !important;
			background-image: none !important;
			border-color: var(--el-color-warning-dark-2) !important;
			color: #ffffff !important;
			opacity: 1 !important;
		}

		&::before,
		&::after {
			display: none !important;
		}
	}

	/* 额外的强制样式，处理 Element Plus 的默认样式覆盖 */
	.el-button--primary.warning-confirm-btn,
	.el-message-box__btns .el-button--primary.warning-confirm-btn {
		background: var(--el-color-warning) !important;
		background-color: var(--el-color-warning) !important;
		background-image: none !important;
		border: 1px solid var(--el-color-warning) !important;
		border-color: var(--el-color-warning) !important;
		color: #ffffff !important;

		&:not(:disabled):not(.is-disabled) {
			background: var(--el-color-warning) !important;
			background-color: var(--el-color-warning) !important;
			background-image: none !important;
			border-color: var(--el-color-warning) !important;
			color: #ffffff !important;
		}
	}

	/* 取消按钮样式 */
	.cancel-confirm-btn {
		background-color: #ffffff !important;
		border-color: #dcdfe6 !important;
		color: #606266 !important;

		&:hover {
			background-color: #f5f7fa !important;
			border-color: #c0c4cc !important;
			color: #606266 !important;
		}
	}
}

/* 过期日期确认对话框的警告按钮样式 */
.expired-date-confirmation-dialog {
	.el-message-box__btns .warning-confirm-btn,
	.warning-confirm-btn {
		background: var(--el-color-warning) !important;
		background-color: var(--el-color-warning) !important;
		background-image: none !important;
		border-color: var(--el-color-warning) !important;
		color: #ffffff !important;
		opacity: 1 !important;
		box-shadow: none !important;

		&:hover,
		&:focus {
			background: var(--el-color-warning-light-3) !important;
			background-color: var(--el-color-warning-light-3) !important;
			background-image: none !important;
			border-color: var(--el-color-warning-light-3) !important;
			color: #ffffff !important;
			opacity: 1 !important;
			box-shadow: 0 0 0 2px var(--el-color-warning-light-8) !important;
		}

		&:active {
			background: var(--el-color-warning-dark-2) !important;
			background-color: var(--el-color-warning-dark-2) !important;
			background-image: none !important;
			border-color: var(--el-color-warning-dark-2) !important;
			color: #ffffff !important;
			opacity: 1 !important;
		}

		&::before,
		&::after {
			display: none !important;
		}
	}

	/* 额外的强制样式，处理 Element Plus 的默认样式覆盖 */
	.el-button--primary.warning-confirm-btn,
	.el-message-box__btns .el-button--primary.warning-confirm-btn {
		background: var(--el-color-warning) !important;
		background-color: var(--el-color-warning) !important;
		background-image: none !important;
		border: 1px solid var(--el-color-warning) !important;
		border-color: var(--el-color-warning) !important;
		color: #ffffff !important;

		&:not(:disabled):not(.is-disabled) {
			background: var(--el-color-warning) !important;
			background-color: var(--el-color-warning) !important;
			background-image: none !important;
			border-color: var(--el-color-warning) !important;
			color: #ffffff !important;
		}
	}

	/* 取消按钮样式 */
	.cancel-confirm-btn {
		background-color: #ffffff !important;
		border-color: #dcdfe6 !important;
		color: #606266 !important;

		&:hover {
			background-color: #f5f7fa !important;
			border-color: #c0c4cc !important;
			color: #606266 !important;
		}
	}
}
</style>
