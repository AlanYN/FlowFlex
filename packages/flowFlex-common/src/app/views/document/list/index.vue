<template>
	<div class="document-list-container">
		<!-- Page header — matches Questionnaire / Checklist pattern -->
		<PageHeader
			title="Documents"
			description="Create and manage documents, spreadsheets, and presentations"
		>
			<template #actions>
				<el-button
					type="primary"
					:icon="Plus"
					class="page-header-btn page-header-btn-primary"
					data-testid="document-list-btn-create"
					@click="dialogVisible = true"
				>
					New Document
				</el-button>
			</template>
		</PageHeader>

		<!-- Search & filter card — matches el-card mb-6 / grid pattern -->
		<el-card class="mb-6">
			<div class="grid grid-cols-1 md:grid-cols-3 gap-4">
				<!-- Search -->
				<div class="space-y-2">
					<label class="text-sm font-medium">Search</label>
					<el-input
						v-model="searchKeyword"
						placeholder="Enter document name and press enter"
						clearable
						data-testid="document-list-input-search"
						@input="onSearchInput"
						@clear="onSearchClear"
					/>
				</div>

				<!-- Type -->
				<div class="space-y-2">
					<label class="text-sm font-medium">Type</label>
					<el-select
						v-model="store.docType"
						placeholder="All types"
						class="w-full"
						clearable
						data-testid="document-list-select-type"
						@change="onTypeChange"
					>
						<el-option label="Spreadsheet" value="Sheet" />
						<el-option label="Document" value="Doc" />
						<el-option label="Presentation" value="Slide" />
					</el-select>
				</div>

				<!-- Sort -->
				<div class="space-y-2">
					<label class="text-sm font-medium">Sort by</label>
					<el-select
						v-model="store.sortBy"
						class="w-full"
						data-testid="document-list-select-sort"
						@change="onSortChange"
					>
						<el-option label="Last modified" value="modify_date" />
						<el-option label="Created date" value="create_date" />
						<el-option label="Name" value="title" />
					</el-select>
				</div>
			</div>
		</el-card>

		<!-- Card grid with loading / empty states -->
		<div v-loading="store.loading && store.list.length === 0" class="min-h-[200px]">
			<!-- Empty state -->
			<div
				v-if="!store.loading && store.list.length === 0"
				class="flex flex-col items-center justify-center py-20 gap-4"
				data-testid="document-list-empty-state"
			>
				<el-icon :size="64" color="#c0c4cc">
					<Document />
				</el-icon>
				<p class="text-base font-semibold text-[#606266]">No documents yet</p>
				<p class="text-sm text-[#c0c4cc]">
					Click "New Document" to create your first document
				</p>
				<el-button
					type="primary"
					:icon="Plus"
					class="page-header-btn page-header-btn-primary"
					data-testid="document-list-empty-btn-create"
					@click="dialogVisible = true"
				>
					New Document
				</el-button>
			</div>

			<!-- Document card grid — same grid pattern as checklist-grid / questionnaire-grid -->
			<div v-else class="document-grid" data-testid="document-list-card-grid">
				<DocumentCard
					v-for="doc in store.list"
					:key="doc.unitId"
					v-bind="doc"
					@click="openEditor(doc.unitId, doc.docType)"
					@rename="(title) => handleRename(doc, title)"
					@delete="store.deleteDocument(doc.id)"
				/>
			</div>
		</div>

		<!-- Pagination -->
		<CustomerPagination
			:total="store.total"
			:limit="store.pageSize"
			:page="store.page"
			:background="true"
			:hidden="store.loading"
			@update:page="store.setPage"
			@update:limit="onPageSizeChange"
		/>

		<!-- Create document dialog -->
		<CreateDocumentDialog v-model="dialogVisible" @created="onDocumentCreated" />
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { ElButton, ElInput, ElSelect, ElOption, ElCard, ElIcon, ElMessage } from 'element-plus';
import { Plus, Document } from '@element-plus/icons-vue';
import { useI18n } from '@/hooks/useI18n';
import { useDocumentListStore } from '@/stores/modules/documentList';
import { saveSnapshot } from '@/apis/ow/documentSnapshot';
import DocumentCard from './DocumentCard.vue';
import CreateDocumentDialog from './CreateDocumentDialog.vue';
import PageHeader from '@/components/global/PageHeader/index.vue';
import CustomerPagination from '@/components/global/u-pagination/index.vue';

const { t } = useI18n();
const router = useRouter();
const store = useDocumentListStore();
const dialogVisible = ref(false);
const searchKeyword = ref('');

let searchTimer: ReturnType<typeof setTimeout> | null = null;

onMounted(() => {
	store.loadList();
});

// ── Search ────────────────────────────────────────────────────────
function onSearchInput() {
	if (searchTimer) clearTimeout(searchTimer);
	searchTimer = setTimeout(() => {
		store.setKeyword(searchKeyword.value);
	}, 300);
}

function onSearchClear() {
	store.setKeyword('');
}

// ── Filters ───────────────────────────────────────────────────────
function onTypeChange() {
	store.setDocType(store.docType);
}

function onSortChange() {
	store.setSortBy(store.sortBy);
}

function onPageSizeChange(size: number) {
	store.pageSize = size;
	store.setPage(1);
}

// ── Navigation ────────────────────────────────────────────────────
function openEditor(unitId: string, docType: string) {
	router.push({
		name: 'DocumentEditor',
		params: { unitId },
		query: { docType },
	});
}

function onDocumentCreated(unitId: string) {
	router.push({ name: 'DocumentEditor', params: { unitId } });
}

// ── Rename ────────────────────────────────────────────────────────
async function handleRename(doc: any, newTitle: string) {
	try {
		// Only the title is sent — passing dataJson here would overwrite the document
		await saveSnapshot(doc.unitId, { title: newTitle });
		ElMessage.success(t('sys.api.operationSuccess'));
		store.loadList();
	} catch {
		ElMessage.error(t('sys.api.operationFailed'));
	}
}
</script>

<style scoped lang="scss">
.document-list-container {
	height: 100%;
	display: flex;
	flex-direction: column;
}

/* Matches checklist-grid / questionnaire-grid — auto-fill responsive grid */
.document-grid {
	display: grid;
	gap: 24px;
	grid-template-columns: repeat(auto-fill, minmax(360px, 1fr));
	width: 100%;

	@media (max-width: 480px) {
		grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
		gap: 16px;
	}
	@media (min-width: 481px) and (max-width: 768px) {
		grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
		gap: 20px;
	}
	@media (min-width: 769px) and (max-width: 1024px) {
		grid-template-columns: repeat(auto-fill, minmax(360px, 1fr));
		gap: 20px;
	}
	@media (min-width: 1025px) and (max-width: 1400px) {
		grid-template-columns: repeat(auto-fill, minmax(360px, 1fr));
		gap: 24px;
	}
	@media (min-width: 1401px) and (max-width: 1920px) {
		grid-template-columns: repeat(auto-fill, minmax(380px, 1fr));
		gap: 28px;
	}
	@media (min-width: 1921px) {
		grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
		gap: 32px;
	}

	& > :deep(.document-card) {
		max-width: 600px;
		width: 100%;
	}
}
</style>
