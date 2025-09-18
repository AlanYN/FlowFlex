<template>
	<div class="file-result-renderer">
		<!-- 简洁的文件展示 -->
		<slot name="label"></slot>
		<div class="file-display">
			<div class="file-info">
				<el-icon class="file-icon" :class="getFileIconClass()">
					<component :is="getFileIcon()" />
				</el-icon>
				<div class="file-content">
					<span class="file-name" :title="displayFileName">{{ displayFileName }}</span>
					<span class="file-meta">
						{{ fileTypeDisplay }}
						<span v-if="fileSize" class="file-size">
							· {{ formatFileSize(fileSize) }}
						</span>
					</span>
				</div>
			</div>
			<div class="file-actions">
				<el-button
					v-if="canPreview"
					type="primary"
					link
					:icon="View"
					@click="handlePreview"
					:loading="previewLoading"
					size="small"
					class="action-btn"
				/>

				<el-button
					type="primary"
					link
					:icon="Download"
					@click="handleDownload"
					:loading="downloadLoading"
					size="small"
					class="action-btn"
				/>
			</div>
		</div>

		<!-- 文件预览组件 -->
		<Teleport to="body">
			<PreviewFile
				:fileUrl="previewUrl"
				:fileBlob="previewBlob"
				:type="fileExtension"
				:isShow="showPreview"
				:offloading="previewLoading"
				@close-office="closePreview"
				@rendered-office="onPreviewRendered"
			/>
		</Teleport>
	</div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { ElMessage } from 'element-plus';
import {
	Document,
	Picture,
	VideoPlay,
	Microphone,
	Files,
	View,
	Download,
} from '@element-plus/icons-vue';
import PreviewFile from '@/components/previewFile/previewFile.vue';
import { dataURLtoBlob } from '@/utils/file';
import type { OutputType } from '@/utils/output-type-detector';
import { isFileDownloadType } from '@/utils/output-type-detector';

// Props定义
interface Props {
	outputType: OutputType;
}

const props = defineProps<Props>();

// Emits定义
const emits = defineEmits<{
	preview: [outputType: OutputType];
	download: [outputType: OutputType];
}>();

// 响应式数据
const showPreview = ref(false);
const previewLoading = ref(false);
const downloadLoading = ref(false);
const previewUrl = ref('');
const previewBlob = ref<Blob | undefined>(undefined);

// 计算属性
const fileData = computed(() => props.outputType.data);
const metadata = computed(() => props.outputType.metadata);

const displayFileName = computed(() => {
	return metadata.value?.fileName || 'file';
});

const fileExtension = computed(() => {
	return metadata.value?.extension || '';
});

const fileTypeDisplay = computed(() => {
	const extension = fileExtension.value.toUpperCase();
	const mimeType = metadata.value?.mimeType;

	if (extension) {
		return `${extension} File`;
	}

	if (mimeType) {
		if (mimeType.startsWith('image/')) return 'Image File';
		if (mimeType.startsWith('video/')) return 'Video File';
		if (mimeType.startsWith('audio/')) return 'Audio File';
		if (mimeType.includes('pdf')) return 'PDF Document';
		if (mimeType.includes('word')) return 'Word Document';
		if (mimeType.includes('excel') || mimeType.includes('spreadsheet'))
			return 'Excel Spreadsheet';
		if (mimeType.includes('powerpoint') || mimeType.includes('presentation'))
			return 'PowerPoint Presentation';
	}

	return 'File';
});

const fileSize = computed(() => {
	return metadata.value?.size;
});

// 检查是否可以预览
const canPreview = computed(() => {
	const ext = fileExtension.value.toLowerCase();
	const supportedTypes = ['jpg', 'jpeg', 'png', 'gif', 'bmp', 'webp', 'pdf', 'docx', 'xlsx'];
	return supportedTypes.includes(ext);
});

