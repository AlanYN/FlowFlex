<template>
	<div class="space-y-6">
		<!-- Success Message -->
		<el-alert
			v-if="successMessage"
			:title="successMessage"
			type="success"
			:closable="false"
			class="mb-4"
		/>

		<!-- Description -->
		<div class="bg-blue-50 dark:bg-blue-900/20 p-4 rounded-md">
			<p class="text-sm text-blue-700 dark:text-blue-300">
				Create login credentials for customers to access the customer portal. They will
				receive an email with instructions to set up their password and complete the
				application form.
			</p>
		</div>

		<!-- Action Buttons -->
		<div class="flex justify-between items-center">			
			<el-button type="primary" @click="handleAddButtonClick">
				<el-icon>
					<Plus />
				</el-icon>
				Add User Login
			</el-button>
		</div>

		<!-- Portal Users Table -->
		<div v-if="portalUsers.length > 0" class="border rounded-md overflow-hidden">
			<el-table :data="portalUsers" style="width: 100%" class="portal-table">
				<el-table-column label="Email" prop="email" />
				<el-table-column label="Status" width="120">
					<template #default="{ row }">
						<el-tag
							:type="getStatusTagType(row.status)"
							:effect="row.status === 'Inactive' ? 'light' : 'dark'"
						>
							{{ row.status }}
						</el-tag>
					</template>
				</el-table-column>
				<el-table-column label="Sent Date" width="140">
					<template #default="{ row }">
						{{ formatDate(row.sentDate) }}
					</template>
				</el-table-column>
				<el-table-column label="Actions" width="320">
					<template #default="{ row }">
						<div class="flex space-x-2">
							<el-button
								size="small"
								@click="resendInvitation(row.email)"
								:disabled="row.status === 'Inactive'"
							>
								<el-icon class="h-3 w-3 mr-1">
									<Refresh />
								</el-icon>
								Resend
							</el-button>
							<el-button
								size="small"
								:type="getToggleButtonType(row.status)"
								@click="handleToggleStatus(row)"
							>
								<el-icon class="h-3 w-3 mr-1">
									<Switch />
								</el-icon>
								{{ getToggleButtonText(row.status) }}
							</el-button>
							<el-button
								size="small"
								type="info"
								@click="handleViewInvitationLink(row)"
							>
								<el-icon class="h-3 w-3 mr-1">
									<View />
								</el-icon>
								View
							</el-button>
						</div>
					</template>
				</el-table-column>
			</el-table>
		</div>

		<!-- Empty State -->
		<div v-else class="text-center p-8 border border-dashed rounded-md">
			<el-icon class="h-10 w-10 mx-auto text-gray-400 mb-2">
				<Message />
			</el-icon>
			<p class="text-gray-500">No portal users added yet.</p>
			<p class="text-gray-500 text-sm mt-1">
				Click the "Add User Login" button to create access for customers.
			</p>
		</div>

		<!-- Add User Dialog -->
		<el-dialog v-model="showAddDialog" title="Add Portal User" width="500px">
			<div class="space-y-4">
				<div>
					<label class="block text-sm font-medium mb-2">Email Addresses</label>
					<InputTag
						v-model="selectedEmails"
						placeholder="Enter email addresses and press enter"
						style-type="normal"
						:limit="10"
						@change="handleEmailTagsChange"
						style="width: 100%; height: 32px"
						class="w-full rounded-md"
					/>
					<div class="text-xs text-gray-500 mt-1">
						Type email addresses and press Enter to add them. You can add multiple
						emails at once.
					</div>
				</div>
			</div>
			<template #footer>
				<div class="flex justify-end space-x-2">
					<el-button @click="showAddDialog = false">Cancel</el-button>
					<el-button
						type="primary"
						@click="handleAddUser"
						:disabled="selectedEmails.length === 0"
					>
						<el-icon>
							<Message />
						</el-icon>
						Send Invitation{{ selectedEmails.length > 1 ? 's' : '' }}
					</el-button>
				</div>
			</template>
		</el-dialog>

		<!-- Invitation Link Dialog -->
		<el-dialog v-model="showInvitationLinkDialog" title="Invitation Link" width="600px">
			<div class="space-y-4">
				<div>
					<h4 class="text-lg font-medium mb-2">Invitation Details</h4>
					<div class="bg-gray-50 p-4 rounded-lg">
						<div class="grid grid-cols-2 gap-4">
							<div>
								<span class="text-sm font-medium text-gray-600">Email:</span>
								<p class="text-sm">{{ currentInvitationUser?.email }}</p>
							</div>
							<div>
								<span class="text-sm font-medium text-gray-600">Status:</span>
								<el-tag
									:type="getStatusTagType(currentInvitationUser?.status || '')"
									size="small"
								>
									{{ currentInvitationUser?.status }}
								</el-tag>
							</div>
						</div>
					</div>
				</div>

				<div>
					<label class="block text-sm font-medium mb-2">Invitation Link:</label>
					<div class="flex items-center space-x-2">
						<el-input v-model="currentInvitationUrl" readonly class="flex-1" />
						<el-button @click="openInvitationLink">
							<el-icon class="h-3 w-3 mr-1">
								<View />
							</el-icon>
							View
						</el-button>
					</div>
				</div>

				<div class="bg-blue-50 p-4 rounded-lg">
					<p class="text-sm text-blue-800">
						<strong>Note:</strong>
						Share this link with the customer to access their onboarding portal. The
						link is encrypted and secure.
					</p>
				</div>
			</div>

			<template #footer>
				<div class="flex justify-end space-x-2">
					<el-button @click="showInvitationLinkDialog = false">Close</el-button>
					<el-button type="primary" @click="openInvitationLink">
						<el-icon class="h-3 w-3 mr-1">
							<View />
						</el-icon>
						View Link
					</el-button>
				</div>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { ref, watchEffect } from 'vue';
