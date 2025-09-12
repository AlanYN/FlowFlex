<template>
	<div class="customer-block">
		<!-- 统一的头部卡片 -->
		<div class="notes-header-card rounded-md" :class="{ expanded: isOpen }" @click="toggleOpen">
			<div class="">
				<div class="flex items-center">
					<el-icon class="expand-icon text-lg mr-2" :class="{ rotated: isOpen }">
						<ArrowRight />
					</el-icon>
					<h3 class="notes-title">Internal Notes</h3>
				</div>
				<div class="notes-subtitle">
					{{ notes.length }} {{ notes.length > 1 ? 'notes' : 'note' }}
				</div>
			</div>
		</div>

		<!-- 可折叠的内容 -->
		<el-collapse-transition>
			<div v-show="isOpen" class="space-y-4 p-4">
				<!-- 加载状态 -->
				<div v-if="loading" class="text-center py-8">
					<el-icon class="text-2xl animate-spin">
						<Loading />
					</el-icon>
					<p class="text-gray-500 dark:text-gray-400 mt-2">Loading notes...</p>
				</div>

				<!-- 笔记列表 -->
				<el-scrollbar v-else class="pr-4" max-height="384px">
					<div class="space-y-3">
						<div
							v-for="note in notes"
							:key="note.id"
							class="flex space-x-3 p-3 bg-gray-50 dark:bg-black-200 rounded-lg"
						>
							<div
								class="w-8 h-8 rounded-full bg-blue-500 flex items-center justify-center text-white text-sm font-medium flex-shrink-0"
							>
								{{ getAuthorInitial(note.createBy) }}
							</div>
							<div class="flex-1 min-w-0">
								<div class="flex items-center justify-between mb-1">
									<span
										class="text-sm font-medium text-gray-900 dark:text-white-100"
									>
										{{ note.createBy || defaultStr }}
									</span>
									<div class="flex items-center space-x-2">
										<span class="text-xs text-gray-500 dark:text-gray-400">
											{{
												timeZoneConvert(
													note?.modifyDate || '',
													false,
													projectTenMinutesSsecondsDate
												)
											}}
										</span>
										<el-dropdown trigger="click">
											<el-button size="small" text class="p-1">
												<el-icon><MoreFilled /></el-icon>
											</el-button>
											<template #dropdown>
												<el-dropdown-menu>
													<el-dropdown-item @click="handleEditNote(note)">
														<el-icon><Edit /></el-icon>
														Edit
													</el-dropdown-item>
													<el-dropdown-item
														@click="handleDeleteNote(note.id)"
														class="text-red-500"
													>
														<el-icon><Delete /></el-icon>
														Delete
													</el-dropdown-item>
												</el-dropdown-menu>
											</template>
										</el-dropdown>
									</div>
								</div>

								<!-- 笔记内容 - 支持内联编辑 -->
								<div
									v-if="editingNoteId !== note.id"
									class="text-sm text-gray-700 dark:text-gray-300 whitespace-pre-wrap"
								>
									<component :is="renderNoteContent(note.content)" />
								</div>

								<!-- 内联编辑模式 -->
								<div v-else class="space-y-2">
									<Mention
										v-model="editingContent"
										placeholder="Edit note content..."
										:disabled="savingNote"
										class="w-full"
									/>
									<div class="flex justify-end space-x-2">
										<el-button
											size="small"
											:icon="Close"
											:disabled="savingNote"
											@click="handleCancelEdit"
										>
											Cancel
										</el-button>
										<el-button
											size="small"
											:icon="Document"
											type="primary"
											:loading="savingNote"
											@click="handleSaveEdit"
										>
											Save
										</el-button>
									</div>
								</div>
							</div>
						</div>

						<!-- 空状态 -->
						<div
							v-if="notes.length === 0"
							class="text-center py-8 text-gray-500 dark:text-gray-400"
						>
							<el-icon class="text-4xl mb-2">
								<ChatDotSquare />
							</el-icon>
							<p>No notes yet</p>
							<p class="text-xs mt-1">Add the first note to get started</p>
						</div>
					</div>
				</el-scrollbar>

				<!-- 添加笔记表单 - 移到最下方 -->
				<div class="border-t pt-4">
					<el-form @submit.prevent="handleAddNote">
						<el-form-item>
							<Mention
								v-model="newNote"
								placeholder="Add a note... Use @username to mention someone"
								:disabled="addingNote"
								class="w-full"
							/>
						</el-form-item>
						<el-form-item class="mb-0">
							<div class="w-full flex justify-end">
								<el-button
									type="primary"
									@click="handleAddNote"
									:disabled="!newNote.trim() || addingNote"
									:loading="addingNote"
									size="small"
									:icon="Promotion"
								>
									Add Note
								</el-button>
							</div>
						</el-form-item>
					</el-form>
				</div>
			</div>
		</el-collapse-transition>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, h, VNode } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useI18n } from '@/hooks/useI18n';
