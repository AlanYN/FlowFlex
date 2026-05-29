<template>
	<div class="">
		<!-- 标题和操作区 -->
		<PageHeader
			title="Cases"
			description="Manage and track business cases with customizable workflows and stage progression"
		>
			<template #actions>
				<!-- Tab切换器 -->
				<TabButtonGroup
					v-model="activeView"
					:tabs="tabsConfig"
					size="small"
					type="adaptive"
					class="mr-4"
					@tab-change="handleViewChange"
				/>
				<el-button
					class="page-header-btn page-header-btn-secondary"
					@click="handleExport"
					:loading="loading"
					:disabled="loading"
					v-if="functionPermission(ProjectPermissionEnum.case.read)"
				>
					<el-icon><Download /></el-icon>
					Export {{ selectedItems.length > 0 ? `(${selectedItems.length})` : 'All' }}
				</el-button>
				<el-button
					class="page-header-btn page-header-btn-primary"
					type="primary"
					@click="handleNewOnboarding"
					:disabled="loading"
					:loading="loading"
					:icon="Plus"
					v-if="functionPermission(ProjectPermissionEnum.case.create)"
				>
					<span>New Case</span>
				</el-button>
			</template>
		</PageHeader>

		<PrototypeTabs
			v-model="activeView"
			:tabs="tabsConfig"
			type="adaptive"
			size="default"
			class="mb-6"
			:hidden-tab="true"
			@tab-change="handleViewChange"
		>
			<!-- 表格视图 -->
			<TabPane value="table">
				<!-- 搜索区域 -->
				<OnboardFilter
					:life-cycle-stage="lifeCycleStage"
					:all-workflows="allWorkflows"
					:onboarding-stages="onboardingStages"
					:loading="loading"
					:selected-items="selectedItems"
					filterType="table"
					@search="handleFilterSearch"
					@export="handleExport"
				/>
				<div class="">
					<el-table
						:data="onboardingList"
						@selection-change="handleSelectionChange"
						@sort-change="handleSortChange"
						class="w-full rounded-none"
						v-loading="loading"
						:max-height="tableMaxHeight"
						row-key="id"
						border
					>
						<template #empty>
							<slot name="empty">
								<el-empty description="No Data" :image-size="50" />
							</slot>
						</template>
						<el-table-column type="selection" fixed="left" width="50" align="center" />
						<el-table-column label="Actions" fixed="left" width="80">
							<template #default="{ row }">
								<el-dropdown trigger="click">
									<el-button size="small" link :icon="ArrowDownBold" />

									<template #dropdown>
										<el-dropdown-menu>
											<!-- Start Onboarding - 只对Inactive状态显示 -->
											<el-dropdown-item
												v-if="
													row.status === 'Inactive' &&
													hasCasePermission(
														row.id,
														ProjectPermissionEnum.case.update
													)
												"
												@click="handleStartOnboarding(row)"
											>
												<el-icon><VideoPlay /></el-icon>
												Start Onboarding
											</el-dropdown-item>

											<!-- Proceed - 对进行中状态显示 -->
											<el-dropdown-item
												v-if="
													isInProgressStatus(row.status) &&
													hasCasePermission(
														row.id,
														ProjectPermissionEnum.case.update
													)
												"
												@click="handleEdit(row.id)"
											>
												<el-icon><Link /></el-icon>
												Proceed
											</el-dropdown-item>

											<!-- Edit Case -->
											<el-dropdown-item
												@click="handleEditCase(row)"
												v-if="
													hasCasePermission(
														row.id,
														ProjectPermissionEnum.case.update
													)
												"
											>
												<el-icon><Edit /></el-icon>
												Edit
											</el-dropdown-item>

											<!-- View - 对Completed、Force Completed、Paused、Aborted状态显示 -->
											<el-dropdown-item
												v-if="
													(row.status === 'Completed' ||
														row.status === 'Force Completed' ||
														row.status === 'Paused' ||
														isAbortedStatus(row.status)) &&
													functionPermission(
														ProjectPermissionEnum.case.read
													)
												"
												@click="handleEdit(row.id)"
											>
												<el-icon><View /></el-icon>
												View
											</el-dropdown-item>

											<el-divider title="Continue" class="my-0" />
											<!-- Pause - 对进行中状态显示 -->
											<el-dropdown-item
												v-if="
													isInProgressStatus(row.status) &&
													hasCasePermission(
														row.id,
														ProjectPermissionEnum.case.update
													)
												"
												@click="handlePause(row)"
											>
												<el-icon><VideoPause /></el-icon>
												Pause
											</el-dropdown-item>

											<!-- Resume - 对Paused状态显示 -->
											<el-dropdown-item
												v-if="
													row.status === 'Paused' &&
													hasCasePermission(
														row.id,
														ProjectPermissionEnum.case.update
													)
												"
												@click="handleResume(row)"
											>
												<el-icon><VideoPlay /></el-icon>
												Resume
											</el-dropdown-item>

											<!-- Abort - 对进行中状态和暂停状态显示 -->
											<el-dropdown-item
												v-if="
													(isInProgressStatus(row.status) ||
														row.status === 'Paused') &&
													hasCasePermission(
														row.id,
														ProjectPermissionEnum.case.update
													)
												"
												@click="handleAbort(row)"
											>
												<el-icon><Close /></el-icon>
												Abort
											</el-dropdown-item>

											<!-- Force Complete - 对进行中状态和暂停状态显示 -->
											<el-dropdown-item
												v-if="
													(isInProgressStatus(row.status) ||
														row.status === 'Paused') &&
													hasCasePermission(
														row.id,
														ProjectPermissionEnum.case.update
													)
												"
												@click="handleForceComplete(row)"
											>
												<el-icon><Check /></el-icon>
												Force Complete
											</el-dropdown-item>

											<!-- Reactivate - 只对已中止状态显示 -->
											<el-dropdown-item
												v-if="
													isAbortedStatus(row.status) &&
													hasCasePermission(
														row.id,
														ProjectPermissionEnum.case.update
													)
												"
												@click="handleReactivate(row)"
											>
												<el-icon><RefreshRight /></el-icon>
												Reactivate
											</el-dropdown-item>

											<el-divider title="Delete" class="my-0" />

											<!-- Change History -->
											<el-dropdown-item
												v-if="
													functionPermission(
														ProjectPermissionEnum.case.read
													)
												"
											>
												<HistoryButton
													:id="row.id"
													:type="WFEMoudels.Onboarding"
												/>
											</el-dropdown-item>

											<!-- Delete - 对所有状态显示，但有不同的限制 -->
											<el-dropdown-item
												@click="handleDelete(row.id)"
												v-if="
													hasCasePermission(
														row.id,
														ProjectPermissionEnum.case.delete
													)
												"
												class="text-red-500 hover:!bg-red-500 hover:!text-white"
											>
												<el-icon><Delete /></el-icon>
												Delete
											</el-dropdown-item>
										</el-dropdown-menu>
									</template>
								</el-dropdown>
							</template>
						</el-table-column>
						<el-table-column
							prop="caseName"
							label="Case Name"
							sortable="custom"
							min-width="220"
							fixed="left"
						>
							<template #default="{ row }">
								<el-link
									:disabled="!functionPermission(ProjectPermissionEnum.case.read)"
									@click="handleEdit(row.id)"
								>
									<div class="table-cell-content" :title="row.caseName">
										{{ row.caseName }}
									</div>
								</el-link>
							</template>
						</el-table-column>
						<el-table-column
							prop="caseCode"
							label="Case Code"
							sortable="custom"
							width="120"
						>
							<template #default="{ row }">
								<div class="table-cell-content" :title="row.caseCode">
									{{ row.caseCode }}
								</div>
							</template>
						</el-table-column>
						<el-table-column
							prop="workflowName"
							label="Workflow"
							sortable="custom"
							min-width="200"
						>
							<template #default="{ row }">
								<el-tag
									v-if="row.workflowName"
									type="primary"
									:title="row.workflowName"
								>
									{{ row.workflowName }}
								</el-tag>
							</template>
						</el-table-column>
						<el-table-column
							prop="currentStageName"
							label="Stage"
							sortable="custom"
							min-width="200"
						>
							<template #default="{ row }">
								<div class="table-cell-content" :title="row.currentStageName">
									{{ row.currentStageName }}
								</div>
							</template>
						</el-table-column>
						<el-table-column
							prop="priority"
							label="Priority"
							sortable="custom"
							width="130"
						>
							<template #default="{ row }">
								<el-tag :type="getPriorityTagType(row.priority)">
									{{ row.priority }}
								</el-tag>
							</template>
						</el-table-column>
						<el-table-column label="Status" sortable="custom" width="180">
							<template #default="{ row }">
								<!-- 只对有效数据行显示状态 -->
								<template v-if="row.id && row.status">
									<el-tag :type="getStatusTagType(row.status)">
										{{ getDisplayStatus(row.status) }}
									</el-tag>
								</template>
								<!-- 对于虚拟行或无效数据，显示占位符 -->
								<template v-else>
									<el-tag type="info" class="opacity-30">--</el-tag>
								</template>
							</template>
						</el-table-column>
						<el-table-column
							prop="currentStageStartTime"
							label="Start Date"
							width="150"
							sortable="custom"
						>
							<template #default="{ row }">
								<div class="space-y-1">
									<div class="flex items-center">
										<span
											class="table-cell-content flex-1"
											:title="
												timeZoneConvert(
													row.currentStageStartTime,
													false,
													projectDate
												)
											"
										>
											{{
												timeZoneConvert(
													row.currentStageStartTime,
													false,
													projectDate
												)
											}}
										</span>
									</div>
								</div>
							</template>
						</el-table-column>
						<el-table-column
							prop="currentStageEndTime"
							label="End Date"
							width="180"
							sortable="custom"
						>
							<template #default="{ row }">
								<div class="flex items-center gap-x-2">
									<span
										class="table-cell-content"
										:title="
											timeZoneConvert(
												row.currentStageEndTime,
												false,
												projectDate
											)
										"
									>
										{{
											timeZoneConvert(
												row.currentStageEndTime,
												false,
												projectDate
											)
										}}
									</span>
									<el-tooltip
										v-if="isOverdue(row.currentStageEndTime)"
										content="Overdue"
									>
										<el-icon class="flex items-center text-red-500">
											<Warning />
										</el-icon>
									</el-tooltip>
								</div>
							</template>
						</el-table-column>
						<el-table-column
							prop="stageUpdatedBy"
							label="Updated By"
							sortable="custom"
							width="140"
						>
							<template #default="{ row }">
								<div class="table-cell-content" :title="row.stageUpdatedBy">
									{{ row?.stageUpdatedBy || row?.modifyBy }}
								</div>
							</template>
						</el-table-column>
						<el-table-column
							prop="stageUpdatedTime"
							label="Update Time"
							sortable="custom"
							width="250"
						>
							<template #default="{ row }">
								<div
									class="table-cell-content"
									:title="formatDateTime(row?.stageUpdatedTime || row.modifyDate)"
								>
									{{ formatDateTime(row?.stageUpdatedTime || row?.modifyDate) }}
								</div>
							</template>
						</el-table-column>
					</el-table>

					<!-- 分页 -->
					<CustomerPagination
						:total="totalElements"
						:limit="pageSize"
						:page="currentPage"
						:background="true"
						:hidden="loading"
						@pagination="handleLimitUpdate"
						@update:page="handleCurrentChange"
						@update:limit="handlePageUpdate"
					/>
				</div>
			</TabPane>

			<!-- 管道视图 -->
			<TabPane value="pipeline">
				<!-- 搜索区域 -->
				<OnboardFilter
					:life-cycle-stage="lifeCycleStage"
					:all-workflows="allWorkflows"
					:onboarding-stages="onboardingStages"
					:loading="loading"
					:selected-items="selectedItems"
					filterType="pipeline"
					@search="handleFilterSearch"
					@export="handleExport"
				/>
				<PrototypeTabs
					v-model="tabWorkflowId"
					:tabs="[
						{
							name: 'All',
							id: '',
						},
						...allWorkflows,
					]"
					type="adaptive"
					class="mb-6"
					:keys="{
						label: 'name',
						value: 'id',
					}"
				/>
				<div class="mb-4">
					<div class="flex justify-between items-center mb-2">
						<div class="text-sm font-medium filter-label">Filter Stages:</div>
						<div class="text-sm filter-count">Total Records: {{ totalElements }}</div>
					</div>
					<!-- 阶段过滤器 -->
					<StageFilter
						:loading="loading"
						:available-stages="getAllAvailableStages"
						:selected-stages="selectedStages"
						:stage-count-map="stageCountMap"
						@stage-click="toggleStageSelection"
					/>
				</div>

				<!-- 阶段卡片 -->
				<StageCardList
					:loading="loading"
					:active-stages="activeStages"
					:grouped-leads="groupedLeads"
					:is-overdue="isOverdue"
					:get-priority-tag-type="getPriorityTagType"
					:get-stage-count-by-priority="getStageCountByPriority"
					:get-stage-overdue-count="getStageOverdueCount"
					:handle-edit="handleEdit"
				/>
			</TabPane>
		</PrototypeTabs>

		<!-- 新建入职弹窗 -->
		<el-dialog
			v-model="dialogVisible"
			title="Create New Cases"
			:width="bigDialogWidth + 'px'"
			destroy-on-close
			custom-class="onboarding-dialog"
			:show-close="true"
			:close-on-click-modal="false"
			draggable
			@close="handleCancel"
		>
			<template #header>
				<div class="dialog-header">
					<div class="flex items-center gap-x-2">
						<h2 class="dialog-title">
							{{ isEditMode ? 'Edit Case' : 'Create New Case' }}
						</h2>
						<div v-if="isEditMode">
							<el-tag type="primary">{{ formData.caseCode }}</el-tag>
						</div>
					</div>
					<p class="dialog-subtitle">
						{{
							isEditMode
								? 'Modify case information and permissions.'
								: 'Create a new onboarding record for lead management.'
						}}
					</p>
				</div>
			</template>

			<el-form
				ref="formRef"
				:model="formData"
				:rules="formRules"
				label-position="top"
				class="onboarding-form"
			>
				<el-form-item label="Case Name" prop="caseName">
					<el-input
						v-model="formData.caseName"
						placeholder="Input Case Name"
						clearable
						class="w-full rounded-xl"
					/>
				</el-form-item>

				<div class="grid grid-cols-1 md:grid-cols-2 gap-4">
					<el-form-item label="Contact Name" prop="ContactPerson">
						<el-input
							v-model="formData.ContactPerson"
							placeholder="Enter Contact Name"
							clearable
							class="w-full rounded-xl"
						/>
					</el-form-item>

					<el-form-item label="Contact Email" prop="ContactEmail">
						<el-input
							v-model="formData.ContactEmail"
							placeholder="Enter Contact Email Address"
							clearable
							type="email"
							class="w-full rounded-xl"
						/>
					</el-form-item>
				</div>

				<el-form-item label="Life Cycle Stage" prop="lifeCycleStageId">
					<el-select
						v-model="formData.lifeCycleStageId"
						placeholder="Select Life Cycle Stage"
						clearable
						class="w-full rounded-xl"
						@change="changeLifeCycleStage"
					>
						<el-option
							v-for="stage in lifeCycleStage"
							:key="stage.id"
							:label="stage.name"
							:value="stage.id"
						/>
					</el-select>
				</el-form-item>

				<el-form-item label="Priority" prop="priority">
					<el-select
						v-model="formData.priority"
						placeholder="Select Priority"
						clearable
						class="w-full rounded-xl"
					>
						<el-option label="High" value="High" />
						<el-option label="Medium" value="Medium" />
						<el-option label="Low" value="Low" />
					</el-select>
				</el-form-item>

				<el-form-item label="Workflow" prop="workFlowId">
					<el-select
						v-model="formData.workFlowId"
						placeholder="Select Workflow"
						clearable
						class="w-full rounded-xl"
					>
						<el-option
							v-for="workflow in allWorkflows"
							:key="workflow.id"
							:label="workflow.name"
							:value="workflow.id"
							:disabled="!workflow.isActive"
						>
							<div class="flex items-center justify-between">
								<span>{{ workflow.name }}</span>
								<el-tag v-if="!workflow.isActive" type="danger" size="small">
									Inactive
								</el-tag>
								<el-tag v-else type="success" size="small">Active</el-tag>
							</div>
						</el-option>
					</el-select>
				</el-form-item>

				<el-form-item label="Ownership" prop="ownership">
					<FlowflexUserSelector
						v-model="formData.ownership"
						selection-type="user"
						:max-count="1"
						placeholder="Select user"
						clearable
						check-strictly
					/>
				</el-form-item>

				<!-- Access Control Section -->
				<div class="access-control-section">
					<div class="section-header">
						<label class="text-base font-bold">Access Control</label>
						<p class="text-sm text-gray-500">
							Configure who can view and operate on this case
						</p>
					</div>

					<CasePermissionSelector
						v-model="casePermissions"
						:workflow-id="formData.workFlowId"
					/>
				</div>
			</el-form>

			<template #footer>
				<div class="dialog-footer">
					<el-button @click="handleCancel" :disabled="saving">Cancel</el-button>
					<el-button type="primary" @click="handleSave" :loading="saving">Save</el-button>
				</div>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, markRaw, watch, nextTick } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import {
	ArrowDownBold,
	Link,
	Edit,
	Delete,
	Plus,
	VideoPlay,
	VideoPause,
	Close,
	RefreshRight,
	View,
	Warning,
	Download,
} from '@element-plus/icons-vue';
import {
	CasePermissionModeEnum,
	PermissionSubjectTypeEnum,
	ProjectPermissionEnum,
} from '@/enums/permissionEnum';
import FlowflexUserSelector from '@/components/form/flowflexUser/index.vue';
import CasePermissionSelector from './components/CasePermissionSelector.vue';
import {
	queryOnboardings,
	deleteOnboarding,
	createOnboarding,
	updateOnboarding,
	exportOnboarding,
	batchUpdateStaticFieldValues,
	startOnboarding,
	abortOnboarding,
	reactivateOnboarding,
	pauseOnboarding,
	resumeOnboardingWithConfirmation,
	forceCompleteOnboarding,
} from '@/apis/ow/onboarding';
import { getAllStages, getWorkflowList } from '@/apis/ow';
import { OnboardingItem, SearchParams, OnboardingQueryRequest, ApiResponse } from '#/onboard';
import type { FlowflexUser } from '#/golbal';
import { PrototypeTabs, TabPane, TabButtonGroup } from '@/components/PrototypeTabs';
import { useUserStore } from '@/stores/modules/user';
import { menuRoles } from '@/stores/modules/menuFunction';
import {
	defaultStr,
	projectTenMinutesSsecondsDate,
	tableMaxHeight,
	bigDialogWidth,
	projectDate,
} from '@/settings/projectSetting';
import CustomerPagination from '@/components/global/u-pagination/index.vue';
import OnboardFilter from './components/OnboardFilter.vue';
import PageHeader from '@/components/global/PageHeader/index.vue';
import { timeZoneConvert, timeExpiredornot } from '@/hooks/time';
import { useI18n } from '@/hooks/useI18n';
import TableViewIcon from '@assets/svg/onboard/tavleView.svg';
import ProgressViewIcon from '@assets/svg/onboard/progressView.svg';
import { pick, omitBy, isNil } from 'lodash-es';
import StageFilter from './components/StageFilter.vue';
import StageCardList from './components/StageCardList.vue';
import { functionPermission } from '@/hooks';
import { WFEMoudels } from '@/enums/appEnum';

