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

import ElementPlus from 'element-plus';
import * as ElementPlusIconsVue from '@element-plus/icons-vue';

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

	app.mount('#app-root');
}

bootstrap();
