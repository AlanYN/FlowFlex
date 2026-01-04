<template>
	<div class="grid grid-cols-1 lg:grid-cols-2 gap-6 p-1">
		<!-- Left Side: Selection Areas -->

		<div class="space-y-4 w-full overflow-hidden">
			<el-scrollbar ref="scrollbarRefLeft" class="stage-components-selector">
				<!-- Required Fields -->
				<div class="space-y-2">
					<label class="text-base font-bold">Fields</label>
					<p class="text-sm">Select fields that are required for this stage</p>
					<div class="border rounded-xl">
						<div class="p-2 border-b">
							<el-input
								v-model="searchQuery"
								placeholder="Search fields..."
								clearable
								class="search-input"
								size="small"
							>
								<template #prefix>
									<el-icon><Search /></el-icon>
								</template>
							</el-input>
						</div>
						<el-scrollbar max-height="160px">
							<div class="p-2">
								<div
									v-for="field in filteredStaticFields"
									:key="field.id"
									class="flex items-center space-x-2 py-1"
								>
									<el-checkbox
										:model-value="isFieldSelected(field.id)"
										@change="(checked) => toggleField(field.id, !!checked)"
										:id="`field-${field.id}`"
										size="small"
									/>
									<div class="flex-1 min-w-0">
										<label
											:for="`field-${field.id}`"
											class="text-sm font-medium leading-none flex-1 cursor-pointer min-w-0"
										>
											<span class="truncate">{{ field.fieldName }}</span>
										</label>
									</div>
								</div>
							</div>
						</el-scrollbar>
					</div>
					<div
						v-if="getFieldsComponent().staticFields.length > 0"
						class="text-xs text-primary"
					>
						{{ getFieldsComponent().staticFields.length }} fields selected
					</div>
				</div>

				<!-- Selected Fields List -->
				<div v-if="selectedFieldsList.length > 0" class="space-y-2">
					<label class="text-base font-bold">Selected Fields</label>
					<p class="text-sm">Drag to reorder, toggle required status</p>
					<div class="border rounded-xl">
						<el-scrollbar max-height="200px">
							<draggable
								v-model="selectedFieldsList"
								group="fields"
								handle=".field-drag-handle"
								class="p-2 space-y-1"
								item-key="id"
								ghost-class="ghost-field"
								@change="updateFieldsOrder"
								:animation="300"
							>
								<template #item="{ element, index }">
									<div
										class="flex items-center gap-2 p-2 border rounded-lg bg-white dark:bg-black-400 hover:shadow-sm transition-all"
									>
										<div
											class="field-drag-handle flex items-center justify-center w-5 h-5 cursor-row-resize flex-shrink-0"
										>
											<el-icon class="text-gray-400 text-sm">
												<GripVertical />
											</el-icon>
										</div>
										<span
											class="text-xs bg-primary text-white w-5 h-5 flex items-center justify-center rounded font-medium flex-shrink-0"
										>
											{{ index + 1 }}
										</span>
										<div class="flex-1 min-w-0">
											<el-tooltip
												:content="element.label"
												placement="top"
												:disabled="element.label.length <= 20"
											>
												<span class="text-sm block truncate">
													{{ element.label }}
												</span>
											</el-tooltip>
										</div>
										<el-checkbox
											:model-value="element.isRequired"
											@change="
												(checked) =>
													toggleFieldRequired(element.id, !!checked)
											"
											size="small"
										>
											<span class="text-xs">Required</span>
										</el-checkbox>
										<el-button
											size="small"
											type="danger"
											link
											@click="removeFieldTag(element.id)"
										>
											<el-icon class="text-sm"><Close /></el-icon>
										</el-button>
									</div>
								</template>
							</draggable>
						</el-scrollbar>
					</div>
				</div>

				<!-- Checklists -->
				<div class="space-y-2 mt-4">
					<label class="text-base font-bold">Checklists</label>
					<p class="text-sm">Select checklists to include in this stage</p>
					<div class="border rounded-xl">
						<el-scrollbar max-height="160px">
							<div class="p-2">
								<div
									v-for="checklist in checklists"
									:key="checklist.id"
									class="flex items-center space-x-2 py-1"
								>
									<el-checkbox
										:model-value="isChecklistSelected(checklist.id)"
										@change="
											(checked) => toggleChecklist(checklist.id, !!checked)
										"
										:id="`checklist-${checklist.id}`"
										size="small"
									/>
									<div class="flex-1 min-w-0">
										<label
											:for="`checklist-${checklist.id}`"
											class="text-sm font-medium cursor-pointer block truncate"
										>
											{{ checklist.name }}
										</label>
									</div>
								</div>
								<div v-if="checklists.length === 0">
									<el-empty description="No checklists found" :image-size="30" />
								</div>
							</div>
						</el-scrollbar>
					</div>
					<div
						v-if="getChecklistComponent().checklistIds.length > 0"
						class="text-xs text-primary-600"
					>
						{{ getChecklistComponent().checklistIds.length }} checklists selected
					</div>
				</div>

				<!-- Questionnaires -->
				<div class="space-y-2 mt-4">
					<label class="text-base font-bold">Questionnaires</label>
					<p class="text-sm">Select questionnaires to include in this stage</p>
					<div class="border rounded-xl">
						<el-scrollbar max-height="160px">
							<div class="p-2">
								<div
									v-for="questionnaire in questionnaires"
									:key="questionnaire.id"
									class="flex items-center space-x-2 py-1"
								>
									<el-checkbox
										:model-value="isQuestionnaireSelected(questionnaire.id)"
										@change="
											(checked) =>
												toggleQuestionnaire(questionnaire.id, !!checked)
										"
										:id="`questionnaire-${questionnaire.id}`"
										size="small"
									/>
									<div class="flex-1 min-w-0">
										<label
											:for="`questionnaire-${questionnaire.id}`"
											class="text-sm font-medium cursor-pointer block truncate"
										>
											{{ questionnaire.name }}
										</label>
									</div>
								</div>
								<div v-if="questionnaires.length === 0">
									<el-empty
										description="No questionnaires found"
										:image-size="30"
									/>
								</div>
							</div>
						</el-scrollbar>
					</div>
					<div
						v-if="getQuestionnaireComponent().questionnaireIds.length > 0"
						class="text-xs text-primary-600"
					>
						{{ getQuestionnaireComponent().questionnaireIds.length }} questionnaires
						selected
					</div>
				</div>

				<!-- Quick Links -->
				<div class="space-y-2 mt-4">
					<label class="text-base font-bold">Quick Links</label>
					<p class="text-sm">
						Select external system links that users can access from this stage
					</p>
					<div class="border rounded-xl">
						<el-scrollbar max-height="160px">
							<div class="p-2">
								<template v-for="quick in quickLinks" :key="quick.id">
									<div class="flex items-center space-x-2 py-1">
										<el-checkbox
											:model-value="isQuickLinkSelected(quick.id || '')"
											@change="
												(checked) =>
													toggleQuickLink(quick.id || '', !!checked)
											"
											:id="`${quick.id}`"
											size="small"
										/>
										<div class="flex-1 min-w-0">
											<label
												:for="`${quick.id}`"
												class="text-sm font-medium cursor-pointer block truncate"
											>
												{{ quick.linkName }}
											</label>
										</div>
									</div>
									<div class="ml-5 text-xs text-text-secondary truncate">
										{{ quick.description || defaultStr }}
									</div>
									<div v-if="questionnaires.length === 0">
										<el-empty
											description="No questionnaires found"
											:image-size="30"
										/>
									</div>
								</template>
							</div>
						</el-scrollbar>
					</div>
				</div>

				<!-- File Management -->
				<div class="space-y-2 mt-4">
					<label class="text-base font-bold">File Management</label>
					<p class="text-sm">Enable file upload and attachment functionality</p>
					<div class="flex items-center space-x-2 p-2 rounded-xl bg-black-400">
						<el-switch
							:model-value="getFileComponent().isEnabled"
							@change="(val) => toggleFileComponent(!!val)"
							id="file-management"
							size="small"
						/>
						<div class="flex-1 min-w-0">
							<label
								for="file-management"
								class="text-sm leading-none font-medium cursor-pointer block truncate"
							>
								Attachment Management Needed
							</label>
							<p class="text-xs mt-1 truncate">
								Allow users to upload and manage files in this stage
							</p>
						</div>
					</div>

					<!-- Attachment Management Needed -->
					<div
						v-if="getFileComponent().isEnabled"
						class="flex items-center space-x-2 p-2 border rounded-xl bg-black-400 mt-2"
					>
						<el-switch
							:model-value="props.modelValue.attachmentManagementNeeded || false"
							@change="(val) => updateAttachmentManagementNeeded(!!val)"
							id="attachment-management-needed"
							size="small"
						/>
						<div class="flex-1 min-w-0">
							<label
								for="attachment-management-needed"
								class="text-sm leading-none font-medium cursor-pointer block truncate"
							>
								Required
							</label>
							<p class="text-xs mt-1 truncate">
								Indicates whether file upload is required for this stage
							</p>
						</div>
					</div>
				</div>
			</el-scrollbar>
		</div>
		<!-- Right Side: Selected Items -->
		<div class="space-y-4 w-full overflow-hidden">
			<el-scrollbar ref="scrollbarRefRight" class="stage-components-selector">
				<div class="space-y-2">
					<label class="text-base font-bold">Selected Items</label>
					<p class="text-sm">
						Items that will be included in this stage (drag to reorder)
					</p>
					<div
						class="border rounded-xl p-3 min-h-[600px] bg-siderbarGray dark:bg-black w-full overflow-hidden"
					>
						<draggable
							v-model="selectedItems"
							group="items"
							handle=".drag-handle"
							class="space-y-2"
							item-key="id"
							ghost-class="ghost-stage"
							@change="updateItemOrder"
							:animation="300"
						>
							<template #item="{ element, index }">
								<div
									class="border rounded-xl shadow-sm hover:shadow-md transition-all w-full overflow-hidden"
								>
									<div class="flex items-center p-2">
										<div class="flex items-center flex-1 min-w-0 gap-x-2">
											<div
												class="drag-handle flex items-center justify-center w-5 h-5 rounded-xl cursor-row-resize transition-colors flex-shrink-0"
											>
												<el-icon class="text-black dark:text-white text-sm">
													<GripVertical />
												</el-icon>
											</div>
											<span
												class="text-xs bg-primary text-white w-6 h-6 flex items-center justify-center rounded-xl font-medium min-w-0 flex-shrink-0"
											>
												{{ index + 1 }}
											</span>
											<el-icon class="text-primary-500 text-sm flex-shrink-0">
												<component :is="getItemIcon(element.type)" />
											</el-icon>
											<div class="flex-1 min-w-0">
												<el-tooltip
													:content="element.name"
													placement="top"
													:disabled="element.name.length <= 25"
												>
													<span
														class="text-sm font-medium block truncate"
													>
														{{ element.name }}
													</span>
												</el-tooltip>
												<el-tooltip
													v-if="element.description"
													:content="element.description"
													placement="top"
													:disabled="element.description.length <= 40"
												>
													<p class="text-xs mt-1 truncate">
														{{ element.description }}
													</p>
												</el-tooltip>
											</div>
										</div>
										<div class="flex items-center space-x-2 flex-shrink-0 ml-2">
											<el-tag size="small" type="primary">
												{{ getItemTypeLabel(element.type) }}
											</el-tag>
											<el-button
												size="small"
												type="danger"
												link
												@click="removeItem(element)"
											>
												<el-icon class="text-sm"><Close /></el-icon>
											</el-button>
										</div>
									</div>
									<div
										v-if="shouldShowPortalAccess"
										class="border-t p-2 flex justify-between items-center"
									>
										<div class="font-bold">Customer Portal Access</div>
										<el-dropdown
											@command="
												(value) => commandChangePortalAccess(element, value)
											"
											trigger="click"
										>
											<div
												class="cursor-pointer text-white hover:opacity-80 transition-opacity inline-flex items-center px-2 py-0.5 rounded-xl text-xs font-medium bg-primary border"
											>
												{{ getElementPortalAccessLabel(element) }}
												<el-icon class="ml-1 text-xs">
													<ArrowDown />
												</el-icon>
											</div>
											<template #dropdown>
												<el-dropdown-menu>
													<el-dropdown-item
														v-for="option in getAvailablePortalOptions"
														:key="option.value"
														:command="option.value"
													>
														<span class="text-xs" :class="option.color">
															{{ option.label }}
														</span>
													</el-dropdown-item>
												</el-dropdown-menu>
											</template>
										</el-dropdown>
									</div>
								</div>
							</template>
						</draggable>
						<div
							v-if="selectedItems.length === 0"
							class="text-center py-8 dark:text-black-400"
						>
							<p class="text-sm">No items selected</p>
							<p class="text-xs">Select items from the left panel to add them here</p>
						</div>
					</div>
				</div>
			</el-scrollbar>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import {
	Search,
	Close,
	Document,
	List,
	QuestionFilled,
	FolderOpened,
	ArrowDown,
	Link,
} from '@element-plus/icons-vue';
import GripVertical from '@assets/svg/workflow/grip-vertical.svg';
import draggable from 'vuedraggable';
import { useAdaptiveScrollbar } from '@/hooks/useAdaptiveScrollbar';
import {
	StageComponentData,
	ComponentsData,
	SelectedItem,
	Checklist,
	Questionnaire,
} from '#/onboard';
import { IQuickLink } from '#/integration';

