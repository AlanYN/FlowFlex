<template>
	<div class="questionnaire-detail-view">
		<PrototypeTabs
			v-model="currentTab"
			:tabs="tabsConfig"
			class="editor-tabs"
			content-class="editor-content"
		>
			<el-scrollbar ref="editorScrollbarRef">
				<TabPane value="questions" class="questions-pane">
					<el-card class="editor-card rounded-xl bg-white dark:bg-black-400">
						<!-- 当前分区信息 -->
						<div
							v-if="showSectionManagement"
							class="flex items-center justify-between mb-4"
						>
							<div v-if="!isEditingTitle" class="title-display">
								<div class="text-2xl font-bold">
									{{ currentSection.name || 'Untitled Section' }}
								</div>
								<el-button
									type="primary"
									link
									@click="isEditingTitle = true"
									class="edit-btn"
									:icon="Edit"
								/>
							</div>
							<div v-else class="title-edit">
								<el-input
									v-model="currentSection.name"
									placeholder="Enter section title"
									@keyup.enter="
										isEditingTitle = false;
										updateCurrentSection();
									"
									@keyup.esc="isEditingTitle = false"
									@blur="
										isEditingTitle = false;
										updateCurrentSection();
									"
									ref="titleInputRef"
								/>
							</div>
							<el-dropdown placement="bottom" @command="handleAddContent">
								<el-button :icon="MoreFilled" link />
								<template #dropdown>
									<el-dropdown-menu>
										<el-dropdown-item command="page-break">
											<div class="flex items-center gap-2">
												<Icon
													icon="material-symbols-light:insert-page-break"
													class="drag-icon"
												/>
												<span class="text-xs">
													Add Page Break
												</span>
											</div>
										</el-dropdown-item>
										<el-dropdown-item command="video" divided>
											<div class="flex items-center gap-2">
												<Icon
													icon="mdi:video-outline"
													class="drag-icon"
												/>
												<span class="text-xs">Add Video</span>
											</div>
										</el-dropdown-item>
										<el-dropdown-item command="image">
											<div class="flex items-center gap-2">
												<Icon
													icon="mdi:image-area"
													class="drag-icon"
												/>
												<span class="text-xs">Add Image</span>
											</div>
										</el-dropdown-item>
									</el-dropdown-menu>
								</template>
							</el-dropdown>
						</div>

						<el-form
							v-if="showSectionManagement"
							:model="currentSection"
							label-position="top"
							@submit.prevent
						>
							<div class="current-section-info">
								<el-form-item label="Section Description">
									<el-input
										v-model="currentSection.description"
										placeholder="Enter section description"
										@input="updateCurrentSection"
									/>
								</el-form-item>
							</div>
						</el-form>

						<!-- 问题列表 -->
						<QuestionsList
							:questions="currentSection.questions"
							:question-types="questionTypes"
							:sections="sectionsForJumpRules"
							:current-section-index="currentSectionIndex"
							:setGoToSection="showSectionManagement"
							@remove-question="handleRemoveQuestion"
							@edit-question="handleEditQuestion"
							@drag-end="handleQuestionDragEnd"
							@update-jump-rules="handleUpdateJumpRules"
							@update-question="handleUpdateQuestionFromList"
						/>

						<el-divider />

						<!-- 新问题编辑器 -->
						<QuestionEditor
							ref="questionEditorRef"
							:question-types="questionTypes"
							:pressent-question-type="pressentQuestionType"
							:editing-question="editingQuestion"
							:is-editing="isEditingQuestion"
							@change-question-type="changeQuestionType"
							@add-question="handleAddQuestion"
							@update-question="handleUpdateQuestion"
							@cancel-edit="cancelEditQuestion"
						/>
					</el-card>
				</TabPane>

				<TabPane value="preview" class="preview-pane">
					<PreviewContent
						:questionnaire="previewData"
						:loading="false"
						:workflows="workflows"
						:all-stages="allStages"
					/>
				</TabPane>
			</el-scrollbar>
		</PrototypeTabs>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, nextTick } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Edit, MoreFilled, Plus, Document } from '@element-plus/icons-vue';
import { Icon } from '@iconify/vue';
import PreviewContent from './PreviewContent.vue';
import { PrototypeTabs, TabPane } from '@/components/PrototypeTabs';
import { useAdaptiveScrollbar } from '@/hooks/useAdaptiveScrollbar';
import QuestionEditor from './QuestionEditor.vue';
import QuestionsList from './QuestionsList.vue';
import { Section } from '#/section';

// 引入API
import {
	createQuestionnaire,
	getQuestionnaireDetail,
	updateQuestionnaire,
} from '@/apis/ow/questionnaire';
import { getWorkflows, getAllStages } from '@/apis/ow';
import { triggerFileUpload } from '@/utils/fileUploadUtils';

