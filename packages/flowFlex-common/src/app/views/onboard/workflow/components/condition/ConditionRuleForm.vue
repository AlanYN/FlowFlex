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

						<!-- Select Stage: 选择来源阶段 -->
						<el-form-item label="Select Stage" prop="sourceStageId">
							<el-select
								v-model="rule.sourceStageId"
								placeholder="Select stage"
								@change="(val: string) => handleStageChange(rule, val, index)"
							>
								<el-option
									v-for="stage in getAvailableStages()"
									:key="stage.id"
									:label="stage.name"
									:value="stage.id"
								/>
							</el-select>
						</el-form-item>

						<!-- Select Component: 显示具体的问卷名称、checklist名称、或 Required Field -->
						<el-form-item label="Select Component" prop="componentType">
							<el-select
								v-model="ruleComponentKeys[index]"
								placeholder="Select component"
								@change="(val: string) => handleComponentChange(rule, val, index)"
							>
								<el-option-group
									v-for="group in getAvailableComponentOptions(index)"
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
									<template #loading>
										<svg class="circular" viewBox="0 0 50 50">
											<circle
												class="path"
												cx="25"
												cy="25"
												r="20"
												fill="none"
											/>
										</svg>
									</template>
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
import { reactive, onMounted, computed, ref } from 'vue';
import { Plus, Delete } from '@element-plus/icons-vue';
import { ElMessageBox } from 'element-plus';
import type { FormInstance, FormRules } from 'element-plus';
import type {
	RuleFormItem,
	QuestionMetadata,
	FieldOption,
	ValueOption,
	ComponentOption,
	ComponentOptionGroup,
} from '#/condition';

import type { Stage } from '#/onboard';
import type { DynamicList } from '#/dynamic';
import { unsupportedQuestionTypes, checklistOperators } from '@/enums/conditionEnum';
import { getQuestionnaireDetail } from '@/apis/ow/questionnaire';
import { getChecklistDetail } from '@/apis/ow/checklist';
import { batchIdsDynamicFields } from '@/apis/global/dyanmicField';
import FlowflexUserSelector from '@/components/form/flowflexUser/index.vue';
import InputNumber from '@/components/form/InputNumber/index.vue';
import MergedArea from '@/components/form/inputPhone/mergedArea.vue';
import {
	parseComponentKey,
	generateComponentKey,
	getFieldIdFromPath,
	getFieldLabel,
	isGridType as checkIsGridType,
	isGridTextInput as checkIsGridTextInput,
	isColumnOther,
	getValueInputType as getInputType,
	getOperatorsForRule as getOperators,
	getFieldConstraints as getConstraints,
	getGridRowOptions as getRowOptions,
	getGridColumnOptions as getColumnOptions,
	getFieldValueOptions,
	getQuestionValueOptions,
} from '@/utils/ruleUtils';

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
	set: (val: 'AND' | 'OR') => {
		// 从 OR 切换到 AND 时，检查是否有重复的组件选择
		if (props.logic === 'OR' && val === 'AND') {
			const duplicates = findDuplicateComponents();
			if (duplicates.length > 0) {
				ElMessageBox.alert(
					`In AND mode, each component can only be used once. Please remove duplicate rules for: ${duplicates.join(
						', '
					)}`,
					'Duplicate Components Detected',
					{
						type: 'warning',
						confirmButtonText: 'OK',
					}
				);
				return; // 阻止切换
			}
		}
		emit('update:logic', val);
	},
});

// 表单引用映射（按规则索引）
const formRefs = reactive<Record<number, FormInstance | null>>({});

// 追踪每个规则是否已触发过校验
const hasValidated = reactive<Record<number, boolean>>({});

// 设置表单引用
const setFormRef = (el: FormInstance | null, index: number) => {
	formRefs[index] = el;
};

