<template>
	<div ref="wrapperRef" class="univer-editor-wrapper" :style="{ height }">
		<div ref="containerRef" class="univer-editor-container"></div>
		<!-- Remote collaborator name tags (positioned over the canvas) -->
		<div ref="tagLayerRef" class="univer-collab-tag-layer"></div>
	</div>
</template>

<script setup lang="ts">
import { ref, watch, onMounted, onBeforeUnmount, toRaw } from 'vue';
import { ElMessage } from 'element-plus';
import { createUniver, LocaleType, CommandType } from '@univerjs/presets';
import { UniverSheetsCorePreset } from '@univerjs/preset-sheets-core';
import '@univerjs/preset-sheets-core/lib/index.css';
import { UniverSheetsDrawingPreset } from '@univerjs/preset-sheets-drawing';
import { UniverSheetsConditionalFormattingPreset } from '@univerjs/preset-sheets-conditional-formatting';
import { UniverSheetsFilterPreset } from '@univerjs/preset-sheets-filter';
import { UniverSheetsHyperLinkPreset } from '@univerjs/preset-sheets-hyper-link';
import { UniverSheetsDataValidationPreset } from '@univerjs/preset-sheets-data-validation';
import { UniverSheetsFindReplacePreset } from '@univerjs/preset-sheets-find-replace';
import { UniverSheetsNotePreset } from '@univerjs/preset-sheets-note';
import { UniverSheetsSortPreset } from '@univerjs/preset-sheets-sort';
import { UniverSheetsTablePreset } from '@univerjs/preset-sheets-table';
import { UniverSheetsThreadCommentPreset } from '@univerjs/preset-sheets-thread-comment';
import sheetsEnUS from '@univerjs/preset-sheets-core/locales/en-US';
import type { CollaboratorInfo, PresenceState } from '@/stores/modules/documentEditor';
import type {
	RangeProtectionEditState,
	RangeProtectionMutationParams,
	RangeProtectionMutationRule,
} from '#/permission';
import { normalizeWorkbookSnapshot } from './univerIds';
import { stripRolePrefix, makeAvatarDataUri, extractCreatorTag } from '@/utils/sheetPermission';

export type RemoteUser = CollaboratorInfo & Partial<PresenceState>;

interface AllUser {
	userId: string;
	name: string;
	avatar?: string;
}

interface Props {
	workbookData?: object;
	height?: string;
	readonly?: boolean;

	docId?: string;

	userId?: string;

	remoteUsers?: RemoteUser[];

	allUsers?: AllUser[];
}

const props = withDefaults(defineProps<Props>(), {
	height: '100%',
	readonly: false,
	remoteUsers: () => [],
	allUsers: () => [],
});

const emit = defineEmits<{
	mutation: [
		info: {
			mutationId: string;
			params: unknown;

			data: string;
			timestamp: number;
			fromRemote: boolean;
		},
	];
	presence: [presence: PresenceState];
	ready: [];

	permissionStatus: [status: 'open' | 'owner' | 'blocked'];
}>();

const wrapperRef = ref<HTMLDivElement>();
const containerRef = ref<HTMLDivElement>();
const tagLayerRef = ref<HTMLDivElement>();

let univerInstance: ReturnType<typeof createUniver> | null = null;
let univerAPI: any = null;
const disposables: Array<{ dispose: () => void }> = [];

let _refreshPermissionOverrides: (() => void) | null = null;
let _refreshPermissionPoints: (() => Promise<void>) | null = null;

const _remoteProtectedPermIds = new Set<string>();

let _ruleModel: any = null;

let _authzService: any = null;

const highlights = new Map<string, { key: string; disposable: any }>();

const tags = new Map<string, HTMLDivElement>();

let editingCell: { row: number; column: number } | null = null;

function sheetOf(sheetId?: string) {
	const workbook = univerAPI?.getActiveWorkbook?.();
	if (!workbook) return null;
	if (sheetId) return workbook.getSheetBySheetId?.(sheetId) ?? workbook.getActiveSheet?.();
	return workbook.getActiveSheet?.();
}

function withAlpha(color: string, alpha: number): string {
	const hex = (color || '#4285F4').replace('#', '');
	if (hex.length === 3) {
		const [r, g, b] = hex.split('');
		return `rgba(${parseInt(r + r, 16)}, ${parseInt(g + g, 16)}, ${parseInt(
			b + b,
			16
		)}, ${alpha})`;
	}
	if (hex.length >= 6) {
		const r = parseInt(hex.slice(0, 2), 16);
		const g = parseInt(hex.slice(2, 4), 16);
		const b = parseInt(hex.slice(4, 6), 16);
		return `rgba(${r}, ${g}, ${b}, ${alpha})`;
	}
	return `rgba(66, 133, 244, ${alpha})`;
}

function richTextToPlain(value: any): string {
	if (value == null) return '';
	const stream = value?.body?.dataStream;
	if (typeof stream === 'string') return stream.replace(/[\r\n\t]+/g, ' ').trim();
	if (typeof value === 'string' || typeof value === 'number') return String(value);
	return '';
}

function rangeText(range: any): string {
	try {
		const display = range?.getDisplayValue?.();
		if (typeof display === 'string' && display.length > 0) return display.slice(0, 60);
		const plain = richTextToPlain(range?.getValue?.(true) ?? range?.getValue?.());
		return plain.slice(0, 60);
	} catch {
		return '';
	}
}

function publishPresence(extra: Partial<PresenceState> = {}) {
	const sheet = sheetOf();
	if (!sheet) return;
	let a1 = '';
	let text = '';
	try {
		const range = sheet.getActiveRange?.();
		a1 = range?.getA1Notation?.() ?? '';
		text = range ? rangeText(range) : '';
	} catch {
		// ignore presence is best effort
	}
	emit('presence', {
		sheetId: sheet.getSheetId?.(),
		a1,
		text,
		editing: Boolean(editingCell),
		updatedAt: Date.now(),
		...extra,
	});
}

let editProgressTimer: number | undefined;
let pendingEditProgress: Partial<PresenceState> | null = null;

function flushEditProgress() {
	const snapshot = pendingEditProgress;
	pendingEditProgress = null;
	if (!snapshot) return;
	emit('presence', { ...snapshot, updatedAt: Date.now() } as PresenceState);
}

