<template>
	<div class="create-questionnaire-container rounded-md">
		<!-- 页面头部 -->
		<QuestionnaireHeader
			:title="pageTitle"
			:description="pageDescription"
			:current-tab="currentTab"
			:saving="saving"
			:is-edit-mode="isEditMode"
			@go-back="handleGoBack"
			@toggle-preview="togglePreview"
			@save-questionnaire="handleSaveQuestionnaire"
		/>

		<!-- 主要内容区域 -->
		<div class="main-content">
			<!-- 加载状态 -->
			<div v-if="loading" class="loading-container">
				<div
					v-loading="loading"
					element-loading-text="Loading questionnaire data..."
					element-loading-background="rgba(0, 0, 0, 0.1)"
					class="loading-content"
				></div>
			</div>

			<!-- 内容网格 -->
			<div v-else class="content-grid">
				<!-- 左侧配置面板 -->
				<div class="config-panel">
					<el-scrollbar ref="configScrollbarRef">
						<el-card class="config-card rounded-md">
							<!-- 基本信息 -->
							<QuestionnaireBasicInfo
								:questionnaire="{
									name: questionnaire.name,
									description: questionnaire.description,
								}"
								@update-questionnaire="updateBasicInfo"
							/>

							<el-divider />

							<!-- 工作流阶段分配 -->
							<WorkflowAssignments
								ref="workflowAssignmentsRef"
								:assignments="initialAssignments"
								:workflows="workflows"
							/>

							<el-divider />

							<!-- 分区管理 -->
							<SectionManager
								:sections="questionnaire.sections"
								:current-section-index="currentSectionIndex"
								@add-section="handleAddSection"
								@remove-section="handleRemoveSection"
								@set-current-section="setCurrentSection"
							/>

							<el-divider />

							<!-- 问题类型 -->
							<QuestionTypesPanel
								:selected-type="pressentQuestionType"
								:question-types="questionTypes"
								@select-type="changeQuestionType"
							/>
						</el-card>
					</el-scrollbar>
				</div>

				<!-- 右侧编辑区域 -->
				<div class="editor-panel">
					<PrototypeTabs
						v-model="currentTab"
						:tabs="tabsConfig"
						class="editor-tabs"
						content-class="editor-content"
					>
						<el-scrollbar ref="editorScrollbarRef">
							<TabPane value="questions" class="questions-pane">
								<el-card class="editor-card rounded-md">
									<!-- 当前分区信息 -->
									<div class="mb-4">
										<div v-if="!isEditingTitle" class="title-display">
											<div class="text-2xl font-bold">
												{{ currentSection.name || 'Untitled Section' }}
											</div>
											<el-button
												type="primary"
												link
												size="large"
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
									</div>

									<el-form :model="currentSection" label-position="top">
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
										:questions="currentSection.items"
										:question-types="questionTypes"
										:sections="sectionsForJumpRules"
										:current-section-index="currentSectionIndex"
										@remove-question="handleRemoveQuestion"
										@edit-question="handleEditQuestion"
										@drag-end="handleQuestionDragEnd"
										@update-jump-rules="handleUpdateJumpRules"
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
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, nextTick } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { ElMessage } from 'element-plus';
import { Edit } from '@element-plus/icons-vue';
import '../styles/errorDialog.css';
import PreviewContent from './components/PreviewContent.vue';
import { PrototypeTabs, TabPane } from '@/components/PrototypeTabs';
import { useAdaptiveScrollbar } from '@/hooks/useAdaptiveScrollbar';
import QuestionnaireHeader from './components/QuestionnaireHeader.vue';
import QuestionnaireBasicInfo from './components/QuestionnaireBasicInfo.vue';
import WorkflowAssignments from './components/WorkflowAssignments.vue';
import SectionManager from './components/SectionManager.vue';
import QuestionTypesPanel from './components/QuestionTypesPanel.vue';
import QuestionEditor from './components/QuestionEditor.vue';
import QuestionsList from './components/QuestionsList.vue';
import { Section } from '#/section';

