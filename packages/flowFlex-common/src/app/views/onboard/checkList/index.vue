<template>
	<!-- 加载状态 -->
	<checklist-loading v-if="loading" />

	<!-- 主要内容 -->
	<div v-else class="flex h-screen bg-gray-50">
		<!-- 左侧边栏 -->
		<div class="w-64 h-screen">
			<h1
				class="font-semibold text-blue-600 mb-4"
				style="
					font-size: 24px;
					color: var(--primary-500, #2468f2);
					margin: 0;
					font-weight: 700;
					line-height: 40px;
					padding-left: 20px;
					height: 60px;
				"
			>
				Checklists
			</h1>
			<div class="border-gray-200 bg-white rounded-lg">
				<h2 class="p-4 text-lg font-medium text-gray-900 mb-4 p-2 bg-blue-50 rounded">
					Teams
				</h2>
				<div class="p-4 space-y-2">
					<el-button
						v-for="team in teams"
						:key="team.id"
						@click="selectedTeam = team.id"
						:class="[
							'team-button w-full text-left px-3 py-2 rounded-md text-sm transition-colors',
							selectedTeam === team.id
								? 'team-button-active text-blue-900 font-medium'
								: 'text-gray-700 hover:bg-gray-100',
						]"
						:style="
							selectedTeam === team.id
								? 'background: linear-gradient(to right, rgb(196, 181, 253), rgb(191, 219, 254)) !important;'
								: ''
						"
						plain
						class="!justify-start !w-full !border-none !px-3 !py-2"
					>
						{{ team.name }}
					</el-button>
				</div>
			</div>
		</div>

		<!-- 主内容区 -->
		<div class="flex-1 flex flex-col border-gray-200 rounded-lg">
			<!-- 头部 -->
			<div class="p-4" style="padding-top: 0px">
				<div class="flex items-center justify-between mb-6">
					<h1 class="text-xl font-semibold" style="visibility: hidden">Checklists</h1>
					<el-button @click="openCreateDialog" type="primary" size="default" icon="Plus">
						New Checklist
					</el-button>
				</div>
				<div class="bg-blue-50 rounded-lg p-4">
					<div class="flex items-center justify-between mb-3">
						<h2 class="text-lg font-medium text-gray-900">Checklists</h2>
						<InputTag
							v-model="searchTags"
							placeholder="Enter checklist name and press enter"
							style-type="normal"
							:limit="10"
							@change="handleSearchTagsChange"
							style="width: 256px; height: 32px"
							class="rounded-md"
						/>
					</div>
					<p class="text-sm text-gray-600">
						Task checklists for different teams during the onboarding process
					</p>
				</div>
			</div>

			<!-- 检查清单内容 -->
			<div class="flex-1 p-4 bg-gray-50">
				<div class="space-y-4">
					<div
						v-for="checklist in filteredChecklists"
						:key="checklist.id"
						:class="['shadow-sm border-gray-200 rounded-lg bg-white']"
					>
						<div class="p-0">
							<!-- 检查清单头部 - 整个区域可点击 -->
							<div
								class="p-4 cursor-pointer hover:bg-blue-50 transition-colors"
								@click="toggleExpanded(checklist.id)"
							>
								<div class="flex items-center justify-between">
									<div class="flex-1">
										<div class="flex items-center justify-between mb-2">
											<h3 class="text-base font-medium text-gray-900">
												{{ checklist.name }}
											</h3>
											<div class="flex items-center">
												<div
													class="inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 text-foreground mr-2 bg-white"
												>
													{{ checklist.team }}
												</div>
												<div
													class="inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 text-foreground mr-2 bg-white"
												>
													{{
														checklist.totalTasks ||
														(checklist.tasks &&
															checklist.tasks.length) ||
														0
													}}
													items
												</div>
												<svg
													xmlns="http://www.w3.org/2000/svg"
													width="24"
													height="24"
													viewBox="0 0 24 24"
													fill="none"
													stroke="currentColor"
													stroke-width="2"
													stroke-linecap="round"
													stroke-linejoin="round"
													class="lucide lucide-chevron-right h-4 w-4"
												>
													<path d="m9 18 6-6-6-6" />
												</svg>
											</div>
										</div>
										<p class="text-sm text-gray-600 mb-1">
											{{ checklist.description }}
										</p>
										<!-- Assignments 信息 -->
										<div
											v-if="
												checklist.assignments &&
												checklist.assignments.length > 0
											"
											class="flex items-center mt-1 text-xs text-muted-foreground"
										>
											<span>Assignments:</span>
											<div class="ml-2 flex flex-wrap gap-1">
												<div
													v-for="assignment in checklist.assignments"
													:key="`${assignment.workflowId}-${assignment.stageId}`"
													class="inline-flex items-center rounded-full border font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 bg-blue-50 border-blue-200 text-xs px-1 py-0"
													style="color: rgb(29 78 216)"
												>
													{{ getWorkflowNameById(assignment.workflowId) }}
													→ {{ getStageNameById(assignment.stageId) }}
												</div>
											</div>
										</div>
									</div>
								</div>
							</div>

							<!-- 任务部分 -->
							<div
								v-if="expandedChecklists.includes(checklist.id)"
								class="p-4 bg-white border-t border-gray-100 rounded-lg tasks-content"
							>
								<div
									class="flex items-center justify-center"
									v-if="
										!checklist.tasksLoaded ||
										(isDragging && draggingChecklistId === checklist.id)
									"
								>
									{{
										!checklist.tasksLoaded
											? 'Loading tasks...'
											: 'Updating task order...'
									}}
								</div>
								<!-- 任务已加载完成时显示 -->
								<div v-if="checklist.tasksLoaded">
									<div class="flex items-center justify-between mb-4">
										<h4 class="text-sm font-medium text-gray-900">Tasks</h4>
										<div class="flex items-center space-x-2">
											<el-dropdown
												:trigger="['click']"
												@visible-change="
													(visible) => !visible && (activeDropdown = null)
												"
											>
												<button
													class="inline-flex items-center justify-center gap-2 whitespace-nowrap text-sm font-medium ring-offset-background transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 [&_svg]:pointer-events-none [&_svg]:size-4 [&_svg]:shrink-0 hover:bg-accent hover:text-accent-foreground h-9 rounded-md px-3"
													type="button"
													aria-haspopup="menu"
													aria-expanded="false"
													data-state="closed"
												>
													<svg
														xmlns="http://www.w3.org/2000/svg"
														width="24"
														height="24"
														viewBox="0 0 24 24"
														fill="none"
														stroke="currentColor"
														stroke-width="2"
														stroke-linecap="round"
														stroke-linejoin="round"
														class="lucide lucide-ellipsis h-4 w-4"
													>
														<circle cx="12" cy="12" r="1" />
														<circle cx="19" cy="12" r="1" />
														<circle cx="5" cy="12" r="1" />
													</svg>
												</button>
												<template #dropdown>
													<el-dropdown-menu>
														<el-dropdown-item
															@click="editChecklist(checklist)"
															icon="Edit"
														>
															Edit Checklist
														</el-dropdown-item>
														<el-dropdown-item
															@click="
																deleteChecklistItem(checklist.id)
															"
															:disabled="deleteLoading"
															icon="Delete"
														>
															<span v-if="deleteLoading">
																Deleting...
															</span>
															<span v-else>Delete Checklist</span>
														</el-dropdown-item>
														<el-dropdown-item
															@click="exportChecklistItem(checklist)"
															:disabled="exportLoading"
															icon="Download"
															divided
														>
															<span v-if="exportLoading">
																Exporting...
															</span>
															<span v-else>Export to PDF</span>
														</el-dropdown-item>
														<el-dropdown-item
															@click="
																duplicateChecklistItem(checklist)
															"
															:disabled="duplicateLoading"
															icon="CopyDocument"
														>
															<span v-if="duplicateLoading">
																Duplicating...
															</span>
															<span v-else>Duplicate</span>
														</el-dropdown-item>
													</el-dropdown-menu>
												</template>
											</el-dropdown>
											<button
												@click="showAddTaskDialog(checklist)"
												class="inline-flex items-center justify-center gap-2 whitespace-nowrap text-sm font-medium ring-offset-background transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 [&_svg]:pointer-events-none [&_svg]:size-4 [&_svg]:shrink-0 border border-input bg-background hover:bg-accent hover:text-accent-foreground h-9 rounded-md px-3"
											>
												<svg
													xmlns="http://www.w3.org/2000/svg"
													width="24"
													height="24"
													viewBox="0 0 24 24"
													fill="none"
													stroke="currentColor"
													stroke-width="2"
													stroke-linecap="round"
													stroke-linejoin="round"
													class="lucide lucide-plus h-4 w-4"
												>
													<path d="M5 12h14" />
													<path d="M12 5v14" />
												</svg>
											</button>
										</div>
									</div>

									<!-- 添加任务输入框 -->
									<div
										v-if="addingTaskTo === checklist.id"
										class="flex gap-2 mb-4"
									>
										<el-input
											v-model="newTaskText"
											placeholder="New task..."
											@keyup.enter="addTask(checklist.id)"
											size="small"
											class="flex-1"
										/>
										<el-button
											@click="addTask(checklist.id)"
											type="primary"
											size="small"
										>
											Add
										</el-button>
										<el-button @click="cancelAddTask" size="small">
											Cancel
										</el-button>
									</div>

									<!-- 任务列表 -->
									<div v-if="checklist.tasks.length > 0">
										<draggable
											v-model="checklist.tasks"
											item-key="id"
											handle=".drag-handle"
											:animation="300"
											ghost-class="ghost-task"
											class="tasks-draggable"
											@start="onTaskDragStart(checklist.id)"
											@change="onTaskDragChange(checklist.id, $event)"
										>
											<template #item="{ element: task }">
												<div
													class="flex items-center gap-3 p-3 transition-all duration-200 border border-transparent rounded-lg task-item"
													:class="{
														'task-disabled':
															isDragging &&
															draggingChecklistId !== checklist.id,
														'task-sorting':
															isDragging &&
															draggingChecklistId === checklist.id,
													}"
												>
													<!-- 排序图标 -->
													<el-button
														size="small"
														text
														circle
														:icon="GripVertical"
														class="cursor-move drag-handle"
														:class="{
															'drag-disabled':
																isDragging &&
																draggingChecklistId !==
																	checklist.id,
															'drag-sorting':
																isDragging &&
																draggingChecklistId ===
																	checklist.id,
														}"
													/>

													<!-- 正常显示模式 -->
													<template
														v-if="
															!(
																editingTask &&
																editingTask.id === task.id
															)
														"
													>
														<span class="flex-1 text-sm text-gray-900">
															{{ task.name }}
														</span>
														<div class="flex items-center space-x-1">
															<button
																@click="
																	editTask(checklist.id, task)
																"
																class="inline-flex items-center justify-center gap-2 whitespace-nowrap text-sm font-medium ring-offset-background transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 [&_svg]:pointer-events-none [&_svg]:size-4 [&_svg]:shrink-0 hover:bg-accent hover:text-accent-foreground h-9 rounded-md px-3"
															>
																<svg
																	xmlns="http://www.w3.org/2000/svg"
																	width="24"
																	height="24"
																	viewBox="0 0 24 24"
																	fill="none"
																	stroke="currentColor"
																	stroke-width="2"
																	stroke-linecap="round"
																	stroke-linejoin="round"
																	class="lucide lucide-square-pen h-4 w-4"
																>
																	<path
																		d="M12 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"
																	/>
																	<path
																		d="M18.375 2.625a1 1 0 0 1 3 3l-9.013 9.014a2 2 0 0 1-.853.505l-2.873.84a.5.5 0 0 1-.62-.62l.84-2.873a2 2 0 0 1 .506-.852z"
																	/>
																</svg>
															</button>
															<button
																@click="
																	deleteTask(
																		checklist.id,
																		task.id
																	)
																"
																class="inline-flex items-center justify-center gap-2 whitespace-nowrap text-sm font-medium ring-offset-background transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 [&_svg]:pointer-events-none [&_svg]:size-4 [&_svg]:shrink-0 hover:bg-accent hover:text-accent-foreground h-9 rounded-md px-3"
															>
																<svg
																	xmlns="http://www.w3.org/2000/svg"
																	width="24"
																	height="24"
																	viewBox="0 0 24 24"
																	fill="none"
																	stroke="currentColor"
																	stroke-width="2"
																	stroke-linecap="round"
																	stroke-linejoin="round"
																	class="lucide lucide-trash2 h-4 w-4"
																>
																	<path d="M3 6h18" />
																	<path
																		d="M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6"
																	/>
																	<path
																		d="M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"
																	/>
																	<line
																		x1="10"
																		x2="10"
																		y1="11"
																		y2="17"
																	/>
																	<line
																		x1="14"
																		x2="14"
																		y1="11"
																		y2="17"
																	/>
																</svg>
															</button>
														</div>
													</template>

													<!-- 编辑模式 -->
													<template v-else>
														<div class="flex-1 pr-2">
															<el-input
																v-model="taskFormData.name"
																placeholder="Task name"
																size="small"
															/>
														</div>
														<div class="flex items-center gap-1">
															<el-button
																@click="saveTaskEdit"
																type="primary"
																size="small"
																icon="Check"
															/>
															<el-button
																@click="cancelTaskEdit"
																size="small"
																icon="Close"
															/>
														</div>
													</template>
												</div>
											</template>
										</draggable>
									</div>
									<div v-else class="text-center py-8 text-gray-500">
										<p class="text-sm">
											No tasks added yet. Click the + button to add a task.
										</p>
									</div>
								</div>
							</div>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 检查清单对话框 (创建/编辑通用) -->
		<el-dialog
			v-model="showDialog"
			:title="dialogConfig.title"
			width="600px"
			:close-on-click-modal="false"
		>
			<template #header>
				<div>
					<h3 class="text-lg font-medium text-gray-900">{{ dialogConfig.title }}</h3>
					<p class="text-sm text-gray-600 mt-1">{{ dialogConfig.description }}</p>
				</div>
			</template>

			<el-form :model="formData" label-position="top" class="space-y-4">
				<el-form-item label="Checklist Name" required>
					<el-input v-model="formData.name" :placeholder="dialogConfig.namePlaceholder" />
				</el-form-item>

				<el-form-item label="Description">
					<el-input
						v-model="formData.description"
						type="textarea"
						:placeholder="dialogConfig.descriptionPlaceholder"
						:rows="3"
					/>
				</el-form-item>

				<el-form-item label="Team" required>
					<el-select v-model="formData.team" placeholder="Select team" class="w-full">
						<el-option
							v-for="team in availableTeams"
							:key="team"
							:value="team"
							:label="team"
						/>
					</el-select>
				</el-form-item>

				<!-- Workflow & Stage Assignments -->
				<WorkflowAssignments
					ref="workflowAssignmentsRef"
					:assignments="initialAssignments"
					:workflows="workflows"
				/>
			</el-form>

			<template #footer>
				<div class="flex justify-end gap-3">
					<el-button @click="closeDialog">Cancel</el-button>
					<el-button
						@click="submitDialog"
						type="primary"
						:disabled="!formData.name || !formData.team"
						:loading="dialogConfig.loading"
					>
						{{ dialogConfig.submitText }}
					</el-button>
				</div>
			</template>
		</el-dialog>
	</div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, shallowRef, watch, nextTick } from 'vue';
