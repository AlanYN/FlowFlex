<template>
	<div class="">
		<!-- 加载状态 -->
		<div>
			<!-- 页面头部 -->
			<div class="page-header rounded-lg p-6 mb-6">
				<div class="flex justify-between items-center">
					<div>
						<h1 class="text-3xl font-bold page-title">Checklist Management</h1>
						<p class="page-subtitle mt-1">
							Task checklists for different teams during the onboarding process
						</p>
					</div>
					<div class="flex space-x-2">
						<el-button @click="openCreateDialog" type="primary" size="default">
							<el-icon class="mr-2"><Plus /></el-icon>
							New Checklist
						</el-button>
					</div>
				</div>
			</div>

			<!-- 搜索和筛选区域 -->
			<div class="filter-panel rounded-lg shadow-sm p-4 mb-6">
				<div class="grid grid-cols-1 md:grid-cols-2 gap-4">
					<div class="space-y-2">
						<label class="filter-label text-sm font-medium">Search</label>
						<el-input-tag
							v-model="searchTags"
							placeholder="Enter checklist name and press enter"
							:max="10"
							:disabled="loading"
							@change="handleSearchTagsChange"
							class="w-full"
						/>
					</div>

					<div class="space-y-2">
						<label class="filter-label text-sm font-medium">Team</label>
						<el-select
							v-model="selectedTeam"
							placeholder="Select team"
							class="w-full filter-select"
							@change="handleSearchTagsChange"
						>
							<el-option label="All Teams" value="all" />
							<el-option
								v-for="team in defaultAssignedGroup"
								:key="team.key"
								:label="team.value"
								:value="team.key"
							/>
						</el-select>
					</div>
				</div>
			</div>

			<!-- 视图切换标签页 -->
			<PrototypeTabs
				v-model="activeView"
				:tabs="tabsConfig"
				type="adaptive"
				size="default"
				@tab-change="handleViewChange"
			>
				<!-- 卡片视图 -->
				<TabPane value="card">
					<el-scrollbar ref="scrollbarRef">
						<ChecklistCardView
							:checklists="checklists"
							:loading="loading"
							:empty-message="getEmptyStateMessage()"
							:workflows="workflows"
							:all-stages="stages"
							:delete-loading="deleteLoading"
							:export-loading="exportLoading"
							:duplicate-loading="duplicateLoading"
							@edit-checklist="editChecklist"
							@delete-checklist="deleteChecklistItem"
							@export-checklist="exportChecklistItem"
							@duplicate-checklist="duplicateChecklistItem"
							@view-tasks="openTaskDialog"
						/>
					</el-scrollbar>
				</TabPane>

				<!-- 列表视图 -->
				<TabPane value="list">
					<ChecklistListView
						:checklists="checklists"
						:loading="loading"
						:workflows="workflows"
						:all-stages="stages"
						:delete-loading="deleteLoading"
						:export-loading="exportLoading"
						:duplicate-loading="duplicateLoading"
						@edit-checklist="editChecklist"
						@delete-checklist="deleteChecklistItem"
						@export-checklist="exportChecklistItem"
						@duplicate-checklist="duplicateChecklistItem"
						@view-tasks="openTaskDialog"
						@selection-change="handleSelectionChange"
						@sort-change="handleSortChange"
					/>
				</TabPane>
			</PrototypeTabs>
		</div>

		<!-- 统一分页组件 -->
		<div v-if="!loading && pagination.total > 0">
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

		<!-- Task列表弹窗 -->
		<el-dialog v-model="showTaskDialog" :width="bigDialogWidth" :close-on-click-modal="false">
			<template #header>
				<div class="w-[750px] truncate text-2xl font-bold">
					{{ currentChecklist?.name }}
				</div>
			</template>
			<el-scrollbar max-height="70vh">
				<!-- Task列表内容 -->
				<div v-if="currentChecklist">
					<TaskList ref="taskListRef" :checklist="currentChecklist" />
				</div>
			</el-scrollbar>
			<template #footer>
				<div class="flex justify-end">
					<el-button @click="closeTaskDialog">Close</el-button>
				</div>
			</template>
		</el-dialog>

		<!-- 检查清单对话框 (创建/编辑通用) -->
		<el-dialog
			v-model="showDialog"
			:title="dialogConfig.title"
			:width="dialogWidth"
			:close-on-click-modal="false"
		>
			<template #header>
				<div>
					<h3 class="text-lg font-medium text-gray-900">{{ dialogConfig.title }}</h3>
					<p class="text-sm text-gray-600 mt-1">{{ dialogConfig.description }}</p>
				</div>
			</template>

			<el-form :model="formData" label-position="top" class="space-y-4 p-1">
				<el-form-item label="Checklist Name" required>
					<el-input v-model="formData.name" :placeholder="dialogConfig.namePlaceholder" />
				</el-form-item>

				<el-form-item label="Description">
					<el-input
						v-model="formData.description"
						type="textarea"
						:placeholder="dialogConfig.descriptionPlaceholder"
						:rows="3"
					/>
				</el-form-item>

				<el-form-item label="Team" required>
					<el-select v-model="formData.team" placeholder="Select team" class="w-full">
						<el-option
							v-for="team in defaultAssignedGroup"
							:key="team.value"
							:label="team.key"
							:value="team.value"
						/>
					</el-select>
				</el-form-item>
			</el-form>

			<template #footer>
				<div class="flex justify-end gap-3">
					<el-button @click="closeDialog">Cancel</el-button>
					<el-button
						@click="submitDialog"
						type="primary"
						:disabled="!formData.name || !formData.team"
						:loading="dialogConfig.loading"
					>
						{{ dialogConfig.submitText }}
					</el-button>
				</div>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, shallowRef, nextTick, markRaw } from 'vue';
