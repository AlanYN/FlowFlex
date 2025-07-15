<template>
	<div class="portal-access-management">
		<!-- Page Header -->
		<div class="page-header mb-6">
			<div class="flex items-center justify-between">
				<div>
					<h1 class="text-2xl font-bold text-gray-900 dark:text-white">
						Portal Access Management
					</h1>
					<p class="text-sm text-gray-600 dark:text-gray-400 mt-1">
						Manage customer portal access for onboarding process
					</p>
				</div>
				<el-button @click="goBack" type="default">
					<el-icon class="mr-2">
						<ArrowLeft />
					</el-icon>
					Back to Onboarding List
				</el-button>
			</div>
		</div>

		<!-- Loading State -->
		<div v-if="loading" class="flex justify-center items-center h-64">
			<el-icon class="animate-spin text-2xl">
				<Loading />
			</el-icon>
		</div>

		<!-- Error State -->
		<el-alert
			v-else-if="error"
			:title="error"
			type="error"
			:closable="false"
			class="mb-4"
		/>

		<!-- Portal Access Content -->
		<div v-else class="portal-access-content">
			<PortalAccessContent :onboarding-id="onboardingId.toString()" />
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElButton, ElAlert, ElIcon } from 'element-plus';
import { ArrowLeft, Loading } from '@element-plus/icons-vue';
import PortalAccessContent from '@/views/onboard/onboardingList/components/PortalAccessContent.vue';

// Router
const route = useRoute();
const router = useRouter();

// Data
const onboardingId = ref<number>(0);
const loading = ref(true);
const error = ref<string>('');

// Methods
const goBack = () => {
	router.push('/onboard/onboardList');
};

const initializePage = () => {
	try {
		const leadId = route.params.leadId as string;
		if (!leadId || isNaN(Number(leadId))) {
			error.value = 'Invalid onboarding ID';
			return;
		}
		
		onboardingId.value = Number(leadId);
		loading.value = false;
	} catch (err) {
		error.value = 'Failed to load onboarding information';
		loading.value = false;
	}
};

// Lifecycle
onMounted(() => {
	initializePage();
});
</script>

<style scoped>
.portal-access-management {
	padding: 20px;
	min-height: 100vh;
	background-color: var(--el-bg-color-page);
}

.page-header {
	border-bottom: 1px solid var(--el-border-color);
	padding-bottom: 20px;
}

.portal-access-content {
	background: var(--el-bg-color);
	border-radius: 8px;
	padding: 24px;
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

@media (max-width: 768px) {
	.portal-access-management {
		padding: 16px;
	}
	
	.portal-access-content {
		padding: 16px;
	}
}
</style> 