// 获取文件图标
const getFileIcon = () => {
	const mimeType = metadata.value?.mimeType || '';
	const extension = fileExtension.value.toLowerCase();

	if (
		mimeType.startsWith('image/') ||
		['jpg', 'jpeg', 'png', 'gif', 'bmp', 'webp', 'svg'].includes(extension)
	) {
		return Picture;
	}
	if (mimeType.startsWith('video/') || ['mp4', 'avi', 'mov', 'wmv', 'flv'].includes(extension)) {
		return VideoPlay;
	}
	if (mimeType.startsWith('audio/') || ['mp3', 'wav', 'flac', 'aac'].includes(extension)) {
		return Microphone;
	}
	if (['pdf', 'doc', 'docx', 'xls', 'xlsx', 'ppt', 'pptx'].includes(extension)) {
		return Files;
	}
	return Document;
};

// 获取文件图标样式类
const getFileIconClass = () => {
	const mimeType = metadata.value?.mimeType || '';
	const extension = fileExtension.value.toLowerCase();

	if (
		mimeType.startsWith('image/') ||
		['jpg', 'jpeg', 'png', 'gif', 'bmp', 'webp', 'svg'].includes(extension)
	) {
		return 'text-green-500';
	}
	if (mimeType.startsWith('video/') || ['mp4', 'avi', 'mov', 'wmv', 'flv'].includes(extension)) {
		return 'text-blue-500';
	}
	if (mimeType.startsWith('audio/') || ['mp3', 'wav', 'flac', 'aac'].includes(extension)) {
		return 'text-purple-500';
	}
	if (['pdf'].includes(extension)) {
		return 'text-red-500';
	}
	if (['doc', 'docx'].includes(extension)) {
		return 'text-blue-600';
	}
	if (['xls', 'xlsx'].includes(extension)) {
		return 'text-green-600';
	}
	if (['ppt', 'pptx'].includes(extension)) {
		return 'text-orange-500';
	}
	return 'text-gray-500';
};

// 格式化文件大小
const formatFileSize = (bytes: number): string => {
	if (!bytes) return '';

	const k = 1024;
	const sizes = ['Bytes', 'KB', 'MB', 'GB'];
	const i = Math.floor(Math.log(bytes) / Math.log(k));

	return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
};

// 创建预览数据
const createPreviewData = async (): Promise<{ url?: string; blob?: Blob }> => {
	const subType = props.outputType.subType;
	try {
		if (subType === 'base64') {
			// Base64 Data URL格式
			const dataURL = fileData.value.data || fileData.value.base64;
			if (dataURL && typeof dataURL === 'string') {
				if (dataURL.startsWith('data:')) {
					// 完整的Data URL
					const blob = dataURLtoBlob(dataURL);
					return { blob };
				} else {
					// 纯Base64数据，需要添加MIME类型
					const mimeType = metadata.value?.mimeType || 'application/octet-stream';
					const fullDataURL = `data:${mimeType};base64,${dataURL}`;
					const blob = dataURLtoBlob(fullDataURL);
					return { blob };
				}
			}
		} else if (subType === 'url') {
			// URL格式 - 检查是否为文件下载流
			if (isFileDownloadType(props.outputType)) {
				// 文件下载流情况，使用完整的URL
				const fullUrl = fileData.value.url || fileData.value.fullUrl;
				if (fullUrl && typeof fullUrl === 'string') {
					return { url: fullUrl };
				}
			} else {
				// 普通URL格式
				const url = fileData.value.url;
				if (url && typeof url === 'string') {
					return { url };
				}
			}
		} else if (subType === 'structured') {
			// 结构化文件数据
			const data = fileData.value.data || fileData.value.content || fileData.value.base64;
			if (data && typeof data === 'string') {
				if (data.startsWith('data:')) {
					const blob = dataURLtoBlob(data);
					return { blob };
				} else {
					// 假设是Base64数据
					const mimeType = metadata.value?.mimeType || 'application/octet-stream';
					const fullDataURL = `data:${mimeType};base64,${data}`;
					const blob = dataURLtoBlob(fullDataURL);
					return { blob };
				}
			}
		}

		throw new Error('Unrecognized file data format');
	} catch (error) {
		console.error('Failed to create preview data:', error);
		throw error;
	}
};

