<template>
	<div class="workflow-container">
		<!-- 标题和操作区 -->
		<PageHeader
			title="Workflows"
			description="Design and manage business workflows with customizable stages and automated processes"
		>
			<template #actions>
				<TabButtonGroup
					v-model="activeView"
					:tabs="tabsConfig"
					size="small"
					type="adaptive"
					class="mr-4"
					@tab-change="handleViewChange"
				/>
				<el-button
					v-permission="ProjectPermissionEnum.workflow.create"
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
			<!-- 搜索和筛选区域 -->
			<el-card class="mb-6" v-show="viewMode === 'list'">
				<div class="grid grid-cols-1 md:grid-cols-3 gap-4">
					<div class="space-y-2">
						<label class="text-sm font-medium">Search</label>
						<InputTag
							v-model="searchWorkflowName"
							placeholder="Enter workflow name and press enter"
							style-type="normal"
							clearable
							:limit="10"
							@change="handleWorkflowChange"
							class="w-full"
						/>
					</div>

					<div class="space-y-2">
						<label class="text-sm font-medium">Status</label>
						<el-select
							v-model="searceWorkflowStatus"
							placeholder="Select workflow status"
							class="w-full"
							clearable
							@change="handleWorkflowChange"
						>
							<el-option label="Active" value="active" />
							<el-option label="Inactive" value="inactive" />
						</el-select>
					</div>
				</div>
			</el-card>
			<!-- 列表视图模式 -->
			<div v-if="viewMode === 'list'">
				<!-- 视图切换标签页 -->
				<PrototypeTabs
					v-model="activeView"
					:tabs="tabsConfig"
					type="adaptive"
					size="default"
					:hidden-tab="true"
					@tab-change="handleViewChange"
				>
					<!-- 卡片视图 -->
					<TabPane value="card">
						<el-scrollbar ref="scrollbarRef">
							<WorkflowCardView
								:workflows="workflowListData"
								:loading="loading.workflows"
								empty-message="No workflows have been created yet"
								:action-loading="actionLoading"
								@command="handleCommand"
								@select-workflow="handleWorkflowSelect"
								@new-workflow="showNewWorkflowDialog"
							/>
						</el-scrollbar>
					</TabPane>

					<!-- 列表视图 -->
					<TabPane value="list">
						<WorkflowListView
							:workflows="workflowListData"
							:loading="loading.workflows"
							:action-loading="actionLoading"
							@command="handleCommand"
							@selection-change="handleSelectionChange"
							@sort-change="handleSortChange"
							@select-workflow="handleWorkflowSelect"
						/>
					</TabPane>
				</PrototypeTabs>

				<!-- 统一分页组件 -->
				<div v-if="!loading.workflows && pagination.total > 0">
					<CustomerPagination
						:total="pagination.total"
						:limit="pagination.pageSize"
						:page="pagination.pageIndex"
						:background="true"
						@pagination="handleLimitUpdate"
						@update:page="handleCurrentChange"
						@update:limit="handlePageUpdate"
					/>
				</div>
			</div>

			<!-- 详情视图模式 -->
			<div v-else-if="viewMode === 'detail'">
				<!-- 返回按钮 -->
				<div class="mb-4">
					<el-button @click="handleBackToList" :icon="ArrowLeft">Back to List</el-button>
				</div>

				<!-- 加载中状态 -->
				<div v-if="loading.workflows" class="rounded-xl bg-el-bg-color dark:bg-el-bg-color">
					<el-skeleton style="width: 100%" :rows="10" animated />
				</div>

				<!-- 工作流详情内容 -->
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
										class="ai-tag"
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
										class="default-tag"
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
									>
										Active
									</el-tag>
									<el-tag v-else type="danger" size="small">Inactive</el-tag>
								</div>
								<span
									class="workflow-desc text-el-text-color-regular dark:text-el-text-color-secondary"
								>
									{{ workflow.description }}
								</span>
							</div>
							<div class="right-section">
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
											<el-dropdown-item
												command="edit"
												v-if="
													hasWorkflowPermission(
														ProjectPermissionEnum.workflow.update
													)
												"
											>
												<el-icon>
													<Edit />
												</el-icon>
												Edit Workflow
											</el-dropdown-item>
											<el-dropdown-item
												v-if="
													hasWorkflowPermission(
														ProjectPermissionEnum.workflow.update
													) &&
													!workflow.isDefault &&
													workflow.status === 'active'
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
												v-if="
													hasWorkflowPermission(
														ProjectPermissionEnum.workflow.update
													) && workflow.status === 'active'
												"
												command="deactivate"
											>
												<el-icon>
													<CircleClose />
												</el-icon>
												Set as Inactive
											</el-dropdown-item>
											<el-dropdown-item
												v-if="
													hasWorkflowPermission(
														ProjectPermissionEnum.workflow.update
													) && workflow.status === 'inactive'
												"
												command="activate"
											>
												<el-icon>
													<Check />
												</el-icon>
												Set as Active
											</el-dropdown-item>
											<el-dropdown-item
												divided
												command="duplicate"
												v-if="
													hasWorkflowPermission(
														ProjectPermissionEnum.workflow.create
													)
												"
											>
												<el-icon>
													<CopyDocument />
												</el-icon>
												Duplicate
											</el-dropdown-item>
											<el-dropdown-item
												command="addStage"
												v-if="
													hasWorkflowPermission(
														ProjectPermissionEnum.workflow.create
													)
												"
											>
												<el-icon>
													<Plus />
												</el-icon>
												Add Stage
											</el-dropdown-item>
											<el-dropdown-item
												command="manageConditions"
												v-if="
													hasWorkflowPermission(
														ProjectPermissionEnum.workflow.update
													)
												"
											>
												<el-icon>
													<Connection />
												</el-icon>
												Manage Conditions
											</el-dropdown-item>

											<el-dropdown-item
												divided
												v-if="
													functionPermission(
														ProjectPermissionEnum.workflow.read
													)
												"
											>
												<HistoryButton
													:id="workflow?.id"
													:type="WFEMoudels.Workflow"
												/>
											</el-dropdown-item>
											<el-dropdown-item
												divided
												command="export"
												v-if="
													functionPermission(
														ProjectPermissionEnum.workflow.read
													)
												"
											>
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
										v-if="
											hasWorkflowPermission(
												ProjectPermissionEnum.workflow.create
											)
										"
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
								:has-workflow-permission="
									workflow.permission ? workflow.permission.canOperate : true
								"
								:static-fields="staticFields"
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
							Workflows help you organize and manage the entire onboarding process.
							Create your first workflow to get started.
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
		</div>

		<!-- 工作流表单对话框 -->
		<el-dialog
			v-model="dialogVisible.workflowForm"
			:title="dialogTitle"
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
					:work-flow-operate-teams="workflow?.operateTeams"
					:work-flow-view-teams="workflow?.viewTeams"
					:work-flow-view-permission-mode="workflow?.viewPermissionMode"
					:work-flow-view-use-same-team-for-operate="workflow?.useSameTeamForOperate"
					:is-editing="isEditingStage"
					:loading="isEditingStage ? loading.updateStage : loading.createStage"
					:checklists="checklists"
					:questionnaires="questionnaires"
					:workflow-id="workflow?.id || ''"
					:quickLinks="quickLinks"
					:static-fields="staticFields"
					@submit="submitStage"
					@cancel="dialogVisible.stageForm = false"
				/>
			</div>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed, markRaw } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElMessageBox, ElNotification } from 'element-plus';