import {
	getChecklists,
	getChecklistTasks,
	createChecklist,
	updateChecklist,
	deleteChecklist,
	createChecklistTask,
	updateChecklistTask,
	deleteChecklistTask,
	duplicateChecklist,
	formatTaskForApi,
	handleApiError,
} from '@/apis/ow/checklist';
import { getWorkflows, getStagesByWorkflow } from '@/apis/ow';
import { useI18n } from '@/hooks/useI18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import ChecklistLoading from './checklist-loading.vue';
import WorkflowAssignments from './components/WorkflowAssignments.vue';
import InputTag from '@/components/global/u-input-tags/index.vue';
import draggable from 'vuedraggable';
import GripVertical from '@assets/svg/workflow/grip-vertical.svg';

// 响应式数据 - 使用shallowRef优化大数组性能
const checklists = shallowRef([]);
const workflows = shallowRef([]);
const stages = shallowRef([]);
const loading = ref(false);
const error = ref(null);

// 任务编辑相关
const editingTask = ref(null);
const editingTaskChecklistId = ref(null);
const taskFormData = ref({
	name: '',
	description: '',
	estimatedMinutes: 0,
	isRequired: false,
});

const availableTeams = [
	'Sales',
	'IT',
	'Billing',
	'Implementation Team',
	'WISE Support',
	'Accounting',
];

// 团队列表
const teams = ref([
	{ id: 'all', name: 'All' },
	{ id: 'sales', name: 'Sales' },
	{ id: 'implementation', name: 'Implementation Team' },
	{ id: 'accounting', name: 'Accounting' },
	{ id: 'it', name: 'IT' },
	{ id: 'billing', name: 'Billing' },
	{ id: 'wise-support', name: 'WISE Support' },
]);

// UI状态
const searchTags = ref([]);
const searchQuery = ref('');
const selectedTeam = ref('all');
const expandedChecklists = ref([]);
const activeDropdown = ref(null);
const addingTaskTo = ref(null);
const newTaskText = ref('');

// 分页和虚拟滚动优化
const pageSize = ref(20); // 每页显示的清单数量
const currentPage = ref(1);

// 防抖搜索
const debouncedSearchQuery = ref('');
let searchTimeout = null;

// 监听搜索输入，添加防抖
watch(searchQuery, (newValue) => {
	if (searchTimeout) {
		clearTimeout(searchTimeout);
	}
	searchTimeout = setTimeout(() => {
		debouncedSearchQuery.value = newValue;
		currentPage.value = 1; // 重置到第一页
	}, 300); // 300ms防抖延迟
});

// 统一对话框状态
const showDialog = ref(false);
const dialogMode = ref('create'); // 'create' | 'edit'
const editingChecklist = ref(null);

// 对话框配置 (根据模式动态计算)
const dialogConfig = computed(() => {
	const isEdit = dialogMode.value === 'edit';
	return {
		title: isEdit ? 'Edit Checklist' : 'Create New Checklist',
		description: isEdit
			? 'Update the checklist details'
			: 'Create a new checklist for a specific team in the onboarding process.',
		namePlaceholder: isEdit ? '' : 'Enter checklist name',
		descriptionPlaceholder: isEdit ? '' : 'Enter checklist description',
		submitText: isEdit ? 'Save Changes' : 'Create Checklist',
		loading: isEdit ? editLoading.value : createLoading.value,
	};
});

// 表单数据
const formData = ref({
	name: '',
	description: '',
	team: '',
	workflow: '',
	stage: '',
	assignments: [],
});

// 组件引用
const workflowAssignmentsRef = ref(null);

// 工作流分配数据（用于初始化子组件）
const initialAssignments = ref([]);

// Loading 状态管理
const createLoading = ref(false);
const editLoading = ref(false);
const deleteLoading = ref(false);
const duplicateLoading = ref(false);
const exportLoading = ref(false);
const stagesLoading = ref(false);

