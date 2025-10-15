<template>
	<div class="w-full h-full wfe-global-block-bg p-4" ref="scrollbarRef">
		<iframe
			ref="iframeRef"
			id="permission-iframe"
			:src="iframeUrl"
			frameborder="0"
			class="w-full h-full border-none"
		></iframe>
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

const { scrollbarRef } = useAdaptiveScrollbar();

const userStore = useUserStoreWithOut();
const settings = useGlobSetting();

// const BASE_DOMAIN = settings.ssoURL;
// const BASE_URL = `${BASE_DOMAIN}`;
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
	iframeUrl.value = `${settings.idmUrl}/permission/permission?appId=${APP_ID}&userId=${userId}&appToken=${token}&theme=${theme}&primary=${primary}`;
});
</script>
