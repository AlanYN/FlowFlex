<template>
	<div class="px-5 py-[18px] border-b border-[var(--el-border-color-lighter)]">
		<div
			class="flex items-center gap-1.5 text-sm font-semibold text-[var(--el-text-color-primary)] mb-3.5"
		>
			<el-icon class="text-[15px] text-[var(--el-color-primary)]"><Lightning /></el-icon>
			Trigger Condition
		</div>

		<el-form label-position="top" class="condition-form">
			<!-- 条件规则列表 -->
			<div class="flex flex-col gap-3 mb-3">
				<div
					v-for="(cond, index) in conditions"
					:key="cond.id"
					class="p-4 rounded-lg border border-[var(--el-border-color)] bg-[var(--el-fill-color-lighter)] flex flex-col gap-3 transition-colors hover:border-[var(--el-color-primary-light-3)]"
				>
					<!-- Rule 头部 -->
					<div class="flex items-center justify-between">
						<span class="text-xs font-semibold text-[var(--el-color-primary)]">
							Rule {{ index + 1 }}
						</span>
						<el-button
							type="danger"
							link
							:icon="Delete"
							@click="emit('remove-condition', index)"
						/>
					</div>

					<!-- 1. Select Stage -->
					<el-form-item label="Select Stage">
						<el-select
							v-model="cond.stageId"
							placeholder="Select stage"
							:loading="loading"
							:disabled="loading"
							@change="(v: string) => handleStageChange(cond, index, v)"
						>
							<el-option
								v-for="s in stageOptions"
								:key="s.id"
								:label="s.name"
								:value="s.id"
							/>
						</el-select>
					</el-form-item>

					<!-- 2. Select Component -->
					<el-form-item label="Select Component">
						<el-select
							v-model="cond.componentKey"
							placeholder="Select component"
							:disabled="loading || !cond.stageId"
							@change="(v: string) => handleComponentChange(cond, index, v)"
						>
							<el-option-group
								v-for="group in getComponentGroups(cond)"
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

					<!-- 3. Field（仅问卷/Checklist 类型） -->
					<el-form-item
						v-if="cond.componentType && cond.componentType !== 'fields'"
						:label="
							cond.componentType === 'checklist' ? 'Select Task' : 'Select Question'
						"
					>
						<el-select
							v-model="cond.resourceId"
							placeholder="Select field"
							:disabled="!cond.componentKey"
							filterable
							@change="
								(v: string) => {
									cond.resourceName = getFieldOptions(cond).find(
										(f) => f.value === v
									)?.label;
									emit('dirty');
								}
							"
						>
							<el-option
								v-for="f in getFieldOptions(cond)"
								:key="f.value"
								:label="f.label"
								:value="f.value"
							/>
						</el-select>
					</el-form-item>

					<!-- 4. Operator + Value -->
					<template
						v-if="
							cond.componentType &&
							(cond.componentType === 'fields' ? cond.componentKey : cond.resourceId)
						"
					>
						<!-- Checklist: only operator, no value needed -->
						<el-form-item
							v-if="cond.componentType === 'checklist'"
							label="Trigger When"
						>
							<el-select v-model="cond.operator" @change="emit('dirty')">
								<el-option label="Task Completed" value="CompleteTask" />
								<el-option label="All Tasks Completed" value="AllCompleted" />
							</el-select>
						</el-form-item>

						<!-- Fields / Questionnaires: operator + typed value input -->
						<template v-else>
							<div class="flex gap-2.5 items-end">
								<el-form-item label="Operator" class="basis-[180px] shrink-0">
									<el-select
										v-model="cond.operator"
										@change="
											() => {
												cond.value = undefined;
												emit('dirty');
											}
										"
									>
										<el-option
											v-for="op in getOperatorOptions(cond)"
											:key="op.value"
											:label="op.label"
											:value="op.value"
										/>
									</el-select>
								</el-form-item>
								<el-form-item label="Value" class="flex-1">
									<!-- Number field -->
									<el-input-number
										v-if="getFieldCategory(cond) === 'number'"
										:model-value="
											cond.value !== undefined
												? Number(cond.value)
												: undefined
										"
										:placeholder="'Enter number'"
										controls-position="right"
										class="w-full"
										@change="
											(v: number | undefined) => {
												cond.value =
													v !== undefined ? String(v) : undefined;
												emit('dirty');
											}
										"
									/>
									<!-- Date field -->
									<el-date-picker
										v-else-if="getFieldCategory(cond) === 'date'"
										:model-value="cond.value"
										type="date"
										placeholder="Select date"
										value-format="YYYY-MM-DD"
										class="w-full"
										@change="
											(v: string) => {
												cond.value = v;
												emit('dirty');
											}
										"
									/>
									<!-- Options field (radio/checkbox/select questionnaire question) -->
									<el-select
										v-else-if="getFieldCategory(cond) === 'options'"
										:model-value="
											isMultiSelectQuestion(cond)
												? cond.value
													? cond.value.split(',')
													: []
												: cond.value
										"
										:placeholder="
											isMultiSelectQuestion(cond)
												? 'Select one or more options'
												: 'Select option'
										"
										:multiple="isMultiSelectQuestion(cond)"
										:collapse-tags="isMultiSelectQuestion(cond)"
										:collapse-tags-tooltip="isMultiSelectQuestion(cond)"
										tag-type="primary"
										clearable
										@change="
											(v: string | string[]) => {
												cond.value = Array.isArray(v) ? v.join(',') : v;
												emit('dirty');
											}
										"
									>
										<el-option
											v-for="opt in getSelectedQuestion(cond)?.options ?? []"
											:key="opt.value"
											:label="opt.label"
											:value="opt.value"
										/>
									</el-select>
									<!-- Text / default field -->
									<el-input
										v-else
										v-model="cond.value"
										placeholder="Enter value"
										@input="emit('dirty')"
									/>
								</el-form-item>
							</div>
						</template>
					</template>
				</div>
			</div>

			<!-- Add condition rule -->
			<el-button class="conn-add-btn" @click="emit('add-condition')">
				<el-icon><Plus /></el-icon>
				Add condition rule
			</el-button>
		</el-form>
	</div>
