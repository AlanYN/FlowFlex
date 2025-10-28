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
					@change="leftTypeChange"
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
					ref="viewTeamSelectorRef"
					v-model="localPermissions.viewTeams"
					selectionType="team"
					:clearable="true"
					@change="leftChange"
				/>
			</div>
		</div>

		<!-- 右侧：Operate Permission -->
		<div class="space-y-4 w-full">
			<div class="space-y-2">
				<label class="text-base font-bold">Operate Permission</label>

				<el-checkbox
					:class="{
						invisible:
							localPermissions.viewPermissionMode ===
							ViewPermissionModeEnum.InvisibleToTeams,
					}"
					v-model="localPermissions.useSameGroups"
				>
					Use same team that have view permission
				</el-checkbox>
			</div>

			<!-- Team（仅在不勾选 useSameGroups 时显示）-->
			<div
				v-if="
					!localPermissions.useSameGroups ||
					localPermissions.viewPermissionMode === ViewPermissionModeEnum.InvisibleToTeams
				"
				class="space-y-2"
			>
				<label class="text-base font-bold">Team</label>
				<FlowflexUserSelector
					v-model="localPermissions.operateTeams"
					selectionType="team"
					:clearable="true"
					:choosable-tree-data="operateChoosableTreeData"
				/>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { reactive, computed, ref, watch, nextTick, onMounted } from 'vue';
import { ViewPermissionModeEnum } from '@/enums/permissionEnum';
import FlowflexUserSelector from '@/components/form/flowflexUser/index.vue';
import { menuRoles } from '@/stores/modules/menuFunction';
import type { FlowflexUser } from '#/golbal';

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

// 本地权限数据
const localPermissions = reactive({
	viewPermissionMode: props.modelValue.viewPermissionMode ?? ViewPermissionModeEnum.Public,
	viewTeams: [...(props.modelValue.viewTeams || [])],
	useSameGroups: props.modelValue.useSameGroups ?? true,
	operateTeams: [...(props.modelValue.operateTeams || [])],
});

// 左侧 Team 选择器的 ref
const viewTeamSelectorRef = ref<InstanceType<typeof FlowflexUserSelector> | null>(null);

// 获取 menuStore 实例
const menuStore = menuRoles();

// 右侧可选的树形数据
const operateChoosableTreeData = ref<FlowflexUser[] | undefined>(undefined);

// 使用一个 ref 来跟踪是否正在处理内部更新
const isProcessingInternalUpdate = ref(false);

const leftTypeChange = () => {
	leftChange(localPermissions.viewTeams);
};

onMounted(() => {
	nextTick(() => {
		if (shouldShowTeamSelector.value && localPermissions.viewTeams.length > 0) {
			leftTypeChange();
		}
	});
});

