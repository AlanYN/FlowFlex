<template>
	<el-dialog
		v-model="dialogTableVisible"
		:close-on-click-modal="false"
		title="Change History"
		:width="moreDialogWidth"
		@close="closeVisible"
		draggable
	>
		<el-table
			:data="tablePagesCount"
			:max-height="tableMaxHeight"
			class="w-full"
			border
			stripe
			row-key="id"
			v-loading="loading"
		>
			<el-table-column label="Type" width="200">
				<template #default="{ row }">
					<el-tag
						:type="getTagType(row.operationType)"
						class="flex items-center w-fit"
						size="small"
					>
						<span class="mr-1 text-xs">
							{{ getOperationTypeInfo(row.operationType)?.icon }}
						</span>
						{{ row.operationTypeDisplayName || row.operationType }}
					</el-tag>
				</template>
			</el-table-column>

			<el-table-column label="Changes" min-width="450">
				<template #default="{ row }">
					<div class="text-sm">
						<component :is="getChangeComponent(row)" :change="row" />
					</div>
				</template>
			</el-table-column>

			<el-table-column label="Updated By" width="150">
				<template #default="{ row }">
					<span
						v-if="row.operatorName && row.operatorName.trim() !== ''"
						class="text-gray-900 dark:text-white-100 truncate"
						:title="row.operatorName"
					>
						{{ row.operatorName }}
					</span>
					<span v-else class="text-gray-400 dark:text-gray-500 text-sm italic">
						System
					</span>
				</template>
			</el-table-column>

			<el-table-column label="Date & Time" width="200">
				<template #default="{ row }">
					<div class="flex items-center text-gray-600 dark:text-gray-400 text-sm">
						<el-icon class="mr-1 text-xs">
							<Clock />
						</el-icon>
						{{ row.operationTimeDisplay || formatDateTime(row.operationTime) }}
					</div>
				</template>
			</el-table-column>

			<template #empty>
				<div class="py-8 text-gray-500 dark:text-gray-400 text-center">
					<el-icon class="text-4xl mb-2">
						<Document />
					</el-icon>
					<div class="text-lg mb-2">No change records found</div>
					<div class="text-sm">No changes recorded for this item yet.</div>
				</div>
			</template>
		</el-table>

		<CustomerPagination
			:total="total"
			:limit="paginationPages.pageSize"
			:page="paginationPages.pageIndex"
			:background="true"
			@pagination="handleCurrentChange"
			@update:page="handlePageUpdate"
			@update:limit="handleLimitUpdate"
		/>
	</el-dialog>
</template>

<script setup lang="ts">
import { ref, h } from 'vue';
import { getChangeLogs } from '@/apis/global';
import CustomerPagination from '@/components/global/u-pagination/index.vue';
import { timeZoneConvert } from '@/hooks/time';
import { Document, Clock } from '@element-plus/icons-vue';
import {
	projectTenMinutesSsecondsDate,
	moreDialogWidth,
	defaultStr,
	tableMaxHeight,
} from '@/settings/projectSetting';
import { WFEMoudels } from '@/enums/appEnum';
import { getOperationTypeInfo } from '@/apis/ow/change-log';

const loading = ref(false);
const total = ref(0);
const paginationPages = ref({
	pageIndex: 1,
	pageSize: 15,
});

const dialogTableVisible = ref(false);
const historyId = ref('');
const historyType = ref<WFEMoudels>();
const tablePagesCount = ref([]);

const showHistoryTable = (id: string, type: WFEMoudels) => {
	dialogTableVisible.value = true;
	historyId.value = id;
	historyType.value = type;
	getHisttoryTable();
};

const closeVisible = () => {
	historyId.value = '';
	historyType.value = undefined;
	dialogTableVisible.value = false;
	paginationPages.value = { pageIndex: 1, pageSize: 15 };
	tablePagesCount.value = [];
	total.value = 0;
};

const getHisttoryTable = async () => {
	try {
		loading.value = true;
		const res = await getChangeLogs(historyId.value, {
			type: historyType.value,
			...paginationPages.value,
		});
		tablePagesCount.value = res.data?.items || [];
		total.value = res.data.totalCount;
	} finally {
		loading.value = false;
	}
};

