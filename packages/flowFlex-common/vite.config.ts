import { readFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import dayjs from 'dayjs';
import { readPackageJSON } from 'pkg-types';
import { defineConfig, loadEnv, type Plugin, type UserConfig } from 'vite';
import { createPlugins } from '@uni/vite-config';

/**
 * Univer resolves its UI text through the locale handed to `createUniver(...)`,
 * but a few components hard-code Chinese labels instead of using their locale
 * service. Rewrite those literals while the module is loaded so the editor is
 * fully English - in the dev pre-bundle as well as in the production bundle.
 */
const UNIVER_TEXT_PATCHES: Array<[string, string]> = [
	// sheets-conditional-formatting-ui: entry of the "cell icon" picker
	['无单元格图标', 'No cell icon'],
	// sheets-table-ui: placeholder of the string condition input
	['请输入', 'Please enter'],
];

function patchUniverText(code: string): string | null {
	let next = code;
	for (const [from, to] of UNIVER_TEXT_PATCHES) next = next.replaceAll(from, to);
	return next === code ? null : next;
}

function univerLocalisationPatch(): Plugin {
	const isUniverModule = (id: string) => id.includes('@univerjs') && /\.(m?js|cjs)$/.test(id);
	return {
		name: 'univer-localisation-patch',
		enforce: 'pre',
		transform(code, id) {
			if (!isUniverModule(id)) return null;
			return patchUniverText(code);
		},
		// The dev server serves pre-bundled dependencies, so the same rewrite has to
		// happen inside esbuild's dependency optimizer as well.
		config() {
			return {
				optimizeDeps: {
					esbuildOptions: {
						plugins: [
							{
								name: 'univer-localisation-patch',
								setup(build: any) {
									build.onLoad(
										{ filter: /@univerjs[/\\].*\.(m?js|cjs)$/ },
										async (args: any) => {
											const patched = patchUniverText(
												await readFile(args.path, 'utf8')
											);
											return patched === null
												? undefined
												: {
														contents: patched,
														loader: 'js',
														resolveDir: dirname(args.path),
												  };
										}
									);
								},
							},
						],
					},
				},
			} as UserConfig;
		},
	};
}

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
					// 暂时不使用 manualChunks，让 Rollup 自动处理分包避免循环依赖
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
		plugins: [...plugins, univerLocalisationPatch()],
		optimizeDeps: {
			include: [
				'@iconify/iconify',
				'element-plus/es',
				'vue',
				'vue-router',
				'pinia',
				// ── Univer ──────────────────────────────────────────────────────
				// All @univerjs/* packages must share the same redi instance
				// (@wendellhu/redi). Each preset bundles redi internally, which creates
				// two separate knownIdentifiers Sets — causing the "You are loading
				// scripts of redi more than once" error and HoverManagerService
				// QuantityCheckError when UniverDoc and UniverSheet are used together.
				//
				// Listing @wendellhu/redi here forces esbuild to deduplicate it into
				// a single module that all Univer packages import from.
				'@wendellhu/redi',
				'@wendellhu/redi/react-bindings',
				// Preset packages — must be pre-bundled together so they share the
				// single deduplicated redi and other shared modules (rxjs, react, etc.)
				'@univerjs/presets',
				'@univerjs/preset-sheets-core',
				'@univerjs/preset-docs-core',
				// Core packages imported directly by editor components
				'@univerjs/core',
				'@univerjs/ui',
				'@univerjs/docs',
				'@univerjs/docs-ui',
				'@univerjs/engine-render',
				// Explicit CJS transitive deps — ensures esbuild converts them to ESM
				// even if Vite's dependency crawler misses them.
				'async-lock',
				'fast-diff',
				'ot-json1',
				'numfmt',
			],
			exclude: ['echarts', 'gsap'],
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
				// Collaborative editing websocket (WebApi/Hubs/CollabWebSocketEndpoint.cs)
				'/ws': {
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