// 引入API
import {
	createQuestionnaire,
	getQuestionnaireDetail,
	updateQuestionnaire,
} from '@/apis/ow/questionnaire';
import { getWorkflows, getAllStages } from '@/apis/ow';

const router = useRouter();
const route = useRoute();

// 使用自适应滚动条 hook，为左右两个面板分别设置滚动
const { scrollbarRef: configScrollbarRef, updateScrollbarHeight: updateConfigScrollbar } =
	useAdaptiveScrollbar();
const { scrollbarRef: editorScrollbarRef, updateScrollbarHeight: updateEditorScrollbar } =
	useAdaptiveScrollbar();

const debouncedUpdateScrollbars = () => {
	nextTick(() => {
		updateConfigScrollbar();
		updateEditorScrollbar();
	});
};

// 编辑模式相关状态
const isEditMode = computed(() => !!route.query.questionnaireId);
const questionnaireId = computed(() => route.query.questionnaireId as string);
const loading = ref(false);
const isEditingTitle = ref(false);

// 加载问卷数据（编辑模式）
const loadQuestionnaireData = async () => {
	if (!isEditMode.value) return;

	try {
		loading.value = true;
		const response = await getQuestionnaireDetail(questionnaireId.value);

		if (response.success && response.data) {
			const data = response.data;

			// 解析问卷结构
			let structure = data.structureJson;
			if (typeof structure === 'string') {
				try {
					structure = JSON.parse(structure);
				} catch (error) {
					structure = { sections: [] };
				}
			}
			console.log('structure:', structure);
			// 填充问卷基本信息
			questionnaire.name = data.name || '';
			questionnaire.description = data.description || '';
			questionnaire.isActive = data.isActive ?? true;

			initialAssignments.value = data.assignments || [];
			// 填充问卷结构 - 适配API返回的数据结构
			if (structure?.sections && Array.isArray(structure.sections)) {
				questionnaire.sections = structure.sections.map((section: any) => ({
					id: section.id || `section-${Date.now()}-${Math.random()}`,
					name: section.name || 'Untitled Section',
					description: section.description || '',
					// 处理questions字段（API返回的是questions，我们内部使用items）
					items: (section.questions || section.items || []).map((item: any) => ({
						id: item.id || `question-${Date.now()}-${Math.random()}`,
						type: item?.type || 'short_answer',
						question: item.title || item.question || '',
						description: item.description || '',
						required: item.required ?? true,
						options: item.options || [],
						// 恢复网格类型问题的行和列数据
						rows: item.rows || [],
						columns: item.columns || [],
						requireOneResponsePerRow: item.requireOneResponsePerRow || false,
						// 恢复线性量表相关字段
						min: item.min,
						max: item.max,
						minLabel: item.minLabel || '',
						maxLabel: item.maxLabel || '',
						// 恢复跳转规则
						jumpRules: item.jumpRules || [],
						...item,
					})),
				}));
			}

			// 确保至少有一个分区
			if (questionnaire.sections.length === 0) {
				questionnaire.sections = [
					{
						id: `section-${Date.now()}`,
						name: 'Untitled Section',
						description: '',
						items: [],
					},
				];
			}

			// 设置当前分区索引
			currentSectionIndex.value = 0;
		} else {
			router.push('/onboard/questionnaire');
		}
	} catch (error) {
		router.push('/onboard/questionnaire');
	} finally {
		loading.value = false;
	}
};

// 获取工作流列表
const fetchWorkflows = async () => {
	try {
		const response = await getWorkflows();
		if (response.code === '200') {
			workflows.value = response.data || [];
		} else {
			console.error('Failed to fetch workflows:', response.msg);
			workflows.value = [];
		}
	} catch (error) {
		workflows.value = [];
	}
};

// 更新基本信息
const updateBasicInfo = (basicInfo: { name: string; description: string }) => {
	questionnaire.name = basicInfo.name;
	questionnaire.description = basicInfo.description;
};

// 子组件引用
const workflowAssignmentsRef = ref<any>(null);
const questionEditorRef = ref<any>(null);

