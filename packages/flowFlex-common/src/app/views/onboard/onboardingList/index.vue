<template>
	<div class="bg-gray-50">
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
				<div class="customer-block !p-0 !ml-0">
					<el-table
						:data="onboardingList"
						@selection-change="handleSelectionChange"
						@sort-change="handleSortChange"
						class="w-full rounded-none"
						v-loading="loading"
						:max-height="tableMaxHeight"
						header-cell-class-name="bg-blue-50"
						row-key="id"
					>
						<template #empty>
							<slot name="empty">
								<el-empty description="No Data" :image-size="50" />
							</slot>
						</template>
						<el-table-column type="selection" fixed="left" width="50" align />
						<el-table-column label="Actions" fixed="left" width="80">
							<template #default="{ row }">
								<el-dropdown trigger="click">
									<el-button
										size="small"
										class="p-1 text-gray-600 hover:text-blue-600"
										link
										:icon="ArrowDownBold"
									/>

									<template #dropdown>
										<el-dropdown-menu>
											<!-- Start Onboarding - 只对Inactive状态显示 -->
											<el-dropdown-item
												v-if="row.status === 'Inactive'"
												@click="handleStartOnboarding(row)"
											>
												<el-icon><VideoPlay /></el-icon>
												Start Onboarding
											</el-dropdown-item>

											<!-- Proceed - 对进行中状态显示 -->
											<el-dropdown-item
												v-if="isInProgressStatus(row.status)"
												@click="handleEdit(row.id)"
											>
												<el-icon><Edit /></el-icon>
												Proceed
											</el-dropdown-item>

											<!-- View - 对Completed状态显示 -->
											<el-dropdown-item
												v-if="row.status === 'Completed'"
												@click="handleEdit(row.id)"
											>
												<el-icon><View /></el-icon>
												View
											</el-dropdown-item>

											<!-- Pause - 对进行中状态显示 -->
											<el-dropdown-item
												v-if="isInProgressStatus(row.status)"
												@click="handlePause(row)"
											>
												<el-icon><VideoPause /></el-icon>
												Pause
											</el-dropdown-item>

											<!-- Resume - 对Paused状态显示 -->
											<el-dropdown-item
												v-if="row.status === 'Paused'"
												@click="handleResume(row)"
											>
												<el-icon><VideoPlay /></el-icon>
												Resume
											</el-dropdown-item>

											<!-- Abort - 对进行中状态和暂停状态显示 -->
											<el-dropdown-item
												v-if="
													isInProgressStatus(row.status) ||
													row.status === 'Paused'
												"
												@click="handleAbort(row)"
												class="text-red-500"
											>
												<el-icon><Close /></el-icon>
												Abort
											</el-dropdown-item>

											<!-- Reactivate - 只对已中止状态显示 -->
											<el-dropdown-item
												v-if="isAbortedStatus(row.status)"
												@click="handleReactivate(row)"
											>
												<el-icon><RefreshRight /></el-icon>
												Reactivate
											</el-dropdown-item>

											<!-- Delete - 对所有状态显示，但有不同的限制 -->
											<el-dropdown-item
												@click="handleDelete(row.id)"
												class="text-red-500"
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
							prop="leadName"
							label="Customer Name"
							sortable="custom"
							min-width="220"
							fixed="left"
						>
							<template #default="{ row }">
								<el-link
									type="primary"
									:underline="false"
									@click="handleEdit(row.id)"
									class="table-cell-link"
								>
									<div class="table-cell-content" :title="row.leadName">
										{{ row.leadName }}
									</div>
								</el-link>
							</template>
						</el-table-column>
						<el-table-column
							prop="leadId"
							label="Lead ID"
							sortable="custom"
							width="100"
						>
							<template #default="{ row }">
								<div class="table-cell-content" :title="row.leadId">
									{{ row.leadId }}
								</div>
							</template>
						</el-table-column>
						<el-table-column
							prop="contactPerson"
							sortable="custom"
							label="Contact Name"
							width="220"
						>
							<template #default="{ row }">
								<div class="table-cell-content" :title="row.contactPerson">
									{{ row.contactPerson }}
								</div>
							</template>
						</el-table-column>
						<el-table-column
							prop="lifeCycleStageName"
							label="Life Cycle Stage"
							sortable="custom"
							width="170"
						>
							<template #default="{ row }">
								<div class="table-cell-content" :title="row.lifeCycleStageName">
									{{ row.lifeCycleStageName }}
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
								<div
									:class="row.workflowName ? `workflow-name-tag` : ''"
									:title="row.workflowName"
								>
									{{ row.workflowName }}
								</div>
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
								<el-tag :type="getPriorityTagType(row.priority)" class="text-white">
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
						<el-table-column label="Start Date" width="150" sortable="custom">
							<template #default="{ row }">
								<div class="text-xs space-y-1">
									<div class="flex items-center">
										<span
											class="table-cell-content flex-1"
											:title="
												timeZoneConvert(
													row.currentStageStartTime,
													false,
													projectTenMinutesSsecondsDate
												)
											"
										>
											{{
												timeZoneConvert(
													row.currentStageStartTime,
													false,
													projectTenMinutesSsecondsDate
												)
											}}
										</span>
									</div>
								</div>
							</template>
						</el-table-column>
						<el-table-column label="End Date" width="150" sortable="custom">
							<template #default="{ row }">
								<div class="text-xs space-y-1">
									<div class="flex items-center">
										<span
											class="table-cell-content flex-1"
											:title="
												timeZoneConvert(
													row.currentStageEndTime,
													false,
													projectTenMinutesSsecondsDate
												)
											"
										>
											{{
												timeZoneConvert(
													row.currentStageEndTime,
													false,
													projectTenMinutesSsecondsDate
												)
											}}
										</span>
									</div>
									<div
										v-if="isOverdue(row.currentStageEndTime)"
										class="flex items-center text-red-500"
									>
										<el-icon class="mr-1"><Warning /></el-icon>
										<span>Overdue</span>
									</div>
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
					<div class="border-t bg-white rounded-b-md">
						<CustomerPagination
							:total="totalElements"
							:limit="pageSize"
							:page="currentPage"
							:background="true"
							@pagination="handleLimitUpdate"
							@update:page="handleCurrentChange"
							@update:limit="handlePageUpdate"
						/>
					</div>
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
						<div class="text-sm font-medium text-gray-700">Filter Stages:</div>
						<div class="text-sm text-gray-500">Total Records: {{ totalElements }}</div>
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
					:get-priority-border-class="getPriorityBorderClass"
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
			:width="dialogWidth + 'px'"
			destroy-on-close
			custom-class="onboarding-dialog"
			:show-close="true"
			:close-on-click-modal="false"
			draggable
		>
			<template #header>
				<div class="dialog-header">
					<h2 class="dialog-title">Create New Cases</h2>
					<p class="dialog-subtitle">
						Create a new onboarding record for lead management.
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
				<el-form-item label="Customer Name" prop="leadName">
					<el-input
						v-model="formData.leadName"
						placeholder="Input Customer Name"
						clearable
						class="w-full rounded-xl"
					/>
				</el-form-item>

				<el-form-item label="Lead ID" prop="leadId">
					<el-input
						v-model="formData.leadId"
						placeholder="Enter Lead ID"
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
						/>
					</el-select>
				</el-form-item>
			</el-form>

			<template #footer>
				<div class="dialog-footer">
					<el-button @click="handleCancel" :disabled="saving">Cancel</el-button>
					<el-button type="primary" @click="handleSave" :loading="saving">Save</el-button>
				</div>
			</template>
		</el-dialog>

		<!-- 删除确认对话框 -->
		<el-dialog v-model="deleteDialogVisible" title="Confirm Deletion" width="400px">
			<p class="text-gray-600">
				Are you sure you want to delete this lead? This action cannot be undone.
			</p>
			<template #footer>
				<div class="flex justify-end space-x-2">
					<el-button @click="deleteDialogVisible = false">Cancel</el-button>
					<el-button type="danger" @click="confirmDelete">Delete</el-button>
				</div>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, markRaw, watch, nextTick } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import '../styles/errorDialog.css';
