<template>
	<!-- FAB button — bottom-right corner -->
	<Teleport to="body">
		<el-tooltip content="Edit History" placement="left" :show-after="300">
			<div class="doc-history-fab" @click="visible = true">
				<el-icon class="text-base"><Clock /></el-icon>
			</div>
		</el-tooltip>
	</Teleport>

	<!-- Dialog -->
	<el-dialog
		v-model="visible"
		title="Edit History"
		width="min(90vw, 1100px)"
		:close-on-click-modal="false"
		:destroy-on-close="true"
		class="doc-history-dialog"
		@open="onOpen"
	>
		<!-- Filter bar -->
		<div class="doc-history-filters">
			<!-- User filter -->
			<el-select
				v-model="filterUser"
				placeholder="All users"
				clearable
				style="width: 180px"
				@change="applyFilters"
				filterable
			>
				<el-option
					v-for="u in userOptions"
					:key="u.value"
					:label="u.label"
					:value="u.value"
				>
					<div class="flex items-center gap-2">
						<el-avatar
							:size="20"
							:style="{
								background: avatarColor(u.label),
								fontSize: '10px',
								fontWeight: 700,
							}"
						>
							{{ u.label[0]?.toUpperCase() }}
						</el-avatar>
						<span>{{ u.label }}</span>
					</div>
				</el-option>
			</el-select>

			<!-- Details search — server-side with 400ms debounce -->
			<el-input
				v-model="filterSearch"
				placeholder="Search cell, old or new value…"
				clearable
				style="width: 280px"
				:prefix-icon="Search"
				@input="onSearchInput"
				@clear="applyFilters"
			/>

			<!-- Date range filter — server-side -->
			<el-date-picker
				v-model="filterDateRange"
				type="daterange"
				start-placeholder="Start date"
				end-placeholder="End date"
				clearable
				style="width: 260px"
				value-format="YYYY-MM-DD"
				@change="onDateRangeChange"
			/>
		</div>

		<div class="doc-history-table-wrap">
			<el-table
				:data="logs"
				v-loading="loading"
				border
				stripe
				:empty-text="'No history records'"
				style="width: 100%"
			>
				<!-- Operation type with colored tag -->
				<el-table-column label="Operation" width="210">
					<template #default="{ row }">
						<el-tag
							:type="getMutationTagType(row.mutationId)"
							size="small"
							effect="light"
						>
							{{ formatMutationId(row.mutationId) }}
						</el-tag>
					</template>
				</el-table-column>

				<!-- Details: cell: oldValue → newValue -->
				<el-table-column label="Details" min-width="220" show-overflow-tooltip>
					<template #default="{ row }">
						<span v-if="parseDetails(row)" class="text-sm font-mono text-gray-700">
							{{ parseDetails(row) }}
						</span>
						<span v-else class="text-gray-400 text-xs">—</span>
					</template>
				</el-table-column>

				<!-- Updated By -->
				<el-table-column label="Updated By" width="170" show-overflow-tooltip>
					<template #default="{ row }">
						<div
							v-if="row.operatorUserName || row.operatorUserId"
							class="flex items-center gap-2"
						>
							<el-avatar
								:size="22"
								:style="{
									background: avatarColor(
										row.operatorUserName ?? row.operatorUserId ?? ''
									),
									fontSize: '10px',
									fontWeight: 700,
									flexShrink: 0,
								}"
							>
								{{
									((row.operatorUserName ?? row.operatorUserId) ||
										'?')[0]?.toUpperCase()
								}}
							</el-avatar>
							<span class="text-sm text-gray-800 truncate">
								{{ row.operatorUserName || row.operatorUserId }}
							</span>
						</div>
						<span v-else class="text-gray-400 text-sm italic">System</span>
					</template>
				</el-table-column>

				<!-- Date & Time — last column -->
				<el-table-column label="Date & Time" width="195">
					<template #default="{ row }">
						<div class="flex items-center text-gray-600 text-sm">
							<el-icon class="mr-1 text-xs flex-shrink-0"><Clock /></el-icon>
							{{ formatTime(row.createDate ?? row.opTimestamp) }}
						</div>
					</template>
				</el-table-column>
			</el-table>
		</div>

		<CustomerPagination
			:total="total"
			:limit="pageSize"
			:page="pageIndex"
			:background="true"
			@update:page="handlePageChange"
			@update:limit="handleLimitChange"
			@pagination="handlePagination"
		/>

		<template #footer>
			<div class="flex justify-end w-full">
				<el-button @click="visible = false">Close</el-button>
			</div>
		</template>
	</el-dialog>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import {
	ElDialog,
	ElTable,
	ElTableColumn,
	ElButton,
	ElAvatar,
	ElTag,
	ElIcon,
	ElTooltip,
	ElSelect,
	ElOption,
	ElInput,
	ElDatePicker,
} from 'element-plus';
import { Clock, Search } from '@element-plus/icons-vue';
import CustomerPagination from '@/components/global/u-pagination/index.vue';
import { getOperationLogs } from '@/apis/ow/documentOperationLog';
import { getAllUser } from '@/apis/global';
import { timeZoneConvert } from '@/hooks/time';

