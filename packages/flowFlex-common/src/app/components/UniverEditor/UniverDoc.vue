<template>
	<div ref="wrapperRef" class="univer-editor-wrapper" :style="{ height }">
		<div ref="containerRef" class="univer-editor-container"></div>
		<!-- Remote collaborator typing indicators (shown at top of editor) -->
		<div v-if="activeRemoteUsers.length" class="univer-doc-collab-bar">
			<div
				v-for="user in activeRemoteUsers"
				:key="user.userId"
				class="univer-doc-collab-tag"
				:style="{ background: user.color, borderColor: user.color }"
			>
				{{ user.name }}
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, toRaw } from 'vue';
import { createUniver, LocaleType, UniverInstanceType, CommandType } from '@univerjs/presets';
import { UniverDocsCorePreset } from '@univerjs/preset-docs-core';
import '@univerjs/preset-docs-core/lib/index.css';
import docsEnUS from '@univerjs/preset-docs-core/locales/en-US';
import type { PresenceState, CollaboratorInfo } from '@/stores/modules/documentEditor';

import { normalizeUnitSnapshot } from './univerIds';

/** A remote collaborator with their current presence state */
type DocRemoteUser = CollaboratorInfo & Partial<PresenceState>;

interface Props {
	documentData?: object;
	/** Backend document id - seeds the deterministic Univer id (see univerIds.ts) */
	docId?: string;
	height?: string;
	readonly?: boolean;
	userId?: string;
	/** Remote collaborators to visualise */
	remoteUsers?: DocRemoteUser[];
}

const props = withDefaults(defineProps<Props>(), {
	height: '100%',
	readonly: false,
	remoteUsers: () => [],
});

// Users who are actively typing — shown as collab tags at the top of the editor
const activeRemoteUsers = computed(() => (props.remoteUsers ?? []).filter((u) => u.editing));

const emit = defineEmits<{
	mutation: [
		info: {
			mutationId: string;
			params: unknown;
			/** Params serialised when the mutation happened - see useCollaboration.ts */
			data: string;
			timestamp: number;
			fromRemote: boolean;
		},
	];
	presence: [presence: PresenceState];
	ready: [];
}>();

const wrapperRef = ref<HTMLDivElement>();
const containerRef = ref<HTMLDivElement>();

let univerInstance: ReturnType<typeof createUniver> | null = null;
let typingTimer: ReturnType<typeof setTimeout> | null = null;
// Guard: set in onBeforeUnmount so any in-flight work aborts immediately
let destroyed = false;
// Store the unitId of the created document so getDocumentData can retrieve
// it by ID rather than relying on getActiveDocument() which may return null
// if the document loses focus (e.g. when clicking Save).
let docUnitId: string | null = null;

/** Apply changesets broadcast by other collaborators */
async function applyRemoteMutations(mutations: Array<{ id: string; data: string }>) {
	if (!univerInstance) return;
	const { univerAPI } = univerInstance;
	// Rewrite unitId to match the locally created document.
	// The sender may have a different unitId (opened before deterministic IDs were set),
	// so we normalise it here — the same as localizeParams() does in UniverSheet.
	const localUnitId = univerAPI.getActiveDocument?.()?.getUnitId?.() ?? '';
	for (const mutation of mutations) {
		try {
			const rawParams = JSON.parse(mutation.data) as Record<string, unknown>;
			const params =
				localUnitId &&
				typeof rawParams.unitId === 'string' &&
				rawParams.unitId !== localUnitId
					? { ...rawParams, unitId: localUnitId }
					: rawParams;
			await univerAPI.executeCommand(mutation.id, params, { fromCollab: true });
		} catch (error) {
			console.warn('[collab] failed to apply remote mutation', mutation.id, error);
		}
	}
}

function publishPresence(editing: boolean) {
	// Docs have no public API to map a text range to screen coordinates, so we
	// publish an "is typing" flag — the toolbar shows it next to the avatar.
	emit('presence', { editing, a1: '', text: '', updatedAt: Date.now() });
}

