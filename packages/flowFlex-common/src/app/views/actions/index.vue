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
					<span>
						Export
						{{ selectedActions.length > 0 ? `(${selectedActions.length})` : 'All' }}
					</span>
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
		<el-card class="mb-6">
			<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
				<div class="space-y-2">
					<label class="filter-label text-sm font-medium">Tool ID or Tool Name</label>
					<el-input
						v-model="searchForm.keyword"
						placeholder="Enter Tool ID or Tool Name"
						clearable
						class="w-full"
						@change="handleSearch"
					/>
				</div>

				<div class="space-y-2">
					<label class="filter-label text-sm font-medium">Type</label>
					<el-select
						v-model="searchForm.type"
						placeholder="Select Type"
						clearable
						class="w-full"
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
		</el-card>

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
				<div class="">
					<el-table
						:data="actionsList"
						style="width: 100%"
						@selection-change="handleSelectionChange"
						:max-height="tableMaxHeight"
						v-loading="loading"
						border
					>
						<el-table-column type="selection" width="55" />
						<el-table-column prop="actionCode" label="Tool ID" width="120" />
						<el-table-column prop="name" label="Tool Name" min-width="200">
							<template #default="{ row }">
								<div class="flex items-center gap-2">
									<span>{{ row.name }}</span>
									<span
										v-if="row.isAIGenerated"
										class="el-tag el-tag--primary el-tag--small el-tag--light ai-tag rounded-md"
									>
										<span class="el-tag__content">
											<div class="flex items-center gap-1">
												<span class="ai-sparkles">✨</span>
												AI
											</div>
										</span>
									</span>
								</div>
							</template>
						</el-table-column>
						<el-table-column prop="actionType" label="Type" width="150">
							<template #default="{ row }">
								<el-tag class="type-tag">
									{{ getActionTypeName(row.actionType) }}
								</el-tag>
							</template>
						</el-table-column>
						<el-table-column label="Actions" width="160" fixed="right">
							<template #default="{ row }">
								<div class="action-buttons">
									<el-tooltip content="Edit" placement="top">
										<el-button type="primary" link @click="handleEdit(row)">
											<el-icon>
												<Edit />
											</el-icon>
										</el-button>
									</el-tooltip>
									<el-tooltip content="Change History" placement="top">
										<el-button
											type="info"
											link
											@click="handleChangeHistory(row)"
										>
											<i class="el-icon mr-2 change-history-icon">
												<svg
													xmlns="http://www.w3.org/2000/svg"
													width="16"
													height="16"
													viewBox="0 0 24 24"
												>
													<path
														fill="currentColor"
														d="M12 5q-1.725 0-3.225.8T6.25 8H8q.425 0 .713.288T9 9t-.288.713T8 10H4q-.425 0-.712-.288T3 9V5q0-.425.288-.712T4 4t.713.288T5 5v1.35q1.275-1.6 3.113-2.475T12 3q1.875 0 3.513.7t2.862 1.925q1.05 1.05 1.725 2.425t.85 2.95q.05.425-.25.7t-.725.275-.712-.275-.338-.7q-.175-1.15-.675-2.15t-1.3-1.8q-.95-.95-2.212-1.5T12 5m-7.9 8.15q.4-.05.725.15t.45.6q.55 2.025 2.125 3.375t3.65 1.625q.525.075.75.388t.225.662q0 .4-.262.725t-.738.275q-2.775-.325-4.887-2.137T3.3 14.325q-.125-.425.125-.775t.675-.4M13 11.6l1.2 1.2q.35.35.313.738t-.313.662-.662.313-.738-.313l-1.5-1.5q-.15-.15-.225-.337T11 11.975V8q0-.425.288-.712T12 7t.713.288T13 8zM18.775 24q-.35 0-.612-.225t-.338-.575l-.15-.7q-.3-.125-.562-.262t-.538-.338l-.725.225q-.325.1-.638-.025t-.487-.4l-.2-.35q-.175-.3-.125-.65t.325-.575l.55-.475q-.05-.325-.05-.65t.05-.65l-.55-.475q-.275-.225-.325-.562t.125-.638l.225-.375q.175-.275.475-.4t.625-.025l.725.225q.275-.2.538-.337t.562-.263l.15-.725q.075-.35.338-.562t.612-.213h.4q.35 0 .613.225t.337.575l.15.7q.3.125.575.287t.525.363l.675-.225q.35-.125.675 0t.5.425l.2.35q.175.3.125.65t-.325.575l-.55.475q.05.325.05.625t-.05.625l.55.475q.275.225.325.563t-.125.637l-.225.375q-.175.275-.475.4t-.625.025l-.725-.225q-.275.2-.538.337t-.562.263l-.15.725q-.075.35-.337.563t-.613.212zm.2-3q.825 0 1.412-.587T20.976 19t-.587-1.412T18.975 17t-1.412.588T16.975 19t.588 1.413 1.412.587"
													/>
												</svg>
											</i>
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
				<div class="">
					<el-table
						:data="actionsList"
						style="width: 100%"
						@selection-change="handleSelectionChange"
						:max-height="tableMaxHeight"
						v-loading="loading"
						border
					>
						<el-table-column type="selection" width="55" />
						<el-table-column prop="actionCode" label="Tool ID" width="120" />
						<el-table-column prop="name" label="Tool Name" min-width="200">
							<template #default="{ row }">
								<div class="flex items-center gap-2">
									<span>{{ row.name }}</span>
									<span
										v-if="row.isAIGenerated"
										class="el-tag el-tag--primary el-tag--small el-tag--light ai-tag rounded-md"
									>
										<span class="el-tag__content">
											<div class="flex items-center gap-1">
												<span class="ai-sparkles">✨</span>
												AI
											</div>
										</span>
									</span>
								</div>
							</template>
						</el-table-column>
						<el-table-column prop="actionType" label="Type" width="150">
							<template #default="{ row }">
								<el-tag class="type-tag">
									{{ getActionTypeName(row.actionType) }}
								</el-tag>
							</template>
						</el-table-column>
						<el-table-column label="Actions" width="160" fixed="right">
							<template #default="{ row }">
								<div class="action-buttons">
									<el-tooltip content="Edit" placement="top">
										<el-button type="primary" link @click="handleEdit(row)">
											<el-icon>
												<Edit />
											</el-icon>
										</el-button>
									</el-tooltip>
									<el-tooltip content="Change History" placement="top">
										<el-button
											type="info"
											link
											@click="handleChangeHistory(row)"
										>
											<i class="el-icon mr-2 change-history-icon">
												<svg
													xmlns="http://www.w3.org/2000/svg"
													width="16"
													height="16"
													viewBox="0 0 24 24"
												>
													<path
														fill="currentColor"
														d="M12 5q-1.725 0-3.225.8T6.25 8H8q.425 0 .713.288T9 9t-.288.713T8 10H4q-.425 0-.712-.288T3 9V5q0-.425.288-.712T4 4t.713.288T5 5v1.35q1.275-1.6 3.113-2.475T12 3q1.875 0 3.513.7t2.862 1.925q1.05 1.05 1.725 2.425t.85 2.95q.05.425-.25.7t-.725.275-.712-.275-.338-.7q-.175-1.15-.675-2.15t-1.3-1.8q-.95-.95-2.212-1.5T12 5m-7.9 8.15q.4-.05.725.15t.45.6q.55 2.025 2.125 3.375t3.65 1.625q.525.075.75.388t.225.662q0 .4-.262.725t-.738.275q-2.775-.325-4.887-2.137T3.3 14.325q-.125-.425.125-.775t.675-.4M13 11.6l1.2 1.2q.35.35.313.738t-.313.662-.662.313-.738-.313l-1.5-1.5q-.15-.15-.225-.337T11 11.975V8q0-.425.288-.712T12 7t.713.288T13 8zM18.775 24q-.35 0-.612-.225t-.338-.575l-.15-.7q-.3-.125-.562-.262t-.538-.338l-.725.225q-.325.1-.638-.025t-.487-.4l-.2-.35q-.175-.3-.125-.65t.325-.575l.55-.475q-.05-.325-.05-.65t.05-.65l-.55-.475q-.275-.225-.325-.562t.125-.638l.225-.375q.175-.275.475-.4t.625-.025l.725.225q.275-.2.538-.337t.562-.263l.15-.725q.075-.35.338-.562t.612-.213h.4q.35 0 .613.225t.337.575l.15.7q.3.125.575.287t.525.363l.675-.225q.35-.125.675 0t.5.425l.2.35q.175.3.125.65t-.325.575l-.55.475q.05.325.05.625t-.05.625l.55.475q.275.225.325.563t-.125.637l-.225.375q-.175.275-.475.4t-.625.025l-.725-.225q-.275.2-.538.337t-.562.263l-.15.725q-.075.35-.337.563t-.613.212zm.2-3q.825 0 1.412-.587T20.976 19t-.587-1.412T18.975 17t-1.412.588T16.975 19t.588 1.413 1.412.587"
													/>
												</svg>
											</i>
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

		<!-- Change History Dialog -->
		<el-dialog
			v-model="showChangeHistoryDialog"
			title="Change History"
			width="900px"
			:close-on-click-modal="false"
			:destroy-on-close="true"
		>
			<template #header>
				<div class="w-[850px] truncate text-2xl font-bold">Change History</div>
			</template>

			<div v-loading="changeHistoryLoading">
				<div
					v-if="!changeHistoryLoading && changeHistoryData.length === 0"
					class="text-center py-8 text-gray-500"
				>
					No change history found for this action.
				</div>

				<div v-else class="space-y-4">
					<div
						v-for="(item, index) in changeHistoryData"
						:key="index"
						class="change-history-item border border-gray-200 rounded-lg p-4"
					>
						<div class="flex items-start gap-4">
							<!-- Type Icon -->
							<div
								class="flex-shrink-0 w-10 h-10 flex items-center justify-center rounded-full"
								:class="{
									'bg-green-100 text-green-600':
										getOperationStatus(item) === 'Success',
									'bg-red-100 text-red-600':
										getOperationStatus(item) === 'Failed',
									'bg-yellow-100 text-yellow-600':
										getOperationStatus(item) === 'Warning',
								}"
							>
								<i
									v-if="getOperationType(item).includes('Create')"
									class="text-green-600 text-sm"
								>
									✓
								</i>
								<i
									v-else-if="getOperationType(item).includes('Update')"
									class="text-orange-600 text-sm"
								>
									📝
								</i>
								<i
									v-else-if="getOperationType(item).includes('Delete')"
									class="text-red-600 text-sm"
								>
									🗑
								</i>
								<i v-else class="text-blue-600 text-sm">📋</i>
							</div>

							<!-- Content -->
							<div class="flex-1 min-w-0">
								<div class="flex items-center gap-2 mb-2">
									<h4 class="text-lg font-medium text-gray-900 truncate">
										{{ getOperationTitle(item) }}
									</h4>
									<el-tag
										:type="
											getOperationStatus(item) === 'Success'
												? 'success'
												: getOperationStatus(item) === 'Failed'
												? 'danger'
												: 'warning'
										"
										size="small"
									>
										{{ getOperationStatus(item) }}
									</el-tag>
								</div>
								<p class="text-sm text-gray-600 mb-3">
									{{ getOperationDescription(item) }}
								</p>
							</div>

							<!-- Meta Info -->
							<div class="flex-shrink-0 text-right">
								<div class="text-sm font-medium text-gray-900 mb-1">
									{{ getOperationBy(item) }}
								</div>
								<div class="text-xs text-gray-500">
									<i class="el-icon-time mr-1"></i>
									{{ getOperationTime(item) }}
								</div>
							</div>
						</div>
					</div>
				</div>
			</div>

			<template #footer>
				<div class="flex justify-between">
					<div class="text-sm text-gray-500">
						{{ changeHistoryData.length }} Results
						<span class="ml-4">Show: 15</span>
					</div>
					<div class="flex justify-end">
						<el-button @click="closeChangeHistoryDialog">Close</el-button>
					</div>
				</div>
			</template>
		</el-dialog>
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
	getActionChangeHistory,
	ActionType,
	ACTION_TYPE_MAPPING,
	type ActionChangeHistoryItem,
} from '@/apis/action';
import { ActionDefinition, ActionQueryRequest } from '#/action';
import { tableMaxHeight } from '@/settings/projectSetting';
import TableViewIcon from '@assets/svg/onboard/tavleView.svg';

