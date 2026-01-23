<template>
	<div class="condition-rule-form">
		<!-- Loading 骨架屏 -->
		<template v-if="isLoading">
			<div class="rules-list">
				<div v-for="(_, index) in modelValue" :key="index" class="rule-item">
					<div class="rule-header">
						<el-skeleton :rows="0" animated style="width: 80px">
							<template #template>
								<el-skeleton-item variant="text" style="width: 80px" />
							</template>
						</el-skeleton>
					</div>
					<el-skeleton :rows="3" animated />
				</div>
			</div>
		</template>

		<!-- 实际内容 -->
		<template v-else>
			<!-- 逻辑运算符选择 -->
			<div class="logic-selector" v-if="modelValue.length > 1">
				<span class="logic-label">Match</span>
				<el-radio-group v-model="logicValue" size="small">
					<el-radio-button value="AND">All (AND)</el-radio-button>
					<el-radio-button value="OR">Any (OR)</el-radio-button>
				</el-radio-group>
				<span class="logic-hint">of the following rules</span>
			</div>

			<!-- 规则列表 -->
			<div class="rules-list">
				<div v-for="(rule, index) in modelValue" :key="index" class="rule-item">
					<el-form
						:ref="(el: any) => setFormRef(el, index)"
						:model="rule"
						:rules="getRuleValidationRules(rule, index)"
						label-position="top"
						:validate-on-rule-change="false"
						@submit.prevent
					>
						<div class="rule-header">
							<span class="rule-number">Rule {{ index + 1 }}</span>
							<el-button
								type="danger"
								link
								:disabled="modelValue.length <= 1"
								@click="handleRemoveRule(index)"
								:icon="Delete"
							/>
						</div>

						<!-- Select Component: 显示具体的问卷名称、checklist名称、或 Required Field -->
						<el-form-item label="Select Component" prop="componentType">
							<el-select
								v-model="ruleComponentKeys[index]"
								placeholder="Select component"
								@change="(val: string) => handleComponentChange(rule, val, index)"
							>
								<el-option-group
									v-for="group in componentOptionGroups"
									:key="group.type"
									:label="group.label"
								>
									<el-option
										v-for="item in group.items"
										:key="item.key"
										:label="item.name"
										:value="item.key"
									/>
								</el-option-group>
							</el-select>
						</el-form-item>

						<!-- 以下字段仅在选择了组件后显示 -->
						<template v-if="rule.componentType">
							<!-- 第二级选择：问题/任务（字段类型不需要第二级选择） -->
							<el-form-item
								v-if="rule.componentType !== 'fields'"
								:label="getFieldLabel(rule.componentType)"
								prop="fieldPath"
							>
								<el-select
									v-model="rule.fieldPath"
									placeholder="Select field"
									:loading="loadingFields[index]"
									@change="() => handleFieldChange(rule, index)"
								>
									<el-option
										v-for="field in ruleFieldOptions[index] || []"
										:key="field.value"
										:label="field.label"
										:value="field.value"
									/>
								</el-select>
							</el-form-item>

							<template v-if="!!rule?.fieldPath">
								<!-- Operator (非 checklist 类型) -->
								<el-form-item
									v-if="rule.componentType !== 'checklist'"
									label="Operator"
									prop="operator"
								>
									<el-select
										v-model="rule.operator"
										placeholder="Select operator"
									>
										<el-option
											v-for="op in getOperatorsForRule(rule, index)"
											:key="op.value"
											:label="op.label"
											:value="op.value"
										/>
									</el-select>
								</el-form-item>

								<!-- Checklist 专用 Operator -->
								<el-form-item v-else label="Trigger When" prop="operator">
									<el-select v-model="rule.operator" placeholder="Select trigger">
										<el-option
											v-for="op in checklistOperators"
											:key="op.value"
											:label="op.label"
											:value="op.value"
										/>
									</el-select>
								</el-form-item>
							</template>
						</template>

						<!-- Value (非 checklist 类型，且已选择组件) -->
						<el-form-item
							v-if="rule.componentType !== 'checklist'"
							label="Value"
							prop="value"
						>
							<!-- Grid 类型：单独处理 -->
							<template v-if="isGridType(index)">
								<div class="grid-selectors w-full mb-3 flex items-center gap-x-2">
									<el-form-item label="Row" class="w-6/12" prop="rowKey">
										<el-select v-model="rule.rowKey" placeholder="Select row">
											<el-option
												v-for="row in getGridRowOptions(index)"
												:key="row.value"
												:label="row.label"
												:value="row.value"
											/>
										</el-select>
									</el-form-item>
									<el-form-item label="Column" class="w-6/12" prop="columnKey">
										<el-select
											v-model="rule.columnKey"
											placeholder="Select column"
											@change="
												(val: string) =>
													handleGridColumnChange(rule, index, val)
											"
										>
											<el-option
												v-for="col in getGridColumnOptions(index)"
												:key="col.value"
												:label="col.label"
												:value="col.value"
											/>
										</el-select>
									</el-form-item>
								</div>
								<!-- Grid Value 输入：short_answer_grid 或 Other 列使用文本输入，否则使用选中/未选中 -->
								<el-input
									v-if="isGridTextInput(index)"
									v-model="rule.value"
									placeholder="Enter value"
								/>
								<el-select v-else v-model="rule.value" placeholder="Select value">
									<el-option label="Selected" value="true" />
									<el-option label="Not Selected" value="false" />
								</el-select>
							</template>

							<!-- 非 Grid 类型：根据值输入类型渲染不同控件 -->
							<template v-else>
								<!-- 下拉选择类型 -->
								<el-select
									v-if="getValueInputType(rule, index) === 'select'"
									v-model="rule.value"
									placeholder="Select value"
								>
									<el-option
										v-for="opt in getValueOptions(rule, index)"
										:key="opt.value"
										:label="opt.label"
										:value="opt.value"
									/>
								</el-select>

								<!-- 数字输入类型 -->
								<InputNumber
									v-else-if="getValueInputType(rule, index) === 'number'"
									v-model="rule.value"
									:is-foloat="getFieldConstraints(rule).isFloat ?? true"
									:minus-number="getFieldConstraints(rule).allowNegative ?? false"
									:is-financial="getFieldConstraints(rule).isFinancial ?? false"
									:decimal-places="getFieldConstraints(rule).decimalPlaces ?? 2"
									:property="{ placeholder: 'Enter number' }"
								/>

								<!-- 日期选择类型 -->
								<el-date-picker
									v-else-if="getValueInputType(rule, index) === 'date'"
									v-model="rule.value"
									:type="getFieldConstraints(rule).dateType || 'date'"
									placeholder="Select date"
									:format="getFieldConstraints(rule).dateFormat || 'YYYY-MM-DD'"
									:value-format="
										getFieldConstraints(rule).dateFormat || 'YYYY-MM-DD'
									"
									class="w-full"
								/>

								<!-- 时间选择类型 -->
								<el-time-picker
									v-else-if="getValueInputType(rule, index) === 'time'"
									v-model="rule.value"
									placeholder="Select time"
									format="HH:mm"
									value-format="HH:mm"
									class="w-full"
								/>

								<!-- 人员选择类型 -->
								<FlowflexUserSelector
									v-else-if="getValueInputType(rule, index) === 'people'"
									v-model="rule.value"
									selection-type="user"
									placeholder="Select user"
								/>

								<!-- 电话输入类型 -->
								<MergedArea
									v-else-if="getValueInputType(rule, index) === 'phone'"
									v-model="rule.value"
								/>

								<!-- 默认文本输入 (包括 text, hidden/file 类型) -->
								<el-input
									v-else
									v-model="rule.value"
									placeholder="Enter value"
									:maxlength="getFieldConstraints(rule).maxLength"
									:show-word-limit="!!getFieldConstraints(rule).maxLength"
								/>
							</template>
						</el-form-item>
					</el-form>
				</div>
			</div>
		</template>

		<!-- 添加规则按钮 -->
		<el-button type="primary" link @click="handleAddRule">
			<el-icon class="mr-1"><Plus /></el-icon>
			Add Rule
		</el-button>
	</div>
