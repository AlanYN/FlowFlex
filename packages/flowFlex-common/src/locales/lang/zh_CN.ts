import { genMessage } from '../helper';

import zhCn from 'element-plus/dist/locale/zh-cn.mjs';

import sysModules from './zh-CN/sys.json';
import chatWindowModules from './zh-CN/chatWindow.json';
import dynamicModules from './zh-CN/dynamic.json';

export default {
	message: {
		...genMessage(
			{
				sys: sysModules as Recordable<Recordable>,
				chatWindow: chatWindowModules as Recordable<Recordable>,
				dynamic: dynamicModules as Recordable<Recordable>,
			},
			'zh-CN'
		),
		antdLocale: {
			...zhCn,
		},
		dateLocale: null,
		dateLocaleName: 'zh-CN',
	},
};