// Use flexible type for change history items to handle different API response structures
type ChangeHistoryItem = ActionChangeHistoryItem | any;

// Reactive data
const loading = ref(false);
const exportLoading = ref(false);
const selectedActions = ref<any[]>([]);

// Action 弹窗相关状态
const actionEditorVisible = ref(false);
const actionInfo = ref(null);
const editActionLoading = ref(false);
const currentEditAction = ref<ActionDefinition | null>(null);

// Change History 弹窗相关状态
const showChangeHistoryDialog = ref(false);
const changeHistoryLoading = ref(false);
const currentActionForHistory = ref<ActionDefinition | null>(null);
const changeHistoryData = ref<ChangeHistoryItem[]>([]);

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

// Safe getter functions for Change History data
const getOperationType = (item: any) => {
	return item.type || item.operationType || 'Unknown';
};

const getOperationStatus = (item: any) => {
	if (item.status) return item.status;
	if (item.operationStatus === 'Success' || item.operationStatus === 1) return 'Success';
	if (item.operationStatus === 'Failed' || item.operationStatus === 2) return 'Failed';
	return 'Warning';
};

const getOperationTitle = (item: any) => {
	return item.changes || item.operationTitle || item.operationDescription || 'No title available';
};

const getOperationDescription = (item: any) => {
	return item.description || item.operationDescription || 'No description available';
};

