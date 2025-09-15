<template>
	<div class="variables-panel">
		<!-- Header Section -->
		<div class="p-4 bg-gray-50 dark:bg-gray-800 rounded-lg">
			<div class="flex items-center justify-between mb-3">
				<div class="flex items-center space-x-2">
					<icon icon="tabler:variable-plus" class="text-primary-500" />
					<h4 class="font-medium text-gray-800 dark:text-white">Available Variables</h4>
				</div>
			</div>
			<p class="text-sm text-gray-600 dark:text-gray-400">
				Click on any variable to copy it to clipboard, then paste into your code or
				configuration.
			</p>
		</div>

		<div class="variables-content">
			<!-- Variables and Examples Section -->
			<el-tabs v-model="activeTab" type="">
				<el-tab-pane label="All" name="all">
					<div class="context-structure-section p-4 rounded-lg">
						<div class="bg-gray-50 dark:bg-gray-800 p-4">
							<div
								class="flex items-center justify-between cursor-pointer transition-colors"
							>
								<div class="flex items-center space-x-2">
									<icon icon="tabler:variable-plus" class="text-primary-500" />
									<h5 class="font-semibold text-gray-800 dark:text-white">
										Context Parameter Structure
									</h5>
								</div>
								<div class="flex items-center space-x-2">
									<el-button
										type="primary"
										:icon="DocumentCopy"
										@click.stop="copyStructure(contextStructure)"
										class="mr-2"
									>
										Copy Structure
									</el-button>
								</div>
							</div>
							<div class="text-sm text-gray-600 dark:text-gray-400 mt-2">
								Complete structure of the context parameter passed to your Python
								main() function
							</div>
						</div>

						<div class="context-code-block">
							<pre class="context-structure-pre">{{ contextStructure }}</pre>
						</div>
					</div>
				</el-tab-pane>
				<el-tab-pane label="Workflow" name="context">
					<div class="grid grid-cols-1 lg:grid-cols-2 gap-6 p p-4">
						<!-- Basic Event Data -->
						<div class="variable-category">
							<div class="category-header">
								<el-icon class="text-blue-500" size="16">
									<User />
								</el-icon>
								<h5 class="font-medium text-gray-700 dark:text-gray-300">
									Basic Event Data
								</h5>
							</div>
							<div class="variable-list">
								<div
									v-for="variable in basicEventVariables"
									:key="variable.name"
									class="variable-item group"
									@click="copyVariable(variable.name)"
								>
									<div class="variable-info">
										<code class="variable-name">
											{{ variable.name }}
										</code>
										<div class="variable-description truncate">
											{{ variable.description }}
										</div>
									</div>
									<el-button text :icon="DocumentCopy" class="copy-btn" />
								</div>
							</div>
						</div>

						<!-- Workflow Data -->
						<div class="variable-category">
							<div class="category-header">
								<el-icon class="text-green-500" size="16">
									<Flag />
								</el-icon>
								<h5 class="font-medium text-gray-700 dark:text-gray-300">
									Workflow & Stage Data
								</h5>
							</div>
							<div class="variable-list">
								<div
									v-for="variable in workflowVariables"
									:key="variable.name"
									class="variable-item group"
									@click="copyVariable(variable.name)"
								>
									<div class="variable-info">
										<code class="variable-name truncate">
											{{ variable.name }}
										</code>
										<span class="variable-description truncate">
											{{ variable.description }}
										</span>
									</div>
									<el-button text :icon="DocumentCopy" class="copy-btn" />
								</div>
							</div>
						</div>

						<!-- Business Context -->
						<div class="variable-category">
							<div class="category-header">
								<icon icon="tabler:variable-plus" class="text-primary-500" />
								<h5 class="font-medium text-gray-700 dark:text-gray-300">
									Business Context
								</h5>
							</div>
							<div class="variable-list">
								<div
									v-for="variable in businessContextVariables"
									:key="variable.name"
									class="variable-item group"
									@click="copyVariable(variable.name)"
								>
									<div class="variable-info">
										<code class="variable-name truncate">
											{{ variable.name }}
										</code>
										<span class="variable-description truncate">
											{{ variable.description }}
										</span>
									</div>
									<el-button text :icon="DocumentCopy" class="copy-btn" />
								</div>
							</div>
						</div>

						<!-- Metadata -->
						<div class="variable-category">
							<div class="category-header">
								<el-icon class="text-orange-500" size="16">
									<Document />
								</el-icon>
								<h5 class="font-medium text-gray-700 dark:text-gray-300">
									Metadata & Tags
								</h5>
							</div>
							<div class="variable-list">
								<div
									v-for="variable in metadataVariables"
									:key="variable.name"
									class="variable-item group"
									@click="copyVariable(variable.name)"
								>
									<div class="variable-info">
										<code class="variable-name truncate">
											{{ variable.name }}
										</code>
										<span class="variable-description truncate">
											{{ variable.description }}
										</span>
									</div>
									<el-button text :icon="DocumentCopy" class="copy-btn" />
								</div>
							</div>
						</div>
					</div>
				</el-tab-pane>

				<el-tab-pane label="Components" name="components">
					<div class="grid grid-cols-1 gap-6 p-4">
						<!-- Checklists -->
						<div class="variable-category">
							<div class="category-header">
								<el-icon class="text-blue-600" size="16">
									<Document />
								</el-icon>
								<h5 class="font-medium text-gray-700 dark:text-gray-300">
									Checklists & Tasks
								</h5>
							</div>
							<div class="variable-list">
								<div
									v-for="variable in checklistVariables"
									:key="variable.name"
									class="variable-item group"
									@click="copyVariable(variable.name)"
								>
									<div class="variable-info">
										<code class="variable-name">
											{{ variable.name }}
										</code>
										<div class="variable-description truncate">
											{{ variable.description }}
										</div>
									</div>
									<el-button text :icon="DocumentCopy" class="copy-btn" />
								</div>
							</div>
						</div>

						<!-- Required Fields -->
						<div class="variable-category">
							<div class="category-header">
								<el-icon class="text-red-500" size="16">
									<Flag />
								</el-icon>
								<h5 class="font-medium text-gray-700 dark:text-gray-300">
									Required Fields
								</h5>
							</div>
							<div class="variable-list">
								<div
									v-for="variable in requiredFieldVariables"
									:key="variable.name"
									class="variable-item group"
									@click="copyVariable(variable.name)"
								>
									<div class="variable-info">
										<code class="variable-name">
											{{ variable.name }}
										</code>
										<div class="variable-description truncate">
											{{ variable.description }}
										</div>
									</div>
									<el-button text :icon="DocumentCopy" class="copy-btn" />
								</div>
							</div>
						</div>

						<!-- Form Responses (copyable fields) -->
						<div class="variable-category">
							<div class="category-header">
								<el-icon class="text-purple-500" size="16">
									<Document />
								</el-icon>
								<h5 class="font-medium text-gray-700 dark:text-gray-300">
									Form Responses
								</h5>
							</div>
							<div class="variable-list">
								<div
									v-for="variable in formResponseVariables"
									:key="variable.name"
									class="variable-item group"
									@click="copyVariable(variable.name)"
								>
									<div class="variable-info">
										<code class="variable-name">
											{{ variable.name }}
										</code>
										<div class="variable-description truncate">
											{{ variable.description }}
										</div>
									</div>
									<el-button text :icon="DocumentCopy" class="copy-btn" />
								</div>
							</div>
						</div>
					</div>
				</el-tab-pane>

				<el-tab-pane label="Examples" name="examples">
					<div class="examples-section p-4">
						<div
							class="example-category"
							v-for="example in variableExamples"
							:key="example.title"
						>
							<h5 class="example-title">{{ example.title }}</h5>
							<p class="example-description">{{ example.description }}</p>
							<div class="example-code">
								<pre>{{ example.code }}</pre>
								<el-button
									text
									:icon="DocumentCopy"
									@click="copyToClipboard(example.code)"
									class="copy-example-btn"
								>
									Copy
								</el-button>
							</div>
						</div>
					</div>
				</el-tab-pane>
			</el-tabs>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { ElMessage } from 'element-plus';