import { ElMessage } from 'element-plus';
import { Plus, Refresh, View, Message, Switch } from '@element-plus/icons-vue';
import * as userInvitationApi from '@/apis/ow/userInvitation';
import type { PortalUser } from '@/apis/ow/userInvitation';
import InputTag from '@/components/global/u-input-tags/index.vue';

// Props
interface Props {
	onboardingId: string;
	onboardingData?: any;
}

const props = defineProps<Props>();

// Reactive data
const showAddDialog = ref(false);
const showInvitationLinkDialog = ref(false);
const selectedEmails = ref<string[]>([]);
const currentInvitationUser = ref<PortalUser | null>(null);
const currentInvitationUrl = ref('');
const successMessage = ref('');
const loading = ref(false);

// Utility functions
const formatDate = (dateString: string) => {
	if (!dateString) return '--';

	try {
		const date = new Date(dateString);
		if (isNaN(date.getTime())) {
			return dateString;
		}
		// Format as MM/dd/yyyy HH:mm:ss (US format)
		const month = String(date.getMonth() + 1).padStart(2, '0');
		const day = String(date.getDate()).padStart(2, '0');
		const year = date.getFullYear();
		const hours = String(date.getHours()).padStart(2, '0');
		const minutes = String(date.getMinutes()).padStart(2, '0');
		const seconds = String(date.getSeconds()).padStart(2, '0');
		return `${month}/${day}/${year} ${hours}:${minutes}:${seconds}`;
	} catch (error) {
		console.error('Error formatting date:', error);
		return dateString;
	}
};

// Portal users data
const portalUsers = ref<PortalUser[]>([]);

// Methods
const handleAddButtonClick = () => {
	console.log('Add button clicked');
	console.log('showAddDialog before:', showAddDialog.value);
	showAddDialog.value = true;
	console.log('showAddDialog after:', showAddDialog.value);
};

const loadPortalUsers = async () => {
	try {
		console.log('Loading portal users for onboarding ID:', props.onboardingId);
		const response = await userInvitationApi.getPortalUsers(props.onboardingId);
		console.log('API response:', response);

		// Handle different response formats
		let users: PortalUser[] = [];
		if (Array.isArray(response)) {
			users = response;
		} else if (response && Array.isArray((response as any).data)) {
			users = (response as any).data;
		} else if (
			response &&
			(response as any).result &&
			Array.isArray((response as any).result)
		) {
			users = (response as any).result;
		} else {
			console.warn('Unexpected API response format:', response);
			users = [];
		}

		portalUsers.value = users;
		console.log('Portal users loaded:', users);
	} catch (error) {
		console.error('Error loading portal users:', error);
		portalUsers.value = [];
		ElMessage.error('Failed to load portal users');
	}
};
const handleAddUser = async () => {
	if (!selectedEmails.value.length) {
		ElMessage.warning('Please select at least one email address');
		return;
	}

	try {
		loading.value = true;
		const response = await userInvitationApi.sendInvitations({
			onboardingId: props.onboardingId,
			emailAddresses: selectedEmails.value,
		});

		// 从响应中提取实际的邀请数据
		const invitationData = response?.data || response;
		console.log('Send invitations response:', response);
		console.log('Send invitations data:', invitationData);

		if (invitationData.totalSent > 0) {
			successMessage.value = `Invitations sent to ${invitationData.successfulInvitations.join(
				', '
			)} successfully.`;

			// Show failed invitations if any
			if (invitationData.totalFailed > 0) {
				const failedMessages = Object.entries(invitationData.failedInvitations)
					.map(([email, error]) => `${email}: ${error}`)
					.join('\n');
				ElMessage.warning(`Some invitations failed:\n${failedMessages}`);
			}

			// Refresh portal users list
			await loadPortalUsers();
		} else {
			ElMessage.error('Failed to send invitations');
		}

		setTimeout(() => {
			successMessage.value = '';
		}, 5000);
	} catch (error) {
		ElMessage.error('Failed to send invitations');
		console.error('Error sending invitations:', error);
	} finally {
		loading.value = false;
		selectedEmails.value = [];
		showAddDialog.value = false;
	}
};

