<template>
	<div class="wfe-global-block-bg">
		<!-- 统一的头部卡片 -->
		<div
			class="case-component-header rounded-xl"
			:class="{ expanded: isExpanded }"
			@click="toggleExpanded"
		>
			<div class="flex justify-between">
				<div>
					<div class="flex items-center">
						<el-icon
							class="case-component-expand-icon text-lg mr-2"
							:class="{ rotated: isExpanded }"
						>
							<ArrowRight />
						</el-icon>
						<h3 class="case-component-title">
							Documents
							<span v-if="documentIsRequired" class="text-red-300 ml-1">*</span>
						</h3>
					</div>
					<div class="case-component-subtitle">
						{{ documents.length }}
						{{ documents.length === 1 ? 'file' : 'files' }} uploaded
					</div>
				</div>
				<div class="case-component-actions">
					<el-button
						:icon="Download"
						v-if="systemId"
						@click.stop="importFormIntegration"
						:disabled="disabled"
						:loading="importLoading"
						type="primary"
					>
						Import from Integration
					</el-button>
					<el-button
						:icon="Upload"
						@click.stop="triggerUpload"
						:disabled="disabled"
						type="primary"
					>
						Upload Files
					</el-button>
				</div>
			</div>
		</div>

		<!-- 可折叠文档内容 -->
		<el-collapse-transition>
			<div v-show="isExpanded" class="space-y-4 p-4">
				<!-- 文件上传区域 -->
				<div>
					<el-upload
						ref="uploadRef"
						class="upload-demo rounded-xl"
						drag
						:auto-upload="false"
						:on-change="handleFileChange"
						:before-upload="handleBeforeUpload"
						:on-exceed="handleExceed"
						:on-error="handleUploadError"
						:show-file-list="false"
						:disabled="disabled"
						multiple
						:limit="5"
						accept=".pdf,.docx,.doc,.jpg,.jpeg,.png,.xlsx,.xls,.msg,.eml"
					>
						<div class="flex flex-col items-center justify-center">
							<el-icon class="text-4xl text-gray-400 mb-2">
								<Upload />
							</el-icon>
							<div class="text-lg text-gray-600 dark:text-gray-300">
								Drag and drop files here
							</div>
							<div class="text-sm text-gray-500 dark:text-gray-400 mt-1">
								or
								<em class="text-primary-500 hover:text-primary-600 cursor-pointer">
									click to browse
								</em>
							</div>
							<div class="text-xs text-gray-400 dark:text-gray-500 mt-2">
								Maximum number of files that can be uploaded at once: 5
							</div>
							<div class="text-xs text-gray-400 dark:text-gray-500">
								Supports: PDF, DOCX, DOC, JPG, JPEG, PNG, XLSX, XLS, MSG, EML (Max
								50MB per file)
							</div>
						</div>
					</el-upload>
				</div>

				<!-- 上传进度 -->
				<div v-if="uploadProgress.length > 0" class="space-y-2">
					<h4 class="text-sm font-medium text-gray-700 dark:text-gray-300">
						Uploading...
					</h4>
					<div
						v-for="progress in uploadProgress"
						:key="progress.uid"
						class="flex items-center space-x-3 p-2 bg-gray-50 dark:bg-black-200 rounded"
					>
						<el-icon class="text-primary-500">
							<Upload />
						</el-icon>
						<div class="flex-1">
							<div class="text-sm font-medium">{{ progress.name }}</div>
							<el-progress
								:percentage="progress.percentage"
								:status="progress.error ? 'exception' : undefined"
								:show-text="false"
							/>
						</div>
						<span class="text-xs text-gray-500">{{ progress.percentage }}%</span>
						<el-tooltip
							v-if="progress.error"
							:content="progress.error"
							placement="top"
							effect="dark"
						>
							<el-icon class="text-red-500 cursor-pointer text-lg">
								<WarningFilled />
							</el-icon>
						</el-tooltip>
					</div>
				</div>

				<!-- 下载进度 -->
				<div v-if="downloadProgress.length > 0" class="space-y-2">
					<h4 class="text-sm font-medium text-gray-700 dark:text-gray-300">
						Downloading...
					</h4>
					<div
						v-for="progress in downloadProgress"
						:key="progress.uid"
						class="flex items-center space-x-3 p-2 bg-gray-50 dark:bg-black-200 rounded"
					>
						<el-tooltip
							v-if="progress.error"
							:content="progress.error"
							placement="top"
							effect="dark"
						>
							<el-icon class="text-red-500 cursor-pointer text-lg">
								<WarningFilled />
							</el-icon>
						</el-tooltip>
						<el-icon v-else class="text-green-500">
							<Download />
						</el-icon>
						<div class="flex-1">
							<div class="text-sm font-medium">{{ progress.name }}</div>
							<el-progress
								:percentage="progress.percentage"
								:status="progress.error ? 'exception' : undefined"
								:show-text="false"
							/>
						</div>
						<span class="text-xs text-gray-500">{{ progress.percentage }}%</span>

						<el-button
							v-if="progress.taskId"
							text
							size="small"
							@click="handleCancelDownload(progress)"
							class="text-gray-500 hover:text-red-500"
						>
							<el-icon><Close /></el-icon>
						</el-button>
					</div>
				</div>

				<!-- 已上传文件列表 -->
				<div v-if="loading" class="text-center py-8">
					<el-icon class="text-2xl animate-spin">
						<Loading />
					</el-icon>
					<p class="text-gray-500 dark:text-gray-400 mt-2">Loading documents...</p>
				</div>

				<div v-else-if="documents.length > 0" class="space-y-2">
					<h4 class="text-sm font-medium text-gray-700 dark:text-gray-300">Files</h4>
					<el-table
						:data="documents"
						stripe
						:max-height="tableMaxHeight"
						class="w-full"
						border
					>
						<el-table-column label="File Name" min-width="200" fixed="left">
							<template #default="{ row }">
								<div class="flex items-center">
									<el-icon class="text-primary mr-2 flex-shrink-0">
										<Document />
									</el-icon>
									<span
										class="text-sm font-medium text-gray-900 dark:text-white-100 table-cell-content"
										:title="row.originalFileName"
									>
										{{ row.originalFileName }}
									</span>
								</div>
							</template>
						</el-table-column>

						<el-table-column label="Size" width="100">
							<template #default="{ row }">
								<div class="text-sm table-cell-content">
									{{ formatFileSize(row.fileSize) }}
								</div>
							</template>
						</el-table-column>

						<el-table-column label="Form" width="200">
							<template #default="{ row }">
								<div class="text-sm table-cell-content" :title="row?.source">
									{{ row?.source }}
								</div>
							</template>
						</el-table-column>

						<el-table-column label="Uploaded By" width="150">
							<template #default="{ row }">
								<div class="text-sm table-cell-content" :title="row.uploadedByName">
									{{ row.uploadedByName }}
								</div>
							</template>
						</el-table-column>

						<el-table-column label="Date" width="150">
							<template #default="{ row }">
								<div class="text-sm table-cell-content" :title="row.uploadedDate">
									{{ row.uploadedDate }}
								</div>
							</template>
						</el-table-column>

						<el-table-column label="Actions" width="80" fixed="right">
							<template #default="{ row }">
								<div class="flex items-center space-x-2">
									<el-button
										type="primary"
										link
										:disabled="viewDocumentIds.includes(row.id)"
										:loading="viewDocumentIds.includes(row.id)"
										@click="handleViewDocument(row)"
										:icon="View"
									/>
									<el-button
										type="danger"
										link
										:disabled="viewDocumentIds.includes(row.id) || disabled"
										@click="handleDeleteDocument(row.id)"
										:icon="Delete"
									/>
								</div>
							</template>
						</el-table-column>
					</el-table>
				</div>

				<!-- 空状态 -->
				<div v-else class="text-center py-8 text-gray-500 dark:text-gray-400">
					<el-icon class="text-4xl mb-2">
						<Folder />
					</el-icon>
					<p>No documents uploaded yet</p>
					<p class="text-xs mt-1">Upload your first document to get started</p>
				</div>
			</div>
		</el-collapse-transition>

		<!-- 文件预览组件 -->
		<vuePreviewFile
			:fileUrl="fileUrl"
			:type="fileType"
			:isShow="perviewFileShow"
			:offloading="offloading"
			@close-office="closeOffice"
			@rendered-office="offloading = false"
		/>

		<!-- Import Attachments Dialog -->
		<ImportAttachmentsDialog
			v-model:visible="importDialogVisible"
			:attachments="importFileList"
			:action-errors="importActionErrors"
			:loading="importLoading"
			@close="handleImportDialogClose"
			@start-download="handleStartDownload"
		/>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';