import { User, Flag, Document, DocumentCopy } from '@element-plus/icons-vue';
import { ActionType } from '@/apis/action';

interface Variable {
	name: string;
	description: string;
	example?: string;
}

interface Props {
	stageId?: string;
	actionType?: ActionType;
}

const props = withDefaults(defineProps<Props>(), {
	stageId: '',
	actionType: ActionType.PYTHON_SCRIPT,
});

// State
const activeTab = ref('all');

// Variables categorized by type
const basicEventVariables: Variable[] = [
	{ name: 'context.timestamp', description: 'Event timestamp' },
	{ name: 'context.tenantId', description: 'Tenant identifier' },
	{ name: 'context.onboardingId', description: 'Onboarding identifier' },
	{ name: 'context.leadId', description: 'Lead identifier' },
	{ name: 'context.responsibleTeam', description: 'Team responsible for this stage' },
	{ name: 'context.assigneeId', description: 'Assigned user ID' },
	{ name: 'context.assigneeName', description: 'Assigned user name' },
];

const workflowVariables: Variable[] = [
	{ name: 'context.workflowId', description: 'Workflow identifier' },
	{ name: 'context.workflowName', description: 'Workflow display name' },
	{ name: 'context.completedStageId', description: 'Completed stage identifier' },
	{ name: 'context.completedStageName', description: 'Completed stage name' },
	{ name: 'context.stageCategory', description: 'Stage category' },
	{ name: 'context.nextStageId', description: 'Next stage identifier' },
	{ name: 'context.nextStageName', description: 'Next stage name' },
	{ name: 'context.completionRate', description: 'Overall completion percentage' },
	{ name: 'context.isFinalStage', description: 'Whether this is the final stage' },
];