import {
	Plus,
	MoreFilled,
	Edit,
	CircleClose,
	Check,
	CopyDocument,
	Download,
	Connection,
	Loading,
	Star,
	DocumentAdd,
	ArrowLeft,
} from '@element-plus/icons-vue';

import StarIcon from '@assets/svg/workflow/star.svg';
import { timeZoneConvert } from '@/hooks/time';
import { bigDialogWidth, projectTenMinuteDate } from '@/settings/projectSetting';
import { useI18n } from '@/hooks/useI18n';

// 引入OW模块API接口
import {
	createWorkflow as createWorkflowApi,
	getWorkflowList,
	updateWorkflow as updateWorkflowApi,
	activateWorkflow as activateWorkflowApi,
	duplicateWorkflow as duplicateWorkflowApi,
	createStage,
	getStagesByWorkflow,
	sortStages,
	updateStage,
	deleteStage as deleteStageApi,
	exportWorkflowToExcel,
} from '@/apis/ow';

import { getChecklists } from '@/apis/ow/checklist';
import { queryQuestionnaires } from '@/apis/ow/questionnaire';
import { getQuickLinks } from '@/apis/integration';
import { getDynamicField } from '@/apis/global/dyanmicField';
import { DynamicList } from '#/dynamic';

// 引入自定义组件
import StagesList from './components/StagesList.vue';
import NewWorkflowForm from './components/NewWorkflowForm.vue';
import StageForm from './components/StageForm.vue';
import WorkflowCardView from './components/WorkflowCardView.vue';
import WorkflowListView from './components/WorkflowListView.vue';
import { Stage, Workflow, Questionnaire, Checklist } from '#/onboard';
import { getFlowflexUser } from '@/apis/global';
import { FlowflexUser } from '#/golbal';
import { WFEMoudels } from '@/enums/appEnum';
import PageHeader from '@/components/global/PageHeader/index.vue';
import { PrototypeTabs, TabPane, TabButtonGroup } from '@/components/PrototypeTabs';
import CustomerPagination from '@/components/global/u-pagination/index.vue';
import InputTag from '@/components/global/u-input-tags/index.vue';
import { useAdaptiveScrollbar } from '@/hooks/useAdaptiveScrollbar';
import TableViewIcon from '@assets/svg/onboard/tavleView.svg';
import ProgressViewIcon from '@assets/svg/onboard/progressView.svg';
import { ProjectPermissionEnum, ViewPermissionModeEnum } from '@/enums/permissionEnum';
import { functionPermission } from '@/hooks';
import { useUserStore } from '@/stores/modules/user';
import { menuRoles } from '@/stores/modules/menuFunction';
import { IQuickLink } from '#/integration';

