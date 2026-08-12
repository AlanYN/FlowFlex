<template>
	<div class="w-full h-full wfe-global-block-bg p-4" ref="scrollbarRef">
		<div data-tour="teams-iframe-container" class="w-full h-full">
			<iframe
				ref="iframeRef"
				:src="iframeUrl"
				frameborder="0"
				id="permission-iframe"
				class="w-full h-full border-none"
			></iframe>
		</div>

		<!-- Manage Teams tour（iframe 外框引导） -->
		<TourGuide
			:persist-key="'manage-teams-tour'"
			:steps="manageTeamsTourSteps"
			:auto-start="true"
			:show-fab="true"
			:check-seen-remote="() => checkTourSeen('manage-teams-tour').then((r) => r.data)"
			:mark-seen-remote="() => markTourSeen('manage-teams-tour').then(() => undefined)"
		/>
	</div>
</template>
<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useUserStoreWithOut } from '@/stores/modules/user';
import { getTokenobj } from '@/utils/auth';
import type { TokenObj } from '@/apis/axios/Axios';
import { useGlobSetting } from '@/settings/';
import { ProjectEnum } from '@/enums/appEnum';
import { useIframeMessage } from '@/hooks/useIframeMessage';
import { useAdaptiveScrollbar } from '@/hooks/useAdaptiveScrollbar';
import TourGuide from '@/components/global/TourGuide/index.vue';
import { manageTeamsTourSteps } from '@/hooks/useAdminTourSteps';
import { checkTourSeen, markTourSeen } from '@/apis/ow';

const { scrollbarRef } = useAdaptiveScrollbar();

const userStore = useUserStoreWithOut();
const settings = useGlobSetting();

// const BASE_DOMAIN = settings.ssoURL;
// const BASE_URL = `${BASE_DOMAIN}permission/user`;
const APP_ID = ProjectEnum.WFE;
const iframeUrl = ref();
const iframeRef = ref();

// Use the iframe message hook
useIframeMessage();

onMounted(() => {
	const tokenObj = getTokenobj() as TokenObj;
	const userInfo = userStore.getUserInfo || {};
	const token = tokenObj?.accessToken?.token;
	const { userId } = userInfo;
	const theme = localStorage.theme;
	const primary = localStorage.primary || 'blue';
	iframeUrl.value = `${settings.idmUrl}/permission/teams?appId=${APP_ID}&userId=${userId}&appToken=${token}&theme=${theme}&primary=${primary}`;
});
</script>
