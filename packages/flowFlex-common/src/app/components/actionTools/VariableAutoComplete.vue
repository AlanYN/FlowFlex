<template>
	<div class="variable-autocomplete-wrapper" ref="wrapperRef">
		<el-input
			v-if="!isTextarea"
			v-bind="$attrs"
			v-model="inputValue"
			@input="handleInput"
			@keydown="handleKeydown"
			@focus="handleFocus"
			@blur="handleBlur"
			ref="inputRef"
			:placeholder="placeholder"
			:class="{ 'variables-active': showVariables }"
		/>

		<el-input
			v-else
			v-bind="$attrs"
			v-model="inputValue"
			type="textarea"
			@input="handleInput"
			@keydown="handleKeydown"
			@focus="handleFocus"
			@blur="handleBlur"
			ref="inputRef"
			:placeholder="placeholder"
			:rows="rows"
			:class="{ 'variables-active': showVariables }"
		/>

		<!-- Variables Dropdown using Teleport -->
		<teleport to="body">
			<div
				v-if="showVariables && filteredVariables.length > 0"
				class="variables-dropdown"
				ref="dropdownRef"
				:style="dropdownStyle"
			>
				<el-scrollbar class="variables-list" height="260px">
					<div
						v-for="(variable, index) in filteredVariables"
						:key="variable.name"
						:class="[
							'variable-option',
							{ 'variable-option-active': index === activeIndex },
						]"
						@click="selectVariable(variable)"
						@mouseenter="activeIndex = index"
					>
						<div class="variable-info">
							<code class="variable-name">{{ variable.name }}</code>
							<span class="variable-description">{{ variable.description }}</span>
						</div>
					</div>
				</el-scrollbar>
			</div>
		</teleport>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, nextTick, onMounted, onUnmounted, watch } from 'vue';

interface Variable {
	name: string;
	description: string;
	category?: string;
}

interface Props {
	modelValue?: string;
	placeholder?: string;
	triggerChar?: string;
	type?: 'input' | 'textarea';
	rows?: number;
}

const props = withDefaults(defineProps<Props>(), {
	modelValue: '',
	placeholder: '',
	triggerChar: '/',
	type: 'input',
	rows: 8,
});

const emit = defineEmits<{
	'update:modelValue': [value: string];
}>();

// Computed
const isTextarea = computed(() => props.type === 'textarea');

// Refs
const wrapperRef = ref<HTMLElement>();
const inputRef = ref<any>();
const dropdownRef = ref<HTMLElement>();

// State
const inputValue = ref(props.modelValue);
const showVariables = ref(false);
const activeIndex = ref(0);
const triggerPosition = ref(-1);
const searchText = ref('');