// 处理预览
const handlePreview = async () => {
	if (!canPreview.value) {
		ElMessage.warning('This file type is not supported for preview');
		return;
	}

	previewLoading.value = true;

	try {
		const previewData = await createPreviewData();

		if (previewData.url) {
			previewUrl.value = previewData.url;
			previewBlob.value = undefined;
		} else if (previewData.blob) {
			previewUrl.value = '';
			previewBlob.value = previewData.blob;
		}

		showPreview.value = true;
		emits('preview', props.outputType);
	} finally {
		previewLoading.value = false;
	}
};

// 处理下载
const handleDownload = async () => {
	downloadLoading.value = true;
	try {
		// 检查是否为文件下载流类型
		if (isFileDownloadType(props.outputType)) {
			const fullUrl = fileData.value.url || fileData.value.fullUrl;
			if (fullUrl && typeof fullUrl === 'string') {
				// 简单直接的下载方法
				fetch(fullUrl)
					.then((response) => response.blob())
					.then((blob) => {
						const url = URL.createObjectURL(blob);
						const a = document.createElement('a');
						a.href = url;
						a.download = displayFileName.value;
						a.click();
						URL.revokeObjectURL(url);
					})
					.catch(() => {
						// 备选方案
						const a = document.createElement('a');
						a.href = fullUrl;
						a.target = '_blank';
						a.download = displayFileName.value;
						a.click();
					});

				emits('download', props.outputType);
			}
		} else {
			const file = new Blob([fileData.value.data], {
				type: metadata.value?.mimeType || 'application/octet-stream',
			});
			const a = document.createElement('a');
			a.href = URL.createObjectURL(file);
			a.download = displayFileName.value;
			a.click();
			URL.revokeObjectURL(a.href);
			emits('download', props.outputType);
		}
	} finally {
		downloadLoading.value = false;
	}
};

// 关闭预览
const closePreview = () => {
	showPreview.value = false;
	previewUrl.value = '';
	previewBlob.value = undefined;
};

// 预览渲染完成
const onPreviewRendered = () => {
	previewLoading.value = false;
};
</script>

<style scoped lang="scss">
.file-result-renderer {
	@apply w-full px-3;
}

.file-display {
	@apply flex items-center justify-between p-2 my-2;
	transition: all 0.2s ease-in-out;
	min-height: 48px;

	&:hover {
		@apply border-blue-300 dark:border-blue-500 bg-blue-50 dark:bg-blue-900/10 rounded-xl;
	}
}

.file-info {
	@apply flex items-center flex-1 min-w-0;
}

.file-icon {
	@apply flex-shrink-0 mr-2 text-lg;
}

.file-content {
	@apply flex flex-col flex-1 min-w-0;
}

.file-name {
	@apply text-sm font-medium text-gray-900 dark:text-gray-100 truncate leading-tight;
	max-width: 300px;
}

.file-meta {
	@apply text-xs text-gray-500 dark:text-gray-400 leading-tight mt-0.5;
}

.file-size {
	@apply text-gray-400 dark:text-gray-500;
}

.file-actions {
	@apply flex items-center space-x-1 flex-shrink-0 ml-2;
}

.action-btn {
	@apply text-xs px-2 py-1 h-auto min-h-0;

	&:hover {
		@apply bg-blue-100 dark:bg-blue-800/30 text-blue-600 dark:text-blue-400;
	}
}

/* 响应式设计 */
@media (max-width: 640px) {
	.file-display {
		@apply flex-col items-stretch space-y-2 py-3;
	}

	.file-info {
		@apply justify-center;
	}

	.file-name {
		@apply text-center;
		max-width: none;
	}

	.file-actions {
		@apply justify-center ml-0;
	}
}

/* Element Plus 按钮样式覆盖 */
:deep(.el-button.action-btn) {
	border: none;
	background: transparent;
	color: var(--el-text-color-regular);
	font-size: 12px;
	padding: 4px 8px;
	height: auto;
	min-height: 24px;

	&:hover {
		background: var(--el-color-primary-light-9);
		color: var(--el-color-primary);
		border: none;
	}

	&:focus {
		background: var(--el-color-primary-light-9);
		color: var(--el-color-primary);
		border: none;
	}

	.el-icon {
		margin-right: 2px;
		font-size: 12px;
	}
}
</style>
