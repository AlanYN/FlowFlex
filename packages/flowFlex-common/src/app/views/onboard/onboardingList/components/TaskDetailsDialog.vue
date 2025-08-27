<template>
	<el-dialog
		v-model="dialogVisible"
		:title="`Task Details - ${task?.name || 'Task'}`"
		width="60%"
		:before-close="handleClose"
		destroy-on-close
		class="task-details-dialog"
	>
		<div v-if="task" class="dialog-content">
			<!-- 任务基本信息 -->
			<div class="task-basic-info">
				<div v-if="task.description" class="task-info-item">
					<span class="info-label">Description:</span>
					<span class="info-value">{{ task.description }}</span>
				</div>
				<div class="task-info-item">
					<span class="info-label">Status:</span>
					<el-tag :type="task.isCompleted ? 'success' : 'warning'" size="small">
						{{ task.isCompleted ? 'Completed' : 'Pending' }}
					</el-tag>
				</div>
				<div class="task-info-item">
					<span class="info-label">Assignee:</span>
					<span class="info-value">
						{{ task.assigneeName || defaultStr }}
					</span>
				</div>
			</div>

			<!-- Notes 区域 -->
			<div class="notes-section">
				<div class="section-header">
					<h3 class="section-title">
						<el-icon><Edit /></el-icon>
						Notes
					</h3>
					<el-button
						type="primary"
						size="small"
						@click="showAddNoteInput = !showAddNoteInput"
						:icon="Plus"
					>
						Add Note
					</el-button>
				</div>

				<!-- 添加新 Note 输入框 -->
				<div v-if="showAddNoteInput" class="add-note-section">
					<el-input
						v-model="newNoteContent"
						type="textarea"
						:rows="3"
						placeholder="Enter your note here..."
						class="add-note-textarea"
					/>
					<div class="add-note-actions">
						<el-button type="primary" size="small" @click="addNote">Add</el-button>
						<el-button size="small" @click="cancelAddNote">Cancel</el-button>
					</div>
				</div>

				<!-- Notes 列表 -->
				<div class="notes-content" v-loading="notesLoading">
					<template v-if="notes && notes.length > 0">
						<el-scrollbar class="notes-scrollbar" max-height="300px">
							<div class="notes-list">
								<div
									v-for="(note, index) in notes"
									:key="note.id || index"
									class="note-item"
								>
									<!-- 查看模式 -->
									<template v-if="editingNoteIndex !== index">
										<div class="note-content">
											<p class="note-text">{{ note.content }}</p>
											<div class="note-meta">
												<span class="note-author">
													By
													{{ note.createdByName || defaultStr }}
												</span>
												<span class="note-date">
													on {{ timeZoneConvert(note.createdAt) }}
												</span>
											</div>
										</div>
										<div class="note-actions">
											<el-button
												type="primary"
												size="small"
												text
												@click="editNote(index)"
												link
												:icon="Edit"
											/>

											<el-button
												type="danger"
												size="small"
												text
												@click="removeNote(index)"
												link
												:icon="Delete"
											/>
										</div>
									</template>

									<!-- 编辑模式 -->
									<template v-else>
										<div class="note-content edit-mode">
											<el-input
												v-model="editingNoteContent"
												type="textarea"
												:rows="3"
												class="edit-note-textarea"
											/>
											<div class="edit-note-actions">
												<el-button
													type="primary"
													size="small"
													@click="saveEditNote(index)"
												>
													Save
												</el-button>
												<el-button size="small" @click="cancelEditNote">
													Cancel
												</el-button>
											</div>
										</div>
									</template>
								</div>
							</div>
						</el-scrollbar>
					</template>
					<template v-else>
						<div class="no-notes">
							<el-icon><Edit /></el-icon>
							<span>No notes added yet</span>
						</div>
					</template>
				</div>
			</div>

			<!-- 附件区域 -->
			<div class="attachments-section">
				<div class="section-header">
					<h3 class="section-title">
						<el-icon><Paperclip /></el-icon>
						Attachments
					</h3>
					<el-button type="primary" size="small" @click="triggerUpload" :icon="Plus">
						Add Attachment
					</el-button>
					<!-- 隐藏的上传组件 -->
					<el-upload
						ref="uploadRef"
						class="hidden-upload"
						:auto-upload="false"
						:on-change="handleFileChange"
						:before-upload="handleBeforeUpload"
						:on-exceed="handleExceed"
						:on-error="handleUploadError"
						:show-file-list="false"
						multiple
						:limit="5"
						accept=".pdf,.docx,.doc,.jpg,.jpeg,.png,.xlsx,.xls"
						style="position: absolute; left: -9999px; opacity: 0; pointer-events: none"
					/>
				</div>

				<!-- 附件列表 -->
				<div class="attachments-content">
					<template v-if="localTask.attachments && localTask.attachments.length > 0">
						<el-scrollbar class="attachments-scrollbar" max-height="300px">
							<div class="attachments-list">
								<div
									v-for="(attachment, index) in localTask.attachments"
									:key="attachment.id || index"
									class="attachment-item"
								>
									<div class="attachment-info">
										<el-icon class="attachment-icon">
											<Document />
										</el-icon>
										<div class="attachment-details">
											<span class="attachment-name">
												{{ attachment.name }}
											</span>
											<span class="attachment-size">
												{{ formatFileSize(attachment.size || 0) }}
											</span>
										</div>
									</div>
									<div class="attachment-actions">
										<el-button
											type="primary"
											size="small"
											text
											@click="downloadAttachment(attachment)"
											link
											:icon="Download"
										/>
										<el-button
											type="danger"
											size="small"
											text
											@click="removeAttachment(index)"
											link
											:icon="Delete"
										/>
									</div>
								</div>
							</div>
						</el-scrollbar>
					</template>
					<template v-else>
						<div class="no-attachments">
							<el-icon><Paperclip /></el-icon>
							<span>No attachments added yet</span>
						</div>
					</template>
				</div>
			</div>
		</div>

		<!-- 弹窗底部按钮 -->
		<template #footer>
			<div class="dialog-footer">
				<el-button @click="handleClose">Cancel</el-button>
				<el-button type="primary" @click="handleSave">Save Changes</el-button>
			</div>
		</template>

		<!-- 上传进度显示区域 -->
		<div v-if="uploadProgress.length > 0" class="upload-progress-section">
			<div class="section-header">
				<h3 class="section-title">
					<el-icon><Upload /></el-icon>
					Uploading Files...
				</h3>
			</div>
			<div class="upload-progress-content">
				<div v-for="progress in uploadProgress" :key="progress.uid" class="progress-item">
					<div class="progress-info">
						<el-icon class="progress-icon">
							<Document />
						</el-icon>
						<div class="progress-details">
							<span class="progress-name">{{ progress.name }}</span>
							<el-progress :percentage="progress.percentage" :show-text="false" />
						</div>
					</div>
					<span class="progress-percentage">{{ progress.percentage }}%</span>
				</div>
			</div>
		</div>
	</el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { Edit, Document, Paperclip, Plus, Upload, Delete, Download } from '@element-plus/icons-vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { defaultStr } from '@/settings/projectSetting';
