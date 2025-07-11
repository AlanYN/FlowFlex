import { resolve } from 'node:path';

import dayjs from 'dayjs';
import { readPackageJSON } from 'pkg-types';
import { defineConfig, loadEnv, mergeConfig, type UserConfig } from 'vite';

import { createPlugins } from '../plugins';
// import { generateModifyVars } from '../utils/modifyVars';
import { commonConfig } from './common';

interface DefineOptions {
	overrides?: UserConfig;
	options?: {
		//
	};
}

function defineApplicationConfig(defineOptions: DefineOptions = {}) {
	console.log('define app==');
	const { overrides = {} } = defineOptions;
	return defineConfig(async ({ command, mode }) => {
		const root = process.cwd();
		const isBuild = command === 'build';
		const { VITE_PUBLIC_PATH, VITE_USE_MOCK, VITE_BUILD_COMPRESS, VITE_ENABLE_ANALYZE } =
			loadEnv(mode, root);
		const defineData = await createDefineData(root);
		const plugins = await createPlugins({
			isBuild,
			root,
			enableAnalyze: VITE_ENABLE_ANALYZE === 'true',
			enableMock: VITE_USE_MOCK === 'true',
			compress: VITE_BUILD_COMPRESS,
		});
		const pathResolve = (pathname: string) => resolve(root, pathname);

		const timestamp = new Date().getTime();
		const applicationConfig: UserConfig = {
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
				rollupOptions: {
					// input: {
					// 	main: resolve(root, './src/index.html'),
					//   },
					output: {
						// 入口文件名
						entryFileNames: `assets/entry/[name]-[hash]-${timestamp}.js`,
						manualChunks: {
							vue: ['vue', 'pinia', 'vue-router'],
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
		};

		const mergedConfig = mergeConfig(commonConfig(mode), applicationConfig);

		return mergeConfig(mergedConfig, overrides);
	});
}

async function createDefineData(root: string) {
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
}

export { defineApplicationConfig };