function discardPendingEditProgress() {
	pendingEditProgress = null;
	if (editProgressTimer) {
		clearTimeout(editProgressTimer);
		editProgressTimer = undefined;
	}
}

function publishEditProgress(snapshot: Partial<PresenceState>) {
	pendingEditProgress = snapshot;
	if (editProgressTimer) return;
	flushEditProgress();
	editProgressTimer = window.setTimeout(() => {
		editProgressTimer = undefined;
		flushEditProgress();
	}, 250);
}

async function resolveSelectionPermissionStatus(): Promise<'open' | 'owner' | 'blocked'> {
	try {
		const injector = univerInstance?.univer.__getInjector?.();
		if (!injector) return 'open';
		const { IAuthzIoService } = await import('@univerjs/core');
		const authzService = injector.get(IAuthzIoService as any) as any;
		if (!authzService) return 'open';

		const permMap: Map<string, any> | undefined = authzService._permissionMap;
		if (!permMap || permMap.size === 0) return 'open';

		const currentId = props.userId ?? '';
		const ownerUID = currentId ? `Owner-${currentId}` : '';

		const matchesUser = (
			c: { subject?: { userID?: string }; id?: string },
			uid: string
		): boolean => {
			const cUid = String(c.subject?.userID ?? c.id ?? '');
			return stripRolePrefix(cUid) === stripRolePrefix(uid);
		};

		let foundProtection = false;
		let userIsOwner = false;

		for (const [, permissionData] of permMap) {
			const collaborators: any[] =
				permissionData.selectRangeObject?.collaborators ??
				permissionData.worksheetObject?.collaborators ??
				[];

			foundProtection = true;

			const isOwner = collaborators.some((c: any) => {
				const cRole: number = c.role ?? 1;
				return cRole === 2 && (matchesUser(c, currentId) || matchesUser(c, ownerUID));
			});

			if (isOwner) {
				userIsOwner = true;
				break;
			}
		}

		if (!foundProtection) return 'open';
		return userIsOwner ? 'owner' : 'blocked';
	} catch {
		return 'open';
	}
}

function registerPresenceListeners() {
	const Event = univerAPI?.Event;
	if (!Event) return;

	disposables.push(
		univerAPI.addEvent(Event.SelectionChanged, async () => {
			if (editingCell) return; // edits are published on edit end
			publishPresence();
			const status = await resolveSelectionPermissionStatus();
			emit('permissionStatus', status);
		})
	);

	disposables.push(
		univerAPI.addEvent(Event.SheetEditStarted, (payload: any) => {
			editingCell = { row: payload?.row, column: payload?.column };
			const sheet = payload?.worksheet ?? sheetOf();
			let a1 = '';
			try {
				a1 =
					sheet?.getRange?.(payload?.row ?? 0, payload?.column ?? 0)?.getA1Notation?.() ??
					'';
			} catch {
				a1 = '';
			}
			emit('presence', {
				sheetId: sheet?.getSheetId?.(),
				a1,
				text: '',
				editing: true,
				updatedAt: Date.now(),
			});
		})
	);

	disposables.push(
		univerAPI.addEvent(Event.SheetEditChanging, (payload: any) => {
			const sheet = payload?.worksheet ?? sheetOf();
			let a1 = '';
			try {
				a1 =
					sheet?.getRange?.(payload?.row ?? 0, payload?.column ?? 0)?.getA1Notation?.() ??
					'';
			} catch {
				a1 = '';
			}
			publishEditProgress({
				sheetId: sheet?.getSheetId?.(),
				a1,
				text: richTextToPlain(payload?.value).slice(0, 60),
				editing: true,
			});
		})
	);

	disposables.push(
		univerAPI.addEvent(Event.BeforeSheetEditEnd, (payload: any) => {
			const sheet = payload?.worksheet ?? sheetOf();
			let a1 = '';
			try {
				a1 =
					sheet?.getRange?.(payload?.row ?? 0, payload?.column ?? 0)?.getA1Notation?.() ??
					'';
			} catch {
				a1 = '';
			}
			emit('presence', {
				sheetId: sheet?.getSheetId?.(),
				a1,
				text: richTextToPlain(payload?.value).slice(0, 60),
				editing: true,
				updatedAt: Date.now(),
			});
		})
	);

	disposables.push(
		univerAPI.addEvent(Event.SheetEditEnded, () => {
			editingCell = null;
			discardPendingEditProgress();
			publishPresence();
		})
	);

	disposables.push(univerAPI.addEvent(Event.Scroll, () => scheduleTagRender()));
	disposables.push(
		univerAPI.addEvent(Event.SheetSkeletonChanged, () => {
			syncScrollSubscription();
			applyHighlights();
			scheduleTagRender();
		})
	);
}

function applyHighlights() {
	const workbook = univerAPI?.getActiveWorkbook?.();
	const activeSheet = workbook?.getActiveSheet?.();
	const activeSheetId = activeSheet?.getSheetId?.();
	const alive = new Set<string>();

	for (const user of props.remoteUsers) {
		if (!user.a1 || !user.sheetId || user.sheetId !== activeSheetId) continue;
		alive.add(user.userId);
		const key = `${user.sheetId}|${user.a1}|${user.color}|${user.editing ? 'editing' : 'idle'}`;
		const existing = highlights.get(user.userId);
		if (existing && existing.key === key) continue;
		existing?.disposable?.dispose?.();

		let disposable: any = null;
		try {
			const sheet = sheetOf(user.sheetId);
			const range = sheet.getRange(user.a1);
			const style = {
				strokeWidth: 2,
				stroke: user.color,
				fill: withAlpha(user.color, user.editing ? 0.2 : 0.1),
				isAnimationDash: false,
			};
			disposable =
				sheet.highlightRanges?.([range], style, null) ??
				range.highlight?.(style, null) ??
				null;
		} catch (error) {
			console.warn('[collab] failed to highlight remote selection', error);
		}
		highlights.set(user.userId, { key, disposable });
	}

	for (const [userId, entry] of Array.from(highlights.entries())) {
		if (alive.has(userId)) continue;
		entry.disposable?.dispose?.();
		highlights.delete(userId);
	}
}

