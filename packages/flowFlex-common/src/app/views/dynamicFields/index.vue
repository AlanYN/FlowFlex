<template>
	<div>
		<PageHeader
			title="Dynamic Fields"
			description="Manage custom fields for your workflows and cases"
		>
			<template #actions>
				<el-button
					class="page-header-btn page-header-btn-secondary"
					@click="handleExport"
					:loading="loading"
					:disabled="loading"
					:icon="Download"
				>
					Export {{ selectedItems.length > 0 ? `(${selectedItems.length})` : 'All' }}
				</el-button>
				<el-button
					class="page-header-btn page-header-btn-primary"
					type="primary"
					@click="handleNewField"
					:disabled="loading"
					:loading="loading"
					:icon="Plus"
				>
					Add New Field
				</el-button>
			</template>
		</PageHeader>
		<div>
			<DynamicFilter @search="handleFilterSearch" />
			<div class="">
				<el-table
					:data="dynamicFieldListData"
					@selection-change="handleSelectionChange"
					v-loading="loading"
					:max-height="tableMaxHeight"
					row-key="id"
					:border="true"
					show-overflow-tooltip
				>
					<template #empty>
						<slot name="empty">
							<el-empty description="No Data" :image-size="50" />
						</slot>
					</template>
					<el-table-column type="selection" fixed="left" width="50" />
					<el-table-column min-width="140" prop="displayName" label="Name">
						<template #default="{ row }">
							<div class="flex items-center gap-1">
								<el-icon
									v-if="row.isSystemDefine"
									class="text-primary flex-shrink-0 mr-2"
									title="System Field"
								>
									<Lock />
								</el-icon>
								<span class="truncate">{{ row.displayName }}</span>
							</div>
						</template>
					</el-table-column>
					<el-table-column min-width="200" prop="description" label="Description" />
					<el-table-column min-width="100" prop="dataType" label="Type">
						<template #default="{ row }">
							{{ fieldsTypeEnum.find((item) => item.key == row.dataType)?.value }}
						</template>
					</el-table-column>
					<el-table-column
						min-width="150"
						prop="createDate"
						label="Created Time"
						:formatter="
							(dateStr) =>
								timeZoneConvert(
									dateStr?.createDate || '',
									false,
									projectTenMinutesSsecondsDate
								)
						"
					/>
					<el-table-column min-width="100" prop="createBy" label="Created By" />
					<el-table-column
						min-width="150"
						prop="modifyDate"
						label="Updated Time"
						:formatter="
							(dateStr) =>
								timeZoneConvert(
									dateStr?.modifyDate || '',
									false,
									projectTenMinutesSsecondsDate
								)
						"
					/>
					<el-table-column min-width="100" prop="modifyBy" label="Updated By" />

					<el-table-column label="Actions" width="80" fixed="right" align="center">
						<template #default="{ row }">
							<el-button v-if="!row.isSystemDefine" link @click="handleEdit(row)">
								<Icon icon="lucide-pencil" />
							</el-button>
							<el-button
								v-if="!row.isSystemDefine"
								link
								@click="hanDelete(row)"
								type="danger"
								:icon="Delete"
							/>
						</template>
					</el-table-column>
				</el-table>

				<!-- 分页 -->
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

		<!-- 新建/编辑字段弹窗 -->
		<el-dialog
			v-model="dialogVisible"
			:width="600"
			destroy-on-close
			:show-close="true"
			:close-on-click-modal="false"
			draggable
			@close="handleCancel"
		>
			<template #header>
				<div class="dialog-header">
					<h2 class="dialog-title">
						{{ handleFieldId ? 'Edit Dynamic Field' : 'Add New Dynamic Field' }}
					</h2>
					<p class="dialog-subtitle">
						{{
							handleFieldId
								? 'Update the dynamic field settings.'
								: 'Create a new dynamic field for your workflows and cases.'
						}}
					</p>
				</div>
			</template>

			<DynamicFieldForm ref="formRef" :is-edit="!!handleFieldId" />

			<template #footer>
				<div class="dialog-footer">
					<el-button @click="handleCancel" :disabled="saving">Cancel</el-button>
					<el-button type="primary" @click="handleSave" :loading="saving">
						{{ handleFieldId ? 'Save Changes' : 'Add Field' }}
					</el-button>
				</div>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import PageHeader from '@/components/global/PageHeader/index.vue';
import { Plus, Download, Delete, Lock } from '@element-plus/icons-vue';
import {
	dynamicFieldList,
	createDynamicField,
	updateDynamicField,
	exportDynamicFields,
	deleteDynamicField,
} from '@/apis/global/dyanmicField';
import DynamicFilter from './components/dynamicFilter.vue';
import DynamicFieldForm from './components/DynamicFieldForm.vue';
import { projectTenMinutesSsecondsDate, tableMaxHeight } from '@/settings/projectSetting';
import { fieldsTypeEnum } from '@/enums/appEnum';
import { useI18n } from '@/hooks/useI18n';
import { timeZoneConvert } from '@/hooks/time';
import CustomerPagination from '@/components/global/u-pagination/index.vue';

