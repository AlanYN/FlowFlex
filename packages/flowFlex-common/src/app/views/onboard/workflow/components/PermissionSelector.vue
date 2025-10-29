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

			<!-- Team（仅在 VisibleTo 或 InvisibleTo 时显示）-->
			<div v-if="shouldShowTeamSelector" class="space-y-2">
				<label class="text-base font-bold">Team</label>
				<FlowflexUserSelector
					ref="viewTeamSelectorRef"
					v-model="localPermissions.viewTeams"
					selectionType="team"
					:clearable="true"
					:choosable-tree-data="viewChoosableTreeData"
					@change="leftChange(localPermissions.viewTeams)"
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
							ViewPermissionModeEnum.InvisibleTo,
					}"
					v-model="localPermissions.useSameTeamForOperate"
				>
					Use same team that have view permission
				</el-checkbox>
			</div>

			<!-- Team（仅在不勾选 useSameTeamForOperate 时显示）-->
			<div
				v-if="
					!localPermissions.useSameTeamForOperate ||
					localPermissions.viewPermissionMode === ViewPermissionModeEnum.InvisibleTo
				"
				class="space-y-2"
			>
				<label class="text-base font-bold">Team</label>
				<FlowflexUserSelector
					v-model="localPermissions.operateTeams"
					selectionType="team"
					:clearable="true"
					:choosable-tree-data="operateChoosableTreeData"
					:before-open="handleBeforeOpen"
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
import { ElMessage } from 'element-plus';
// Props
interface Props {
	modelValue?: {
		viewPermissionMode: number;
		viewTeams: string[];
		useSameTeamForOperate: boolean;
		operateTeams: string[];
	};
	viewLimitData?: string[];
	operateLimitData?: string[];
	workFlowViewPermissionMode?: number;
	isWorkflowLevel?: boolean; // 是否是 workflow 级别调用，true 时不使用 workflow 过滤
}

const props = withDefaults(defineProps<Props>(), {
	modelValue: () => ({
		viewPermissionMode: ViewPermissionModeEnum.Public,
		viewTeams: [],
		useSameTeamForOperate: true,
		operateTeams: [],
	}),
	viewLimitData: () => [],
	operateLimitData: () => [],
	workFlowViewPermissionMode: undefined,
	isWorkflowLevel: false,
});

// Emits
const emit = defineEmits(['update:modelValue']);

// 权限类型选项
const permissionTypeOptions = [
	{ label: 'Public', value: ViewPermissionModeEnum.Public },
	{ label: 'Visible to', value: ViewPermissionModeEnum.VisibleTo },
	{ label: 'Invisible to', value: ViewPermissionModeEnum.InvisibleTo },
];

// 是否需要显示 Team 选择器 - 只有 VisibleTo 和 InvisibleTo 需要选择团队
const shouldShowTeamSelector = computed(() => {
	const mode = localPermissions.viewPermissionMode;
	// Public 模式下不显示
	return mode === ViewPermissionModeEnum.VisibleTo || mode === ViewPermissionModeEnum.InvisibleTo;
});

// 本地权限数据
const localPermissions = reactive({
	viewPermissionMode: props.modelValue.viewPermissionMode ?? ViewPermissionModeEnum.Public,
	viewTeams: [...(props.modelValue.viewTeams || [])],
	useSameTeamForOperate: props.modelValue.useSameTeamForOperate ?? true,
	operateTeams: [...(props.modelValue.operateTeams || [])],
});

// 左侧 Team 选择器的 ref
const viewTeamSelectorRef = ref<InstanceType<typeof FlowflexUserSelector> | null>(null);

// 获取 menuStore 实例
const menuStore = menuRoles();

// 右侧可选的树形数据
const operateChoosableTreeData = ref<FlowflexUser[] | undefined>(undefined);
const viewChoosableTreeData = ref<FlowflexUser[] | undefined>(undefined);
const fullTreeDataCache = ref<FlowflexUser[] | null>(null);

// 使用一个 ref 来跟踪是否正在处理内部更新
const isProcessingInternalUpdate = ref(false);

const handleBeforeOpen = async () => {
	if (
		localPermissions.viewPermissionMode !== ViewPermissionModeEnum.Public &&
		localPermissions.viewTeams.length === 0
	) {
		ElMessage.warning('Please select a team for view permission');
		return false;
	}
	return true;
};