type RuleType =
	| 'string'
	| 'number'
	| 'boolean'
	| 'method'
	| 'regexp'
	| 'integer'
	| 'float'
	| 'array'
	| 'object'
	| 'enum'
	| 'date'
	| 'url'
	| 'hex'
	| 'email'
	| 'pattern'
	| 'any';

const { t } = useI18n();

// Store 实例
const userStore = useUserStore();
const menuStore = menuRoles();

// 入职阶段定义
const onboardingStages = ref<any[]>([]);

// 响应式数据
const router = useRouter();
const route = useRoute();
const loading = ref(false);
const onboardingList = ref<OnboardingItem[]>([]);

const selectedItems = ref<OnboardingItem[]>([]);
const activeView = ref('table');
const currentPage = ref(1);
const pageSize = ref(15);
const totalElements = ref(0);

const handleViewChange = (value: string) => {
	activeView.value = value;
	loadOnboardingList(null, false);
};

// 新建入职弹窗相关状态
const dialogVisible = ref(false);
const formRef = ref();
// 编辑相关状态
const isEditMode = ref(false);
const editingCaseId = ref<string | null>(null);

const formData = reactive({
	caseCode: '',
	caseName: '',
	lifeCycleStageName: '',
	lifeCycleStageId: '',
	priority: '',
	ContactPerson: '',
	ContactEmail: '',
	workFlowId: '',
	// 新增权限字段
	ownership: '',
	viewPermissionMode: CasePermissionModeEnum.Public,
	viewTeams: [] as string[],
	viewUsers: [] as string[],
	viewPermissionSubjectType: PermissionSubjectTypeEnum.Team,
	operateTeams: [] as string[],
	operateUsers: [] as string[],
	operatePermissionSubjectType: PermissionSubjectTypeEnum.Team,
	useSameTeamForOperate: true,
});

