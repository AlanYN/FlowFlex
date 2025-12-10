<template>
	<div class="dynamic-form">
		<div v-loading="loading" class="">
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
				<div class="flex flex-col space-y-1" v-if="!currentSection.isDefault">
					<h4 class="section-title" v-if="currentSection.title">
						{{ currentSectionIndex + 1 }}.{{ currentSection.title }}
					</h4>
					<p v-if="currentSection.description" class="section-description">
						{{ currentSection.description }}
					</p>
				</div>

				<div
					v-for="(question, questionIndex) in currentSection.questions"
					:key="question.id"
					:id="question.id || question.temporaryId"
					:data-question-id="question.id || question.temporaryId"
					class="question-item"
					:class="{ '!bg-white !border-none': question.type == 'page_break' }"
				>
					<div class="mb-2" v-if="question.type !== 'page_break'">
						<div class="flex items-center gap-2">
							<span class="text-sm font-medium form-question-number">
								{{ currentSectionIndex + 1 }}-{{
									getQuestionNumber(questionIndex)
								}}.
								{{ question.title }}
								<span
									v-if="question.required && !isQuestionSkipped(question)"
									class="text-red-500"
								>
									*
								</span>
							</span>
							<!-- Action Tag for question -->
							<ActionTag
								v-if="
									question.action &&
									question.action.id &&
									question.action.name &&
									onboardingId
								"
								:action="question.action"
								:trigger-source-id="question.id"
								trigger-source-type="question"
								:onboarding-id="onboardingId"
								type="success"
								size="small"
							/>
						</div>
						<p v-if="question.description" class="text-xs form-question-desc mt-1">
							{{ question.description }}
						</p>
						<div
							v-if="question.questionProps && question.questionProps.fileUrl"
							class="flex justify-center items-center"
						>
							<el-image
								v-if="question.questionProps.type === 'image'"
								:src="question.questionProps.fileUrl"
								class="responsive-image"
								:preview-src-list="[`${question.questionProps.fileUrl}`]"
								fit="contain"
							/>
							<video
								v-else-if="question.questionProps.type === 'video'"
								:src="question.questionProps.fileUrl"
								:alt="question.questionProps.fileName || 'Uploaded video'"
								controls
								class="max-h-[500px] w-auto object-contain"
							></video>
						</div>
					</div>
					<!-- 短答题 -->
					<el-input
						v-if="question.type === 'short_answer' || question.type === 'text'"
						v-model="formData[question.id]"
						:maxlength="questionMaxlength"
						:placeholder="'Enter ' + question.question"
						:disabled="questionIsDisabled(question.id)"
						@change="handleInputChange(question.id, $event)"
					/>

					<!-- 长答题 -->
					<el-input
						v-else-if="
							question.type === 'long_answer' ||
							question.type === 'paragraph' ||
							question.type === 'textarea'
						"
						:disabled="questionIsDisabled(question.id)"
						v-model="formData[question.id]"
						:maxlength="notesPageTextraMaxLength"
						type="textarea"
						:rows="3"
						show-word-limit
						:placeholder="'Enter ' + question.question"
						@change="handleInputChange(question.id, $event)"
					/>

					<!-- 单选题 -->
					<div v-else-if="question.type === 'multiple_choice'" class="w-full">
						<div class="space-y-2">
							<div
								v-for="option in question.options"
								:key="option.id || option.value"
								class="w-full flex items-center space-x-2 p-2 form-radio-option rounded"
								:class="{
									'cursor-not-allowed form-radio-disabled': questionIsDisabled(
										question.id
									),
								}"
								@click="
									!questionIsDisabled(question.id) &&
										handleHasOtherQuestion(question, option.value)
								"
							>
								<div
									:class="[
										'w-4 h-4 border-2 rounded-full flex items-center justify-center flex-shrink-0',
										formData[question.id] === (option.value || option.label)
											? 'form-radio-checked'
											: 'form-radio-unchecked',
									]"
								>
									<div
										v-if="
											formData[question.id] === (option.value || option.label)
										"
										class="w-2 h-2 bg-white rounded-full"
									></div>
								</div>
								<div v-if="option.isOther">
									<el-input
										@click.stop
										:disabled="
											formData[question.id] != option.value ||
											questionIsDisabled(question.id)
										"
										v-model="formData[`${question.id}_${option.id}`]"
										:maxlength="questionMaxlength"
										placeholder="Enter other"
									/>
								</div>
								<div v-else class="flex items-center gap-2">
									<span
										class="text-sm"
										:class="{
											'text-primary-500 font-bold':
												formData[question.id] ===
												(option.value || option.label),
										}"
									>
										{{ option.label || option.text || option.value }}
									</span>
									<!-- Action Tag for option -->
									<ActionTag
										v-if="
											option.action &&
											option.action.id &&
											option.action.name &&
											onboardingId
										"
										:action="option.action"
										:trigger-source-id="option.id || option.temporaryId"
										trigger-source-type="option"
										:onboarding-id="onboardingId"
										type="success"
										size="small"
									/>
								</div>
							</div>
						</div>
					</div>

					<!-- 多选题 -->
					<el-checkbox-group
						v-else-if="question.type === 'checkboxes'"
						v-model="formData[question.id]"
						@change="handleHasOtherQuestion(question, $event)"
						class="w-full"
						:disabled="questionIsDisabled(question.id)"
					>
						<div class="space-y-2">
							<el-checkbox
								v-for="option in question.options"
								:key="option.id"
								:value="option.value"
								class="w-full"
							>
								<div class="flex items-center gap-x-2">
									<div v-if="option.isOther">
										<el-input
											:disabled="
												!formData[question.id]?.includes(option.value) ||
												questionIsDisabled(question.id)
											"
											v-model="formData[`${question.id}_${option.id}`]"
											:maxlength="questionMaxlength"
											placeholder="Enter other"
										/>
									</div>
									<span v-else class="text-sm">
										{{ option.label }}
									</span>
									<ActionTag
										v-if="
											option.action &&
											option.action.id &&
											option.action.name &&
											onboardingId
										"
										:action="option.action"
										:trigger-source-id="option.id || option.temporaryId"
										trigger-source-type="option"
										:onboarding-id="onboardingId"
										type="success"
										size="small"
									/>
								</div>
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
						:disabled="questionIsDisabled(question.id)"
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
						:disabled="questionIsDisabled(question.id)"
					/>

					<!-- 时间选择 -->
					<el-time-picker
						v-else-if="question.type === 'time'"
						v-model="formData[question.id]"
						:placeholder="'Select time'"
						class="w-full"
						@change="handleInputChange(question.id, $event)"
						:disabled="questionIsDisabled(question.id)"
					/>
					<!-- 评分 -->
					<div v-else-if="question.type === 'rating'" class="flex items-center space-x-2">
						<el-rate
							v-model="formData[question.id]"
							:max="question.max || 5"
							:icons="getSelectedFilledIcon(question.iconType)"
							:void-icon="getSelectedVoidIcon(question.iconType)"
							@change="handleInputChange(question.id, $event)"
							:disabled="questionIsDisabled(question.id)"
						/>
						<span v-if="question.showText" class="text-sm form-star-text">
							({{ question.max || 5 }} stars)
						</span>
					</div>

					<!-- 线性量表 -->
					<div v-else-if="question.type === 'linear_scale'" class="space-y-2">
						<el-slider
							:key="`slider-${question.id}-${formData[question.id] || 0}`"
							v-model="formData[question.id]"
							:min="question.min"
							:max="question.max"
							:marks="getSliderMarks(question)"
							class="preview-linear-scale"
							@change="handleInputChange(question.id, $event)"
							:validate-event="false"
							show-stops
							:disabled="questionIsDisabled(question.id)"
						/>
						<div class="flex justify-between text-xs form-slider-labels">
							<span>{{ question.minLabel || question.min }}</span>
							<span>{{ question.maxLabel || question.max }}</span>
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
							v-model:file-list="formData[question.id]"
							:accept="question.accept"
							class="w-full"
							:disabled="questionIsDisabled(question.id)"
						>
							<el-icon class="el-icon--upload text-4xl"><Upload /></el-icon>
							<div>
								<text class="text-primary dark:text-white">Drop file here</text>
								<text>or</text>
								<em class="text-primary">click to select</em>
							</div>
							<div v-if="question.accept" class="el-upload__tip text-xs">
								Accepted formats: {{ question.accept }}
							</div>
						</el-upload>
					</div>

					<!-- 多选网格 -->
					<div v-else-if="question.type === 'multiple_choice_grid'" class="preview-grid">
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
										:disabled="questionIsDisabled(question.id)"
									>
										<el-checkbox :value="column.id" class="grid-checkbox" />
									</el-checkbox-group>

									<!-- Other选项的文字输入框 -->
									<div v-if="column.isOther">
										<el-input
											v-model="
												formData[`${question.id}_${row.id}_${column.id}`]
											"
											:disabled="
												!formData[`${question.id}_${row.id}`]?.includes(
													column.id
												)
											"
											:maxlength="questionMaxlength"
											placeholder="Enter other"
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
										:disabled="questionIsDisabled(question.id)"
										@change="handleHasOtherQuestion(question, row.id)"
										class="grid-radio"
									/>

									<!-- Other选项的文字输入框 -->
									<div v-if="column.isOther">
										<el-input
											v-model="
												formData[`${question.id}_${row.id}_${column.id}`]
											"
											:disabled="
												formData[`${question.id}_${row.id}`] !=
													(column.value || column.label) ||
												questionIsDisabled(question.id)
											"
											placeholder="Enter other"
											:maxlength="questionMaxlength"
											class="other-input"
										/>
									</div>
								</div>
							</div>
						</div>

						<!-- 如果没有数据，显示占位符 -->
						<div
							v-else
							class="form-unsupported-type italic p-4 border border-dashed rounded"
						>
							<el-icon class="mr-2"><Warning /></el-icon>
							Checkbox grid: No rows or columns data available
							<div class="text-xs mt-1">
								Rows: {{ question.rows?.length || 0 }}, Columns:
								{{ question.columns?.length || 0 }}
							</div>
						</div>
					</div>

					<div v-else-if="question.type === 'short_answer_grid'" class="preview-grid">
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
									<el-input
										v-model="formData[`${question.id}_${column.id}_${row.id}`]"
										:maxlength="questionMaxlength"
										:disabled="questionIsDisabled(question.id)"
									/>
								</div>
							</div>
						</div>
					</div>

					<div v-else-if="question.type === 'page_break'" class="form-page-break italic">
						<div class="border-t-2 border-dashed border-primary-300 pt-4 mt-4">
							<div class="text-center text-primary-500 text-sm">— Page Break —</div>
						</div>
					</div>

					<div
						v-else-if="question.type === 'image'"
						class="flex justify-center items-center"
					>
						<el-image
							:src="question.fileUrl"
							class="responsive-image"
							:preview-src-list="[`${question.fileUrl}`]"
							fit="contain"
						/>
					</div>

					<div
						v-else-if="question.type === 'video'"
						class="flex justify-center items-center"
					>
						<video
							:src="question.fileUrl"
							controls
							class="max-h-[500px] w-auto object-contain"
						></video>
					</div>
				</div>
				<div
					v-if="!currentSection.questions || currentSection.questions.length <= 0"
					class="empty-state-container"
				>
					<el-empty :image-size="60" description="No questions available in this section">
						<template #description>
							<p class="form-empty-text text-sm">
								This section doesn't contain any questions yet.
							</p>
						</template>
					</el-empty>
				</div>

				<!-- 统一的底部导航控件 -->
				<div class="bottom-navigation">
					<!-- 左侧：上一页按钮 -->
					<div class="nav-left">
						<el-button
							v-if="!isFirstSection && totalSections > 1"
							@click="goToPreviousSection"
						>
							<el-icon class="mr-1"><ArrowLeft /></el-icon>
							Previous
						</el-button>
					</div>

					<!-- 中间：进度指示器 -->
					<div class="section-progress">
						<div class="section-dots">
							<button
								v-for="(section, index) in formattedQuestionnaires[0].sections"
								:key="section.id"
								@click="goToSection(index)"
								:class="['section-dot', { active: index === currentSectionIndex }]"
								:title="section.title"
							></button>
						</div>
					</div>

					<!-- 右侧：下一页按钮 -->
					<div class="nav-right">
						<el-button
							v-if="!isLastSection && totalSections > 1"
							@click="goToNextSection"
						>
							Next
							<el-icon class="ml-1"><ArrowRight /></el-icon>
						</el-button>
						<el-button
							v-if="isLastSection"
							@click="Submit()"
							type="primary"
							:icon="Document"
							:loading="loading"
							:disabled="!isSubmitEnabled || disabled"
						>
							Submit
						</el-button>
					</div>
				</div>
			</div>

			<!-- 如果没有当前 section 的占位符 -->
			<div v-else-if="totalSections === 0" class="no-sections-placeholder">
				<el-empty description="No sections available" :image-size="80">
					<template #description>
						<p class="form-empty-text">
							This questionnaire doesn't have any sections configured.
						</p>
					</template>
				</el-empty>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch, nextTick, readonly } from 'vue';