onMounted(() => {
	if (destroyed || !containerRef.value) return;

	try {
		univerInstance = createUniver({
			locale: LocaleType.EN_US,
			locales: { [LocaleType.EN_US]: docsEnUS },
			presets: [
				// Only the core preset — Drawing/HyperLink/ThreadComment presets carry
				// docs-drawing-ui which has an implicit dependency on sheets-ui services
				// (HoverManagerService2, transformer) that are not registered in a pure
				// Doc context, causing "transformer is not init" crashes on load.
				UniverDocsCorePreset({ container: containerRef.value }),
			],
		});

		if (destroyed) {
			univerInstance.univer.dispose();
			univerInstance = null;
			return;
		}

		const { univerAPI, univer } = univerInstance;

		// ⚠️ toRaw — the snapshot must not be a reactive proxy when handed to Univer
		// Ensure the snapshot has a valid body — an empty `{}` causes the render
		// engine to crash with "Cannot read properties of undefined (reading
		// 'getDataModel')" because DocSkeleton expects at minimum dataStream + paragraphs.
		const rawData = toRaw(props.documentData as any) ?? {};
		const normalized = normalizeUnitSnapshot(rawData, props.docId);
		if (!normalized.body || !normalized.body.dataStream) {
			normalized.body = {
				dataStream: '\r\n',
				paragraphs: [{ startIndex: 0 }],
				sectionBreaks: [{ startIndex: 1 }],
				textRuns: [],
				customRanges: [],
				customDecorations: [],
			};
		} else if (!normalized.body.textRuns) {
			// Ensure textRuns exists so JSONX text-x operations can be applied.
			// If textRuns is absent, RichTextEditingMutation's RETAIN operations
			// have nothing to update and bold/italic/etc. silently fail.
			normalized.body.textRuns = [];
		}
		// DocumentFlavor.TRADITIONAL = 1 renders the document as a paginated page with
		// margins and a page border, which is the standard "paper" view.
		// Without pageSize, the page dimensions default to Infinity × Infinity making
		// the white page rect invisible (it fills the entire canvas). A4 = 794×1124.
		if (!normalized.documentStyle) {
			normalized.documentStyle = {
				documentFlavor: 1,
				pageSize: { width: 794, height: 1124 },
				marginTop: 75,
				marginBottom: 75,
				marginLeft: 75,
				marginRight: 75,
			};
		} else {
			// Always use TRADITIONAL (1) mode — ensures paper view with visible page rect.
			// MODERN (2) renders infinite-scroll with no page boundary.
			normalized.documentStyle.documentFlavor = 1;
			if (!normalized.documentStyle.pageSize) {
				normalized.documentStyle.pageSize = { width: 794, height: 1124 };
			}
			if (!normalized.documentStyle.marginTop) {
				normalized.documentStyle.marginTop = 75;
				normalized.documentStyle.marginBottom = 75;
				normalized.documentStyle.marginLeft = 75;
				normalized.documentStyle.marginRight = 75;
			}
		}
		univer.createUnit(UniverInstanceType.UNIVER_DOC, normalized as any);
		docUnitId = (normalized.id as string) ?? null;

		// ── Fix Univer 0.25.x: getSelfOrHeaderFooterModel("") returns undefined ──
		// When user selects text, the selection's segmentId is "" (empty string).
		// DocumentDataModel.getSelfOrHeaderFooterModel("") treats "" as non-null,
		// looks in header/footer map, finds nothing, returns undefined, causing all
		// inline format commands (Bold, Italic, etc.) to silently fail.
		// Fix: patch the document model instance to treat "" the same as null/undefined.
		try {
			// Get doc model via univerAPI — avoids IoC identifier resolution issues
			const docModel = (univerAPI as any).getActiveDocument?.()?._documentDataModel;
			if (docModel && typeof docModel.getSelfOrHeaderFooterModel === 'function') {
				const orig = docModel.getSelfOrHeaderFooterModel.bind(docModel);
				docModel.getSelfOrHeaderFooterModel = (segmentId?: string) =>
					orig(segmentId === '' ? undefined : segmentId);
			}
		} catch {
			// non-critical
		}

		univerAPI.addEvent(univerAPI.Event.CommandExecuted, (payload: any) => {
			if (payload?.type !== CommandType.MUTATION) return;
			if (payload.options?.onlyLocal) return;
			const fromRemote = Boolean(payload.options?.fromCollab);
			emit('mutation', {
				mutationId: payload.id,
				params: payload.params,
				data: JSON.stringify(payload.params ?? {}),
				timestamp: Date.now(),
				fromRemote,
			});
			if (fromRemote) return;
			publishPresence(true);
			if (typingTimer) clearTimeout(typingTimer);
			typingTimer = setTimeout(() => publishPresence(false), 4000);
		});

		publishPresence(false);
		emit('ready');
	} catch (e) {
		console.error('[UniverDoc] Failed to initialize Doc editor:', e);
	}
});

onBeforeUnmount(() => {
	destroyed = true;
	docUnitId = null;
	if (typingTimer) clearTimeout(typingTimer);
	univerInstance?.univer.dispose();
	univerInstance = null;
});

defineExpose({
	getDocumentData: () => {
		if (!univerInstance) return undefined;
		// Use the stored unitId to retrieve the doc by ID — getActiveDocument()
		// returns null when the doc loses focus (e.g. when the Save button is clicked).
		const doc = docUnitId
			? (univerInstance.univerAPI as any).getUniverDoc?.(docUnitId) ??
			  univerInstance.univerAPI.getActiveDocument?.()
			: univerInstance.univerAPI.getActiveDocument?.();
		// FDocument.save() is not available in all Univer versions.
		// Use getSnapshot() which reliably returns the full document data.
		const data = (doc as any)?.getSnapshot?.() ?? doc?.save?.();
		return data;
	},
	getInstance: () => univerInstance,
	applyRemoteMutations,
	publishPresence: () => publishPresence(false),
});
</script>

<style lang="scss">
.univer-editor-wrapper {
	position: relative;
	width: 100%;
	height: 100%;
	overflow: hidden;
}

.univer-editor-container {
	width: 100%;
	height: 100%;
}

// ── Ribbon tab row styling ────────────────────────────────────────
// Hide the original ribbon-header-menu — our header bar renders its own
// mirrored tab buttons that forward clicks to the hidden originals.
[data-u-comp='ribbon-header-menu'] {
	display: none !important;
}

// ── Document collaborator typing indicators ────────────────────────
// Shown at the top of the editor when remote users are actively typing.
.univer-doc-collab-bar {
	position: absolute;
	top: 6px;
	right: 10px;
	display: flex;
	gap: 4px;
	z-index: 10;
	pointer-events: none;
}

.univer-doc-collab-tag {
	padding: 2px 8px;
	border-radius: 3px 3px 3px 0;
	border: 1.5px solid transparent;
	color: #fff;
	font-size: 11px;
	font-weight: 500;
	line-height: 17px;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	max-width: 120px;
	box-shadow: 0 2px 6px rgb(0 0 0 / 25%);
}
</style>
