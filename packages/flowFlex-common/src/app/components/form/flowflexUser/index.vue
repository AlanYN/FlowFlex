<template>
	<div class="w-full">
		<!-- 只读模式 -->
		<div v-if="readonly" class="flex items-center">
			<div v-if="selectedItems.length > 0" class="flex items-center">
				<div
					v-for="(item, index) in selectedItems"
					:key="item.id"
					class="inline-flex items-center"
					:style="{
						marginLeft: index > 0 ? '-8px' : '0',
						zIndex: index + 1,
					}"
				>
					<div
						class="w-6 h-6 rounded-full flex items-center justify-center text-white font-semibold text-xs flex-shrink-0 border-2 border-white"
						:style="{ backgroundColor: getAvatarColor(item.name) }"
						:title="
							item.name +
							(showEmail && item.userDetails?.email
								? ` (${item.userDetails.email})`
								: '')
						"
					>
						{{ getInitials(item.name) }}
					</div>
				</div>
			</div>
			<div v-else class="text-gray-400 text-sm">
				{{ computedPlaceholder || 'No users selected' }}
			</div>
		</div>

		<!-- 可编辑模式 -->
		<div
			v-else
			class="w-full h-8 border border-gray-300 rounded-md px-3 py-1.5 bg-white cursor-pointer transition-colors duration-200 flex items-center"
			:class="[disabled ? 'bg-gray-50 cursor-not-allowed' : 'hover:border-gray-400']"
			@click="openModal"
		>
			<!-- 已选择的用户头像 -->
			<div v-if="selectedItems.length > 0" class="flex flex-wrap gap-1.5 items-center flex-1">
				<div v-for="item in selectedItems" :key="item.id" class="relative">
					<div
						class="w-6 h-6 rounded-full flex items-center justify-center text-white font-semibold text-xs relative cursor-pointer transition-transform duration-200 hover:scale-105 group"
						:class="{ 'cursor-not-allowed': disabled }"
						:style="{ backgroundColor: getAvatarColor(item.name) }"
						:title="
							item.name +
							(showEmail && item.userDetails?.email
								? ` (${item.userDetails.email})`
								: '')
						"
					>
						{{ getInitials(item.name) }}
						<div
							v-if="!disabled"
							class="absolute -top-1 -right-1 w-3 h-3 bg-red-500 rounded-full flex items-center justify-center text-white opacity-0 group-hover:opacity-100 transition-opacity duration-200 cursor-pointer border border-white hover:bg-red-600"
							@click.stop="removeSelectedItem(item.id)"
						>
							<el-icon class="text-[8px]"><Close /></el-icon>
						</div>
					</div>
				</div>
			</div>

			<!-- 空状态占位符 -->
			<div
				v-if="selectedItems.length === 0"
				class="text-gray-400 text-sm text-left select-none flex-1"
			>
				{{ computedPlaceholder }}
			</div>
		</div>

		<!-- 选择弹窗 -->
		<el-dialog
			v-model="modalVisible"
			title="Select Users"
			:width="800"
			:before-close="handleModalClose"
			append-to-body
		>
			<div>
				<!-- 左右分栏布局 -->
				<div class="flex gap-5 h-[450px]">
					<!-- 左侧：可选用户 -->
					<div
						class="flex-1 flex flex-col border border-gray-200 rounded-md overflow-hidden"
					>
						<div
							class="bg-gray-50 px-4 py-3 border-b border-gray-200 flex items-center gap-2"
						>
							<span class="font-semibold text-gray-800 text-sm">
								{{
									props.selectionType === 'user'
										? 'Available Items'
										: 'Available Teams'
								}}
							</span>
							<span
								class="bg-blue-500 text-white text-xs px-2 py-0.5 rounded-full min-w-[20px] text-center"
							>
								{{ getAvailableUsersCount() }}
							</span>
						</div>
						<!-- 搜索框 -->
						<div class="p-3 border-b border-gray-200">
							<el-input
								v-model="searchText"
								placeholder="Search..."
								:prefix-icon="Search"
								clearable
								size="small"
								@input="handleSearch"
							/>
						</div>
						<el-scrollbar class="flex-1">
							<div class="p-2">
								<el-tree
									ref="treeRef"
									:data="treeData"
									:props="treeProps"
									:show-checkbox="true"
									:check-strictly="props.maxCount === 1"
									:filter-node-method="filterNode"
									:default-checked-keys="defaultCheckedKeys"
									node-key="id"
									class="bg-transparent"
									@check="handleTreeCheck"
								>
									<template #default="{ data }">
										<div class="w-full">
											<div class="flex items-center space-x-2">
												<div
													v-if="data.type === 'user'"
													class="w-6 h-6 rounded-full flex items-center justify-center text-white font-semibold text-xs flex-shrink-0"
													:style="{
														backgroundColor: getAvatarColor(data.name),
													}"
												>
													{{ getInitials(data.name) }}
												</div>
												<el-icon
													v-else-if="data.type === 'team'"
													class="text-blue-500"
												>
													<UserFilled />
												</el-icon>
												<el-icon v-else class="text-gray-400">
													<Folder />
												</el-icon>

												<div class="flex-1 min-w-0">
													<div
														class="font-medium text-gray-800 block overflow-hidden text-ellipsis whitespace-nowrap"
														:title="data.name"
													>
														{{ data.name }}
													</div>
													<div
														v-if="
															data.type === 'user' &&
															data.userDetails?.email &&
															showEmail
														"
														class="text-gray-500 text-xs block overflow-hidden text-ellipsis whitespace-nowrap mt-0.5"
														:title="data.userDetails.email"
													>
														{{ data.userDetails.email }}
													</div>
												</div>

												<span
													v-if="data.type === 'team' && data.memberCount"
													class="text-gray-500 text-xs flex-shrink-0"
												>
													({{ data.memberCount }})
												</span>
											</div>
										</div>
									</template>
								</el-tree>
							</div>
						</el-scrollbar>
					</div>

					<!-- 右侧：已选用户 -->
					<div
						class="flex-1 flex flex-col border border-gray-200 rounded-md overflow-hidden"
					>
						<div
							class="bg-gray-50 px-4 py-3 border-b border-gray-200 flex items-center gap-2"
						>
							<span class="font-semibold text-gray-800 text-sm">
								Selected {{ getSelectedTypeText() }}
							</span>
							<span
								class="bg-blue-500 text-white text-xs px-2 py-0.5 rounded-full min-w-[20px] text-center"
							>
								{{ tempSelectedItems.length }}
							</span>
							<span v-if="maxCount > 0" class="text-gray-500 text-xs">
								/ {{ maxCount }}
							</span>
						</div>
						<el-scrollbar class="flex-1">
							<div class="p-2">
								<div
									v-if="tempSelectedItems.length === 0"
									class="flex items-center justify-center h-full text-gray-400 text-sm"
								>
									No {{ getSelectedTypeText().toLowerCase() }} selected
								</div>
								<div v-else class="flex flex-col gap-2">
									<div
										v-for="item in tempSelectedItems"
										:key="item.id"
										class="flex items-center gap-3 p-3 bg-gray-50 rounded-md transition-colors duration-200 hover:bg-gray-100"
									>
										<div
											class="w-10 h-10 rounded-full flex items-center justify-center text-white font-semibold text-base flex-shrink-0"
											:style="{ backgroundColor: getAvatarColor(item.name) }"
										>
											{{ getInitials(item.name) }}
										</div>
										<div class="flex-1 min-w-0">
											<div
												class="font-medium text-gray-800 overflow-hidden text-ellipsis whitespace-nowrap"
											>
												{{ item.name }}
											</div>
											<div
												v-if="item.userDetails?.email && showEmail"
												class="text-gray-500 text-xs overflow-hidden text-ellipsis whitespace-nowrap mt-0.5"
											>
												{{ item.userDetails.email }}
											</div>
										</div>
										<el-button
											link
											class="text-gray-500 p-1 hover:text-red-500"
											@click="removeFromSelection(item.id)"
										>
											<el-icon><Close /></el-icon>
										</el-button>
									</div>
								</div>
							</div>
						</el-scrollbar>
					</div>
				</div>
			</div>

			<!-- 弹窗底部按钮 -->
			<template #footer>
				<div class="text-right">
					<el-button @click="handleModalClose">Cancel</el-button>
					<el-button type="primary" class="ml-2" @click="confirmSelection">
						Confirm
					</el-button>
				</div>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick, watch } from 'vue';
