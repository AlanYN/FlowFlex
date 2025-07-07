<template>
	<div class="create-questionnaire-container rounded-md">
		<!-- 页面头部 -->
		<div class="page-header rounded-md">
			<div class="header-content">
				<div class="header-left">
					<el-button
						type="primary"
						link
						class="back-button"
						@click="handleGoBack"
						size="small"
					>
						<el-icon class="back-icon"><Back /></el-icon>
					</el-button>
					<div class="header-info">
						<h1 class="page-title">
							{{ pageTitle }}
						</h1>
						<p class="page-description">
							{{ pageDescription }}
						</p>
					</div>
				</div>
				<div class="header-actions">
					<el-button
						type="default"
						class="preview-button"
						@click="togglePreview"
						:icon="currentTab === 'questions' ? View : Edit"
					>
						{{ currentTab === 'questions' ? 'Preview' : 'Edit' }}
					</el-button>
					<el-button
						type="primary"
						class="save-button"
						@click="handleSaveQuestionnaire"
						:loading="saving"
						:icon="Document"
					>
						{{ isEditMode ? 'Update Questionnaire' : 'Save Questionnaire' }}
					</el-button>
				</div>
			</div>
		</div>

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
							<div class="config-section">
								<h3 class="section-title">Basic Information</h3>
								<el-form :model="questionnaire" label-position="top">
									<el-form-item label="Questionnaire Title" required>
										<el-input
											v-model="questionnaire.name"
											placeholder="Enter questionnaire title"
										/>
									</el-form-item>
									<el-form-item label="Description">
										<el-input
											v-model="questionnaire.description"
											type="textarea"
											:rows="3"
											placeholder="Enter questionnaire description"
										/>
									</el-form-item>
									<el-form-item label="Workflow">
										<el-select
											v-model="questionnaire.workflowId"
											placeholder="Select workflow"
											style="width: 100%"
											@change="handleWorkflowChange"
										>
											<el-option
												v-for="workflow in workflows"
												:key="workflow.id"
												:label="workflow.name"
												:value="workflow.id"
											/>
										</el-select>
									</el-form-item>
									<el-form-item label="Stage">
										<el-select
											v-model="questionnaire.stageId"
											placeholder="Select stage"
											style="width: 100%"
											:disabled="!questionnaire.workflowId || stagesLoading"
											:loading="stagesLoading"
										>
											<el-option
												v-for="stage in workflowStages"
												:key="stage.id"
												:label="stage.name"
												:value="stage.id"
											/>
										</el-select>
									</el-form-item>
								</el-form>
							</div>

							<el-divider />

							<!-- 分区管理 -->
							<div class="config-section">
								<div class="section-header">
									<h3 class="section-title">Sections</h3>
									<el-button
										type="primary"
										size="small"
										@click="handleAddSection"
										:icon="Plus"
									>
										Add Section
									</el-button>
								</div>
								<el-scrollbar max-height="300px" class="sections-list-container">
									<div class="sections-list">
										<div
											v-for="(section, index) in questionnaire.sections"
											:key="section.id"
											class="section-item"
											:class="{ active: currentSectionIndex === index }"
											@click="setCurrentSection(index)"
										>
											<div class="section-info">
												<div class="section-name">{{ section.title }}</div>
												<div class="section-count">
													{{ section.items.length }} items
												</div>
											</div>
											<el-button
												v-if="questionnaire.sections.length > 1"
												type="primary"
												link
												size="small"
												@click.stop="handleRemoveSection(index)"
												:icon="Delete"
												class="delete-btn"
											/>
										</div>
									</div>
								</el-scrollbar>
							</div>

							<el-divider />

							<!-- 问题类型 -->
							<div class="config-section">
								<h3 class="section-title">Question Types</h3>
								<div class="question-types-grid">
									<div
										v-for="type in questionTypes"
										:key="type.id"
										class="question-type-item"
										@click="setNewQuestionType(type.id)"
										:class="{ active: newQuestion.type === type.id }"
									>
										<el-icon class="type-icon">
											<component :is="type.icon" />
										</el-icon>
										<div class="type-info">
											<div class="type-content">
												<span class="type-name">{{ type.name }}</span>
												<el-tag
													v-if="type.isNew"
													size="small"
													type="success"
													class="type-new-tag"
												>
													New
												</el-tag>
											</div>
										</div>
									</div>
								</div>
							</div>
						</el-card>
					</el-scrollbar>
				</div>

				<!-- 右侧编辑区域 -->
				<div class="editor-panel">
					<el-scrollbar ref="editorScrollbarRef">
						<PrototypeTabs
							v-model="currentTab"
							:tabs="tabsConfig"
							class="editor-tabs"
							content-class="editor-content"
						>
							<TabPane value="questions" class="questions-pane">
								<el-card class="editor-card rounded-md">
									<!-- 分区编辑器 -->
									<div class="section-header">
										<h3 class="section-title">Section Editor</h3>
									</div>

									<!-- 当前分区信息 -->
									<div class="current-section-info">
										<el-form :model="currentSection" label-position="top">
											<el-row :gutter="16">
												<el-col :span="12">
													<el-form-item label="Section Title">
														<el-input
															v-model="currentSection.title"
															placeholder="Enter section title"
															@input="updateCurrentSection"
														/>
													</el-form-item>
												</el-col>
												<el-col :span="12">
													<el-form-item label="Section Description">
														<el-input
															v-model="currentSection.description"
															placeholder="Enter section description"
															@input="updateCurrentSection"
														/>
													</el-form-item>
												</el-col>
											</el-row>
										</el-form>
									</div>

									<!-- 问题列表 -->
									<div class="questions-list">
										<draggable
											v-model="currentSection.items"
											item-key="id"
											handle=".drag-handle"
											@change="handleQuestionDragEnd"
											ghost-class="ghost-question"
											class="questions-draggable"
											:animation="300"
										>
											<template #item="{ element: item, index }">
												<div class="question-item">
													<div class="question-header">
														<div class="question-left">
															<div class="drag-handle">
																<DragIcon class="drag-icon" />
															</div>
															<div class="question-info">
																<div class="question-text">
																	{{ item.question }}
																</div>
																<div class="question-meta">
																	<el-tag
																		size="small"
																		class="card-tag"
																	>
																		{{
																			getQuestionTypeName(
																				item.type
																			)
																		}}
																	</el-tag>
																	<el-tag
																		v-if="item.required"
																		size="small"
																		type="danger"
																	>
																		Required
																	</el-tag>
																</div>
															</div>
														</div>
														<el-button
															type="danger"
															link
															@click="handleRemoveQuestion(index)"
															:icon="Delete"
															class="delete-question-btn"
														/>
													</div>
													<div
														v-if="item.description"
														class="question-description"
													>
														{{ item.description }}
													</div>
													<div
														v-if="
															item.options && item.options.length > 0
														"
														class="question-options"
													>
														<div class="options-label">Options:</div>
														<div class="options-list">
															<el-tag
																v-for="option in item.options"
																:key="option.id"
																size="small"
																class="option-tag"
															>
																{{ option.label }}
															</el-tag>
														</div>
													</div>
												</div>
											</template>
										</draggable>

										<!-- 空状态 -->
										<div
											v-if="currentSection.items.length === 0"
											class="empty-questions"
										>
											<el-empty description="No questions yet">
												<template #image>
													<el-icon size="48" color="var(--el-color-info)">
														<Document />
													</el-icon>
												</template>
											</el-empty>
										</div>
									</div>

									<el-divider />

									<!-- 新问题编辑器 -->
									<div class="new-question-editor">
										<h4 class="editor-title">Add New Question</h4>
										<el-form :model="newQuestion" label-position="top">
											<el-row :gutter="16">
												<el-col :span="12">
													<el-form-item label="Question Type">
														<el-select
															v-model="newQuestion.type"
															placeholder="Select question type"
															style="width: 100%"
														>
															<template #prefix>
																<div
																	v-if="newQuestion.type"
																	class="type-option"
																>
																	<el-icon class="type-icon">
																		<component
																			:is="
																				getQuestionTypeIcon(
																					newQuestion.type
																				)
																			"
																		/>
																	</el-icon>
																</div>
															</template>
															<el-option
																v-for="type in questionTypes"
																:key="type.id"
																:label="type.name"
																:value="type.id"
															>
																<div class="type-option">
																	<el-icon class="type-icon">
																		<component
																			:is="type.icon"
																		/>
																	</el-icon>
																	<span class="type-option-name">
																		{{ type.name }}
																	</span>
																	<el-tag
																		v-if="type.isNew"
																		size="small"
																		type="success"
																		class="type-option-tag"
																	>
																		New
																	</el-tag>
																</div>
															</el-option>
														</el-select>
													</el-form-item>
												</el-col>
												<el-col :span="12">
													<el-form-item label="Required">
														<el-switch v-model="newQuestion.required" />
													</el-form-item>
												</el-col>
											</el-row>
											<el-form-item label="Question Text" required>
												<el-input
													v-model="newQuestion.question"
													placeholder="Enter question text"
												/>
											</el-form-item>
											<el-form-item label="Question Description">
												<el-input
													v-model="newQuestion.description"
													type="textarea"
													:rows="2"
													placeholder="Enter question description or help text"
												/>
											</el-form-item>

											<!-- 选项编辑器 -->
											<div
												v-if="needsOptions(newQuestion.type)"
												class="options-editor"
											>
												<div class="options-section">
													<div class="options-header">
														<label class="options-label">Options</label>
													</div>

													<div class="options-input-grid">
														<div class="option-input-item">
															<label class="input-label">Value</label>
															<el-input
																v-model="newOption.value"
																placeholder="Option value"
																class="option-input"
															/>
														</div>
														<div class="option-input-item">
															<label class="input-label">Label</label>
															<div class="label-input-group">
																<el-input
																	v-model="newOption.label"
																	placeholder="Option label"
																	class="option-input"
																/>
																<el-button
																	type="primary"
																	@click="handleAddOption"
																	:disabled="
																		!newOption.value ||
																		!newOption.label
																	"
																	class="add-option-btn"
																>
																	Add
																</el-button>
															</div>
														</div>
													</div>
												</div>

												<!-- 当前选项列表 -->
												<div
													v-if="newQuestion.options.length > 0"
													class="current-options"
												>
													<div class="current-options-container">
														<label class="current-options-label">
															Current Options
														</label>
														<div class="options-list">
															<div
																v-for="option in newQuestion.options"
																:key="option.id"
																class="option-item"
															>
																<div class="option-content">
																	<span class="option-label-text">
																		{{ option.label }}
																	</span>
																	<span class="option-value-text">
																		({{ option.value }})
																	</span>
																</div>
																<el-button
																	type="danger"
																	link
																	size="small"
																	@click="
																		handleRemoveOption(
																			option.id
																		)
																	"
																	:icon="Delete"
																	class="delete-option-btn"
																/>
															</div>
														</div>
													</div>
												</div>
											</div>

											<!-- 网格编辑器 -->
											<div
												v-if="needsGrid(newQuestion.type)"
												class="grid-editor"
											>
												<div class="grid-section">
													<div class="grid-header">
														<label class="grid-label">
															Grid Configuration
														</label>
													</div>

													<!-- 行列编辑区域 - 左右布局 -->
													<div class="grid-editor-layout">
														<!-- 左侧：行编辑 -->
														<div class="grid-column-editor">
															<div class="grid-column-header">
																<h4 class="grid-column-title">
																	Rows
																</h4>
															</div>

															<!-- 行列表 -->
															<div class="grid-items-container">
																<div
																	v-for="(
																		row, index
																	) in newQuestion.rows"
																	:key="row.id"
																	class="grid-editor-item"
																>
																	<span class="grid-item-number">
																		{{ index + 1 }}.
																	</span>
																	<span class="grid-item-label">
																		{{ row.label }}
																	</span>
																	<el-button
																		type="danger"
																		text
																		size="small"
																		@click="
																			handleRemoveRow(row.id)
																		"
																		class="grid-delete-btn"
																	>
																		<el-icon>
																			<Close />
																		</el-icon>
																	</el-button>
																</div>

																<!-- 添加行输入 -->
																<div class="grid-add-item">
																	<span class="grid-item-number">
																		{{
																			newQuestion.rows
																				.length + 1
																		}}.
																	</span>
																	<el-input
																		v-model="newRow.label"
																		placeholder="Add row"
																		class="grid-add-input"
																		@keyup.enter="handleAddRow"
																	/>
																	<el-button
																		type="primary"
																		size="small"
																		@click="handleAddRow"
																		:disabled="
																			!newRow.label.trim()
																		"
																	>
																		Add
																	</el-button>
																</div>
															</div>
														</div>

														<!-- 右侧：列编辑 -->
														<div class="grid-column-editor">
															<div class="grid-column-header">
																<h4 class="grid-column-title">
																	Columns
																</h4>
															</div>

															<!-- 列列表 -->
															<div class="grid-items-container">
																<div
																	v-for="column in newQuestion.columns"
																	:key="column.id"
																	class="grid-editor-item"
																>
																	<el-icon
																		class="grid-column-icon"
																	>
																		<Check />
																	</el-icon>
																	<span class="grid-item-label">
																		{{ column.label }}
																	</span>
																	<el-button
																		type="danger"
																		text
																		size="small"
																		@click="
																			handleRemoveColumn(
																				column.id
																			)
																		"
																		class="grid-delete-btn"
																	>
																		<el-icon>
																			<Close />
																		</el-icon>
																	</el-button>
																</div>

																<!-- 添加列输入 -->
																<div class="grid-add-item">
																	<el-icon
																		class="grid-column-icon"
																	>
																		<Check />
																	</el-icon>
																	<el-input
																		v-model="newColumn.label"
																		placeholder="Add column"
																		class="grid-add-input"
																		@keyup.enter="
																			handleAddColumn
																		"
																	/>
																	<el-button
																		type="primary"
																		size="small"
																		@click="handleAddColumn"
																		:disabled="
																			!newColumn.label.trim()
																		"
																	>
																		Add
																	</el-button>
																</div>
															</div>
														</div>
													</div>

													<!-- 网格选项 -->
													<div class="grid-options">
														<el-checkbox
															v-if="
																newQuestion.type === 'checkbox_grid'
															"
															v-model="
																newQuestion.requireOneResponsePerRow
															"
															class="grid-option-checkbox"
														>
															Require a response in each row
														</el-checkbox>
													</div>
												</div>
											</div>

											<!-- 线性量表配置 -->
											<div
												v-if="needsLinearScale(newQuestion.type)"
												class="linear-scale-editor"
											>
												<div class="linear-scale-section">
													<div class="linear-scale-header">
														<label class="linear-scale-label">
															Linear Scale Configuration
														</label>
													</div>

													<!-- 范围配置 -->
													<div class="scale-range-config">
														<div class="range-selectors">
															<div class="range-item">
																<label class="range-label">
																	From
																</label>
																<el-select
																	v-model="newQuestion.min"
																	placeholder="Select minimum"
																	class="range-select"
																>
																	<el-option
																		v-for="num in [0, 1]"
																		:key="num"
																		:label="num.toString()"
																		:value="num"
																	/>
																</el-select>
															</div>
															<div class="range-separator">to</div>
															<div class="range-item">
																<label class="range-label">
																	To
																</label>
																<el-select
																	v-model="newQuestion.max"
																	placeholder="Select maximum"
																	class="range-select"
																>
																	<el-option
																		v-for="num in [
																			2, 3, 4, 5, 6, 7, 8, 9,
																			10,
																		]"
																		:key="num"
																		:label="num.toString()"
																		:value="num"
																	/>
																</el-select>
															</div>
														</div>
													</div>

													<!-- 标签配置 -->
													<div class="scale-labels-config">
														<div class="labels-grid">
															<div class="label-item">
																<label class="label-title">
																	{{ newQuestion.min }}
																</label>
																<el-input
																	v-model="newQuestion.minLabel"
																	placeholder="Left label (optional)"
																	class="label-input"
																/>
															</div>
															<div class="label-item">
																<label class="label-title">
																	{{ newQuestion.max }}
																</label>
																<el-input
																	v-model="newQuestion.maxLabel"
																	placeholder="Right label (optional)"
																	class="label-input"
																/>
															</div>
														</div>
													</div>
												</div>
											</div>

											<el-button
												type="primary"
												@click="handleAddQuestion"
												:disabled="
													!newQuestion.question || !newQuestion.type
												"
												:icon="Plus"
												class="add-question-btn"
											>
												Add Question
											</el-button>
										</el-form>
									</div>
								</el-card>
							</TabPane>

							<TabPane value="preview" class="preview-pane">
								<PreviewContent :questionnaire="previewData" :loading="false" />
							</TabPane>
						</PrototypeTabs>
					</el-scrollbar>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, nextTick } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { ElMessage } from 'element-plus';
