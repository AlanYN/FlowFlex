// ========================= Adobe Sign 类型定义 (OW-731) =========================

/** 签署顺序 */
export type AdobeSigningOrder = 'Sequential' | 'Parallel';

/** 签署人角色 */
export type AdobeSignerRole = 'Signer' | 'Approver' | 'CC';

/** 协议状态 */
export type AdobeAgreementStatus = 'Awaiting' | 'Completed' | 'Declined' | 'Expired' | 'Cancelled';

/** 单个签署人信息 */
export interface AdobeSigner {
	/** 签署人邮箱（必填） */
	email: string;
	/** 签署人姓名（必填） */
	name: string;
	/** 签署角色：Signer / Approver / CC */
	role: AdobeSignerRole;
	/** 签署顺序（Sequential 模式下使用，从 1 开始） */
	order: number;
	/** 当前签署状态（输出时有值） */
	status?: AdobeAgreementStatus;
	/** 完成签署的时间戳（输出时有值） */
	signedAt?: string | null;
}

/** 发起签署请求的输入参数 */
export interface RequestAdobeSignInput {
	/** 所属 Onboarding（Case）ID */
	onboardingId: string;
	/** 所属 Stage ID */
	stageId: string;
	/** 原始 PDF 文件 ID（ff_onboarding_file.id） */
	sourceFileId: string;
	/** 签署人列表（1-10 人） */
	signers: AdobeSigner[];
	/** 签署顺序：Sequential（逐一）或 Parallel（同时） */
	signingOrder: AdobeSigningOrder;
	/** 过期天数（7 / 14 / 30 / 60 / 90） */
	expirationDays: number;
	/** 发给签署人的说明消息（可选） */
	message?: string;
}

/** Adobe Sign 协议详情（输出） */
export interface AdobeSignAgreement {
	/** WFE 内部协议记录 ID */
	id: string;
	/** Adobe Sign 返回的 Agreement ID */
	agreementId: string;
	/** 所属 Onboarding ID */
	onboardingId: number;
	/** 所属 Stage ID */
	stageId: number;
	/** 原始 PDF 文件 ID */
	sourceFileId: number;
	/** 已签署 PDF 文件 ID（完成后有值） */
	signedFileId?: number | null;
	/** Audit Trail PDF 文件 ID（完成后有值） */
	auditTrailFileId?: number | null;
	/** 当前协议状态 */
	status: AdobeAgreementStatus;
	/** 签署人列表（含各自状态） */
	signers: AdobeSigner[];
	/** 签署顺序 */
	signingOrder: AdobeSigningOrder;
	/** 过期天数 */
	expirationDays: number;
	/** 给签署人的消息 */
	message?: string | null;
	/** 发起人姓名 */
	requestedByName?: string;
	/** 创建时间 */
	createDate: string;
	/** 完成时间（全部签署后有值） */
	completedDate?: string | null;
}
