<template>
	<div class="">
		<!-- Header and Action Area -->
		<PageHeader
			title="Tools"
			description="Configure and manage automation tools to streamline your business processes"
		>
			<template #actions>
				<!-- Tab切换器 -->
				<TabButtonGroup
					v-model="activeTab"
					:tabs="tabsConfig"
					size="small"
					type="adaptive"
					class="mr-4"
					@tab-change="handleTabChange"
				/>
				<el-button
					class="page-header-btn page-header-btn-secondary"
					@click="handleExport"
					:loading="exportLoading"
					:icon="Download"
				>
					<span>Export</span>
				</el-button>
				<el-button
					class="page-header-btn page-header-btn-primary"
					type="primary"
					@click="handleCreateAction"
					:icon="Plus"
				>
					<span>New Tool</span>
				</el-button>
			</template>
		</PageHeader>

		<!-- Search and Filter Area -->
		<div class="filter-panel rounded-xl shadow-sm p-4 mb-6">
			<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
				<div class="space-y-2">
					<label class="filter-label text-sm font-medium">Tool ID or Tool Name</label>
					<el-input
						v-model="searchForm.keyword"
						placeholder="Enter Tool ID or Tool Name"
						clearable
						class="w-full rounded-xl filter-input"
						@change="handleSearch"
					/>
				</div>

				<div class="space-y-2">
					<label class="filter-label text-sm font-medium">Type</label>
					<el-select
						v-model="searchForm.type"
						placeholder="Select Type"
						clearable
						class="w-full filter-select"
						@change="handleSearch"
					>
						<el-option
							v-for="option in getActionTypeOptions()"
							:key="option.value"
							:label="option.label"
							:value="option.value"
						/>
					</el-select>
				</div>
			</div>
		</div>

		<!-- Tabs Area -->
		<PrototypeTabs
			v-model="activeTab"
			:tabs="tabsConfig"
			type="adaptive"
			size="default"
			:hidden-tab="true"
			@tab-change="handleTabChange"
		>
			<!-- Tools Tab -->
			<TabPane value="tools">
				<div class="customer-block !p-0 !ml-0">
					<el-table
						:data="actionsList"
						style="width: 100%"
						@selection-change="handleSelectionChange"
						:max-height="tableMaxHeight"
						v-loading="loading"
					>
						<el-table-column type="selection" width="55" />
						<el-table-column prop="actionCode" label="Tool ID" width="120" />
						<el-table-column prop="name" label="Tool Name" min-width="200" />
						<el-table-column prop="actionType" label="Type" width="150">
							<template #default="{ row }">
								<el-tag class="type-tag">
									{{ getActionTypeName(row.actionType) }}
								</el-tag>
							</template>
						</el-table-column>
						<el-table-column label="Actions" width="120" fixed="right">
							<template #default="{ row }">
								<div class="action-buttons">
									<el-tooltip content="Edit" placement="top">
										<el-button type="primary" link @click="handleEdit(row)">
											<el-icon>
												<Edit />
											</el-icon>
										</el-button>
									</el-tooltip>
									<el-tooltip content="Delete" placement="top">
										<el-button type="danger" link @click="handleDelete(row)">
											<el-icon>
												<Delete />
											</el-icon>
										</el-button>
									</el-tooltip>
								</div>
							</template>
						</el-table-column>
					</el-table>

					<!-- Pagination -->
					<CustomerPagination
						:total="pagination.total"
						:limit="pagination.pageSize"
						:page="pagination.currentPage"
						:background="true"
						@pagination="handleLimitUpdate"
						@update:page="handleCurrentChange"
						@update:limit="handlePageUpdate"
					/>
				</div>
			</TabPane>

			<!-- My Action Tab -->
			<TabPane value="myAction">
				<div class="customer-block !p-0 !ml-0">
					<el-table
						:data="actionsList"
						style="width: 100%"
						@selection-change="handleSelectionChange"
						:max-height="tableMaxHeight"
						v-loading="loading"
					>
						<el-table-column type="selection" width="55" />
						<el-table-column prop="actionCode" label="Tool ID" width="120" />
						<el-table-column prop="name" label="Tool Name" min-width="200" />
						<el-table-column prop="actionType" label="Type" width="150">
							<template #default="{ row }">
								<el-tag class="type-tag">
									{{ getActionTypeName(row.actionType) }}
								</el-tag>
							</template>
						</el-table-column>
						<el-table-column label="Actions" width="120" fixed="right">
							<template #default="{ row }">
								<div class="action-buttons">
									<el-tooltip content="Edit" placement="top">
										<el-button type="primary" link @click="handleEdit(row)">
											<el-icon>
												<Edit />
											</el-icon>
										</el-button>
									</el-tooltip>
									<el-tooltip content="Delete" placement="top">
										<el-button type="danger" link @click="handleDelete(row)">
											<el-icon>
												<Delete />
											</el-icon>
										</el-button>
									</el-tooltip>
								</div>
							</template>
						</el-table-column>
					</el-table>

					<!-- Pagination -->
					<div class="border-t bg-white rounded-b-md">
						<CustomerPagination
							:total="pagination.total"
							:limit="pagination.pageSize"
							:page="pagination.currentPage"
							:background="true"
							@pagination="handleLimitUpdate"
							@update:page="handleCurrentChange"
							@update:limit="handlePageUpdate"
						/>
					</div>
				</div>
			</TabPane>
		</PrototypeTabs>

		<!-- Action Config Dialog -->
		<ActionConfigDialog
			v-model="actionEditorVisible"
			:action="actionInfo"
			:is-editing="!!actionInfo"
			:triggerSourceId="currentEditAction?.id"
			:loading="editActionLoading"
			:force-editable="true"
			@save-success="onActionSave"
			@cancel="onActionCancel"
		/>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, markRaw } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Plus, Download, Edit, Delete } from '@element-plus/icons-vue';
