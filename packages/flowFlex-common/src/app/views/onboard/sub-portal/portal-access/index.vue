<template>
	<div
		class="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4"
	>
		<div class="max-w-md w-full bg-white rounded-lg shadow-lg overflow-hidden">
			<!-- Header -->
			<div class="bg-gradient-to-r from-blue-500 to-indigo-600 px-6 py-4">
				<h1 class="text-xl font-semibold text-white text-center">
					Portal Access Verification
				</h1>
			</div>

			<!-- Content -->
			<div class="p-6">
				<!-- Loading State (during auto-verification) -->
				<div v-if="loading && verificationState === 'form'" class="text-center space-y-4">
					<div
						class="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto"
					>
						<svg
							class="w-8 h-8 text-blue-600 animate-spin"
							fill="none"
							stroke="currentColor"
							viewBox="0 0 24 24"
						>
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
							/>
						</svg>
					</div>
					<h3 class="text-lg font-semibold text-gray-800">Checking Access...</h3>
					<p class="text-gray-600">Verifying your invitation, please wait...</p>
				</div>

				<!-- User Already Logged In State -->
				<div v-if="verificationState === 'userLoggedIn'" class="space-y-4">
					<div class="text-center mb-6">
						<div
							class="w-16 h-16 bg-yellow-100 rounded-full flex items-center justify-center mx-auto mb-3"
						>
							<svg
								class="w-8 h-8 text-yellow-600"
								fill="none"
								stroke="currentColor"
								viewBox="0 0 24 24"
							>
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 14.5c-.77.833.192 2.5 1.732 2.5z"
								/>
							</svg>
						</div>
						<h3 class="text-lg font-semibold text-gray-800">User Already Logged In</h3>
						<p class="text-gray-600 mb-4">
							You are currently logged in as
							<span class="font-semibold text-blue-600">
								{{ currentLoggedUser?.email || currentLoggedUser?.userName }}
							</span>
						</p>
						<p class="text-sm text-gray-500">
							Using the portal access will replace your current login session. You can
							choose to continue with your current account or enter a different email
							address.
						</p>
					</div>

					<div class="space-y-3">
						<el-button
							type="primary"
							size="large"
							class="w-full"
							@click="useContinueWithCurrentUser"
							:loading="loading"
						>
							Continue with
							{{ currentLoggedUser?.email || currentLoggedUser?.userName }}
						</el-button>

						<el-button size="large" class="w-full" @click="switchToEmailForm">
							Use Different Email Address
						</el-button>

						<el-button
							type="text"
							size="small"
							class="w-full text-gray-500"
							@click="logoutAndContinue"
						>
							Logout Current User
						</el-button>
					</div>
				</div>

				<!-- Form State -->
				<div v-if="verificationState === 'form' && !loading" class="space-y-4">
					<div class="text-center mb-6">
						<div
							class="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-3"
						>
							<svg
								class="w-8 h-8 text-blue-600"
								fill="none"
								stroke="currentColor"
								viewBox="0 0 24 24"
							>
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"
								/>
							</svg>
						</div>
						<p class="text-gray-600">
							Please enter your email address to verify your invitation and access the
							customer portal.
						</p>
					</div>

					<el-form ref="formRef" :model="form" :rules="rules" label-position="top">
						<el-form-item label="Email Address" prop="email">
							<el-input
								v-model="form.email"
								type="email"
								placeholder="Enter your email address"
								size="large"
								:disabled="loading"
							/>
						</el-form-item>

						<el-button
							type="primary"
							size="large"
							class="w-full"
							@click="handleVerify"
							:loading="loading"
						>
							Verify Access
						</el-button>

						<el-button
							v-if="currentLoggedUser"
							type="text"
							size="small"
							class="w-full text-gray-500 mt-2"
							@click="backToUserSelection"
						>
							← Back to User Selection
						</el-button>
					</el-form>
				</div>

				<!-- Success State -->
				<div v-if="verificationState === 'success'" class="text-center space-y-4">
					<div
						class="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto"
					>
						<svg
							class="w-8 h-8 text-green-600"
							fill="none"
							stroke="currentColor"
							viewBox="0 0 24 24"
						>
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
							/>
						</svg>
					</div>
					<h3 class="text-lg font-semibold text-gray-800">{{ successMessage }}</h3>
					<p class="text-gray-600">
						You will be redirected to the customer portal shortly.
					</p>
					<el-button
						type="primary"
						size="large"
						@click="redirectToCustomerPortal"
						class="w-full"
					>
						Continue to Customer Portal
					</el-button>
				</div>

				<!-- Error State -->
				<div v-if="verificationState === 'error'" class="text-center space-y-4">
					<div
						class="w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mx-auto"
					>
						<svg
							class="w-8 h-8 text-red-600"
							fill="none"
							stroke="currentColor"
							viewBox="0 0 24 24"
						>
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 14.5c-.77.833.192 2.5 1.732 2.5z"
							/>
						</svg>
					</div>
					<h3 class="text-lg font-semibold text-red-800">{{ errorMessage }}</h3>
					<p class="text-gray-600">{{ errorDescription }}</p>
					<div class="flex gap-3">
						<el-button @click="retryVerification" class="flex-1">Try Again</el-button>
						<el-button type="primary" @click="contactSupport" class="flex-1">
							Contact Support
						</el-button>
					</div>
				</div>

				<!-- Expired State -->
				<div v-if="verificationState === 'expired'" class="text-center space-y-4">
					<div
						class="w-16 h-16 bg-orange-100 rounded-full flex items-center justify-center mx-auto"
					>
						<svg
							class="w-8 h-8 text-orange-600"
							fill="none"
							stroke="currentColor"
							viewBox="0 0 24 24"
						>
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
							/>
						</svg>
					</div>
					<h3 class="text-lg font-semibold text-orange-800">Invitation Expired</h3>
					<p class="text-gray-600">
						This invitation link has expired. Please contact the organization to request
						a new invitation.
					</p>
					<div class="flex gap-3">
						<el-button @click="requestNewInvitation" class="flex-1">
							Request New Invitation
						</el-button>
						<el-button type="primary" @click="contactSupport" class="flex-1">
							Contact Support
						</el-button>
					</div>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, type FormInstance } from 'element-plus';