</template>

<script setup lang="ts">
import { reactive, watch, onMounted, computed, ref } from 'vue';
import { Plus, Delete } from '@element-plus/icons-vue';
import type { FormInstance, FormRules } from 'element-plus';
import type {
	RuleFormItem,
	DynamicFieldConstraints,
	QuestionMetadata,
	FieldOption,
	ValueOption,
	ComponentOption,
	ComponentOptionGroup,
} from '#/condition';

import type { Stage } from '#/onboard';
import type { DynamicList, DynamicDropdownItem } from '#/dynamic';
import { propertyTypeEnum } from '@/enums/appEnum';
import {
	dynamicFieldInputTypeMap,
	questionTypeInputMap,
	unsupportedQuestionTypes,
	dynamicFieldOperatorMap,
	questionTypeOperatorMap,
	checklistOperators,
	allOperators,
} from '@/enums/conditionEnum';
import { getQuestionnaireDetail } from '@/apis/ow/questionnaire';
import { getChecklistDetail } from '@/apis/ow/checklist';
import { batchIdsDynamicFields } from '@/apis/global/dyanmicField';
import FlowflexUserSelector from '@/components/form/flowflexUser/index.vue';
import InputNumber from '@/components/form/InputNumber/index.vue';
import MergedArea from '@/components/form/inputPhone/mergedArea.vue';

