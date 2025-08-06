import { createApp } from 'vue';
import App from './App.vue';
import { setupStore } from '@/stores';
import { autoAnimatePlugin } from '@formkit/auto-animate/vue';
import './style.scss';
import { setupI18n } from './locales/setupI18n';
import { setupRouter, router } from '@/router';
import { setupRouterGuard } from '@/router/guard';
import { vPerMission } from '@/hooks';
import { setTheme, setPrimary } from '@/utils/theme';
import 'default-passive-events';
import ElementPlus from 'element-plus';
import * as ElementPlusIconsVue from '@element-plus/icons-vue';
import { useWujie } from '@/hooks/wujie/micro-app.config';
import { initPortalAccess } from '@/utils/portalAccess';
import { Icon } from '@iconify/vue';

let appInstance: any = null;

async function bootstrap() {
	const app = createApp(App);

	// Initialize portal access tenant info from URL if needed
	// 如果是portal访问页面，从URL中提取租户信息
	initPortalAccess();

	// Multilingual configuration
	// 多语言配置
	await setupI18n(app);

	setupStore(app);
	// 不需要的plugin可注释掉或移除掉

	//配置路由守卫
	await setupRouterGuard(router);

	setupRouter(app);

	// 配置自定义指令
	vPerMission(app);

	app.use(autoAnimatePlugin);

	setTheme(localStorage.getItem('theme') || 'light', true);
	// setTheme(globSetting.environment == 'ITEM' ? 'dark' : localStorage.getItem('theme'), true);
	setPrimary(localStorage.getItem('primary') || 'blue');

	app.use(ElementPlus);
	app.component('Icon', Icon);
	for (const [key, component] of Object.entries(ElementPlusIconsVue)) {
		app.component(key, component);
	}

	appInstance = app;
	await app.mount('#app-root');

	// 确保 sub-app-body class 始终存在
	if (!document.documentElement.classList.contains('sub-app-body')) {
		document.documentElement.classList.add('sub-app-body');
	}

	return app;
}

if (window.__POWERED_BY_WUJIE__) {
	// 禁用无界的样式处理
	window.__WUJIE_MOUNT = () => {
		bootstrap().then(() => {
			setTimeout(() => {
				const { initWujieSubApp } = useWujie();
				initWujieSubApp();
			}, 100);
		});
	};

	window.__WUJIE_UNMOUNT = () => {
		window.$wujie.bus.$off('props-update');
		window.$wujie.bus.$off('primary-change');
		window.$wujie.bus.$off('theme-change');
		window.$wujie.bus.$off('logout');
		appInstance && appInstance.unmount();
		appInstance = null;
	};

	window.__WUJIE.mount();
} else {
	bootstrap();
}