import { StageComponentPortal } from '@/enums/appEnum';
import { defaultStr } from '@/settings/projectSetting';
import { DynamicList } from '#/dynamic';

// Props
const props = defineProps<{
	modelValue: ComponentsData;
	checklists: Checklist[];
	questionnaires: Questionnaire[];
	quickLinks: IQuickLink[];
	staticFields: DynamicList[];
}>();

// Emits
const emit = defineEmits<{
	'update:modelValue': [value: ComponentsData];
	'module-order-changed': [items: SelectedItem[]];
}>();

// 使用自适应滚动条 hook
const { scrollbarRef: scrollbarRefLeft } = useAdaptiveScrollbar(200); // 预留 120px 给底部按钮
const { scrollbarRef: scrollbarRefRight } = useAdaptiveScrollbar(200); // 预留 120px 给底部按钮

// Data
const searchQuery = ref('');
const selectedItems = ref<SelectedItem[]>([]);

// 选中的字段列表（用于拖动排序）- 使用 computed
interface SelectedFieldItem {
	id: string;
	label: string;
	isRequired: boolean;
	order: number;
}

// 标准化静态字段数据（兼容旧数据格式）
const normalizeStaticField = (
	field: string | { id: string; isRequired?: boolean; order?: number },
	index: number
): { id: string; isRequired: boolean; order: number } => {
	// 旧数据格式：string[]
	if (typeof field === 'string') {
		return { id: field, isRequired: false, order: index + 1 };
	}
	// 新数据格式：{ id, isRequired, order }
	return {
		id: field.id,
		isRequired: field.isRequired ?? false,
		order: field.order ?? index + 1,
	};
};

