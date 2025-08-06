<template>
	<div class="dynamic-form">
		<div class="questionnaire-sections mt-4">
			<!-- 加载状态 -->
			<div v-if="loading" class="flex justify-center items-center py-8">
				<el-icon class="animate-spin mr-2"><Loading /></el-icon>
				<span class="text-sm text-gray-500">Loading questionnaire data...</span>
			</div>

			<!-- 问卷内容 -->
			<template v-else>
				<!-- 问卷描述 -->
				<div
					v-if="
						formattedQuestionnaires.length > 0 && formattedQuestionnaires[0].description
					"
					class="text-sm text-gray-500"
				>
					<div>
						{{ formattedQuestionnaires[0].description }}
						<span v-if="!!formattedQuestionnaires[0]?.hasError" class="error-indicator">
							(Load Error)
						</span>
					</div>

					<el-divider />
				</div>

				<!-- 错误状态显示 -->
				<div v-if="!!formattedQuestionnaires[0]?.hasError" class="questionnaire-error">
					<el-alert
						title="Failed to load questionnaire"
						:description="`There was an error loading the questionnaire structure for '${formattedQuestionnaires[0].title}'. Please check the data format.`"
						type="warning"
						show-icon
						:closable="false"
					/>
				</div>

				<!-- 当前 Section 内容 -->
				<div v-if="currentSection" class="space-y-4">
					<div class="flex flex-col space-y-1.5 p-6 bg-primary-50 section-header">
						<h4 class="section-title">
							{{ currentSectionIndex + 1 }}.{{ currentSection.title }}
						</h4>
						<p v-if="currentSection.description" class="section-description">
							{{ currentSection.description }}
						</p>
					</div>

					<div
						v-for="(question, questionIndex) in currentSection.questions"
						:key="question.id"
						class="question-item"
					>
						<div class="mb-2" v-if="question.type !== 'page_break'">
							<span class="text-sm font-medium text-gray-700">
								{{ currentSectionIndex + 1 }}-{{ questionIndex + 1 }}.
								{{ question.title }}
								<span v-if="question.required" class="text-red-500">*</span>
							</span>
							<p v-if="question.description" class="text-xs text-gray-500 mt-1">
								{{ question.description }}
							</p>
							<div
								v-if="question.questionProps && question.questionProps.fileUrl"
								class="flex flex-col mt-2"
							>
								<el-image
									v-if="question.questionProps.type === 'image'"
									:src="`${globSetting.domainUrl}${question.questionProps.fileUrl}`"
								/>
								<video
									v-else-if="question.questionProps.type === 'video'"
									:src="`${globSetting.domainUrl}${question.questionProps.fileUrl}`"
									:alt="question.questionProps.fileName || 'Uploaded video'"
									controls
								></video>
							</div>
						</div>

						<!-- 短答题 -->
						<el-input
							v-if="question.type === 'short_answer' || question.type === 'text'"
							v-model="formData[question.id]"
							:placeholder="'Enter ' + question.question"
							@input="handleInputChange(question.id, $event)"
						/>

						<!-- 长答题 -->
						<el-input
							v-else-if="
								question.type === 'long_answer' ||
								question.type === 'paragraph' ||
								question.type === 'textarea'
							"
							v-model="formData[question.id]"
							type="textarea"
							:rows="3"
							:placeholder="'Enter ' + question.question"
							@input="handleInputChange(question.id, $event)"
						/>

						<!-- 单选题 -->
						<div v-else-if="question.type === 'multiple_choice'" class="w-full">
							<div class="space-y-2">
								<div
									v-for="option in question.options"
									:key="option.id || option.value"
									class="w-full cursor-pointer flex items-center space-x-2 p-2 hover:bg-gray-50 rounded"
									@click="handleHasOtherQuestion(question, option.value)"
								>
									<div
										:class="[
											'w-4 h-4 border-2 rounded-full flex items-center justify-center',
											formData[question.id] === (option.value || option.label)
												? 'border-blue-500 bg-blue-500'
												: 'border-gray-300',
										]"
									>
										<div
											v-if="
												formData[question.id] ===
												(option.value || option.label)
											"
											class="w-2 h-2 bg-white rounded-full"
										></div>
									</div>
									<div v-if="option.isOther">
										<el-input
											@click.stop
											:disabled="formData[question.id] != option.value"
											v-model="formData[`${question.id}_${option.id}`]"
											placeholder="Enter other"
										/>
									</div>
									<span v-else class="text-sm">
										{{ option.label || option.text || option.value }}
									</span>
								</div>
							</div>
						</div>

						<!-- 多选题 -->
						<el-checkbox-group
							v-else-if="question.type === 'checkboxes'"
							v-model="formData[question.id]"
							@change="handleHasOtherQuestion(question, $event)"
							class="w-full"
						>
							<div class="space-y-2">
								<el-checkbox
									v-for="option in question.options"
									:key="option.id"
									:value="option.value"
									class="w-full"
								>
									<div v-if="option.isOther">
										<el-input
											:disabled="
												!formData[question.id]?.includes(option.value)
											"
											v-model="formData[`${question.id}_${option.id}`]"
											placeholder="Enter other"
										/>
									</div>
									<span v-else class="text-sm">
										{{ option.label }}
									</span>
								</el-checkbox>
							</div>
						</el-checkbox-group>

						<!-- 下拉选择 -->
						<el-select
							v-else-if="question.type === 'dropdown'"
							v-model="formData[question.id]"
							:placeholder="'Select ' + question.question"
							class="w-full"
							@change="handleInputChange(question.id, $event)"
						>
							<el-option
								v-for="option in question.options"
								:key="option.id || option.value"
								:label="option.label || option.text || option.value"
								:value="option.value || option.label"
							/>
						</el-select>

						<!-- 日期选择 -->
						<el-date-picker
							v-else-if="question.type === 'date'"
							v-model="formData[question.id]"
							type="date"
							:placeholder="'Select date'"
							class="w-full"
							:format="projectDate"
							@change="handleInputChange(question.id, $event)"
						/>

						<!-- 时间选择 -->
						<el-time-picker
							v-else-if="question.type === 'time'"
							v-model="formData[question.id]"
							:placeholder="'Select time'"
							class="w-full"
							@change="handleInputChange(question.id, $event)"
						/>
						<!-- 评分 -->
						<div
							v-else-if="question.type === 'rating'"
							class="flex items-center space-x-2"
						>
							<el-rate
								v-model="formData[question.id]"
								:max="question.max || 5"
								:icons="getSelectedFilledIcon(question.iconType)"
								:void-icon="getSelectedVoidIcon(question.iconType)"
								@change="handleInputChange(question.id, $event)"
							/>
							<span v-if="question.showText" class="text-sm text-gray-500">
								({{ question.max || 5 }} stars)
							</span>
						</div>

						<!-- 线性量表 -->
						<div v-else-if="question.type === 'linear_scale'" class="space-y-2">
							<el-slider
								v-model="formData[question.id]"
								:min="question.min || 1"
								:max="question.max || 5"
								:step="1"
								:show-stops="true"
								:show-input="false"
								@change="handleInputChange(question.id, $event)"
								class="preview-linear-scale"
							/>
							<div class="flex justify-between text-xs text-gray-500">
								<span>{{ question.minLabel || question.min || 1 }}</span>
								<span>{{ question.maxLabel || question.max || 5 }}</span>
							</div>
						</div>

						<!-- 文件上传 -->
						<div
							v-else-if="question.type === 'file' || question.type === 'file_upload'"
							class="w-full"
						>
							<el-upload
								drag
								:auto-upload="false"
								:show-file-list="true"
								:on-change="
									(file, fileList) => {
										handleFileChange(question.id, file, fileList);
									}
								"
								:accept="question.accept"
								class="w-full"
							>
								<el-icon class="el-icon--upload text-4xl"><Upload /></el-icon>
								<div class="el-upload__text">
									Drop file here or
									<em>click to select</em>
								</div>
								<div v-if="question.accept" class="el-upload__tip text-xs">
									Accepted formats: {{ question.accept }}
								</div>
							</el-upload>
						</div>

						<!-- 多选网格 -->
						<div
							v-else-if="question.type === 'multiple_choice_grid'"
							class="preview-grid"
						>
							<div v-if="question.columns && question.rows" class="grid-container">
								<div class="grid-header">
									<div class="grid-cell grid-row-header"></div>
									<div
										v-for="(column, colIndex) in question.columns"
										:key="colIndex"
										class="grid-cell grid-column-header"
									>
										{{ column.label }}
										<el-tag
											v-if="column.isOther"
											size="small"
											type="warning"
											class="other-column-tag"
										>
											Other
										</el-tag>
									</div>
								</div>
								<div
									v-for="(row, rowIndex) in question.rows"
									:key="rowIndex"
									class="grid-row"
								>
									<div class="grid-cell grid-row-header">{{ row.label }}</div>
									<div
										v-for="(column, colIndex) in question.columns"
										:key="colIndex"
										class="grid-cell grid-checkbox-cell gap-x-2"
									>
										<el-checkbox-group
											v-model="formData[`${question.id}_${row.id}`]"
											@change="handleHasOtherQuestion(question, row.id)"
										>
											<el-checkbox :value="column.id" class="grid-checkbox" />
										</el-checkbox-group>

										<!-- Other选项的文字输入框 -->
										<div v-if="column.isOther">
											<el-input
												v-model="
													formData[
														`${question.id}_${row.id}_${column.id}`
													]
												"
												:disabled="
													!formData[`${question.id}_${row.id}`]?.includes(
														column.id
													)
												"
												placeholder="Enter other"
												size="small"
												class="other-input"
											/>
										</div>
									</div>
								</div>
							</div>
						</div>

						<!-- 单选网格 (Checkbox grid) -->
						<div v-else-if="question.type === 'checkbox_grid'" class="preview-grid">
							<div
								v-if="
									question.rows &&
									question.rows.length > 0 &&
									question.columns &&
									question.columns.length > 0
								"
								class="grid-container"
							>
								<div class="grid-header">
									<div class="grid-cell grid-row-header"></div>
									<div
										v-for="(column, colIndex) in question.columns"
										:key="colIndex"
										class="grid-cell grid-column-header"
									>
										{{ column.label }}
										<el-tag
											v-if="column.isOther"
											size="small"
											type="warning"
											class="other-column-tag"
										>
											Other
										</el-tag>
									</div>
								</div>
								<div
									v-for="(row, rowIndex) in question.rows"
									:key="rowIndex"
									class="grid-row"
								>
									<div class="grid-cell grid-row-header">{{ row.label }}</div>
									<div
										v-for="(column, colIndex) in question.columns"
										:key="colIndex"
										class="grid-cell grid-radio-cell gap-x-2"
									>
										<el-radio
											v-model="formData[`${question.id}_${row.id}`]"
											:name="`grid_${question.id}_${rowIndex}`"
											:value="
												column.value ||
												column.label ||
												`${rowIndex}_${colIndex}`
											"
											@change="handleHasOtherQuestion(question, row.id)"
											class="grid-radio"
										/>

										<!-- Other选项的文字输入框 -->
										<div v-if="column.isOther">
											<el-input
												v-model="
													formData[
														`${question.id}_${row.id}_${column.id}`
													]
												"
												:disabled="
													formData[`${question.id}_${row.id}`] !=
													(column.value || column.label)
												"
												placeholder="Please specify..."
												size="small"
												class="other-input"
											/>
										</div>
									</div>
								</div>
							</div>

							<!-- 如果没有数据，显示占位符 -->
							<div
								v-else
								class="text-gray-400 italic p-4 border border-dashed border-gray-300 rounded"
							>
								<el-icon class="mr-2"><Warning /></el-icon>
								Checkbox grid: No rows or columns data available
								<div class="text-xs mt-1">
									Rows: {{ question.rows?.length || 0 }}, Columns:
									{{ question.columns?.length || 0 }}
								</div>
							</div>
						</div>

						<div
							v-else-if="question.type === 'page_break'"
							class="text-gray-600 italic"
						>
							<div class="border-t-2 border-dashed border-primary-300 pt-4 mt-4">
								<div class="text-center text-primary-500 text-sm">
									— Page Break —
								</div>
							</div>
						</div>

						<div v-else-if="question.type === 'image'">
							<el-image :src="`${globSetting.domainUrl}${question.fileUrl}`" />
						</div>

						<div v-else-if="question.type === 'video'">
							<video
								:src="`${globSetting.domainUrl}${question.fileUrl}`"
								controls
							></video>
						</div>
					</div>
					<div
						v-if="!currentSection.questions || currentSection.questions.length <= 0"
						class="empty-state-container"
					>
						<el-empty
							:image-size="60"
							description="No questions available in this section"
						>
							<template #description>
								<p class="text-gray-500 text-sm">
									This section doesn't contain any questions yet.
								</p>
							</template>
						</el-empty>
					</div>

					<!-- 统一的底部导航控件 -->
					<div v-if="totalSections > 1" class="bottom-navigation">
						<!-- 左侧：上一页按钮 -->
						<div class="nav-left">
							<el-button
								v-if="!isFirstSection"
								@click="goToPreviousSection"
								class="pagination-btn"
							>
								<el-icon class="mr-1"><ArrowLeft /></el-icon>
								Previous
							</el-button>
						</div>

						<!-- 中间：进度指示器 -->
						<div class="section-progress">
							<!-- <div class="section-dots">
								<button
									v-for="(section, index) in formattedQuestionnaires[0].sections"
									:key="section.id"
									@click="goToSection(index)"
									:class="[
										'section-dot',
										{ active: index === currentSectionIndex },
									]"
									:title="section.title"
								></button>
							</div> -->
						</div>

						<!-- 右侧：下一页按钮 -->
						<div class="nav-right">
							<el-button
								v-if="!isLastSection"
								@click="goToNextSection"
								type="primary"
								class="pagination-btn"
							>
								Next
								<el-icon class="ml-1"><ArrowRight /></el-icon>
							</el-button>
						</div>
					</div>
				</div>

				<!-- 如果没有当前 section 的占位符 -->
				<div v-else-if="totalSections === 0" class="no-sections-placeholder">
					<el-empty description="No sections available" :image-size="80">
						<template #description>
							<p class="text-gray-500">
								This questionnaire doesn't have any sections configured.
							</p>
						</template>
					</el-empty>
				</div>
			</template>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch, nextTick, readonly } from 'vue';
