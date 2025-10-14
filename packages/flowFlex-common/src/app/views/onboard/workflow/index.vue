<template>
	<div class="workflow-container">
		<!-- 标题和操作区 -->
		<PageHeader
			title="Workflows"
			description="Design and manage business workflows with customizable stages and automated processes"
		>
			<template #actions>
				<el-button
					type="primary"
					@click="showNewWorkflowDialog"
					:disabled="loading.createWorkflow"
					class="page-header-btn page-header-btn-primary"
				>
					<el-icon v-if="loading.createWorkflow">
						<Loading />
					</el-icon>
					<el-icon v-else>
						<Plus />
					</el-icon>
					<span>New Workflow</span>
				</el-button>
			</template>
		</PageHeader>

		<!-- 主要内容区 -->
		<div>
			<!-- 加载中状态 -->
			<div
				v-if="loading.workflows"
				class="loading-container rounded-xl bg-el-bg-color dark:bg-el-bg-color"
			>
				<el-skeleton style="width: 100%" :rows="10" animated />
			</div>

			<!-- 工作流内容 -->
			<div class="workflow-list" v-else-if="workflow">
				<div
					class="workflow-card rounded-xl bg-el-bg-color border border-el-border-color-light dark:border-el-border-color"
					:class="{ active: workflow.isActive }"
				>
					<div
						class="workflow-card-header bg-el-color-primary-light-9 dark:bg-el-fill-color border-b border-el-border-color-light dark:border-el-border-color"
					>
						<div class="left-section">
							<div class="title-and-tags">
								<span
									class="workflow-name text-el-text-color-primary dark:text-el-text-color-primary"
								>
									{{ workflow.name }}
								</span>
								<el-tag
									v-if="workflow.isAIGenerated"
									type="primary"
									size="small"
									class="ai-tag rounded-xl"
								>
									<div class="flex items-center gap-1">
										<span class="ai-sparkles">✨</span>
										AI
									</div>
								</el-tag>
								<el-tag
									v-if="workflow.isDefault"
									type="warning"
									size="small"
									class="default-tag rounded-xl"
								>
									<div class="flex items-center gapx-2">
										<StarIcon class="star-icon" />
										Default
									</div>
								</el-tag>
								<el-tag
									v-if="workflow.status === 'active'"
									type="success"
									size="small"
									class="rounded-xl"
								>
									Active
								</el-tag>
								<el-tag v-else type="danger" size="small" class="rounded-xl">
									Inactive
								</el-tag>
							</div>
							<span
								class="workflow-desc text-el-text-color-regular dark:text-el-text-color-secondary"
							>
								{{ workflow.description }}
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
										<div class="flex items-center">
											<el-icon v-if="workflowItem.id === workflow?.id">
												<Check />
											</el-icon>
											<div class="max-w-[250px] truncate">
												{{ workflowItem.name }}
											</div>
										</div>
										<div class="flex items-center gap-1">
											<span
												v-if="workflowItem.isAIGenerated"
												class="ai-dropdown-sparkles"
											>
												✨
											</span>
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
								@command="(cmd) => workflow && handleCommand(cmd)"
								:disabled="
									loading.activateWorkflow ||
									loading.deactivateWorkflow ||
									loading.duplicateWorkflow ||
									loading.updateWorkflow ||
									loading.exportWorkflow
								"
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
									class="more-actions-btn rounded-xl"
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
									<el-icon v-else>
										<MoreFilled />
									</el-icon>
								</el-button>
								<template #dropdown>
									<el-dropdown-menu class="actions-dropdown">
										<el-dropdown-item command="edit">
											<el-icon>
												<Edit />
											</el-icon>
											Edit Workflow
										</el-dropdown-item>
										<el-dropdown-item
											v-if="
												!workflow.isDefault && workflow.status === 'active'
											"
											divided
											command="setDefault"
										>
											<el-icon>
												<Star />
											</el-icon>
											Set as Default
										</el-dropdown-item>
										<el-dropdown-item
											v-if="workflow.status === 'active'"
											command="deactivate"
										>
											<el-icon>
												<CircleClose />
											</el-icon>
											Set as Inactive
										</el-dropdown-item>
										<el-dropdown-item
											v-if="workflow.status === 'inactive'"
											command="activate"
										>
											<el-icon>
												<Check />
											</el-icon>
											Set as Active
										</el-dropdown-item>
										<el-dropdown-item divided command="duplicate">
											<el-icon>
												<CopyDocument />
											</el-icon>
											Duplicate
										</el-dropdown-item>
										<el-dropdown-item command="addStage">
											<el-icon>
												<Plus />
											</el-icon>
											Add Stage
										</el-dropdown-item>

										<!-- <el-dropdown-item
											command="combineStages"
											:disabled="workflow.stages.length < 2"
										>
											<el-icon><Connection /></el-icon>
											Combine Stages
										</el-dropdown-item> -->
										<el-dropdown-item divided>
											<HistoryButton
												:id="workflow?.id"
												:type="WFEMoudels.Workflow"
											/>
										</el-dropdown-item>
										<el-dropdown-item divided command="export">
											<el-icon>
												<Download />
											</el-icon>
											Export Workflow
										</el-dropdown-item>
									</el-dropdown-menu>
								</template>
							</el-dropdown>
						</div>
					</div>

					<!-- Workflow 内容 -->
					<div
						class="workflow-card-body bg-el-color-primary-light-9 dark:bg-el-fill-color"
					>
						<div class="workflow-header-actions">
							<div class="dates-container">
								<el-tooltip content="last mdify by">
									<div class="flex items-center gap-2">
										<Icon
											icon="ic:baseline-person-3"
											class="text-primary-500 w-5 h-5"
										/>
										<span
											class="card-value font-medium text-el-text-color-regular dark:text-el-text-color-secondary"
										>
											{{ workflow.modifyBy }}
										</span>
									</div>
								</el-tooltip>
								<el-tooltip content="last modify date">
									<div class="flex items-center gap-2">
										<Icon
											icon="ic:baseline-calendar-month"
											class="text-primary-500 w-5 h-5"
										/>
										<span
											class="card-value font-medium text-el-text-color-regular dark:text-el-text-color-secondary"
										>
											{{
												timeZoneConvert(
													workflow.modifyDate,
													false,
													projectTenMinuteDate
												)
											}}
										</span>
									</div>
								</el-tooltip>
							</div>
							<div class="action-buttons-group">
								<el-button
									@click="addStage()"
									:disabled="loading.createStage"
									:icon="loading.createStage ? Loading : Plus"
								>
									Add Stage
								</el-button>
							</div>
						</div>

						<!-- Stages 标题 -->
						<div class="stages-header">
							<div class="stages-header-actions"></div>
						</div>

						<StagesList
							v-model:stages="workflow.stages"
							:workflow-id="workflow.id"
							:is-editing="isEditing"
							:loading="{
								stages: loading.stages,
								deleteStage: loading.deleteStage,
								sortStages: loading.sortStages,
							}"
							:userList="userList"
							@edit="(stage) => editStage(stage)"
							@delete="(stageId) => deleteStage(stageId)"
							@drag-start="onDragStart"
							@order-changed="() => updateStagesOrder()"
						/>
					</div>

					<!-- 总阶段数信息 -->
					<div
						class="workflow-footer bg-el-fill-color-light dark:bg-el-fill-color border-t border-el-border-color-light dark:border-el-border-color px-6 py-4"
					>
						<p
							class="stage-count text-el-text-color-regular dark:text-el-text-color-secondary text-sm"
						>
							Total stages: {{ workflow?.stages?.length || 0 }}
						</p>
					</div>
				</div>
			</div>

			<!-- 空状态 - 没有工作流时显示 -->
			<div
				v-else
				class="empty-state-container rounded-xl bg-el-bg-color border border-dashed border-el-border-color dark:border-el-border-color"
			>
				<div class="empty-state-content">
					<el-icon class="empty-state-icon text-primary-300 dark:text-primary-400">
						<DocumentAdd />
					</el-icon>
					<h2
						class="empty-state-title text-el-text-color-primary dark:text-el-text-color-primary"
					>
						No Workflows Found
					</h2>
					<p
						class="empty-state-desc text-el-text-color-regular dark:text-el-text-color-secondary"
					>
						Workflows help you organize and manage the entire onboarding process. Create
						your first workflow to get started.
					</p>
					<el-button
						type="primary"
						size="large"
						@click="showNewWorkflowDialog"
						:loading="loading.createWorkflow"
						class="create-workflow-btn"
					>
						<el-icon><Plus /></el-icon>
						<span>Create Workflow</span>
					</el-button>
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
				:initial-data="isEditingWorkflow && workflow ? workflow : undefined"
				:is-editing="isEditingWorkflow"
				@submit="handleWorkflowSubmit"
				@cancel="handleWorkflowCancel"
				:loading="isEditingWorkflow ? loading.updateWorkflow : loading.createWorkflow"
			/>
		</el-dialog>

		<!-- 新建/编辑阶段对话框 -->
		<el-dialog
			v-model="dialogVisible.stageForm"
			:title="isEditingStage ? 'Edit Stage' : 'Add New Stage'"
			destroy-on-close
			custom-class="workflow-dialog"
			:show-close="true"
			:width="bigDialogWidth"
			:close-on-click-modal="false"
			top="5vh"
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
			<div class="p-1">
				<StageForm
					v-if="dialogVisible.stageForm"
					:stage="currentStage"
					:is-editing="isEditingStage"
					:loading="isEditingStage ? loading.updateStage : loading.createStage"
					:checklists="checklists"
					:questionnaires="questionnaires"
					:workflow-id="workflow?.id || ''"
					@submit="submitStage"
					@cancel="dialogVisible.stageForm = false"
				/>
			</div>
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
import { ref, reactive, onMounted, computed } from 'vue';
import { ElMessage, ElMessageBox, ElNotification } from 'element-plus';
import {
	Plus,
	MoreFilled,
	Edit,
	CircleClose,
	Check,
	CopyDocument,
	Download,
	// Connection,
	Loading,
	Star,
	VideoPause,
	DocumentAdd,
} from '@element-plus/icons-vue';