import { TaskData, TaskNote, TaskAttachment } from '#/onboard';
import { useI18n } from '@/hooks/useI18n';
import {
	getOnboardingTaskNoteList,
	saveOnboardingTaskNote,
	deleteOnboardingTaskNote,
	editOnboardingTaskNote,
	saveCheckListTask,
	uploadCheckListTaskFile,
} from '@/apis/ow/onboarding';
import { timeZoneConvert } from '@/hooks/time';
import { useGlobSetting } from '@/settings';

// 扩展的TaskData类型，包含本地需要的字段
interface ExtendedTaskData extends TaskData {
	attachments?: TaskAttachment[];
	filesJson?: string; // 添加filesJson字段
}

// 本地文件附件接口，匹配filesJson格式
interface LocalFileAttachment {
	fileName: string;
	accessUrl: string;
	fileSize: number;
	id?: string; // 本地生成的ID，用于React key
}

const { t } = useI18n();

// Props
interface Props {
	visible: boolean;
	task: TaskData | null;
	onboardingId?: string | number;
	stageId: string | number;
}

const props = defineProps<Props>();

// Events
const emit = defineEmits<{
	'update:visible': [value: boolean];
	'update:task': [task: TaskData];
}>();

// 响应式数据
const dialogVisible = computed({
	get: () => props.visible,
	set: (value) => emit('update:visible', value),
});