import { UserFilled, Folder, Search, Close } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import { menuRoles } from '@/stores/modules/menuFunction';
import type { FlowflexUser } from '#/golbal';

interface Props {
	modelValue?: string | string[];
	placeholder?: string;
	disabled?: boolean;
	showEmail?: boolean;
	selectionType?: 'user' | 'team'; // 选择类型：仅用户、仅团队
	maxCount?: number; // 最大选择数量，0表示无限制
	minCount?: number; // 最小选择数量
	readonly?: boolean; // 只读模式，只显示用户不显示输入框样式
}

interface Emits {
	(e: 'update:modelValue', value: string | string[] | undefined): void;
	(
		e: 'change',
		value: string | string[] | undefined,
		selectedData?: FlowflexUser | FlowflexUser[]
	): void;
	(e: 'clear'): void;
	(e: 'modal-open'): void;
	(e: 'modal-close'): void;
}

const props = withDefaults(defineProps<Props>(), {
	modelValue: undefined,
	placeholder: '',
	disabled: false,
	showEmail: true,
	selectionType: 'user',
	maxCount: 0,
	minCount: 0,
	readonly: false,
});

// 计算 placeholder
const computedPlaceholder = computed(() => {
	if (props.placeholder) return props.placeholder;

	return props.selectionType === 'user' ? 'Select users' : 'Select teams';
});

