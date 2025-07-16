<template>
	<div class="dynamic-form">
		<el-collapse v-model="activeCollapses" class="questionnaire-sections">
			<!-- 加载状态 -->
			<div v-if="loading" class="flex justify-center items-center py-8">
				<el-icon class="animate-spin mr-2"><Loading /></el-icon>
				<span class="text-sm text-gray-500">Loading questionnaire data...</span>
			</div>

			<!-- 问卷内容 -->
			<template
				v-else
				v-for="(questionnaire, qIndex) in formattedQuestionnaires"
				:key="qIndex"
			>
				<!-- 问卷标题 -->
				<div v-if="formattedQuestionnaires.length > 1" class="questionnaire-header">
					<h3 class="questionnaire-title">
						{{ questionnaire.title }}
						<span v-if="questionnaire.hasError" class="error-indicator">
							(Load Error)
						</span>
					</h3>
					<el-divider class="questionnaire-divider" />
				</div>

				<!-- 错误状态显示 -->
				<div v-if="questionnaire.hasError" class="questionnaire-error">
					<el-alert
						title="Failed to load questionnaire"
						:description="`There was an error loading the questionnaire structure for '${questionnaire.title}'. Please check the data format.`"
						type="warning"
						show-icon
						:closable="false"
					/>
				</div>

				<el-collapse-item
					v-for="section in questionnaire.sections"
					:key="section.id"
					:title="section.title"
					:name="section.id"
				>
					<div class="space-y-4">
						<div
							v-for="question in section.questions"
							:key="question.id"
							class="question-item"
						>
							<div class="mb-2">
								<span class="text-sm font-medium text-gray-700">
									{{ question.title }}
									<span v-if="question.required" class="text-red-500">*</span>
								</span>
								<p v-if="question.description" class="text-xs text-gray-500 mt-1">
									{{ question.description }}
								</p>
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
							<el-radio-group
								v-else-if="
									question.type === 'multiple_choice' || question.type === 'radio'
								"
								v-model="formData[question.id]"
								@change="handleInputChange(question.id, $event)"
								class="w-full"
							>
								<div class="space-y-2">
									<el-radio
										v-for="option in question.options"
										:key="option.id || option.value"
										:value="option.value || option.label"
										class="w-full"
									>
										{{ option.label || option.text || option.value }}
									</el-radio>
								</div>
							</el-radio-group>

							<!-- 多选题 -->
							<el-checkbox-group
								v-else-if="
									question.type === 'checkboxes' || question.type === 'checkbox'
								"
								v-model="formData[question.id]"
								@change="handleInputChange(question.id, $event)"
								class="w-full"
							>
								<div class="space-y-2">
									<el-checkbox
										v-for="option in question.options"
										:key="option.id || option.value"
										:value="option.value || option.label"
										class="w-full"
									>
										{{ option.label || option.text || option.value }}
									</el-checkbox>
								</div>
							</el-checkbox-group>

							<!-- 下拉选择 -->
							<el-select
								v-else-if="
									question.type === 'dropdown' || question.type === 'select'
								"
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

							<!-- 日期时间选择 -->
							<el-date-picker
								v-else-if="question.type === 'datetime'"
								v-model="formData[question.id]"
								type="datetime"
								:placeholder="'Select date and time'"
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
								/>
								<div class="flex justify-between text-xs text-gray-500">
									<span>{{ question.minLabel || question.min || 1 }}</span>
									<span>{{ question.maxLabel || question.max || 5 }}</span>
								</div>
							</div>

							<!-- 文件上传 -->
							<div
								v-else-if="
									question.type === 'file' || question.type === 'file_upload'
								"
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
								<div
									v-if="question.columns && question.rows"
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
											class="grid-cell grid-checkbox-cell"
										>
											<el-checkbox-group
												v-model="
													formData[`${question.id}_${row.id || rowIndex}`]
												"
												@change="
													handleInputChange(
														`${question.id}_${row.id || rowIndex}`,
														$event
													)
												"
											>
												<el-checkbox
													:value="
														column.value ||
														column.label ||
														`col_${colIndex}`
													"
													class="grid-checkbox"
												/>
											</el-checkbox-group>
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
											class="grid-cell grid-radio-cell"
										>
											<el-radio
												v-model="
													formData[`${question.id}_${row.id || rowIndex}`]
												"
												:name="`grid_${question.id}_${rowIndex}`"
												:value="
													column.value ||
													column.label ||
													`${rowIndex}_${colIndex}`
												"
												@change="
													handleInputChange(
														`${question.id}_${row.id || rowIndex}`,
														$event
													)
												"
												class="grid-radio"
											/>
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
						</div>
						<div
							v-if="!section.questions || section.questions.length <= 0"
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
					</div>
				</el-collapse-item>
			</template>
		</el-collapse>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { Upload, Loading, Warning } from '@element-plus/icons-vue';
import { getQuestionnaireAnswer } from '@/apis/ow/onboarding';