const localTask = ref<ExtendedTaskData>({} as ExtendedTaskData);
const uploadRef = ref();
const uploadProgress = ref<{ uid: string; name: string; percentage: number }[]>([]);

// Notes 相关
const notes = ref<TaskNote[]>([]);
const showAddNoteInput = ref(false);
const newNoteContent = ref('');
const notesLoading = ref(false);

// 编辑笔记相关
const editingNoteIndex = ref(-1);
const editingNoteContent = ref('');

// 监听task变化，更新本地数据
watch(
	() => props.task,
	(newTask) => {
		if (newTask) {
			// 解析filesJson字段获取附件列表
			let attachments: TaskAttachment[] = [];
			try {
				const extendedTask = newTask as ExtendedTaskData;
				if (extendedTask.filesJson) {
					const filesData: LocalFileAttachment[] = JSON.parse(extendedTask.filesJson);
					attachments = filesData.map((file, index) => ({
						id: file.id || `file_${index}_${Date.now()}`,
						name: file.fileName,
						size: file.fileSize,
						url: file.accessUrl,
						uploadDate: new Date().toISOString(),
					}));
				}
			} catch (error) {
				console.warn('Failed to parse filesJson:', error);
				attachments = [];
			}

			localTask.value = {
				...newTask,
				attachments,
			};
			// 重置notes
			notes.value = [];
		}
	},
	{ immediate: true }
);

// 监听弹窗visible变化，每次打开时重新加载数据
watch(
	() => props.visible,
	async (newVisible, oldVisible) => {
		if (newVisible && props.task) {
			// 当弹窗打开时，重新加载数据
			// 包括初始打开(oldVisible为undefined)和从关闭变为打开的情况
			await loadTaskNotes();
		} else if (!newVisible && oldVisible) {
			// 当弹窗关闭时，清理编辑状态
			resetForm();
		}
	},
	{ immediate: true }
);

// 加载任务笔记
const loadTaskNotes = async () => {
	if (!props.task?.id || !props.onboardingId) return;

	try {
		notesLoading.value = true;
		const response = await getOnboardingTaskNoteList(
			String(props.onboardingId),
			String(props.task.id)
		);

		// 转换API响应为组件需要的格式
		if (response.code == '200') {
			notes.value = response?.data || [];
		}
	} finally {
		notesLoading.value = false;
	}
};

// 方法
const handleClose = () => {
	const hasUnsavedAdd = showAddNoteInput.value && newNoteContent.value.trim();
	const hasUnsavedEdit = editingNoteIndex.value !== -1 && editingNoteContent.value.trim();

	if (hasUnsavedAdd || hasUnsavedEdit) {
		ElMessageBox.confirm(
			'You have unsaved changes. Are you sure you want to close?',
			'Confirm Close',
			{
				confirmButtonText: t('sys.app.confirmText'),
				cancelButtonText: t('sys.app.cancelText'),
				type: 'warning',
			}
		)
			.then(() => {
				dialogVisible.value = false;
				resetForm();
			})
			.catch(() => {
				// 用户取消，不执行任何操作
			});
	} else {
		dialogVisible.value = false;
		resetForm();
	}
};

const resetForm = () => {
	showAddNoteInput.value = false;
	newNoteContent.value = '';
	editingNoteIndex.value = -1;
	editingNoteContent.value = '';
	uploadProgress.value = [];
};

const handleSave = async () => {
	if (!props.task?.id || !props.onboardingId) {
		ElMessage.error('Missing task or onboarding information');
		return;
	}

	try {
		// 将attachments转换为filesJson格式
		const filesData: LocalFileAttachment[] = (localTask.value.attachments || []).map(
			(attachment) => ({
				fileName: attachment.name,
				accessUrl: attachment.url || '',
				fileSize: attachment.size || 0,
			})
		);

		const filesJson = JSON.stringify(filesData);
		console.log(props.task);
		// 调用saveCheckListTask API保存文件信息
		const response = await saveCheckListTask({
			checklistId: props.task.checklistId,
			isCompleted: props.task.isCompleted,
			onboardingId: String(props.onboardingId),
			stageId: props.stageId,
			taskId: String(props.task.id),
			filesJson,
		});

		if (response.code === '200') {
			// 更新本地task数据并发出事件
			const updatedTask = {
				...localTask.value,
				filesJson,
			};
			emit('update:task', updatedTask);
			ElMessage.success('Task details saved successfully');
			dialogVisible.value = false;
			resetForm();
		} else {
			ElMessage.error(response.msg || 'Failed to save task details');
		}
	} finally {
		uploadProgress.value = [];
	}
};

