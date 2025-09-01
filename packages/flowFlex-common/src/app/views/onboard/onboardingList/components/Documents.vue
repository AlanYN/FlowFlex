<template>
	<div class="customer-block">
		<div class="flex items-center justify-between">
			<h2 class="text-lg font-semibold text-center flex">
				Documents
				<div v-if="props?.component?.isEnabled" class="text-red-500 ml-1">*</div>
			</h2>
			<el-button :icon="Upload" @click="triggerUpload">Upload Files</el-button>
		</div>
		<el-divider />

		<div class="space-y-4">
			<!-- 文件上传区域 -->
			<div>
				<el-upload
					ref="uploadRef"
					class="upload-demo rounded-md"
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
					<div class="flex flex-col items-center justify-center py-4">
						<el-icon class="text-4xl text-gray-400 mb-2">
							<Upload />
						</el-icon>
						<div class="text-lg text-gray-600 dark:text-gray-300">
							Drag and drop files here
						</div>
						<div class="text-sm text-gray-500 dark:text-gray-400 mt-1">
							or
							<em class="text-blue-500 hover:text-blue-600 cursor-pointer">
								click to browse
							</em>
						</div>
						<div class="text-xs text-gray-400 dark:text-gray-500 mt-2">
							Maximum number of files that can be uploaded at once: 5
						</div>
						<div class="text-xs text-gray-400 dark:text-gray-500">
							Supports: PDF, DOCX, DOC, JPG, JPEG, PNG, XLSX, XLS, MSG, EML (Max 10MB
							per file)
						</div>
					</div>
				</el-upload>
			</div>

			<!-- 上传进度 -->
			<div v-if="uploadProgress.length > 0" class="space-y-2">
				<h4 class="text-sm font-medium text-gray-700 dark:text-gray-300">Uploading...</h4>
				<div
					v-for="progress in uploadProgress"
					:key="progress.uid"
					class="flex items-center space-x-3 p-2 bg-gray-50 dark:bg-black-200 rounded"
				>
					<el-icon class="text-blue-500">
						<Document />
					</el-icon>
					<div class="flex-1">
						<div class="text-sm font-medium">{{ progress.name }}</div>
						<el-progress :percentage="progress.percentage" :show-text="false" />
					</div>
					<span class="text-xs text-gray-500">{{ progress.percentage }}%</span>
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
				<h4 class="text-sm font-medium text-gray-700 dark:text-gray-300">Uploaded Files</h4>
				<el-table :data="documents" stripe class="w-full">
					<el-table-column label="File Name" min-width="200">
						<template #default="{ row }">
							<div class="flex items-center">
								<el-icon class="text-blue-500 mr-2 flex-shrink-0">
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
							<div
								class="text-sm text-gray-500 dark:text-gray-400 table-cell-content"
								:title="formatFileSize(row.fileSize)"
							>
								{{ formatFileSize(row.fileSize) }}
							</div>
						</template>
					</el-table-column>

					<el-table-column label="Uploaded By" width="150">
						<template #default="{ row }">
							<div
								class="text-sm text-gray-500 dark:text-gray-400 table-cell-content"
								:title="row.uploadedByName"
							>
								{{ row.uploadedByName }}
							</div>
						</template>
					</el-table-column>

					<el-table-column label="Date" width="150">
						<template #default="{ row }">
							<div
								class="text-sm text-gray-500 dark:text-gray-400 table-cell-content"
								:title="row.uploadedDate"
							>
								{{ row.uploadedDate }}
							</div>
						</template>
					</el-table-column>

					<el-table-column label="Actions" width="150" fixed="right">
						<template #default="{ row }">
							<div class="flex items-center space-x-2">
								<el-button
									size="small"
									type="primary"
									link
									@click="handleViewDocument(row)"
								>
									<el-icon><View /></el-icon>
									View
								</el-button>
								<el-button
									size="small"
									type="danger"
									link
									@click="handleDeleteDocument(row.id)"
								>
									<el-icon><Delete /></el-icon>
									Delete
								</el-button>
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

		<!-- 文件预览组件 -->
		<vuePreviewFile
			:fileUrl="fileUrl"
			:type="fileType"
			:isShow="perviewFileShow"
			:offloading="offloading"
			@close-office="closeOffice"
			@rendered-office="offloading = false"
		/>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';

import { Upload, Document, Loading, View, Delete, Folder } from '@element-plus/icons-vue';
import {
	uploadOnboardingFile,
	getOnboardingFilesByStage,
	deleteOnboardingFile,
	previewOnboardingFile,
} from '@/apis/ow/onboarding';
import { timeZoneConvert } from '@/hooks/time';
import { DocumentItem, ComponentData } from '#/onboard';
import vuePreviewFile from '@/components/previewFile/previewFile.vue';

// Props
interface Props {
	onboardingId: string;
	stageId?: string;
	component: ComponentData;
	disabled?: boolean;
}

const props = defineProps<Props>();

// 响应式数据
const documents = ref<DocumentItem[]>([]);
const loading = ref(false);
const uploadProgress = ref<{ uid: string; name: string; percentage: number }[]>([]);
const uploadRef = ref();

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

