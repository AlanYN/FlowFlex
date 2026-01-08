<template>
	<div class="condition-rule-form">
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
				<el-form-item label="Select Component">
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

				<!-- 第二级选择：问题/任务/字段 -->
				<el-form-item :label="getFieldLabel(rule.componentType)">
					<el-select
						v-model="rule.fieldPath"
						placeholder="Select field"
						:loading="loadingFields[index]"
					>
						<el-option
							v-for="field in ruleFieldOptions[index] || []"
							:key="field.value"
							:label="field.label"
							:value="field.value"
						/>
					</el-select>
				</el-form-item>

				<!-- Operator -->
				<el-form-item label="Operator">
					<el-select v-model="rule.operator" placeholder="Select operator">
						<el-option
							v-for="op in operators"
							:key="op.value"
							:label="op.label"
							:value="op.value"
						/>
					</el-select>
				</el-form-item>

				<!-- Value -->
				<el-form-item label="Value">
					<el-input v-model="rule.value" placeholder="Enter value" />
				</el-form-item>
			</div>
		</div>

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
import type { RuleFormItem } from '#/condition';
import type { Stage } from '#/onboard';
import type { DynamicList } from '#/dynamic';
import { getQuestionnaireDetail } from '@/apis/ow/questionnaire';
import { getChecklistDetail } from '@/apis/ow/checklist';
import { batchIdsDynamicFields } from '@/apis/global/dyanmicField';

// 字段选项接口
interface FieldOption {
	label: string;
	value: string; // expressionPath
}

// 组件选项接口
interface ComponentOption {
	key: string; // 唯一标识: questionnaire_{id}, checklist_{id}, fields
	type: 'questionnaires' | 'checklist' | 'fields';
	id?: string; // 问卷或checklist的ID
	name: string; // 显示名称
}

// 组件选项分组
interface ComponentOptionGroup {
	type: string;
	label: string;
	items: ComponentOption[];
}

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

// 操作符选项
const operators = [
	{ value: '==', label: '=' },
	{ value: '!=', label: '≠' },
	{ value: '>', label: '>' },
	{ value: '<', label: '<' },
	{ value: '>=', label: '>=' },
	{ value: '<=', label: '<=' },
	{ value: 'Contains', label: 'Contains' },
	{ value: 'NotContains', label: 'Does Not Contain' },
	{ value: 'StartsWith', label: 'Starts With' },
	{ value: 'EndsWith', label: 'Ends With' },
	{ value: 'IsEmpty', label: 'Is Empty' },
	{ value: 'IsNotEmpty', label: 'Is Not Empty' },
	{ value: 'InList', label: 'In List' },
	{ value: 'NotInList', label: 'Not In List' },
];

// 每个规则的字段选项（按规则索引）
const ruleFieldOptions = reactive<Record<number, FieldOption[]>>({});

// 每个规则选择的组件key
const ruleComponentKeys = reactive<Record<number, string>>({});

// 加载状态（按规则索引）
const loadingFields = reactive<Record<number, boolean>>({});

// 静态字段映射缓存
const staticFieldsMap = ref<Map<string, DynamicList>>(new Map());

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
	let hasFields = false;

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
			hasFields = true;
		}
	});

	// 添加问卷组
	if (questionnaireItems.length > 0) {
		groups.push({ type: 'questionnaires', label: 'Questionnaires', items: questionnaireItems });
	}

	// 添加 Checklist 组
	if (checklistItems.length > 0) {
		groups.push({ type: 'checklist', label: 'Checklists', items: checklistItems });
	}

	// 添加静态字段组
	if (hasFields) {
		groups.push({
			type: 'fields',
			label: 'Required Fields',
			items: [{ key: 'fields', type: 'fields' as const, name: 'Required Field' }],
		});
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

	try {
		const res: any = await batchIdsDynamicFields({ ids: uncachedIds });
		if (res.code === '200' && res.data) {
			res.data.forEach((field: DynamicList) => {
				staticFieldsMap.value.set(field.id, field);
			});
		}
	} catch (error) {
		console.error('Failed to load static fields mapping:', error);
	}
};