// ============ 类型定义 ============

// 值输入类型（从 condition.d.ts 导入的类型别名）
type ValueInputType = 'text' | 'number' | 'select' | 'date' | 'time' | 'people' | 'phone';

// Props
const props = defineProps<{
	modelValue: RuleFormItem[];
	stages: Stage[];
	currentStageIndex: number;
	logic: 'AND' | 'OR';
}>();

// Emits
const emit = defineEmits<{
	(e: 'update:modelValue', value: RuleFormItem[]): void;
	(e: 'update:logic', value: 'AND' | 'OR'): void;
}>();

// 逻辑运算符的计算属性
const logicValue = computed({
	get: () => props.logic,
	set: (val: 'AND' | 'OR') => emit('update:logic', val),
});

// 表单引用映射（按规则索引）
const formRefs = reactive<Record<number, FormInstance | null>>({});

// 设置表单引用
const setFormRef = (el: FormInstance | null, index: number) => {
	formRefs[index] = el;
};

// 获取规则的验证规则
const getRuleValidationRules = (rule: RuleFormItem, index: number): FormRules => {
	const noValueOperators = ['IsEmpty', 'IsNotEmpty'];
	const needsValue =
		rule.componentType !== 'checklist' && !noValueOperators.includes(rule.operator);

	return {
		componentType: [
			{ required: true, message: 'Please select a component', trigger: 'change' },
		],
		fieldPath: [
			{
				required: rule.componentType !== 'fields',
				message: `Please select a ${getFieldLabel(rule.componentType).toLowerCase()}`,
				trigger: 'change',
			},
		],
		operator: [{ required: true, message: 'Please select an operator', trigger: 'change' }],
		value: [
			{
				required: needsValue,
				message: 'Please enter a value',
				trigger: ['change', 'blur'],
				validator: (_rule: any, value: any, callback: any) => {
					if (!needsValue) {
						callback();
						return;
					}
					if (value === '' || value === undefined || value === null) {
						callback(new Error('Please enter a value'));
					} else {
						callback();
					}
				},
			},
		],
	};
};

// 每个规则的字段选项（按规则索引）
const ruleFieldOptions = reactive<Record<number, FieldOption[]>>({});

// 每个规则选择的组件key
const ruleComponentKeys = reactive<Record<number, string>>({});

// 加载状态（按规则索引）
const loadingFields = reactive<Record<number, boolean>>({});

// 静态字段映射缓存
const staticFieldsMap = ref<Map<string, DynamicList>>(new Map());

// 每个规则的 Value 选项（用于下拉类型字段）
const ruleValueOptions = reactive<Record<number, ValueOption[]>>({});

// 问题元数据映射（按规则索引）
const questionMetadataMap = reactive<Record<number, QuestionMetadata>>({});

// ============ Grid 类型辅助函数 ============

/**
 * 判断问题是否为 Grid 类型
 */
const isGridType = (ruleIndex: number): boolean => {
	const metadata = questionMetadataMap[ruleIndex];
	if (!metadata) return false;
	return ['multiple_choice_grid', 'checkbox_grid', 'short_answer_grid'].includes(metadata.type);
};

/**
 * 获取 Grid 问题的行选项
 */
const getGridRowOptions = (ruleIndex: number): ValueOption[] => {
	const metadata = questionMetadataMap[ruleIndex];
	if (!metadata?.rows?.length) return [];
	return metadata.rows.map((row) => ({
		label: row.label,
		value: row.id,
	}));
};

/**
 * 获取 Grid 问题的列选项
 */
const getGridColumnOptions = (ruleIndex: number): ValueOption[] => {
	const metadata = questionMetadataMap[ruleIndex];
	if (!metadata?.columns?.length) return [];
	return metadata.columns.map((col) => ({
		label: col.label + (col.isOther ? ' (Other)' : ''),
		value: col.id,
	}));
};

/**
 * 判断 Grid 类型是否需要文本输入
 * - short_answer_grid 类型：始终使用文本输入
 * - multiple_choice_grid / checkbox_grid 类型：如果选择的列是 Other，使用文本输入
 */
const isGridTextInput = (ruleIndex: number): boolean => {
	const metadata = questionMetadataMap[ruleIndex];
	if (!metadata) return true;

	// short_answer_grid 始终使用文本输入
	if (metadata.type === 'short_answer_grid') {
		return true;
	}

	// multiple_choice_grid / checkbox_grid：检查选择的列是否是 Other
	const rule = props.modelValue[ruleIndex];
	const selectedColId = rule?.columnKey;
	if (selectedColId && metadata.columns?.length) {
		const selectedCol = metadata.columns.find((col) => col.id === selectedColId);
		if (selectedCol?.isOther) {
			return true;
		}
	}

	return false;
};

