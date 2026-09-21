<template>
	<div v-if="!loading && totalCount > 0" class="wfe-global-block-bg">
		<!-- Header — same pattern as OnboardingProgress / InternalNotes -->
		<div
			class="case-notes-header rounded-xl"
			:class="{ expanded: isOpen }"
			@click="isOpen = !isOpen"
		>
			<div class="flex items-center justify-between">
				<div class="flex items-center">
					<el-icon
						class="case-component-expand-icon text-lg mr-2"
						:class="{ rotated: isOpen }"
					>
						<ArrowRight />
					</el-icon>
					<h3 class="case-component-title">Related Cases</h3>
				</div>
				<span v-if="totalCount > 0" class="case-component-subtitle">
					{{ totalCount }} related
				</span>
			</div>
		</div>

		<!-- Collapsible body -->
		<el-collapse-transition>
			<div v-show="isOpen" class="p-4 space-y-4">
				<!-- Loading -->
				<div v-if="loading" class="text-center py-6">
					<el-icon class="text-2xl animate-spin text-[var(--el-color-primary)]">
						<Loading />
					</el-icon>
				</div>

				<template v-else>
					<!-- Upstream -->
					<div v-if="upstream.length > 0">
						<div
							class="flex items-center gap-1 text-xs font-semibold text-[var(--el-text-color-secondary)] mb-2"
						>
							<el-icon class="text-[var(--el-color-warning)]"><Top /></el-icon>
							Upstream Cases
							<span class="font-normal text-[var(--el-text-color-placeholder)]">
								(triggered this)
							</span>
						</div>
						<div class="space-y-1.5">
							<div
								v-for="item in upstream"
								:key="item.logId"
								class="flex items-center justify-between px-3 py-2 rounded-lg bg-[var(--el-fill-color-lighter)] hover:bg-[var(--el-fill-color-light)] cursor-pointer transition-colors"
								@click="goToCase(item.onboardingId)"
							>
								<div class="flex items-center gap-2 min-w-0">
									<el-tag type="warning" effect="plain" class="shrink-0">
										{{ item.caseCode || '—' }}
									</el-tag>
									<span
										class="text-sm text-[var(--el-text-color-primary)] truncate"
									>
										{{ item.caseName || item.onboardingId }}
									</span>
								</div>
								<span
									class="text-xs text-[var(--el-text-color-placeholder)] shrink-0 ml-2"
								>
									{{ formatDate(item.createDate) }}
								</span>
							</div>
						</div>
					</div>

					<!-- Downstream -->
					<div v-if="downstream.length > 0">
						<div
							class="flex items-center gap-1 text-xs font-semibold text-[var(--el-text-color-secondary)] mb-2"
						>
							<el-icon class="text-[var(--el-color-success)]"><Bottom /></el-icon>
							Downstream Cases
							<span class="font-normal text-[var(--el-text-color-placeholder)]">
								(triggered by this)
							</span>
						</div>
						<div class="space-y-1.5">
							<div
								v-for="item in downstream"
								:key="item.logId"
								class="flex items-center justify-between px-3 py-2 rounded-lg bg-[var(--el-fill-color-lighter)] hover:bg-[var(--el-fill-color-light)] cursor-pointer transition-colors"
								@click="goToCase(item.onboardingId)"
							>
								<div class="flex items-center gap-2 min-w-0">
									<el-tag type="success" effect="plain" class="shrink-0">
										{{ item.caseCode || '—' }}
									</el-tag>
									<span
										class="text-sm text-[var(--el-text-color-primary)] truncate"
									>
										{{ item.caseName || item.onboardingId }}
									</span>
								</div>
								<span
									class="text-xs text-[var(--el-text-color-placeholder)] shrink-0 ml-2"
								>
									{{ formatDate(item.createDate) }}
								</span>
							</div>
						</div>
					</div>
				</template>
			</div>
		</el-collapse-transition>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { ArrowRight, Top, Bottom, Loading } from '@element-plus/icons-vue';
import { getRelatedCases } from '@/apis/ow/triggers';

const props = defineProps<{ onboardingId: string }>();
const router = useRouter();

const isOpen = ref(true);
const loading = ref(false);
const upstream = ref<any[]>([]);
const downstream = ref<any[]>([]);

const totalCount = computed(() => upstream.value.length + downstream.value.length);

const formatDate = (dateStr: string) => {
	if (!dateStr) return '';
	try {
		return new Date(dateStr).toLocaleDateString('zh-CN', {
			month: '2-digit',
			day: '2-digit',
			hour: '2-digit',
			minute: '2-digit',
		});
	} catch {
		return dateStr;
	}
};

const goToCase = (id: string) => {
	const url = router.resolve({
		path: '/onboard/onboardDetail',
		query: { onboardingId: id },
	}).href;
	window.open(url, '_blank');
};

const load = async () => {
	if (!props.onboardingId) return;
	loading.value = true;
	try {
		const res = await getRelatedCases(props.onboardingId);
		if (res?.data) {
			upstream.value = res.data.upstream ?? [];
			downstream.value = res.data.downstream ?? [];
		}
	} catch {
		upstream.value = [];
		downstream.value = [];
	} finally {
		loading.value = false;
	}
};

watch(
	() => props.onboardingId,
	(id) => {
		if (id) load();
	}
);
onMounted(() => {
	if (props.onboardingId) load();
});
</script>