// Variables data - Complete list from VariablesPanel.vue
const allVariables: Variable[] = [
	// Basic Event Data
	{ name: 'context.eventId', description: 'Unique event identifier', category: 'basic' },
	{ name: 'context.timestamp', description: 'Event timestamp', category: 'basic' },
	{ name: 'context.version', description: 'Event version', category: 'basic' },
	{ name: 'context.tenantId', description: 'Tenant identifier', category: 'basic' },
	{ name: 'context.onboardingId', description: 'Onboarding identifier', category: 'basic' },
	{ name: 'context.leadId', description: 'Lead identifier', category: 'basic' },
	{
		name: 'context.responsibleTeam',
		description: 'Team responsible for this stage',
		category: 'basic',
	},
	{ name: 'context.assigneeId', description: 'Assigned user ID', category: 'basic' },
	{ name: 'context.assigneeName', description: 'Assigned user name', category: 'basic' },

	// Workflow Data
	{ name: 'context.workflowId', description: 'Workflow identifier', category: 'workflow' },
	{ name: 'context.workflowName', description: 'Workflow display name', category: 'workflow' },
	{
		name: 'context.completedStageId',
		description: 'Completed stage identifier',
		category: 'workflow',
	},
	{
		name: 'context.completedStageName',
		description: 'Completed stage name',
		category: 'workflow',
	},
	{ name: 'context.stageCategory', description: 'Stage category', category: 'workflow' },
	{ name: 'context.nextStageId', description: 'Next stage identifier', category: 'workflow' },
	{ name: 'context.nextStageName', description: 'Next stage name', category: 'workflow' },
	{
		name: 'context.completionRate',
		description: 'Overall completion percentage',
		category: 'workflow',
	},
	{
		name: 'context.isFinalStage',
		description: 'Whether this is the final stage',
		category: 'workflow',
	},

	// Business Context
	{
		name: 'context.businessContext.CompletionMethod',
		description: 'How the stage was completed',
		category: 'business',
	},
	{
		name: 'context.businessContext.AutoMoveToNext',
		description: 'Auto progression flag',
		category: 'business',
	},
	{
		name: 'context.businessContext.CompletionNotes',
		description: 'Stage completion notes',
		category: 'business',
	},
	{
		name: 'context.businessContext["Components.ChecklistsCount"]',
		description: 'Number of checklists in this stage',
		category: 'business',
	},
	{
		name: 'context.businessContext["Components.QuestionnairesCount"]',
		description: 'Number of questionnaires in this stage',
		category: 'business',
	},
	{
		name: 'context.businessContext["Components.TaskCompletionsCount"]',
		description: 'Number of completed tasks',
		category: 'business',
	},
	{
		name: 'context.businessContext["Components.RequiredFieldsCount"]',
		description: 'Number of required fields',
		category: 'business',
	},

	// Metadata
	{
		name: 'context.routingTags',
		description: 'Array of routing tags for event processing',
		category: 'metadata',
	},
	{ name: 'context.priority', description: 'Event priority level', category: 'metadata' },
	{
		name: 'context.source',
		description: 'Source system that generated the event',
		category: 'metadata',
	},
	{
		name: 'context.relatedEntity',
		description: 'Related entity information',
		category: 'metadata',
	},
	{
		name: 'context.description',
		description: 'Human-readable event description',
		category: 'metadata',
	},
	{ name: 'context.tags', description: 'Array of general tags', category: 'metadata' },

	// Checklists & Tasks Components
	{
		name: 'context.components.checklists',
		description: 'Array of checklist data',
		category: 'components',
	},
	{
		name: 'context.components.checklists[i].checklistId',
		description: 'Checklist identifier',
		category: 'components',
	},
	{
		name: 'context.components.checklists[i].checklistName',
		description: 'Checklist name',
		category: 'components',
	},
	{
		name: 'context.components.checklists[i].description',
		description: 'Checklist description',
		category: 'components',
	},
	{
		name: 'context.components.checklists[i].completionRate',
		description: 'Checklist completion percentage',
		category: 'components',
	},
	{
		name: 'context.components.checklists[i].totalTasks',
		description: 'Total number of tasks',
		category: 'components',
	},
	{
		name: 'context.components.checklists[i].completedTasks',
		description: 'Number of completed tasks',
		category: 'components',
	},
	{
		name: 'context.components.checklists[i].tasks',
		description: 'Array of tasks in checklist',
		category: 'components',
	},
	{
		name: 'context.components.taskCompletions',
		description: 'Array of task completion records',
		category: 'components',
	},

	// Required Fields Components
	{
		name: 'context.components.requiredFields',
		description: 'Array of required field data',
		category: 'components',
	},
	{
		name: 'context.components.requiredFields[i].fieldName',
		description: 'Field name identifier',
		category: 'components',
	},
	{
		name: 'context.components.requiredFields[i].displayName',
		description: 'Human-readable field name',
		category: 'components',
	},
	{
		name: 'context.components.requiredFields[i].fieldType',
		description: 'Field data type',
		category: 'components',
	},
	{
		name: 'context.components.requiredFields[i].isRequired',
		description: 'Whether field is required',
		category: 'components',
	},
	{
		name: 'context.components.requiredFields[i].fieldValue',
		description: 'Current field value',
		category: 'components',
	},
	{
		name: 'context.components.requiredFields[i].validationStatus',
		description: 'Validation status',
		category: 'components',
	},
	{
		name: 'context.components.requiredFields[i].validationErrors',
		description: 'Array of validation errors',
		category: 'components',
	},

	// Questionnaire Components
	{
		name: 'context.components.questionnaires',
		description: 'Array of questionnaire data',
		category: 'questionnaires',
	},
	{
		name: 'context.components.questionnaires[i].questionnaireId',
		description: 'Questionnaire identifier',
		category: 'questionnaires',
	},
	{
		name: 'context.components.questionnaires[i].questionnaireName',
		description: 'Questionnaire name',
		category: 'questionnaires',
	},
	{
		name: 'context.components.questionnaires[i].description',
		description: 'Questionnaire description',
		category: 'questionnaires',
	},
	{
		name: 'context.components.questionnaires[i].status',
		description: 'Questionnaire status',
		category: 'questionnaires',
	},
	{
		name: 'context.components.questionnaires[i].version',
		description: 'Questionnaire version',
		category: 'questionnaires',
	},
	{
		name: 'context.components.questionnaires[i].category',
		description: 'Questionnaire category',
		category: 'questionnaires',
	},
	{
		name: 'context.components.questionnaires[i].totalQuestions',
		description: 'Total number of questions',
		category: 'questionnaires',
	},
	{
		name: 'context.components.questionnaires[i].requiredQuestions',
		description: 'Number of required questions',
		category: 'questionnaires',
	},
	{
		name: 'context.components.questionnaires[i].allowDraft',
		description: 'Whether draft submissions are allowed',
		category: 'questionnaires',
	},
	{
		name: 'context.components.questionnaires[i].allowMultipleSubmissions',
		description: 'Whether multiple submissions are allowed',
		category: 'questionnaires',
	},
	{
		name: 'context.components.questionnaires[i].isActive',
		description: 'Whether questionnaire is active',
		category: 'questionnaires',
	},
	{
		name: 'context.components.questionnaires[i].structureJson',
		description: 'JSON string containing questionnaire structure',
		category: 'questionnaires',
	},

	// Questionnaire Answers
	{
		name: 'context.components.questionnaireAnswers',
		description: 'Array of questionnaire answer records',
		category: 'questionnaires',
	},
	{
		name: 'context.components.questionnaireAnswers[i].answerId',
		description: 'Answer record identifier',
		category: 'questionnaires',
	},
	{
		name: 'context.components.questionnaireAnswers[i].questionnaireId',
		description: 'Related questionnaire ID',
		category: 'questionnaires',
	},
	{
		name: 'context.components.questionnaireAnswers[i].questionId',
		description: 'Question identifier',
		category: 'questionnaires',
	},
	{
		name: 'context.components.questionnaireAnswers[i].questionText',
		description: 'Question text',
		category: 'questionnaires',
	},
	{
		name: 'context.components.questionnaireAnswers[i].questionType',
		description: 'Question type',
		category: 'questionnaires',
	},
	{
		name: 'context.components.questionnaireAnswers[i].isRequired',
		description: 'Whether question is required',
		category: 'questionnaires',
	},
	{
		name: 'context.components.questionnaireAnswers[i].answer',
		description: 'JSON string containing responses array',
		category: 'questionnaires',
	},
	{
		name: 'context.components.questionnaireAnswers[i].answerTime',
		description: 'Answer submission time',
		category: 'questionnaires',
	},
	{
		name: 'context.components.questionnaireAnswers[i].status',
		description: 'Answer status (Draft/Submitted)',
		category: 'questionnaires',
	},
];

