<template>
	<div class="bg-gray-50">
		<!-- 标题和操作区 -->
		<div class="onboarding-header">
			<h1 class="title">Onboarding List</h1>
			<div class="actions">
				<el-button
					class="new-onboarding-btn"
					type="primary"
					@click="handleNewOnboarding"
					:disabled="loading"
				>
					<el-icon v-if="loading"><Loading /></el-icon>
					<el-icon v-else><Plus /></el-icon>
					<span>New Onboarding</span>
				</el-button>
			</div>
		</div>

		<!-- 搜索区域 -->
		<el-card class="mb-6 rounded-md">
			<template #default>
				<div class="pt-6">
					<el-form
						ref="searchFormRef"
						:model="searchParams"
						@submit.prevent="handleSearch"
						class="onboardSearch-form"
					>
						<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mb-4">
							<div class="space-y-2">
								<label class="text-sm font-medium text-gray-700">Lead ID</label>
								<el-input
									v-model="searchParams.leadId"
									placeholder="Search or enter Lead ID"
									clearable
									class="w-full rounded-md"
								/>
							</div>

							<div class="space-y-2">
								<label class="text-sm font-medium text-gray-700">
									Company/Contact Name
								</label>
								<el-input
									v-model="searchParams.leadName"
									placeholder="Enter Company or Contact Name"
									clearable
									class="w-full rounded-md"
								/>
							</div>

							<div class="space-y-2">
								<label class="text-sm font-medium text-gray-700">
									Life Cycle Stage
								</label>
								<el-select
									v-model="searchParams.lifeCycleStageName"
									placeholder="Select Stage"
									clearable
									class="w-full rounded-md"
								>
									<el-option label="All Stages" value="" />
									<el-option
										v-for="stage in lifeCycleStage"
										:key="stage.name"
										:label="stage.name"
										:value="stage.name"
									/>
								</el-select>
							</div>

							<div class="space-y-2">
								<label class="text-sm font-medium text-gray-700">
									Onboard Work Flow
								</label>
								<el-select
									v-model="searchParams.workFlowId"
									placeholder="Select Work Flow"
									clearable
									class="w-full rounded-md"
								>
									<el-option label="All Work Flows" value="" />
									<el-option
										v-for="workflow in allWorkflows"
										:key="workflow.id"
										:label="workflow.name"
										:value="workflow.id"
									/>
								</el-select>
							</div>

							<div class="space-y-2">
								<label class="text-sm font-medium text-gray-700">
									Onboard Stage
								</label>
								<el-select
									v-model="searchParams.currentStageId"
									placeholder="Select Stage"
									clearable
									class="w-full rounded-md"
								>
									<el-option label="All Stages" value="" />
									<el-option
										v-for="stage in onboardingStages"
										:key="stage.id"
										:label="stage.name"
										:value="stage.id"
									/>
								</el-select>
							</div>

							<div class="space-y-2">
								<label class="text-sm font-medium text-gray-700">Updated By</label>
								<el-input
									v-model="searchParams.updatedBy"
									placeholder="Enter User Name"
									clearable
									class="w-full rounded-md"
								/>
							</div>

							<div class="space-y-2">
								<label class="text-sm font-medium text-gray-700">Priority</label>
								<el-select
									v-model="searchParams.priority"
									placeholder="Select Priority"
									clearable
									class="w-full rounded-md"
								>
									<el-option label="All Priorities" value="" />
									<el-option label="High" value="High" />
									<el-option label="Medium" value="Medium" />
									<el-option label="Low" value="Low" />
								</el-select>
							</div>
						</div>

						<div class="flex justify-end space-x-2">
							<el-button @click="handleReset">
								<el-icon><Close /></el-icon>
								Reset
							</el-button>
							<el-button type="primary" @click="handleSearch">
								<el-icon><Search /></el-icon>
								Search
							</el-button>
							<el-button @click="handleExport" :loading="loading" :disabled="loading">
								<el-icon><Download /></el-icon>
								Export
								{{ selectedItems.length > 0 ? `(${selectedItems.length})` : 'All' }}
							</el-button>
						</div>
					</el-form>
				</div>
			</template>
		</el-card>

		<PrototypeTabs
			v-model="activeView"
			:tabs="tabsConfig"
			type="adaptive"
			size="default"
			class="mb-6"
		>
			<!-- 表格视图 -->
			<TabPane value="table">
				<div class="rounded-md border bg-white shadow-sm overflow-hidden">
					<el-table
						:data="currentPageData"
						@selection-change="handleSelectionChange"
						@sort-change="handleSortChange"
						class="w-full rounded-none"
						v-loading="loading"
						:max-height="tableMaxHeight"
						header-cell-class-name="bg-blue-50"
					>
						<template #empty>
							<slot name="empty">
								<el-empty description="No Data" :image-size="50" />
							</slot>
						</template>
						<el-table-column type="selection" width="50" />
						<el-table-column label="Actions" width="80">
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
											<el-dropdown-item @click="handleEdit(row.id)">
												<el-icon><Edit /></el-icon>
												Edit
											</el-dropdown-item>
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
							prop="leadName"
							label="Company/Contact Name"
							sortable="custom"
							min-width="200"
						>
							<template #default="{ row }">
								<div class="table-cell-content" :title="row.leadName">
									{{ row.leadName }}
								</div>
							</template>
						</el-table-column>
						<el-table-column
							prop="lifeCycleStageName"
							label="Life Cycle Stage"
							sortable="custom"
							width="150"
						>
							<template #default="{ row }">
								<div class="table-cell-content" :title="row.lifeCycleStageName">
									{{ row.lifeCycleStageName }}
								</div>
							</template>
						</el-table-column>
						<el-table-column
							prop="workflowName"
							label="Work Flow"
							sortable="custom"
							min-width="200"
						>
							<template #default="{ row }">
								<div class="table-cell-content" :title="row.workflowName">
									{{ row.workflowName }}
								</div>
							</template>
						</el-table-column>
						<el-table-column
							prop="currentStageName"
							label="Onboard Stage"
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
						<el-table-column label="Timeline" width="180" sortable="custom">
							<template #default="{ row }">
								<div class="text-xs space-y-1">
									<div class="flex items-center">
										<span class="font-medium mr-1">Start:</span>
										<span
											class="table-cell-content flex-1"
											:title="formatDate(row.startDate) || defaultStr"
										>
											{{ formatDate(row.startDate) || defaultStr }}
										</span>
									</div>
									<div class="flex items-center">
										<span class="font-medium mr-1">ETA:</span>
										<span
											class="table-cell-content flex-1"
											:title="
												formatDate(row.estimatedCompletionDate) ||
												defaultStr
											"
										>
											{{
												formatDate(row.estimatedCompletionDate) ||
												defaultStr
											}}
										</span>
									</div>
									<div
										v-if="isOverdue(row.estimatedCompletionDate)"
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
							width="120"
						>
							<template #default="{ row }">
								<div class="table-cell-content" :title="row.stageUpdatedBy">
									{{ row.stageUpdatedBy }}
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
									:title="formatDateTime(row.stageUpdatedTime)"
								>
									{{ formatDateTime(row.stageUpdatedTime) }}
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
				<!-- 阶段过滤器和总记录数 -->
				<div class="mb-4">
					<div class="flex justify-between items-center mb-2">
						<div class="text-sm font-medium text-gray-700">Filter Stages:</div>
						<div class="text-sm text-gray-500">Total Records: {{ totalElements }}</div>
					</div>
					<div class="flex flex-wrap gap-2">
						<el-tag
							v-for="stage in activeStages"
							:key="stage"
							:type="visibleStages.includes(stage) ? 'primary' : 'info'"
							:effect="visibleStages.includes(stage) ? 'dark' : 'plain'"
							class="cursor-pointer"
							@click="toggleStageVisibility(stage)"
						>
							{{ stage }} ({{ groupedLeads[stage]?.length || 0 }})
						</el-tag>
					</div>
				</div>

				<!-- 阶段卡片 -->
				<div class="space-y-4">
					<el-card
						v-for="stage in visibleStages"
						:key="stage"
						class="stage-card shadow-sm rounded-md"
					>
						<template #header>
							<div
								class="flex justify-between items-center cursor-pointer py-1"
								@click="toggleStageExpansion(stage)"
							>
								<div class="flex items-center">
									<el-icon
										class="mr-2 transition-transform text-gray-600"
										:class="{ 'rotate-90': expandedStages.includes(stage) }"
									>
										<ArrowRight />
									</el-icon>
									<span class="text-base font-medium text-gray-900">
										{{ stage }}
									</span>
								</div>
								<el-tag type="info" class="flex items-center">
									<el-icon class="mr-1"><User /></el-icon>
									{{ groupedLeads[stage]?.length || 0 }}
								</el-tag>
							</div>
						</template>

						<div v-if="expandedStages.includes(stage)" class="stage-content p-3">
							<div v-if="groupedLeads[stage]?.length" class="flex flex-wrap gap-2">
								<el-tooltip
									v-for="lead in groupedLeads[stage]"
									:key="lead.id"
									placement="top"
									:show-after="500"
									effect="light"
								>
									<template #content>
										<div class="p-3 max-w-xs">
											<div class="font-medium mb-1 text-gray-900">
												{{ lead.leadName }}
											</div>
											<div class="text-xs text-gray-500 mb-1">
												{{ lead.leadId }}
											</div>
											<div
												class="grid grid-cols-2 gap-2 text-xs mb-2 text-gray-600"
											>
												<div class="flex items-center">
													<el-icon class="mr-1 text-gray-500">
														<Calendar />
													</el-icon>
													<span>
														Start:
														{{
															formatDate(lead.startDate) || defaultStr
														}}
													</span>
												</div>
												<div class="flex items-center">
													<el-icon class="mr-1 text-gray-500">
														<Calendar />
													</el-icon>
													<span>
														ETA:
														{{
															formatDate(
																lead.estimatedCompletionDate
															) || defaultStr
														}}
													</span>
												</div>
											</div>
											<div class="flex items-center justify-between text-xs">
												<el-tag
													:type="getPriorityTagType(lead.priority)"
													size="small"
													class="text-white"
												>
													{{ lead.priority }}
												</el-tag>
												<span
													v-if="isOverdue(lead.estimatedCompletionDate)"
													class="text-red-500 flex items-center"
												>
													<el-icon class="mr-1"><Warning /></el-icon>
													Overdue
												</span>
											</div>
										</div>
									</template>
									<el-button
										size="small"
										class="pipeline-lead-button rounded-md"
										:class="[
											getPriorityBorderClass(lead.priority),
											isOverdue(lead.estimatedCompletionDate)
												? 'border-red-500'
												: '',
										]"
										@click="handleEdit(lead.id)"
									>
										<span class="truncate max-w-[180px]">
											{{ lead.leadName }}
										</span>
										<div class="flex items-center ml-1">
											<el-icon
												v-if="isOverdue(lead.estimatedCompletionDate)"
												class="text-red-500 mr-1"
											>
												<Warning />
											</el-icon>
										</div>
									</el-button>
								</el-tooltip>
							</div>
							<div v-else class="text-center py-4 text-gray-500 text-sm">
								No customers in this stage
							</div>
						</div>

						<template #footer>
							<div class="bg-gray-50 py-2 px-3 border-t -mx-6 -mb-6">
								<div
									class="flex justify-between items-center text-xs text-gray-500"
								>
									<span>High: {{ getStageCountByPriority(stage, 'High') }}</span>
									<span>
										Medium: {{ getStageCountByPriority(stage, 'Medium') }}
									</span>
									<span>Low: {{ getStageCountByPriority(stage, 'Low') }}</span>
									<span>Overdue: {{ getStageOverdueCount(stage) }}</span>
								</div>
							</div>
						</template>
					</el-card>
				</div>

				<div
					v-if="visibleStages.length === 0"
					class="text-center py-10 bg-gray-50 rounded-lg"
				>
					<p class="text-gray-500">
						No stages selected. Please select at least one stage to view.
					</p>
				</div>
			</TabPane>
		</PrototypeTabs>

		<!-- 新建入职弹窗 -->
		<el-dialog
			v-model="dialogVisible"
			title="Create New Onboarding"
			:width="dialogWidth + 'px'"
			destroy-on-close
			custom-class="onboarding-dialog"
			:show-close="true"
			:close-on-click-modal="false"
			draggable
		>
			<template #header>
				<div class="dialog-header">
					<h2 class="dialog-title">Create New Onboarding</h2>
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
				<el-form-item label="Company Name" prop="leadName">
					<el-input
						v-model="formData.leadName"
						placeholder="Input Company Name"
						clearable
						class="w-full rounded-md"
					/>
				</el-form-item>

				<div class="grid grid-cols-1 md:grid-cols-2 gap-4">
					<el-form-item label="Contact Name" prop="ContactPerson">
						<el-input
							v-model="formData.ContactPerson"
							placeholder="Enter Contact Name"
							clearable
							class="w-full rounded-md"
						/>
					</el-form-item>

					<el-form-item label="Contact Email" prop="ContactEmail">
						<el-input
							v-model="formData.ContactEmail"
							placeholder="Enter Contact Email Address"
							clearable
							type="email"
							class="w-full rounded-md"
						/>
					</el-form-item>
				</div>

				<el-form-item label="Lead ID" prop="leadId">
					<el-input
						v-model="formData.leadId"
						placeholder="Enter Lead ID"
						clearable
						class="w-full rounded-md"
					/>
				</el-form-item>

				<el-form-item label="Life Cycle Stage" prop="lifeCycleStageId">
					<el-select
						v-model="formData.lifeCycleStageId"
						placeholder="Select Life Cycle Stage"
						clearable
						class="w-full rounded-md"
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
						class="w-full rounded-md"
					>
						<el-option label="High" value="High" />
						<el-option label="Medium" value="Medium" />
						<el-option label="Low" value="Low" />
					</el-select>
				</el-form-item>

				<el-form-item label="Work flow" prop="workFlowId">
					<el-select
						v-model="formData.workFlowId"
						placeholder="Select Work Flow"
						clearable
						class="w-full rounded-md"
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
import { ref, reactive, computed, onMounted, markRaw } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import '../styles/errorDialog.css';
import {
	Search,
	Close,
	Download,
	ArrowDownBold,
	Edit,
	Delete,
	Warning,
	ArrowRight,
	User,
	Calendar,
	Plus,
	Loading,
} from '@element-plus/icons-vue';
import {
	queryOnboardings,
	deleteOnboarding,
	createOnboarding,
	exportOnboarding,
	batchUpdateStaticFieldValues,
} from '@/apis/ow/onboarding';
import { getAllStages, getWorkflowList } from '@/apis/ow';
import { OnboardingItem, SearchParams, OnboardingQueryRequest, ApiResponse } from '#/onboard';
import { PrototypeTabs, TabPane } from '@/components/PrototypeTabs';
import {
	defaultStr,
	projectTenMinutesSsecondsDate,
	tableMaxHeight,
	dialogWidth,
} from '@/settings/projectSetting';
import CustomerPagination from '@/components/global/u-pagination/index.vue';
import { timeZoneConvert } from '@/hooks/time';
import { useI18n } from '@/hooks/useI18n';
import TableViewIcon from '@assets/svg/onboard/tavleView.svg';
import ProgressViewIcon from '@assets/svg/onboard/progressView.svg';
import { pick, omitBy, isNil } from 'lodash-es';