const handlePageUpdate = (val: number) => {
	paginationPages.value.pageIndex = val;
};
const handleLimitUpdate = (val) => {
	paginationPages.value.pageSize = val;
};
const handleCurrentChange = () => {
	getHisttoryTable();
};

const getTagType = (operationType: string): string => {
	if (!operationType) return 'info';

	// 根据实际的operationType进行判断
	if (operationType.includes('Create')) return 'success';
	if (operationType.includes('Update')) return 'warning';
	if (operationType.includes('Delete')) return 'danger';
	if (operationType.includes('Complete') || operationType.includes('Success')) return 'success';
	if (operationType.includes('Failed') || operationType.includes('Error')) return 'danger';

	return 'info';
};

// 动态组件渲染
const getChangeComponent = (change: any) => {
	const operationType = change.operationType;

	// 根据实际的operationType进行匹配
	if (operationType?.includes('Update') && change.beforeData && change.afterData) {
		return () => renderFieldChange(change);
	}
	if (operationType?.includes('Upload') || operationType?.includes('File')) {
		return () => renderFileUpload(change);
	}
	if (operationType?.includes('Task')) {
		return () => renderTaskChange(change);
	}
	if (operationType?.includes('Action') || change.operationStatus === 'Failed') {
		return () => renderActionChange(change);
	}

	return () => renderDefault(change);
};

// 渲染函数
const renderFieldChange = (change: any) => {
	// 对于问卷类型的变更，直接使用 operationDescription
	if (change.operationType === 'QuestionnaireUpdate') {
		return h(
			'div',
			{ class: 'bg-blue-50 dark:bg-blue-900/20 p-3 rounded-xl border-l-4 border-blue-400' },
			[
				h(
					'div',
					{ class: 'font-semibold text-blue-800 dark:text-blue-200 mb-2' },
					change.operationTitle || 'Questionnaire Update'
				),
				h(
					'div',
					{ class: 'text-gray-700 dark:text-gray-300 text-sm whitespace-pre-line' },
					change.operationDescription || 'Questionnaire has been updated'
				),
			]
		);
	}

	if (!change.beforeData && !change.afterData) {
		return h(
			'div',
			{ class: 'text-gray-700 dark:text-gray-300' },
			change.operationTitle || 'Field changed'
		);
	}

	// 解析JSON数据以便比较
	let beforeObj, afterObj;
	try {
		beforeObj = change.beforeData ? JSON.parse(change.beforeData) : {};
		afterObj = change.afterData ? JSON.parse(change.afterData) : {};
	} catch (e) {
		return h('div', { class: 'text-gray-700 dark:text-gray-300' }, change.operationDescription);
	}

	// 显示变更的字段
	const changedFields = change.changedFields || [];

	return h(
		'div',
		{ class: 'bg-yellow-50 dark:bg-yellow-900/20 p-3 rounded-xl border-l-4 border-yellow-400' },
		[
			h(
				'div',
				{ class: 'font-semibold text-yellow-800 dark:text-yellow-200 mb-2' },
				change.operationTitle || 'Field Update'
			),

			// 显示每个变更的字段
			...changedFields.map((fieldName) =>
				h('div', { class: 'mb-2' }, [
					h(
						'div',
						{ class: 'font-medium text-sm text-gray-800 dark:text-gray-200 mb-1' },
						fieldName
					),
					h('div', { class: 'flex items-center text-xs mb-1' }, [
						h(
							'span',
							{ class: 'text-red-600 dark:text-red-400 font-medium mr-2' },
							'Before:'
						),
						h(
							'span',
							{
								class: 'bg-red-100 dark:bg-red-900/30 px-2 py-1 rounded-xl text-red-800 dark:text-red-200',
							},
							String(beforeObj[fieldName] || 'N/A')
						),
					]),
					h('div', { class: 'flex items-center text-xs' }, [
						h(
							'span',
							{ class: 'text-green-600 dark:text-green-400 font-medium mr-2' },
							'After:'
						),
						h(
							'span',
							{
								class: 'bg-green-100 dark:bg-green-900/30 px-2 py-1 rounded-xl text-green-800 dark:text-green-200',
							},
							String(afterObj[fieldName] || 'N/A')
						),
					]),
				])
			),

			// 如果没有具体字段信息，显示描述
			changedFields.length === 0 &&
				h('div', { class: 'text-gray-600 text-sm' }, change.operationDescription),
		]
	);
};