</template>

<script setup lang="ts">
import { Delete, Plus, Connection as Lightning } from '@element-plus/icons-vue';

// ── Types (reuse from parent) ─────────────────────────────
interface StageFieldOption {
	id: string;
	name: string;
	fieldType: string;
}
interface StageTaskOption {
	id: string;
	name: string;
	taskType?: string;
}
interface StageQuestionOption {
	id: string;
	name: string;
	type?: string;
	options?: { label: string; value: string }[]; // label=display text, value=stored answer value
}
interface StageQuestionnaire {
	id: string;
	name: string;
	questions: StageQuestionOption[];
}
interface StageChecklist {
	id: string;
	name: string;
	tasks: StageTaskOption[];
}
interface StageOption {
	id: string;
	name: string;
	fields: StageFieldOption[];
	questionnaires: StageQuestionnaire[];
	checklists: StageChecklist[];
}

interface ComponentOptionItem {
	key: string;
	name: string;
	type: string;
	id: string;
}
interface ComponentOptionGroup {
	type: string;
	label: string;
	items: ComponentOptionItem[];
}

interface ConditionRow {
	id: string;
	logic: 'AND' | 'OR';
	stageId?: string;
	stageName?: string;
	componentKey?: string;
	componentType?: string;
	componentId?: string;
	componentName?: string;
	resourceId?: string;
	resourceName?: string;
	operator?: string;
	value?: string;
}

// ── Props / Emits ─────────────────────────────────────────
const props = defineProps<{
	conditions: ConditionRow[];
	stageOptions: StageOption[];
	loading?: boolean;
}>();

const emit = defineEmits<{
	'add-condition': [];
	'remove-condition': [index: number];
	dirty: [];
}>();