import { Upload, Loading, Warning, ArrowLeft, ArrowRight } from '@element-plus/icons-vue';
import { QuestionnaireAnswer, QuestionnaireData, ComponentData, SectionAnswer } from '#/onboard';
import { QuestionnaireSection } from '#/section';
import { ElNotification } from 'element-plus';
import { projectDate } from '@/settings/projectSetting';
import { useGlobSetting } from '@/settings';

// 使用 MDI 图标库
import IconStar from '~icons/mdi/star';
import IconStarOutline from '~icons/mdi/star-outline';
import IconHeart from '~icons/mdi/heart';
import IconHeartOutline from '~icons/mdi/heart-outline';
import IconThumbUp from '~icons/mdi/thumb-up';
import IconThumbUpOutline from '~icons/mdi/thumb-up-outline';

const globSetting = useGlobSetting();

// 组件属性
interface Props {
	stageId: string;
	onboardingId?: string;
	questionnaireData?: ComponentData;
	isStageCompleted?: boolean;
	questionnaireAnswers?: SectionAnswer;
}

const props = defineProps<Props>();

const formData = ref<Record<string, any>>({});
const loading = ref(false);
const currentSectionIndex = ref(0);

// 计算属性 - 检查是否有问卷数据
const hasQuestionnaireData = computed(() => {
	return props.questionnaireData && props.questionnaireData.id;
});

