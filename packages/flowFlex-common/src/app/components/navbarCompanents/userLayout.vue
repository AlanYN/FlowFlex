<template>
	<div class="flex relative group cursor-pointer items-center">
		<el-popover :width="200" class="p-0">
			<template #reference>
				<div class="flex items-center">
					<div
						v-if="!avatar"
						class="h-[35px] w-[35px] rounded-full text-xl font-bold leading-[35px] text-white flex items-center justify-center"
						:style="{ backgroundColor: getAvatarColor(userName) }"
					>
						{{
							userName.split(' ').length > 1
								? userName.split(' ')[1].substring(0, 1)
								: userName.substring(0, 1)
						}}
					</div>
					<img
						v-else
						:src="avatar"
						alt="avatar"
						class="h-[35px] w-[35px] rounded-full"
						:style="{ backgroundColor: getAvatarColor(userName) }"
					/>
				</div>
			</template>
			<div>
				<div class="font-bold text-xs">PROFILE</div>
				<div class="flex items-center my-3">
					<el-tag>{{ userName }}</el-tag>
				</div>
				<div class="flex items-center my-3">
					<el-tag>{{ userStore.getUserInfo.email }}</el-tag>
				</div>

				<div class="flex justify-end">
					<el-button text :icon="LogoutIcon" @click="logout">
						{{ t('sys.app.logout') }}
					</el-button>
				</div>
			</div>
		</el-popover>
	</div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { useUserStore } from '@/stores/modules/user';
import { useI18n } from '@/hooks/useI18n';
import { getAvatarColor } from '@/utils';

import LogoutIcon from '@assets/svg/global/logout.svg';

const userStore = useUserStore();
const { t } = useI18n();

const userName = computed(() => {
	const { userName = '' } = userStore.getUserInfo || {};
	if (userName) {
		return userName;
	} else {
		return 'welcome';
	}
});

const avatar = ref('');

const logout = () => {
	userStore.confirmLoginOut();
};
</script>