import {
	ArrowRight,
	ChatDotSquare,
	MoreFilled,
	Edit,
	Delete,
	Promotion,
	Document,
	Close,
	Loading,
} from '@element-plus/icons-vue';
import {
	getInternalNotesByOnboarding,
	createInternalNote,
	updateInternalNote,
	deleteInternalNote,
} from '@/apis/ow/onboarding';
import Mention from '@/components/mention/mention.vue';
import { defaultStr, projectTenMinutesSsecondsDate } from '@/settings/projectSetting';
import { timeZoneConvert } from '@/hooks/time';

// 笔记类型定义
interface Note {
	id: string;
	content: string;
	createBy?: string;
	createdAt: string;
	modifyDate?: string;
	isResolved?: boolean;
	onboardingId?: string;
}

// API响应类型定义
interface ApiResponse<T = any> {
	code: string;
	data: T;
	msg?: string;
}

// Props
interface Props {
	onboardingId: string;
	stageId?: string;
}

const props = defineProps<Props>();

// 国际化
const { t } = useI18n();

// 响应式数据
const isOpen = ref(true);
const newNote = ref('');
const editingNoteId = ref<string | null>(null);
const editingContent = ref('');
const notes = ref<Note[]>([]);
const loading = ref(false);
const addingNote = ref(false);
const savingNote = ref(false);

// 获取备注列表
const fetchNotes = async () => {
	if (!props.onboardingId) return;

	loading.value = true;
	try {
		const response: ApiResponse<Note[]> = await getInternalNotesByOnboarding(
			props.onboardingId,
			props?.stageId || ''
		);
		if (response.code === '200') {
			notes.value = response.data || [];
		} else {
			ElMessage.error(response.msg || t('sys.api.operationFailed'));
		}
	} catch (error) {
		console.error('Error fetching notes:', error);
		ElMessage.error(t('sys.api.operationFailed'));
	} finally {
		loading.value = false;
	}
};

// 组件挂载时获取数据
onMounted(() => {
	// watch已经设置了immediate选项，会处理初始stageId
	// 这里只处理组件展开但没有stageId的情况
	if (isOpen.value && !props.stageId) {
		fetchNotes();
	}
});

// 监听stageId变化，重新获取对应stage的笔记
watch(
	() => props.stageId,
	(newStageId, oldStageId) => {
		// 当stageId变化时，重新获取笔记（确保获取最新数据）
		if (newStageId !== oldStageId && newStageId) {
			fetchNotes();
		}
	},
	{ immediate: true }
);

// 事件处理函数
const toggleOpen = async () => {
	isOpen.value = !isOpen.value;
	if (isOpen.value && notes.value.length === 0) {
		await fetchNotes();
	}
};

const handleAddNote = async () => {
	if (!newNote.value.trim() || !props.onboardingId) return;

	addingNote.value = true;
	try {
		const params = {
			onboardingId: props.onboardingId,
			content: newNote.value.trim(),
			isResolved: false,
			stageId: props?.stageId || '',
		};

		const response: ApiResponse = await createInternalNote(params);
		if (response.code === '200') {
			ElMessage.success(t('sys.api.operationSuccess'));
			newNote.value = '';
			// Refresh the notes list
			await fetchNotes();
		} else {
			ElMessage.error(response.msg || t('sys.api.operationFailed'));
		}
	} finally {
		addingNote.value = false;
	}
};

const handleEditNote = (note: Note) => {
	editingNoteId.value = note.id;
	editingContent.value = note.content;
};

const handleSaveEdit = async () => {
	if (!editingNoteId.value || !editingContent.value.trim()) return;

	savingNote.value = true;
	try {
		const params = {
			content: editingContent.value.trim(),
		};

		const response: ApiResponse = await updateInternalNote(editingNoteId.value, params);
		if (response.code === '200') {
			ElMessage.success(t('sys.api.operationSuccess'));
			await fetchNotes();
		} else {
			ElMessage.error(response.msg || t('sys.api.operationFailed'));
		}

		editingNoteId.value = null;
		editingContent.value = '';
	} finally {
		savingNote.value = false;
	}
};

const handleCancelEdit = () => {
	editingNoteId.value = null;
	editingContent.value = '';
};