/**
 * 判断列是否是 Other 类型
 */
const isColumnOther = (ruleIndex: number, colId: string): boolean => {
	const metadata = questionMetadataMap[ruleIndex];
	if (!metadata?.columns?.length) return false;
	const col = metadata.columns.find((c) => c.id === colId);
	return col?.isOther ?? false;
};

// 保存上一次的列选择 Other 状态（用于检测切换）
const prevColumnOtherState = reactive<Record<number, boolean>>({});

/**
 * 处理 Grid 列选择变化
 * 当从普通列切换到 Other 列或从 Other 列切换到普通列时，清空 value
 */
const handleGridColumnChange = (rule: RuleFormItem, ruleIndex: number, newColId: string) => {
	const wasOther = prevColumnOtherState[ruleIndex] ?? false;
	const isNowOther = isColumnOther(ruleIndex, newColId);

	// 如果 Other 状态发生变化，清空 value
	if (wasOther !== isNowOther) {
		rule.value = '';
	}

	// 更新状态
	prevColumnOtherState[ruleIndex] = isNowOther;
};

// Loading 状态
const loadingStaticFields = ref(false);

// 整体 loading 状态
const isLoading = computed(() => loadingStaticFields.value);

// 获取当前 Stage
const getCurrentStage = (): Stage | undefined => {
	return props.stages[props.currentStageIndex];
};

// 构建组件选项分组列表
const componentOptionGroups = computed<ComponentOptionGroup[]>(() => {
	const stage = getCurrentStage();
	if (!stage?.components) return [];

	const groups: ComponentOptionGroup[] = [];
	const questionnaireItems: ComponentOption[] = [];
	const checklistItems: ComponentOption[] = [];
	const fieldItems: ComponentOption[] = [];

	// 遍历所有组件，收集问卷、checklist 和静态字段
	stage.components.forEach((comp) => {
		if (!comp.isEnabled) return;

		if (comp.key === 'questionnaires' && comp.questionnaireIds?.length) {
			// 收集所有问卷
			comp.questionnaireIds.forEach((id, index) => {
				questionnaireItems.push({
					key: `questionnaire_${id}`,
					type: 'questionnaires' as const,
					id,
					name: comp.questionnaireNames?.[index] || `Questionnaire ${index + 1}`,
				});
			});
		} else if (comp.key === 'checklist' && comp.checklistIds?.length) {
			// 收集所有 checklist
			comp.checklistIds.forEach((id, index) => {
				checklistItems.push({
					key: `checklist_${id}`,
					type: 'checklist' as const,
					id,
					name: comp.checklistNames?.[index] || `Checklist ${index + 1}`,
				});
			});
		} else if (comp.key === 'fields' && comp.staticFields?.length) {
			// 收集所有动态字段，显示具体字段名称
			comp.staticFields.forEach((field) => {
				const fieldInfo = staticFieldsMap.value.get(field.id);
				fieldItems.push({
					key: `field_${field.id}`,
					type: 'fields' as const,
					id: field.id,
					name: fieldInfo?.fieldName || field.id,
				});
			});
		}
	});

	// 添加动态字段组
	if (fieldItems.length > 0) {
		groups.push({
			type: 'fields',
			label: 'Required Fields',
			items: fieldItems,
		});
	}

	// 添加 Checklist 组
	if (checklistItems.length > 0) {
		groups.push({ type: 'checklist', label: 'Checklists', items: checklistItems });
	}

	// 添加问卷组
	if (questionnaireItems.length > 0) {
		groups.push({ type: 'questionnaires', label: 'Questionnaires', items: questionnaireItems });
	}

	return groups;
});

// 根据组件类型获取 Field label
const getFieldLabel = (componentType: string): string => {
	const labelMap: Record<string, string> = {
		questionnaires: 'Question',
		checklist: 'Task',
		fields: 'Field',
	};
	return labelMap[componentType] || 'Field';
};

// 解析组件key获取类型和ID
const parseComponentKey = (key: string): { type: string; id?: string } => {
	if (key === 'fields') {
		return { type: 'fields' };
	}
	if (key.startsWith('field_')) {
		return { type: 'fields', id: key.replace('field_', '') };
	}
	if (key.startsWith('questionnaire_')) {
		return { type: 'questionnaires', id: key.replace('questionnaire_', '') };
	}
	if (key.startsWith('checklist_')) {
		return { type: 'checklist', id: key.replace('checklist_', '') };
	}
	return { type: 'fields' };
};

