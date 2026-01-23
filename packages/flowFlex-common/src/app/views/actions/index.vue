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
					v-if="functionPermission(ProjectPermissionEnum.tool.create)"
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
						v-model="searchForm.search"
						placeholder="Enter Tool ID or Tool Name"
						clearable
						class="w-full"
						@change="handleSearch"
					/>
				</div>

				<div class="space-y-2">
					<label class="filter-label text-sm font-medium">Type</label>
					<el-select
						v-model="searchForm.actionType"
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
								<el-tag v-if="row.isAIGenerated" type="primary">
									<div class="flex items-center gap-1">✨ AI</div>
								</el-tag>
							</div>
						</template>
					</el-table-column>
					<el-table-column prop="actionType" label="Type" width="150">
						<template #default="{ row }">
							<el-tag type="primary">
								{{ ACTION_TYPE_MAPPING[row.actionType] }}
							</el-tag>
						</template>
					</el-table-column>
					<el-table-column v-if="isShowTools" label="Actions" width="100" fixed="right">
						<template #default="{ row }">
							<div class="flex items-center">
								<el-tooltip content="Edit" placement="top">
									<el-button
										v-if="functionPermission(ProjectPermissionEnum.tool.update)"
										type="primary"
										link
										@click="handleEdit(row)"
										:icon="Edit"
									/>
								</el-tooltip>
								<el-tooltip content="Change History" placement="top">
									<el-button
										v-if="functionPermission(ProjectPermissionEnum.tool.read)"
										type="primary"
										link
										@click="handleChangeHistory(row)"
									>
										<Icon icon="material-symbols:history" />
									</el-button>
								</el-tooltip>
								<el-tooltip content="Delete" placement="top">
									<el-button
										v-if="functionPermission(ProjectPermissionEnum.tool.delete)"
										type="danger"
										link
										@click="handleDelete(row)"
										:icon="Delete"
									/>
								</el-tooltip>
							</div>
						</template>
					</el-table-column>
				</el-table>
			</TabPane>

			<!-- My Action Tab -->
			<TabPane value="myAction">
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
								<el-tag v-if="row.isAIGenerated" type="primary">
									<div class="flex items-center gap-1">✨AI</div>
								</el-tag>
							</div>
						</template>
					</el-table-column>
					<el-table-column prop="actionType" label="Type" width="150">
						<template #default="{ row }">
							<el-tag type="primary">
								{{ ACTION_TYPE_MAPPING[row.actionType] }}
							</el-tag>
						</template>
					</el-table-column>
					<el-table-column label="Actions" width="100" fixed="right">
						<template #default="{ row }">
							<div class="flex items-center">
								<el-tooltip content="Edit" placement="top">
									<el-button
										v-if="functionPermission(ProjectPermissionEnum.tool.update)"
										type="primary"
										link
										@click="handleEdit(row)"
										:icon="Edit"
									/>
								</el-tooltip>
								<el-tooltip content="Change History" placement="top">
									<el-button
										v-if="functionPermission(ProjectPermissionEnum.tool.read)"
										type="primary"
										link
										@click="handleChangeHistory(row)"
									>
										<Icon icon="material-symbols:history" />
									</el-button>
								</el-tooltip>
								<el-tooltip content="Delete" placement="top">
									<el-button
										v-if="functionPermission(ProjectPermissionEnum.tool.delete)"
										type="danger"
										link
										@click="handleDelete(row)"
										:icon="Delete"
									/>
								</el-tooltip>
							</div>
						</template>
					</el-table-column>
				</el-table>
			</TabPane>
		</PrototypeTabs>

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

		<!-- Action Config Dialog -->
		<ActionConfigDialog
			ref="actionConfigDialogRef"
			:triggerSourceId="currentEditAction?.id"
			@save-success="onActionSave"
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
				<div class="flex justify-between items-center">
					<CustomerPagination
						:total="historyTotalElements"
						:limit="historyPageSize"
						:page="historyCurrentPage"
						:background="true"
						@pagination="handleHistoryLimitUpdate"
						@update:page="handleHistoryCurrentChange"
						@update:limit="handleHistoryPageUpdate"
					/>
					<el-button @click="closeChangeHistoryDialog">Close</el-button>
				</div>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted, markRaw, computed, useTemplateRef } from 'vue';
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
	getActionChangeHistory,
	ACTION_TYPE_MAPPING,
	type ActionChangeHistoryItem,
} from '@/apis/action';
import { ActionDefinition } from '#/action';
import { tableMaxHeight } from '@/settings/projectSetting';
import TableViewIcon from '@assets/svg/onboard/tavleView.svg';
import { functionPermission } from '@/hooks';
import { ProjectPermissionEnum, UserType } from '@/enums/permissionEnum';
import { useUserStoreWithOut } from '@/stores/modules/user';
import { useI18n } from '@/hooks/useI18n';

