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
					<template #item="{ element }">
						<div
							class="stage-item"
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
										class="drag-handle flex items-center justify-center h-8 w-8 rounded-full"
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
										<GripVertical style="color: white" />
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
										<a class="stage-name">
											{{ element.name }}
										</a>
										<div class="stage-description">
											{{ element.description || 'No description available' }}
										</div>
									</div>
								</div>
								<div class="right-section">
									<div
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
									<el-dropdown
										trigger="click"
										:disabled="isLoading || isSorting"
										@command="(cmd) => handleCommand(cmd, element)"
										@click.stop
									>
										<div
											class="inline-flex items-center justify-center h-9 rounded-md px-3 hover:bg-accent hover:text-accent-foreground"
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
								<!-- Stage Components Section -->
								<div class="stage-components-section">
									<div class="text-sm font-medium mb-3">Stage Components</div>
									<div
										v-if="getStageComponents(element).length > 0"
										class="components-list"
									>
										<div
											v-for="(component, index) in getStageComponents(
												element
											)"
											:key="component.id"
											class="component-item"
										>
											<span class="component-number">{{ index + 1 }}</span>
											<div class="component-icon">
												<component :is="getComponentIcon(component.type)" />
											</div>
											<span class="component-name">{{ component.name }}</span>
											<span class="component-type">{{ component.type }}</span>
										</div>
									</div>
									<div v-else class="no-components">
										<span class="text-xs text-muted-foreground">
											No components configured
										</span>
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
											<div>
												<span class="text-xs text-muted-foreground">
													Assignee:
												</span>
												<span class="font-medium">
													{{ element.defaultAssignee || 'Not assigned' }}
												</span>
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
import GripVertical from '@assets/svg/workflow/grip-vertical.svg';
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
import { useAdaptiveScrollbar } from '@/hooks/useAdaptiveScrollbar';
import { Stage } from '#/onboard';

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