const { t } = useI18n();

// 计算属性 - 优化过滤和排序性能
const filteredChecklists = computed(() => {
	const searchTerm = debouncedSearchQuery.value?.toLowerCase() || '';
	const selectedTeamValue = selectedTeam.value;

	const filtered = checklists.value
		.filter((checklist) => {
			// 优化团队匹配逻辑
			const matchesTeam =
				selectedTeamValue === 'all' ||
				checklist.team === selectedTeamValue ||
				checklist.team.toLowerCase().replace(/\s+/g, '-') === selectedTeamValue ||
				// 添加反向匹配：根据selectedTeamValue找到对应的team name进行匹配
				(() => {
					const selectedTeamObj = teams.value.find((t) => t.id === selectedTeamValue);
					return selectedTeamObj && checklist.team === selectedTeamObj.name;
				})();

			// 优化搜索匹配逻辑 - 支持多标签搜索
			if (!searchTerm) return matchesTeam;

			// 将搜索词按逗号分割，支持多标签搜索
			const searchTerms = searchTerm.split(',').map(term => term.trim()).filter(term => term.length > 0);
			
			if (searchTerms.length === 0) return matchesTeam;

			// 使用 OR 逻辑：任何一个搜索词匹配即可
			const hasMatch = searchTerms.some(term => {
				const nameMatch = checklist.name.toLowerCase().includes(term);
				const descMatch = checklist.description?.toLowerCase().includes(term) || false;
				return nameMatch || descMatch;
			});

			return matchesTeam && hasMatch;
		})
		.sort((a, b) => {
			// 缓存日期对象避免重复创建
			const dateA =
				a._sortDate || (a._sortDate = new Date(a.createDate || a.createdAt || 0).getTime());
			const dateB =
				b._sortDate || (b._sortDate = new Date(b.createDate || b.createdAt || 0).getTime());
			return dateA - dateB;
		});

	// 分页优化：只返回当前页的数据
	const startIndex = (currentPage.value - 1) * pageSize.value;
	const endIndex = startIndex + pageSize.value;
	const result = filtered.slice(startIndex, endIndex);

	return result;
});

// 过滤活跃的workflow（排除Inactive状态且过期的）
const filteredWorkflows = computed(() => {
	// 返回所有workflows，如果需要过滤可以在这里添加逻辑
	return workflows.value || [];
});

// 根据选择的workflow过滤stages的逻辑已经移到getStagesForAssignment函数中

// 拖拽状态管理
const isDragging = ref(false);
const draggingChecklistId = ref(null);

// vuedraggable 拖拽事件处理
const onTaskDragStart = (checklistId) => {
	draggingChecklistId.value = checklistId;
};

const onTaskDragChange = async (checklistId, event) => {
	// 设置拖拽状态，显示loading效果
	isDragging.value = true;

	// 重新分配order属性
	const checklist = checklists.value.find((c) => c.id === checklistId);
	if (!checklist) {
		isDragging.value = false;
		draggingChecklistId.value = null;
		return;
	}

	const reorderedTasks = checklist.tasks.map((task, index) => ({
		...task,
		order: index,
	}));

	// 更新本地状态
	checklist.tasks = reorderedTasks;

	// 强制触发响应式更新
	checklists.value = [...checklists.value];

	// 创建最小loading时间的Promise
	const minLoadingTime = new Promise((resolve) => setTimeout(resolve, 800));

	try {
		// 更新后端数据 - 为每个任务分配新的顺序号
		const updatePromises = checklist.tasks.map((task, index) => {
			const updatedTask = formatTaskForApi({
				...task,
				checklistId: checklistId,
				order: index,
			});
			return updateChecklistTask(task.id, updatedTask);
		});

		// 等待API调用和最小loading时间
		await Promise.all([Promise.all(updatePromises), minLoadingTime]);
		ElMessage.success('Task order updated successfully');
	} catch (err) {
		ElMessage.warning('Failed to save new order, but changes are visible locally');
		// 即使出错也要等待最小loading时间
		await minLoadingTime;
	} finally {
		// 确保重置拖拽状态
		isDragging.value = false;
		draggingChecklistId.value = null;
	}
};

// 数据加载方法 - 优化性能
const loadChecklists = async () => {
	try {
		loading.value = true;
		error.value = null;
		const response = await getChecklists();
		const checklistData = response.data || response || [];

		// 先设置基础数据，保留已加载的任务数据
		const processedChecklists = checklistData
			.map((checklist) => {
				// 检查是否已经有加载的任务数据
				const existingChecklist = checklists.value.find(c => c.id === checklist.id);
				if (existingChecklist && existingChecklist.tasksLoaded && existingChecklist.tasks?.length > 0) {
					// 保留已加载的任务数据
				return {
					...checklist,
						tasks: existingChecklist.tasks,
						tasksLoaded: true,
					};
				} else {
					// 初始化为空数组
					return {
						...checklist,
						tasks: [], 
						tasksLoaded: false,
					};
				}
			})
			.sort((a, b) => {
				// 按创建时间升序排序（最早的在前面）
				const dateA = new Date(a.createDate || a.createdAt || 0);
				const dateB = new Date(b.createDate || b.createdAt || 0);
				return dateA.getTime() - dateB.getTime();
			});

		// 使用新的数组引用确保响应式更新
		checklists.value = processedChecklists;
	} catch (err) {
		error.value = handleApiError(err);
		console.error('Failed to load checklists:', err);
		// 使用示例数据作为后备
		checklists.value = getSampleData();

		// 默认展开第一个示例清单
		if (checklists.value.length > 0) {
			expandedChecklists.value = [checklists.value[0].id];
		}
	} finally {
		loading.value = false;
	}
};

// 任务加载缓存
const taskLoadingCache = new Map();

// 懒加载单个清单的任务 - 优化版本
const loadChecklistTasks = async (checklistId, forceReload = false) => {
	const checklist = checklists.value.find((c) => c.id === checklistId);
	if (!checklist) {
		console.warn(`Checklist not found: ${checklistId}`);
		return;
	}
	if (checklist.tasksLoaded && !forceReload) {
		return;
	}

	// 如果强制重新加载，清除缓存
	if (forceReload) {
		taskLoadingCache.delete(checklistId);
		checklist.tasksLoaded = false;
	}

	// 防止重复加载
	if (taskLoadingCache.has(checklistId)) {
		return taskLoadingCache.get(checklistId);
	}

	// 立即设置加载状态，避免无限加载
	checklist.tasksLoaded = false;

	const loadPromise = (async () => {
		try {
			console.log(`Loading tasks for checklist: ${checklistId}`);
			
			// 添加超时机制
			const timeoutPromise = new Promise((_, reject) => {
				setTimeout(() => reject(new Error('API request timeout')), 10000);
			});

			const tasks = await Promise.race([getChecklistTasks(checklistId), timeoutPromise]);
			console.log(`Tasks loaded for checklist ${checklistId}:`, tasks);

			const processedTasks = (tasks.data || tasks || []).map((task) => ({
				...task,
				completed: task.isCompleted || task.completed || false,
				estimatedMinutes: task.estimatedHours ? task.estimatedHours * 60 : 0,
			}));

			// 使用Object.assign确保响应式更新
			Object.assign(checklist, {
				tasks: processedTasks,
				tasksLoaded: true,
			});

			// 强制触发响应式更新
			checklists.value = [...checklists.value];
			console.log(`Tasks loaded successfully for checklist ${checklistId}, count: ${processedTasks.length}`);
			return processedTasks;
		} catch (taskError) {
			console.error(`Failed to load tasks for checklist ${checklistId}:`, taskError);

			// 确保即使出错也要设置tasksLoaded为true，避免无限加载
			Object.assign(checklist, {
				tasks: [],
				tasksLoaded: true,
			});

			// 强制触发响应式更新
			checklists.value = [...checklists.value];

			// 显示用户友好的错误消息
			ElMessage.error(`Failed to load tasks: ${taskError.message || 'Unknown error'}`);
			return [];
		} finally {
			// 清理缓存
			taskLoadingCache.delete(checklistId);
			
			// 确保在任何情况下都设置 tasksLoaded 为 true
			if (!checklist.tasksLoaded) {
				console.warn(`Force setting tasksLoaded=true for checklist ${checklistId}`);
				checklist.tasksLoaded = true;
				checklists.value = [...checklists.value];
			}
		}
	})();

	taskLoadingCache.set(checklistId, loadPromise);
	return loadPromise;
};

