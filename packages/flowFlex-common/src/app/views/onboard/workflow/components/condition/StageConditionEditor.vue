<template>
	<el-drawer
		v-model="visible"
		:title="isEditMode ? 'Edit Condition' : 'Add Condition'"
		direction="rtl"
		size="500px"
		:close-on-click-modal="false"
		:close-on-press-escape="false"
		class="stage-condition-editor"
		append-to-body
	>
		<template #header>
			<div class="drawer-header">
				<span class="drawer-title">
					{{ isEditMode ? 'Edit Condition' : 'Add Condition' }}
				</span>
				<span class="drawer-subtitle">{{ currentStageName }}</span>
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
import { ref, computed, reactive } from 'vue';
import { ElMessage } from 'element-plus';
import type { FormInstance, FormRules } from 'element-plus';
import type { Stage } from '#/onboard';
import type { StageCondition, RuleFormItem, ActionFormItem, FallbackConfig } from '#/condition';
import ConditionRuleForm from './ConditionRuleForm.vue';
import ConditionActionForm from './ConditionActionForm.vue';
import ConditionFallbackForm from './ConditionFallbackForm.vue';
import { createCondition, updateCondition } from '@/apis/ow';

// Props
const props = defineProps<{
	stages: Stage[];
	workflowId: string;
}>();

// Emits
const emit = defineEmits<{
	(e: 'save', condition: StageCondition): void;
	(e: 'cancel'): void;
}>();

// 内部状态
const visible = ref(false);
const currentStageId = ref('');
const currentStageName = ref('');
const currentStageIndex = ref(0);
const currentCondition = ref<StageCondition | null>(null);

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
const isEditMode = computed(() => !!currentCondition.value);

// 可选的 Source Stage（当前及之前的 Stage）
const availableSourceStages = computed(() => {
	return props.stages.slice(0, currentStageIndex.value + 1);
});

// 初始化表单数据
const initFormData = () => {
	if (currentCondition.value) {
		// 编辑模式：加载现有数据
		formData.name = currentCondition.value.name;
		formData.description = currentCondition.value.description || '';

		// 解析 rulesJson - 格式为 JSON 字符串，解析后是 {logic, rules}
		let parsedRules: RuleFormItem[] = [];
		let logic: 'AND' | 'OR' = 'AND';

		try {
			const rulesData =
				typeof currentCondition.value.rulesJson === 'string'
					? JSON.parse(currentCondition.value.rulesJson)
					: currentCondition.value.rulesJson;
			logic = rulesData?.logic || 'AND';
			parsedRules = (rulesData?.rules || []).map((rule: any) => ({ ...rule }));
		} catch (e) {
			console.error('Failed to parse rulesJson:', e);
		}

		formData.logic = logic;
		formData.rules = parsedRules;

		// 解析 actionsJson（可能是字符串或对象）
		let actionsData = currentCondition.value.actionsJson;
		if (typeof actionsData === 'string') {
			try {
				actionsData = JSON.parse(actionsData);
			} catch (e) {
				console.error('Failed to parse actionsJson:', e);
				actionsData = [];
			}
		}

		formData.actions = (actionsData || []).map((action: any) => ({
			...action,
		}));

		formData.fallback = {
			type: currentCondition.value.fallbackStageId ? 'specified' : 'default',
			fallbackStageId: currentCondition.value.fallbackStageId,
		};

		// 如果没有规则，添加默认规则
		if (formData.rules.length === 0) {
			formData.rules = [
				{
					sourceStageId: currentStageId.value,
					componentType: 'questionnaires',
					fieldPath: '',
					operator: '==',
					value: '',
				},
			];
		}

		// 如果没有动作，添加默认动作
		if (formData.actions.length === 0) {
			formData.actions = [
				{
					type: 'GoToStage',
					targetStageId: '',
					order: 0,
				},
			];
		}
	} else {
		// 新建模式：初始化默认值
		formData.name = '';
		formData.description = '';
		formData.logic = 'AND';
		formData.rules = [
			{
				sourceStageId: currentStageId.value,
				componentType: 'questionnaires',
				fieldPath: '',
				operator: '==',
				value: '',
			},
		];
		formData.actions = [
			{
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

// 打开弹窗
const open = (
	stageId: string,
	stageName: string,
	stageIndex: number,
	condition?: StageCondition | null
) => {
	currentStageId.value = stageId;
	currentStageName.value = stageName;
	currentStageIndex.value = stageIndex;
	currentCondition.value = condition || null;
	initFormData();
	visible.value = true;
};

// 关闭弹窗
const close = () => {
	visible.value = false;
};

// 处理取消
const handleCancel = () => {
	emit('cancel');
	close();
};

// 构建提交数据
const buildSubmitData = () => {
	// 构建 rulesJson - 格式为 {logic, rules}
	const rulesJson = JSON.stringify({
		logic: formData.logic,
		rules: formData.rules,
	});

	// 构建 actionsJson
	const actionsJson = JSON.stringify(
		formData.actions.map((action, index) => ({
			...action,
			order: index,
		}))
	);

	return {
		stageId: currentStageId.value,
		workflowId: props.workflowId,
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
		let res: any;

		if (isEditMode.value && currentCondition.value) {
			// 更新 - PUT /stage-conditions/v1/{id}
			res = await updateCondition(currentCondition.value.id, submitData);
		} else {
			// 创建 - POST /stage-conditions/v1
			res = await createCondition(submitData);
		}

		if (res.code === '200') {
			ElMessage.success(
				isEditMode.value
					? 'Condition updated successfully'
					: 'Condition created successfully'
			);
			emit('save', res.data);
			close();
		}
	} finally {
		saving.value = false;
	}
};

// 暴露方法给父组件
defineExpose({
	open,
	close,
});
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

:deep(.el-drawer__body) {
	padding: 0;
}
</style>