const selectedFieldsList = computed<SelectedFieldItem[]>({
	get() {
		const fieldsComponent = getFieldsComponent();
		const normalizedFields = fieldsComponent.staticFields.map((staticField, index) => {
			const normalized = normalizeStaticField(staticField as any, index);
			const field = props.staticFields.find((f) => f.id === normalized.id);
			return {
				id: normalized.id,
				label: field?.fieldName || normalized.id,
				isRequired: normalized.isRequired,
				order: normalized.order,
			};
		});
		// 按 order 排序
		return normalizedFields.sort((a, b) => a.order - b.order);
	},
	set(newList: SelectedFieldItem[]) {
		const newStaticFields = newList.map((item, index) => ({
			id: item.id,
			isRequired: item.isRequired,
			order: index + 1,
		}));

		updateComponent('fields', {
			staticFields: newStaticFields,
			isEnabled: newStaticFields.length > 0,
			customerPortalAccess: StageComponentPortal.Hidden,
		});
	},
});

// Computed
const filteredStaticFields = computed(() => {
	if (!searchQuery.value) {
		return props.staticFields;
	}
	return props.staticFields.filter((field) =>
		field.fieldName.toLowerCase().includes(searchQuery.value.toLowerCase())
	);
});

// 判断是否显示Portal Access按钮
const shouldShowPortalAccess = computed(() => {
	return props.modelValue.visibleInPortal === true;
});

