import type { LocaleType } from '@/../types/config';

import { set } from 'lodash-es';

export const loadLocalePool: LocaleType[] = [];

export function setHtmlPageLang(locale: LocaleType) {
	document.querySelector('html')?.setAttribute('lang', locale);
}

export function setLoadLocalePool(cb: (loadLocalePool: LocaleType[]) => void) {
	cb(loadLocalePool);
}

export function genMessage(langs: Record<string, Record<string, any>>, prefix = 'lang') {
	const obj: Recordable = {};
	Object.keys(langs).forEach((key) => {
		const langFileModule = langs[key];
		const fileName = key.replace(`./${prefix}/`, '').replace(/^\.\//, '');
		const keyList = fileName.split('/');
		const moduleName = keyList.shift();
		const objKey = keyList.join('.');

		if (moduleName) {
			if (objKey) {
				set(obj, moduleName, obj[moduleName] || {});
				set(obj[moduleName], objKey, langFileModule);
			} else {
				set(obj, moduleName, langFileModule || {});
			}
		}
	});
	return obj;
}