// ── Props ──────────────────────────────────────────────────────────
const props = defineProps<{ unitId: string }>();

// ── State ──────────────────────────────────────────────────────────
const visible = ref(false);
const loading = ref(false);
const logs = ref<any[]>([]);
const total = ref(0);
const pageIndex = ref(1);
const pageSize = ref(20);

// Filter state
const filterUser = ref(''); // userId — server-side
const filterSearch = ref(''); // keyword — server-side with debounce
const filterDateRange = ref<[string, string] | []>([]); // [from, to] — server-side

// User options loaded from API (independent of pagination)
const userOptions = ref<{ value: string; label: string }[]>([]);
const usersLoaded = ref(false);

// ── Debounce for keyword search ────────────────────────────────────
let searchTimer: ReturnType<typeof setTimeout> | null = null;
function onSearchInput() {
	if (searchTimer) clearTimeout(searchTimer);
	searchTimer = setTimeout(() => {
		searchTimer = null;
		pageIndex.value = 1;
		void load();
	}, 400);
}

// ── Load ───────────────────────────────────────────────────────────
function onOpen() {
	pageIndex.value = 1;
	filterUser.value = '';
	filterSearch.value = '';
	filterDateRange.value = [];
	void loadUsers();
	void load();
}

/** Load all users from API once — populates the filter dropdown */
async function loadUsers() {
	if (usersLoaded.value) return;
	try {
		const res = (await getAllUser()) as any;
		// Axios interceptor unwraps SuccessResponse → res may already be the array
		// or still have a .data wrapper depending on the interceptor version.
		const raw: any[] = Array.isArray(res) ? res : Array.isArray(res?.data) ? res.data : [];
		userOptions.value = raw
			// Keep only real user nodes (not team/group nodes)
			.filter((u: any) => u.id && (u.type === 'user' || !u.type))
			.map((u: any) => ({
				value: String(u.id),
				label: u.username ?? u.username ?? u.name ?? String(u.id),
			}));
		usersLoaded.value = true;
	} catch {
		// Non-critical — filter dropdown stays empty
	}
}

async function load(dateRange?: [string, string] | null) {
	if (!props.unitId) return;
	loading.value = true;
	// Use dateRange passed from @change when available; otherwise fall back to ref value.
	// This avoids the timing issue where @change fires before v-model updates the ref.
	const range = dateRange !== undefined ? dateRange : filterDateRange.value;
	// Extract just YYYY-MM-DD from the value (Element Plus may return "YYYY-MM-DD HH:mm:ss")
	const fromDate = range?.[0] ? String(range[0]).slice(0, 10) : undefined;
	const toDate = range?.[1] ? String(range[1]).slice(0, 10) : undefined;
	try {
		const res = (await getOperationLogs(props.unitId, {
			pageIndex: pageIndex.value,
			pageSize: pageSize.value,
			userId: filterUser.value || undefined,
			keyword: filterSearch.value.trim() || undefined,
			// Send as plain date strings — backend DateTime? parses them as UTC midnight
			from: fromDate,
			to: toDate ? `${toDate}T23:59:59Z` : undefined,
		})) as any;
		const data = res?.data ?? res;
		logs.value = data?.items ?? data?.list ?? (Array.isArray(data) ? data : []);
		total.value = data?.totalCount ?? data?.total ?? logs.value.length;
	} finally {
		loading.value = false;
	}
}

function applyFilters() {
	// User filter is server-side: reset to page 1 and reload
	pageIndex.value = 1;
	void load();
}