// 组件属性
interface Props {
	stageId: string;
	onboardingId?: string;
	questionnaireData?: any;
	isStageCompleted?: boolean;
}

const props = defineProps<Props>();

const formData = ref<Record<string, any>>({});
const activeCollapses = ref<string[]>([]); // 初始为空数组，稍后会填充所有section的id
const loading = ref(false);

// 计算属性 - 检查是否有问卷数据
const hasQuestionnaireData = computed(() => {
	return props.questionnaireData && props.questionnaireData.id;
});

// 格式化问卷数据 - 现在处理单个问卷对象
const formattedQuestionnaires = computed(() => {
	if (!hasQuestionnaireData.value) return [];

	const questionnaire = props.questionnaireData;

	try {
		// 处理 structureJson
		let structure: any = {};
		if (questionnaire.structureJson) {
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
			title: questionnaire.name || 'Questionnaire',
			sections: structure.sections.map((section: any) => ({
				...section,
				id: section.id || `section_${Math.random()}`,
				title: section.title || section.name || `Section ${section.id || 'Unknown'}`,
				questions: (section.questions || []).map((question: any) => ({
					...question,
					// 保持原有的title作为问题内容
					question: question.title || question.question || '',
					// 使用原始的question.id，不要重新生成
					id: question.id || `question_${Math.random()}`,
				})),
			})),
		};

		return [processedQuestionnaire]; // 返回数组以保持模板兼容性
	} catch (error) {
		// 即使处理失败，也返回一个基本的结构
		return [
			{
				...questionnaire,
				title: questionnaire.name || 'Questionnaire (Error)',
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

// 监听问卷数据变化，自动展开所有sections
watch(
	formattedQuestionnaires,
	(newQuestionnaires) => {
		if (newQuestionnaires.length > 0) {
			const allSectionIds: string[] = [];
			newQuestionnaires.forEach((questionnaire) => {
				questionnaire.sections.forEach((section: any) => {
					allSectionIds.push(section.id);
				});
			});

			// 只有当activeCollapses为空时才设置默认值，避免重复设置
			if (activeCollapses.value.length === 0 && allSectionIds.length > 0) {
				activeCollapses.value = allSectionIds;
			}
		}
	},
	{ immediate: true }
);

// 处理表单值变化
const handleInputChange = (questionId: string, value: any) => {
	formData.value[questionId] = value;
};

// 处理文件变化
const handleFileChange = (questionId: string, file: any, fileList: any[]) => {
	formData.value[questionId] = fileList;
	handleInputChange(questionId, fileList);
};

// 验证表单
const validateForm = () => {
	let isValid = true;
	const errors: string[] = [];

	if (!formattedQuestionnaires.value || formattedQuestionnaires.value.length === 0) {
		return { isValid: true, errors: [] };
	}

	formattedQuestionnaires.value.forEach((questionnaire, qIndex) => {
		questionnaire.sections.forEach((section: any, sIndex: number) => {
			if (!section.questions || section.questions.length === 0) {
				return;
			}

			section.questions.forEach((question: any, qIdx: number) => {
				if (question.required) {
					const questionText =
						question.title || question.question || `Question ${question.id}`;

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
								const errorMsg = `${questionText} - Please complete all rows in the grid`;
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
								const errorMsg = `${questionText} - Please complete all rows in the grid`;
								errors.push(errorMsg);
							}
						}
					} else {
						// 其他类型的验证
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
							const errorMsg = `${questionText} is required`;
							errors.push(errorMsg);
						}
					}
				}
			});
		});
	});

	return { isValid, errors };
};

// 类型定义
interface QuestionnaireAnswer {
	questionId: string;
	question: string;
	answer: any;
	type: string;
	responseText: string;
}

interface QuestionnaireData {
	questionnaireId: string;
	stageId: string;
	answerJson: QuestionnaireAnswer[];
}

// 转换表单数据为API格式
const transformFormDataForAPI = () => {
	const apiData: QuestionnaireData[] = [];

	for (const questionnaire of formattedQuestionnaires.value) {
		const questionnaireData: QuestionnaireData = {
			questionnaireId: questionnaire.id,
			stageId: props.stageId,
			answerJson: [],
		};

		for (const section of questionnaire.sections) {
			for (const question of section.questions) {
				if (question.type === 'multiple_choice_grid' || question.type === 'checkbox_grid') {
					// 网格类型：为每一行创建单独的答案记录
					if (question.rows && question.rows.length > 0) {
						question.rows.forEach((row: any, rowIndex: number) => {
							const gridKey = `${question.id}_${row.id || rowIndex}`;
							const gridValue = formData.value[gridKey];
							const answer: QuestionnaireAnswer = {
								questionId: gridKey,
								question: `${question.question} - ${row.label}`,
								answer: gridValue,
								type: question.type,
								responseText: Array.isArray(gridValue)
									? gridValue.join(', ')
									: gridValue || '',
							};
							questionnaireData.answerJson.push(answer);
						});
					}
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

	return apiData.map((item) => {
		return {
			...item,
			answerJson: JSON.stringify({
				responses: item.answerJson,
			}),
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

	// 遍历表单数据，生成新格式的数据
	Object.keys(formData.value).forEach((fieldKey) => {
		if (formData.value[fieldKey] !== undefined && formData.value[fieldKey] !== '') {
			// 根据问题类型确定字段类型
			let fieldType = 'text';
			let isRequired = false;

			// 查找对应的问题信息来确定类型和是否必填
			formattedQuestionnaires.value.forEach((questionnaire) => {
				questionnaire.sections.forEach((section: any) => {
					section.questions.forEach((question: any) => {
						// 检查是否是网格类型的字段
						const isGridField = fieldKey.startsWith(`${question.id}_`);

						if (question.id === fieldKey || isGridField) {
							// 映射问题类型到字段类型
							const typeMap = {
								short_answer: 'text',
								text: 'text',
								long_answer: 'textarea',
								paragraph: 'textarea',
								textarea: 'textarea',
								multiple_choice: 'radio',
								radio: 'radio',
								checkboxes: 'checkbox',
								checkbox: 'checkbox',
								dropdown: 'select',
								select: 'select',
								date: 'date',
								time: 'time',
								datetime: 'datetime',
								rating: 'rating',
								linear_scale: 'range',
								file: 'file',
								file_upload: 'file',
								multiple_choice_grid: 'grid',
								checkbox_grid: 'grid',
							};
							fieldType = typeMap[question.type] || 'text';
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

// 加载已保存的问卷答案
const loadSavedAnswers = async () => {
	if (!props.stageId || !props.onboardingId) {
		return;
	}

	try {
		loading.value = true;
		const response = await getQuestionnaireAnswer(props.onboardingId, props.stageId);

		if (response.code === '200' && response.data) {
			// 将保存的答案填充到表单中
			const anserForm = JSON.parse(response.data[0].answerJson);
			anserForm?.responses?.forEach((questionnaireAnswer: any) => {
				if (questionnaireAnswer.questionId) {
					formData.value[questionnaireAnswer.questionId] =
						questionnaireAnswer.responseText;
				}
			});
		}
	} catch (error) {
		// 不显示错误消息，因为可能是第一次加载没有答案数据
	} finally {
		loading.value = false;
	}
};

// 设置表单字段值的方法
const setFieldValues = () => {
	loadSavedAnswers();
};

// 监听 stageId 和 onboardingId 变化
watch(
	() => [props.stageId, props.onboardingId],
	async ([newStageId, newOnboardingId]) => {
		if (newStageId && newOnboardingId) {
			await loadSavedAnswers();
		}
	},
	{ immediate: false }
);

// 初始化
onMounted(async () => {
	// 初始化表单数据
	if (hasQuestionnaireData.value) {
		formattedQuestionnaires.value.forEach((questionnaire) => {
			questionnaire.sections.forEach((section: any) => {
				section.questions.forEach((question: any) => {
					// 根据问题类型初始化表单数据
					if (question.type === 'multiple_choice_grid') {
						// 多选网格：为每一行初始化多选值（数组）
						if (question.rows && question.rows.length > 0) {
							question.rows.forEach((row: any, rowIndex: number) => {
								formData.value[`${question.id}_${row.id || rowIndex}`] = [];
							});
						}
					} else if (question.type === 'checkbox_grid') {
						// 单选网格：为每一行初始化单选值
						if (question.rows && question.rows.length > 0) {
							question.rows.forEach((row: any, rowIndex: number) => {
								formData.value[`${question.id}_${row.id || rowIndex}`] = null;
							});
						}
					} else if (question.type === 'checkboxes' || question.type === 'checkbox') {
						// 多选题：初始化为数组
						formData.value[question.id] = [];
					} else {
						// 其他类型：初始化为空字符串
						formData.value[question.id] = '';
					}
				});
			});
		});
	}

	// 无论是否有问卷数据，都尝试加载已保存的答案
	await loadSavedAnswers();
});

defineExpose({
	validateForm,
	transformFormDataForAPI,
	getFormData,
	setFieldValues,
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

/* 多问卷标题样式 */
.questionnaire-header {
	margin: 24px 0 16px 0;

	&:first-child {
		margin-top: 0;
	}
}

.questionnaire-title {
	font-size: 18px;
	font-weight: 600;
	color: #1f2937;
	margin: 0 0 8px 0;
	display: flex;
	align-items: center;
	gap: 8px;
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

.questionnaire-divider {
	margin: 8px 0 0 0;
}

.questionnaire-error {
	margin-bottom: 16px;
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

	/* 多问卷暗色主题 */
	.questionnaire-title {
		color: var(--white-100);
	}

	.error-indicator {
		color: #fca5a5;
		background-color: rgba(239, 68, 68, 0.1);
		border-color: rgba(239, 68, 68, 0.3);
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