// 根据rule生成组件key
const generateComponentKey = (rule: RuleFormItem): string => {
	if (rule.componentType === 'fields') {
		// 从 fieldPath 中提取字段 ID: input.fields.{fieldId}
		if (rule.fieldPath) {
			const match = rule.fieldPath.match(/input\.fields\.(.+)/);
			if (match) {
				return `field_${match[1]}`;
			}
		}
		return 'fields';
	}
	if (rule.componentType === 'questionnaires' && rule.componentId) {
		return `questionnaire_${rule.componentId}`;
	}
	if (rule.componentType === 'checklist' && rule.componentId) {
		return `checklist_${rule.componentId}`;
	}
	return '';
};

// 从 fieldPath 中提取字段 ID
const getFieldIdFromPath = (fieldPath: string): string | null => {
	const match = fieldPath.match(/input\.fields\.(.+)/);
	return match ? match[1] : null;
};

// 获取字段信息
const getFieldInfo = (rule: RuleFormItem): DynamicList | undefined => {
	if (rule.componentType !== 'fields') return undefined;
	const fieldId = getFieldIdFromPath(rule.fieldPath);
	if (!fieldId) return undefined;
	return staticFieldsMap.value.get(fieldId);
};

/**
 * 获取动态字段的约束配置
 */
const getFieldConstraints = (rule: RuleFormItem): DynamicFieldConstraints => {
	const fieldInfo = getFieldInfo(rule);
	if (!fieldInfo) return {};

	const constraints: DynamicFieldConstraints = {};

	// Number 类型约束
	if (fieldInfo.dataType === propertyTypeEnum.Number) {
		constraints.isFloat = fieldInfo.additionalInfo?.isFloat ?? true;
		constraints.allowNegative = fieldInfo.additionalInfo?.allowNegative ?? false;
		constraints.isFinancial = fieldInfo.additionalInfo?.isFinancial ?? false;
		constraints.decimalPlaces = Number(fieldInfo.format?.decimalPlaces) || 2;
	}

	// DatePicker 类型约束
	if (fieldInfo.dataType === propertyTypeEnum.DatePicker) {
		constraints.dateFormat = fieldInfo.format?.dateFormat || 'YYYY-MM-DD';
		// 根据格式判断是否包含时间
		const format = fieldInfo.format?.dateFormat || '';
		constraints.dateType = format.includes('HH:mm') ? 'datetime' : 'date';
	}

	// Text 类型约束
	if (
		fieldInfo.dataType === propertyTypeEnum.SingleLineText ||
		fieldInfo.dataType === propertyTypeEnum.MultilineText
	) {
		constraints.maxLength = fieldInfo.fieldValidate?.maxLength;
	}

	// DropdownSelect 类型约束
	if (fieldInfo.dataType === propertyTypeEnum.DropdownSelect) {
		constraints.allowMultiple = fieldInfo.additionalInfo?.allowMultiple ?? false;
		constraints.allowSearch = fieldInfo.additionalInfo?.allowSearch ?? true;
	}

	return constraints;
};

// 获取字段的值选项
const getValueOptions = (rule: RuleFormItem, ruleIndex: number): ValueOption[] => {
	// 如果已经缓存了选项，直接返回
	if (ruleValueOptions[ruleIndex]?.length > 0) {
		return ruleValueOptions[ruleIndex];
	}

	const options: ValueOption[] = [];

	// 动态字段类型
	if (rule.componentType === 'fields') {
		const fieldInfo = getFieldInfo(rule);
		if (!fieldInfo) return [];

		if (fieldInfo.dataType === propertyTypeEnum.DropdownSelect) {
			// 下拉框类型，使用 dropdownItems
			if (fieldInfo.dropdownItems?.length) {
				fieldInfo.dropdownItems.forEach((item: DynamicDropdownItem) => {
					options.push({
						label: item.value,
						value: item.value,
					});
				});
			}
		} else if (fieldInfo.dataType === propertyTypeEnum.Switch) {
			// 开关类型，固定 Yes/No 选项
			const trueLabel = fieldInfo.additionalInfo?.trueLabel || 'Yes';
			const falseLabel = fieldInfo.additionalInfo?.falseLabel || 'No';
			options.push(
				{ label: trueLabel, value: 'true' },
				{ label: falseLabel, value: 'false' }
			);
		}
	}

	// 问卷问题类型
	if (rule.componentType === 'questionnaires') {
		const metadata = questionMetadataMap[ruleIndex];
		if (metadata?.options?.length) {
			// 选择类型问题 (multiple_choice, checkboxes, dropdown)
			metadata.options.forEach((opt) => {
				options.push({
					label: opt.label,
					value: opt.value || opt.label,
				});
			});
		} else if (metadata?.type === 'rating') {
			// 评分类型：生成 1 到 max 的选项
			const max = metadata.max || 5;
			for (let i = 1; i <= max; i++) {
				options.push({
					label: `${i} Star${i > 1 ? 's' : ''}`,
					value: String(i),
				});
			}
		} else if (metadata?.type === 'linear_scale') {
			// 线性量表：生成 min 到 max 的选项
			const min = metadata.min || 1;
			const max = metadata.max || 10;
			for (let i = min; i <= max; i++) {
				let label = String(i);
				if (i === min && metadata.minLabel) {
					label = `${i} - ${metadata.minLabel}`;
				} else if (i === max && metadata.maxLabel) {
					label = `${i} - ${metadata.maxLabel}`;
				}
				options.push({ label, value: String(i) });
			}
		}
		// Grid 类型使用文本输入，不需要选项
	}

	// 缓存选项
	ruleValueOptions[ruleIndex] = options;
	return options;
};

