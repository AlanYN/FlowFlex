<template>
	<el-drawer
		:model-value="!!connection"
		direction="rtl"
		size="480px"
		:close-on-click-modal="true"
		:close-on-press-escape="true"
		class="conn-panel-drawer"
		append-to-body
		@close="emit('close')"
	>
		<!-- Header -->
		<template #header>
			<div class="flex flex-col gap-1 flex-1 min-w-0">
				<span
					class="text-[10px] font-bold tracking-widest text-[var(--el-text-color-placeholder)] uppercase"
				>
					TRIGGER CONNECTION
				</span>
				<div
					class="flex items-center gap-1.5 text-[15px] font-bold text-[var(--el-text-color-primary)] flex-wrap"
				>
					<span>{{ sourceWorkflow?.name ?? '—' }}</span>
					<el-icon class="text-[13px] text-[var(--el-text-color-secondary)] shrink-0">
						<ArrowRight />
					</el-icon>
					<span>{{ targetWorkflow?.name ?? '—' }}</span>
				</div>
			</div>
			<div class="flex items-start gap-1 shrink-0 pt-0.5">
				<el-tooltip content="Delete connection" placement="top">
					<el-button type="danger" link :icon="Delete" @click="handleDelete" />
				</el-tooltip>
			</div>
		</template>

		<!-- Body -->
		<div class="flex flex-col">
			<!-- Skeleton -->
			<template v-if="nodeInfoLoading">
				<div class="p-5">
					<el-skeleton :rows="2" animated />
					<el-skeleton :rows="3" animated style="margin-top: 20px" />
					<el-skeleton :rows="4" animated style="margin-top: 20px" />
				</div>
			</template>

			<template v-else>
				<!-- Rule Name -->
				<div class="px-5 py-[18px] border-b border-[var(--el-border-color-lighter)]">
					<div
						class="flex items-center gap-1 text-sm font-semibold text-[var(--el-text-color-primary)] mb-2"
					>
						<span class="text-[var(--el-color-danger)] font-bold">*</span>
						Rule Name
						<span
							class="ml-auto text-xs font-normal text-[var(--el-text-color-placeholder)]"
						>
							{{ localRuleName.length }}/100
						</span>
					</div>
					<el-input
						v-model="localRuleName"
						placeholder="Enter rule name"
						:maxlength="100"
						@input="markDirty"
					/>
				</div>

				<!-- Trigger Condition -->
				<ConditionSection
					:conditions="localConditions"
					:stage-options="stageOptions"
					:loading="nodeInfoLoading"
					@add-condition="addCondition"
					@remove-condition="removeCondition"
					@dirty="markDirty"
				/>

				<!-- Data Mapping -->
				<DataMappingSection
					:case-info-mappings="caseInfoMappings"
					:auto-mapped-fields="autoMappedFields"
					:local-mappings="localMappings"
					:auto-map="autoMap"
					:source-option-groups="sourceOptionGroups"
					:dynamic-field-options="dynamicFieldOptions"
					:questionnaire-options="questionnaireOptions"
					:target-field-options="targetFieldOptions"
					:source-workflow-name="sourceWorkflow?.name"
					:target-workflow-name="targetWorkflow?.name"
					:loading="nodeInfoLoading"
					@update:auto-map="
						(v) => {
							autoMap = v;
							markDirty();
						}
					"
					@add-mapping="addMapping"
					@remove-mapping="removeMapping"
					@dirty="markDirty"
				/>
			</template>
		</div>

		<!-- Footer -->
		<template #footer>
			<div class="flex justify-end gap-3">
				<el-button @click="emit('close')">Cancel</el-button>
				<el-button
					type="primary"
					:loading="saving"
					:disabled="nodeInfoLoading"
					@click="handleSave"
				>
					{{ saving ? 'Saving...' : 'Apply' }}
				</el-button>
			</div>
		</template>
	</el-drawer>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { ArrowRight, Delete } from '@element-plus/icons-vue';