const getOperationBy = (item: any) => {
	return item.updatedBy || item.operatorName || 'System';
};

const getOperationTime = (item: any) => {
	if (item.dateTime) return item.dateTime;
	if (item.operationTime) {
		// Format the date-time if it's in ISO format
		const date = new Date(item.operationTime);
		return date.toLocaleString();
	}
	return 'Unknown time';
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

		// Build query parameters
		let params: ActionQueryRequest = {};
		let exportMessage = '';

		// If there are selected items, prioritize exporting selected data
		if (selectedActions.value.length > 0) {
			// Export selected data - use selected item IDs, converted to comma-separated string
			const selectedActionIds = selectedActions.value.map((item) => item.id).join(',');
			params = {
				actionIds: selectedActionIds,
				pageIndex: 1,
				pageSize: 10000, // Large page to ensure all matching data is retrieved
			};

			exportMessage = `Export completed successfully (${selectedActions.value.length} items selected)`;
		} else {
			// No selected data, export based on current search conditions
			params = {
				pageIndex: 1,
				pageSize: 10000, // Large page to ensure all matching data is retrieved
			};

			// Add search conditions
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

			exportMessage = 'Filtered data exported successfully';
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

		// Set file name, including timestamp and export type
		const timestamp = new Date()
			.toISOString()
			.slice(0, 19)
			.replace(/[-:]/g, '')
			.replace('T', '_');
		const fileNameSuffix = selectedActions.value.length > 0 ? 'Selected' : 'Filtered';
		link.download = `Actions_${fileNameSuffix}_${timestamp}.xlsx`;

		// Trigger download
		document.body.appendChild(link);
		link.click();
		document.body.removeChild(link);
		window.URL.revokeObjectURL(url);

		ElMessage.success(exportMessage);
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

const handleChangeHistory = async (row: ActionDefinition) => {
	currentActionForHistory.value = row;
	showChangeHistoryDialog.value = true;

	if (!row.id) {
		ElMessage.error('Action ID is missing');
		return;
	}

	try {
		changeHistoryLoading.value = true;

		// Call the real API to get change history
		const historyRes = await getActionChangeHistory(row.id.toString(), {
			pageIndex: 1,
			pageSize: 50, // Get more records to show full history
		});

		if (historyRes.code === '200' && historyRes.success && historyRes?.data?.items) {
			// Debug: Log the actual data structure from backend
			console.log('Change History API Response:', historyRes.data.items);
			changeHistoryData.value = historyRes.data.items;
		} else {
			// Fallback to empty array if no data
			changeHistoryData.value = [];
			if (historyRes.msg) {
				ElMessage.warning(historyRes.msg);
			}
		}
	} catch (error) {
		console.error('Failed to load change history:', error);
		ElMessage.warning('Failed to load change history');
		changeHistoryData.value = [];
	} finally {
		changeHistoryLoading.value = false;
	}
};

const closeChangeHistoryDialog = () => {
	showChangeHistoryDialog.value = false;
	currentActionForHistory.value = null;
	changeHistoryData.value = [];
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
			color: var(--el-text-color-regular);
			margin-bottom: 4px;
		}

		.assignment-date {
			font-size: 12px;
			color: var(--el-text-color-secondary);
		}
	}
}

