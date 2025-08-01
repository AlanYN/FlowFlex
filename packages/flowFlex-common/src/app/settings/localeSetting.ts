import type { LocaleSetting, LocaleType } from '@/../types/config';

type DropMenu = {
	text: string;
	event: string;
};

export const LOCALE: { [key: string]: LocaleType } = {
	ZH_CN: 'zh_CN',
	EN_US: 'en',
};

export const localeSetting: LocaleSetting = {
	showPicker: false,
	// Locale
	locale: LOCALE.EN_US,
	// Default locale
	fallback: LOCALE.EN_US,
	// available Locales
	availableLocales: [LOCALE.ZH_CN, LOCALE.EN_US],
};

// locale list
export const localeList: DropMenu[] = [
	{
		text: '简体中文',
		event: LOCALE.ZH_CN,
	},
	{
		text: 'English',
		event: LOCALE.EN_US,
	},
];