const businessContextVariables: Variable[] = [
	{
		name: 'context.businessContext.CompletionMethod',
		description: 'How the stage was completed',
	},
	{ name: 'context.businessContext.AutoMoveToNext', description: 'Auto progression flag' },
	{ name: 'context.businessContext.CompletionNotes', description: 'Stage completion notes' },
	{
		name: 'context.businessContext["Components.ChecklistsCount"]',
		description: 'Number of checklists in this stage',
	},
	{
		name: 'context.businessContext["Components.QuestionnairesCount"]',
		description: 'Number of questionnaires in this stage',
	},
	{
		name: 'context.businessContext["Components.TaskCompletionsCount"]',
		description: 'Number of completed tasks',
	},
	{
		name: 'context.businessContext["Components.RequiredFieldsCount"]',
		description: 'Number of required fields',
	},
];

const metadataVariables: Variable[] = [
	{ name: 'context.routingTags', description: 'Array of routing tags for event processing' },
	{ name: 'context.priority', description: 'Event priority level' },
	{ name: 'context.source', description: 'Source system that generated the event' },
	{ name: 'context.relatedEntity', description: 'Related entity information' },
	{ name: 'context.description', description: 'Human-readable event description' },
	{ name: 'context.tags', description: 'Array of general tags' },
];

const checklistVariables: Variable[] = [
	{ name: 'context.components.checklists', description: 'Array of checklist data' },
	{ name: 'context.components.checklists[i].checklistId', description: 'Checklist identifier' },
	{ name: 'context.components.checklists[i].checklistName', description: 'Checklist name' },
	{ name: 'context.components.checklists[i].description', description: 'Checklist description' },
	{
		name: 'context.components.checklists[i].completionRate',
		description: 'Checklist completion percentage',
	},
	{ name: 'context.components.checklists[i].totalTasks', description: 'Total number of tasks' },
	{
		name: 'context.components.checklists[i].completedTasks',
		description: 'Number of completed tasks',
	},
	{ name: 'context.components.checklists[i].tasks', description: 'Array of tasks in checklist' },
	{ name: 'context.components.taskCompletions', description: 'Array of task completion records' },
];