// ── Constants ─────────────────────────────────────────────
const OPERATORS_COMPARE = [
	{ label: '== (equals)', value: '==' },
	{ label: '!= (not equals)', value: '!=' },
	{ label: '> (greater than)', value: '>' },
	{ label: '>= (greater or equal)', value: '>=' },
	{ label: '< (less than)', value: '<' },
	{ label: '<= (less or equal)', value: '<=' },
];
const OPERATORS_TEXT = [
	{ label: '== (equals)', value: '==' },
	{ label: '!= (not equals)', value: '!=' },
	{ label: 'contains', value: 'contains' },
];
const OPERATORS_DATE = [
	{ label: '== (equals)', value: '==' },
	{ label: '!= (not equals)', value: '!=' },
	{ label: '> (after)', value: '>' },
	{ label: '>= (on or after)', value: '>=' },
	{ label: '< (before)', value: '<' },
	{ label: '<= (on or before)', value: '<=' },
];
const OPERATORS_EQUALITY = [
	{ label: '== (equals)', value: '==' },
	{ label: '!= (not equals)', value: '!=' },
];

// Numeric fieldType values from propertyTypeEnum
const NUMBER_FIELD_TYPES = new Set(['13', 'Number', 13]);
const DATE_FIELD_TYPES = new Set(['10', 'DatePicker', 10]);

/**
 * For questionnaire questions: returns the question object (with type + options).
 */
const getSelectedQuestion = (cond: ConditionRow) => {
	if (cond.componentType !== 'questionnaires' || !cond.resourceId) return null;
	const stage = props.stageOptions.find((s) => s.id === cond.stageId);
	if (!stage) return null;
	const qId = cond.componentKey?.replace('questionnaire_', '');
	const q = stage.questionnaires?.find((x) => x.id === qId);
	return q?.questions?.find((qq) => qq.id === cond.resourceId) ?? null;
};

/**
 * Returns the logical category of a condition's field/question:
 * 'number' | 'date' | 'equality' | 'options' | 'text' (default)
 */
const getFieldCategory = (
	cond: ConditionRow
): 'number' | 'date' | 'equality' | 'options' | 'text' => {
	if (cond.componentType === 'questionnaires') {
		const question = getSelectedQuestion(cond);
		if (!question) return 'text';
		const t = (question.type ?? '').toLowerCase();
		if (t === 'number' || t === 'integer') return 'number';
		if (t === 'date' || t === 'date_picker' || t === 'datepicker') return 'date';
		if (
			(t === 'radio' ||
				t === 'checkbox' ||
				t === 'checkboxes' ||
				t === 'select' ||
				t === 'single_choice' ||
				t === 'multiple_choice') &&
			question.options &&
			question.options.length > 0
		)
			return 'options';
		return 'text';
	}
	if (cond.componentType !== 'fields') return 'text';
	const stage = props.stageOptions.find((s) => s.id === cond.stageId);
	if (!stage) return 'text';
	const fId = cond.componentKey?.replace('field_', '');
	const fieldType = stage.fields?.find((f) => f.id === fId)?.fieldType;
	if (NUMBER_FIELD_TYPES.has(fieldType as any)) return 'number';
	if (DATE_FIELD_TYPES.has(fieldType as any)) return 'date';
	if (fieldType === '5' || fieldType === 'DropdownSelect' || fieldType === 5) return 'equality';
	if (fieldType === '7' || fieldType === 'Switch' || fieldType === 7) return 'equality';
	return 'text';
};

const getOperatorOptions = (cond: ConditionRow) => {
	const cat = getFieldCategory(cond);
	if (cat === 'number') return OPERATORS_COMPARE;
	if (cat === 'date') return OPERATORS_DATE;
	if (cat === 'options') {
		// Multi-select (checkboxes) supports contains/not-contains + equality
		if (isMultiSelectQuestion(cond)) {
			return [
				{ label: 'contains', value: 'contains' },
				{ label: '== (equals all)', value: '==' },
				{ label: '!= (not equals)', value: '!=' },
			];
		}
		return OPERATORS_EQUALITY;
	}
	if (cat === 'equality') return OPERATORS_EQUALITY;
	return OPERATORS_TEXT;
};

/** Returns true if the selected question is a multi-select type (checkboxes / multiple_choice) */
const isMultiSelectQuestion = (cond: ConditionRow): boolean => {
	const question = getSelectedQuestion(cond);
	if (!question) return false;
	const t = (question.type ?? '').toLowerCase();
	return t === 'checkboxes' || t === 'multiple_choice';
};