const handleViewInvitationLink = async (user: PortalUser) => {
	try {
		// 获取邀请链接
		const response = await userInvitationApi.getInvitationLink(props.onboardingId, user.email);
		const invitationUrl = response?.invitationUrl || (response as any)?.data?.invitationUrl;

		if (invitationUrl) {
			// 解析URL，仅保留路径部分
			try {
				const url = new URL(invitationUrl);
				const localInvitationUrl = `${url.pathname}${url.search}`;
				
				// 显示邀请链接对话框
				currentInvitationUser.value = user;
				currentInvitationUrl.value = localInvitationUrl;
				showInvitationLinkDialog.value = true;
			} catch (error) {
				// 如果URL解析失败，使用原始URL
				currentInvitationUser.value = user;
				currentInvitationUrl.value = invitationUrl;
				showInvitationLinkDialog.value = true;
			}
		} else {
			ElMessage.warning('Unable to retrieve invitation link');
		}
	} catch (error) {
		console.error('Failed to get invitation link:', error);
		ElMessage.error('Failed to retrieve invitation link');
	}
};

// Open invitation link in new tab
const openInvitationLink = () => {
	if (currentInvitationUrl.value) {
		window.open(currentInvitationUrl.value, '_blank');
	} else {
		ElMessage.error('Invitation link not available');
	}
};

const resendInvitation = async (email: string) => {
	try {
		const response = await userInvitationApi.resendInvitation({
			onboardingId: props.onboardingId,
			email,
		});
		console.log('Resend invitation response:', response);

		// Refresh portal users list
		await loadPortalUsers();

		// Show success message
		successMessage.value = `Invitation resent to ${email} successfully.`;
		setTimeout(() => {
			successMessage.value = '';
		}, 5000);
	} catch (error) {
		ElMessage.error('Failed to resend invitation');
		console.error('Error resending invitation:', error);
	}
};

const handleViewCustomerPortal = () => {
	// Generate customer portal URL using current environment
	const customerPortalUrl = `/customer-portal?onboardingId=${props.onboardingId}`;

	// Open in new window/tab
	window.open(customerPortalUrl, '_blank');
};

// Get status tag type for different statuses
const getStatusTagType = (status: string) => {
	switch (status) {
		case 'Active':
			return 'success';
		case 'Inactive':
			return 'info';
		default:
			return 'info';
	}
};

// Get toggle button type based on status
const getToggleButtonType = (status: string) => {
	switch (status) {
		case 'Active':
			return 'warning'; // Orange for deactivate
		case 'Inactive':
			return 'primary'; // Blue for activate
		default:
			return 'primary';
	}
};

// Get toggle button text based on status
const getToggleButtonText = (status: string) => {
	switch (status) {
		case 'Active':
			return 'Deactivate';
		case 'Inactive':
			return 'Activate';
		default:
			return 'Activate';
	}
};

// Handle status toggle (Active/Inactive)
const handleToggleStatus = async (user: PortalUser) => {
	try {
		// Determine if we're activating or deactivating
		// Active -> Inactive (deactivating)
		// Pending/Inactive -> Active (activating)
		// Used status cannot be changed back to Active
		const isActivating = user.status !== 'Active';
		const response = await userInvitationApi.togglePortalAccessStatus(
			props.onboardingId,
			user.email,
			isActivating
		);

		console.log('Toggle status response:', response);

		// Refresh portal users list
		await loadPortalUsers();

		// Show success message
		const statusText = isActivating ? 'activated' : 'deactivated';
		successMessage.value = `Portal access ${statusText} for ${user.email} successfully.`;
		setTimeout(() => {
			successMessage.value = '';
		}, 5000);
	} catch (error) {
		ElMessage.error('Failed to toggle portal access status');
		console.error('Error toggling portal access status:', error);
	}
};

// Email tags change handler for InputTag component
const handleEmailTagsChange = (emails: string[]) => {
	selectedEmails.value = emails;
};

// Load data when component is ready
watchEffect(async () => {
	if (props.onboardingId) {
		console.log('Portal Access Content loading for onboarding ID:', props.onboardingId);
		await loadPortalUsers();
	}
});
</script>

<style scoped lang="scss">
.space-y-2 > * + * {
	margin-top: 0.5rem;
}

.space-y-4 > * + * {
	margin-top: 1rem;
}