const getFullTreeData = async (): Promise<FlowflexUser[]> => {
	if (!fullTreeDataCache.value) {
		const data = await menuStore.getFlowflexUserDataWithCache('');
		fullTreeDataCache.value = Array.isArray(data) ? data : [];
	}
	return fullTreeDataCache.value!;
};

const updateViewChoosableTreeData = async () => {
	// 如果是 workflow 级别调用，左侧显示全部数据
	if (props.isWorkflowLevel) {
		viewChoosableTreeData.value = undefined; // undefined 表示不限制，显示全部数据
		return;
	}

	// 第一层过滤：使用 viewLimitData 过滤全部数据，得到左侧可选数据 A
	const limitIds = props.viewLimitData ?? [];
	const mode =
		props.workFlowViewPermissionMode !== undefined
			? props.workFlowViewPermissionMode
			: undefined;

	if (mode === undefined || mode === ViewPermissionModeEnum.Public) {
		viewChoosableTreeData.value = undefined;
		return;
	}

	const fullTreeData = await getFullTreeData();
	if (!fullTreeData || fullTreeData.length === 0) {
		viewChoosableTreeData.value = [];
		return;
	}

	const nodeMap = new Map<string, FlowflexUser>();
	const childToParentMap = new Map<string, string>();
	const buildMaps = (nodes: FlowflexUser[], parentId?: string) => {
		nodes.forEach((node) => {
			nodeMap.set(node.id, node);
			if (parentId) {
				childToParentMap.set(node.id, parentId);
			}
			if (node.children && node.children.length > 0) {
				buildMaps(node.children, node.id);
			}
		});
	};
	buildMaps(fullTreeData);

	if (mode === ViewPermissionModeEnum.VisibleTo) {
		if (!limitIds.length) {
			viewChoosableTreeData.value = [];
			return;
		}

		const limitSet = new Set<string>(limitIds);
		const resultIds = new Set<string>();

		limitIds.forEach((nodeId) => {
			if (!nodeMap.has(nodeId)) {
				return;
			}

			let currentId = nodeId;
			let hasSelectedParent = false;

			while (childToParentMap.has(currentId)) {
				const parentId = childToParentMap.get(currentId)!;
				if (limitSet.has(parentId)) {
					hasSelectedParent = true;
					break;
				}
				currentId = parentId;
			}

			if (!hasSelectedParent) {
				resultIds.add(nodeId);
			}
		});

		const cloneNode = (node: FlowflexUser): FlowflexUser => {
			const newNode: FlowflexUser = { ...node };
			if (newNode.children && newNode.children.length > 0) {
				// 过滤掉 user 类型的子节点，只保留 team
				newNode.children = newNode.children
					.filter((child) => child.type === 'team')
					.map((child) => cloneNode(child));
			}
			return newNode;
		};

		const whitelistTree = Array.from(resultIds)
			.map((id) => nodeMap.get(id))
			.filter(Boolean)
			.map((node) => cloneNode(node!));

		viewChoosableTreeData.value = whitelistTree.length > 0 ? whitelistTree : [];
		return;
	}

	// Blacklist (InvisibleTo)
	if (!limitIds.length) {
		viewChoosableTreeData.value = undefined;
		return;
	}

	const excludeIds = new Set<string>();
	const collectNodeAndChildren = (nodeId: string) => {
		if (excludeIds.has(nodeId)) return;
		excludeIds.add(nodeId);
		const node = nodeMap.get(nodeId);
		if (node && node.children && node.children.length > 0) {
			node.children.forEach((child) => collectNodeAndChildren(child.id));
		}
	};

	limitIds.forEach((nodeId) => {
		if (nodeMap.has(nodeId)) {
			collectNodeAndChildren(nodeId);
		}
	});

	const filterTree = (nodes: FlowflexUser[]): FlowflexUser[] => {
		const result: FlowflexUser[] = [];
		nodes.forEach((node) => {
			// 跳过被排除的节点
			if (excludeIds.has(node.id)) {
				return;
			}
			// 只保留 team 类型的节点，过滤掉 user
			if (node.type !== 'team') {
				return;
			}
			const newNode: FlowflexUser = { ...node };
			if (newNode.children && newNode.children.length > 0) {
				newNode.children = filterTree(newNode.children);
			}
			result.push(newNode);
		});
		return result;
	};

	const filteredTree = filterTree(fullTreeData);
	viewChoosableTreeData.value = filteredTree.length > 0 ? filteredTree : [];
};