// 获取文档列表
const fetchDocuments = async () => {
	if (!props.onboardingId) return;

	loading.value = true;
	try {
		let response;

		// 如果有stageId，按阶段获取文件；否则获取所有文件
		if (!props.stageId) return;
		response = await getOnboardingFilesByStage(props.onboardingId, props.stageId);

		if (response.code === '200') {
			documents.value =
				response.data.map((item) => {
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
		console.error('Error fetching documents:', error);
		documents.value = [];
		ElMessage.error('Failed to load documents');
	} finally {
		loading.value = false;
	}
};

// 文件上传处理
const triggerUpload = () => {
	uploadRef.value?.$el?.querySelector('input[type="file"]')?.click();
};

const handleBeforeUpload = (file: File) => {
	// 检查文件大小（10MB限制）
	const maxSize = 10 * 1024 * 1024; // 10MB
	if (file.size > maxSize) {
		ElMessage.error('File size cannot exceed 10MB');
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

			// 重新加载文档列表
			await fetchDocuments();

			// 触发事件
			emit('documentUploaded', response.data?.data);
		} else {
			ElMessage.error(response.data?.msg || `Failed to upload ${file.name}`);
		}
	} catch (error) {
		console.error('Upload error:', error);
		ElMessage.error(`Failed to upload ${file.name}`);

		// 从进度列表中移除
		uploadProgress.value = uploadProgress.value.filter((p) => p.uid !== file.uid);
	}
};

// 文档操作
const handleViewDocument = async (document: DocumentItem) => {
	try {
		// 获取文件扩展名
		const fileExt = document.originalFileName.split('.').pop()?.toLowerCase() || '';
		fileType.value = fileExt;

		perviewFileShow.value = true;
		offloading.value = true;
		// 调用API获取文件内容
		const res = await previewOnboardingFile(props.onboardingId, document.id);

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
			await fetchDocuments();
			emit('documentDeleted', documentId);
		} else {
			ElMessage.error(response.msg || 'Failed to delete document');
		}
	} catch (error) {
		if (error !== 'cancel') {
			console.error('Error deleting document:', error);
			ElMessage.error('Failed to delete document');
		}
	}
};

// 工具函数
const formatFileSize = (bytes: string): string => {
	if (!bytes) return '0 Bytes';

	const k = 1024;
	const sizes = ['Bytes', 'KB', 'MB', 'GB'];
	const i = Math.floor(Math.log(+bytes) / Math.log(k));

	return parseFloat((+bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
};

// 根据文件扩展名获取标准MIME类型
const getMimeType = (fileExtension: string) => {
	const mimeTypes = {
		// PDF文档
		pdf: 'application/pdf',
		// Microsoft Word文档
		doc: 'application/msword',
		docx: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
		// Microsoft Excel文档
		xls: 'application/vnd.ms-excel',
		xlsx: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
		// 图片文件
		jpg: 'image/jpeg',
		jpeg: 'image/jpeg',
		png: 'image/png',
		// 邮件文件
		msg: 'application/vnd.ms-outlook',
		eml: 'message/rfc822',
	} as const;

	return (
		mimeTypes[fileExtension.toLowerCase() as keyof typeof mimeTypes] ||
		'application/octet-stream'
	);
};

const vailComponent = () => {
	try {
		if (props?.component?.isEnabled && documents?.value?.length <= 0) {
			ElMessage.warning('Please upload at least one document');
			return false;
		}
		return true;
	} catch {
		return true;
	}
};

// 生命周期
onMounted(() => {
	fetchDocuments();
});

// 监听 stageId 变化，重新加载文档
watch(
	() => props.stageId,
	() => {
		fetchDocuments();
	}
);

defineExpose({
	vailComponent,
});
</script>

<style scoped>
:deep(.el-upload-dragger) {
	border: 2px dashed #d1d5db;
	background: transparent;
	transition: all 0.2s;
	padding: 2rem;
}

:deep(.el-upload-dragger:hover) {
	border-color: #3b82f6;
	background-color: #f0f9ff;
}

:deep(.el-progress-bar__outer) {
	background-color: #e5e7eb;
}

:deep(.el-progress-bar__inner) {
	background-color: #3b82f6;
}

/* 暗色主题支持 */
html.dark :deep(.el-upload-dragger) {
	border-color: #4b5563;
	background: transparent;
}

html.dark :deep(.el-upload-dragger:hover) {
	border-color: #3b82f6;
	background-color: rgba(59, 130, 246, 0.1);
}

html.dark :deep(.el-progress-bar__outer) {
	background-color: #374151;
}

/* 上传组件样式 */
.el-icon--upload {
	color: #3b82f6;
}

:deep(.el-upload__tip) {
	margin-top: 4px;
	color: #6b7280;
}

html.dark :deep(.el-upload__tip) {
	color: #9ca3af;
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

/* 自定义进度条颜色 */
:deep(.el-progress-bar__inner) {
	transition: width 0.3s ease;
}

/* 文件项悬停效果 */
.file-item:hover {
	transform: translateY(-1px);
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

html.dark .file-item:hover {
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
}

/* 表格单元格内容样式 - 参考 index.vue 实现 */
.table-cell-content {
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
	max-width: 100%;
	display: block;
}
</style>