import {
	Upload,
	Document,
	Loading,
	View,
	Delete,
	Folder,
	ArrowRight,
	Download,
	WarningFilled,
	Close,
} from '@element-plus/icons-vue';
import {
	uploadOnboardingFile,
	getOnboardingFilesByStage,
	deleteOnboardingFile,
	previewOnboardingFile,
} from '@/apis/ow/onboarding';
import {
	getCaseAttachmentIntegration,
	importDownLoadFiles,
	queryImportProgress,
	cancelImportDownload,
} from '@/apis/integration';
import { timeZoneConvert } from '@/hooks/time';
import { DocumentItem, StageComponentData } from '#/onboard';
import { IntegrationAttachment } from '#/integration';
import vuePreviewFile from '@/components/previewFile/previewFile.vue';
import ImportAttachmentsDialog from './ImportAttachmentsDialog.vue';
import { useI18n } from '@/hooks/useI18n';
import { tableMaxHeight } from '@/settings/projectSetting';
import { formatFileSize, getMimeType } from '@/utils/format';

const { t } = useI18n();
// Props
interface Props {
	onboardingId: string;
	stageId?: string;
	component: StageComponentData;
	disabled?: boolean;
	documentIsRequired?: boolean;
	systemId?: string;
	entityId?: string;
}