import {
	getChecklists,
	createChecklist,
	updateChecklist,
	deleteChecklist,
	duplicateChecklist,
	getChecklistTasks,
} from '@/apis/ow/checklist';
import { getWorkflows, getAllStages } from '@/apis/ow';
import { useI18n } from '@/hooks/useI18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { defaultAssignedGroup } from '@/enums/dealsAndLeadsOptions';
import { exportChecklistToPdf } from '@/utils/pdfExport';
import { PrototypeTabs, TabPane } from '@/components/PrototypeTabs';
import { Plus } from '@element-plus/icons-vue';
import ChecklistCardView from './components/ChecklistCardView.vue';
import ChecklistListView from './components/ChecklistListView.vue';
import TaskList from './components/TaskList.vue';
import { useAdaptiveScrollbar } from '@/hooks/useAdaptiveScrollbar';
import CustomerPagination from '@/components/global/u-pagination/index.vue';
import { Checklist } from '#/checklist';
import { dialogWidth, bigDialogWidth } from '@/settings/projectSetting';
import TableViewIcon from '@assets/svg/onboard/tavleView.svg';
import ProgressViewIcon from '@assets/svg/onboard/progressView.svg';

interface Workflow {
	id: string;
	name: string;
	isDefault?: boolean;
}

interface Stage {
	id: string;
	name: string;
	workflowId?: string;
}

// 响应式数据
const checklists = shallowRef<Checklist[]>([]);
const workflows = shallowRef<Workflow[]>([]);
const stages = shallowRef<Stage[]>([]);
const loading = ref(false);

// UI状态
const searchTags = ref([]);
const selectedTeam = ref('all');
const activeDropdown = ref(null);

// Task弹窗状态
const showTaskDialog = ref(false);
const currentChecklist = ref<Checklist | null>(null);

// 视图切换
const activeView = ref('list');
const tabsConfig = ref([
	{ label: 'Table View', value: 'list', icon: markRaw(TableViewIcon) },
	{ label: 'Card View', value: 'card', icon: markRaw(ProgressViewIcon) },
]);

// 使用自适应滚动条 hook
const { scrollbarRef } = useAdaptiveScrollbar(70);

// 分页相关状态
const pagination = ref({
	pageIndex: 1,
	pageSize: 15,
	total: 0,
});

// 防抖搜索
let searchTimeout: any = null;

// 统一对话框状态
const showDialog = ref(false);
const dialogMode = ref<'create' | 'edit'>('create');
const editingChecklist = ref<Checklist | null>(null);

// 对话框配置 (根据模式动态计算)
const dialogConfig = computed(() => {
	const isEdit = dialogMode.value === 'edit';
	return {
		title: isEdit ? 'Edit Checklist' : 'Create New Checklist',
		description: isEdit
			? 'Update the checklist details'
			: 'Create a new checklist for a specific team in the onboarding process.',
		namePlaceholder: isEdit ? '' : 'Enter checklist name',
		descriptionPlaceholder: isEdit ? '' : 'Enter checklist description',
		submitText: isEdit ? 'Save Changes' : 'Create Checklist',
		loading: isEdit ? editLoading.value : createLoading.value,
	};
});