// Notes 相关方法
const addNote = async () => {
	const content = newNoteContent.value.trim();
	if (!content) {
		ElMessage.warning('Please enter note content');
		return;
	}

	if (!props.task?.id || !props.onboardingId) {
		ElMessage.error('Missing task or onboarding information');
		return;
	}

	try {
		// 调用API保存笔记
		const res = await saveOnboardingTaskNote({
			taskId: String(props.task.id),
			onboardingId: String(props.onboardingId),
			content: content,
		});
		if (res.code === '200') {
			ElMessage.success(t('sys.api.operationSuccess'));
			newNoteContent.value = '';
			showAddNoteInput.value = false;
			await loadTaskNotes();
		} else {
			ElMessage.error(res.msg || t('sys.api.operationFailed'));
		}
	} catch (error) {
		await loadTaskNotes();
	}
};

const cancelAddNote = () => {
	newNoteContent.value = '';
	showAddNoteInput.value = false;
};

// 编辑笔记方法
const editNote = (index: number) => {
	const note = notes.value?.[index];
	if (!note) return;

	editingNoteIndex.value = index;
	editingNoteContent.value = note.content;
};

const cancelEditNote = () => {
	editingNoteIndex.value = -1;
	editingNoteContent.value = '';
};

const saveEditNote = async (index: number) => {
	const note = notes.value?.[index];
	if (!note || !props.task?.id || !props.onboardingId) return;

	const content = editingNoteContent.value.trim();
	if (!content) {
		ElMessage.warning('Please enter note content');
		return;
	}

	try {
		// 调用API编辑笔记
		const res = await editOnboardingTaskNote(note.id, {
			taskId: String(props.task.id),
			onboardingId: String(props.onboardingId),
			content: content,
		});
		if (res.code === '200') {
			// 重新加载笔记列表
			await loadTaskNotes();

			editingNoteIndex.value = -1;
			editingNoteContent.value = '';
		} else {
			ElMessage.error(res.msg || t('sys.api.operationFailed'));
		}
		ElMessage.success('Note updated successfully');
	} catch (error) {
		await loadTaskNotes();
	}
};

const removeNote = async (index: number) => {
	const note = notes.value?.[index];
	if (!note) return;

	try {
		await ElMessageBox.confirm(
			'Are you sure you want to delete this note? This action cannot be undone.',
			'Confirm Delete Note',
			{
				confirmButtonText: t('sys.app.confirmText'),
				cancelButtonText: t('sys.app.cancelText'),
				type: 'warning',
			}
		);

		// 调用API删除笔记
		const res = await deleteOnboardingTaskNote(note.id);
		if (res.code === '200') {
			// 重新加载笔记列表
			await loadTaskNotes();

			ElMessage.success(t('sys.api.operationSuccess'));
		} else {
			ElMessage.error(res.msg || t('sys.api.operationFailed'));
		}
	} catch (error) {
		if (error !== 'cancel') {
			// 不是用户取消操作
			console.error('Failed to delete note:', error);
			ElMessage.error('Failed to delete note');
		}
	}
};