import { getWorkflowNodeInfo } from '@/apis/ow/triggers';
import type {
	TriggerConnection,
	TriggerCondition,
	DataMapping,
	WorkflowItem,
} from '@/hooks/useTriggerEditor';
import ConditionSection from './ConditionSection.vue';
import DataMappingSection from './DataMappingSection.vue';

// ========================= Props / Emits =========================

interface Props {
	connection: TriggerConnection | null;
	allWorkflows: readonly WorkflowItem[];
	saving?: boolean;
}

const props = withDefaults(defineProps<Props>(), { saving: false });

const emit = defineEmits<{
	close: [];
	save: [patch: { conditionSummary: string; configJson: string; ruleName: string }];
	delete: [connectionId: string];
}>();

// ========================= Computed ==============================

const sourceWorkflow = computed(() =>
	props.allWorkflows.find((w) => w.id === props.connection?.sourceWorkflowId)
);
const targetWorkflow = computed(() =>
	props.allWorkflows.find((w) => w.id === props.connection?.targetWorkflowId)
);

// ========================= Local State ===========================

const localRuleName = ref('');
const localMappings = ref<MappingRow[]>([]);
const autoMap = ref(true);
const isDirty = ref(false);
const markDirty = () => {
	isDirty.value = true;
};

// ── Types ─────────────────────────────────────────────────
interface ConditionRow extends TriggerCondition {
	stageId?: string;
	stageName?: string;
	componentKey?: string;
	componentType?: string;
	componentId?: string;
	componentName?: string;
}

interface MappingRow extends DataMapping {
	enabled: boolean;
	staticValue?: string;
}

interface AutoMappedField {
	id: string;
	sourceName: string;
	targetName: string;
	sourceId: string;
	targetId: string;
	type: string;
	enabled: boolean;
}

interface StageFieldOption {
	id: string;
	name: string;
	fieldType: string;
}
interface StageTaskOption {
	id: string;
	name: string;
	taskType?: string;
}
interface StageQuestionOption {
	id: string;
	name: string;
}
interface StageQuestionnaire {
	id: string;
	name: string;
	questions: StageQuestionOption[];
}
interface StageChecklist {
	id: string;
	name: string;
	tasks: StageTaskOption[];
}
interface StageOption {
	id: string;
	name: string;
	fields: StageFieldOption[];
	questionnaires: StageQuestionnaire[];
	checklists: StageChecklist[];
}
interface SourceOptionGroup {
	label: string;
	options: { id: string; name: string }[];
}

// ── Refs ──────────────────────────────────────────────────
const localConditions = ref<ConditionRow[]>([]);
const autoMappedFields = ref<AutoMappedField[]>([]);
const caseInfoMappings = ref<AutoMappedField[]>([]);
const nodeInfoLoading = ref(false);
const stageOptions = ref<StageOption[]>([]);
const sourceOptionGroups = ref<SourceOptionGroup[]>([]);
const dynamicFieldOptions = ref<{ id: string; name: string }[]>([]);
const questionnaireOptions = ref<{ id: string; name: string }[]>([]);
const targetFieldOptions = ref<{ id: string; name: string }[]>([]);

// ========================= Watch =================================

watch(
	() => props.connection,
	(conn) => {
		if (!conn) return;
		isDirty.value = false;
		localRuleName.value = conn.ruleName ?? '';
		if (conn.configJson) {
			try {
				const cfg = JSON.parse(conn.configJson);
				localConditions.value = cfg.conditions ?? [];
				localMappings.value = (cfg.mappings ?? []).map((m: any) => ({
					...m,
					enabled: m.enabled !== false,
					// sourceQuestionType may be absent in legacy configs — keep as-is,
					// the backend auto-detects short_answer_grid from rawValue shape
				}));
				autoMap.value = cfg.autoMap !== false;
			} catch {
				localConditions.value = (conn.conditions ?? []) as ConditionRow[];
				localMappings.value = (conn.mappings ?? []).map((m) => ({ ...m, enabled: true }));
			}
		} else {
			localConditions.value = (conn.conditions ?? []) as ConditionRow[];
			localMappings.value = (conn.mappings ?? []).map((m) => ({ ...m, enabled: true }));
		}
		if (conn.sourceWorkflowId) loadNodeInfo(conn);
	},
	{ immediate: true }
);