// 工作流分配数据（用于初始化子组件）
const initialAssignments = ref<Array<{ workflowId: string; stageId: string }>>([]);

// 问题类型定义
const questionTypes = [
	{
		id: 'short_answer',
		name: 'Short answer',
		icon: 'EditPen',
	},
	{
		id: 'paragraph',
		name: 'Paragraph',
		icon: 'Document',
	},
	{
		id: 'multiple_choice',
		name: 'Multiple choice',
		icon: 'CircleCheck',
		isNew: true,
	},
	{
		id: 'checkboxes',
		name: 'Checkboxes',
		icon: 'Select',
	},
	{
		id: 'dropdown',
		name: 'Dropdown',
		icon: 'ArrowDown',
	},
	{
		id: 'file_upload',
		name: 'File upload',
		icon: 'Upload',
	},
	{
		id: 'linear_scale',
		name: 'Linear scale',
		icon: 'Histogram',
	},
	{
		id: 'rating',
		name: 'Rating',
		icon: 'Star',
		isNew: true,
	},
	{
		id: 'multiple_choice_grid',
		name: 'Multiple choice grid',
		icon: 'Grid',
	},
	{
		id: 'checkbox_grid',
		name: 'Checkbox grid',
		icon: 'Grid',
	},
	{
		id: 'date',
		name: 'Date',
		icon: 'Calendar',
	},
	{
		id: 'time',
		name: 'Time',
		icon: 'Clock',
	},
];

// 工作流数据
const workflows = ref<any[]>([]);
const allStages = ref<any[]>([]);

// 状态管理
const currentTab = ref('questions');
const currentSectionIndex = ref(0);
const saving = ref(false);

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

// 问卷数据
const questionnaire = reactive({
	name: '',
	description: '',
	isActive: true,
	sections: [
		{
			id: `section-${Date.now()}`,
			name: 'Untitled Section',
			description: '',
			items: [] as Array<{
				id: string;
				type: string;
				question: string;
				description: string;
				required: boolean;
				options: Array<{ id: string; value: string; label: string }>;
				rows?: Array<{ id: string; label: string }>;
				columns?: Array<{ id: string; label: string }>;
				requireOneResponsePerRow?: boolean;
				min?: number;
				max?: number;
				minLabel?: string;
				maxLabel?: string;
				jumpRules?: any[];
				iconType?: string;
			}>,
		},
	] as Section[],
});

// 当前分区
const currentSection = computed(() => {
	return questionnaire.sections[currentSectionIndex.value] || questionnaire.sections[0];
});

// 页面标题和描述
const pageTitle = computed(() => {
	if (isEditMode.value && questionnaire.name) {
		return questionnaire.name;
	}
	return isEditMode.value ? 'Edit Questionnaire' : 'Create New Questionnaire';
});

const pageDescription = computed(() => {
	if (isEditMode.value && questionnaire.name) {
		return "Gather information about customer's inbound and outbound processes";
	}
	return isEditMode.value
		? 'Modify your questionnaire with sections and questions'
		: 'Design your questionnaire with sections and questions';
});

// 预览数据
const previewData = computed(() => {
	// 获取第一个工作流阶段分配用于显示

	return {
		id: isEditMode.value ? questionnaireId.value : `0`,
		name: questionnaire.name || 'Untitled Questionnaire',
		title: questionnaire.name || 'Untitled Questionnaire',
		description: questionnaire.description || '',
		sections: questionnaire.sections || [],
		totalQuestions:
			questionnaire.sections?.reduce(
				(total: number, section) => total + (section.items?.length || 0),
				0
			) || 0,
		requiredQuestions:
			questionnaire.sections?.reduce(
				(total: number, section) =>
					total + (section.items?.filter((item) => item.required)?.length || 0),
				0
			) || 0,
		status: 'draft',
		version: '1.0',
		estimatedMinutes: Math.ceil(
			(questionnaire.sections?.reduce(
				(total: number, section) => total + (section.items?.length || 0),
				0
			) || 0) * 0.5
		),
		allowMultipleSubmissions: false,
		isActive: true,
		createBy: 'Current User',
		createDate: new Date().toISOString(),
		// 添加工作流阶段分配信息
		assignments: workflowAssignmentsRef.value?.getAssignments() || [],
	};
});