// 优化的workflow和stage加载逻辑
const loadWorkflowsAndStages = async () => {
	try {
		console.log('开始加载 workflows 和 stages...');

		// 加载workflows
		const workflowResponse = await getWorkflows();

		if (workflowResponse.code === '200') {
			workflows.value = workflowResponse.data || [];
			console.log('Workflows 加载成功:', workflows.value.length, '个');
		} else {
			workflows.value = [];
			console.warn('Workflows 加载失败:', workflowResponse);
			return; // 如果workflows加载失败，直接返回
		}

		// 加载所有workflows的stages，不仅仅是活跃的
		const allWorkflows = workflows.value;

		if (allWorkflows.length === 0) {
			stages.value = [];
			console.log('没有可用的 workflows');
			return;
		}

		// 批量加载stages，限制并发数量
		const batchSize = 3; // 限制并发请求数量
		const stageResponses = [];

		console.log(`开始为 ${allWorkflows.length} 个 workflows 加载 stages...`);

		for (let i = 0; i < allWorkflows.length; i += batchSize) {
			const batch = allWorkflows.slice(i, i + batchSize);
			const batchPromises = batch.map((workflow) =>
				getStagesByWorkflow(workflow.id)
					.then((response) => {
						console.log(
							`Workflow ${workflow.name} (ID: ${workflow.id}) stages 响应:`,
							response
						);
						if (response.code === '200') {
							const stageData = response.data || [];
							console.log(
								`Workflow ${workflow.name} 加载了 ${stageData.length} 个 stages`
							);
							return { workflowId: workflow.id, data: stageData };
						} else {
							console.warn(`Workflow ${workflow.name} stages 加载失败:`, response);
							return { workflowId: workflow.id, data: [] };
						}
					})
					.catch((err) => {
						console.warn(`Failed to load stages for workflow ${workflow.id}:`, err);
						return { workflowId: workflow.id, data: [] };
					})
			);

			const batchResults = await Promise.all(batchPromises);
			stageResponses.push(...batchResults);
		}

		// 合并所有stages，并确保每个stage都有workflowId
		stages.value = stageResponses.reduce((allStages, response) => {
			const stageData = response.data || [];
			// 确保每个stage都有正确的workflowId
			const stagesWithWorkflowId = stageData.map((stage) => ({
				...stage,
				workflowId: stage.workflowId || response.workflowId,
			}));
			return [...allStages, ...stagesWithWorkflowId];
		}, []);

		console.log('Stages 加载完成，总计:', stages.value.length, '个');
		console.log(
			'所有 stages:',
			stages.value.map((s) => ({ id: s.id, name: s.name, workflowId: s.workflowId }))
		);
	} catch (err) {
		console.error('Failed to load workflows and stages:', err);
		workflows.value = [];
		stages.value = [];
	}
};

const getSampleData = () => [];

// UI交互方法
const toggleExpanded = async (checklistId) => {
	const index = expandedChecklists.value.indexOf(checklistId);
	if (index > -1) {
		// 如果当前已展开，则收起
		expandedChecklists.value.splice(index, 1);
	} else {
		// 如果当前未展开，则先收起所有其他的，再展开当前的（保持只有一个展开）
		expandedChecklists.value = [checklistId];

		// 展开时懒加载任务
		try {
			await loadChecklistTasks(checklistId);
		} catch (error) {
			console.error('Failed to load tasks on expand:', error);
			// 确保即使加载失败也设置为已加载，避免无限加载状态
			const checklist = checklists.value.find((c) => c.id === checklistId);
			if (checklist) {
				checklist.tasksLoaded = true;
				checklist.tasks = [];
				checklists.value = [...checklists.value];
			}
		}
	}
};

// Element Plus dropdown handles positioning automatically, so these functions are no longer needed

const showAddTaskDialog = (checklist) => {
	addingTaskTo.value = checklist.id;
	newTaskText.value = '';
};

const cancelAddTask = () => {
	addingTaskTo.value = null;
	newTaskText.value = '';
};

// handleTaskKeyPress is replaced by @keyup.enter in template

// 任务管理方法
const addTask = async (checklistId) => {
	if (!newTaskText.value.trim()) return;

	try {
		const taskData = formatTaskForApi({
			checklistId: checklistId,
			name: newTaskText.value.trim(),
			description: '',
			isRequired: false,
			order: 0,
		});

		await createChecklistTask(taskData);
		ElMessage.success(t('sys.api.operationSuccess'));

		// 重新加载该清单的任务
		const checklist = checklists.value.find((c) => c.id === checklistId);
		if (checklist) {
			const tasks = await getChecklistTasks(checklistId);
			const processedTasks = (tasks.data || tasks || []).map((task) => ({
				...task,
				completed: task.isCompleted || task.completed || false,
				estimatedMinutes: task.estimatedHours ? task.estimatedHours * 60 : 0,
			}));

			// 使用Object.assign确保响应式更新
			Object.assign(checklist, {
				tasks: processedTasks,
				tasksLoaded: true,
			});

			// 强制触发响应式更新
			checklists.value = [...checklists.value];
		}

		cancelAddTask();
	} catch (err) {
		console.error('Failed to create task:', err);
		ElMessage.error(t('sys.api.operationFailed'));
		// 后备方案：本地添加
		const checklist = checklists.value.find((c) => c.id === checklistId);
		if (checklist) {
			checklist.tasks.push({
				id: Date.now(),
				name: newTaskText.value,
				completed: false,
				estimatedMinutes: 0,
			});
			// 强制触发响应式更新
			checklists.value = [...checklists.value];
		}
		cancelAddTask();
	}
};

const deleteTask = async (checklistId, taskId) => {
	try {
		await ElMessageBox.confirm(
			'Are you sure you want to delete this task? This action cannot be undone.',
			'Confirm Deletion',
			{
				confirmButtonText: 'Delete Task',
				cancelButtonText: 'Cancel',
				type: 'warning',
				customClass: 'custom-confirm-dialog',
				confirmButtonClass: 'el-button--danger',
			}
		);
	} catch {
		return; // 用户取消删除
	}

	try {
		await deleteChecklistTask(taskId, true);
		ElMessage.success('Task deleted successfully');

		// 重新加载该清单的任务
		const checklist = checklists.value.find((c) => c.id === checklistId);
		if (checklist) {
			const tasks = await getChecklistTasks(checklistId);
			const processedTasks = (tasks.data || tasks || []).map((task) => ({
				...task,
				completed: task.isCompleted || task.completed || false,
				estimatedMinutes: task.estimatedHours ? task.estimatedHours * 60 : 0,
			}));

			// 使用Object.assign确保响应式更新
			Object.assign(checklist, {
				tasks: processedTasks,
				tasksLoaded: true,
			});

			// 强制触发响应式更新
			checklists.value = [...checklists.value];
		}
	} catch (err) {
		console.error('Failed to delete task:', err);
		ElMessage.error('Failed to delete task');
		// 后备方案：本地删除
		const checklist = checklists.value.find((c) => c.id === checklistId);
		if (checklist) {
			checklist.tasks = checklist.tasks.filter((t) => t.id !== taskId);
			// 强制触发响应式更新
			checklists.value = [...checklists.value];
		}
	}
};

// Workflow和Stage联动处理
const handleWorkflowChange = async () => {
	// 清空当前选择的stage
	formData.value.stage = '';
	// 根据选择的workflow加载对应的stages
	await loadStagesByWorkflow(formData.value.workflow);
};

// handleWorkflowChangeEdit is replaced by handleWorkflowChangeForAssignment function

// 根据workflow加载stages
const loadStagesByWorkflow = async (workflowName) => {
	if (!workflowName) {
		stages.value = [];
		return;
	}

	try {
		stagesLoading.value = true;
		// 根据workflow名称找到对应的workflow ID
		const selectedWorkflow = workflows.value.find((w) => w.name === workflowName);

		if (!selectedWorkflow) {
			stages.value = [];
			return;
		}

		const response = await getStagesByWorkflow(selectedWorkflow.id);

		if (response.code === '200') {
			// 加载当前workflow的stages
			const workflowStages = response.data || [];

			// 确保每个stage都有workflowId属性
			const stagesWithWorkflowId = workflowStages.map((stage) => ({
				...stage,
				workflowId: selectedWorkflow.id,
			}));

			// 过滤出其他workflow的stages，并与当前workflow的stages合并
			const otherWorkflowStages = stages.value.filter(
				(stage) =>
					stage.workflowId &&
					stage.workflowId.toString() !== selectedWorkflow.id.toString()
			);
			stages.value = [...otherWorkflowStages, ...stagesWithWorkflowId];
			console.log(`Loaded ${workflowStages.length} stages for workflow: ${workflowName}`);
		} else {
			console.warn('Failed to load stages, API response code:', response.code);
		}
	} catch (error) {
		console.warn(`Failed to load stages for workflow ${workflowName}:`, error);
	} finally {
		stagesLoading.value = false;
	}
};

// 清单管理方法
const editChecklist = async (checklist) => {
	editingChecklist.value = checklist;

	// 根据ID查找workflow名称
	let workflowName = '';
	if (checklist.workflowId) {
		const workflow = workflows.value.find(
			(w) => w.id.toString() === checklist.workflowId.toString()
		);
		workflowName = workflow ? workflow.name : '';
	}

	// 加载所有assignments需要的stages
	const uniqueWorkflowIds = new Set();

	// 添加单个workflow（如果存在）
	if (workflowName) {
		const workflow = workflows.value.find((w) => w.name === workflowName);
		if (workflow) {
			uniqueWorkflowIds.add(workflow.id.toString());
		}
	}

	// 添加assignments中的workflows
	(checklist.assignments || []).forEach((assignment) => {
		if (assignment.workflowId) {
			uniqueWorkflowIds.add(assignment.workflowId.toString());
		}
	});

	// 为每个唯一的workflow加载stages
	for (const workflowId of uniqueWorkflowIds) {
		const workflow = workflows.value.find((w) => w.id.toString() === workflowId);
		if (workflow) {
			await loadStagesByWorkflow(workflow.name);
		}
	}

	// 现在查找stage名称（stages已经加载）
	let stageName = '';
	if (checklist.stageId) {
		const stage = stages.value.find((s) => s.id.toString() === checklist.stageId.toString());
		stageName = stage ? stage.name : '';
		if (stage) {
			console.log(`Found stage: ${stage.name} for checklist: ${checklist.name}`);
		}
	}

	// 处理assignments，转换为WorkflowAssignments组件需要的格式
	const assignments = (checklist.assignments || [])
		.map((assignment) => ({
			workflowId: assignment.workflowId,
			stageId: assignment.stageId,
		}))
		.filter((assignment) => assignment.workflowId && assignment.stageId);

	console.log(
		`Processed ${assignments.length} valid assignments out of ${
			(checklist.assignments || []).length
		} total assignments`
	);

	// 设置初始化数据给 WorkflowAssignments 组件
	initialAssignments.value = assignments;

	formData.value = {
		name: checklist.name,
		description: checklist.description,
		team: checklist.team,
		workflow: workflowName,
		stage: stageName,
		assignments: [], // 这个字段不再使用，由 WorkflowAssignments 组件管理
	};

	dialogMode.value = 'edit';
	showDialog.value = true;
	activeDropdown.value = null;
};