// 加载问卷问题列表
const loadQuestionnaireQuestions = async (questionnaireId: string, ruleIndex: number) => {
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

			questions.forEach((q: any) => {
				fields.push({
					label: q.title || q.questionText || `Question ${q.order || ''}`,
					value: `input.questionnaire.answers["${questionnaireId}"]["${
						q.id || q.questionId
					}"]`,
				});
			});
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

// 加载静态字段选项
const loadStaticFieldOptions = (ruleIndex: number) => {
	const stage = getCurrentStage();
	if (!stage?.components) {
		ruleFieldOptions[ruleIndex] = [];
		return;
	}

	const fieldsComponent = stage.components.find((c) => c.key === 'fields');
	if (!fieldsComponent?.staticFields?.length) {
		ruleFieldOptions[ruleIndex] = [];
		return;
	}

	ruleFieldOptions[ruleIndex] = fieldsComponent.staticFields.map((field) => {
		const fieldInfo = staticFieldsMap.value.get(field.id);
		return {
			label: fieldInfo?.displayName || field.id,
			value: `input.fields.${field.id}`,
		};
	});
};

// 处理组件选择变化
const handleComponentChange = (rule: RuleFormItem, componentKey: string, ruleIndex: number) => {
	const { type, id } = parseComponentKey(componentKey);

	rule.componentType = type as any;
	rule.componentId = id;
	rule.fieldPath = '';
	ruleFieldOptions[ruleIndex] = [];

	// 根据类型加载对应的字段选项
	if (type === 'fields') {
		loadStaticFieldOptions(ruleIndex);
	} else if (type === 'questionnaires' && id) {
		loadQuestionnaireQuestions(id, ruleIndex);
	} else if (type === 'checklist' && id) {
		loadChecklistTasks(id, ruleIndex);
	}
};

// 添加规则
const handleAddRule = () => {
	const stage = getCurrentStage();

	// 获取第一个可用的组件选项
	let defaultKey = 'fields';
	let defaultType: 'questionnaires' | 'checklist' | 'fields' = 'fields';
	let defaultId: string | undefined;

	if (componentOptionGroups.value.length > 0) {
		const firstGroup = componentOptionGroups.value[0];
		if (firstGroup.items.length > 0) {
			const firstItem = firstGroup.items[0];
			defaultKey = firstItem.key;
			defaultType = firstItem.type;
			defaultId = firstItem.id;
		}
	}

	const newRule: RuleFormItem = {
		sourceStageId: stage?.id || '',
		componentType: defaultType,
		componentId: defaultId,
		fieldPath: '',
		operator: '==',
		value: '',
	};

	const newIndex = props.modelValue.length;
	emit('update:modelValue', [...props.modelValue, newRule]);

	// 设置组件key
	ruleComponentKeys[newIndex] = defaultKey;

	// 加载字段选项
	if (defaultType === 'fields') {
		loadStaticFieldOptions(newIndex);
	} else if (defaultType === 'questionnaires' && defaultId) {
		loadQuestionnaireQuestions(defaultId, newIndex);
	} else if (defaultType === 'checklist' && defaultId) {
		loadChecklistTasks(defaultId, newIndex);
	}
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
};

// 初始化现有规则的字段选项
const initExistingRules = async () => {
	await loadStaticFieldsMapping();

	props.modelValue.forEach((rule, index) => {
		// 生成并设置组件key
		const componentKey = generateComponentKey(rule);
		ruleComponentKeys[index] = componentKey;

		// 加载字段选项
		if (rule.componentType === 'fields') {
			loadStaticFieldOptions(index);
		} else if (rule.componentType === 'questionnaires' && rule.componentId) {
			loadQuestionnaireQuestions(rule.componentId, index);
		} else if (rule.componentType === 'checklist' && rule.componentId) {
			loadChecklistTasks(rule.componentId, index);
		}
	});
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