// 表单数据
const formData = ref({
	name: '',
	description: '',
	team: '',
	workflow: '',
	stage: '',
	assignments: [],
});

// Loading 状态管理
const createLoading = ref(false);
const editLoading = ref(false);
const deleteLoading = ref(false);
const duplicateLoading = ref(false);
const exportLoading = ref(false);

const { t } = useI18n();

// 视图切换和辅助方法
const handleViewChange = (value) => {
	activeView.value = value;
};

const getEmptyStateMessage = () => {
	if (searchTags.value.length > 0 || selectedTeam.value !== 'all') {
		return 'Try adjusting your filters';
	}
	return 'No checklists have been created yet';
};

// 分页处理方法
const handleCurrentChange = (page: number) => {
	pagination.value.pageIndex = page;
};

const handlePageUpdate = (pageSize: number) => {
	pagination.value.pageSize = pageSize;
};

const handleLimitUpdate = () => {
	loadChecklists();
};

// 数据加载方法 - 支持分页和过滤
const loadChecklists = async (resetPage = false) => {
	try {
		loading.value = true;

		// 如果需要重置页码
		if (resetPage) {
			pagination.value.pageIndex = 1;
		}

		// 构建查询参数
		const queryParams: any = {
			pageIndex: pagination.value.pageIndex,
			pageSize: pagination.value.pageSize,
		};

		// 只有当搜索关键词不为空时才添加搜索参数
		if (searchTags.value && searchTags.value.length > 0) {
			queryParams.name = searchTags.value.join(',');
		}

		// 只有当team不是'all'时才添加team参数
		if (selectedTeam.value) {
			queryParams.team = selectedTeam.value !== 'all' ? selectedTeam.value : null;
		}

		const response = await getChecklists(queryParams);

		// 处理API响应
		if (response.code === '200' || response.data) {
			checklists.value = response.data?.items || [];
			pagination.value.total = response.data?.totalCount || 0;
		} else {
			checklists.value = [];
			pagination.value.total = 0;
			if (response.msg) {
				ElMessage.error(response.msg);
			}
		}
	} catch (err: any) {
		checklists.value = [];
		pagination.value.total = 0;
		ElMessage.error('Failed to load checklists');
	} finally {
		loading.value = false;
	}
};

// 优化的workflow和stage加载逻辑 - 使用统一的getAllStages接口
const loadWorkflowsAndStages = async () => {
	try {
		// 并行加载workflows和stages，提高性能
		const [workflowResponse, stagesResponse] = await Promise.all([
			getWorkflows(),
			getAllStages(),
		]);

		// 处理workflows响应
		if (workflowResponse.code === '200') {
			workflows.value = workflowResponse.data || [];
		} else {
			workflows.value = [];
		}

		// 处理stages响应
		if (stagesResponse.code === '200') {
			// 所有stages都已经包含workflowId，直接使用
			stages.value = stagesResponse.data || [];
		} else {
			stages.value = [];
		}
	} catch (err) {
		workflows.value = [];
		stages.value = [];
	}
};

const editChecklist = async (checklist) => {
	editingChecklist.value = checklist;

	// 根据ID查找workflow名称
	let workflowName = '';
	if (checklist.workflowId) {
		const workflow = workflows.value.find(
			(w) => w.id.toString() === checklist.workflowId.toString()
		);
		workflowName = workflow ? workflow.name : '';
	}

	// 现在查找stage名称（stages已经在全局加载）
	let stageName = '';
	if (checklist.stageId) {
		const stage = stages.value.find((s) => s.id.toString() === checklist.stageId.toString());
		stageName = stage ? stage.name : '';
	}

	formData.value = {
		name: checklist.name,
		description: checklist.description,
		team: checklist.team,
		workflow: workflowName,
		stage: stageName,
		assignments: [],
	};

	dialogMode.value = 'edit';
	showDialog.value = true;
	activeDropdown.value = null;
};

