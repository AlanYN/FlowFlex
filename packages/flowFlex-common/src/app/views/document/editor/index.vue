<template>
	<div ref="rootRef" class="flex flex-col overflow-hidden" :style="{ height: pageHeight }">
		<!-- Toolbar 56px — title left, ribbon tabs center, actions right -->
		<div
			class="flex items-center justify-between h-14 px-4 bg-white border-b border-[#e4e7ed] flex-shrink-0 gap-3"
		>
			<div class="flex items-center gap-2 flex-1 min-w-0">
				<el-button
					link
					:icon="ArrowLeft"
					size="small"
					class="mr-3 !p-1 hover:bg-white/20 rounded-xl transition-colors"
					data-testid="document-editor-btn-back"
					@click="handleBack"
				/>

				<span
					v-if="!editingTitle"
					class="text-[15px] font-bold cursor-text py-0.5 rounded hover:bg-[#f5f7fa] transition-colors truncate max-w-[400px]"
					:title="store.title"
					data-testid="document-editor-title-display"
					@click="startEditTitle"
				>
					{{ store.title || 'Untitled' }}
				</span>

				<el-input
					v-else
					ref="titleInputRef"
					v-model="titleDraft"
					class="min-w-[200px] max-w-[400px]"
					maxlength="200"
					data-testid="document-editor-title-input"
					@blur="endEditTitle"
					@keydown.enter.prevent="endEditTitle"
					@keydown.escape.prevent="cancelEditTitle"
				/>
				<!-- Avatars + presence details -->
				<el-popover
					v-if="store.collaborators.length > 0"
					placement="bottom"
					:width="300"
					trigger="hover"
				>
					<template #reference>
						<div class="flex items-center cursor-default">
							<el-tooltip
								v-for="c in displayCollaborators"
								:key="c.userId"
								:content="c.name"
								placement="bottom"
								:show-after="300"
							>
								<div
									class="collab-avatar-wrapper"
									:class="{
										'collab-avatar-editing': store.presence[c.userId]?.editing,
									}"
									:style="{ '--collab-color': c.color }"
								>
									<el-avatar
										:size="28"
										:style="{
											background: c.color,
											border: store.presence[c.userId]?.editing
												? `2.5px solid ${c.color}`
												: '2px solid #fff',
											marginLeft: '-6px',
										}"
										class="!text-white !text-[11px] font-semibold cursor-default"
										data-testid="document-editor-collaborator-avatar"
									>
										{{ c.name?.[0]?.toUpperCase() }}
									</el-avatar>
								</div>
							</el-tooltip>
							<el-avatar
								v-if="extraCollaboratorCount > 0"
								:size="28"
								class="!bg-[#909399] !text-white !text-[11px] font-semibold"
								style="border: 2px solid #fff; margin-left: -6px"
							>
								+{{ extraCollaboratorCount }}
							</el-avatar>
						</div>
					</template>

					<div class="space-y-2">
						<p class="text-xs font-semibold text-[#909399]">Online collaborators</p>
						<div
							v-for="c in store.collaborators"
							:key="c.userId"
							class="flex items-center gap-2 text-xs"
							:data-testid="`document-editor-presence-${c.userId}`"
						>
							<span
								class="w-2 h-2 rounded-full flex-shrink-0"
								:style="{ background: c.color }"
							></span>
							<span class="font-medium truncate max-w-[90px]">{{ c.name }}</span>
							<span class="text-[#909399] truncate">
								{{ describePresence(c.userId) }}
							</span>
						</div>
					</div>
				</el-popover>

				<!-- Collaboration status -->
				<el-tag
					v-if="store.sessionStatus === 'offline'"
					type="warning"
					size="small"
					effect="light"
					data-testid="document-editor-collab-offline"
				>
					Collaboration offline
				</el-tag>
				<el-tag
					v-else-if="store.collaborators.length > 0"
					size="small"
					effect="plain"
					data-testid="document-editor-collab-online"
				>
					{{ store.collaborators.length }} online
				</el-tag>
			</div>

			<div class="flex items-center gap-3 flex-shrink-0">
				<!-- Ribbon tab buttons — mirrors Univer's ribbon-header-menu tabs
				     but renders in our header bar. Clicks are forwarded to the hidden
				     Univer original buttons so React's event system is not bypassed. -->
				<div v-if="editorReady && univerRibbonTabs.length" class="flex items-center gap-1">
					<button
						v-for="tab in univerRibbonTabs"
						:key="tab.title"
						type="button"
						:class="[
							'px-3 py-1.5 text-sm font-medium rounded transition-colors',
							tab.selected
								? 'bg-primary-50 text-primary-600 shadow-sm'
								: 'text-gray-600 hover:bg-gray-100',
						]"
						@click="clickUniversTab(tab.title)"
					>
						{{ tab.title }}
					</button>
				</div>

				<!-- Cell permission status indicator -->
				<el-tooltip
					v-if="cellPermissionStatus !== 'open'"
					:content="
						cellPermissionStatus === 'owner'
							? 'You have protected this cell — only you can edit it'
							: 'This cell is protected — you cannot edit it'
					"
					placement="bottom"
					:show-after="200"
				>
					<div
						class="flex items-center gap-1.5 px-2 py-1 rounded text-xs font-medium select-none"
						:class="
							cellPermissionStatus === 'owner'
								? 'bg-[#ecf5ff] text-[#409eff] border border-[#b3d8ff]'
								: 'bg-[#fef0f0] text-[#f56c6c] border border-[#fbc4c4]'
						"
						data-testid="document-editor-cell-permission-badge"
					>
						<svg
							v-if="cellPermissionStatus === 'owner'"
							width="12"
							height="12"
							viewBox="0 0 24 24"
							fill="none"
							stroke="currentColor"
							stroke-width="2.5"
							stroke-linecap="round"
							stroke-linejoin="round"
						>
							<rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
							<path d="M7 11V7a5 5 0 0 1 9.9-1" />
						</svg>
						<svg
							v-else
							width="12"
							height="12"
							viewBox="0 0 24 24"
							fill="none"
							stroke="currentColor"
							stroke-width="2.5"
							stroke-linecap="round"
							stroke-linejoin="round"
						>
							<rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
							<path d="M7 11V7a5 5 0 0 1 10 0v4" />
						</svg>
						<span>
							{{
								cellPermissionStatus === 'owner' ? 'You own this lock' : 'Read-only'
							}}
						</span>
					</div>
				</el-tooltip>

				<el-button
					type="primary"
					:loading="store.saving"
					class="page-header-btn page-header-btn-primary"
					data-testid="document-editor-btn-save"
					@click="handleSave"
				>
					{{ saveLabel }}
				</el-button>
			</div>
		</div>

		<!-- Editor area: :key forces full remount on each navigation -->
		<div class="flex-1 min-h-0 overflow-hidden relative" v-loading="store.loading">
			<UniverSheet
				v-if="store.ready && store.docType === 'Sheet'"
				:key="`sheet-${editorKey}`"
				ref="editorRef"
				:workbook-data="store.snapshotData ?? undefined"
				:doc-id="docId"
				height="100%"
				:user-id="currentUserId"
				:remote-users="remoteUsers"
				:all-users="allSystemUsers"
				@mutation="onMutation"
				@presence="onLocalPresence"
				@ready="onEditorReady"
				@permission-status="onPermissionStatus"
			/>
			<UniverDoc
				v-else-if="store.ready && store.docType === 'Doc'"
				:key="`doc-${editorKey}`"
				ref="editorRef"
				:document-data="store.snapshotData ?? undefined"
				:doc-id="docId"
				height="100%"
				:user-id="currentUserId"
				:remote-users="remoteUsers"
				@mutation="onMutation"
				@presence="onLocalPresence"
				@ready="onEditorReady"
			/>
		</div>

		<!-- Document history panel — FAB + dialog, self-contained -->
		<DocumentHistoryPanel v-if="store.unitId" :unit-id="store.unitId" />
	</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, nextTick, defineAsyncComponent } from 'vue';
