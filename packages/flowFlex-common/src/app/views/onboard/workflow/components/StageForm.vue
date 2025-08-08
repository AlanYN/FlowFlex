<template>
	<div class="stage-form-container">
		<PrototypeTabs
			v-model="currentTab"
			:tabs="tabsConfig"
			class="editor-tabs"
			content-class="editor-content"
		>
			<TabPane value="basicInfo">
				<el-form ref="formRef" :model="formData" :rules="rules" label-position="top">
					<el-form-item label="Stage Name" prop="name">
						<el-input v-model="formData.name" placeholder="Enter stage name" />
					</el-form-item>

					<el-form-item label="Description" prop="description">
						<el-input
							v-model="formData.description"
							type="textarea"
							:rows="3"
							placeholder="Enter stage description"
						/>
					</el-form-item>

					<!-- Visible in Customer Portal -->
					<el-form-item label="Available in Customer Portal" prop="visibleInPortal">
						<el-switch
							v-model="formData.visibleInPortal"
							inline-prompt
							active-text="Yes"
							inactive-text="No"
						/>
					</el-form-item>

					<div class="flex items-center gap-2 w-full">
						<el-form-item
							label="Assigned User Group"
							prop="defaultAssignedGroup"
							class="w-1/2"
						>
							<el-select
								v-model="formData.defaultAssignedGroup"
								placeholder="Select user group"
								style="width: 100%"
							>
								<el-option
									v-for="item in defaultAssignedGroup"
									:key="item.value"
									:label="item.key"
									:value="item.value"
								/>
							</el-select>
						</el-form-item>

						<el-form-item label="Default Assignee" prop="defaultAssignee" class="w-1/2">
							<el-input
								v-model="formData.defaultAssignee"
								placeholder="Enter default assignee role"
							/>
						</el-form-item>
					</div>

					<el-form-item label="Estimated Duration" prop="estimatedDuration">
						<InputNumber
							v-model="formData.estimatedDuration as number"
							placeholder="e.g., 3 days"
						/>
					</el-form-item>

					<el-form-item label="Stage Color" prop="color">
						<div class="color-picker-container">
							<div class="color-grid">
								<div
									v-for="color in colorOptions"
									:key="color"
									class="color-option"
									:class="{ selected: formData.color === color }"
									:style="{ backgroundColor: color }"
									@click="formData.color = color"
								></div>
							</div>
						</div>
					</el-form-item>
				</el-form>
			</TabPane>
			<TabPane value="components">
				<StageComponentsSelector
					:checklists="checklists"
					:questionnaires="questionnaires"
					:model-value="{
						components: formData.components,
						visibleInPortal: formData.visibleInPortal,
						attachmentManagementNeeded: formData.attachmentManagementNeeded,
					}"
					@update:model-value="updateComponentsData"
				/>
			</TabPane>
		</PrototypeTabs>

		<div class="form-actions">
			<el-button @click="$emit('cancel')">Cancel</el-button>
			<el-button
				type="primary"
				:loading="loading"
				:disabled="!isFormValid"
				@click="submitForm"
			>
				{{ isEditing ? 'Update Stage' : 'Add Stage' }}
			</el-button>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, PropType, computed } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';
import InputNumber from '@/components/form/InputNumber/index.vue';
import { stageColorOptions, StageColorType } from '@/enums/stageColorEnum';
import { defaultAssignedGroup } from '@/enums/dealsAndLeadsOptions';
import StageComponentsSelector from './StageComponentsSelector.vue';

import { PrototypeTabs, TabPane } from '@/components/PrototypeTabs';
import { Checklist, Questionnaire, Stage, ComponentData } from '#/onboard';

// 颜色选项
const colorOptions = stageColorOptions;

// Props
const props = defineProps({
	stage: {
		type: Object as PropType<Stage | null>,
		default: null,
	},
	isEditing: {
		type: Boolean,
		default: false,
	},
	loading: {
		type: Boolean,
		default: false,
	},
	checklists: {
		type: Array as PropType<Checklist[]>,
		default: () => [],
	},
	questionnaires: {
		type: Array as PropType<Questionnaire[]>,
		default: () => [],
	},
});