// ========================= Node-info =============================

const applyNodeInfo = (nodeInfo: any, side: 'source' | 'target') => {
	const stages: StageOption[] = (nodeInfo?.stages ?? []).map((s: any) => ({
		id: String(s.id),
		name: s.name ?? '',
		fields: (s.fields ?? []).map((f: any) => ({
			id: String(f.id),
			name: f.name ?? '',
			fieldType: f.fieldType ?? '',
		})),
		questionnaires: (s.questionnaires ?? []).map((q: any) => ({
			id: String(q.id),
			name: q.name ?? '',
			questions: (q.questions ?? []).map((qq: any) => ({
				id: String(qq.id),
				name: qq.title ?? qq.name ?? '',
				type: qq.type ?? '',
				options: Array.isArray(qq.options)
					? qq.options.map((opt: any) =>
							typeof opt === 'string'
								? { label: opt, value: opt }
								: {
										label: opt.label ?? opt.value ?? opt,
										value: opt.value ?? opt.label ?? opt,
								  }
					  )
					: [],
			})),
		})),
		checklists: (s.checklists ?? []).map((c: any) => ({
			id: String(c.id),
			name: c.name ?? '',
			tasks: (c.tasks ?? []).map((t: any) => ({
				id: String(t.id),
				name: t.name ?? '',
				taskType: t.taskType ?? '',
			})),
		})),
	}));

	if (side === 'source') {
		stageOptions.value = stages;
		const dynamicOpts = stages.flatMap((s) =>
			s.fields.map((f: any) => ({
				id: `input.fields.${f.id}`,
				name: `${s.name} · ${f.name}`,
				fieldKind: 'static_field' as const,
				fieldType: f.fieldType ?? '',
			}))
		);
		dynamicFieldOptions.value = dynamicOpts;

		const questionnaireOpts = stages.flatMap((s) =>
			s.questionnaires.flatMap((q: any) =>
				q.questions.map((qq: any) => ({
					id: `input.questionnaire.answers["${q.id}"]["${qq.id}"]`,
					name: `${q.name}: ${qq.name}`,
					fieldKind: 'questionnaire' as const,
					fieldType: qq.type ?? 'short_answer',
				}))
			)
		);
		questionnaireOptions.value = questionnaireOpts;
		sourceOptionGroups.value = [
			{
				label: 'Case Info',
				options: [
					{ id: 'case.caseName', name: 'Case Name' },
					{ id: 'case.contactPerson', name: 'Contact Name' },
					{ id: 'case.contactEmail', name: 'Contact Email' },
					{ id: 'case.priority', name: 'Priority' },
				],
			},
			...(dynamicOpts.length ? [{ label: 'Dynamic Fields', options: dynamicOpts }] : []),
			...(questionnaireOpts.length
				? [{ label: 'Questionnaire Answers', options: questionnaireOpts }]
				: []),
		];
	} else {
		// Target: static fields + questionnaire questions — each carries type info for compatibility filtering
		const targetFieldOpts = stages.flatMap((s) =>
			s.fields.map((f: any) => ({
				id: `input.fields.${f.id}`,
				name: `${s.name} · ${f.name}`,
				group: `${s.name} — Static Fields`,
				fieldKind: 'static_field' as const,
				fieldType: f.fieldType ?? '',
			}))
		);
		const targetQnOpts = stages.flatMap((s) =>
			s.questionnaires.flatMap((q: any) =>
				q.questions.map((qq: any) => ({
					id: `input.questionnaire.answers["${q.id}"]["${qq.id}"]`,
					name: `${q.name}: ${qq.name}`,
					group: `${s.name} — Questionnaire`,
					fieldKind: 'questionnaire' as const,
					fieldType: qq.type ?? 'short_answer',
				}))
			)
		);
		targetFieldOptions.value = [...targetFieldOpts, ...targetQnOpts];
	}
};