import { useRoute, useRouter, onBeforeRouteLeave } from 'vue-router';
import {
	ElButton,
	ElAvatar,
	ElTooltip,
	ElPopover,
	ElTag,
	ElMessageBox,
	ElMessage,
} from 'element-plus';
import { ArrowLeft } from '@element-plus/icons-vue';
import { useI18n } from '@/hooks/useI18n';
import { useUserStore } from '@/stores/modules/user';
import { useDocumentEditorStore } from '@/stores/modules/documentEditor';
import { useCollaboration, type CollaborationSession } from '@/composables/useCollaboration';
import { getAllUser } from '@/apis/global';
import DocumentHistoryPanel from './DocumentHistoryPanel.vue';

// Sheet is the only supported editor type. UniverDoc and UniverSlide share
// the same @univerjs/ui service identifiers as UniverSheet and cannot coexist
// in the same bundle without identifier conflicts in redi's IoC container.
const UniverSheet = defineAsyncComponent(() => import('@/components/UniverEditor/UniverSheet.vue'));
const UniverDoc = defineAsyncComponent(() => import('@/components/UniverEditor/UniverDoc.vue'));

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const store = useDocumentEditorStore();
const userStore = useUserStore();

const rootRef = ref<HTMLDivElement>();
const editorRef = ref<any>();
const titleInputRef = ref<HTMLInputElement>();