const props = defineProps<Props>();

// 响应式数据
const documents = ref<DocumentItem[]>([]);
const loading = ref(false);
const uploadProgress = ref<
	{
		uid: string;
		name: string;
		percentage: number;
		error?: string;
	}[]
>([]);
const downloadProgress = ref<
	{
		uid: string;
		name: string;
		percentage: number;
		error?: string;
		taskId?: string; // 用于取消下载
	}[]
>([]);

// 轮询定时器
const pollingTimers = ref<Map<string, number>>(new Map());
const uploadRef = ref();

// 折叠状态
const isExpanded = ref(true); // 默认展开

// 切换展开状态
const toggleExpanded = () => {
	isExpanded.value = !isExpanded.value;
};

// 预览相关状态
const fileUrl = ref('');
const fileType = ref('');
const offloading = ref(false);
const perviewFileShow = ref(false);

// 事件定义
const emit = defineEmits<{
	documentUploaded: [document: DocumentItem];
	documentDeleted: [documentId: string];
}>();

// 获取文档列表（带loading，用于初始加载）
const fetchDocuments = async () => {
	if (!props.onboardingId) return;

	loading.value = true;
	try {
		// 如果有stageId，按阶段获取文件；否则获取所有文件
		if (!props.stageId) return;
		const response = await getOnboardingFilesByStage(props.onboardingId, props.stageId);

		if (response.code === '200') {
			documents.value =
				response.data.map((item: DocumentItem) => {
					return {
						...item,
						uploadedDate: timeZoneConvert(item?.uploadedDate || ''),
					};
				}) || [];
		} else {
			documents.value = [];
			ElMessage.error(response.msg || 'Failed to load documents');
		}
	} catch (error) {
		documents.value = [];
	} finally {
		loading.value = false;
	}
};