// Props 定义
interface Props {
	questionnaire?: any;
	questionTypes?: any[];
	workflows?: any[];
	allStages?: any[];
	isEditMode?: boolean;
	questionnaireId?: string;
	saving?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	questionnaire: () => ({
		name: '',
		description: '',
		isActive: true,
		sections: [
			{
				temporaryId: `section-${Date.now()}`,
				name: 'Untitled Section',
				description: '',
				isDefault: true,
				questions: [],
			},
		],
	}),
	questionTypes: () => [],
	workflows: () => [],
	allStages: () => [],
	isEditMode: false,
	questionnaireId: '',
	saving: false,
});

// Emits 定义
const emit = defineEmits<{
	'update:questionnaire': [value: any];
	'save-questionnaire': [];
}>();

const router = useRouter();
const route = useRoute();

// 使用自适应滚动条 hook
const { scrollbarRef: editorScrollbarRef, updateScrollbarHeight: updateEditorScrollbar } =
	useAdaptiveScrollbar();

// 状态管理
const currentTab = ref('questions');
const currentSectionIndex = ref(0);
const isEditingTitle = ref(false);
const titleInputRef = ref();

// Tab配置
const tabsConfig = [
	{
		value: 'questions',
		label: 'Questions',
	},
	{
		value: 'preview',
		label: 'Preview',
	},
];

// 当前分区
const currentSection = computed(() => {
	return props.questionnaire?.sections[currentSectionIndex.value] || props.questionnaire?.sections[0];
});

// 显示控制的计算属性
const showSectionManagement = computed(() => {
	// 有多个分区 OR 存在非默认分区 = 显示分区管理
	return props.questionnaire.sections.some((section) => !section.isDefault);
});

// 预览数据
const previewData = computed(() => {
	return {
		id: props.isEditMode ? props.questionnaireId : `0`,
		name: props.questionnaire.name || 'Untitled Questionnaire',
		title: props.questionnaire.name || 'Untitled Questionnaire',
		description: props.questionnaire.description || '',
		sections: props.questionnaire.sections || [],
		totalQuestions:
			props.questionnaire.sections?.reduce(
				(total: number, section) => total + (section.questions?.length || 0),
				0
			) || 0,
		requiredQuestions:
			props.questionnaire.sections?.reduce(
				(total: number, section) =>
					total + (section.questions?.filter((item) => item.required)?.length || 0),
				0
			) || 0,
		status: 'draft',
		version: '1.0',
		estimatedMinutes: Math.ceil(
			(props.questionnaire.sections?.reduce(
				(total: number, section) => total + (section.questions?.length || 0),
				0
			) || 0) * 0.5
		),
		allowMultipleSubmissions: false,
		isActive: true,
		createBy: 'Current User',
		createDate: new Date().toISOString(),
	};
});

// 为跳转规则转换sections数据格式
const sectionsForJumpRules = computed(() => {
	return props.questionnaire.sections.map((section, index) => ({
		...section,
		name: section.name,
		description: section.description,
		questionIds: section.questions.map((item) => item.temporaryId).filter(Boolean) as string[],
		order: index,
	}));
});

const updateCurrentSection = () => {
	// 更新当前分区信息
	const updatedQuestionnaire = { ...props.questionnaire };
	updatedQuestionnaire.sections[currentSectionIndex.value] = { ...currentSection.value };
	emit('update:questionnaire', updatedQuestionnaire);
};

// 当前分区索引（用于文件上传）
const currentSectionIndexForUpload = ref<number>(0);

const handleAddContent = async (command: string) => {
	switch (command) {
		case 'page-break':
			const updatedQuestionnaire = { ...props.questionnaire };
			updatedQuestionnaire.sections[currentSectionIndex.value].questions.push({
				temporaryId: `page-break-${Date.now()}`,
				type: 'page_break',
				question: 'Page Break',
			});
			emit('update:questionnaire', updatedQuestionnaire);
			break;
		case 'video':
			await handleFileUpload('video');
			break;
		case 'image':
			await handleFileUpload('image');
			break;
	}
};

// 处理文件上传
const handleFileUpload = async (type: 'video' | 'image') => {
	currentSectionIndexForUpload.value = currentSectionIndex.value;

	await triggerFileUpload(type, (result) => {
		if (result.success) {
			const mediaItem = {
				temporaryId: `${type}-${Date.now()}`,
				type: type,
				question: result.fileName!,
				fileUrl: result.fileUrl!,
				required: false,
			};

			const updatedQuestionnaire = { ...props.questionnaire };
			updatedQuestionnaire.sections[currentSectionIndexForUpload.value].questions.push(mediaItem);
			emit('update:questionnaire', updatedQuestionnaire);
		}
	});
};

const handleRemoveQuestion = (index: number) => {
	const updatedQuestionnaire = { ...props.questionnaire };
	updatedQuestionnaire.sections[currentSectionIndex.value].questions.splice(index, 1);
	emit('update:questionnaire', updatedQuestionnaire);
};