import CustomerPagination from '@/components/global/u-pagination/index.vue';
import ActionConfigDialog from '@/components/actionTools/ActionConfigDialog.vue';
import PageHeader from '@/components/global/PageHeader/index.vue';
import { PrototypeTabs, TabPane, TabButtonGroup } from '@/components/PrototypeTabs';
import {
	getActionDefinitions,
	deleteAction,
	exportActions,
	getActionDetail,
	ActionType,
	ACTION_TYPE_MAPPING,
} from '@/apis/action';
import { ActionDefinition, ActionQueryRequest } from '#/action';
import { tableMaxHeight } from '@/settings/projectSetting';
import TableViewIcon from '@assets/svg/onboard/tavleView.svg';

// Reactive data
const loading = ref(false);
const exportLoading = ref(false);
const selectedActions = ref<any[]>([]);

// Action 弹窗相关状态
const actionEditorVisible = ref(false);
const actionInfo = ref(null);
const editActionLoading = ref(false);
const currentEditAction = ref<ActionDefinition | null>(null);

// Search form
const searchForm = reactive({
	keyword: '',
	type: 'all',
});

// Tabs configuration
const activeTab = ref('tools');
const tabsConfig = ref([
	{ label: 'Tools', value: 'tools', icon: markRaw(TableViewIcon) },
	{ label: 'My Tools', value: 'myAction', icon: markRaw(TableViewIcon) },
]);

// Pagination
const pagination = reactive({
	currentPage: 1,
	pageSize: 20,
	total: 0,
});

// Current page data
const actionsList = ref<ActionDefinition[]>([]);

// Methods
const getActionTypeName = (actionType: number) => {
	return ACTION_TYPE_MAPPING[actionType as ActionType] || 'Unknown';
};

// Get all available action type options
const getActionTypeOptions = () => {
	return [
		{ label: 'All Types', value: 'all' },
		...Object.entries(ACTION_TYPE_MAPPING).map(([key, value]) => ({
			label: value,
			value: key,
		})),
	];
};

const handleCreateAction = () => {
	currentEditAction.value = null;
	actionInfo.value = null;
	actionEditorVisible.value = true;
};