// 静默刷新文档列表（增量更新，不显示loading，保持接口返回的顺序）
const refreshDocumentsSilently = async () => {
	if (!props.onboardingId || !props.stageId) return;

	try {
		const response = await getOnboardingFilesByStage(props.onboardingId, props.stageId);

		if (response.code === '200') {
			const newDocuments =
				response.data.map((item: DocumentItem) => {
					return {
						...item,
						uploadedDate: timeZoneConvert(item?.uploadedDate || ''),
					};
				}) || [];

			// 直接使用接口返回的顺序，确保顺序与后端一致
			// 这样可以避免增量更新时打乱顺序
			documents.value = newDocuments;
		}
	} catch (error) {
		console.error('Error refreshing documents silently:', error);
		// 静默刷新失败时不显示错误消息，保持当前状态
	}
};

// 文件上传处理
const triggerUpload = () => {
	uploadRef.value?.$el?.querySelector('input[type="file"]')?.click();
};

const handleBeforeUpload = (file: File) => {
	// 检查文件大小（50MB限制）
	const maxSize = 50 * 1024 * 1024; // 10MB
	if (file.size > maxSize) {
		ElMessage.error('File size cannot exceed 50MB');
		return false;
	}

	if (file.name.includes('.msg')) {
		return true;
	}

	// 检查文件类型
	const allowedTypes = [
		'application/pdf',
		'application/msword',
		'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
		'image/jpeg',
		'image/png',
		'application/vnd.ms-excel',
		'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
		'application/vnd.ms-outlook', // .msg
		'message/rfc822', // .eml
	];

	if (!allowedTypes.includes(file.type)) {
		ElMessage.error('Unsupported file type');
		return false;
	}

	return true;
};

// 文件超出数量限制处理
const handleExceed = () => {
	ElMessage.warning('Upload up to 5 files at a time');
};

// 文件上传错误处理
const handleUploadError = () => {
	ElMessage.error('Upload failed, please try again');
};

// 处理文件选择变化
const handleFileChange = async (file: any) => {
	if (!file.raw) return;

	// 验证文件
	if (!handleBeforeUpload(file.raw)) {
		return;
	}

	try {
		// 添加到进度列表
		uploadProgress.value.push({
			uid: file.uid,
			name: file.name,
			percentage: 0,
		});

		// 构建上传参数，根据curl命令结构
		const uploadParams = {
			name: 'formFile', // 表单字段名（与curl一致）
			file: file.raw, // 文件对象
			filename: file.raw.name, // 文件名
			data: {
				category: 'Document', // 文档类别
				description: `${file.raw.name} uploaded via customer portal`, // 文件描述
				stageId: props.stageId, // stage ID
			},
		};

		// 调用上传API，带进度回调
		const response = await uploadOnboardingFile(
			props.onboardingId,
			uploadParams,
			(progressEvent: any) => {
				// 实时更新上传进度
				const existingIndex = uploadProgress.value.findIndex((p) => p.uid === file.uid);
				if (existingIndex >= 0 && progressEvent.total > 0) {
					uploadProgress.value[existingIndex].percentage = Math.round(
						(progressEvent.loaded * 100) / progressEvent.total
					);
				}
			}
		);

		// 从进度列表中移除
		uploadProgress.value = uploadProgress.value.filter((p) => p.uid !== file.uid);

		if (response.data?.code === '200') {
			ElMessage.success(`${file.name} uploaded successfully`);

			// 静默刷新文档列表（增量更新）
			await refreshDocumentsSilently();

			// 触发事件
			emit('documentUploaded', response.data?.data);
		} else {
			ElMessage.error(response.data?.msg || `Failed to upload ${file.name}`);
		}
	} catch (error) {
		console.error('Upload error:', error);

		// 从进度列表中移除
		uploadProgress.value = uploadProgress.value.filter((p) => p.uid !== file.uid);
	}
};

// 文档操作
const viewDocumentIds = ref<string[]>([]);
const handleViewDocument = async (document: DocumentItem) => {
	try {
		// 获取文件扩展名
		const fileExt = document.originalFileName.split('.').pop()?.toLowerCase() || '';
		fileType.value = fileExt;
		viewDocumentIds.value.push(document.id);
		offloading.value = true;
		// 调用API获取文件内容
		const res = await previewOnboardingFile(props.onboardingId, document.id);

		perviewFileShow.value = true;
		// doc、msg、eml文件不支持预览，直接下载
		if (fileExt === 'doc' || fileExt === 'msg' || fileExt === 'eml') {
			downloadFile(res, document);
			perviewFileShow.value = false;
		} else {
			// 支持预览的文件类型，使用正确的MIME类型
			const mimeType = document?.contentType || getMimeType(fileExt);
			const blob = new Blob([res], { type: mimeType });
			fileUrl.value = URL.createObjectURL(blob);
		}
	} finally {
		offloading.value = false;
		viewDocumentIds.value = viewDocumentIds.value.filter((id) => id !== document.id);
	}
};