import { DynamicList } from '#/dynamic';

const { t } = useI18n();

const loading = ref(false);

// 选中的项目
const selectedItems = ref<DynamicList[]>([]);

// 搜索参数
const searchParams = ref<any>({});

// 弹窗相关状态
const dialogVisible = ref(false);
const formRef = ref<InstanceType<typeof DynamicFieldForm>>();
const saving = ref(false);
const handleFieldId = ref('');

const handleExport = async () => {
	try {
		loading.value = true;

		// 构建导出参数
		let exportParams: any = {};
		let exportMessage = '';

		// 如果有选中的数据，优先导出选中的数据
		if (selectedItems.value.length > 0) {
			const selectedIds = selectedItems.value.map((item) => item.id).join(',');
			exportParams = {
				ids: selectedIds,
				pageSize: 10000,
			};
			exportMessage = `Selected ${selectedItems.value.length} items exported successfully`;
		} else {
			// 没有选中数据时，按当前搜索条件导出全部
			exportParams = {
				...searchParams.value,
				pageSize: 10000,
			};
			exportMessage = 'Filtered data exported successfully';
		}

		// 调用导出接口
		const response = await exportDynamicFields(exportParams);

		// 创建下载链接
		const blob = new Blob([response.data], {
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
		link.download = `DynamicFields_${fileNameSuffix}_${timestamp}.xlsx`;

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

// 打开新建字段弹窗
const handleNewField = () => {
	handleFieldId.value = '';
	dialogVisible.value = true;
};

// 取消弹窗
const handleCancel = () => {
	dialogVisible.value = false;
	handleFieldId.value = '';
};

// 保存字段
const handleSave = async () => {
	if (!formRef.value) return;

	try {
		await formRef.value.validate();
		saving.value = true;

		const formData = formRef.value.getFormData();
		const res = handleFieldId.value
			? await updateDynamicField(handleFieldId.value, formData)
			: await createDynamicField(formData);

		if (res.code == '200') {
			ElMessage.success(t('sys.api.operationSuccess'));
			dialogVisible.value = false;
			handleFieldId.value = '';
			dynamicList();
		} else {
			ElMessage.error(res.msg || t('sys.api.operationFailed'));
		}
	} finally {
		saving.value = false;
	}
};

const dynamicFieldListData = ref<DynamicList[]>([]);
const currentPage = ref(1);
const pageSize = ref(15);
const totalElements = ref(0);
const dynamicList = async () => {
	try {
		loading.value = true;
		const res = await dynamicFieldList({
			pageIndex: currentPage.value,
			pageSize: pageSize.value,
			...searchParams.value,
		});
		if (res.code == '200') {
			dynamicFieldListData.value = res.data.items;
			totalElements.value = res.data.totalCount;
		}
	} finally {
		loading.value = false;
	}
};

const handlePageUpdate = async (size: number) => {
	pageSize.value = size;
};

const handleCurrentChange = async (page: number) => {
	currentPage.value = page;
};

const handleLimitUpdate = async () => {
	await dynamicList();
};

// 处理表格选择变化
const handleSelectionChange = (selection: DynamicList[]) => {
	selectedItems.value = selection;
};

// 处理筛选搜索
const handleFilterSearch = (params: any) => {
	searchParams.value = params;
	dynamicList();
};

// 编辑字段
const handleEdit = (row: DynamicList) => {
	handleFieldId.value = row.id;
	dialogVisible.value = true;
	// 等待弹窗渲染后设置表单数据
	setTimeout(() => {
		formRef.value?.setFormData(row);
	}, 0);
};

const hanDelete = (row: DynamicList) => {
	ElMessageBox.confirm(
		`Are you sure you want to delete the field "${row.displayName}"? This action cannot be undone.`,
		'⚠️ Confirm Field Deletion',
		{
			confirmButtonText: 'Delete Field',
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
						const res = await deleteDynamicField(row.id);

						if (res.code === '200') {
							ElMessage.success(t('sys.api.operationSuccess'));
							dynamicList();
							done();
						} else {
							ElMessage.error(res.msg || t('sys.api.operationFailed'));
							instance.confirmButtonLoading = false;
							instance.confirmButtonText = 'Delete Field';
						}
					} catch (error) {
						instance.confirmButtonLoading = false;
						instance.confirmButtonText = 'Delete Field';
					}
				} else {
					done();
				}
			},
		}
	);
};

onMounted(() => {
	dynamicList();
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

.dialog-footer {
	display: flex;
	justify-content: flex-end;
	gap: 12px;
	padding: 16px 0 0 0;
}

/* 暗色主题样式 */
html.dark {
	.dialog-title {
		color: var(--white-100);
	}

	.dialog-subtitle {
		color: var(--gray-300);
	}
}
</style>