import StarIcon from '@assets/svg/workflow/star.svg';
import { timeZoneConvert } from '@/hooks/time';
import { dialogWidth, bigDialogWidth, projectTenMinuteDate } from '@/settings/projectSetting';
import { useI18n } from '@/hooks/useI18n';

// 引入OW模块API接口
import {
	createWorkflow as createWorkflowApi,
	getWorkflowList,
	updateWorkflow as updateWorkflowApi,
	deactivateWorkflow as deactivateWorkflowApi,
	activateWorkflow as activateWorkflowApi,
	duplicateWorkflow as duplicateWorkflowApi,
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
import { getFlowflexUser } from '@/apis/global';
import { FlowflexUser } from '#/golbal';
import { getAvatarColor } from '@/utils';
import { WFEMoudels } from '@/enums/appEnum';
import PageHeader from '@/components/global/PageHeader/index.vue';

const { t } = useI18n();

// 状态
const workflow = ref<Workflow | null>(null); // 当前操作的工作流
// const displayWorkflow = ref<Workflow | null>(null); // 用于显示的工作流副本
// const isEditingFromHistory = ref(false); // 是否从历史版本编辑
// const editingWorkflowData = ref<Workflow | null>(null); // 编辑中的工作流数据

const isEditing = ref(true);
const currentStage = ref<Stage | null>(null);
const isEditingStage = ref(false);
const selectedWorkflow = ref<string>('');
const workflowListData = ref<any[]>([]); // 工作流列表数据
const isEditingWorkflow = ref(false);
const originalStagesOrder = ref<Stage[]>([]); // 保存拖动前的原始阶段顺序

// API加载状态变量
const loading = reactive({
	workflows: false, // 获取工作流列表
	stages: false, // 获取阶段列表
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
	combineStages: false,
});

// 计算对话框标题
const dialogTitle = computed(() => {
	if (isEditingWorkflow.value && workflow.value) {
		return `Edit Workflow`;
	}
	return 'Create New Workflow';
});

// 计算对话框副标题
const dialogSubtitle = computed(() => {
	if (isEditingWorkflow.value) {
		const currentWorkflow = workflow.value;
		if (currentWorkflow) {
			return `Update the details for "${currentWorkflow.name}".`;
		}
	}
	return 'Create a new workflow version for the onboarding process.';
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
		selectedWorkflow.value = workflow.value.id;
	}
});