const loadNodeInfo = async (conn: TriggerConnection) => {
	nodeInfoLoading.value = true;
	stageOptions.value = [];
	dynamicFieldOptions.value = [];
	questionnaireOptions.value = [];
	targetFieldOptions.value = [];
	autoMappedFields.value = [];
	caseInfoMappings.value = [];
	try {
		const needTarget = conn.targetWorkflowId && conn.targetWorkflowId !== conn.sourceWorkflowId;
		const [sourceRes, targetRes] = await Promise.all([
			getWorkflowNodeInfo(conn.sourceWorkflowId),
			...(needTarget ? [getWorkflowNodeInfo(conn.targetWorkflowId)] : []),
		]);
		const sourceData = sourceRes?.data;
		const targetData = needTarget ? targetRes?.data : sourceData;
		applyNodeInfo(sourceData, 'source');
		applyNodeInfo(targetData, 'target');

		let savedStates: {
			id: string;
			enabled: boolean;
			sourceId?: string;
			sourceName?: string;
		}[] = [];
		let savedCaseInfoStates: {
			id: string;
			enabled: boolean;
			sourceId?: string;
			sourceName?: string;
		}[] = [];
		try {
			if (conn.configJson) {
				const cfg = JSON.parse(conn.configJson);
				savedStates = cfg.autoMappedStates ?? [];
				savedCaseInfoStates = cfg.caseInfoStates ?? [];
			}
		} catch {
			/* ignore */
		}

		autoMappedFields.value = buildAutoMappedFields(sourceData, targetData, savedStates);
		caseInfoMappings.value = buildCaseInfoMappings(savedCaseInfoStates);
	} catch {
		/* keep empty */
	} finally {
		nodeInfoLoading.value = false;
	}
};

// ========================= Build helpers =========================

const normalizeFieldName = (name: string) =>
	name
		.toLowerCase()
		.trim()
		.replace(/[^a-z0-9]+/g, '_')
		.replace(/^_|_$/g, '');

const CASE_INFO_FIELDS = [
	{ id: 'case_info_caseName', name: 'Case Name', fieldType: 'SingleLineText', required: true },
	{
		id: 'case_info_contactPerson',
		name: 'Contact Name',
		fieldType: 'SingleLineText',
		required: false,
	},
	{ id: 'case_info_contactEmail', name: 'Contact Email', fieldType: 'Email', required: true },
	{ id: 'case_info_priority', name: 'Priority', fieldType: 'Select', required: true },
];

const buildAutoMappedFields = (
	sourceData: any,
	targetData: any,
	savedStates: { id: string; enabled: boolean; sourceId?: string; sourceName?: string }[] = []
): AutoMappedField[] => {
	const sourceFields = extractAllFields(sourceData);
	const targetFields = extractAllFields(targetData);
	const stateMap = new Map(savedStates.map((s) => [s.id, s]));
	const sourceByName = new Map<string, (typeof sourceFields)[0]>();
	for (const f of sourceFields) {
		const key = normalizeFieldName(f.name);
		if (!sourceByName.has(key)) sourceByName.set(key, f);
	}
	return targetFields.map((tf) => {
		const sf = sourceByName.get(normalizeFieldName(tf.name));
		const id = sf ? `auto_${sf.id}_${tf.id}` : `auto_none_${tf.id}`;
		const saved = stateMap.get(id);
		return {
			id,
			sourceName: saved?.sourceName ?? sf?.name ?? '',
			targetName: tf.name,
			sourceId: saved?.sourceId ?? sf?.id ?? '',
			targetId: tf.id,
			type: tf.fieldType || 'Text',
			enabled: saved ? saved.enabled : !!sf,
		};
	});
};

const buildCaseInfoMappings = (
	savedStates: { id: string; enabled: boolean; sourceId?: string; sourceName?: string }[] = []
): AutoMappedField[] => {
	const stateMap = new Map(savedStates.map((s) => [s.id, s]));
	return CASE_INFO_FIELDS.map((cf) => {
		const id = `case_info_${cf.id}`;
		const saved = stateMap.get(id);
		return {
			id,
			sourceName: saved?.sourceName ?? cf.name,
			targetName: cf.name,
			sourceId: saved?.sourceId ?? `case.${cf.id.replace('case_info_', '')}`,
			targetId: cf.id,
			type: cf.fieldType,
			enabled: saved ? saved.enabled : cf.required,
		};
	});
};