// 获取规则的验证规则
const getRuleValidationRules = (rule: RuleFormItem, index: number): FormRules => {
	const noValueOperators = ['IsEmpty', 'IsNotEmpty'];
	const needsValue =
		rule.componentType !== 'checklist' && !noValueOperators.includes(rule.operator);

	// 校验前不触发，校验后输入可清除错误
	const trigger = hasValidated[index] ? 'change' : [];

	return {
		sourceStageId: [{ required: true, message: 'Please select a stage', trigger }],
		componentType: [{ required: true, message: 'Please select a component', trigger }],
		fieldPath: [
			{
				required: rule.componentType !== 'fields',
				message: `Please select a ${getFieldLabel(rule.componentType).toLowerCase()}`,
				trigger,
			},
		],
		operator: [{ required: true, message: 'Please select an operator', trigger }],
		value: [
			{
				required: needsValue,
				message: 'Please enter a value',
				trigger,
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

// 每个规则选择的 Stage ID（按规则索引）
const ruleStageIds = reactive<Record<number, string>>({});

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

// 组件原始字段总数缓存（componentKey -> totalCount）
const componentTotalFieldCount = ref<Map<string, number>>(new Map());

// ============ 工具函数包装（使用索引访问状态） ============

const isGridType = (ruleIndex: number): boolean => {
	return checkIsGridType(questionMetadataMap[ruleIndex]);
};

const isGridTextInput = (ruleIndex: number): boolean => {
	const rule = props.modelValue[ruleIndex];
	return checkIsGridTextInput(questionMetadataMap[ruleIndex], rule?.columnKey);
};

const getGridRowOptions = (ruleIndex: number): ValueOption[] => {
	return getRowOptions(questionMetadataMap[ruleIndex]);
};

const getGridColumnOptions = (ruleIndex: number): ValueOption[] => {
	return getColumnOptions(questionMetadataMap[ruleIndex]);
};

// 保存上一次的列选择 Other 状态（用于检测切换）
const prevColumnOtherState = reactive<Record<number, boolean>>({});

/**
 * 处理 Grid 列选择变化
 * 当从普通列切换到 Other 列或从 Other 列切换到普通列时，清空 value
 */
const handleGridColumnChange = (rule: RuleFormItem, ruleIndex: number, newColId: string) => {
	const wasOther = prevColumnOtherState[ruleIndex] ?? false;
	const isNowOther = isColumnOther(questionMetadataMap[ruleIndex], newColId);

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

// 获取可选的 Stage 列表（当前 stage 及之前的 stage）
const getAvailableStages = (): Stage[] => {
	return props.stages.slice(0, props.currentStageIndex + 1);
};

// 根据 stageId 获取 Stage
const getStageById = (stageId: string): Stage | undefined => {
	return props.stages.find((s) => s.id === stageId);
};

// 构建组件选项分组列表（根据指定的 stageId）
const getComponentOptionGroupsForStage = (stageId: string): ComponentOptionGroup[] => {
	const stage = getStageById(stageId);
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
};

/**
 * 获取指定规则的可用组件选项
 * 基于该规则选择的 sourceStageId 获取组件
 * AND 模式下：
 * - fields 类型：过滤掉已被其他规则选择的字段
 * - questionnaires 和 checklist 类型：只有当所有问题/任务都被选完时才过滤掉该组件
 * OR 模式下返回全部
 */
const getAvailableComponentOptions = (currentIndex: number): ComponentOptionGroup[] => {
	const rule = props.modelValue[currentIndex];
	const stageId = rule?.sourceStageId || ruleStageIds[currentIndex];

	// 如果没有选择 stage，返回空
	if (!stageId) return [];

	// 获取该 stage 的组件选项
	const stageGroups = getComponentOptionGroupsForStage(stageId);

	// OR 模式下不限制，返回全部选项
	if (props.logic === 'OR') {
		return stageGroups;
	}

	// AND 模式下，收集其他规则已选择的 fields 组件 key
	const selectedFieldKeys = new Set<string>();
	props.modelValue.forEach((r, index) => {
		if (index !== currentIndex && r.sourceStageId === stageId && r.componentType === 'fields') {
			const key = ruleComponentKeys[index];
			if (key) selectedFieldKeys.add(key);
		}
	});

	// 收集每个 questionnaire/checklist 已选择的 fieldPath 数量
	const componentFieldPathCount = new Map<string, number>(); // componentKey -> selected count
	props.modelValue.forEach((r, index) => {
		if (
			index !== currentIndex &&
			r.sourceStageId === stageId &&
			(r.componentType === 'questionnaires' || r.componentType === 'checklist') &&
			r.fieldPath
		) {
			const key = ruleComponentKeys[index];
			if (key) {
				componentFieldPathCount.set(key, (componentFieldPathCount.get(key) || 0) + 1);
			}
		}
	});

	// 过滤组件
	return stageGroups
		.map((group) => ({
			...group,
			items: group.items.filter((item) => {
				// fields 类型：直接过滤已选择的
				if (group.type === 'fields') {
					return !selectedFieldKeys.has(item.key);
				}

				// questionnaires 和 checklist 类型：检查是否所有问题/任务都被选完
				if (group.type === 'questionnaires' || group.type === 'checklist') {
					const selectedCount = componentFieldPathCount.get(item.key) || 0;
					const availableFieldCount = getComponentAvailableFieldCount(item.key);
					// 如果没有缓存总数，不过滤；只有当所有字段都被选完时才过滤掉
					if (availableFieldCount === null) return true;
					return selectedCount < availableFieldCount;
				}

				return true;
			}),
		}))
		.filter((group) => group.items.length > 0); // 移除空分组
};

/**
 * 获取组件的可用字段数量
 */
const getComponentAvailableFieldCount = (componentKey: string): number | null => {
	return componentTotalFieldCount.value.get(componentKey) || null;
};

// 获取字段信息
const getFieldInfo = (rule: RuleFormItem): DynamicList | undefined => {
	if (rule.componentType !== 'fields') return undefined;
	const fieldId = getFieldIdFromPath(rule.fieldPath);
	if (!fieldId) return undefined;
	return staticFieldsMap.value.get(fieldId);
};

// 包装工具函数（需要访问组件状态）
const getFieldConstraints = (rule: RuleFormItem) => {
	return getConstraints(getFieldInfo(rule));
};

const getValueInputType = (rule: RuleFormItem, ruleIndex: number) => {
	return getInputType(rule, getFieldInfo(rule), questionMetadataMap[ruleIndex]);
};

const getOperatorsForRule = (rule: RuleFormItem, ruleIndex: number) => {
	return getOperators(rule, getFieldInfo(rule), questionMetadataMap[ruleIndex]);
};

// 获取字段的值选项
const getValueOptions = (rule: RuleFormItem, ruleIndex: number): ValueOption[] => {
	// 如果已经缓存了选项，直接返回
	if (ruleValueOptions[ruleIndex]?.length > 0) {
		return ruleValueOptions[ruleIndex];
	}

	let options: ValueOption[] = [];

	// 动态字段类型
	if (rule.componentType === 'fields') {
		options = getFieldValueOptions(getFieldInfo(rule));
	}

	// 问卷问题类型
	if (rule.componentType === 'questionnaires') {
		options = getQuestionValueOptions(questionMetadataMap[ruleIndex]);
	}

	// 缓存选项
	ruleValueOptions[ruleIndex] = options;
	return options;
};

// 加载指定 stage 的静态字段映射
const loadStaticFieldsMappingForStage = async (stageId: string) => {
	const stage = getStageById(stageId);
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
	let fields: FieldOption[] = [];

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
					label: q.title || q.question || q.questionText,
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

			// 缓存该问卷的问题总数（在过滤之前）
			const componentKey = `questionnaire_${questionnaireId}`;
			componentTotalFieldCount.value.set(componentKey, fields.length);

			// AND 模式下，过滤掉同一问卷中已被其他规则选择的问题
			if (props.logic === 'AND') {
				const rule = props.modelValue[ruleIndex];
				const selectedQuestionPaths = new Set<string>();

				props.modelValue.forEach((r, idx) => {
					if (
						idx !== ruleIndex &&
						r.componentType === 'questionnaires' &&
						r.componentId === questionnaireId &&
						r.sourceStageId === rule.sourceStageId &&
						r.fieldPath
					) {
						selectedQuestionPaths.add(r.fieldPath);
					}
				});

				fields = fields.filter((f) => !selectedQuestionPaths.has(f.value));
			}

			// 如果需要恢复元数据（初始化现有规则时）
			if (restoreMetadata) {
				const rule = props.modelValue[ruleIndex];
				if (rule?.fieldPath) {
					// 需要从原始 fields 中查找（过滤前的），因为当前规则的 fieldPath 可能已被过滤
					const allFields: FieldOption[] = [];
					questions.forEach((q: any) => {
						const questionId = q.id || q.questionId || q.temporaryId;
						allFields.push({
							label: q.title,
							value: `input.questionnaire.answers["${questionnaireId}"]["${questionId}"]`,
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
					const selectedField = allFields.find((f) => f.value === rule.fieldPath);
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

					// 确保当前规则的 fieldPath 在选项中（即使被过滤了也要加回来）
					if (!fields.find((f) => f.value === rule.fieldPath) && selectedField) {
						fields.push(selectedField);
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
const loadChecklistTasks = async (
	checklistId: string,
	ruleIndex: number,
	restoreMetadata = false
) => {
	loadingFields[ruleIndex] = true;
	let fields: FieldOption[] = [];
	let allFields: FieldOption[] = [];

	try {
		const res: any = await getChecklistDetail(checklistId);
		if (res.code === '200' && res.data) {
			const tasks = res.data.tasks || [];
			tasks.forEach((task: any) => {
				const field = {
					label: task.name,
					value: `input.checklist.tasks["${checklistId}"]["${task.id}"].isCompleted`,
				};
				fields.push(field);
				allFields.push(field);
			});

			// 缓存该 checklist 的任务总数（在过滤之前）
			const componentKey = `checklist_${checklistId}`;
			componentTotalFieldCount.value.set(componentKey, fields.length);

			// AND 模式下，过滤掉同一 checklist 中已被其他规则选择的任务
			if (props.logic === 'AND') {
				const rule = props.modelValue[ruleIndex];
				const selectedTaskPaths = new Set<string>();

				props.modelValue.forEach((r, idx) => {
					if (
						idx !== ruleIndex &&
						r.componentType === 'checklist' &&
						r.componentId === checklistId &&
						r.sourceStageId === rule.sourceStageId &&
						r.fieldPath
					) {
						selectedTaskPaths.add(r.fieldPath);
					}
				});

				fields = fields.filter((f) => !selectedTaskPaths.has(f.value));

				// 如果是恢复模式，确保当前规则的 fieldPath 在选项中
				if (restoreMetadata && rule.fieldPath) {
					if (!fields.find((f) => f.value === rule.fieldPath)) {
						const selectedField = allFields.find((f) => f.value === rule.fieldPath);
						if (selectedField) {
							fields.push(selectedField);
						}
					}
				}
			}
		}
	} catch (error) {
		console.error('Failed to load checklist tasks:', error);
	} finally {
		ruleFieldOptions[ruleIndex] = fields;
		loadingFields[ruleIndex] = false;
	}
};

// 处理 Stage 选择变化
const handleStageChange = (rule: RuleFormItem, stageId: string, ruleIndex: number) => {
	// 更新 ruleStageIds
	ruleStageIds[ruleIndex] = stageId;

	// 清空组件相关的选择
	rule.componentType = '';
	rule.componentId = '';
	rule.fieldPath = '';
	rule.operator = '==';
	rule.value = '';
	rule.rowKey = undefined;
	rule.columnKey = undefined;

	// 清空组件 key
	ruleComponentKeys[ruleIndex] = '';

	// 清空字段选项和值选项
	ruleFieldOptions[ruleIndex] = [];
	ruleValueOptions[ruleIndex] = [];

	// 清除问题元数据
	delete questionMetadataMap[ruleIndex];
	delete prevColumnOtherState[ruleIndex];

	// 加载该 stage 的静态字段映射
	loadStaticFieldsMappingForStage(stageId);
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
	const defaultStageId = stage?.id || '';

	const newRule: RuleFormItem = {
		sourceStageId: defaultStageId,
		componentType: '',
		componentId: '',
		fieldPath: '',
		operator: '==',
		value: '',
	};

	const newIndex = props.modelValue.length;
	emit('update:modelValue', [...props.modelValue, newRule]);

	// 设置组件key和stageId，重置校验状态
	ruleComponentKeys[newIndex] = '';
	ruleStageIds[newIndex] = defaultStageId;
	hasValidated[newIndex] = false;
};

// 删除规则
const handleRemoveRule = (index: number) => {
	if (props.modelValue.length <= 1) return;

	// 使用 splice 删除
	const newRules = [...props.modelValue];
	newRules.splice(index, 1);
	emit('update:modelValue', newRules);

	// 同步删除对应索引的状态，并将后续索引前移
	const totalCount = props.modelValue.length; // 删除前的长度
	for (let i = index; i < totalCount - 1; i++) {
		ruleComponentKeys[i] = ruleComponentKeys[i + 1] ?? '';
		ruleStageIds[i] = ruleStageIds[i + 1] ?? '';
		ruleFieldOptions[i] = ruleFieldOptions[i + 1] ?? [];
		loadingFields[i] = loadingFields[i + 1] ?? false;
		ruleValueOptions[i] = ruleValueOptions[i + 1] ?? [];
		hasValidated[i] = hasValidated[i + 1] ?? false;
		questionMetadataMap[i + 1]
			? (questionMetadataMap[i] = questionMetadataMap[i + 1])
			: delete questionMetadataMap[i];
		prevColumnOtherState[i + 1] !== undefined
			? (prevColumnOtherState[i] = prevColumnOtherState[i + 1])
			: delete prevColumnOtherState[i];
	}

	// 删除最后一个索引的状态
	const lastIndex = totalCount - 1;
	delete ruleComponentKeys[lastIndex];
	delete ruleStageIds[lastIndex];
	delete ruleFieldOptions[lastIndex];
	delete loadingFields[lastIndex];
	delete ruleValueOptions[lastIndex];
	delete hasValidated[lastIndex];
	delete questionMetadataMap[lastIndex];
	delete prevColumnOtherState[lastIndex];
};

// 初始化现有规则的字段选项
const initExistingRules = async () => {
	// 先加载所有可能用到的 stage 的静态字段
	const availableStages = getAvailableStages();
	await Promise.all(availableStages.map((stage) => loadStaticFieldsMappingForStage(stage.id)));

	const currentStage = getCurrentStage();
	const defaultStageId = currentStage?.id || '';

	props.modelValue.forEach((rule, index) => {
		// 设置 stageId（如果没有则使用当前 stage）
		const stageId = rule.sourceStageId || defaultStageId;
		ruleStageIds[index] = stageId;

		// 如果规则没有 sourceStageId，更新它
		if (!rule.sourceStageId && stageId) {
			rule.sourceStageId = stageId;
		}

		// 生成并设置组件key，重置校验状态
		const componentKey = generateComponentKey(rule);
		ruleComponentKeys[index] = componentKey;
		hasValidated[index] = false;

		// 加载字段选项（字段类型不需要加载，因为已经在第一级选择了）
		if (rule.componentType === 'questionnaires' && rule.componentId) {
			// 传入 restoreMetadata = true 以恢复元数据和 Grid 选择
			loadQuestionnaireQuestions(rule.componentId, index, true);
		} else if (rule.componentType === 'checklist' && rule.componentId) {
			// 传入 restoreMetadata = true 以确保当前规则的 fieldPath 在选项中
			loadChecklistTasks(rule.componentId, index, true);
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

/**
 * 检查是否存在重复的组件选择
 * 对于 questionnaires 和 checklist 类型，检查的是 fieldPath（问题/任务）级别的重复
 * 对于 fields 类型，检查的是组件级别的重复
 * @returns 重复的组件/问题/任务名称列表
 */
const findDuplicateComponents = (): string[] => {
	const seenFieldKeys = new Map<string, string>(); // stageId_key -> component name (for fields)
	const seenFieldPaths = new Map<string, string>(); // stageId_componentId_fieldPath -> name (for questionnaires/checklist)
	const duplicates: string[] = [];

	props.modelValue.forEach((rule, index) => {
		const key = ruleComponentKeys[index];
		if (!key) return;

		// Questionnaires 和 Checklist 类型：检查 fieldPath（问题/任务）级别的重复
		if (
			(rule.componentType === 'questionnaires' || rule.componentType === 'checklist') &&
			rule.fieldPath
		) {
			const fieldPathKey = `${rule.sourceStageId}_${rule.componentId}_${rule.fieldPath}`;
			if (seenFieldPaths.has(fieldPathKey)) {
				// 找到问题/任务名称
				const fieldName = findFieldPathName(index);
				if (fieldName && !duplicates.includes(fieldName)) {
					duplicates.push(fieldName);
				}
			} else {
				seenFieldPaths.set(fieldPathKey, fieldPathKey);
			}
		} else if (rule.componentType === 'fields') {
			// Fields 类型：检查组件级别的重复
			const componentKey = `${rule.sourceStageId}_${key}`;
			if (seenFieldKeys.has(componentKey)) {
				const componentName = findComponentName(key);
				if (componentName && !duplicates.includes(componentName)) {
					duplicates.push(componentName);
				}
			} else {
				seenFieldKeys.set(componentKey, key);
			}
		}
	});

	return duplicates;
};

/**
 * 根据规则索引查找 fieldPath 对应的名称（问卷问题或 checklist 任务）
 */
const findFieldPathName = (ruleIndex: number): string => {
	const rule = props.modelValue[ruleIndex];
	const fieldOptions = ruleFieldOptions[ruleIndex] || [];
	const field = fieldOptions.find((f) => f.value === rule.fieldPath);
	return field?.label || rule.fieldPath;
};

/**
 * 根据组件 key 查找组件名称
 */
const findComponentName = (key: string): string => {
	// 遍历所有可用的 stage 查找组件名称
	const availableStages = getAvailableStages();
	for (const stage of availableStages) {
		const groups = getComponentOptionGroupsForStage(stage.id);
		for (const group of groups) {
			const item = group.items.find((i) => i.key === key);
			if (item) return item.name;
		}
	}
	return key;
};

// 验证规则完整性
const validate = async (): Promise<{ valid: boolean; message: string }> => {
	if (props.modelValue.length === 0) {
		return { valid: false, message: 'Please add at least one rule' };
	}

	// 标记所有规则为已校验，触发 trigger 变为 'change'
	props.modelValue.forEach((_, index) => {
		hasValidated[index] = true;
	});

	// 等待下一个 tick 让规则更新生效
	await new Promise((resolve) => setTimeout(resolve, 0));

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