const emit = defineEmits<Emits>();

// 使用 store 进行数据管理
const menuStore = menuRoles();

// 数据相关
const treeData = ref<FlowflexUser[]>([]);
const loading = ref(false);
const userDataMap = ref<Map<string, FlowflexUser>>(new Map());
const searchText = ref('');
const modalVisible = ref(false);
const treeRef = ref();
const isOriginallyArray = ref(false); // 记录原始 modelValue 是否为数组

// 选择相关状态
const selectedItems = ref<FlowflexUser[]>([]);
const tempSelectedItems = ref<FlowflexUser[]>([]);

// 树形配置
const treeProps = {
	value: 'id',
	label: 'name',
	children: 'children',
};

// 计算默认选中的keys
const defaultCheckedKeys = computed(() => {
	return tempSelectedItems.value.map((item) => item.id);
});

// 初始化选中项
const initializeSelectedItems = () => {
	if (!props.modelValue) {
		selectedItems.value = [];
		isOriginallyArray.value = false; // 默认为单选模式
		return;
	}

	// 记录原始值的类型
	isOriginallyArray.value = Array.isArray(props.modelValue);
	const values = Array.isArray(props.modelValue) ? props.modelValue : [props.modelValue];

	console.log('Initializing selected items:', {
		modelValue: props.modelValue,
		values,
		userDataMapSize: userDataMap.value.size,
	});

	selectedItems.value = values
		.map((id) => {
			const user = userDataMap.value.get(id);
			if (!user) {
				console.warn(`User with id ${id} not found in userDataMap`);
			}
			return user;
		})
		.filter(Boolean) as FlowflexUser[];

	console.log('Selected items after initialization:', selectedItems.value);
};

// 构建用户数据映射
const buildUserDataMap = (data: FlowflexUser[], clear = false) => {
	if (clear) {
		userDataMap.value.clear();
	}

	const traverse = (items: FlowflexUser[]) => {
		items.forEach((item) => {
			userDataMap.value.set(item.id, item);
			if (item.children && item.children.length > 0) {
				traverse(item.children);
			}
		});
	};

	traverse(data);
};

