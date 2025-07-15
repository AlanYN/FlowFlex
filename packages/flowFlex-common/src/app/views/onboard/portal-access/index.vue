<template>
	<div class="portal-access-page">
		<div class="container">
			<div class="header">
				<h1>FlowFlex Onboarding Portal</h1>
				<p>Please verify your email to access the onboarding portal</p>
			</div>

			<div v-if="!isVerified" class="verification-form">
				<el-form ref="formRef" :model="form" :rules="rules" label-width="120px">
					<el-form-item label="Email" prop="email">
						<el-input v-model="form.email" placeholder="Enter your email address" />
					</el-form-item>
					<el-form-item>
						<el-button type="primary" @click="handleVerify" :loading="loading">
							Verify Access
						</el-button>
					</el-form-item>
				</el-form>
			</div>

			<div v-else class="success-message">
				<el-result icon="success" title="Access Verified!" :sub-title="successMessage">
					<template #extra>
						<el-button type="primary" @click="redirectToOnboarding">
							Continue to Onboarding
						</el-button>
					</template>
				</el-result>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElForm } from 'element-plus';
import * as userInvitationApi from '@/apis/ow/userInvitation';

const route = useRoute();
const router = useRouter();

// Reactive data
const formRef = ref<InstanceType<typeof ElForm>>();
const loading = ref(false);
const isVerified = ref(false);
const successMessage = ref('');

const form = ref({
	email: '',
});

const rules = {
	email: [
		{ required: true, message: 'Please enter your email address', trigger: 'blur' },
		{ type: 'email', message: 'Please enter a valid email address', trigger: 'blur' },
	],
};

// Methods
const handleVerify = async () => {
	if (!formRef.value) return;

	try {
		await formRef.value.validate();
		loading.value = true;

		const token = route.query.token as string;
		if (!token) {
			ElMessage.error('Invalid invitation link');
			return;
		}

		const response = await userInvitationApi.verifyPortalAccess({
			token,
			email: form.value.email,
		});

		if (response.isValid) {
			isVerified.value = true;
			successMessage.value = `Welcome! You now have access to the onboarding portal.`;
			
			// Store access token for future API calls
			localStorage.setItem('portal_access_token', response.accessToken);
			localStorage.setItem('onboarding_id', response.onboardingId.toString());
		} else {
			ElMessage.error(response.errorMessage || 'Verification failed');
		}
	} catch (error) {
		ElMessage.error('Verification failed. Please try again.');
		console.error('Verification error:', error);
	} finally {
		loading.value = false;
	}
};

const redirectToOnboarding = () => {
	const onboardingId = localStorage.getItem('onboarding_id');
	if (onboardingId) {
		router.push(`/onboarding/${onboardingId}`);
	} else {
		router.push('/onboarding');
	}
};

// Load data on mount
onMounted(() => {
	const token = route.query.token as string;
	if (!token) {
		ElMessage.error('Invalid invitation link');
		router.push('/');
	}
});
</script>

<style scoped lang="scss">
.portal-access-page {
	min-height: 100vh;
	display: flex;
	align-items: center;
	justify-content: center;
	background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.container {
	max-width: 500px;
	width: 100%;
	padding: 2rem;
	background: white;
	border-radius: 12px;
	box-shadow: 0 10px 30px rgba(0, 0, 0, 0.1);
}

.header {
	text-align: center;
	margin-bottom: 2rem;

	h1 {
		color: #333;
		margin-bottom: 0.5rem;
		font-size: 2rem;
		font-weight: 600;
	}

	p {
		color: #666;
		font-size: 1rem;
	}
}

.verification-form {
	.el-form-item {
		margin-bottom: 1.5rem;
	}

	.el-button {
		width: 100%;
	}
}

.success-message {
	:deep(.el-result__title) {
		color: #333;
	}

	:deep(.el-result__subtitle) {
		color: #666;
	}
}
</style> 