/** Receives the new value directly from @change — avoids v-model timing issues.
 *  Debounced to absorb Element Plus's double-fire on clear (emits change twice). */
let dateChangeTimer: ReturnType<typeof setTimeout> | null = null;
function onDateRangeChange(val: [string, string] | null) {
	if (dateChangeTimer) clearTimeout(dateChangeTimer);
	dateChangeTimer = setTimeout(() => {
		dateChangeTimer = null;
		pageIndex.value = 1;
		void load(val);
	}, 50);
}

// ── Pagination ─────────────────────────────────────────────────────
function handlePageChange(page: number) {
	pageIndex.value = page;
}
function handleLimitChange(limit: number) {
	pageSize.value = limit;
	pageIndex.value = 1;
}
function handlePagination({ page, limit }: { page: number; limit: number }) {
	pageIndex.value = page;
	pageSize.value = limit;
	void load();
}

// ── Avatar color ───────────────────────────────────────────────────
const AVATAR_COLORS = [
	'#C53030',
	'#2C7A7B',
	'#2B6CB0',
	'#38A169',
	'#D69E2E',
	'#9F7AEA',
	'#319795',
	'#805AD5',
	'#3182CE',
	'#DD6B20',
];
function avatarColor(name: string): string {
	if (!name) return AVATAR_COLORS[0];
	let h = 0;
	for (let i = 0; i < name.length; i++) h = name.charCodeAt(i) + ((h << 5) - h);
	return AVATAR_COLORS[Math.abs(h) % AVATAR_COLORS.length];
}

// ── Time ───────────────────────────────────────────────────────────
function formatTime(ts: string | number | undefined): string {
	if (!ts) return '—';
	const str = typeof ts === 'number' ? new Date(ts).toISOString() : String(ts);
	return timeZoneConvert(str, false, 'MM/DD/YYYY HH:mm:ss') || str;
}

// ── Mutation labels ────────────────────────────────────────────────
const MUTATION_LABELS: Record<string, string> = {
	'doc.mutation.rich-text-editing': 'Edited Text',
	'doc.mutation.rename-doc': 'Renamed Document',
	'sheet.mutation.set-range-values': 'Set Cell Value',
	'sheet.mutation.set-range-values-dirty': 'Set Cell Value',
	'sheet.mutation.set-range-styles': 'Changed Style',
	'sheet.mutation.insert-row': 'Inserted Row',
	'sheet.mutation.insert-col': 'Inserted Column',
	'sheet.mutation.remove-row': 'Removed Row',
	'sheet.mutation.remove-col': 'Removed Column',
	'sheet.mutation.remove-worksheet': 'Removed Sheet',
	'sheet.mutation.insert-sheet': 'Inserted Sheet',
	'sheet.mutation.merge-undo-mutation': 'Merged Cells',
	'sheet.mutation.remove-worksheet-merge': 'Removed Cell Merge',
	'sheet.mutation.add-worksheet-merge': 'Added Cell Merge',
	'sheet.mutation.set-worksheet-name': 'Renamed Sheet',
	'sheet.mutation.set-num-fmt-mutation': 'Set Number Format',
	'sheet.mutation.set-frozen': 'Set Frozen Pane',
	'sheet.mutation.add-conditional-rule': 'Added Conditional Rule',
	'sheet.mutation.set-worksheet-col-width': 'Set Column Width',
	'sheet.mutation.set-worksheet-row-height': 'Set Row Height',
};

function formatMutationId(id: string = ''): string {
	if (MUTATION_LABELS[id]) return MUTATION_LABELS[id];
	const last = id.split('.').pop() ?? id;
	return last.replace(/-/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase());
}

