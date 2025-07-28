<template>
	<div class="questionnaire-container">
		<!-- 页面头部 -->
		<div class="page-header rounded-lg p-6 mb-6">
			<div class="flex justify-between items-center">
				<div>
					<h1 class="text-3xl font-bold page-title">Questionnaire Setup</h1>
					<p class="page-subtitle mt-1">
						Create and manage questionnaires for different workflow stages
					</p>
				</div>
				<div class="flex space-x-2">
					<el-button
						type="primary"
						@click="() => handleNewQuestionnaire()"
						class="primary-button"
					>
						<el-icon class="mr-2"><Plus /></el-icon>
						New Questionnaire
					</el-button>
				</div>
			</div>
		</div>

		<!-- 搜索和筛选区域 -->
		<div class="filter-panel rounded-lg shadow-sm p-4 mb-6">
			<div class="grid grid-cols-1 md:grid-cols-3 gap-4">
				<div class="space-y-2">
					<label class="filter-label text-sm font-medium">Search</label>
					<InputTag
						v-model="searchTags"
						placeholder="Enter questionnaire name and press enter"
						style-type="normal"
						:limit="10"
						@change="handleSearchTagsChange"
						class="w-full rounded-md"
					/>
				</div>

				<div class="space-y-2">
					<label class="filter-label text-sm font-medium">Workflow</label>
					<el-select
						v-model="selectedWorkflow"
						placeholder="Select workflow"
						class="w-full filter-select"
						@change="handleWorkflowChange"
					>
						<el-option label="All Workflows" value="all" />
						<el-option
							v-for="workflow in workflows"
							:key="workflow.id"
							:label="workflow.name"
							:value="workflow.id"
						/>
					</el-select>
				</div>

				<div class="space-y-2">
					<label class="filter-label text-sm font-medium">Stage</label>
					<el-select
						v-model="selectedStage"
						placeholder="Select stage"
						class="w-full filter-select"
						:disabled="selectedWorkflow === 'all' || stagesLoading"
						:loading="stagesLoading"
					>
						<el-option label="All Stages" value="all" />
						<el-option
							v-for="stage in workflowStages"
							:key="stage.id"
							:label="stage.name"
							:value="stage.id"
						/>
					</el-select>
				</div>
			</div>
		</div>

		<!-- 主要内容区域 - 滚动容器 -->
		<el-scrollbar ref="scrollbarRef">
			<!-- 加载状态 -->
			<div v-if="loading" class="flex justify-center items-center py-12">
				<el-icon class="animate-spin h-8 w-8 text-primary-500">
					<Loading />
				</el-icon>
				<span class="ml-2 text-primary-600">Loading questionnaires...</span>
			</div>

			<!-- 问卷卡片网格 -->
			<div v-else class="questionnaire-grid">
				<template v-if="filteredQuestionnaires.length > 0">
					<el-card
						v-for="questionnaire in filteredQuestionnaires"
						:key="questionnaire.id"
						class="questionnaire-card overflow-hidden transition-all"
					>
						<!-- 卡片头部 -->
						<template #header>
							<div class="card-header -m-5 p-4">
								<div class="flex items-center justify-between">
									<div class="flex items-center space-x-3">
										<div class="card-icon p-2 rounded-full">
											<el-icon class="h-5 w-5"><Document /></el-icon>
										</div>
										<h3
											class="card-title text-xl font-semibold leading-tight tracking-tight line-clamp-2"
											:title="questionnaire.name"
											style="line-height: 60px"
										>
											{{ questionnaire.name }}
										</h3>
									</div>
									<el-dropdown
										trigger="click"
										@command="(cmd) => handleCommand(cmd, questionnaire)"
									>
										<el-button text class="card-more-btn">
											<el-icon class="h-4 w-4"><More /></el-icon>
										</el-button>
										<template #dropdown>
											<el-dropdown-menu>
												<el-dropdown-item command="edit">
													<el-icon class="mr-2"><Edit /></el-icon>
													Edit
												</el-dropdown-item>
												<el-dropdown-item command="preview">
													<el-icon class="mr-2"><View /></el-icon>
													Preview
												</el-dropdown-item>
												<el-dropdown-item command="duplicate">
													<el-icon class="mr-2"><CopyDocument /></el-icon>
													Duplicate
												</el-dropdown-item>
												<el-dropdown-item
													divided
													command="delete"
													class="text-red-500"
												>
													<el-icon class="mr-2"><Delete /></el-icon>
													Delete
												</el-dropdown-item>
											</el-dropdown-menu>
										</template>
									</el-dropdown>
								</div>
								<p class="text-primary-600 text-sm mt-1.5 truncate h-6">
									{{ questionnaire.description }}
								</p>
							</div>
						</template>

						<!-- 卡片内容 -->
						<div class="">
							<div class="space-y-3">
								<!-- Assignments区域 -->
								<div class="space-y-2">
									<div class="flex items-center text-sm">
										<span class="card-label">Assignments:</span>
									</div>
									<div
										class="flex items-start gap-2 flex-wrap assignments-container"
										style="height: 60px; overflow: hidden"
									>
										<!-- 显示前5个组合的assignments -->
										<span
											class="card-link"
											v-for="assignment in getDisplayedAssignments(
												questionnaire.assignments
											)"
											:key="`${assignment.workflowId}-${assignment.stageId}`"
											:title="`${getWorkflowName(
												assignment.workflowId
											)} → ${getStageName(assignment.stageId)}`"
										>
											<text
												class="w-full overflow-hidden text-ellipsis whitespace-nowrap"
											>
												{{
													`${getWorkflowName(
														assignment.workflowId
													)} → ${getStageName(assignment.stageId)}`
												}}
											</text>
										</span>
										<!-- 显示剩余数量的按钮 -->
										<el-popover
											v-if="
												questionnaire.assignments &&
												getRemainingCount(questionnaire.assignments) > 0
											"
											placement="top"
											:width="400"
											trigger="click"
										>
											<template #reference>
												<span class="card-link-more">
													+{{
														getRemainingCount(questionnaire.assignments)
													}}
												</span>
											</template>
											<div class="popover-content">
												<h4 class="popover-title">More Assignments</h4>
												<div class="popover-tags">
													<span
														class="popover-tag"
														v-for="assignment in getRemainingAssignments(
															questionnaire.assignments
														)"
														:key="`${assignment.workflowId}-${assignment.stageId}`"
														:title="`${getWorkflowName(
															assignment.workflowId
														)} → ${getStageName(assignment.stageId)}`"
													>
														{{
															`${getWorkflowName(
																assignment.workflowId
															)} → ${getStageName(
																assignment.stageId
															)}`
														}}
													</span>
												</div>
											</div>
										</el-popover>
									</div>
								</div>
								<div class="flex items-center justify-between text-sm">
									<span class="card-label">Sections:</span>
									<span class="card-value font-medium">
										{{
											JSON.parse(questionnaire.structureJson).sections.length
										}}
									</span>
								</div>
								<div class="flex items-center justify-between text-sm">
									<span class="card-label">Questions:</span>
									<span class="card-value font-medium">
										{{ questionnaire.totalQuestions }}
									</span>
								</div>
							</div>
						</div>
					</el-card>
				</template>
			</div>

			<!-- 空状态 -->
			<div
				v-if="(!filteredQuestionnaires || filteredQuestionnaires.length === 0) && !loading"
				class="empty-state flex flex-col items-center justify-center py-12 text-center rounded-lg shadow-sm"
			>
				<div class="empty-icon-bg p-4 rounded-full mb-4">
					<el-icon class="h-12 w-12 empty-icon"><Document /></el-icon>
				</div>
				<h3 class="text-lg font-medium empty-title">No questionnaires found</h3>
				<p class="empty-subtitle mt-1 mb-4">
					{{ getEmptyStateMessage() }}
				</p>
				<el-button
					type="primary"
					@click="() => handleNewQuestionnaire()"
					class="primary-button"
				>
					<el-icon class="mr-2"><Plus /></el-icon>
					Create Your First Questionnaire
				</el-button>
			</div>
		</el-scrollbar>

		<!-- 分页组件 - 移出滚动容器 -->
		<div v-if="!loading && pagination.total > 0">
			<CustomerPagination
				:total="pagination.total"
				:limit="pagination.pageSize"
				:page="pagination.pageIndex"
				:background="true"
				@pagination="handleLimitUpdate"
				@update:page="handleCurrentChange"
				@update:limit="handlePageUpdate"
			/>
		</div>

		<!-- 删除确认对话框 -->
		<el-dialog
			v-model="deleteDialogVisible"
			:width="smallDialogWidth"
			:show-close="true"
			custom-class="delete-dialog"
		>
			<template #header>
				<div class="delete-dialog-header">
					<h2 class="delete-dialog-title">Delete Questionnaire</h2>
				</div>
			</template>
			<p class="text-gray-600">
				Are you sure you want to delete
				<strong class="text-red-600">
					"{{
						filteredQuestionnaires.find((q) => q.id === deleteQuestionnaireId)?.name ||
						'this questionnaire'
					}}"
				</strong>
				? This action cannot be undone.
			</p>
			<template #footer>
				<div class="flex justify-end space-x-2">
					<el-button @click="deleteDialogVisible = false" :disabled="deleteLoading">
						Cancel
					</el-button>
					<el-button
						type="danger"
						@click="confirmDeleteQuestionnaire"
						:loading="deleteLoading"
					>
						Delete
					</el-button>
				</div>
			</template>
		</el-dialog>

		<!-- 预览对话框 -->
		<QuestionnairePreview
			v-model:visible="showPreview"
			:questionnaire-id="selectedQuestionnaireId"
			:questionnaire-data="selectedQuestionnaireData"
			:workflows="workflows"
			:all-stages="allStages"
		/>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue';
import { ElMessage } from 'element-plus';
import {
	Plus,
	Edit,
	Delete,
	CopyDocument,
	Document,
	More,
	View,
	Loading,
} from '@element-plus/icons-vue';
import CustomerPagination from '@/components/global/u-pagination/index.vue';
import QuestionnairePreview from './components/QuestionnairePreview.vue';
import { useAdaptiveScrollbar } from '@/hooks/useAdaptiveScrollbar';
import InputTag from '@/components/global/u-input-tags/index.vue';

// 引入问卷相关API接口
import {
	queryQuestionnaires,
	deleteQuestionnaire,
	duplicateQuestionnaire,
} from '@/apis/ow/questionnaire';
// 引入工作流和阶段相关API接口
import { getWorkflows, getStagesByWorkflow, getAllStages } from '@/apis/ow';
import { Questionnaire } from '#/onboard';
import { useRouter } from 'vue-router';
import { smallDialogWidth, defaultStr } from '@/settings/projectSetting';

const router = useRouter();

// 使用自适应滚动条 hook，设置底部留白为 80px（为分页组件预留空间）
const { scrollbarRef } = useAdaptiveScrollbar(70);

// 工作流和阶段数据
const workflows = ref<any[]>([]);
const workflowStages = ref<any[]>([]);

// 响应式数据
const loading = ref(false);
const deleteLoading = ref(false); // 专门的删除loading状态
const stagesLoading = ref(false); // 新增：stages加载状态
const searchTags = ref<string[]>([]);
const searchQuery = ref('');
const selectedWorkflow = ref('all');
const selectedStage = ref('all');
const deleteDialogVisible = ref(false);
const deleteQuestionnaireId = ref<string | null>(null);

// 分页相关状态
const pagination = ref({
	pageIndex: 1,
	pageSize: 15,
	total: 0,
});

// 计算属性 - 由于使用服务端分页，直接返回当前页的数据
const filteredQuestionnaires = ref<Questionnaire[]>([]);

// 预览相关
const showPreview = ref(false);
const selectedQuestionnaireId = ref('');
const selectedQuestionnaireData = ref<any>(null);

// 方法
const getWorkflowName = (workflowId: string) => {
	if (!workflowId || workflowId === '0') return defaultStr;
	const workflow = workflows.value.find((w) => w.id === workflowId);
	return workflow?.name || workflowId;
};

const getStageName = (stageId: string) => {
	if (!stageId || stageId === '0') return defaultStr;
	const stage = allStages.value.find((s) => s.id === stageId);
	return stage ? stage.name : stageId;
};

const getEmptyStateMessage = () => {
	if (searchQuery.value || selectedWorkflow.value !== 'all' || selectedStage.value !== 'all') {
		return 'Try adjusting your filters';
	}
	return 'No questionnaires have been created yet';
};

// 标签变化处理函数
const handleSearchTagsChange = (tags: string[]) => {
	searchQuery.value = tags.join(',');
};

// 获取显示的分配数量（去重）
const getDisplayedAssignments = (assignments: any[]) => {
	const displayedCount = 5; // 显示5个
	if (!assignments || assignments.length === 0) {
		return [];
	}

	// 根据workflowId+stageId组合进行去重
	const uniqueAssignments = assignments.filter((assignment, index, self) => {
		return (
			index ===
			self.findIndex(
				(a) => a.workflowId === assignment.workflowId && a.stageId === assignment.stageId
			)
		);
	});

	// 返回前N个去重后的数据
	return uniqueAssignments.slice(0, displayedCount);
};

// 获取去重后的所有数据
const getUniqueAssignments = (assignments: any[]) => {
	if (!assignments || assignments.length === 0) {
		return [];
	}

	return assignments.filter((assignment, index, self) => {
		return (
			index ===
			self.findIndex(
				(a) => a.workflowId === assignment.workflowId && a.stageId === assignment.stageId
			)
		);
	});
};

// 获取剩余数量（去重后）
const getRemainingCount = (assignments: any[]) => {
	const uniqueAssignments = getUniqueAssignments(assignments);
	return Math.max(0, uniqueAssignments.length - 5); // 调整为3个
};

// 获取剩余的标签（去重后，跳过前5个）
const getRemainingAssignments = (assignments: any[]) => {
	const uniqueAssignments = getUniqueAssignments(assignments);
	return uniqueAssignments.slice(5); // 跳过前5个，返回剩余的
};

// 初始化数据
onMounted(async () => {
	// 只加载工作流和问卷数据，不预加载stages
	await Promise.all([fetchWorkflows(), fetchQuestionnaires(), fetchAllStages()]);
});

// 获取工作流列表
const fetchWorkflows = async () => {
	try {
		console.log('Fetching workflows...');
		const response = await getWorkflows();
		console.log('Workflows response:', response);

		if (response.code === '200') {
			workflows.value = response.data || [];
			console.log('Workflows loaded:', workflows.value);
		} else {
			console.warn('Failed to fetch workflows:', response.msg);
			workflows.value = [];
			ElMessage.error(response.msg || 'Failed to fetch workflows');
		}
	} catch (error) {
		console.error('Error fetching workflows:', error);
		workflows.value = [];
		ElMessage.error('Failed to fetch workflows');
	}
};

// 获取阶段列表 - 修改为根据workflowId获取
const fetchStages = async (workflowId?: string) => {
	if (!workflowId || workflowId === 'all') {
		workflowStages.value = [];
		return;
	}

	try {
		stagesLoading.value = true;
		const response = await getStagesByWorkflow(workflowId);
		if (response.code === '200') {
			workflowStages.value = response.data || [];
		} else {
			workflowStages.value = [];
		}
	} catch (error) {
		workflowStages.value = [];
	} finally {
		stagesLoading.value = false;
	}
};

// 监听workflow变化
const handleWorkflowChange = async (workflowId: string) => {
	// 清空当前选择的stage
	selectedStage.value = 'all';
	// 获取新的stages
	await fetchStages(workflowId);
};

// 监听搜索和筛选条件变化
let searchTimeout: any;
watch([searchQuery, selectedWorkflow, selectedStage], () => {
	// 清除之前的定时器
	if (searchTimeout) {
		clearTimeout(searchTimeout);
	}
	// 设置新的定时器，实现防抖
	searchTimeout = setTimeout(() => {
		// 当筛选条件改变时，重置到第一页并重新获取数据
		fetchQuestionnaires(true);
	}, 300);
});

const allStages = ref<any[]>([]);
const fetchAllStages = async () => {
	const response = await getAllStages();
	if (response.code === '200') {
		allStages.value = response.data || [];
	}
};

// 获取问卷列表
const fetchQuestionnaires = async (resetPage = false) => {
	try {
		loading.value = true;

		// 如果需要重置页码
		if (resetPage) {
			pagination.value.pageIndex = 1;
		}

		// 构建查询参数
		const queryParams: any = {
			pageIndex: pagination.value.pageIndex,
			pageSize: pagination.value.pageSize,
			sortField: 'CreateDate',
			sortDirection: 'desc',
		};

		// 只有当搜索关键词不为空时才添加name参数
		if (searchQuery.value && searchQuery.value.trim() !== '') {
			queryParams.name = searchQuery.value.trim();
		}

		// 只有当workflow不是'all'时才添加workflow参数
		if (selectedWorkflow.value && selectedWorkflow.value !== 'all') {
			queryParams.workflowId = selectedWorkflow.value;
		}

		// 只有当stage不是'all'时才添加stage参数
		if (selectedStage.value && selectedStage.value !== 'all') {
			queryParams.stageId = selectedStage.value;
		}

		const response = await queryQuestionnaires(queryParams);
		if (response.code === '200') {
			// 适配API数据格式
			const data = response.data;
			filteredQuestionnaires.value = data.items;
			pagination.value.total = data.totalCount || 0;
		} else {
			ElMessage.error(response.msg || 'Failed to fetch questionnaires');
		}
	} finally {
		loading.value = false;
	}
};

const handleNewQuestionnaire = (id?: string) => {
	// 跳转到新建问卷页面
	router.push({
		path: '/onboard/createQuestion',
		query: {
			questionnaireId: id || '',
		},
	});
};

const handlePreviewQuestionnaire = async (id: string) => {
	try {
		// 从列表数据中找到对应的问卷
		const questionnaireData = filteredQuestionnaires.value.find((q) => q.id === id);
		if (questionnaireData) {
			// 解析问卷结构
			let structure: any = questionnaireData.structureJson;
			if (typeof structure === 'string') {
				try {
					structure = JSON.parse(structure);
				} catch (error) {
					structure = { sections: [] };
				}
			}

			// 确保structure是对象类型
			if (!structure || typeof structure !== 'object') {
				structure = { sections: [] };
			}

			// 适配数据结构，参考 createQuestion.vue 的处理方式
			const adaptedSections =
				structure?.sections?.map((section: any) => ({
					id: section.id || `section-${Date.now()}-${Math.random()}`,
					title: section.title || 'Untitled Section',
					description: section.description || '',
					// 处理questions字段（API返回的是questions，PreviewContent期望的是items）
					items: (section.questions || section.items || []).map((item: any) => ({
						id: item.id || `question-${Date.now()}-${Math.random()}`,
						type: item.type || 'text',
						question: item.title || item.question || '',
						title: item.title || item.question || '', // 保持兼容性
						description: item.description || '',
						required: item.required ?? true,
						options: item.options || [],
						// 处理 rows 字段：对于网格类型是数组，对于 textarea 是数字
						rows:
							item.type === 'multiple_choice_grid' || item.type === 'checkbox_grid'
								? Array.isArray(item.rows)
									? item.rows
									: []
								: typeof item.rows === 'number'
								? item.rows
								: 3,
						columns: Array.isArray(item.columns) ? item.columns : [],
						requireOneResponsePerRow: item.requireOneResponsePerRow,
						min: typeof item.min === 'number' ? item.min : undefined,
						max: typeof item.max === 'number' ? item.max : undefined,
						minLabel: item.minLabel || '',
						maxLabel: item.maxLabel || '',
						placeholder: item.placeholder || '',
						accept: item.accept || '',
						step: typeof item.step === 'number' ? item.step : undefined,
					})),
					...section,
				})) || [];

			// 构建完整的预览数据
			const enrichedData = {
				...questionnaireData,
				sections: adaptedSections,
			};

			// 直接使用处理好的数据，不需要额外的API调用
			selectedQuestionnaireData.value = enrichedData;
			selectedQuestionnaireId.value = id;
		} else {
			// 如果在列表中找不到，则清空数据，使用ID方式（兼容）
			selectedQuestionnaireData.value = null;
			selectedQuestionnaireId.value = id;
		}
		showPreview.value = true;
	} catch (error) {
		ElMessage.error('Failed to prepare preview data');
	}

	console.log('selectedQuestionnaireData.value:', selectedQuestionnaireData.value);
};

const handleDeleteQuestionnaire = (id: string) => {
	deleteQuestionnaireId.value = id;
	deleteDialogVisible.value = true;
};

const handleDuplicateQuestionnaire = async (id: string) => {
	try {
		const questionnaireToDuplicate = filteredQuestionnaires.value.find((q) => q.id === id);
		if (!questionnaireToDuplicate) {
			ElMessage.error('Questionnaire not found');
			return;
		}

		const duplicateParams = {
			name: `${questionnaireToDuplicate.name} (Copy)`,
			description: questionnaireToDuplicate.description,
			copyStructure: true,
			setAsTemplate: false,
		};

		const response = await duplicateQuestionnaire(id, duplicateParams);
		if (response.code === '200') {
			ElMessage.success('Questionnaire duplicated successfully');
			// 重新获取问卷列表
			await fetchQuestionnaires();
		} else {
			ElMessage.error(response.msg || 'Failed to duplicate questionnaire');
		}
	} catch (error) {
		ElMessage.error('Failed to duplicate questionnaire');
	}
};

const confirmDeleteQuestionnaire = async () => {
	if (!deleteQuestionnaireId.value) return;

	try {
		deleteLoading.value = true;
		const response = await deleteQuestionnaire(deleteQuestionnaireId.value, true);
		if (response.code === '200') {
			ElMessage.success('Questionnaire deleted successfully');
			// 重新获取问卷列表
			deleteLoading.value = false;
			deleteDialogVisible.value = false;
			await fetchQuestionnaires();
		} else {
			ElMessage.error(response.msg || 'Failed to delete questionnaire');
		}
	} finally {
		deleteQuestionnaireId.value = null;
	}
};

const handleCommand = (command: string, questionnaire: any) => {
	switch (command) {
		case 'edit':
			handleNewQuestionnaire(questionnaire.id);
			break;
		case 'preview':
			handlePreviewQuestionnaire(questionnaire.id);
			break;
		case 'duplicate':
			handleDuplicateQuestionnaire(questionnaire.id);
			break;
		case 'delete':
			handleDeleteQuestionnaire(questionnaire.id);
			break;
	}
};

// 分页处理方法
const handleCurrentChange = (page: number) => {
	pagination.value.pageIndex = page;
};

const handlePageUpdate = (pageSize: number) => {
	pagination.value.pageSize = pageSize;
};

const handleLimitUpdate = () => {
	fetchQuestionnaires();
};
</script>

<style scoped lang="scss">
/* 容器样式 */
.questionnaire-container {
	height: 100%;
	display: flex;
	flex-direction: column;
}

/* 滚动条样式 */
:deep(.el-scrollbar__wrap) {
	overflow-x: hidden;
}

:deep(.el-scrollbar__view) {
	padding: 0;
}

/* 页面头部样式 */
.page-header {
	@apply dark:from-primary-600 dark:to-primary-500;
	background: linear-gradient(to right, var(--primary-50), var(--primary-100));
	flex-shrink: 0;
}

.page-title {
	color: var(--primary-500);
	@apply dark:text-white;
}

.page-subtitle {
	color: var(--primary-600);
}

.primary-button {
	background-color: var(--primary-500) !important;
	border-color: var(--primary-500) !important;
	color: white !important;
}

.primary-button:hover {
	background-color: var(--primary-600) !important;
	border-color: var(--primary-600) !important;
}

/* 筛选面板样式 */
.filter-panel {
	@apply bg-white dark:bg-black-400;
	border: 1px solid var(--primary-100);
	@apply dark:border-black-200;
	flex-shrink: 0;
}

.filter-label {
	color: var(--primary-700);
	@apply dark:text-primary-300;
}

/* 问卷卡片样式 */
.questionnaire-card {
	border: 1px solid var(--primary-100);
	@apply dark:border-black-200 dark:bg-black-400;
	transition: all 0.3s ease;
	border-bottom: 6px solid var(--primary-500);
	border-bottom-left-radius: 6px;
	border-bottom-right-radius: 6px;
}

.questionnaire-card:hover {
	border-color: var(--primary-300);
	@apply dark:border-primary-600;
}

.card-header {
	background: linear-gradient(to right, var(--primary-50), var(--primary-100));
	@apply dark:from-primary-600 dark:to-primary-500;
	display: flex;
	flex-direction: column;
	justify-content: space-between;
}

.card-icon {
	background-color: var(--primary-500);
	color: white;
	width: 36px;
	height: 36px;
}

.card-title {
	color: var(--primary-800);
	@apply dark:text-white;
	display: -webkit-box;
	-webkit-line-clamp: 2;
	-webkit-box-orient: vertical;
	overflow: hidden;
	text-overflow: ellipsis;
	word-break: break-word;
}

.card-more-btn {
	color: var(--primary-700);
	@apply dark:text-primary-300;
}

.card-label {
	@apply text-gray-500 dark:text-gray-400 font-medium;
	min-width: 70px;
}

.card-link {
	@apply inline-flex items-center rounded-full border text-xs font-semibold transition-colors bg-primary-50 text-primary-500 border-primary-200 px-2 py-1;
	white-space: nowrap;
	width: calc(100% / 3 - 10px); /* 固定宽度 */
	flex-shrink: 0; /* 防止收缩 */
	padding-right: 8px; /* 增加右边距 */
	background: linear-gradient(to right, rgb(196, 181, 253), rgb(191, 219, 254)) !important;
}

.card-link:hover {
	@apply bg-primary-100 border-primary-300;
}

.card-link-more {
	@apply inline-flex items-center rounded-full border text-xs font-semibold transition-colors bg-primary-50 text-primary-500 border-primary-200 px-2 py-1;
	white-space: nowrap;
	width: 40px; /* 固定宽度 */
	overflow: hidden;
	text-overflow: ellipsis;
	justify-content: center; /* 文本居中 */
	flex-shrink: 0; /* 防止收缩 */
	margin-right: 8px; /* 增加右边距 */
	background: linear-gradient(to right, rgb(196, 181, 253), rgb(191, 219, 254)) !important;
}

.card-link-more:hover {
	@apply bg-primary-100 border-primary-300;
}

/* 响应式优化 */
@media (max-width: 768px) {
	.card-label {
		min-width: 60px;
		font-size: 12px;
	}

	.card-link {
		font-size: 11px;
		padding: 2px 6px;
		width: 120px; /* 小屏幕固定宽度 */
		justify-content: flex-start; /* 确保小屏幕也是左对齐 */
	}

	.card-link-more {
		font-size: 11px;
		padding: 2px 6px;
		width: 35px; /* 小屏幕固定宽度 */
	}

	.popover-tag {
		font-size: 11px;
		padding: 2px 6px;
		width: 120px; /* 小屏幕固定宽度 */
		justify-content: flex-start; /* 确保小屏幕也是左对齐 */
	}

	.popover-title {
		font-size: 12px;
	}
}

.card-value {
	color: var(--primary-700);
	@apply dark:text-primary-300;
}

/* 删除对话框样式 */
:deep(.delete-dialog .el-dialog__header) {
	padding: 20px 20px 0 20px;
	margin-right: 0;
	border-bottom: none;
}

.delete-dialog-header {
	display: flex;
	align-items: center;
	justify-content: flex-start;
}

.delete-dialog-title {
	color: #f56565; /* 红色标题，与删除按钮颜色一致 */
	font-size: 18px;
	font-weight: 600;
	margin: 0;
	@apply dark:text-red-400;
}

.card-action-btn {
	color: var(--primary-600);
	border-color: var(--primary-200);
	@apply dark:text-white dark:border-primary-500;
}

.card-action-btn:hover {
	background-color: var(--primary-100);
	@apply dark:bg-primary-500;
}

/* 空状态样式 */
.empty-state {
	@apply bg-white dark:bg-black-400;
	border: 1px solid var(--primary-100);
	@apply dark:border-black-200;
}

.empty-icon-bg {
	background-color: var(--primary-50);
	@apply dark:bg-primary-800;
}

.empty-icon {
	color: var(--primary-400);
	@apply dark:text-primary-500;
}

.empty-title {
	color: var(--primary-800);
	@apply dark:text-white;
}

.empty-subtitle {
	color: var(--primary-600);
	@apply dark:text-primary-300;
}

/* InputTag组件样式调整 - 优化显示效果 */
:deep(.filter-panel .layout) {
	min-height: 32px;
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

:deep(.filter-panel .layout:hover) {
	border-color: var(--el-border-color-hover, #c0c4cc);
}

:deep(.filter-panel .layout:focus-within) {
	border-color: var(--primary-500, #409eff);
	box-shadow: 0 0 0 1px var(--primary-500, #409eff) inset !important;
}

:deep(.filter-panel .input-tag) {
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

:deep(.filter-panel .input-tag::placeholder) {
	color: var(--el-text-color-placeholder, #a8abb2);
	font-size: 14px;
}

:deep(.filter-panel .label-box) {
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

:deep(.filter-panel .label-title) {
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

:deep(.filter-panel .label-close) {
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

:deep(.filter-panel .label-close:hover) {
	background: var(--el-fill-color-dark, #e6e8eb);
	color: var(--el-text-color-regular, #606266);
}

:deep(.filter-panel .label-close:after) {
	content: '×';
	font-size: 12px;
	line-height: 1;
	font-weight: bold;
}

/* Element Plus 组件样式覆盖 */
:deep(.filter-select .el-input__wrapper) {
	border-color: var(--primary-200);
	@apply dark:border-black-200;
}

:deep(.filter-select .el-input__wrapper:hover) {
	border-color: var(--primary-400);
	@apply dark:border-primary-600;
}

:deep(.filter-select .el-input__wrapper.is-focus) {
	border-color: var(--primary-500);
	@apply dark:border-primary-500;
}

/* 自定义卡片样式 */
:deep(.el-card) {
	/* border-radius removed - using rounded-md class */
	box-shadow: 0 1px 3px 0 rgba(0, 0, 0, 0.1);
	@apply dark:shadow-black-50;
}

:deep(.el-card__header) {
	padding: 0;
	border-bottom: none;
}

:deep(.el-card__body) {
	padding: 16px;
	@apply dark:bg-black-400;
}

/* 移除Element Plus默认的卡片内边距和边框 */
:deep(.el-card .el-card__header) {
	margin: 0;
	padding: 20px;
}

:deep(.el-card .el-card__footer) {
	margin: 0;
	padding: 0 20px 20px 20px;
}

/* 对话框样式 */
:deep(.el-dialog) {
	@apply dark:bg-black-400;
}

:deep(.el-dialog__header) {
	@apply dark:border-black-200;
}

:deep(.el-dialog__title) {
	color: var(--primary-800);
	@apply dark:text-white;
}

/* 问卷卡片网格布局 */
.questionnaire-grid {
	display: grid;
	gap: 24px;
	/* 使用auto-fill保持卡片合适宽度，避免过度拉伸 */
	grid-template-columns: repeat(auto-fill, minmax(360px, 1fr));
	width: 100%;

	/* 响应式断点调整 - 主要调整gap和minmax，避免使用固定列数 */
	@media (max-width: 480px) {
		/* 超小屏幕：1列，全宽 */
		grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
		gap: 16px;
		padding: 0 8px;
	}

	@media (min-width: 481px) and (max-width: 768px) {
		/* 小屏幕：自适应，但偏向1列 */
		grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
		gap: 20px;
	}

	@media (min-width: 769px) and (max-width: 1024px) {
		/* 中等屏幕：自适应，偏向2列 */
		grid-template-columns: repeat(auto-fill, minmax(360px, 1fr));
		gap: 20px;
	}

	@media (min-width: 1025px) and (max-width: 1400px) {
		/* 大屏幕：自适应，2-3列之间 */
		grid-template-columns: repeat(auto-fill, minmax(360px, 1fr));
		gap: 24px;
	}

	@media (min-width: 1401px) and (max-width: 1920px) {
		/* 更大屏幕：自适应，偏向3列 */
		grid-template-columns: repeat(auto-fill, minmax(380px, 1fr));
		gap: 28px;
	}

	@media (min-width: 1921px) and (max-width: 2560px) {
		/* 超宽屏：自适应，3-4列之间 */
		grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
		gap: 32px;
	}

	@media (min-width: 2561px) {
		/* 超大屏幕：自适应，4列以上 */
		grid-template-columns: repeat(auto-fill, minmax(420px, 1fr));
		gap: 32px;
	}

	/* 限制单个卡片的最大宽度，防止过度拉伸 */
	& > .questionnaire-card {
		max-width: 600px;
		width: 100%;
	}
}

/* 空状态在网格中的样式 */
.questionnaire-grid .empty-state {
	grid-column: 1 / -1; /* 占据整行 */
}

.popover-title {
	font-size: 14px;
	font-weight: 600;
	color: var(--primary-700);
	@apply dark:text-primary-300;
	margin-bottom: 10px;
}

.popover-tags {
	display: flex;
	flex-wrap: wrap;
	gap: 8px;
}

.popover-tag {
	@apply inline-flex items-center rounded-full border text-xs font-semibold transition-colors bg-primary-50 text-primary-500 border-primary-200 px-2 py-1;
	white-space: nowrap;
	width: 150px; /* 与主要标签保持一致的固定宽度 */
	overflow: hidden;
	text-overflow: ellipsis;
	justify-content: flex-start; /* 左对齐显示，优先显示workflow */
	flex-shrink: 0; /* 防止收缩 */
	background: linear-gradient(to right, rgb(196, 181, 253), rgb(191, 219, 254)) !important;
}

.popover-tag:hover {
	@apply bg-primary-100 border-primary-300;
}

/* Assignments容器样式 */
.assignments-container {
	height: 60px !important; /* 固定高度 */
}

/* 暗色主题样式 */
html.dark {
	/* InputTag暗色主题 - 优化暗色显示效果 */
	:deep(.filter-panel .layout) {
		background-color: var(--black-200) !important;
		border: 1px solid var(--black-200) !important;
		color: var(--white-100) !important;
	}

	:deep(.filter-panel .layout:hover) {
		border-color: var(--black-100) !important;
	}

	:deep(.filter-panel .layout:focus-within) {
		border-color: var(--primary-500) !important;
		box-shadow: 0 0 0 1px var(--primary-500) inset !important;
	}

	:deep(.filter-panel .input-tag) {
		color: var(--white-100) !important;
		background-color: transparent !important;
	}

	:deep(.filter-panel .input-tag::placeholder) {
		color: var(--gray-300) !important;
	}

	:deep(.filter-panel .label-box) {
		background-color: var(--black-300) !important;
		border: 1px solid var(--black-100) !important;
	}

	:deep(.filter-panel .label-title) {
		color: var(--white-100) !important;
	}

	:deep(.filter-panel .label-close) {
		background: var(--black-200) !important;
		color: var(--gray-300) !important;
	}

	:deep(.filter-panel .label-close:hover) {
		background: var(--black-100) !important;
		color: var(--white-100) !important;
	}

	/* 筛选面板暗色主题 */
	.filter-panel {
		@apply bg-black-400 dark:border-black-200;
	}

	.filter-label {
		@apply dark:text-primary-300;
	}

	/* Element Plus 组件暗色主题 */
	:deep(.filter-select .el-input__wrapper) {
		background-color: var(--black-200) !important;
		border-color: var(--black-200) !important;
	}

	:deep(.filter-select .el-input__wrapper:hover) {
		border-color: var(--black-100) !important;
	}

	:deep(.filter-select .el-input__wrapper.is-focus) {
		border-color: var(--primary-500);
		box-shadow: 0 0 0 3px rgba(126, 34, 206, 0.2);
	}

	:deep(.filter-select .el-input__inner) {
		@apply text-white-100;
	}
}
</style>