const leftTypeChange = async () => {
	// 清空右侧已选的 operateTeams
	localPermissions.operateTeams = [];
	// 先更新左侧可选数据（第一层过滤）
	await updateViewChoosableTreeData();
	// 再根据左侧已选数据过滤右侧（第二层过滤）
	await leftChange(localPermissions.viewTeams);
};

onMounted(() => {
	nextTick(() => {
		updateViewChoosableTreeData().then(() => {
			leftChange(localPermissions.viewTeams, false);
		});
	});
});

const leftChange = async (value, needEditLocalPermissions: boolean = true) => {
	const mode = localPermissions.viewPermissionMode;

	if (mode === ViewPermissionModeEnum.InvisibleTo) {
		localPermissions.useSameTeamForOperate = false;
	}

	console.log('operateLimitData:', props.viewLimitData);
	console.log('=== leftChange Start ===');
	console.log('localPermissions.viewPermissionMode:', mode);
	console.log('Left selected (value - 数据B):', value);
	console.log('operateLimitData:', props.operateLimitData);

	// 获取完整树数据
	const fullTreeData = await getFullTreeData();

	// Build node map and parent-child relationship
	const nodeMap = new Map<string, FlowflexUser>();
	const childToParentMap = new Map<string, string>();

	const buildMaps = (nodes: FlowflexUser[], parentId?: string) => {
		nodes.forEach((node) => {
			nodeMap.set(node.id, node);
			if (parentId) {
				childToParentMap.set(node.id, parentId);
			}
			if (node.children && node.children.length > 0) {
				buildMaps(node.children, node.id);
			}
		});
	};
	buildMaps(fullTreeData);

	// ========== 获取左侧可选数据 A (viewChoosableTreeData) ==========
	// 从 viewChoosableTreeData 中提取所有 ID（只提取 team，过滤掉 user）
	const dataA = new Set<string>();
	const extractIds = (nodes: FlowflexUser[] | undefined) => {
		if (!nodes || nodes.length === 0) return;
		nodes.forEach((node) => {
			// 只添加 team 类型的节点，跳过 user 类型
			if (node.type === 'team') {
				dataA.add(node.id);
			}
			// 递归处理子节点
			if (node.children && node.children.length > 0) {
				extractIds(node.children);
			}
		});
	};

	// 修复：如果 viewChoosableTreeData 是 undefined（表示不限制），使用 fullTreeData
	if (viewChoosableTreeData.value === undefined) {
		console.log('viewChoosableTreeData is undefined, using fullTreeData as 数据A');
		extractIds(fullTreeData);
	} else {
		console.log('viewChoosableTreeData has data, using it as 数据A');
		extractIds(viewChoosableTreeData.value);
	}

	console.log('数据A (left side available):', Array.from(dataA));

	// 如果数据A为空（这种情况不应该发生，除非 fullTreeData 也为空）
	if (dataA.size === 0) {
		console.log('数据A is empty, right side should be empty');
		operateChoosableTreeData.value = [];
		return;
	}

	// ========== 获取左侧已选数据 B ==========
	const dataB = new Set<string>(value as string[]);
	console.log('数据B (left selected):', Array.from(dataB));

	// 如果没有选中数据
	if (dataB.size === 0) {
		if (mode === ViewPermissionModeEnum.VisibleTo) {
			// VisibleTo 模式：没选择，右侧没有可选数据
			operateChoosableTreeData.value = undefined;
			return;
		}
		// InvisibleTo 模式：没选择（黑名单为空），继续处理
	}

	// ========== 第二层过滤：计算数据 C ==========
	let dataC = new Set<string>();

	if (props.isWorkflowLevel) {
		// Workflow 级别：简化逻辑，不使用 operateLimitData
		console.log('=== Workflow Level: Simplified Logic ===');
		if (mode === ViewPermissionModeEnum.VisibleTo) {
			// VisibleTo: C = B（直接使用左侧选中的）
			dataC = dataB;
			console.log('数据C = 数据B (VisibleTo):', Array.from(dataC));

			// 如果没有选中数据，右侧显示全部数据（不限制）
			if (dataC.size === 0) {
				console.log('No data selected, right side shows all data');
				operateChoosableTreeData.value = undefined;
				return;
			}
		} else if (mode === ViewPermissionModeEnum.Public) {
			dataC = dataA;
		} else if (mode === ViewPermissionModeEnum.InvisibleTo) {
			// InvisibleTo: C = A - B（全部数据减去左侧已选）
			dataA.forEach((id) => {
				if (!dataB.has(id)) {
					dataC.add(id);
				}
			});
			console.log('数据C = A - B (InvisibleTo):', Array.from(dataC));
		}
	} else {
		// Stage 级别：使用 operateLimitData 进行额外过滤
		const operateLimitData = new Set<string>(props.operateLimitData || []);
		console.log('operateLimitData:', Array.from(operateLimitData));

		if (mode === ViewPermissionModeEnum.VisibleTo) {
			// VisibleTo: C = B ∩ operateLimitData
			console.log('=== VisibleTo Mode: C = B ∩ operateLimitData ===');

			if (operateLimitData.size === 0) {
				// 如果没有操作限制数据，直接使用左侧选中的数据
				dataC = dataB;
				console.log('No operateLimitData, using dataB directly:', Array.from(dataC));
			} else {
				// 计算交集
				dataB.forEach((id) => {
					if (operateLimitData.has(id)) {
						dataC.add(id);
					}
				});
				console.log('数据C (B ∩ operateLimitData):', Array.from(dataC));

				// 如果没有交集且左侧有选择，右侧显示空（保持一致性）
				if (dataC.size === 0 && dataB.size > 0) {
					console.log('No intersection but dataB has selections, right side shows empty');
					operateChoosableTreeData.value = [];
					return;
				}

				// 如果左侧没有选择，右侧显示 operateLimitData 的数据
				if (dataB.size === 0 && operateLimitData.size > 0) {
					console.log('No left selection, right side shows operateLimitData');
					dataC = operateLimitData;
				}
			}
		} else if (mode === ViewPermissionModeEnum.Public) {
			// Public 模式：C = operateLimitData
			dataC = operateLimitData;
		} else if (mode === ViewPermissionModeEnum.InvisibleTo) {
			// InvisibleTo 黑名单模式：C = (A - B) ∩ operateLimitData
			console.log('=== InvisibleTo Mode: C = (A - B) ∩ operateLimitData ===');

			// 先计算 A - B（从可见数据中排除左侧选中的）
			const aMinusB = new Set<string>();
			dataA.forEach((id) => {
				if (!dataB.has(id)) {
					aMinusB.add(id);
				}
			});
			console.log('A - B:', Array.from(aMinusB));

			if (operateLimitData.size === 0) {
				// 如果没有操作限制数据，直接使用 A - B
				dataC = aMinusB;
				console.log('No operateLimitData, using A - B directly:', Array.from(dataC));
			} else {
				// 再计算 (A - B) ∩ operateLimitData
				aMinusB.forEach((id) => {
					if (operateLimitData.has(id)) {
						dataC.add(id);
					}
				});
				console.log('数据C ((A - B) ∩ operateLimitData):', Array.from(dataC));
			}
		}
	}

	// Use dataC as the final base for building tree
	const baseAvailableIds = dataC;

	console.log('=== Building Final Tree Structure ===');
	console.log('Base available IDs for tree building:', Array.from(baseAvailableIds));

	// 构建最终树形结构：避免父子重复（如果父节点已在结果中，子节点不再单独显示）
	const resultIds = new Set<string>();

	baseAvailableIds.forEach((nodeId: string) => {
		const node = nodeMap.get(nodeId);
		if (!node) {
			console.log(`Node ${nodeId} not found in map`);
			return;
		}

		// 检查是否有父节点也在基础可选数据中
		let currentId: string = nodeId;
		let hasSelectedParent = false;

		while (childToParentMap.has(currentId)) {
			const parentId: string = childToParentMap.get(currentId)!;
			if (baseAvailableIds.has(parentId)) {
				console.log(
					`Node ${nodeId} (${node.name}) has parent ${parentId} in base, skipping`
				);
				hasSelectedParent = true;
				break;
			}
			currentId = parentId;
		}

		// 只添加没有父节点在基础可选数据中的节点
		if (!hasSelectedParent) {
			console.log(`Adding node ${nodeId} (${node.name}) to final result`);
			resultIds.add(nodeId);
		}
	});

	// 构建最终结果数组 - 递归过滤子节点
	// 确保父节点的 children 只包含在 baseAvailableIds 中的节点
	const cloneNodeWithFilter = (node: FlowflexUser): FlowflexUser => {
		const newNode: FlowflexUser = { ...node };

		if (newNode.children && newNode.children.length > 0) {
			// 递归克隆子节点，只保留在 baseAvailableIds 中的
			newNode.children = newNode.children
				.filter((child) => baseAvailableIds.has(child.id))
				.map((child) => cloneNodeWithFilter(child));
		}

		return newNode;
	};

	const newTreeData = Array.from(resultIds)
		.map((id: string) => nodeMap.get(id))
		.filter(Boolean)
		.map((node) => cloneNodeWithFilter(node!));

	console.log('Final result tree data (with filtered children):', newTreeData);
	operateChoosableTreeData.value = newTreeData.length > 0 ? newTreeData : [];

	// 清理右侧已选数据：移除不在可选范围内的项
	if (localPermissions.operateTeams.length > 0 && needEditLocalPermissions) {
		const validOperateTeams = localPermissions.operateTeams.filter((teamId) =>
			baseAvailableIds.has(teamId)
		);

		// 如果有数据被过滤掉，更新 operateTeams
		if (validOperateTeams.length !== localPermissions.operateTeams.length) {
			console.log(
				'Removing invalid operate teams:',
				localPermissions.operateTeams.filter((id) => !baseAvailableIds.has(id))
			);
			localPermissions.operateTeams = validOperateTeams;
		}
	}
};

