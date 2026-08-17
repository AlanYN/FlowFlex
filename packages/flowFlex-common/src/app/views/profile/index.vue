<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Delete } from '@element-plus/icons-vue';
import PageHeader from '@/components/global/PageHeader/index.vue';
import AddSignatureDialog from './components/AddSignatureDialog.vue';
import { getSignatures, deleteSignature } from '@/apis/ow/profile';
import type { SignatureItem } from '@/apis/ow/profile';

// ========================= Constants =========================

const MAX_SIGNATURES = 7;

// ========================= State =========================

const signatures = ref<SignatureItem[]>([]);
const loading = ref(false);
const deletingId = ref<string | null>(null);
const addDialogVisible = ref(false);

// ========================= Computed =========================

const isAtLimit = computed(() => signatures.value.length >= MAX_SIGNATURES);

// ========================= Data Loading =========================

/**
 * Fetches signature list from API and updates the reactive state.
 * Validates: Requirements 2.1, 2.3
 */
async function loadSignatures() {
	loading.value = true;
	try {
		const res: any = await getSignatures();
		signatures.value = Array.isArray(res) ? res : res?.data ?? [];
	} catch (error: unknown) {
		const msg =
			error instanceof Error ? error.message : 'Failed to load signatures. Please try again.';
		ElMessage.error(msg);
	} finally {
		loading.value = false;
	}
}

// ========================= Delete =========================

/**
 * Prompts confirmation, then soft-deletes the signature and refreshes list.
 * Validates: Requirements 5.1, 5.2
 */
async function handleDelete(sig: SignatureItem) {
	try {
		await ElMessageBox.confirm(
			'Are you sure you want to delete this signature? This cannot be undone.',
			'Delete Signature',
			{
				confirmButtonText: 'Delete',
				cancelButtonText: 'Cancel',
				type: 'warning',
				confirmButtonClass: 'el-button--danger',
			}
		);
	} catch {
		// User cancelled
		return;
	}

	deletingId.value = sig.id;
	try {
		await deleteSignature(sig.id);
		// Optimistic local removal for immediate feedback (Req 5.2)
		signatures.value = signatures.value.filter((s) => s.id !== sig.id);
	} catch (error: unknown) {
		const msg =
			error instanceof Error
				? error.message
				: 'Failed to delete signature. Please try again.';
		ElMessage.error(msg);
		// Re-fetch to ensure list is in sync if optimistic removal was incorrect
		await loadSignatures();
	} finally {
		deletingId.value = null;
	}
}

// ========================= Add Signature Dialog =========================

/**
 * Opens the add-signature dialog.
 * Button is disabled when at limit so this guard is a safety net.
 * Validates: Requirements 4.1, 4.2, 4.3
 */
function handleOpenAdd() {
	if (isAtLimit.value) return;
	addDialogVisible.value = true;
}

/**
 * Called when AddSignatureDialog emits 'saved'.
 * Refreshes the list to show the new signature.
 * Validates: Requirements 2.1, 2.3
 */
async function handleSaved() {
	addDialogVisible.value = false;
	await loadSignatures();
}

// ========================= Lifecycle =========================

onMounted(() => {
	loadSignatures();
});
</script>