import * as userInvitationApi from '@/apis/ow/userInvitation';
import * as userApi from '@/apis/ow/user';
import { useUserStore } from '@/stores/modules/user';
import dayjs from 'dayjs';
import { parseJWT } from '@/utils';

// Router and route
const route = useRoute();
const router = useRouter();

// Refs
const formRef = ref<FormInstance>();

// States
const loading = ref(false);
const registering = ref(false);
const verificationState = ref<'form' | 'success' | 'error' | 'expired' | 'userLoggedIn'>('form');
const successMessage = ref('');
const errorMessage = ref('');
const errorDescription = ref('');
const currentLoggedUser = ref<any>(null);

// Form data
const form = ref({
	email: '',
});

// Validation rules
const rules = {
	email: [
		{ required: true, message: 'Please enter your email address', trigger: 'blur' },
		{ type: 'email' as const, message: 'Please enter a valid email address', trigger: 'blur' },
	],
};

// Methods
const handleVerify = async () => {
	if (!formRef.value) return;

	try {
		await formRef.value.validate();
		loading.value = true;

		// 只使用短URL验证
		const shortUrlId = route.params.shortUrlId as string;

		if (!shortUrlId) {
			ElMessage.error('Invalid invitation link');
			return;
		}

		// 使用短URL验证
		const response = (await userInvitationApi.verifyPortalAccessByShortUrl(shortUrlId, {
			email: form.value.email,
		})) as any;
		const verificationData = response.data || response;

		if (verificationData.isValid) {
			// Store portal access token
			localStorage.setItem('portal_access_token', verificationData.accessToken);
			localStorage.setItem('onboarding_id', verificationData.onboardingId.toString());

			// 邮箱验证成功后，自动内部注册并跳转（无需密码）
			await handleAutoRegisterAndRedirect();
		} else {
			if (verificationData.isExpired) {
				verificationState.value = 'expired';
			} else {
				verificationState.value = 'error';
				errorMessage.value = verificationData.errorMessage || 'Verification failed';
				errorDescription.value = getErrorDescription(verificationData.errorMessage);
			}
		}
	} catch (error) {
		verificationState.value = 'error';
		errorMessage.value = 'Connection Error';
		errorDescription.value =
			'Unable to verify your invitation. Please check your internet connection and try again.';
		console.error('Verification error:', error);
	} finally {
		loading.value = false;
	}
};

const redirectToCustomerPortal = () => {
	const onboardingId = localStorage.getItem('onboarding_id');
	if (onboardingId) {
		router.push(`/customer-portal?onboardingId=${onboardingId}`);
	} else {
		router.push('/customer-portal');
	}
};

