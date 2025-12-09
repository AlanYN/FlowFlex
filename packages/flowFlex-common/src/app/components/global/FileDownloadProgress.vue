<template>
	<teleport to="body">
		<!-- Minimized Side Button -->
		<transition name="slide-in">
			<div
				v-if="downloadTasks.length > 0 && isMinimized"
				class="download-side-button"
				@click="handleExpand"
			>
				<el-icon class="text-white text-lg mb-1">
					<Download />
				</el-icon>
				<transition name="badge-bounce">
					<div v-if="activeDownloads > 0" class="download-count">
						{{ activeDownloads }}
					</div>
				</transition>
			</div>
		</transition>

		<!-- Expanded Panel -->
		<transition name="slide-fade">
			<div v-if="downloadTasks.length > 0 && !isMinimized" class="download-panel">
				<!-- Header -->
				<div class="download-panel-header">
					<div class="flex items-center space-x-2">
						<el-icon class="text-primary text-lg">
							<Download />
						</el-icon>
						<span class="font-semibold text-gray-900 dark:text-white">
							Downloading ({{ activeDownloads }}/{{ downloadTasks.length }})
						</span>
					</div>
					<div class="flex items-center space-x-1">
						<el-button
							v-if="hasCompletedTasks"
							link
							@click="clearCompleted"
							class="panel-action-btn"
						>
							<el-icon><Close /></el-icon>
						</el-button>
						<el-button link @click="handleMinimize" class="panel-action-btn">
							<el-icon><Minus /></el-icon>
						</el-button>
					</div>
				</div>

				<!-- Download List -->
				<div class="download-list">
					<transition-group name="list-item">
						<div
							v-for="task in downloadTasks"
							:key="task.id"
							class="download-item"
							:class="{
								'download-item--downloading': task.status === '',
								'download-item--success': task.status === 'success',
								'download-item--error': task.status === 'exception',
								'download-item--cancelled': task.status === 'cancelled',
							}"
						>
							<!-- Cancel Button in Top Right -->
							<el-button
								v-if="task.status === '' && task.cancelFn"
								link
								@click="handleCancelTask(task)"
								class="cancel-btn-absolute"
							>
								<el-icon><Close /></el-icon>
							</el-button>

							<div class="flex items-start space-x-3">
								<el-icon class="text-primary text-xl flex-shrink-0 mt-1">
									<Document />
								</el-icon>
								<div class="flex-1 min-w-0 pr-8">
									<div class="flex items-center justify-between mb-2">
										<span
											class="text-sm font-medium text-gray-900 dark:text-white truncate"
											:title="task.fileName"
										>
											{{ task.fileName }}
										</span>
										<span
											class="text-xs font-semibold text-gray-600 dark:text-gray-300 ml-2 flex-shrink-0"
										>
											{{ task.percentage }}%
										</span>
									</div>
									<el-progress
										:percentage="task.percentage"
										:status="
											task.status === 'cancelled'
												? 'warning'
												: (task.status as
														| ''
														| 'success'
														| 'exception'
														| 'warning')
										"
										:show-text="false"
										:stroke-width="6"
										class="mb-2"
									/>
									<div
										v-if="task.status === ''"
										class="text-xs text-gray-500 dark:text-gray-400"
									>
										{{ formatSpeed(task.speed) }} •
										{{ formatSize(task.loaded) }} / {{ formatSize(task.total) }}
									</div>
									<!-- Error Message Display -->
									<div
										v-if="task.status === 'exception' && task.errorMessage"
										class="error-message mt-2"
									>
										{{ task.errorMessage }}
									</div>
								</div>
							</div>
						</div>
					</transition-group>
				</div>
			</div>
		</transition>
	</teleport>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { Download, Document, Minus, Close } from '@element-plus/icons-vue';
import { defHttp } from '@/apis/axios';

export interface DownloadTask {
	id: string;
	fileName: string;
	percentage: number;
	loaded: number;
	total: number;
	speed: number;
	status: '' | 'success' | 'exception' | 'cancelled';
	errorMessage?: string; // Error message to display
	cancelFn?: () => void; // Function to cancel the download
}

export interface AttachmentToDownload {
	id: string;
	fileName: string;
	downloadLink: string;
}

// Emits
const emit = defineEmits<{
	downloadComplete: [files: File[]];
}>();

const downloadTasks = ref<DownloadTask[]>([]);
const isMinimized = ref(false);

const activeDownloads = computed(() => {
	return downloadTasks.value.filter((task) => task.status === '').length;
});

const hasCompletedTasks = computed(() => {
	return downloadTasks.value.some(
		(task) =>
			task.status === 'success' || task.status === 'exception' || task.status === 'cancelled'
	);
});

// Handle expand
const handleExpand = () => {
	isMinimized.value = false;
};