const renderFileUpload = (change: any) => {
	const fileInfo = parseFileInfo(change.extendedData);

	return h(
		'div',
		{
			class: 'bg-cyan-50 dark:bg-cyan-900/20 p-2 rounded-xl text-xs border-l-4 border-cyan-400',
		},
		[
			h('div', { class: 'flex items-center' }, [
				h('span', { class: 'mr-2 text-cyan-600' }, '📁'),
				h('span', { class: 'font-medium' }, fileInfo.fileName),
				fileInfo.fileSize &&
					h(
						'span',
						{ class: 'ml-2 text-gray-500' },
						`(${formatFileSize(fileInfo.fileSize)})`
					),
			]),
			h('div', { class: 'text-gray-600 mt-1 text-xs' }, change.operationDescription),
		]
	);
};

const renderTaskChange = (change: any) => {
	const isCreate = change.operationType?.includes('Create');
	const bgClass = isCreate
		? 'bg-blue-50 dark:bg-blue-900/20 border-blue-400'
		: 'bg-green-50 dark:bg-green-900/20 border-green-400';

	return h('div', { class: `${bgClass} p-3 rounded-xl border-l-4` }, [
		h(
			'div',
			{ class: 'font-semibold mb-2 text-gray-800 dark:text-gray-200' },
			change.operationTitle
		),
		h('div', { class: 'text-gray-600 text-sm' }, change.operationDescription),
	]);
};

const renderActionChange = (change: any) => {
	const isSuccess = change.operationStatus === 'Success';
	const bgClass = isSuccess
		? 'bg-green-50 dark:bg-green-900/20 border-green-400'
		: 'bg-red-50 dark:bg-red-900/20 border-red-400';

	return h('div', { class: `${bgClass} p-3 rounded-xl border-l-4` }, [
		h(
			'div',
			{ class: 'font-semibold mb-2 text-gray-800 dark:text-gray-200' },
			change.operationTitle
		),
		h('div', { class: 'text-gray-600 text-sm mb-2' }, change.operationDescription),
		h('div', { class: 'flex items-center text-xs' }, [
			h('span', { class: 'text-gray-500 mr-2' }, 'Status:'),
			h(
				'span',
				{
					class: isSuccess
						? 'bg-green-100 text-green-800 px-2 py-1 rounded'
						: 'bg-red-100 text-red-800 px-2 py-1 rounded',
				},
				change.operationStatusDisplayName || change.operationStatus
			),
		]),
	]);
};

const renderDefault = (change: any) => {
	return h('div', { class: 'space-y-2' }, [
		h('div', { class: 'font-medium text-gray-800 dark:text-gray-200' }, change.operationTitle),
		change.operationDescription &&
			h('div', { class: 'text-gray-600 text-sm' }, change.operationDescription),
	]);
};

// 简化的工具函数
const formatDateTime = (dateString: string): string => {
	if (!dateString) return defaultStr;
	try {
		return timeZoneConvert(dateString, false, projectTenMinutesSsecondsDate);
	} catch {
		return dateString;
	}
};

const parseFileInfo = (extendedData: any) => {
	if (!extendedData) return { fileName: 'Unknown file', fileSize: null };

	try {
		const info = typeof extendedData === 'string' ? JSON.parse(extendedData) : extendedData;
		return {
			fileName:
				info.FileName ||
				info.fileName ||
				info.filename ||
				info.TaskName ||
				info.ChecklistName ||
				'Unknown file',
			fileSize: info.FileSize || info.fileSize || info.size,
		};
	} catch {
		return { fileName: 'Unknown file', fileSize: null };
	}
};

const formatFileSize = (bytes: number): string => {
	if (bytes === 0) return '0 Bytes';
	const k = 1024;
	const sizes = ['Bytes', 'KB', 'MB', 'GB'];
	const i = Math.floor(Math.log(bytes) / Math.log(k));
	return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
};

defineExpose({
	showHistoryTable,
});
</script>

<style scoped lang="scss">
.avatar-icon {
	width: 22px;
	height: 22px;
	background-color: hsl(0 0% 96.1%);
	@apply flex items-center justify-center mr-2
	rounded-full bg-gradient-to-r 
	from-[var(--primary-500)] to-[var(--primary-reverse-500)] text-white;

	svg {
		transform: scale(0.6);
	}
}
</style>