<template>
	<div class="profile-page">
		<!-- Page header -->
		<PageHeader title="My Profile" description="Manage your signatures for document signing">
			<template #actions>
				<!-- Add Signature button — disabled at limit, wrapped in tooltip when at limit.
                     Validates: Requirements 4.2, 4.3 -->
				<el-tooltip
					:content="
						isAtLimit ? 'Signature limit reached (7 max). Delete one to add more.' : ''
					"
					:disabled="!isAtLimit"
					placement="bottom"
				>
					<!-- Wrapped in span so tooltip still triggers on a disabled button -->
					<span>
						<el-button
							type="primary"
							:disabled="isAtLimit"
							class="page-header-btn page-header-btn-primary"
							@click="handleOpenAdd"
						>
							Add Signature
						</el-button>
					</span>
				</el-tooltip>
			</template>
		</PageHeader>

		<!-- Content area -->
		<div class="profile-content">
			<!-- Loading skeleton -->
			<template v-if="loading">
				<div class="signature-grid">
					<el-skeleton v-for="i in 3" :key="i" animated class="signature-skeleton">
						<template #template>
							<el-skeleton-item variant="image" style="width: 100%; height: 100px" />
						</template>
					</el-skeleton>
				</div>
			</template>

			<!-- Empty state — shown when no signatures.
                 Validates: Requirements 2.2 -->
			<template v-else-if="signatures.length === 0">
				<div class="empty-state">
					<el-empty description="No saved signatures yet">
						<el-tooltip
							content="Signature limit reached (7 max). Delete one to add more."
							:disabled="!isAtLimit"
							placement="bottom"
						>
							<span>
								<el-button
									type="primary"
									:disabled="isAtLimit"
									@click="handleOpenAdd"
								>
									Add Signature
								</el-button>
							</span>
						</el-tooltip>
					</el-empty>
				</div>
			</template>

			<!-- Signature card grid.
                 Validates: Requirements 2.1, 2.3 -->
			<template v-else>
				<div class="section-header">
					<span class="section-title">My Signatures</span>
					<span class="section-count">
						{{ signatures.length }} / {{ MAX_SIGNATURES }}
					</span>
				</div>

				<div class="signature-grid">
					<div v-for="sig in signatures" :key="sig.id" class="signature-card">
						<!-- Signature preview image.
                             Validates: Requirement 2.3 -->
						<div class="signature-preview">
							<img
								:src="sig.imageBase64"
								alt="Signature preview"
								class="signature-img"
							/>
						</div>

						<!-- Card footer: date + delete button -->
						<div class="signature-footer">
							<span class="signature-date">
								{{ new Date(sig.createdDate).toLocaleDateString() }}
							</span>

							<!-- Delete button.
                                 Validates: Requirements 5.1, 5.2 -->
							<el-button
								type="danger"
								size="small"
								link
								:loading="deletingId === sig.id"
								:disabled="deletingId !== null && deletingId !== sig.id"
								class="delete-btn"
								@click="handleDelete(sig)"
							>
								<el-icon v-if="deletingId !== sig.id"><Delete /></el-icon>
							</el-button>
						</div>
					</div>
				</div>
			</template>
		</div>

		<!-- Add Signature Dialog.
             Validates: Requirements 3.1–3.7 -->
		<AddSignatureDialog v-model:visible="addDialogVisible" @saved="handleSaved" />
	</div>
</template>

<style scoped lang="scss">
.profile-page {
	@apply p-0;
}

.profile-content {
	@apply px-0;
}

/* Section header above the grid */
.section-header {
	@apply flex items-center justify-between mb-3 px-1;
}

.section-title {
	@apply text-base font-semibold;
	color: var(--el-text-color-primary);
}

.section-count {
	@apply text-sm;
	color: var(--el-text-color-secondary);
}

/* Signature card grid */
.signature-grid {
	@apply grid gap-4;
	grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
}

.signature-skeleton {
	@apply rounded-xl p-4;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	min-height: 140px;
}

/* Individual signature card */
.signature-card {
	@apply rounded-xl overflow-hidden flex flex-col;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	box-shadow: 0 1px 3px rgba(0, 0, 0, 0.06);
	transition: box-shadow 0.2s ease;
}

.signature-card:hover {
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

/* Preview area — simulates PDF background */
.signature-preview {
	@apply flex items-center justify-center p-4 flex-1;
	background-color: #f8f8f5;
	min-height: 100px;
}

.signature-img {
	@apply max-h-24 max-w-full object-contain;
}

/* Card footer */
.signature-footer {
	@apply flex items-center justify-between px-3 py-2;
	border-top: 1px solid var(--el-border-color-lighter);
}

.signature-date {
	@apply text-xs;
	color: var(--el-text-color-secondary);
}

.delete-btn {
	padding: 4px !important;
}

.delete-btn .el-icon {
	font-size: 15px;
}

/* Empty state */
.empty-state {
	@apply flex items-center justify-center py-20;
}

/* Dark mode — flat selectors required (no nesting outside component scope) */
html.dark .signature-preview {
	background-color: #2a2a2a;
}

html.dark .signature-card {
	box-shadow: 0 1px 3px rgba(0, 0, 0, 0.3);
}

html.dark .signature-card:hover {
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.4);
}
</style>