// 获取可用的Portal Access选项
const getAvailablePortalOptions = computed(() => {
	if (!shouldShowPortalAccess.value) {
		return [];
	}

	const { portalPermission } = props.modelValue;

	// 如果Stage设置为Viewable only，只显示Hidden和View Only选项
	if (portalPermission === 1) {
		// PortalPermissionEnum.Viewable
		return [
			{ value: StageComponentPortal.Hidden, label: 'Hidden', color: 'text-gray-600' },
			{ value: StageComponentPortal.Viewable, label: 'View Only', color: 'text-blue-600' },
		];
	}

	// 如果Stage设置为Completable，显示所有三个选项
	if (portalPermission === 2) {
		// PortalPermissionEnum.Completable
		return [
			{ value: StageComponentPortal.Hidden, label: 'Hidden', color: 'text-gray-600' },
			{ value: StageComponentPortal.Viewable, label: 'View Only', color: 'text-blue-600' },
			{
				value: StageComponentPortal.Completable,
				label: 'Completable',
				color: 'text-green-600',
			},
		];
	}

	return [];
});

// Helper methods to get components
const getFieldsComponent = (): StageComponentData => {
	const components = props.modelValue.components || [];
	return (
		components.find((c) => c.key === 'fields') || {
			key: 'fields',
			order: props?.modelValue?.components?.length + 1,
			isEnabled: false,
			staticFields: [],
			checklistIds: [],
			questionnaireIds: [],
			quickLinkIds: [],
			quickLinkNames: [],
			customerPortalAccess: StageComponentPortal.Hidden,
		}
	);
};

// 获取所有checklist组件
const getChecklistComponents = (): StageComponentData[] => {
	const components = props.modelValue.components || [];
	return components.filter((c) => c.key === 'checklist');
};

// 获取所有questionnaire组件
const getQuestionnaireComponents = (): StageComponentData[] => {
	const components = props.modelValue.components || [];
	return components.filter((c) => c.key === 'questionnaires');
};

const getQuickLinkComponents = (): StageComponentData[] => {
	const components = props.modelValue.components || [];
	return components.filter((c) => c.key === 'quickLink');
};