// ── Ribbon tab mirroring ───────────────────────────────────────────
// We render our own tab buttons in the header and forward clicks to Univer's
// hidden originals so React's synthetic event system stays intact.
interface RibbonTab {
	title: string;
	selected: boolean;
}
const univerRibbonTabs = ref<RibbonTab[]>([]);

function syncRibbonTabs() {
	const orig = document.querySelector('[data-u-comp="ribbon-header-menu"]');
	if (!orig) return;
	const buttons = Array.from(orig.querySelectorAll<HTMLButtonElement>('[role="tab"]'));
	univerRibbonTabs.value = buttons.map((b) => ({
		title: b.getAttribute('title') ?? b.textContent?.trim() ?? '',
		selected: b.getAttribute('aria-selected') === 'true',
	}));
}

function clickUniversTab(title: string) {
	const orig = document.querySelector('[data-u-comp="ribbon-header-menu"]');
	if (!orig) return;
	const btn = Array.from(orig.querySelectorAll<HTMLButtonElement>('[role="tab"]')).find(
		(b) => (b.getAttribute('title') ?? b.textContent?.trim()) === title
	);
	btn?.click();
	// Sync selected state after click
	setTimeout(syncRibbonTabs, 50);
}

let tabSyncTimer: ReturnType<typeof setInterval> | null = null;
const editingTitle = ref(false);
const titleDraft = ref('');
let lastValidTitle = '';

// Unique key per navigation — forces full Vue remount of the editor component
// so Univer initializes into a fresh DOM container every time.
const editorKey = ref<string | number>('');

const saveLabel = ref('Save');
let saveLabelTimer: ReturnType<typeof setTimeout> | null = null;

/**
 * Reflects the protection status of the currently selected cell.
 * - 'open'    → no protection
 * - 'owner'   → protected and the current user owns the rule (can edit + manage)
 * - 'blocked' → protected and the current user is locked out
 * Resets to 'open' whenever the editor remounts (editorKey changes).
 */
const cellPermissionStatus = ref<'open' | 'owner' | 'blocked'>('open');

function onPermissionStatus(status: 'open' | 'owner' | 'blocked') {
	cellPermissionStatus.value = status;
}

const MAX_AVATARS = 5;
const displayCollaborators = computed(() => store.collaborators.slice(0, MAX_AVATARS));
const extraCollaboratorCount = computed(() =>
	Math.max(0, store.collaborators.length - MAX_AVATARS)
);

/** Collaborators merged with the position/content they are currently on */
const remoteUsers = computed(() =>
	store.collaborators.map((c) => ({ ...c, ...(store.presence[c.userId] ?? {}) }))
);

/**
 * All system users fetched once on page load — fed into UniverSheet so
 * the protection "Add user" dialog shows the full user roster.
 */
const allSystemUsers = ref<Array<{ userId: string; name: string; avatar?: string }>>([]);

async function fetchAllSystemUsers() {
	try {
		const res = await getAllUser();
		// getAllUser returns UserTreeNodeDto[] (flat list after tree extraction).
		// Only nodes of type 'user' carry a meaningful userId + name.
		const raw: any[] = Array.isArray(res) ? res : (res as any)?.data ?? [];
		allSystemUsers.value = raw
			.filter((u) => u.type === 'user' && u.id)
			.map((u) => ({ userId: String(u.id), name: u.username ?? u.name ?? u.id }));
	} catch (e) {
		// Non-critical — protection dialog will fall back to online-collaborator list
		console.warn('[editor] Failed to fetch all system users:', e);
	}
}

// Backend document id - every editor derives its stable Univer ids from it
const docId = computed(() => String(route.params.unitId ?? ''));
const currentUserId = computed(() => String((userStore.getUserInfo as any)?.userId ?? ''));
const currentUserName = computed(() =>
	String(
		(userStore.getUserInfo as any)?.userName ??
			(userStore.getUserInfo as any)?.realName ??
			'Someone'
	)
);

// ── Layout: the editor must fill exactly the space left by the app shell ──
const pageHeight = ref('calc(100vh - 136px)');

function measurePageHeight() {
	const el = rootRef.value;
	if (!el) return;
	const top = el.getBoundingClientRect().top;
	pageHeight.value = `${Math.max(320, Math.round(window.innerHeight - top))}px`;
}