const handleQuestionDragEnd = (questions: any[]) => {
	// 更新当前分区的问题列表
	const updatedQuestionnaire = { ...props.questionnaire };
	updatedQuestionnaire.sections[currentSectionIndex.value].questions = questions;
	emit('update:questionnaire', updatedQuestionnaire);
};

const handleUpdateJumpRules = (questionIndex: number, rules: any[]) => {
	// 更新指定问题的跳转规则
	const updatedQuestionnaire = { ...props.questionnaire };
	const question = updatedQuestionnaire.sections[currentSectionIndex.value].questions[questionIndex];
	if (question) {
		question.jumpRules = rules;
		emit('update:questionnaire', updatedQuestionnaire);
	}
};

const pressentQuestionType = ref('short_answer');
const changeQuestionType = (type: string) => {
	pressentQuestionType.value = type;
	// 如果不在编辑模式，更新子组件的问题类型
	if (!isEditingQuestion.value) {
		questionEditorRef.value?.updateQuestionType(type);
	}
};

const handleAddQuestion = (questionData: any) => {
	if (!questionData.question.trim() || !questionData.type) return;

	const question = {
		temporaryId: `question-${Date.now()}`,
		...questionData,
		options: [...questionData.options],
		rows: [...questionData.rows],
		columns: [...questionData.columns],
	};

	const updatedQuestionnaire = { ...props.questionnaire };
	updatedQuestionnaire.sections[currentSectionIndex.value].questions.push(question);
	emit('update:questionnaire', updatedQuestionnaire);
};

// 编辑问题状态
const isEditingQuestion = ref(false);
const editingQuestion = ref<any>(null);
const questionEditorRef = ref<any>(null);

const handleEditQuestion = (index: number) => {
	const question = props.questionnaire.sections[currentSectionIndex.value].questions[index];
	if (question) {
		isEditingQuestion.value = true;
		editingQuestion.value = { ...question };
		pressentQuestionType.value = question.type || 'short_answer';

		// 调用子组件的方法加载编辑数据
		nextTick(() => {
			questionEditorRef.value?.loadEditingData();
		});
	}
};

const cancelEditQuestion = () => {
	isEditingQuestion.value = false;
	editingQuestion.value = null;
	pressentQuestionType.value = 'short_answer';

	// 调用子组件的方法重置表单
	questionEditorRef.value?.resetForm();
	// 恢复当前选中的问题类型
	nextTick(() => {
		questionEditorRef.value?.updateQuestionType(pressentQuestionType.value);
	});
};

const handleUpdateQuestion = (updatedQuestion: any) => {
	const updatedQuestionnaire = { ...props.questionnaire };
	const index = updatedQuestionnaire.sections[currentSectionIndex.value].questions.findIndex(
		(item) => item.temporaryId === updatedQuestion.temporaryId
	);
	if (index !== -1) {
		updatedQuestionnaire.sections[currentSectionIndex.value].questions[index] = updatedQuestion;
		emit('update:questionnaire', updatedQuestionnaire);
	}
	isEditingQuestion.value = false;
	editingQuestion.value = null;
};

const handleUpdateQuestionFromList = (index: number, updatedQuestion: any) => {
	const updatedQuestionnaire = { ...props.questionnaire };
	updatedQuestionnaire.sections[currentSectionIndex.value].questions[index] = updatedQuestion;
	emit('update:questionnaire', updatedQuestionnaire);
};

onMounted(() => {
	nextTick(() => {
		updateEditorScrollbar();
	});
});
</script>

<style scoped lang="scss">
.questionnaire-detail-view {
	height: 100%;
	overflow: hidden;
}

.editor-tabs {
	display: flex;
	flex-direction: column;
	height: 100%;
}

.editor-tabs :deep(.el-tabs__content) {
	flex: 1;
	overflow: hidden;
}

.editor-tabs :deep(.el-tab-pane) {
	height: 100%;
	overflow-y: auto;
}

.editor-card,
.preview-card {
	height: 100%;
	border: 1px solid var(--primary-100);
}

.current-section-info {
	margin-bottom: 1.5rem;
	padding: 1rem;
	background-color: var(--primary-50);
	border: 1px solid var(--primary-100);
	@apply rounded-xl;
}

/* 深色模式支持 */
.dark .current-section-info {
	background-color: var(--primary-700);
	border-color: var(--primary-600);
}

.title-display {
	display: flex;
	align-items: center;
	gap: 0.5rem;
}

.title-display h1 {
	margin: 0;
	flex: 1;
}

.edit-btn {
	:deep(.el-icon) {
		width: 1.5rem !important;
		height: 1.5rem !important;

		svg {
			width: 1.5rem !important;
			height: 1.5rem !important;
		}
	}
}

.edit-btn:hover {
	opacity: 1;
}

.title-edit {
	width: 50%;
}
</style>