// 获取工作流列表
const fetchWorkflows = async (workflowId?: string) => {
	try {
		loading.workflows = true;
		const res = await getWorkflowList();
		if (res.code === '200' && res.data && res.data.length > 0) {
			// 获取默认工作流或第一个工作流
			const defaultWorkflow = res.data.find((wf) => wf.isDefault) || res.data[0];

			workflowListData.value = res.data;

			// 设置当前工作流并获取阶段
			if (workflowId) {
				await setCurrentWorkflow(workflowId);
			} else if (defaultWorkflow) {
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
	// 从工作流列表中查找
	let selectedWorkflowData = workflowListData.value.find((wf) => wf.id === workflowId);

	if (selectedWorkflowData) {
		workflow.value = selectedWorkflowData;
		selectedWorkflow.value = workflowId.toString();
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
		if (!userList.value || userList.value.length <= 0) {
			await getUserGroup();
		}
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

const showNewWorkflowDialog = () => {
	isEditingWorkflow.value = false;
	dialogVisible.workflowForm = true;
};

const handleCommand = (command: string) => {
	switch (command) {
		case 'edit':
			isEditingWorkflow.value = true;
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
		await updateWorkflow(workflowData);
	} else {
		await createWorkflow(workflowData);
	}
};

// 处理工作流取消
const handleWorkflowCancel = () => {
	dialogVisible.workflowForm = false;
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
			isActive: newWorkflow.status === 'active',
			version: 1,
		};
		// 调用创建工作流API
		const res = await createWorkflowApi(params);

		if (res.code === '200') {
			ElMessage.success(t('sys.api.operationSuccess'));
			// 重新获取工作流列表
			dialogVisible.workflowForm = false;
			await fetchWorkflows();
		} else {
			ElMessage.error(res.msg || t('sys.api.operationFailed'));
		}
	} finally {
		loading.createWorkflow = false;
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
			isActive: (updatedWorkflow.status || workflow.value.status) === 'active',
			version: workflow.value.version + 1,
		};

		// 调用更新工作流API
		const res = await updateWorkflowApi(workflow.value.id, params);

		if (res.code === '200') {
			ElMessage.success(t('sys.api.operationSuccess'));
			// 重新获取工作流列表
			dialogVisible.workflowForm = false;
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
			fetchWorkflows(workflow.value!.id);
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
							fetchWorkflows(workflow.value!.id);
							done(); // 关闭对话框
						} else {
							ElMessage.error(res.msg || t('sys.api.operationFailed'));
							// 恢复按钮状态
							instance.confirmButtonLoading = false;
							instance.confirmButtonText = 'Set as Inactive';
						}
					} catch (error) {
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
			stages: null,
		};

		const res = await updateWorkflowApi(workflow.value.id, params);

		if (res.code === '200') {
			ElMessage.success(t('sys.api.operationSuccess'));
			// 重新获取工作流列表以更新所有工作流的默认状态
			await fetchWorkflows();
		} else {
			ElMessage.error(res.msg || t('sys.api.operationFailed'));
		}
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
		// 更新阶段
		loading.updateStage = true;
		const params = {
			workflowId: workflow.value.id,
			...stage,
		};

		const res =
			isEditingStage.value && currentStage.value
				? await updateStage(currentStage.value.id, params)
				: await createStage(params);

		if (res.code === '200') {
			ElMessage.success(t('sys.api.operationSuccess'));
			// 重新获取阶段列表
			dialogVisible.stageForm = false;
			await fetchStages(workflow.value.id);
		} else {
			ElNotification({
				title: t('sys.api.operationFailed'),
				dangerouslyUseHTMLString: true,
				message: res?.msg || '',
				type: 'warning',
			});
		}
		loading.updateStage = false;
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

const userList = ref<FlowflexUser[]>([]);
const getUserGroup = async () => {
	try {
		const res = await getFlowflexUser({});
		if (res.code == '200') {
			userList.value = res.data || [];
		}
	} catch {
		userList.value = [];
	}
};
</script>

<style lang="scss" scoped>
.workflow-container {
	margin: 0 auto;
	height: 100%;
}

/* 使用 Tailwind 类替代 */

.workflow-card {
	@apply mb-4 shadow-sm transition-all duration-300 overflow-hidden flex flex-col;
}

.workflow-card.active {
	border: 1px solid var(--el-border-color-light);
}

.workflow-card-header {
	@apply flex justify-between items-center p-6;
}

.left-section {
	@apply flex items-start flex-col gap-1 max-w-[60%];
}

.title-and-tags {
	@apply flex items-center gap-2.5 flex-wrap;
}

.workflow-name {
	@apply font-semibold text-lg;
}

.workflow-desc {
	@apply text-xs mt-1;
}

.right-section {
	@apply flex items-center gap-4;
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
	border: 1px solid var(--el-border-color-light);
}

.actions-dropdown {
	min-width: 180px;
}

.actions-dropdown :deep(.el-dropdown-menu__item) {
	display: flex;
	align-items: center;
}

.ai-tag {
	background: var(--el-color-primary);
	background-color: var(--el-color-primary);
	color: white;
	border-color: transparent;
	padding: 2px 8px;
	font-size: 11px;
	display: inline-flex;
	align-items: center;
	margin-left: 8px;
}

/* Increase specificity to override Element Plus tag presets */
.ai-tag.el-tag,
.ai-tag.el-tag--primary,
.ai-tag.is-light,
.ai-tag.el-tag.el-tag--primary,
.el-tag.ai-tag,
.el-tag--primary.ai-tag,
.el-tag--primary.is-light.ai-tag {
	background: var(--el-color-primary) !important;
	background-color: var(--el-color-primary) !important;
	background-image: none !important;
	color: var(--el-color-white) !important;
	border-color: transparent !important;
	--el-tag-text-color: var(--el-color-white) !important;
}

.default-tag {
	background: var(--el-color-warning);
	color: var(--el-color-white);
	border-color: transparent;
	padding: 2px 8px;
	font-size: 11px;
	display: inline-flex;
	align-items: center;
	margin-left: 8px;
}

.ai-sparkles {
	font-size: 12px;
	animation: sparkle 2s ease-in-out infinite;
	display: inline-block;
}

.ai-dropdown-sparkles {
	font-size: 14px;
	animation: sparkle 2s ease-in-out infinite;
	display: inline-block;
}

@keyframes sparkle {
	0%,
	100% {
		transform: scale(1) rotate(0deg);
		opacity: 1;
	}
	25% {
		transform: scale(1.1) rotate(5deg);
		opacity: 0.9;
	}
	50% {
		transform: scale(1.2) rotate(-5deg);
		opacity: 0.8;
	}
	75% {
		transform: scale(1.1) rotate(3deg);
		opacity: 0.9;
	}
}

.star-icon {
	color: white;
	margin-right: 4px;
	width: 12px;
	height: 12px;
}

.ai-dropdown-icon {
	color: var(--el-color-primary);
	font-size: 14px;
}

.inactive-icon {
	color: var(--el-color-danger);
	font-size: 14px;
}

.calendar-icon {
	color: var(--primary-500);
	margin-right: 0;
	font-size: 16px;
}

.delete-item {
	color: var(--red-500, var(--el-color-danger));
}

.workflow-card-body {
	@apply py-4 px-2.5 pl-6 flex-1;
}

.workflow-header-actions {
	@apply flex justify-between items-center;
}

.dates-container {
	@apply flex items-center gap-4 text-sm;
}

.date-item {
	display: flex;
	align-items: center;
	gap: 4px;
}

.date-label {
	font-weight: 500;
	color: var(--el-text-color-primary);
	margin-right: 2px;
}

.date-value {
	color: var(--el-text-color-regular);
}

.stages-header {
	display: flex;
	justify-content: right;
	margin: 16px 0;
}

.stages-header h3 {
	margin: 0;
	font-size: 16px;
	color: var(--el-text-color-primary);
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
	font-weight: 600;
	background: var(--el-color-primary);
	color: var(--el-color-white);
	border: none;
}

.action-buttons-group {
	@apply flex items-center gap-3;
}

/* Hover and disabled states handled by Tailwind classes */

.drag-handle {
	cursor: move;
	color: var(--el-text-color-placeholder);
}

.ghost-workflow {
	opacity: 0.5;
	background: var(--el-color-info-light-7);
}

.version-history-placeholder {
	padding: 10px;
}

:deep(.el-icon) {
	vertical-align: middle;
}

.workflow-footer {
	@apply flex justify-start items-center m-0;
}

.stage-count {
	@apply font-normal m-0;
}

.text-primary {
	color: var(--el-color-primary);
}

.combine-stages-form {
	padding: 0 10px;
}

.text-muted {
	color: var(--el-text-color-secondary);
}

.stage-item-select {
	padding: 8px;
	margin-bottom: 4px;
	transition: background-color 0.2s;
	@apply rounded-xl;
}

.stage-item-select:hover {
	background-color: var(--el-fill-color-lighter);
}

.stage-color-indicator {
	width: 12px;
	height: 12px;
	border-radius: 50%;
	margin-right: 8px;
	display: inline-block;
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
	margin: 0 0 4px 0;
}

.dialog-subtitle {
	color: var(--el-text-color-regular);
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
	overflow: hidden;
	@apply rounded-xl;
}

:deep(.version-history-dialog .el-dialog__header) {
	padding: 0;
	margin-right: 0;
	border-bottom: 1px solid var(--el-border-color-lighter);
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
	border-top: 1px solid var(--el-border-color-lighter);
	background-color: var(--el-fill-color-lighter);
}

.version-dialog-header {
	padding: 24px 24px 20px 24px;
	background-color: var(--el-bg-color);
}

.version-dialog-title {
	font-size: 20px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	margin: 0 0 8px 0;
	line-height: 1.2;
}

.version-dialog-subtitle {
	color: var(--el-text-color-secondary);
	font-size: 14px;
	margin: 0;
	font-weight: normal;
	line-height: 1.4;
}

.version-history-content {
	padding: 0;
	background-color: var(--el-bg-color);
}

.version-history-table {
	width: 100%;
	table-layout: fixed;
}

.version-history-table .el-table__body-wrapper {
	overflow-x: hidden;
}

:deep(.version-table-header) {
	background-color: var(--el-fill-color-lighter);
	border-bottom: 1px solid var(--el-border-color-light);
}

:deep(.version-table-header th) {
	background-color: var(--el-fill-color-lighter) !important;
	color: var(--el-text-color-primary);
	font-weight: 600;
	font-size: 13px;
	padding: 16px 12px;
	border-bottom: 1px solid var(--el-border-color-light);
}

:deep(.version-table-row td) {
	padding: 14px 12px;
	border-bottom: 1px solid var(--el-fill-color-light);
	vertical-align: middle;
}

:deep(.version-table-header th) {
	padding: 14px 12px;
}

:deep(.version-table-row:hover td) {
	background-color: var(--el-fill-color-lighter);
}

.version-name {
	font-weight: 500;
	color: var(--el-text-color-primary);
	font-size: 14px;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.status-tag {
	font-weight: 500;
	font-size: 12px;
}

.default-tag {
	background: var(--el-color-warning);
	color: var(--el-color-white);
	border: none;
	font-weight: 500;
	font-size: 12px;
}

.date-text {
	color: var(--el-text-color-secondary);
	font-size: 13px;
	white-space: nowrap;
}

.created-by {
	color: var(--el-text-color-primary);
	font-size: 13px;
	font-weight: 500;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
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
	transition: all 0.2s ease;
	@apply rounded-xl;
}

.edit-btn:hover {
	background-color: var(--el-fill-color-light);
}

.version-dialog-footer {
	display: flex;
	justify-content: flex-end;
	margin: 0;
}

.close-btn {
	color: var(--el-text-color-secondary);
	border-color: var(--el-border-color);
	background-color: var(--el-bg-color);
	font-weight: 500;
	padding: 8px 16px;
}

.close-btn:hover {
	background-color: var(--el-fill-color-lighter);
	border-color: var(--el-text-color-secondary);
}

.new-workflow-btn-footer {
	background: var(--el-color-primary);
	border: none;
	color: var(--el-color-white);
	font-weight: 500;
	padding: 8px 16px;
	display: flex;
	align-items: center;
	gap: 6px;
	transition: all 0.2s ease;
}

.new-workflow-btn-footer:hover {
	background: var(--el-color-primary-dark-2);
	transform: translateY(-1px);
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
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

/* 空状态样式 */
.empty-state-container {
	@apply py-16 px-5 text-center mb-4;
}

.empty-state-content {
	@apply max-w-lg mx-auto flex flex-col items-center;
}

.empty-state-icon {
	@apply text-6xl mb-6;
}

.empty-state-title {
	@apply text-2xl font-semibold m-0 mb-4;
}

.empty-state-desc {
	@apply text-base m-0 mb-8 leading-relaxed;
}

.create-workflow-btn {
	@apply py-3 px-6 text-base;
}
</style>

<!-- 全局样式用于 MessageBox 删除确认对话框 -->
<style lang="scss">
/* 删除确认对话框样式 */
.delete-confirmation-dialog {
	/* border-radius removed - using rounded-xl class */

	.el-message-box__message {
		color: var(--el-text-color-regular);
		font-size: 14px;
		line-height: 1.5;
	}
}

/* 停用确认对话框样式 */
.deactivate-confirmation-dialog {
	.el-message-box__message {
		color: var(--el-text-color-regular);
		font-size: 14px;
		line-height: 1.5;
	}
}

/* 过期日期确认对话框样式 */
.expired-date-confirmation-dialog {
	.el-message-box__message {
		color: var(--el-text-color-regular);
		font-size: 14px;
		line-height: 1.5;
		white-space: pre-line;
		/* 保持换行格式 */
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
		color: var(--el-color-white) !important;
		opacity: 1 !important;
		box-shadow: none !important;

		&:hover,
		&:focus {
			background: var(--el-color-danger-light-3) !important;
			background-color: var(--el-color-danger-light-3) !important;
			background-image: none !important;
			border-color: var(--el-color-danger-light-3) !important;
			color: var(--el-color-white) !important;
			opacity: 1 !important;
			box-shadow: 0 0 0 2px var(--el-color-danger-light-8) !important;
		}

		&:active {
			background: var(--el-color-danger-dark-2) !important;
			background-color: var(--el-color-danger-dark-2) !important;
			background-image: none !important;
			border-color: var(--el-color-danger-dark-2) !important;
			color: var(--el-color-white) !important;
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
		color: var(--el-color-white) !important;

		&:not(:disabled):not(.is-disabled) {
			background: var(--el-color-danger) !important;
			background-color: var(--el-color-danger) !important;
			background-image: none !important;
			border-color: var(--el-color-danger) !important;
			color: var(--el-color-white) !important;
		}
	}
}

/* 取消按钮样式 - 仅限于删除确认对话框 */
.delete-confirmation-dialog {
	.cancel-confirm-btn {
		background-color: var(--el-color-white) !important;
		border-color: var(--el-border-color) !important;
		color: var(--el-text-color-regular) !important;

		&:hover {
			background-color: var(--el-fill-color-lighter) !important;
			border-color: var(--el-border-color-dark) !important;
			color: var(--el-text-color-regular) !important;
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
		color: var(--el-color-white) !important;
		opacity: 1 !important;
		box-shadow: none !important;

		&:hover,
		&:focus {
			background: var(--el-color-warning-light-3) !important;
			background-color: var(--el-color-warning-light-3) !important;
			background-image: none !important;
			border-color: var(--el-color-warning-light-3) !important;
			color: var(--el-color-white) !important;
			opacity: 1 !important;
			box-shadow: 0 0 0 2px var(--el-color-warning-light-8) !important;
		}

		&:active {
			background: var(--el-color-warning-dark-2) !important;
			background-color: var(--el-color-warning-dark-2) !important;
			background-image: none !important;
			border-color: var(--el-color-warning-dark-2) !important;
			color: var(--el-color-white) !important;
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
		color: var(--el-color-white) !important;

		&:not(:disabled):not(.is-disabled) {
			background: var(--el-color-warning) !important;
			background-color: var(--el-color-warning) !important;
			background-image: none !important;
			border-color: var(--el-color-warning) !important;
			color: var(--el-color-white) !important;
		}
	}

	/* 取消按钮样式 */
	.cancel-confirm-btn {
		background-color: var(--el-color-white) !important;
		border-color: var(--el-border-color) !important;
		color: var(--el-text-color-regular) !important;

		&:hover {
			background-color: var(--el-fill-color-lighter) !important;
			border-color: var(--el-border-color-dark) !important;
			color: var(--el-text-color-regular) !important;
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
		color: var(--el-color-white) !important;
		opacity: 1 !important;
		box-shadow: none !important;

		&:hover,
		&:focus {
			background: var(--el-color-warning-light-3) !important;
			background-color: var(--el-color-warning-light-3) !important;
			background-image: none !important;
			border-color: var(--el-color-warning-light-3) !important;
			color: var(--el-color-white) !important;
			opacity: 1 !important;
			box-shadow: 0 0 0 2px var(--el-color-warning-light-8) !important;
		}

		&:active {
			background: var(--el-color-warning-dark-2) !important;
			background-color: var(--el-color-warning-dark-2) !important;
			background-image: none !important;
			border-color: var(--el-color-warning-dark-2) !important;
			color: var(--el-color-white) !important;
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
		color: var(--el-color-white) !important;

		&:not(:disabled):not(.is-disabled) {
			background: var(--el-color-warning) !important;
			background-color: var(--el-color-warning) !important;
			background-image: none !important;
			border-color: var(--el-color-warning) !important;
			color: var(--el-color-white) !important;
		}
	}

	/* 取消按钮样式 */
	.cancel-confirm-btn {
		background-color: var(--el-color-white) !important;
		border-color: var(--el-border-color) !important;
		color: var(--el-text-color-regular) !important;

		&:hover {
			background-color: var(--el-fill-color-lighter) !important;
			border-color: var(--el-border-color-dark) !important;
			color: var(--el-text-color-regular) !important;
		}
	}
}
</style>