// 自动注册并重定向到portal页面
const handleAutoRegisterAndRedirect = async () => {
	try {
		registering.value = true;

		const onboardingId = localStorage.getItem('onboarding_id');
		if (!onboardingId) {
			throw new Error('Case ID not found');
		}

		// 检查用户是否已存在
		const emailExistsResponse = await userApi.checkEmailExists(form.value.email);
		const emailExists = emailExistsResponse?.data || emailExistsResponse;

		let loginData;

		if (!emailExists) {
			// 用户不存在，自动内部注册（无需密码）
			const autoPassword = `portal_${Date.now()}_${Math.random().toString(36).substring(2)}`;

			await userApi.portalAutoRegisterAndLogin({
				email: form.value.email,
				password: autoPassword,
				onboardingId,
			});

			// 注册后自动登录
			const loginResponse = await userApi.loginUser({
				email: form.value.email,
				password: autoPassword,
			});
			loginData = loginResponse?.data || loginResponse;
		} else {
			// 用户已存在，使用短URL验证返回的access token
			const portalAccessToken = localStorage.getItem('portal_access_token');
			if (portalAccessToken) {
				// 模拟loginData结构，使用portal access token
				loginData = {
					accessToken: portalAccessToken,
					expiresIn: 86400, // 24 hours
					tokenType: 'Bearer',
					user: {
						email: form.value.email,
						username: form.value.email,
						id: form.value.email, // 临时使用邮箱作为用户ID
					},
				};
			} else {
				throw new Error('Portal access token not found');
			}
		}

		// 设置用户认证信息
		const userStore = useUserStore();
		const currentDate = dayjs(new Date()).unix();

		userStore.setTokenobj({
			accessToken: {
				token: loginData.accessToken,
				expire: currentDate + (loginData.expiresIn || 86400), // Default 24 hours
				tokenType: loginData.tokenType || 'Bearer',
			},
			refreshToken: loginData.accessToken, // Use access token as refresh token for simplicity
		});

		userStore.setUserInfo({
			...loginData.user,
			userName: loginData.user.email || loginData.user.username,
			userId: loginData.user.id,
		});

		// 显示成功状态
		verificationState.value = 'success';
		successMessage.value = 'Welcome! Redirecting to customer portal...';

		// 自动跳转
		setTimeout(() => {
			redirectToCustomerPortal();
		}, 1000);
	} catch (error: any) {
		console.error('Auto registration error:', error);
		verificationState.value = 'error';
		errorMessage.value = 'Registration Failed';
		errorDescription.value =
			'Failed to access the portal. Please try again or contact support.';
	} finally {
		registering.value = false;
	}
};

// Get user-friendly error description
const getErrorDescription = (errorMessage?: string): string => {
	if (!errorMessage) return 'Please try again or contact support if the problem persists.';

	if (errorMessage.includes('Email address does not match')) {
		return 'Please make sure you are using the correct email address that received the invitation.';
	}
	if (errorMessage.includes('deactivated')) {
		return 'Your access has been temporarily disabled. Please contact the organization for assistance.';
	}
	if (errorMessage.includes('corrupted')) {
		return 'The invitation link appears to be damaged. Please request a new invitation link.';
	}
	return 'Please try again or contact support if the problem persists.';
};

// Request new invitation
const requestNewInvitation = () => {
	ElMessage.info(
		'Please contact the organization that sent you the original invitation to request a new link.'
	);
};

// Contact support
const contactSupport = () => {
	const supportEmail = 'support@flowflex.com';
	window.location.href = `mailto:${supportEmail}?subject=Portal Access Issue&body=I need help with my portal access invitation.`;
};

// Retry verification
const retryVerification = () => {
	verificationState.value = 'form';
	errorMessage.value = '';
	errorDescription.value = '';
	form.value.email = '';
};

// Continue with current logged user
const useContinueWithCurrentUser = async () => {
	console.log('useContinueWithCurrentUser called');
	console.log('currentLoggedUser:', currentLoggedUser.value);

	const userEmail = currentLoggedUser.value?.email || currentLoggedUser.value?.userName;

	if (!userEmail) {
		console.error('No email found in currentLoggedUser:', currentLoggedUser.value);
		ElMessage.error('Current user email not found');
		return;
	}

	console.log('Using email for verification:', userEmail);

	try {
		loading.value = true;

		// 只使用短URL验证
		const shortUrlId = route.params.shortUrlId as string;

		if (!shortUrlId) {
			ElMessage.error('Invalid invitation link');
			return;
		}

		// 使用短URL验证
		const response = (await userInvitationApi.verifyPortalAccessByShortUrl(shortUrlId, {
			email: userEmail,
		})) as any;
		const verificationData = response.data || response;

		if (verificationData.isValid) {
			// Store portal access token
			localStorage.setItem('portal_access_token', verificationData.accessToken);
			localStorage.setItem('onboarding_id', verificationData.onboardingId.toString());

			// 邮箱验证成功后，自动内部注册并跳转（无需密码）
			form.value.email = userEmail; // Set form email for registration flow
			await handleAutoRegisterAndRedirect();
		} else {
			if (verificationData.isExpired) {
				verificationState.value = 'expired';
			} else {
				verificationState.value = 'error';
				errorMessage.value = verificationData.errorMessage || 'Verification failed';
				errorDescription.value = getErrorDescription(verificationData.errorMessage);
			}
		}
	} catch (error) {
		verificationState.value = 'error';
		errorMessage.value = 'Connection Error';
		errorDescription.value =
			'Unable to verify your invitation. Please check your internet connection and try again.';
		console.error('Verification error:', error);
	} finally {
		loading.value = false;
	}
};