const leftChange = async (value) => {
	const mode = localPermissions.viewPermissionMode;

	if (mode === ViewPermissionModeEnum.InvisibleToTeams) {
		localPermissions.useSameGroups = false;
	}
	// Build a map of selected IDs for quick lookup
	const selectedIdSet = new Set<string>(value as string[]);

	// 如果没有选中任何数据，直接返回
	if (selectedIdSet.size === 0) {
		operateChoosableTreeData.value = undefined;
		return;
	}

	// 获取完整树数据（不依赖 ref）
	const fullTreeData = await menuStore.getFlowflexUserDataWithCache('');
	console.log('Using full tree data for building maps');

	// Build parent-child relationship map
	const nodeMap = new Map<string, FlowflexUser>();
	const childToParentMap = new Map<string, string>();

	function buildMaps(nodes: FlowflexUser[], parentId?: string) {
		nodes.forEach((node) => {
			nodeMap.set(node.id, node);
			if (parentId) {
				childToParentMap.set(node.id, parentId);
			}
			if (node.children && node.children.length > 0) {
				buildMaps(node.children, node.id);
			}
		});
	}

	buildMaps(fullTreeData);

	// Whitelist mode: only keep selected nodes (avoid parent-child duplicates)
	if (mode === ViewPermissionModeEnum.VisibleToTeams) {
		console.log('Whitelist mode: filtering selected nodes');

		// Filter logic: avoid duplicates (parent + children both selected)
		const resultIds = new Set<string>();

		selectedIdSet.forEach((nodeId: string) => {
			const node = nodeMap.get(nodeId);
			if (!node) {
				console.log(`Node ${nodeId} not found in map`);
				return;
			}

			// Check if any parent is also selected
			let currentId: string = nodeId;
			let hasSelectedParent = false;

			while (childToParentMap.has(currentId)) {
				const parentId: string = childToParentMap.get(currentId)!;
				if (selectedIdSet.has(parentId)) {
					console.log(
						`Node ${nodeId} (${node.name}) has selected parent ${parentId}, skipping`
					);
					hasSelectedParent = true;
					break;
				}
				currentId = parentId;
			}

			// Only add if no parent is selected
			if (!hasSelectedParent) {
				console.log(`Adding node ${nodeId} (${node.name})`);
				resultIds.add(nodeId);
			}
		});

		// Build final result array
		const newTreeData = Array.from(resultIds)
			.map((id: string) => nodeMap.get(id)!)
			.filter(Boolean);
		console.log('Final newTreeData (whitelist):', newTreeData);
		operateChoosableTreeData.value = newTreeData.length > 0 ? newTreeData : [];
		return;
	}

	// Blacklist mode: exclude selected nodes from full tree
	if (mode === ViewPermissionModeEnum.InvisibleToTeams) {
		console.log('Blacklist mode: excluding selected nodes from full tree');

		// Extract all selected IDs (including children if parent is selected)
		const excludeIds = new Set<string>();

		const collectNodeAndChildren = (nodeId: string) => {
			excludeIds.add(nodeId);
			const node = nodeMap.get(nodeId);
			if (node && node.children && node.children.length > 0) {
				node.children.forEach((child) => {
					collectNodeAndChildren(child.id);
				});
			}
		};

		selectedIdSet.forEach((nodeId: string) => {
			collectNodeAndChildren(nodeId);
		});

		console.log('Exclude IDs:', Array.from(excludeIds));

		// Recursively filter tree to exclude selected nodes
		const filterTree = (nodes: FlowflexUser[]): FlowflexUser[] => {
			const result: FlowflexUser[] = [];

			for (const node of nodes) {
				// Skip excluded nodes
				if (excludeIds.has(node.id)) {
					console.log(`Excluding node: ${node.id} (${node.name})`);
					continue;
				}

				// Keep node but filter children
				const newNode: FlowflexUser = { ...node };
				if (newNode.children && newNode.children.length > 0) {
					newNode.children = filterTree(newNode.children);
				}

				result.push(newNode);
			}

			return result;
		};

		const filteredTreeData = filterTree(fullTreeData);
		console.log('Final filtered tree (blacklist):', filteredTreeData);
		operateChoosableTreeData.value = filteredTreeData.length > 0 ? filteredTreeData : [];
		return;
	}

	// Public mode or other modes
	console.log('Public or other mode: no filtering');
	operateChoosableTreeData.value = undefined;
};

// 统一的数据处理函数
const processPermissionChanges = () => {
	if (isProcessingInternalUpdate.value) return;

	// 先设置标志位，防止内部修改触发 watch
	isProcessingInternalUpdate.value = true;

	// 使用 nextTick 确保在下一个事件循环中处理
	nextTick(() => {
		// 处理 viewPermissionMode 的变化
		if (!shouldShowTeamSelector.value) {
			// Public 模式清空
			if (localPermissions.viewTeams.length > 0) {
				localPermissions.viewTeams = [];
			}
			if (localPermissions.operateTeams.length > 0) {
				localPermissions.operateTeams = [];
			}
			operateChoosableTreeData.value = undefined;
		}

		// 处理 operateTeams 的同步
		if (localPermissions.useSameGroups) {
			// 勾选"使用相同"时，同步 view 的选择到 operate
			const newOperateTeams = shouldShowTeamSelector.value
				? [...localPermissions.viewTeams]
				: [];

			if (JSON.stringify(newOperateTeams) !== JSON.stringify(localPermissions.operateTeams)) {
				localPermissions.operateTeams = newOperateTeams;
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

// 监听 localPermissions 的变化，统一处理
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