// 过滤节点方法，支持搜索
const filterNode = (value: string, data: FlowflexUser) => {
	if (!value) return true;
	const searchValue = value.toLowerCase();
	return (
		data.name.toLowerCase().includes(searchValue) ||
		(data.userDetails?.email && data.userDetails.email.toLowerCase().includes(searchValue))
	);
};

// 搜索处理
const handleSearch = () => {
	if (treeRef.value) {
		treeRef.value.filter(searchText.value);
	}
};

// 打开弹窗
const openModal = () => {
	if (props.disabled || props.readonly) return;

	modalVisible.value = true;
	tempSelectedItems.value = [...selectedItems.value];
	emit('modal-open');

	// 如果没有数据，则加载数据
	if (treeData.value.length === 0) {
		initializeData();
	} else {
		// 设置树的选中状态
		nextTick(() => {
			if (treeRef.value) {
				const checkedKeys = selectedItems.value.map((item) => item.id);
				treeRef.value.setCheckedKeys(checkedKeys);
			}
		});
	}
};

// 关闭弹窗
const handleModalClose = () => {
	modalVisible.value = false;
	tempSelectedItems.value = [];
	searchText.value = '';
	emit('modal-close');
};

// 移除选中项
const removeSelectedItem = (id: string) => {
	if (props.disabled) return;

	selectedItems.value = selectedItems.value.filter((item) => item.id !== id);
	updateModelValue();

	// 如果清空了所有选择，重置类型限制，允许重新选择不同类型
	if (selectedItems.value.length === 0) {
		console.log('All selections cleared from main display, you can now select any type');
	}
};

// 树节点选中事件
const handleTreeCheck = (data: FlowflexUser, checkState: any) => {
	const targetType = props.selectionType;

	// 单选模式下的特殊处理
	if (props.maxCount === 1) {
		// 如果点击的是非目标类型的节点（比如在用户模式下点击团队）
		if (data.type !== targetType) {
			// 阻止选择并提示
			nextTick(() => {
				treeRef.value.setChecked(data.id, false);
			});
			const typeText = targetType === 'user' ? 'user' : 'team';
			ElMessage.warning(
				`In single selection mode, you can only select individual ${typeText}s`
			);
			return;
		}

		// 如果点击的是目标类型，实现单选替换逻辑
		if (checkState.checkedKeys.includes(data.id)) {
			// 选中了新项目，取消其他所有选择
			nextTick(() => {
				treeRef.value.setCheckedKeys([data.id]);
			});
			tempSelectedItems.value = [data];
		} else {
			// 取消选中
			tempSelectedItems.value = [];
		}
		return;
	}

	// 多选模式的原有逻辑
	const checkedNodes = treeRef.value.getCheckedNodes();
	const filteredNodes = checkedNodes.filter((node: FlowflexUser) => node.type === targetType);

	// 检查数量限制
	if (props.maxCount > 0 && filteredNodes.length > props.maxCount) {
		ElMessage.warning(`Maximum ${props.maxCount} items can be selected`);
		// 取消最后选中的目标类型节点
		nextTick(() => {
			if (data.type === targetType) {
				treeRef.value.setChecked(data.id, false);
			}
		});
		return;
	}

	// 只将目标类型的节点设置为临时选中项
	tempSelectedItems.value = filteredNodes;
};

// 从右侧选择区域移除项目
const removeFromSelection = (id: string) => {
	tempSelectedItems.value = tempSelectedItems.value.filter((item) => item.id !== id);
	// 同步更新树的选中状态
	nextTick(() => {
		if (treeRef.value) {
			treeRef.value.setChecked(id, false);
		}
	});

	// 如果清空了所有选择，重置类型限制，允许重新选择不同类型
	if (tempSelectedItems.value.length === 0) {
		// 可以在这里添加一些UI提示表明现在可以选择任意类型
		console.log('All selections cleared, you can now select any type');
	}
};

// 确认选择
const confirmSelection = () => {
	// 检查最小数量限制
	if (props.minCount > 0 && tempSelectedItems.value.length < props.minCount) {
		ElMessage.warning(`At least ${props.minCount} items must be selected`);
		return;
	}

	selectedItems.value = [...tempSelectedItems.value];
	updateModelValue();
	handleModalClose();
};

