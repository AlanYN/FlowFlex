<template>
	<div class="space-y-6">
		<!-- Success Message -->
		<el-alert v-if="successMessage" :title="successMessage" type="success" :closable="false" class="mb-4" />

		<!-- Description -->
		<div class="bg-blue-50 dark:bg-blue-900/20 p-4 rounded-md">
			<p class="text-sm text-blue-700 dark:text-blue-300">
				Create login credentials for customers to access the onboarding portal. They will
				receive an email with instructions to set up their password and complete the
				application form.
			</p>
		</div>

		<!-- Add User Button -->
		<div class="flex justify-end">
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
						<el-tag :type="row.status === 'Active' ? 'success' : 'warning'">
							{{ row.status }}
						</el-tag>
					</template>
				</el-table-column>
				<el-table-column label="Sent Date" width="140">
					<template #default="{ row }">
						{{ formatDate(row.sentDate) }}
					</template>
				</el-table-column>
				<el-table-column label="Actions" width="200">
					<template #default="{ row }">
						<div class="flex space-x-2">
							<el-button size="small" @click="resendInvitation(row.email)"
								:disabled="row.status === 'Active'">
								<el-icon class="h-3 w-3 mr-1">
									<Refresh />
								</el-icon>
								Resend
							</el-button>
							<el-button size="small" class="text-red-500 hover:text-red-700 hover:bg-red-50"
								@click="handleRemoveUser(row)">
								<el-icon class="h-3 w-3 mr-1">
									<Delete />
								</el-icon>
								Remove
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
					<el-select v-model="selectedEmails" multiple filterable allow-create default-first-option
						collapse-tags collapse-tags-tooltip placeholder="Enter email addresses..." class="w-full"
						no-data-text="Type email addresses to add" filter-placeholder="Type email addresses...">
						<el-option v-for="email in emailOptions" :key="email" :label="email" :value="email" />
					</el-select>
					<div class="text-xs text-gray-500 mt-1">
						Type email addresses and press Enter to add them. You can add multiple
						emails at once.
					</div>
				</div>
			</div>
			<template #footer>
				<div class="flex justify-end space-x-2">
					<el-button @click="showAddDialog = false">Cancel</el-button>
					<el-button type="primary" @click="handleAddUser" :disabled="selectedEmails.length === 0">
						<el-icon>
							<Message />
						</el-icon>
						Send Invitation{{ selectedEmails.length > 1 ? 's' : '' }}
					</el-button>
				</div>
			</template>
		</el-dialog>

		<!-- Remove User Confirmation Dialog -->
		<el-dialog v-model="showRemoveDialog" title="Remove Portal Access" width="400px">
			<p>
				Are you sure you want to remove portal access for
				<strong>{{ userToRemove?.email }}</strong>
				?
			</p>
			<template #footer>
				<div class="flex justify-end space-x-2">
					<el-button @click="showRemoveDialog = false">Cancel</el-button>
					<el-button type="danger" @click="confirmRemoveUser">Remove Access</el-button>
				</div>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { ref, watchEffect } from 'vue';
import { ElMessage } from 'element-plus';
import { Plus, Refresh, Delete, Message } from '@element-plus/icons-vue';
import * as userInvitationApi from '@/apis/ow/userInvitation';
import type { PortalUser } from '@/apis/ow/userInvitation';

// Props
interface Props {
	onboardingId: string;
	onboardingData?: any;
}

const props = defineProps<Props>();

// Reactive data
const showAddDialog = ref(false);
const showRemoveDialog = ref(false);
const selectedEmails = ref<string[]>([]);
const emailOptions = ref<string[]>([]);
const userToRemove = ref<PortalUser | null>(null);
const successMessage = ref('');
const loading = ref(false);

// Utility functions
const formatDate = (dateString: string) => {
	if (!dateString) return '--';
	
	try {
		const date = new Date(dateString);
		// 格式化为 YYYY-MM-DD HH:mm
		return date.toLocaleString('zh-CN', {
			year: 'numeric',
			month: '2-digit',
			day: '2-digit',
			hour: '2-digit',
			minute: '2-digit',
			hour12: false
		}).replace(/\//g, '-');
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
		} else if (response && (response as any).result && Array.isArray((response as any).result)) {
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
		const invitationData = response.data || response;
		console.log('Send invitations response:', response);
		console.log('Send invitations data:', invitationData);

		if (invitationData.totalSent > 0) {
			successMessage.value = `Invitations sent to ${invitationData.successfulInvitations.join(', ')} successfully.`;

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

const handleRemoveUser = (user: PortalUser) => {
	userToRemove.value = user;
	showRemoveDialog.value = true;
};

const confirmRemoveUser = async () => {
	if (userToRemove.value) {
		try {
			const removedEmail = userToRemove.value.email;
			const response = await userInvitationApi.removePortalAccess(props.onboardingId, removedEmail);
			console.log('Remove portal access response:', response);

			// Refresh portal users list
			await loadPortalUsers();
			showRemoveDialog.value = false;

			// Show success message
			successMessage.value = `Access removed for ${removedEmail} successfully.`;
			setTimeout(() => {
				successMessage.value = '';
			}, 5000);

			userToRemove.value = null;
		} catch (error) {
			ElMessage.error('Failed to remove portal access');
			console.error('Error removing portal access:', error);
		}
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

const handleComplete = () => {
	ElMessage.success('Portal access management stage completed');
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
.space-y-2>*+* {
	margin-top: 0.5rem;
}

.space-y-4>*+* {
	margin-top: 1rem;
}

.space-y-6>*+* {
	margin-top: 1.5rem;
}

.space-x-2>*+* {
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
	background-color: #eff6ff;
}

:deep(.portal-table .el-table__header) {
	background-color: #eff6ff;
}

:deep(.portal-table .el-table__header th) {
	background-color: #eff6ff !important;
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
</style>
