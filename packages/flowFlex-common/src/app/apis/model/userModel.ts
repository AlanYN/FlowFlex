/**
 * @description: Login interface parameters
 */
export interface LoginParams {
	userName: string;
	password: string;
	channel: string;
}

export interface RoleInfo {
	roleName: string;
	value: string;
}

export interface SetUserInfoType {
	avatarUrl?: string;
	backgroundCheckDate?: string;
	birthday?: string;
	class?: string;
	contact?: number | string;
	createBy?: string;
	createDate?: string;
	email?: string;
	experience: number | string;
	firstName?: string;
	id?: string | number;
	lastName?: string;
	lastupBy?: string;
	lastupDate?: string;
	licenseClass?: number | string;
	location?: string;
	phone?: string;
	position: number[] | string;
	recruitType?: number | string;
	recruiterComments?: string;
	recruiterEmail?: string;
	shift: number | string;
	status: number | string;
	sysFiles?: any[];
	terminalId?: number | string;
	terminalCode?: string;
	terminalManagerComments?: string;
	managerComments?: string;
	yearsExperience?: string;
	recruiterContact?: string | null;
	qualifications?: number[];
	days?: number | string;
}

/**
 * @description: Login interface return value
 */
export interface LoginResultModel {
	userId: string | number;
	token: string;
	roles: string[];
}

/**
 * @description: 新的接口返回数据类型
 */
export interface UserInfoPost {
	access_token: string;
	expires_in: number;
	refresh_token: string;
	scope: string;
	token_type: string;
}

/**
 * @description: Get user information return value
 */
export interface GetUserInfoModel {
	stsatus: string;
	code: number;
	message: string;
	data: {
		roleIds: string[];
		// 用户id
		userId: string | number;
		firstName: string | null;
		lastName: string | null;
		email: string | null;
		// 用户名
		userName: string;
		// 真实名字
		realName: string;
		// 头像
		avatar: string;
		// 介绍
		desc?: string;

		tenantId?: number;
		homePath?: string;
		roles?: string[];
		companyIds?: Array<string | number>;
		tenants?: CompanyEnum;
		clientShortName: string;
		defaultTimeZone?: string;
	};
}

interface CompanyEnum {
	[key: number]: string;
}
