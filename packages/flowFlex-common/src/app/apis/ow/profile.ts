import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';

const globSetting = useGlobSetting();

const Api = (signatureId?: string) => {
	return {
		// 用户签名相关 API
		signatures: `${globSetting.apiProName}/ow/profile/${globSetting.apiVersion}/signatures`,
		signature: `${globSetting.apiProName}/ow/profile/${globSetting.apiVersion}/signatures/${signatureId}`,
	};
};

// ========================= 类型定义 =========================

export interface SignatureItem {
	id: string; // snowflake long serialized as string
	imageBase64: string;
	createdDate: string; // ISO 8601
}

// ========================= 签名相关接口 =========================

/**
 * 获取当前用户的签名列表
 * @returns SignatureItem[]
 */
export function getSignatures() {
	return defHttp.get<SignatureItem[]>({
		url: Api().signatures,
	});
}

/**
 * 新增签名
 * @param imageBase64 签名图片的 Base64 编码字符串
 * @returns 新签名 ID（string）
 */
export function createSignature(imageBase64: string) {
	return defHttp.post<string>({
		url: Api().signatures,
		params: { imageBase64 },
	});
}

/**
 * 删除指定签名（软删除）
 * @param signatureId 签名 ID
 * @returns boolean
 */
export function deleteSignature(signatureId: string) {
	return defHttp.delete<boolean>({
		url: Api(signatureId).signature,
	});
}