// 计算属性 - 当前显示的 section
const currentSection = computed(() => {
	if (!hasQuestionnaireData.value || formattedQuestionnaires.value.length === 0) return null;
	const questionnaire = formattedQuestionnaires.value[0];
	if (!questionnaire.sections || questionnaire.sections.length === 0) return null;
	return questionnaire.sections[currentSectionIndex.value] || null;
});

// 计算属性 - 所有 sections 的总数
const totalSections = computed(() => {
	if (!hasQuestionnaireData.value || formattedQuestionnaires.value.length === 0) return 0;
	const questionnaire = formattedQuestionnaires.value[0];
	return questionnaire.sections?.length || 0;
});

// 计算属性 - 是否是第一页
const isFirstSection = computed(() => currentSectionIndex.value === 0);

// 计算属性 - 是否是最后一页
const isLastSection = computed(() => currentSectionIndex.value >= totalSections.value - 1);

// 格式化问卷数据 - 现在处理单个问卷对象
const formattedQuestionnaires = computed(() => {
	if (!hasQuestionnaireData.value) return [];

	const questionnaire = props.questionnaireData;

	try {
		// 处理 structureJson
		let structure: any = {};
		if (questionnaire?.structureJson) {
			try {
				structure = JSON.parse(questionnaire.structureJson);
			} catch (parseError) {
				structure = {};
			}
		}

		// 确保有 sections 数组
		if (!structure.sections || !Array.isArray(structure.sections)) {
			structure.sections = [];
		}

		const processedQuestionnaire = {
			...questionnaire,
			title: questionnaire?.name || 'Questionnaire',
			sections: structure.sections.map((section: any) => ({
				...section,
				id: section?.id,
				title: section.title || section.name,
				questions: (section.questions || []).map((question: any) => ({
					...question,
					// 保持原有的title作为问题内容
					question: question.title || question.question || '',
					// 使用原始的question.id，不要重新生成
					id: question.id,
				})),
			})),
		};

		return [processedQuestionnaire]; // 返回数组以保持模板兼容性
	} catch (error) {
		// 即使处理失败，也返回一个基本的结构
		return [
			{
				...questionnaire,
				title: questionnaire?.name || 'Questionnaire (Error)',
				sections: [
					{
						id: `error_section`,
						title: 'Error Loading Questionnaire',
						questions: [],
					},
				],
				hasError: true,
			},
		];
	}
});

