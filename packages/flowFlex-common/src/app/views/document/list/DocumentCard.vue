<template>
	<el-card
		class="document-card overflow-hidden transition-all cursor-pointer hover:shadow-lg"
		:data-testid="`document-list-card-${unitId}`"
		@click="emit('click')"
	>
		<!-- Card header — same pattern as ChecklistCard / QuestionnaireCard -->
		<template #header>
			<div class="card-header -m-5 p-4">
				<div class="flex items-center justify-between w-full">
					<!-- Icon + title -->
					<div class="flex items-center space-x-3 flex-1 min-w-0">
						<div
							class="card-icon rounded-full flex-shrink-0 flex items-center justify-center"
						>
							<span class="text-lg">{{ typeIcon }}</span>
						</div>
						<h3
							class="card-title text-base font-semibold leading-tight tracking-tight truncate"
							:title="title"
						>
							{{ title }}
						</h3>
					</div>

					<!-- More menu — trigger="click" matches project standard, @click.stop prevents card click -->
					<el-dropdown
						trigger="click"
						:data-testid="`document-list-btn-more-${unitId}`"
						class="flex-shrink-0"
						@click.stop
						@command="handleCommand"
					>
						<el-button text class="card-more-btn" link @click.stop>
							<el-icon class="h-4 w-4"><MoreFilled /></el-icon>
						</el-button>
						<template #dropdown>
							<el-dropdown-menu>
								<el-dropdown-item :icon="Edit" command="rename">
									Rename
								</el-dropdown-item>
								<el-divider class="my-0" />
								<el-dropdown-item
									:icon="Delete"
									command="delete"
									class="text-red-500 hover:!bg-red-500 hover:!text-white"
								>
									Delete
								</el-dropdown-item>
							</el-dropdown-menu>
						</template>
					</el-dropdown>
				</div>

				<!-- Type tag below title -->
				<div class="mt-2">
					<el-tag :type="typeTagVariant" size="small" effect="light">
						{{ typeLabel }}
					</el-tag>
				</div>
			</div>
		</template>

		<!-- Card body: meta information -->
		<div class="space-y-2">
			<div class="flex items-center justify-between text-sm">
				<el-tooltip class="flex-1" content="Last modified by">
					<div class="flex flex-1 items-center gap-2">
						<Icon icon="ic:baseline-person-3" class="text-primary-500 w-4 h-4" />
						<span class="font-medium truncate max-w-[120px]">
							{{ createBy || '—' }}
						</span>
					</div>
				</el-tooltip>
				<el-tooltip class="flex-1" content="Last modified">
					<div class="flex flex-1 items-center gap-2">
						<Icon icon="ic:baseline-calendar-month" class="text-primary-500 w-4 h-4" />
						<span class="font-medium">{{ relativeTime }}</span>
					</div>
				</el-tooltip>
			</div>
		</div>
	</el-card>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import {
	ElCard,
	ElTag,
	ElDropdown,
	ElDropdownMenu,
	ElDropdownItem,
	ElButton,
	ElIcon,
	ElTooltip,
	ElDivider,
	ElMessageBox,
} from 'element-plus';
import { MoreFilled, Edit, Delete } from '@element-plus/icons-vue';
import { Icon } from '@iconify/vue';

interface Props {
	id: string;
	unitId: string;
	title: string;
	docType: 'Sheet' | 'Doc' | 'Slide';
	modifyDate: string;
	createBy: string;
}

const props = defineProps<Props>();
const emit = defineEmits<{
	click: [];
	rename: [newTitle: string];
	delete: [];
}>();

// ── Type display ──────────────────────────────────────────────────
const TYPE_META: Record<
	string,
	{ icon: string; label: string; tagVariant: 'success' | 'warning' | 'info' | 'danger' }
> = {
	Sheet: { icon: '📊', label: 'Spreadsheet', tagVariant: 'success' },
	Doc: { icon: '📄', label: 'Document', tagVariant: 'info' },
	Slide: { icon: '📑', label: 'Presentation', tagVariant: 'warning' },
};

const typeMeta = computed(() => TYPE_META[props.docType] ?? TYPE_META.Doc);
const typeIcon = computed(() => typeMeta.value.icon);
const typeLabel = computed(() => typeMeta.value.label);
const typeTagVariant = computed(() => typeMeta.value.tagVariant);

// ── Relative time ─────────────────────────────────────────────────
const relativeTime = computed(() => {
	if (!props.modifyDate) return '—';
	const diff = Date.now() - new Date(props.modifyDate).getTime();
	const m = Math.floor(diff / 60_000);
	if (m < 1) return 'Just now';
	if (m < 60) return `${m}m ago`;
	const h = Math.floor(m / 60);
	if (h < 24) return `${h}h ago`;
	const d = Math.floor(h / 24);
	if (d < 7) return `${d}d ago`;
	return new Date(props.modifyDate).toLocaleDateString('en-US', {
		month: 'short',
		day: 'numeric',
	});
});

// ── Actions ───────────────────────────────────────────────────────
function handleCommand(command: string) {
	if (command === 'delete') {
		ElMessageBox.confirm(
			'This action cannot be undone. Are you sure you want to delete this document?',
			'Delete Document',
			{
				confirmButtonText: 'Delete',
				cancelButtonText: 'Cancel',
				type: 'warning',
				confirmButtonClass: 'el-button--danger',
			}
		).then(() => emit('delete'));
	} else if (command === 'rename') {
		ElMessageBox.prompt('Enter a new name for this document', 'Rename', {
			confirmButtonText: 'Save',
			cancelButtonText: 'Cancel',
			inputValue: props.title,
			inputValidator: (v) => (v?.trim() ? true : 'Name cannot be empty'),
		}).then(({ value }) => emit('rename', value.trim()));
	}
}
</script>

<style scoped lang="scss">
/* Align with the project-wide card grid: auto-fill, same breakpoints as
   checklist-grid / questionnaire-grid / workflow-grid */
</style>
