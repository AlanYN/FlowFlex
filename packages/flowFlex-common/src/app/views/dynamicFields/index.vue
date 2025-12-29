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
					stripe
				>
					<template #empty>
						<slot name="empty">
							<el-empty description="No Data" :image-size="50" />
						</slot>
					</template>
					<el-table-column type="selection" fixed="left" width="50" align="center" />
					<el-table-column min-width="140" prop="displayName" label="Name" />
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
							<el-button link @click="handleEdit(row)">
								<Icon icon="lucide-pencil" />
							</el-button>
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

		<!-- 新建字段弹窗 -->
		<el-dialog
			v-model="dialogVisible"
			:width="500"
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

			<el-form ref="formRef" :model="formData" :rules="formRules" label-position="top">
				<el-form-item label="Field Name" prop="fieldName">
					<el-input
						v-model="formData.fieldName"
						placeholder="e.g., Customer Email"
						clearable
						class="w-full"
					/>
				</el-form-item>

				<el-form-item label="Description" prop="description">
					<el-input
						v-model="formData.description"
						type="textarea"
						:rows="3"
						placeholder="Describe what this field is used for..."
						clearable
						class="w-full"
					/>
				</el-form-item>

				<el-form-item label="Field Type" prop="dataType">
					<el-select
						v-model="formData.dataType"
						placeholder="Select field type"
						class="w-full"
						filterable
					>
						<el-option
							v-for="type in fieldsTypeEnum"
							:key="type.key"
							:label="type.value"
							:value="type.key"
						/>
					</el-select>
				</el-form-item>
			</el-form>

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
import { onMounted, ref, reactive } from 'vue';
import { ElMessage } from 'element-plus';
import PageHeader from '@/components/global/PageHeader/index.vue';
import { Plus, Download } from '@element-plus/icons-vue';
import {
	dynamicFieldList,
	createDynamicField,
	updateDynamicField,
} from '@/apis/global/dyanmicField';
import DynamicFilter from './components/dynamicFilter.vue';
import { projectTenMinutesSsecondsDate, tableMaxHeight } from '@/settings/projectSetting';
import { fieldsTypeEnum, propertyTypeEnum } from '@/enums/appEnum';
import { useI18n } from '@/hooks/useI18n';
import { timeZoneConvert } from '@/hooks/time';
import CustomerPagination from '@/components/global/u-pagination/index.vue';

import { DynamicList, CreateDynamicFieldParams } from '#/dynamic';

const { t } = useI18n();

const loading = ref(false);

// 选中的项目
const selectedItems = ref<DynamicList[]>([]);

// 搜索参数
const searchParams = ref<any>({});

// 弹窗相关状态
const dialogVisible = ref(false);
const formRef = ref();
const saving = ref(false);

// 表单数据 - 使用创建专用类型
const formData = reactive<CreateDynamicFieldParams>({
	fieldName: '',
	displayName: '',
	description: '',
	dataType: propertyTypeEnum.SingleLineText,
});

// 表单验证规则
const formRules = {
	fieldName: [{ required: true, message: 'Field Name is required', trigger: 'blur' }],
	dataType: [{ required: true, message: 'Field Type is required', trigger: 'change' }],
};

const handleExport = async () => {
	try {
		loading.value = true;

		let exportMessage = '';

		// 如果有选中的数据，优先导出选中的数据
		if (selectedItems.value.length > 0) {
			const selectedIds = selectedItems.value.map((item) => item.id).join(',');
			// TODO: 调用导出接口，传入选中的 IDs
			console.log('Export selected IDs:', selectedIds);
			exportMessage = `Selected ${selectedItems.value.length} items exported successfully`;
		} else {
			// 没有选中数据时，按当前搜索条件导出全部
			// TODO: 调用导出接口，传入搜索参数
			console.log('Export all with params:', searchParams.value);
			exportMessage = 'All data exported successfully';
		}

		// 模拟导出（实际项目中替换为真实的导出 API 调用）
		// const response = await exportDynamicFields(exportParams);
		// 创建下载链接...

		ElMessage.success(exportMessage);
	} finally {
		loading.value = false;
	}
};

// 打开新建字段弹窗
const handleNewField = () => {
	resetForm();
	dialogVisible.value = true;
};

// 取消弹窗
const handleCancel = () => {
	dialogVisible.value = false;
	resetForm();
};

// 重置表单
const resetForm = () => {
	formData.displayName = '';
	formData.fieldName = '';
	formData.description = '';
	formData.dataType = propertyTypeEnum.SingleLineText;
	handleFieldId.value = '';
	if (formRef.value) {
		formRef.value.clearValidate();
	}
};

// 保存字段
const handleSave = async () => {
	if (!formRef.value) return;

	try {
		await formRef.value.validate();
		saving.value = true;

		// TODO: 调用创建字段的 API
		const res = handleFieldId.value
			? await updateDynamicField(handleFieldId.value, {
					...formData,
					displayName: formData.fieldName,
			  })
			: await createDynamicField({
					...formData,
					displayName: formData.fieldName,
			  });
		if (res.code == '200') {
			ElMessage.success(t('sys.api.operationSuccess'));
		} else {
			ElMessage.success(res.msg || t('sys.api.operationFailed'));
		}

		dialogVisible.value = false;
		resetForm();

		// 重新加载列表
		dynamicList();
	} catch (error) {
		console.error('Save field error:', error);
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

const handleFieldId = ref('');
const handleEdit = (row: DynamicList) => {
	handleFieldId.value = row.id;
	formData.fieldName = row.fieldName;
	formData.description = row.description;
	formData.dataType = row.dataType;
	dialogVisible.value = true;
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