const filteredVariables = computed(() => {
	if (!searchText.value) {
		return allVariables;
	}

	return allVariables.filter(
		(variable) =>
			variable.name.toLowerCase().includes(searchText.value.toLowerCase()) ||
			variable.description.toLowerCase().includes(searchText.value.toLowerCase())
	);
});

// Position tracking with better scroll handling
const dropdownStyle = ref<any>({});

// Track all scrollable parents
const scrollableParents = ref<Element[]>([]);

const findScrollableParents = (element: Element): Element[] => {
	const parents: Element[] = [];
	let parent = element.parentElement;

	while (parent && parent !== document.body) {
		const overflow = window.getComputedStyle(parent).overflow;
		const overflowY = window.getComputedStyle(parent).overflowY;

		if (
			overflow === 'auto' ||
			overflow === 'scroll' ||
			overflowY === 'auto' ||
			overflowY === 'scroll' ||
			parent === document.documentElement
		) {
			parents.push(parent);
		}
		parent = parent.parentElement;
	}

	// Always include window
	parents.push(window as any);
	return parents;
};

const updateDropdownPosition = () => {
	if (!wrapperRef.value || !showVariables.value) {
		dropdownStyle.value = { display: 'none' };
		return;
	}

	const rect = wrapperRef.value.getBoundingClientRect();
	const viewportHeight = window.innerHeight;
	const dropdownHeight = 260; // max-height估计值

	// 检查是否有足够空间在下方显示
	const spaceBelow = viewportHeight - rect.bottom;
	const spaceAbove = rect.top;

	let top = rect.bottom + 4;
	let maxHeight = spaceBelow - 20;

	// 如果下方空间不够，尝试显示在上方
	if (spaceBelow < dropdownHeight && spaceAbove > dropdownHeight) {
		top = rect.top - dropdownHeight - 4;
		maxHeight = spaceAbove - 20;
	}

	dropdownStyle.value = {
		position: 'fixed',
		top: `${Math.max(10, top)}px`,
		left: `${Math.max(10, rect.left)}px`,
		width: `${Math.min(rect.width, window.innerWidth - 20)}px`,
		maxHeight: `${Math.max(200, maxHeight)}px`,
		zIndex: 99999,
		display: 'block',
	};
};