let shellObserver: ResizeObserver | null = null;

/**
 * The app shell (navbar + scroll-container padding) decides how much room is
 * left, and it also resizes when the sidebar collapses — observe it so the
 * page never relies on a hard-coded offset.
 */
function observeShell() {
	const wrap = rootRef.value?.closest('.el-scrollbar__wrap') as HTMLElement | null;
	if (!wrap || typeof ResizeObserver === 'undefined') return;
	shellObserver?.disconnect();
	shellObserver = new ResizeObserver(() => measurePageHeight());
	shellObserver.observe(wrap);
}

function resetOuterScroll() {
	const wrap = rootRef.value?.closest('.el-scrollbar__wrap') as HTMLElement | null;
	if (wrap) wrap.scrollTop = 0;
}

let session: CollaborationSession | null = null;

onMounted(async () => {
	const unitId = route.params.unitId as string;
	const docType = (route.query.docType as string) || 'Sheet';

	store.resetState();
	store.docType = docType as any;
	editorKey.value = Date.now();
	editorReady = false;
	queuedRemoteMutations = [];

	// The page scroller keeps its position between routes — reset it, then size
	// the page to the visible area so no outer scrollbar can appear.
	resetOuterScroll();
	measurePageHeight();
	window.addEventListener('resize', measurePageHeight);
	observeShell();
	window.addEventListener('keydown', onKeydown, true);

	await store.loadDocument(unitId);
	await nextTick();
	resetOuterScroll();
	measurePageHeight();

	// Fetch all system users for Univer's protection "Add user" dialog.
	// Run in parallel — it should not block the document from loading.
	void fetchAllSystemUsers();

	startCollaboration(unitId);
});

onBeforeUnmount(() => {
	if (tabSyncTimer) {
		clearInterval(tabSyncTimer);
		tabSyncTimer = null;
	}
	window.removeEventListener('resize', measurePageHeight);
	shellObserver?.disconnect();
	shellObserver = null;
	window.removeEventListener('keydown', onKeydown, true);
	session?.disconnect();
	session = null;
	queuedRemoteMutations = [];
	// Last chance to persist the audit trail - `resetState` drops the buffer.
	void store.flushPendingLogs();
	store.resetState();
});

onBeforeRouteLeave(async (_to, _from, next) => {
	if (!store.hasUnsaved) return next();

	try {
		await ElMessageBox.confirm(
			'You have unsaved changes. Would you like to save before leaving?',
			'Unsaved Changes',
			{
				distinguishCancelAndClose: true,
				confirmButtonText: 'Save & Leave',
				cancelButtonText: 'Leave Without Saving',
				type: 'warning',
			}
		);
		await handleSave();
		next();
	} catch (action) {
		if (action === 'cancel') {
			next();
		} else {
			next(false);
		}
	}
});

// ── Collaboration ──────────────────────────────────────────────────

function startCollaboration(unitId: string) {
	if (!unitId) return;
	session = useCollaboration({
		unitId,
		user: { id: currentUserId.value || 'anonymous', name: currentUserName.value },
		token: userStore.getToken,
		onInit: (members) => {
			store.setCollaborators(
				members.map((m) => ({ userId: m.userId, name: m.name, color: m.color }))
			);
			members.forEach((m) => {
				if (m.presence) store.setPresence(m.userId, m.presence);
			});
		},
		onJoin: (member) => {
			store.upsertCollaborator({
				userId: member.userId,
				name: member.name,
				color: member.color,
			});
			if (member.presence) store.setPresence(member.userId, member.presence);
		},
		onLeave: (userId) => store.removeCollaborator(userId),
		onPresence: (userId, presence) => store.setPresence(userId, presence),
		onMutations: applyRemoteChanges,
		onStatusChange: (status) => {
			store.setSessionStatus(status);
			// The editor may publish its cursor before the socket is up — re-announce
			// it as soon as the room has been joined so peers see us immediately.
			if (status === 'online' && store.localPresence) {
				session?.sendPresence(store.localPresence);
			}
		},
	});
	session.connect();
}

/** Editor reported a local cursor / edit state → broadcast it */
function onLocalPresence(presence: any) {
	store.setLocalPresence(presence);
	session?.sendPresence(presence);
}

