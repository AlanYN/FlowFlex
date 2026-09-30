import { defineStore } from 'pinia';
import { markRaw } from 'vue';
import { ElMessage } from 'element-plus';
import { getSnapshotByUnitId, saveSnapshot } from '@/apis/ow/documentSnapshot';
import { addOperationLog } from '@/apis/ow/documentOperationLog';
import { useUserStore } from '@/stores/modules/user';

export interface CollaboratorInfo {
	userId: string;
	name: string;
	color: string;
}

/** What a collaborator is currently doing inside the document */
export interface PresenceState {
	/** Active worksheet id (Sheets only) */
	sheetId?: string;
	/** A1 notation of the current selection, e.g. "B3" or "B3:D5" */
	a1?: string;
	/** Short preview of the selected / edited content */
	text?: string;
	/** True while the user is typing inside a cell */
	editing?: boolean;
	updatedAt: number;
}

export interface MutationInfo {
	mutationId: string;
	params: unknown;
	timestamp: number;
	/** True when the mutation was produced by a remote collaborator */
	fromRemote?: boolean;
}

export type SessionStatus = 'idle' | 'connecting' | 'online' | 'offline';

interface DocumentEditorState {
	unitId: string | null;
	docType: 'Sheet' | 'Doc' | 'Slide';
	/** ⚠️ Not reactive on purpose — never wrap in ref/reactive */
	snapshotData: object | null;
	/** True once the snapshot has been loaded (successfully or not) */
	ready: boolean;
	title: string;
	loading: boolean;
	saving: boolean;
	hasUnsaved: boolean;
	collaborators: CollaboratorInfo[];
	/** Remote presence keyed by userId */
	presence: Record<string, PresenceState>;
	localPresence: PresenceState | null;
	sessionStatus: SessionStatus;
	currentRevision: number;
	error: string | null;
}

/**
 * Pending operation logs, keyed by `mutationId + target`.
 *
 * Every keystroke is a mutation, so one record per mutation turned a single cell
 * edit into hundreds of records - and the flush could never drain the queue, which
 * is why `/ow/document-operation-logs/v1` was called non-stop. The key collapses a
 * whole burst into one record that always holds the latest state.
 */
const logBuffer = new Map<string, Record<string, unknown>>();
let logTimer: ReturnType<typeof setTimeout> | null = null;

/** Idle time after the last edit before the buffered logs are sent */
const LOG_FLUSH_DELAY = 1500;
/** Max records per request - keeps the body small */
const LOG_BATCH_SIZE = 20;

/**
 * Auto-save: fires after the user stops editing for AUTO_SAVE_DELAY ms.
 * Callback is registered by the editor component via `setSnapshotProvider`.
 */
let autoSaveTimer: ReturnType<typeof setTimeout> | null = null;
const AUTO_SAVE_DELAY = 2000;
/** Injected by the editor — returns the current workbook/doc snapshot */
let snapshotProvider: (() => Promise<object | null> | object | null) | null = null;

/**
 * Where a mutation applies, e.g. `sheetId/r7:c4`. Cell coordinates are read from
 * the serialised `cellValue` matrix (`{ row: { col: cell } }`), so typing in one
 * cell collapses into a single log while a different cell starts a new one.
 */
function mutationTarget(params: any): string {
	if (!params || typeof params !== 'object') return '';
	const parts: string[] = [];
	if (params.subUnitId) parts.push(String(params.subUnitId));
	const cellValue = params.cellValue;
	if (cellValue && typeof cellValue === 'object') {
		const rowKeys = Object.keys(cellValue).slice(0, 1);
		const cols = rowKeys.length > 0 ? cellValue[rowKeys[0]] : null;
		if (cols && typeof cols === 'object') {
			const colKeys = Object.keys(cols).slice(0, 1);
			if (colKeys.length > 0) parts.push(`${rowKeys[0]}:${colKeys[0]}`);
		}
	} else if (params.range) {
		parts.push(JSON.stringify(params.range));
	} else if (params.ranges) {
		parts.push(JSON.stringify(params.ranges).slice(0, 120));
	}
	return parts.join('/');
}

/** Identity of the signed-in user — stamped on every operation log */
function currentOperator() {
	const userInfo: any = useUserStore().getUserInfo ?? {};
	return {
		userId: String(userInfo.userId ?? userInfo.id ?? ''),
		userName: String(userInfo.userName ?? userInfo.realName ?? ''),
	};
}

