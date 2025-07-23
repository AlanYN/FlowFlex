<template>
	<div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
		<!-- Left Side: Selection Areas -->

		<div class="space-y-4 w-full overflow-hidden">
			<el-scrollbar ref="scrollbarRefLeft" class="stage-components-selector">
				<!-- Required Fields -->
				<div class="space-y-2">
					<label class="text-base font-bold text-primary-800 dark:text-primary-300">
						Required Fields
					</label>
					<p class="text-sm text-primary-600 dark:text-primary-400">
						Select fields that are required for this stage
					</p>
					<div class="border rounded-md border-primary-200">
						<div class="p-2 border-b border-primary-100">
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
									:key="field.vIfKey"
									class="flex items-center space-x-2 py-1"
								>
									<el-checkbox
										:model-value="isFieldSelected(field.vIfKey)"
										@change="(checked) => toggleField(field.vIfKey, checked)"
										:id="`field-${field.vIfKey}`"
										size="small"
									/>
									<div class="flex-1 min-w-0">
										<label
											:for="`field-${field.vIfKey}`"
											class="text-sm font-medium leading-none flex-1 cursor-pointer min-w-0"
										>
											<span class="truncate">{{ field.label }}</span>
										</label>
									</div>
								</div>
							</div>
						</el-scrollbar>
					</div>
					<div
						v-if="getFieldsComponent().staticFields.length > 0"
						class="text-xs text-primary-600"
					>
						{{ getFieldsComponent().staticFields.length }} fields selected
					</div>
				</div>

				<!-- Selected Fields Tags -->
				<div v-if="getSelectedFieldTags().length > 0" class="space-y-2">
					<label class="text-base font-bold text-primary-800 dark:text-primary-300">
						Selected Fields
					</label>
					<div class="border rounded-md p-2 border-primary-200 bg-primary-50">
						<div class="flex flex-wrap gap-1">
							<el-tag
								v-for="fieldTag in getSelectedFieldTags()"
								:key="fieldTag.key"
								size="small"
								type="primary"
								closable
								@close="removeFieldTag(fieldTag.key)"
								class="max-w-[200px]"
							>
								<span class="truncate">{{ fieldTag.label }}</span>
							</el-tag>
						</div>
					</div>
				</div>

				<!-- Checklists -->
				<div class="space-y-2 mt-4">
					<label class="text-base font-bold text-primary-800 dark:text-primary-300">
						Checklists
					</label>
					<p class="text-sm text-primary-600 dark:text-primary-400">
						Select checklists to include in this stage
					</p>
					<div class="border rounded-md border-primary-200">
						<el-scrollbar max-height="160px">
							<div class="p-2">
								<div
									v-for="checklist in checklists"
									:key="checklist.id"
									class="flex items-start space-x-2 py-1"
								>
									<el-checkbox
										:model-value="isChecklistSelected(checklist.id)"
										@change="
											(checked) => toggleChecklist(checklist.id, checked)
										"
										:id="`checklist-${checklist.id}`"
										size="small"
									/>
									<div class="flex-1 min-w-0">
										<label
											:for="`checklist-${checklist.id}`"
											class="text-sm leading-none font-medium cursor-pointer block truncate"
										>
											{{ checklist.name }}
										</label>
										<p class="text-xs mt-1 truncate">
											{{ checklist.description }}
										</p>
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
					<label class="text-base font-bold text-primary-800 dark:text-primary-300">
						Questionnaires
					</label>
					<p class="text-sm text-primary-600 dark:text-primary-400">
						Select questionnaires to include in this stage
					</p>
					<div class="border rounded-md border-primary-200">
						<el-scrollbar max-height="160px">
							<div class="p-2">
								<div
									v-for="questionnaire in questionnaires"
									:key="questionnaire.id"
									class="flex items-start space-x-2 py-1"
								>
									<el-checkbox
										:model-value="isQuestionnaireSelected(questionnaire.id)"
										@change="
											(checked) =>
												toggleQuestionnaire(questionnaire.id, checked)
										"
										:id="`questionnaire-${questionnaire.id}`"
										size="small"
									/>
									<div class="flex-1 min-w-0">
										<label
											:for="`questionnaire-${questionnaire.id}`"
											class="text-sm leading-none font-medium cursor-pointer block truncate"
										>
											{{ questionnaire.name }}
										</label>
										<p class="text-xs mt-1 truncate">
											{{ questionnaire.description }}
										</p>
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

				<!-- File Management -->
				<div class="space-y-2 mt-4">
					<label class="text-base font-bold text-primary-800 dark:text-primary-300">
						File Management
					</label>
					<p class="text-sm text-primary-600 dark:text-primary-400">
						Enable file upload and attachment functionality
					</p>
					<div
						class="flex items-center space-x-2 p-2 border rounded-md border-primary-200 bg-primary-50"
					>
						<el-switch
							:model-value="getFileComponent().isEnabled"
							@change="(val) => toggleFileComponent(val)"
							id="file-management"
							size="small"
						/>
						<div class="flex-1 min-w-0 dark:text-black-400">
							<label
								for="file-management"
								class="text-sm leading-none font-medium cursor-pointer block truncate"
							>
								Enable File Attachments
							</label>
							<p class="text-xs mt-1 truncate">
								Allow users to upload and manage files in this stage
							</p>
						</div>
					</div>
				</div>
			</el-scrollbar>
		</div>
		<!-- Right Side: Selected Items -->
		<div class="space-y-4 w-full overflow-hidden">
			<el-scrollbar ref="scrollbarRefRight" class="stage-components-selector">
				<div class="space-y-2 mb-6">
					<label class="text-base font-bold text-primary-800 dark:text-primary-300">
						Selected Items
					</label>
					<p class="text-sm text-primary-600 dark:text-primary-400">
						Items that will be included in this stage (drag to reorder)
					</p>
					<div
						class="border rounded-md p-3 min-h-[200px] border-primary-200 bg-gradient-to-br from-primary-50 to-primary-100 w-full overflow-hidden"
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
									class="flex items-center p-2 bg-white border rounded-md shadow-sm hover:shadow-md transition-all border-primary-200 w-full overflow-hidden"
								>
									<div class="flex items-center space-x-3 flex-1 min-w-0">
										<div
											class="drag-handle flex items-center justify-center w-5 h-5 bg-primary-100 rounded cursor-move hover:bg-primary-200 transition-colors flex-shrink-0"
										>
											<el-icon class="text-primary-600 text-sm">
												<GripVertical />
											</el-icon>
										</div>
										<span
											class="text-xs bg-primary-100 text-primary-800 px-2 py-1 rounded font-medium flex-shrink-0"
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
												<span class="text-sm font-medium block truncate">
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
										<el-tag
											size="small"
											class="border-primary-200"
											:type="getItemTypeColor(element.type)"
										>
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

				<!-- Lower Section: Code Editor -->
				<div class="space-y-2">
					<CodeEditor
						v-model="pythonCode"
						language="python"
						title="Python Code"
						description="Write custom Python logic for this stage"
						height="250px"
						@change="handleCodeChange"
					/>
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
} from '@element-plus/icons-vue';
import GripVertical from '@assets/svg/workflow/grip-vertical.svg';
import draggable from 'vuedraggable';
import staticFieldsData from '../static-field.json';
import { useAdaptiveScrollbar } from '@/hooks/useAdaptiveScrollbar';
import CodeEditor from '@/components/codeEditor/index.vue';
import {
	ComponentData,
	ComponentsData,
	StaticField,
	SelectedItem,
	FieldTag,
	Checklist,
	Questionnaire,
} from '#/onboard';

