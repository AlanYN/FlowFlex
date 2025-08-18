<template>
	<div class="variables-panel">
		<!-- Header Section -->
		<div class="variables-header">
			<div class="flex items-center justify-between mb-3">
				<div class="flex items-center space-x-2">
					<el-icon class="text-primary-500" size="20">
						<Operation />
					</el-icon>
					<h4 class="font-medium text-gray-800 dark:text-white">Available Variables</h4>
				</div>
				<el-button
					size="small"
					:type="showVariables ? 'primary' : 'info'"
					@click="showVariables = !showVariables"
				>
					{{ showVariables ? 'Hide' : 'Show' }} Variables
				</el-button>
			</div>
			<p class="text-sm text-gray-600 dark:text-gray-400">
				Click on any variable to copy it to clipboard, then paste into your code or
				configuration.
			</p>
		</div>

		<div v-if="showVariables" class="variables-content">
			<!-- Variables and Examples Section -->
			<div class="variables-tabs-wrapper">
				<el-tabs v-model="activeTab" class="variables-tabs">
					<el-tab-pane label="Event Variables" name="context">
						<el-scrollbar max-height="400px" class="tab-content-scrollbar">
							<div class="grid grid-cols-1 lg:grid-cols-2 gap-6 pr-4">
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
											<el-button
												size="small"
												text
												:icon="DocumentCopy"
												class="copy-btn"
											/>
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
											<el-button
												size="small"
												text
												:icon="DocumentCopy"
												class="copy-btn"
											/>
										</div>
									</div>
								</div>

								<!-- Business Context -->
								<div class="variable-category">
									<div class="category-header">
										<el-icon class="text-purple-500" size="16">
											<Operation />
										</el-icon>
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
											<el-button
												size="small"
												text
												:icon="DocumentCopy"
												class="copy-btn"
											/>
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
											<el-button
												size="small"
												text
												:icon="DocumentCopy"
												class="copy-btn"
											/>
										</div>
									</div>
								</div>
							</div>
						</el-scrollbar>
					</el-tab-pane>

					<el-tab-pane label="Components" name="components">
						<el-scrollbar max-height="400px" class="tab-content-scrollbar">
							<div class="grid grid-cols-1 gap-6 pr-4">
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
											<el-button
												size="small"
												text
												:icon="DocumentCopy"
												class="copy-btn"
											/>
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
											<el-button
												size="small"
												text
												:icon="DocumentCopy"
												class="copy-btn"
											/>
										</div>
									</div>
								</div>
							</div>
						</el-scrollbar>
					</el-tab-pane>

					<el-tab-pane label="Form Responses" name="responses">
						<el-scrollbar max-height="400px" class="tab-content-scrollbar">
							<div class="questionnaire-section">
								<div class="section-header mb-4">
									<el-icon class="text-purple-500" size="16">
										<Document />
									</el-icon>
									<h5 class="font-medium text-gray-700 dark:text-gray-300">
										Questionnaire Response Structure
									</h5>
								</div>
								<div class="response-preview">
									<div class="preview-header">
										<span class="text-sm text-gray-600 dark:text-gray-400">
											Example JSON structure of form responses:
										</span>
										<el-button
											size="small"
											text
											:icon="DocumentCopy"
											@click="copyVariable('questionnaire_responses')"
										>
											Copy Variable Name
										</el-button>
									</div>
									<pre class="json-preview">{{
										questionnaireStructurePreview
									}}</pre>
								</div>
							</div>
						</el-scrollbar>
					</el-tab-pane>

					<el-tab-pane label="Examples" name="examples">
						<el-scrollbar max-height="400px" class="tab-content-scrollbar">
							<div class="examples-section">
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
											size="small"
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
						</el-scrollbar>
					</el-tab-pane>
				</el-tabs>
			</div>

			<!-- Context Structure Section (Collapsible) -->
			<div class="context-structure-section">
				<div
					class="context-toggle-header"
					@click="showContextStructure = !showContextStructure"
				>
					<div
						class="flex items-center justify-between p-4 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors"
					>
						<div class="flex items-center space-x-2">
							<el-icon class="text-blue-500" size="18">
								<Operation />
							</el-icon>
							<h5 class="font-semibold text-gray-800 dark:text-white">
								Context Parameter Structure
							</h5>
						</div>
						<div class="flex items-center space-x-2">
							<el-button
								size="small"
								type="primary"
								:icon="DocumentCopy"
								@click.stop="copyStructure(contextStructure)"
								class="mr-2"
							>
								Copy Structure
							</el-button>
							<el-icon
								class="text-gray-500 transform transition-transform duration-200"
								:class="{ 'rotate-180': showContextStructure }"
							>
								<ArrowDown />
							</el-icon>
						</div>
					</div>
				</div>

				<el-collapse-transition>
					<div v-show="showContextStructure" class="context-content">
						<div class="p-4 border-t border-gray-200 dark:border-gray-700">
							<p class="text-sm text-gray-600 dark:text-gray-400 mb-3">
								Complete structure of the context parameter passed to your Python
								main() function
							</p>
							<div class="context-code-block">
								<el-scrollbar max-height="400px">
									<pre class="context-structure-pre">{{ contextStructure }}</pre>
								</el-scrollbar>
							</div>
						</div>
					</div>
				</el-collapse-transition>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { ElMessage, ElCollapseTransition } from 'element-plus';
