<template>
	<div class="grid grid-cols-1 lg:grid-cols-2 gap-6 p-1">
		<!-- 左侧：View Permission -->
		<div class="space-y-4 w-full">
			<div class="space-y-2">
				<label class="text-base font-bold">View Permission</label>
				<p class="text-sm">Controls who can view cases using this workflow</p>

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
					placeholder="Select teams"
					:clearable="true"
				/>
			</div>
		</div>

		<!-- 右侧：Operate Permission -->
		<div class="space-y-4 w-full">
			<div class="space-y-2">
				<label class="text-base font-bold">Operate Permission</label>
				<p class="text-sm">Controls who can operate on cases using this workflow</p>

				<el-checkbox v-model="localPermissions.useSameGroups">
					Use same team that have view permission
				</el-checkbox>
			</div>

			<!-- Available Teams（仅在不勾选 useSameGroups 时显示）-->
			<div v-if="!localPermissions.useSameGroups" class="space-y-2">
				<label class="text-base font-bold">Available Teams</label>
				<FlowflexUserSelector
					v-model="localPermissions.operateTeams"
					selectionType="team"
					placeholder="Select teams"
					:clearable="true"
					:available-ids="shouldShowTeamSelector ? localPermissions.viewTeams : undefined"
				/>
				<p
					v-if="shouldShowTeamSelector && localPermissions.viewTeams.length === 0"
					class="text-xs text-gray-500"
				>
					Please select view permission teams first
				</p>
				<p
					v-if="shouldShowTeamSelector && localPermissions.viewTeams.length > 0"
					class="text-xs text-gray-500"
				>
					Only teams selected in view permission can be chosen
				</p>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { reactive, computed, watch, nextTick, ref } from 'vue';
import { ViewPermissionModeEnum, CasePermissionModeEnum } from '@/enums/permissionEnum';
import FlowflexUserSelector from '@/components/form/flowflexUser/index.vue';

// Props
interface Props {
	modelValue?: {
		viewPermissionMode: number;
		viewTeams: string[];
		useSameGroups: boolean;
		operateTeams: string[];
	};
	// 权限类型：'workflow' | 'stage' 使用 ViewPermissionModeEnum，'case' 使用 CasePermissionModeEnum
	// 未来可扩展其他类型
	type?: string;
}

const props = withDefaults(defineProps<Props>(), {
	modelValue: () => ({
		viewPermissionMode: ViewPermissionModeEnum.Public,
		viewTeams: [],
		useSameGroups: true,
		operateTeams: [],
	}),
	type: 'workflow', // 默认为 workflow 类型
});

// Emits
const emit = defineEmits(['update:modelValue']);

// 权限类型选项 - 根据 type 返回不同的枚举选项
const permissionTypeOptions = computed(() => {
	switch (props.type) {
		case 'case':
			// Case 场景：使用 CasePermissionModeEnum
			return [
				{ label: 'Public', value: CasePermissionModeEnum.Public },
				{ label: 'Visible to', value: CasePermissionModeEnum.VisibleToTeams },
				{ label: 'Invisible to', value: CasePermissionModeEnum.InvisibleToTeams },
				{ label: 'Private', value: CasePermissionModeEnum.Private },
			];
		case 'workflow':
		case 'stage':
		default:
			// 默认（workflow/stage）：使用 ViewPermissionModeEnum
			return [
				{ label: 'Public', value: ViewPermissionModeEnum.Public },
				{ label: 'Visible to', value: ViewPermissionModeEnum.VisibleToTeams },
				{ label: 'Invisible to', value: ViewPermissionModeEnum.InvisibleToTeams },
			];
	}
});

// Public 值辅助计算属性 - 根据 type 返回对应的 Public 枚举值
const publicValue = computed(() => {
	switch (props.type) {
		case 'case':
			return CasePermissionModeEnum.Public;
		case 'workflow':
		case 'stage':
		default:
			return ViewPermissionModeEnum.Public;
	}
});

// Private 值辅助计算属性 - 根据 type 返回对应的 Private 枚举值
const privateValue = computed(() => {
	switch (props.type) {
		case 'case':
			return CasePermissionModeEnum.Private;
		case 'workflow':
		case 'stage':
		default:
			// workflow/stage 没有 Private 选项，返回 null
			return null;
	}
});

// 是否需要显示 Team 选择器 - 只有 VisibleToTeams 和 InvisibleToTeams 需要选择团队
const shouldShowTeamSelector = computed(() => {
	const mode = localPermissions.viewPermissionMode;
	// Public 和 Private 模式下都不显示
	if (mode === publicValue.value || mode === privateValue.value) {
		return false;
	}
	return true;
});

// 本地权限数据
const localPermissions = reactive({
	viewPermissionMode: props.modelValue.viewPermissionMode ?? publicValue.value,
	viewTeams: [...(props.modelValue.viewTeams || [])],
	useSameGroups: props.modelValue.useSameGroups ?? true,
	operateTeams: [...(props.modelValue.operateTeams || [])],
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
		// 处理 viewPermissionMode 的变化 - Public 和 Private 模式下清空 viewTeams
		if (!shouldShowTeamSelector.value && localPermissions.viewTeams.length > 0) {
			localPermissions.viewTeams = [];
		}

		// 处理 operateTeams 的同步或过滤
		if (localPermissions.useSameGroups) {
			// 勾选"使用相同团队"时，同步 viewTeams 到 operateTeams
			const newOperateTeams = shouldShowTeamSelector.value
				? [...localPermissions.viewTeams]
				: [];
			if (JSON.stringify(newOperateTeams) !== JSON.stringify(localPermissions.operateTeams)) {
				localPermissions.operateTeams = newOperateTeams;
			}
		} else {
			// 不勾选时的处理逻辑
			if (!shouldShowTeamSelector.value) {
				// Public 和 Private 模式下，operateTeams 可以自由选择任何团队，无需过滤
				// 保持用户选择的 operateTeams 不变
			} else {
				// VisibleToTeams/InvisibleToTeams 模式下，Operate权限的团队范围必须 <= View权限的团队范围
				// 过滤掉不在 viewTeams 中的团队
				const filtered = localPermissions.operateTeams.filter((teamId) =>
					localPermissions.viewTeams.includes(teamId)
				);
				if (JSON.stringify(filtered) !== JSON.stringify(localPermissions.operateTeams)) {
					localPermissions.operateTeams = filtered;
				}
			}
		}

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

// 监听外部数据变化（从父组件接收更新）
watch(
	() => props.modelValue,
	(newVal) => {
		if (newVal && !isProcessingInternalUpdate.value) {
			// 检查是否真的有变化，避免不必要的更新
			const hasChanges =
				localPermissions.viewPermissionMode !== newVal.viewPermissionMode ||
				JSON.stringify(localPermissions.viewTeams) !== JSON.stringify(newVal.viewTeams) ||
				localPermissions.useSameGroups !== newVal.useSameGroups ||
				JSON.stringify(localPermissions.operateTeams) !==
					JSON.stringify(newVal.operateTeams);

			if (hasChanges) {
				isProcessingInternalUpdate.value = true;
				localPermissions.viewPermissionMode =
					newVal.viewPermissionMode ?? publicValue.value;
				localPermissions.viewTeams = [...(newVal.viewTeams || [])];
				localPermissions.useSameGroups = newVal.useSameGroups ?? true;
				localPermissions.operateTeams = [...(newVal.operateTeams || [])];
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