// Props
const props = defineProps<{
	modelValue: ComponentsData;
	checklists: Checklist[];
	questionnaires: Questionnaire[];
}>();

// Emits
const emit = defineEmits<{
	'update:modelValue': [value: ComponentsData];
	'module-order-changed': [items: SelectedItem[]];
}>();

// 使用自适应滚动条 hook
const { scrollbarRef: scrollbarRefLeft } = useAdaptiveScrollbar(200); // 预留 120px 给底部按钮
const { scrollbarRef: scrollbarRefRight } = useAdaptiveScrollbar(200); // 增加高度以适应代码编辑器

// Data
const searchQuery = ref('');
const staticFields = ref<StaticField[]>(staticFieldsData.formFields);
const selectedItems = ref<SelectedItem[]>([]);
const pythonCode = ref(`# 在这里编写Python代码
# 示例：处理表单数据
def process_form_data(data):
    """
    处理表单数据的示例函数
    """
    result = {}
    
    # 验证必填字段
    required_fields = ['lead_id', 'company_name', 'contact_name']
    for field in required_fields:
        if field not in data or not data[field]:
            raise ValueError(f"必填字段 {field} 不能为空")
    
    # 处理数据
    result['processed'] = True
    result['timestamp'] = datetime.now().isoformat()
    
    return result

# 使用示例
# data = {
#     'lead_id': 'LEAD001',
#     'company_name': '示例公司',
#     'contact_name': '张三',
#     'contact_email': 'zhangsan@example.com'
# }
# 
# result = process_form_data(data)
# print(result)
`);

