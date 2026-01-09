<template>
	<div>
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
						@input="handleChange"
					/>
				</el-form-item>
				<el-form-item label="Description" prop="description">
					<el-input
						v-model="formData.description"
						type="textarea"
						placeholder="Enter description (optional)"
						:rows="2"
						maxlength="500"
						show-word-limit
						@input="handleChange"
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
					@update:model-value="handleChange"
					@update:logic="handleChange"
				/>
			</div>

			<!-- 动作配置 -->
			<div class="form-section">
				<div class="section-title">Actions</div>
				<ConditionActionForm
					v-model="formData.actions"
					:stages="stages"
					:current-stage-index="currentStageIndex"
					@update:model-value="handleChange"
				/>
			</div>

			<!-- Fallback 配置 -->
			<div class="form-section">
				<div class="section-title">Fallback</div>
				<ConditionFallbackForm
					v-model="formData.fallback"
					:stages="stages"
					:current-stage-index="currentStageIndex"
					@update:model-value="handleChange"
				/>
			</div>
		</el-form>

		<!-- 底部按钮 -->
		<div class="panel-footer">
			<el-button @click="$emit('cancel')">Cancel</el-button>
			<el-button type="primary" :loading="saving" @click="handleSave">
				{{ saving ? 'Saving...' : 'Save' }}
			</el-button>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue';
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
import ConditionRuleForm from '@/views/onboard/workflow/components/condition/ConditionRuleForm.vue';
import ConditionActionForm from '@/views/onboard/workflow/components/condition/ConditionActionForm.vue';
import ConditionFallbackForm from '@/views/onboard/workflow/components/condition/ConditionFallbackForm.vue';

// Props
const props = defineProps<{
	condition: StageCondition | null;
	stages: Stage[];
	currentStageIndex: number;
	saving?: boolean;
}>();

// Emits
const emit = defineEmits<{
	(e: 'save', input: StageConditionInput): void;
	(e: 'cancel'): void;
	(e: 'change'): void;
}>();

// Refs
const formRef = ref<FormInstance>();

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

// 可选的 Source Stage（当前及之前的 Stage）
const availableSourceStages = computed(() => {
	return props.stages.slice(0, props.currentStageIndex + 1);
});

// 获取当前 Stage ID
const getCurrentStageId = (): string => {
	return props.stages[props.currentStageIndex]?.id || '';
};

// 初始化表单数据
const initFormData = () => {
	if (props.condition) {
		formData.name = props.condition.name;
		formData.description = props.condition.description || '';

		// 解析 rulesJson（格式: {logic, rules}）
		let parsedRules: RuleFormItem[] = [];
		let logic: 'AND' | 'OR' = 'AND';

		try {
			const rulesJsonData =
				typeof props.condition.rulesJson === 'string'
					? JSON.parse(props.condition.rulesJson)
					: props.condition.rulesJson;

			if (rulesJsonData && typeof rulesJsonData === 'object') {
				logic = rulesJsonData.logic || 'AND';
				parsedRules = (rulesJsonData.rules || []).map((rule: any) => ({ ...rule }));
			}
		} catch (e) {
			console.error('Failed to parse rulesJson:', e);
		}

		formData.logic = logic;
		formData.rules = parsedRules;

		let actionsData = props.condition.actionsJson;
		if (typeof actionsData === 'string') {
			try {
				actionsData = JSON.parse(actionsData);
			} catch (e) {
				actionsData = [];
			}
		}
		formData.actions = (actionsData || []).map((action: any) => ({ ...action }));

		formData.fallback = {
			type: props.condition.fallbackStageId ? 'specified' : 'default',
			fallbackStageId: props.condition.fallbackStageId,
		};

		// 添加默认规则
		if (formData.rules.length === 0) {
			formData.rules = [
				{
					sourceStageId: getCurrentStageId(),
					componentType: 'questionnaires',
					fieldPath: '',
					operator: '==',
					value: '',
				},
			];
		}

		// 添加默认动作
		if (formData.actions.length === 0) {
			formData.actions = [{ type: 'GoToStage', targetStageId: '', order: 0 }];
		}
	} else {
		resetFormData();
	}
};

// 重置表单数据
const resetFormData = () => {
	formData.name = '';
	formData.description = '';
	formData.logic = 'AND';
	formData.rules = [
		{
			sourceStageId: getCurrentStageId(),
			componentType: 'questionnaires',
			fieldPath: '',
			operator: '==',
			value: '',
		},
	];
	formData.actions = [{ type: 'GoToStage', targetStageId: '', order: 0 }];
	formData.fallback = { type: 'default', fallbackStageId: undefined };
};

// 构建提交数据
const buildSubmitData = (): StageConditionInput => {
	const rulesJson = {
		logic: formData.logic,
		rules: formData.rules,
	};

	const actionsJson = formData.actions.map((action, index) => ({
		...action,
		order: index,
	}));

	return {
		name: formData.name,
		description: formData.description || undefined,
		rulesJson,
		actionsJson,
		fallbackStageId:
			formData.fallback.type === 'specified' ? formData.fallback.fallbackStageId : undefined,
		isActive: true,
	};
};

// 处理保存
const handleSave = async () => {
	if (!formRef.value) return;

	try {
		await formRef.value.validate();

		if (formData.rules.length === 0) {
			ElMessage.error('Please add at least one rule');
			return;
		}

		if (formData.actions.length === 0) {
			ElMessage.error('Please add at least one action');
			return;
		}

		const submitData = buildSubmitData();
		emit('save', submitData);
	} catch (error) {
		// 验证失败
	}
};

// 处理数据变更
const handleChange = () => {
	emit('change');
};

// 监听 condition 变化
watch(
	() => props.condition,
	() => {
		initFormData();
	},
	{ immediate: true }
);
</script>

<style lang="scss" scoped>
.form-section {
	margin-bottom: 16px;
	padding-bottom: 16px;
	border-bottom: 1px solid var(--el-border-color-lighter);

	&:last-of-type {
		margin-bottom: 0;
		padding-bottom: 0;
		border-bottom: none;
	}
}

.section-title {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	margin-bottom: 12px;
}

.panel-footer {
	display: flex;
	justify-content: flex-end;
	gap: 12px;
	padding-top: 16px;
	margin-top: auto;
	border-top: 1px solid var(--el-border-color-lighter);
}
</style>
