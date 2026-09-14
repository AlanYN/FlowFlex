import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';
import type { AdobeSignAgreementApi, AdobeSignAgreement, RequestAdobeSignInput } from '#/adobeSign';

const globSetting = useGlobSetting();

const Api = (id?: string | number) => {
	const base = `${globSetting.apiProName}/ow/adobe-sign/${globSetting.apiVersion}`;
	return {
		request: `${base}/request`,
		getById: `${base}/${id}`,
		getByFile: `${base}/by-file/${id}`,
		pending: `${base}/pending/${id}`,
		remind: `${base}/${id}/remind`,
		recall: `${base}/${id}`,
	};
};

// ========================= 发起签署请求 =========================

/**
 * 发起 Adobe Sign 电子签署请求
 * 上传 PDF 到 Adobe Sign，创建协议并向所有签署人发送签署邮件
 * @param data 签署请求配置（签署人列表、顺序、过期时间等）
 * @returns 创建成功的协议详情
 */
export function requestAdobeSign(data: RequestAdobeSignInput) {
	return defHttp.post<AdobeSignAgreementApi<AdobeSignAgreement>>({
		url: Api().request,
		params: data,
	});
}

// ========================= 查询协议 =========================

/**
 * 按内部 WFE ID 查询协议详情
 * @param id WFE 内部协议记录 ID
 */
export function getAgreement(id: string | number) {
	return defHttp.get<AdobeSignAgreementApi<AdobeSignAgreement>>({
		url: Api(id).getById,
	});
}

/**
 * 按原始文件 ID 查询关联的签署协议
 * 用于文件列表展示签署状态，无协议时返回 null
 * @param sourceFileId 原始 PDF 文件 ID（ff_onboarding_file.id）
 */
export function getAgreementByFileId(sourceFileId: string | number) {
	return defHttp.get<AdobeSignAgreementApi<AdobeSignAgreement | null>>({
		url: Api(sourceFileId).getByFile,
	});
}

// ========================= 发送提醒 =========================

/**
 * 向未签署的签署人发送提醒邮件
 * 仅对 Awaiting 状态的协议有效
 * @param id WFE 内部协议记录 ID
 * @param signerEmails 需要提醒的签署人邮箱列表
 */
export function sendReminder(id: string | number, signerEmails: string[]) {
	return defHttp.post<AdobeSignAgreementApi<boolean>>({
		url: Api(id).remind,
		params: { signerEmails },
	});
}

// ========================= 撤回协议 =========================

/**
 * 撤回（取消）进行中的签署请求
 * 所有待签署人将无法继续签署，此操作不可撤销
 * 仅对 Awaiting 状态的协议有效
 * @param id WFE 内部协议记录 ID
 */
export function recallAgreement(id: string | number) {
	return defHttp.delete<AdobeSignAgreementApi<boolean>>({
		url: Api(id).recall,
	});
}

// ========================= Force Complete 前置检查 =========================

export interface PendingSignatureItem {
	agreementId: string;
	fileName: string;
	stageName: string;
	pendingSignerCount: number;
	totalSignerCount: number;
	createdAt: string;
}

/**
 * 查询某个 Case 下待签署（Awaiting）的协议列表，含文件名、阶段名、签名人进度
 * 用于 Force Complete 前弹窗提示用户哪些文件仍在等待签名
 */
export function getPendingSignatures(onboardingId: string | number) {
	return defHttp.get<AdobeSignAgreementApi<PendingSignatureItem[]>>({
		url: Api(onboardingId).pending,
	});
}