const extractAllFields = (nodeInfo: any): { id: string; name: string; fieldType: string }[] =>
	(nodeInfo?.stages ?? []).flatMap((s: any) =>
		(s.fields ?? []).map((f: any) => ({
			id: `input.fields.${f.id}`,
			name: f.name ?? '',
			fieldType: f.fieldType ?? 'Text',
		}))
	);

// ========================= Condition ops =========================

const addCondition = () => {
	localConditions.value.push({
		id: `cond_${Date.now()}`,
		logic: 'AND',
		operator: '',
	} as ConditionRow);
	markDirty();
};
const removeCondition = (i: number) => {
	localConditions.value.splice(i, 1);
	markDirty();
};

// ========================= Mapping ops ===========================

const addMapping = () => {
	localMappings.value.push({
		id: `map_${Date.now()}`,
		sourceType: 'dynamic_field',
		enabled: true,
	} as MappingRow);
	markDirty();
};
const removeMapping = (i: number) => {
	localMappings.value.splice(i, 1);
	markDirty();
};

// ========================= Save / Delete =========================

const buildConditionSummary = (): string => {
	if (localConditions.value.length === 0) return 'Completed';
	return (localConditions.value as ConditionRow[])
		.map(
			(c, i) =>
				`${i > 0 ? ` ${c.logic} ` : ''}${c.resourceName ?? c.resourceId ?? 'field'} ${
					c.operator ?? '>'
				} ${c.value ?? ''}`
		)
		.join('');
};

const handleSave = () => {
	emit('save', {
		conditionSummary: buildConditionSummary(),
		configJson: JSON.stringify({
			conditions: localConditions.value,
			mappings: localMappings.value,
			autoMap: autoMap.value,
			caseInfoStates: caseInfoMappings.value.map((f) => ({
				id: f.id,
				enabled: f.enabled,
				sourceId: f.sourceId,
				sourceName: f.sourceName,
			})),
			autoMappedStates: autoMappedFields.value.map((f) => ({
				id: f.id,
				enabled: f.enabled,
				sourceId: f.sourceId,
				sourceName: f.sourceName,
			})),
		}),
		ruleName: localRuleName.value,
	});
	isDirty.value = false;
	ElMessage.success('Trigger connection saved. Click Save to persist changes.');
};

const handleDelete = async () => {
	if (!props.connection) return;
	try {
		await ElMessageBox.confirm(
			'Delete this connection? This action cannot be undone.',
			'Delete Connection',
			{ confirmButtonText: 'Delete', cancelButtonText: 'Cancel', type: 'warning' }
		);
		emit('delete', props.connection.id);
	} catch {
		/* cancel */
	}
};
</script>

<style lang="scss">
/* ── 全局覆盖 Drawer 样式（必须非 scoped，穿透 el-drawer 内部结构） ── */
.conn-panel-drawer {
	.el-drawer__header {
		position: relative;
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		padding: 16px 20px 12px;
		margin-bottom: 0 !important;
		border-bottom: 1px solid var(--el-border-color-lighter);
	}
	.el-drawer__close-btn {
		display: none;
	}
	.el-drawer__body {
		padding: 0;
		overflow-y: auto;
	}
	.el-drawer__footer {
		padding: 12px 20px;
		border-top: 1px solid var(--el-border-color-lighter);
	}
}
</style>

<style lang="scss" scoped>
/* Add condition / field mapping 按钮全宽样式 */
.conn-add-btn {
	width: 100%;
	border: 1px dashed var(--el-border-color);
	border-radius: 6px;
	color: var(--el-text-color-secondary);
	background: transparent;
	&:hover {
		border-color: var(--el-color-primary);
		color: var(--el-color-primary);
	}
}
</style>