// 根据答案数组填充表单
const applyAnswers = (answers?: QuestionnaireAnswer[]) => {
	if (!Array.isArray(answers) || answers.length === 0) return;
	answers.forEach((ans) => {
		if (!ans || !ans.questionId) return;

		// 检查是否是网格问题（包含下划线分隔的ID）
		if (ans.type === 'multiple_choice_grid' || ans.type === 'checkbox_grid') {
			if (ans?.type === 'multiple_choice_grid') {
				// 多选网格：将逗号分隔的字符串转换为数组
				const selectedValues = ans.answer;

				formData.value[ans.questionId] = selectedValues;
			} else if (ans?.type === 'checkbox_grid') {
				// 单选网格：直接设置字符串值
				formData.value[ans.questionId] = ans.answer;
			}

			if (ans.responseText) {
				const responseText = JSON.parse(ans.responseText);
				Object.keys(responseText).forEach((key) => {
					formData.value[key] = responseText[key];
				});
			}
		} else if (ans.type === 'multiple_choice' || ans.type === 'checkboxes') {
			// 对于多选题，需要将答案转换为数组格式
			if (ans.type === 'checkboxes') {
				// 处理多选题答案：确保是数组格式
				if (Array.isArray(ans.answer)) {
					formData.value[ans.questionId] = ans.answer;
				} else if (ans.answer) {
					// 将字符串转换为数组
					const answerStr = String(ans.answer);
					if (answerStr.includes(',')) {
						formData.value[ans.questionId] = answerStr
							.split(',')
							.map((item) => item.trim())
							.filter(Boolean);
					} else {
						formData.value[ans.questionId] = [answerStr];
					}
				} else {
					formData.value[ans.questionId] = [];
				}
			} else {
				// 单选题保持原有逻辑
				formData.value[ans.questionId] = ans.answer;
			}

			if (ans.responseText) {
				const responseText = JSON.parse(ans.responseText);
				Object.keys(responseText).forEach((key) => {
					formData.value[key] = responseText[key];
				});
			}
		} else {
			formData.value[ans.questionId] = ans.answer;
		}
	});
	nextTick(() => {
		if (props.questionnaireAnswers?.currentSectionIndex) {
			currentSectionIndex.value = props.questionnaireAnswers?.currentSectionIndex || 0;
		}
	});
};

// 监听答案数据变化，确保答案能正确应用
watch(
	() => props.questionnaireAnswers,
	(newAnswers) => {
		if (newAnswers && formattedQuestionnaires.value.length > 0) {
			// 确保表单数据已初始化后再应用答案
			nextTick(() => {
				applyAnswers(newAnswers.answer);
			});
		}
	},
	{ immediate: true, deep: true }
);

// 处理单选按钮点击（支持取消选择）
const handleRadioClick = (questionId: string, optionValue: string) => {
	// 检查当前点击的选项是否已经被选中
	if (formData.value[questionId] === optionValue) {
		// 如果已经选中，则取消选择
		formData.value[questionId] = '';
		handleInputChange(questionId, '');
	} else {
		// 如果没有选中，则正常选择该选项
		formData.value[questionId] = optionValue;
		handleInputChange(questionId, optionValue);
	}
};

// 处理表单值变化
const handleInputChange = (questionId: string, value: any) => {
	formData.value[questionId] = value;
};

