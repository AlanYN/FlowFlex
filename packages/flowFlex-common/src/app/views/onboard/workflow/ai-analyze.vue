<template>
	<div class="ai-analyze-page">
		<div class="header">
			<h1 class="title">AI Analyze Files</h1>
			<p class="subtitle">
				Upload files, analyze with AI, preview structured data, and import into Workflow
			</p>
		</div>

		<el-steps :active="step" align-center finish-status="success" class="mb-4">
			<el-step title="Upload" />
			<el-step title="Analyze" />
			<el-step title="Preview" />
			<el-step title="Import" />
		</el-steps>

		<div v-show="step === 0" class="card">
			<el-upload
				drag
				:auto-upload="false"
				:show-file-list="true"
				:on-change="onFileChange"
				accept=".txt,.md,.csv,.tsv,.xlsx,.xls"
				class="upload"
			>
				<el-icon class="el-icon--upload text-4xl"><Upload /></el-icon>
				<div>
					<text class="text-primary dark:text-white">Drop file here</text>
					<text>or</text>
					<em class="text-primary">click to select</em>
				</div>
				<div class="el-upload__tip text-xs">
					Supported: .txt, .md, .csv, .tsv, .xlsx, .xls
				</div>
			</el-upload>
			<div class="actions">
				<el-button type="primary" :disabled="!rawText" @click="next">Next</el-button>
			</div>
		</div>

		<div v-show="step === 1" class="card">
			<div class="flex items-center gap-2 mb-3">
				<!-- Model switching controls -->
				<el-select
					v-model="modelId"
					placeholder="Model from config"
					clearable
					style="width: 260px"
					@change="onModelIdChange"
				>
					<el-option
						v-for="m in modelOptions"
						:key="m.id"
						:label="mDisplay(m)"
						:value="String(m.id)"
					/>
				</el-select>
				<el-input
					v-model="modelProvider"
					placeholder="Provider (override)"
					style="width: 160px"
				/>
				<el-input v-model="modelName" placeholder="Model (override)" style="width: 200px" />
				<el-tooltip :content="defaultModelTip" placement="top">
					<el-tag type="info" effect="plain">
						Default: {{ aiDefaultProvider || '-' }}/{{ aiDefaultModel || '-' }}
					</el-tag>
				</el-tooltip>
				<el-button @click="showModelConfig = true" type="success" plain>
					AI Model Config
				</el-button>
			</div>
			<div class="flex items-center gap-2 mb-3">
				<el-input
					v-model="promptOverride"
					placeholder="Optional: Brief description to guide AI"
				/>
				<el-button type="primary" :loading="analyzing" @click="analyze">Analyze</el-button>
			</div>
			<el-input
				type="textarea"
				:rows="12"
				v-model="rawText"
				placeholder="Preview file content"
			/>
			<div class="actions">
				<el-button @click="prev">Back</el-button>
				<el-button type="primary" :disabled="!analysisReady" @click="next">Next</el-button>
			</div>
		</div>

		<div v-show="step === 2" class="card">
			<el-card shadow="hover" class="mb-3">
				<template #header>
					<div class="flex items-center justify-between">
						<span class="font-semibold">Workflow Summary</span>
					</div>
				</template>
				<div>
					<div class="mb-2">
						<strong>Name:</strong>
						{{ workflowPreview.name }}
					</div>
					<div class="mb-2">
						<strong>Description:</strong>
						{{ workflowPreview.description }}
					</div>
					<div class="mb-2">
						<strong>Stages:</strong>
						{{ stagePreview.length }}
					</div>
				</div>
			</el-card>

			<el-table :data="stagePreview" border height="420">
				<el-table-column type="index" width="60" label="#" />
				<el-table-column prop="name" label="Stage Name" min-width="200" />
				<el-table-column prop="assignedGroup" label="Team" width="160" />
				<el-table-column prop="estimatedDuration" label="Days" width="100" />
				<el-table-column label="Required Fields" min-width="260">
					<template #default="{ row }">
						<el-tag v-for="f in row.requiredFields || []" :key="f" class="mr-1 mb-1">
							{{ f }}
						</el-tag>
					</template>
				</el-table-column>
				<el-table-column label="Checklist" min-width="220">
					<template #default="{ row }">
						<template v-if="row.checklistText && row.checklistText.trim()">
							<el-tooltip placement="top" :content="row.checklistText">
								<span class="text-gray-700 truncate block max-w-[200px]">
									{{ row.checklistText }}
								</span>
							</el-tooltip>
						</template>
						<template v-else>
							<el-tag v-if="row.checklistCount > 0" type="success">
								Existing ({{ row.checklistCount }})
							</el-tag>
							<el-tag v-else type="info">Create</el-tag>
						</template>
					</template>
				</el-table-column>
				<el-table-column label="Questionnaire" min-width="240">
					<template #default="{ row }">
						<template v-if="row.questionnaireText && row.questionnaireText.trim()">
							<el-tooltip placement="top" :content="row.questionnaireText">
								<span class="text-gray-700 truncate block max-w-[220px]">
									{{ row.questionnaireText }}
								</span>
							</el-tooltip>
						</template>
						<template v-else>
							<el-tag v-if="row.questionnaireCount > 0" type="success">
								Existing ({{ row.questionnaireCount }})
							</el-tag>
							<el-tag v-else type="info">Create</el-tag>
						</template>
					</template>
				</el-table-column>
			</el-table>

			<div class="actions">
				<el-button @click="prev">Back</el-button>
				<el-button type="primary" :loading="importing" @click="importAll">Import</el-button>
			</div>
		</div>

		<div v-show="step === 3" class="card">
			<el-result
				icon="success"
				title="Import completed"
				sub-title="You can review the created workflow in Workflow page."
			/>
			<div class="actions">
				<el-button type="primary" @click="goWorkflow">Go to Workflow</el-button>
			</div>
		</div>
	</div>
	<!-- AI Model Config Dialog -->
	<el-dialog
		v-model="showModelConfig"
		title="AI Model Configuration"
		width="60%"
		:close-on-click-modal="false"
	>
		<AIModelConfig />
		<template #footer>
			<el-button @click="showModelConfig = false">Close</el-button>
		</template>
	</el-dialog>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import { Upload } from '@element-plus/icons-vue';
