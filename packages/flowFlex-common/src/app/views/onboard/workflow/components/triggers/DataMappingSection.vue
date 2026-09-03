<template>
	<div class="px-5 py-[18px] border-b border-[var(--el-border-color-lighter)]">
		<!-- Section 标题 -->
		<div
			class="flex items-center gap-1.5 text-sm font-semibold text-[var(--el-text-color-primary)] mb-2"
		>
			<el-icon class="text-[15px] text-[var(--el-color-primary)]"><Switch /></el-icon>
			Data Mapping
			<el-tag class="ml-1.5 text-xs" round>{{ fieldMappingCount }} fields</el-tag>
		</div>
		<p class="text-xs text-[var(--el-text-color-secondary)] mb-2.5">
			Sync values from {{ sourceWorkflowName ?? '—' }} → {{ targetWorkflowName ?? '—' }}
		</p>

		<!-- ── Case Info（始终显示） ── -->
		<div
			class="mb-4 p-3 rounded-lg bg-[var(--el-color-primary-light-9)] border border-[var(--el-color-primary-light-7)]"
		>
			<div
				class="flex items-center gap-1.5 text-sm font-semibold text-[var(--el-color-primary)] mb-1"
			>
				<Icon icon="ph:identification-card" class="w-3.5 h-3.5" />
				Case Info
				<span
					class="text-[10px] font-medium px-1.5 py-px rounded bg-[var(--el-color-primary)] text-[var(--el-color-white)] ml-0.5"
				>
					Required
				</span>
			</div>
			<p class="text-xs text-[var(--el-text-color-secondary)] m-0 mb-2.5">
				These fields are always mapped to create the new case.
			</p>
			<div class="flex flex-col gap-1.5 mb-3.5">
				<div
					v-for="item in caseInfoMappings"
					:key="item.id"
					class="flex items-center gap-1.5 text-xs text-[var(--el-text-color-regular)]"
				>
					<el-checkbox
						:model-value="item.enabled"
						@change="
							(v: boolean) => {
								item.enabled = v;
								emit('dirty');
							}
						"
					/>
					<el-select
						:model-value="item.sourceId"
						placeholder="Select source"
						class="w-[180px] shrink-0"
						filterable
						:disabled="!item.enabled"
						@change="(v: string) => handleSourceChange(item, v)"
					>
						<el-option-group
							v-for="group in sourceOptionGroups"
							:key="group.label"
							:label="group.label"
						>
							<el-option
								v-for="o in group.options"
								:key="o.id"
								:label="o.name"
								:value="o.id"
							/>
						</el-option-group>
					</el-select>
					<el-icon class="text-xs text-[var(--el-text-color-secondary)] shrink-0">
						<ArrowRight />
					</el-icon>
					<span class="font-medium">{{ item.targetName }}</span>
					<span class="ml-auto text-[var(--el-text-color-placeholder)] text-xs">
						{{ item.type }}
					</span>
				</div>
			</div>
		</div>

		<!-- ── Auto-map ── -->
		<div class="mb-2.5">
			<el-checkbox :model-value="autoMap" @change="(v: boolean) => emit('update:autoMap', v)">
				Auto-map matching fields
			</el-checkbox>
		</div>

		<!-- Auto-map 字段列表 -->
		<div v-if="autoMap && autoMappedFields.length > 0" class="flex flex-col gap-1.5 mb-3.5">
			<div
				v-for="item in autoMappedFields"
				:key="item.id"
				class="flex items-center gap-1.5 text-xs text-[var(--el-text-color-regular)]"
			>
				<el-checkbox
					:model-value="item.enabled"
					@change="
						(v: boolean) => {
							item.enabled = v;
							emit('dirty');
						}
					"
				/>
				<el-select
					:model-value="item.sourceId"
					placeholder="Select source"
					class="w-[180px] shrink-0"
					filterable
					:disabled="loading || !item.enabled"
					@change="(v: string) => handleSourceChange(item, v)"
				>
					<el-option-group
						v-for="group in getCompatibleSourceGroups(item.targetId)"
						:key="group.label"
						:label="group.label"
					>
						<el-option
							v-for="o in group.options"
							:key="o.id"
							:label="o.name"
							:value="o.id"
						/>
					</el-option-group>
				</el-select>
				<el-icon class="text-xs text-[var(--el-text-color-secondary)] shrink-0">
					<ArrowRight />
				</el-icon>
				<span class="font-medium">{{ item.targetName }}</span>
				<span class="ml-auto text-[var(--el-text-color-placeholder)] text-xs">
					{{ item.type }}
				</span>
			</div>
		</div>

		<!-- ── Field Mappings ── -->
		<div
			class="text-xs font-bold tracking-widest text-[var(--el-text-color-placeholder)] uppercase mb-2.5"
		>
			FIELD MAPPINGS
		</div>

		<div class="flex flex-col gap-2.5 mb-2.5">
			<div
				v-for="(mapping, index) in localMappings"
				:key="mapping.id"
				class="p-3 rounded-lg border border-[var(--el-border-color)] bg-[var(--el-fill-color-lighter)] flex flex-col gap-2 transition-colors hover:border-[var(--el-color-primary-light-3)]"
			>
				<!-- 卡片头部 -->
				<div class="flex items-center justify-between">
					<div class="flex items-center gap-2">
						<el-checkbox v-model="mapping.enabled" @change="emit('dirty')" />
						<span
							class="text-xs font-bold tracking-widest text-[var(--el-text-color-placeholder)] uppercase"
						>
							SOURCE
						</span>
					</div>
					<el-button
						type="danger"
						link
						:icon="Delete"
						@click="emit('remove-mapping', index)"
					/>
				</div>

				<!-- 来源类型 -->
				<el-select
					v-model="mapping.sourceType"
					class="w-full"
					@change="
						() => {
							mapping.sourceId = '';
							mapping.sourceName = '';
							mapping.staticValue = '';
							mapping.targetFieldId = undefined;
							mapping.targetFieldName = undefined;
							emit('dirty');
						}
					"
				>
					<el-option label="Dynamic field" value="dynamic_field" />
					<el-option label="Questionnaire answer" value="questionnaire" />
					<el-option label="Static value" value="static" />
				</el-select>

				<!-- 来源字段 -->
				<el-select
					v-if="mapping.sourceType !== 'static'"
					v-model="mapping.sourceId"
					placeholder="Select source field"
					class="w-full"
					filterable
					:loading="loading"
					:disabled="loading"
					@change="
						(v: string) => {
							const opts =
								mapping.sourceType === 'dynamic_field'
									? dynamicFieldOptions
									: questionnaireOptions;
							const found = opts.find((o) => o.id === v) as any;
							mapping.sourceName = found?.name;
							// Carry the question type so the backend knows the exact format to write
							mapping.sourceQuestionType =
								mapping.sourceType === 'questionnaire'
									? found?.fieldType
									: undefined;
							// Clear target when source changes — type may no longer be compatible
							mapping.targetFieldId = undefined;
							mapping.targetFieldName = undefined;
							emit('dirty');
						}
					"
				>
					<el-option
						v-for="o in mapping.sourceType === 'dynamic_field'
							? dynamicFieldOptions
							: questionnaireOptions"
						:key="o.id"
						:label="o.name"
						:value="o.id"
					/>
				</el-select>

				<!-- Static value -->
				<el-input
					v-else
					v-model="mapping.staticValue"
					placeholder="Enter static value"
					class="w-full"
					@input="emit('dirty')"
				/>

				<!-- 目标分隔 -->
				<div
					class="flex items-center gap-1 text-xs text-[var(--el-text-color-secondary)] py-0.5"
				>
					<el-icon><ArrowRight /></el-icon>
					Into target field
				</div>

				<!-- 目标字段 -->
				<el-select
					v-model="mapping.targetFieldId"
					placeholder="Select target field"
					class="w-full"
					filterable
					:loading="loading"
					:disabled="loading || (!mapping.sourceId && mapping.sourceType !== 'static')"
					@change="
						(v: string) => {
							mapping.targetFieldName = targetFieldOptions.find(
								(f) => f.id === v
							)?.name;
							emit('dirty');
						}
					"
				>
					<el-option-group
						v-for="group in getCompatibleTargetGroups(mapping)"
						:key="group.label"
						:label="group.label"
					>
						<el-option
							v-for="f in group.options"
							:key="f.id"
							:label="f.name"
							:value="f.id"
						/>
					</el-option-group>
					<template v-if="getCompatibleTargetGroups(mapping).length === 0" #empty>
						<div class="text-center text-xs text-[var(--el-text-color-secondary)] py-3">
							No compatible target fields for this source type
						</div>
					</template>
				</el-select>
			</div>
		</div>

		<!-- Add mapping -->
		<el-button class="conn-add-btn" @click="emit('add-mapping')">
			<el-icon><Plus /></el-icon>
			Add field mapping
		</el-button>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { ArrowRight, Delete, Plus, Sort as Switch } from '@element-plus/icons-vue';