const { t } = useI18n();

// Router instance
const route = useRoute();
const router = useRouter();

// Store instances
const userStore = useUserStore();
const menuStore = menuRoles();

// 使用自适应滚动条 hook
const { scrollbarRef } = useAdaptiveScrollbar(70);

// 状态
const workflow = ref<Workflow | null>(null); // 当前操作的工作流
const isEditing = ref(true);
const currentStage = ref<Stage | null>(null);
const isEditingStage = ref(false);
const selectedWorkflow = ref<string>('');
const workflowListData = ref<any[]>([]); // 工作流列表数据（分页数据，用于列表视图）
const isEditingWorkflow = ref(false);
const originalStagesOrder = ref<Stage[]>([]); // 保存拖动前的原始阶段顺序

// 新增状态变量
const viewMode = ref<'list' | 'detail'>('list'); // 视图模式
const activeView = ref('list'); // table/card切换
const pagination = ref({
	pageIndex: 1,
	pageSize: 15,
	total: 0,
});

// 路由 query 中的 workflowId
const routeWorkflowId = computed(() => route.query.id as string | undefined);

// 视图切换配置
const tabsConfig = ref([
	{ label: 'Table View', value: 'list', icon: markRaw(TableViewIcon) },
	{ label: 'Card View', value: 'card', icon: markRaw(ProgressViewIcon) },
]);

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
	exportWorkflow: false, // 导出工作流
});

// 当前正在执行操作的workflow ID和操作类型
const currentActionWorkflow = ref<string | null>(null);
const currentActionType = ref<string | null>(null);

// 计算actionLoading状态，用于子组件
const actionLoading = computed(() => {
	if (!currentActionWorkflow.value || !currentActionType.value) {
		return {};
	}

	const workflowId = currentActionWorkflow.value;
	const actionType = currentActionType.value;

	// 检查对应的loading状态
	let isLoading = false;
	switch (actionType) {
		case 'edit':
		case 'setDefault':
			isLoading = loading.updateWorkflow;
			break;
		case 'activate':
			isLoading = loading.activateWorkflow;
			break;
		case 'deactivate':
			isLoading = loading.deactivateWorkflow;
			break;
		case 'duplicate':
			isLoading = loading.duplicateWorkflow;
			break;
		case 'export':
			isLoading = loading.exportWorkflow;
			break;
		default:
			isLoading = false;
	}

	return {
		[workflowId]: {
			[actionType]: isLoading,
		},
	};
});