export const useDocumentEditorStore = defineStore({
	id: 'item-wfe-document-editor',

	state: (): DocumentEditorState => ({
		unitId: null,
		docType: 'Sheet',
		snapshotData: null,
		ready: false,
		title: '',
		loading: false,
		saving: false,
		hasUnsaved: false,
		collaborators: [],
		presence: {},
		localPresence: null,
		sessionStatus: 'idle',
		currentRevision: 0,
		error: null,
	}),

	getters: {
		onlineCount(state): number {
			return state.collaborators.length;
		},
		/** Collaborators that are currently editing something (for the toolbar hint) */
		activeEditors(state): Array<CollaboratorInfo & PresenceState> {
			return state.collaborators
				.map((c) => ({ ...c, ...(state.presence[c.userId] ?? { updatedAt: 0 }) }))
				.filter((c) => Boolean(c.a1));
		},
	},

	actions: {
		async loadDocument(unitId: string) {
			this.unitId = unitId;
			this.loading = true;
			this.ready = false;
			this.error = null;
			try {
				// transformResponseHook returns res.data (the full SuccessResponse body):
				// { code: "200", data: DocumentSnapshotOutputDto, msg, success }
				const res = (await getSnapshotByUnitId(unitId)) as {
					code: string;
					success: boolean;
					msg?: string;
					data: {
						id: string;
						unitId: string;
						title?: string;
						docType?: 'Sheet' | 'Doc' | 'Slide';
						/** Raw JSON string from backend — parse before passing to Univer */
						dataJson?: string;
						revision?: number;
					};
				};
				if (res?.data) {
					this.title = res.data.title ?? '';
					this.docType = res.data.docType ?? 'Sheet';
					this.currentRevision = res.data.revision ?? 0;
					// ⚠️ markRaw — must NOT be a reactive proxy when handed to Univer.
					//
					// Why double-parse?
					//
					// The frontend sends:
					//   dataJson = JSON.stringify(workbookData)  →  a JSON string
					//
					// The backend stores it as a C# string in a JSONB column. When ASP.NET
					// Core serialises the OutputDto for the HTTP response, it treats
					// DataJson (a C# string) as a JSON string value — wrapping it in quotes
					// and escaping every inner quote. So what arrives over the wire is:
					//
					//   "dataJson": "{\"id\":\"...\",\"sheets\":{...}}"
					//                ↑ the whole workbook JSON is now a quoted string
					//
					// axios parses the HTTP response body → res.data.dataJson is still a
					// *string* (the original JSON.stringify() output the frontend sent).
					// One JSON.parse() gets you that string; a second one gets the object.
					//
					// The guard below handles both the double-encoded case and any future
					// fix where the backend returns the value already decoded.
					const raw = res.data.dataJson as unknown;
					let parsed: object = {};
					try {
						if (raw && typeof raw === 'object') {
							// Backend now returns dataJson as an inline JSON object
							parsed = raw as object;
						} else if (typeof raw === 'string') {
							// Legacy: double-encoded string — parse twice
							const once = JSON.parse(raw);
							parsed = typeof once === 'string' ? JSON.parse(once) : once;
						}
					} catch {
						parsed = {};
					}
					this.snapshotData = markRaw(parsed);
				} else {
					this.error = res?.msg ?? 'Failed to load document';
				}
			} finally {
				this.loading = false;
				this.ready = true;
			}
		},

		async saveDocument(data: object, silent = false) {
			if (!this.unitId) return;
			this.saving = true;
			try {
				await saveSnapshot(this.unitId, {
					title: this.title,
					docType: this.docType,
					dataJson: JSON.stringify(data),
				});
				this.hasUnsaved = false;
				if (!silent) ElMessage.success('Saved');
			} finally {
				this.saving = false;
			}
		},

		/** Rename without touching the stored document content [DS-03] */
		async renameDocument(title: string) {
			if (!this.unitId) return;
			// dataJson is intentionally omitted — sending "{}" would erase the document
			await saveSnapshot(this.unitId, { title });
		},

		/**
		 * Register the callback that returns the current document snapshot.
		 * Called by the editor component on `@ready`, cleared on unmount.
		 */
		setSnapshotProvider(fn: (() => Promise<object | null> | object | null) | null) {
			snapshotProvider = fn;
		},

		/**
		 * Fire-and-forget operation log. Mutations are buffered and flushed in a
		 * short window so a burst of edits does not turn into a burst of HTTP calls.
		 */
		logMutation(info: MutationInfo) {
			if (!this.unitId) return;
			// Remote collaborators' mutations belong to them, not to this user
			if (info.fromRemote) return;
			this.hasUnsaved = true;

			const operator = currentOperator();
			const key = `${info.mutationId}|${mutationTarget(info.params)}`;
			const existing = logBuffer.get(key);
			logBuffer.set(key, {
				unitId: this.unitId,
				userId: operator.userId,
				userName: operator.userName,
				// Keep the start of the burst, so the log shows when it began
				timestamp: existing?.timestamp ?? info.timestamp,
				mutationId: info.mutationId,
				paramsJson: JSON.stringify(info.params ?? null),
				revision: this.currentRevision,
			});

			this.scheduleLogFlush();
			this.scheduleAutoSave();
		},

		/** Debounced flush - the timer restarts while the user keeps editing */
		scheduleLogFlush() {
			if (logTimer) clearTimeout(logTimer);
			logTimer = setTimeout(() => {
				logTimer = null;
				void this.flushOperationLogs();
			}, LOG_FLUSH_DELAY);
		},

		/**
		 * Debounced auto-save — fires 2 s after the last mutation.
		 * Uses snapshotProvider() to get the latest data from the editor without
		 * requiring the user to click Save.
		 */
		scheduleAutoSave() {
			if (autoSaveTimer) clearTimeout(autoSaveTimer);
			autoSaveTimer = setTimeout(async () => {
				autoSaveTimer = null;
				if (!snapshotProvider) return;
				const data = await snapshotProvider();
				if (!data) return;
				void this.saveDocument(data, true);
			}, AUTO_SAVE_DELAY);
		},

		async flushOperationLogs() {
			if (logTimer) {
				clearTimeout(logTimer);
				logTimer = null;
			}
			if (logBuffer.size === 0) return;

			const entries = Array.from(logBuffer.entries());
			logBuffer.clear();
			const batch = entries.slice(0, LOG_BATCH_SIZE);
			// Anything above the batch size waits for the next round
			entries.slice(LOG_BATCH_SIZE).forEach(([key, entry]) => logBuffer.set(key, entry));

			// One request per record, but fired together instead of one after another.
			// Operation logs are audit-only, so failures are ignored (BR-04).
			await Promise.allSettled(batch.map(([, entry]) => addOperationLog(entry as any)));

			if (logBuffer.size > 0) this.scheduleLogFlush();
		},

		/** Flush what is still buffered (leaving the page / unmount) */
		flushPendingLogs() {
			if (logBuffer.size === 0) {
				if (logTimer) {
					clearTimeout(logTimer);
					logTimer = null;
				}
				return;
			}
			return this.flushOperationLogs();
		},

		/** Replace the roster with the authoritative member list from the server */
		setCollaborators(list: CollaboratorInfo[]) {
			this.collaborators = list;
			const alive = new Set(list.map((c) => c.userId));
			Object.keys(this.presence).forEach((userId) => {
				if (!alive.has(userId)) delete this.presence[userId];
			});
		},

		upsertCollaborator(info: CollaboratorInfo) {
			const idx = this.collaborators.findIndex((c) => c.userId === info.userId);
			if (idx >= 0) {
				this.collaborators[idx] = { ...this.collaborators[idx], ...info };
			} else {
				this.collaborators.push(info);
			}
		},

		removeCollaborator(userId: string) {
			this.collaborators = this.collaborators.filter((c) => c.userId !== userId);
			delete this.presence[userId];
		},

		setPresence(userId: string, payload: Partial<PresenceState>) {
			this.presence[userId] = {
				...this.presence[userId],
				...payload,
				updatedAt: Date.now(),
			};
		},

		setLocalPresence(payload: Partial<PresenceState>) {
			this.localPresence = {
				...this.localPresence,
				...payload,
				updatedAt: Date.now(),
			} as PresenceState;
		},

		setSessionStatus(status: SessionStatus) {
			this.sessionStatus = status;
		},

		resetState() {
			this.unitId = null;
			this.snapshotData = null;
			this.ready = false;
			this.title = '';
			this.loading = false;
			this.saving = false;
			this.hasUnsaved = false;
			this.collaborators = [];
			this.presence = {};
			this.localPresence = null;
			this.sessionStatus = 'idle';
			this.currentRevision = 0;
			this.error = null;
			logBuffer.clear();
			if (logTimer) {
				clearTimeout(logTimer);
				logTimer = null;
			}
			if (autoSaveTimer) {
				clearTimeout(autoSaveTimer);
				autoSaveTimer = null;
			}
			snapshotProvider = null;
		},
	},
});