// Computed
const filteredStaticFields = computed(() => {
	if (!searchQuery.value) {
		return staticFields.value;
	}
	return staticFields.value.filter((field) =>
		field.label.toLowerCase().includes(searchQuery.value.toLowerCase())
	);
});

// Helper methods to get components
const getFieldsComponent = (): ComponentData => {
	const components = props.modelValue.components || [];
	return (
		components.find((c) => c.key === 'fields') || {
			key: 'fields',
			order: 1,
			isEnabled: false,
			staticFields: [],
			checklistIds: [],
			questionnaireIds: [],
		}
	);
};

// 获取所有checklist组件
const getChecklistComponents = (): ComponentData[] => {
	const components = props.modelValue.components || [];
	return components.filter((c) => c.key === 'checklist');
};

// 获取所有questionnaire组件
const getQuestionnaireComponents = (): ComponentData[] => {
	const components = props.modelValue.components || [];
	return components.filter((c) => c.key === 'questionnaires');
};

const getFileComponent = (): ComponentData => {
	const components = props.modelValue.components || [];
	return (
		components.find((c) => c.key === 'files') || {
			key: 'files',
			order: 4,
			isEnabled: false,
			staticFields: [],
			checklistIds: [],
			questionnaireIds: [],
		}
	);
};

// Helper method to update component
const updateComponent = (key: string, updates: Partial<ComponentData>) => {
	const components = props.modelValue.components || [];
	const newComponents = [...components];
	const index = newComponents.findIndex((c) => c.key === key);

	if (index >= 0) {
		newComponents[index] = { ...newComponents[index], ...updates };
	} else {
		const defaultComponent: ComponentData = {
			key: key as any,
			order:
				key === 'fields' ? 1 : key === 'checklist' ? 2 : key === 'questionnaires' ? 3 : 4,
			isEnabled: false,
			staticFields: [],
			checklistIds: [],
			questionnaireIds: [],
			checklistNames: [],
			questionnaireNames: [],
		};
		newComponents.push({ ...defaultComponent, ...updates });
	}

	const newModelValue = { ...props.modelValue, components: newComponents };
	// 发送整个 formData 对象，只更新 components 字段
	emit('update:modelValue', newModelValue);
};

// 添加新的组件项
const addComponentItem = (componentData: ComponentData) => {
	const components = props.modelValue.components || [];
	const newComponents = [...components, componentData];
	const newModelValue = { ...props.modelValue, components: newComponents };
	emit('update:modelValue', newModelValue);
};

// 删除组件项
const removeComponentItem = (predicate: (component: ComponentData) => boolean) => {
	const components = props.modelValue.components || [];
	const newComponents = components.filter((c) => !predicate(c));
	const newModelValue = { ...props.modelValue, components: newComponents };
	emit('update:modelValue', newModelValue);
};

// 更新所有组件
const updateAllComponents = (newComponents: ComponentData[]) => {
	const newModelValue = { ...props.modelValue, components: newComponents };
	emit('update:modelValue', newModelValue);
};

// Methods
const isFieldSelected = (fieldKey: string): boolean => {
	return getFieldsComponent().staticFields.includes(fieldKey);
};

const isChecklistSelected = (checklistId: string): boolean => {
	const checklistComponents = getChecklistComponents();
	return checklistComponents.some((c) => c.checklistIds.includes(checklistId));
};

const isQuestionnaireSelected = (questionnaireId: string): boolean => {
	const questionnaireComponents = getQuestionnaireComponents();
	return questionnaireComponents.some((c) => c.questionnaireIds.includes(questionnaireId));
};