// Handle minimize
const handleMinimize = () => {
	isMinimized.value = true;
};

// Format file size
const formatSize = (bytes: number): string => {
	if (bytes === 0) return '0 B';
	const k = 1024;
	const sizes = ['B', 'KB', 'MB', 'GB'];
	const i = Math.floor(Math.log(bytes) / Math.log(k));
	return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
};

// Format download speed
const formatSpeed = (bytesPerSecond: number): string => {
	return formatSize(bytesPerSecond) + '/s';
};

// Add download task
const addTask = (task: DownloadTask) => {
	downloadTasks.value.push(task);
};

// Update task progress
const updateTask = (id: string, updates: Partial<DownloadTask>) => {
	const index = downloadTasks.value.findIndex((t) => t.id === id);
	if (index >= 0) {
		downloadTasks.value[index] = { ...downloadTasks.value[index], ...updates };
	}
};

// Remove task
const removeTask = (id: string) => {
	downloadTasks.value = downloadTasks.value.filter((t) => t.id !== id);
};

// Handle cancel task
const handleCancelTask = (task: DownloadTask) => {
	if (task.cancelFn) {
		task.cancelFn();
		updateTask(task.id, {
			status: 'cancelled',
		});
	}
};

// Clear completed tasks
const clearCompleted = () => {
	downloadTasks.value = downloadTasks.value.filter(
		(t) => t.status !== 'success' && t.status !== 'exception' && t.status !== 'cancelled'
	);
};

// Download file with progress tracking
const downloadFileWithProgress = async (attachment: AttachmentToDownload): Promise<File | null> => {
	const taskId = attachment.id;

	// Create abort controller for cancellation
	const abortController = new AbortController();

	try {
		// Add task to progress list with cancel function
		addTask({
			id: taskId,
			fileName: attachment.fileName,
			percentage: 0,
			loaded: 0,
			total: 0,
			speed: 0,
			status: '',
			cancelFn: () => {
				abortController.abort();
			},
		});

		let lastLoaded = 0;
		let lastTime = Date.now();

		// Download file using defHttp
		const response = await defHttp.request(
			{
				url: attachment.downloadLink,
				method: 'GET',
				responseType: 'blob',
				signal: abortController.signal,
				onDownloadProgress: (progressEvent: any) => {
					const currentTime = Date.now();
					const timeElapsed = (currentTime - lastTime) / 1000;
					const bytesLoaded = progressEvent.loaded - lastLoaded;
					const speed = timeElapsed > 0 ? bytesLoaded / timeElapsed : 0;

					updateTask(taskId, {
						loaded: progressEvent.loaded,
						total: progressEvent.total || 0,
						percentage: progressEvent.total
							? Math.round((progressEvent.loaded * 100) / progressEvent.total)
							: 0,
						speed,
					});

					lastLoaded = progressEvent.loaded;
					lastTime = currentTime;
				},
			},
			{ isReturnNativeResponse: true }
		);

		// Mark as success
		updateTask(taskId, {
			status: 'success',
			percentage: 100,
		});

		// Convert blob to File
		const blob = response.data;
		const file = new File([blob], attachment.fileName, { type: blob.type });
		return file;
	} catch (error: any) {
		// Check if download was cancelled
		if (error.name === 'AbortError' || error.name === 'CanceledError') {
			console.log(`Download cancelled: ${attachment.fileName}`);
			return null;
		}

		console.error(`Failed to download ${attachment.fileName}:`, error);

		// Extract error message
		const errorMessage =
			error.response?.data?.message ||
			error.message ||
			'Network error or file not accessible';

		// Mark as failed with error message
		updateTask(taskId, {
			status: 'exception',
			errorMessage,
		});

		return null;
	}
};

// Start downloading multiple attachments
const startDownloads = async (attachments: AttachmentToDownload[]) => {
	if (attachments.length === 0) return;

	// Download files in background
	const downloadedFiles: File[] = [];

	for (const attachment of attachments) {
		try {
			const file = await downloadFileWithProgress(attachment);
			if (file) {
				downloadedFiles.push(file);
			}
		} catch (error) {
			console.error(`Failed to download ${attachment.fileName}:`, error);
		}
	}

	// Emit download complete event with files
	if (downloadedFiles.length > 0) {
		emit('downloadComplete', downloadedFiles);

		// Clear completed downloads after a delay
		setTimeout(() => {
			clearCompleted();
		}, 3000);
	}
};

defineExpose({
	addTask,
	updateTask,
	removeTask,
	clearCompleted,
	startDownloads,
});
</script>

