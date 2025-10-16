<template>
	<div class="grid grid-cols-1 lg:grid-cols-2 gap-6 p-1">
		<!-- 左侧：View Permission -->
		<div class="space-y-4 w-full">
			<div class="space-y-2">
				<label class="text-base font-bold">View Permission</label>
				<p class="text-sm">Controls who can view cases using this workflow</p>

				<el-select
					v-model="localPermissions.viewPermissionType"
					placeholder="Select permission type"
					class="w-full"
				>
					<el-option
						v-for="option in permissionTypeOptions"
						:key="option.value"
						:label="option.label"
						:value="option.value"
					/>
				</el-select>
			</div>

			<!-- User Groups（仅在非 public 时显示）-->
			<div v-if="localPermissions.viewPermissionType !== 'public'" class="space-y-2">
				<label class="text-base font-bold">User Groups</label>
				<el-select
					v-model="localPermissions.viewGroups"
					multiple
					placeholder="Add group..."
					class="w-full"
				>
					<el-option
						v-for="group in availableGroups"
						:key="group.value"
						:label="group.label"
						:value="group.value"
					/>
				</el-select>
			</div>
		</div>

		<!-- 右侧：Operate Permission -->
		<div class="space-y-4 w-full">
			<div class="space-y-2">
				<label class="text-base font-bold">Operate Permission</label>
				<p class="text-sm">Controls who can operate on cases using this workflow</p>

				<el-checkbox v-model="localPermissions.useSameGroups">
					Use same groups that have view permission
				</el-checkbox>
			</div>

			<!-- Available Groups（仅在不勾选 useSameGroups 时显示）-->
			<div v-if="!localPermissions.useSameGroups" class="space-y-2">
				<label class="text-base font-bold">Available Groups</label>
				<el-select
					v-model="localPermissions.operateGroups"
					multiple
					placeholder="Add group..."
					class="w-full"
					:disabled="availableOperateGroups.length === 0"
				>
					<el-option
						v-for="group in availableOperateGroups"
						:key="group.value"
						:label="group.label"
						:value="group.value"
					/>
				</el-select>
				<p v-if="availableOperateGroups.length === 0" class="text-xs text-gray-500">
					Please select view permission groups first
				</p>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { reactive, computed, watch, nextTick, ref } from 'vue';

// Props
interface Props {
	modelValue?: {
		viewPermissionType: string;
		viewGroups: string[];
		useSameGroups: boolean;
		operateGroups: string[];
	};
}

const props = withDefaults(defineProps<Props>(), {
	modelValue: () => ({
		viewPermissionType: 'public',
		viewGroups: [],
		useSameGroups: true,
		operateGroups: [],
	}),
});

// Emits
const emit = defineEmits(['update:modelValue']);

// 权限类型选项（临时写死）
const permissionTypeOptions = [
	{ label: 'Public', value: 'public' },
	{ label: 'Visible to', value: 'visible_to' },
	{ label: 'Invisible to', value: 'invisible_to' },
];

// 可用组列表（临时写死）
const availableGroups = [
	{ label: 'Admin', value: 'admin' },
	{ label: 'Manager', value: 'manager' },
	{ label: 'HR', value: 'hr' },
	{ label: 'Finance', value: 'finance' },
	{ label: 'Sales', value: 'sales' },
	{ label: 'Support', value: 'support' },
];

// 本地权限数据
const localPermissions = reactive({
	viewPermissionType: props.modelValue.viewPermissionType || 'public',
	viewGroups: [...(props.modelValue.viewGroups || [])],
	useSameGroups: props.modelValue.useSameGroups ?? true,
	operateGroups: [...(props.modelValue.operateGroups || [])],
});

// 计算 Operate 可选的组（必须是 View 已选的子集）
const availableOperateGroups = computed(() => {
	if (localPermissions.viewPermissionType === 'public') {
		return availableGroups;
	}
	// 只能选择 View 已选择的组
	return availableGroups.filter((group) => localPermissions.viewGroups.includes(group.value));
});

// 使用一个 ref 来跟踪是否正在处理内部更新
const isProcessingInternalUpdate = ref(false);

// 统一的数据处理函数
const processPermissionChanges = () => {
	if (isProcessingInternalUpdate.value) return;

	// 先设置标志位，防止内部修改触发 watch
	isProcessingInternalUpdate.value = true;

	// 使用 nextTick 确保在下一个事件循环中处理
	nextTick(() => {
		// 处理 viewPermissionType 的变化
		if (
			localPermissions.viewPermissionType === 'public' &&
			localPermissions.viewGroups.length > 0
		) {
			localPermissions.viewGroups = [];
		}

		// 处理 operateGroups 的同步或过滤
		if (localPermissions.useSameGroups) {
			const newOperateGroups =
				localPermissions.viewPermissionType === 'public'
					? []
					: [...localPermissions.viewGroups];
			if (
				JSON.stringify(newOperateGroups) !== JSON.stringify(localPermissions.operateGroups)
			) {
				localPermissions.operateGroups = newOperateGroups;
			}
		} else {
			const filtered = localPermissions.operateGroups.filter((group) =>
				localPermissions.viewGroups.includes(group)
			);
			if (JSON.stringify(filtered) !== JSON.stringify(localPermissions.operateGroups)) {
				localPermissions.operateGroups = filtered;
			}
		}

		// emit 更新到父组件
		emit('update:modelValue', {
			viewPermissionType: localPermissions.viewPermissionType,
			viewGroups: [...localPermissions.viewGroups],
			useSameGroups: localPermissions.useSameGroups,
			operateGroups: [...localPermissions.operateGroups],
		});

		// 重置标志位
		nextTick(() => {
			isProcessingInternalUpdate.value = false;
		});
	});
};

// 只监听 localPermissions 的变化，统一处理
watch(
	localPermissions,
	() => {
		processPermissionChanges();
	},
	{ deep: true }
);

// 监听外部数据变化（从父组件接收更新）
watch(
	() => props.modelValue,
	(newVal) => {
		if (newVal && !isProcessingInternalUpdate.value) {
			// 检查是否真的有变化，避免不必要的更新
			const hasChanges =
				localPermissions.viewPermissionType !== newVal.viewPermissionType ||
				JSON.stringify(localPermissions.viewGroups) !== JSON.stringify(newVal.viewGroups) ||
				localPermissions.useSameGroups !== newVal.useSameGroups ||
				JSON.stringify(localPermissions.operateGroups) !==
					JSON.stringify(newVal.operateGroups);

			if (hasChanges) {
				isProcessingInternalUpdate.value = true;
				localPermissions.viewPermissionType = newVal.viewPermissionType || 'public';
				localPermissions.viewGroups = [...(newVal.viewGroups || [])];
				localPermissions.useSameGroups = newVal.useSameGroups ?? true;
				localPermissions.operateGroups = [...(newVal.operateGroups || [])];
				nextTick(() => {
					isProcessingInternalUpdate.value = false;
				});
			}
		}
	},
	{ deep: true }
);
</script>

<style scoped>
/* 确保 checkbox label 换行显示 */
:deep(.el-checkbox__label) {
	white-space: normal;
	line-height: 1.5;
}
/* :deep(.el-tag.el-tag--default) { */
/* } */
</style>
