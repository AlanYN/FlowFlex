<template>
	<div class="space-y-4">
		<div class="space-y-1">
			<h3 class="text-base font-bold">Stage Permissions</h3>
			<p class="text-sm text-gray-600">Configure who can view and operate this stage</p>
		</div>
		<PermissionSelector v-model="permissionsData" />
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
	};
}

const props = withDefaults(defineProps<Props>(), {
	modelValue: () => ({
		viewPermissionMode: ViewPermissionModeEnum.Public,
		viewTeams: [],
		operateTeams: [],
	}),
});

const emit = defineEmits(['update:modelValue']);

// 表单数据
const formData = reactive({
	viewPermissionMode: props.modelValue.viewPermissionMode ?? ViewPermissionModeEnum.Public,
	viewTeams: [...(props.modelValue.viewTeams || [])],
	operateTeams: [...(props.modelValue.operateTeams || [])],
});

// 权限数据计算属性（用于 PermissionSelector 的 v-model）
const permissionsData = computed({
	get: () => ({
		viewPermissionMode: formData.viewPermissionMode,
		viewTeams: formData.viewTeams,
		useSameGroups: JSON.stringify(formData.viewTeams) === JSON.stringify(formData.operateTeams),
		operateTeams: formData.operateTeams,
	}),
	set: (value: {
		viewPermissionMode: number;
		viewTeams: string[];
		useSameGroups: boolean;
		operateTeams: string[];
	}) => {
		formData.viewPermissionMode = value.viewPermissionMode;
		formData.viewTeams = value.viewTeams;
		formData.operateTeams = value.operateTeams;

		// 向父组件发送更新
		emit('update:modelValue', {
			viewPermissionMode: formData.viewPermissionMode,
			viewTeams: formData.viewTeams,
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
		}
	},
	{ deep: true }
);
</script>

<style scoped>
/* 简洁的布局样式 */
</style>