function tagLabel(user: RemoteUser): string {
	const suffix = user.text
		? `: ${user.text.length > 12 ? `${user.text.slice(0, 12)}` : user.text}`
		: '';
	return `${user.name}${suffix}`;
}

let viewportScrollX = 0;
let viewportScrollY = 0;
let scrollDisposable: { dispose?: () => void } | null = null;
let trackedSheetId = '';

function resetViewportScroll() {
	viewportScrollX = 0;
	viewportScrollY = 0;
}

function readViewportScroll(sheet: any) {
	try {
		const state = sheet?.getScrollState?.();
		if (!state) return null;
		const skeleton = sheet?.getSkeleton?.();
		const colStart = skeleton?.columnWidthAccumulation?.[state.sheetViewStartColumn - 1] ?? 0;
		const rowStart = skeleton?.rowHeightAccumulation?.[state.sheetViewStartRow - 1] ?? 0;
		return {
			x: (Number(colStart) || 0) + (Number(state.offsetX) || 0),
			y: (Number(rowStart) || 0) + (Number(state.offsetY) || 0),
		};
	} catch {
		return null;
	}
}

function subscribeViewportScroll() {
	scrollDisposable?.dispose?.();
	scrollDisposable = null;
	const sheet = sheetOf();
	if (!sheet?.onScroll) return;
	try {
		scrollDisposable = sheet.onScroll((state: any) => {
			if (!state) return;
			const x = Number(state.viewportScrollX) || 0;
			const y = Number(state.viewportScrollY) || 0;
			if (x === viewportScrollX && y === viewportScrollY) return;
			viewportScrollX = x;
			viewportScrollY = y;
			scheduleTagRender();
		});
	} catch (error) {
		console.warn('[collab] cannot track worksheet scroll', error);
	}
}

function syncScrollSubscription() {
	const sheetId = sheetOf()?.getSheetId?.() ?? '';
	if (sheetId === trackedSheetId) return;
	trackedSheetId = sheetId;
	resetViewportScroll();
	const state = readViewportScroll(sheetOf());
	if (state) {
		viewportScrollX = state.x;
		viewportScrollY = state.y;
	}
	subscribeViewportScroll();
}

function canvasMetrics() {
	const fallback = { scale: 1, offsetX: 0, offsetY: 0 };
	const wrapper = wrapperRef.value;
	const container = containerRef.value;
	if (!wrapper || !container) return fallback;
	const wrapperRect = wrapper.getBoundingClientRect();
	let canvas: HTMLCanvasElement | undefined;
	let bestArea = 0;
	for (const el of Array.from(container.querySelectorAll('canvas'))) {
		const rect = el.getBoundingClientRect();
		const area = rect.width * rect.height;
		if (area > bestArea) {
			bestArea = area;
			canvas = el;
		}
	}
	if (!canvas) return fallback;
	const rect = canvas.getBoundingClientRect();
	const declaredWidth = Number.parseFloat(canvas.style.width) || rect.width;
	return {
		scale: declaredWidth > 0 ? rect.width / declaredWidth : 1,
		offsetX: rect.left - wrapperRect.left,
		offsetY: rect.top - wrapperRect.top,
	};
}

let rafId = 0;

function scheduleTagRender() {
	ensureTagLoop();
	if (rafId) return;
	rafId = requestAnimationFrame(renderTags);
}

function getCellScreenPosition(
	sheet: any,
	a1: string
): { left: number; top: number; width: number; height: number } | null {
	try {
		const match = /^([A-Z]+)(\d+)/i.exec(a1.split(':')[0]);
		if (!match) return null;
		const col =
			match[1]
				.toUpperCase()
				.split('')
				.reduce((n, c) => n * 26 + (c.charCodeAt(0) - 64), 0) - 1;
		const row = parseInt(match[2], 10) - 1;
		if (row < 0 || col < 0) return null;

		const skeleton = sheet.getSkeleton?.();
		if (!skeleton) return null;

		const rowAccum: number[] = skeleton.rowHeightAccumulation ?? [];
		const colAccum: number[] = skeleton.columnWidthAccumulation ?? [];

		const rowHeaderWidth: number = Number(
			skeleton.rowHeaderWidth ?? sheet.getConfig?.()?.rowHeader?.width ?? 46
		);
		const columnHeaderHeight: number = Number(
			skeleton.columnHeaderHeight ?? sheet.getConfig?.()?.columnHeader?.height ?? 20
		);

		const rowStart = row === 0 ? 0 : rowAccum[row - 1] ?? 0;
		const rowEnd = rowAccum[row] ?? rowStart + 24;
		const colStart = col === 0 ? 0 : colAccum[col - 1] ?? 0;
		const colEnd = colAccum[col] ?? colStart + 88;

		const metrics = canvasMetrics();
		const zoom = Number(sheet.getZoom?.()) || 1;
		const scale = metrics.scale * zoom;

		return {
			left: (colStart + rowHeaderWidth - viewportScrollX) * scale + metrics.offsetX,
			top: (rowStart + columnHeaderHeight - viewportScrollY) * scale + metrics.offsetY,
			width: (colEnd - colStart) * scale,
			height: (rowEnd - rowStart) * scale,
		};
	} catch {
		return null;
	}
}

function renderTags() {
	rafId = 0;
	const layer = tagLayerRef.value;
	const wrapper = wrapperRef.value;
	if (!layer || !wrapper) return;

	const workbook = univerAPI?.getActiveWorkbook?.();
	const activeSheet = workbook?.getActiveSheet?.();
	const activeSheetId = activeSheet?.getSheetId?.();
	const wrapperRect = wrapper.getBoundingClientRect();
	const alive = new Set<string>();

	for (const user of props.remoteUsers) {
		if (!user.a1 || !user.sheetId || user.sheetId !== activeSheetId) continue;
		alive.add(user.userId);

		let el = tags.get(user.userId);
		if (!el) {
			el = document.createElement('div');
			el.className = 'univer-collab-tag';
			layer.appendChild(el);
			tags.set(user.userId, el);
		}

		const pos = getCellScreenPosition(activeSheet, user.a1);
		if (!pos || pos.width === 0) {
			el.style.display = 'none';
			continue;
		}

		const { left, top, width, height } = pos;

		const tagLeft = Math.min(Math.round(left + width - 2), Math.round(wrapperRect.width - 4));
		const tagTop = Math.max(2, Math.round(top - 20));

		const visible =
			left + width > 0 &&
			top + height > 0 &&
			left < wrapperRect.width &&
			top < wrapperRect.height;

		if (!visible) {
			el.style.display = 'none';
			continue;
		}

		el.style.display = 'block';
		el.style.transform = `translate(${tagLeft}px, ${tagTop}px)`;
		el.textContent = tagLabel(user);
		el.style.background = user.color;
		el.style.borderColor = user.color;
	}

	for (const [userId, el] of Array.from(tags.entries())) {
		if (alive.has(userId)) continue;
		el.remove();
		tags.delete(userId);
	}
}