const deleteChecklistItem = async (checklistId) => {
	try {
		await ElMessageBox.confirm(
			'Are you sure you want to delete this checklist? This action cannot be undone.',
			'Confirm Deletion',
			{
				confirmButtonText: 'Delete Checklist',
				cancelButtonText: 'Cancel',
				type: 'warning',
				customClass: 'custom-confirm-dialog',
				confirmButtonClass: 'el-button--danger',
			}
		);
	} catch {
		return; // 用户取消删除
	}

	deleteLoading.value = true;
	try {
		await deleteChecklist(checklistId, true);
		console.log('Checklist deleted successfully');
		ElMessage.success('Checklist deleted successfully');
		activeDropdown.value = null;

		// 删除成功后立即刷新页面数据
		console.log('Refreshing checklist data after deletion...');
		await loadChecklists();

		// 清空展开状态，避免引用已删除的checklist
		expandedChecklists.value = expandedChecklists.value.filter((id) => id !== checklistId);
	} catch (err) {
		console.error('Failed to delete checklist:', err);

		// 提供更详细的错误信息
		let errorMessage = 'Failed to delete checklist';
		if (err.response?.status === 404) {
			errorMessage = 'Checklist not found or already deleted';
		} else if (err.response?.status === 403) {
			errorMessage = 'You do not have permission to delete this checklist';
		} else if (err.message) {
			errorMessage = `Deletion failed: ${err.message}`;
		}

		ElMessage.error(errorMessage);
		activeDropdown.value = null;

		// 即使删除失败，也刷新一下数据，可能后端已经删除成功了
		console.log('Refreshing checklist data after deletion error...');
		await loadChecklists();

		// 清空展开状态
		expandedChecklists.value = expandedChecklists.value.filter((id) => id !== checklistId);
	} finally {
		deleteLoading.value = false;
	}
};

// 手动复制任务的辅助函数
const copyTasksManually = async (originalChecklist, newChecklistId) => {
	try {
		// 确保原checklist的任务已加载
		let sourceChecklist = checklists.value.find(
			(c) => c.id.toString() === originalChecklist.id.toString()
		);

		// 如果没有找到或任务未加载，先加载任务
		if (
			!sourceChecklist ||
			!sourceChecklist.tasksLoaded ||
			!sourceChecklist.tasks ||
			sourceChecklist.tasks.length === 0
		) {
			console.log('Source checklist tasks not loaded, loading now...');
			console.log('Original checklist ID:', originalChecklist.id);
			console.log(
				'Available checklists:',
				checklists.value.map((c) => ({
					id: c.id,
					name: c.name,
					tasksCount: c.tasks?.length || 0,
				}))
			);
			await loadChecklistTasks(originalChecklist.id);
			sourceChecklist = checklists.value.find(
				(c) => c.id.toString() === originalChecklist.id.toString()
			);
		}

		// 如果仍然没有找到或没有任务，检查是否直接传入了任务数据
		if (!sourceChecklist || !sourceChecklist.tasks || sourceChecklist.tasks.length === 0) {
			// 检查originalChecklist是否直接包含任务数据
			if (originalChecklist.tasks && originalChecklist.tasks.length > 0) {
				console.log('Using tasks from originalChecklist parameter');
				sourceChecklist = originalChecklist;
			} else {
				console.log('No tasks to copy after loading');
				console.log('Source checklist:', sourceChecklist);
				console.log('Original checklist:', originalChecklist);
				return;
			}
		}

		console.log(
			`Copying ${sourceChecklist.tasks.length} tasks to new checklist ${newChecklistId}`
		);

		// 为每个任务创建新的任务
		const taskPromises = sourceChecklist.tasks.map(async (task, index) => {
			const newTaskData = {
				checklistId: newChecklistId,
				name: task.name,
				description: task.description || '',
				isRequired: task.isRequired !== false,
				estimatedHours: task.estimatedHours || 0,
				order: index,
				taskType: task.taskType || 'Standard',
			};

			try {
				const newTask = await createChecklistTask(newTaskData);
				console.log('Created task:', newTask);
				return newTask;
			} catch (taskError) {
				console.error('Failed to create task:', taskError);
				return null;
			}
		});

		await Promise.all(taskPromises);

		// 重新加载新checklist的任务
		await loadChecklistTasks(newChecklistId);
		console.log('Tasks copied successfully');
	} catch (error) {
		console.error('Failed to copy tasks manually:', error);
	}
};

// 生成唯一的复制名称
const generateUniqueName = (baseName) => {
	const existingNames = checklists.value.map((c) => c.name.toLowerCase());
	let counter = 1;
	let newName = `${baseName}-${counter}`;

	// 持续递增直到找到唯一名称
	while (existingNames.includes(newName.toLowerCase())) {
		counter++;
		newName = `${baseName}-${counter}`;
	}

	return newName;
};

const duplicateChecklistItem = async (checklist) => {
	duplicateLoading.value = true;
	try {
		// 确保任务已加载
		if (!checklist.tasksLoaded || !checklist.tasks || checklist.tasks.length === 0) {
			console.log('Loading tasks for checklist before duplication:', checklist.id);
			await loadChecklistTasks(checklist.id);
		}

		// 获取最新的checklist数据（包含任务）
		const updatedChecklist = checklists.value.find((c) => c.id === checklist.id) || checklist;
		console.log('Duplicating checklist with tasks:', updatedChecklist.tasks?.length || 0);

		// 生成唯一的名称，避免重名问题
		const duplicateName = generateUniqueName(checklist.name);

		// 确保参数符合DuplicateChecklistInputDto接口
		const duplicateData = {
			name: duplicateName,
			description: checklist.description || '',
			team: checklist.team || 'Sales', // 确保team不为空
			copyTasks: true,
			setAsTemplate: false,
		};

		console.log('Duplicate request data:', duplicateData);
		console.log('Original checklist ID:', checklist.id);
		console.log('Original checklist data:', checklist);

		const newChecklist = await duplicateChecklist(checklist.id, duplicateData);
		console.log('Duplicate response:', newChecklist);

		ElMessage.success('Checklist duplicated successfully');
		activeDropdown.value = null;

		// 复制成功后立即刷新页面数据
		console.log('Refreshing checklist data after duplication...');
		await loadChecklists();

		// 查找新创建的checklist并展开它
		const newChecklistItem = checklists.value.find((c) => c.name === duplicateName);
		if (newChecklistItem) {
			// 展开新创建的checklist
			expandedChecklists.value = [newChecklistItem.id];
			// 加载任务
			await loadChecklistTasks(newChecklistItem.id);

			// 检查任务是否被成功复制，如果没有则手动复制
			const updatedNewChecklist = checklists.value.find((c) => c.id === newChecklistItem.id);
			if (
				updatedNewChecklist &&
				(!updatedNewChecklist.tasks || updatedNewChecklist.tasks.length === 0)
			) {
				console.log('Tasks were not copied by backend, manually copying tasks...');
				await copyTasksManually(updatedChecklist, newChecklistItem.id);
			}
		}
	} catch (err) {
		console.error('Failed to duplicate checklist:', err);

		// 提供更详细的错误信息
		let errorMessage = 'Failed to duplicate checklist';
		if (err.response?.status === 500) {
			errorMessage = 'Server error occurred. Please try again.';
		} else if (err.response?.status === 404) {
			errorMessage = 'Checklist not found.';
		} else if (err.response?.status === 400) {
			errorMessage = 'Invalid request parameters.';
		} else if (err.message) {
			errorMessage = `Duplication failed: ${err.message}`;
		}

		ElMessage.error(errorMessage);
		activeDropdown.value = null;

		// 即使复制失败，也刷新一下数据，检查是否有新的checklist被创建
		console.log('Refreshing checklist data after duplication error...');
		await loadChecklists();
	} finally {
		duplicateLoading.value = false;
	}
};

// 导出PDF文件功能
const exportChecklistItem = async (checklist) => {
	exportLoading.value = true;
	try {
		// 确保任务已加载
		if (!checklist.tasksLoaded || !checklist.tasks || checklist.tasks.length === 0) {
			await loadChecklistTasks(checklist.id);
		}

		// 直接使用前端生成PDF（后端暂不支持PDF导出）
		await exportPdfWithFrontend(checklist);
	} catch (err) {
		console.error('PDF导出失败:', err);
		ElMessage.error(`PDF export failed: ${err.message || 'Unknown error'}`);
		activeDropdown.value = null;
	} finally {
		exportLoading.value = false;
	}
};