import '../styles/errorDialog.css';
import PreviewContent from './components/PreviewContent.vue';
import { Back, View, Edit, Document, Plus, Delete, Close, Check } from '@element-plus/icons-vue';
import { PrototypeTabs, TabPane } from '@/components/PrototypeTabs';
import { useAdaptiveScrollbar } from '@/hooks/useAdaptiveScrollbar';
import draggable from 'vuedraggable';
import DragIcon from '@assets/svg/publicPage/drag.svg';

// 引入API
import {
	createQuestionnaire,
	getQuestionnaireDetail,
	updateQuestionnaire,
} from '@/apis/ow/questionnaire';
import { getWorkflows, getStagesByWorkflow } from '@/apis/ow';

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
const stagesLoading = ref(false); // 新增：stages加载状态

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

			// 填充问卷基本信息
			questionnaire.name = data.name || '';
			questionnaire.description = data.description || '';
			questionnaire.workflowId = data.workflowId || '';
			questionnaire.stageId = data.stageId || '';
			questionnaire.isActive = data.isActive ?? true;

			// 填充问卷结构 - 适配API返回的数据结构
			if (structure?.sections && Array.isArray(structure.sections)) {
				questionnaire.sections = structure.sections.map((section: any) => ({
					id: section.id || `section-${Date.now()}-${Math.random()}`,
					title: section.title || 'Untitled Section',
					description: section.description || '',
					// 处理questions字段（API返回的是questions，我们内部使用items）
					items: (section.questions || section.items || []).map((item: any) => ({
						id: item.id || `question-${Date.now()}-${Math.random()}`,
						type: mapApiTypeToInternalType(item.type || 'short_answer'),
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
					})),
				}));
			}

			// 确保至少有一个分区
			if (questionnaire.sections.length === 0) {
				questionnaire.sections = [
					{
						id: `section-${Date.now()}`,
						title: 'Untitled Section',
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

// 获取阶段列表 - 修改为根据workflowId获取
const fetchStages = async (workflowId?: string) => {
	if (!workflowId) {
		workflowStages.value = [];
		return;
	}

	try {
		stagesLoading.value = true;
		const response = await getStagesByWorkflow(workflowId);
		if (response.code === '200') {
			workflowStages.value = response.data || [];
		} else {
			workflowStages.value = [];
		}
	} catch (error) {
		workflowStages.value = [];
	} finally {
		stagesLoading.value = false;
	}
};

// 监听workflow变化
const handleWorkflowChange = async (workflowId: string) => {
	// 清空当前选择的stage
	questionnaire.stageId = '';
	// 获取新的stages
	await fetchStages(workflowId);
};

// 问题类型定义
const questionTypes = [
	{
		id: 'short_answer',
		name: 'Short answer',
		icon: 'EditPen',
		description: 'Single line text input',
	},
	{
		id: 'paragraph',
		name: 'Paragraph',
		icon: 'Document',
		description: 'Multi-line text input',
	},
	{
		id: 'multiple_choice',
		name: 'Multiple choice',
		icon: 'CircleCheck',
		description: 'Choose one option',
	},
	{
		id: 'checkboxes',
		name: 'Checkboxes',
		icon: 'Select',
		description: 'Choose multiple options',
	},
	{
		id: 'dropdown',
		name: 'Dropdown',
		icon: 'ArrowDown',
		description: 'Select from dropdown',
	},
	{
		id: 'file_upload',
		name: 'File upload',
		icon: 'Upload',
		description: 'File attachment',
	},
	{
		id: 'linear_scale',
		name: 'Linear scale',
		icon: 'Histogram',
		description: 'Rate on a scale',
	},
	{
		id: 'rating',
		name: 'Rating',
		icon: 'Star',
		description: 'Rate with stars',
		isNew: true,
	},
	{
		id: 'multiple_choice_grid',
		name: 'Multiple choice grid',
		icon: 'Grid',
		description: 'Multiple choices in a grid',
	},
	{
		id: 'checkbox_grid',
		name: 'Checkbox grid',
		icon: 'Grid',
		description: 'Single choice in a grid',
	},
	{
		id: 'date',
		name: 'Date',
		icon: 'Calendar',
		description: 'Date picker',
	},
	{
		id: 'time',
		name: 'Time',
		icon: 'Clock',
		description: 'Time picker',
	},
];

// 工作流阶段定义
const workflowStages = ref<any[]>([]);

// 工作流数据
const workflows = ref<any[]>([]);

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
	workflowId: '',
	stageId: '',
	isActive: true,
	sections: [
		{
			id: `section-${Date.now()}`,
			title: 'Untitled Section',
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
			}>,
		},
	],
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
	// 查找workflow和stage的名称
	const workflow = workflows.value.find((w) => w.id === questionnaire.workflowId);
	const stage = workflowStages.value.find((s) => s.id === questionnaire.stageId);

	return {
		id: isEditMode.value ? questionnaireId.value : `0`,
		name: questionnaire.name || 'Untitled Questionnaire',
		title: questionnaire.name || 'Untitled Questionnaire',
		description: questionnaire.description || '',
		workflowId: questionnaire.workflowId,
		stageId: questionnaire.stageId,
		workflowName: workflow?.name || '',
		stageName: stage?.name || '',
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
	};
});

