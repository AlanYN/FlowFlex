<template>
	<el-dialog
		:model-value="visible"
		:before-close="handleClose"
		:width="bigDialogWidth"
		:close-on-click-modal="true"
		:close-on-press-escape="true"
		append-to-body
		draggable
	>
		<template #header>
			<div>
				<h2 class="text-xl font-semibold text-gray-900 dark:text-white mb-2">
					Import Attachments from Integration Systems
				</h2>
				<p class="text-sm text-gray-600 dark:text-gray-400">
					Select attachments from your connected integration systems to import into
				</p>
			</div>
		</template>

		<div class="min-h-[200px]">
			<!-- Table with attachments -->
			<el-table
				:data="attachments"
				stripe
				:border="true"
				:max-height="tableMaxHeight"
				@row-click="handleRowClick"
				v-loading="loading"
			>
				<el-table-column width="55" align="center">
					<template #default="{ row }">
						<el-checkbox
							:model-value="selectedAttachments.has(row.id)"
							@change="toggleSelection(row.id)"
							@click.stop
						/>
					</template>
				</el-table-column>

				<el-table-column label="File Name" min-width="250">
					<template #default="{ row }">
						<div
							class="flex items-center"
							:class="{ 'opacity-100': selectedAttachments.has(row.id) }"
						>
							<el-icon class="text-primary text-lg mr-2 flex-shrink-0">
								<Document />
							</el-icon>
							<span
								class="font-medium overflow-hidden text-ellipsis whitespace-nowrap"
								:class="
									selectedAttachments.has(row.id)
										? 'text-primary'
										: 'text-gray-900 dark:text-white'
								"
							>
								{{ row.fileName }}
							</span>
						</div>
					</template>
				</el-table-column>

				<el-table-column label="Source" min-width="150">
					<template #default="{ row }">
						<el-tag type="primary">{{ row.integrationName }}</el-tag>
					</template>
				</el-table-column>

				<el-table-column label="Module" min-width="150">
					<template #default="{ row }">
						<span class="text-sm text-gray-700 dark:text-gray-300">
							{{ row.moduleName }}
						</span>
					</template>
				</el-table-column>

				<!-- Empty state slot -->
				<template #empty>
					<div class="flex flex-col items-center justify-center py-8">
						<el-icon class="text-6xl text-gray-400 dark:text-gray-600 mb-4">
							<Folder />
						</el-icon>
						<p class="text-gray-600 dark:text-gray-400 text-base font-medium mb-1">
							No attachments found
						</p>
						<p class="text-gray-500 dark:text-gray-500 text-sm">
							There are no attachments available from your connected integration
							systems
						</p>
					</div>
				</template>
			</el-table>
		</div>

		<template #footer>
			<div class="flex items-center justify-between">
				<span class="text-sm text-gray-600 dark:text-gray-400">
					{{ selectedCount }} file(s) selected
				</span>
				<div class="space-x-2">
					<el-button @click="handleClose">Cancel</el-button>
					<el-button type="primary" :disabled="selectedCount === 0" @click="handleSave">
						Import
					</el-button>
				</div>
			</div>
		</template>
	</el-dialog>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { Document, Folder } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import { IntegrationAttachment } from '#/integration';
import { bigDialogWidth, tableMaxHeight } from '@/settings/projectSetting';

// Props interface
interface ImportAttachmentsDialogProps {
	visible: boolean;
	attachments: IntegrationAttachment[];
	loading?: boolean;
}

// Emits interface
interface ImportAttachmentsDialogEmits {
	(e: 'update:visible', value: boolean): void;
	(e: 'close'): void;
	(e: 'select', selected: IntegrationAttachment[]): void;
	(e: 'startDownload', attachments: IntegrationAttachment[]): void;
}

// Define props
const props = withDefaults(defineProps<ImportAttachmentsDialogProps>(), {
	visible: false,
	attachments: () => [],
	loading: false,
});

// Define emits
const emit = defineEmits<ImportAttachmentsDialogEmits>();

// Internal state for selection tracking
const selectedAttachments = ref<Set<string>>(new Set());

// Computed property for selected count
const selectedCount = computed(() => selectedAttachments.value.size);

// Computed property to get selected attachment objects
const getSelectedAttachments = computed(() => {
	return props.attachments.filter((att) => selectedAttachments.value.has(att.id));
});

// Toggle selection for a specific attachment
const toggleSelection = (attachmentId: string) => {
	if (selectedAttachments.value.has(attachmentId)) {
		selectedAttachments.value.delete(attachmentId);
	} else {
		selectedAttachments.value.add(attachmentId);
	}
	// Trigger reactivity
	selectedAttachments.value = new Set(selectedAttachments.value);

	// Emit select event with current selection
	emit('select', getSelectedAttachments.value);
};

// Handle row click to toggle checkbox
const handleRowClick = (row: IntegrationAttachment) => {
	toggleSelection(row.id);
};

// Handle save button click - emit event to start download
const handleSave = () => {
	const selected = getSelectedAttachments.value;
	if (selected.length === 0) {
		ElMessage.warning('Please select at least one file');
		return;
	}

	// Emit event to parent to start download process
	emit('startDownload', selected);

	// Close dialog immediately - download happens in background
	selectedAttachments.value.clear();
	handleClose();
};

// Handle dialog close
const handleClose = () => {
	emit('update:visible', false);
	emit('close');
};
</script>

<style scoped lang="scss">
/* Minimal custom styles only when necessary */
</style>