// 前端生成PDF的后备方案
const exportPdfWithFrontend = async (checklist) => {
	try {
		// 动态导入jsPDF库 - 兼容不同版本
		const jsPDFModule = await import('jspdf');

		// 尝试不同的导入方式
		let jsPDF;
		if (jsPDFModule.jsPDF) {
			jsPDF = jsPDFModule.jsPDF;
		} else if (jsPDFModule.default && jsPDFModule.default.jsPDF) {
			jsPDF = jsPDFModule.default.jsPDF;
		} else if (jsPDFModule.default) {
			jsPDF = jsPDFModule.default;
		} else {
			throw new Error('无法找到jsPDF构造函数');
		}

		// 获取最新的checklist数据（包含任务）
		const updatedChecklist = checklists.value.find((c) => c.id === checklist.id) || checklist;

		// 创建PDF实例
		const pdf = new jsPDF({
			orientation: 'portrait',
			unit: 'mm',
			format: 'a4',
		});

		let y = 20;
		const margin = 20;
		const pageWidth = 210; // A4宽度

		// 添加头部背景色和标题
		pdf.setFillColor(52, 71, 103); // 更深的蓝色，匹配设计图
		pdf.rect(0, 0, pageWidth, 30, 'F');

		// 添加白色标题文字
		pdf.setTextColor(255, 255, 255);
		pdf.setFontSize(20);
		pdf.text('UNIS', margin, 20);
		pdf.setFontSize(16);
		pdf.text('Warehousing Solutions', margin + 60, 20);

		// 重置文字颜色为黑色
		pdf.setTextColor(0, 0, 0);
		y = 45;

		// 添加清单名称作为主标题
		pdf.setFontSize(18);
		const checklistName = String(updatedChecklist.name || 'Untitled');
		pdf.text(checklistName, margin, y);
		y += 15;

		// 添加基本信息
		pdf.setFontSize(12);

		const description = String(updatedChecklist.description || 'No description');
		pdf.text(`Description: ${description}`, margin, y);
		y += 8;

		const team = String(updatedChecklist.team || 'No team');
		pdf.text(`Team: ${team}`, margin, y);
		y += 8;

		// 处理 assignments 信息，分行显示以避免字体渲染问题
		const assignmentsText = getAssignmentsForPdf(updatedChecklist);
		pdf.text('Assignments:', margin, y);
		y += 6;

		// 将 assignments 文本分割并逐行显示
		if (assignmentsText && assignmentsText !== 'No assignments specified') {
			const assignmentLines = assignmentsText.split(', ');
			assignmentLines.forEach((line) => {
				// 替换箭头符号为更兼容的符号
				const cleanLine = line.replace(/→/g, ' -> ');
				pdf.text(`  ${cleanLine}`, margin + 5, y);
				y += 5;
			});
		} else {
			pdf.text('  No assignments specified', margin + 5, y);
			y += 5;
		}
		y += 10;

		// 创建任务表格
		const tasks = updatedChecklist.tasks || [];

		if (tasks.length > 0) {
			// 表格头部
			pdf.setFillColor(52, 71, 103); // 与头部保持一致的深蓝色
			pdf.rect(margin, y, pageWidth - 2 * margin, 8, 'F');

			// 表格头部文字 - 两列布局
			pdf.setTextColor(255, 255, 255);
			pdf.setFontSize(12);
			pdf.text('Task', margin + 20, y + 5.5);

			// 绘制表格头部列分隔线
			pdf.setDrawColor(255, 255, 255);
			pdf.setLineWidth(0.1);
			pdf.line(margin + 15, y, margin + 15, y + 8);

			y += 8;
			pdf.setTextColor(0, 0, 0);
			pdf.setFontSize(11);

			// 添加任务行
			tasks.forEach((task, index) => {
				// 检查是否需要新页面
				if (y > 250) {
					pdf.addPage();
					y = 20;

					// 重新添加表格头部
					pdf.setFillColor(52, 71, 103);
					pdf.rect(margin, y, pageWidth - 2 * margin, 8, 'F');
					pdf.setTextColor(255, 255, 255);
					pdf.setFontSize(12);
					pdf.text('Task', margin + 20, y + 5.5);
					pdf.setDrawColor(255, 255, 255);
					pdf.setLineWidth(0.1);
					pdf.line(margin + 15, y, margin + 15, y + 8);
					y += 8;
					pdf.setTextColor(0, 0, 0);
					pdf.setFontSize(11);
				}

				// 绘制表格行背景（交替颜色）
				if (index % 2 === 1) {
					pdf.setFillColor(245, 247, 250); // 更浅的灰色，接近设计图
					pdf.rect(margin, y, pageWidth - 2 * margin, 8, 'F');
				}

				// 绘制表格边框
				pdf.setDrawColor(209, 213, 219); // 更深一点的边框颜色，增强对比度
				pdf.setLineWidth(0.1);

				// 绘制行的边框
				pdf.rect(margin, y, pageWidth - 2 * margin, 8, 'S');

				// 绘制列分隔线
				pdf.line(margin + 15, y, margin + 15, y + 8);

				// 添加序号和任务名称
				const taskName = String(task.name || `Task ${index + 1}`);
				pdf.setTextColor(0, 0, 0);
				pdf.setFontSize(12);
				pdf.text(`${index + 1}`, margin + 6, y + 5.5);
				pdf.text(taskName, margin + 20, y + 5.5);

				y += 8;
			});
		} else {
			// 如果没有任务，显示空状态
			pdf.setFillColor(52, 71, 103);
			pdf.rect(margin, y, pageWidth - 2 * margin, 8, 'F');

			pdf.setTextColor(255, 255, 255);
			pdf.setFontSize(12);
			pdf.text('Task', margin + 20, y + 5.5);

			// 绘制列分隔线
			pdf.setDrawColor(255, 255, 255);
			pdf.setLineWidth(0.1);
			pdf.line(margin + 15, y, margin + 15, y + 8);

			y += 8;

			// 绘制空行边框
			pdf.setDrawColor(209, 213, 219);
			pdf.setLineWidth(0.1);
			pdf.rect(margin, y, pageWidth - 2 * margin, 8, 'S');
			pdf.line(margin + 15, y, margin + 15, y + 8);

			pdf.setTextColor(156, 163, 175); // 灰色文字
			pdf.setFontSize(11);
			pdf.text('No tasks available', margin + 20, y + 5.5);
		}

		// 生成文件名
		const filename = `${checklistName.replace(/[^\w\s-]/g, '_')}.pdf`;

		// 保存PDF
		pdf.save(filename);

		ElMessage.success('PDF exported successfully');
		activeDropdown.value = null;
	} catch (frontendErr) {
		console.error('前端PDF生成失败:', frontendErr);
		console.error('错误详情:', frontendErr.stack);

		// 尝试最简单的方案
		await exportBasicPdf(checklist);
	}
};

// 最基本的PDF生成方案
const exportBasicPdf = async (checklist) => {
	try {
		// 创建纯文本内容
		const updatedChecklist = checklists.value.find((c) => c.id === checklist.id) || checklist;

		let content = 'UNIS Checklist Export\n\n';
		content += `Name: ${updatedChecklist.name || 'Untitled'}\n`;
		content += `Description: ${updatedChecklist.description || 'No description'}\n`;
		content += `Team: ${updatedChecklist.team || 'No team'}\n`;
		content += `Assignments: ${getAssignmentsForPdf(updatedChecklist)}\n\n`;
		content += 'Tasks:\n';

		const tasks = updatedChecklist.tasks || [];
		if (tasks.length > 0) {
			tasks.forEach((task, index) => {
				content += `${index + 1}. ${task.name || `Task ${index + 1}`}\n`;
			});
		} else {
			content += 'No tasks available\n';
		}

		// 创建文本文件作为后备
		const blob = new Blob([content], { type: 'text/plain;charset=utf-8' });
		const url = URL.createObjectURL(blob);

		const link = document.createElement('a');
		link.href = url;
		link.download = `${(checklist.name || 'checklist').replace(/[^\w\s-]/g, '_')}.txt`;
		link.style.display = 'none';

		document.body.appendChild(link);
		link.click();

		setTimeout(() => {
			document.body.removeChild(link);
			URL.revokeObjectURL(url);
		}, 100);

		ElMessage.info('PDF generation failed, exported as text file instead');
		activeDropdown.value = null;
	} catch (basicErr) {
		console.error('基本导出也失败:', basicErr);

		// 最后的后备方案：打印
		await exportWithPrint(checklist);
	}
};

// 打印方案（最后的后备）
const exportWithPrint = async (checklist) => {
	try {
		// 获取最新的checklist数据（包含任务）
		const updatedChecklist = checklists.value.find((c) => c.id === checklist.id) || checklist;

		// 创建打印窗口
		const printWindow = window.open('', '_blank');
		if (!printWindow) {
			throw new Error('Unable to open print window. Please check popup settings.');
		}

		// 生成PDF内容
		const pdfContent = createPdfContent(updatedChecklist);

		// 写入打印窗口
		printWindow.document.write(pdfContent);
		printWindow.document.close();

		// 等待内容加载完成
		printWindow.onload = () => {
			setTimeout(() => {
				printWindow.print();
				printWindow.close();
			}, 500);
		};

		ElMessage.info('Print dialog opened. You can save as PDF from the print dialog.');
		activeDropdown.value = null;
	} catch (printErr) {
		console.error('打印方案也失败:', printErr);
		throw new Error('All export methods failed');
	}
};

// PDF导出辅助函数
// 获取 assignments 信息用于 PDF 导出
const getAssignmentsForPdf = (checklist) => {
	if (!checklist.assignments || checklist.assignments.length === 0) {
		return 'No assignments specified';
	}

	const assignmentTexts = checklist.assignments.map((assignment) => {
		const workflowName = getWorkflowNameById(assignment.workflowId);
		const stageName = getStageNameById(assignment.stageId);
		return `${workflowName} → ${stageName}`;
	});

	return assignmentTexts.join(', ');
};