// ── Helpers ───────────────────────────────────────────────
const getComponentGroups = (cond: ConditionRow): ComponentOptionGroup[] => {
	const stage = props.stageOptions.find((s) => s.id === cond.stageId);
	if (!stage) return [];
	const groups: ComponentOptionGroup[] = [];
	if (stage.fields?.length)
		groups.push({
			type: 'fields',
			label: 'Required Fields',
			items: stage.fields.map((f) => ({
				key: `field_${f.id}`,
				name: f.name,
				type: 'fields',
				id: f.id,
			})),
		});
	if (stage.checklists?.length)
		groups.push({
			type: 'checklist',
			label: 'Checklists',
			items: stage.checklists.map((c) => ({
				key: `checklist_${c.id}`,
				name: c.name,
				type: 'checklist',
				id: c.id,
			})),
		});
	if (stage.questionnaires?.length)
		groups.push({
			type: 'questionnaires',
			label: 'Questionnaires',
			items: stage.questionnaires.map((q) => ({
				key: `questionnaire_${q.id}`,
				name: q.name,
				type: 'questionnaires',
				id: q.id,
			})),
		});
	return groups;
};

const getFieldOptions = (cond: ConditionRow): { value: string; label: string }[] => {
	const stage = props.stageOptions.find((s) => s.id === cond.stageId);
	if (!stage || !cond.componentKey) return [];
	if (cond.componentKey.startsWith('questionnaire_')) {
		const q = stage.questionnaires?.find(
			(x) => x.id === cond.componentKey!.replace('questionnaire_', '')
		);
		return (q?.questions ?? []).map((item) => ({ value: item.id, label: item.name }));
	}
	if (cond.componentKey.startsWith('checklist_')) {
		const cl = stage.checklists?.find(
			(x) => x.id === cond.componentKey!.replace('checklist_', '')
		);
		return (cl?.tasks ?? []).map((item) => ({ value: item.id, label: item.name }));
	}
	return [];
};

const handleStageChange = (cond: ConditionRow, _i: number, stageId: string) => {
	const stage = props.stageOptions.find((s) => s.id === stageId);
	cond.stageName = stage?.name;
	cond.componentKey = undefined;
	cond.componentType = undefined;
	cond.componentId = undefined;
	cond.componentName = undefined;
	cond.resourceId = undefined;
	cond.resourceName = undefined;
	cond.operator = '';
	cond.value = undefined;
	emit('dirty');
};

const handleComponentChange = (cond: ConditionRow, _i: number, key: string) => {
	const stage = props.stageOptions.find((s) => s.id === cond.stageId);
	if (!stage) return;
	cond.componentKey = key;
	cond.resourceId = undefined;
	cond.resourceName = undefined;
	cond.operator = '';
	cond.value = undefined;
	if (key.startsWith('field_')) {
		const fId = key.replace('field_', '');
		cond.componentType = 'fields';
		cond.componentId = fId;
		cond.componentName = stage.fields?.find((f) => f.id === fId)?.name;
	} else if (key.startsWith('checklist_')) {
		const cId = key.replace('checklist_', '');
		cond.componentType = 'checklist';
		cond.componentId = cId;
		cond.componentName = stage.checklists?.find((c) => c.id === cId)?.name;
	} else if (key.startsWith('questionnaire_')) {
		const qId = key.replace('questionnaire_', '');
		cond.componentType = 'questionnaires';
		cond.componentId = qId;
		cond.componentName = stage.questionnaires?.find((q) => q.id === qId)?.name;
	}
	emit('dirty');
};
</script>

<style scoped lang="scss">
/* el-form-item 样式覆盖（无法用 tailwind 替代的 :deep） */
.condition-form {
	:deep(.el-form-item) {
		margin-bottom: 0;
	}
	:deep(.el-form-item__label) {
		font-size: 13px;
		font-weight: 500;
		color: var(--el-text-color-regular);
		padding-bottom: 4px;
		line-height: 1.4;
	}
	:deep(.el-select),
	:deep(.el-input) {
		width: 100%;
	}
}
</style>