const handleDeleteNote = async (noteId: string) => {
	try {
		await ElMessageBox.confirm(
			'Are you sure you want to delete this note?',
			'Delete Confirmation',
			{
				confirmButtonText: 'Confirm',
				cancelButtonText: 'Cancel',
				type: 'warning',
			}
		);

		const response: ApiResponse = await deleteInternalNote(noteId);
		if (response.code === '200') {
			ElMessage.success(t('sys.api.operationSuccess'));
			// Refresh the notes list
			await fetchNotes();
		} else {
			ElMessage.error(response.msg || t('sys.api.operationFailed'));
		}
	} catch (error) {
		console.error('Error deleting note:', error);
	}
};

// 内容片段类型定义
interface ContentSegment {
	type: 'text' | 'mention';
	content: string;
}

// 解析内容为片段
const parseNoteContent = (content: string): ContentSegment[] => {
	const segments: ContentSegment[] = [];
	const mentionRegex = /(\[~(\S+?)\]|@(\w+(?:\.\w+)*))/g;
	let lastIndex = 0;
	let match;

	while ((match = mentionRegex.exec(content)) !== null) {
		// 添加提及前的普通文本
		if (match.index > lastIndex) {
			segments.push({
				type: 'text',
				content: content.slice(lastIndex, match.index),
			});
		}

		// 添加提及内容
		segments.push({
			type: 'mention',
			content: match[2] || match[3], // [~用户名] 或 @用户名
		});

		lastIndex = match.index + match[0].length;
	}

	// 添加剩余文本
	if (lastIndex < content.length) {
		segments.push({
			type: 'text',
			content: content.slice(lastIndex),
		});
	}

	return segments;
};

// 使用Render函数渲染内容
const renderNoteContent = (content: string): VNode => {
	const segments = parseNoteContent(content);

	return h(
		'div',
		{ class: 'inline' },
		segments.map((segment, index) => {
			if (segment.type === 'mention') {
				return h(
					'span',
					{
						key: `mention-${index}`,
						class: 'bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 px-1 rounded font-medium cursor-pointer hover:bg-blue-200 dark:hover:bg-blue-800 transition-colors duration-200',
						onClick: () => handleMentionClick(segment.content),
					},
					`@${segment.content}`
				);
			} else {
				return h(
					'span',
					{
						key: `text-${index}`,
					},
					segment.content
				);
			}
		})
	);
};

// 处理@提及点击事件
const handleMentionClick = (username: string) => {
	console.log('Clicked mention:', username);
	// 这里可以添加更多交互逻辑，比如显示用户信息、发送通知等
	ElMessage.info(`Clicked on @${username}`);
};

const getAuthorInitial = (createBy: string | undefined | null): string => {
	if (createBy && typeof createBy === 'string' && createBy.length > 0) {
		// If it looks like an email, use the first letter of the username part
		if (createBy.includes('@')) {
			const username = createBy.split('@')[0];
			return username.charAt(0).toUpperCase();
		}
		return createBy.charAt(0).toUpperCase();
	}
	return '?';
};
</script>

<style scoped lang="scss">
/* 统一的头部卡片样式 */
.notes-header-card {
	background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%);
	padding: 10px;
	color: white;
	box-shadow: 0 4px 12px rgba(59, 130, 246, 0.2);
	display: flex;
	flex-direction: column;
	gap: 16px;
	cursor: pointer;
	transition: all 0.2s ease;

	&:hover {
		box-shadow: 0 6px 16px rgba(59, 130, 246, 0.3);
		transform: translateY(-1px);
	}

	&.expanded {
		border-bottom-left-radius: 0;
		border-bottom-right-radius: 0;
	}
}

.notes-title {
	font-size: 16px;
	font-weight: 600;
	margin: 0;
}

.notes-subtitle {
	font-size: 14px;
	opacity: 0.9;
	margin-top: 4px;
}

.progress-info {
	text-align: right;
	display: flex;
	flex-direction: column;
	align-items: flex-end;
	gap: 2px;
}

.progress-percentage {
	font-size: 20px;
	font-weight: 700;
	line-height: 1;
}

.progress-label {
	font-size: 12px;
	opacity: 0.8;
	letter-spacing: 0.5px;
}

.expand-icon {
	transition: transform 0.2s ease;

	&.rotated {
		transform: rotate(90deg);
	}
}

.customer-block {
	margin-bottom: 16px;

	&:last-child {
		margin-bottom: 0;
	}
}

/* 暗色主题样式 */
html.dark {
	.notes-header-card {
		background: linear-gradient(135deg, #1e40af 0%, #1e3a8a 100%);
		box-shadow: 0 4px 12px rgba(30, 64, 175, 0.3);
	}
}

/* 响应式样式 */
@media (max-width: 768px) {
	.notes-header-card {
		padding: 16px;
	}

	.progress-info {
		align-items: flex-start;
		text-align: left;
	}
}

.rotate-180 {
	transform: rotate(180deg);
}
</style>
