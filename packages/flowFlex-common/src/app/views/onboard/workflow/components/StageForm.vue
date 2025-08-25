<template>
	<div class="stage-form-container">
		<PrototypeTabs
			v-model="currentTab"
			:tabs="tabsConfig"
			class="editor-tabs"
			content-class="editor-content"
			@tab-change="onTabChange"
		>
			<TabPane value="basicInfo">
				<el-form
					ref="formRef"
					:model="formData"
					:rules="rules"
					label-position="top"
					class="p-1"
				>
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

					<!-- Portal Permission Options - only show when visibleInPortal is true -->
					<el-form-item v-if="formData.visibleInPortal" prop="portalPermission">
						<el-radio-group
							v-model="formData.portalPermission"
							class="portal-permission-group"
						>
							<el-radio
								v-for="option in portalPermissionOptions"
								:key="option.value"
								:value="option.value"
								class="portal-permission-option"
							>
								{{ option.label }}
							</el-radio>
						</el-radio-group>
					</el-form-item>

					<div class="flex items-center gap-2 w-full">
						<el-form-item
							label="Assigned User Group"
							prop="defaultAssignedGroup"
							class="w-1/2"
						>
							<FlowflexUser
								v-model="formData.defaultAssignedGroup"
								placeholder="Select user group"
								:multiple="false"
								:clearable="true"
								team-only
							/>
						</el-form-item>

						<el-form-item label="Default Assignee" prop="defaultAssignee" class="w-1/2">
							<FlowflexUser
								v-model="formData.defaultAssignee"
								placeholder="Select default assignee"
								:multiple="false"
								:clearable="true"
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
						portalPermission: formData.portalPermission,
						attachmentManagementNeeded: formData.attachmentManagementNeeded,
					}"
					@update:model-value="updateComponentsData"
				/>
			</TabPane>
			<TabPane value="actions">
				<Action ref="actionRef" :stage-id="formData.id" :workflow-id="workflowId" />
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
import { PortalPermissionEnum, portalPermissionOptions } from '@/enums/portalPermissionEnum';
import StageComponentsSelector from './StageComponentsSelector.vue';
import Action from '@/components/actionTools/Action.vue';
import FlowflexUser from '@/components/form/flowflexUser/index.vue';

import { PrototypeTabs, TabPane } from '@/components/PrototypeTabs';
import { Checklist, Questionnaire, Stage, ComponentsData, StageComponentData } from '#/onboard';

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
	workflowId: {
		type: String,
		default: '',
	},
});

// Tab配置
const currentTab = ref('basicInfo');
const tabsConfig = computed(() => {
	return props?.stage?.id
		? [
				{
					value: 'basicInfo',
					label: 'Basic Info',
				},
				{
					value: 'components',
					label: 'Components',
				},
				{
					value: 'actions',
					label: 'Actions',
				},
		  ]
		: [
				{
					value: 'basicInfo',
					label: 'Basic Info',
				},
				{
					value: 'components',
					label: 'Components',
				},
		  ];
});

// 表单数据
const formData = ref({
	id: '',
	name: '',
	description: '',
	visibleInPortal: false,
	portalPermission: PortalPermissionEnum.Viewable,
	defaultAssignedGroup: '',
	defaultAssignee: '',
	estimatedDuration: null as number | null,
	requiredFieldsJson: '',
	components: [] as StageComponentData[],
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
const actionRef = ref<InstanceType<typeof Action>>();

const onTabChange = (tab: string) => {
	currentTab.value = tab;
	if (tab === 'actions') {
		actionRef.value?.getActionList();
	}
};

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
			} else if (key === 'portalPermission') {
				formData.value[key] =
					props.stage?.portalPermission || PortalPermissionEnum.Viewable;
			} else {
				formData.value[key] = props.stage ? (props.stage as any)[key] : '';
			}
		});
	}
});

// Method to update components data
function updateComponentsData(val: ComponentsData) {
	formData.value.components = val.components;
	formData.value.visibleInPortal = val.visibleInPortal ?? false;
	if (val.portalPermission !== undefined) {
		formData.value.portalPermission = val.portalPermission;
	}
	formData.value.attachmentManagementNeeded = val.attachmentManagementNeeded ?? false;
}

// 提交
function submitForm() {
	// 透传表单数据
	const payload = { ...formData.value } as any;
	// 颜色值
	payload.color = formData.value.color;
	// 发出提交事件
	// @ts-ignore
	emit('submit', payload);
}

// emits
const emit = defineEmits(['submit', 'cancel']);
</script>

<style scoped>
.stage-form-container {
	width: 100%;
}
.editor-tabs {
	margin-bottom: 16px;
}
.editor-content {
	padding-top: 8px;
}
.color-picker-container {
	width: 100%;
}
.color-grid {
	display: grid;
	grid-template-columns: repeat(10, 1fr);
	gap: 12px;
}
.color-option {
	width: 28px;
	height: 28px;
	border-radius: 9999px;
	cursor: pointer;
	border: 2px solid transparent;
}
.color-option.selected {
	border-color: var(--el-color-primary);
}
.form-actions {
	display: flex;
	justify-content: flex-end;
	gap: 12px;
	margin-top: 16px;
}
.text-muted {
	color: #6b7280;
}
.portal-permission-group {
	width: 100%;
}
.portal-permission-option {
	display: block;
	margin-bottom: 8px;
	width: 100%;
}
</style>
