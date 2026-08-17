import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';

const globSetting = useGlobSetting();

const Api = (fileId?: string) => {
    return {
        // 文档签署相关 API
        signDocument: `${globSetting.apiProName}/ow/files/${globSetting.apiVersion}/${fileId}/sign`,
    };
};

// ========================= 类型定义 =========================

export interface SignDocumentResponse {
    signedFileId: string;
    downloadUrl: string;
    fileName: string;
    fileHash: string; // SHA-256 hex
}

// ========================= 文档签署相关接口 =========================

/**
 * 提交已签署的 PDF 文件到后端处理
 * @param fileId 原始文件 ID
 * @param formData 包含已签署 PDF 文件和签署元数据的 FormData
 *                 - file: 已签署 PDF 文件（Blob/File）
 *                 - signerName: 签署人姓名
 *                 - signedAt: 签署时间（ISO 8601 UTC）
 * @returns SignDocumentResponse
 */
export function signDocument(fileId: string, formData: FormData) {
    return defHttp.post<SignDocumentResponse>({
        url: Api(fileId).signDocument,
        params: formData,
        headers: {
            'Content-Type': 'multipart/form-data',
        },
    });
}