// 加载静态字段映射
const loadStaticFieldsMapping = async () => {
	const stage = getCurrentStage();
	if (!stage?.components) return;

	const fieldsComponent = stage.components.find((c) => c.key === 'fields');
	if (!fieldsComponent?.staticFields?.length) return;

	const fieldIds = fieldsComponent.staticFields.map((f) => f.id);

	// 过滤掉已经缓存的ID
	const uncachedIds = fieldIds.filter((id) => !staticFieldsMap.value.has(id));

	if (uncachedIds.length === 0) return;

	loadingStaticFields.value = true;
	try {
		const res: any = await batchIdsDynamicFields({ ids: uncachedIds });
		if (res.code === '200' && res.data) {
			res.data.forEach((field: DynamicList) => {
				staticFieldsMap.value.set(field.id, field);
			});
		}
	} catch (error) {
		console.error('Failed to load static fields mapping:', error);
	} finally {
		loadingStaticFields.value = false;
	}
};

// 加载问卷问题列表
const loadQuestionnaireQuestions = async (
	questionnaireId: string,
	ruleIndex: number,
	restoreMetadata = false
) => {
	loadingFields[ruleIndex] = true;
	const fields: FieldOption[] = [];

	try {
		const res: any = await getQuestionnaireDetail(questionnaireId);
		if (res.code === '200' && res.data) {
			// 解析 structureJson 获取问题
			let questions: any[] = [];
			if (res.data.structureJson) {
				try {
					const structure = JSON.parse(res.data.structureJson);
					// 遍历 sections 获取所有问题
					if (structure.sections) {
						structure.sections.forEach((section: any) => {
							if (section.questions) {
								questions = questions.concat(section.questions);
							}
						});
					}
				} catch (e) {
					console.error('Failed to parse structureJson:', e);
				}
			}

			// 过滤不支持条件配置的问题类型
			questions = questions.filter((q) => !unsupportedQuestionTypes.includes(q.type));

			questions.forEach((q: any) => {
				const questionId = q.id || q.questionId || q.temporaryId;
				fields.push({
					label: q.title || q.question || q.questionText || `Question ${q.order || ''}`,
					value: `input.questionnaire.answers["${questionnaireId}"]["${questionId}"]`,
					// 存储问题元数据供后续使用
					metadata: {
						type: q.type,
						options: q.options?.map((opt: any) => ({
							id: opt.id || opt.temporaryId,
							label: opt.label || opt.value,
							value: opt.value || opt.label,
						})),
						rows: q.rows?.map((row: any) => ({
							id: row.id,
							label: row.label,
						})),
						columns: q.columns?.map((col: any) => ({
							id: col.id,
							label: col.label,
							isOther: col.isOther,
						})),
						min: q.min,
						max: q.max,
						minLabel: q.minLabel,
						maxLabel: q.maxLabel,
						iconType: q.iconType || 'star',
					},
				});
			});

			// 如果需要恢复元数据（初始化现有规则时）
			if (restoreMetadata) {
				const rule = props.modelValue[ruleIndex];
				if (rule?.fieldPath) {
					const selectedField = fields.find((f) => f.value === rule.fieldPath);
					if (selectedField?.metadata) {
						questionMetadataMap[ruleIndex] = selectedField.metadata;

						// 如果是 Grid 类型且有 columnKey，初始化 prevColumnOtherState
						if (
							['multiple_choice_grid', 'checkbox_grid', 'short_answer_grid'].includes(
								selectedField.metadata.type
							) &&
							rule.columnKey
						) {
							const col = selectedField.metadata.columns?.find(
								(c) => c.id === rule.columnKey
							);
							prevColumnOtherState[ruleIndex] = col?.isOther ?? false;
						}
					}
				}
			}
		}
	} catch (error) {
		console.error('Failed to load questionnaire questions:', error);
	} finally {
		ruleFieldOptions[ruleIndex] = fields;
		loadingFields[ruleIndex] = false;
	}
};