import { Operation, User, Flag, Document, DocumentCopy, ArrowDown } from '@element-plus/icons-vue';

interface Variable {
	name: string;
	description: string;
	example?: string;
}

interface Props {
	stageId?: string;
	actionType?: 'python' | 'http';
}

const props = withDefaults(defineProps<Props>(), {
	stageId: '',
	actionType: 'python',
});

// State
const showVariables = ref(false);
const activeTab = ref('context');
const showContextStructure = ref(false);

// Variables categorized by type
const basicEventVariables: Variable[] = [
	{ name: 'context.eventId', description: 'Unique event identifier' },
	{ name: 'context.timestamp', description: 'Event timestamp' },
	{ name: 'context.version', description: 'Event version' },
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

// Context parameter structure for Python scripts
const contextStructure = computed(() => {
	return `context = {
    "eventId": "782d6ea0-a8d8-4c15-afb7-e2a7d3d92c45",
    "timestamp": "2025-08-18T02:55:11.0739053+00:00",
    "version": "1.0",
    "tenantId": "DEFAULT",
    "onboardingId": 1956197932571168769,
    "leadId": "Tech Company",
    "workflowId": 1956187014160322560,
    "workflowName": "Recruitment Workflow for a Tech Company",
    "completedStageId": 1956187014630084608,
    "completedStageName": "Job Posting & Sourcing",
    "stageCategory": "Job Posting & Sourcing",
    "nextStageId": 1956187014919491584,
    "nextStageName": "Interviews & Assessments",
    "completionRate": 50.0,
    "isFinalStage": false,
    "responsibleTeam": "Default",
    "assigneeId": null,
    "assigneeName": "System",
    "businessContext": {
        "CompletionMethod": "CompleteCurrentStage",
        "AutoMoveToNext": true,
        "CompletionNotes": "Stage completed via CompleteCurrentStageAsync",
        "Components.ChecklistsCount": 1,
        "Components.QuestionnairesCount": 1,
        "Components.TaskCompletionsCount": 2,
        "Components.RequiredFieldsCount": 3
    },
    "routingTags": ["onboarding", "stage-completion", "customer-portal", "auto-progression"],
    "priority": "High",
    "source": "CustomerPortal",
    "relatedEntity": null,
    "description": "Stage 'Job Posting & Sourcing' completed for Onboarding 1956197932571168769 via CompleteCurrentStageAsync",
    "tags": ["onboarding", "stage-completion", "auto-progression"],
    "components": {
        "checklists": [{
            "checklistId": 1956187020887986176,
            "checklistName": "Job Posting & Sourcing Checklist",
            "description": "Essential tasks to complete during the Job Posting & Sourcing stage",
            "team": "Default Team",
            "type": "Template",
            "status": "Active",
            "isTemplate": true,
            "completionRate": 0,
            "totalTasks": 4,
            "completedTasks": 0,
            "isActive": true,
            "tasks": [{
                "id": 1956187021609406464,
                "checklistId": 1956187020887986176,
                "name": "Plan Tasks",
                "description": "Plan all tasks for Job Posting & Sourcing",
                "orderIndex": 1,
                "taskType": "",
                "isRequired": true,
                "estimatedHours": 0,
                "priority": "Medium",
                "isCompleted": false,
                "status": "Pending",
                "isActive": true
            }, {
                "id": 1956187022041419776,
                "checklistId": 1956187020887986176,
                "name": "Allocate Resources",
                "description": "Ensure necessary resources are allocated",
                "orderIndex": 2,
                "taskType": "",
                "isRequired": true,
                "estimatedHours": 0,
                "priority": "Medium",
                "isCompleted": false,
                "status": "Pending",
                "isActive": true
            }, {
                "id": 1956187022473433088,
                "checklistId": 1956187020887986176,
                "name": "Monitor Progress",
                "description": "Track and monitor stage progress",
                "orderIndex": 3,
                "taskType": "",
                "isRequired": true,
                "estimatedHours": 0,
                "priority": "Medium",
                "isCompleted": false,
                "status": "Pending",
                "isActive": true
            }, {
                "id": 1956187022905446400,
                "checklistId": 1956187020887986176,
                "name": "Complete Deliverables",
                "description": "Finish all stage deliverables",
                "orderIndex": 4,
                "taskType": "",
                "isRequired": true,
                "estimatedHours": 0,
                "priority": "Medium",
                "isCompleted": false,
                "status": "Pending",
                "isActive": true
            }]
        }],
        "taskCompletions": [{
            "checklistId": 1956187020887986176,
            "taskId": 1956187021609406464,
            "isCompleted": true,
            "completionNotes": "",
            "completedBy": "",
            "completedTime": "2025-08-15T14:16:47.796071+08:00"
        }, {
            "checklistId": 1956187020887986176,
            "taskId": 1956187022041419776,
            "isCompleted": true,
            "completionNotes": "",
            "completedBy": "",
            "completedTime": "2025-08-15T14:16:49.125102+08:00"
        }],
        "questionnaires": [{
            "questionnaireId": 1956187036725678080,
            "questionnaireName": "Job Posting & Sourcing Questionnaire",
            "description": "Key questions to gather information for the Job Posting & Sourcing stage",
            "status": "Draft",
            "version": 1,
            "category": "General",
            "totalQuestions": 4,
            "requiredQuestions": 2,
            "allowDraft": true,
            "allowMultipleSubmissions": false,
            "isActive": true,
            "structureJson": "<JSON string containing sections array with order, title, questions fields>"
        }],
        "questionnaireAnswers": [{
            "answerId": 1956202363115147264,
            "questionnaireId": 1956187036725678080,
            "questionId": 0,
            "questionText": "",
            "questionType": "",
            "isRequired": false,
            "answer": "<JSON string containing responses array with questionId, question, answer, type, responseText, changeHistory, lastModifiedBy, lastModifiedAt fields>",
            "answerTime": null,
            "status": "Draft"
        }],
        "requiredFields": [{
            "fieldName": "CONTACTEMAIL",
            "displayName": null,
            "fieldType": "email",
            "isRequired": false,
            "fieldValue": "deng.wang@item.com3",
            "validationStatus": "Pending",
            "validationErrors": []
        }, {
            "fieldName": "CUSTOMERNAME",
            "displayName": null,
            "fieldType": "",
            "isRequired": true,
            "fieldValue": null,
            "validationStatus": "Pending",
            "validationErrors": []
        }, {
            "fieldName": "CONTACTNAME",
            "displayName": null,
            "fieldType": "",
            "isRequired": true,
            "fieldValue": null,
            "validationStatus": "Pending",
            "validationErrors": []
        }]
    }
}`;
});

// Examples based on action type
const variableExamples = computed(() => {
	if (props.actionType === 'python') {
		return [
			{
				title: 'Access Basic Event Information',
				description: 'Get event details from stage completion data',
				code: `# Access basic event information
event_id = context.get('eventId', '')
onboarding_id = context.get('onboardingId', '')
workflow_name = context.get('workflowName', '')
tenant_id = context.get('tenantId', '')
priority = context.get('priority', '')
source = context.get('source', '')

print(f"Processing event {event_id} for onboarding {onboarding_id}")
print(f"Workflow: {workflow_name} (Tenant: {tenant_id})")
print(f"Priority: {priority}, Source: {source}")`,
			},
			{
				title: 'Stage Completion & Business Context',
				description: 'Access completed stage and business context information',
				code: `# Get stage information
completed_stage = context.get('completedStageName', '')
next_stage = context.get('nextStageName', '')
completion_rate = context.get('completionRate', 0)
is_final = context.get('isFinalStage', False)

# Access business context
business_context = context.get('businessContext', {})
completion_method = business_context.get('CompletionMethod', '')
auto_move = business_context.get('AutoMoveToNext', False)
completion_notes = business_context.get('CompletionNotes', '')

print(f"Completed: {completed_stage} -> Next: {next_stage}")
print(f"Progress: {completion_rate}% (Final: {is_final})")
print(f"Method: {completion_method}, Auto Move: {auto_move}")`,
			},
			{
				title: 'Process Questionnaire Responses',
				description: 'Parse and iterate through questionnaire answers',
				code: `import json

# Get questionnaire answers
components = context.get('components', {})
questionnaire_answers = components.get('questionnaireAnswers', [])

for answer_record in questionnaire_answers:
    # Parse the JSON answer string
    answer_json = answer_record.get('answer', '{}')
    parsed_data = json.loads(answer_json)
    
    # Access individual responses
    responses = parsed_data.get('responses', [])
    for response in responses:
        question = response.get('question', '')
        answer = response.get('answer', '')
        question_type = response.get('type', '')
        print(f"Q ({question_type}): {question}")
        print(f"A: {answer}")`,
			},
			{
				title: 'Process Checklists and Tasks',
				description: 'Access checklist and task completion data',
				code: `# Get checklist data
components = context.get('components', {})
checklists = components.get('checklists', [])
task_completions = components.get('taskCompletions', [])

for checklist in checklists:
    checklist_name = checklist.get('checklistName', '')
    completion_rate = checklist.get('completionRate', 0)
    total_tasks = checklist.get('totalTasks', 0)
    completed_tasks = checklist.get('completedTasks', 0)
    
    print(f"Checklist: {checklist_name}")
    print(f"Progress: {completed_tasks}/{total_tasks} ({completion_rate}%)")
    
    # Process individual tasks
    tasks = checklist.get('tasks', [])
    for task in tasks:
        task_name = task.get('name', '')
        is_completed = task.get('isCompleted', False)
        priority = task.get('priority', '')
        print(f"  - {task_name} ({priority}): {'✓' if is_completed else '○'}")`,
			},
			{
				title: 'Validate Required Fields',
				description: 'Check required field validation status',
				code: `# Get required fields
components = context.get('components', {})
required_fields = components.get('requiredFields', [])

for field in required_fields:
    field_name = field.get('fieldName', '')
    field_value = field.get('fieldValue', None)
    is_required = field.get('isRequired', False)
    validation_status = field.get('validationStatus', '')
    validation_errors = field.get('validationErrors', [])
    
    print(f"Field: {field_name}")
    print(f"Value: {field_value}")
    print(f"Required: {is_required}, Status: {validation_status}")
    
    if validation_errors:
        print(f"Errors: {validation_errors}")`,
			},
		];
	} else {
		return [
			{
				title: 'Dynamic URL with Event Variables',
				description: 'Use event variables in API endpoint URLs',
				code: `https://api.example.com/onboarding/{{context.onboardingId}}/stages/{{context.completedStageId}}?source={{context.source}}&priority={{context.priority}}`,
			},
			{
				title: 'Request Body with Complete Event Data',
				description: 'Include comprehensive event data in JSON payload',
				code: `{
  "event_id": "{{context.eventId}}",
  "timestamp": "{{context.timestamp}}",
  "version": "{{context.version}}",
  "tenant_id": "{{context.tenantId}}",
  "onboarding_id": "{{context.onboardingId}}",
  "lead_id": "{{context.leadId}}",
  "workflow": {
    "id": "{{context.workflowId}}",
    "name": "{{context.workflowName}}",
    "completed_stage": "{{context.completedStageName}}",
    "next_stage": "{{context.nextStageName}}",
    "completion_rate": {{context.completionRate}},
    "is_final_stage": {{context.isFinalStage}}
  },
  "assignment": {
    "responsible_team": "{{context.responsibleTeam}}",
    "assignee_id": "{{context.assigneeId}}",
    "assignee_name": "{{context.assigneeName}}"
  },
  "business_context": {
    "completion_method": "{{context.businessContext.CompletionMethod}}",
    "auto_move_to_next": {{context.businessContext.AutoMoveToNext}},
    "completion_notes": "{{context.businessContext.CompletionNotes}}",
    "components_count": {
      "checklists": {{context.businessContext["Components.ChecklistsCount"]}},
      "questionnaires": {{context.businessContext["Components.QuestionnairesCount"]}},
      "task_completions": {{context.businessContext["Components.TaskCompletionsCount"]}},
      "required_fields": {{context.businessContext["Components.RequiredFieldsCount"]}}
    }
  },
  "metadata": {
    "priority": "{{context.priority}}",
    "source": "{{context.source}}",
    "description": "{{context.description}}",
    "routing_tags": {{context.routingTags}},
    "tags": {{context.tags}}
  }
}`,
			},
			{
				title: 'Headers with Event Values',
				description: 'Use event variables in request headers',
				code: `{
  "Content-Type": "application/json",
  "X-Event-ID": "{{context.eventId}}",
  "X-Event-Version": "{{context.version}}",
  "X-Onboarding-ID": "{{context.onboardingId}}",
  "X-Workflow-ID": "{{context.workflowId}}",
  "X-Stage-ID": "{{context.completedStageId}}",
  "X-Tenant-ID": "{{context.tenantId}}",
  "X-Priority": "{{context.priority}}",
  "X-Source": "{{context.source}}",
  "Authorization": "Bearer YOUR_API_TOKEN"
}`,
			},
			{
				title: 'Webhook with Component Data',
				description: 'Send component completion data to external webhook',
				code: `{
  "webhook_type": "stage_completion",
  "event_id": "{{context.eventId}}",
  "onboarding_id": "{{context.onboardingId}}",
  "stage_name": "{{context.completedStageName}}",
  "completion_summary": {
    "checklists_completed": "{{context.businessContext[\\"Components.ChecklistsCount\\"]}}",
    "questionnaires_submitted": "{{context.businessContext[\\"Components.QuestionnairesCount\\"]}}",
    "tasks_completed": "{{context.businessContext[\\"Components.TaskCompletionsCount\\"]}}",
    "required_fields_validated": "{{context.businessContext[\\"Components.RequiredFieldsCount\\"]}}",
    "overall_completion_rate": {{context.completionRate}}
  },
  "next_stage": "{{context.nextStageName}}",
  "auto_progression": {{context.businessContext.AutoMoveToNext}},
  "timestamp": "{{context.timestamp}}"
}`,
			},
		];
	}
});

// Computed
const questionnaireStructurePreview = computed(() => {
	return JSON.stringify(
		{
			// Access questionnaire data from components
			questionnaires: 'event.components.questionnaires[i]',
			questionnaireAnswers: 'event.components.questionnaireAnswers[i]',

			// Questionnaire structure
			questionnaireInfo: {
				questionnaireId: '<string>',
				questionnaireName: '<string>',
				description: '<string>',
				status: '<string>',
				version: '<number>',
				category: '<string>',
				totalQuestions: '<number>',
				requiredQuestions: '<number>',
				allowDraft: '<boolean>',
				allowMultipleSubmissions: '<boolean>',
				isActive: '<boolean>',
				structureJson: '<JSON string with questions structure>',
			},

			// Answer structure (answer field is JSON string)
			answerStructure: {
				answerId: '<string>',
				questionnaireId: '<string>',
				questionId: '<number>',
				questionText: '<string>',
				questionType: '<string>',
				isRequired: '<boolean>',
				answer: '<JSON string containing responses>',
				answerTime: '<string|null>',
				status: '<string>',
			},

			// Parsed responses from answer JSON string
			parsedResponses: {
				responses: [
					{
						questionId: '<string>',
						question: '<string>',
						answer: '<string>',
						type: '<string>',
						responseText: '<string>',
						changeHistory: '<array>',
						lastModifiedBy: '<string>',
						lastModifiedAt: '<string>',
					},
				],
			},

			note: [
				'To access questionnaire responses in Python:',
				'1. Get questionnaire answers: answers = event["components"]["questionnaireAnswers"]',
				'2. Parse the answer JSON: import json; responses = json.loads(answer["answer"])',
				'3. Access individual responses: responses["responses"]',
				'',
				'Answer formats by question type:',
				'- text/paragraph: answer = "text content"',
				'- multiple_choice: answer = "selected_option"',
				'- checkboxes: answer = "option1,option2,option3"',
				'- rating/linear_scale: answer = "5"',
				'- file: answer = "file1.pdf,file2.doc"',
			],
		},
		null,
		2
	);
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
	@apply border border-gray-200 dark:border-gray-700 rounded-lg bg-gray-50 dark:bg-gray-800 overflow-hidden;
}

.variables-header {
	@apply p-4 bg-white dark:bg-gray-900;
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
	@apply rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900;
}

.variables-tabs-wrapper {
	@apply p-4;
}

.context-structure-section {
	@apply border-t border-gray-200 dark:border-gray-700;
}

.context-toggle-header {
	@apply border-b border-gray-200 dark:border-gray-700;

	&:hover {
		@apply bg-gray-50 dark:bg-gray-800;
	}
}

.context-content {
	@apply bg-gray-50 dark:bg-gray-800;
}

.tab-content-scrollbar {
	@apply w-full;

	:deep(.el-scrollbar__view) {
		@apply p-1;
	}
}

.variables-tabs {
	:deep(.el-tabs__header) {
		@apply mb-4;
	}

	:deep(.el-tabs__nav-wrap) {
		@apply bg-gray-50 dark:bg-gray-700 rounded-lg p-1;
	}

	:deep(.el-tabs__active-bar) {
		@apply bg-primary-500;
	}

	:deep(.el-tabs__item) {
		@apply rounded-md transition-all;

		&.is-active {
			@apply bg-white dark:bg-gray-600 text-primary-600 dark:text-primary-400 shadow-sm;
		}
	}
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
	@apply text-xs bg-gray-50 dark:bg-gray-900 p-4 rounded-lg overflow-auto border border-gray-200 dark:border-gray-600 font-mono text-gray-800 dark:text-gray-200 whitespace-pre-wrap;
	max-height: 400px;
}

// Dark mode
.dark {
	.variables-header {
		@apply bg-gray-800;
	}
}
</style>