// 复杂表单值变化处理
const handleHasOtherQuestion = (question: QuestionnaireSection, value: any) => {
	if (question.type == 'multiple_choice') {
		handleRadioClick(question.id, value);
	} else {
		formData.value[question.id] = value;
	}
	if (question.type == 'multiple_choice' || question.type == 'checkboxes') {
		question?.options?.forEach((option) => {
			if (
				option.isOther &&
				((!Array.isArray(formData.value[question.id]) &&
					formData.value[question.id] !== option.value) ||
					(Array.isArray(formData.value[question.id]) &&
						!formData.value[question.id]?.includes(option.value)))
			) {
				formData.value[`${question.id}_${option.id}`] = '';
			}
		});
	} else if (question.type == 'multiple_choice_grid' || question.type == 'checkbox_grid') {
		question?.columns?.forEach((column) => {
			if (
				column.isOther &&
				((!Array.isArray(formData.value[`${question.id}_${value}`]) &&
					formData.value[`${question.id}_${value}`] !== column.id) ||
					(Array.isArray(formData.value[`${question.id}_${value}`]) &&
						!formData.value[`${question.id}_${value}`]?.includes(column.id)))
			) {
				formData.value[`${question.id}_${value}_${column.id}`] = '';
			}
		});
	}
};

// 处理文件变化
const handleFileChange = (questionId: string, file: any, fileList: any[]) => {
	formData.value[questionId] = fileList;
	handleInputChange(questionId, fileList);
};

// 验证表单
const validateForm = (presentQuestionIndex?: number) => {
	let isValid = true;
	const errors: string[] = [];
	if (!formattedQuestionnaires.value || formattedQuestionnaires.value.length === 0) {
		return { isValid: true, errors: [] };
	}

	const questionnaire = formattedQuestionnaires.value[0];
	let vailSection: any[] = [];
	if (presentQuestionIndex != undefined && presentQuestionIndex != null) {
		vailSection = [questionnaire.sections[presentQuestionIndex]];
	} else {
		vailSection = questionnaire.sections.slice(
			currentSectionIndex.value,
			questionnaire.sections.length
		);
	}

	vailSection.forEach((section: any, sIndex: number) => {
		if (!section.questions || section.questions.length === 0) {
			return true;
		}
		section.questions.forEach((question: any, qIdx: number) => {
			if (question.required) {
				if (question.type === 'multiple_choice_grid') {
					// 多选网格：检查每一行是否都有选择
					if (question.rows && question.rows.length > 0) {
						let allRowsCompleted = true;
						question.rows.forEach((row: any, rowIndex: number) => {
							const gridKey = `${question.id}_${row.id || rowIndex}`;
							const gridValue = formData.value[gridKey];
							if (!Array.isArray(gridValue) || gridValue.length === 0) {
								allRowsCompleted = false;
							}
						});
						if (!allRowsCompleted) {
							isValid = false;
							const errorMsg = `${sIndex + currentSectionIndex.value + 1} - ${
								qIdx + 1
							}`;
							errors.push(errorMsg);
						}
					}
				} else if (question.type === 'checkbox_grid') {
					// 单选网格：检查每一行是否都有选择
					if (question.rows && question.rows.length > 0) {
						let allRowsCompleted = true;
						question.rows.forEach((row: any, rowIndex: number) => {
							const gridKey = `${question.id}_${row.id || rowIndex}`;
							const gridValue = formData.value[gridKey];
							if (!gridValue || gridValue === '') {
								allRowsCompleted = false;
							}
						});
						if (!allRowsCompleted) {
							isValid = false;
							const errorMsg = `${sIndex + currentSectionIndex.value + 1} - ${
								qIdx + 1
							}`;
							errors.push(errorMsg);
						}
					}
				} else if (question.type == 'rating') {
					const value = formData.value[question.id];
					if ((typeof value === 'number' && value < 1) || !value) {
						isValid = false;
						const errorMsg = `${sIndex + currentSectionIndex.value + 1} - ${qIdx + 1}`;
						errors.push(errorMsg);
					}
				} else if (question.type == 'linear_scale') {
					const value = formData.value[question.id];
					if ((typeof value === 'number' && value <= question.min) || !value) {
						isValid = false;
						const errorMsg = `${sIndex + currentSectionIndex.value + 1} - ${qIdx + 1}`;
						errors.push(errorMsg);
					}
				} else {
					// 其他类型的验证  其他类型的验证也需要单独处理
					const value = formData.value[question.id];
					// 更严格的空值检查
					const isEmpty =
						value === null ||
						value === undefined ||
						value === '' ||
						(typeof value === 'string' && value.trim() === '') ||
						(Array.isArray(value) && value.length === 0);

					if (isEmpty) {
						isValid = false;
						const errorMsg = `${sIndex + currentSectionIndex.value + 1} - ${qIdx + 1}`;
						errors.push(errorMsg);
					}
				}
			}
		});
	});
	return { isValid, errors };
};

