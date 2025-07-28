declare module '*.svg';
declare module '*.png';
declare module '*.jpg';
declare module '*.jpeg';
declare module '*.gif';
declare module '*.bmp';
declare module '*.tiff';
declare module '*.css';
declare module '*.vue';

// unplugin-icons 模块声明
declare module '~icons/*' {
	import { FunctionalComponent, SVGAttributes } from 'vue';

	const component: FunctionalComponent<SVGAttributes>;
	export default component;
}

// linc配置信息
declare let linc: any;