const getFileComponent = (): StageComponentData => {
	const components = props.modelValue.components || [];
	return (
		components.find((c) => c.key === 'files') || {
			key: 'files',
			order: props?.modelValue?.components?.length + 1,
			isEnabled: false,
			staticFields: [],
			checklistIds: [],
			questionnaireIds: [],
			quickLinkIds: [],
			quickLinkNames: [],
		}
	);
};

// Helper method to update component
const updateComponent = (key: string, updates: Partial<StageComponentData>) => {
	const components = props.modelValue.components || [];
	const newComponents = [...components];
	const index = newComponents.findIndex((c) => c.key === key);
	if (index >= 0) {
		newComponents[index] = { ...newComponents[index], ...updates };
	} else {
		const defaultComponent: StageComponentData = {
			key: key as any,
			order: components.length + 1,
			isEnabled: false,
			staticFields: [],
			checklistIds: [],
			questionnaireIds: [],
			checklistNames: [],
			quickLinkIds: [],
			quickLinkNames: [],
			questionnaireNames: [],
		};
		newComponents.push({ ...defaultComponent, ...updates });
	}
	const newModelValue = {
		...props.modelValue,
		components: newComponents
			.filter((item) => item.isEnabled)
			.map((item, index) => {
				return { ...item, order: index + 1 };
			}),
	};
	// 发送整个 formData 对象，只更新 components 字段
	emit('update:modelValue', newModelValue);
};

// 添加新的组件项
const addComponentItem = (StageComponentData: StageComponentData) => {
	const components = props.modelValue.components || [];
	const newComponents = [...components, StageComponentData];
	const newModelValue = { ...props.modelValue, components: newComponents };
	emit('update:modelValue', newModelValue);
};

// 删除组件项
const removeComponentItem = (predicate: (component: StageComponentData) => boolean) => {
	const components = props.modelValue.components || [];
	const newComponents = components.filter((c) => !predicate(c));
	const newModelValue = { ...props.modelValue, components: newComponents };
	emit('update:modelValue', newModelValue);
};

// 更新所有组件
const updateAllComponents = (newComponents: StageComponentData[]) => {
	const newModelValue = { ...props.modelValue, components: newComponents };
	emit('update:modelValue', newModelValue);
};

// Methods
const isFieldSelected = (fieldId: string): boolean => {
	const staticFields = getFieldsComponent().staticFields || [];
	return staticFields.some((item) => {
		// 兼容旧数据格式（string）和新数据格式（object）
		if (typeof item === 'string') {
			return item === fieldId;
		}
		return item?.id === fieldId;
	});
};

const isChecklistSelected = (checklistId: string): boolean => {
	const checklistComponents = getChecklistComponents();
	return checklistComponents.some((c) => c.checklistIds.includes(checklistId));
};

const isQuestionnaireSelected = (questionnaireId: string): boolean => {
	const questionnaireComponents = getQuestionnaireComponents();
	return questionnaireComponents.some((c) => c.questionnaireIds.includes(questionnaireId));
};

const isQuickLinkSelected = (quickLinkId: string): boolean => {
	const quickLinkComponents = getQuickLinkComponents();
	return quickLinkComponents.some((c) => c.quickLinkIds.includes(quickLinkId));
};

const toggleField = (fieldId: string, checked: boolean) => {
	const fieldsComponent = getFieldsComponent();
	// 标准化现有字段数据
	const normalizedFields = fieldsComponent.staticFields.map((field, index) =>
		normalizeStaticField(field as any, index)
	);

	if (checked) {
		const existingIndex = normalizedFields.findIndex((item) => item.id === fieldId);
		if (existingIndex === -1) {
			normalizedFields.push({
				id: fieldId,
				isRequired: false,
				order: normalizedFields.length + 1,
			});
		}
	} else {
		const index = normalizedFields.findIndex((item) => item.id === fieldId);
		if (index > -1) {
			normalizedFields.splice(index, 1);
			// 重新计算 order
			normalizedFields.forEach((field, idx) => {
				field.order = idx + 1;
			});
		}
	}

	updateComponent('fields', {
		staticFields: normalizedFields,
		isEnabled: normalizedFields.length > 0,
		customerPortalAccess: StageComponentPortal.Hidden,
	});
};

const toggleChecklist = (checklistId: string, checked: boolean) => {
	if (checked) {
		// 检查是否已经存在
		if (!isChecklistSelected(checklistId)) {
			// 获取checklist名称
			const checklist = props.checklists.find((c) => c.id === checklistId);
			const checklistName = checklist ? checklist.name : '';

			// 创建新的checklist组件
			const newOrder = (props.modelValue.components || []).length + 1;
			const newComponent: StageComponentData = {
				key: 'checklist',
				order: newOrder,
				isEnabled: true,
				staticFields: [],
				checklistIds: [checklistId],
				checklistNames: [checklistName],
				questionnaireIds: [],
				questionnaireNames: [],
				quickLinkIds: [],
				quickLinkNames: [],
				customerPortalAccess: StageComponentPortal.Hidden,
			};
			addComponentItem(newComponent);
		}
	} else {
		// 删除包含该checklistId的组件
		removeComponentItem((c) => c.key === 'checklist' && c.checklistIds.includes(checklistId));
	}
};

