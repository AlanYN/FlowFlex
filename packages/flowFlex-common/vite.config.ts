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
						// node_modules 中的包单独打包
						if (id.includes('node_modules')) {
							// 大型库单独打包
							if (id.includes('element-plus')) {
								return 'element-plus';
							}
							if (id.includes('echarts')) {
								return 'echarts';
							}
							if (id.includes('gsap')) {
								return 'gsap';
							}
							if (id.includes('chart.js')) {
								return 'chartjs';
							}
							if (
								id.includes('vue-router') ||
								id.includes('pinia') ||
								id.includes('vue')
							) {
								return 'vue-vendor';
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
