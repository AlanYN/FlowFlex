import { onMounted, onUnmounted } from 'vue';
import { useGlobSetting } from '@/settings';

export function useIframeMessage() {
	const handleMessage = (event: MessageEvent) => {
		const globSetting = useGlobSetting();
		const allowedOrigin = globSetting.idmUrl;

		// 验证消息来源
		if (!allowedOrigin || !event.origin.includes(allowedOrigin)) {
			return;
		}

		if (event.data?.exceedToken) {
			window.location.reload();
		}
	};

	onMounted(() => {
		window.addEventListener('message', handleMessage);
	});

	onUnmounted(() => {
		window.removeEventListener('message', handleMessage);
	});

	return {
		handleMessage,
	};
}