let tagTimer: number | undefined;
let disposed = false;

function ensureTagLoop() {
	if (disposed || tagTimer) return;
	if (!props.remoteUsers.some((u) => u.a1)) return;
	tagTimer = window.setTimeout(() => {
		tagTimer = undefined;
		renderTags();
		ensureTagLoop();
	}, 120);
}

let ownUnitId = '';
let ownSheetIds: string[] = [];

function localSheetIds(): Set<string> {
	const ids = new Set<string>();
	const sheets = univerAPI?.getActiveWorkbook?.()?.getSheets?.();
	if (Array.isArray(sheets)) {
		for (const sheet of sheets) {
			const id = sheet?.getSheetId?.();
			if (typeof id === 'string' && id) ids.add(id);
		}
	}
	if (ids.size === 0) for (const id of ownSheetIds) ids.add(id);
	return ids;
}

function localizeParams(params: any): { params: any; ok: boolean } {
	if (!params || typeof params !== 'object' || Array.isArray(params)) {
		return { params, ok: true };
	}
	const unitId = univerAPI?.getActiveWorkbook?.()?.getId?.() || ownUnitId;
	if (!unitId) return { params, ok: true };

	const next = { ...params };
	if (typeof next.unitId === 'string' && next.unitId !== unitId) next.unitId = unitId;
	if (typeof next.subUnitId === 'string' && !localSheetIds().has(next.subUnitId)) {
		return { params: next, ok: false };
	}
	return { params: next, ok: true };
}

async function applyRemoteMutations(mutations: Array<{ id: string; data: string }>) {
	if (!univerAPI) return;
	const SHEET_MUTATION_PREFIX = ['sheet.mutation.', 'sheets.mutation.'];
	const DOC_ONLY_MUTATIONS = ['doc.mutation.rich-text-editing', 'doc.mutation.'];
	const sheetMutations = mutations.filter((m) => {
		if (SHEET_MUTATION_PREFIX.some((p) => m.id.startsWith(p))) return true;
		if (DOC_ONLY_MUTATIONS.some((p) => m.id.startsWith(p))) return false;
		return true;
	});
	for (const mutation of sheetMutations) {
		const isProtectionMutation =
			typeof mutation.id === 'string' &&
			(mutation.id.includes('range-protection') ||
				mutation.id.includes('worksheet-protection'));
		try {
			const rawParams = JSON.parse(mutation.data);
			const localized = localizeParams(rawParams);
			if (!localized.ok) {
				if (isProtectionMutation) {
					try {
						await univerAPI.executeCommand(mutation.id, rawParams, {
							fromCollab: true,
						});
					} catch {
						// ignore  permission refresh below handles the state update
					}
				}
				continue;
			}
			await univerAPI.executeCommand(mutation.id, localized.params, { fromCollab: true });
		} catch (error) {
			console.warn('[collab] failed to apply remote mutation', mutation.id, error);
		}

		if (isProtectionMutation) {
			const mutationParams = JSON.parse(mutation.data) as RangeProtectionMutationParams;
			setTimeout(async () => {
				try {
					const injector2 = univerInstance?.univer?.__getInjector?.();
					if (!injector2) return;
					const { IPermissionService } = await import('@univerjs/core');
					const { getAllRangePermissionPoint } = await import(
						'@univerjs/preset-sheets-core'
					);
					const permSvc2 = injector2.get(IPermissionService as unknown as object) as {
						updatePermissionPoint(id: string, allowed: boolean): void;
					};
					const allPts = ((getAllRangePermissionPoint as () => unknown[])?.() ??
						[]) as Array<
						new (
							unitId: string,
							subUnitId: string,
							permId: string
						) => {
							subType: number;
							id: string;
						}
					>;
					if (!permSvc2 || allPts.length === 0) return;

					const unitId = mutationParams?.unitId ?? '';
					const subUnitId = mutationParams?.subUnitId ?? '';
					const rules: RangeProtectionMutationRule[] = mutationParams?.rules ?? [];

					for (const rule of rules) {
						const pid = rule.permissionId;
						if (!pid) continue;
						const editState: RangeProtectionEditState | '' = rule.editState ?? '';
						const allowedUsers: string[] = rule.allowedUsers ?? [];

						if (mutation.id.includes('add') || mutation.id.includes('set')) {
							_remoteProtectedPermIds.add(pid);
						} else if (mutation.id.includes('delete')) {
							_remoteProtectedPermIds.delete(pid);
						} else {
							_remoteProtectedPermIds.add(pid);
						}

						let canEdit: boolean;
						let canManage: boolean;
						if (editState === 'onlyMe') {
							canEdit = false;
							canManage = false;
						} else if (editState === 'designedUserCanEdit') {
							canEdit = allowedUsers.some(
								(u) => stripRolePrefix(u) === stripRolePrefix(props.userId ?? '')
							);
							canManage = false;
						} else {
							continue;
						}

						for (const Pt of allPts) {
							const pt = new Pt(unitId, subUnitId, pid);
							let val: boolean;
							if (pt.subType === 1) val = canEdit;
							else if (pt.subType === 2) val = canManage;
							else val = true;
							permSvc2.updatePermissionPoint(pt.id, val);
						}
					}
				} catch {
					// non-critical  worst case the permission defaults apply
				}

				const overFn = _refreshPermissionOverrides;
				if (overFn) {
					overFn();
				}
			}, 50);
		}
	}
}