<style scoped lang="scss">
/* Side Button Styles */
.download-side-button {
	position: fixed;
	bottom: 100px;
	right: 0;
	transform: translateY(-50%);
	width: 48px;
	padding: 12px 8px;
	background: linear-gradient(
		180deg,
		var(--el-color-primary) 0%,
		var(--el-color-primary-light-3) 100%
	);
	border-radius: 8px 0 0 8px;
	cursor: pointer;
	z-index: 9999;
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	user-select: none;
	transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
	box-shadow: -2px 0 8px rgba(0, 0, 0, 0.1);

	&:hover {
		width: 56px;
		box-shadow: -4px 0 12px rgba(0, 0, 0, 0.15);
	}

	&:active {
		transform: translateY(-50%) scale(0.98);
	}
}

.download-count {
	margin-top: 4px;
	padding: 2px 6px;
	background: var(--el-color-danger);
	border-radius: 10px;
	color: var(--white-100);
	font-size: 11px;
	font-weight: 700;
	min-width: 20px;
	text-align: center;
}

/* Expanded Panel Styles */
.download-panel {
	position: fixed;
	bottom: 24px;
	right: 24px;
	width: 384px;
	max-height: 500px;
	background: var(--el-bg-color-overlay);
	border-radius: var(--el-border-radius-large);
	z-index: 9999;
	overflow: hidden;
	backdrop-filter: blur(10px);
	border: 1px solid var(--el-border-color-lighter);
}

.download-panel-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 16px 20px;
	border-bottom: 1px solid var(--el-border-color-lighter);
	background: linear-gradient(to bottom, var(--black-5), transparent);
}

.panel-action-btn {
	color: var(--el-text-color-secondary);
	transition: all 0.2s;

	&:hover {
		color: var(--el-text-color-primary);
		background: var(--el-fill-color-light);
	}
}

.download-list {
	max-height: 420px;
	overflow-y: auto;
	padding: 16px;
}

.download-item {
	position: relative;
	padding: 16px;
	background: var(--el-fill-color);
	border-radius: var(--el-border-radius-base);
	border-left: 3px solid transparent;
	margin-bottom: 12px;
	transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);

	&:hover {
		background: var(--el-fill-color-light);
		transform: translateY(-2px);
	}

	&:last-child {
		margin-bottom: 0;
	}

	// Status indicated by left border color
	&--downloading {
		border-left-color: var(--el-color-primary);
	}

	&--success {
		border-left-color: var(--el-color-success);
	}

	&--error {
		border-left-color: var(--el-color-danger);
	}

	&--cancelled {
		border-left-color: var(--el-color-warning);
	}
}

.cancel-btn-absolute {
	position: absolute;
	top: 4px;
	right: 4px;
	z-index: 2;
	color: var(--el-text-color-secondary);
	transition: all 0.2s;
	padding: 4px;
	min-height: auto;
	width: 24px;
	height: 24px;
	display: flex;
	align-items: center;
	justify-content: center;

	&:hover {
		color: var(--el-color-danger);
		background: var(--el-color-danger-light-9);
		border-radius: 4px;
	}
}

.error-message {
	padding: 8px 12px;
	background: var(--el-color-danger-light-9);
	border-radius: 4px;
	color: var(--el-color-danger);
	font-size: 12px;
	line-height: 1.4;
	word-break: break-word;
}

/* Custom scrollbar */
.download-list::-webkit-scrollbar {
	width: 6px;
}

.download-list::-webkit-scrollbar-track {
	background: transparent;
}

.download-list::-webkit-scrollbar-thumb {
	background: var(--el-border-color);
	border-radius: 3px;
	transition: background 0.2s;
}

.download-list::-webkit-scrollbar-thumb:hover {
	background: var(--el-border-color-hover);
}

/* Animations */
.slide-in-enter-active,
.slide-in-leave-active {
	transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.slide-in-enter-from {
	opacity: 0;
	transform: translateY(-50%) translateX(100%);
}

.slide-in-leave-to {
	opacity: 0;
	transform: translateY(-50%) translateX(100%);
}

.slide-fade-enter-active {
	transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
}

.slide-fade-leave-active {
	transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.slide-fade-enter-from {
	opacity: 0;
	transform: translateY(20px) scale(0.95);
}

.slide-fade-leave-to {
	opacity: 0;
	transform: translateY(20px) scale(0.95);
}

.badge-bounce-enter-active {
	animation: badge-bounce 0.5s cubic-bezier(0.68, -0.55, 0.265, 1.55);
}

@keyframes badge-bounce {
	0% {
		transform: scale(0);
	}
	50% {
		transform: scale(1.2);
	}
	100% {
		transform: scale(1);
	}
}

.list-item-enter-active,
.list-item-leave-active {
	transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.list-item-enter-from {
	opacity: 0;
	transform: translateX(-20px);
}

.list-item-leave-to {
	opacity: 0;
	transform: translateX(20px);
}

.list-item-move {
	transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}
</style>