const formRules = {
	caseCode: [{ required: false, message: 'Lead ID is required', trigger: 'blur' }],
	priority: [{ required: true, message: 'Priority is required', trigger: 'change' }],
	caseName: [{ required: true, message: 'Customer Name is required', trigger: 'blur' }],
	ContactPerson: [{ required: false, message: 'Contact Name is required', trigger: 'blur' }], // 必填
	ContactEmail: [
		{ required: true, message: 'Contact Email is required', trigger: 'blur' },
		{
			type: 'email' as RuleType,
			message: 'Please enter a valid email address',
			trigger: 'blur',
		},
	], // 必填，且需要验证邮箱格式
	workFlowId: [{ required: true, message: 'Workflow is required', trigger: 'blur' }],
};

// 计算属性适配 CasePermissionSelector
const casePermissions = computed({
	get: () => ({
		viewPermissionMode: formData.viewPermissionMode,
		viewTeams: formData.viewTeams,
		viewUsers: formData.viewUsers,
		viewPermissionSubjectType: formData.viewPermissionSubjectType,
		operateTeams: formData.operateTeams,
		operateUsers: formData.operateUsers,
		operatePermissionSubjectType: formData.operatePermissionSubjectType,
		useSameTeamForOperate: formData.useSameTeamForOperate,
	}),
	set: (value) => {
		formData.viewPermissionMode = value.viewPermissionMode;
		formData.viewTeams = value.viewTeams;
		formData.viewUsers = value.viewUsers;
		formData.viewPermissionSubjectType = value.viewPermissionSubjectType;
		formData.operateTeams = value.operateTeams;
		formData.operateUsers = value.operateUsers;
		formData.operatePermissionSubjectType = value.operatePermissionSubjectType;
		formData.useSameTeamForOperate = value.useSameTeamForOperate;
	},
});

const changeLifeCycleStage = (value: string) => {
	const stage = lifeCycleStage.value.find((stage) => stage.id === value);
	if (stage) {
		formData.lifeCycleStageName = stage.name;
	}
};

const saving = ref(false);

