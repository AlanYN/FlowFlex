import { resolve } from 'node:path';
import dayjs from 'dayjs';
import { readPackageJSON } from 'pkg-types';
import { defineConfig, loadEnv, type UserConfig } from 'vite';
import { createPlugins } from '@uni/vite-config';

export default defineConfig(async ({ command, mode }) => {
	const root = process.cwd();
	const isBuild = command === 'build';
	// console.log('isBuild:::::', isBuild, mode);
	const {
		VITE_PUBLIC_PATH,
		VITE_DROP_CONSOLE,
		VITE_USE_MOCK,
		VITE_BUILD_COMPRESS,
		VITE_ENABLE_ANALYZE,
		VITE_PROXY_URL,
	} = loadEnv(mode, root);
	const defineData = await createDefineData(root);
	const plugins = await createPlugins({
		isBuild,
		root,
		enableAnalyze: VITE_ENABLE_ANALYZE === 'true',
		enableMock: VITE_USE_MOCK === 'true',
		compress: VITE_BUILD_COMPRESS,
	});
	const pathResolve = (pathname: string) => resolve(root, pathname);
	const viteConfig: UserConfig = {
		base: VITE_PUBLIC_PATH,
		resolve: {
			alias: [
				{
					find: 'vue-i18n',
					replacement: 'vue-i18n/dist/vue-i18n.cjs.js',
				},
				// @/xxxx => src/xxxx
				{
					find: /@\//,
					replacement: pathResolve('./src/app') + '/',
				},
				// #/xxxx => types/xxxx
				{
					find: /#\//,
					replacement: pathResolve('./types') + '/',
				},
				{
					find: /@assets/,
					replacement: pathResolve('./src/assets') + '/',
				},
				{
					find: /@styles/,
					replacement: pathResolve('./src/styles') + '/',
				},
				{
					find: /@locales/,
					replacement: pathResolve('./src/locales') + '/',
				},
			],
		},
		define: defineData,
		build: {
			target: 'es2015',
			cssTarget: 'chrome80',
			outDir: 'dist',
			chunkSizeWarningLimit: 2000, // 消除打包大小超过500kb警告
			minify: 'terser', // Vite 2.6.x 以上需要配置 minify: "terser", terserOptions 才能生效
			terserOptions: {
				compress: {
					keep_infinity: true, // 防止 Infinity 被压缩成 1/0，这可能会导致 Chrome 上的性能问题
					drop_console: VITE_DROP_CONSOLE === 'true', // 生产环境去除 console
					drop_debugger: VITE_DROP_CONSOLE === 'true', // 生产环境去除 debugger
				},
				format: {
					comments: false, // 删除注释
				},
			},
			rollupOptions: {
				output: {
					manualChunks: (id) => {
						if (id.includes('node_modules')) {
							// Vue 核心 - 精确匹配，避免误匹配其他 vue 相关库
							if (
								id.includes('/vue/') ||
								id.includes('/@vue/runtime') ||
								id.includes('/@vue/reactivity') ||
								id.includes('/@vue/shared')
							) {
								return 'vue-core';
							}
							if (id.includes('/vue-router/')) return 'vue-core';
							if (id.includes('/pinia/')) return 'vue-core';
							if (id.includes('/vue-i18n/')) return 'vue-core';

							// UI 框架
							if (id.includes('element-plus')) return 'element-plus';

							// 图表库
							if (id.includes('echarts') || id.includes('zrender')) return 'echarts';
							if (id.includes('chart.js') || id.includes('vue-chartjs'))
								return 'chartjs';

							// 动画库
							if (id.includes('gsap')) return 'gsap';

							// VueUse 工具库
							if (id.includes('@vueuse')) return 'vueuse';

							// Vue Flow 流程图
							if (id.includes('@vue-flow')) return 'vue-flow';

							// Office 文档处理
							if (id.includes('@vue-office') || id.includes('xlsx')) return 'office';

							// 富文本/Markdown 编辑器
							if (
								id.includes('quill') ||
								id.includes('markdown-it') ||
								id.includes('highlight.js')
							) {
								return 'editor';
							}

							// PDF/Canvas 处理
							if (id.includes('jspdf') || id.includes('html2canvas')) return 'pdf';

							// 工具库
							if (
								id.includes('lodash') ||
								id.includes('dayjs') ||
								id.includes('axios')
							) {
								return 'utils';
							}

							// 图标库
							if (id.includes('@iconify') || id.includes('@element-plus/icons')) {
								return 'icons';
							}

							// 其他第三方库
							return 'vendor';
						}
					},
					// 入口文件名
					entryFileNames: 'js/[name].[hash].js',
					// 用于命名代码拆分时创建的共享块的输出命名
					chunkFileNames: 'js/[name].[hash].js',
					// 用于输出静态资源的命名，[ext]表示文件扩展名
					assetFileNames: (assetInfo: any) => {
						const info = assetInfo.name.split('.');
						let extType = info[info.length - 1];
						// console.log('文件信息', assetInfo.name)
						if (/\.(mp4|webm|ogg|mp3|wav|flac|aac)(\?.*)?$/i.test(assetInfo.name)) {
							extType = 'media';
						} else if (/\.(png|jpe?g|gif|svg)(\?.*)?$/.test(assetInfo.name)) {
							extType = 'img';
						} else if (/\.(woff2?|eot|ttf|otf)(\?.*)?$/i.test(assetInfo.name)) {
							extType = 'fonts';
						}
						return `${extType}/[name].[hash].[ext]`;
					},
				},
			},
		},
		css: {
			postcss: {
				plugins: [require('tailwindcss'), require('autoprefixer')],
			},
			preprocessorOptions: {
				scss: {
					// modifyVars: generateModifyVars(),
					javascriptEnabled: true,
					api: 'modern-compiler',
				},
			},
		},
		plugins,
		optimizeDeps: {
			include: [
				'@iconify/iconify',
				'element-plus/es', // 预构建 Element Plus
				'vue',
				'vue-router',
				'pinia',
			],
			exclude: [
				// 排除不需要预构建的大型库，让它们按需加载
				'echarts',
				'gsap',
			],
		},
		server: {
			open: true,
			host: true,
			cors: {
				origin: 'http://localhost:5174',
				credentials: true,
			},
			proxy: {
				'/api': {
					target: VITE_PROXY_URL,
					changeOrigin: true,
					ws: true,
					secure: false,
				},
			},
			warmup: {
				// clientFiles: ['./public/index.html', './src/{views,components}/*'],
			},
		},
	};

	return viteConfig;
});

const createDefineData = async (root: string) => {
	try {
		const pkgJson = await readPackageJSON(root);
		const { dependencies, devDependencies, name, version } = pkgJson;

		const __APP_INFO__ = {
			pkg: { dependencies, devDependencies, name, version },
			lastBuildTime: dayjs().format('YYYY-MM-DD HH:mm:ss'),
		};
		return {
			__APP_INFO__: JSON.stringify(__APP_INFO__),
		};
	} catch (error) {
		return {};
	}
};