function getMutationTagType(
	id: string = ''
): 'success' | 'warning' | 'danger' | 'primary' | 'info' {
	if (
		id === 'sheet.mutation.set-range-values' ||
		id === 'sheet.mutation.set-range-values-dirty' ||
		id === 'sheet.mutation.insert-row' ||
		id === 'sheet.mutation.insert-col' ||
		id === 'sheet.mutation.insert-sheet'
	)
		return 'success';

	if (
		id === 'sheet.mutation.remove-row' ||
		id === 'sheet.mutation.remove-col' ||
		id === 'sheet.mutation.remove-worksheet'
	)
		return 'danger';

	if (
		id === 'sheet.mutation.set-range-styles' ||
		id === 'sheet.mutation.merge-undo-mutation' ||
		id === 'sheet.mutation.add-worksheet-merge' ||
		id === 'sheet.mutation.remove-worksheet-merge' ||
		id === 'sheet.mutation.set-worksheet-name' ||
		id === 'doc.mutation.rename-doc'
	)
		return 'warning';

	if (
		id === 'sheet.mutation.set-num-fmt-mutation' ||
		id === 'sheet.mutation.set-frozen' ||
		id === 'sheet.mutation.add-conditional-rule' ||
		id === 'sheet.mutation.set-worksheet-col-width' ||
		id === 'sheet.mutation.set-worksheet-row-height'
	)
		return 'primary';

	return 'info';
}

// ── Column index → A1 letter ───────────────────────────────────────
function colIndexToLetter(colIdx: number): string {
	let result = '';
	let n = colIdx + 1;
	while (n > 0) {
		const rem = (n - 1) % 26;
		result = String.fromCharCode(65 + rem) + result;
		n = Math.floor((n - 1) / 26);
	}
	return result;
}