// ── Types ─────────────────────────────────────────────────
interface AutoMappedField {
	id: string;
	sourceName: string;
	targetName: string;
	sourceId: string;
	targetId: string;
	type: string;
	enabled: boolean;
}

interface SourceOptionGroup {
	label: string;
	options: { id: string; name: string }[];
}

interface MappingRow {
	id: string;
	sourceType: 'dynamic_field' | 'questionnaire' | 'static';
	sourceId?: string;
	sourceName?: string;
	sourceQuestionType?: string; // question type for questionnaire source (e.g. 'short_answer_grid')
	targetFieldId?: string;
	targetFieldName?: string;
	staticValue?: string;
	enabled: boolean;
}

// ── Props / Emits ─────────────────────────────────────────
const props = defineProps<{
	caseInfoMappings: AutoMappedField[];
	autoMappedFields: AutoMappedField[];
	localMappings: MappingRow[];
	autoMap: boolean;
	sourceOptionGroups: SourceOptionGroup[];
	dynamicFieldOptions: { id: string; name: string }[];
	questionnaireOptions: { id: string; name: string }[];
	targetFieldOptions: { id: string; name: string }[];
	sourceWorkflowName?: string;
	targetWorkflowName?: string;
	loading?: boolean;
}>();

const emit = defineEmits<{
	'update:autoMap': [value: boolean];
	'add-mapping': [];
	'remove-mapping': [index: number];
	dirty: [];
}>();

