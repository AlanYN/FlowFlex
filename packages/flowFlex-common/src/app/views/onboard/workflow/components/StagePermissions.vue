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
			:is-workflow-level="false"
		/>
	</div>
</template>

<script setup lang="ts">
import { computed, reactive, watch } from 'vue';
import PermissionSelector from './PermissionSelector.vue';
import { ViewPermissionModeEnum } from '@/enums/permissionEnum';

interface Props {
	modelValue?: {
		viewPermissionMode: number;
		viewTeams: string[];
		operateTeams: string[];
		useSameTeamForOperate: boolean;
	};
	workFlowOperateTeams?: string[];
	workFlowViewTeams?: string[];
	workFlowViewPermissionMode?: number;
}

const props = withDefaults(defineProps<Props>(), {
	modelValue: () => ({
		viewPermissionMode: ViewPermissionModeEnum.Public,
		viewTeams: [],
		operateTeams: [],
		useSameTeamForOperate: true,
	}),
	workFlowOperateTeams: () => [],
	workFlowViewTeams: () => [],
	viewPermissionMode: ViewPermissionModeEnum.Public,
});

const emit = defineEmits(['update:modelValue']);

// 表单数据
const formData = reactive({
	viewPermissionMode: props.modelValue.viewPermissionMode ?? ViewPermissionModeEnum.Public,
	viewTeams: [...(props.modelValue.viewTeams || [])],
	operateTeams: [...(props.modelValue.operateTeams || [])],
	useSameTeamForOperate: props.modelValue.useSameTeamForOperate ?? true,
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
		});
	},
});

// 监听外部数据变化
watch(
	() => props.modelValue,
	(newVal) => {
		if (newVal) {
			formData.viewPermissionMode =
				newVal.viewPermissionMode ?? ViewPermissionModeEnum.Public;
			formData.viewTeams = [...(newVal.viewTeams || [])];
			formData.operateTeams = [...(newVal.operateTeams || [])];
			formData.useSameTeamForOperate = newVal.useSameTeamForOperate ?? true;
		}
	},
	{ deep: true }
);
</script>

<style scoped>
/* 简洁的布局样式 */
</style>