const toggleQuestionnaire = (questionnaireId: string, checked: boolean) => {
	if (checked) {
		// 检查是否已经存在
		if (!isQuestionnaireSelected(questionnaireId)) {
			// 获取questionnaire名称
			const questionnaire = props.questionnaires.find((q) => q.id === questionnaireId);
			const questionnaireName = questionnaire ? questionnaire.name : '';

			// 创建新的questionnaire组件
			const newOrder = (props.modelValue.components || []).length + 1;
			const newComponent: StageComponentData = {
				key: 'questionnaires',
				order: newOrder,
				isEnabled: true,
				staticFields: [],
				checklistIds: [],
				questionnaireIds: [questionnaireId],
				checklistNames: [],
				quickLinkIds: [],
				quickLinkNames: [],
				questionnaireNames: [questionnaireName],
				customerPortalAccess: StageComponentPortal.Hidden,
			};
			addComponentItem(newComponent);
		}
	} else {
		// 删除包含该questionnaireId的组件
		removeComponentItem(
			(c) => c.key === 'questionnaires' && c.questionnaireIds.includes(questionnaireId)
		);
	}
};

const toggleQuickLink = (quickLinkId: string, checked: boolean) => {
	if (checked) {
		// 检查是否已经存在
		if (!isQuickLinkSelected(quickLinkId)) {
			// 获取quickLink名称
			const quickLink = props.quickLinks.find((q) => q.id === quickLinkId);
			const quickLinkName = quickLink ? quickLink.linkName : '';
			const newOrder = (props.modelValue.components || []).length + 1;
			const newComponent: StageComponentData = {
				key: 'quickLink',
				order: newOrder,
				isEnabled: true,
				staticFields: [],
				checklistIds: [],
				checklistNames: [],
				questionnaireIds: [],
				questionnaireNames: [],
				quickLinkIds: [quickLinkId],
				quickLinkNames: [quickLinkName],
				customerPortalAccess: StageComponentPortal.Hidden,
			};
			addComponentItem(newComponent);
		}
	} else {
		// 删除包含该quickLinkId的组件
		removeComponentItem((c) => c.key === 'quickLink' && c.quickLinkIds.includes(quickLinkId));
	}
};

const toggleFileComponent = (enabled: boolean) => {
	updateComponent('files', {
		isEnabled: enabled,
		customerPortalAccess: StageComponentPortal.Hidden,
	});
};

const updateAttachmentManagementNeeded = (needed: boolean) => {
	const newModelValue = { ...props.modelValue, attachmentManagementNeeded: needed };
	emit('update:modelValue', newModelValue);
};

// 获取元素的 Portal Access 标签
const getElementPortalAccessLabel = (element: SelectedItem): string => {
	const availableOptions = getAvailablePortalOptions.value;

	// 如果当前值在可用选项中，直接返回对应标签
	const currentOption = availableOptions.find(
		(option) => option.value === element.customerPortalAccess
	);
	if (currentOption) {
		return currentOption.label;
	}

	// 如果当前值不在可用选项中，返回第一个可用选项的标签
	if (availableOptions.length > 0) {
		return availableOptions[0].label;
	}

	// 默认返回
	return 'View Only';
};

const commandChangePortalAccess = (element: SelectedItem, value) => {
	// 检查选择的值是否在允许的选项中
	const availableOptions = getAvailablePortalOptions.value;
	const isValidOption = availableOptions.some((option) => option.value === value);

	if (!isValidOption) {
		console.warn('Invalid portal access option selected:', value);
		return;
	}

	element.customerPortalAccess = value;
	updateItemOrder();
};

// 移除字段tag
const removeFieldTag = (fieldId: string) => {
	const fieldsComponent = getFieldsComponent();
	// 标准化并过滤
	const normalizedFields = fieldsComponent.staticFields
		.map((field, index) => normalizeStaticField(field as any, index))
		.filter((item) => item.id !== fieldId);
	// 重新计算 order
	normalizedFields.forEach((field, idx) => {
		field.order = idx + 1;
	});

	updateComponent('fields', {
		staticFields: normalizedFields,
		isEnabled: normalizedFields.length > 0,
		customerPortalAccess: StageComponentPortal.Hidden,
	});
};

// 更新字段排序（draggable @change 事件触发）
const updateFieldsOrder = () => {
	// computed setter 已经处理了更新逻辑，这里不需要额外操作
};