// ── parseDetails ───────────────────────────────────────────────────
function parseDetails(row: any): string {
	const mutationId: string = row.mutationId ?? '';
	const raw: string = row.paramsJson ?? '';
	if (!raw) return '';
	let params: any;
	try {
		params = typeof raw === 'string' ? JSON.parse(raw) : raw;
	} catch {
		return '';
	}

	// ── Set cell value: "cell: old → new" ────────────────────────────────────
	if (mutationId === 'sheet.mutation.set-range-values' && params.cellValue) {
		const oldValues: Record<string, Record<string, string | null>> = (() => {
			const rawOld = row.oldValuesJson;
			if (!rawOld) return {};
			try {
				return typeof rawOld === 'string' ? JSON.parse(rawOld) : rawOld;
			} catch {
				return {};
			}
		})();

		const parts: string[] = [];
		for (const [rowStr, cols] of Object.entries<any>(params.cellValue)) {
			for (const [colStr, cell] of Object.entries<any>(cols)) {
				const cellRef = `${colIndexToLetter(Number(colStr))}${Number(rowStr) + 1}`;
				const newVal =
					cell?.v !== null && cell?.v !== undefined && cell?.v !== ''
						? String(cell.v)
						: null;
				const oldVal = oldValues[rowStr]?.[colStr] ?? null;
				if (oldVal === newVal) continue;
				parts.push(`${cellRef}: ${oldVal ?? '(empty)'} → ${newVal ?? '(cleared)'}`);
			}
		}
		if (!parts.length) return '';
		const preview = parts.slice(0, 3).join('  ·  ');
		return parts.length > 3 ? `${preview}  … (+${parts.length - 3})` : preview;
	}

	// ── Style change ─────────────────────────────────────────────────────────
	if (mutationId === 'sheet.mutation.set-range-styles' && params.ranges?.length) {
		const r = params.ranges[0];
		const tl = `${colIndexToLetter(r.startColumn ?? 0)}${(r.startRow ?? 0) + 1}`;
		const br = `${colIndexToLetter(r.endColumn ?? r.startColumn ?? 0)}${
			(r.endRow ?? r.startRow ?? 0) + 1
		}`;
		return tl === br ? tl : `${tl}:${br}`;
	}

	// ── Insert / remove row ──────────────────────────────────────────────────
	if (
		(mutationId === 'sheet.mutation.insert-row' ||
			mutationId === 'sheet.mutation.remove-row') &&
		params.range
	) {
		const s = (params.range.startRow ?? 0) + 1;
		const e = (params.range.endRow ?? params.range.startRow ?? 0) + 1;
		return s === e ? `Row ${s}` : `Row ${s}–${e}`;
	}

	// ── Insert / remove column ───────────────────────────────────────────────
	if (
		(mutationId === 'sheet.mutation.insert-col' ||
			mutationId === 'sheet.mutation.remove-col') &&
		params.range
	) {
		const s = colIndexToLetter(params.range.startColumn ?? 0);
		const e = colIndexToLetter(params.range.endColumn ?? params.range.startColumn ?? 0);
		return s === e ? `Col ${s}` : `Col ${s}–${e}`;
	}

	// ── Column width ─────────────────────────────────────────────────────────
	// params: { unitId, subUnitId, ranges: IRange[], colWidth: number | {0: w, 1: w, ...} }
	if (mutationId === 'sheet.mutation.set-worksheet-col-width') {
		const ranges: any[] = params.ranges ?? [];
		const rawWidth = params.colWidth;

		// colWidth can be a plain number (same width for all) or an object
		// keyed by column index when widths differ: { "0": 120, "1": 85 }
		const getWidth = (colIdx: number): string => {
			if (rawWidth === null || rawWidth === undefined) return '';
			if (typeof rawWidth === 'number') return `${Math.round(rawWidth)}px`;
			if (typeof rawWidth === 'object') {
				const v = rawWidth[String(colIdx)] ?? rawWidth[colIdx];
				return v !== null && v !== undefined ? `${Math.round(Number(v))}px` : '';
			}
			return '';
		};

		const parts: string[] = [];
		for (const r of ranges.slice(0, 4)) {
			const s = r.startColumn ?? 0;
			const e = r.endColumn ?? s;
			const colStr =
				s === e
					? `Col ${colIndexToLetter(s)}`
					: `Col ${colIndexToLetter(s)}–${colIndexToLetter(e)}`;
			const w = getWidth(s);
			parts.push(w ? `${colStr}: ${w}` : colStr);
		}

		return parts.join(', ') || 'Col ?';
	}

	// ── Row height ───────────────────────────────────────────────────────────
	if (mutationId === 'sheet.mutation.set-worksheet-row-height') {
		const height: number | undefined = params.rowHeight ?? params.height;
		const heightStr = height !== undefined ? `${Math.round(height)}px` : '';

		const rows: string[] = [];
		const ranges: any[] = params.ranges ?? (params.startRow !== undefined ? [params] : []);
		for (const r of ranges.slice(0, 4)) {
			const s = (r.startRow ?? 0) + 1;
			const e = (r.endRow ?? r.startRow ?? 0) + 1;
			rows.push(s === e ? `Row ${s}` : `Row ${s}–${e}`);
		}

		const rowStr = rows.join(', ') || 'Row ?';
		return heightStr ? `${rowStr}: ${heightStr}` : rowStr;
	}

	// ── Rename sheet ─────────────────────────────────────────────────────────
	if (mutationId === 'sheet.mutation.set-worksheet-name' && params.name) return params.name;

	// ── Doc text edit ────────────────────────────────────────────────────────
	if (mutationId === 'doc.mutation.rich-text-editing') {
		const body =
			params.body ?? params.mutations?.[0]?.body ?? params.mutations?.[0]?.params?.body;
		if (body?.dataStream) {
			return String(body.dataStream)
				.replace(/\r\n|\n|\r/g, ' ')
				.slice(0, 80);
		}
	}

	// ── Conditional rule ─────────────────────────────────────────────────────
	if (mutationId === 'sheet.mutation.add-conditional-rule' && params.rule) {
		const r = params.rule;
		const parts: string[] = [];
		if (r.ranges?.length) {
			const rangeParts = (r.ranges as any[]).slice(0, 3).map((range: any) => {
				const tl = `${colIndexToLetter(range.startColumn ?? 0)}${
					(range.startRow ?? 0) + 1
				}`;
				const br = `${colIndexToLetter(range.endColumn ?? range.startColumn ?? 0)}${
					(range.endRow ?? range.startRow ?? 0) + 1
				}`;
				return tl === br ? tl : `${tl}:${br}`;
			});
			parts.push(`Range: ${rangeParts.join(', ')}`);
		}
		const cfg = r.rule;
		if (cfg) {
			const condDesc = describeCFRule(cfg);
			if (condDesc) parts.push(`Rule: ${condDesc}`);
		}
		return parts.join('  ·  ') || '';
	}

	return '';
}