// 关闭预览
const closeOffice = () => {
	perviewFileShow.value = false;
	offloading.value = false;
	fileUrl.value = '';
	fileType.value = '';
};

// 下载文件
const downloadFile = (res: any, file: DocumentItem) => {
	// 获取文件扩展名并设置正确的MIME类型
	const fileExt = file.originalFileName.split('.').pop()?.toLowerCase() || '';
	const mimeType = file?.contentType || getMimeType(fileExt);
	const blob = new Blob([res], { type: mimeType });
	const link = document.createElement('a');
	link.download = file.originalFileName;
	link.href = URL.createObjectURL(blob);
	document.body.appendChild(link);
	link.click();
	window.URL.revokeObjectURL(link.href);
	document.body.removeChild(link);
};

const handleDeleteDocument = async (documentId: string) => {
	try {
		await ElMessageBox.confirm(
			'Are you sure you want to delete this document?',
			'Delete Confirmation',
			{
				confirmButtonText: 'Confirm',
				cancelButtonText: 'Cancel',
				type: 'warning',
			}
		);

		// 调用删除API
		const response = await deleteOnboardingFile(props.onboardingId, documentId);

		if (response.code === '200') {
			ElMessage.success('Document deleted successfully');
			// 静默刷新文档列表（增量更新）
			await refreshDocumentsSilently();
			emit('documentDeleted', documentId);
		} else {
			ElMessage.error(response.msg || 'Failed to delete document');
		}
	} catch (error) {
		if (error !== 'cancel') {
			console.error('Error deleting document:', error);
		}
	}
};

const vailComponent = () => {
	try {
		if (props?.documentIsRequired && documents?.value?.length <= 0) {
			ElMessage.warning('Please upload at least one document');
			return false;
		}
		return true;
	} catch {
		return true;
	}
};

const importLoading = ref(false);
const importFileList = ref<IntegrationAttachment[]>([]);
const importDialogVisible = ref(false);
const importActionErrors = ref<
	Array<{ actionName: string; errorMessage: string; moduleName: string; integrationName: string }>
>([]);

const importFormIntegration = async () => {
	try {
		if (!props?.systemId) return;
		importDialogVisible.value = true; // Open dialog on success
		importLoading.value = true;
		const res = await getCaseAttachmentIntegration({
			systemId: props?.systemId || '',
			entityId: props?.entityId || '',
		});
		if (res?.code == '200') {
			// Process the new API response structure with actionExecutions array
			const actionExecutions = res?.data?.actionExecutions || [];
			const allAttachments: IntegrationAttachment[] = [];
			const actionErrors: Array<{
				actionName: string;
				errorMessage: string;
				moduleName: string;
				integrationName: string;
			}> = [];

			// Iterate through all action executions and collect attachments
			actionExecutions.forEach((execution: any) => {
				const hasAttachments =
					execution.attachments &&
					Array.isArray(execution.attachments) &&
					execution.attachments.length > 0;

				if (hasAttachments) {
					// Add all attachments from successful execution
					allAttachments.push(
						...execution.attachments.map((item) => {
							return {
								...item,
								actionName: execution.actionName,
							};
						})
					);
				} else if (execution.errorMessage) {
					// If action failed (has error and no attachments), collect error info separately
					actionErrors.push({
						...execution,
						actionName: execution?.actionName,
						errorMessage: execution.errorMessage,
						moduleName: execution.moduleName,
						integrationName: execution.integrationName,
					});
				}
			});

			importFileList.value = allAttachments;
			importActionErrors.value = actionErrors;
		} else {
			ElMessage.error(res?.msg || t('sys.api.operationFailed'));
		}
	} finally {
		importLoading.value = false;
	}
};