// Switch to email form
const switchToEmailForm = () => {
	verificationState.value = 'form';
	form.value.email = '';
};

// Back to user selection
const backToUserSelection = () => {
	if (currentLoggedUser.value) {
		verificationState.value = 'userLoggedIn';
	} else {
		verificationState.value = 'form';
	}
};

// Logout current user and continue
const logoutAndContinue = () => {
	const userStore = useUserStore();
	userStore.logout();
	currentLoggedUser.value = null;
	verificationState.value = 'form';
	form.value.email = '';
	ElMessage.success('Successfully logged out. Please enter your email to continue.');
};

// Check if current user matches the invitation email
const checkCurrentUserAndAutoLogin = async () => {
	const shortUrlId = route.params.shortUrlId as string;

	if (!shortUrlId) {
		ElMessage.error('Invalid invitation link');
		router.push('/');
		return;
	}

	const userStore = useUserStore();
	const currentUserInfo = userStore.getUserInfo;
	const currentToken = userStore.getToken;

	console.log('Portal Access - Current User Info:', currentUserInfo);
	console.log('Portal Access - Current Token:', currentToken);
	console.log('Portal Access - User Email:', currentUserInfo?.email);
	console.log('Portal Access - User Info Keys:', Object.keys(currentUserInfo || {}));
	console.log('Portal Access - User Info Full:', JSON.stringify(currentUserInfo, null, 2));

	// Try to get email from user info or parse from token
	let userEmail = currentUserInfo?.email || currentUserInfo?.userName;
	if (!userEmail && currentToken) {
		try {
			const parsedToken = parseJWT(currentToken);
			userEmail =
				parsedToken?.email ||
				parsedToken?.[
					'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'
				] ||
				parsedToken?.username ||
				parsedToken?.sub;
			console.log('Portal Access - Email from token:', userEmail);
		} catch (error) {
			console.error('Portal Access - Failed to parse token:', error);
		}
	}

	// If user is already logged in
	if (currentToken && (userEmail || currentUserInfo)) {
		console.log('Portal Access - Current user detected:', userEmail);

		// Store current user info for display
		currentLoggedUser.value = {
			email: userEmail,
			userName: currentUserInfo?.userName || userEmail,
			...currentUserInfo,
		};

		// Show user logged in state instead of auto-verification
		verificationState.value = 'userLoggedIn';

		// Optional: Try auto-verification in background for exact email match
		if (userEmail) {
			try {
				loading.value = true;

				// 使用短URL验证当前用户邮箱是否匹配
				const response = (await userInvitationApi.verifyPortalAccessByShortUrl(shortUrlId, {
					email: userEmail,
				})) as any;
				const verificationData = response.data || response;

				if (verificationData.isValid) {
					// Store portal access token and onboarding ID
					localStorage.setItem('portal_access_token', verificationData.accessToken);
					localStorage.setItem('onboarding_id', verificationData.onboardingId.toString());

					// Auto redirect without requiring additional confirmation if email matches exactly
					verificationState.value = 'success';
					successMessage.value =
						'Welcome back! Email verified. Redirecting to customer portal...';

					setTimeout(() => {
						redirectToCustomerPortal();
					}, 1500);
					return;
				}
			} catch (error) {
				console.error('Background auto-verification error:', error);
				// Continue to show user selection even if auto-verification fails
			} finally {
				loading.value = false;
			}
		}
	} else {
		// No current user, show the form
		verificationState.value = 'form';
	}
};

// Load data on mount
onMounted(() => {
	checkCurrentUserAndAutoLogin();
});
</script>

<style scoped lang="scss">
.disabled-input {
	:deep(.el-input__inner) {
		background-color: #f5f7fa;
		color: #909399;
	}
}
</style>