const usersStore = useUserStoreWithOut();
const { t } = useI18n();

// Reactive data
const loading = ref(false);
const exportLoading = ref(false);
const selectedActions = ref<any[]>([]);

// Action 弹窗相关状态
const actionConfigDialogRef = useTemplateRef('actionConfigDialogRef');
const currentEditAction = ref<ActionDefinition | null>(null);

// Change History 弹窗相关状态
const showChangeHistoryDialog = ref(false);
const changeHistoryLoading = ref(false);
const currentActionForHistory = ref<ActionDefinition | null>(null);
const changeHistoryData = ref<ActionChangeHistoryItem[]>([]);
const historyCurrentPage = ref(1);
const historyPageSize = ref(15);
const historyTotalElements = ref(0);

// Search form
const searchForm = ref({
	search: '',
	actionType: '',
});

// Tabs configuration
const activeTab = ref('tools');
const tabsConfig = ref([
	{ label: 'Tools', value: 'tools', icon: markRaw(TableViewIcon) },
	{ label: 'My Tools', value: 'myAction', icon: markRaw(TableViewIcon) },
]);

// Pagination
const pagination = ref({
	currentPage: 1,
	pageSize: 15,
	total: 0,
});

// Current page data
const actionsList = ref<ActionDefinition[]>([]);

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
		...Object.entries(ACTION_TYPE_MAPPING).map(([key, value]) => ({
			label: value,
			value: key,
		})),
	];
};

const handleCreateAction = () => {
	currentEditAction.value = null;
	actionConfigDialogRef.value?.open({
		forceEditable: true,
	});
};

const handleExport = async () => {
	try {
		// Show export loading
		exportLoading.value = true;
		// Call export API
		const response = await exportActions({
			...pagination.value,
			...searchForm.value,
			isTools: activeTab.value === 'tools',
		});

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

		ElMessage.success(t('sys.api.operationSuccess'));
	} catch (error) {
		console.error('Export failed:', error);
		ElMessage.error('Export failed. Please try again.');
	} finally {
		exportLoading.value = false;
	}
};

const handleSearch = async () => {
	pagination.value.currentPage = 1;
	await loadActionsList();
};

const handleTabChange = async (tabValue: string) => {
	pagination.value.currentPage = 1;
	await loadActionsList();
};

const handleSelectionChange = (selection: any[]) => {
	selectedActions.value = selection;
};

const handleEdit = async (row: ActionDefinition) => {
	currentEditAction.value = row;
	actionConfigDialogRef.value?.open({
		actionId: row.id,
		forceEditable: true,
	});
};