.space-y-6 > * + * {
	margin-top: 1.5rem;
}

.space-x-2 > * + * {
	margin-left: 0.5rem;
}

.text-sm {
	font-size: 0.875rem;
	line-height: 1.25rem;
}

.text-gray-500 {
	color: #6b7280;
}

.rounded-md {
	border-radius: 0.375rem;
}

.p-4 {
	padding: 1rem;
}

.p-8 {
	padding: 2rem;
}

.mb-2 {
	margin-bottom: 0.5rem;
}

.mb-4 {
	margin-bottom: 1rem;
}

.pt-4 {
	padding-top: 1rem;
}

.text-center {
	text-align: center;
}

.flex {
	display: flex;
}

.items-center {
	align-items: center;
}

.justify-end {
	justify-content: flex-end;
}

.w-3 {
	width: 0.75rem;
}

.w-10 {
	width: 2.5rem;
}

.h-3 {
	height: 0.75rem;
}

.h-10 {
	height: 2.5rem;
}

.mr-1 {
	margin-right: 0.25rem;
}

.mx-auto {
	margin-left: auto;
	margin-right: auto;
}

.border {
	border-width: 1px;
}

.border-t {
	border-top-width: 1px;
}

.border-dashed {
	border-style: dashed;
}

.overflow-hidden {
	overflow: hidden;
}

/* Portal Table 样式 */
:deep(.portal-table .el-table__header-wrapper) {
	background-color: var(--primary-10);
}

:deep(.portal-table .el-table__header) {
	background-color: var(--primary-10);
}

:deep(.portal-table .el-table__header th) {
	background-color: var(--primary-10) !important;
	border: none;
	font-weight: 500;
}

:deep(.portal-table .el-table__header th .cell) {
	color: #374151;
	font-weight: 500;
}

/* 暗色主题下的表格样式 */
html.dark :deep(.portal-table .el-table__header-wrapper) {
	background-color: rgba(59, 130, 246, 0.2);
}

html.dark :deep(.portal-table .el-table__header) {
	background-color: rgba(59, 130, 246, 0.2);
}

html.dark :deep(.portal-table .el-table__header th) {
	background-color: rgba(59, 130, 246, 0.2) !important;
}

html.dark :deep(.portal-table .el-table__header th .cell) {
	color: rgb(147, 197, 253);
}

/* 暗色主题样式 */
html.dark {
	.text-gray-500 {
		color: #9ca3af;
	}
}

/* InputTag组件样式调整 - 保持原有高度和宽度 */
:deep(.layout) {
	min-height: 32px;
	height: 32px;
	border: 1px solid var(--el-border-color, #dcdfe6);
	border-radius: 8px;
	padding: 4px 11px;
	background-color: var(--el-fill-color-blank, #ffffff);
	transition: all var(--el-transition-duration, 0.2s);
	box-shadow: 0 0 0 1px transparent inset;
	font-size: 14px;
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 4px;
}

:deep(.layout:hover) {
	border-color: var(--el-border-color-hover, #c0c4cc);
}

:deep(.layout:focus-within) {
	border-color: var(--primary-500, #409eff);
	box-shadow: 0 0 0 1px var(--primary-500, #409eff) inset !important;
}

:deep(.input-tag) {
	min-width: 100px;
	height: 24px;
	line-height: 24px;
	font-size: 14px;
	color: var(--el-text-color-regular, #606266);
	border: none;
	outline: none;
	background: transparent;
	flex: 1;
	padding: 0;
}

:deep(.input-tag::placeholder) {
	color: var(--el-text-color-placeholder, #a8abb2);
	font-size: 14px;
}

:deep(.label-box) {
	height: 24px;
	margin: 0;
	border-radius: 12px;
	background-color: var(--el-fill-color-light, #f5f7fa);
	border: 1px solid var(--el-border-color-lighter, #e4e7ed);
	display: inline-flex;
	align-items: center;
	padding: 0 8px;
	transition: all 0.2s ease;
}

:deep(.label-title) {
	font-size: 12px;
	padding: 0;
	line-height: 24px;
	color: var(--el-text-color-regular, #606266);
	font-weight: 500;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	max-width: 120px;
}

:deep(.label-close) {
	padding: 0;
	margin-left: 6px;
	color: var(--el-text-color-placeholder, #a8abb2);
	cursor: pointer;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 16px;
	height: 16px;
	border-radius: 50%;
	background: var(--el-fill-color, #f0f2f5);
	transition: all 0.2s ease;
	transform: none;
}

:deep(.label-close:hover) {
	background: var(--el-fill-color-dark, #e6e8eb);
	color: var(--el-text-color-regular, #606266);
}

:deep(.label-close:after) {
	content: '×';
	font-size: 12px;
	line-height: 1;
	font-weight: bold;
}
</style>