import { Upload, Warning, ArrowLeft, ArrowRight, Document } from '@element-plus/icons-vue';
import { QuestionnaireAnswer, QuestionnaireData, ComponentData, SectionAnswer } from '#/onboard';
import { QuestionnaireSection } from '#/section';
// import { ElNotification } from 'element-plus';
import {
	projectDate,
	notesPageTextraMaxLength,
	questionMaxlength,
} from '@/settings/projectSetting';
import ActionTag from '@/components/actionTools/ActionTag.vue';

// 使用 MDI 图标库
import IconStar from '~icons/mdi/star';
import IconStarOutline from '~icons/mdi/star-outline';
import IconHeart from '~icons/mdi/heart';
import IconHeartOutline from '~icons/mdi/heart-outline';
import IconThumbUp from '~icons/mdi/thumb-up';
import IconThumbUpOutline from '~icons/mdi/thumb-up-outline';

// 组件属性
interface Props {
	stageId: string;
	onboardingId?: string;
	questionnaireData?: ComponentData;
	isStageCompleted?: boolean;
	questionnaireAnswers?: SectionAnswer;
	disabled?: boolean;
	isSubmitEnabled?: boolean;
	skippedQuestions?: Set<string>;
	loading?: boolean;
}

const props = defineProps<Props>();
const emit = defineEmits(['submit', 'change']);

