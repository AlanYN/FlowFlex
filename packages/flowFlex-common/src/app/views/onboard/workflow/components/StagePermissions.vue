<template>
	<div class="space-y-4">
		<div class="space-y-1">
			<h3 class="text-base font-bold">Stage Permissions</h3>
			<p class="text-sm text-gray-600">Configure who can view and operate this stage</p>
		</div>
		<PermissionSelector
			v-model="permissionsData"
			:view-limit-data="workFlowViewTeams"
			:operate-limit-data="workFlowOperateTeams"
			:work-flow-view-permission-mode="workFlowViewPermissionMode"
			:work-flow-view-use-same-team-for-operate="workFlowViewUseSameTeamForOperate"
			:is-workflow-level="false"
		/>

		<!-- Roll Back Teams -->
		<div class="space-y-2">
			<label class="text-base font-bold inline-flex items-center gap-x-1">
				Roll Back Teams
				<el-tooltip
					content="Only users from these teams can roll back this stage. Leave empty to disable roll back for all users."
					placement="top"
				>
					<Icon icon="mdi:information-outline" class="text-gray-400 cursor-help" />
				</el-tooltip>
			</label>
			<FlowflexUserSelector
				v-model="formData.rollBackTeams"
				selectionType="team"
				:clearable="true"
			/>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed, reactive, watch } from 'vue';
import PermissionSelector from './PermissionSelector.vue';
import FlowflexUserSelector from '@/components/form/flowflexUser/index.vue';
import { ViewPermissionModeEnum } from '@/enums/permissionEnum';

interface Props {
	modelValue?: {
		viewPermissionMode: number;
		viewTeams: string[];
		operateTeams: string[];
		useSameTeamForOperate: boolean;
		rollBackTeams: string[];
	};
	workFlowOperateTeams?: string[];
	workFlowViewTeams?: string[];
	workFlowViewPermissionMode?: number;
	workFlowViewUseSameTeamForOperate?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	modelValue: () => ({
		viewPermissionMode: ViewPermissionModeEnum.Public,
		viewTeams: [],
		operateTeams: [],
		useSameTeamForOperate: true,
		rollBackTeams: [],
	}),
	workFlowOperateTeams: () => [],
	workFlowViewTeams: () => [],
	workFlowViewPermissionMode: ViewPermissionModeEnum.Public,
	workFlowViewUseSameTeamForOperate: undefined,
});

const emit = defineEmits(['update:modelValue']);

// 表单数据
const formData = reactive({
	viewPermissionMode: props.modelValue.viewPermissionMode ?? ViewPermissionModeEnum.Public,
	viewTeams: [...(props.modelValue.viewTeams || [])],
	operateTeams: [...(props.modelValue.operateTeams || [])],
	useSameTeamForOperate: props.modelValue.useSameTeamForOperate ?? true,
	rollBackTeams: [...(props.modelValue.rollBackTeams || [])],
});

// 权限数据计算属性（用于 PermissionSelector 的 v-model）
const permissionsData = computed({
	get: () => ({
		viewPermissionMode: formData.viewPermissionMode,
		viewTeams: formData.viewTeams,
		useSameTeamForOperate: formData.useSameTeamForOperate,
		operateTeams: formData.operateTeams,
	}),
	set: (value: {
		viewPermissionMode: number;
		viewTeams: string[];
		useSameTeamForOperate: boolean;
		operateTeams: string[];
	}) => {
		formData.viewPermissionMode = value.viewPermissionMode;
		formData.viewTeams = value.viewTeams;
		formData.useSameTeamForOperate = value.useSameTeamForOperate;
		formData.operateTeams = value.operateTeams;

		// 向父组件发送更新
		emit('update:modelValue', {
			viewPermissionMode: formData.viewPermissionMode,
			viewTeams: formData.viewTeams,
			useSameTeamForOperate: formData.useSameTeamForOperate,
			operateTeams: formData.operateTeams,
			rollBackTeams: formData.rollBackTeams,
		});
	},
});

// 监听 rollBackTeams 变化，向父组件发送更新
watch(
	() => formData.rollBackTeams,
	() => {
		emit('update:modelValue', {
			viewPermissionMode: formData.viewPermissionMode,
			viewTeams: formData.viewTeams,
			useSameTeamForOperate: formData.useSameTeamForOperate,
			operateTeams: formData.operateTeams,
			rollBackTeams: formData.rollBackTeams,
		});
	},
	{ deep: true }
);

// 监听外部数据变化（逐字段比对，避免 emit → props 变化 → emit 的响应式循环）
watch(
	() => props.modelValue,
	(newVal) => {
		if (!newVal) return;
		if (
			(newVal.viewPermissionMode ?? ViewPermissionModeEnum.Public) !==
			formData.viewPermissionMode
		) {
			formData.viewPermissionMode =
				newVal.viewPermissionMode ?? ViewPermissionModeEnum.Public;
		}
		if (JSON.stringify(newVal.viewTeams || []) !== JSON.stringify(formData.viewTeams)) {
			formData.viewTeams = [...(newVal.viewTeams || [])];
		}
		if (JSON.stringify(newVal.operateTeams || []) !== JSON.stringify(formData.operateTeams)) {
			formData.operateTeams = [...(newVal.operateTeams || [])];
		}
		if ((newVal.useSameTeamForOperate ?? true) !== formData.useSameTeamForOperate) {
			formData.useSameTeamForOperate = newVal.useSameTeamForOperate ?? true;
		}
		if (JSON.stringify(newVal.rollBackTeams || []) !== JSON.stringify(formData.rollBackTeams)) {
			formData.rollBackTeams = [...(newVal.rollBackTeams || [])];
		}
	},
	{ deep: true }
);
</script>

<style scoped>
/* 简洁的布局样式 */
</style>