// 转换表单数据为API格式
const transformFormDataForAPI = () => {
	const apiData: QuestionnaireData[] = [];
	for (const questionnaire of formattedQuestionnaires.value) {
		const questionnaireData: QuestionnaireData = {
			questionnaireId: questionnaire?.id || '',
			stageId: props.stageId,
			answerJson: [],
		};

		for (const section of questionnaire.sections) {
			for (const question of section.questions) {
				if (question.type === 'multiple_choice_grid' || question.type === 'checkbox_grid') {
					// 网格类型：为每一行创建单独的答案记录
					if (question.rows && question.rows.length > 0) {
						question.rows.forEach((row: any) => {
							const gridKey = `${question.id}_${row.id}`;
							const gridValue = formData.value[gridKey];

							// 处理Other选项的文本
							let responseText = {};

							question.columns.forEach((column: any) => {
								const otherTextKey = `${question.id}_${row.id}_${column.id}`;
								if (column.isOther && formData.value[otherTextKey]) {
									responseText[otherTextKey] = formData.value[otherTextKey];
								}
							});
							const answer: QuestionnaireAnswer = {
								questionId: gridKey,
								question: `${question.question} - ${row.label}`,
								answer: gridValue,
								type: question.type,
								responseText: JSON.stringify(responseText),
							};
							questionnaireData.answerJson.push(answer);
						});
					}
				} else if (question.type === 'checkboxes' || question.type === 'multiple_choice') {
					// 单选题
					let responseText = {};
					question.options.forEach((option: any) => {
						const otherTextKey = `${question.id}_${option.id}`;
						if (option.isOther && formData.value[otherTextKey]) {
							const otherText = formData.value[otherTextKey];
							if (otherText && otherText.trim()) {
								responseText = {
									[otherTextKey]: otherText,
								};
							}
						}
					});
					const answer: QuestionnaireAnswer = {
						questionId: question.id,
						question: question.question,
						answer: formData.value[question.id],
						type: question.type,
						responseText: JSON.stringify(responseText),
					};
					questionnaireData.answerJson.push(answer);
				} else {
					// 普通类型的问题
					const answer: QuestionnaireAnswer = {
						questionId: question.id,
						question: question.question,
						answer: formData.value[question.id],
						type: question.type,
						responseText: Array.isArray(formData.value[question.id])
							? formData.value[question.id].join(', ')
							: formData.value[question.id] || '',
					};
					questionnaireData.answerJson.push(answer);
				}
			}
		}
		apiData.push(questionnaireData);
	}

	return apiData?.map((item) => {
		return {
			...item,
			answerJson: JSON.stringify({
				responses: item.answerJson,
			}),
			currentSectionIndex: currentSectionIndex.value,
		};
	});
};

// 转换表单数据为新的字段格式
const getFormData = () => {
	const result: Array<{
		fieldName: string;
		fieldValueJson: any;
		fieldType: string;
		isRequired: boolean;
	}> = [];

	// 遍历表单数据，生成新格式的数据（过滤掉空值）
	Object.keys(formData.value).forEach((fieldKey) => {
		const value = formData.value[fieldKey];
		const hasValue = Array.isArray(value)
			? value.length > 0
			: value !== undefined && value !== '' && value !== null;

		if (hasValue) {
			// 根据问题类型确定字段类型
			let fieldType = '';
			let isRequired = false;

			// 查找对应的问题信息来确定类型和是否必填
			formattedQuestionnaires.value.forEach((questionnaire) => {
				questionnaire.sections.forEach((section: any) => {
					section.questions.forEach((question: any) => {
						// 检查是否是网格类型的字段
						const isGridField = fieldKey.startsWith(`${question.id}_`);

						if (question.id === fieldKey || isGridField) {
							fieldType = question?.type;
							isRequired = question.required || false;
						}
					});
				});
			});

			result.push({
				fieldName: fieldKey,
				fieldValueJson: formData.value[fieldKey],
				fieldType: fieldType,
				isRequired: isRequired,
			});
		}
	});

	return result;
};

// 根据跳转规则获取目标section ID
const getJumpTargetSection = () => {
	if (!currentSection.value?.questions) return null;

	// 从最后一个问题开始向前查找，找到最后一个有效的跳转规则
	const questions = currentSection.value.questions;

	// 倒序遍历问题数组，找到最后一个符合条件的跳转规则
	for (let i = questions.length - 1; i >= 0; i--) {
		const question = questions[i];

		// 检查是否是单选题且有跳转规则
		if (
			(question.type === 'multiple_choice' || question.type === 'checkboxes') &&
			question.jumpRules &&
			question.jumpRules.length > 0
		) {
			const userAnswer = formData.value[question.id];

			// 检查用户是否已经选择了答案
			if (userAnswer && userAnswer !== '') {
				// 查找匹配的跳转规则
				const matchingRule = question.jumpRules.find((rule) => {
					return (
						rule.optionId &&
						question.options.some(
							(option) =>
								option.id === rule.optionId &&
								(option.value === userAnswer || option.label === userAnswer)
						)
					);
				});

				// 如果找到匹配的跳转规则，立即返回
				if (matchingRule) {
					return matchingRule.targetSectionId;
				}
			}
		}
	}

	return null;
};

// 根据section ID查找在sections数组中的索引
const findSectionIndexById = (sectionId: string) => {
	if (!formattedQuestionnaires.value.length) return -1;

	const questionnaire = formattedQuestionnaires.value[0];
	if (!questionnaire.sections) return -1;

	return questionnaire.sections.findIndex((section) => section.id === sectionId);
};

// 分页控制方法
const goToPreviousSection = () => {
	if (!isFirstSection.value) {
		currentSectionIndex.value--;
	}
};

const goToNextSection = async () => {
	const { isValid, errors } = await validateForm(currentSectionIndex.value);
	if (!isValid) {
		const errorHtml = errors.map((error) => `<p>${error}</p>`).join('');
		ElNotification({
			title: 'Please complete all required fields',
			dangerouslyUseHTMLString: true,
			message: errorHtml,
			type: 'warning',
		});
		return;
	}
	// 检查是否有跳转规则需要应用
	const targetSectionId = getJumpTargetSection();

	if (targetSectionId) {
		// 根据跳转规则跳转到指定section
		const targetSectionIndex = findSectionIndexById(targetSectionId);
		if (targetSectionIndex !== -1) {
			currentSectionIndex.value = targetSectionIndex;
			return;
		}
	}

	// 没有跳转规则或找不到目标section，使用默认的下一个section
	if (!isLastSection.value) {
		currentSectionIndex.value++;
	}
};