const handleImportDialogClose = () => {
	importDialogVisible.value = false;
	importFileList.value = [];
	importActionErrors.value = [];
};

// Handle start download from dialog
const handleStartDownload = async (attachments: IntegrationAttachment[]) => {
	if (attachments.length === 0) return;
	try {
		// 调用importDownLoadFiles接口开始下载
		const files = attachments.map((att) => ({
			downLoadLink: att.downloadLink,
			fileName: att.fileName,
			source: att.integrationName,
		}));

		const res = await importDownLoadFiles(props.onboardingId, {
			stageId: props.stageId!,
			files,
		});
		if (res.code == '200') {
			// 立即查询一次进度来初始化 downloadProgress
			await checkAndStartPolling();
			refreshDocumentsSilently();
		} else {
			ElMessage.error(res?.msg || t('sys.api.operationFailed'));
		}
	} catch (error: any) {
		console.error('Failed to start download:', error);
	}
};

// 检查是否有进行中的下载任务，并启动轮询
const checkAndStartPolling = async () => {
	if (!props.stageId) return;

	// 检查是否已经有轮询在运行，避免重复启动
	const pollingKey = `polling-${props.stageId}`;
	if (pollingTimers.value.has(pollingKey)) return;

	try {
		const response = await queryImportProgress(props.onboardingId, {
			stageId: props.stageId,
		});

		if (response?.code === '200' && Array.isArray(response.data)) {
			const tasks = response.data;

			// 遍历所有任务，收集所有进行中的文件
			const allInProgressItems: any[] = [];

			tasks.forEach((task: any) => {
				if (task.items && Array.isArray(task.items)) {
					task.items.forEach((item: any) => {
						if (item.status !== 'completed') {
							allInProgressItems.push({
								uid: item.itemId,
								name: item.fileName,
								percentage: item.progressPercentage || 0,
								error: item.status === 'failed' ? item.errorMessage : undefined,
								taskId: task.taskId,
							});
						}
					});
				}
			});

			// 如果有进行中的任务，初始化 downloadProgress 并启动轮询
			if (allInProgressItems.length > 0) {
				downloadProgress.value = allInProgressItems;
				startPollingProgress(props.stageId!);
			}
		}
	} catch (error) {
		console.error('Error checking download progress:', error);
	}
};

// 开始轮询查询进度
const startPollingProgress = (stageId: string) => {
	const pollInterval = 2000; // 每2秒轮询一次
	const pollingKey = `polling-${stageId}`;

	// 如果已经在轮询，不重复启动
	if (pollingTimers.value.has(pollingKey)) return;

	const timer = setInterval(async () => {
		try {
			const response = await queryImportProgress(props.onboardingId, { stageId });
			if (response?.code !== '200') {
				// 停止轮询
				stopPolling(pollingKey);
				// 更新所有文件为错误状态
				downloadProgress.value.forEach((progress) => {
					progress.error = response?.msg || 'Failed to query progress';
					progress.percentage = 100;
				});
				return;
			}

			const tasks = response.data;

			// 更新进度 - 遍历所有任务的 items
			if (Array.isArray(tasks)) {
				// 收集所有任务中的所有 items（包含 taskId）
				const allItems: any[] = [];
				tasks.forEach((task: any) => {
					if (task.items && Array.isArray(task.items)) {
						task.items.forEach((item: any) => {
							allItems.push({
								...item,
								taskId: task.taskId, // 保存 taskId 用于取消操作
							});
						});
					}
				});

				// 过滤出未完成的项
				const inProgressItems = allItems.filter((item) => item.status !== 'completed');

				// 如果接口返回的未完成文件数量与当前下载列表不同，说明有变化
				if (inProgressItems.length !== downloadProgress.value.length) {
					// 如果数量减少，说明有文件完成了，刷新文件列表
					if (inProgressItems.length < downloadProgress.value.length) {
						refreshDocumentsSilently();
					}

					// 重新构建下载进度列表
					downloadProgress.value = inProgressItems.map((item) => ({
						uid: item.itemId,
						name: item.fileName,
						percentage: item.progressPercentage || 0,
						error: item.status === 'Failed' ? item.errorMessage : undefined,
						taskId: item.taskId,
					}));
				} else {
					// 数量相同，只更新进度和状态
					downloadProgress.value = downloadProgress.value.map((progress) => {
						const item = allItems.find((i: any) => i.itemId === progress.uid);
						if (item) {
							return {
								...progress,
								percentage: item.progressPercentage || 0,
								error: item.status === 'Failed' ? item.errorMessage : undefined,
								taskId: item.taskId,
							};
						}
						return progress;
					});
				}
			}

			// 如果所有下载都完成，停止轮询
			if (downloadProgress.value.length == 0) {
				stopPolling(pollingKey);
				ElMessage.success('All files imported successfully');
				// 静默刷新文档列表（增量更新）
				await refreshDocumentsSilently();
			}
		} catch (error) {
			stopPolling(pollingKey);

			// 更新所有文件为错误状态
			downloadProgress.value.forEach((progress) => {
				progress.error = 'Failed to query progress';
				progress.percentage = 100;
			});
		}
	}, pollInterval);

	pollingTimers.value.set(pollingKey, timer as any);
};