// 高性能滚动监听
let ticking = false;
const requestUpdatePosition = () => {
	if (!ticking) {
		requestAnimationFrame(() => {
			updateDropdownPosition();
			ticking = false;
		});
		ticking = true;
	}
};

const addScrollListeners = () => {
	if (!wrapperRef.value) return;

	scrollableParents.value = findScrollableParents(wrapperRef.value);

	scrollableParents.value.forEach((parent) => {
		parent.addEventListener('scroll', requestUpdatePosition, { passive: true });
	});

	window.addEventListener('resize', updateDropdownPosition);
};

const removeScrollListeners = () => {
	scrollableParents.value.forEach((parent) => {
		parent.removeEventListener('scroll', requestUpdatePosition);
	});

	window.removeEventListener('resize', updateDropdownPosition);
	scrollableParents.value = [];
};

// Event handlers
const handleInput = (value: string) => {
	inputValue.value = value;
	emit('update:modelValue', value);

	// Check for trigger character
	const inputElement = isTextarea.value ? inputRef.value?.textarea : inputRef.value?.input;
	const cursorPosition = inputElement?.selectionStart || 0;
	const beforeCursor = value.substring(0, cursorPosition);
	const triggerIndex = beforeCursor.lastIndexOf(props.triggerChar);

	if (triggerIndex !== -1) {
		const afterTrigger = beforeCursor.substring(triggerIndex + 1);

		// Show variables if trigger char is at the start or preceded by space/special char
		const charBeforeTrigger = beforeCursor[triggerIndex - 1];
		const shouldShow =
			triggerIndex === 0 || !charBeforeTrigger || /\s|[^\w]/.test(charBeforeTrigger);

		if (shouldShow) {
			triggerPosition.value = triggerIndex;
			searchText.value = afterTrigger;
			showVariables.value = true;
			activeIndex.value = 0;

			// Setup scroll listeners and update position
			nextTick(() => {
				addScrollListeners();
				updateDropdownPosition();
			});
		} else {
			hideVariables();
		}
	} else {
		hideVariables();
	}
};

const handleKeydown = (event: KeyboardEvent) => {
	if (!showVariables.value) return;

	switch (event.key) {
		case 'ArrowDown':
			event.preventDefault();
			activeIndex.value = Math.min(activeIndex.value + 1, filteredVariables.value.length - 1);
			break;
		case 'ArrowUp':
			event.preventDefault();
			activeIndex.value = Math.max(activeIndex.value - 1, 0);
			break;
		case 'Enter':
			event.preventDefault();
			if (filteredVariables.value[activeIndex.value]) {
				selectVariable(filteredVariables.value[activeIndex.value]);
			}
			break;
		case 'Escape':
			event.preventDefault();
			hideVariables();
			break;
	}
};