// Tab配置
const currentTab = ref('basicInfo');
const tabsConfig = [
	{
		value: 'basicInfo',
		label: 'Basic Info',
	},
	{
		value: 'components',
		label: 'Components',
	},
];

// 表单数据
const formData = ref({
	name: '',
	description: '',
	visibleInPortal: false,
	defaultAssignedGroup: '',
	defaultAssignee: '',
	estimatedDuration: null as number | null,
	requiredFieldsJson: '',
	components: [] as ComponentData[],
	order: 0,
	color: colorOptions[Math.floor(Math.random() * colorOptions.length)] as StageColorType,
	attachmentManagementNeeded: false,
});

// 表单验证规则
const rules = reactive<FormRules>({
	name: [
		{ required: true, message: 'Please enter stage name', trigger: 'blur' },
		{ min: 1, max: 50, message: 'Length should be 1 to 50 characters', trigger: 'blur' },
	],
	defaultAssignedGroup: [
		{ required: true, message: 'Please select user group', trigger: 'change' },
	],
	estimatedDuration: [
		{ required: true, message: 'Please enter estimated duration', trigger: 'change' },
	],
});

// 计算属性
const isFormValid = computed(() => {
	return (
		!!formData.value.name &&
		!!formData.value.defaultAssignedGroup &&
		!!`${formData.value.estimatedDuration}`
	);
});

// 表单引用
const formRef = ref<FormInstance>();

// 初始化表单数据
onMounted(() => {
	if (props.stage) {
		Object.keys(formData.value).forEach((key) => {
			if (key === 'color') {
				formData.value[key] =
					props.stage && props.stage?.color
						? (props.stage[key] as StageColorType)
						: (colorOptions[
								Math.floor(Math.random() * colorOptions.length)
						  ] as StageColorType);
			} else if (key === 'components') {
				formData.value[key] = props.stage?.components || [];
			} else {
				formData.value[key] = props.stage ? props.stage[key] : '';
			}
		});
	} else {
		formData.value.color = colorOptions[
			Math.floor(Math.random() * colorOptions.length)
		] as StageColorType;
		formData.value.components = [];
	}
});

// 提交表单
const submitForm = async () => {
	if (!formRef.value) return;

	await formRef.value.validate((valid, fields) => {
		if (valid) {
			// 包含模块顺序信息
			const submitData = {
				...formData.value,
			};
			emit('submit', submitData);
		}
	});
};

// 定义事件
const emit = defineEmits(['submit', 'cancel']);

// 更新组件数据的方法
const updateComponentsData = (newData: any) => {
	formData.value.components = newData.components;
	if (Object.prototype.hasOwnProperty.call(newData, 'visibleInPortal')) {
		formData.value.visibleInPortal = newData.visibleInPortal;
	}
	if (Object.prototype.hasOwnProperty.call(newData, 'attachmentManagementNeeded')) {
		formData.value.attachmentManagementNeeded = newData.attachmentManagementNeeded;
	}
};
</script>

<style scoped>
.stage-form-container {
	width: 100%;
	max-width: 100%;
	overflow: hidden;
	display: flex;
	flex-direction: column;
	min-height: 0;
}

.form-actions {
	display: flex;
	justify-content: flex-end;
	gap: 10px;
	margin-top: 20px;
	flex-shrink: 0;
}

.color-picker-container {
	width: 100%;
}

.color-grid {
	display: flex;
	justify-content: space-between;
	padding: 0 10px;
}

.color-option {
	width: 32px;
	height: 32px;
	border-radius: 50%;
	cursor: pointer;
	transition: all 0.2s;
	border: 2px solid transparent;
}

.color-option:hover {
	transform: scale(1.1);
}

.color-option.selected {
	border-color: #333;
	transform: scale(1.1);
}

.form-item-description {
	font-size: 12px;
	color: #666;
	margin-top: 4px;
	line-height: 1.4;
}
</style>