// 创建PDF内容的函数
const createPdfContent = (checklist) => {
	const tasks = checklist.tasks || [];
	console.log('PDF Export - Checklist:', checklist);
	console.log('PDF Export - Tasks:', tasks);

	const tasksHtml =
		tasks.length > 0
			? tasks
					.map(
						(task, index) => `
			<tr>
				<td class="task-cell">${index + 1}</td>
				<td class="task-cell">${task.name || `Task ${index + 1}`}</td>
			</tr>
		`
					)
					.join('')
			: `
			<tr>
				<td class="task-cell" colspan="2" style="text-align: center; color: #9ca3af; font-style: italic;">
					No tasks available
				</td>
			</tr>
		`;

	return `
		<!DOCTYPE html>
		<html>
		<head>
			<meta charset="utf-8">
			<title>${checklist.name} - Checklist</title>
			<style>
				@page {
					size: A4;
					margin: 0;
				}
				
				* {
					margin: 0;
					padding: 0;
					box-sizing: border-box;
				}
				
				body {
					font-family: Arial, sans-serif;
					background: white;
					color: #333;
					line-height: 1.4;
				}
				
				.pdf-container {
					width: 210mm;
					min-height: 297mm;
					padding: 15mm;
					background: white;
				}
				
				.header {
					background: #3b4d66;
					color: white;
					padding: 15px 20px;
					margin: -15mm -15mm 20px -15mm;
					display: flex;
					justify-content: space-between;
					align-items: center;
				}
				
				.header-left {
					font-size: 24px;
					font-weight: bold;
				}
				
				.header-right {
					font-size: 18px;
				}
				
				.title {
					font-size: 24px;
					color: #1f2937;
					margin: 0 0 20px 0;
					font-weight: bold;
				}
				
				.info-section {
					margin-bottom: 25px;
				}
				
				.info-item {
					margin: 6px 0;
					font-size: 14px;
					color: #374151;
				}
				
				.info-label {
					font-weight: bold;
				}
				
				.tasks-table {
					width: 100%;
					border-collapse: collapse;
					margin-top: 15px;
					border: 1px solid #e5e7eb;
				}
				
				.table-header {
					background: #3b4d66;
					color: white;
				}
				
				.header-cell {
					padding: 10px 8px;
					text-align: left;
					font-size: 14px;
					font-weight: bold;
				}
				
				.header-cell:first-child {
					width: 50px;
				}
				
				.task-cell {
					padding: 8px;
					border-bottom: 1px solid #e5e7eb;
					font-size: 12px;
					color: #374151;
				}
				
				@media print {
					body {
						-webkit-print-color-adjust: exact;
						print-color-adjust: exact;
					}
					
					.pdf-container {
						margin: 0;
						padding: 15mm;
					}
				}
			</style>
		</head>
		<body>
			<div class="pdf-container">
				<!-- 头部 -->
				<div class="header">
					<div class="header-left">UNIS</div>
					<div class="header-right">Warehousing Solutions</div>
				</div>

				<!-- 标题 -->
				<h1 class="title">${checklist.name}</h1>

				<!-- 基本信息 -->
				<div class="info-section">
					<div class="info-item">
						<span class="info-label">Description:</span> ${checklist.description || 'No description'}
					</div>
					<div class="info-item">
						<span class="info-label">Team:</span> ${checklist.team || 'No team specified'}
					</div>
					<div class="info-item">
						<span class="info-label">Assignments:</span> ${getAssignmentsForPdf(checklist)}
					</div>
				</div>

				<!-- 任务表格 -->
				<table class="tasks-table">
					<thead class="table-header">
						<tr>
							<th class="header-cell" style="width: 50px;">Status</th>
							<th class="header-cell">Task</th>
						</tr>
					</thead>
					<tbody>
						${tasksHtml}
					</tbody>
				</table>
			</div>
		</body>
		</html>
	`;
};

// 对话框管理方法
// 打开创建对话框并设置默认值
const openCreateDialog = async () => {
	dialogMode.value = 'create';
	editingChecklist.value = null;
	showDialog.value = true;

	// 重置初始化数据
	initialAssignments.value = [];

	// 重置表单数据
	formData.value = {
		name: '',
		description: '',
		team: '',
		workflow: '',
		stage: '',
		assignments: [],
	};

	// 等待下一个 tick，确保 WorkflowAssignments 组件已经渲染
	await nextTick();
	
	// 清空 WorkflowAssignments 组件的数据
	if (workflowAssignmentsRef.value?.clearAssignments) {
		workflowAssignmentsRef.value.clearAssignments();
	}

	// 设置默认workflow（只在活跃的workflow中查找）
	const defaultWorkflow = filteredWorkflows.value.find((w) => w.isDefault);
	if (defaultWorkflow) {
		formData.value.workflow = defaultWorkflow.name;
		// 触发workflow变化处理
		await handleWorkflowChange();
	}
};

// 统一关闭对话框
const closeDialog = () => {
	showDialog.value = false;
	editingChecklist.value = null;
	initialAssignments.value = [];
	
	// 清空 WorkflowAssignments 组件的数据
	if (workflowAssignmentsRef.value?.clearAssignments) {
		workflowAssignmentsRef.value.clearAssignments();
	}
	
	formData.value = {
		name: '',
		description: '',
		team: '',
		workflow: '',
		stage: '',
		assignments: [],
	};
};

// 统一的对话框提交处理
const submitDialog = async () => {
	if (!formData.value.name.trim() || !formData.value.team) return;

	const isEdit = dialogMode.value === 'edit';

	if (isEdit) {
		editLoading.value = true;
	} else {
		createLoading.value = true;
	}

	try {
		console.log(`${isEdit ? 'Updating' : 'Creating'} checklist with data:`, formData.value);

		// 从 WorkflowAssignments 组件获取 assignments 数据
		const assignments = workflowAssignmentsRef.value?.getAssignments() || [];

		const checklistData = {
			name: formData.value.name.trim(),
			description: formData.value.description || '',
			team: formData.value.team,
			type: isEdit ? editingChecklist.value?.type || 'Instance' : 'Instance',
			status: isEdit ? editingChecklist.value?.status || 'Active' : 'Active',
			isTemplate: isEdit ? editingChecklist.value?.isTemplate || false : false,
			isActive: isEdit ? editingChecklist.value?.isActive !== false : true,
			assignments: assignments,
		};

		if (isEdit) {
			const originalChecklistId = editingChecklist.value.id;
			await updateChecklist(originalChecklistId, checklistData);
			console.log('Checklist updated successfully');
			ElMessage.success('Checklist updated successfully');

			// 如果编辑的checklist当前是展开状态，保持展开并强制重新加载任务
			if (expandedChecklists.value.includes(originalChecklistId)) {
				console.log('Force reloading tasks for updated checklist:', originalChecklistId);
				await loadChecklistTasks(originalChecklistId, true);
			}
		} else {
			const newChecklist = await createChecklist(checklistData);
			console.log('Checklist created successfully:', newChecklist);
			ElMessage.success(t('sys.api.operationSuccess'));
		}

		closeDialog();

		// 成功后刷新页面数据，但保留已展开和已加载任务的清单
		console.log(`Refreshing checklist data after ${isEdit ? 'update' : 'creation'}...`);
		const currentExpandedIds = [...expandedChecklists.value];
		const taskLoadedChecklists = new Map();
		
		// 保存已加载任务的清单
		checklists.value.forEach((checklist) => {
			if (checklist.tasksLoaded && checklist.tasks?.length > 0) {
				taskLoadedChecklists.set(checklist.id, {
					tasks: checklist.tasks,
					tasksLoaded: true
				});
			}
		});
		
		await loadChecklists();
		
		// 恢复展开状态和任务数据
		expandedChecklists.value = currentExpandedIds;
		checklists.value.forEach((checklist) => {
			const savedData = taskLoadedChecklists.get(checklist.id);
			if (savedData) {
				Object.assign(checklist, savedData);
			}
		});
	} catch (err) {
		console.error(`Failed to ${isEdit ? 'update' : 'create'} checklist:`, err);

		// 提供更详细的错误信息
		let errorMessage = `Failed to ${isEdit ? 'update' : 'create'} checklist`;
		if (err.response?.status === 404) {
			errorMessage = 'Checklist not found';
		} else if (err.response?.status === 403) {
			errorMessage = `You do not have permission to ${
				isEdit ? 'update' : 'create'
			} this checklist`;
		} else if (err.response?.status === 400) {
			errorMessage = 'Invalid checklist data';
		} else if (err.message) {
			errorMessage = `${isEdit ? 'Update' : 'Creation'} failed: ${err.message}`;
		}

		ElMessage.error(isEdit ? errorMessage : t('sys.api.operationFailed'));
		closeDialog();

		// 即使失败，也刷新一下数据，可能后端已经成功了
		console.log(`Refreshing checklist data after ${isEdit ? 'update' : 'creation'} error...`);
		await loadChecklists();

		if (isEdit && expandedChecklists.value.includes(editingChecklist.value?.id)) {
			await loadChecklistTasks(editingChecklist.value.id, true);
		}
	} finally {
		if (isEdit) {
			editLoading.value = false;
		} else {
			createLoading.value = false;
		}
	}
};

// closeEditDialog函数已合并到closeDialog中，saveEditChecklist函数已合并到submitDialog中

// 标签变化处理函数
const handleSearchTagsChange = (tags) => {
	searchQuery.value = tags.join(',');
};

// 点击外部关闭下拉菜单
const handleClickOutside = (event) => {
	// 检查点击是否在下拉菜单或触发按钮外部
	const target = event.target;
	const isClickInsideDropdown = target.closest('.dropdown-menu');
	const isClickOnTrigger = target.closest('[data-checklist-id]');

	if (!isClickInsideDropdown && !isClickOnTrigger) {
		activeDropdown.value = null;
	}
};