import {
	ArrowDownBold,
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
	queryOnboardings,
	deleteOnboarding,
	createOnboarding,
	exportOnboarding,
	batchUpdateStaticFieldValues,
	startOnboarding,
	abortOnboarding,
	reactivateOnboarding,
	pauseOnboarding,
	resumeOnboardingWithConfirmation,
} from '@/apis/ow/onboarding';
import { getAllStages, getWorkflowList } from '@/apis/ow';
import { OnboardingItem, SearchParams, OnboardingQueryRequest, ApiResponse } from '#/onboard';
import { PrototypeTabs, TabPane, TabButtonGroup } from '@/components/PrototypeTabs';
import {
	defaultStr,
	projectTenMinutesSsecondsDate,
	tableMaxHeight,
	dialogWidth,
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

const { t } = useI18n();

// 入职阶段定义
const onboardingStages = ref<any[]>([]);

// 响应式数据
const router = useRouter();
const loading = ref(false);
const onboardingList = ref<OnboardingItem[]>([]);

// 监听onboardingList的变化（用于调试）
watch(
	onboardingList,
	(newList) => {
		// 检查是否有status字段丢失
		const itemsWithoutStatus = newList.filter((item) => item.id && !item.status);
		if (itemsWithoutStatus.length > 0) {
			console.error('❌ [Watch] Found items without status:', itemsWithoutStatus);
		}
	},
	{ deep: true }
);
const selectedItems = ref<OnboardingItem[]>([]);
const activeView = ref('table');
const currentPage = ref(1);
const pageSize = ref(15);
const totalElements = ref(0);
const deleteDialogVisible = ref(false);
const itemToDelete = ref<string | null>(null);

const handleViewChange = (value: string) => {
	activeView.value = value;
	loadOnboardingList(null, false);
};

// 新建入职弹窗相关状态
const dialogVisible = ref(false);
const formRef = ref();
const formData = reactive({
	leadId: '',
	leadName: '',
	lifeCycleStageName: '',
	lifeCycleStageId: '',
	priority: '',
	ContactPerson: '',
	ContactEmail: '',
	workFlowId: '',
});
const formRules = {
	leadId: [{ required: true, message: 'Lead ID is required', trigger: 'blur' }],
	priority: [{ required: true, message: 'Priority is required', trigger: 'change' }],
	leadName: [{ required: true, message: 'Customer Name is required', trigger: 'blur' }],
	ContactPerson: [{ required: false, message: 'Contact Name is required', trigger: 'blur' }], // 必填
	ContactEmail: [
		{ required: true, message: 'Contact Email is required', trigger: 'blur' },
		{ type: 'email', message: 'Please enter a valid email address', trigger: 'blur' },
	], // 必填，且需要验证邮箱格式
	workFlowId: [{ required: true, message: 'Workflow is required', trigger: 'blur' }],
};

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
	leadId: '',
	leadName: '',
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
			sort: event?.prop ? event.prop : '',
			sortType: event?.isAsc ? 'asc' : 'desc',
			...omitBy(
				pick(searchParams, [
					'leadId',
					'leadName',
					'lifeCycleStageName',
					'currentStageId',
					'updatedBy',
					'priority',
					'workFlowId',
				]),
				(value) => isNil(value) || value === ''
			),
		};

		console.log('🔍 [Onboarding List] Query params:', queryParams);
		const res: ApiResponse<OnboardingItem> = await queryOnboardings(queryParams);
		console.log('📊 [Onboarding List] API response:', res);

		if (res.code === '200') {
			// 确保每个项目都有status字段
			const processedData = (res.data.data || []).map((item) => ({
				...item,
				status: item.status || 'Unknown', // 确保status字段存在
			}));

			onboardingList.value = processedData;
			totalElements.value = res.data.total || 0;

			console.log('📋 [Onboarding List] Loaded items:', onboardingList.value.length);
		} else {
			console.warn('⚠️ [Onboarding List] API returned error:', res.code, res.msg);
			onboardingList.value = [];
			totalElements.value = 0;
		}
	} catch (error) {
		console.error('❌ [Onboarding List] API call failed:', error);
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
					'leadId',
					'leadName',
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
		case 'Paused':
			return 'warning';
		case 'Aborted':
		case 'Cancelled':
			return 'danger';
		default:
			console.warn('⚠️ [Status Tag] Unknown status:', status);
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

const getPriorityBorderClass = (priority: string) => {
	switch (priority.toLowerCase()) {
		case 'high':
			return 'border-red-500';
		case 'medium':
			return 'border-yellow-500';
		case 'low':
			return 'border-green-500';
		default:
			return 'border-gray-500';
	}
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

const handleSortChange = (event) => {
	// 这里可以添加排序逻辑，发送到后端
	// 暂时使用前端排序
	loadOnboardingList(event);
	return false;
};

const handleEdit = (itemId: string) => {
	router.push(`/onboard/onboardDetail?onboardingId=${itemId}`);
};

const handleNewOnboarding = () => {
	if (allWorkflows.value.length > 0) {
		formData.workFlowId = allWorkflows.value.find((item) => item.isDefault)?.id || '';
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

const handleDelete = (itemId: string) => {
	itemToDelete.value = itemId;
	deleteDialogVisible.value = true;
};

// ========================= 状态管理函数 =========================

const handleStartOnboarding = async (row: OnboardingItem) => {
	console.log('🚀 [Start Onboarding] Clicked for row:', row);
	console.log('🚀 [Start Onboarding] Row status:', row.status);
	console.log('🚀 [Start Onboarding] Row ID:', row.id);

	ElMessageBox.confirm(
		`Are you sure you want to start the onboarding process for "${row.leadName}"? This will activate the onboarding and begin the workflow.`,
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
						console.log('📡 [Start Onboarding] Calling API with params:', {
							id: row.id,
							params: {
								reason: 'Manual activation from onboarding list',
								resetProgress: true,
							},
						});

						const res = await startOnboarding(row.id, {
							reason: 'Manual activation from onboarding list',
							resetProgress: true,
						});

						console.log('📡 [Start Onboarding] API response:', res);

						if (res.code === '200') {
							ElMessage.success('Onboarding started successfully');
							await loadOnboardingList();
						} else {
							ElMessage.error(res.msg || 'Failed to start onboarding');
						}
					} catch (error) {
						console.error('❌ [Start Onboarding] Error:', error);
						ElMessage.error('Failed to start onboarding');
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
		`Are you sure you want to pause the onboarding process for "${row.leadName}"? The account will stay at the current stage and lose ETA. All workflow content will become read-only.`,
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
		`Are you sure you want to resume the onboarding process for "${row.leadName}"? The account will restore ETA and current stage timing will continue from where it was paused.`,
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
		`Are you sure you want to abort the onboarding process for "${row.leadName}"? This will terminate the process and the account will exit the onboarding workflow. Please provide a reason for this action.`,
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
		`Are you sure you want to reactivate the onboarding process for "${row.leadName}"? This will restart the process from stage 1 while preserving questionnaire answers. Please provide a reason for this action.`,
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

const confirmDelete = async () => {
	if (itemToDelete.value) {
		try {
			loading.value = true;
			const res = await deleteOnboarding(itemToDelete.value, true);

			if (res.code == '200') {
				// 重新加载数据
				deleteDialogVisible.value = false;
				ElMessage.success(t('sys.api.operationSuccess'));
				await loadOnboardingList();
			}
		} finally {
			loading.value = false;
		}
	}
	itemToDelete.value = null;
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
						'leadId',
						'leadName',
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
const handleCancel = () => {
	dialogVisible.value = false;
	resetForm();
};

const resetForm = () => {
	formData.leadId = '';
	formData.leadName = '';
	formData.lifeCycleStageName = '';
	formData.lifeCycleStageId = '';
	formData.priority = '';
	formData.ContactPerson = '';
	formData.ContactEmail = '';
	if (formRef.value) {
		formRef.value.clearValidate();
	}
};

const handleSave = async () => {
	if (!formRef.value) return;

	try {
		// 表单验证
		await formRef.value.validate();

		saving.value = true;

		// 调用创建入职的API接口
		const res = await createOnboarding(formData);

		if (res.code === '200') {
			const onboardingId = res.data;

			// 如果创建成功且有 onboardingId，则更新静态字段值
			if (onboardingId) {
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
						leadId: {
							apiField: 'LEADID',
							type: 'text',
							required: true,
							label: 'Lead ID',
						},
						leadName: {
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

			ElMessage.success(t('sys.api.operationSuccess'));
			dialogVisible.value = false;
			resetForm();

			// 获取返回的 onboarding ID 并跳转到详情页面
			if (onboardingId) {
				router.push(`/onboard/onboardDetail?onboardingId=${onboardingId}`);
			} else {
				// 如果没有返回 ID，则重新加载列表数据
				await loadOnboardingList();
			}
		}
	} catch (error) {
		console.log('error:', error);
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
		allWorkflows.value = response.data.filter((item) => item.isActive) || [];
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

.onboarding-form {
	padding: 0 4px;
}

.dialog-footer {
	display: flex;
	justify-content: flex-end;
	gap: 12px;
	padding: 16px 0 0 0;
}

:deep(.onboarding-dialog .el-dialog__header) {
	padding: 0;
	margin-right: 0;
	border-bottom: none;
}

:deep(.onboarding-dialog .el-dialog__headerbtn) {
	top: 16px;
	right: 16px;
	z-index: 10;
}

:deep(.onboarding-dialog .el-dialog__body) {
	padding: 24px;
}

:deep(.onboarding-dialog .el-dialog__footer) {
	padding: 0 24px 24px 24px;
	border-top: 1px solid #f0f0f0;
	margin-top: 16px;
}

/* 优先级标签样式 */
:deep(.el-tag.el-tag--danger) {
	background-color: #dc2626;
	border-color: #dc2626;
	color: white;
}

:deep(.el-tag.el-tag--warning) {
	background-color: #d97706;
	border-color: #d97706;
	color: white;
}

:deep(.el-tag.el-tag--success) {
	background-color: #059669;
	border-color: #059669;
	color: white;
}

/* 管道视图样式 */
.stage-card {
	border: 1px solid #e5e7eb;
	overflow: hidden;
}

.stage-card :deep(.el-card__header) {
	background-color: var(--primary-10);
	border-bottom: 1px solid #e5e7eb;
	padding: 12px 20px;
}

.stage-card :deep(.el-card__body) {
	padding: 0;
}

.stage-content {
	padding: 16px;
}

:deep(.pipeline-lead-button) {
	border: 1px solid #d1d5db;
	border-left-width: 4px;
	background: white;
	color: #374151;
	font-size: 14px;
	padding: 8px 12px;
	margin: 2px;
	transition: all 0.2s;
	height: 32px;
	text-align: left;
	justify-content: flex-start;
	font-weight: normal;
}

:deep(.pipeline-lead-button:hover) {
	background-color: #f9fafb;
}

:deep(.pipeline-lead-button.border-red-500) {
	border-color: #dc2626 !important;
	border-left-color: #dc2626 !important;
}

:deep(.pipeline-lead-button.border-yellow-500) {
	border-color: #d97706 !important;
	border-left-color: #d97706 !important;
}

:deep(.pipeline-lead-button.border-green-500) {
	border-color: #059669 !important;
	border-left-color: #059669 !important;
}

:deep(.pipeline-lead-button.border-gray-500) {
	border-color: #6b7280 !important;
	border-left-color: #6b7280 !important;
}

/* 逾期时的红色边框覆盖优先级颜色 */
:deep(
		.pipeline-lead-button.border-red-500:not(.border-yellow-500):not(.border-green-500):not(
				.border-gray-500
			)
	) {
	border-color: #dc2626 !important;
	border-left-color: #dc2626 !important;
}

/* 工具提示样式 */
:deep(.el-tooltip__popper) {
	background-color: #1f2937;
	border: none;
	box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
}

/* Light 效果的 tooltip 增强阴影效果 */
:deep(.el-popper.is-light) {
	box-shadow:
		0 25px 50px -12px rgba(0, 0, 0, 0.25),
		0 12px 24px -6px rgba(0, 0, 0, 0.15),
		0 6px 12px -3px rgba(0, 0, 0, 0.1) !important;
	border: 1px solid rgba(0, 0, 0, 0.15) !important;
}

/* 全局 tooltip 阴影增强 - 使用更高优先级 */
.el-popper.is-light {
	box-shadow:
		0 25px 50px -12px rgba(0, 0, 0, 0.25),
		0 12px 24px -6px rgba(0, 0, 0, 0.15),
		0 6px 12px -3px rgba(0, 0, 0, 0.1) !important;
	border: 1px solid rgba(0, 0, 0, 0.15) !important;
}

/* 箭头样式 */
.el-popper.is-light .el-popper__arrow::before {
	border-color: rgba(0, 0, 0, 0.15) !important;
}

.el-popper.is-light .el-popper__arrow::after {
	border-color: #ffffff !important;
}

/* 旋转动画 */
.rotate-90 {
	transform: rotate(90deg);
}

/* 表格单元格内容样式 */
.table-cell-content {
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
	max-width: 100%;
	display: block;
}

/* Ensure Element Plus link wrapper doesn't break ellipsis */
.table-cell-link {
	display: block;
	width: 100%;
}
:deep(.table-cell-link .el-link__inner) {
	display: block;
	width: 100%;
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
	/* 页面背景 - 使用项目标准的 black-400 */
	.bg-gray-50 {
		@apply bg-black-400 !important;
	}

	/* 弹窗暗色主题 */
	.dialog-title {
		color: var(--white-100);
	}

	.dialog-subtitle {
		color: var(--gray-300);
	}

	:deep(.onboarding-dialog .el-dialog__wrapper) {
		background-color: rgba(0, 0, 0, 0.7);
	}

	:deep(.onboarding-dialog .el-dialog) {
		background-color: var(--black-400);
		border: 1px solid var(--black-200);
	}

	:deep(.onboarding-dialog .el-dialog__header) {
		background-color: var(--black-400);
	}

	:deep(.onboarding-dialog .el-dialog__body) {
		background-color: var(--black-400);
	}

	:deep(.onboarding-dialog .el-dialog__footer) {
		background-color: var(--black-400);
		border-top: 1px solid var(--black-200);
	}

	:deep(.onboarding-form .el-form-item__label) {
		color: var(--white-100);
	}

	:deep(.onboarding-form .el-input__wrapper) {
		background-color: var(--black-200);
		border-color: var(--black-200);
	}

	:deep(.onboarding-form .el-input__inner) {
		color: var(--white-100);
	}

	:deep(.onboarding-form .el-select .el-input__wrapper) {
		background-color: var(--black-200);
		border-color: var(--black-200);
	}

	/* 管道视图暗色主题 */
	.stage-card {
		border: 1px solid #4a5568 !important; /* 使用更明显的灰色边框 */
		background-color: var(--black-400) !important;
	}

	.stage-card :deep(.el-card__header) {
		background-color: #003c76 !important; /* 使用项目的 sea-700 深蓝色 */
		border-bottom: 1px solid #00509d !important; /* 使用项目的 sea-600 */
		color: #cce8d0 !important; /* 使用项目的浅色文字 */
	}

	.stage-card :deep(.el-card__body) {
		background-color: var(--black-400) !important;
	}

	.stage-content {
		background-color: var(--black-400) !important;
	}

	/* 客户按钮暗色主题 */
	:deep(.pipeline-lead-button) {
		background-color: var(--black-200) !important;
		border: 1px solid var(--black-200) !important;
		border-left-width: 4px !important;
		color: var(--white-100) !important;
	}

	:deep(.pipeline-lead-button:hover) {
		background-color: var(--black-300) !important;
	}

	/* 卡片底部统计区域暗色主题 */
	.stage-card :deep(.el-card__footer) {
		background-color: var(--black-200) !important;
		border-top: 1px solid #4a5568 !important; /* 使用更明显的灰色边框 */
		color: #9ca3af !important;
	}

	/* 文本颜色调整 - 使用项目标准的白色文本 */
	.text-gray-700,
	.text-gray-600,
	.text-gray-900 {
		@apply text-white-100 !important;
	}

	.text-gray-500 {
		@apply text-gray-300 !important;
	}

	/* 边框颜色调整 */
	.border-gray-200,
	.border-gray-300 {
		@apply border-black-200 !important;
	}

	/* 标签暗色主题 */
	:deep(.el-tag) {
		@apply bg-black-200 border-black-200 text-white-100;
	}

	:deep(.el-tag.el-tag--info) {
		@apply bg-black-200 border-black-200 text-gray-300;
	}

	:deep(.el-tag.el-tag--primary) {
		background-color: var(--primary-500);
		border-color: var(--primary-500);
		color: white;
	}

	/* 工具提示在暗色主题下的增强 */
	:deep(.el-popper.is-light) {
		@apply bg-black-400 text-white-100 !important;
		border: 1px solid var(--black-200) !important;
		box-shadow:
			0 25px 50px -12px rgba(0, 0, 0, 0.6),
			0 12px 24px -6px rgba(0, 0, 0, 0.4),
			0 6px 12px -3px rgba(0, 0, 0, 0.25) !important;
	}

	.el-popper.is-light {
		@apply bg-black-400 text-white-100 !important;
		border: 1px solid var(--black-200) !important;
		box-shadow:
			0 25px 50px -12px rgba(0, 0, 0, 0.6),
			0 12px 24px -6px rgba(0, 0, 0, 0.4),
			0 6px 12px -3px rgba(0, 0, 0, 0.25) !important;
	}

	.el-popper.is-light .el-popper__arrow::after {
		border-color: var(--black-400) !important;
	}

	/* 工具提示内容文本颜色 */
	:deep(.el-tooltip__content) {
		@apply text-white-100 !important;
	}

	/* 空状态区域 */
	.bg-gray-50 {
		@apply bg-black-200 !important;
	}
}

/* 工作流名称标签样式 */
.workflow-name-tag {
	@apply block px-3 py-1 text-sm text-center font-medium text-primary-600 bg-primary-50 rounded-full truncate;
	border: 1px solid #dbeafe;
	transition: all 0.3s ease;
}

.workflow-name-tag:hover {
	@apply bg-blue-100 text-blue-700;
}
</style>
