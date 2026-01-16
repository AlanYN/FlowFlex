<template>
	<div class="create-questionnaire-container rounded-xl">
		<!-- 页面头部 -->
		<PageHeader
			:title="pageTitle"
			:description="pageDescription"
			:show-back-button="true"
			@go-back="handleGoBack"
		>
			<template #actions>
				<el-button
					type="primary"
					class="page-header-btn page-header-btn-primary"
					@click="handleSaveQuestionnaire"
					:loading="saving"
					:icon="Document"
					v-if="
						isEditMode
							? functionPermission(ProjectPermissionEnum.question.update)
							: functionPermission(ProjectPermissionEnum.question.create)
					"
				>
					{{ isEditMode ? 'Update Questionnaire' : 'Save Questionnaire' }}
				</el-button>
			</template>
		</PageHeader>

		<!-- 主要内容区域 -->
		<div class="main-content" v-loading="loading">
			<!-- 内容网格 -->
			<div class="content-grid">
				<!-- 左侧配置面板 -->
				<div class="config-panel">
					<el-scrollbar ref="configScrollbarRef">
						<el-card class="config-card">
							<!-- 基本信息 -->
							<QuestionnaireBasicInfo
								ref="questionnaireBasicInfoRef"
								:questionnaire="{
									name: questionnaire.name,
									description: questionnaire.description,
								}"
								@update-questionnaire="updateBasicInfo"
							/>

							<el-divider />

							<!-- 分区管理 -->
							<SectionManager
								v-if="showSectionManagement"
								:sections="questionnaire.sections"
								:current-section-index="currentSectionIndex"
								@add-section="handleAddSection"
								@remove-section="handleRemoveSection"
								@set-current-section="setCurrentSection"
								@drag-end="handleSectionDragEnd"
								@question-drop="handleQuestionDropToSection"
							/>

							<!-- 添加分区按钮（仅在简单模式下显示） -->
							<div v-else class="add-section-simple">
								<el-button
									type="primary"
									:icon="Plus"
									@click="handleAddSection"
									class="w-full"
									v-if="functionPermission(ProjectPermissionEnum.question.create)"
								>
									Add Section
								</el-button>
								<p
									class="text-xs section-hint mt-2 text-center"
									v-if="functionPermission(ProjectPermissionEnum.question.create)"
								>
									Organize your questions into sections
								</p>
							</div>

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
								<el-card class="editor-card">
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
										<el-dropdown
											placement="bottom"
											@command="handleAddContent"
											v-if="
												functionPermission(
													ProjectPermissionEnum.question.update
												)
											"
										>
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
										@drag-start="handleQuestionDragStart"
										@update-jump-rules="handleUpdateJumpRules"
										@update-question="handleUpdateQuestionFromList"
									/>

									<el-divider />

									<!-- 新问题编辑器 -->
									<QuestionEditor
										v-if="
											functionPermission(
												ProjectPermissionEnum.question.create
											)
										"
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
import { ref, reactive, computed, onMounted, nextTick, useTemplateRef } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Edit, MoreFilled, Plus, Document } from '@element-plus/icons-vue';
import PreviewContent from './components/PreviewContent.vue';
import { PrototypeTabs, TabPane } from '@/components/PrototypeTabs';
import { useAdaptiveScrollbar } from '@/hooks/useAdaptiveScrollbar';
import PageHeader from '@/components/global/PageHeader/index.vue';
import QuestionnaireBasicInfo from './components/QuestionnaireBasicInfo.vue';
import SectionManager from './components/SectionManager.vue';
import QuestionTypesPanel from './components/QuestionTypesPanel.vue';
import QuestionEditor from './components/QuestionEditor.vue';
import QuestionsList from './components/QuestionsList.vue';
import { Section } from '#/section';
import { functionPermission } from '@/hooks';
import { ProjectPermissionEnum } from '@/enums/permissionEnum';

// 引入API
import {
	createQuestionnaire,
	getQuestionnaireDetail,
	updateQuestionnaire,
} from '@/apis/ow/questionnaire';
import { getWorkflows, getAllStages } from '@/apis/ow';
import { triggerFileUpload } from '@/utils/fileUploadUtils';

const router = useRouter();
const route = useRoute();