// 附件相关方法
const handleBeforeUpload = (file: File) => {
	// 检查文件大小（10MB限制）
	const maxSize = 10 * 1024 * 1024; // 10MB
	if (file.size > maxSize) {
		ElMessage.error('File size cannot exceed 10MB');
		return false;
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

// 处理文件选择变化 - 在这里直接处理上传
const handleFileChange = async (file: any) => {
	if (!file.raw) return;

	// 验证文件
	if (!handleBeforeUpload(file.raw)) {
		return;
	}

	if (!props.task?.id || !props.onboardingId) {
		ElMessage.error('Missing task or onboarding information');
		return;
	}

	try {
		// 添加到进度列表
		uploadProgress.value.push({
			uid: file.uid,
			name: file.name,
			percentage: 0,
		});

		// 构造FormData
		const formData = {
			name: 'formFile', // 表单字段名（与curl一致）
			file: file.raw, // 文件对象
			filename: file.raw.name, // 文件名
		};

		// 调用上传API，带进度回调
		const response = await uploadCheckListTaskFile(
			String(props.onboardingId),
			formData,
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

		if (response.data.code === '200' && response.data.data) {
			// 添加成功上传的文件到列表
			const newAttachment: TaskAttachment = {
				id: `attachment_${Date.now()}_${Math.random()}`,
				name: file.name,
				size: file.size || 0,
				url: response.data.data.accessUrl || '',
				uploadDate: new Date().toISOString(),
			};

			if (!localTask.value.attachments) {
				localTask.value.attachments = [];
			}

			localTask.value.attachments.push(newAttachment);
			ElMessage.success(`${file.name} uploaded successfully`);
		} else {
			console.error(`Failed to upload ${file.name}:`, response.data.msg);
			ElMessage.error(
				`Failed to upload ${file.name}: ${response.data.msg || 'Unknown error'}`
			);
		}
	} catch (error) {
		console.error(`Error uploading ${file.name}:`, error);
		ElMessage.error(`Error uploading ${file.name}`);

		// 从进度列表中移除
		uploadProgress.value = uploadProgress.value.filter((p) => p.uid !== file.uid);
	}
};

// 触发文件选择
const triggerUpload = () => {
	if (uploadRef.value) {
		// 尝试多种方式访问文件输入元素
		const fileInput =
			uploadRef.value.$el?.querySelector('input[type="file"]') ||
			uploadRef.value.$refs?.['upload-inner']?.querySelector('input[type="file"]') ||
			uploadRef.value.uploadRef?.querySelector('input[type="file"]');

		if (fileInput) {
			fileInput.click();
		} else {
			console.warn('Cannot find file input element');
			// 备用方案：直接创建文件输入
			const input = document.createElement('input');
			input.type = 'file';
			input.multiple = true;
			input.accept = '.pdf,.docx,.doc,.jpg,.jpeg,.png,.xlsx,.xls';
			input.style.display = 'none';
			input.onchange = (e) => {
				const target = e.target as HTMLInputElement;
				const files = Array.from(target.files || []);
				files.forEach((file: File) => {
					handleFileChange({
						raw: file,
						name: file.name,
						size: file.size,
						uid: Date.now() + Math.random(),
					});
				});
				document.body.removeChild(input);
			};
			document.body.appendChild(input);
			input.click();
		}
	} else {
		console.error('Upload ref not found');
	}
};

const removeAttachment = async (index: number) => {
	try {
		await ElMessageBox.confirm(
			'Are you sure you want to remove this attachment?',
			'Confirm Remove Attachment',
			{
				confirmButtonText: t('sys.app.confirmText'),
				cancelButtonText: t('sys.app.cancelText'),
				type: 'warning',
			}
		);

		localTask.value.attachments?.splice(index, 1);
		ElMessage.success('Attachment removed successfully');
	} catch {
		// 用户取消，不执行任何操作
	}
};

const downloadAttachment = (attachment: TaskAttachment) => {
	if (!attachment.url) {
		ElMessage.warning('File download URL not available');
		return;
	}

	try {
		const globalSetting = useGlobSetting();
		// 创建下载链接
		const link = document.createElement('a');
		link.href = `${globalSetting.domainUrl}${attachment.url}`;
		link.download = attachment.name;
		link.target = '_blank';
		document.body.appendChild(link);
		link.click();
		document.body.removeChild(link);
		ElMessage.success(`Downloading ${attachment.name}`);
	} catch (error) {
		console.error('Error downloading file:', error);
		ElMessage.error('Failed to download file');
	}
};

const formatFileSize = (bytes: number): string => {
	if (bytes === 0) return '0 Bytes';
	const k = 1024;
	const sizes = ['Bytes', 'KB', 'MB', 'GB'];
	const i = Math.floor(Math.log(bytes) / Math.log(k));
	return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
};
</script>

<style scoped lang="scss">
.task-details-dialog {
	:deep(.el-dialog__body) {
		padding: 20px;
		max-height: 70vh;
		overflow-y: auto;
	}
}

.dialog-content {
	display: flex;
	flex-direction: column;
	gap: 20px;
}

/* 任务基本信息 */
.task-basic-info {
	background-color: #f8f9fa;
	padding: 16px;
	border-radius: 8px;
	border-left: 4px solid #3b82f6;
}

.task-info-item {
	display: flex;
	align-items: center;
	margin-bottom: 8px;

	&:last-child {
		margin-bottom: 0;
	}
}

.info-label {
	font-weight: 600;
	color: #374151;
	min-width: 120px;
	margin-right: 12px;
}

.info-value {
	color: #6b7280;
	flex: 1;
}

/* 区域标题 */
.section-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	margin-bottom: 16px;
}

.section-title {
	display: flex;
	align-items: center;
	gap: 8px;
	font-size: 16px;
	font-weight: 600;
	color: #1f2937;
	margin: 0;
	flex: 1;
}

/* Notes 区域 */
.notes-section {
	.notes-content {
		min-height: 60px;
	}

	.add-note-section {
		margin-bottom: 16px;
		padding: 12px;
		background-color: #f8f9fa;
		border-radius: 6px;
		border: 1px solid #e5e7eb;
	}

	.add-note-textarea {
		margin-bottom: 12px;
	}

	.add-note-actions {
		display: flex;
		gap: 8px;
	}

	.notes-scrollbar {
		border-radius: 6px;
		border: 1px solid #e5e7eb;
	}

	.notes-list {
		display: flex;
		flex-direction: column;
		gap: 12px;
		padding: 8px;
	}

	.note-item {
		display: flex;
		justify-content: space-between;
		align-items: flex-start;
		padding: 12px;
		background-color: #f8f9fa;
		border: 1px solid #e5e7eb;
		border-radius: 6px;
		transition: all 0.2s ease;

		&:hover {
			border-color: #3b82f6;
			box-shadow: 0 2px 4px rgba(59, 130, 246, 0.1);
		}
	}

	.note-content {
		flex: 1;
		min-width: 0;
		margin-right: 12px;
	}

	.note-text {
		margin: 0 0 8px 0;
		color: #1f2937;
		line-height: 1.5;
		white-space: pre-wrap;
		word-break: break-word;
	}

	.note-meta {
		display: flex;
		align-items: center;
		gap: 8px;
		font-size: 12px;
		color: #6b7280;
	}

	.note-author {
		font-weight: 500;
	}

	.note-date {
		opacity: 0.8;
	}

	.note-actions {
		flex-shrink: 0;
		display: flex;
		align-items: flex-start;
		gap: 4px;
	}

	.edit-mode {
		width: 100%;

		.edit-note-textarea {
			margin-bottom: 8px;
		}

		.edit-note-actions {
			display: flex;
			gap: 8px;
		}
	}

	.no-notes {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 8px;
		padding: 24px;
		color: #9ca3af;
		background-color: #f9fafb;
		border: 2px dashed #d1d5db;
		border-radius: 8px;
		font-style: italic;
	}
}

/* 附件区域 */
.attachments-section {
	.attachments-content {
		min-height: 60px;
	}

	.attachments-scrollbar {
		border-radius: 6px;
		border: 1px solid #e5e7eb;
	}

	.attachments-list {
		display: flex;
		flex-direction: column;
		gap: 8px;
		padding: 8px;
	}

	.attachment-item {
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding: 12px;
		background-color: #f8f9fa;
		border: 1px solid #e5e7eb;
		border-radius: 6px;
		transition: all 0.2s ease;

		&:hover {
			border-color: #3b82f6;
			box-shadow: 0 2px 4px rgba(59, 130, 246, 0.1);
		}
	}

	.attachment-info {
		display: flex;
		align-items: center;
		gap: 12px;
		flex: 1;
		min-width: 0;
	}

	.attachment-icon {
		color: #6b7280;
		font-size: 20px;
		flex-shrink: 0;
	}

	.attachment-details {
		display: flex;
		flex-direction: column;
		min-width: 0;
		flex: 1;
	}

	.attachment-name {
		font-weight: 500;
		color: #1f2937;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.attachment-size {
		font-size: 12px;
		color: #6b7280;
	}

	.attachment-actions {
		display: flex;
		gap: 8px;
		flex-shrink: 0;
	}

	.no-attachments {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 8px;
		padding: 24px;
		color: #9ca3af;
		background-color: #f9fafb;
		border: 2px dashed #d1d5db;
		border-radius: 8px;
		font-style: italic;
	}
}

/* 弹窗底部 */
.dialog-footer {
	display: flex;
	justify-content: flex-end;
	gap: 8px;
}

/* 上传弹窗 */
.upload-dialog-footer {
	display: flex;
	justify-content: flex-end;
	gap: 8px;
}

/* 暗色主题适配 */
.dark {
	.task-basic-info {
		background-color: var(--black-300);
		border-left-color: #3b82f6;
	}

	.info-label {
		color: var(--white-100);
	}

	.info-value {
		color: var(--gray-300);
	}

	.section-title {
		color: var(--white-100);
	}

	.add-note-section {
		background-color: var(--black-300);
		border-color: var(--black-200);
	}

	.notes-scrollbar,
	.attachments-scrollbar {
		border-color: var(--black-200);
	}

	.note-item {
		background-color: var(--black-300);
		border-color: var(--black-200);

		&:hover {
			border-color: #3b82f6;
			box-shadow: 0 2px 4px rgba(59, 130, 246, 0.2);
		}
	}

	.note-text {
		color: var(--white-100);
	}

	.note-meta {
		color: var(--gray-400);
	}

	.no-notes,
	.no-attachments {
		background-color: var(--black-300);
		border-color: var(--black-200);
		color: var(--gray-400);
	}

	.attachment-item {
		background-color: var(--black-300);
		border-color: var(--black-200);

		&:hover {
			border-color: #3b82f6;
			box-shadow: 0 2px 4px rgba(59, 130, 246, 0.2);
		}
	}

	.attachment-name {
		color: var(--white-100);
	}

	.attachment-size {
		color: var(--gray-400);
	}

	.attachment-icon {
		color: var(--gray-400);
	}

	.progress-item {
		background-color: var(--black-300);
	}

	.progress-name {
		color: var(--white-100);
	}

	.progress-percentage {
		color: var(--gray-400);
	}

	.progress-icon {
		color: var(--gray-400);
	}

	.upload-progress-content {
		border-color: var(--black-200);
	}
}

/* 上传进度区域 */
.upload-progress-section {
	margin-bottom: 20px;
}

.upload-progress-content {
	display: flex;
	flex-direction: column;
	gap: 8px;
	padding: 8px;
	border-radius: 6px;
	border: 1px solid #e5e7eb;
}

.progress-item {
	display: flex;
	justify-content: space-between;
	align-items: center;
	padding: 8px;
	background-color: #f8f9fa;
	border-radius: 4px;
}

.progress-info {
	display: flex;
	align-items: center;
	gap: 12px;
	flex: 1;
	min-width: 0;
}

.progress-icon {
	color: #3b82f6;
	font-size: 16px;
	flex-shrink: 0;
}

.progress-details {
	flex: 1;
	min-width: 0;
}

.progress-name {
	font-size: 14px;
	font-weight: 500;
	color: #1f2937;
	display: block;
	margin-bottom: 4px;
}

.progress-percentage {
	font-size: 12px;
	color: #6b7280;
	flex-shrink: 0;
}

.hidden-upload {
	display: none;
}

/* 响应式设计 */
@media (max-width: 768px) {
	.task-details-dialog {
		:deep(.el-dialog) {
			width: 95% !important;
			margin: 5vh auto !important;
		}

		:deep(.el-dialog__body) {
			padding: 15px;
			max-height: 60vh;
		}
	}

	.section-header {
		flex-direction: column;
		align-items: flex-start;
		gap: 8px;
	}

	.task-info-item {
		flex-direction: column;
		align-items: flex-start;
		margin-bottom: 12px;
	}

	.info-label {
		min-width: auto;
		margin-right: 0;
		margin-bottom: 4px;
	}

	.attachment-item {
		flex-direction: column;
		align-items: flex-start;
		gap: 8px;
	}

	.attachment-info {
		width: 100%;
	}

	.attachment-actions {
		width: 100%;
		justify-content: flex-end;
	}

	/* 移动端调整滚动条高度 */
	.notes-scrollbar,
	.attachments-scrollbar {
		max-height: 200px;
	}
}
</style>