const requiredFieldVariables: Variable[] = [
	{ name: 'context.components.requiredFields', description: 'Array of required field data' },
	{
		name: 'context.components.requiredFields[i].fieldName',
		description: 'Field name identifier',
	},
	{
		name: 'context.components.requiredFields[i].displayName',
		description: 'Human-readable field name',
	},
	{ name: 'context.components.requiredFields[i].fieldType', description: 'Field data type' },
	{
		name: 'context.components.requiredFields[i].isRequired',
		description: 'Whether field is required',
	},
	{ name: 'context.components.requiredFields[i].fieldValue', description: 'Current field value' },
	{
		name: 'context.components.requiredFields[i].validationStatus',
		description: 'Validation status',
	},
	{
		name: 'context.components.requiredFields[i].validationErrors',
		description: 'Array of validation errors',
	},
];

const formResponseVariables: Variable[] = [
	{
		name: 'context.components.questionnaireAnswers',
		description: 'Array of questionnaire answer records',
	},
	{
		name: 'context.components.questionnaireAnswers[i].answerId',
		description: 'Answer record identifier',
	},
	{
		name: 'context.components.questionnaireAnswers[i].questionnaireId',
		description: 'Related questionnaire ID',
	},
	{
		name: 'context.components.questionnaireAnswers[i].questionId',
		description: 'Question identifier',
	},
	{
		name: 'context.components.questionnaireAnswers[i].questionText',
		description: 'Question text',
	},
	{
		name: 'context.components.questionnaireAnswers[i].questionType',
		description: 'Question type',
	},
	{
		name: 'context.components.questionnaireAnswers[i].isRequired',
		description: 'Whether question is required',
	},
	{
		name: 'context.components.questionnaireAnswers[i].status',
		description: 'Answer status (Draft/Submitted)',
	},
	{
		name: 'context.components.questionnaireAnswers[i].answerTime',
		description: 'Answer submission time',
	},
	// Answer JSON (as string) and common nested fields in the JSON structure
	{
		name: 'context.components.questionnaireAnswers[i].answer',
		description: 'JSON string containing responses array',
	},
	{
		name: 'context.components.questionnaireAnswers[i].answer.responses',
		description: 'Array of response items (in answer JSON)',
	},
	{
		name: 'context.components.questionnaireAnswers[i].answer.responses[i].question',
		description: 'Response question text (in answer JSON)',
	},
	{
		name: 'context.components.questionnaireAnswers[i].answer.responses[i].answer',
		description: 'Response value (in answer JSON)',
	},
	{
		name: 'context.components.questionnaireAnswers[i].answer.responses[i].type',
		description: 'Response question type (in answer JSON)',
	},
	{
		name: 'context.components.questionnaireAnswers[i].answer.responses[i].responseText',
		description: 'Additional response text (in answer JSON)',
	},
	{
		name: 'context.components.questionnaireAnswers[i].answer.responses[i].changeHistory',
		description: 'Change history array (in answer JSON)',
	},
	{
		name: 'context.components.questionnaireAnswers[i].answer.responses[i].lastModifiedBy',
		description: 'Last modified by (in answer JSON)',
	},
	{
		name: 'context.components.questionnaireAnswers[i].answer.responses[i].lastModifiedAt',
		description: 'Last modified timestamp (in answer JSON)',
	},
];