// 加载 Checklist 任务列表
const loadChecklistTasks = async (checklistId: string, ruleIndex: number) => {
	loadingFields[ruleIndex] = true;
	const fields: FieldOption[] = [];

	try {
		const res: any = await getChecklistDetail(checklistId);
		if (res.code === '200' && res.data) {
			const tasks = res.data.tasks || [];
			tasks.forEach((task: any, index: number) => {
				fields.push({
					label: task.name || `Task ${index + 1}`,
					value: `input.checklist.tasks["${checklistId}"]["${task.id}"].isCompleted`,
				});
			});
		}
	} catch (error) {
		console.error('Failed to load checklist tasks:', error);
	} finally {
		ruleFieldOptions[ruleIndex] = fields;
		loadingFields[ruleIndex] = false;
	}
};

// 处理组件选择变化
const handleComponentChange = (rule: RuleFormItem, componentKey: string, ruleIndex: number) => {
	const { type, id } = parseComponentKey(componentKey);

	rule.componentType = type as any;
	rule.componentId = id;
	rule.fieldPath = '';
	ruleFieldOptions[ruleIndex] = [];
	ruleValueOptions[ruleIndex] = []; // 清空值选项

	// 清除 Grid 选择
	rule.rowKey = undefined;
	rule.columnKey = undefined;

	// 清除问题元数据
	delete questionMetadataMap[ruleIndex];

	// Checklist 类型设置默认 operator 并清空 value
	if (type === 'checklist') {
		rule.operator = 'CompleteTask';
		rule.value = '';
	} else {
		rule.operator = '==';
		rule.value = '';
	}

	// 根据类型加载对应的字段选项
	if (type === 'fields') {
		// 选择具体字段时，直接设置 fieldPath
		if (id) {
			rule.fieldPath = `input.fields.${id}`;
			// 字段类型不需要第二级选择，清空选项
			ruleFieldOptions[ruleIndex] = [];
			// 预加载值选项
			getValueOptions(rule, ruleIndex);
		}
	} else if (type === 'questionnaires' && id) {
		loadQuestionnaireQuestions(id, ruleIndex);
	} else if (type === 'checklist' && id) {
		loadChecklistTasks(id, ruleIndex);
	}
};

/**
 * 根据规则获取值输入类型
 */
const getValueInputType = (rule: RuleFormItem, ruleIndex: number): ValueInputType => {
	// 动态字段类型
	if (rule.componentType === 'fields') {
		const fieldInfo = getFieldInfo(rule);
		if (!fieldInfo) return 'text';
		return dynamicFieldInputTypeMap[fieldInfo.dataType] || 'text';
	}

	// 问卷问题类型
	if (rule.componentType === 'questionnaires') {
		const metadata = questionMetadataMap[ruleIndex];
		if (!metadata) return 'text';
		return questionTypeInputMap[metadata.type] || 'text';
	}

	return 'text';
};

/**
 * 根据规则获取可用的操作符列表
 */
const getOperatorsForRule = (rule: RuleFormItem, ruleIndex: number) => {
	// Checklist 类型使用专用操作符
	if (rule.componentType === 'checklist') {
		return checklistOperators;
	}

	// 动态字段类型
	if (rule.componentType === 'fields') {
		const fieldInfo = getFieldInfo(rule);
		if (!fieldInfo) return allOperators;
		return dynamicFieldOperatorMap[fieldInfo.dataType] || allOperators;
	}

	// 问卷问题类型
	if (rule.componentType === 'questionnaires') {
		const metadata = questionMetadataMap[ruleIndex];
		if (!metadata) return allOperators;
		return questionTypeOperatorMap[metadata.type] || allOperators;
	}

	return allOperators;
};

