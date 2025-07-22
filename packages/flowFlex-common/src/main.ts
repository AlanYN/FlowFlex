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

let appInstance: any = null;

async function bootstrap() {
	const app = createApp(App);
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
	console.log('无界环境');
	window.__WUJIE_MOUNT = () => {
		console.log('无界环境挂载开始');
		console.log('window.$wujie:', window.$wujie);

		bootstrap()
			.then(() => {
				console.log('应用挂载完成，初始化无界配置');
				// 确保在应用挂载完成后再初始化无界配置
				setTimeout(() => {
					const { initWujieSubApp } = useWujie();
					initWujieSubApp();
				}, 100);
			})
			.catch((error) => {
				console.error('应用启动失败:', error);
			});
	};

	window.__WUJIE_UNMOUNT = () => {
		console.log('无界环境卸载');
		if (appInstance) {
			appInstance.unmount();
			appInstance = null;
		}
	};

	window.__WUJIE.mount();
} else {
	console.log('非无界环境');
	bootstrap().catch((error) => {
		console.error('应用启动失败:', error);
	});
}