// 搜索参数
const searchParams = reactive<SearchParams>({
	workFlowId: '',
	caseCode: '',
	caseName: '',
	lifeCycleStageName: '',
	currentStageId: '',
	updatedBy: '',
	priority: '',
	page: 1,
	size: 15,
});

// 标签页配置
const tabsConfig = ref([
	{
		value: 'table',
		label: 'Table View',
		icon: markRaw(TableViewIcon),
	},
	{
		value: 'pipeline',
		label: 'Progress View',
		icon: markRaw(ProgressViewIcon),
	},
]);

// 添加选中的阶段状态
const selectedStages = ref<string[]>([]);

// 过滤后的管道数据
const filteredPipelineList = computed(() => {
	let filtered = pipelineOnboardingList.value;

	// 根据工作流ID过滤
	if (tabWorkflowId.value && tabWorkflowId.value !== '') {
		filtered = filtered.filter((item) => item.workflowId === tabWorkflowId.value);
	}

	// 根据选中的阶段过滤
	if (selectedStages.value.length > 0) {
		filtered = filtered.filter((item) => selectedStages.value.includes(item.currentStageName));
	}

	return filtered;
});

const groupedLeads = computed(() => {
	const grouped: Record<string, OnboardingItem[]> = {};

	// 按阶段分组过滤后的数据
	filteredPipelineList.value.forEach((item) => {
		if (grouped[item.currentStageName]) {
			grouped[item.currentStageName].push(item);
		} else {
			grouped[item.currentStageName] = [item];
		}
	});

	return grouped;
});

const activeStages = computed(() => {
	return Object.keys(groupedLeads.value).filter((stage) => groupedLeads.value[stage].length > 0);
});

// 获取所有可用的阶段（根据当前工作流过滤）
const getAllAvailableStages = computed(() => {
	const stages = new Set<string>();

	// 根据当前工作流过滤数据
	let dataToCheck = pipelineOnboardingList.value;
	if (tabWorkflowId.value && tabWorkflowId.value !== '') {
		dataToCheck = dataToCheck.filter((item) => item.workflowId === tabWorkflowId.value);
	}

	// 收集所有阶段
	dataToCheck.forEach((item) => {
		stages.add(item.currentStageName);
	});

	return Array.from(stages);
});

// 获取每个阶段的数量（根据当前工作流过滤，但不考虑阶段过滤）
const getAllStageCount = (stage: string) => {
	let dataToCheck = pipelineOnboardingList.value;

	// 根据当前工作流过滤数据
	if (tabWorkflowId.value && tabWorkflowId.value !== '') {
		dataToCheck = dataToCheck.filter((item) => item.workflowId === tabWorkflowId.value);
	}

	return dataToCheck.filter((item) => item.currentStageName === stage).length;
};

// 计算阶段数量映射表
const stageCountMap = computed(() => {
	const countMap: Record<string, number> = {};
	getAllAvailableStages.value.forEach((stage) => {
		countMap[stage] = getAllStageCount(stage);
	});
	return countMap;
});

// 切换阶段选中状态
const toggleStageSelection = (stage: string) => {
	const index = selectedStages.value.indexOf(stage);
	if (index > -1) {
		selectedStages.value.splice(index, 1);
	} else {
		selectedStages.value.push(stage);
	}
};

// API调用函数
const loadOnboardingList = async (event?: any, trigger: boolean = true) => {
	try {
		loading.value = true;
		if (activeView.value === 'table') {
			if (!trigger && onboardingList.value.length > 0) return;
			await getTableViewOnboarding(event);
		} else {
			if (!trigger && pipelineOnboardingList.value.length > 0) return;
			await getPipelineViewOnboarding();
		}
	} finally {
		loading.value = false;
	}
};

const getTableViewOnboarding = async (event) => {
	try {
		const queryParams: OnboardingQueryRequest = {
			pageIndex: currentPage.value,
			pageSize: pageSize.value,
			...event,
			...sortordObj.value,
			...omitBy(
				pick(searchParams, [
					'caseCode',
					'caseName',
					'lifeCycleStageName',
					'currentStageId',
					'updatedBy',
					'priority',
					'workFlowId',
				]),
				(value) => isNil(value) || value === ''
			),
		};

		const res: ApiResponse<OnboardingItem> = await queryOnboardings(queryParams);

		if (res.code === '200') {
			// 确保每个项目都有status字段
			const processedData = (res.data.data || []).map((item) => ({
				...item,
				status: item.status || 'Unknown', // 确保status字段存在
			}));

			onboardingList.value = processedData;
			totalElements.value = res.data.total || 0;
		} else {
			onboardingList.value = [];
			totalElements.value = 0;
		}
	} catch (error) {
		onboardingList.value = [];
		totalElements.value = 0;
	}
};

const pipelineOnboardingList = ref<OnboardingItem[]>([]);
const getPipelineViewOnboarding = async () => {
	try {
		const queryParams: OnboardingQueryRequest = {
			allData: true,
			...omitBy(
				pick(searchParams, [
					'caseCode',
					'caseName',
					'lifeCycleStageName',
					'currentStageId',
					'updatedBy',
					'priority',
					'workFlowId',
				]),
				(value) => isNil(value) || value === ''
			),
		};
		const res = await queryOnboardings(queryParams);
		if (res.code === '200') {
			pipelineOnboardingList.value = res.data.data || [];
		} else {
			pipelineOnboardingList.value = [];
		}
	} catch {
		pipelineOnboardingList.value = [];
	}
};

// 工具函数
const getPriorityTagType = (priority: string) => {
	switch (priority.toLowerCase()) {
		case 'high':
			return 'danger';
		case 'medium':
			return 'warning';
		case 'low':
			return 'success';
		default:
			return 'info';
	}
};

const getStatusTagType = (status: string) => {
	// 处理Element Plus表格虚拟行的情况
	if (status === undefined || status === null) {
		// 这通常是Element Plus表格的虚拟行，静默处理
		return 'info';
	}

	switch (status) {
		case 'Inactive':
			return 'info';
		case 'Active':
		case 'InProgress':
		case 'Started':
			return 'primary';
		case 'Completed':
			return 'success';
		case 'Force Completed':
			return 'success';
		case 'Paused':
			return 'warning';
		case 'Aborted':
		case 'Cancelled':
			return 'danger';
		default:
			return 'info';
	}
};

// 状态显示转换函数 - 统一显示逻辑
const getDisplayStatus = (status: string) => {
	if (status === undefined || status === null) {
		return 'Unknown';
	}

	switch (status) {
		case 'Active':
		case 'Started':
			return 'InProgress';
		case 'Cancelled':
			return 'Aborted';
		case 'Force Completed':
			return 'Force Completed';
		default:
			return status;
	}
};

// 判断是否为进行中状态的辅助函数
const isInProgressStatus = (status: string) => {
	return status === 'Active' || status === 'InProgress' || status === 'Started';
};

// 判断是否为已中止状态的辅助函数
const isAbortedStatus = (status: string) => {
	return status === 'Aborted' || status === 'Cancelled';
};

const isOverdue = (eta: string | null) => {
	if (!eta) return false;
	return timeExpiredornot(eta);
};

const formatDateTime = (dateString: string | null) => {
	if (!dateString) return defaultStr;
	try {
		return timeZoneConvert(dateString, false, projectTenMinutesSsecondsDate);
	} catch {
		return defaultStr;
	}
};

const getStageCountByPriority = (stage: string, priority: string) => {
	return (
		groupedLeads.value[stage]?.filter(
			(item) => item.priority.toLowerCase() === priority.toLowerCase()
		).length || 0
	);
};

const getStageOverdueCount = (stage: string) => {
	return (
		groupedLeads.value[stage]?.filter((item) => isOverdue(item.currentStageEndTime)).length || 0
	);
};

// 事件处理函数
const handleFilterSearch = async (params: SearchParams) => {
	// 更新搜索参数
	Object.assign(searchParams, params);
	currentPage.value = 1;
	searchParams.page = 1;
	await loadOnboardingList();
};

