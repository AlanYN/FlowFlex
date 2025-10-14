/** @type {import('tailwindcss').Config} */

/**
 * Tailwind CSS 配置
 * 完全基于 CSS 变量，支持运行时主题切换
 * 符合 Item Element Plus 设计规范
 */

export default {
	content: [
		'./public/index.html',
		'./src/**/*.{js,ts,vue}',
		'./node_modules/tw-elements/dist/js/**/*.js',
	],

	// 作用域前缀
	prefix: '',
	important: ['.sub-app-body', '.flowflex-app'],

	theme: {
		extend: {
			/* ========== 颜色系统 ========== */
			colors: {
				transparent: 'transparent',
				current: 'currentColor',

				/* 基础颜色 - 黑色透明度系列 */
				black: {
					DEFAULT: '#000000',
					5: 'var(--black-5)',
					10: 'var(--black-10)',
					20: 'var(--black-20)',
					30: 'var(--black-30)',
					40: 'var(--black-40)',
					50: 'var(--black-50)',
					60: 'var(--black-60)',
					70: 'var(--black-70)',
					80: 'var(--black-80)',
					90: 'var(--black-90)',
					100: 'var(--black-100)',
					150: 'var(--black-150)',
					200: 'var(--black-200)',
					300: 'var(--black-300)',
					400: 'var(--black-400)',
					500: 'var(--black-500)',
					600: 'var(--black-600)',
					700: 'var(--black-700)',
				},

				/* 基础颜色 - 白色透明度系列 */
				white: {
					DEFAULT: '#ffffff',
					5: 'var(--white-5)',
					10: 'var(--white-10)',
					20: 'var(--white-20)',
					30: 'var(--white-30)',
					40: 'var(--white-40)',
					50: 'var(--white-50)',
					60: 'var(--white-60)',
					70: 'var(--white-70)',
					80: 'var(--white-80)',
					90: 'var(--white-90)',
					100: 'var(--white-100)',
				},

				/* 基础颜色 - 灰度系列 */
				gray: {
					50: 'var(--gray-50)',
					100: 'var(--gray-100)',
					200: 'var(--gray-200)',
					300: 'var(--gray-300)',
					400: 'var(--gray-400)',
					500: 'var(--gray-500)',
					600: 'var(--gray-600)',
					700: 'var(--gray-700)',
					800: 'var(--gray-800)',
					900: 'var(--gray-900)',
					DEFAULT: 'var(--gray-500)',
				},

				/* 项目特定颜色系列（向后兼容旧 tailwind-base.json）*/
				night: {
					50: '#e2e3f5',
					100: '#cbccde',
					200: '#b3b6c8',
					300: '#9c9fb1',
					400: '#84889b',
					500: '#6d7284',
					600: '#555b6e',
					700: '#3e4457',
					800: '#262e41',
					900: '#1c1e33',
				},
				sky: {
					50: '#e3f3fc',
					100: '#cce7f9',
					200: '#b4dcf6',
					300: '#9dd0f3',
					400: '#85c4f0',
					500: '#6eb9ed',
					600: '#56adea',
					700: '#3fa1e7',
					800: '#2795e4',
					900: '#1089e1',
				},
				sea: {
					50: '#e7f4f5',
					100: '#d0e9ec',
					200: '#b8dfe2',
					300: '#a1d4d9',
					400: '#89c9cf',
					500: '#72bfc6',
					600: '#5ab4bc',
					700: '#43a9b3',
					800: '#2b9fa9',
					900: '#1494a0',
				},
				blue: {
					50: '#e6f1fa',
					100: '#cce2f6',
					200: '#99c5ec',
					300: '#66a8e3',
					400: '#338bd9',
					500: '#006ed0',
					600: '#0058a6',
					700: '#00427d',
					800: '#002c53',
					900: '#00162a',
				},
				teal: {
					50: '#e6faf6',
					100: '#ccf5ed',
					200: '#99ecdb',
					300: '#66e2c9',
					400: '#33d9b7',
					500: '#00cfa5',
					600: '#00a684',
					700: '#007c63',
					800: '#005342',
					900: '#002921',
				},
				green: {
					50: '#eaf7ea',
					100: '#d5efd5',
					200: '#abe0ab',
					300: '#80d080',
					400: '#56c156',
					500: '#2cb12c',
					600: '#238e23',
					700: '#1a6a1a',
					800: '#124712',
					900: '#092309',
				},
				yellow: {
					50: '#fff9e6',
					100: '#fff3cc',
					200: '#ffe799',
					300: '#ffdc66',
					400: '#ffd033',
					500: '#ffc400',
					600: '#cc9d00',
					700: '#997600',
					800: '#664e00',
					900: '#332700',
				},
				orange: {
					50: '#fff2e6',
					100: '#ffe5cc',
					200: '#ffcb99',
					300: '#ffb066',
					400: '#ff9633',
					500: '#ff7c00',
					600: '#cc6300',
					700: '#994a00',
					800: '#663200',
					900: '#331900',
				},
				red: {
					50: '#ffe8ea',
					100: '#ffd2d5',
					200: '#ffa4ab',
					300: '#ff7782',
					400: '#ff4958',
					500: '#ff1c2e',
					600: '#cc1625',
					700: '#99111c',
					800: '#660b12',
					900: '#330609',
				},
				rose: {
					50: '#ffeaef',
					100: '#ffd6df',
					200: '#ffadbf',
					300: '#ff839e',
					400: '#ff5a7e',
					400: '#ff315e',
					500: '#ff083e',
					600: '#cc0632',
					700: '#990525',
					800: '#660319',
					900: '#33020c',
				},
				pink: {
					50: '#fdeaf5',
					100: '#fcd5eb',
					200: '#f9aad6',
					300: '#f580c2',
					400: '#f255ad',
					500: '#ef2b99',
					600: '#bf227a',
					700: '#8f1a5c',
					800: '#60113d',
					900: '#30091f',
				},
				purple: {
					50: '#f3e8ff',
					100: '#e9d5ff',
					200: '#d8b4fe',
					300: '#c084fc',
					400: '#a855f7',
					500: '#7e22ce',
					600: '#7c3aed',
					700: '#6b21a8',
					800: '#581c87',
					900: '#4c1d95',
				},

				/* 主题颜色 - Primary（支持蓝色/紫色切换） */
				primary: {
					10: 'var(--primary-10)',
					50: 'var(--primary-50)',
					100: 'var(--primary-100)',
					200: 'var(--primary-200)',
					300: 'var(--primary-300)',
					400: 'var(--primary-400)',
					500: 'var(--primary-500)',
					600: 'var(--primary-600)',
					700: 'var(--primary-700)',
					800: 'var(--primary-800)',
					900: 'var(--primary-900)',
					DEFAULT: 'var(--primary-500)',
				},

				/* Element Plus 语义颜色 */
				success: {
					DEFAULT: 'var(--el-color-success)',
					light: 'var(--el-color-success-light-7)',
				},
				warning: {
					DEFAULT: 'var(--el-color-warning)',
					light: 'var(--el-color-warning-light-7)',
				},
				danger: {
					DEFAULT: 'var(--el-color-danger)',
					light: 'var(--el-color-danger-light-7)',
				},
				info: {
					DEFAULT: 'var(--el-color-info)',
					light: 'var(--el-color-info-light-7)',
				},

				/* Element Plus 文本颜色 */
				text: {
					primary: 'var(--el-text-color-primary)',
					regular: 'var(--el-text-color-regular)',
					secondary: 'var(--el-text-color-secondary)',
					placeholder: 'var(--el-text-color-placeholder)',
					disabled: 'var(--el-text-color-disabled)',
				},

				/* Element Plus 背景颜色 */
				bg: {
					DEFAULT: 'var(--el-bg-color)',
					page: 'var(--el-bg-color-page)',
					overlay: 'var(--el-bg-color-overlay)',
				},

				/* Element Plus 边框颜色 */
				border: {
					DEFAULT: 'var(--el-border-color)',
					light: 'var(--el-border-color-light)',
					lighter: 'var(--el-border-color-lighter)',
					hover: 'var(--el-border-color-hover)',
				},
			},

			/* ========== Typography 系统 ========== */
			fontSize: {
				// 标题层级
				'heading-1': [
					'var(--heading-1-size)',
					{
						lineHeight: 'var(--heading-1-line-height)',
						fontWeight: 'var(--heading-1-weight)',
					},
				],
				'heading-2': [
					'var(--heading-2-size)',
					{
						lineHeight: 'var(--heading-2-line-height)',
						fontWeight: 'var(--heading-2-weight)',
					},
				],
				'heading-3': [
					'var(--heading-3-size)',
					{
						lineHeight: 'var(--heading-3-line-height)',
						fontWeight: 'var(--heading-3-weight)',
					},
				],
				'heading-4': [
					'var(--heading-4-size)',
					{
						lineHeight: 'var(--heading-4-line-height)',
						fontWeight: 'var(--heading-4-weight)',
					},
				],
				'heading-5': [
					'var(--heading-5-size)',
					{
						lineHeight: 'var(--heading-5-line-height)',
						fontWeight: 'var(--heading-5-weight)',
					},
				],
				'heading-6': [
					'var(--heading-6-size)',
					{
						lineHeight: 'var(--heading-6-line-height)',
						fontWeight: 'var(--heading-6-weight)',
					},
				],

				// 正文层级
				xl: ['var(--text-xl-size)', { lineHeight: 'var(--text-xl-line-height)' }],
				lg: ['var(--text-lg-size)', { lineHeight: 'var(--text-lg-line-height)' }],
				base: ['var(--text-base-size)', { lineHeight: 'var(--text-base-line-height)' }],
				sm: ['var(--text-sm-size)', { lineHeight: 'var(--text-sm-line-height)' }],
				xs: ['var(--text-xs-size)', { lineHeight: 'var(--text-xs-line-height)' }],
			},

			/* ========== 字体家族 ========== */
			fontFamily: {
				sans: [
					'Satoshi',
					'HelveticaNow',
					'-apple-system',
					'BlinkMacSystemFont',
					'Segoe UI',
					'Roboto',
					'Helvetica Neue',
					'Arial',
					'sans-serif',
				],
			},

			/* ========== 圆角 ========== */
			borderRadius: {
				base: 'var(--el-border-radius-base)',
				small: 'var(--el-border-radius-small)',
				large: 'var(--el-border-radius-large)',
			},

			/* ========== 阴影 ========== */
			boxShadow: {
				el: 'var(--el-box-shadow)',
				'el-light': 'var(--el-box-shadow-light)',
			},
		},
	},

	/* ========== 深色模式 ========== */
	darkMode: 'class',

	/* ========== 插件 ========== */
	plugins: [
		import('tw-elements/dist/plugin.cjs'),
		function ({ addBase }) {
			addBase({
				'.el-button': {
					'background-color': 'var(--el-button-bg-color,var(--el-color-white))',
				},
			});
		},
	],
};
