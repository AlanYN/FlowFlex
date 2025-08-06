import vue from '@vitejs/plugin-vue';
import vueJsx from '@vitejs/plugin-vue-jsx';
import AutoImport from 'unplugin-auto-import/vite';
import IconsResolver from 'unplugin-icons/resolver';
import Icons from 'unplugin-icons/vite';
import Components from 'unplugin-vue-components/vite';
import { type PluginOption } from 'vite';
import { VueMcp } from 'vite-plugin-vue-mcp';
import svgLoader from 'vite-svg-loader';

import { createAppConfigPlugin } from './appConfig';
import { configCompressPlugin } from './compress';
import { configHtmlPlugin } from './html';
import { configMockPlugin } from './mock';
import { configSvgIconsPlugin } from './svgSprite';
import { configVisualizerConfig } from './visualizer';

interface Options {
	isBuild: boolean;
	root: string;
	compress: string;
	enableMock?: boolean;
	enableAnalyze?: boolean;
}

async function createPlugins({ isBuild, root, enableMock, compress, enableAnalyze }: Options) {
	const vitePlugins: (PluginOption | PluginOption[] | any)[] = [vue(), vueJsx(), VueMcp()];

	const appConfigPlugin = await createAppConfigPlugin({ root, isBuild });
	vitePlugins.push(appConfigPlugin);

	//   vite-plugin-html
	vitePlugins.push(configHtmlPlugin({ isBuild }));

	// vite-plugin-svg-icons
	vitePlugins.push(configSvgIconsPlugin({ isBuild }));

	// The following plugins only work in the production environment
	if (isBuild) {
		// rollup-plugin-gzip
		vitePlugins.push(
			configCompressPlugin({
				compress,
			})
		);
	}

	// rollup-plugin-visualizer
	if (enableAnalyze) {
		vitePlugins.push(configVisualizerConfig());
	}

	// vite-plugin-mock
	if (enableMock) {
		vitePlugins.push(configMockPlugin({ isBuild }));
	}

	vitePlugins.push(svgLoader());

	vitePlugins.push(
		AutoImport({
			resolvers: [IconsResolver()],
		})
	);

	vitePlugins.push(
		Components({
			resolvers: [IconsResolver({ prefix: 'Icon' })],
		})
	);

	vitePlugins.push(
		Icons({
			compiler: 'vue3',
			autoInstall: true, // 自动安装图标包
		})
	);

	return vitePlugins;
}

export { createPlugins };