const deleteChecklistItem = async (checklistId) => {
	try {
		await ElMessageBox.confirm(
			'Are you sure you want to delete this checklist? This action cannot be undone.',
			'Confirm Deletion',
			{
				confirmButtonText: 'Delete Checklist',
				cancelButtonText: 'Cancel',
				type: 'warning',
				customClass: 'custom-confirm-dialog',
				confirmButtonClass: 'el-button--danger',
			}
		);
	} catch {
		return; // 用户取消删除
	}

	deleteLoading.value = true;
	try {
		await deleteChecklist(checklistId, true);
		ElMessage.success('Checklist deleted successfully');
		activeDropdown.value = null;

		// 删除成功后立即刷新页面数据
		await loadChecklists();

		// 清理工作完成（不再需要展开状态管理）
	} catch (err) {
		// 提供更详细的错误信息
		let errorMessage = 'Failed to delete checklist';
		if (err.response?.status === 404) {
			errorMessage = 'Checklist not found or already deleted';
		} else if (err.response?.status === 403) {
			errorMessage = 'You do not have permission to delete this checklist';
		} else if (err.message) {
			errorMessage = `Deletion failed: ${err.message}`;
		}

		ElMessage.error(errorMessage);
		activeDropdown.value = null;
		await loadChecklists();
		// 错误处理完成（不再需要展开状态管理）
	} finally {
		deleteLoading.value = false;
	}
};

const duplicateChecklistItem = async (checklist: Checklist) => {
	duplicateLoading.value = true;
	try {
		// 让后端自动处理名称生成（添加 "(Copy)" 后缀和唯一性保证）
		// 不传递 name 字段，这样后端会自动使用 "{原名称} (Copy)" 格式
		const duplicateData = {
			// name: undefined, // 不传 name，让后端自动生成
			description: checklist.description || '',
			targetTeam: checklist.team || 'Sales', // 注意：使用 targetTeam 而不是 team
			copyTasks: true,
			setAsTemplate: false,
		};

		const newChecklist = await duplicateChecklist(checklist.id, duplicateData);
		console.log('Duplicate response:', newChecklist);

		ElMessage.success('Checklist duplicated successfully');
		activeDropdown.value = null;

		await loadChecklists();
	} catch (err: any) {
		// 提供更详细的错误信息
		let errorMessage = 'Failed to duplicate checklist';
		if (err.response?.status === 500) {
			errorMessage = 'Server error occurred. Please try again.';
		} else if (err.response?.status === 404) {
			errorMessage = 'Checklist not found.';
		} else if (err.response?.status === 400) {
			errorMessage = 'Invalid request parameters.';
		} else if (err.message) {
			errorMessage = `Duplication failed: ${err.message}`;
		}

		ElMessage.error(errorMessage);
		activeDropdown.value = null;

		await loadChecklists();
	} finally {
		duplicateLoading.value = false;
	}
};

// Task弹窗管理
const taskListRef = ref<InstanceType<typeof TaskList> | null>(null);
const openTaskDialog = async (checklist: Checklist) => {
	currentChecklist.value = checklist;
	showTaskDialog.value = true;
	activeDropdown.value = null;
	nextTick(() => {
		taskListRef.value?.loadTasks();
	});
};

const closeTaskDialog = () => {
	taskListRef.value?.resetTaskList();
	nextTick(() => {
		showTaskDialog.value = false;
		currentChecklist.value = null;
		loadChecklists();
	});
};

// 处理表格选择变化
const handleSelectionChange = (selection) => {
	console.log('Selection changed:', selection);
	// 可以在这里处理批量操作
};

// 处理表格排序变化
const handleSortChange = (sort) => {
	console.log('Sort changed:', sort);
	// 可以在这里处理排序逻辑
};

// 导出PDF文件功能
const exportChecklistItem = async (checklist: Checklist) => {
	exportLoading.value = true;
	try {
		// 使用公用的PDF导出函数
		let tasksData = [];
		try {
			const response = await getChecklistTasks(checklist.id);
			tasksData = response.data || response || [];
		} catch {
			tasksData = [];
		}
		await exportChecklistToPdf(
			{
				...checklist,
				tasks: tasksData,
			},
			{ workflows: workflows.value, stages: stages.value },
			{
				headerTitle: '',
				headerSubtitle: 'Warehousing Solutions',
				showHeader: true,
				showAssignments: true,
				showTaskTable: true,
			}
		);
		activeDropdown.value = null;
	} catch (err: any) {
		ElMessage.error(`PDF export failed: ${err.message || 'Unknown error'}`);
		activeDropdown.value = null;
	} finally {
		exportLoading.value = false;
	}
};