// 获取可用用户数量
const getAvailableUsersCount = (): number => {
	const countNodes = (nodes: FlowflexUser[]): number => {
		let count = 0;
		nodes.forEach((node) => {
			if (props.selectionType === 'user') {
				// user 模式：计算所有类型的节点
				if (node.type === 'user' || node.type === 'team') {
					count++;
				}
			} else {
				// team 模式：只计算 team 类型的节点
				if (node.type === 'team') {
					count++;
				}
			}
			if (node.children && node.children.length > 0) {
				count += countNodes(node.children);
			}
		});
		return count;
	};
	return countNodes(treeData.value);
};

// 获取当前选择类型的显示文本
const getSelectedTypeText = (): string => {
	// 直接根据组件配置返回文本
	return props.selectionType === 'user' ? 'Users' : 'Teams';
};

// 生成随机头像颜色
const getAvatarColor = (name: string): string => {
	const colors = [
		'#FF6B6B',
		'#4ECDC4',
		'#45B7D1',
		'#96CEB4',
		'#FFEAA7',
		'#DDA0DD',
		'#98D8C8',
		'#F7DC6F',
		'#BB8FCE',
		'#85C1E9',
		'#F8C471',
		'#82E0AA',
		'#F1948A',
		'#85C1E9',
		'#D7BDE2',
	];

	// 使用名字生成一个稳定的索引
	let hash = 0;
	for (let i = 0; i < name.length; i++) {
		hash = name.charCodeAt(i) + ((hash << 5) - hash);
	}
	return colors[Math.abs(hash) % colors.length];
};

// 获取名字首字母
const getInitials = (name: string): string => {
	if (!name) return '';
	const words = name.trim().split(/\s+/);
	if (words.length === 1) {
		return words[0].charAt(0).toUpperCase();
	} else {
		return (words[0].charAt(0) + words[words.length - 1].charAt(0)).toUpperCase();
	}
};

// 更新模型值
const updateModelValue = () => {
	const ids = selectedItems.value.map((item) => item.id);

	let newValue: string | string[] | undefined;

	if (ids.length === 0) {
		// 没有选择时：根据原始类型决定返回值
		newValue = isOriginallyArray.value ? [] : undefined;
	} else if (ids.length === 1) {
		// 选择了一个时：根据原始类型和maxCount决定返回值
		if (isOriginallyArray.value || props.maxCount !== 1) {
			newValue = [ids[0]]; // 返回数组格式
		} else {
			newValue = ids[0]; // 返回字符串格式
		}
	} else {
		// 选择了多个时：总是返回数组
		newValue = ids;
	}

	emit('update:modelValue', newValue);
	emit(
		'change',
		newValue,
		selectedItems.value.length === 1 ? selectedItems.value[0] : selectedItems.value
	);
};

// 清空选择
const handleClear = () => {
	selectedItems.value = [];
	updateModelValue();
	emit('clear');
};

// 初始化加载数据（使用缓存优化版）
const initializeData = async (searchQuery = '') => {
	if (loading.value) return; // 防止重复加载

	try {
		loading.value = true;

		// 使用 store 中的缓存方法
		const data = await menuStore.getFlowflexUserDataWithCache(searchQuery);

		if (data && data.length > 0) {
			// 确保数据结构正确，为没有子节点的项目添加空数组
			const processedData = processTreeData(data);
			treeData.value = processedData;
			buildUserDataMap(processedData, true);

			// 等待下一个 tick 确保数据映射完全建立后再初始化选中项
			await nextTick();

			// 初始化选中项
			initializeSelectedItems();

			// 设置树的选中状态
			if (modalVisible.value) {
				nextTick(() => {
					if (treeRef.value) {
						const checkedKeys = selectedItems.value.map((item) => item.id);
						treeRef.value.setCheckedKeys(checkedKeys);
					}
				});
			}
		} else {
			if (!searchQuery) {
				ElMessage.warning('Failed to load user data');
			}
			treeData.value = [];
		}
	} catch (error) {
		console.error('Failed to initialize user data:', error);
		ElMessage.error('Failed to load user data, please try again later');
		treeData.value = [];
	} finally {
		loading.value = false;
	}
};