const getAvatarColor = (name: string) => {
	const colors = [
		'#4F46E5', // Indigo
		'#0EA5E9', // Sky
		'#10B981', // Emerald
		'#F59E0B', // Amber
		'#EC4899', // Pink
		'#8B5CF6', // Violet
		'#06B6D4', // Cyan
		'#14B8A6', // Teal
		'#F43F5E', // Rose
		'#22C55E', // Green
		'#3B82F6', // Blue
		'#A855F7', // Purple
	];

	// 使用名称的哈希值来确定颜色，这样同名的 stage 颜色一致
	let hash = 0;
	for (let i = 0; i < name.length; i++) {
		hash = name.charCodeAt(i) + ((hash << 5) - hash);
	}

	return colors[Math.abs(hash) % colors.length];
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

const onDragStart = () => {
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
						componentList.push({
							id: `${stage.id}-checklist-${checklistId}`,
							name: `Initial Setup Checklist`,
							type: 'checklist',
						});
					});
					break;
				case 'questionnaires':
					// 每个questionnaire可能有多个ID，但在这里显示为一个组件
					component.questionnaireIds.forEach((questionnaireId, index) => {
						componentList.push({
							id: `${stage.id}-questionnaire-${questionnaireId}`,
							name: `Business Questionnaire`,
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
	return componentList.sort((a, b) => {
		const componentA = stage.components?.find(
			(c) =>
				c.key === a.type ||
				(a.type.includes('checklist') && c.key === 'checklist') ||
				(a.type.includes('questionnaire') && c.key === 'questionnaires')
		);
		const componentB = stage.components?.find(
			(c) =>
				c.key === b.type ||
				(b.type.includes('checklist') && c.key === 'checklist') ||
				(b.type.includes('questionnaire') && c.key === 'questionnaires')
		);
		return (componentA?.order || 0) - (componentB?.order || 0);
	});
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
	background-color: white;
	border: 1px solid var(--el-border-color-light, #e4e7ed);
	border-radius: var(--el-border-radius-base, 8px);
	overflow: hidden;
	box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
	position: relative;
	border-left-width: 4px;
	border-left-style: solid;
}

.stage-item:hover {
	box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
}

.stage-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	padding: 16px;
	cursor: pointer;
	background-color: #fff;
	transition: background-color 0.2s;
	gap: 16px;
}

.stage-header:hover {
	background-color: rgba(0, 0, 0, 0.03);
}

.left-section {
	display: flex;
	align-items: center;
	gap: 10px;
	flex: 1;
	min-width: 0; /* 允许收缩 */
	max-width: calc(100% - 280px); /* 为右侧区域预留空间 */
}

.stage-avatar {
	width: 32px;
	height: 32px;
	border-radius: 50%;
	display: flex;
	align-items: center;
	justify-content: center;
	color: white;
	font-weight: bold;
	font-size: 12px;
	flex-shrink: 0; /* 防止头像被压缩 */
}

.stage-info {
	display: flex;
	flex-direction: column;
	flex: 1;
	min-width: 0; /* 允许内容溢出处理 */
	overflow: hidden;
}

.stage-name {
	font-weight: 500;
	font-size: 15px;
	color: #111827;
	margin-bottom: 2px;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.stage-description {
	font-size: 12px;
	color: #6b7280;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	max-width: 100%;
}

.right-section {
	display: flex;
	align-items: center;
	flex-shrink: 0; /* 防止右侧区域被压缩 */
	gap: 8px;
}

/* 标签样式 */
.stage-tag {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	border-radius: 9999px;
	border: 1px solid #e5e7eb;
	padding: 4px 10px;
	font-size: 11px;
	font-weight: 600;
	background-color: white;
	white-space: nowrap;
	flex-shrink: 0;
	max-width: 120px;
	overflow: hidden;
	text-overflow: ellipsis;
	transition: all 0.2s ease;
}

.stage-tag:hover {
	background-color: #f9fafb;
	border-color: #d1d5db;
}

.stage-group-tag {
	color: #374151;
}

.stage-duration-tag {
	color: #059669;
	border-color: #d1fae5;
	background-color: #ecfdf5;
}

.stage-duration-tag:hover {
	background-color: #d1fae5;
}

.drag-handle {
	cursor: move;
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
	padding: 0 16px 16px 16px;
	border-top: 1px solid var(--el-border-color-light, #ebeef5);
	background-color: rgba(255, 255, 255, 0.8);
}

/* Stage Components Section */
.stage-components-section {
	margin-bottom: 16px;
}

.components-list {
	display: flex;
	flex-direction: column;
	gap: 8px;
}

.component-item {
	display: flex;
	align-items: center;
	gap: 12px;
	padding: 8px 12px;
	background-color: #f8f9fa;
	border-radius: 6px;
	border: 1px solid #e9ecef;
	transition: all 0.2s ease;
}

.component-item:hover {
	background-color: #f1f3f4;
	border-color: #dee2e6;
}

.component-number {
	display: flex;
	align-items: center;
	justify-content: center;
	width: 20px;
	height: 20px;
	background-color: var(--primary-100, #dbeafe);
	color: var(--primary-600, #2563eb);
	border-radius: 50%;
	font-size: 11px;
	font-weight: 600;
	flex-shrink: 0;
}

.component-icon {
	display: flex;
	align-items: center;
	justify-content: center;
	width: 20px;
	height: 20px;
	color: var(--primary-600, #2563eb);
	flex-shrink: 0;
}

.component-icon svg {
	width: 16px;
	height: 16px;
}

.component-name {
	flex: 1;
	font-size: 13px;
	font-weight: 500;
	color: #374151;
	min-width: 0;
}

.component-type {
	font-size: 11px;
	font-weight: 500;
	color: #6b7280;
	background-color: #f3f4f6;
	padding: 2px 8px;
	border-radius: 12px;
	text-transform: lowercase;
	flex-shrink: 0;
}

.no-components {
	padding: 12px;
	text-align: center;
	background-color: #f8f9fa;
	border-radius: 6px;
	border: 1px dashed #dee2e6;
}

.stage-info-section {
	border-top: 1px solid #e5e7eb;
	padding-top: 12px;
}

.required-fields-section {
	margin-top: 12px;
	margin-bottom: 16px;
}

.field-tag {
	display: inline-flex;
	align-items: center;
	border-radius: 9999px;
	padding: 2px 8px;
	font-size: 0.75rem;
	font-weight: 600;
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
	color: #6b7280;
	@apply mr-2;
}

.toggle-arrow {
	display: flex;
	align-items: center;
	justify-content: center;
	height: 28px;
	width: 28px;
	cursor: pointer;
	margin-left: 4px;
}

.toggle-arrow:hover {
	background-color: rgba(0, 0, 0, 0.05);
	border-radius: 50%;
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
	display: flex;
	flex-direction: column;
	gap: 16px;
	margin-top: 10px;
}

.stage-skeleton {
	background-color: white;
	border: 1px solid var(--el-border-color-light, #e4e7ed);
	border-radius: var(--el-border-radius-base, 8px);
	overflow: hidden;
	box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
	border-left-width: 4px;
	border-left-style: solid;
	border-left-color: #e5e7eb;
}

.skeleton-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	padding: 16px;
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
	flex: 1;
	margin-left: 12px;
	margin-right: 16px;
}

.skeleton-title {
	height: 16px;
	background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
	background-size: 200% 100%;
	animation: skeleton-loading 1.5s infinite;
	border-radius: 4px;
	margin-bottom: 8px;
	width: 60%;
}

.skeleton-description {
	height: 12px;
	background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
	background-size: 200% 100%;
	animation: skeleton-loading 1.5s infinite;
	border-radius: 4px;
	width: 80%;
}

.skeleton-tags {
	display: flex;
	gap: 8px;
}

.skeleton-tag {
	height: 24px;
	width: 80px;
	background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
	background-size: 200% 100%;
	animation: skeleton-loading 1.5s infinite;
	border-radius: 12px;
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
	border-radius: 8px;
	padding: 12px 16px;
	margin-bottom: 16px;
	animation: banner-slide-down 0.3s ease-out;
}

/* Fixed sorting banner styles - sticky within scroll container */
.sorting-banner-fixed {
	position: sticky;
	top: 0;
	z-index: 10;
	background: linear-gradient(135deg, rgba(36, 104, 242, 0.95), rgba(139, 92, 246, 0.95));
	backdrop-filter: blur(8px);
	border: 1px solid rgba(36, 104, 242, 0.3);
	border-radius: 8px;
	padding: 12px 16px;
	margin-bottom: 16px;
	box-shadow: 0 2px 8px rgba(36, 104, 242, 0.15);
	animation: banner-slide-down 0.3s ease-out;
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
	border-radius: 1px;
	animation: sorting-wave 1.5s infinite ease-in-out;
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
}
</style>