// 切换字段必填状态
const toggleFieldRequired = (fieldId: string, isRequired: boolean) => {
	const fieldsComponent = getFieldsComponent();
	// 标准化并更新
	const normalizedFields = fieldsComponent.staticFields.map((field, index) => {
		const normalized = normalizeStaticField(field as any, index);
		if (normalized.id === fieldId) {
			return { ...normalized, isRequired };
		}
		return normalized;
	});

	updateComponent('fields', {
		staticFields: normalizedFields,
		isEnabled: normalizedFields.length > 0,
		customerPortalAccess: StageComponentPortal.Hidden,
	});
};

// 更新右侧显示的项目
const updateItemsDisplay = () => {
	const newSelectedItems: SelectedItem[] = [];

	// 获取当前可用的Portal Access选项
	const availableOptions = getAvailablePortalOptions.value;
	const getValidPortalAccess = (componentAccess?: number) => {
		// 如果没有可用选项，返回undefined
		if (availableOptions.length === 0) {
			return undefined;
		}

		// 如果组件的当前设置在可用选项中，保持不变
		if (
			componentAccess &&
			availableOptions.some((option) => option.value === componentAccess)
		) {
			return componentAccess;
		}

		// 否则使用第一个可用选项作为默认值
		return availableOptions[0].value;
	};

	// 根据组件数据生成选中项目
	const components = props.modelValue.components || [];
	components.forEach((component) => {
		if (component.isEnabled) {
			switch (component.key) {
				case 'fields':
					// Required Fields 保持显示为一个模块
					newSelectedItems.push({
						...component,
						id: component.key,
						name: 'Fields',
						description: `${component.staticFields.length} fields selected`,
						type: 'fields',
						order: component.order,
						key: component.key,
						customerPortalAccess: getValidPortalAccess(component?.customerPortalAccess),
					});
					break;
				case 'checklist':
					// 每个checklist组件对应一个独立的checklist项
					component.checklistIds.forEach((checklistId, index) => {
						// 优先使用names字段中的名称，没有则查找实际对象
						let checklistName = component.checklistNames?.[index];
						let checklistDescription = '';

						if (!checklistName) {
							const checklist = props.checklists.find((c) => c.id === checklistId);
							checklistName = checklist?.name || 'Unknown Checklist';
							checklistDescription = checklist?.description || '';
						}

						newSelectedItems.push({
							...component,
							id: `checklist-${checklistId}`,
							name: checklistName,
							description: checklistDescription,
							type: 'checklist',
							order: component.order,
							key: checklistId,
							customerPortalAccess: getValidPortalAccess(
								component?.customerPortalAccess
							),
						});
					});
					break;
				case 'questionnaires':
					// 每个questionnaire组件对应一个独立的questionnaire项
					component.questionnaireIds.forEach((questionnaireId, index) => {
						// 优先使用names字段中的名称，没有则查找实际对象
						let questionnaireName = component.questionnaireNames?.[index];
						let questionnaireDescription = '';

						if (!questionnaireName) {
							const questionnaire = props.questionnaires.find(
								(q) => q.id === questionnaireId
							);
							questionnaireName = questionnaire?.name || 'Unknown Questionnaire';
							questionnaireDescription = questionnaire?.description || '';
						}

						newSelectedItems.push({
							...component,
							id: `questionnaire-${questionnaireId}`,
							name: questionnaireName,
							description: questionnaireDescription,
							type: 'questionnaires',
							order: component.order,
							key: questionnaireId,
							customerPortalAccess: getValidPortalAccess(
								component?.customerPortalAccess
							),
						});
					});
					break;
				case 'files':
					// Files 保持显示为一个模块
					newSelectedItems.push({
						...component,
						id: component.key,
						name: 'File Attachments',
						description: 'Upload and manage files in this stage',
						type: 'files',
						order: component.order,
						key: component.key,
						customerPortalAccess: getValidPortalAccess(component?.customerPortalAccess),
					});
					break;
				case 'quickLink':
					// Quick Link 保持显示为一个模块
					component.quickLinkIds.forEach((quick, index) => {
						const quickLink = props.quickLinks.find((q) => q.id === quick);
						const quickLinkName = quickLink?.linkName || 'Unknown Quick Link';
						const quickLinkDescription = quickLink?.description || '';

						newSelectedItems.push({
							...component,
							id: `quickLink-${quick}`,
							name: quickLinkName,
							description: quickLinkDescription,
							type: 'quickLink',
							order: component.order,
							key: quick,
							customerPortalAccess: getValidPortalAccess(
								component?.customerPortalAccess
							),
						});
					});
					break;
			}
		}
	});
	// 按顺序排序

	selectedItems.value = newSelectedItems.sort((a, b) => a.order - b.order);
};