// 处理树形数据，确保结构正确
const processTreeData = (data: FlowflexUser[]): FlowflexUser[] => {
	if (!Array.isArray(data)) return [];

	const filterNodesByType = (nodes: FlowflexUser[]): FlowflexUser[] => {
		return nodes
			.map((item) => {
				// 递归处理子节点
				const filteredChildren =
					item.children && Array.isArray(item.children)
						? filterNodesByType(item.children)
						: [];

				// 根据选择类型决定显示逻辑
				if (props.selectionType === 'user') {
					// user 模式：显示所有数据，用户可以选择任何类型
					return {
						...item,
						children: filteredChildren,
					};
				} else {
					// team 模式：只显示 team 类型的数据
					if (item.type === 'team') {
						return {
							...item,
							children: filteredChildren,
						};
					}

					// 如果是容器节点且包含 team 子节点，则保留
					if (filteredChildren.length > 0) {
						return {
							...item,
							children: filteredChildren,
						};
					}

					// 其他情况不显示
					return null;
				}
			})
			.filter(Boolean) as FlowflexUser[];
	};

	return filterNodesByType(data);
};

// 根据ID获取用户名称（用于后期ID转换为名称的功能）
const getUserNameById = (id: string): string => {
	const user = userDataMap.value.get(id);
	return user ? user.name : id;
};

// 根据ID数组获取用户名称数组
const getUserNamesByIds = (ids: string[]): string[] => {
	return ids.map((id) => getUserNameById(id));
};

// 获取用户详细信息
const getUserById = (id: string): FlowflexUser | undefined => {
	return userDataMap.value.get(id);
};

// 获取多个用户详细信息
const getUsersByIds = (ids: string[]): FlowflexUser[] => {
	return ids.map((id) => getUserById(id)).filter(Boolean) as FlowflexUser[];
};

// 监听modelValue变化
const handleModelValueChange = async () => {
	if (userDataMap.value.size > 0) {
		initializeSelectedItems();
	} else if (props.modelValue) {
		// 如果有 modelValue 但没有数据映射，需要先加载数据
		await initializeData();
	}
};

// 组件挂载时初始化
onMounted(async () => {
	// 监听modelValue变化（会自动处理初始值）
	await handleModelValueChange();
});

// 组件卸载时清理
onUnmounted(() => {
	userDataMap.value.clear();
	treeData.value = [];
	searchText.value = '';
	selectedItems.value = [];
	tempSelectedItems.value = [];
	isOriginallyArray.value = false;
});

// 监听props.modelValue变化
watch(() => props.modelValue, handleModelValueChange, { deep: true });

// 暴露方法供外部使用
defineExpose({
	getUserNameById,
	getUserNamesByIds,
	getUserById,
	getUsersByIds,
	refreshData: initializeData,
	clearCache: () => {
		userDataMap.value.clear();
		treeData.value = [];
		searchText.value = '';
		selectedItems.value = [];
		tempSelectedItems.value = [];
		isOriginallyArray.value = false;
	},
	getSelectedData: () => selectedItems.value,
	openModal,
	closeModal: handleModalClose,
	clearSelection: handleClear,
});
</script>

<style>
/* Element Plus 树形组件样式优化 */
.el-tree-node__content {
	height: auto !important;
	min-height: 40px !important;
	padding: 8px 12px !important;
	align-items: flex-center !important;
	line-height: 1.4 !important;
}

.el-tree-node__content:hover {
	background-color: #f9fafb !important;
}

/* 确保树节点内容正确换行 */
.el-tree-node__label {
	width: 100% !important;
	overflow: visible !important;
}

/* 防止内容重叠 */
.el-tree .el-tree-node {
	white-space: normal !important;
}
</style>