// 对话框状态
const dialogVisible = reactive({
	workflowForm: false,
	stageForm: false,
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

// 注意：onWorkflowChange函数已移除，因为详情视图不再需要选择器

const checklists = ref<Checklist[]>([]);
const questionnaires = ref<Questionnaire[]>([]);
const quickLinks = ref<IQuickLink[]>([]);
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

const fetchQuickLinks = async () => {
	try {
		const res = await getQuickLinks();
		if (res.code === '200') {
			quickLinks.value = res.data;
		}
	} catch (error) {
		quickLinks.value = [];
	}
};

// 加载 workflow 详情
const loadWorkflowDetail = async (workflowId: string) => {
	// 先从列表数据中查找
	let selectedWorkflowData = workflowListData.value.find((wf) => wf.id === workflowId);

	if (selectedWorkflowData) {
		// 设置基本信息
		workflow.value = selectedWorkflowData;
		selectedWorkflow.value = workflowId;
		viewMode.value = 'detail';

		// 获取完整的workflow详情（包括stages）
		await fetchStages(workflowId);
	} else {
		ElMessage.error('Workflow not found');
		// 清除无效的 query 参数
		router.replace({ query: {} });
	}
};

// 获取工作流列表（分页数据，用于列表视图）
// 筛选
const searchWorkflowName = ref<string[]>([]);
const searceWorkflowStatus = ref<string>('');
const handleWorkflowChange = () => {
	fetchWorkflows(true);
};
const fetchWorkflows = async (resetPage = false) => {
	try {
		loading.workflows = true;

		// 如果需要重置页码
		if (resetPage) {
			pagination.value.pageIndex = 1;
		}

		const searchParams = {} as any;
		if (searchWorkflowName.value) {
			searchParams.name = searchWorkflowName.value;
		}
		if (searceWorkflowStatus.value && searceWorkflowStatus.value !== '') {
			searchParams.status = searceWorkflowStatus.value;
		}

		// 构建查询参数
		const params = {
			pageIndex: pagination.value.pageIndex,
			pageSize: pagination.value.pageSize,
			...searchParams,
		};

		const res = await getWorkflowList(params);
		if (res.code === '200') {
			workflowListData.value = res?.data?.items || [];

			pagination.value.total = res?.data?.totalCount || 0;
		} else {
			workflowListData.value = [];
			pagination.value.total = 0;
		}
	} finally {
		loading.workflows = false;
	}
};

// 注意：fetchAllWorkflows函数已移除，因为详情视图不再需要选择器

// 注意：setCurrentWorkflow函数已被handleWorkflowSelect替代

// 获取工作流关联的阶段
const fetchStages = async (workflowId: string | number) => {
	try {
		loading.stages = true;
		if (!userList.value || userList.value.length <= 0) {
			await getUserGroup();
		}
		const res = await getStagesByWorkflow(workflowId);
		if (res.code === '200') {
			// 只有当 workflow 还存在时才更新（用户可能已经返回列表）
			if (workflow.value) {
				workflow.value.stages = res.data || [];
			}
			// workflow 为 null 时静默忽略（用户已返回列表）
		} else {
			// 只在 API 真正失败时显示错误
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

// 新增事件处理函数
const handleViewChange = (value: string) => {
	activeView.value = value;
};

const handleWorkflowSelect = async (workflowId: string) => {
	// 从列表数据中查找选中的workflow

	const selectedWorkflowData = workflowListData.value.find((wf) => wf.id === workflowId);

	if (selectedWorkflowData) {
		// 设置基本信息

		workflow.value = selectedWorkflowData;

		selectedWorkflow.value = workflowId.toString();

		// 切换到详情视图

		viewMode.value = 'detail';

		// 更新路由 query 参数（不会触发组件重新挂载）

		router.replace({ query: { id: workflowId } });

		// 获取完整的workflow详情（包括stages）

		await fetchStages(workflowId);
	} else {
		workflow.value = null;
	}
};

const handleBackToList = () => {
	viewMode.value = 'list';
	workflow.value = null;
	// 清除路由 query 参数
	router.replace({ query: {} });
};

// 分页处理函数
const handleCurrentChange = (page: number) => {
	pagination.value.pageIndex = page;
};

const handlePageUpdate = (pageSize: number) => {
	pagination.value.pageSize = pageSize;
};

const handleLimitUpdate = () => {
	fetchWorkflows();
};

// 表格相关方法 (列表视图)
const handleSelectionChange = (selection: any[]) => {
	// 在列表视图中，selectionChange 通常用于多选，这里不需要特殊处理
};

const handleSortChange = (sort: any) => {
	// 在列表视图中，sortChange 用于排序，这里不需要特殊处理
};

const handleCommand = (command: string, targetWorkflow?: any) => {
	// 设置当前操作的workflow和操作类型（用于loading状态
	if (targetWorkflow) {
		currentActionWorkflow.value = targetWorkflow.id;
		currentActionType.value = command;
	}

	switch (command) {
		case 'edit':
			if (targetWorkflow) {
				// 设置编辑模式和当前workflow
				workflow.value = targetWorkflow;
				isEditingWorkflow.value = true;
				dialogVisible.workflowForm = true;
				// 编辑操作不需要loading状态，立即清除
				currentActionWorkflow.value = null;
				currentActionType.value = null;
			}
			break;
		case 'setDefault':
			if (targetWorkflow) {
				setAsDefault(targetWorkflow);
			}
			break;
		case 'activate':
			if (targetWorkflow) {
				activateWorkflow(targetWorkflow);
			}
			break;
		case 'deactivate':
			if (targetWorkflow) {
				deactivateWorkflow(targetWorkflow);
			}
			break;
		case 'addStage':
			addStage();
			// 清除loading状态
			currentActionWorkflow.value = null;
			currentActionType.value = null;
			break;
		case 'duplicate':
			if (targetWorkflow) {
				duplicateWorkflow(targetWorkflow);
			}
			break;
		case 'export':
			if (targetWorkflow) {
				exportWorkflow(targetWorkflow);
			}
			break;
		case 'manageConditions':
			// 跳转到可视化条件编辑器页面
			if (targetWorkflow) {
				router.push(`/onboard/workflow/${targetWorkflow.id}/conditions`);
			}
			// 清除loading状态
			currentActionWorkflow.value = null;
			currentActionType.value = null;
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

// 验证并检查权限：验证必填项 + 检查当前用户是否会被权限设置排除
const validateAndCheckPermissions = async (
	viewPermissionMode: number,
	viewTeams: string[],
	operateTeams: string[],
	useSameTeamForOperate: boolean,
	entityType: 'workflow' | 'stage' = 'workflow'
): Promise<{
	hasWarning: boolean;
	showMessage: boolean;
	warningMessage: string;
}> => {
	// Validate: 检查是否至少选择了一个团队
	const entityName = entityType === 'workflow' ? 'workflow' : 'stage';

	if (viewPermissionMode === ViewPermissionModeEnum.Public) {
		if (useSameTeamForOperate == false && operateTeams.length === 0) {
			return {
				hasWarning: false,
				showMessage: true,
				warningMessage: `Please select at least one team for Operate Permission of this ${entityName}.`,
			};
		}

		if (viewTeams.length > 0) {
			viewTeams.splice(0, viewTeams.length);
		}
		if (useSameTeamForOperate && operateTeams.length > 0) {
			operateTeams.splice(0, operateTeams.length);
		}

		return { hasWarning: false, showMessage: false, warningMessage: '' };
	}

	if (viewTeams.length === 0) {
		return {
			hasWarning: false,
			showMessage: true,
			warningMessage: `Please select at least one team for View Permission of this ${entityName}.`,
		};
	}
	if (operateTeams.length === 0) {
		return {
			hasWarning: false,
			showMessage: true,
			warningMessage: `Please select at least one team for Operate Permission of this ${entityName}.`,
		};
	}

	const currentUser = userStore.getUserInfo;
	if (!currentUser || !currentUser.userId) {
		return { hasWarning: false, showMessage: false, warningMessage: '' };
	}

	// 递归查找用户所属团队的辅助函数
	const findUserTeams = (data: FlowflexUser[], userId: string): string[] => {
		const teams: string[] = [];
		for (const item of data) {
			if (item.type === 'team' && item.children) {
				// 检查团队下是否有当前用户
				const hasCurrentUser = item.children.some(
					(child) => child.type === 'user' && child.id === userId
				);
				if (hasCurrentUser) {
					teams.push(item.id);
				}
				// 递归查找子团队
				teams.push(...findUserTeams(item.children, userId));
			}
		}
		return teams;
	};

	try {
		// 获取用户数据
		const userData = await menuStore.getFlowflexUserDataWithCache();
		const currentUserId = String(currentUser.userId);
		//是否包含不可用team, 如果包含，则返回警告
		const collectTeamIds = (nodes: FlowflexUser[]): Set<string> => {
			const teamIds = new Set<string>();
			const traverse = (items: FlowflexUser[]) => {
				items.forEach((item) => {
					if (item.type === 'team') {
						teamIds.add(item.id);
					}
					if (item.children && item.children.length > 0) {
						traverse(item.children);
					}
				});
			};
			traverse(nodes);
			return teamIds;
		};

		const normalizedUserData = Array.isArray(userData) ? userData : [];
		const teamIds = collectTeamIds(normalizedUserData);
		const missingTeams = [
			...viewTeams.filter((id) => !teamIds.has(id)),
			...operateTeams.filter((id) => !teamIds.has(id)),
		];

		if (missingTeams.length > 0) {
			return {
				hasWarning: false,
				showMessage: true,
				warningMessage:
					'Some selected teams no longer exist. Please update your selection.',
			};
		}

		const userTeams = findUserTeams(normalizedUserData, currentUserId);

		let isUserExcludedFromView = false;
		let isUserExcludedFromOperate = false;

		// 检查 View Permission（基于团队）
		const isInViewList = userTeams.some((teamId) => viewTeams.includes(teamId));
		// 白名单：不在列表中 = 被排除；黑名单：在列表中 = 被排除
		isUserExcludedFromView =
			viewPermissionMode === ViewPermissionModeEnum.VisibleTo ? !isInViewList : isInViewList;

		// 检查 Operate Permission（基于团队）
		const isInOperateList = userTeams.some((teamId) => operateTeams.includes(teamId));
		// 白名单：不在列表中 = 被排除；黑名单：在列表中 = 被排除
		isUserExcludedFromOperate =
			viewPermissionMode === ViewPermissionModeEnum.VisibleTo
				? !isInOperateList
				: isInOperateList;

		// 生成警告信息
		if (isUserExcludedFromView || isUserExcludedFromOperate) {
			let warningMessage = '';
			const entityName = entityType === 'workflow' ? 'workflow' : 'stage';
			if (isUserExcludedFromView && isUserExcludedFromOperate) {
				warningMessage = `Warning: You are setting permissions that will exclude yourself from viewing and operating this ${entityName}. You will not be able to access this ${entityName} after saving. Do you want to continue?`;
			} else if (isUserExcludedFromView) {
				warningMessage = `Warning: You are setting permissions that will exclude yourself from viewing this ${entityName}. You will not be able to access this ${entityName} after saving. Do you want to continue?`;
			} else {
				warningMessage = `Warning: You are setting permissions that will exclude yourself from operating this ${entityName}. You will be able to view but not operate on this ${entityName} after saving. Do you want to continue?`;
			}

			if (currentUser.userType === 1 || currentUser.userType === 2) {
				return { hasWarning: false, showMessage: false, warningMessage };
			}

			return { hasWarning: true, showMessage: false, warningMessage };
		}
	} catch (error) {
		console.error('Failed to check user permissions:', error);
	}

	return { hasWarning: false, showMessage: false, warningMessage: '' };
};

const createWorkflow = async (newWorkflow: Partial<Workflow>) => {
	try {
		// 验证并检查权限：验证必填项 + 检查当前用户是否会被权限设置排除
		const permissionCheck = await validateAndCheckPermissions(
			newWorkflow.viewPermissionMode ?? ViewPermissionModeEnum.Public,
			newWorkflow.viewTeams ?? [],
			newWorkflow.operateTeams ?? [],
			newWorkflow.useSameTeamForOperate ?? true,
			'workflow'
		);
		if (permissionCheck.hasWarning || permissionCheck.showMessage) {
			try {
				if (permissionCheck.showMessage) {
					ElMessage.warning(permissionCheck.warningMessage);
					return;
				} else {
					await ElMessageBox.confirm(
						permissionCheck.warningMessage,
						'⚠️ Permission Warning',
						{
							confirmButtonText: 'Continue',
							cancelButtonText: 'Cancel',
							type: 'warning',
							distinguishCancelAndClose: true,
						}
					);
				}
			} catch (error) {
				// 用户点击取消
				return;
			}
		}
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
			// 权限字段
			viewPermissionMode: newWorkflow.viewPermissionMode ?? 0, // 默认为 Public
			viewTeams: newWorkflow.viewTeams ?? [],
			operateTeams: newWorkflow.operateTeams ?? [],
			useSameTeamForOperate: newWorkflow.useSameTeamForOperate ?? true,
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
		// 验证并检查权限：验证必填项 + 检查当前用户是否会被权限设置排除
		const permissionCheck = await validateAndCheckPermissions(
			updatedWorkflow.viewPermissionMode ?? workflow.value.viewPermissionMode,
			updatedWorkflow.viewTeams ?? workflow.value.viewTeams,
			updatedWorkflow.operateTeams ?? workflow.value.operateTeams,
			updatedWorkflow.useSameTeamForOperate ?? workflow.value.useSameTeamForOperate,
			'workflow'
		);
		if (permissionCheck.hasWarning || permissionCheck.showMessage) {
			try {
				if (permissionCheck.showMessage) {
					ElMessage.warning(permissionCheck.warningMessage);
					return;
				} else {
					await ElMessageBox.confirm(
						permissionCheck.warningMessage,
						'⚠️ Permission Warning',
						{
							confirmButtonText: 'Continue',
							cancelButtonText: 'Cancel',
							type: 'warning',
							distinguishCancelAndClose: true,
						}
					);
				}
			} catch (error) {
				// 用户点击取消
				return;
			}
		}
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
			// 使用 ?? 运算符，允许 0 作为有效值
			viewPermissionMode:
				updatedWorkflow.viewPermissionMode ?? workflow.value.viewPermissionMode,
			viewTeams: updatedWorkflow.viewTeams ?? workflow.value.viewTeams,
			operateTeams: updatedWorkflow.operateTeams ?? workflow.value.operateTeams,
			useSameTeamForOperate:
				updatedWorkflow.useSameTeamForOperate ?? workflow.value.useSameTeamForOperate,
		};

		// 调用更新工作流API
		const res = await updateWorkflowApi(workflow.value.id, params);

		if (res.code === '200') {
			ElMessage.success(t('sys.api.operationSuccess'));
			// 重新获取工作流列表
			dialogVisible.workflowForm = false;
			await fetchWorkflows();

			// 如果当前在详情视图，更新当前显示的 workflow 数据
			if (viewMode.value === 'detail' && workflow.value) {
				const updatedWorkflowData = workflowListData.value.find(
					(wf) => wf.id === workflow.value!.id
				);
				if (updatedWorkflowData) {
					// 保留 stages 数据，只更新基本信息
					workflow.value = {
						...updatedWorkflowData,
						stages: workflow.value.stages,
					};
				}
			}
		} else {
			ElMessage.error(res.msg || t('sys.api.operationFailed'));
		}
	} finally {
		loading.updateWorkflow = false;
	}
};

const activateWorkflow = async (targetWorkflow?: any) => {
	const workflowToActivate = targetWorkflow || workflow.value;
	if (!workflowToActivate) return;

	// 如果没有end date或者end date未过期，直接激活
	try {
		loading.activateWorkflow = true;
		// 调用激活工作流API
		const res = await activateWorkflowApi(workflowToActivate.id);

		if (res.code === '200') {
			ElMessage.success(t('sys.api.operationSuccess'));
			// 更新本地状态
			if (workflow.value && workflow.value.id === workflowToActivate.id) {
				workflow.value.status = 'active';
				workflow.value.isActive = true;
			}
			fetchWorkflows();
		} else {
			ElMessage.error(res.msg || t('sys.api.operationFailed'));
		}
	} finally {
		loading.activateWorkflow = false;
		// 清除当前操作状态
		currentActionWorkflow.value = null;
		currentActionType.value = null;
	}
};

const deactivateWorkflow = async (targetWorkflow?: any) => {
	const workflowToDeactivate = targetWorkflow || workflow.value;
	if (!workflowToDeactivate) return;

	ElMessageBox.confirm(
		`Are you sure you want to set the workflow "${workflowToDeactivate.name}" as inactive? This will stop all active processes and cannot be easily undone.`,
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
						const params = {
							...workflowToDeactivate,
							status: 'inactive',
							isDefault: false,
							stages: null,
						};
						const res = await updateWorkflowApi(workflowToDeactivate.id, params);

						if (res.code === '200') {
							ElMessage.success(t('sys.api.operationSuccess'));
							// 更新本地状态
							if (workflow.value && workflow.value.id === workflowToDeactivate.id) {
								workflow.value.status = 'inactive';
								workflow.value.isActive = false;
							}
							fetchWorkflows();
							done(); // 关闭对话框
						} else {
							ElMessage.error(res.msg || t('sys.api.operationFailed'));
							// 恢复按钮状态
							instance.confirmButtonLoading = false;
							instance.confirmButtonText = 'Set as Inactive';
						}
						// 清除当前操作状态
						currentActionWorkflow.value = null;
						currentActionType.value = null;
					} catch (error) {
						// 恢复按钮状态
						instance.confirmButtonLoading = false;
						instance.confirmButtonText = 'Set as Inactive';
						// 清除当前操作状态
						currentActionWorkflow.value = null;
						currentActionType.value = null;
					}
				} else {
					done(); // 取消或关闭时直接关闭对话框
				}
			},
		}
	);
};

const setAsDefault = async (targetWorkflow?: any, isDefault: boolean = true) => {
	const workflowToSetDefault = targetWorkflow || workflow.value;
	if (!workflowToSetDefault) return;

	try {
		loading.updateWorkflow = true;
		// 调用设置默认工作流API
		const params = {
			...workflowToSetDefault,
			isDefault: isDefault,
			stages: null,
		};

		const res = await updateWorkflowApi(workflowToSetDefault.id, params);

		if (res.code === '200') {
			isDefault && ElMessage.success(t('sys.api.operationSuccess'));
			// 重新获取工作流列表以更新所有工作流的默认状态
			await fetchWorkflows();

			// 如果当前在详情视图，更新当前显示的 workflow 数据
			if (viewMode.value === 'detail' && workflow.value) {
				const updatedWorkflowData = workflowListData.value.find(
					(wf) => wf.id === workflow.value!.id
				);
				if (updatedWorkflowData) {
					// 保留 stages 数据，只更新基本信息
					workflow.value = {
						...updatedWorkflowData,
						stages: workflow.value.stages,
					};
				}
			}
		} else {
			ElMessage.error(res.msg || t('sys.api.operationFailed'));
		}
	} finally {
		loading.updateWorkflow = false;
		// 清除当前操作状态
		currentActionWorkflow.value = null;
		currentActionType.value = null;
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
		// 注意：权限检查已在 StageForm.vue 组件内部完成，这里不需要重复检查

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

const exportWorkflow = async (targetWorkflow?: any) => {
	const workflowToExport = targetWorkflow || workflow.value;
	if (!workflowToExport) return;

	try {
		loading.exportWorkflow = true;
		// 调用导出工作流API，直接返回文件流
		const res = await exportWorkflowToExcel(workflowToExport.id);

		// 检查返回的是否为文件流
		if (res instanceof Blob) {
			// 生成基础文件名（不含扩展名）
			const baseFileName = `${workflowToExport.name}_workflow_${
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
		// 清除当前操作状态
		currentActionWorkflow.value = null;
		currentActionType.value = null;
	}
};

const duplicateWorkflow = async (targetWorkflow?: any) => {
	const workflowToDuplicate = targetWorkflow || workflow.value;
	if (!workflowToDuplicate) return;

	try {
		loading.duplicateWorkflow = true;
		const params = {
			name: `${workflowToDuplicate.name} (Copy)`,
			description: workflowToDuplicate.description,
			setAsDefault: false,
		};

		const res = await duplicateWorkflowApi(workflowToDuplicate.id, params);

		if (res.code === '200') {
			ElMessage.success(t('sys.api.operationSuccess'));
			// 重新获取工作流列表
			await fetchWorkflows();
		} else {
			ElMessage.error(res.msg || t('sys.api.operationFailed'));
		}
	} finally {
		loading.duplicateWorkflow = false;
		// 清除当前操作状态
		currentActionWorkflow.value = null;
		currentActionType.value = null;
	}
};

// 检查是否有权限（功能权限 && 数据权限）
const hasWorkflowPermission = (functionalPermission: string) => {
	if (workflow.value && workflow.value.permission) {
		return functionPermission(functionalPermission) && workflow.value.permission.canOperate;
	}
	return functionPermission(functionalPermission);
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

const staticFields = ref<DynamicList[]>([]);
const loadingDynamicField = ref(false);
const loadDynamicField = async () => {
	try {
		loadingDynamicField.value = true;
		const response = await getDynamicField();
		if (response.code === '200') {
			staticFields.value = response?.data || [];
		}
	} finally {
		loadingDynamicField.value = false;
	}
};

onMounted(async () => {
	// 初始化数据
	await fetchWorkflows();
	fetchChecklists();
	fetchQuestionnaires();
	fetchQuickLinks();
	loadDynamicField();

	// 如果路由 query 中有 id，加载详情
	if (routeWorkflowId.value) {
		await loadWorkflowDetail(routeWorkflowId.value);
	}
});
</script>

<style lang="scss" scoped>
.workflow-container {
	margin: 0 auto;
	height: 100%;
}

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

.ai-tag {
	padding: 2px 8px;
	font-size: var(--caption-size);
	display: inline-flex;
	align-items: center;
	margin-left: 8px;
}

.default-tag {
	padding: 2px 8px;
	font-size: var(--caption-size);
	display: inline-flex;
	align-items: center;
	margin-left: 8px;
}

.ai-sparkles {
	font-size: var(--button-2-size);
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

.workflow-card-body {
	@apply py-4 px-2.5 pl-6 flex-1;
}

.workflow-header-actions {
	@apply flex justify-between items-center;
}

.dates-container {
	@apply flex items-center gap-4 text-sm;
}

.stages-header {
	display: flex;
	justify-content: right;
	margin: 16px 0;
}

.stages-header-actions {
	display: flex;
	align-items: center;
	gap: 12px;
}

.action-buttons-group {
	@apply flex items-center gap-3;
}

.workflow-footer {
	@apply flex justify-start items-center m-0;
}

.stage-count {
	@apply font-normal m-0;
}

.dialog-header {
	border-bottom: none;
}

.dialog-title {
	font-size: var(--body-2-size);
	font-weight: 600;
	margin: 0 0 4px 0;
}

.dialog-subtitle {
	color: var(--el-text-color-regular);
	font-size: var(--button-2-size);
	margin: 0;
	font-weight: normal;
	line-height: 1.4;
}

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
<<<<<<< HEAD

<!-- 全局样式用于 MessageBox 删除确认对话框 -->
<style lang="scss">
/* 删除确认对话框样式 */
.delete-confirmation-dialog {
	/* border-radius removed - using rounded-xl class */

	.el-message-box__message {
		color: var(--el-text-color-regular);
		font-size: var(--button-1-size); /* 14px - Item Button 1 */
		line-height: 1.5;
	}
}

/* 停用确认对话框样式 */
.deactivate-confirmation-dialog {
	.el-message-box__message {
		color: var(--el-text-color-regular);
		font-size: var(--button-1-size); /* 14px - Item Button 1 */
		line-height: 1.5;
	}
}

/* 过期日期确认对话框样式 */
.expired-date-confirmation-dialog {
	.el-message-box__message {
		color: var(--el-text-color-regular);
		font-size: var(--button-1-size); /* 14px - Item Button 1 */
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
		background-color: var(--el-fill-color-lighter) !important;
		border-color: var(--el-border-color-dark) !important;
		color: var(--el-text-color-regular) !important;
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
		background-color: var(--el-fill-color-lighter) !important;
		border-color: var(--el-border-color-dark) !important;
		color: var(--el-text-color-regular) !important;

		&:hover {
			background-color: var(--el-fill-color-lighter) !important;
			border-color: var(--el-border-color-dark) !important;
			color: var(--el-text-color-regular) !important;
		}
	}
}
</style>
=======
>>>>>>> feature/1223