// 任务编辑方法
const editTask = (checklistId, task) => {
	editingTask.value = task;
	editingTaskChecklistId.value = checklistId;
	taskFormData.value = {
		name: task.name,
		description: task.description || '',
		estimatedMinutes: task.estimatedMinutes || 0,
		isRequired: task.isRequired || false,
	};
};

const cancelTaskEdit = () => {
	editingTask.value = null;
	editingTaskChecklistId.value = null;
	taskFormData.value = {
		name: '',
		description: '',
		estimatedMinutes: 0,
		isRequired: false,
	};
};



const saveTaskEdit = async () => {
	if (!editingTask.value || !editingTaskChecklistId.value) return;

	try {
		const taskData = formatTaskForApi({
			...editingTask.value,
			checklistId: editingTaskChecklistId.value,
			name: taskFormData.value.name,
			description: taskFormData.value.description,
			estimatedMinutes: taskFormData.value.estimatedMinutes,
			isRequired: taskFormData.value.isRequired,
		});

		await updateChecklistTask(editingTask.value.id, taskData);
		ElMessage.success(t('sys.api.operationSuccess'));

		// 重新加载该清单的任务
		const checklist = checklists.value.find((c) => c.id === editingTaskChecklistId.value);
		if (checklist) {
			const tasks = await getChecklistTasks(editingTaskChecklistId.value);
			const processedTasks = (tasks.data || tasks || []).map((task) => ({
				...task,
				completed: task.isCompleted || task.completed || false,
				estimatedMinutes: task.estimatedHours ? task.estimatedHours * 60 : 0,
			}));

			// 使用Object.assign确保响应式更新
			Object.assign(checklist, {
				tasks: processedTasks,
				tasksLoaded: true,
			});

			// 强制触发响应式更新
			checklists.value = [...checklists.value];
		}

		cancelTaskEdit();
	} catch (err) {
		console.error('Failed to update task:', err);
		ElMessage.error(t('sys.api.operationFailed'));
		// 后备方案：本地更新
		const checklist = checklists.value.find((c) => c.id === editingTaskChecklistId.value);
		if (checklist && editingTask.value) {
			const taskIndex = checklist.tasks.findIndex((t) => t.id === editingTask.value.id);
			if (taskIndex !== -1) {
				checklist.tasks[taskIndex] = {
					...checklist.tasks[taskIndex],
					name: taskFormData.value.name,
					description: taskFormData.value.description,
					estimatedMinutes: taskFormData.value.estimatedMinutes,
					isRequired: taskFormData.value.isRequired,
				};
				// 强制触发响应式更新
				checklists.value = [...checklists.value];
			}
		}
		cancelTaskEdit();
	}
};

onMounted(async () => {
	document.addEventListener('click', handleClickOutside);
	// 并行加载workflows/stages和checklists，提高加载速度
	await Promise.all([loadWorkflowsAndStages(), loadChecklists()]);
});

onUnmounted(() => {
	// 清理事件监听器
	document.removeEventListener('click', handleClickOutside);

	// 清理搜索防抖定时器
	if (searchTimeout) {
		clearTimeout(searchTimeout);
		searchTimeout = null;
	}

	// 清理任务加载缓存
	taskLoadingCache.clear();
});



// 辅助函数：根据 ID 获取工作流名称
const getWorkflowNameById = (workflowId) => {
	if (!workflowId) return '';
	const workflow = workflows.value.find((w) => w.id.toString() === workflowId.toString());
	if (!workflow) {
		console.log(
			`Workflow not found for ID: ${workflowId}. Available workflows:`,
			workflows.value.map((w) => ({ id: w.id, name: w.name }))
		);
	}
	return workflow ? workflow.name : '--';
};

// 辅助函数：根据 ID 获取阶段名称
const getStageNameById = (stageId) => {
	if (!stageId) return '';
	const stage = stages.value.find((s) => s.id.toString() === stageId.toString());
	if (!stage) {
		console.log(
			`Stage not found for ID: ${stageId}. Available stages:`,
			stages.value.map((s) => ({ id: s.id, name: s.name, workflowId: s.workflowId }))
		);
	}
	return stage ? stage.name : '--';
};
</script>

<style scoped>
/* 自定义样式 */
.bg-gradient-to-r {
	background: linear-gradient(to right, #e9d5ff, #bfdbfe);
}

/* 团队按钮样式 */
.team-button {
	justify-content: flex-start !important;
	border: none !important;
	padding: 8px 12px !important;
	height: auto !important;
	margin-bottom: 8px !important;
	border-radius: 6px !important;
	font-size: 14px !important;
	transition: all 0.2s ease !important;
}

.team-button:hover {
	background-color: #f3f4f6 !important;
	color: #374151 !important;
}

.team-button-active {
	background: linear-gradient(to right, #dbeafe, #3b82f6) !important;
	color: #1e3a8a !important;
	font-weight: 500 !important;
}

/* 拖拽样式 */
.tasks-draggable {
	display: flex;
	flex-direction: column;
	gap: 4px;
}

.ghost-task {
	opacity: 0.6;
	background: var(--primary-50, #f0f7ff) !important;
	border: 1px dashed var(--primary-500, #2468f2) !important;
	box-shadow: 0 4px 12px rgba(36, 104, 242, 0.15);
	transform: rotate(1deg);
}

.task-item {
	transition: all 0.3s ease;
	background-color: #ffffff;
	border: 1px solid transparent;
	border-radius: 8px;
	box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
	position: relative;
}

.task-item:hover:not(.task-disabled):not(.task-sorting) {
	background-color: #f9fafb !important;
	border-color: #e5e7eb;
	box-shadow: 0 2px 6px rgba(0, 0, 0, 0.15);
	transform: translateY(-1px);
}

/* 拖拽状态样式 */
.task-disabled {
	opacity: 0.6;
	filter: grayscale(0.3);
	/* 移除 pointer-events: none 以避免阻止拖拽 */
}

.task-sorting {
	transition: all 0.3s ease;
	transform: scale(0.98);
	filter: blur(0.5px);
	/* 移除 pointer-events: none 以避免阻止拖拽 */
}

/* 拖拽手柄样式 */
.drag-handle {
	transition: all 0.2s ease;
	cursor: move !important;
	/* 确保始终显示移动光标 */
}

.drag-handle:hover:not(.drag-disabled):not(.drag-sorting) {
	background-color: #e5e7eb !important;
	color: #374151 !important;
}

.drag-disabled {
	cursor: not-allowed !important;
	opacity: 0.5;
}

.drag-sorting {
	/* 保持移动光标，而不是等待光标 */
	cursor: move !important;
	opacity: 0.8;
	animation: pulse-sorting 1.5s infinite;
}

@keyframes pulse-sorting {
	0%,
	100% {
		opacity: 0.7;
	}

	50% {
		opacity: 0.4;
	}
}

/* 任务内容区域样式 */
.tasks-content {
	transition: all 0.3s ease;
}

/* 间距调整 */
.space-y-4 > * + * {
	margin-top: 1rem;
}

.space-y-3 > * + * {
	margin-top: 0.75rem;
}

.space-y-2 > * + * {
	margin-top: 0.5rem;
}

.gap-2 {
	gap: 0.5rem;
}

.gap-3 {
	gap: 0.75rem;
}

/* InputTag组件样式调整 - 保持原有高度和宽度 */
:deep(.bg-blue-50 .layout) {
	min-height: 32px;
	height: 32px;
	border: 1px solid var(--el-border-color, #dcdfe6);
	border-radius: 8px;
	padding: 4px 11px;
	background-color: var(--el-fill-color-blank, #ffffff);
	transition: all var(--el-transition-duration, 0.2s);
	box-shadow: 0 0 0 1px transparent inset;
	font-size: 14px;
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 4px;
}

:deep(.bg-blue-50 .layout:hover) {
	border-color: var(--el-border-color-hover, #c0c4cc);
}

:deep(.bg-blue-50 .layout:focus-within) {
	border-color: var(--primary-500, #409eff);
	box-shadow: 0 0 0 1px var(--primary-500, #409eff) inset !important;
}

:deep(.bg-blue-50 .input-tag) {
	min-width: 100px;
	height: 24px;
	line-height: 24px;
	font-size: 14px;
	color: var(--el-text-color-regular, #606266);
	border: none;
	outline: none;
	background: transparent;
	flex: 1;
	padding: 0;
}

:deep(.bg-blue-50 .input-tag::placeholder) {
	color: var(--el-text-color-placeholder, #a8abb2);
	font-size: 14px;
}

:deep(.bg-blue-50 .label-box) {
	height: 24px;
	margin: 0;
	border-radius: 12px;
	background-color: var(--el-fill-color-light, #f5f7fa);
	border: 1px solid var(--el-border-color-lighter, #e4e7ed);
	display: inline-flex;
	align-items: center;
	padding: 0 8px;
	transition: all 0.2s ease;
}

:deep(.bg-blue-50 .label-title) {
	font-size: 12px;
	padding: 0;
	line-height: 24px;
	color: var(--el-text-color-regular, #606266);
	font-weight: 500;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	max-width: 120px;
}

:deep(.bg-blue-50 .label-close) {
	padding: 0;
	margin-left: 6px;
	color: var(--el-text-color-placeholder, #a8abb2);
	cursor: pointer;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 16px;
	height: 16px;
	border-radius: 50%;
	background: var(--el-fill-color, #f0f2f5);
	transition: all 0.2s ease;
	transform: none;
}

:deep(.bg-blue-50 .label-close:hover) {
	background: var(--el-fill-color-dark, #e6e8eb);
	color: var(--el-text-color-regular, #606266);
}

:deep(.bg-blue-50 .label-close:after) {
	content: '×';
	font-size: 12px;
	line-height: 1;
	font-weight: bold;
}
</style>
