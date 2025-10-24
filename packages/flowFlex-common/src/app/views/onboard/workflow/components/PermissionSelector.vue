<template>
	<div class="grid grid-cols-1 lg:grid-cols-2 gap-6 p-1">
		<!-- 左侧：View Permission -->
		<div class="space-y-4 w-full">
			<div class="space-y-2">
				<label class="text-base font-bold">View Permission</label>

				<el-select
					v-model="localPermissions.viewPermissionMode"
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

			<!-- Team（仅在 VisibleToTeams 或 InvisibleToTeams 时显示）-->
			<div v-if="shouldShowTeamSelector" class="space-y-2">
				<label class="text-base font-bold">Team</label>
				<FlowflexUserSelector
					v-model="localPermissions.viewTeams"
					selectionType="team"
					:clearable="true"
				/>
			</div>
		</div>

		<!-- 右侧：Operate Permission -->
		<div class="space-y-4 w-full">
			<div class="space-y-2">
				<label class="text-base font-bold">Operate Permission</label>

				<el-checkbox v-model="localPermissions.useSameGroups">
					{{ checkboxLabel }}
				</el-checkbox>
			</div>

			<!-- Team（仅在不勾选 useSameGroups 时显示）-->
			<div v-if="!localPermissions.useSameGroups" class="space-y-2">
				<label class="text-base font-bold">Team</label>
				<FlowflexUserSelector
					v-model="localPermissions.operateTeams"
					selectionType="team"
					:clearable="true"
					:available-ids="operateFilterConfig.availableIds"
					:excluded-ids="operateFilterConfig.excludedIds"
					@focus="handleOperateFocus"
				/>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { reactive, computed, watch, nextTick, ref } from 'vue';
import { ViewPermissionModeEnum } from '@/enums/permissionEnum';
import FlowflexUserSelector from '@/components/form/flowflexUser/index.vue';
import { ElMessage } from 'element-plus';

// Props
interface Props {
	modelValue?: {
		viewPermissionMode: number;
		viewTeams: string[];
		useSameGroups: boolean;
		operateTeams: string[];
	};
}

const props = withDefaults(defineProps<Props>(), {
	modelValue: () => ({
		viewPermissionMode: ViewPermissionModeEnum.Public,
		viewTeams: [],
		useSameGroups: true,
		operateTeams: [],
	}),
});

// Emits
const emit = defineEmits(['update:modelValue']);

// 权限类型选项
const permissionTypeOptions = [
	{ label: 'Public', value: ViewPermissionModeEnum.Public },
	{ label: 'Visible to', value: ViewPermissionModeEnum.VisibleToTeams },
	{ label: 'Invisible to', value: ViewPermissionModeEnum.InvisibleToTeams },
];

// 是否需要显示 Team 选择器 - 只有 VisibleToTeams 和 InvisibleToTeams 需要选择团队
const shouldShowTeamSelector = computed(() => {
	const mode = localPermissions.viewPermissionMode;
	// Public 模式下不显示
	return (
		mode === ViewPermissionModeEnum.VisibleToTeams ||
		mode === ViewPermissionModeEnum.InvisibleToTeams
	);
});

// 动态checkbox文案 - Invisible模式显示特殊文案
const checkboxLabel = computed(() => {
	const mode = localPermissions.viewPermissionMode;
	if (mode === ViewPermissionModeEnum.InvisibleToTeams) {
		return 'Use same team that have view permission (not editable)';
	}
	return 'Use same team that have view permission';
});

// 本地权限数据
const localPermissions = reactive({
	viewPermissionMode: props.modelValue.viewPermissionMode ?? ViewPermissionModeEnum.Public,
	viewTeams: [...(props.modelValue.viewTeams || [])],
	useSameGroups: props.modelValue.useSameGroups ?? true,
	operateTeams: [...(props.modelValue.operateTeams || [])],
});

// 使用一个 ref 来跟踪是否正在处理内部更新
const isProcessingInternalUpdate = ref(false);

// 跟踪上一次的viewPermissionMode和useSameGroups状态
const previousViewPermissionMode = ref(localPermissions.viewPermissionMode);
const previousUseSameGroups = ref(localPermissions.useSameGroups);

const operateFilterConfig = computed(() => {
	const mode = localPermissions.viewPermissionMode;

	// Visible to 模式：白名单（只能从左侧已选team中选择）
	if (mode === ViewPermissionModeEnum.VisibleToTeams) {
		return { availableIds: localPermissions.viewTeams, excludedIds: undefined };
	}

	// Invisible to 模式：黑名单（排除左侧已选team）
	if (mode === ViewPermissionModeEnum.InvisibleToTeams) {
		return { availableIds: undefined, excludedIds: localPermissions.viewTeams };
	}

	// Public 或其他模式：不限制
	return { availableIds: undefined, excludedIds: undefined };
});

// 处理右侧team选择器获得焦点事件
const handleOperateFocus = () => {
	// 如果左侧未选择team，显示提示
	if (shouldShowTeamSelector.value && localPermissions.viewTeams.length === 0) {
		ElMessage.warning('Please select View Permission teams first');
	}
};

// 统一的数据处理函数
const processPermissionChanges = () => {
	if (isProcessingInternalUpdate.value) return;

	// 先设置标志位，防止内部修改触发 watch
	isProcessingInternalUpdate.value = true;

	// 使用 nextTick 确保在下一个事件循环中处理
	nextTick(() => {
		// 处理 viewPermissionMode 的变化 - Public 模式下清空 viewTeams
		if (!shouldShowTeamSelector.value && localPermissions.viewTeams.length > 0) {
			localPermissions.viewTeams = [];
		}

		// 检测viewPermissionMode在VisibleToTeams和InvisibleToTeams之间切换，清空operateTeams
		const currentMode = localPermissions.viewPermissionMode;
		const previousMode = previousViewPermissionMode.value;
		const isModeSwitchBetweenVisibleAndInvisible =
			(currentMode === ViewPermissionModeEnum.VisibleToTeams &&
				previousMode === ViewPermissionModeEnum.InvisibleToTeams) ||
			(currentMode === ViewPermissionModeEnum.InvisibleToTeams &&
				previousMode === ViewPermissionModeEnum.VisibleToTeams);

		if (isModeSwitchBetweenVisibleAndInvisible) {
			localPermissions.operateTeams = [];
		}

		// 更新previousViewPermissionMode
		previousViewPermissionMode.value = currentMode;

		// 检测useSameGroups变化
		const currentUseSameGroups = localPermissions.useSameGroups;
		const previousSameGroups = previousUseSameGroups.value;

		// 处理 operateTeams 的同步
		if (currentUseSameGroups) {
			// 勾选"使用相同团队"时，同步 viewTeams 到 operateTeams
			const newOperateTeams = shouldShowTeamSelector.value
				? [...localPermissions.viewTeams]
				: [];
			if (JSON.stringify(newOperateTeams) !== JSON.stringify(localPermissions.operateTeams)) {
				localPermissions.operateTeams = newOperateTeams;
			}
		} else if (previousSameGroups && !currentUseSameGroups) {
			// 从勾选变为取消勾选时，清空operateTeams
			localPermissions.operateTeams = [];
		}
		// 其他情况不做操作，保持operateTeams不变

		// 更新previousUseSameGroups
		previousUseSameGroups.value = currentUseSameGroups;

		// emit 更新到父组件
		emit('update:modelValue', {
			viewPermissionMode: localPermissions.viewPermissionMode,
			viewTeams: [...localPermissions.viewTeams],
			useSameGroups: localPermissions.useSameGroups,
			operateTeams: [...localPermissions.operateTeams],
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