/** Editor reported a mutation — log it and (for local edits) broadcast it */
function onMutation(info: {
	mutationId: string;
	params: unknown;
	data: string;
	timestamp: number;
	fromRemote: boolean;
}) {
	if (info.fromRemote) return; // remote changes are never echoed back
	store.logMutation({
		mutationId: info.mutationId,
		params: info.params,
		timestamp: info.timestamp,
		fromRemote: info.fromRemote,
	});
	session?.queueMutation({ id: info.mutationId, data: info.data });
}

function describePresence(userId: string): string {
	const presence = store.presence[userId];
	if (!presence) return 'Viewing the document';
	if (presence.a1) {
		const content = presence.text ? `: ${presence.text}` : '';
		return presence.editing
			? `Editing ${presence.a1}${content}`
			: `At ${presence.a1}${content}`;
	}
	return presence.editing ? 'Editing' : 'Viewing the document';
}

// ── Actions ────────────────────────────────────────────────────────

function onKeydown(event: KeyboardEvent) {
	if ((event.ctrlKey || event.metaKey) && event.key?.toLowerCase() === 's') {
		event.preventDefault();
		void handleSave();
	}
}

/**
 * The editor is lazy-loaded, so a changeset can arrive before it is mounted.
 * Dropping it would silently fork the two peers' copies of the document, so
 * they are queued here and applied as soon as the editor reports itself ready.
 */
let editorReady = false;
let queuedRemoteMutations: Array<{ id: string; data: string }> = [];

function applyRemoteChanges(mutations: Array<{ id: string; data: string }>) {
	if (mutations.length === 0) return;
	if (!editorReady || !editorRef.value?.applyRemoteMutations) {
		queuedRemoteMutations.push(...mutations);
		return;
	}
	void editorRef.value.applyRemoteMutations(mutations);
}

function onEditorReady() {
	editorReady = true;
	// Start syncing ribbon tabs into our header bar
	syncRibbonTabs();
	tabSyncTimer = setInterval(syncRibbonTabs, 500);

	// Register the snapshot provider so the store can auto-save after each mutation
	store.setSnapshotProvider(async () => {
		if (!editorRef.value) return null;
		if (store.docType === 'Sheet') return (await editorRef.value.getWorkbookData?.()) ?? null;
		if (store.docType === 'Doc') return editorRef.value.getDocumentData?.() ?? null;
		return null;
	});

	if (queuedRemoteMutations.length === 0) return;
	const queued = queuedRemoteMutations;
	queuedRemoteMutations = [];
	void editorRef.value?.applyRemoteMutations?.(queued);
}

async function handleSave() {
	if (!editorRef.value) return;
	let data: object | undefined;
	if (store.docType === 'Sheet') data = await editorRef.value.getWorkbookData?.();
	else if (store.docType === 'Doc') data = editorRef.value.getDocumentData?.();
	if (!data) return;
	await store.saveDocument(data);
	saveLabel.value = '✓ Saved';
	if (saveLabelTimer) clearTimeout(saveLabelTimer);
	saveLabelTimer = setTimeout(() => {
		saveLabel.value = 'Save';
	}, 1200);
}

function startEditTitle() {
	lastValidTitle = store.title;
	titleDraft.value = store.title;
	editingTitle.value = true;
	nextTick(() => titleInputRef.value?.select());
}

async function endEditTitle() {
	editingTitle.value = false;
	const newTitle = titleDraft.value.trim();
	if (!newTitle) {
		store.title = lastValidTitle;
		return;
	}
	if (newTitle === store.title) return;
	const previous = store.title;
	store.title = newTitle;
	try {
		// Only the title is sent — the backend keeps the stored document content
		await store.renameDocument(newTitle);
	} catch {
		store.title = previous;
		ElMessage.error(t('sys.api.operationFailed'));
	}
}

function cancelEditTitle() {
	editingTitle.value = false;
	store.title = lastValidTitle;
}

function handleBack() {
	router.push({ name: 'DocumentList' });
}
</script>

<style lang="scss" scoped>
// Status dot at bottom-right of avatar — green when online, colored+bouncing when typing
.collab-avatar-wrapper {
	position: relative;
	display: inline-flex;

	&::after {
		content: '';
		position: absolute;
		bottom: 1px;
		right: 1px;
		width: 8px;
		height: 8px;
		border-radius: 50%;
		border: 1.5px solid #fff;
		background: #52c41a;
		pointer-events: none;
		transition: background 0.2s;
	}

	&.collab-avatar-editing::after {
		background: var(--collab-color, #52c41a);
		animation: collab-dot-bounce 0.5s ease-in-out infinite alternate;
	}
}

@keyframes collab-dot-bounce {
	from {
		transform: scale(1);
	}
	to {
		transform: scale(1.8);
	}
}
</style>