const handleSelectionChange = (selection: OnboardingItem[]) => {
	selectedItems.value = selection;
};

const sortordObj = ref<any>({});
const handleSortChange = (event: any) => {
	// 这里可以添加排序逻辑，发送到后端
	sortordObj.value = event.order
		? {
				sortDirection: event.order && event.order == 'ascending' ? 'asc' : 'desc',
				sortField: event.order ? event.prop : '',
		  }
		: {};
	loadOnboardingList();
	return false;
};

const handleEdit = (itemId: string) => {
	router.push(`/onboard/onboardDetail?onboardingId=${itemId}`);
};

const handleNewOnboarding = async () => {
	if (allWorkflows.value.length > 0) {
		formData.workFlowId = allWorkflows.value.find((item) => item.isDefault)?.id || '';

		// 自动填充当前用户
		await autoFillCurrentUser();

		dialogVisible.value = true;
	} else {
		// End date已过期，显示警告提示
		ElMessageBox.confirm(
			`⚠️ Warning: No default workflow found. Please create a default workflow first.`,
			'No Workflow Found',
			{
				confirmButtonText: 'Create Workflow',
				cancelButtonText: 'Cancel',
				confirmButtonClass: 'warning-confirm-btn',
				cancelButtonClass: 'cancel-confirm-btn',
				distinguishCancelAndClose: true,
				customClass: 'expired-date-confirmation-dialog',
				showCancelButton: true,
				showConfirmButton: true,
				beforeClose: async (action, instance, done) => {
					if (action === 'confirm') {
						done();
						router.push('/onboard/onboardWorkflow');
					} else {
						done(); // 取消或关闭时直接关闭对话框
					}
				},
			}
		);
	}
};

const handleDelete = async (itemId: string) => {
	try {
		await ElMessageBox.confirm(
			'Are you sure you want to delete this lead? This action cannot be undone.',
			'⚠️ Confirm Deletion',
			{
				confirmButtonText: 'Delete',
				cancelButtonText: 'Cancel',
				confirmButtonClass: 'danger-confirm-btn',
				cancelButtonClass: 'cancel-confirm-btn',
				distinguishCancelAndClose: true,
				customClass: 'delete-confirmation-dialog',
				showCancelButton: true,
				showConfirmButton: true,
				beforeClose: async (action, instance, done) => {
					if (action === 'confirm') {
						instance.confirmButtonLoading = true;
						instance.confirmButtonText = 'Deleting...';
						try {
							const res = await deleteOnboarding(itemId, true);
							if (res.code == '200') {
								ElMessage.success(t('sys.api.operationSuccess'));
								done();
								await loadOnboardingList();
							} else {
								ElMessage.error(res.msg || t('sys.api.operationFailed'));
								instance.confirmButtonLoading = false;
								instance.confirmButtonText = 'Delete';
							}
						} catch {
							instance.confirmButtonLoading = false;
							instance.confirmButtonText = 'Delete';
						}
					} else {
						done();
					}
				},
			}
		);
	} catch {
		// User cancelled, do nothing
	}
};

// ========================= 状态管理函数 =========================

const handleStartOnboarding = async (row: OnboardingItem) => {
	ElMessageBox.confirm(
		`Are you sure you want to start the onboarding process for "${row.caseName}"? This will activate the onboarding and begin the workflow.`,
		'⚡ Start Onboarding',
		{
			confirmButtonText: 'Start Onboarding',
			cancelButtonText: 'Cancel',
			distinguishCancelAndClose: true,
			showCancelButton: true,
			showConfirmButton: true,
			beforeClose: async (action, instance, done) => {
				if (action === 'confirm') {
					instance.confirmButtonLoading = true;
					instance.confirmButtonText = 'Starting...';

					try {
						const res = await startOnboarding(row.id, {
							reason: 'Manual activation from onboarding list',
							resetProgress: true,
						});

						if (res.code === '200') {
							ElMessage.success('Onboarding started successfully');
							await loadOnboardingList();
						} else {
							ElMessage.error(res.msg || 'Failed to start onboarding');
						}
					} finally {
						instance.confirmButtonLoading = false;
						instance.confirmButtonText = 'Start Onboarding';
					}
					done();
				} else {
					done();
				}
			},
		}
	);
};

const handlePause = async (row: OnboardingItem) => {
	ElMessageBox.confirm(
		`Are you sure you want to pause the onboarding process for "${row.caseName}"? The account will stay at the current stage and lose ETA. All workflow content will become read-only.`,
		'⏸️ Pause Onboarding',
		{
			confirmButtonText: 'Pause',
			cancelButtonText: 'Cancel',
			distinguishCancelAndClose: true,
			showCancelButton: true,
			showConfirmButton: true,
			beforeClose: async (action, instance, done) => {
				if (action === 'confirm') {
					instance.confirmButtonLoading = true;
					instance.confirmButtonText = 'Pausing...';

					try {
						const res = await pauseOnboarding(row.id);

						if (res.code === '200') {
							ElMessage.success('Onboarding paused successfully');
							await loadOnboardingList();
						} else {
							ElMessage.error(res.msg || 'Failed to pause onboarding');
						}
					} catch (error) {
						ElMessage.error('Failed to pause onboarding');
					} finally {
						instance.confirmButtonLoading = false;
						instance.confirmButtonText = 'Pause';
					}
					done();
				} else {
					done();
				}
			},
		}
	);
};

const handleResume = async (row: OnboardingItem) => {
	ElMessageBox.confirm(
		`Are you sure you want to resume the onboarding process for "${row.caseName}"? The account will restore ETA and current stage timing will continue from where it was paused.`,
		'▶️ Resume Onboarding',
		{
			confirmButtonText: 'Resume',
			cancelButtonText: 'Cancel',
			distinguishCancelAndClose: true,
			showCancelButton: true,
			showConfirmButton: true,
			beforeClose: async (action, instance, done) => {
				if (action === 'confirm') {
					instance.confirmButtonLoading = true;
					instance.confirmButtonText = 'Resuming...';

					try {
						const res = await resumeOnboardingWithConfirmation(row.id, {
							reason: 'Manual resume from onboarding list',
						});

						if (res.code === '200') {
							ElMessage.success('Onboarding resumed successfully');
							await loadOnboardingList();
						} else {
							ElMessage.error(res.msg || 'Failed to resume onboarding');
						}
					} catch (error) {
						ElMessage.error('Failed to resume onboarding');
					} finally {
						instance.confirmButtonLoading = false;
						instance.confirmButtonText = 'Resume';
					}
					done();
				} else {
					done();
				}
			},
		}
	);
};

const handleAbort = async (row: OnboardingItem) => {
	ElMessageBox.prompt(
		`Are you sure you want to abort the onboarding process for "${row.caseName}"? This will terminate the process and the account will exit the onboarding workflow. Please provide a reason for this action.`,
		'🛑 Abort Onboarding',
		{
			confirmButtonText: 'Abort',
			cancelButtonText: 'Cancel',
			inputPlaceholder: 'Enter reason for aborting...',
			inputValidator: (value) => {
				if (!value || value.trim().length === 0) {
					return 'Reason is required for aborting onboarding';
				}
				return true;
			},
			beforeClose: async (action, instance, done) => {
				if (action === 'confirm') {
					instance.confirmButtonLoading = true;
					instance.confirmButtonText = 'Aborting...';

					try {
						const res = await abortOnboarding(row.id, {
							reason: instance.inputValue,
							notes: 'Aborted from onboarding list',
						});

						if (res.code === '200') {
							ElMessage.success('Onboarding aborted successfully');
							await loadOnboardingList();
						} else {
							ElMessage.error(res.msg || 'Failed to abort onboarding');
						}
					} catch (error) {
						ElMessage.error('Failed to abort onboarding');
					} finally {
						instance.confirmButtonLoading = false;
						instance.confirmButtonText = 'Abort';
					}
					done();
				} else {
					done();
				}
			},
		}
	);
};

