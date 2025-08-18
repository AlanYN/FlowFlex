<template>
	<div class="variables-panel">
		<div class="p-4">
			<div class="flex items-center justify-between mb-4">
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
			<p class="text-sm text-gray-600 dark:text-gray-400 mb-4">
				Click on any variable to copy it to clipboard, then paste into your code or
				configuration.
			</p>
		</div>

		<div v-if="showVariables" class="p-4 bg-white">
			<el-tabs v-model="activeTab" class="variables-tabs">
				<el-tab-pane label="Event Variables" name="context">
					<el-scrollbar max-height="400px" class="tab-content-scrollbar">
						<div class="grid grid-cols-1 lg:grid-cols-2 gap-6 pr-4">
							<!-- Event Variables -->
							<div class="variable-category">
								<div class="category-header">
									<el-icon class="text-blue-500" size="16">
										<User />
									</el-icon>
									<h5 class="font-medium text-gray-700 dark:text-gray-300">
										Event Data
									</h5>
								</div>
								<div class="variable-list">
									<div
										v-for="variable in onboardingVariables"
										:key="variable.name"
										class="variable-item group"
										@click="copyVariable(variable.name)"
									>
										<div class="variable-info">
											<code class="variable-name">{{ variable.name }}</code>
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

							<!-- Stage Variables -->
							<div class="variable-category">
								<div class="category-header">
									<el-icon class="text-green-500" size="16">
										<Flag />
									</el-icon>
									<h5 class="font-medium text-gray-700 dark:text-gray-300">
										Stage Completion Data
									</h5>
								</div>
								<div class="variable-list">
									<div
										v-for="variable in stageVariables"
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
								<pre class="json-preview">{{ questionnaireStructurePreview }}</pre>
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
	</div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { ElMessage } from 'element-plus';
import { Operation, User, Flag, Document, DocumentCopy } from '@element-plus/icons-vue';

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

// Variables
const onboardingVariables: Variable[] = [
	{ name: 'event.eventId', description: 'Unique event identifier' },
	{ name: 'event.timestamp', description: 'Event timestamp' },
	{ name: 'event.tenantId', description: 'Tenant identifier' },
	{ name: 'event.onboardingId', description: 'Onboarding identifier' },
	{ name: 'event.leadId', description: 'Lead identifier' },
	{ name: 'event.workflowId', description: 'Workflow identifier' },
	{ name: 'event.workflowName', description: 'Workflow display name' },
	{ name: 'event.completionRate', description: 'Overall completion percentage' },
	{ name: 'event.isFinalStage', description: 'Whether this is the final stage' },
	{ name: 'event.responsibleTeam', description: 'Team responsible for this stage' },
	{ name: 'event.assigneeId', description: 'Assigned user ID' },
	{ name: 'event.assigneeName', description: 'Assigned user name' },
];

const stageVariables: Variable[] = [
	{ name: 'event.completedStageId', description: 'Completed stage identifier' },
	{ name: 'event.completedStageName', description: 'Completed stage name' },
	{ name: 'event.stageCategory', description: 'Stage category' },
	{ name: 'event.nextStageId', description: 'Next stage identifier' },
	{ name: 'event.nextStageName', description: 'Next stage name' },
	{ name: 'event.businessContext.CompletionMethod', description: 'How the stage was completed' },
	{ name: 'event.businessContext.AutoMoveToNext', description: 'Auto progression flag' },
	{ name: 'event.businessContext.CompletionNotes', description: 'Stage completion notes' },
];