// 为跳转规则转换sections数据格式
const sectionsForJumpRules = computed(() => {
	return questionnaire.sections.map((section, index) => ({
		id: section.id,
		name: section.name,
		description: section.description,
		questions: section.items.map((item) => item.id),
		order: index,
		items: section.items,
	}));
});

// 方法定义
const handleGoBack = () => {
	router.push('/onboard/questionnaire');
};

const togglePreview = () => {
	currentTab.value = currentTab.value === 'questions' ? 'preview' : 'questions';
};

const handleAddSection = () => {
	questionnaire.sections.push({
		id: `section-${Date.now()}`,
		name: 'Untitled Section',
		description: '',
		items: [],
	});
	currentSectionIndex.value = questionnaire.sections.length - 1;
};

const handleRemoveSection = (index: number) => {
	if (questionnaire.sections.length <= 1) {
		ElMessage.warning('At least one section must be kept');
		return;
	}

	questionnaire.sections.splice(index, 1);
	if (currentSectionIndex.value >= questionnaire.sections.length) {
		currentSectionIndex.value = questionnaire.sections.length - 1;
	}
};

const setCurrentSection = (index: number) => {
	cancelEditQuestion();
	currentSectionIndex.value = index;
};

const updateCurrentSection = () => {
	// 更新当前分区信息
	questionnaire.sections[currentSectionIndex.value] = { ...currentSection.value };
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
		id: `question-${Date.now()}`,
		...questionData,
		options: [...questionData.options],
		rows: [...questionData.rows],
		columns: [...questionData.columns],
	};

	questionnaire.sections[currentSectionIndex.value].items.push(question);
};

const handleRemoveQuestion = (index: number) => {
	questionnaire.sections[currentSectionIndex.value].items.splice(index, 1);
};

const handleQuestionDragEnd = (questions: any[]) => {
	// 更新当前分区的问题列表
	questionnaire.sections[currentSectionIndex.value].items = questions;
};

const handleUpdateJumpRules = (questionIndex: number, rules: any[]) => {
	console.log('rules:', rules);
	// 更新指定问题的跳转规则
	const question = questionnaire.sections[currentSectionIndex.value].items[questionIndex];
	if (question) {
		question.jumpRules = rules;
	}
};

const handleSaveQuestionnaire = async () => {
	if (!questionnaire.name.trim()) {
		return;
	}

	try {
		saving.value = true;

		// 从子组件获取工作流分配数据
		const assignments = workflowAssignmentsRef.value?.getAssignments() || [];

		// 构建问卷结构JSON - 适配API期望的数据结构
		const structureJson = JSON.stringify({
			sections: questionnaire.sections.map((section) => ({
				id: section.id,
				name: section.name,
				description: section.description,
				// API期望的是questions字段，不是items
				questions: section.items.map((item) => ({
					id: item.id,
					title: item.question, // API期望的是title字段，不是question
					type: item?.type,
					description: item.description,
					required: item.required,
					options: item.options || [],
					// 网格类型问题的行和列数据
					rows: item.rows || [],
					columns: item.columns || [],
					requireOneResponsePerRow: item.requireOneResponsePerRow || false,
					// 线性量表相关字段
					min: item.min,
					max: item.max,
					minLabel: item.minLabel || '',
					maxLabel: item.maxLabel || '',
					// 跳转规则
					jumpRules: item.jumpRules || [],
					iconType: item.iconType || 'star',
				})),
			})),
		});

		// 计算问题统计
		const totalQuestions = questionnaire.sections.reduce(
			(total: number, section) => total + section.items.length,
			0
		);
		const requiredQuestions = questionnaire.sections.reduce(
			(total: number, section) =>
				total + (section.items?.filter((item) => item.required)?.length || 0),
			0
		);

		const params = {
			name: questionnaire.name,
			description: questionnaire.description,
			isActive: questionnaire.isActive,
			structureJson,
			totalQuestions,
			requiredQuestions,
			estimatedMinutes: Math.max(1, Math.ceil(totalQuestions * 0.5)),
			category: 'custom',
			type: 'questionnaire',
			assignments: assignments.filter((assignment) => assignment.workflowId),
		};

		let result;
		if (isEditMode.value) {
			// 编辑模式：更新问卷
			result = await updateQuestionnaire(questionnaireId.value, params);
		} else {
			// 创建模式：创建新问卷
			result = await createQuestionnaire(params);
		}

		if (result.success || result.code === '200') {
			ElMessage.success(
				isEditMode.value
					? 'Questionnaire updated successfully'
					: 'Questionnaire created successfully'
			);
			router.push('/onboard/questionnaire');
		}
	} finally {
		saving.value = false;
	}
};