const handleFocus = () => {
	// Optional functionality
};

const handleBlur = (event: FocusEvent) => {
	// Delay hiding to allow click on dropdown
	setTimeout(() => {
		if (!dropdownRef.value?.contains(event.relatedTarget as Node)) {
			hideVariables();
		}
	}, 150);
};

const selectVariable = (variable: Variable) => {
	const inputElement = isTextarea.value ? inputRef.value?.textarea : inputRef.value?.input;
	const cursorPosition = inputElement?.selectionStart || 0;
	const value = inputValue.value;

	// Replace from trigger position to cursor with variable wrapped in {{}}
	const beforeTrigger = value.substring(0, triggerPosition.value);
	const afterCursor = value.substring(cursorPosition);
	const variableText = `{{${variable.name}}}`;

	const newValue = beforeTrigger + variableText + afterCursor;
	inputValue.value = newValue;
	emit('update:modelValue', newValue);

	// Set cursor position after the inserted variable
	nextTick(() => {
		const newCursorPos = beforeTrigger.length + variableText.length;
		inputElement?.setSelectionRange(newCursorPos, newCursorPos);
		inputRef.value?.focus();
	});

	hideVariables();
};

const hideVariables = () => {
	showVariables.value = false;
	triggerPosition.value = -1;
	searchText.value = '';
	activeIndex.value = 0;
	dropdownStyle.value = { display: 'none' };
	removeScrollListeners();
};

// Handle clicks outside
const handleClickOutside = (event: Event) => {
	if (
		!wrapperRef.value?.contains(event.target as Node) &&
		!dropdownRef.value?.contains(event.target as Node)
	) {
		hideVariables();
	}
};

onMounted(() => {
	document.addEventListener('click', handleClickOutside);
	// Sync initial value
	inputValue.value = props.modelValue;
});

onUnmounted(() => {
	document.removeEventListener('click', handleClickOutside);
	removeScrollListeners();
});

// Watch props.modelValue changes
watch(
	() => props.modelValue,
	(newValue) => {
		inputValue.value = newValue;
	}
);
</script>

<style scoped lang="scss">
.variable-autocomplete-wrapper {
	@apply relative;
}

.variables-dropdown {
	@apply bg-white  dark:bg-gray-800 border border-gray-200 dark:border-gray-600 rounded-xl shadow-lg overflow-hidden;
	box-shadow: 0 10px 25px rgba(0, 0, 0, 0.15);
}

.variables-list {
	@apply overflow-y-auto;
}

.variable-option {
	@apply px-3 py-2 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700 border-b border-gray-100 dark:border-gray-700 last:border-b-0;
}

.variable-option-active {
	@apply bg-blue-50 dark:bg-blue-900/20 text-blue-600 dark:text-blue-400;
}

.variable-info {
	@apply space-y-1;
}

.variable-name {
	@apply block font-mono text-sm font-medium text-blue-600 dark:text-blue-400;
}

.variable-description {
	@apply block text-xs text-gray-500 dark:text-gray-400 truncate;
}

.variables-active {
	:deep(.el-input__wrapper) {
		@apply border-blue-300 dark:border-blue-600;
	}

	:deep(.el-textarea__inner) {
		@apply border-blue-300 dark:border-blue-600;
	}
}

// Scrollbar styles
.variables-list {
	&::-webkit-scrollbar {
		@apply w-1;
	}

	&::-webkit-scrollbar-track {
		@apply bg-gray-100 dark:bg-gray-700;
	}

	&::-webkit-scrollbar-thumb {
		@apply bg-gray-300 dark:bg-gray-500 rounded;
	}

	&::-webkit-scrollbar-thumb:hover {
		@apply bg-gray-400 dark:bg-gray-400;
	}
}
</style>
