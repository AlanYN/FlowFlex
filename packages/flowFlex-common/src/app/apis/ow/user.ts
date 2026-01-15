import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';

const globSetting = useGlobSetting();

const Api = () => {
	return {
		// 用户相关API
		checkEmail: `${globSetting.apiProName}/ow/users/${globSetting.apiVersion}/check-email`,
		register: `${globSetting.apiProName}/ow/users/${globSetting.apiVersion}/register`,
		login: `${globSetting.apiProName}/ow/users/${globSetting.apiVersion}/login`,
		sendVerificationCode: `${globSetting.apiProName}/ow/users/${globSetting.apiVersion}/send-verification-code`,
		verifyEmail: `${globSetting.apiProName}/ow/users/${globSetting.apiVersion}/verify-email`,
		currentUser: `${globSetting.apiProName}/ow/user/${globSetting.apiVersion}s/current`,
		userTree: `${globSetting.apiProName}/ow/users/${globSetting.apiVersion}/tree`,
	};
};

// 检查邮箱是否存在
export function checkEmailExists(email: string) {
	return defHttp.get({
		url: Api().checkEmail,
		params: { email },
	});
}

// 用户注册
export function registerUser(params: {
	email: string;
	password: string;
	confirmPassword: string;
	verificationCode: string;
}) {
	return defHttp.post({
		url: Api().register,
		params,
	});
}

// 用户登录
export function loginUser(params: { email: string; password: string }) {
	return defHttp.post({
		url: Api().login,
		params,
	});
}

// 发送验证码
export function sendVerificationCode(params: { email: string }) {
	return defHttp.post({
		url: Api().sendVerificationCode,
		params,
	});
}

// 验证邮箱
export function verifyEmail(params: { email: string; verificationCode: string }) {
	return defHttp.post({
		url: Api().verifyEmail,
		params,
	});
}

// 获取当前用户信息
export function getCurrentUser() {
	return defHttp.get({
		url: Api().currentUser,
	});
}

// Portal用户自动注册和登录（不需要验证码）
export function portalAutoRegisterAndLogin(params: {
	email: string;
	password: string;
	onboardingId: string;
}) {
	return defHttp.post({
		url: Api().register,
		params: {
			email: params.email,
			password: params.password,
			confirmPassword: params.password,
			verificationCode: '000000', // Portal用户不需要验证码验证
			skipEmailVerification: true, // 特殊标记，表示portal用户跳过邮箱验证
		},
	});
}

// 获取用户树（包含团队和用户的层级结构）
export function getUserTree() {
	return defHttp.get({
		url: Api().userTree,
	});
}
