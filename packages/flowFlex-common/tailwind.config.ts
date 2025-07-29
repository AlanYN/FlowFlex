/** @type {import('tailwindcss').Config} */
import colors from 'tailwindcss/colors';
import tailwind_classes from './src/styles/design-system/tailwind-base.json';

export default {
	content: [
		'./public/index.html',
		'./src/**/*.{js,ts,vue}',
		'./node_modules/tw-elements/dist/js/**/*.js',
	],
	theme: {
		extend: {
			colors: {
				...tailwind_classes,
				transparent: 'transparent',
				current: 'currentColor',
				black: {
					DEFAULT: colors.black,
					...tailwind_classes.black,
				},
				white: {
					DEFAULT: colors.white,
					...tailwind_classes.white,
				},
				gray: {
					DEFAULT: colors.neutral,
					...tailwind_classes.gray,
				},
				siderbarGray: '#fff',
				mainGray: '#F9F9F9',
				customBlue: '#006ED0',
				disabledBgGray: '#ECECEC',
				disabledTextGray: '#989A9C',
				'black-5': 'var(--black-5)',
				'black-10': 'var(--black-10)',
				'black-20': 'var(--black-20)',
				'black-30': 'var(--black-30)',
				'black-40': 'var(--black-40)',
				'black-50': 'var(--black-50)',
				'black-60': 'var(--black-60)',
				'black-70': 'var(--black-70)',
				'black-80': 'var(--black-80)',
				'black-90': 'var(--black-90)',
				'black-100': 'var(--black-100)',
				'black-200': 'var(--black-200)',
				'black-300': 'var(--black-300)',
				'black-400': 'var(--black-400)',
				'black-500': 'var(--black-500)',
				'black-600': 'var(--black-600)',
				'black-700': 'var(--black-700)',
				'primary-10': 'var(--primary-10)',
				'primary-50': 'var(--primary-50)',
				'primary-100': 'var(--primary-100)',
				'primary-200': 'var(--primary-200)',
				'primary-300': 'var(--primary-300)',
				'primary-400': 'var(--primary-400)',
				'primary-500': 'var(--primary-500)',
				'primary-600': 'var(--primary-600)',
				'primary-700': 'var(--primary-700)',
				'primary-800': 'var(--primary-800)',
				'primary-900': 'var(--primary-900)',
				'light-gray': '#f6f6f6',
				144: '144px',
				165: '165px',
				'260px': '260px',
				textGreen: '#88B24E',
				textCount: '#262B24',
				darkGreen: '#698E6D',
				darkCount: '#FDFAE9',
				textTitle: '#547296',
			},
			fontFamily: {
				sans: ['Helvetica Now'],
			},
		},
	},
	darkMode: 'class',
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
