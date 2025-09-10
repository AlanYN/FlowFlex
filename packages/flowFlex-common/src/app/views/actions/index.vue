<template>
	<div class="bg-gray-50">
		<!-- Header and Action Area -->
		<div class="actions-header">
			<h1 class="title">Tools</h1>
			<div class="actions">
				<el-button class="new-action-btn" type="primary" @click="handleCreateAction">
					<el-icon>
						<Plus />
					</el-icon>
					<span>New Tool</span>
				</el-button>
				<el-button @click="handleExport" :loading="exportLoading">
					<el-icon>
						<Download />
					</el-icon>
					<span>Export</span>
				</el-button>
			</div>
		</div>

		<!-- Search and Filter Area -->
		<el-card class="mb-6 rounded-md filter_card">
			<template #default>
				<div class="">
					<el-form ref="searchFormRef" :model="searchForm" class="actionsSearch-form">
						<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
							<div class="space-y-2">
								<label class="text-sm font-medium text-primary-500">
									Tool ID or Tool Name
								</label>
								<el-input
									v-model="searchForm.keyword"
									placeholder="Enter Action ID or Action Name"
									clearable
									class="w-full rounded-md"
									@change="handleSearch"
								>
									<template #prefix>
										<el-icon>
											<Search />
										</el-icon>
									</template>
								</el-input>
							</div>

							<div class="space-y-2">
								<label class="text-sm font-medium text-primary-500">Type</label>
								<el-select
									v-model="searchForm.type"
									placeholder="Select Type"
									clearable
									class="w-full rounded-md"
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
					</el-form>
				</div>
			</template>
		</el-card>

		<!-- Tabs Area -->
		<PrototypeTabs
			v-model="activeTab"
			:tabs="tabsConfig"
			type="adaptive"
			size="default"
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
import { Plus, Download, Search, Edit, Delete } from '@element-plus/icons-vue';
import CustomerPagination from '@/components/global/u-pagination/index.vue';
import ActionConfigDialog from '@/components/actionTools/ActionConfigDialog.vue';
import { PrototypeTabs, TabPane } from '@/components/PrototypeTabs';
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
/* Header title bar styles */
.actions-header {
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
	border-radius: 16px !important;
	padding: 4px 12px !important;
	font-size: 12px !important;
	font-weight: 500 !important;
}

.filter_card {
	border: 1px solid var(--primary-100);
}

/* 搜索表单样式 */
.actionsSearch-form :deep(.el-form-item) {
	margin-bottom: 0;
}

.actionsSearch-form :deep(.el-input__wrapper) {
	transition: all 0.2s;
}

.actionsSearch-form :deep(.el-input__wrapper:hover) {
	border-color: #9ca3af;
}

.actionsSearch-form :deep(.el-input__wrapper.is-focus) {
	border-color: #3b82f6;
	box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.actionsSearch-form :deep(.el-select__wrapper) {
	transition: all 0.2s;
}

.actionsSearch-form :deep(.el-select__wrapper:hover) {
	border-color: #9ca3af;
}

.actionsSearch-form :deep(.el-select__wrapper.is-focused) {
	border-color: #3b82f6;
	box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

/* 暗色主题样式 */
html.dark {
	/* 卡片和容器背景 */
	.filter_card {
		background: linear-gradient(to right, var(--primary-900), var(--primary-800)) !important;
	}

	.rounded-md {
		background-color: var(--black-400) !important;
		border: 1px solid var(--black-200) !important;
	}

	/* 搜索表单暗色主题 */
	.actionsSearch-form :deep(.el-input__wrapper) {
		background-color: #2d3748 !important;
		border: 1px solid var(--black-200) !important;
	}

	.actionsSearch-form :deep(.el-input__wrapper:hover) {
		border-color: var(--black-100) !important;
	}

	.actionsSearch-form :deep(.el-input__wrapper.is-focus) {
		border-color: var(--primary-500);
		box-shadow: 0 0 0 3px rgba(126, 34, 206, 0.2);
	}

	.actionsSearch-form :deep(.el-input__inner) {
		@apply text-white-100;
	}

	/* Select 暗色主题 */
	.actionsSearch-form :deep(.el-select__wrapper) {
		background-color: #2d3748 !important;
		border: 1px solid var(--black-200) !important;
	}

	.actionsSearch-form :deep(.el-select__wrapper:hover) {
		border-color: var(--black-100) !important;
	}

	.actionsSearch-form :deep(.el-select__wrapper.is-focused) {
		border-color: var(--primary-500);
		box-shadow: 0 0 0 3px rgba(126, 34, 206, 0.2);
	}

	.actionsSearch-form :deep(.el-select__selection) {
		@apply text-white-100;
	}

	.actionsSearch-form :deep(.el-select__placeholder) {
		color: var(--el-text-color-placeholder, #a8abb2);
	}

	/* 表格和分页暗色适配 */
	.customer-block {
		background-color: var(--black-400) !important;
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