// 编辑问题状态
const isEditingQuestion = ref(false);
const editingQuestion = ref<any>(null);

const handleEditQuestion = (index: number) => {
	const question = questionnaire.sections[currentSectionIndex.value].items[index];
	if (question) {
		isEditingQuestion.value = true;
		editingQuestion.value = { ...question };
		pressentQuestionType.value = question.type;

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
	const index = questionnaire.sections[currentSectionIndex.value].items.findIndex(
		(item) => item.id === updatedQuestion.id
	);
	if (index !== -1) {
		questionnaire.sections[currentSectionIndex.value].items[index] = updatedQuestion;
	}
	isEditingQuestion.value = false;
	editingQuestion.value = null;
};

// 获取所有stages
const fetchAllStages = async () => {
	try {
		const response = await getAllStages();
		if (response.code === '200') {
			allStages.value = response.data || [];
		}
	} catch (error) {
		console.error('Failed to fetch all stages:', error);
	}
};

onMounted(async () => {
	// 初始化数据 - 先加载问卷数据和工作流
	await Promise.all([loadQuestionnaireData(), fetchWorkflows(), fetchAllStages()]);

	// 获取stages的逻辑已经移动到WorkflowAssignments.vue中
	// 当WorkflowAssignments组件挂载后会自动加载stages数据

	debouncedUpdateScrollbars();
});
</script>

<style scoped lang="scss">
.create-questionnaire-container {
	display: flex;
	flex-direction: column;
	background-color: var(--el-bg-color-page);
}

.main-content {
	flex: 1;
	padding: 1.5rem 0;
	overflow: hidden;
}

.loading-container {
	height: 100%;
	display: flex;
	align-items: center;
	justify-content: center;
}

.loading-content {
	width: 100%;
	height: 400px;
	display: flex;
	align-items: center;
	justify-content: center;
}

.content-grid {
	display: grid;
	grid-template-columns: 400px 1fr;
	gap: 1.5rem;
	height: 100%;
	width: 100%;
}

.config-panel {
	height: 100%;
	overflow: hidden;
}

.config-card {
	height: fit-content;
	border: 1px solid var(--primary-100);
}

.config-section {
	margin-bottom: 1.5rem;
}

.config-section:last-child {
	margin-bottom: 0;
}

.section-title {
	font-size: 1.125rem;
	font-weight: 600;
	color: var(--primary-700);
	margin: 0 0 1rem 0;
}

.editor-panel {
	height: 100%;
	overflow: hidden;
}

.editor-tabs {
	display: flex;
	flex-direction: column;
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
	border-radius: 0.375rem;
	border: 1px solid var(--primary-100);
}

/* 深色模式支持 */
.dark .config-card {
	border-color: var(--primary-600);
	background-color: var(--primary-800);
}

.dark .section-title {
	color: var(--primary-200);
}

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
	opacity: 0.6;
	transition: opacity 0.2s;
}

.edit-btn:hover {
	opacity: 1;
}

.title-edit {
	width: 50%;
}
</style>