const handleReactivate = async (row: OnboardingItem) => {
	ElMessageBox.prompt(
		`Are you sure you want to reactivate the onboarding process for "${row.caseName}"? This will restart the process from stage 1 while preserving questionnaire answers. Please provide a reason for this action.`,
		'🔄 Reactivate Onboarding',
		{
			confirmButtonText: 'Reactivate',
			cancelButtonText: 'Cancel',
			inputPlaceholder: 'Enter reason for reactivation...',
			inputValidator: (value) => {
				if (!value || value.trim().length === 0) {
					return 'Reason is required for reactivation';
				}
				return true;
			},
			beforeClose: async (action, instance, done) => {
				if (action === 'confirm') {
					instance.confirmButtonLoading = true;
					instance.confirmButtonText = 'Reactivating...';

					try {
						const res = await reactivateOnboarding(row.id, {
							reason: instance.inputValue,
							resetProgress: true,
							preserveAnswers: true,
							notes: 'Reactivated from onboarding list',
						});

						if (res.code === '200') {
							ElMessage.success('Onboarding reactivated successfully');
							await loadOnboardingList();
						} else {
							ElMessage.error(res.msg || 'Failed to reactivate onboarding');
						}
					} catch (error) {
						ElMessage.error('Failed to reactivate onboarding');
					} finally {
						instance.confirmButtonLoading = false;
						instance.confirmButtonText = 'Reactivate';
					}
					done();
				} else {
					done();
				}
			},
		}
	);
};

const handleForceComplete = async (row: OnboardingItem) => {
	ElMessageBox.prompt(
		`Are you sure you want to force complete the onboarding process for "${row.caseName}"? This action will bypass all validation and mark the onboarding as Force Completed. Please provide a reason for this action.`,
		'⚠️ Force Complete Onboarding',
		{
			confirmButtonText: 'Force Complete',
			cancelButtonText: 'Cancel',
			inputPlaceholder: 'Enter reason for force completion...',
			inputValidator: (value) => {
				if (!value || value.trim().length === 0) {
					return 'Reason is required for force completion';
				}
				return true;
			},
			beforeClose: async (action, instance, done) => {
				if (action === 'confirm') {
					const reason = instance.inputValue?.trim();
					if (!reason) {
						return;
					}

					instance.confirmButtonLoading = true;
					instance.confirmButtonText = 'Force Completing...';

					try {
						const res = await forceCompleteOnboarding(row.id, {
							reason: reason,
							completionNotes: 'Force completed from onboarding list',
						});

						if (res.code === '200') {
							ElMessage.success('Onboarding force completed successfully');
							await loadOnboardingList();
						} else {
							ElMessage.error(res.msg || 'Failed to force complete onboarding');
						}
					} finally {
						instance.confirmButtonLoading = false;
						instance.confirmButtonText = 'Force Complete';
					}
					done();
				} else {
					done();
				}
			},
		}
	);
};

const handleExport = async () => {
	try {
		loading.value = true;

		// 构建导出参数
		let exportParams: any = {};
		let exportMessage = '';

		// 如果有选中的数据，优先导出选中的数据
		if (selectedItems.value.length > 0) {
			// 导出选中的数据 - 使用选中项的 onboardingId，转换为逗号分隔的字符串
			const selectedOnboardingIds = selectedItems.value.map((item) => item.id).join(',');
			exportParams = {
				onboardingIds: selectedOnboardingIds,
				pageSize: 10000, // 大页面以确保获取所有匹配的数据
			};
			exportMessage = `Selected ${selectedItems.value.length} items exported successfully`;
		} else {
			// 没有选中数据时，按当前搜索条件导出
			exportParams = {
				...omitBy(
					pick(searchParams, [
						'caseCode',
						'caseName',
						'lifeCycleStageName',
						'currentStageId',
						'updatedBy',
						'priority',
						'workFlowId',
					]),
					(value) => isNil(value) || value === ''
				),
				pageSize: 10000, // 大页面以确保获取所有匹配的数据
			};
			exportMessage = 'Filtered data exported successfully';
		}

		// 调用导出接口
		const response = await exportOnboarding(exportParams);

		// 创建下载链接
		const blob = new Blob([response], {
			type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
		});
		const url = window.URL.createObjectURL(blob);
		const link = document.createElement('a');
		link.href = url;

		// 设置文件名，包含时间戳和导出类型
		const timestamp = new Date()
			.toISOString()
			.slice(0, 19)
			.replace(/[-:]/g, '')
			.replace('T', '_');
		const fileNameSuffix = selectedItems.value.length > 0 ? 'Selected' : 'Filtered';
		link.download = `Cases_${fileNameSuffix}_${timestamp}.xlsx`;

		// 触发下载
		document.body.appendChild(link);
		link.click();
		document.body.removeChild(link);
		window.URL.revokeObjectURL(url);

		ElMessage.success(exportMessage);
	} finally {
		loading.value = false;
	}
};

const handlePageUpdate = async (size: number) => {
	pageSize.value = size;
	searchParams.size = size;
	currentPage.value = 1;
	searchParams.page = 1;
};

const handleCurrentChange = async (page: number) => {
	currentPage.value = page;
	searchParams.page = page;
};

const handleLimitUpdate = async () => {
	await loadOnboardingList();
};

// 新建入职相关方法
// 编辑 Case
const handleEditCase = (row: any) => {
	isEditMode.value = true;
	editingCaseId.value = row.id;

	// 使用列表数据直接回显
	formData.caseName = row.caseName || '';
	formData.caseCode = row.caseCode || '';
	formData.ContactPerson = row.contactPerson || '';
	formData.ContactEmail = row.contactEmail || '';
	formData.lifeCycleStageId = row.lifeCycleStageId || '';
	formData.lifeCycleStageName = row.lifeCycleStageName || ''; // ✅ 添加 lifeCycleStageName 回显
	formData.priority = row.priority || '';
	formData.workFlowId = row.workflowId || '';
	formData.ownership = row.ownership || '';
	formData.viewPermissionMode = row.viewPermissionMode ?? CasePermissionModeEnum.Public;
	formData.viewTeams = row.viewTeams || [];
	formData.viewUsers = row.viewUsers || [];
	formData.viewPermissionSubjectType =
		row.viewPermissionSubjectType ?? PermissionSubjectTypeEnum.Team;
	formData.operateTeams = row.operateTeams || [];
	formData.operateUsers = row.operateUsers || [];
	formData.operatePermissionSubjectType =
		row.operatePermissionSubjectType ?? PermissionSubjectTypeEnum.Team;
	formData.useSameTeamForOperate = row.useSameTeamForOperate ?? true;
	// 打开弹窗
	dialogVisible.value = true;
};

// 自动填充当前用户到 ownership
const autoFillCurrentUser = async () => {
	try {
		// 获取当前登录用户信息
		const currentUser = userStore.getUserInfo;
		if (!currentUser || !currentUser.userId || !currentUser.userName) {
			return;
		}

		// 获取用户数据列表（使用缓存）
		const userData = await menuStore.getFlowflexUserDataWithCache();
		if (!userData || userData.length === 0) {
			return;
		}

		// 递归查找当前用户
		const findUserById = (data: FlowflexUser[], userId: string): FlowflexUser | null => {
			for (const item of data) {
				if (item.type === 'user' && item.id === userId) {
					return item;
				}
				if (item.children && item.children.length > 0) {
					const found = findUserById(item.children, userId);
					if (found) return found;
				}
			}
			return null;
		};

		const currentUserData = findUserById(userData, String(currentUser.userId));

		// 如果找到用户，自动填充
		if (currentUserData) {
			formData.ownership = currentUserData.id;
		} else {
			// 留空，不填充
		}
	} catch (error) {
		// 出错时留空
	}
};

const handleCancel = () => {
	dialogVisible.value = false;
	isEditMode.value = false;
	editingCaseId.value = null;
	resetForm();
};