// Context parameter structure for Python scripts
const contextStructure = computed(() => {
	return `context = {
    "eventId": <string>,
    "timestamp": <string>,
    "version": <string>,
    "tenantId": <string>,
    "onboardingId": <string>,
    "leadId": <string>,
    "workflowId": <string>,
    "workflowName": <string>,
    "completedStageId": <string>,
    "completedStageName": <string>,
    "stageCategory": <string>,
    "nextStageId": <string>,
    "nextStageName": <string>,
    "completionRate": <number>,
    "isFinalStage": <boolean>,
    "responsibleTeam": <string>,
    "assigneeId": <string>,
    "assigneeName": <string>,
    "businessContext": {
        "CompletionMethod": <string>,
        "AutoMoveToNext": <boolean>,
        "CompletionNotes": <string>,
        "Components.ChecklistsCount": <number>,
        "Components.QuestionnairesCount": <number>,
        "Components.TaskCompletionsCount": <number>,
        "Components.RequiredFieldsCount": <number>
    },
    "routingTags": <array>,
    "priority": <string>,
    "source": <string>,
    "relatedEntity": <string>,
    "description": <string>,
    "tags": <array>,
    "components": {
        "checklists": [{
            "checklistId": <string>,
            "checklistName": <string>,
            "description": <string>,
            "team": <string>,
            "type": <string>,
            "status": <string>,
            "isTemplate": <boolean>,
            "completionRate": <number>,
            "totalTasks": <number>,
            "completedTasks": <number>,
            "isActive": <boolean>,
            "tasks": [{
                "id": <string>,
                "checklistId": <string>,
                "name": <string>,
                "description": <string>,
                "orderIndex": <number>,
                "taskType": <string>,
                "isRequired": <boolean>,
                "estimatedHours": <number>,
                "priority": <string>,
                "isCompleted": <boolean>,
                "status": <string>,
                "isActive": <boolean>
            }]
        }],
        "taskCompletions": [{
            "checklistId": <string>,
            "taskId": <string>,
            "isCompleted": <boolean>,
            "completionNotes": <string>,
            "completedBy": <string>,
            "completedTime": <string>
        }],
        "questionnaires": [{
            "questionnaireId": <string>,
            "questionnaireName": <string>,
            "description": <string>,
            "status": <string>,
            "version": <number>,
            "category": <string>,
            "totalQuestions": <number>,
            "requiredQuestions": <number>,
            "allowDraft": <boolean>,
            "allowMultipleSubmissions": <boolean>,
            "isActive": <boolean>,
            "structureJson": <string>
        }],
        "questionnaireAnswers": [{
            "answerId": <string>,
            "questionnaireId": <string>,
            "questionId": <string>,
            "questionText": <string>,
            "questionType": <string>,
            "isRequired": <boolean>,
            "answer": <string>,
            "answerTime": <string>,
            "status": <string>
        }],
        "requiredFields": [{
            "fieldName": <string>,
            "displayName": <string>,
            "fieldType": <string>,
            "isRequired": <boolean>,
            "fieldValue": <string>,
            "validationStatus": <string>,
            "validationErrors": <array>
        }]
    }
}`;
});

// Examples based on action type
const variableExamples = computed(() => {
	if (props.actionType === ActionType.PYTHON_SCRIPT) {
		return [
			{
				title: 'Access Basic Event Information',
				description: 'Get event details from stage completion data',
				code: `event_id = context.get('eventId', '')\nworkflow_name = context.get('workflowName', '')\nprint(event_id, workflow_name)`,
			},
			{
				title: 'Read Questionnaire Answers',
				description: 'Parse responses array from answer JSON',
				code: `import json\nanswers = context.get('components', {}).get('questionnaireAnswers', [])\nfor a in answers:\n    data = json.loads(a.get('answer', '{}'))\n    for r in data.get('responses', []):\n        print(r.get('question'), r.get('answer'))`,
			},
		];
	} else if (props.actionType === ActionType.HTTP_API) {
		return [
			{
				title: 'Dynamic URL',
				description: 'Compose URL with variables',
				code: `https://api.example.com/onboarding/{{context.onboardingId}}/stages/{{context.completedStageId}}`,
			},
			{
				title: 'Minimal JSON Body',
				description: 'Include key event fields',
				code: `{"event_id":"{{context.eventId}}","workflow":"{{context.workflowName}}"}`,
			},
		];
	} else {
		return [];
	}
});