const goToSection = (index: number) => {
	if (index >= 0 && index < totalSections.value) {
		currentSectionIndex.value = index;
	}
};

// 不再监听 props 重新拉取答案，父组件负责注入

// 初始化
onMounted(async () => {
	await nextTick();

	// 初始化表单数据
	if (hasQuestionnaireData.value && formattedQuestionnaires.value.length > 0) {
		formattedQuestionnaires.value.forEach((questionnaire) => {
			questionnaire.sections.forEach((section: any) => {
				section.questions.forEach((question: any) => {
					// 根据问题类型初始化表单数据
					if (
						question.type === 'multiple_choice_grid' ||
						question.type === 'checkbox_grid'
					) {
						// 多选网格：为每一行初始化多选值（数组）
						if (question.rows && question.rows.length > 0) {
							question.rows.forEach((row: any) => {
								const key = `${question.id}_${row.id}`;
								if (!(key in formData.value)) {
									formData.value[key] =
										question.type === 'multiple_choice_grid' ? [] : '';
								}
								question.columns.forEach((column: any) => {
									if (column.isOther) {
										const otherTextKey = `${question.id}_${row.id}_${column.id}`;
										if (!(otherTextKey in formData.value)) {
											formData.value[otherTextKey] = '';
										}
									}
								});
							});
						}
					} else if (question.type === 'checkboxes' || question.type === 'checkbox') {
						// 多选题：初始化为数组
						if (!(question.id in formData.value)) {
							formData.value[question.id] = [];
						}
					} else {
						// 其他类型：初始化为空字符串
						if (!(question.id in formData.value)) {
							formData.value[question.id] = '';
						}
					}
				});

				section?.columns?.forEach((column: any) => {
					if (column.isOther) {
						const otherTextKey = `${section.id}_${column.id}`;
						if (!(otherTextKey in formData.value)) {
							formData.value[otherTextKey] = '';
						}
					}
				});

				section?.options?.forEach((option: any) => {
					if (option.isOther) {
						const otherTextKey = `${section.id}_${option.id}`;
						if (!(otherTextKey in formData.value)) {
							formData.value[otherTextKey] = '';
						}
					}
				});
			});
		});
		// 初始化完毕后再应用答案，防止被覆盖
		applyAnswers(props.questionnaireAnswers?.answer);
	}
});

// 图标选项
const iconOptions = {
	star: {
		filledIcon: [IconStar, IconStar, IconStar],
		voidIcon: IconStarOutline,
	},
	heart: {
		filledIcon: [IconHeart, IconHeart, IconHeart],
		voidIcon: IconHeartOutline,
	},
	thumbs: {
		filledIcon: [IconThumbUp, IconThumbUp, IconThumbUp],
		voidIcon: IconThumbUpOutline,
	},
};

const getSelectedFilledIcon = (iconType: string) => {
	return iconOptions[iconType]?.filledIcon;
};

const getSelectedVoidIcon = (iconType: string) => {
	return iconOptions[iconType]?.voidIcon;
};

defineExpose({
	validateForm,
	transformFormDataForAPI,
	getFormData,
	goToPreviousSection,
	goToNextSection,
	goToSection,
	currentSectionIndex: readonly(currentSectionIndex),
	totalSections,
	isFirstSection,
	isLastSection,
});
</script>

<style scoped lang="scss">
/* 问卷区域样式 */
.questionnaire-sections {
	:deep(.el-collapse-item__header) {
		font-size: 16px;
		font-weight: 500;
		color: #374151;
		background-color: var(--primary-50);
		border-bottom: 1px solid #e5e7eb;
		padding: 16px;
	}

	:deep(.el-collapse-item__content) {
		padding: 20px;
		background-color: #ffffff;
	}
}

.error-indicator {
	font-size: 14px;
	font-weight: normal;
	color: #ef4444;
	background-color: #fef2f2;
	padding: 2px 8px;
	border-radius: 4px;
	border: 1px solid #fecaca;
}

.questionnaire-error {
	margin-bottom: 16px;
}

/* 统一的底部导航样式 */
.bottom-navigation {
	margin-top: 32px;
	padding: 20px;
	background-color: #f9fafb;
	border-radius: 8px;
	border: 1px solid #e5e7eb;
	display: flex;
	align-items: center;
	justify-content: space-between;
	min-height: 60px;
}

/* 左侧导航区域 */
.nav-left {
	display: flex;
	align-items: center;
	justify-content: flex-start;
	min-width: 120px;
}

/* 右侧导航区域 */
.nav-right {
	display: flex;
	align-items: center;
	justify-content: flex-end;
	min-width: 120px;
}

/* 进度指示器区域 */
.section-progress {
	display: flex;
	align-items: center;
	justify-content: center;
	flex: 1;
}

.section-dots {
	display: flex;
	justify-content: center;
	gap: 8px;
	align-items: center;
}

.section-dot {
	width: 12px;
	height: 12px;
	border-radius: 50%;
	border: 2px solid #d1d5db;
	background-color: #f9fafb;
	cursor: pointer;
	transition: all 0.2s ease;
	padding: 0;
	outline: none;

	&:hover {
		border-color: var(--primary-400);
		background-color: var(--primary-50);
	}

	&.active {
		border-color: var(--primary-500);
		background-color: var(--primary-500);
	}
}

.pagination-btn {
	display: flex;
	align-items: center;
	gap: 4px;
}