const resetForm = () => {
	formData.caseCode = '';
	formData.caseName = '';
	formData.lifeCycleStageName = '';
	formData.lifeCycleStageId = '';
	formData.priority = '';
	formData.ContactPerson = '';
	formData.ContactEmail = '';
	formData.workFlowId = '';
	formData.ownership = '';
	formData.viewPermissionMode = CasePermissionModeEnum.Public;
	formData.viewTeams = [];
	formData.viewUsers = [];
	formData.viewPermissionSubjectType = PermissionSubjectTypeEnum.Team;
	formData.operateTeams = [];
	formData.operateUsers = [];
	formData.operatePermissionSubjectType = PermissionSubjectTypeEnum.Team;
	formData.useSameTeamForOperate = true;
	if (formRef.value) {
		formRef.value.clearValidate();
	}
};

// 验证并检查权限：验证必填项 + 检查当前用户是否会被权限设置排除
const validateAndCheckPermissions = async (): Promise<{
	hasWarning: boolean;
	showMessage: boolean;
	warningMessage: string;
}> => {
	if (
		formData.viewPermissionMode === CasePermissionModeEnum.InvisibleTo ||
		formData.viewPermissionMode === CasePermissionModeEnum.VisibleTo
	) {
		// Validate View Permission selection
		if (formData.viewPermissionSubjectType === PermissionSubjectTypeEnum.User) {
			formData.viewTeams = [];
			if (formData.viewUsers.length === 0) {
				return {
					hasWarning: false,
					showMessage: true,
					warningMessage: 'Please select at least one user for View Permission.',
				};
			}
		} else if (formData.viewPermissionSubjectType === PermissionSubjectTypeEnum.Team) {
			formData.viewUsers = [];
			if (formData.viewTeams.length === 0) {
				return {
					hasWarning: false,
					showMessage: true,
					warningMessage: 'Please select at least one team for View Permission.',
				};
			}
		}
		// Validate Operate Permission selection
		if (formData.operatePermissionSubjectType === PermissionSubjectTypeEnum.User) {
			formData.operateTeams = [];
			if (formData.operateUsers.length === 0) {
				return {
					hasWarning: false,
					showMessage: true,
					warningMessage: 'Please select at least one user for Operate Permission.',
				};
			}
		} else if (formData.operatePermissionSubjectType === PermissionSubjectTypeEnum.Team) {
			formData.operateUsers = [];
			if (formData.operateTeams.length === 0) {
				return {
					hasWarning: false,
					showMessage: true,
					warningMessage: 'Please select at least one team for Operate Permission.',
				};
			}
		}
	} else {
		formData.viewTeams = [];
		formData.viewUsers = [];
		formData.operateTeams = [];
		formData.operateUsers = [];
	}

	// 只在 VisibleTo 或 InvisibleTo 模式下检查
	if (
		formData.viewPermissionMode !== CasePermissionModeEnum.VisibleTo &&
		formData.viewPermissionMode !== CasePermissionModeEnum.InvisibleTo
	) {
		return { hasWarning: false, showMessage: false, warningMessage: '' };
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
		const userTeams = findUserTeams(userData, currentUserId);
		//是否包含不可用team 和 user, 如果包含，则返回警告
		const collectNodeIds = (nodes: FlowflexUser[]) => {
			const teamIds = new Set<string>();
			const userIds = new Set<string>();
			const traverse = (items: FlowflexUser[]) => {
				items.forEach((item) => {
					if (item.type === 'team') {
						teamIds.add(item.id);
					}
					if (item.type === 'user') {
						userIds.add(item.id);
					}
					if (item.children && item.children.length > 0) {
						traverse(item.children);
					}
				});
			};
			traverse(nodes);
			return { teamIds, userIds };
		};

		const { teamIds, userIds } = collectNodeIds(userData);
		const missingSelections = [
			...formData.viewUsers.filter((id) => !userIds.has(id)),
			...formData.operateUsers.filter((id) => !userIds.has(id)),
			...formData.viewTeams.filter((id) => !teamIds.has(id)),
			...formData.operateTeams.filter((id) => !teamIds.has(id)),
		];

		if (missingSelections.length > 0) {
			return {
				hasWarning: false,
				showMessage: true,
				warningMessage:
					'Some selected teams or users no longer exist. Please update your selection.',
			};
		}

		let isUserExcludedFromView = false;
		let isUserExcludedFromOperate = false;

		// 检查 View Permission
		if (formData.viewPermissionSubjectType === PermissionSubjectTypeEnum.User) {
			const isInList = formData.viewUsers.includes(currentUserId);
			// 白名单：不在列表中 = 被排除；黑名单：在列表中 = 被排除
			isUserExcludedFromView =
				formData.viewPermissionMode === CasePermissionModeEnum.VisibleTo
					? !isInList
					: isInList;
		} else if (formData.viewPermissionSubjectType === PermissionSubjectTypeEnum.Team) {
			const isInList = userTeams.some((teamId) => formData.viewTeams.includes(teamId));
			// 白名单：不在列表中 = 被排除；黑名单：在列表中 = 被排除
			isUserExcludedFromView =
				formData.viewPermissionMode === CasePermissionModeEnum.VisibleTo
					? !isInList
					: isInList;
		}

		// 检查 Operate Permission
		if (formData.operatePermissionSubjectType === PermissionSubjectTypeEnum.User) {
			const isInList = formData.operateUsers.includes(currentUserId);
			// 白名单：不在列表中 = 被排除；黑名单：在列表中 = 被排除
			isUserExcludedFromOperate =
				formData.viewPermissionMode === CasePermissionModeEnum.VisibleTo
					? !isInList
					: isInList;
		} else if (formData.operatePermissionSubjectType === PermissionSubjectTypeEnum.Team) {
			const isInList = userTeams.some((teamId) => formData.operateTeams.includes(teamId));
			// 白名单：不在列表中 = 被排除；黑名单：在列表中 = 被排除
			isUserExcludedFromOperate =
				formData.viewPermissionMode === CasePermissionModeEnum.VisibleTo
					? !isInList
					: isInList;
		}

		// 生成警告信息
		if (isUserExcludedFromView || isUserExcludedFromOperate) {
			let warningMessage = '';
			if (isUserExcludedFromView && isUserExcludedFromOperate) {
				warningMessage =
					'Warning: You are setting permissions that will exclude yourself from viewing and operating this case. You will not be able to access this case after saving. Do you want to continue?';
			} else if (isUserExcludedFromView) {
				warningMessage =
					'Warning: You are setting permissions that will exclude yourself from viewing this case. You will not be able to access this case after saving. Do you want to continue?';
			} else {
				warningMessage =
					'Warning: You are setting permissions that will exclude yourself from operating this case. You will be able to view but not operate on this case after saving. Do you want to continue?';
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

const handleSave = async () => {
	if (!formRef.value) return;

	try {
		// 表单验证
		await formRef.value.validate();

		// 验证并检查权限：验证必填项 + 检查当前用户是否会被权限设置排除
		const permissionCheck = await validateAndCheckPermissions();
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

		saving.value = true;

		// 根据 ownership ID 获取用户名
		let ownershipName = '';
		if (formData.ownership && formData.ownership.trim() !== '') {
			try {
				const userData = await menuStore.getFlowflexUserDataWithCache();
				if (userData && userData.length > 0) {
					// 递归查找用户
					const findUserById = (
						data: FlowflexUser[],
						userId: string
					): FlowflexUser | null => {
						for (const item of data) {
							if (item.type === 'user' && item.id === userId) {
								return item;
							}
							if (item.children && item.children.length > 0) {
								const found = findUserById(item.children, userId);
								if (found) return found;
							}
						}
						return null;
					};

					const ownershipUser = findUserById(userData, formData.ownership);
					if (ownershipUser) {
						ownershipName = ownershipUser.name || '';
					}
				}
			} catch (error) {
				console.error('Failed to get ownership name:', error);
			}
		}

		// 处理 ownership 字段：如果为空字符串，转换为 null
		const submitData = {
			...formData,
			ownership:
				formData.ownership && formData.ownership.trim() !== '' ? formData.ownership : null,
			ownershipName: ownershipName || null,
		};

		let res;
		if (isEditMode.value && editingCaseId.value) {
			// 编辑模式：调用更新接口
			res = await updateOnboarding(editingCaseId.value, submitData);
		} else {
			// 创建模式：调用创建接口
			res = await createOnboarding(submitData);
		}

		if (res.code === '200') {
			const onboardingId = res.data;

			// 仅在创建模式下更新静态字段值（编辑模式不调用）
			if (!isEditMode.value && onboardingId) {
				try {
					const fieldValues: Array<{
						fieldName: string;
						fieldValueJson: string;
						fieldType: string;
						isRequired: boolean;
						fieldLabel: string;
					}> = [];

					// 字段映射关系
					const fieldMapping = {
						caseName: {
							apiField: 'CUSTOMERNAME',
							type: 'text',
							required: true,
							label: 'Customer Name',
						},
						ContactPerson: {
							apiField: 'CONTACTNAME',
							type: 'text',
							required: true,
							label: 'Contact Name',
						},
						ContactEmail: {
							apiField: 'CONTACTEMAIL',
							type: 'email',
							required: true,
							label: 'Contact Email',
						},
						priority: {
							apiField: 'PRIORITY',
							type: 'select',
							required: true,
							label: 'Priority',
						},
						lifeCycleStageId: {
							apiField: 'LIFECYCLESTAGE',
							type: 'select',
							required: false,
							label: 'Life Cycle Stage',
						},
					};

					// 遍历表单数据，构建字段值数组
					Object.keys(fieldMapping).forEach((formField) => {
						const value = (formData as any)[formField];
						if (value !== undefined && value !== '') {
							const mapping = fieldMapping[formField];
							fieldValues.push({
								fieldName: mapping.apiField,
								fieldValueJson: String(value),
								fieldType: mapping.type,
								isRequired: mapping.required,
								fieldLabel: mapping.label,
							});
						}
					});

					// 如果有字段值需要更新，则调用批量更新 API
					if (fieldValues.length > 0) {
						await batchUpdateStaticFieldValues(onboardingId, fieldValues);
					}
				} catch (staticFieldError) {
					console.error('Failed to update static fields:', staticFieldError);
					// 静态字段更新失败不影响主流程，只记录错误
				}
			}

			const wasEditMode = isEditMode.value;

			ElMessage.success(
				isEditMode.value ? 'Case updated successfully' : t('sys.api.operationSuccess')
			);
			dialogVisible.value = false;
			isEditMode.value = false;
			editingCaseId.value = null;
			resetForm();

			// 编辑模式：刷新列表
			if (wasEditMode) {
				await loadOnboardingList();
			} else {
				// 创建模式：获取返回的 onboarding ID 并跳转到详情页面
				if (onboardingId) {
					router.push(`/onboard/onboardDetail?onboardingId=${onboardingId}`);
				} else {
					// 如果没有返回 ID，则重新加载列表数据
					await loadOnboardingList();
				}
			}
		}
	} finally {
		saving.value = false;
	}
};

const fetchAllStages = async () => {
	const response = await getAllStages();
	if (response.code === '200') {
		onboardingStages.value = response.data || [];
	}
};

const allWorkflows = ref<any[]>([]);
const fetchAllWorkflows = async () => {
	const response = await getWorkflowList();
	if (response.code === '200') {
		allWorkflows.value = response.data || [];
		const defaultWorkflow = allWorkflows.value.find((item) => item.isDefault);
		if (defaultWorkflow) {
			defaultWorkflow.name = '⭐ ' + defaultWorkflow.name;
		}
	}
};

const lifeCycleStage = ref<any[]>([]);
const getLifeCycleStage = async () => {
	lifeCycleStage.value = [
		{
			id: '1',
			name: 'Lead',
		},
		{
			id: '2',
			name: 'Qualified',
		},
		{
			id: '3',
			name: 'Proposal',
		},
		{
			id: '4',
			name: 'Negotiation',
		},
		{
			id: '5',
			name: 'Closed Won',
		},
	];
};

const tabWorkflowId = ref('');

// 检查是否有权限（功能权限 && 数据权限）
const hasCasePermission = (caseId: string, functionalPermission: string) => {
	// 从列表数据中查找对应的 case
	const caseItem = onboardingList.value.find((item) => item.id === caseId);
	if (caseItem && caseItem.permission) {
		return functionPermission(functionalPermission) && caseItem.permission.canOperate;
	}

	// 使用列表数据中的 permission.canOperate 字段
	return functionPermission(functionalPermission);
};

// 监听工作流切换，选中所有阶段
watch(tabWorkflowId, () => {
	nextTick(() => {
		selectedStages.value = [...getAllAvailableStages.value];
	});
});

// 监听可用阶段变化，自动选中所有阶段
watch(
	getAllAvailableStages,
	(newStages) => {
		if (newStages.length > 0) {
			selectedStages.value = [...newStages];
		}
	},
	{ immediate: true }
);

// 初始化
onMounted(async () => {
	// 加载初始数据
	await Promise.all([
		loadOnboardingList(),
		fetchAllStages(),
		getLifeCycleStage(),
		fetchAllWorkflows(),
	]);
	if (route.query?.newOnboarding && functionPermission(ProjectPermissionEnum.case.create)) {
		handleNewOnboarding();
	}
	// 默认选中所有阶段
	selectedStages.value = [...getAllAvailableStages.value];
});
</script>

<style scoped lang="scss">
/* 弹窗样式 */
.dialog-header {
	border-bottom: none;
}

.dialog-title {
	font-size: 18px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.dialog-subtitle {
	color: var(--el-text-color-regular);
	font-size: 13px;
	margin: 0;
	font-weight: normal;
	line-height: 1.4;
}

.onboarding-form {
	padding: 0 4px;
}

.dialog-footer {
	display: flex;
	justify-content: flex-end;
	gap: 12px;
	padding: 16px 0 0 0;
}

/* 管道视图样式 */
.stage-card {
	border: 1px solid var(--el-border-color-light);
	overflow: hidden;
}

.stage-card :deep(.el-card__header) {
	background-color: var(--primary-10);
	border-bottom: 1px solid var(--el-border-color-light);
	padding: 12px 20px;
}

.stage-card :deep(.el-card__body) {
	padding: 0;
}

.stage-content {
	padding: 16px;
}

/* 箭头样式 */
.el-popper.is-light .el-popper__arrow::before {
	border-color: rgba(0, 0, 0, 0.15) !important;
}

.el-popper.is-light .el-popper__arrow::after {
	border-color: var(--el-color-white) !important;
}

/* 旋转动画 */
.rotate-90 {
	transform: rotate(90deg);
}

/* 响应式调整 */
@media (max-width: 768px) {
	.onboarding-list-container {
		padding: 16px;
	}

	.pipeline-lead-button {
		width: 100%;
		margin: 4px 0;
	}
}

:deep(.el-card__footer) {
	@apply pt-0;
}

/* 暗色主题样式 - 参考项目设计规范 */
html.dark {
	/* 弹窗暗色主题 */
	.dialog-title {
		color: var(--white-100);
	}

	.dialog-subtitle {
		color: var(--gray-300);
	}

	/* 管道视图暗色主题 */
	.stage-card {
		border: 1px solid var(--el-border-color) !important;
		background-color: var(--black-400) !important;
	}

	.stage-content {
		background-color: var(--black-400) !important;
	}
}

/* Access Control 样式 */
.access-control-section {
	margin-top: 24px;
	padding: 20px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 8px;
	background-color: var(--el-fill-color-blank);
}

.section-header {
	margin-bottom: 16px;
}

.section-header label {
	display: block;
	margin-bottom: 4px;
}
</style>