onMounted(async () => {
	univerInstance = createUniver({
		locale: LocaleType.EN_US,
		locales: { [LocaleType.EN_US]: sheetsEnUS },
		presets: [
			UniverSheetsCorePreset({ container: containerRef.value! }),
			UniverSheetsDrawingPreset(),
			UniverSheetsConditionalFormattingPreset(),
			UniverSheetsFilterPreset(),
			UniverSheetsHyperLinkPreset(),
			UniverSheetsDataValidationPreset(),
			UniverSheetsFindReplacePreset(),
			UniverSheetsNotePreset(),
			UniverSheetsSortPreset(),
			UniverSheetsTablePreset(),
			UniverSheetsThreadCommentPreset(),
		],
	});

	univerAPI = univerInstance.univerAPI;

	// toRaw: the snapshot must never reach Univer as a reactive proxy (KD-02).
	const snapshot = normalizeWorkbookSnapshot(toRaw(props.workbookData as any) ?? {}, props.docId);
	ownUnitId = typeof snapshot.id === 'string' ? snapshot.id : '';
	ownSheetIds = Object.keys((snapshot.sheets as Record<string, unknown>) ?? {});

	try {
		const injector = univerInstance?.univer?.__getInjector?.();
		if (injector) {
			let UserManagerService: any, IAuthzIoService: any;
			try {
				const core = await import('@univerjs/core');
				UserManagerService = core.UserManagerService;
				IAuthzIoService = core.IAuthzIoService;
			} catch (e) {
				console.error('[UniverSheet] Failed to import @univerjs/core:', e);
				return;
			}
			const userMgr = injector.get(UserManagerService);

			const currentUserId = props.userId ?? '';
			// Owner- prefix needed for workbook-level ops
			const ownerUID = currentUserId ? `Owner-${currentUserId}` : '';

			const resolveLocalName = () => {
				const fromAll = props.allUsers?.find((u) => u.userId === currentUserId);
				if (fromAll) return fromAll.name;
				const fromRemote = props.remoteUsers?.find((u) => u.userId === currentUserId);
				return fromRemote?.name ?? currentUserId ?? '';
			};

			const getCandidates = (): AllUser[] => {
				if (props.allUsers && props.allUsers.length > 0) return props.allUsers;
				return (props.remoteUsers ?? []).map((u) => ({
					userId: u.userId,
					name: u.name,
					avatar: '',
				}));
			};

			const registerCandidates = () => {
				getCandidates().forEach((u) => {
					if (u.userId === currentUserId) return;
					userMgr.addUser({
						userID: u.userId,
						name: u.name,
						avatar: u.avatar ?? '',
					});
				});
			};

			if (ownerUID) {
				userMgr.setCurrentUser({
					userID: ownerUID,
					name: resolveLocalName(),
					avatar: props.allUsers?.find((u) => u.userId === currentUserId)?.avatar ?? '',
				});
			}

			registerCandidates();

			const authzService = injector.get(IAuthzIoService as any);
			_authzService = authzService; // cache for synchronous use in BeforeCommandExecute
			let permSvcRef: any = null;
			let permPtClassesRef: any[] = [];
			try {
				const { IPermissionService } = await import('@univerjs/core');
				const { getAllRangePermissionPoint } = await import('@univerjs/preset-sheets-core');
				permSvcRef = injector.get(IPermissionService as unknown as object);
				permPtClassesRef =
					((getAllRangePermissionPoint as () => unknown[])?.() as any[]) ?? [];
			} catch {
				/* non-critical */
			}

			try {
				const injAny = injector as any;
				const resolvedMap = injAny.resolvedDependencyCollection?.resolvedDependencies;
				const scanMap = (map: any): boolean => {
					if (!(map instanceof Map)) return false;
					for (const [, arr] of map) {
						const items: any[] = Array.isArray(arr) ? arr : [arr];
						for (const inst of items) {
							if (
								inst &&
								typeof inst.getSubunitRuleList === 'function' &&
								typeof inst.getTargetByPermissionId === 'function'
							) {
								_ruleModel = inst;
								return true;
							}
							if (
								inst?._rangeProtectionRuleModel &&
								typeof inst._rangeProtectionRuleModel.getSubunitRuleList ===
									'function'
							) {
								_ruleModel = inst._rangeProtectionRuleModel;
								return true;
							}
						}
					}
					return false;
				};
				if (!scanMap(resolvedMap)) {
					for (const child of injAny.children ?? []) {
						if (scanMap(child?.resolvedDependencyCollection?.resolvedDependencies))
							break;
					}
				}
			} catch {
				// non-critical  will use _permissionMap fallback
			}

			if (authzService && typeof authzService === 'object') {
				const rawId = stripRolePrefix;

				const matchesUser = (
					c: { subject?: { userID?: string }; id?: string },
					uid: string
				): boolean => {
					const cUid = String(c.subject?.userID ?? c.id ?? '');
					return stripRolePrefix(cUid) === stripRolePrefix(uid);
				};

				const computeAllowed = (objectID: string, action: number): boolean => {
					const isManagementAction = action === 2;

					if (!_ruleModel) {
						try {
							const injAny2 = injector as any;
							const scanMap2 = (map: any): boolean => {
								if (!(map instanceof Map)) return false;
								for (const [, arr] of map) {
									const items: any[] = Array.isArray(arr) ? arr : [arr];
									for (const inst of items) {
										if (
											inst &&
											typeof inst.getSubunitRuleList === 'function' &&
											typeof inst.getTargetByPermissionId === 'function'
										) {
											_ruleModel = inst;
											return true;
										}
										if (
											inst?._rangeProtectionRuleModel &&
											typeof inst._rangeProtectionRuleModel
												.getSubunitRuleList === 'function'
										) {
											_ruleModel = inst._rangeProtectionRuleModel;
											return true;
										}
									}
								}
								return false;
							};
							const resolvedMap2 =
								injAny2.resolvedDependencyCollection?.resolvedDependencies;
							if (!scanMap2(resolvedMap2)) {
								for (const child of injAny2.children ?? []) {
									if (
										scanMap2(
											child?.resolvedDependencyCollection
												?.resolvedDependencies
										)
									)
										break;
								}
							}
						} catch {
							/* ignore */
						}
					}

					let ruleEditState: string | undefined;
					let ruleAllowedUsers: string[] = [];
					try {
						if (_ruleModel) {
							let target: [string, string] | null =
								(_ruleModel as any).getTargetByPermissionId?.(objectID) ?? null;

							if (!target) {
								const workbook = univerAPI?.getActiveWorkbook?.();
								if (workbook) {
									const wbId: string = workbook.getId?.() ?? '';
									outer: for (const sheet of workbook.getSheets?.() ?? []) {
										const sheetId: string = sheet.getSheetId?.();
										const sheetRules: any[] =
											(_ruleModel as any).getSubunitRuleList?.(
												wbId,
												sheetId
											) ?? [];
										if (
											sheetRules.find((r: any) => r.permissionId === objectID)
										) {
											target = [wbId, sheetId];
											break outer;
										}
									}
								}
							}

							if (target) {
								const [unitId, subUnitId] = target;
								const rules: any[] =
									(_ruleModel as any).getSubunitRuleList?.(unitId, subUnitId) ??
									[];
								const rule = rules.find((r: any) => r.permissionId === objectID);
								if (rule) {
									ruleEditState = rule.editState;
									ruleAllowedUsers = Array.isArray(rule.allowedUsers)
										? rule.allowedUsers
										: [];
								}
							}
						}
					} catch (e) {
						console.warn('[Permission] RangeProtectionRuleModel lookup failed:', e);
					}

					// Fallback to _permissionMap — only present on creator's client for new rules
					if (!ruleEditState) {
						const permMap: Map<string, any> = (authzService as any)._permissionMap;
						const permData = permMap?.get(objectID);
						if (!permData) {
							if (!_remoteProtectedPermIds.has(objectID)) {
								return true;
							}
							if (permSvcRef && permPtClassesRef.length > 0) {
								const targetSubType = action === 1 ? 1 : action === 2 ? 2 : null;
								if (targetSubType !== null) {
									for (const PtClass of permPtClassesRef) {
										try {
											const pt = new PtClass(objectID);
											if (pt.subType === targetSubType) {
												const cached = (
													permSvcRef as any
												).getPermissionPoint?.(pt.id);
												if (cached !== undefined && cached !== null) {
													const val =
														typeof cached === 'object'
															? cached.value
															: cached;
													return Boolean(val);
												}
											}
										} catch {
											/* ignore */
										}
									}
								}
							}
							return false;
						}

						const collaborators: any[] =
							permData.selectRangeObject?.collaborators ??
							permData.worksheetObject?.collaborators ??
							[];
						const scope =
							permData.selectRangeObject?.scope ?? permData.worksheetObject?.scope;
						const editScope: number = scope?.edit ?? -1;

						const isOwner = collaborators.some(
							(c: any) =>
								(c.role ?? 1) === 2 &&
								(matchesUser(c, currentUserId) || matchesUser(c, ownerUID))
						);
						const inList = collaborators.some(
							(c: any) =>
								(c.role ?? 1) !== 0 &&
								(matchesUser(c, currentUserId) || matchesUser(c, ownerUID))
						);
						if (isManagementAction) return isOwner; // management: only owner
						if (editScope === 1) return true; // AllCollaborator: everyone
						if (editScope === 2) return isOwner; // OneSelf: only creator (role=2)
						return inList;
					}

					const ONLY_ME = 'onlyMe';
					const DESIGNED = 'designedUserCanEdit';

					const permMap: Map<string, any> = (authzService as any)._permissionMap;
					const permData = permMap?.get(objectID);
					const collaborators: any[] =
						permData?.selectRangeObject?.collaborators ??
						permData?.worksheetObject?.collaborators ??
						[];
					const isCreator = collaborators.some(
						(c: any) =>
							(c.role ?? 1) === 2 &&
							(matchesUser(c, currentUserId) || matchesUser(c, ownerUID))
					);

					if (ruleEditState === ONLY_ME) {
						const descriptionCreator = (() => {
							try {
								const wb2 = univerAPI?.getActiveWorkbook?.();
								if (!wb2) return null;
								const wbId2: string = wb2.getId?.() ?? '';
								for (const sh2 of wb2.getSheets?.() ?? []) {
									const shId2: string = sh2.getSheetId?.();
									const rules2: any[] =
										(_ruleModel as any).getSubunitRuleList?.(wbId2, shId2) ??
										[];
									const r2 = rules2.find((r: any) => r.permissionId === objectID);
									if (r2) return extractCreatorTag(r2);
								}
							} catch {
								/* ignore */
							}
							return null;
						})();

						const isCreatorByDesc =
							descriptionCreator !== null &&
							stripRolePrefix(descriptionCreator) === stripRolePrefix(currentUserId);
						const isCreatorByAllowedUsers =
							ruleAllowedUsers.length > 0 &&
							ruleAllowedUsers.some(
								(u) => stripRolePrefix(u) === stripRolePrefix(currentUserId)
							);

						return isCreatorByDesc || isCreatorByAllowedUsers || isCreator;
					}

					if (ruleEditState === DESIGNED) {
						if (isManagementAction) return isCreator;
						const allowedSet = new Set<string>(ruleAllowedUsers.map(rawId));
						const isInList =
							allowedSet.has(rawId(currentUserId)) || allowedSet.has(rawId(ownerUID));
						return isCreator || isInList;
					}

					return false;
				};

				const originalCreate = authzService.create?.bind(authzService);
				authzService.create = async (config: any) => {
					const rangeObj = config.selectRangeObject ?? config.worksheetObject;
					if (rangeObj && currentUserId) {
						const existing: any[] = rangeObj.collaborators ?? [];
						const alreadyPresent = existing.some((c: any) =>
							matchesUser(c, currentUserId)
						);
						if (!alreadyPresent) {
							rangeObj.collaborators = [
								...existing,
								{
									id: ownerUID || currentUserId,
									role: 2, // UnitRole.Owner
									subject: {
										userID: ownerUID || currentUserId,
										name: userMgr.getCurrentUser()?.name ?? currentUserId,
										avatar: userMgr.getCurrentUser()?.avatar ?? '',
									},
								},
							];
						}

						const creatorTag = `__creator:${currentUserId}`;
						if (
							rangeObj.rule &&
							!String(rangeObj.rule.description ?? '').includes('__creator:')
						) {
							rangeObj.rule.description = rangeObj.rule.description
								? `${rangeObj.rule.description} ${creatorTag}`
								: creatorTag;
						}
					}
					const objectID = await originalCreate?.(config);
					if (objectID) refreshPermissionOverrides(objectID);
					return objectID;
				};

				const ourAllowed = async (config: any) => {
					const { objectID, actions } = config;
					if (!objectID || !actions?.length) return [];
					return (actions as number[]).map((action: number) => ({
						action,
						allowed: computeAllowed(objectID, action),
					}));
				};
				const ourBatchAllowed = async (configs: any[]) => {
					return configs.map((config: any) => ({
						unitID: config.unitID,
						objectID: config.objectID,
						actions: (config.actions as number[]).map((action: number) => ({
							action,
							allowed: computeAllowed(config.objectID, action),
						})),
					}));
				};

				authzService.allowed = ourAllowed;
				authzService.batchAllowed = ourBatchAllowed;

				authzService.listCollaborators = async () => {
					return getCandidates().map((u) => ({
						id: u.userId === currentUserId ? ownerUID : u.userId,
						role: u.userId === currentUserId ? 2 : 1,
						subject: {
							userID: u.userId === currentUserId ? ownerUID : u.userId,
							name: u.name,
							avatar: u.avatar || makeAvatarDataUri(u.name),
						},
					}));
				};

				const refreshPermissionPoints = async () => {
					try {
						const { IPermissionService, IUniverInstanceService } = await import(
							'@univerjs/core'
						);
						const { getAllRangePermissionPoint } = await import(
							'@univerjs/preset-sheets-core'
						);
						const permSvc = injector.get(IPermissionService as any) as any;
						const instanceSvc = injector.get(IUniverInstanceService as any) as any;

						if (!_ruleModel) {
							try {
								const injAny3 = injector as any;
								const scanLazy = (map: any): boolean => {
									if (!(map instanceof Map)) return false;
									for (const [, arr] of map) {
										const items: any[] = Array.isArray(arr) ? arr : [arr];
										for (const inst of items) {
											if (
												inst &&
												typeof inst.getSubunitRuleList === 'function' &&
												typeof inst.getTargetByPermissionId === 'function'
											) {
												_ruleModel = inst;
												return true;
											}
											if (
												inst?._rangeProtectionRuleModel &&
												typeof inst._rangeProtectionRuleModel
													.getSubunitRuleList === 'function'
											) {
												_ruleModel = inst._rangeProtectionRuleModel;
												return true;
											}
										}
									}
									return false;
								};
								const rm =
									injAny3.resolvedDependencyCollection?.resolvedDependencies;
								if (!scanLazy(rm)) {
									for (const child of injAny3.children ?? []) {
										if (
											scanLazy(
												child?.resolvedDependencyCollection
													?.resolvedDependencies
											)
										)
											break;
									}
								}
							} catch {
								/* ignore */
							}
						}

						if (!permSvc || !instanceSvc || !_ruleModel) {
							return;
						}

						const requests: any[] = [];
						const ruleIndex = new Map<string, { unitId: string; subUnitId: string }>();
						const allPointClasses: any[] =
							(getAllRangePermissionPoint as any)?.() ?? [];

						const sheets: any[] = instanceSvc.getAllUnitsForType?.(1) ?? [];
						for (const wb of sheets) {
							const unitId: string = wb.getUnitId?.();
							for (const sheet of wb.getSheets?.() ?? []) {
								const subUnitId: string = sheet.getSheetId?.();
								const rules: any[] =
									(_ruleModel as any).getSubunitRuleList?.(unitId, subUnitId) ??
									[];
								for (const rule of rules) {
									const actions = allPointClasses.map(
										(P: any) =>
											new P(unitId, subUnitId, rule.permissionId).subType
									);
									requests.push({
										objectID: rule.permissionId,
										unitID: unitId,
										objectType: 3,
										actions,
									});
									ruleIndex.set(rule.permissionId, { unitId, subUnitId });
								}
							}
						}

						if (requests.length === 0) return;

						const results = await authzService.batchAllowed(requests);
						for (const result of results) {
							const info = ruleIndex.get(result.objectID);
							if (!info) continue;
							for (const PointClass of allPointClasses) {
								const pt = new PointClass(
									info.unitId,
									info.subUnitId,
									result.objectID
								);
								const match = (result.actions as any[]).find(
									(a: any) => a.action === pt.subType
								);
								if (match !== undefined) {
									permSvc.updatePermissionPoint(pt.id, match.allowed);
								}
							}
						}
					} catch {
						// non-critical  permission defaults apply
					}
				};

				const RANGE_PROTECTION_ACTIONS = [
					0 /* View */, 1 /* Edit */, 2 /* ManageCollaborator */,
				];
				const refreshPermissionOverrides = (specificObjectId?: string) => {
					const permMap: Map<string, any> = (authzService as any)._permissionMap;
					if (!permMap) return;
					const idsToRefresh = specificObjectId
						? [specificObjectId]
						: [...permMap.keys()];
					for (const objectID of idsToRefresh) {
						for (const action of RANGE_PROTECTION_ACTIONS) {
							authzService.setPermissionOverride(
								objectID,
								action,
								computeAllowed(objectID, action)
							);
						}
					}
					refreshPermissionPoints();
				};

				_refreshPermissionOverrides = refreshPermissionOverrides;
				_refreshPermissionPoints = refreshPermissionPoints;
			}
		}
	} catch (e) {
		console.error('[UniverSheet] FATAL: Failed to inject user system:', e);
	}

	univerAPI.createWorkbook(snapshot as any);

	const doRefresh = () => {
		const fn = _refreshPermissionOverrides;
		const fn2 = _refreshPermissionPoints;
		if (fn) {
			fn();
		}
		if (fn2) {
			fn2().catch(() => {});
		}
	};
	setTimeout(doRefresh, 300);
	setTimeout(doRefresh, 1500);

	univerAPI.addEvent(univerAPI.Event.BeforeCommandExecute, (e: any) => {
		try {
			if (e?.id !== 'sheet.command.delete-range-protection') return;
			const params = e?.params as any;
			const unitId3: string = params?.unitId ?? '';
			const subUnitId3: string = params?.subUnitId ?? '';
			let ruleIds: string[] = params?.ruleIds ?? [];
			if (!ruleIds.length && params?.rule) {
				const rId = params.rule.id ?? params.rule.permissionId;
				if (rId) ruleIds = [rId];
			}
			if (!ruleIds.length || !_ruleModel) return;
			const permMap2: Map<string, any> | undefined = _authzService?._permissionMap;

			const allRules: any[] =
				(_ruleModel as any).getSubunitRuleList?.(unitId3, subUnitId3) ?? [];
			for (const ruleId of ruleIds) {
				const rule = allRules.find(
					(r: any) => r.id === ruleId || r.permissionId === ruleId
				);
				if (!rule) continue;
				const creatorId = extractCreatorTag(rule);
				if (creatorId !== null) {
					if (stripRolePrefix(creatorId) !== stripRolePrefix(props.userId ?? '')) {
						e.cancel = true;
						ElMessage.warning(
							'You cannot delete a protection rule created by another user.'
						);
						return;
					}
				} else {
					const pid = rule.permissionId ?? rule.id ?? ruleId;
					const hasInPermMap = permMap2?.has(pid);
					if (!hasInPermMap) {
						e.cancel = true;
						ElMessage.warning(
							'You cannot delete a protection rule created by another user.'
						);
						return;
					}
				}
			}
		} catch {
			/* non-critical */
		}
	});

	univerAPI.addEvent(univerAPI.Event.CommandExecuted, (payload: any) => {
		if (payload?.type !== CommandType.MUTATION) return;
		if (payload.options?.onlyLocal) return;
		if (typeof payload.id === 'string' && payload.id.startsWith('doc.mutation.')) return;

		if (
			typeof payload.id === 'string' &&
			(payload.id.includes('range-protection') || payload.id.includes('worksheet-protection'))
		) {
			if (
				payload.id === 'sheet.mutation.add-range-protection' &&
				!payload.options?.fromCollab
			) {
				const addParams = payload.params as any;
				const unitId2: string = addParams?.unitId ?? '';
				const subUnitId2: string = addParams?.subUnitId ?? '';
				const addedRules: any[] = addParams?.rules ?? [];
				for (const addedRule of addedRules) {
					if (
						addedRule?.permissionId &&
						!String(addedRule.description ?? '').includes('__creator:')
					) {
						if (_ruleModel) {
							const storedRules: any[] =
								(_ruleModel as any).getSubunitRuleList?.(unitId2, subUnitId2) ?? [];
							const stored = storedRules.find(
								(r: any) => r.permissionId === addedRule.permissionId
							);
							if (stored) {
								const creatorTag = `\u200B__creator:${props.userId ?? ''}`;
								if (!String(stored.name ?? '').includes('__creator:')) {
									stored.name = (stored.name ?? '') + creatorTag;
								}
							}
						}
					}
				}
			}

			setTimeout(() => {
				const overFn = _refreshPermissionOverrides;
				const ptsFn = _refreshPermissionPoints;
				if (overFn) {
					overFn();
				}
				if (ptsFn) {
					ptsFn().catch(() => {});
				}
			}, 50);
		}

		emit('mutation', {
			mutationId: payload.id,
			params: payload.params,
			data: JSON.stringify(payload.params ?? {}),
			timestamp: Date.now(),
			fromRemote: Boolean(payload.options?.fromCollab),
		});
	});

	registerPresenceListeners();
	syncScrollSubscription();
	watch(
		() => props.remoteUsers,
		() => {
			applyHighlights();
			scheduleTagRender();
		},
		{ deep: true, immediate: true }
	);

	window.addEventListener('resize', scheduleTagRender);
	scheduleTagRender();

	publishPresence();
	window.setTimeout(publishPresence, 600);

	emit('ready');
});