// 停止轮询
const stopPolling = (key: string) => {
	const timer = pollingTimers.value.get(key);
	if (timer) {
		clearInterval(timer);
		pollingTimers.value.delete(key);
	}
};

// 取消下载
const handleCancelDownload = async (progress: any) => {
	if (!progress.taskId) return;

	try {
		// 显示确认弹窗
		await ElMessageBox.confirm(
			`Are you sure you want to cancel downloading "${progress.name}"?`,
			'Cancel Download',
			{
				confirmButtonText: 'Confirm',
				cancelButtonText: 'Cancel',
				type: 'warning',
			}
		);

		// 使用uid作为fileId
		const fileId = progress.uid;

		const response = await cancelImportDownload(props.onboardingId, progress.taskId, fileId);

		if (response?.code === '200') {
			// 移除进度
			downloadProgress.value = downloadProgress.value.filter((p) => p.uid !== progress.uid);

			ElMessage.success('Download cancelled');

			// 如果没有下载任务了，停止轮询
			if (downloadProgress.value.length === 0) {
				stopPolling(`polling-${props.stageId}`);
			}
		} else {
			ElMessage.error(response?.msg || 'Failed to cancel download');
		}
	} catch (error) {
		// 用户点击取消按钮时，error 为 'cancel'
		if (error !== 'cancel') {
			console.error('Error cancelling download:', error);
			ElMessage.error('Failed to cancel download');
		}
	}
};
onMounted(() => {
	fetchDocuments();
	// 检查是否有进行中的下载任务
	checkAndStartPolling();
});

// 组件卸载时清理所有定时器
onUnmounted(() => {
	pollingTimers.value.forEach((timer) => {
		clearInterval(timer);
	});
	pollingTimers.value.clear();
});

// 监听 stageId 变化，重新加载文档
watch(
	() => props.stageId,
	(newStageId, oldStageId) => {
		// 如果 stageId 变化，先停止旧的轮询
		if (oldStageId) {
			stopPolling(`polling-${oldStageId}`);
		}

		// 清空下载进度
		downloadProgress.value = [];

		// 重新加载文档
		fetchDocuments();

		// 检查新 stageId 是否有进行中的下载任务
		if (newStageId) {
			checkAndStartPolling();
		}
	}
);

defineExpose({
	vailComponent,
});
</script>

<style scoped>
.expand-icon {
	transition: transform 0.2s ease;
	color: var(--el-color-white);

	&.rotated {
		transform: rotate(90deg);
	}
}
/* 文件列表样式 */
.animate-spin {
	animation: spin 1s linear infinite;
}

@keyframes spin {
	from {
		transform: rotate(0deg);
	}
	to {
		transform: rotate(360deg);
	}
}

/* 文件项悬停效果 */
.file-item:hover {
	transform: translateY(-1px);
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

html.dark .file-item:hover {
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
}
</style>