import {
	parseAIRequirements,
	generateAIWorkflow,
	getAIWorkflowStatus,
	getAIModels,
} from '@/apis/ai/workflow';
import { createWorkflow, getStagesByWorkflow, updateStage } from '@/apis/ow';
import { createChecklist } from '@/apis/ow/checklist';
import { createQuestionnaire } from '@/apis/ow/questionnaire';
import * as XLSX from 'xlsx-js-style';
import AIModelConfig from './ai-config.vue';

const router = useRouter();
const step = ref(0);
const rawText = ref('');
const promptOverride = ref('');
const analyzing = ref(false);
const importing = ref(false);

const workflowPreview = ref<{ name: string; description: string }>({ name: '', description: '' });
const stagePreview = ref<any[]>([]);
let createdWorkflowId: number | null = null;

// Model switching state
const modelProvider = ref<string>('');
const modelName = ref<string>('');
const modelId = ref<string>('');
const showModelConfig = ref(false);
const aiDefaultProvider = ref<string>('');
const aiDefaultModel = ref<string>('');
const defaultModelTip = ref<string>('Using tenant default model when Provider/Model are empty');
const modelOptions = ref<any[]>([]);

const excelRows = ref<any[][]>([]);
const stageExtraMap = ref<Record<string, { checklistText?: string; questionnaireText?: string }>>(
	{}
);

const onFileChange = async (file: any) => {
	if (!file?.raw) return;
	const name: string = (file.name || '').toLowerCase();
	try {
		if (name.endsWith('.xlsx') || name.endsWith('.xls')) {
			const buf = await file.raw.arrayBuffer();
			const wb = XLSX.read(buf, { type: 'array' });
			const sheet = wb.Sheets[wb.SheetNames[0]];
			const rows: any[][] = XLSX.utils.sheet_to_json(sheet, { header: 1, raw: true });
			excelRows.value = rows;
			// build extras map: Stage name in col 3, Checklist col 5, Questionnaire col 6
			const extras: Record<string, { checklistText?: string; questionnaireText?: string }> =
				{};
			rows.slice(1).forEach((r) => {
				const stageName = (r[3] || '').toString().trim();
				if (!stageName) return;
				const checklistCell = (r[5] || '').toString().trim();
				const questionnaireCell = (r[6] || '').toString().trim();
				if (checklistCell || questionnaireCell) {
					extras[stageName] = {
						checklistText:
							checklistCell && checklistCell !== '--' ? checklistCell : undefined,
						questionnaireText:
							questionnaireCell && questionnaireCell !== '--'
								? questionnaireCell
								: undefined,
					};
				}
			});
			stageExtraMap.value = extras;
			rawText.value = tableToTsv(rows);
		} else {
			// txt/md/csv/tsv 默认按文本读取
			rawText.value = await readFileAsText(file.raw);
		}
	} catch (e) {
		console.error('Parse file error:', e);
		rawText.value = await readFileAsText(file.raw);
	}
};

