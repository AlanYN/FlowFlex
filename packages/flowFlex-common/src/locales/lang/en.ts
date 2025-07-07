import { genMessage } from '../helper';

import en from 'element-plus/dist/locale/en.mjs';

import sysModules from './en/sys.json';
import chatWindowModules from './en/chatWindow.json';
import dynamicModules from './en/dynamic.json';

export default {
	message: {
		...genMessage(
			{
				sys: sysModules as Recordable<Recordable>,
				chatWindow: chatWindowModules as Recordable<Recordable>,
				dynamic: dynamicModules as Recordable<Recordable>,
			},
			'en'
		),
		en,
	},
	dateLocale: null,
	dateLocaleName: 'en',
};