const toggleField = (fieldKey: string, checked: boolean) => {
	const fieldsComponent = getFieldsComponent();
	const newStaticFields = [...fieldsComponent.staticFields];

	if (checked) {
		if (!newStaticFields.includes(fieldKey)) {
			newStaticFields.push(fieldKey);
		}
	} else {
		const index = newStaticFields.indexOf(fieldKey);
		if (index > -1) {
			newStaticFields.splice(index, 1);
		}
	}

	updateComponent('fields', {
		staticFields: newStaticFields,
		isEnabled: newStaticFields.length > 0,
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
			const newComponent: ComponentData = {
				key: 'checklist',
				order: newOrder,
				isEnabled: true,
				staticFields: [],
				checklistIds: [checklistId],
				questionnaireIds: [],
				checklistNames: [checklistName],
				questionnaireNames: [],
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
			const newComponent: ComponentData = {
				key: 'questionnaires',
				order: newOrder,
				isEnabled: true,
				staticFields: [],
				checklistIds: [],
				questionnaireIds: [questionnaireId],
				checklistNames: [],
				questionnaireNames: [questionnaireName],
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

const toggleFileComponent = (enabled: boolean) => {
	updateComponent('files', {
		isEnabled: enabled,
	});
};

// 获取选中的字段tags
const getSelectedFieldTags = (): FieldTag[] => {
	const fieldsComponent = getFieldsComponent();
	return fieldsComponent.staticFields.map((fieldKey) => {
		const field = staticFields.value.find((f) => f.vIfKey === fieldKey);
		return {
			key: fieldKey,
			label: field?.label || fieldKey,
		};
	});
};

// 移除字段tag
const removeFieldTag = (fieldKey: string) => {
	const fieldsComponent = getFieldsComponent();
	const newStaticFields = fieldsComponent.staticFields.filter((key) => key !== fieldKey);

	updateComponent('fields', {
		staticFields: newStaticFields,
		isEnabled: newStaticFields.length > 0,
	});
};

// 更新右侧显示的项目
const updateItemsDisplay = () => {
	const newSelectedItems: SelectedItem[] = [];

	// 根据组件数据生成选中项目
	const components = props.modelValue.components || [];
	components.forEach((component) => {
		if (component.isEnabled) {
			switch (component.key) {
				case 'fields':
					// Required Fields 保持显示为一个模块
					newSelectedItems.push({
						id: component.key,
						name: 'Required Fields',
						description: `${component.staticFields.length} fields selected`,
						type: 'fields',
						order: component.order,
						key: component.key,
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
							id: `checklist-${checklistId}`,
							name: checklistName,
							description: checklistDescription,
							type: 'checklist',
							order: component.order,
							key: checklistId,
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
							id: `questionnaire-${questionnaireId}`,
							name: questionnaireName,
							description: questionnaireDescription,
							type: 'questionnaires',
							order: component.order,
							key: questionnaireId,
						});
					});
					break;
				case 'files':
					// Files 保持显示为一个模块
					newSelectedItems.push({
						id: component.key,
						name: 'File Attachments',
						description: 'Upload and manage files in this stage',
						type: 'files',
						order: component.order,
						key: component.key,
					});
					break;
			}
		}
	});

	// 按顺序排序
	newSelectedItems.sort((a, b) => a.order - b.order);

	selectedItems.value = newSelectedItems;
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
	const newComponents: ComponentData[] = [];

	selectedItems.value.forEach((item, index) => {
		const order = index + 1;

		if (item.type === 'fields') {
			// 查找现有的fields组件
			const existingFieldsComponent = getFieldsComponent();
			newComponents.push({
				...existingFieldsComponent,
				order,
			});
		} else if (item.type === 'checklist') {
			// 为每个checklist项创建一个组件
			newComponents.push({
				key: 'checklist',
				order,
				isEnabled: true,
				staticFields: [],
				checklistIds: [item.key],
				questionnaireIds: [],
			});
		} else if (item.type === 'questionnaires') {
			// 为每个questionnaire项创建一个组件
			newComponents.push({
				key: 'questionnaires',
				order,
				isEnabled: true,
				staticFields: [],
				checklistIds: [],
				questionnaireIds: [item.key],
			});
		} else if (item.type === 'files') {
			// 查找现有的files组件
			const existingFileComponent = getFileComponent();
			newComponents.push({
				...existingFileComponent,
				order,
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
		default:
			return 'Unknown';
	}
};

const getItemTypeColor = (type: string) => {
	switch (type) {
		case 'fields':
			return 'primary';
		case 'checklist':
			return 'success';
		case 'questionnaires':
			return 'warning';
		case 'files':
			return 'info';
		default:
			return '';
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

// 处理代码编辑器变化
const handleCodeChange = (value: string) => {
	// console.log('Python代码已更新:', value);
	// 这里可以添加代码验证、保存等逻辑
};
</script>

<style scoped lang="scss">
.stage-components-selector {
	@apply pr-4;
}

.search-input {
	--el-input-border-color: transparent;
	--el-input-focus-border-color: transparent;
}

.search-input :deep(.el-input__wrapper) {
	border: none;
	box-shadow: none;
}

.search-input :deep(.el-input__wrapper:focus) {
	border: none;
	box-shadow: none;
}

.ghost-stage {
	opacity: 0.6;
	background: var(--primary-50);
	border: 1px dashed var(--primary-500);
}

// 确保主题色变量在深色模式下也能正常工作
:deep(.el-empty) {
	--el-text-color-regular: var(--black-400);
}

// 确保拖拽项在深色模式下的背景色
:deep(.el-tag) {
	--el-tag-border-color: var(--primary-200);
}
</style>