// Methods
const copyVariable = async (variableName: string) => {
	await copyToClipboard(variableName);
	ElMessage.success(`Variable ${variableName} copied to clipboard`);
};

const copyStructure = async (structure: string) => {
	await copyToClipboard(structure);
	ElMessage.success('Context structure copied to clipboard');
};

const copyToClipboard = async (text: string) => {
	try {
		await navigator.clipboard.writeText(text);
	} catch (error) {
		// Fallback for older browsers
		const textArea = document.createElement('textarea');
		textArea.value = text;
		document.body.appendChild(textArea);
		textArea.select();
		document.execCommand('copy');
		document.body.removeChild(textArea);
	}
};
</script>

<style scoped lang="scss">
.variables-panel {
	@apply overflow-hidden flex flex-col gap-4 pr-4;
}

.variables-content {
	@apply bg-white dark:bg-gray-900;
}

.context-structure-wrapper {
	@apply p-4 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800;
}

.context-header {
	@apply mb-4;
}

.context-code-block {
	@apply rounded-lg  bg-white dark:bg-gray-900;
}

.context-structure-section {
	@apply border-gray-200 dark:border-gray-700;
}

.variable-category {
	@apply space-y-3;
}

.category-header {
	@apply flex items-center space-x-2 mb-3;
}

.variable-list {
	@apply space-y-2;
}

.variable-item {
	@apply flex items-center justify-between p-3 rounded-lg border border-gray-200 dark:border-gray-600 hover:border-primary-300 dark:hover:border-primary-600 hover:bg-primary-50 dark:hover:bg-gray-700 cursor-pointer transition-all;
	min-width: 0; /* 允许flex子元素收缩 */
}

.variable-info {
	@apply flex flex-col space-y-1 flex-1;
	min-width: 0; /* 允许内容收缩 */
}

.variable-name {
	@apply font-mono text-sm text-primary-600 dark:text-primary-400 font-medium;
	max-width: 100%;
	overflow: hidden;
	white-space: nowrap;
	text-overflow: ellipsis;
}

.variable-description {
	@apply text-xs text-gray-500 dark:text-gray-400;
	max-width: 100%;
	overflow: hidden;
	white-space: nowrap;
	text-overflow: ellipsis;
}

.copy-btn {
	@apply opacity-0 group-hover:opacity-100 transition-opacity text-gray-400 hover:text-primary-500;
	flex-shrink: 0; /* 防止按钮被压缩 */
}

.questionnaire-section {
	@apply space-y-4;
}

.section-header {
	@apply flex items-center space-x-2;
}

.response-preview {
	@apply space-y-3;
}

.preview-header {
	@apply flex items-center justify-between;
}

.json-preview {
	@apply text-xs bg-gray-50 dark:bg-gray-900 p-4 rounded-lg overflow-x-auto border border-gray-200 dark:border-gray-600 font-mono;
}

.examples-section {
	@apply space-y-6;
}

.example-category {
	@apply space-y-3;
}

.example-title {
	@apply font-medium text-gray-800 dark:text-white;
}

.example-description {
	@apply text-sm text-gray-600 dark:text-gray-400;
}

.example-code {
	@apply relative;

	pre {
		@apply text-xs bg-gray-50 dark:bg-gray-900 p-4 rounded-lg overflow-x-auto border border-gray-200 dark:border-gray-600 font-mono;
	}
}

.copy-example-btn {
	@apply absolute top-2 right-2 text-gray-400 hover:text-primary-500;
}

.context-structure-section {
	@apply space-y-4;
}

.context-preview {
	@apply space-y-3;
}

.preview-header {
	@apply border-b border-gray-200 dark:border-gray-600 pb-2;
}

.context-output {
	@apply relative;
}

.context-structure-pre {
	@apply text-xs bg-gray-50 dark:bg-gray-900 p-4 rounded-lg overflow-auto font-mono text-gray-800 dark:text-gray-200 whitespace-pre-wrap;
}

// Dark mode
.dark {
	.variables-header {
		@apply bg-gray-800;
	}
}
</style>