// 统一的数据处理函数
const processPermissionChanges = () => {
	if (isProcessingInternalUpdate.value) return;

	// 先设置标志位，防止内部修改触发 watch
	isProcessingInternalUpdate.value = true;

	// 使用 nextTick 确保在下一个事件循环中处理
	nextTick(() => {
		//处理 viewPermissionMode 的变化
		if (!shouldShowTeamSelector.value) {
			//Public 模式清空
			if (localPermissions.viewTeams.length > 0) {
				localPermissions.viewTeams = [];
			}
		}

		// 处理 operateTeams 的同步
		// InvisibleTo 模式下不同步，因为右侧是左侧的反选

		if (
			localPermissions.useSameTeamForOperate &&
			localPermissions.viewPermissionMode !== ViewPermissionModeEnum.InvisibleTo
		) {
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
			useSameTeamForOperate: localPermissions.useSameTeamForOperate,
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

// 同步外部 v-model 到本地状态，避免始终回退到 Public
watch(
	() => props.modelValue,
	(newVal) => {
		if (!newVal || isProcessingInternalUpdate.value) return;

		const hasChanges =
			localPermissions.viewPermissionMode !== newVal.viewPermissionMode ||
			JSON.stringify(localPermissions.viewTeams) !== JSON.stringify(newVal.viewTeams) ||
			localPermissions.useSameTeamForOperate !== newVal.useSameTeamForOperate ||
			JSON.stringify(localPermissions.operateTeams) !== JSON.stringify(newVal.operateTeams);

		if (hasChanges) {
			isProcessingInternalUpdate.value = true;
			localPermissions.viewPermissionMode =
				newVal.viewPermissionMode ?? ViewPermissionModeEnum.Public;
			localPermissions.viewTeams = [...(newVal.viewTeams || [])];
			localPermissions.useSameTeamForOperate = newVal.useSameTeamForOperate ?? true;
			localPermissions.operateTeams = [...(newVal.operateTeams || [])];

			nextTick(() => {
				isProcessingInternalUpdate.value = false;
				updateViewChoosableTreeData();
			});
		}
	},
	{ deep: true }
);

// 限制数据或工作流可见模式变化时，刷新可选树数据
watch(
	() => props.viewLimitData,
	() => {
		updateViewChoosableTreeData();
	},
	{ deep: true, immediate: true }
);

watch(
	() => props.workFlowViewPermissionMode,
	() => {
		updateViewChoosableTreeData();
	},
	{ immediate: true }
);

watch(
	() => localPermissions.viewPermissionMode,
	() => {
		updateViewChoosableTreeData();
	}
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
