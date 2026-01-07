<template>
	<el-drawer
		:model-value="visible"
		:title="isEditMode ? 'Edit Condition' : 'Add Condition'"
		direction="rtl"
		size="500px"
		:close-on-click-modal="false"
		:close-on-press-escape="false"
		@update:model-value="handleVisibleChange"
		class="stage-condition-editor"
	>
		<template #header>
			<div class="drawer-header">
				<span class="drawer-title">
					{{ isEditMode ? 'Edit Condition' : 'Add Condition' }}
				</span>
				<span class="drawer-subtitle">{{ stageName }}</span>
			</div>
		</template>

		<el-form
			ref="formRef"
			:model="formData"
			:rules="formRules"
			label-position="top"
			@submit.prevent
		>
			<!-- 基本信息 -->
			<div class="form-section">
				<div class="section-title">Basic Information</div>
				<el-form-item label="Condition Name" prop="name">
					<el-input
						v-model="formData.name"
						placeholder="Enter condition name"
						maxlength="100"
						show-word-limit
					/>
				</el-form-item>
				<el-form-item label="Description" prop="description">
					<el-input
						v-model="formData.description"
						type="textarea"
						placeholder="Enter description (optional)"
						:rows="3"
						maxlength="500"
						show-word-limit
					/>
				</el-form-item>
			</div>

			<!-- 条件规则 -->
			<div class="form-section">
				<div class="section-title">Condition Rules</div>
				<ConditionRuleForm
					v-model="formData.rules"
					v-model:logic="formData.logic"
					:stages="availableSourceStages"
					:current-stage-index="currentStageIndex"
				/>
			</div>

			<!-- 动作配置 -->
			<div class="form-section">
				<div class="section-title">Actions</div>
				<ConditionActionForm
					v-model="formData.actions"
					:stages="stages"
					:current-stage-index="currentStageIndex"
				/>
			</div>

			<!-- Fallback 配置 -->
			<div class="form-section">
				<div class="section-title">Fallback</div>
				<ConditionFallbackForm
					v-model="formData.fallback"
					:stages="stages"
					:current-stage-index="currentStageIndex"
				/>
			</div>
		</el-form>

		<template #footer>
			<div class="editor-footer">
				<el-button @click="handleCancel">Cancel</el-button>
				<el-button type="primary" :loading="saving" @click="handleSave">
					{{ saving ? 'Saving...' : 'Save' }}
				</el-button>
			</div>
		</template>
	</el-drawer>
</template>

<script setup lang="ts">
import { ref, computed, watch, reactive } from 'vue';
import { ElMessage } from 'element-plus';
import type { FormInstance, FormRules } from 'element-plus';
import type { Stage } from '#/onboard';
import type {
	StageCondition,
	StageConditionInput,
	RuleFormItem,
	ActionFormItem,
	FallbackConfig,
} from '#/condition';
import ConditionRuleForm from './ConditionRuleForm.vue';
import ConditionActionForm from './ConditionActionForm.vue';
import ConditionFallbackForm from './ConditionFallbackForm.vue';
import { createCondition, updateCondition } from '@/apis/ow';

// Props
const props = defineProps<{
	visible: boolean;
	stageId: string;
	stageName: string;
	condition?: StageCondition | null;
	stages: Stage[];
	currentStageIndex: number;
}>();

// Emits
const emit = defineEmits<{
	(e: 'update:visible', value: boolean): void;
	(e: 'save', condition: StageCondition): void;
	(e: 'cancel'): void;
}>();

// Refs
const formRef = ref<FormInstance>();
const saving = ref(false);

// 表单数据
const formData = reactive({
	name: '',
	description: '',
	logic: 'AND' as 'AND' | 'OR',
	rules: [] as RuleFormItem[],
	actions: [] as ActionFormItem[],
	fallback: {
		type: 'default',
		fallbackStageId: undefined,
	} as FallbackConfig,
});

// 表单验证规则
const formRules: FormRules = {
	name: [
		{ required: true, message: 'Please enter condition name', trigger: 'blur' },
		{ max: 100, message: 'Name must be less than 100 characters', trigger: 'blur' },
	],
	description: [
		{ max: 500, message: 'Description must be less than 500 characters', trigger: 'blur' },
	],
};

// Computed
const isEditMode = computed(() => !!props.condition);