// 处理字段选择变化（checklist 类型需要设置默认 operator）
const handleFieldChange = (rule: RuleFormItem, ruleIndex: number) => {
	// 清除之前的值
	rule.value = '';
	ruleValueOptions[ruleIndex] = [];

	// 清除 Grid 选择
	rule.rowKey = undefined;
	rule.columnKey = undefined;

	// 如果是问卷问题，存储元数据
	if (rule.componentType === 'questionnaires') {
		const fieldOptions = ruleFieldOptions[ruleIndex] || [];
		const selectedField = fieldOptions.find((f) => f.value === rule.fieldPath);
		if (selectedField?.metadata) {
			questionMetadataMap[ruleIndex] = selectedField.metadata;

			// 如果有选项，预加载值选项
			if (selectedField.metadata.options?.length) {
				ruleValueOptions[ruleIndex] = selectedField.metadata.options.map((opt) => ({
					label: opt.label,
					value: opt.value || opt.label,
				}));
			}
		} else {
			delete questionMetadataMap[ruleIndex];
		}

		// 重置操作符为该类型的第一个可用操作符
		const availableOperators = getOperatorsForRule(rule, ruleIndex);
		if (
			availableOperators.length > 0 &&
			!availableOperators.find((op) => op.value === rule.operator)
		) {
			rule.operator = availableOperators[0].value as any;
		}
	} else if (rule.componentType === 'checklist') {
		// 确保 checklist 类型使用正确的 operator
		if (!['CompleteTask', 'CompleteStage'].includes(rule.operator)) {
			rule.operator = 'CompleteTask';
		}
	}
};

// 添加规则
const handleAddRule = () => {
	const stage = getCurrentStage();

	const newRule: RuleFormItem = {
		sourceStageId: stage?.id || '',
		componentType: '',
		componentId: '',
		fieldPath: '',
		operator: '==',
		value: '',
	};

	const newIndex = props.modelValue.length;
	emit('update:modelValue', [...props.modelValue, newRule]);

	// 设置组件key
	ruleComponentKeys[newIndex] = '';
};

// 删除规则
const handleRemoveRule = (index: number) => {
	if (props.modelValue.length <= 1) return;
	const newRules = props.modelValue.filter((_, i) => i !== index);
	emit('update:modelValue', newRules);

	// 清理对应的状态
	delete ruleFieldOptions[index];
	delete ruleComponentKeys[index];
	delete loadingFields[index];
	delete ruleValueOptions[index];
	delete questionMetadataMap[index];
	delete prevColumnOtherState[index];
};

// 初始化现有规则的字段选项
const initExistingRules = async () => {
	await loadStaticFieldsMapping();

	props.modelValue.forEach((rule, index) => {
		// 生成并设置组件key
		const componentKey = generateComponentKey(rule);
		ruleComponentKeys[index] = componentKey;

		// 加载字段选项（字段类型不需要加载，因为已经在第一级选择了）
		if (rule.componentType === 'questionnaires' && rule.componentId) {
			// 传入 restoreMetadata = true 以恢复元数据和 Grid 选择
			loadQuestionnaireQuestions(rule.componentId, index, true);
		} else if (rule.componentType === 'checklist' && rule.componentId) {
			loadChecklistTasks(rule.componentId, index);
		}
	});

	// 清除所有表单的验证状态
	setTimeout(() => {
		Object.values(formRefs).forEach((formRef) => {
			formRef?.clearValidate();
		});
	}, 0);
};

// 初始化
onMounted(() => {
	initExistingRules();
});

// 监听 stage 变化重新加载
watch(
	() => props.currentStageIndex,
	() => {
		loadStaticFieldsMapping();
		// 重新初始化现有规则
		initExistingRules();
	}
);

// 验证规则完整性
const validate = async (): Promise<{ valid: boolean; message: string }> => {
	if (props.modelValue.length === 0) {
		return { valid: false, message: 'Please add at least one rule' };
	}

	// 验证所有表单
	const validationPromises: Promise<boolean>[] = [];
	for (let i = 0; i < props.modelValue.length; i++) {
		const formRef = formRefs[i];
		if (formRef) {
			validationPromises.push(
				formRef
					.validate()
					.then(() => true)
					.catch(() => false)
			);
		}
	}

	const results = await Promise.all(validationPromises);
	const allValid = results.every((result) => result);

	if (!allValid) {
		return { valid: false, message: '' }; // 错误信息已由表单显示
	}

	return { valid: true, message: '' };
};

// 暴露方法给父组件
defineExpose({
	validate,
});
</script>

<style lang="scss" scoped>
.condition-rule-form {
	@apply flex flex-col gap-4;
}

.logic-selector {
	@apply flex items-center gap-2 p-3 rounded-lg;
	background-color: var(--el-fill-color-lighter);
}

.logic-label {
	@apply text-sm font-medium;
	color: var(--el-text-color-regular);
}

.logic-hint {
	@apply text-sm;
	color: var(--el-text-color-secondary);
}

.rules-list {
	@apply flex flex-col gap-4;
}

.rule-item {
	@apply p-4 rounded-lg border;
	background-color: var(--el-fill-color-lighter);
}

.rule-header {
	@apply flex items-center justify-between mb-3;
}

.rule-number {
	@apply text-sm font-medium  text-primary;
}
</style>