// Load change history with pagination
const loadChangeHistory = async () => {
	if (!currentActionForHistory.value?.id) {
		return;
	}

	try {
		changeHistoryLoading.value = true;

		// Call the real API to get change history
		const historyRes = await getActionChangeHistory(
			currentActionForHistory.value.id.toString(),
			{
				pageIndex: historyCurrentPage.value,
				pageSize: historyPageSize.value,
			}
		);

		if (historyRes.code === '200' && historyRes.success && historyRes?.data?.items) {
			// Debug: Log the actual data structure from backend
			console.log('Change History API Response:', historyRes.data.items);
			changeHistoryData.value = historyRes.data.items;
			historyTotalElements.value = historyRes.data.totalCount || 0;
		} else {
			// Fallback to empty array if no data
			changeHistoryData.value = [];
			historyTotalElements.value = 0;
			if (historyRes.msg) {
				ElMessage.warning(historyRes.msg);
			}
		}
	} catch (error) {
		console.error('Failed to load change history:', error);
		ElMessage.warning('Failed to load change history');
		changeHistoryData.value = [];
		historyTotalElements.value = 0;
	} finally {
		changeHistoryLoading.value = false;
	}
};

const handleChangeHistory = async (row: ActionDefinition) => {
	currentActionForHistory.value = row;
	showChangeHistoryDialog.value = true;

	if (!row.id) {
		ElMessage.error('Action ID is missing');
		return;
	}

	// Reset pagination
	historyCurrentPage.value = 1;
	await loadChangeHistory();
};

const closeChangeHistoryDialog = () => {
	showChangeHistoryDialog.value = false;
	currentActionForHistory.value = null;
	changeHistoryData.value = [];
	historyCurrentPage.value = 1;
	historyPageSize.value = 15;
	historyTotalElements.value = 0;
};

// Pagination handlers for change history
const handleHistoryPageUpdate = async (size: number) => {
	historyPageSize.value = size;
	historyCurrentPage.value = 1;
};

const handleHistoryCurrentChange = async (page: number) => {
	historyCurrentPage.value = page;
};

const handleHistoryLimitUpdate = async () => {
	await loadChangeHistory();
};

const handleDelete = async (row: ActionDefinition) => {
	try {
		await ElMessageBox.confirm(
			`Are you sure you want to delete action "${row.name}"? This action cannot be undone.`,
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
							const response = await deleteAction(row?.id || '');
							if (response.code === '200' && response.success) {
								ElMessage.success('Action deleted successfully');
								done();
								await loadActionsList();
							} else {
								ElMessage.error(response.msg || 'Failed to delete action');
								instance.confirmButtonLoading = false;
								instance.confirmButtonText = 'Delete';
							}
						} catch (error) {
							console.error('Failed to delete action:', error);
							ElMessage.error('Failed to delete action');
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
		// User cancelled
	}
};

const handlePageUpdate = async (size: number) => {
	pagination.value.pageSize = size;
	pagination.value.currentPage = 1;
};

const handleCurrentChange = async (page: number) => {
	pagination.value.currentPage = page;
};

const handleLimitUpdate = () => {
	loadActionsList();
};

// Action 保存成功回调
const onActionSave = async (actionResult) => {
	if (actionResult.id) {
		// 重新加载列表数据
		await loadActionsList();
	}
};

// Load Actions list from API
const loadActionsList = async () => {
	try {
		loading.value = true;
		// Call API
		const response = await getActionDefinitions({
			...pagination.value,
			...searchForm.value,
			isTools: activeTab.value === 'tools',
		});

		if (response.code === '200' && response.success) {
			actionsList.value = response.data.data || [];
			pagination.value.total = response.data.total || 0;
		} else {
			actionsList.value = [];
			pagination.value.total = 0;
			ElMessage.error(response.msg || 'Failed to load actions');
		}
	} catch (error) {
		console.error('Failed to load actions:', error);
		actionsList.value = [];
		pagination.value.total = 0;
		ElMessage.error('Failed to load actions');
	} finally {
		loading.value = false;
	}
};

const isShowTools = computed(() => {
	const userInfo = usersStore.getUserInfo?.userType;
	return userInfo == UserType.SystemAdmin || userInfo == UserType.TenantAdmin;
});

// Lifecycle
onMounted(() => {
	// Initialize data
	loadActionsList();
});
</script>

<style scoped lang="scss">
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
}
</style>