// Examples based on action type
const variableExamples = computed(() => {
	if (props.actionType === 'python') {
		return [
			{
				title: 'Access Event Information',
				description: 'Get event details from stage completion data',
				code: `# Get event information
              event_id = event.get('eventId', '')
              onboarding_id = event.get('onboardingId', '')
              workflow_name = event.get('workflowName', '')
              tenant_id = event.get('tenantId', '')

              print(f"Processing event {event_id} for onboarding {onboarding_id}")
              print(f"Workflow: {workflow_name} (Tenant: {tenant_id})")`,
			},
			{
				title: 'Stage Completion Details',
				description: 'Access completed and next stage information',
				code: `# Get stage information
              completed_stage = event.get('completedStageName', '')
              next_stage = event.get('nextStageName', '')
              completion_rate = event.get('completionRate', 0)
              is_final = event.get('isFinalStage', False)

              print(f"Completed: {completed_stage}")
              print(f"Next: {next_stage}")
              print(f"Progress: {completion_rate}%")
              print(f"Final stage: {is_final}")`,
			},
			{
				title: 'Process Form Responses',
				description: 'Iterate through questionnaire responses',
				code: `# Process questionnaire responses
              for response in questionnaire_responses:
                  question = response.get('question', '')
                  answer = response.get('answer', '')
                  print(f"Q: {question}")
                  print(f"A: {answer}")`,
			},
		];
	} else {
		return [
			{
				title: 'Dynamic URL with Event Variables',
				description: 'Use event variables in API endpoint URLs',
				code: `https://api.example.com/onboarding/{{event.onboardingId}}/stages/{{event.completedStageId}}`,
			},
			{
				title: 'Request Body with Event Data',
				description: 'Include event data in JSON payload',
				code: `{
          "event_id": "{{event.eventId}}",
          "onboarding_id": "{{event.onboardingId}}",
          "workflow_name": "{{event.workflowName}}",
          "completed_stage": "{{event.completedStageName}}",
          "next_stage": "{{event.nextStageName}}",
          "completion_rate": "{{event.completionRate}}",
          "is_final_stage": "{{event.isFinalStage}}",
          "responsible_team": "{{event.responsibleTeam}}",
          "assignee": "{{event.assigneeName}}"
        }`,
			},
			{
				title: 'Headers with Event Values',
				description: 'Use event variables in request headers',
				code: `{
                "Content-Type": "application/json",
                "X-Event-ID": "{{event.eventId}}",
                "X-Onboarding-ID": "{{event.onboardingId}}",
                "X-Workflow-ID": "{{event.workflowId}}",
                "X-Stage-ID": "{{event.completedStageId}}",
                "X-Tenant-ID": "{{event.tenantId}}",
                "Authorization": "Bearer YOUR_API_TOKEN"
              }`,
			},
		];
	}
});

// Computed
const questionnaireStructurePreview = computed(() => {
	return JSON.stringify(
		{
			questionnaireId: '<string>',
			stageId: '<string>',
			responses: [
				{
					questionId: '<string>',
					question: '<string>',
					answer: '<string>',
					type: '<string>',
					responseText: '<string>',
					lastModifiedAt: '<string>',
					lastModifiedBy: '<string>',
				},
				'// Different answer formats by type:',
				'// text types (short_answer, paragraph, dropdown, number, date, time): answer = "text"',
				'// multiple_choice: answer = "selected_option"',
				'// checkboxes: answer = "option1,option2,option3"',
				'// rating, linear_scale: answer = "5"',
				'// file: answer = "file1.pdf,file2.doc"',
				'// grid types: questionId = "questionId_rowId"',
				'// multiple_choice_grid: answer = "col1,col2"',
				'// checkbox_grid: answer = "selected_column"',
				'// display types (divider, description, page_break, video, image): no answer data',
				'',
				'// responseText contains:',
				'// - Text types: same as answer',
				'// - Types with "other" options: JSON string with other texts',
				'// - Grid types: JSON string with other column texts',
				'// - File uploads: comma-separated file names',
				'// - Display types: no responseText data',
			],
		},
		null,
		2
	);
});

// Methods
const copyVariable = (variableName: string) => {
	copyToClipboard(variableName);
	ElMessage.success(`Variable ${variableName} copied to clipboard`);
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
	@apply border pb-4 border-gray-200 dark:border-gray-700 rounded-lg bg-gray-50 dark:bg-gray-800;
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

// Dark mode
.dark {
	.variables-header {
		@apply bg-gray-800;
	}
}
</style>
