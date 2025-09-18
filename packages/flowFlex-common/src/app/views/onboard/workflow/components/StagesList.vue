<template>
	<div class="stages-list">
		<!-- Actual content with scrollbar -->
		<el-scrollbar ref="scrollbarRef" class="pr-4">
			<!-- Loading skeleton -->
			<div v-if="isLoading" class="stages-loading">
				<div v-for="i in 3" :key="i" class="stage-skeleton">
					<div class="skeleton-header">
						<div class="skeleton-avatar"></div>
						<div class="skeleton-content">
							<div class="skeleton-title"></div>
							<div class="skeleton-description"></div>
						</div>
						<div class="skeleton-tags">
							<div class="skeleton-tag"></div>
							<div class="skeleton-tag"></div>
						</div>
					</div>
				</div>
			</div>
			<!-- Fixed sorting indicator at top of scroll area -->
			<div v-if="isSorting" class="sorting-banner-fixed">
				<div class="sorting-content">
					<div class="sorting-icon">
						<div class="sorting-lines">
							<div class="line line-1"></div>
							<div class="line line-2"></div>
							<div class="line line-3"></div>
						</div>
					</div>
				</div>
			</div>
			<div class="stages-content" :class="{ 'sorting-active': isSorting }">
				<draggable
					v-if="!isLoading"
					v-model="stagesList"
					item-key="id"
					handle=".drag-handle"
					:disabled="!isEditing || isLoading || isSorting"
					@start="onDragStart"
					@change="onDragEnd"
					ghost-class="ghost-stage"
					class="stages-draggable"
					:animation="300"
				>
					<template #item="{ element, index }">
						<div
							class="stage-item drag-handle"
							:class="{
								'stage-disabled': isLoading,
								'stage-sorting': isSorting,
							}"
							:style="{
								'border-left-color': element.color || getAvatarColor(element.name),
							}"
						>
							<div class="stage-header">
								<div
									class="left-section"
									@click="!isLoading && !isSorting && toggleStage(element.id)"
								>
									<el-checkbox
										v-if="isEditing"
										v-model="element.selected"
										:disabled="isLoading || isSorting"
										@change="handleSelectionChange(element)"
										@click.stop
									/>
									<div
										tabindex="0"
										role="button"
										class="flex items-center justify-center h-8 w-8 rounded-full"
										v-if="isEditing"
										:class="{
											'drag-disabled': isLoading,
											'drag-sorting': isSorting,
										}"
										:style="{
											backgroundColor:
												element.color || getAvatarColor(element.name),
										}"
									>
										<!-- <GripVertical style="color: white" /> -->
										<span class="text-base font-bold text-white">
											{{ index + 1 }}
										</span>
									</div>
									<div
										class="stage-avatar"
										:style="{
											backgroundColor:
												element.color || getAvatarColor(element.name),
										}"
										v-else
									>
										{{ getInitials(element.name) }}
									</div>
									<div class="stage-info">
										<div class="stage-name-container">
											<span class="stage-name">
												{{ element.name }}
												<!-- Portal Permission Icon -->
												<el-tooltip
													v-if="element.visibleInPortal"
													:content="
														getPortalPermissionTooltip(
															element.portalPermission
														)
													"
													placement="top"
												>
													<Icon
														:icon="
															getPortalPermissionIcon(
																element.portalPermission
															)
														"
														class="portal-icon"
													/>
												</el-tooltip>
											</span>
										</div>
										<div class="stage-description">
											{{ element.description || 'No description available' }}
										</div>
									</div>
								</div>
								<div class="right-section">
									<div
										v-if="element.defaultAssignedGroup"
										class="stage-tag stage-group-tag"
										:title="element.defaultAssignedGroup"
									>
										{{ element.defaultAssignedGroup }}
									</div>
									<div
										class="stage-tag stage-duration-tag"
										:title="`${element.estimatedDuration} ${
											element.estimatedDuration > 1 ? 'days' : 'day'
										}`"
									>
										{{ element.estimatedDuration }}
										{{ element.estimatedDuration > 1 ? 'days' : 'day' }}
									</div>
									<div
										v-if="getStageComponents(element).length > 0"
										class="stage-tag stage-group-tag"
										:title="`${getStageComponents(element).length} ${
											getStageComponents(element).length > 1
												? 'components'
												: 'component'
										}`"
									>
										{{ getStageComponents(element).length }}
										{{
											getStageComponents(element).length > 1
												? 'components'
												: 'component'
										}}
									</div>
									<el-dropdown
										trigger="click"
										:disabled="isLoading || isSorting"
										@command="(cmd) => handleCommand(cmd, element)"
										@click.stop
										:ref="(el) => (dropdownRefs[index] = el)"
									>
										<div
											class="inline-flex items-center justify-center h-9 rounded-xl px-3 hover:bg-accent hover:text-accent-foreground"
											:class="{
												'dropdown-disabled': isLoading,
												'dropdown-sorting': isSorting,
											}"
										>
											<Ellipsis />
										</div>
										<template #dropdown>
											<el-dropdown-menu>
												<el-dropdown-item command="edit">
													<div class="flex items-center gap-2 font-bold">
														<Edit class="w-4 h-4" />
														Edit
													</div>
												</el-dropdown-item>
												<el-dropdown-item
													command="delete"
													class="delete-item"
												>
													<div class="flex items-center gap-2 font-bold">
														<Delete class="w-4 h-4" />
														Delete
													</div>
												</el-dropdown-item>
											</el-dropdown-menu>
										</template>
									</el-dropdown>
									<!-- 箭头图标独立控制折叠 -->
									<div
										@click.stop="
											!isLoading && !isSorting && toggleStage(element.id)
										"
										class="toggle-arrow"
										:class="{
											'toggle-disabled': isLoading,
											'toggle-sorting': isSorting,
										}"
									>
										<ChevronRight
											v-if="!expandedStages || expandedStages !== element.id"
										/>
										<ChevronDown v-else />
									</div>
								</div>
							</div>

							<!-- 展开显示的详细信息 -->
							<div
								v-show="expandedStages === element.id && !isLoading && !isSorting"
								class="stage-details"
							>
								<!-- Required Fields -->
								<div
									class="stage-components-section"
									v-if="getSelectedStaticFields(element).length > 0"
								>
									<div class="text-sm font-medium mb-3">Required Fields</div>
									<div class="required-fields-tags">
										<span
											v-for="fieldName in getSelectedStaticFields(element)"
											:key="fieldName"
											class="field-tag"
										>
											{{ fieldName }}
										</span>
									</div>
								</div>

								<!-- Stage Components Section -->
								<div
									class="stage-components-section"
									v-if="getStageComponents(element).length > 0"
								>
									<div class="text-sm font-medium mb-3">Stage Components</div>
									<div class="components-list">
										<div
											v-for="(component, itemIndex) in getStageComponents(
												element
											)"
											:key="component.id"
											class="component-item"
										>
											<span class="component-number">
												{{ itemIndex + 1 }}
											</span>
											<div class="component-icon">
												<component :is="getComponentIcon(component.type)" />
											</div>
											<span class="component-name">{{ component.name }}</span>
											<span class="component-type">{{ component.type }}</span>
										</div>
									</div>
								</div>

								<!-- Stage Details Section -->
								<div class="stage-info-section">
									<div class="flex items-center space-x-6 mt-4">
										<div class="flex items-center">
											<Users
												:style="{
													color:
														element.color ||
														getAvatarColor(element.name),
													marginRight: '8px',
												}"
											/>
											<div>
												<span class="text-xs text-muted-foreground">
													Group:
												</span>
												<span class="font-medium">
													{{
														element.defaultAssignedGroup ||
														'Not assigned'
													}}
												</span>
											</div>
										</div>
										<div class="flex items-center">
											<Users
												:style="{
													color:
														element.color ||
														getAvatarColor(element.name),
													marginRight: '8px',
												}"
											/>
											<div class="flex items-center gap-2">
												<div class="text-xs text-muted-foreground">
													Assignee:
												</div>
												<FlowflexUserAssign
													v-model="element.defaultAssignee"
													selection-type="user"
													readonly
												/>
											</div>
										</div>
										<div class="flex items-center">
											<Clock
												:style="{
													color:
														element.color ||
														getAvatarColor(element.name),
													marginRight: '8px',
												}"
											/>
											<div>
												<span class="text-xs text-muted-foreground">
													Duration:
												</span>
												<span class="font-medium">
													{{ element.estimatedDuration }}
													{{
														element.estimatedDuration > 1
															? 'days'
															: 'day'
													}}
												</span>
											</div>
										</div>
									</div>
								</div>
							</div>
						</div>
					</template>
				</draggable>
			</div>
		</el-scrollbar>
	</div>
</template>

<script setup lang="ts">
import { computed, PropType, ref, watch } from 'vue';
import draggable from 'vuedraggable';
// 引入SVG图标作为组件
import ChevronRight from '@assets/svg/workflow/chevron-right.svg';
import ChevronDown from '@assets/svg/workflow/chevron-down.svg';
// import GripVertical from '@assets/svg/workflow/grip-vertical.svg';
import Ellipsis from '@assets/svg/workflow/ellipsis.svg';
import Users from '@assets/svg/workflow/users.svg';
import Clock from '@assets/svg/workflow/clock.svg';
import {
	Edit,
	Delete,
	Document,
	List,
	QuestionFilled,
	FolderOpened,
} from '@element-plus/icons-vue';
import { Icon } from '@iconify/vue';
import { useAdaptiveScrollbar } from '@/hooks/useAdaptiveScrollbar';
import { Stage } from '#/onboard';
import FlowflexUserAssign from '@/components/form/flowflexUser/index.vue';
// 导入静态字段配置
import staticFieldConfig from '../static-field.json';
import { defaultStr } from '@/settings/projectSetting';
import { ElDropdown } from 'element-plus';
import { FlowflexUser } from '#/golbal';
import { getAvatarColor } from '@/utils';

// Portal权限枚举常量
const PortalPermissionEnum = {
	Viewable: 1,
	Completable: 2,
};

// Props
const props = defineProps({
	stages: {
		type: Array as PropType<Stage[]>,
		required: true,
	},
	workflowId: {
		type: String,
		required: true,
	},
	isEditing: {
		type: Boolean,
		default: false,
	},
	loading: {
		type: [Boolean, Object] as PropType<
			boolean | { stages?: boolean; deleteStage?: boolean; sortStages?: boolean }
		>,
		default: false,
	},
	userList: {
		type: Array as PropType<FlowflexUser[]>,
		required: true,
	},
});

// Emits
const emit = defineEmits(['update:stages', 'edit', 'delete', 'order-changed', 'drag-start']);

// 使用自适应滚动条 hook，设置底部留白为 20px
const { scrollbarRef } = useAdaptiveScrollbar(100);

// 内部状态
const expandedStages = ref<string | null>(null);

// 计算loading状态
const isLoading = computed(() => {
	if (typeof props.loading === 'boolean') {
		return props.loading;
	}
	return props.loading?.stages || false;
});

// 计算排序loading状态
const isSorting = computed(() => {
	if (typeof props.loading === 'object') {
		return props.loading?.sortStages || false;
	}
	return false;
});

const stagesList = computed({
	get: () => props.stages.map((stage) => ({ ...stage, selected: stage.selected || false })),
	set: (value) => {
		emit('update:stages', value);
	},
});

// 监听 stages 数组变化，实现每次刷新列表时默认展开第一条 stage
watch(
	() => props.stages,
	(newStages, oldStages) => {
		if (newStages && newStages.length > 0) {
			// 检查是否是初始加载或者数据真的发生了变化
			const isInitialLoad = !oldStages || oldStages.length === 0;
			const isDataChanged =
				oldStages &&
				(newStages.length !== oldStages.length || newStages[0]?.id !== oldStages[0]?.id);

			// 如果是初始加载或数据发生变化（如刷新列表），自动展开第一条 stage
			if (isInitialLoad || isDataChanged) {
				expandedStages.value = newStages[0].id;
			}
		} else {
			// 如果没有数据，清除展开状态
			expandedStages.value = null;
		}
	},
	{ immediate: true } // 组件初始化时立即执行一次
);

// 方法
const toggleStage = (id: string) => {
	if (expandedStages.value === id) {
		// 如果点击的是当前展开的stage，则收起
		expandedStages.value = null;
	} else {
		// 否则切换到新的stage
		expandedStages.value = id;
	}
};

const getInitials = (name: string) => {
	if (!name) return '';
	return name
		.split(' ')
		.map((word) => word[0])
		.join('')
		.substring(0, 2)
		.toUpperCase();
};

const handleCommand = (command: string, stage: Stage) => {
	switch (command) {
		case 'edit':
			emit('edit', stage);
			break;
		case 'delete':
			emit('delete', stage.id);
			break;
	}
};

const dropdownRefs = ref<any[]>([]);
const closeAllDropdowns = async () => {
	dropdownRefs.value.forEach((d) => d?.handleClose?.());
};

const onDragStart = () => {
	closeAllDropdowns();
	// 触发拖动开始事件，让父组件保存原始顺序
	emit('drag-start');
};

const onDragEnd = () => {
	// 重新排序
	const reorderedStages = stagesList.value.map((stage, index) => ({
		...stage,
		order: index + 1,
	}));

	emit('update:stages', reorderedStages);
	emit('order-changed', reorderedStages);
};

// 恢复 handleSelectionChange 方法
const handleSelectionChange = (stage: Stage) => {
	// 更新选中状态
	const updatedStages = stagesList.value.map((s) => {
		if (s.id === stage.id) {
			return { ...s, selected: stage.selected };
		}
		return s;
	});

	emit('update:stages', updatedStages);
};

// 获取阶段组件列表
const getStageComponents = (stage: Stage) => {
	if (!stage.components || stage.components.length === 0) {
		return [];
	}

	const componentList: Array<{ id: string; name: string; type: string }> = [];

	stage.components.forEach((component) => {
		if (component.isEnabled) {
			switch (component.key) {
				case 'fields':
					componentList.push({
						id: `${stage.id}-fields`,
						name: 'Required Fields',
						type: 'fields',
					});
					break;
				case 'checklist':
					// 每个checklist可能有多个ID，但在这里显示为一个组件
					component.checklistIds.forEach((checklistId, index) => {
						// 优先使用names字段中的名称
						const checklistName = component.checklistNames?.[index] || defaultStr;
						componentList.push({
							id: `${stage.id}-checklist-${checklistId}`,
							name: checklistName,
							type: 'checklist',
						});
					});
					break;
				case 'questionnaires':
					// 每个questionnaire可能有多个ID，但在这里显示为一个组件
					component.questionnaireIds.forEach((questionnaireId, index) => {
						// 优先使用names字段中的名称
						const questionnaireName =
							component.questionnaireNames?.[index] || defaultStr;
						componentList.push({
							id: `${stage.id}-questionnaire-${questionnaireId}`,
							name: questionnaireName,
							type: 'questionnaires',
						});
					});
					break;
				case 'files':
					componentList.push({
						id: `${stage.id}-files`,
						name: 'File Attachments',
						type: 'files',
					});
					break;
			}
		}
	});

	// 按照order排序
	return componentList;
};

// 获取组件图标
const getComponentIcon = (type: string) => {
	switch (type) {
		case 'fields':
			return Document;
		case 'checklist':
			return List;
		case 'questionnaires':
			return QuestionFilled;
		case 'files':
			return FolderOpened;
		default:
			return Document;
	}
};

// 创建从vIfKey到label的映射
const fieldLabelMap = computed(() => {
	const map: Record<string, string> = {};
	staticFieldConfig.formFields.forEach((field) => {
		map[field.vIfKey] = field.label;
	});
	return map;
});

// 静态字段标签映射
const getFieldLabel = (apiFieldName: string) => {
	return fieldLabelMap.value[apiFieldName] || apiFieldName;
};

// 获取选择的静态字段
const getSelectedStaticFields = (stage: Stage) => {
	const fieldsComponent = stage.components?.find(
		(component) => component.key === 'fields' && component.isEnabled
	);

	if (!fieldsComponent || !fieldsComponent.staticFields) {
		return [];
	}

	return fieldsComponent.staticFields.map((field) => getFieldLabel(field));
};

// 获取Portal权限图标
const getPortalPermissionIcon = (permission?: number) => {
	if (permission === PortalPermissionEnum.Completable) {
		return 'material-symbols:person-edit-outline';
	}
	return 'material-symbols:table-eye-outline-rounded';
};

// 获取Portal权限工具提示文本
const getPortalPermissionTooltip = (permission?: number) => {
	if (permission === PortalPermissionEnum.Completable) {
		return 'Available in portal (Completable)';
	}
	return 'Available in portal (view only)';
};
</script>

<style lang="scss" scoped>
.stages-list {
	margin-top: 10px;
}

/* 滚动条样式 */
.stages-scrollbar {
	width: 100%;
}

.stages-scrollbar :deep(.el-scrollbar__view) {
	padding: 0;
}

.stages-actions {
	display: flex;
	gap: 10px;
	margin-bottom: 10px;
}

.stages-draggable {
	display: flex;
	flex-direction: column;
	gap: 16px;
}

.stage-item {
	@apply bg-white dark:bg-gray-800 border border-gray-100 dark:border-gray-600 overflow-hidden relative rounded-xl;
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.04);
	border-left-width: 4px;
	border-left-style: solid;
	transition: all 0.2s ease;
}

.stage-item:hover {
	box-shadow: 0 6px 20px rgba(0, 0, 0, 0.08);
	transform: translateY(-1px);
}

/* Dark mode hover effect */
html.dark .stage-item:hover {
	box-shadow: 0 6px 20px rgba(0, 0, 0, 0.25);
}

.stage-header {
	@apply flex justify-between items-center p-4 cursor-pointer bg-white dark:bg-gray-800 gap-4;
	transition: background-color 0.2s;
}

.stage-header:hover {
	@apply bg-blue-50/50 dark:bg-gray-700;
}

.left-section {
	@apply flex items-center gap-2.5 flex-1 min-w-0;
	max-width: calc(100% - 280px); /* 为右侧区域预留空间 */
}

.stage-avatar {
	@apply w-8 h-8 rounded-full flex items-center justify-center text-white font-bold text-xs flex-shrink-0;
}

.stage-info {
	@apply flex flex-col flex-1 min-w-0 overflow-hidden;
}

.stage-name-container {
	@apply flex items-center mb-0.5;
}

.stage-name {
	@apply font-medium text-gray-900 dark:text-gray-100 whitespace-nowrap overflow-hidden text-ellipsis flex-1 min-w-0 flex items-center gap-1.5;
	font-size: 15px;
}

.portal-icon {
	@apply w-5 h-5 text-gray-900 dark:text-gray-100 flex-shrink-0 outline-none border-none pt-0.5;
}

.portal-icon:focus,
.portal-icon:active,
.portal-icon:hover {
	outline: none !important;
	border: none !important;
	box-shadow: none !important;
}

.stage-description {
	@apply text-xs text-gray-500 dark:text-gray-400 whitespace-nowrap overflow-hidden text-ellipsis max-w-full;
}

.right-section {
	@apply flex items-center flex-shrink-0 gap-2;
}

/* 标签样式 */
.stage-tag {
	@apply inline-flex items-center justify-center rounded-full border border-gray-100 dark:border-gray-600 px-2.5 py-1 text-xs font-semibold bg-gray-50 dark:bg-gray-700 whitespace-nowrap flex-shrink-0 overflow-hidden text-ellipsis transition-all duration-200;
	max-width: 120px;
}

.stage-tag:hover {
	@apply bg-gray-100 dark:bg-gray-600 border-gray-200 dark:border-gray-500;
}

.stage-group-tag {
	@apply text-gray-700 dark:text-gray-300;
}

.stage-duration-tag {
	@apply text-emerald-600 dark:text-emerald-400 border-emerald-100 dark:border-emerald-700 bg-emerald-50 dark:bg-emerald-900/20;
}

.stage-duration-tag:hover {
	@apply bg-emerald-100 dark:bg-emerald-900/30;
}

.drag-handle {
	cursor: pointer;
}

.ghost-stage {
	opacity: 0.6;
	background: var(--primary-50, #f0f7ff);
	border: 1px dashed var(--primary-500, #2468f2);
}

.delete-item {
	color: var(--red-500, #f56c6c);
}

/* 展开详情样式 */
.stage-details {
	@apply px-4 pb-4 border-t border-gray-100 dark:border-gray-600 bg-slate-50/50 dark:bg-gray-800/80;
}

/* Stage Components Section */
.stage-components-section {
	@apply mb-4;
}

.components-list {
	@apply flex flex-col gap-2;
}

.component-item {
	@apply flex items-center gap-3 p-2 px-3 bg-white dark:bg-gray-700 border border-gray-100 dark:border-gray-600 rounded-xl transition-all duration-200;
	box-shadow: 0 1px 3px rgba(0, 0, 0, 0.02);
}

.component-item:hover {
	@apply bg-blue-50 dark:bg-gray-600 border-blue-100 dark:border-gray-500;
	box-shadow: 0 2px 6px rgba(0, 0, 0, 0.04);
}

.component-number {
	@apply flex items-center justify-center w-5 h-5 bg-blue-500 dark:bg-blue-600 text-white rounded-full text-xs font-semibold flex-shrink-0;
}

.component-icon {
	@apply flex items-center justify-center w-5 h-5 text-blue-600 dark:text-blue-400 flex-shrink-0;
}

.component-icon svg {
	@apply w-4 h-4;
}

.component-name {
	@apply flex-1 text-sm font-medium text-gray-700 dark:text-gray-300 min-w-0;
}

.component-type {
	@apply text-xs font-medium text-slate-600 dark:text-gray-400 bg-slate-100 dark:bg-gray-600 px-2 py-0.5 lowercase flex-shrink-0 rounded-xl;
}

.no-components {
	@apply p-3 text-center bg-gray-50 dark:bg-gray-700 border border-dashed border-gray-300 dark:border-gray-600 rounded-xl;
}

.stage-info-section {
	@apply border-t border-gray-200 dark:border-gray-600 pt-3;
}

.required-fields-section {
	@apply mt-3 mb-4;
}

.required-fields-tags {
	@apply flex flex-wrap gap-1.5 mt-2;
}

.field-tag {
	@apply inline-flex items-center px-2.5 py-1 text-xs font-semibold bg-indigo-50 dark:bg-indigo-900/20 text-indigo-700 dark:text-indigo-300 border border-indigo-200 dark:border-indigo-700 whitespace-nowrap transition-all duration-200 rounded-xl;
}

.field-tag:hover {
	@apply bg-indigo-100 dark:bg-indigo-900/30 border-indigo-300 dark:border-indigo-600;
}

:deep(.el-checkbox__inner) {
	border-color: var(--primary-100, #d9e6ff);
	background-color: #fff;
}

:deep(.el-checkbox__input.is-checked .el-checkbox__inner) {
	background-color: var(--primary-500, #2468f2);
	border-color: var(--primary-500, #2468f2);
}

.text-muted-foreground {
	@apply text-gray-500 dark:text-gray-400 mr-2;
}

.toggle-arrow {
	@apply flex items-center justify-center h-7 w-7 cursor-pointer ml-1;
}

.toggle-arrow:hover {
	@apply bg-blue-100 dark:bg-gray-600 rounded-full;
}

/* Loading states */
.stage-disabled {
	opacity: 0.6;
	pointer-events: none;
}

.drag-disabled {
	cursor: not-allowed !important;
	opacity: 0.5;
}

.dropdown-disabled {
	cursor: not-allowed;
	opacity: 0.5;
}

.dropdown-disabled:hover {
	background-color: transparent !important;
}

.toggle-disabled {
	cursor: not-allowed;
	opacity: 0.5;
}

.toggle-disabled:hover {
	background-color: transparent !important;
}

/* Loading skeleton styles */
.stages-loading {
	@apply flex flex-col gap-4 mt-2.5;
}

.stage-skeleton {
	@apply bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-600 overflow-hidden rounded-xl;
	box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
	border-left-width: 4px;
	border-left-style: solid;
	border-left-color: #e5e7eb;
}

.skeleton-header {
	@apply flex justify-between items-center p-4;
}

.skeleton-avatar {
	width: 32px;
	height: 32px;
	border-radius: 50%;
	background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
	background-size: 200% 100%;
	animation: skeleton-loading 1.5s infinite;
}

.skeleton-content {
	@apply flex-1 ml-3 mr-4;
}

.skeleton-title {
	height: 16px;
	background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
	background-size: 200% 100%;
	animation: skeleton-loading 1.5s infinite;
	margin-bottom: 8px;
	width: 60%;
	@apply rounded-xl;
}

.skeleton-description {
	height: 12px;
	background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
	background-size: 200% 100%;
	animation: skeleton-loading 1.5s infinite;
	width: 80%;
	@apply rounded-xl;
}

.skeleton-tags {
	@apply flex gap-2;
}

.skeleton-tag {
	height: 24px;
	width: 80px;
	background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
	background-size: 200% 100%;
	animation: skeleton-loading 1.5s infinite;
	@apply rounded-xl;
}

@keyframes skeleton-loading {
	0% {
		background-position: 200% 0;
	}
	100% {
		background-position: -200% 0;
	}
}

/* Sorting banner styles */
.sorting-banner {
	position: relative;
	background: linear-gradient(135deg, rgba(36, 104, 242, 0.08), rgba(139, 92, 246, 0.08));
	border: 1px solid rgba(36, 104, 242, 0.2);
	padding: 12px 16px;
	margin-bottom: 16px;
	animation: banner-slide-down 0.3s ease-out;
	@apply rounded-xl;
}

/* Fixed sorting banner styles - sticky within scroll container */
.sorting-banner-fixed {
	position: sticky;
	top: 0;
	z-index: 10;
	background: linear-gradient(135deg, rgba(36, 104, 242, 0.95), rgba(139, 92, 246, 0.95));
	backdrop-filter: blur(8px);
	border: 1px solid rgba(36, 104, 242, 0.3);
	padding: 12px 16px;
	margin-bottom: 16px;
	box-shadow: 0 2px 8px rgba(36, 104, 242, 0.15);
	animation: banner-slide-down 0.3s ease-out;
	@apply rounded-xl;
}

.sorting-content {
	display: flex;
	align-items: center;
	gap: 12px;
}

.sorting-icon {
	width: 24px;
	height: 24px;
	display: flex;
	align-items: center;
	justify-content: center;
}

.sorting-lines {
	width: 100%;
	height: 100%;
	display: flex;
	flex-direction: column;
	justify-content: space-between;
	gap: 2px;
}

.line {
	height: 2px;
	background: var(--primary-500, #2468f2);
	animation: sorting-wave 1.5s infinite ease-in-out;
	@apply rounded-xl;
}

.line-1 {
	width: 100%;
	animation-delay: 0s;
}

.line-2 {
	width: 75%;
	animation-delay: 0.2s;
}

.line-3 {
	width: 50%;
	animation-delay: 0.4s;
}

@keyframes sorting-wave {
	0%,
	100% {
		opacity: 1;
		transform: scaleX(1);
	}
	50% {
		opacity: 0.4;
		transform: scaleX(0.6);
	}
}

@keyframes banner-slide-down {
	from {
		opacity: 0;
		transform: translateY(-10px);
	}
	to {
		opacity: 1;
		transform: translateY(0);
	}
}

.sorting-banner-fixed .line {
	background: white;
}

.stages-content {
	transition: all 0.3s ease;
}

.sorting-active .stage-item {
	pointer-events: none;
}

/* Sorting state styles for individual elements */
.stage-sorting {
	transition: all 0.3s ease;
	transform: scale(0.98);
	filter: blur(1px);
}

.drag-sorting {
	cursor: wait !important;
	opacity: 0.7;
	animation: pulse-sorting 1.5s infinite;
}

.dropdown-sorting {
	cursor: wait;
	opacity: 0.7;
}

.dropdown-sorting:hover {
	background-color: transparent !important;
}

.toggle-sorting {
	cursor: wait;
	opacity: 0.7;
}

.toggle-sorting:hover {
	background-color: transparent !important;
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

/* 响应式设计 */
@media (max-width: 1024px) {
	.left-section {
		max-width: calc(100% - 240px); /* 中等屏幕减少右侧预留空间 */
	}
}

@media (max-width: 768px) {
	.stage-header {
		flex-direction: column;
		align-items: stretch;
		gap: 12px;
		padding: 12px 16px;
	}

	.left-section {
		max-width: none; /* 移动端取消宽度限制 */
		order: 1;
	}

	.right-section {
		order: 2;
		justify-content: flex-start;
		flex-wrap: wrap;
		gap: 6px;
	}

	.stage-name {
		font-size: 14px;
	}

	.stage-description {
		font-size: 11px;
	}

	.stage-tag {
		font-size: 10px;
		padding: 2px 6px;
		max-width: 100px;
	}
}

@media (max-width: 480px) {
	.stage-header {
		padding: 10px 12px;
	}

	.left-section {
		gap: 8px;
	}

	.stage-avatar {
		width: 28px;
		height: 28px;
		font-size: 10px;
	}

	.drag-handle {
		width: 28px;
		height: 28px;
	}

	.right-section {
		gap: 4px;
	}

	.stage-tag {
		font-size: 9px;
		padding: 1px 4px;
		max-width: 80px;
	}

	.field-tag {
		background-color: rgba(14, 165, 233, 0.1);
		color: #60a5fa;
		border-color: rgba(14, 165, 233, 0.3);
	}

	.field-tag:hover {
		background-color: rgba(14, 165, 233, 0.2);
		border-color: rgba(14, 165, 233, 0.5);
	}
}
</style>