const readFileAsText = (raw: File) =>
	new Promise<string>((resolve, reject) => {
		const reader = new FileReader();
		reader.onload = () => resolve(String(reader.result || ''));
		reader.onerror = (err) => reject(err);
		reader.readAsText(raw);
	});

const tableToTsv = (rows: any[][], maxRows: number = 200): string => {
	if (!Array.isArray(rows) || rows.length === 0) return '';
	const slice = rows.slice(0, maxRows);
	const lines = slice.map((r) =>
		(Array.isArray(r) ? r : [r])
			.map((c) => (c == null ? '' : typeof c === 'object' ? JSON.stringify(c) : String(c)))
			.join('\t')
	);
	return lines.join('\n');
};

const next = () => {
	if (step.value === 0 && !rawText.value.trim()) {
		ElMessage.warning('Please upload a file');
		return;
	}
	if (step.value === 1 && !analysisReady.value) {
		ElMessage.warning('Please analyze first');
		return;
	}
	step.value++;
};
const prev = () => (step.value = Math.max(0, step.value - 1));

const analysisReady = ref(false);
const analyze = async () => {
	if (!rawText.value.trim()) return;
	analyzing.value = true;
	try {
		const base = rawText.value.slice(0, 5000);
		const parsed = await parseAIRequirements(base, {
			modelProvider: modelProvider.value?.trim() || undefined,
			modelName: modelName.value?.trim() || undefined,
			modelId: modelId.value?.trim() || undefined,
		});
		let description = base;
		if (parsed?.data?.success && parsed?.data?.structuredText) {
			description = parsed.data.structuredText;
		}
		const payload: any = {
			description: promptOverride.value?.trim()
				? `${promptOverride.value}\n\n${description}`
				: description,
		};
		if (modelProvider.value?.trim()) payload.modelProvider = modelProvider.value.trim();
		if (modelName.value?.trim()) payload.modelName = modelName.value.trim();
		if (modelId.value?.trim()) payload.modelId = modelId.value.trim();
		const gen = await generateAIWorkflow(payload);
		const data = gen?.data || {};
		if (data?.success === false || !Array.isArray(data?.stages) || data.stages.length === 0) {
			ElMessage.error(data?.message || 'AI service unavailable');
			return;
		}
		workflowPreview.value = data.generatedWorkflow || {
			name: 'AI Generated Workflow',
			description: 'Auto-created by AI',
		};
		stagePreview.value = (data.stages || []).map((s: any, idx: number) => ({
			name: s?.name || `Stage ${idx + 1}`,
			description: s?.description || '',
			order: Number.isFinite(Number(s?.order)) ? Math.trunc(Number(s?.order)) : idx + 1,
			assignedGroup: s?.assignedGroup || 'General',
			requiredFields: Array.isArray(s?.requiredFields) ? s.requiredFields : [],
			estimatedDuration: Number(s?.estimatedDuration) || 1,
			checklistCount: Array.isArray(s?.checklistIds) ? s.checklistIds.length : 0,
			questionnaireCount: Array.isArray(s?.questionnaireIds) ? s.questionnaireIds.length : 0,
			checklistText:
				stageExtraMap.value[(s?.name || '').toString().trim()]?.checklistText || '',
			questionnaireText:
				stageExtraMap.value[(s?.name || '').toString().trim()]?.questionnaireText || '',
		}));
		analysisReady.value = true;
	} catch (e) {
		console.error(e);
		ElMessage.error('Analyze failed');
	} finally {
		analyzing.value = false;
	}
};