/** Human-readable description of a conditional formatting rule config */
function describeCFRule(cfg: any): string {
	const type: string = cfg.type ?? '';
	const subType: string = cfg.subType ?? '';

	if (type === 'highlightCell') {
		switch (subType) {
			case 'uniqueValues':
				return 'Unique values';
			case 'duplicateValues':
				return 'Duplicate values';
			case 'average': {
				const m: Record<string, string> = {
					greaterThan: 'above average',
					greaterThanOrEqual: 'above or equal to average',
					lessThan: 'below average',
					lessThanOrEqual: 'below or equal to average',
					equal: 'equal to average',
					notEqual: 'not equal to average',
				};
				return m[cfg.operator] ?? 'Average';
			}
			case 'rank': {
				const dir = cfg.isBottom ? 'Bottom' : 'Top';
				return `${dir} ${cfg.value ?? ''}${cfg.isPercent ? '%' : ''}`;
			}
			case 'text': {
				const m: Record<string, string> = {
					beginsWith: 'begins with',
					endsWith: 'ends with',
					containsText: 'contains',
					notContainsText: 'does not contain',
					equal: '=',
					notEqual: '≠',
					containsBlanks: 'contains blanks',
					notContainsBlanks: 'no blanks',
					containsErrors: 'contains errors',
					notContainsErrors: 'no errors',
				};
				const op = m[cfg.operator] ?? cfg.operator ?? '';
				return cfg.value !== undefined ? `Text ${op} "${cfg.value}"` : `Text ${op}`;
			}
			case 'timePeriod': {
				const m: Record<string, string> = {
					today: 'Today',
					yesterday: 'Yesterday',
					tomorrow: 'Tomorrow',
					last7Days: 'Last 7 days',
					thisWeek: 'This week',
					lastWeek: 'Last week',
					nextWeek: 'Next week',
					thisMonth: 'This month',
					lastMonth: 'Last month',
					nextMonth: 'Next month',
				};
				return m[cfg.operator] ?? cfg.operator ?? 'Time period';
			}
			case 'number': {
				const m: Record<string, string> = {
					greaterThan: '>',
					greaterThanOrEqual: '≥',
					lessThan: '<',
					lessThanOrEqual: '≤',
					equal: '=',
					notEqual: '≠',
					between: 'between',
					notBetween: 'not between',
				};
				const op = m[cfg.operator] ?? cfg.operator ?? '';
				if (Array.isArray(cfg.value))
					return `Number ${op} ${cfg.value[0]} and ${cfg.value[1]}`;
				return cfg.value !== undefined ? `Number ${op} ${cfg.value}` : `Number ${op}`;
			}
			case 'formula':
				return `Formula: ${cfg.value ?? ''}`;
			default:
				return subType || 'Highlight cell';
		}
	}
	if (type === 'dataBar') {
		const color = cfg.config?.positiveColor ?? '';
		return color ? `Data bar (${color})` : 'Data bar';
	}
	if (type === 'colorScale') {
		const colors = (cfg.config ?? []).map((c: any) => c.color).filter(Boolean);
		return colors.length ? `Color scale: ${colors.join(' → ')}` : 'Color scale';
	}
	if (type === 'iconSet') {
		const iconType = cfg.config?.[0]?.iconType ?? '';
		return iconType ? `Icon set: ${iconType}` : 'Icon set';
	}
	return type || '';
}
</script>

<style lang="scss">
// FAB button — fixed at bottom-right
.doc-history-fab {
	position: fixed;
	bottom: 80px;
	right: 24px;
	width: 40px;
	height: 40px;
	border-radius: 50%;
	background: #fff;
	box-shadow: 0 2px 12px rgba(0, 0, 0, 0.18);
	display: flex;
	align-items: center;
	justify-content: center;
	cursor: pointer;
	color: #606266;
	z-index: 100;
	transition:
		box-shadow 0.2s,
		color 0.2s;

	&:hover {
		box-shadow: 0 4px 16px rgba(0, 0, 0, 0.24);
		color: #409eff;
	}
}

// Dialog overrides
.doc-history-dialog {
	.el-dialog__body {
		padding: 0;
	}

	.el-dialog__footer {
		padding: 12px 20px;
		border-top: 1px solid #e4e7ed;
	}
}

// Filter bar
.doc-history-filters {
	display: flex;
	align-items: center;
	gap: 12px;
	padding: 10px 16px;
	border-bottom: 1px solid #e4e7ed;
	background: #fafafa;
}

// Table wrapper — fixed height, both axes scrollable
.doc-history-table-wrap {
	height: 52vh;
	overflow: auto;

	.el-table {
		height: 100%;
	}

	.el-table__inner-wrapper {
		height: 100%;
	}
}
</style>