// 对话框管理方法
// 打开创建对话框并设置默认值
const openCreateDialog = async () => {
	dialogMode.value = 'create';
	editingChecklist.value = null;
	showDialog.value = true;

	// 重置表单数据
	formData.value = {
		name: '',
		description: '',
		team: '',
		workflow: '',
		stage: '',
		assignments: [],
	};
};

// 统一关闭对话框
const closeDialog = () => {
	showDialog.value = false;
	editingChecklist.value = null;

	formData.value = {
		name: '',
		description: '',
		team: '',
		workflow: '',
		stage: '',
		assignments: [],
	};
};

// 统一的对话框提交处理
const submitDialog = async () => {
	if (!formData.value.name.trim() || !formData.value.team) return;

	const isEdit = dialogMode.value === 'edit';

	if (isEdit) {
		editLoading.value = true;
	} else {
		createLoading.value = true;
	}

	try {
		const checklistData = {
			name: formData.value.name.trim(),
			description: formData.value.description || '',
			team: formData.value.team,
			type: isEdit ? editingChecklist.value?.type || 'Instance' : 'Instance',
			status: isEdit ? editingChecklist.value?.status || 'Active' : 'Active',
			isTemplate: isEdit ? editingChecklist.value?.isTemplate || false : false,
			isActive: isEdit ? editingChecklist.value?.isActive !== false : true,
		};

		if (isEdit && editingChecklist.value) {
			const originalChecklistId = editingChecklist.value.id;
			await updateChecklist(originalChecklistId, checklistData);
			ElMessage.success('Checklist updated successfully');
		} else {
			const newChecklist = await createChecklist(checklistData);
			console.log('Checklist created successfully:', newChecklist);
			ElMessage.success(t('sys.api.operationSuccess'));
		}

		closeDialog();

		// 重新加载数据（不再需要展开状态管理）
		await loadChecklists();
	} catch (err) {
		// 提供更详细的错误信息
		let errorMessage = `Failed to ${isEdit ? 'update' : 'create'} checklist`;
		if (err.response?.status === 404) {
			errorMessage = 'Checklist not found';
		} else if (err.response?.status === 403) {
			errorMessage = `You do not have permission to ${
				isEdit ? 'update' : 'create'
			} this checklist`;
		} else if (err.response?.status === 400) {
			errorMessage = 'Invalid checklist data';
		} else if (err.message) {
			errorMessage = `${isEdit ? 'Update' : 'Creation'} failed: ${err.message}`;
		}

		ElMessage.error(isEdit ? errorMessage : t('sys.api.operationFailed'));
		closeDialog();

		await loadChecklists();
	} finally {
		if (isEdit) {
			editLoading.value = false;
		} else {
			createLoading.value = false;
		}
	}
};

// closeEditDialog函数已合并到closeDialog中，saveEditChecklist函数已合并到submitDialog中

// 搜索处理函数
const handleSearch = () => {
	// 清除之前的定时器
	if (searchTimeout) {
		clearTimeout(searchTimeout);
	}
	// 设置新的定时器，实现防抖
	searchTimeout = setTimeout(() => {
		// 当筛选条件改变时，重置到第一页并重新获取数据
		loadChecklists(true);
	}, 300);
};

// 标签变化处理函数
const handleSearchTagsChange = () => {
	handleSearch();
};

onMounted(() => {
	Promise.all([loadWorkflowsAndStages(), loadChecklists()]);
});
</script>

<style scoped lang="scss">
/* 页面头部样式 */
.page-header {
	@apply dark:from-primary-600 dark:to-primary-500;
	background: linear-gradient(to right, var(--primary-50), var(--primary-100));
	flex-shrink: 0;
}

.page-title {
	color: var(--primary-500);
	@apply dark:text-white;
}

.page-subtitle {
	color: var(--primary-600);
}

/* 筛选面板样式 */
.filter-panel {
	@apply bg-white dark:bg-black-400;
	border: 1px solid var(--primary-100);
	@apply dark:border-black-200;
	flex-shrink: 0;
}

.filter-label {
	color: var(--primary-700);
	@apply dark:text-primary-300;
}

/* Task弹窗样式 */
.task-dialog-content {
	min-height: 400px;
	max-height: 70vh;
	overflow-y: auto;
}

/* 暗色主题样式 */
html.dark {
	/* 筛选面板暗色主题 */
	.filter-panel {
		@apply bg-black-400 dark:border-black-200;
	}

	.filter-label {
		@apply dark:text-primary-300;
	}
}
</style>