// ── Computed ──────────────────────────────────────────────
const fieldMappingCount = computed(
	() =>
		props.caseInfoMappings.filter((f) => f.enabled).length +
		props.localMappings.filter((m) => m.enabled).length +
		(props.autoMap ? props.autoMappedFields.filter((f) => f.enabled).length : 0)
);

// ── Type compatibility ──────────────────────────────────────────
// Strict rules:
//   - dynamic_field  → only dynamic_field target with EXACT same DataType (backend enum name)
//   - questionnaire  → only questionnaire target with EXACT same question type string
//   - static value   → only SingleLineText / MultilineText dynamic_field targets
//   - cross-kind (dynamic ↔ questionnaire) is NOT allowed

/**
 * Returns targetFieldOptions compatible with the current mapping source.
 * Strict: same kind + same type only. No compatibility fallbacks.
 */
const getCompatibleTargets = (mapping: MappingRow) => {
	if (mapping.sourceType === 'static') {
		// Static string → only text dynamic fields (SingleLineText / MultilineText)
		return props.targetFieldOptions.filter((t) => {
			const kind = (t as any).fieldKind ?? 'static_field';
			const ft = (t as any).fieldType ?? '';
			return kind === 'static_field' && (ft === 'SingleLineText' || ft === 'MultilineText');
		});
	}

	if (!mapping.sourceId) return props.targetFieldOptions as any[];

	const allSourceOpts = [
		...(props.dynamicFieldOptions as any[]),
		...(props.questionnaireOptions as any[]),
	];
	const sourceOpt = allSourceOpts.find((o) => o.id === mapping.sourceId);
	if (!sourceOpt) return props.targetFieldOptions as any[];

	const sourceKind: string = sourceOpt.fieldKind ?? 'static_field';
	const sourceType: string = sourceOpt.fieldType ?? '';

	// Strict: target must have same kind AND same type
	return props.targetFieldOptions.filter((t) => {
		const targetKind = (t as any).fieldKind ?? 'static_field';
		const targetType = (t as any).fieldType ?? '';
		return targetKind === sourceKind && targetType === sourceType;
	});
};

// Grouped version for use in el-option-group
const getCompatibleTargetGroups = (mapping: MappingRow) => {
	const compatible = getCompatibleTargets(mapping);
	const map = new Map<string, { id: string; name: string }[]>();
	for (const f of compatible) {
		const g = (f as any).group ?? 'Fields';
		if (!map.has(g)) map.set(g, []);
		map.get(g)!.push(f);
	}
	return Array.from(map.entries()).map(([label, options]) => ({ label, options }));
};

// ── Helpers ───────────────────────────────────────────────
const handleSourceChange = (item: AutoMappedField, v: string) => {
	const allOpts = props.sourceOptionGroups.flatMap((g) => g.options);
	const opt = allOpts.find((o) => o.id === v);
	item.sourceId = v;
	item.sourceName = opt?.name ?? v;
	emit('dirty');
};

/**
 * Returns sourceOptionGroups filtered to only options compatible with the given target field.
 * For auto-map rows: dynamic-field target → only same-type dynamic-field sources.
 * For questionnaire target → only same-type questionnaire sources.
 */
const getCompatibleSourceGroups = (targetId: string) => {
	// Determine target kind and type
	const allTargets = props.targetFieldOptions as any[];
	const targetOpt = allTargets.find((t) => t.id === targetId);
	if (!targetOpt) return props.sourceOptionGroups;

	const targetKind: string = targetOpt.fieldKind ?? 'static_field';
	const targetType: string = targetOpt.fieldType ?? '';

	// Filter each source group's options
	return props.sourceOptionGroups
		.map((group) => ({
			...group,
			options: group.options.filter((o: any) => {
				const sourceKind: string = o.fieldKind ?? 'static_field';
				const sourceType: string = o.fieldType ?? '';
				// Strict: same kind + same type
				return sourceKind === targetKind && sourceType === targetType;
			}),
		}))
		.filter((g) => g.options.length > 0);
};
</script>