// 可选的 Source Stage（当前及之前的 Stage）
const availableSourceStages = computed(() => {
	return props.stages.slice(0, props.currentStageIndex + 1);
});

// 生成唯一 ID
const generateId = () => `temp_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;

// 初始化表单数据
const initFormData = () => {
	if (props.condition) {
		// 编辑模式：加载现有数据
		formData.name = props.condition.name;
		formData.description = props.condition.description || '';
		formData.logic = props.condition.rulesJson.logic;
		formData.rules = props.condition.rulesJson.rules.map((rule) => ({
			...rule,
			_id: generateId(),
		}));
		formData.actions = props.condition.actionsJson.map((action) => ({
			...action,
			_id: generateId(),
		}));
		formData.fallback = {
			type: props.condition.fallbackStageId ? 'specified' : 'default',
			fallbackStageId: props.condition.fallbackStageId,
		};
	} else {
		// 新建模式：初始化默认值
		formData.name = '';
		formData.description = '';
		formData.logic = 'AND';
		formData.rules = [
			{
				_id: generateId(),
				sourceStageId: props.stageId,
				componentType: 'checklist',
				componentId: '',
				fieldPath: '',
				operator: 'Equals',
				value: '',
			},
		];
		formData.actions = [
			{
				_id: generateId(),
				type: 'GoToStage',
				targetStageId: '',
				order: 0,
			},
		];
		formData.fallback = {
			type: 'default',
			fallbackStageId: undefined,
		};
	}
};

// 监听 visible 变化，初始化表单
watch(
	() => props.visible,
	(newVal) => {
		if (newVal) {
			initFormData();
		}
	},
	{ immediate: true }
);

// 处理可见性变化
const handleVisibleChange = (val: boolean) => {
	emit('update:visible', val);
};

// 处理取消
const handleCancel = () => {
	emit('cancel');
	emit('update:visible', false);
};

// 构建提交数据
const buildSubmitData = (): StageConditionInput => {
	return {
		name: formData.name,
		description: formData.description || undefined,
		rulesJson: {
			logic: formData.logic,
			rules: formData.rules.map(({ _id, _componentOptions, _fieldOptions, ...rule }) => rule),
		},
		actionsJson: formData.actions.map(({ _id, ...action }, index) => ({
			...action,
			order: index,
		})),
		fallbackStageId:
			formData.fallback.type === 'specified' ? formData.fallback.fallbackStageId : undefined,
		isActive: true,
	};
};

// 处理保存
const handleSave = async () => {
	if (!formRef.value) return;

	try {
		// 表单验证
		await formRef.value.validate();

		// 验证规则
		if (formData.rules.length === 0) {
			ElMessage.error('Please add at least one rule');
			return;
		}

		// 验证动作
		if (formData.actions.length === 0) {
			ElMessage.error('Please add at least one action');
			return;
		}

		saving.value = true;

		const submitData = buildSubmitData();
		let result: StageCondition;

		if (isEditMode.value && props.condition) {
			// 更新
			result = await updateCondition(props.stageId, props.condition.id, submitData);
			ElMessage.success('Condition updated successfully');
		} else {
			// 创建
			result = await createCondition(props.stageId, submitData);
			ElMessage.success('Condition created successfully');
		}

		emit('save', result);
		emit('update:visible', false);
	} catch (error: any) {
		if (error !== false) {
			// 非表单验证错误
			ElMessage.error(error?.message || 'Failed to save condition');
		}
	} finally {
		saving.value = false;
	}
};
</script>

<style lang="scss">
.stage-condition-editor {
	.el-drawer__header {
		margin-bottom: 0px !important;
	}
}
</style>

<style lang="scss" scoped>
.drawer-header {
	display: flex;
	flex-direction: column;
	gap: 4px;
}

.drawer-title {
	font-size: 16px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.drawer-subtitle {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.form-section {
	@apply mb-2 pb-2;
	border-bottom: 1px solid var(--el-border-color-lighter);

	&:last-child {
		margin-bottom: 0;
		padding-bottom: 0;
		border-bottom: none;
	}
}

.section-title {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	margin-bottom: 16px;
}

.editor-footer {
	display: flex;
	justify-content: flex-end;
	gap: 12px;
	padding: 16px 20px;
}
</style>