const formData = ref<Record<string, any>>({});
const currentSectionIndex = ref(0);

// 内部维护被跳过的问题集合（用于响应式更新）
const internalSkippedQuestions = ref<Set<string>>(new Set());

// 合并内部和外部传入的跳过问题集合
const skippedQuestions = computed(() => {
	const merged = new Set<string>();
	// 先添加外部传入的跳过问题
	if (props.skippedQuestions) {
		props.skippedQuestions.forEach((id) => merged.add(id));
	}
	// 再添加内部维护的跳过问题
	internalSkippedQuestions.value.forEach((id) => merged.add(id));
	return merged;
});

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
				temporaryId: section?.temporaryId || section?.id,
				title: section.title || section.name,
				questions: (section.questions || []).map((question: any) => ({
					...question,
					// 保持原有的title作为问题内容
					question: question.title || question.question || '',
					// 使用原始的question.id，不要重新生成
					id: question.id,
					temporaryId: question?.temporaryId || question?.id,
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
		} else if (ans.type === 'short_answer_grid') {
			formData.value[ans.questionId] = ans.answer;
			if (ans.responseText) {
				const responseText = JSON.parse(ans.responseText);
				Object.keys(responseText).forEach((key) => {
					formData.value[key] = responseText[key];
				});
			}
		} else if (ans.type === 'linear_scale' || ans.type === 'rating') {
			// 确保数字类型的答案保持为数字
			const numValue = Number(ans.answer);
			formData.value[ans.questionId] = isNaN(numValue) ? 0 : numValue;
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

	emit('change');
};

// 复杂表单值变化处理
const handleHasOtherQuestion = (question: QuestionnaireSection & { id: string }, value: any) => {
	if (question.type == 'multiple_choice') {
		handleRadioClick(question?.id, value);
	} else {
		formData.value[question?.id] = value;
	}
	if (question.type == 'multiple_choice' || question.type == 'checkboxes') {
		question?.options?.forEach((option) => {
			if (
				option.isOther &&
				((!Array.isArray(formData.value[question?.id]) &&
					formData.value[question?.id] !== option.value) ||
					(Array.isArray(formData.value[question?.id]) &&
						!formData.value[question?.id]?.includes(option.value)))
			) {
				formData.value[`${question.id}_${option.id}`] = '';
			}
		});

		// 处理跳转逻辑
		if (question.jumpRules && question.jumpRules.length > 0 && question.options) {
			const userAnswer = formData.value[question.id];

			// 如果答案被清空，清除跳过状态
			if (!isAnswerValid(userAnswer)) {
				internalSkippedQuestions.value = new Set();
				return;
			}

			// 查找匹配的跳转规则
			const matchingRule = findMatchingJumpRule(question, userAnswer);

			// 如果找到匹配的跳转规则，执行跳转
			if (matchingRule) {
				// 单选题：如果有targetQuestionId，跳转到具体问题；否则跳转到section
				if (question.type === 'multiple_choice' && matchingRule.targetQuestionId) {
					// 计算被跳过的问题并触发跳转
					handleJumpToQuestion(matchingRule, question);
				}
			} else {
				// 如果没有匹配的跳转规则，清除跳过状态
				internalSkippedQuestions.value = new Set();
			}
		}
	} else if (question.type == 'multiple_choice_grid' || question.type == 'checkbox_grid') {
		question?.columns?.forEach((column) => {
			if (
				column.isOther &&
				((!Array.isArray(formData.value[`${question?.id}_${value}`]) &&
					formData.value[`${question?.id}_${value}`] !== column.id) ||
					(Array.isArray(formData.value[`${question?.id}_${value}`]) &&
						!formData.value[`${question?.id}_${value}`]?.includes(column.id)))
			) {
				formData.value[`${question?.id}_${value}_${column.id}`] = '';
			}
		});
	}

	emit('change');
};

// 处理跳转到具体问题
const handleJumpToQuestion = (jumpRule: any, currentQuestion: any) => {
	if (!formattedQuestionnaires.value.length) return;

	const questionnaire = formattedQuestionnaires.value[0];
	const currentSection = questionnaire.sections[currentSectionIndex.value];
	if (!currentSection) return;

	const currentQuestionIndex = currentSection.questions.findIndex(
		(q: any) => q.id === currentQuestion.id || q.temporaryId === currentQuestion.temporaryId
	);

	if (currentQuestionIndex === -1) return;

	// 查找目标section和问题
	const targetSectionIndex = questionnaire.sections.findIndex(
		(s: any) => s.id === jumpRule.targetSectionId || s.temporaryId === jumpRule.targetSectionId
	);

	if (targetSectionIndex === -1) return;

	const targetSection = questionnaire.sections[targetSectionIndex];
	if (!targetSection) return;

	// 如果跳转规则有targetQuestionId，使用它；否则跳转到section的第一个问题（兼容旧数据）
	let targetQuestionIndex = -1;
	if (jumpRule.targetQuestionId) {
		targetQuestionIndex = targetSection.questions.findIndex(
			(q: any) =>
				q.id === jumpRule.targetQuestionId || q.temporaryId === jumpRule.targetQuestionId
		);
	} else {
		// 兼容旧数据：跳转到section的第一个问题
		targetQuestionIndex = 0;
	}

	if (targetQuestionIndex === -1) return;

	// 计算被跳过的问题
	const skipped = new Set<string>();

	if (currentSectionIndex.value === targetSectionIndex) {
		// 同section内跳转：跳过当前问题之后到目标问题之前的所有问题
		for (let i = currentQuestionIndex + 1; i < targetQuestionIndex; i++) {
			const skippedQuestion = currentSection.questions[i];
			if (skippedQuestion) {
				const questionId =
					skippedQuestion.id || skippedQuestion.temporaryId || skippedQuestion.questionId;
				if (questionId) skipped.add(questionId);
			}
		}
	} else {
		// 跨section跳转：
		// 1. 跳过当前section中当前问题之后的所有问题
		for (let i = currentQuestionIndex + 1; i < currentSection.questions.length; i++) {
			const skippedQuestion = currentSection.questions[i];
			if (skippedQuestion) {
				const questionId =
					skippedQuestion.id || skippedQuestion.temporaryId || skippedQuestion.questionId;
				if (questionId) skipped.add(questionId);
			}
		}

		// 2. 跳过目标section中目标问题之前的所有问题
		for (let i = 0; i < targetQuestionIndex; i++) {
			const skippedQuestion = targetSection.questions[i];
			if (skippedQuestion) {
				const questionId =
					skippedQuestion.id || skippedQuestion.temporaryId || skippedQuestion.questionId;
				if (questionId) skipped.add(questionId);
			}
		}
	}

	// 清除之前的跳过状态，然后更新新的跳过问题集合
	// 创建新的 Set 以触发响应式更新
	internalSkippedQuestions.value = new Set(skipped);

	// 跳转到目标section和问题
	currentSectionIndex.value = targetSectionIndex;

	// 滚动到目标问题（使用nextTick确保DOM已更新）
	nextTick(() => {
		const targetQuestion = targetSection.questions[targetQuestionIndex];
		if (targetQuestion) {
			const questionId = targetQuestion.id || targetQuestion.temporaryId;
			if (questionId) {
				const element =
					document.getElementById(questionId) ||
					document.querySelector(`[data-question-id="${questionId}"]`);
				if (element) {
					element.scrollIntoView({ behavior: 'smooth', block: 'center' });
				}
			}
		}
	});
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
		vailSection = questionnaire.sections;
	}

	// 用于记录所有已经验证过的section，避免重复验证
	const validatedSectionIds = new Set<string>();

	for (let sIndex = 0; sIndex < vailSection.length; sIndex++) {
		const section = vailSection[sIndex];

		// 如果这个section已经被验证过，跳过
		if (validatedSectionIds.has(section.id)) {
			continue;
		}

		if (!section.questions || section.questions.length === 0) {
			validatedSectionIds.add(section.id);
			continue;
		}

		// 标记当前section为已验证
		validatedSectionIds.add(section.id);

		// 验证当前section的所有问题
		section.questions
			?.filter((item) => {
				return item.type != 'page_break';
			})
			?.forEach((question: any, qIdx: number) => {
				// 跳过被跳过的问题（即使它们是必填的）
				if (isQuestionSkipped(question)) {
					return;
				}

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
								const errorMsg = `${sIndex + 1} - ${qIdx + 1}`;
								errors.push(errorMsg);
							}
						}
					} else if (question.type === 'short_answer_grid') {
						if (question.rows && question.columns && question.columns.length > 0) {
							let allRowsCompleted = true;
							question.rows.forEach((row: any, rowIndex: number) => {
								// 检查该行是否至少有一个单元格有内容
								let rowHasValue = false;
								question.columns.forEach((column: any, columnIndex: number) => {
									const gridKey = `${question.id}_${column.id}_${row.id}`;
									const gridValue = formData.value[gridKey];
									if (gridValue && gridValue.trim() !== '') {
										rowHasValue = true;
									}
								});
								// 如果该行没有任何内容，则标记为未完成
								if (!rowHasValue) {
									allRowsCompleted = false;
								}
							});
							if (!allRowsCompleted) {
								isValid = false;
								const errorMsg = `${sIndex + 1} - ${qIdx + 1}`;
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
								const errorMsg = `${sIndex + 1} - ${qIdx + 1}`;
								errors.push(errorMsg);
							}
						}
					} else if (question.type == 'rating') {
						const value = formData.value[question.id];
						if (value == null || value == undefined) {
							isValid = false;
							const errorMsg = `${sIndex + 1} - ${qIdx + 1}`;
							errors.push(errorMsg);
						}
					} else if (question.type == 'linear_scale') {
						const value = formData.value[question.id];
						if (value == null || value == undefined) {
							isValid = false;
							const errorMsg = `${sIndex + 1} - ${qIdx + 1}`;
							errors.push(errorMsg);
						}
					} else {
						// 其他类型的验证  其他类型的验证也需要单独处理
						if (question.type !== 'image' && question.type != 'video') {
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
								const errorMsg = `${sIndex + 1} - ${qIdx + 1}`;
								errors.push(errorMsg);
							}
						}
					}
				}
			});

		// 验证完当前section后，检查是否有跳转逻辑被触发
		let jumpTargetSectionId = null;

		// 从最后一个问题开始向前查找，找到最后一个有效的跳转规则
		for (let i = section.questions.length - 1; i >= 0; i--) {
			const question = section.questions[i];

			// 检查是否是有跳转规则的问题
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
									(option.id === rule.optionId ||
										option.temporaryId === rule.optionId) &&
									(option.value === userAnswer || option.label === userAnswer)
							)
						);
					});

					// 如果找到匹配的跳转规则，记录目标并跳出循环
					if (matchingRule) {
						jumpTargetSectionId = matchingRule.targetSectionId;
						break;
					}
				}
			}
		}

		// 如果有跳转目标，调整索引直接跳转到目标section
		if (jumpTargetSectionId) {
			// 找到目标section在vailSection数组中的位置
			const targetInVailSections = vailSection.findIndex(
				(s) => s.id === jumpTargetSectionId || s.temporaryId === jumpTargetSectionId
			);

			if (targetInVailSections !== -1 && targetInVailSections > sIndex) {
				// 直接跳转到目标section，跳过中间的section
				sIndex = targetInVailSections - 1; // -1 是因为for循环会自动+1
			}
		}
	}
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
				} else if (question.type === 'short_answer_grid') {
					if (question.rows && question.rows.length > 0) {
						question.rows.forEach((row: any) => {
							let responseText = {};

							question.columns.forEach((column: any) => {
								const gridKey = `${question.id}_${column.id}_${row.id}`;
								if (formData.value[gridKey]) {
									responseText[gridKey] = formData.value[gridKey];
								}
							});
							const answer: QuestionnaireAnswer = {
								questionId: question.id,
								question: `${question.question} - ${row.label}`,
								answer: '',
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

/**
 * 检查答案是否有效（支持数组和字符串类型）
 * @param userAnswer 用户答案
 * @returns 答案是否有效
 */
const isAnswerValid = (userAnswer: any): boolean => {
	if (!userAnswer) return false;
	return Array.isArray(userAnswer) ? userAnswer.length > 0 : userAnswer !== '';
};

/**
 * 查找匹配的跳转规则
 * @param question 问题对象
 * @param userAnswer 用户答案
 * @returns 匹配的跳转规则，如果没有则返回 undefined
 */
const findMatchingJumpRule = (question: any, userAnswer: any) => {
	if (!question.jumpRules || !question.jumpRules.length || !question.options) {
		return undefined;
	}

	return question.jumpRules.find((rule: any) => {
		if (!rule.optionId) return false;

		return question.options.some((option: any) => {
			// 检查选项ID是否匹配
			const isOptionMatch =
				option.id === rule.optionId || option.temporaryId === rule.optionId;
			if (!isOptionMatch) return false;

			// 检查答案是否匹配（支持数组和字符串）
			if (Array.isArray(userAnswer)) {
				// 多选题：检查数组中是否包含该选项的值或标签
				return userAnswer.includes(option.value) || userAnswer.includes(option.label);
			} else {
				// 单选题：直接比较
				return option.value === userAnswer || option.label === userAnswer;
			}
		});
	});
};

// 根据跳转规则获取目标section ID
const getJumpTargetSection = () => {
	if (!currentSection.value?.questions) return null;

	// 从最后一个问题开始向前查找，找到最后一个有效的跳转规则
	const questions = currentSection.value.questions;

	// 倒序遍历问题数组，找到最后一个符合条件的跳转规则
	for (let i = questions.length - 1; i >= 0; i--) {
		const question = questions[i];
		// 检查是否有跳转规则
		if (question.jumpRules && question.jumpRules.length > 0) {
			const userAnswer = formData.value[question.id];

			// 检查用户是否已经选择了答案
			if (isAnswerValid(userAnswer)) {
				// 查找匹配的跳转规则
				const matchingRule = findMatchingJumpRule(question, userAnswer);

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

	return questionnaire.sections.findIndex(
		(section) => section.id === sectionId || section.temporaryId === sectionId
	);
};

// 分页控制方法
const goToPreviousSection = () => {
	if (!isFirstSection.value) {
		currentSectionIndex.value--;
	}
};

const goToNextSection = async () => {
	// const { isValid, errors } = await validateForm(currentSectionIndex.value);
	// if (!isValid) {
	// 	const errorHtml = errors.map((error) => `<p>${error}</p>`).join('');
	// 	ElNotification({
	// 		title: 'Please complete all required fields',
	// 		dangerouslyUseHTMLString: true,
	// 		message: errorHtml,
	// 		type: 'warning',
	// 	});
	// 	return;
	// }
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

// 初始化
onMounted(async () => {
	await nextTick();

	// 初始化表单数据
	if (hasQuestionnaireData.value && formattedQuestionnaires.value.length > 0) {
		formattedQuestionnaires.value.forEach((questionnaire) => {
			questionnaire.sections
				?.filter((item) => {
					return (
						item.type !== 'image' && item.type != 'video' && item.type != 'page_break'
					);
				})
				.forEach((section: any) => {
					section.questions.forEach((question: any) => {
						// 根据问题类型初始化表单数据
						if (
							question.type === 'multiple_choice_grid' ||
							question.type === 'checkbox_grid'
						) {
							// 多选网格：为每一行初始化多选值（数组）
							if (question.rows && question.rows.length > 0) {
								question.rows.forEach((row: any) => {
									const key = `${question?.id}_${row.id}`;
									if (!(key in formData.value)) {
										formData.value[key] =
											question.type === 'multiple_choice_grid' ? [] : '';
									}
									question.columns.forEach((column: any) => {
										if (column.isOther) {
											const otherTextKey = `${question?.id}_${row.id}_${column.id}`;
											if (!(otherTextKey in formData.value)) {
												formData.value[otherTextKey] = '';
											}
										}
									});
								});
							}
						} else if (question.type == 'short_answer_grid') {
							if (question.rows && question.rows.length > 0) {
								question.rows.forEach((row: any) => {
									question.columns.forEach((column: any) => {
										const otherTextKey = `${question?.id}_${column.id}_${row.id}`;
										if (!(otherTextKey in formData.value)) {
											formData.value[otherTextKey] = '';
										}
									});
								});
							}
						} else if (question.type === 'checkboxes' || question.type === 'checkbox') {
							// 多选题：初始化为数组
							if (!(question?.id in formData.value)) {
								formData.value[question?.id] = [];
							}
						} else if (question.type === 'file' || question.type === 'file_upload') {
							if (!(question?.id in formData.value)) {
								formData.value[question?.id] = [];
							}
						} else if (question.type === 'linear_scale') {
							// 线性量表：初始化为最小值（数字类型）
							if (!(question?.id in formData.value)) {
								formData.value[question?.id] = question.min;
							}
						} else if (question.type === 'rating') {
							// 评分：初始化为0（数字类型）
							if (!(question?.id in formData.value)) {
								formData.value[question?.id] = 0;
							}
						} else {
							// 其他类型：初始化为空字符串
							if (!(question?.id in formData.value)) {
								formData.value[question?.id] = '';
							}
						}
					});

					section?.columns?.forEach((column: any) => {
						if (column.isOther) {
							const otherTextKey = `${section?.id}_${column.id}`;
							if (!(otherTextKey in formData.value)) {
								formData.value[otherTextKey] = '';
							}
						}
					});

					section?.options?.forEach((option: any) => {
						if (option.isOther) {
							const otherTextKey = `${section?.id}_${option.id}`;
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

// 生成slider的刻度标记
const getSliderMarks = (question: any) => {
	const marks: Record<number, string> = {};
	const min = question.min;
	const max = question.max;

	for (let i = min; i <= max; i++) {
		marks[i] = '';
	}

	return marks;
};

// 计算问题的实际序号（跳过page_break类型）
const getQuestionNumber = (questionIndex: number) => {
	if (!currentSection.value?.questions) return questionIndex + 1;

	let actualQuestionNumber = 1;
	for (let i = 0; i <= questionIndex; i++) {
		const question = currentSection.value.questions[i];
		if (question.type !== 'page_break') {
			if (i === questionIndex) {
				return actualQuestionNumber;
			}
			actualQuestionNumber++;
		}
	}
	return actualQuestionNumber;
};

// 检查问题是否被跳过
const isQuestionSkipped = (question: any): boolean => {
	const questionId = question.id || question.temporaryId || question.questionId;
	return skippedQuestions.value.has(questionId);
};

const questionIsDisabled = (questionId: string): boolean => {
	return props.disabled || !!internalSkippedQuestions.value.has(questionId);
};

const Submit = () => {
	emit('submit');
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
	// 暴露 formData 以供父组件访问
	get formData() {
		return formData.value;
	},
});
</script>

<style scoped lang="scss">
/* 问卷区域样式 */
.error-indicator {
	font-size: var(--button-1-size); /* 14px - Item Button 1 */
	font-weight: normal;
	color: var(--el-color-danger);
	background-color: var(--el-color-danger-light-9);
	padding: 2px 8px;
	border: 1px solid var(--el-color-danger-light-7);
	@apply rounded-xl;
}

.questionnaire-error {
	margin-bottom: 16px;
}

/* 统一的底部导航样式 */
.bottom-navigation {
	margin-top: 32px;
	padding: 20px;
	background-color: var(--el-fill-color-blank);
	border: 1px solid var(--el-border-color-lighter);
	display: flex;
	align-items: center;
	justify-content: space-between;
	min-height: 60px;
	@apply rounded-xl;
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
	border: 2px solid var(--el-border-color);
	background-color: var(--el-fill-color-blank);
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

.section-title {
	font-size: var(--body-2-size); /* 18px - Item Body 2 */
	font-weight: 600;
	color: var(--primary-800);
	margin: 0;
}

.section-description {
	font-size: var(--button-1-size); /* 14px - Item Button 1 */
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
	border: 1px solid var(--el-border-color-lighter);
	background-color: var(--el-fill-color-blank);
	@apply rounded-xl;

	&:hover {
		border-color: var(--el-border-color);
	}
}

/* 暗色主题 */
html.dark {
	.question-item {
		background-color: var(--black-200) !important;
		border-color: var(--black-100) !important;

		&:hover {
			border-color: var(--black-50) !important;
		}
	}

	/* 移除硬编码的 dark 模式样式，使用新的自定义类 */

	.error-indicator {
		color: var(--el-color-danger-light-3);
		background-color: var(--el-color-danger-dark-2);
		border-color: var(--el-color-danger);
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
		overflow: hidden;
		@apply rounded-xl;
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
		background-color: var(--el-color-white);
	}

	.grid-checkbox-cell {
		background-color: var(--el-color-white);
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

.responsive-image {
	@apply block;
	max-height: 500px;
	max-width: 100%;
	width: auto;
	height: auto;
	object-fit: contain;

	:deep(.el-image__inner) {
		max-height: 500px;
		max-width: 100%;
		width: auto;
		height: auto;
		object-fit: contain;
	}
}

.form-question-number {
	color: var(--el-text-color-regular);
}

.form-question-desc {
	color: var(--el-text-color-secondary);
}

.form-radio-option:hover {
	background-color: var(--el-fill-color-lighter);
}

.form-radio-disabled {
	background-color: var(--el-fill-color-light);
}

.form-radio-checked {
	border-color: var(--el-color-primary);
	background-color: var(--el-color-primary);
}

.form-radio-unchecked {
	border-color: var(--el-border-color);
}

.form-star-text {
	color: var(--el-text-color-secondary);
}

.form-slider-labels {
	color: var(--el-text-color-secondary);
}

.form-unsupported-type {
	color: var(--el-text-color-placeholder);
	border-color: var(--el-border-color);
}

.form-page-break {
	color: var(--el-text-color-regular);
}

.form-empty-text {
	color: var(--el-text-color-secondary);
}
</style>