/* Section 标题样式 */
.section-header {
	background: linear-gradient(135deg, var(--primary-50) 0%, var(--primary-100) 100%);
	border: 1px solid var(--primary-200);
	border-radius: 8px;
}

.section-title {
	font-size: 18px;
	font-weight: 600;
	color: var(--primary-800);
	margin: 0;
}

.section-description {
	font-size: 14px;
	color: var(--primary-600);
	margin: 0;
	line-height: 1.5;
}

/* 无section占位符 */
.no-sections-placeholder {
	margin: 40px 0;
	text-align: center;
}

.question-item {
	padding: 16px;
	border: 1px solid #e5e7eb;
	border-radius: 8px;
	background-color: #f9fafb;

	&:hover {
		border-color: #d1d5db;
	}
}

/* 暗色主题 */
html.dark {
	.questionnaire-sections {
		:deep(.el-collapse-item__header) {
			background-color: var(--black-200) !important;
			border-bottom: 1px solid var(--black-100) !important;
			color: var(--white-100) !important;
		}

		:deep(.el-collapse-item__content) {
			background-color: var(--black-400) !important;
			color: var(--white-100) !important;
		}
	}

	.question-item {
		background-color: var(--black-200) !important;
		border-color: var(--black-100) !important;

		&:hover {
			border-color: var(--black-50) !important;
		}
	}

	.text-gray-900,
	.text-gray-700,
	.text-gray-600 {
		color: var(--white-100) !important;
	}

	.text-gray-500 {
		color: #d1d5db !important;
	}

	.text-gray-400 {
		color: #9ca3af !important;
	}

	.error-indicator {
		color: #fca5a5;
		background-color: rgba(239, 68, 68, 0.1);
		border-color: rgba(239, 68, 68, 0.3);
	}

	/* 底部导航暗色主题 */
	.bottom-navigation {
		background-color: var(--black-200);
		border-color: var(--black-100);
	}

	.section-dot {
		border-color: var(--black-100);
		background-color: var(--black-200);

		&:hover {
			border-color: var(--primary-400);
			background-color: var(--primary-800);
		}

		&.active {
			border-color: var(--primary-500);
			background-color: var(--primary-500);
		}
	}

	.section-header {
		background: linear-gradient(135deg, var(--primary-800) 0%, var(--primary-700) 100%);
		border-color: var(--primary-600);
	}

	.section-title {
		color: var(--white-100);
	}

	.section-description {
		color: var(--primary-200);
	}

	.no-sections-placeholder {
		color: var(--white-200);
	}
}

/* 网格样式 */
.preview-grid {
	@apply w-full;

	.grid-container {
		border: 1px solid var(--el-border-color);
		border-radius: 4px;
		overflow: hidden;
	}

	.grid-header {
		display: flex;
		background-color: var(--el-fill-color-light);
		border-bottom: 1px solid var(--el-border-color);
	}

	.grid-row {
		display: flex;
		border-bottom: 1px solid var(--el-border-color);
		&:last-child {
			border-bottom: none;
		}

		&:hover {
			background-color: var(--el-fill-color-lighter);
		}
	}

	.grid-cell {
		padding: 12px;
		display: flex;
		align-items: center;
		justify-content: center;
		align-items: center;
		min-width: 120px;
		flex: 1;
		border-right: 1px solid var(--el-border-color);

		&:first-child {
			justify-content: flex-start;
			font-weight: 500;
			background-color: var(--el-fill-color-lighter);
			min-width: 200px;
			flex: 2;
		}

		&:last-child {
			border-right: none;
		}
	}

	.grid-row-header {
		background-color: var(--el-fill-color-lighter);
		font-weight: 500;
		text-align: left;
		justify-content: flex-start !important;
	}

	.grid-column-header {
		font-weight: 500;
		text-align: center;
		background-color: var(--el-fill-color-light);
	}

	.grid-radio-cell {
		background-color: white;
	}

	.grid-checkbox-cell {
		background-color: white;
		justify-content: center;
	}

	.grid-radio {
		margin: 0;

		:deep(.el-radio__input) {
			margin: 0;
		}

		:deep(.el-radio__label) {
			display: none;
		}
	}

	.grid-checkbox {
		margin: 0;

		:deep(.el-checkbox__input) {
			margin: 0;
		}

		:deep(.el-checkbox__label) {
			display: none;
		}
	}
}

.other-column-tag {
	font-size: 0.625rem;
	height: 1.125rem;
	line-height: 1;
	padding: 0.125rem 0.25rem;
	margin-left: 0.25rem;
}

.other-input {
	width: 100%;
}

/* 线性量表样式 */
.preview-linear-scale {
	:deep(.el-slider__runway) {
		background-color: var(--primary-100);
		@apply dark:bg-black-200;
	}

	:deep(.el-slider__bar) {
		background-color: var(--primary-500);
	}

	:deep(.el-slider__button) {
		border-color: var(--primary-500);
	}
}
/* 暗色主题支持 */
html.dark {
	.preview-grid {
		.grid-container {
			border-color: var(--el-border-color-dark);
		}

		.grid-header,
		.grid-row {
			border-color: var(--el-border-color-dark);
		}

		.grid-cell {
			border-color: var(--el-border-color-dark);
		}

		.grid-row-header,
		.grid-column-header {
			background-color: var(--el-bg-color-dark);
		}

		.grid-radio-cell,
		.grid-checkbox-cell {
			background-color: var(--el-bg-color-dark);
		}
	}
}
</style>
