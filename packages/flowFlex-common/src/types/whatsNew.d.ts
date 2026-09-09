export interface WhatsNewPanelItem {
	id: string;
	title: string;
	summary: string;
	category: 'NewFeature' | 'Improvement' | 'BugFix' | 'Announcement';
	publishTime: string;
	isRead: boolean;
}

export interface WhatsNewPanelResponse {
	items: WhatsNewPanelItem[];
	unreadCount: number;
}

export interface WhatsNewDetail extends WhatsNewPanelItem {
	/** 原始 HTML，渲染前须 DOMPurify.sanitize() */
	content: string;
}

export interface WhatsNewAdminItem {
	id: string;
	title: string;
	summary: string;
	category: string;
	status: 0 | 1;
	publishTime: string | null;
	readCount: number;
}

export interface WhatsNewAdminListResponse {
	items: WhatsNewAdminItem[];
	publishedCount: number;
	draftCount: number;
}

export interface CreateWhatsNewRequest {
	title: string;
	summary: string;
	content: string;
	category: string;
	status: 0 | 1;
}

export interface UpdateWhatsNewRequest extends CreateWhatsNewRequest {}