const handleExport = async () => {
	try {
		// Show export loading
		exportLoading.value = true;

		// Build query parameters (same as search)
		const params: ActionQueryRequest = {
			pageIndex: 1,
			pageSize: 10000, // Export all data
		};

		// Add search conditions if any
		if (searchForm.keyword) {
			params.search = searchForm.keyword;
		}

		if (searchForm.type && searchForm.type !== 'all') {
			params.actionType = searchForm.type;
		}

		// Handle tab-based filtering
		if (activeTab.value === 'tools') {
			params.isTools = true; // 只筛选 isTools = true 的记录
		} else if (activeTab.value === 'myAction') {
			params.isTools = false; // 只筛选 isTools = false 的记录
		}

		// Call export API
		const response = await exportActions(params);

		// Create download link
		const blob = new Blob([response], {
			type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
		});
		const url = window.URL.createObjectURL(blob);
		const link = document.createElement('a');
		link.href = url;
		link.download = `actions_export_${new Date().toISOString().split('T')[0]}.xlsx`;
		document.body.appendChild(link);
		link.click();
		document.body.removeChild(link);
		window.URL.revokeObjectURL(url);

		ElMessage.success('Export completed successfully');
	} catch (error) {
		console.error('Export failed:', error);
		ElMessage.error('Export failed. Please try again.');
	} finally {
		exportLoading.value = false;
	}
};

const handleSearch = async () => {
	// Reset to first page when searching
	pagination.currentPage = 1;
	// Reload data with search conditions
	await loadActionsList();
};

const handleTabChange = async (tabValue: string) => {
	// Reset to first page when switching tabs
	pagination.currentPage = 1;
	// Reload data based on the selected tab
	await loadActionsList();
};

const handleSelectionChange = (selection: any[]) => {
	selectedActions.value = selection;
};

const handleEdit = async (row: ActionDefinition) => {
	currentEditAction.value = row;
	actionEditorVisible.value = true;

	// 获取 action 详情
	if (!row.id) {
		ElMessage.error('Action ID is missing');
		return;
	}

	try {
		editActionLoading.value = true;
		const actionDetailRes = await getActionDetail(row.id);
		if (actionDetailRes.code === '200' && actionDetailRes?.data) {
			actionInfo.value = {
				...actionDetailRes?.data,
				actionConfig: JSON.parse(actionDetailRes?.data?.actionConfig || '{}'),
				type: actionDetailRes?.data?.actionType,
			};
		}
	} catch (error) {
		console.error('Failed to load action details:', error);
		ElMessage.warning('Failed to load action details');
	} finally {
		editActionLoading.value = false;
	}
};