const importAll = async () => {
	if (!analysisReady.value) return;
	importing.value = true;
	try {
		const payload = {
			name: workflowPreview.value.name,
			description: workflowPreview.value.description,
			isDefault: false,
			status: 'active',
			startDate: new Date().toISOString(),
			isActive: true,
			version: 1,
			stages: stagePreview.value.map((s, idx) => ({
				name: s.name,
				description: s.description,
				defaultAssignedGroup: s.assignedGroup,
				estimatedDuration: s.estimatedDuration,
				order: Number.isFinite(Number(s.order)) ? Math.trunc(Number(s.order)) : idx + 1,
				visibleInPortal: true,
				attachmentManagementNeeded: false,
			})),
		} as any;

		const wfRes = await createWorkflow(payload);
		if (!(wfRes && (wfRes.success || wfRes.code === '200'))) {
			ElMessage.error('Create workflow failed');
			return;
		}
		createdWorkflowId = wfRes.data || wfRes?.id || null;
		if (!createdWorkflowId) {
			ElMessage.error('Workflow id not returned');
			return;
		}

		const stagesRes = await getStagesByWorkflow(createdWorkflowId);
		const createdStages: any[] = stagesRes?.data || [];
		const stageMap = new Map<string, any>();
		createdStages.forEach((s) => stageMap.set((s.name || '').toString().trim(), s));

		// create checklist & questionnaire + update fields component
		for (const s of stagePreview.value) {
			const target = stageMap.get(s.name.trim());
			if (!target?.id) continue;
			try {
				await createChecklist({
					name: `${s.name} Checklist`,
					description:
						(s as any).checklistText || s.description || 'Auto-generated by AI',
					team: s.assignedGroup || 'General',
					type: 'Instance',
					status: 'Active',
					isTemplate: false,
					estimatedHours: 0,
					isActive: true,
					assignments: [{ workflowId: createdWorkflowId, stageId: Number(target.id) }],
				});
			} catch (e) {
				console.error(e);
			}
			try {
				const qn = `${s.name} Questionnaire`;
				await createQuestionnaire({
					name: qn,
					description:
						(s as any).questionnaireText || s.description || 'Auto-generated by AI',
					status: 'Draft',
					structureJson: JSON.stringify({ title: qn, sections: [] }),
					version: 1,
					category: 'Onboarding',
					tagsJson: '[]',
					estimatedMinutes: 0,
					allowDraft: true,
					allowMultipleSubmissions: false,
					isActive: true,
					assignments: [{ workflowId: createdWorkflowId, stageId: Number(target.id) }],
					sections: [],
				});
			} catch (e) {
				console.error(e);
			}

			const staticFields: string[] = Array.isArray(s.requiredFields)
				? s.requiredFields
						.map((x: string) => (x || '').toUpperCase().replace(/\s+/g, ''))
						.filter((x: string) => !!x)
				: [];
			if (staticFields.length > 0) {
				try {
					await updateStage(target.id, {
						workflowId: createdWorkflowId,
						name: s.name,
						description: s.description || '',
						order: target.sortOrder || target.order || 1,
						defaultAssignedGroup: s.assignedGroup || 'General',
						estimatedDuration:
							target.estimatedDays ||
							target.estimatedDuration ||
							s.estimatedDuration ||
							1,
						visibleInPortal: true,
						attachmentManagementNeeded: false,
						components: [
							{
								key: 'fields',
								order: 1,
								isEnabled: true,
								configuration: '',
								staticFields,
								checklistIds: [],
								questionnaireIds: [],
								checklistNames: [],
								questionnaireNames: [],
							},
						],
					});
				} catch (e) {
					console.error(e);
				}
			}
		}
		step.value = 3;
		ElMessage.success('Imported successfully');
	} catch (e) {
		console.error(e);
		ElMessage.error('Import failed');
	} finally {
		importing.value = false;
	}
};

const goWorkflow = () => router.push('/onboard/onboardWorkflow');

// Load AI default status for display
(async () => {
	try {
		const status = await getAIWorkflowStatus();
		const data = status?.data || {};
		aiDefaultProvider.value = data?.provider || '';
		aiDefaultModel.value = data?.model || '';
	} catch (e) {
		console.error(e);
	}
	try {
		const res = await getAIModels();
		modelOptions.value = Array.isArray(res?.data) ? res.data : [];
	} catch (e) {
		console.error(e);
	}
})();

const mDisplay = (m: any) => `${m.provider || ''}/${m.modelName || ''} (ID:${m.id})`;
const onModelIdChange = (val: string) => {
	const matched = modelOptions.value.find((x: any) => String(x.id) === String(val));
	if (matched) {
		modelProvider.value = matched.provider || '';
		modelName.value = matched.modelName || '';
	}
};
</script>

<style scoped lang="scss">
.ai-analyze-page {
	padding: 24px;
	max-width: 1200px;
	margin: 0 auto;
}
.header {
	margin-bottom: 16px;
}
.title {
	font-size: 22px;
	font-weight: 700;
}
.subtitle {
	color: #64748b;
}
.card {
	background: #fff;
	border: 1px solid #e5e7eb;
	padding: 16px;
	@apply rounded-xl;
}
.upload {
	width: 100%;
}
.actions {
	display: flex;
	justify-content: flex-end;
	gap: 8px;
	margin-top: 12px;
}
</style>