const { t } = useI18n();

// 入职阶段定义
const onboardingStages = ref<any[]>([]);

// 响应式数据
const router = useRouter();
const searchFormRef = ref();
const loading = ref(false);
const onboardingList = ref<OnboardingItem[]>([]);
const selectedItems = ref<OnboardingItem[]>([]);
const activeView = ref('table');
const currentPage = ref(1);
const pageSize = ref(15);
const totalElements = ref(0);
const deleteDialogVisible = ref(false);
const itemToDelete = ref<string | null>(null);

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
	leadName: [{ required: true, message: 'Company Name is required', trigger: 'blur' }],
	ContactPerson: [{ required: true, message: 'Contact Name is required', trigger: 'blur' }], // 必填
	ContactEmail: [
		{ required: true, message: 'Contact Email is required', trigger: 'blur' },
		{ type: 'email', message: 'Please enter a valid email address', trigger: 'blur' },
	], // 必填，且需要验证邮箱格式
	workFlowId: [{ required: true, message: 'Work Flow is required', trigger: 'blur' }],
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

// 管道视图状态
const visibleStages = ref<string[]>([]);
const expandedStages = ref<string[]>([]);

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

// 计算属性
const currentPageData = computed(() => {
	return onboardingList.value;
});

const groupedLeads = computed(() => {
	const grouped: Record<string, OnboardingItem[]> = {};

	// 按阶段分组
	onboardingList.value.forEach((item) => {
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

// API调用函数
const loadOnboardingList = async (event?: any) => {
	try {
		loading.value = true;
		const queryParams: OnboardingQueryRequest = {
			page: currentPage.value,
			size: pageSize.value,
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

		const response: ApiResponse<OnboardingItem> = await queryOnboardings(queryParams);

		onboardingList.value = response.data.data || [];
		totalElements.value = response.data.total || 0;
	} catch (error) {
		onboardingList.value = [];
		totalElements.value = 0;
	} finally {
		loading.value = false;
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
	const etaDate = new Date(eta);
	const today = new Date();
	today.setHours(0, 0, 0, 0);
	return etaDate < today;
};

const formatDate = (dateString: string | null) => {
	if (!dateString) return '';
	try {
		const date = new Date(dateString);
		if (isNaN(date.getTime())) return '';
		return date.toLocaleDateString('en-US', {
			month: '2-digit',
			day: '2-digit',
			year: 'numeric',
		});
	} catch {
		return '';
	}
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
		groupedLeads.value[stage]?.filter((item) => isOverdue(item.estimatedCompletionDate))
			.length || 0
	);
};

// 事件处理函数
const handleSearch = async () => {
	currentPage.value = 1;
	searchParams.page = 1;
	await loadOnboardingList();
};

const handleReset = async () => {
	// 重置搜索参数，但保留分页参数
	searchParams.leadId = '';
	searchParams.leadName = '';
	searchParams.lifeCycleStageName = '';
	searchParams.currentStageId = '';
	searchParams.updatedBy = '';
	searchParams.priority = '';

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

		// 调用导出接口
		const response = await exportOnboarding();

		// 创建下载链接
		const blob = new Blob([response], {
			type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
		});
		const url = window.URL.createObjectURL(blob);
		const link = document.createElement('a');
		link.href = url;

		// 设置文件名，包含时间戳
		const timestamp = new Date()
			.toISOString()
			.slice(0, 19)
			.replace(/[-:]/g, '')
			.replace('T', '_');
		link.download = `Onboarding_List_${timestamp}.xlsx`;

		// 触发下载
		document.body.appendChild(link);
		link.click();
		document.body.removeChild(link);
		window.URL.revokeObjectURL(url);

		ElMessage.success('Export completed successfully');
	} finally {
		loading.value = false;
	}
};

const handlePageUpdate = async (size: number) => {
	pageSize.value = size;
	searchParams.size = size;
	currentPage.value = 1;
	searchParams.page = 1;
	await loadOnboardingList();
};

const handleCurrentChange = async (page: number) => {
	currentPage.value = page;
	searchParams.page = page;
	await loadOnboardingList();
};

const handleLimitUpdate = async () => {
	await loadOnboardingList();
};

const toggleStageVisibility = (stage: string) => {
	const index = visibleStages.value.indexOf(stage);
	if (index > -1) {
		visibleStages.value.splice(index, 1);
	} else {
		visibleStages.value.push(stage);
	}
};

const toggleStageExpansion = (stage: string) => {
	const index = expandedStages.value.indexOf(stage);
	if (index > -1) {
		expandedStages.value.splice(index, 1);
	} else {
		expandedStages.value.push(stage);
	}
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
							apiField: 'COMPANYNAME',
							type: 'text',
							required: true,
							label: 'Lead Company Name',
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
		allWorkflows.value = response.data?.filter((item) => item.isDefault) || [];
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

// 初始化
onMounted(async () => {
	// 加载初始数据
	await Promise.all([
		loadOnboardingList(),
		fetchAllStages(),
		getLifeCycleStage(),
		fetchAllWorkflows(),
	]);
	// 初始化可见阶段
	visibleStages.value = [...activeStages.value];
	// 初始化展开阶段
	expandedStages.value = [...activeStages.value];
});
</script>

<style scoped lang="scss">
/* 头部标题栏样式 */
.onboarding-header {
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

/* 搜索表单样式 */
.onboardSearch-form :deep(.el-form-item) {
	margin-bottom: 0;
}

.onboardSearch-form :deep(.el-input__wrapper) {
	border: 1px solid #d1d5db;
	transition: all 0.2s;
}

.onboardSearch-form :deep(.el-input__wrapper:hover) {
	border-color: #9ca3af;
}

.onboardSearch-form :deep(.el-input__wrapper.is-focus) {
	border-color: #3b82f6;
	box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

/* 表格样式 */
:deep(.el-table) {
	border: none;
}

:deep(.el-table .bg-blue-50) {
	background-color: #eff6ff !important;
}

:deep(.el-table th) {
	background-color: #eff6ff;
	border-bottom: 1px solid #e5e7eb;
	color: #374151;
	font-weight: 500;
}

:deep(.el-table td) {
	border-bottom: 1px solid #f3f4f6;
	padding: 12px 8px;
}

:deep(.el-table tbody tr:hover > td) {
	background-color: #f9fafb;
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
	background-color: #eff6ff;
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

/* 卡片阴影 */
.shadow-sm {
	box-shadow: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
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

	/* 头部标题栏暗色主题 */
	.onboarding-header {
		background-color: var(--black-400);
	}

	.title {
		color: var(--primary-500, #2468f2);
	}

	.new-onboarding-btn {
		background-color: var(--primary-500, #2468f2);
		border-color: var(--primary-500, #2468f2);
		color: white;
	}

	.new-onboarding-btn:hover {
		background-color: var(--primary-600, #1d5ad8);
		border-color: var(--primary-600, #1d5ad8);
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

	/* 卡片和容器背景 - 统一使用 black-400 */
	.bg-white {
		@apply bg-black-400 !important;
	}

	.rounded-md {
		background-color: var(--black-400) !important;
		border: 1px solid var(--black-200) !important;
	}

	/* 搜索表单暗色主题 */
	.onboardSearch-form :deep(.el-input__wrapper) {
		background-color: var(--black-200) !important;
		border: 1px solid var(--black-200) !important;
	}

	.onboardSearch-form :deep(.el-input__wrapper:hover) {
		border-color: var(--black-100) !important;
	}

	.onboardSearch-form :deep(.el-input__wrapper.is-focus) {
		border-color: var(--primary-500);
		box-shadow: 0 0 0 3px rgba(126, 34, 206, 0.2);
	}

	.onboardSearch-form :deep(.el-input__inner) {
		@apply text-white-100;
	}

	/* 表格暗色主题 - 保持头部蓝色样式 */
	:deep(.el-table .bg-blue-50) {
		background-color: #003c76 !important; /* 使用项目的 sea-700 深蓝色 */
	}

	:deep(.el-table th) {
		background-color: #003c76 !important; /* 使用项目的 sea-700 深蓝色 */
		border-bottom: 1px solid #00509d !important; /* 使用项目的 sea-600 */
		color: #cce8d0 !important; /* 使用项目的浅色文字 */
	}

	:deep(.el-table td) {
		border-bottom: 1px solid var(--black-200) !important;
		background-color: var(--black-400) !important;
		color: var(--white-100) !important;
	}

	:deep(.el-table tbody tr:hover > td) {
		background-color: var(--black-300) !important;
	}

	/* 表格整体边框 */
	:deep(.el-table) {
		border: 1px solid var(--black-200) !important;
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
</style>