onBeforeUnmount(() => {
	disposed = true;
	window.removeEventListener('resize', scheduleTagRender);
	if (tagTimer) clearTimeout(tagTimer);
	scrollDisposable?.dispose?.();
	scrollDisposable = null;
	trackedSheetId = '';
	if (rafId) cancelAnimationFrame(rafId);
	if (editProgressTimer) clearTimeout(editProgressTimer);
	pendingEditProgress = null;
	highlights.forEach((entry) => entry.disposable?.dispose?.());
	highlights.clear();
	tags.forEach((el) => el.remove());
	tags.clear();
	disposables.forEach((d) => d?.dispose?.());
	disposables.length = 0;
	univerInstance?.univer.dispose();
	univerInstance = null;
	univerAPI = null;
});

defineExpose({
	getWorkbookData: async () => {
		if (!univerInstance) return undefined;
		try {
			const { IResourceLoaderService } = await import('@univerjs/core');
			const injector = univerInstance.univer.__getInjector?.();
			if (injector) {
				const svc = injector.get(IResourceLoaderService as any);
				const unitId = univerInstance.univerAPI.getActiveWorkbook()?.getId?.();
				if (svc && unitId) return (svc as any).saveUnit(unitId);
			}
		} catch {
			// fallback resources won't be saved but workbook data is preserved
		}
		return univerInstance.univerAPI.getActiveWorkbook()?.save();
	},
	getInstance: () => univerInstance,
	applyRemoteMutations,
});
</script>

<style lang="scss">
.univer-editor-wrapper {
	position: relative;
	width: 100%;
	overflow: hidden;
}

.univer-editor-container {
	width: 100%;
	height: 100%;
}

.univer-collab-tag-layer {
	position: absolute;
	inset: 0;
	overflow: hidden;
	pointer-events: none;
	z-index: 5;
}

.univer-collab-tag {
	position: absolute;
	top: 0;
	left: 0;
	display: none;
	max-width: 160px;
	padding: 2px 7px;
	border-radius: 3px 3px 3px 0;
	border: 1.5px solid transparent;
	color: #fff;
	font-size: 11px;
	font-weight: 500;
	line-height: 17px;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	box-shadow: 0 2px 6px rgb(0 0 0 / 30%);
	/* GPU-composited transform for smooth repositioning on scroll */
	will-change: transform;
}

[data-u-comp='ribbon-header-menu'] {
	display: none !important;
}
.sheet-permission-user-dialog {
	width: 480px !important;
	min-width: 480px !important;

	.univer-min-w-72 {
		min-width: unset !important;
		width: 100%;
	}
}
</style>