// 新问题数据
const newQuestion = reactive({
	type: 'short_answer',
	question: '',
	description: '',
	required: true,
	options: [] as { id: string; value: string; label: string }[],
	rows: [] as { id: string; label: string }[],
	columns: [] as { id: string; label: string }[],
	requireOneResponsePerRow: false,
	// 线性量表相关字段
	min: 1,
	max: 5,
	minLabel: '',
	maxLabel: '',
});

// 新选项数据
const newOption = reactive({
	value: '',
	label: '',
});

// 新行数据
const newRow = reactive({
	label: '',
});

// 新列数据
const newColumn = reactive({
	label: '',
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
		title: 'Untitled Section',
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
	currentSectionIndex.value = index;
};

const updateCurrentSection = () => {
	// 更新当前分区信息
	questionnaire.sections[currentSectionIndex.value] = { ...currentSection.value };
};

const setNewQuestionType = (type: string) => {
	newQuestion.type = type;
	// 清空选项和网格数据
	newQuestion.options = [];
	newQuestion.rows = [];
	newQuestion.columns = [];
	newQuestion.requireOneResponsePerRow = false;

	// 重置线性量表字段
	newQuestion.min = 1;
	newQuestion.max = 5;
	newQuestion.minLabel = '';
	newQuestion.maxLabel = '';

	// 清空输入框
	newOption.value = '';
	newOption.label = '';
	newRow.label = '';
	newColumn.label = '';
};

const needsOptions = (type: string) => {
	return ['multiple_choice', 'checkboxes', 'dropdown'].includes(type);
};

const needsGrid = (type: string) => {
	return ['multiple_choice_grid', 'checkbox_grid'].includes(type);
};

const needsLinearScale = (type: string) => {
	return type === 'linear_scale';
};

const handleAddOption = () => {
	if (!newOption.value.trim() || !newOption.label.trim()) return;

	newQuestion.options.push({
		id: `option-${Date.now()}`,
		value: newOption.value,
		label: newOption.label,
	});

	newOption.value = '';
	newOption.label = '';
};

const handleRemoveOption = (id: string) => {
	newQuestion.options = newQuestion.options.filter((option) => option.id !== id);
};

const handleAddRow = () => {
	if (!newRow.label.trim()) {
		return;
	}

	const newRowData = {
		id: `row-${Date.now()}`,
		label: newRow.label,
	};

	newQuestion.rows.push(newRowData);
	newRow.label = '';
};

const handleRemoveRow = (id: string) => {
	newQuestion.rows = newQuestion.rows.filter((row) => row.id !== id);
};

const handleAddColumn = () => {
	if (!newColumn.label.trim()) {
		return;
	}

	const newColumnData = {
		id: `column-${Date.now()}`,
		label: newColumn.label,
	};

	newQuestion.columns.push(newColumnData);
	newColumn.label = '';
};

const handleRemoveColumn = (id: string) => {
	newQuestion.columns = newQuestion.columns.filter((column) => column.id !== id);
};

const handleAddQuestion = () => {
	if (!newQuestion.question.trim() || !newQuestion.type) return;

	const question = {
		id: `question-${Date.now()}`,
		...newQuestion,
		options: [...newQuestion.options],
		rows: [...newQuestion.rows],
		columns: [...newQuestion.columns],
	};

	questionnaire.sections[currentSectionIndex.value].items.push(question);

	// 重置新问题表单，但保持问题类型不变
	newQuestion.question = '';
	newQuestion.description = '';
	newQuestion.required = true;
	newQuestion.options = [];
	newQuestion.rows = [];
	newQuestion.columns = [];
	newQuestion.requireOneResponsePerRow = false;
	// 重置线性量表字段
	newQuestion.min = 1;
	newQuestion.max = 5;
	newQuestion.minLabel = '';
	newQuestion.maxLabel = '';
	// 不重置 newQuestion.type，保持当前选择的类型
};

const handleRemoveQuestion = (index: number) => {
	questionnaire.sections[currentSectionIndex.value].items.splice(index, 1);
};

const handleQuestionDragEnd = () => {
	// 拖拽结束后，问题顺序已经通过v-model自动更新
	// 这里可以添加额外的逻辑，比如保存到服务器
};

const getQuestionTypeName = (type: string) => {
	const questionType = questionTypes.find((t) => t.id === type);
	return questionType ? questionType.name : type;
};

const getQuestionTypeIcon = (type: string) => {
	const questionType = questionTypes.find((t) => t.id === type);
	return questionType ? questionType.icon : 'Document';
};

const handleSaveQuestionnaire = async () => {
	if (!questionnaire.name.trim()) {
		return;
	}

	try {
		saving.value = true;

		// 构建问卷结构JSON - 适配API期望的数据结构
		const structureJson = JSON.stringify({
			sections: questionnaire.sections.map((section) => ({
				id: section.id,
				title: section.title,
				description: section.description,
				// API期望的是questions字段，不是items
				questions: section.items.map((item) => ({
					id: item.id,
					title: item.question, // API期望的是title字段，不是question
					type: mapInternalTypeToApiType(item.type),
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
			workflowId: questionnaire.workflowId,
			stageId: questionnaire.stageId,
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

// 问题类型映射函数
const mapApiTypeToInternalType = (apiType: string): string => {
	const typeMapping: Record<string, string> = {
		text: 'short_answer',
		email: 'short_answer',
		phone: 'short_answer',
		textarea: 'paragraph',
		select: 'dropdown',
		radio: 'multiple_choice',
		checkbox: 'checkboxes',
		file: 'file_upload',
		date: 'date',
		time: 'time',
		datetime: 'datetime',
		number: 'short_answer',
		url: 'short_answer',
	};
	return typeMapping[apiType] || apiType;
};

// 内部类型映射到API类型
const mapInternalTypeToApiType = (internalType: string): string => {
	const typeMapping: Record<string, string> = {
		short_answer: 'text',
		paragraph: 'textarea',
		dropdown: 'select',
		multiple_choice: 'radio',
		checkboxes: 'checkbox',
		file_upload: 'file',
		date: 'date',
		time: 'time',
		datetime: 'datetime',
	};
	return typeMapping[internalType] || internalType;
};

onMounted(async () => {
	// 初始化数据 - 先加载问卷数据和工作流，不预加载stages
	await Promise.all([loadQuestionnaireData(), fetchWorkflows()]);

	// 如果是编辑模式且已有workflowId，则加载对应的stages
	if (isEditMode.value && questionnaire.workflowId) {
		await fetchStages(questionnaire.workflowId);
	}

	debouncedUpdateScrollbars();
});
</script>

<style scoped lang="scss">
.create-questionnaire-container {
	display: flex;
	flex-direction: column;
	background-color: var(--el-bg-color-page);
}

.page-header {
	background: linear-gradient(135deg, var(--primary-50) 0%, var(--primary-100) 100%);
	border-bottom: 1px solid var(--primary-200);
	padding: 1.5rem 2rem;
}

.header-content {
	display: flex;
	justify-content: space-between;
	align-items: center;
	width: 100%;
}

.header-left {
	display: flex;
	align-items: center;
	gap: 1rem;
}

.back-button {
	background-color: transparent;
	border: none;
	color: var(--primary-700);
	padding: 0.5rem;
	margin-right: 1rem;
	transition: all 0.2s;
	min-height: 2rem;
	display: flex;
	align-items: center;
	justify-content: center;
}

.back-button:hover {
	background-color: var(--primary-200);
	color: var(--primary-700);
}

.back-icon {
	font-size: 1.25rem;
	width: 1.25rem;
	height: 1.25rem;
}

.header-info {
	display: flex;
	flex-direction: column;
}

.page-title {
	font-size: 1.875rem;
	font-weight: 700;
	color: var(--primary-800);
	margin: 0;
}

.page-description {
	color: var(--primary-600);
	margin: 0.25rem 0 0 0;
}

.header-actions {
	display: flex;
	gap: 0.5rem;
}

.preview-button {
	border-color: var(--primary-300);
	color: var(--primary-700);
	background-color: transparent;
}

.preview-button:hover {
	background-color: var(--primary-100);
	border-color: var(--primary-400);
}

.save-button {
	background-color: var(--primary-600);
	border-color: var(--primary-600);
}

.save-button:hover {
	background-color: var(--primary-700);
	border-color: var(--primary-700);
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

.section-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	margin-bottom: 1rem;
}

.sections-list-container {
	width: 100%;
}

.sections-list {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
	padding-right: 10px;
}

.section-item {
	display: flex;
	justify-content: space-between;
	align-items: center;
	padding: 0.75rem;
	border: 1px solid var(--primary-200);
	border-radius: 0.375rem;
	cursor: pointer;
	transition: all 0.2s;
}

.section-item:hover {
	border-color: var(--primary-300);
	background-color: var(--primary-50);
}

.section-item.active {
	border-color: var(--primary-500);
	background-color: var(--primary-50);
}

.section-info {
	flex: 1;
	min-width: 0;
}

.section-name {
	font-size: 0.875rem;
	font-weight: 500;
	color: var(--primary-800);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	flex: 1;
	min-width: 0;
	line-height: 1.25;
	@apply dark:text-primary-200;
}

.section-count {
	font-size: 0.75rem;
	color: var(--primary-500);
}

.question-types-grid {
	display: grid;
	grid-template-columns: repeat(2, 1fr);
	gap: 0.5rem;
}

.question-type-item {
	display: flex;
	align-items: center;
	padding: 0.75rem;
	border: 1px solid var(--primary-100);
	border-radius: 0.375rem;
	cursor: pointer;
	transition: all 0.2s;
	min-height: 3rem;
	width: 100%;
	box-sizing: border-box;
	@apply dark:border-black-200;
}

.question-type-item:hover {
	background-color: var(--primary-50);
	border-color: var(--primary-300);
	@apply dark:bg-primary-800 dark:border-primary-500;
}

.question-type-item.active {
	background-color: var(--primary-100);
	border-color: var(--primary-500);
	@apply dark:bg-primary-700 dark:border-primary-400;
}

.type-icon {
	color: var(--primary-600);
	margin-right: 0.5rem;
}

.type-info {
	flex: 1;
	min-width: 0;
}

.type-content {
	display: flex;
	align-items: center;
	gap: 0.375rem;
	min-height: 1.5rem;
}

.type-name {
	font-size: 0.875rem;
	font-weight: 500;
	color: var(--primary-800);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	flex: 1;
	min-width: 0;
	line-height: 1.25;
	@apply dark:text-primary-200;
}

.type-new-tag {
	flex-shrink: 0;
	font-size: 0.625rem;
	height: 1.125rem;
	line-height: 1;
	padding: 0.125rem 0.25rem;
}

.editor-panel {
	height: 100%;
	overflow: hidden;
}

.editor-tabs {
	height: 100%;
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

.questions-list {
	margin-bottom: 1.5rem;
}

.questions-draggable {
	display: flex;
	flex-direction: column;
	gap: 0.75rem;
}

.question-item {
	padding: 0.75rem;
	border: 1px solid var(--primary-200);
	border-radius: 0.375rem;
	background-color: white;
	transition: all 0.2s ease;
}

.question-item:hover {
	border-color: var(--primary-300);
	box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
}

.question-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	margin-bottom: 0.5rem;
}

.question-left {
	display: flex;
	align-items: center;
	gap: 0.75rem;
	flex: 1;
	min-width: 0;
}

.drag-handle {
	display: flex;
	align-items: center;
	justify-content: center;
	width: 1.5rem;
	height: 1.5rem;
	cursor: move;
	color: var(--primary-400);
	transition: color 0.2s;
	flex-shrink: 0;
}

.drag-handle:hover {
	color: var(--primary-600);
}

.drag-handle:active {
	cursor: grabbing;
}

.drag-icon {
	width: 1rem;
	height: 1rem;
}

.question-info {
	flex: 1;
	min-width: 0;
}

.question-text {
	font-weight: 500;
	color: var(--primary-800);
	word-break: break-word;
}

.question-meta {
	display: flex;
	gap: 0.5rem;
	flex-wrap: wrap;
}

.card-tag {
	background-color: var(--primary-50);
	color: var(--primary-700);
	border-color: var(--primary-200);
	@apply dark:bg-primary-800 dark:text-primary-200 dark:border-primary-600;
}

.question-description {
	font-size: 0.875rem;
	color: var(--el-text-color-regular);
}

.question-options {
	margin-top: 0.5rem;
}

.options-label {
	font-size: 0.75rem;
	font-weight: 500;
	color: var(--primary-700);
	margin-bottom: 0.25rem;
}

.options-list {
	display: flex;
	flex-wrap: wrap;
	gap: 0.25rem;
}

.option-tag {
	background-color: var(--primary-100);
	color: var(--primary-700);
	border-color: var(--primary-200);
}

.empty-questions {
	text-align: center;
	padding: 2rem;
}

.new-question-editor {
	padding: 1rem;
	background-color: var(--primary-50);
	border-radius: 0.375rem;
	border: 1px solid var(--primary-100);
}

.editor-title {
	font-weight: 600;
	color: var(--primary-800);
	margin: 0 0 1rem 0;
}

/* 选项编辑器样式 */
.options-editor {
	margin-top: 1rem;
}

.options-section {
	margin-bottom: 1rem;
}

.options-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	margin-bottom: 1rem;
}

.options-label {
	font-size: 0.875rem;
	font-weight: 500;
	color: var(--primary-700);
}

.options-input-grid {
	display: grid;
	grid-template-columns: 1fr 1fr;
	gap: 0.5rem;
}

.option-input-item {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
}

.input-label {
	font-size: 0.875rem;
	font-weight: 500;
	color: var(--primary-700);
}

.label-input-group {
	display: flex;
	gap: 0.5rem;
}

.add-option-btn {
	flex-shrink: 0;
	background-color: var(--primary-600) !important;
	border-color: var(--primary-600) !important;
	color: white !important;
}

.add-option-btn:hover {
	background-color: var(--primary-700) !important;
	border-color: var(--primary-700) !important;
}

.add-option-btn:disabled {
	background-color: var(--primary-200) !important;
	border-color: var(--primary-200) !important;
}

/* 当前选项列表样式 */
.current-options-container {
	border: 1px solid var(--primary-200);
	border-radius: 0.375rem;
	padding: 0.75rem;
	background-color: var(--primary-50);
}

.current-options-label {
	display: block;
	margin-bottom: 0.5rem;
	font-size: 0.875rem;
	font-weight: 500;
	color: var(--primary-700);
}

.options-list {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
}

.option-content {
	display: flex;
	align-items: center;
	gap: 0.5rem;
}

.option-label-text {
	font-weight: 500;
	color: var(--primary-800);
}

.option-value-text {
	font-size: 0.875rem;
	color: var(--primary-600);
}

.delete-option-btn {
	color: var(--el-color-danger) !important;
}

.delete-option-btn:hover {
	background-color: var(--el-color-danger-light-9) !important;
}

/* Add Question按钮样式 */
.add-question-btn {
	width: 100%;
	margin-top: 1rem;
	background-color: var(--primary-600) !important;
	border-color: var(--primary-600) !important;
	color: white !important;
}

.add-question-btn:hover {
	background-color: var(--primary-700) !important;
	border-color: var(--primary-700) !important;
}

.add-question-btn:disabled {
	background-color: var(--primary-200) !important;
	border-color: var(--primary-200) !important;
}

.type-option {
	display: flex;
	align-items: center;
	gap: 0.5rem;
	width: 100%;
}

.type-option-name {
	flex: 1;
	min-width: 0;
}

.type-option-tag {
	flex-shrink: 0;
	font-size: 0.625rem;
	height: 1.125rem;
	line-height: 1;
	padding: 0.125rem 0.25rem;
}

.option-input-group {
	margin-bottom: 0.75rem;
}

.current-options {
	margin-top: 0.75rem;
}

.option-item {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 0.5rem;
	border: 1px solid var(--primary-100);
	border-radius: 0.375rem;
	background-color: var(--el-bg-color);
}

.option-text {
	font-weight: 500;
	color: var(--primary-800);
}

.option-value {
	font-size: 0.875rem;
	color: var(--primary-500);
	margin-left: 0.5rem;
}

.delete-btn:hover {
	background-color: var(--el-color-danger-light-9);
}

.delete-question-btn {
	color: var(--el-color-danger) !important;
	flex-shrink: 0;
}

.delete-question-btn:hover {
	background-color: var(--el-color-danger-light-9) !important;
}

/* 深色模式支持 */
.dark .page-header {
	background: linear-gradient(135deg, var(--primary-800) 0%, var(--primary-700) 100%);
	border-bottom-color: var(--primary-600);
}

.dark .page-title {
	color: var(--primary-100);
}

.dark .page-description {
	color: var(--primary-200);
}

.dark .back-button {
	background-color: transparent;
	color: var(--primary-200);
}

.dark .back-button:hover {
	background-color: var(--primary-600);
	color: var(--primary-200);
}

.dark .preview-button {
	border-color: var(--primary-600);
	color: var(--primary-200);
	background-color: transparent;
}

.dark .preview-button:hover {
	background-color: var(--primary-700);
	border-color: var(--primary-500);
}

.dark .save-button {
	background-color: var(--primary-600);
	border-color: var(--primary-600);
}

.dark .save-button:hover {
	background-color: var(--primary-500);
	border-color: var(--primary-500);
}

.dark .config-card {
	border-color: var(--primary-600);
	background-color: var(--primary-800);
}

.dark .section-title {
	color: var(--primary-200);
}

.dark .section-item {
	border-color: var(--primary-600);
	background-color: var(--primary-800);
}

.dark .section-item:hover {
	border-color: var(--primary-500);
	background-color: var(--primary-700);
}

.dark .section-item.active {
	border-color: var(--primary-400);
	background-color: var(--primary-700);
}

.dark .section-name {
	color: var(--primary-100);
}

.dark .section-count {
	color: var(--primary-300);
}

.dark .question-type-item {
	border-color: var(--primary-600);
	background-color: var(--primary-800);
}

.dark .question-type-item:hover {
	background-color: var(--primary-700);
	border-color: var(--primary-500);
}

.dark .question-type-item.active {
	background-color: var(--primary-600);
	border-color: var(--primary-400);
}

.dark .type-name {
	color: var(--primary-100);
}

.dark .questionnaire-header {
	background: linear-gradient(135deg, var(--primary-700) 0%, var(--primary-600) 100%);
	border-color: var(--primary-500);
}

.dark .questionnaire-title {
	color: var(--primary-100);
}

.dark .questionnaire-description {
	color: var(--primary-200);
}

/* 深色模式下的Section Editor样式 */
.dark .editor-title {
	color: var(--primary-200);
}

.dark .current-section-info {
	background-color: var(--primary-700);
	border-color: var(--primary-600);
}

.dark .question-item {
	background-color: var(--primary-800);
	border-color: var(--primary-600);
}

.dark .question-text {
	color: var(--primary-100);
}

.dark .question-description {
	color: var(--primary-300);
}

.dark .options-label {
	color: var(--primary-200);
}

.dark .option-tag {
	background-color: var(--primary-600);
	color: var(--primary-200);
	border-color: var(--primary-500);
}

.dark .new-question-editor {
	background-color: var(--primary-700);
	border-color: var(--primary-600);
}

/* 深色模式下的选项编辑器样式 */
.dark .options-label,
.dark .input-label,
.dark .current-options-label {
	color: var(--primary-200);
}

.dark .current-options-container {
	background-color: var(--primary-700);
	border-color: var(--primary-600);
}

/* 网格编辑器样式 */
.grid-editor {
	margin-top: 1.5rem;
	padding: 1rem;
	border: 1px solid var(--primary-200);
	border-radius: 0.375rem;
	background-color: var(--primary-25);
}

.grid-section {
	display: flex;
	flex-direction: column;
	gap: 1rem;
}

.grid-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	margin-bottom: 0.5rem;
}

.grid-label {
	font-size: 0.875rem;
	font-weight: 600;
	color: var(--primary-800);
}

/* Google Form风格的网格编辑器布局 */
.grid-editor-layout {
	display: flex;
	gap: 2rem;
	margin-top: 1rem;
}

.grid-column-editor {
	flex: 1;
	min-width: 0;
}

.grid-column-header {
	margin-bottom: 1rem;
}

.grid-column-title {
	font-size: 1rem;
	font-weight: 500;
	color: var(--primary-800);
	margin: 0;
}

.grid-items-container {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
}

.grid-editor-item {
	display: flex;
	align-items: center;
	gap: 0.75rem;
	padding: 0.5rem;
	border-radius: 0.375rem;
	transition: background-color 0.2s;
}

.grid-editor-item:hover {
	background-color: var(--primary-25);
}

.grid-item-number {
	font-size: 0.875rem;
	color: var(--primary-600);
	min-width: 1.5rem;
}

.grid-item-label {
	flex: 1;
	font-size: 0.875rem;
	color: var(--primary-700);
}

.grid-column-icon {
	color: var(--primary-500);
	font-size: 1rem;
}

.grid-delete-btn {
	opacity: 0.6;
	transition: opacity 0.2s;
}

.grid-delete-btn:hover {
	opacity: 1;
}

.grid-add-item {
	display: flex;
	align-items: center;
	gap: 0.75rem;
	padding: 0.5rem;
	border: 1px dashed var(--primary-200);
	border-radius: 0.375rem;
	margin-top: 0.5rem;
}

.grid-add-input {
	flex: 1;
}

.grid-options {
	margin-top: 1.5rem;
	padding-top: 1rem;
	border-top: 1px solid var(--primary-100);
}

.grid-option-checkbox {
	font-size: 0.875rem;
}

/* 深色模式下的网格编辑器样式 */
.dark .grid-editor {
	background-color: var(--primary-700);
	border-color: var(--primary-600);
}

.dark .grid-label {
	color: var(--primary-200);
}

.dark .grid-column-title {
	color: var(--primary-200);
}

.dark .grid-editor-item:hover {
	background-color: var(--primary-600);
}

.dark .grid-item-number {
	color: var(--primary-300);
}

.dark .grid-item-label {
	color: var(--primary-200);
}

.dark .grid-column-icon {
	color: var(--primary-400);
}

.dark .grid-add-item {
	border-color: var(--primary-500);
}

.dark .grid-options {
	border-top-color: var(--primary-600);
}

.dark .option-item {
	background-color: var(--primary-800);
	border-color: var(--primary-600);
}

.dark .option-label-text {
	color: var(--primary-100);
}

.dark .option-value-text {
	color: var(--primary-300);
}

/* 深色模式下的空状态样式 */
.dark .empty-questions {
	color: var(--primary-300);
}

/* 深色模式下的滚动条样式 */
.dark .sections-list-container {
	background-color: transparent;
}

/* 拖拽时的幽灵样式 */
.ghost-question {
	opacity: 0.6;
	background: var(--primary-50, #f0f7ff);
	border: 1px dashed var(--primary-500, #2468f2);
}

/* 深色模式下的拖拽样式 */
.dark .question-item {
	background-color: var(--primary-800);
	border-color: var(--primary-600);
}

.dark .question-item:hover {
	border-color: var(--primary-500);
}

.dark .drag-handle {
	color: var(--primary-500);
}

.dark .drag-handle:hover {
	color: var(--primary-400);
}

.dark .ghost-question {
	background: var(--primary-700);
	border: 1px dashed var(--primary-400);
}

/* 线性量表编辑器样式 */
.linear-scale-editor {
	margin-top: 1.5rem;
	padding: 1rem;
	border: 1px solid var(--primary-200);
	border-radius: 0.375rem;
	background-color: var(--primary-25);
	@apply dark:bg-primary-700 dark:border-primary-600;
}

.linear-scale-section {
	display: flex;
	flex-direction: column;
	gap: 1rem;
}

.linear-scale-header {
	margin-bottom: 0.5rem;
}

.linear-scale-label {
	font-size: 0.875rem;
	font-weight: 600;
	color: var(--primary-800);
	@apply dark:text-primary-200;
}

.scale-range-config {
	margin-bottom: 1rem;
}

.range-selectors {
	display: flex;
	align-items: center;
	gap: 1rem;
}

.range-item {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
	min-width: 100px;
}

.range-label {
	font-size: 0.875rem;
	font-weight: 500;
	color: var(--primary-700);
	@apply dark:text-primary-300;
}

.range-select {
	width: 100%;
}

.range-separator {
	font-size: 0.875rem;
	color: var(--primary-600);
	margin-top: 1.5rem;
	@apply dark:text-primary-400;
}

.scale-labels-config {
	border-top: 1px solid var(--primary-100);
	padding-top: 1rem;
	@apply dark:border-primary-600;
}

.labels-grid {
	display: grid;
	grid-template-columns: 1fr 1fr;
	gap: 1rem;
}

.label-item {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
}

.label-title {
	font-size: 0.875rem;
	font-weight: 500;
	color: var(--primary-700);
	text-align: center;
	@apply dark:text-primary-300;
}

.label-input {
	width: 100%;
}
</style>