const removeItem = (item: SelectedItem) => {
	if (item.type === 'fields') {
		// 删除整个 fields 组件
		updateComponent('fields', {
			isEnabled: false,
			staticFields: [],
			checklistIds: [],
			questionnaireIds: [],
		});
	} else if (item.type === 'checklist') {
		// 删除包含该checklistId的组件
		removeComponentItem((c) => c.key === 'checklist' && c.checklistIds.includes(item.key));
	} else if (item.type === 'questionnaires') {
		// 删除包含该questionnaireId的组件
		removeComponentItem(
			(c) => c.key === 'questionnaires' && c.questionnaireIds.includes(item.key)
		);
	} else if (item.type === 'files') {
		// 删除整个 files 组件
		updateComponent('files', {
			isEnabled: false,
			staticFields: [],
			checklistIds: [],
			questionnaireIds: [],
		});
	}
};

const updateItemOrder = () => {
	// 重新构建components数组
	const newComponents: StageComponentData[] = [];
	console.log('selectedItems.value', selectedItems.value);
	selectedItems.value.forEach((item, index) => {
		const order = index + 1;

		if (item.type === 'fields') {
			// 查找现有的fields组件
			const existingFieldsComponent = getFieldsComponent();
			newComponents.push({
				...existingFieldsComponent,
				order,
				customerPortalAccess: item?.customerPortalAccess,
			});
		} else if (item.type === 'checklist') {
			// 为每个checklist项创建一个组件
			newComponents.push({
				...item,
				key: 'checklist',
				order,
				isEnabled: true,
				staticFields: [],
				checklistIds: [item.key],
				questionnaireIds: [],
				quickLinkIds: [],
				quickLinkNames: [],
				customerPortalAccess: item?.customerPortalAccess,
			});
		} else if (item.type === 'questionnaires') {
			// 为每个questionnaire项创建一个组件
			newComponents.push({
				...item,
				key: 'questionnaires',
				order,
				isEnabled: true,
				staticFields: [],
				checklistIds: [],
				questionnaireIds: [item.key],
				quickLinkIds: [],
				quickLinkNames: [],
				customerPortalAccess: item?.customerPortalAccess,
			});
		} else if (item.type === 'files') {
			// 查找现有的files组件
			const existingFileComponent = getFileComponent();
			newComponents.push({
				...existingFileComponent,
				order,
				customerPortalAccess: item?.customerPortalAccess,
			});
		} else if (item.type === 'quickLink') {
			// 为每个quickLink项创建一个组件
			newComponents.push({
				...item,
				key: 'quickLink',
				order,
				isEnabled: true,
				staticFields: [],
				checklistIds: [],
				questionnaireIds: [],
				quickLinkIds: [item.key],
				quickLinkNames: [],
				customerPortalAccess: item?.customerPortalAccess,
			});
		}
	});

	// 更新所有组件
	updateAllComponents(newComponents);
	emit('module-order-changed', selectedItems.value);
};

const getItemIcon = (type: string) => {
	switch (type) {
		case 'fields':
			return Document;
		case 'checklist':
			return List;
		case 'questionnaires':
			return QuestionFilled;
		case 'files':
			return FolderOpened;
		case 'quickLink':
			return Link;
		default:
			return Document;
	}
};

const getItemTypeLabel = (type: string) => {
	switch (type) {
		case 'fields':
			return 'Fields';
		case 'checklist':
			return 'Checklist';
		case 'questionnaires':
			return 'Questionnaire';
		case 'files':
			return 'File';
		case 'quickLink':
			return 'Quick Link';
		default:
			return 'Unknown';
	}
};

// Watch for changes and update items display
watch(() => props.modelValue, updateItemsDisplay, { deep: true, immediate: true });

// 获取统计信息的辅助函数
const getChecklistComponent = (): { checklistIds: string[] } => {
	const checklistComponents = getChecklistComponents();
	const allChecklistIds = checklistComponents.flatMap((c) => c.checklistIds);
	return { checklistIds: allChecklistIds };
};

const getQuestionnaireComponent = (): { questionnaireIds: string[] } => {
	const questionnaireComponents = getQuestionnaireComponents();
	const allQuestionnaireIds = questionnaireComponents.flatMap((c) => c.questionnaireIds);
	return { questionnaireIds: allQuestionnaireIds };
};
</script>

<style scoped lang="scss">
.stage-components-selector {
	@apply pr-4 max-h-[75vh];
}

.ghost-stage {
	opacity: 0.6;
	background: var(--primary-50);
	border: 1px dashed var(--primary-500);
}

.ghost-field {
	opacity: 0.6;
	background: var(--primary-50);
	border: 1px dashed var(--primary-500);
}
</style>