// 使用自适应滚动条 hook，为左右两个面板分别设置滚动
const { scrollbarRef: configScrollbarRef, updateScrollbarHeight: updateConfigScrollbar } =
	useAdaptiveScrollbar();
const { scrollbarRef: editorScrollbarRef, updateScrollbarHeight: updateEditorScrollbar } =
	useAdaptiveScrollbar();

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

			// 填充问卷结构 - 适配API返回的数据结构
			if (structure?.sections && Array.isArray(structure.sections)) {
				questionnaire.sections = structure.sections.map((section: any) => ({
					...section,
					temporaryId: section?.temporaryId ? section.temporaryId : section.id,
					name: section.name || 'Untitled Section',
					description: section.description || '',
					// 处理 isDefault 字段的兼容性
					isDefault: !!section.isDefault,
					// 处理questions字段（API返回的是questions，我们内部也使用questions）
					questions: (section.questions || []).map((item: any) => ({
						...item,
						temporaryId: item?.temporaryId ? item.temporaryId : item.id,
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
						// 恢复问题附加属性（包含上传的文件）
						questionProps: item.questionProps || {},
					})),
				}));
			}

			// 确保至少有一个分区
			if (questionnaire.sections.length === 0) {
				questionnaire.sections = [
					{
						temporaryId: `section-${Date.now()}`,
						name: 'Untitled Section',
						description: '',
						questions: [],
						isDefault: true, // 如果没有分区，创建默认分区
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
		nextTick(() => {
			updateConfigScrollbar();
			updateEditorScrollbar();
		});
	}
};

// 获取工作流列表
const fetchWorkflows = async () => {
	try {
		const response = await getWorkflows();
		if (response.code === '200') {
			workflows.value = response.data || [];
			const defaultWorkflow = workflows.value.find((item) => item.isDefault);
			if (defaultWorkflow) {
				defaultWorkflow.name = '⭐ ' + defaultWorkflow.name;
			}
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

const questionEditorRef = ref<any>(null);

// 问题类型定义
const questionTypes = [
	{
		id: 'short_answer',
		name: 'Short Answer',
		icon: 'mdi-light:pencil',
	},
	{
		id: 'paragraph',
		name: 'Paragraph',
		icon: 'fluent:text-wrap-20-regular',
	},
	{
		id: 'multiple_choice',
		name: 'Multiple Choice',
		icon: 'mdi:checkbox-marked-circle-outline',
	},
	{
		id: 'checkboxes',
		name: 'Checkboxes',
		icon: 'material-symbols-light:check-box-outline-sharp',
	},
	{
		id: 'dropdown',
		name: 'Dropdown',
		icon: 'ic:outline-arrow-drop-down-circle',
	},
	{
		id: 'file_upload',
		name: 'File Upload',
		icon: 'ic:outline-drive-folder-upload',
	},
	{
		id: 'linear_scale',
		name: 'Linear Scale',
		icon: 'material-symbols:scan-outline-sharp',
	},
	{
		id: 'rating',
		name: 'Rating',
		icon: 'ic:twotone-star-rate',
	},
	{
		id: 'multiple_choice_grid',
		name: 'Multiple Choice Grid',
		icon: 'tabler:grid-dots',
	},
	{
		id: 'checkbox_grid',
		name: 'Checkbox Grid',
		icon: 'gridicons:grid',
	},
	{
		id: 'date',
		name: 'Date',
		icon: 'ic:baseline-calendar-month',
	},
	{
		id: 'time',
		name: 'Time',
		icon: 'ic:outline-access-alarms',
	},
	{
		id: 'short_answer_grid',
		name: 'Short Answer Grid',
		icon: 'ph:grid-nine-light',
		isNew: true,
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
			temporaryId: `section-${Date.now()}`,
			name: 'Untitled Section',
			description: '',
			isDefault: true, // 标识为默认分区
			questions: [] as Array<{
				temporaryId: string;
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
	return questionnaire?.sections[currentSectionIndex.value] || questionnaire?.sections[0];
});

// 显示控制的计算属性
const showSectionManagement = computed(() => {
	// 有多个分区 OR 存在非默认分区 = 显示分区管理
	return questionnaire.sections.some((section) => !section.isDefault);
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
				(total: number, section) => total + (section.questions?.length || 0),
				0
			) || 0,
		requiredQuestions:
			questionnaire.sections?.reduce(
				(total: number, section) =>
					total + (section.questions?.filter((item) => item.required)?.length || 0),
				0
			) || 0,
		status: 'draft',
		version: '1.0',
		estimatedMinutes: Math.ceil(
			(questionnaire.sections?.reduce(
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
	return questionnaire.sections.map((section, index) => ({
		...section,
		name: section.name,
		description: section.description,
		questionIds: section.questions.map((item) => item.temporaryId).filter(Boolean) as string[],
		order: index,
	}));
});

// 方法定义
const handleGoBack = () => {
	router.push('/onboard/questionnaire');
};

const handleAddSection = async () => {
	// 检查是否是首次启用分区管理（只有默认分区的情况）
	const hasOnlyDefaultSection =
		questionnaire.sections.length === 1 && questionnaire.sections[0].isDefault;

	if (hasOnlyDefaultSection) {
		// 检查默认分区是否有问题
		const hasQuestions = questionnaire.sections[0].questions.length > 0;

		if (hasQuestions) {
			try {
				await ElMessageBox.confirm(
					'You are about to enable section management. All existing questions will be moved to the new section.',
					'Enable Section Management',
					{
						confirmButtonText: 'Continue',
						cancelButtonText: 'Cancel',
						type: 'warning',
						customClass: 'section-confirm-dialog',
					}
				);
			} catch {
				// 用户取消操作
				return;
			}
		}

		// 首次点击：将默认分区转换为用户创建的分区
		questionnaire.sections[0].isDefault = false;
		// 可以选择给分区一个更好的默认名称
		if (questionnaire.sections[0].name === 'Untitled Section') {
			questionnaire.sections[0].name = 'Section 1';
		}

		if (hasQuestions) {
			ElMessage.success(
				'Section management enabled. Your questions have been moved to Section 1.'
			);
		}
	} else {
		// 后续点击：正常添加新分区
		questionnaire.sections.push({
			temporaryId: `section-${Date.now()}`,
			name: `Section ${questionnaire.sections.length + 1}`,
			description: '',
			questions: [],
			// 不设置 isDefault，默认为 undefined（非默认分区）
		});
		currentSectionIndex.value = questionnaire.sections.length - 1;
	}
};

const handleRemoveSection = async (index: number) => {
	const sectionToRemove = questionnaire.sections[index];
	const questionCount = sectionToRemove.questions.length;
	const sectionName = sectionToRemove.name || 'Untitled Section';
	const sectionId = sectionToRemove.temporaryId;

	// 检查是否有其他分区的问题跳转到当前分区
	const jumpReferences: Array<{ sectionName: string; questionTitle: string }> = [];

	questionnaire.sections.forEach((section, sectionIndex) => {
		// 跳过当前要删除的分区
		if (sectionIndex === index) return;

		section.questions.forEach((question) => {
			if (question.jumpRules && question.jumpRules.length > 0) {
				const hasJumpToTarget = question.jumpRules.some(
					(rule: any) => rule.targetSectionId === sectionId
				);

				if (hasJumpToTarget) {
					jumpReferences.push({
						sectionName: section.name || 'Untitled Section',
						questionTitle: question.question || 'Untitled Question',
					});
				}
			}
		});
	});

	// 如果有跳转引用，显示警告并阻止删除
	if (jumpReferences.length > 0) {
		const message = `
			<div style="text-align: left;">
				<p style="margin-bottom: 12px;">This section cannot be deleted because other questions have jump rules targeting it.</p>
				<div style="padding-left: 8px; line-height: 1.8;">
					${jumpReferences
						.map(
							(ref, idx) =>
								`<div>${idx + 1}. ${ref.sectionName} <span class="text-gray-500">(${
									ref.questionTitle
								})</span></div>`
						)
						.join('')}
				</div>
			</div>
		`;

		ElMessageBox.alert(message, 'Cannot Delete Section', {
			confirmButtonText: 'OK',
			type: 'warning',
			dangerouslyUseHTMLString: true,
			customClass: 'section-delete-blocked-dialog',
		});
		return;
	}

	// 构建确认消息
	let confirmMessage = `Are you sure you want to delete "${sectionName}"?`;
	if (questionCount > 0) {
		confirmMessage += ` This section contains ${questionCount} question${
			questionCount > 1 ? 's' : ''
		} that will be permanently deleted.`;
	}

	try {
		await ElMessageBox.confirm(confirmMessage, 'Delete Section', {
			confirmButtonText: 'Delete',
			cancelButtonText: 'Cancel',
			type: 'warning',
			customClass: 'section-delete-dialog',
		});
	} catch {
		// 用户取消操作
		return;
	}

	// 检查删除后的情况
	if (questionnaire.sections.length === 1) {
		// 如果只剩一个分区，将其转换为默认分区（回到简单模式）
		questionnaire.sections[0].isDefault = true;
		questionnaire.sections[0].name = 'Untitled Section';
		questionnaire.sections[0].questions = [];
		questionnaire.sections[0].temporaryId = `section-${Date.now()}`;
		questionnaire.sections[0].description = '';
		currentSectionIndex.value = 0;
		ElMessage.success('Returned to simple mode. You can start adding questions directly.');
	} else {
		questionnaire.sections.splice(index, 1);
		// 调整当前分区索引
		if (currentSectionIndex.value >= questionnaire.sections.length) {
			currentSectionIndex.value = questionnaire.sections.length - 1;
		}
		ElMessage.success(`Section "${sectionName}" has been deleted.`);
	}
};

const setCurrentSection = (index: number) => {
	cancelEditQuestion();
	currentSectionIndex.value = index;
};

const handleSectionDragEnd = (
	reorderedSections: Section[],
	dragInfo: { oldIndex: number; newIndex: number }
) => {
	// 更新问卷的分区顺序
	questionnaire.sections = reorderedSections;

	// 计算新的当前分区索引
	const { newIndex } = dragInfo;

	// 确保索引在有效范围内
	if (currentSectionIndex.value >= questionnaire.sections.length) {
		currentSectionIndex.value = questionnaire.sections.length - 1;
	} else if (currentSectionIndex.value < 0) {
		currentSectionIndex.value = 0;
	} else {
		currentSectionIndex.value = newIndex;
	}

	// 取消当前的问题编辑状态（如果有的话）
	cancelEditQuestion();
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
		temporaryId: `question-${Date.now()}`,
		...questionData,
		options: [...questionData.options],
		rows: [...questionData.rows],
		columns: [...questionData.columns],
	};

	questionnaire.sections[currentSectionIndex.value].questions.push(question);
};

// 当前分区索引（用于文件上传）
const currentSectionIndexForUpload = ref<number>(0);

const handleAddContent = async (command: string) => {
	switch (command) {
		case 'page-break':
			questionnaire.sections[currentSectionIndex.value].questions.push({
				temporaryId: `page-break-${Date.now()}`,
				type: 'page_break',
				question: 'Page Break',
			});
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

			questionnaire.sections[currentSectionIndexForUpload.value].questions.push(mediaItem);
		}
	});
};

const handleRemoveQuestion = (index: number) => {
	questionnaire.sections[currentSectionIndex.value].questions.splice(index, 1);
};

const handleQuestionDragEnd = (questions: any[]) => {
	// 更新当前分区的问题列表
	// 注意：只有在列表内排序时才调用此函数，跨组件拖拽不会触发此函数
	// 确保只更新当前 section 的问题，而不是所有问题
	if (questions && Array.isArray(questions)) {
		// 使用展开运算符创建新数组，确保响应式更新
		questionnaire.sections[currentSectionIndex.value].questions = [...questions];
	}
};

const handleUpdateJumpRules = (questionIndex: number, rules: any[]) => {
	console.log('rules:', rules);
	// 更新指定问题的跳转规则
	const question = questionnaire.sections[currentSectionIndex.value].questions[questionIndex];
	if (question) {
		question.jumpRules = rules;
	}
};

const questionnaireBasicInfoRef = useTemplateRef('questionnaireBasicInfoRef');
const handleSaveQuestionnaire = async () => {
	const valid = await questionnaireBasicInfoRef.value?.validate();
	if (!valid) {
		return;
	}

	try {
		saving.value = true;

		// 构建问卷结构JSON - 适配API期望的数据结构
		const structureJson = JSON.stringify({
			sections: questionnaire.sections.map((section) => ({
				...section,
				temporaryId: section?.temporaryId ? section.temporaryId : section.id,
				name: section.name,
				description: section.description,
				// API期望的是questions字段
				questions: section.questions.map((item) => ({
					...item,
					temporaryId: item?.temporaryId ? item.temporaryId : item.id,
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
					// 问题附加属性（包含上传的文件）
					questionProps: item.questionProps || {},
				})),
			})),
		});

		// 计算问题统计
		const totalQuestions = questionnaire.sections.reduce(
			(total: number, section) => total + section.questions.length,
			0
		);
		const requiredQuestions = questionnaire.sections.reduce(
			(total: number, section) =>
				total + (section.questions?.filter((item) => item.required)?.length || 0),
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
	const question = questionnaire.sections[currentSectionIndex.value].questions[index];
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
	const index = questionnaire.sections[currentSectionIndex.value].questions.findIndex(
		(item) => item.temporaryId === updatedQuestion.temporaryId
	);
	if (index !== -1) {
		questionnaire.sections[currentSectionIndex.value].questions[index] = updatedQuestion;
	}
	isEditingQuestion.value = false;
	editingQuestion.value = null;
};

const handleUpdateQuestionFromList = (index: number, updatedQuestion: any) => {
	questionnaire.sections[currentSectionIndex.value].questions[index] = updatedQuestion;
};

// 拖拽相关状态
const draggingQuestion = ref<any>(null);
const draggingQuestionIndex = ref<number>(-1);
const draggingFromSectionIndex = ref<number>(-1);
const isProcessingDrop = ref<boolean>(false);

// 处理问题拖拽开始
const handleQuestionDragStart = (question: any, questionIndex: number) => {
	draggingQuestion.value = { ...question };
	draggingQuestionIndex.value = questionIndex;
	draggingFromSectionIndex.value = currentSectionIndex.value;
};

// 处理问题拖拽到分区
const handleQuestionDropToSection = async (targetSectionIndex: number) => {
	if (!draggingQuestion.value || draggingQuestionIndex.value === -1) {
		return;
	}
	// 防止重复处理
	if (isProcessingDrop.value) {
		return;
	}
	isProcessingDrop.value = true;

	const question = draggingQuestion.value;
	const sourceSectionIndex = draggingFromSectionIndex.value;

	// 检查是否是拖拽到同一个分区
	if (sourceSectionIndex === targetSectionIndex) {
		// 重置拖拽状态
		draggingQuestion.value = null;
		draggingQuestionIndex.value = -1;
		draggingFromSectionIndex.value = -1;
		isProcessingDrop.value = false;
		return;
	}

	// 检查问题是否有跳转规则
	const hasJumpRules = question.jumpRules && question.jumpRules.length > 0;

	if (hasJumpRules) {
		// 检查跳转规则是否引用了当前分区或其他分区
		const affectedRules = question.jumpRules.filter((rule: any) => {
			// 检查规则是否引用了源分区或目标分区
			const sourceSection = questionnaire.sections[sourceSectionIndex];
			const targetSection = questionnaire.sections[targetSectionIndex];

			return (
				rule.targetSectionId === sourceSection?.temporaryId ||
				rule.targetSectionId === targetSection?.temporaryId
			);
		});

		if (affectedRules.length > 0) {
			// 构建提示消息
			const ruleCount = question.jumpRules.length;
			const affectedCount = affectedRules.length;
			const targetSectionName =
				questionnaire.sections[targetSectionIndex]?.name || 'target section';

			let confirmMessage = `The question "${question.question}" has ${ruleCount} jump rule${
				ruleCount > 1 ? 's' : ''
			} configured.`;
			if (affectedCount > 0) {
				confirmMessage += `\n\n${affectedCount} of these rule${
					affectedCount > 1 ? 's' : ''
				} reference section-related settings that may need adjustment after moving to "${targetSectionName}".`;
			}
			confirmMessage += `\n\nDo you want to move this question to "${targetSectionName}"? The jump rules will be preserved, but you may need to review them.`;

			try {
				await ElMessageBox.confirm(confirmMessage, 'Move Question to Section', {
					confirmButtonText: 'Move',
					cancelButtonText: 'Cancel',
					type: 'warning',
					customClass: 'question-move-dialog',
				});
			} catch {
				// 用户取消操作，重置拖拽状态
				draggingQuestion.value = null;
				draggingQuestionIndex.value = -1;
				draggingFromSectionIndex.value = -1;
				isProcessingDrop.value = false;
				return;
			}
		}
	}

	// 执行移动操作
	try {
		// 从源分区找到并移除问题（使用 temporaryId 来查找，因为索引可能已变化）
		const sourceSection = questionnaire.sections[sourceSectionIndex];
		if (!sourceSection) {
			ElMessage.warning('Source section not found');
			return;
		}

		const questionIndex = sourceSection.questions.findIndex(
			(q) => q.temporaryId === question.temporaryId
		);

		if (questionIndex === -1) {
			// 问题不存在，可能已经被移动或删除
			ElMessage.warning('Question not found in source section');
			return;
		}

		const questionToMove = sourceSection.questions.splice(questionIndex, 1)[0];

		// 确保问题数据正确
		if (!questionToMove || questionToMove.temporaryId !== question.temporaryId) {
			console.error('Error: Question mismatch during move', {
				questionToMove,
				question,
				questionIndex,
			});
			// 如果出错，将问题还原
			sourceSection.questions.splice(questionIndex, 0, questionToMove);
			return;
		}
		// 调整跳转规则（如果需要）
		if (hasJumpRules && questionToMove.jumpRules) {
			// 更新跳转规则中的目标分区名称
			questionToMove.jumpRules = questionToMove.jumpRules.map((rule: any) => {
				const targetSection = questionnaire.sections.find(
					(s) => s.temporaryId === rule.targetSectionId
				);
				if (targetSection) {
					return {
						...rule,
						targetSectionName: targetSection.name,
					};
				}
				return rule;
			});
		}

		// 添加到目标分区（在底部添加）
		const targetSection = questionnaire.sections[targetSectionIndex];
		if (!targetSection) {
			ElMessage.warning('Target section not found');
			// 如果目标分区不存在，将问题还原到源分区
			if (questionIndex !== -1) {
				sourceSection.questions.splice(questionIndex, 0, questionToMove);
			}
			return;
		}

		// 确保只添加一个问题到目标分区的底部，而不是替换整个列表
		// 检查问题是否已经在目标分区中（避免重复添加）
		const alreadyInTarget = targetSection.questions.some(
			(q) => q.temporaryId === questionToMove.temporaryId
		);
		if (!alreadyInTarget) {
			targetSection.questions.push(questionToMove);
		}

		// 切换到目标分区
		// 使用 setCurrentSection 确保正确切换并取消编辑状态
		// setCurrentSection(targetSectionIndex);

		// 等待 DOM 更新完成

		// 强制更新滚动条高度
		updateConfigScrollbar();
		updateEditorScrollbar();
	} catch (error) {
		console.error('Error moving question:', error);
		ElMessage.error('Failed to move question');
	} finally {
		// 重置拖拽状态
		draggingQuestion.value = null;
		draggingQuestionIndex.value = -1;
		draggingFromSectionIndex.value = -1;
		isProcessingDrop.value = false;
	}
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
});
</script>

<style scoped lang="scss">
.create-questionnaire-container {
	display: flex;
	flex-direction: column;
}

.main-content {
	flex: 1;
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
}

.current-section-info {
	margin-bottom: 1.5rem;
	padding: 1rem;
	background-color: var(--black-300);
	@apply rounded-xl;
}

.dark .section-title {
	color: var(--primary-200);
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

.add-section-simple {
	padding: 1rem;
	text-align: center;
}

.add-section-simple .el-button {
	margin-bottom: 0.5rem;
}

/* 自定义确认对话框样式 */
:deep(.section-confirm-dialog) {
	.el-message-box__message {
		color: var(--el-text-color-primary);
		line-height: 1.6;
	}
}

:deep(.section-delete-dialog) {
	.el-message-box__message {
		color: var(--el-text-color-primary);
		line-height: 1.6;
	}
}

.section-hint {
	color: var(--el-text-color-secondary);
}
</style>
