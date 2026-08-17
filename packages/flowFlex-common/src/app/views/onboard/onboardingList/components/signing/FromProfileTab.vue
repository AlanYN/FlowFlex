<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { ElMessage } from 'element-plus';
import { useRouter } from 'vue-router';
import { useUserStore } from '@/stores/modules/user';
import { getSignatures } from '@/apis/ow/profile';
import type { SignatureItem } from '@/apis/ow/profile';

// ========================= Emits =========================

const emit = defineEmits<{
	signatureSelected: [imageBase64: string];
}>();

// ========================= Dependencies =========================

const router = useRouter();
const userStore = useUserStore();

// ========================= Computed =========================

const userName = computed(() => {
	const { userName = '' } = userStore.getUserInfo || {};
	return userName || 'your';
});

// ========================= State =========================

const signatures = ref<SignatureItem[]>([]);
const loading = ref(false);

// ========================= Data Loading =========================

/**
 * Loads the current user's saved signatures from API.
 * Validates: Requirements 11.2, 11.4
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

// ========================= Interactions =========================

/**
 * Emits the selected signature's base64 image back to the parent dialog.
 * Validates: Requirements 11.2, 11.3
 */
function handleSelectSignature(sig: SignatureItem) {
	emit('signatureSelected', sig.imageBase64);
}

/**
 * Navigates the user to the Profile page to add signatures.
 * Validates: Requirement 11.4
 */
function goToProfile() {
	router.push('/profile');
}

// ========================= Lifecycle =========================

onMounted(() => {
	loadSignatures();
});
</script>

<template>
	<div class="from-profile-tab py-4">
		<!-- Loading skeleton -->
		<template v-if="loading">
			<div class="signature-grid">
				<el-skeleton v-for="i in 3" :key="i" animated class="signature-skeleton-card">
					<template #template>
						<el-skeleton-item variant="image" style="width: 100%; height: 80px" />
					</template>
				</el-skeleton>
			</div>
		</template>

		<!-- Has signatures: grid + prompt text.
             Validates: Requirement 11.2 -->
		<template v-else-if="signatures.length > 0">
			<p class="prompt-text text-sm text-gray-500 mb-3">
				Signatures from
				<span class="font-medium">{{ userName }}</span>
				's profile. Click one to place it.
			</p>
			<div class="signature-grid">
				<div
					v-for="sig in signatures"
					:key="sig.id"
					class="signature-card"
					role="button"
					tabindex="0"
					:aria-label="`Select signature created ${new Date(
						sig.createdDate
					).toLocaleDateString()}`"
					@click="handleSelectSignature(sig)"
					@keydown.enter.space.prevent="handleSelectSignature(sig)"
				>
					<div class="signature-preview">
						<img :src="sig.imageBase64" alt="Signature preview" class="signature-img" />
					</div>
					<div class="signature-date">
						{{ new Date(sig.createdDate).toLocaleDateString() }}
					</div>
				</div>
			</div>
		</template>

		<!-- Empty state: guide user to profile or Draw tab.
             Validates: Requirement 11.4 -->
		<template v-else>
			<div
				class="empty-state flex flex-col items-center justify-center gap-3 py-8 text-center"
			>
				<el-empty description="" :image-size="64">
					<template #description>
						<p class="text-sm text-gray-500 mb-2">
							You don't have any saved signatures yet.
						</p>
						<p class="text-sm text-gray-400">
							Visit
							<el-button link type="primary" class="!px-0.5" @click="goToProfile">
								My Profile
							</el-button>
							to add signatures, or switch to the
							<span class="font-medium text-gray-600">Draw</span>
							tab to draw one now.
						</p>
					</template>
				</el-empty>
			</div>
		</template>
	</div>
</template>

<style scoped lang="scss">
.from-profile-tab {
	min-height: 160px;
}

/* Prompt text */
.prompt-text {
	color: var(--el-text-color-secondary);
}

/* Signature card grid */
.signature-grid {
	@apply grid gap-3;
	grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
}

.signature-skeleton-card {
	@apply rounded-lg p-2;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	min-height: 110px;
}

/* Individual clickable card */
.signature-card {
	@apply rounded-lg overflow-hidden flex flex-col cursor-pointer transition-all duration-150;
	background: var(--el-bg-color);
	border: 2px solid var(--el-border-color-lighter);
	box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);

	&:hover {
		border-color: var(--el-color-primary);
		box-shadow: 0 3px 10px rgba(0, 0, 0, 0.1);
		transform: translateY(-1px);
	}

	&:focus-visible {
		outline: 2px solid var(--el-color-primary);
		outline-offset: 2px;
	}
}

/* Signature image area — simulates PDF background */
.signature-preview {
	@apply flex items-center justify-center p-3 flex-1;
	background-color: #f8f8f5;
	min-height: 80px;
}

.signature-img {
	@apply max-h-16 max-w-full object-contain;
}

/* Date label below the image */
.signature-date {
	@apply text-xs text-center py-1 px-2;
	color: var(--el-text-color-placeholder);
	border-top: 1px solid var(--el-border-color-lighter);
}

/* Dark mode adjustments */
html.dark .signature-preview {
	background-color: #2a2a2a;
}

html.dark .signature-card {
	box-shadow: 0 1px 3px rgba(0, 0, 0, 0.3);
}

html.dark .signature-card:hover {
	box-shadow: 0 3px 10px rgba(0, 0, 0, 0.4);
}
</style>