const handleDelete = async (row: ActionDefinition) => {
	try {
		await ElMessageBox.confirm(
			`Are you sure you want to delete action "${row.name}"?`,
			'Confirm Delete',
			{
				confirmButtonText: 'Delete',
				cancelButtonText: 'Cancel',
				type: 'warning',
			}
		);

		loading.value = true;
		const response = await deleteAction(row?.id || '');

		if (response.code === '200' && response.success) {
			ElMessage.success('Action deleted successfully');
			// Reload data
			await loadActionsList();
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

const handlePageUpdate = async (size: number) => {
	pagination.pageSize = size;
	pagination.currentPage = 1;
	// Reload data
	await loadActionsList();
};

const handleCurrentChange = async (page: number) => {
	pagination.currentPage = page;
	// Reload data
	await loadActionsList();
};

const handleLimitUpdate = async () => {
	await loadActionsList();
};

// Action 保存成功回调
const onActionSave = async (actionResult) => {
	if (actionResult.id) {
		// 重新加载列表数据
		await loadActionsList();
	}
	onActionCancel();
};

// 取消 Action 编辑
const onActionCancel = () => {
	actionEditorVisible.value = false;
	actionInfo.value = null;
	currentEditAction.value = null;
};

// Load Actions list from API
const loadActionsList = async () => {
	try {
		loading.value = true;

		// Build query parameters
		const params: ActionQueryRequest = {
			pageIndex: pagination.currentPage,
			pageSize: pagination.pageSize,
		};

		// Add search conditions
		if (searchForm.keyword) {
			params.search = searchForm.keyword;
		}

		if (searchForm.type && searchForm.type !== 'all') {
			// Use enum value directly, convert to string
			params.actionType = searchForm.type;
		}

		// Handle tab-based filtering
		if (activeTab.value === 'tools') {
			params.isTools = true; // 只筛选 isTools = true 的记录
		} else if (activeTab.value === 'myAction') {
			params.isTools = false; // 只筛选 isTools = false 的记录
		}

		// Call API
		const response = await getActionDefinitions(params);

		if (response.code === '200' && response.success) {
			actionsList.value = response.data.data || [];
			pagination.total = response.data.total || 0;
			pagination.currentPage = response.data.pageIndex || 1;
			pagination.pageSize = response.data.pageSize || 20;
		} else {
			actionsList.value = [];
			pagination.total = 0;
			ElMessage.error(response.msg || 'Failed to load actions');
		}
	} catch (error) {
		console.error('Failed to load actions:', error);
		actionsList.value = [];
		pagination.total = 0;
		ElMessage.error('Failed to load actions');
	} finally {
		loading.value = false;
	}
};

// Lifecycle
onMounted(() => {
	// Initialize data
	loadActionsList();
});
</script>

<style scoped lang="scss">
.assignments-list {
	.assignment-item {
		display: flex;
		flex-direction: column;
		margin-bottom: 8px;
		padding: 8px;

		&:last-child {
			margin-bottom: 0;
		}

		.assignment-name {
			font-weight: 500;
			color: #374151;
			margin-bottom: 4px;
		}

		.assignment-date {
			font-size: 12px;
			color: #6b7280;
		}
	}
}

.action-buttons {
	display: flex;
	gap: 8px;
	justify-content: center;
}

.type-tag {
	background-color: #e6f3ff !important;
	border-color: #b3d9ff !important;
	color: #2468f2 !important;
	padding: 4px 12px !important;
	font-size: 12px !important;
	font-weight: 500 !important;
}

/* 筛选面板样式 */
.filter-panel {
	@apply bg-white dark:bg-black-400;
	border: 1px solid var(--primary-100);
	@apply dark:border-black-200;
}

.filter-label {
	color: var(--primary-700);
	@apply dark:text-primary-300;
}

/* Element Plus 组件样式覆盖 */
:deep(.filter-input .el-input__wrapper) {
	border-color: var(--primary-200);
	@apply dark:border-black-200;
}

:deep(.filter-input .el-input__wrapper:hover) {
	border-color: var(--primary-400);
	@apply dark:border-primary-600;
}

:deep(.filter-input .el-input__wrapper.is-focus) {
	border-color: var(--primary-500);
	@apply dark:border-primary-500;
}

:deep(.filter-select .el-input__wrapper) {
	border-color: var(--primary-200);
	@apply dark:border-black-200;
}

:deep(.filter-select .el-input__wrapper:hover) {
	border-color: var(--primary-400);
	@apply dark:border-primary-600;
}

:deep(.filter-select .el-input__wrapper.is-focus) {
	border-color: var(--primary-500);
	@apply dark:border-primary-500;
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

	/* Element Plus 组件暗色主题 */
	:deep(.filter-input .el-input__wrapper) {
		background-color: var(--black-200) !important;
		border-color: var(--black-200) !important;
	}

	:deep(.filter-input .el-input__wrapper:hover) {
		border-color: var(--black-100) !important;
	}

	:deep(.filter-input .el-input__wrapper.is-focus) {
		border-color: var(--primary-500);
		box-shadow: 0 0 0 3px rgba(126, 34, 206, 0.2);
	}

	:deep(.filter-input .el-input__inner) {
		@apply text-white-100;
	}

	:deep(.filter-select .el-input__wrapper) {
		background-color: var(--black-200) !important;
		border-color: var(--black-200) !important;
	}

	:deep(.filter-select .el-input__wrapper:hover) {
		border-color: var(--black-100) !important;
	}

	:deep(.filter-select .el-input__wrapper.is-focus) {
		border-color: var(--primary-500);
		box-shadow: 0 0 0 3px rgba(126, 34, 206, 0.2);
	}

	:deep(.filter-select .el-input__inner) {
		@apply text-white-100;
	}

	/* 标签样式在暗色主题下的适配 */
	.assignments-list .assignment-item .assignment-name {
		color: var(--white-100) !important;
	}

	.assignments-list .assignment-item .assignment-date {
		color: var(--gray-300) !important;
	}
}
</style>