.action-buttons {
	display: flex;
	gap: 8px;
	justify-content: center;
}

.change-history-icon {
	color: var(--primary-500) !important;
}

.type-tag {
	background-color: var(--el-color-primary-light-9) !important;
	border-color: var(--el-color-primary-light-5) !important;
	color: var(--el-color-primary) !important;
	padding: 4px 12px !important;
	font-size: 12px !important;
	font-weight: 500 !important;
}

/* AI标签样式 */
.ai-tag {
	background-color: var(--el-color-primary-light-9) !important;
	border-color: var(--el-color-primary-light-5) !important;
	color: var(--el-color-primary) !important;
	border-radius: 4px !important;
	padding: 2px 6px !important;
	font-size: 11px !important;
	font-weight: 500 !important;
	display: inline-flex !important;
	align-items: center !important;
	gap: 2px !important;
}

.ai-sparkles {
	font-size: 10px !important;
}
/* Change History 弹窗样式 */
.change-history-item {
	@apply transition-all duration-200 hover:shadow-md;
	background: var(--el-bg-color);
}

.change-history-item:hover {
	border-color: var(--primary-300) !important;
	transform: translateY(-1px);
}

/* 暗色主题样式 */
html.dark {
	/* 标签样式在暗色主题下的适配 */
	.assignments-list .assignment-item .assignment-name {
		color: var(--white-100) !important;
	}

	.assignments-list .assignment-item .assignment-date {
		color: var(--gray-300) !important;
	}

	/* Change History 暗色主题样式 */
	.change-history-item {
		@apply bg-black-400 border-black-200;
		background: var(--el-bg-color-page);
	}

	.change-history-item:hover {
		border-color: var(--primary-400) !important;
	}

	.change-history-item h4 {
		@apply text-white-100;
	}

	.change-history-item p {
		@apply text-gray-300;
	}

	.change-history-item .text-gray-900 {
		@apply text-white-100;
	}

	.change-history-item .text-gray-500 {
		@apply text-gray-400;
	}

	/* Change History 图标暗色主题样式 */
	.change-history-icon {
		color: var(--primary-400) !important;
	}
}
</style>
