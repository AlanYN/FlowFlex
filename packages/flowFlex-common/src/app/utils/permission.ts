import { intersection } from 'lodash-es';
import { menuRoles } from '@/stores/modules/menuFunction';

const menuRolesPer = menuRoles();
/**
 * 是否展示按钮
 * @param codes 权限codes
 * @returns
 */
export const showPermissionBtn = (codes: string | string[]) => {
	if (codes instanceof Array) {
		// 获取交集长度
		return intersection(menuRolesPer?.getFunctionIds, codes)?.length > 0;
	} else {
		return menuRolesPer?.getFunctionIds?.includes(codes);
	}
};
